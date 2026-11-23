using System.Collections.Generic;
using System.Linq;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>The fade's variant given to the materials already painted (D359), without rebuilding a scene: every material asset on a piece a named scene fades
    /// (<see cref="SceneryOcclusion"/>) that can't draw the fade gets `_OCCFADE_ON` and its toggle, exactly as its builder's paint now gives it. Scenes are opened to find them
    /// and never saved: a material is an asset the scene points at. Run with <c>-fade-scenes A,B</c>; marker <c>FADE_KEYWORDS_OK</c>.</summary>
    public static class FadeKeywordRepair
    {
        public static void Apply()=>BatchTools.Run(()=>
        {
            string arg=Lattice.Core.DevArgs.Value("-fade-scenes");if(string.IsNullOrEmpty(arg))throw new System.ArgumentException("-fade-scenes A,B is required");
            var given=new SortedSet<string>();
            foreach(var zone in arg.Split(',').Select(z=>z.Trim()).Where(z=>z.Length>0))
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity",OpenSceneMode.Single);
                foreach(var fade in Object.FindObjectsByType<SceneryOcclusion>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    foreach(var drawn in fade.GetComponentsInChildren<Renderer>(true))
                        foreach(var material in drawn.sharedMaterials)
                        {
                            if(material==null||!material.HasProperty("_OccFade")||material.IsKeywordEnabled("_OCCFADE_ON"))continue;
                            if(!AssetDatabase.Contains(material))throw new System.InvalidOperationException($"FADE_KEYWORDS {zone}: {fade.name} fades in {material.name}, which is in the scene and not an asset: rebuild the scene");
                            ZoneKit.With(material,"_OCCFADE_ON","_OccFadeToggle");material.SetFloat("_OccFade",0);EditorUtility.SetDirty(material);given.Add(zone+": "+material.name);
                        }
            }
            AssetDatabase.SaveAssets();
            foreach(var g in given)Debug.Log("FADE_KEYWORD_GIVEN "+g);
            Debug.Log("FADE_KEYWORDS_OK materials="+given.Select(g=>g.Substring(g.IndexOf(':')+2)).Distinct().Count());
        });
    }
}
