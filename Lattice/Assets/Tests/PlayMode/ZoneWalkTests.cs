using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The walk the shared ...AcrossZones checks read every zone through (ZoneWalk): a zone that isn't built, a zone whose visit throws and a zone that logs an error
    /// each stay that zone's, and the walk goes on to the next.</summary>
    public sealed class ZoneWalkTests
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
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTest] public IEnumerator AWalkAcrossZonesReadsPastAZoneThatIsNotBuiltThrowsOrLogsAnError()
        {
            const string Check="ZoneWalkTests fixture";const string NotBuilt="ZoneWalkFixtureNotBuilt";
            int visits=0,recovered=0;
            IEnumerator Visit(string zone)
            {
                visits++;
                yield return Zone(zone);
                // What an unfinished chapter does to a walk: an error of its own as it loads (an unstaged track), or a check in the visit that can't go on.
                if(zone=="Arena_Ground")Debug.LogError("ZONE_WALK_FIXTURE an error the zone logs as it loads");
                if(zone=="Arena_Flight")Assert.Fail("ZONE_WALK_FIXTURE the visit can't go on");
                ZoneWalk.Keep(Check,zone,zone.Length);
            }
            IEnumerator Again(){recovered++;yield return Cleanup();yield return Boot();}
            var read=new Dictionary<string,int>();var unread=new List<string>();
            yield return ZoneWalk.Walk(Check,new[]{NotBuilt,"Arena_Ground","Arena_Flight","Hub_Decks"},Visit,Again,read,unread);
            Assert.IsFalse(LogAssert.ignoreFailingMessages,"the walk leaves errors ignored after it: the rest of the test can't fail on one");
            CollectionAssert.AreEqual(new[]{"Arena_Ground","Hub_Decks"},read.Keys.ToArray(),"the walk doesn't read every zone it can, past the ones it can't");
            Assert.AreEqual(3,visits,"a zone that isn't a scene in the build is loaded anyway, or one that is is never visited");
            Assert.AreEqual(1,recovered,"the game isn't put back once after the visit that threw");
            Assert.AreEqual(2,unread.Count,"the walk doesn't name the two zones it couldn't read: "+string.Join("; ",unread));
            StringAssert.Contains(NotBuilt,unread[0]);StringAssert.Contains("not a scene in the build",unread[0]);
            StringAssert.Contains("Arena_Flight",unread[1]);StringAssert.Contains("can't go on",unread[1]);
            // A second walk takes what was kept and visits nothing it already read: a whole class's run reads each zone once for its case and once more for nobody.
            visits=0;var again=new Dictionary<string,int>();var notAgain=new List<string>();
            yield return ZoneWalk.Walk(Check,new[]{"Arena_Ground","Hub_Decks"},Visit,Again,again,notAgain);
            Assert.AreEqual(0,visits,"the walk loads a zone that was already read this run");
            Assert.AreEqual(read["Hub_Decks"],again["Hub_Decks"],"the walk doesn't hand back what the zone's visit kept");
        }
    }
}
