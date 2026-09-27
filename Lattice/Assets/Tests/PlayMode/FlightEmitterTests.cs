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
    [DefaultExecutionOrder(1000)]
    public sealed class FlightOriginObserver:MonoBehaviour
    {
        public Transform hull;
        public Vector3 nose;
        public readonly List<float> errors=new();
        readonly HashSet<Projectile> seen=new();
        public void ResetSample(){seen.Clear();errors.Clear();}
        void LateUpdate()
        {
            if(hull==null)return;
            foreach(var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                if(Vector3.Distance(p.transform.position,transform.position)<2.5f&&seen.Add(p))
                    errors.Add(Vector3.Distance(p.transform.position,hull.TransformPoint(nose)));
        }
    }
    public sealed class FlightEmitterTests
    {
        CombatActor taren,sela;GameObject fixture;EnemyBrain victim;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;if(hero.character=="Taren")taren=hero;else sela=hero;}
            yield return new WaitForSecondsRealtime(.7f);
            Place(taren,new Vector3(200,1,0));Place(sela,new Vector3(210,1,0));
            fixture=new GameObject("Flight emitter fixture");
            victim=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDart"),new Vector3(200,1,8));
            victim.Passive=true;victim.Health.maximum=victim.Health.integrity=1000;victim.transform.SetParent(fixture.transform);
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();if(fixture!=null)Object.Destroy(fixture);SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor hero,Vector3 position,float yaw=0,float roll=0)
        {
            var cc=hero.GetComponent<CharacterController>();var motor=hero.GetComponent<FlightMotor>();cc.enabled=false;motor.Halt();
            var heading=Quaternion.Euler(0,yaw,0)*Vector3.forward;motor.Move(new Vector2(heading.x,heading.z),false,false);motor.Halt();
            hero.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));cc.enabled=true;hero.target=null;
            var hull=hero.GetComponent<FormController>().flight;hull.GetComponent<FlightShipMotion>().enabled=false;
            hull.transform.localRotation=Quaternion.Euler(-5,0,roll);Physics.SyncTransforms();
        }
        static FlightOriginObserver Observe(CombatActor actor)
        {
            var hull=actor.GetComponent<FormController>().flight.transform;
            // Independent measured tip region from FlightGeometryAudit, not a
            // runtime socket value. Shipping meshes do not retain CPU vertices.
            var nose=actor.character=="Taren"?new Vector3(0,.2f,1.59f):new Vector3(0,.1f,1.59f);
            var observer=actor.gameObject.AddComponent<FlightOriginObserver>();observer.hull=hull;observer.nose=nose;
            return observer;
        }
        [UnityTest] public IEnumerator BothShipsFireFromTheirRenderedNoseAcrossYawAndBank()
        {
            foreach(var hero in new[]{taren,sela})
            {
                Place(taren,new Vector3(210,1,0));Place(sela,new Vector3(220,1,0));
                var observer=Observe(hero);
                foreach(float yaw in new[]{0f,90f,180f})foreach(float roll in new[]{0f,28f,90f})
                {
                    Place(hero,new Vector3(200,1,0),yaw,roll);observer.ResetSample();Assert.IsTrue(hero.Attack());
                    yield return new WaitForSecondsRealtime(.23f);
                    Assert.AreEqual(1,observer.errors.Count,"fixture must observe the actual newly released projectile");
                    Assert.LessOrEqual(observer.errors[0],.22f,$"{hero.character} yaw={yaw} bank={roll} fire is detached from the rendered nose");
                }
                Object.Destroy(observer);yield return null;
            }
        }
        [UnityTest] public IEnumerator ScatterStartsAtTheSameHullEmitter()
        {
            Place(taren,new Vector3(210,1,0));Place(sela,new Vector3(200,1,0),90,28);
            var observer=Observe(sela);sela.charge=100;Assert.IsTrue(sela.Skill(1));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(5,observer.errors.Count);foreach(float error in observer.errors)Assert.LessOrEqual(error,.22f,"fan detached from banked hull");
        }
        [UnityTest] public IEnumerator NearCoverCannotBeSkippedByTheHullMuzzleOffset()
        {
            foreach(var hero in new[]{taren,sela})
            {
                Place(taren,new Vector3(210,1,0));Place(sela,new Vector3(220,1,0));Place(hero,new Vector3(200,1,0));hero.target=victim.Health;
                var wall=new GameObject("Thin near cover",typeof(BoxCollider));wall.transform.SetParent(fixture.transform);
                wall.transform.position=new Vector3(200,2,.3f);wall.GetComponent<BoxCollider>().size=new Vector3(8,6,.12f);
                victim.Health.ResetFull();Physics.SyncTransforms();Assert.IsTrue(hero.Attack());yield return new WaitForSecondsRealtime(.5f);
                Assert.AreEqual(1000,victim.Health.integrity,hero.character+" shot originated beyond near cover");
                wall.SetActive(false);Physics.SyncTransforms();Assert.IsTrue(hero.Attack());yield return new WaitForSecondsRealtime(.5f);
                Assert.Less(victim.Health.integrity,1000,"unobstructed shot must still hit");Object.Destroy(wall);
            }
        }
        GameObject Cover(float z,bool fixedCover)
        {
            var wall=new GameObject("Flight beam cover",typeof(BoxCollider));wall.transform.SetParent(fixture.transform);
            wall.transform.position=new Vector3(200,2,z);wall.GetComponent<BoxCollider>().size=new Vector3(8,6,.12f);
            wall.isStatic=fixedCover;Physics.SyncTransforms();return wall;
        }
        [UnityTest] public IEnumerator FlightLanceStopsAtCoverAndPiercesTheUncoveredTargetOnce()
        {
            Place(taren,new Vector3(210,1,0));Place(sela,new Vector3(200,1,0));sela.target=victim.Health;
            foreach(bool fixedCover in new[]{true,false})foreach(float z in new[]{.3f,4f})
            {
                var wall=Cover(z,fixedCover);victim.Health.ResetFull();sela.charge=100;sela.cooldowns[0]=0;
                Assert.IsTrue(sela.Skill(0));yield return new WaitForSecondsRealtime(.3f);
                Assert.AreEqual(1000,victim.Health.integrity,"flight lance passed opaque cover");Object.Destroy(wall);yield return null;
            }
            int hits=0;victim.Health.Damaged+=(_,__,___)=>hits++;sela.charge=100;sela.cooldowns[0]=0;
            Assert.IsTrue(sela.Skill(0));yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(1,hits,"uncovered lance must hit this body exactly once");
        }
        [UnityTest] public IEnumerator FlightCounterUsesTheCoveredBeamPath()
        {
            Place(taren,new Vector3(210,1,0));Place(sela,new Vector3(200,1,0));sela.target=victim.Health;
            var wall=Cover(4,false);sela.charge=100;Assert.IsTrue(sela.Skill(3));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(sela.Deflect(new DamagePacket{source=victim.Health,amount=1,type=DamageType.Beam}));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(1000,victim.Health.integrity,"flight counter passed movable cover");
            wall.SetActive(false);Physics.SyncTransforms();sela.charge=100;sela.cooldowns[3]=0;Assert.IsTrue(sela.Skill(3));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(sela.Deflect(new DamagePacket{source=victim.Health,amount=1,type=DamageType.Beam}));
            yield return new WaitForSecondsRealtime(.3f);Assert.Less(victim.Health.integrity,1000,"uncovered counter did not fire");
        }
        [UnityTest] public IEnumerator PendingFlightShotFreezesDuringPauseAndCancelsOnStagger()
        {
            var observer=Observe(taren);Assert.IsTrue(taren.Attack());GameTime.Paused=true;
            yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(0,observer.errors.Count,"paused queued shot released");
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(.24f);Assert.AreEqual(1,observer.errors.Count,"held shot failed to resume");
            observer.ResetSample();Assert.IsTrue(taren.Attack());taren.Stagger(.5f);
            yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(0,observer.errors.Count,"cancelled shot released after interruption");
        }
    }
}
