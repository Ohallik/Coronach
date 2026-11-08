using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The fourth eye pass: crossing the Halo, every star stayed on the same pixel from 102 m to 3 m from the warp, dust on the lens rather than space going past, in the emptiest
    /// stretch of the first flight. A flight camera never rotates, so a skybox is painted on the screen. The Reach already had the answer (D219), and these are the other two open skies.</summary>
    public sealed class QaLastSkyTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        [UnityTest] public IEnumerator TheOpenSkyGoesPastAsTheShipDoes([Values("Hub_CinderHalo","TallowApproach")] string zone)
        {
            yield return Zone(zone);
            Assert.AreEqual("Lattice/DriftingSky",RenderSettings.skybox.shader.name,$"{zone}'s sky is painted on the screen: every star stays on its pixel while the ship crosses 100 m");
            Assert.IsTrue(RenderSettings.skybox.shader.isSupported,"the sky's shader didn't compile");
            var lead=PartyController.Current.Active;var start=lead.transform.position;
            Put(lead,start);yield return new WaitForSecondsRealtime(1.5f);
            var there=Shader.GetGlobalVector("_SkyTurn");
            Put(lead,start+Vector3.forward*100);yield return new WaitForSecondsRealtime(1.5f);
            var here=Shader.GetGlobalVector("_SkyTurn");
            Assert.Greater(Mathf.Abs(here.x-there.x),2.5f,$"flying 100 m in {zone} turned the sky by {Mathf.Abs(here.x-there.x):0.0} degrees: nothing passes behind the ships");
        }
    }
}
