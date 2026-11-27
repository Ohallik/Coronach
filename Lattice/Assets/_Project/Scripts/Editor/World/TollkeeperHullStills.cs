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
    /// <summary>Round ten (W1-mooring-05): the Tollkeeper's hull as the game draws it, before and after its blotched faces are mended. Opens Last Mooring with the live pipeline and its local art,
    /// shows the carrier the room draws (the fight's body is the same prefab at the same place, Mooring.TollkeeperAt), and renders it at 1920 by 1080 from the two cameras nov25-whole had in its
    /// fight (s65/056 docked on its arm, s65/064 in flight) with the room round it, then alone on grey from the fight camera's side, from above, from the north and from the west. Points named with
    /// -hull-probes view:x,y;view:x,y (y down from the top) log the hull's triangle and texture coordinate under them. Nothing is saved. Run:
    /// exec-when-green.ps1 -Method Lattice.EditorTools.TollkeeperHullStills.Render -Marker TOLLKEEPER_STILLS_OK -Graphics -ExtraArgs @('-look-run','r10-hull-before').</summary>
    public static class TollkeeperHullStills
    {
        static readonly (string name,Vector3 eye,bool flight)[] Captures=
        {
            ("dock056",new Vector3(-10.078f,23.282f,417.162f),false),
            ("fight064",new Vector3(10f,20.235f,423.144f),true),
        };

        public static void Render()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??throw new ArgumentException("-look-run <name> is required");
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(System.IO.Directory.Exists(folder))throw new InvalidOperationException("Preserve earlier look run: "+folder);
            System.IO.Directory.CreateDirectory(folder);
            var probes=new List<(string view,Vector2 at)>();
            foreach(var item in (DevArgs.Value("-hull-probes")??"").Split(';',StringSplitOptions.RemoveEmptyEntries))
            {
                var parts=item.Split(':');var xy=parts[1].Split(',');
                probes.Add((parts[0],new Vector2(float.Parse(xy[0],System.Globalization.CultureInfo.InvariantCulture),float.Parse(xy[1],System.Globalization.CultureInfo.InvariantCulture))));
            }
            var before=GraphicsSettings.defaultRenderPipeline;var beforeQuality=QualitySettings.renderPipeline;
            var pipeline=Resources.Load<RenderPipelineAsset>("LocalBiomes/LocalPipeline")??throw new InvalidOperationException("TOLLKEEPER_STILLS the live pipeline is missing");
            var ground=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
            var flight=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Flight.asset");
            int count=0;
            try
            {
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/LastMooring.unity");
                foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                foreach(var patch in UnityEngine.Object.FindObjectsByType<BiomePatch>(FindObjectsSortMode.None))patch.ApplyLocalArt();
                var def=AssetDatabase.FindAssets("t:ZoneDef").Select(g=>AssetDatabase.LoadAssetAtPath<ZoneDef>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(d=>d.scene=="LastMooring"||d.id=="LastMooring");
                if(def!=null)RenderSettings.skybox=def.skybox;
                // The hull: every renderer drawn with the Tollkeeper's own material, shown with its parents.
                var hull=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                    .Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.name=="Tollkeeper_Toon")).ToList();
                if(hull.Count==0)throw new InvalidOperationException("TOLLKEEPER_STILLS no renderer carries Tollkeeper_Toon in LastMooring");
                foreach(var r in hull){for(var t=r.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);r.enabled=true;Debug.Log("TOLLKEEPER_STILLS hull "+PathOf(r.transform)+" bounds="+r.bounds);}
                var drawn=hull[0].bounds;foreach(var r in hull)drawn.Encapsulate(r.bounds);
                var riggers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
                    .Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.name=="TollkeeperOutrigger_Toon")).ToList();
                var colliders=new List<MeshCollider>();
                foreach(var r in hull){var f=r.GetComponent<MeshFilter>();if(f==null||f.sharedMesh==null)continue;var c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;colliders.Add(c);}
                Physics.SyncTransforms();
                Shader.SetGlobalVector("_SkyTurn",Vector4.zero);

                // With the room round it, from the capture's own cameras.
                foreach(var shot in Captures)
                {
                    var profile=shot.flight?flight:ground;
                    var camera=Camera(profile.fov,null);camera.transform.SetPositionAndRotation(shot.eye,Quaternion.Euler(profile.pitch,profile.yaw,0));
                    Shoot(camera,folder,shot.name,probes,colliders);count++;
                }
                // Alone on grey: everything else is hidden.
                var hidden=new List<Renderer>();
                var keep=new HashSet<Renderer>(hull.Cast<Renderer>().Concat(riggers));
                foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.enabled&&!keep.Contains(r)){r.enabled=false;hidden.Add(r);}
                foreach(var r in riggers)r.enabled=false;
                var views=new (string name,float pitch,float yaw,float zoom)[]{("south",flight.pitch,0,1),("south2x",flight.pitch,0,2.2f),("above",89.5f,0,1),("north",flight.pitch,180,1),("west",25,90,1)};
                foreach(var v in views)
                {
                    var camera=Camera(flight.fov/v.zoom,new Color(.30f,.32f,.36f));
                    var rotation=Quaternion.Euler(v.pitch,v.yaw,0);var forward=rotation*Vector3.forward;
                    // Framed whole at the fight camera's own lens; a zoomed view stands as far off with a narrower lens.
                    float vHalf=Mathf.Tan(flight.fov*.5f*Mathf.Deg2Rad),hHalf=vHalf*camera.aspect;
                    float across=v.yaw==90?drawn.extents.z:drawn.extents.x,deep=v.pitch>80?(v.yaw==90?drawn.extents.x:drawn.extents.z):drawn.extents.y;
                    float distance=Mathf.Max(across/hHalf,deep/vHalf)*1.1f+drawn.extents.magnitude*.5f;
                    camera.transform.SetPositionAndRotation(drawn.center-forward*distance,rotation);
                    Shoot(camera,folder,v.name,probes,colliders);count++;
                }
                foreach(var c in colliders)if(c!=null)UnityEngine.Object.DestroyImmediate(c);
                Debug.Log($"TOLLKEEPER_STILLS_OK count={count} hull={hull.Count} riggers={riggers.Count} drawn={drawn} folder={folder}");
            }
            finally{GraphicsSettings.defaultRenderPipeline=before;QualitySettings.renderPipeline=beforeQuality;Shader.SetGlobalVector("_SkyTurn",Vector4.zero);}
        });

        static Camera Camera(float fov,Color? solid)
        {
            var camera=new GameObject("Tollkeeper still camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.clearFlags=solid==null&&RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=solid??new Color(.02f,.03f,.06f);
            camera.nearClipPlane=.3f;camera.farClipPlane=2000;camera.fieldOfView=fov;camera.aspect=16f/9;
            var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
            return camera;
        }

        static void Shoot(Camera camera,string folder,string name,List<(string view,Vector2 at)> probes,List<MeshCollider> colliders)
        {
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();camera.Render();
            var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),image.EncodeToPNG());
            RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
            var ours=new HashSet<Collider>(colliders);
            foreach(var p in probes.Where(p=>p.view==name))
            {
                var ray=camera.ViewportPointToRay(new Vector3(p.at.x/1920f,1-p.at.y/1080f,0));
                var hits=Physics.RaycastAll(ray,3000,~0,QueryTriggerInteraction.Collide).Where(h=>ours.Contains(h.collider)).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0){Debug.Log($"TOLLKEEPER_PROBE {name} ({p.at.x},{p.at.y}) nothing");continue;}
                var h=hits[0];
                Debug.Log($"TOLLKEEPER_PROBE {name} ({p.at.x},{p.at.y}) tri={h.triangleIndex} uv=({h.textureCoord.x:F4},{h.textureCoord.y:F4}) at={h.point.ToString("F2")} normal={h.normal.ToString("F2")}");
            }
            Debug.Log($"TOLLKEEPER_STILL {name} eye={camera.transform.position.ToString("F1")} fov={camera.fieldOfView:F1}");
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }

        static string PathOf(Transform t){var names=new List<string>();for(var at=t;at!=null;at=at.parent)names.Insert(0,at.name);return string.Join("/",names);}
    }
}
