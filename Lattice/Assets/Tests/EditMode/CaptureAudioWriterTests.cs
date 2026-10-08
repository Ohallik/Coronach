using System;
using System.IO;
using System.Threading;
using Lattice.UI;
using NUnit.Framework;

namespace Lattice.Tests.EditMode
{
    public sealed class CaptureAudioWriterTests
    {
        static void Wait(Func<bool> condition,string label)
        {Assert.IsTrue(SpinWait.SpinUntil(condition,5000),label);}
        static byte[] Finish(CaptureAudioWriter writer,MemoryStream stream)
        {writer.Complete();Wait(()=>writer.Completed,"writer did not close");return stream.ToArray();}
        static void CheckHeader(byte[] wav,int channels,int count)
        {
            using var read=new BinaryReader(new MemoryStream(wav));
            Assert.AreEqual("RIFF",new string(read.ReadChars(4)));Assert.AreEqual(36+count*4,read.ReadUInt32());
            Assert.AreEqual("WAVEfmt ",new string(read.ReadChars(8)));Assert.AreEqual(16,read.ReadInt32());
            Assert.AreEqual(3,read.ReadInt16());Assert.AreEqual(channels,read.ReadInt16());Assert.AreEqual(48000,read.ReadInt32());
            Assert.AreEqual(48000*channels*4,read.ReadInt32());Assert.AreEqual(channels*4,read.ReadInt16());Assert.AreEqual(32,read.ReadInt16());
            Assert.AreEqual("data",new string(read.ReadChars(4)));Assert.AreEqual(count*4,read.ReadUInt32());
            Assert.AreEqual(44+count*4,wav.Length);
        }
        sealed class BlockedStream:MemoryStream
        {
            public readonly ManualResetEventSlim entered=new(),release=new();
            public bool throwOnData;
            public override void Write(byte[] buffer,int offset,int count)
            {
                if(Position>=44)
                {
                    entered.Set();if(!release.Wait(5000))throw new IOException("fixture write gate timeout");
                    if(throwOnData)throw new IOException("fixture disk full");
                }
                base.Write(buffer,offset,count);
            }
        }
        sealed class CountingStream:Stream
        {
            long position,length;
            public override bool CanRead=>false;public override bool CanWrite=>true;public override bool CanSeek=>true;
            public override long Length=>length;public override long Position{get=>position;set=>position=value;}
            public override void Write(byte[] b,int o,int n){position+=n;length=Math.Max(length,position);}
            public override void Flush(){}public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();
            public override void SetLength(long n)=>throw new NotSupportedException();public override int Read(byte[] b,int o,int n)=>throw new NotSupportedException();
        }
        [Test] public void CopiesEveryFloatInOrderAndDrainsAfterCompletion()
        {
            var stream=new BlockedStream();var writer=new CaptureAudioWriter(stream,48000,4,8);
            try
            {
                var samples=new[]{-.75f,.5f,-.25f,.125f};
                Assert.IsTrue(writer.Append(samples,2));Assert.IsTrue(stream.entered.Wait(2000));
                samples[0]=.875f;Assert.IsTrue(writer.Append(samples,2));samples[0]=.0625f;Assert.IsTrue(writer.Append(samples,2));
                Array.Clear(samples,0,samples.Length);writer.Complete();stream.release.Set();
                var wav=Finish(writer,stream);CheckHeader(wav,2,12);
                float[] expected={-.75f,.5f,-.25f,.125f,.875f,.5f,-.25f,.125f,.0625f,.5f,-.25f,.125f};
                for(int i=0;i<expected.Length;i++)Assert.AreEqual(expected[i],BitConverter.ToSingle(wav,44+i*4),"sample "+i);
                Assert.IsNull(writer.Failure);Assert.AreEqual(12,writer.Snapshot().acceptedSamples);Assert.AreEqual(12,writer.WrittenSamples);
                Assert.IsFalse(writer.Append(samples,2),"closed writer accepted late callback");
            }
            finally{stream.release.Set();writer.Complete();Wait(()=>writer.Completed,"cleanup");}
        }
        [Test] public void FullQueueRejectsWithoutDiscardingPreviouslyAcceptedAudio()
        {
            var stream=new BlockedStream();var writer=new CaptureAudioWriter(stream,48000,2,8);
            try
            {
                var samples=new float[8];Assert.IsTrue(writer.Append(samples,2));Assert.IsTrue(stream.entered.Wait(2000));
                Assert.IsTrue(writer.Append(samples,2));Assert.IsFalse(writer.Append(samples,2));
                Assert.That(writer.Failure,Does.Contain("overflow"));Assert.AreEqual(2,writer.Snapshot().peakQueuedBlocks);
                writer.Complete();stream.release.Set();var wav=Finish(writer,stream);CheckHeader(wav,2,16);
                Assert.That(writer.Failure,Does.Contain("overflow"));Assert.AreEqual(16,writer.WrittenSamples);
            }
            finally{stream.release.Set();writer.Complete();Wait(()=>writer.Completed,"cleanup");}
        }
        [Test] public void LongRecordingDoesNotRetainAllPreviouslyWrittenSamples()
        {
            var writer=new CaptureAudioWriter(new CountingStream(),48000,4,4096);var samples=new float[4096];
            try
            {
                Assert.IsTrue(writer.Append(samples,2));Wait(()=>writer.BufferedBlocks==0,"warmup");
                long before=GC.GetTotalMemory(true);
                for(int i=0;i<1000;i++)
                {Assert.IsTrue(writer.Append(samples,2));Wait(()=>writer.BufferedBlocks==0,"drain");}
                long growth=GC.GetTotalMemory(true)-before;
                TestContext.WriteLine("CAPTURE_AUDIO_RETAINED_GROWTH "+growth+"; 16,384,000 submitted bytes");
                Assert.Less(growth,2*1024*1024,"capture retains previously written audio");
                Assert.AreEqual(5*4096*4,writer.Snapshot().sampleBufferBytes);Assert.IsNull(writer.Failure);
                Assert.AreEqual(1001L*4096,writer.WrittenSamples);
            }
            finally{writer.Complete();Wait(()=>writer.Completed,"cleanup");}
        }
        [Test] public void DiskFailureIsRetainedAndWorkerStillCloses()
        {
            var stream=new BlockedStream{throwOnData=true};stream.release.Set();
            var writer=new CaptureAudioWriter(stream,48000,2,8);
            writer.Append(new float[8],2);writer.Complete();Wait(()=>writer.Completed,"disk failure hung worker");
            Assert.That(writer.Failure,Does.Contain("IOException"));Assert.AreEqual(0,writer.WrittenSamples);
            Assert.AreEqual(8,writer.Snapshot().acceptedSamples);
            Assert.IsFalse(writer.Append(new float[8],2),"failed writer accepted more samples");
        }
        [TestCase("channel")][TestCase("oversize")][TestCase("nan")][TestCase("alignment")][TestCase("limit")]
        public void MalformedOrUnrepresentableAudioCannotProduceASuccess(string kind)
        {
            var stream=new MemoryStream();var writer=new CaptureAudioWriter(stream,48000,4,8,32);
            try
            {
                Assert.IsTrue(writer.Append(new float[4],2));Wait(()=>writer.BufferedBlocks==0,"first block");
                var samples=kind=="oversize"?new float[16]:kind=="alignment"?new float[3]:kind=="limit"?new float[8]:new float[4];
                if(kind=="nan")samples[0]=float.NaN;
                Assert.IsFalse(writer.Append(samples,kind=="channel"?1:2));
                Finish(writer,stream);Assert.IsNotEmpty(writer.Failure);Assert.AreEqual(4,writer.WrittenSamples);
            }
            finally{writer.Complete();Wait(()=>writer.Completed,"cleanup");}
        }
        [Test] public void EmptyCaptureIsRejected()
        {
            var stream=new MemoryStream();var writer=new CaptureAudioWriter(stream,48000,2,8);
            Finish(writer,stream);Assert.That(writer.Failure,Does.Contain("empty"));Assert.AreEqual(0,writer.WrittenSamples);
        }
    }
}
