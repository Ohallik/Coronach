using UnityEngine;
namespace Lattice.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GroundMotor:MonoBehaviour,IMotor
    {
        CharacterController controller;CombatActor actor;
        public float speed=6.7f;
        public Vector3 Facing{get;private set;}=Vector3.forward;
        public Vector3 Velocity{get;private set;}
        Vector3 dash;float dashTime;
        void Awake(){controller=GetComponent<CharacterController>();controller.minMoveDistance=0;actor=GetComponent<CombatActor>();}
        public void Move(Vector2 input,bool boost,bool brake)
        {
            Vector3 direction=new Vector3(input.x,0,input.y);if(direction.sqrMagnitude>1)direction.Normalize();
            if(direction.sqrMagnitude>.01f){Facing=direction;transform.rotation=Quaternion.LookRotation(direction);}
            bool natural=GetComponent<FormController>()?.Current==Lattice.Data.BodyForm.Natural;
            float pace=natural?(boost?5.4f:2.6f):speed*(boost?1.55f:1);
            if(actor!=null&&(actor.State==Lattice.Data.ActorState.Attack||actor.State==Lattice.Data.ActorState.Skill))pace*=.35f;
            if(actor!=null&&actor.State==Lattice.Data.ActorState.Guard)pace*=.5f;
            Velocity=direction*pace;
            float dt=actor!=null?actor.MotorDelta:Time.deltaTime;
            if(dashTime>0){Velocity=dash;dashTime-=dt;}
            if(controller.enabled)controller.Move((Velocity+Vector3.down*7)*dt);
        }
        public void Dash(Vector3 direction,float distance){dash=direction.normalized*distance/.16f;dashTime=.16f;Facing=direction.normalized;}
    }
}
