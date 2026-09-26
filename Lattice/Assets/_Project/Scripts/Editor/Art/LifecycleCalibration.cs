using System.Linq;
using Lattice.Combat;
using UnityEditor;
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
            var animator=sample.GetComponentInChildren<Animator>();var clips=animator.runtimeAnimatorController.animationClips;
            var vanes=sample.GetComponent<GeneratedVanes>();animator.runtimeAnimatorController=null;
            AnimationCurve down=new(),revive=new();
            try
            {
                foreach(string name in row.enemy=="SentinelHusk"?new[]{"Down"}:new[]{"Down","Revive"})
                {
                    var clip=clips.Single(c=>c.name==name);var graph=PlayableGraph.Create("Lifecycle surface calibration");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
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
                        if(name=="Down")down=curve;else revive=curve;
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
                support.downSurface=down;support.reviveSurface=revive;
                PrefabUtility.SaveAsPrefabAsset(body,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(body);}
            if(guid!=AssetDatabase.AssetPathToGUID(path))throw new System.InvalidOperationException("Lifecycle prefab GUID changed "+path);
        }
    }
}
