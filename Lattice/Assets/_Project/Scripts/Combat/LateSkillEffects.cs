using System;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>What the partner needs to know to spend Charge on a skill learned at Sync 16 to 24 as a player would (S3). PartnerBrain asks; the rules live with the skills.</summary>
    public static class LateSkills
    {
        /// <summary>Under this share of their Integrity a hero is worth a Mend Lattice.</summary>
        public const float MendBelow=.65f;
        public static bool Is(SkillKind kind)=>kind>=SkillKind.BulwarkRush&&kind<=SkillKind.LongExposure;
        static Vector3 Planar(Vector3 v){v.y=0;return v;}
        /// <summary>How near the target has to be for the skill to be worth a try. A mend has no target at all.</summary>
        public static float Reach(CombatActor actor,SkillKind kind)=>kind switch
        {
            SkillKind.BulwarkRush=>actor.flight?HeroSkills.RushFlightLength+1:HeroSkills.RushLength+1,
            SkillKind.CinderBrand=>actor.flight?HeroSkills.BrandFlightReach-1:2.4f,
            SkillKind.Severance=>actor.flight?HeroSkills.SeverFlightReach-1.5f:2.6f,
            SkillKind.MendLattice=>float.PositiveInfinity,
            SkillKind.PrismRelay=>PrismChain.Reach-2,SkillKind.LongExposure=>HeroSkills.ExposureReach-2,
            _=>IdraSkills.Reach(actor,kind),
        };
        /// <summary>True when these rules settle it, with the answer in <paramref name="worth"/>: a mend is decided here outright, and the others are refused here or left to
        /// the partner's general rules (a target in front of it, no wall on the line).</summary>
        public static bool Decides(CombatActor actor,SkillKind kind,out bool worth)
        {
            worth=false;if(!Is(kind))return IdraSkills.Decides(actor,kind,out worth);
            // Nobody mends at full Integrity: it's the costliest thing she has, on a long cooldown.
            if(kind==SkillKind.MendLattice){worth=NeedsMending(actor);return true;}
            var target=actor.target;if(target==null)return true;
            float gap=Planar(target.transform.position-actor.transform.position).magnitude;
            switch(kind)
            {
                case SkillKind.BulwarkRush:
                    // A gap-closer, like the slam and the hook: not from on top of the target and not with the hero nearly down. In flight a hull goes the whole length with
                    // nothing steering it, so not into a wall either.
                    if(gap<3||actor.Health.integrity<actor.Health.maximum*.3f)return true;
                    if(!actor.flight)return false;
                    var hull=actor.transform.position+Vector3.up*.8f;
                    return CombatCover.Sweep(hull,hull+Planar(target.transform.position-actor.transform.position).normalized*HeroSkills.RushFlightLength,HeroCollision.HullRadius(actor.character),out _);
                // On something that will live to be hit again: a brand on a body a swing or two from the floor buys nothing, and neither does a second brand on a first.
                case SkillKind.CinderBrand:return target.Branded||target.integrity<actor.damage*4;
                // The finisher waits for what it's for: a Broken body, or one it would put down.
                case SkillKind.Severance:return !(target.Broken||target.integrity<=actor.damage*actor.damageScale*HeroSkills.SeverCut);
                // One body is half a lance for more Charge: it wants somewhere to go.
                case SkillKind.PrismRelay:return Crowd(actor,target)<2;
                // Two and a half seconds standing still: not at arm's length, and never where something is about to land.
                case SkillKind.LongExposure:return gap<4||InDanger(actor);
            }
            return false;
        }
        public static bool NeedsMending(CombatActor actor)
        {
            if(Hurt(actor))return true;
            var party=PartyController.Current;if(party==null)return false;
            float reach=actor.flight?MendWake.Reach:MendField.Radius*2-1;
            foreach(var member in party.members)
                if(member!=null&&member!=actor&&Hurt(member)&&Planar(member.transform.position-actor.transform.position).magnitude<=reach)return true;
            return false;
        }
        static bool Hurt(CombatActor hero)=>hero.Health.Alive&&hero.Health.integrity<hero.Health.maximum*MendBelow;
        static int Crowd(CombatActor actor,Health target)
        {
            int count=0;var at=target.transform.position;
            foreach(var body in Health.All)
                if(body!=null&&body.friendly!=actor.Health.friendly&&body.Targetable&&Planar(body.transform.position-at).magnitude<=PrismChain.Hop)count++;
            return count;
        }
        /// <summary>Whether standing still here is asking for it: a danger somebody has marked lies over the hero. A lantern's ward is a marking too, and no danger.</summary>
        public static bool InDanger(CombatActor actor)
        {
            var at=actor.transform.position;
            foreach(var marking in Telegraph.Live)
                if(marking!=null&&!marking.Released&&(marking.owner is BossController||marking.owner is EnemyBrain||marking.owner is DeathBlast)&&marking.Contains(at))return true;
            return false;
        }
    }

    /// <summary>Cinder Brand's mark on a body: a toothed ring of embers turning round it for as long as the brand holds (Health.Brand). The +20% is Health's; this is what shows it.</summary>
    public sealed class CinderBrand:MonoBehaviour
    {
        const int Teeth=28;
        Health body;LineRenderer ring;Material material;float nextSpark;
        /// <summary>Brands what was struck, and its carrier with it: a brand on a collar link is a brand on the animal.</summary>
        public static void Mark(Health struck,float seconds)
        {
            if(struck==null||!struck.Alive)return;
            struck.Brand(seconds);
            var carrier=struck.reactionOwner!=null?struck.reactionOwner:struck;
            Debug.Log("BRANDED "+carrier.id);
            // Still burning from the last one: its time is the body's own, which has just been pushed on.
            if(carrier.GetComponentInChildren<CinderBrand>()!=null)return;
            var go=new GameObject("Cinder brand",typeof(CinderBrand));go.transform.SetParent(carrier.transform,false);
            go.GetComponent<CinderBrand>().Build(carrier);
        }
        void Build(Health carrier)
        {
            body=carrier;var shape=carrier.GetComponent<CharacterController>();bool flight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            float radius=(shape!=null?shape.radius:.5f)+.35f;
            transform.localPosition=Vector3.up*(flight?0:shape!=null?Mathf.Min(shape.height*.6f,1.6f):1);
            ring=gameObject.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=Teeth;ring.startWidth=ring.endWidth=.09f;
            material=new Material(Resources.Load<Material>("Effects/flare_01"));material.SetTexture("_BaseMap",Texture2D.whiteTexture);ring.sharedMaterial=material;
            ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            // A seal's teeth, not a marking's smooth edge: it has to read as something stamped on the body.
            for(int i=0;i<Teeth;i++){float turn=i*Mathf.PI*2/Teeth,reach=radius*(i%2==0?1:.84f);ring.SetPosition(i,new Vector3(Mathf.Cos(turn)*reach,0,Mathf.Sin(turn)*reach));}
            ring.startColor=ring.endColor=new Color(1.7f,.95f,.3f,.9f);
        }
        void Update()
        {
            if(body==null||!body.Alive||!body.Branded){Destroy(gameObject);return;}
            if(GameTime.CombatHeld)return;
            transform.localRotation=Quaternion.Euler(10,Time.time*110,0);
            ring.startColor=ring.endColor=new Color(1.7f,.95f,.3f,Mathf.Clamp01((body.BrandUntil-Time.time)/.6f)*(.72f+.28f*Mathf.Sin(Time.time*9)));
            if(Time.time<nextSpark)return;
            nextSpark=Time.time+.55f;CombatVfx.Burst(transform.position,new Color(1.6f,.7f,.2f),"spark");
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }

    /// <summary>A flight skill's forward arc, sweeping out to its reach and fading: SkillRing draws only a whole circle.</summary>
    public sealed class SkillArc:MonoBehaviour
    {
        FlightEnergyArc arc;float reach,half,life,age;Color color;
        public static void Flash(Vector3 origin,Vector3 aim,float reach,float halfAngle,Color color,float life)
        {
            var go=new GameObject("Skill arc",typeof(SkillArc));go.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(aim,Vector3.up));
            var flash=go.GetComponent<SkillArc>();flash.reach=reach;flash.half=halfAngle;flash.life=life;flash.color=color;
            flash.arc=FlightEnergyArc.Create("Skill arc edge");flash.arc.transform.SetParent(go.transform,false);flash.arc.Draw(0,halfAngle,Color.clear,true);
        }
        void Update()
        {
            if(GameTime.CombatHeld)return;
            age+=Time.deltaTime;var tint=color;tint.a=Mathf.Clamp01((1-age/life)*1.6f);
            arc.Draw(reach*Mathf.Clamp01(age/(life*.6f)),half,tint,true);
            if(age>=life)Destroy(gameObject);
        }
    }

    /// <summary>Mend Lattice on foot: a field laid on the floor that gives every hero standing in it 6% of their Integrity a second for five seconds, in ticks of half a
    /// second. The game's first mend that isn't an item. It mends the living only: Health.Heal on a hero who's down is a revive, and this is not one.</summary>
    public sealed class MendField:MonoBehaviour
    {
        public const float Radius=4.5f,Seconds=5,Tick=.5f,PerSecond=.06f;
        /// <summary>How many times it gives, and so the most any one cast gives any one hero: ten ticks of 3%.</summary>
        public const int Ticks=10;
        public static readonly Color Mint=new(.45f,1.5f,1.1f);
        public static MendField Latest{get;private set;}
        /// <summary>What it has given back so far, both heroes together.</summary>
        public float Mended{get;private set;}
        CombatActor owner;GroundRing ring;Transform lattice;Material material;float age;int ticks;
        public static MendField Place(CombatActor owner,Vector3 at)
        {
            var go=new GameObject("Mend lattice",typeof(MendField));go.transform.position=CombatCover.Ground(at+Vector3.up*2);
            var field=go.GetComponent<MendField>();field.owner=owner;Latest=field;
            field.ring=go.AddComponent<GroundRing>();field.ring.Initialize(Radius+.5f);field.ring.Draw(Radius,Tint(.85f));
            // The lattice itself: two hexagons turning against each other inside the ring.
            field.material=new Material(Resources.Load<Material>("Effects/flare_01"));field.material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            field.lattice=new GameObject("Lattice").transform;field.lattice.SetParent(go.transform,false);field.lattice.localPosition=Vector3.up*.16f;
            for(int k=0;k<2;k++)
            {
                var line=new GameObject("Hexagon",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(field.lattice,false);
                line.useWorldSpace=false;line.loop=true;line.positionCount=6;line.startWidth=line.endWidth=.07f;line.sharedMaterial=field.material;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.startColor=line.endColor=Tint(.55f);
                float reach=Radius*(k==0?.66f:.38f);
                for(int i=0;i<6;i++){float turn=(i+k*.5f)*Mathf.PI/3;line.SetPosition(i,new Vector3(Mathf.Cos(turn)*reach,0,Mathf.Sin(turn)*reach));}
            }
            HeroSkills.Sound(owner,go.transform.position+Vector3.up,"forceField_000",.22f,1.25f);
            CombatVfx.Burst(go.transform.position+Vector3.up*.3f,Mint,"shape");
            return field;
        }
        static Color Tint(float alpha)=>new(Mint.r,Mint.g,Mint.b,alpha);
        void Update()
        {
            if(GameTime.CombatHeld)return;
            age+=Time.deltaTime;
            while(ticks<Ticks&&age>=Tick*(ticks+1)){ticks++;Mend();}
            float fade=Mathf.Clamp01((Seconds+.25f-age)/.25f);
            ring.Draw(Radius,Tint(.85f*fade));
            lattice.localRotation=Quaternion.Euler(0,age*24,0);
            foreach(var line in lattice.GetComponentsInChildren<LineRenderer>())line.startColor=line.endColor=Tint(.55f*fade);
            if(age>=Seconds+.25f)Destroy(gameObject);
        }
        void Mend()
        {
            var party=PartyController.Current;if(party==null)return;bool any=false;
            foreach(var member in party.members)
            {
                if(member==null||!member.Health.Alive)continue;
                var gap=member.transform.position-transform.position;if(Mathf.Abs(gap.y)>3)continue;
                gap.y=0;if(gap.magnitude>Radius+.3f)continue;
                float before=member.Health.integrity;member.Health.Heal(member.Health.maximum*PerSecond*Tick);
                float got=member.Health.integrity-before;if(got<=0)continue;
                Mended+=got;any=true;CombatVfx.Burst(member.transform.position+Vector3.up,Mint,"spark");
            }
            // Heard as the gel is heard, quietly and every other tick.
            if(any&&ticks%2==1)AudioManager.PlayAt("ui_confirm",owner!=null?owner.transform:transform,transform.position+Vector3.up,.1f,AudioBus.SFX,160,.04f,1.2f);
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(Latest==this)Latest=null;}
    }

    /// <summary>Mend Lattice in flight: for three seconds her ship lays a wake behind it, each stretch lasting five, and a hero flying in it is mended as the field mends
    /// (the same ticks, the same most for one cast, the living only). It rewards flying her line, as the field rewards holding its ground.</summary>
    public sealed class MendWake:MonoBehaviour
    {
        public const float Lay=3,Width=3,Spacing=1.5f,Reach=12;
        const int Most=24;
        public static MendWake Latest{get;private set;}
        public float Mended{get;private set;}
        CombatActor owner;LineRenderer glow,core;Material material;float age,nextTick=MendField.Tick;
        readonly List<Vector3> nodes=new();readonly List<float> born=new();readonly Dictionary<CombatActor,int> given=new();
        public static MendWake Begin(CombatActor owner)
        {
            var go=new GameObject("Mend wake",typeof(MendWake));go.transform.position=owner.transform.position;
            var wake=go.GetComponent<MendWake>();wake.owner=owner;Latest=wake;
            wake.material=new Material(Resources.Load<Material>("Effects/flare_01"));wake.material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            wake.glow=Line(go.transform,wake.material,1.7f,new Color(MendField.Mint.r,MendField.Mint.g,MendField.Mint.b,.26f),0);
            wake.core=Line(go.transform,wake.material,.14f,new Color(MendField.Mint.r,MendField.Mint.g,MendField.Mint.b,.9f),1);
            HeroSkills.Sound(owner,go.transform.position+Vector3.up,"forceField_000",.22f,1.25f);
            CombatVfx.Burst(go.transform.position+Vector3.up*.8f,MendField.Mint,"shape");
            return wake;
        }
        static LineRenderer Line(Transform parent,Material material,float width,Color color,int order)
        {
            var line=new GameObject(order==0?"Wake glow":"Wake core",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(parent,false);
            line.useWorldSpace=true;line.positionCount=0;line.numCapVertices=4;line.numCornerVertices=3;line.startWidth=line.endWidth=width;line.sharedMaterial=material;
            line.startColor=line.endColor=color;line.sortingOrder=order;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;
        }
        static Vector3 Planar(Vector3 v){v.y=0;return v;}
        void Update()
        {
            if(GameTime.CombatHeld)return;
            age+=Time.deltaTime;
            bool laying=owner!=null&&owner.Health.Alive&&age<Lay&&nodes.Count<Most;
            if(laying)
            {
                var at=owner.transform.position+Vector3.up*.5f;
                if(nodes.Count==0||Planar(at-nodes[nodes.Count-1]).magnitude>=Spacing){nodes.Add(at);born.Add(age);}
            }
            while(nodes.Count>0&&age-born[0]>=MendField.Seconds){nodes.RemoveAt(0);born.RemoveAt(0);}
            if(age>=nextTick){nextTick+=MendField.Tick;Mend();}
            Draw(laying);
            if(!laying&&nodes.Count==0)Destroy(gameObject);
        }
        void Mend()
        {
            var party=PartyController.Current;if(party==null)return;bool any=false;
            foreach(var member in party.members)
            {
                if(member==null||!member.Health.Alive||!In(member.transform.position))continue;
                given.TryGetValue(member,out int had);if(had>=MendField.Ticks)continue;
                float before=member.Health.integrity;member.Health.Heal(member.Health.maximum*MendField.PerSecond*MendField.Tick);
                float got=member.Health.integrity-before;if(got<=0)continue;
                given[member]=had+1;Mended+=got;any=true;CombatVfx.Burst(member.transform.position+Vector3.up*.8f,MendField.Mint,"spark");
            }
            if(any)AudioManager.PlayAt("ui_confirm",owner!=null?owner.transform:transform,transform.position+Vector3.up,.1f,AudioBus.SFX,160,.9f,1.2f);
        }
        /// <summary>Whether a ship is in the wake: within its width of any stretch still lying there.</summary>
        public bool In(Vector3 at)
        {
            foreach(var node in nodes)if(Planar(at-node).magnitude<=Width)return true;
            return false;
        }
        void Draw(bool laying)
        {
            int count=nodes.Count+(laying||nodes.Count==1?1:0);
            if(glow.positionCount!=count)glow.positionCount=core.positionCount=count;
            for(int i=0;i<nodes.Count;i++){glow.SetPosition(i,nodes[i]);core.SetPosition(i,nodes[i]);}
            if(count<=nodes.Count)return;
            // Its head is the ship laying it; one stretch alone (she's hovering) is drawn as a short one, since a line of one point is nothing.
            var head=laying?owner.transform.position+Vector3.up*.5f:nodes[0]+Vector3.forward*.6f;
            if(nodes.Count==1&&Planar(head-nodes[0]).magnitude<.6f)head=nodes[0]+Vector3.forward*.6f;
            glow.SetPosition(count-1,head);core.SetPosition(count-1,head);
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(Latest==this)Latest=null;}
    }

    /// <summary>Prism Relay: a beam that finds a first body along her aim and then jumps to the nearest body it hasn't struck, up to five in all, a beat apart, each for the
    /// same hit. Walls stop a jump as they stop a shot. With nothing to start on it leaves along her aim and is spent, as a lance that missed is.</summary>
    public sealed class PrismChain:MonoBehaviour
    {
        public const int Most=5;
        public const float Reach=14,Hop=7.5f,Beat=.07f,Linger=.22f;
        static readonly Color Cyan=new(.3f,1,1.6f);
        public static PrismChain Latest{get;private set;}
        /// <summary>Bodies it has struck so far.</summary>
        public int Struck=>visited.Count;
        CombatActor owner;Func<DamagePacket> packet;Vector3 direction,last;bool flight,done;float age,doneAt;int hops;
        readonly List<Health> visited=new();LineRenderer glow,core;Material material;
        public static PrismChain Fire(CombatActor owner,Vector3 origin,Vector3 direction,bool flight,Func<DamagePacket> packet)
        {
            var go=new GameObject("Prism relay",typeof(PrismChain));go.transform.position=origin;
            var chain=go.GetComponent<PrismChain>();chain.owner=owner;chain.packet=packet;chain.direction=direction.normalized;chain.flight=flight;chain.last=origin;Latest=chain;
            chain.material=new Material(Resources.Load<Material>("Effects/flare_01"));chain.material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            chain.glow=Line(go.transform,chain.material,.22f,0);chain.core=Line(go.transform,chain.material,.07f,1);chain.Tint(1);chain.Extend(origin);
            HeroSkills.Sound(owner,origin,"laserSmall_000",.2f,1.2f);
            return chain;
        }
        static LineRenderer Line(Transform parent,Material material,float width,int order)
        {
            var line=new GameObject(order==0?"Relay glow":"Relay core",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(parent,false);
            line.useWorldSpace=true;line.positionCount=0;line.numCapVertices=3;line.numCornerVertices=2;line.startWidth=line.endWidth=width;line.sharedMaterial=material;
            line.sortingOrder=order;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;
        }
        static Vector3 Planar(Vector3 v){v.y=0;return v;}
        Vector3 Point(Health body)=>body.transform.position+Vector3.up*(flight?0:.9f);
        void Extend(Vector3 point){glow.positionCount=core.positionCount=glow.positionCount+1;glow.SetPosition(glow.positionCount-1,point);core.SetPosition(core.positionCount-1,point);}
        void Tint(float alpha){glow.startColor=glow.endColor=new Color(.1f,.8f,1.1f,.5f*alpha);core.startColor=core.endColor=new Color(.7f,1.7f,2.2f,alpha);}
        void Finish(){done=true;doneAt=age;}
        void Update()
        {
            if(GameTime.CombatHeld)return;
            if(owner==null){Destroy(gameObject);return;}
            age+=Time.deltaTime;
            while(!done&&age>=hops*Beat)
            {
                var next=hops==0?First():Next();
                if(next==null)
                {
                    if(hops==0)Extend(last+direction*(CombatCover.Sweep(last,last+direction*Reach,.1f,out var wall)?wall.distance:Reach));
                    Finish();break;
                }
                var at=Point(next);Extend(at);visited.Add(next);last=at;hops++;
                next.Receive(packet());
                CombatVfx.Burst(at,Cyan,"hit");HeroSkills.Sound(owner,at,"laserSmall_000",.14f,1.3f+hops*.12f);
                if(hops>=Most)Finish();
            }
            if(!done)return;
            float gone=(age-doneAt)/Linger;Tint(1-gone);
            if(gone>=1)Destroy(gameObject);
        }
        bool Hostile(Health body)=>body!=null&&body.friendly!=owner.Health.friendly&&body.Targetable&&!visited.Contains(body);
        /// <summary>What she's aiming at if it's in front of her, in reach and in sight; else the nearest body close to her line.</summary>
        Health First()
        {
            var target=owner.target;
            if(Hostile(target))
            {
                var to=Planar(Point(target)-last);
                if(to.magnitude<=Reach&&(to.magnitude<1||Vector3.Angle(direction,to)<=45)&&CombatCover.Clear(last,Point(target)))return target;
            }
            Health best=null;float nearest=Reach;
            foreach(var body in Health.All)
            {
                if(!Hostile(body))continue;
                var to=Planar(Point(body)-last);float far=to.magnitude;
                if(far>=nearest||far>1&&Vector3.Angle(direction,to)>20||!CombatCover.Clear(last,Point(body)))continue;
                best=body;nearest=far;
            }
            return best;
        }
        Health Next()
        {
            Health best=null;float nearest=Hop;
            foreach(var body in Health.All)
            {
                if(!Hostile(body))continue;
                float far=Planar(Point(body)-last).magnitude;
                if(far>=nearest||!CombatCover.Clear(last,Point(body)))continue;
                best=body;nearest=far;
            }
            return best;
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(Latest==this)Latest=null;}
    }
}
