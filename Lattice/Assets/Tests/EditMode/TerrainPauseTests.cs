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
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(50,1,50);
            var root=new GameObject("terrain pause regression");root.transform.position=Vector3.up*.22f;
            var definition=GameCatalog.Find<CharacterDef>(hero);
            var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var profile=driver.strideProfile;Object.DestroyImmediate(driver);
            var clip=rig.runtimeAnimatorController.animationClips.Single(c=>c.name=="Run");
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
                feet.Correct(paused?Vector3.zero:Vector3.forward*(shaped?6.7f:5.4f),"Run",phase,1,false,paused:paused,targetFacing:false,deltaTime:dt);
            }
            try
            {
                Physics.SyncTransforms();Evaluate(1.12f,false);
                floor.transform.rotation=Quaternion.Euler(-39,0,0);Physics.SyncTransforms();Evaluate(1.13f,false);
                var positions=bones.Select(b=>b.position).ToArray();var rotations=bones.Select(b=>b.rotation).ToArray();
                for(int frame=0;frame<30;frame++)
                {
                    Evaluate(1.13f,true);
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
