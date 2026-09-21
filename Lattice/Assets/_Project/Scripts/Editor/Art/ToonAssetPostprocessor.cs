using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    /// <summary>Single material authority for generated model intake.</summary>
    public sealed class ToonAssetPostprocessor:AssetPostprocessor
    {
        bool Generated=>assetPath.StartsWith("Assets/_Project/Art/Generated/",StringComparison.Ordinal);
        void OnPreprocessTexture()
        {
            if(!Generated)return;
            var importer=(TextureImporter)assetImporter;
            importer.maxTextureSize=assetPath.Contains("-sky")||assetPath.Contains("-map")?4096:1024;importer.mipmapEnabled=true;
        }
        Material OnAssignMaterialModel(Material original,Renderer renderer)
        {
            if(!Generated)return null;
            string dir=Path.GetDirectoryName(assetPath).Replace('\\','/');
            string token=Path.GetFileNameWithoutExtension(assetPath).Split('_')[0];
            var textures=Directory.GetFiles(dir,token+"*base_color.png",SearchOption.AllDirectories);
            if(textures.Length>1)throw new InvalidOperationException("Ambiguous albedo for "+assetPath);
            var shader=Shader.Find("Lattice/Toon");if(shader==null)throw new InvalidOperationException("Lattice shader absent");
            string path=dir+"/"+token+"_Toon.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)throw new InvalidOperationException("Pre-create generated material before model import: "+path);
            if(textures.Length==1)material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures[0].Replace('\\','/')));
            EditorUtility.SetDirty(material);return material;
        }
    }
}
