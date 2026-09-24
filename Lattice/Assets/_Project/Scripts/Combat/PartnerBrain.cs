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
            float distance=d.magnitude,stop=fighting?(actor.character=="Sela"&&!actor.flight?7.5f:2.2f):1.5f;
            // Permanent flight braking capped the partner near 2 m/s, leaving her
            // a whole encounter behind and making swap pull the camera backwards.
            actor.motor.Move(distance>stop?new Vector2(d.x,d.z).normalized:Vector2.zero,separation>10,actor.flight&&distance<stop+2);
            if(fighting)
            {
                var threat=actor.target.GetComponent<EnemyBrain>();var boss=actor.target.GetComponent<BossController>();
                var fromThreat=transform.position-actor.target.transform.position;fromThreat.y=0;
                bool incoming=boss!=null&&boss.Telegraphing&&boss.TelegraphRemaining<.12f&&fromThreat.magnitude<18||
                    threat!=null&&threat.Telegraphing&&threat.TelegraphRemaining<.12f&&fromThreat.magnitude<5;
                if(incoming)
                {
                    // Dodge across a committed charge, rather than chase it through
                    // another attack wind-up. The same defensive rules apply to AI.
                    var evade=Vector3.Cross(Vector3.up,fromThreat.normalized);
                    if(evade.sqrMagnitude<.01f)evade=Vector3.right;
                    actor.Dodge(evade);return;
                }
                if(fromThreat.magnitude<(actor.flight||actor.character=="Sela"?14:3))actor.Attack();
            }
        }
    }
}
