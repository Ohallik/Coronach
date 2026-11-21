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
        /// <summary>The Sentinel Husk's rig and its reskins (the Muster's soldiers, D202) share one lifecycle, and so does their captain: her own body has the same Meshy skeleton and the same five states.</summary>
        internal static bool HuskRig(GenIntake.Row row)=>row.enemy=="SentinelHusk"||row.enemy=="HessaKorl"||row.model!=null&&(row.model.Contains("P3-SentinelHusk/")
            // The Ancelane guard and its coats (the Drowned Sentry first): another Meshy skeleton with the same five states.
            ||row.model.Contains("/AncelaneGuard_clean")
            // The Host Marshal (chapter 6): its own mesh at 4.8 m on the same Meshy skeleton, so it gets the five states and the recoil layer, not the heroes' Revive clip alone.
            ||row.model.Contains("/HostMarshal_clean")
            // Chapter 7: the Last Warden's own mesh, and the gardener's when it is an enemy (the Pruner). The gardener who only keeps the gardens is a person, not an enemy, and keeps its feet.
            ||row.model.Contains("/LastWarden_clean")||!string.IsNullOrEmpty(row.enemy)&&row.model.Contains("/Gardener_clean")
            // Chapter 8: the Warmaster's war-frame, and his pikeguards, who wear the same mesh in the Muster's gunmetal (tools/c15_intake.py).
            ||row.model.Contains("/DraskVaal_clean")
            // Chapter 8's elite, Vessa Korl: her sister's mesh in chapter 6's coat. Only as an enemy: Vessa the person, and Pip, keep their feet on it.
            ||!string.IsNullOrEmpty(row.enemy)&&row.model.Contains("QA-Hessa/HessaBody_clean")
            // Chapter 9's Compact Wardens: their own mesh (made with chapter 10's batch, its riot shield cut away), rigged on the same Meshy skeleton.
            ||!string.IsNullOrEmpty(row.enemy)&&row.model.Contains("/CompactWarden_clean"));
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
                foreach(string name in HuskRig(row)?new[]{"Down","Idle","Walk","Attack","Stagger","Revive"}.Where(clips.ContainsKey):new[]{"Down","Revive"})
                {
                    var clip=clips[name];var graph=PlayableGraph.Create("Lifecycle surface calibration");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    try
                    {
                        var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                        AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
                        if(name=="Down"&&HuskRig(row))
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
