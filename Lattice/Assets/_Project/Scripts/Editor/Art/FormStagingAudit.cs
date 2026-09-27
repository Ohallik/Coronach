using System;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace Lattice.EditorTools
{
    public static class FormStagingAudit
    {
        public static void EnableFlightReadback()=>BatchTools.Run(()=>
        {
            foreach(string hero in new[]{"Taren","Sela"})
            {
                string path="Assets/_Project/Art/Generated/Models/"+hero+"Flight/"+hero+"Flight_clean.fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();Debug.Log("FORM_FOLD_IMPORT_OK");
        });
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-form-stages-run")??"form-stages-01";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new InvalidOperationException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C4",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve prior form evidence");
            Directory.CreateDirectory(folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
            var light=new GameObject("Form audit light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(40,-25,0);
            var camera=new GameObject("Form audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.5f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.1f,.13f);
            using var csv=new StreamWriter(Path.Combine(folder,"geometry.csv"));csv.WriteLine("hero,form,weight,minX,maxX,minY,maxY,minZ,maxZ");
            foreach(string hero in new[]{"Taren","Sela"})foreach(string form in new[]{"Natural","Shaped","Flight"})
            {
                var root=new GameObject(hero);var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+hero+form+".prefab"),root.transform);
                if(form=="Flight")body.transform.localPosition=Vector3.up*.8f;
                var animator=body.GetComponentInChildren<Animator>();PlayableGraph graph=default;
                if(animator!=null&&animator.isHuman)
                {
                    var controller=(AnimatorController)animator.runtimeAnimatorController;
                    var clip=(AnimationClip)controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Idle").state.motion;animator.runtimeAnimatorController=null;
                    graph=PlayableGraph.Create("Form audit pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var play=AnimationClipPlayable.Create(graph,clip);AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(play);graph.Play();play.SetTime(.25);graph.Evaluate(0);
                }
                var fold=body.AddComponent<FormFold>();fold.Prepare(true);
                foreach(float weight in new[]{0,.5f,1})
                {
                    fold.Apply(weight);var bounds=ModelGeometry.BoundsOf(body);
                    csv.WriteLine(FormattableString.Invariant($"{hero},{form},{weight},{bounds.min.x},{bounds.max.x},{bounds.min.y},{bounds.max.y},{bounds.min.z},{bounds.max.z}"));
                    foreach(string view in new[]{"front","side","top"})
                    {
                        var center=Vector3.up*.95f;var direction=view=="front"?new Vector3(.15f,.15f,1):view=="side"?new Vector3(1,.1f,0):new Vector3(.01f,1,.001f);
                        camera.transform.position=center+direction.normalized*20;camera.transform.LookAt(center);
                        FacingAudit.Capture(camera,Path.Combine(folder,hero+"-"+form+"-"+(int)(weight*100)+"-"+view+".png"));
                    }
                }
                fold.Restore();if(graph.IsValid())graph.Destroy();UnityEngine.Object.DestroyImmediate(root);
            }
            Debug.Log("FORM_STAGING_AUDIT_OK");
        });
    }
}
