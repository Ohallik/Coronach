using System.Collections;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class ChapterLoadoutTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);}
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Cave()
        {
            SceneFlow.Current.LoadZone("Hushwell","Arrival");float deadline=Time.unscaledTime+12;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual("Hushwell",SceneManager.GetActiveScene().name);
        }
        [UnityTest] public IEnumerator TheHushwellFixtureStillHasItsDiscoveryAndBossToPlay()
        {
            DevLoadout.Apply("hushwell");var flags=GameServices.Current.Flags;
            Assert.IsTrue(flags.GetBool("warpkey"));Assert.IsTrue(flags.GetBool("bossdown.Burrower"));
            Assert.IsFalse(flags.GetBool("bossdown.BellowsBelow"));Assert.IsFalse(flags.GetBool("hushwell.nursery"));
            Assert.IsFalse(flags.GetBool("legacy.gulletAccess"));yield return Cave();
            Assert.IsTrue(Object.FindFirstObjectByType<DiscoveryPoint>().Available);
            Assert.AreEqual(0,GameServices.Current.State.questSteps["Hushwell"]);
        }
        [UnityTest] public IEnumerator TheGulletFixtureHasAlreadyCompletedTheCaveWithoutLegacyAccess()
        {
            DevLoadout.Apply("gullet");var flags=GameServices.Current.Flags;
            Assert.IsTrue(flags.GetBool("hushwell.nursery"),"the Gullet fixture skips the newly required chapter");
            Assert.IsTrue(flags.GetBool("bossdown.BellowsBelow"));Assert.IsTrue(flags.GetBool("quest.Hushwell.complete"));
            Assert.IsFalse(flags.GetBool("legacy.gulletAccess"));yield return Cave();
            Assert.IsFalse(Object.FindFirstObjectByType<DiscoveryPoint>().Available);
            Assert.AreEqual(2,GameServices.Current.State.questSteps["Hushwell"]);
            Assert.IsFalse(Lattice.Combat.PressureOrgan.AnyPumping,"the completed-cave fixture still has live targets");
        }
    }
}
