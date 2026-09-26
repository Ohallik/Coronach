using System.Collections.Generic;
using Lattice.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Lattice.EditorTools
{
    public static class StationNavigationBake
    {
        public static void Audit()=>BatchTools.Run(()=>
        {
            var data=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/_Project/Scenes/Hub_Decks-Navigation.asset");
            var instance=NavMesh.AddNavMeshData(data);
            try
            {
                var tri=NavMesh.CalculateTriangulation();Debug.Log("NAV_AUDIT triangles="+tri.indices.Length/3);
                foreach(var point in new[]{new Vector3(-25.6f,0,21.5f),new Vector3(-28,0,-11),new Vector3(0,0,-3)})
                    Debug.Log("NAV_AUDIT sample="+point+" found="+NavMesh.SamplePosition(point,out var hit,2,NavMesh.AllAreas)+" at="+hit.position);
                var path=new NavMeshPath();Debug.Log("NAV_AUDIT path="+NavMesh.CalculatePath(new Vector3(-25.6f,0,21.5f),new Vector3(-28,0,-11),NavMesh.AllAreas,path)+" status="+path.status+" corners="+string.Join(";",path.corners));
            }
            finally{instance.Remove();}
            Debug.Log("NAV_AUDIT_OK");
        });
        public static void Bake(string zone)
        {
            Physics.SyncTransforms();
            var sources=new List<NavMeshBuildSource>();
            Bounds bounds=new Bounds(Vector3.zero,new Vector3(1,5,1));
            foreach(var box in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            {
                if(!box.enabled||box.isTrigger||!box.gameObject.isStatic||box.bounds.max.y<-.1f||box.bounds.min.y>2.5f)continue;
                sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,
                    transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=0});
                bounds.Encapsulate(box.bounds);
            }
            // Residents stay at their work positions; include their personal
            // space so the companion does not choose a path through Mira/Hal.
            foreach(var npc in Object.FindObjectsByType<Lattice.World.Npc>(FindObjectsSortMode.None))
            {
                var collider=npc.GetComponent<Collider>();if(collider==null||!collider.enabled)continue;
                sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,
                    transform=Matrix4x4.TRS(collider.bounds.center,Quaternion.identity,Vector3.one),size=collider.bounds.size,area=0});
            }
            var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.55f;settings.agentHeight=2;settings.agentClimb=.3f;
            settings.overrideVoxelSize=true;settings.voxelSize=.12f;
            var built=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
            if(built==null)throw new System.InvalidOperationException("Station navigation bake failed: "+zone);
            var check=NavMesh.AddNavMeshData(built);
            try
            {
                var start=zone=="Hub_Decks"?new Vector3(-25.6f,0,21.5f):new Vector3(-3,0,7.4f);
                var finish=zone=="Hub_Decks"?new Vector3(-28,0,-11):new Vector3(0,0,-8);
                var route=new NavMeshPath();
                if(!NavMesh.CalculatePath(start,finish,NavMesh.AllAreas,route)||route.status!=NavMeshPathStatus.PathComplete)
                    throw new System.InvalidOperationException("Station bake does not connect service and arrival: "+zone);
            }
            finally{check.Remove();}
            string path="Assets/_Project/Scenes/"+zone+"-Navigation.asset";
            var prior=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if(prior==null){AssetDatabase.CreateAsset(built,path);prior=built;}
            else {EditorUtility.CopySerialized(built,prior);Object.DestroyImmediate(built);EditorUtility.SetDirty(prior);}
            new GameObject("Baked room navigation",typeof(GroundNavigation)).GetComponent<GroundNavigation>().data=prior;
            Debug.Log("STATION_NAVIGATION_BAKED "+zone+" colliders="+sources.Count);
        }
    }
}
