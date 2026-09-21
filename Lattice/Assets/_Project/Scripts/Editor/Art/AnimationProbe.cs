using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace Lattice.EditorTools
{
    public static class AnimationProbe
    {
        public static void Run()=>BatchTools.Run(()=>
        {
            var reports=new List<object>();
            foreach(string path in new[]{AnimationDonors.UalPath,"Assets/_Project/Prefabs/Characters/TarenNatural.prefab"})
            {
                var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));var animator=go.GetComponentInChildren<Animator>();
                foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(AnimationDonors.UalPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")))
                {
                    var poses=new List<object>();var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);AnimationPlayableOutput.Create(graph,"probe",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
                    foreach(float phase in new[]{.15f,.65f})
                    {
                        playable.SetTime(clip.length*phase);graph.Evaluate(0);
                        poses.Add(new{phase,bones=new[]{HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightUpperLeg}.Select(b=>new{bone=b.ToString(),rotation=Point(animator.GetBoneTransform(b).localEulerAngles),position=Point(animator.GetBoneTransform(b).position)}).ToArray()});
                    }
                    graph.Destroy();
                    var curves=AnimationUtility.GetCurveBindings(clip).Where(b=>b.type==typeof(Animator)&&b.propertyName.Contains("Leg")).Select(b=>new{name=b.propertyName,range=AnimationUtility.GetEditorCurve(clip,b).keys.Select(k=>k.value).Max()-AnimationUtility.GetEditorCurve(clip,b).keys.Select(k=>k.value).Min()}).ToArray();
                    reports.Add(new{path,clip=clip.name,poses,curves});
                }
                UnityEngine.Object.DestroyImmediate(go);
            }
            File.WriteAllText(Path.GetFullPath("../Builds/logs/animation-probe.json"),JsonConvert.SerializeObject(reports,Formatting.Indented));Debug.Log("ANIMATION_PROBE_OK");
        });
        static float[] Point(Vector3 p)=>new[]{p.x,p.y,p.z};
    }
}
