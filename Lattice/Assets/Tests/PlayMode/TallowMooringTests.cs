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
    // Controlled blockout geometry/motion; ordinary launch/inspect/redock is separate.
    public sealed class TallowMooringTests
    {
        DiscoveryPoint point;TallowMarkerDrift drift;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("TallowApproach");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            point=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).SingleOrDefault(p=>p.flag=="tallow.lineObserved");
            Assert.IsNotNull(point,"Tallow has no actual slack-line observation");
            drift=Object.FindFirstObjectByType<TallowMarkerDrift>();Assert.IsNotNull(drift);
            foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static Bounds Bounds(Transform root)=>root.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
        void AtLine()
        {
            int i=0;foreach(var hero in PartyController.Current.members)
            {var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=point.transform.position+new Vector3(i++*4,0,-2);cc.enabled=true;}
            Physics.SyncTransforms();
        }
        [UnityTest] public IEnumerator BothHullsCanReachTheLineWithoutTakingTheDockPrompt()
        {
            var dock=Object.FindObjectsByType<DockingPad>(FindObjectsSortMode.None).Single();
            Assert.Greater(Vector3.Distance(point.transform.position,dock.transform.position),point.range+dock.range+2);
            foreach(string hero in new[]{"Taren","Sela"})
            {
                float radius=HeroCollision.HullRadius(hero);var center=point.transform.position;
                foreach(float x in new[]{-3f,0,3f})
                {
                    var from=center+new Vector3(x,0,-9);var to=center+new Vector3(x,0,0);
                    var hits=Physics.SphereCastAll(from,radius,(to-from).normalized,9,~0,QueryTriggerInteraction.Ignore);
                    Assert.IsFalse(hits.Any(h=>h.collider.GetComponentInParent<CombatActor>()==null),hero+" cannot approach the slack line");
                }
            }
            foreach(var collider in drift.GetComponentsInChildren<Collider>())Assert.Less(collider.bounds.max.y,-1.8f,"service arm enters the flight lane");
            yield return null;
        }
        [UnityTest] public IEnumerator CableHasAFreeFittingAndNoHiddenAnchorOrDamageCollider()
        {
            var cable=GameObject.Find("Slack docking line").GetComponent<LineRenderer>();
            var fitting=GameObject.Find("Free docking-line fitting");var reel=GameObject.Find("Port docking reel");
            Assert.IsFalse(cable.loop);Assert.GreaterOrEqual(cable.positionCount,6);
            Assert.Less(Vector3.Distance(cable.transform.TransformPoint(cable.GetPosition(cable.positionCount-1)),fitting.transform.position),.01f);
            float length=0;for(int i=1;i<cable.positionCount;i++)length+=Vector3.Distance(cable.GetPosition(i-1),cable.GetPosition(i));
            float direct=Vector3.Distance(cable.GetPosition(0),cable.GetPosition(cable.positionCount-1));Assert.Greater(length,direct*1.5f,"line is taut rather than slack");
            Assert.IsEmpty(fitting.GetComponentsInChildren<Collider>());Assert.IsEmpty(drift.marker.GetComponentsInChildren<Collider>());
            Assert.Greater(Vector3.Distance(drift.marker.position,fitting.transform.position),6,"free line appears tied to the marker");
            var platform=Bounds(GameObject.Find("Mooring reel platform").transform);var machinery=Bounds(reel.transform);
            Assert.LessOrEqual(machinery.min.y,platform.max.y+.08f,"reel floats above its platform");Assert.Greater(machinery.max.y,platform.max.y+.7f);
            Assert.Greater(Bounds(drift.marker).size.y,2,"detached marker cannot be read from a ship");
            yield return null;
        }
        [UnityTest] public IEnumerator MarkerFallsBehindAndStopsDuringPause()
        {
            AtLine();yield return null;var before=drift.marker.position;
            yield return new WaitForSecondsRealtime(.8f);var after=drift.marker.position;
            Assert.Less(after.z,before.z-.18f);Assert.Less(Mathf.Abs(after.x-before.x),.001f);Assert.Less(Mathf.Abs(after.y-before.y),.001f);
            GameTime.Paused=true;yield return new WaitForSecondsRealtime(.6f);
            Assert.Less(Vector3.Distance(after,drift.marker.position),.001f,"marker moves through pause");GameTime.Paused=false;
            yield return new WaitForSecondsRealtime(.4f);Assert.Less(drift.marker.position.z,after.z-.08f);
        }
        [UnityTest] public IEnumerator MarkerTravelRemainsBoundedWithoutReturningTowardTheRefuge()
        {
            drift.maxTravel=.4f;AtLine();yield return new WaitForSecondsRealtime(2);
            Assert.That(drift.Travel,Is.InRange(.399f,.401f));var final=drift.marker.position;
            yield return new WaitForSecondsRealtime(.6f);Assert.Less(Vector3.Distance(final,drift.marker.position),.001f);
        }
    }
}
