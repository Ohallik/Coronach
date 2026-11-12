using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The QA pass's open findings on the flight view, words in the world and the Wick curtain (November 10). Each check fails on the build the findings were
    /// written against (e37c9b4), and says what the player saw go wrong.</summary>
    public sealed class QaNov10CameraSignTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members[1-PartyController.Current.index];
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Camera.main.aspect=16f/9;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(DialogueSystem.Current!=null&&!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        static void ClearFights(){foreach(var fight in All<EncounterVolume>())Flag("clear."+fight.encounterId);}
        static bool Inside(Vector3 view)=>view.z>0&&view.x>.02f&&view.x<.98f&&view.y>.02f&&view.y<.98f;
        /// <summary>How far back the flight view stands from the ship, along its own axis: its height over the ship over the sine of its pitch.</summary>
        static float Back(CameraRig rig,CombatActor ship)=>(Camera.main.transform.position.y-ship.transform.position.y)/Mathf.Sin(rig.profile.pitch*Mathf.Deg2Rad);

        // ------------------------------------------------------------------ E2-gullet-10: the flight view pumps
        /// <summary>A fight that needs less room than it had. On e37c9b4 the view came in the moment the bounds shrank, at 28 m a second: the ship closing on a far target pulled it in,
        /// and the next target out again (the final chain's Gullet: 75 m to 39 m in 1.35 s after a mine kill, then back out at the next).</summary>
        [UnityTest] public IEnumerator TheViewHoldsBackAWhileBeforeComingInMidFight()
        {
            yield return Zone("Arena_Flight");
            var party=PartyController.Current;party.enabled=false;var hero=Lead;hero.GetComponent<FlightMotor>().Halt();
            var rig=Object.FindFirstObjectByType<CameraRig>();yield return new WaitForSecondsRealtime(1);
            var ship=hero.transform.position;
            var near=new Bounds(ship+Vector3.forward*6,new Vector3(6,4,14));var far=new Bounds(ship+Vector3.forward*14,new Vector3(10,4,34));
            rig.FrameEncounter(far);yield return new WaitForSecondsRealtime(3.5f);
            float before=Back(rig,hero);
            rig.FrameEncounter(near);float lowest=before;
            for(float end=Time.unscaledTime+1.2f;Time.unscaledTime<end;){yield return null;lowest=Mathf.Min(lowest,Back(rig,hero));}
            yield return new WaitForSecondsRealtime(5);float settled=Back(rig,hero);
            TestContext.WriteLine($"COME_IN_HOLD before={before:0.0} lowest={lowest:0.0} settled={settled:0.0}");
            Assert.Greater(before-settled,10,"fixture: the smaller fight needs the view much nearer");
            Assert.GreaterOrEqual(lowest,before-1,$"the view comes {before-lowest:0.0} m in within 1.2 s of the fight needing less room: it pumps with every target the ship closes on");
            party.enabled=true;
        }

        // ------------------------------------------------------------------ QA-037: a lying sign grows into the bed
        /// <summary>At the flight view's farthest pull, about 3.3 times its rest. On e37c9b4 a lying sign grew with the camera without limit, and SALVAGE EDDY's outer letters went into
        /// the Gullet's curving bed.</summary>
        [UnityTest] public IEnumerator TheEddysSignNeverGrowsIntoTheBed()
        {
            yield return Zone("Gullet_Tunnel");ClearFights();
            var party=PartyController.Current;var hero=Lead;
            var sign=All<TextMeshPro>().First(t=>t.text=="SALVAGE EDDY");
            var made=sign.transform.lossyScale.y;
            Put(hero,new Vector3(sign.transform.position.x,1,sign.transform.position.z-4));hero.GetComponent<FlightMotor>().Halt();
            party.enabled=false;var rig=Object.FindFirstObjectByType<CameraRig>();
            // About 87 m back, the farthest the final chain's Gullet fights pulled the view (3.3 times its 26).
            rig.FrameEncounter(new Bounds(hero.transform.position+Vector3.forward*10,new Vector3(36,4,40)));
            yield return new WaitForSecondsRealtime(6);
            float pulled=Back(rig,hero)/rig.profile.distance,grown=sign.transform.lossyScale.y/made;
            TestContext.WriteLine($"EDDY_SIGN pulled={pulled:0.00} grown={grown:0.00}");
            Assert.Greater(pulled,2.8f,"fixture: the view is at the farthest a Gullet fight pulls it");
            Assert.GreaterOrEqual(grown,.99f,"the sign shrank as the view drew back");
            var size=sign.GetPreferredValues(sign.text);var problems=new List<string>();
            for(int i=0;i<=8;i++)for(int j=-1;j<=1;j++)
            {
                var point=sign.transform.TransformPoint(new Vector3((i/4f-1)*size.x*.5f,j*.25f*size.y,0));
                foreach(var hit in Physics.RaycastAll(point+Vector3.up*3,Vector3.down,6,~0,QueryTriggerInteraction.Ignore))
                    if(!(hit.collider is CharacterController)&&hit.collider.GetComponentInParent<Membrane>()==null&&hit.point.y>point.y)
                    {problems.Add($"{i},{j}: {hit.point.y-point.y:0.00} m under {hit.collider.name}");break;}
            }
            Assert.IsEmpty(problems,$"grown {grown:0.0} times, SALVAGE EDDY's letters go into the bed: {string.Join("; ",problems.Take(6))}");
            party.enabled=true;
        }

        // ------------------------------------------------------------------ E4-hushwell-03: legs through a word
        /// <summary>Standing just in front of a floor sign. On e37c9b4 a sign thinned only by how far its middle was from the hero, so one standing in front of its first letters, 2 m off
        /// its middle, left it at full strength with his legs through it: 'LO R CHAMBER'.</summary>
        [UnityTest] public IEnumerator AWordUnderAHerosLegsStepsAside()
        {
            yield return Zone("Hushwell");ClearFights();
            var sign=All<TextMeshPro>().First(t=>t.text=="LOWER CHAMBER");var fade=sign.GetComponent<SignFade>();
            Assert.IsNotNull(fade,"fixture: the sign has its fade");Assert.IsFalse(fade.bossOnly,"fixture: a floor sign people walk over");
            var camera=Camera.main;var toCamera=-camera.transform.forward;toCamera.y=0;toCamera.Normalize();
            var along=sign.transform.right;along.y=0;along.Normalize();var at=sign.transform.position;
            Put(Partner,at-along*9+(-toCamera)*7);
            // Behind the word, up the screen from it: in view, clear of it and over 1.6 m from its middle.
            Put(Lead,at-toCamera*2.6f);yield return new WaitForSecondsRealtime(1.5f);
            Assert.GreaterOrEqual(sign.alpha,.9f,"fixture: with nobody in front of it the sign reads in full (and isn't cut by a plate or the screen's edge)");
            // In front of it, toward the camera, and off to one side: his body stands over its letters on the screen, 1.8 m from its middle.
            Put(Lead,at+toCamera*1.4f+along*1.2f);yield return new WaitForSecondsRealtime(1.2f);
            var feet=camera.WorldToScreenPoint(Lead.transform.position);var head=camera.WorldToScreenPoint(Lead.transform.position+Vector3.up*1.8f);
            var size=sign.GetPreferredValues(sign.text);
            var one=camera.WorldToScreenPoint(sign.transform.TransformPoint(new Vector3(-size.x*.5f,-size.y*.5f,0)));var two=camera.WorldToScreenPoint(sign.transform.TransformPoint(new Vector3(size.x*.5f,size.y*.5f,0)));
            var words=Rect.MinMaxRect(Mathf.Min(one.x,two.x),Mathf.Min(one.y,two.y),Mathf.Max(one.x,two.x),Mathf.Max(one.y,two.y));
            Assert.IsTrue(words.Contains(new Vector2(feet.x,Mathf.Clamp(words.center.y,Mathf.Min(feet.y,head.y),Mathf.Max(feet.y,head.y)))),"fixture: his body stands across the word on the screen");
            TestContext.WriteLine($"COVERED_SIGN alpha={sign.alpha:0.00}");
            Assert.Greater(sign.alpha,.05f,"the sign went altogether: cut by a plate or the edge, which isn't what's being judged");
            Assert.LessOrEqual(sign.alpha,.4f,$"his legs go through the middle of a word at {sign.alpha:P0}: it reads 'LO R CHAMBER'");
        }

        // ------------------------------------------------------------------ E3-tallow-01: the curtain's moment
        /// <summary>From where the second relay is lit. On e37c9b4 each strand faded evenly over 2.6 s and the parting crossed the curtain in a second, so the stretch in view was mostly
        /// gone a second after it opened, with Sela's 'Curtain's coming down' still up for two more.</summary>
        [UnityTest] public IEnumerator TheCurtainIsStillComingDownWhileSelaSaysSo()
        {
            yield return Zone("WickReach");
            var strands=All<LineRenderer>().Where(l=>l.name=="Curtain strand").ToArray();Assert.Greater(strands.Length,20,"fixture: the curtain's strands");
            var full=strands.ToDictionary(s=>s,s=>s.startColor.a);
            Put(Lead,new Vector3(21,1,228));
            Flag("wick.relay1");yield return new WaitForSecondsRealtime(.6f);
            Flag("wick.relay2");
            float deadline=Time.unscaledTime+3;while(!Has("wick.gateOpen")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("wick.gateOpen"),"fixture: two relays open the gate");
            yield return new WaitForSecondsRealtime(2.2f);
            var camera=Camera.main;
            int strong=strands.Count(s=>s.gameObject.activeInHierarchy&&s.startColor.a>=.5f*full[s]&&Inside(camera.WorldToViewportPoint(s.transform.TransformPoint(s.GetPosition(0)))));
            TestContext.WriteLine($"CURTAIN_HELD strong={strong}");
            Assert.GreaterOrEqual(strong,3,"2.2 s after the curtain opens, with Sela still saying it's coming down, no strand on screen is at half its strength: the payoff is already gone");
            Assert.IsTrue(Inside(camera.WorldToViewportPoint(Lead.transform.position)),"showing the curtain lost the ship");
        }
    }
}
