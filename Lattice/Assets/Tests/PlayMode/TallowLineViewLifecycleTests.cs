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
    public sealed class TallowLineViewLifecycleTests
    {
        TallowLineView view;CameraRig rig;PartyController party;Vector3 ordinary,ordinaryOffset;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("TallowApproach");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;Assert.IsFalse(SceneFlow.Current.Loading);
            party=PartyController.Current;view=Object.FindFirstObjectByType<TallowLineView>();rig=Object.FindFirstObjectByType<CameraRig>();
            Assert.IsNotNull(view);view.enabled=false;
            Place(party.Active,view.drift.observation.position+new Vector3(-1,0,-3));
            Place(party.members.First(h=>h!=party.Active),view.drift.observation.position+new Vector3(3,0,-6));
            Physics.SyncTransforms();yield return new WaitForSecondsRealtime(2.5f);
            ordinary=Camera.main.transform.position;ordinaryOffset=ordinary-party.Active.transform.position;
            view.enabled=true;yield return new WaitForSecondsRealtime(2);
            Assert.Greater(Vector3.Distance(ordinary,Camera.main.transform.position),3,"fixture did not acquire a visible scene interest");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 position)
        {
            actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=position;cc.enabled=true;
            actor.GetComponent<FlightMotor>().Move(Vector2.up,false,false);actor.GetComponent<FlightMotor>().Halt();
        }
        IEnumerator Returns()
        {
            var before=Camera.main.transform.position;yield return null;
            Assert.Less(Vector3.Distance(before,Camera.main.transform.position),2,"scene interest release snaps in one frame");
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.Less(Vector3.Distance(ordinary,Camera.main.transform.position),.75f,"scene interest retains the camera after release");
        }
        [UnityTest] public IEnumerator DisablingReleasesAndReenablingReacquires()
        {
            view.enabled=false;yield return Returns();view.enabled=true;yield return new WaitForSecondsRealtime(2);
            Assert.Greater(Vector3.Distance(ordinary,Camera.main.transform.position),3,"reenabled scene never reacquires the view");
        }
        [UnityTest] public IEnumerator CompletedObservationReleasesTheView()
        {GameServices.Current.Flags.SetBool("tallow.lineObserved",true);yield return Returns();}
        [UnityTest] public IEnumerator ReplacingTheGameStateReleasesTheView()
        {GameServices.Current.State=new GameState();yield return Returns();}
        [UnityTest] public IEnumerator LeavingThePocketReturnsToMovingHeroFollow()
        {
            Place(party.Active,party.Active.transform.position+Vector3.right*25);yield return new WaitForSecondsRealtime(2.5f);
            Assert.Less(Vector3.Distance(ordinaryOffset,Camera.main.transform.position-party.Active.transform.position),.75f,"camera remains at the abandoned story assembly");
        }
        [UnityTest] public IEnumerator PauseFreezesTheStoryCameraAndMarkerThenResumes()
        {
            // Pause an actual transition. A settled view need not move while
            // the marker is still inside the assembly's existing envelope.
            view.enabled=false;yield return Returns();view.enabled=true;yield return null;
            GameTime.Paused=true;yield return null;yield return null;var camera=Camera.main.transform.position;var marker=view.drift.marker.position;
            yield return new WaitForSecondsRealtime(.5f);
            Assert.Less(Vector3.Distance(camera,Camera.main.transform.position),.001f,"paused story view keeps easing");
            Assert.Less(Vector3.Distance(marker,view.drift.marker.position),.001f,"paused marker keeps moving");
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(1);
            Assert.Greater(Vector3.Distance(marker,view.drift.marker.position),.25f,"marker did not resume");
            Assert.Greater(Vector3.Distance(camera,Camera.main.transform.position),.03f,"story framing did not resume");
        }
        [UnityTest] public IEnumerator SwapKeepsTheStoryViewContinuous()
        {
            var before=Camera.main.transform.position;Assert.IsTrue(party.Swap());
            foreach(var actor in party.members)Place(actor,actor.transform.position);
            yield return null;Assert.Less(Vector3.Distance(before,Camera.main.transform.position),2,"swap resets the scene view");
            yield return new WaitForSecondsRealtime(1);Assert.Greater(Vector3.Distance(ordinary,Camera.main.transform.position),3);
        }
        [UnityTest] public IEnumerator CombatFramingTakesPriorityThenReturnsToTheStory()
        {
            party.enabled=false;var story=Camera.main.transform.position;
            var combat=new Bounds(party.Active.transform.position+Vector3.right*65,new Vector3(24,8,24));
            rig.FrameEncounter(combat);yield return new WaitForSecondsRealtime(2);
            for(int i=0;i<8;i++)
            {
                var p=Camera.main.WorldToViewportPoint(combat.center+Vector3.Scale(combat.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                Assert.Greater(p.z,0);Assert.That(p.x,Is.InRange(.05f,.95f));Assert.That(p.y,Is.InRange(.05f,.95f));
            }
            var fighting=Camera.main.transform.position;Assert.Greater(Vector3.Distance(story,fighting),25);
            rig.FrameEncounter(null);yield return new WaitForSecondsRealtime(2);
            Assert.Greater(Vector3.Distance(fighting,Camera.main.transform.position),25,"completed combat retains priority over story");
            Assert.Less(Vector3.Distance(story,Camera.main.transform.position),3,"story does not reacquire after combat");
        }
        [UnityTest] public IEnumerator AnUnrenewedSceneLeaseExpiresWithoutTheCallerReleasingIt()
        {
            view.enabled=false;yield return Returns();var owner=new GameObject("one-shot interest owner");
            var bounds=new Bounds(party.Active.transform.position+Vector3.forward*20,new Vector3(28,8,30));float until=Time.unscaledTime+2;
            while(Time.unscaledTime<until){rig.FrameSceneInterest(owner,bounds,new Rect(.05f,.45f,.9f,.5f));yield return null;}
            Assert.Greater(Vector3.Distance(ordinary,Camera.main.transform.position),10,"temporary lease never framed its geometry");
            yield return Returns();Object.Destroy(owner);
        }
    }
}
