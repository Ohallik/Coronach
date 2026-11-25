using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 10 on foot as the whole-game capture of November 23 saw it (D392), from each still's own camera (frames.csv) at the play profile's pitch and yaw: Mother of Doors' crater
    /// from the east clamp's shelf (s63/150) and the pad after the launch (s63/164); the Continuance's back as walked (s66/149) and flown (s66/153); the anchor chamber's collar with
    /// the smiths at their clamps (s67/204); Meret in her well (s67/112, s67/117), drawn at her definition's size; Many Hands' chart table (s55/035). Marker <c>FOOT_LOOK_OK</c>;
    /// <c>-look-run &lt;name&gt;</c> (Builds/quality/look/&lt;name&gt;), <c>-foot-shots a,b</c> takes only those. Nothing is saved.
    /// </summary>
    public static partial class LastMooringBuilder
    {
        public static void FootLook()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??"foot-look";
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(Directory.Exists(folder))throw new System.InvalidOperationException("Preserve earlier look run: "+folder);
            Directory.CreateDirectory(folder);
            var only=DevArgs.Value("-foot-shots")?.Split(',');bool Wants(params string[] names)=>only==null||names.Any(n=>only.Contains(n));
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            Camera camera=null;var placed=new List<GameObject>();
            void Open(string zone)
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                foreach(var text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                var def=Find<ZoneDef>(zone);if(def!=null)RenderSettings.skybox=def.skybox;
                foreach(var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                {
                    if(animator.GetComponentInParent<Posed>()!=null)continue;
                    var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                    var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
                }
                foreach(var posed in Object.FindObjectsByType<Posed>(FindObjectsSortMode.None))posed.Hold();
                camera=new GameObject("Look camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);
                camera.nearClipPlane=.3f;camera.farClipPlane=1500;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
            }
            void Close(){if(camera!=null)Object.DestroyImmediate(camera.gameObject);camera=null;}
            GameObject Stand(string folder,string id,Vector3 at,float yaw,float scale=1)
            {
                var prefab=ZoneKit.Prefab(folder,id);if(prefab==null){Debug.LogWarning("FOOT_LOOK_NO_BODY "+folder+"/"+id);return null;}
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));body.transform.localScale*=scale;
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
                foreach(var animator in body.GetComponentsInChildren<Animator>())
                {
                    var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                    var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
                }
                placed.Add(body);return body;
            }
            void Clear(){foreach(var p in placed)if(p!=null)Object.DestroyImmediate(p);placed.Clear();}
            void Shot(string name,CameraProfile profile,Vector3 from)
            {
                camera.fieldOfView=profile.fov;camera.transform.SetPositionAndRotation(from,Quaternion.Euler(profile.pitch,profile.yaw,0));
                var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();
                var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
                Debug.Log("FOOT_LOOK_SHOT "+name);
            }
            Vector3 Near(CameraProfile profile,Vector3 point,float back)=>point-Quaternion.Euler(profile.pitch,profile.yaw,0)*Vector3.forward*profile.distance*back;
            float Yaw(float fx,float fz)=>Mathf.Atan2(fx,fz)*Mathf.Rad2Deg;

            // ---- Mother of Doors' crater: from the east clamp's shelf (s63/150) and over its pad after the launch (s63/164)
            if(Wants("crater-150","pad-164"))
            {
                Open(SorrelZone);
                if(Wants("crater-150")){Stand("Characters","TarenShaped",new Vector3(21,-1.92f,319.53f),Yaw(-.513f,-.858f));Stand("Characters","IdraNatural",new Vector3(22.37f,-1.92f,321.6f),200);
                    Shot("crater-150",ground,new Vector3(-.833f,40.135f,283.797f));Clear();}
                if(Wants("pad-164")){Stand("Characters","TarenFlight",new Vector3(26.17f,1,315.33f),Yaw(.961f,-.276f));Stand("Characters","IdraFlight",new Vector3(22.54f,1,317.31f),300);
                    Shot("pad-164",flight,new Vector3(26.102f,20.347f,298.314f));Clear();}
                Close();
            }
            // ---- the Continuance's back: walked to the launch (s66/149) and flown into her lane (s66/153)
            if(Wants("hull-149","hull-153"))
            {
                Open(ContinuanceZone);
                if(Wants("hull-149")){Stand("Characters","TarenShaped",new Vector3(1.76f,-2.92f,235.79f),Yaw(.753f,.659f));Stand("Characters","IdraNatural",new Vector3(-4.63f,-2.92f,234.72f),90);
                    Shot("hull-149",ground,new Vector3(-9.395f,31.1f,206.912f));Clear();}
                if(Wants("hull-153")){Stand("Characters","TarenFlight",new Vector3(-4.3f,19,211.58f),Yaw(.124f,.992f));Stand("Characters","IdraFlight",new Vector3(-.37f,19,209.31f),0);
                    Shot("hull-153",flight,new Vector3(-4.083f,39.325f,195.032f));Clear();}
                Close();
            }
            // ---- the anchor chamber: the collar with the crews at their posts (s67/204), Meret in her well (s67/112, s67/117)
            if(Wants("collar-204","well-112","well-117"))
            {
                Open(ChamberZone);
                if(Wants("collar-204"))
                {
                    // The crews as they stand once she has told them (their walks end at their posts): the smiths at the clamps, facing them.
                    var people=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene.IsValid()).Select(t=>t.gameObject).ToList();
                    for(int i=0;i<4;i++)
                    {
                        var name=new[]{"Tovra, at the first clamp","Brekk, at the second clamp","A smith, at the third clamp","A smith, at the fourth clamp"}[i];
                        var body=people.FirstOrDefault(g=>g.name==name);if(body==null){Debug.LogWarning("FOOT_LOOK_NO_SMITH "+name);continue;}
                        for(var p=body.transform;p!=null;p=p.parent)p.gameObject.SetActive(true);
                        var post=Chamber.SmithPost(i);var clamp=Chamber.Clamp(i);body.transform.SetPositionAndRotation(post,Quaternion.Euler(0,Yaw(clamp.x-post.x,clamp.z-post.z),0));
                        foreach(var tint in body.GetComponentsInChildren<PropTint>(true)){var block=new MaterialPropertyBlock();foreach(var r in tint.GetComponentsInChildren<Renderer>(true)){r.GetPropertyBlock(block);block.SetColor("_BaseColor",tint.tint);r.SetPropertyBlock(block);}}
                        foreach(var animator in body.GetComponentsInChildren<Animator>()){var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);}
                        Debug.Log($"FOOT_LOOK_SMITH {name} at {post} tint={body.GetComponentInChildren<PropTint>(true)?.tint}");
                    }
                    Stand("Characters","TarenShaped",new Vector3(0,Chamber.Well,77.5f),Yaw(.591f,.807f));Stand("Characters","IdraNatural",new Vector3(-.79f,Chamber.Well,74.33f),20);
                    Shot("collar-204",ground,new Vector3(-5.102f,-3.386f,63.063f));Shot("collar-204-close",ground,Near(ground,Chamber.SmithPost(0)+Vector3.up*1.1f,.4f));Clear();
                }
                var meretAt=new Vector3(5.2f,Chamber.Well,71.6f);
                if(Wants("well-117"))
                {
                    Stand("Characters","TarenShaped",new Vector3(3.57f,Chamber.Well,73.07f),Yaw(.6f,-.8f));Stand("Characters","IdraNatural",new Vector3(7.6f,Chamber.Well,70.4f),-60);
                    var her=Stand("Enemies","MeretWellDirector",meretAt,200,MeretFrame.Drawn);if(her!=null)foreach(var gear in her.GetComponentsInChildren<HessaRegalia>(true))gear.Follow();
                    foreach(var seal in Object.FindObjectsByType<Membrane>(FindObjectsSortMode.None))seal.SetOpen(false);
                    Shot("well-117",ground,new Vector3(-2.196f,-2.694f,58.11f));Shot("well-117-close",ground,Near(ground,meretAt+Vector3.up*1.1f,.32f));Clear();
                }
                if(Wants("well-112"))
                {
                    var at=new Vector3(5.6f,Chamber.Well,66.6f);
                    Stand("Characters","TarenShaped",new Vector3(5.27f,Chamber.Well,64.2f),Yaw(-.022f,-1));Stand("Characters","IdraNatural",new Vector3(4.03f,Chamber.Well,61.6f),20);
                    var her=Stand("Enemies","MeretWellDirector",at,215,MeretFrame.Drawn);if(her!=null)foreach(var gear in her.GetComponentsInChildren<HessaRegalia>(true))gear.Follow();
                    foreach(var seal in Object.FindObjectsByType<Membrane>(FindObjectsSortMode.None))seal.SetOpen(false);
                    Shot("well-112",ground,new Vector3(-.313f,-3.051f,49.866f));Shot("well-112-column",ground,Near(ground,Chamber.WellCover(1)+Vector3.up*1.1f,.4f));Clear();
                }
                Close();
            }
            // ---- Many Hands' chart table (s55/035)
            if(Wants("hands-035"))
            {
                Open("ManyHands");
                Stand("Characters","TarenNatural",new Vector3(-1.18f,3.68f,44.72f),Yaw(-.181f,.983f));Stand("Characters","IdraNatural",new Vector3(1.2f,3.68f,41.82f),330);
                Shot("hands-035",ground,new Vector3(-6.221f,16.214f,30.283f));Shot("hands-035-close",ground,Near(ground,new Vector3(0,4.7f,47.2f),.45f));Clear();
                Close();
            }
            Debug.Log("FOOT_LOOK_OK "+folder);
        });
    }
}
