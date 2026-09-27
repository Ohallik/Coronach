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
    // Independent observation of the visible wrist edge, including its swept
    // path since the previous rendered pose. No move timing constants are used.
    [DefaultExecutionOrder(1000)]
    public sealed class VisibleEdgeObserver : MonoBehaviour
    {
        public readonly List<float> contactDistances=new();
        Vector3 previousHand,previousTip;
        bool ready;
        bool Edge(out Vector3 hand,out Vector3 tip)
        {
            hand=tip=Vector3.zero;
            foreach(var line in GetComponentsInChildren<LineRenderer>())
                if(line.name=="Wrist energy"&&line.enabled&&line.positionCount>1)
                {hand=line.GetPosition(0);tip=line.GetPosition(line.positionCount-1);return true;}
            return false;
        }
        void LateUpdate(){ready=Edge(out previousHand,out previousTip);}
        public void Observe(Collider victim)
        {
            if(!Edge(out var hand,out var tip)){contactDistances.Add(float.PositiveInfinity);return;}
            float nearest=float.PositiveInfinity;
            for(int frame=0;frame<=12;frame++)
                for(int edge=0;edge<=24;edge++)
                {
                    float blend=ready?frame/12f:1;
                    var p=Vector3.Lerp(Vector3.Lerp(previousHand,hand,blend),Vector3.Lerp(previousTip,tip,blend),edge/24f);
                    nearest=Mathf.Min(nearest,Vector3.Distance(p,victim.ClosestPoint(p)));
                }
            contactDistances.Add(nearest);
        }
    }

    public sealed class AttackContactTests
    {
        GameObject probes;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(probes!=null)Object.Destroy(probes);
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTest] public IEnumerator MeleeDamageStaysOnTheVisibleSweptEdge()=>CheckContacts(false);
        [UnityTest] public IEnumerator ObserverRejectsTheLegacyOversizedSphere()=>CheckContacts(true);
        [UnityTest] public IEnumerator SolidCoverAndLeavingReachPreventMeleeDamage()
        {
            var actor=PartyController.Current.Active;
            var controller=actor.GetComponent<CharacterController>();controller.enabled=false;
            actor.transform.SetPositionAndRotation(new Vector3(200,0,0),Quaternion.identity);controller.enabled=true;
            actor.target=null;actor.TargetLocked=false;
            probes=new GameObject("Cover and retreat fixture");
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),actor.transform.position+Vector3.forward*1.9f);
            enemy.transform.SetParent(probes.transform);enemy.Passive=true;enemy.enabled=false;
            enemy.Health.maximum=enemy.Health.integrity=1000;
            var cover=new GameObject("Solid cover",typeof(BoxCollider));cover.transform.SetParent(probes.transform);
            cover.transform.position=actor.transform.position+new Vector3(0,1,.95f);cover.GetComponent<BoxCollider>().size=new Vector3(6,4,.18f);cover.isStatic=true;
            Physics.SyncTransforms();yield return null;yield return null;
            for(int strike=0;strike<3;strike++)
            {Assert.IsTrue(actor.Attack());yield return new WaitForSecondsRealtime(.7f);}
            Assert.AreEqual(1000,enemy.Health.integrity,"an edge cannot damage through solid cover");
            yield return LegacySphereMustHit(actor,enemy.Health);
            cover.SetActive(false);
            for(int strike=0;strike<3;strike++)
            {
                enemy.transform.position=actor.transform.position+Vector3.forward*1.9f;Physics.SyncTransforms();
                Assert.IsTrue(actor.Attack());yield return new WaitForSecondsRealtime(.06f);
                enemy.transform.position=actor.transform.position+Vector3.forward*3.2f;Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(.64f);
            }
            Assert.AreEqual(1000,enemy.Health.integrity,"a target leaving the visible edge before contact must escape the swing");
            yield return LegacySphereMustHit(actor,enemy.Health);
            enemy.transform.position=actor.transform.position+Vector3.forward*1.9f;Physics.SyncTransforms();
            for(int strike=0;strike<3;strike++)
            {Assert.IsTrue(actor.Attack());yield return new WaitForSecondsRealtime(.7f);}
            Assert.Less(enemy.Health.integrity,1000,"removing cover with the same target in reach must allow contact");
        }
        static IEnumerator LegacySphereMustHit(CombatActor actor,Health victim)
        {
            // Fault control is confined to the test assembly. The old hidden
            // sphere damages this same covered/distant body; the checks above
            // would reject that regression rather than accepting an empty test.
            CombatActor.Strike(actor.transform.position+Vector3.up*.8f+Vector3.forward*1.2f,1.65f,actor.Packet(10,DamageType.Kinetic,0));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.Less(victim.integrity,1000,"legacy sphere fault control failed to reproduce the forbidden contact");
            victim.ResetFull();
        }
        IEnumerator CheckContacts(bool legacyControl)
        {
            var actor=PartyController.Current.Active;
            Assert.AreEqual("Taren",actor.character);
            var controller=actor.GetComponent<CharacterController>();controller.enabled=false;
            actor.transform.SetPositionAndRotation(new Vector3(200,0,0),Quaternion.identity);controller.enabled=true;
            actor.target=null;actor.TargetLocked=false;
            var observation=actor.gameObject.AddComponent<VisibleEdgeObserver>();
            probes=new GameObject("Independent contact probes");
            foreach(float radius in new[]{1.1f,1.9f,2.7f})
                for(int angle=0;angle<360;angle+=30)
                {
                    var go=new GameObject("Contact probe",typeof(BoxCollider),typeof(Health),typeof(Hurtbox));
                    go.transform.SetParent(probes.transform);
                    go.transform.position=actor.transform.position+Quaternion.Euler(0,angle,0)*Vector3.forward*radius;
                    var box=go.GetComponent<BoxCollider>();box.center=Vector3.up*.9f;box.size=new Vector3(.18f,1.8f,.18f);
                    var health=go.GetComponent<Health>();health.maximum=health.integrity=10000;
                    go.GetComponent<Hurtbox>().owner=health;
                    health.Damaged+=(h,p,amount)=>observation.Observe(box);
                }
            Physics.SyncTransforms();yield return null;yield return null;
            for(int strike=0;strike<3;strike++)
            {
                Assert.IsTrue(actor.Attack());
                if(legacyControl)
                {
                    yield return null;yield return null;
                    CombatActor.Strike(actor.transform.position+Vector3.up*.8f+Vector3.forward*1.2f,1.65f,actor.Packet(10,DamageType.Kinetic,0));
                }
                float deadline=GameTime.Now+actor.VisualDuration+.12f;
                while(GameTime.Now<deadline)yield return null;
            }
            Assert.Greater(observation.contactDistances.Count,0,"the authored chain never reaches a nearby target");
            if(legacyControl)
            {
                Assert.IsFalse(observation.contactDistances.Exists(float.IsInfinity),"control lacks a visible edge");
                Assert.IsTrue(observation.contactDistances.Exists(d=>d>.2f),"the geometry observer accepted the old oversized sphere");
            }
            else foreach(float distance in observation.contactDistances)
                Assert.LessOrEqual(distance,.2f,"melee damages a body beyond the visible edge and its swept path");
        }
    }
}
