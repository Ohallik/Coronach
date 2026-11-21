using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The stomper (the Bogstrider; later anything long-legged that strikes both ways): it has a front and a back and both of them hurt. Before it, a stomp on a marked circle; behind it, a
    /// kick across a marked fan; at its flanks, nothing. It turns slowly (<c>tune.speed</c> degrees a second) and not at all while it tells, so its side is a place a player can keep to, and
    /// circling it is the fight. A stomp that strikes nobody sinks its foot in the mire: it stands stuck and open for <c>tune.recover</c> seconds. So the answer is its flank, and the better
    /// answer is to draw the stomp from in front and step aside of it. Getting behind it, the answer to a guardian, is what its kick punishes.
    /// Reads: attacks[0] (the stomp: telegraph, range it stomps at, radius, damage, cooldown), attacks[1] (the kick: telegraph, range is its reach behind, damage, cooldown), tune.speed (its
    /// turn, 60), arc (the kick's fan, 120: what counts as behind it), recover (the stuck foot, 1.6).
    /// </summary>
    public sealed class EnemyStomper:EnemyBehaviour
    {
        enum State{Wade,Stomp,Kick}
        State state;float stuckUntil;
        float Turn=>Or(tune.speed,60);float Rear=>Mathf.Clamp(Or(tune.arc,120),30,180);float Stuck=>Or(tune.recover,1.6f);
        /// <summary>The half-angle before it that it stomps into: a hero further round than this is at its flank.</summary>
        public const float FrontHalf=35;
        bool Kicks=>def.attacks!=null&&def.attacks.Length>1&&def.attacks[1]!=null;
        float StompRadius=>attack!=null&&attack.radius>0?attack.radius:2.2f;
        /// <summary>Where the stomp lands: a stride before it, inside its reach.</summary>
        Vector3 StompAt=>body.position+body.forward*(Range*.55f);
        public bool StuckFast=>Time.time<stuckUntil;
        public override void Tick(CombatActor victim)
        {
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            if(brain.Telegraphing)
            {
                if(!brain.TellRunOut)return;
                bool kick=state==State.Kick;var move=kick?Second:attack;var mark=brain.EndTell(.4f);int landed=0;
                if(mark!=null)
                {
                    landed=StrikeIn(mark,brain.Blow(move,HitWeight.Heavy),Chest);mark.Release();
                    CombatVfx.Burst(mark.Origin+(kick?mark.Forward*(Second.range*.5f):Vector3.zero)+Vector3.up*.3f,new Color(.5f,.56f,.42f),"kill");
                }
                Sound(kick?"swing_1":"explosionCrunch_000",kick?.24f:.2f,kick?1.15f:.8f);
                brain.Rest(move!=null?move.cooldown:2);Posing=false;state=State.Wade;
                if(!kick&&landed==0)
                {
                    // Its foot is in the mud to the knee: nose down, and no turning, until it pulls it out.
                    stuckUntil=Time.time+Stuck;Posing=true;Pitch=12;Open(Stuck);Debug.Log("STOMPER_STUCK "+def.id);
                }
                return;
            }
            if(Time.time<stuckUntil)return;
            Posing=false;
            float off=distance>.01f?Vector3.Angle(body.forward,to):0;
            if(brain.Rested&&brain.Sees(victim))
            {
                if(off<=FrontHalf&&distance<Range)
                {
                    state=State.Stomp;float tell=Tell;
                    Mark(Telegraph.Circle(StompAt,StompRadius,tell,Telegraph.Threat,true),tell,body.forward,victim,"root_groan",.75f);
                    return;
                }
                if(Kicks&&off>=180-Rear*.5f&&distance<Second.range)
                {
                    state=State.Kick;float tell=Mathf.Max(.5f,Second.telegraph);
                    Mark(Telegraph.Cone(body.position,-body.forward,Second.range,Rear,tell,Telegraph.Threat,true),tell,-body.forward,victim,"water_splash",.7f);
                    return;
                }
            }
            // Slow to come round: at its flank a hero has a moment before it faces him again.
            Face(to,Turn);
            brain.Spread(to,distance,Range);
            // It wades where it faces: a body that walked straight at a hero at its side would have no side.
            if(distance>Range*.8f&&off<60)brain.Walk(body.forward,def.speed);
        }
        public override void Cancel(){state=State.Wade;Posing=false;}
    }
}
