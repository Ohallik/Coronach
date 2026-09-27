using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>Wrist energy uses the licensed particle material, attached to the generated skeleton.</summary>
    [DefaultExecutionOrder(650)]
    public sealed class HeroWeaponVfx:MonoBehaviour
    {
        CombatActor actor;FormController form;readonly LineRenderer[] blades=new LineRenderer[2];int sequence;
        Material bladeMaterial;
        public const float BladeLength=1.5f;
        void Start()
        {
            actor=GetComponent<CombatActor>();form=GetComponent<FormController>();
            bladeMaterial=new Material(Resources.Load<Material>("Effects/flare_01"));
            bladeMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);
            for(int i=0;i<2;i++)
            {
                var go=new GameObject("Wrist energy");go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();
                line.sharedMaterial=bladeMaterial;line.positionCount=3;line.useWorldSpace=true;line.widthCurve=new AnimationCurve(new Keyframe(0,.04f),new Keyframe(.3f,.08f),new Keyframe(1,0));line.numCapVertices=2;
                line.startColor=line.endColor=actor.character=="Taren"?new Color(1,.62f,.12f,.9f):new Color(.12f,.85f,1,.9f);line.enabled=false;blades[i]=line;
            }
        }
        public bool TryGetBlade(out Vector3 hand,out Vector3 tip)
        {
            var blade=blades[1];hand=tip=Vector3.zero;
            if(blade==null||!blade.enabled)return false;
            hand=blade.GetPosition(0);tip=blade.GetPosition(2);return true;
        }
        void OnDestroy(){if(bladeMaterial!=null)Destroy(bladeMaterial);}
        void LateUpdate()
        {
            bool active=actor.Health.Alive&&form.Current==BodyForm.Shaped&&Lattice.Core.GameTime.Now<actor.VisualAttackUntil&&actor.VisualAction!="Dodge";
            var animator=active?form.shaped.GetComponentInChildren<Animator>():null;
            for(int i=0;i<2;i++)
            {
                var line=blades[i];line.enabled=active&&i==1&&actor.character=="Taren"&&animator!=null&&animator.isHuman;if(!line.enabled)continue;
                var hand=animator.GetBoneTransform(i==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                // The retargeted wrist bends during the reverse cut. Its authored
                // longitudinal axis, rather than the elbow-to-wrist chord, owns
                // the visible edge. Length is visible, never hidden hit reach.
                var direction=hand.up;line.SetPosition(0,hand.position);line.SetPosition(1,hand.position+direction*.2f);line.SetPosition(2,hand.position+direction*BladeLength);
                if(sequence!=actor.AttackSequence)CombatVfx.Burst(hand.position,line.startColor,"lunge");
            }
            sequence=actor.AttackSequence;
        }
    }
}
