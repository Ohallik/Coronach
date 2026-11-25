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
            if(ZoneController.Current!=null&&ZoneController.Current.definition!=null&&ZoneController.Current.definition.quietArrival)return;
            nextArrival=Time.unscaledTime+ArrivalGap;
            bool flying=ZoneController.Current!=null&&ZoneController.Current.Flight&&Pool(character,"zone.flight")!=null;
            Play(character,flying?"zone.flight":"zone",true);
        }
        /// <summary>A moment nothing is said over (chapter 9's Coronach, where a town says what its doors did for it): while any owner holds it, no bark plays at all, an arrival's,
        /// a swap's or a place's. An owner that is destroyed lets go by itself.</summary>
        public static void Hush(UnityEngine.Object owner,bool on){if(owner==null)return;if(on)hushes.Add(owner);else hushes.Remove(owner);}
        public static bool Hushed{get{hushes.RemoveWhere(owner=>owner==null);return hushes.Count>0;}}
        static readonly HashSet<UnityEngine.Object> hushes=new();
        public static void Play(string character,string situation,bool priority=false)
        {
            if(Hushed)return;
            character=Present(character,situation);if(character==null)return;
            if(Time.unscaledTime<next&&!priority)return;
            // Repeated successful evasions must not restart the same banter
            // every few seconds. Urgent cover/story calls keep their priority.
            if(situation=="flash"&&nextFlash.TryGetValue(character,out var after)&&Time.unscaledTime<after)return;
            var pool=Pool(character,situation);if(pool==null)return;
            string key=character+"/"+situation;turn.TryGetValue(key,out int index);turn[key]=index+1;
            var line=pool[index%pool.Length];lastSpoken[key]=line;
            if(situation=="flash")nextFlash[character]=Time.unscaledTime+12;
            next=Time.unscaledTime+4;Situation=situation;Spoken?.Invoke(character,line);
        }
        /// <summary>The situation of the line being spoken, for a listener that needs to know what kind of line it is: a kill's word is a fight's last, not one of its lines (BarkPanel).</summary>
        public static string Situation{get;private set;}
        /// <summary>A pool's line by its place in the pool, whatever was said before it this session: a count said in a fixed order (the Picket's route lock, four pieces) stays in step
        /// after a reload in the middle of it.</summary>
        public static void PlayAt(string character,string situation,int index,bool priority=false)
        {
            var who=Present(character,situation);if(who!=null)turn[who+"/"+situation]=Mathf.Max(0,index);
            Play(character,situation,priority);
        }
        /// <summary>The line last spoken for a situation, or the first authored one; null when none is authored.</summary>
        public static string Line(string character,string situation)
        {
            if(lastSpoken.TryGetValue(character+"/"+situation,out var spoken))return spoken;
            var pool=Pool(character,situation);return pool!=null?pool[0]:null;
        }
        /// <summary>A situation spoken over the radio (RadioHail's, Maru's "hail.bright"): a voice from somewhere else, which a hero resting can be too.</summary>
        public const string RadioPrefix="hail.";
        /// <summary>Who says a line. A hero resting with three in the party (Deployment) has no body anywhere, so a line of theirs from the field, a gate a world object
        /// gives to Sela or a quip a place hands its hero, would come from nobody: the lead says their own line for it if they have one, and otherwise nothing is said.
        /// Anyone out, anyone not in the party, a voice on the radio, and every line of a party of two are spoken as asked.</summary>
        static string Present(string character,string situation)
        {
            var state=GameServices.Current!=null?GameServices.Current.State:null;
            if(!Deployment.Choosing(state)||situation!=null&&situation.StartsWith(RadioPrefix,StringComparison.Ordinal)||Deployment.PartyIndex(state,character)<0||Deployment.Out(state,character))return character;
            var lead=state.activeMember>=0&&state.activeMember<state.party.Count?state.party[state.activeMember]?.id:null;
            return lead!=null&&lead!=character&&Deployment.Out(state,lead)&&Pool(lead,situation)!=null?lead:null;
        }
        static string[] Pool(string character,string situation)
        {
            if(table==null){var file=Resources.Load<TextAsset>("Audio/barks");if(file==null)return null;table=JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string,string>>>(file.text);}
            return table.TryGetValue(character,out var rows)&&rows.TryGetValue(situation,out var lines)?lines.Split('|'):null;
        }
    }
}
