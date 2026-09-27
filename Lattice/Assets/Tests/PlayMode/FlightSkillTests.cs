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
    public sealed class FlightSkillTests
    {
        CombatActor taren,sela;EnemyBrain victim;GameObject fixture;
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
            fixture=new GameObject("Flight skill fixture");
            victim=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDart"),new Vector3(200,1,4));
            victim.Passive=true;victim.enabled=false;victim.Health.maximum=victim.Health.integrity=1000;victim.transform.SetParent(fixture.transform);
            taren.target=sela.target=victim.Health;Physics.SyncTransforms();yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();if(fixture!=null)Object.Destroy(fixture);SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor hero,Vector3 position)
        {
            var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.GetComponent<FlightMotor>().Halt();
            hero.transform.SetPositionAndRotation(position,Quaternion.identity);cc.enabled=true;hero.charge=100;Physics.SyncTransforms();
        }
        GameObject Cover(float z)
        {
            var wall=new GameObject("Flight skill cover",typeof(BoxCollider));wall.transform.SetParent(fixture.transform);
            wall.transform.position=new Vector3(200,4,z);wall.GetComponent<BoxCollider>().size=new Vector3(12,20,.12f);Physics.SyncTransforms();return wall;
        }
        static LineRenderer NamedEdge(string name)
        {
            foreach(var edge in Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))if(edge.name==name)return edge;
            return null;
        }
        [UnityTest] public IEnumerator CleaveWaitsForTheVisibleReleaseThenContactsTheActualEdgeOnce()=>AreaContact(0);
        [UnityTest] public IEnumerator PulseWaitsForTheVisibleReleaseThenContactsTheActualEdgeOnce()=>AreaContact(2);
        IEnumerator AreaContact(int slot)
        {
            {
                victim.Health.ResetFull();taren.charge=100;taren.cooldowns[slot]=0;int hits=0;float worstGap=0;
                System.Action<Health,DamagePacket,float> witness=(_,packet,amount)=>
                {
                    if(packet.source!=taren.Health)return;hits++;
                    var edge=NamedEdge("Flight skill edge");Assert.IsNotNull(edge,"invisible instantaneous area damage");
                    float gap=float.PositiveInfinity;var body=victim.GetComponent<CapsuleCollider>();
                    for(int i=0;i<edge.positionCount;i++)
                    {var point=edge.transform.TransformPoint(edge.GetPosition(i));gap=Mathf.Min(gap,Vector3.Distance(point,body.ClosestPoint(point)));}
                    worstGap=Mathf.Max(worstGap,gap);
                };
                victim.Health.Damaged+=witness;Assert.IsTrue(taren.Skill(slot));yield return new WaitForSecondsRealtime(.06f);
                Assert.AreEqual(1000,victim.Health.integrity,"area skill damaged before anticipation and travelling edge");
                yield return new WaitForSecondsRealtime(.6f);victim.Health.Damaged-=witness;
                Assert.AreEqual(1,hits);Assert.LessOrEqual(worstGap,.15f,"damage arrived before the rendered edge reached the target");
            }
        }
        [UnityTest] public IEnumerator AreaSkillsAndTheirVisibleEdgesStopAtOpaqueCover()
        {
            var wall=Cover(2);
            foreach(int slot in new[]{0,2})
            {
                taren.charge=100;taren.cooldowns[slot]=0;Assert.IsTrue(taren.Skill(slot));float deadline=Time.unscaledTime+.7f;bool sawEdge=false;
                while(Time.unscaledTime<deadline)
                {
                    var edge=NamedEdge("Flight skill edge");
                    if(edge!=null)
                    {
                        sawEdge=true;for(int i=0;i<edge.positionCount;i++)
                            Assert.Less(edge.transform.TransformPoint(edge.GetPosition(i)).z,2.02f,"visible arc crosses its solid cover");
                    }
                    yield return null;
                }
                Assert.IsTrue(sawEdge,"skill never produced a visible travelling boundary");Assert.AreEqual(1000,victim.Health.integrity,"area skill bypassed cover");
            }
        }
        [UnityTest] public IEnumerator PassingWaveDoesNotLeaveHiddenDamageInItsWake()
        {
            victim.transform.position=new Vector3(200,1,7);Physics.SyncTransforms();Assert.IsTrue(taren.Skill(2));
            yield return new WaitForSecondsRealtime(.41f);Assert.IsNotNull(NamedEdge("Flight skill edge"),"fixture must enter behind a still-visible outgoing wave");
            victim.transform.position=new Vector3(200,1,1.8f);Physics.SyncTransforms();yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(1000,victim.Health.integrity,"damage lingered inside the already-passed visible edge");
        }
        [UnityTest] public IEnumerator FlightNetTravelsFromTheBankedMuzzleAndCannotTeleportThroughCover()
        {
            Place(taren,new Vector3(210,1,0));Place(sela,new Vector3(200,1,0));victim.transform.position=new Vector3(200,1,8);Physics.SyncTransforms();
            var hull=sela.GetComponent<FormController>().flight;hull.GetComponent<FlightShipMotion>().enabled=false;hull.transform.localRotation=Quaternion.Euler(-5,0,28);
            foreach(float z in new[]{.3f,3f})
            {
                var wall=Cover(z);sela.charge=100;sela.cooldowns[2]=0;Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1);
                Assert.AreEqual(1000,victim.Health.integrity,"flight field teleported through opaque cover");
                var fields=Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None);Assert.AreEqual(1,fields.Length);
                Assert.Less(fields[0].transform.position.z,z,"field landed beyond the wall");
                Object.Destroy(fields[0].gameObject);Object.Destroy(wall);yield return null;
            }
            var observer=sela.gameObject.AddComponent<FlightNetBirthObserver>();observer.hull=hull.transform;
            sela.charge=100;sela.cooldowns[2]=0;Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(1,observer.count);Assert.Less(observer.error,.22f,"net does not emerge from independently measured nose region");
            Assert.Less(victim.Health.integrity,1000,"unobstructed net failed to reach its nearby target");
        }
        [UnityTest] public IEnumerator FlightFieldKeepsItsAltitudeAndCannotFollowATargetOutsideThrowRange()
        {
            var floor=new GameObject("Floor below flight field",typeof(BoxCollider));floor.transform.SetParent(fixture.transform);
            floor.transform.position=new Vector3(200,-.5f,6);floor.GetComponent<BoxCollider>().size=new Vector3(20,1,20);
            Place(taren,new Vector3(210,4,0));Place(sela,new Vector3(200,4,0));victim.transform.position=new Vector3(200,4,30);Physics.SyncTransforms();
            Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreEqual(1000,victim.Health.integrity,"distant target extended flight net range");
            var fields=Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None);Assert.AreEqual(1,fields.Length);
            Assert.That(fields[0].transform.position.z,Is.InRange(5.5f,6.2f));
            Assert.That(fields[0].transform.position.y,Is.InRange(4.75f,4.85f),"flight field dropped to the map floor");
            var mesh=fields[0].GetComponent<MeshFilter>();Assert.IsNotNull(mesh);
            foreach(var vertex in mesh.sharedMesh.vertices)Assert.That(mesh.transform.TransformPoint(vertex).y,Is.InRange(4.75f,4.85f));
        }
        [UnityTest] public IEnumerator PausedAndInterruptedFlightCastsCannotReleasePendingWork()
        {
            foreach(var hero in new[]{taren,sela})foreach(int slot in hero==taren?new[]{0,2,3}:new[]{2,3})
            {
                Place(taren,new Vector3(210,1,0));Place(sela,new Vector3(220,1,0));Place(hero,new Vector3(200,1,0));hero.cooldowns[slot]=0;
                Assert.IsTrue(hero.Skill(slot));GameTime.Paused=true;yield return new WaitForSecondsRealtime(.25f);
                Assert.IsFalse(hero.Overdriving||hero.Refracting,"buff armed while paused before release");
                Assert.AreEqual(0,Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None).Length,"net appeared during paused anticipation");
                Assert.AreEqual(1000,victim.Health.integrity);
                hero.Stagger(.3f);GameTime.Paused=false;yield return new WaitForSecondsRealtime(.6f);
                Assert.AreEqual(1000,victim.Health.integrity,"interrupted cast retained damage");
                Assert.AreEqual(0,Object.FindObjectsByType<PulseField>(FindObjectsSortMode.None).Length);
                Assert.IsFalse(hero.Overdriving||hero.Refracting,"interrupted buff armed later");
            }
        }
        [UnityTest] public IEnumerator ReleasedNetKeepsItsFactionAfterSourceCleanup()
        {
            Place(taren,new Vector3(200,1,4));Place(sela,new Vector3(210,1,0));
            victim.transform.position=new Vector3(203,1,4);Physics.SyncTransforms();
            var source=new GameObject("Released friendly seed source",typeof(Health));source.transform.SetParent(fixture.transform);
            source.GetComponent<Health>().friendly=true;float allyHealth=taren.Health.integrity;
            NetSeed.Throw(new Vector3(200,1.8f,0),new Vector3(200,1.8f,4),
                new DamagePacket{source=source.GetComponent<Health>(),amount=50,type=DamageType.Pulse},true);
            Object.Destroy(source);yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(allyHealth,taren.Health.integrity,"released net changed teams after its source was destroyed");
            Assert.Less(victim.Health.integrity,1000,"released seed must still damage the opposing body after source cleanup");
        }
        [UnityTest] public IEnumerator FlightBuffCuesFollowTheTimedStateAndClearOnDefeat()
        {
            foreach(var hero in new[]{taren,sela})
            {
                hero.charge=100;Assert.IsTrue(hero.Skill(3));yield return new WaitForSecondsRealtime(.25f);
                Assert.IsTrue(hero.character=="Taren"?hero.Overdriving:hero.Refracting);
                var cue=hero.transform.Find("Flight skill charge");Assert.IsNotNull(cue,"flight buff lacks its visible hull cue");
                Assert.IsTrue(cue.gameObject.activeInHierarchy);
                var line=cue.GetComponent<LineRenderer>();Assert.IsNotNull(line);Assert.Greater(line.GetPosition(0).magnitude,1.5f,"cue does not surround the visible hull");
                var lethal=new DamagePacket{amount=10000,type=DamageType.Pulse};
                hero.Health.Receive(lethal);hero.Health.Receive(lethal); // Refract intercepts the first incoming packet.
                Assert.IsFalse(hero.Health.Alive);yield return null;yield return null;
                Assert.IsFalse(cue.gameObject.activeInHierarchy,"dead hull retains live buff effect");
            }
        }
    }
    [DefaultExecutionOrder(1000)]
    public sealed class FlightNetBirthObserver:MonoBehaviour
    {
        public Transform hull;public int count;public float error;NetSeed seen;
        void LateUpdate()
        {
            var seed=Object.FindFirstObjectByType<NetSeed>();if(seed==null||seed==seen)return;seen=seed;count++;
            error=Vector3.Distance(seed.transform.position,hull.TransformPoint(new Vector3(0,.1f,1.59f)));
        }
    }
}
