using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The hit-stop's clock (S1 Impact). The heroes run on real time and the world on scaled time, so a stop has to hold both: an earlier sample that only set the time
    /// scale froze the enemy while the hero swung on (D056). A pause outranks a stop and ends it, a Flash's slow motion comes back after one, and two are the longer of them.</summary>
    public sealed class ImpactClockTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();ImpactSettings.Reset();ImpactSettings.Volatile=true;
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Ground");
            float deadline=Time.unscaledTime+15;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture load timed out");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.5f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();ImpactSettings.Reset();
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator UntilTheStopEnds()
        {for(float end=Time.unscaledTime+2;GameTime.Stopped&&Time.unscaledTime<end;)yield return null;}

        [UnityTest] public IEnumerator AStopHoldsTheWorldAndTheHeroesClockThenLetsGo()
        {
            float now=GameTime.Now,real=Time.unscaledTime;
            GameTime.Stop(.1f);
            Assert.AreEqual(0,Time.timeScale,"a stop leaves the world running");
            Assert.IsTrue(GameTime.Stopped,"a stop just asked for isn't one yet");
            int held=0;float world=0;
            for(float end=Time.unscaledTime+2;Time.unscaledTime<end;)
            {
                yield return null;
                if(!GameTime.Stopped)break;
                held++;world+=Time.deltaTime;
                Assert.AreEqual(now,GameTime.Now,.0005f,"the heroes' clock ran on through a held frame: their swings and timers go on while the enemy stands frozen");
                Assert.AreEqual(0,GameTime.Delta,"a hero's frame was counted through a held frame");
            }
            float took=Time.unscaledTime-real;
            Assert.Greater(held,0,"fixture: no frame was held");
            Assert.AreEqual(0,world,"the world's clock moved in a held frame");
            Assert.AreEqual(1,Time.timeScale,"the clock doesn't come back after a stop");
            Assert.GreaterOrEqual(took,.1f,"a tenth of a second's stop let go early");
            // The first free frame is on the heroes' clock again, and what was held is gone from it for good.
            Assert.Greater(GameTime.Delta,0,"the heroes' clock stays held after the stop");
            Assert.GreaterOrEqual(took-(GameTime.Now-now),.099f,$"the stop held {took-(GameTime.Now-now):0.000} s of the heroes' clock, not its tenth");
        }
        [UnityTest] public IEnumerator TheHeroesClockNeverStepsBackThroughStopsAndPauses()
        {
            // Held, the clock is one growing number taken from another. Their rounding stepped it back a float's width, and a shot was recorded as released a negative time ago.
            float last=GameTime.Now;int frames=0;
            for(int round=0;round<6;round++)
            {
                GameTime.Stop(.12f);
                for(float end=Time.unscaledTime+.2f;Time.unscaledTime<end;)
                {yield return null;float now=GameTime.Now;Assert.GreaterOrEqual(now,last,$"the heroes' clock stepped back {last-now:0.0000000} s in frame {frames}, through a stop");last=now;frames++;}
                GameTime.Paused=true;
                for(float end=Time.unscaledTime+.1f;Time.unscaledTime<end;)
                {yield return null;float now=GameTime.Now;Assert.GreaterOrEqual(now,last,$"the heroes' clock stepped back {last-now:0.0000000} s in frame {frames}, behind a menu");last=now;frames++;}
                GameTime.Paused=false;
            }
            Assert.Greater(frames,30,"fixture: too few frames were read");
        }
        [UnityTest] public IEnumerator APauseOutranksAStopAndEndsIt()
        {
            GameTime.Stop(.2f);yield return null;
            Assert.Greater(GameTime.StopLeft,0,"fixture: the stop outlasts a frame");
            GameTime.Paused=true;
            Assert.AreEqual(0,Time.timeScale);Assert.IsFalse(GameTime.Stopped,"a paused game says it is in a hit-stop");
            Assert.AreEqual(0,GameTime.StopLeft,"a stop waits behind the menu to hold the game again when it closes");
            float now=GameTime.Now;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(now,GameTime.Now,.0005f,"the heroes' clock ran behind the menu");
            GameTime.Paused=false;
            Assert.AreEqual(1,Time.timeScale,"the menu closed on a stopped clock: everything that waits on it after a pause would wait on a stop it can't see");
            Assert.IsFalse(GameTime.Stopped);
            yield return null;
            Assert.Greater(GameTime.Delta,0,"the heroes' clock stays held after the pause");
            // A stop asked for behind the menu is not one either.
            GameTime.Paused=true;GameTime.Stop(.2f);
            Assert.AreEqual(0,GameTime.StopLeft,"a stop can be asked for behind a menu");
            GameTime.Paused=false;
            Assert.AreEqual(1,Time.timeScale);
        }
        [UnityTest] public IEnumerator AFlashComesBackAfterAStop()
        {
            GameTime.BeginFlash();Assert.AreEqual(.25f,Time.timeScale,.001f,"fixture: a Flash");
            GameTime.Stop(.05f);Assert.AreEqual(0,Time.timeScale,"a hit in a Flash has no stop");
            yield return UntilTheStopEnds();
            Assert.AreEqual(.25f,Time.timeScale,.001f,"a hit landed in a Flash ended its slow motion");
            GameTime.EndFlash();Assert.AreEqual(1,Time.timeScale,.001f);
        }
        [UnityTest] public IEnumerator TwoStopsAreTheLongerOfThemAndResetClearsOne()
        {
            GameTime.Stop(.1f);GameTime.Stop(.04f);
            Assert.AreEqual(.1f,GameTime.StopLeft,.0001f,"a shorter stop cut a longer one short");
            GameTime.Stop(.1f);
            Assert.AreEqual(.1f,GameTime.StopLeft,.0001f,"two stops added up: three bodies under one blade held the game three times as long");
            GameTime.Stop(5);
            Assert.AreEqual(GameTime.MostStop,GameTime.StopLeft,.0001f,"a stop can hold the game for as long as anyone asks");
            GameTime.Reset();
            Assert.AreEqual(1,Time.timeScale,"a zone change in a stop leaves the world frozen");
            Assert.IsFalse(GameTime.Stopped);Assert.AreEqual(0,GameTime.StopLeft);
            yield return null;
            Assert.Greater(GameTime.Delta,0,"the heroes' clock stays held after a reset");
        }
        [UnityTest] public IEnumerator AHerosSwingTimersAndFeetHoldThroughAStop()
        {
            var hero=PartyController.Current.Active;var rig=hero.GetComponentInChildren<Animator>();
            Assert.IsTrue(hero.Attack(),"fixture: a swing");
            yield return new WaitForSecondsRealtime(.08f);
            Assert.IsFalse(hero.CanAct,"fixture: mid swing");
            GameTime.Stop(.2f);
            // Two frames in, every pose has been told to hold.
            yield return null;yield return null;
            Assert.IsTrue(GameTime.Stopped,"fixture: the stop outlasts two frames");
            float phase=rig.GetCurrentAnimatorStateInfo(0).normalizedTime,left=hero.VisualAttackUntil-GameTime.Now;var at=hero.transform.position;
            int held=0;
            for(int frame=0;frame<4;frame++)
            {
                hero.motor.Move(Vector2.up,false,false);yield return null;
                if(!GameTime.Stopped)break;
                held++;
                Assert.AreEqual(phase,rig.GetCurrentAnimatorStateInfo(0).normalizedTime,.0005f,"the hero's swing plays on through a hit-stop while the body it struck stands frozen");
                Assert.AreEqual(left,hero.VisualAttackUntil-GameTime.Now,.0005f,"the swing's own timer runs through a hit-stop");
                Assert.AreEqual(0,hero.ActionDelta,"a skill's release is counted through a hit-stop");
                Assert.AreEqual(0,hero.MotorDelta,"the hero's feet are counted through a hit-stop");
                Assert.Less(Vector3.Distance(at,hero.transform.position),.001f,"the hero walks through a hit-stop");
            }
            Assert.Greater(held,0,"fixture: no held frame was sampled");
            yield return UntilTheStopEnds();
            yield return null;yield return null;yield return null;
            Assert.AreNotEqual(phase,rig.GetCurrentAnimatorStateInfo(0).normalizedTime,"the swing never took up again after the stop");
        }
        [UnityTest] public IEnumerator TheViewHoldsItsFramingThroughAStop()
        {
            // No kick, so the view may not move at all: the solver runs on real time and would ease in on a frozen hero.
            ImpactSettings.Shake=ImpactSettings.ShakeLevel.Off;
            var hero=PartyController.Current.Active;
            for(float end=Time.unscaledTime+.4f;Time.unscaledTime<end;){hero.motor.Move(Vector2.right,false,false);yield return null;}
            GameTime.Stop(.2f);yield return null;yield return null;
            Assert.IsTrue(GameTime.Stopped,"fixture: the stop outlasts two frames");
            var view=Camera.main.transform;var held=view.position;var angle=view.rotation;
            for(int frame=0;frame<5&&GameTime.Stopped;frame++)
            {
                yield return null;
                if(!GameTime.Stopped)break;
                Assert.Less(Vector3.Distance(held,view.position),.0005f,"the view drifts through a hit-stop");
                Assert.Less(Quaternion.Angle(angle,view.rotation),.001f);
            }
        }
    }
}
