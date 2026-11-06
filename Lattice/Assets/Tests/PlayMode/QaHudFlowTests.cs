using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
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
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass on the HUD and the moments around it: the layout on any screen, what the strip offers, what a
    /// press answers, a conversation's edges, and the Flash lessons. Every check here fails on the build it was written against.
    /// (QaHudTests.cs is the earlier batch's file; it stays as it is.)</summary>
    public sealed class QaHudFlowTests
    {
        Gamepad pad;Keyboard keyboard;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual devices into play.
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();PromptService.ForceDevice(null);SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");pad=InputSystem.AddDevice<Gamepad>();keyboard=InputSystem.AddDevice<Keyboard>();
            yield return Load("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            PromptService.ForceDevice(null);InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+20;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            yield return new WaitForSecondsRealtime(.3f);
        }
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static Rect Box(RectTransform rect,float inset=0)
        {var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x+inset,c[0].y+inset,c[2].x-inset,c[2].y-inset);}
        static void Put(CombatActor hero,Vector3 at)
        {var body=hero.GetComponent<CharacterController>();body.enabled=false;hero.transform.position=at;body.enabled=true;Physics.SyncTransforms();}
        static bool Has(string flag)=>GameServices.Current.Flags.GetBool(flag);
        IEnumerator Pad(GamepadState state){InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;}
        IEnumerator Tap(GamepadButton button){yield return Pad(new GamepadState().WithButton(button));yield return Pad(new GamepadState());yield return null;}
        sealed class FixturePrompt:InteractionPrompt{public int Uses;public override void Interact(){Uses++;}}

        // ------------------------------------------------------------------ ui227: the layout on any screen
        static readonly (int w,int h,string label)[] Screens={(1920,1080,"16:9"),(1920,1200,"16:10"),(2560,1080,"21:9"),(1280,1024,"5:4")};
        /// <summary>Where the 1080p layout puts each thing (centre and size). A zero size is a box that grows with its words.</summary>
        static readonly (string surface,string name,float x,float y,float w,float h)[] Layout=
        {
            ("HUD","VitalsFrame",277,918,524,298),("HUD","Status",297,1010,470,38),("HUD","IntegrityLabel",297,976,470,30),("HUD","ChargeLabel",297,933,470,30),("HUD","ThrustLabel",297,890,470,30),
            ("HUD","IntegrityBack",262,955,400,14),("HUD","ChargeBack",262,912,400,14),("HUD","ThrustBack",262,869,400,14),("HUD","Partner",279,833,435,32),
            ("HUD","Target",960,972,700,80),("HUD","Zone",1635,1034,500,60),("HUD","Skills",625,50,1190,60),("HUD","Interaction",960,205,1200,60),
            ("HUD","Skill0",1340,112,74,74),("HUD","Skill1",1490,112,74,74),("HUD","Skill2",1640,112,74,74),("HUD","Skill3",1790,112,74,74),
            ("HUD","SkillLabel0",1340,42,145,62),("HUD","SkillLabel1",1490,42,145,62),("HUD","SkillLabel2",1640,42,145,62),("HUD","SkillLabel3",1790,42,145,62),
            ("HUD","BossName",960,216,900,36),("HUD","BossEpithet",960,216,900,36),("HUD","BossIntegrityBack",960,186,900,20),("HUD","BossBreakBack",960,168,900,7),("HUD","BossState",960,140,900,36),
            ("HUD","BossTitleCard",960,640,1400,90),("HUD","BossTitleEpithet",960,580,1400,44),
            ("Objective","ObjectiveFrame",1620,905,570,165),("Barks","Bark",960,285,1250,65),("QuestToast","QuestToastFrame",0,0,0,0),
            ("Dialogue","DialoguePanel",960,205,1760,330),("Dialogue","Portrait",250,265,300,390),
        };
        static Canvas Surface(string name)
        {var found=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(c=>c.name==name);Assert.IsNotNull(found,"the "+name+" surface was never built");return found;}
        static RectTransform Spot(string surface,string name)
        {var found=Surface(surface).GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r=>r.name==name);Assert.IsNotNull(found,surface+" has no "+name);return found;}
        /// <summary>The canvas a screen gets, exactly as the surface's scaler would size it.</summary>
        static Vector2 CanvasAt(CanvasScaler scaler,int w,int h)
        {
            var r=scaler.referenceResolution;
            float scale=scaler.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand?Mathf.Min(w/r.x,h/r.y):scaler.screenMatchMode==CanvasScaler.ScreenMatchMode.Shrink?Mathf.Max(w/r.x,h/r.y):
                Mathf.Pow(2,Mathf.Lerp(Mathf.Log(w/r.x,2),Mathf.Log(h/r.y,2),scaler.matchWidthOrHeight));
            return new Vector2(w/scale,h/scale);
        }
        /// <summary>Where a rect lands on a canvas of this size, computed from its anchors and offsets up to the canvas, so a screen the
        /// test machine doesn't have can still be checked (and a surface that is hidden right now).</summary>
        static Rect Placed(RectTransform rect,RectTransform root,Vector2 size)
        {
            if(rect==root)return new Rect(Vector2.zero,size);
            var outer=Placed((RectTransform)rect.parent,root,size);
            var min=outer.position+Vector2.Scale(outer.size,rect.anchorMin)+rect.offsetMin;var max=outer.position+Vector2.Scale(outer.size,rect.anchorMax)+rect.offsetMax;
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        [UnityTest] public IEnumerator TheHudHoldsItsCornersOnEveryScreen()
        {
            yield return null;
            var failures=new List<string>();
            void Anchored(string surface,string name,float x,float y)
            {
                var rect=Spot(surface,name);var anchor=new Vector2(x,y);
                if(rect.anchorMin!=anchor||rect.anchorMax!=anchor)failures.Add($"{name} hangs from {rect.anchorMin} instead of {anchor}: it leaves its corner on any screen that isn't 16:9");
            }
            Anchored("HUD","VitalsFrame",0,1);Anchored("HUD","Status",0,1);Anchored("HUD","IntegrityBack",0,1);Anchored("HUD","Partner",0,1);
            Anchored("HUD","Target",.5f,1);Anchored("HUD","Zone",1,1);Anchored("HUD","Skills",0,0);Anchored("HUD","Skill3",1,0);Anchored("HUD","SkillLabel3",1,0);
            Anchored("HUD","Interaction",.5f,0);Anchored("HUD","BossBar",.5f,0);Anchored("HUD","BossTitleCard",.5f,.5f);Anchored("HUD","BossTitleEpithet",.5f,.5f);
            Anchored("HUD","SkillsPlate",0,0);Anchored("HUD","TargetPlate",.5f,1);Anchored("HUD","InteractionPlate",.5f,0);
            var scaler=Surface("HUD").GetComponent<CanvasScaler>();
            var clear=new List<(string a,string b)>();
            foreach(var (w,h,label) in Screens)
            {
                var size=CanvasAt(scaler,w,h);var boxes=new Dictionary<string,Rect>();
                foreach(var spot in Layout)
                {
                    var box=Placed(Spot(spot.surface,spot.name),(RectTransform)Surface(spot.surface).transform,size);
                    // A conversation hides the HUD, so its panel only has to stay on the screen.
                    if(spot.surface!="Dialogue")boxes[spot.name]=box;
                    if(label=="16:9"&&spot.w>0&&(Mathf.Abs(box.center.x-spot.x)>.6f||Mathf.Abs(box.center.y-spot.y)>.6f||Mathf.Abs(box.width-spot.w)>.6f||Mathf.Abs(box.height-spot.h)>.6f))
                        failures.Add($"16:9: {spot.name} moved to {box} from its place at ({spot.x},{spot.y}), size ({spot.w},{spot.h}): the layout everyone plays on changed");
                    if(box.xMin<-.6f||box.yMin<-.6f||box.xMax>size.x+.6f||box.yMax>size.y+.6f)
                        failures.Add($"{label}: {spot.name} runs off a {size.x:0}x{size.y:0} screen at {box}: part of it is cut off");
                }
                if(label=="16:9")
                {
                    var names=boxes.Keys.ToList();
                    for(int i=0;i<names.Count;i++)for(int j=i+1;j<names.Count;j++)if(!boxes[names[i]].Overlaps(boxes[names[j]]))clear.Add((names[i],names[j]));
                    Assert.Greater(clear.Count,100,"fixture: the 16:9 layout has clear pairs to keep clear");
                }
                else foreach(var (a,b) in clear)
                {
                    var one=boxes[a];var other=boxes[b];one.xMin+=.5f;one.xMax-=.5f;one.yMin+=.5f;one.yMax-=.5f;
                    if(one.Overlaps(other))failures.Add($"{label}: {a} lands on top of {b}, which are clear of each other at 16:9");
                }
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }

        // ------------------------------------------------------------------ ui238, copy16: the quick item
        static string Feed()=>string.Join(" | ",Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.name=="PickupLine").Select(t=>t.text));
        IEnumerator Squeeze(){yield return Pad(new GamepadState{leftTrigger=1});yield return Pad(new GamepadState());}
        [UnityTest] public IEnumerator TheQuickItemShowsItsCountAndAnswersEveryPress()
        {
            var hero=PartyController.Current.Active;hero.GetComponent<PlayerBrain>().AutoPilot=false;
            var state=GameServices.Current.State;state.quickItem="RepairGel";state.consumables["RepairGel"]=3;
            var heard=new List<string>();void Cue(string key,Transform emitter)=>heard.Add(key);
            AudioManager.CueStarted+=Cue;
            try
            {
                yield return null;yield return null;
                var hints=Text("Skills");
                StringAssert.Contains("REPAIR GEL ×3",hints.text,"the strip names the quick item but not how many are left");
                StringAssert.DoesNotContain("<color=",hints.text,"a stocked quick item is dimmed as if it were spent");
                // Full Integrity: refused, kept, and the press says so.
                hero.Health.integrity=hero.Health.maximum;yield return Squeeze();
                Assert.AreEqual(3,state.consumables["RepairGel"],"fixture: no gel is spent at full Integrity");
                CollectionAssert.Contains(heard,"ui_error","pressing the quick item at full Integrity makes no sound: it reads as a dead button");
                CollectionAssert.DoesNotContain(heard,"ui_confirm","a refused quick item sounds accepted");
                StringAssert.Contains("already full",Feed(),"pressing the quick item at full Integrity gives no reason");
                // Hurt: spent, and heard.
                heard.Clear();hero.Health.integrity=hero.Health.maximum*.5f;yield return Squeeze();
                Assert.AreEqual(2,state.consumables["RepairGel"],"fixture: a hurt hero uses the gel");
                CollectionAssert.Contains(heard,"ui_confirm","a Repair Gel goes off in the middle of a fight without a sound");
                CollectionAssert.DoesNotContain(heard,"ui_error","a used gel buzzes as if it were refused");
                StringAssert.Contains("REPAIR GEL ×2",hints.text,"the count on the strip doesn't follow the use");
                // None left: refused, and the strip reads as spent.
                heard.Clear();state.consumables["RepairGel"]=0;hero.Health.integrity=hero.Health.maximum*.5f;yield return Squeeze();
                CollectionAssert.Contains(heard,"ui_error","pressing the quick item with none left makes no sound");
                StringAssert.Contains("No Repair Gel left",Feed(),"pressing the quick item with none left gives no reason");
                yield return new WaitForSecondsRealtime(.35f);yield return null;
                StringAssert.Contains("REPAIR GEL ×0",hints.text,"the strip doesn't show that the item ran out");
                StringAssert.Contains("<color=",hints.text,"a spent quick item looks exactly like a stocked one");
            }
            finally{AudioManager.CueStarted-=Cue;}
        }

        // ------------------------------------------------------------------ copy15, copy30: what the strip and the card offer
        [UnityTest] public IEnumerator TheTownStripOffersSwapOnlyWithAPartnerAndNamesTheQuestLog()
        {
            // The first morning: Taren alone, nobody to swap with.
            GameServices.Current.State=new GameState();yield return Load("Hub_Decks");
            Assert.AreEqual(1,PartyController.Current.members.Length,"fixture: a new game starts solo");
            foreach(var device in new[]{PromptDevice.Keyboard,PromptDevice.Gamepad})
            {
                PromptService.ForceDevice(device);yield return null;yield return null;
                var strip=Text("Skills").text;
                StringAssert.DoesNotContain("SWAP",strip,$"on {device} the strip offers SWAP with nobody to swap with");
                StringAssert.Contains(PromptService.Tag("Log")+"  QUESTS",strip,$"on {device} nothing on screen says how to open the quest log");
            }
            DevLoadout.Apply("starter");yield return Load("Hub_Decks");
            foreach(var device in new[]{PromptDevice.Keyboard,PromptDevice.Gamepad})
            {
                PromptService.ForceDevice(device);yield return null;yield return null;
                var strip=Text("Skills").text;
                StringAssert.Contains(PromptService.Tag("Swap")+"  SWAP",strip,$"on {device} the strip hides SWAP from a party of two");
                StringAssert.Contains(PromptService.Tag("Log")+"  QUESTS",strip,$"on {device} the quest log hint went away with a partner");
            }
        }
        [UnityTest] public IEnumerator AQuestCardSaysWhichButtonReadsIt()
        {
            var state=GameServices.Current.State;state.questSteps.Clear();state.questCounts.Clear();
            var toast=Object.FindFirstObjectByType<QuestToast>();
            yield return new WaitForSecondsRealtime(.3f);
            RpgServices.Quests.Start("IshensRecord");
            float deadline=Time.unscaledTime+3;while(!toast.Heading.Contains("NEW QUEST")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(toast.Heading.Contains("NEW QUEST"),"fixture: a new quest is announced");
            var hint=Text("QuestToastHint");Assert.IsNotNull(hint,"a quest card doesn't say which button opens the quest log");
            StringAssert.Contains(PromptService.Tag("Log"),hint.text,"a new quest's card doesn't say which button reads it");
            RpgServices.Quests.Report(ObjectiveKind.Interact,"RecordPlate");
            deadline=Time.unscaledTime+8;while(!toast.Heading.Contains("QUEST UPDATED")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(toast.Heading.Contains("QUEST UPDATED"),"fixture: taking a record plate updates the quest");
            StringAssert.Contains(PromptService.Tag("Log"),hint.text,"a QUEST UPDATED card doesn't say which button reads it");
            Notices.Post("SAVED","Your progress is saved.");
            deadline=Time.unscaledTime+3;while(toast.Heading!="SAVED"&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual("SAVED",toast.Heading,"fixture: a notice takes over the card");
            Assert.IsTrue(string.IsNullOrEmpty(hint.text),"a card with nothing in the quest log still points at the quest log");
        }

        // ------------------------------------------------------------------ ui240: choices
        [UnityTest] public IEnumerator AChoiceSaysChooseAndInteractPicksWhatIsSelected()
        {
            var panel=Object.FindFirstObjectByType<DialoguePanel>();
            var heard=new List<string>();void Cue(string key,Transform emitter)=>heard.Add(key);
            AudioManager.CueStarted+=Cue;
            try
            {
                panel.Show("Hal",PortraitEmotion.Neutral,null,"Repair and save, or just visiting?");panel.SetVisibleCharacters(int.MaxValue);yield return null;
                int picked=-1;panel.Options(new List<string>{"Repair and save.","Just visiting."},i=>picked=i);yield return null;yield return null;
                var hint=Text("Continue");
                foreach(var device in new[]{PromptDevice.Keyboard,PromptDevice.Gamepad})
                {
                    PromptService.ForceDevice(device);yield return null;
                    StringAssert.DoesNotContain("CONTINUE",hint.text,$"on {device} the hint still says CONTINUE while a choice waits");
                    StringAssert.Contains("CHOOSE",hint.text,$"on {device} the hint doesn't say a choice is waiting");
                }
                PromptService.ForceDevice(null);
                EventSystem.current.SetSelectedGameObject(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="Option_1").gameObject);yield return null;
                heard.Clear();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return null;yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
                Assert.AreEqual(1,picked,"E plays a tick at a choice and chooses nothing: the keyboard player gets a sound and no result");
                CollectionAssert.DoesNotContain(heard,"dialogue_tick","choosing with E also plays the line's advance tick");
                // A pad's A chooses through the UI: it must not tick as well.
                panel.Options(new List<string>{"Repair and save.","Just visiting."},i=>picked=i);yield return null;yield return null;
                picked=-1;heard.Clear();yield return Tap(GamepadButton.South);
                Assert.AreEqual(0,picked,"fixture: A chooses the selected option");
                CollectionAssert.DoesNotContain(heard,"dialogue_tick","choosing with A also plays the line's advance tick");
                panel.ClearOptions();yield return null;
                StringAssert.Contains("CONTINUE",hint.text,"the hint stays on CHOOSE after the choice has gone");
                panel.Hide();
            }
            finally{AudioManager.CueStarted-=Cue;}
        }

        // ------------------------------------------------------------------ audi176: a conversation's edge
        [UnityTest] public IEnumerator AMashedLastLineDoesNotStartTheTalkAgain()
        {
            yield return Load("Hub_Decks");
            var dialogue=DialogueSystem.Current;var hero=PartyController.Current.Active;hero.GetComponent<PlayerBrain>().AutoPilot=false;
            var talker=new GameObject("Talker").AddComponent<Npc>();talker.speaker="Orrin";talker.firstNode="OrrinRepeat";talker.range=6;talker.transform.position=hero.transform.position;
            yield return null;yield return null;
            yield return Tap(GamepadButton.South);
            Assert.IsTrue(dialogue.Running,"fixture: A beside someone talks to them");
            float deadline=Time.unscaledTime+10;
            while(dialogue.Running&&Time.unscaledTime<deadline)
            {
                yield return Pad(new GamepadState().WithButton(GamepadButton.South));
                InputSystem.QueueStateEvent(pad,new GamepadState());
                for(int i=0;i<3&&dialogue.Running;i++)yield return null;
            }
            Assert.IsFalse(dialogue.Running,"fixture: the conversation closes under the thumb");
            float closed=Time.unscaledTime;yield return null;yield return null;
            int taps=0;
            while(Time.unscaledTime-closed<.2f)
            {
                taps++;yield return Tap(GamepadButton.South);
                Assert.IsFalse(dialogue.Running,$"tap {taps}, {Time.unscaledTime-closed:0.00} s after the last line: the mashed press starts the same conversation over");
            }
            Assert.Greater(taps,0,"fixture: the thumb kept tapping while the conversation closed");
            yield return new WaitForSecondsRealtime(Mathf.Max(0,closed+.5f-Time.unscaledTime));
            yield return Tap(GamepadButton.South);
            Assert.IsTrue(dialogue.Running,"half a second after a conversation closes, A still won't talk to the person standing there");
        }
        [UnityTest] public IEnumerator AttackAndDodgeDoNotWaitForTheHoldOff()
        {
            var dialogue=DialogueSystem.Current;var hero=PartyController.Current.Active;
            var prompt=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();prompt.range=6;prompt.transform.position=hero.transform.position;
            hero.GetComponent<PlayerBrain>().AutoPilot=false;
            dialogue.AutoAdvance=true;Assert.IsTrue(dialogue.StartNode("OrrinRepeat","Orrin"),"fixture: the conversation starts");
            float deadline=Time.unscaledTime+10;while(dialogue.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(dialogue.Running,"fixture: the conversation closes");
            yield return null;yield return null;
            int swings=hero.AttackSequence;
            yield return Pad(new GamepadState().WithButton(GamepadButton.South));
            Assert.AreEqual(0,prompt.Uses,"the press straight after a conversation closed went to the prompt beside the hero");
            Assert.Greater(hero.AttackSequence,swings,"attack is dead for a moment after every conversation");
            yield return Pad(new GamepadState());
            yield return Pad(new GamepadState().WithButton(GamepadButton.East));
            Assert.AreEqual(ActorState.Dodge,hero.State,"dodge is dead for a moment after every conversation");
            yield return Pad(new GamepadState());
        }

        // ------------------------------------------------------------------ ui229: the lessons
        [UnityTest] public IEnumerator TheFirstMarkedAttacksTeachTheFlashTimingOncePerSave()
        {
            // The proving grounds have Hal's own lesson: a mark there teaches nothing.
            var practice=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),PartyController.Current.Active.transform.position+Vector3.right*3);practice.Passive=false;
            float deadline=Time.unscaledTime+3;while(!practice.Telegraphing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(practice.Telegraphing,"fixture: the hound marks an attack");
            Assert.IsFalse(Has(FlashLessonFlags.Dodge),"a lesson spent itself in the practice yard");
            practice.Passive=true;Object.Destroy(practice.gameObject);

            yield return Load("Sorrel_Ridges");
            var party=PartyController.Current;var hero=party.Active;var partner=party.members[1-party.index];
            Put(hero,new Vector3(0,0,32));foreach(var member in party.members)member.Health.InvulnerableUntil=GameTime.Now+200;
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(ZoneController.Current.InSafePocket,"fixture: open ground");
            var heard=new List<string>();void Heard(string who,string line)=>heard.Add(who+": "+line);
            BarkService.Spoken+=Heard;
            try
            {
                for(int i=0;i<4;i++)
                {
                    var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),hero.transform.position+Quaternion.Euler(0,i*90,0)*Vector3.forward*3.5f);
                    hound.Passive=false;hound.Health.Damageable=false;
                }
                deadline=Time.unscaledTime+6;while(!Has(FlashLessonFlags.Dodge)&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(Has(FlashLessonFlags.Dodge),"the first fight's marked attacks start and nobody says a word about dodging them");
                string dodge=BarkService.Line(partner.character,"teach.flash");
                Assert.IsNotNull(dodge,"the partner has no line for the dodge lesson");
                Assert.AreEqual(1,heard.Count(l=>l==partner.character+": "+dodge),"the partner doesn't speak the dodge lesson: "+string.Join(" | ",heard));
                Assert.IsFalse(Has(FlashLessonFlags.Guard),"both lessons arrive on one mark");
                deadline=Time.unscaledTime+30;while(!Has(FlashLessonFlags.Guard)&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(Has(FlashLessonFlags.Guard),"nothing ever teaches the guard timing in play");
                string guard=BarkService.Line(partner.character,"teach.guard");
                Assert.IsNotNull(guard,"the partner has no line for the guard lesson");
                Assert.AreEqual(1,heard.Count(l=>l==partner.character+": "+guard),"the partner doesn't speak the guard lesson: "+string.Join(" | ",heard));
                Assert.AreEqual(1,heard.Count(l=>l==partner.character+": "+dodge),"the dodge lesson repeats in the same fight");
                // Whichever hero the player leads, the other one has the words; and a reload doesn't start the lessons again.
                foreach(var who in new[]{"Taren","Sela"})foreach(var line in new[]{"teach.flash","teach.guard"})Assert.IsNotNull(BarkService.Line(who,line),who+" has no "+line+" line");
                var saves=new SaveSystem(Path.Combine(Application.temporaryCachePath,"QaHudFlow-"+System.Guid.NewGuid().ToString("N")));
                saves.Save("slot1",GameServices.Current.State);GameServices.Current.State=saves.Load("slot1");
                heard.Clear();yield return new WaitForSecondsRealtime(4);
                Assert.IsFalse(heard.Any(l=>l.EndsWith(dodge)||l.EndsWith(guard)),"the Flash lessons start over after a reload: "+string.Join(" | ",heard));
            }
            finally{BarkService.Spoken-=Heard;}
            // The game rebuilds its copy from the writing folder: a line in only one of them would vanish.
            string writing=File.ReadAllText(Path.Combine(Application.dataPath,"..","..","docs","writing","barks.json"));
            string game=File.ReadAllText(Path.Combine(Application.dataPath,"_Project","Resources","Audio","barks.json"));
            Assert.AreEqual(writing,game,"the lessons exist in only one copy of the barks, so a rebuild would take them out of the game");
        }
        /// <summary>Spelled here so these checks read the save's own flags, whatever the code calls them.</summary>
        static class FlashLessonFlags{public const string Dodge="taught.flash",Guard="taught.guard";}

        // ------------------------------------------------------------------ boss279: the dive
        [UnityTest] public IEnumerator TheLockHoldsThroughTheBurrowersDive()
        {
            var hero=PartyController.Current.Active;var partner=PartyController.Current.members[1-PartyController.Current.index];
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            yield return null;
            Put(hero,new Vector3(30,0,30));hero.Health.InvulnerableUntil=GameTime.Now+30;
            var rig=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),new Vector3(30,0,40));rig.Passive=false;var boss=rig.GetComponent<BossController>();
            yield return null;yield return null;yield return null;rig.Passive=true;yield return null;
            // Something else to fight while the rig is below ground, and it lasts the whole check: the partner's needles would finish it.
            var mite=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Scrapmite"),new Vector3(34,0,30));mite.Passive=true;mite.Health.InvulnerableUntil=GameTime.Now+30;yield return null;
            hero.GetComponent<PlayerBrain>().AutoPilot=false;hero.target=rig.Health;hero.TargetLocked=true;yield return null;yield return null;
            Assert.IsTrue(hero.TargetLocked&&hero.target==rig.Health,"fixture: the rig is locked");
            // The partner fights beside the hero, as in the real fight.
            Put(partner,new Vector3(26,0,30));partner.Health.InvulnerableUntil=GameTime.Now+30;partner.GetComponent<PartnerBrain>().enabled=true;
            Assert.IsTrue(boss.Perform("Burrow"),"fixture: the rig dives");
            float deadline=Time.unscaledTime+3;while(!boss.Burrowed&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(boss.Burrowed,"fixture: the rig is below ground");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(rig.Health,hero.target,"the dive moves the lock to whatever else is standing there");
            Assert.IsTrue(hero.TargetLocked,"the dive drops the lock-on");
            Assert.AreEqual(mite.Health,partner.target,"the partner goes on swinging at a rig nothing can hit while a mite stands open");
            // The hunt ends exactly under a hero who stands still, for up to 1.75 s, and a skill or a shot can be pressed in that time.
            float Underfoot(){var gap=rig.transform.position-hero.transform.position;gap.y=0;return gap.magnitude;}
            deadline=Time.unscaledTime+3;while(Underfoot()>=.05f&&Time.unscaledTime<deadline)yield return null;
            Assert.Less(Underfoot(),.05f,"fixture: the hunt reaches the hero's feet");
            var aim=(Vector3)typeof(CombatActor).GetMethod("Aim",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(hero,null);
            Assert.Greater(aim.sqrMagnitude,.5f,"with the rig underfoot the hero's aim is no direction at all: a stickless blink goes nowhere and a slam leaves the hero facing nothing");
            var shots=hero.GetComponent<RangedContact>();shots.Begin(GroundMove.Needle,aim,default);
            Assert.IsNull(typeof(RangedContact).GetField("aimedTarget",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(shots),"a shot fired with the rig underfoot is steered at a body nothing can hit, and leaves behind the hero instead of ahead");
            // With nothing open, the partner goes back to following.
            mite.Health.Damageable=false;yield return null;yield return null;
            Assert.IsNull(partner.target,"with nothing to fight during the dive, the partner stands in the dust ring swinging at the buried rig instead of following");
            deadline=Time.unscaledTime+6;while(boss.Burrowed&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(boss.Burrowed,"fixture: the rig surfaces");
            yield return null;yield return null;
            Assert.AreEqual(rig.Health,hero.target,"the rig surfaces into its opening and the hero isn't aimed at it");
            Assert.IsTrue(hero.TargetLocked,"the rig surfaces into its opening with the lock gone, and the player has to press again");
            Assert.AreEqual(rig.Health,partner.target,"the rig surfaces into its opening and the partner doesn't come back to it");
            Object.Destroy(mite.gameObject);Object.Destroy(rig.gameObject);
        }

        // ------------------------------------------------------------------ ui242: with an enemy on the hero, A is an attack
        static T Beside<T>(Vector3 at) where T:InteractionPrompt
        {var prompt=new GameObject(typeof(T).Name+" fixture").AddComponent<T>();prompt.range=6;prompt.transform.position=at;return prompt;}
        [UnityTest] public IEnumerator WithAnEnemyOnTheHeroAIsAnAttackNotAPrompt()
        {
            var hero=PartyController.Current.Active;var menu=Object.FindFirstObjectByType<PauseMenu>();
            Put(hero,new Vector3(30,0,30));hero.GetComponent<PlayerBrain>().AutoPilot=false;
            hero.Health.InvulnerableUntil=GameTime.Now+60;hero.Health.integrity=hero.Health.maximum*.5f;
            var at=hero.transform.position;
            var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),at+Vector3.right*3);hound.Passive=false;hound.Health.InvulnerableUntil=GameTime.Now+60;
            var bench=Beside<Bench>(at);var node=Beside<MineNode>(at);node.nodeId="qa.calm";var bay=Beside<RepairBay>(at);bay.free=true;
            var dock=Beside<DockingPad>(at);dock.scene="Arena_Flight";var warp=Beside<WarpBeacon>(at);warp.requiredFlag="";warp.scene="Arena_Flight";
            yield return null;yield return null;
            void Shut(InteractionPrompt prompt,string answer)=>Assert.IsFalse(prompt.Available,$"with a hound on the hero, A still {answer} instead of swinging");
            Shut(bench,"opens the Lattice Anvil and pauses the fight");Shut(node,"mines the crystal");Shut(bay,"repairs and saves mid-fight");
            Shut(dock,"launches out of the fight");Shut(warp,"warps out of the fight");
            float integrity=hero.Health.integrity;int swings=hero.AttackSequence;
            yield return Tap(GamepadButton.South);
            Assert.IsFalse(menu.IsOpen,"A beside a Lattice Anvil with a hound on the hero pauses the fight on the bench");
            Assert.IsFalse(Has("mined.qa.calm"),"A beside a crystal with a hound on the hero mines it");
            Assert.AreEqual(integrity,hero.Health.integrity,"A beside a repair bay with a hound on the hero repairs in the middle of the fight");
            Assert.IsFalse(SceneFlow.Current.Loading,"A beside a way out with a hound on the hero leaves the zone");
            Assert.Greater(hero.AttackSequence,swings,"A with a hound on the hero and no prompt answering doesn't swing");
            // Ten metres back, the quiet work still waits but the way out is open: a party can run for the door.
            hound.transform.position=at+Vector3.right*10;
            Assert.IsTrue(dock.Available&&warp.Available,"with the enemy ten metres behind, the dock and the warp stay shut: a party can't run for the way out");
            Assert.IsFalse(bench.Available,"the Lattice Anvil opens with an awake enemy ten metres away");
            // With nothing awake, everything answers again.
            hound.Passive=true;
            Assert.IsTrue(bench.Available&&node.Available&&bay.Available&&dock.Available&&warp.Available,"the prompts stay shut with nothing awake near them");
        }

        // ------------------------------------------------------------------ what floats across the frames
        [UnityTest] public IEnumerator NumbersAndPickupsStayOffTheVitalsAndTheObjective()
        {
            var camera=Camera.main;Assert.IsNotNull(camera,"fixture: a camera");
            var frames=new[]{"VitalsFrame","ObjectiveFrame"}.Select(Picture).ToArray();
            foreach(var frame in frames)
            {
                Assert.IsNotNull(frame,"fixture: both frames are up");
                var c=new Vector3[4];frame.rectTransform.GetWorldCorners(c);var centre=(c[0]+c[2])*.5f;
                // A body standing so that its number floats exactly over this frame's middle.
                var body=new GameObject("Body fixture").AddComponent<Health>();body.id="Fixture";body.maximum=body.integrity=500;
                body.transform.position=camera.ScreenToWorldPoint(new Vector3(centre.x,centre.y,20))-Vector3.up*1.9f;
                body.Receive(new DamagePacket{amount=5,type=DamageType.Pulse});
                int seen=0;
                for(int i=0;i<6;i++)
                {
                    yield return null;
                    foreach(var number in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.name=="Damage"))
                    {
                        seen++;
                        foreach(var other in frames)Assert.IsFalse(Box(number.rectTransform,1).Overlaps(Box(other.rectTransform)),$"a damage number floats across {other.name}, over what the player is reading mid-fight");
                    }
                }
                Assert.Greater(seen,0,"fixture: the hit raised a number");
                Object.Destroy(body.gameObject);
            }
            RpgServices.Inventory.Grant("ScrapAlloy",2);yield return null;yield return null;
            var line=Text("PickupLine");Assert.IsNotNull(line,"fixture: the pickup shows a line");
            Assert.IsFalse(Box(line.rectTransform).Overlaps(Box(frames[0].rectTransform)),"the pickup line is drawn across the bottom of the vitals frame");
        }

        // ------------------------------------------------------------------ ui230: one type floor
        [UnityTest] public IEnumerator ThePlanAndTheCheckShareOneFloorAndTheBossTextMeetsIt()
        {
            string plan=File.ReadAllText(Path.Combine(Application.dataPath,"..","..","docs","TEST_PLAN.md"));
            StringAssert.DoesNotContain("below 18 px",plan,"TEST_PLAN 4.12 promises 18 px while ReadableTypeTests enforces 16: nobody can tell which bar small text has to clear");
            StringAssert.Contains("below 16 px at 1280×720",plan,"TEST_PLAN 4.12 doesn't state the 16 px floor the check enforces");
            // The boss bar, the title card and a quest card are only up in their moments, so the screen audit never sees them.
            var hero=PartyController.Current.Active;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*12);boss.Passive=false;
            hero.Health.InvulnerableUntil=GameTime.Now+10;
            Notices.Post("QUEST UPDATED","A quest's name\nThe next step of it");
            yield return new WaitForSecondsRealtime(.5f);
            var failures=new List<string>();
            foreach(string name in new[]{"BossName","BossEpithet","BossState","BossTitleCard","BossTitleEpithet","QuestToastHeading","QuestToastBody","QuestToastHint"})
            {
                var text=Text(name);if(text==null){failures.Add("no "+name);continue;}
                if(!text.isActiveAndEnabled){failures.Add($"fixture: {name} isn't up");continue;}
                if(text.fontSize<24)failures.Add($"{name} is {text.fontSize} units: under 16 px at 1280x720");
                if(string.IsNullOrWhiteSpace(text.text))continue;
                var rect=text.rectTransform.rect;var need=text.GetPreferredValues(text.text,rect.width,0);
                if(need.x>rect.width+1||need.y>rect.height+1)failures.Add($"{name} overflows its {rect.width:0}x{rect.height:0} box, needing {need.x:0}x{need.y:0}");
            }
            boss.Passive=true;Object.Destroy(boss.gameObject);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
