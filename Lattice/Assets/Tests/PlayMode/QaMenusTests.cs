using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
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
    /// <summary>The November QA pass on the menus: the next press lands where the player thinks it will, every row says what
    /// its thing does, and nothing is refused, spent or destroyed without a word. Every check here failed on the build the
    /// pass started from.</summary>
    public sealed class QaMenusTests
    {
        static GameState State=>GameServices.Current.State;
        static PauseMenu Menu=>Object.FindFirstObjectByType<PauseMenu>();
        static ShopUi ShopPanel=>Object.FindFirstObjectByType<ShopUi>();
        static MemberState Taren=>State.party.Find(m=>m.id=="Taren");
        static MemberState Sela=>State.party.Find(m=>m.id=="Sela");
        static TechPartDef Part(string id)=>GameCatalog.Find<TechPartDef>(id);
        string savedBindings;
        Gamepad pad;Keyboard keyboard;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            savedBindings=PlayerPrefs.GetString("keyboardBindings","");
            GameInput.Current.Asset.RemoveAllBindingOverrides();
            DevLoadout.Apply("starter");yield return Load("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(pad!=null){InputSystem.RemoveDevice(pad);pad=null;}
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
            if(GameInput.Current!=null)GameInput.Current.Asset.RemoveAllBindingOverrides();
            if(string.IsNullOrEmpty(savedBindings))PlayerPrefs.DeleteKey("keyboardBindings");else PlayerPrefs.SetString("keyboardBindings",savedBindings);
            // Back to the default device, which also drops any tag cached off an override: a pad press or a rebind here mustn't decide what the next fixture's prompts say.
            PromptService.ForceDevice(PromptDevice.Keyboard);PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);
            float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture load timed out");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return null;yield return null;
        }

        // ------------------------------------------------------------------ what's on screen
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static Button[] Rows()=>Buttons().Where(b=>b.name.StartsWith("Row_")).OrderBy(b=>int.Parse(b.name.Substring(4))).ToArray();
        static string Words(Component c){var t=c.GetComponentInChildren<TMP_Text>();return t!=null?t.text:"";}
        static string[] Shown()=>Buttons().Select(b=>Words(b)).ToArray();
        static Button With(string words)
        {
            var found=Buttons().FirstOrDefault(b=>Words(b).Contains(words));
            Assert.IsNotNull(found,$"no row says \"{words}\". On screen: {string.Join(" | ",Shown())}");return found;
        }
        static GameObject Selected()
        {var s=EventSystem.current.currentSelectedGameObject;Assert.IsNotNull(s,"nothing has focus: a pad player is stuck");return s;}
        static TMP_Text Label(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static string Note(){var t=Label("Note");Assert.IsNotNull(t,"the page has no note");return t.text;}
        static string Feedback(){var t=Label("Feedback");return t!=null?t.text:"";}
        static Rect Box(Component c){var corners=new Vector3[4];((RectTransform)c.transform).GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);}
        static void ClearOfTheNote(string surface)
        {
            var note=Box(Label("Note"));
            foreach(var row in Rows().Concat(Buttons().Where(b=>b.name=="Pager")))Assert.IsFalse(Box(row).Overlaps(note),$"{surface}: {row.name} sits under the note's words");
        }
        static void Fits(string surface,List<string> failures)
        {
            foreach(var button in Buttons())
            {
                var text=button.GetComponentInChildren<TMP_Text>();if(text==null)continue;var box=text.rectTransform.rect;
                // TMP's measure ignores the label's own margins, and the row really wraps inside them.
                var need=text.GetPreferredValues(text.text,box.width-text.margin.x-text.margin.z,0);
                if(need.y>box.height+1)failures.Add($"{surface}: \"{text.text}\" needs {need.y:0} in a {box.height:0} tall row");
            }
        }
        static IEnumerator Action(int presses)
        {for(int i=0;i<presses;i++){Row("Row_1").onClick.Invoke();yield return null;}}
        static IEnumerator Bindings(PauseMenu menu)
        {menu.Open(PauseMenu.SettingsPage);yield return null;With("Keyboard bindings").onClick.Invoke();yield return null;}
        IEnumerator PressKey(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.25f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        IEnumerator PadPress(GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;
        }
        static string KeyPath(InputActionMap map,string action)
        {var found=map.FindAction(action);return found.bindings.First(b=>b.path.StartsWith("<Keyboard>")).effectivePath.ToLowerInvariant();}

        // ------------------------------------------------------------------ focus
        /// <summary>ui217. Choosing a slot used to land on Back, and equipping on the swap row: the next A swapped hero.</summary>
        [UnityTest] public IEnumerator TheSkillChooserOpensOnTheSkillInThatSlotAndComesBackToIt()
        {
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;int hero=State.activeMember;
            Row("Row_3").onClick.Invoke();yield return null;
            var inSlot=SkillBook.Equipped("Taren",2);
            StringAssert.Contains(inSlot.displayName,Words(Selected().transform),"choosing a slot lands elsewhere, so the next A does not pick what the player is looking at");
            var other=SkillBook.Equipped("Taren",0);
            Rows().First(b=>Words(b).Contains(other.displayName)).onClick.Invoke();yield return null;
            Assert.AreEqual(other,SkillBook.Equipped("Taren",2),"fixture: the skill went on the slot");
            Assert.AreEqual("Row_3",Selected().name,"after equipping, focus left the slot just changed");
            Selected().GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.AreEqual(hero,State.activeMember,"the next A after equipping swapped hero");
            With("Back to").onClick.Invoke();yield return null;
            Assert.AreEqual("Row_3",Selected().name,"backing out left the slot just opened");
            var header=Row("Row_0");if(header!=null){header.onClick.Invoke();yield return null;}
            Assert.AreEqual(hero,State.activeMember,"the page's title row swaps hero when pressed");
            menu.Close();
        }
        /// <summary>ui218. Sync 5+ skills sat on a second page, behind a pager that lost its place.</summary>
        [UnityTest] public IEnumerator AllEightSkillsFitWithoutAPage()
        {
            Taren.level=12;var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;
            Row("Row_1").onClick.Invoke();yield return null;
            var all=SkillBook.All("Taren");
            Assert.AreEqual(all.Length+1,Rows().Length,"Back and every skill don't share one page: "+string.Join(" | ",Shown()));
            foreach(var skill in all)With(skill.displayName);
            ClearOfTheNote("chooser");
            menu.Close();
        }
        /// <summary>ui218. Focus went by row index, so a page turn could land on a buy, an equip or a salvage.</summary>
        [UnityTest] public IEnumerator EveryPagerKeepsItsPlaceAndSpendsNothing()
        {
            var inventory=RpgServices.Inventory;
            foreach(string id in new[]{"EdgesT1","EdgesT2","EmittersT1","EmittersT2","FramesT1","FramesT2","DrivesT1","DrivesT2","ModulesT1"})inventory.GivePart(Part(id));
            foreach(string id in new[]{"HuskCore","ChoirResin","CantorPearl","WarpKey"})State.materials[id]=1;
            foreach(var quest in GameCatalog.All<QuestDef>())State.questSteps[quest.id]=0;
            var menu=Menu;
            foreach(int page in new[]{PauseMenu.GearPage,PauseMenu.FabricatePage,PauseMenu.ItemsPage,PauseMenu.QuestsPage})
            {menu.Open(page);yield return null;yield return WalkPager("pause page "+page);menu.Close();yield return null;}
            yield return Bindings(menu);yield return WalkPager("keyboard bindings");menu.Close();yield return null;
            var shop=ShopPanel;shop.Open(GameCatalog.Find<ShopInventory>("Lanternwick"));yield return null;yield return WalkPager("Corra's stock");
            Row("ShopRow0").onClick.Invoke();yield return null;yield return WalkPager("the sell page");shop.Close();
        }
        static IEnumerator WalkPager(string surface)
        {
            var pager=Row("Pager");Assert.IsNotNull(pager,$"{surface}: the pager has no name of its own, so focus after it lands on whatever took its index. On screen: {string.Join(" | ",Shown())}");
            EventSystem.current.SetSelectedGameObject(pager.gameObject);
            int scrip=State.scrip,parts=State.parts.Count;string first=Words(pager);
            for(int i=0;i<6;i++)
            {
                Row("Pager").onClick.Invoke();yield return null;
                Assert.AreEqual("Pager",Selected().name,$"{surface}: after Next page the next A lands on \"{Words(Selected().transform)}\"");
                Assert.AreEqual(scrip,State.scrip,surface+": turning a page spent scrip");Assert.AreEqual(parts,State.parts.Count,surface+": turning a page changed the parts");
                if(Words(Row("Pager"))==first)yield break;
            }
            Assert.Fail(surface+": Next page never came back to the first page");
        }

        // ------------------------------------------------------------------ the quest log
        /// <summary>ui220, copy10, ques115, ques122. One description, whoever's, in no order, with the tenth row under the note.</summary>
        [UnityTest] public IEnumerator TheQuestLogLeadsWithTheMainQuestAndEveryRowHasItsOwnNote()
        {
            var quests=GameCatalog.All<QuestDef>();Assert.AreEqual(10,quests.Length,"fixture: the log is meant to overflow one page");
            State.questSteps.Clear();foreach(var quest in quests)State.questSteps[quest.id]=0;
            foreach(string id in new[]{"Sorrel","Hushwell","MiraCrystals"})State.questSteps[id]=quests.First(q=>q.id==id).steps.Length;
            var menu=Menu;menu.Open(PauseMenu.QuestsPage);yield return null;
            StringAssert.Contains("The Lantern Road",Words(Rows()[0]),"the main story isn't at the top of the log");
            Assert.AreEqual(8,Rows().Length,"the log doesn't page at eight rows: "+string.Join(" | ",Shown()));
            var seen=new List<string>();
            for(int page=0;page<2;page++)
            {
                ClearOfTheNote("quest log");
                foreach(var row in Rows())
                {
                    var quest=quests.First(q=>Words(row).Contains(q.title));
                    EventSystem.current.SetSelectedGameObject(row.gameObject);
                    StringAssert.Contains($"{quest.title}: {quest.description}",Note(),"the note doesn't follow the focused row and name it");
                    seen.Add(Words(row));
                }
                Row("Pager").onClick.Invoke();yield return null;
            }
            Assert.AreEqual(quests.Length,seen.Count,"a started quest isn't in the log");
            Assert.Less(seen.FindLastIndex(w=>!w.Contains("COMPLETE")),seen.FindIndex(w=>w.Contains("COMPLETE")),"finished quests sit among the live ones: "+string.Join(" | ",seen));
            menu.Close();
        }
        /// <summary>copy21. Orrin doesn't know why Sorrel went quiet: he's asking.</summary>
        [UnityTest] public IEnumerator TheEmptyLogPointsAtOrrin()
        {
            State.questSteps.Clear();var menu=Menu;menu.Open(PauseMenu.QuestsPage);yield return null;
            StringAssert.Contains("Orrin at the dock office wants to see you about Sorrel.",Note(),"the empty log says something the story contradicts");
            Assert.AreEqual("Close",Selected().name,"an empty log must still leave the pad on Back");
            menu.Close();
        }
        /// <summary>ui221. Nothing in the log covers the Gullet: a lost player was told everything is finished.</summary>
        [UnityTest] public IEnumerator AFinishedLogGivesTheObjective()
        {
            State.questSteps.Clear();State.questSteps["Sorrel"]=GameCatalog.Find<QuestDef>("Sorrel").steps.Length;State.questSteps["Hushwell"]=GameCatalog.Find<QuestDef>("Hushwell").steps.Length;
            float until=Time.unscaledTime+3;while(string.IsNullOrEmpty(Label("Objective").text)&&Time.unscaledTime<until)yield return null;
            string objective=Label("Objective").text.Split('\n')[0];Assert.IsNotEmpty(objective,"fixture: the objective panel never spoke");
            var menu=Menu;menu.Open(PauseMenu.QuestsPage);yield return null;
            StringAssert.Contains(objective,Note(),"the page a lost player opens doesn't say what to do next");
            StringAssert.DoesNotContain("finished",Note(),"the log says everything is finished in the middle of the main story");
            menu.Close();
        }

        // ------------------------------------------------------------------ what things do
        /// <summary>ui226, copy11, prog149. No screen printed a stat or an effect, so choosing gear was blind.</summary>
        [UnityTest] public IEnumerator RowsSayWhatThingsDo()
        {
            var inventory=RpgServices.Inventory;var menu=Menu;var shop=ShopPanel;var corra=GameCatalog.Find<ShopInventory>("Lanternwick");
            inventory.GivePart(Part("FramesT2"));
            menu.Open(PauseMenu.GearPage);yield return null;With("Refined Frame  T2 +0  ·  Plating +16");menu.Close();yield return null;
            shop.Open(corra);yield return null;With("Refined Frame  ·  Plating +16  —  260 scrip");shop.Close();yield return null;
            menu.Open(PauseMenu.ItemsPage);yield return null;
            With($"Repair Gel ×{State.consumables["RepairGel"]}  ·  restores 60 Integrity");With($"Charge Cell ×{State.consumables["ChargeCell"]}  ·  restores 50 Charge");menu.Close();yield return null;
            menu.Open(PauseMenu.FabricatePage);yield return null;With("Scrap Alloy 20/3");menu.Close();yield return null;
            menu.Open(PauseMenu.PartyPage);yield return null;
            StringAssert.Contains("Output 50",Words(Row("Row_0")),"the Party page hides what a hero hits for");StringAssert.Contains("Plating 10",Words(Row("Row_0")));menu.Close();yield return null;
            // Set against what the hero wears, where the part would replace something.
            var scrap=Part("FramesT1");inventory.Equip(Taren,inventory.GivePart(scrap),scrap);
            menu.Open(PauseMenu.GearPage);yield return null;With("Refined Frame  T2 +0  ·  Plating +16  ·  now +8");menu.Close();yield return null;
            shop.Open(corra);yield return null;With("Refined Frame  ·  Plating +16  —  260 scrip  ·  now +8");shop.Close();yield return null;
            // The longest rows the game can draw still stay on their row, in either device's words.
            var failures=new List<string>();Taren.level=40;
            foreach(string id in new[]{"ModulesT2","EmittersT2","EdgesT2"})
            {var def=Part(id);var worn=inventory.GivePart(def);worn.upgrade=5;worn.affixes.Add("Resonant");inventory.Equip(Sela,worn,def);}
            foreach(var quest in GameCatalog.All<QuestDef>())State.questSteps[quest.id]=0;
            foreach(var device in new[]{PromptDevice.Gamepad,PromptDevice.Keyboard})
            {
                PromptService.ForceDevice(device);yield return null;
                for(int page=0;page<menu.PageCount;page++){menu.Open(page);yield return null;Fits("pause page "+page,failures);menu.Close();yield return null;}
                shop.Open(corra);yield return null;Fits("shop",failures);Row("ShopRow0").onClick.Invoke();yield return null;Fits("sell",failures);shop.Close();yield return null;
                menu.Open(PauseMenu.SkillsPage);yield return null;Row("Row_1").onClick.Invoke();yield return null;Fits("chooser",failures);menu.Close();yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        /// <summary>prog158. Boss trophies sat in the list with no word on what they're for.</summary>
        [UnityTest] public IEnumerator KeepsakesSaySo()
        {
            foreach(string id in new[]{"HuskCore","ChoirResin","CantorPearl"})State.materials[id]=1;
            var menu=Menu;menu.Open(PauseMenu.ItemsPage);yield return null;
            foreach(string name in new[]{"Husk Core","Choir Resin","Cantor Pearl"})With($"{name} ×1  ·  keep this: no use yet");
            StringAssert.DoesNotContain("no use yet",Words(With("Scrap Alloy")),"a material the bench uses says it has no use");
            menu.Close();
        }
        /// <summary>ui248, skil209. Cooldowns and mastery progress weren't on any screen, and a skill traded out of a slot moved silently.</summary>
        [UnityTest] public IEnumerator SkillRowsShowCooldownMasteryAndWhereASkillIs()
        {
            Taren.skillUses=null;var first=SkillBook.Equipped("Taren",0);var second=SkillBook.Equipped("Taren",1);
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;
            StringAssert.Contains("Mastery 1 (0/6)",Words(Row("Row_1")),"nothing says how far off the next mastery is");
            StringAssert.Contains($"{first.cooldown:0} s",Words(Row("Row_1")),"no screen states the cooldown");
            for(int i=0;i<7;i++)SkillBook.RecordUse("Taren",first);
            Row("Tab_Party").onClick.Invoke();yield return null;Row("Tab_Skills").onClick.Invoke();yield return null;
            StringAssert.Contains("Mastery 2 (7/18)",Words(Row("Row_1")));
            Row("Row_1").onClick.Invoke();yield return null;
            StringAssert.Contains("In use · "+first.displayName,Words(With(first.displayName)));
            var elsewhere=With(second.displayName);StringAssert.Contains($"On {GameHud.SkillBinding(1,false)} · ",Words(elsewhere),"a skill in another slot looks free until it silently trades");
            elsewhere.onClick.Invoke();yield return null;
            StringAssert.Contains(first.displayName,Feedback(),"the skill that was displaced moved without a word");
            menu.Close();
        }
        /// <summary>ui246. "Not enough materials" meant a trip to Items, and a crafted part just vanished into a list.</summary>
        [UnityTest] public IEnumerator TheBenchShowsHeldAgainstNeededAndWhereThePartWent()
        {
            var inventory=RpgServices.Inventory;var edge=Part("EdgesT1");inventory.Equip(Taren,inventory.GivePart(edge),edge);inventory.Equip(Sela,inventory.GivePart(edge),edge);
            var menu=Menu;menu.Open(PauseMenu.FabricatePage,true);yield return null;
            var row=With("Scrap Edge");StringAssert.Contains("Scrap Alloy 20/3",Words(row),"the recipe doesn't say what's held");
            row.onClick.Invoke();yield return null;
            StringAssert.Contains("Now in Gear",Feedback(),"nothing says where the part went");
            StringAssert.Contains("Edges 1 (10/30)",Note(),"no progress toward the next discipline level");
            State.materials["ScrapAlloy"]=1;With("Scrap Edge").onClick.Invoke();yield return null;
            StringAssert.Contains("Scrap Alloy 1/3",Feedback(),"a refusal must say what's short");
            menu.Close();
        }

        // ------------------------------------------------------------------ equipping
        /// <summary>ui225, prog157, copy4, copy29. "Unequip this part" with no way to, and a partner's parts that looked free.</summary>
        [UnityTest] public IEnumerator EquipModeTakesAPartOffAndNamesTheWearer()
        {
            var inventory=RpgServices.Inventory;var def=Part("EdgesT2");var item=inventory.GivePart(def);inventory.Equip(Sela,item,def);
            bool Worn()=>State.party.Any(m=>m.equipped.ContainsValue(item.instanceId));
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return null;
            StringAssert.Contains("Sela · Refined Edge",Words(With("Refined Edge")),"a part the partner wears looks free");
            StringAssert.DoesNotContain("[E]",string.Join(" ",Shown()),"a bracketed E reads as a key prompt");
            With("Refined Edge").onClick.Invoke();yield return null;
            StringAssert.Contains("Moved from Sela to Taren.",Feedback(),"the part left Sela without a word");
            Assert.AreEqual(item.instanceId,Taren.equipped[GearSlot.Edge]);Assert.IsFalse(Sela.equipped.ContainsKey(GearSlot.Edge));
            StringAssert.Contains("Equipped · Refined Edge",Words(With("Refined Edge")));
            yield return Action(3);
            With("Refined Edge").onClick.Invoke();yield return null;
            Assert.IsTrue(State.parts.Contains(item),"salvaged a part in use");
            StringAssert.DoesNotContain("Unequip",Feedback());StringAssert.Contains("Take it off",Feedback(),"a refusal must say what to do");
            yield return Action(1);
            With("Refined Edge").onClick.Invoke();yield return null;
            Assert.IsFalse(Worn(),"Equip on a worn part must take it off: there is no other way");
            StringAssert.DoesNotContain("Equipped ·",Words(With("Refined Edge")));
            yield return Action(1);
            With("Refined Edge").onClick.Invoke();yield return null;
            StringAssert.Contains("module",Feedback().ToLowerInvariant(),"the slot 2 refusal doesn't say what fits");
            yield return Action(3);
            With("Refined Edge").onClick.Invoke();yield return null;
            Assert.IsTrue(Worn(),"fixture: back in Equip mode the part goes on");
            menu.Close();yield return null;
            var shop=ShopPanel;shop.Open(null);yield return null;Row("ShopRow0").onClick.Invoke();yield return null;
            With("Refined Edge").onClick.Invoke();yield return null;
            Assert.IsTrue(State.parts.Contains(item),"sold a part in use");StringAssert.DoesNotContain("Unequip",Feedback());StringAssert.Contains("Take it off",Feedback());
            shop.Close();
        }
        /// <summary>ui234, copy31. One press destroyed a part, in a mode left on from the last visit, and said only "Salvaged".</summary>
        [UnityTest] public IEnumerator SalvageAsksTwiceAndNamesTheYield()
        {
            var inventory=RpgServices.Inventory;var item=inventory.GivePart(Part("EdgesT2"));item.affixes.Add("Resonant");
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return null;
            yield return Action(3);StringAssert.Contains("Action: Salvage",Words(Row("Row_1")),"fixture");
            menu.Close();yield return null;menu.Open(PauseMenu.GearPage);yield return null;
            StringAssert.Contains("Action: Equip",Words(Row("Row_1")),"coming back to Gear, A still destroys parts");
            var row=With("Refined Edge");StringAssert.Contains("Refined Edge  T2 +0  ·  Resonant",Words(row),"an overclocked part looks like a plain one");StringAssert.Contains("Resonance +4",Words(row));
            yield return Action(3);int alloy=inventory.Count("ScrapAlloy");
            With("Refined Edge").onClick.Invoke();yield return null;
            Assert.IsTrue(State.parts.Contains(item),"one press destroyed a part");
            StringAssert.Contains("4 Scrap Alloy",Feedback(),"the question doesn't say what the part is worth");
            // A device flip wipes the question off the screen, so it has to disarm it too: a bumped mouse, then A.
            PromptService.ForceDevice(PromptService.Device==PromptDevice.Gamepad?PromptDevice.Keyboard:PromptDevice.Gamepad);yield return null;
            Assert.IsEmpty(Feedback(),"fixture: a device flip clears the question");
            With("Refined Edge").onClick.Invoke();yield return null;
            Assert.IsTrue(State.parts.Contains(item),"the question left the screen but the next press still destroyed the part");
            StringAssert.Contains("4 Scrap Alloy",Feedback(),"after the flip, the press must ask again");
            With("Refined Edge").onClick.Invoke();yield return null;
            Assert.IsFalse(State.parts.Contains(item),"the second press didn't salvage");Assert.AreEqual(alloy+4,inventory.Count("ScrapAlloy"));
            StringAssert.Contains("Salvaged Refined Edge for 4 Scrap Alloy.",Feedback());
            menu.Close();
        }
        /// <summary>ui233. Salvaging the end of the list left an empty page with no pager: the parts looked gone.</summary>
        [UnityTest] public IEnumerator SalvagingTheLastPageLeavesARealPage()
        {
            var inventory=RpgServices.Inventory;for(int i=0;i<7;i++)inventory.GivePart(Part("EdgesT1"));
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return null;
            for(int i=0;i<4;i++)
            {
                var next=Buttons().First(b=>Words(b).StartsWith("Next page"));var count=System.Text.RegularExpressions.Regex.Match(Words(next),@"\((\d+)/(\d+)\)");
                if(count.Groups[1].Value==count.Groups[2].Value)break;
                next.onClick.Invoke();yield return null;
            }
            yield return Action(3);
            int left=State.parts.Count-Rows().Count(b=>Words(b).Contains("Scrap Edge"));
            for(int i=0;i<14&&State.parts.Count>left;i++){Rows().First(b=>Words(b).Contains("Scrap Edge")).onClick.Invoke();yield return null;}
            Assert.AreEqual(left,State.parts.Count,"fixture: the page emptied");
            Assert.Greater(Rows().Count(b=>Words(b).Contains("Scrap Edge")),0,"the parts that are left look gone: no rows and no way to another page");
            menu.Close();
        }
        /// <summary>prog153. A new game had nothing equipped, and nothing said a part was waiting in Gear.</summary>
        [UnityTest] public IEnumerator ANewGameStartsArmedAndSelaJoinsArmed()
        {
            GameServices.Current.NewGame();
            Assert.IsTrue(Taren.equipped.TryGetValue(GearSlot.Edge,out string edge),"a new game starts with empty hands");
            Assert.AreEqual("EdgesT1",State.parts.Find(p=>p.instanceId==edge).definitionId);
            yield return Load("Hub_Decks");
            // copy15: alone, there's nobody to swap with and no page says otherwise.
            var menu=Menu;
            foreach(int page in new[]{PauseMenu.PartyPage,PauseMenu.SkillsPage,PauseMenu.GearPage})
            {
                menu.Open(page);yield return null;
                var words=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Select(t=>t.text).ToArray();
                Assert.IsFalse(words.Any(w=>w.Contains("Swap active")||w.Contains("switches member")),$"page {page} offers a swap while Taren is alone");
                menu.Close();yield return null;
            }
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;
            Assert.IsTrue(dialogue.StartNode("OrrinFirst","Orrin"),"fixture: Orrin's briefing started");
            float deadline=Time.unscaledTime+30;while(dialogue.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsNotNull(Sela,"fixture: Sela joined through the real path");
            Assert.IsTrue(Sela.equipped.TryGetValue(GearSlot.Emitter,out string emitter),"Sela joins with nothing to fire");
            Assert.AreEqual("EmittersT1",State.parts.Find(p=>p.instanceId==emitter).definitionId);
            menu.Open(PauseMenu.PartyPage);yield return null;With("Swap active member");menu.Close();yield return null;
            menu.Open(PauseMenu.SkillsPage);yield return null;
            Assert.IsTrue(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("switches member")),"nothing says how to reach Sela's skills");
            menu.Close();
        }
        /// <summary>prog153. Rewards and purchases sat unequipped: a player could finish a chapter with empty slots and a Refined Frame in the bag.</summary>
        [UnityTest] public IEnumerator ANewPartEquipsItselfWhenItsSlotIsEmptyAndSaysSo()
        {
            var toast=Object.FindFirstObjectByType<QuestToast>();var inventory=RpgServices.Inventory;yield return null;yield return null;
            inventory.Grant("FramesT2",1);
            Assert.IsTrue(Taren.equipped.ContainsKey(GearSlot.Frame),"a part with an empty slot waiting went into the bag");
            yield return Card(toast,"Taren");
            inventory.Grant("FramesT2",1);Assert.IsTrue(Sela.equipped.ContainsKey(GearSlot.Frame),"the partner's slot was empty too");
            inventory.Grant("FramesT2",1);yield return Card(toast,"Equip it from Gear");
            Assert.AreEqual(2,State.party.Count(m=>m.equipped.ContainsKey(GearSlot.Frame)));
            State.scrip=1000;var shop=ShopPanel;shop.Open(null);yield return null;
            Row("ShopRow3").onClick.Invoke();yield return null;StringAssert.Contains("Purchased Scrap Frame. Equip it from Gear.",Feedback(),"nothing says the part isn't on anyone");
            Row("ShopRow1").onClick.Invoke();yield return null;StringAssert.Contains("Purchased Scrap Emitter. Equipped on Sela.",Feedback(),"a bought part with a slot waiting isn't worn");
            shop.Close();
        }
        static IEnumerator Card(QuestToast toast,string words)
        {
            float until=Time.unscaledTime+6;while(!toast.Body.Contains(words)&&Time.unscaledTime<until)yield return null;
            StringAssert.Contains(words,toast.Body,"no card says where the new part went");
        }

        // ------------------------------------------------------------------ backing out
        /// <summary>ui235. B from a sub-page dropped the player out of the whole menu.</summary>
        [UnityTest] public IEnumerator BackStepsOutOneLevelAtATime()
        {
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;Row("Row_2").onClick.Invoke();yield return null;
            menu.StepBack();yield return null;
            Assert.IsTrue(menu.IsOpen);Assert.IsFalse(Buttons().Any(b=>Words(b).StartsWith("Back to")),"Back stays in the skill chooser");
            Assert.AreEqual("Row_2",Selected().name,"backing out of a slot forgets which slot");
            menu.StepBack();yield return null;Assert.IsFalse(menu.IsOpen,"at the top of a page, Back closes the menu");
            yield return Bindings(menu);menu.StepBack();yield return null;
            Assert.IsTrue(menu.IsOpen);Assert.AreEqual(5,menu.GetComponentsInChildren<Slider>().Length,"Back stays in the keyboard bindings");
            StringAssert.Contains("Keyboard bindings",Words(Selected().transform),"backing out forgets where the player came from");
            With("Leave the game").onClick.Invoke();yield return null;menu.StepBack();yield return null;
            Assert.IsTrue(menu.IsOpen);Assert.IsFalse(Buttons().Any(b=>Words(b).Contains("Return to title")),"Back stays on the leave question");
            StringAssert.Contains("Leave the game",Words(Selected().transform));
            menu.Close();yield return null;
            // The Back button is the same step as B, or one Up and A from the chooser's top row drops the player in the world.
            menu.Open(PauseMenu.SkillsPage);yield return null;Row("Row_2").onClick.Invoke();yield return null;
            Row("Close").onClick.Invoke();yield return null;
            Assert.IsTrue(menu.IsOpen,"the Back button left the whole menu from a sub-page");Assert.AreEqual("Row_2",Selected().name,"the Back button forgets which slot");
            Row("Close").onClick.Invoke();yield return null;
            Assert.IsFalse(menu.IsOpen,"at the top of a page, the Back button closes the menu");
        }
        [UnityTest] public IEnumerator ThePadsBStepsOutOfTheChooser()
        {
            pad=InputSystem.AddDevice<Gamepad>();
            var menu=Menu;menu.Open(PauseMenu.SkillsPage);yield return null;Row("Row_2").onClick.Invoke();yield return null;
            yield return PadPress(GamepadButton.East);
            Assert.IsTrue(menu.IsOpen,"B backing out of a slot choice dropped the player in the world");
            Assert.IsFalse(Buttons().Any(b=>Words(b).StartsWith("Back to")),"B stays in the skill chooser");
            yield return PadPress(GamepadButton.East);
            Assert.IsFalse(menu.IsOpen,"B at the top of a page closes the menu");
        }

        // ------------------------------------------------------------------ mouse and shop
        /// <summary>ui236. A rebuild under a parked mouse raises a pointer-enter with no movement, and it stole keyboard focus.</summary>
        [UnityTest] public IEnumerator AParkedMouseStealsNothing()
        {
            PromptService.ForceDevice(PromptDevice.Keyboard);yield return null;
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            var focus=Selected();var other=Rows().First(b=>b.gameObject!=focus);var hover=other.GetComponent<HoverSelect>();
            hover.OnPointerEnter(new PointerEventData(EventSystem.current));
            Assert.AreEqual(focus,Selected(),"a mouse that didn't move took the focus from the keyboard");
            hover.OnPointerEnter(new PointerEventData(EventSystem.current){delta=new Vector2(6,0)});
            Assert.AreEqual(other.gameObject,Selected(),"a moving mouse must still select what it touches");
            menu.Close();
        }
        /// <summary>ui237. Shops opened on the Sell toggle, and sold on one press with "Part sold."</summary>
        [UnityTest] public IEnumerator TheShopOpensOnTheFirstOfferAndSellingSaysWhat()
        {
            var shop=ShopPanel;shop.Open(null);yield return null;
            Assert.AreEqual("ShopRow1",Selected().name,"the shop doesn't open on what it sells");
            shop.Close();yield return null;
            var inventory=RpgServices.Inventory;var scrap=inventory.GivePart(Part("EdgesT1"));var refined=inventory.GivePart(Part("FramesT2"));
            shop.Open(null);yield return null;Row("ShopRow0").onClick.Invoke();yield return null;
            int scrip=State.scrip;
            With("Scrap Edge").onClick.Invoke();yield return null;
            Assert.IsFalse(State.parts.Contains(scrap),"a plain starter part takes one press");
            StringAssert.Contains("Sold Scrap Edge for 20 scrip.",Feedback());Assert.AreEqual(scrip+20,State.scrip);
            With("Refined Frame").onClick.Invoke();yield return null;
            Assert.IsTrue(State.parts.Contains(refined),"one press sold a Refined part");StringAssert.Contains("40 scrip",Feedback());
            With("Refined Frame").onClick.Invoke();yield return null;
            Assert.IsFalse(State.parts.Contains(refined));StringAssert.Contains("Sold Refined Frame for 40 scrip.",Feedback());
            shop.Close();
        }
        /// <summary>ui237. The A that ended the conversation is often pressed twice, and focus now starts on a row that spends scrip.</summary>
        [UnityTest] public IEnumerator AStrayPressAsTheShopOpensSpendsNothing()
        {
            pad=InputSystem.AddDevice<Gamepad>();int scrip=State.scrip;
            var shop=ShopPanel;shop.Open(null);yield return null;
            yield return PadPress(GamepadButton.South);
            StringAssert.Contains("BUY",Label("Heading").text,"the press that ended the conversation also flipped the shop to Sell");
            Assert.AreEqual(scrip,State.scrip,"the press that ended the conversation also bought something");
            yield return new WaitForSecondsRealtime(.4f);
            yield return PadPress(GamepadButton.South);
            Assert.Less(State.scrip,scrip,"a deliberate press a moment later must buy");
            shop.Close();
        }
        /// <summary>ui237. Focus stays on a row after a sale, so the next part slid under it and a repeat press sold that too.</summary>
        [UnityTest] public IEnumerator ARepeatPressAfterASaleSellsNothingMore()
        {
            pad=InputSystem.AddDevice<Gamepad>();var inventory=RpgServices.Inventory;
            for(int i=0;i<3;i++)inventory.GivePart(Part("EdgesT1"));int parts=State.parts.Count;
            var shop=ShopPanel;shop.Open(null);yield return null;Row("ShopRow0").onClick.Invoke();yield return null;
            yield return new WaitForSecondsRealtime(.4f);
            EventSystem.current.SetSelectedGameObject(Row("ShopRow1").gameObject);
            yield return PadPress(GamepadButton.South);
            Assert.AreEqual(parts-1,State.parts.Count,"fixture: A sold the focused part");
            yield return PadPress(GamepadButton.South);
            Assert.AreEqual(parts-1,State.parts.Count,"a repeat press sold the next part, which slid under the cursor");
            yield return new WaitForSecondsRealtime(.4f);
            yield return PadPress(GamepadButton.South);
            Assert.AreEqual(parts-2,State.parts.Count,"a deliberate press a moment later must sell");
            shop.Close();
        }
        /// <summary>ui237's hold on the bench. It opens from a conversation on a row that spends materials, and that A is often pressed twice.</summary>
        [UnityTest] public IEnumerator AStrayPressAsTheBenchOpensCraftsNothing()
        {
            pad=InputSystem.AddDevice<Gamepad>();int held=State.materials.Values.Sum();
            var menu=Menu;menu.Open(PauseMenu.FabricatePage,true);yield return null;
            yield return PadPress(GamepadButton.South);
            Assert.AreEqual(held,State.materials.Values.Sum(),"the press that ended the conversation also crafted");
            yield return new WaitForSecondsRealtime(.4f);
            yield return PadPress(GamepadButton.South);
            Assert.Less(State.materials.Values.Sum(),held,"a deliberate press a moment later must craft");
            menu.Close();
        }

        // ------------------------------------------------------------------ the frame around every page
        /// <summary>ui243, copy30. Nothing marked the open tab, the tab buttons were hinted on one page, and nothing said how to change the quick item.</summary>
        [UnityTest] public IEnumerator TheOpenTabIsMarkedAndEveryPageSaysHowToDriveIt()
        {
            string[] tabs={"Party","Skills","Gear","Fabricate","Items","Quests","Settings"};var menu=Menu;
            foreach(var device in new[]{PromptDevice.Gamepad,PromptDevice.Keyboard})
            {
                PromptService.ForceDevice(device);yield return null;
                for(int page=0;page<tabs.Length;page++)
                {
                    menu.Open(page);yield return null;
                    for(int t=0;t<tabs.Length;t++)
                    {
                        var mark=Row("Tab_"+tabs[t]).transform.Find("ActiveTab");
                        if(t==page)Assert.IsNotNull(mark,$"nothing marks {tabs[t]} as the open tab");else Assert.IsNull(mark,$"{tabs[t]} is marked while {tabs[page]} is open");
                    }
                    var footer=Label("Footer");Assert.IsNotNull(footer,tabs[page]+" has no line saying which buttons drive it");
                    StringAssert.Contains(DeviceHints.Pages(),footer.text,tabs[page]+": the tab buttons aren't hinted");
                    StringAssert.Contains(PromptService.SubmitTag(),footer.text);
                    StringAssert.Contains((device==PromptDevice.Gamepad?"B":"Esc")+"  BACK",footer.text,tabs[page]+": nothing says how to back out");
                    StringAssert.DoesNotContain(device==PromptDevice.Gamepad?"Esc":"LB",footer.text,"the footer names the other device");
                    if(page==PauseMenu.ItemsPage)
                    {
                        StringAssert.Contains("Quick item",Note(),"nothing says how to change the quick item");
                        StringAssert.Contains(device==PromptDevice.Gamepad?"up/down":PromptService.Label("CycleItem","Positive"),Note());
                    }
                    menu.Close();yield return null;
                }
            }
        }
        /// <summary>ui231. A result kept following the player across tabs, in the same grey as the help text.</summary>
        [UnityTest] public IEnumerator FeedbackIsBrighterThanTheNoteAndLeavesWithThePage()
        {
            var menu=Menu;menu.Open(PauseMenu.FabricatePage);yield return null;
            With("Scrap Edge").onClick.Invoke();yield return null;
            var said=Label("Feedback");Assert.IsNotNull(said,"a result has no line of its own: it reads as more help text");
            StringAssert.Contains("Lattice Anvil",said.text);
            float Bright(Color c)=>c.r+c.g+c.b;
            Assert.Greater(Bright(said.color),Bright(Label("Note").color),"a refusal is as dim as the help beside it");
            string words=said.text;
            Row("Tab_Items").onClick.Invoke();yield return null;
            Assert.IsEmpty(Feedback(),"a result followed the player to another tab");StringAssert.DoesNotContain(words,Note());
            menu.Close();
        }

        // ------------------------------------------------------------------ items
        /// <summary>ui232, copy16. Refused uses sounded accepted and said nothing, spent stacks stayed at ×0, and the page hid the numbers that decide.</summary>
        [UnityTest] public IEnumerator ItemsSayWhatHappenedAndSpentStacksLeave()
        {
            var hero=PartyController.Current.Active;hero.Health.integrity=hero.Health.maximum;hero.charge=100;
            State.consumables["RepairGel"]=2;State.consumables["ChargeCell"]=1;
            var menu=Menu;menu.Open(PauseMenu.ItemsPage);yield return null;
            StringAssert.Contains($"Integrity {hero.Health.integrity:0}/{hero.Health.maximum:0}",Note(),"the Items page hides the numbers that decide whether to use something");
            StringAssert.Contains("Charge 100/100",Note());
            using var log=new CueLog();
            With("Repair Gel").onClick.Invoke();yield return null;
            StringAssert.Contains("Integrity is already full.",Feedback());Assert.AreEqual(2,State.consumables["RepairGel"],"a refused gel was spent");
            Assert.AreEqual(1,log.Count("ui_error"),"a refused use sounded accepted: "+log.All);Assert.AreEqual(0,log.Count("ui_confirm"),log.All);
            With("Charge Cell").onClick.Invoke();yield return null;
            StringAssert.Contains("Charge is already full.",Feedback());
            hero.Health.integrity=10;
            With("Repair Gel").onClick.Invoke();yield return null;
            StringAssert.Contains("Used Repair Gel on Taren.",Feedback());Assert.AreEqual(1,State.consumables["RepairGel"]);
            // The last one, pressed for real. Rows go by place, so the Charge Cell is now under the cursor, and it's not full.
            pad=InputSystem.AddDevice<Gamepad>();hero.Health.integrity=10;hero.charge=50;
            EventSystem.current.SetSelectedGameObject(With("Repair Gel").gameObject);
            yield return PadPress(GamepadButton.South);
            Assert.AreEqual(0,State.consumables["RepairGel"],"fixture: A used the focused gel");
            Assert.IsFalse(Buttons().Any(b=>Words(b).Contains("Repair Gel")),"a spent stack stays listed at ×0");
            StringAssert.Contains("Charge Cell",Words(Selected().transform),"fixture: the next item took the row");
            yield return PadPress(GamepadButton.South);
            Assert.AreEqual(1,State.consumables["ChargeCell"],"the press after the last gel landed on the next item and spent it");
            yield return new WaitForSecondsRealtime(.4f);
            yield return PadPress(GamepadButton.South);
            Assert.AreEqual(0,State.consumables["ChargeCell"],"the menu stays deaf after a stack runs out");
            menu.Close();
        }
        /// <summary>ui232. The swap row did nothing while the partner was down, with a confirm sound.</summary>
        [UnityTest] public IEnumerator SwappingToADownedPartnerSaysSo()
        {
            var party=PartyController.Current;var partner=party.members[1-party.index];
            partner.Health.Receive(new DamagePacket{amount=partner.Health.maximum*10});yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(partner.Health.Alive,"fixture: the partner went down");
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            With("Swap active member").onClick.Invoke();yield return null;
            StringAssert.Contains("Sela is down.",Feedback(),"the row did nothing and said nothing");
            menu.Close();
        }

        // ------------------------------------------------------------------ rebinding
        /// <summary>ui215. Closing mid-listen left the action disabled, nothing showed a listen was on, and the pad kept navigating under it.</summary>
        [UnityTest] public IEnumerator ARebindListenSaysSoAndLeavingGivesTheActionBack()
        {
            var attack=GameInput.Current.Ground.FindAction("Attack");var dodge=GameInput.Current.Ground.FindAction("Dodge");
            var menu=Menu;yield return Bindings(menu);
            With("Attack").onClick.Invoke();yield return null;
            StringAssert.Contains("Press a key for Attack",Note(),"nothing on screen says the game is waiting for a key");
            Assert.IsTrue(menu.Listening);Assert.IsFalse(attack.enabled,"fixture: a listen holds its action");
            Assert.IsFalse(EventSystem.current.sendNavigationEvents,"the pad keeps moving the cursor under a listen");
            menu.Close();yield return null;
            Assert.IsTrue(attack.enabled,"Attack stays dead after leaving the menu during a listen");
            Assert.IsTrue(EventSystem.current.sendNavigationEvents,"the menus stay deaf after a listen");Assert.IsFalse(menu.Listening);
            yield return Bindings(menu);
            With("Attack").onClick.Invoke();yield return null;With("Dodge").onClick.Invoke();yield return null;
            Assert.IsTrue(attack.enabled,"a second row left the first action dead");Assert.IsFalse(dodge.enabled);
            menu.StepBack();yield return null;
            Assert.IsTrue(dodge.enabled,"Back during a listen left the action dead");Assert.IsTrue(menu.IsOpen,"Back during a listen only ends the listen");Assert.IsFalse(menu.Listening);
            // Esc is the listen's own way out, on the same key as Pause and Cancel: it ends the listen and goes no further.
            keyboard=InputSystem.AddDevice<Keyboard>();
            With("Attack").onClick.Invoke();yield return null;Assert.IsTrue(menu.Listening,"fixture: the listen started again");
            yield return PressKey(Key.Escape);
            Assert.IsFalse(menu.Listening,"Esc doesn't end a listen");Assert.IsTrue(attack.enabled,"Esc during a listen left the action dead");
            Assert.IsTrue(menu.IsOpen,"the Esc that ended the listen also closed the menu");With("Back to sound controls");
            menu.Close();
        }
        /// <summary>ui216, copy12. A rebind moved one form's action only, while the rows never said which key anything was on.</summary>
        [UnityTest] public IEnumerator ARebindMovesBothFormsAndTheRowShowsIt()
        {
            PromptService.ForceDevice(PromptDevice.Keyboard);yield return null;keyboard=InputSystem.AddDevice<Keyboard>();
            var input=GameInput.Current;var menu=Menu;yield return Bindings(menu);
            StringAssert.Contains("Tab",Words(With("Swap")),"a binding row doesn't say which key it's on");
            With("Swap").onClick.Invoke();yield return null;
            yield return PressKey(Key.G);
            Assert.IsFalse(menu.Listening,"the listen never took the key");
            Assert.AreEqual("<keyboard>/g",KeyPath(input.Ground,"Swap"));Assert.AreEqual("<keyboard>/g",KeyPath(input.Flight,"Swap"),"a rebind on foot doesn't reach flight, where the prompt will name it");
            StringAssert.Contains("G",Words(With("Swap")));
            With("Attack").onClick.Invoke();yield return null;
            yield return PressKey(Key.K);
            Assert.AreEqual("<keyboard>/k",KeyPath(input.Ground,"Attack"));Assert.AreEqual("<keyboard>/k",KeyPath(input.Flight,"Fire"),"Attack and Fire are one button");
            StringAssert.Contains("Guard / Lunge",Feedback(),"a key now doing two jobs passed without a word");
            input.SetFlight(true);
            try{StringAssert.Contains("G",PromptService.Tag("Swap"),"the flight prompt doesn't name the new key");}
            finally{input.SetFlight(false);}
            menu.Close();
        }
        /// <summary>ui216, copy12. Bindings got new ids every launch, and a saved rebind finds its binding by id, so the key went back on restart.</summary>
        [UnityTest] public IEnumerator ARebindSurvivesARestart()
        {
            PromptService.ForceDevice(PromptDevice.Keyboard);yield return null;keyboard=InputSystem.AddDevice<Keyboard>();
            var menu=Menu;yield return Bindings(menu);
            With("Swap").onClick.Invoke();yield return null;yield return PressKey(Key.G);
            With("Attack").onClick.Invoke();yield return null;yield return PressKey(Key.H);
            menu.Close();
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            var input=GameInput.Current;
            Assert.AreEqual("<keyboard>/g",KeyPath(input.Ground,"Swap"),"the key went back to Tab on the next launch");Assert.AreEqual("<keyboard>/g",KeyPath(input.Flight,"Swap"),"the new key came back on foot but not in flight");
            Assert.AreEqual("<keyboard>/h",KeyPath(input.Ground,"Attack"),"the second rebind went back on the next launch");Assert.AreEqual("<keyboard>/h",KeyPath(input.Flight,"Fire"),"Attack and Fire are one button, on the next launch too");
        }
        /// <summary>ui216. In flight the Attack, Dodge and Guard rows did nothing, with a confirm sound.</summary>
        [UnityTest] public IEnumerator RebindingWorksInFlight()
        {
            yield return Load("Arena_Flight");var fire=GameInput.Current.Flight.FindAction("Fire");Assert.IsTrue(fire.enabled,"fixture: the flight map is live");
            var menu=Menu;yield return Bindings(menu);
            With("Attack").onClick.Invoke();yield return null;
            Assert.IsFalse(fire.enabled,"in flight the Attack row starts no listen");
            menu.StepBack();yield return null;Assert.IsTrue(fire.enabled,"the listen left Fire dead");
            menu.Close();
        }
        /// <summary>ui216, copy12. The prompt cache isn't keyed by map: the HUD named a key that only works in the other form.</summary>
        [UnityTest] public IEnumerator APromptDoesNotOutliveItsMap()
        {
            PromptService.ForceDevice(PromptDevice.Keyboard);yield return null;
            var input=GameInput.Current;var interact=input.Ground.FindAction("Interact");
            int key=interact.bindings.ToList().FindIndex(b=>b.path.StartsWith("<Keyboard>"));
            interact.ApplyBindingOverride(key,"<Keyboard>/g");PromptService.NotifyBindingsChanged();
            string onFoot=PromptService.Tag("Interact");
            input.SetFlight(true);
            try{Assert.AreNotEqual(onFoot,PromptService.Tag("Interact"),"after a map change the HUD still names the other map's key");}
            finally{input.SetFlight(false);}
        }
    }
}
