using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>The six skills learned at Sync 16, 20 and 24 (S3), which keep the Skills page offering something through the middle of the game. Each does a job the
    /// first sixteen don't:
    /// <list type="bullet">
    /// <item>Taren: Bulwark Rush (an armoured rush that shrugs off what lands on the way and breaks what it meets), Cinder Brand (a strike that marks a body to take 20%
    /// more from everyone for eight seconds), Severance (a slow, planted finisher: half again on a Broken body, and half its Charge back for a kill).</item>
    /// <item>Sela: Mend Lattice (a field that mends both heroes, the first mend that isn't an item, and no revive), Prism Relay (a beam that jumps between up to five
    /// bodies), Long Exposure (a 2.5 s channel, rooted, of piercing ticks along her aim, which a roll lets go of).</item>
    /// </list>
    /// Each has a flight form. A hero also carries their EXTRA move (<see cref="ExtraMove"/>): it comes with this component, so nothing that builds a hero has to know.</summary>
    [RequireComponent(typeof(ExtraMove))]
    public sealed partial class HeroSkills
    {
        /// <summary>Bulwark Rush: how far it carries on foot and in flight, how long the run takes after its brace, what it lands for in Output, and its Break and reach.</summary>
        public const float RushLength=8,RushFlightLength=10,RushBrace=.12f,RushSeconds=.3f,RushImpact=1.8f,RushBreak=130,RushRadius=2.6f;
        /// <summary>Cinder Brand: its strike in Output, how long the brand holds (Health.BrandTaken is what it's worth), its Break and its reach on foot and in flight.</summary>
        public const float BrandStrike=1.5f,BrandSeconds=8,BrandBreak=35,BrandRadius=2,BrandFlightReach=6;
        /// <summary>Severance: its cut in Output, what a Broken body takes on top of the game's own bonus for one, the share of its Charge a kill gives back, its wind-ups
        /// on foot and in flight, its Break and its reach.</summary>
        public const float SeverCut=3.5f,SeverBroken=1.5f,SeverRefund=.5f,SeverWindUp=.68f,SeverFlightWindUp=.5f,SeverBreak=60,SeverRadius=2.3f,SeverFlightReach=7;
        /// <summary>Prism Relay: each body's hit in Output, and its Break.</summary>
        public const float RelayEach=1.2f,RelayBreak=16;
        /// <summary>Long Exposure: how long she holds it, how often it ticks and for what, how far it reaches, how fast its line can be turned (degrees a second), and a tick's Break.</summary>
        public const float ExposureSeconds=2.5f,ExposureTick=.25f,ExposureEach=.5f,ExposureReach=14,ExposureTurn=50,ExposureBreak=8;
        public const int ExposureTicks=10;
        static readonly Color Ember=new(1.6f,.75f,.2f);

        void GroundLate(SkillKind kind,float power,Vector3 aim)
        {
            switch(kind)
            {
                case SkillKind.BulwarkRush:
                    actor.Perform("Dash",.62f,.56f);StartCoroutine(Rush(power,aim));
                    break;
                case SkillKind.CinderBrand:
                    actor.Perform("Attack3",.5f,.42f);Sound("forceField_000",.14f,.6f);
                    After(.26f,()=>Brand(power,aim,false));
                    break;
                case SkillKind.Severance:
                {
                    // Planted for the wind-up: the ring that gathers on the floor is where the cut lands.
                    actor.Perform("Attack3",1.2f,1.02f);actor.Root(SeverWindUp);float paid=Paid(kind);
                    SkillRing.Expand(CombatCover.Ground(Ahead(aim,1.5f)+Vector3.up*.6f),SeverRadius,GroundRing.Friendly,SeverWindUp,false,true,SeverRadius*1.8f);
                    Sound("forceField_000",.2f,.5f);
                    After(SeverWindUp,()=>Sever(power,aim,false,paid));
                    break;
                }
                case SkillKind.MendLattice:
                    actor.Perform("Buff",.55f,.46f);Sound("forceField_000",.14f,1.5f);
                    After(.3f,()=>MendField.Place(actor,MendPoint()));
                    break;
                case SkillKind.PrismRelay:
                    actor.Perform("Shoot",.5f,.42f);
                    After(.22f,()=>PrismChain.Fire(actor,Hand(aim),aim,false,()=>Relay(power)));
                    break;
                case SkillKind.LongExposure:
                    actor.Perform("Shoot",ExposureSeconds+.3f,ExposureSeconds+.2f);StartCoroutine(Exposure(power,aim,false));
                    break;
                default:Idra(kind,power,aim,false);break;
            }
        }

        void FlightLate(SkillKind kind,float power,Vector3 aim)
        {
            var nose=transform.position+Vector3.up*.8f;
            switch(kind)
            {
                case SkillKind.BulwarkRush:
                    // A ramming burn through a line: the hull goes its whole length and strikes whatever it passes, once each.
                    actor.Perform("Dash",.45f,.3f);actor.armoredUntil=GameTime.Now+.5f;StartCoroutine(Through(RushFlightLength+4,.3f));actor.motor.Dash(aim,RushFlightLength);
                    GetComponent<FlightDashContact>().Begin(Hit(Melee(RushImpact,power),DamageType.Kinetic,RushBreak,HitWeight.Heavy));
                    CombatAudio.Launch(actor);Sound("forceField_000",.2f,.8f);
                    break;
                case SkillKind.CinderBrand:
                    actor.Perform("Cleave",.45f,.24f);Brand(power,aim,true);
                    break;
                case SkillKind.Severance:
                {
                    // A ship isn't planted: the cut leaves along wherever the nose has come round to.
                    actor.Perform("Cleave",.95f,.7f);float paid=Paid(kind);
                    CombatVfx.Burst(nose+aim,GroundRing.Friendly,"shot");Sound("forceField_000",.2f,.5f);
                    After(SeverFlightWindUp,()=>Sever(power,actor.motor.Facing,true,paid));
                    break;
                }
                case SkillKind.MendLattice:
                    actor.Perform("Buff",.4f,.16f);MendWake.Begin(actor);
                    break;
                case SkillKind.PrismRelay:
                    actor.Perform("Shoot",.4f,.16f);PrismChain.Fire(actor,Muzzle(aim),aim,true,()=>Relay(power));
                    break;
                case SkillKind.LongExposure:
                    actor.Perform("Shoot",ExposureSeconds+.3f,ExposureSeconds+.2f);StartCoroutine(Exposure(power,aim,true));
                    break;
                default:Idra(kind,power,aim,true);break;
            }
        }

        // ---------- shared pieces ----------
        bool Lead=>PartyController.Current==null||PartyController.Current.Active==actor;
        /// <summary>A point ahead of the hero for a strike to centre on, never past a wall he stands at: centred inside one, the strike found what stood behind it.</summary>
        Vector3 Ahead(Vector3 aim,float distance)=>Body+aim*(CombatCover.Sweep(Body,Body+aim*distance,.2f,out var wall)?Mathf.Max(0,wall.distance-.2f):distance);
        /// <summary>Where a ship's beam leaves it: a metre off the nose, or short of the wall the nose is against.</summary>
        Vector3 Muzzle(Vector3 aim){var nose=transform.position+Vector3.up*.8f;return nose+aim*(CombatCover.Sweep(nose,nose+aim,.12f,out var wall)?Mathf.Max(0,wall.distance-.05f):1);}
        bool Dropped(int sequence)=>actor.AttackSequence!=sequence||!actor.Health.Alive||actor.Recovering;
        /// <summary>What this cast cost: asked while it's being cast, before the use that might raise its mastery is counted.</summary>
        float Paid(SkillKind kind)
        {
            foreach(var skill in SkillBook.All(actor.character))if(skill.kind==kind)return actor.CostOf(skill);
            return 0;
        }
        readonly List<Hurtbox> reached=new();readonly List<Health> swept=new();readonly HashSet<Health> pierced=new();
        static readonly Collider[] Parts=new Collider[96];
        /// <summary>Who a strike reaches right now: every enemy with a part in the sphere and no wall between, by the part found first. A HitVolume does the same a frame
        /// later; a skill that answers to who it struck (a brand, a refund for the kill) needs them now.</summary>
        List<Hurtbox> Reached(Vector3 centre,float radius)
        {
            reached.Clear();int count=Physics.OverlapSphereNonAlloc(centre,radius,Parts,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                var part=Parts[i];
                if(!part.TryGetComponent<Hurtbox>(out var box)||box.owner==null||box.owner.friendly==actor.Health.friendly||!box.owner.Alive)continue;
                bool known=false;foreach(var other in reached)if(other.owner==box.owner){known=true;break;}
                if(known)continue;
                var nearest=part is MeshCollider mesh&&!mesh.convex?part.bounds.ClosestPoint(centre):part.ClosestPoint(centre);
                if(CombatCover.Clear(centre,nearest))reached.Add(box);
            }
            return reached;
        }
        /// <summary>Who a flight skill's forward arc reaches: bodies on the plane, inside its reach and its angle, with no wall between, as FlightWave finds them.</summary>
        List<Health> Swept(Vector3 origin,Vector3 aim,float reach,float halfAngle)
        {
            swept.Clear();
            foreach(var health in Health.All)
            {
                if(health==null||!health.Alive||health.friendly==actor.Health.friendly)continue;
                var shape=health.GetComponent<CapsuleCollider>();
                var point=shape!=null?shape.ClosestPoint(origin):health.transform.position+Vector3.up*.75f;
                var offset=point-origin;if(Mathf.Abs(offset.y)>1.2f)continue;
                offset.y=0;if(offset.sqrMagnitude>reach*reach)continue;
                var middle=Planar(health.transform.position-origin);
                if(offset.sqrMagnitude>1&&Vector3.Angle(aim,middle)>halfAngle||!CombatCover.Clear(origin,point))continue;
                swept.Add(health);
            }
            return swept;
        }
        /// <summary>A beam's tick: everything on the line takes it, once.</summary>
        void Pierce(Vector3 origin,Vector3 along,float length,DamagePacket packet)
        {
            pierced.Clear();
            foreach(var contact in Physics.SphereCastAll(origin,.22f,along,length,~0,QueryTriggerInteraction.Collide))
            {
                if(!contact.collider.TryGetComponent<Hurtbox>(out var box)||box.owner==null||!box.owner.Alive||box.owner.friendly==actor.Health.friendly||!pierced.Add(box.owner))continue;
                box.Hit(packet);
            }
        }

        // ---------- Bulwark Rush ----------
        IEnumerator Rush(float power,Vector3 aim)
        {
            int sequence=actor.AttackSequence;var ground=GetComponent<GroundMotor>();
            // Braced from the first frame: what lands on the way lands as on a guard, costs no Thrust and doesn't stop him.
            actor.armoredUntil=GameTime.Now+RushBrace+RushSeconds+.25f;
            Sound("forceField_000",.2f,.8f);
            for(float t=0;t<RushBrace;t+=actor.ActionDelta){if(Dropped(sequence)){actor.armoredUntil=0;yield break;}yield return null;}
            float gone=0,puffed=0;bool met=false;
            while(gone<RushLength&&!met)
            {
                if(Dropped(sequence)){actor.armoredUntil=0;yield break;}
                float dt=actor.ActionDelta;
                if(dt>0)
                {
                    float step=Mathf.Min(RushLength-gone,RushLength/RushSeconds*dt);var from=transform.position;
                    ground.MoveAction(aim*step,aim);gone+=step;
                    float moved=Planar(transform.position-from).magnitude;
                    if((puffed+=moved)>=1.2f){puffed=0;CombatVfx.Burst(Body,GroundRing.Friendly,"lunge");}
                    // It ends on the first body it meets, or on whatever stops him.
                    met=Reached(Ahead(aim,.6f),1).Count>0||moved<step*.3f;
                }
                yield return null;
            }
            actor.armoredUntil=Mathf.Max(actor.armoredUntil,GameTime.Now+.15f);
            var packet=Hit(Melee(RushImpact,power),DamageType.Kinetic,RushBreak,HitWeight.Heavy);
            var front=Ahead(aim,.9f);
            foreach(var box in Reached(front,RushRadius))box.Hit(packet);
            SkillRing.Expand(CombatCover.Ground(front+Vector3.up*.6f),RushRadius,GroundRing.Friendly,.3f,false);CombatVfx.Burst(front,new Color(1,.6f,.2f),"hit");
            Sound("impactMetal_000",.26f,.7f);
            // It lands whether or not anyone is in front of it.
            Impact.Thud(actor,aim);
        }

        /// <summary>For a ramming burn the hull goes through what it could shove aside: small bodies don't stop it, they're struck as it passes. A boss, a husk, a serpent or a
        /// mine holds its ground (EnemyBrain.CanBeMoved, Heavy), and the hull stops against it as it stops against a wall. Whatever becomes of the dash, the hulls are solid
        /// to each other again in a moment.</summary>
        IEnumerator Through(float reach,float seconds)
        {
            var hull=GetComponent<CharacterController>();var passed=new List<Collider>();
            foreach(var health in Health.All)
            {
                if(health==null||health.friendly==actor.Health.friendly||(health.transform.position-transform.position).sqrMagnitude>reach*reach)continue;
                if(!health.TryGetComponent<EnemyBrain>(out var brain)||!brain.CanBeMoved||brain.Heavy||!health.TryGetComponent<CharacterController>(out var body))continue;
                Physics.IgnoreCollision(hull,body,true);passed.Add(body);
            }
            // On the clock the dash itself runs on: through somebody's Flash it takes four times as long, and the hulls mustn't go solid halfway.
            for(float t=0;t<seconds;t+=actor.MotorDelta)yield return null;
            foreach(var body in passed)if(body!=null)Physics.IgnoreCollision(hull,body,false);
        }

        // ---------- Cinder Brand ----------
        void Brand(float power,Vector3 aim,bool flight)
        {
            var packet=Hit(Melee(BrandStrike,power),DamageType.Plasma,BrandBreak,HitWeight.Medium);bool marked=false;
            if(flight)
            {
                var origin=transform.position+Vector3.up*.8f;
                // Only what the strike hurt is branded: a boss mid-shift, or anything else it can't touch, takes no mark.
                foreach(var body in Swept(origin,aim,BrandFlightReach,50))if(body.Receive(packet)>0&&body.Alive){CinderBrand.Mark(body,BrandSeconds);marked=true;}
                SkillArc.Flash(origin,aim,BrandFlightReach,50,Ember,.3f);
            }
            else
            {
                var centre=Ahead(aim,1.4f);
                foreach(var box in Reached(centre,BrandRadius))if(box.Hit(packet)>0&&box.owner.Alive){CinderBrand.Mark(box.owner,BrandSeconds);marked=true;}
                CombatVfx.Burst(centre,Ember,"lunge");
            }
            Sound("impactMetal_000",.22f,.8f);if(marked)Sound("explosionCrunch_000",.14f,1.6f);
        }

        // ---------- Severance ----------
        void Sever(float power,Vector3 aim,bool flight,float paid)
        {
            bool killed=false;
            // The game already gives a Broken body half again; this is the finisher's own half again on top, so a break is what it waits for.
            DamagePacket Cut(Health body)=>Hit(Melee(SeverCut*(body.Broken?SeverBroken:1),power),DamageType.Kinetic,SeverBreak,HitWeight.Finisher);
            if(flight)
            {
                var origin=transform.position+Vector3.up*.8f;
                foreach(var body in Swept(origin,aim,SeverFlightReach,40)){body.Receive(Cut(body));killed|=!body.Alive;}
                SkillArc.Flash(origin,aim,SeverFlightReach,40,GroundRing.Friendly,.35f);
            }
            else
            {
                var centre=Ahead(aim,1.5f);
                foreach(var box in Reached(centre,SeverRadius)){box.Hit(Cut(box.owner));killed|=!box.owner.Alive;}
                SkillRing.Expand(CombatCover.Ground(centre+Vector3.up*.6f),SeverRadius,GroundRing.Friendly,.3f,false);CombatVfx.Burst(centre,new Color(1,.85f,.5f),"lunge");
            }
            Sound("explosionCrunch_000",.26f,.6f);Impact.Thud(actor,aim);
            if(!killed||paid<=0)return;
            actor.charge=Mathf.Min(100,actor.charge+paid*SeverRefund);
            CombatVfx.Burst(Body+Vector3.up*.4f,new Color(.3f,.8f,1.6f),"shape");Debug.Log($"SEVERANCE_REFUND {actor.character} {paid*SeverRefund:0}");
        }

        // ---------- Mend Lattice ----------
        /// <summary>Where the field goes: between the two of them if her partner is near enough for it to cover both, and where she stands if not.</summary>
        Vector3 MendPoint()
        {
            var here=transform.position;var party=PartyController.Current;if(party==null)return here;
            foreach(var member in party.members)
            {
                if(member==null||member==actor||!member.Health.Alive)continue;
                var gap=Planar(member.transform.position-here);
                if(gap.magnitude<=MendField.Radius*2-1&&CombatCover.Clear(Body,member.transform.position+Vector3.up*.9f))return here+gap*.5f;
            }
            return here;
        }

        // ---------- Prism Relay ----------
        DamagePacket Relay(float power)=>CombatActor.MultiHit(Hit(Ranged(RelayEach,power),DamageType.Beam,RelayBreak));

        // ---------- Long Exposure ----------
        IEnumerator Exposure(float power,Vector3 aim,bool flight)
        {
            int sequence=actor.AttackSequence;actor.Root(ExposureSeconds+.2f);
            if(flight&&TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            var material=new Material(Resources.Load<Material>("Effects/flare_01"));material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            // Hers to take with her: its lines are drawn in the world, but if she goes (a zone change mid-channel) so does it.
            var beam=new GameObject("Long Exposure beam");beam.transform.SetParent(transform,false);var glow=BeamLine(beam,material,0);var core=BeamLine(beam,material,1);
            float elapsed=0,pulse=1;int ticks=0;var along=Planar(aim).sqrMagnitude>.01f?Planar(aim).normalized:transform.forward;
            Sound("forceField_000",.2f,1.6f);
            try
            {
                while(elapsed<ExposureSeconds)
                {
                    // Any new action lets go of it: a roll is the way out, and a stagger takes it away.
                    if(Dropped(sequence))yield break;
                    float dt=GameTime.CombatHeld?0:actor.ActionDelta;
                    if(dt>0)
                    {
                        elapsed+=dt;along=Turned(along,dt);
                        // After the motor has had its say this frame: in flight a touch of the stick would swing the nose, and the beam with it, all the way round at once.
                        if(flight){if(TryGetComponent<FlightMotor>(out var nose))nose.KeepFacing(along);}
                        else{transform.rotation=Quaternion.LookRotation(along);if(TryGetComponent<GroundMotor>(out var feet))feet.KeepFacing(along);}
                        var origin=flight?Muzzle(along):Hand(along);
                        float length=CombatCover.Sweep(origin,origin+along*ExposureReach,.1f,out var wall)?wall.distance:ExposureReach;
                        bool ticked=false;
                        while(ticks<ExposureTicks&&elapsed>=ExposureTick*(ticks+1)-.01f)
                        {
                            ticks++;ticked=true;
                            Pierce(origin,along,length,CombatActor.MultiHit(Hit(Ranged(ExposureEach,power),DamageType.Beam,ExposureBreak)));
                            Sound("laserSmall_000",.12f,1.3f+ticks*.03f);
                        }
                        // It swells on each tick and settles between them.
                        pulse=ticked?1:Mathf.MoveTowards(pulse,0,dt/ExposureTick);
                        glow.SetPosition(0,origin);glow.SetPosition(1,origin+along*length);core.SetPosition(0,origin);core.SetPosition(1,origin+along*length);
                        glow.startWidth=glow.endWidth=.34f+.22f*pulse;core.startWidth=core.endWidth=.1f+.06f*pulse;
                        glow.startColor=glow.endColor=new Color(.1f,.8f,1.1f,.4f+.2f*pulse);
                        // The partner doesn't finish a channel standing in a marking: she lets go and walks out, as she would have.
                        if(!Lead&&LateSkills.InDanger(actor)){actor.Perform("Idle",.05f,0);yield break;}
                    }
                    yield return null;
                }
            }
            finally{Destroy(beam);Destroy(material);if(actor.AttackSequence==sequence)actor.rootedUntil=0;}
        }
        static LineRenderer BeamLine(GameObject parent,Material material,int order)
        {
            var line=new GameObject(order==0?"Glow":"Core",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(parent.transform,false);
            line.positionCount=2;line.useWorldSpace=true;line.numCapVertices=3;line.sharedMaterial=material;line.sortingOrder=order;line.startWidth=line.endWidth=0;
            line.startColor=line.endColor=new Color(.7f,1.7f,2.2f,1);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;
        }
        /// <summary>Where the line is going: at her target while the stick rests or a lock holds it (and always for the partner), else where the stick points. Slowly, either
        /// way: it's a sweep, and a body that runs round her gets out of it.</summary>
        Vector3 Turned(Vector3 along,float dt)
        {
            var wanted=along;bool steering=Lead&&!actor.TargetLocked&&actor.MoveIntent.sqrMagnitude>.05f;
            if(steering)wanted=Planar(actor.MoveIntent).normalized;
            else if(actor.target!=null&&actor.target.Alive){var to=Planar(actor.target.transform.position-transform.position);if(to.sqrMagnitude>.01f)wanted=to.normalized;}
            return Vector3.RotateTowards(along,wanted,ExposureTurn*Mathf.Deg2Rad*dt,0);
        }
    }
}
