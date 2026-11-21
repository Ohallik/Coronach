using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Bryn Asta's tug, the <i>Welcome Back</i>, in the Returning Passage (chapter 10, docs/maps/ReturningPassage.md): on the tug's drawn root, at the plane. While she waits she is
    /// solid (<see cref="solid"/>: cover, a harpoon line breaks on her) and her fight's count is on the panel ("Clear the tow-drones off her (n/3)", <see cref="ZoneLine"/>), each
    /// drone cleared the lead's <see cref="clearedBark"/> in turn. From <see cref="lineFlag"/> she is on the lead's line: no longer solid, she follows him <see cref="length"/> metres
    /// back and a little to port of his wake, turned to him, with the line drawn from his stern to her bow. She slows nobody. While a conversation holds the controls she holds where
    /// she is (D275); a zone loaded with the line on starts her on it, behind the lead.
    /// <para><see cref="heldByTalk"/> is only ever turned off by the test that shows the old way failing first.</para>
    /// </summary>
    public sealed class TugTow:MonoBehaviour
    {
        public string lineFlag="end.lineOn";
        public EncounterVolume fight;
        public Collider solid;
        public LineRenderer line;
        /// <summary>Where the line is made fast on her, in her own frame.</summary>
        public Vector3 bow=new(0,.6f,5.4f);
        public float length=11,offset=2.5f,lag=.9f,height=1;
        public string clearedBark="tugdrones";
        public string countLine="Clear the tow-drones off her";
        public bool heldByTalk=true;
        public bool OnTheLine{get;private set;}
        int drones=-1,cleared;bool attachedAtLoad;
        public bool Held=>heldByTalk&&GameServices.Current!=null&&GameServices.Current.Input.Blocked;
        static bool Set(string flag)=>!string.IsNullOrEmpty(flag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(flag);
        void Start()
        {
            if(line!=null)line.enabled=false;
            if(Set(lineFlag))attachedAtLoad=true;
        }
        void OnDisable()=>ZoneLine.Clear(this);
        void Update()
        {
            var party=PartyController.Current;var lead=party!=null?party.Active:null;
            if(!OnTheLine&&Set(lineFlag))Attach(lead,attachedAtLoad);
            if(!OnTheLine){Count(lead);return;}
            if(lead==null)return;
            if(!GameTime.Paused&&!Held)Follow(lead,Time.deltaTime);
            DrawLine(lead);
        }
        /// <summary>The fight's count on the panel while it is on, and the lead's word as each drone comes off her.</summary>
        void Count(CombatActor lead)
        {
            if(fight==null){ZoneLine.Clear(this);return;}
            if(drones<0&&fight.spawners!=null){drones=0;foreach(var s in fight.spawners)if(s!=null)drones+=Mathf.Max(1,s.count);}
            if(fight.Started&&!fight.Cleared&&drones>0)
            {
                int off=Mathf.Clamp(drones-fight.Remaining,0,drones);
                while(cleared<off){if(lead!=null)BarkService.PlayAt(lead.character,clearedBark,cleared,true);cleared++;Debug.Log("TUG_DRONE_OFF "+cleared+"/"+drones);}
                ZoneLine.Say(this,countLine+" ("+off+"/"+drones+")",transform.position);
            }
            else
            {
                if(fight.Started&&fight.Cleared&&cleared<drones&&lead!=null){BarkService.PlayAt(lead.character,clearedBark,drones-1,true);cleared=drones;Debug.Log("TUG_DRONE_OFF "+drones+"/"+drones);}
                ZoneLine.Clear(this);
            }
        }
        void Attach(CombatActor lead,bool atLoad)
        {
            OnTheLine=true;ZoneLine.Clear(this);
            if(solid!=null)solid.enabled=false;
            foreach(var c in GetComponentsInChildren<Collider>())if(!c.isTrigger)c.enabled=false;
            // Loaded with her line on, she is behind the lead already: she was on the line when the save was made.
            if(atLoad&&lead!=null){var at=Station(lead);transform.SetPositionAndRotation(at,Quaternion.LookRotation(Flat(lead.transform.position-at)));}
            if(line!=null)line.enabled=true;
            Debug.Log("TUG_ON_THE_LINE "+(atLoad?"(loaded)":""));
        }
        /// <summary>Where she rides on the line: astern of the lead by its length, a little to port of his wake, at the plane.</summary>
        public Vector3 Station(CombatActor lead)
        {
            var forward=Heading(lead);var across=Vector3.Cross(Vector3.up,forward);
            var at=lead.transform.position-forward*length-across*offset;at.y=height;return at;
        }
        static Vector3 Heading(CombatActor lead)
        {
            var motor=lead.GetComponent<FlightMotor>();var v=motor!=null?motor.Velocity:Vector3.zero;v.y=0;
            if(v.sqrMagnitude<1){v=lead.transform.forward;v.y=0;}
            return v.sqrMagnitude<.01f?Vector3.forward:v.normalized;
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v.sqrMagnitude<.0001f?Vector3.forward:v;}
        void Follow(CombatActor lead,float dt)
        {
            // Eased toward her place on the line; she never comes nearer the lead than three quarters of it, so a lead who stops or turns back isn't run into.
            var goal=Station(lead);var at=transform.position;
            var next=Vector3.Lerp(at,goal,1-Mathf.Exp(-dt/Mathf.Max(.05f,lag)));
            var gap=Flat(next-lead.transform.position);float most=length*.75f;
            if(gap.magnitude<most){next=lead.transform.position+gap.normalized*most;next.y=height;}
            var facing=Flat(lead.transform.position-next);
            transform.SetPositionAndRotation(next,Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(facing),90*dt));
        }
        void DrawLine(CombatActor lead)
        {
            if(line==null)return;
            var stern=lead.transform.position-Heading(lead)*1.6f+Vector3.up*.4f;var fast=transform.TransformPoint(bow);
            line.positionCount=2;line.SetPosition(0,stern);line.SetPosition(1,fast);
        }
    }
}
