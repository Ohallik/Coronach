using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Lattice.EditorTools
{
    public static class ParticleIntake
    {
        public static void Prepare()
        {
            foreach(string name in new[]{"flare_01","circle_02","slash_01"})
                PackStaging.StageFile("art-src/Imported/KenneyParticles/"+name+".png","Art/Particles/"+name+".png");
            PackStaging.StageFile("art-src/Imported/KenneyParticles/License.txt","Art/Particles/License.txt");
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Assets/_Project/Resources/Effects");
            foreach(string name in new[]{"flare_01","circle_02","slash_01"})
            {
                string image="Assets/_Project/Art/Particles/"+name+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(image);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
                string path="Assets/_Project/Resources/Effects/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(mat,path);}
                mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(image));mat.SetColor("_BaseColor",Color.white);
                mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_ZWrite",0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=(int)RenderQueue.Transparent;EditorUtility.SetDirty(mat);
            }
        }
    }
}
