using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>People in other ships: the town is flown around and talked to.
    /// The Compact's survey and a Nacre-bound household are visible before
    /// Meret appears (STORY_CAMPAIGN Act I).</summary>
    public sealed class CinderResidentTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Hub_CinderHalo");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        [UnityTest] public IEnumerator TheCompactSurveyAndANacreHouseholdHailFromTheirShips()
        {
            foreach(string id in new[]{"Ilo","Oda"})
            {
                var npc=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).SingleOrDefault(n=>n.speaker==id);
                Assert.IsNotNull(npc,id+" is not in Cinder Halo");
                var hail=npc.GetComponent<HailPoint>();Assert.IsNotNull(hail,id+" cannot be hailed from a ship");
                Assert.IsFalse(npc.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),id+" stands in space instead of speaking from a ship");
                // The ship reads as present but never blocks the recorded flight routes.
                var ship=GameObject.Find(id+"'s ship");Assert.IsNotNull(ship,id+" has no ship");
                Assert.Less(Vector3.Distance(ship.transform.position,npc.transform.position),4,id+"'s ship is not where they speak from");
                Assert.IsFalse(ship.GetComponentsInChildren<Collider>().Any(c=>c.enabled)||npc.GetComponentsInChildren<Collider>().Any(c=>c.enabled),id+"'s ship obstructs flight");
                // They have their own faces: a full expression set, not a borrowed one.
                var face=PortraitLookup.Get(id,Lattice.Data.BodyForm.Natural,Lattice.Data.PortraitEmotion.Neutral);
                Assert.IsNotNull(face,id+" has no portrait");
                Assert.AreNotEqual(PortraitLookup.Get("Neve",Lattice.Data.BodyForm.Natural,Lattice.Data.PortraitEmotion.Neutral),face,id+" borrows Neve's face");
                Assert.AreNotEqual(face,PortraitLookup.Get(id,Lattice.Data.BodyForm.Natural,Lattice.Data.PortraitEmotion.Worried),id+"'s expressions do not change");
                // Every tier of their conversation presents real lines.
                foreach(string node in new[]{id+"First",id+"Repeat",id+"Post"})
                {
                    var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;int before=dialogue.LinesPresented;dialogue.StartNode(node,id);
                    float until=Time.realtimeSinceStartup+10;yield return null;
                    while(dialogue.Running&&Time.realtimeSinceStartup<until)yield return null;
                    Assert.IsFalse(dialogue.Running,node+" never finished");Assert.Greater(dialogue.LinesPresented,before,node+" presented no lines");
                }
            }
        }
    }
}
