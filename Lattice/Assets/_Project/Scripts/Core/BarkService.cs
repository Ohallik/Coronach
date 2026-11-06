using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
namespace Lattice.Core
{
    public static class BarkService
    {
        public static event Action<string,string> Spoken;
        static Dictionary<string,Dictionary<string,string>> table;
        static readonly Dictionary<string,float> nextFlash=new();
        static float next,nextArrival;
        /// <summary>The next line of each pool: a line separated by | in barks.json is one of several, spoken in turn.</summary>
        static readonly Dictionary<string,int> turn=new();
        static readonly Dictionary<string,string> lastSpoken=new();
        /// <summary>Arriving somewhere says something now and then, not on every load: a dock and launch takes seconds (QA-007).</summary>
        public const float ArrivalGap=60;
        public static void PlayArrival(string character)
        {
            if(Time.unscaledTime<nextArrival)return;
            nextArrival=Time.unscaledTime+ArrivalGap;
            bool flying=ZoneController.Current!=null&&ZoneController.Current.Flight&&Pool(character,"zone.flight")!=null;
            Play(character,flying?"zone.flight":"zone",true);
        }
        public static void Play(string character,string situation,bool priority=false)
        {
            if(Time.unscaledTime<next&&!priority)return;
            // Repeated successful evasions must not restart the same banter
            // every few seconds. Urgent cover/story calls keep their priority.
            if(situation=="flash"&&nextFlash.TryGetValue(character,out var after)&&Time.unscaledTime<after)return;
            var pool=Pool(character,situation);if(pool==null)return;
            string key=character+"/"+situation;turn.TryGetValue(key,out int index);turn[key]=index+1;
            var line=pool[index%pool.Length];lastSpoken[key]=line;
            if(situation=="flash")nextFlash[character]=Time.unscaledTime+12;
            next=Time.unscaledTime+4;Spoken?.Invoke(character,line);
        }
        /// <summary>The line last spoken for a situation, or the first authored one; null when none is authored.</summary>
        public static string Line(string character,string situation)
        {
            if(lastSpoken.TryGetValue(character+"/"+situation,out var spoken))return spoken;
            var pool=Pool(character,situation);return pool!=null?pool[0]:null;
        }
        static string[] Pool(string character,string situation)
        {
            if(table==null){var file=Resources.Load<TextAsset>("Audio/barks");if(file==null)return null;table=JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string,string>>>(file.text);}
            return table.TryGetValue(character,out var rows)&&rows.TryGetValue(situation,out var lines)?lines.Split('|'):null;
        }
    }
}
