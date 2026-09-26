using System.IO;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>Read-only scene review renders. Never saves or regenerates maps.</summary>
    public static class MapEvidence
    {
        public static void Baseline() => BatchTools.Run(() => Capture("C0/maps-before"));
        public static void StationAfter() => BatchTools.Run(() => Capture("station/maps-after"));
        static void Capture(string run)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/quality", run));
            Directory.CreateDirectory(folder);
            foreach (string zone in new[] { "Hub_CinderHalo", "Hub_Decks", "TallowApproach", "TallowDrift" })
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + zone + ".unity");
                foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.gameObject.SetActive(false);
                bool exterior = zone == "Hub_CinderHalo" || zone == "TallowApproach";
                var camera = new GameObject("Evidence camera", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                camera.backgroundColor = new Color(.025f, .035f, .06f); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                Vector3 center = zone == "Hub_CinderHalo" ? new Vector3(0,0,15) : Vector3.zero;
                float scale = exterior ? 80 : 27;
                Shot(camera, folder, zone + "-overhead", center, Quaternion.Euler(90,0,0), scale);
                Shot(camera, folder, zone + "-structure", center, Quaternion.Euler(25,15,0), scale);
                Shot(camera, folder, zone + "-arrival", zone == "Hub_Decks" ? new Vector3(-20,0,-3) : zone == "Hub_CinderHalo" ? new Vector3(-20,1,3) : Vector3.zero, Quaternion.Euler(40,20,0), exterior ? 22 : 7);
            }
            Debug.Log("MAP_EVIDENCE_OK " + folder);
        }
        static void Shot(Camera camera, string folder, string name, Vector3 center, Quaternion rotation, float scale)
        {
            camera.orthographicSize = scale;
            camera.transform.SetPositionAndRotation(center - rotation * Vector3.forward * 400, rotation);
            var rt = new RenderTexture(1920,1080,24); camera.targetTexture = rt;
            camera.Render(); camera.Render();
            var prior = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            RenderTexture.active = prior; camera.targetTexture = null;
            Object.DestroyImmediate(image); Object.DestroyImmediate(rt);
        }
    }
}
