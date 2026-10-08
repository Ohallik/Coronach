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
    public sealed class CantorCollarTests
    {
        EnemyBrain enemy;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var other in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))other.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.GetComponent<FlightMotor>().Halt();}
            enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(30,1,0));enemy.Passive=true;
            yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        [UnityTest] public IEnumerator TheProtectedLockCannotResolveTheAnimalBeforeTheLinks()
        {
            float before=enemy.Health.integrity;
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=enemy.Health.maximum*100,type=DamageType.Pulse});
            Assert.AreEqual(before,enemy.Health.integrity,"a direct hit bypasses the still-attached collar links");
            Assert.IsFalse(GameServices.Current.Flags.GetBool("bossdown.Cantor"),"collar lock releases prematurely");
            yield return null;
        }
        [UnityTest] public IEnumerator ThreeSeparateMovingLinksShareTheOriginalTotalIntegrityBudget()
        {
            var parts=enemy.GetComponentsInChildren<Health>().Where(h=>h!=enemy.Health).ToArray();
            Assert.AreEqual(3,parts.Length,"the fight still has only one animal health pool");
            Assert.AreEqual(GameCatalog.Find<EnemyDef>("Cantor").integrity,parts.Sum(h=>h.maximum)+enemy.Health.maximum,.01f);
            Assert.AreEqual(3,parts.Select(h=>h.transform.parent).Distinct().Count(),"links need distinct moving body sites");
            foreach(var part in parts)
            {
                Assert.IsTrue(part.Alive);
                Assert.IsNotNull(part.GetComponent<Hurtbox>());
                Assert.AreSame(part,part.GetComponent<Hurtbox>().owner,"link damage still drains the animal directly");
                Assert.Greater(part.GetComponent<Collider>().bounds.size.sqrMagnitude,1);
            }
            yield return null;
        }
        DamagePacket CutPacket(Health part)=>new DamagePacket{source=PartyController.Current.Active.Health,amount=part.maximum*100,type=DamageType.Kinetic};
        void Cut(int index)
        {
            var link=enemy.GetComponent<CantorCollar>().Links[index];
            link.GetComponent<Hurtbox>().Hit(CutPacket(link));
        }
        void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;actor.transform.position=position;body.enabled=true;
            actor.GetComponent<FlightMotor>().Halt();
        }
        [UnityTest] public IEnumerator AllSixCutOrdersRequireTheLockAndResolveRewardsOnlyOnce()
        {
            var orders=new[]{new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}};
            foreach(var order in orders)
            {
                GameServices.Current.Flags.SetBool("bossdown.Cantor",false);
                int scrip=GameServices.Current.State.scrip,deaths=0;
                var collar=enemy.GetComponent<CantorCollar>();enemy.Health.Died+=(_,__)=>deaths++;
                for(int n=0;n<3;n++)
                {
                    Cut(order[n]);
                    Assert.AreEqual(2-n,collar.Remaining);
                    Assert.IsTrue(enemy.Health.Alive);Assert.AreEqual(0,deaths);
                    Assert.IsFalse(GameServices.Current.Flags.GetBool("bossdown.Cantor"));
                    Assert.AreEqual(scrip,GameServices.Current.State.scrip,"a severed link awarded boss rewards");
                    if(n<2)
                    {
                        float held=enemy.Health.integrity;enemy.GetComponent<Hurtbox>().Hit(CutPacket(enemy.Health));
                        Assert.AreEqual(held,enemy.Health.integrity,"lock exposed before all three links were cut");
                    }
                    yield return null;
                }
                Assert.IsTrue(enemy.Health.Targetable);
                enemy.GetComponent<Hurtbox>().Hit(CutPacket(enemy.Health));enemy.GetComponent<Hurtbox>().Hit(CutPacket(enemy.Health));
                Assert.AreEqual(1,deaths);Assert.IsFalse(enemy.Health.Alive);
                Assert.IsTrue(GameServices.Current.Flags.GetBool("bossdown.Cantor"));
                Assert.AreEqual(scrip+150,GameServices.Current.State.scrip,"lock resolution must grant the existing reward once");
                Debug.Log("COLLAR_ORDER_OK "+string.Join(",",order));
                Object.Destroy(enemy.gameObject);yield return null;
                if(order!=orders[orders.Length-1])
                {enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(30,1,0));enemy.Passive=true;yield return null;yield return null;}
            }
        }
        [UnityTest] public IEnumerator TargetCyclingSkipsTheProtectedLockAndFindsItAfterAllCuts()
        {
            foreach(var other in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))if(other!=enemy)
                foreach(var health in other.GetComponentsInChildren<Health>())health.Damageable=false;
            var actor=PartyController.Current.Active;Place(actor,new Vector3(30,1,-8));
            var brain=actor.GetComponent<PlayerBrain>();var collar=enemy.GetComponent<CantorCollar>();
            actor.target=brain.FindTarget();Assert.Contains(actor.target,collar.Links.ToArray());
            var found=new System.Collections.Generic.HashSet<Health>();
            for(int i=0;i<3;i++){brain.CycleTarget(1);Assert.Contains(actor.target,collar.Links.ToArray());found.Add(actor.target);}
            Assert.AreEqual(3,found.Count,"cycling cannot reach each independent link");
            var target=actor.target;int index=System.Array.IndexOf(collar.Links.ToArray(),target);Cut(index);
            brain.CycleTarget(1);Assert.AreNotSame(target,actor.target);Assert.AreNotSame(enemy.Health,actor.target);
            for(int i=0;i<3;i++)if(collar.Links[i].Alive)Cut(i);
            Assert.AreSame(enemy.Health,brain.FindTarget());yield return null;
        }
        [UnityTest] public IEnumerator ARealProjectileCanSeverAMovingLinkWithoutResolvingTheAnimal()
        {
            var link=enemy.GetComponent<CantorCollar>().Links[1];
            var position=link.GetComponent<Collider>().bounds.center;
            Projectile.Fire(position+Vector3.right*5,Vector3.left,CutPacket(link));
            float deadline=Time.unscaledTime+1;
            while(link.Alive&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(link.Alive,"projectile cannot reach the actual moving link surface");
            Assert.IsTrue(enemy.Health.Alive);Assert.IsFalse(enemy.Health.Targetable);
        }
        [UnityTest] public IEnumerator LinkBreakCancelsTheCarriersActualSpecialTell()
        {
            var actor=PartyController.Current.Active;Place(actor,new Vector3(30,1,12));actor.Health.InvulnerableUntil=Time.time+30;
            enemy.Passive=false;enemy.GetComponent<CharacterController>().enabled=false;
            var boss=enemy.GetComponent<BossController>();float deadline=Time.unscaledTime+8;
            while(!boss.Telegraphing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(boss.Telegraphing);
            var link=enemy.GetComponent<CantorCollar>().Links[2];
            link.GetComponent<Hurtbox>().Hit(new DamagePacket{source=actor.Health,amount=1,breakPower=10000,type=DamageType.Pulse});
            Assert.IsTrue(enemy.Health.Broken);Assert.IsTrue(link.Broken);
            Assert.AreEqual(enemy.Health.BreakMeter,link.BreakMeter);
            yield return null;Assert.IsFalse(boss.Busy);Assert.IsFalse(boss.Telegraphing);
        }
        [UnityTest] public IEnumerator SeveredPlatesPauseAndRecoveryRestoresAllLinks()
        {
            var collar=enemy.GetComponent<CantorCollar>();Cut(0);yield return new WaitForSecondsRealtime(.2f);
            var plates=collar.Presentation(0).GetComponentsInChildren<Renderer>();Assert.AreEqual(8,plates.Length);
            // Interrupt a turn before the next chain evaluation, so a stale
            // tangent cannot hide behind an already stationary parent.
            enemy.transform.rotation=Quaternion.Euler(0,35,0);
            GameTime.Paused=true;yield return null;var held=plates.Select(p=>p.transform.position).ToArray();
            var chain=enemy.GetComponent<SerpentSegments>().Parts;var heldChain=chain.Select(p=>p.rotation).ToArray();
            yield return new WaitForSecondsRealtime(.3f);
            for(int i=0;i<plates.Length;i++)Assert.Less(Vector3.Distance(held[i],plates[i].transform.position),.0001f);
            for(int i=0;i<chain.Count;i++)Assert.Less(Quaternion.Angle(heldChain[i],chain[i].rotation),.001f,"paused body tangent changes beneath attached equipment");
            GameTime.Paused=false;Cut(1);Cut(2);enemy.GetComponent<Hurtbox>().Hit(CutPacket(enemy.Health));
            yield return new WaitForSecondsRealtime(.2f);enemy.Health.Heal(enemy.Health.maximum);
            Assert.AreEqual(3,collar.Remaining);Assert.IsFalse(enemy.Health.Targetable);
            for(int i=0;i<collar.Links.Count;i++)
            {
                var link=collar.Links[i];
                Assert.AreEqual(link.maximum,link.integrity);Assert.IsTrue(link.GetComponent<Collider>().enabled);
                foreach(var plate in collar.Presentation(i).GetComponentsInChildren<Renderer>(true))
                {Assert.IsTrue(plate.gameObject.activeSelf);Assert.Greater(plate.transform.localScale.sqrMagnitude,.8f);}
            }
        }
        [UnityTest] public IEnumerator TargetingAnyLinkKeepsTheWholeAnimalInView()
        {
            var actor=PartyController.Current.Active;Place(actor,new Vector3(30,1,-17));
            var collar=enemy.GetComponent<CantorCollar>();
            foreach(var link in collar.Links)
            {
                actor.target=link;actor.TargetLocked=true;yield return new WaitForSecondsRealtime(1.3f);
                FlightFramingTests.CheckMeshes(enemy.gameObject,"whole Cantor while targeting "+link.id);
                FlightFramingTests.CheckMeshes(actor.GetComponent<FormController>().flight,"hero targeting a collar link");
                for(int i=0;i<4;i++)FlightFramingTests.CheckMeshes(collar.Presentation(i).gameObject,"attached collar equipment "+i);
            }
        }
        [UnityTest] public IEnumerator RemovingTheCarrierCleansAllOwnedEquipment()
        {
            var collar=enemy.GetComponent<CantorCollar>();var roots=Enumerable.Range(0,4).Select(collar.Presentation).ToArray();
            Assert.AreEqual(4,roots.Length);Assert.IsTrue(roots.All(r=>r!=null));
            Object.Destroy(enemy.gameObject);yield return null;yield return null;
            Assert.IsTrue(roots.All(r=>r==null),"discarded equipment survives its owning encounter");
        }
        [UnityTest] public IEnumerator DelayedRecoveryKeepsTheRestoredLinksHittable()
        {
            var collar=enemy.GetComponent<CantorCollar>();for(int i=0;i<3;i++)Cut(i);
            enemy.GetComponent<Hurtbox>().Hit(CutPacket(enemy.Health));yield return new WaitForSecondsRealtime(.2f);
            enemy.Health.Heal(enemy.Health.maximum);yield return new WaitForSecondsRealtime(CombatActor.ReviveDuration+.2f);
            foreach(var link in collar.Links)Assert.IsTrue(link.GetComponent<Collider>().enabled,"late presentation restore disabled a fresh link");
            var target=collar.Links[1];var position=target.GetComponent<Collider>().bounds.center;
            Projectile.Fire(position+Vector3.right*5,Vector3.left,CutPacket(target));
            float deadline=Time.unscaledTime+1;while(target.Alive&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(target.Alive,"a restored link cannot receive an ordinary projectile");
            Assert.IsTrue(enemy.Health.Alive);Assert.IsFalse(enemy.Health.Targetable);
        }
        [UnityTest] public IEnumerator DelayedRecoveryRestartsTheCarrierAndItsRealAttack()
        {
            var actor=PartyController.Current.Active;Place(actor,new Vector3(30,1,10));actor.Health.InvulnerableUntil=Time.time+30;
            for(int i=0;i<3;i++)Cut(i);enemy.GetComponent<Hurtbox>().Hit(CutPacket(enemy.Health));
            yield return new WaitForSecondsRealtime(.2f);enemy.Health.Heal(enemy.Health.maximum);
            yield return new WaitForSecondsRealtime(CombatActor.ReviveDuration+.2f);
            Assert.IsTrue(enemy.enabled,"carrier brain remains shut down after recovery");
            Assert.IsTrue(enemy.GetComponent<SerpentSegments>().enabled,"restored body remains frozen");
            var boss=enemy.GetComponent<BossController>();Assert.IsTrue(boss.enabled);Assert.AreEqual(1,boss.Phase);
            var parts=enemy.GetComponent<SerpentSegments>().Parts;var restored=parts.Select(p=>p.localPosition).ToArray();
            enemy.transform.rotation=Quaternion.Euler(0,35,0);yield return new WaitForSecondsRealtime(.4f);
            Assert.Greater(parts.Select((p,i)=>Vector3.Distance(p.localPosition,restored[i])).Max(),.02f,"restored anatomy never resumes after its carrier turns");
            enemy.Passive=false;enemy.GetComponent<CharacterController>().enabled=false;
            float deadline=Time.unscaledTime+8;
            while(!boss.Telegraphing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(boss.Telegraphing,"recovered carrier never starts its actual marked special");
        }
        [UnityTest] public IEnumerator DeactivatingTheCarrierHidesItsOwnedEquipment()
        {
            var collar=enemy.GetComponent<CantorCollar>();var roots=Enumerable.Range(0,4).Select(collar.Presentation).ToArray();
            enemy.gameObject.SetActive(false);yield return null;
            Assert.IsFalse(roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).Any(r=>r.enabled&&r.gameObject.activeInHierarchy),"inactive carrier leaves separate collar equipment visible");
            enemy.gameObject.SetActive(true);yield return null;
            Assert.IsTrue(roots.All(r=>r.gameObject.activeInHierarchy));
            Assert.AreEqual(3,collar.Remaining);Assert.IsFalse(enemy.Health.Targetable);
            Cut(0);yield return new WaitForSecondsRealtime(.2f);enemy.gameObject.SetActive(false);yield return null;
            Assert.IsFalse(roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).Any(r=>r.enabled&&r.gameObject.activeInHierarchy));
            enemy.gameObject.SetActive(true);yield return new WaitForSecondsRealtime(1.4f);
            Assert.AreEqual(2,collar.Remaining,"reactivation resets earned link damage");
            Assert.IsFalse(roots[0].GetComponentsInChildren<Renderer>(true).Any(r=>r.gameObject.activeInHierarchy),"spent plate lifetime restarts on reactivation");
            Assert.IsTrue(roots.Skip(1).All(r=>r.GetComponentsInChildren<Renderer>().Length==8));
        }
        IEnumerator ReplacementEnemy()
        {
            Object.Destroy(enemy.gameObject);yield return null;
            enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(30,1,0));enemy.Passive=true;
            yield return null;yield return null;
        }
        IEnumerator WaitForSpecial(bool song)
        {
            var boss=enemy.GetComponent<BossController>();float deadline=Time.unscaledTime+15;
            do
            {
                while(!boss.Telegraphing&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(boss.Telegraphing,"boss never supplied the required real tell");
                bool observedSong=boss.TelegraphRemaining>.9f;
                if(observedSong==song)yield break;
                while(boss.Busy&&Time.unscaledTime<deadline)yield return null;
            }while(Time.unscaledTime<deadline);
            Assert.Fail("required attack type never arrived");
        }
        void QuietShots()
        {foreach(var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))Object.Destroy(shot.gameObject);}
        [UnityTest] public IEnumerator CuttingTheSweepLinkRemovesTheOuterSweepHit()
        {
            var actor=PartyController.Current.Active;
            foreach(bool cut in new[]{false,true})
            {
                Place(actor,new Vector3(30,1,6));actor.Health.maximum=actor.Health.integrity=10000;actor.Health.InvulnerableUntil=Time.time+30;
                if(cut)Cut(0);enemy.Passive=false;enemy.GetComponent<CharacterController>().enabled=false;
                yield return WaitForSpecial(false);QuietShots();actor.Health.InvulnerableUntil=0;float before=actor.Health.integrity;
                var boss=enemy.GetComponent<BossController>();while(boss.Busy)yield return null;
                float damage=before-actor.Health.integrity;Debug.Log($"COLLAR_SWEEP cut={cut} damageAtSixMetres={damage:R}");
                if(cut)Assert.AreEqual(0,damage,"cut sweep still reaches its old outer ring");else Assert.Greater(damage,0,"attached sweep never hits its marked ring");
                if(!cut)yield return ReplacementEnemy();
            }
        }
        [UnityTest] public IEnumerator CuttingTheChorusLinkRemovesTheSongStagger()
        {
            var actor=PartyController.Current.Active;
            foreach(bool cut in new[]{false,true})
            {
                Place(actor,new Vector3(30,1,6));actor.Health.maximum=actor.Health.integrity=10000;actor.Health.InvulnerableUntil=Time.time+40;
                if(cut)Cut(1);enemy.Health.integrity=enemy.Health.maximum*.25f;
                enemy.Passive=false;enemy.GetComponent<CharacterController>().enabled=false;
                yield return WaitForSpecial(true);QuietShots();actor.Health.InvulnerableUntil=0;float before=actor.Health.integrity;
                var boss=enemy.GetComponent<BossController>();while(boss.Busy)yield return null;
                Assert.Less(actor.Health.integrity,before,"both versions must release a real song hit");
                Debug.Log($"COLLAR_SONG cut={cut} actorState={actor.State}");
                Assert.AreEqual(!cut,actor.State==ActorState.Stagger,"chorus link must control the song stagger");
                if(!cut){yield return new WaitForSecondsRealtime(.8f);yield return ReplacementEnemy();}
            }
        }
        [UnityTest] public IEnumerator CuttingTheVolleyLinkChangesTheEmittedFanFromThreeToOne()
        {
            var actor=PartyController.Current.Active;
            foreach(bool cut in new[]{false,true})
            {
                Place(actor,new Vector3(30,1,10));actor.Health.InvulnerableUntil=Time.time+30;QuietShots();yield return null;
                if(cut)Cut(2);enemy.Passive=false;enemy.GetComponent<CharacterController>().enabled=false;
                float deadline=Time.unscaledTime+4;
                while(!enemy.Telegraphing&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(enemy.Telegraphing);
                while(enemy.Telegraphing&&Time.unscaledTime<deadline)yield return null;
                int shots=Projectile.Live.Count(p=>p.Threatens(true));
                Debug.Log($"COLLAR_VOLLEY cut={cut} shots={shots}");Assert.AreEqual(cut?1:3,shots);
                if(!cut)yield return ReplacementEnemy();
            }
        }
    }
}
