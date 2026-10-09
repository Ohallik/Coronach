using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;
using Object=UnityEngine.Object;

namespace Lattice.Tests.PlayMode
{
    // Real imported Tallow nodes and NPC callback bridge, without a substitute speaker.
    public sealed class TallowAcknowledgementFlowTests
    {
        Npc keeper;DialogueSystem dialogue;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("TallowDrift");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);dialogue=DialogueSystem.Current;
            Assert.IsTrue(dialogue.Prepared);keeper=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Keeper");
            Assert.AreEqual("tallow.lineObserved",keeper.finalFlag);
            Assert.AreEqual("tallow.driftDiscussed",keeper.finalCompletionFlag);
            Assert.AreEqual("KeeperDriftReturn",keeper.finalNode);Assert.AreEqual("KeeperDriftRepeat",keeper.finalRepeatNode);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }
        bool Acknowledged=>GameServices.Current.Flags.GetBool("tallow.driftDiscussed");
        void Observe(){GameServices.Current.Flags.SetBool("tallow.lineObserved",true);}
        IEnumerator Finish()
        {
            dialogue.AutoAdvance=true;float until=Time.unscaledTime+12;
            while(dialogue.Running&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(dialogue.Running);yield return null;
        }
        [UnityTest] public IEnumerator ActualConversationSavesOnlyAfterItsLastLine()
        {
            Observe();var state=GameServices.Current.State;int scrip=state.scrip;
            keeper.Interact();Assert.IsTrue(dialogue.Running);Assert.IsFalse(Acknowledged);
            Assert.IsFalse(keeper.Available);yield return null;yield return null;
            Assert.Greater(dialogue.LinesPresented,0);Assert.IsFalse(Acknowledged);
            yield return Finish();Assert.IsTrue(Acknowledged);Assert.IsTrue(keeper.Available);
            Assert.AreEqual(scrip,state.scrip);Assert.IsFalse(GameServices.Current.Flags.GetBool("hushwell.nursery"));
            var saved=GameServices.Current.Saves.Load("autosave");Assert.NotNull(saved);
            Assert.IsTrue(new FlagService(saved.flags).GetBool("tallow.lineObserved"));
            Assert.IsTrue(new FlagService(saved.flags).GetBool("tallow.driftDiscussed"));
            GameServices.Current.State=saved;keeper.Interact();yield return Finish();
            Assert.AreEqual(scrip,GameServices.Current.State.scrip);
        }
        [UnityTest] public IEnumerator InterruptedActualConversationCanBeRetried()
        {
            Observe();keeper.Interact();yield return null;yield return null;
            Assert.IsTrue(dialogue.Running);dialogue.Runner.Stop().Forget();
            float until=Time.unscaledTime+3;while(dialogue.Running&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(dialogue.Running);Assert.IsFalse(Acknowledged);Assert.IsTrue(keeper.Available);
            keeper.Interact();yield return Finish();Assert.IsTrue(Acknowledged);
        }
        [UnityTest] public IEnumerator BusyDialogueRejectsTheRequestWithoutLosingItsRetry()
        {
            Observe();Assert.IsTrue(dialogue.StartNode("KeeperRepeat","Keeper"));
            keeper.Interact();Assert.IsTrue(keeper.Available);Assert.IsFalse(Acknowledged);
            yield return Finish();Assert.IsFalse(Acknowledged);
            keeper.Interact();yield return Finish();Assert.IsTrue(Acknowledged);
        }
        [UnityTest] public IEnumerator DisabledSpeakerCannotAcknowledgeAfterDialogueFinishes()
        {
            Observe();keeper.Interact();yield return null;keeper.enabled=false;
            yield return Finish();Assert.IsFalse(Acknowledged);
            keeper.enabled=true;keeper.Interact();yield return Finish();Assert.IsTrue(Acknowledged);
        }
        [UnityTest] public IEnumerator SceneExitDoesNotTurnInterruptedWordsIntoAnAcknowledgement()
        {
            Observe();keeper.Interact();yield return null;yield return null;
            SceneFlow.Current.LoadZone("TallowApproach");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);Assert.IsFalse(Acknowledged);
            var saved=GameServices.Current.Saves.Load("autosave");Assert.NotNull(saved);
            Assert.IsFalse(new FlagService(saved.flags).GetBool("tallow.driftDiscussed"));
        }
        [UnityTest] public IEnumerator UnknownNodeCannotOwnInputOrStrandTheNpc()
        {
            Observe();keeper.finalNode="__Missing_Tallow_Node_Regression";
            keeper.Interact();yield return null;yield return null;
            Assert.IsFalse(dialogue.Running);Assert.IsFalse(GameInput.Current.Blocked);
            Assert.IsTrue(keeper.Available);Assert.IsFalse(Acknowledged);
            keeper.finalNode="KeeperDriftReturn";keeper.Interact();yield return Finish();Assert.IsTrue(Acknowledged);
        }
    }
}
