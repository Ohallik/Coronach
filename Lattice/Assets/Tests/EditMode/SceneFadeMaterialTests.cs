using System.Collections.Generic;
using System.Linq;
using Lattice.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.Tests.EditMode
{
    /// <summary>Every piece a scene fades as scenery (<see cref="SceneryOcclusion"/>) is made of materials that can draw the fade, in every scene in the build as it is saved (D359).
    /// The fade is a shader variant, drawn only where the material carries _OCCFADE_ON. `ZoneKit.Fadeable` sets it on the materials of the piece that asks, and `ZoneKit.Toon`
    /// clears every keyword whenever any builder paints the material again: when the Neighbourhood Afloat's builder painted Many Hands' materials for its own district after the
    /// city was built, the city's panel, wood, cloth and hull plate lost it, and the player drew the gantry's beam solid across the spine (s28/043). The fade's amount was still set on
    /// every renderer, so every check that read the amount passed.</summary>
    public sealed class SceneFadeMaterialTests
    {
        static string Path(Transform t){var names=new List<string>();for(;t!=null;t=t.parent)names.Insert(0,t.name);return string.Join("/",names);}
        static List<string> Faults(string zone)
        {
            var faults=new List<string>();
            foreach(var fade in Object.FindObjectsByType<SceneryOcclusion>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                foreach(var drawn in fade.GetComponentsInChildren<Renderer>(true))
                {
                    if(drawn is not MeshRenderer&&drawn is not SkinnedMeshRenderer)continue;
                    foreach(var material in drawn.sharedMaterials)
                        if(material!=null&&material.HasProperty("_OccFade")&&!material.IsKeywordEnabled("_OCCFADE_ON"))
                            faults.Add($"{zone}: {Path(fade.transform)}{(drawn.transform==fade.transform?"":" ("+drawn.name+")")} fades in {material.name}, which can't draw the fade");
                }
            return faults.Distinct().ToList();
        }

        [Test] public void EveryPieceAnySceneFadesIsMadeOfMaterialsThatCanDrawTheFade()
        {
            // The broken state, built here: Many Hands' gantry in the panel as the Neighbourhood's repaint left it, a copy with the fade's variant off, in memory and never saved.
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/ManyHands.unity",OpenSceneMode.Single);
            var gantry=Object.FindObjectsByType<SceneryOcclusion>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(f=>f.name=="The gantry crane");
            Assert.IsNotNull(gantry,"fixture: Many Hands has no fading gantry crane");
            var drawn=gantry.GetComponentInChildren<MeshRenderer>(true);var kept=drawn.sharedMaterials;
            var repainted=kept.Select(m=>{if(m==null||!m.HasProperty("_OccFade"))return m;var copy=new Material(m){name=m.name};copy.DisableKeyword("_OCCFADE_ON");return copy;}).ToArray();
            drawn.sharedMaterials=repainted;
            var broken=Faults("ManyHands");
            drawn.sharedMaterials=kept;foreach(var copy in repainted)if(copy!=null&&!kept.Contains(copy))Object.DestroyImmediate(copy);
            Assert.IsTrue(broken.Any(f=>f.Contains("The gantry crane")&&f.Contains("hands-panel")),"the check doesn't see the gantry in a panel that can't draw its fade:\n"+string.Join("\n",broken));

            // Every scene in the build, as saved.
            var faults=new List<string>();int scenes=0,pieces=0;
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
            {
                var scene=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Single);scenes++;
                pieces+=Object.FindObjectsByType<SceneryOcclusion>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
                faults.AddRange(Faults(scene.name));
            }
            TestContext.WriteLine($"SCENE_FADES scenes={scenes} fading={pieces} wrong={faults.Count}");
            foreach(var f in faults)TestContext.WriteLine("SCENE_FADES_ITEM "+f);
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
    }
}
