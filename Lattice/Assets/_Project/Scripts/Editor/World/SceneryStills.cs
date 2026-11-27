using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Round eight's scenery stills: the places the looks kept seeing something wrong in, drawn from the cameras the captures had (frames.csv of nov26-r7-chain and
    /// nov25-whole), with the live pipeline and the local art put in as the player puts them in, Taren where the hero stood on foot. Each shot can also name screen points: what
    /// is drawn first under each is logged (every mesh given a collider for the moment), so a still's stray thing is found by name. Nothing is saved. Run:
    /// exec-when-green.ps1 -Method Lattice.EditorTools.SceneryStills.Render -Marker SCENERY_STILLS_OK -Graphics -ExtraArgs @('-look-run','name','-still-zones','ManyHands').</summary>
    public static class SceneryStills
    {
        static readonly (string zone,string name,Vector3 hero,Vector3 eye,bool flight,Vector2[] probes)[] Shots=
        {
            // nov26-r7-chain s30/423: the concourse, the orange line from the left edge under the vitals panel to about x 880.
            ("ManyHands","s30-423",new Vector3(52.632f,.08f,3.306f),new Vector3(47.607f,12.614f,-9.931f),false,new[]{new Vector2(700,220),new Vector2(860,191),new Vector2(560,246)}),
            // nov25-whole s54/111: the dock after travel, Idra set down at (-2, -60) beside the lead at the arrival.
            ("ManyHands","s54-111",new Vector3(-4,.08f,-60),new Vector3(-9.109f,12.61f,-74.037f),false,new[]{new Vector2(1210,410),new Vector2(1150,470)}),
            // nov25-whole s68/123: the rendezvous framing, the pleated purple sheet across the bottom of the frame under the ring hull.
            ("ReturningPassage","s68-123",new Vector3(.019f,1,1236.068f),new Vector3(.019f,20.322f,1219.07f),true,new[]{new Vector2(700,1000),new Vector2(1500,950),new Vector2(1800,1000),new Vector2(1350,880),new Vector2(400,1060)}),
        };

        public static void Render()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??throw new ArgumentException("-look-run <name> is required");
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(System.IO.Directory.Exists(folder))throw new InvalidOperationException("Preserve earlier look run: "+folder);
            System.IO.Directory.CreateDirectory(folder);
            var zones=(DevArgs.Value("-still-zones")??string.Join(",",Shots.Select(s=>s.zone).Distinct())).Split(',',StringSplitOptions.RemoveEmptyEntries);
            bool probe=DevArgs.Has("-still-probe");
            var before=GraphicsSettings.defaultRenderPipeline;var beforeQuality=QualitySettings.renderPipeline;
            var pipeline=Resources.Load<RenderPipelineAsset>("LocalBiomes/LocalPipeline")??throw new InvalidOperationException("SCENERY_STILLS the live pipeline is missing");
            var ground=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
            var flight=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Flight.asset");
            int count=0;
            try
            {
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                foreach(var zone in zones)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                    foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                    int local=0;foreach(var patch in UnityEngine.Object.FindObjectsByType<BiomePatch>(FindObjectsSortMode.None))if(patch.ApplyLocalArt())local++;
                    var def=AssetDatabase.FindAssets("t:ZoneDef").Select(g=>AssetDatabase.LoadAssetAtPath<ZoneDef>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(d=>d.scene==zone||d.id==zone);
                    if(def!=null)RenderSettings.skybox=def.skybox;
                    foreach(var shot in Shots.Where(s=>s.zone==zone))
                    {
                        var profile=shot.flight?flight:ground;
                        GameObject taren=null;
                        if(!shot.flight)
                        {
                            taren=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Characters","TarenNatural"));
                            var rig=taren.GetComponentInChildren<Animator>();var idle=rig!=null&&rig.runtimeAnimatorController!=null?rig.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle")):null;
                            taren.transform.SetPositionAndRotation(shot.hero,Quaternion.Euler(0,200,0));if(idle!=null)idle.SampleAnimation(rig.gameObject,.3f);
                        }
                        // A SkyDrift in play turns the sky by where the view meets the flight plane (LookDev's way).
                        bool drifts=shot.flight&&UnityEngine.Object.FindAnyObjectByType<SkyDrift>()!=null;
                        Shader.SetGlobalVector("_SkyTurn",drifts?new Vector4(shot.hero.z*.06f,shot.hero.x*.06f,0,0):Vector4.zero);
                        var camera=new GameObject("Scenery still camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)).GetComponent<Camera>();
                        camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);
                        camera.nearClipPlane=.3f;camera.farClipPlane=1000;camera.fieldOfView=profile.fov;camera.aspect=16f/9;
                        var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
                        camera.transform.SetPositionAndRotation(shot.eye,Quaternion.Euler(profile.pitch,profile.yaw,0));
                        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();camera.Render();
                        var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,zone+"-"+shot.name+".png"),image.EncodeToPNG());
                        RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
                        if(probe)Probe(camera,zone+"-"+shot.name,shot.probes);
                        UnityEngine.Object.DestroyImmediate(camera.gameObject);if(taren!=null)UnityEngine.Object.DestroyImmediate(taren);count++;
                        Debug.Log($"SCENERY_STILL {zone} {shot.name} local={local}");
                    }
                }
                Debug.Log($"SCENERY_STILLS_OK count={count} folder={folder}");
            }
            finally{GraphicsSettings.defaultRenderPipeline=before;QualitySettings.renderPipeline=beforeQuality;Shader.SetGlobalVector("_SkyTurn",Vector4.zero);}
        });

        /// <summary>What is drawn first under each screen point (y down from the top, as a still is read): every mesh in the scene given a collider of its own shape for the moment,
        /// the nearest of those the ray meets logged with its path and material.</summary>
        static void Probe(Camera camera,string shot,Vector2[] points)
        {
            var added=new List<MeshCollider>();
            foreach(var view in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(!view.enabled||!view.gameObject.activeInHierarchy)continue;var filter=view.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                try{var c=view.gameObject.AddComponent<MeshCollider>();c.sharedMesh=filter.sharedMesh;added.Add(c);}catch(Exception){}
            }
            Physics.SyncTransforms();
            var ours=new HashSet<Collider>(added);
            foreach(var p in points)
            {
                var ray=camera.ViewportPointToRay(new Vector3(p.x/1920f,1-p.y/1080f,0));
                var hits=Physics.RaycastAll(ray,1000,~0,QueryTriggerInteraction.Collide).Where(h=>ours.Contains(h.collider)).OrderBy(h=>h.distance).Take(3).ToArray();
                if(hits.Length==0){Debug.Log($"SCENERY_PROBE {shot} ({p.x},{p.y}) nothing");continue;}
                foreach(var h in hits)
                {
                    var view=h.collider.GetComponent<MeshRenderer>();
                    Debug.Log($"SCENERY_PROBE {shot} ({p.x},{p.y}) -> {Path(h.collider.transform)} mat={(view!=null&&view.sharedMaterial!=null?view.sharedMaterial.name:"-")} at={h.point.ToString("F2")} dist={h.distance:F1}");
                }
            }
            foreach(var c in added)if(c!=null)UnityEngine.Object.DestroyImmediate(c);
        }
        static string Path(Transform t){var names=new List<string>();for(var at=t;at!=null;at=at.parent)names.Insert(0,at.name);return string.Join("/",names);}
    }
}
