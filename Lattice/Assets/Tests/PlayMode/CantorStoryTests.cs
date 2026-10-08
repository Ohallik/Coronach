using System.Collections;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace Lattice.Tests.PlayMode
{
    public sealed class CantorStoryTests
    {
        Gamepad pad;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        DiscoveryPoint point;
        EncounterVolume encounter;
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");pad=InputSystem.AddDevice<Gamepad>();yield return Load();
        }
        IEnumerator Load()
        {
            SceneFlow.Current.LoadZone("Gullet_Tunnel");float deadline=Time.unscaledTime+12;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);DialogueSystem.Current.AutoAdvance=false;
            point=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsSortMode.None).Single(d=>d.flag=="gullet.collarObserved");encounter=point.previewEncounter;
            foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.GetComponent<FlightMotor>().Halt();}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(DialogueSystem.Current!=null&&DialogueSystem.Current.Running){DialogueSystem.Current.Runner.Stop().Forget();yield return null;yield return null;}
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        IEnumerator Finish(float brake=0)
        {
            float deadline=Time.unscaledTime+30;
            while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)
            {
                InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=brake}.WithButton(GamepadButton.South));yield return null;yield return null;
                InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=brake});yield return null;yield return null;yield return null;
            }
            Assert.IsFalse(DialogueSystem.Current.Running,"preview conversation did not finish through normal confirm input");
        }
        [UnityTest] public IEnumerator TheOptionalConversationSavesItsObservationOnlyAfterAllSixLines()
        {
            var state=GameServices.Current.State;int scrip=state.scrip,xp=state.party.Sum(m=>m.xp),lines=DialogueSystem.Current.LinesPresented;
            point.Interact();yield return null;yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running);Assert.IsFalse(GameServices.Current.Flags.GetBool(point.flag));Assert.IsFalse(encounter.Started);
            yield return Finish();Assert.AreEqual(6,DialogueSystem.Current.LinesPresented-lines);Assert.IsTrue(GameServices.Current.Flags.GetBool(point.flag));
            Assert.IsFalse(encounter.Started);Assert.AreEqual("Starfight",MusicDirector.Current.Track);Assert.AreEqual(scrip,state.scrip);Assert.AreEqual(xp,state.party.Sum(m=>m.xp));
            Assert.IsFalse(GameServices.Current.Flags.GetBool("hushwell.nursery"),"legacy access must not invent the nursery discovery");
            var saved=GameServices.Current.Saves.Load("autosave");Assert.IsTrue(saved.flags[point.flag]);Assert.IsFalse(point.Available);
            string path=GameServices.Current.Saves.PathForSlot("autosave");var bytes=File.ReadAllBytes(path);point.Interact();yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
            GameServices.Current.State=saved;yield return Load();Assert.IsFalse(point.Available);Assert.IsFalse(encounter.Started);
        }
        [UnityTest] public IEnumerator HoldingBrakeThroughAllSixLinesReturnsNormalFlightAndFreshFire()
        {
            var hero=PartyController.Current.Active;hero.GetComponent<PlayerBrain>().AutoPilot=false;
            InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1});yield return null;yield return null;
            int lines=DialogueSystem.Current.LinesPresented;
            point.Interact();yield return null;yield return null;Assert.IsTrue(DialogueSystem.Current.Running);
            yield return Finish(1);
            InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1});yield return null;yield return null;
            Assert.AreEqual(6,DialogueSystem.Current.LinesPresented-lines);Assert.IsTrue(GameServices.Current.Flags.GetBool(point.flag));
            Assert.IsFalse(GameInput.Current.Blocked);Assert.IsTrue(GameInput.Current.Held("Brake"),"real conversation trapped the held brake");
            Assert.IsFalse(GameInput.Current.Held("Fire"));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1}.WithButton(GamepadButton.South));yield return null;yield return null;
            Assert.IsTrue(GameInput.Current.Held("Fire"),"fresh fire is swallowed after the completed conversation");
        }
        [UnityTest] public IEnumerator CancelledPreviewLeavesTheSaveUntouchedAndCanBeCompletedLater()
        {
            GameServices.Current.Saves.Save("autosave",GameServices.Current.State);string path=GameServices.Current.Saves.PathForSlot("autosave");var bytes=File.ReadAllBytes(path);
            point.Interact();yield return null;yield return null;Assert.IsTrue(DialogueSystem.Current.Running);
            bool stopped=false;async YarnTask Stop(){await DialogueSystem.Current.Runner.Stop();stopped=true;}Stop().Forget();
            float deadline=Time.unscaledTime+5;while(!stopped&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(stopped);Assert.IsFalse(GameServices.Current.Flags.GetBool(point.flag));Assert.IsTrue(point.Available);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
            point.Interact();yield return null;yield return null;yield return Finish();Assert.IsTrue(GameServices.Current.Flags.GetBool(point.flag));
        }
        [UnityTest] public IEnumerator StartingCombatDuringPendingPreviewDoesNotSaveAnObservation()
        {
            GameServices.Current.Saves.Save("autosave",GameServices.Current.State);string path=GameServices.Current.Saves.PathForSlot("autosave");var bytes=File.ReadAllBytes(path);
            point.Interact();yield return null;yield return null;encounter.Begin();yield return Finish();
            Assert.IsFalse(GameServices.Current.Flags.GetBool(point.flag));Assert.IsFalse(point.Available);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
        }
        [UnityTest] public IEnumerator CompletedEncountersDoNotOfferAnAbsentAnimalConversation()
        {
            GameServices.Current.Flags.SetBool("bossdown.Cantor",true);yield return null;yield return null;
            Assert.IsFalse(point.Available);point.Interact();yield return null;Assert.IsFalse(DialogueSystem.Current.Running);
            Assert.IsFalse(GameServices.Current.Flags.GetBool(point.flag));
        }
        [UnityTest] public IEnumerator TheObjectiveSuggestsThePocketThenHandsBackToTheCollar()
        {
            for(int i=0;i<3;i++)GameServices.Current.Flags.SetBool("clear.Gullet_Chamber_"+i,true);
            // ObjectiveHud polls every 0.25 unscaled seconds. Observe each
            // transition after that bounded refresh, not after two frames.
            yield return new WaitForSecondsRealtime(.35f);
            var text=Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t=>t.name=="Objective");
            StringAssert.Contains("Study the strained collar",text.text);
            StringAssert.Contains($"{Vector3.Distance(point.transform.position,PartyController.Current.Active.transform.position):0} m",text.text,"objective points past the optional pocket");
            point.Interact();yield return null;yield return null;yield return Finish();yield return new WaitForSecondsRealtime(.35f);
            StringAssert.Contains("Approach the collar links",text.text);
            encounter.Begin();yield return new WaitForSecondsRealtime(.35f);
            StringAssert.Contains("Cut the collar links (0/3)",text.text);
        }
        IEnumerator ReleaseBark(string character,string expected)
        {
            if(PartyController.Current.Active.character!=character)Assert.IsTrue(PartyController.Current.Swap());
            encounter.Begin();yield return null;yield return null;
            var enemy=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="Cantor");
            var packet=new DamagePacket{source=PartyController.Current.Active.Health,amount=100000,type=DamageType.Pulse};
            foreach(var link in enemy.GetComponent<CantorCollar>().Links)link.Receive(packet);
            var lines=new System.Collections.Generic.List<string>();System.Action<string,string> spoken=(who,line)=>{if(who==character)lines.Add(line);};
            BarkService.Spoken+=spoken;
            try{enemy.Health.Receive(packet);enemy.Health.Receive(packet);yield return null;}
            finally{BarkService.Spoken-=spoken;}
            CollectionAssert.AreEqual(new[]{expected},lines,"resolution must give one contextual release line");Assert.IsTrue(encounter.Cleared);
        }
        [UnityTest] public IEnumerator TarenNamesTheOpenedLockAtActualResolution()=>ReleaseBark("Taren","Lock's open. Give it room.");
        [UnityTest] public IEnumerator SelaNamesTheOpeningRouteAtActualResolution()=>ReleaseBark("Sela","It's turning. Follow the opening.");
    }
}
