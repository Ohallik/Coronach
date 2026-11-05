using System;
using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>The eight skills learned with Sync level (D201). Each has a ground and a flight variant and a distinct
    /// job in a fight, borrowed from the action games this one learns from:
    /// <list type="bullet">
    /// <item>Taren: Anchor Slam (a leaping gap-closer that breaks guards, Ys/Elden Ring), Tether Hook (a hookshot that
    /// yanks small enemies in and pulls Taren to big ones, Zelda), Forge Spiral (a travelling spin attack for swarms,
    /// Zelda), Faultline (a ground rupture that reaches range, Ys).</item>
    /// <item>Sela: Survey Drone (a turret that keeps firing while she dodges), Waypoint Blink (an evasive blink that
    /// leaves a charge behind), Gravity Fold (a well that gathers a crowd, then implodes), Starfall (a delayed orbital
    /// strike that rewards reading where enemies will be).</item>
    /// </list>
    /// Effects outlive the pose (a drone keeps firing after Sela moves on); a pose interrupted before its release
    /// releases nothing.</summary>
    public sealed class HeroSkills:MonoBehaviour
    {
        CombatActor actor;
        void Awake(){actor=GetComponent<CombatActor>();}
        static readonly Color Amber=new(1.4f,.62f,.18f),Cyan=new(.25f,1.1f,1.6f),Violet=new(.75f,.45f,1.6f);

        DamagePacket Hit(float amount,DamageType type,float breakPower)=>actor.Packet(amount,type,breakPower);
        float Melee(float k,float power)=>actor.damage*k*power;
        float Ranged(float k,float power)=>actor.rangedDamage*k*power;
        Vector3 Body=>transform.position+Vector3.up*(actor.flight?0:.9f);
        float TargetDistance(float fallback){return actor.target!=null&&actor.target.Alive?Planar(actor.target.transform.position-transform.position).magnitude:fallback;}
        static Vector3 Planar(Vector3 v){v.y=0;return v;}

        /// <summary>Runs <paramref name="release"/> after the pose's anticipation, unless the pose is interrupted.</summary>
        void After(float seconds,Action release)=>StartCoroutine(Release(seconds,actor.AttackSequence,release));
        IEnumerator Release(float seconds,int sequence,Action release)
        {
            for(float t=0;t<seconds;)
            {
                if(actor.AttackSequence!=sequence||!actor.Health.Alive||actor.Recovering)yield break;
                if(!GameTime.Paused)t+=Time.deltaTime;yield return null;
            }
            if(actor.AttackSequence==sequence&&actor.Health.Alive)release();
        }

        public void Ground(SkillKind kind,float power,Vector3 aim)
        {
            switch(kind)
            {
                case SkillKind.AnchorSlam:
                {
                    actor.Perform("Attack3",.62f,.55f);
                    float distance=Mathf.Clamp(TargetDistance(4)-1.4f,1.5f,7f);
                    actor.Health.InvulnerableUntil=Mathf.Max(actor.Health.InvulnerableUntil,GameTime.Now+.45f);
                    After(.1f,()=>actor.motor.Dash(aim,distance));
                    After(.42f,()=>Slam(transform.position+aim*.8f,3.2f,Hit(Melee(2.2f,power),DamageType.Kinetic,70)));
                    break;
                }
                case SkillKind.TetherHook:
                    actor.Perform("Pulse",.55f,.45f);
                    After(.2f,()=>TetherLine.Cast(actor,Body+aim*.6f,aim,12,Hit(Melee(.9f,power),DamageType.Pulse,45)));
                    break;
                case SkillKind.ForgeSpiral:
                    actor.Perform("Cleave",1f,.9f);
                    StartCoroutine(Spiral(power,aim));
                    break;
                case SkillKind.Faultline:
                    actor.Perform("Attack3",.55f,.5f);
                    After(.3f,()=>FaultlineWave.Release(actor,transform.position,aim,false,()=>Hit(Melee(1.3f,power),DamageType.Kinetic,40)));
                    break;
                case SkillKind.SurveyDrone:
                    actor.Perform("Buff",.5f,.42f);
                    After(.34f,()=>SurveyDrone.Deploy(actor,power));
                    break;
                case SkillKind.WaypointBlink:
                    Blink(power,aim,false);
                    break;
                case SkillKind.GravityFold:
                    actor.Perform("Pulse",.65f,.56f);
                    After(.255f,()=>GravityWell.Throw(actor,Body+aim*.6f,Destination(aim,7),false,power));
                    break;
                case SkillKind.Starfall:
                    actor.Perform("Buff",.6f,.5f);
                    After(.42f,()=>StarfallStrike.Call(actor,Destination(aim,7),false,power));
                    break;
            }
        }

        public void Flight(SkillKind kind,float power,Vector3 aim)
        {
            switch(kind)
            {
                case SkillKind.AnchorSlam:
                    actor.Perform("Dash",.4f,.2f);actor.motor.Dash(aim,11);
                    GetComponent<FlightDashContact>().Begin(Hit(Melee(2.2f,power),DamageType.Kinetic,70));
                    After(.2f,()=>{FlightWave.Release(transform.position+Vector3.up*.8f,aim,Hit(Melee(.9f,power),DamageType.Kinetic,30),false);Sound("explosionCrunch_000",.32f,.8f);});
                    break;
                case SkillKind.TetherHook:
                    actor.Perform("Shoot",.4f,.16f);
                    TetherLine.Cast(actor,transform.position+Vector3.up*.8f+aim,aim,14,Hit(Melee(.9f,power),DamageType.Pulse,45));
                    break;
                case SkillKind.ForgeSpiral:
                    actor.Perform("Pulse",.7f,.3f);
                    for(int i=0;i<3;i++)After(i*.22f,()=>FlightWave.Release(transform.position+Vector3.up*.8f,actor.motor.Facing,Hit(Melee(.9f,power),DamageType.Plasma,25),false));
                    break;
                case SkillKind.Faultline:
                    actor.Perform("Pulse",.5f,.24f);
                    FaultlineWave.Release(actor,transform.position+Vector3.up*.8f,aim,true,()=>Hit(Melee(1.3f,power),DamageType.Kinetic,40));
                    break;
                case SkillKind.SurveyDrone:
                    actor.Perform("Buff",.4f,.16f);SurveyDrone.Deploy(actor,power);
                    break;
                case SkillKind.WaypointBlink:
                    Blink(power,aim,true);
                    break;
                case SkillKind.GravityFold:
                    actor.Perform("Shoot",.4f,.16f);
                    GravityWell.Throw(actor,transform.position+Vector3.up*.8f+aim,Destination(aim,8)+Vector3.up*.8f,true,power);
                    break;
                case SkillKind.Starfall:
                    actor.Perform("Buff",.45f,.2f);
                    StarfallStrike.Call(actor,Destination(aim,8)+Vector3.up*.8f,true,power);
                    break;
            }
        }

        Vector3 Destination(Vector3 aim,float fallback)
        {
            if(actor.target!=null&&actor.target.Alive){var at=actor.target.transform.position;at.y=transform.position.y;return at;}
            return transform.position+aim*fallback;
        }

        void Slam(Vector3 point,float radius,DamagePacket packet)
        {
            if(CombatCover.Sweep(Body,point+Vector3.up*.9f,.2f,out var wall))point=Body+(point+Vector3.up*.9f-Body).normalized*Mathf.Max(0,wall.distance-.2f)-Vector3.up*.9f;
            CombatActor.Strike(point+Vector3.up*.9f,radius,packet).life=.14f;
            SkillRing.Expand(point,radius,Amber,.35f,false);CombatVfx.Burst(point+Vector3.up*.3f,new Color(1,.6f,.2f),"hit");
            Sound("explosionCrunch_000",.38f,.85f);
        }

        IEnumerator Spiral(float power,Vector3 aim)
        {
            int sequence=actor.AttackSequence;var form=GetComponent<FormController>();
            var visual=form!=null&&form.shaped!=null?form.shaped.transform:null;var rest=visual!=null?visual.localRotation:Quaternion.identity;
            float elapsed=0;int ticks=0;
            try
            {
                while(elapsed<.86f)
                {
                    if(actor.AttackSequence!=sequence||!actor.Health.Alive||actor.Recovering)yield break;
                    if(!GameTime.Paused)
                    {
                        float dt=Time.deltaTime;elapsed+=dt;
                        if(visual!=null)visual.localRotation=rest*Quaternion.Euler(0,720*Mathf.Clamp01(elapsed/.86f),0);
                        if(actor.motor is GroundMotor ground)ground.MoveAction(aim*(2.2f*dt),aim);
                        while(ticks<5&&elapsed>=.15f*(ticks+1)-.05f)
                        {
                            ticks++;
                            CombatActor.Strike(Body,2.9f,Hit(Melee(.75f,power),DamageType.Plasma,22));
                            SkillRing.Expand(transform.position,2.9f,new Color(1.5f,.5f,.15f),.18f,false);
                            Sound("impactMetal_000",.16f,1.1f+ticks*.06f);
                        }
                    }
                    yield return null;
                }
            }
            finally{if(visual!=null)visual.localRotation=rest;}
        }

        void Blink(float power,Vector3 aim,bool flight)
        {
            var origin=transform.position+(flight?Vector3.up*.8f:Vector3.zero);
            Vector3 away=actor.target!=null&&actor.target.Alive?-Planar(actor.target.transform.position-transform.position).normalized:-actor.motor.Facing;
            if(away.sqrMagnitude<.01f)away=-aim;
            actor.Perform("Dodge",flight?.3f:.35f,flight?.18f:.3f);
            actor.Health.InvulnerableUntil=Mathf.Max(actor.Health.InvulnerableUntil,GameTime.Now+.4f);
            var facing=actor.motor.Facing;
            if(flight){actor.motor.Dash(aim,8);GetComponent<FlightDashContact>().Begin(Hit(Ranged(.8f,power),DamageType.Pulse,20));}
            else actor.motor.Dash(away,5.5f);
            CombatVfx.Burst(origin+(flight?Vector3.zero:Vector3.up*.9f),Cyan,"shot");Sound("forceField_000",.22f,1.35f);
            WaypointCharge.Leave(actor,origin,flight,Hit(Ranged(1.7f,power),DamageType.Pulse,55));
        }

        internal static void Sound(string key,float volume,float pitch)=>AudioManager.PlayAt(key,null,Vector3.zero,volume,AudioBus.SFX,128,.04f,pitch);
    }

    /// <summary>An expanding marker on the ground (or the flight plane) for a skill's reach.</summary>
    public sealed class SkillRing:MonoBehaviour
    {
        GroundRing ring;FlightEnergyArc arc;float radius,life,age,from;Color color;bool shrink;
        public static SkillRing Expand(Vector3 center,float radius,Color color,float life,bool flight,bool shrink=false,float from=0)
        {
            var go=new GameObject("Skill ring",typeof(SkillRing));go.transform.position=center;
            var r=go.GetComponent<SkillRing>();r.radius=radius;r.color=color;r.life=life;r.shrink=shrink;r.from=from;
            if(flight){r.arc=FlightEnergyArc.Create("Skill ring arc");r.arc.transform.SetParent(go.transform,false);r.arc.Draw(0,180,Color.clear,true);}
            else{r.ring=go.AddComponent<GroundRing>();r.ring.Initialize(Mathf.Max(radius,from)+.5f);r.ring.Draw(0,Color.clear);}
            return r;
        }
        void Update()
        {
            if(GameTime.Paused)return;
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/life);
            float r=shrink?Mathf.Lerp(from,radius,t):radius*Mathf.Clamp01(age/(life*.7f));
            var c=color;c.a=Mathf.Clamp01((1-t)*1.6f);
            if(ring!=null)ring.Draw(r,c);if(arc!=null)arc.Draw(r,180,c,true);
            if(age>=life)Destroy(gameObject);
        }
    }

    /// <summary>Taren's hook: flies out along the aim, catches the first enemy it touches. Small bodies are reeled in and
    /// left staggered; heavy bodies and bosses pull Taren in instead.</summary>
    public sealed class TetherLine:MonoBehaviour
    {
        CombatActor owner;DamagePacket packet;Vector3 direction,tip;float travelled,length;LineRenderer line;Material material;
        Health caught;bool reeling;float reelAge;
        public static void Cast(CombatActor owner,Vector3 origin,Vector3 direction,float length,DamagePacket packet)
        {
            var go=new GameObject("Tether hook",typeof(TetherLine));go.transform.position=origin;
            var t=go.GetComponent<TetherLine>();t.owner=owner;t.packet=packet;t.direction=direction.normalized;t.length=length;t.tip=origin;
            t.line=go.AddComponent<LineRenderer>();t.line.positionCount=2;t.line.useWorldSpace=true;t.line.startWidth=.07f;t.line.endWidth=.12f;
            t.material=new Material(Resources.Load<Material>("Effects/flare_01"));t.material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            t.line.sharedMaterial=t.material;t.line.startColor=new Color(1,.5f,.15f,.6f);t.line.endColor=new Color(1.6f,.9f,.3f,1);
            t.line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            HeroSkills.Sound("laserSmall_000",.24f,.7f);
        }
        Vector3 Hand=>owner!=null?owner.transform.position+Vector3.up*(owner.flight?.8f:1.1f):transform.position;
        void Update()
        {
            if(GameTime.Paused)return;
            if(owner==null||!owner.Health.Alive){Destroy(gameObject);return;}
            if(!reeling)
            {
                float step=45*Time.deltaTime;var next=tip+direction*step;
                if(CombatCover.Sweep(tip,next,.1f,out var wall)){tip+=direction*Mathf.Max(0,wall.distance-.05f);Draw();CombatVfx.Burst(tip,new Color(1,.6f,.2f),"hit");Destroy(gameObject,.08f);enabled=false;return;}
                tip=next;travelled+=step;
                foreach(var health in Health.All.ToArray())
                {
                    if(health==null||!health.Targetable||health.friendly||health==owner.Health)continue;
                    var body=health.transform.position+Vector3.up*(owner.flight?0:.8f);
                    if(Vector3.Distance(Planar(body),Planar(tip))>1.1f||Mathf.Abs(body.y-tip.y)>2.2f)continue;
                    caught=health;break;
                }
                if(caught!=null)
                {
                    caught.Receive(packet);CombatVfx.Burst(tip,new Color(1,.6f,.2f),"hit");HeroSkills.Sound("impactMetal_000",.3f,.9f);
                    var brain=caught.GetComponent<EnemyBrain>();
                    if(brain!=null&&brain.CanBeMoved&&caught.Alive){reeling=true;}
                    else
                    {
                        // A body too heavy to move pulls the hero to it instead.
                        var gap=Planar(caught.transform.position-owner.transform.position);
                        if(gap.magnitude>2.4f)owner.motor.Dash(gap.normalized,gap.magnitude-2f);
                        Destroy(gameObject,.12f);enabled=false;
                    }
                }
                else if(travelled>=length){Destroy(gameObject,.05f);enabled=false;}
                Draw();return;
            }
            reelAge+=Time.deltaTime;
            if(caught==null||!caught.Alive){Destroy(gameObject);return;}
            var landing=owner.transform.position+owner.motor.Facing*1.7f;
            var delta=Planar(landing-caught.transform.position);
            float pull=Mathf.Min(delta.magnitude,24*Time.deltaTime);
            if(pull>.01f)caught.GetComponent<EnemyBrain>().Displace(delta.normalized*pull);
            tip=caught.transform.position+Vector3.up*(owner.flight?0:.8f);Draw();
            if(delta.magnitude<.25f||reelAge>.5f){caught.Expose(.9f);Destroy(gameObject);}
        }
        static Vector3 Planar(Vector3 v){v.y=0;return v;}
        void Draw(){line.SetPosition(0,Hand);line.SetPosition(1,tip);}
        void OnDestroy(){if(material!=null)Destroy(material);}
    }

    /// <summary>Faultline: the ground (or the flight plane) ruptures in a line of bursts that stop at cover.</summary>
    public sealed class FaultlineWave:MonoBehaviour
    {
        CombatActor owner;Func<DamagePacket> packet;Vector3 origin,direction;bool flight;int burst;float age;
        readonly HashSet<Health> victims=new();
        public static void Release(CombatActor owner,Vector3 origin,Vector3 direction,bool flight,Func<DamagePacket> packet)
        {
            var go=new GameObject("Faultline",typeof(FaultlineWave));
            var f=go.GetComponent<FaultlineWave>();f.owner=owner;f.origin=origin;f.direction=direction.normalized;f.flight=flight;f.packet=packet;
            HeroSkills.Sound("explosionCrunch_000",.3f,1.15f);
        }
        void Update()
        {
            if(GameTime.Paused)return;
            age+=Time.deltaTime;
            while(burst<5&&age>=burst*.07f)
            {
                float distance=2+burst*2;var point=origin+direction*distance;
                var from=origin+Vector3.up*(flight?0:.9f);var to=point+Vector3.up*(flight?0:.9f);
                if(!CombatCover.Clear(from,to)){Destroy(gameObject);return;}
                var strike=CombatActor.Strike(to,1.7f,packet());strike.hit=victims;strike.life=.1f;
                SkillRing.Expand(point,1.7f,new Color(1.4f,.55f,.15f),.3f,flight);
                CombatVfx.Burst(point+Vector3.up*(flight?0:.25f),new Color(1,.55f,.2f),"hit");
                burst++;
            }
            if(burst>=5)Destroy(gameObject,.15f);
        }
    }

    /// <summary>Sela's Survey Drone: a small hovering emitter that follows her and fires needles at the nearest enemy
    /// in sight. One at a time; recasting replaces it.</summary>
    public sealed class SurveyDrone:MonoBehaviour
    {
        public const float Lifetime=9,Interval=.45f,Range=14;
        static SurveyDrone current;
        CombatActor owner;float power,age,nextShot;Transform body;float spin;
        public int Shots{get;private set;}
        public static SurveyDrone Current=>current;
        public static SurveyDrone Deploy(CombatActor owner,float power)
        {
            if(current!=null)Destroy(current.gameObject);
            var go=new GameObject("Survey drone",typeof(SurveyDrone));go.transform.position=owner.transform.position+Vector3.up*(owner.flight?1.2f:2.1f);
            var d=go.GetComponent<SurveyDrone>();d.owner=owner;d.power=power;d.nextShot=.3f;current=d;
            var core=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(core.GetComponent<Collider>());core.name="Drone core";
            core.transform.SetParent(go.transform,false);core.transform.localScale=Vector3.one*.32f;
            core.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Blockout/Emission");
            var ringA=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(ringA.GetComponent<Collider>());ringA.name="Drone vane";
            ringA.transform.SetParent(core.transform,false);ringA.transform.localScale=new Vector3(2.3f,.04f,2.3f);ringA.transform.localRotation=Quaternion.Euler(14,0,0);
            ringA.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Blockout/Sela");
            d.body=core.transform;
            CombatVfx.Burst(go.transform.position,new Color(.3f,1,1.6f),"shot");HeroSkills.Sound("forceField_000",.2f,1.5f);
            return d;
        }
        void Update()
        {
            if(GameTime.Paused)return;
            if(owner==null||!owner.Health.Alive){Destroy(gameObject);return;}
            age+=Time.deltaTime;if(age>=Lifetime){CombatVfx.Burst(transform.position,new Color(.3f,1,1.6f),"hit");Destroy(gameObject);return;}
            var side=Vector3.Cross(Vector3.up,owner.motor.Facing).normalized;
            var hover=owner.transform.position+Vector3.up*(owner.flight?1.2f:2.1f)-side*1.1f-owner.motor.Facing*.4f+Vector3.up*Mathf.Sin(age*3)*.12f;
            transform.position=Vector3.Lerp(transform.position,hover,1-Mathf.Exp(-8*Time.deltaTime));
            spin+=Time.deltaTime*240;if(body!=null)body.localRotation=Quaternion.Euler(0,spin,0);
            nextShot-=Time.deltaTime;if(nextShot>0)return;
            Health best=null;float bestDistance=Range;
            foreach(var health in Health.All)
            {
                if(health==null||!health.Targetable||health.friendly)continue;
                var aimPoint=health.transform.position+Vector3.up*(owner.flight?0:.8f);float d=Vector3.Distance(transform.position,aimPoint);
                if(d<bestDistance&&CombatCover.Clear(transform.position,aimPoint)){best=health;bestDistance=d;}
            }
            if(best==null){nextShot=.15f;return;}
            nextShot=Interval;Shots++;
            var target=best.transform.position+Vector3.up*(owner.flight?0:.8f);
            var packet=new DamagePacket{source=owner.Health,amount=owner.rangedDamage*.32f*power*owner.damageScale,type=DamageType.Beam,breakPower=6};
            Projectile.Fire(transform.position,target-transform.position,packet,30);
            HeroSkills.Sound("laserSmall_000",.1f,1.6f);
        }
        void OnDestroy(){if(current==this)current=null;}
    }

    /// <summary>Waypoint Blink's charge: left where Sela stood, it detonates a moment later.</summary>
    public sealed class WaypointCharge:MonoBehaviour
    {
        DamagePacket packet;float age;bool flight;
        public const float Fuse=.6f,Radius=3.2f;
        public static void Leave(CombatActor owner,Vector3 at,bool flight,DamagePacket packet)
        {
            var go=new GameObject("Waypoint charge",typeof(WaypointCharge));go.transform.position=at;
            var c=go.GetComponent<WaypointCharge>();c.packet=packet;c.flight=flight;
            SkillRing.Expand(flight?at:CombatCover.Ground(at+Vector3.up*2),Radius,new Color(.3f,1,1.6f,.7f),Fuse,flight,true,.4f);
        }
        void Update()
        {
            if(GameTime.Paused)return;
            age+=Time.deltaTime;if(age<Fuse)return;
            var center=transform.position+Vector3.up*(flight?0:.9f);
            CombatActor.Strike(center,Radius,packet).life=.12f;
            SkillRing.Expand(flight?transform.position:CombatCover.Ground(transform.position+Vector3.up*2),Radius,new Color(.4f,1.2f,1.8f),.3f,flight);
            CombatVfx.Burst(center,new Color(.3f,1,1.6f),"hit");HeroSkills.Sound("explosionCrunch_000",.28f,1.3f);
            Destroy(gameObject);
        }
    }

    /// <summary>Gravity Fold: a thrown seed opens a well that drags movable enemies to its centre, ticks Pulse damage,
    /// then implodes.</summary>
    public sealed class GravityWell:MonoBehaviour
    {
        public const float Duration=2.2f,PullRadius=6.5f,PullSpeed=4f,TickRadius=3.5f,ImplodeRadius=4f;
        CombatActor owner;Vector3 origin,destination;bool flight,landed;float age,power,nextTick;
        public static GravityWell Throw(CombatActor owner,Vector3 origin,Vector3 destination,bool flight,float power)
        {
            var go=new GameObject("Gravity fold",typeof(GravityWell));go.transform.position=origin;
            var w=go.GetComponent<GravityWell>();w.owner=owner;w.origin=origin;w.flight=flight;w.power=power;
            w.destination=flight?destination:CombatCover.Ground(destination+Vector3.up*2)+Vector3.up*.1f;
            var core=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(core.GetComponent<Collider>());core.name="Fold core";
            core.transform.SetParent(go.transform,false);core.transform.localScale=Vector3.one*.4f;
            core.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Blockout/Emission");
            HeroSkills.Sound("laserSmall_000",.2f,.6f);
            return w;
        }
        void Update()
        {
            if(GameTime.Paused)return;
            if(owner==null){Destroy(gameObject);return;}
            age+=Time.deltaTime;
            if(!landed)
            {
                float t=Mathf.Clamp01(age/.34f);
                var next=Vector3.Lerp(origin,destination,t)+Vector3.up*((flight?0:.8f)*4*t*(1-t));
                if(CombatCover.Sweep(transform.position,next,.12f,out var hit))next=transform.position+(next-transform.position).normalized*Mathf.Max(0,hit.distance-.05f);
                bool blocked=(next-transform.position).sqrMagnitude<1e-6f&&t<1;
                transform.position=next;
                if(t<1&&!blocked)return;
                landed=true;age=0;nextTick=.4f;
                SkillRing.Expand(transform.position,PullRadius*.15f,new Color(.7f,.4f,1.5f,.8f),Duration,flight,true,PullRadius);
                HeroSkills.Sound("forceField_000",.26f,.55f);return;
            }
            float spin=age*540;transform.rotation=Quaternion.Euler(0,spin,0);transform.localScale=Vector3.one*(1+.25f*Mathf.Sin(age*12));
            foreach(var health in Health.All.ToArray())
            {
                if(health==null||!health.Alive||health.friendly)continue;
                var offset=transform.position-health.transform.position;offset.y=0;float distance=offset.magnitude;
                if(distance>PullRadius||distance<.6f)continue;
                if(health.TryGetComponent<EnemyBrain>(out var brain)&&brain.CanBeMoved)brain.Displace(offset.normalized*Mathf.Min(distance-.6f,PullSpeed*Time.deltaTime));
                health.SlowUntil=Mathf.Max(health.SlowUntil,Time.time+.3f);
            }
            nextTick-=Time.deltaTime;
            if(nextTick<=0&&age<Duration)
            {
                nextTick=.4f;
                CombatActor.Strike(transform.position+Vector3.up*(flight?0:.8f),TickRadius,Packet(.25f,12)).life=.08f;
            }
            if(age>=Duration)
            {
                CombatActor.Strike(transform.position+Vector3.up*(flight?0:.8f),ImplodeRadius,Packet(1.4f,45)).life=.12f;
                SkillRing.Expand(transform.position,ImplodeRadius,new Color(.9f,.5f,1.8f),.35f,flight);
                CombatVfx.Burst(transform.position+Vector3.up*(flight?0:.6f),new Color(.7f,.4f,1.5f),"hit");HeroSkills.Sound("explosionCrunch_000",.3f,.7f);
                Destroy(gameObject);
            }
        }
        DamagePacket Packet(float k,float breakPower)=>new(){source=owner.Health,amount=owner.rangedDamage*k*power*owner.damageScale,type=DamageType.Pulse,breakPower=breakPower};
    }

    /// <summary>Starfall: marks a circle, then beams fall into it from above, finishing with a heavy centre strike.</summary>
    public sealed class StarfallStrike:MonoBehaviour
    {
        public const float Warning=.7f,Radius=3.4f;const int Beams=7;
        CombatActor owner;bool flight;float power,age;int fired;
        readonly HashSet<Health> centre=new();
        public static StarfallStrike Call(CombatActor owner,Vector3 at,bool flight,float power)
        {
            var go=new GameObject("Starfall",typeof(StarfallStrike));go.transform.position=flight?at:CombatCover.Ground(at+Vector3.up*2);
            var s=go.GetComponent<StarfallStrike>();s.owner=owner;s.flight=flight;s.power=power;
            SkillRing.Expand(go.transform.position,Radius,new Color(.3f,1,1.6f,.85f),Warning,flight,true,Radius);
            HeroSkills.Sound("forceField_000",.22f,1.7f);
            return s;
        }
        void Update()
        {
            if(GameTime.Paused)return;
            if(owner==null){Destroy(gameObject);return;}
            age+=Time.deltaTime;if(age<Warning)return;
            float beamAge=age-Warning;
            while(fired<Beams&&beamAge>=fired*.085f)
            {
                var offset=UnityEngine.Random.insideUnitCircle*(Radius-.6f);var point=transform.position+new Vector3(offset.x,0,offset.y);
                Beam(point);
                CombatActor.Strike(point+Vector3.up*(flight?0:.8f),1.5f,Packet(.85f,14)).life=.08f;
                fired++;
            }
            if(fired>=Beams&&beamAge>=Beams*.085f+.1f)
            {
                Beam(transform.position);
                var strike=CombatActor.Strike(transform.position+Vector3.up*(flight?0:.8f),2.2f,Packet(1.5f,40));strike.hit=centre;strike.life=.1f;
                SkillRing.Expand(transform.position,Radius,new Color(.4f,1.2f,1.8f),.3f,flight);
                HeroSkills.Sound("explosionCrunch_000",.3f,1.45f);
                Destroy(gameObject);
            }
        }
        void Beam(Vector3 point)
        {
            var go=new GameObject("Starfall beam");var line=go.AddComponent<LineRenderer>();line.positionCount=2;line.useWorldSpace=true;
            line.startWidth=.35f;line.endWidth=.12f;var material=new Material(Resources.Load<Material>("Effects/flare_01"));material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            line.sharedMaterial=material;line.startColor=new Color(.5f,1.4f,2,0);line.endColor=new Color(.6f,1.6f,2.2f,1);
            line.SetPosition(0,point+Vector3.up*14);line.SetPosition(1,point);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(go,.14f);Destroy(material,.15f);
            CombatVfx.Burst(point+Vector3.up*(flight?0:.2f),new Color(.3f,1,1.6f),"hit");HeroSkills.Sound("laserSmall_000",.14f,1.9f);
        }
        DamagePacket Packet(float k,float breakPower)=>new(){source=owner.Health,amount=owner.rangedDamage*k*power*owner.damageScale,type=DamageType.Beam,breakPower=breakPower};
    }
}
