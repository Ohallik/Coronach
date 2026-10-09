using System.Collections.Generic;
using System.IO;
using Lattice.Core;
using Lattice.Data;
using Lattice.Combat;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
namespace Lattice.EditorTools
{
    /// <summary>
    /// The Gullet as a Choir's interior passage, following GulletProfile: a mouth
    /// open to space, entry canal, feeding chamber, slalom throat with a salvage
    /// eddy, nursery gate chamber, the Cantor's coil and the exit valve.
    /// Encounter, membrane, spawn, cache and flag identities are unchanged.
    /// </summary>
    public static class GulletBuilder
    {
        public static void Build()=>BatchTools.Run(()=>{WorldBuilder.Prepare();BuildZone();Debug.Log("GULLET_OK");});
        /// <summary>Rebuild only this scene; shared definitions and other maps stay untouched.</summary>
        public static void Rebuild()=>BatchTools.Run(()=>{BuildZone();Debug.Log("GULLET_REBUILT_OK");});
        /// <summary>Update the coil's shell and moorings without regenerating gameplay objects.</summary>
        public static void RefreshCoil()=>BatchTools.Run(()=>
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gullet_Tunnel.unity");
            foreach(var root in scene.GetRootGameObjects())if(root.name=="Collar anchor clamp")Object.DestroyImmediate(root);
            Shell();GulletCoilArt.Moorings();VerifyWalls();
            AssetDatabase.SaveAssets();WorldBuilder.Save(scene,"Gullet_Tunnel");Debug.Log("GULLET_COIL_REFRESHED_OK");
        });
        static System.Random random;
        static float Range(float a,float b)=>a+(float)random.NextDouble()*(b-a);
        public static void BuildZone()
        {
            random=new System.Random(1905);
            var scene=WorldBuilder.Begin("Gullet_Tunnel");WorldBuilder.Spawn("Arrival",new(0,1,8));
            WorldBuilder.Spawn("Performance",new(GulletProfile.RouteX(GulletProfile.PerformanceZ),1,GulletProfile.PerformanceZ));
            var path=new GameObject("Gullet travel line",typeof(SplineContainer)).GetComponent<SplineContainer>();var spline=new Spline();
            for(float z=0;z<=GulletProfile.ExitZ;z+=15)spline.Add(new BezierKnot(new float3(GulletProfile.RouteX(z),1,z)));
            spline.SetTangentMode(TangentMode.AutoSmooth);path.Spline=spline;
            Shell();Walls();Mouth();Valves();Folds();

            // Feeding chamber: pods grow from both bellies where darts feed.
            foreach(float z in new[]{128f,150f,176f,200f,222f})
            {Cluster(z,-1,3,5.5f,7.5f);Cluster(z+11,1,2,5f,7f);}
            // Nursery gate chamber: clustered egg pods, densest around the gate.
            foreach(float z in new[]{530f,556f,582f,608f,634f,660f})
            {Cluster(z,-1,z>590?5:4,4f,6f);Cluster(z+12,1,z>590?5:4,4f,6f);}
            foreach(int side in new[]{-1,1})Pod(684,side,8.5f,-1);
            GulletCoilArt.Moorings();
            // Salvage eddy: the current slows in the right-hand pocket and debris settles there.
            float eddyX=GulletProfile.RightEdge(GulletProfile.CacheZ)-9;
            // It settles at the back of the pocket, clear of the mines pilots must
            // clear from every side.
            var wreck=WorldBuilder.Piece("Skiff",new Vector3(eddyX+4.5f,.4f,GulletProfile.CacheZ+12),new Vector3(4,1.5f,6),"Rock");wreck.name="Lost skiff in the eddy";
            wreck.transform.rotation=Quaternion.Euler(8,118,14);
            foreach(var (dx,dz) in new[]{(7.5f,6.5f),(6f,9f),(3f,17f)})
                WorldBuilder.Piece("DeckCrate",new Vector3(eddyX+dx,.5f,GulletProfile.CacheZ+dz),new Vector3(1.4f,1.1f,1.4f),"Rock").transform.rotation=Quaternion.Euler(0,Range(0,360),Range(-12,12));

            string[] anatomy={"FEEDING CHAMBER","SLALOM THROAT","NURSERY GATE"};
            float[][] volumes={new[]{58f,110f},new[]{28f,150f},new[]{50f,120f}};
            for(int chamber=0;chamber<3;chamber++)
            {
                float z=GulletProfile.Chambers[chamber],valve=GulletProfile.Valves[chamber];
                // Sized to the pinch: the sphincter walls, not the void outside, hold its edges.
                var membrane=WorldBuilder.Membrane("Choir membrane "+chamber,new(GulletProfile.Center(valve),2,valve),GulletProfile.Width(valve)*.97f,false);
                var encounter=WorldBuilder.Encounter("Gullet_Chamber_"+chamber,new(GulletProfile.Center(z),1,z-10),new(volumes[chamber][0],6,volumes[chamber][1]),"ChoristerDart",6,new[]{membrane});
                var spawners=new List<Spawner>(encounter.spawners);
                // Mines lodge where the current eddies: in the pocket or behind a fold.
                float mineZ=chamber==1?GulletProfile.CacheZ-4:z+34;
                float mineX=chamber==1?eddyX-4:GulletProfile.RouteX(mineZ);
                foreach(var extra in new[]{("ChoristerDrifter",2,new Vector3(GulletProfile.RouteX(z+15),1,z+15)),("Shellmine",4,new Vector3(mineX,1,mineZ))})
                {
                    var go=new GameObject("Spawn_"+extra.Item1,typeof(Spawner));go.transform.position=extra.Item3;var spawn=go.GetComponent<Spawner>();
                    spawn.definition=GameCatalog.Find<EnemyDef>(extra.Item1);spawn.count=extra.Item2;spawn.radius=extra.Item1=="Shellmine"?6:9;spawners.Add(spawn);
                }
                encounter.spawners=spawners.ToArray();
                WorldBuilder.Label(anatomy[chamber],new(GulletProfile.Center(z),-1,z-40));
            }
            var cache=WorldBuilder.Piece("NeveCache",new(eddyX+2,1,GulletProfile.CacheZ),new(2,2,2),"Taren").AddComponent<SalvageField>();cache.prompt="Neve's salvage cache";
            var cacheTrigger=cache.gameObject.GetComponent<BoxCollider>();cacheTrigger.isTrigger=true;cacheTrigger.size=Vector3.one*2;
            WorldBuilder.Label("SALVAGE EDDY",new(eddyX,-1,GulletProfile.CacheZ-16));
            // The coil is wide enough for both ships and the serpent to turn.
            var cantor=WorldBuilder.Encounter("Gullet_Cantor",new(GulletProfile.Center(GulletProfile.CantorZ),1,GulletProfile.CantorZ),new(66,6,58),"Cantor",1);
            GulletPreviewStaging.Configure(cantor);
            WorldBuilder.Label("THE CANTOR'S COIL",new(0,-1,GulletProfile.CantorZ-50));
            WorldBuilder.Warp("Exit warp — Tallow Drift",new(GulletProfile.Center(GulletProfile.ExitZ),1,GulletProfile.ExitZ),"TallowApproach","bossdown.Cantor");
            VerifyWalls();
            WorldBuilder.Save(scene,"Gullet_Tunnel");
        }
        static void Cluster(float z,int side,int count,float low,float high)
        {for(int i=0;i<count;i++)Pod(z+Range(-4,4),side,Range(low,high),Range(.6f,2.4f));}
        static void Pod(float z,int side,float height,float inset=1.2f)
        {
            float edge=side<0?GulletProfile.LeftEdge(z):GulletProfile.RightEdge(z);
            var pod=WorldBuilder.Piece("ChoirPod",new Vector3(edge-side*(inset+height*.3f),height*.3f,z),new Vector3(3,height,4),"Emission");
            pod.transform.rotation=Quaternion.Euler(Range(-8,8),side<0?90:-90,side*Range(10,22));
        }
        static float Yaw(float z,int side)
        {
            float a=side<0?GulletProfile.LeftEdge(z-2):GulletProfile.RightEdge(z-2),b=side<0?GulletProfile.LeftEdge(z+2):GulletProfile.RightEdge(z+2);
            return Mathf.Atan2(b-a,4)*Mathf.Rad2Deg;
        }
        static void Walls()
        {
            // Ribs follow the curving wall at varied spacing and height: tissue, not a fence.
            foreach(int side in new[]{-1,1})
                for(float z=GulletProfile.Start+6;z<GulletProfile.End-4;z+=Range(8.5f,12.5f))
                {
                    if(System.Array.Exists(GulletProfile.Valves,v=>Mathf.Abs(v-z)<7))continue;
                    float edge=side<0?GulletProfile.LeftEdge(z):GulletProfile.RightEdge(z);
                    var rib=WorldBuilder.Piece(random.Next(2)==0?"GulletWallA":"GulletWallB",new Vector3(edge,2,z),new Vector3(1,Range(5.2f,7.2f),11),"Rock");
                    rib.transform.rotation=Quaternion.Euler(0,Yaw(z,side)+Range(-4,4),0);
                    // The shell is the wall; a rib may lap onto it but never stand in
                    // the passage. Where the wall bends, a straight rib's ends meet a
                    // different edge than its centre, so test its whole collision
                    // footprint against the edge at each point and push out by the worst.
                    var box=rib.GetComponent<BoxCollider>();float worst=0;
                    for(int i=0;i<=8;i++)for(int j=0;j<=8;j++)
                    {
                        var local=box.center+Vector3.Scale(box.size*.5f,new Vector3(Mathf.Lerp(-1,1,i/8f),0,Mathf.Lerp(-1,1,j/8f)));
                        var p=rib.transform.TransformPoint(local);
                        float wall=side<0?GulletProfile.LeftEdge(p.z):GulletProfile.RightEdge(p.z);
                        worst=Mathf.Max(worst,side*((wall-side*.4f)-p.x));
                    }
                    if(worst>0)rib.transform.position+=Vector3.right*side*worst;
                }
        }
        static void Mouth()
        {
            // Heavy lip folds frame the opening to space, so the arrival reads as an entrance.
            foreach(int side in new[]{-1,1})
                for(int i=0;i<3;i++)
                {
                    float z=GulletProfile.Start+2+i*6;float edge=side<0?GulletProfile.LeftEdge(z):GulletProfile.RightEdge(z);
                    var lip=WorldBuilder.Piece(i%2==0?"GulletWallA":"GulletWallB",new Vector3(edge+side*1.5f,2.5f,z),new Vector3(1,9-i,11),"Rock");lip.name="Mouth lip fold";
                    lip.transform.rotation=Quaternion.Euler(0,Yaw(z,side)-side*(35-i*10),side*8);
                }
        }
        static void Valves()
        {
            // Sphincter lips: thick folds on both sides pinch the passage around each membrane.
            foreach(float valve in GulletProfile.Valves)
                foreach(int side in new[]{-1,1})
                    foreach(float offset in new[]{-4f,4f})
                    {
                        float z=valve+offset;float edge=side<0?GulletProfile.LeftEdge(z):GulletProfile.RightEdge(z);
                        var lip=WorldBuilder.Piece("GulletWallA",new Vector3(edge-side*1.2f,2.2f,z),new Vector3(1,7.4f,11),"Rock");lip.name="Valve lip";
                        lip.transform.rotation=Quaternion.Euler(0,side*(offset<0?-28:28),0);
                    }
            foreach(float z in new[]{GulletProfile.ExitZ-10,GulletProfile.ExitZ+6})
                foreach(int side in new[]{-1,1})
                {
                    float edge=side<0?GulletProfile.LeftEdge(z):GulletProfile.RightEdge(z);
                    var lip=WorldBuilder.Piece("GulletWallB",new Vector3(edge-side,2.2f,z),new Vector3(1,7,11),"Rock");lip.name="Exit valve lip";
                    lip.transform.rotation=Quaternion.Euler(0,side*(z<GulletProfile.ExitZ?-30:30),0);
                }
        }
        static void Folds()
        {
            // Tissue folds reach across the throat from alternating walls; the route weaves between them.
            foreach(var fold in GulletProfile.Folds)
            {
                float edge=fold.side<0?GulletProfile.LeftEdge(fold.z):GulletProfile.RightEdge(fold.z);
                for(int i=0;i<2;i++)
                {
                    var ridge=WorldBuilder.Piece(i==0?"GulletWallA":"GulletWallB",new Vector3(edge,2,fold.z+i*1.8f),new Vector3(1,6.4f-i*.6f,11),"Rock");
                    ridge.name="Throat fold";ridge.transform.rotation=Quaternion.Euler(0,90+fold.side*12,0);
                    // Place by the measured tip, not the nominal size: the fold reaches
                    // exactly its planned depth, so the route's clearance holds.
                    var bounds=ModelGeometry.BoundsOf(ridge);float tip=fold.side>0?bounds.min.x:bounds.max.x;
                    float wanted=edge-fold.side*fold.depth*(i==0?1:.7f);
                    ridge.transform.position+=Vector3.right*(wanted-tip);
                }
            }
        }
        static void VerifyWalls()
        {
            // Every flight-height sample of the travel line must see walls on both sides.
            Physics.SyncTransforms();int samples=0;
            for(float z=5;z<GulletProfile.End-8;z+=2.5f)for(int sign=-1;sign<=1;sign+=2)
            {
                float x=GulletProfile.RouteX(z),edge=sign<0?GulletProfile.LeftEdge(z):GulletProfile.RightEdge(z);
                if(!Physics.Raycast(new Vector3(x,1,z),Vector3.right*sign,Mathf.Abs(edge-x)+5,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                    throw new System.Exception("FAILED: Gullet wall gap at z="+z+" side="+sign);
                samples++;
            }
            Debug.Log("GULLET_WALLS_OK samples="+samples);
        }
        static void Shell()
        {
            // Open upper shell: a readable cutaway diorama, with separate left and right bellies.
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();int sides=48;
            var stations=new List<float>();for(float z=GulletProfile.Start;z<GulletProfile.End;z+=z>=705&&z<874?1.5f:4)stations.Add(z);
            stations.Add(GulletProfile.End);
            float alongUv=GulletProfile.Start/20;
            for(int step=0;step<stations.Count;step++)
            {
                float z=stations[step],c=GulletProfile.Center(z),left=GulletProfile.Left(z),right=GulletProfile.Right(z);
                if(step>0)alongUv+=(z-stations[step-1])/Mathf.Lerp(20,9,GulletCoilArt.Blend((z+stations[step-1])*.5f));
                for(int side=0;side<=sides;side++)
                {
                    float angle=side/(float)sides*Mathf.PI,cos=Mathf.Cos(angle);
                    float x=c+cos*(cos>=0?right:left),blend=GulletCoilArt.Blend(z);
                    vertices.Add(new Vector3(x,GulletCoilArt.Height(x,z),z));
                    // Retain the other organs' original scale; the broad coil uses
                    // metre-scaled cells and sculpted relief instead of giant flat cells.
                    // Keep longitudinal UVs continuous; blending absolute z scales
                    // creates a compressed striped band at the end of the organ.
                    uv.Add(new Vector2(Mathf.Lerp(side/(float)sides*4,2-(x-c)/9,blend),alongUv));
                    colors.Add(GulletCoilArt.Tissue(x,z));
                    if(step==stations.Count-1||side==sides)continue;
                    int a=step*(sides+1)+side,b=a+sides+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});
                }
            }
            // Tint changes happen at the valves, where each sphincter hides the seam.
            float[] bounds={GulletProfile.Valves[0],GulletProfile.Valves[1],GulletProfile.Valves[2],874};
            var parts=new List<int>[bounds.Length+1];for(int i=0;i<parts.Length;i++)parts[i]=new List<int>();
            for(int t=0;t<triangles.Count;t+=3)
            {
                float z=(vertices[triangles[t]].z+vertices[triangles[t+1]].z+vertices[triangles[t+2]].z)/3;int part=0;while(part<bounds.Length&&z>=bounds[part])part++;
                parts[part].AddRange(new[]{triangles[t],triangles[t+1],triangles[t+2]});
            }
            var mesh=new Mesh{name="Gullet cutaway shell"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.subMeshCount=parts.Length;
            for(int i=0;i<parts.Length;i++)mesh.SetTriangles(parts[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();
            Directory.CreateDirectory("Assets/_Project/Art/WorldMeshes");const string path="Assets/_Project/Art/WorldMeshes/GulletShell.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
            var go=GameObject.Find("Gullet membrane shell")??new GameObject("Gullet membrane shell",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.isStatic=true;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshCollider>().sharedMesh=null;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterials=SectionMaterials();
        }
        // One generated membrane, tinted per organ: warm feeding tissue, dark
        // muscular throat, luminous nursery, deep coil and a bright exit.
        static Material[] SectionMaterials()
        {
            var source=Resources.Load<Material>("WorldMaterials/gullet-membrane");
            var tints=new[]{("feeding",new Color(1.08f,.92f,1.04f),new Color(.6f,1.3f,2.4f)),("throat",new Color(.8f,.84f,.95f),new Color(.03f,1.5f,1.9f)),
                ("nursery",new Color(.95f,1.06f,1.1f),new Color(.1f,3.8f,4.2f)),("coil",new Color(.84f,.8f,1f),new Color(.45f,1.1f,3.2f)),("exit",new Color(1,1,1),new Color(.05f,3.2f,3.8f))};
            var result=new Material[tints.Length];
            for(int i=0;i<tints.Length;i++)
            {
                string path="Assets/_Project/Resources/WorldMaterials/gullet-membrane-"+tints[i].Item1+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
                mat.CopyPropertiesFromMaterial(source);mat.SetColor("_BaseColor",tints[i].Item2);mat.SetColor("_EmissionColor",tints[i].Item3);
                if(tints[i].Item1=="coil")GulletCoilArt.ConfigureMembrane(mat);
                EditorUtility.SetDirty(mat);result[i]=mat;
            }
            return result;
        }
    }
}
