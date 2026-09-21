using UnityEngine;
namespace Lattice.Combat
{
    public sealed class GeneratedAnimator:MonoBehaviour
    {
        Animator animator;CombatActor actor;FormController form;Vector3 previous;string state;
        void Start(){animator=GetComponentInChildren<Animator>();actor=GetComponentInParent<CombatActor>();form=GetComponentInParent<FormController>();previous=transform.position;}
        void Update()
        {
            if(animator==null||!animator.isActiveAndEnabled)return;
            float speed=(transform.position-previous).magnitude/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;
            string wanted=actor!=null&&(actor.State==Lattice.Data.ActorState.Attack||actor.State==Lattice.Data.ActorState.Skill)?"Attack":speed>.1f?"Walk":"Idle";
            if(form!=null&&(form.Current==Lattice.Data.BodyForm.Flight||form.Current==Lattice.Data.BodyForm.CivilFlight))wanted="Idle";
            if(!animator.HasState(0,Animator.StringToHash(wanted)))wanted=speed>.1f?"Walk":"Idle";
            if(state==wanted)return;state=wanted;animator.CrossFadeInFixedTime(wanted,.1f);
        }
    }
}
