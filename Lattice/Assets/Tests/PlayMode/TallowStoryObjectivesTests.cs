using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class TallowStoryObjectivesTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();DevLoadout.Apply("starter");
            foreach(string flag in new[]{"met.Keeper","sliceComplete","tallow.lineObserved","tallow.driftDiscussed"})GameServices.Current.Flags.SetBool(flag,false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.4f);
        }
        string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Objective").text;
        [UnityTest] public IEnumerator FreshArrivalStillDirectsThePartyToTheDock()
        {yield return Load("TallowApproach");StringAssert.Contains("Dock at Tallow",Objective());Assert.IsFalse(GameServices.Current.Flags.GetBool("tallow.lineObserved"));}
        [UnityTest] public IEnumerator KeeperRequestDirectsTheRealLineThenReturnsToTheDock()
        {
            GameServices.Current.Flags.SetBool("met.Keeper",true);yield return Load("TallowApproach");
            StringAssert.Contains("port docking line",Objective());StringAssert.Contains(" m",Objective(),"story target has no actual location");
            Assert.IsTrue(Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).Single(p=>p.flag=="tallow.lineObserved").Available);
            GameServices.Current.Flags.SetBool("tallow.lineObserved",true);yield return new WaitForSecondsRealtime(.4f);
            StringAssert.Contains("Dock at Tallow",Objective());
        }
        [UnityTest] public IEnumerator OldSliceCompletionDoesNotHideTheUnacknowledgedDiscovery()
        {
            GameServices.Current.Flags.SetBool("sliceComplete",true);GameServices.Current.Flags.SetBool("tallow.lineObserved",true);
            yield return Load("TallowDrift");StringAssert.Contains("Tell the Keeper",Objective());StringAssert.Contains(" m",Objective());
            Assert.IsTrue(GameServices.Current.Flags.GetBool("sliceComplete"));Assert.IsFalse(GameServices.Current.Flags.GetBool("hushwell.nursery"));
        }
        [UnityTest] public IEnumerator FreeRepairStillSavesBeforeDirectingTheUnfinishedLineJob()
        {
            GameServices.Current.Flags.SetBool("met.Keeper",true);yield return Load("TallowDrift");StringAssert.Contains("Repair and save",Objective());
            int money=GameServices.Current.State.scrip;var repair=Object.FindFirstObjectByType<RepairBay>();Assert.IsTrue(repair.free);Assert.IsTrue(repair.CompleteSlice);repair.Interact();
            yield return new WaitForSecondsRealtime(.4f);Assert.IsTrue(GameServices.Current.Flags.GetBool("sliceComplete"));Assert.AreEqual(money,GameServices.Current.State.scrip);
            Assert.IsTrue(new FlagService(GameServices.Current.Saves.Load("autosave").flags).GetBool("sliceComplete"));
            Assert.IsFalse(GameServices.Current.Flags.GetBool("tallow.lineObserved"));StringAssert.Contains("Launch to inspect",Objective());
        }
        [UnityTest] public IEnumerator AcknowledgedAndRepairedRefugeKeepsItsSavedCompletion()
        {
            foreach(string flag in new[]{"met.Keeper","sliceComplete","tallow.lineObserved","tallow.driftDiscussed"})GameServices.Current.Flags.SetBool(flag,true);
            yield return Load("TallowDrift");StringAssert.Contains("Tallow is underway",Objective());Assert.IsTrue(GameServices.Current.Flags.GetBool("sliceComplete"));
        }
    }
}
