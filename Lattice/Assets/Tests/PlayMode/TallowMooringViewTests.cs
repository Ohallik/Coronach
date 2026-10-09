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
    public sealed class TallowMooringViewTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("TallowApproach");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;Assert.IsFalse(SceneFlow.Current.Loading);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 position)
        {
            actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=position;cc.enabled=true;
            actor.GetComponent<FlightMotor>().Move(Vector2.up,false,false);actor.GetComponent<FlightMotor>().Halt();
        }
        static Rect Footprint(Transform root)
        {
            var minimum=Vector2.one*float.PositiveInfinity;var maximum=Vector2.one*float.NegativeInfinity;
            var meshes=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();Assert.IsNotEmpty(meshes);
            foreach(var mesh in meshes)
            {
                var b=mesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=Camera.main.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    Assert.Greater(p.z,0);Assert.That(p.x,Is.InRange(.025f,.975f),root.name+" horizontal crop");Assert.That(p.y,Is.InRange(.025f,.975f),root.name+" vertical crop");
                    minimum=Vector2.Min(minimum,p);maximum=Vector2.Max(maximum,p);
                }
            }
            return Rect.MinMaxRect(minimum.x,minimum.y,maximum.x,maximum.y);
        }
        IEnumerator Watch(string active)
        {
            var party=PartyController.Current;if(party.Active.character!=active)Assert.IsTrue(party.Swap());
            var point=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).Single(p=>p.flag=="tallow.lineObserved");
            Place(party.Active,point.transform.position+new Vector3(-1,0,-3));Place(party.members.First(h=>h!=party.Active),point.transform.position+new Vector3(3,0,-6));
            Physics.SyncTransforms();yield return new WaitForSecondsRealtime(2);
            Assert.IsTrue(point.Available);var drift=Object.FindFirstObjectByType<TallowMarkerDrift>();
            var marker=Footprint(drift.marker);var fitting=Footprint(GameObject.Find("Free docking-line fitting").transform);
            Footprint(GameObject.Find("Port docking reel").transform);Footprint(GameObject.Find("Slack docking line").transform);
            foreach(var hero in party.members)
            {
                var ship=Footprint(hero.GetComponent<FormController>().flight.transform);
                Assert.IsFalse(ship.Overlaps(fitting),hero.character+" hides the free line fitting");
            }
            Assert.GreaterOrEqual(Mathf.Max(marker.width,marker.height),.035f,"detached marker is too small to track");
            Assert.GreaterOrEqual(Mathf.Max(fitting.width,fitting.height),.008f,"free line fitting is too small to find");
            var before=drift.marker.position;TallowSnapshot.View(active+"-watch-before");
            yield return new WaitForSecondsRealtime(4);Footprint(drift.marker);
            Assert.Less(drift.marker.position.z,before.z-1,"marker did not visibly fall behind during the watch");
            Debug.Log($"TALLOW_WATCH active={active} markerSpan={Mathf.Max(marker.width,marker.height):R} fittingSpan={Mathf.Max(fitting.width,fitting.height):R} markerStart={before} markerEnd={drift.marker.position}");
            TallowSnapshot.View(active+"-watch-after");TallowSnapshot.Construction(active);
        }
        [UnityTest] public IEnumerator TarenCanSeeTheLineAndDetachedMarker()=>Watch("Taren");
        [UnityTest] public IEnumerator SelaCanSeeTheLineAndDetachedMarker()=>Watch("Sela");
    }
}
