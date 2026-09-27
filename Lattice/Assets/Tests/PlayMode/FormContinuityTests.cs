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
        [UnityTest] public IEnumerator RepeatingTheRequestedFormDoesNotRestartItsShrinkingBody()
        {
            form.Set(BodyForm.Natural);yield return new WaitForSecondsRealtime(.15f);
            float before=form.shaped.transform.localScale.x;Assert.Less(before,.9f);
            form.Set(BodyForm.Natural);
            Assert.LessOrEqual(form.shaped.transform.localScale.x,before+.05f,"same-form request pops the shrinking body back to full size");
            yield return new WaitForSecondsRealtime(.55f);
            Assert.IsFalse(form.Shaping,"same request unnecessarily extended the transition");Assert.AreEqual(BodyForm.Natural,form.Current);
            Assert.AreEqual(Vector3.one,form.natural.transform.localScale);
        }
        [UnityTest] public IEnumerator ReversingBeforeTheSwapGrowsTheExistingBodySmoothly()
        {
            form.Set(BodyForm.Natural);yield return new WaitForSecondsRealtime(.15f);
            float before=form.shaped.transform.localScale.x;form.Set(BodyForm.Shaped);
            Assert.LessOrEqual(form.shaped.transform.localScale.x,before+.05f,"reversal jumps to full scale immediately");
            float lowest=before;float deadline=Time.unscaledTime+.7f;
            while(Time.unscaledTime<deadline){lowest=Mathf.Min(lowest,form.shaped.transform.localScale.x);Assert.IsTrue(form.shaped.activeSelf);yield return null;}
            Assert.GreaterOrEqual(lowest,before-.05f,"returning to the visible form unnecessarily collapses it first");
            Assert.AreEqual(BodyForm.Shaped,form.Current);Assert.IsFalse(form.Shaping);Assert.AreEqual(Vector3.one,form.shaped.transform.localScale);
        }
        [UnityTest] public IEnumerator CivilAndCombatFlightKeepTheSameVisibleHull()
        {
            form.Set(BodyForm.Flight);yield return new WaitForSecondsRealtime(.7f);
            Assert.IsTrue(form.flight.activeSelf);form.Set(BodyForm.CivilFlight);
            Assert.AreEqual(BodyForm.CivilFlight,form.Current,"form semantics should update without rebuilding the same hull");
            Assert.IsFalse(form.Shaping,"same hull should not perform a shrink/swap/grow");
            Assert.AreEqual(Vector3.one,form.flight.transform.localScale);
        }
        [UnityTest] public IEnumerator PauseFreezesThenCompletesTheSameTransition()
        {
            form.Set(BodyForm.Natural);yield return new WaitForSecondsRealtime(.15f);GameTime.Paused=true;
            Vector3 before=form.shaped.transform.localScale;yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(before,form.shaped.transform.localScale);Assert.IsTrue(form.Shaping);
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
                    float before=visible.transform.localScale.x;target.Set(requested);
                    Assert.LessOrEqual(visible.transform.localScale.x,before+.05f,hero.character+" request snapped the visible body scale");
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
