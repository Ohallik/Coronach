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
            PlayerSettings.companyName="Nathan";PlayerSettings.productName="Lattice";
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
        internal static void RegisterScenes()
        {
            var names=new[]{"_Boot","Title","Hub_CinderHalo","Hub_Decks","Sorrel_Ridges","Gullet_Tunnel","TallowDrift","Arena_Ground","Arena_Flight"};
            EditorBuildSettings.scenes=names.Select(n=>new EditorBuildSettingsScene(Scenes+n+".unity",true)).ToArray();
        }
        public static void BuildWindowsPlayer()=>Build(false);
        public static void BuildWindowsDevPlayer()=>Build(true);
        static void Build(bool dev)=>Run(()=>
        {
            RegisterScenes();
            var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            var output=Path.Combine(root,"Builds",dev?"WindowsDev":"Windows","Lattice.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName=output,target=BuildTarget.StandaloneWindows64,
                options=dev?BuildOptions.Development:BuildOptions.None,
                extraScriptingDefines=dev?new[]{"LATTICE_DEV"}:Array.Empty<string>()});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+result.summary.result);
            Debug.Log("BUILD_OK "+output);
        });
        public static void Run(Action action)
        {
            try{action();EditorApplication.Exit(0);}catch(Exception e){Debug.LogError("FAILED: "+e);EditorApplication.Exit(1);}
        }
    }
}
