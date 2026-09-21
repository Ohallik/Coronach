using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class PartnerBrain:MonoBehaviour
    {
        CombatActor actor;
        void Awake(){actor=GetComponent<CombatActor>();}
        void Update()
        {
            var party=PartyController.Current;if(GameTime.Paused||party==null||party.Active==actor||!actor.Health.Alive||actor.motor==null||GameServices.Current.Input.Blocked)return;
            var active=party.Active;actor.target=active.target;
            if(actor.State==Lattice.Data.ActorState.Stagger){actor.motor.Move(Vector2.zero,false,true);return;}
            Vector3 goal=active.transform.position-active.motor.Facing*3+Vector3.right*2;
            bool fighting=ZoneController.Current!=null&&ZoneController.Current.Combat&&actor.target!=null&&actor.target.Alive;
            float separation=(active.transform.position-transform.position).magnitude;
            if(fighting&&separation<18&&actor.Health.integrity>actor.Health.maximum*.3f)goal=actor.target.transform.position;
            var d=goal-transform.position;
            float distance=d.magnitude,stop=fighting?2.2f:1.5f;
            // Permanent flight braking capped the partner near 2 m/s, leaving her
            // a whole encounter behind and making swap pull the camera backwards.
            actor.motor.Move(distance>stop?new Vector2(d.x,d.z).normalized:Vector2.zero,separation>10,actor.flight&&distance<stop+2);
            if(fighting&&(actor.target.transform.position-transform.position).magnitude<(actor.flight||actor.character=="Sela"?14:3))actor.Attack();
        }
    }
}
