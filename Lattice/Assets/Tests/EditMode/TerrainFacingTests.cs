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
    public sealed class TerrainFacingTests
    {
        [TestCase("Taren",false)][TestCase("Sela",false)]
        [TestCase("Taren",true)][TestCase("Sela",true)]
        public void SteepBodyFacesPlanarTravelWithoutYawingDownhill(string hero,bool shaped)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(500,1,500);
            var root=new GameObject("terrain facing matrix");
            var definition=GameCatalog.Find<CharacterDef>(hero);
            var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var profile=driver.strideProfile;Object.DestroyImmediate(driver);
            string state=shaped?"Run":"Walk";float speed=shaped?6.7f:2.6f;
            var clip=rig.runtimeAnimatorController.animationClips.Single(c=>c.name==state);
            rig.runtimeAnimatorController=null;
            var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
            var graph=PlayableGraph.Create("terrain facing");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var pose=AnimationClipPlayable.Create(graph,clip);pose.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(pose);graph.Play();
            const float dt=1f/120;float maximum=0;int samples=0;
            try
            {
                foreach(float slope in new[]{39f,-39f})
                {
                    floor.transform.rotation=Quaternion.Euler(-slope,0,0);
                    var normal=floor.transform.up;var surface=floor.transform.position+normal*.5f;Physics.SyncTransforms();
                    foreach(float angle in new[]{0f,45f,90f,135f,180f,225f,270f,315f})
                    {
                        var travel=Quaternion.Euler(0,angle,0)*Vector3.forward;
                        root.transform.rotation=Quaternion.LookRotation(travel);feet.ResetContacts();
                        float grade=Mathf.Abs(Vector3.Dot(normal,travel))/normal.y;
                        float rate=profile.Cadence(state,speed,grade),stride=profile.Stride(state,speed,rate,grade);
                        for(int frame=0;frame<Mathf.CeilToInt(clip.length/rate*3/dt);frame++)
                        {
                            float time=frame*dt,phase=time*rate/clip.length;
                            var position=travel*speed*time;position.y=Vector3.Dot(normal,surface-position)/normal.y+.22f;root.transform.position=position;
                            pose.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                            feet.Correct(travel*speed,state,phase,stride,false,targetFacing:false,deltaTime:dt);
                            if(phase<1)continue;
                            var span=rig.GetBoneTransform(HumanBodyBones.RightUpperLeg).position-rig.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
                            var visible=Vector3.Cross(span,Vector3.up);visible.y=0;
                            maximum=Mathf.Max(maximum,Vector3.Angle(visible,travel));samples++;
                        }
                    }
                }
                Debug.Log($"TERRAIN_FACING {hero}/{shaped} samples={samples} maximum={maximum:F3}");
                Assert.Greater(samples,100);
                Assert.Less(maximum,10,"terrain alignment must preserve the body's measured travel heading");
            }
            finally{graph.Destroy();Object.DestroyImmediate(root);Object.DestroyImmediate(floor);}
        }

        [TestCase("Taren",false)][TestCase("Sela",false)]
        [TestCase("Taren",true)][TestCase("Sela",true)]
        public void MidStrideContactRedirectionKeepsTheBodyFacingActualTravel(string hero,bool shaped)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(100,1,100);
            var root=new GameObject("mid-stride collision redirection");root.transform.position=Vector3.up*.08f;
            var definition=GameCatalog.Find<CharacterDef>(hero);
            var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var profile=driver.strideProfile;Object.DestroyImmediate(driver);
            string state=shaped?"Run":"Walk";float speed=shaped?6.7f:2.6f;
            var clip=rig.runtimeAnimatorController.animationClips.Single(c=>c.name==state);
            rig.runtimeAnimatorController=null;
            var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
            var graph=PlayableGraph.Create("mid-stride facing");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var pose=AnimationClipPlayable.Create(graph,clip);pose.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(pose);graph.Play();
            const float dt=1f/60;float time=0;
            float rate=profile.Cadence(state,speed),stride=profile.Stride(state,speed,rate);
            void Evaluate(Vector3 travel)
            {
                time+=dt;float phase=time*rate/clip.length;
                root.transform.position+=travel*speed*dt;
                pose.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                feet.Correct(travel*speed,state,phase,stride,false,targetFacing:false,deltaTime:dt);
            }
            try
            {
                Physics.SyncTransforms();
                for(int frame=0;frame<60;frame++)Evaluate(Vector3.forward);
                // A wall/pad redirects an already moving body. This is not
                // another startup, nor a motor-heading turn with braking.
                foreach(float angle in new[]{90f,-90f,180f})
                {
                    var travel=Quaternion.Euler(0,angle,0)*Vector3.forward;
                    for(int frame=0;frame<20;frame++)
                    {
                        Evaluate(travel);
                        var span=rig.GetBoneTransform(HumanBodyBones.RightUpperLeg).position-rig.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
                        var visible=Vector3.Cross(span,Vector3.up);visible.y=0;
                        Assert.Less(Vector3.Angle(visible,travel),10,"mid-stride contact changed travel; body must follow it");
                    }
                }
            }
            finally{graph.Destroy();Object.DestroyImmediate(root);Object.DestroyImmediate(floor);}
        }
    }
}
