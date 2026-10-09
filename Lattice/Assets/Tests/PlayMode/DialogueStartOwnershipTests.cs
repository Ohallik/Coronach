using System.Collections;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class DialogueStartOwnershipTests
    {
        DialogueSystem dialogue;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);GameServices.Current.State=new GameState();DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("TallowDrift");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);dialogue=DialogueSystem.Current;
            Assert.IsTrue(dialogue.Prepared);Assert.IsFalse(dialogue.Running);Assert.IsFalse(GameInput.Current.Blocked);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        IEnumerator Reject(string node)
        {
            int completed=0;bool accepted=dialogue.StartNode(node,"Keeper",_=>completed++);
            Debug.Log($"DIALOGUE_INVALID_START node={node??"<null>"} accepted={accepted} running={dialogue.Running} blocked={GameInput.Current.Blocked}");
            Assert.IsFalse(accepted,"invalid node was accepted and claimed conversation ownership");
            yield return null;yield return null;
            Assert.IsFalse(dialogue.Running);Assert.IsFalse(GameInput.Current.Blocked);Assert.AreEqual(0,completed);
            Assert.IsTrue(dialogue.StartNode("KeeperRepeat","Keeper"));Assert.IsTrue(dialogue.Running);
            dialogue.AutoAdvance=true;float until=Time.unscaledTime+8;
            while(dialogue.Running&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(dialogue.Running);Assert.IsFalse(GameInput.Current.Blocked);
        }
        [UnityTest] public IEnumerator UnknownNodeDoesNotClaimInput()=>Reject("__Missing_Tallow_Node_Regression");
        [UnityTest] public IEnumerator EmptyNodeDoesNotClaimInput()=>Reject("");
        [UnityTest] public IEnumerator NullNodeDoesNotClaimInput()=>Reject(null);
    }
}
