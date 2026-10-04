using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>Actual raster coverage of a hero hidden by generated boss geometry.</summary>
    public static class BossVisibilityAudit
    {
        const int Width = 1280, Height = 720;
        public static void Capture() => BatchTools.Run(() =>
        {
            string run = DevArgs.Value("-boss-visibility-run") ?? "boss-visibility-01";
            if (run.IndexOfAny(new[] { '/', '\\', ':' }) >= 0) throw new InvalidOperationException("Use one evidence folder name");
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/quality/workshop", run));
            if (Directory.Exists(folder)) throw new InvalidOperationException("Preserve prior visibility evidence");
            Directory.CreateDirectory(folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.6f, .65f, .72f);
            var light = new GameObject("Visibility light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(48, -35, 0);
            var camera = new GameObject("Visibility camera", typeof(Camera), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.fieldOfView = 30; camera.aspect = Width / (float)Height; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var rotation = Quaternion.Euler(40, 20, 0);
            camera.transform.SetPositionAndRotation(-(rotation * Vector3.forward) * 19.5f, rotation);
            var marker = new Material(Shader.Find("Universal Render Pipeline/Unlit")); marker.SetColor("_BaseColor", Color.magenta);
            var failures = new List<string>();
            using var report = new StreamWriter(Path.Combine(folder, "coverage.csv"));
            report.WriteLine("hero,boss,baselinePixels,opaqueFraction,revealedFraction,changedOutsideEllipse");
            foreach (string character in new[] { "Taren", "Sela" })
            {
                var root = new GameObject(character, typeof(CombatActor)); var hero = root.GetComponent<CombatActor>();
                var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/" + character + "Shaped.prefab"), root.transform);
                Freeze(model);
                foreach (var body in model.GetComponentsInChildren<Renderer>())
                    if (body is MeshRenderer or SkinnedMeshRenderer) body.sharedMaterials = Enumerable.Repeat(marker, body.sharedMaterials.Length).ToArray();
                var baseline = Render(camera, Path.Combine(folder, character + "-unobscured.png"));
                var mask = baseline.Select(Magenta).ToArray(); int count = mask.Count(x => x);
                if (count < 300) throw new InvalidOperationException("Hero mask did not render: " + character + " pixels=" + count);
                foreach (string id in new[] { "Burrower", "BellowsBelow" })
                {
                    var boss = new GameObject(id);
                    boss.transform.position = new Vector3(-.34202f, 0, -.939693f) * 2.3f;
                    var body = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/" + id + ".prefab"), boss.transform);
                    Freeze(body);
                    var renderers = body.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers) { var p = new MaterialPropertyBlock(); p.SetFloat("_OccFade", 0); renderer.SetPropertyBlock(p); }
                    string name = character + "-" + id;
                    var opaque = Render(camera, Path.Combine(folder, name + "-opaque.png"));
                    // Reflection keeps the probe compilable on the original broken runtime.
                    var type = typeof(CombatActor).Assembly.GetType("Lattice.Combat.BossOcclusion");
                    var component = type != null ? boss.AddComponent(type) : null;
                    if (component != null && DevArgs.Value("-boss-visibility-fault") != "opaque")
                        for (int i = 0; i < 30; i++) type.GetMethod("RefreshView").Invoke(component, new object[] { camera, hero, 1f / 60 });
                    var properties = new MaterialPropertyBlock(); renderers.First().GetPropertyBlock(properties);
                    Vector4 ellipse = properties.GetVector("_OccFadeEllipse");
                    if (DevArgs.Value("-boss-visibility-fault") == "wide")
                        foreach (var renderer in renderers) { renderer.GetPropertyBlock(properties); properties.SetVector("_OccFadeEllipse", new Vector4(.5f, .5f, 1, 1)); renderer.SetPropertyBlock(properties); }
                    var revealed = Render(camera, Path.Combine(folder, name + "-revealed.png"));
                    int before = 0, after = 0, outside = 0;
                    for (int p = 0; p < mask.Length; p++)
                    {
                        if (mask[p]) { if (Magenta(opaque[p])) before++; if (Magenta(revealed[p])) after++; }
                        float x = (p % Width + .5f) / Width, y = (p / Width + .5f) / Height;
                        float dx = (x - ellipse.x) / Mathf.Max(.0001f, ellipse.z), dy = (y - ellipse.y) / Mathf.Max(.0001f, ellipse.w);
                        if (dx * dx + dy * dy > 1.1f && Difference(opaque[p], revealed[p]) > .1f) outside++;
                    }
                    float opaqueFraction = before / (float)count, revealedFraction = after / (float)count;
                    report.WriteLine($"{character},{id},{count},{opaqueFraction:F4},{revealedFraction:F4},{outside}"); report.Flush();
                    if (opaqueFraction > .35f) failures.Add(name + " fixture does not obscure enough of the hero: " + opaqueFraction);
                    if (revealedFraction < .55f) failures.Add(name + " hero remains obscured: " + revealedFraction);
                    if (outside > Width * Height * .001f) failures.Add(name + " changed boss pixels outside the hero ellipse: " + outside);
                    Object.DestroyImmediate(boss);
                }
                Object.DestroyImmediate(root);
            }
            Object.DestroyImmediate(marker);
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
            Debug.Log("BOSS_VISIBILITY_PIXELS_OK " + folder);
        });
        static bool Magenta(Color c) => c.r > .7f && c.b > .7f && c.g < .15f;
        static float Difference(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        static void Freeze(GameObject root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>())
            {
                var idle = animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.Contains("Idle")) : null;
                if (idle != null) idle.SampleAnimation(animator.gameObject, 0);
                animator.enabled = false;
            }
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = Object.Instantiate(skin.sharedMesh); mesh.vertices = GeneratedGeometry.WorldSkinPoints(skin); mesh.RecalculateBounds(); mesh.RecalculateNormals();
                var baked = new GameObject("Frozen visible skin", typeof(MeshFilter), typeof(MeshRenderer));
                baked.GetComponent<MeshFilter>().sharedMesh = mesh; baked.GetComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                baked.transform.SetParent(root.transform, true); skin.enabled = false;
            }
        }
        static Color[] Render(Camera camera, string path)
        {
            var rt = new RenderTexture(Width, Height, 24); camera.targetTexture = rt;
            camera.Render(); camera.Render(); var previous = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG()); var pixels = image.GetPixels();
            RenderTexture.active = previous; camera.targetTexture = null; Object.DestroyImmediate(image); Object.DestroyImmediate(rt); return pixels;
        }
    }
}
