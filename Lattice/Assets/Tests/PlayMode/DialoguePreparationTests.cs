using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.Dialogue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class DialoguePreparationTests
    {
        [UnityTest] public IEnumerator LoadingPreparesDialogueWithoutProgressOrEarlyControl()
        {
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();
            var state=GameServices.Current.State;
            var flags=state.flags.ToArray();int scrip=state.scrip;
            SceneFlow.Current.LoadZone("Hub_Decks");
            float deadline=Time.realtimeSinceStartup+8;
            while(SceneFlow.Current.Loading&&Time.realtimeSinceStartup<deadline)
            {
                Assert.IsTrue(GameInput.Current.Blocked,"preparing dialogue must not release world input before the load fade completes");
                yield return null;
            }
            Assert.IsFalse(SceneFlow.Current.Loading,"dialogue preparation must finish under the loading deadline");
            var dialogue=DialogueSystem.Current;
            Assert.IsTrue(dialogue.Prepared);Assert.IsFalse(dialogue.Preparing);Assert.IsFalse(dialogue.Running);
            Assert.AreEqual(0,dialogue.LinesPresented,"preparation is not a story line");
            Assert.IsNull(dialogue.Speaker);CollectionAssert.AreEquivalent(flags,state.flags.ToArray());Assert.AreEqual(scrip,state.scrip);
            Assert.IsFalse(GameInput.Current.Blocked);
            dialogue.StartNode("MiraFirst","Mira");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(dialogue.Running);Assert.Greater(dialogue.LinesPresented,0);Assert.IsTrue(GameInput.Current.Blocked);
            SceneManager.LoadScene("_Boot");yield return null;
        }
    }
}
