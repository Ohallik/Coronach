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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Batch 9's flow track: what QaPlan*Tests don't already ask of the same fixes. One rule for a press carried across a screen change, the save a conversation
    /// earns, the title's first words, refusals that answer, a line held through a pause, the picker and the Gear list's order, and the old saves. Nearly all of these fail on the build before
    /// the fix; the few that pass there (no save in the proving grounds, nothing saved by a talk cut off, the camera at the south bank) pin what the fix could break.</summary>
    public sealed class QaFlowTests
    {
        Gamepad pad;Keyboard keyboard;string heard,savedBindings;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Slots={"autosave","slot1","slot2","slot3"};
        void Listen(string node,string speaker,System.Action<bool> done)=>heard=node;

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
            ClearSaves();DevLoadout.Apply("starter");heard=null;
            Npc.TalkRequested+=Listen;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Listen;
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Menu;if(menu!=null&&menu.IsOpen)menu.Close();
            var shop=ShopPanel;if(shop!=null&&shop.IsOpen)shop.Close();
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);pad=null;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;
            if(GameInput.Current!=null)GameInput.Current.Asset.RemoveAllBindingOverrides();
            if(string.IsNullOrEmpty(savedBindings))PlayerPrefs.DeleteKey("keyboardBindings");else PlayerPrefs.SetString("keyboardBindings",savedBindings);
            PromptService.ForceDevice(PromptDevice.Keyboard);PromptService.ForceDevice(null);InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null){GameServices.Current.Saves.ResumeFrom("autosave");ClearSaves();}
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
        static DialoguePanel Panel=>Object.FindFirstObjectByType<DialoguePanel>();
        static CombatActor Hero=>PartyController.Current.Active;
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        static TechPartDef Part(string id)=>GameCatalog.Find<TechPartDef>(id);
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static Npc Person(string speaker)=>All<Npc>().Single(n=>n.speaker==speaker);
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
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
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            hero.transform.position=at;body.enabled=true;Physics.SyncTransforms();
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
            var shop=ShopPanel;if(shop!=null&&shop.IsOpen){shop.Close();yield return null;}
        }
        static void RoadToAumi()
        {
            Flag("compact.hailHeard");Flag("lantern.routeKnown");var quests=RpgServices.Quests;quests.Start("LanternRoad");
            quests.Report(ObjectiveKind.Reach,"WickReach");quests.Report(ObjectiveKind.Reach,"Lanternwick");
            Flag("met.Aumi");quests.Report(ObjectiveKind.TalkTo,"Aumi");
        }
        // ------------------------------------------------------------------ what's on screen
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static TMP_Text Hud(string name)
        {
            var canvas=All<Canvas>().First(c=>c.name=="HUD");var text=canvas.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name==name);
            Assert.IsNotNull(text,"the HUD has no "+name);return text;
        }
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
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void WriteOld(string slot,string json)
        {
            string path=Saves.PathForSlot(slot);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,json.Replace('\'','"'));
        }

        // ------------------------------------------------------------------ pad and keys
        void AddPad(){if(pad==null||!pad.added)pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();}
        IEnumerator Hold(GamepadState state){InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;}
        IEnumerator Tap(GamepadButton button){yield return Hold(new GamepadState().WithButton(button));yield return Hold(new GamepadState());yield return null;}
        IEnumerator KeyTap(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        sealed class FixturePrompt:InteractionPrompt{public int Uses;public override void Interact(){Uses++;}}

        // ------------------------------------------------------------------ item 1: one rule for whatever hands the world back
        [UnityTest] public IEnumerator AThumbOffAShopsBackRowDoesNotStartTheTalkAgain()
        {
            AddPad();yield return Zone("Hub_Decks");
            var hero=Hero;hero.GetComponent<PlayerBrain>().AutoPilot=false;
            var prompt=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();prompt.range=6;prompt.transform.position=hero.transform.position;yield return Frames(2);
            var shop=ShopPanel;shop.Open(null);yield return new WaitForSecondsRealtime(.4f);
            EventSystem.current.SetSelectedGameObject(Row("ShopBack").gameObject);yield return null;
            yield return Tap(GamepadButton.South);
            Assert.IsFalse(shop.IsOpen,"fixture: A on Back closes the shop");
            yield return Tap(GamepadButton.South);
            Assert.AreEqual(0,prompt.Uses,"the next tap after A closed the shop goes straight to what stands beside the counter: the same conversation starts over");
            yield return new WaitForSecondsRealtime(.4f);
            yield return Tap(GamepadButton.South);
            Assert.AreEqual(1,prompt.Uses,"half a second after the shop closed, A still won't answer what's beside the hero");
            Object.Destroy(prompt.gameObject);
        }
        [UnityTest] public IEnumerator ThePauseMenuHandsTheWorldBackTheSameWay()
        {
            AddPad();yield return Zone("Hub_Decks");
            var hero=Hero;hero.GetComponent<PlayerBrain>().AutoPilot=false;
            var prompt=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();prompt.range=6;prompt.transform.position=hero.transform.position;yield return Frames(2);
            var menu=Menu;menu.Open();yield return Frames(2);
            EventSystem.current.SetSelectedGameObject(Row("Close").gameObject);yield return null;
            yield return Tap(GamepadButton.South);
            Assert.IsFalse(menu.IsOpen,"fixture: A on Back closes the menu");
            yield return Tap(GamepadButton.South);
            Assert.AreEqual(0,prompt.Uses,"a thumb still tapping off the menu's Back goes through the door the hero was standing at");
            Object.Destroy(prompt.gameObject);
        }

        // ------------------------------------------------------------------ item 2: the first press at a choice
        [UnityTest] public IEnumerator AKeyPressAtAChoiceWaitsAsAPadPressDoes()
        {
            keyboard=InputSystem.AddDevice<Keyboard>();yield return Zone("Hub_Decks");
            var panel=Panel;
            panel.Show("Hal",PortraitEmotion.Neutral,null,"Repair and save, or just visiting?");panel.SetVisibleCharacters(int.MaxValue);yield return Frames(2);
            int picked=-1;panel.Options(new List<string>{"Repair and save.","Just visiting."},i=>picked=i);yield return Frames(3);
            yield return KeyTap(Key.E);
            Assert.AreEqual(-1,picked,"E answers a choice nobody has had time to read, where A waits");
            yield return new WaitForSecondsRealtime(.35f);
            Assert.IsTrue(EventSystem.current.sendNavigationEvents,"the hold never lets go: the pad stays deaf at the choice");
            yield return KeyTap(Key.E);
            Assert.AreEqual(0,picked,"a moment later E doesn't answer the choice");
            // A conversation cut off in the middle of the hold leaves nothing deaf behind it.
            panel.Options(new List<string>{"Repair and save.","Just visiting."},i=>picked=i);yield return null;
            Assert.IsFalse(EventSystem.current.sendNavigationEvents,"fixture: the hold is on");
            panel.ClearOptions();
            Assert.IsTrue(EventSystem.current.sendNavigationEvents,"a choice gone in the middle of its hold leaves every menu after it deaf");
            panel.Hide();
        }

        // ------------------------------------------------------------------ item 3: the save a conversation earns
        [UnityTest] public IEnumerator AQuestFinishedInAConversationSurvivesAQuitAndPaysOnce()
        {
            Flag("met.Ishen");Flag("met.Aumi");State.questSteps["IshensRecord"]=1;
            yield return Zone("Lanternwick");
            yield return Talk(Person("Ishen"));Assert.AreEqual("IshenRecordReturn",heard,"fixture: Ishen takes the plates");
            Assert.IsTrue(Has("quest.IshensRecord.complete"),"fixture: the quest completes");
            int paid=State.scrip,lenses=State.parts.Count(p=>p.definitionId=="EmittersT2");
            var saved=Saves.Load("autosave");
            Assert.IsTrue(saved!=null&&saved.flags.TryGetValue("quest.IshensRecord.complete",out bool done)&&done,"the screen says QUEST COMPLETE, and a quit now loses it and its reward");
            Assert.AreEqual(paid,saved.scrip,"the save doesn't hold what the quest paid");
            // Continue from that save: nothing is handed over a second time.
            GameServices.Current.State=Saves.Load("autosave");Saves.ResumeFrom("autosave");
            yield return Zone("Lanternwick");
            Assert.AreEqual(paid,State.scrip,"Continue pays the quest again");Assert.AreEqual(lenses,State.parts.Count(p=>p.definitionId=="EmittersT2"),"Continue hands over the emitter again");
            yield return Talk(Person("Ishen"));Assert.AreEqual("IshenPost",heard,"Ishen takes the plates a second time");
            Assert.AreEqual(paid,State.scrip,"talking to Ishen after a Continue pays the quest again");
        }
        [UnityTest] public IEnumerator TheSaveWaitsOutAFightAndThenComes()
        {
            yield return Zone("Sorrel_Ridges");
            foreach(var volume in All<EncounterVolume>())volume.enabled=false;
            var hero=Hero;Put(hero,new Vector3(0,0,32));foreach(var member in PartyController.Current.members)member.Health.InvulnerableUntil=GameTime.Now+120;
            yield return new WaitForSecondsRealtime(.5f);
            var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),hero.transform.position+Vector3.forward*3);hound.Passive=false;hound.Health.Damageable=false;yield return null;
            Assert.IsTrue(DialogueSystem.Current.StartNode("AbriLanternHome","Keeper"),"fixture: a conversation that ends the chapter");
            yield return FinishTalk();yield return Frames(3);
            bool Kept(){var saved=Saves.Load("autosave");return saved!=null&&saved.flags.TryGetValue("chapter2.homecoming",out bool ended)&&ended;}
            Assert.IsTrue(Has("chapter2.homecoming"),"fixture: the chapter's flag is up");
            Assert.IsFalse(Kept(),"with a hound on the hero the game autosaves in the middle of the fight");
            Object.Destroy(hound.gameObject);yield return Until(Kept,2);
            Assert.IsTrue(Kept(),"the fight is over and the save the conversation earned never comes");
        }
        [UnityTest] public IEnumerator AConversationCutOffSavesNothing()
        {
            yield return Zone("TallowDrift");
            var bytes=File.ReadAllBytes(Saves.PathForSlot("autosave"));
            Assert.IsTrue(DialogueSystem.Current.StartNode("AbriLanternHome","Keeper"),"fixture: the homecoming starts");yield return Frames(3);
            DialogueSystem.Current.Cancel();yield return new WaitForSecondsRealtime(.3f);
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(Saves.PathForSlot("autosave")),"a conversation that never finished wrote the autosave");
        }
        [UnityTest] public IEnumerator TheProvingGroundsWriteNoAutosave()
        {
            if(!Application.isBatchMode)Assert.Ignore("needs the batch runner's empty save folder");
            yield return Zone("Arena_Ground");
            Assert.IsFalse(File.Exists(Saves.PathForSlot("autosave")),"fixture: no save yet");
            Assert.IsTrue(DialogueSystem.Current.StartNode("AbriLanternHome","Keeper"),"fixture: a conversation that ends the chapter");
            yield return FinishTalk();yield return Frames(3);
            Assert.IsFalse(File.Exists(Saves.PathForSlot("autosave")),"a conversation in the proving grounds wrote an autosave, and Continue would land there");
        }

        // ------------------------------------------------------------------ item 5: the title's first words
        [UnityTest] public IEnumerator TheTitleSpeaksPadWhenAPadIsConnectedAndKeepsWhatAKeyTakes()
        {
            yield return Zone("Arena_Ground");
            PromptService.Reset();AddPad();
            yield return ToTitle();
            Assert.AreEqual(PromptDevice.Gamepad,PromptService.Device,"a pad is connected and nothing has been pressed, and the first screen of a pad-first game talks keyboard");
            var title=Object.FindFirstObjectByType<TitleScreen>();var hint=title.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="Hint");
            StringAssert.Contains(PromptService.SubmitTagFor(PromptDevice.Gamepad),hint.text,"the title's hint doesn't name the pad's button");
            keyboard=InputSystem.AddDevice<Keyboard>();yield return KeyTap(Key.J);yield return Frames(2);
            Assert.AreEqual(PromptDevice.Keyboard,PromptService.Device,"a key at the title doesn't take the prompts back");
            yield return Zone("Arena_Ground");yield return ToTitle();
            Assert.AreEqual(PromptDevice.Keyboard,PromptService.Device,"sent back to the title, a keyboard player with a pad plugged in is shown pad buttons again");
        }

        // ------------------------------------------------------------------ item 6: swap in the first fight
        [UnityTest] public IEnumerator TheSwapLessonComesOnceBetweenTheOtherTwoInThePartnersVoice()
        {
            foreach(var who in new[]{"Taren","Sela"})
            {
                string line=BarkService.Line(who,"teach.swap");Assert.IsNotNull(line,who+" has no swap lesson");
                StringAssert.IsMatch("(?i)swap",line,who+"'s lesson doesn't name the button's word");
                Assert.IsFalse(Regex.IsMatch(line,"[;—]"),who+"'s lesson breaks the script's punctuation rule: "+line);
            }
            yield return Zone("Sorrel_Ridges");
            var party=PartyController.Current;var hero=party.Active;var partner=party.members[1-party.index];
            Put(hero,new Vector3(0,0,32));foreach(var member in party.members)member.Health.InvulnerableUntil=GameTime.Now+200;
            yield return new WaitForSecondsRealtime(.5f);
            var said=new List<string>();float dodgeAt=-1,swapAt=-1;
            void Heard(string who,string line)
            {
                said.Add(who+": "+line);
                if(line==BarkService.Line(who,"teach.flash"))dodgeAt=Time.unscaledTime;else if(line==BarkService.Line(who,"teach.swap"))swapAt=Time.unscaledTime;
            }
            BarkService.Spoken+=Heard;
            try
            {
                for(int i=0;i<4;i++){var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),hero.transform.position+Quaternion.Euler(0,i*90,0)*Vector3.forward*3.5f);hound.Passive=false;hound.Health.Damageable=false;}
                yield return Until(()=>Has("taught.swap"),25);
                Assert.IsTrue(Has("taught.swap"),"marks go by and nobody says swapping exists");
                Assert.IsTrue(Has("taught.flash"),"the swap lesson came before the dodge");Assert.IsFalse(Has("taught.guard"),"the swap and guard lessons arrive together");
                Assert.AreEqual(1,said.Count(l=>l==partner.character+": "+BarkService.Line(partner.character,"teach.swap")),"the partner doesn't say the swap lesson once: "+string.Join(" | ",said));
                Assert.GreaterOrEqual(swapAt-dodgeAt,3.5f,"four hounds mark quickly, and the swap lesson comes over the dodge lesson before its 3.5 s are up");
            }
            finally{BarkService.Spoken-=Heard;}
        }
        [UnityTest] public IEnumerator TheFightStripsNameSwapOnlyWithSomeoneToSwapTo()
        {
            // Not the proving grounds: they add Sela to any party that walks in (ArenaRuntime), so nobody can stand alone there.
            GameServices.Current.State=new GameState();yield return Zone("Sorrel_Ridges");
            Assert.AreEqual(1,PartyController.Current.members.Length,"fixture: alone");
            foreach(var volume in All<EncounterVolume>())volume.enabled=false;
            Put(Hero,new Vector3(0,0,32));yield return new WaitForSecondsRealtime(.5f);
            StringAssert.Contains(PromptService.Tag("Attack")+"  ATTACK",Hud("Skills").text,"fixture: on open ground the strip is the fight's");
            StringAssert.DoesNotContain("SWAP",Hud("Skills").text,"the fight strip offers a swap with nobody to swap with");
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");yield return Frames(2);
            StringAssert.Contains(PromptService.Tag("Swap")+"  SWAP",Hud("Skills").text,"the fight strip doesn't name the swap button");
            yield return Zone("Arena_Flight");yield return Frames(2);
            StringAssert.Contains(PromptService.Tag("Swap")+"  SWAP",Hud("Skills").text,"the flight strip doesn't name the swap button");
        }

        // ------------------------------------------------------------------ item 7: refusals that answer
        [UnityTest] public IEnumerator PressingAMaterialRowAnswers()
        {
            AddPad();yield return Zone("Arena_Ground");
            State.materials["HuskCore"]=1;
            var menu=Menu;menu.Open(PauseMenu.ItemsPage);yield return null;
            // Husk Core was the material with no use until November 10 (prog158); it's the Husk Frame's now, and refused the same way.
            foreach(var (name,says) in new[]{("Scrap Alloy","fabricat"),("Husk Core","fabricat")})
            {
                var row=With(name);string place=row.name;EventSystem.current.SetSelectedGameObject(row.gameObject);yield return null;
                using(var log=new CueLog())
                {
                    yield return Tap(GamepadButton.South);
                    Assert.Greater(log.Count("ui_error"),0,$"A on {name} does nothing and says nothing: {log.All}");
                    Assert.AreEqual(0,log.Count("ui_confirm"),$"a refused press on {name} sounds accepted");
                }
                StringAssert.Contains(name,Feedback(),"the refusal doesn't say what was refused");StringAssert.Contains(says,Feedback(),$"the refusal for {name} doesn't say why");
                Assert.AreEqual(place,Selected().name,"a refused press moved the cursor");
            }
            menu.Close();
        }
        [UnityTest] public IEnumerator ARefusedSwapFlashesThePartnerLineAndAHeroAloneStaysSilent()
        {
            AddPad();yield return Zone("Arena_Ground");
            // Open floor far from the proving ground's walls, and too far apart for the lead to revive the partner by standing there.
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Plan fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            var party=PartyController.Current;var lead=party.Active;var other=party.members[1-party.index];
            Put(lead,new Vector3(200,0,0));Put(other,new Vector3(210,0,0));lead.GetComponent<PlayerBrain>().AutoPilot=false;
            other.Health.Receive(new DamagePacket{amount=other.Health.maximum*10,type=DamageType.Pulse});yield return Frames(2);
            StringAssert.DoesNotContain("<color=",Hud("Partner").text,"fixture: the line starts plain");
            yield return Hold(new GamepadState().WithButton(GamepadButton.North));yield return Frames(1);
            StringAssert.Contains("<color=",Hud("Partner").text,"a refused swap doesn't flash the partner's line, as a refused roll flashes the Thrust bar");
            yield return Hold(new GamepadState());yield return new WaitForSecondsRealtime(.4f);
            StringAssert.DoesNotContain("<color=",Hud("Partner").text,"the partner's line stays red");
            // Not the proving grounds: they add Sela to any party that walks in (ArenaRuntime), and the press would simply swap.
            GameServices.Current.State=new GameState();yield return Zone("Sorrel_Ridges");
            Assert.AreEqual(1,PartyController.Current.members.Length,"fixture: alone");
            Hero.GetComponent<PlayerBrain>().AutoPilot=false;
            using var log=new CueLog();
            yield return Tap(GamepadButton.North);
            Assert.AreEqual(0,log.Count("ui_error"),"a hero alone pressing swap gets a refusal, for a swap nothing on screen offers");
        }

        // ------------------------------------------------------------------ item 8: words that keep up
        [UnityTest] public IEnumerator AumiNoticesTheRelaysBurning()
        {
            RoadToAumi();yield return Zone("Lanternwick");
            yield return Talk(Person("Aumi"));Assert.AreEqual("AumiRepeat",heard,"fixture: with the relays dark she asks for them");
            foreach(var f in new[]{"gorge.relay1","gorge.relay2","gorge.relay3","gorge.relaysLit"})Flag(f);
            yield return Talk(Person("Aumi"));Assert.AreEqual("AumiRelaysLit",heard,"all three relays burn and Aumi still asks for them");
            Assert.IsFalse(Has("lanternwick.chartGiven"),"Aumi hands over the chart before the captain has been dealt with");
            yield return new WaitForSecondsRealtime(.35f);
            StringAssert.Contains("Sunken Relay",Objective(),"with the relays lit the objective doesn't send the party on to the relay");
            Flag("bossdown.HessaKorl");
            yield return Talk(Person("Aumi"));Assert.AreEqual("AumiReturn",heard,"the relays' tier gets in the way of the chart");
        }
        [UnityTest] public IEnumerator MiraThanksThePartyOnceAndGoesBackToHerPatter()
        {
            Flag("met.Mira");Flag("quest.MiraCrystals.complete");
            yield return Zone("Hub_Decks");
            yield return Talk(Person("Mira"));Assert.AreEqual("MiraPost",heard,"fixture: Mira notices the crystals");
            yield return new WaitForSecondsRealtime(.4f);
            yield return Talk(Person("Mira"));Assert.AreEqual("MiraRepeat",heard,"Mira exclaims over the same six crystals on every visit");
        }
        [UnityTest] public IEnumerator NeveAndIloWaitForWhatTheirLinesTalkAbout()
        {
            foreach(var f in new[]{"met.Neve","met.Ilo","warpkey","quest.NeveParcel.complete","neve.afterParcel"})Flag(f);State.questSteps["NeveParcel"]=2;
            yield return Zone("Hub_CinderHalo");
            yield return Talk(Person("Neve"));Assert.AreEqual("NeveRepeat",heard,"with the key and no nursery Neve sends the party to a chart that doesn't exist, and a warp that refuses them");
            yield return Talk(Person("Ilo"));Assert.AreEqual("IloRepeat",heard,"Ilo asks about the Gullet of a party that has only been to Sorrel");
            Flag("hushwell.nursery");yield return Talk(Person("Neve"));Assert.AreEqual("NevePost",heard,"with the chart in hand Neve doesn't send the party on");
            Flag("bossdown.Cantor");yield return Talk(Person("Ilo"));Assert.AreEqual("IloPost",heard,"back from the Gullet, Ilo doesn't ask about it");
        }
        [UnityTest] public IEnumerator TheCourierDockIsCalledOneThing()
        {
            yield return Zone("Lanternwick");
            var step=GameCatalog.Find<QuestDef>("LanternRoad").stepText[1];
            StringAssert.Contains("courier dock",step,"the log and the sign call it different things");StringAssert.DoesNotContain("courier pad",step);
            Assert.IsTrue(All<TMP_Text>().Any(t=>t.text=="COURIER DOCK"),"fixture: the sign on the square");
        }
        [UnityTest] public IEnumerator AWarpWithNoLockCarriesNoLockedWords()
        {
            yield return Zone("Hushwell");
            foreach(var warp in All<WarpBeacon>().Where(w=>string.IsNullOrEmpty(w.requiredFlag)))
                StringAssert.DoesNotMatch("(?i)sorrel key",warp.lockedPrompt??"",warp.prompt+" still holds the retired term");
            yield return Zone("WickReach");
            var home=All<WarpBeacon>().Single(w=>w.scene=="TallowApproach");
            Assert.IsTrue(string.IsNullOrEmpty(home.requiredFlag),"the Reach's way home is locked by a flag nobody can stand here without: its words can only ever be wrong");
            Assert.IsTrue(string.IsNullOrEmpty(home.lockedPrompt),"the Reach's way home has no lock and still carries the field's default words about the survey key");
        }
        [UnityTest] public IEnumerator PellsLampStandsOnTheStairOnceSheHasIt()
        {
            yield return Zone("Lanternwick");
            var lamp=All<Transform>().Single(t=>t.name=="Pell's lamp");
            Assert.IsFalse(lamp.gameObject.activeInHierarchy,"the lamp is on the stair before anyone has found it");
            Flag("pell.lampReturned");yield return Frames(2);
            Assert.IsTrue(lamp.gameObject.activeInHierarchy,"'It's on the stair, where she kept it', and nothing is");
            var aumi=Person("Aumi").transform.position;var at=lamp.position;at.y=aumi.y=0;
            Assert.Less(Vector3.Distance(at,aumi),6,"the lamp isn't anywhere near the stair's keeper");
            Flag("pell.aboard");yield return Frames(2);
            Assert.IsFalse(lamp.gameObject.activeInHierarchy,"Pell's lamp stays on the stair after she has taken it to Tallow");
        }

        // ------------------------------------------------------------------ item 9: a line held through a pause
        [UnityTest] public IEnumerator ALineOnScreenComesBackAfterAPauseWithItsTimeLeft()
        {
            AddPad();yield return Zone("Arena_Ground");
            BarkService.Play("Taren","teach.flash",true);yield return Frames(2);
            string line=BarkService.Line("Taren","teach.flash");var bark=Text("Bark");var surface=All<Canvas>().First(c=>c.name=="Barks");
            StringAssert.Contains(line,bark.text,"fixture: the line is up");
            var menu=Menu;menu.Open();yield return Frames(2);
            Assert.IsFalse(surface.enabled,"a line stands over the pause menu");
            yield return new WaitForSecondsRealtime(4);
            menu.Close();yield return Frames(2);
            Assert.IsTrue(surface.enabled,"the barks don't come back after a pause");
            StringAssert.Contains(line,bark.text,"a pause wiped the line on screen for good");
            yield return new WaitForSecondsRealtime(3.8f);
            Assert.IsEmpty(bark.text,"a held line never leaves");
        }
        [UnityTest] public IEnumerator AHailsNextLineWaitsOutThePauseToo()
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
                yield return new WaitForSecondsRealtime(5);
                yield return Tap(GamepadButton.Start);Assert.IsFalse(Menu.IsOpen,"fixture: the menu closes");
                yield return new WaitForSecondsRealtime(.5f);
                Assert.AreEqual(1,said.Count,"the gap ran on behind the pause: Orrin's next line comes straight over the one still on screen");
                StringAssert.Contains(said[0],Text("Bark").text,"Orrin's warning is gone after a pause");
                yield return Until(()=>said.Count>=2,8);
                Assert.AreEqual(2,said.Count,"the rest of the hail never comes");
            }
            finally{BarkService.Spoken-=Heard;}
        }

        // ------------------------------------------------------------------ item 10: the south mouth
        [UnityTest] public IEnumerator TheCameraStaysBackAtLanternwicksSouthBank()
        {
            yield return Zone("Lanternwick");
            var rig=Object.FindFirstObjectByType<CameraRig>();var lead=Hero;
            foreach(float x in new[]{-25f,0f,25f})
            {
                Put(lead,new Vector3(x,.3f,-32.6f));yield return new WaitForSecondsRealtime(1.5f);
                Assert.Greater(rig.GroundCameraDistance,14,$"at x {x} the bank that stops the party also drags the camera in to {rig.GroundCameraDistance:0.0} m");
                var view=Camera.main.WorldToViewportPoint(lead.transform.position+Vector3.up);
                Assert.IsTrue(view.z>0&&view.x>0&&view.x<1&&view.y>0&&view.y<1,$"at x {x} the hero isn't on screen at the foot of the bank");
            }
        }

        // ------------------------------------------------------------------ item 11: the picker and the Gear list
        [UnityTest] public IEnumerator TheSavePickerOpensOnTheNewestSave()
        {
            yield return Zone("Arena_Ground");
            // A clock's grain apart, so the two dates can't tie.
            Saves.Save("autosave",State);yield return new WaitForSecondsRealtime(.05f);Saves.Save("slot1",State);
            yield return ToTitle();yield return OpenPicker();
            Assert.AreEqual("slot1",Selected().name,"slot 1 is newer than the autosave, and two presses of A load the older save");
            Row("Back").onClick.Invoke();yield return Frames(2);
            Saves.Save("autosave",State);
            yield return OpenPicker();
            Assert.AreEqual("autosave",Selected().name,"the autosave is newest and the picker doesn't open on it");
            Row("Back").onClick.Invoke();yield return Frames(2);
            // Saves too old to carry a date: the first row, as ever.
            foreach(string slot in new[]{"autosave","slot1"})
            {string path=Saves.PathForSlot(slot);File.WriteAllText(path,Regex.Replace(File.ReadAllText(path),"\"savedAtUtc\":\\s*\"[^\"]*\"","\"savedAtUtc\": null"));}
            yield return OpenPicker();
            Assert.AreEqual("autosave",Selected().name,"with no dates to go by the picker doesn't open on its first row");
        }
        [UnityTest] public IEnumerator EveryRouteThroughThePickerNamesItsSave()
        {
            yield return null;
            int routes=0;
            foreach(string file in Directory.GetFiles(Path.Combine(Application.dataPath,"..","..","docs","quality","routes"),"*.json"))
            {
                string text=File.ReadAllText(file);var route=JsonUtility.FromJson<QualityRoute>(text);if(route.steps==null||!route.steps.Any(s=>s.expectedUi=="saves"))continue;
                routes++;
                Assert.IsTrue(Regex.IsMatch(text,"\"pickRow\"\\s*:\\s*\"(autosave|slot[123])\""),Path.GetFileName(file)+" counts presses through a list that opens on whichever save is newest");
                foreach(var step in route.steps.Where(s=>s.expectedUi=="saves"))
                    CollectionAssert.DoesNotContain(step.buttons??new string[0],"DpadDown",Path.GetFileName(file)+": "+step.name+" moves down from a row that isn't fixed any more");
            }
            Assert.Greater(routes,20,"fixture: the routes through the picker");
        }
        [UnityTest] public IEnumerator TheGearListLeadsWithWhatTheHeroWearsAndFocusFollowsThePart()
        {
            yield return Zone("Arena_Ground");
            var inventory=RpgServices.Inventory;var taren=Member("Taren");var sela=Member("Sela");
            var edge=Part("EdgesT1");var refined=Part("EdgesT2");var frame=Part("FramesT1");var module=Part("ModulesT1");
            inventory.Equip(sela,inventory.GivePart(edge),edge);inventory.GivePart(edge);var better=inventory.GivePart(refined);
            inventory.Equip(taren,inventory.GivePart(edge),edge);inventory.Equip(taren,inventory.GivePart(frame),frame);
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return null;
            var rows=Rows();
            StringAssert.StartsWith("Action:",Words(rows[0]),"fixture: the Action row leads");
            StringAssert.StartsWith("Equipped · Scrap Edge",Words(rows[1]),"what the hero wears doesn't lead the list");StringAssert.StartsWith("Equipped · Scrap Frame",Words(rows[2]),"what the hero wears isn't grouped");
            StringAssert.StartsWith("Refined Edge",Words(rows[3]),"the spare worth a look doesn't come next");StringAssert.StartsWith("Scrap Edge",Words(rows[4]),"the spares aren't grouped");
            StringAssert.StartsWith("Sela · Scrap Edge",Words(rows[5]),"the partner's part comes before the spares");
            // On: it moves up, and the cursor goes with it.
            EventSystem.current.SetSelectedGameObject(rows[3].gameObject);yield return null;rows[3].onClick.Invoke();yield return null;
            Assert.AreEqual(better.instanceId,taren.equipped[GearSlot.Edge],"fixture: the refined edge is on");
            StringAssert.StartsWith("Equipped · Refined Edge",Words(Selected().transform),"the part moved up the list and the cursor stayed behind, so the next A lands on another part");
            // Off: down it goes, and the cursor still has it.
            Selected().GetComponent<Button>().onClick.Invoke();yield return null;
            StringAssert.StartsWith("Refined Edge",Words(Selected().transform),"the part moved down the list and the cursor stayed behind");
            // Across a page: the list's last spare, equipped, comes to the first page with the cursor on it.
            for(int i=0;i<7;i++)inventory.GivePart(module);
            menu.Close();yield return null;menu.Open(PauseMenu.GearPage);yield return null;
            Row("Pager").onClick.Invoke();yield return null;
            var far=Rows().First(r=>Words(r).StartsWith("Scrap Module"));EventSystem.current.SetSelectedGameObject(far.gameObject);yield return null;far.onClick.Invoke();yield return null;
            Assert.IsTrue(taren.equipped.ContainsKey(GearSlot.Module),"fixture: the module is on");
            StringAssert.StartsWith("Equipped · Scrap Module",Words(Selected().transform),"the part went to another page and the cursor didn't");
            menu.Close();
        }

        // ------------------------------------------------------------------ item 13: old saves and the defeat panel
        const string HandDamaged="{'version':2,'savedAtUtc':'2026-10-04T23:16:47.1981503Z','zone':'Arena_Ground','spawn':'Arrival','activeMember':0,'quickItem':'RepairGel',"+
            "'party':[{'id':'Taren','level':3,'xp':0,'integrity':156.0,'charge':0.0,'gear':null,'equipped':null,'module2':null}],'flags':{'met.Orrin':true},"+
            "'materials':null,'collectedMaterials':null,'consumables':null,'craftingXp':null,'questSteps':null,'questCounts':null,'parts':null,'scrip':10,'playtime':1.0}";
        const string EmptyHanded="{'version':2,'savedAtUtc':'2026-10-04T23:16:47.1981503Z','zone':'Arena_Ground','spawn':'Arrival','activeMember':0,'quickItem':'RepairGel',"+
            "'party':[{'id':'Taren','level':7,'xp':0,'integrity':228.0,'charge':0.0,'gear':[],'equipped':{},'module2':null},{'id':'Sela','level':7,'xp':0,'integrity':228.0,'charge':0.0,'gear':[],'equipped':{},'module2':null}],"+
            "'flags':{'met.Orrin':true},'materials':{'ScrapAlloy':5},'collectedMaterials':{},'consumables':{'RepairGel':1},'craftingXp':{},'questSteps':{},'questCounts':{},'parts':[],'scrip':10,'playtime':1.0}";
        [UnityTest] public IEnumerator ASaveWithItsListsMissingStillOpensTheTitleAndLoads()
        {
            yield return Zone("Arena_Ground");
            WriteOld("slot1",HandDamaged);
            Assert.DoesNotThrow(()=>Saves.Load("slot1"),"a save with an empty list throws where the title reads it, and the title never comes up");
            yield return ToTitle();
            Assert.IsNotNull(Row("Continue"),"the title has no menu after reading that save");
            yield return OpenPicker();yield return Pick("slot1","Arena_Ground");
            Assert.AreEqual(10,State.scrip,"the save lost what it did hold");Assert.IsNotNull(State.materials,"fixture: the bag is there");
            RpgServices.Inventory.Give("ScrapAlloy",1);Assert.AreEqual(1,State.materials["ScrapAlloy"],"a save with a missing list can't be played on");
        }
        [UnityTest] public IEnumerator AnEmptyHandedOldSaveComesBackArmedOnce()
        {
            yield return Zone("Arena_Ground");
            WriteOld("slot1",EmptyHanded);
            yield return ToTitle();yield return OpenPicker();yield return Pick("slot1","Arena_Ground");
            Assert.IsTrue(Member("Taren").equipped.ContainsKey(GearSlot.Edge),"an old save reaches its next fight with Taren empty-handed");
            Assert.IsTrue(Member("Sela").equipped.ContainsKey(GearSlot.Emitter),"an old save reaches its next fight with Sela empty-handed");
            int parts=State.parts.Count;Assert.Greater(parts,0,"fixture: the parts are in Gear");
            Saves.Save("slot1",State);
            yield return ToTitle();yield return OpenPicker();yield return Pick("slot1","Arena_Ground");
            Assert.AreEqual(parts,State.parts.Count,"every Continue hands an old save another set of parts");
            // A hero who has taken everything off has only taken it off.
            foreach(var part in State.parts.ToArray())RpgServices.Inventory.Unequip(part);
            Saves.Save("slot1",State);
            yield return ToTitle();yield return OpenPicker();yield return Pick("slot1","Arena_Ground");
            Assert.AreEqual(parts,State.parts.Count,"parts left in Gear don't stop the free set");Assert.IsFalse(Member("Taren").equipped.ContainsKey(GearSlot.Edge),"a part taken off goes back on by itself on Continue");
        }
        [UnityTest] public IEnumerator ASecondPressAtTheDefeatPanelChangesNothing()
        {
            yield return Zone("Arena_Ground");
            Saves.Save("autosave",State);
            foreach(var hero in PartyController.Current.members)hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*100,type=DamageType.Pulse});
            var panel=Object.FindFirstObjectByType<DefeatPanel>();yield return Until(()=>panel.IsOpen,6);
            Assert.IsTrue(panel.IsOpen,"fixture: the defeat panel");
            var retry=Row("Retry");var title=Row("Title");
            title.onClick.Invoke();
            Assert.IsTrue(SceneFlow.Current.Loading,"fixture: the panel starts a load");var state=State;
            title.onClick.Invoke();
            Assert.IsTrue(GameInput.Current.Blocked,"a second press of Return to title hands the controls back in the middle of the load");
            retry.onClick.Invoke();
            Assert.AreSame(state,State,"a press of Retry in the fade reads the save and replaces the game under the load");
            yield return Until(()=>!SceneFlow.Current.Loading,15);
            Assert.IsNotNull(Object.FindFirstObjectByType<TitleScreen>(),"the load the first press asked for didn't land");
        }
    }
}
