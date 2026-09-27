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
    public sealed class FlightContactTests
    {
        GameObject fixture;CombatActor taren,sela;EnemyBrain victim;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var member in PartyController.Current.members)
            {
                member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;
                if(member.character=="Taren")taren=member;else sela=member;
            }
            yield return new WaitForSecondsRealtime(.7f);
            fixture=new GameObject("Flight contact fixture");
            Place(taren,new Vector3(200,1,0));Place(sela,new Vector3(210,1,0));
            victim=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDart"),new Vector3(200,1,4));
            victim.Passive=true;victim.transform.SetParent(fixture.transform);victim.Health.maximum=victim.Health.integrity=1000;
            taren.target=victim.Health;sela.target=victim.Health;Physics.SyncTransforms();yield return null;yield return null;
        }
        static void Place(CombatActor actor,Vector3 point)
        {
            actor.GetComponent<FlightMotor>().Halt();var cc=actor.GetComponent<CharacterController>();cc.enabled=false;
            actor.transform.SetPositionAndRotation(point,Quaternion.identity);cc.enabled=true;actor.charge=100;actor.TargetLocked=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();if(fixture!=null)Object.Destroy(fixture);SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        IEnumerator Travel(CombatActor actor,float seconds)
        {
            float deadline=Time.unscaledTime+seconds;
            while(Time.unscaledTime<deadline){actor.motor.Move(Vector2.zero,false,false);yield return null;}
        }
        [UnityTest] public IEnumerator LungeCannotDamageBeyondSolidCover()
        {
            foreach(var actor in new[]{taren,sela})foreach(bool fixedCover in new[]{true,false})foreach(float distance in new[]{4f,2.8f})
            {
                Place(taren,new Vector3(210,1,0));Place(sela,new Vector3(212,1,0));Place(actor,new Vector3(200,1,0));
                victim.transform.position=new Vector3(200,1,distance);victim.Health.ResetFull();actor.target=victim.Health;
                var wall=new GameObject("Lunge cover",typeof(BoxCollider));wall.transform.SetParent(fixture.transform);
                wall.transform.position=new Vector3(200,2,2);wall.GetComponent<BoxCollider>().size=new Vector3(8,6,.2f);wall.isStatic=fixedCover;
                Physics.SyncTransforms();Assert.IsTrue(actor.Lunge());yield return Travel(actor,.85f);
                Assert.Less(actor.transform.position.z,2,"fixture cover must stop the craft");
                Assert.AreEqual(1000,victim.Health.integrity,$"{actor.character} lunge damaged through solid cover (static={fixedCover})");
                Object.Destroy(wall);yield return null;
            }
        }
        [UnityTest] public IEnumerator LungeDamageRequiresActualCraftTravel()
        {
            Assert.IsTrue(taren.Lunge());yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(0,taren.transform.position.z,"fixture deliberately makes no motor movement");
            Assert.AreEqual(1000,victim.Health.integrity,"frame-count lunge samples hit distant space while the craft remains stationary");
        }
        [UnityTest] public IEnumerator BothCraftHitAlongTheirTravelOnlyOnce()
        {
            foreach(var actor in new[]{taren,sela})
            {
                Place(taren,new Vector3(210,1,0));Place(sela,new Vector3(212,1,0));Place(actor,new Vector3(200,1,0));
                victim.transform.position=new Vector3(200,1,6);victim.Health.ResetFull();actor.target=victim.Health;Physics.SyncTransforms();
                int hits=0;float greatestEdgeGap=0;
                System.Action<Health,DamagePacket,float> observe=(_,packet,amount)=>
                {
                    if(packet.source!=actor.Health)return;hits++;
                    var edge=actor.transform.Find("Flight lunge edge").GetComponent<LineRenderer>();
                    Assert.IsTrue(edge.enabled,"damaging lunge lacks its visible energy edge");
                    var body=victim.GetComponent<CapsuleCollider>();float gap=float.PositiveInfinity;
                    for(int i=0;i<edge.positionCount;i++)
                    {
                        Vector3 point=edge.transform.TransformPoint(edge.GetPosition(i));
                        gap=Mathf.Min(gap,Vector3.Distance(point,body.ClosestPoint(point)));
                    }
                    greatestEdgeGap=Mathf.Max(greatestEdgeGap,gap);
                };
                victim.Health.Damaged+=observe;Assert.IsTrue(actor.Lunge());float deadline=Time.unscaledTime+.85f;
                float closestBody=float.PositiveInfinity;
                while(Time.unscaledTime<deadline)
                {
                    actor.motor.Move(Vector2.zero,false,false);
                    Vector3 center=actor.transform.position+Vector3.up*.8f;
                    closestBody=Mathf.Min(closestBody,Vector3.Distance(center,victim.GetComponent<CapsuleCollider>().ClosestPoint(center)));
                    yield return null;
                }
                victim.Health.Damaged-=observe;
                Assert.AreEqual(1,hits,actor.character+" should damage this target once per lunge; closest body="+closestBody+" end="+actor.transform.position);
                Assert.LessOrEqual(greatestEdgeGap,.12f,"damage arrived before the actual visible hull edge reached the victim");
            }
        }
        [UnityTest] public IEnumerator InterruptedLungeCannotReleaseDeferredContact()
        {
            Assert.IsTrue(taren.Lunge());taren.Stagger(.5f);yield return Travel(taren,.6f);
            Assert.AreEqual(1000,victim.Health.integrity,"cancelled lunge retained damaging travel");
        }
        [UnityTest] public IEnumerator PausedLungeWaitsThenResumesItsActualTravel()
        {
            Assert.IsTrue(taren.Lunge());GameTime.Paused=true;yield return Travel(taren,.2f);
            Assert.AreEqual(0,taren.transform.position.z);Assert.AreEqual(1000,victim.Health.integrity);
            GameTime.Paused=false;yield return Travel(taren,.85f);
            Assert.Less(victim.Health.integrity,1000,"paused lunge never resumed contact");
        }
        [UnityTest] public IEnumerator FlightDashSkillCannotPierceCoverBeforeItsCraftArrives()
        {
            var wall=new GameObject("Skill dash cover",typeof(BoxCollider));wall.transform.SetParent(fixture.transform);
            wall.transform.position=new Vector3(200,2,2);wall.GetComponent<BoxCollider>().size=new Vector3(8,6,.2f);Physics.SyncTransforms();
            Assert.IsTrue(taren.Skill(1));yield return Travel(taren,.85f);
            Assert.Less(taren.transform.position.z,2);Assert.AreEqual(1000,victim.Health.integrity,"flight dash skill pierced cover without travelling through it");
            wall.SetActive(false);Place(taren,new Vector3(200,1,0));taren.cooldowns[1]=0;Physics.SyncTransforms();
            Assert.IsTrue(taren.Skill(1));yield return Travel(taren,.85f);
            Assert.Less(victim.Health.integrity,1000,"unobstructed skill dash failed to hit");
        }
    }
}
