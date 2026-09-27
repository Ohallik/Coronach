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
    public sealed class EnemyHitTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 position)
        {var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}

        [UnityTest] public IEnumerator RecoilPausesAndYieldsImmediatelyToBreakAndDeath()
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),new Vector3(0,1,-10));enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.25f);
            var rig=enemy.GetComponentInChildren<Animator>();var head=rig.GetBoneTransform(HumanBodyBones.Head);
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=1,type=enemy.definition.weakness});
            yield return new WaitForSecondsRealtime(.13f);
            var held=head.position;GameTime.Paused=true;yield return new WaitForSecondsRealtime(.25f);
            Assert.Less(Vector3.Distance(held,head.position),.005f,"ordinary recoil moves during pause");
            GameTime.Paused=false;
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=1,breakPower=1000,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.12f);
            int layer=rig.GetLayerIndex("Recoil");Assert.GreaterOrEqual(layer,0);
            Assert.AreEqual(0,rig.GetLayerWeight(layer),"ordinary reaction overrides full break");
            Assert.IsTrue(rig.GetCurrentAnimatorStateInfo(0).IsName("Stagger")||rig.IsInTransition(0)&&rig.GetNextAnimatorStateInfo(0).IsName("Stagger"),"break failed to start its full-body pose");
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=10000,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(0,rig.GetLayerWeight(layer),"ordinary reaction overrides death");
            Assert.IsTrue(rig.GetCurrentAnimatorStateInfo(0).IsName("Down"));
        }

        [UnityTest] public IEnumerator MovingRecoilPreservesTheUninjuredLegStride()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(50,.5f,-10);floor.transform.localScale=new Vector3(12,1,12);Physics.SyncTransforms();
            var hit=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),new Vector3(48,1,-10));hit.Passive=true;
            var control=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),new Vector3(52,1,-10));control.Passive=true;
            var rig=hit.GetComponentInChildren<Animator>();var other=control.GetComponentInChildren<Animator>();
            yield return new WaitForSecondsRealtime(.25f);
            float start=Time.time;bool fired=false;float worst=0,headDifference=0,minimum=float.PositiveInfinity;
            while(Time.time-start<.9f)
            {
                hit.GetComponent<CharacterController>().Move(Vector3.forward*2.1f*Time.deltaTime);
                control.GetComponent<CharacterController>().Move(Vector3.forward*2.1f*Time.deltaTime);
                if(!fired&&Time.time-start>.25f)
                {hit.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=1,type=hit.definition.weakness});fired=true;}
                yield return null;
                foreach(var bone in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot})
                    worst=Mathf.Max(worst,Vector3.Distance(hit.transform.InverseTransformPoint(rig.GetBoneTransform(bone).position),control.transform.InverseTransformPoint(other.GetBoneTransform(bone).position)));
                headDifference=Mathf.Max(headDifference,Vector3.Distance(hit.transform.InverseTransformPoint(rig.GetBoneTransform(HumanBodyBones.Head).position),control.transform.InverseTransformPoint(other.GetBoneTransform(HumanBodyBones.Head).position)));
                foreach(var skin in hit.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);var world=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                    foreach(var vertex in mesh.vertices)minimum=Mathf.Min(minimum,world.MultiplyPoint3x4(vertex).y-1);
                    Object.Destroy(mesh);
                }
            }
            Debug.Log($"SENTINEL_MOVING_RECOIL footDifference={worst:F5} headDifference={headDifference:F5} minimum={minimum:F5}");
            Assert.Less(worst,.03f,"upper-body impact changed the matched uninjured leg stride");
            Assert.That(headDifference,Is.InRange(.12f,.6f),"moving body lacks a readable recoil");
            Assert.Greater(minimum,-.03f,"recoil pushes the rendered body through the floor");
            Object.Destroy(floor);
        }

        [UnityTest] public IEnumerator ArmorRemainsVisibleAfterItsInitialBurstAndClearsOnBreak()
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),new Vector3(0,1,-10));enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.25f);
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=1,type=enemy.definition.resistance});
            yield return new WaitForSecondsRealtime(.8f);
            Assert.Greater(enemy.Health.ShieldUntil,Time.time,"fixture shield already expired");
            bool Visible()
            {
                foreach(var particles in enemy.GetComponentsInChildren<ParticleSystem>())
                    if(particles.particleCount>0&&particles.GetComponent<ParticleSystemRenderer>().enabled)return true;
                return false;
            }
            Assert.IsTrue(Visible(),"active armor becomes invisible when its initial burst expires");
            GameTime.Paused=true;yield return new WaitForSecondsRealtime(.3f);Assert.IsTrue(Visible(),"pause erased active armor");GameTime.Paused=false;
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=1,breakPower=1000,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsFalse(Visible(),"broken armor retains its protective visual");
            while(enemy.Health.Broken)yield return null;
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=1,type=enemy.definition.resistance});
            yield return new WaitForSecondsRealtime(.15f);Assert.IsTrue(Visible(),"recovered armor never returns");
            yield return new WaitForSecondsRealtime(2.05f);Assert.IsFalse(Visible(),"expired armor remains visible");
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=1,type=enemy.definition.resistance});
            yield return new WaitForSecondsRealtime(.15f);Assert.IsTrue(Visible());
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=10000,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.1f);Assert.IsFalse(Visible(),"dead body retains active armor");
        }

        [UnityTest] public IEnumerator OrdinarySentinelHitHasAVisibleBoundedRecoil()
        {
            var actor=PartyController.Current.Active;
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),new Vector3(0,1,-10));enemy.Passive=true;
            var armored=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),new Vector3(5,1,-10));armored.Passive=true;
            yield return new WaitForSecondsRealtime(.25f);
            var rig=enemy.GetComponentInChildren<Animator>();var head=rig.GetBoneTransform(HumanBodyBones.Head);
            var armorHead=armored.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
            Vector3 idle=enemy.transform.InverseTransformPoint(head.position),armorIdle=armored.transform.InverseTransformPoint(armorHead.position);
            enemy.Health.Receive(new DamagePacket{source=actor.Health,amount=1,breakPower=0,type=enemy.definition.weakness});
            armored.Health.Receive(new DamagePacket{source=actor.Health,amount=1,breakPower=0,type=armored.definition.resistance});
            Assert.IsFalse(enemy.Health.Broken,"this must test an ordinary hit, not full break");
            float maximum=0,armorMaximum=0,until=Time.unscaledTime+.36f;
            while(Time.unscaledTime<until)
            {
                maximum=Mathf.Max(maximum,Vector3.Distance(idle,enemy.transform.InverseTransformPoint(head.position)));
                armorMaximum=Mathf.Max(armorMaximum,Vector3.Distance(armorIdle,armored.transform.InverseTransformPoint(armorHead.position)));
                yield return null;
            }
            Debug.Log($"SENTINEL_HIT_RECOIL maximum={maximum:F5} armored={armorMaximum:F5}");
            Assert.That(maximum,Is.InRange(.12f,.6f),"ordinary impact lacks a visible bounded body response");
            Assert.That(armorMaximum,Is.InRange(.04f,.2f),"armor needs a smaller bounded body response");
            Assert.Less(armorMaximum,maximum*.75f,"armor and exposed hits have indistinguishable recoil");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.Less(Vector3.Distance(idle,enemy.transform.InverseTransformPoint(head.position)),.10f,"ordinary recoil never recovers");
        }
    }
}
