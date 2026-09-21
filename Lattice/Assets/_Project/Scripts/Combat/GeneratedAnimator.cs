using UnityEngine;
namespace Lattice.Combat
{
    public sealed class GeneratedAnimator:MonoBehaviour
    {
        Animator animator;CombatActor actor;EnemyBrain enemy;FormController form;Vector3 previous;string state;int attackSequence;
        void Start(){animator=GetComponentInChildren<Animator>();actor=GetComponentInParent<CombatActor>();enemy=GetComponentInParent<EnemyBrain>();form=GetComponentInParent<FormController>();previous=transform.position;}
        void Update()
        {
            if(animator==null||!animator.isActiveAndEnabled)return;
            float speed=(transform.position-previous).magnitude/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;
            string wanted=actor!=null&&(Time.time<actor.VisualAttackUntil||actor.State==Lattice.Data.ActorState.Skill)?"Attack":speed>.1f?"Walk":"Idle";
            if(enemy!=null&&enemy.Attacking)wanted="Attack";
            if(form!=null&&(form.Current==Lattice.Data.BodyForm.Flight||form.Current==Lattice.Data.BodyForm.CivilFlight))wanted="Idle";
            if(!animator.HasState(0,Animator.StringToHash(wanted)))wanted=speed>.1f?"Walk":"Idle";
            bool nextStrike=actor!=null&&wanted=="Attack"&&attackSequence!=actor.AttackSequence;
            if(state==wanted&&!nextStrike)return;if(actor!=null)attackSequence=actor.AttackSequence;
            state=wanted;animator.speed=wanted=="Attack"&&actor!=null?1.7f:1;animator.CrossFadeInFixedTime(wanted,wanted=="Attack"?.035f:.1f);
        }
    }
}
