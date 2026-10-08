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
        static float next;
        public static void Play(string character,string situation,bool priority=false)
        {
            if(Time.unscaledTime<next&&!priority)return;
            // Repeated successful evasions must not restart the same banter
            // every few seconds. Urgent cover/story calls keep their priority.
            if(situation=="flash"&&nextFlash.TryGetValue(character,out var after)&&Time.unscaledTime<after)return;
            var line=Line(character,situation);if(line==null)return;
            if(situation=="flash")nextFlash[character]=Time.unscaledTime+12;
            next=Time.unscaledTime+4;Spoken?.Invoke(character,line);
        }
        /// <summary>The authored line for a situation, or null.</summary>
        public static string Line(string character,string situation)
        {
            if(table==null){var file=Resources.Load<TextAsset>("Audio/barks");if(file==null)return null;table=JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string,string>>>(file.text);}
            return table.TryGetValue(character,out var rows)&&rows.TryGetValue(situation,out var line)?line:null;
        }
    }
}
