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
        static float next;
        public static void Play(string character,string situation,bool priority=false)
        {
            if(Time.unscaledTime<next&&!priority)return;
            if(table==null){var file=Resources.Load<TextAsset>("Audio/barks");if(file==null)return;table=JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string,string>>>(file.text);}
            if(!table.TryGetValue(character,out var rows)||!rows.TryGetValue(situation,out var line))return;next=Time.unscaledTime+4;Spoken?.Invoke(character,line);
        }
    }
}
