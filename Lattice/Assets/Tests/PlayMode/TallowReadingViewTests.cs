using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
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
    public sealed class TallowReadingViewTests
    {
        RenderTexture viewport;
        [UnitySetUp] public IEnumerator Boot()
        {
            Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"Tallow reading fixture requires -Graphics before allocating its rendered camera/UI target");
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("TallowApproach");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;Assert.IsFalse(SceneFlow.Current.Loading);
            // Hidden editor windows default to 640x480. Render the actual camera
            // and UI into an explicit player-sized target; no pose/profile edit.
            viewport=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);Assert.IsTrue(viewport.Create());
            Camera.main.targetTexture=viewport;Camera.main.aspect=16f/9;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(canvas.renderMode==RenderMode.ScreenSpaceOverlay)
                {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=.5f;}
            Canvas.ForceUpdateCanvases();yield return null;
            Assert.AreEqual(1920,Camera.main.pixelWidth);Assert.AreEqual(1080,Camera.main.pixelHeight);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();if(Camera.main!=null)Camera.main.targetTexture=null;if(viewport!=null){viewport.Release();Object.Destroy(viewport);}SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 position)
        {
            actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=position;cc.enabled=true;
            actor.GetComponent<FlightMotor>().Move(Vector2.up,false,false);actor.GetComponent<FlightMotor>().Halt();
        }
        static Rect UiBox(RectTransform rect)
        {
            var canvas=rect.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var low=Vector2.one*float.PositiveInfinity;var high=Vector2.one*float.NegativeInfinity;
            foreach(var corner in corners)
            {
                var pixel=RectTransformUtility.WorldToScreenPoint(camera,corner);var point=new Vector2(pixel.x/(camera!=null?camera.pixelWidth:Screen.width),pixel.y/(camera!=null?camera.pixelHeight:Screen.height));
                low=Vector2.Min(low,point);high=Vector2.Max(high,point);
            }
            return Rect.MinMaxRect(low.x,low.y,high.x,high.y);
        }
        static Rect Footprint(Renderer[] meshes)
        {
            var low=Vector2.one*float.PositiveInfinity;var high=Vector2.one*float.NegativeInfinity;
            foreach(var mesh in meshes)
            {
                var bounds=mesh.bounds;
                for(int corner=0;corner<8;corner++)
                {
                    var p=Camera.main.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                    Assert.Greater(p.z,0,"story geometry behind the camera");low=Vector2.Min(low,p);high=Vector2.Max(high,p);
                }
            }
            return Rect.MinMaxRect(low.x,low.y,high.x,high.y);
        }
        IEnumerator Read(string active)
        {
            Assert.Greater(Screen.width,0);Assert.Greater(Screen.height,0);
            var party=PartyController.Current;if(party.Active.character!=active)Assert.IsTrue(party.Swap());
            var point=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).Single(p=>p.flag=="tallow.lineObserved");
            Place(party.Active,point.transform.position+new Vector3(-1,0,-3));Place(party.members.First(h=>h!=party.Active),point.transform.position+new Vector3(3,0,-6));
            Physics.SyncTransforms();
            // The real replay watches, swaps and approaches before opening the
            // six-line observation. Keep that elapsed drift, then allow reading.
            yield return new WaitForSecondsRealtime(12);
            var drift=Object.FindFirstObjectByType<TallowMarkerDrift>();var start=drift.marker.position;
            DialogueSystem.Current.AutoAdvance=false;point.Interact();yield return null;yield return null;
            var panel=Object.FindFirstObjectByType<DialoguePanel>();Assert.IsTrue(panel.IsVisible);Assert.IsTrue(DialogueSystem.Current.Running);
            Canvas.ForceUpdateCanvases();var rects=panel.GetComponentsInChildren<RectTransform>(true);
            float coveredTop=Mathf.Max(UiBox(rects.Single(r=>r.name=="DialoguePanel")).yMax,UiBox(rects.Single(r=>r.name=="Portrait")).yMax);
            Assert.That(coveredTop,Is.InRange(.25f,.55f),"actual dialogue overlay fixture is not on the expected lower screen");
            var roots=new List<Transform>{drift.marker,GameObject.Find("Free docking-line fitting").transform,GameObject.Find("Port docking reel").transform,GameObject.Find("Slack docking line").transform};
            roots.AddRange(party.members.Select(h=>h.GetComponent<FormController>().flight.transform));
            var meshes=roots.Select(r=>r.GetComponentsInChildren<Renderer>().Where(m=>m.enabled&&m.gameObject.activeInHierarchy).ToArray()).ToArray();
            Assert.IsTrue(meshes.All(m=>m.Length>0));
            int samples=0,occluded=0,cropped=0;float minY=1,minSpan=1;string firstFailure=null;
            Capture(active+"-reading-start");bool middle=false;
            float began=Time.unscaledTime,until=began+32;
            while(Time.unscaledTime<until)
            {
                yield return null;samples++;
                if(!middle&&Time.unscaledTime-began>16){middle=true;Capture(active+"-reading-middle");}
                for(int i=0;i<roots.Count;i++)
                {
                    var rect=Footprint(meshes[i]);
                    if(rect.yMin<coveredTop+.025f){occluded++;firstFailure??=$"{roots[i].name} behind actual panel at {Time.unscaledTime-began:F3}s, minY={rect.yMin:R}, coveredTop={coveredTop:R}";}
                    if(rect.xMin<.025f||rect.xMax>.975f||rect.yMax>.975f){cropped++;firstFailure??=roots[i].name+" screen crop";}
                    if(i==0){minY=Mathf.Min(minY,rect.yMin);minSpan=Mathf.Min(minSpan,Mathf.Max(rect.width,rect.height));}
                }
            }
            Capture(active+"-reading-end");
            Debug.Log($"TALLOW_READING_VIEW active={active} screen={Screen.width}x{Screen.height} target={Camera.main.pixelWidth}x{Camera.main.pixelHeight} samples={samples} occluded={occluded} cropped={cropped} markerMinY={minY:R} minMarkerSpan={minSpan:R} coveredTop={coveredTop:R} first={firstFailure}");
            Assert.GreaterOrEqual(samples,1000,"reading interval lacks frame coverage");
            Assert.Less(drift.marker.position.z,start.z-9,"framing cannot pass by stopping or slowing the marker's real drift");
            Assert.IsFalse(GameServices.Current.Flags.GetBool(point.flag),"holding a line cannot complete its observation");
            Assert.AreEqual(0,occluded,firstFailure);Assert.AreEqual(0,cropped,firstFailure);Assert.GreaterOrEqual(minSpan,.035f);
        }
        void Capture(string name)
        {
            string requested=System.Environment.GetEnvironmentVariable("CORONACH_TALLOW_READING_CAPTURE");if(string.IsNullOrEmpty(requested))return;
            Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"reading capture needs a graphics-enabled runner");
            string root=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality"))+System.IO.Path.DirectorySeparatorChar;
            string folder=System.IO.Path.GetFullPath(requested);Assert.IsTrue((folder+System.IO.Path.DirectorySeparatorChar).StartsWith(root,System.StringComparison.OrdinalIgnoreCase));System.IO.Directory.CreateDirectory(folder);
            string file=System.IO.Path.Combine(folder,name+".png");Assert.IsFalse(System.IO.File.Exists(file),"preserve earlier reading images");
            var prior=RenderTexture.active;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try
            {
                Camera.main.Render();RenderTexture.active=viewport;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                var bytes=image.EncodeToPNG();Assert.Greater(bytes.Length,30000);System.IO.File.WriteAllBytes(file,bytes);
            }
            finally{RenderTexture.active=prior;Object.DestroyImmediate(image);}
        }
        [UnityTest] public IEnumerator TarenKeepsTheWholeStoryAboveTheDialogueDuringReading()=>Read("Taren");
        [UnityTest] public IEnumerator SelaKeepsTheWholeStoryAboveTheDialogueDuringReading()=>Read("Sela");
    }
}
