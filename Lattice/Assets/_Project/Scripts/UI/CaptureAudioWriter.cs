using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Lattice.UI
{
    /// <summary>Bounded listener recording. The audio callback only copies into
    /// preallocated slots; a worker owns file IO. Overflow is a retained failure.</summary>
    public sealed class CaptureAudioWriter
    {
        readonly Stream stream;
        readonly int sampleRate, blockSamples;
        readonly long maximumDataBytes;
        readonly float[][] slots;
        readonly int[] lengths;
        readonly byte[] bytes;
        readonly object gate=new();
        int head,tail,queued,channels,peakQueued;
        long accepted,written;
        bool accepting=true;
        volatile bool completed;
        string failure;
        public string Failure => Volatile.Read(ref failure);
        public bool Completed => completed;
        public int BufferedBlocks { get { lock(gate)return queued; } }
        public long WrittenSamples => Interlocked.Read(ref written);

        public CaptureAudioWriter(Stream output,int rate,int bufferCount=32,int maximumBlockSamples=32768,
            long dataByteLimit=uint.MaxValue-36L)
        {
            if(output==null||!output.CanWrite||!output.CanSeek)throw new ArgumentException("Seekable output is required");
            if(rate<=0||bufferCount<=0||maximumBlockSamples<=0||dataByteLimit<4||dataByteLimit>uint.MaxValue-36L)
                throw new ArgumentOutOfRangeException(nameof(rate));
            stream=output;sampleRate=rate;blockSamples=maximumBlockSamples;maximumDataBytes=dataByteLimit;
            slots=new float[bufferCount][];lengths=new int[bufferCount];bytes=new byte[checked(blockSamples*4)];
            for(int i=0;i<slots.Length;i++)slots[i]=new float[blockSamples];
            stream.Write(new byte[44],0,44);
            new Thread(Write){IsBackground=true,Name="Quality listener writer"}.Start();
        }
        void Reject(string reason)=>Interlocked.CompareExchange(ref failure,reason,null);
        public bool Append(float[] samples,int channelCount)
        {
            lock(gate)
            {
                // A callback already in flight at Finish is allowed to stop.
                if(!accepting)return false;
                if(samples==null||samples.Length==0||channelCount<=0||channelCount>32||samples.Length%channelCount!=0)
                {Reject("Invalid listener block");return false;}
                if(channels!=0&&channels!=channelCount){Reject("Listener channel count changed");return false;}
                if(samples.Length>blockSamples){Reject("Listener block exceeds capture buffer");return false;}
                if(queued==slots.Length){Reject("Listener capture queue overflow; audio incomplete");return false;}
                if((accepted+samples.Length)*4>maximumDataBytes){Reject("Listener WAV size limit; audio incomplete");return false;}
                for(int i=0;i<samples.Length;i++)if(float.IsNaN(samples[i])||float.IsInfinity(samples[i]))
                {Reject("Nonfinite listener sample");return false;}
                channels=channelCount;
                Array.Copy(samples,slots[tail],samples.Length);lengths[tail]=samples.Length;
                tail=(tail+1)%slots.Length;queued++;accepted+=samples.Length;peakQueued=Math.Max(peakQueued,queued);
                return true;
            }
        }
        public void Complete(){lock(gate)accepting=false;}
        void Write()
        {
            try
            {
                while(true)
                {
                    int count;float[] samples;
                    lock(gate)
                    {
                        if(queued==0&&!accepting)break;
                        count=queued==0?0:lengths[head];samples=slots[head];
                    }
                    if(count==0){Thread.Sleep(2);continue;}
                    Buffer.BlockCopy(samples,0,bytes,0,count*4);
                    stream.Write(bytes,0,count*4);
                    Interlocked.Add(ref written,count);
                    lock(gate){head=(head+1)%slots.Length;queued--;}
                }
                if(written==0)Reject("Listener mix capture empty");
                if(written!=accepted)Reject("Listener samples missing at finalization");
                stream.Position=0;
                using var header=new BinaryWriter(stream,Encoding.ASCII,true);
                header.Write(Encoding.ASCII.GetBytes("RIFF"));header.Write((uint)(36+written*4));
                header.Write(Encoding.ASCII.GetBytes("WAVEfmt "));header.Write(16);header.Write((short)3);
                header.Write((short)channels);header.Write(sampleRate);header.Write(sampleRate*channels*4);
                header.Write((short)(channels*4));header.Write((short)32);
                header.Write(Encoding.ASCII.GetBytes("data"));header.Write((uint)(written*4));header.Flush();
                stream.Flush();
            }
            catch(Exception e){Reject("Listener writer: "+e.GetType().Name+": "+e.Message);}
            finally
            {
                try{stream.Dispose();}catch(Exception e){Reject("Listener close: "+e.Message);}
                lock(gate)accepting=false;
                completed=true;
            }
        }
        [Serializable] public sealed class Report
        {
            public int sampleRate,channels,bufferCount,maximumBlockSamples,peakQueuedBlocks;
            public long sampleBufferBytes,acceptedSamples,writtenSamples,writtenBytes;
            public bool completed;
            public string failure;
        }
        public Report Snapshot()
        {
            lock(gate)return new Report{sampleRate=sampleRate,channels=channels,bufferCount=slots.Length,
                maximumBlockSamples=blockSamples,peakQueuedBlocks=peakQueued,
                sampleBufferBytes=(long)(slots.Length+1)*blockSamples*4,acceptedSamples=accepted,
                writtenSamples=WrittenSamples,writtenBytes=WrittenSamples*4,completed=Completed,failure=Failure};
        }
    }
}
