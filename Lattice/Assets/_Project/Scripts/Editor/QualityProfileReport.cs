using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditorInternal;
using UnityEngine;

namespace Lattice.EditorTools
{
    public static class QualityProfileReport
    {
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
