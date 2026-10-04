using System;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>Replace only the two retired NPC visual children, preserving maps.</summary>
    public static class SpeakerBodyUpgrade
    {
        public static void Apply() => BatchTools.Run(() =>
        {
            foreach (var pair in new[] { ("Sorrel_Ridges", "Survivor"), ("TallowDrift", "Keeper") })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + pair.Item1 + ".unity", OpenSceneMode.Single);
                var npc = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Npc>(true)).Single(n => n.speaker == pair.Item2);
                var old = npc.transform.Find("Generated civilian");
                if (old == null) throw new InvalidOperationException("Expected NPC visual child is missing: " + pair.Item2);
                var definition = GameCatalog.Find<CharacterDef>(pair.Item2);
                if (definition.natural == null) throw new InvalidOperationException("Generated body is missing: " + pair.Item2);
                var body = (GameObject)PrefabUtility.InstantiatePrefab(definition.natural, npc.transform);
                body.name = "Generated civilian";
                body.transform.localPosition = old.localPosition;
                body.transform.localRotation = old.localRotation;
                // Intake owns metre height. Do not inherit the donor model's scale.
                UnityEngine.Object.DestroyImmediate(old.gameObject);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("SPEAKER_BODIES_OK");
        });
    }
}
