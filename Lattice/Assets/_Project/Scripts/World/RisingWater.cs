using System;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Water that climbs a hall behind the party: the Walking Canopy's flooding gallery, and any later place that fills (a drained ruin letting the swamp back in, a forge's quench). It drives one
    /// <see cref="WaterVolume"/> up from <see cref="restLevel"/> at <see cref="rate"/>, so the hall's own slope turns a rising level into a waterline that walks up the floor, and the water's own rule
    /// slows whoever it reaches. It starts when the leading hero crosses this object's trigger (once <see cref="startFlag"/> is set, if there is one) and is over for good, drained, once
    /// <see cref="stopFlag"/> is: that flag is in the save, the flood itself never is, so a hall left half-flooded starts again from rest.
    /// It never kills and never costs the zone. A hero the water closes over by <see cref="caughtDepth"/> loses a modest share of integrity (never the last of it) and the party is stood back on the
    /// last of the <see cref="ledges"/> it reached, with the water fallen back below that ledge and a breath before it climbs again: the stretch is tried again, not the hall.
    /// The floor it is about to take is marked as every danger is (D217): one lane in the house's threat colour, from where the water is already deep enough to catch someone to
    /// <see cref="lead"/> metres past the waterline, moving up the hall with it. Ledges are the party's ground and wear none of those colours.
    /// </summary>
    public sealed class RisingWater:MonoBehaviour
    {
        /// <summary>The water has just closed over the party and set it back on this ledge.</summary>
        public static event Action<RisingWater,Transform> Caught;
        public WaterVolume water;
        public string startFlag,stopFlag;
        /// <summary>Where the surface rests before and after, where it stops climbing, and how fast it climbs (metres of height a second).</summary>
        public float restLevel,topLevel,rate=.1f;
        public float caughtDepth=.75f,damageShare=.12f;
        /// <summary>After a catch: how far below the ledge the water falls back, and how long it waits there.</summary>
        public float fallBack=1.2f,grace=2.5f,drainSeconds=3;
        /// <summary>The hall's line along its floor, from its low end to its high end, and how much of its width is marked.</summary>
        public Transform low,high;public float width=10,lead=3.5f;
        /// <summary>The floor's height along that line, every <see cref="profileStep"/> metres from the low end, as built. Empty: read from the ground when the water starts.</summary>
        public float[] profile=Array.Empty<float>();public float profileStep=.5f;
        /// <summary>Dry standing along the hall, lowest first; each one's own right is the way a second body stands beside the first.</summary>
        public Transform[] ledges=Array.Empty<Transform>();public float ledgeReach=2.4f;
        public string sound="water_rush";
        public bool Rising{get;private set;}
        public bool Stopped{get;private set;}
        /// <summary>The last ledge the leading hero stood on since the water started.</summary>
        public Transform LastLedge{get;private set;}
        float level,holdUntil,bandFrom,bandTo;Telegraph band;AudioManager.Loop rush;
        static readonly RaycastHit[] Hits=new RaycastHit[8];
        void Start()
        {
            if(GameServices.Current!=null&&!string.IsNullOrEmpty(stopFlag)&&GameServices.Current.Flags.GetBool(stopFlag))Stopped=true;
        }
        void OnTriggerEnter(Collider other)=>Crossed(other);
        void OnTriggerStay(Collider other)=>Crossed(other);
        void Crossed(Collider other)
        {
            if(Rising||Stopped||!isActiveAndEnabled)return;
            var party=PartyController.Current;if(party==null||party.Active==null||other.gameObject!=party.Active.gameObject)return;
            Begin();
        }
        /// <summary>The water starts to climb, if it may.</summary>
        public void Begin()
        {
            var services=GameServices.Current;if(Rising||Stopped||water==null||services==null)return;
            if(!string.IsNullOrEmpty(stopFlag)&&services.Flags.GetBool(stopFlag)){Stopped=true;return;}
            if(!string.IsNullOrEmpty(startFlag)&&!services.Flags.GetBool(startFlag))return;
            if(profile.Length<2)Measure();
            Rising=true;level=Mathf.Max(restLevel,water.Level);holdUntil=0;LastLedge=null;
            rush=AudioManager.StartLoop(sound,transform,.18f,AudioBus.SFX,120);
            Debug.Log("FLOOD_RISING "+name);
        }
        /// <summary>The floor along the hall's line, from the ground itself: the highest thing under each point that isn't somebody standing there.</summary>
        void Measure()
        {
            if(low==null||high==null)return;
            var span=high.position-low.position;float run=new Vector2(span.x,span.z).magnitude;int count=Mathf.Max(2,Mathf.CeilToInt(run/profileStep)+1);
            profile=new float[count];
            for(int i=0;i<count;i++)
            {
                var at=Vector3.Lerp(low.position,high.position,i/(float)(count-1));float top=Mathf.Max(low.position.y,high.position.y)+4;float ground=at.y;bool found=false;
                int hits=Physics.RaycastNonAlloc(new Vector3(at.x,top,at.z),Vector3.down,Hits,top-Mathf.Min(low.position.y,high.position.y)+6,~0,QueryTriggerInteraction.Ignore);
                for(int h=0;h<hits;h++)
                {
                    if(Hits[h].collider.GetComponentInParent<Health>()!=null)continue;
                    if(!found||Hits[h].point.y>ground){ground=Hits[h].point.y;found=true;}
                }
                profile[i]=ground;
            }
        }
        void Update()
        {
            var services=GameServices.Current;if(services==null||water==null)return;
            if(!Stopped&&!string.IsNullOrEmpty(stopFlag)&&services.Flags.GetBool(stopFlag))
            {
                Stopped=true;
                if(Rising||water.Level>restLevel+.01f){water.MoveTo(restLevel,drainSeconds);Debug.Log("FLOOD_DRAINED "+name);}
                Rising=false;Quiet();return;
            }
            if(!Rising||GameTime.Paused||services.Input.Blocked)return;
            var party=PartyController.Current;var hero=party!=null?party.Active:null;
            if(hero!=null)Reach(hero.transform.position);
            if(Time.time>=holdUntil&&level<topLevel){level=Mathf.Min(topLevel,level+rate*Time.deltaTime);water.MoveTo(level);}
            Mark();
            if(hero!=null&&hero.Health.Alive&&water.Depth(hero.transform.position)>=caughtDepth)Catch(party,hero);
        }
        /// <summary>A ledge counts as reached while it is still dry: one the water has already taken is nowhere to be stood back on.</summary>
        void Reach(Vector3 at)
        {
            foreach(var ledge in ledges)
            {
                if(ledge==null||ledge==LastLedge||ledge.position.y<level+.05f)continue;
                var offset=at-ledge.position;if(Mathf.Abs(offset.y)>1.2f)continue;offset.y=0;
                if(offset.sqrMagnitude<=ledgeReach*ledgeReach)LastLedge=ledge;
            }
        }
        void Catch(PartyController party,CombatActor hero)
        {
            var ledge=LastLedge!=null?LastLedge:ledges.Length>0&&ledges[0]!=null?ledges[0]:low;if(ledge==null)return;
            // Never the last of it: the water sets the party back, it doesn't end the run.
            float amount=Mathf.Min(hero.Health.maximum*damageShare,hero.Health.integrity-1);
            if(amount>0)hero.Health.Receive(new DamagePacket{amount=amount,type=DamageType.Kinetic});
            CombatVfx.Burst(hero.transform.position+Vector3.up,new Color(.8f,.93f,.9f,.6f),"steam");
            AudioManager.PlayAt(water.splash,null,hero.transform.position,.4f,AudioBus.SFX,40);
            int place=0;
            foreach(var member in party.members)
            {
                if(member==null)continue;
                var to=ledge.position+ledge.right*(place++*1.6f);
                var body=member.GetComponent<CharacterController>();bool was=body!=null&&body.enabled;
                if(body!=null)body.enabled=false;member.transform.position=to;if(body!=null)body.enabled=was;
                if(member.TryGetComponent<GroundMotor>(out var motor))motor.Halt();
                member.Health.InvulnerableUntil=Mathf.Max(member.Health.InvulnerableUntil,GameTime.Now+1);
            }
            level=Mathf.Max(restLevel,Mathf.Min(level,ledge.position.y-fallBack));water.MoveTo(level,.8f);holdUntil=Time.time+grace;
            if(band!=null){band.Clear();band=null;}
            Debug.Log("FLOOD_CAUGHT "+ledge.name);
            Caught?.Invoke(this,ledge);
        }
        /// <summary>The marked lane, laid again when either end has moved a stride.</summary>
        void Mark()
        {
            if(profile.Length<2||low==null||high==null)return;
            int wet=0;while(wet<profile.Length&&profile[wet]<level)wet++;
            int drowned=0;while(drowned<profile.Length&&profile[drowned]<level-caughtDepth)drowned++;
            float end=(profile.Length-1)*profileStep,from=Mathf.Min(drowned*profileStep,end),to=Mathf.Min(wet*profileStep+lead,end);
            if(to-from<.5f||wet==0){if(band!=null){band.Cancel();band=null;}return;}
            if(band!=null&&Mathf.Abs(from-bandFrom)<.35f&&Mathf.Abs(to-bandTo)<.35f)return;
            if(band!=null)band.Clear();
            var axis=high.position-low.position;axis.y=0;axis.Normalize();
            var origin=low.position+axis*from;origin.y=profile[Mathf.Clamp(drowned,0,profile.Length-1)];
            band=Telegraph.Line(origin,axis,to-from,width,99999,Telegraph.Threat,true);band.Steady=true;band.owner=this;bandFrom=from;bandTo=to;
        }
        void Quiet()
        {
            if(band!=null){band.Cancel();band=null;}
            if(rush!=null){rush.Dispose();rush=null;}
        }
        void OnDisable()=>Quiet();
    }
}
