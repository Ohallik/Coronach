using System;
using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Lattice.Tests.PlayMode
{
    // Optional NPC completion ownership, isolated from the actual Yarn/bridge flow.
    // Actual Yarn/bridge integration has separate end-to-end requirements.
    public sealed class NpcAcknowledgementTests
    {
        Npc npc;Action<bool> complete;string node;int requests,saves;
        void Heard(string selected,string speaker,Action<bool> callback)
        {node=selected;complete=callback;requests++;}
        void Log(string text,string stack,LogType type)
        {if(text.StartsWith("SAVE_OK slot=autosave "))saves++;}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();
            foreach(var bridge in Object.FindObjectsByType<WorldUiBridge>(FindObjectsSortMode.None))bridge.enabled=false;
            npc=new GameObject("Completion fixture").AddComponent<Npc>();
            npc.speaker="Keeper";npc.firstNode="First";npc.repeatNode="Repeat";npc.postNode="Post";npc.postFlag="warpkey";
            npc.finalNode="Final";npc.finalFlag="tallow.lineObserved";
            npc.finalCompletionFlag="tallow.driftDiscussed";npc.finalRepeatNode="FinalRepeat";
            requests=0;saves=0;node=null;complete=null;
            Npc.TalkRequested+=Heard;Application.logMessageReceived+=Log;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Heard;Application.logMessageReceived-=Log;
            if(npc!=null)Object.Destroy(npc.gameObject);
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
        }
        void Observe(){GameServices.Current.Flags.SetBool("tallow.lineObserved",true);}
        bool Acknowledged=>GameServices.Current.Flags.GetBool("tallow.driftDiscussed");
        [Test] public void AcknowledgementWaitsForCompletionAndPersistsExactlyOnce()
        {
            Observe();int scrip=GameServices.Current.State.scrip;npc.Interact();
            Assert.AreEqual("Final",node);Assert.IsFalse(Acknowledged);Assert.IsFalse(npc.Available);
            npc.Interact();Assert.AreEqual(1,requests,"pending request duplicated");
            complete(true);Assert.IsTrue(Acknowledged);Assert.IsTrue(npc.Available);Assert.AreEqual(1,saves);
            var loaded=GameServices.Current.Saves.Load("autosave");Assert.NotNull(loaded);
            Assert.IsTrue(new FlagService(loaded.flags).GetBool("tallow.driftDiscussed"));
            complete(true);Assert.AreEqual(1,saves,"duplicate callback saved again");
            Assert.AreEqual(scrip,GameServices.Current.State.scrip,"acknowledgement awarded money");
            npc.Interact();Assert.AreEqual("FinalRepeat",node);complete(true);Assert.AreEqual(1,saves);
        }
        [Test] public void CancelledRequestCanRetryWithoutAcknowledgement()
        {
            Observe();npc.Interact();complete(false);
            Assert.IsFalse(Acknowledged);Assert.IsTrue(npc.Available);Assert.AreEqual(0,saves);
            npc.Interact();Assert.AreEqual("Final",node);complete(true);Assert.IsTrue(Acknowledged);
        }
        [Test] public void DisabledRequestCannotComplete()
        {
            Observe();npc.Interact();npc.enabled=false;complete(true);
            Assert.IsFalse(Acknowledged);Assert.AreEqual(0,saves);
        }
        [Test] public void DestroyedRequestCannotComplete()
        {
            Observe();npc.Interact();Object.DestroyImmediate(npc.gameObject);complete(true);
            Assert.IsFalse(Acknowledged);Assert.AreEqual(0,saves);
        }
        [Test] public void ReplacedStateCannotReceiveOldAcknowledgement()
        {
            Observe();var old=GameServices.Current.State;npc.Interact();GameServices.Current.State=new GameState();complete(true);
            Assert.IsFalse(Acknowledged);Assert.IsFalse(new FlagService(old.flags).GetBool("tallow.driftDiscussed"));Assert.AreEqual(0,saves);
        }
        [Test] public void EarlierCallbackCannotCloseANewerRequest()
        {
            Observe();npc.Interact();var stale=complete;npc.enabled=false;npc.enabled=true;npc.Interact();
            var current=complete;Assert.AreEqual(2,requests);stale(true);
            Assert.IsFalse(Acknowledged);Assert.IsFalse(npc.Available);Assert.AreEqual(0,saves);
            current(true);Assert.IsTrue(Acknowledged);Assert.AreEqual(1,saves);
        }
        [Test] public void ExistingFirstRepeatAndPostTiersRemainAndDoNotAcknowledge()
        {
            npc.Interact();Assert.AreEqual("First",node);Assert.IsTrue(GameServices.Current.Flags.GetBool("met.Keeper"));complete(true);
            npc.Interact();Assert.AreEqual("Repeat",node);complete(true);
            GameServices.Current.Flags.SetBool("warpkey",true);npc.Interact();Assert.AreEqual("Post",node);complete(true);
            Assert.IsFalse(Acknowledged);Assert.AreEqual(0,saves);
            Observe();npc.Interact();Assert.AreEqual("Final",node);complete(true);
            Assert.IsTrue(Acknowledged);Assert.AreEqual(1,saves);
        }
        [Test] public void OlderNpcWithoutCompletionFieldsKeepsItsFinalTier()
        {
            npc.finalCompletionFlag=null;npc.finalRepeatNode=null;Observe();
            npc.Interact();Assert.AreEqual("Final",node);complete(true);
            npc.Interact();Assert.AreEqual("Final",node);complete(true);
            Assert.IsFalse(Acknowledged);Assert.AreEqual(0,saves);
        }
    }
}
