using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Lattice.Tests.EditMode
{
    public sealed class TerrainReplayContactTests
    {
        // Root/heading/cadence samples retained from the rejected ordinary-input
        // Sorrel recordings. They make the specific edge/shoulder reproducible
        // despite small timing differences in a complete gamepad route.
        [TestCase("pad", "Sela")]
        [TestCase("slope", "Taren")]
        public void RecordedTerrainStrideKeepsItsVisibleSoleSupported(string fixture,string hero)
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Sorrel_Ridges.unity",OpenSceneMode.Additive);
            var root=new GameObject("recorded terrain stride");SceneManager.MoveGameObjectToScene(root,scene);
            var body=Object.Instantiate(GameCatalog.Find<CharacterDef>(hero).shaped,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var profile=driver.strideProfile;Object.DestroyImmediate(driver);
            var soles=new[]{new VisibleSole(rig,true),new VisibleSole(rig,false)};
            var clips=rig.runtimeAnimatorController.animationClips;
            var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
            rig.runtimeAnimatorController=null;
            var graph=PlayableGraph.Create("recorded terrain contact");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output=AnimationPlayableOutput.Create(graph,"pose",rig);graph.Play();
            var poses=new Dictionary<string,AnimationClipPlayable>();
            var contacts=new[]{new List<Vector3>(),new List<Vector3>()};
            float drift=0,penetration=0,lift=0,reach=0,drop=0;int samples=0,complete=0;
            var previousKnees=new Vector3[2];var previousAngles=new float[2];
            float angleRatio=0,kneeRatio=0;int jointSamples=0;bool priorTransition=true;
            string angleAt="",kneeAt="";
            string prior="";float previousTime=0,firstTime=-1;
            void Flush(int side)
            {
                var points=contacts[side];
                foreach(var a in points)foreach(var b in points)drift=Mathf.Max(drift,new Vector2(a.x-b.x,a.z-b.z).magnitude);
                if(points.Count>=2)complete++;points.Clear();
            }
            try
            {
                Physics.SyncTransforms();
                foreach(string line in File.ReadLines("Assets/Tests/EditMode/Fixtures/terrain-"+fixture+".csv").Skip(1))
                {
                    var v=line.Split(',');float F(int i)=>float.Parse(v[i],CultureInfo.InvariantCulture);
                    float time=F(0),phase=F(9);string clip=v[8];bool transitioning=bool.Parse(v[11]);
                    if(firstTime<0)firstTime=time;
                    root.transform.SetPositionAndRotation(new Vector3(F(1),F(2),F(3)),Quaternion.LookRotation(new Vector3(F(6),0,F(7))));
                    if(!poses.TryGetValue(clip,out var pose)){pose=AnimationClipPlayable.Create(graph,clips.Single(c=>c.name==clip));pose.SetApplyFootIK(true);poses.Add(clip,pose);}
                    output.SetSourcePlayable(pose);pose.SetTime(Mathf.Repeat(phase,1)*pose.GetAnimationClip().length);graph.Evaluate(0);
                    feet.Correct(new Vector3(F(4),0,F(5)),clip,phase,F(10),transitioning,targetFacing:false,deltaTime:time-previousTime);
                    for(int side=0;side<2;side++)
                    {
                        var hip=rig.GetBoneTransform(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg).position;
                        var knee=rig.GetBoneTransform(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg).position;
                        var foot=rig.GetBoneTransform(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot).position;
                        // Remove the motor's legitimate whole-body turn when
                        // measuring joint discontinuity through a reversal.
                        float angle=Vector3.Angle(hip-knee,foot-knee);knee=root.transform.InverseTransformPoint(knee);
                        if(time-firstTime>=.5f&&prior==clip&&!transitioning&&!priorTransition)
                        {
                            float dt=time-previousTime;Assert.Greater(dt,0);
                            float angular=Mathf.Abs(angle-previousAngles[side])/dt/(clip=="Walk"?1200:3000);
                            float moving=Vector3.Distance(knee,previousKnees[side])/dt/(clip=="Walk"?6:clip=="Run"?15:18);
                            if(angular>angleRatio){angleRatio=angular;angleAt=$"{time:F6}/{clip}/{phase:F6}/{side}";}
                            if(moving>kneeRatio)
                            {
                                kneeRatio=moving;kneeAt=$"{time:F6}/{clip}/{phase:F6}/{side} {previousKnees[side]} -> {knee} hip={root.transform.InverseTransformPoint(hip)} foot={root.transform.InverseTransformPoint(foot)}";
                            }
                            jointSamples++;
                        }
                        previousKnees[side]=knee;previousAngles[side]=angle;
                    }
                    priorTransition=transitioning;
                    previousTime=time;reach=Mathf.Max(reach,feet.MaximumReachCorrection);drop=Mathf.Max(drop,feet.RequestedSupportDrop);
                    if(prior!=clip||transitioning){Flush(0);Flush(1);}prior=clip;
                    for(int side=0;side<2;side++)
                    {
                        float p=Mathf.Repeat(phase-side*(clip=="Sprint"?.55f:.5f),1);
                        bool central=clip=="Walk"?p>=.2f&&p<=.4f:clip=="Run"?p>=.08f&&p<=.16f:clip=="Sprint"&&p>=.1f&&p<=.15f;
                        if(!central||transitioning||time-firstTime<.5f){Flush(side);continue;}
                        var sole=soles[side].Point();contacts[side].Add(sole);samples++;
                        Assert.IsTrue(Physics.Raycast(sole+Vector3.up,Vector3.down,out var support,2,~0,QueryTriggerInteraction.Ignore),"recorded stride has no physical support");
                        float clearance=sole.y-support.point.y;
                        penetration=Mathf.Max(penetration,-clearance);lift=Mathf.Max(lift,clearance);
                    }
                }
                Flush(0);Flush(1);
                Debug.Log($"TERRAIN_STRIDE {fixture}/{hero} samples={samples} contacts={complete} drift={drift:F6} penetration={penetration:F6} lift={lift:F6} reach={reach:F6} requestedDrop={drop:F6} kneeAngleRatio={angleRatio:F6} kneeSpeedRatio={kneeRatio:F6} jointSamples={jointSamples}");
                Debug.Log($"TERRAIN_JOINT_PEAK angle={angleAt} knee={kneeAt}");
                Assert.GreaterOrEqual(samples,8);Assert.GreaterOrEqual(complete,2);
                var failures=new List<string>();
                if(drift>.05f)failures.Add("visible planted sole slides "+drift+" m");
                if(penetration>.03f)failures.Add("visible sole penetrates physical support "+penetration+" m");
                if(jointSamples<16)failures.Add("too few recorded joint samples");
                if(angleRatio>1)failures.Add("recorded terrain knee extension exceeds the whole-step limit by "+angleRatio);
                if(kneeRatio>1)failures.Add("recorded terrain knee movement exceeds the whole-step limit by "+kneeRatio);
                Assert.IsEmpty(failures,string.Join("; ",failures));
            }
            finally{graph.Destroy();foreach(var sole in soles)sole.Dispose();Object.DestroyImmediate(root);EditorSceneManager.CloseScene(scene,true);}
        }

        // Independently bake generated geometry; no IK target/profile marker is
        // used to determine the observed contact point.
        sealed class VisibleSole
        {
            sealed class Part {public SkinnedMeshRenderer renderer;public Mesh mesh=new Mesh();public List<Vector3> vertices=new();public int[] indices;}
            readonly List<Part> parts=new();
            public VisibleSole(Animator rig,bool left)
            {
                var foot=rig.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                var other=rig.GetBoneTransform(left?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
                var candidates=new List<(Part part,int index,Vector3 point)>();
                foreach(var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var part=new Part{renderer=renderer};parts.Add(part);renderer.BakeMesh(part.mesh);part.mesh.GetVertices(part.vertices);
                    for(int i=0;i<part.vertices.Count;i++)
                    {
                        var p=renderer.transform.TransformPoint(part.vertices[i]);
                        if(Vector3.Distance(p,foot.position)<.48f&&Vector3.Dot(p-(foot.position+other.position)*.5f,foot.position-other.position)>0)candidates.Add((part,i,p));
                    }
                }
                Assert.IsNotEmpty(candidates);float bottom=candidates.Min(c=>c.point.y);
                var sole=candidates.Where(c=>c.point.y<bottom+.025f).ToArray();
                float back=sole.Min(c=>c.point.z),front=sole.Max(c=>c.point.z);Assert.Greater(front-back,.08f);
                foreach(var part in parts)part.indices=sole.Where(c=>c.part==part&&c.point.z>front-(front-back)*.25f).Select(c=>c.index).ToArray();
            }
            public Vector3 Point()
            {
                Vector3 sum=Vector3.zero;int count=0;
                foreach(var part in parts){if(part.indices.Length==0)continue;part.renderer.BakeMesh(part.mesh);part.mesh.GetVertices(part.vertices);foreach(int i in part.indices){sum+=part.renderer.transform.TransformPoint(part.vertices[i]);count++;}}
                return sum/count;
            }
            public void Dispose(){foreach(var part in parts)Object.DestroyImmediate(part.mesh);}
        }
    }
}
