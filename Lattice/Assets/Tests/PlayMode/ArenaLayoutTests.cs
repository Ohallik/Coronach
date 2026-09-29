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
    /// <summary>The practice scenes are places with a clear working core: a
    /// proving yard and a ship proving berth (docs/maps/Arena_*.md).</summary>
    public sealed class ArenaLayoutTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");}
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.3f);
        }
        // Static scenery inside the working core, at body height.
        static string[] CoreObstacles(float y)=>Physics.OverlapBox(new Vector3(0,y,-4),new Vector3(10,.6f,14),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)
            .Where(c=>c.gameObject.isStatic&&c.GetComponentInParent<CombatActor>()==null&&c.GetComponentInParent<EnemyBrain>()==null).Select(c=>c.name).Distinct().ToArray();

        // The first static structure along a ray; bodies (heroes, opponents) are ignored.
        static RaycastHit? Structure(Vector3 from,Vector3 dir,float range)
        {
            var hits=Physics.RaycastAll(from,dir,range,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.isStatic).OrderBy(h=>h.distance).ToArray();
            return hits.Length>0?hits[0]:null;
        }
        [UnityTest] public IEnumerator TheProvingYardKeepsItsCoreClearAndItsOperatorOutOfTheFiringPath()
        {
            yield return Load("Arena_Ground");
            Assert.IsEmpty(CoreObstacles(1),"scenery obstructs the yard's working core");
            var hal=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.firstNode=="ArenaGuide");
            var h=hal.transform.position;
            Assert.IsFalse(Mathf.Abs(h.x)<10&&h.z>-20&&h.z<10,"Hal stands in the firing path at "+h);
            Assert.AreEqual(3,Object.FindObjectsByType<Spawner>(FindObjectsSortMode.None).Count(s=>Mathf.Approximately(s.transform.position.z,5)));
            // Every yard edge stops a walker: west outcrop, north backstop, east barrier, south crates.
            foreach(var dir in new[]{Vector3.left,Vector3.forward,Vector3.right})
                Assert.IsNotNull(Structure(new Vector3(0,1,0),dir,24),"the yard is open toward "+dir);
            Assert.IsNotNull(Structure(new Vector3(-12,1,-5),Vector3.back,24),"the south crate line does not stop a walker");
        }

        [UnityTest] public IEnumerator TheProvingBerthIsRailedWithItsGuideInTheCabin()
        {
            yield return Load("Arena_Flight");
            Assert.IsEmpty(CoreObstacles(1),"structure obstructs the berth's working core at the flight plane");
            // The collision boundary is visible structure: rails at the flight plane on every side.
            foreach(var (from,dir) in new[]{(new Vector3(0,1,0),Vector3.left),(new Vector3(0,1,0),Vector3.right),(new Vector3(0,1,0),Vector3.forward),(new Vector3(0,1,-30),Vector3.back),(new Vector3(0,1,-30),Vector3.left)})
            {
                var hit=Structure(from,dir,30);Assert.IsNotNull(hit,"the berth is open toward "+dir+" from "+from);
                string structure=hit.Value.collider.transform.root.name;
                Assert.That(structure,Is.EqualTo("Berth rail").Or.EqualTo("Berth post"),"the boundary toward "+dir+" is not the berth's visible rails and posts");
            }
            var hal=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.firstNode=="ArenaGuide");
            Assert.IsFalse(hal.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Hal stands on the flight plane instead of speaking from the cabin");
            var cabin=GameObject.Find("Control cabin");Assert.IsNotNull(cabin,"the berth has no control cabin");
            Assert.Less(Vector3.Distance(hal.transform.position,cabin.transform.position),16,"the call point is not at the cabin");
        }
    }
}
