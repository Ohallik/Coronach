using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The burster (the Spore Pod; later anything that drifts in a lane and goes off): a hazard, not a hunter. It drifts on a slow loop where it was set and does nothing until something
    /// sets it off: a hero coming inside <c>tune.wake</c> (rammed, or flown too close), or a blow that doesn't kill it. Then it swells on a marked circle for its telegraph and bursts: it
    /// strikes who is still in the circle and leaves what it was full of on the spot (<c>tune.effect</c> for <c>tune.linger</c> seconds: the Spore Pod's is a Blind cloud that hides the
    /// markings under it, <see cref="LingeringArea"/>). Shot dead before it swells, it bursts at once into its cloud and strikes nobody: the cloud is the price of clearing a lane from
    /// range, and it stands where the pod was, not on the ship. A pod that went off by itself pays nothing, as a mine doesn't.
    /// Reads: attacks[0] (telegraph is the swell, radius of the blast, damage), tune.wake (how close sets it off, the attack's range by default), linger, area (the cloud's radius, the
    /// blast's by default), effect, pace (a Slow's share), tick (a Burn's damage), reach (the radius of its drift, none by default).
    /// Give it the Mine archetype: it holds its ground against a hook and a shove.
    /// </summary>
    public sealed class EnemyBurster:EnemyBehaviour
    {
        bool set,burst;float drift;
        float Wake=>Or(tune.wake,Range);float Blast=>attack!=null&&attack.radius>0?attack.radius:3;
        public override bool Movable=>false;
        public override bool Roams=>false;
        /// <summary>Set off and swelling: what a check reads.</summary>
        public bool Swelling=>set&&brain.Telegraphing;
        protected override void Begin()
        {
            drift=brain.GetInstanceID()*.37f;
            // Drawn round its capsule: a model whose origin isn't at its middle (the Spore Pod's is at the tip of its stalk) is moved inside what's drawn, so the pod a player sees
            // hangs where its capsule and its marked circle are. The model's own parts move, not the body that poses and bobs it.
            if(visual==null||controller==null)return;
            var drawn=visual.GetComponentsInChildren<Renderer>();if(drawn.Length==0)return;
            var shown=drawn[0].bounds;foreach(var part in drawn)shown.Encapsulate(part.bounds);
            var off=body.position+controller.center-shown.center;if(off.sqrMagnitude<.0025f)return;
            bool onRoot=visual.GetComponent<Renderer>()!=null;
            if(onRoot)visual.position+=off;else foreach(Transform child in visual)child.position+=off;
        }
        /// <summary>Where what's drawn of it is centred, for a check.</summary>
        public Vector3 DrawnMiddle{get{var drawn=visual!=null?visual.GetComponentsInChildren<Renderer>():new Renderer[0];if(drawn.Length==0)return body.position;var b=drawn[0].bounds;foreach(var part in drawn)b.Encapsulate(part.bounds);return b.center;}}
        public override void Tick(CombatActor victim)
        {
            if(brain.Telegraphing)
            {
                float k=brain.TellProgress;Posing=true;Swell=.35f*k;Pitch=4*Mathf.Sin(Time.time*40)*k;
                if(!brain.TellRunOut)return;
                var mark=brain.EndTell(.2f);
                if(mark!=null){StrikeIn(mark,brain.Blow(attack,HitWeight.Medium),Chest);mark.Release();}
                Pop();
                return;
            }
            // A slow loop on the current, so a lane of them is never the same lane twice.
            float loop=tune.reach;
            if(loop>0)
            {
                drift+=Time.deltaTime*Mathf.Max(.1f,def.speed)/loop;
                var on=brain.Home+new Vector3(Mathf.Cos(drift),0,Mathf.Sin(drift))*loop;var way=Flat(on-body.position);
                if(way.sqrMagnitude>.0025f)brain.Walk(way,Mathf.Max(.1f,def.speed));
            }
            if(PartyController.Current==null)return;
            foreach(var hero in PartyController.Current.members)
                if(hero.Health.Alive&&Flat(hero.transform.position-body.position).magnitude<Wake){SetOff(hero);return;}
        }
        void SetOff(CombatActor by)
        {
            if(set||burst||!health.Alive)return;set=true;
            var toward=by!=null?Flat(by.transform.position-body.position):body.forward;
            Mark(Telegraph.Circle(body.position,Blast,Tell,Telegraph.Threat,true),Tell,toward,by,"forceField_000",.55f);
            Debug.Log("BURSTER_SET "+def.id);
        }
        /// <summary>Gone off: its cloud where it hung, and the body with it. Its own blast is no kill anyone made.</summary>
        void Pop()
        {
            if(burst)return;burst=true;Lay();
            health.Receive(new DamagePacket{amount=health.maximum*3,type=DamageType.Pulse,tag=EnemyBrain.SelfDetonation});
        }
        void Lay()
        {
            var at=body.position;Debug.Log("BURSTER_BURST "+def.id);
            CombatVfx.Burst(at+Vector3.up*.5f,tune.tint.a>0?tune.tint:new Color(.8f,.95f,.7f,.85f),"kill");Sound("explosionCrunch_000",.16f,1.4f);
            if(tune.linger>0)LingeringArea.Lay(at,Or(tune.area,Blast),tune.linger,tune.effect,Or(tune.pace,.6f),tune.tick,health,attack!=null?attack.type:DamageType.Pulse);
        }
        public override void Struck(DamagePacket packet,float amount)
        {
            // A shot that didn't finish it sets it off: it swells where it hangs. Not a pod held for a scene: it hangs there as it was put.
            if(amount>0&&!brain.Telegraphing&&!brain.Passive)
            {
                CombatActor by=null;if(packet.source!=null)packet.source.TryGetComponent(out by);
                SetOff(by!=null?by:PartyController.Current!=null?PartyController.Current.Active:null);
            }
        }
        public override void Cancel(){set=false;Posing=false;Swell=0;}
        public override void Fell(DamagePacket packet){if(!burst){burst=true;Lay();}}
    }
}
