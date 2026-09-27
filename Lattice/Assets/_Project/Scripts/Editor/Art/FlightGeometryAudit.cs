using System;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    public static class FlightGeometryAudit
    {
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-flight-geometry-run")??"hull-geometry-01";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new InvalidOperationException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C4",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve prior hull evidence");
            Directory.CreateDirectory(folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
            var light=new GameObject("Hull audit light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(40,-25,0);
            var camera=new GameObject("Hull audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.1f,.13f);
            foreach(string id in new[]{"TarenFlight","SelaFlight"})
            {
                var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+id+".prefab"));
                var points=ModelGeometry.Points(body);var bounds=ModelGeometry.BoundsOf(body);
                var sockets=body.GetComponent<FlightSockets>();
                var attachments=sockets!=null?new[]{sockets.muzzle}.Concat(sockets.engines).ToArray():Array.Empty<Transform>();
                File.WriteAllText(Path.Combine(folder,id+"-geometry.json"),JsonConvert.SerializeObject(new{
                    id,minimum=new[]{bounds.min.x,bounds.min.y,bounds.min.z},maximum=new[]{bounds.max.x,bounds.max.y,bounds.max.z},
                    sockets=attachments.Select(t=>new{name=t.name,point=new[]{t.position.x,t.position.y,t.position.z},nearestVertex=points.Min(p=>Vector3.Distance(p,t.position))}),
                    points=points.Select(p=>new[]{p.x,p.y,p.z}).ToArray()},Formatting.Indented));
                foreach(var socket in attachments)
                {
                    var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Audit "+socket.name;marker.transform.SetParent(socket,false);marker.transform.localScale=Vector3.one*.07f;
                    UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
                    var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",socket==sockets.muzzle?Color.magenta:Color.green);marker.GetComponent<Renderer>().sharedMaterial=material;
                    var direction=new GameObject("Socket direction",typeof(LineRenderer)).GetComponent<LineRenderer>();direction.transform.SetParent(socket,false);
                    direction.sharedMaterial=material;direction.positionCount=2;direction.startWidth=direction.endWidth=.016f;
                    direction.SetPosition(0,socket.position);direction.SetPosition(1,socket.position+socket.forward*.22f);
                }
                camera.orthographicSize=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))*.62f;
                foreach(string view in new[]{"top","front","back","side"})
                {
                    Vector3 direction=view=="top"?new Vector3(0,1,.001f):view=="front"?new Vector3(.15f,.2f,1):view=="back"?new Vector3(.15f,.2f,-1):new Vector3(1,.15f,0);
                    camera.transform.position=bounds.center+direction.normalized*20;camera.transform.LookAt(bounds.center);
                    FacingAudit.Capture(camera,Path.Combine(folder,id+"-"+view+".png"));
                }
                UnityEngine.Object.DestroyImmediate(body);
            }
            Debug.Log("FLIGHT_GEOMETRY_AUDIT_OK");
        });
    }
}
