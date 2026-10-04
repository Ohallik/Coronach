using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>Integration recipe only. Every licensed derivative stays in the ignored root.</summary>
    public static class LocalBiomeBake
    {
        public const string Root="Assets/_Project/LocalStaggart/";
        const string ResourcesRoot=Root+"Resources/LocalBiomes/";
        const string GrassRoot="Packages/xyz.staggart-creations.stylized-grass/";
        const string WaterRoot="Assets/Stylized Water 3/";

        public static void Inspect()=>BatchTools.Run(()=>
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Nursery-water.mat");var shader=material.shader;
            Debug.Log("WATER_INTERFACE shader="+shader.name+" keywords="+string.Join(",",material.shaderKeywords));
            for(int i=0;i<shader.GetPropertyCount();i++)
            {
                var name=shader.GetPropertyName(i);
                if(!(name.Contains("Foam")||name.Contains("Normal")||name.Contains("Bump")||name.Contains("Refraction")||name.Contains("Lighting")||name.Contains("Depth")))continue;
                string value=shader.GetPropertyType(i) switch
                {
                    ShaderPropertyType.Texture=>material.GetTexture(name)?.name??"NONE",
                    ShaderPropertyType.Color=>material.GetColor(name).ToString(),
                    ShaderPropertyType.Vector=>material.GetVector(name).ToString(),
                    _=>material.GetFloat(name).ToString(System.Globalization.CultureInfo.InvariantCulture)
                };
                Debug.Log("WATER_INTERFACE "+name+"="+value+" attributes="+string.Join(",",shader.GetPropertyAttributes(i)));
            }
            Debug.Log("BIOME_INSPECT_OK");
        });

        public static void Build()=>BatchTools.Run(()=>
        {
            var grass=AssetDatabase.LoadAssetAtPath<GameObject>(GrassRoot+"Prefabs/GrassThin.prefab");
            var grassBase=AssetDatabase.LoadAssetAtPath<Material>(GrassRoot+"Materials/StylizedGrass.mat");
            var waterBase=AssetDatabase.LoadAssetAtPath<Material>(WaterRoot+"Materials/StylizedWater3_Toon.mat");
            if(grass==null||grassBase==null||waterBase==null)throw new InvalidOperationException("Install your licensed Staggart packages with tools/install_staggart.py first. Public fallback builds do not require them.");
            Directory.CreateDirectory(ResourcesRoot);AssetDatabase.Refresh();
            Pipeline();
            // At the fixed gameplay camera, the smaller supplied LOD retains the blade silhouette.
            var source=grass.GetComponentsInChildren<MeshFilter>().OrderBy(f=>f.sharedMesh.vertexCount).First();
            var bounds=source.sharedMesh.bounds;
            Debug.Log("BIOME_GRASS_SOURCE vertices="+source.sharedMesh.vertexCount+" bounds="+bounds);
            int patches=0,sprouts=0;
            foreach(var zone in new[]{"Sorrel_Ridges","Arena_Ground","Hushwell","TallowDrift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                foreach(var patch in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BiomePatch>(true)))
                {
                    Mesh mesh;
                    if(patch.water)mesh=patch.GetComponent<MeshFilter>().sharedMesh;
                    else
                    {
                        var combines=new List<CombineInstance>();
                        foreach(var p in patch.sprouts)
                        {
                            float scale=p.w/bounds.size.y;
                            var normalize=Matrix4x4.Translate(new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z));
                            combines.Add(new CombineInstance{mesh=source.sharedMesh,transform=Matrix4x4.TRS(new Vector3(p.x,p.y,p.z),Quaternion.Euler(0,BiomeLandscapeUpgrade.Yaw(p),0),new Vector3(scale*.72f,scale,scale*.72f))*normalize});
                        }
                        mesh=new Mesh{name=patch.resourceKey,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines.ToArray());mesh.RecalculateBounds();
                        mesh.bounds=new Bounds(mesh.bounds.center,mesh.bounds.size+new Vector3(.2f,.05f,.2f));
                        mesh=Save(mesh,Root+patch.resourceKey+".asset");sprouts+=patch.sprouts.Length;
                    }
                    var art=ScriptableObject.CreateInstance<BiomeVisualAsset>();art.mesh=mesh;
                    art.material=Material(patch.palette,patch.water,patch.water?waterBase:grassBase);
                    Save(art,ResourcesRoot+patch.resourceKey+".asset");patches++;
                }
            }
            AssetDatabase.SaveAssets();Debug.Log("LOCAL_BIOMES_OK patches="+patches+" sprouts="+sprouts);
        });

        static T Save<T>(T incoming,string path) where T:Object
        {
            var prior=AssetDatabase.LoadAssetAtPath<T>(path);
            if(prior==null){AssetDatabase.CreateAsset(incoming,path);return incoming;}
            EditorUtility.CopySerialized(incoming,prior);Object.DestroyImmediate(incoming);EditorUtility.SetDirty(prior);return prior;
        }

        static Material Material(string palette,bool water,Material source)
        {
            string path=Root+palette+(water?"-water":"-grass")+".mat";
            var material=new Material(source){name=palette+(water?" water":" grass")};
            if(water)
            {
                Float(material,"_LightingOn",1);material.DisableKeyword("_UNLIT");
                Float(material,"_ReceiveShadows",1);material.DisableKeyword("_RECEIVE_SHADOWS_OFF");
                Float(material,"_AnimationSpeed",.16f);Float(material,"_WaveHeight",.035f);Float(material,"_WavesOn",0);
                Float(material,"_FoamOpacity",.07f);Float(material,"_FoamSpeed",.12f);Float(material,"_NormalStrength",.65f);
                // These are absorption strengths, not metres of depth. The pools are shallow.
                Float(material,"_DepthVertical",8);Float(material,"_DepthHorizontal",12);Float(material,"_EdgeFade",.18f);
                Float(material,"_FoamBaseAmount",.68f);Float(material,"_FoamStrength",.6f);Float(material,"_FoamClipping",.3f);
                Float(material,"_FoamDistanceOn",0);material.DisableKeyword("_SURFACE_FOAM_DUAL");
                Float(material,"_RefractionOn",1);material.EnableKeyword("_REFRACTION");Float(material,"_RefractionStrength",.025f);
                Float(material,"_FoamBubblesStrength",.08f);
                material.SetVector("_FoamTiling",new Vector4(.35f,.35f,0,0));
                material.SetVector("_NormalTiling",new Vector4(.6f,.6f,0,0));
                Float(material,"_IntersectionLength",.25f);Float(material,"_IntersectionFalloff",.2f);
                Float(material,"_IntersectionRippleStrength",.05f);
                material.SetColor("_BaseColor",new Color(.13f,.30f,.33f,.94f).linear);
                material.SetColor("_ShallowColor",new Color(.32f,.48f,.46f,1).linear);
                material.SetColor("_HorizonColor",new Color(.3f,.4f,.4f,0));
                material.SetColor("_FoamColor",new Color(.52f,.69f,.61f,.3f).linear);
                material.SetColor("_IntersectionColor",new Color(.36f,.55f,.46f,.3f).linear);
                Float(material,"_EnvironmentReflectionsOn",1);material.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                Float(material,"_ScreenSpaceReflectionsEnabled",1);Float(material,"_ReflectionStrength",.5f);Float(material,"_ReflectionFresnel",2);
                Float(material,"_ReflectionDistortion",.08f);Float(material,"_ReflectionBlur",.1f);
                Float(material,"_SpecularReflectionsOn",1);material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                Float(material,"_SunReflectionStrength",.12f);Float(material,"_PointSpotLightReflectionStrength",.3f);
                Float(material,"_SparkleIntensity",.015f);Float(material,"_CausticsOn",0);
            }
            else
            {
                material.SetColor("_BaseColor",palette=="Sorrel"?new Color(.68f,.63f,.37f):palette=="Nursery"?new Color(.25f,.52f,.46f):new Color(.49f,.63f,.42f));
                material.SetColor("_HueVariationColor",new Color(.8f,.71f,.48f,.12f));
                Float(material,"_WindAmbientStrength",.08f);Float(material,"_WindGustStrength",.06f);Float(material,"_WindStrength",.08f);
                Float(material,"_WindSpeed",.7f);Float(material,"_WindSwinging",.08f);Float(material,"_WindFlutter",.08f);
                Float(material,"_PerspectiveCorrection",.12f);Float(material,"_BendInfluence",0);
                Float(material,"_FadingOn",0);material.DisableKeyword("_FADING");
                Float(material,"_ColorMapStrength",0);Float(material,"_ScaleMapStrength",0);material.DisableKeyword("_SCALEMAP");
                Float(material,"_NormalFlattening",.7f);Float(material,"_Translucency",.08f);
            }
            return Save(material,path);
        }

        static void Float(Material material,string name,float value)
        {
            if(material.HasProperty(name))material.SetFloat(name,value);
        }

        static void Pipeline()
        {
            const string sourceRenderer="Assets/_Project/Settings/URP-3D-Renderer.asset";
            const string sourcePipeline="Assets/_Project/Settings/URP-3D-Pipeline.asset";
            string rendererPath=Root+"LocalRenderer.asset",pipelinePath=ResourcesRoot+"LocalPipeline.asset";
            // Only these two exact, locally owned files are replaced. Public settings stay untouched.
            if(AssetDatabase.LoadMainAssetAtPath(rendererPath)!=null)AssetDatabase.DeleteAsset(rendererPath);
            if(AssetDatabase.LoadMainAssetAtPath(pipelinePath)!=null)AssetDatabase.DeleteAsset(pipelinePath);
            if(!AssetDatabase.CopyAsset(sourceRenderer,rendererPath)||!AssetDatabase.CopyAsset(sourcePipeline,pipelinePath))throw new InvalidOperationException("Could not clone local render settings");
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            // Water needs the opaque scene depth before its transparent pass.
            // The public renderer copies it after transparents for the HD-2D finish.
            var rendererSettings=new SerializedObject(renderer);rendererSettings.FindProperty("m_CopyDepthMode").intValue=0;
            rendererSettings.ApplyModifiedPropertiesWithoutUndo();
            Feature(renderer,"StylizedWater3.StylizedWaterRenderFeature",new[]{"transparencyRefraction","allowDirectionalCaustics","heightPrePassSettings.enable"});
            Feature(renderer,"sc.stylizedgrass.runtime.GrassRenderFeature",new[]{"settings.enableBending"});
            renderer.SetDirty();EditorUtility.SetDirty(renderer);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            pipeline.supportsCameraOpaqueTexture=true;
            var serialized=new SerializedObject(pipeline);var data=serialized.FindProperty("m_RendererDataList");data.arraySize=1;data.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
        }

        static void Feature(UniversalRendererData renderer,string typeName,string[] disable)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(typeName)).FirstOrDefault(t=>t!=null);
            if(type==null)throw new InvalidOperationException("Local renderer feature missing: "+typeName);
            var feature=(ScriptableRendererFeature)ScriptableObject.CreateInstance(type);feature.name=type.Name;
            var serialized=new SerializedObject(feature);
            foreach(var key in disable){var property=serialized.FindProperty(key);if(property!=null)property.boolValue=false;else Debug.LogWarning("Optional feature setting not present: "+key);}
            serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);feature.Create();EditorUtility.SetDirty(feature);
        }
    }
}
