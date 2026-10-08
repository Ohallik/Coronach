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
    public sealed class CameraReturnTests
    {
        CameraRig rig;
        CombatActor hero;
        Vector3 ordinary;
        int previousCap;
        [UnitySetUp] public IEnumerator Boot()
        {
            previousCap=Application.targetFrameRate;Application.targetFrameRate=60;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");
            float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            var party=PartyController.Current;hero=party.Active;
            foreach(var actor in party.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.GetComponent<FlightMotor>().Halt();}
            party.enabled=false;
            rig=Object.FindFirstObjectByType<CameraRig>();
            yield return new WaitForSecondsRealtime(1);
            ordinary=Camera.main.transform.position;
            // The camera receives the same generic envelope used by the
            // combat bridge; separate Cantor tests retain actual mesh coverage.
            rig.FrameEncounter(new Bounds(hero.transform.position+Vector3.forward*18,new Vector3(36,8,40)));
            yield return new WaitForSecondsRealtime(2);
            Assert.Greater(Vector3.Distance(ordinary,Camera.main.transform.position),10,"fixture needs an expanded encounter view");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();Application.targetFrameRate=previousCap;
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTest] public IEnumerator TheReturnDoesNotCollapseMostOfTheViewInAQuarterSecond()
        {
            float initial=Vector3.Distance(ordinary,Camera.main.transform.position);
            rig.FrameEncounter(null);float started=Time.unscaledTime;
            yield return new WaitForSecondsRealtime(.25f);
            float elapsed=Time.unscaledTime-started,remaining=Vector3.Distance(ordinary,Camera.main.transform.position)/initial;
            yield return new WaitForSecondsRealtime(1.75f);
            float settled=Vector3.Distance(ordinary,Camera.main.transform.position);
            Debug.Log($"CAMERA_RETURN elapsed={elapsed:R} remaining={remaining:R} settled={settled:R}");
            Assert.Less(elapsed,.35f,"quarter-second sample was not observed in time");
            Assert.GreaterOrEqual(remaining,.5f,"most of the expanded view collapses within a quarter second");
            Assert.Less(settled,.5f,"return must still finish promptly");
        }
        [UnityTest] public IEnumerator AnActiveReturnHoldsItsRenderedPositionWhilePaused()
        {
            rig.FrameEncounter(null);yield return new WaitForSecondsRealtime(.25f);
            GameTime.Paused=true;yield return null;
            var held=Camera.main.transform.position;
            yield return new WaitForSecondsRealtime(.35f);
            Assert.Less(Vector3.Distance(held,Camera.main.transform.position),.001f,"paused release return continues to move the view");
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(1.75f);
            Assert.Less(Vector3.Distance(ordinary,Camera.main.transform.position),.5f,"unpaused return never resumes");
        }
        [UnityTest] public IEnumerator TheReturningViewContinuesFollowingTheMovingHero()
        {
            rig.FrameEncounter(null);var controller=hero.GetComponent<CharacterController>();controller.enabled=false;
            float started=Time.unscaledTime;int samples=0;
            while(Time.unscaledTime-started<1.3f)
            {
                hero.transform.position+=Vector3.right*(20*Time.unscaledDeltaTime);
                yield return null;
                FlightFramingTests.CheckMeshes(hero.GetComponent<FormController>().flight,"hero moving during encounter return");samples++;
            }
            Assert.Greater(samples,50);
        }
        [UnityTest] public IEnumerator SwappingHeroesDoesNotSnapAnActiveReturn()
        {
            rig.FrameEncounter(null);yield return new WaitForSecondsRealtime(.25f);
            var before=Camera.main.transform.position;
            Assert.IsTrue(PartyController.Current.Swap());
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.GetComponent<FlightMotor>().Halt();}
            yield return null;
            float jump=Vector3.Distance(before,Camera.main.transform.position);
            Debug.Log($"CAMERA_RETURN_SWAP jump={jump:R}");
            Assert.Less(jump,2,"hero swap resets the returning camera in one frame");
        }
        [UnityTest] public IEnumerator ANewEncounterTakesPriorityOverTheReturn()
        {
            rig.FrameEncounter(null);yield return new WaitForSecondsRealtime(.25f);
            var bounds=new Bounds(hero.transform.position+Vector3.right*18,new Vector3(12,8,12));
            rig.FrameEncounter(bounds);yield return new WaitForSecondsRealtime(1);
            for(int corner=0;corner<8;corner++)
            {
                var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                var view=Camera.main.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,sign));
                Assert.Greater(view.z,0,"new encounter behind the camera");
                Assert.That(view.x,Is.InRange(.06f,.94f),"new encounter loses horizontal priority to return");
                Assert.That(view.y,Is.InRange(.10f,.90f),"new encounter loses vertical priority to return");
            }
        }
    }
}
