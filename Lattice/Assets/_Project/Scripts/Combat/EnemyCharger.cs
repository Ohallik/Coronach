using System.Collections.Generic;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The charger (the Thornback; later the Glassback, the Heat-scavenger): it squares up to a hero, marks a lane and runs it. The lane is the whole of the danger and ends where
    /// its run does, at a wall if one stands in it. Run into rock it is stuck fast and open for <c>tune.recover</c> seconds; run to the end of a lane that touched nobody it is
    /// winded for a short beat of the same; having hit someone it simply turns. So the answer is a sidestep, and the better answer is a sidestep with a wall behind you.
    /// Reads: attacks[0] (telegraph, range to start from, damage, cooldown), tune.reach (the run, 11 m), width (2.4), speed (15), recover (2.2), and linger/area/pace/effect/tick
    /// for what its run leaves where it ends (a Glassback's shed plates), nothing by default.
    /// </summary>
    public sealed class EnemyCharger:EnemyBehaviour
    {
        enum State{Stalk,Told,Run}
        State state;Vector3 direction;float travelled,length;Telegraph lane;bool hitSomeone;
        readonly HashSet<Health> met=new();
        float Reach=>Or(tune.reach,11);float Width=>Or(tune.width,2.4f);float Speed=>Or(tune.speed,15);float Stuck=>Or(tune.recover,2.2f);
        /// <summary>How much of the stuck time a clean miss costs it.</summary>
        public const float WindedShare=.45f;
        public override void Tick(CombatActor victim)
        {
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            switch(state)
            {
                case State.Stalk:
                    Posing=false;
                    if(OnATrack)
                    {
                        // A shard on a track runs where it was set facing, whoever is about, and never turns to anyone.
                        if(!brain.Rested)return;
                        direction=Flat(body.forward).sqrMagnitude>.01f?Flat(body.forward).normalized:Vector3.forward;
                        length=Wall(direction,Reach,out float ahead)?Mathf.Max(2,ahead+controller.radius):Reach;
                        Mark(Telegraph.Line(body.position,direction,length+controller.radius,Width,Tell,Telegraph.Heavy,true),Tell,direction,victim,"spaceEngineLow_000",1.3f);
                        state=State.Told;return;
                    }
                    if(distance<Range&&brain.Rested&&brain.Sees(victim))
                    {
                        direction=distance>.01f?to/distance:body.forward;body.rotation=Quaternion.LookRotation(direction);
                        // The lane ends where the run will: at the wall, if there is one in it. A hero reads the bait before he takes it.
                        length=Wall(direction,Reach,out float gap)?Mathf.Max(2,gap+controller.radius):Reach;
                        Mark(Telegraph.Line(body.position,direction,length+controller.radius,Width,Tell,Telegraph.Heavy,true),Tell,direction,victim,"spaceEngineLow_000",1.6f);
                        state=State.Told;return;
                    }
                    brain.Spread(to,distance,Range);
                    // Squaring up is slow: it turns like the heavy thing it is, and closes only to where its run starts.
                    Face(to,160);
                    if(distance>Range*.75f||!brain.Rested&&distance>Range*.4f)brain.Walk(to,def.speed);
                    break;
                case State.Told:
                    if(!brain.TellRunOut)return;
                    lane=brain.EndTell(.2f);if(lane!=null)lane.Release();
                    state=State.Run;travelled=0;hitSomeone=false;met.Clear();brain.Acting=true;PassThrough(true);
                    Posing=true;Pitch=10;Sound("thrusterFire_000",.2f,.7f);
                    break;
                case State.Run:
                    float step=Mathf.Min(Speed*Time.deltaTime,Reach-travelled);
                    if(Wall(direction,step+.35f,out _)){End(true);return;}
                    var before=body.position;if(controller.enabled)controller.Move(direction*step);
                    float moved=Flat(body.position-before).magnitude;travelled+=Mathf.Max(moved,step*.25f);
                    if(Touch(met,controller.radius+.75f,brain.Blow(attack,HitWeight.Heavy),lane)>0)hitSomeone=true;
                    // Held by something the cast didn't see (a body, a lip of ground): it has stopped, and that is a wall to it.
                    if(moved<step*.2f&&travelled>1){End(true);return;}
                    if(travelled>=Reach||travelled>=length+controller.radius)End(false);
                    break;
            }
        }
        /// <summary>A shard on a marked track (chapter 7's Moonshard, calved off a moon's leading face): <c>tune.count</c> over nought runs its own heading that many times, starting as it
        /// wakes, and is spent after the last run: it breaks up where it stops and pays nothing, as a mine gone off by itself doesn't. Shot down first, it is a kill like any other.</summary>
        bool OnATrack=>tune.count>0;int runs;
        /// <summary>A shard runs its track wherever the party is: it isn't walked home.</summary>
        public override bool Roams=>!OnATrack;
        void End(bool wall)
        {
            PassThrough(false);brain.Acting=false;state=State.Stalk;brain.Rest(Cooldown);
            if(OnATrack&&++runs>=tune.count)
            {
                CombatVfx.Burst(body.position+Vector3.up*.5f,new Color(.9f,.9f,.86f),"kill");Sound("explosionCrunch_000",.16f,1.6f);Debug.Log("CHARGER_SPENT "+def.id);
                health.Receive(new DamagePacket{amount=health.maximum*3,type=DamageType.Pulse,tag=EnemyBrain.SelfDetonation});return;
            }
            if(tune.linger>0)LingeringArea.Lay(body.position,Or(tune.area,2.2f),tune.linger,tune.effect,Or(tune.pace,.6f),tune.tick,health,attack!=null?attack.type:DamageType.Kinetic);
            // What it left is the party's to see said once (a Glassback's shed plates: "Watch the floor").
            if(tune.linger>0&&!string.IsNullOrEmpty(tune.bark)&&PartyController.Current!=null)Lattice.Core.BarkService.Play(PartyController.Current.Active.character,tune.bark);
            if(wall)
            {
                // Its head is in the wall: nose down, and open for as long as it takes to pull free.
                Pitch=18;Sound("impactMetal_000",.3f,.7f);CombatVfx.Burst(Chest+direction*controller.radius,new Color(.72f,.6f,.44f),"kill");
                Debug.Log("CHARGER_STUCK "+def.id);Open(Stuck);
            }
            else if(!hitSomeone){Pitch=6;Debug.Log("CHARGER_WINDED "+def.id);Open(Stuck*WindedShare);}
            else Posing=false;
        }
        public override void Cancel(){PassThrough(false);state=State.Stalk;lane=null;}
        public override void Dispose()=>PassThrough(false);
    }
}
