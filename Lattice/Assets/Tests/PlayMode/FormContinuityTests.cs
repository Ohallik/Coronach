using System.Collections;
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
    public sealed class FormContinuityTests
    {
        FormController form;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var member in PartyController.Current.members)
            {member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            form=PartyController.Current.Active.GetComponent<FormController>();yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(BodyForm.Shaped,form.Current);Assert.IsFalse(form.Shaping);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        [UnityTest] public IEnumerator RepeatingTheRequestedFormDoesNotRestartItsFold()
        {
            form.Set(BodyForm.Natural);yield return new WaitForSecondsRealtime(.15f);
            var before=form.shaped.transform.localRotation;Assert.Greater(Quaternion.Angle(before,Quaternion.identity),2,"outgoing body did not start its staged pose");
            form.Set(BodyForm.Natural);
            Assert.Less(Quaternion.Angle(before,form.shaped.transform.localRotation),.05f,"same-form request pops the posed body");
            yield return new WaitForSecondsRealtime(.55f);
            Assert.IsFalse(form.Shaping,"same request unnecessarily extended the transition");Assert.AreEqual(BodyForm.Natural,form.Current);
            Assert.AreEqual(Vector3.one,form.natural.transform.localScale);
        }
        [UnityTest] public IEnumerator ReversingBeforeTheExchangeOpensTheExistingBodySmoothly()
        {
            form.Set(BodyForm.Natural);yield return new WaitForSecondsRealtime(.15f);
            var before=form.shaped.transform.localRotation;float angle=Quaternion.Angle(before,Quaternion.identity);form.Set(BodyForm.Shaped);
            Assert.Less(Quaternion.Angle(before,form.shaped.transform.localRotation),.05f,"reversal snaps to the rest pose immediately");
            float deadline=Time.unscaledTime+.7f;
            while(Time.unscaledTime<deadline)
            {
                Assert.LessOrEqual(Quaternion.Angle(form.shaped.transform.localRotation,Quaternion.identity),angle+.1f,"returning to the visible form unnecessarily folds it further");
                Assert.IsTrue(form.shaped.activeSelf);Assert.AreEqual(Vector3.one,form.shaped.transform.localScale);yield return null;
            }
            Assert.AreEqual(BodyForm.Shaped,form.Current);Assert.IsFalse(form.Shaping);Assert.AreEqual(Vector3.one,form.shaped.transform.localScale);
        }
        [UnityTest] public IEnumerator CivilAndCombatFlightKeepTheSameVisibleHull()
        {
            form.Set(BodyForm.Flight);yield return new WaitForSecondsRealtime(.7f);
            Assert.IsTrue(form.flight.activeSelf);form.Set(BodyForm.CivilFlight);
            Assert.AreEqual(BodyForm.CivilFlight,form.Current,"form semantics should update without rebuilding the same hull");
            Assert.IsFalse(form.Shaping,"same hull should not restart its transformation");
            Assert.AreEqual(Vector3.one,form.flight.transform.localScale);
        }
        [UnityTest] public IEnumerator PauseFreezesThenCompletesTheSameTransition()
        {
            form.Set(BodyForm.Natural);yield return new WaitForSecondsRealtime(.15f);GameTime.Paused=true;
            var rootPose=form.shaped.transform.localRotation;var vanes=form.shaped.GetComponent<GeneratedVanes>();var vanePose=vanes.vanes[0].localRotation;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(rootPose,form.shaped.transform.localRotation);Assert.AreEqual(vanePose,vanes.vanes[0].localRotation);Assert.IsTrue(form.Shaping);
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(BodyForm.Natural,form.Current);Assert.IsFalse(form.Shaping);Assert.AreEqual(Vector3.one,form.natural.transform.localScale);
        }
        [UnityTest] public IEnumerator FlightRuleRetargetDoesNotRestartAnIncomingHull()
        {
            form.Set(BodyForm.Flight);yield return new WaitForSecondsRealtime(.15f);
            form.Set(BodyForm.CivilFlight);yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(form.Shaping,"flight rule change restarted an in-progress arrival of the same hull");
            Assert.AreEqual(BodyForm.CivilFlight,form.Current);Assert.AreEqual(Vector3.one,form.flight.transform.localScale);
        }
        [UnityTest] public IEnumerator BothHeroesKeepOneContinuousBodyThroughRapidRequests()
        {
            foreach(var hero in PartyController.Current.members)
            {
                var target=hero.GetComponent<FormController>();
                foreach(var requested in new[]{BodyForm.Natural,BodyForm.Natural,BodyForm.Shaped,BodyForm.Natural,BodyForm.Flight,BodyForm.CivilFlight})
                {
                    var visible=target.natural.activeSelf?target.natural:target.shaped.activeSelf?target.shaped:target.flight;
                    var rotation=visible.transform.localRotation;var position=visible.transform.localPosition;target.Set(requested);
                    Assert.Less(Quaternion.Angle(rotation,visible.transform.localRotation),.05f,hero.character+" request snapped the visible body pose");
                    Assert.Less(Vector3.Distance(position,visible.transform.localPosition),.001f);Assert.AreEqual(Vector3.one,visible.transform.localScale);
                    yield return new WaitForSecondsRealtime(.12f);
                    Assert.AreEqual(1,(target.natural.activeSelf?1:0)+(target.shaped.activeSelf?1:0)+(target.flight.activeSelf?1:0));
                }
                yield return new WaitForSecondsRealtime(.7f);
                Assert.AreEqual(BodyForm.CivilFlight,target.Current);Assert.IsFalse(target.Shaping);
                Assert.IsTrue(target.flight.activeSelf);Assert.AreEqual(Vector3.one,target.flight.transform.localScale);
            }
        }
    }
}
