using System.Linq;
using Lattice.Data;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>Folded tissue and embedded Compact moorings below the flight plane.</summary>
    public static class GulletCoilArt
    {
        public static float Blend(float z) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(705, 748, z)) *
            (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(850, 874, z)));

        public static float Height(float x, float z)
        {
            float center = GulletProfile.Center(z), half = x < center ? GulletProfile.Left(z) : GulletProfile.Right(z);
            float across = Mathf.Clamp((x - center) / half, -1, 1);
            float sine = Mathf.Sqrt(Mathf.Max(0, 1 - across * across));
            float radius = Mathf.Sqrt(x * x / (26 * 26) + (z - 805) * (z - 805) / (45 * 45));
            float fold = 4.3f * Gaussian((radius - .70f) / .16f) + 1.8f * Gaussian((radius - 1.04f) / .11f);
            // A recessed bed, with broad muscular folds bearing the moorings.
            // The upper rim and all existing flight-height wall contacts stay fixed.
            float height = 4 - sine * 8 + Blend(z) * sine * sine * (-5 + fold);
            foreach (int side in new[] { -1, 1 }) foreach (int end in new[] { -1, 1 })
            {
                float distance = Vector2.Distance(new Vector2(x, z), new Vector2(side * 18, 805 + end * 20));
                // The complete bolted shoe sits in a flat pressed socket; uneven
                // tissue must not cut through its drum or leave isolated bolts.
                float socket = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(7, 10, distance));
                height = Mathf.Lerp(height, -6.4f, socket);
            }
            return height;
        }

        static float Gaussian(float value) => Mathf.Exp(-value * value);
        public static Color Tissue(float x, float z)
        {
            float depth = Mathf.InverseLerp(-9, -2, Height(x, z));
            var tissue = Color.Lerp(new Color(.32f, .38f, .62f), new Color(4, 2.3f, 3.8f), depth);
            return Color.Lerp(Color.white, tissue, Blend(z));
        }

        // Material-only refresh: retain every socket, mesh, collider and placement.
        public static void RefreshSurfaces()=>BatchTools.Run(()=>
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gullet_Tunnel.unity");
            ConfigureMembrane(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/gullet-membrane-coil.mat"));
            var anchors=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Collar anchor clamp").ToArray();
            if(anchors.Length!=4)throw new System.InvalidOperationException("Expected four existing moorings");
            foreach(var anchor in anchors)ConfigureMooring(anchor.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("GULLET_COIL_SURFACES_OK");
        });

        public static void ConfigureMembrane(Material material)
        {
            material.EnableKeyword("_VCOLOR_ON");material.SetFloat("_VColor",1);
            material.SetFloat("_BaseMapStrength",.98f);
            material.SetColor("_BaseColor",new Color(.65f,.60f,.84f));
            material.SetColor("_EmissionColor",new Color(.12f,.28f,.52f));
            EditorUtility.SetDirty(material);
        }

        static void ConfigureMooring(GameObject mooring)
        {
            const string path="Assets/_Project/Resources/WorldMaterials/gullet-coil-mooring.mat";
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Generated/Models/CoilMooring/CoilMooring_Toon.mat");
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,path);}
            material.CopyPropertiesFromMaterial(source);
            material.SetColor("_BaseColor",new Color(.48f,.54f,.65f));
            material.SetColor("_EmissionColor",new Color(.06f,.25f,.3f));
            material.SetColor("_RimColor",new Color(.08f,.08f,.1f));
            EditorUtility.SetDirty(material);
            foreach(var renderer in mooring.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial=material;
                if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        public static void Moorings()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/CoilMooring.prefab") == null)
                throw new System.InvalidOperationException("The reviewed generated CoilMooring must pass isolated intake first");
            foreach (int side in new[] { -1, 1 }) foreach (int end in new[] { -1, 1 })
            {
                float x = side * 18, z = 805 + end * 20, bed = Height(x, z);
                var mooring = WorldBuilder.Piece("CoilMooring", new Vector3(x, bed + 1.9f - .06f, z), new Vector3(9, 3.8f, 10));
                mooring.name = "Collar anchor clamp";
                ConfigureMooring(mooring);
                mooring.transform.rotation = Quaternion.LookRotation(new Vector3(-x, 0, 805 - z));
            }
        }
    }
}
