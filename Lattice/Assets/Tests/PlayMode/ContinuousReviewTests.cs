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
    }
}
