using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>What a hit does beyond its damage (S1 Impact): weights on real attacks and none on a packet built by hand, one beat a swing, a budget for the clock, a kick that
    /// stays in the camera's plane, a body that flashes and gives without losing what else is written on it, a shove that never costs an attack, and the sounds and the
    /// screen's edge that say what happened. Each fixture plays at the defaults, whatever this machine's own settings are.</summary>
    public sealed class ImpactTests
    {
        Gamepad pad;
        static CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static readonly int FlashId=Shader.PropertyToID("_HitFlash"),FadeId=Shader.PropertyToID("_OccFade");
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();ImpactSettings.Reset();ImpactSettings.Volatile=true;
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);pad=null;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();ImpactSettings.Reset();
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        /// <summary>Open floor far from the arena's walls.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Impact fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(actor.motor is GroundMotor ground)ground.KeepFacing(Vector3.forward);
            Physics.SyncTransforms();
        }
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        /// <summary>A hound that never dies or breaks, to which every type is plain.</summary>
        static EnemyBrain Dummy(Vector3 at)
        {
            var enemy=Enemy("Ridgehound",at);var body=enemy.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;
            body.weakness=body.resistance=DamageType.Kinetic;return enemy;
        }
        static DamagePacket Blow(CombatActor from,HitWeight weight,float amount=10)=>new DamagePacket{source=from.Health,amount=amount,type=DamageType.Plasma,weight=weight};
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Settled(){for(float end=Time.unscaledTime+2;GameTime.Stopped&&Time.unscaledTime<end;)yield return null;}
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            return party.Active;
        }
        static bool Sparks()=>Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.name=="Lattice particles"&&p.IsAlive());

        // ------------------------------------------------------------------ weights
        [UnityTest] public IEnumerator RealAttacksCarryTheirWeightAndAHandBuiltPacketCarriesNone()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var dummy=Dummy(new Vector3(200,0,1.5f));yield return Frames(2);taren.target=dummy.Health;
            var seen=new List<(Health from,HitWeight weight)>();
            System.Action<Health,DamagePacket,float,HitKind,bool,bool> record=(victim,packet,_,__,___,____)=>{if(victim==dummy.Health)seen.Add((packet.source,packet.weight));};
            Health.Impacted+=record;
            try
            {
                for(int swing=0;swing<3;swing++)
                {
                    float end=Time.unscaledTime+2;while(!taren.Attack()&&Time.unscaledTime<end)yield return null;
                }
                yield return new WaitForSecondsRealtime(.9f);
                var cuts=seen.Where(s=>s.from==taren.Health).Select(s=>s.weight).ToArray();
                Assert.AreEqual(3,cuts.Length,"fixture: all three cuts reach the body");
                CollectionAssert.AreEqual(new[]{HitWeight.Light,HitWeight.Light,HitWeight.Finisher},cuts,"Taren's three cuts land as two light hits and a finisher");
                // Taren steps out of her line of fire.
                var sela=Lead("Sela");Place(sela,new Vector3(200,0,-4));Place(Taren,new Vector3(260,0,0));yield return Frames(2);sela.target=dummy.Health;seen.Clear();
                for(float end=Time.unscaledTime+2;!sela.Attack()&&Time.unscaledTime<end;)yield return null;
                for(float end=Time.unscaledTime+2;!seen.Any(s=>s.from==sela.Health)&&Time.unscaledTime<end;)yield return null;
                Assert.IsTrue(seen.Any(s=>s.from==sela.Health),"fixture: Sela's needle never reached the body");
                Assert.AreEqual(HitWeight.Tick,seen.First(s=>s.from==sela.Health).weight,"a needle is one of a stream: it marks the body and holds nothing");
            }
            finally{Health.Impacted-=record;}
            Assert.AreEqual(HitWeight.Heavy,taren.Packet(10,DamageType.Kinetic,35).weight,"a skill that breaks hard lands hard without being told to");
            Assert.AreEqual(HitWeight.Medium,taren.Packet(10,DamageType.Kinetic,25).weight);
            Assert.AreEqual(HitWeight.Light,taren.Packet(10,DamageType.Kinetic,5).weight);
            Assert.AreEqual(HitWeight.Tick,taren.Packet(10,DamageType.Kinetic,60,null,HitWeight.Tick).weight,"a weight given is the weight it has");
            Assert.AreEqual(HitWeight.None,new DamagePacket{source=taren.Health,amount=10}.weight);
            Assert.IsFalse(Impact.Answers(new DamagePacket{source=taren.Health,amount=10}),"a packet built by hand is answered: every fixture's hit would hold the clock");
        }
        [UnityTest] public IEnumerator AHandBuiltPacketChangesNothingButIntegrity()
        {
            // The body is given time to come down onto the floor first: a controller made at 0 settles by its own skin over its first frames, hit or no hit.
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));var dummy=Dummy(new Vector3(200,0,3));yield return new WaitForSecondsRealtime(.5f);
            var impact=Impact.Current;var rig=Object.FindFirstObjectByType<CameraRig>();var flash=dummy.GetComponent<HitFlash>();
            Assert.IsNotNull(impact,"fixture: the zone has an Impact");Assert.IsNotNull(flash,"fixture: the body has a HitFlash");
            int stops=impact.Stops;float before=dummy.Health.integrity;var at=dummy.transform.position;
            dummy.Health.Receive(new DamagePacket{source=taren.Health,amount=100,type=DamageType.Plasma,breakPower=10});
            Assert.Less(dummy.Health.integrity,before,"fixture: the hit landed");
            Assert.AreEqual(1,Time.timeScale,"a hand-built hit held the clock");Assert.IsFalse(GameTime.Stopped);
            yield return null;yield return null;
            Assert.AreEqual(stops,impact.Stops);
            Assert.AreEqual(0,flash.Shown,"a hand-built hit flashed the body");Assert.IsFalse(flash.Posed,"a hand-built hit set the body back");
            Assert.AreEqual(0,rig.ShakeShare.magnitude,"a hand-built hit kicked the camera");
            Assert.Less(Vector3.Distance(at,dummy.transform.position),.001f,"a hand-built hit moved the body");
            Assert.IsTrue(Sparks(),"a hit with no weight lost the burst it always had");
        }

        // ------------------------------------------------------------------ the clock
        [UnityTest] public IEnumerator OneSwingThroughThreeBodiesHoldsTheFightOnce()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            // Three bodies a hound's size standing in one place, the place where the first cut is known to reach one (1.5 m dead ahead): whatever the edge's path, it is
            // in all three at once. Set side by side they were a guess at the cut's arc, and it passed through one. They are shapes with integrity and nothing else,
            // so they can share the spot without shouldering each other off it.
            var plain=taren.edgeType==DamageType.Pulse?DamageType.Beam:DamageType.Pulse;
            for(int i=0;i<3;i++)
            {
                var body=new GameObject("Impact fixture body",typeof(CapsuleCollider),typeof(Health),typeof(Hurtbox));body.transform.position=new Vector3(200,0,1.5f);
                var shape=body.GetComponent<CapsuleCollider>();shape.isTrigger=true;shape.radius=.55f;shape.height=1.5f;shape.center=Vector3.up*.75f;
                var health=body.GetComponent<Health>();health.maximum=health.integrity=1000000;health.breakThreshold=1e9f;health.weakness=health.resistance=plain;
                body.GetComponent<Hurtbox>().owner=health;
            }
            Physics.SyncTransforms();yield return Frames(2);
            var struck=new HashSet<Health>();bool crit=false;
            System.Action<Health,DamagePacket,float,HitKind,bool,bool> record=(victim,packet,_,__,___,____)=>{if(packet.source==taren.Health){struck.Add(victim);crit|=packet.isCrit;}};
            Health.Impacted+=record;
            var impact=Impact.Current;int stops=impact.Stops;float held=impact.Held;
            try
            {
                Assert.IsTrue(taren.Attack(),"fixture: a swing");
                yield return new WaitForSecondsRealtime(.8f);
            }
            finally{Health.Impacted-=record;}
            Assert.GreaterOrEqual(struck.Count,2,"fixture: the blade passed through "+struck.Count+" of the three bodies");
            var beat=Impact.Tune.Dealt[(int)HitWeight.Light];float most=beat.stop+(crit?Impact.Tune.CritStop*.001f:0);
            Assert.Greater(impact.Held-held,0,"a swing that landed held nothing");
            Assert.LessOrEqual(impact.Held-held,most+.0005f,$"one swing through {struck.Count} bodies held the fight for {impact.Held-held:0.000} s: each body added its own stop");
            if(!crit)Assert.AreEqual(1,impact.Stops-stops,"one swing was more than one stop");
            Assert.AreEqual(1,Time.timeScale,"the clock doesn't come back after the swing");
        }
        [UnityTest] public IEnumerator StopsKeepToTheirBudgetAndAnOffSettingHoldsNothing()
        {
            var impact=Impact.Current;float held=impact.Held;
            for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end;){impact.Hold(.2f);yield return null;}
            float budget=Impact.Tune.BudgetWindow*Impact.Tune.BudgetShare;
            Assert.LessOrEqual(impact.Held-held,budget+.0005f,$"a second and a half of heavy hits held the game for {impact.Held-held:0.00} s: a mash is slow motion");
            Assert.Greater(impact.Held-held,.19f,"the first heavy hit of a fight was refused its stop");
            yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(1,Time.timeScale,"the clock doesn't come back once the budget is spent");
            // The window moves on and the next hit has its beat again.
            yield return new WaitForSecondsRealtime(Impact.Tune.BudgetWindow);
            Assert.Greater(impact.Hold(.05f),0,"a spent budget never comes back");
            yield return Settled();
            // A boss going down is held whatever is left.
            held=impact.Held;impact.Hold(.2f);impact.Hold(.2f);yield return Settled();impact.Hold(.2f);yield return Settled();
            Assert.Greater(impact.Hold(.25f,true),.2f,"a boss's last beat was cut by the hits that led up to it");
            yield return Settled();
            ImpactSettings.HitStop=false;held=impact.Held;
            Assert.AreEqual(0,impact.Hold(.2f,true),"hit pause is off and the game still holds");
            Assert.AreEqual(1,Time.timeScale);Assert.AreEqual(held,impact.Held);
        }
        [UnityTest] public IEnumerator APartnersHitMarksTheBodyAndHoldsNothingAndTheHeldHerosHoldsTheFight()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(203,0,0));
            var dummy=Dummy(new Vector3(200,0,3));yield return Frames(2);
            var impact=Impact.Current;var rig=Object.FindFirstObjectByType<CameraRig>();var flash=dummy.GetComponent<HitFlash>();int stops=impact.Stops;
            dummy.Health.Receive(Blow(Sela,HitWeight.Heavy));
            Assert.AreEqual(1,Time.timeScale,"the partner's hit held the player's game");
            yield return null;
            Assert.AreEqual(stops,impact.Stops);Assert.AreEqual(0,rig.ShakeShare.magnitude,"the partner's hit kicked the player's camera");
            Assert.Greater(flash.Shown,0,"a body the partner struck shows nothing");
            yield return new WaitForSecondsRealtime(.4f);
            dummy.Health.Receive(Blow(taren,HitWeight.Heavy));
            Assert.AreEqual(0,Time.timeScale,"the held hero's heavy hit has no stop");Assert.AreEqual(stops+1,impact.Stops);
            float share=0;for(int frame=0;frame<3;frame++){yield return null;share=Mathf.Max(share,rig.ShakeShare.magnitude);}
            Assert.Greater(share,0,"the held hero's heavy hit didn't move the camera");
            yield return Settled();
            // With the pause turned off the body still answers.
            ImpactSettings.HitStop=false;yield return new WaitForSecondsRealtime(.3f);
            dummy.Health.Receive(Blow(taren,HitWeight.Heavy));
            Assert.AreEqual(1,Time.timeScale,"hit pause is off and a heavy hit still holds the game");
            yield return null;Assert.Greater(flash.Shown,0,"with hit pause off the body lost its flash as well");
        }

        // ------------------------------------------------------------------ the camera
        [UnityTest] public IEnumerator TheKickStaysInTheCamerasPlaneFollowsTheSettingAndHoldsForAPause()
        {
            var rig=Object.FindFirstObjectByType<CameraRig>();yield return new WaitForSecondsRealtime(.3f);
            var angle=Quaternion.Euler(rig.profile.pitch,rig.profile.yaw,0);
            rig.Impulse(Vector3.right,.01f,.3f);yield return null;
            Assert.AreEqual(.01f,rig.ShakeShare.magnitude,.0002f,"a kick's first frame isn't its size");
            float most=0;
            for(float end=Time.unscaledTime+.3f;Time.unscaledTime<end;)
            {
                most=Mathf.Max(most,rig.ShakeShare.magnitude);
                Assert.Less(Mathf.Abs(Vector3.Dot(rig.ShakeOffset,angle*Vector3.forward)),.0001f,"the kick moves the camera along its own line of sight: the framing's distance changes");
                Assert.Less(Quaternion.Angle(angle,Camera.main.transform.rotation),.01f,"the kick turns the camera: the fixed angle is the one thing it never changes");
                Assert.LessOrEqual(rig.ShakeShare.magnitude,CameraRig.MostShake+.0001f);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(0,rig.ShakeShare.magnitude,.00001f,"a kick never dies away");
            rig.Impulse(Vector3.forward,5,.2f);yield return null;
            Assert.AreEqual(CameraRig.MostShake,rig.ShakeShare.magnitude,.0002f,"nothing limits how far a kick throws the view");
            yield return new WaitForSecondsRealtime(.4f);
            ImpactSettings.Shake=ImpactSettings.ShakeLevel.Reduced;rig.Impulse(Vector3.right,.01f,.2f);yield return null;
            Assert.AreEqual(.005f,rig.ShakeShare.magnitude,.0002f,"Reduced isn't half");
            yield return new WaitForSecondsRealtime(.4f);
            ImpactSettings.Shake=ImpactSettings.ShakeLevel.Off;rig.Impulse(Vector3.right,.01f,.3f);rig.Tremor(.3f,.1f);
            for(int frame=0;frame<6;frame++){yield return null;Assert.AreEqual(0,rig.ShakeOffset.magnitude,"screen shake is off and the camera still moves");}
            yield return new WaitForSecondsRealtime(.4f);
            // A pause holds a kick where it was; it used to ring on behind the menu.
            ImpactSettings.Shake=ImpactSettings.ShakeLevel.Full;rig.Impulse(Vector3.right,.012f,.6f);yield return null;yield return null;
            GameTime.Paused=true;yield return null;var held=rig.ShakeOffset;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.Less(Vector3.Distance(held,rig.ShakeOffset),.00001f,"a kick rings on through a pause");
            GameTime.Paused=false;
        }

        // ------------------------------------------------------------------ the held hero is struck
        [UnityTest] public IEnumerator AHitOnTheHeldHeroReddensTheScreensEdgeAndLowIntegrityPulsesInAFight()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(204,0,0));
            var enemy=Enemy("SentinelHusk",new Vector3(200,0,6));yield return Frames(3);
            var edge=Object.FindFirstObjectByType<ImpactHud>();Assert.IsNotNull(edge,"fixture: the zone has an ImpactHud");
            taren.Health.Receive(new DamagePacket{source=enemy.Health,amount=taren.Health.maximum*.1f});
            yield return null;yield return null;
            Assert.AreEqual(0,edge.Hurt,"a hand-built hit reddened the screen");
            Sela.Health.Receive(new DamagePacket{source=enemy.Health,amount=Sela.Health.maximum*.2f,weight=HitWeight.Light});
            yield return null;yield return null;
            Assert.AreEqual(0,edge.Hurt,"the partner's hit reddened the player's screen");Assert.AreEqual(1,Time.timeScale,"the partner's hit held the player's game");
            int stops=Impact.Current.Stops;
            taren.Health.Receive(new DamagePacket{source=enemy.Health,amount=taren.Health.maximum*.2f,weight=HitWeight.Light});
            Assert.AreEqual(stops+1,Impact.Current.Stops,"a fifth of the hero's integrity in one hit has no stop");
            yield return null;yield return null;
            Assert.Greater(edge.Hurt,.3f,"a heavy hit on the held hero leaves the screen's edge as it was");
            Assert.IsTrue(taren.GetComponent<HitFlash>().Shown>0,"the hero's body shows nothing");
            yield return Settled();yield return new WaitForSecondsRealtime(Impact.Tune.VignetteSeconds+.3f);
            Assert.AreEqual(0,edge.Hurt,.01f,"the edge stays red after the hit has passed");
            // Under three tenths, with something awake nearby: a slow pulse and no hit. Out of the fight it stops.
            taren.Health.integrity=taren.Health.maximum*.2f;taren.Health.InvulnerableUntil=Sela.Health.InvulnerableUntil=GameTime.Now+30;
            enemy.Passive=false;
            float most=0;for(float end=Time.unscaledTime+1.6f;Time.unscaledTime<end;){most=Mathf.Max(most,edge.Hurt);yield return null;}
            Assert.Greater(most,.05f,"a hero fighting on at a fifth of their integrity is told nothing");
            Assert.LessOrEqual(most,Impact.Tune.LowPulse+.01f);
            enemy.Passive=true;foreach(var mark in Telegraph.Live.ToArray())if(mark!=null)mark.Clear();
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.AreEqual(0,edge.Hurt,.01f,"the warning pulses on with nothing awake to fight");
        }
        [UnityTest] public IEnumerator AStruckHeroFlinchesAndIsNeverMoved()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var enemy=Enemy("SentinelHusk",new Vector3(200,0,6));yield return new WaitForSecondsRealtime(.8f);
            var rig=taren.GetComponentInChildren<Animator>();var head=rig.GetBoneTransform(HumanBodyBones.Head);
            var rest=taren.transform.InverseTransformPoint(head.position);var at=taren.transform.position;
            taren.Health.Receive(new DamagePacket{source=enemy.Health,amount=taren.Health.maximum*.2f,weight=HitWeight.Light});
            // Read on the next frame before its pose is evaluated: the bones still hold what the last frame's LateUpdate left on them.
            float most=0;
            for(float end=Time.unscaledTime+.3f;Time.unscaledTime<end;){yield return null;most=Mathf.Max(most,Vector3.Distance(rest,taren.transform.InverseTransformPoint(head.position)));}
            Assert.Greater(most,.06f,$"a heavy hit moved the hero's head {most:0.000} m: the body takes it without a sign");
            Assert.Less(Vector3.Distance(at,taren.transform.position),.01f,"a flinch moved the hero: it may bend the body and nothing else");
            Assert.AreNotEqual(ActorState.Stagger,taren.State,"an ordinary hit took the hero's control away");
            yield return Settled();yield return new WaitForSecondsRealtime(Impact.Tune.FlinchSeconds+.3f);yield return null;
            Assert.Less(Vector3.Distance(rest,taren.transform.InverseTransformPoint(head.position)),.06f,"the hero never straightens up again");
        }
        [UnityTest] public IEnumerator AStruckHullRocksAndComesBack()
        {
            yield return Zone("Arena_Flight");
            var taren=Lead("Taren");var enemy=Enemy("ChoristerDart",taren.transform.position+Vector3.right*8);yield return new WaitForSecondsRealtime(.5f);
            var hull=taren.GetComponentInChildren<FlightShipMotion>();Assert.IsNotNull(hull,"fixture: the flight hull");
            var rest=hull.transform.localRotation;
            taren.Health.Receive(new DamagePacket{source=enemy.Health,amount=taren.Health.maximum*.2f,weight=HitWeight.Light});
            float most=0;
            for(float end=Time.unscaledTime+.25f;Time.unscaledTime<end;){yield return null;most=Mathf.Max(most,Quaternion.Angle(rest,hull.transform.localRotation));}
            Assert.Greater(most,4,$"a heavy shot rocked the hull {most:0.0} degrees: the craft flies on as if nothing touched it");
            yield return Settled();yield return new WaitForSecondsRealtime(Impact.Tune.JoltSeconds+.4f);yield return null;
            Assert.Less(Quaternion.Angle(rest,hull.transform.localRotation),1.5f,"the hull never levels again");
        }

        // ------------------------------------------------------------------ the body struck
        [UnityTest] public IEnumerator ABlowCarriesALightBodyBackAndNeverOneThatHasMarkedAnAttack()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,60));
            taren.Health.InvulnerableUntil=Sela.Health.InvulnerableUntil=GameTime.Now+60;
            var hound=Enemy("Ridgehound",new Vector3(200,0,10));var body=hound.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;
            body.weakness=body.resistance=DamageType.Kinetic;
            yield return new WaitForSecondsRealtime(.4f);hound.Passive=false;
            // Opened, as a broken body is: awake, and standing where it is instead of walking at the hero or home, so what moves it is the blow.
            body.Expose(1.5f);yield return Frames(2);
            Assert.IsTrue(hound.Shovable,"fixture: an awake hound with no attack marked");
            var from=hound.transform.position;
            body.Receive(taren.Packet(5,DamageType.Plasma,20));
            yield return Settled();yield return new WaitForSecondsRealtime(Impact.Tune.KnockbackSeconds+.2f);
            var moved=hound.transform.position-from;moved.y=0;
            Assert.AreEqual(Impact.Tune.Knockback[(int)HitWeight.Light],moved.magnitude,.08f,$"a light blow carried the hound {moved.magnitude:0.00} m");
            Assert.Greater(Vector3.Dot(moved.normalized,Vector3.forward),.9f,"the hound was carried some other way than along the blow");
            // A sleeping body keeps its place.
            hound.Passive=true;from=hound.transform.position;
            body.Receive(taren.Packet(5,DamageType.Plasma,20,null,HitWeight.Finisher));
            yield return Settled();yield return new WaitForSecondsRealtime(.3f);
            Assert.Less(Vector3.Distance(from,hound.transform.position),.01f,"a body that isn't in the fight was shoved");
            // A heavy body isn't carried at all.
            var husk=Enemy("SentinelHusk",new Vector3(210,0,45));husk.Health.maximum=husk.Health.integrity=1000000;yield return Frames(3);husk.Passive=false;yield return Frames(2);
            Assert.IsFalse(husk.Shovable,"a Sentinel can be shoved about");husk.Passive=true;
            // Its attack marked, nothing moves it and nothing takes the attack away. It comes out of its opening first, and waits out the rest it took in it.
            body.ResetFull();for(float end=Time.unscaledTime+3;body.Broken&&Time.unscaledTime<end;)yield return null;
            Place(taren,hound.transform.position-Vector3.forward*4);hound.Passive=false;
            for(float end=Time.unscaledTime+8;!hound.Telegraphing&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(hound.Telegraphing,"fixture: the hound never marked an attack");
            Assert.IsFalse(hound.Shovable);from=hound.transform.position;
            body.Receive(taren.Packet(5,DamageType.Plasma,30,null,HitWeight.Finisher));
            float drift=0;
            for(float end=Time.unscaledTime+3;hound.Telegraphing&&Time.unscaledTime<end;){drift=Mathf.Max(drift,Vector3.Distance(from,hound.transform.position));yield return null;}
            Assert.Less(drift,.02f,$"a finisher carried a hound {drift:0.00} m off the lane it had marked");
            Assert.IsTrue(hound.Attacking,"a blow took a marked attack away: a three-hit combo would cancel everything a hound began");
        }
        [UnityTest] public IEnumerator AFlashLeavesEveryOtherPropertyOnTheRendererAsItWas()
        {
            Arena();var shared=Dummy(new Vector3(200,0,3));var bare=Dummy(new Vector3(204,0,3));yield return Frames(3);
            static Renderer[] Toon(EnemyBrain body)=>body.GetComponentsInChildren<Renderer>().Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.sharedMaterial!=null&&r.sharedMaterial.HasProperty(FlashId)).ToArray();
            var mine=Toon(shared);var theirs=Toon(bare);
            Assert.Greater(mine.Length,0,"fixture: a body drawn with the toon shader's flash");Assert.Greater(theirs.Length,0);
            var block=new MaterialPropertyBlock();
            // Somebody else's value on the same renderers, as a boss's door or a rig's dying lamps would be.
            foreach(var body in mine){body.GetPropertyBlock(block);block.SetFloat(FadeId,.37f);body.SetPropertyBlock(block);}
            foreach(var body in theirs)Assert.IsFalse(body.HasPropertyBlock(),"fixture: a body nobody has written on");
            shared.GetComponent<HitFlash>().Struck(HitWeight.Heavy,Vector3.forward,Color.white,false,1,0);
            bare.GetComponent<HitFlash>().Struck(HitWeight.Heavy,Vector3.forward,Color.white,false,1,0);
            yield return null;
            foreach(var body in mine)
            {
                body.GetPropertyBlock(block);
                Assert.Greater(block.GetColor(FlashId).a,.2f,"a struck body doesn't flash");
                Assert.AreEqual(.37f,block.GetFloat(FadeId),.0001f,"the flash wrote over what else was on the renderer");
            }
            foreach(var body in theirs){body.GetPropertyBlock(block);Assert.Greater(block.GetColor(FlashId).a,.2f);}
            Assert.Greater(shared.GetComponent<HitFlash>().Shown,0);
            ImpactSettings.FullFlashes=false;yield return null;
            Assert.LessOrEqual(shared.GetComponent<HitFlash>().Shown,Impact.Tune.Flash[(int)HitWeight.Heavy]*.4f+.001f,"Reduced flashes are as bright as full ones");
            yield return new WaitForSecondsRealtime(Impact.Tune.FlashSeconds+.2f);
            foreach(var body in mine)
            {
                Assert.IsTrue(body.HasPropertyBlock(),"the flash took the renderer's other properties away with it");
                body.GetPropertyBlock(block);
                Assert.AreEqual(0,block.GetColor(FlashId).a,"the flash never leaves the body");
                Assert.AreEqual(.37f,block.GetFloat(FadeId),.0001f,"the flash's end reset what else was on the renderer");
            }
            foreach(var body in theirs)Assert.IsFalse(body.HasPropertyBlock(),"a body nobody else wrote on keeps a property block after its flash: it no longer batches as it did");
            Assert.AreEqual(0,shared.GetComponent<HitFlash>().Shown);
        }
        [UnityTest] public IEnumerator AnUnriggedBodyIsPressedAndSetBackAndHandedBackAsItWas()
        {
            // A rigged body's drawn root is not still: its ground support (GroundLifecycleSupport) reseats it on the floor every frame as its idle breathes. So a struck Husk is
            // judged against one made in the same frame and left alone, as the recoil's own tests judge a stride (EnemyHitTests), and not against where it stood a moment ago.
            Arena();var hound=Dummy(new Vector3(200,0,3));var husk=Enemy("SentinelHusk",new Vector3(206,0,3));var untouched=Enemy("SentinelHusk",new Vector3(212,0,3));
            yield return new WaitForSecondsRealtime(.4f);
            var visual=hound.GetComponent<DefeatPresentation>().visual;var huskVisual=husk.GetComponent<DefeatPresentation>().visual;var control=untouched.GetComponent<DefeatPresentation>().visual;
            Vector3 at=visual.localPosition,scale=visual.localScale;
            Assert.Less(Vector3.Distance(control.localPosition,huskVisual.localPosition),.0001f,"fixture: two Husks made together stand alike");
            var flash=hound.GetComponent<HitFlash>();
            flash.Struck(HitWeight.Heavy,Vector3.forward,Color.white,true,1,0);
            husk.GetComponent<HitFlash>().Struck(HitWeight.Heavy,Vector3.forward,Color.white,true,1,0);
            yield return null;
            Assert.IsTrue(flash.Posed,"a struck hound stands as it stood");
            float back=Vector3.Distance(visual.parent.TransformPoint(at),visual.position);
            Assert.That(back,Is.InRange(.05f,Impact.Tune.Recoil[(int)HitWeight.Heavy]+.01f),"a heavy blow set the hound's body back "+back.ToString("0.00")+" m");
            Assert.Less(visual.localScale.y,scale.y,"a heavy blow doesn't press the body");
            Assert.Less(Vector3.Distance(hound.transform.position,new Vector3(200,hound.transform.position.y,3)),.01f,"the blow moved the body itself: only what is drawn gives");
            // A rigged fighter has an authored torso recoil, and its legs are to be left alone (D095).
            Assert.IsFalse(husk.GetComponent<HitFlash>().Posed,"a rigged body's root was moved under its own recoil");
            Assert.Less(Vector3.Distance(control.localPosition,huskVisual.localPosition),.0001f,"a blow moved a rigged body's drawn root");
            Assert.Less(Vector3.Distance(control.localScale,huskVisual.localScale),.0001f,"a blow pressed a rigged body");
            yield return new WaitForSecondsRealtime(Impact.Tune.PoseSeconds+.2f);
            Assert.IsFalse(flash.Posed);
            Assert.Less(Vector3.Distance(control.localPosition,huskVisual.localPosition),.0001f,"a rigged body's drawn root was moved in the frames after a blow");
            Assert.Less(Vector3.Distance(at,visual.localPosition),.0001f,"the body never comes back from a blow");
            Assert.Less(Vector3.Distance(scale,visual.localScale),.0001f,"the body stays pressed");
            // Struck and then killed while still pressed: the defeat measures a body at rest.
            var dying=Enemy("Ridgehound",new Vector3(218,0,3));yield return new WaitForSecondsRealtime(.3f);
            var body=dying.GetComponent<DefeatPresentation>().visual;var restScale=body.localScale;
            dying.GetComponent<HitFlash>().Struck(HitWeight.Finisher,Vector3.forward,Color.white,true,1,0);yield return null;
            Assert.IsTrue(dying.GetComponent<HitFlash>().Posed,"fixture: pressed when the killing blow lands");
            dying.Health.Receive(new DamagePacket{amount=dying.Health.maximum*10,type=DamageType.Pulse});
            Assert.IsFalse(dying.Health.Alive,"fixture: the killing blow");
            Assert.Less(Vector3.Distance(restScale,body.localScale),.0001f,"a body killed mid-blow lies down squashed for good");
            Assert.IsFalse(dying.GetComponent<HitFlash>().Posed);
        }

        // ------------------------------------------------------------------ what is said about it
        [UnityTest] public IEnumerator ACritsNumberSaysSo()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));var plain=Dummy(new Vector3(198,0,4));var struck=Dummy(new Vector3(202,0,4));yield return Frames(3);
            static TMP_Text Newest()=>Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<TMP_Text>(true).Last(t=>t.name=="Damage");
            plain.Health.Receive(new DamagePacket{source=taren.Health,amount=50,type=DamageType.Plasma});
            var ordinary=Newest();float size=ordinary.fontSize;var colour=ordinary.color;
            Assert.AreEqual(FontStyles.Normal,ordinary.fontStyle&(FontStyles.Italic|FontStyles.Bold),"fixture: an ordinary number is upright");
            struck.Health.Receive(new DamagePacket{source=taren.Health,amount=50,type=DamageType.Plasma,isCrit=true});
            var crit=Newest();Assert.AreNotSame(ordinary,crit,"fixture: the crit made its own number");
            Assert.Greater(crit.fontSize,size,"a crit's number is the size of any other");
            Assert.AreNotEqual((FontStyles)0,crit.fontStyle&FontStyles.Italic,"a crit's number stands as upright as any other");
            Assert.AreNotEqual(colour,crit.color,"a crit's number is the colour of any other");
            yield return null;
        }
        [UnityTest] public IEnumerator HeavyHitsCritsBreaksKillsAndGuardsHaveTheirOwnSounds()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var a=Dummy(new Vector3(196,0,4));var b=Dummy(new Vector3(199,0,4));var c=Dummy(new Vector3(202,0,4));c.Health.breakThreshold=10;
            var d=Enemy("Ridgehound",new Vector3(205,0,4));var foe=Enemy("SentinelHusk",new Vector3(200,0,8));yield return Frames(3);
            var cues=new List<string>();System.Action<string,Transform> record=(key,_)=>cues.Add(key);AudioManager.CueStarted+=record;
            int Count(string family)=>cues.Count(key=>key==family||key.StartsWith(family+"_"));
            try
            {
                a.Health.Receive(new DamagePacket{source=taren.Health,amount=10,type=DamageType.Plasma});yield return null;
                Assert.AreEqual(1,Count("hit_soft"),"fixture: a plain hit's impact: "+string.Join(", ",cues));
                Assert.AreEqual(0,Count("thump")+Count("crit")+Count("break")+Count("kill"),"a hand-built hit has an accent: "+string.Join(", ",cues));
                yield return Settled();yield return new WaitForSecondsRealtime(.2f);cues.Clear();
                a.Health.Receive(Blow(taren,HitWeight.Heavy));yield return null;
                Assert.AreEqual(1,Count("thump"),"a heavy hit sounds like a light one: "+string.Join(", ",cues));
                Assert.AreEqual(1,Count("hit_soft"),"the accent replaced the impact that says what was struck: "+string.Join(", ",cues));
                yield return Settled();yield return new WaitForSecondsRealtime(.2f);cues.Clear();
                var crit=Blow(taren,HitWeight.Light);crit.isCrit=true;b.Health.Receive(crit);yield return null;
                Assert.AreEqual(1,Count("crit"),"a crit sounds like any hit: "+string.Join(", ",cues));Assert.AreEqual(0,Count("thump"));
                yield return Settled();yield return new WaitForSecondsRealtime(.2f);cues.Clear();
                var breaker=Blow(taren,HitWeight.Light);breaker.breakPower=50;c.Health.Receive(breaker);yield return null;
                Assert.IsTrue(c.Health.Broken,"fixture: the break");Assert.AreEqual(1,Count("break"),"a break makes no sound of its own: "+string.Join(", ",cues));
                yield return Settled();yield return new WaitForSecondsRealtime(.2f);cues.Clear();
                d.Health.Receive(Blow(taren,HitWeight.Light,1000000));yield return null;
                Assert.IsFalse(d.Health.Alive,"fixture: the kill");Assert.AreEqual(1,Count("kill"),"a kill has only the body's own death: "+string.Join(", ",cues));
                yield return Settled();yield return new WaitForSecondsRealtime(.3f);cues.Clear();
                // A guard held past its Flash window is a plain guard: a dull clank over the armoured impact.
                taren.Guard(true);yield return new WaitForSecondsRealtime(.5f);
                Assert.AreEqual(ActorState.Guard,taren.State,"fixture: the guard is up");
                taren.Health.Receive(new DamagePacket{source=foe.Health,amount=taren.Health.maximum*.1f,weight=HitWeight.Light});yield return null;
                Assert.AreEqual(1,Count("guard_block"),"a blocked hit sounds like a hull hit: "+string.Join(", ",cues));Assert.AreEqual(1,Count("hit_armor"),string.Join(", ",cues));
                taren.Guard(false);
            }
            finally{AudioManager.CueStarted-=record;}
        }
        [UnityTest] public IEnumerator AFlashGuardHoldsTheFightAndALevelAndATagInStrikeAreHeardAndSeen()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(203,0,0));
            var foe=Enemy("SentinelHusk",new Vector3(200,0,8));yield return new WaitForSecondsRealtime(.6f);
            var cues=new List<string>();System.Action<string,Transform> record=(key,_)=>cues.Add(key);AudioManager.CueStarted+=record;
            try
            {
                var impact=Impact.Current;int stops=impact.Stops,guards=taren.FlashGuards;
                taren.Guard(true);
                Assert.AreEqual(0,taren.Health.Receive(new DamagePacket{source=foe.Health,amount=30,weight=HitWeight.Light}),"fixture: a guard on the beat stops the hit");
                Assert.AreEqual(guards+1,taren.FlashGuards,"fixture: a Flash Guard");
                Assert.AreEqual(stops+1,impact.Stops,"a perfect guard is a spark and a sound, and the blow doesn't stop on it");
                Assert.AreEqual(0,Time.timeScale);
                taren.Guard(false);yield return Settled();yield return new WaitForSecondsRealtime(.3f);cues.Clear();
                var member=GameServices.Current.State.party.Find(m=>m.id=="Taren");
                Levels.Grant(member,Levels.RequiredXp(member.level));yield return null;
                Assert.IsTrue(cues.Contains("level_up"),"a level gained has the toast's blip and nothing of its own: "+string.Join(", ",cues));
                Assert.IsTrue(Object.FindObjectsByType<SkillRing>(FindObjectsSortMode.None).Length>0,"a level gained shows nothing on the hero");
                yield return new WaitForSecondsRealtime(1);cues.Clear();
                Assert.IsTrue(PartyController.Current.Swap(),"fixture: the swap");yield return null;
                Assert.IsTrue(cues.Contains("swap_strike"),"the tag-in strike has only its borrowed force field: "+string.Join(", ",cues));
                Assert.IsTrue(cues.Contains("forceField_000"),"the tag-in strike lost the cue it had: "+string.Join(", ",cues));
                Assert.IsTrue(Object.FindObjectsByType<SkillRing>(FindObjectsSortMode.None).Length>0,"the tag-in strike shows nothing unless it hits");
            }
            finally{AudioManager.CueStarted-=record;}
        }
        [UnityTest] public IEnumerator AVirtualPadIsNeverRumbled()
        {
            pad=InputSystem.AddDevice<Gamepad>("ImpactPad");pad.MakeCurrent();yield return null;
            Assert.IsFalse(PadRumble.Supports(pad),"a virtual pad is taken for one with motors: the bridged Logitech and the replay's pad would be sent rumble commands");
            PadRumble.Pulse(1,1,.2f);
            Assert.IsFalse(PadRumble.Active);
            Assert.IsFalse(PadRumble.Supports(null));
        }

        // ------------------------------------------------------------------ the settings
        [UnityTest] public IEnumerator TheSettingsRowsStepThroughTheirValuesAndBackingOutRemembersTheRow()
        {
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.SettingsPage);yield return null;
            Button With(string words)=>menu.GetComponentsInChildren<Button>().FirstOrDefault(b=>{var text=b.GetComponentInChildren<TMP_Text>();return text!=null&&text.text.StartsWith(words);});
            string Words(GameObject row)=>row!=null&&row.GetComponentInChildren<TMP_Text>()!=null?row.GetComponentInChildren<TMP_Text>().text:"";
            Assert.IsNotNull(With("Keyboard bindings"));Assert.IsNotNull(With("Leave the game"),"the way out of the game left the Settings page");
            var open=With("Screen and impact");Assert.IsNotNull(open,"the pause menu has no way to turn the shake down");
            open.onClick.Invoke();yield return null;
            Assert.AreEqual(ImpactSettings.ShakeLevel.Full,ImpactSettings.Shake,"fixture: the defaults");
            StringAssert.Contains("Full",Words(With("Screen shake").gameObject));
            EventSystem.current.SetSelectedGameObject(With("Screen shake").gameObject);
            With("Screen shake").onClick.Invoke();yield return null;
            Assert.AreEqual(ImpactSettings.ShakeLevel.Reduced,ImpactSettings.Shake);StringAssert.Contains("Reduced",Words(With("Screen shake").gameObject),"the row doesn't say what it is set to");
            StringAssert.StartsWith("Screen shake",Words(EventSystem.current.currentSelectedGameObject),"a press moved the cursor off the row it changed");
            With("Screen shake").onClick.Invoke();yield return null;
            Assert.AreEqual(ImpactSettings.ShakeLevel.Off,ImpactSettings.Shake);StringAssert.Contains("Off",Words(With("Screen shake").gameObject));
            With("Screen shake").onClick.Invoke();yield return null;
            Assert.AreEqual(ImpactSettings.ShakeLevel.Full,ImpactSettings.Shake,"the last value doesn't come round to the first");
            With("Hit pause").onClick.Invoke();yield return null;
            Assert.IsFalse(ImpactSettings.HitStop);StringAssert.Contains("Off",Words(With("Hit pause").gameObject));
            With("Flashes").onClick.Invoke();yield return null;
            Assert.IsFalse(ImpactSettings.FullFlashes);StringAssert.Contains("Reduced",Words(With("Flashes").gameObject));
            With("Controller rumble").onClick.Invoke();yield return null;
            Assert.IsFalse(ImpactSettings.Rumble);StringAssert.Contains("Off",Words(With("Controller rumble").gameObject));
            menu.StepBack();yield return null;
            StringAssert.StartsWith("Screen and impact",Words(EventSystem.current.currentSelectedGameObject),"backing out forgets where the player came from");
            With("Leave the game").onClick.Invoke();yield return null;menu.StepBack();yield return null;
            StringAssert.StartsWith("Leave the game",Words(EventSystem.current.currentSelectedGameObject),"the new row moved the one below it, and backing out of its question lands elsewhere");
            With("Keyboard bindings").onClick.Invoke();yield return null;menu.StepBack();yield return null;
            StringAssert.StartsWith("Keyboard bindings",Words(EventSystem.current.currentSelectedGameObject));
            menu.Close();yield return null;
            Assert.AreEqual(4,ImpactSettingsUi.Rows.Length);
        }
        [UnityTest] public IEnumerator TheTitlesSettingsHaveTheSameFourRows()
        {
            SceneFlow.Current.LoadZone("Title");
            float deadline=Time.unscaledTime+15;while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: the title");yield return new WaitForSecondsRealtime(.3f);
            title.OpenSettings();yield return null;
            var rows=title.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Feel_")).ToArray();
            Assert.AreEqual(4,rows.Length,"the title's settings can't turn the shake, the pause, the flashes or the rumble down before the first fight");
            var back=title.GetComponentsInChildren<Button>().Single(b=>b.name=="Back");
            foreach(var row in rows)Assert.Greater(((RectTransform)row.transform).anchoredPosition.y-((RectTransform)back.transform).anchoredPosition.y,50,row.name+" lies on the Back button");
            var shake=rows.Single(b=>b.name=="Feel_Shake");
            shake.onClick.Invoke();yield return null;
            Assert.AreEqual(ImpactSettings.ShakeLevel.Reduced,ImpactSettings.Shake);
            StringAssert.Contains("Reduced",shake.GetComponentInChildren<TMP_Text>().text,"the title's row doesn't say what it is set to");
            Assert.IsNotNull(shake.navigation.selectOnDown,"the rows aren't in the settings' column: a pad can't reach them");
            title.CloseSettings();
        }
    }
}
