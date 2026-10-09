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

namespace Lattice.Tests.PlayMode
{
    // Actual imported line observation and saved return; ordinary flight input is separate.
    public sealed class TallowObservationFlowTests
    {
        DiscoveryPoint Point=>Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).Single(p=>p.flag=="tallow.lineObserved");
        bool Observed=>GameServices.Current.Flags.GetBool("tallow.lineObserved");
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();DevLoadout.Apply("starter");yield return Load("TallowApproach");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);Assert.AreEqual(zone,SceneManager.GetActiveScene().name);
        }
        IEnumerator Finish()
        {
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;float until=Time.unscaledTime+12;
            while(dialogue.Running&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(dialogue.Running);yield return null;
        }
        void CheckBinding()
        {Assert.AreEqual("SelaTallowLine",Point.dialogueNode);Assert.AreEqual("Sela",Point.dialogueSpeaker);Assert.IsEmpty(Point.report);Assert.IsEmpty(Point.situation);}
        [UnityTest] public IEnumerator CompletedLinePersistsThroughRevisitAndActualKeeperReturn()
        {
            CheckBinding();int money=GameServices.Current.State.scrip;
            Point.Interact();Assert.IsTrue(DialogueSystem.Current.Running);Assert.IsFalse(Observed);Assert.IsFalse(Point.Available);
            yield return null;yield return null;Assert.Greater(DialogueSystem.Current.LinesPresented,0);Assert.IsFalse(Observed);
            yield return Finish();Assert.IsTrue(Observed);Assert.IsFalse(Point.Available);
            Assert.IsFalse(GameServices.Current.Flags.GetBool("tallow.driftDiscussed"));Assert.AreEqual(money,GameServices.Current.State.scrip);
            var saved=GameServices.Current.Saves.Load("autosave");Assert.IsNotNull(saved);Assert.IsTrue(new FlagService(saved.flags).GetBool("tallow.lineObserved"));
            GameServices.Current.State=saved;yield return Load("TallowDrift");
            Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Keeper").Interact();
            Assert.IsTrue(DialogueSystem.Current.Running);Assert.IsFalse(GameServices.Current.Flags.GetBool("tallow.driftDiscussed"));
            yield return Finish();Assert.IsTrue(GameServices.Current.Flags.GetBool("tallow.driftDiscussed"));
            saved=GameServices.Current.Saves.Load("autosave");Assert.IsTrue(new FlagService(saved.flags).GetBool("tallow.driftDiscussed"));
            GameServices.Current.State=saved;yield return Load("TallowApproach");Assert.IsTrue(Observed);Assert.IsFalse(Point.Available);
            Point.Interact();Assert.IsFalse(DialogueSystem.Current.Running);Assert.AreEqual(money,GameServices.Current.State.scrip);
            Assert.IsFalse(GameServices.Current.Flags.GetBool("hushwell.nursery"));
        }
        [UnityTest] public IEnumerator CancelledLineDoesNotSaveTheDiscoveryAndCanBeRetriedAfterRevisit()
        {
            CheckBinding();Point.Interact();yield return null;yield return null;Assert.IsTrue(DialogueSystem.Current.Running);
            DialogueSystem.Current.Runner.Stop().Forget();float until=Time.unscaledTime+3;
            while(DialogueSystem.Current.Running&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running);Assert.IsFalse(Observed);Assert.IsTrue(Point.Available);
            Assert.IsFalse(new FlagService(GameServices.Current.Saves.Load("autosave").flags).GetBool("tallow.lineObserved"));
            yield return Load("TallowDrift");yield return Load("TallowApproach");Assert.IsFalse(Observed);Assert.IsTrue(Point.Available);
            Point.Interact();Assert.IsTrue(DialogueSystem.Current.Running);yield return Finish();Assert.IsTrue(Observed);
        }
    }
}
