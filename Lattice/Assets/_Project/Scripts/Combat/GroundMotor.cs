using UnityEngine;
using Lattice.Core;
namespace Lattice.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GroundMotor:MonoBehaviour,IMotor
    {
        public enum TravelPhase {Idle,Starting,Travelling,Stopping,Turning}
        public TravelPhase Phase{get;private set;}
        CharacterController controller;CombatActor actor;
        public float speed=6.7f;
        /// <summary>Civil (Natural form) paces, D197. The owned walk is a 1.1 m/s stroll and the owned jog only reads as
        /// running near its own pace, so the old 2.6 m/s "walk" drove a stretched, crouched stroll and the 5.4 m/s sprint a
        /// slow-motion jog (Nathan, November 4: "walking or running through a swamp"). Full tilt is now a compact jog; a
        /// light tilt walks; sprint is the jog at its own stride.</summary>
        public const float NaturalWalk=1.5f,NaturalJog=4.5f,NaturalSprint=6.6f,WalkTilt=.5f;
        /// <summary>Heading change carried at speed (degrees per second). A sprint that turns keeps its pace instead of
        /// braking through a straight-line velocity blend; only a near-reversal stops and restarts.</summary>
        public const float CivilTurnRate=720,CombatTurnRate=900,ReversalAngle=150;
        public Vector3 Facing{get;private set;}=Vector3.forward;
        Vector3 measuredVelocity;
        Vector3 travelVelocity;
        int movedFrame=-2;
        public Vector3 DesiredVelocity{get;private set;}
        public Vector3 Velocity=>enabled&&!GameTime.Paused&&(actor==null||actor.Health.Alive)&&Time.frameCount<=movedFrame+1?measuredVelocity:Vector3.zero;
        Vector3 dash;float dashTime;
        void Awake(){controller=GetComponent<CharacterController>();controller.minMoveDistance=0;actor=GetComponent<CombatActor>();}
        void OnEnable(){measuredVelocity=DesiredVelocity=travelVelocity=Vector3.zero;dashTime=0;movedFrame=-2;Phase=TravelPhase.Idle;}
        public void Halt(){measuredVelocity=DesiredVelocity=travelVelocity=dash=Vector3.zero;dashTime=0;movedFrame=-2;Phase=TravelPhase.Idle;}
        public void Move(Vector2 input,bool boost,bool brake)
        {
            if(actor!=null&&(!actor.Health.Alive||actor.Recovering||actor.ChangingForm)){Halt();return;}
            Vector3 direction=new Vector3(input.x,0,input.y);if(direction.sqrMagnitude>1)direction.Normalize();
            Vector3 look=direction;
            if(actor!=null&&actor.TargetLocked&&actor.target!=null&&actor.target.Alive)
            {look=actor.target.transform.position-transform.position;look.y=0;}
            float dt=actor!=null?actor.MotorDelta:Time.deltaTime;
            if(!GameTime.Paused&&dt>0&&look.sqrMagnitude>.01f)
            {
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(look),900*dt);
                Facing=transform.forward;
            }
            bool natural=GetComponent<FormController>()?.Current==Lattice.Data.BodyForm.Natural;
            float tilt=direction.magnitude;
            // Civil movement: a light tilt walks; past half tilt the body jogs at a fixed pace (a digital pad or keys
            // always jog). Combat keeps proportional analog control.
            float pace=natural?(tilt<WalkTilt?NaturalWalk/WalkTilt:(boost?NaturalSprint:NaturalJog)/Mathf.Max(tilt,.001f)):speed*(boost?1.55f:1);
            if(actor!=null&&(actor.State==Lattice.Data.ActorState.Attack||actor.State==Lattice.Data.ActorState.Skill))pace*=.35f;
            if(actor!=null&&actor.State==Lattice.Data.ActorState.Guard)pace*=.5f;
            DesiredVelocity=direction*pace;
            if(Time.frameCount>movedFrame+1)travelVelocity=Vector3.zero;
            measuredVelocity=Vector3.zero;movedFrame=Time.frameCount;
            if(!controller.enabled||(actor!=null&&!actor.Health.Alive)){dashTime=0;DesiredVelocity=travelVelocity=Vector3.zero;Phase=TravelPhase.Idle;return;}
            if(dt<=0)return;
            bool dashing=dashTime>0;
            bool stopping=DesiredVelocity.sqrMagnitude<.0001f;
            // Short, physical start/braking steps keep first-frame response while
            // avoiding an instantaneous full-speed stride or a long release slide.
            float acceleration=natural?(stopping?36:32):(stopping?80:65);
            travelVelocity=Steer(travelVelocity,DesiredVelocity,acceleration*dt,(natural?CivilTurnRate:CombatTurnRate)*dt);
            Phase=stopping?(travelVelocity.sqrMagnitude>.0025f?TravelPhase.Stopping:TravelPhase.Idle):
                Vector3.Angle(transform.forward,direction)>12?TravelPhase.Turning:
                (travelVelocity-DesiredVelocity).sqrMagnitude>.04f?TravelPhase.Starting:TravelPhase.Travelling;
            Vector3 movement=travelVelocity;
            if(dashing){DesiredVelocity=movement=dash;travelVelocity=Vector3.zero;dashTime-=dt;}
            Vector3 before=transform.position;
            var collisions=controller.Move((movement+Vector3.down*7)*dt);
            var displacement=transform.position-before;displacement.y=0;
            // Retain sub-pixel acceleration between very short frames. Replacing
            // it with a quantized zero displacement could prevent a start at all.
            // On a wall, remove the blocked component so it cannot store momentum.
            if(!dashing&&(collisions&CollisionFlags.Sides)!=0)
                travelVelocity=Vector3.ClampMagnitude(displacement/dt,travelVelocity.magnitude);
            // Humanoid presentation uses the unscaled clock. Measure real planar
            // displacement after collision, so walls, slopes and Flash have the
            // same visual stride clock. Teleports outside Move do not become speed.
            // Do not impose a 1 ms denominator: uncapped/high-frequency frames
            // can be shorter, and that clamp under-reported real travel speed.
            measuredVelocity=Time.unscaledDeltaTime>0?displacement/Time.unscaledDeltaTime:Vector3.zero;
        }
        /// <summary>Velocity toward the request. While already moving, a turn swings the heading at the turn rate and changes
        /// pace at the ordinary rate, so a sprint keeps its speed around a corner; starting, stopping and near-reversals
        /// keep the original straight-line response (a short brake, then the new direction).</summary>
        public static Vector3 Steer(Vector3 current,Vector3 desired,float maxSpeedChange,float maxTurnDegrees)
        {
            float have=current.magnitude,want=desired.magnitude;
            if(have<.5f||want<.01f||Vector3.Angle(current,desired)>ReversalAngle)return Vector3.MoveTowards(current,desired,maxSpeedChange);
            var heading=Vector3.RotateTowards(current/have,desired/want,maxTurnDegrees*Mathf.Deg2Rad,0);
            return heading*Mathf.MoveTowards(have,want,maxSpeedChange);
        }
        public void Dash(Vector3 direction,float distance){dash=direction.normalized*distance/.16f;dashTime=.16f;Facing=direction.normalized;}
        /// <summary>For a dash that doesn't turn the hero: the aim stays on what she was looking at.</summary>
        public void KeepFacing(Vector3 facing){if(facing.sqrMagnitude>.01f)Facing=facing.normalized;}
        // Authored action travel is evaluated from the posed clip, then passed
        // through the same controller as ordinary movement. Never damage along
        // an intended path which collision prevented the body from travelling.
        public void MoveAction(Vector3 displacement,Vector3 heading)
        {
            if(GameTime.Paused||!enabled||!controller.enabled||!actor.Health.Alive||actor.ChangingForm)return;
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(heading),900*Time.unscaledDeltaTime);
            Facing=transform.forward;
            Vector3 before=transform.position;controller.Move(displacement);var actual=transform.position-before;actual.y=0;
            if(movedFrame!=Time.frameCount)measuredVelocity=Vector3.zero;
            movedFrame=Time.frameCount;
            if(Time.unscaledDeltaTime>0)measuredVelocity+=actual/Time.unscaledDeltaTime;
        }
    }
}
