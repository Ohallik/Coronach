// Coronach's opt-in D3D12 camera timestamp diagnostic. This does not replace
// Unity's raw counters or establish physical presentation quality.
#include <windows.h>
#include <d3d12.h>
#include <dxgi.h>
#include <stdint.h>
#include "IUnityGraphics.h"
#include "IUnityGraphicsD3D12.h"

namespace {
constexpr int Capacity=64, SignalStart=1+Capacity*2, EventCount=1+Capacity*3;
constexpr UINT64 TagPrefix=0xC0A0CAFE00000000ULL;
struct Sample {
    int32_t frame,status;
    UINT64 begin,end,frequency,fenceRequired,fenceCompleted,tag;
    INT64 beginCpuQpc,endCpuQpc;
};
static_assert(sizeof(Sample)==72,"managed sample layout");
struct Slot {int state;Sample sample;}; // free, reserved, begin recorded, end recorded, signaled
SRWLOCK mutex=SRWLOCK_INIT;
struct Lock {Lock(){AcquireSRWLockExclusive(&mutex);}~Lock(){ReleaseSRWLockExclusive(&mutex);}};
IUnityGraphics* graphics=nullptr;
IUnityGraphicsD3D12v7* api=nullptr;
ID3D12QueryHeap* heap=nullptr;
ID3D12Resource* readback=nullptr;
ID3D12Resource* upload=nullptr;
ID3D12Fence* completionFence=nullptr;
UINT64* readWords=nullptr;
UINT64* uploadWords=nullptr;
UINT64 frequency=0,nextFence=1;
Slot slots[Capacity]{};
int eventBase=0,state=0,cursor=0;
HRESULT lastResult=S_OK;
void Fail(int code,HRESULT hr=S_OK){if(state>=0){state=-code;lastResult=hr;}}
INT64 Qpc(){LARGE_INTEGER value{};QueryPerformanceCounter(&value);return value.QuadPart;}
D3D12_RESOURCE_DESC Buffer(UINT64 count){
    D3D12_RESOURCE_DESC desc{};desc.Dimension=D3D12_RESOURCE_DIMENSION_BUFFER;
    desc.Width=count;desc.Height=1;desc.DepthOrArraySize=1;desc.MipLevels=1;
    desc.SampleDesc.Count=1;desc.Layout=D3D12_TEXTURE_LAYOUT_ROW_MAJOR;return desc;
}
bool Resource(ID3D12Device* device,D3D12_HEAP_TYPE type,D3D12_RESOURCE_STATES initial,ID3D12Resource** output){
    D3D12_HEAP_PROPERTIES props{};props.Type=type;props.CreationNodeMask=props.VisibleNodeMask=1;
    auto desc=Buffer(Capacity*32);
    HRESULT hr=device->CreateCommittedResource(&props,D3D12_HEAP_FLAG_NONE,&desc,initial,nullptr,
        __uuidof(ID3D12Resource),reinterpret_cast<void**>(output));
    if(FAILED(hr)){Fail(4,hr);return false;}return true;
}
// This event is configured for the submission thread. It alone accesses the
// command queue. Timestamp events instead use Unity's current recording list.
void Initialize(){
    if(state!=0)return;
    if(!graphics||graphics->GetRenderer()!=kUnityGfxRendererD3D12||!api){Fail(1);return;}
    auto device=api->GetDevice();auto queue=api->GetCommandQueue();
    if(!device||!queue){Fail(2);return;}
    HRESULT hr=queue->GetTimestampFrequency(&frequency);
    if(FAILED(hr)||frequency==0){Fail(3,hr);return;}
    D3D12_QUERY_HEAP_DESC query{};query.Count=Capacity*2;query.Type=D3D12_QUERY_HEAP_TYPE_TIMESTAMP;
    hr=device->CreateQueryHeap(&query,__uuidof(ID3D12QueryHeap),reinterpret_cast<void**>(&heap));
    if(FAILED(hr)){Fail(4,hr);return;}
    if(!Resource(device,D3D12_HEAP_TYPE_READBACK,D3D12_RESOURCE_STATE_COPY_DEST,&readback)||
       !Resource(device,D3D12_HEAP_TYPE_UPLOAD,D3D12_RESOURCE_STATE_GENERIC_READ,&upload))return;
    D3D12_RANGE readRange{0,Capacity*32},noReads{0,0};
    hr=readback->Map(0,&readRange,reinterpret_cast<void**>(&readWords));
    if(FAILED(hr)){Fail(5,hr);return;}
    hr=upload->Map(0,&noReads,reinterpret_cast<void**>(&uploadWords));
    if(FAILED(hr)){Fail(5,hr);return;}
    hr=device->CreateFence(0,D3D12_FENCE_FLAG_NONE,__uuidof(ID3D12Fence),reinterpret_cast<void**>(&completionFence));
    if(FAILED(hr)){Fail(6,hr);return;}state=1;
}
void UNITY_INTERFACE_API RenderEvent(int event){
    Lock lock;
    int index=event-eventBase;
    if(index==0){Initialize();return;}
    if(state!=1||index<1||index>=EventCount)return;
    if(index>=SignalStart){
        auto& slot=slots[index-SignalStart];
        if(slot.state!=3){Fail(17);return;}
        // This submission-thread event flushes the timestamp/copy command
        // buffers first. Its private queue fence identifies this exact work;
        // Unity's next-frame fence can repeat while another list is recorded.
        auto queue=api->GetCommandQueue();if(!queue){Fail(2);return;}
        UINT64 value=nextFence++;
        HRESULT hr=queue->Signal(completionFence,value);
        if(FAILED(hr)){Fail(18,hr);return;}
        slot.sample.fenceRequired=value;slot.state=4;return;
    }
    int slotIndex=(index-1)/2;bool begin=((index-1)%2)==0;auto& slot=slots[slotIndex];
    UnityGraphicsD3D12RecordingState recording{};
    if(!api->CommandRecordingState(&recording)||!recording.commandList){Fail(7);return;}
    if(begin){
        if(slot.state!=1){Fail(8);return;}
        slot.sample.beginCpuQpc=Qpc();
        recording.commandList->EndQuery(heap,D3D12_QUERY_TYPE_TIMESTAMP,slotIndex*2);
        slot.state=2;
    }else{
        if(slot.state!=2){Fail(9);return;}
        auto list=recording.commandList;
        list->EndQuery(heap,D3D12_QUERY_TYPE_TIMESTAMP,slotIndex*2+1);
        list->ResolveQueryData(heap,D3D12_QUERY_TYPE_TIMESTAMP,slotIndex*2,2,readback,slotIndex*32);
        // The GPU writes an identity tag after the two query results. A stale
        // readback cannot be mistaken for this frame merely because a fence or
        // ring slot was reused. The upload slot stays owned until completion.
        list->CopyBufferRegion(readback,slotIndex*32+16,upload,slotIndex*32,8);
        slot.sample.endCpuQpc=Qpc();
        slot.state=3;
    }
}
void Release(){
    if(readback){if(readWords)readback->Unmap(0,nullptr);readback->Release();}
    if(upload){if(uploadWords)upload->Unmap(0,nullptr);upload->Release();}
    if(heap)heap->Release();if(completionFence)completionFence->Release();
    readback=nullptr;upload=nullptr;heap=nullptr;completionFence=nullptr;readWords=uploadWords=nullptr;
}
void UNITY_INTERFACE_API DeviceEvent(UnityGfxDeviceEventType type){
    if(type==kUnityGfxDeviceEventShutdown){Lock lock;Release();Fail(11);}
}
}
extern "C" {
UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces){
    graphics=interfaces->Get<IUnityGraphics>();api=interfaces->Get<IUnityGraphicsD3D12v7>();
    if(!graphics||!api){Fail(1);return;}
    eventBase=graphics->ReserveEventIDRange(EventCount);
    UnityD3D12PluginEventConfig setup{kUnityD3D12GraphicsQueueAccess_Allow,0,false};
    UnityD3D12PluginEventConfig timestamp{kUnityD3D12GraphicsQueueAccess_DontCare,0,false};
    api->ConfigureEvent(eventBase,&setup);
    UnityD3D12PluginEventConfig signal{kUnityD3D12GraphicsQueueAccess_Allow,
        kUnityD3D12EventConfigFlag_FlushCommandBuffers|kUnityD3D12EventConfigFlag_SyncWorkerThreads,false};
    for(int i=1;i<EventCount;i++)api->ConfigureEvent(eventBase+i,i<SignalStart?&timestamp:&signal);
    graphics->RegisterDeviceEventCallback(DeviceEvent);
}
UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginUnload(){
    if(graphics)graphics->UnregisterDeviceEventCallback(DeviceEvent);
    Lock lock;Release();
}
UNITY_INTERFACE_EXPORT UnityRenderingEvent UNITY_INTERFACE_API GpuClockEvent(){return RenderEvent;}
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GpuClockEventBase(){return eventBase;}
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GpuClockSignalEvent(int beginEvent){
    int index=beginEvent-eventBase-1;
    return index>=0&&index<Capacity*2&&index%2==0?eventBase+SignalStart+index/2:-1;
}
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GpuClockState(){Lock lock;return state;}
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GpuClockLastResult(){Lock lock;return static_cast<int>(lastResult);}
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GpuClockPrepare(int frame){
    Lock lock;if(state!=1)return -1;
    if(frame<=0){Fail(12);return -1;}
    for(int i=0;i<Capacity;i++){
        int index=(cursor+i)%Capacity;auto& slot=slots[index];
        if(slot.state)continue;
        slot.sample={};slot.sample.frame=frame;slot.sample.frequency=frequency;slot.state=1;
        uploadWords[index*4]=TagPrefix|static_cast<uint32_t>(frame);
        cursor=(index+1)%Capacity;return eventBase+1+index*2;
    }
    Fail(13);return -1; // Retain overflow, never block the renderer waiting on GPU.
}
UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GpuClockPop(Sample* output){
    Lock lock;if(!completionFence||!output)return 0;
    UINT64 completed=completionFence->GetCompletedValue();
    if(completed==UINT64_MAX){Fail(14);return 0;}
    int oldest=-1;
    for(int i=0;i<Capacity;i++)if(slots[i].state==4&&slots[i].sample.fenceRequired<=completed&&
        (oldest<0||slots[i].sample.frame<slots[oldest].sample.frame))oldest=i;
    if(oldest<0)return 0;
    auto& slot=slots[oldest];MemoryBarrier();
    slot.sample.begin=readWords[oldest*4];slot.sample.end=readWords[oldest*4+1];
    slot.sample.tag=readWords[oldest*4+2];slot.sample.fenceCompleted=completed;
    if(slot.sample.tag!=(TagPrefix|static_cast<uint32_t>(slot.sample.frame))){slot.sample.status=15;Fail(15);}
    else if(!slot.sample.begin||slot.sample.end<=slot.sample.begin){slot.sample.status=16;Fail(16);}
    *output=slot.sample;slot.state=0;return 1;
}
}
