using System.Collections;
using System.Linq;
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
    public sealed class PressureOrganPersistenceTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("hushwell");}
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Cave()
        {
            SceneFlow.Current.LoadZone("Hushwell");float deadline=Time.unscaledTime+12;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual("Hushwell",SceneManager.GetActiveScene().name);
        }
        static PressureOrgan[] Organs()=>Object.FindObjectsByType<PressureOrgan>(FindObjectsSortMode.None).OrderBy(o=>o.transform.position.x).ToArray();
        static void SaveAndReloadState()
        {
            var services=GameServices.Current;services.Saves.Save("slot3",services.State);
            var loaded=services.Saves.Load("slot3");Assert.IsNotNull(loaded);services.State=loaded;
        }
        static void Break(Health health)=>health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=100000,type=DamageType.Kinetic});

        [UnityTest] public IEnumerator OneBrokenOrganStaysCollapsedAfterSaveAndReloadWhileTheOtherStillPumps()
        {
            for(int broken=0;broken<2;broken++)
            {
                GameServices.Current.NewGame();DevLoadout.Apply("hushwell");
                yield return Cave();var organs=Organs();Assert.AreEqual(2,organs.Length);
                Break(organs[broken].Health);Assert.IsFalse(organs[broken].Pumping);Assert.IsTrue(organs[1-broken].Pumping);
                var collapsed=organs[broken].visual.localScale;
                SaveAndReloadState();yield return Cave();organs=Organs();
                Assert.IsFalse(organs[broken].Pumping,"the saved safe half starts pumping again");
                Assert.IsTrue(organs[1-broken].Pumping,"breaking one organ incorrectly cleared the other half");
                Assert.Less(Vector3.Distance(collapsed,organs[broken].visual.localScale),.001f,"loaded damage lost its collapsed presentation");
                Assert.IsFalse(GameServices.Current.Flags.GetBool("bossdown.BellowsBelow"));
            }
        }

        [UnityTest] public IEnumerator BellowsDefeatSettlesRemainingOrgansWithoutReplayingTheirDamageEvents()
        {
            yield return Cave();var organs=Organs();int deaths=0;
            foreach(var organ in organs)organ.Health.Died+=(_,__)=>deaths++;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),PartyController.Current.Active.transform.position+Vector3.forward*12);
            boss.Passive=true;yield return null;Break(boss.Health);yield return null;
            Assert.IsTrue(GameServices.Current.Flags.GetBool("bossdown.BellowsBelow"));
            Assert.IsFalse(PressureOrgan.AnyPumping,"the boss is down but its organs still pump");
            Assert.AreEqual(0,deaths,"settling the chamber replayed organ damage/death effects");
            SaveAndReloadState();yield return Cave();
            Assert.IsFalse(PressureOrgan.AnyPumping,"the cleared chamber resurrected on reload");
        }

        [UnityTest] public IEnumerator AnOlderCompletedCaveNeedsNoInventedOrganFlagsToStayQuiet()
        {
            var state=GameServices.Current.State;state.flags["bossdown.BellowsBelow"]=true;state.flags["clear.Hushwell_Bellows"]=true;
            var before=state.flags.Keys.OrderBy(k=>k).ToArray();int scrip=state.scrip,xp=state.party.Sum(m=>m.xp);
            yield return Cave();var organs=Organs();Assert.AreEqual(2,organs.Length);
            Assert.IsFalse(PressureOrgan.AnyPumping,"an existing completion restores live organs");
            foreach(var organ in organs){var scale=organ.visual.localScale;organ.Breathe(1);Assert.AreEqual(scale,organ.visual.localScale);}
            CollectionAssert.AreEqual(before,state.flags.Keys.OrderBy(k=>k).ToArray(),"loading invented organ defeat history");
            Assert.AreEqual(scrip,state.scrip);Assert.AreEqual(xp,state.party.Sum(m=>m.xp));
        }
    }
}
