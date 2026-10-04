using System;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    public static class BossReadabilityUpgrade
    {
        public static void Apply() => BatchTools.Run(() =>
        {
            foreach (string id in new[] { "Burrower", "BellowsBelow" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Generated/Models/" + id + "/" + id + "_Toon.mat");
                if (material == null || !material.HasProperty("_OccFade")) throw new InvalidOperationException("Missing boss toon material: " + id);
                material.EnableKeyword("_OCCFADE_ON"); material.SetFloat("_OccFadeToggle", 1); material.SetFloat("_OccFade", 0);
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets(); Debug.Log("BOSS_READABILITY_MATERIALS_OK");
        });
    }
}
