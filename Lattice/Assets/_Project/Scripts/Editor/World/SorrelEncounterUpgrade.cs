using System;
using System.Linq;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    // Isolated scene upgrade: preserve terrain, dressing, actors and progression.
    public static class SorrelEncounterUpgrade
    {
        public static void Apply() => BatchTools.Run(() =>
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Sorrel_Ridges.unity");
            foreach(string name in new[]{"Drill entry seal","Drill cradle seal"})
            {
                var seal=UnityEngine.Object.FindObjectsByType<Membrane>(FindObjectsSortMode.None).Single(m=>m.name==name);
                var renderer=seal.GetComponent<Renderer>();
                if(renderer==null||renderer.bounds.size.y<=0)throw new InvalidOperationException("Missing measured seal geometry: "+name);
                var scale=seal.transform.localScale;scale.y*=.6f/renderer.bounds.size.y;seal.transform.localScale=scale;
                var position=seal.transform.position;position.y+=.3f-renderer.bounds.center.y;seal.transform.position=position;
                EditorUtility.SetDirty(seal.gameObject);
            }
            Physics.SyncTransforms();EditorSceneManager.SaveScene(scene);
            Debug.Log("SORREL_ENCOUNTER_SEALS_OK");
        });
    }
}
