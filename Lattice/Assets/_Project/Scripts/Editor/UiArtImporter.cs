using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class UiArtImporter
    {
        public static void Import()=>BatchTools.Run(()=>
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/_Project/Resources/UI/Generated"}))
            {
                var importer=AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                importer.textureType=TextureImporterType.Default;importer.maxTextureSize=4096;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            Debug.Log("UI_ART_IMPORT_OK");
        });
    }
}
