using System.Collections;
using System.Collections.Generic;
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
    public sealed class EnemyBreakTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 position)
        {var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}

        [UnityTest] public IEnumerator BreakImmediatelyCancelsBothBossSpecialTells()
        {
            var failures=new List<string>();var actor=PartyController.Current.Active;
            foreach(var partner in PartyController.Current.members)Place(partner,new Vector3(40,1,-16));
            foreach(string id in new[]{"Burrower","Cantor"})
            {
                Place(actor,new Vector3(0,1,-14));actor.Health.integrity=10000;actor.Health.InvulnerableUntil=0;
                var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),new Vector3(0,1,-12));
                var boss=enemy.GetComponent<BossController>();
                float deadline=Time.unscaledTime+9;
                while(!boss.Telegraphing&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(boss.Telegraphing,id+" never began a real special");
                enemy.Health.Receive(new DamagePacket{source=actor.Health,amount=1,breakPower=10000,type=DamageType.Pulse});
                Assert.IsTrue(enemy.Health.Broken);
                yield return null;
                if(boss.Telegraphing||boss.Busy)failures.Add(id+" special tell survives break");
                float before=actor.Health.integrity;
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.AreEqual(before,actor.Health.integrity,id+" interrupted special released damage");
                Object.Destroy(enemy.gameObject);yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        [UnityTest] public IEnumerator AnAlreadyReleasedProjectileSurvivesItsOwnersBreak()
        {
            var actor=PartyController.Current.Active;Place(actor,new Vector3(0,1,-14));
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(0,1,-6));enemy.Passive=true;
            yield return null;
            actor.Health.InvulnerableUntil=0;float before=actor.Health.integrity;
            Projectile.Fire(enemy.transform.position+Vector3.up*.9f,Vector3.back,new DamagePacket{source=enemy.Health,amount=10,type=DamageType.Pulse},11);
            enemy.Health.Receive(new DamagePacket{source=actor.Health,amount=1,breakPower=1000,type=DamageType.Pulse});
            Assert.IsTrue(enemy.Health.Broken);
            yield return new WaitForSecondsRealtime(1);
            Assert.Less(actor.Health.integrity,before,"break must not erase a projectile already released into the world");
        }

        [UnityTest] public IEnumerator BreakCancelsAnExistingUnreleasedHitVolume()
        {
            var actor=PartyController.Current.Active;Place(actor,new Vector3(0,1,-14));
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),new Vector3(0,1,-10));enemy.Passive=true;
            yield return null;
            actor.Health.InvulnerableUntil=0;float before=actor.Health.integrity;
            CombatActor.Strike(actor.transform.position+Vector3.up*.7f,1,new DamagePacket{source=enemy.Health,amount=10,type=DamageType.Pulse});
            enemy.Health.Receive(new DamagePacket{source=actor.Health,amount=1,breakPower=1000,type=DamageType.Pulse});
            Assert.IsTrue(enemy.Health.Broken,"fixture must actually break its owner");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(before,actor.Health.integrity,"a broken enemy's already-created pending volume still damages");
        }

        [UnityTest] public IEnumerator BreakCancelsTheOldTellAndRequiresANewWindup()
        {
            var failures=new List<string>();var actor=PartyController.Current.Active;
            foreach(var partner in PartyController.Current.members)Place(partner,new Vector3(40,1,-16));
            foreach(string id in new[]{"SentinelHusk","Ridgehound","ChoristerDrifter"})
            {
                Place(actor,new Vector3(0,1,-14));actor.Health.maximum=actor.Health.integrity=10000;actor.Health.InvulnerableUntil=0;
                foreach(var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))Object.Destroy(shot.gameObject);
                foreach(var hit in Object.FindObjectsByType<HitVolume>(FindObjectsSortMode.None))Object.Destroy(hit.gameObject);
                var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),new Vector3(0,1,-12));
                float deadline=Time.unscaledTime+3;
                while(!enemy.Telegraphing&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(enemy.Telegraphing,id+" never entered its real attack tell");
                enemy.Health.Receive(new DamagePacket{source=actor.Health,amount=1,breakPower=1000,type=DamageType.Pulse});
                Assert.IsTrue(enemy.Health.Broken,id+" break stimulus failed");
                yield return null;
                if(enemy.Telegraphing)failures.Add(id+" retained the pre-break attack tell");
                if(enemy.transform.Find("Telegraph").gameObject.activeSelf)failures.Add(id+" retained the visible pre-break warning");
                float before=actor.Health.integrity,breakEnds=enemy.Health.BrokenUntil;
                while(Time.time<breakEnds)yield return null;
                bool newTell=false;float firstNewTell=-1,firstHit=-1;
                deadline=Time.unscaledTime+3;
                while(Time.unscaledTime<deadline&&firstHit<0)
                {
                    if(enemy.Telegraphing&&!newTell){newTell=true;firstNewTell=Time.time;if(enemy.TelegraphRemaining<.3f)failures.Add(id+" reused an expired tell on recovery");}
                    if(actor.Health.integrity<before)firstHit=Time.time;
                    yield return null;
                }
                if(!newTell)failures.Add(id+" recovered without a fresh visible tell");
                if(firstHit>=0&&firstHit-breakEnds<.3f)failures.Add(id+" released an old hit immediately after break");
                Assert.GreaterOrEqual(firstHit,0,id+" never resumed a real damaging attack");
                Debug.Log($"BREAK_RECOVERY {id} end={breakEnds:F4} tell={firstNewTell:F4} hit={firstHit:F4}");
                Object.Destroy(enemy.gameObject);yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
