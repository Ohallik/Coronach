using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>Measures a generated flight hull as the game uses it (November 12, for Idra's cutter). The collision radius is the furthest
    /// vertex from the hull's origin, which is what FlightSocketTests holds HeroCollision.HullRadius to (within 4 cm above it). Idra's two
    /// engines are round wells sunk in her stern deck either side of the winch (tools/meshy/c13_idra_batch.py): they are found as the two
    /// deepest depressions in a top-down height map of the stern third, one each side. The socket goes on each well's stern lip, 3 cm over
    /// the highest plating along the flame's path aft: from the well's floor, the flame FlightThrusters draws straight aft ran into the hull
    /// behind it and showed as a dot from the flight camera. With -hull-stills the hull is drawn from above and from behind with the wells'
    /// centres (green) and the prefab's sockets (red), and from the flight camera on four headings with a full-drive flame from each well's
    /// floor (green) and lip (yellow), to be looked at.
    /// Run: exec-when-green.ps1 -Method Lattice.EditorTools.FlightHullMeasure.Run -Marker FLIGHT_HULL_MEASURE_OK [-Graphics -ExtraArgs @('-hull-stills','name')]</summary>
    public static class FlightHullMeasure
    {
        public static void Run()=>BatchTools.Run(()=>
        {
            string stills=DevArgs.Value("-hull-stills");
            string folder=stills==null?null:Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C2/hull",stills));
            if(folder!=null)Directory.CreateDirectory(folder);
            foreach(string id in new[]{"TarenFlight","SelaFlight","IdraFlight"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+id+".prefab");
                if(prefab==null){Debug.Log("FLIGHT_HULL "+id+" no prefab");continue;}
                Debug.Log(System.FormattableString.Invariant($"FLIGHT_HULL {id} furthest={Furthest(prefab):F3} radius={HeroCollision.HullRadius(id.Replace("Flight","")):F2}"));
                if(id!="IdraFlight")continue;
                var wells=Wells(prefab);
                foreach(var w in wells)Debug.Log(System.FormattableString.Invariant($"FLIGHT_WELL {id} centre=({w.centre.x:F3},{w.centre.y:F3},{w.centre.z:F3}) rim={w.rim:F3} depth={w.depth:F3} radius={w.radius:F3} nearestVertex={w.nearest:F3} lip=({w.lip.x:F3},{w.lip.y:F3},{w.lip.z:F3}) lipNearestVertex={w.lipNearest:F3}"));
                if(folder!=null)Stills(prefab,wells,folder);
            }
            Debug.Log("FLIGHT_HULL_MEASURE_OK");
        });
        /// <summary>The furthest vertex from the hull's origin, in the hull's own space: the stable flight sphere must contain it through any bank or roll.</summary>
        public static float Furthest(GameObject prefab)
        {
            var hull=Object.Instantiate(prefab);
            try{return Points(hull,true).Max(p=>p.magnitude);}
            finally{Object.DestroyImmediate(hull);}
        }
        static Vector3[] Points(GameObject hull,bool local)=>hull.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>
        {var world=f.transform.TransformPoint(v);return local?hull.transform.InverseTransformPoint(world):world;})).ToArray();
        public struct Well{public Vector3 centre,lip;public float rim,depth,radius,nearest,lipNearest;}
        /// <summary>The deepest depression either side of the centre line in the stern third, seen from above.</summary>
        public static Well[] Wells(GameObject prefab)
        {
            var hull=Object.Instantiate(prefab);hull.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            try
            {
                var triangles=new List<(Vector3 a,Vector3 b,Vector3 c)>();
                foreach(var f in hull.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=f.sharedMesh;var v=mesh.vertices.Select(p=>f.transform.TransformPoint(p)).ToArray();var t=mesh.triangles;
                    for(int i=0;i+2<t.Length;i+=3)triangles.Add((v[t[i]],v[t[i+1]],v[t[i+2]]));
                }
                var all=triangles.SelectMany(t=>new[]{t.a,t.b,t.c}).ToArray();
                float minX=all.Min(p=>p.x),maxX=all.Max(p=>p.x),minZ=all.Min(p=>p.z),maxZ=all.Max(p=>p.z),floor=all.Min(p=>p.y),top=all.Max(p=>p.y);
                const float cell=.02f;float sternEnd=minZ+(maxZ-minZ)*.4f;
                int nx=Mathf.CeilToInt((maxX-minX)/cell)+1,nz=Mathf.CeilToInt((sternEnd-minZ)/cell)+1;
                var height=new float[nx,nz];var covered=new bool[nx,nz];for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)height[i,j]=floor;
                // The top surface: the highest face over each cell.
                foreach(var (a,b,c) in triangles)
                {
                    float x0=Mathf.Min(a.x,Mathf.Min(b.x,c.x)),x1=Mathf.Max(a.x,Mathf.Max(b.x,c.x)),z0=Mathf.Min(a.z,Mathf.Min(b.z,c.z)),z1=Mathf.Max(a.z,Mathf.Max(b.z,c.z));
                    if(z0>sternEnd)continue;
                    float d=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(d)<1e-9f)continue;
                    for(int i=Mathf.Max(0,Mathf.FloorToInt((x0-minX)/cell));i<=Mathf.Min(nx-1,Mathf.CeilToInt((x1-minX)/cell));i++)
                    for(int j=Mathf.Max(0,Mathf.FloorToInt((z0-minZ)/cell));j<=Mathf.Min(nz-1,Mathf.CeilToInt((z1-minZ)/cell));j++)
                    {
                        float x=minX+i*cell,z=minZ+j*cell;
                        float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/d,w=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/d,s=1-u-w;
                        if(u<-.001f||w<-.001f||s<-.001f)continue;
                        height[i,j]=Mathf.Max(height[i,j],u*a.y+w*b.y+s*c.y);covered[i,j]=true;
                    }
                }
                // A well: a cell well below every point of a ring around it. Score each cell by the lowest of the ring over it, at the ring
                // radius that scores best; the well is the best cell each side, and its extent the cells under that ring's lowest point.
                var wells=new List<Well>();
                foreach(int sign in new[]{-1,1})
                {
                    float best=0;Well found=default;
                    for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)
                    {
                        // Only cells the hull covers: off its edge the map reads nothing, and a ring of hull round that is no well.
                        // An engine well is sunk in the deck: its floor stands in the hull's upper half. Lower down, the hull's flanks and
                        // the arms along them make false "wells" between their edges.
                        float x=minX+i*cell;if(x*sign<(maxX-minX)*.08f||!covered[i,j]||height[i,j]<floor+(top-floor)*.5f)continue;
                        foreach(float ring in new[]{.12f,.16f,.2f,.25f,.3f})
                        {
                            float lowest=float.PositiveInfinity;bool inside=true;
                            for(int k=0;k<24&&inside;k++)
                            {
                                float angle=k*Mathf.PI*2/24;int ri=i+Mathf.RoundToInt(Mathf.Cos(angle)*ring/cell),rj=j+Mathf.RoundToInt(Mathf.Sin(angle)*ring/cell);
                                if(ri<0||ri>=nx||rj<0||rj>=nz||!covered[ri,rj]){inside=false;break;}
                                lowest=Mathf.Min(lowest,height[ri,rj]);
                            }
                            if(!inside)continue;
                            float depth=lowest-height[i,j];
                            if(depth>best){best=depth;found=new Well{rim=lowest,depth=depth,radius=ring,centre=new Vector3(x,height[i,j],minZ+j*cell)};}
                        }
                    }
                    if(best<=.03f)continue;
                    // The well's floor: every cell within the ring that lies below its rim by over half the depth; the socket goes at its middle.
                    var floorCells=new List<Vector3>();
                    for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)
                    {
                        float x=minX+i*cell,z=minZ+j*cell;
                        if(new Vector2(x-found.centre.x,z-found.centre.z).magnitude<=found.radius&&height[i,j]<found.rim-found.depth*.5f)floorCells.Add(new Vector3(x,height[i,j],z));
                    }
                    if(floorCells.Count>0){var middle=floorCells.Aggregate(Vector3.zero,(p,q)=>p+q)/floorCells.Count;found.centre=new Vector3(middle.x,floorCells.Min(p=>p.y),middle.z);}
                    found.nearest=all.Min(p=>Vector3.Distance(p,found.centre));
                    // The well's mouth at its stern edge: where exhaust leaving the well would pass over the deck.
                    // Three centimetres over the highest deck along the flame's path aft (FlightThrusters draws it 0.9 m long at full
                    // drive), so the exhaust starts in the open and no plating behind the well swallows it.
                    int li=Mathf.Clamp(Mathf.RoundToInt((found.centre.x-minX)/cell),0,nx-1);float aft=float.NegativeInfinity;
                    for(float back=0;back<=.45f;back+=cell){int lj=Mathf.RoundToInt((found.centre.z-found.radius-back-minZ)/cell);if(lj>=0&&lj<nz&&covered[li,lj])aft=Mathf.Max(aft,height[li,lj]);}
                    found.lip=new Vector3(found.centre.x,aft+.03f,found.centre.z-found.radius);found.lipNearest=all.Min(p=>Vector3.Distance(p,found.lip));
                    wells.Add(found);
                }
                return wells.ToArray();
            }
            finally{Object.DestroyImmediate(hull);}
        }
        static void Stills(GameObject prefab,Well[] wells,string folder)
        {
            var measured=wells.Select(w=>w.centre).ToArray();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.62f,.68f);
            var light=new GameObject("Hull key",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,-30,0);
            var hull=Object.Instantiate(prefab);hull.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var sockets=hull.GetComponent<FlightSockets>();
            void Mark(Vector3 at,Color color,float size)
            {
                var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);ball.transform.position=at;ball.transform.localScale=Vector3.one*size;
                var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.color=color;ball.GetComponent<Renderer>().sharedMaterial=m;
            }
            foreach(var p in measured)Mark(p,Color.green,.12f);
            if(sockets!=null){foreach(var e in sockets.engines)Mark(e.position,Color.red,.08f);if(sockets.muzzle!=null)Mark(sockets.muzzle.position,Color.cyan,.08f);}
            var camera=new GameObject("Hull camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.16f);
            camera.orthographicSize=2.1f;camera.transform.position=new Vector3(0,6,0);camera.transform.rotation=Quaternion.Euler(90,0,0);
            FacingAudit.Capture(camera,Path.Combine(folder,prefab.name+"-top.png"));
            camera.orthographicSize=1.6f;camera.transform.position=new Vector3(2.4f,3.2f,-4.2f);camera.transform.LookAt(new Vector3(0,.5f,-.9f));
            FacingAudit.Capture(camera,Path.Combine(folder,prefab.name+"-stern.png"));
            // The exhaust as FlightThrusters draws it at full drive (0.9 m straight aft, 12 cm wide), from each well's floor (green)
            // and from its mouth's stern edge (yellow), seen from the flight camera (48 degrees down) with the ship on four headings.
            void Flame(Vector3 from,Color color)
            {
                var rod=GameObject.CreatePrimitive(PrimitiveType.Cylinder);rod.transform.position=from+Vector3.back*.45f;rod.transform.rotation=Quaternion.Euler(90,0,0);rod.transform.localScale=new Vector3(.12f,.45f,.12f);
                var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.color=color;rod.GetComponent<Renderer>().sharedMaterial=m;
            }
            foreach(var w in wells){Flame(w.centre,Color.green);Flame(w.lip,Color.yellow);}
            var pivot=new GameObject("Flight view pivot").transform;
            foreach(float heading in new[]{0f,90f,180f,270f})
            {
                camera.orthographicSize=2.4f;var view=Quaternion.Euler(48,heading,0);
                camera.transform.rotation=view;camera.transform.position=new Vector3(0,.5f,0)-view*Vector3.forward*26;
                FacingAudit.Capture(camera,Path.Combine(folder,prefab.name+"-flight-"+heading+".png"));
            }
            Object.DestroyImmediate(pivot.gameObject);
            Debug.Log("FLIGHT_HULL_STILLS "+folder);
        }
    }
}
