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
        public struct View{public string name;public Vector3 point;public bool flight;public float zoom;public View(string n,Vector3 p,bool f=false,float z=1){name=n;point=p;flight=f;zoom=z;}}

        public static readonly (string zone,View[] views)[] Views=
        {
            ("Hub_Decks",new[]{new View("arrival",new Vector3(-20,0,0)),new View("promenade",new Vector3(0,0,0)),new View("market",new Vector3(20,0,0)),
                new View("vestibule",new Vector3(-28,0,-11)),new View("office",new Vector3(-31,0,4)),new View("cabins",new Vector3(-28,0,14)),new View("stalls",new Vector3(0,0,4)),
                new View("galley",new Vector3(0,0,14)),new View("workshop",new Vector3(29,0,5)),new View("stores",new Vector3(28,0,14)),new View("receiving",new Vector3(28,0,-11)),new View("service",new Vector3(0,0,21.5f)),
                // Paired overview with the Halo exterior (MAP_EYE_TEST exterior/interior agreement).
                new View("overview",new Vector3(0,0,3),false,3.6f),
                // The opening scene's walkway job (D195), staged in its closed morning state.
                new View("walkway",new Vector3(-14,0,-3.5f)),new View("walkway-wide",new Vector3(-14,0,-3),false,1.7f)}),
            ("Hub_CinderHalo",new[]{new View("dock",new Vector3(-20,1,0),true),new View("ring",new Vector3(25,1,25),true),
                new View("hulls-overview",new Vector3(0,1,0),true,4f),new View("station-overview",new Vector3(0,1,0),true,7f),
                // The old dock/ring points lie inside the redesigned hulls; these sit where ships actually fly.
                new View("office-dock",new Vector3(-28,1,-27),true),new View("market-roof",new Vector3(0,1,-25),true,2.2f),new View("workshop-roof",new Vector3(28,1,-25),true,2.2f)}),
            ("Sorrel_Ridges",new[]{new View("outpost",new Vector3(0,0,18)),new View("seam",new Vector3(-35,0,58)),new View("service",new Vector3(35,0,82)),new View("bore",new Vector3(8,0,164)),
                new View("landing",new Vector3(2,0,-11)),new View("habs",new Vector3(12,0,10)),new View("haul",new Vector3(5,0,47)),new View("basin",new Vector3(-5,0,96)),new View("drill",new Vector3(4,0,141)),
                // The Burrower's arena and the slope beside it, and the wrecked rig past the bore (E2-sorrel-07, E2-gullet-06).
                new View("arena-slope",new Vector3(10,0,168)),new View("rig",new Vector3(-2,0,180))}),
            ("Arena_Ground",new[]{new View("yard",new Vector3(-6,0,4))}),
            ("Hushwell",new[]{new View("descent",new Vector3(0,0,20)),new View("nursery",new Vector3(14,-15,334)),
                new View("breach",new Vector3(0,0,6)),new View("gallery-a",new Vector3(-12,0,48)),new View("gallery-b",new Vector3(8,0,84)),
                new View("survey-wall",new Vector3(5,0,112)),new View("ramp",new Vector3(1,-4,135)),new View("chamber-1",new Vector3(0,-8,164)),
                new View("chamber-2",new Vector3(-4,-8,196)),new View("chamber-3",new Vector3(1,-8,226)),new View("bellows",new Vector3(0,-8,262)),
                new View("east-pocket",new Vector3(40,-8,164)),new View("west-pocket",new Vector3(-36,-8,212)),new View("lower-ramp",new Vector3(9,-12,306)),
                new View("nursery-lift",new Vector3(24,-16,342)),
                // The Bellows' two doors, from the chamber side (E4-hushwell-04).
                new View("bellows-exit",new Vector3(0,-8,276)),new View("bellows-entry",new Vector3(0,-8,250))}),
            ("Gullet_Tunnel",new[]{new View("mouth",new Vector3(0,1,8),true),new View("throat",new Vector3(Mathf.Sin(320f/900*Mathf.PI*4)*12,1,320),true),
                new View("canal",new Vector3(GulletProfile.RouteX(70),1,70),true),new View("feeding",new Vector3(GulletProfile.RouteX(165),1,165),true),
                new View("feeding-far",new Vector3(GulletProfile.RouteX(225),1,225),true),new View("valve-0",new Vector3(GulletProfile.RouteX(252),1,252),true),
                new View("slalom",new Vector3(GulletProfile.RouteX(362),1,362),true),new View("eddy",new Vector3(GulletProfile.RouteX(410)+8,1,410),true),
                new View("gate",new Vector3(GulletProfile.RouteX(560),1,560),true),new View("gate-far",new Vector3(GulletProfile.RouteX(660),1,660),true),
                new View("coil-entry",new Vector3(GulletProfile.RouteX(745),1,745),true),new View("coil",new Vector3(GulletProfile.RouteX(805),1,805),true),
                new View("exit",new Vector3(GulletProfile.RouteX(880),1,880),true),
                // The way in as the party arrives, and drawn back as the first fight draws the view back (E2-gullet-15).
                new View("arrival",new Vector3(0,1,8),true,1.6f),new View("arrival-far",new Vector3(0,1,14),true,2.6f)}),
            ("TallowApproach",new[]{new View("refuge",new Vector3(0,1,-8),true),new View("deck",new Vector3(4.75f,1,18),true,1.8f),new View("mooring",new Vector3(-25,1,3),true),
                // The dock at the play camera's own distance, where the hull was soft beside the ships (E2-tallow-11).
                new View("dock-close",new Vector3(4.75f,1,12),true)}),
            ("TallowDrift",new[]{new View("refuge",new Vector3(0,0,-3)),new View("garden",new Vector3(6,0,3)),
                new View("waiting",new Vector3(-7,0,1)),new View("repair",new Vector3(7,0,5)),new View("arrival",new Vector3(0,0,-9)),new View("rest",new Vector3(0,0,8))}),
            // Chapter 2, the Lantern Road (D202).
            ("WickReach",new[]{new View("arrival",new Vector3(0,1,8),true),new View("relay",new Vector3(-22,1,150),true),new View("gate",new Vector3(0,1,250),true),
                new View("dock",new Vector3(0,1,282),true),new View("overview",new Vector3(0,1,150),true,4f)}),
            ("Lanternwick",new[]{new View("dock",new Vector3(0,0,-14)),new View("arch-market",new Vector3(0,0,14)),new View("wick-house",new Vector3(-12,0,28)),
                new View("lantern-stair",new Vector3(24,3,52)),new View("orchard",new Vector3(-28,1.4f,62)),new View("archive",new Vector3(0,0,92)),new View("overview",new Vector3(0,0,40),false,3.4f)}),
            ("LanternGorge",new[]{new View("entry",new Vector3(0,0,6)),new View("first-relay",new Vector3(6,0,52)),new View("den",new Vector3(-20,0,98)),
                new View("camp",new Vector3(-8,0,116)),new View("ridge",new Vector3(8,0,186)),new View("relay-door",new Vector3(0,0,222)),new View("overview",new Vector3(0,0,110),false,3.6f)}),
            ("SunkenRelay",new[]{new View("entry",new Vector3(0,0,8)),new View("corridor",new Vector3(0,0,32)),new View("gallery",new Vector3(0,0,66)),
                new View("vault",new Vector3(0,0,104)),new View("great-lantern",new Vector3(0,0,140)),new View("overview",new Vector3(0,0,90),false,3.6f)}),
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
                        camera.transform.SetPositionAndRotation(view.point+Vector3.up*(view.flight?0:1)-rotation*Vector3.forward*profile.distance*Mathf.Max(1,view.zoom),rotation);
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
