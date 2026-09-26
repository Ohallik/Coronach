using System.Collections;
using System.Collections.Generic;
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
    public sealed class DeathLifecycleTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Load("Arena_Ground");
        }
        IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);
            float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture load timed out");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 position)
        {var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}

        [UnityTest] public IEnumerator EnemyResolutionIsImmediateButItsBodyHasAReadableFinish()
        {
            var failures=new List<string>();var actor=PartyController.Current.Active;
            foreach(string id in new[]{"SentinelHusk","Ridgehound","Scrapmite","Shellmine","ChoristerDart","ChoristerDrifter","Burrower","Cantor"})
            {
                var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),new Vector3(0,0,-12));enemy.Passive=true;
                yield return null;
                int rewards=GameServices.Current.State.scrip,kills=actor.Kills;
                var packet=new DamagePacket{source=actor.Health,amount=enemy.Health.maximum*100,type=DamageType.Pulse};
                enemy.Health.Receive(packet);enemy.Health.Receive(packet);
                int reward=enemy.definition.boss?150:8;
                Assert.AreEqual(rewards+reward,GameServices.Current.State.scrip,id+" rewards must resolve once, independently of visual cleanup");
                Assert.AreEqual(kills+1,actor.Kills,id+" duplicate lethal delivery must not award another kill");
                yield return new WaitForSecondsRealtime(.35f);
                if(enemy==null){failures.Add(id+" disappeared before a readable death motion");continue;}
                Assert.IsFalse(enemy.GetComponent<CharacterController>().enabled,id+" dead movement collision remains active");
                foreach(var box in enemy.GetComponentsInChildren<Hurtbox>())
                    foreach(var collider in box.GetComponents<Collider>())Assert.IsFalse(collider.enabled,id+" dead hurtbox still participates");
                Assert.IsTrue(enemy.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),id+" visible body disappeared before cleanup");
                GameTime.Paused=true;var root=enemy.transform.position;
                yield return new WaitForSecondsRealtime(.3f);
                Assert.IsNotNull(enemy,id+" paused death was cleaned up");Assert.AreEqual(root,enemy.transform.position);
                GameTime.Paused=false;
                yield return new WaitForSecondsRealtime(enemy.definition.boss?4.5f:3.5f);
                Assert.IsTrue(enemy==null,id+" terminal body was never deliberately cleaned up");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        [UnityTest] public IEnumerator DownedBodiesHoldTheirPoseAndGetUpBeforeControlReturns()
        {
            foreach(string zone in new[]{"Hub_Decks","Arena_Ground"})
            {
                yield return Load(zone);
                var party=PartyController.Current;
                foreach(var actor in party.members)
                {
                    var partner=party.members.First(a=>a!=actor);
                    Place(actor,new Vector3(-4,0,-3));Place(partner,new Vector3(8,0,-3));
                    var rig=actor.GetComponentInChildren<Animator>();
                    float standing=rig.GetBoneTransform(HumanBodyBones.Hips).position.y;
                    actor.Health.Receive(new DamagePacket{amount=actor.Health.maximum*100,type=DamageType.Pulse});
                    yield return new WaitForSecondsRealtime(1.4f);
                    Assert.Less(rig.GetBoneTransform(HumanBodyBones.Hips).position.y,standing-.5f,actor.character+" "+zone+" did not visibly collapse");
                    Vector3 down=rig.GetBoneTransform(HumanBodyBones.Head).position;
                    yield return new WaitForSecondsRealtime(.35f);
                    Assert.Less(Vector3.Distance(down,rig.GetBoneTransform(HumanBodyBones.Head).position),.025f,"downed terminal pose must hold");
                    Assert.IsFalse(actor.Attack(),"downed hero can attack");
                    // The automatic down-swap reapplies the party and re-enables
                    // the follower. Hold this pose fixture stationary once more;
                    // its walking pelvis is deliberately lower than standing.
                    foreach(var member in party.members)
                    {member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
                    actor.Health.Heal(actor.Health.maximum*.35f);yield return null;yield return null;
                    Assert.IsFalse(actor.CanAct,"revive skipped protected get-up and immediately returned control");
                    float revived=actor.Health.integrity;
                    actor.Health.Receive(new DamagePacket{amount=5,type=DamageType.Kinetic});
                    Assert.AreEqual(revived,actor.Health.integrity,"get-up lacks protection");
                    yield return new WaitForSecondsRealtime(1.6f);
                    Assert.IsTrue(actor.CanAct,"get-up never restored control");
                    Debug.Log($"REVIVE_POSE {actor.character} {zone} before={standing:F3} after={rig.GetBoneTransform(HumanBodyBones.Hips).position.y:F3} clip={rig.GetCurrentAnimatorStateInfo(0).normalizedTime:F3}");
                    Assert.Greater(rig.GetBoneTransform(HumanBodyBones.Hips).position.y,standing-.2f,actor.character+" "+zone+" revive did not return to standing");
                }
            }
        }

        [UnityTest] public IEnumerator LethalResolutionCancelsAlreadyCreatedAndPendingLungeContacts()
        {
            yield return Load("Arena_Flight");
            var actor=PartyController.Current.Active;
            Place(actor,new Vector3(0,1,-15));
            foreach(var target in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))target.Passive=true;
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDart"),new Vector3(0,1,-10));enemy.Passive=true;
            yield return null;
            actor.target=enemy.Health;float before=enemy.Health.integrity;
            Assert.IsTrue(actor.Lunge());GameTime.Paused=true;
            actor.Health.Receive(new DamagePacket{amount=actor.Health.maximum*100,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.1f);
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(.3f);
            Assert.IsNotNull(enemy,"dead owner's delayed lunge killed its target");
            Assert.AreEqual(before,enemy.Health.integrity,"dead owner's delayed lunge still damages");
        }

        [UnityTest] public IEnumerator DisabledFlightCraftRemainsRecoverableOnItsFlightPlane()
        {
            yield return Load("Arena_Flight");
            var party=PartyController.Current;var actor=party.Active;var other=party.members.First(a=>a!=actor);
            float plane=ZoneController.Current.definition.flightPlane;
            Place(actor,new Vector3(0,plane,-15));Place(other,new Vector3(12,plane,-15));
            actor.motor.Move(Vector2.up,true,false);yield return null;
            var visual=actor.GetComponent<FormController>().flight.transform;
            var before=visual.localRotation;
            actor.Health.Receive(new DamagePacket{amount=actor.Health.maximum*100,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(actor.transform.position.y,Is.EqualTo(plane).Within(.03f),"disabled craft fell off its flight plane");
            Assert.That(actor.motor.Velocity.magnitude,Is.LessThan(.01f),"disabled craft retains live thrust velocity");
            Assert.Greater(Quaternion.Angle(before,visual.localRotation),15,"disabled craft lacks a visible failure attitude");
            Assert.IsTrue(visual.gameObject.activeInHierarchy,"recoverable craft disappeared");
            actor.Health.Heal(actor.Health.maximum*.35f);yield return new WaitForSecondsRealtime(1.6f);
            Assert.IsTrue(actor.CanAct);Assert.Less(Quaternion.Angle(Quaternion.identity,visual.localRotation),10,"recovered craft retained its disabled attitude");
        }

        [UnityTest] public IEnumerator BothDownFinishTheirCollapseBeforeTheDefeatMenuPausesPlay()
        {
            foreach(var actor in PartyController.Current.members)
                actor.Health.Receive(new DamagePacket{amount=actor.Health.maximum*100,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(GameTime.Paused,"defeat menu froze both heroes before their collapse could play");
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.IsTrue(GameTime.Paused,"both-down presentation never opened the retry menu");
            Assert.IsTrue(GameServices.Current.Input.Blocked,"defeat must own input");
            Assert.IsNotNull(GameObject.Find("Defeat"),"retry interface is missing");
        }
    }
}
