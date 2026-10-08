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
    public sealed class FlightFramingTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTest] public IEnumerator CantorAndTheHeroStayVisibleAtOrdinaryFiringRange()
        {
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("starter"); SceneFlow.Current.LoadZone("Arena_Flight");
            while (SceneFlow.Current.Loading) yield return null;
            foreach (var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) enemy.Passive = true;
            foreach (var hero in PartyController.Current.members)
            {
                hero.GetComponent<PlayerBrain>().AutoPilot = true;
                hero.GetComponent<PartnerBrain>().enabled = false;
                hero.GetComponent<FlightMotor>().Halt();
            }
            var actor = PartyController.Current.Active;
            var boss = ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"), actor.transform.position + Vector3.forward * 17);
            boss.Passive = true;
            actor.target = boss.Health; actor.TargetLocked = true;
            for (int bearing = 0; bearing < 8; bearing++)
            {
                var direction = Quaternion.Euler(0, bearing * 45, 0) * Vector3.forward;
                boss.transform.position = actor.transform.position + direction * 17;
                boss.transform.rotation = Quaternion.LookRotation(-direction);
                yield return new WaitForSecondsRealtime(1.5f);
                CheckMeshes(actor.GetComponent<FormController>().flight, "hero at bearing " + bearing);
                CheckMeshes(boss.gameObject, "Cantor at bearing " + bearing);
                if (bearing == 0)
                {
                    Assert.IsTrue(PartyController.Current.Swap());
                    actor = PartyController.Current.Active;
                    for (int frame = 0; frame < 15; frame++)
                    {
                        yield return null;
                        CheckMeshes(actor.GetComponent<FormController>().flight, "hero during swap");
                        CheckMeshes(boss.gameObject, "Cantor during swap");
                    }
                }
            }
            Object.Destroy(boss.gameObject);
            yield return new WaitForSecondsRealtime(2.5f);
            var rig = Object.FindFirstObjectByType<CameraRig>();
            Assert.That(Vector3.Distance(Camera.main.transform.position, actor.transform.position),
                Is.EqualTo(rig.profile.distance).Within(1), "ordinary travel framing should return after the encounter");
        }
        internal static void CheckMeshes(GameObject root, string label)
        {
            int count = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                if (renderer.name == "Telegraph") continue;
                count++;
                var bounds = renderer.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    var view = Camera.main.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents, sign));
                    Assert.Greater(view.z, 0, label + " behind camera");
                    Assert.That(view.x, Is.InRange(.06f, .94f), label + " clips horizontally");
                    Assert.That(view.y, Is.InRange(.10f, .90f), label + " clips vertically");
                }
            }
            Assert.Greater(count, 0, label + " has no measured body");
        }

        [UnityTest] public IEnumerator GroundCameraIgnoresTheFlightEncounterEnvelope()
        {
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("starter"); SceneFlow.Current.LoadZone("Arena_Ground");
            while (SceneFlow.Current.Loading) yield return null;
            foreach (var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) enemy.Passive = true;
            var party = PartyController.Current;
            foreach (var hero in party.members)
            {
                hero.GetComponent<PlayerBrain>().AutoPilot = true;
                hero.GetComponent<PartnerBrain>().enabled = false;
            }
            // Isolate CameraRig's guard from the bridge's independent ground guard.
            party.enabled = false;
            yield return new WaitForSecondsRealtime(1);
            var rig = Object.FindFirstObjectByType<CameraRig>();
            var camera = Camera.main;
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            float fov = camera.fieldOfView;
            rig.FrameEncounter(new Bounds(new Vector3(120, 20, 90), Vector3.one * 40));
            yield return new WaitForSecondsRealtime(1);
            Assert.Less(Vector3.Distance(position, camera.transform.position), .001f, "flight composition moved the fixed ground view");
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), .01f);
            Assert.AreEqual(fov, camera.fieldOfView);
        }
    }
}
