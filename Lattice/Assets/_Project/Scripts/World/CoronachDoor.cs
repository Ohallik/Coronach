using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// One place in chapter 9's Coronach (docs/maps/DecksLeaving.md, "The Coronach"): a neighbour at their door, a guest at the edge, one of the heroes' last words, Orrin's close, or someone who
    /// stands there only because the party once finished their story. Its <see cref="point"/> is the prompt (a <see cref="DiscoveryPoint"/>: its flag is the world's and hearing it saves); this
    /// decides who stands here and when, keeps their lantern, plays the extras in the same stop straight after the door's own line, and takes them aboard once the party has gone on.
    /// The rule a player reads: <b>a lit lantern at someone's feet means somebody here still has something to say.</b> Heard, they put it out and stay with their door; once the party is away and
    /// none of it is in the camera's view, they and the lantern are gone and what they faced is shut. Nobody here turns to the party: the body faces its door, and it is no <see cref="Npc"/>, so
    /// the conversation's staging has nobody to turn. A save loads it as the flags say, with nothing played. Logs <c>CORONACH_HEARD name</c>, <c>CORONACH_EXTRA node</c>, <c>CORONACH_ABOARD name</c>.
    /// </summary>
    public sealed class CoronachDoor:MonoBehaviour
    {
        /// <summary>One side story's line, said in the same stop after the door's own if that story was finished, and what stands here only then.</summary>
        [Serializable] public struct Extra
        {
            public string node;
            /// <summary>The flag the story left behind: <c>quest.Id.complete</c>.</summary>
            public string needs;
            /// <summary>Here only if the story was finished (a gardener, a crew, a kettle), and gone aboard with the speaker.</summary>
            public GameObject[] shows;
            /// <summary>Heard once, the first time the lead comes within <see cref="soundReach"/> of this place while it is unheard: the school raft's bell, one stroke.</summary>
            public string sound;public float volume,soundReach;
            /// <summary>Walked once along <see cref="path"/> from the moment this door's talk opens, each a little behind the one before, and gone at its end: the table top in two halves.</summary>
            public Transform[] carried;public Vector3[] path;public float pace;
            /// <summary>Glows kept low while shown, as this share of their own: the gardener's light.</summary>
            public Renderer[] dim;public float dimTo;
        }
        public DiscoveryPoint point;
        /// <summary>The speaker of every line here, and the door's own node (empty for someone whose only lines are extras: then the first finished one is the point's).</summary>
        public string speaker,node;
        public Extra[] extras=Array.Empty<Extra>();
        public string calledFlag="home.coronachCalled";
        /// <summary>Nothing offered and no lantern until this is set: the heroes' last words wait on the seven doors, Orrin's close on the three.</summary>
        public string opensWith;
        /// <summary>A hero's own last word: <see cref="body"/> is that hero standing at their place, there only while they rest (the two out walk the town as the party).</summary>
        public string hero;
        /// <summary>Who stands here, facing the door.</summary>
        public GameObject body;
        /// <summary>A body that walks here and is shown by its own holder (Orrin's, a <see cref="Guide"/>): nothing is offered until it stands within a step of <see cref="post"/>.</summary>
        public Transform arrives,post;
        /// <summary>The lantern at their feet, its light and the glass that glows with it.</summary>
        public GameObject lamp;public Light lampLight;public Renderer[] glass=Array.Empty<Renderer>();
        /// <summary>Stays standing once heard and never goes aboard: the heroes, and Orrin at the line.</summary>
        public bool stays;
        /// <summary>Shown once they have gone (a shutter down), and hidden then (the awning they take with them).</summary>
        public GameObject[] shuts=Array.Empty<GameObject>(),takes=Array.Empty<GameObject>();
        /// <summary>How far the lead must be from here, with every part of it out of the camera's view, before they go aboard.</summary>
        public float goneFrom=12;
        /// <summary>The walk is called in front of this place and the day's own person stands here (Hal, in his workshop, as Orrin calls it outside): the body takes over at once instead of
        /// waiting to be out of view, starting at <see cref="turnsFrom"/> (the day's person's heading, degrees; below nought: its own) and turning to its door.</summary>
        public bool handsOver;public float turnsFrom=-1;
        /// <summary>Seconds a lantern takes to light or go out.</summary>
        public const float LampSeconds=1.4f;

        public static readonly List<CoronachDoor> All=new();
        bool settled,heard,gone,carrying,turning;float lampLevel,lampFull,along;Quaternion faces;
        bool[] sounded=Array.Empty<bool>();
        /// <summary>The door's talk has opened this frame (its extras are asked for after it at the frame's end), and how many of them are still to finish.</summary>
        bool opening;int following;
        MaterialPropertyBlock block;Color[] glassGlow;readonly Dictionary<Renderer,Color> dimGlow=new();

        static bool Flag(string key)=>!string.IsNullOrEmpty(key)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        static bool Done(in Extra extra)=>Flag(extra.needs);
        public bool Called=>Flag(calledFlag);
        public bool Opened=>string.IsNullOrEmpty(opensWith)||Flag(opensWith);
        public bool Heard=>point!=null&&Flag(point.flag);
        /// <summary>Someone whose only lines are extras and none of whose stories was finished: nobody stands here, and nothing is set out for them.</summary>
        public bool Nobody{get;private set;}
        public bool Gone=>gone;
        /// <summary>Somebody is here, the walk is on and their place is open: the place is in the street.</summary>
        public bool Live=>settled&&Called&&!gone&&!Nobody;
        public bool Lit=>lamp!=null&&lamp.activeInHierarchy&&lampLevel>.5f;
        public bool Offered=>point!=null&&point.isActiveAndEnabled&&point.Wanted;
        /// <summary>The extras this place will say after its own line, as things stand.</summary>
        public IEnumerable<string> ExtrasDue(){if(!settled)yield break;foreach(var extra in extras)if(Done(extra)&&extra.node!=point.dialogueNode)yield return extra.node;}

        void OnEnable(){All.Add(this);DiscoveryPoint.DialogueRequested+=Asked;}
        void OnDisable(){All.Remove(this);DiscoveryPoint.DialogueRequested-=Asked;}
        void Asked(DiscoveryPoint asked){if(asked==point&&asked!=null){opening=true;Carry();}}

        /// <summary>Once, when the save's flags are in: who is here at all, which line is the point's, and everything already heard is long gone aboard.</summary>
        void Settle()
        {
            settled=true;block=new MaterialPropertyBlock();
            if(lampLight!=null)lampFull=lampLight.intensity;
            glassGlow=new Color[glass.Length];
            for(int i=0;i<glass.Length;i++)glassGlow[i]=glass[i]!=null&&glass[i].sharedMaterial!=null&&glass[i].sharedMaterial.HasProperty("_EmissionColor")?glass[i].sharedMaterial.GetColor("_EmissionColor"):Color.black;
            foreach(var extra in extras)if(extra.dim!=null)foreach(var r in extra.dim)if(r!=null&&r.sharedMaterial!=null&&r.sharedMaterial.HasProperty("_EmissionColor"))dimGlow[r]=r.sharedMaterial.GetColor("_EmissionColor");
            string first=node;
            if(string.IsNullOrEmpty(first))foreach(var extra in extras)if(Done(extra)){first=extra.node;break;}
            Nobody=string.IsNullOrEmpty(first);
            if(point!=null){point.dialogueNode=first??"";point.dialogueSpeaker=speaker;}
            heard=Heard;gone=heard&&!stays;sounded=new bool[extras.Length];
            for(int i=0;i<extras.Length;i++)sounded[i]=heard;
            foreach(var extra in extras)if(extra.carried!=null)foreach(var who in extra.carried)if(who!=null)who.gameObject.SetActive(false);
            lampLevel=Called&&Opened&&!heard?1:0;if(body!=null)faces=body.transform.rotation;
            Apply(true);
        }

        void Update()
        {
            if(GameServices.Current==null)return;
            if(!settled)Settle();
            bool was=heard;heard=Heard;
            if(heard&&!was)Debug.Log("CORONACH_HEARD "+name);
            if(heard&&!gone&&!stays&&!opening&&following==0&&Away()){gone=true;Debug.Log("CORONACH_ABOARD "+name);}
            Listen();Walk();
            if(turning&&body!=null){body.transform.rotation=Quaternion.RotateTowards(body.transform.rotation,faces,70*Time.deltaTime);turning=Quaternion.Angle(body.transform.rotation,faces)>.5f;}
            float want=Live&&Opened&&!heard?1:0;
            lampLevel=Mathf.MoveTowards(lampLevel,want,Time.deltaTime/LampSeconds);
            Apply(false);
        }
        /// <summary>The extras, straight after: asked for as the door's talk opens, each to follow the one before in the same box (DialogueSystem.Follow), so it never closes between them
        /// and a talk cut short takes them with it. Asked at the frame's end, once whatever started the door's talk has started it.</summary>
        void LateUpdate()
        {
            if(!opening)return;opening=false;
            // Only behind the door's own talk: refused (something else running), the point is offered again and nothing is asked to follow.
            if(heard||point==null||point.Available)return;
            foreach(var extra in extras)
            {
                if(!Done(extra)||extra.node==point.dialogueNode)continue;
                string node=extra.node;following++;
                if(!Npc.SayNext(node,speaker,completed=>{following--;if(completed)Debug.Log("CORONACH_EXTRA "+node);}))following--;
            }
        }
        /// <summary>A sound that belongs here (the school raft's bell) is heard once, as the party comes near a place still unheard.</summary>
        void Listen()
        {
            if(!Live||heard)return;
            var lead=Lead();if(lead==null)return;
            for(int i=0;i<extras.Length;i++)
            {
                var extra=extras[i];if(sounded[i]||string.IsNullOrEmpty(extra.sound)||!Done(extra))continue;
                float reach=extra.soundReach>0?extra.soundReach:7;if(Flat(lead.position-Anchor).magnitude>reach)continue;
                sounded[i]=true;AudioManager.PlayAt(extra.sound,transform,Anchor+Vector3.up*1.6f,extra.volume>0?extra.volume:.3f,AudioBus.SFX,40);Debug.Log("CORONACH_SOUND "+extra.sound);
            }
        }
        /// <summary>The door's talk has opened: whatever is carried past it comes out now.</summary>
        void Carry()
        {
            if(carrying||!settled||heard)return;
            foreach(var extra in extras)if(Done(extra)&&extra.carried!=null&&extra.path!=null&&extra.path.Length>1)
                foreach(var who in extra.carried)if(who!=null){who.position=extra.path[0];who.gameObject.SetActive(true);carrying=true;}
            along=0;if(carrying)Debug.Log("CORONACH_CARRIED "+name);
        }
        void Walk()
        {
            if(!carrying)return;
            along+=Time.deltaTime;bool any=false;
            foreach(var extra in extras)
            {
                if(!Done(extra)||extra.carried==null||extra.path==null||extra.path.Length<2)continue;
                float pace=extra.pace>0?extra.pace:.8f;
                for(int k=0;k<extra.carried.Length;k++)
                {
                    var who=extra.carried[k];if(who==null||!who.gameObject.activeSelf)continue;
                    // Each a body's length behind the one before, as two people carry the halves of one thing.
                    float at=along*pace-k*1.7f;if(at<0){who.position=extra.path[0];any=true;continue;}
                    if(!Along(extra.path,at,out var spot,out var heading)){who.gameObject.SetActive(false);continue;}
                    any=true;who.position=spot;if(heading.sqrMagnitude>.001f)who.rotation=Quaternion.RotateTowards(who.rotation,Quaternion.LookRotation(heading),360*Time.deltaTime);
                }
            }
            if(!any)carrying=false;
        }
        static bool Along(Vector3[] path,float distance,out Vector3 spot,out Vector3 heading)
        {
            for(int i=1;i<path.Length;i++)
            {
                var leg=path[i]-path[i-1];float length=Flat(leg).magnitude;
                if(distance<=length){spot=path[i-1]+leg*(length>0?distance/length:0);heading=Flat(leg);return true;}
                distance-=length;
            }
            spot=path[^1];heading=Vector3.zero;return false;
        }

        void Apply(bool loading)
        {
            bool live=Live,open=Opened;
            if(body!=null)
            {
                bool want=live&&(string.IsNullOrEmpty(hero)||Resting());
                // Once the walk is on, somebody arrives at their place only where nobody sees it happen (the day's own people go aboard the same way); a hero brought out of rest leaves it
                // at once, so there are never two of them. A load decides it all behind the black.
                if(want&&!body.activeSelf&&!loading&&!handsOver&&Seen(body.transform.position))want=false;
                if(want&&!body.activeSelf&&!loading&&handsOver&&turnsFrom>=0){body.transform.rotation=Quaternion.Euler(0,turnsFrom,0);turning=true;}
                if(body.activeSelf!=want)body.SetActive(want);
            }
            // What stands with someone (a gardener, a crew, a kettle) comes and goes with them.
            bool with=body==null||body.activeSelf||!string.IsNullOrEmpty(hero);
            foreach(var extra in extras)
            {
                bool show=live&&Done(extra)&&with;
                if(extra.shows!=null)foreach(var thing in extra.shows)if(thing!=null&&thing.activeSelf!=show)thing.SetActive(show);
                if(show&&extra.dim!=null)foreach(var r in extra.dim)if(r!=null&&dimGlow.TryGetValue(r,out var full)){r.GetPropertyBlock(block);block.SetColor("_EmissionColor",full*Mathf.Clamp01(extra.dimTo));r.SetPropertyBlock(block);}
            }
            if(lamp!=null)
            {
                // Someone who walks on once heard (Orrin from his board) takes their lantern with them.
                bool show=live&&open&&(!heard||arrives==null||Arrived());if(lamp.activeSelf!=show)lamp.SetActive(show);
                if(lampLight!=null){lampLight.intensity=lampFull*lampLevel;lampLight.enabled=lampLevel>.01f;}
                for(int i=0;i<glass.Length;i++)if(glass[i]!=null){glass[i].GetPropertyBlock(block);block.SetColor("_EmissionColor",glassGlow[i]*lampLevel);glass[i].SetPropertyBlock(block);}
            }
            if(point!=null)
            {
                // Nobody is spoken to before they are standing there: a hero's place is spoken from wherever that hero is.
                bool offer=live&&open&&!heard&&Arrived()&&(body==null||body.activeSelf||!string.IsNullOrEmpty(hero));
                if(point.enabled!=offer)point.enabled=offer;
            }
            foreach(var shut in shuts)if(shut!=null&&shut.activeSelf!=gone)shut.SetActive(gone);
            foreach(var taken in takes)if(taken!=null&&taken.activeSelf==gone)taken.SetActive(!gone);
        }
        bool Resting()
        {
            var state=GameServices.Current.State;
            return !Deployment.Out(state,hero);
        }
        bool Arrived()=>arrives==null||post==null||Flat(arrives.position-post.position).magnitude<1.2f&&arrives.gameObject.activeInHierarchy;
        Vector3 Anchor=>point!=null?point.transform.position:transform.position;
        static Transform Lead(){var party=PartyController.Current;return party!=null&&party.members!=null&&party.members.Length>0&&party.Active!=null?party.Active.transform:null;}
        /// <summary>The party has gone on: the lead is well away and nothing of this place is in the camera's view.</summary>
        bool Away()
        {
            var lead=Lead();if(lead==null)return false;
            if(Flat(lead.position-Anchor).magnitude<goneFrom)return false;
            if(Seen(Anchor))return false;
            if(body!=null&&body.activeInHierarchy&&Seen(body.transform.position))return false;
            if(lamp!=null&&lamp.activeInHierarchy&&Seen(lamp.transform.position))return false;
            foreach(var shut in shuts)if(shut!=null&&Seen(shut.transform.position))return false;
            foreach(var extra in extras)if(extra.shows!=null)foreach(var thing in extra.shows)if(thing!=null&&thing.activeInHierarchy&&Seen(thing.transform.position))return false;
            return true;
        }
        /// <summary>In the camera's view, with a margin, at a person's middle.</summary>
        public static bool Seen(Vector3 at)
        {
            var eye=Camera.main;if(eye==null)return false;
            var v=eye.WorldToViewportPoint(at+Vector3.up*.9f);
            return v.z>0&&v.x>-.12f&&v.x<1.12f&&v.y>-.15f&&v.y<1.15f;
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
    }
}
