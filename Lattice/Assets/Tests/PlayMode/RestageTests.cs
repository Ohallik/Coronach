using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Every piece a staging step says it leaves in a scene is in the built scene, as the player loads it. Scenes are build output and chapters stage into
    /// each other's afterwards (Lattice.EditorTools.Restage); a rebuild of the earlier scene drops a later chapter's piece without a word, and the route that walks
    /// to it fails an hour into the chain. One case per scene any step declares something for, gathered from the steps' own [Restage.Step] attributes, so a new
    /// step is checked without anyone adding it here. The editor assembly is read by reflection: the test assembly doesn't reference it.</summary>
    public sealed class RestageTests
    {
        const string Runner="Lattice.EditorTools.Restage";
        static System.Type Type=>System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(Runner,false)).FirstOrDefault(t=>t!=null);
        static string[] Call(string method,params object[] args)=>(string[])Type.GetMethod(method).Invoke(null,args);
        /// <summary>The cases: every scene a step leaves something in. Without the editor assembly there is one case, and it fails saying so.</summary>
        static string[] Scenes()=>Type==null?new[]{"(no "+Runner+")"}:Call("StagedScenes");
        static string[] Missing(string zone)=>Call("Missing",SceneManager.GetSceneByName(zone));

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Assert.IsTrue(SceneManager.GetSceneByName(zone).isLoaded,zone+" isn't the scene that loaded");
            yield return null;
        }
        static string Restage(string zone)=>$"restage it: & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.Restage.All -Marker RESTAGE_OK -Graphics -TimeoutSec 3000 -ExtraArgs @('-restage-scenes','{zone}')";

        [UnityTest] public IEnumerator EveryPieceItsStagingLeavesIsInTheBuiltScene([ValueSource(nameof(Scenes))] string zone)
        {
            Assert.IsNotNull(Type,Runner+" isn't loaded, so nothing says what the scenes should carry");
            yield return Zone(zone);
            var missing=Missing(zone);
            Assert.IsEmpty(missing,$"{zone} lacks {missing.Length} piece(s) its staging steps leave ({string.Join(", ",Call("StepsFor",zone))}):\n  {string.Join("\n  ",missing)}\nA rebuild of {zone} drops them; {Restage(zone)}");
        }

        /// <summary>The check above can fail, and says what is missing and whose it is: a staged scene with one step's warp taken out and another's spawn put in twice.
        /// The broken state is made here, in the loaded scene; nothing on disk changes.</summary>
        [UnityTest] public IEnumerator APieceTakenOutOfAStagedSceneFailsByNameAndAPieceMadeTwiceFailsByCount()
        {
            Assert.IsNotNull(Type,Runner+" isn't loaded");
            const string zone="TallowApproach",warp="Warp — Xylos Nearspace",spawn="Spawn_Wendmire";
            yield return Zone(zone);
            var scene=SceneManager.GetSceneByName(zone);
            var before=Missing(zone);
            Assert.IsFalse(before.Any(m=>m.Contains(warp)||m.Contains(spawn)),"fixture: the built approach already lacks the pieces this takes out: "+string.Join("; ",before));
            var everything=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var taken=everything.Where(t=>t.name==warp).ToArray();
            Assert.AreEqual(1,taken.Length,"fixture: the approach should have one "+warp);
            var doubled=everything.Single(t=>t.name==spawn).gameObject;
            foreach(var t in taken)Object.DestroyImmediate(t.gameObject);
            var twin=Object.Instantiate(doubled);twin.name=spawn;SceneManager.MoveGameObjectToScene(twin,scene);
            yield return null;
            var after=Missing(zone).Except(before).ToArray();
            Assert.AreEqual(2,after.Length,"taking out one piece and doubling another should fail on exactly those two: "+string.Join("; ",after));
            StringAssert.Contains(warp,after.Single(m=>m.Contains(warp)));
            StringAssert.Contains("chapter3.tallow-warp",after.Single(m=>m.Contains(warp)),"the failure names the step that leaves the warp");
            StringAssert.Contains("XylosBuilder.StageApproach",after.Single(m=>m.Contains(warp)),"the failure names the method to run");
            StringAssert.Contains("found 2",after.Single(m=>m.Contains(spawn)),"a piece made twice fails by its count");
            StringAssert.Contains("chapter4.tallow-warp",after.Single(m=>m.Contains(spawn)));
        }
    }
}
