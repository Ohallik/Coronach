using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>Wrist energy uses the licensed particle material, attached to the generated skeleton.</summary>
    public sealed class HeroWeaponVfx:MonoBehaviour
    {
        CombatActor actor;FormController form;readonly LineRenderer[] blades=new LineRenderer[2];int sequence;
        void Start()
        {
            actor=GetComponent<CombatActor>();form=GetComponent<FormController>();var material=Resources.Load<Material>("Effects/flare_01");
            for(int i=0;i<2;i++)
            {
                var go=new GameObject("Wrist energy");go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();
                line.sharedMaterial=material;line.positionCount=3;line.useWorldSpace=true;line.widthCurve=new AnimationCurve(new Keyframe(0,.04f),new Keyframe(.3f,.11f),new Keyframe(1,0));line.numCapVertices=2;
                line.startColor=line.endColor=actor.character=="Taren"?new Color(1,.62f,.12f,.9f):new Color(.12f,.85f,1,.9f);line.enabled=false;blades[i]=line;
            }
        }
        void LateUpdate()
        {
            bool active=actor.Health.Alive&&form.Current==BodyForm.Shaped&&Lattice.Core.GameTime.Now<actor.VisualAttackUntil&&actor.VisualAction!="Dodge";
            var animator=active?form.shaped.GetComponentInChildren<Animator>():null;
            for(int i=0;i<2;i++)
            {
                var line=blades[i];line.enabled=active&&i==1&&actor.character=="Taren"&&animator!=null&&animator.isHuman;if(!line.enabled)continue;
                var hand=animator.GetBoneTransform(i==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);var arm=animator.GetBoneTransform(i==0?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
                var direction=(hand.position-arm.position).normalized;line.SetPosition(0,hand.position);line.SetPosition(1,hand.position+direction*.2f);line.SetPosition(2,hand.position+direction*.9f);
                if(sequence!=actor.AttackSequence)CombatVfx.Burst(hand.position,line.startColor,"lunge");
            }
            sequence=actor.AttackSequence;
        }
    }
}
