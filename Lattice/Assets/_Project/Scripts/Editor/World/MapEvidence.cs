using System.IO;
using System.Linq;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>Read-only scene review renders. Never saves or regenerates maps.</summary>
    public static class MapEvidence
    {
        public static void Biomes()=>BatchTools.Run(()=>
        {
            string folder=Folder("workshop/biomes/views-01");
            bool local=Lattice.Core.DevArgs.Has("-local-biomes");
            var before=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            var beforeQuality=QualitySettings.renderPipeline;
            try
            {
                if(local)
                {
                    var pipeline=Resources.Load<UnityEngine.Rendering.RenderPipelineAsset>("LocalBiomes/LocalPipeline");
                    if(pipeline==null)throw new System.InvalidOperationException("Local biome pipeline is missing");
                    UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                }
                foreach(var zone in new[]{"Sorrel_Ridges","Arena_Ground","Hushwell","TallowDrift"})
                {
                    var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                    foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                    if(local)foreach(var patch in Object.FindObjectsByType<Lattice.World.BiomePatch>(FindObjectsSortMode.None))
                        if(!patch.ApplyLocalArt())throw new System.InvalidOperationException("Local art missing for "+patch.resourceKey);
                    var camera=new GameObject("Evidence camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.06f);
                    camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.fieldOfView=30;
                    camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                    var views=zone=="Sorrel_Ridges"?new[]{("outpost",new Vector3(0,1,20)),("seam",new Vector3(-35,1,58)),("service",new Vector3(35,1,82)),("bore",new Vector3(8,1,174))}:
                        zone=="Arena_Ground"?new[]{("yard",new Vector3(-13,1,9))}:
                        zone=="Hushwell"?new[]{("nursery",new Vector3(14,-15,334)),("nursery-west",new Vector3(6,-15,336)),("nursery-north",new Vector3(14,-15,340))}:
                        new[]{("refuge",new Vector3(7,1,-1))};
                    var rotation=Quaternion.Euler(40,20,0);
                    foreach(var view in views)
                    {camera.transform.SetPositionAndRotation(view.Item2-rotation*Vector3.forward*19.5f,rotation);Render(camera,folder,zone+"-"+view.Item1);}
                }
            }
            finally{UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=before;QualitySettings.renderPipeline=beforeQuality;}
            Debug.Log("BIOME_EVIDENCE_OK "+folder);
        });
        public static void Baseline() => BatchTools.Run(() => Capture("C0/maps-before"));
        public static void StationAfter() => BatchTools.Run(() => Capture("station/maps-after"));
        public static void StationBlockout() => BatchTools.Run(() => Capture("station/blockout-03"));
        public static void WorkshopOthers() => BatchTools.Run(() => Capture("workshop/maps-before",new[]{"Sorrel_Ridges","Gullet_Tunnel","Arena_Ground","Arena_Flight"}));
        public static void SorrelReview()=>BatchTools.Run(()=>
        {
            string run=Lattice.Core.DevArgs.Value("-map-evidence-run")??"workshop/sorrel-blockout-01";
            Capture(run,new[]{"Sorrel_Ridges"});
            // Capture has opened this scene and hidden all explanatory labels.
            var camera=GameObject.Find("Evidence camera").GetComponent<Camera>();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality",run));
            var views=new[]{("receiving",new Vector3(7,0,-10),11f),("repair",new Vector3(-7,0,6),10f),
                ("hab-access",new Vector3(0,0,17),14f),("first-junction",new Vector3(0,0,30),14f),
                ("seam-bend",new Vector3(-35,0,70),14f),("service-branch",new Vector3(34,0,77),14f),
                ("drill-approach",new Vector3(7,0,143),12f),("drill-excavation",new Vector3(0,0,174),21f),
                ("north-return",new Vector3(0,0,200),13f)};
            foreach(var v in views)Shot(camera,folder,"Sorrel_Ridges-"+v.Item1,v.Item2,Quaternion.Euler(40,20,0),v.Item3);
            Shot(camera,folder,"Sorrel_Ridges-excavation-section",new Vector3(0,0,174),Quaternion.Euler(15,0,0),35);
            Debug.Log("SORREL_MAP_EVIDENCE_OK "+folder);
        });
        public static void SorrelPropAudit()=>BatchTools.Run(()=>
        {
            string run=Lattice.Core.DevArgs.Value("-map-evidence-run")??"workshop/sorrel-prop-audit-01";
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality",run));
            if(Directory.Exists(folder))throw new System.InvalidOperationException("Preserve previous prop audit");
            Directory.CreateDirectory(folder);WorldBuilder.Begin("Sorrel_Ridges");
            var camera=new GameObject("Audit camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.backgroundColor=new Color(.08f,.09f,.12f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=1000;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            using(var writer=new StreamWriter(Path.Combine(folder,"bounds.csv")))
            {
                writer.WriteLine("key,width,height,depth");
                foreach(var item in new[]{("OutpostHab",4f),("OutpostDrill",8f),("LatticeAnvil",2f),("DeckDoorway",2.6f),("LandingPad",5f)})
                {
                    var go=WorldBuilder.Piece(item.Item1,Vector3.zero,Vector3.one*item.Item2);
                    var bounds=ModelGeometry.BoundsOf(go);
                    writer.WriteLine(System.FormattableString.Invariant($"{item.Item1},{bounds.size.x:F4},{bounds.size.y:F4},{bounds.size.z:F4}"));
                    for(int angle=0;angle<360;angle+=90)Shot(camera,folder,item.Item1+"-"+angle,bounds.center,Quaternion.Euler(20,angle,0),bounds.size.magnitude*.55f);
                    Object.DestroyImmediate(go);
                }
            }
            Debug.Log("SORREL_PROP_AUDIT_OK");
        });
        public static void TallowEnvelope()=>BatchTools.Run(()=>
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            var hull=GameObject.Find("TallowStationHull");
            var points=ModelGeometry.Points(hull);
            string file=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/station/tallow-envelope.csv"));
            using(var writer=new StreamWriter(file))
            {writer.WriteLine("x,y,z");foreach(var p in points)writer.WriteLine(System.FormattableString.Invariant($"{p.x:F5},{p.y:F5},{p.z:F5}"));}
            Debug.Log("TALLOW_ENVELOPE_OK "+ModelGeometry.BoundsOf(hull));
        });
        public static void TallowEnvelopeMesh()=>BatchTools.Run(()=>
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            var hull=GameObject.Find("TallowStationHull");
            string file=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/station/tallow-envelope.obj"));
            if(File.Exists(file))throw new System.InvalidOperationException("Preserve the previous envelope mesh");
            using(var writer=new StreamWriter(file))
            {
                int offset=1;
                foreach(var filter in hull.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=filter.sharedMesh;
                    foreach(var vertex in mesh.vertices)
                    {var p=filter.transform.TransformPoint(vertex);writer.WriteLine(System.FormattableString.Invariant($"v {p.x:F5} {p.y:F5} {p.z:F5}"));}
                    var triangles=mesh.triangles;
                    for(int i=0;i<triangles.Length;i+=3)writer.WriteLine($"f {triangles[i]+offset} {triangles[i+1]+offset} {triangles[i+2]+offset}");
                    offset+=mesh.vertexCount;
                }
            }
            Debug.Log("TALLOW_ENVELOPE_MESH_OK");
        });
        // Close construction views of the Cinder ring at roughly gameplay scale:
        // a free span, the far arc, a hull contact and a wider quadrant.
        public static void CinderRing()=>BatchTools.Run(()=>
        {
            string folder=Folder("station/ring-review");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hub_CinderHalo.unity");
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.gameObject.SetActive(false);
            var camera = new GameObject("Evidence camera", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.backgroundColor = new Color(.025f, .035f, .06f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            Shot(camera,folder,"ring-west",new Vector3(-62,-4,70),Quaternion.Euler(55,20,0),12);
            Shot(camera,folder,"ring-far",new Vector3(0,-4,132),Quaternion.Euler(55,20,0),12);
            Shot(camera,folder,"ring-hull-joint",new Vector3(-37,-3,20),Quaternion.Euler(55,20,0),14);
            Shot(camera,folder,"ring-quadrant",new Vector3(-44,-4,110),Quaternion.Euler(40,20,0),30);
            Shot(camera,folder,"ring-edge",new Vector3(-62,-5.5f,70),Quaternion.Euler(12,90,0),8);
            Debug.Log("MAP_EVIDENCE_OK " + folder);
        });
        // The Gullet's anatomy: one full-length overhead with the passage running
        // across the frame, then each section from a gameplay-like angle.
        public static void GulletReview()=>BatchTools.Run(()=>
        {
            string folder=Folder("workshop/gullet-review");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gullet_Tunnel.unity");
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.gameObject.SetActive(false);
            var camera = new GameObject("Evidence camera", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.backgroundColor = new Color(.025f, .035f, .06f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            Shot(camera,folder,"Gullet-overhead-length",new Vector3(0,0,450),Quaternion.Euler(90,90,0),265);
            foreach(var (name,z,scale) in new[]{("mouth",4f,24f),("feeding-chamber",165f,38f),("valve-0",262f,16f),("slalom-throat",330f,26f),
                ("salvage-eddy",410f,26f),("valve-1",490f,16f),("nursery-gate",600f,34f),("valve-2",700f,16f),("cantor-coil",805f,44f),("exit-valve",888f,20f)})
                Shot(camera,folder,"Gullet-"+name,new Vector3(Lattice.Data.GulletProfile.Center(z),0,z),Quaternion.Euler(52,0,0),scale);
            Debug.Log("MAP_EVIDENCE_OK " + folder);
        });
        public static void GulletCoilViews()=>BatchTools.Run(()=>
        {
            string folder=Folder("workshop/gullet-coil-views");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gullet_Tunnel.unity");
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
            var camera=new GameObject("Evidence camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.backgroundColor=new Color(.025f,.035f,.06f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.fieldOfView=30;camera.aspect=16f/9;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            // Review-only ship for scale, using the actual flight prefab.
            var ship=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/TarenFlight.prefab"));
            var rotation=Quaternion.Euler(48,0,0);
            foreach(var (name,at,distance) in new[]{("approach",new Vector3(0,1,757),26f),("mooring",new Vector3(-11,1,785),26f),
                ("chamber",new Vector3(0,1,805),65f),("exit",new Vector3(0,1,850),26f)})
            {
                ship.transform.position=at;camera.transform.SetPositionAndRotation(at-rotation*Vector3.forward*distance,rotation);
                Render(camera,folder,"Gullet-coil-"+name);
            }
            Debug.Log("MAP_EVIDENCE_OK "+folder);
        });
        public static void GulletPreviewViews()=>BatchTools.Run(()=>
        {
            string folder=Folder("C8/cantor-preview/map02");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gullet_Tunnel.unity");
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
            var encounter=Object.FindObjectsByType<Lattice.World.EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Gullet_Cantor");
            // Editor geometry review only: the same factory anatomy/equipment,
            // before play-mode Start. These views are not gameplay evidence.
            var spawn=encounter.spawners[0];
            var animal=Lattice.Combat.ActorFactory.Enemy(spawn.definition,spawn.transform.position+Vector3.right*spawn.radius);
            var point=encounter.previewPoint.position;
            var ships=new GameObject[2];int index=0;
            foreach(var id in new[]{"Taren","Sela"})
            {
                var ship=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+id+"Flight.prefab"));
                ship.transform.position=point+(id=="Sela"?new Vector3(-4,0,-3):Vector3.zero);
                ships[index++]=ship;
            }
            var camera=new GameObject("Evidence camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.backgroundColor=new Color(.025f,.035f,.06f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.fieldOfView=30;camera.aspect=16f/9;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            camera.orthographic=true;
            Shot(camera,folder,"preview-overhead",new Vector3(4,0,785),Quaternion.Euler(90,0,0),43);
            Shot(camera,folder,"preview-fold-section",new Vector3(31,0,767),Quaternion.Euler(12,90,0),18);
            camera.orthographic=false;var rotation=Quaternion.Euler(48,0,0);
            // Fit the review geometry rather than assuming the runtime camera's
            // framing distance. The ordinary replay independently checks that rig.
            var bounds=ModelGeometry.BoundsOf(animal.gameObject);
            foreach(var ship in ships)bounds.Encapsulate(ModelGeometry.BoundsOf(ship));
            var inverse=Quaternion.Inverse(rotation);float distance=26,tan=Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)*.9f;
            for(int corner=0;corner<8;corner++)
            {
                var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                var local=inverse*(p-bounds.center);
                distance=Mathf.Max(distance,Mathf.Abs(local.x)/(tan*camera.aspect)-local.z,Mathf.Abs(local.y)/tan-local.z);
            }
            camera.transform.SetPositionAndRotation(bounds.center-rotation*Vector3.forward*distance,rotation);Render(camera,folder,"preview-shoulder");
            var entry=new Vector3(0,1,744);
            ships[0].transform.position=entry;ships[1].transform.position=entry+new Vector3(-4,0,-3);
            camera.transform.SetPositionAndRotation(entry-rotation*Vector3.forward*26,rotation);Render(camera,folder,"preview-entry");
            Debug.Log("GULLET_PREVIEW_VIEWS_OK "+folder);
        });
        public static void HushwellReview()=>BatchTools.Run(()=>
        {
            string folder=Folder("workshop/hushwell-review");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hushwell.unity");
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.gameObject.SetActive(false);
            var camera = new GameObject("Evidence camera", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.backgroundColor = new Color(.025f, .035f, .06f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            Shot(camera,folder,"Hushwell-overhead",new Vector3(0,0,170),Quaternion.Euler(90,90,0),72);
            Shot(camera,folder,"Hushwell-bellows-chamber",new Vector3(Lattice.World.HushwellLayout.Bellows.x,-8,Lattice.World.HushwellLayout.Bellows.y),Quaternion.Euler(90,0,0),24);
            Shot(camera,folder,"Hushwell-bellows-oblique",new Vector3(Lattice.World.HushwellLayout.Bellows.x,-8,Lattice.World.HushwellLayout.Bellows.y),Quaternion.Euler(40,20,0),24);
            // Review-only stand-in: the boss at its spawn, turned to face the
            // camera as it would face an approaching hero. Never saved.
            var bellows=Lattice.Core.GameCatalog.Find<Lattice.Data.EnemyDef>("BellowsBelow");
            if(bellows!=null&&bellows.prefab!=null)
            {
                var body=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(bellows.prefab);
                body.transform.SetPositionAndRotation(new Vector3(0,-8,268),Quaternion.Euler(0,200,0));
            }
            // The player's own view: the fixed ground camera's pitch, yaw, distance and field of view.
            camera.orthographic=false;camera.fieldOfView=30;var view=Quaternion.Euler(40,20,0);
            foreach(var (name,at) in new[]{("breach",new Vector2(0,4)),("gallery-a",new Vector2(-12,46)),("gallery-b",new Vector2(8,84)),("survey-grid",new Vector2(7,106)),
                ("ramp-down",new Vector2(1,138)),("chamber-1",new Vector2(0,162)),("east-vent",new Vector2(20,164)),("chamber-2",new Vector2(-4,196)),("west-pocket",new Vector2(-34,212)),
                ("chamber-3",new Vector2(1,226)),("bellows-entry",new Vector2(0,244)),("bellows",new Vector2(0,262)),("curling-descent",new Vector2(7,304)),("nursery",new Vector2(14,334))})
            {
                var target=Lattice.World.HushwellLayout.OnFloor(at)+Vector3.up;
                camera.transform.SetPositionAndRotation(target-view*Vector3.forward*19.5f,view);
                Render(camera,folder,"Hushwell-view-"+name);
            }
            Debug.Log("MAP_EVIDENCE_OK " + folder);
        });
        public static void ArenaReview()=>BatchTools.Run(()=>
        {
            string folder=Folder("workshop/arena-review");
            foreach(string zone in new[]{"Arena_Ground","Arena_Flight"})
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.gameObject.SetActive(false);
                var camera = new GameObject("Evidence camera", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                camera.backgroundColor = new Color(.025f, .035f, .06f); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                bool flight=zone=="Arena_Flight";
                Shot(camera,folder,zone+"-overhead",new Vector3(0,0,flight?-5:-6),Quaternion.Euler(90,0,0),flight?34:30);
                if(flight)Shot(camera,folder,zone+"-section",new Vector3(-10,-4,0),Quaternion.Euler(8,0,0),22);
                // The player's own view: each form's camera pitch, yaw, distance and field of view.
                camera.orthographic=false;camera.fieldOfView=30;var view=Quaternion.Euler(flight?48:40,flight?0:20,0);float distance=flight?26:19.5f;
                var stations=flight?new[]{("arrival",new Vector3(0,1,-4)),("targets",new Vector3(0,1,6)),("west-rail-and-cabin",new Vector3(-17,1,-6)),("north-rail",new Vector3(0,1,22)),("entry-lane",new Vector3(0,1,-32))}
                    :new[]{("arrival",new Vector3(0,0,-4)),("operator",new Vector3(10,0,-12)),("targets",new Vector3(0,0,12)),("west-outcrop",new Vector3(-16,0,0)),("east-barrier",new Vector3(16,0,4)),("service-entrance",new Vector3(0,0,-22))};
                foreach(var (name,at) in stations)
                {
                    var target=at+Vector3.up;camera.transform.SetPositionAndRotation(target-view*Vector3.forward*distance,view);
                    Render(camera,folder,zone+"-view-"+name);
                }
            }
            Debug.Log("MAP_EVIDENCE_OK " + folder);
        });
        public static void CinderResidents()=>BatchTools.Run(()=>
        {
            string folder=Folder("workshop/cinder-residents");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hub_CinderHalo.unity");
            var camera = new GameObject("Evidence camera", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.backgroundColor = new Color(.025f, .035f, .06f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            Shot(camera,folder,"Hub_CinderHalo-south-approaches",new Vector3(10,0,-25),Quaternion.Euler(90,0,0),38);
            // Labels stay on: a hailable ship is marked by its crew's name in play.
            camera.orthographic=false;camera.fieldOfView=30;var view=Quaternion.Euler(48,0,0);
            foreach(var (name,at) in new[]{("neve",new Vector3(-5,1,-37)),("oda",new Vector3(17,1,-47)),("ilo",new Vector3(50,1,-11))})
            {camera.transform.SetPositionAndRotation(at-view*Vector3.forward*26,view);Render(camera,folder,"Hub_CinderHalo-view-"+name);}
            Debug.Log("MAP_EVIDENCE_OK " + folder);
        });
        static void Render(Camera camera,string folder,string name)
        {
            var rt = new RenderTexture(1920,1080,24); camera.targetTexture = rt;
            camera.Render(); camera.Render();
            var prior = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            RenderTexture.active = prior; camera.targetTexture = null;
            Object.DestroyImmediate(image); Object.DestroyImmediate(rt);
        }
        static string Folder(string run)
        {
            run=Lattice.Core.DevArgs.Value("-map-evidence-run")??run;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/quality", run));
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality"))+Path.DirectorySeparatorChar;
            if(!folder.StartsWith(root,System.StringComparison.OrdinalIgnoreCase))throw new System.InvalidOperationException("Map evidence must stay under Builds/quality");
            if(Directory.Exists(folder)&&Directory.GetFiles(folder).Length>0)throw new System.InvalidOperationException("Preserve prior map evidence; choose a new -map-evidence-run");
            Directory.CreateDirectory(folder);
            return folder;
        }
        static void Capture(string run,string[] zones=null)
        {
            run=Lattice.Core.DevArgs.Value("-map-evidence-run")??run;
            string folder=Folder(run);
            foreach (string zone in zones??new[] { "Hub_CinderHalo", "Hub_Decks", "TallowApproach", "TallowDrift" })
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + zone + ".unity");
                foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.gameObject.SetActive(false);
                bool exterior = zone == "Hub_CinderHalo" || zone == "TallowApproach";
                var camera = new GameObject("Evidence camera", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                camera.backgroundColor = new Color(.025f, .035f, .06f); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                bool redesigned=run.StartsWith("station/");
                Vector3 center = zone == "Hub_CinderHalo" ? new Vector3(0,0,redesigned?70:15) : zone=="TallowApproach"?new Vector3(0,0,15):Vector3.zero;
                float scale = zone=="Hub_CinderHalo"?(redesigned?150:80):zone=="TallowApproach"?30:zone=="TallowDrift"?18:27;
                if(zone=="Sorrel_Ridges"){center=new Vector3(0,0,90);scale=145;}
                if(zone=="Gullet_Tunnel"){center=new Vector3(0,1,450);scale=500;}
                if(zone=="Arena_Flight")scale=48;
                Shot(camera, folder, zone + "-overhead", center, Quaternion.Euler(90,0,0), scale);
                Shot(camera, folder, zone + "-structure", center, Quaternion.Euler(25,15,0), scale);
                Shot(camera, folder, zone + "-arrival", zone == "Hub_Decks" ? (redesigned?new Vector3(-28,0,-11):new Vector3(-20,0,-3)) : zone == "Hub_CinderHalo" ? (redesigned?new Vector3(-28,1,-28):new Vector3(-20,1,3)) : Vector3.zero, Quaternion.Euler(40,20,0), exterior ? 22 : 7);
                if(zones!=null||zone=="TallowDrift")
                {
                    foreach(var spawn in Object.FindObjectsByType<Lattice.Core.SpawnPoint>(FindObjectsSortMode.None))
                        if(spawn.id=="Arrival"){Shot(camera,folder,zone+"-arrival",spawn.transform.position,Quaternion.Euler(40,20,0),zone=="TallowDrift"?7:zone=="Gullet_Tunnel"||zone=="Arena_Flight"?22:12);break;}
                }
            }
            Debug.Log("MAP_EVIDENCE_OK " + folder);
        }
        static void Shot(Camera camera, string folder, string name, Vector3 center, Quaternion rotation, float scale)
        {
            camera.orthographicSize = scale;
            camera.transform.SetPositionAndRotation(center - rotation * Vector3.forward * 400, rotation);
            var rt = new RenderTexture(1920,1080,24); camera.targetTexture = rt;
            camera.Render(); camera.Render();
            var prior = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            RenderTexture.active = prior; camera.targetTexture = null;
            Object.DestroyImmediate(image); Object.DestroyImmediate(rt);
        }
    }
}
