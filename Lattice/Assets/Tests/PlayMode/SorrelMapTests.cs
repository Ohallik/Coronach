using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Lattice.Tests.PlayMode
{
    public sealed class SorrelMapTests
    {
        [UnitySetUp]public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Sorrel_Ridges");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.5f);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static bool Complete(Vector3 from,Vector3 to)
        {
            Assert.IsTrue(NavMesh.SamplePosition(from,out var start,1.5f,NavMesh.AllAreas),"start floor missing");
            Assert.IsTrue(NavMesh.SamplePosition(to,out var end,1.5f,NavMesh.AllAreas),"destination floor missing");
            var path=new NavMeshPath();bool complete=NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
            TestContext.WriteLine("Seal route "+path.status+": "+string.Join(" -> ",path.corners.Select(p=>p.ToString("F2"))));
            return complete;
        }
        [UnityTest]public IEnumerator OpeningTheDrillSealRestoresTheCompanionRouteInBothDirections()
        {
            Assert.IsNotNull(GroundNavigation.Current,"new exploration terrain requires companion paths");
            var gate=Object.FindObjectsByType<Membrane>(FindObjectsSortMode.None).Single(m=>m.name=="Drill cradle seal");
            var inside=new Vector3(0,0,184);var outside=new Vector3(0,0,195);
            Assert.IsFalse(Complete(inside,outside),"closed seal has a walkable bypass through its enclosing cliffs");
            gate.SetOpen(true);yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(Complete(inside,outside),"opened physical seal still blocks the companion navigation");
            Assert.IsTrue(Complete(outside,inside),"return path to repair must reopen too");
            gate.SetOpen(false);yield return new WaitForSecondsRealtime(.6f);
            Assert.IsFalse(Complete(inside,outside),"closing a seal must restore its navigation barrier");
        }
        [UnityTest]public IEnumerator ReclosingDuringDissolveRestoresTheWholeVisibleBarrier()
        {
            var gate=Object.FindObjectsByType<Membrane>(FindObjectsSortMode.None).Single(m=>m.name=="Drill cradle seal");
            var renderer=gate.GetComponentInChildren<Renderer>();var block=new MaterialPropertyBlock();
            gate.SetOpen(true);yield return new WaitForSecondsRealtime(.15f);
            renderer.GetPropertyBlock(block);Assert.Greater(block.GetVector("_DissolveParams").x,.05f,"exercise an actual partial dissolve");
            gate.SetOpen(false);yield return null;
            Assert.IsTrue(renderer.enabled,"closed seal must be visible");
            Assert.IsTrue(gate.GetComponent<Collider>().enabled,"closed seal must be solid");
            renderer.GetPropertyBlock(block);
            Assert.AreEqual(0,block.GetVector("_DissolveParams").x,"reclosed seal retains a partially erased visual surface");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(renderer.enabled,"old opening coroutine must not hide a reclosed seal");
        }
    }
}
