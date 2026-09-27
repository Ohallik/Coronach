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
        int thrustFrame=-1;float engineDrive;
        // Sampled after the input/AI update. Momentum without a command is not thrust.
        public float EngineDrive=>enabled&&thrustFrame==Time.frameCount?engineDrive:0;
        public int DashSequence{get;private set;}
        public event System.Action<int,Vector3,Vector3> DashMoved;
        public Vector3 Facing{get;private set;}=Vector3.forward;
        public Vector3 Velocity=>Core.GameTime.Paused||actor!=null&&(!actor.Health.Alive||actor.Recovering)?Vector3.zero:velocity;
        public void Halt(){velocity=dashVelocity=Vector3.zero;dashRemaining=0;engineDrive=0;thrustFrame=-1;}
        void OnDisable(){Halt();}
        void Awake(){controller=GetComponent<CharacterController>();controller.minMoveDistance=0;actor=GetComponent<CombatActor>();}
        public static Vector3 Integrate(Vector3 velocity,Vector3 input,float dt,float acceleration,float drag,float max,bool brake)
        {
            if(dt<=0)return velocity;
            float damping=brake?9:Mathf.Max(0,drag),decay=Mathf.Exp(-damping*dt);
            // Integrate constant thrust with drag, rather than applying all
            // thrust at the beginning of the frame and damping it immediately.
            float thrustTime=damping>.0001f?(1-decay)/damping:dt;
            return Vector3.ClampMagnitude(velocity*decay+input*(acceleration*thrustTime),max);
        }
        public static Vector3 Advance(ref Vector3 velocity,Vector3 input,float dt,float acceleration,float drag,float max,bool brake)
        {
            if(dt<=0)return Vector3.zero;
            // Bound the speed-cap/turn integration interval independently of
            // render rate. Average velocity preserves travel during braking;
            // using only the end velocity shortened every slower frame.
            int steps=Mathf.Max(1,Mathf.CeilToInt(dt*120));float step=dt/steps;
            Vector3 displacement=Vector3.zero;
            for(int i=0;i<steps;i++)
            {
                Vector3 before=velocity;velocity=Integrate(velocity,input,step,acceleration,drag,max,brake);
                displacement+=(before+velocity)*(.5f*step);
            }
            return displacement;
        }
        public void Move(Vector2 input,bool boost,bool brake)
        {
            if(actor!=null&&(!actor.Health.Alive||actor.Recovering)){Halt();return;}
            if(Core.GameTime.Paused)return;
            Vector3 thrust=Vector3.ClampMagnitude(new Vector3(input.x,0,input.y),1);float dt=actor!=null?actor.MotorDelta:Time.deltaTime;
            thrustFrame=Time.frameCount;engineDrive=thrust.magnitude*(boost?2:1);
            if(dashRemaining>0)
            {
                engineDrive=2.3f;
                float step=Mathf.Min(dt,dashRemaining);dashRemaining-=step;
                velocity=dashVelocity;Vector3 before=transform.position;
                if(controller.enabled&&step>0)
                {
                    controller.Move(dashVelocity*step+Vector3.up*(plane-transform.position.y));
                    DashMoved?.Invoke(DashSequence,before,transform.position);
                }
                if(dashRemaining<=0)velocity=Vector3.ClampMagnitude(velocity,maxSpeed);
                return;
            }
            Vector3 displacement=Advance(ref velocity,thrust,dt,acceleration*(boost?2.1f:1),drag,maxSpeed*(boost?1.9f:1),brake);
            if(thrust.sqrMagnitude>.01f)Facing=thrust.normalized;
            if(Facing.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(Facing);
            if(controller.enabled)controller.Move(displacement+Vector3.up*(plane-transform.position.y));
        }
        public void Dash(Vector3 direction,float distance){DashSequence++;Facing=direction.normalized;dashVelocity=Facing*distance/.16f;dashRemaining=.16f;thrustFrame=Time.frameCount;engineDrive=2.3f;transform.rotation=Quaternion.LookRotation(Facing);}
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Combatants resolve damage through their attacks. Brushing a partner or a
            // lunge target must not count as hitting the tunnel wall.
            if(!enabled||Mathf.Abs(hit.normal.y)>.5f||hit.collider.GetComponentInParent<Health>()!=null||Time.unscaledTime-contactAt<.6f)return;
            contactAt=Time.unscaledTime;dashRemaining=0;velocity=Vector3.Reflect(velocity,hit.normal)*.4f+hit.normal*4;
            if(Lattice.Core.ZoneController.Current!=null&&Lattice.Core.ZoneController.Current.Combat&&TryGetComponent<Health>(out var health))health.Receive(new DamagePacket{amount=12,type=DamageType.Kinetic});
        }
    }
}
