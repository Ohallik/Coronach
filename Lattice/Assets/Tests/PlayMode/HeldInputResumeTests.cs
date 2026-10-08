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
    public sealed class HeldInputResumeTests
    {
        Gamepad pad;
        CombatActor hero;
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
            DevLoadout.Apply("starter");pad=InputSystem.AddDevice<Gamepad>();SceneFlow.Current.LoadZone("Arena_Flight");
            float deadline=Time.unscaledTime+12;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);hero=PartyController.Current.Active;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.GetComponent<FlightMotor>().Halt();}
            foreach(var actor in PartyController.Current.members)if(actor!=hero)actor.transform.position=new Vector3(-8,1,-4);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            // SceneFlow also blocks input during loading. Drain that neutral
            // release gate before establishing the held control under test.
            yield return State(new GamepadState());GameInput.Current.Held("Fire");yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameInput.Current.Blocked=false;if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior=background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=editor;
#endif
            PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        IEnumerator State(GamepadState state)
        {InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;}
        IEnumerator Continuous(string action,bool flight,float left,float right)
        {
            var input=GameInput.Current;input.SetFlight(flight);yield return null;
            var held=new GamepadState{leftTrigger=left,rightTrigger=right};yield return State(held);
            Assert.IsTrue(input.Held(action),"fixture did not establish the held control");
            input.Blocked=true;yield return null;Assert.IsFalse(input.Held(action),"UI must still block gameplay");
            input.Blocked=false;yield return null;yield return null;
            Assert.IsTrue(input.Held(action),action+" was trapped behind the UI release guard");
        }
        [UnityTest] public IEnumerator BrakeSurvivesUiExit()=>Continuous("Brake",true,1,0);
        [UnityTest] public IEnumerator BoostSurvivesUiExit()=>Continuous("Boost",true,0,1);
        [UnityTest] public IEnumerator SprintSurvivesUiExit()=>Continuous("Sprint",false,0,1);
        [UnityTest] public IEnumerator ClosingConfirmCannotBecomeFireWhileBrakeIsHeld()
        {
            var input=GameInput.Current;yield return State(new GamepadState{leftTrigger=1}.WithButton(GamepadButton.South));
            input.Blocked=true;yield return null;input.Blocked=false;
            for(int i=0;i<10;i++)
            {yield return null;Assert.IsFalse(input.Held("Fire"),"closing confirm leaked into fire");Assert.IsFalse(input.Pressed("Interact"));}
        }
        [UnityTest] public IEnumerator ReleasingOnlyConfirmAllowsANewCombatPressWithBrakeStillHeld()
        {
            var input=GameInput.Current;yield return State(new GamepadState{leftTrigger=1}.WithButton(GamepadButton.South));
            input.Blocked=true;yield return null;input.Blocked=false;yield return null;
            Assert.IsFalse(input.Held("Fire"));yield return State(new GamepadState{leftTrigger=1});
            Assert.IsFalse(input.Held("Fire"));yield return null;yield return null;
            yield return State(new GamepadState{leftTrigger=1}.WithButton(GamepadButton.South));
            Assert.IsTrue(input.Held("Fire"),"the new press is still swallowed by the held brake");
        }
        [UnityTest] public IEnumerator TheRealCraftBrakesAfterUiExitWithoutLeakingTheClosingShot()
        {
            var motor=hero.GetComponent<FlightMotor>();float end=Time.unscaledTime+.9f;
            while(Time.unscaledTime<end){motor.Move(Vector2.right,false,false);yield return null;}
            Assert.Greater(motor.Velocity.magnitude,8,"fixture did not establish flight momentum");
            GameInput.Current.Blocked=true;yield return State(new GamepadState{leftTrigger=1}.WithButton(GamepadButton.South));
            Vector3 start=hero.transform.position;float stoppingLimit=motor.Velocity.magnitude/9+.4f;int shots=Projectile.ActiveCount;
            GameInput.Current.Blocked=false;hero.GetComponent<PlayerBrain>().AutoPilot=false;
            yield return new WaitForSecondsRealtime(.8f);
            Assert.Less(motor.Velocity.magnitude,.1f,"held brake failed to stop the actual motor");
            Assert.Less(Vector3.Distance(start,hero.transform.position),stoppingLimit,"UI exit discarded normal braking");
            Assert.AreEqual(shots,Projectile.ActiveCount,"closing confirm fired a projectile");
        }
    }
}
