using System;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class PortraitIntake
    {
        static readonly string[] Sheets={"Taren_Natural","Taren_Shaped","Sela_Natural","Sela_Shaped","Orrin_Natural","Mira_Natural","Hal_Natural","Neve_Natural"};
        const string Destination="Assets/_Project/Art/Portraits/";
        public static void Import()=>BatchTools.Run(()=>ImportSheets(Sheets,true));
        /// <summary>Isolated intake of the named sheets (-portrait-names A_Natural,B_Natural).
        /// A speaker without a character definition, such as a ship-borne resident,
        /// is found by the dialogue's Resources fallback instead of a binding.</summary>
        public static void ImportNamed()=>BatchTools.Run(()=>
        {
            var names=(DevArgs.Value("-portrait-names")??"").Split(new[]{','},StringSplitOptions.RemoveEmptyEntries);
            if(names.Length==0||names.Any(n=>!n.EndsWith("_Natural")&&!n.EndsWith("_Shaped")))throw new InvalidOperationException("-portrait-names needs Name_Natural/Name_Shaped entries");
            ImportSheets(names,false);
        });
        static void ImportSheets(string[] Sheets,bool requireDefinition)
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            foreach(string name in Sheets)
                if(!File.Exists(Path.Combine(root,"art-src/Generated/P3/portraits",name,name+".png")))throw new FileNotFoundException("Portrait sheet missing: "+name);
            Directory.CreateDirectory(Destination);Directory.CreateDirectory("Assets/_Project/Resources/Portraits");
            // Stage the complete batch first so no serialized set references an earlier low-res import.
            foreach(string name in Sheets)PackStaging.StageFile("art-src/Generated/P3/portraits/"+name+"/"+name+".png","Art/Portraits/"+name+".png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string name in Sheets)
            {
                string path=Destination+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                var cells=new SpriteMetaData[16];for(int i=0;i<16;i++)cells[i]=new SpriteMetaData{name=name+"_"+i.ToString("00"),rect=new Rect(i%4*576,(3-i/4)*576,576,576),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
#pragma warning disable CS0618
                importer.spritesheet=cells;
#pragma warning restore CS0618
                importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
                if(texture.width!=2304||texture.height!=2304||sprites.Length!=16||sprites.Any(s=>s.rect.width!=576||s.rect.height!=576))throw new InvalidOperationException("Portrait import resolution/rect gate failed: "+name);
                string setPath="Assets/_Project/Resources/Portraits/"+name+".asset";var set=AssetDatabase.LoadAssetAtPath<PortraitSet>(setPath);
                if(set==null){set=ScriptableObject.CreateInstance<PortraitSet>();AssetDatabase.CreateAsset(set,setPath);}
                string character=name.Split('_')[0];set.characterName=character;set.entries.Clear();for(int i=0;i<16;i++)set.entries.Add(new PortraitSet.Entry{emotion=(PortraitEmotion)i,sprite=sprites[i]});EditorUtility.SetDirty(set);
                var definition=GameCatalog.Find<CharacterDef>(character);if(definition==null&&requireDefinition)throw new InvalidOperationException("No character definition for "+character);
                if(definition!=null){if(name.EndsWith("Natural"))definition.portraitNatural=set;else definition.portraitShaped=set;EditorUtility.SetDirty(definition);}
                Debug.Log("PORTRAIT_RECTS_OK "+name+" cell0=576x576 count=16");
            }
            AssetDatabase.SaveAssets();Debug.Log("PORTRAIT_INTAKE_OK count="+Sheets.Length);
        }
    }
}
