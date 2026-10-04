using System.Collections;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace Lattice.Tests.PlayMode
{
    public sealed class OpeningChapterTests
    {
        Gamepad pad;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");pad=InputSystem.AddDevice<Gamepad>();
            var flags=GameServices.Current.Flags;
            flags.SetBool("met.Orrin",true);flags.SetBool("met.Survivor",true);
            flags.SetBool("bossdown.Burrower",true);flags.SetBool("clear.Sorrel_Burrower",true);flags.SetBool("warpkey",true);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Runner.Stop().Forget();yield return null;yield return null;}
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");
            yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+12;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);Assert.AreEqual(zone,SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(.4f);DialogueSystem.Current.AutoAdvance=false;
        }
        IEnumerator Tap()
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;yield return null;
        }
        IEnumerator Finish()
        {
            float deadline=Time.unscaledTime+30;
            while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return Tap();
            Assert.IsFalse(DialogueSystem.Current.Running,"discovery did not finish through ordinary confirm input");
        }
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text;
        static DiscoveryPoint Discovery()=>Object.FindFirstObjectByType<DiscoveryPoint>();
        static bool Discovered=>GameServices.Current.Flags.GetBool("hushwell.nursery");
        static void BeforeDiscovery()
        {
            var state=GameServices.Current.State;state.flags["bossdown.BellowsBelow"]=true;
            state.flags["clear.Hushwell_Bellows"]=true;state.questSteps["Hushwell"]=1;
        }

        [UnityTest] public IEnumerator ANewKeyOwnerIsDirectedIntoTheBoreBeforeReturningToOrbit()
        {
            yield return Load("Sorrel_Ridges","Hushwell");
            StringAssert.Contains("Hushwell",Objective(),"the main objective still skips the nursery");
            Assert.IsTrue(Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Any(b=>b.scene=="Hushwell"));
        }

        [UnityTest] public IEnumerator TheHaloBeaconRequiresTheDiscoveryAsWellAsTheKey()
        {
            yield return Load("Hub_CinderHalo","Outer");
            var beacon=Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Single(b=>b.scene=="Gullet_Tunnel");
            beacon.Interact();yield return new WaitForSecondsRealtime(2);
            Assert.AreEqual("Hub_CinderHalo",GameServices.Current.State.zone,"an uncalibrated key bypassed Hushwell");
            Assert.IsFalse(SceneFlow.Current.Loading);StringAssert.Contains("Sorrel",Objective());
            GameServices.Current.Flags.SetBool("hushwell.nursery",true);beacon.Interact();
            float deadline=Time.unscaledTime+12;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);Assert.AreEqual("Gullet_Tunnel",GameServices.Current.State.zone);
        }

        [UnityTest] public IEnumerator TheNurseryConversationCommitsAndReloadsItsRewardOnlyOnce()
        {
            BeforeDiscovery();yield return Load("Hushwell");
            var state=GameServices.Current.State;int money=state.scrip;
            int lines=DialogueSystem.Current.LinesPresented;var discovery=Discovery();discovery.Interact();yield return null;yield return null;
            Assert.IsFalse(Discovered,"interacting granted the discovery before its conversation");
            Assert.IsTrue(DialogueSystem.Current.Running);Assert.IsTrue(GameInput.Current.Blocked);
            yield return Finish();Assert.GreaterOrEqual(DialogueSystem.Current.LinesPresented-lines,4);
            Assert.IsTrue(Discovered);Assert.IsFalse(discovery.Available);
            Assert.AreEqual(money+150,state.scrip);Assert.AreEqual(2,state.questSteps["Hushwell"]);
            int xp=state.party.Sum(m=>m.xp);var saved=GameServices.Current.Saves.Load("autosave");
            Assert.IsNotNull(saved);Assert.IsTrue(saved.flags["hushwell.nursery"]);Assert.AreEqual(state.scrip,saved.scrip);
            discovery.Interact();yield return null;Assert.IsFalse(DialogueSystem.Current.Running);
            Assert.AreEqual(money+150,state.scrip);Assert.AreEqual(xp,state.party.Sum(m=>m.xp));
            GameServices.Current.State=saved;yield return Load("Sorrel_Ridges","Hushwell");
            StringAssert.Contains("Halo",Objective());
        }

        [UnityTest] public IEnumerator CancellingTheNurseryLeavesItAvailableAndDoesNotSaveAReward()
        {
            BeforeDiscovery();yield return Load("Hushwell");
            var state=GameServices.Current.State;int money=state.scrip;GameServices.Current.Saves.Save("autosave",state);
            string path=GameServices.Current.Saves.PathForSlot("autosave");var before=File.ReadAllBytes(path);
            var discovery=Discovery();discovery.Interact();yield return null;yield return null;
            Assert.IsFalse(Discovered,"the old instant discovery cannot be cancelled");
            Assert.IsTrue(DialogueSystem.Current.Running);bool stopped=false;
            async YarnTask Stop(){await DialogueSystem.Current.Runner.Stop();stopped=true;}Stop().Forget();
            float deadline=Time.unscaledTime+5;while(!stopped&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(stopped);Assert.IsFalse(Discovered);Assert.IsTrue(discovery.Available);
            Assert.AreEqual(money,state.scrip);Assert.AreEqual(1,state.questSteps["Hushwell"]);
            CollectionAssert.AreEqual(before,File.ReadAllBytes(path));Assert.IsFalse(GameInput.Current.Blocked);
            discovery.Interact();yield return null;yield return null;yield return Finish();Assert.IsTrue(Discovered);
        }

        [UnityTest] public IEnumerator LeavingDuringTheDiscoveryCannotCompleteItInTheNextScene()
        {
            BeforeDiscovery();yield return Load("Hushwell");Discovery().Interact();yield return null;yield return null;
            Assert.IsFalse(Discovered);Assert.IsTrue(DialogueSystem.Current.Running);
            int money=GameServices.Current.State.scrip;yield return Load("Sorrel_Ridges","Hushwell");
            Assert.IsFalse(Discovered);Assert.AreEqual(money,GameServices.Current.State.scrip);
            Assert.AreEqual(1,GameServices.Current.State.questSteps["Hushwell"]);
            StringAssert.Contains("Hushwell",Objective());
        }

        [UnityTest] public IEnumerator ReplacingTheLoadedStateCannotGiveItsProfileAnEarlierDiscovery()
        {
            BeforeDiscovery();yield return Load("Hushwell");var discovery=Discovery();
            discovery.Interact();yield return null;yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,"the discovery has no conversation to own");
            var replacement=new GameState();replacement.flags["warpkey"]=true;
            replacement.questSteps["Hushwell"]=1;GameServices.Current.State=replacement;
            GameServices.Current.Saves.Save("autosave",replacement);
            var path=GameServices.Current.Saves.PathForSlot("autosave");var bytes=File.ReadAllBytes(path);
            yield return Finish();Assert.IsFalse(Discovered);Assert.IsTrue(discovery.Available);
            Assert.AreEqual(120,replacement.scrip);Assert.AreEqual(1,replacement.questSteps["Hushwell"]);
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
        }
    }
}
