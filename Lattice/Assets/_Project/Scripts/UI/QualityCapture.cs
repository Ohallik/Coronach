using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.UI
{
    /// <summary>Opt-in game framebuffer and listener mix recording. Never enabled
    /// in a clean timing run. A bounded encoder queue reports overflow as failure.</summary>
    public sealed class QualityCapture : MonoBehaviour
    {
        readonly ConcurrentQueue<(int index, byte[] bytes)> video = new();
        readonly ConcurrentQueue<byte[]> buffers = new();
        readonly List<float[]> audio = new();
        readonly List<string> times = new();
        readonly object audioLock = new();
        Process encoder;
        Thread worker;
        RenderTexture target;
        volatile bool running;
        volatile string failure;
        int pending, audioChannels, sampleRate, lastRequest = -1;
        double start, firstAudioDsp = -1, beginDsp;
        string folder;
        public string Failure => failure;
        public void Begin(string output, string ffmpeg)
        {
            folder = output; sampleRate = AudioSettings.outputSampleRate;
            target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            target.Create();
            // Reuse bounded exact-size readback storage. Allocating an 8 MB
            // array thirty times each second distorted the motion being recorded.
            for(int i=0;i<12;i++)buffers.Enqueue(new byte[Screen.width*Screen.height*4]);
            encoder = Process.Start(new ProcessStartInfo {
                FileName = ffmpeg,
                Arguments = $"-hide_banner -loglevel error -y -f rawvideo -pixel_format rgba -video_size {Screen.width}x{Screen.height} -framerate 30 -i pipe:0 -c:v libx264 -preset ultrafast -crf 18 -pix_fmt yuv420p \"{Path.Combine(folder, "video.mp4")}\"",
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardError = true
            });
            encoder.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) failure = e.Data; };
            encoder.BeginErrorReadLine();
            running = true; start = Time.realtimeSinceStartupAsDouble; beginDsp = AudioSettings.dspTime;
            worker = new Thread(Encode) { IsBackground = true, Name = "Quality video encoder" }; worker.Start();
            StartCoroutine(Frames());
        }
        IEnumerator Frames()
        {
            var end = new WaitForEndOfFrame();
            while (running)
            {
                yield return end;
                int index = (int)((Time.realtimeSinceStartupAsDouble - start) * 30);
                if (index <= lastRequest) continue;
                if (pending > 3 || video.Count > 6) { failure = "Capture queue overflow; motion recording incomplete"; continue; }
                if(!buffers.TryDequeue(out var bytes)){failure="Capture buffer pool exhausted; motion recording incomplete";continue;}
                lastRequest = index; pending++;
                times.Add(index + "," + (Time.realtimeSinceStartupAsDouble - start).ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                ScreenCapture.CaptureScreenshotIntoRenderTexture(target);
                AsyncGPUReadback.Request(target, 0, TextureFormat.RGBA32, request => {
                    if (request.hasError){failure = "GPU readback failed";buffers.Enqueue(bytes);}
                    else {request.GetData<byte>().CopyTo(bytes);video.Enqueue((index,bytes));}
                    pending--;
                });
            }
        }
        void Encode()
        {
            try
            {
                byte[] previous = null; int written = 0;
                var stream = encoder.StandardInput.BaseStream;
                while (running || pending > 0 || !video.IsEmpty)
                {
                    if (!video.TryDequeue(out var frame)) { Thread.Sleep(2); continue; }
                    while (written < frame.index) { var bytes = previous ?? frame.bytes; stream.Write(bytes, 0, bytes.Length); written++; }
                    stream.Write(frame.bytes, 0, frame.bytes.Length); written++;
                    if(previous!=null)buffers.Enqueue(previous);
                    previous = frame.bytes;
                }
                stream.Close();
                if(previous!=null)buffers.Enqueue(previous);
            }
            catch (Exception e) { failure = "Encoder: " + e.Message; }
        }
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!running) return;
            lock (audioLock)
            {
                if (firstAudioDsp < 0) firstAudioDsp = AudioSettings.dspTime;
                audioChannels = channels;
                audio.Add((float[])data.Clone());
            }
        }
        public IEnumerator Finish()
        {
            running = false;
            float deadline = Time.realtimeSinceStartup + 30;
            while ((pending > 0 || worker.IsAlive || !encoder.HasExited) && Time.realtimeSinceStartup < deadline) yield return null;
            if (!encoder.HasExited) { failure = "Encoder finalization timeout"; encoder.Kill(); }
            else if (encoder.ExitCode != 0) failure = "Encoder exit " + encoder.ExitCode;
            lock (audioLock)
            {
                int count = 0; foreach (var block in audio) count += block.Length;
                if (count == 0) failure = "Listener mix capture empty";
                using var writer = new BinaryWriter(File.Create(Path.Combine(folder, "mix.wav")));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 4);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)3);
                writer.Write((short)audioChannels); writer.Write(sampleRate); writer.Write(sampleRate * audioChannels * 4);
                writer.Write((short)(audioChannels * 4)); writer.Write((short)32);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 4);
                foreach (var block in audio) foreach (float sample in block) writer.Write(sample);
            }
            File.WriteAllLines(Path.Combine(folder, "capture-frames.csv"), times);
            File.WriteAllText(Path.Combine(folder, "capture.txt"), "30 fps native framebuffer; repeated frames retain wall-clock stalls.\nAudio is the actual AudioListener float mix.\nFirst audio offset seconds: " + (firstAudioDsp - beginDsp).ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "\nFailure: " + failure);
            target.Release(); Destroy(target); encoder.Dispose();
        }
        void OnDestroy() { running = false; }
    }
}
