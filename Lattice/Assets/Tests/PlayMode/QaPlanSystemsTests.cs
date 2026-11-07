using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>TEST_PLAN 4.8 to 4.13, checked from what a player would see: the menus by pad and mouse, the HUD's words, which track
    /// plays where, what skill spam leaves behind, saves old and new, and the ways a tester tries to break things. These checks are
    /// exploratory: they say what the plan says should be true, and one that fails is a finding, not a broken test.</summary>
    public sealed class QaPlanSystemsTests
    {
        Gamepad pad;Keyboard keyboard;string savedBindings,heard;bool touchedLevels;int mashed;
        readonly List<string> logged=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Slots={"autosave","slot1","slot2","slot3"};
        static readonly string[] Tabs={"Party","Skills","Gear","Fabricate","Items","Quests","Settings"};
        static readonly AudioBus[] Buses={AudioBus.Master,AudioBus.Music,AudioBus.SFX,AudioBus.Ambience,AudioBus.UI};
        const string BossTrack="Alien Boss Battle";

        void Listen(string node,string speaker,System.Action<bool> done)=>heard=node;
        void Logged(string text,string stack,LogType type){if(text.StartsWith("MUSIC_CUE")||text.StartsWith("NEW_GAME_OK")||text.StartsWith("ZONE_ENTER"))logged.Add(text);}

        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual devices into play.
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();PromptService.ForceDevice(null);SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            savedBindings=PlayerPrefs.GetString("keyboardBindings","");GameInput.Current.Asset.RemoveAllBindingOverrides();
            ClearSaves();DevLoadout.Apply("starter");
            heard=null;logged.Clear();touchedLevels=false;mashed=0;
            Npc.TalkRequested+=Listen;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Listen;Application.logMessageReceived-=Logged;
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Menu;if(menu!=null&&menu.IsOpen)menu.Close();
            var shop=ShopPanel;if(shop!=null&&shop.IsOpen)shop.Close();
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            pad=null;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            keyboard=null;
            if(GameInput.Current!=null)GameInput.Current.Asset.RemoveAllBindingOverrides();
            if(string.IsNullOrEmpty(savedBindings))PlayerPrefs.DeleteKey("keyboardBindings");else PlayerPrefs.SetString("keyboardBindings",savedBindings);
            // Back to the default device, which also drops any tag cached off another device's words.
            PromptService.ForceDevice(PromptDevice.Keyboard);PromptService.ForceDevice(null);InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(touchedLevels&&AudioMix.Current!=null){foreach(var bus in Buses)AudioMix.Current.SetLevel(bus,1);AudioMix.Current.Flush();}
            if(GameServices.Current!=null){Saves.ResumeFrom("autosave");ClearSaves();}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static void ClearSaves()
        {
            // The batch runner's save shield holds the player's real saves elsewhere for the whole run.
            if(!Application.isBatchMode)return;
            foreach(var slot in Slots)foreach(var suffix in new[]{"",".bak",".tmp"})
            {var path=Saves.PathForSlot(slot)+suffix;if(File.Exists(path))File.Delete(path);}
            Saves.ResumeFrom("autosave");
        }

        // ------------------------------------------------------------------ the world
        static GameState State=>GameServices.Current.State;
        static SaveSystem Saves=>GameServices.Current.Saves;
        static PauseMenu Menu=>Object.FindFirstObjectByType<PauseMenu>();
        static ShopUi ShopPanel=>Object.FindFirstObjectByType<ShopUi>();
        static QuestToast Toast=>Object.FindFirstObjectByType<QuestToast>();
        static CombatActor Hero=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=PartyController.Current.Active);
        static CombatActor Body(string id)=>PartyController.Current.members.First(m=>m.character==id);
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        static TechPartDef Part(string id)=>GameCatalog.Find<TechPartDef>(id);
        static SkillDef Skill(string hero,SkillKind kind)=>SkillBook.All(hero).First(s=>s.kind==kind);
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static bool Live<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length>0;
        static EncounterVolume Encounter(string id)=>All<EncounterVolume>().Single(e=>e.encounterId==id);
        static DiscoveryPoint Discovery(string flag)=>All<DiscoveryPoint>().Single(d=>d.flag==flag);
        static WarpBeacon WarpTo(string scene)=>All<WarpBeacon>().Single(w=>w.scene==scene);
        static Npc Person(string speaker)=>All<Npc>().Single(n=>n.speaker==speaker);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Settle(){yield return new WaitForSecondsRealtime(.4f);}
        static IEnumerator Until(System.Func<bool> done,float seconds){float end=Time.unscaledTime+seconds;while(!done()&&Time.unscaledTime<end)yield return null;}
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            yield return Quieted();
        }
        static IEnumerator Quieted()
        {
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            float deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            yield return new WaitForSecondsRealtime(.35f);
        }
        /// <summary>Open floor far from a proving ground's walls.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Plan fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            hero.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static void FaceForward(CombatActor hero)
        {
            hero.transform.rotation=Quaternion.LookRotation(Vector3.forward);
            if(hero.motor is GroundMotor ground)ground.KeepFacing(Vector3.forward);else if(hero.motor is FlightMotor ship)ship.KeepFacing(Vector3.forward);
        }
        /// <summary>The motor only carries out a dash while it is being driven, as a brain does every frame.</summary>
        static IEnumerator Drive(CombatActor actor,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(Vector2.zero,false,false);yield return null;}}
        static IEnumerator Walk(CombatActor actor,Vector3 direction,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(new Vector2(direction.x,direction.z),false,false);yield return null;}}
        static EnemyBrain Spawn(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        /// <summary>A body to hit: no brain, and more integrity than any fixture can take off it.</summary>
        static EnemyBrain Dummy(string id,Vector3 at)
        {var enemy=Spawn(id,at);enemy.enabled=false;enemy.Health.maximum=enemy.Health.integrity=1000000;enemy.Health.breakThreshold=1e9f;return enemy;}
        static void Remove<T>()where T:Component{foreach(var left in Object.FindObjectsByType<T>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);}
        static void Defeat(EnemyBrain boss)
        {
            var packet=new DamagePacket{source=Hero.Health,amount=boss.Health.maximum*50,type=DamageType.Pulse};
            if(boss.TryGetComponent<CantorCollar>(out var collar))foreach(var link in collar.Links)link.GetComponent<Hurtbox>().Hit(packet);
            boss.Health.Receive(packet);
        }
        static IEnumerator FinishTalk(float seconds=25)
        {
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;
            yield return null;
            float deadline=Time.unscaledTime+seconds;
            while((dialogue.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)
            {
                var shop=ShopPanel;if(shop!=null&&shop.IsOpen)break;var menu=Menu;if(menu!=null&&menu.IsOpen)break;
                yield return null;
            }
            dialogue.AutoAdvance=false;yield return new WaitForSecondsRealtime(.2f);
        }
        IEnumerator Talk(Npc npc)
        {
            heard=null;DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;
            Assert.IsNotNull(heard,npc.speaker+" did not speak");
            yield return FinishTalk();
        }

        // ------------------------------------------------------------------ what's on screen
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static TMP_Text Hud(string name)
        {
            var canvas=All<Canvas>().First(c=>c.name=="HUD");var text=canvas.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name==name);
            Assert.IsNotNull(text,"the HUD has no "+name);return text;
        }
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static string Words(Component c){var t=c.GetComponentInChildren<TMP_Text>();return t!=null?t.text:"";}
        static Button With(string words)
        {
            var found=Buttons().FirstOrDefault(b=>Words(b).Contains(words));
            Assert.IsNotNull(found,$"no row says \"{words}\". On screen: {string.Join(" | ",Buttons().Select(b=>Words(b)))}");return found;
        }
        static GameObject Selected()
        {var s=EventSystem.current.currentSelectedGameObject;Assert.IsNotNull(s,"nothing has focus: a pad player is stuck");return s;}
        static string Heading(){var t=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(n=>n.name=="Heading");Assert.IsNotNull(t,"the page has no heading");return t.text;}
        static string Note(){var t=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(n=>n.name=="Note");Assert.IsNotNull(t,"the page has no note");return t.text;}
        static string Feedback(){var t=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(n=>n.name=="Feedback");return t!=null?t.text:"";}
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text;
        static IEnumerator ToTitle()
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone("Title");deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"never reached the title");
            yield return null;yield return null;
        }
        static IEnumerator OpenPicker()
        {
            var row=Row("Continue");Assert.IsNotNull(row,"the title has no Continue");Assert.IsTrue(row.interactable,"Continue is greyed out with a save on disk");
            row.onClick.Invoke();yield return null;yield return null;
        }
        static IEnumerator Pick(string slot,string zone)
        {
            var row=Row(slot);Assert.IsNotNull(row,"the picker doesn't list "+slot);Assert.IsTrue(row.interactable,slot+" can't be chosen: "+Words(row));
            row.onClick.Invoke();float deadline=Time.unscaledTime+25;
            while((SceneFlow.Current.Loading||SceneFlow.Current.Zone!=zone)&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(zone,SceneFlow.Current.Zone,"Continue landed in the wrong zone");Assert.IsFalse(SceneFlow.Current.Loading);
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.35f);
        }

        // ------------------------------------------------------------------ pad and keys
        void AddPad(){if(pad==null||!pad.added)pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();}
        IEnumerator Hold(GamepadState state){InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;}
        IEnumerator Tap(GamepadButton button){yield return Hold(new GamepadState().WithButton(button));yield return Hold(new GamepadState());yield return null;}
        /// <summary>A thumb hammering a button, about twelve presses a second: call it once a frame.</summary>
        void MashStep(GamepadButton button=GamepadButton.South)
        {
            int beat=mashed++%5;
            if(beat==0)InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));else if(beat==2)InputSystem.QueueStateEvent(pad,new GamepadState());
        }
        void MashStop(){InputSystem.QueueStateEvent(pad,new GamepadState());mashed=0;}
        IEnumerator KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.25f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        IEnumerator KeyTap(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        sealed class FixturePrompt:InteractionPrompt{public int Uses;public override void Interact(){Uses++;}}
        /// <summary>A spot where a hero can stand and get this prompt.</summary>
        static Vector3 Beside(InteractionPrompt prompt,CombatActor hero)
        {
            var at=prompt.transform.position;
            if(hero.flight)return new Vector3(at.x,hero.transform.position.y,at.z-1.5f);
            return NavMesh.SamplePosition(at,out var near,prompt.range+3,NavMesh.AllAreas)?near.position+Vector3.up*.1f:at;
        }

        // ------------------------------------------------------------------ 4.8: the tabs
        static Selectable[] MenuControls()=>Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None).Where(s=>{var c=s.GetComponentInParent<Canvas>();return c!=null&&c.rootCanvas.name=="Menus";}).ToArray();
        [UnityTest] public IEnumerator EveryTabWalksByPadWithFocusNeverLost()
        {
            AddPad();State.questSteps["PellsLamp"]=0;State.questSteps["IshensRecord"]=0;
            yield return Zone("Arena_Ground");
            RpgServices.Inventory.GivePart(Part("EdgesT1"));RpgServices.Inventory.GivePart(Part("FramesT1"));
            var menu=Menu;menu.Open();yield return null;
            for(int page=0;page<Tabs.Length;page++)
            {
                StringAssert.Contains(Tabs[page].ToUpperInvariant(),Heading(),"the shoulder button went to the wrong tab");
                Assert.IsFalse(Selected().name.StartsWith("Tab_"),Tabs[page]+" opens with focus on the tab column, so the next A does nothing the player can see");
                var controls=MenuControls().Where(s=>!s.name.StartsWith("Tab_")).Select(s=>s.name).ToArray();
                var seen=new HashSet<string>{Selected().name};
                for(int i=0;i<controls.Length;i++){yield return Tap(GamepadButton.DpadDown);seen.Add(Selected().name);}
                var missing=controls.Where(n=>!seen.Contains(n)).ToArray();
                Assert.IsEmpty(missing,$"{Tabs[page]}: walking down the page with the d-pad never reaches {string.Join(", ",missing)}: a pad player can't get to them");
                EventSystem.current.SetSelectedGameObject(Row("Close").gameObject);yield return null;
                yield return Tap(GamepadButton.DpadLeft);
                Assert.AreEqual("Tab_"+Tabs[page],Selected().name,$"{Tabs[page]}: left from the page doesn't land on its own tab");
                yield return Tap(GamepadButton.DpadRight);
                Assert.IsFalse(Selected().name.StartsWith("Tab_"),$"{Tabs[page]}: right from the tab column doesn't come back to the page");
                yield return Tap(GamepadButton.RightShoulder);
            }
            StringAssert.Contains("PARTY",Heading(),"RB past the last tab doesn't wrap to the first");
            for(int page=Tabs.Length-1;page>=0;page--)
            {
                yield return Tap(GamepadButton.LeftShoulder);
                StringAssert.Contains(Tabs[page].ToUpperInvariant(),Heading(),"LB doesn't walk the tabs back");
                Selected();
            }
            menu.Close();
        }
        static Vector2 Centre(RectTransform rect){var c=new Vector3[4];rect.GetWorldCorners(c);return (c[0]+c[2])*.5f;}
        /// <summary>What a pointer here lands on, worked out as the raycaster does: the highest live canvas, then what is drawn last. Done by hand
        /// because a headless run has no rendered depth for the real raycaster to read.</summary>
        static Graphic Under(Vector2 point)
        {
            Graphic best=null;int bestOrder=int.MinValue,bestIndex=-1;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.enabled&&c.GetComponent<GraphicRaycaster>()!=null))
            {
                var graphics=canvas.GetComponentsInChildren<Graphic>();
                for(int i=0;i<graphics.Length;i++)
                {
                    var g=graphics[i];if(!g.raycastTarget||!g.isActiveAndEnabled||!RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform,point,null))continue;
                    if(canvas.sortingOrder>bestOrder||canvas.sortingOrder==bestOrder&&i>bestIndex){best=g;bestOrder=canvas.sortingOrder;bestIndex=i;}
                }
            }
            return best;
        }
        static void Click(Selectable control)=>ExecuteEvents.Execute(control.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        [UnityTest] public IEnumerator EveryRowOnEveryTabAnswersAMouseWhereItIsDrawn()
        {
            State.questSteps["PellsLamp"]=0;
            yield return Zone("Arena_Ground");
            RpgServices.Inventory.GivePart(Part("EdgesT1"));
            PromptService.ForceDevice(PromptDevice.Keyboard);yield return null;
            var menu=Menu;menu.Open();yield return null;
            var covered=new List<string>();
            foreach(string tab in Tabs)
            {
                Click(Row("Tab_"+tab));yield return null;
                StringAssert.Contains(tab.ToUpperInvariant(),Heading(),"clicking the "+tab+" tab doesn't open it");
                Selected();
                foreach(var control in MenuControls())
                {
                    var over=Under(Centre((RectTransform)control.transform));
                    if(over==null||!over.transform.IsChildOf(control.transform))covered.Add($"{tab}: a click on {control.name} lands on {(over!=null?over.name:"nothing")}");
                }
                // A moving mouse takes the focus to the row it touches, and a click on that row is the row's press.
                var row=MenuControls().OfType<Button>().First(s=>!s.name.StartsWith("Tab_")&&s.name!="Close");
                ExecuteEvents.Execute(row.gameObject,new PointerEventData(EventSystem.current){delta=new Vector2(6,0)},ExecuteEvents.pointerEnterHandler);
                Assert.AreSame(row.gameObject,Selected(),$"{tab}: a moving mouse doesn't select the row under it");
            }
            Assert.IsEmpty(covered,string.Join("\n",covered));
            Click(Row("Close"));yield return null;
            Assert.IsFalse(menu.IsOpen,"clicking Back at the top of a page doesn't close the menu");
        }

        // ------------------------------------------------------------------ 4.8: the Skills page
        [UnityTest] public IEnumerator TheSkillsPageNamesEachSlotsButtonCostAndMasteryAndEachLockedSkillsLevel()
        {
            yield return Zone("Arena_Ground");
            var taren=Member("Taren");taren.level=5;taren.skillUses=null;
            var all=SkillBook.All("Taren");var next=all.Where(s=>s.unlockLevel>5).OrderBy(s=>s.unlockLevel).First();
            foreach(var device in new[]{PromptDevice.Keyboard,PromptDevice.Gamepad})
            {
                PromptService.ForceDevice(device);yield return null;
                var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;
                for(int slot=0;slot<SkillBook.Slots;slot++)
                {
                    var skill=SkillBook.Equipped("Taren",slot);var words=Words(Row("Row_"+(slot+1)));
                    StringAssert.Contains(GameHud.SkillBinding(slot,false),words,$"on {device} slot {slot+1} doesn't say which button fires it");
                    StringAssert.Contains(skill.displayName,words,$"slot {slot+1} doesn't name its skill");
                    StringAssert.Contains($"{SkillBook.Cost("Taren",skill):0} Charge",words,$"slot {slot+1} doesn't say what it costs");
                    StringAssert.Contains("Mastery 1",words,$"slot {slot+1} doesn't say how mastered it is");
                }
                StringAssert.Contains(next.displayName,Note(),"the page doesn't say which skill comes next");
                StringAssert.Contains($"Sync {next.unlockLevel}",Note(),"the page doesn't say when the next skill comes");
                Row("Row_1").onClick.Invoke();yield return null;
                foreach(var skill in all)
                {
                    var words=Words(With(skill.displayName));
                    if(skill.unlockLevel<=taren.level)
                    {
                        StringAssert.DoesNotContain("Locked",words,skill.displayName+" is learned and reads as locked");
                        StringAssert.Contains($"{SkillBook.Cost("Taren",skill):0} Charge",words,skill.displayName+"'s row doesn't say what it costs");
                        StringAssert.Contains("Mastery",words,skill.displayName+"'s row doesn't show its mastery");
                        StringAssert.Contains($"{skill.cooldown:0} s",words,skill.displayName+"'s row doesn't show its cooldown");
                    }
                    else
                    {
                        StringAssert.Contains("Locked",words,skill.displayName+" isn't learned at Sync 5 and doesn't say so");
                        StringAssert.Contains($"Sync {skill.unlockLevel}",words,skill.displayName+" is locked and doesn't say at what level it comes");
                    }
                }
                EventSystem.current.SetSelectedGameObject(With(next.displayName).gameObject);yield return null;
                StringAssert.Contains(next.description,Note(),"focus on a locked skill doesn't say what it will do");
                menu.Close();yield return null;
            }
        }
        [UnityTest] public IEnumerator PressingALockedSkillAnswers()
        {
            AddPad();yield return Zone("Arena_Ground");
            var taren=Member("Taren");taren.level=1;var locked=SkillBook.All("Taren").First(s=>s.unlockLevel>1);var worn=SkillBook.Equipped("Taren",0);
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;Row("Row_1").onClick.Invoke();yield return null;
            EventSystem.current.SetSelectedGameObject(With(locked.displayName).gameObject);yield return null;
            using var log=new CueLog();
            yield return Tap(GamepadButton.South);
            Assert.AreEqual(worn,SkillBook.Equipped("Taren",0),"a locked skill went on the slot");
            Assert.IsTrue(log.Count("ui_error")>0||Feedback().Length>0,"A on a locked skill does nothing and says nothing, where every other refusal in these menus has a sound and a line: "+log.All);
            menu.Close();
        }

        // ------------------------------------------------------------------ 4.8: the HUD
        static int Out(string label,string word)
        {
            var found=Regex.Match(label,word+@"\s+(\d+)");Assert.IsTrue(found.Success,$"the {word} line doesn't give its number: \"{label}\"");return int.Parse(found.Groups[1].Value);
        }
        [UnityTest] public IEnumerator TheHudShowsVitalsPartnerAndTargetInWords()
        {
            yield return Zone("Arena_Ground");Arena();
            var party=PartyController.Current;var hero=party.Active;var partner=party.members[1-party.index];
            Put(hero,new Vector3(200,0,0));Put(partner,new Vector3(212,0,0));
            hero.Health.integrity=hero.Health.maximum*.5f;hero.charge=40;hero.thrust=50;yield return Frames(2);
            string who=DisplayNames.Speaker(hero.character).ToUpperInvariant();
            StringAssert.Contains(who,Hud("Status").text,"the HUD doesn't name the hero being played");
            StringAssert.Contains($"{hero.Health.integrity:0} / {hero.Health.maximum:0}",Hud("IntegrityLabel").text,"the Integrity line doesn't say what's left");
            Assert.AreEqual(40,Out(Hud("ChargeLabel").text,"CHARGE"),1,"the Charge line doesn't follow the hero's Charge");
            Assert.AreEqual(50,Out(Hud("ThrustLabel").text,"THRUST"),3,"the Thrust line doesn't follow the hero's Thrust");
            foreach(var (bar,share) in new[]{("Integrity",.5f),("Charge",.4f),("Thrust",.5f)})
            {
                var fill=Picture(bar);var back=Picture(bar+"Back");Assert.IsNotNull(fill,"no "+bar+" bar");Assert.IsNotNull(back,"no "+bar+" bar");
                Assert.AreEqual(share,fill.rectTransform.rect.width/back.rectTransform.rect.width,.04f,$"the {bar} bar doesn't show what the hero has");
            }
            string mate=DisplayNames.Speaker(partner.character).ToUpperInvariant();
            StringAssert.Contains(mate,Hud("Partner").text,"the partner line doesn't name the partner");
            StringAssert.Contains($"{partner.Health.integrity:0}/{partner.Health.maximum:0}",Hud("Partner").text,"the partner line doesn't say how they are");
            partner.Health.Receive(new DamagePacket{amount=partner.Health.maximum*10,type=DamageType.Pulse});yield return Frames(2);
            StringAssert.Contains("APPROACH TO REVIVE",Hud("Partner").text,"a downed partner's line doesn't say what to do");
            Put(partner,hero.transform.position+Vector3.right*1.5f);
            yield return Until(()=>Hud("Partner").text.Contains("RESTORING"),1);
            StringAssert.Contains("RESTORING",Hud("Partner").text,"standing over a downed partner shows no progress");
            yield return Until(()=>Hud("Partner").text.Contains("RECOVERING"),3);
            StringAssert.Contains("RECOVERING",Hud("Partner").text,"a partner getting up isn't shown as getting up");
            var hound=Dummy("Ridgehound",hero.transform.position+Vector3.forward*6);hound.Health.breakThreshold=100;yield return null;
            hero.target=hound.Health;yield return Frames(2);
            StringAssert.Contains("RIDGEHOUND",Hud("Target").text,"the target line doesn't name the target");
            StringAssert.Contains($"{hound.Health.integrity:0}/{hound.Health.maximum:0}",Hud("Target").text,"the target line doesn't say how it is");
            Assert.AreEqual(0,Out(Hud("Target").text,"BREAK"),"the target line has no break gauge");
            hound.Health.Receive(new DamagePacket{source=hero.Health,amount=1,type=DamageType.Pulse,breakPower=10});yield return Frames(2);
            Assert.Greater(Out(Hud("Target").text,"BREAK"),0,"the break gauge doesn't move when the target is hit");
            StringAssert.Contains(DisplayNames.Zone("Arena_Ground").ToUpperInvariant(),Hud("Zone").text,"the HUD doesn't name the zone");
            yield return Zone("Hub_Decks");
            StringAssert.Contains(DisplayNames.Zone("Hub_Decks").ToUpperInvariant(),Hud("Zone").text,"the zone name doesn't follow the party");
        }
        [UnityTest] public IEnumerator TheHudShowsEachSkillsIconCostAndCooldownInAFight()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,0,0));Put(Partner,new Vector3(212,0,0));
            hero.charge=100;for(int i=0;i<4;i++)hero.cooldowns[i]=0;yield return Frames(2);
            for(int i=0;i<4;i++)
            {
                var skill=SkillBook.Equipped(hero.character,i);var icon=Picture("Skill"+i);var label=Hud("SkillLabel"+i);
                Assert.IsNotNull(icon,"no icon for slot "+i);Assert.IsTrue(icon.enabled&&icon.sprite!=null,$"slot {i} shows no icon in a fight");Assert.IsTrue(label.enabled,$"slot {i} shows no label in a fight");
                StringAssert.Contains($"{SkillBook.Cost(hero.character,skill):0}C",label.text,$"slot {i} doesn't say what its skill costs");
                StringAssert.Contains(string.IsNullOrEmpty(skill.shortName)?skill.displayName:skill.shortName,label.text,$"slot {i} doesn't name its skill");
                Assert.AreEqual(Color.white,icon.color,$"slot {i} looks unavailable with full Charge and no cooldown");
            }
            hero.cooldowns[0]=3.4f;yield return Frames(2);
            Assert.IsTrue(Regex.IsMatch(Hud("SkillLabel0").text,@"\d\.\ds"),"a skill on cooldown doesn't count down: "+Hud("SkillLabel0").text);
            Assert.AreNotEqual(Color.white,Picture("Skill0").color,"a skill on cooldown looks ready");
            hero.charge=0;yield return Frames(2);
            Assert.AreNotEqual(Color.white,Picture("Skill1").color,"a skill the hero can't pay for looks ready");
            string first=Hud("SkillLabel1").text;
            Assert.IsTrue(PartyController.Current.Swap(),"fixture: someone to swap with");yield return Frames(2);
            Assert.AreNotEqual(first,Hud("SkillLabel1").text,"after a swap the strip still shows the other hero's skills");
            yield return Zone("Hub_Decks");
            Assert.IsFalse(Picture("Skill0").enabled,"skill icons stay up in a town, where nothing can be fired");
        }
        [UnityTest] public IEnumerator TheBossBarHidesThePromptAndGivesItBack()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,0,0));Put(Partner,new Vector3(212,0,0));hero.Health.InvulnerableUntil=GameTime.Now+30;
            var fixture=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();fixture.prompt="Fixture prompt";fixture.range=6;fixture.transform.position=hero.transform.position;
            yield return Frames(3);
            StringAssert.Contains("Fixture prompt",Hud("Interaction").text,"fixture: the prompt shows");
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*14);boss.Passive=false;
            float deadline=Time.unscaledTime+1;while(string.IsNullOrEmpty(Hud("BossName").text)&&Time.unscaledTime<deadline)yield return null;
            yield return Frames(2);
            Assert.AreEqual("HESSA KORL",Hud("BossName").text,"no boss bar");
            Assert.IsTrue(string.IsNullOrEmpty(Hud("Interaction").text),"the prompt sits on top of the boss bar: "+Hud("Interaction").text);
            StringAssert.Contains("HESSA KORL",Hud("BossTitleCard").text,"the fight opens with no title card");
            boss.Passive=true;Object.Destroy(boss.gameObject);yield return Frames(3);
            StringAssert.Contains("Fixture prompt",Hud("Interaction").text,"the prompt never comes back after the fight");
        }

        // ------------------------------------------------------------------ 4.8: shops and item names
        [UnityTest] public IEnumerator AShopPricesEveryOfferRefusesWithAReasonAndConfirmsASale()
        {
            yield return Zone("Lanternwick");
            var shop=ShopPanel;var corra=GameCatalog.Find<ShopInventory>("Lanternwick");
            foreach(var stock in new[]{corra,null})
            {
                string whose=stock!=null?"Corra's stock":"the Decks' stock";
                shop.Open(stock);yield return null;
                var offers=new List<string>();
                for(int page=0;page<5;page++)
                {
                    offers.AddRange(Buttons().Where(b=>b.name.StartsWith("ShopRow")&&b.name!="ShopRow0").Select(b=>Words(b)));
                    var pager=Row("Pager");if(pager==null)break;
                    pager.onClick.Invoke();yield return null;
                    if(Words(Row("Pager")).Contains("(1/"))break;
                }
                Assert.IsNotEmpty(offers,whose+" offers nothing");
                foreach(var offer in offers)
                {
                    Assert.IsTrue(Regex.IsMatch(offer,@"\d+ scrip"),$"{whose}: \"{offer}\" has no price");
                    Assert.IsFalse(Regex.IsMatch(offer,@"[a-z][A-Z]|_|T\d\b"),$"{whose}: \"{offer}\" shows a raw id");
                    Assert.IsTrue(char.IsLetter(offer[0]),$"{whose}: \"{offer}\" doesn't lead with what it sells");
                }
                shop.Close();yield return null;
            }
            // Too dear: nothing moves, and the player is told why.
            State.scrip=1;int parts=State.parts.Count;
            shop.Open(corra);yield return null;
            var first=Row("ShopRow1");string words=Words(first);
            string name=words.Split(new[]{"  ·  ","  —  "},System.StringSplitOptions.None)[0];int price=int.Parse(Regex.Match(words,@"(\d+) scrip").Groups[1].Value);
            using(var log=new CueLog())
            {
                first.onClick.Invoke();yield return null;
                StringAssert.Contains(name,Feedback(),"a refused purchase doesn't say what was refused");
                StringAssert.Contains(price.ToString(),Feedback(),"a refused purchase doesn't say what it costs");
                StringAssert.Contains("you have 1",Feedback(),"a refused purchase doesn't say what the player has");
                Assert.AreEqual(1,log.Count("ui_error"),"a refused purchase sounds accepted: "+log.All);
            }
            Assert.AreEqual(1,State.scrip,"a refused purchase spent scrip");Assert.AreEqual(parts,State.parts.Count,"a refused purchase handed over a part");
            // The price on the row is what is charged.
            State.scrip=1000;Row("ShopRow1").onClick.Invoke();yield return null;
            Assert.AreEqual(1000-price,State.scrip,"the row's price isn't what was charged");
            StringAssert.Contains(name,Feedback(),"a purchase doesn't say what was bought");
            // Selling back says what went and for how much.
            var spare=RpgServices.Inventory.GivePart(Part("EdgesT1"));
            Row("ShopRow0").onClick.Invoke();yield return null;
            var row=With(Part("EdgesT1").displayName);int value=int.Parse(Regex.Match(Words(row),@"(\d+) scrip").Groups[1].Value);int held=State.scrip;
            row.onClick.Invoke();yield return null;
            Assert.IsFalse(State.parts.Contains(spare),"fixture: the part sold");Assert.AreEqual(held+value,State.scrip,"the sale paid something other than the row's price");
            StringAssert.Contains(Part("EdgesT1").displayName,Feedback(),"a sale doesn't say what went");StringAssert.Contains($"{value} scrip",Feedback(),"a sale doesn't say what it paid");
            shop.Close();
        }
        [UnityTest] public IEnumerator EscAtAShopClosesOnlyTheShop()
        {
            keyboard=InputSystem.AddDevice<Keyboard>();PromptService.ForceDevice(PromptDevice.Keyboard);
            yield return Zone("Hub_Decks");
            var shop=ShopPanel;shop.Open(null);yield return new WaitForSecondsRealtime(.4f);
            yield return KeyTap(Key.Escape);
            Assert.IsFalse(shop.IsOpen,"Esc doesn't close the shop");
            Assert.IsFalse(Menu.IsOpen,"the Esc that closed the shop also opened the pause menu");
            Assert.IsFalse(GameTime.Paused,"the Esc that closed the shop left the game paused");Assert.IsFalse(GameInput.Current.Blocked,"the Esc that closed the shop left the player without control");
        }
        [UnityTest] public IEnumerator ItemNamesAreSpelledOneWayEverywhere()
        {
            yield return Zone("Arena_Ground");
            var names=new Dictionary<string,string>();
            foreach(var m in GameCatalog.All<MaterialDef>())names[m.id]=m.displayName;
            foreach(var c in GameCatalog.All<ConsumableDef>())names[c.id]=c.displayName;
            foreach(var p in GameCatalog.All<TechPartDef>())names[p.id]=p.displayName;
            Assert.AreEqual("Scrap Alloy",names["ScrapAlloy"]);Assert.AreEqual("Repair Gel",names["RepairGel"]);
            foreach(var (id,name) in names)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(name),id+" has no name");
                Assert.IsFalse(Regex.IsMatch(name,@"[a-z][A-Z]|_"),$"{id} shows as a raw id: {name}");
                Assert.IsTrue(name.Split(' ').All(w=>char.IsUpper(w[0])),$"{id} is named \"{name}\", where every other item's words are capitalised");
                Assert.AreEqual(name,DisplayNames.Item(id),$"{id} is \"{name}\" on one screen and \"{DisplayNames.Item(id)}\" on another");
            }
            foreach(var recipe in GameCatalog.All<RecipeDef>())
            {
                string makes=recipe.output!=null?recipe.output.displayName:DisplayNames.Item(recipe.consumableOutput);
                Assert.AreEqual(makes,recipe.displayName,"the bench calls it one thing and the Gear page another");
            }
            // The screens all say it the same way.
            var menu=Menu;menu.Open(PauseMenu.ItemsPage);yield return null;
            StringAssert.StartsWith("Repair Gel ×",Words(With("Repair Gel")));StringAssert.StartsWith("Scrap Alloy ×",Words(With("Scrap Alloy")));
            menu.Close();yield return null;
            menu.Open(PauseMenu.FabricatePage);yield return null;With("Scrap Alloy ");menu.Close();yield return null;
            RpgServices.Inventory.Grant("ScrapAlloy",2);yield return null;yield return null;
            Assert.IsTrue(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t=>t.name=="PickupLine"&&t.text.Contains("Scrap Alloy")),"the pickup feed spells it another way");
            // And so does everything people say. Sentence case is the only slip that gets past a screen.
            var writing=new List<(string where,string text)>();
            string root=Path.Combine(Application.dataPath,"_Project/Resources/Dialogue");
            foreach(var file in Directory.GetFiles(root,"*.yarn"))writing.Add((Path.GetFileName(file),File.ReadAllText(file)));
            writing.Add(("barks.json",Resources.Load<TextAsset>("Audio/barks").text));
            foreach(var quest in GameCatalog.All<QuestDef>())writing.Add((quest.id,quest.title+"\n"+quest.description+"\n"+string.Join("\n",quest.stepText??new string[0])));
            var slips=new List<string>();
            foreach(var name in names.Values)foreach(var (where,text) in writing)foreach(Match found in Regex.Matches(text,Regex.Escape(name)+"s?",RegexOptions.IgnoreCase))
                if(!found.Value.StartsWith(name))slips.Add($"{where}: \"{found.Value}\" for {name}");
            Assert.IsEmpty(slips,"the script spells an item differently from every screen:\n"+string.Join("\n",slips));
        }

        // ------------------------------------------------------------------ 4.9: which track plays where
        static void Plays(string track,string where)
        {
            Assert.AreEqual(track,MusicDirector.Current.Track,where+" plays the wrong track");
            var source=MusicDirector.Current.ActiveSource;
            Assert.IsNotNull(source.clip,where+" has a cue and no clip: silence");Assert.AreEqual(track,source.clip.name,where+"'s voice holds the wrong clip");
            Assert.IsTrue(source.isPlaying,where+"'s track isn't playing");
        }
        [UnityTest] public IEnumerator EachZonePlaysThePlansTrack()
        {
            foreach(var (zone,track) in new[]
            {
                ("Hub_Decks","Hub Town Groove"),("Hub_CinderHalo","Hub Town Groove"),("TallowDrift","Hub Town Groove"),("Lanternwick","Old Village"),
                ("LanternGorge","Adventure Awaits"),("Hushwell","Moon Caverns"),("SunkenRelay","Moon Caverns"),("Gullet_Tunnel","Starfight"),("WickReach","Starfight"),
            })
            {yield return Zone(zone);Plays(track,zone);}
            // Past the outpost's shelter Sorrel is combat ground.
            yield return Zone("Sorrel_Ridges");Put(Hero,new Vector3(0,0,32));yield return new WaitForSecondsRealtime(.4f);
            Plays("Adventure Awaits","Sorrel's open ridges");
        }
        IEnumerator BossTrackIn(string zone,string encounter,string boss,string home,params string[] flags)
        {
            foreach(var f in flags)Flag(f);
            yield return Zone(zone);
            Plays(home,zone+" before the fight");
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+60;
            Encounter(encounter).Begin();yield return Frames(3);
            Plays(BossTrack,boss+"'s fight");
            DialogueSystem.Current.AutoAdvance=true;
            Defeat(All<EnemyBrain>().First(e=>e.definition.id==boss));yield return Frames(2);
            Assert.AreEqual(home,MusicDirector.Current.Track,boss+" is down and the boss track plays on");
            yield return new WaitForSecondsRealtime(.2f);
            var dialogue=DialogueSystem.Current;if(dialogue.Running){dialogue.Cancel();yield return Frames(2);}
        }
        [UnityTest] public IEnumerator EachBossFightPlaysTheBossTrackAndGivesTheZoneBack()
        {
            yield return BossTrackIn("Hushwell","Hushwell_Bellows","BellowsBelow","Moon Caverns");
            yield return BossTrackIn("Gullet_Tunnel","Gullet_Cantor","Cantor","Starfight");
            yield return BossTrackIn("SunkenRelay","Relay_Hessa","HessaKorl","Moon Caverns","relay.hessaConfronted");
            yield return Zone("Sorrel_Ridges");Put(Hero,new Vector3(0,0,32));yield return new WaitForSecondsRealtime(.4f);
            Plays("Adventure Awaits","Sorrel before the Burrower");
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+60;
            Put(Hero,new Vector3(0,.2f,164));Put(Partner,new Vector3(2,.2f,164));
            Encounter("Sorrel_Burrower").Begin();yield return Frames(3);
            Plays(BossTrack,"the Burrower's fight");
            Defeat(All<EnemyBrain>().First(e=>e.definition.id=="Burrower"));yield return Frames(2);
            Assert.AreEqual("Adventure Awaits",MusicDirector.Current.Track,"the Burrower is down and the boss track plays on");
        }
        [UnityTest] public IEnumerator TheShopThemeTakesOverAndTheTownPicksUpWhereItLeftOff()
        {
            yield return Zone("Lanternwick");yield return new WaitForSecondsRealtime(1.3f);
            Plays("Old Village","Lanternwick");
            var town=MusicDirector.Current.ActiveSource;int reached=town.timeSamples;
            Assert.Greater(reached,0,"fixture: the town's track has been running");
            var shop=ShopPanel;shop.Open(GameCatalog.Find<ShopInventory>("Lanternwick"));yield return null;
            Plays("Moonbase Market","Corra's stall");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(MusicDirector.Current.ActiveSource.volume,.4f,"the shop theme never comes up");
            shop.Close();yield return null;
            Plays("Old Village","Lanternwick after the shop");
            Assert.GreaterOrEqual(MusicDirector.Current.ActiveSource.timeSamples,reached,"the town's track starts over every time the player leaves a shop");
        }

        // ------------------------------------------------------------------ 4.9: transitions
        [UnityTest] public IEnumerator AZoneLoadOnTheSameTrackDoesNotRestartOrDipIt()
        {
            yield return Zone("Hub_Decks","Office");yield return new WaitForSecondsRealtime(1.3f);
            Plays("Hub Town Groove","the Decks");
            var music=MusicDirector.Current.ActiveSource;int last=music.timeSamples;float lowest=music.volume;logged.Clear();
            foreach(string zone in new[]{"Hub_CinderHalo","TallowDrift","Hub_CinderHalo","Hub_Decks"})
            {
                SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
                do
                {
                    yield return null;
                    Assert.AreSame(music,MusicDirector.Current.ActiveSource,"loading "+zone+" moved the track to the other voice");
                    Assert.IsTrue(music.isPlaying,"the track stops while "+zone+" loads");
                    bool looped=last>music.clip.samples*.9f;
                    if(!looped)Assert.GreaterOrEqual(music.timeSamples,last,"the track jumps back while "+zone+" loads: it restarted");
                    last=music.timeSamples;lowest=Mathf.Min(lowest,music.volume);
                }
                while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline);
                Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            }
            Assert.Greater(lowest,.45f,"the track dips on a zone load that doesn't change it");
            Assert.IsEmpty(logged.Where(l=>l.StartsWith("MUSIC_CUE")),"a zone load re-cued the track it was already playing");
        }
        static AudioSource[] MusicVoices()=>GameServices.Current.GetComponents<AudioSource>().Where(s=>s.outputAudioMixerGroup!=null&&s.outputAudioMixerGroup.name=="Music").ToArray();
        /// <summary>A change of track: no voice jumps or stops while it can be heard, and at some point both are audible.</summary>
        static IEnumerator WatchTheFade(string to,string where,System.Action change)
        {
            var voices=MusicVoices();Assert.AreEqual(2,voices.Length,"fixture: two voices to fade between");
            var old=MusicDirector.Current.ActiveSource;var last=voices.Select(v=>v.volume).ToArray();bool both=false;
            change();
            for(float end=Time.unscaledTime+8;Time.unscaledTime<end;)
            {
                yield return null;
                // The fastest fade that still reads as one: across the whole level in about 0.4 s.
                float limit=1.5f*Time.unscaledDeltaTime+.02f;
                for(int i=0;i<voices.Length;i++)
                {
                    Assert.LessOrEqual(Mathf.Abs(voices[i].volume-last[i]),limit,$"{where}: a track jumps from {last[i]:0.00} to {voices[i].volume:0.00} in one frame: a cut, not a crossfade");
                    if(voices[i].volume>.05f)Assert.IsTrue(voices[i].isPlaying,where+": a track stops while it can still be heard");
                    last[i]=voices[i].volume;
                }
                both|=voices.All(v=>v.isPlaying&&v.volume>.05f);
                var now=MusicDirector.Current.ActiveSource;
                if(MusicDirector.Current.Track==to&&now!=old&&now.volume>=.5f&&old.volume<=.001f&&!SceneFlow.Current.Loading)break;
            }
            Assert.IsTrue(both,where+": the old track is gone before the new one starts: silence, not a crossfade");
            Assert.AreEqual(to,MusicDirector.Current.Track,where+": the wrong track came up");
            Assert.GreaterOrEqual(MusicDirector.Current.ActiveSource.volume,.5f,where+": the new track never comes up to level");
            Assert.AreEqual(0,old.volume,.001f,where+": the old track never fades out");
        }
        [UnityTest] public IEnumerator AChangeOfTrackCrossfadesOnALoadAtAShopAndAtABoss()
        {
            yield return Zone("Hub_Decks","Office");yield return new WaitForSecondsRealtime(1.3f);
            yield return WatchTheFade("Moon Caverns","the Decks to Hushwell",()=>SceneFlow.Current.LoadZone("Hushwell","Arrival"));
            yield return Quieted();
            var shop=ShopPanel;
            yield return WatchTheFade("Moonbase Market","opening a shop",()=>shop.Open(null));
            yield return WatchTheFade("Moon Caverns","closing a shop",()=>shop.Close());
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+30;
            EnemyBrain boss=null;
            yield return WatchTheFade(BossTrack,"a boss waking",()=>{boss=Spawn("BellowsBelow",Hero.transform.position+Vector3.forward*30);});
            yield return WatchTheFade("Moon Caverns","a boss falling",()=>Defeat(boss));
        }

        // ------------------------------------------------------------------ 4.9: skills against the fight, and the sliders
        IEnumerator NoLearnedSkillIsLouderThanABoss(bool flight,float y)
        {
            if(flight)yield return Zone("Arena_Flight");else{yield return Zone("Arena_Ground");Arena();}
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var member in State.party)member.level=12;yield return new WaitForSecondsRealtime(.6f);
            var victim=Dummy(flight?"ChoristerDart":"Ridgehound",new Vector3(200,y,5));
            var taren=Body("Taren");var sela=Body("Sela");var failures=new List<string>();
            string playing="";float bar=.3f;
            void Cue(string key,Transform owner)
            {
                foreach(var source in AudioManager.Current.GetComponentsInChildren<AudioSource>())
                    if(source.clip!=null&&source.clip.name==key&&source.isPlaying&&source.volume>bar)failures.Add($"{playing}: {key} at {source.volume:0.00}");
            }
            AudioManager.CueStarted+=Cue;
            try
            {
                foreach(var (hero,other,kind) in new[]{(taren,sela,SkillKind.AnchorSlam),(taren,sela,SkillKind.TetherHook),(taren,sela,SkillKind.ForgeSpiral),(taren,sela,SkillKind.Faultline),
                    (sela,taren,SkillKind.SurveyDrone),(sela,taren,SkillKind.WaypointBlink),(sela,taren,SkillKind.GravityFold),(sela,taren,SkillKind.Starfall)})
                {
                    Put(hero,new Vector3(200,y,0));Put(other,new Vector3(260,y,0));FaceForward(hero);hero.target=victim.Health;playing=kind.ToString();
                    for(float end=Time.unscaledTime+2;!hero.CanAct&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}
                    Assert.IsTrue(SkillBook.Equip(Member(hero.character),0,Skill(hero.character,kind)),kind+" can't be equipped");
                    hero.charge=100;hero.cooldowns[0]=0;Assert.IsTrue(hero.Skill(0),kind+" did not start");
                    yield return Drive(hero,kind==SkillKind.GravityFold?2.7f:kind==SkillKind.SurveyDrone?1.6f:1.3f);
                    Remove<SurveyDrone>();yield return null;
                }
            }
            finally{AudioManager.CueStarted-=Cue;}
            Assert.IsEmpty(failures,"a learned skill plays louder than nearly every boss cue, so the fight goes under it:\n"+string.Join("\n",failures.Distinct()));
        }
        [UnityTest] public IEnumerator NoLearnedSkillIsLouderThanABossOnFoot()=>NoLearnedSkillIsLouderThanABoss(false,0);
        [UnityTest] public IEnumerator NoLearnedSkillIsLouderThanABossInFlight()=>NoLearnedSkillIsLouderThanABoss(true,1);
        [UnityTest] public IEnumerator EverySliderMovesItsGainAndTheLevelsSurviveARelaunch()
        {
            Assert.IsNotNull(AudioMix.Current.SettingsFile,"audio UI tests require the runner's isolated settings path");
            touchedLevels=true;
            yield return Zone("Arena_Ground");
            var menu=Menu;menu.Open(PauseMenu.SettingsPage);yield return null;
            var mixer=AudioMix.Group(AudioBus.Master).audioMixer;
            foreach(var bus in Buses)
            {
                var slider=menu.GetComponentsInChildren<Slider>().Single(s=>s.name=="Sound_"+bus);
                slider.value=0;yield return null;
                Assert.IsTrue(mixer.GetFloat(bus+"Volume",out float silent),"the mixer has no gain for "+bus);
                Assert.Less(silent,-70,$"{bus} at zero still plays at {silent:0} dB: the slider doesn't reach the mix");
                slider.value=1;yield return null;
                mixer.GetFloat(bus+"Volume",out float full);Assert.AreEqual(0,full,3.8f,$"{bus} at full plays at {full:0} dB");
                slider.value=.5f;yield return null;
                mixer.GetFloat(bus+"Volume",out float half);Assert.Less(half,full-3,$"{bus} at half plays as loud as at full");
                StringAssert.Contains("50%",Text("SoundLabel_"+bus).text,bus+"'s label doesn't follow its slider");
            }
            menu.Close();yield return null;
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            mixer=AudioMix.Group(AudioBus.Master).audioMixer;
            foreach(var bus in Buses)
            {
                Assert.AreEqual(.5f,AudioMix.Current.Level(bus),.01f,bus+"'s level went back on the next launch");
                mixer.GetFloat(bus+"Volume",out float kept);Assert.Less(kept,-3,$"{bus}'s slider says half on the next launch and the mix plays it at {kept:0} dB");
            }
            float deadline=Time.unscaledTime+15;while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title,"fixture: boot reaches the title");
            title.OpenSettings();yield return null;
            StringAssert.Contains("50%",Text("SoundLabel_Music").text,"the title's settings don't show the saved level");
            title.CloseSettings();
        }

        // ------------------------------------------------------------------ 4.10: what skill spam leaves behind
        static readonly string[] Pooled={"Lattice particles","Pooled sound","Projectile"};
        static Dictionary<string,int> Census()
        {
            var count=new Dictionary<string,int>();
            foreach(var t in All<Transform>()){if(Pooled.Contains(t.name))continue;count.TryGetValue(t.name,out int n);count[t.name]=n+1;}
            return count;
        }
        static IEnumerator Cast(CombatActor hero,SkillKind kind)
        {
            for(float end=Time.unscaledTime+2;!hero.CanAct&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsTrue(SkillBook.Equip(Member(hero.character),0,Skill(hero.character,kind)),kind+" can't be equipped");
            hero.charge=100;hero.cooldowns[0]=0;Assert.IsTrue(hero.Skill(0),kind+" did not start");
            hero.motor.Move(Vector2.zero,false,false);yield return null;
        }
        IEnumerator SpamLeavesNothing(bool flight,float y)
        {
            if(flight)yield return Zone("Arena_Flight");else{yield return Zone("Arena_Ground");Arena();}
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var member in State.party)member.level=12;yield return new WaitForSecondsRealtime(.6f);
            string dummy=flight?"ChoristerDart":"Ridgehound";
            var first=Dummy(dummy,new Vector3(200,y,5));Dummy(dummy,new Vector3(203,y,7));
            var sela=Body("Sela");var taren=Body("Taren");
            void Stand(CombatActor hero,CombatActor other){Put(hero,new Vector3(200,y,0));Put(other,new Vector3(260,y,0));FaceForward(hero);hero.target=first.Health;}
            var hers=new[]{SkillKind.SurveyDrone,SkillKind.GravityFold,SkillKind.Starfall,SkillKind.WaypointBlink};var his=new[]{SkillKind.AnchorSlam,SkillKind.TetherHook,SkillKind.ForgeSpiral,SkillKind.Faultline};
            // First casts load what they use: count from after one of each.
            Stand(sela,taren);foreach(var kind in hers){yield return Cast(sela,kind);Stand(sela,taren);}
            Stand(taren,sela);foreach(var kind in his){yield return Cast(taren,kind);Stand(taren,sela);}
            Remove<SurveyDrone>();yield return Drive(taren,3.5f);
            var census=Census();int meshes=Resources.FindObjectsOfTypeAll<Mesh>().Length,materials=Resources.FindObjectsOfTypeAll<Material>().Length;
            int mostDrones=0;
            for(int round=0;round<4;round++)
            {
                Stand(sela,taren);foreach(var kind in hers)
                {
                    yield return Cast(sela,kind);Stand(sela,taren);
                    mostDrones=Mathf.Max(mostDrones,Object.FindObjectsByType<SurveyDrone>(FindObjectsSortMode.None).Length);
                }
            }
            Stand(taren,sela);foreach(var kind in his.Concat(his)){yield return Cast(taren,kind);Stand(taren,sela);}
            Assert.AreEqual(1,mostDrones,"recasting Survey Drone leaves more than one in the air");
            // The fight is over. Everything a skill made has run its course by now, the drone's nine seconds included.
            yield return Until(()=>SurveyDrone.Current==null,10);
            // Its last shot flies for up to three seconds.
            yield return new WaitForSecondsRealtime(3.2f);
            Assert.IsNull(SurveyDrone.Current,"the drone outlives its nine seconds");
            foreach(var (left,what) in new[]{(Live<SkillRing>(),"Skill ring"),(Live<StarfallStrike>(),"Starfall"),(Live<GravityWell>(),"Gravity fold"),(Live<WaypointCharge>(),"Waypoint charge"),(Live<TetherLine>(),"Tether hook"),(Live<FaultlineWave>(),"Faultline"),(Live<HitVolume>(),"HitVolume")})
                Assert.IsFalse(left,$"{what} objects are still in the world after the fight");
            Assert.AreEqual(0,Projectile.ActiveCount,"shots are still flying long after the fight");
            var now=Census();
            var grown=now.Where(p=>p.Value>(census.TryGetValue(p.Key,out int was)?was:0)).Select(p=>$"{p.Key} {(census.TryGetValue(p.Key,out int was)?was:0)} to {p.Value}").ToArray();
            Assert.IsEmpty(grown,"objects pile up with every cast:\n"+string.Join("\n",grown));
            Assert.LessOrEqual(Resources.FindObjectsOfTypeAll<Mesh>().Length,meshes+3,"meshes pile up with every cast");
            Assert.LessOrEqual(Resources.FindObjectsOfTypeAll<Material>().Length,materials+3,"materials pile up with every cast");
        }
        [UnityTest] public IEnumerator SkillSpamOnFootLeavesNothingBehind()=>SpamLeavesNothing(false,0);
        [UnityTest] public IEnumerator SkillSpamInFlightLeavesNothingBehind()=>SpamLeavesNothing(true,1);
        [UnityTest] public IEnumerator ARealThumbCannotStackDroneFoldOrStarfall()
        {
            yield return Zone("Arena_Ground");Arena();
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var member in State.party)member.level=12;yield return new WaitForSecondsRealtime(.6f);
            var sela=Body("Sela");if(Hero!=sela)PartyController.Current.Swap();
            foreach(var actor in PartyController.Current.members)actor.GetComponent<PartnerBrain>().enabled=false;
            Put(sela,new Vector3(200,0,0));Put(Body("Taren"),new Vector3(260,0,0));FaceForward(sela);
            var victim=Dummy("Ridgehound",new Vector3(200,0,5));sela.target=victim.Health;
            var mine=Member("Sela");mine.skillUses=null;
            Assert.IsTrue(SkillBook.Equip(mine,0,Skill("Sela",SkillKind.SurveyDrone))&&SkillBook.Equip(mine,1,Skill("Sela",SkillKind.GravityFold))&&SkillBook.Equip(mine,2,Skill("Sela",SkillKind.Starfall)),"fixture: the three go on their slots");
            for(int i=0;i<4;i++)sela.cooldowns[i]=0;
            int drones=0,folds=0,falls=0;
            // Every button, every frame, with Charge to spare, for longer than the longest cooldown.
            for(float end=Time.unscaledTime+15.5f;Time.unscaledTime<end;)
            {
                sela.charge=100;for(int slot=0;slot<3;slot++)sela.Skill(slot);
                sela.motor.Move(Vector2.zero,false,false);yield return null;
                drones=Mathf.Max(drones,Object.FindObjectsByType<SurveyDrone>(FindObjectsSortMode.None).Length);
                folds=Mathf.Max(folds,Object.FindObjectsByType<GravityWell>(FindObjectsSortMode.None).Length);
                falls=Mathf.Max(falls,Object.FindObjectsByType<StarfallStrike>(FindObjectsSortMode.None).Length);
            }
            Assert.Greater(SkillBook.Uses("Sela",Skill("Sela",SkillKind.SurveyDrone).id),1,"fixture: the drone came off cooldown and went again");
            Assert.Greater(SkillBook.Uses("Sela",Skill("Sela",SkillKind.Starfall).id),1,"fixture: Starfall came off cooldown and went again");
            Assert.AreEqual(1,drones,"two Survey Drones fire at once");Assert.AreEqual(1,folds,"two Gravity Folds tick at once");Assert.AreEqual(1,falls,"two Starfalls land at once");
        }
        [UnityTest] public IEnumerator AReleasedSkillDoesNotCrossAZoneLoad()
        {
            yield return Zone("Arena_Ground");Arena();
            foreach(var member in State.party)member.level=12;yield return new WaitForSecondsRealtime(.6f);
            var sela=Body("Sela");Put(sela,new Vector3(200,0,0));Put(Body("Taren"),new Vector3(260,0,0));FaceForward(sela);
            var victim=Dummy("Ridgehound",new Vector3(200,0,5));sela.target=victim.Health;
            foreach(var kind in new[]{SkillKind.SurveyDrone,SkillKind.Starfall,SkillKind.GravityFold})yield return Cast(sela,kind);
            yield return Drive(sela,.5f);
            Assert.IsTrue(Live<GravityWell>()&&Live<SurveyDrone>(),"fixture: the skills are out");
            yield return Zone("Arena_Flight");
            foreach(var (left,what) in new[]{(Live<SkillRing>(),"Skill ring"),(Live<StarfallStrike>(),"Starfall"),(Live<GravityWell>(),"Gravity fold"),(Live<SurveyDrone>(),"Survey drone")})
                Assert.IsFalse(left,what+" followed the party into the next zone");
            Assert.IsNull(SurveyDrone.Current,"the drone's slot still points at a drone from the last zone");
            Assert.AreEqual(1,Time.timeScale,"a zone load left the clock slowed");
        }

        // ------------------------------------------------------------------ 4.11: autosaves
        static void Holds(string kind,string zone,string spawn,string marker)
        {
            var saved=Saves.Load("autosave");Assert.IsNotNull(saved,kind+" wrote no save");
            Assert.AreEqual(zone,saved.zone,kind+": the autosave is in the wrong zone");
            if(spawn!=null)Assert.AreEqual(spawn,saved.spawn,kind+": the autosave resumes at the wrong spawn");
            Assert.IsTrue(saved.flags.TryGetValue(marker,out bool kept)&&kept,kind+" didn't save what happened just before it");
        }
        [UnityTest] public IEnumerator DockWarpRepairAndDiscoveryEachWriteTheAutosave()
        {
            yield return Zone("Hub_CinderHalo","Office");
            Flag("qa.dock");All<DockingPad>().First(d=>d.spawn=="Office").Interact();
            Assert.IsTrue(SceneFlow.Current.Loading,"fixture: the dock answers");
            yield return Until(()=>!SceneFlow.Current.Loading,25);yield return null;
            Holds("Docking","Hub_Decks","Office","qa.dock");
            yield return Zone("Hub_CinderHalo","Moon");
            Flag("met.Orrin");Flag("qa.warp");var warp=WarpTo("Sorrel_Ridges");string arrival=warp.spawn;warp.Interact();
            Assert.IsTrue(SceneFlow.Current.Loading,"fixture: the warp answers");
            yield return Until(()=>!SceneFlow.Current.Loading,25);yield return null;
            Holds("A warp","Sorrel_Ridges",arrival,"qa.warp");
            yield return Quieted();
            var hurt=Hero;hurt.Health.integrity=hurt.Health.maximum*.5f;yield return Frames(2);
            Flag("qa.repair");var bay=All<RepairBay>().First(b=>b.free&&b.saveSpawn=="Outpost");bay.Interact();yield return null;
            Holds("A repair","Sorrel_Ridges","Outpost","qa.repair");
            Assert.AreEqual(Levels.MaxIntegrity(Member(hurt.character).level),Saves.Load("autosave").party.Find(m=>m.id==hurt.character).integrity,.01f,"the autosave a repair writes still holds the wound");
            Flag("met.Aumi");State.questSteps["LanternRoad"]=3;
            yield return Zone("LanternGorge");
            Flag("qa.discovery");var relay=Discovery("gorge.relay2");Assert.IsTrue(relay.Available,"fixture: the relay answers");relay.Interact();yield return null;
            Holds("A discovery","LanternGorge",null,"qa.discovery");
            Assert.IsTrue(Saves.Load("autosave").flags.TryGetValue("gorge.relay2",out bool lit)&&lit,"a discovery's own flag isn't in the save it writes");
            // And one with a conversation: the save comes when it has been heard.
            Flag("qa.spoken");var spoken=Discovery("gorge.relay1");spoken.Interact();yield return FinishTalk();
            Holds("A discovery with a conversation","LanternGorge",null,"qa.spoken");
            Assert.IsTrue(Saves.Load("autosave").flags.TryGetValue("gorge.relay1",out bool told)&&told,"the conversation's discovery isn't in its save");
        }

        // ------------------------------------------------------------------ 4.11: Continue
        [UnityTest] public IEnumerator ContinueBringsBackZoneSpawnPartyGearQuestsFlagsLoadoutAndMastery()
        {
            var state=State;var inventory=RpgServices.Inventory;var taren=Member("Taren");var sela=Member("Sela");
            foreach(var member in state.party)member.level=12;
            var edge=inventory.GivePart(Part("EdgesT2"));edge.upgrade=2;Assert.IsTrue(inventory.Equip(taren,edge,Part("EdgesT2")),"fixture: the edge goes on");
            var emitter=inventory.GivePart(Part("EmittersT2"));Assert.IsTrue(inventory.Equip(sela,emitter,Part("EmittersT2")),"fixture: the emitter goes on");
            var second=inventory.GivePart(Part("ModulesT1"));Assert.IsTrue(inventory.Equip(taren,second,Part("ModulesT1"),true),"fixture: the module goes in slot 2");
            var slam=Skill("Taren",SkillKind.AnchorSlam);var starfall=Skill("Sela",SkillKind.Starfall);
            Assert.IsTrue(SkillBook.Equip(taren,0,slam),"fixture: Anchor Slam on slot 1");Assert.IsTrue(SkillBook.Equip(sela,1,starfall),"fixture: Starfall on slot 2");
            for(int i=0;i<7;i++){SkillBook.RecordUse("Taren",slam);SkillBook.RecordUse("Sela",starfall);}
            RpgServices.Quests.Start("IshensRecord");RpgServices.Quests.Report(ObjectiveKind.Interact,"RecordPlate");
            Assert.AreEqual(1,state.questCounts["IshensRecord/0"],"fixture: one plate counted");
            Flag("qa.continue");state.activeMember=1;state.scrip=1234;state.quickItem="ChargeCell";
            yield return Zone("Lanternwick","Gorge");
            var hurt=Body("Sela");hurt.Health.integrity=77;hurt.charge=33;yield return Frames(2);
            float output=Body("Taren").damage;Saves.Save("autosave",State);string edgeId=edge.instanceId;
            yield return ToTitle();yield return OpenPicker();
            StringAssert.Contains("Lanternwick",Words(Row("autosave")),"the save row doesn't say where the save is");
            StringAssert.Contains("Sync 12",Words(Row("autosave")),"the save row doesn't say how far along it is");
            yield return Pick("autosave","Lanternwick");
            // Where.
            Assert.AreEqual("Gorge",State.spawn,"Continue forgot which door the party came through");
            var spawn=All<SpawnPoint>().Single(s=>s.id=="Gorge");Assert.Less(Flat(Hero.transform.position,spawn.transform.position),4,"Continue doesn't resume at the saved spawn");
            // Who.
            Assert.AreEqual(2,PartyController.Current.members.Length,"Continue lost a party member");Assert.AreEqual("Sela",Hero.character,"Continue changed who leads");
            Assert.AreEqual(12,Member("Taren").level,"Continue lost Taren's Sync level");Assert.AreEqual(1234,State.scrip,"Continue lost scrip");Assert.AreEqual("ChargeCell",State.quickItem,"Continue lost the quick item");
            Assert.AreEqual(77,Hero.Health.integrity,1,"Continue healed or hurt the saved hero");Assert.AreEqual(33,Hero.charge,2,"Continue lost the saved hero's Charge");
            // What they wear, and that it still counts.
            Assert.AreEqual(edgeId,Member("Taren").equipped[GearSlot.Edge],"Continue took Taren's edge off");
            Assert.AreEqual(2,State.parts.Find(p=>p.instanceId==edgeId).upgrade,"Continue lost a part's upgrade");
            Assert.AreEqual(second.instanceId,Member("Taren").module2,"Continue emptied module slot 2");
            Assert.IsTrue(Member("Sela").equipped.ContainsKey(GearSlot.Emitter),"Continue took Sela's emitter off");
            Assert.AreEqual(output,Body("Taren").damage,.01f,"the saved gear no longer counts toward what Taren hits for");
            // What's going on.
            Assert.IsTrue(Has("qa.continue"),"Continue lost a story flag");
            Assert.AreEqual(0,Step("IshensRecord"),"Continue moved a quest");Assert.AreEqual(1,State.questCounts["IshensRecord/0"],"Continue lost a quest's count");
            // What they can do.
            Assert.AreEqual(SkillKind.AnchorSlam,SkillBook.Equipped("Taren",0).kind,"Continue put Taren's first slot back to the starting skill");
            Assert.AreEqual(SkillKind.Starfall,SkillBook.Equipped("Sela",1).kind,"Continue put Sela's second slot back to the starting skill");
            Assert.AreEqual(2,SkillBook.Mastery("Taren",slam.id),"Continue lost Anchor Slam's mastery");Assert.AreEqual(7,SkillBook.Uses("Sela",starfall.id),"Continue lost Starfall's count of uses");
            // On screen.
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;
            StringAssert.Contains("Mastery 2 (7/18)",Words(With("Starfall")),"the Skills page doesn't show the saved mastery");menu.Close();yield return null;
            menu.Open(PauseMenu.QuestsPage);yield return null;
            StringAssert.Contains("(1/3)",Words(With(GameCatalog.Find<QuestDef>("IshensRecord").title)),"the quest log doesn't show the saved count");menu.Close();yield return null;
            yield return Settle();
            Assert.IsFalse(Toast.Showing&&Toast.Heading!="SAVED","loading a save announces something that already happened: "+Toast.Heading);
        }
        [UnityTest] public IEnumerator ASlotSavedInFlightContinuesInFlightAtItsSpawn()
        {
            yield return Zone("WickReach","Lanternwick");
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            With("Save to slot 2").onClick.Invoke();yield return null;
            StringAssert.Contains("Saved to slot 2",Feedback(),"the save row doesn't say it saved");menu.Close();
            yield return ToTitle();yield return OpenPicker();yield return Pick("slot2","WickReach");
            var spawn=All<SpawnPoint>().Single(s=>s.id=="Lanternwick");
            Assert.Less(Flat(Hero.transform.position,spawn.transform.position),8,"a slot doesn't resume at the spawn it was saved at");
            Assert.IsTrue(GameInput.Current.FlightMode,"a save made in flight wakes up on foot's controls");
            var form=Hero.GetComponent<FormController>();yield return Until(()=>form.Current==BodyForm.Flight,2);
            Assert.AreEqual(BodyForm.Flight,form.Current,"a save made in flight wakes up in the wrong body");
            Assert.AreEqual("slot2",Saves.RetrySlot,"a defeat here would go back to another campaign's autosave");
        }

        // ------------------------------------------------------------------ 4.11: old saves
        /// <summary>A real save from before skill loadouts and chapter 2, as the October build wrote it (Builds/smoke-saves/route-slice), with its quotes swapped so it reads here.</summary>
        const string OctoberSave="{'version':2,'savedAtUtc':'2026-10-04T23:16:47.1981503Z','zone':'TallowDrift','spawn':'Arrival','activeMember':0,'quickItem':'RepairGel',"+
            "'party':[{'id':'Taren','level':7,'xp':562,'integrity':228.0,'charge':100.0,'gear':[],'equipped':{},'module2':null},{'id':'Sela','level':7,'xp':562,'integrity':228.0,'charge':100.0,'gear':[],'equipped':{},'module2':null}],"+
            "'flags':{'met.Orrin':true,'met.Hal':true,'hal.lesson':true,'met.Neve':true,'neve.cache':true,'met.Survivor':true,'clear.Sorrel_4':true,'clear.Sorrel_5':true,'clear.Sorrel_6':true,'clear.Sorrel_7':true,'clear.Sorrel_Burrower':true,"+
            "'bossdown.Burrower':true,'quest.Sorrel.complete':true,'warpkey':true,'clear.Hushwell_0':true,'clear.Hushwell_1':true,'clear.Hushwell_2':true,'clear.Hushwell_3':true,'clear.Hushwell_Bellows':true,'bossdown.BellowsBelow':true,"+
            "'hushwell.nursery':true,'quest.Hushwell.complete':true,'clear.Gullet_Chamber_0':true,'clear.Gullet_Chamber_1':true,'cache.Neve':true,'clear.Gullet_Chamber_2':true,'clear.Gullet_Cantor':true,'bossdown.Cantor':true,'met.Keeper':true},"+
            "'materials':{'ScrapAlloy':206,'LatticeFilament':20,'HuskCore':4,'WarpKey':1,'RidgeCrystal':3,'ChoirResin':21,'CantorPearl':1},'collectedMaterials':{'ScrapAlloy':206,'LatticeFilament':20,'HuskCore':4,'WarpKey':1,'RidgeCrystal':3,'ChoirResin':21,'CantorPearl':1},"+
            "'consumables':{'RepairGel':0},'craftingXp':{},'questSteps':{'Sorrel':4,'Hushwell':2},'questCounts':{'Sorrel/0':1,'Sorrel/1':1,'Sorrel/2':1,'Sorrel/3':1,'Hushwell/0':1,'Hushwell/1':1},'parts':[],'scrip':1504,'playtime':648.1296}";
        static void WriteOld(string slot,string json)
        {
            string path=Saves.PathForSlot(slot);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,json.Replace('\'','"'));
        }
        [UnityTest] public IEnumerator ASaveFromBeforeSkillLoadoutsLoadsWithTheStartingFour()
        {
            // Boot's own title came up before this save existed, and only a zone's flow can take the game back to a fresh one.
            yield return Zone("Arena_Ground");
            WriteOld("slot1",OctoberSave);
            yield return ToTitle();yield return OpenPicker();
            string row=Words(Row("slot1"));
            StringAssert.Contains("Tallow Drift",row,"the old save's row doesn't say where it is");StringAssert.Contains("Sync 7",row,"the old save's row doesn't say how far along it is");
            yield return Pick("slot1","TallowDrift");
            Assert.AreEqual(1504,State.scrip,"the old save lost its scrip");Assert.AreEqual(2,PartyController.Current.members.Length,"the old save lost a hero");
            foreach(string hero in new[]{"Taren","Sela"})
            {
                var starting=GameCatalog.Find<CharacterDef>(hero).skills;
                for(int slot=0;slot<SkillBook.Slots;slot++)Assert.AreEqual(starting[slot],SkillBook.Equipped(hero,slot),$"{hero}'s slot {slot+1} isn't the starting skill on a save with no loadout");
                Assert.AreEqual(6,SkillBook.Learned(hero).Length,$"at Sync 7 {hero} should know the starting four and the two learned at Sync 3 and 5");
                Assert.AreEqual(1,SkillBook.Mastery(hero,starting[0].id),"a save with no use counts starts somewhere other than mastery 1");
            }
            StringAssert.DoesNotContain("Practice",Objective(),"the old save has no objective");
            // The page reads it, and what is chosen there sticks.
            Assert.AreEqual("Taren",Hero.character,"fixture: Taren leads");
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;
            StringAssert.Contains("6 of 8",Note(),"the Skills page miscounts an old save's skills");
            Row("Row_1").onClick.Invoke();yield return null;With(Skill("Taren",SkillKind.AnchorSlam).displayName).onClick.Invoke();yield return null;
            Assert.AreEqual(SkillKind.AnchorSlam,SkillBook.Equipped("Taren",0).kind,"an old save can't change its loadout");
            menu.Close();yield return null;
            Saves.Save("slot2",State);
            var again=Saves.Load("slot2");Assert.IsNotNull(again.party[0].skillLoadout,"a loadout chosen on an old save isn't written");Assert.AreEqual(Skill("Taren",SkillKind.AnchorSlam).id,again.party[0].skillLoadout[0],"the wrong skill was written");
            // And it fights.
            yield return Zone("Arena_Ground");
            var fighter=Hero;fighter.charge=100;yield return Until(()=>fighter.CanAct,2);
            Assert.IsTrue(fighter.Skill(1),"a hero off an old save can't fire a skill");
            yield return Frames(2);Assert.IsTrue(Picture("Skill0").enabled&&Picture("Skill0").sprite!=null,"a hero off an old save has no skill icons");
        }
        [UnityTest] public IEnumerator ASaveFromBeforeChapterTwoFindsTheLanternRoad()
        {
            yield return Zone("Arena_Ground");
            WriteOld("slot1",OctoberSave.Replace("'met.Keeper':true","'met.Keeper':true,'sliceComplete':true,'tallow.lineObserved':true,'tallow.driftDiscussed':true,'compact.hailHeard':true"));
            yield return ToTitle();yield return OpenPicker();yield return Pick("slot1","TallowDrift");
            yield return Settle();
            StringAssert.Contains("route that waits",Objective(),"a save from the end of chapter 1 doesn't say where chapter 2 starts");
            foreach(var actor in PartyController.Current.members)actor.GetComponent<PartnerBrain>().enabled=false;
            yield return Talk(Person("Keeper"));
            Assert.AreEqual("AbriLanternTale",heard,"on an old save Abri doesn't tell the lantern tale");
            Assert.AreEqual(0,Step("LanternRoad"),"on an old save the Lantern Road doesn't start");
            yield return Zone("TallowApproach");yield return Settle();
            StringAssert.Contains("Wick Reach",Objective(),"on an old save the objective doesn't point at the Wick Reach");
            WarpTo("WickReach").Interact();
            Assert.IsTrue(SceneFlow.Current.Loading,"on an old save the warp to the Wick Reach stays shut");
            yield return Until(()=>!SceneFlow.Current.Loading,25);yield return null;
            Assert.AreEqual(1,Step("LanternRoad"),"on an old save arriving doesn't move the road on");
            var saved=Saves.Load("autosave");Assert.IsNotNull(saved,"on an old save the warp's autosave is missing");Assert.AreEqual("WickReach",saved.zone);
        }
        [UnityTest] public IEnumerator AVersionOneSaveKeepsItsRoadAndItsLead()
        {
            yield return Zone("Arena_Ground");
            WriteOld("slot1","{'version':1,'savedAtUtc':'2026-09-21T07:44:11.9650573Z','zone':'Hub_CinderHalo','spawn':'Outer','activeMember':1,'quickItem':'RepairGel',"+
                "'party':[{'id':'Taren','level':4,'xp':0,'integrity':174.0,'charge':0.0,'gear':[],'equipped':{},'module2':null},{'id':'Sela','level':4,'xp':0,'integrity':174.0,'charge':20.8466682,'gear':[],'equipped':{},'module2':null}],"+
                "'flags':{'met.Orrin':true,'bossdown.Burrower':true,'clear.Sorrel_Burrower':true,'warpkey':true,'quest.Sorrel.complete':true},'materials':{'WarpKey':1},'collectedMaterials':{'WarpKey':1},'consumables':{'RepairGel':3},'craftingXp':{},"+
                "'questSteps':{'Sorrel':4},'questCounts':{},'parts':[],'scrip':120,'playtime':9.419481}");
            yield return ToTitle();yield return OpenPicker();yield return Pick("slot1","Hub_CinderHalo");
            yield return Settle();
            Assert.AreEqual("Sela",Hero.character,"the oldest saves come back with the wrong lead");
            Assert.AreEqual(ChapterProgress.SaveVersion,State.version,"an old save isn't brought up to date");
            StringAssert.Contains("Gullet",Objective(),"a key-holder from before the nursery was required is told to go back for it");
            var warp=WarpTo("Gullet_Tunnel");warp.Interact();
            Assert.IsTrue(SceneFlow.Current.Loading,"a key-holder from before the nursery was required is locked out of the Gullet: "+warp.prompt);
            yield return Until(()=>!SceneFlow.Current.Loading,25);yield return null;
            Assert.AreEqual("Gullet_Tunnel",SceneFlow.Current.Zone);
        }

        // ------------------------------------------------------------------ 4.11: defeat
        [UnityTest] public IEnumerator DefeatOffersTheAutosaveAndTakesNothingItHeld()
        {
            AddPad();
            State.flags["qa.kept"]=true;State.scrip=555;State.questSteps["PellsLamp"]=0;
            var edge=RpgServices.Inventory.GivePart(Part("EdgesT2"));RpgServices.Inventory.Equip(Member("Taren"),edge,Part("EdgesT2"));string edgeId=edge.instanceId;
            yield return Zone("Sorrel_Ridges");
            Saves.Save("autosave",State);var bytes=File.ReadAllBytes(Saves.PathForSlot("autosave"));
            State.scrip=1;Flag("qa.sinceTheSave");
            foreach(var hero in PartyController.Current.members)hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*100,type=DamageType.Pulse});
            var panel=Object.FindFirstObjectByType<DefeatPanel>();yield return Until(()=>panel.IsOpen,6);
            Assert.IsTrue(panel.IsOpen,"a wipe opens no defeat panel");
            StringAssert.Contains("Return to last autosave",Words(Row("Retry")),"the defeat panel doesn't offer the autosave in those words");
            Assert.IsNotNull(Row("Title"),"the defeat panel offers no way to the title");
            Assert.AreEqual("Retry",Selected().name,"a pad's first press at the defeat panel isn't on the retry");
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(Saves.PathForSlot("autosave")),"being beaten wrote over the autosave");
            yield return Tap(GamepadButton.South);
            yield return Until(()=>!SceneFlow.Current.Loading&&PartyController.Current!=null&&PartyController.Current.members.All(m=>m.Health.Alive),15);
            yield return Quieted();
            Assert.IsFalse(GameTime.Paused,"the retry is paused");Assert.IsFalse(GameInput.Current.Blocked,"the retry takes no input");
            Assert.AreEqual(555,State.scrip,"the retry didn't go back to what the autosave held");
            Assert.IsTrue(Has("qa.kept"),"the retry lost a flag the autosave held");Assert.IsFalse(Has("qa.sinceTheSave"),"the retry kept something from after the autosave, and rolled nothing back");
            Assert.AreEqual(edgeId,Member("Taren").equipped[GearSlot.Edge],"the retry took Taren's gear off");Assert.AreEqual(0,Step("PellsLamp"),"the retry lost a quest");
            // The other way out loses nothing either.
            foreach(var hero in PartyController.Current.members)hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*100,type=DamageType.Pulse});
            panel=Object.FindFirstObjectByType<DefeatPanel>();yield return Until(()=>panel.IsOpen,6);
            Assert.IsTrue(panel.IsOpen,"a second wipe opens no defeat panel");
            Row("Title").onClick.Invoke();
            yield return Until(()=>!SceneFlow.Current.Loading&&Object.FindFirstObjectByType<TitleScreen>()!=null,15);yield return Frames(2);
            Assert.IsFalse(GameTime.Paused,"the pause came along to the title");
            var kept=Saves.Load("autosave");Assert.IsTrue(kept.flags.TryGetValue("qa.kept",out bool held)&&held,"leaving through the defeat panel lost the save's flags");
            yield return OpenPicker();StringAssert.Contains("Sorrel Ridges",Words(Row("autosave")),"after a defeat the autosave isn't where the party was");
        }

        // ------------------------------------------------------------------ 4.12: shapes
        static Vector3[] Outline(Telegraph marking){var filter=marking.GetComponent<MeshFilter>();Assert.IsNotNull(filter,"a marking draws nothing");Assert.Greater(filter.sharedMesh.vertexCount,0,"a marking's mesh is empty");return filter.sharedMesh.vertices;}
        [UnityTest] public IEnumerator EachMarkingShapeHasItsOwnOutlineWhateverItsColour()
        {
            yield return Zone("Arena_Ground");Arena();
            var at=new Vector3(200,0,0);var color=Telegraph.Threat;
            var circle=Telegraph.Circle(at,4,5,color);var ring=Telegraph.Ring(at+Vector3.right*20,3,6,5,color);
            var cone=Telegraph.Cone(at+Vector3.forward*20,Vector3.forward,6,60,5,color);var lane=Telegraph.Line(at+Vector3.back*20,Vector3.forward,10,2,5,color);
            yield return null;
            float Reach(Telegraph t)=>Outline(t).Max(v=>Flat(v,t.Origin));float Hole(Telegraph t)=>Outline(t).Min(v=>Flat(v,t.Origin));
            Assert.AreEqual(4,Reach(circle),.1f,"a circle's mesh isn't as wide as what it strikes");Assert.Less(Hole(circle),.1f,"a circle has a hole in it");
            Assert.AreEqual(6,Reach(ring),.1f,"a ring's mesh isn't as wide as what it strikes");Assert.AreEqual(3,Hole(ring),.1f,"a ring draws as a disc: nothing shows the safe middle");
            Assert.IsFalse(ring.Contains(ring.Origin),"the middle of a ring strikes");Assert.IsTrue(ring.Contains(ring.Origin+Vector3.right*4.5f),"the band of a ring doesn't strike");
            foreach(var v in Outline(cone))
            {
                var away=v-cone.Origin;away.y=0;
                if(away.magnitude>.1f)Assert.LessOrEqual(Vector3.Angle(Vector3.forward,away),30.5f,"a cone's mesh runs outside its angle");
            }
            Assert.AreEqual(6,Reach(cone),.1f,"a cone's mesh isn't as long as what it strikes");Assert.IsFalse(cone.Contains(cone.Origin+Vector3.back*3),"behind a cone strikes");
            var box=Outline(lane);
            Assert.AreEqual(10,box.Max(v=>v.z)-box.Min(v=>v.z),.1f,"a lane's mesh isn't as long as what it strikes");Assert.AreEqual(2,box.Max(v=>v.x)-box.Min(v=>v.x),.1f,"a lane's mesh isn't as wide as what it strikes");
            Assert.IsFalse(lane.Contains(lane.Origin+Vector3.right*3),"beside a lane strikes");
            foreach(var (mark,kind) in new[]{(circle,Telegraph.Kind.Circle),(ring,Telegraph.Kind.Ring),(cone,Telegraph.Kind.Cone),(lane,Telegraph.Kind.Line)})Assert.AreEqual(kind,mark.Shape);
            foreach(var mark in new[]{circle,ring,cone,lane})mark.Cancel();
        }

        // ------------------------------------------------------------------ 4.12: the Choir rule
        [UnityTest] public IEnumerator EveryOrdinaryEnemysWarningSoundHasAMarkOnTheGround()
        {
            yield return Zone("Arena_Ground");Arena();
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);yield return null;
            var hero=Hero;Put(hero,new Vector3(200,0,0));Put(Partner,new Vector3(260,0,0));hero.Health.InvulnerableUntil=GameTime.Now+120;
            var failures=new List<string>();int types=0;
            foreach(var def in GameCatalog.All<EnemyDef>().Where(d=>!d.boss))
            {
                types++;
                var enemy=ActorFactory.Enemy(def,hero.transform.position+Vector3.forward*2);var body=enemy.transform;bool told=false;var unseen=new List<string>();
                void Cue(string key,Transform owner)
                {
                    if(owner!=body||key.StartsWith("step_"))return;
                    if(enemy.Telegraphing&&enemy.AttackMark!=null)told=true;
                    else if(!enemy.Attacking)unseen.Add(key);
                }
                AudioManager.CueStarted+=Cue;
                try
                {
                    for(float end=Time.unscaledTime+4;!enemy.Telegraphing&&Time.unscaledTime<end;)yield return null;
                    yield return null;
                    if(!enemy.Telegraphing)failures.Add(def.id+" never winds up on a hero two metres away");
                    else
                    {
                        if(enemy.AttackMark==null)failures.Add(def.id+" winds up with no marking on the ground");
                        if(!told)failures.Add(def.id+" winds up in silence, or its wind-up sound comes with nothing marked");
                    }
                    foreach(var key in unseen)failures.Add($"{def.id}: {key} plays with nothing to see");
                }
                finally{AudioManager.CueStarted-=Cue;}
                enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(2);
                foreach(var mark in Telegraph.Live.ToArray())if(mark!=null)Object.Destroy(mark.gameObject);
                yield return null;
            }
            Assert.GreaterOrEqual(types,10,"fixture: the ordinary enemies");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        static bool SomethingToSee(BossController boss)
        {
            if(Telegraph.Live.Any(t=>t!=null&&t.owner==boss)||boss.Burrowed||Projectile.ActiveCount>0)return true;
            return Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.name=="Lattice particles"&&p.IsAlive());
        }
        IEnumerator EveryCueHasSomethingToSee(string id,bool flight,params string[] moves)
        {
            float y=flight?1:.1f;
            if(flight)yield return Zone("Arena_Flight");else{yield return Zone("Arena_Ground");Arena();}
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);yield return null;
            var lead=Hero;var partner=Partner;Put(lead,new Vector3(200,y,7));Put(partner,new Vector3(260,y,0));
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+120;
            var body=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),new Vector3(200,y,0));yield return Frames(3);body.Passive=true;yield return null;
            var boss=body.GetComponent<BossController>();var failures=new List<string>();
            foreach(string move in moves)
            {
                for(float end=Time.unscaledTime+7;(boss.Busy||body.Health.Broken)&&Time.unscaledTime<end;)yield return null;
                Put(lead,new Vector3(200,y,7));lead.Health.InvulnerableUntil=GameTime.Now+120;
                var cues=new List<(string key,int frame)>();var seen=new HashSet<int>();bool marked=false;
                void Cue(string key,Transform owner){if(owner==body.transform)cues.Add((key,Time.frameCount));}
                AudioManager.CueStarted+=Cue;
                try
                {
                    if(!boss.Perform(move)){failures.Add(move+": fixture: the move didn't start");continue;}
                    for(float end=Time.unscaledTime+8;Time.unscaledTime<end;)
                    {
                        if(SomethingToSee(boss))seen.Add(Time.frameCount);
                        marked|=Telegraph.Live.Any(t=>t!=null&&t.owner==boss);
                        if(!boss.Busy)break;
                        yield return null;
                    }
                }
                finally{AudioManager.CueStarted-=Cue;}
                if(!marked)failures.Add($"{id}'s {move} marks nothing on the ground");
                foreach(var (key,frame) in cues)if(!seen.Any(f=>Mathf.Abs(f-frame)<=3))failures.Add($"{id}'s {move}: {key} plays with nothing on screen to go with it");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        [UnityTest] public IEnumerator EveryBurrowerCueHasSomethingToSee()=>EveryCueHasSomethingToSee("Burrower",false,"DrillCharge","SpoilToss","TailSweep","Burrow","SeismicRings");
        [UnityTest] public IEnumerator EveryBellowsCueHasSomethingToSee()=>EveryCueHasSomethingToSee("BellowsBelow",false,"Breath","RibStar","Bubbles","Crossing");
        [UnityTest] public IEnumerator EveryCantorCueHasSomethingToSee()=>EveryCueHasSomethingToSee("Cantor",true,"Choir","SongSpiral","CoilLash");
        [UnityTest] public IEnumerator EveryHessaCueHasSomethingToSee()=>EveryCueHasSomethingToSee("HessaKorl",false,"HookLance","Advance","GrappleChain","Sidearm","SweepingCut","Lampbreaker");

        // ------------------------------------------------------------------ 4.12: the type floor, off the audit's beaten track
        static float ScaleAt720(CanvasScaler scaler)
        {var r=scaler.referenceResolution;return Mathf.Pow(2,Mathf.Lerp(Mathf.Log(1280/r.x,2),Mathf.Log(720/r.y,2),scaler.matchWidthOrHeight));}
        static void Audit(string surface,List<string> failures)
        {
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if(!text.isActiveAndEnabled||string.IsNullOrWhiteSpace(text.text)||text is TextMeshPro||text.name=="InputProbe"||text.name=="Damage")continue;
                var canvas=text.canvas!=null?text.canvas.rootCanvas:null;var scaler=canvas!=null?canvas.GetComponent<CanvasScaler>():null;
                if(scaler==null||scaler.uiScaleMode!=CanvasScaler.ScaleMode.ScaleWithScreenSize||!canvas.enabled)continue;
                float size=text.enableAutoSizing?text.fontSizeMin:text.fontSize;float px=size*ScaleAt720(scaler);
                string sample=text.text.Replace("\n"," ");sample=sample.Substring(0,Mathf.Min(40,sample.Length));
                if(px<15.99f)failures.Add($"{surface}: {canvas.name}/{text.name} renders at {px:0.0} px (\"{sample}\")");
                var rect=text.rectTransform.rect;
                if(rect.width>1)
                {
                    var need=text.GetPreferredValues(text.text,rect.width-text.margin.x-text.margin.z,0);
                    if(need.y>rect.height+1)failures.Add($"{surface}: {canvas.name}/{text.name} overflows its {rect.width:0}x{rect.height:0} box, needing {need.y:0} of height (\"{sample}\")");
                }
            }
        }
        [UnityTest] public IEnumerator TheScreensTheFloorAuditMissesReadAt720pToo()
        {
            var failures=new List<string>();
            yield return Zone("Arena_Ground");
            Saves.Save("autosave",State);Saves.Save("slot1",State);
            File.WriteAllText(Saves.PathForSlot("slot3"),"{");
            yield return ToTitle();Audit("title",failures);
            Row("New Game").onClick.Invoke();yield return Frames(2);Audit("the new game question",failures);
            Row("KeepSave").onClick.Invoke();yield return Frames(2);
            yield return OpenPicker();Audit("the save picker",failures);
            Row("Back").onClick.Invoke();yield return Frames(2);
            foreach(var (zone,device) in new[]{("Arena_Ground",PromptDevice.Keyboard),("Arena_Flight",PromptDevice.Gamepad)})
            {
                PromptService.ForceDevice(device);
                yield return Zone(zone);Audit(zone+" HUD",failures);
                var menu=Menu;
                menu.Open(PauseMenu.SkillsPage);yield return null;Row("Row_1").onClick.Invoke();yield return null;Audit(zone+" skill chooser",failures);menu.Close();yield return null;
                menu.Open(PauseMenu.SettingsPage);yield return null;With("Keyboard bindings").onClick.Invoke();yield return null;Audit(zone+" bindings",failures);
                Row("Pager").onClick.Invoke();yield return null;Audit(zone+" bindings, second page",failures);
                With("Back to sound controls").onClick.Invoke();yield return null;
                With("Leave the game").onClick.Invoke();yield return null;Audit(zone+" leave question",failures);menu.Close();yield return null;
                RpgServices.Inventory.GivePart(Part("EdgesT2"));
                var shop=ShopPanel;shop.Open(GameCatalog.Find<ShopInventory>("Lanternwick"));yield return null;Audit(zone+" shop",failures);
                Row("ShopRow0").onClick.Invoke();yield return null;Audit(zone+" sell page",failures);shop.Close();yield return null;
            }
            yield return Zone("Arena_Ground");
            foreach(var hero in PartyController.Current.members)hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*100,type=DamageType.Pulse});
            var panel=Object.FindFirstObjectByType<DefeatPanel>();yield return Until(()=>panel.IsOpen,6);
            Assert.IsTrue(panel.IsOpen,"fixture: the defeat panel");Audit("the defeat panel",failures);
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        [UnityTest] public IEnumerator WorldLabelsReadFromTheGamesCameraAtTheTypeFloor()
        {
            var failures=new List<string>();int read=0;
            foreach(string zone in new[]{"Hub_Decks","Hub_CinderHalo","TallowDrift","Sorrel_Ridges","Hushwell","Lanternwick","LanternGorge","SunkenRelay","WickReach"})
            {
                yield return Zone(zone);
                var view=ZoneController.Current.definition.cameraProfile;float seen=2*view.distance*Mathf.Tan(view.fov*.5f*Mathf.Deg2Rad);
                foreach(var label in Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None).Where(t=>t.text.Length>0&&t.enabled&&t.GetComponent<Renderer>().enabled))
                {
                    read++;float pixels=label.fontSize*.1f*label.transform.lossyScale.y/seen*1080;
                    if(pixels<24)failures.Add($"{zone}: \"{label.text}\" stands {pixels:0} px tall on a 1080p screen, under the type floor of 24");
                }
            }
            Assert.Greater(read,20,"fixture: the world has labels");
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }

        // ------------------------------------------------------------------ 4.12: rebinding, in play
        [UnityTest] public IEnumerator AReboundKeyActsAndItsOldKeyDoesNot()
        {
            keyboard=InputSystem.AddDevice<Keyboard>();PromptService.ForceDevice(PromptDevice.Keyboard);
            yield return Zone("Arena_Ground");Arena();
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            var party=PartyController.Current;var hero=party.Active;Put(hero,new Vector3(200,0,0));Put(party.members[1-party.index],new Vector3(206,0,0));
            var menu=Menu;menu.Open(PauseMenu.SettingsPage);yield return null;With("Keyboard bindings").onClick.Invoke();yield return null;
            foreach(var (row,key) in new[]{("Attack",Key.H),("Dodge",Key.G),("Guard",Key.Y),("Swap",Key.T),("Interact",Key.U)})
            {
                With(row).onClick.Invoke();yield return null;Assert.IsTrue(menu.Listening,"fixture: the row listens");
                yield return KeyPress(key);Assert.IsFalse(menu.Listening,"the listen never took "+key);
            }
            Row("Pager").onClick.Invoke();yield return null;
            With("Pause").onClick.Invoke();yield return null;yield return KeyPress(Key.P);Assert.IsFalse(menu.Listening,"the listen never took P");
            menu.Close();yield return null;
            foreach(var actor in party.members)actor.GetComponent<PlayerBrain>().AutoPilot=false;
            yield return new WaitForSecondsRealtime(.4f);
            int swings=hero.AttackSequence;yield return KeyTap(Key.J);
            Assert.AreEqual(swings,hero.AttackSequence,"the old attack key still attacks");
            yield return KeyTap(Key.H);Assert.Greater(hero.AttackSequence,swings,"the new attack key doesn't attack");
            yield return new WaitForSecondsRealtime(.7f);
            yield return KeyTap(Key.Space);Assert.AreNotEqual(ActorState.Dodge,hero.State,"the old dodge key still rolls");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.G));yield return Frames(2);
            Assert.AreEqual(ActorState.Dodge,hero.State,"the new dodge key doesn't roll");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.7f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.K));yield return Frames(2);
            Assert.AreNotEqual(ActorState.Guard,hero.State,"the old guard key still guards");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Y));yield return Frames(2);
            Assert.AreEqual(ActorState.Guard,hero.State,"the new guard key doesn't guard");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames(2);
            int swaps=party.SwapCount;yield return KeyTap(Key.Tab);
            Assert.AreEqual(swaps,party.SwapCount,"the old swap key still swaps");
            yield return KeyTap(Key.T);Assert.AreEqual(swaps+1,party.SwapCount,"one press of the new swap key doesn't swap once");
            var prompt=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();prompt.range=6;prompt.transform.position=party.Active.transform.position;
            yield return Frames(2);
            yield return KeyTap(Key.E);Assert.AreEqual(0,prompt.Uses,"the old interact key still interacts");
            yield return KeyTap(Key.U);Assert.AreEqual(1,prompt.Uses,"the new interact key doesn't interact");
            Object.Destroy(prompt.gameObject);yield return Frames(2);
            yield return KeyTap(Key.P);Assert.IsTrue(menu.IsOpen,"the new pause key doesn't open the menu");
            menu.Close();
        }

        // ------------------------------------------------------------------ 4.13: spam interact
        [UnityTest] public IEnumerator AThumbMashedThroughAConversationDoesNotPickAChoiceItNeverSaw()
        {
            AddPad();yield return Zone("Hub_Decks");
            var dialogue=DialogueSystem.Current;var panel=Object.FindFirstObjectByType<DialoguePanel>();
            Assert.IsTrue(dialogue.StartNode("HalFirst","Hal"),"fixture: Hal's talk starts");yield return Frames(2);
            float shownAt=-1,gone=-1;int start=dialogue.LinesPresented,presses=0;
            for(float deadline=Time.unscaledTime+15;Time.unscaledTime<deadline&&gone<0;)
            {
                if(mashed%5==0)presses++;
                MashStep();yield return null;
                Assert.LessOrEqual(dialogue.LinesPresented-start,presses+1,"a mash skipped a line nobody saw");
                if(shownAt<0&&panel.OptionsVisible)shownAt=Time.unscaledTime;
                if(shownAt>=0&&!panel.OptionsVisible)gone=Time.unscaledTime;
            }
            MashStop();
            Assert.GreaterOrEqual(shownAt,0,"fixture: the choice came up");
            Assert.Greater(gone,0,"a thumb on A never answers a choice: it is deaf");
            Assert.GreaterOrEqual(gone-shownAt,.25f,$"a thumb still tapping through Hal's lines answers his choice {gone-shownAt:0.00} s after it appears, on its top row, before anyone could read it: the shop and the bench hold their first press for this reason");
        }
        /// <summary>A's next press goes to whatever stands in reach, and an arrival can stand beside the pad it came in on.</summary>
        IEnumerator MashThroughAnExit(string from,string spawn,System.Func<InteractionPrompt> exit,string arrives,List<string> failures)
        {
            yield return Zone(from,spawn);
            var hero=Hero;var door=exit();string name=door.name;Put(hero,Beside(door,hero));hero.GetComponent<PlayerBrain>().AutoPilot=false;
            yield return Frames(2);
            PromptService.FindInteraction(hero.transform.position);
            if(PromptService.Interaction!=door){failures.Add($"fixture: beside {name} in {from} A answers {(PromptService.Interaction!=null?PromptService.Interaction.name:"nothing")}");yield break;}
            mashed=0;float deadline=Time.unscaledTime+6;
            while(!SceneFlow.Current.Loading&&Time.unscaledTime<deadline){MashStep();yield return null;}
            if(!SceneFlow.Current.Loading){failures.Add($"fixture: A at {name} in {from} starts no load");MashStop();yield break;}
            deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline){MashStep();yield return null;}
            Assert.AreEqual(arrives,SceneFlow.Current.Zone,$"fixture: {name} leads to {arrives}");
            // The screen is up, and a thumb doesn't stop in the same instant.
            float until=Time.unscaledTime+.25f;bool bounced=false;
            while(Time.unscaledTime<until){MashStep();yield return null;bounced|=SceneFlow.Current.Loading;}
            MashStop();
            if(bounced)failures.Add($"{from} to {arrives}: a thumb still tapping as the fade lifts sends the party straight back out through the exit it arrived beside");
            yield return Until(()=>!SceneFlow.Current.Loading,25);
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return Frames(2);}
        }
        [UnityTest] public IEnumerator AThumbMashedThroughALoadDoesNotBounceTheParty()
        {
            AddPad();Flag("met.Orrin");Flag("gorge.relaysLit");Flag("met.Aumi");
            var failures=new List<string>();
            yield return MashThroughAnExit("Hub_CinderHalo","Office",()=>All<DockingPad>().First(d=>d.spawn=="Office"),"Hub_Decks",failures);
            yield return MashThroughAnExit("Hub_CinderHalo","Moon",()=>WarpTo("Sorrel_Ridges"),"Sorrel_Ridges",failures);
            yield return MashThroughAnExit("TallowApproach","Dock",()=>All<DockingPad>().First(d=>d.scene=="TallowDrift"),"TallowDrift",failures);
            yield return MashThroughAnExit("LanternGorge","Relay",()=>WarpTo("SunkenRelay"),"SunkenRelay",failures);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator AThumbMashedAtTheTitleStartsOneGame()
        {
            AddPad();
            if(Saves.Exists("autosave"))Assert.Ignore("needs the batch runner's empty save folder");
            float deadline=Time.unscaledTime+20;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return Frames(2);logged.Clear();
            Assert.AreEqual("New Game",Selected().name,"fixture: with no save the title's focus is New Game");
            deadline=Time.unscaledTime+6;
            while(!SceneFlow.Current.Loading&&Time.unscaledTime<deadline){MashStep();yield return null;}
            Assert.IsTrue(SceneFlow.Current.Loading,"A at the title starts nothing");
            deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline){MashStep();yield return null;}
            float until=Time.unscaledTime+.3f;while(Time.unscaledTime<until){MashStep();yield return null;}
            MashStop();
            Assert.AreEqual(1,logged.Count(l=>l.StartsWith("NEW_GAME_OK")),"a mash at the title started the game more than once");
            Assert.AreEqual(1,logged.Count(l=>l.StartsWith("ZONE_ENTER")),"a mash carried on past the first zone");
            Assert.IsFalse(Menu.IsOpen,"a mash opened a menu");
        }
        [UnityTest] public IEnumerator AThumbMashedAtTheDefeatPanelRetriesOnce()
        {
            AddPad();yield return Zone("Arena_Ground");
            Saves.Save("autosave",State);logged.Clear();
            foreach(var hero in PartyController.Current.members)hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*100,type=DamageType.Pulse});
            var panel=Object.FindFirstObjectByType<DefeatPanel>();yield return Until(()=>panel.IsOpen,6);
            Assert.IsTrue(panel.IsOpen,"fixture: the defeat panel");
            float deadline=Time.unscaledTime+6;
            while(!SceneFlow.Current.Loading&&Time.unscaledTime<deadline){MashStep();yield return null;}
            Assert.IsTrue(SceneFlow.Current.Loading,"A at the defeat panel starts nothing");
            deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline){MashStep();yield return null;}
            float until=Time.unscaledTime+.3f;while(Time.unscaledTime<until){MashStep();yield return null;}
            MashStop();
            Assert.AreEqual(1,logged.Count(l=>l.StartsWith("ZONE_ENTER")),"a mash at the defeat panel loaded more than once");
            Assert.IsFalse(SceneFlow.Current.Loading,"a mash at the defeat panel keeps loading");
            Assert.IsTrue(PartyController.Current.members.All(m=>m.Health.Alive),"a mash at the defeat panel came back with someone down");
            Assert.IsFalse(GameTime.Paused,"a mash at the defeat panel left the retry paused");
        }
        [UnityTest] public IEnumerator AThumbMashedThroughABossIntroOnlySwings()
        {
            AddPad();yield return Zone("Sorrel_Ridges");
            var hero=Hero;Put(hero,new Vector3(0,.2f,164));Put(Partner,new Vector3(2,.2f,164));
            foreach(var member in PartyController.Current.members)member.Health.InvulnerableUntil=GameTime.Now+60;
            hero.GetComponent<PlayerBrain>().AutoPilot=false;
            int swings=hero.AttackSequence;bool card=false;string zone=SceneFlow.Current.Zone;
            Encounter("Sorrel_Burrower").Begin();
            foreach(var enemy in All<EnemyBrain>())if(enemy.definition.boss)enemy.Passive=false;
            for(float end=Time.unscaledTime+2.5f;Time.unscaledTime<end;)
            {
                MashStep();yield return null;
                var title=Hud("BossTitleCard");card|=title.text.Contains("BURROWER")&&title.alpha>.5f;
                Assert.IsFalse(DialogueSystem.Current.Running,"a mash through the boss's entrance started a conversation");
                Assert.IsFalse(Menu.IsOpen,"a mash through the boss's entrance opened a menu");
                Assert.IsFalse(SceneFlow.Current.Loading,"a mash through the boss's entrance left the zone");
                Assert.IsFalse(GameTime.Paused,"a mash through the boss's entrance paused the fight");
            }
            MashStop();
            Assert.IsTrue(card,"fixture: the title card came up");Assert.AreEqual(zone,SceneFlow.Current.Zone);
            Assert.Greater(hero.AttackSequence,swings,"A does nothing through the boss's entrance");
        }
        [UnityTest] public IEnumerator AThumbMashedThroughHessasConfrontationLandsInTheFight()
        {
            AddPad();yield return Zone("SunkenRelay");
            var lead=Hero;var dialogue=DialogueSystem.Current;heard=null;
            foreach(var member in PartyController.Current.members){Put(member,new Vector3(member==lead?8:6,.2f,123));member.Health.InvulnerableUntil=GameTime.Now+60;}
            lead.GetComponent<PlayerBrain>().AutoPilot=false;
            yield return Until(()=>dialogue.Running,3);
            Assert.IsTrue(dialogue.Running,"fixture: the captain speaks");
            for(float end=Time.unscaledTime+25;dialogue.Running&&Time.unscaledTime<end;){MashStep();yield return null;}
            Assert.IsFalse(dialogue.Running,"a mash can't finish Hessa's confrontation");
            Assert.IsTrue(Has("relay.hessaConfronted"),"a mash lost what her confrontation sets");
            for(float end=Time.unscaledTime+.8f;Time.unscaledTime<end;)
            {
                MashStep();yield return null;
                Assert.IsFalse(dialogue.Running,"a thumb still tapping starts her confrontation over");
            }
            MashStop();
            var keep=All<EnemyBrain>().FirstOrDefault(e=>e.definition.id=="HessaKorl");
            Assert.IsNotNull(keep,"she is gone after a mash through her lines");Assert.IsTrue(keep.Health.Alive,"she fell to a mash through her lines");
            Assert.IsFalse(GameInput.Current.Blocked,"a mash through her lines leaves the player without control");
        }
        [UnityTest] public IEnumerator APromptTheBossBarHidesDoesNotEatTheAttack()
        {
            AddPad();yield return Zone("Sorrel_Ridges");
            var hero=Hero;var bore=WarpTo("Hushwell");
            foreach(var member in PartyController.Current.members)member.Health.InvulnerableUntil=GameTime.Now+60;
            Put(Partner,new Vector3(-4,.2f,164));Put(hero,Beside(bore,hero));yield return Frames(2);
            Encounter("Sorrel_Burrower").Begin();
            foreach(var enemy in All<EnemyBrain>())if(enemy.definition.boss)enemy.Passive=false;
            yield return Frames(3);
            hero.GetComponent<PlayerBrain>().AutoPilot=false;
            yield return Until(()=>!string.IsNullOrEmpty(Hud("BossName").text)&&Hud("BossName").gameObject.activeInHierarchy,2);
            Assert.IsTrue(string.IsNullOrEmpty(Hud("Interaction").text),"fixture: the boss bar has hidden the prompt");
            // Before anyone has landed a hit, and with the rig still across the arena: the one moment a hidden prompt can be live.
            PromptService.FindInteraction(hero.transform.position);
            if(PromptService.Interaction!=null)
            {
                int swings=hero.AttackSequence;
                yield return Tap(GamepadButton.South);
                Assert.Greater(hero.AttackSequence,swings,$"beside {PromptService.Interaction.name} in the Burrower's first moments, with no prompt on screen, A answers the hidden prompt and the hero doesn't swing");
            }
            else Assert.Ignore("the rig reached the bore before the press: nothing hidden to answer");
        }

        // ------------------------------------------------------------------ 4.13: the pause menu
        IEnumerator StartAndSelectChangeNothing(string kind)
        {
            var dialogue=DialogueSystem.Current;var menu=Menu;
            Assert.IsTrue(dialogue.Running,"fixture: "+kind+" is running");
            string line=Text("Body").text;int lines=dialogue.LinesPresented;
            foreach(var button in new[]{GamepadButton.Start,GamepadButton.Select})
            {
                yield return Tap(button);
                Assert.IsFalse(menu.IsOpen,$"{button} during {kind} opens a menu over it");
                Assert.IsFalse(GameTime.Paused,$"{button} during {kind} paused the game behind the conversation");
                Assert.IsTrue(dialogue.Running,$"{button} during {kind} ended it");
                Assert.AreEqual(lines,dialogue.LinesPresented,$"{button} during {kind} moved it on");
                Assert.AreEqual(line,Text("Body").text,$"{button} during {kind} changed the line");
            }
        }
        IEnumerator AfterwardsStartStillWorks(string kind)
        {
            var menu=Menu;
            if(menu.IsOpen)menu.Close();var shop=ShopPanel;if(shop.IsOpen)shop.Close();
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsFalse(menu.IsOpen,"a press made during "+kind+" opens the menu when it ends");
            Assert.IsFalse(GameInput.Current.Blocked,kind+" leaves the player without control");
            yield return Tap(GamepadButton.Start);
            Assert.IsTrue(menu.IsOpen,"Start is dead after "+kind);
            yield return Tap(GamepadButton.Start);
            Assert.IsFalse(menu.IsOpen,"Start doesn't close the menu after "+kind);
            Assert.AreEqual(1,Time.timeScale,"the clock stays stopped after "+kind);
        }
        [UnityTest] public IEnumerator StartAndSelectChangeNothingInEveryKindOfConversation()
        {
            AddPad();yield return Zone("Hub_Decks");
            var dialogue=DialogueSystem.Current;var panel=Object.FindFirstObjectByType<DialoguePanel>();
            // Someone in town: a line typing out, a line waiting, and a choice.
            Assert.IsTrue(dialogue.StartNode("HalFirst","Hal"),"fixture: Hal's talk");yield return new WaitForSecondsRealtime(.1f);
            Assert.Less(Text("Body").maxVisibleCharacters,Text("Body").text.Length,"fixture: the line is still typing");
            yield return StartAndSelectChangeNothing("a line still typing");
            yield return Tap(GamepadButton.South);
            yield return StartAndSelectChangeNothing("a line waiting for a press");
            // Esc is the keyboard's pause and its back, and neither belongs to a conversation.
            keyboard=InputSystem.AddDevice<Keyboard>();string waiting=Text("Body").text;
            yield return KeyTap(Key.Escape);
            Assert.IsFalse(Menu.IsOpen,"Esc during a conversation opens a menu over it");Assert.IsTrue(dialogue.Running,"Esc walks out of a conversation");Assert.AreEqual(waiting,Text("Body").text,"Esc moved the conversation on");
            for(float end=Time.unscaledTime+10;!panel.OptionsVisible&&Time.unscaledTime<end;)yield return Tap(GamepadButton.South);
            Assert.IsTrue(panel.OptionsVisible,"fixture: the choice came up");
            yield return StartAndSelectChangeNothing("a choice");
            yield return Hold(new GamepadState());
            yield return new WaitForSecondsRealtime(.4f);
            yield return Tap(GamepadButton.DpadDown);yield return Tap(GamepadButton.DpadDown);yield return Tap(GamepadButton.South);yield return FinishTalk();
            yield return AfterwardsStartStillWorks("a conversation in town");
            // A discovery the party triggers.
            Flag("met.Aumi");State.questSteps["LanternRoad"]=3;
            yield return Zone("LanternGorge");
            Discovery("gorge.relay1").Interact();yield return new WaitForSecondsRealtime(.2f);
            yield return StartAndSelectChangeNothing("a discovery's conversation");
            yield return FinishTalk();
            Assert.IsTrue(Has("gorge.relay1"),"pressing Start through a discovery's conversation lost the discovery");
            yield return AfterwardsStartStillWorks("a discovery's conversation");
            // A recorded message nobody is standing there for.
            Assert.IsTrue(Npc.Say("CacheFound","Neve"),"fixture: the recording");yield return new WaitForSecondsRealtime(.2f);
            yield return StartAndSelectChangeNothing("a recorded message");
            yield return FinishTalk();
            yield return AfterwardsStartStillWorks("a recorded message");
            // A hail, in flight, on the other button map.
            Flag("met.Neve");yield return Zone("Hub_CinderHalo","Shop");
            var hail=All<HailPoint>().First(h=>h.contact!=null&&h.contact.speaker=="Neve");hail.Interact();yield return new WaitForSecondsRealtime(.2f);
            yield return StartAndSelectChangeNothing("a hail in flight");
            yield return FinishTalk();
            yield return AfterwardsStartStillWorks("a hail in flight");
            // A speaker who starts on her own: the captain's surrender.
            Flag("relay.hessaConfronted");Flag("bossdown.HessaKorl");Flag("clear.Relay_Hessa");
            yield return Zone("SunkenRelay");
            var person=Person("Hessa");foreach(var member in PartyController.Current.members)Put(member,person.transform.position+new Vector3(0,.2f,-4));
            yield return Until(()=>DialogueSystem.Current.Running,5);
            yield return new WaitForSecondsRealtime(.2f);
            yield return StartAndSelectChangeNothing("a speaker who starts unasked");
            yield return FinishTalk();
            Assert.IsTrue(Has("relay.hessaYielded"),"pressing Start through her surrender lost it");
            yield return AfterwardsStartStillWorks("a speaker who starts unasked");
        }
        [UnityTest] public IEnumerator APauseInARadioHailLosesNoLine()
        {
            AddPad();Flag("met.Orrin");Flag("warpkey");Flag("hushwell.nursery");
            var said=new List<string>();void Heard(string who,string line){if(who=="Orrin")said.Add(line);}
            BarkService.Spoken+=Heard;
            try
            {
                yield return Zone("Hub_CinderHalo","Outer");
                yield return Until(()=>said.Count>0,10);
                Assert.AreEqual(1,said.Count,"fixture: Orrin's first line");
                yield return Tap(GamepadButton.Start);Assert.IsTrue(Menu.IsOpen,"fixture: the menu opens");
                yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(1,said.Count,"the radio talks on behind a pause menu");
                yield return Tap(GamepadButton.Start);
                yield return Until(()=>said.Count>=2,8);
                Assert.AreEqual(2,said.Count,"a pause in the middle of Orrin's hail loses the rest of it");
            }
            finally{BarkService.Spoken-=Heard;}
        }
        static void QuietBrains(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static readonly (string hero,SkillKind kind)[] Learned=
        {
            ("Taren",SkillKind.AnchorSlam),("Taren",SkillKind.TetherHook),("Taren",SkillKind.ForgeSpiral),("Taren",SkillKind.Faultline),
            ("Sela",SkillKind.SurveyDrone),("Sela",SkillKind.WaypointBlink),("Sela",SkillKind.GravityFold),("Sela",SkillKind.Starfall),
        };
        IEnumerator PauseMidSkill(bool flight,float y)
        {
            AddPad();
            if(flight)yield return Zone("Arena_Flight");else{yield return Zone("Arena_Ground");Arena();}
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var member in State.party)member.level=12;yield return new WaitForSecondsRealtime(.6f);
            var victim=Dummy(flight?"ChoristerDart":"Ridgehound",new Vector3(200,y,3.5f));var body=victim.Health;var menu=Menu;var failures=new List<string>();
            foreach(var (who,kind) in Learned)
            {
                var party=PartyController.Current;var caster=Body(who);if(party.Active!=caster)party.Swap();
                QuietBrains();var other=party.members.First(m=>m!=caster);
                Put(caster,new Vector3(200,y,0));Put(other,new Vector3(260,y,0));FaceForward(caster);caster.target=body;
                Remove<SurveyDrone>();Remove<GravityWell>();Remove<StarfallStrike>();Remove<WaypointCharge>();Remove<TetherLine>();Remove<FaultlineWave>();yield return null;
                for(float end=Time.unscaledTime+2;!caster.CanAct&&Time.unscaledTime<end;){caster.motor.Move(Vector2.zero,false,false);yield return null;}
                Assert.IsTrue(SkillBook.Equip(Member(who),0,Skill(who,kind)),kind+" can't be equipped");
                caster.charge=100;caster.cooldowns[0]=0;float before=body.integrity;
                Assert.IsTrue(caster.Skill(0),kind+" did not start");
                caster.motor.Move(Vector2.zero,false,false);
                // The real button, mid-pose.
                yield return Hold(new GamepadState().WithButton(GamepadButton.Start));yield return Hold(new GamepadState());
                if(!menu.IsOpen){failures.Add(kind+": Start during the skill doesn't open the menu");continue;}
                float held=body.integrity,charge=caster.charge,cooldown=caster.cooldowns[0];var at=caster.transform.position;
                yield return new WaitForSecondsRealtime(.5f);
                if(body.integrity!=held)failures.Add(kind+": the target takes damage behind the pause menu");
                if(caster.charge!=charge)failures.Add(kind+": Charge drains behind the pause menu");
                if(caster.cooldowns[0]!=cooldown)failures.Add(kind+": the cooldown runs behind the pause menu");
                if(Vector3.Distance(at,caster.transform.position)>.001f)failures.Add(kind+": the hero moves behind the pause menu");
                yield return Tap(GamepadButton.Start);
                if(menu.IsOpen){failures.Add(kind+": Start doesn't close the menu");menu.Close();}
                if(Time.timeScale!=1)failures.Add(kind+": the clock doesn't come back after the pause");
                for(float end=Time.unscaledTime+3.5f;body.integrity>=before&&Time.unscaledTime<end;){caster.motor.Move(Vector2.zero,false,false);yield return null;}
                if(body.integrity>=before)failures.Add(kind+": paid for, paused, and it never lands");
                float cooled=caster.cooldowns[0];
                for(float end=Time.unscaledTime+2;!caster.CanAct&&Time.unscaledTime<end;){caster.motor.Move(Vector2.zero,false,false);yield return null;}
                if(!caster.CanAct)failures.Add(kind+": the hero never comes back from the pose after a pause");
                if(cooled<=0&&Skill(who,kind).cooldown>4)failures.Add(kind+": the cooldown finished during the pause");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator APauseMidSkillHoldsItAndItStillLandsOnFoot()=>PauseMidSkill(false,0);
        [UnityTest] public IEnumerator APauseMidSkillHoldsItAndItStillLandsInFlight()=>PauseMidSkill(true,1);

        // ------------------------------------------------------------------ 4.13: swapping
        [UnityTest] public IEnumerator ASwapMidSkillLetsItLandAndTheNewLeadActsAtOnce()
        {
            yield return Zone("Arena_Ground");Arena();
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var member in State.party)member.level=12;yield return new WaitForSecondsRealtime(.6f);
            var party=PartyController.Current;var victim=Dummy("Ridgehound",new Vector3(200,0,3.5f));var failures=new List<string>();
            bool Shows(SkillKind kind)=>kind switch
            {
                SkillKind.TetherHook=>Live<TetherLine>(),SkillKind.Faultline=>Live<FaultlineWave>(),SkillKind.SurveyDrone=>Live<SurveyDrone>(),SkillKind.WaypointBlink=>Live<WaypointCharge>(),
                SkillKind.GravityFold=>Live<GravityWell>(),SkillKind.Starfall=>Live<StarfallStrike>(),_=>Live<SkillRing>(),
            };
            foreach(var (who,kind) in Learned)
            {
                var hero=Body(who);if(party.Active!=hero)party.Swap();
                QuietBrains();var next=party.members.First(m=>m!=hero);
                Remove<SurveyDrone>();Remove<GravityWell>();Remove<StarfallStrike>();Remove<WaypointCharge>();Remove<TetherLine>();Remove<FaultlineWave>();Remove<SkillRing>();yield return null;
                Put(hero,new Vector3(200,0,0));Put(next,new Vector3(207,0,0));FaceForward(hero);hero.target=victim.Health;
                for(float end=Time.unscaledTime+3;(!hero.CanAct||!next.CanAct)&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);next.motor.Move(Vector2.zero,false,false);yield return null;}
                Assert.IsTrue(SkillBook.Equip(Member(who),0,Skill(who,kind)),kind+" can't be equipped");
                hero.charge=100;hero.cooldowns[0]=0;Assert.IsTrue(hero.Skill(0),kind+" did not start");
                hero.motor.Move(Vector2.zero,false,false);yield return null;
                int swaps=party.SwapCount;
                if(!party.Swap()||party.Active!=next||party.SwapCount!=swaps+1){failures.Add(kind+": the swap was refused mid-skill");continue;}
                if(!next.Attack())failures.Add(kind+": the hero swapped in can't act at once");
                bool shown=false;
                for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end&&!shown;){shown=Shows(kind);yield return null;}
                if(!shown)failures.Add($"{who}'s {kind}: paid for, then swapped out of, and it never comes");
                // The companion's own swings aside: left alone, the body comes free.
                hero.GetComponent<PartnerBrain>().enabled=false;
                yield return new WaitForSecondsRealtime(1.2f);
                if(hero.State==ActorState.Skill)failures.Add($"{who} is stuck in {kind}'s pose after being swapped out of it");
                if(!hero.CanAct&&hero.Health.Alive)failures.Add($"{who} can't act long after being swapped out of {kind}");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        IEnumerator SwapMidRoll(bool flight,float y)
        {
            if(flight)yield return Zone("Arena_Flight");else{yield return Zone("Arena_Ground");Arena();}
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);yield return null;
            var party=PartyController.Current;var hero=party.Active;var next=party.members[1-party.index];
            Put(hero,new Vector3(200,y,0));Put(next,new Vector3(207,y,0));yield return new WaitForSecondsRealtime(.2f);
            var start=hero.transform.position;
            Assert.IsTrue(hero.Dodge(Vector3.forward),"fixture: the roll goes");
            hero.motor.Move(Vector2.zero,false,false);yield return null;hero.motor.Move(Vector2.zero,false,false);yield return null;
            Assert.AreEqual(ActorState.Dodge,hero.State,"fixture: mid-roll");
            int swaps=party.SwapCount;Assert.IsTrue(party.Swap(),"a swap is refused mid-roll");Assert.AreEqual(swaps+1,party.SwapCount);
            Assert.AreEqual(ActorState.Idle==next.State||ActorState.Move==next.State,true,"the hero swapped in isn't free to act: "+next.State);
            Assert.IsTrue(next.Attack(),"the hero swapped in during a roll can't act at once");
            yield return new WaitForSecondsRealtime(.6f);
            Assert.Greater(Flat(start,hero.transform.position),2,"a roll stops dead when its hero is swapped out: it travelled "+Flat(start,hero.transform.position).ToString("0.0")+" m");
            Assert.AreNotEqual(ActorState.Dodge,hero.State,"the hero swapped out mid-roll stays in the roll");
            Assert.IsTrue(hero.CanAct,"the hero swapped out mid-roll can't act afterwards");
            // And it can roll again when it comes back.
            Assert.IsTrue(party.Swap(),"can't swap back");QuietBrains();yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(hero.Dodge(Vector3.back),"a hero swapped out of a roll can't roll again");
        }
        [UnityTest] public IEnumerator ASwapMidRollFinishesTheRollAndFreesBothHeroesOnFoot()=>SwapMidRoll(false,0);
        [UnityTest] public IEnumerator ASwapMidRollFinishesTheRollAndFreesBothHeroesInFlight()=>SwapMidRoll(true,1);
        IEnumerator SwapWhileDowned(bool flight,float y)
        {
            AddPad();
            if(flight)yield return Zone("Arena_Flight");else{yield return Zone("Arena_Ground");Arena();}
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);yield return null;
            var party=PartyController.Current;var lead=party.Active;var other=party.members[1-party.index];
            Put(lead,new Vector3(200,y,0));Put(other,new Vector3(210,y,0));
            foreach(var actor in party.members)actor.GetComponent<PlayerBrain>().AutoPilot=false;
            yield return new WaitForSecondsRealtime(.2f);
            // The partner is down: the button can't bring them.
            other.Health.Receive(new DamagePacket{amount=other.Health.maximum*10,type=DamageType.Pulse});yield return Frames(2);
            Assert.IsFalse(other.Health.Alive,"fixture: the partner is down");
            int swaps=party.SwapCount;yield return Tap(GamepadButton.North);
            Assert.AreEqual(swaps,party.SwapCount,"a swap landed on a downed hero");Assert.AreSame(lead,party.Active,"the lead changed to a downed hero");
            var from=lead.transform.position;yield return Hold(new GamepadState{leftStick=Vector2.right});yield return new WaitForSecondsRealtime(.3f);yield return Hold(new GamepadState());
            Assert.Greater(Vector3.Distance(from,lead.transform.position),.3f,"a press of swap with the partner down leaves the lead stuck");
            // Up again, and a swap onto a body still getting up gives control back when it is up.
            float reach=flight?HeroCollision.HullRadius(lead.character)+HeroCollision.HullRadius(other.character)+.2f:1.5f;
            Put(other,lead.transform.position+Vector3.right*reach);
            yield return Until(()=>other.Health.Alive,4);Assert.IsTrue(other.Health.Alive,"standing over a downed partner doesn't bring them back");
            yield return Tap(GamepadButton.North);
            yield return new WaitForSecondsRealtime(CombatActor.ReviveDuration+.5f);
            Assert.IsTrue(party.Active.CanAct,"after swapping onto a hero getting up, control never comes back");
            // The lead falls: the partner covers at once, and the button stays honest.
            if(party.Active!=lead){yield return Tap(GamepadButton.North);Assert.AreSame(lead,party.Active,"fixture: back to the first lead");}
            lead.Health.Receive(new DamagePacket{amount=lead.Health.maximum*10,type=DamageType.Pulse});yield return Frames(2);
            Assert.AreSame(other,party.Active,"when the lead falls the partner doesn't take over");
            Assert.AreEqual(party.index,State.activeMember,"the save's lead doesn't follow the forced swap");
            swaps=party.SwapCount;yield return Tap(GamepadButton.North);
            Assert.AreEqual(swaps,party.SwapCount,"a swap landed back on the fallen hero");
            from=other.transform.position;yield return Hold(new GamepadState{leftStick=Vector2.right});yield return new WaitForSecondsRealtime(.3f);yield return Hold(new GamepadState());
            Assert.Greater(Vector3.Distance(from,other.transform.position),.3f,"the hero covering for a fallen lead doesn't answer the stick");
            // Both down: the button does nothing strange on the way to the defeat panel.
            other.Health.Receive(new DamagePacket{amount=other.Health.maximum*10,type=DamageType.Pulse});
            var panel=Object.FindFirstObjectByType<DefeatPanel>();
            for(float end=Time.unscaledTime+4;!panel.IsOpen&&Time.unscaledTime<end;)yield return Tap(GamepadButton.North);
            Assert.IsTrue(panel.IsOpen,"swapping through a wipe stops the defeat panel coming up");
        }
        [UnityTest] public IEnumerator SwapWhileDownedStaysHonestOnFoot()=>SwapWhileDowned(false,0);
        [UnityTest] public IEnumerator SwapWhileDownedStaysHonestInFlight()=>SwapWhileDowned(true,1);
        [UnityTest] public IEnumerator ASwapRefusedForADownedPartnerAnswers()
        {
            AddPad();yield return Zone("Arena_Ground");Arena();
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            var party=PartyController.Current;var lead=party.Active;var other=party.members[1-party.index];
            Put(lead,new Vector3(200,0,0));Put(other,new Vector3(210,0,0));lead.GetComponent<PlayerBrain>().AutoPilot=false;
            other.Health.Receive(new DamagePacket{amount=other.Health.maximum*10,type=DamageType.Pulse});yield return Frames(2);
            using var log=new CueLog();
            yield return Tap(GamepadButton.North);
            Assert.Greater(log.Count("ui_error"),0,"Y with the partner down does nothing and says nothing, where a refused skill, roll or quick item each answer: it reads as a dead button in the worst moment of a fight");
        }

        // ------------------------------------------------------------------ 4.13: leaving a boss fight
        IEnumerator NoWayOut(string zone,string encounter,string bossId,params string[] flags)
        {
            AddPad();foreach(var f in flags)Flag(f);
            yield return Zone(zone);
            var party=PartyController.Current;var lead=party.Active;var mate=party.members[1-party.index];var door=Encounter(encounter);
            foreach(var hero in party.members)hero.Health.InvulnerableUntil=GameTime.Now+200;
            Vector3 Inside(Vector3 at)
            {
                if(lead.flight)return new Vector3(at.x,lead.transform.position.y,at.z);
                return NavMesh.SamplePosition(at,out var near,8,NavMesh.AllAreas)?near.position+Vector3.up*.1f:at;
            }
            Put(lead,Inside(door.transform.position));Put(mate,Inside(door.transform.position)+Vector3.right*2);
            door.Begin();yield return Frames(3);
            var boss=All<EnemyBrain>().First(e=>e.definition.id==bossId);boss.Passive=true;
            var failures=new List<string>();int walked=0;
            // Every seal is shut and holds, walking or rolling at it, across its width.
            foreach(var seal in door.membranes)
            {
                if(seal==null)continue;
                if(seal.Open){failures.Add(seal.name+" stands open in the middle of the fight");continue;}
                var inward=door.transform.position-seal.transform.position;inward.y=0;inward.Normalize();var across=Vector3.Cross(Vector3.up,inward);
                foreach(float side in new[]{0f,-3f,3f})
                {
                    var from=seal.transform.position+inward*2.5f+across*side;
                    if(!NavMesh.SamplePosition(from,out var near,3,NavMesh.AllAreas))continue;
                    walked++;Put(lead,near.position+Vector3.up*.1f);
                    yield return Walk(lead,-inward,.7f);
                    if(Vector3.Dot(lead.transform.position-seal.transform.position,inward)<-.1f)failures.Add($"walking at {seal.name} at {side:0} m off its centre goes through it");
                    if(side!=0)continue;
                    Put(lead,near.position+Vector3.up*.1f);yield return new WaitForSecondsRealtime(.45f);
                    lead.Dodge(-inward);yield return Drive(lead,.4f);
                    if(Vector3.Dot(lead.transform.position-seal.transform.position,inward)<-.1f)failures.Add($"rolling at {seal.name} goes through it");
                }
            }
            if(door.membranes.Any(m=>m!=null))Assert.Greater(walked,0,"fixture: nowhere to stand at "+encounter+"'s seals");
            // A hit has landed: this is the middle of the fight.
            Put(lead,Inside(door.transform.position));boss.Passive=false;
            var packet=new DamagePacket{source=lead.Health,amount=1,type=DamageType.Pulse};
            if(boss.TryGetComponent<CantorCollar>(out var collar))collar.Links[0].Receive(packet);else boss.Health.Receive(packet);
            yield return Frames(2);
            Assert.IsTrue(BossController.AnyFightUnderway,"fixture: "+bossId+"'s fight is under way");
            lead.GetComponent<PlayerBrain>().AutoPilot=false;
            int tried=0;
            foreach(var exit in All<InteractionPrompt>().Where(p=>(p is DockingPad||p is WarpBeacon)&&p.gameObject.activeInHierarchy))
            {
                Put(lead,lead.flight?new Vector3(exit.transform.position.x,lead.transform.position.y,exit.transform.position.z-1.5f):Inside(exit.transform.position));yield return Frames(2);
                bool inFight=BossController.AnyFightUnderway;bool storyLocked=exit is WarpBeacon warp&&!string.IsNullOrEmpty(warp.requiredFlag)&&!Has(warp.requiredFlag);
                // A party clear of the fight may leave (D198): these seals keep it from walking there.
                if(!inFight&&!storyLocked)continue;
                tried++;
                if(inFight)
                {
                    PromptService.FindInteraction(lead.transform.position);
                    if(PromptService.Interaction==exit)failures.Add($"{exit.name} answers in the middle of {bossId}'s fight");
                }
                yield return Tap(GamepadButton.South);
                if(SceneFlow.Current.Loading){failures.Add($"A beside {exit.name} left {zone} in the middle of {bossId}'s fight");break;}
            }
            yield return Until(()=>!SceneFlow.Current.Loading,25);
            Assert.Greater(tried,0,"fixture: a way out to try");
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
            // The fight over, the seals let go.
            lead.GetComponent<PlayerBrain>().AutoPilot=true;DialogueSystem.Current.AutoAdvance=true;
            var at=boss.transform.position;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.Health.Alive&&Flat(e.transform.position,at)<60))Defeat(enemy);
            yield return new WaitForSecondsRealtime(.3f);
            foreach(var seal in door.membranes)if(seal!=null)Assert.IsTrue(seal.Open,seal.name+" stays shut after "+bossId+" is down");
        }
        [UnityTest] public IEnumerator NoWayOutAnswersInTheBurrowersFight()=>NoWayOut("Sorrel_Ridges","Sorrel_Burrower","Burrower");
        [UnityTest] public IEnumerator NoWayOutAnswersInTheBellowsFight()=>NoWayOut("Hushwell","Hushwell_Bellows","BellowsBelow");
        [UnityTest] public IEnumerator NoWayOutAnswersInTheCantorsFight()=>NoWayOut("Gullet_Tunnel","Gullet_Cantor","Cantor");
        [UnityTest] public IEnumerator NoWayOutAnswersInHessasFight()=>NoWayOut("SunkenRelay","Relay_Hessa","HessaKorl","relay.hessaConfronted");

        // ------------------------------------------------------------------ 4.13: sequence breaks
        void StartTheRoad(int step)
        {
            Flag("lantern.routeKnown");RpgServices.Quests.Start("LanternRoad");
            var goals=new[]{(ObjectiveKind.Reach,"WickReach"),(ObjectiveKind.Reach,"Lanternwick"),(ObjectiveKind.TalkTo,"Aumi")};
            for(int i=0;i<step&&i<goals.Length;i++)RpgServices.Quests.Report(goals[i].Item1,goals[i].Item2);
            Assert.AreEqual(step,Step("LanternRoad"),"fixture: the road's step");
        }
        [UnityTest] public IEnumerator TheOrchardGateWaitsForAumi()
        {
            AddPad();StartTheRoad(1);
            yield return Zone("Lanternwick","Gorge");
            yield return Settle();
            var hero=Hero;var gate=WarpTo("LanternGorge");
            Put(hero,Beside(gate,hero));hero.GetComponent<PlayerBrain>().AutoPilot=false;yield return Frames(2);
            PromptService.FindInteraction(hero.transform.position);Assert.AreSame(gate,PromptService.Interaction,"fixture: the hero stands at the gate");
            yield return Tap(GamepadButton.South);
            Assert.IsFalse(SceneFlow.Current.Loading,"the gate into the gorge opens before anyone has met Aumi");
            StringAssert.Contains("Aumi",gate.prompt,"the shut gate doesn't say who opens it");
            StringAssert.DoesNotContain("orchard gate",Objective().ToLowerInvariant(),"the objective sends the party through a gate that won't open");
            StringAssert.Contains("lantern-keeper",Objective(),"nothing says who to find");
            hero.GetComponent<PlayerBrain>().AutoPilot=true;
            yield return Talk(Person("Aumi"));
            Assert.AreEqual("AumiFirst",heard,"the first talk with Aumi isn't her introduction");
            Assert.AreEqual(3,Step("LanternRoad"),"meeting Aumi doesn't move the road on");
            yield return Settle();
            StringAssert.Contains("orchard gate",Objective().ToLowerInvariant(),"after Aumi the objective doesn't send the party down");
            gate.Interact();Assert.IsTrue(SceneFlow.Current.Loading,"the gate stays shut after Aumi");
            yield return Until(()=>!SceneFlow.Current.Loading,25);
        }
        [UnityTest] public IEnumerator ReachingTheGorgeBeforeAumiStillMakesSense()
        {
            StartTheRoad(2);
            // No player arrives like this: the orchard gate has wanted Aumi, and the relays the road's step 3, in every build since the road opened (6a4f06a). Only a debug warp lands here,
            // and all that's owed then is a way out: climb back, meet her, and the road carries on.
            yield return Zone("LanternGorge");yield return Settle();
            WarpTo("Lanternwick").Interact();
            Assert.IsTrue(SceneFlow.Current.Loading,"a party in the gorge before meeting Aumi can't climb back to her");
            yield return Until(()=>!SceneFlow.Current.Loading,25);yield return Quieted();
            // What keeps a player from standing there at all: the one way down waits for her, and nothing else leads to the gorge.
            var gate=WarpTo("LanternGorge");Assert.IsFalse(All<DockingPad>().Any(d=>d.scene=="LanternGorge"),"a dock into the gorge goes round the gate that waits for Aumi");
            gate.Interact();Assert.IsFalse(SceneFlow.Current.Loading,"the gate opens before anyone has met Aumi: a party can now stand in the gorge with relays that don't answer");
            yield return Talk(Person("Aumi"));
            Assert.AreEqual("AumiFirst",heard,"coming back up, Aumi doesn't introduce herself");Assert.AreEqual(3,Step("LanternRoad"),"meeting her after all doesn't move the road on");
            // And the way a player does arrive: told to relight the relays, every one of them answers.
            yield return Settle();
            gate.Interact();Assert.IsTrue(SceneFlow.Current.Loading,"the gate stays shut after Aumi");
            yield return Until(()=>!SceneFlow.Current.Loading,25);yield return Quieted();yield return Settle();
            var relays=All<DiscoveryPoint>().Where(p=>p.flag.StartsWith("gorge.relay")).ToArray();Assert.AreEqual(3,relays.Length,"fixture: the gorge's relays");
            string told=Objective();StringAssert.Contains("Relight",told,"a party Aumi sent down isn't told to relight the relays");
            Assert.IsTrue(relays.All(r=>r.Available),"a party Aumi sent down is told to relight relays that don't answer, with nothing saying why: "+told);
        }
        [UnityTest] public IEnumerator TheRelayDoorWaitsForAllThreeRelays()
        {
            AddPad();StartTheRoad(2);Flag("met.Aumi");RpgServices.Quests.Report(ObjectiveKind.TalkTo,"Aumi");
            yield return Zone("LanternGorge","Relay");
            var hero=Hero;var door=WarpTo("SunkenRelay");
            Put(hero,Beside(door,hero));hero.GetComponent<PlayerBrain>().AutoPilot=false;yield return Frames(2);
            PromptService.FindInteraction(hero.transform.position);Assert.AreSame(door,PromptService.Interaction,"fixture: the hero stands at the door");
            for(int lit=0;lit<3;lit++)
            {
                yield return Settle();
                StringAssert.Contains($"({lit}/3)",Objective(),"the objective doesn't count the relays");
                yield return Tap(GamepadButton.South);
                Assert.IsFalse(SceneFlow.Current.Loading,$"the relay door opens with {lit} of 3 relays lit");
                StringAssert.Contains("relay",door.prompt.ToLowerInvariant(),"the shut door doesn't say what opens it");
                Flag("gorge.relay"+(lit+1));yield return Frames(2);
            }
            yield return Settle();
            Assert.IsTrue(Has("gorge.relaysLit"),"three relays don't open the door");
            StringAssert.Contains("Sunken Relay",Objective(),"with the door open the objective doesn't point through it");
            yield return new WaitForSecondsRealtime(.5f);
            yield return Tap(GamepadButton.South);
            Assert.IsTrue(SceneFlow.Current.Loading,"the door stays shut after three relays");
            yield return Until(()=>!SceneFlow.Current.Loading,25);
        }
        /// <summary>A party past the relay door with its relays dark can't exist: the door and the relays' step have both been there since the road opened (6a4f06a).
        /// What must hold is that the two never part: the relight that opens the door is the one that moves the count on, so the log can't sit on a step nothing can finish.
        /// From the relays on, the count and the door come from the game's own relays, door and lift, not the reports TheLanternRoadEndsAtTheArchiveWithAChartAndAHook makes by hand.</summary>
        [UnityTest] public IEnumerator ThreeRelaysCarryTheRoadThroughItsDoorToTheChart()
        {
            StartTheRoad(2);Flag("met.Aumi");RpgServices.Quests.Report(ObjectiveKind.TalkTo,"Aumi");
            yield return Zone("LanternGorge");yield return Settle();
            foreach(string flag in new[]{"gorge.relay1","gorge.relay2","gorge.relay3"})
            {
                var relay=Discovery(flag);Assert.IsTrue(relay.Available,flag+" doesn't answer at the road's own step");
                relay.Interact();yield return FinishTalk();
                Assert.IsTrue(Has(flag),flag+" won't relight");
                Assert.AreEqual(Has("gorge.relaysLit"),Step("LanternRoad")>3,$"after {flag} the door and the road's count disagree: door open {Has("gorge.relaysLit")}, step {Step("LanternRoad")}");
            }
            Assert.AreEqual(4,Step("LanternRoad"),"three relays don't finish the road's step");
            Flag("relay.hessaConfronted");
            WarpTo("SunkenRelay").Interact();Assert.IsTrue(SceneFlow.Current.Loading,"the door stays shut after three relays");
            yield return Until(()=>!SceneFlow.Current.Loading,25);yield return Quieted();yield return Settle();
            StringAssert.Contains("Hessa",Objective(),"inside the relay the objective doesn't name who to stop");
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+60;
            Encounter("Relay_Hessa").Begin();yield return Frames(3);
            DialogueSystem.Current.AutoAdvance=true;
            Defeat(All<EnemyBrain>().First(e=>e.definition.id=="HessaKorl"));
            yield return Until(()=>Has("relay.hessaYielded"),12);
            Assert.IsTrue(Has("relay.hessaYielded"),"fixture: she has said her piece");
            yield return FinishTalk();
            WarpTo("Lanternwick").Interact();Assert.IsTrue(SceneFlow.Current.Loading,"the relay lift stays shut after she yields");
            yield return Until(()=>!SceneFlow.Current.Loading,25);yield return Quieted();
            yield return Talk(Person("Aumi"));
            Assert.IsTrue(Has("lanternwick.chartGiven"),"fixture: Aumi hands over the chart");
            // The log must not sit on a step nothing can finish.
            Assert.IsTrue(Has("quest.LanternRoad.complete"),$"the chart is handed over and The Lantern Road stays open for good on step {Step("LanternRoad")}: \"{QuestText.Step(GameCatalog.Find<QuestDef>("LanternRoad"),Step("LanternRoad"),State)}\"");
        }
        [UnityTest] public IEnumerator LanternwickAfterTheChartStillMakesSense()
        {
            foreach(var f in new[]{"lantern.routeKnown","met.Aumi","met.Pell","met.Corra","met.Keeper","sliceComplete","tallow.lineObserved","tallow.driftDiscussed","compact.hailHeard","wick.relay1","wick.relay2","wick.gateOpen","gorge.relay1","gorge.relay2","gorge.relay3","gorge.relaysLit",
                "relay.hessaConfronted","bossdown.HessaKorl","clear.Relay_Hessa","relay.hessaYielded","lanternwick.chartGiven","chapter2.complete","quest.LanternRoad.complete","pell.lampReturned","quest.PellsLamp.complete"})Flag(f);
            State.questSteps["LanternRoad"]=6;State.questSteps["PellsLamp"]=2;
            yield return Zone("Lanternwick");yield return Settle();
            StringAssert.Contains("home",Objective(),"after the chart Lanternwick still has a job for the party: "+Objective());
            yield return Talk(Person("Aumi"));
            Assert.AreEqual("AumiFinal",heard,"after the chart Aumi plays an earlier scene");
            Assert.AreEqual(6,Step("LanternRoad"),"talking to Aumi again moved the finished road");
            yield return Talk(Person("Pell"));
            Assert.AreEqual("PellPost",heard,"Pell, packing to leave, plays an earlier scene");
            yield return Talk(Person("Corra"));
            var shop=ShopPanel;Assert.IsTrue(shop.IsOpen,"after the chart Corra's stock doesn't open");shop.Close();yield return null;
            yield return Talk(Person("Ishen"));
            Assert.AreEqual("IshenFirst",heard,"a first meeting with Ishen after the chart is lost");
            Assert.AreEqual(0,Step("IshensRecord"),"Ishen's request after the chart doesn't start his quest");
            yield return Settle();
            StringAssert.Contains("home",Objective(),"Ishen's late request takes the main objective's place");
            var gate=WarpTo("LanternGorge");Assert.IsTrue(gate.Available,"after the chart the way down is shut");gate.Interact();Assert.IsTrue(SceneFlow.Current.Loading,"after the chart the way down doesn't open");
            yield return Until(()=>!SceneFlow.Current.Loading,25);yield return Quieted();yield return Settle();
            StringAssert.Contains("Lanternwick",Objective(),"after the chart the gorge has a job for the party: "+Objective());
            yield return Zone("SunkenRelay");yield return Settle();
            Assert.IsTrue(Discovery("relay.plate1").visual.activeSelf,"Ishen's late request asks for plates that aren't there");
            StringAssert.Contains("lift",Objective(),"after the chart the relay has a job for the party: "+Objective());
            yield return Zone("WickReach","Lanternwick");yield return Settle();
            StringAssert.Contains("home",Objective().ToLowerInvariant(),"after the chart the Wick Reach doesn't point home");
            var home=WarpTo("TallowApproach");Assert.IsTrue(home.Available,"after the chart the warp home is shut");home.Interact();Assert.IsTrue(SceneFlow.Current.Loading,"after the chart the warp home doesn't go");
            yield return Until(()=>!SceneFlow.Current.Loading,25);
            Assert.AreEqual("TallowApproach",SceneFlow.Current.Zone,"fixture: the warp home arrives");
        }

        // ------------------------------------------------------------------ 4.13: stuck spots and falls
        IEnumerator NoCornerTraps(string zone,int samples)
        {
            yield return Zone(zone);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var volume in All<EncounterVolume>())volume.enabled=false;
            foreach(var point in All<DiscoveryPoint>())point.enabled=false;
            yield return null;
            var hero=Hero;Put(Partner,hero.transform.position);
            var corners=NavMesh.CalculateTriangulation().vertices;Assert.Greater(corners.Length,samples,"fixture: "+zone+" has navigation");
            var compass=new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left};var failures=new List<string>();
            for(int i=0;i<samples;i++)
            {
                var at=corners[i*corners.Length/samples];var into=compass[i%4];
                Put(hero,at+Vector3.up*.1f);
                for(float end=Time.unscaledTime+1;hero.ChangingForm&&Time.unscaledTime<end;)yield return null;
                // Into whatever stands there, as a player leaning on a wall does.
                yield return Walk(hero,into,.25f);
                for(float end=Time.unscaledTime+1;hero.ChangingForm&&Time.unscaledTime<end;)yield return null;
                var pressed=hero.transform.position;
                if(!NavMesh.SamplePosition(pressed,out _,3,NavMesh.AllAreas)){failures.Add($"{zone}: pushing {into} from {at} drops the hero off the world at {pressed}");continue;}
                bool free=false;
                foreach(var way in compass)
                {
                    for(float end=Time.unscaledTime+1;hero.ChangingForm&&Time.unscaledTime<end;)yield return null;
                    var from=hero.transform.position;
                    yield return Walk(hero,way,.15f);
                    if(Flat(from,hero.transform.position)>.1f){free=true;break;}
                }
                if(!free)failures.Add($"{zone}: a hero pushed {into} from {at} is stuck at {pressed}: no direction moves them");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator NoWallFootOrCornerOfLanternwickTrapsAHero()=>NoCornerTraps("Lanternwick",30);
        [UnityTest] public IEnumerator NoWallFootOrCornerOfTheGorgeTrapsAHero()=>NoCornerTraps("LanternGorge",30);
        [UnityTest] public IEnumerator NoWallFootOrCornerOfTheRelayTrapsAHero()=>NoCornerTraps("SunkenRelay",30);
    }
}
