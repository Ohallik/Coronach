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
            try
            {
                foreach(string name in row.enemy=="SentinelHusk"?new[]{"Down","Idle","Walk","Attack","Stagger"}.Where(clips.ContainsKey):new[]{"Down","Revive"})
                {
                    var clip=clips[name];var graph=PlayableGraph.Create("Lifecycle surface calibration");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    try
                    {
                        var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                        AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
                        var keys=new Keyframe[121];
                        for(int i=0;i<keys.Length;i++)
                        {
                            float phase=i/120f;playable.SetTime(clip.length*Mathf.Min(phase,.9999f));graph.Evaluate(0);
                            if(vanes!=null)vanes.SetDown(GroundLifecycleSupport.VaneWeight(name,phase),Vector3.up);
                            keys[i]=new Keyframe(phase,ModelGeometry.BoundsOf(sample).min.y);
                        }
                        var curve=new AnimationCurve(keys);
                        for(int i=0;i<keys.Length;i++)
                        {AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
                        surfaces[name]=curve;
                    }
                    finally{graph.Destroy();}
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
                PrefabUtility.SaveAsPrefabAsset(body,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(body);}
            if(guid!=AssetDatabase.AssetPathToGUID(path))throw new System.InvalidOperationException("Lifecycle prefab GUID changed "+path);
        }
    }
}
