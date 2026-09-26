using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class StationCompanionTests
    {
        [UnityTest] public IEnumerator FollowerLeavesTheServiceGalleryThroughTheHousingDoor()
        {
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Hub_Decks");
            while(SceneFlow.Current.Loading)yield return null;
            var party=PartyController.Current;
            Assert.AreEqual(2,party.members.Length);
            var leader=party.Active;leader.GetComponent<PlayerBrain>().AutoPilot=true;
            var follower=party.members[1-party.index];
            Place(leader,new Vector3(-28,0,-11));Place(follower,new Vector3(-25.6f,0,21.5f));
            float deadline=Time.unscaledTime+18;
            while(Time.unscaledTime<deadline&&Vector3.Distance(leader.transform.position,follower.transform.position)>6)yield return null;
            Assert.Less(Vector3.Distance(leader.transform.position,follower.transform.position),6,
                "A follower must route through real door openings, not push against the gallery wall and remain thirty metres behind.");
            Assert.Greater(follower.transform.position.y,-.3f,"following cannot escape beneath the deck");
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var controller=actor.GetComponent<CharacterController>();controller.enabled=false;
            actor.transform.position=position;controller.enabled=true;
        }
    }
}
