using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Lattice.Tests.PlayMode
{
    public sealed class ProjectileProvenanceTests
    {
        // Test-owned wire DTO compiles against the old recorder too. Missing
        // identities must fail assertions, not prevent the original from building.
        [Serializable] sealed class Hit
        {
            public bool projectile,provenanceAvailable,sourceAvailable,sourceAlive,deathAttack;
            public string releaseCapture,releaseSource,source;
            public long volleyId,shotId;
            public int sourceInstanceId,impactSourceInstanceId,projectileInstanceId;
            public double releasedAt,seconds;
            public Vector3 releasePosition,releaseVelocity;
        }
        [Serializable] sealed class Report
        {
            public int version,dropped;
            public bool complete;
            public string captureId;
            public long projectileLaunches,projectileVolleys;
            public Hit[] hits;
        }
        string folder;
        double began;
        Health victim;
        int received;
        [UnitySetUp] public IEnumerator Boot()
        {
            received=0;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+12;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members)
            {
                hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;
                var motor=hero.GetComponent<FlightMotor>();motor.Halt();
                var controller=hero.GetComponent<CharacterController>();controller.enabled=false;
                hero.transform.position=new Vector3(hero==PartyController.Current.Active?200:220,1,-4);controller.enabled=true;
            }
            var body=new GameObject("Provenance witness");body.transform.position=new Vector3(200,2,-1);
            victim=body.AddComponent<Health>();victim.id="Provenance witness";victim.friendly=true;victim.maximum=victim.integrity=10000;
            body.AddComponent<SphereCollider>().radius=1.5f;body.AddComponent<Hurtbox>();victim.Damaged+=Count;
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C4/damage-provenance/test-"+Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(folder);began=Time.realtimeSinceStartupAsDouble;Physics.SyncTransforms();yield return null;
        }
        void Count(Health _,DamagePacket packet,float amount){received++;}
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        QualityDamageCapture Capture(string name="one")
        {
            string destination=Path.Combine(folder,name);Directory.CreateDirectory(destination);
            var trace=new QualityDamageCapture(destination,()=>Time.realtimeSinceStartupAsDouble-began,()=>3);trace.Watch(victim);return trace;
        }
        Report Read(string name="one")=>JsonUtility.FromJson<Report>(File.ReadAllText(Path.Combine(folder,name,"damage.json")));
        EnemyBrain Source(float x=200)
        {var source=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(x,1,4));source.Passive=true;return source;}
        Projectile Fire(Health source,float speed=20)
        {
            var before=Projectile.Live.ToArray();Projectile.Fire(new Vector3(200,2,3),Vector3.back,new DamagePacket{source=source,amount=10,type=DamageType.Beam},speed);
            return Projectile.Live.Except(before).Single();
        }
        IEnumerator Impacts(int count)
        {float until=Time.unscaledTime+4;while(received<count&&Time.unscaledTime<until)yield return null;Assert.AreEqual(count,received,"real projectile did not reach the witness");}
        static void Identified(Report report,Hit hit)
        {
            Assert.AreEqual(2,report.version);Assert.IsTrue(report.complete);Assert.AreEqual(0,report.dropped);
            Assert.IsTrue(hit.projectile);Assert.IsTrue(hit.provenanceAvailable);Assert.IsNotEmpty(report.captureId);
            Assert.AreEqual(report.captureId,hit.releaseCapture);Assert.Greater(hit.shotId,0);Assert.Greater(hit.volleyId,0);
            Assert.AreNotEqual(0,hit.sourceInstanceId);Assert.AreNotEqual(0,hit.projectileInstanceId);
            Assert.AreEqual("ChoristerDrifter",hit.releaseSource);Assert.That(hit.releasedAt,Is.InRange(0,hit.seconds));
            Assert.Greater(hit.releaseVelocity.magnitude,0);
        }
        [UnityTest] public IEnumerator RealDrifterFanSharesOneVolleyButThreeLaunches()
        {
            var source=Source();yield return null;yield return null;
            using(var trace=Capture())
            {
                source.Passive=false;yield return Impacts(3);source.Passive=true;trace.Finish();
                var report=Read();Assert.AreEqual(3,report.hits.Length);
                foreach(var hit in report.hits){Identified(report,hit);Assert.AreEqual(source.Health.GetInstanceID(),hit.sourceInstanceId);}
                Assert.AreEqual(1,report.hits.Select(h=>h.volleyId).Distinct().Count());
                Assert.AreEqual(3,report.hits.Select(h=>h.shotId).Distinct().Count());
                Assert.AreEqual(3,report.projectileLaunches);Assert.AreEqual(1,report.projectileVolleys);
                Assert.AreEqual(3,report.hits.Select(h=>h.releaseVelocity).Distinct().Count());
            }
        }
        [UnityTest] public IEnumerator SameDefinitionSourcesKeepDistinctActorAndVolleyIdentity()
        {
            var a=Source();var b=Source(205);yield return null;
            using(var trace=Capture())
            {
                Fire(a.Health);yield return Impacts(1);Fire(b.Health);yield return Impacts(2);trace.Finish();var report=Read();
                foreach(var hit in report.hits)Identified(report,hit);
                CollectionAssert.AreEqual(new[]{a.Health.GetInstanceID(),b.Health.GetInstanceID()},report.hits.Select(h=>h.sourceInstanceId).ToArray());
                Assert.AreNotEqual(report.hits[0].volleyId,report.hits[1].volleyId);Assert.AreNotEqual(report.hits[0].shotId,report.hits[1].shotId);
            }
        }
        [UnityTest] public IEnumerator RecycledProjectileGetsAFreshLaunchAndOriginalReleaseGeometry()
        {
            var source=Source();yield return null;
            using(var trace=Capture())
            {
                int instance=Fire(source.Health).GetInstanceID();yield return Impacts(1);
                Assert.AreEqual(instance,Fire(source.Health).GetInstanceID(),"fixture did not actually reuse the pooled projectile");yield return Impacts(2);
                trace.Finish();var report=Read();foreach(var hit in report.hits)Identified(report,hit);
                Assert.AreEqual(report.hits[0].projectileInstanceId,report.hits[1].projectileInstanceId);
                Assert.AreNotEqual(report.hits[0].shotId,report.hits[1].shotId);
                Assert.AreEqual(new Vector3(200,2,3),report.hits[0].releasePosition);Assert.AreEqual(Vector3.back*20,report.hits[0].releaseVelocity);
            }
        }
        IEnumerator SourceEnds(bool destroy)
        {
            var source=Source();yield return null;yield return null;int instance=source.Health.GetInstanceID();
            using(var trace=Capture())
            {
                GameTime.Paused=true;Fire(source.Health);
                source.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=100000,type=DamageType.Pulse});
                Assert.IsFalse(source.Health.Alive);if(destroy)Object.Destroy(source.gameObject);yield return null;yield return null;
                GameTime.Paused=false;yield return Impacts(1);trace.Finish();var report=Read();var hit=report.hits.Single();Identified(report,hit);
                Assert.AreEqual(instance,hit.sourceInstanceId);Assert.AreEqual(!destroy,hit.sourceAvailable);Assert.IsFalse(hit.sourceAlive);Assert.IsFalse(hit.deathAttack);
                Assert.AreEqual(destroy?0:instance,hit.impactSourceInstanceId);
            }
        }
        [UnityTest] public IEnumerator ReleasedShotKeepsTheDeadSourceIdentity()=>SourceEnds(false);
        [UnityTest] public IEnumerator ReleasedShotKeepsTheDestroyedSourceIdentity()=>SourceEnds(true);
        [UnityTest] public IEnumerator ShotReleasedBeforeCaptureIsExplicitlyUnattributed()
        {
            var source=Source();yield return null;GameTime.Paused=true;Fire(source.Health);
            using(var trace=Capture())
            {
                GameTime.Paused=false;yield return Impacts(1);trace.Finish();var report=Read();var hit=report.hits.Single();
                Assert.AreEqual(2,report.version);Assert.IsTrue(report.complete);Assert.IsTrue(hit.projectile);Assert.IsFalse(hit.provenanceAvailable);
                Assert.AreEqual(0,hit.shotId);Assert.AreEqual(0,report.projectileLaunches);
            }
        }
        [UnityTest] public IEnumerator CaptureOwnershipAndDisposalCannotClobberAnotherSession()
        {
            var source=Source();yield return null;
            using(var first=Capture())
            {
                QualityDamageCapture duplicate=null;
                try{Assert.Throws<InvalidOperationException>(()=>duplicate=Capture("duplicate"));}
                finally{duplicate?.Dispose();}
                Fire(source.Health);yield return Impacts(1);first.Finish();
                var owner=(IDisposable)typeof(QualityDamageCapture).GetField("provenance",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(first);
                using(var second=Capture("two"))
                {
                    first.Dispose();owner.Dispose();Fire(source.Health);yield return Impacts(2);second.Finish();
                    var a=Read();var b=Read("two");Identified(a,a.hits.Single());Identified(b,b.hits.Single());
                    Assert.AreNotEqual(a.captureId,b.captureId);Assert.AreEqual(1,b.hits[0].shotId);Assert.AreEqual(1,b.projectileLaunches);
                }
            }
        }
        [UnityTest] public IEnumerator FailedReportWriteStillReleasesTheCaptureOwner()
        {
            string file=Path.Combine(folder,"not-a-directory");File.WriteAllText(file,"preserve this conflicting file");
            var broken=new QualityDamageCapture(file,()=>0,()=>0);
            Assert.Throws<DirectoryNotFoundException>(()=>broken.Finish());broken.Dispose();
            using(var next=Capture("recovered")){next.Finish();}
            Assert.IsTrue(Read("recovered").complete);Assert.AreEqual("preserve this conflicting file",File.ReadAllText(file));
            yield return null;
        }
    }
}
