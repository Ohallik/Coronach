using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The swarm cloud (the Mirefly Swarm; later the Spore Swarm, Frost Motes, Mire Leeches): one weak body drawn as many motes (<see cref="SwarmMotes"/>). It weaves in, draws
    /// tight, marks the ground a hero stands on and darts onto it. A dart that lands latches: the cloud rides him for <c>tune.linger</c> seconds and he wades at <c>tune.pace</c>
    /// of his stride (a latch slows and can drain Thrust; it does no damage, since nothing unmarked may). A roll sheds it, and so does any hit on it. And it bursts when it is
    /// broken or killed: a marked blast on a short fuse where it hung (<see cref="DeathBlast"/>), so the answer to a swarm is Break, from a step back.
    /// Reads: attacks[0] (the dart: telegraph, range, damage, cooldown), attacks[1] (the burst's damage and type; the dart's if there is no second), tune.linger (the latch, 2.5 s),
    /// pace (.7), tick (Thrust drained a second while latched, none), recover (how long it hangs back after, 1.2 s), count (motes, 18), tint (the motes' colour), wake (in a place where
    /// light draws it, how near a ship flown dark must come to be found, 6 m).
    /// Where a place has the rule that light draws it (<see cref="ShipGlow"/>: the Spore Veil), a ship flown dark for two seconds loses a swarm riding it, and an awake one leaves a dark
    /// ship alone unless it comes close. Anywhere else a ship has no glow, is never dark, and none of that applies.
    /// </summary>
    public sealed class EnemySwarmCloud:EnemyBehaviour
    {
        enum State{Harry,Told,Latched,Loose}
        static readonly List<EnemySwarmCloud> latched=new();
        State state;SwarmMotes motes;CombatActor held;float until;bool popped;Health popper;DamageType popType;
        readonly List<CombatActor> caught=new();
        float Latch=>Or(tune.linger,2.5f);float Pace=>Or(tune.pace,.7f);float HangsBack=>Or(tune.recover,1.2f);
        /// <summary>How near a ship flown dark has to come before a light-drawn swarm finds it anyway (tune.wake, 6 m).</summary>
        float Lost=>Or(tune.wake,6);
        public const string Burst="swarmBurst";
        public bool Latched=>state==State.Latched;
        public override bool Roams=>state!=State.Latched;
        /// <summary>Whether a swarm is riding this hero now.</summary>
        public static bool On(CombatActor hero){foreach(var swarm in latched)if(swarm.held==hero)return true;return false;}
        protected override void Begin()
        {
            motes=visual!=null?visual.GetComponent<SwarmMotes>():null;
            // Each swarm its own colour: a mirefly's gold, a spore cloud's green, a leech knot's dark water.
            if(motes!=null&&tune.tint.a>0)motes.tone=tune.tint;
        }
        void Gather(float share){if(motes!=null)motes.Gather=share;}
        public override void Tick(CombatActor victim)
        {
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            var side=Vector3.Cross(Vector3.up,distance>.01f?to/distance:body.forward)*Mathf.Sin(Time.time*2.3f+brain.GetInstanceID());
            switch(state)
            {
                case State.Harry:
                    Gather(1);
                    // Drawn to light: a ship that has gone dark and keeps its distance is lost to it, and it hangs about where it lives until one is lit again or flies into it.
                    var glow=ShipGlow.On(victim);
                    if(glow!=null&&glow.Dark&&distance>Lost)
                    {
                        var home=Flat(brain.Home+Quaternion.Euler(0,Time.time*40+brain.GetInstanceID(),0)*Vector3.forward*1.5f-body.position);
                        if(home.sqrMagnitude>.04f)brain.Walk(home,def.speed*.4f);
                        return;
                    }
                    if(distance<Range&&brain.Rested&&brain.Sees(victim))
                    {
                        var at=Flying?victim.transform.position:CombatCover.Ground(victim.transform.position+Vector3.up);
                        Mark(Telegraph.Circle(at,Mathf.Max(1,Range*.6f),Tell,Telegraph.Threat,true),Tell,to,victim,"computerNoise_000",2.2f);
                        state=State.Told;Gather(.35f);return;
                    }
                    brain.Spread(to,distance,Range);
                    // Never still: it weaves as it closes, and circles once it is close.
                    brain.Walk(distance>Range*.7f?to.normalized+side*.6f:side,distance>Range*.7f?def.speed:def.speed*.5f);
                    break;
                case State.Told:
                    if(!brain.TellRunOut)return;
                    var mark=brain.EndTell(.3f);Gather(1);brain.Rest(Cooldown);
                    if(mark==null){state=State.Harry;return;}
                    // The dart: the whole cloud is on the mark as it lands.
                    if(controller.enabled)controller.Move(Flat(mark.Origin-body.position));
                    caught.Clear();StrikeIn(mark,brain.Blow(attack,HitWeight.Light),mark.Origin+Vector3.up*.6f,caught);mark.Release();
                    if(caught.Count==0){state=State.Loose;until=Time.time+HangsBack*.5f;return;}
                    held=caught[0];state=State.Latched;until=Time.time+Latch;latched.Add(this);PassThrough(true);Gather(1.15f);
                    Debug.Log("SWARM_LATCH "+def.id);BarkService.Play(held.character,"swarm");
                    break;
                case State.Latched:
                    if(held==null||!held.Health.Alive||Time.time>=until||held.State==ActorState.Dodge){Shed(held!=null&&held.State==ActorState.Dodge?"rolled":"spent");return;}
                    // Where light is what draws it (ShipGlow: the Spore Veil), a ship flown dark for two seconds of the ride is lost to it. Anywhere else nothing is ever dark.
                    if(Mathf.Min(ShipGlow.DarkFor(held),Time.time-(until-Latch))>=ShipGlow.ShedAfter){Shed("dark");return;}
                    held.Health.Hinder(Pace);if(tune.tick>0)held.thrust=Mathf.Max(0,held.thrust-tune.tick*Time.deltaTime);
                    var ride=Flat(held.transform.position-body.position);if(ride.magnitude>.25f)brain.Walk(ride,Mathf.Max(12,def.speed*2));
                    break;
                case State.Loose:
                    Gather(1.3f);brain.Walk(-to.normalized+side*.5f,def.speed*.8f);
                    if(Time.time>=until)state=State.Harry;
                    break;
            }
        }
        void Shed(string why)
        {
            if(state!=State.Latched)return;
            latched.Remove(this);PassThrough(false);held=null;state=State.Loose;until=Time.time+HangsBack;Debug.Log($"SWARM_SHED {def.id} {why}");
        }
        public override void Struck(DamagePacket packet,float amount)
        {
            if(motes!=null)motes.Jolt();
            if(amount>0)Shed("struck");
            // Broken, it bursts. Not from inside the blow that broke it: that hit is still being counted.
            if(health.Broken&&!popped){popped=true;popper=packet.source;popType=packet.type;}
        }
        public override void Present()
        {
            if(!popped||!health.Alive)return;
            popped=false;health.Receive(new DamagePacket{source=popper,amount=health.maximum*4,type=popType,tag=Burst});
        }
        public override void Cancel(){if(state==State.Told)state=State.Harry;Gather(1);}
        public override void Fell(DamagePacket packet)
        {
            Shed("burst");if(motes!=null)motes.Burst();
            var burst=Second;
            DeathBlast.Arm(body.position,new DamagePacket{source=health,amount=(burst!=null?burst.damage:12)*brain.DamageScale,type=burst!=null?burst.type:DamageType.Pulse,deathAttack=true,weight=HitWeight.Medium});
            Debug.Log("SWARM_BURST "+def.id);
        }
        public override void Dispose(){latched.Remove(this);PassThrough(false);}
    }
}
