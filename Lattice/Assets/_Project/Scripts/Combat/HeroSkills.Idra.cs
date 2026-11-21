using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Idra Sorn's hands (S6a): the guard hero's plain attack and her eight skills, on foot and in flight. She hits slower and heavier than Taren, and nearly
    /// everything she has is about standing between something and somebody:
    /// <list type="bullet">
    /// <item>the hammer: a three-hit chain, each heavier than his in damage and Break and slower, the third putting a light body on the floor; in flight a short
    /// breaching cutter, an arc off the prow;</item>
    /// <item>Breach (a braced charge through a line), Tow Line (a harpoon that reels light things in and pulls her to heavy ones, wreckage included), Shelter (a dome
    /// that stops enemy shots), Slag Hammer (a planted overhead slam, the heaviest Break she has);</item>
    /// <item>Rescue Flare (mends her partner, and raises one who's down, from across the fight: the only skill that does), Counterweight (her next three guarded hits
    /// answer with a shockwave), Avalanche (three leaping slams), Hold the Line (ordinary enemies turn to her, and her Plating rises).</item>
    /// </list>
    /// Her guard's own answers are <see cref="BraceKit"/>'s; what it lets through and costs is her <see cref="HeroStyle"/>. Every hero carries the kit, as every
    /// hero carries the EXTRA move: on one whose style asks nothing of it, it does nothing.</summary>
    [RequireComponent(typeof(BraceKit))]
    public sealed partial class HeroSkills
    {
        /// <summary>The hammer's three hits in Output, their Break and how long each swing takes (Taren's cuts: 1, 1, 1.5; Break 20, 20, 30; .32, .36, .48 s).</summary>
        public static readonly float[] HammerHit={1.3f,1.5f,2.2f},HammerBreak={30,36,60},HammerSeconds={.5f,.56f,.72f};
        /// <summary>How far ahead the head lands and what it reaches, how far through the swing it lands, and how long the third leaves a light body slowed.</summary>
        public const float HammerReach=1.5f,HammerRadius=1.6f,HammerLastRadius=2.1f,HammerContact=.55f,HammerDown=1.2f;
        /// <summary>The cutter, her plain attack in flight: its hit in Output, its Break, its beat, and the arc ahead it sweeps.</summary>
        public const float CutterHit=.9f,CutterBreak=22,CutterSeconds=.4f,CutterReach=5.5f,CutterArc=45;
        public const float BreachLength=7,BreachFlightLength=9,BreachHit=2,BreachBreak=50,BreachRadius=1.4f;
        public const float TowHit=1.1f,TowBreak=40,TowLength=12,TowFlightLength=14;
        public const float SlagHit=2.8f,SlagBreak=110,SlagRadius=3,SlagWindUp=.45f,SlagFlightWindUp=.3f,SlagFlightReach=6;
        public static readonly float[] AvalancheHit={1.2f,1.2f,2.2f},AvalancheBreak={30,30,70},AvalancheRadius={2.6f,2.6f,3.4f};
        /// <summary>Avalanche: how far each leap carries and how long each of its three beats is.</summary>
        public const float AvalancheLeap=2.2f,AvalancheBeat=.4f;

        // ---------- the hammer ----------
        /// <summary>Her plain attack, from CombatActor.Attack, which has counted the chain's stage. A swing's damage doesn't wait on a rig's wrist as Taren's blade does:
        /// the head lands where she's facing, a set share of the way through the swing, whatever body she's wearing.</summary>
        internal bool Hammer(int stage,Vector3 aim)
        {
            if(actor.flight)
            {
                actor.BeginAttack("Cleave",CutterSeconds,CutterSeconds*.5f);
                var origin=transform.position+Vector3.up*.8f;var cut=Hit(actor.damage*CutterHit,actor.edgeType,CutterBreak,HitWeight.Medium);
                foreach(var body in Swept(origin,aim,CutterReach,CutterArc))body.Receive(cut);
                SkillArc.Flash(origin,aim,CutterReach,CutterArc,Ember,.2f);CombatAudio.Swing(actor,origin+aim*2);
                return true;
            }
            stage=Mathf.Clamp(stage,0,2);float seconds=HammerSeconds[stage];
            actor.BeginAttack("Attack"+(stage+1),seconds,seconds*.85f);
            // The marked body a stride away, with the stick at rest, as Taren's swing finds it.
            if(!actor.TargetLocked&&actor.target!=null&&actor.target.Alive)
            {
                var to=Planar(actor.target.transform.position-transform.position);
                if(to.sqrMagnitude>.01f&&to.sqrMagnitude<3.2f*3.2f&&(actor.MoveIntent.sqrMagnitude<.04f||Vector3.Angle(actor.motor.Facing,to)<120)){aim=to.normalized;actor.FaceFor(to);}
            }
            After(seconds*HammerContact,()=>HammerLands(stage,aim));
            return true;
        }
        void HammerLands(int stage,Vector3 aim)
        {
            bool last=stage==2;var centre=Ahead(aim,HammerReach);
            var packet=Hit(actor.damage*HammerHit[stage],actor.edgeType,HammerBreak[stage],last?HitWeight.Finisher:stage==1?HitWeight.Heavy:HitWeight.Medium);
            CombatAudio.Swing(actor,centre);
            foreach(var box in Reached(centre,last?HammerLastRadius:HammerRadius))
            {
                if(box.Hit(packet)<=0||!last||!box.owner.Alive)continue;
                // The third puts a light body on the floor: slowed, and its next attack put back. Only one that can be shoved (EnemyBrain.Shovable), which is never a
                // boss, a heavy body, or one that has marked an attack: what it marked is where it strikes from, and a tell is never taken back.
                if(box.owner.TryGetComponent<EnemyBrain>(out var brain)&&brain.Shovable){brain.Interrupt();box.owner.SlowUntil=Mathf.Max(box.owner.SlowUntil,Time.time+HammerDown);}
            }
            CombatVfx.Burst(centre,Ember,"lunge");
            if(!last)return;
            SkillRing.Expand(CombatCover.Ground(centre+Vector3.up*.6f),HammerLastRadius,GroundRing.Friendly,.25f,false);Impact.Thud(actor,aim);
        }
        /// <summary>A shockwave round her, a guard's answer (BraceKit): everything in reach, a ring for the moment it lands so it reads as hers.</summary>
        internal void Shock(float hit,float breakPower,float reach,HitWeight weight)
        {
            var packet=Hit(actor.damage*hit,DamageType.Kinetic,breakPower,weight);var chest=transform.position+Vector3.up*(actor.flight?.8f:.9f);
            if(actor.flight){reach+=1.5f;foreach(var body in Swept(chest,actor.motor.Facing,reach,180))body.Receive(packet);}
            else foreach(var box in Reached(chest,reach))box.Hit(packet);
            SkillRing.Expand(transform.position,reach,GroundRing.Friendly,.35f,actor.flight);
            CombatVfx.Burst(chest,Ember,"hit");Sound("explosionCrunch_000",.24f,1.1f);Impact.Thud(actor,actor.motor.Facing);
        }

        // ---------- her skills ----------
        void Idra(SkillKind kind,float power,Vector3 aim,bool flight)
        {
            switch(kind)
            {
                case SkillKind.Breach:
                {
                    // The brace leads: what lands on the way lands as on a guard, and small bodies on the line are gone through, not stopped at.
                    float length=flight?BreachFlightLength:BreachLength;
                    actor.Perform("Dash",flight?.45f:.5f,flight?.3f:.42f);actor.armoredUntil=GameTime.Now+.45f;Sound("forceField_000",.2f,.75f);
                    StartCoroutine(Through(length+4,.3f));actor.motor.Dash(aim,length);
                    var packet=Hit(Melee(BreachHit,power),DamageType.Kinetic,BreachBreak,HitWeight.Heavy);
                    if(flight){GetComponent<FlightDashContact>().Begin(packet);CombatAudio.Launch(actor);}
                    else StartCoroutine(Breaching(aim,packet));
                    break;
                }
                case SkillKind.TowLine:
                {
                    var packet=Hit(Melee(TowHit,power),DamageType.Pulse,TowBreak,HitWeight.Medium);
                    if(flight){actor.Perform("Shoot",.4f,.16f);TowCable.Cast(actor,Muzzle(aim),aim,TowFlightLength,packet);}
                    else{actor.Perform("Pulse",.55f,.45f);After(.2f,()=>TowCable.Cast(actor,Hand(aim),aim,TowLength,packet));}
                    break;
                }
                case SkillKind.Shelter:
                    Sound("forceField_000",.16f,.9f);
                    if(flight){actor.Perform("Buff",.4f,.16f);ShelterDome.Plant(actor,Between(ShelterDome.FlightRadius),true);}
                    else{actor.Perform("Buff",.5f,.42f);After(.25f,()=>ShelterDome.Plant(actor,Between(ShelterDome.Radius),false));}
                    break;
                case SkillKind.SlagHammer:
                    Sound("forceField_000",.2f,.6f);
                    if(flight){actor.Perform("Cleave",.7f,.5f);After(SlagFlightWindUp,()=>Slag(power,actor.motor.Facing,true));break;}
                    // Planted for the wind-up, with the ring gathering where it will land.
                    actor.Perform("Attack3",.8f,.7f);actor.Root(SlagWindUp);
                    SkillRing.Expand(CombatCover.Ground(Ahead(aim,1.6f)+Vector3.up*.6f),SlagRadius,GroundRing.Friendly,SlagWindUp,false,true,SlagRadius*1.7f);
                    After(SlagWindUp,()=>Slag(power,aim,false));
                    break;
                case SkillKind.RescueFlare:
                    Sound("laserSmall_000",.2f,.6f);
                    if(flight){actor.Perform("Buff",.45f,.2f);RescueFlare.Fire(actor);}
                    else{actor.Perform("Buff",.6f,.5f);After(.3f,()=>RescueFlare.Fire(actor));}
                    break;
                case SkillKind.Counterweight:
                    actor.Perform("Buff",.45f,flight?.2f:.35f);if(TryGetComponent<BraceKit>(out var weights))weights.Arm();
                    Sound("impactMetal_000",.2f,.6f);CombatVfx.Burst(transform.position+Vector3.up*.9f,Ember,"shape");
                    SkillRing.Expand(transform.position,BraceKit.CounterweightReach,GroundRing.Friendly,.3f,flight);
                    break;
                case SkillKind.Avalanche:
                    StartCoroutine(Avalanching(power,aim,flight));
                    break;
                case SkillKind.HoldTheLine:
                    actor.Perform("Buff",.5f,flight?.2f:.4f);if(TryGetComponent<BraceKit>(out var line))line.Hold();
                    Sound("explosionCrunch_000",.2f,.8f);CombatVfx.Burst(transform.position+Vector3.up*.9f,Ember,"shape");
                    SkillRing.Expand(transform.position,6,GroundRing.Friendly,.5f,flight);
                    break;
                // Sync 16, 20 and 24 (HeroSkills.IdraLate).
                default:IdraLate(kind,power,aim,flight);break;
            }
        }
        Vector3 Between(float radius)=>IdraSkills.Between(actor,radius);
        /// <summary>Breach on foot: as the dash carries her, whatever her brace meets is struck, once.</summary>
        IEnumerator Breaching(Vector3 aim,DamagePacket packet)
        {
            int sequence=actor.AttackSequence;var struck=new HashSet<Health>();
            for(float t=0;t<.3f;t+=actor.ActionDelta)
            {
                if(Dropped(sequence)){actor.armoredUntil=0;yield break;}
                foreach(var box in Reached(Ahead(aim,.5f),BreachRadius))if(struck.Add(box.owner))box.Hit(packet);
                yield return null;
            }
        }
        void Slag(float power,Vector3 aim,bool flight)
        {
            var packet=Hit(Melee(SlagHit,power),DamageType.Plasma,SlagBreak,HitWeight.Finisher);
            if(flight)
            {
                var origin=transform.position+Vector3.up*.8f;
                foreach(var body in Swept(origin,aim,SlagFlightReach,50))body.Receive(packet);
                SkillArc.Flash(origin,aim,SlagFlightReach,50,Ember,.35f);
            }
            else
            {
                var centre=Ahead(aim,1.6f);
                foreach(var box in Reached(centre,SlagRadius))box.Hit(packet);
                SkillRing.Expand(CombatCover.Ground(centre+Vector3.up*.6f),SlagRadius,GroundRing.Friendly,.3f,false);CombatVfx.Burst(centre,Ember,"kill");
            }
            Sound("explosionCrunch_000",.26f,.7f);Impact.Thud(actor,aim);
        }
        /// <summary>Avalanche: three beats, each a leap and a slam, the last the heaviest. Braced throughout, as her charge is; a roll lets go of it.</summary>
        IEnumerator Avalanching(float power,Vector3 aim,bool flight)
        {
            actor.armoredUntil=GameTime.Now+AvalancheBeat*3+.15f;float half=AvalancheBeat*.5f;
            for(int beat=0;beat<3;beat++)
            {
                actor.Perform(flight?"Dash":"Attack"+(beat+1),AvalancheBeat,AvalancheBeat*(3-beat)+.1f);int sequence=actor.AttackSequence;
                for(float t=0;t<AvalancheBeat;)
                {
                    if(Dropped(sequence)){actor.armoredUntil=0;yield break;}
                    float dt=actor.ActionDelta;
                    if(dt>0&&t<half)
                    {
                        var step=aim*(AvalancheLeap*Mathf.Min(dt,half-t)/half);
                        if(!flight){if(TryGetComponent<GroundMotor>(out var ground))ground.MoveAction(step,aim);}
                        else if(TryGetComponent<CharacterController>(out var hull)&&hull.enabled)hull.Move(step);
                    }
                    t+=dt;yield return null;
                }
                bool last=beat==2;float radius=AvalancheRadius[beat]+(flight?2:0);
                var packet=Hit(Melee(AvalancheHit[beat],power),DamageType.Kinetic,AvalancheBreak[beat],last?HitWeight.Finisher:HitWeight.Heavy);
                if(flight)
                {
                    var origin=transform.position+Vector3.up*.8f;
                    foreach(var body in Swept(origin,aim,radius,180))body.Receive(packet);
                    SkillRing.Expand(transform.position,radius,GroundRing.Friendly,.3f,true);
                }
                else
                {
                    var centre=Ahead(aim,1.2f);
                    foreach(var box in Reached(centre,radius))box.Hit(packet);
                    SkillRing.Expand(CombatCover.Ground(centre+Vector3.up*.6f),radius,GroundRing.Friendly,.3f,false);CombatVfx.Burst(centre,Ember,last?"kill":"hit");
                }
                Sound("explosionCrunch_000",last?.26f:.2f,last?.65f:.95f+beat*.1f);Impact.Thud(actor,aim);
            }
        }
    }
}
