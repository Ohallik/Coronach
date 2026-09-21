using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>
    /// One-shot conversion of the Phase 0 2D-URP template setup to the standard 3D
    /// forward renderer (plan §2 note). Deterministic and re-runnable.
    /// </summary>
    public static class PipelineConverter
    {
        const string RendererPath = "Assets/_Project/Settings/URP-3D-Renderer.asset";
        const string PipelinePath = "Assets/_Project/Settings/URP-3D-Pipeline.asset";

        static readonly string[] ObsoleteAssets =
        {
            "Assets/_Project/Settings/Renderer2D.asset",
            "Assets/_Project/Settings/UniversalRP.asset",
            "Assets/_Project/Settings/Lit2DSceneTemplate.scenetemplate",
            "Assets/_Project/Settings/URP2DSceneTemplate.unity",
        };

        public static void ConvertTo3D()
        {
            try
            {
                ConvertTo3DInternal();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Lattice] ConvertTo3D FAILED: {e}");
                EditorApplication.Exit(1);
            }
        }

        internal static UniversalRenderPipelineAsset ConvertTo3DInternal()
        {
            Directory.CreateDirectory("Assets/_Project/Settings");
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath) is { } existing)
            {BindPostProcessing(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath));return existing;}
            // 1. 3D forward renderer with SSAO + depth/normal textures for the diorama look.
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.name = "URP-3D-Renderer";
            renderer.renderingMode = RenderingMode.Forward;
            renderer.depthPrimingMode = DepthPrimingMode.Disabled;
            AssetDatabase.CreateAsset(renderer, RendererPath);
            BindPostProcessing(renderer);

            var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssao.name = "SSAO";
            renderer.rendererFeatures.Add(ssao);
            AssetDatabase.AddObjectToAsset(ssao, renderer);
            renderer.SetDirty();

            // 2. Pipeline asset wired to it.
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.name = "URP-3D-Pipeline";
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 70f;
            pipeline.shadowCascadeCount = 2;
            pipeline.colorGradingMode = ColorGradingMode.HighDynamicRange;
            pipeline.colorGradingLutSize = 32;
            AssetDatabase.CreateAsset(pipeline, PipelinePath);

            // Several lighting/shadow knobs have no public setters in URP 17 — serialized writes.
            var pso = new SerializedObject(pipeline);
            SetInt(pso, "m_MainLightRenderingMode", 1);          // PerPixel
            SetBool(pso, "m_MainLightShadowsSupported", true);
            SetInt(pso, "m_MainLightShadowmapResolution", 2048);
            SetInt(pso, "m_AdditionalLightsRenderingMode", 1);   // PerPixel
            SetInt(pso, "m_AdditionalLightsPerObjectLimit", 6);
            SetBool(pso, "m_SoftShadowsSupported", true);
            pso.ApplyModifiedPropertiesWithoutUndo();

            // 3. Make it THE pipeline everywhere (graphics settings + every quality level).
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int activeLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(activeLevel, false);

            // 4. Drop the 2D template leftovers.
            foreach (var path in ObsoleteAssets)
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    AssetDatabase.DeleteAsset(path);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Lattice] PIPELINE_3D_OK pipeline={PipelinePath} renderer={RendererPath} " +
                      $"defaultRP={(GraphicsSettings.defaultRenderPipeline != null ? GraphicsSettings.defaultRenderPipeline.name : "null")}");
            return pipeline;
        }

        static void SetInt(SerializedObject so, string prop, int value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.intValue = value;
            else throw new InvalidOperationException($"Missing serialized property {prop}");
        }
        internal static void BindPostProcessing(UniversalRendererData renderer)
        {
            var data=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if(data==null)throw new InvalidOperationException("URP post-process resources missing");
            var so=new SerializedObject(renderer);so.FindProperty("postProcessData").objectReferenceValue=data;so.ApplyModifiedPropertiesWithoutUndo();renderer.SetDirty();EditorUtility.SetDirty(renderer);
        }

        static void SetBool(SerializedObject so, string prop, bool value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.boolValue = value;
            else throw new InvalidOperationException($"Missing serialized property {prop}");
        }
    }
}
