using System.Linq;
using System.Collections.Generic;
using Lattice.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.EditorTools
{
    public static class LifecycleCalibration
    {
        internal static void Install(GenIntake.Row row)
        {
            string path="Assets/_Project/Prefabs/"+row.folder+"/"+row.id+".prefab";
            string guid=AssetDatabase.AssetPathToGUID(path);
            var sample=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var animator=sample.GetComponentInChildren<Animator>();
            var controller=(AnimatorController)animator.runtimeAnimatorController;
            var clips=controller.layers[0].stateMachine.states.ToDictionary(s=>s.state.name,s=>(AnimationClip)s.state.motion);
            var vanes=sample.GetComponent<GeneratedVanes>();animator.runtimeAnimatorController=null;
            var surfaces=new Dictionary<string,AnimationCurve>();
            Vector3 restEuler=Vector3.zero,restPivot=Vector3.zero;
            try
            {
                foreach(string name in row.enemy=="SentinelHusk"?new[]{"Down","Idle","Walk","Attack","Stagger"}.Where(clips.ContainsKey):new[]{"Down","Revive"})
                {
                    var clip=clips[name];var graph=PlayableGraph.Create("Lifecycle surface calibration");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    try
                    {
                        var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                        AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
                        if(name=="Down"&&row.enemy=="SentinelHusk")
                        {
                            playable.SetTime(clip.length*.9999f);graph.Evaluate(0);
                            restPivot=animator.GetBoneTransform(HumanBodyBones.Hips).position;
                            var points=sample.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(GeneratedGeometry.WorldSkinPoints).ToArray();
                            float best=float.PositiveInfinity;
                            // Settle the retargeted rigid pose around the hips:
                            // minimize supported pelvis height within a bounded tilt.
                            // This search runs only at intake, never per frame.
                            for(int pitch=-40;pitch<=40;pitch+=2)for(int roll=-40;roll<=40;roll+=2)
                            {
                                var q=Quaternion.Euler(pitch,0,roll);float minimum=float.PositiveInfinity;
                                foreach(var p in points)minimum=Mathf.Min(minimum,(q*(p-restPivot)).y);
                                if(-minimum<best){best=-minimum;restEuler=new Vector3(pitch,0,roll);}
                            }
                        }
                        var keys=new Keyframe[121];
                        for(int i=0;i<keys.Length;i++)
                        {
                            sample.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                            float phase=i/120f;playable.SetTime(clip.length*Mathf.Min(phase,.9999f));graph.Evaluate(0);
                            var tilt=Quaternion.Slerp(Quaternion.identity,Quaternion.Euler(restEuler),GroundLifecycleSupport.RestWeight(name,phase));
                            sample.transform.rotation=tilt;sample.transform.position=restPivot-tilt*restPivot;
                            if(vanes!=null)vanes.SetDown(GroundLifecycleSupport.VaneWeight(name,phase),Vector3.up);
                            keys[i]=new Keyframe(phase,ModelGeometry.BoundsOf(sample).min.y-sample.transform.position.y);
                        }
                        var curve=new AnimationCurve(keys);
                        for(int i=0;i<keys.Length;i++)
                        {AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
                        surfaces[name]=curve;
                    }
                    finally{graph.Destroy();sample.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);}
                }
            }
            finally{Object.DestroyImmediate(sample);}
            // Save a fresh, unposed prefab, preserving every rig transform and GUID.
            var body=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var support=body.GetComponent<GroundLifecycleSupport>()??body.AddComponent<GroundLifecycleSupport>();
                AnimationCurve Surface(string state)=>surfaces.TryGetValue(state,out var curve)?curve:new AnimationCurve();
                support.downSurface=Surface("Down");support.reviveSurface=Surface("Revive");
                support.idleSurface=Surface("Idle");support.walkSurface=Surface("Walk");
                support.attackSurface=Surface("Attack");support.staggerSurface=Surface("Stagger");
                support.downRestEuler=restEuler;support.downRestPivot=restPivot;
                PrefabUtility.SaveAsPrefabAsset(body,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(body);}
            if(guid!=AssetDatabase.AssetPathToGUID(path))throw new System.InvalidOperationException("Lifecycle prefab GUID changed "+path);
        }
    }
}
