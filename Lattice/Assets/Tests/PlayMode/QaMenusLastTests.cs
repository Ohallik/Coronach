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
    /// <summary>Batch 11's menus track: what the final audit check still found. What an upgrade buys before it's paid for, and the stats no page named (ui226). A thumb still tapping past the
    /// hand-back hold (audi176). The partner's skills without a swap (skil209). These fail on the build before the fix, but for three guards that pin what the fixes could break: a slower tap still
    /// gets through, as the routes' pulsed presses need; a tap doesn't cut a longer hold short; and a hero alone has no partner's page.</summary>
    public sealed class QaMenusLastTests
    {
        Gamepad pad;string savedBindings;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Slots={"autosave","slot1","slot2","slot3"};

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
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Menu;if(menu!=null&&menu.IsOpen)menu.Close();
            var shop=ShopPanel;if(shop!=null&&shop.IsOpen)shop.Close();
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);pad=null;
            if(GameInput.Current!=null)GameInput.Current.Asset.RemoveAllBindingOverrides();
            if(string.IsNullOrEmpty(savedBindings))PlayerPrefs.DeleteKey("keyboardBindings");else PlayerPrefs.SetString("keyboardBindings",savedBindings);
            PromptService.HoldOffInteract(0);PromptService.ForceDevice(PromptDevice.Keyboard);PromptService.ForceDevice(null);InputSystem.settings.backgroundBehavior=priorBackground;
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
            var saves=GameServices.Current.Saves;
            foreach(var slot in Slots)foreach(var suffix in new[]{"",".bak",".tmp"})
            {var path=saves.PathForSlot(slot)+suffix;if(File.Exists(path))File.Delete(path);}
            saves.ResumeFrom("autosave");
        }

        // ------------------------------------------------------------------ the world
        static GameState State=>GameServices.Current.State;
        static PauseMenu Menu=>Object.FindFirstObjectByType<PauseMenu>();
        static ShopUi ShopPanel=>Object.FindFirstObjectByType<ShopUi>();
        static CombatActor Hero=>PartyController.Current.Active;
        static CombatActor Body(string id)=>PartyController.Current.members.First(m=>m.character==id);
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        static TechPartDef Part(string id)=>GameCatalog.Find<TechPartDef>(id);
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Zone(string zone)
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone);deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            // Past the load's own hand-back hold.
            yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
        }
        sealed class FixturePrompt:InteractionPrompt{public int Uses;public override void Interact(){Uses++;}}
        static FixturePrompt PromptBeside(CombatActor hero)
        {var prompt=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();prompt.range=6;prompt.transform.position=hero.transform.position;return prompt;}

        // ------------------------------------------------------------------ what's on screen
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
        static TMP_Text Label(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static string Header(){var t=Label("Header");Assert.IsNotNull(t,"the page has no header");return t.text;}
        static string Note(){var t=Label("Note");Assert.IsNotNull(t,"the page has no note");return t.text;}
        static string Feedback(){var t=Label("Feedback");return t!=null?t.text:"";}
        static IEnumerator Action(int presses){for(int i=0;i<presses;i++){Row("Row_1").onClick.Invoke();yield return null;}}

        // ------------------------------------------------------------------ pad
        void AddPad(){if(pad==null||!pad.added)pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();}
        IEnumerator Hold(GamepadState state){InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;}
        IEnumerator Tap(GamepadButton button){yield return Hold(new GamepadState().WithButton(button));yield return Hold(new GamepadState());yield return null;}

        // ------------------------------------------------------------------ ui226: what an upgrade buys
        /// <summary>Upgrade mode showed only what a part has now, so what the Scrap Alloy bought was seen after paying. What the row promises has to be what the upgrade pays.</summary>
        [UnityTest] public IEnumerator AnUpgradeRowSaysWhatTheNextLevelBuysBeforeItIsPaidFor()
        {
            yield return Zone("Arena_Ground");
            var inventory=RpgServices.Inventory;var def=Part("EdgesT2");var edge=inventory.GivePart(def);inventory.GivePart(Part("ModulesT1"));
            State.materials["ScrapAlloy"]=200;
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return null;
            yield return Action(2);StringAssert.Contains("Action: Upgrade",Words(Row("Row_1")),"fixture: the Action row reads Upgrade");
            StringAssert.Contains("Refined Edge  T2 +0  ·  Output +30 to +32.4",Words(With("Refined Edge")),"the row shows only what the part has now, so what the upgrade buys is seen after paying");
            // Rounded, this one read "+3 to +3": a quarter of a point, and it looked like paying for nothing.
            StringAssert.Contains("Resonance +5 to +5.4, Fortune +3 to +3.2",Words(With("Scrap Module")),"each stat the part gives has to say where it goes");
            for(int level=0;level<5;level++)
            {
                var promise=Regex.Match(Words(With("Refined Edge")),@"T2 \+"+level+@"  ·  Output \+([\d.]+) to \+([\d.]+)");
                Assert.IsTrue(promise.Success,$"at +{level} the row doesn't say where Output goes: "+Words(With("Refined Edge")));
                int alloy=inventory.Count("ScrapAlloy");
                With("Refined Edge").onClick.Invoke();yield return null;
                Assert.AreEqual(level+1,edge.upgrade,"fixture: the press pays for the upgrade");Assert.Less(inventory.Count("ScrapAlloy"),alloy,"fixture: the upgrade costs Scrap Alloy");
                float paid=Levels.PartStats(def,edge).output;
                Assert.AreEqual(float.Parse(promise.Groups[2].Value,System.Globalization.CultureInfo.InvariantCulture),paid,.051f,$"the row promised {promise.Groups[2].Value} and the upgrade paid {paid}");
                StringAssert.Contains($"Upgraded Refined Edge to +{edge.upgrade}: Output +{promise.Groups[2].Value}.",Feedback(),"the line under the row doesn't give the number the row just promised");
                if(edge.upgrade<5)StringAssert.Contains($"T2 +{edge.upgrade}  ·  Output +{promise.Groups[2].Value} to +",Words(With("Refined Edge")),"what it promised isn't what the row starts from once it's paid");
            }
            // The top: nothing further to buy, and the row says so before a press does.
            string top=Words(With("Refined Edge"));
            StringAssert.DoesNotContain(" to +",top,"a part at +5 still offers a next level");
            StringAssert.Contains("Output +42",top);StringAssert.Contains("fully upgraded",top,"nothing on the row says a part at +5 can't go further");
            int held=inventory.Count("ScrapAlloy");With("Refined Edge").onClick.Invoke();yield return null;
            Assert.AreEqual(held,inventory.Count("ScrapAlloy"),"a part at +5 charged for nothing");StringAssert.Contains("already at +5",Feedback());
            menu.Close();yield return null;
            // What an upgrade can't move stands as it is: an affix's bonus isn't scaled.
            var resonant=inventory.GivePart(def);resonant.affixes.Add("Resonant");
            menu.Open(PauseMenu.GearPage);yield return null;yield return Action(2);
            StringAssert.Contains("Refined Edge  T2 +0  ·  Resonant  ·  Output +30 to +32.4, Resonance +4",Words(With("Resonant")),"an affix's stat reads as if the upgrade raised it");
            menu.Close();yield return null;
            // The other modes keep their whole numbers and their comparison.
            menu.Open(PauseMenu.GearPage);yield return null;
            StringAssert.Contains("Refined Edge  T2 +5  ·  Output +42",Words(With("T2 +5")));
            StringAssert.DoesNotContain(" to +",string.Join(" ",Rows().Select(r=>Words(r))),"the next level's value leaks into Equip mode");
            menu.Close();
        }
        /// <summary>Now and next share a row. The widest is a worn two-stat module with an affix, and only the type floor holds it on one line.</summary>
        [UnityTest] public IEnumerator TheLongestUpgradeRowsStayOnOneLineAtTheTypeFloor()
        {
            yield return Zone("Arena_Ground");
            var inventory=RpgServices.Inventory;var failures=new List<string>();
            foreach(string kind in new[]{"Emitters","Edges","Frames","Drives","Modules"})
            {
                var refined=Part(kind+"T2");var mine=inventory.GivePart(refined);mine.affixes.Add("Resonant");Assert.IsTrue(inventory.Equip(Member("Taren"),mine,refined),"fixture: "+kind+"T2 goes on Taren");
                var scrap=Part(kind+"T1");var hers=inventory.GivePart(scrap);hers.affixes.Add("Resonant");Assert.IsTrue(inventory.Equip(Member("Sela"),hers,scrap),"fixture: "+kind+"T1 goes on Sela");
            }
            void Measure(string surface,string says)
            {
                foreach(var row in Rows())
                {
                    var text=row.GetComponentInChildren<TMP_Text>();
                    if(text.fontSize<24)failures.Add($"{surface}: \"{text.text}\" is under the type floor, at {text.fontSize}");
                    float room=text.rectTransform.rect.width-text.margin.x-text.margin.z;
                    float one=text.GetPreferredValues("Output",room,0).y,need=text.GetPreferredValues(text.text,room,0).y;
                    if(need>one*1.5f)failures.Add($"{surface}: \"{text.text}\" takes a second line");
                    if(row.name!="Row_1"&&!text.text.Contains(says))failures.Add($"{surface}: \"{text.text}\" doesn't say \"{says}\"");
                }
            }
            var menu=Menu;
            for(int level=0;level<=5;level++)
            {
                foreach(var part in State.parts)part.upgrade=level;
                string says=level<5?" to +":"fully upgraded";
                menu.Open(PauseMenu.GearPage);yield return null;yield return Action(2);
                Measure("at +"+level+", first page",says);
                Row("Pager").onClick.Invoke();yield return null;
                Measure("at +"+level+", second page",says);
                menu.Close();yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        /// <summary>The Party page totalled only Output and Plating: a hero's other three stats were on no screen. They have to be the numbers the fight reads, which take a weapon's affix from the Edge alone.</summary>
        [UnityTest] public IEnumerator ThePartyPageNamesEachHerosResponseResonanceAndFortune()
        {
            yield return Zone("Arena_Ground");
            var inventory=RpgServices.Inventory;float plain=Body("Taren").resonance;
            void Wear(string who,string id,bool resonant=false)
            {var def=Part(id);var item=inventory.GivePart(def);if(resonant)item.affixes.Add("Resonant");Assert.IsTrue(inventory.Equip(Member(who),item,def),$"fixture: {id} goes on {who}");}
            Wear("Taren","ModulesT2");Wear("Sela","DrivesT2");Wear("Sela","EdgesT1",true);Wear("Sela","EmittersT1",true);
            PartyController.Current.RefreshStats();
            Assert.Greater(Body("Taren").resonance,plain,"fixture: Taren's module counts");
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            foreach(var hero in PartyController.Current.members)
                StringAssert.Contains($"{DisplayNames.Speaker(hero.character)}  ·  Response {hero.response:0}  ·  Resonance {hero.resonance:0}  ·  Fortune {hero.fortune:0}",Note(),$"the page doesn't give {hero.character}'s other three stats as the fight has them");
            StringAssert.Contains($"{State.scrip} scrip",Note(),"the note lost the party's scrip");StringAssert.Contains("autosaves",Note(),"the note lost what it says about saving");
            menu.Close();yield return null;
            // The longest it can read: the level cap, every part on, a fortune in scrip.
            foreach(string id in new[]{"ModulesT2","DrivesT2","FramesT2"})Wear("Taren",id,true);foreach(var part in State.parts)part.upgrade=5;
            Member("Taren").level=Member("Sela").level=Levels.Cap;State.scrip=999999;PartyController.Current.RefreshStats();
            menu.Open(PauseMenu.PartyPage);yield return null;
            var note=Label("Note");var box=note.rectTransform.rect;
            Assert.GreaterOrEqual(note.fontSize,24,"the note is under the type floor");
            Assert.LessOrEqual(note.GetPreferredValues(note.text,box.width,0).y,box.height+1,"the note runs out of its box: "+note.text);
            foreach(var hero in PartyController.Current.members)StringAssert.Contains($"Resonance {hero.resonance:0}",Note());
            menu.Close();
        }

        // ------------------------------------------------------------------ audi176: a thumb still tapping
        /// <summary>Only taps in the first .35 s after the close were eaten, so a thumb still going past that started the talk over. It counts from the last tap now, however long the thumb goes on.</summary>
        [UnityTest] public IEnumerator AThumbStillTappingPastTheHoldDoesNotStartTheTalkAgain()
        {
            AddPad();yield return Zone("Hub_Decks");
            var dialogue=DialogueSystem.Current;var hero=Hero;hero.GetComponent<PlayerBrain>().AutoPilot=false;
            var talker=new GameObject("Talker").AddComponent<Npc>();talker.speaker="Orrin";talker.firstNode="OrrinRepeat";talker.range=6;talker.transform.position=hero.transform.position;
            yield return Frames(2);
            yield return Tap(GamepadButton.South);
            Assert.IsTrue(dialogue.Running,"fixture: A beside someone talks to them");
            float deadline=Time.unscaledTime+10;
            while(dialogue.Running&&Time.unscaledTime<deadline)yield return Tap(GamepadButton.South);
            Assert.IsFalse(dialogue.Running,"fixture: the conversation closes under the thumb");
            // Well past the old .35 s, with no gap a thumb would call stopping.
            float closed=Time.unscaledTime;int taps=0;
            while(Time.unscaledTime-closed<1.2f)
            {
                float pressed=Time.unscaledTime;taps++;yield return Tap(GamepadButton.South);
                // Ahead of the check itself: a hitch inside this tap lets its press through, and that isn't the thumb's doing.
                Assert.Less(Time.unscaledTime-pressed,GameInput.HandBackHold,"fixture: the frames ran too slow to tap inside the hold");
                Assert.IsFalse(dialogue.Running,$"tap {taps}, {Time.unscaledTime-closed:0.00} s after the last line, with the thumb still going: the same conversation starts over");
            }
            yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
            yield return Tap(GamepadButton.South);
            Assert.IsTrue(dialogue.Running,"the thumb has stopped, and A still won't talk to the person standing there");
        }
        /// <summary>One rule for whatever hands the world back. Off a shop's Back row is how a mash reaches Mira's repeat and her shop again.</summary>
        [UnityTest] public IEnumerator TheHoldCountsFromTheLastTapOffAShopsBackRowToo()
        {
            AddPad();yield return Zone("Hub_Decks");
            var hero=Hero;hero.GetComponent<PlayerBrain>().AutoPilot=false;var prompt=PromptBeside(hero);yield return Frames(2);
            var shop=ShopPanel;shop.Open(null);yield return new WaitForSecondsRealtime(.4f);
            EventSystem.current.SetSelectedGameObject(Row("ShopBack").gameObject);yield return null;
            yield return Tap(GamepadButton.South);
            Assert.IsFalse(shop.IsOpen,"fixture: A on Back closes the shop");
            for(float end=Time.unscaledTime+1.2f;Time.unscaledTime<end;)
            {
                float pressed=Time.unscaledTime;yield return Tap(GamepadButton.South);
                Assert.Less(Time.unscaledTime-pressed,GameInput.HandBackHold,"fixture: the frames ran too slow to tap inside the hold");
                Assert.AreEqual(0,prompt.Uses,"a thumb still going past the hold reaches what stands beside the counter");
            }
            yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
            yield return Tap(GamepadButton.South);
            Assert.AreEqual(1,prompt.Uses,"the thumb has stopped, and A still won't answer what's beside the hero");
            Object.Destroy(prompt.gameObject);
        }
        /// <summary>A guard for the routes, which passes before the fix too. chapter-10-gullet-preview's 'study the strained collar' presses on a .4 s pulse: each eaten press starts the hold over, and a period
        /// longer than the hold has to get through on its next press, or a slow tap after a close never would.</summary>
        [UnityTest] public IEnumerator ATapSlowerThanTheHoldGetsThroughOnItsNextPress()
        {
            AddPad();yield return Zone("Hub_Decks");
            var hero=Hero;hero.GetComponent<PlayerBrain>().AutoPilot=false;var prompt=PromptBeside(hero);yield return Frames(2);
            var menu=Menu;menu.Open();yield return Frames(2);menu.Close();
            // The clock the hold reads, on the frame the press lands.
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;float pressed=Time.unscaledTime;yield return null;
            yield return Hold(new GamepadState());yield return null;
            Assert.AreEqual(0,prompt.Uses,"fixture: the tap straight after the menu closed is inside the hold");
            // The pulse's own period, press to press. Counted from the tap's end it came to nearer .5, and a hold raised past .4 would have stranded that step with this still green.
            yield return new WaitForSecondsRealtime(Mathf.Max(0,pressed+.4f-Time.unscaledTime));
            yield return Tap(GamepadButton.South);
            Assert.AreEqual(1,prompt.Uses,"a press .4 s after the one the hold ate is still eaten: a pulsed press at this rate is stranded at its prompt");
            Object.Destroy(prompt.gameObject);
        }
        /// <summary>A guard, which passes before the fix too. HoldOffInteract is public for anything that wants a longer hold, and a tap restarts the hand-back's .35 s without cutting a longer one short.</summary>
        [UnityTest] public IEnumerator ATapInsideALongerHoldDoesNotCutItShort()
        {
            AddPad();yield return Zone("Hub_Decks");
            var hero=Hero;hero.GetComponent<PlayerBrain>().AutoPilot=false;var prompt=PromptBeside(hero);yield return Frames(2);
            PromptService.HoldOffInteract(2);
            yield return Tap(GamepadButton.South);
            yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.15f);
            yield return Tap(GamepadButton.South);
            Assert.AreEqual(0,prompt.Uses,"a tap cut a longer hold down to the hand-back's");
            Object.Destroy(prompt.gameObject);
        }

        // ------------------------------------------------------------------ skil209: the partner's page
        /// <summary>The partner's loadout could only be seen by pressing Swap, which really swapped the party: it spent the swap's bark and changed who the player came back to.</summary>
        [UnityTest] public IEnumerator SwapOnTheSkillsPageShowsThePartnerAndLeavesThePartyAsItIs()
        {
            AddPad();PromptService.ForceDevice(PromptDevice.Gamepad);yield return Zone("Arena_Ground");
            var party=PartyController.Current;var lead=party.Active;int swaps=party.SwapCount,active=State.activeMember;
            Assert.AreEqual("Taren",lead.character,"fixture: Taren leads");
            Member("Taren").level=1;Member("Sela").level=12;
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return Frames(2);
            StringAssert.StartsWith("Taren's skills",Header(),"fixture: the page opens on who leads");
            StringAssert.Contains(PromptService.Tag("Swap")+" shows Sela's skills",Header(),"nothing says Swap shows the partner here, and doesn't swap");
            EventSystem.current.SetSelectedGameObject(Row("Row_3").gameObject);yield return null;
            yield return Tap(GamepadButton.North);
            Assert.AreSame(lead,party.Active,"looking at the partner's skills really swapped the party");
            Assert.AreEqual(swaps,party.SwapCount,"looking at the partner's skills counts as a swap");Assert.AreEqual(active,State.activeMember,"looking at the partner's skills changed who leads in the save");
            StringAssert.StartsWith("Sela's skills",Header(),"the page doesn't say whose skills these are");
            StringAssert.Contains(PromptService.Tag("Swap")+" shows Taren's skills",Header(),"the way back isn't named");
            for(int slot=0;slot<SkillBook.Slots;slot++)
            {
                var skill=SkillBook.Equipped("Sela",slot);var words=Words(Row("Row_"+(slot+1)));
                StringAssert.Contains(skill.displayName,words,$"slot {slot+1} doesn't show Sela's skill");
                StringAssert.Contains(GameHud.SkillBinding(slot,false),words,$"slot {slot+1} doesn't say which button fires it");
                StringAssert.Contains($"{SkillBook.Cost("Sela",skill):0} Charge",words,$"slot {slot+1} doesn't give her cost");
            }
            var hers=SkillBook.All("Sela");StringAssert.Contains($"{hers.Count(s=>SkillBook.Knows(Member("Sela"),s))} of {hers.Length} skills learned",Note(),"the note still counts the leader's skills");
            Assert.AreEqual("Row_3",Selected().name,"the cursor left its slot");
            yield return Tap(GamepadButton.North);
            StringAssert.StartsWith("Taren's skills",Header(),"Swap doesn't flip back");Assert.AreSame(lead,party.Active,"flipping back swapped the party");
            menu.Close();yield return null;
            Assert.AreSame(lead,PartyController.Current.Active,"the player came back to the world as someone else");
        }
        /// <summary>Trades apply to the hero on the page. Whose loadout it was used to be whoever led, so the only way to trade for the partner was to swap first.</summary>
        [UnityTest] public IEnumerator ATradeOnThePartnersPageGoesOnThePartnersLoadout()
        {
            AddPad();PromptService.ForceDevice(PromptDevice.Gamepad);yield return Zone("Arena_Ground");
            Member("Taren").level=Member("Sela").level=12;
            var party=PartyController.Current;var lead=party.Active;
            string[] Worn(string who)=>Enumerable.Range(0,SkillBook.Slots).Select(i=>SkillBook.Equipped(who,i).id).ToArray();
            var his=Worn("Taren");var first=SkillBook.Equipped("Sela",0);var spare=SkillBook.All("Sela").First(s=>!Worn("Sela").Contains(s.id));
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return Frames(2);
            yield return Tap(GamepadButton.North);
            StringAssert.StartsWith("Sela's skills",Header(),"fixture: Swap shows the partner");
            Row("Row_1").onClick.Invoke();yield return null;
            With("Back to Sela's slots");
            StringAssert.Contains(first.displayName,Words(Selected().transform),"choosing her slot lands on something else");
            With(spare.displayName).onClick.Invoke();yield return null;
            Assert.AreEqual(spare,SkillBook.Equipped("Sela",0),"the trade didn't go on the hero on the page");
            CollectionAssert.AreEqual(his,Worn("Taren"),"a trade on Sela's page changed Taren's loadout");
            StringAssert.StartsWith("Sela's skills",Header(),"a trade took the page back to the leader");
            Assert.AreEqual("Row_1",Selected().name,"after equipping, focus left the slot just changed");
            StringAssert.Contains(spare.displayName,Feedback(),"the trade said nothing");
            Assert.AreSame(lead,party.Active,"a trade on the partner's page swapped the party");
            menu.Close();
        }
        /// <summary>The chooser goes by the hero on the page too: what's locked is her level's, and Swap from inside it comes out on the same slot of the other hero.</summary>
        [UnityTest] public IEnumerator TheChooserGoesByTheHeroOnThePage()
        {
            AddPad();PromptService.ForceDevice(PromptDevice.Gamepad);yield return Zone("Arena_Ground");
            Member("Taren").level=12;Member("Sela").level=1;
            var party=PartyController.Current;int swaps=party.SwapCount;
            var locked=SkillBook.All("Sela").First(s=>s.unlockLevel>1);var worn=SkillBook.Equipped("Sela",1);
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return Frames(2);
            yield return Tap(GamepadButton.North);
            Row("Row_2").onClick.Invoke();yield return null;
            StringAssert.StartsWith("Locked",Words(With(locked.displayName)),"her skills read by the leader's level");
            With(locked.displayName).onClick.Invoke();yield return null;
            StringAssert.Contains($"is learned at Sync {locked.unlockLevel}",Feedback(),"a skill locked at her level doesn't say so");Assert.AreEqual(worn,SkillBook.Equipped("Sela",1),"a locked skill went on her slot");
            yield return Tap(GamepadButton.North);
            StringAssert.StartsWith("Taren's skills",Header(),"Swap in the chooser doesn't reach the other hero");
            Assert.IsFalse(Buttons().Any(b=>Words(b).StartsWith("Back to")),"Swap left the chooser up, for the other hero's slot");
            Assert.AreEqual("Row_2",Selected().name,"Swap from the chooser forgets which slot");
            Row("Row_2").onClick.Invoke();yield return null;
            Assert.IsFalse(Buttons().Any(b=>Words(b).StartsWith("Locked")),"at Sync 12 Taren has nothing locked: his chooser reads her level");
            Assert.AreEqual(swaps,party.SwapCount,"going through the chooser by Swap really swapped the party");
            menu.Close();
        }
        /// <summary>With the partner down, Swap only said so, and their skills couldn't be seen at all. The Gear page's Swap is the real one and still says it.</summary>
        [UnityTest] public IEnumerator APartnerWhoIsDownCanStillBeLookedAtAndGivenSkills()
        {
            AddPad();PromptService.ForceDevice(PromptDevice.Gamepad);yield return Zone("Arena_Ground");
            Member("Sela").level=12;
            var party=PartyController.Current;var lead=party.Active;var partner=party.members[1-party.index];
            partner.Health.Receive(new DamagePacket{amount=partner.Health.maximum*10});yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(partner.Health.Alive,"fixture: the partner went down");
            var spare=SkillBook.All("Sela").First(s=>Enumerable.Range(0,SkillBook.Slots).All(i=>SkillBook.Equipped("Sela",i)!=s));
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return Frames(2);
            using(var log=new CueLog())
            {
                yield return Tap(GamepadButton.North);
                Assert.AreEqual(0,log.Count("ui_error"),"looking at a downed partner's skills is refused: "+log.All);
            }
            StringAssert.DoesNotContain("is down",Feedback(),"the page still only says the partner is down");
            StringAssert.StartsWith("Sela's skills",Header(),"with the partner down their skills can't be seen");
            Row("Row_1").onClick.Invoke();yield return null;With(spare.displayName).onClick.Invoke();yield return null;
            Assert.AreEqual(spare,SkillBook.Equipped("Sela",0),"a downed partner can't be given a skill for when they're up");
            Row("Tab_Gear").onClick.Invoke();yield return null;
            yield return Tap(GamepadButton.North);
            StringAssert.Contains("Sela is down.",Feedback(),"Swap on the Gear page really swaps, and has to say when it can't");
            Assert.AreSame(lead,party.Active,"fixture: nobody swapped");
            menu.Close();
        }
        /// <summary>A look at the partner lasts as long as the page: back on it, it opens on whoever leads. The Gear page's Swap really swaps, as its words say, and the Skills page follows it.</summary>
        [UnityTest] public IEnumerator ThePageOpensOnWhoeverLeadsAndGearStillReallySwaps()
        {
            AddPad();PromptService.ForceDevice(PromptDevice.Gamepad);yield return Zone("Arena_Ground");
            var party=PartyController.Current;
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return Frames(2);
            yield return Tap(GamepadButton.North);StringAssert.StartsWith("Sela's skills",Header(),"fixture: Swap shows the partner");
            menu.Close();yield return null;menu.Open(PauseMenu.SkillsPage);yield return Frames(2);
            StringAssert.StartsWith("Taren's skills",Header(),"the menu reopened on the last look, not on who leads");
            yield return Tap(GamepadButton.North);StringAssert.StartsWith("Sela's skills",Header());
            Row("Tab_Party").onClick.Invoke();yield return null;Row("Tab_Skills").onClick.Invoke();yield return null;
            StringAssert.StartsWith("Taren's skills",Header(),"coming back to the tab kept the last look, not who leads");
            Row("Tab_Gear").onClick.Invoke();yield return null;
            StringAssert.Contains("switches member",Header(),"the Gear page's Swap is still the real one: its words shouldn't have changed");
            yield return Tap(GamepadButton.North);
            Assert.AreEqual("Sela",party.Active.character,"Swap on the Gear page no longer swaps");StringAssert.StartsWith("Equipping Sela",Header());
            Row("Tab_Skills").onClick.Invoke();yield return null;
            StringAssert.StartsWith("Sela's skills",Header(),"the Skills page doesn't follow a real swap");
            yield return Tap(GamepadButton.North);
            StringAssert.StartsWith("Taren's skills",Header());Assert.AreEqual("Sela",party.Active.character,"a look at Taren's page swapped back");
            menu.Close();
        }
        /// <summary>A hero alone has nobody to look at: no words about it, and the press does nothing.</summary>
        [UnityTest] public IEnumerator AHeroAloneHasNoPartnerToLookAt()
        {
            AddPad();PromptService.ForceDevice(PromptDevice.Gamepad);GameServices.Current.NewGame();yield return Zone("Hub_Decks");
            Assert.AreEqual(1,State.party.Count,"fixture: a new game starts alone");
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return Frames(2);
            StringAssert.StartsWith("Taren's skills",Header());StringAssert.DoesNotContain(PromptService.Tag("Swap"),Header(),"the page offers a partner who isn't there");
            yield return Tap(GamepadButton.North);
            StringAssert.StartsWith("Taren's skills",Header(),"Swap with nobody to swap to changed the page");
            menu.Close();
        }
    }
}
