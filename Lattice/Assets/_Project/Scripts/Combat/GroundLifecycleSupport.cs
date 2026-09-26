using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    [DefaultExecutionOrder(550)]
    public sealed class GroundLifecycleSupport : MonoBehaviour
    {
        public AnimationCurve downSurface,reviveSurface;
        Animator animator;GeneratedVanes vanes;Transform heading;
        Vector3 origin;Quaternion rotation;
        void Awake()
        {animator=GetComponentInChildren<Animator>();vanes=GetComponent<GeneratedVanes>();heading=transform.parent;origin=transform.localPosition;rotation=transform.localRotation;}
        static float Phase(AnimatorStateInfo state)=>Mathf.Clamp01(state.normalizedTime);
        float Surface(AnimatorStateInfo state)=>state.IsName("Down")?downSurface.Evaluate(Phase(state)):
            state.IsName("Revive")?reviveSurface.Evaluate(Phase(state)):0;
        public static float VaneWeight(string state,float phase)=>state=="Down"?Mathf.Clamp01(phase/.25f):
            state=="Revive"?1-Mathf.Clamp01((phase-.55f)/.45f):0;
        static float VaneWeight(AnimatorStateInfo state)=>VaneWeight(state.IsName("Down")?"Down":state.IsName("Revive")?"Revive":"",Phase(state));
        void LateUpdate()
        {
            if(animator==null||heading==null||downSurface==null||reviveSurface==null)return;
            var state=animator.GetCurrentAnimatorStateInfo(0);float surface=Surface(state),fold=VaneWeight(state);
            bool supported=state.IsName("Down")||state.IsName("Revive");
            if(animator.IsInTransition(0))
            {
                var next=animator.GetNextAnimatorStateInfo(0);float blend=Mathf.Clamp01(animator.GetAnimatorTransitionInfo(0).normalizedTime);
                surface=Mathf.Lerp(surface,Surface(next),blend);fold=Mathf.Lerp(fold,VaneWeight(next),blend);
                supported|=next.IsName("Down")||next.IsName("Revive");
            }
            transform.localPosition=origin;transform.localRotation=rotation;
            Vector3 normal=Vector3.up;
            if(supported&&Physics.Raycast(heading.position+Vector3.up*1.5f,Vector3.down,out var ground,4,~0,QueryTriggerInteraction.Ignore))
            {
                normal=ground.normal;transform.rotation=Quaternion.FromToRotation(heading.up,normal)*transform.rotation;
                float baseClearance=Vector3.Dot(normal,transform.position-ground.point);
                transform.position+=normal*Mathf.Max(0,.012f-surface-baseClearance);
            }
            if(vanes!=null)vanes.SetDown(fold,normal);
        }
    }
}
