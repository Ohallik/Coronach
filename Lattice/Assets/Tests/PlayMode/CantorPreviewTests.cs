using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class CantorPreviewTests
    {
        EncounterVolume encounter;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return LoadGullet();
        }
        IEnumerator LoadGullet()
        {
            SceneFlow.Current.LoadZone("Gullet_Tunnel");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            encounter=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Gullet_Cantor");
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();}
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        EnemyBrain Preview()
        {
            var animals=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition.id=="Cantor").ToArray();
            Assert.AreEqual(1,animals.Length,"the actual animal must exist before its encounter begins");return animals[0];
        }
        static void Place(CombatActor actor,Vector3 point)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.SetPositionAndRotation(point,Quaternion.identity);cc.enabled=true;
            actor.GetComponent<FlightMotor>().Halt();actor.target=null;actor.TargetLocked=false;
        }
        [UnityTest] public IEnumerator ActualAnimalAndLinksAreVisibleButCannotBeTargetedOrDamaged()
        {
            Assert.IsFalse(encounter.Started);var enemy=Preview();var collar=enemy.GetComponent<CantorCollar>();
            Assert.AreEqual(3,collar.Links.Count);Assert.Greater(enemy.GetComponentsInChildren<Renderer>().Count(r=>r.enabled),7);
            for(int i=0;i<4;i++)Assert.IsTrue(collar.Presentation(i).GetComponentsInChildren<Renderer>().All(r=>r.enabled&&r.gameObject.activeInHierarchy));
            foreach(var health in enemy.GetComponentsInChildren<Health>())
            {
                Assert.IsFalse(health.Targetable,"preview leaks into normal target selection");float before=health.integrity;
                health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=100000,type=DamageType.Pulse});
                Assert.AreEqual(before,health.integrity,"preview damage changes the encounter");
            }
            Assert.AreEqual(3,collar.Remaining);Assert.IsFalse(GameServices.Current.Flags.GetBool("bossdown.Cantor"));yield return null;
        }
        [UnityTest] public IEnumerator DormantAnimalWaitsWithoutAttacksTranslationOrBossMusic()
        {
            var enemy=Preview();Place(PartyController.Current.Active,new Vector3(18,1,770));
            var position=enemy.transform.position;var rotation=enemy.transform.rotation;int shots=Projectile.ActiveCount;
            yield return new WaitForSecondsRealtime(7);
            Assert.IsFalse(encounter.Started);Assert.AreEqual(position,enemy.transform.position);Assert.Less(Quaternion.Angle(rotation,enemy.transform.rotation),.001f);
            Assert.IsFalse(enemy.Telegraphing);Assert.IsFalse(enemy.Attacking);Assert.IsFalse(enemy.GetComponent<BossController>().Busy);
            Assert.AreEqual(shots,Projectile.ActiveCount);Assert.AreEqual("Starfight",MusicDirector.Current.Track,"preview starts the combat cue");
        }
        [UnityTest] public IEnumerator DormancyAlsoSuppressesNearbyTellsShotsAndDamage()
        {
            var enemy=Preview();encounter.GetComponent<Collider>().enabled=false;
            var party=PartyController.Current;Place(party.Active,enemy.transform.position+Vector3.back*8);
            Place(party.members.First(h=>h!=party.Active),enemy.transform.position+Vector3.right*9);
            float integrity=party.Active.Health.integrity;int shots=Projectile.ActiveCount;var origin=enemy.transform.position;
            for(float began=Time.unscaledTime;Time.unscaledTime-began<7;)
            {
                yield return null;Assert.IsFalse(enemy.Telegraphing);Assert.IsFalse(enemy.Attacking);Assert.IsFalse(enemy.GetComponent<BossController>().Busy);
                Assert.AreEqual(shots,Projectile.ActiveCount);Assert.AreEqual(integrity,party.Active.Health.integrity);Assert.AreEqual(origin,enemy.transform.position);
            }
            Assert.IsFalse(encounter.Started);Assert.AreEqual("Starfight",MusicDirector.Current.Track);
        }
        [UnityTest] public IEnumerator ActivationUsesThePreviewAndProtectsTheLockUntilAllLinksAreCut()
        {
            var enemy=Preview();var collar=enemy.GetComponent<CantorCollar>();var links=collar.Links.ToArray();
            encounter.Begin();encounter.Begin();yield return null;yield return null;
            Assert.AreSame(enemy,Preview());Assert.AreEqual(1,encounter.Remaining);Assert.IsFalse(enemy.Health.Targetable);
            Assert.IsTrue(enemy.enabled);Assert.IsTrue(enemy.GetComponent<BossController>().enabled);Assert.AreEqual("Alien Boss Battle",MusicDirector.Current.Track);
            int rewards=GameServices.Current.State.scrip,deaths=0;enemy.Health.Died+=(_,__)=>deaths++;
            foreach(var link in links)
            {
                Assert.IsTrue(link.Targetable);Assert.Contains(link,collar.Links.ToArray());
                link.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=100000,type=DamageType.Pulse});
                Assert.IsTrue(enemy.Health.Alive);Assert.AreEqual(rewards,GameServices.Current.State.scrip);
            }
            Assert.IsTrue(enemy.Health.Targetable);
            var packet=new DamagePacket{source=PartyController.Current.Active.Health,amount=100000,type=DamageType.Pulse};
            enemy.Health.Receive(packet);enemy.Health.Receive(packet);encounter.Begin();
            Assert.AreEqual(1,deaths);Assert.AreEqual(rewards+150,GameServices.Current.State.scrip);Assert.AreEqual(0,encounter.Remaining);Assert.IsTrue(encounter.Cleared);
            Assert.IsTrue(GameServices.Current.Flags.GetBool("bossdown.Cantor"));Assert.IsTrue(GameServices.Current.Flags.GetBool("clear.Gullet_Cantor"));yield return null;
        }
        [UnityTest] public IEnumerator BothCompletedSaveIdentitiesAvoidPrewarmingOrAwardingAnotherAnimal()
        {
            foreach(string flag in new[]{"clear.Gullet_Cantor","bossdown.Cantor"})
            {
                GameServices.Current.Flags.SetBool("clear.Gullet_Cantor",false);GameServices.Current.Flags.SetBool("bossdown.Cantor",false);
                GameServices.Current.Flags.SetBool(flag,true);int scrip=GameServices.Current.State.scrip;
                yield return LoadGullet();encounter.Begin();yield return null;
                Assert.IsFalse(Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Any(e=>e.definition.id=="Cantor"));
                Assert.IsTrue(encounter.Cleared);Assert.AreEqual(scrip,GameServices.Current.State.scrip);Assert.AreEqual("Starfight",MusicDirector.Current.Track);
            }
        }
        [UnityTest] public IEnumerator ANewClearFlagCancelsTheWaitingAnimalWithoutRewards()
        {
            var enemy=Preview();int scrip=GameServices.Current.State.scrip;
            GameServices.Current.Flags.SetBool("clear.Gullet_Cantor",true);yield return null;yield return null;
            Assert.IsTrue(enemy==null);Assert.IsTrue(encounter.Cleared);encounter.Begin();
            Assert.IsNull(DormantEnemy.Interest(encounter.previewPoint.position));Assert.AreEqual(scrip,GameServices.Current.State.scrip);
            Assert.IsFalse(GameServices.Current.Flags.GetBool("bossdown.Cantor"),"cancelling a preview invented a new victory");
        }
        [UnityTest] public IEnumerator DisablingAndReenablingPreviewDoesNotCancelAnActivatedEncounter()
        {
            var enemy=Preview();encounter.enabled=false;yield return null;yield return null;
            Assert.IsTrue(enemy==null);Assert.IsNull(DormantEnemy.Interest(encounter.previewPoint.position));
            encounter.enabled=true;yield return null;yield return null;enemy=Preview();
            Assert.IsFalse(encounter.Started);encounter.Begin();yield return null;
            encounter.enabled=false;yield return null;yield return null;
            Assert.IsTrue(enemy!=null&&enemy.Health.Alive);Assert.AreEqual(1,encounter.Remaining,"preview cleanup cancelled live combat");
        }
        [UnityTest] public IEnumerator RemovingTheSpawnerDisposesOnlyItsUnconsumedPreview()
        {
            var enemy=Preview();var point=encounter.previewPoint.position;
            Object.Destroy(encounter.spawners[0].gameObject);yield return null;yield return null;
            Assert.IsTrue(enemy==null);Assert.IsNull(DormantEnemy.Interest(point));Assert.AreEqual("Starfight",MusicDirector.Current.Track);
            yield return LoadGullet();enemy=Preview();encounter.Begin();yield return null;
            Object.Destroy(encounter.spawners[0].gameObject);yield return null;yield return null;
            Assert.IsTrue(enemy!=null&&enemy.Health.Alive,"spawner removal must not delete an already handed-off encounter");
        }
        [UnityTest] public IEnumerator PreviewPocketAndItsApproachClearRealTissueAndTheEncounterTrigger()
        {
            Assert.IsNotNull(encounter.previewPoint);var point=encounter.previewPoint.position;
            var trigger=encounter.GetComponent<Collider>();
            Assert.Greater(Vector3.Distance(trigger.ClosestPoint(point),point),encounter.previewRadius+HeroCollision.HullRadius("Taren")+.15f,
                "the preview's safe area overlaps automatic encounter activation");
            foreach(string hero in new[]{"Taren","Sela"})
            {
                float radius=HeroCollision.HullRadius(hero)+.3f;
                foreach(var from in new[]{new Vector3(0,1,744),new Vector3(0,1,768)})
                {
                    var delta=point-from;
                    for(float t=0;t<=1;t+=.025f)
                    {
                        var center=Vector3.Lerp(from,point,t)+Vector3.up*.8f;
                        Assert.IsFalse(Physics.OverlapSphere(center,radius,~0,QueryTriggerInteraction.Ignore).Any(CombatCover.Opaque),
                            hero+" cannot reach or turn in the actual viewing pocket at "+center);
                    }
                }
            }
            Assert.GreaterOrEqual(encounter.GetComponent<BoxCollider>().size.x,60,"preview must not shrink the established coil");yield return null;
        }
        static void InFrame(Renderer renderer)
        {
            var b=renderer.bounds;
            for(int i=0;i<8;i++)
            {
                var p=Camera.main.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                Assert.Greater(p.z,0);Assert.That(p.x,Is.InRange(.025f,.975f),renderer.name+" horizontal crop");
                Assert.That(p.y,Is.InRange(.025f,.975f),renderer.name+" vertical crop");
            }
        }
        [UnityTest] public IEnumerator PocketCameraFitsTheRealAnimalAndShipsThenYieldsToActiveTargetsAndDeparture()
        {
            var enemy=Preview();var point=encounter.previewPoint.position;var party=PartyController.Current;
            Place(party.Active,point);Place(party.members.First(h=>h!=party.Active),point+new Vector3(-4,0,-3));
            yield return new WaitForSecondsRealtime(2);
            Assert.IsFalse(encounter.Started);Assert.AreSame(enemy.Health,DormantEnemy.Interest(point));
            foreach(var r in enemy.GetComponentsInChildren<Renderer>().Where(r=>r.enabled))InFrame(r);
            foreach(var hero in party.members)foreach(var r in hero.GetComponent<FormController>().flight.GetComponentsInChildren<Renderer>().Where(r=>r.enabled))InFrame(r);
            float previewDistance=Vector3.Distance(Camera.main.transform.position,party.Active.transform.position);
            var target=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),point+Vector3.back*9);target.Passive=true;
            party.Active.target=target.Health;party.Active.TargetLocked=true;yield return new WaitForSecondsRealtime(2);
            Assert.Less(Vector3.Distance(Camera.main.transform.position,party.Active.transform.position),previewDistance-5,"dormant interest overrides live combat");
            party.Active.target=null;Object.Destroy(target.gameObject);Place(party.Active,point+Vector3.back*15);
            yield return new WaitForSecondsRealtime(3);
            Assert.IsNull(DormantEnemy.Interest(party.Active.transform.position));
            Assert.Less(Vector3.Distance(Camera.main.transform.position,party.Active.transform.position),previewDistance-5,"leaving the pocket retains remote preview framing");
        }
        [UnityTest] public IEnumerator VisiblePocketFoldsAreRootedAndLeaveTheWholeViewingAreaClear()
        {
            var point=encounter.previewPoint.position;
            var folds=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Rooted preview lip").ToArray();Assert.AreEqual(2,folds.Length);
            var shell=GameObject.Find("Gullet membrane shell").GetComponent<MeshCollider>();
            foreach(var fold in folds)
            {
                var b=fold.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,c)=>{a.Encapsulate(c);return a;});
                Assert.LessOrEqual(b.max.y,4.01f,"preview breaks the map's cutaway height");
                Assert.Greater(b.min.x,point.x+encounter.previewRadius+HeroCollision.HullRadius("Taren")+.3f,"fold enters the viewing area");
                Assert.GreaterOrEqual(b.max.x,GulletProfile.RightEdge(fold.position.z)-.2f,"fold does not join the wall");
                Assert.IsTrue(shell.Raycast(new Ray(new Vector3(fold.position.x,8,fold.position.z),Vector3.down),out var bed,30));
                Assert.LessOrEqual(b.min.y,bed.point.y+.15f,"preview fold floats above its tissue bed");
            }
            for(int i=0;i<24;i++)
            {
                float angle=i*Mathf.PI/12;var p=point+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*encounter.previewRadius+Vector3.up*.8f;
                Assert.IsFalse(Physics.OverlapSphere(p,HeroCollision.HullRadius("Taren")+.3f,~0,QueryTriggerInteraction.Ignore).Any(CombatCover.Opaque));
            }
            Place(PartyController.Current.Active,point);yield return new WaitForSecondsRealtime(2);
            var enemy=Preview();var targets=enemy.GetComponent<CantorCollar>().Links.Select(h=>h.transform.position).Append(enemy.transform.position+Vector3.up*1.5f);
            foreach(var target in targets)
            {
                var from=Camera.main.transform.position;var d=target-from;
                Assert.IsFalse(Physics.RaycastAll(from,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>CombatCover.Opaque(h.collider)),"actual collar is hidden behind preview scenery");
            }
        }
    }
}
