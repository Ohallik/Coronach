using System.Collections;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Idra Sorn's three learned at Sync 16, 20 and 24 (S6a), on the ladder the first two heroes climb, each in her line of work:
    /// <list type="bullet">
    /// <item>Take the Weight (16): for six seconds every enemy hit that would land on her partner lands on her instead, through her Plating and her brace
    /// (<see cref="WeightLine"/>). The others answer a hit by not being there; she answers it for somebody else.</item>
    /// <item>Cutting Torch (20): a rescue cutter's torch held on what's in front of her, planted, seven ticks of Plasma with a lot of Break, that a plated front doesn't
    /// stop (Health.CutsPlate). The one answer in the party to a guardian met head on.</item>
    /// <item>Bulkhead (24): she slams the brace down and a wall of hard light stands across her aim for six seconds; the slam strikes and shoves what's under it, and while
    /// it stands it stops enemy shots and holds ordinary bodies on their own side (<see cref="BulkheadWall"/>).</item>
    /// </list>
    /// Each has a flight form: the line runs between two ships, the torch is a short cutter off the prow, and the wall stands in the flight plane.</summary>
    public sealed partial class HeroSkills
    {
        /// <summary>Cutting Torch: how long she holds it, how often it ticks and for what in Output, each tick's Break, where the flame reaches on foot (centre and radius)
        /// and in flight (length and half-angle).</summary>
        public const float TorchSeconds=1.4f,TorchTick=.2f,TorchEach=.55f,TorchBreak=18,TorchReach=1.8f,TorchRadius=1.3f,TorchFlightReach=6,TorchFlightArc=18;
        public const int TorchTicks=7;
        /// <summary>Bulkhead: the slam's wind-up, how far ahead the wall stands on foot and in flight, what the slam lands for in Output, its Break, and how deep a band
        /// either side of the wall it strikes.</summary>
        public const float BulkheadWindUp=.3f,BulkheadAhead=3,BulkheadFlightAhead=4,BulkheadHit=1.8f,BulkheadBreak=70,BulkheadBand=1.6f;
        static readonly Color Torch=new(2.2f,1.5f,.8f);

        void IdraLate(SkillKind kind,float power,Vector3 aim,bool flight)
        {
            switch(kind)
            {
                case SkillKind.TakeTheWeight:
                    actor.Perform("Buff",.45f,flight?.2f:.35f);Sound("forceField_000",.2f,.55f);
                    WeightLine.Take(actor);
                    break;
                case SkillKind.CuttingTorch:
                    // On foot she plants for it, as for the slag hammer; a ship keeps its way on and the cutter turns with the nose.
                    actor.Perform(flight?"Shoot":"Attack2",TorchSeconds+.25f,TorchSeconds+.15f);
                    StartCoroutine(Cutting(power,aim,flight));
                    break;
                case SkillKind.Bulkhead:
                {
                    float ahead=flight?BulkheadFlightAhead:BulkheadAhead;
                    actor.Perform(flight?"Cleave":"Attack3",.75f,flight?.35f:.6f);if(!flight)actor.Root(BulkheadWindUp);Sound("forceField_000",.22f,.5f);
                    SkillRing.Expand(flight?transform.position+aim*ahead:CombatCover.Ground(Ahead(aim,ahead)+Vector3.up*.6f),(flight?BulkheadWall.FlightWidth:BulkheadWall.Width)*.5f,GroundRing.Friendly,BulkheadWindUp,flight,true,1);
                    After(BulkheadWindUp,()=>Slam(power,aim,flight));
                    break;
                }
            }
        }

        // ---------- Cutting Torch ----------
        IEnumerator Cutting(float power,Vector3 aim,bool flight)
        {
            int sequence=actor.AttackSequence;if(!flight)actor.Root(TorchSeconds+.1f);
            var material=new Material(Resources.Load<Material>("Effects/flare_01"));material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            var flame=new GameObject("Cutting Torch");flame.transform.SetParent(transform,false);var glow=BeamLine(flame,material,0);var core=BeamLine(flame,material,1);
            float elapsed=0,pulse=1;int ticks=0;var along=Planar(aim).sqrMagnitude>.01f?Planar(aim).normalized:transform.forward;
            Sound("forceField_000",.22f,1.9f);
            try
            {
                while(elapsed<TorchSeconds)
                {
                    // Any new action lets go of it, a roll first of all; a stagger takes it away.
                    if(Dropped(sequence))yield break;
                    float dt=GameTime.CombatHeld?0:actor.ActionDelta;
                    if(dt>0)
                    {
                        elapsed+=dt;
                        if(flight)along=Planar(actor.motor.Facing).normalized;
                        else{along=Turned(along,dt);transform.rotation=Quaternion.LookRotation(along);if(TryGetComponent<GroundMotor>(out var feet))feet.KeepFacing(along);}
                        var origin=flight?Muzzle(along):Hand(along);
                        float reach=flight?TorchFlightReach:TorchReach+TorchRadius*.6f;
                        float length=CombatCover.Sweep(origin,origin+along*reach,.1f,out var wall)?wall.distance:reach;
                        bool ticked=false;
                        while(ticks<TorchTicks&&elapsed>=TorchTick*(ticks+1)-.01f)
                        {
                            ticks++;ticked=true;
                            var packet=CombatActor.MultiHit(Hit(Melee(TorchEach,power),DamageType.Plasma,TorchBreak));packet.tag=Health.CutsPlate;
                            if(flight)foreach(var body in Swept(transform.position+Vector3.up*.8f,along,TorchFlightReach,TorchFlightArc))body.Receive(packet);
                            else foreach(var box in Reached(Ahead(along,TorchReach),TorchRadius))box.Hit(packet);
                            CombatVfx.Burst(origin+along*length,Torch,"hit");Sound("impactMetal_000",.12f,1.5f+ticks*.04f);
                        }
                        pulse=ticked?1:Mathf.MoveTowards(pulse,0,dt/TorchTick);
                        glow.SetPosition(0,origin);glow.SetPosition(1,origin+along*length);core.SetPosition(0,origin);core.SetPosition(1,origin+along*length);
                        glow.startWidth=.3f+.12f*pulse;glow.endWidth=.12f;core.startWidth=.12f+.04f*pulse;core.endWidth=.04f;
                        glow.startColor=glow.endColor=new Color(1.6f,.6f,.15f,.55f+.2f*pulse);core.startColor=core.endColor=new Color(2.4f,1.9f,1.3f,1);
                        // As a companion she doesn't finish a cut standing in a marking: she lets go and walks out.
                        if(!Lead&&LateSkills.InDanger(actor)){actor.Perform("Idle",.05f,0);yield break;}
                    }
                    yield return null;
                }
            }
            finally{Destroy(flame);Destroy(material);if(actor.AttackSequence==sequence)actor.rootedUntil=0;}
        }

        // ---------- Bulkhead ----------
        /// <summary>The slam that raises it: everything in a band either side of where it stands is struck, and a light body under it goes to its far side.</summary>
        void Slam(float power,Vector3 aim,bool flight)
        {
            aim=Planar(aim).sqrMagnitude>.01f?Planar(aim).normalized:transform.forward;
            var centre=flight?transform.position+aim*BulkheadFlightAhead:Ahead(aim,BulkheadAhead);
            var wall=BulkheadWall.Raise(actor,centre,aim,flight);
            var packet=Hit(Melee(BulkheadHit,power),DamageType.Kinetic,BulkheadBreak,HitWeight.Heavy);var chest=Body;
            foreach(var body in Health.All.ToArray())
            {
                if(body==null||!body.Alive||body.friendly==actor.Health.friendly)continue;
                var from=Planar(body.transform.position-wall.transform.position);float across=Vector3.Dot(from,aim),sideways=Vector3.Dot(from,wall.Along);
                if(Mathf.Abs(across)>BulkheadBand||Mathf.Abs(sideways)>wall.Half+.5f||Mathf.Abs(body.transform.position.y-transform.position.y)>2.5f)continue;
                if(!CombatCover.Clear(chest,body.transform.position+Vector3.up*(flight?0:.8f)))continue;
                body.Receive(packet);
                // A shove never takes back a marked attack (EnemyBrain.Shovable): what's telling one stays where it painted.
                if(body.Alive&&body.TryGetComponent<EnemyBrain>(out var brain)&&brain.Shovable)brain.Displace(aim*(BulkheadWall.Hold+.9f-across));
            }
            Sound("explosionCrunch_000",.26f,.75f);CombatVfx.Burst(wall.transform.position+Vector3.up*(flight?0:.6f),Ember,"kill");Impact.Thud(actor,aim);
        }
    }
}
