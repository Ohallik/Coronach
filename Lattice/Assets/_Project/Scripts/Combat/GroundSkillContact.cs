using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    // After pose/feet, before wrist VFX and melee contact. The clip owns both
    // action travel and release; interruption invalidates only unreleased work.
    [DefaultExecutionOrder(620)]
    public sealed class GroundSkillContact:MonoBehaviour
    {
        CombatActor actor;Animator rig;GroundMotor motor;
        GroundMove move;DamagePacket packet;Vector3 direction,destination;
        int sequence;bool pending;float travel;
        void Awake(){actor=GetComponent<CombatActor>();motor=GetComponent<GroundMotor>();}
        public void Begin(GroundMove definition,Vector3 aim,DamagePacket damage)
        {
            move=definition;direction=aim.normalized;packet=damage;sequence=actor.AttackSequence;
            rig=GetComponentInChildren<Animator>();pending=true;travel=0;
            float distance=actor.target!=null&&actor.target.Alive?Mathf.Min(6,Vector3.Distance(transform.position,actor.target.transform.position)):4;
            destination=transform.position+direction*distance;
        }
        void OnDisable(){pending=false;}
        void LateUpdate()
        {
            if(!pending)return;
            if(!actor.Health.Alive||actor.Recovering||actor.flight||sequence!=actor.AttackSequence||actor.State!=ActorState.Skill)
            {pending=false;return;}
            if(GameTime.Paused||rig==null||!rig.isActiveAndEnabled||!rig.isHuman)return;
            var state=rig.GetCurrentAnimatorStateInfo(0);
            if(rig.IsInTransition(0)&&rig.GetNextAnimatorStateInfo(0).IsName(move.clip))state=rig.GetNextAnimatorStateInfo(0);
            if(!state.IsName(move.clip))return;
            float phase=state.normalizedTime;
            if(move.clip=="Dash")
            {
                float progress=Mathf.InverseLerp(.25f,.75f,phase);
                motor.MoveAction(direction*(6*(progress-travel)),direction);travel=progress;
                if(phase>=move.contactEnd)pending=false;
                return;
            }
            if(phase<move.contactStart)return;
            pending=false;
            if(move.clip=="Pulse")
            {
                var hand=rig.GetBoneTransform(HumanBodyBones.RightHand);
                var body=transform.position+Vector3.up*.9f;
                // The animated hand can cross thin nearby cover between pose
                // samples. A release must not start beyond that obstruction.
                if(CombatCover.Sweep(body,hand.position,.12f,out var obstruction))
                {
                    var safe=body+(hand.position-body).normalized*Mathf.Max(0,obstruction.distance-.025f);
                    NetSeed.Land(safe,packet);CombatVfx.Burst(obstruction.point,Color.cyan,"hit");
                }
                else NetSeed.Throw(hand.position,destination,packet);
                AudioManager.Play("laserSmall_000",.16f);
            }
            else
            {
                PulseWave.Release(transform.position,packet);
                CombatVfx.Burst(rig.GetBoneTransform(HumanBodyBones.LeftHand).position,new Color(1,.65f,.2f),"shot");
                AudioManager.Play("forceField_000",.2f);
            }
        }
    }
}
