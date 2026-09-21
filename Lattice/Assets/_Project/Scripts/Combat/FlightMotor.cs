using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FlightMotor:MonoBehaviour,IMotor
    {
        public float acceleration=19,maxSpeed=12,drag=.75f,plane=1;
        CharacterController controller;CombatActor actor;
        Vector3 velocity;
        float contactAt,dashRemaining;
        Vector3 dashVelocity;
        public Vector3 Facing{get;private set;}=Vector3.forward;
        public Vector3 Velocity=>velocity;
        void Awake(){controller=GetComponent<CharacterController>();controller.minMoveDistance=0;actor=GetComponent<CombatActor>();}
        public static Vector3 Integrate(Vector3 velocity,Vector3 input,float dt,float acceleration,float drag,float max,bool brake)
        {return Vector3.ClampMagnitude((velocity+input*acceleration*dt)*Mathf.Exp(-(brake?9:drag)*dt),max);}
        public void Move(Vector2 input,bool boost,bool brake)
        {
            Vector3 thrust=Vector3.ClampMagnitude(new Vector3(input.x,0,input.y),1);float dt=actor!=null?actor.MotorDelta:Time.deltaTime;
            if(dashRemaining>0)
            {
                float step=Mathf.Min(dt,dashRemaining);dashRemaining-=step;
                velocity=dashVelocity; if(controller.enabled)controller.Move(dashVelocity*step+Vector3.up*(plane-transform.position.y));
                if(dashRemaining<=0)velocity=Vector3.ClampMagnitude(velocity,maxSpeed);
                return;
            }
            velocity=Integrate(velocity,thrust,dt,acceleration*(boost?2.1f:1),drag,maxSpeed*(boost?1.9f:1),brake);
            if(thrust.sqrMagnitude>.01f)Facing=thrust.normalized;
            if(Facing.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(Facing);
            if(controller.enabled)controller.Move(velocity*dt+Vector3.up*(plane-transform.position.y));
        }
        public void Dash(Vector3 direction,float distance){Facing=direction.normalized;dashVelocity=Facing*distance/.16f;dashRemaining=.16f;transform.rotation=Quaternion.LookRotation(Facing);}
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(!enabled||Mathf.Abs(hit.normal.y)>.5f||Time.unscaledTime-contactAt<.6f)return;
            contactAt=Time.unscaledTime;dashRemaining=0;velocity=Vector3.Reflect(velocity,hit.normal)*.4f+hit.normal*4;
            if(TryGetComponent<Health>(out var health))health.Receive(new DamagePacket{amount=12,type=DamageType.Kinetic});
        }
    }
}
