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
    public sealed class FormStageTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.8f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        [UnityTest] public IEnumerator BothHeroesFoldAttachedVanesWithoutMiniaturisingTheirBody()
        {
            foreach(var actor in PartyController.Current.members)
            {
                var form=actor.GetComponent<FormController>();var vanes=form.shaped.GetComponent<GeneratedVanes>();
                var rotation=vanes.vanes.Select(v=>v.localRotation).ToArray();var positions=vanes.vanes.Select(v=>v.localPosition).ToArray();
                form.Set(BodyForm.Flight);yield return new WaitForSecondsRealtime(.16f);
                Assert.AreEqual(Vector3.one,form.shaped.transform.localScale,actor.character+" body collapses instead of folding");
                for(int i=0;i<vanes.vanes.Length;i++)
                {
                    Assert.Greater(Quaternion.Angle(rotation[i],vanes.vanes[i].localRotation),12,actor.character+" attached vane never folds");
                    Assert.Less(Vector3.Distance(positions[i],vanes.vanes[i].localPosition),.001f,"fold detached its bone-mounted hinge");
                }
                yield return new WaitForSecondsRealtime(.5f);Assert.IsFalse(form.Shaping);Assert.AreEqual(BodyForm.Flight,form.Current);
            }
        }
        [UnityTest] public IEnumerator HullWingsFoldBeforeLandingAndReturnToTheirExactGeneratedMesh()
        {
            foreach(var actor in PartyController.Current.members)
            {
                var form=actor.GetComponent<FormController>();form.Set(BodyForm.Flight);yield return new WaitForSecondsRealtime(.7f);
                var meshes=form.flight.GetComponentsInChildren<MeshFilter>();
                var original=meshes.Select(f=>f.sharedMesh.vertices).ToArray();
                form.Set(BodyForm.Shaped);yield return new WaitForSecondsRealtime(.16f);
                Assert.AreEqual(Vector3.one,form.flight.transform.localScale,actor.character+" hull shrinks instead of folding its wings");
                float largest=0;
                for(int i=0;i<meshes.Length;i++)
                {var current=meshes[i].sharedMesh.vertices;for(int v=0;v<original[i].Length;v++)largest=Mathf.Max(largest,meshes[i].transform.TransformVector(current[v]-original[i][v]).magnitude);}
                Assert.Greater(largest,.18f,actor.character+" visible generated wing vertices never fold");
                yield return new WaitForSecondsRealtime(.5f);
                for(int i=0;i<meshes.Length;i++)CollectionAssert.AreEqual(original[i],meshes[i].sharedMesh.vertices,"rest mesh was not restored");
            }
        }
        [UnityTest] public IEnumerator TransitionCancelsPendingCombatAndRejectsNewActionsUntilOneBodySettles()
        {
            var actor=PartyController.Current.Active;var form=actor.GetComponent<FormController>();actor.charge=100;
            Assert.IsTrue(actor.Attack());int attack=actor.AttackSequence;form.Set(BodyForm.Natural);
            Assert.Greater(actor.AttackSequence,attack,"pending contact survived body replacement");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(actor.Attack());Assert.IsFalse(actor.Skill(0));Assert.IsFalse(actor.Dodge(Vector3.forward));
            Assert.AreEqual(1,(form.natural.activeSelf?1:0)+(form.shaped.activeSelf?1:0)+(form.flight.activeSelf?1:0));
            yield return new WaitForSecondsRealtime(.5f);Assert.IsTrue(actor.CanAct);
        }
        [UnityTest] public IEnumerator LethalInterruptionLeavesOneFullHullForFlightDefeat()
        {
            var party=PartyController.Current;party.enabled=false;var actor=party.Active;var form=actor.GetComponent<FormController>();
            actor.flight=true;actor.GetComponent<HeroCollision>().SetFlight(true);form.Set(BodyForm.Flight);
            yield return new WaitForSecondsRealtime(.14f);
            actor.Health.Receive(new DamagePacket{amount=10000,type=DamageType.Kinetic});
            Assert.IsFalse(actor.Health.Alive);Assert.IsFalse(form.Shaping,"death left a live body replacement coroutine");
            Assert.IsTrue(form.flight.activeSelf,"flight defeat captured an invisible hull");Assert.IsFalse(form.shaped.activeSelf);Assert.IsFalse(form.natural.activeSelf);
            Assert.AreEqual(Vector3.one,form.flight.transform.localScale);
            yield return new WaitForSecondsRealtime(.2f);Assert.IsTrue(form.flight.activeSelf);
        }
        [UnityTest] public IEnumerator OrdinaryDockInteractionFoldsBeforeSceneFadeAndOpensTheArrival()
        {
            var actor=PartyController.Current.Active;var form=actor.GetComponent<FormController>();
            var pad=new GameObject("Dock transition test",typeof(Lattice.World.DockingPad)).GetComponent<Lattice.World.DockingPad>();pad.scene="Hub_CinderHalo";
            pad.Interact();Assert.IsTrue(SceneFlow.Current.Loading);Assert.IsTrue(GameServices.Current.Input.Blocked);
            Assert.IsTrue(form.Shaping,"ordinary dock skipped departure folding");
            yield return new WaitForSecondsRealtime(.14f);Assert.IsTrue(form.shaped.activeSelf);Assert.AreEqual(Vector3.one,form.shaped.transform.localScale);
            float deadline=Time.unscaledTime+8;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);Assert.AreEqual("Hub_CinderHalo",SceneFlow.Current.Zone);
            foreach(var member in PartyController.Current.members)
            {
                var arrived=member.GetComponent<FormController>();Assert.IsTrue(member.flight);Assert.IsTrue(arrived.flight.activeSelf);
                Assert.IsFalse(arrived.Shaping,"control returned before the arrival opened");Assert.AreEqual(Vector3.one,arrived.flight.transform.localScale);
                Assert.AreEqual(member.GetComponent<CharacterController>().radius,member.GetComponent<CapsuleCollider>().radius);
            }
            Assert.IsFalse(GameServices.Current.Input.Blocked);if(pad!=null)Object.Destroy(pad.gameObject);
        }
        [UnityTest] public IEnumerator FoldingStopsAnExistingDashAndHoldsTravelUntilTheBodyIsReady()
        {
            var actor=PartyController.Current.Active;var form=actor.GetComponent<FormController>();
            Assert.IsTrue(actor.Dodge(Vector3.forward));var position=actor.transform.position;
            form.Set(BodyForm.Natural);
            float deadline=Time.unscaledTime+.2f;
            while(Time.unscaledTime<deadline)
            {
                actor.motor.Move(Vector2.up,true,false);
                Assert.Less(Vector3.Distance(position,actor.transform.position),.001f,"folding body kept travelling under old dash or new input");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(.5f);Assert.IsFalse(form.Shaping);
            deadline=Time.unscaledTime+.15f;while(Time.unscaledTime<deadline){actor.motor.Move(Vector2.up,false,false);yield return null;}
            Assert.Greater(Vector3.Distance(position,actor.transform.position),.1f,"travel did not resume after unfolding");
        }
    }
}
