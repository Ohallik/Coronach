using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>What a companion needs to know to spend Idra's Charge as a player would (S6a). PartnerBrain asks, through LateSkills; the rules live with the skills.
    /// Three of them are urgent (<see cref="Urgent"/>): a dome for shots already coming in, a flare for a lead nearly down and a line to take the hits off a lead
    /// being beaten answer what's happening, not the six to nine seconds between a companion's skills.</summary>
    public static class IdraSkills
    {
        /// <summary>Under this share of their Integrity the other hero is worth a Rescue Flare; over this share of her own she'll hold the line; and how many ordinary
        /// bodies near the lead make that worth doing.</summary>
        public const float FlareBelow=.35f,HoldAbove=.5f;
        public const int HoldCrowd=2;
        /// <summary>Counterweight: something has to be this near to hit her, and she needs this much Thrust to guard what it throws. And how soon a shot has to be
        /// arriving, and how near a hero it has to pass, to be worth a dome.</summary>
        public const float CounterNear=5,CounterThrust=40,ShotSeconds=1.5f,ShotPasses=1.5f;
        /// <summary>Take the Weight: under this share of their Integrity, with an enemy this near them, the other hero is worth standing in for. And how many ordinary
        /// bodies near her make a Bulkhead worth raising.</summary>
        public const float WeightBelow=.6f,WeightNear=8;
        public const int BulkheadCrowd=2;
        public static bool Is(SkillKind kind)=>kind>=SkillKind.Breach&&kind<=SkillKind.HoldTheLine||kind>=SkillKind.TakeTheWeight&&kind<=SkillKind.Bulkhead;
        static Vector3 Planar(Vector3 v){v.y=0;return v;}
        /// <summary>How near the target has to be for the skill to be worth a try. Six of them answer the party's trouble and have no reach.</summary>
        public static float Reach(CombatActor actor,SkillKind kind)=>kind switch
        {
            SkillKind.Breach=>(actor.flight?HeroSkills.BreachFlightLength:HeroSkills.BreachLength)+1,
            SkillKind.TowLine=>(actor.flight?HeroSkills.TowFlightLength:HeroSkills.TowLength)-1,
            SkillKind.SlagHammer=>actor.flight?HeroSkills.SlagFlightReach-1:2.6f,
            SkillKind.Avalanche=>actor.flight?8:6,
            SkillKind.CuttingTorch=>actor.flight?HeroSkills.TorchFlightReach-1:HeroSkills.TorchReach+HeroSkills.TorchRadius-.3f,
            SkillKind.Shelter or SkillKind.RescueFlare or SkillKind.Counterweight or SkillKind.HoldTheLine or SkillKind.TakeTheWeight or SkillKind.Bulkhead=>float.PositiveInfinity,
            _=>12,
        };
        /// <summary>Whether this is one of the two that don't wait their turn, and is wanted now.</summary>
        public static bool Urgent(CombatActor actor,SkillKind kind)=>kind==SkillKind.Shelter?ShotsInbound(actor):kind==SkillKind.RescueFlare?PartnerFailing(actor):kind==SkillKind.TakeTheWeight&&PartnerPressed(actor);
        /// <summary>True when these rules settle it, with the answer in <paramref name="worth"/>; false leaves it to the partner's general rules (a target in front of
        /// her, no wall on the line).</summary>
        public static bool Decides(CombatActor actor,SkillKind kind,out bool worth)
        {
            worth=false;if(!Is(kind))return false;
            switch(kind)
            {
                case SkillKind.Shelter:worth=ShotsInbound(actor);return true;
                case SkillKind.RescueFlare:worth=PartnerFailing(actor);return true;
                // Armed for what's about to hit her: something near enough to, the Thrust to guard it, and none still owed.
                case SkillKind.Counterweight:
                    worth=actor.thrust>=CounterThrust&&actor.TryGetComponent<BraceKit>(out var kit)&&kit.Counterweights==0&&Near(actor,actor.transform.position,CounterNear)>0;
                    return true;
                // A crowd on the lead, and she's fit to take it. Never a boss: Hold the Line doesn't turn one (BraceKit.Holder), so a boss alone is no reason to.
                case SkillKind.HoldTheLine:
                {
                    var party=PartyController.Current;var lead=party!=null?party.Active:null;
                    worth=lead!=null&&lead!=actor&&actor.Health.integrity>=actor.Health.maximum*HoldAbove&&actor.TryGetComponent<BraceKit>(out var line)&&!line.Holding&&
                        Planar(lead.transform.position-actor.transform.position).magnitude<=BraceKit.HoldReach&&Near(actor,lead.transform.position,10)>=HoldCrowd;
                    return true;
                }
                // The other hero hurt with something on them, and she fit to stand in: a boss counts, since this asks nothing of the boss.
                case SkillKind.TakeTheWeight:worth=PartnerPressed(actor);return true;
                // A crowd on her, or shots coming in, and no wall standing already.
                case SkillKind.Bulkhead:worth=BulkheadWall.Latest==null&&(Near(actor,actor.transform.position,WeightNear)>=BulkheadCrowd||ShotsInbound(actor));return true;
            }
            var target=actor.target;if(target==null)return true;
            var to=Planar(target.transform.position-actor.transform.position);float gap=to.magnitude;var hull=actor.transform.position+Vector3.up*.8f;
            switch(kind)
            {
                // A gap-closer, like the slam and the rush: not from on top of the target and not with her nearly down; a hull goes the whole length with nothing steering it.
                case SkillKind.Breach:
                    if(gap<3||actor.Health.integrity<actor.Health.maximum*.3f)return true;
                    return actor.flight&&CombatCover.Sweep(hull,hull+to.normalized*HeroSkills.BreachFlightLength,HeroCollision.HullRadius(actor.character),out _);
                // Nothing to reel that's already at her feet; and a ship pulled to a heavy body goes down the line whether or not the way is clear.
                case SkillKind.TowLine:
                    if(gap<3.5f)return true;
                    if(!actor.flight)return false;
                    var brain=target.GetComponent<EnemyBrain>();bool pulled=brain==null||!brain.Mine&&(!brain.CanBeMoved||brain.Heavy);
                    return pulled&&CombatCover.Sweep(hull,hull+to.normalized*(gap-2),HeroCollision.HullRadius(actor.character),out _);
                // Planted for the wind-up: never where something is about to land.
                case SkillKind.SlagHammer:case SkillKind.CuttingTorch:return !actor.flight&&LateSkills.InDanger(actor);
                case SkillKind.Avalanche:
                    return actor.flight&&CombatCover.Sweep(hull,hull+to.normalized*HeroSkills.AvalancheLeap*3,HeroCollision.HullRadius(actor.character),out _);
            }
            return false;
        }
        /// <summary>Where a thing meant for both heroes is planted: between them if the other is near enough for it to cover both, and where she stands if not.</summary>
        public static Vector3 Between(CombatActor actor,float radius)
        {
            var here=actor.transform.position;var party=PartyController.Current;if(party==null)return here;
            foreach(var member in party.members)
            {
                if(member==null||member==actor||!member.Health.Alive)continue;
                var gap=Planar(member.transform.position-here);
                if(gap.magnitude<=radius*2-1)return here+gap*.5f;
            }
            return here;
        }
        /// <summary>An enemy's shot in the air that will pass a hero the dome would stand over, soon: and no dome up already.</summary>
        public static bool ShotsInbound(CombatActor actor)
        {
            var party=PartyController.Current;if(party==null||ShelterDome.Latest!=null)return false;
            float radius=actor.flight?ShelterDome.FlightRadius:ShelterDome.Radius;var centre=Between(actor,radius);
            foreach(var member in party.members)
            {
                if(member==null||!member.Health.Alive||Planar(member.transform.position-centre).magnitude>radius-.5f)continue;
                foreach(var shot in Projectile.Live)
                {
                    if(shot==null||!shot.Threatens(true))continue;
                    var velocity=Planar(shot.Velocity);if(velocity.sqrMagnitude<.01f)continue;
                    var rel=Planar(member.transform.position-shot.transform.position);float t=Vector3.Dot(rel,velocity)/velocity.sqrMagnitude;
                    if(t<0||t>ShotSeconds)continue;
                    if((rel-velocity*t).magnitude<=ShotPasses)return true;
                }
            }
            return false;
        }
        /// <summary>The other hero down within the flare's reach, or standing under the share of Integrity it's kept for.</summary>
        public static bool PartnerFailing(CombatActor actor)
        {
            var party=PartyController.Current;if(party==null)return false;
            foreach(var member in party.members)
            {
                if(member==null||member==actor||Planar(member.transform.position-actor.transform.position).magnitude>RescueFlare.Reach)continue;
                if(!member.Health.Alive||member.Health.integrity<member.Health.maximum*FlareBelow)return true;
            }
            return false;
        }
        /// <summary>Take the Weight is wanted: the other hero standing within its reach, under the share of Integrity it's kept for, with an awake enemy (a boss as well)
        /// near them; she over the share of her own she'll hold the line at; and no line of hers holding already.</summary>
        public static bool PartnerPressed(CombatActor actor)
        {
            var party=PartyController.Current;if(party==null||actor.Health.integrity<actor.Health.maximum*HoldAbove)return false;
            var line=WeightLine.Latest;if(line!=null&&line.Owner==actor&&line.Holds)return false;
            foreach(var member in party.members)
            {
                if(member==null||member==actor||!member.Health.Alive||member.Health.integrity>=member.Health.maximum*WeightBelow)continue;
                if(Planar(member.transform.position-actor.transform.position).magnitude>WeightLine.Reach)continue;
                foreach(var body in Health.All)
                {
                    if(body==null||body.friendly==actor.Health.friendly||!body.Targetable||Planar(body.transform.position-member.transform.position).magnitude>WeightNear)continue;
                    if(body.TryGetComponent<EnemyBrain>(out var brain)&&!brain.Passive&&!brain.Mine)return true;
                }
            }
            return false;
        }
        /// <summary>Ordinary bodies awake within a distance of a place: not a boss, not a mine, not one standing passive.</summary>
        static int Near(CombatActor actor,Vector3 at,float within)
        {
            int count=0;
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly==actor.Health.friendly||!body.Targetable||Planar(body.transform.position-at).magnitude>within)continue;
                if(!body.TryGetComponent<EnemyBrain>(out var brain)||brain.Passive||brain.Mine||brain.definition==null||brain.definition.boss)continue;
                count++;
            }
            return count;
        }
    }

    /// <summary>What a tow line can drag that isn't an enemy: wreckage, a snagged barge, a crew pod. The world's things implement it; the line only pulls, a step at a
    /// time, toward whoever cast it.</summary>
    public interface ITowable
    {
        bool CanTow{get;}
        void Tow(Vector3 toward,float metres);
    }

    /// <summary>Idra's Tow Line: a harpoon out along her aim that takes the first thing it touches. A light body is reeled in to her feet and left open for a moment;
    /// a heavy one, or a boss, pulls her to it instead; a mine is only struck. Wreckage that can be towed (<see cref="ITowable"/>) is dragged toward her for as long
    /// as the reel runs. It stops at walls, as a hook does.</summary>
    public sealed class TowCable:MonoBehaviour
    {
        public const float Speed=45,ReelSeconds=.5f,ReelSpeed=24,Open=.6f;
        public static TowCable Latest{get;private set;}
        /// <summary>What it took hold of, if anything: an enemy, or a thing of the world's.</summary>
        public Health Caught{get;private set;}
        public ITowable Towing{get;private set;}
        CombatActor owner;DamagePacket packet;Vector3 direction,tip;float travelled,length,reelAge;LineRenderer line;Material material;bool reeling;
        static readonly Collider[] Touched=new Collider[32];
        public static TowCable Cast(CombatActor owner,Vector3 origin,Vector3 direction,float length,DamagePacket packet)
        {
            var go=new GameObject("Tow line",typeof(TowCable));go.transform.position=origin;
            var cable=go.GetComponent<TowCable>();cable.owner=owner;cable.packet=packet;cable.direction=direction.normalized;cable.length=length;cable.tip=origin;Latest=cable;
            cable.line=go.AddComponent<LineRenderer>();cable.line.positionCount=2;cable.line.useWorldSpace=true;cable.line.startWidth=.09f;cable.line.endWidth=.14f;
            cable.material=new Material(Resources.Load<Material>("Effects/flare_01"));cable.material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            cable.line.sharedMaterial=cable.material;cable.line.startColor=new Color(1,.55f,.15f,.6f);cable.line.endColor=new Color(1.7f,1,.35f,1);
            cable.line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;cable.Draw();
            HeroSkills.Sound(owner,origin,"laserSmall_000",.24f,.6f);
            return cable;
        }
        static Vector3 Planar(Vector3 v){v.y=0;return v;}
        Vector3 Hand=>owner.transform.position+Vector3.up*(owner.flight?.8f:1.1f);
        void Draw(){line.SetPosition(0,owner!=null?Hand:transform.position);line.SetPosition(1,tip);}
        void End(float after=0){enabled=false;Destroy(gameObject,after);}
        void Update()
        {
            if(GameTime.CombatHeld)return;
            if(owner==null||!owner.Health.Alive){Destroy(gameObject);return;}
            if(!reeling){Fly();Draw();return;}
            reelAge+=Time.deltaTime;
            if(Caught!=null)
            {
                if(!Caught.Alive){Destroy(gameObject);return;}
                var delta=Planar(owner.transform.position+owner.motor.Facing*1.9f-Caught.transform.position);float pull=Mathf.Min(delta.magnitude,ReelSpeed*Time.deltaTime);
                if(pull>.01f)Caught.GetComponent<EnemyBrain>().Displace(delta.normalized*pull);
                tip=Caught.transform.position+Vector3.up*(owner.flight?0:.8f);Draw();
                if(delta.magnitude<.25f||reelAge>ReelSeconds){Caught.Expose(Open);Destroy(gameObject);}
                return;
            }
            var body=Towing as Component;
            if(body==null||!Towing.CanTow||reelAge>ReelSeconds){Destroy(gameObject);return;}
            var toward=Planar(owner.transform.position-body.transform.position);
            if(toward.magnitude>3)Towing.Tow(toward.normalized,Mathf.Min(toward.magnitude-3,ReelSpeed*.5f*Time.deltaTime));
            tip=body.transform.position+Vector3.up*(owner.flight?0:.8f);Draw();
        }
        /// <summary>The frame's flight in steps no longer than half what the head catches, so a slow frame can't carry it clean past a body or through a thin wall.</summary>
        void Fly(){for(float left=Speed*Time.deltaTime;left>0&&enabled&&!reeling;left-=.6f)Advance(Mathf.Min(left,.6f));}
        void Advance(float step)
        {
            var next=tip+direction*step;
            // The tip rides the ground at body height, as the hook's does: a gentle rise isn't a wall.
            if(!owner.flight){float rise=CombatCover.Ground(next+Vector3.up*2.5f).y+.9f-next.y;if(Mathf.Abs(rise)<=step*.8f)next.y+=rise;}
            if(CombatCover.Sweep(tip,next,.1f,out var wall)){tip+=direction*Mathf.Max(0,wall.distance-.05f);CombatVfx.Burst(tip,new Color(1,.6f,.2f),"hit");End(.08f);return;}
            tip=next;travelled+=step;
            foreach(var health in Health.All.ToArray())
            {
                if(health==null||!health.Targetable||health.friendly==owner.Health.friendly)continue;
                var at=health.transform.position+Vector3.up*(owner.flight?0:.8f);
                if(Vector3.Distance(Planar(at),Planar(tip))>1.1f||Mathf.Abs(at.y-tip.y)>2.2f)continue;
                Caught=health;break;
            }
            if(Caught!=null)
            {
                Caught.Receive(packet);CombatVfx.Burst(tip,new Color(1,.6f,.2f),"hit");HeroSkills.Sound(owner,tip,"impactMetal_000",.3f,.8f);
                var brain=Caught.GetComponent<EnemyBrain>();
                // A line that kills, or that takes a mine, moves nobody.
                if(!Caught.Alive||brain!=null&&brain.Mine)End(.12f);
                else if(brain!=null&&brain.CanBeMoved&&!brain.Heavy){reeling=true;brain.Interrupt();}
                else
                {
                    // Too heavy to move: it's she who goes.
                    var gap=Planar(Caught.transform.position-owner.transform.position);
                    if(gap.magnitude>2.4f)owner.motor.Dash(gap.normalized,gap.magnitude-2);
                    End(.12f);
                }
                return;
            }
            int count=Physics.OverlapSphereNonAlloc(tip,1.1f,Touched,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                var thing=Touched[i].GetComponentInParent<ITowable>();if(thing==null||!thing.CanTow)continue;
                Towing=thing;reeling=true;CombatVfx.Burst(tip,new Color(1,.6f,.2f),"hit");HeroSkills.Sound(owner,tip,"impactMetal_000",.3f,.7f);return;
            }
            if(travelled>=length)End(.05f);
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(Latest==this)Latest=null;}
    }

    /// <summary>Shelter: Idra plants the brace and a dome stands over it for five seconds. An enemy's shot that reaches the dome from outside burns out on it, and any
    /// marking that was carrying that shot goes with it (Telegraph follows its shots, and a spent one is no longer flying). It stops shots only: a blade, a beam's
    /// marked lane or a boss's ground still have to be answered. The party's own shots pass, and so does everyone on foot. It runs before the shots do each frame, so
    /// one is stopped on the step that would have carried it in.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class ShelterDome:MonoBehaviour
    {
        public const float Radius=5,FlightRadius=7,Seconds=5;
        public static ShelterDome Latest{get;private set;}
        /// <summary>Shots it has stopped.</summary>
        public int Stopped{get;private set;}
        public float Reach{get;private set;}
        float age;bool flight;GroundRing ring;FlightEnergyArc arc;MeshRenderer rim;Mesh band;Material glow;
        static readonly List<Projectile> entering=new();
        /// <summary>D415 (nov24-whole s46/128 and 132, s48/229): drawn as D387 left it, a clear sphere of its reach with a pale rim, it was still a circle 880 to 1200 px across, its
        /// limb standing over the party and the skill row and its glass laying amber over a third of the frame, with Idra herself off the screen. What a player needs from it is where
        /// its edge is and that it is up: so it is its ring on the floor at its reach and a low wall of light standing on that ring (<see cref="RimHeight"/>, a fifth of a hero), lit at
        /// its foot and gone by its top, that rises as it goes up and sinks as it ends. Nothing is drawn over what it covers. Its reach (5 m, 7 m in flight) and its five seconds are as
        /// they were; it stops shots that reach it from outside, as it did.</summary>
        public const float RimHeight=.38f,RiseSeconds=.25f,FallSeconds=.4f;
        /// <summary>The rim's colour at its foot: a pale amber held under white, so the bloom has nothing to spread; its alpha is how strongly it is laid over what is behind it.</summary>
        public static readonly Color RimColour=new(1f,.86f,.6f,.7f);
        /// <summary>How many spokes the rim is drawn on, and the ring's own strength while it stands (the party's cream, GroundRing.Friendly).</summary>
        const int Spokes=64;const float RingAlpha=.9f;
        /// <summary>How high the rim stands now (0 before it rises and after it has sunk), and how far from the middle it is drawn at the most: what a check reads.</summary>
        public float RimStands{get;private set;}
        public float DrawnRadius=>rim!=null?Mathf.Max(rim.bounds.extents.x,rim.bounds.extents.z):0;
        public static ShelterDome Plant(CombatActor owner,Vector3 at,bool flight)
        {
            var dome=Stand(flight?at:CombatCover.Ground(at+Vector3.up*2),flight);Latest=dome;
            HeroSkills.Sound(owner,dome.transform.position+Vector3.up,"forceField_000",.24f,.7f);CombatVfx.Burst(dome.transform.position+Vector3.up*.5f,new Color(1.5f,.9f,.35f),"shape");
            return dome;
        }
        /// <summary>Only what it draws, standing at a place (an editor still draws it with this and <see cref="Pose"/>): no sound, no burst, not the one a companion reads.</summary>
        public static ShelterDome Stand(Vector3 at,bool flight)
        {
            var go=new GameObject("Shelter",typeof(ShelterDome));go.transform.position=at;
            var dome=go.GetComponent<ShelterDome>();dome.flight=flight;dome.Reach=flight?FlightRadius:Radius;
            // Its edge on the floor (or the flight plane), stopping at a wall as a skill's ring does.
            var edge=new GameObject("Shelter edge");edge.transform.SetParent(go.transform,false);
            if(flight){dome.arc=FlightEnergyArc.Create("Shelter edge arc");dome.arc.transform.SetParent(edge.transform,false);}
            else{dome.ring=edge.AddComponent<GroundRing>();dome.ring.Initialize(dome.Reach+.5f);}
            // The rim: a band with no collider on the same edge, its foot on the ground under each spoke.
            var rimGo=new GameObject("Shelter rim",typeof(MeshFilter),typeof(MeshRenderer));rimGo.transform.SetParent(go.transform,false);
            dome.band=new Mesh{name="Shelter rim"};dome.band.MarkDynamic();rimGo.GetComponent<MeshFilter>().sharedMesh=dome.band;
            dome.glow=new Material(Resources.Load<Material>("Effects/flare_01")){name="Shelter rim"};dome.glow.SetTexture("_BaseMap",Texture2D.whiteTexture);
            dome.rim=rimGo.GetComponent<MeshRenderer>();dome.rim.sharedMaterial=dome.glow;dome.rim.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;dome.rim.receiveShadows=false;
            dome.Lay();dome.Pose(0);
            return dome;
        }
        Vector3[] foot;Color[] tone;Vector3[] shape;static readonly RaycastHit[] Floors=new RaycastHit[8];
        /// <summary>Where the rim's foot is on each spoke: at its reach, or at a wall before it; on the ground there, not on one plane at the middle's height.</summary>
        void Lay()
        {
            foot=new Vector3[Spokes];var centre=transform.position;var cover=centre+Vector3.up*(flight?0:.75f);
            for(int i=0;i<Spokes;i++)
            {
                float angle=i*Mathf.PI*2/Spokes;var way=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float reach=CombatCover.Sweep(cover,cover+way*Reach,.01f,out var wall)?Mathf.Max(0,wall.distance-.03f):Reach;
                var at=centre+way*reach;float y=centre.y;
                // The floor under it nearest the middle's height, within a step: a body standing on the edge isn't the floor.
                if(!flight)
                {
                    int count=Physics.RaycastNonAlloc(at+Vector3.up*1.2f,Vector3.down,Floors,2.4f,~0,QueryTriggerInteraction.Ignore);float off=float.PositiveInfinity;
                    for(int h=0;h<count;h++)
                    {
                        var hit=Floors[h];if(hit.collider is CharacterController||hit.collider.GetComponentInParent<Health>()!=null)continue;
                        if(Mathf.Abs(hit.point.y-centre.y)<off){off=Mathf.Abs(hit.point.y-centre.y);y=hit.point.y;}
                    }
                }
                foot[i]=new Vector3(way.x*reach,y-centre.y+(flight?-.1f:.02f),way.z*reach);
            }
            // Four rows on each spoke (foot, a third up, two thirds, top), each face drawn on both sides: the near side is seen from outside, the far side from within.
            shape=new Vector3[Spokes*4];tone=new Color[Spokes*4];var triangles=new int[Spokes*3*12];int k=0;
            for(int i=0;i<Spokes;i++)for(int row=0;row<3;row++)
            {
                int a=row*Spokes+i,b=row*Spokes+(i+1)%Spokes,c=a+Spokes,d=b+Spokes;
                triangles[k++]=a;triangles[k++]=c;triangles[k++]=b;triangles[k++]=b;triangles[k++]=c;triangles[k++]=d;
                triangles[k++]=a;triangles[k++]=b;triangles[k++]=c;triangles[k++]=b;triangles[k++]=d;triangles[k++]=c;
            }
            band.vertices=shape;band.colors=tone;band.triangles=triangles;
        }
        /// <summary>Draws it as it stands this long after it was planted: rising over its first quarter second, standing, sinking over its last <see cref="FallSeconds"/>.</summary>
        public void Pose(float since)
        {
            float rise=Mathf.Clamp01(since/RiseSeconds),fall=Mathf.Clamp01((Seconds-since)/FallSeconds),up=Mathf.Min(1-(1-rise)*(1-rise),fall);
            RimStands=RimHeight*up;
            for(int row=0;row<4;row++)
            {
                float t=row/3f;var c=RimColour;c.a*=(1-t)*(1-t)*Mathf.Lerp(.35f,1,up);
                for(int i=0;i<Spokes;i++){int v=row*Spokes+i;shape[v]=foot[i]+Vector3.up*(RimStands*t);tone[v]=c;}
            }
            band.vertices=shape;band.colors=tone;band.RecalculateBounds();
            var ringColour=GroundRing.Friendly;ringColour.a=RingAlpha*Mathf.Min(rise,fall);
            if(ring!=null)ring.Draw(Reach,ringColour);if(arc!=null)arc.Draw(Reach,180,ringColour,true);
        }
        /// <summary>Whether a point is under it.</summary>
        public bool Covers(Vector3 point)=>(point-transform.position).sqrMagnitude<=Reach*Reach;
        void Update()
        {
            if(GameTime.CombatHeld)return;
            age+=Time.deltaTime;if(age>=Seconds){Destroy(gameObject);return;}
            var centre=transform.position;float dt=Time.deltaTime,reach=Reach*Reach;
            entering.Clear();
            foreach(var shot in Projectile.Live)
            {
                if(shot==null||!shot.Threatens(true))continue;
                var from=shot.transform.position-centre;
                // Already under it: fired from in here, or in before it was planted. Those are somebody's to dodge.
                if(from.sqrMagnitude<=reach)continue;
                var step=shot.Velocity*dt;float along=step.magnitude;if(along<1e-5f)continue;
                var heading=step/along;float nearest=Mathf.Clamp(-Vector3.Dot(from,heading),0,along);
                if((from+heading*nearest).sqrMagnitude<=reach)entering.Add(shot);
            }
            foreach(var shot in entering){shot.Spend();Stopped++;}
            entering.Clear();
            Pose(age);
        }
        // Its ring and rim are its own children and go with it, the fight's end (a conversation, a pause) included.
        void OnDestroy()
        {
            if(Application.isPlaying){if(glow!=null)Destroy(glow);if(band!=null)Destroy(band);}
            else{if(glow!=null)DestroyImmediate(glow);if(band!=null)DestroyImmediate(band);}
            if(Latest==this)Latest=null;
        }
    }

    /// <summary>Rescue Flare: a light thrown to the other hero. Standing, it mends them a quarter of their Integrity; down, it raises them, from anywhere in thirty
    /// metres, where every other way to raise a hero means standing over them. With nobody to throw it to, it mends her a little instead. The one skill in the game
    /// that can raise: its Charge and its cooldown are the price.</summary>
    public sealed class RescueFlare:MonoBehaviour
    {
        /// <summary>What it mends a standing partner, what it raises a downed one to, what it mends her alone, how far it reaches and how long it takes to arrive.</summary>
        public const float Mend=.25f,Raise=.35f,Self=.15f,Reach=30,Flight=.5f;
        public static RescueFlare Latest{get;private set;}
        public bool Raised{get;private set;}
        public bool Landed{get;private set;}
        CombatActor owner,toward;Vector3 from;float age;Transform light;
        static readonly Color Orange=new(1.7f,.8f,.25f,.95f);
        public static RescueFlare Fire(CombatActor owner)
        {
            var go=new GameObject("Rescue flare",typeof(RescueFlare));var flare=go.GetComponent<RescueFlare>();flare.owner=owner;flare.toward=owner;Latest=flare;
            var party=PartyController.Current;
            // The partner who's down before the one who's hurt; out of reach, there's nobody to throw it to.
            if(party!=null)foreach(var member in party.members)
            {
                if(member==null||member==owner)continue;
                var gap=member.transform.position-owner.transform.position;gap.y=0;if(gap.magnitude>Reach)continue;
                flare.toward=member;break;
            }
            flare.from=owner.transform.position+Vector3.up*(owner.flight?.8f:1.6f);go.transform.position=flare.from;
            var core=new GameObject("Flare light",typeof(MeshFilter),typeof(MeshRenderer));core.transform.SetParent(go.transform,false);core.transform.localScale=Vector3.one*.35f;
            core.GetComponent<MeshFilter>().sharedMesh=Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            var view=core.GetComponent<MeshRenderer>();view.sharedMaterial=ActorFactory.Energy(Orange);view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            flare.light=core.transform;
            HeroSkills.Sound(owner,flare.from,"forceField_000",.22f,1.3f);CombatVfx.Burst(flare.from,new Color(1.6f,.8f,.25f),"shot");
            return flare;
        }
        void Update()
        {
            if(GameTime.CombatHeld)return;
            if(owner==null||toward==null){Destroy(gameObject);return;}
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/Flight);
            var to=toward.transform.position+Vector3.up*(toward.flight?.8f:1);
            // Up and over, so it's seen leaving her and arriving.
            transform.position=Vector3.Lerp(from,to,t)+Vector3.up*((toward.flight?0:2.2f)*4*t*(1-t));
            if(t<1)return;
            var body=toward.Health;
            if(toward==owner)body.Heal(body.maximum*Self);
            else if(!body.Alive){body.Heal(body.maximum*Raise);Raised=true;Debug.Log("FLARE_RAISED "+toward.character);}
            else body.Heal(body.maximum*Mend);
            Landed=true;
            CombatVfx.Burst(to,new Color(1.6f,.8f,.25f),"shape");SkillRing.Expand(toward.flight?toward.transform.position:CombatCover.Ground(toward.transform.position+Vector3.up),1.8f,GroundRing.Friendly,.5f,toward.flight);
            AudioManager.PlayAt("ui_confirm",toward.transform,to,.2f);
            Destroy(gameObject);
        }
        void OnDestroy(){if(Latest==this)Latest=null;}
    }
}
