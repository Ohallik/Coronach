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
    public sealed class TallowReelVisibilityTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();DevLoadout.Apply("starter");GameServices.Current.Flags.SetBool("met.Keeper",true);SceneFlow.Current.LoadZone("TallowApproach");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);Camera.main.aspect=16f/9;
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
            foreach(var mesh in meshes)for(int i=0;i<8;i++)
            {
                var b=mesh.bounds;var p=Camera.main.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                Assert.Greater(p.z,0);Assert.That(p.x,Is.InRange(.025f,.975f),root.name+" cropped horizontally");Assert.That(p.y,Is.InRange(.025f,.975f),root.name+" cropped vertically");
                minimum=Vector2.Min(minimum,p);maximum=Vector2.Max(maximum,p);
            }
            return Rect.MinMaxRect(minimum.x,minimum.y,maximum.x,maximum.y);
        }
        static bool ClipAxis(float from,float direction,float minimum,float maximum,ref float enter,ref float leave)
        {
            if(Mathf.Abs(direction)<.000001f)return from>=minimum&&from<=maximum;
            float a=(minimum-from)/direction,b=(maximum-from)/direction;
            enter=Mathf.Max(enter,Mathf.Min(a,b));leave=Mathf.Min(leave,Mathf.Max(a,b));return enter<=leave;
        }
        static bool LineCrosses(LineRenderer line,Rect ship)
        {
            for(int i=1;i<line.positionCount;i++)
            {
                var a=line.transform.TransformPoint(line.GetPosition(i-1));var b=line.transform.TransformPoint(line.GetPosition(i));
                var p=Camera.main.WorldToViewportPoint(a);var q=Camera.main.WorldToViewportPoint(b);Assert.Greater(p.z,0);Assert.Greater(q.z,0);
                float radius=line.startWidth*.5f;
                float rx=Mathf.Abs(Camera.main.WorldToViewportPoint(a+Camera.main.transform.right*radius).x-p.x);
                float ry=Mathf.Abs(Camera.main.WorldToViewportPoint(a+Camera.main.transform.up*radius).y-p.y);
                float enter=0,leave=1;
                if(ClipAxis(p.x,q.x-p.x,ship.xMin-rx,ship.xMax+rx,ref enter,ref leave)&&
                    ClipAxis(p.y,q.y-p.y,ship.yMin-ry,ship.yMax+ry,ref enter,ref leave))return true;
            }
            return false;
        }
        IEnumerator Watch(string active,bool checkLine=false)
        {
            var party=PartyController.Current;if(party.Active.character!=active)Assert.IsTrue(party.Swap());
            var point=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).Single(p=>p.flag=="tallow.lineObserved");
            Place(party.Active,point.transform.position+new Vector3(-1,0,-3));Place(party.members.First(h=>h!=party.Active),point.transform.position+new Vector3(3,0,-6));
            Physics.SyncTransforms();yield return new WaitForSecondsRealtime(2);
            Assert.IsTrue(point.Available);var reel=GameObject.Find("Port docking reel").transform;
            var ships=party.members.Select(h=>h.GetComponent<FormController>().flight.transform).ToArray();
            var cable=GameObject.Find("Slack docking line").GetComponent<LineRenderer>();
            int samples=0,overlaps=0,lineOverlaps=0;float end=Time.unscaledTime+3;
            while(Time.unscaledTime<end)
            {
                yield return null;var area=Footprint(reel);
                foreach(var ship in ships)
                {var footprint=Footprint(ship);if(area.Overlaps(footprint))overlaps++;if(LineCrosses(cable,footprint))lineOverlaps++;}
                samples++;
            }
            TallowSnapshot.View(active+(checkLine?"-attached-line":"-reel-separation"));
            if(!checkLine&&!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("CORONACH_TALLOW_CAPTURE")))
            {
                var camera=Camera.main;var position=camera.transform.position;bool orthographic=camera.orthographic;float size=camera.orthographicSize;
                try
                {
                    var fitting=GameObject.Find("Free docking-line fitting").GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
                    camera.orthographic=true;camera.orthographicSize=.42f;camera.transform.position=fitting.center-camera.transform.forward*5;
                    TallowSnapshot.View(active+"-coupler-close");
                }
                finally{camera.transform.position=position;camera.orthographic=orthographic;camera.orthographicSize=size;}
            }
            Debug.Log($"TALLOW_REEL_VISIBILITY active={active} samples={samples} projectedOverlaps={overlaps} lineOverlaps={lineOverlaps}");
            Assert.GreaterOrEqual(samples,100,"insufficient actual view samples");
            if(checkLine)Assert.AreEqual(0,lineOverlaps,"the attached cable crosses a ship silhouette");
            else Assert.AreEqual(0,overlaps,"a ship hides the actual docking reel in the reading pocket");
        }
        [UnityTest] public IEnumerator TarenCanReadTheReelBesideBothCraft()=>Watch("Taren");
        [UnityTest] public IEnumerator SelaCanReadTheReelBesideBothCraft()=>Watch("Sela");
        [UnityTest] public IEnumerator TarenCanSeeTheAttachedLineBetweenShipAndReel()=>Watch("Taren",true);
        [UnityTest] public IEnumerator SelaCanSeeTheAttachedLineBetweenShipAndReel()=>Watch("Sela",true);
    }
}
