using System.Collections;
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
    public sealed class FlightDisableTests
    {
        CombatActor[] members;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            var party=PartyController.Current;party.enabled=false;members=party.members;
            foreach(var actor in members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.8f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 point)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.SetPositionAndRotation(point,Quaternion.identity);cc.enabled=true;
            actor.GetComponent<FlightMotor>().Halt();Physics.SyncTransforms();
        }
        static void Down(CombatActor actor)=>actor.Health.Receive(new DamagePacket{amount=100000,type=DamageType.Kinetic});
        [UnityTest] public IEnumerator DisabledHullStopsItsLivePartnerWithoutRemainingDamageable()
        {
            foreach(var disabled in members)
            {
                var live=members.First(m=>m!=disabled);Place(live,new Vector3(200,1,0));Place(disabled,new Vector3(200,1,6));Down(disabled);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.IsFalse(disabled.GetComponent<CapsuleCollider>().enabled,"disabled damage trigger remains active");
                float deadline=Time.unscaledTime+1,minGap=float.PositiveInfinity;
                while(Time.unscaledTime<deadline)
                {
                    live.motor.Move(Vector2.up,false,false);yield return null;
                    minGap=Mathf.Min(minGap,Vector3.Distance(live.transform.position,disabled.transform.position));
                }
                float envelope=HeroCollision.HullRadius(live.character)+HeroCollision.HullRadius(disabled.character);
                Assert.GreaterOrEqual(minGap,envelope-.04f,"live craft passes through recoverable generated hull");
                Assert.Less(minGap,envelope+.5f,"fixture never reached the disabled hull");
                Assert.AreEqual(0,disabled.Health.integrity);Assert.IsFalse(disabled.Attack());
                disabled.Health.Heal(disabled.Health.maximum*.35f);yield return new WaitForSecondsRealtime(1.3f);
                Assert.IsTrue(disabled.GetComponent<CapsuleCollider>().enabled);Assert.IsTrue(disabled.CanAct);
            }
        }
        [UnityTest] public IEnumerator EarlyReviveStartsFromTheVisibleFailurePoseWithoutAJump()
        {
            foreach(var actor in members)
            {
                Place(actor,new Vector3(200,1,0));Place(members.First(m=>m!=actor),new Vector3(215,1,0));Down(actor);
                yield return new WaitForSecondsRealtime(.12f);
                var hull=actor.GetComponent<FormController>().flight.transform;var pose=hull.localRotation;var point=hull.localPosition;
                actor.Health.Heal(actor.Health.maximum*.35f);yield return null;yield return null;
                Assert.Less(Quaternion.Angle(pose,hull.localRotation),8,"early revive jumped to a terminal pose it never reached");
                Assert.Less(Vector3.Distance(point,hull.localPosition),.04f,"early revive snapped its altitude");
                yield return new WaitForSecondsRealtime(1.3f);Assert.IsTrue(actor.CanAct);
                Assert.Less(Quaternion.Angle(Quaternion.identity,hull.localRotation),1);Assert.Less(Vector3.Distance(hull.localPosition,Vector3.up*.8f),.001f);
            }
        }
        [UnityTest] public IEnumerator RescueSignalHoldsDuringPauseAndClearsAfterRecovery()
        {
            var actor=members[1];Place(actor,new Vector3(200,1,0));Place(members[0],new Vector3(215,1,0));Down(actor);
            yield return new WaitForSecondsRealtime(.7f);
            var ring=actor.GetComponentsInChildren<LineRenderer>().FirstOrDefault(r=>r.gameObject.name=="Flight rescue signal");
            Assert.IsNotNull(ring,"recoverable craft has no visible rescue signal");Assert.IsTrue(ring.enabled);
            Assert.Greater(ring.bounds.size.x,2,"rescue signal cannot be read around the hull");
            var hull=actor.GetComponent<FormController>().flight.transform;var pose=hull.localRotation;var color=ring.startColor;
            GameTime.Paused=true;yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(pose,hull.localRotation);Assert.AreEqual(color,ring.startColor);
            GameTime.Paused=false;actor.Health.Heal(actor.Health.maximum*.35f);yield return new WaitForSecondsRealtime(1.4f);
            Assert.IsFalse(ring.enabled,"recovered craft still advertises a rescue");
            yield return new WaitForSecondsRealtime(.3f);Down(actor);yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(ring.enabled);Assert.AreEqual(1,actor.GetComponentsInChildren<LineRenderer>().Count(r=>r.gameObject.name=="Flight rescue signal"),"repeat down leaked rescue renderers");
        }
        [UnityTest] public IEnumerator LandingADisabledPartnerRemovesItsFlightSignalAndSolidHull()
        {
            var actor=members[1];Place(actor,new Vector3(200,1,0));Place(members[0],new Vector3(215,1,0));Down(actor);
            yield return new WaitForSecondsRealtime(.35f);ZoneController.Current.SafePocket(true);
            yield return new WaitForSecondsRealtime(.7f);
            var form=actor.GetComponent<FormController>();Assert.IsTrue(form.natural.activeSelf);Assert.IsFalse(form.Shaping);
            var ring=actor.GetComponentsInChildren<LineRenderer>().First(r=>r.gameObject.name=="Flight rescue signal");
            Assert.IsFalse(ring.enabled,"landed body retains a floating ship rescue ring");
            Assert.IsFalse(actor.GetComponent<CharacterController>().enabled,"landed downed body retained the solid ship obstacle");
            ZoneController.Current.SafePocket(false);yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(form.flight.activeSelf);Assert.IsFalse(form.Shaping);Assert.IsTrue(ring.enabled);Assert.IsTrue(actor.GetComponent<CharacterController>().enabled);
        }
    }
}
