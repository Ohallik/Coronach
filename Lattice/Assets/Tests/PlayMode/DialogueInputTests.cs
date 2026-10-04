using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace Lattice.Tests.PlayMode
{
    public sealed class DialogueInputTests
    {
        Gamepad pad;
        Keyboard keyboard;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditorInput;
#endif
        DialogueSystem dialogue;
        DialoguePanel panel;

        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual keyboard events into play.
            priorEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();PromptService.ForceDevice(null);
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");pad=InputSystem.AddDevice<Gamepad>();keyboard=InputSystem.AddDevice<Keyboard>();
            SceneFlow.Current.LoadZone("Hub_Decks");
            float deadline=Time.realtimeSinceStartup+10;
            while(SceneFlow.Current.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            dialogue=DialogueSystem.Current;dialogue.AutoAdvance=false;
            panel=Object.FindFirstObjectByType<DialoguePanel>();Assert.IsNotNull(panel);
            yield return Release();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(dialogue!=null&&dialogue.Running){dialogue.Runner.Stop().Forget();yield return null;yield return null;}
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            PromptService.ForceDevice(null);InputSystem.settings.backgroundBehavior=priorBackground;GameTime.Reset();
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditorInput;
#endif
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        IEnumerator Release()
        {
            InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return null;yield return null;yield return null;
        }
        IEnumerator Press(GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;
            yield return Release();
        }
        TMP_Text Text(string name)=>panel.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name==name);
        int ChoiceCount=>panel.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Option_"));

        [UnityTest] public IEnumerator EveryCompiledStoryNodeAndChoiceAcceptsRapidInputAndRoundTripsItsConsequences()
        {
            var branches=new Dictionary<string,string[]>
            {
                ["MiraFirst"]=new[]{"shop","world"},["HalFirst"]=new[]{"bench","repair","world"},
                ["HalRepeat"]=new[]{"repair","bench","world"},["HalPost"]=new[]{"repair","bench"},
                ["ArenaGuide"]=new[]{"bench","world"}
            };
            var nodes=Resources.Load<YarnProject>("Dialogue/Lattice").NodeNames.Where(n=>!n.StartsWith("__")).OrderBy(n=>n).ToArray();
            Assert.GreaterOrEqual(nodes.Length,25);
            var saves=new SaveSystem(System.IO.Path.Combine(Application.temporaryCachePath,"C6-all-branches-"+System.Guid.NewGuid().ToString("N")));
            foreach(string node in nodes)
            {
                string speaker=node=="ArenaGuide"?"Hal":node=="CacheFound"?"Neve":node.StartsWith("Sela")?"Sela":
                    new[]{"Orrin","Mira","Hal","Neve","Survivor","Keeper","Oda","Ilo"}.Single(n=>node.StartsWith(n));
                var outcomes=branches.TryGetValue(node,out var choices)?choices:new[]{node.StartsWith("Mira")?"shop":"world"};
                for(int choice=0;choice<outcomes.Length;choice++)
                {
                    // A real serialized reload before and after every implemented branch.
                    saves.Save("slot3",GameServices.Current.State);GameServices.Current.State=saves.Load("slot3");
                    int money=GameServices.Current.State.scrip;
                    dialogue.StartNode(node,speaker);yield return null;yield return null;
                    Assert.IsFalse(dialogue.AutoAdvance);Assert.IsTrue(dialogue.Running,node);
                    if(choices!=null)
                    {
                        yield return AdvanceToChoice();Assert.AreEqual(outcomes.Length,ChoiceCount,node);
                        for(int down=0;down<choice;down++)yield return Press(GamepadButton.DpadDown);
                        yield return Press(GamepadButton.South);
                    }
                    yield return FinishLines();
                    var shop=Object.FindFirstObjectByType<ShopUi>();var bench=Object.FindFirstObjectByType<PauseMenu>();
                    Assert.AreEqual(outcomes[choice]=="shop",shop.IsOpen,node+" choice "+choice);
                    Assert.AreEqual(outcomes[choice]=="bench",bench.IsOpen,node+" choice "+choice);
                    if(outcomes[choice]=="repair")Assert.AreEqual(money-8*GameServices.Current.State.party[0].level,GameServices.Current.State.scrip,node);
                    if(shop.IsOpen||bench.IsOpen)yield return Press(GamepadButton.East);
                    var before=GameServices.Current.State;saves.Save("slot3",before);
                    GameServices.Current.State=saves.Load("slot3");var after=GameServices.Current.State;
                    Assert.IsNotNull(after);Assert.AreEqual(before.scrip,after.scrip);
                    CollectionAssert.AreEquivalent(before.flags,after.flags);CollectionAssert.AreEquivalent(before.materials,after.materials);
                    CollectionAssert.AreEquivalent(before.questSteps,after.questSteps);CollectionAssert.AreEquivalent(before.questCounts,after.questCounts);
                    CollectionAssert.AreEqual(before.party.Select(m=>m.id),after.party.Select(m=>m.id));
                    Assert.IsFalse(GameInput.Current.Blocked);Assert.IsFalse(GameTime.Paused);
                    Debug.Log("C6_BRANCH_OK node="+node+" choice="+(choices==null?-1:choice)+" outcome="+outcomes[choice]);
                }
            }
        }

        [UnityTest] public IEnumerator MirasQuestRewardPersistsAndCannotBeCollectedTwice()
        {
            var state=GameServices.Current.State;state.materials.Remove("RidgeCrystal");state.collectedMaterials.Remove("RidgeCrystal");
            var mira=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Mira");
            // Mining before accepting the job is valid. Starting the conversation
            // must recognize those already collected crystals exactly once.
            Assert.IsFalse(GameServices.Current.Flags.GetBool("quest.MiraCrystals.complete"));
            RpgServices.Inventory.Give("RidgeCrystal",6);
            var saves=new SaveSystem(System.IO.Path.Combine(Application.temporaryCachePath,"C6-reward-"+System.Guid.NewGuid().ToString("N")));
            saves.Save("slot1",state);GameServices.Current.State=saves.Load("slot1");
            int money=GameServices.Current.State.scrip;
            mira.Interact();yield return null;yield return null;yield return AdvanceToChoice();
            yield return Press(GamepadButton.DpadDown);yield return Press(GamepadButton.South);yield return FinishLines();
            Assert.IsTrue(GameServices.Current.Flags.GetBool("quest.MiraCrystals.complete"));
            Assert.AreEqual(money+GameCatalog.Find<QuestDef>("MiraCrystals").rewardScrip,GameServices.Current.State.scrip);
            yield return Press(GamepadButton.East);
            saves.Save("slot1",GameServices.Current.State);GameServices.Current.State=saves.Load("slot1");
            money=GameServices.Current.State.scrip;var xp=GameServices.Current.State.party.Select(m=>m.xp).ToArray();
            mira.Interact();yield return null;yield return null;yield return FinishLines();yield return Press(GamepadButton.East);
            Assert.AreEqual(money,GameServices.Current.State.scrip,"repeated reward after reload");
            CollectionAssert.AreEqual(xp,GameServices.Current.State.party.Select(m=>m.xp));
        }

        // Rapid physical edges through the Input System, never presenter callbacks.
        IEnumerator AdvanceToChoice()
        {
            float deadline=Time.realtimeSinceStartup+15;
            while(dialogue.Running&&ChoiceCount==0&&Time.realtimeSinceStartup<deadline)
            {
                int lines=dialogue.LinesPresented;
                yield return Press(GamepadButton.South);
                Assert.LessOrEqual(dialogue.LinesPresented-lines,1,"one confirm skipped multiple lines");
            }
            Assert.IsTrue(dialogue.Running);Assert.Greater(ChoiceCount,0,"choice never arrived");
        }

        IEnumerator FinishLines()
        {
            float deadline=Time.realtimeSinceStartup+15;
            while(dialogue.Running&&Time.realtimeSinceStartup<deadline)
            {
                Assert.AreEqual(0,ChoiceCount,"unexpected unselected branch");
                int lines=dialogue.LinesPresented;
                yield return Press(GamepadButton.South);
                Assert.LessOrEqual(dialogue.LinesPresented-lines,1,"one confirm skipped multiple lines");
            }
            Assert.IsFalse(dialogue.Running);yield return Release();
        }

        [UnityTest] public IEnumerator ShopAndBenchHandoffsDoNotReuseHeldConfirmOrClosingCancel()
        {
            foreach(string node in new[]{"MiraFirst","HalFirst"})
            {
                bool shop=node.StartsWith("Mira");
                dialogue.StartNode(node,shop?"Mira":"Hal");yield return null;yield return null;
                yield return AdvanceToChoice();
                int scrip=GameServices.Current.State.scrip,parts=GameServices.Current.State.parts.Count;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
                yield return new WaitForSecondsRealtime(.8f);
                Assert.IsFalse(dialogue.Running);Assert.IsFalse(panel.IsVisible);Assert.AreEqual(0,ChoiceCount);
                var shopUi=Object.FindFirstObjectByType<ShopUi>();var bench=Object.FindFirstObjectByType<PauseMenu>();
                Assert.AreEqual(shop,shopUi.IsOpen);Assert.AreEqual(!shop,bench.IsOpen);
                Assert.IsTrue(GameInput.Current.Blocked);Assert.IsTrue(PadFocus.IsUsable(EventSystem.current.currentSelectedGameObject));
                string heading=(shop?shopUi.GetComponentsInChildren<TMP_Text>():bench.GetComponentsInChildren<TMP_Text>()).Single(t=>t.name=="Heading").text;
                Assert.IsTrue(shop?heading.Contains("BUY"):heading=="FABRICATE","handoff confirm activated the next UI");
                Assert.AreEqual(scrip,GameServices.Current.State.scrip);Assert.AreEqual(parts,GameServices.Current.State.parts.Count);
                yield return Release();
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));
                yield return null;yield return null;
                Assert.IsFalse(shopUi.IsOpen);Assert.IsFalse(bench.IsOpen);Assert.IsFalse(GameInput.Current.Blocked);
                Assert.IsFalse(GameInput.Current.Held("Dodge"),"closing cancel leaked into world dodge");
                Assert.IsFalse(GameInput.Current.Pressed("Dodge"));Assert.IsFalse(GameTime.Paused);
                yield return new WaitForSecondsRealtime(.25f);
                Assert.IsFalse(GameInput.Current.Held("Dodge"),"held closing input resumed gameplay before release");
                yield return Release();
            }
        }

        [UnityTest] public IEnumerator ChoiceCancelPauseAndLostSelectionPreserveThePendingBranch()
        {
            dialogue.StartNode("HalFirst","Hal");yield return null;yield return null;yield return AdvanceToChoice();
            Assert.AreEqual(3,ChoiceCount);
            var first=EventSystem.current.currentSelectedGameObject;
            yield return Press(GamepadButton.East);yield return Press(GamepadButton.Start);
            Assert.IsTrue(dialogue.Running);Assert.AreEqual(3,ChoiceCount);
            Assert.IsFalse(Object.FindFirstObjectByType<PauseMenu>().IsOpen);
            Assert.AreEqual(first,EventSystem.current.currentSelectedGameObject);
            EventSystem.current.SetSelectedGameObject(null);
            yield return Press(GamepadButton.DpadDown);
            Assert.IsTrue(PadFocus.IsUsable(EventSystem.current.currentSelectedGameObject));
            Assert.IsTrue(EventSystem.current.currentSelectedGameObject.name.StartsWith("Option_"));
            Assert.IsTrue(dialogue.Running);Assert.AreEqual(3,ChoiceCount);
        }

        [UnityTest] public IEnumerator StoppingRealYarnAtAChoiceFinishesWithoutAConsequenceOrRunnerException()
        {
            int requests=0;void Requested(string action)=>requests++;
            DialogueSystem.UiRequested+=Requested;
            try
            {
                dialogue.StartNode("MiraFirst","Mira");yield return null;yield return null;yield return AdvanceToChoice();
                bool stopped=false;
                async YarnTask Stop(){await dialogue.Runner.Stop();stopped=true;}
                Stop().Forget();
                float deadline=Time.realtimeSinceStartup+2;
                while(!stopped&&Time.realtimeSinceStartup<deadline)yield return null;
                yield return null;yield return null;
                Assert.IsTrue(stopped);Assert.IsFalse(dialogue.Running);Assert.AreEqual(0,ChoiceCount);
                Assert.AreEqual(0,requests);Assert.IsFalse(Object.FindFirstObjectByType<ShopUi>().IsOpen);
                Assert.IsFalse(panel.IsVisible);Assert.IsFalse(GameInput.Current.Blocked);
                RpgServices.Quests.Start("Sorrel");
                RpgServices.Quests.Report(ObjectiveKind.Reach,"Sorrel_Ridges");
                int step=GameServices.Current.State.questSteps["Sorrel"];
                Assert.AreEqual(1,step,"fixture must be waiting for the Survivor conversation");
                dialogue.StartNode("SurvivorFirst","Survivor");yield return null;yield return null;
                stopped=false;Stop().Forget();
                deadline=Time.realtimeSinceStartup+2;
                while(!stopped&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsTrue(stopped);
                Assert.AreEqual(step,GameServices.Current.State.questSteps["Sorrel"],"aborted conversation granted quest credit");
            }
            finally{DialogueSystem.UiRequested-=Requested;}
        }

        [UnityTest] public IEnumerator OrrinJoinAndHalsLessonSurviveReloadAndRepeatExactlyOnce()
        {
            GameServices.Current.State=new GameState();
            SceneFlow.Current.LoadZone("Hub_Decks");
            float deadline=Time.realtimeSinceStartup+10;
            while(SceneFlow.Current.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return Release();
            dialogue=DialogueSystem.Current;dialogue.AutoAdvance=false;
            panel=Object.FindFirstObjectByType<DialoguePanel>();
            var saves=new SaveSystem(System.IO.Path.Combine(Application.temporaryCachePath,"C6-branches-"+System.Guid.NewGuid().ToString("N")));
            saves.Save("slot1",GameServices.Current.State);
            GameServices.Current.State=saves.Load("slot1");
            Assert.AreEqual(1,GameServices.Current.State.party.Count);
            Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Orrin").Interact();
            yield return null;yield return null;yield return FinishLines();
            Assert.AreEqual(2,GameServices.Current.State.party.Count);Assert.AreEqual(2,PartyController.Current.members.Length);
            Assert.AreEqual(1,GameServices.Current.State.party.Count(m=>m.id=="Sela"));
            Assert.IsTrue(GameServices.Current.State.questSteps.ContainsKey("Sorrel"));
            saves.Save("slot1",GameServices.Current.State);GameServices.Current.State=saves.Load("slot1");
            int lines=dialogue.LinesPresented;
            Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Orrin").Interact();
            yield return null;yield return null;yield return FinishLines();
            Assert.AreEqual(1,dialogue.LinesPresented-lines,"reload forgot the first visit");
            Assert.AreEqual(2,GameServices.Current.State.party.Count);Assert.AreEqual(2,PartyController.Current.members.Length);
            saves.Save("slot2",GameServices.Current.State);GameServices.Current.State=saves.Load("slot2");
            var hal=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Hal");
            hal.Interact();yield return null;yield return null;yield return AdvanceToChoice();
            yield return Press(GamepadButton.DpadDown);yield return Press(GamepadButton.DpadDown);yield return Press(GamepadButton.South);yield return FinishLines();
            Assert.AreEqual(12,GameServices.Current.State.materials["ScrapAlloy"]);
            Assert.AreEqual(3,GameServices.Current.State.materials["LatticeFilament"]);Assert.IsTrue(GameServices.Current.Flags.GetBool("hal.lesson"));
            saves.Save("slot2",GameServices.Current.State);GameServices.Current.State=saves.Load("slot2");
            hal.Interact();yield return null;yield return null;yield return AdvanceToChoice();
            yield return Press(GamepadButton.DpadDown);yield return Press(GamepadButton.DpadDown);yield return Press(GamepadButton.South);yield return FinishLines();
            Assert.AreEqual(12,GameServices.Current.State.materials["ScrapAlloy"],"repeat duplicated the lesson's reward");
            Assert.AreEqual(3,GameServices.Current.State.materials["LatticeFilament"]);
        }

        [UnityTest] public IEnumerator HeldConfirmRevealsOnceAndRequiresANewPressToAdvance()
        {
            dialogue.StartNode("MiraFirst","Mira");yield return new WaitForSecondsRealtime(.15f);
            string line=Text("Body").text;Assert.Less(Text("Body").maxVisibleCharacters,line.Length);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(line,Text("Body").text,"reveal also advanced the line");
            Assert.GreaterOrEqual(Text("Body").maxVisibleCharacters,line.Length);
            yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(line,Text("Body").text,"held confirm repeated into the next line");
            yield return Release();yield return Press(GamepadButton.South);
            Assert.AreNotEqual(line,Text("Body").text);Assert.AreEqual(2,dialogue.LinesPresented);
        }

        [UnityTest] public IEnumerator DeviceSwitchAndBackPreserveTheCurrentConversation()
        {
            dialogue.StartNode("MiraFirst","Mira");yield return new WaitForSecondsRealtime(.15f);
            string line=Text("Body").text;
            yield return Press(GamepadButton.East);
            Assert.IsTrue(dialogue.Running,"Back must not silently leave a story conversation");
            Assert.AreEqual(line,Text("Body").text);Assert.AreEqual(PromptDevice.Gamepad,PromptService.Device);
            string padHint=Text("Continue").text;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));yield return null;yield return null;
            Assert.IsTrue(keyboard.leftShiftKey.isPressed,"virtual keyboard events never reached the game input update");
            Assert.AreEqual(PromptDevice.Keyboard,PromptService.Device);Assert.AreNotEqual(padHint,Text("Continue").text);
            Assert.AreEqual(line,Text("Body").text,"switching devices consumed a line");
            yield return Release();yield return Press(GamepadButton.East);
            Assert.AreEqual(PromptDevice.Gamepad,PromptService.Device);Assert.AreEqual(padHint,Text("Continue").text);
        }

        static DialogueOption Option(int id,bool available)=>new DialogueOption
        {
            DialogueOptionID=id,IsAvailable=available,
            Line=new LocalizedLine{Text=new Yarn.Markup.MarkupParseResult("Choice "+id,new List<Yarn.Markup.MarkupAttribute>())}
        };

        [UnityTest] public IEnumerator CancellingAnOptionWaitNeverSelectsTheFirstConsequence()
        {
            var presenter=dialogue.GetComponent<Lattice.Dialogue.OptionsPresenter>();
            using var cancel=new CancellationTokenSource();
            DialogueOption selected=null;bool done=false;
            async YarnTask Run(){selected=await presenter.RunOptionsAsync(new[]{Option(17,true),Option(42,true)},new LineCancellationToken{NextContentToken=cancel.Token});done=true;}
            Run().Forget();yield return null;yield return null;
            Assert.IsNotNull(EventSystem.current.currentSelectedGameObject,"choice UI did not open");
            cancel.Cancel();
            float deadline=Time.realtimeSinceStartup+2;while(!done&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsTrue(done);Assert.IsNull(selected,"cancellation silently picked a consequential option");
            yield return null;
            Assert.IsFalse(panel.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Option_")));
        }

        [UnityTest] public IEnumerator UnavailableChoicesAreSkippedAndSelectionKeepsTheOriginalOptionIdentity()
        {
            var presenter=dialogue.GetComponent<Lattice.Dialogue.OptionsPresenter>();
            DialogueOption selected=null;bool done=false;
            using var cancel=new CancellationTokenSource();
            async YarnTask Run(){selected=await presenter.RunOptionsAsync(new[]{Option(5,false),Option(17,true),Option(42,true)},new LineCancellationToken{NextContentToken=cancel.Token});done=true;}
            Run().Forget();yield return null;yield return null;
            try
            {
                Assert.AreEqual(2,panel.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Option_")));
                yield return Press(GamepadButton.DpadDown);yield return Press(GamepadButton.South);
                Assert.IsTrue(done);Assert.IsNotNull(selected);Assert.AreEqual(42,selected.DialogueOptionID);
            }
            finally{cancel.Cancel();}
        }

        [UnityTest] public IEnumerator AllUnavailableOptionsReturnNoChoiceAndLeaveNoFocusTarget()
        {
            var presenter=dialogue.GetComponent<Lattice.Dialogue.OptionsPresenter>();
            bool done=false;DialogueOption selected=Option(99,true);
            using var cancel=new CancellationTokenSource();
            async YarnTask Run(){selected=await presenter.RunOptionsAsync(new[]{Option(5,false),Option(17,false)},new LineCancellationToken{NextContentToken=cancel.Token});done=true;}
            Run().Forget();yield return null;
            try
            {
                Assert.IsTrue(done);Assert.IsNull(selected);
                Assert.IsFalse(panel.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Option_")));
            }
            finally{cancel.Cancel();}
        }
    }
}
