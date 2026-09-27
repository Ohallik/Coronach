using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    [DefaultExecutionOrder(550)]
    public sealed class GroundLifecycleSupport : MonoBehaviour
    {
        public AnimationCurve downSurface,reviveSurface;
        public AnimationCurve idleSurface,walkSurface,attackSurface,staggerSurface;
        Animator animator;GeneratedVanes vanes;Transform heading;
        readonly RaycastHit[] supportHits=new RaycastHit[16];
        Vector3 origin;Quaternion rotation;
        void Awake()
        {animator=GetComponentInChildren<Animator>();vanes=GetComponent<GeneratedVanes>();heading=transform.parent;origin=transform.localPosition;rotation=transform.localRotation;}
        static float Phase(AnimatorStateInfo state)=>state.loop?Mathf.Repeat(state.normalizedTime,1):Mathf.Clamp01(state.normalizedTime);
        AnimationCurve Curve(AnimatorStateInfo state)=>state.IsName("Down")?downSurface:state.IsName("Revive")?reviveSurface:
            state.IsName("Idle")?idleSurface:state.IsName("Walk")?walkSurface:state.IsName("Attack")?attackSurface:
            state.IsName("Stagger")?staggerSurface:null;
        float Surface(AnimatorStateInfo state){var curve=Curve(state);return curve!=null&&curve.length>0?curve.Evaluate(Phase(state)):0;}
        bool Supported(AnimatorStateInfo state){var curve=Curve(state);return curve!=null&&curve.length>0;}
        public static float VaneWeight(string state,float phase)=>state=="Down"?Mathf.Clamp01(phase/.25f):
            state=="Revive"?1-Mathf.Clamp01((phase-.55f)/.45f):0;
        static float VaneWeight(AnimatorStateInfo state)=>VaneWeight(state.IsName("Down")?"Down":state.IsName("Revive")?"Revive":"",Phase(state));
        bool FindGround(out RaycastHit ground)
        {
            ground=default;float nearest=float.PositiveInfinity;
            int count=Physics.RaycastNonAlloc(heading.position+Vector3.up*1.5f,Vector3.down,supportHits,4,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=supportHits[i];
                if(hit.collider.transform.IsChildOf(heading)||hit.collider.GetComponentInParent<Health>()!=null||hit.distance>=nearest)continue;
                nearest=hit.distance;ground=hit;
            }
            return !float.IsInfinity(nearest);
        }
        void LateUpdate()
        {
            if(animator==null||heading==null||downSurface==null||reviveSurface==null)return;
            var state=animator.GetCurrentAnimatorStateInfo(0);float surface=Surface(state),fold=VaneWeight(state);
            bool supported=Supported(state);
            if(animator.IsInTransition(0))
            {
                var next=animator.GetNextAnimatorStateInfo(0);float blend=Mathf.Clamp01(animator.GetAnimatorTransitionInfo(0).normalizedTime);
                surface=Mathf.Lerp(surface,Surface(next),blend);fold=Mathf.Lerp(fold,VaneWeight(next),blend);
                supported|=Supported(next);
            }
            transform.localPosition=origin;transform.localRotation=rotation;
            Vector3 normal=Vector3.up;
            if(supported&&FindGround(out var ground))
            {
                normal=ground.normal;transform.rotation=Quaternion.FromToRotation(heading.up,normal)*transform.rotation;
                float baseClearance=Vector3.Dot(normal,transform.position-ground.point);
                transform.position+=normal*Mathf.Max(0,.012f-surface-baseClearance);
            }
            if(vanes!=null)vanes.SetDown(fold,normal);
        }
    }
}
