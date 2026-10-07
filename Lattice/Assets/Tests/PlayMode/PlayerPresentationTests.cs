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
            if(GameServices.Current!=null)Object.Destroy(GameServices.Current.gameObject);
            yield return null;
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            Assert.IsNotNull(GameServices.Current);
            Assert.AreEqual(1,QualitySettings.vSyncCount,"software frame caps permit tearing during camera movement");
            Assert.AreEqual(Application.isBatchMode?60:-1,Application.targetFrameRate,"desktop sync must own presentation; batch tests need an explicit clock");
        }
    }
}
