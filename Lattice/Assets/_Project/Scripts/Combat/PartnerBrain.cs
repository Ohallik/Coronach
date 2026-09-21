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
            var party=PartyController.Current;if(party==null||party.Active==actor||!actor.Health.Alive||actor.motor==null||GameServices.Current.Input.Blocked)return;
            var active=party.Active;actor.target=active.target;
            if(actor.State==Lattice.Data.ActorState.Stagger){actor.motor.Move(Vector2.zero,false,true);return;}
            Vector3 goal=active.transform.position-active.motor.Facing*3+Vector3.right*2;
            bool fighting=ZoneController.Current!=null&&ZoneController.Current.Combat&&actor.target!=null&&actor.target.Alive;
            if(fighting&&actor.Health.integrity>actor.Health.maximum*.3f)goal=actor.target.transform.position;
            var d=goal-transform.position;
            actor.motor.Move(d.magnitude>(fighting?2.2f:1.5f)?new Vector2(d.x,d.z).normalized:Vector2.zero,false,true);
            if(fighting&&d.magnitude<(actor.flight||actor.character=="Sela"?14:3))actor.Attack();
        }
    }
}
