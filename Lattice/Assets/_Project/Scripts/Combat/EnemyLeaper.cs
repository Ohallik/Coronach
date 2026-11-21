using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The leaper (chapter 6's Bridle-hound, an Ancelane hunting construct): it circles a hero at the edge of its reach, squares up, marks a lane and leaps down it. Whoever it lands on
    /// inside the lane is struck and pinned: held at <c>tune.pace</c> of his stride for <c>tune.linger</c> seconds, until he rolls or it is hit. Three answers, one for each kind of hero:
    /// roll out of the lane; strike it in the air, where a construct built to pin has nothing to brace on and <b>shatters</b> at the first blow that lands (any blow, whatever its size); or
    /// guard as it lands, and a braced hero throws it off, open, instead of going down under it. A leap that lands on nobody leaves it standing for <c>tune.recover</c> seconds.
    /// Reads: attacks[0] (telegraph, range to leap from, damage, cooldown), tune.reach (the leap's length, 7 m), width (1.6), speed (16), linger (the pin, 1.4 s), pace (.15),
    /// recover (after a miss, 1 s; thrown off a guard, half as long again and open).
    /// <para><b>Heavy</b> (<c>tune.heavy</c>: chapter 8's Forge Hound, the Muster's cruder copy): caught in the air it doesn't shatter. It is knocked down where it is, open as long as one thrown off
    /// a guard, and gets up again. <b>A scorch</b> (<c>tune.area</c> over nought: the same hound, hotter): wherever it comes down, on a hero, on nobody or knocked out of the air, it leaves a
    /// <see cref="LingeringArea"/> of that radius and <c>tune.effect</c> (a Burn's <c>tick</c> a second) for <see cref="ScorchSeconds"/>. Roll its lane, and don't roll back onto where it landed.</para>
    /// </summary>
    public sealed class EnemyLeaper:EnemyBehaviour
    {
        enum State{Prowl,Told,Leap,Pin,Recover}
        State state;Vector3 direction,rest;float travelled,length,until;Telegraph lane;CombatActor pinned;bool shatter;Health shatterer;DamageType shatterType;int side=1;
        readonly HashSet<Health> met=new();
        float Reach=>Or(tune.reach,7);float Width=>Or(tune.width,1.6f);float Speed=>Or(tune.speed,16);float PinFor=>Or(tune.linger,1.4f);float Held=>Or(tune.pace,.15f);float After=>Or(tune.recover,1);
        /// <summary>How high the leap carries what's drawn at its middle, and how much longer than a miss it stands open thrown off a guard.</summary>
        public const float Arc=1.1f,ThrownShare=1.5f;
        public const string Shatter="shatter";
        /// <summary>How long a hot leaper's scorch stays where it came down.</summary>
        public const float ScorchSeconds=3.5f;
        /// <summary>Knocked out of the air and down, getting up: a heavy leaper's opening.</summary>
        public bool KnockedDown{get;private set;}
        void Scorch(){if(tune.area>0)Leave(LingeringArea.Lay(CombatCover.Ground(body.position+Vector3.up),tune.area,ScorchSeconds,tune.effect,1,tune.tick,health,attack!=null?attack.type:DamageType.Kinetic));}
        /// <summary>In the air: the first blow that lands shatters it.</summary>
        public bool Airborne=>state==State.Leap;
        public bool Pinning=>state==State.Pin;
        public CombatActor Pinned=>pinned;
        public override bool Movable=>state==State.Prowl||state==State.Recover;
        protected override void Begin(){if(visual!=null)rest=visual.localPosition;side=(brain.GetInstanceID()&1)==0?1:-1;}
        public override void Tick(CombatActor victim)
        {
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;var toward=distance>.01f?to/distance:body.forward;
            switch(state)
            {
                case State.Prowl:
                    Posing=false;
                    if(distance<Range&&brain.Rested&&brain.Sees(victim))
                    {
                        direction=toward;body.rotation=Quaternion.LookRotation(direction);
                        // The lane runs a stride past him, and stops at a wall: what's painted is where it can land.
                        length=Mathf.Min(Reach,distance+1.5f);if(Wall(direction,length,out float gap))length=Mathf.Max(1.5f,gap);
                        Mark(Telegraph.Line(body.position,direction,length+controller.radius,Width,Tell,Telegraph.Threat,true),Tell,direction,victim,"swing_1",1.6f);
                        state=State.Told;Posing=true;Pitch=-12;return;
                    }
                    brain.Spread(to,distance,Range);
                    // It circles at the edge of its reach, as a hunting dog does, and closes if he backs off.
                    var round=Vector3.Cross(Vector3.up,toward)*side+toward*Mathf.Clamp((distance-Range*.8f)*.6f,-1,1);
                    brain.Walk(round,def.speed);Face(Flat(round).sqrMagnitude>.01f?Flat(round):toward,540);
                    break;
                case State.Told:
                    // Crouched for the spring: the wind-up deepens as the tell runs out.
                    Pitch=-12-8*brain.TellProgress;
                    if(!brain.TellRunOut)return;
                    lane=brain.EndTell(.2f);if(lane!=null)lane.Release();
                    state=State.Leap;travelled=0;met.Clear();brain.Acting=true;PassThrough(true);Pitch=8;
                    Sound("swing_2",.2f,1.4f);Debug.Log("LEAPER_LEAP "+def.id);
                    break;
                case State.Leap:
                    float step=Mathf.Min(Speed*Time.deltaTime,length-travelled);
                    var before=body.position;if(controller.enabled)controller.Move(direction*step);
                    travelled+=Mathf.Max(Flat(body.position-before).magnitude,step*.25f);
                    if(Land(victim))return;
                    if(travelled>=length-.01f){Landed(null);return;}
                    break;
                case State.Pin:
                    if(pinned==null||!pinned.Health.Alive||Time.time>=until||pinned.State==ActorState.Dodge){Release(pinned!=null&&pinned.State==ActorState.Dodge?"rolled":"spent");return;}
                    pinned.Health.Hinder(Held);
                    // It stands on him: where he is dragged, it goes.
                    var on=Flat(pinned.transform.position-body.position);if(on.magnitude>.5f&&controller.enabled)controller.Move(on.normalized*Mathf.Min(on.magnitude-.4f,12*Time.deltaTime));
                    break;
                case State.Recover:
                    if(Time.time<until)return;
                    state=State.Prowl;Posing=false;side=-side;KnockedDown=false;
                    break;
            }
        }
        /// <summary>Whoever it comes down on inside its lane: struck once, and the first of them pinned, unless he was braced for it.</summary>
        bool Land(CombatActor victim)
        {
            if(PartyController.Current==null)return false;
            foreach(var hero in PartyController.Current.members)
            {
                if(!hero.Health.Alive||met.Contains(hero.Health)||Flat(hero.transform.position-body.position).sqrMagnitude>=(controller.radius+.7f)*(controller.radius+.7f))continue;
                if(!ReferenceEquals(lane,null)&&!lane.Contains(hero.transform.position))continue;
                if(!CombatCover.Clear(Chest,hero.transform.position+Vector3.up*(hero.flight?0:.9f)))continue;
                met.Add(hero.Health);bool braced=hero.Guarding;
                if(hero.Health.Receive(brain.Blow(attack,HitWeight.Heavy))<=0)continue;
                Landed(braced?null:hero,braced);return true;
            }
            return false;
        }
        void Landed(CombatActor on,bool thrown=false)
        {
            PassThrough(false);brain.Acting=false;brain.Rest(Cooldown);Posing=true;Pitch=0;Scorch();
            if(on!=null)
            {
                pinned=on;state=State.Pin;until=Time.time+PinFor;PassThrough(true);Pitch=14;
                Sound("impactMetal_000",.22f,1.3f);Debug.Log($"LEAPER_PIN {def.id} {on.character}");
                return;
            }
            state=State.Recover;
            if(thrown)
            {
                // A braced hero throws it off: it lands on its side, open, a stride back the way it came.
                until=Time.time+After*ThrownShare;Pitch=-10;Roll=side*30;if(controller.enabled)controller.Move(-direction*1.2f);
                Sound("impactMetal_000",.24f,.8f);Debug.Log("LEAPER_THROWN "+def.id);Open(After*ThrownShare);
                return;
            }
            until=Time.time+After;Debug.Log("LEAPER_MISSED "+def.id);
        }
        void Release(string why)
        {
            if(state!=State.Pin)return;
            PassThrough(false);pinned=null;state=State.Recover;until=Time.time+After*.6f;Posing=false;Debug.Log($"LEAPER_FREED {def.id} {why}");
        }
        public override void Struck(DamagePacket packet,float amount)
        {
            if(amount<=0||packet.tag==Shatter)return;
            // Caught in the air it has nothing to brace on. Not from inside the blow that caught it: that hit is still being counted.
            if(state==State.Leap&&!shatter){shatter=true;shatterer=packet.source;shatterType=packet.type;return;}
            if(state==State.Pin)Release("struck");
        }
        public override void Present()
        {
            if(shatter&&health.Alive&&tune.heavy)
            {
                // Too crude to break: knocked out of the air and down where it is, open as long as one thrown off a guard, and up again.
                shatter=false;PassThrough(false);brain.Acting=false;brain.Rest(Cooldown);lane=null;state=State.Recover;KnockedDown=true;
                until=Time.time+After*ThrownShare;Posing=true;Pitch=-10;Roll=side*30;Open(After*ThrownShare);Scorch();
                Sound("impactMetal_000",.24f,.7f);Debug.Log("LEAPER_KNOCKED_DOWN "+def.id);
            }
            if(shatter&&health.Alive)
            {
                shatter=false;Debug.Log("LEAPER_SHATTERED "+def.id);
                CombatVfx.Burst(body.position+Vector3.up*(.6f+Arc*.5f),new Color(1.2f,.8f,.4f),"kill");
                health.Receive(new DamagePacket{source=shatterer,amount=health.maximum*4,type=shatterType,tag=Shatter});
            }
            if(visual==null)return;
            // The leap carries what's drawn up and down again; the capsule, its hit shape, stays on the floor so a blade at chest height still finds it.
            float k=state==State.Leap&&length>.01f?Mathf.Sin(Mathf.Clamp01(travelled/length)*Mathf.PI):0;
            visual.localPosition=rest+Vector3.up*(Arc*k);
        }
        public override void Cancel()
        {
            if(state==State.Pin)Release("cancelled");
            PassThrough(false);brain.Acting=false;if(state==State.Told||state==State.Leap)state=State.Prowl;lane=null;Posing=false;Roll=0;
        }
        public override void Fell(DamagePacket packet){if(state==State.Pin)Release("fell");PassThrough(false);if(visual!=null)visual.localPosition=rest;TakeUp();}
        public override void Dispose(){pinned=null;PassThrough(false);TakeUp();}
    }
}
