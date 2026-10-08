using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.UI
{
    // Diagnostic main-camera envelope, not GPU busy time or scan-out. Raw Unity
    // timings remain separate, including every rejected value. Native query
    // identities and fences must match every scheduled replay frame.
    public sealed class NativeGpuClock:MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)] struct NativeSample
        {
            public int frame,status;
            public ulong begin,end,frequency,fenceRequired,fenceCompleted,tag;
            public long beginCpuQpc,endCpuQpc;
        }
        const string Library="CoronachGpuClock";
        [DllImport(Library,CallingConvention=CallingConvention.StdCall)] static extern IntPtr GpuClockEvent();
        [DllImport(Library,CallingConvention=CallingConvention.StdCall)] static extern int GpuClockEventBase();
        [DllImport(Library,CallingConvention=CallingConvention.StdCall)] static extern int GpuClockState();
        [DllImport(Library,CallingConvention=CallingConvention.StdCall)] static extern int GpuClockSignalEvent(int beginEvent);
        [DllImport(Library,CallingConvention=CallingConvention.StdCall)] static extern int GpuClockLastResult();
        [DllImport(Library,CallingConvention=CallingConvention.StdCall)] static extern int GpuClockPrepare(int frame);
        [DllImport(Library,CallingConvention=CallingConvention.StdCall)] static extern int GpuClockPop(out NativeSample sample);
        IntPtr callback;
        CommandBuffer command;
        StreamWriter identities,results;
        string folder;
        bool initialized,setupSent,recording,finished;
        int eventBase,markedFrame=-1,queuedFrame=-1,pendingEvent=-1,submitted,received;
        public string Failure {get;private set;}
        public bool Ready => initialized&&GpuClockState()==1;
        void Reject(string reason){Failure??=reason;}
        public void Initialize(string output)
        {
            folder=output;
            try
            {
                if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Direct3D12)
                    throw new InvalidOperationException("Native GPU diagnostic requires Direct3D12");
                callback=GpuClockEvent();eventBase=GpuClockEventBase();
                if(callback==IntPtr.Zero)throw new InvalidOperationException("Native GPU callback unavailable");
                command=new CommandBuffer{name="Coronach diagnostic GPU timestamps"};
                RenderPipelineManager.beginCameraRendering+=BeginCamera;
                RenderPipelineManager.endCameraRendering+=EndCamera;
                initialized=true;
            }
            catch(Exception e){Reject(e.GetType().Name+": "+e.Message);}
        }
        void Send(ScriptableRenderContext context,int id,bool submit=false)
        {
            command.Clear();command.IssuePluginEvent(callback,id);context.ExecuteCommandBuffer(command);
            if(submit)context.Submit();
        }
        void BeginCamera(ScriptableRenderContext context,Camera camera)
        {
            if(camera!=Camera.main||finished)return;
            if(!setupSent){Send(context,eventBase);setupSent=true;return;}
            CheckState();
            if(!recording||markedFrame!=Time.frameCount||Failure!=null)return;
            if(queuedFrame==Time.frameCount){Reject("Main camera rendered twice for one replay frame");return;}
            pendingEvent=GpuClockPrepare(Time.frameCount);queuedFrame=Time.frameCount;
            if(pendingEvent<0){CheckState();Reject("Native GPU slot unavailable");return;}
            submitted++;Send(context,pendingEvent);
        }
        void EndCamera(ScriptableRenderContext context,Camera camera)
        {
            if(camera!=Camera.main||pendingEvent<0)return;
            if(queuedFrame!=Time.frameCount){Reject("Native GPU camera end did not match begin");return;}
            // URP may already have submitted this camera. Submit the ending
            // query explicitly so it cannot drift into the next frame. This
            // extra submission and CPU gaps make this diagnostic-only evidence.
            Send(context,pendingEvent+1);
            Send(context,GpuClockSignalEvent(pendingEvent),true);pendingEvent=-1;
        }
        void CheckState()
        {
            if(!initialized)return;
            int state=GpuClockState();
            if(state<0)Reject("Native GPU failure "+state+", HRESULT 0x"+GpuClockLastResult().ToString("X8"));
        }
        public void Begin()
        {
            if(!Ready)Reject("Native GPU setup did not become ready during settle");
            identities=new StreamWriter(Path.Combine(folder,"native-gpu-frames.csv"),false,new System.Text.UTF8Encoding(false),65536);
            results=new StreamWriter(Path.Combine(folder,"native-gpu-results.csv"),false,new System.Text.UTF8Encoding(false),65536);
            identities.WriteLine("sample,frame,step,elapsed");
            results.WriteLine("frame,status,begin,end,frequency,fenceRequired,fenceCompleted,tag,beginCpuQpc,endCpuQpc");
            recording=true;
        }
        public void Mark(int sample,int frame,int step,double elapsed)
        {
            markedFrame=frame;
            identities.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3:R}",sample,frame,step,elapsed));
            Drain();
        }
        void Drain()
        {
            if(!initialized)return;
            while(GpuClockPop(out var r)!=0)
            {
                received++;
                results.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                    r.frame,r.status,r.begin,r.end,r.frequency,r.fenceRequired,r.fenceCompleted,r.tag,r.beginCpuQpc,r.endCpuQpc));
                if(r.status!=0)Reject("Native GPU result rejected with status "+r.status);
            }
            CheckState();
        }
        [Serializable] sealed class Report
        {
            public int schema=2,submitted,received;
            public string fenceKind="private_queue_fence";
            public bool complete;
            public string failure;
            public string method="D3D12 bottom-of-pipe timestamps around main-camera callbacks; explicit ending submission and private queue fence; includes CPU submission gaps, excludes later overlays/present; diagnostic only";
        }
        public IEnumerator Finish()
        {
            recording=false;
            float deadline=Time.realtimeSinceStartup+5;
            while(received<submitted&&Time.realtimeSinceStartup<deadline){Drain();yield return null;}
            Drain();finished=true;
            if(received!=submitted)Reject("Native GPU finalization has missing results");
            if(submitted==0)Reject("Native GPU capture empty");
            identities?.Dispose();results?.Dispose();
            File.WriteAllText(Path.Combine(folder,"native-gpu.json"),JsonUtility.ToJson(new Report{
                submitted=submitted,received=received,complete=received==submitted&&Failure==null,failure=Failure},true));
        }
        void OnDestroy()
        {
            recording=false;
            RenderPipelineManager.beginCameraRendering-=BeginCamera;
            RenderPipelineManager.endCameraRendering-=EndCamera;
            command?.Dispose();identities?.Dispose();results?.Dispose();
        }
    }
}
