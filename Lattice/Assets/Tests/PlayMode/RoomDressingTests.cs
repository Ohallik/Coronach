using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Room and outpost dressing (Decks, Tallow, Sorrel) against D119 and the station's construction.</summary>
    public sealed class RoomDressingTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Assert.AreEqual(zone,SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(.3f);
        }
        static Transform Dressing()=>SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Room dressing").transform;
        static Vector3 Arrival()=>Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).First(s=>s.id=="Arrival").transform.position;
        static void ClearParty(){foreach(var c in Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None))c.enabled=false;}

        [UnityTest] public IEnumerator DressingNeverHidesAHeroFromTheFixedCamera([Values("Hub_Decks","TallowDrift","Sorrel_Ridges")] string zone)
        {
            yield return Load(zone);ClearParty();
            var root=Dressing();
            Assert.Greater(root.GetComponentsInChildren<MeshRenderer>().Length,15,zone+" has almost no dressing");
            var report=SightlineAudit.Run(root,Arrival());
            Assert.Greater(report.samples,250,zone+": the sweep sampled too little floor");
            Assert.IsEmpty(report.hidden,zone+": dressing hides the hero:\n"+report);
        }

        [UnityTest] public IEnumerator TheSightlineAuditCatchesATallPieceOnOpenFloor()
        {
            // Negative control: a 2.4 m crate stack dropped on the open promenade must be reported.
            yield return Load("Hub_Decks");ClearParty();
            var root=Dressing();
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name="Negative control stack";
            Object.Destroy(block.GetComponent<Collider>());
            block.transform.SetParent(root,false);block.transform.position=new Vector3(-20,1.2f,-4.6f);block.transform.localScale=new Vector3(1.2f,2.4f,1.2f);
            var report=SightlineAudit.Run(root,Arrival());
            Object.DestroyImmediate(block);
            Assert.IsTrue(report.offenders.Contains("Negative control stack"),"the audit missed a 2.4 m stack on open floor:\n"+report);
        }

        [UnityTest] public IEnumerator PrivateCabinsAreClosedToTheOfficeAndGallery()
        {
            yield return Load("Hub_Decks");
            // The 16 m cross walls left side gaps into the four cabins behind closed hatches.
            // Probe lines run through each gap between furniture, stopping short of the bunks inside.
            var gaps=new[]{(new Vector3(-38.5f,.5f,8.8f),new Vector3(-38.5f,.5f,10.9f)),(new Vector3(-37.85f,.5f,20.2f),new Vector3(-37.85f,.5f,18.3f)),
                (new Vector3(-18.15f,.5f,8.8f),new Vector3(-18.15f,.5f,10.9f)),(new Vector3(-18.15f,.5f,20.2f),new Vector3(-18.15f,.5f,18.3f))};
            foreach(var (a,b) in gaps)Assert.IsTrue(Physics.Linecast(a,b,out _,~0,QueryTriggerInteraction.Ignore),"cabin side gap is open at "+a);
            // Negative control: without the closing walls the same probes pass straight through.
            var walls=Dressing().Cast<Transform>().Where(t=>t.name=="Cabin end wall").ToArray();
            Assert.AreEqual(4,walls.Length,"expected four cabin end walls");
            foreach(var w in walls)w.gameObject.SetActive(false);Physics.SyncTransforms();
            foreach(var (a,b) in gaps)Assert.IsFalse(Physics.Linecast(a,b,out var hit,~0,QueryTriggerInteraction.Ignore),"control: something else closes the gap at "+a+" ("+(hit.collider!=null?hit.collider.name:"")+")");
            foreach(var w in walls)w.gameObject.SetActive(true);
        }
    }
}
