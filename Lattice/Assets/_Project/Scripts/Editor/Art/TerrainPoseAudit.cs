using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    // Re-evaluate a retained ordinary player's trajectory against its real
    // terrain. This is pose diagnosis, never a replacement for that recording.
    public static class TerrainPoseAudit
    {
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-terrain-run")??throw new ArgumentException("terrain run required");
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new ArgumentException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C2",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve prior pose evidence");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"scope.txt"),"Single-clip pose diagnosis against recorded terrain. Animator transition mixtures are NOT reconstructed. Transition rows are labeled and cannot establish the original blended pose. No continuous-motion acceptance.\n");
            string fixture=DevArgs.Value("-terrain-fixture")??throw new ArgumentException("recorded fixture required");
            string hero=DevArgs.Value("-terrain-hero")??"Sela";
            float shot=float.Parse(DevArgs.Value("-terrain-shot")??"-1",CultureInfo.InvariantCulture);
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Sorrel_Ridges.unity",OpenSceneMode.Single);
            var root=new GameObject("retained player pose");
            var body=Object.Instantiate(GameCatalog.Find<CharacterDef>(hero).shaped,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var profile=driver.strideProfile;Object.DestroyImmediate(driver);
            var soles=new[]{new SoleMarkers(rig,true),new SoleMarkers(rig,false)};
            var controller=(UnityEditor.Animations.AnimatorController)rig.runtimeAnimatorController;
            var clips=controller.layers[0].stateMachine.states.ToDictionary(s=>s.state.name,s=>s.state.motion as AnimationClip);
            rig.runtimeAnimatorController=null;
            var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
            var graph=PlayableGraph.Create("retained player terrain pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output=AnimationPlayableOutput.Create(graph,"pose",rig);graph.Play();
            var poses=new Dictionary<string,AnimationClipPlayable>();
            float previousTime=-1;bool captured=false;
            using var writer=new StreamWriter(Path.Combine(folder,"pose.csv"));
            writer.WriteLine("elapsed,clip,phase,stride,transitioning,grade,requestedDrop,reach,hipHeight,torsoBehindRearSole,hipX,hipY,hipZ,chestX,chestY,chestZ");
            try
            {
                Physics.SyncTransforms();GameTime.Reset();
                foreach(string line in File.ReadLines(fixture).Skip(1))
                {
                    var v=line.Split(',');float F(int i)=>float.Parse(v[i],CultureInfo.InvariantCulture);
                    float time=F(0),phase=F(9);string clip=v[8];bool transitioning=bool.Parse(v[11]);
                    root.transform.SetPositionAndRotation(new Vector3(F(1),F(2),F(3)),Quaternion.LookRotation(new Vector3(F(6),0,F(7))));
                    if(!poses.TryGetValue(clip,out var pose)){pose=AnimationClipPlayable.Create(graph,clips[clip]);pose.SetApplyFootIK(true);poses.Add(clip,pose);}
                    output.SetSourcePlayable(pose);pose.SetTime(Mathf.Repeat(phase,1)*pose.GetAnimationClip().length);graph.Evaluate(0);
                    var velocity=new Vector3(F(4),0,F(5));
                    feet.Correct(velocity,clip,phase,F(10),transitioning,targetFacing:false,deltaTime:previousTime<0?1f/60:time-previousTime);
                    previousTime=time;
                    var hips=rig.GetBoneTransform(HumanBodyBones.Hips).position;var chest=rig.GetBoneTransform(HumanBodyBones.Chest).position;
                    var direction=velocity.sqrMagnitude>.01f?velocity.normalized:root.transform.forward;
                    float rear=Mathf.Min(Vector3.Dot(soles[0].Heel,direction),Vector3.Dot(soles[0].Toe,direction),Vector3.Dot(soles[1].Heel,direction),Vector3.Dot(soles[1].Toe,direction));
                    Physics.Raycast(hips+Vector3.up,Vector3.down,out var ground,3,~0,QueryTriggerInteraction.Ignore);
                    writer.WriteLine(FormattableString.Invariant($"{time:F6},{clip},{phase:F6},{F(10):F6},{transitioning},{feet.TerrainGrade:F6},{feet.RequestedSupportDrop:F6},{feet.MaximumReachCorrection:F6},{hips.y-ground.point.y:F6},{rear-Vector3.Dot((hips+chest)*.5f,direction):F6},{hips.x:F6},{hips.y:F6},{hips.z:F6},{chest.x:F6},{chest.y:F6},{chest.z:F6}"));
                    if(!captured&&time>=shot&&shot>=0)
                    {
                        File.WriteAllText(Path.Combine(folder,"shot.txt"),FormattableString.Invariant($"elapsed={time:F6}; state={clip}; transitioning={transitioning}; single clip only\n"));
                        var camera=new GameObject("retained pose side",typeof(Camera)).GetComponent<Camera>();
                        camera.orthographic=true;camera.orthographicSize=1.4f;
                        camera.transform.position=root.transform.position+Vector3.Cross(Vector3.up,direction)*4+Vector3.up*1.5f;
                        camera.transform.LookAt(root.transform.position+Vector3.up*.9f);
                        FacingAudit.Capture(camera,Path.Combine(folder,"side.png"));
                        Object.DestroyImmediate(camera.gameObject);captured=true;
                    }
                }
                if(shot>=0&&!captured)throw new InvalidOperationException("Requested pose was not reached");
                File.Copy(fixture,Path.Combine(folder,"fixture.csv"));
                Debug.Log("TERRAIN_POSE_OK "+folder);
            }
            finally{graph.Destroy();Object.DestroyImmediate(root);}
        });
    }
}
