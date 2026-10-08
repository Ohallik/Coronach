using System.Linq;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    // Isolated staging: retain the existing profile, shell, triggers, exits,
    // spawn identities and all other maps. No shared-world preparation.
    public static class GulletPreviewStaging
    {
        public static void Refresh()=>BatchTools.Run(()=>
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gullet_Tunnel.unity");
            var encounter=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Gullet_Cantor");
            Configure(encounter);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("GULLET_PREVIEW_STAGED_OK");
        });
        public static void Configure(EncounterVolume encounter)
        {
            var point=GameObject.Find("Cantor preview pocket");
            if(point==null)point=new GameObject("Cantor preview pocket");
            point.transform.position=new Vector3(23,1,768);encounter.previewPoint=point.transform;encounter.previewRadius=3.5f;
            var discovery=point.GetComponent<DiscoveryPoint>()??point.AddComponent<DiscoveryPoint>();
            discovery.prompt="Study the strained collar";discovery.range=3.5f;discovery.flag="gullet.collarObserved";
            discovery.report="";discovery.situation="";discovery.dialogueNode="SelaCantorPreview";discovery.dialogueSpeaker="Sela";
            discovery.previewEncounter=encounter;
            var old=GameObject.Find("Cantor preview folds");if(old!=null)Object.DestroyImmediate(old);
            var folds=new GameObject("Cantor preview folds");
            foreach(float z in new[]{756f,777f})
            {
                // Two rooted lips form an open shoulder, before the unchanged
                // encounter boundary. The centre flight lane stays clear.
                var ridge=WorldBuilder.Piece("GulletWallA",Vector3.zero,new Vector3(1,7.5f,11));
                ridge.name="Preview tissue lip";ridge.transform.rotation=Quaternion.Euler(0,90,0);
                var bounds=ModelGeometry.BoundsOf(ridge);float edge=GulletProfile.RightEdge(z);
                var fit=new GameObject("Rooted preview lip").transform;fit.SetParent(folds.transform,false);ridge.transform.SetParent(fit,false);
                fit.localScale=new Vector3((edge-29)/bounds.size.x,1,4/bounds.size.z);
                fit.position=new Vector3((edge+29)*.5f,.25f,z);
            }
        }
    }
}
