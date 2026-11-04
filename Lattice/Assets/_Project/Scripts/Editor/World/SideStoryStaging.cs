using Lattice.Combat;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Isolated staging for chapter 1's side stories (D199). Only touches its own root in Sorrel_Ridges: the four survey
    /// beacons of "Quiet ridges", placed just off the west seam and east service trails between encounters, each on the
    /// walkable floor (NavMesh), collider-free (the camera sightline rule, D119) and with a reachable prompt. Re-running
    /// replaces the root; nothing else in the scene changes.
    /// </summary>
    public static class SideStoryStaging
    {
        const string RootName="Side stories";
        /// <summary>West seam (two) and east service branch (two), each nudged outward off the trail centre.</summary>
        public static readonly Vector2[] Beacons={new(-39,58),new(-29,98),new(37,55),new(36,113)};
        public static void Refresh()=>BatchTools.Run(()=>
        {
            const string kit="Assets/_Project/Prefabs/Environment/DressSurveyBeacon.prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(kit);
            if(prefab==null)throw new System.InvalidOperationException("Missing generated beacon kit "+kit);
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Sorrel_Ridges.unity");
            var prior=GameObject.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior);
            var root=new GameObject(RootName);
            // The scene's baked navigation is only live in edit mode once added; every beacon must be on it and reachable
            // on foot from the outpost.
            var navigation=Object.FindFirstObjectByType<GroundNavigation>();
            if(navigation==null||navigation.data==null)throw new System.InvalidOperationException("Sorrel has no baked ground navigation");
            var instance=NavMesh.AddNavMeshData(navigation.data);
            try
            {
            if(!NavMesh.SamplePosition(new Vector3(0,0,18),out var outpost,4,NavMesh.AllAreas))throw new System.InvalidOperationException("No walkable floor at the outpost court");
            for(int i=0;i<Beacons.Length;i++)
            {
                var wanted=new Vector3(Beacons[i].x,0,Beacons[i].y);
                if(!NavMesh.SamplePosition(wanted+Vector3.up*2,out var hit,4,NavMesh.AllAreas))throw new System.InvalidOperationException($"Beacon {i} has no walkable floor near {wanted}");
                var at=hit.position;
                var path=new NavMeshPath();
                if(!NavMesh.CalculatePath(outpost.position,at,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                    throw new System.InvalidOperationException($"Beacon {i} at {at} cannot be reached on foot from the outpost");
                var beacon=new GameObject("Evacuation beacon "+(i+1));beacon.transform.SetParent(root.transform,false);beacon.transform.position=at;
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetParent(beacon.transform,false);
                body.transform.localRotation=Quaternion.Euler(0,i*67,0);
                foreach(var c in body.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
                // Low mast under the open-floor sightline rule (D119).
                var bounds=ModelGeometry.BoundsOf(body);float scale=1.35f/Mathf.Max(.01f,bounds.size.y);body.transform.localScale*=scale;
                bounds=ModelGeometry.BoundsOf(body);body.transform.position+=new Vector3(0,at.y-bounds.min.y,0);bounds=ModelGeometry.BoundsOf(body);
                var light=new GameObject("Beacon call").AddComponent<Light>();light.transform.SetParent(beacon.transform,false);
                light.transform.position=new Vector3(at.x,bounds.max.y+.15f,at.z);light.type=LightType.Point;light.color=new Color(.4f,.78f,1f);light.range=7;light.intensity=2.2f;light.shadows=LightShadows.None;
                var signal=beacon.AddComponent<SignalBeacon>();signal.flag="sorrel.beacon."+(i+1);signal.glow=light;
                foreach(var r in body.GetComponentsInChildren<Renderer>())if(r.sharedMaterial!=null&&r.sharedMaterial.HasProperty("_EmissionColor")){signal.lamp=r;break;}
                var point=beacon.AddComponent<DiscoveryPoint>();
                point.flag=signal.flag;point.report="SorrelBeacon";point.situation="beacon";point.dialogueNode="";point.dialogueSpeaker="";
                point.prompt="Silence the evacuation beacon";point.range=2.4f;point.requiredQuest="SorrelBeacons";point.requiredStep=0;
                Debug.Log($"SIDE_STORY_BEACON {i+1} at={at} height={bounds.size.y:F2}");
            }
            }
            finally{instance.Remove();}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("SIDE_STORY_STAGING_OK");
        });
    }
}
