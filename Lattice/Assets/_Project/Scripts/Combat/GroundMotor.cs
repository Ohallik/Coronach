using UnityEngine;
using Lattice.Core;
namespace Lattice.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GroundMotor:MonoBehaviour,IMotor
    {
        CharacterController controller;CombatActor actor;
        public float speed=6.7f;
        public Vector3 Facing{get;private set;}=Vector3.forward;
        Vector3 measuredVelocity;
        int movedFrame=-2;
        public Vector3 DesiredVelocity{get;private set;}
        public Vector3 Velocity=>enabled&&!GameTime.Paused&&(actor==null||actor.Health.Alive)&&Time.frameCount<=movedFrame+1?measuredVelocity:Vector3.zero;
        Vector3 dash;float dashTime;
        void Awake(){controller=GetComponent<CharacterController>();controller.minMoveDistance=0;actor=GetComponent<CombatActor>();}
        void OnEnable(){measuredVelocity=DesiredVelocity=Vector3.zero;dashTime=0;movedFrame=-2;}
        public void Move(Vector2 input,bool boost,bool brake)
        {
            Vector3 direction=new Vector3(input.x,0,input.y);if(direction.sqrMagnitude>1)direction.Normalize();
            if(direction.sqrMagnitude>.01f){Facing=direction;transform.rotation=Quaternion.LookRotation(direction);}
            bool natural=GetComponent<FormController>()?.Current==Lattice.Data.BodyForm.Natural;
            float pace=natural?(boost?5.4f:2.6f):speed*(boost?1.55f:1);
            if(actor!=null&&(actor.State==Lattice.Data.ActorState.Attack||actor.State==Lattice.Data.ActorState.Skill))pace*=.35f;
            if(actor!=null&&actor.State==Lattice.Data.ActorState.Guard)pace*=.5f;
            DesiredVelocity=direction*pace;
            float dt=actor!=null?actor.MotorDelta:Time.deltaTime;
            measuredVelocity=Vector3.zero;movedFrame=Time.frameCount;
            if(!controller.enabled||(actor!=null&&!actor.Health.Alive)){dashTime=0;DesiredVelocity=Vector3.zero;return;}
            if(dt<=0)return;
            if(dashTime>0){DesiredVelocity=dash;dashTime-=dt;}
            Vector3 before=transform.position;
            controller.Move((DesiredVelocity+Vector3.down*7)*dt);
            var displacement=transform.position-before;displacement.y=0;
            // Humanoid presentation uses the unscaled clock. Measure real planar
            // displacement after collision, so walls, slopes and Flash have the
            // same visual stride clock. Teleports outside Move do not become speed.
            measuredVelocity=displacement/Mathf.Max(.001f,Time.unscaledDeltaTime);
        }
        public void Dash(Vector3 direction,float distance){dash=direction.normalized*distance/.16f;dashTime=.16f;Facing=direction.normalized;}
    }
}
