using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The strafer (the Podskimmer; later the Muster Lancer), a flier that fights by passes: it circles off a hero, marks a lane through him, runs it fast and breaks away at the
    /// far end, and comes round again. It strikes only what it touches inside its lane, and it is easiest to hit as it breaks off. Strafers fly as a wing: when one of them dies,
    /// every other within <see cref="WingReach"/> gives up what it was doing and scatters from the place for a beat, so a kill buys the party room instead of a crossfire.
    /// Reads: attacks[0] (telegraph, range to start a run from, damage, cooldown), tune.reach (the pass, 16 m), width (2.2), speed (20), recover (how long it breaks away, 1.3 s).
    /// <para><b>On a circuit</b> (<c>tune.wake</c> over zero: chapter 4's Veil Ray): it doesn't circle a ship but glides a fixed loop of that radius round where it was set, at its own
    /// speed, and makes its pass only at a ship that comes within its run-in; after the break it goes back to the loop. So where it is on its loop is something a pilot can read and
    /// time (the dead beacon is relit on its far pass). <b>A tail</b> (a second attack): a ship close behind it, inside that attack's range and <c>tune.arc</c> (140) of its tail, is swept
    /// on a marked fan. Tailing it is what it punishes; crossing its nose is what its pass does.</para>
    /// <para><b>Comes about</b> (<c>tune.pace</c> over nought: chapter 8's Muster Lancer, a long lance run and then a slow turn): at the end of its pass it doesn't break away. It comes about
    /// through a wide half turn at <c>pace</c> of its run's speed for <c>tune.recover</c> seconds, open (a blow lands at half again), and then circles again. Cross out of its lane, then hit it on
    /// its turn. <b>Loses its quarry</b> (<c>tune.linger</c> over nought: the Lancer hunting the Picket of Teeth): a ship it hasn't been able to see for that many seconds (a rock's
    /// <see cref="SensorShade"/> between them, or the ship past <see cref="LosesAt"/>) is lost; it breaks off and circles where it is until it sees one again. It isn't walked home meanwhile.</para>
    /// <para><b>A pair</b> (<c>tune.pair</c>: chapter 9's Interceptor), one pinning with a tractor line while the other strikes, is <c>EnemyStrafer.Pair.cs</c>.</para>
    /// <para><b>Comes round</b> what is in the way: circling with its quarry out of sight and held against something solid, it goes the other way round the ring, and breaks away into the
    /// open if that is shut too (<see cref="ComeRound"/>).</para>
    /// </summary>
    public sealed partial class EnemyStrafer:EnemyBehaviour
    {
        enum State{Circle,Told,Run,Break,Sweep,Turn,Lost}
        static readonly List<EnemyStrafer> wing=new();
        State state;Vector3 direction,away;float travelled,until,loop;Telegraph lane;int side;
        readonly HashSet<Health> met=new();
        float Reach=>Or(tune.reach,16);float Width=>Or(tune.width,2.2f);float Speed=>Or(tune.speed,20);float Breaks=>Or(tune.recover,1.3f);
        // A pair's second attack is its tractor line (EnemyStrafer.Pair.cs), not a tail.
        float Circuit=>tune.wake;bool Tail=>!tune.pair&&def.attacks!=null&&def.attacks.Length>1&&def.attacks[1]!=null;float TailArc=>Mathf.Clamp(Or(tune.arc,140),30,300);
        public const float WingReach=24;
        public bool Scattering=>state==State.Break;
        /// <summary>Coming about after its pass, open; and having lost the ship it hunted, circling where it is.</summary>
        public bool ComingAbout=>state==State.Turn;
        public bool LostItsQuarry=>state==State.Lost;
        /// <summary>How far off a hunted ship is out of its sight, and how wide it circles where it lost one.</summary>
        public const float LosesAt=40,LostCircle=8;
        bool ComesAbout=>tune.pace>0;bool Hunts=>tune.linger>0;
        float unseenSince=-1;Vector3 lostAt,turnHeading;
        /// <summary>One on a circuit keeps its loop when the party is far off; it isn't walked home. Nor is a hunter that has lost its ship: it circles where it lost it.</summary>
        public override bool Roams=>Circuit<=0&&!Hunts;
        /// <summary>Where on its circuit it is: radians round its home from north, toward the east.</summary>
        public float Bearing=>loop;
        protected override void Begin()
        {
            wing.Add(this);side=(wing.Count&1)==0?1:-1;
            // It joins its loop where it was set down, not at the loop's first point.
            if(Circuit>0){var from=Flat(body.position-brain.Home);loop=from.sqrMagnitude>.01f?Mathf.Atan2(from.x,from.z):0;}
        }
        /// <summary>A ship close behind it, inside its tail's reach and arc.</summary>
        bool Behind(CombatActor victim)
        {
            if(!Tail)return false;
            var from=Flat(victim.transform.position-body.position);
            return from.magnitude<Second.range&&Vector3.Angle(-body.forward,from)<=TailArc*.5f;
        }
        public override void Tick(CombatActor victim)
        {
            // In a pair (EnemyStrafer.Pair.cs): the one that pins flies that and nothing else; the one that strikes runs its lane through whoever is pinned.
            if(tune.pair){if(PairTick(victim))return;victim=Quarry(victim);}
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;var toward=distance>.01f?to/distance:body.forward;
            if(Hunts&&(state==State.Circle||state==State.Lost))
            {
                bool seen=distance<=LosesAt&&!SensorShade.Blocks(body.position,victim.transform.position);
                if(seen)unseenSince=-1;else if(unseenSince<0)unseenSince=Time.time;
                if(state==State.Lost)
                {
                    if(seen){state=State.Circle;Debug.Log("STRAFER_FOUND "+def.id);}
                    else
                    {
                        // Round where it lost him, a little ahead on the loop, at its own pace.
                        var from=Flat(body.position-lostAt);float bearing=from.sqrMagnitude>.01f?Mathf.Atan2(from.x,from.z):0;
                        var ahead=lostAt+new Vector3(Mathf.Sin(bearing+side*.5f),0,Mathf.Cos(bearing+side*.5f))*LostCircle;var glide=Flat(ahead-body.position);
                        if(glide.sqrMagnitude>.01f){brain.Walk(glide,def.speed);body.rotation=Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(glide),120*Time.deltaTime);}
                        return;
                    }
                }
                else if(unseenSince>=0&&Time.time-unseenSince>=tune.linger){state=State.Lost;lostAt=body.position;Posing=false;Roll=0;Debug.Log("STRAFER_LOST "+def.id);return;}
            }
            switch(state)
            {
                case State.Circle:
                    Posing=false;Roll=0;
                    if(brain.Rested&&Behind(victim)&&brain.Sees(victim))
                    {
                        float tell=Mathf.Max(.5f,Second.telegraph);
                        Mark(Telegraph.Cone(body.position,-body.forward,Second.range,TailArc,tell,Telegraph.Threat,true),tell,-body.forward,victim,"swing_2",.7f);
                        state=State.Sweep;return;
                    }
                    if(distance<Range&&brain.Rested&&brain.Sees(victim))
                    {
                        direction=toward;body.rotation=Quaternion.LookRotation(direction);
                        Mark(Telegraph.Line(body.position,direction,Reach,Width,Tell,Telegraph.Threat,true),Tell,direction,victim,"thrusterFire_000",1.5f);
                        state=State.Told;return;
                    }
                    brain.Spread(to,distance,Range);
                    if(Circuit>0)
                    {
                        // Its own loop, whoever is about: aimed a little ahead on the loop, so it glides round it rather than chasing a point.
                        loop+=Time.deltaTime*Mathf.Max(.5f,def.speed)/Circuit;
                        var ahead=brain.Home+new Vector3(Mathf.Sin(loop+.35f),0,Mathf.Cos(loop+.35f))*Circuit;var glide=Flat(ahead-body.position);
                        if(glide.sqrMagnitude>.01f){brain.Walk(glide,def.speed);body.rotation=Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(glide),120*Time.deltaTime);}
                        break;
                    }
                    // Round him at the far end of its run-in, closing if he opens the gap: a wing is a ring of these.
                    var round=Vector3.Cross(Vector3.up,toward)*side+toward*Mathf.Clamp((distance-Range*.8f)*.5f,-1,1);
                    var was=body.position;
                    brain.Walk(round,def.speed);body.rotation=Quaternion.LookRotation(Flat(round).sqrMagnitude>.01f?Flat(round):toward);
                    ComeRound(victim,was,toward);
                    break;
                case State.Told:
                    if(!brain.TellRunOut)return;
                    lane=brain.EndTell(.2f);if(lane!=null)lane.Release();
                    state=State.Run;travelled=0;met.Clear();brain.Acting=true;PassThrough(true);Posing=true;Pitch=9;Roll=0;
                    break;
                case State.Run:
                    float step=Mathf.Min(Speed*Time.deltaTime,Reach-travelled);
                    var before=body.position;if(controller.enabled)controller.Move(direction*step);
                    float moved=Flat(body.position-before).magnitude;travelled+=Mathf.Max(moved,step*.25f);
                    Touch(met,controller.radius+.7f,brain.Blow(attack,HitWeight.Medium),lane);
                    if(travelled>=Reach||moved<step*.2f&&travelled>1){if(ComesAbout)ComeAbout();else BreakAway(Quaternion.Euler(0,side*105,0)*direction,Breaks,Cooldown);}
                    break;
                case State.Turn:
                    // Open through its turn, which the brain doesn't tick: the turn is carried out in Present. Here only once it is over.
                    if(Time.time>=until){state=State.Circle;Posing=false;Roll=0;side=-side;}
                    break;
                case State.Lost:state=State.Circle;break;
                case State.Break:
                    brain.Walk(away,def.speed*1.5f);body.rotation=Quaternion.LookRotation(away);
                    if(Time.time>=until){state=State.Circle;side=-side;}
                    break;
                case State.Sweep:
                    // Its tail comes across the fan it marked: it holds its heading while it does.
                    Posing=true;Roll=side*18*brain.TellProgress;
                    if(!brain.TellRunOut)return;
                    var swept=brain.EndTell(.3f);
                    if(swept!=null){StrikeIn(swept,brain.Blow(Second,HitWeight.Heavy),Chest);swept.Release();}
                    Sound("swing_2",.24f,.75f);brain.Rest(Second.cooldown);state=State.Circle;Posing=false;Roll=0;
                    break;
            }
        }
        /// <summary>The end of a lance run: no breaking away. It comes about through a wide half turn at a fraction of its run's speed, open all the way round.</summary>
        void ComeAbout()
        {
            PassThrough(false);brain.Acting=false;lane=null;state=State.Turn;until=Time.time+Breaks;unseenSince=-1;brain.Rest(Mathf.Max(Cooldown,Breaks+.4f));
            turnHeading=Flat(direction).sqrMagnitude>.01f?Flat(direction).normalized:Flat(body.forward).normalized;
            Posing=true;Pitch=0;Roll=side*28;Open(Breaks);Debug.Log("STRAFER_COMES_ABOUT "+def.id);
        }
        /// <summary>The turn, late every frame: the brain ticks nothing open, so this is where it flies it. Half round over its open time, slowing as it comes about.</summary>
        public override void Present()
        {
            PresentPin();
            if(state!=State.Turn||GameTime.Paused||!health.Alive||controller==null||!controller.enabled)return;
            float left=Mathf.Clamp01((until-Time.time)/Mathf.Max(.1f,Breaks));
            turnHeading=Quaternion.Euler(0,side*180/Mathf.Max(.1f,Breaks)*Time.deltaTime,0)*turnHeading;
            controller.Move(turnHeading*(Speed*tune.pace*(.5f+.5f*left)*Time.deltaTime));
            body.rotation=Quaternion.LookRotation(turnHeading);Posing=true;Roll=side*28*Mathf.Sin(left*Mathf.PI);
        }
        /// <summary>A test's switch for the strafer as it was before it came round (BargeSkimmerReplayTests sets it and puts it back): never set in a scene.</summary>
        public static bool ComesRoundOff;
        /// <summary>How long it is held against something solid, its quarry out of sight, before it goes the other way round; how soon after that, held again, it breaks away instead.</summary>
        public const float HeldFor=.4f,TurnedWithin=4;
        float heldSince=-1,turnedAt=-100;
        /// <summary>Circling its quarry with something large between them (a barge's hull, a pane), the ring ran it into the solid and held it there: pushed along the ring and in toward
        /// him at once, against a hull that faces him, a body slides to the one place where that push meets the hull square, and stops. The last skimmer of barge three's wing sat
        /// so for seventy seconds against the hull's far side, and once wedged between the bow and its knot, never seeing the ship it circled and so never making its pass (route 17,
        /// nov15-recon38 and nov20-recon40 s17). Held there with its quarry out of sight, it goes the other way round, as a wing comes round a thing in the way; held again soon after,
        /// both ways shut, it breaks away from him into the open and comes round from there. A hunter that loses its quarry (tune.linger) keeps its own answer.</summary>
        void ComeRound(CombatActor victim,Vector3 was,Vector3 toward)
        {
            if(ComesRoundOff||Hunts)return;
            float asked=def.speed*health.Pace*Time.deltaTime,moved=Flat(body.position-was).magnitude;
            // A slowing (Health.SlowUntil: four tenths of its pace) is no wall.
            if(asked<.0001f||moved>asked*.3f){heldSince=-1;return;}
            if(heldSince<0){heldSince=Time.time;return;}
            if(Time.time-heldSince<HeldFor)return;
            // Against him, a partner or a wall with him in plain sight, its pass is what comes next.
            if(CombatCover.Clear(body.position+Vector3.up,victim.transform.position+Vector3.up*.9f))return;
            heldSince=-1;
            if(Time.time-turnedAt<TurnedWithin)
            {
                turnedAt=-100;BreakAway(Quaternion.Euler(0,side*40,0)*-toward,Breaks,Breaks);Debug.Log("STRAFER_BREAKS_OUT "+def.id);
                return;
            }
            side=-side;turnedAt=Time.time;Debug.Log("STRAFER_COMES_ROUND "+def.id);
        }
        void BreakAway(Vector3 heading,float seconds,float rest)
        {
            PassThrough(false);brain.Acting=false;heading.y=0;away=heading.sqrMagnitude>.01f?heading.normalized:-body.forward;unseenSince=-1;
            state=State.Break;until=Time.time+seconds;brain.Rest(rest);lane=null;Posing=true;Pitch=0;Roll=-side*24;
        }
        /// <summary>One of the wing has died there: whatever this one was doing, it leaves it and flies from the place.</summary>
        void Scatter(Vector3 from)
        {
            if(!health.Alive)return;
            if(brain.Telegraphing)brain.Interrupt();
            var flee=Flat(body.position-from);if(flee.sqrMagnitude<.01f)flee=Quaternion.Euler(0,side*90,0)*body.forward;
            BreakAway(flee,Breaks*1.3f,Mathf.Max(Cooldown,Breaks*1.6f));Debug.Log("STRAFER_SCATTER "+def.id);
        }
        public override void Cancel(){PassThrough(false);if(state==State.Told||state==State.Run||state==State.Sweep)state=State.Circle;lane=null;Posing=false;Roll=0;Unpin("broken");}
        public override void Struck(DamagePacket packet,float amount){if(amount>0)Unpin("cut");}
        public override void Fell(DamagePacket packet)
        {
            Unpin("cut");LeavePair();
            wing.Remove(this);var at=body.position;
            foreach(var other in wing.ToArray())if(other!=this&&Flat(other.body.position-at).sqrMagnitude<WingReach*WingReach)other.Scatter(at);
        }
        public override void Dispose(){wing.Remove(this);PassThrough(false);Unpin("gone");LeavePair();}
        /// <summary>Carried by a current: the lane its pass is running down goes with it once its paint has gone from the scene (.32 s into a .8 s run: while it is there the current
        /// carries it as it carries every marking), and where it lost its quarry. A lane left where it was laid passed beside a ship carried across it.</summary>
        public override void Carried(Vector3 delta){if(!ReferenceEquals(lane,null)&&lane==null)lane.Shift(delta);lostAt+=delta;}
    }
}
