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
    // Observe the first visible projectile pose, independently of the firing
    // implementation. The actual retargeted left hand is the emitter landmark.
    [DefaultExecutionOrder(1000)]
    public sealed class ShotBirthObserver : MonoBehaviour
    {
        readonly HashSet<Projectile> seen=new();
        public readonly List<float> errors=new();
        public readonly List<float> phases=new();
        public Animator rig;
        public void ResetObservation(){seen.Clear();errors.Clear();phases.Clear();}
        void LateUpdate()
        {
            if(rig==null)return;
            var hand=rig.GetBoneTransform(HumanBodyBones.LeftHand);
            foreach(var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                if(Vector3.Distance(projectile.transform.position,transform.position)>3||!seen.Add(projectile))continue;
                errors.Add(Vector3.Distance(projectile.transform.position,hand.position));
                phases.Add(rig.GetCurrentAnimatorStateInfo(0).normalizedTime);
            }
        }
    }
    public sealed class RangedContactTests
    {
        GameObject fixture;
        CombatActor actor;
        EnemyBrain victim;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Ground");
            float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var member in PartyController.Current.members)
            {
                member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;
                if(member.character=="Sela")actor=member;
            }
            yield return new WaitForSecondsRealtime(.7f);
            Assert.IsNotNull(actor);
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;
            actor.transform.SetPositionAndRotation(new Vector3(200,0,0),Quaternion.identity);cc.enabled=true;
            actor.TargetLocked=false;
            fixture=new GameObject("Ranged contact fixture");
            victim=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),actor.transform.position+Vector3.forward*8);
            victim.transform.SetParent(fixture.transform);victim.Passive=true;victim.enabled=false;
            victim.Health.maximum=victim.Health.integrity=1000;
            actor.target=victim.Health;Physics.SyncTransforms();yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(fixture!=null)Object.Destroy(fixture);
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTest] public IEnumerator NeedleShotsBeginAtThePosedLeftEmitterAndReachAShortTarget()
        {
            var observer=actor.gameObject.AddComponent<ShotBirthObserver>();
            observer.rig=actor.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>();
            for(int shot=0;shot<3;shot++)
            {
                observer.ResetObservation();float before=victim.Health.integrity;
                Assert.IsTrue(actor.Attack());yield return new WaitForSecondsRealtime(.75f);
                Assert.AreEqual(1,observer.errors.Count,"one ordinary shot must create one visible projectile at the emitter");
                Assert.LessOrEqual(observer.errors[0],.35f,"projectile is born away from the actual posed left hand");
                Assert.Less(victim.Health.integrity,before,"aim from the raised emitter must still reach a short creature");
                TestContext.WriteLine($"NEEDLE shot={shot+1} handError={observer.errors[0]:F6} phase={observer.phases[0]:F6}");
            }
        }
        [UnityTest] public IEnumerator ThinCoverBetweenBodyAndMuzzlePreventsAHiddenShot()
        {
            var cover=new GameObject("Near muzzle cover",typeof(BoxCollider));cover.transform.SetParent(fixture.transform);
            cover.transform.position=actor.transform.position+new Vector3(0,1,.45f);
            cover.GetComponent<BoxCollider>().size=new Vector3(4,4,.1f);cover.isStatic=true;
            Physics.SyncTransforms();Assert.IsTrue(actor.Attack());yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(1000,victim.Health.integrity,"a muzzle offset must not spawn a projectile past solid cover");
            cover.SetActive(false);Physics.SyncTransforms();Assert.IsTrue(actor.Attack());
            yield return new WaitForSecondsRealtime(.8f);
            Assert.Less(victim.Health.integrity,1000,"the same unobstructed shot must reach its target");
        }
        [UnityTest] public IEnumerator PauseHoldsThePosedReleaseAndResumesOneShot()
        {
            var observer=actor.gameObject.AddComponent<ShotBirthObserver>();
            observer.rig=actor.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>();
            Assert.IsTrue(actor.Attack());GameTime.Paused=true;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(0,observer.errors.Count,"pause released a pending projectile");
            Assert.AreEqual(1000,victim.Health.integrity);
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(.75f);
            Assert.AreEqual(1,observer.errors.Count,"resume must release exactly one shot");
            Assert.Less(victim.Health.integrity,1000);
        }
        [UnityTest] public IEnumerator GuardDodgeStaggerAndDeathCancelUnreleasedShots()
        {
            var observer=actor.gameObject.AddComponent<ShotBirthObserver>();
            observer.rig=actor.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>();
            for(int cancellation=0;cancellation<4;cancellation++)
            {
                observer.ResetObservation();victim.Health.ResetFull();
                Assert.IsTrue(actor.Attack());
                if(cancellation==0)actor.Guard(true);
                else if(cancellation==1)Assert.IsTrue(actor.Dodge(Vector3.right));
                else if(cancellation==2)actor.Stagger(.3f);
                else actor.Health.Receive(new DamagePacket{source=victim.Health,amount=100000});
                yield return new WaitForSecondsRealtime(.75f);
                Assert.AreEqual(0,observer.errors.Count,$"cancellation {cancellation} left a visible ghost shot");
                Assert.AreEqual(1000,victim.Health.integrity,$"cancellation {cancellation} left hidden damage");
                if(cancellation==0)actor.Guard(false);
                // A deliberate uncancelled-shot control must fail those same
                // observations, even after guard/dodge/stagger/down presentation.
                var hand=observer.rig.GetBoneTransform(HumanBodyBones.LeftHand).position;
                var targetPoint=victim.GetComponent<CapsuleCollider>().bounds.center;
                Projectile.Fire(hand,(targetPoint-hand).normalized,actor.Packet(20,DamageType.Beam,0));
                yield return new WaitForSecondsRealtime(.5f);
                Assert.Greater(observer.errors.Count,0,"ghost-shot control was not observed");
                Assert.Less(victim.Health.integrity,1000,"ghost-shot damage control did not reach the real target");
            }
        }
        [UnityTest] public IEnumerator ScatterFanStartsAtThePosedEmitter()
        {
            var observer=actor.gameObject.AddComponent<ShotBirthObserver>();
            observer.rig=actor.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>();
            actor.charge=100;Assert.IsTrue(actor.Skill(1));
            yield return new WaitForSecondsRealtime(.65f);
            Assert.AreEqual(5,observer.errors.Count,"Scatter Bloom must create five visible projectiles");
            foreach(float error in observer.errors)
                Assert.LessOrEqual(error,.35f,"fan projectile is detached from the posed emitter");
            Assert.Less(victim.Health.integrity,1000,"central fan ray must reach the real short target");
            TestContext.WriteLine("FAN handErrors="+string.Join(",",observer.errors));
        }
        [UnityTest] public IEnumerator LanceStopsAtSolidCoverAndPiercesUnobstructedBodiesOnce()
        {
            var second=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),actor.transform.position+Vector3.forward*11);
            second.transform.SetParent(fixture.transform);second.Passive=true;second.enabled=false;
            second.Health.maximum=second.Health.integrity=1000;
            int firstHits=0,secondHits=0;
            victim.Health.Damaged+=(_,__,___)=>firstHits++;
            second.Health.Damaged+=(_,__,___)=>secondHits++;
            var origins=new List<float>();var contacts=new List<float>();
            var rig=actor.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>();
            void ObserveBeam(Health body,DamagePacket _,float __)
            {
                var trace=VisibleTrace();
                if(trace==null){origins.Add(float.PositiveInfinity);contacts.Add(float.PositiveInfinity);return;}
                var start=trace.GetPosition(0);var end=trace.GetPosition(trace.positionCount-1);
                origins.Add(Vector3.Distance(start,rig.GetBoneTransform(HumanBodyBones.LeftHand).position));
                var point=body.GetComponent<CapsuleCollider>().bounds.center;var along=end-start;
                var closest=start+along*Mathf.Clamp01(Vector3.Dot(point-start,along)/along.sqrMagnitude);
                contacts.Add(Vector3.Distance(point,closest));
            }
            victim.Health.Damaged+=ObserveBeam;second.Health.Damaged+=ObserveBeam;
            var cover=new GameObject("Lance cover",typeof(BoxCollider));cover.transform.SetParent(fixture.transform);
            cover.transform.position=actor.transform.position+new Vector3(0,1,4);
            cover.GetComponent<BoxCollider>().size=new Vector3(4,4,.18f);cover.isStatic=true;
            Physics.SyncTransforms();actor.charge=100;Assert.IsTrue(actor.Skill(0));
            float deadline=Time.unscaledTime+.6f;
            while(VisibleTrace()==null&&Time.unscaledTime<deadline)yield return null;
            var blockedTrace=VisibleTrace();Assert.IsNotNull(blockedTrace,"lance has no visible path");
            Assert.LessOrEqual(blockedTrace.GetPosition(blockedTrace.positionCount-1).z,cover.GetComponent<BoxCollider>().bounds.min.z+.001f,
                "the visible beam extends through its blocking surface");
            yield return new WaitForSecondsRealtime(.75f);
            Assert.AreEqual(1000,victim.Health.integrity,"piercing energy crossed solid cover");
            Assert.AreEqual(1000,second.Health.integrity,"piercing energy crossed solid cover to the rear target");
            cover.SetActive(false);Physics.SyncTransforms();actor.cooldowns[0]=0;actor.charge=100;
            Assert.IsTrue(actor.Skill(0));yield return new WaitForSecondsRealtime(.75f);
            Assert.AreEqual(1,firstHits,"front body must receive exactly one lance contact");
            Assert.AreEqual(1,secondHits,"rear body must receive exactly one piercing contact");
            Assert.AreEqual(2,origins.Count);
            foreach(float distance in origins)Assert.LessOrEqual(distance,.35f,"visible lance is detached from the hand");
            foreach(float distance in contacts)Assert.LessOrEqual(distance,.65f,"damaged body is away from the visible lance");
            TestContext.WriteLine("LANCE handErrors="+string.Join(",",origins)+" bodyDistances="+string.Join(",",contacts));
        }
        LineRenderer VisibleTrace()
        {
            foreach(var line in actor.GetComponentsInChildren<LineRenderer>())
                if(line.enabled&&line.positionCount>=2)return line;
            return null;
        }
        [UnityTest] public IEnumerator GroundRangedSkillsCancelBeforeRelease()
        {
            var observer=actor.gameObject.AddComponent<ShotBirthObserver>();
            observer.rig=actor.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>();
            for(int slot=0;slot<2;slot++)for(int cancellation=0;cancellation<4;cancellation++)
            {
                actor.charge=100;actor.cooldowns[slot]=0;observer.ResetObservation();victim.Health.ResetFull();
                Assert.IsTrue(actor.Skill(slot));
                if(cancellation==0)actor.Guard(true);
                else if(cancellation==1)Assert.IsTrue(actor.Dodge(Vector3.right));
                else if(cancellation==2)actor.Stagger(.3f);
                else actor.Health.Receive(new DamagePacket{source=victim.Health,amount=100000});
                yield return new WaitForSecondsRealtime(.75f);
                Assert.AreEqual(0,observer.errors.Count,$"skill {slot} cancellation {cancellation} released projectiles");
                Assert.IsNull(VisibleTrace(),$"skill {slot} cancellation {cancellation} retained a beam");
                Assert.AreEqual(1000,victim.Health.integrity,$"skill {slot} cancellation {cancellation} left ghost damage");
                if(cancellation==0)actor.Guard(false);
                if(cancellation==3){actor.Health.ResetFull();yield return new WaitForSecondsRealtime(1.65f);}
            }
        }
    }
}
