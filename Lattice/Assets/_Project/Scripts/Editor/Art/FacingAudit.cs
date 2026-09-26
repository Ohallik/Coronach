using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    public static class FacingAudit
    {
        public static void Baseline() => BatchTools.Run(() => Run("C0/facing-audit"));
        public static void After() => BatchTools.Run(() => Run("C2/facing-audit"));
        public static void CalibratedBaseline() => BatchTools.Run(() => Run("C2/calibrated-before"));
        public static void CompareClipOffsets() => BatchTools.Run(() => {
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/_Project/Art/Animation/HeroLocomotion.fbx");
            var original=importer.clipAnimations;
            try {
                var clips=importer.clipAnimations;
                foreach(var clip in clips)
                {
                    if(clip.name=="Run")clip.rotationOffset=28;
                    if(clip.name=="Sprint")clip.rotationOffset=20;
                    if(clip.name=="Walk")clip.rotationOffset=7;
                }
                importer.clipAnimations=clips;importer.SaveAndReimport();
                Run("C2/clip-offset-comparison");
            } finally {importer.clipAnimations=original;importer.SaveAndReimport();}
            Debug.Log("CLIP_OFFSET_COMPARISON_OK");
        });
        public static void CompareRootOrientation() => BatchTools.Run(() => {
            const string path="Assets/_Project/Art/Animation/HeroLocomotion.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            var original=importer.clipAnimations;
            // A bounded diagnostic: no actor/prefab offsets, and always restore imports.
            try {
                var candidates=importer.clipAnimations;
                foreach(var clip in candidates)
                    if(clip.name=="Idle"||clip.name=="Walk"||clip.name=="Run"||clip.name=="Sprint")clip.keepOriginalOrientation=true;
                importer.clipAnimations=candidates;importer.SaveAndReimport();
                Run("C2/original-root-comparison");
            } finally {importer.clipAnimations=original;importer.SaveAndReimport();}
            Debug.Log("ROOT_COMPARISON_OK");
        });
        static void Run(string run)
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality",run));Directory.CreateDirectory(folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
            var light=new GameObject("Audit key",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-25,0);
            var camera=new GameObject("Audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.55f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.14f,.19f);
            using var writer=new StreamWriter(Path.Combine(folder,"poses.csv"));writer.WriteLine("hero,form,clip,phase,pelvisYaw,chestYaw,leftFootYaw,rightFootYaw,rootYaw");
            foreach(string id in new[]{"Taren","Sela"}) foreach(bool shaped in new[]{false,true})
            {
                var def=GameCatalog.Find<CharacterDef>(id);var body=UnityEngine.Object.Instantiate(shaped?def.shaped:def.natural);
                var driver=body.GetComponent<GeneratedAnimator>();if(driver!=null)UnityEngine.Object.DestroyImmediate(driver);
                var animator=body.GetComponentInChildren<Animator>();var clips=animator.runtimeAnimatorController.animationClips;animator.runtimeAnimatorController=null;
                foreach(var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
                foreach(string state in new[]{"Idle","Walk","Run","Sprint"})
                {
                    var clip=clips.SingleOrDefault(c=>c.name==state);
                    if(clip==null)continue;
                    var graph=PlayableGraph.Create("Facing audit");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var play=AnimationClipPlayable.Create(graph,clip);AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(play);graph.Play();graph.Evaluate(0);
                    for(int i=0;i<60;i++)
                    {
                        play.SetTime(clip.length*i/60d);graph.Evaluate(0);
                        float pelvis=Span(animator,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg),chest=Span(animator,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm);
                        writer.WriteLine(FormattableString.Invariant($"{id},{(shaped?"Shaped":"Natural")},{state},{i/60f:F4},{pelvis:F3},{chest:F3},{Foot(animator,HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes):F3},{Foot(animator,HumanBodyBones.RightFoot,HumanBodyBones.RightToes):F3},{animator.transform.eulerAngles.y:F3}"));
                        if(i==15)
                        {
                            camera.transform.position=new Vector3(0,6,.01f);camera.transform.LookAt(new Vector3(0,.8f,0),Vector3.forward);
                            Capture(camera,Path.Combine(folder,id+"-"+(shaped?"Shaped":"Natural")+"-"+state+"-top.png"));
                            camera.transform.position=new Vector3(0,1.5f,5);camera.transform.LookAt(new Vector3(0,1,0));
                            Capture(camera,Path.Combine(folder,id+"-"+(shaped?"Shaped":"Natural")+"-"+state+"-front.png"));
                        }
                    }
                    graph.Destroy();
                }
                UnityEngine.Object.DestroyImmediate(body);
            }
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/_Project/Art/Animation/HeroLocomotion.fbx");
            File.WriteAllLines(Path.Combine(folder,"donor-takes.txt"),importer.defaultClipAnimations.Select(c=>c.takeName+" "+c.firstFrame+" "+c.lastFrame));
            File.WriteAllLines(Path.Combine(folder,"import-settings.txt"),importer.clipAnimations.Select(c=>$"{c.name}: keepOriginalOrientation={c.keepOriginalOrientation} offset={c.rotationOffset} bakeRootRotation={c.lockRootRotation} mirror={c.mirror}"));
            Debug.Log("FACING_AUDIT_OK "+folder);
        }
        static float Span(Animator a,HumanBodyBones l,HumanBodyBones r)
        {var left=a.GetBoneTransform(l);var right=a.GetBoneTransform(r);return left!=null&&right!=null?Yaw(Vector3.Cross(right.position-left.position,Vector3.up)):float.NaN;}
        static float Foot(Animator a,HumanBodyBones foot,HumanBodyBones toe)
        {var f=a.GetBoneTransform(foot);var t=a.GetBoneTransform(toe);return f!=null&&t!=null?Yaw(t.position-f.position):float.NaN;}
        static float Yaw(Vector3 v)=>Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg;
        internal static void Capture(Camera c,string path)
        {
            // Manual editor sampling does not advance the GPU skinning frame.
            // Bake this evaluated pose into temporary renderers so consecutive
            // captures cannot silently reuse a prior clip's cached skin pose.
            var skins=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            var baked=new List<(GameObject body,Mesh mesh)>();
            foreach(var skin in skins)
            {
                var mesh=new Mesh();skin.BakeMesh(mesh);
                var body=new GameObject("Evaluated pose",typeof(MeshFilter),typeof(MeshRenderer));
                body.transform.SetParent(skin.transform,false);body.GetComponent<MeshFilter>().sharedMesh=mesh;
                body.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                skin.enabled=false;baked.Add((body,mesh));
            }
            var rt=new RenderTexture(640,640,24);c.targetTexture=rt;c.Render();c.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(640,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=previous;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
            foreach(var part in baked){UnityEngine.Object.DestroyImmediate(part.body);UnityEngine.Object.DestroyImmediate(part.mesh);}
            foreach(var skin in skins)skin.enabled=true;
        }
    }
}
