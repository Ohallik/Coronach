using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D197, Nathan's November 4 report: "when you are sprinting and change directions it immediately slows you to
    /// walk speed even though you are still sprinting". Ordinary pad input through the real brain and motor.</summary>
    public sealed class SprintFeelTests
    {
        Gamepad pad;
        InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editor;
#endif
        [UnitySetUp] public IEnumerator Boot()
        {
            background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");pad=InputSystem.AddDevice<Gamepad>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameInput.Current!=null)GameInput.Current.Blocked=false;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=editor;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        IEnumerator State(GamepadState state){InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;}
        IEnumerator Zone(string zone,string spawn)
        {
            SceneFlow.Current.LoadZone(zone,spawn);
            float deadline=Time.unscaledTime+12;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var actor in PartyController.Current.members)actor.GetComponent<PartnerBrain>().enabled=false;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return State(new GamepadState());yield return new WaitForSecondsRealtime(.3f);
        }
        static Vector2 Stick(Vector3 world)
        {
            var local=Quaternion.Euler(0,-ZoneController.Current.definition.cameraProfile.yaw,0)*world;
            return new Vector2(local.x,local.z).normalized;
        }
        static void Put(CombatActor actor,Vector3 at,Vector3 facing)
        {
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;
            actor.transform.SetPositionAndRotation(at,Quaternion.LookRotation(facing));cc.enabled=true;Physics.SyncTransforms();
        }
        static float Planar(CombatActor actor){var v=actor.motor.Velocity;v.y=0;return v.magnitude;}
        // The D197 specification, mirrored so the same checks also run (and fail) against the code they replaced.
        const float CivilSprint=6.6f,SprintRecovery=30;

        [UnityTest] public IEnumerator CivilSprintKeepsItsPaceForAsLongAsItIsHeld()
        {
            yield return Zone("Hub_Decks","Office");
            var party=PartyController.Current;var hero=party.Active;
            foreach(var member in party.members)if(member!=hero)Put(member,new Vector3(-28,0,-11),Vector3.forward);
            Put(hero,new Vector3(-26,0,-3),Vector3.right);
            yield return State(new GamepadState{leftStick=Stick(Vector3.right),rightTrigger=1});
            float start=Time.unscaledTime,early=0,late=float.MaxValue;int earlySamples=0,lateSamples=0;
            while(Time.unscaledTime-start<7.5f&&hero.transform.position.x<25)
            {
                float t=Time.unscaledTime-start,speed=Planar(hero);
                if(t>=1.5f&&t<3){early+=speed;earlySamples++;}
                if(t>=5.5f){late=Mathf.Min(late,speed);lateSamples++;}
                yield return null;
            }
            yield return State(new GamepadState());
            Assert.Greater(earlySamples,30);Assert.Greater(lateSamples,30,"the sprint did not run long enough to cover the old Thrust budget");
            early/=earlySamples;
            TestContext.WriteLine($"CIVIL_SPRINT early={early:F3} lateSlowest={late:F3}");
            Assert.GreaterOrEqual(late,early*.95f,"a held civil sprint collapses once its Thrust runs out");
            Assert.That(early,Is.EqualTo(CivilSprint).Within(.35f),"civil sprint pace");
        }

        [UnityTest] public IEnumerator ACombatSprintTurningACornerKeepsItsPace()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(300,-.5f,300);floor.transform.localScale=new Vector3(120,1,120);
            var root=new GameObject("sprint turn",typeof(CharacterController),typeof(GroundMotor));root.transform.position=new Vector3(300,.08f,300);
            var motor=root.GetComponent<GroundMotor>();Physics.SyncTransforms();
            try
            {
                foreach(var (turn,brake) in new[]{(90f,false),(45f,false),(180f,true)})
                {
                    var cc=root.GetComponent<CharacterController>();cc.enabled=false;root.transform.SetPositionAndRotation(new Vector3(300,.08f,300),Quaternion.identity);cc.enabled=true;
                    motor.Halt();Physics.SyncTransforms();
                    float start=Time.unscaledTime;while(Time.unscaledTime-start<.8f){motor.Move(Vector2.up,true,false);yield return null;}
                    float top=motor.Velocity.magnitude;var request=Quaternion.Euler(0,turn,0)*Vector3.forward;
                    float slowest=float.MaxValue,settled=-1;start=Time.unscaledTime;
                    while(Time.unscaledTime-start<.6f)
                    {
                        motor.Move(new Vector2(request.x,request.z),true,false);yield return null;
                        var v=motor.Velocity;v.y=0;slowest=Mathf.Min(slowest,v.magnitude);
                        if(settled<0&&v.magnitude>.5f&&Vector3.Angle(v,request)<5)settled=Time.unscaledTime-start;
                    }
                    TestContext.WriteLine($"SPRINT_TURN turn={turn} top={top:F3} slowest={slowest:F3} settled={settled:F3}");
                    Assert.Greater(top,10,"fixture did not reach combat sprint pace");
                    Assert.GreaterOrEqual(settled,0,turn+"-degree turn never settles on the new heading");
                    Assert.Less(settled,.3f,turn+"-degree turn is sluggish");
                    if(brake)Assert.Less(slowest,top*.3f,"control: a reversal still brakes and restarts");
                    else Assert.GreaterOrEqual(slowest,top*.9f,turn+"-degree sprint turn sheds its pace");
                }
            }
            finally{Object.Destroy(root);Object.Destroy(floor);}
        }

        [UnityTest] public IEnumerator ASpentCombatSprintRestsInsteadOfFlickering()
        {
            yield return Zone("Arena_Ground","Arrival");
            var party=PartyController.Current;var hero=party.Active;
            foreach(var member in party.members)if(member!=hero)Put(member,new Vector3(0,.1f,-30),Vector3.forward);
            Put(hero,new Vector3(-17,.1f,-15),Vector3.right);
            float run=hero.GetComponent<GroundMotor>().speed;hero.thrust=2;
            yield return State(new GamepadState{leftStick=Stick(Vector3.right),rightTrigger=1});
            float start=Time.unscaledTime,fastest=0;int samples=0;
            while(Time.unscaledTime-start<1.4f){if(Time.unscaledTime-start>.4f){fastest=Mathf.Max(fastest,Planar(hero));samples++;}yield return null;}
            Assert.Greater(samples,30);
            Assert.Less(hero.thrust,SprintRecovery,"control: Thrust must still be refilling during the measured rest");
            TestContext.WriteLine($"SPENT_SPRINT run={run:F3} fastestWhileSpent={fastest:F3}");
            Assert.LessOrEqual(fastest,run*1.08f,"a spent sprint flickers back on every few frames");
            // Still held: the sprint comes back by itself once Thrust has refilled to the recovery mark (and starts
            // spending it again at once), long before the yard ends.
            float resumedAt=-1,thrustAtResume=0,peak=0;
            while(Time.unscaledTime-start<3.2f)
            {
                float speed=Planar(hero);float thrustNow=hero.thrust;yield return null;
                if(resumedAt<0&&Planar(hero)>run*1.15f){resumedAt=Time.unscaledTime-start;thrustAtResume=thrustNow;}
                if(resumedAt>=0)peak=Mathf.Max(peak,Planar(hero));
                if(resumedAt>=0&&Time.unscaledTime-start>resumedAt+.35f)break;
            }
            yield return State(new GamepadState());
            TestContext.WriteLine($"SPENT_SPRINT resumedAt={resumedAt:F3} thrustAtResume={thrustAtResume:F1} peak={peak:F3}");
            Assert.GreaterOrEqual(resumedAt,0,"the sprint never resumes once Thrust has refilled");
            Assert.GreaterOrEqual(thrustAtResume,SprintRecovery-1,"the sprint resumed before Thrust reached its recovery mark");
            Assert.Greater(peak,run*1.3f,"the resumed sprint never reaches sprint pace");
        }
    }
}
