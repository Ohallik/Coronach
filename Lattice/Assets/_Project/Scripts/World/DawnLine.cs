using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.World
{
    /// <summary>
    /// The dawn line (chapter 9's Terminator; docs/maps/Terminator.md, "The dawn"; guide 15.31): on a tidally locked world, the edge of the day coming across the ground behind the party.
    /// One straight line, moving along <see cref="axis"/> on the plan; <see cref="At"/> is where it stands along that way, and a point's <see cref="Ahead"/> is how many metres it stands in
    /// front of it (in the dusk), below nought behind it (in the light).
    /// <para><b>How it moves.</b> At its stretch's speed (<see cref="speeds"/>, by how many couplings are mated, times the player's setting: <see cref="Pace"/>), and never more than
    /// <see cref="reach"/> metres behind the lead: a party that runs on keeps it there, in the top of the frame; one that stops, fights or turns aside sees it come down the screen. It holds
    /// while anyone's hands are on a coupling (<see cref="TreadCoupling.HandsOn"/>), in a conversation and with the game paused. At the run's start and at every restart it stands at its
    /// reach, still, until the lead has walked <see cref="startWalk"/> metres or <see cref="startWait"/> seconds have passed. On the ride out it chases the engine car at
    /// <see cref="rideSpeed"/> and comes no nearer than <see cref="rideGap"/>; once the ride is over it stops where it is (far behind).</para>
    /// <para><b>What it does.</b> A hero more than <see cref="edgeGrace"/> behind the edge burns, faster the longer they stay (<see cref="burnFrom"/> of their bar a second, climbing by
    /// <see cref="burnClimb"/> a second each second; the heat cools a second a second out of it), and falls: a fast burn and then a fall, never an instant kill at the edge. It is the place's
    /// damage (no source). A partner behind the edge while the lead is ahead of it is set down beside the lead, never burnt. Every ordinary enemy it passes is taken (combat's
    /// <see cref="Dawn"/>/DawnTakes, which this registers with its own answers). Shade (<see cref="Shade"/>) is the long shadow the low sun throws along the line's way from each of
    /// <see cref="casters"/>, as long as the sun's pitch makes it: combat reads it (the Heat-scavenger flees it); it never keeps the dawn off anyone.</para>
    /// <para><b>A fall, a load.</b> The checkpoint is the last coupling mated (each sets its spawn and saves); on a load the line stands at its reach behind that checkpoint's spawn
    /// (<see cref="checkpoints"/>: the run's start, then each coupling's), and a party that comes in at the run's start with couplings mated is stood at its checkpoint.</para>
    /// <para><b>How it looks.</b> Sets <c>_DawnAxis</c> for the Lattice/Dawn shader (the light on the ground and walls, frost burning off, the veil at the edge); turns the key light
    /// (<see cref="sun"/>) by how near the line is to the lead (it climbs and whitens as the line comes: shadows shorten); stands the veil and the edge's sparks where the line is; marks the
    /// strip it is about to take (<see cref="markAhead"/>, dusk side only) as a danger; poses every <see cref="DawnTouched"/>. An editor still is posed at render time, the line
    /// <see cref="stillGaps"/> metres behind whichever of <see cref="stills"/> the camera looks at (<see cref="stillGap"/> elsewhere), by globals, the light's turn and property blocks
    /// only.</para>
    /// <para><b>What it says.</b> The lead's <c>dawn</c> bark (far as it first stands in view, near the first time it is within <see cref="nearWithin"/>, on them the first time a hero
    /// burns) and Dorrow's <c>hail.dawn</c> after each; the objective's line (<see cref="ZoneLine"/>: "Keep ahead of the dawn", "Re-mate the coupling (n/3)", "Ride the tread-hall out");
    /// the pursuit track (<see cref="MusicDirector.Fight"/>) from the first time it closes within <see cref="pursueWithin"/> until the ride is over.</para>
    /// Logs <c>DAWN_LIVE</c>, <c>DAWN_RESTART &lt;checkpoint&gt; &lt;at&gt;</c>, <c>DAWN_RESUMED_AT &lt;checkpoint&gt;</c>, <c>DAWN_BURNS_HERO &lt;hero&gt;</c>, <c>DAWN_SPARED &lt;hero&gt;</c>,
    /// <c>DAWN_PURSUIT</c>. Events <see cref="Burnt"/>.
    /// </summary>
    [ExecuteAlways]
    public sealed class DawnLine:MonoBehaviour
    {
        /// <summary>Something that throws a shadow: its footprint on the plan and how tall it stands.</summary>
        [Serializable] public struct Caster{public Vector2 min,max;public float height;}
        public static DawnLine Current{get;private set;}
        /// <summary>A hero has just begun to burn in the light.</summary>
        public static event Action<DawnLine,CombatActor> Burnt;

        // ---------------------------------------------------------------- the line
        /// <summary>The way the line comes on, on the plan (x, z), unit length.</summary>
        public Vector2 axis=new(0,-1);
        public float reach=11;
        /// <summary>Metres a second on Standard, by how many couplings are mated (the run's start, then after each).</summary>
        public float[] speeds={1.6f,2.2f,2.7f,2.7f};
        public float rideSpeed=3.2f,rideGap=6;
        /// <summary>The quickest it closes to its reach behind a party running away from it: more than a sprint, so it is never left out of the frame.</summary>
        public const float Hurry=16;
        public string[] couplingFlags={"home.coupling1","home.coupling2","home.coupling3"};
        /// <summary>The run's start, then each coupling's spawn, in order: where a checkpoint stands.</summary>
        public Transform[] checkpoints=Array.Empty<Transform>();
        public string rideFlag="home.rideDone";
        public float startWalk=6,startWait=8,overBehind=90;
        public TreadRide ride;
        /// <summary>After the ride: the way home (Dorrow's crawler), which the panel's arrow points at until <see cref="homeFlag"/> is set (the talk that brings the hall in, in the Crawl).</summary>
        public Transform home;public string homeFlag="home.treadRecovered";

        // ---------------------------------------------------------------- the rule
        public float burnFrom=.1f,burnClimb=.28f,edgeGrace=.25f;
        public float markAhead=1.6f,markWidth=40;
        public float pursueWithin=8,nearWithin=5;

        // ---------------------------------------------------------------- the look
        public Light sun;public float sunYaw=180,pitchNear=24,pitchFar=9,farGap=26,intensityNear=1.15f,intensityFar=.62f;
        public Color sunNear=new(1,.93f,.82f),sunFar=new(1,.78f,.6f);
        public Transform veil;public ParticleSystem edge;
        public Caster[] casters=Array.Empty<Caster>();
        public Vector3[] stills=Array.Empty<Vector3>();public float[] stillGaps=Array.Empty<float>();public float stillGap=9;

        /// <summary>Where the line stands along its way.</summary>
        public float At{get=>at;set=>at=value;}
        /// <summary>A test may stop it where it is.</summary>
        public bool Held{get;set;}
        /// <summary>It is moving (the party has set off, or the wait is over).</summary>
        public bool Live{get;private set;}
        /// <summary>The ride is over: it stands where it is.</summary>
        public bool Over{get;private set;}
        public bool Pursuing{get;private set;}
        /// <summary>The checkpoint it was put back at on load: 0 the run's start, then each coupling's.</summary>
        public int Checkpoint{get;private set;}
        public float SunPitch{get;private set;}=15;
        float at,startClock,dorrowAt=-1,farAt=-1;int dorrowLine;bool begun,saidNear,saidOn;Vector3 startFrom;
        readonly Dictionary<CombatActor,float> heat=new(),owed=new(),nextTick=new(),nextPuff=new();
        Telegraph mark;AudioManager.Loop roar;
        static readonly int AxisId=Shader.PropertyToID("_DawnAxis");

        // ---------------------------------------------------------------- the line's arithmetic
        public float Along(Vector3 p)=>p.x*axis.x+p.z*axis.y;
        /// <summary>Metres a point stands ahead of the edge: in the dusk above nought, in the light below.</summary>
        public float Ahead(Vector3 p)=>Along(p)-at;
        public Vector3 Way=>new(axis.x,0,axis.y);
        /// <summary>The point on the line level with another (across the way from it).</summary>
        public Vector3 On(Vector3 near)=>near+Way*(at-Along(near));
        /// <summary>The setting's share of the line's speed: Gentle slower, the hardest faster. Nothing else about the line changes with it.</summary>
        public static float Pace(DifficultySetting setting)=>setting switch{DifficultySetting.Gentle=>.7f,DifficultySetting.Demanding=>1.15f,_=>1};
        int Mated(){var services=GameServices.Current;int k=0;while(services!=null&&k<couplingFlags.Length&&services.Flags.GetBool(couplingFlags[k]))k++;return k;}
        /// <summary>Its speed now: its stretch's, at the player's setting.</summary>
        public float Speed=>speeds.Length==0?0:speeds[Mathf.Clamp(Mated(),0,speeds.Length-1)]*Pace(Difficulty.Current);
        /// <summary>One step of the rule, as the line moves in play: at its speed unless hands hold it, and closing to its reach behind the lead (at most <see cref="Hurry"/>) when the
        /// lead runs away from it. Never back. For a test to run a whole stretch without waiting for it.</summary>
        public static float Step(float at,float leadAlong,float speed,float reach,bool hands,float dt)
        {
            if(!hands)at+=speed*dt;
            float least=leadAlong-reach;if(at<least)at=Mathf.Min(least,at+Hurry*dt);
            return at;
        }
        /// <summary>In the long shadow of anything that throws one, as the sun is now.</summary>
        public bool Shade(Vector3 p)
        {
            float length=1/Mathf.Tan(Mathf.Max(3,SunPitch)*Mathf.Deg2Rad);var q=new Vector2(p.x,p.z);
            foreach(var c in casters)if(Swept(q,c,axis,c.height*length))return true;
            return false;
        }
        /// <summary>Whether a point lies in a box swept along a way for a length: the shadow of the box.</summary>
        static bool Swept(Vector2 p,Caster c,Vector2 d,float length)
        {
            float t0=0,t1=length;
            for(int i=0;i<2;i++)
            {
                float pi=i==0?p.x:p.y,lo=i==0?c.min.x:c.min.y,hi=i==0?c.max.x:c.max.y,di=i==0?d.x:d.y;
                if(Mathf.Abs(di)<1e-5f){if(pi<lo||pi>hi)return false;continue;}
                float a=(pi-hi)/di,b=(pi-lo)/di;if(a>b)(a,b)=(b,a);
                t0=Mathf.Max(t0,a);t1=Mathf.Min(t1,b);if(t0>t1)return false;
            }
            return true;
        }

        // ---------------------------------------------------------------- life
        void OnEnable()
        {
            if(Application.isPlaying){Current=this;Dawn.Register(this,Ahead,Shade,true);return;}
            RenderPipelineManager.beginCameraRendering-=Still;RenderPipelineManager.beginCameraRendering+=Still;
            Pose(Along(transform.position)-stillGap,transform.position,stillGap,true);
        }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering-=Still;
            if(!Application.isPlaying)return;
            if(Current==this)Current=null;Dawn.Unregister(this);ZoneLine.Clear(this);MusicDirector.Fight(this,false);
            if(mark!=null){mark.Cancel();mark=null;}if(roar!=null){roar.Dispose();roar=null;}
        }
        /// <summary>The checkpoint, the line's place and the party's, once the party stands in the zone.</summary>
        void Begin(PartyController party)
        {
            begun=true;var lead=party.Active;int k=Mated();Checkpoint=k;
            var services=GameServices.Current;Over=services!=null&&services.Flags.GetBool(rideFlag);
            var anchor=k<checkpoints.Length?checkpoints[k]:null;
            if(Over)at=Along(lead.transform.position)-overBehind;
            else if(anchor!=null)
            {
                // In at the run's start with couplings mated (back from the Crawl by the chart, mid-run): stood at the checkpoint, where a retry would have put them.
                if(k>0&&Along(lead.transform.position)<Along(anchor.position)-3)
                {
                    int place=0;foreach(var member in party.members)if(member!=null)Put(member,anchor.position+anchor.right*2*place++);
                    Debug.Log("DAWN_RESUMED_AT "+anchor.name);
                }
                at=Along(anchor.position)-reach;
            }
            else at=Along(lead.transform.position)-reach;
            startFrom=lead.transform.position;startClock=0;Live=false;farAt=Over?-1:Time.unscaledTime+1.2f;
            Debug.Log($"DAWN_RESTART {(anchor!=null?anchor.name:"none")} {at:0.0}");
        }
        void Update()
        {
            if(!Application.isPlaying)return;
            var party=PartyController.Current;if(party==null||party.Active==null||party.members==null)return;
            if(!begun)Begin(party);
            var lead=party.Active;var services=GameServices.Current;
            if(!Over&&services!=null&&services.Flags.GetBool(rideFlag)){Over=true;Pursue(false);}
            bool riding=ride!=null&&ride.Riding;
            float dt=Time.deltaTime;
            if(!GameTime.CombatHeld&&!Held&&!Over)
            {
                if(!Live)
                {
                    startClock+=dt;var moved=lead.transform.position-startFrom;moved.y=0;
                    if(moved.magnitude>=startWalk||startClock>=startWait||riding){Live=true;Debug.Log("DAWN_LIVE");}
                }
                if(Live)
                {
                    if(riding)at=Mathf.Min(at+rideSpeed*dt,Mathf.Max(at,Along(lead.transform.position)-rideGap));
                    else at=Step(at,Along(lead.transform.position),Speed,reach,TreadCoupling.HandsOn,dt);
                }
            }
            float gap=Ahead(lead.transform.position);
            if(!GameTime.CombatHeld)Burn(party,dt);
            Speak(lead,gap,riding);
            Line(lead,riding);
            if(Live&&!Over&&!riding&&gap<=pursueWithin)Pursue(true);
            Pose(at,lead.transform.position,gap,false);
            Mark(lead,riding);
        }

        // ---------------------------------------------------------------- the rule
        void Burn(PartyController party,float dt)
        {
            var lead=party.Active;bool leadOut=lead!=null&&lead.Health.Alive&&Ahead(lead.transform.position)>=-edgeGrace;
            foreach(var hero in party.members)
            {
                if(hero==null||hero.flight||!hero.Health.Alive)continue;
                heat.TryGetValue(hero,out float held);
                if(Ahead(hero.transform.position)>=-edgeGrace){heat[hero]=Mathf.Max(0,held-dt);owed[hero]=0;continue;}
                // The partner doesn't choose where it stands: caught alone, it is set down beside the lead.
                if(hero!=lead&&leadOut){Spare(hero,lead);heat[hero]=0;continue;}
                held+=dt;heat[hero]=held;
                owed.TryGetValue(hero,out float due);due+=hero.Health.maximum*(burnFrom+burnClimb*held)*dt;owed[hero]=due;
                nextTick.TryGetValue(hero,out float tick);
                if(Time.time>=tick)
                {
                    nextTick[hero]=Time.time+.2f;owed[hero]=0;
                    hero.Health.Receive(new DamagePacket{amount=due,type=DamageType.Plasma});
                }
                nextPuff.TryGetValue(hero,out float puff);
                if(Time.time>=puff){nextPuff[hero]=Time.time+.3f;CombatVfx.Burst(hero.transform.position+Vector3.up*1.1f,new Color(1.8f,1.5f,1.05f,.7f),"steam");}
                if(held<=dt*1.01f)
                {
                    Debug.Log("DAWN_BURNS_HERO "+hero.character);Burnt?.Invoke(this,hero);
                    AudioManager.PlayAt("forceField_000",null,hero.transform.position+Vector3.up,.2f,AudioBus.SFX,60,.2f,.5f);
                    if(!saidOn){saidOn=true;Say(lead!=null?lead.character:hero.character,2);}
                }
            }
        }
        void Spare(CombatActor partner,CombatActor lead)
        {
            var right=Vector3.Cross(Vector3.up,Way).normalized;
            foreach(var to in new[]{lead.transform.position+right*1.8f,lead.transform.position-right*1.8f,lead.transform.position+Way*1.8f})
            {
                if(Ahead(to)<1||!Physics.Raycast(to+Vector3.up*3,Vector3.down,out var hit,6,~0,QueryTriggerInteraction.Ignore)||hit.collider.GetComponentInParent<CombatActor>()!=null)continue;
                Put(partner,hit.point+Vector3.up*.05f);Debug.Log("DAWN_SPARED "+partner.character);return;
            }
            Put(partner,lead.transform.position+Way*1.2f);Debug.Log("DAWN_SPARED "+partner.character);
        }
        static void Put(CombatActor member,Vector3 to)
        {
            var body=member.GetComponent<CharacterController>();bool was=body!=null&&body.enabled;
            if(body!=null)body.enabled=false;member.transform.position=to;if(body!=null)body.enabled=was;
            if(member.TryGetComponent<GroundMotor>(out var motor))motor.Halt();
            member.Health.InvulnerableUntil=Mathf.Max(member.Health.InvulnerableUntil,GameTime.Now+.6f);
        }

        // ---------------------------------------------------------------- what it says
        /// <summary>The lead's line from the pool by its place (far, near, on them), and Dorrow's answer a breath after.</summary>
        void Say(string lead,int line)
        {
            BarkService.PlayAt(lead,"dawn",line,true);
            dorrowAt=Time.unscaledTime+2.6f;dorrowLine=line;
        }
        void Speak(CombatActor lead,float gap,bool riding)
        {
            if(farAt>0&&Time.unscaledTime>=farAt&&!GameTime.CombatHeld){farAt=-1;Say(lead.character,0);}
            if(!saidNear&&Live&&!riding&&!Over&&gap<nearWithin){saidNear=true;Say(lead.character,1);}
            if(dorrowAt>0&&Time.unscaledTime>=dorrowAt&&!GameTime.CombatHeld){dorrowAt=-1;BarkService.PlayAt("Dorrow","hail.dawn",dorrowLine,true);}
        }
        /// <summary>The objective panel's line and arrow, while the run is on. It only ever moves forward: the story's own step ("Re-mate three couplings and bring the tread-hall in
        /// ahead of the dawn") is done only by Dorrow's talk in the Crawl, so from the ride on this zone keeps its own words rather than let that step show again (D423).</summary>
        public const string RideLine="Ride the tread-hall out",HomeLine="Climb aboard the Meridian Crawl";
        void Line(CombatActor lead,bool riding)
        {
            var services=GameServices.Current;bool homeward=home!=null&&(services==null||!services.Flags.GetBool(homeFlag));
            if(riding){ZoneLine.Say(this,RideLine,homeward?home.position:(Vector3?)null);return;}
            if(Over)
            {
                if(homeward)ZoneLine.Say(this,HomeLine,home.position);else ZoneLine.Clear(this);
                return;
            }
            var next=TreadCoupling.Next;
            if(next==null)
            {
                if(ride!=null&&ride.Woken)ZoneLine.Say(this,RideLine,ride.transform.position);else ZoneLine.Clear(this);
                return;
            }
            var offset=next.transform.position-lead.transform.position;offset.y=0;
            if(offset.magnitude<16)ZoneLine.Say(this,$"Re-mate the coupling ({TreadCoupling.MatedCount}/3)",next.transform.position);
            else ZoneLine.Say(this,Live?"Keep ahead of the dawn":null,next.transform.position);
        }
        void Pursue(bool on)
        {
            if(on==Pursuing)return;Pursuing=on;MusicDirector.Fight(this,on);
            if(on)Debug.Log("DAWN_PURSUIT");
        }

        // ---------------------------------------------------------------- how it looks
        /// <summary>The strip it is about to take, marked as every danger is, on the dusk side of the edge only, so its rim lies on dark ground.</summary>
        void Mark(CombatActor lead,bool riding)
        {
            if(!Live||Over||riding){if(mark!=null){mark.Cancel();mark=null;}return;}
            var origin=On(lead.transform.position)+Way*.02f;
            if(Physics.Raycast(origin+Vector3.up*20,Vector3.down,out var hit,60,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<CombatActor>()==null)origin.y=hit.point.y;
            if(mark==null){mark=Telegraph.Line(origin,Way,markAhead,markWidth,99999,Telegraph.Threat,false);mark.Steady=true;mark.owner=this;}
            else mark.MoveTo(origin);
        }
        /// <summary>Everything the line's place decides: the shader's global, the sun, the veil and the sparks, every touched thing; and in play the roar.</summary>
        void Pose(float line,Vector3 focus,float gap,bool still)
        {
            Shader.SetGlobalVector(AxisId,new Vector4(axis.x,axis.y,line,0));
            float near=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(gap/Mathf.Max(1,farGap)));
            SunPitch=Mathf.Lerp(pitchFar,pitchNear,near);
            if(sun!=null)
            {
                sun.transform.rotation=Quaternion.Euler(SunPitch,sunYaw,0);
                sun.intensity=Mathf.Lerp(intensityFar,intensityNear,near);sun.color=Color.Lerp(sunFar,sunNear,near);
            }
            var edgeAt=On(focus);
            if(veil!=null)veil.SetPositionAndRotation(new Vector3(edgeAt.x,transform.position.y,edgeAt.z),Quaternion.LookRotation(Way,Vector3.up));
            if(edge!=null)edge.transform.SetPositionAndRotation(new Vector3(edgeAt.x,transform.position.y+.2f,edgeAt.z),Quaternion.LookRotation(Way,Vector3.up));
            foreach(var touched in Touched())touched.Pose(line,still);
            if(still)return;
            if(roar==null&&edge!=null)roar=AudioManager.StartLoop("thrusterFire_000",edge.transform,0,AudioBus.SFX,140);
            roar?.Set(Over?0:.24f*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((gap-3)/16))),.5f);
        }
        DawnTouched[] touched;
        DawnTouched[] Touched(){if(touched==null||touched.Length>0&&touched[0]==null)touched=FindObjectsByType<DawnTouched>(FindObjectsSortMode.None);return touched;}
        /// <summary>An editor still, as its camera renders: the line a still's gap behind what the camera looks at.</summary>
        void Still(ScriptableRenderContext context,Camera camera)
        {
            if(Application.isPlaying||camera==null||camera.cameraType==CameraType.Preview||camera.cameraType==CameraType.Reflection)return;
            var ray=camera.transform.forward;float down=-ray.y;if(down<.05f)return;
            var focus=camera.transform.position+ray*((camera.transform.position.y-transform.position.y)/down);
            float gap=stillGap,best=24*24;
            for(int i=0;i<stills.Length&&i<stillGaps.Length;i++){var d=stills[i]-focus;d.y=0;if(d.sqrMagnitude<best){best=d.sqrMagnitude;gap=stillGaps[i];}}
            Pose(Along(focus)-gap,focus,gap,true);
        }
        /// <summary>The lead's distance ahead of the edge, for a test.</summary>
        public float Gap{get{var party=PartyController.Current;return party!=null&&party.Active!=null?Ahead(party.Active.transform.position):float.PositiveInfinity;}}
        /// <summary>The marking it lays, for a test.</summary>
        public Telegraph Marking=>mark;
    }
}
