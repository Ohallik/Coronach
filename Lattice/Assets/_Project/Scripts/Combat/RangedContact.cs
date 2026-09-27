using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    // The Animator must finish posing before we read the generated emitter wrist.
    [DefaultExecutionOrder(700)]
    public sealed class RangedContact : MonoBehaviour
    {
        public enum Pattern { Needle, Fan, Lance }
        CombatActor actor;Animator rig;GeneratedAnimator driver;Health aimedTarget;EmitterBeam beam;
        Pattern pattern;ActorState actionState;
        GroundMove move;DamagePacket packet;Vector3 aim;
        int sequence;bool pending;
        void Awake(){actor=GetComponent<CombatActor>();}
        public void Begin(GroundMove definition,Vector3 direction,DamagePacket damage,Pattern shotPattern=Pattern.Needle)
        {
            move=definition;aim=direction;packet=damage;sequence=actor.AttackSequence;
            pattern=shotPattern;actionState=actor.State;
            rig=GetComponentInChildren<Animator>();driver=rig!=null?rig.GetComponentInParent<GeneratedAnimator>():null;pending=true;
            // Keep the selected target for this action, including its body height.
            // Free fire stays horizontal; acquiring another target during windup
            // must not redirect a shot behind the posed arm.
            aimedTarget=actor.target;
            if(aimedTarget!=null)
            {
                var offset=aimedTarget.transform.position-transform.position;offset.y=0;
                if(!aimedTarget.Alive||Vector3.Angle(aim,offset)>1)aimedTarget=null;
            }
        }
        void OnDisable(){pending=false;}
        void LateUpdate()
        {
            if(!pending)return;
            if(!actor.Health.Alive||actor.Recovering||actor.flight||sequence!=actor.AttackSequence||actor.State!=actionState)
            {pending=false;return;}
            if(GameTime.Paused||rig==null||!rig.isActiveAndEnabled||!rig.isHuman)return;
            int layer=driver!=null?driver.ActionLayer:0;
            var state=rig.GetCurrentAnimatorStateInfo(layer);
            if(rig.IsInTransition(layer)&&rig.GetNextAnimatorStateInfo(layer).IsName(move.clip))state=rig.GetNextAnimatorStateInfo(layer);
            if(!state.IsName(move.clip)||state.normalizedTime<move.contactStart)return;
            pending=false;
            var hand=rig.GetBoneTransform(HumanBodyBones.LeftHand);
            if(hand==null)return;
            var origin=hand.position+aim*.08f;
            var direction=aim;
            if(aimedTarget!=null&&aimedTarget.Alive)
            {
                var planar=aimedTarget.transform.position-transform.position;planar.y=0;
                if(Vector3.Angle(aim,planar)<30)
                {
                    var body=aimedTarget.GetComponent<CapsuleCollider>();
                    var point=body!=null?body.bounds.center:aimedTarget.transform.position+Vector3.up*.75f;
                    direction=(point-origin).normalized;
                }
            }
            AudioManager.Play("laserSmall_000",.18f);
            CombatVfx.Burst(origin,Color.cyan,"shot");
            if(pattern==Pattern.Lance)
            {
                if(beam==null)beam=gameObject.AddComponent<EmitterBeam>();
                beam.Fire(origin,direction,packet,13);
            }
            else if(pattern==Pattern.Fan)
                for(int ray=-2;ray<=2;ray++)Projectile.Fire(origin,Quaternion.AngleAxis(ray*12,Vector3.up)*direction,packet);
            else Projectile.Fire(origin,direction,packet);
        }
    }
}
