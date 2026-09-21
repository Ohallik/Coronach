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
    public static class GulletBuilder
    {
        public static float CenterX(float z)=>Mathf.Sin(z/900*Mathf.PI*4)*12;
        public static float Radius(float z)=>15-3*Mathf.Pow(Mathf.Cos(z/225*Mathf.PI),8);
        public static void Build()=>BatchTools.Run(()=>{WorldBuilder.Prepare();BuildZone();Debug.Log("GULLET_OK");});
        public static void BuildZone()
        {
            var scene=WorldBuilder.Begin("Gullet_Tunnel");WorldBuilder.Spawn("Arrival",new(0,1,8));
            WorldBuilder.Spawn("Performance",new(CenterX(250),1,250));
            var path=new GameObject("900m tunnel spline",typeof(SplineContainer)).GetComponent<SplineContainer>();var spline=new Spline();
            for(int i=0;i<=18;i++){float z=i*50;spline.Add(new BezierKnot(new float3(CenterX(z),1,z)));}spline.SetTangentMode(TangentMode.AutoSmooth);path.Spline=spline;
            Tube();
            float[] centers={110,330,550};float[] doors={215,440,675};
            for(int chamber=0;chamber<3;chamber++)
            {
                float z=centers[chamber];var membrane=WorldBuilder.Membrane("Choir membrane "+chamber,new(CenterX(doors[chamber]),2,doors[chamber]),31,false);
                var encounter=WorldBuilder.Encounter("Gullet_Chamber_"+chamber,new(CenterX(z),1,z-10),new(28,6,90),"ChoristerDart",6,new[]{membrane});
                var spawners=new List<Spawner>(encounter.spawners);
                foreach(var extra in new[]{("ChoristerDrifter",2,15f),("Shellmine",4,40f)})
                {
                    var go=new GameObject("Spawn_"+extra.Item1,typeof(Spawner));go.transform.position=new Vector3(CenterX(z+extra.Item3),1,z+extra.Item3);var spawn=go.GetComponent<Spawner>();spawn.definition=GameCatalog.Find<EnemyDef>(extra.Item1);spawn.count=extra.Item2;spawn.radius=extra.Item1=="Shellmine"?8:10;spawners.Add(spawn);
                }
                encounter.spawners=spawners.ToArray();
                WorldBuilder.Piece("ChoirPod",new(CenterX(z)-12,2,z),new(3,4,4),"Emission");WorldBuilder.Piece("ChoirPod",new(CenterX(z+20)+12,2,z+20),new(3,4,4),"Emission");
                WorldBuilder.Label("CHAMBER "+(chamber+1),new(CenterX(z),-1,z-45));
            }
            var cache=WorldBuilder.Piece("NeveCache",new(CenterX(400)-9,1,400),new(2,2,2),"Taren").AddComponent<SalvageField>();cache.prompt="Neve's salvage cache";
            var cacheTrigger=cache.gameObject.GetComponent<BoxCollider>();cacheTrigger.isTrigger=true;cacheTrigger.size=Vector3.one*2;
            WorldBuilder.Encounter("Gullet_Cantor",new(CenterX(805),1,805),new(44,6,36),"Cantor",1);
            WorldBuilder.Warp("Exit warp — Tallow Drift",new(CenterX(890),1,890),"TallowApproach","bossdown.Cantor");
            WorldBuilder.Save(scene,"Gullet_Tunnel");
        }
        static void Tube()
        {
            // Open upper shell makes the spline dungeon a readable diorama cutaway.
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int sides=16,steps=180;
            for(int step=0;step<=steps;step++)
            {
                float z=step*5f;
                for(int side=0;side<=sides;side++)
                {
                    float angle=side/(float)sides*Mathf.PI;
                    vertices.Add(new Vector3(CenterX(z)+Mathf.Cos(angle)*Radius(z),4-Mathf.Sin(angle)*8,z));uv.Add(new Vector2(side/4f,z/20));
                    if(step==steps||side==sides)continue;
                    int a=step*(sides+1)+side,b=a+sides+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});
                }
            }
            var mesh=new Mesh{name="Gullet cutaway shell"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            Directory.CreateDirectory("Assets/_Project/Art/WorldMeshes");const string path="Assets/_Project/Art/WorldMeshes/GulletShell.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
            var go=new GameObject("Spline membrane shell",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.isStatic=true;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("WorldMaterials/gullet-membrane");
            // Side rails prevent leaving the cutaway at flight height and apply motor wall damage.
            for(int i=0;i<90;i++){float z=i*10+5;for(int sign=-1;sign<=1;sign+=2)WorldBuilder.Piece(i%2==0?"GulletWallA":"GulletWallB",new(CenterX(z)+sign*Radius(z),2,z),new(1,6,11),"Rock");}
        }
    }
}
