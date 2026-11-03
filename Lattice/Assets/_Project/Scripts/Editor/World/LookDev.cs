using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Read-only look-development stills. Opens each zone, places static hero
    /// bodies at authored points and renders through the zone's own gameplay
    /// camera profile and post volume. Never saves a scene or asset.
    /// </summary>
    public static class LookDev
    {
        public struct View{public string name;public Vector3 point;public bool flight;public View(string n,Vector3 p,bool f=false){name=n;point=p;flight=f;}}

        public static readonly (string zone,View[] views)[] Views=
        {
            ("Hub_Decks",new[]{new View("arrival",new Vector3(-20,0,0)),new View("promenade",new Vector3(0,0,0)),new View("market",new Vector3(20,0,0))}),
            ("Hub_CinderHalo",new[]{new View("dock",new Vector3(-20,1,0),true),new View("ring",new Vector3(25,1,25),true)}),
            ("Sorrel_Ridges",new[]{new View("outpost",new Vector3(0,0,18)),new View("seam",new Vector3(-35,0,58)),new View("service",new Vector3(35,0,82)),new View("bore",new Vector3(8,0,164))}),
            ("Arena_Ground",new[]{new View("yard",new Vector3(-6,0,4))}),
            ("Hushwell",new[]{new View("descent",new Vector3(0,0,20)),new View("nursery",new Vector3(14,-15,334))}),
            ("Gullet_Tunnel",new[]{new View("mouth",new Vector3(0,1,8),true),new View("throat",new Vector3(Mathf.Sin(320f/900*Mathf.PI*4)*12,1,320),true)}),
            ("TallowApproach",new[]{new View("refuge",new Vector3(0,1,-8),true)}),
            ("TallowDrift",new[]{new View("refuge",new Vector3(0,0,-3)),new View("garden",new Vector3(6,0,3))}),
        };

        public static void Render()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??throw new System.ArgumentException("-look-run <name> is required");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(Directory.Exists(folder))throw new System.InvalidOperationException("Preserve earlier look run: "+folder);
            Directory.CreateDirectory(folder);
            string only=DevArgs.Value("-look-zones");
            bool local=DevArgs.Has("-local-biomes");
            var before=GraphicsSettings.defaultRenderPipeline;var beforeQuality=QualitySettings.renderPipeline;
            var ground=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
            var flight=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Flight.asset");
            try
            {
                if(local)
                {
                    var pipeline=Resources.Load<RenderPipelineAsset>("LocalBiomes/LocalPipeline");
                    if(pipeline==null)throw new System.InvalidOperationException("Local biome pipeline is missing");
                    GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                }
                int count=0;
                foreach(var (zone,views) in Views)
                {
                    if(!string.IsNullOrEmpty(only)&&!only.Split(',').Contains(zone))continue;
                    EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                    foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                    if(local)foreach(var patch in Object.FindObjectsByType<Lattice.World.BiomePatch>(FindObjectsSortMode.None))patch.ApplyLocalArt();
                    if(DevArgs.Has("-look-preview"))ZoneLook.Preview(zone);
                    // ZoneController assigns the skybox at runtime; mirror it so editor stills match play.
                    var def=AssetDatabase.FindAssets("t:ZoneDef").Select(g=>AssetDatabase.LoadAssetAtPath<ZoneDef>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(d=>d.scene==zone||d.id==zone);
                    if(def!=null)RenderSettings.skybox=def.skybox;
                    var camera=new GameObject("Look camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                    camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);
                    camera.nearClipPlane=.3f;camera.farClipPlane=1500;
                    var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
                    foreach(var view in views)
                    {
                        var heroes=Heroes(view);
                        var profile=view.flight?flight:ground;var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);
                        camera.fieldOfView=profile.fov;
                        camera.transform.SetPositionAndRotation(view.point+Vector3.up*(view.flight?0:1)-rotation*Vector3.forward*profile.distance,rotation);
                        Shot(camera,Path.Combine(folder,zone+"-"+view.name+".png"));count++;
                        foreach(var h in heroes)Object.DestroyImmediate(h);
                    }
                    Object.DestroyImmediate(camera.gameObject);
                }
                Debug.Log($"LOOKDEV_OK count={count} folder={folder}");
            }
            finally{GraphicsSettings.defaultRenderPipeline=before;QualitySettings.renderPipeline=beforeQuality;}
        });

        static GameObject[] Heroes(View view)
        {
            string[] names=view.flight?new[]{"TarenFlight","SelaFlight"}:new[]{"TarenNatural","SelaNatural"};
            var result=new GameObject[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+names[i]+".prefab");
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var at=view.point+new Vector3(i*2.2f-1.1f,0,0);
                if(!view.flight&&Physics.Raycast(at+Vector3.up*6,Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore))at=hit.point;
                body.transform.SetPositionAndRotation(at,Quaternion.Euler(0,160,0));
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
                foreach(var animator in body.GetComponentsInChildren<Animator>())
                {
                    var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                    var idle=clips.FirstOrDefault(c=>c.name.ToLowerInvariant().Contains("idle"));
                    if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
                }
                result[i]=body;
            }
            return result;
        }

        static void Shot(Camera camera,string path)
        {
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;
            camera.Render();camera.Render();
            var prior=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=prior;camera.targetTexture=null;
            Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
        }
    }
}
