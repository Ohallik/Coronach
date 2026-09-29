using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class MusicCueTests
    {
        GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(fixture!=null)Object.Destroy(fixture);
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.3f);
        }
        EnemyBrain Boss(string id,Vector3 offset)
        {
            if(fixture==null)fixture=new GameObject("Boss music fixture");
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),PartyController.Current.Active.transform.position+offset);
            boss.transform.SetParent(fixture.transform);boss.Passive=true;return boss;
        }
        static void Defeat(EnemyBrain boss)=>boss.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=boss.Health.maximum*50,type=DamageType.Pulse});

        [UnityTest] public IEnumerator FlightCombatAndBossEncountersHaveTheirOwnCues()
        {
            yield return Load("Gullet_Tunnel");
            Assert.AreEqual("Starfight",MusicDirector.Current.Track,"the Gullet's combat route is silent");
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsTrue(MusicDirector.Current.ActiveSource.isPlaying);Assert.Greater(MusicDirector.Current.ActiveSource.volume,.4f);

            var cantor=Boss("Cantor",Vector3.forward*25);yield return null;yield return null;
            Assert.AreEqual("Alien Boss Battle",MusicDirector.Current.Track,"a boss encounter keeps the zone cue");
            Defeat(cantor);yield return null;
            Assert.IsFalse(cantor.Health.Alive);
            Assert.AreEqual("Starfight",MusicDirector.Current.Track,"boss defeat did not return to the zone cue");

            // A retry or zone reset removes a living boss without a death.
            var again=Boss("Cantor",Vector3.forward*25);yield return null;yield return null;
            Assert.AreEqual("Alien Boss Battle",MusicDirector.Current.Track);
            Object.Destroy(again.gameObject);yield return null;yield return null;
            Assert.AreEqual("Starfight",MusicDirector.Current.Track,"a removed boss left its cue playing");

            // Ground bosses use the same cue over the moon's combat music; the
            // four original assignments are unchanged.
            yield return Load("Sorrel_Ridges");ZoneController.Current.SafePocket(false);
            Assert.AreEqual("Adventure Awaits",MusicDirector.Current.Track);
            var burrower=Boss("Burrower",Vector3.forward*12);yield return null;yield return null;
            Assert.AreEqual("Alien Boss Battle",MusicDirector.Current.Track);
            Defeat(burrower);yield return null;
            Assert.AreEqual("Adventure Awaits",MusicDirector.Current.Track);
            yield return Load("Hub_Decks");Assert.AreEqual("Hub Town Groove",MusicDirector.Current.Track);
        }
    }
}
