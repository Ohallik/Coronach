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
    public sealed class TerrainPauseTests
    {
        [TestCase("Taren",false)][TestCase("Sela",false)]
        [TestCase("Taren",true)][TestCase("Sela",true)]
        public void PauseHoldsTheRenderedPoseDuringTerrainAdaptation(string hero,bool shaped)
            =>Measure(hero,shaped,"Run",1);

        [TestCase("Taren",false)][TestCase("Sela",false)]
        [TestCase("Taren",true)][TestCase("Sela",true)]
        public void PauseHoldsAnActivelyBalancingUphillWalk(string hero,bool shaped)
            =>Measure(hero,shaped,"Walk",3);

        [TestCase("Taren",false)][TestCase("Sela",false)]
        [TestCase("Taren",true)][TestCase("Sela",true)]
        public void PauseHoldsAnActivelyBalancingUphillIdle(string hero,bool shaped)
            =>Measure(hero,shaped,"Idle",3);

        [TestCase("Taren",false)][TestCase("Sela",false)]
        [TestCase("Taren",true)][TestCase("Sela",true)]
        public void PauseHoldsTheRisingIdleSupport(string hero,bool shaped)
            =>Measure(hero,shaped,"Idle",30);

        static void Measure(string hero,bool shaped,string state,int slopeFrames)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(50,1,50);
            var root=new GameObject("terrain pause regression");root.transform.position=Vector3.up*.22f;
            var definition=GameCatalog.Find<CharacterDef>(hero);
            var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var profile=driver.strideProfile;Object.DestroyImmediate(driver);
            string take=state=="Idle"&&shaped?(hero=="Taren"?"CombatIdle":"RangedIdle"):state;
            var clip=rig.runtimeAnimatorController.animationClips.Single(c=>c.name==take);
            rig.runtimeAnimatorController=null;
            var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
            var graph=PlayableGraph.Create("terrain pause");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var pose=AnimationClipPlayable.Create(graph,clip);pose.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(pose);graph.Play();
            var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftHand,HumanBodyBones.RightHand}.Select(rig.GetBoneTransform).ToArray();
            const float dt=1f/60;
            void Evaluate(float phase,bool paused)
            {
                pose.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                feet.Correct(paused||state=="Idle"?Vector3.zero:Vector3.forward*(state=="Walk"?1.2f:shaped?6.7f:5.4f),state,phase,1,false,paused:paused,targetFacing:false,deltaTime:dt);
            }
            try
            {
                Physics.SyncTransforms();Evaluate(1.12f,false);
                floor.transform.rotation=Quaternion.Euler(-39,0,0);Physics.SyncTransforms();
                for(int frame=1;frame<=slopeFrames;frame++)Evaluate(1.12f+frame*.01f,false);
                var positions=bones.Select(b=>b.position).ToArray();var rotations=bones.Select(b=>b.rotation).ToArray();
                for(int frame=0;frame<30;frame++)
                {
                    Evaluate(1.12f+slopeFrames*.01f,true);
                    for(int i=0;i<bones.Length;i++)
                    {
                        Assert.Less(Vector3.Distance(positions[i],bones[i].position),.001f,bones[i].name+" moves while paused");
                        Assert.Less(Quaternion.Angle(rotations[i],bones[i].rotation),.1f,bones[i].name+" rotates while paused");
                    }
                }
            }
            finally{graph.Destroy();Object.DestroyImmediate(root);Object.DestroyImmediate(floor);}
        }
    }
}
