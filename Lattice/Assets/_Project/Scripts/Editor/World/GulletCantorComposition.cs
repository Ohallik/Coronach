using System;
using System.Linq;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    // Isolated actual-animal placement. Does not regenerate the map.
    public static class GulletCantorComposition
    {
        public static void Refresh()=>BatchTools.Run(()=>
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gullet_Tunnel.unity");
            var encounter=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Gullet_Cantor");
            Configure(encounter);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("GULLET_CANTOR_COMPOSITION_OK");
        });
        // Shared with the existing preview builder for reproducible rebuilds.
        public static void Configure(EncounterVolume encounter)
        {
            var spawner=encounter.spawners.Single(s=>s.definition!=null&&s.definition.id=="Cantor");
            if(spawner.count!=1||spawner.radius!=0)throw new InvalidOperationException("Cantor composition requires the same single actual animal.");
            // Keep the full trailing body inside the original activation volume
            // and clear of the central approach. No activation relocation.
            spawner.transform.position=new Vector3(12,1,794);
        }
    }
}

