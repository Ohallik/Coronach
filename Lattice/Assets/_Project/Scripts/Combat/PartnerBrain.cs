using Lattice.Core;
using UnityEngine;
using UnityEngine.AI;
namespace Lattice.Combat
{
    public sealed class PartnerBrain:MonoBehaviour
    {
        CombatActor actor;
        NavMeshPath path;
        Vector3[] corners;
        int corner;
        float nextPath;
        void OnEnable(){corners=null;nextPath=0;}
        void Awake(){actor=GetComponent<CombatActor>();path=new NavMeshPath();}
        void Update()
        {
            var party=PartyController.Current;if(GameTime.Paused||party==null||party.Active==actor||!actor.Health.Alive||actor.motor==null||GameServices.Current.Input.Blocked)return;
            var active=party.Active;actor.target=active.target;
            if(actor.State==Lattice.Data.ActorState.Stagger){actor.motor.Move(Vector2.zero,false,true);return;}
            var followingRight=Vector3.Cross(Vector3.up,active.motor.Facing).normalized;
            Vector3 goal=active.transform.position-active.motor.Facing*3+followingRight*2;
            bool fighting=ZoneController.Current!=null&&ZoneController.Current.Combat&&actor.target!=null&&actor.target.Alive;
            float separation=(active.transform.position-transform.position).magnitude;
            if(fighting&&separation<18&&actor.Health.integrity>actor.Health.maximum*.3f)goal=actor.target.transform.position;
            var d=goal-transform.position;
            float distance=d.magnitude,stop=fighting?(actor.character=="Sela"&&!actor.flight?7.5f:2.2f):1.5f;
            if(!actor.flight&&GroundNavigation.Current!=null&&!fighting)
            {
                if(Time.unscaledTime>=nextPath)
                {
                    nextPath=Time.unscaledTime+.3f;
                    corners=GroundNavigation.Current.FindPath(transform.position,goal,active.transform.position,path)?path.corners:null;
                    corner=1;
                }
                if(corners!=null&&corners.Length>1)
                {
                    while(corner<corners.Length-1&&Planar(corners[corner]-transform.position).magnitude<.35f)corner++;
                    d=Planar(corners[corner]-transform.position);
                    // Do not stop at the formation radius before traversing a doorway.
                    stop=corner<corners.Length-1?.15f:1.2f;distance=d.magnitude;
                }
                else {d=Vector3.zero;distance=0;}
            }
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
        static Vector3 Planar(Vector3 value){value.y=0;return value;}
    }
}
