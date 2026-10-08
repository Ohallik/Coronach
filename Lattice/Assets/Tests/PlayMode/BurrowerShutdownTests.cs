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
    public sealed class BurrowerShutdownTests
    {
        GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            foreach(AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(bus,1,false);
            fixture=new GameObject("Burrower shutdown fixture");yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Object.Destroy(fixture);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        EnemyBrain Spawn(Vector3 position)
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),position);enemy.Passive=true;enemy.transform.SetParent(fixture.transform);return enemy;
        }
        static void Resolve(EnemyBrain enemy)=>enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=enemy.Health.maximum*100,type=DamageType.Pulse});
        static Color Emission(Renderer renderer)
        {
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            Color value=block.GetVector("_EmissionColor");return value.a==0?(Color)renderer.sharedMaterial.GetVector("_EmissionColor"):value;
        }
        static float Brightness(Color color)=>Mathf.Max(color.r,color.g,color.b);
        [UnityTest] public IEnumerator DisabledBurrowerHasOneMechanicalFinishWithoutTheGenericBossExplosion()
        {
            var enemy=Spawn(new Vector3(30,0,0));yield return null;
            using var cues=new CueLog();Resolve(enemy);Resolve(enemy);yield return null;
            Assert.AreEqual(1,cues.Count("death_mech",enemy.transform),"disabled rig needs exactly one mechanical finish: "+cues.All);
            Assert.AreEqual(0,cues.Count("explosionCrunch_000",enemy.transform),"disable still uses the generic boss explosion: "+cues.All);
            Assert.AreEqual(0,cues.Count("death_creature",enemy.transform),"machine was presented as a dying creature: "+cues.All);
        }
        [UnityTest] public IEnumerator DisabledBurrowerPowersDownWithoutDimmingAnotherRigOrItsSharedMaterial()
        {
            var disabled=Spawn(new Vector3(30,0,0));var working=Spawn(new Vector3(45,0,0));yield return null;
            var renderer=disabled.GetComponent<DefeatPresentation>().visual.GetComponentsInChildren<Renderer>().First(r=>r.sharedMaterial.HasProperty("_EmissionColor"));
            var other=working.GetComponent<DefeatPresentation>().visual.GetComponentsInChildren<Renderer>().First(r=>r.sharedMaterial.HasProperty("_EmissionColor"));
            var material=renderer.sharedMaterial;Color original=material.GetVector("_EmissionColor");float full=Brightness(original);
            Assert.Greater(full,.1f,"fixture must have a powered generated body");
            Debug.Log("SHUTDOWN_RAW_EMISSION original="+original.ToString("R")+" actual="+Emission(renderer).ToString("R"));
            Assert.AreEqual(original,Emission(renderer),"attaching the shutdown changed the live HDR emission");
            // The shutdown shares a renderer block with the actual boss
            // occlusion system. It must not erase another owner's fields.
            disabled.GetComponent<BossOcclusion>().enabled=false;
            var properties=new MaterialPropertyBlock();renderer.GetPropertyBlock(properties);
            var ellipse=new Vector4(.25f,.75f,.1f,.2f);properties.SetFloat("_OccFade",.73f);properties.SetVector("_OccFadeEllipse",ellipse);renderer.SetPropertyBlock(properties);
            Resolve(disabled);yield return new WaitForSecondsRealtime(.2f);
            float partial=Brightness(Emission(renderer));Assert.That(partial,Is.InRange(full*.05f,full*.98f),"disabled rig has no readable power loss");
            renderer.GetPropertyBlock(properties);Assert.AreEqual(.73f,properties.GetFloat("_OccFade"),"shutdown erased the hero visibility fade");
            Assert.AreEqual(ellipse,properties.GetVector("_OccFadeEllipse"),"shutdown changed the hero visibility boundary");
            GameTime.Paused=true;yield return null;var held=Emission(renderer);
            yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(held,Emission(renderer),"shutdown advances while paused");
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(1.1f);
            Assert.IsNotNull(disabled,"shutdown must finish before the existing body cleanup");
            Assert.LessOrEqual(Brightness(Emission(renderer)),full*.02f,"disabled rig still glows at full power");
            Assert.AreEqual(original,(Color)material.GetVector("_EmissionColor"),"shared material was modified");
            Assert.AreEqual(original,Emission(other),"another working rig lost power");
            Assert.IsFalse(disabled.Health.Alive);Assert.IsTrue(working.Health.Alive);
        }
        [UnityTest] public IEnumerator HealthRecoveryRestoresTheRigEmissionState()
        {
            var enemy=Spawn(new Vector3(30,0,0));yield return null;
            var renderer=enemy.GetComponent<DefeatPresentation>().visual.GetComponentsInChildren<Renderer>().First(r=>r.sharedMaterial.HasProperty("_EmissionColor"));
            Color original=renderer.sharedMaterial.GetVector("_EmissionColor");
            Resolve(enemy);yield return new WaitForSecondsRealtime(.3f);
            Assert.Less(Brightness(Emission(renderer)),Brightness(original)*.95f,"fixture must reach actual power loss before recovery");
            // Exercise the existing Health callback, without claiming that
            // enemy revival is an authored gameplay ability or resets its AI.
            enemy.Health.Heal(enemy.Health.maximum);yield return null;
            Assert.AreEqual(original,Emission(renderer),"recovery retained the shutdown tint");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(original,Emission(renderer),"the old shutdown clock resumed after recovery");
            Assert.AreEqual(original,(Color)renderer.sharedMaterial.GetVector("_EmissionColor"));
        }
    }
}
