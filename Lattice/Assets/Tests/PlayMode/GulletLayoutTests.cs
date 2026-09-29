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
    public sealed class GulletLayoutTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Gullet_Tunnel");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        [UnityTest] public IEnumerator TheTravelLineClearsTissueAndOnlyValvesCloseIt()
        {
            // A hull-sized sphere swept along the whole line touches nothing but
            // the three closed valve membranes.
            var membranes=Object.FindObjectsByType<Membrane>(FindObjectsSortMode.None);
            Assert.AreEqual(3,membranes.Length,"the Gullet needs its three valve membranes");
            var blockers=new System.Collections.Generic.List<string>();
            for(float z=20;z<GulletProfile.ExitZ-8;z+=4)
            {
                var from=new Vector3(GulletProfile.RouteX(z),1,z);var to=new Vector3(GulletProfile.RouteX(z+4),1,z+4);
                foreach(var hit in Physics.SphereCastAll(from,2.6f,(to-from).normalized,(to-from).magnitude,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.collider.GetComponentInParent<Membrane>()!=null)continue;
                    if(hit.collider.attachedRigidbody!=null||hit.collider is CharacterController)continue;
                    blockers.Add($"{hit.collider.name} at z={z:0}");
                }
            }
            Assert.IsEmpty(blockers,"tissue or props block the travel line:\n"+string.Join("\n",blockers.Distinct().Take(12)));
            yield return null;
        }
        [UnityTest] public IEnumerator ValvesChambersEddyAndCoilHaveTheirAnatomy()
        {
            foreach(var membrane in Object.FindObjectsByType<Membrane>(FindObjectsSortMode.None))
            {
                float valve=GulletProfile.Valves.OrderBy(v=>Mathf.Abs(v-membrane.transform.position.z)).First();
                Assert.Less(Mathf.Abs(valve-membrane.transform.position.z),1,membrane.name+" is not at a valve");
                var bounds=membrane.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
                Assert.GreaterOrEqual(bounds.min.x,GulletProfile.LeftEdge(valve)-.6f,membrane.name+" pokes out of the left wall");
                Assert.LessOrEqual(bounds.max.x,GulletProfile.RightEdge(valve)+.6f,membrane.name+" pokes out of the right wall");
                Assert.Greater(bounds.size.x,GulletProfile.Width(valve)*.85f,membrane.name+" leaves a gap in its valve");
            }
            var cache=Object.FindFirstObjectByType<SalvageField>();Assert.IsNotNull(cache);
            float cz=cache.transform.position.z;
            Assert.Greater(cache.transform.position.x,GulletProfile.Center(cz)+GulletProfile.Right(cz)*.5f,"Neve's cache is not in the eddy pocket");
            var coil=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.name=="Gullet_Cantor");
            Assert.GreaterOrEqual(coil.GetComponent<BoxCollider>().size.x,60,"the Cantor has no room to turn");
            Assert.GreaterOrEqual(GulletProfile.Width(coil.transform.position.z),70);
            foreach(int i in new[]{0,1,2})
            {
                var chamber=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.name=="Gullet_Chamber_"+i);
                Assert.Less(chamber.transform.position.z,GulletProfile.Valves[i],"chamber "+i+" is not before its own valve");
            }
            yield return null;
        }
    }
}
