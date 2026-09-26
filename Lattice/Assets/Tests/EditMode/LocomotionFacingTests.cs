using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.Tests.EditMode
{
    public sealed class LocomotionFacingTests
    {
        [TestCase("Taren",false)][TestCase("Taren",true)]
        [TestCase("Sela",false)][TestCase("Sela",true)]
        public void VisibleHipAndShoulderSpansFaceFreeTravel(string hero,bool shaped)
        {
            var definition=GameCatalog.Find<CharacterDef>(hero);
            Vector3 travel=Quaternion.Euler(0,73,0)*Vector3.forward;
            foreach(string state in new[]{"Walk","Run","Sprint"})
            {
                var body=Object.Instantiate(shaped?definition.shaped:definition.natural);
                body.transform.rotation=Quaternion.LookRotation(travel);
                var driver=body.GetComponent<GeneratedAnimator>();if(driver!=null)Object.DestroyImmediate(driver);
                var animator=body.GetComponentInChildren<Animator>();
                var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name==state);
                animator.runtimeAnimatorController=null;
                var graph=PlayableGraph.Create("locomotion anatomical facing test");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable=AnimationClipPlayable.Create(graph,clip);
                AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(playable);graph.Play();
                float pelvisSum=0,chestSum=0,pelvisPeak=0;
                try
                {
                    for(int frame=0;frame<60;frame++)
                    {
                        playable.SetTime(clip.length*frame/60d);graph.Evaluate(0);
                        float hips=Angle(animator,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,travel);
                        float chest=Angle(animator,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,travel);
                        pelvisSum+=hips;chestSum+=chest;pelvisPeak=Mathf.Max(pelvisPeak,Mathf.Abs(hips));
                    }
                }
                finally {graph.Destroy();Object.DestroyImmediate(body);}
                Assert.Less(Mathf.Abs(pelvisSum/60),10,$"{hero}/{shaped}/{state}: cycle mean hips must face independent travel, not the actor's own transform");
                Assert.Less(Mathf.Abs(chestSum/60),15,$"{hero}/{shaped}/{state}: cycle mean shoulders face travel");
                Assert.Less(pelvisPeak,10,$"{hero}/{shaped}/{state}: reject large cyclic hip excursions as well as a biased mean");
            }
        }
        static float Angle(Animator animator,HumanBodyBones left,HumanBodyBones right,Vector3 travel)
        {
            var l=animator.GetBoneTransform(left);var r=animator.GetBoneTransform(right);
            Assert.IsNotNull(l);Assert.IsNotNull(r);
            var forward=Vector3.Cross(r.position-l.position,Vector3.up);forward.y=0;
            Assert.Greater(forward.sqrMagnitude,.001f,"bone span must be measurable");
            return Vector3.SignedAngle(travel,forward,Vector3.up);
        }
    }
}
