using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>A development loadout stands in for a real save, so it must be a
    /// state a player can reach: the warp key comes only from the anvil the
    /// Burrower guards, and Hushwell opens only once it is down.</summary>
    public sealed class LoadoutStateTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");}
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        [UnityTest] public IEnumerator TheGulletPartyReturnsFromHushwellToADefeatedBurrower()
        {
            DevLoadout.Apply("gullet");var flags=GameServices.Current.Flags;
            Assert.IsTrue(flags.GetBool("warpkey"));
            Assert.IsTrue(flags.GetBool("bossdown.Burrower"),"the gullet party holds the warp key but its Burrower still lives");
            // The drill-shaft lift's arrival sits in the Burrower's arena.
            SceneFlow.Current.LoadZone("Sorrel_Ridges","Hushwell");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(1.5f);
            var burrowers=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition.id=="Burrower"&&e.Health.Alive).ToArray();
            Assert.IsEmpty(burrowers,"the lift delivers the party into a living Burrower's arena");
            Assert.Greater(PartyController.Current.Active.Health.integrity,PartyController.Current.Active.Health.maximum-.5f,"the party is struck on arrival");
            // Every earlier Sorrel objective is behind the party: only the way home remains.
            string objective=Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Objective").text;
            StringAssert.Contains("Return to the Halo",objective,"the gullet party's Sorrel objective has not moved on");
        }
    }
}
