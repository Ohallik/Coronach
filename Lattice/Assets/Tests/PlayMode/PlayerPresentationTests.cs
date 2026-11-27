using System.Collections;
using Lattice.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class PlayerPresentationTests
    {
        [UnityTest] public IEnumerator BootEnablesDisplaySynchronization()
        {
            // The services go and the boot scene loads in the one frame: the test before may have left a zone up,
            // and a frame between would have its bodies ask services that are gone (the whole suite of November 26:
            // six null references from a party, a boss and a panel, none of them the game's).
            if(GameServices.Current!=null)Object.Destroy(GameServices.Current.gameObject);
            SceneManager.LoadScene("_Boot");yield return null;yield return null;yield return new WaitForSecondsRealtime(.5f);
            Assert.IsNotNull(GameServices.Current);
            Assert.AreEqual(1,QualitySettings.vSyncCount,"software frame caps permit tearing during camera movement");
            Assert.AreEqual(Application.isBatchMode?60:-1,Application.targetFrameRate,"desktop sync must own presentation; batch tests need an explicit clock");
        }
    }
}
