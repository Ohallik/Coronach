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
        Health flankTarget;
        float flankSide;
        void OnEnable(){corners=null;nextPath=0;flankTarget=null;}
        void Awake(){actor=GetComponent<CombatActor>();path=new NavMeshPath();}
        void Update()
        {
            var party=PartyController.Current;if(GameTime.Paused||party==null||party.Active==actor||!actor.Health.Alive||actor.Recovering||actor.motor==null||GameServices.Current.Input.Blocked)return;
            var active=party.Active;actor.target=active.target;
            if(actor.State==Lattice.Data.ActorState.Stagger){actor.motor.Move(Vector2.zero,false,true);return;}
            // Read incoming fire as a player does: roll across a shot about to hit.
            var shot=IncomingShot();if(shot.HasValue&&actor.Dodge(shot.Value))return;
            var followingRight=Vector3.Cross(Vector3.up,active.motor.Facing).normalized;
            Vector3 goal=active.transform.position-active.motor.Facing*3+followingRight*2;
            bool fighting=ZoneController.Current!=null&&ZoneController.Current.Combat&&actor.target!=null&&actor.target.Alive;
            float separation=(active.transform.position-transform.position).magnitude;
            bool flanking=false;
            if(fighting&&separation<18&&actor.Health.integrity>actor.Health.maximum*.3f)
            {
                goal=actor.target.transform.position;
                var carrier=actor.target.reactionOwner!=null?actor.target.reactionOwner:actor.target;
                if(actor.flight&&carrier.TryGetComponent<SerpentSegments>(out _))
                {
                    // A link lies inside a long body: chasing its centre sends
                    // the ship through the animal. Keep one side per selection.
                    var across=Planar(actor.target.transform.right).normalized;
                    if(flankTarget!=actor.target)
                    {flankTarget=actor.target;flankSide=Vector3.Dot(transform.position-goal,across)<0?-1:1;}
                    goal+=across*(6*flankSide);goal.y=transform.position.y;flanking=true;
                }
            }
            var d=goal-transform.position;
            // Ground melee closes to the actual wrist edge. The old 2.2 m
            // stand-off relied on the removed oversized invisible hit sphere.
            float distance=d.magnitude,stop=fighting?(actor.flight?2.2f:actor.character=="Sela"?7.5f:1.35f):1.5f;
            if(flanking)stop=.6f;
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
            if(flanking&&distance<stop+1&&actor.State!=Lattice.Data.ActorState.Dodge)
                GetComponent<FlightMotor>().FaceTarget(actor.target.transform.position-transform.position);
            if(fighting)
            {
                var opponent=actor.target.reactionOwner!=null?actor.target.reactionOwner:actor.target;
                var threat=opponent.GetComponent<EnemyBrain>();var boss=opponent.GetComponent<BossController>();
                var fromThreat=transform.position-opponent.transform.position;fromThreat.y=0;
                bool incoming=boss!=null&&boss.Telegraphing&&boss.TelegraphRemaining<.12f*Time.timeScale&&fromThreat.magnitude<18||
                    threat!=null&&threat.Telegraphing&&threat.TelegraphRemaining<.12f*Time.timeScale&&fromThreat.magnitude<5;
                if(incoming)
                {
                    // Dodge across a committed charge, rather than chase it through
                    // another attack wind-up. The same defensive rules apply to AI.
                    var evade=Vector3.Cross(Vector3.up,fromThreat.normalized);
                    if(evade.sqrMagnitude<.01f)evade=Vector3.right;
                    actor.Dodge(evade);return;
                }
                var fromTarget=Planar(actor.target.transform.position-transform.position);
                if(fromTarget.magnitude<(actor.flight||actor.character=="Sela"?14:3))actor.Attack();
            }
        }
        Vector3? IncomingShot()
        {
            float body=GetComponent<CharacterController>().radius+.45f;
            float motionScale=actor.flight?actor.MotorDelta/Mathf.Max(.000001f,Time.unscaledDeltaTime):1;
            foreach(var shot in Projectile.Live)
            {
                if(!shot.Threatens(actor.Health.friendly))continue;
                var v=Planar(shot.Velocity*Time.timeScale-actor.motor.Velocity*motionScale);if(v.sqrMagnitude<.01f)continue;
                var rel=Planar(transform.position-shot.transform.position);
                float t=Vector3.Dot(rel,v)/v.sqrMagnitude;if(t<0)continue;
                var miss=rel-v*t;if(miss.magnitude>body)continue;
                // Predict entry into the hull in real seconds. Flash slows a
                // shot; time to its centre would react after slow contact.
                t-=Mathf.Sqrt(Mathf.Max(0,body*body-miss.sqrMagnitude)/v.sqrMagnitude);
                if(t>.35f)continue;
                // Roll to whichever side the shot is already passing, or square across it.
                var across=Vector3.Cross(Vector3.up,v.normalized);
                return miss.sqrMagnitude>.01f&&Vector3.Dot(miss,across)<0?-across:across;
            }
            return null;
        }
        static Vector3 Planar(Vector3 value){value.y=0;return value;}
    }
}
