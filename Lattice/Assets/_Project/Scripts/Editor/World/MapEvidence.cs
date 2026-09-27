using System.IO;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>Read-only scene review renders. Never saves or regenerates maps.</summary>
    public static class MapEvidence
    {
        public static void Baseline() => BatchTools.Run(() => Capture("C0/maps-before"));
        public static void StationAfter() => BatchTools.Run(() => Capture("station/maps-after"));
        public static void StationBlockout() => BatchTools.Run(() => Capture("station/blockout-03"));
        public static void WorkshopOthers() => BatchTools.Run(() => Capture("workshop/maps-before",new[]{"Sorrel_Ridges","Gullet_Tunnel","Arena_Ground","Arena_Flight"}));
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
        static void Capture(string run,string[] zones=null)
        {
            run=Lattice.Core.DevArgs.Value("-map-evidence-run")??run;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/quality", run));
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality"))+Path.DirectorySeparatorChar;
            if(!folder.StartsWith(root,System.StringComparison.OrdinalIgnoreCase))throw new System.InvalidOperationException("Map evidence must stay under Builds/quality");
            if(Directory.Exists(folder)&&Directory.GetFiles(folder).Length>0)throw new System.InvalidOperationException("Preserve prior map evidence; choose a new -map-evidence-run");
            Directory.CreateDirectory(folder);
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
