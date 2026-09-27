using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    // Flight contact follows the movement the controller actually completed.
    // The motor reports the final partial dash step as well as full steps.
    public sealed class FlightDashContact:MonoBehaviour
    {
        CombatActor actor;FlightMotor motor;DamagePacket packet;
        int actionSequence,dashSequence;bool pending;float effectTravel;
        readonly HashSet<Health> victims=new();
        readonly Collider[] overlaps=new Collider[128];
        void Awake()
        {actor=GetComponent<CombatActor>();motor=GetComponent<FlightMotor>();motor.DashMoved+=Resolve;}
        void OnDestroy(){if(motor!=null)motor.DashMoved-=Resolve;}
        void OnDisable(){pending=false;}
        public void Begin(DamagePacket damage)
        {
            packet=damage;actionSequence=actor.AttackSequence;dashSequence=motor.DashSequence;
            pending=true;effectTravel=0;victims.Clear();
        }
        void Resolve(int dash,Vector3 from,Vector3 to)
        {
            if(!pending)return;
            if(dash!=dashSequence||actor.AttackSequence!=actionSequence||!actor.flight||!actor.Health.Alive||actor.Recovering)
            {pending=false;return;}
            if(GameTime.Paused)return;
            Vector3 a=from+Vector3.up*.9f,b=to+Vector3.up*.9f,delta=b-a;
            int count=Physics.OverlapCapsuleNonAlloc(a,b,1.05f,overlaps,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                var collider=overlaps[i];
                if(!collider.TryGetComponent<Hurtbox>(out var box)||box.owner==null||!box.owner.Alive||
                    box.owner==actor.Health||box.owner.friendly==actor.Health.friendly||victims.Contains(box.owner))continue;
                Vector3 target=collider.ClosestPoint(b);
                float fraction=delta.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(target-a,delta)/delta.sqrMagnitude):0;
                if(!CombatCover.Clear(a+delta*fraction,target))continue;
                victims.Add(box.owner);box.Hit(packet);
            }
            // Emit only on travelled space, at distance intervals rather than
            // five frame-count samples which could advertise damage ahead.
            float distance=delta.magnitude;
            for(float sample=1-effectTravel;sample<=distance;sample+=1)
                CombatVfx.Burst(a+delta*(sample/distance),actor.character=="Taren"?new Color(1,.65f,.2f):Color.cyan,"lunge");
            effectTravel=(effectTravel+distance)%1;
        }
    }
}
