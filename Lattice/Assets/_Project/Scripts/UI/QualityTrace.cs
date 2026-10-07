using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Lattice.UI
{
    // Diagnostic only. Rotation costs remain in the replay; no frame is discarded.
    public sealed class QualityTrace
    {
        public static readonly Guid MetadataId = new("d1e83a42-68bc-4562-ade7-4981a4a4d356");
        public const string SampleMarker = "Coronach.Replay.Sample";
        static readonly ProfilerMarker marker = new(SampleMarker);
        readonly double[] metadata = new double[4]; // sample, Unity frame, step, elapsed
        readonly List<Segment> segments = new();
        readonly string folder;
        readonly int frameLimit;
        Segment current;
        [Serializable] public sealed class Segment
        {
            public string file;
            public int firstSample, lastSample = -1;
        }
        public QualityTrace(string folder, int frameLimit)
        {
            if (!Debug.isDebugBuild) throw new InvalidOperationException("Segmented profiling requires a development player");
            if (frameLimit < 120 || frameLimit > 1800) throw new ArgumentOutOfRangeException(nameof(frameLimit));
            this.folder = folder; this.frameLimit = frameLimit;
        }
        public void Sample(int sample, int unityFrame, int step, double elapsed)
        {
            if (current == null || sample - current.firstSample >= frameLimit)
            {
                Stop();
                current = new Segment { file = $"trace-{segments.Count:D3}.raw", firstSample = sample };
                segments.Add(current);
                Profiler.logFile = Path.Combine(folder, current.file);
                Profiler.enableBinaryLog = true;
                Profiler.enabled = true;
            }
            metadata[0] = sample; metadata[1] = unityFrame; metadata[2] = step; metadata[3] = elapsed;
            Profiler.EmitFrameMetaData(MetadataId, 0, metadata);
            using (marker.Auto()) { }
            current.lastSample = sample;
        }
        public void Stop()
        {
            if (current == null) return;
            Profiler.enabled = false;
            Profiler.logFile = "";
            current = null;
        }
        public void Finish()
        {
            Stop();
            File.WriteAllText(Path.Combine(folder, "trace-index.json"), Newtonsoft.Json.JsonConvert.SerializeObject(
                new { frameLimit, metadataId = MetadataId, cleanTimingEligible = false, segments }, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
