using System.Linq;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using Lattice.World;

namespace Lattice.Tests.EditMode
{
    public sealed class SpeakerBodyTests
    {
        [TestCase("Sorrel_Ridges", "Survivor")]
        [TestCase("TallowDrift", "Keeper")]
        public void ShippedSceneUsesTheSpeakersCurrentBody(string zone, string speaker)
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + zone + ".unity", OpenSceneMode.Additive);
            try
            {
                var npc = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Npc>(true)).Single(n => n.speaker == speaker);
                var expected = GameCatalog.Find<CharacterDef>(speaker).natural.GetComponentsInChildren<SkinnedMeshRenderer>().Select(s => s.sharedMesh).ToArray();
                var actual = npc.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s => s.sharedMesh).ToArray();
                CollectionAssert.AreEquivalent(expected, actual, zone + " still bakes a different speaker's visible body");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [TestCase("Survivor", "Orrin")]
        [TestCase("Keeper", "Hal")]
        public void SpeakerHasAnIndependentGeneratedHumanoidBody(string speaker, string formerDonor)
        {
            var body = GameCatalog.Find<CharacterDef>(speaker).natural;
            var donor = GameCatalog.Find<CharacterDef>(formerDonor).natural;
            Assert.IsNotNull(body, speaker + " has no body");
            Assert.AreNotSame(donor, body, speaker + " still borrows " + formerDonor + "'s body");
            var animator = body.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator); Assert.IsTrue(animator.isHuman);
            Assert.IsNotNull(animator.runtimeAnimatorController);
            var skins = body.GetComponentsInChildren<SkinnedMeshRenderer>();
            Assert.IsNotEmpty(skins);
            var borrowed = donor.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skin in skins)
            {
                Assert.IsNotNull(skin.sharedMesh);
                Assert.IsFalse(borrowed.Any(s => s.sharedMesh == skin.sharedMesh), "a renamed prefab still contains the borrowed visible body");
                foreach (var material in skin.sharedMaterials)
                {
                    var albedo = material.GetTexture("_BaseMap");
                    Assert.IsNotNull(albedo);
                    Assert.IsFalse(borrowed.SelectMany(s => s.sharedMaterials).Any(m => m.GetTexture("_BaseMap") == albedo), "the new body still wears the donor's albedo");
                }
            }
        }
    }
}
