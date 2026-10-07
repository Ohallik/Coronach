using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditorInternal;
using UnityEngine;
using Lattice.Core;
using Lattice.UI;
using Newtonsoft.Json.Linq;

namespace Lattice.EditorTools
{
    public static class QualityProfileReport
    {
        public static void Segments() => BatchTools.Run(() => {
            string root = Path.GetFullPath(DevArgs.Value("-quality-profile-folder"));
            var index = JObject.Parse(File.ReadAllText(Path.Combine(root, "trace-index.json")));
            int limit = (int)index["frameLimit"];
            if (limit < 120 || limit > 1800) throw new InvalidOperationException("Invalid trace frame bound");
            var history = typeof(ProfilerDriver).GetMethod("SetMaxFrameHistoryLength", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (history == null) throw new InvalidOperationException("Bounded profiler import unavailable");
            history.Invoke(null, new object[] { limit + 100 });
            // CSV intervals end in LateUpdate; inspect both bordering native frames.
            var wanted = new HashSet<int>();
            var rows = File.ReadAllLines(Path.Combine(root, "frames.csv"));
            for (int i = 1; i < rows.Length; i++)
                if (double.Parse(rows[i].Split(',')[1], System.Globalization.CultureInfo.InvariantCulture) > 25)
                    for (int n = i - 2; n <= i; n++) wanted.Add(n);
            foreach (var segment in index["segments"])
            {
                string name = (string)segment["file"];
                if (Path.GetFileName(name) != name) throw new InvalidOperationException("Trace name must be local");
                string output = Path.Combine(root, name + ".summary.json");
                if (File.Exists(output)) throw new InvalidOperationException("Preserve existing trace report: " + output);
                ProfilerDriver.ClearAllFrames();
                if (!ProfilerDriver.LoadProfile(Path.Combine(root, name), false)) throw new InvalidOperationException("Trace import failed: " + name);
                var frames = new List<object>();
                var seen = new HashSet<int>();
                for (int f = ProfilerDriver.firstFrameIndex; f <= ProfilerDriver.lastFrameIndex; f++)
                {
                    using var view = ProfilerDriver.GetRawFrameDataView(f, 0);
                    if (!view.valid) throw new InvalidOperationException("Missing native frame " + f);
                    var meta = view.GetFrameMetaData<double>(QualityTrace.MetadataId, 0);
                    int sample = meta.Length == 4 ? (int)meta[0] : -1;
                    if (sample >= 0 && !seen.Add(sample)) throw new InvalidOperationException("Duplicate replay sample " + sample);
                    var details = new List<object>();
                    double markerMs = -1;
                    bool detail = wanted.Contains(sample) || view.frameTimeMs > 25;
                    for (int t = 0; t < 128; t++)
                    {
                        using var thread = ProfilerDriver.GetRawFrameDataView(f, t);
                        if (!thread.valid) break;
                        for (int s = 0; s < thread.sampleCount; s++)
                        {
                            string markerName = thread.GetSampleName(s);
                            double ms = thread.GetSampleTimeMs(s);
                            if (t == 0 && markerName == QualityTrace.SampleMarker) markerMs = thread.GetSampleStartTimeMs(s);
                            if (detail && ms >= .1) details.Add(new { thread = thread.threadName, name = markerName, ms, startMs = thread.GetSampleStartTimeMs(s) });
                        }
                        if (!detail) break;
                    }
                    frames.Add(new { frame = f, sample, unityFrame = meta.Length == 4 ? (int)meta[1] : -1,
                        step = meta.Length == 4 ? (int)meta[2] : -1, elapsed = meta.Length == 4 ? meta[3] : -1,
                        startMs = view.frameStartTimeMs, ms = view.frameTimeMs, markerMs, details });
                }
                int first = (int)segment["firstSample"], last = (int)segment["lastSample"];
                var missing = Enumerable.Range(first, last - first + 1).Where(n => !seen.Contains(n)).ToArray();
                File.WriteAllText(output, JsonConvert.SerializeObject(new { source = name, first, last, missing,
                    valid = missing.Length == 0, note = "Diagnostic only. Inclusive marker costs overlap. Boundary/flush frames retained. Replay intervals span adjacent native frames.", frames }, Formatting.None));
                if (missing.Length != 0) throw new InvalidOperationException("Incomplete trace coverage: " + name + " missing=" + string.Join(",", missing));
                Debug.Log("QUALITY_TRACE_SEGMENT_OK " + name + " samples=" + seen.Count);
            }
            ProfilerDriver.ClearAllFrames();
            Debug.Log("QUALITY_TRACE_REPORT_OK");
        });

        public static void Latest() => BatchTools.Run(() => {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C1"));
            string path=Directory.GetFiles(root,"cpu-ui-render.raw",SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).First();
            ProfilerDriver.LoadProfile(path,false);
            var frames=new List<object>();var expensive=new Dictionary<string,(int count,double total,double max)>();
            for(int frame=ProfilerDriver.firstFrameIndex;frame<=ProfilerDriver.lastFrameIndex;frame++)
            {
                using var view=ProfilerDriver.GetRawFrameDataView(frame,0);
                if(!view.valid)continue;
                var samples=new List<(string name,double ms)>();
                for(int i=0;i<view.sampleCount;i++)
                {
                    string name=view.GetSampleName(i);double ms=view.GetSampleTimeMs(i);
                    if(ms>.5)samples.Add((name,ms));
                    if(name.Contains("Coronach")||name.Contains("Canvas")||name.Contains("GC.Collect")||name.Contains("Resources.Load")||name.Contains("ReadObject"))
                    {
                        expensive.TryGetValue(name,out var prior);
                        expensive[name]=(prior.count+1,prior.total+ms,Math.Max(prior.max,ms));
                    }
                }
                // Inclusive samples deliberately retain parent/child overlap. This
                // identifies the work inside a hitch, not a sum of active CPU cost.
                if(view.frameTimeMs>25)frames.Add(new{frame,thread=view.threadName,ms=view.frameTimeMs,markers=samples.OrderByDescending(s=>s.ms).Take(30).Select(s=>new{s.name,s.ms}).ToArray()});
            }
            var report=new {source=path,firstFrame=ProfilerDriver.firstFrameIndex,lastFrame=ProfilerDriver.lastFrameIndex,
                note="Inclusive main-thread sample times overlap. Presentation waits are preserved; do not sum these as active CPU/GPU work.",
                markers=expensive.Select(p=>new{name=p.Key,p.Value.count,p.Value.total,p.Value.max}).OrderByDescending(p=>p.max).ToArray(),hitches=frames};
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path),"profiler-summary.json"),JsonConvert.SerializeObject(report,Formatting.Indented));
            if(ProfilerDriver.firstFrameIndex<0)throw new InvalidOperationException("No profiler frames loaded");
            Debug.Log("QUALITY_PROFILE_REPORT_OK "+path);
        });
    }
}
