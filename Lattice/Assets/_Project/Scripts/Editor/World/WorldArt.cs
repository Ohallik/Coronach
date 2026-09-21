using System.IO;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class WorldArt
    {
        const string Root="Assets/_Project/Art/Generated/Textures/";
        const string Materials="Assets/_Project/Resources/WorldMaterials/";
        public static void Prepare()
        {
            Directory.CreateDirectory(Materials);
            foreach(string file in Directory.GetFiles(Root,"*.png"))
            {
                string path=file.Replace('\\','/');var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.textureType=TextureImporterType.Default;importer.maxTextureSize=path.Contains("-sky")||path.Contains("-map")?4096:1024;
                importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Bilinear;importer.sRGBTexture=!path.Contains("-emission");importer.SaveAndReimport();
            }
            foreach(string name in new[]{"sorrel-ground","sorrel-rock","deck-panels","gullet-membrane","vorun-map","sorrel-map"})
            {
                var mat=Material(name,"Lattice/Toon");mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+name+".png"));mat.SetColor("_BaseColor",Color.white);
                if(name=="gullet-membrane"){mat.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+name+"-emission.png"));mat.SetColor("_EmissionColor",new Color(.05f,2.5f,3));mat.EnableKeyword("_DISSOLVE_ON");mat.SetVector("_DissolveParams",new Vector4(0,3,0,0));mat.SetColor("_DissolveEdgeColor",new Color(.1f,3,4));}
                EditorUtility.SetDirty(mat);
            }
            foreach(string name in new[]{"halo-sky","gullet-sky","tallow-sky"})
            {
                var mat=Material(name,"Skybox/Panoramic");mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+name+".png"));mat.SetFloat("_Exposure",.8f);mat.SetFloat("_Mapping",1);EditorUtility.SetDirty(mat);
            }
            foreach(var zone in GameCatalog.All<ZoneDef>())
            {
                if(zone.id.StartsWith("Arena"))continue;
                zone.skybox=AssetDatabase.LoadAssetAtPath<Material>(Materials+(zone.id=="Gullet_Tunnel"?"gullet-sky":zone.id.StartsWith("Tallow")?"tallow-sky":"halo-sky")+".mat");EditorUtility.SetDirty(zone);
            }
            AssetDatabase.SaveAssets();
        }
        static Material Material(string name,string shader)
        {
            string path=Materials+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){var found=Shader.Find(shader);if(found==null)throw new System.Exception("Shader missing "+shader);mat=new Material(found);AssetDatabase.CreateAsset(mat,path);}return mat;
        }
        public static void Dress(GameObject go,string key,Vector3 size)
        {
            string material=key.StartsWith("MoonGround")?"sorrel-ground":key.StartsWith("RidgeRock")?"sorrel-rock":key.StartsWith("Gullet")?"gullet-membrane":
                key.StartsWith("Deck")||key.StartsWith("Outpost")||key.StartsWith("DockingPod")||key.StartsWith("RingSegment")||key=="TallowStationHull"?"deck-panels":null;
            if(material==null)return;
            var source=AssetDatabase.LoadAssetAtPath<Material>(Materials+material+".mat");if(source==null)return;
            string variant=material+"-"+Mathf.RoundToInt(size.x)+"-"+Mathf.RoundToInt(size.z);var mat=Material(variant,"Lattice/Toon");mat.CopyPropertiesFromMaterial(source);
            mat.SetTextureScale("_BaseMap",new Vector2(Mathf.Max(1,size.x/8),Mathf.Max(1,size.z/8)));mat.SetTextureScale("_EmissionMap",mat.GetTextureScale("_BaseMap"));EditorUtility.SetDirty(mat);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=mat;
        }
        public static void Planet(string name,Vector3 position,float diameter)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.position=position;go.transform.localScale=Vector3.one*diameter;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Materials+(name=="Vorun"?"vorun-map":"sorrel-map")+".mat");
            go.isStatic=true;
        }
    }
}
