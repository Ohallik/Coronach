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
    public sealed class GroundBuffTests
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
        [UnityTest] public IEnumerator RefractBlocksOneIncomingHitButItsReturnBeamCannotCrossCover()
        {
            Place(taren,new Vector3(208,0,0));Place(sela,new Vector3(200,0,0));var cover=Cover(2);
            Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            float before=sela.Health.integrity;sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});
            Assert.AreEqual(before,sela.Health.integrity,"armed counter did not intercept the incoming hit");
            yield return new WaitForSecondsRealtime(.65f);
            Assert.AreEqual(1000,victim.Health.integrity,"Refract returned hidden damage through solid cover");
            cover.SetActive(false);sela.cooldowns[3]=0;sela.charge=100;Physics.SyncTransforms();
            Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.65f);
            Assert.Less(victim.Health.integrity,1000,"the same unobstructed counter must reach its attacker");
        }
        [UnityTest] public IEnumerator RefractReturnWaitsForAnActualInterceptionAndThePosedEmitter()
        {
            Place(taren,new Vector3(208,0,0));Place(sela,new Vector3(200,0,0));
            int hits=0;float error=0,delay=0,intercepted=0;bool sawBeam=false;
            victim.Health.Damaged+=(_,__,___)=>
            {
                hits++;delay=GameTime.Now-intercepted;
                foreach(var line in sela.GetComponentsInChildren<LineRenderer>())
                    if(line.name=="Emitter core"&&line.enabled)
                    {sawBeam=true;error=Vector3.Distance(line.GetPosition(0),sela.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.LeftHand).position);}
            };
            Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            Assert.AreEqual(0,hits,"counter fired without an incoming hit");
            intercepted=GameTime.Now;float before=sela.Health.integrity;
            sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});
            Assert.AreEqual(before,sela.Health.integrity);
            Assert.AreEqual("Shoot",sela.VisualAction,"actual interception must start the return-shot pose");
            sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});
            Assert.Less(sela.Health.integrity,before,"a single arm must not intercept two incoming hits");
            yield return new WaitForSecondsRealtime(.7f);
            Assert.AreEqual(1,hits);Assert.IsTrue(sawBeam,"counter damage has no visible emitter path");
            Assert.Less(error,.35f);Assert.That(delay,Is.InRange(.1f,.45f),"return contact must wait for the posed release");
            TestContext.WriteLine($"REFRACT handError={error:F6} releaseDelay={delay:F6}");
        }
        [UnityTest] public IEnumerator ExplicitDodgeReplacesAnArmedRefractWindow()
        {
            Place(taren,new Vector3(208,0,0));Place(sela,new Vector3(200,0,0));
            Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            int flash=sela.FlashMoves;Assert.IsTrue(sela.Dodge(Vector3.right));
            sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});
            Assert.AreEqual(flash+1,sela.FlashMoves,"armed Refract stole the deliberate Flash Move");
            yield return new WaitForSecondsRealtime(.65f);
            Assert.AreEqual(1000,victim.Health.integrity,"evade cancellation left a counter attack");
        }
        ParticleSystem Effect(CombatActor actor,string label)
        {foreach(var p in actor.GetComponentsInChildren<ParticleSystem>(true))if(p.name==label)return p;Assert.Fail("Missing persistent cue "+label);return null;}
        void Cue(CombatActor actor,string label,bool visible,int count=1)
        {
            var effect=Effect(actor,label);Assert.AreEqual(visible,effect.GetComponent<ParticleSystemRenderer>().enabled,label);
            Assert.AreEqual(visible?count:0,effect.particleCount,label+" particle count");
            if(!visible)return;
            var particles=new ParticleSystem.Particle[2];effect.GetParticles(particles);var rig=actor.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>();
            for(int i=0;i<count;i++)Assert.Less(Vector3.Distance(effect.transform.TransformPoint(particles[i].position),rig.GetBoneTransform(i==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand).position),.02f,"persistent cue left the posed hand");
        }
        [UnityTest] public IEnumerator BuffArmingFollowsThePoseAndCancelsBeforeRelease()
        {
            foreach(var actor in new[]{taren,sela})
            {
                string cue=actor==taren?"Overdrive charge":"Refract ward";
                Assert.IsTrue(actor.Skill(3));Assert.IsFalse(actor==taren?actor.Overdriving:actor.Refracting);
                float start=GameTime.Now;yield return new WaitForSecondsRealtime(.09f);
                Assert.IsFalse(actor==taren?actor.Overdriving:actor.Refracting,"buff armed before its posed release");Cue(actor,cue,false);
                actor.Guard(true);yield return new WaitForSecondsRealtime(.65f);actor.Guard(false);
                Assert.IsFalse(actor==taren?actor.Overdriving:actor.Refracting);Cue(actor,cue,false);
                actor.cooldowns[3]=0;actor.charge=100;Assert.IsTrue(actor.Skill(3));start=GameTime.Now;
                while(!(actor==taren?actor.Overdriving:actor.Refracting)&&GameTime.Now-start<.6f)yield return null;
                Assert.IsTrue(actor==taren?actor.Overdriving:actor.Refracting);yield return null;
                Cue(actor,cue,true,actor==taren?2:1);
                float armedDelay=GameTime.Now-start;
                Assert.That(armedDelay,Is.InRange(actor==taren?.22f:.14f,actor==taren?.4f:.32f));
                TestContext.WriteLine($"ARM {actor.character} delay={armedDelay:F6}");
            }
        }
        [UnityTest] public IEnumerator OverdriveCueAndTwentyPercentBonusShareExpiryPauseAndDownState()
        {
            // Party stat refresh restores equipped fortune between samples.
            // Suppress only the critical bonus for each synchronous measurement
            // so this test isolates Overdrive without rerolling or relaxing it.
            float Sample(){taren.fortune=0;return taren.Packet(100,DamageType.Pulse,0).amount;}
            float baseline=Sample();Assert.AreEqual(100,baseline,.001f);
            Assert.IsTrue(taren.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            Assert.IsTrue(taren.Overdriving);Cue(taren,"Overdrive charge",true,2);
            Assert.AreEqual(baseline*1.2f,Sample(),.001f);
            GameTime.Paused=true;yield return new WaitForSecondsRealtime(.7f);Assert.IsTrue(taren.Overdriving);Cue(taren,"Overdrive charge",true,2);GameTime.Paused=false;
            yield return new WaitForSecondsRealtime(9.7f);yield return null;
            Assert.IsFalse(taren.Overdriving);Cue(taren,"Overdrive charge",false);Assert.AreEqual(baseline,Sample(),.001f);
            taren.cooldowns[3]=0;taren.charge=100;Assert.IsTrue(taren.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            taren.Health.Receive(new DamagePacket{source=victim.Health,amount=9999,type=DamageType.Pulse});yield return null;yield return null;
            Assert.IsFalse(taren.Health.Alive);Cue(taren,"Overdrive charge",false);
            SceneManager.LoadScene("_Boot");yield return null;Assert.IsTrue(taren==null,"scene unload retained buff actor");
        }
        [UnityTest] public IEnumerator RefractWardHoldsThroughPauseAndEndsWithTheRealWindow()
        {
            Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);Cue(sela,"Refract ward",true);
            GameTime.Paused=true;yield return new WaitForSecondsRealtime(1.5f);Assert.IsTrue(sela.Refracting);Cue(sela,"Refract ward",true);GameTime.Paused=false;
            yield return new WaitForSecondsRealtime(1);yield return null;Assert.IsFalse(sela.Refracting);Cue(sela,"Refract ward",false);
            sela.cooldowns[3]=0;sela.charge=100;Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);sela.Guard(true);
            int flash=sela.FlashGuards;sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});
            Assert.AreEqual(flash+1,sela.FlashGuards);yield return null;yield return null;Cue(sela,"Refract ward",false);sela.Guard(false);
        }
        [UnityTest] public IEnumerator PendingCounterPausesAndCancelsButReleasedBeamFinishes()
        {
            Place(taren,new Vector3(208,0,0));Place(sela,new Vector3(200,0,0));
            Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});
            GameTime.Paused=true;yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual(1000,victim.Health.integrity);GameTime.Paused=false;
            sela.Stagger(.3f);yield return new WaitForSecondsRealtime(.6f);Assert.AreEqual(1000,victim.Health.integrity);
            sela.cooldowns[3]=0;sela.charge=100;Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});
            float deadline=GameTime.Now+.5f;while(victim.Health.integrity==1000&&GameTime.Now<deadline)yield return null;
            Assert.Less(victim.Health.integrity,1000);sela.Stagger(.3f);GameTime.Paused=true;
            yield return new WaitForSecondsRealtime(.3f);bool visible=false;foreach(var line in sela.GetComponentsInChildren<LineRenderer>())if(line.name=="Emitter core")visible=line.enabled;
            Assert.IsTrue(visible,"released beam vanished during pause/cancel");GameTime.Paused=false;
            yield return new WaitForSecondsRealtime(.3f);foreach(var line in sela.GetComponentsInChildren<LineRenderer>())if(line.name=="Emitter core")Assert.IsFalse(line.enabled);
        }
        [UnityTest] public IEnumerator RefractReturnStopsAtMovableAndNearCover()
        {
            Place(taren,new Vector3(208,0,0));Place(sela,new Vector3(200,0,0));
            foreach(float distance in new[]{2f,.38f,.45f,.65f})
            {
                var cover=Cover(distance);cover.isStatic=false;sela.cooldowns[3]=0;sela.charge=100;
                Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);
                sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});yield return new WaitForSecondsRealtime(.65f);
                Assert.AreEqual(1000,victim.Health.integrity,"solid cover was ignored at "+distance);
                cover.SetActive(false);Physics.SyncTransforms();
            }
            sela.cooldowns[3]=0;sela.charge=100;Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            sela.Health.Receive(new DamagePacket{source=victim.Health,amount=10,type=DamageType.Pulse});yield return new WaitForSecondsRealtime(.65f);
            Assert.Less(victim.Health.integrity,1000,"unobstructed positive control did not hit");
        }
    }
}
