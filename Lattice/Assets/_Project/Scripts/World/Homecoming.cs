using System;
using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// One person in chapter 10's neighbourhood (docs/maps/NeighbourhoodAfloat.md, "People"): who stands here, which prompt is theirs, and the extras said straight after their own line.
    /// Their own line is their <see cref="npc"/>'s, every time (a SideStories beat in the zone). While their counted <see cref="point"/> is wanted (the Homecomings visit, offered at its step
    /// until it is heard) it is the person's only prompt: two prompts on one body left which one A pressed to the nearer, and a visit heard through the Npc was never counted. Each finished
    /// story's extra is asked to follow whichever of the two opened (<c>Npc.SayNext</c>, as chapter 9's Coronach does), in the same box, and is gone with a talk cut short. Someone whose only
    /// lines are extras stands here only if one of their stories was finished (<see cref="Nobody"/>); what an extra stands with (<see cref="Extra.shows"/>) is here only with it. Decided as
    /// the scene loads, so nobody blinks in or out in front of the party. From <see cref="quietFrom"/> the person has nothing more to say and offers nothing (Orrin, chalking at his board).
    /// Logs <c>HOMECOMING_EXTRA node</c>.
    /// </summary>
    public sealed class Homecoming:MonoBehaviour
    {
        [Serializable] public struct Extra
        {
            public string node;
            /// <summary>The flag the story left behind: <c>quest.Id.complete</c>.</summary>
            public string needs;
            /// <summary>Here only if the story was finished: the lamps on the rail, the baskets in the window.</summary>
            public GameObject[] shows;
        }
        public string speaker;
        public Npc npc;
        /// <summary>A point whose talk this person's extras follow too: the counted visit, or a place a hero speaks at (the moorings, for Idra).</summary>
        public DiscoveryPoint point;
        public Extra[] extras=Array.Empty<Extra>();
        /// <summary>Someone whose only lines are extras: nobody stands here unless one of their stories was finished, and the first finished is their own line.</summary>
        public bool onlyExtras;
        /// <summary>Nothing offered from this flag on: they have said what they came to say.</summary>
        public string quietFrom;
        public static readonly List<Homecoming> All=new();
        bool settled,opening;string lead;
        public bool Nobody{get;private set;}
        public bool Settled=>settled;
        static bool Flag(string key)=>!string.IsNullOrEmpty(key)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        static bool Done(in Extra extra)=>Flag(extra.needs);
        /// <summary>The extras that would follow this opening line now, in order.</summary>
        public IEnumerable<string> ExtrasAfter(string opened){foreach(var extra in extras)if(Done(extra)&&extra.node!=opened)yield return extra.node;}
        /// <summary>The line this person opens with when spoken to now (their Npc's, or the first finished extra's).</summary>
        public string Opening
        {
            get
            {
                if(!onlyExtras)return npc!=null?npc.repeatNode:point!=null?point.dialogueNode:null;
                foreach(var extra in extras)if(Done(extra))return extra.node;
                return null;
            }
        }

        void OnEnable(){All.Add(this);DiscoveryPoint.DialogueRequested+=Asked;Npc.TalkRequested+=Talked;}
        void OnDisable(){All.Remove(this);DiscoveryPoint.DialogueRequested-=Asked;Npc.TalkRequested-=Talked;opening=false;}
        void Asked(DiscoveryPoint asked){if(settled&&asked!=null&&asked==point){opening=true;lead=asked.dialogueNode;}}
        /// <summary>Their Npc has been spoken to: its first word is the opening line (a beat, or their tier).</summary>
        void Talked(string node,string who,Action<bool> done)
        {
            if(!settled||npc==null||who!=speaker||!npc.isActiveAndEnabled||string.IsNullOrEmpty(node))return;
            bool theirs=node==npc.repeatNode||node==npc.firstNode;foreach(var extra in extras)theirs|=extra.node==node;
            if(theirs){opening=true;lead=node;}
        }

        /// <summary>Once, when the save's flags are in.</summary>
        void Settle()
        {
            settled=true;
            Nobody=onlyExtras&&Opening==null;
            if(onlyExtras&&npc!=null&&!Nobody){npc.firstNode=npc.repeatNode=Opening;}
            foreach(var extra in extras)
            {
                bool show=!Nobody&&Done(extra);
                if(extra.shows!=null)foreach(var thing in extra.shows)if(thing!=null&&thing.activeSelf!=show)thing.SetActive(show);
            }
            if(Nobody){if(npc!=null)npc.gameObject.SetActive(false);if(point!=null)point.gameObject.SetActive(false);}
            Apply();
        }
        void Update()
        {
            if(GameServices.Current==null)return;
            if(!settled)Settle();
            Apply();
        }
        /// <summary>The Npc answers only while the counted point doesn't, and not at all once they're done.</summary>
        void Apply()
        {
            if(Nobody||npc==null)return;
            bool quiet=Flag(quietFrom);
            bool offered=!quiet&&!(point!=null&&point.isActiveAndEnabled&&point.Wanted);
            // A talk already under way keeps its Npc: switched off mid-talk, it would drop the talk's end.
            if(npc.enabled!=offered&&(offered||npc.Available))npc.enabled=offered;
        }
        /// <summary>Asked at the frame's end, once whatever started the talk has started it: refused, the prompt is free again and nothing is asked to follow.</summary>
        void LateUpdate()
        {
            if(!opening)return;opening=false;
            bool started=point!=null&&lead==point.dialogueNode&&!point.Available&&point.isActiveAndEnabled||npc!=null&&npc.isActiveAndEnabled&&!npc.Available;
            if(!started)return;
            foreach(var node in ExtrasAfter(lead))
            {
                string said=node;
                Npc.SayNext(said,speaker,completed=>{if(completed)Debug.Log("HOMECOMING_EXTRA "+said);});
            }
        }
    }
}
