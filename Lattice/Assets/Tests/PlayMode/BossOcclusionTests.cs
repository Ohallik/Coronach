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
    public sealed class BossOcclusionTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("starter"); SceneFlow.Current.LoadZone("Arena_Ground");
            while (SceneFlow.Current.Loading) yield return null;
            foreach (var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) enemy.Passive = true;
            foreach (var hero in PartyController.Current.members)
            {
                hero.GetComponent<PlayerBrain>().AutoPilot = true;
                hero.GetComponent<PartnerBrain>().enabled = false;
            }
            yield return new WaitForSecondsRealtime(1);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }
        static Renderer Body(EnemyBrain boss) => boss.GetComponent<DefeatPresentation>().visual.GetComponentsInChildren<Renderer>()
            .First(r => r is MeshRenderer or SkinnedMeshRenderer);
        static MaterialPropertyBlock Properties(Renderer body)
        { var block = new MaterialPropertyBlock(); body.GetPropertyBlock(block); return block; }
        IEnumerator Visibility(string id)
        {
            var hero = PartyController.Current.Active; var camera = Camera.main;
            var toward = camera.transform.position - hero.transform.position; toward.y = 0; toward.Normalize();
            var boss = ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id), hero.transform.position + toward * 2.3f); boss.Passive = true;
            yield return new WaitForSecondsRealtime(.4f);
            var body = Body(boss); var props = Properties(body);
            Assert.Greater(props.GetFloat("_OccFade"), .9f, id + " hides the hero with its opaque body");
            Assert.IsTrue(body.sharedMaterials.All(m => m.IsKeywordEnabled("_OCCFADE_ON")), "player builds must retain the enabled fade shader variant");
            var ellipse = props.GetVector("_OccFadeEllipse");
            var chest = camera.WorldToViewportPoint(hero.transform.position + Vector3.up * .95f);
            Assert.That(ellipse.x, Is.EqualTo(chest.x).Within(.01));
            Assert.That(ellipse.y, Is.EqualTo(chest.y).Within(.01));
            Assert.That(ellipse.z, Is.InRange(.025f, .15f), "fade must stay local to the hero");
            Assert.That(ellipse.w, Is.InRange(.05f, .25f));
            var before = camera.transform.position; var rotation = camera.transform.rotation;
            // This is a stationary visibility fixture, not a movement replay.
            // Reset the controller's cached pose before moving it across the arena.
            var destination = hero.transform.position - toward * 15;
            var controller = boss.GetComponent<CharacterController>(); controller.enabled = false;
            boss.transform.position = destination; controller.enabled = true;
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.5f);
            var displacement = boss.transform.position - destination; displacement.y = 0;
            Assert.Less(displacement.magnitude, .1f, "visibility fixture did not reach the unobstructing position");
            Assert.Less(Properties(body).GetFloat("_OccFade"), .01f, "unobstructing boss must recover its full body; hero=" + hero.transform.position +
                " boss=" + boss.transform.position + " camera=" + camera.transform.position + " bounds=" + body.bounds);
            Assert.Less(Vector3.Distance(before, camera.transform.position), .01f, "ground visibility must not move the fixed camera");
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), .01f);
        }
        [UnityTest] public IEnumerator BurrowerRevealsTheHeroAndRestoresItsBody() => Visibility("Burrower");
        [UnityTest] public IEnumerator BellowsRevealsTheHeroAndRestoresItsBody() => Visibility("BellowsBelow");
        [UnityTest] public IEnumerator FlightDoesNotFadeGroundBodies()
        {
            var hero = PartyController.Current.Active; hero.flight = true;
            var camera = Camera.main; var toward = camera.transform.position - hero.transform.position; toward.y = 0; toward.Normalize();
            var boss = ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"), hero.transform.position + toward * 2.3f); boss.Passive = true;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.Less(Properties(Body(boss)).GetFloat("_OccFade"), .01f);
        }
    }
}
