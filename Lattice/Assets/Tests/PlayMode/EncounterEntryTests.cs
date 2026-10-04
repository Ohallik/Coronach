using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class EncounterEntryTests
    {
        PartyController party;
        EncounterVolume encounter;
        Membrane entry;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("starter"); SceneFlow.Current.LoadZone("Sorrel_Ridges");
            while (SceneFlow.Current.Loading) yield return null;
            party = PartyController.Current;
            foreach (var hero in party.members)
            {
                hero.GetComponent<PlayerBrain>().AutoPilot = true;
                hero.GetComponent<PartnerBrain>().enabled = false;
            }
            encounter = Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e => e.encounterId == "Sorrel_Burrower");
            entry = encounter.membranes.Single(m => m.name == "Drill entry seal");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }
        static void Place(CombatActor actor, Vector3 point)
        {
            var controller = actor.GetComponent<CharacterController>(); controller.enabled = false;
            actor.transform.position = point; controller.enabled = true;
        }
        IEnumerator EnterWithTrailingPartner(bool down)
        {
            Place(party.members[1], new Vector3(3, 0, 146));
            if (down) party.members[1].Health.integrity = 0;
            Place(party.Active, new Vector3(0, 0, 159)); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(encounter.Started, "boss started while a partner was stranded outside the entry seal");
            Assert.IsTrue(entry.Open, "the route back to the trailing/downed partner must remain open");
            // The companion has crossed the gate with body clearance, but need
            // not reach the centre of the encounter volume to start the fight.
            party.members[1].Health.ResetFull();
            Place(party.members[1], new Vector3(3, 0, 156)); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(encounter.Started, "the whole party crossed the entry but the encounter did not begin");
            Assert.IsFalse(entry.Open);
            Assert.AreEqual(1, encounter.Remaining, "entry must spawn the boss once");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(1, encounter.Remaining, "staying in the trigger must not spawn another boss");
        }
        [UnityTest] public IEnumerator EntryWaitsForTheTrailingPartner() => EnterWithTrailingPartner(false);
        [UnityTest] public IEnumerator EntryDoesNotAbandonADownedPartner() => EnterWithTrailingPartner(true);
        [UnityTest] public IEnumerator LoadedMembranesReportTheirAuthoredPassability()
        {
            Assert.IsTrue(entry.GetComponentsInChildren<Collider>(true).All(c => !c.enabled));
            Assert.IsTrue(entry.Open, "the authored open entry loses its runtime state on load");
            var exit = encounter.membranes.Single(m => m != entry);
            Assert.IsTrue(exit.GetComponentsInChildren<Collider>(true).Any(c => c.enabled));
            Assert.IsFalse(exit.Open); yield return null;
        }
        [UnityTest] public IEnumerator ClosedSealsKeepTheExcavationVisible()
        {
            foreach (var seal in encounter.membranes) seal.SetOpen(false);
            Physics.SyncTransforms();
            var profile = ZoneController.Current.definition.cameraProfile;
            var rotation = Quaternion.Euler(profile.pitch, profile.yaw, 0);
            for (float z = 154.5f; z <= 189; z += 2)
                for (float x = -9; x <= 9; x += 3)
                {
                    var point = new Vector3(x, 0, z);
                    var origin = point - rotation * Vector3.forward * profile.distance;
                    foreach (var hit in Physics.RaycastAll(origin, point + Vector3.up * .85f - origin,
                        Vector3.Distance(origin, point + Vector3.up * .85f), ~0, QueryTriggerInteraction.Ignore))
                        Assert.IsFalse(encounter.membranes.Any(m => hit.transform.IsChildOf(m.transform)),
                            "closed seal occludes the hero at " + point);
                }
            Place(party.Active, new Vector3(0, 0, 151)); Physics.SyncTransforms();
            party.Active.GetComponent<CharacterController>().Move(Vector3.forward * 6);
            Assert.Less(party.Active.transform.position.z, 153,
                "the visible low seal must still block ordinary ground travel");
            yield return null;
        }
    }
}
