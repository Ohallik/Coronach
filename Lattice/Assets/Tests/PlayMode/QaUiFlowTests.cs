using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass, last UI round: what a kill paid, who has something new to say, what else is waiting in this zone,
    /// how the chapter ends, when a save was made, and the Precision's LB. Every check here fails on the build it was written against.</summary>
    public sealed class QaUiFlowTests
    {
        static GameState State=>GameServices.Current.State;
        static PauseMenu Menu=>Object.FindFirstObjectByType<PauseMenu>();
        static QuestToast Toast=>Object.FindFirstObjectByType<QuestToast>();
        static CombatActor Hero=>PartyController.Current.Active;
        Gamepad pad;Keyboard keyboard;InputDevice joystick;
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
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Menu;if(menu!=null&&menu.IsOpen)menu.Close();
            if(joystick!=null&&joystick.added)InputSystem.RemoveDevice(joystick);
            joystick=null;
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            pad=null;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            keyboard=null;
            // Back to the default device, which also drops any tag cached off another device's words.
            PromptService.ForceDevice(PromptDevice.Keyboard);PromptService.ForceDevice(null);InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            yield return new WaitForSecondsRealtime(.35f);
        }
        /// <summary>Long enough for anything that looks at the game on a timer: the objective panel, the quest card's scan, the people's marks.</summary>
        static IEnumerator Settle(){yield return new WaitForSecondsRealtime(.4f);}
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            hero.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static EnemyBrain Spawn(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        static void Kill(EnemyBrain enemy)=>enemy.Health.Receive(new DamagePacket{source=Hero.Health,amount=1000000,type=DamageType.Pulse});
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static float ColorGap(Color a,Color b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);

        // ------------------------------------------------------------------ what's on screen
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static Button[] Rows()=>Buttons().Where(b=>b.name.StartsWith("Row_")).OrderBy(b=>int.Parse(b.name.Substring(4))).ToArray();
        static string Words(Component c){var t=c.GetComponentInChildren<TMP_Text>();return t!=null?t.text:"";}
        static Button With(string words)
        {
            var found=Buttons().FirstOrDefault(b=>Words(b).Contains(words));
            Assert.IsNotNull(found,$"no row says \"{words}\". On screen: {string.Join(" | ",Buttons().Select(b=>Words(b)))}");return found;
        }
        static GameObject Selected()
        {var s=EventSystem.current.currentSelectedGameObject;Assert.IsNotNull(s,"nothing has focus: a pad player is stuck");return s;}
        static string Note(){var t=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(n=>n.name=="Note");Assert.IsNotNull(t,"the page has no note");return t.text;}
        static string Feedback(){var t=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(n=>n.name=="Feedback");return t!=null?t.text:"";}
        static void FitsItsBox(TMP_Text text,string surface)
        {
            var box=text.rectTransform.rect;var need=text.GetPreferredValues(text.text,box.width,0);
            Assert.LessOrEqual(need.x,box.width+1,$"{surface}: \"{text.text}\" runs {need.x:0} wide in a {box.width:0} box");
            Assert.LessOrEqual(need.y,box.height+1,$"{surface}: \"{text.text}\" needs {need.y:0} of height in a {box.height:0} box, so a word has dropped to a line with no room");
        }
        /// <summary>The label wraps inside its own margins, and a row's height holds one line.</summary>
        static void RowFits(Button row,string surface)
        {
            var text=row.GetComponentInChildren<TMP_Text>();var box=text.rectTransform.rect;
            var need=text.GetPreferredValues(text.text,box.width-text.margin.x-text.margin.z,0);
            Assert.LessOrEqual(need.y,text.fontSize*1.3f,$"{surface}: \"{text.text}\" takes more than one line of its row");
        }
        IEnumerator PadPress(GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;
        }
        IEnumerator Hold(GamepadState state){InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;}
        static GamepadState LB=>new GamepadState().WithButton(GamepadButton.LeftShoulder);
        /// <summary>The real thing, bridged as Nathan's is: a digital pad whose only directions walk.</summary>
        const string PrecisionLayout="{ \"name\": \"QaUiFlowPrecision\", \"extend\": \"Joystick\", \"device\": { \"interface\": \"QaUiFlow\", \"product\": \"Logitech.*Precision.*\" }, \"controls\": [ { \"name\": \"button2\", \"layout\": \"Button\", \"format\": \"BIT\", \"offset\": 0, \"bit\": 5, \"sizeInBits\": 1 } ] }";
        static bool precisionRegistered;
        IEnumerator AddPrecision()
        {
            PadSupport.Install();if(!PadBridge.Installed)PadBridge.Install();
            if(!precisionRegistered){InputSystem.RegisterLayout(PrecisionLayout);precisionRegistered=true;}
            joystick=InputSystem.AddDevice(new InputDeviceDescription{interfaceName="QaUiFlow",product="Logitech(R) Precision(TM) Gamepad"});yield return null;
            pad=PadBridge.PadFor(joystick);Assert.IsNotNull(pad,"fixture: the Precision isn't bridged: "+PadBridge.Describe());
            Assert.IsTrue(PadBridge.StickIsDpad(pad),"fixture: this pad's directions are its d-pad");
            pad.MakeCurrent();yield return null;
        }

        // ------------------------------------------------------------------ copy6: what a kill paid
        static TMP_Text[] FeedLines()=>Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.name=="PickupLine").ToArray();
        [UnityTest] public IEnumerator AKillsXpShowsQuietlyInTheFeedAndABurstAddsUpOnOneLine()
        {
            int xp=GameCatalog.Find<EnemyDef>("Scrapmite").xp;Assert.Greater(xp,0,"fixture: a Scrapmite is worth XP");
            var first=Spawn("Scrapmite",new Vector3(30,0,30));var second=Spawn("Scrapmite",new Vector3(34,0,30));yield return null;yield return null;
            Kill(first);yield return null;yield return null;
            TMP_Text Xp()=>FeedLines().FirstOrDefault(t=>t.text.EndsWith(" XP"));
            Assert.IsNotNull(Xp(),"a kill pays XP and the feed under the vitals says nothing about it: "+string.Join(" | ",FeedLines().Select(t=>t.text)));
            Assert.AreEqual($"+{xp} XP",Xp().text,"the XP line doesn't say what the kill paid");
            Assert.Less(ColorGap(Xp().color,UiKit.DimTextColor),.02f,"the XP line comes with every kill and shouts as loud as a real pickup");
            var scrip=FeedLines().First(t=>t.text.EndsWith(" scrip"));Assert.Greater(ColorGap(scrip.color,UiKit.DimTextColor),.2f,"fixture: what was picked up stays bright beside it");
            Kill(second);yield return null;yield return null;
            Assert.AreEqual(1,FeedLines().Count(t=>t.text.EndsWith(" XP")),"two quick kills stack two XP lines, and a real fight would flood the feed");
            Assert.AreEqual($"+{xp*2} XP",Xp().text,"a burst of kills doesn't add up on its one line");
            // A quest's XP is named on its card: the feed stays out of it, as it does for the rest of that reward.
            string before=Xp().text;State.questSteps["PellsLamp"]=1;RpgServices.Quests.Report(ObjectiveKind.Flag,"pell.lampReturned");yield return null;yield return null;
            Assert.AreEqual(GameCatalog.Find<QuestDef>("PellsLamp").steps.Length,State.questSteps["PellsLamp"],"fixture: the quest finished");
            Assert.AreEqual(before,Xp().text,"a quest's XP lands in the kill's line as well as on its own card");
        }

        // ------------------------------------------------------------------ copy23: where parts are made
        [UnityTest] public IEnumerator TheFabricatePageSaysPartsCanAlsoBeMadeAtHalsBench()
        {
            var menu=Menu;menu.Open(PauseMenu.FabricatePage);yield return null;
            StringAssert.Contains("Lattice Anvil or Hal's bench",Note(),"the page sends a player at the Decks hunting for an Anvil, with Hal's bench a walk away");
            With("Scrap Edge").onClick.Invoke();yield return null;
            StringAssert.Contains("Lattice Anvil or Hal's bench",Feedback(),"the refusal names only the Anvil, and Hal's bench makes parts too");
            menu.Close();yield return null;
            menu.Open(PauseMenu.FabricatePage,true);yield return null;
            StringAssert.DoesNotContain("Lattice Anvil linked",Note(),"at Hal's bench the page says a Lattice Anvil is linked: it's the same code path for both");
            StringAssert.Contains("made here",Note(),"at a bench the page doesn't say what it can make");
            menu.Close();
        }

        // ------------------------------------------------------------------ copy30: switching targets
        /// <summary>A keyboard names these keys as a player's does: a real one's keys go by the OS's names, and Windows calls them Left and
        /// Right. A headless run has no keyboard at all, so on its own it only ever reads the layout's Left Arrow.</summary>
        const string OsNamedKeyboard="{ \"name\": \"QaUiFlowOsKeyboard\", \"extend\": \"Keyboard\", \"controls\": [ { \"name\": \"leftArrow\", \"displayName\": \"Left\" }, { \"name\": \"rightArrow\", \"displayName\": \"Right\" } ] }";
        static bool osNamesRegistered;
        [UnityTest] public IEnumerator TheTargetLineSaysHowToSwitchTargetsInEachDevicesWords()
        {
            var hound=Spawn("Ridgehound",Hero.transform.position+Vector3.forward*6);yield return null;yield return null;
            // The widest gauge a body has: three digits on both sides.
            var body=hound.Health;body.breakThreshold=999;body.Receive(new DamagePacket{source=Hero.Health,amount=1,type=DamageType.Pulse,breakPower=400});
            Hero.target=body;Hero.TargetLocked=true;
            var line=Text("Target");Assert.IsNotNull(line,"fixture: the target line");
            PromptService.ForceDevice(PromptDevice.Keyboard);yield return null;yield return null;
            StringAssert.Contains("BREAK",line.text,"fixture: the gauge shows");
            StringAssert.Contains("Left/Right Arrow",line.text,"on a keyboard nothing on screen says the arrow keys switch targets");
            StringAssert.DoesNotContain("D-pad",line.text,"the keyboard's line names a pad");
            FitsItsBox(line,"keyboard target line");
            // The words are looked up once and kept, so they're let go of before reading them off the keyboard a player has.
            if(!osNamesRegistered){InputSystem.RegisterLayout(OsNamedKeyboard);osNamesRegistered=true;}
            keyboard=(Keyboard)InputSystem.AddDevice("QaUiFlowOsKeyboard");yield return null;
            Assert.AreEqual("Left",PromptService.Label("CycleTarget","Negative"),"fixture: the key goes by the name the OS gives it");
            typeof(DeviceHints).GetField("targetKeys",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,null);yield return null;yield return null;
            StringAssert.Contains("Left/Right Arrow",line.text,"on a real keyboard the OS calls these keys Left and Right and the line loses the word saying which keys it means: left and right here are A and D");
            FitsItsBox(line,"keyboard target line, with the OS's names");
            InputSystem.RemoveDevice(keyboard);keyboard=null;
            pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();PromptService.ForceDevice(PromptDevice.Gamepad);yield return null;yield return null;
            StringAssert.Contains("D-pad left/right",line.text,"on an analog pad nothing on screen says the d-pad switches targets");
            StringAssert.DoesNotContain("Arrow",line.text,"the pad's line names keys");
            FitsItsBox(line,"analog pad target line");
            InputSystem.RemoveDevice(pad);pad=null;yield return AddPrecision();yield return null;
            StringAssert.Contains(PromptService.Tag("LockOn")+" + left/right",line.text,"on a Precision the d-pad walks: the line has to say LB and a direction");
            StringAssert.DoesNotContain("D-pad",line.text,"a Precision is told to use a d-pad that walks instead");
            FitsItsBox(line,"Precision target line");
            // A boss's bar takes the gauge and the name, and the line stays for the lock and the switch: the mites and links are targets too.
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),Hero.transform.position+Vector3.forward*12);boss.Passive=false;Hero.Health.InvulnerableUntil=GameTime.Now+10;
            yield return null;yield return null;yield return null;
            Hero.target=boss.Health;yield return null;yield return null;
            StringAssert.DoesNotContain("BREAK",line.text,"fixture: the boss's own line");
            StringAssert.Contains("left/right",line.text,"in a boss fight nothing says how to get from the boss to what's around it");
            FitsItsBox(line,"boss target line");
            boss.Passive=true;Object.Destroy(boss.gameObject);
        }

        // ------------------------------------------------------------------ ques135: which card goes first
        [UnityTest] public IEnumerator AQuestFinishedInTheFieldShowsItsCardBeforeItsLevelUp()
        {
            var toast=Toast;State.questSteps["PellsLamp"]=1;yield return Settle();
            float deadline=Time.unscaledTime+12;while(toast.Showing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(toast.Showing,"fixture: the first card leaves");
            foreach(var member in State.party)member.xp=Levels.RequiredXp(member.level)-1;
            // As it usually stands when a quest pays: the card's next look at the quests is a moment away and the level card is queued already.
            typeof(QuestToast).GetField("next",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(toast,Time.unscaledTime+1);
            RpgServices.Quests.Report(ObjectiveKind.Flag,"pell.lampReturned");
            Assert.AreEqual(GameCatalog.Find<QuestDef>("PellsLamp").steps.Length,State.questSteps["PellsLamp"],"fixture: the quest is finished");
            deadline=Time.unscaledTime+3;while(!toast.Showing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(toast.Showing,"fixture: a card shows");
            Assert.AreEqual("QUEST COMPLETE",toast.Heading,"the level-up the quest paid for takes the card first, and QUEST COMPLETE waits behind it for 4.5 s");
            deadline=Time.unscaledTime+8;while(toast.Heading=="QUEST COMPLETE"&&Time.unscaledTime<deadline)yield return null;
            StringAssert.StartsWith("SYNC",toast.Heading,"the level-up never got its turn");
        }

        // ------------------------------------------------------------------ skil209: the chooser
        [UnityTest] public IEnumerator TheSkillChooserShowsEachSkillsMasteryAndItsNoteSaysWhatItDoes()
        {
            Assert.AreEqual("Taren",State.party[State.activeMember].id,"fixture: Taren leads");
            var taren=State.party.Find(m=>m.id=="Taren");taren.level=12;taren.skillUses=null;
            var all=SkillBook.All("Taren");var worn=SkillBook.Equipped("Taren",0);
            var spare=all.First(s=>Enumerable.Range(0,SkillBook.Slots).All(i=>SkillBook.Equipped("Taren",i)!=s));
            for(int i=0;i<7;i++)SkillBook.RecordUse("Taren",spare);
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;
            Row("Row_1").onClick.Invoke();yield return null;
            StringAssert.Contains("Mastery 2 (7/18)",Words(With(spare.displayName)),"a skill that isn't in a slot shows no mastery anywhere");
            StringAssert.Contains($"{spare.cooldown:0} s",Words(With(spare.displayName)),"the chooser lost its cooldown");
            // What a skill does follows the focus, as the quest log's note does, and the trade rule stays beside it.
            StringAssert.Contains(worn.description,Note(),"the note doesn't open on the skill in this slot");
            EventSystem.current.SetSelectedGameObject(With(spare.displayName).gameObject);yield return null;
            StringAssert.Contains($"{spare.displayName}: {spare.description}",Note(),"moving to a skill doesn't say what it does");
            StringAssert.DoesNotContain(worn.description,Note(),"the note kept the last skill's words");
            StringAssert.Contains("trades places",Note(),"the note lost what happens to a skill in another slot");
            menu.Close();yield return null;
            // The widest row the chooser can draw, in either device's words.
            taren.skillUses=all.ToDictionary(s=>s.id,s=>59);
            foreach(var device in new[]{PromptDevice.Gamepad,PromptDevice.Keyboard})
            {
                PromptService.ForceDevice(device);yield return null;
                menu.Open(PauseMenu.SkillsPage);yield return null;Row("Row_1").onClick.Invoke();yield return null;
                StringAssert.Contains("Mastery 4 (59/60)",Words(With(spare.displayName)),"fixture: the longest mastery");
                foreach(var row in Rows())RowFits(row,device+" chooser");
                menu.Close();yield return null;
            }
        }

        // ------------------------------------------------------------------ ui238: the Precision's LB
        [UnityTest] public IEnumerator OnAPrecisionLbTogglesTheLockOnItsReleaseUnlessADirectionWasPressed()
        {
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            yield return null;
            var hero=Hero;Put(hero,new Vector3(30,0,30));
            Spawn("Ridgehound",new Vector3(34,0,30));Spawn("Ridgehound",new Vector3(26,0,30));yield return null;yield return null;
            yield return AddPrecision();
            hero.GetComponent<PlayerBrain>().AutoPilot=false;yield return null;yield return null;
            Assert.IsFalse(hero.TargetLocked,"fixture: nothing is locked yet");
            // A tap.
            yield return Hold(LB);
            Assert.IsFalse(hero.TargetLocked,"on a Precision LB also starts a cycle, and the press alone locks onto the nearest enemy before any direction comes");
            yield return Hold(new GamepadState());
            Assert.IsTrue(hero.TargetLocked,"a tap of LB, let go with no direction, doesn't lock on");
            var locked=hero.target;Assert.IsNotNull(locked,"fixture: the lock has a target");
            // LB and a direction: a different quick item, and the lock untouched.
            string item=State.quickItem;
            yield return Hold(LB);yield return Hold(LB.WithButton(GamepadButton.DpadUp));yield return Hold(LB);yield return Hold(new GamepadState());
            Assert.AreNotEqual(item,State.quickItem,"fixture: LB and up cycles the quick item");
            Assert.IsTrue(hero.TargetLocked,"changing the quick item in a fight drops the lock");
            Assert.AreSame(locked,hero.target,"changing the quick item in a fight moves the lock");
            // LB and another direction: the next target, still locked when LB comes up.
            yield return Hold(LB);yield return Hold(LB.WithButton(GamepadButton.DpadRight));yield return Hold(LB);yield return Hold(new GamepadState());
            Assert.AreNotSame(locked,hero.target,"fixture: LB and right moves the lock");
            Assert.IsTrue(hero.TargetLocked,"letting LB go after switching targets drops the lock it just moved");
            // A tap while walking: a direction already held isn't a cycle. WithButton changes the state it's called on, so the walk's stays clean of LB.
            var walking=new GamepadState().WithButton(GamepadButton.DpadLeft);
            yield return Hold(walking);yield return Hold(LB.WithButton(GamepadButton.DpadLeft));yield return Hold(walking);
            Assert.IsFalse(hero.TargetLocked,"tapping LB while walking on the d-pad doesn't unlock");
            yield return Hold(new GamepadState());
            // A key on the same desk answers on its press, as it always has.
            keyboard=InputSystem.AddDevice<Keyboard>();yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));yield return null;yield return null;
            Assert.IsTrue(hero.TargetLocked,"with a Precision plugged in, the lock-on key waits for its release");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
            Assert.IsTrue(hero.TargetLocked,"releasing the lock-on key toggles it a second time");
            // Any other pad is untouched: LB answers on the press, and a stick leaves its d-pad free.
            InputSystem.RemoveDevice(joystick);joystick=null;pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();yield return null;
            yield return Hold(LB);
            Assert.IsFalse(hero.TargetLocked,"an analog pad's LB no longer answers on its press");
            yield return Hold(new GamepadState());
            Assert.IsFalse(hero.TargetLocked,"an analog pad's LB toggles twice, on the press and on the release");
        }

        // ------------------------------------------------------------------ ui238: the Items page
        [UnityTest] public IEnumerator TheItemsPageSetsTheQuickItemWithTheSwapButton()
        {
            pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();PromptService.ForceDevice(PromptDevice.Gamepad);
            State.quickItem="RepairGel";int active=State.activeMember;
            var menu=Menu;menu.Open(PauseMenu.ItemsPage);yield return null;
            StringAssert.Contains("Quick item",Words(With("Repair Gel")),"nothing on the Items page shows which row is the quick item");
            StringAssert.DoesNotContain("Quick item",Words(With("Charge Cell")),"two rows claim to be the quick item");
            StringAssert.Contains("Press "+PromptService.Tag("Swap"),Note(),"the note doesn't say which button sets the quick item");
            EventSystem.current.SetSelectedGameObject(With("Charge Cell").gameObject);yield return null;
            yield return PadPress(GamepadButton.North);
            Assert.AreEqual("ChargeCell",State.quickItem,"the Swap button on a consumable's row doesn't make it the quick item: the page can only say how to change it mid-fight");
            Assert.AreEqual(active,State.activeMember,"setting the quick item also swapped hero");
            StringAssert.Contains("Quick item",Words(With("Charge Cell")),"the row doesn't show the change");
            StringAssert.DoesNotContain("Quick item",Words(With("Repair Gel")),"the old row still says it's the quick item");
            StringAssert.Contains("Charge Cell is now the quick item",Feedback(),"nothing says what the press did");
            StringAssert.Contains("Charge Cell",Words(Selected().transform),"the press moved the cursor off the row");
            // A material can't be used at all, and says so.
            EventSystem.current.SetSelectedGameObject(With("Scrap Alloy").gameObject);yield return null;
            yield return PadPress(GamepadButton.North);
            Assert.AreEqual("ChargeCell",State.quickItem,"a material became the quick item, a button that can only ever refuse");
            StringAssert.Contains("consumable",Feedback(),"the press on a material says nothing");
            // Tab does it on a keyboard, and the note says Tab there.
            PromptService.ForceDevice(PromptDevice.Keyboard);yield return null;
            StringAssert.Contains("Press "+PromptService.Tag("Swap"),Note(),"the keyboard's note names the pad's button");
            keyboard=InputSystem.AddDevice<Keyboard>();yield return null;
            EventSystem.current.SetSelectedGameObject(With("Repair Gel").gameObject);yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
            Assert.AreEqual("RepairGel",State.quickItem,"Tab doesn't set the quick item on a keyboard");
            menu.Close();
        }

        // ------------------------------------------------------------------ ques137, scri105: people with something new to say
        static Npc Person(string speaker)=>Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(n=>n.speaker==speaker);
        /// <summary>The label the camera can see: a hailed person's own child label is switched off, and their sign hangs beside them.</summary>
        static TextMeshPro ShownLabel(Npc npc)
        {
            string name="Sign_"+DisplayNames.Speaker(npc.speaker);
            var label=Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name&&t.GetComponent<Renderer>().enabled&&Vector3.Distance(t.transform.position,npc.transform.position)<6);
            Assert.IsNotNull(label,"fixture: "+npc.speaker+" has a label on screen");return label;
        }
        static IEnumerator Talk(Npc npc)
        {
            DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;
            float deadline=Time.unscaledTime+25;
            while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,npc.speaker+"'s conversation never ended");
            DialogueSystem.Current.AutoAdvance=false;yield return Settle();
        }
        [UnityTest] public IEnumerator SomeoneWithSomethingNewToSayShowsItOnTheirLabelAndPrompt()
        {
            State.questSteps["LanternRoad"]=3;
            yield return Zone("Lanternwick");yield return Settle();
            var pell=Person("Pell");var label=ShownLabel(pell);var hero=Hero;
            string name=label.text;Color color=label.color;float size=label.fontSize;
            Put(hero,pell.transform.position+new Vector3(0,.5f,-1.8f));yield return Settle();
            var prompt=Text("Interaction");StringAssert.Contains(DisplayNames.Speaker("Pell"),prompt.text,"fixture: the hero stands at Pell");
            StringAssert.DoesNotContain("something to say",prompt.text,"someone the party has never met is marked as having news: a first meeting isn't a beat");
            // Met: her offer is waiting, on a repeat talk nothing else points at.
            Flag("met.Pell");Assert.AreEqual("PellLampOffer",SideStories.NodeFor("Pell",State),"fixture: the offer waits");yield return Settle();
            Assert.AreNotEqual(name,label.text,"Pell has a quest to offer and her name label looks the same as anyone's");
            StringAssert.Contains(name,label.text,"the mark took her name's place");
            Assert.Greater(ColorGap(label.color,color),.2f,"the mark doesn't change the label's colour, so it can't be seen across a square");
            Assert.Greater(label.fontSize,size,"the mark is no bigger than the plain name, and the name is small at this camera");
            StringAssert.Contains("something to say",prompt.text,"up close the prompt still reads like any other talk");
            // Heard: back to plain.
            yield return Talk(pell);
            Assert.GreaterOrEqual(State.questSteps["PellsLamp"],0,"fixture: the offer was heard");
            Assert.AreEqual(name,label.text,"the mark stays after the conversation it pointed at");
            Assert.Less(ColorGap(label.color,color),.02f,"the label keeps its gold after the conversation");
            Assert.AreEqual(size,label.fontSize,.001f,"the label stays big after the conversation");
            StringAssert.DoesNotContain("something to say",prompt.text,"the prompt keeps offering what has just been said");
            // A beat that only repeats itself never clears, so it isn't news.
            Flag("pell.lampReturned");Assert.AreEqual("PellLampAfter",SideStories.NodeFor("Pell",State),"fixture: the line she repeats");yield return Settle();
            Assert.AreEqual(name,label.text,"a line Pell has already said keeps a mark on her until the chapter ends");
            // A scene holding the speaker (D197) has them say a holding line whatever waits.
            Flag("pell.lampReturned",false);State.questSteps.Remove("PellsLamp");pell.holdNode="PellRepeat";yield return Settle();
            Assert.AreEqual("PellLampOffer",SideStories.NodeFor("Pell",State),"fixture: the offer waits again");
            Assert.AreEqual(name,label.text,"a held speaker is marked, and talking plays only the holding line");
            pell.holdNode=null;yield return Settle();
            Assert.AreNotEqual(name,label.text,"fixture: the mark comes back with the beat");
        }
        [UnityTest] public IEnumerator SomeoneHailedFromAShipShowsItOnTheSignBesideThem()
        {
            Flag("met.Neve");
            yield return Zone("Hub_CinderHalo");yield return Settle();
            var neve=Person("Neve");Assert.AreEqual("NeveParcelOffer",SideStories.NodeFor("Neve",State),"fixture: Neve's offer waits");
            var label=ShownLabel(neve);
            Assert.AreNotEqual(DisplayNames.Speaker("Neve"),label.text,"Neve's parcel waits and the sign the player can see beside her is unchanged");
            var hail=neve.GetComponent<HailPoint>();Assert.IsNotNull(hail,"fixture: Neve is hailed");
            Put(Hero,neve.transform.position+Vector3.right);yield return Settle();
            Assert.AreSame(hail,PromptService.Interaction,"fixture: the ship is in hailing range");
            StringAssert.Contains("something to say",Text("Interaction").text,"a hail's prompt doesn't say someone has news");
        }

        // ------------------------------------------------------------------ copy33, worl80: what else is waiting in this zone
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text;
        static string AlsoLine()=>Objective().Split('\n').FirstOrDefault(l=>l.Contains("Also:"));
        static int Metres(string line)
        {
            var found=Regex.Match(Regex.Replace(line,"<[^>]+>",""),@"(\d+) m\b");
            Assert.IsTrue(found.Success,"the Also line gives no distance: "+line);return int.Parse(found.Groups[1].Value);
        }
        [UnityTest] public IEnumerator TheObjectivePanelAddsANearbySideQuestAsADimLineWithItsDistance()
        {
            State.questSteps["PellsLamp"]=0;State.questSteps["LanternRoad"]=3;
            yield return Zone("LanternGorge");yield return Settle();
            var line=AlsoLine();
            Assert.IsNotNull(line,"Pell's lamp is in this zone and the panel names only the relays: "+Objective());
            StringAssert.Contains("den",line,"the line doesn't say what to do");
            StringAssert.Contains("<color=#",line,"the side quest reads as loud as the main objective");
            StringAssert.Contains("relays",Objective().Split('\n')[0],"the side quest took the main objective's place");
            var den=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(d=>d.flag=="pell.lampFound");
            var hero=Hero;var gap=den.transform.position-hero.transform.position;gap.y=0;
            Assert.AreEqual(gap.magnitude,Metres(line),3,"the distance isn't the distance to the den");
            // Four lines at the type floor: inside the frame's art, on every side.
            var text=Text("Objective");var frame=Picture("ObjectiveFrame");Assert.AreEqual(24f,text.fontSize,.01f,"the panel left the type floor");
            Assert.AreEqual(frame.pixelsPerUnitMultiplier,Picture("VitalsFrame").pixelsPerUnitMultiplier,.001f,"the two top corners draw the same frame at different border weights on every screen");
            FitsItsBox(text,"objective panel");
            text.ForceMeshUpdate();var bounds=text.textBounds;var corners=new Vector3[4];frame.rectTransform.GetWorldCorners(corners);
            float scale=frame.rectTransform.lossyScale.x,weight=frame.pixelsPerUnitMultiplier;
            var low=text.transform.TransformPoint(bounds.min);var high=text.transform.TransformPoint(bounds.max);
            Assert.GreaterOrEqual((corners[2].y-high.y)/scale,30/weight,"the objective's first line crosses the frame's top edge");
            Assert.GreaterOrEqual((low.y-corners[0].y)/scale,42.5f/weight,"the Also line crosses the frame's bottom edge");
            Assert.GreaterOrEqual((low.x-corners[0].x)/scale,34/weight,"the panel's words cross the frame's left edge");
            Assert.GreaterOrEqual((corners[2].x-high.x)/scale,34/weight,"the panel's words cross the frame's right edge");
            Put(hero,den.transform.position+new Vector3(0,.5f,-6));yield return Settle();
            var nearer=den.transform.position-hero.transform.position;nearer.y=0;Assert.Less(nearer.magnitude,gap.magnitude-5,"fixture: the hero walked toward the den");
            Assert.AreEqual(nearer.magnitude,Metres(AlsoLine()??""),3,"the distance doesn't follow the hero");
            // A boss fight has the player's whole attention.
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*12);boss.Passive=false;hero.Health.InvulnerableUntil=GameTime.Now+10;
            yield return Settle();
            Assert.IsNull(AlsoLine(),"an errand stays on the panel in the middle of a boss fight");
            boss.Passive=true;Object.Destroy(boss.gameObject);yield return Settle();
            Assert.IsNotNull(AlsoLine(),"the line doesn't come back after the fight");
            // Step done: nothing here for it, and the relays are the main objective's own.
            State.questSteps["PellsLamp"]=1;yield return Settle();
            Assert.IsNull(AlsoLine(),"the line outlives the step, or repeats the main quest's relays as a side errand: "+Objective());
        }
        [UnityTest] public IEnumerator ASideQuestWaitingOnSomeoneOrOnOreShowsInTheirZoneAndTheNearestGoesFirst()
        {
            State.questSteps["PellsLamp"]=1;State.questSteps["IshensRecord"]=1;
            yield return Zone("Lanternwick");
            var hero=Hero;
            Put(hero,Person("Pell").transform.position+new Vector3(0,.5f,-3));yield return Settle();
            StringAssert.Contains("Pell",AlsoLine()??"","the lamp is ready to hand back and nothing points at Pell: "+Objective());
            Put(hero,Person("Ishen").transform.position+new Vector3(0,.5f,-3));yield return Settle();
            StringAssert.Contains("Ishen",AlsoLine()??"","with two errands waiting the line doesn't go to the nearer one: "+Objective());
            State.materials["RidgeCrystal"]=0;State.questSteps["MiraCrystals"]=0;
            yield return Zone("Sorrel_Ridges");yield return Settle();
            StringAssert.Contains("Ridge Crystals",AlsoLine()??"","Mira's crystals stand in this zone and the panel doesn't mention them: "+Objective());
            StringAssert.Contains("(0/6)",AlsoLine()??"","the line doesn't count them");
        }
        [UnityTest] public IEnumerator EverySideQuestStepFitsOneLineOfThePanelAtTheTypeFloor()
        {
            yield return null;
            float room=Text("Objective").rectTransform.rect.width;
            var probe=UiKit.Text(Object.FindFirstObjectByType<GameHud>().transform,"Also probe","",24,Color.white);
            var failures=new List<string>();int steps=0;
            foreach(var quest in GameCatalog.All<QuestDef>().Where(q=>q.id!="Sorrel"&&q.id!="Hushwell"&&q.id!="LanternRoad"))
                for(int step=0;step<quest.steps.Length;step++)
                {
                    steps++;State.questCounts[quest.id+"/"+step]=quest.steps[step].count;
                    string line=$"Also: {QuestText.Brief(quest,step,State)}  ·  999 m";float width=probe.GetPreferredValues(line).x;
                    if(width>room)failures.Add($"\"{line}\" runs {width:0} wide in a {room:0} line: under two lines of main objective it leaves the frame");
                }
            Object.Destroy(probe.gameObject);
            Assert.Greater(steps,10,"fixture: the side quests' steps");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ scri107: the chapter's end
        [UnityTest] public IEnumerator TheChapterEndsOnOneCardAndNeverOnALoad()
        {
            var toast=Toast;yield return Settle();
            Flag("chapter2.homecoming");
            float deadline=Time.unscaledTime+3;while(!toast.Heading.Contains("CHAPTER 2")&&Time.unscaledTime<deadline)yield return null;
            StringAssert.Contains("CHAPTER 2 COMPLETE",toast.Heading,"the last conversation of the chapter ends with nothing to say so");
            StringAssert.Contains("still being drawn",toast.Body,"nothing says the road's chart is out of the player's hands, so 'isn't charted yet' still reads as a task");
            yield return new WaitForSecondsRealtime(5.5f);
            Assert.IsTrue(toast.Showing,"three lines at the end of a chapter are gone before they can be read");
            deadline=Time.unscaledTime+6;while(toast.Showing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(toast.Showing,"fixture: the card leaves");
            IEnumerator NoCard(string complaint)
            {
                float until=Time.unscaledTime+1.2f;
                while(Time.unscaledTime<until)
                {
                    var now=Toast;
                    if(now!=null&&now.Showing)StringAssert.DoesNotContain("CHAPTER 2",now.Heading,complaint);
                    yield return null;
                }
            }
            yield return Zone("Arena_Ground");yield return NoCard("the chapter's card plays again on the next zone");
            var saves=new SaveSystem(Path.Combine(Application.temporaryCachePath,"QaUiFlow-"+System.Guid.NewGuid().ToString("N")));
            saves.Save("slot1",State);GameServices.Current.State=saves.Load("slot1");
            yield return Zone("Arena_Ground");yield return NoCard("loading a save from after the homecoming plays the chapter's card");
        }

        // ------------------------------------------------------------------ ui249: when a save was made
        static void Stamp(string path,string utc)
        {
            string json=Regex.Replace(File.ReadAllText(path),"\"savedAtUtc\": (\"[^\"]*\"|null)","\"savedAtUtc\": "+(utc==null?"null":"\""+utc+"\""));
            File.WriteAllText(path,json);
        }
        [UnityTest] public IEnumerator SaveRowsSayWhenTheSaveWasMadeOnThePlayersOwnClock()
        {
            var saves=GameServices.Current.Saves;
            // As long as a row gets: the longest zone name, a long campaign, a two-digit day and hour.
            var longest=new GameState{zone="SunkenRelay",playtime=410000};longest.party.Clear();longest.party.Add(new MemberState{id="Taren",level=40,integrity=100});
            const string utc="2026-11-15T20:41:00.0000000Z";
            foreach(string slot in new[]{"autosave","slot1","slot2"})saves.Save(slot,longest);
            Stamp(saves.PathForSlot("autosave"),utc);Stamp(saves.PathForSlot("slot1"),utc);Stamp(saves.PathForSlot("slot2"),null);
            var at=new System.DateTime(2026,11,15,20,41,0,System.DateTimeKind.Utc).ToLocalTime();
            string day=at.ToString("MMM d",CultureInfo.InvariantCulture),clock=at.ToString("t",CultureInfo.CurrentCulture);
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            var row=With("Save to slot 1");
            StringAssert.Contains(day,Words(row),"the save row doesn't say which day the slot was saved");StringAssert.Contains(clock,Words(row),"the save row gives no local time");
            RowFits(row,"pause save row");
            StringAssert.EndsWith("Sync 40",Words(With("Save to slot 2")),"a save too old to have a date leaves a dangling separator");
            menu.Close();yield return null;
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
            Row("Continue").onClick.Invoke();yield return null;yield return null;
            foreach(string slot in new[]{"autosave","slot1"})
            {
                var button=Row(slot);Assert.IsNotNull(button,"fixture: the picker lists "+slot);var words=Words(button);
                StringAssert.Contains(day,words,"Continue can't tell two saves apart by when they were made");StringAssert.Contains(clock,words,"the row gives no local time");
                var text=button.GetComponentInChildren<TMP_Text>();var box=text.rectTransform.rect;var need=text.GetPreferredValues(words,box.width,0);
                Assert.GreaterOrEqual(text.fontSize,24,"the row fell under the type floor to fit");
                Assert.LessOrEqual(need.x,box.width,$"\"{words}\" runs {need.x:0} wide in a {box.width:0} row");
                Assert.LessOrEqual(need.y,text.fontSize*1.3f,$"\"{words}\" takes two lines of its row");
            }
            StringAssert.EndsWith("min",Words(Row("slot2")),"a save too old to have a date leaves a dangling separator");
            if(Application.isBatchMode)foreach(string slot in new[]{"autosave","slot1","slot2"})foreach(string suffix in new[]{"",".bak",".tmp"})
            {var path=saves.PathForSlot(slot)+suffix;if(File.Exists(path))File.Delete(path);}
            saves.ResumeFrom("autosave");
        }

        // ------------------------------------------------------------------ ui249: hearing a slider
        static readonly AudioBus[] Buses={AudioBus.Master,AudioBus.Music,AudioBus.SFX,AudioBus.Ambience,AudioBus.UI};
        static AudioSource[] Playing(AudioBus bus)=>AudioManager.Current.GetComponentsInChildren<AudioSource>().Where(s=>!s.loop&&s.clip!=null&&s.clip.name!="ui_move"&&s.isPlaying&&s.outputAudioMixerGroup!=null&&s.outputAudioMixerGroup.name==bus.ToString()).ToArray();
        static void Nudge(Selectable target,MoveDirection direction)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
            ExecuteEvents.Execute(target.gameObject,new AxisEventData(EventSystem.current){moveDir=direction},ExecuteEvents.moveHandler);
        }
        IEnumerator EachSliderSpeaks(Slider[] sliders,string page)
        {
            Assert.AreEqual(5,sliders.Length,"fixture: "+page+" has its five sliders");
            foreach(var bus in Buses)
            {
                yield return new WaitForSecondsRealtime(.1f);
                int before=Playing(bus).Length;
                Nudge(sliders.Single(s=>s.name=="Sound_"+bus),MoveDirection.Left);
                yield return null;
                var voices=Playing(bus);
                Assert.Greater(voices.Length,before,$"moving {page}'s {bus} slider plays nothing on that bus, so the player has to leave the menu to judge it");
                Assert.Less(voices.Max(v=>v.clip.length),1.5f,$"{page}'s {bus} slider plays something too long to call a cue");
            }
        }
        [UnityTest] public IEnumerator AMovedSliderPlaysACueOnItsOwnBusAndOpeningThePagePlaysNone()
        {
            Assert.IsNotNull(AudioMix.Current.SettingsFile,"audio UI tests require the runner's isolated settings path");
            foreach(var bus in Buses)AudioMix.Current.SetLevel(bus,.6f,false);
            var heard=new List<string>();void Cue(string key,Transform emitter){if(key!="ui_move")heard.Add(key);}
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
            AudioManager.CueStarted+=Cue;
            try
            {
                var title=Object.FindFirstObjectByType<TitleScreen>();title.OpenSettings();yield return null;yield return null;
                Assert.IsEmpty(heard,"opening the title's settings plays a slider's cue with nothing moved");
                yield return EachSliderSpeaks(title.GetComponentsInChildren<Slider>(),"the title's");
                title.CloseSettings();yield return null;
                yield return Zone("Arena_Ground");
                heard.Clear();
                var menu=Menu;menu.Open(PauseMenu.SettingsPage);yield return null;yield return null;
                Assert.IsTrue(GameTime.Paused,"fixture: the pause menu pauses");
                Assert.IsEmpty(heard,"opening the pause menu's settings plays a slider's cue with nothing moved");
                // Paused is when a world sound is held back, and the pause menu is where most players are on this page.
                yield return EachSliderSpeaks(menu.GetComponentsInChildren<Slider>(),"the pause menu's");
                // A held direction or a dragged handle: not a machine gun.
                var sfx=menu.GetComponentsInChildren<Slider>().Single(s=>s.name=="Sound_SFX");
                yield return new WaitForSecondsRealtime(.4f);heard.Clear();
                for(int i=0;i<3;i++)Nudge(sfx,MoveDirection.Left);
                Assert.AreEqual(1,heard.Count,"a burst of slider moves plays a cue for every step: "+string.Join(" | ",heard));
                yield return new WaitForSecondsRealtime(.4f);
                Nudge(sfx,MoveDirection.Right);
                Assert.AreEqual(2,heard.Count,"after a moment the slider goes quiet for good: "+string.Join(" | ",heard));
                menu.Close();yield return null;
            }
            finally
            {
                AudioManager.CueStarted-=Cue;
                foreach(var bus in Buses)AudioMix.Current.SetLevel(bus,1);
                AudioMix.Current.Flush();
            }
        }

        // ------------------------------------------------------------------ Nathan's play: the top right, and the warp signs
        [UnityTest] public IEnumerator TheZoneNameStandsClearOfTheObjectiveFrame()
        {
            yield return null;yield return null;
            var zone=Text("Zone");var frame=Picture("ObjectiveFrame");Assert.IsNotEmpty(zone.text,"fixture: the zone has a name");
            zone.ForceMeshUpdate();var bounds=zone.textBounds;Assert.Greater(bounds.size.x,0,"fixture: the name has glyphs");
            var corners=new Vector3[4];frame.rectTransform.GetWorldCorners(corners);float scale=frame.rectTransform.lossyScale.y;
            float gap=(zone.transform.TransformPoint(bounds.min).y-corners[2].y)/scale;
            Assert.GreaterOrEqual(gap,16,"the zone name sits on the objective frame's top edge, reading as part of the panel");
            var screen=((RectTransform)frame.canvas.transform);var top=new Vector3[4];screen.GetWorldCorners(top);
            Assert.GreaterOrEqual((top[2].y-zone.transform.TransformPoint(bounds.max).y)/scale,16,"the zone name is jammed against the top of the screen");
        }
        [UnityTest] public IEnumerator WarpSignsReadFromTheGamesCameraAtTheTypeFloor()
        {
            var failures=new List<string>();int read=0;
            foreach(string zone in new[]{"Hub_CinderHalo","Gullet_Tunnel","TallowApproach","WickReach","Lanternwick","LanternGorge","SunkenRelay"})
            {
                yield return Zone(zone);
                var view=ZoneController.Current.definition.cameraProfile;float seen=2*view.distance*Mathf.Tan(view.fov*.5f*Mathf.Deg2Rad);
                // A warp's prompt can say more than its sign does, and a sign the story hasn't reached yet waits switched off.
                var warps=Object.FindObjectsByType<WarpBeacon>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                foreach(var sign in Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(s=>s.text.Length>0&&(s.text.StartsWith("Warp")||warps.Any(w=>w.prompt.StartsWith(s.text)))))
                {
                    read++;float em=sign.fontSize*.1f*sign.transform.lossyScale.y,pixels=em/seen*1080;
                    if(sign.gameObject.activeInHierarchy)Assert.AreEqual(em*1.12f,sign.GetPreferredValues(sign.text).y,em*.2f,"fixture: a TextMeshPro point is a tenth of a metre");
                    if(pixels<24)failures.Add($"{zone}: the sign \"{sign.text}\" stands {pixels:0} px tall on a 1080p screen, under the type floor of 24");
                }
            }
            Assert.Greater(read,8,"fixture: the warps carry signs");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
