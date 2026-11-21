using System;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Lattice.EditorTools
{
    public static class BatchTools
    {
        const string Scenes="Assets/_Project/Scenes/";
        public static void Verify()=>Run(()=>
        {
            if(EditorUtility.scriptCompilationFailed)throw new Exception("Compilation failed");
            if(Shader.Find("Lattice/Toon")==null)throw new Exception("Lattice toon shader missing");
            Debug.Log("VERIFY_OK");
        });
        public static void CreateBootScenes()=>Run(()=>
        {
            Directory.CreateDirectory(Scenes);
            PipelineConverter.ConvertTo3DInternal();
            PlayerSettings.companyName="Nathan";PlayerSettings.productName="Coronach";
            PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("activeInputHandler").intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();
            var tmp=Resources.Load<TMP_Settings>("TMP Settings");
            var font=AssetDatabase.FindAssets("t:TMP_FontAsset").Select(AssetDatabase.GUIDToAssetPath)
                .Where(p=>p.Contains("LiberationSans SDF.asset")).Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>).FirstOrDefault();
            if(tmp==null||font==null)throw new Exception("TMP resources missing");
            var ts=new SerializedObject(tmp);ts.FindProperty("m_defaultFontAsset").objectReferenceValue=font;ts.ApplyModifiedPropertiesWithoutUndo();
            var titleTexture=AssetImporter.GetAtPath("Assets/_Project/Resources/UI/Title/key-art.png") as TextureImporter;
            titleTexture.maxTextureSize=2048;titleTexture.textureCompression=TextureImporterCompression.Compressed;titleTexture.SaveAndReimport();
            var boot=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("[Services]",typeof(GameServices),typeof(SceneFlow),typeof(BootLoader));
            new GameObject("[UI]",typeof(PadFocus),typeof(ScreenFade));
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var cam=new GameObject("[Camera]",typeof(Camera),typeof(AudioListener));
            cam.tag="MainCamera";cam.GetComponent<Camera>().backgroundColor=new Color(.02f,.03f,.065f);
            cam.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
            cam.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=true;
            var matPath="Assets/_Project/Settings/LatticeToon.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null){material=new Material(Shader.Find("Lattice/Toon"));AssetDatabase.CreateAsset(material,matPath);}
            var keep=GameObject.CreatePrimitive(PrimitiveType.Cube);keep.name="ShaderReference";
            keep.GetComponent<Renderer>().sharedMaterial=material;keep.transform.position=new Vector3(0,-1000,0);
            EditorSceneManager.SaveScene(boot,Scenes+"_Boot.unity");
            var title=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Title",typeof(TitleScreen));EditorSceneManager.SaveScene(title,Scenes+"Title.unity");
            foreach(var name in new[]{"Hub_CinderHalo","Hub_Decks","Arena_Ground","Arena_Flight","Sorrel_Ridges","Gullet_Tunnel","TallowDrift"})
            {
                if(File.Exists(Scenes+name+".unity"))continue;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("ZoneRoot");EditorSceneManager.SaveScene(scene,Scenes+name+".unity");
            }
            RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("BOOT_SCENES_OK");
        });
        /// <summary>The first fifteen scenes, at the build indices they have always had: the two that aren't zones, then chapters 1 and 2. Nothing is added here.</summary>
        static readonly string[] FirstScenes={"_Boot","Title","Hub_CinderHalo","Hub_Decks","Sorrel_Ridges","Hushwell","Gullet_Tunnel","TallowApproach","TallowDrift","Arena_Ground","Arena_Flight",
            "WickReach","Lanternwick","LanternGorge","SunkenRelay"};
        /// <summary>Every scene of the build, in build order: the first fifteen, then each other zone that has a ZoneDef asset and a built scene, by its chapter and then its id.
        /// A new zone joins by being defined and built: a zone left off the old hand-kept list simply couldn't be loaded in the player.</summary>
        internal static string[] SceneNames()
        {
            var names=FirstScenes.ToList();var later=new System.Collections.Generic.List<Lattice.Data.ZoneDef>();
            const string folder="Assets/_Project/Resources/Definitions/ZoneDef";
            foreach(string path in Directory.Exists(folder)?Directory.GetFiles(folder,"*.asset"):Array.Empty<string>())
            {
                var zone=AssetDatabase.LoadAssetAtPath<Lattice.Data.ZoneDef>(path.Replace('\\','/'));if(zone==null)continue;
                string file=Path.GetFileNameWithoutExtension(path);
                if(zone.id!=file||zone.scene!=file)throw new Exception($"ZoneDef {file}: its id ('{zone.id}'), its scene ('{zone.scene}') and its file name must be the same string");
                if(!names.Contains(file))later.Add(zone);
            }
            foreach(var zone in later.OrderBy(z=>z.chapter<=0?int.MaxValue:z.chapter).ThenBy(z=>z.id,StringComparer.Ordinal))
            {
                // Defined before it is built is an ordinary day; built and never registered was the silent failure.
                if(File.Exists(Scenes+zone.id+".unity"))names.Add(zone.id);else Debug.LogWarning("SCENE_UNBUILT "+zone.id+": it has a ZoneDef and no scene yet, so it isn't in the build");
            }
            return names.ToArray();
        }
        internal static void RegisterScenes()
        {
            EditorBuildSettings.scenes=SceneNames().Select(n=>new EditorBuildSettingsScene(Scenes+n+".unity",true)).ToArray();
        }
        public static void BuildWindowsPlayer()=>Build(false);
        public static void BuildWindowsDevPlayer()=>Build(true);
        public static void BuildTracePlayer()=>Build(true, "Builds/quality/TracePlayer/Coronach.exe");
        public static void BuildNativeGpuPlayer()=>Build(true, "Builds/quality/NativeGpuPlayer/Coronach.exe");
        public static void RefreshDialogue()=>Run(()=>{ImportDialogue();Debug.Log("DIALOGUE_IMPORT_OK");});
        static void ImportDialogue()
        {
            AssetDatabase.ImportAsset("Assets/_Project/Resources/Dialogue/Lattice.yarnproject",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var project=Resources.Load<Yarn.Unity.YarnProject>("Dialogue/Lattice");
            if(project==null||!project.Program.Nodes.ContainsKey("__RuntimeReady")||!project.Program.Nodes.ContainsKey("OrrinFirst"))
                throw new Exception("Dialogue project is missing required compiled nodes");
        }
        static void Build(bool dev, string isolatedOutput=null)=>Run(()=>
        {
            ImportDialogue();
            RegisterScenes();
            var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            var output=isolatedOutput == null ? Path.Combine(root,"Builds",dev?"WindowsDev":"Windows","Coronach.exe") : Path.Combine(root,isolatedOutput);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName=output,target=BuildTarget.StandaloneWindows64,
                options=dev?BuildOptions.Development:BuildOptions.None,
                extraScriptingDefines=dev?new[]{"LATTICE_DEV"}:Array.Empty<string>()});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+result.summary.result);
            File.Copy(Path.Combine(root,"tools/native_gpu/THIRD-PARTY-NOTICES.txt"),
                Path.Combine(Path.GetDirectoryName(output),"CoronachGpuClock-NOTICES.txt"),true);
            Debug.Log("BUILD_OK "+output);
        });
        /// <summary>Set by a runner that calls other entry points inside its own run (Restage.All): an entry point then runs, and throws as any call does, without
        /// quitting Unity, and the runner decides how its own run ends.</summary>
        internal static bool Nested;
        public static void Run(Action action)
        {
            if(Nested){action();return;}
            try{action();EditorApplication.Exit(0);}catch(Exception e){Debug.LogError("FAILED: "+e);EditorApplication.Exit(1);}
        }
    }
}
