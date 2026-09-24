using System;
using System.Collections;
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
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    public static class PresentationReview
    {
        static IEnumerator routine;
        static Camera camera;
        static double next;
        static readonly string Output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/logs/presentation"));
        public static void Render(){routine=Run();EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.05;
            try{EditorApplication.QueuePlayerLoopUpdate();if(routine.MoveNext())return;EditorApplication.update-=Tick;Debug.Log("PRESENTATION_REVIEW_OK");}
            catch(Exception e){EditorApplication.update-=Tick;Debug.LogError("FAILED: PRESENTATION_REVIEW "+e);EditorApplication.Exit(1);}
        }
        static IEnumerator Run()
        {
            Directory.CreateDirectory(Output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);RenderSettings.fog=false;
            var light=new GameObject("ReviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(35,-25,0);
            camera=new GameObject("Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.45f;camera.nearClipPlane=.01f;camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.13f,.18f);camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            foreach(string id in new[]{"Taren","Sela"})
            {
                var definition=GameCatalog.Find<CharacterDef>(id);
                foreach(bool shaped in new[]{false,true})
                {
                    var body=UnityEngine.Object.Instantiate(shaped?definition.shaped:definition.natural);
                    UnityEngine.Object.DestroyImmediate(body.GetComponent<GeneratedAnimator>());
                    var animator=body.GetComponentInChildren<Animator>();var controller=animator.runtimeAnimatorController;
                    var clips=controller.animationClips;animator.runtimeAnimatorController=null;
                    foreach(var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
                    string[] states=shaped?new[]{"Run","Sprint","Attack1","Attack2","Attack3","Cleave","Dash","Guard","Shoot","Pulse"}:new[]{"Idle","Walk","Run","Sprint"};
                    camera.orthographicSize=1.45f;camera.transform.position=new Vector3(2.8f,2.2f,5);camera.transform.LookAt(new Vector3(0,1,0));
                    foreach(string state in states)
                    {
                        var clip=clips.Single(c=>c.name==state);
                        var graph=PlayableGraph.Create("Motion review");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        var play=AnimationClipPlayable.Create(graph,clip);play.SetApplyFootIK(true);AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(play);graph.Play();graph.Evaluate(0);
                        for(int frame=0;frame<4;frame++)
                        {
                            play.SetTime(clip.length*(.1f+frame*.25f));graph.Evaluate(0);yield return null;
                            Capture(id+"-"+(shaped?"Shaped":"Natural")+"-"+state+"-"+frame);
                        }
                        graph.Destroy();
                    }
                    UnityEngine.Object.DestroyImmediate(body);
                }
                var ship=UnityEngine.Object.Instantiate(definition.flight);var bounds=ModelGeometry.BoundsOf(ship);
                if(bounds.size.z<2.5f||bounds.size.y>bounds.size.z*.65f)throw new Exception("Flight silhouette is not a low ship: "+id+" "+bounds.size);
                foreach(string view in new[]{"front","top"})
                {
                    camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.z)*.66f;
                    camera.transform.position=bounds.center+(view=="top"?new Vector3(0,7,.01f):new Vector3(4,5,6));camera.transform.LookAt(bounds.center);
                    yield return null;Capture(id+"-Flight-"+view);
                }
                UnityEngine.Object.DestroyImmediate(ship);
            }
        }
        static void Capture(string name)
        {
            var rt=new RenderTexture(640,640,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
            var texture=new Texture2D(640,640,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,640,640),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
