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
    public sealed class ProjectileCoverTests
    {
        GameObject fixture;CombatActor taren,sela;EnemyBrain victim;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var member in PartyController.Current.members)
            {
                member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;
                if(member.character=="Taren")taren=member;else if(member.character=="Sela")sela=member;
            }
            yield return new WaitForSecondsRealtime(.7f);
            fixture=new GameObject("Ground skill fixture");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(fixture.transform);floor.transform.position=new Vector3(200,-.5f,5);floor.transform.localScale=new Vector3(20,1,20);
            Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(208,0,0));
            victim=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),new Vector3(200,0,4));victim.Passive=true;victim.enabled=false;
            victim.transform.SetParent(fixture.transform);victim.Health.maximum=victim.Health.integrity=1000;
            taren.target=victim.Health;sela.target=victim.Health;Physics.SyncTransforms();yield return null;yield return null;
        }
        static void Place(CombatActor actor,Vector3 p)
        {var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.SetPositionAndRotation(p,Quaternion.identity);cc.enabled=true;actor.TargetLocked=false;actor.charge=100;}
        GameObject Cover(float z)
        {var g=new GameObject("Skill cover",typeof(BoxCollider));g.transform.SetParent(fixture.transform);g.transform.position=new Vector3(200,1,z);g.GetComponent<BoxCollider>().size=new Vector3(6,4,.2f);g.isStatic=true;Physics.SyncTransforms();return g;}
        [UnityTearDown] public IEnumerator Cleanup()
        {if(fixture!=null)Object.Destroy(fixture);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        [UnityTest] public IEnumerator ReleasedProjectileCannotPassSolidMovableCover()
        {
            Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(208,0,0));
            var cover=Cover(2);cover.isStatic=false;
            Projectile.Fire(new Vector3(200,.8f,0),Vector3.forward,new DamagePacket{source=taren.Health,amount=50,type=DamageType.Pulse},240);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(1000,victim.Health.integrity,"projectile ignored solid movable cover");
            cover.SetActive(false);Physics.SyncTransforms();
            Projectile.Fire(new Vector3(200,.8f,0),Vector3.forward,new DamagePacket{source=taren.Health,amount=50,type=DamageType.Pulse},240);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.Less(victim.Health.integrity,1000,"unobstructed control did not reach target");
        }
        [UnityTest] public IEnumerator NearestSurfaceWinsEvenWhenOneFrameCrossesBodiesAndCover()
        {
            // The farther victim is created first. Physics query ordering may not
            // be interpreted as travel ordering; use the nearest real surface.
            var cc=victim.GetComponent<CharacterController>();cc.enabled=false;victim.transform.position=new Vector3(200,0,5);
            var nearer=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),new Vector3(200,0,3));nearer.Passive=true;nearer.enabled=false;
            nearer.transform.SetParent(fixture.transform);nearer.Health.maximum=nearer.Health.integrity=1000;Physics.SyncTransforms();
            var packet=new DamagePacket{source=taren.Health,amount=50,type=DamageType.Pulse};
            Projectile.Fire(new Vector3(200,.8f,0),Vector3.forward,packet,1000);yield return new WaitForSecondsRealtime(.15f);
            Assert.Less(nearer.Health.integrity,1000,"farther query entry stole the nearer contact");
            Assert.AreEqual(1000,victim.Health.integrity,"one non-piercing projectile hit a farther victim");
            nearer.Health.integrity=1000;var cover=Cover(1.5f);
            Projectile.Fire(new Vector3(200,.8f,0),Vector3.forward,packet,1000);yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual(1000,nearer.Health.integrity,"nearer opaque cover lost to a body in the same sweep");Assert.AreEqual(1000,victim.Health.integrity);
        }
        [UnityTest] public IEnumerator PauseHoldsReleasedProjectileAndSceneUnloadReconcilesActiveCount()
        {
            foreach(var old in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))Object.Destroy(old.gameObject);
            yield return null;
            Assert.AreEqual(0,Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length);
            Projectile.Fire(new Vector3(200,.8f,-8),Vector3.forward,new DamagePacket{source=taren.Health,amount=50,type=DamageType.Pulse},2);
            yield return null;var projectile=Object.FindFirstObjectByType<Projectile>();Assert.IsNotNull(projectile);
            GameTime.Paused=true;Vector3 held=projectile.transform.position;
            yield return new WaitForSecondsRealtime(.3f);Assert.Less(Vector3.Distance(held,projectile.transform.position),.0001f);GameTime.Paused=false;
            yield return new WaitForSecondsRealtime(.2f);Assert.Greater(Vector3.Distance(held,projectile.transform.position),.2f);
            SceneManager.LoadScene("_Boot");yield return null;yield return null;
            Assert.AreEqual(0,Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(0,Projectile.ActiveCount,"scene unload left stale live projectile accounting");
        }
        [UnityTest] public IEnumerator ReleasedEnemyShotRetainsItsTeamAfterSourceCleanup()
        {
            Place(taren,new Vector3(200,0,7));Place(sela,new Vector3(208,0,0));
            var attacker=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),new Vector3(205,0,0));attacker.Passive=true;attacker.enabled=false;
            attacker.transform.SetParent(fixture.transform);float before=taren.Health.integrity;
            // Hold released energy until destruction is completed. A long first
            // frame must not let it cross the ally before the source disappears.
            GameTime.Paused=true;
            Projectile.Fire(new Vector3(200,.8f,0),Vector3.forward,new DamagePacket{source=attacker.Health,amount=10,type=DamageType.Pulse},12);
            Object.Destroy(attacker.gameObject);yield return null;yield return null;
            Assert.IsTrue(attacker==null,"source cleanup did not complete");
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(1000,victim.Health.integrity,"released enemy energy changed teams after its source disappeared");
            Assert.Less(taren.Health.integrity,before,"released shot no longer threatened its intended opposing team");
        }
    }
}
