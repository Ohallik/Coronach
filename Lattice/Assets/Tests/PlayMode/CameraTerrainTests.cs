using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D198: the fixed ground camera keeps the party in sight beside a terrain rim (the November 4 Burrower
    /// capture lost Taren behind the excavation's south cliff for five steps), without moving anywhere it already can.</summary>
    public sealed class CameraTerrainTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
            foreach(var flag in new[]{"met.Survivor","bossdown.Burrower","clear.Sorrel_Burrower"})GameServices.Current.Flags.SetBool(flag,true);
            SceneFlow.Current.LoadZone("Sorrel_Ridges");float deadline=Time.unscaledTime+12;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Stand(Vector3 at)
        {
            foreach(var actor in PartyController.Current.members)
            {
                var cc=actor.GetComponent<CharacterController>();cc.enabled=false;
                var point=at+(actor==PartyController.Current.Active?Vector3.zero:Vector3.right*1.5f);
                if(Physics.Raycast(point+Vector3.up*20,Vector3.down,out var ground,60,~0,QueryTriggerInteraction.Ignore))point=ground.point;
                actor.transform.position=point;cc.enabled=true;
            }
            Physics.SyncTransforms();yield return new WaitForSecondsRealtime(2f);
        }
        static bool TerrainBetweenCameraAndHero(out float distance)
        {
            var eye=Camera.main.transform.position;var chest=PartyController.Current.Active.transform.position+Vector3.up*1.2f;
            distance=Vector3.Distance(eye,chest);
            return Physics.RaycastAll(eye,(chest-eye).normalized,distance,~0,QueryTriggerInteraction.Ignore).Any(h=>CameraRig.Scenery(h.collider));
        }

        [UnityTest] public IEnumerator BesideTheExcavationsSouthRimTheCameraStillSeesTheParty()
        {
            yield return Stand(new Vector3(-12.3f,0,156.8f));
            bool blocked=TerrainBetweenCameraAndHero(out float distance);
            TestContext.WriteLine($"RIM_CAMERA distance={distance:F2} blocked={blocked}");
            Assert.IsFalse(blocked,"the excavation's south cliff hides the party from the camera");
            Assert.GreaterOrEqual(distance,CameraRig.NearestGroundDistance-.5f,"the camera dives into the party");
        }

        [UnityTest] public IEnumerator InTheOpenExcavationTheCameraKeepsItsFraming()
        {
            yield return Stand(new Vector3(0,0,172));
            var rig=Object.FindFirstObjectByType<CameraRig>();
            bool blocked=TerrainBetweenCameraAndHero(out float distance);
            TestContext.WriteLine($"OPEN_CAMERA distance={distance:F2} rig={rig.GroundCameraDistance:F2} profile={rig.profile.distance:F2}");
            Assert.IsFalse(blocked,"control: the open excavation must be in sight");
            Assert.AreEqual(rig.profile.distance,rig.GroundCameraDistance,.01f,"the camera pulls in where nothing hides the party");
        }
    }
}
