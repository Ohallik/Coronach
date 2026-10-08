using System.Collections.Generic;
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
    public sealed class TerrainBalanceTests
    {
        [TestCase("Taren",false)][TestCase("Sela",false)]
        [TestCase("Taren",true)][TestCase("Sela",true)]
        public void SlowUphillWalkDoesNotLeaveTorsoBehindBothVisibleFeet(string hero,bool shaped)
            =>Measure(hero,shaped,"Walk",1.2f);

        [TestCase("Taren",false)][TestCase("Sela",false)]
        [TestCase("Taren",true)][TestCase("Sela",true)]
        public void StationaryUphillPoseDoesNotSitBehindBothVisibleFeet(string hero,bool shaped)
            =>Measure(hero,shaped,"Idle",0);

        static void Measure(string hero,bool shaped,string state,float speed)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(100,1,100);
            var root=new GameObject("slow uphill balance regression");
            var definition=GameCatalog.Find<CharacterDef>(hero);
            var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var profile=driver.strideProfile;Object.DestroyImmediate(driver);
            var soles=new[]{new Sole(rig,true),new Sole(rig,false)};
            string take=state=="Idle"&&shaped?(hero=="Taren"?"CombatIdle":"RangedIdle"):state;
            var clip=rig.runtimeAnimatorController.animationClips.Single(c=>c.name==take);rig.runtimeAnimatorController=null;
            var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
            var graph=PlayableGraph.Create("slow slope balance");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var pose=AnimationClipPlayable.Create(graph,clip);pose.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(pose);graph.Play();
            const float dt=1f/120;
            try
            {
                foreach(float slope in state=="Idle"?new[]{0f,39f,-39f}:new[]{0f,39f})
                {
                    floor.transform.rotation=Quaternion.Euler(-slope,0,0);Physics.SyncTransforms();feet.ResetContacts();
                    var normal=floor.transform.up;var surface=floor.transform.position+normal*.5f;
                    float grade=Mathf.Abs(normal.z)/normal.y,rate=state=="Idle"?1:profile.Cadence(state,speed,grade),stride=state=="Idle"?1:profile.Stride(state,speed,rate,grade);
                    float worst=0;int samples=0;
                    for(int frame=0;frame<Mathf.CeilToInt(clip.length/rate*3/dt);frame++)
                    {
                        float time=frame*dt,phase=time*rate/clip.length;
                        var position=Vector3.forward*speed*time;position.y=Vector3.Dot(normal,surface-position)/normal.y+.22f;root.transform.position=position;
                        pose.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                        feet.Correct(Vector3.forward*speed,state,phase,stride,false,targetFacing:false,deltaTime:dt);
                        float halfStep=Mathf.Repeat(phase,.5f);
                        if(phase<1||halfStep<.2f||halfStep>.4f)continue;
                        float rear=Mathf.Min(soles[0].Rear(),soles[1].Rear());
                        float torso=(rig.GetBoneTransform(HumanBodyBones.Hips).position.z+rig.GetBoneTransform(HumanBodyBones.Chest).position.z)*.5f;
                        worst=Mathf.Max(worst,rear-torso);samples++;
                    }
                    Debug.Log($"TERRAIN_BALANCE {hero}/{shaped}/{state} slope={slope} samples={samples} torsoBehindRearSole={worst:F6}");
                    Assert.Greater(samples,80,"must cover both repeated stance intervals");
                    // This generous posture bound rejects the measured seated
                    // pose. It is not a COM simulation or visual acceptance.
                    Assert.LessOrEqual(worst,.15f,"slow uphill torso stays behind both visible feet");
                }
            }
            finally{graph.Destroy();foreach(var sole in soles)sole.Dispose();Object.DestroyImmediate(root);Object.DestroyImmediate(floor);}
        }

        // Independent rendered-mesh probe: select the bottom foot vertices in
        // bind pose, then bake them again in each sampled stance. No IK targets.
        sealed class Sole
        {
            sealed class Part {public SkinnedMeshRenderer renderer;public Mesh mesh=new Mesh();public List<Vector3> vertices=new();public int[] indices;}
            readonly List<Part> parts=new();
            public Sole(Animator rig,bool left)
            {
                var foot=rig.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                var other=rig.GetBoneTransform(left?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
                var candidates=new List<(Part part,int index,Vector3 point)>();
                foreach(var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var part=new Part{renderer=renderer};parts.Add(part);renderer.BakeMesh(part.mesh);part.mesh.GetVertices(part.vertices);
                    for(int i=0;i<part.vertices.Count;i++)
                    {
                        var point=renderer.transform.TransformPoint(part.vertices[i]);
                        if(Vector3.Distance(point,foot.position)<.48f&&Vector3.Dot(point-(foot.position+other.position)*.5f,foot.position-other.position)>0)candidates.Add((part,i,point));
                    }
                }
                Assert.IsNotEmpty(candidates);float bottom=candidates.Min(c=>c.point.y);
                foreach(var part in parts)part.indices=candidates.Where(c=>c.part==part&&c.point.y<bottom+.025f).Select(c=>c.index).ToArray();
            }
            public float Rear()
            {
                float rear=float.PositiveInfinity;
                foreach(var part in parts)
                {
                    if(part.indices.Length==0)continue;part.renderer.BakeMesh(part.mesh);part.mesh.GetVertices(part.vertices);
                    foreach(int i in part.indices)rear=Mathf.Min(rear,part.renderer.transform.TransformPoint(part.vertices[i]).z);
                }
                return rear;
            }
            public void Dispose(){foreach(var part in parts)Object.DestroyImmediate(part.mesh);}
        }
    }
}
