using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class ContinuousReviewTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject fixture;
        ContinuousReview replay;
        static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
        static object Call(object obj, string name) => obj.GetType().GetMethod(name, Private).Invoke(obj, null);

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            fixture = new GameObject("Replay regression fixture"); fixture.SetActive(false);
            replay = fixture.AddComponent<ContinuousReview>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            // Unity does not call OnDestroy on a never-active fixture. Remove
            // its synthetic device explicitly, including any held buttons.
            if (replay != null)
            {
                var pad = (Gamepad)typeof(ContinuousReview).GetField("pad", Private).GetValue(replay);
                if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
                Set(replay, "pad", null);
            }
            if (fixture != null) Object.Destroy(fixture);
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }

        [UnityTest] public IEnumerator MissingPartyRejectsTheCheckpointWithoutLosingTheReport()
        {
            Assert.IsNull(PartyController.Current, "title fixture unexpectedly has a party");
            Set(replay, "route", new QualityRoute { scene = "Gullet_Tunnel" });
            Set(replay, "step", new QualityStep { name = "Tallow arrival", expectedScene = "TallowApproach", expectedForm = "CivilFlight" });
            Assert.DoesNotThrow(() => Call(replay, "ValidateStep"), "a rejected run must still reach WriteEvidence");
            var failures = (List<string>)typeof(ContinuousReview).GetField("failures", Private).GetValue(replay);
            Assert.IsTrue(failures.Exists(f => f.Contains("party unavailable")), "missing actors must reject the run explicitly");
            yield return null;
        }

        [UnityTest] public IEnumerator FlightChaseReaimsThroughInputAfterALateralRoll()
        {
            DevLoadout.Apply("starter"); SceneFlow.Current.LoadZone("Arena_Flight");
            float deadline = Time.unscaledTime + 10;
            while (SceneFlow.Current.Loading && Time.unscaledTime < deadline) yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach (var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) enemy.Passive = true;
            foreach (var hero in PartyController.Current.members)
            {
                hero.GetComponent<PlayerBrain>().AutoPilot = true;
                hero.GetComponent<PartnerBrain>().enabled = false;
                var cc = hero.GetComponent<CharacterController>(); cc.enabled = false;
                hero.transform.position = new Vector3(hero.character == "Taren" ? 200 : 220, 1, 0);
                cc.enabled = true; hero.GetComponent<FlightMotor>().Halt();
            }
            yield return new WaitForSecondsRealtime(.8f);
            var actor = PartyController.Current.Active;
            var victim = ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDart"), new Vector3(200, 1, 12));
            victim.Passive = true; victim.Health.maximum = victim.Health.integrity = 1000;
            yield return null;
            actor.target = victim.Health; actor.TargetLocked = true;
            Assert.IsTrue(actor.Dodge(Vector3.right));
            yield return new WaitForSecondsRealtime(.5f);
            Assert.Greater(Vector3.Angle(actor.motor.Facing, victim.transform.position - actor.transform.position), 60,
                "fixture did not turn away from the target");
            actor.GetComponent<PlayerBrain>().AutoPilot = false;
            Set(replay, "pad", InputSystem.AddDevice<Gamepad>("ReplayRegression"));
            // Clear the title/scene UI's release latch before introducing held
            // fire, just as the real replay's settle interval does.
            Call(replay, "InputUpdate"); yield return null; yield return null;
            Set(replay, "recording", true);
            Set(replay, "step", new QualityStep { name = "resume pursuit", navigate = true, approachTarget = true,
                tolerance = 17, leftTrigger = 1, buttons = new[] { "South" } });
            deadline = Time.unscaledTime + 3;
            while (Time.unscaledTime < deadline) { Call(replay, "InputUpdate"); yield return null; }
            Assert.Less(victim.Health.integrity, 950, $"the replay fires across the target instead of re-aiming after the roll; facing={actor.motor.Facing}, actor={actor.transform.position}, victim={victim.transform.position}, input={GameInput.Current.Move}, blocked={GameInput.Current.Blocked}, fire={GameInput.Current.Held("Fire")}");
        }

        [UnityTest] public IEnumerator TelegraphReactionDodgesThroughInputAndReleasesForTheNextTell()=>ReactToTell(false);
        [UnityTest] public IEnumerator SelectingACollarLinkStillReadsItsCarriersTell()=>ReactToTell(true);
        [UnityTest] public IEnumerator ASlowedTellStillReceivesTheDodgeAtItsActualImpact()=>ReactToTell(false,true);
        IEnumerator ReactToTell(bool throughLink,bool duringFlash=false)
        {
            DevLoadout.Apply("starter"); SceneFlow.Current.LoadZone("Arena_Ground");
            float deadline = Time.unscaledTime + 10;
            while (SceneFlow.Current.Loading && Time.unscaledTime < deadline) yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach (var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) enemy.Passive = true;
            foreach (var hero in PartyController.Current.members) hero.GetComponent<PartnerBrain>().enabled = false;
            var actor = PartyController.Current.Active;
            var target = new GameObject("Visible boss tell fixture", typeof(Health));
            target.transform.position = actor.transform.position + Vector3.forward * 8;
            var boss = target.AddComponent<BossController>(); boss.enabled = false;
            actor.target = target.GetComponent<Health>(); actor.TargetLocked = true;
            if(throughLink)
            {
                var link=new GameObject("Selected equipment",typeof(Health));link.transform.SetParent(target.transform,false);
                link.GetComponent<Health>().reactionOwner=actor.target;actor.target=link.GetComponent<Health>();
            }
            Set(replay, "pad", InputSystem.AddDevice<Gamepad>("ReplayTelegraphRegression"));
            Call(replay, "InputUpdate"); yield return null; yield return null;
            Set(replay, "recording", true);
            var chase = new QualityStep { navigate = true, approachTarget = true, tolerance = 18, evadeTelegraphs = true };
            Set(replay, "step", chase);
            if(duringFlash)GameTime.BeginFlash();
            try
            {
                for (int tell = 0; tell < 2; tell++)
                {
                    Set(boss, "busy", true); Set(boss, "strikeAt", Time.time + .45f);
                    bool dodged = false;
                    while (boss.Telegraphing)
                    {
                        Call(replay, "InputUpdate"); yield return null;
                        if (actor.State == ActorState.Dodge) dodged = true;
                    }
                    Assert.IsTrue(dodged, "visible tell " + tell + " received no ordinary dodge input");
                    Assert.IsTrue(GameInput.Current.Move.sqrMagnitude > .1f, "dodge requires lateral stick input, not an actor call");
                    float before = actor.Health.integrity;
                    actor.Health.Receive(new DamagePacket { source = actor.target, amount = 20, type = DamageType.Kinetic });
                    Assert.AreEqual(before, actor.Health.integrity, "the input arrived outside the dodge window");
                    Set(boss, "busy", false);
                    deadline = Time.unscaledTime + .6f;
                    while (Time.unscaledTime < deadline) { Call(replay, "InputUpdate"); yield return null; }
                    Assert.IsFalse(GameInput.Current.Held("Dodge"), "dodge must release between tells");
                }
                chase.evadeTelegraphs = false;
                Set(boss, "busy", true); Set(boss, "strikeAt", Time.time + .1f);
                Call(replay, "InputUpdate"); yield return null; yield return null;
                Assert.IsFalse(GameInput.Current.Held("Dodge"), "ordinary routes must not gain implicit evasive input");
            }
            finally { Object.Destroy(target); }
        }

        [UnityTest] public IEnumerator IncomingFlightShotsRequireExplicitEvasionAndUseRealInput()=>ReactToShot(false);
        [UnityTest] public IEnumerator ASlowedFlightShotStillReactsBeforeHullContact()=>ReactToShot(true);
        IEnumerator ReactToShot(bool duringFlash)
        {
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");
            float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members)
            {
                hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;
                var cc=hero.GetComponent<CharacterController>();cc.enabled=false;
                hero.transform.position=new Vector3(hero.character=="Taren"?200:230,1,0);cc.enabled=true;hero.GetComponent<FlightMotor>().Halt();
            }
            yield return new WaitForSecondsRealtime(.8f);
            var actor=PartyController.Current.Active;actor.GetComponent<PlayerBrain>().AutoPilot=false;
            var shooter=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(250,1,0));shooter.Passive=true;
            Set(replay,"pad",InputSystem.AddDevice<Gamepad>("ReplayShotRegression"));
            Call(replay,"InputUpdate");yield return null;yield return null;
            Set(replay,"recording",true);
            var input=new QualityStep{leftTrigger=1};Set(replay,"step",input);
            if(duringFlash)GameTime.BeginFlash();
            for(int attempt=0;attempt<2;attempt++)
            {
                input.evadeProjectiles=attempt==1;
                float before=actor.Health.integrity;bool rolled=false;float moved=0;
                var start=actor.transform.position;var aim=start+Vector3.up*.8f;
                Projectile.Fire(aim+Vector3.right*9,Vector3.left,new DamagePacket{source=shooter.Health,amount=14,type=DamageType.Beam},11);
                deadline=Time.unscaledTime+(duringFlash?4.5f:1.8f);
                while(Time.unscaledTime<deadline)
                {
                    Call(replay,"InputUpdate");yield return null;
                    rolled|=actor.State==ActorState.Dodge;moved=Mathf.Max(moved,Vector3.Distance(start,actor.transform.position));
                }
                if(attempt==0)
                {Assert.IsFalse(rolled,"default route gained implicit evasion");Assert.Less(actor.Health.integrity,before,"fixture shot did not threaten the real hull");}
                else
                {
                    Assert.IsTrue(rolled,"explicit incoming-shot policy never sent a roll");
                    Assert.AreEqual(before,actor.Health.integrity,"ordinary input failed to clear the real incoming shot");
                    Assert.Greater(moved,2,"no lateral flight displacement occurred");
                    Assert.IsFalse(GameInput.Current.Held("Roll"),"roll did not release after the threat passed");
                }
            }
        }
    }
}
