using System.Linq;
using Lattice.Data;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// One person, one face (November 21). Idra Sorn's braced sheet (Idra_Braced, bound as her shaped form's by PortraitIntake) was generated from her own sheet with
    /// her face asked for again, and came back a broader jaw, amber eyes where hers are silver, and a visor pushed up over the crest that reads as a crown: in the
    /// whole-game capture the reviewers took the woman at s48/061 and s46/020 for someone other than the one at s44/064. Her frame is on her body in the world, so the
    /// dialogue keeps her own face in both forms, with all sixteen of its expressions. Taren's and Sela's shaped sheets keep their faces under the visor's glow and stay.
    /// A later <c>PortraitIntake.ImportNamed</c> of Idra_Braced binds it again: run this after one.
    /// <para>Run: <c>exec-when-green.ps1 -Method Lattice.EditorTools.PortraitFaces.Apply -Marker PORTRAIT_FACES_OK</c></para>
    /// </summary>
    public static class PortraitFaces
    {
        /// <summary>Who shows which sheet in their shaped form, where it isn't the sheet intake gave them.</summary>
        static readonly (string speaker,string shaped)[] OneFace={("Idra","Idra_Natural")};
        public static void Apply()=>BatchTools.Run(()=>
        {
            foreach(var (speaker,shaped) in OneFace)
            {
                var character=AssetDatabase.LoadAssetAtPath<CharacterDef>($"Assets/_Project/Resources/Definitions/CharacterDef/{speaker}.asset");
                var set=AssetDatabase.LoadAssetAtPath<PortraitSet>($"Assets/_Project/Resources/Portraits/{shaped}.asset");
                if(character==null||set==null)throw new System.InvalidOperationException($"PortraitFaces: no {speaker} or no {shaped}");
                character.portraitShaped=set;EditorUtility.SetDirty(character);
                Debug.Log($"PORTRAIT_FACES {speaker} shaped={set.name} natural={(character.portraitNatural!=null?character.portraitNatural.name:"none")}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("PORTRAIT_FACES_OK");
        });
    }
}
