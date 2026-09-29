using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class DefeatFinishTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Kill(Health body)=>body.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=body.maximum*100,type=DamageType.Pulse});

        [UnityTest] public IEnumerator CorpsesLeaveThroughAVisibleCleanupNotAPop()
        {
            yield return Load("Arena_Ground");
            foreach(string id in new[]{"Ridgehound","SentinelHusk","Scrapmite"})
            {
                var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),new Vector3(0,0,-12));enemy.Passive=true;yield return null;
                var root=enemy.transform;float full=root.localScale.x,last=1;bool partial=false;
                Kill(enemy.Health);float deadline=Time.unscaledTime+6;
                while(enemy!=null&&Time.unscaledTime<deadline)
                {
                    float scale=root.localScale.x/full;if(scale>.15f&&scale<.85f)partial=true;last=scale;yield return null;
                }
                Assert.IsTrue(enemy==null,id+" corpse was never cleaned up");
                Assert.IsTrue(partial,id+" left in a single frame instead of a visible cleanup");
                Assert.Less(last,.3f,id+" popped out of existence from full size");
            }
        }
        [UnityTest] public IEnumerator KilledFliersTumbleAndFallBelowTheFlightPlane()
        {
            yield return Load("Arena_Flight");
            foreach(string id in new[]{"ChoristerDart","ChoristerDrifter"})
            {
                var flier=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),new Vector3(40,1,-20));flier.Passive=true;yield return null;yield return null;
                var body=flier.GetComponent<DefeatPresentation>().visual;Assert.IsNotNull(body,id+" has no presented body");
                var start=body.position;var up=body.up;
                Kill(flier.Health);yield return new WaitForSecondsRealtime(1.3f);
                Assert.IsTrue(flier!=null,id+" vanished before its failure finished");
                Assert.Greater(start.y-body.position.y,1.2f,id+" hovers on the flight plane after death");
                Assert.Greater(Vector3.Angle(up,body.up),90,id+" barely tilts after death");
                float deadline=Time.unscaledTime+4;while(flier!=null&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(flier==null,id+" was never cleaned up");
            }
        }
        [UnityTest] public IEnumerator AForcedSwapCoversTheDownedPartnerInsteadOfJoking()
        {
            yield return Load("Arena_Ground");
            var party=PartyController.Current;var first=party.Active;string speaker=null,line=null;
            System.Action<string,string> heard=(who,what)=>{speaker=who;line=what;};BarkService.Spoken+=heard;
            try
            {
                first.Health.Receive(new DamagePacket{amount=first.Health.maximum*5,type=DamageType.Kinetic});
                float deadline=Time.unscaledTime+2;while(party.Active==first&&Time.unscaledTime<deadline)yield return null;
                Assert.AreNotSame(first,party.Active,"the partner never took over");
                var partner=party.Active.character;
                Assert.AreEqual(partner,speaker);
                Assert.IsNotNull(BarkService.Line(partner,"cover"),"no authored cover line");
                Assert.AreEqual(BarkService.Line(partner,"cover"),line,"the partner joked while the other hero was down");

                // An ordinary player swap keeps its banter.
                first.Health.Heal(first.Health.maximum);yield return new WaitForSecondsRealtime(CombatActor.ReviveDuration+.3f);
                line=null;Assert.IsTrue(party.Swap());
                Assert.AreEqual(BarkService.Line(first.character,"swap"),line);
            }
            finally{BarkService.Spoken-=heard;}
        }
    }
}
