using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The loot and progression layer in play (S2, D223): what a stall sells and buys, what the Gear and Fabricate pages let a player see and do, what a consumable does
    /// and how often, what a module's passive and an affix do in a fight, what an enemy and a cache pay. ProgressionTests (EditMode) holds the sums; these are the flows.
    /// Each check fails on the build before: a stall with no inventory sold the catalogue, nothing was capped or paced, gear changed five numbers and nothing else,
    /// an upgrade was made from the pause menu for Scrap Alloy alone, and no enemy dropped a part.</summary>
    public sealed class QaProgressionTests
    {
        static GameState State=>GameServices.Current.State;
        static PauseMenu Menu=>Object.FindFirstObjectByType<PauseMenu>();
        static ShopUi Shop=>Object.FindFirstObjectByType<ShopUi>();
        static PartyController Party=>PartyController.Current;
        static CombatActor Body(string id)=>Party.members.First(m=>m.character==id);
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        static T Def<T>(string id)where T:ScriptableObject
        {var def=GameCatalog.Find<T>(id);Assert.IsNotNull(def,$"fixture: no {typeof(T).Name} named {id}. Run Lattice.EditorTools.ProgressionBuilder.Build.");return def;}
        static int Held(string id)=>State.materials.TryGetValue(id,out int n)?n:0;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in Party.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}

        // ------------------------------------------------------------------ what's on screen
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static string Words(Component c)=>ListRow.Words(c);
        static string Plain(string words)=>Regex.Replace(words,"<[^>]+>","");
        static string Shown()=>string.Join(" | ",Buttons().Select(b=>b.name+": "+Words(b)));
        static Button With(string words)
        {
            var found=Buttons().FirstOrDefault(b=>Plain(Words(b)).Contains(words));
            Assert.IsNotNull(found,$"no row says \"{words}\". On screen: {Shown()}");return found;
        }
        /// <summary>The numbered rows of a page, in order: a pause page's from the given number up, a shop's offers.</summary>
        static Button[] Numbered(string prefix,int from)=>Buttons().Where(b=>b.name.StartsWith(prefix)&&int.TryParse(b.name.Substring(prefix.Length),out int n)&&n>=from).OrderBy(b=>int.Parse(b.name.Substring(prefix.Length))).ToArray();
        static GameObject Selected(){var s=EventSystem.current.currentSelectedGameObject;Assert.IsNotNull(s,"nothing has focus: a pad player is stuck");return s;}
        static TMP_Text Label(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name&&t.gameObject.activeInHierarchy);
        static string Note(){var t=Label("Note");Assert.IsNotNull(t,"the page has no note");return Plain(t.text);}
        static string Said(){var t=Label("Feedback");return t!=null?Plain(t.text):"";}
        static void Focus(Component row){EventSystem.current.SetSelectedGameObject(row.gameObject);}
        /// <summary>A note says what it has to inside its own box, at the type floor: a fourth line would be drawn over the footer.</summary>
        static void Fits(string name,string surface)
        {
            var t=Label(name);Assert.IsNotNull(t,surface+": no "+name);var box=t.rectTransform.rect;
            Assert.GreaterOrEqual(t.fontSize,24,surface+": the "+name+" is under the type floor");
            Assert.LessOrEqual(t.GetPreferredValues(t.text,box.width,0).y,box.height+1,$"{surface}: the {name} runs out of its box: \"{Plain(t.text)}\"");
        }
        static void RowsFit(string surface)
        {
            foreach(var button in Buttons())
            {
                var text=button.GetComponentInChildren<TMP_Text>();if(text==null)continue;var box=text.rectTransform.rect;
                var need=text.GetPreferredValues(text.text,box.width-text.margin.x-text.margin.z,0);
                Assert.LessOrEqual(need.y,box.height+1,$"{surface}: \"{Plain(text.text)}\" needs {need.y:0} in a {box.height:0} tall row");
            }
        }
        static IEnumerator Action(int presses){for(int i=0;i<presses;i++){Row("Row_1").onClick.Invoke();yield return null;}}
        static IEnumerator Press(string row,int presses){for(int i=0;i<presses;i++){Row(row).onClick.Invoke();yield return null;}}
        /// <summary>Every offer a stall shows, across its pages, in order.</summary>
        static IEnumerator Offers(List<string> into)
        {
            into.Clear();
            for(int page=0;page<8;page++)
            {
                into.AddRange(Numbered("ShopRow",1).Select(b=>Plain(Words(b))));
                var pager=Row("Pager");if(pager==null)yield break;
                pager.onClick.Invoke();yield return null;
                if(Words(Row("Pager")).Contains("(1/"))yield break;
            }
        }

        // ------------------------------------------------------------------ fixtures in the world
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Progression fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(actor.motor is GroundMotor ground)ground.KeepFacing(Vector3.forward);
            Physics.SyncTransforms();
        }
        /// <summary>Taren leading on open floor, Sela well away from him, both whole.</summary>
        static IEnumerator Stage()
        {
            Arena();if(Party.Active.character!="Taren")Assert.IsTrue(Party.Swap(),"fixture: the swap");
            foreach(var member in Party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            Place(Body("Sela"),new Vector3(260,0,0));Place(Body("Taren"),new Vector3(200,0,0));yield return Frames(2);
            foreach(var member in Party.members)member.Health.integrity=member.Health.maximum;
        }
        /// <summary>A body that never dies or breaks and is neither weak to nor resists anything a check throws at it.</summary>
        static EnemyBrain Dummy(Vector3 at)
        {
            var enemy=ActorFactory.Enemy(Def<EnemyDef>("Ridgehound"),at);enemy.Passive=true;var body=enemy.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;
            body.weakness=body.resistance=DamageType.Kinetic;return enemy;
        }
        static float Hit(CombatActor from,Health on,DamageType type=DamageType.Beam,float amount=100,float breakPower=0)
        {float before=on.integrity;on.Receive(new DamagePacket{source=from.Health,amount=amount,type=type,breakPower=breakPower});return before-on.integrity;}
        static float Take(CombatActor hero,float amount){float before=hero.Health.integrity;hero.Health.Receive(new DamagePacket{amount=amount,type=DamageType.Pulse});return before-hero.Health.integrity;}
        static PartInstance Wear(string hero,string id,params string[] affixes)
        {
            var def=Def<TechPartDef>(id);var bag=RpgServices.Inventory;var member=Member(hero);var item=bag.GivePart(def);item.affixes.AddRange(affixes);
            bool second=def.slot==GearSlot.Module&&member.equipped.ContainsKey(GearSlot.Module);
            Assert.IsTrue(bag.Equip(member,item,def,second),$"fixture: {id} goes on {hero}");Party.RefreshStats();return item;
        }

        // ------------------------------------------------------------------ item 9: stalls
        [UnityTest] public IEnumerator AStallSellsItsOwnOffersAtTheirOwnPricesAndOneWithNoStockSellsNothing()
        {
            var shop=Shop;var said=new List<string>();State.scrip=5000;
            foreach(string id in new[]{"Hub_Decks","Lanternwick","Rootmarket_Hadda"})
            {
                var stall=Def<ShopInventory>(id);var stock=ShopUi.Stock(stall,State).ToArray();
                shop.Open(stall);yield return null;yield return Offers(said);
                Assert.AreEqual(stock.Length,said.Count,$"{id} shows {said.Count} offers of its {stock.Length}: {string.Join(" | ",said)}");
                for(int i=0;i<stock.Length;i++)
                {
                    StringAssert.StartsWith(DisplayNames.Item(stock[i].id),said[i],$"{id}'s offers aren't in the order the stall keeps them");
                    StringAssert.Contains($"  ·  {stock[i].price} scrip",said[i],$"{id}: {stock[i].id} is shown at another offer's price. The prices were one list read by position after the parts were sorted by slot");
                }
                RowsFit(id);shop.Close();yield return null;
            }
            // A talk that opens a shop in a place nobody stocked: it listed every tier 1 part at 60 and every consumable in the catalogue at 20, thirteen of them now. Its counter is bare.
            var nobody=ShopUi.For("NoSuchTown","Nobody");Assert.IsNotNull(nobody);Assert.IsEmpty(ShopUi.Stock(nobody,State).ToArray());
            shop.Open(nobody);yield return null;
            Assert.IsEmpty(Numbered("ShopRow",1),"a stall with no inventory still sells: "+Shown());StringAssert.Contains("Nothing on the counter",Said());
            Assert.AreEqual("ShopRow0",Selected().name,"an empty stall leaves the pad with nothing under it");
            shop.Close();yield return null;
            // With no inventory named at all it's the outfitter's, as ever: Mira's own stock, and not one consumable from a later chapter.
            shop.Open(null);yield return null;yield return Offers(said);
            Assert.AreEqual(Def<ShopInventory>("Hub_Decks").offers.Length,said.Count,"the outfitter's stall isn't Mira's own stock: "+string.Join(" | ",said));
            Assert.IsFalse(said.Any(w=>w.StartsWith("Repair Salve")||w.StartsWith("Relink Spike")||w.StartsWith("Surge Draught")||w.StartsWith("Pod Stew")),"the outfitter sells the whole catalogue of consumables");
            shop.Close();yield return null;
            // Bought from a stall, a part comes clean and the price on its row is what's charged.
            Loot.Seed(3);State.scrip=10000;shop.Open(Def<ShopInventory>("Rootmarket_Hadda"));yield return null;
            for(int i=0;i<12;i++){With("Prime Edge").onClick.Invoke();yield return null;}
            var bought=State.parts.Where(p=>p.definitionId=="EdgesT3").ToArray();
            Assert.AreEqual(12,bought.Length,"fixture: twelve Prime Edges bought");Assert.IsTrue(bought.All(p=>p.affixes.Count==0),"a shop's part rolled an affix: what's found or made is meant to be able to beat what's bought");
            Assert.AreEqual(10000-12*600,State.scrip,"the row's price isn't what was charged");
            shop.Close();
        }
        [UnityTest] public IEnumerator AStallShowsAModulesPassiveAndTheWholeComparisonUnderItsRows()
        {
            Wear("Taren","ModulesT2");
            var shop=Shop;shop.Open(Def<ShopInventory>("Rootmarket_Hadda"));yield return null;
            var row=With("Guard Capacitor");StringAssert.Contains("720 scrip",Words(row));
            Focus(row);yield return null;
            StringAssert.Contains("Guarding costs 40% less Thrust",Said(),"nothing at the counter says what a module with a passive does: its row has room for a stat and a price");
            StringAssert.Contains("a Flash pays 15 more Charge",Said());
            StringAssert.Contains("Against Taren's Refined Module: Resonance -4, Fortune -6.",Said(),"the counter sets one stat against what's worn, and a module is two or three");
            Fits("Feedback","Hadda's counter, on the Guard Capacitor");
            // The answer to a press isn't talked over by the row it lands on, and the next move lets the row speak again.
            State.scrip=0;row.onClick.Invoke();yield return null;
            StringAssert.Contains("Not enough scrip",Said(),"the row's own note covers the refusal the press just earned");
            Focus(With("Prime Emitter"));yield return null;StringAssert.Contains("slot is empty",Said(),"after a refusal the rows stop saying what they are");
            shop.Close();
        }
        [UnityTest] public IEnumerator AStallRefusesWhatThePouchCantHoldAndSaysHowManyAreCarried()
        {
            var gel=Def<ConsumableDef>("RepairGel");int cap=Inventory.Cap(gel);State.scrip=1000;State.consumables["RepairGel"]=cap-1;
            var shop=Shop;shop.Open(Def<ShopInventory>("Lanternwick"));yield return null;
            StringAssert.Contains($"{cap-1} of {cap}",Words(With("Repair Gel")),"the counter doesn't say how many are carried, or how many can be");
            With("Repair Gel").onClick.Invoke();yield return null;
            Assert.AreEqual(cap,State.consumables["RepairGel"],"fixture: the last one the pouch holds");Assert.AreEqual(1000-22,State.scrip);
            StringAssert.Contains($"{cap} of {cap}",Words(With("Repair Gel")));
            With("Repair Gel").onClick.Invoke();yield return null;
            Assert.AreEqual(cap,State.consumables["RepairGel"],"a stall sold a gel past the carry cap (prog159)");Assert.AreEqual(1000-22,State.scrip,"a refused purchase spent scrip");
            StringAssert.Contains("can't carry more Repair Gel",Said(),"the refusal doesn't say why");
            shop.Close();
        }
        [UnityTest] public IEnumerator AStallBuysSpareMaterialsFiveAtATimeAndAsksTwiceForARareOne()
        {
            var scrap=Def<MaterialDef>("ScrapAlloy");var pearl=Def<MaterialDef>("CantorPearl");Assert.IsTrue(pearl.rare,"fixture: the Cantor's pearl is rare");
            State.materials["ScrapAlloy"]=12;State.materials["CantorPearl"]=1;int purse=State.scrip;
            var shop=Shop;shop.Open(Def<ShopInventory>("Lanternwick"));yield return null;
            StringAssert.Contains("materials",Words(Row("ShopRow0")),"the page toggle doesn't say materials sell here");
            Row("ShopRow0").onClick.Invoke();yield return null;
            StringAssert.Contains("spare materials",Said());
            With("Scrap Alloy ×12").onClick.Invoke();yield return null;
            Assert.AreEqual(12-ShopUi.MaterialLot,Held("ScrapAlloy"),"a press on a common material doesn't sell five (prog160: scrip and materials had nowhere to go)");
            Assert.AreEqual(purse+ShopUi.MaterialLot*scrap.value,State.scrip,"the sale didn't pay the material's value");StringAssert.Contains("Sold 5 Scrap Alloy",Said());
            With("Cantor Pearl").onClick.Invoke();yield return null;
            Assert.AreEqual(1,Held("CantorPearl"),"one press sold a boss's trophy");StringAssert.Contains("Press again",Said());
            With("Cantor Pearl").onClick.Invoke();yield return null;
            Assert.AreEqual(0,Held("CantorPearl"),"the second press didn't sell");Assert.AreEqual(purse+ShopUi.MaterialLot*scrap.value+pearl.value,State.scrip);
            Assert.IsFalse(Buttons().Any(b=>Words(b).StartsWith("Cantor Pearl")),"a sold-out material stays on the counter at ×0");
            RowsFit("the sell page");shop.Close();
        }

        // ------------------------------------------------------------------ item 11: the Gear page
        [UnityTest] public IEnumerator TheGearPageFiltersBySlotAndItsNoteSaysWhatARowCant()
        {
            var bag=RpgServices.Inventory;Wear("Taren","EdgesT2");
            var ember=bag.GivePart(Def<TechPartDef>("EdgesT3Plasma"));ember.affixes.Add("Keen");ember.affixes.Add("Vicious");
            bag.GivePart(Def<TechPartDef>("ModulesGuardCapacitor"));bag.GivePart(Def<TechPartDef>("FramesT1"));bag.GivePart(Def<TechPartDef>("EmittersT1"));bag.GivePart(Def<TechPartDef>("EmittersT2"));
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return null;
            Assert.AreEqual("Row_1",Selected().name,"the Gear page no longer opens on its Action row");
            var filter=Row("Filter");Assert.IsNotNull(filter,"the Gear page has no slot filter: one list held every part the party owns. On screen: "+Shown());
            StringAssert.Contains("every slot",Words(filter));
            Assert.AreEqual("Filter",Selected().GetComponent<Selectable>().FindSelectableOnUp().name,"the filter isn't one press up from where the page opens");
            Assert.AreEqual(6,Numbered("Row_",2).Length,"fixture: six parts, all shown");
            filter.onClick.Invoke();yield return null;
            StringAssert.Contains("Emitters",Words(Row("Filter")));Assert.AreEqual("Filter",Selected().name,"turning the filter moved the cursor off it, onto a part");
            var rows=Numbered("Row_",2);Assert.AreEqual(2,rows.Length,"the Emitters filter doesn't show the two emitters: "+Shown());Assert.IsTrue(rows.All(r=>Words(r).Contains("Emitter")));
            Row("Filter").onClick.Invoke();yield return null;
            rows=Numbered("Row_",2);Assert.AreEqual(2,rows.Length,"the Edges filter doesn't show the two edges: "+Shown());Assert.IsTrue(rows.All(r=>Words(r).Contains("Edge")));
            // The row names what it rolled; the note under the list says what those do and sets all its stats against what's worn.
            var row=With("Prime Ember Edge");StringAssert.Contains("Keen",Words(row));StringAssert.Contains("Vicious",Words(row));
            Focus(row);yield return null;
            StringAssert.Contains("Keen: crit chance +5%",Note(),"no screen says what an affix does");StringAssert.Contains("Vicious: crits hit 25% harder",Note());
            StringAssert.Contains("Against Taren's Refined Edge: Output +16.",Note(),"the note doesn't set the part against what's worn");
            Fits("Note","Gear, on a two-affix edge");RowsFit("Gear, Edges");
            yield return Press("Filter",3);StringAssert.Contains("Modules",Words(Row("Filter")));
            Focus(With("Guard Capacitor"));yield return null;
            StringAssert.Contains("Guarding costs 40% less Thrust, a Flash pays 15 more Charge",Note(),"a module's passive is on no screen");StringAssert.Contains("Taren's Module slot is empty.",Note());
            // Off the parts, the note is the page's own again.
            Focus(Row("Row_1"));yield return null;StringAssert.Contains("Taren wears Refined Edge",Note(),"the note stays on the last part looked at");
            yield return Press("Filter",1);StringAssert.Contains("every slot",Words(Row("Filter")),"the filter doesn't come back round to everything");
            menu.Close();yield return null;menu.Open(PauseMenu.GearPage);yield return null;
            StringAssert.Contains("every slot",Words(Row("Filter")),"the filter is left on from the last visit: parts look gone");
            menu.Close();
        }
        [UnityTest] public IEnumerator AModulesStatsAreSetAgainstTheSlotItWouldGoIn()
        {
            Wear("Taren","ModulesT1");Wear("Taren","ModulesT2");var spare=RpgServices.Inventory.GivePart(Def<TechPartDef>("ModulesT3"));
            Assert.AreEqual(State.parts.Find(p=>p.definitionId=="ModulesT2").instanceId,Member("Taren").module2,"fixture: the Refined Module is in the second slot");
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return null;
            Focus(With("Prime Module"));yield return null;
            StringAssert.Contains("Against Taren's Scrap Module: Resonance +10, Fortune +6.",Note());
            yield return Action(1);StringAssert.Contains("module slot 2",Words(Row("Row_1")));
            Focus(With("Prime Module"));yield return null;
            StringAssert.Contains("Against Taren's Refined Module: Resonance +5, Fortune +3.",Note(),"in the second-slot mode the note still compares against the first module");
            Fits("Note","Gear, module slot 2");menu.Close();
            Assert.IsTrue(State.parts.Contains(spare));
        }
        [UnityTest] public IEnumerator AnUpgradeIsBenchWorkAndItsNoteSaysWhatTheNextLevelCosts()
        {
            var bag=RpgServices.Inventory;var prime=Def<TechPartDef>("EdgesT3");var edge=bag.GivePart(prime);
            State.materials["ScrapAlloy"]=9;State.scrip=75;State.materials[prime.upgradeMaterial]=0;
            var menu=Menu;menu.Open(PauseMenu.GearPage,true);yield return null;
            yield return Action(2);StringAssert.Contains("Action: Upgrade",Words(Row("Row_1")));
            var row=With("Prime Edge");Focus(row);yield return null;
            StringAssert.Contains("+1 costs Scrap Alloy ×9 (have 9), "+DisplayNames.Item(prime.upgradeMaterial)+" ×1 (have 0), 75 scrip.",Note(),"the price of an upgrade is seen after paying, or not at all");
            Fits("Note","Gear, Upgrade");
            // Short of the region's material: refused, with what's short, and nothing spent.
            row.onClick.Invoke();yield return null;
            Assert.AreEqual(0,edge.upgrade,"a Prime part was upgraded without its region's material");Assert.AreEqual(9,Held("ScrapAlloy"));Assert.AreEqual(75,State.scrip);
            StringAssert.Contains(DisplayNames.Item(prime.upgradeMaterial)+" ×1 (have 0)",Said(),"the refusal doesn't say what's short");
            State.materials[prime.upgradeMaterial]=1;With("Prime Edge").onClick.Invoke();yield return null;
            Assert.AreEqual(1,edge.upgrade,"an upgrade with its whole price in hand at a bench is refused");Assert.AreEqual(0,Held("ScrapAlloy"));Assert.AreEqual(0,State.scrip);Assert.AreEqual(0,Held(prime.upgradeMaterial));
            menu.Close();
        }
        [UnityTest] public IEnumerator ARetuneIsOfferedAtABenchAndRollsAnAffixAgainOnTheSecondPress()
        {
            var bag=RpgServices.Inventory;var def=Def<TechPartDef>("EdgesT2");var item=bag.GivePart(def);item.affixes.Add("Keen");State.scrip=1000;
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return null;
            yield return Action(4);StringAssert.Contains("Action: Equip",Words(Row("Row_1")),"in the field the action no longer turns through the four it always had");
            Assert.IsFalse(Words(Row("Row_1")).Contains("Re-tune"));
            menu.Close();yield return null;menu.Open(PauseMenu.GearPage,true);yield return null;
            yield return Action(4);StringAssert.Contains("Action: Re-tune",Words(Row("Row_1")),"a bench doesn't offer a re-tune");
            var row=With("Keen: crit chance +5%");StringAssert.StartsWith("Refined Edge  ·  T2",Words(row),"a re-tune row doesn't lead with its part");
            Focus(row);yield return null;StringAssert.Contains("A re-tune costs 150 scrip.",Note());
            Loot.Seed(5);row.onClick.Invoke();yield return null;
            Assert.AreEqual("Keen",item.affixes[0],"one press re-tuned an affix");Assert.AreEqual(1000,State.scrip);StringAssert.Contains("Press again to re-tune Keen on Refined Edge for 150 scrip",Said());
            With("Keen: crit chance").onClick.Invoke();yield return null;
            Assert.AreNotEqual("Keen",item.affixes[0],"the second press didn't re-tune");Assert.AreEqual(850,State.scrip,"a re-tune didn't take its scrip");
            Assert.IsTrue(def.affixPool.Any(a=>a.id==item.affixes[0]),"a re-tune rolled an affix the part can't carry");
            StringAssert.Contains("Re-tuned Refined Edge: Keen became "+GameCatalog.Find<AffixDef>(item.affixes[0]).displayName,Said(),"nothing says what the affix became");
            // Too poor: refused, with the price.
            State.scrip=10;string now=item.affixes[0];
            Numbered("Row_",2).First(r=>Words(r).StartsWith("Refined Edge")).onClick.Invoke();yield return null;
            Assert.AreEqual(now,item.affixes[0]);Assert.AreEqual(10,State.scrip);StringAssert.Contains("takes",Said());StringAssert.Contains("150 scrip",Said());
            RowsFit("Gear, Re-tune");menu.Close();
        }

        // ------------------------------------------------------------------ item 11: the bench
        [UnityTest] public IEnumerator TheBenchFiltersByKindAndListsALaterRegionsRecipesOnceItsMaterialIsHeld()
        {
            var menu=Menu;menu.Open(PauseMenu.FabricatePage,true);yield return null;
            Assert.AreEqual("Row_0",Selected().name,"the bench no longer opens on the row that spends nothing (QA-036)");
            var kind=Row("Kind");Assert.IsNotNull(kind,"the bench has no filter by kind: "+Shown());StringAssert.Contains("every kind",Words(kind));
            Assert.AreEqual("Kind",Selected().GetComponent<Selectable>().FindSelectableOnUp().name,"the filter isn't one press up from where the bench opens");
            yield return Press("Kind",2);StringAssert.Contains("Edges",Words(Row("Kind")));Assert.AreEqual("Kind",Selected().name,"turning the filter moved the cursor onto a recipe");
            var edges=Numbered("Row_",1).Select(r=>Plain(Words(r))).ToArray();
            Assert.AreEqual(2,edges.Length,"a new party's bench lists more than the Scrap and Refined Edge under Edges: "+string.Join(" | ",edges));
            Assert.IsFalse(edges.Any(w=>w.Contains("Prime")),"the bench lists a recipe made from a plant nobody has seen");
            menu.Close();yield return null;
            // With Xylos's materials in the bag its recipes are there, and the third tier opens with the ladder.
            var bag=RpgServices.Inventory;bag.Give("Rootfibre",10);bag.Give("AmberSap",10);State.craftingXp["Edges"]=30;
            menu.Open(PauseMenu.FabricatePage,true);yield return null;yield return Press("Kind",2);
            var prime=With("Prime Edge");StringAssert.Contains("Needs Edges 3",Words(prime),"a Prime recipe on a ladder at 2 doesn't say what it's waiting for");
            menu.Close();yield return null;State.craftingXp["Edges"]=60;
            menu.Open(PauseMenu.FabricatePage,true);yield return null;yield return Press("Kind",2);
            Assert.IsTrue(Numbered("Row_",1).All(r=>Words(r).Contains("Edge")),"the Edges filter shows something else: "+Shown());
            prime=With("Prime Edge");StringAssert.Contains("Rootfibre ×4",Plain(Words(prime)),"the row doesn't say what the recipe takes");
            Focus(prime);yield return null;StringAssert.Contains("Prime Edge: Output +50",Note(),"the note doesn't say what the recipe under the cursor makes");
            StringAssert.Contains("10 Rootfibre",Note(),"nothing on the bench says what's held of what the recipe takes");
            Fits("Note","the bench, on a Prime recipe");RowsFit("the bench, Edges");
            prime.onClick.Invoke();yield return null;
            Assert.IsTrue(State.parts.Any(p=>p.definitionId=="EdgesT3"),"a Prime Edge can't be made on a ladder at 3 with its materials in hand: the bench's page stopped at its own copy of the tier gate");
            StringAssert.Contains("Prime Edge",Said());Assert.AreEqual(6,Held("Rootfibre"));
            // Consumables alone.
            yield return Press("Kind",4);StringAssert.Contains("Consumables",Words(Row("Kind")));
            var items=Numbered("Row_",1).Select(r=>Plain(Words(r))).ToArray();
            Assert.IsTrue(items.Any(w=>w.StartsWith("Repair Salve")),"with Amber Sap in the bag the Repair Salve isn't on the bench: "+string.Join(" | ",items));
            Assert.IsFalse(items.Any(w=>w.Contains("Edge")||w.Contains("Emitter")||w.Contains("Frame")||w.Contains("Drive")||w.Contains("Module")),"the Consumables filter shows a part: "+string.Join(" | ",items));
            Assert.IsFalse(items.Any(w=>w.StartsWith("River Skewer")||w.StartsWith("Pod Stew")||w.StartsWith("Spice Cake")),"a meal can be fabricated: it's cooked and bought");
            menu.Close();
        }
        [UnityTest] public IEnumerator AConsumableIsNotFabricatedInAFightNorPastWhatThePouchHolds()
        {
            var hero=Party.Active;var gel=Def<ConsumableDef>("RepairGel");int cap=Inventory.Cap(gel);
            State.consumables["RepairGel"]=2;int scrap=Held("ScrapAlloy");hero.Health.InvulnerableUntil=GameTime.Now+120;
            var hound=ActorFactory.Enemy(Def<EnemyDef>("Ridgehound"),hero.transform.position+Vector3.forward*8);yield return Frames(3);
            Assert.IsTrue(GearEffects.InFight(hero.transform.position),"fixture: something awake is near");
            var menu=Menu;menu.Open(PauseMenu.FabricatePage);yield return null;yield return Press("Kind",6);
            StringAssert.Contains("Not in a fight",Plain(Words(With("Repair Gel"))),"the row doesn't say why it's grey");
            With("Repair Gel").onClick.Invoke();yield return null;
            Assert.AreEqual(2,State.consumables["RepairGel"],"a gel was fabricated from the pause menu with an enemy frozen mid-fight (prog159)");Assert.AreEqual(scrap,Held("ScrapAlloy"),"a refused craft spent its materials");
            StringAssert.Contains("Not in a fight",Said());StringAssert.Contains("until the fight is over",Note());
            menu.Close();yield return null;Object.Destroy(hound.gameObject);yield return Frames(3);
            Assert.IsFalse(GearEffects.InFight(hero.transform.position),"fixture: the fight is over");
            State.consumables["RepairGel"]=cap-1;
            menu.Open(PauseMenu.FabricatePage);yield return null;yield return Press("Kind",6);
            With("Repair Gel").onClick.Invoke();yield return null;
            Assert.AreEqual(cap,State.consumables["RepairGel"],"with the fight over and room in the pouch a gel can't be made");StringAssert.Contains($"You have {cap} of {cap}",Said());
            StringAssert.Contains($"Carrying {cap} of {cap}",Plain(Words(With("Repair Gel"))),"a full pouch isn't why the row is grey");
            scrap=Held("ScrapAlloy");With("Repair Gel").onClick.Invoke();yield return null;
            Assert.AreEqual(cap,State.consumables["RepairGel"],"the bench made a gel the pouch has no room for");Assert.AreEqual(scrap,Held("ScrapAlloy"));StringAssert.Contains("can't carry more Repair Gel",Said());
            menu.Close();
        }

        // ------------------------------------------------------------------ item 7: consumables
        [UnityTest] public IEnumerator TheItemsPageSetsEachCountAgainstItsCapAndSaysWhatADishDoes()
        {
            State.consumables["PodStew"]=3;State.consumables["SurgeDraught"]=2;State.consumables["RelinkSpike"]=1;
            var menu=Menu;menu.Open(PauseMenu.ItemsPage);yield return null;
            StringAssert.StartsWith($"Repair Gel ×{State.consumables["RepairGel"]} of 9",Words(With("Repair Gel")),"a count doesn't stand against the most that can be carried (prog159)");
            StringAssert.StartsWith("Pod Stew ×3 of 3",Words(With("Pod Stew")));
            foreach(var (name,does) in new[]{("Pod Stew","both heroes: restores 0.25% Integrity a second for 120 s"),("Surge Draught","both heroes: damage dealt +15% for 60 s"),("Relink Spike","raises a downed partner at 40% Integrity")})
            {
                var row=With(name);Focus(row);yield return null;
                StringAssert.Contains(does,Words(row)+" "+Note(),$"neither {name}'s row nor the note under it says what it does");
                Fits("Note","Items, on "+name);
            }
            RowsFit("Items");menu.Close();
        }
        [UnityTest] public IEnumerator ItemsWaitFourSecondsBetweenUsesInAFightAndNotBetweenFights()
        {
            var hero=Party.Active;var brain=hero.GetComponent<PlayerBrain>();State.quickItem="RepairGel";State.consumables["RepairGel"]=6;hero.Health.InvulnerableUntil=GameTime.Now+120;
            // Between fights: one after another, as a hurt party at rest needs.
            hero.Health.integrity=10;Assert.IsTrue(brain.UseItem("RepairGel").used,"fixture: a gel on a hurt hero");
            hero.Health.integrity=10;Assert.IsTrue(brain.UseItem("RepairGel").used,"a second gel is refused with nothing awake anywhere: the wait is for a fight");
            var hound=ActorFactory.Enemy(Def<EnemyDef>("Ridgehound"),hero.transform.position+Vector3.forward*8);yield return Frames(3);
            Assert.IsTrue(GearEffects.InFight(hero.transform.position),"fixture: something awake is near");
            ItemUse.Reset();hero.Health.integrity=10;Assert.IsTrue(brain.UseItem("RepairGel").used,"fixture: the first gel of the fight");
            hero.Health.integrity=10;var second=brain.UseItem("RepairGel");
            Assert.IsFalse(second.used,"two gels went down one after the other in a fight (prog159)");Assert.AreEqual(3,State.consumables["RepairGel"],"a refused gel was spent");
            StringAssert.Contains("Not yet",second.message);
            Assert.Greater(ItemUse.Wait(hero),ItemUse.Cooldown-1);Assert.LessOrEqual(ItemUse.Wait(hero),ItemUse.Cooldown);
            // The strip says so: the seconds after the count, greyed as a spent stack is.
            yield return Frames(2);var strip=Label("Skills");Assert.IsNotNull(strip,"fixture: the hint strip");
            StringAssert.IsMatch(@"REPAIR GEL ×3 \ds",strip.text,"the quick item doesn't show the wait: a refused press has no reason on screen");StringAssert.Contains("<color=#7D8894>",strip.text,"a quick item that would be refused looks ready");
            // A menu doesn't serve the wait: it runs on the fight's clock.
            float left=ItemUse.Wait(hero);var menu=Menu;menu.Open(PauseMenu.ItemsPage);yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(left,ItemUse.Wait(hero),.05f,"the wait runs down in the pause menu: pause, wait, drink");StringAssert.Contains("doesn't run in this menu",Note());
            menu.Close();yield return null;
            yield return new WaitForSeconds(ItemUse.Cooldown+.3f);
            Assert.AreEqual(0,ItemUse.Wait(hero),"the wait never ends");StringAssert.DoesNotContain("<color=",Label("Skills").text,"the quick item stays greyed after the wait");
            hero.Health.integrity=10;Assert.IsTrue(brain.UseItem("RepairGel").used,"after the wait a gel is still refused");
            Object.Destroy(hound.gameObject);
        }
        [UnityTest] public IEnumerator AHealIsAShareOfTheHerosOwnBarAndASpikeRaisesWhoIsDown()
        {
            yield return Stage();var taren=Body("Taren");var sela=Body("Sela");var brain=taren.GetComponent<PlayerBrain>();
            foreach(string id in new[]{"RepairGel","RepairSalve","SyncTonic","RelinkSpike"})State.consumables[id]=3;
            foreach(var member in State.party)member.level=20;Party.RefreshStats();float max=taren.Health.maximum;
            Assert.AreEqual(Levels.MaxIntegrity(20),max,.01f,"fixture: Sync 20");
            taren.Health.integrity=10;Assert.IsTrue(brain.UseItem("RepairGel").used);
            Assert.AreEqual(10+max*.25f,taren.Health.integrity,1,"a gel restores a flat 60 at Sync 20, a seventh of the bar: it stops being worth a press");
            taren.Health.integrity=10;Assert.IsTrue(brain.UseItem("RepairSalve").used);Assert.AreEqual(10+max*.5f,taren.Health.integrity,1,"a salve restores half the bar");
            // Both heroes at once.
            taren.Health.integrity=sela.Health.integrity=10;taren.charge=sela.charge=0;
            var tonic=brain.UseItem("SyncTonic");Assert.IsTrue(tonic.used,tonic.message);StringAssert.Contains("the party",tonic.message);
            Assert.AreEqual(10+max*.4f,taren.Health.integrity,1);Assert.AreEqual(10+sela.Health.maximum*.4f,sela.Health.integrity,1,"a Sync Tonic leaves the partner out");
            Assert.AreEqual(40,taren.charge,1);Assert.AreEqual(40,sela.charge,1);
            // A new hero's gel is still worth its 60.
            foreach(var member in State.party)member.level=1;Party.RefreshStats();taren.Health.integrity=10;
            Assert.IsTrue(brain.UseItem("RepairGel").used);Assert.AreEqual(70,taren.Health.integrity,1,"at Sync 1 a gel restores a quarter of 120, half of what it always did");
            // The spike: nothing to do with everyone standing, and a partner back on her feet when she's down.
            var idle=brain.UseItem("RelinkSpike");Assert.IsFalse(idle.used,"a Relink Spike is spent with nobody down");StringAssert.Contains("Nobody is down",idle.message);Assert.AreEqual(3,State.consumables["RelinkSpike"]);
            sela.Health.Receive(new DamagePacket{amount=1000000,type=DamageType.Pulse});yield return null;
            Assert.IsFalse(sela.Health.Alive,"fixture: Sela is down");
            var raised=brain.UseItem("RelinkSpike");Assert.IsTrue(raised.used,raised.message);StringAssert.Contains("raised Sela",raised.message);
            Assert.IsTrue(sela.Health.Alive,"a Relink Spike doesn't raise the partner");Assert.AreEqual(sela.Health.maximum*.4f,sela.Health.integrity,1);Assert.AreEqual(2,State.consumables["RelinkSpike"]);
        }
        [UnityTest] public IEnumerator ADraughtAndAMealLendBothHeroesTheirEffectAndTheHudCountsThemDown()
        {
            yield return Stage();var taren=Body("Taren");var sela=Body("Sela");var brain=taren.GetComponent<PlayerBrain>();
            var victim=Dummy(new Vector3(200,0,6));yield return Frames(2);
            Assert.AreEqual(100,Hit(taren,victim.Health),.01f,"fixture: a plain hit of 100");
            State.consumables["SurgeDraught"]=1;State.consumables["PlatingWax"]=1;State.consumables["PodStew"]=1;
            var used=brain.UseItem("SurgeDraught");Assert.IsTrue(used.used,used.message);
            Assert.AreEqual(115,Hit(taren,victim.Health),.01f,"a Surge Draught doesn't lift the hit that lands");Assert.AreEqual(115,Hit(sela,victim.Health),.01f,"the draught is the drinker's alone");
            Assert.AreEqual(State.draught,"SurgeDraught");Assert.Greater(State.draughtLeft,50);
            yield return Frames(3);var marks=Object.FindFirstObjectByType<BuffMarks>();Assert.IsNotNull(marks,"the HUD has nothing to show a buff with");
            Assert.AreEqual(1,marks.Shown.Count(),"a running draught isn't on the HUD");StringAssert.IsMatch(@"^SURGE \d+s$",marks.Shown.First());
            // A meal beside it, not in its place.
            Assert.IsTrue(brain.UseItem("PodStew").used);yield return Frames(3);
            Assert.AreEqual(2,marks.Shown.Count(),"a meal took the draught's place on the HUD");Assert.IsTrue(marks.Shown.Any(m=>m.StartsWith("STEW ")));
            Assert.AreEqual(115,Hit(taren,victim.Health),.01f,"a meal ended the draught");
            taren.Health.integrity=taren.Health.maximum*.5f;float was=taren.Health.integrity;yield return new WaitForSeconds(2);
            Assert.Greater(taren.Health.integrity,was+taren.Health.maximum*.0025f,"Pod Stew doesn't mend a hero over time");
            // A second draught takes the first one's place.
            Assert.IsTrue(brain.UseItem("PlatingWax").used);
            Assert.AreEqual(100,Hit(taren,victim.Health),.01f,"two draughts run at once");
            taren.Health.integrity=taren.Health.maximum;Assert.AreEqual(40,Take(taren,50),.5f,"Plating Wax doesn't take a fifth off a hit");
            yield return Frames(3);Assert.IsTrue(marks.Shown.Any(m=>m.StartsWith("WAX ")));Assert.IsFalse(marks.Shown.Any(m=>m.StartsWith("SURGE ")));
            // They run out, and the HUD and the fight both know.
            State.draughtLeft=.2f;State.mealLeft=.2f;yield return new WaitForSeconds(1.3f);
            Assert.IsNull(State.draught);Assert.IsNull(State.meal);Assert.IsFalse(taren.mods.Any,"a draught that ran out still counts in the fight");Assert.IsEmpty(marks.Shown.ToArray(),"a buff that ran out is still on the HUD");
            taren.Health.integrity=taren.Health.maximum;Assert.AreEqual(50,Take(taren,50),.5f);
        }

        // ------------------------------------------------------------------ items 2 and 5: what gear does in a fight
        [UnityTest] public IEnumerator ModulePassivesChangeTheFightTheWayTheirRowsSay()
        {
            yield return Stage();var taren=Body("Taren");var sela=Body("Sela");var victim=Dummy(new Vector3(200,0,6));yield return Frames(2);
            // Twin Thread: the partner's share of a hit.
            Assert.AreEqual(PartyController.PartnerDamage,sela.damageScale,.001f,"fixture: a partner deals 60%");
            var thread=Wear("Taren","ModulesTwinThread");
            Assert.AreEqual(PartyController.PartnerDamage+.25f,sela.damageScale,.001f,"a Twin Thread doesn't lift the partner's share");Assert.AreEqual(1,taren.damageScale,.001f,"the lead's own hits changed");
            RpgServices.Inventory.Unequip(thread);Party.RefreshStats();Assert.AreEqual(PartyController.PartnerDamage,sela.damageScale,.001f,"a Twin Thread taken off still counts");
            // Glass Lattice: more out, more in.
            var glass=Wear("Taren","ModulesGlassLattice");
            Assert.AreEqual(125,Hit(taren,victim.Health),.01f,"Glass Lattice: damage dealt +25%");Assert.AreEqual(50,Take(taren,40),.5f,"Glass Lattice: damage taken +25%");
            RpgServices.Inventory.Unequip(glass);Party.RefreshStats();taren.Health.integrity=taren.Health.maximum;
            // A prism: its own type and no other.
            var prism=Wear("Taren","ModulesPrismPlasma");
            Assert.AreEqual(115,Hit(taren,victim.Health,DamageType.Plasma),.01f,"a Plasma Prism doesn't lift a Plasma hit");Assert.AreEqual(100,Hit(taren,victim.Health,DamageType.Beam),.01f,"a Plasma Prism lifts a Beam hit");
            RpgServices.Inventory.Unequip(prism);Party.RefreshStats();
            // Breaker's Knuckle: more Break, and a longer one.
            victim.Health.breakThreshold=100;var knuckle=Wear("Taren","ModulesBreakersKnuckle");
            Hit(taren,victim.Health,DamageType.Beam,1,50);Assert.AreEqual(70,victim.Health.BreakMeter,.01f,"Breaker's Knuckle: Break power +40%");
            Hit(taren,victim.Health,DamageType.Beam,1,50);Assert.IsTrue(victim.Health.Broken,"fixture: the body breaks");
            Assert.AreEqual(4,victim.Health.BrokenUntil-Time.time,.05f,"Breaker's Knuckle: a Break lasts a second longer");
            RpgServices.Inventory.Unequip(knuckle);Party.RefreshStats();victim.Health.breakThreshold=1e9f;
            // A Broken body takes half again as much: the hits below are measured on a whole one.
            for(float end=Time.time+6;victim.Health.Broken&&Time.time<end;)yield return null;
            Assert.IsFalse(victim.Health.Broken,"fixture: the Break ended");
            // Guard Capacitor: a held guard costs less, and a Flash Guard pays more.
            float hit=taren.Health.maximum*.2f;
            float Guarded()
            {taren.Health.integrity=taren.Health.maximum;taren.thrust=100;Take(taren,hit);return 100-taren.thrust;}
            taren.Guard(true);yield return new WaitForSecondsRealtime(.4f);float dear=Guarded();taren.Guard(false);yield return new WaitForSecondsRealtime(.6f);
            Assert.Greater(dear,15,"fixture: a held guard costs Thrust");
            var capacitor=Wear("Taren","ModulesGuardCapacitor");
            taren.Guard(true);yield return new WaitForSecondsRealtime(.4f);float cheap=Guarded();taren.Guard(false);
            Assert.AreEqual(dear*.6f,cheap,dear*.06f,"Guard Capacitor: guarding costs 40% less Thrust");
            yield return new WaitForSecondsRealtime(.6f);
            int flashes=taren.FlashGuards;taren.charge=0;taren.Guard(true);Take(taren,hit);taren.Guard(false);
            Assert.AreEqual(flashes+1,taren.FlashGuards,"fixture: a guard on the beat");Assert.AreEqual(40,taren.charge,.5f,"Guard Capacitor: a Flash pays 15 more Charge");
            RpgServices.Inventory.Unequip(capacitor);Party.RefreshStats();
            // Flash Coil: the hits after a Flash.
            yield return new WaitForSecondsRealtime(.6f);
            var coil=Wear("Taren","ModulesFlashCoil");Assert.AreEqual(100,Hit(taren,victim.Health),.01f,"a Flash Coil lifts every hit");
            taren.Guard(true);Take(taren,hit);taren.Guard(false);
            Assert.AreEqual(130,Hit(taren,victim.Health),.01f,"Flash Coil: +30% damage after a Flash");
            RpgServices.Inventory.Unequip(coil);Party.RefreshStats();
            // Mastery Lens and Fortune Compass.
            var lens=Wear("Taren","ModulesMasteryLens");var skill=SkillBook.Equipped("Taren",0);
            Assert.AreEqual(SkillBook.Mastery("Taren",skill.id)+1,taren.MasteryOf(skill),"Mastery Lens: skills act one mastery level higher");
            RpgServices.Inventory.Unequip(lens);Party.RefreshStats();
            Wear("Taren","ModulesFortuneCompass");taren.fortune=100;Assert.AreEqual(.25f,LootDrop.Luck(taren),.001f,"Fortune Compass: drop luck can reach 25%");
        }
        [UnityTest] public IEnumerator AReservoirHoldsChargeAndRefillsItBetweenFights()
        {
            yield return Stage();var taren=Body("Taren");
            taren.charge=50;yield return new WaitForSeconds(2);Assert.Less(taren.charge,49.5f,"fixture: Charge fades");
            Wear("Taren","ModulesReservoir");
            taren.charge=50;yield return new WaitForSeconds(2);Assert.AreEqual(50,taren.charge,.01f,"Reservoir: Charge doesn't fade");
            taren.charge=0;yield return new WaitForSeconds(4);
            Assert.AreEqual(30,taren.charge,.5f,"Reservoir: out of a fight, Charge refills to 30");
            // In a fight it holds what it has and adds nothing.
            taren.Health.InvulnerableUntil=GameTime.Now+60;var hound=ActorFactory.Enemy(Def<EnemyDef>("Ridgehound"),taren.transform.position+Vector3.forward*8);yield return new WaitForSeconds(.8f);
            taren.charge=0;yield return new WaitForSeconds(2);
            Assert.Less(taren.charge,1,"a Reservoir refills Charge in the middle of a fight");
            Object.Destroy(hound.gameObject);
        }
        [UnityTest] public IEnumerator ASecondWindLeavesOneIntegrityOnceAFight()
        {
            yield return Stage();var taren=Body("Taren");Wear("Taren","ModulesSecondWind");
            float taken=Take(taren,1000000);
            Assert.IsTrue(taren.Health.Alive,"Second Wind: a killing blow killed");Assert.AreEqual(1,taren.Health.integrity,.01f);Assert.AreEqual(taren.Health.maximum-1,taken,.01f,"the damage shown isn't what was taken");
            // It comes back when nothing near is awake.
            yield return new WaitForSeconds(3.2f);taren.Health.integrity=taren.Health.maximum;
            Take(taren,1000000);Assert.IsTrue(taren.Health.Alive,"Second Wind doesn't come back between fights");
            // And not while a fight goes on: the second killing blow of one fight kills. (The hound is kept off him until then.)
            taren.Health.integrity=taren.Health.maximum;taren.Health.InvulnerableUntil=GameTime.Now+60;
            var hound=ActorFactory.Enemy(Def<EnemyDef>("Ridgehound"),taren.transform.position+Vector3.forward*8);yield return new WaitForSeconds(3.2f);
            taren.Health.integrity=taren.Health.maximum;taren.Health.InvulnerableUntil=0;Take(taren,1000000);
            Assert.IsFalse(taren.Health.Alive,"Second Wind saves a hero twice in one fight");
            Object.Destroy(hound.gameObject);
        }
        [UnityTest] public IEnumerator AMedicFilamentSharesAHealWithThePartner()
        {
            yield return Stage();var taren=Body("Taren");var sela=Body("Sela");var brain=taren.GetComponent<PlayerBrain>();State.consumables["RepairGel"]=3;
            taren.Health.integrity=sela.Health.integrity=10;Assert.IsTrue(brain.UseItem("RepairGel").used);
            Assert.AreEqual(10,sela.Health.integrity,.01f,"fixture: a gel is the drinker's alone");
            Wear("Taren","ModulesMedicFilament");float heal=ItemUse.Heal(Def<ConsumableDef>("RepairGel"),sela);
            taren.Health.integrity=10;Assert.IsTrue(brain.UseItem("RepairGel").used);
            Assert.AreEqual(10+heal*.5f,sela.Health.integrity,.5f,"Medic Filament: a healing item also gives the partner half as much");
            // Full himself, with her hurt: still worth a press.
            taren.Health.integrity=taren.Health.maximum;sela.Health.integrity=10;
            Assert.IsTrue(brain.UseItem("RepairGel").used,"with a Medic Filament a full hero can't use a gel for a hurt partner");Assert.Greater(sela.Health.integrity,10);
        }
        [UnityTest] public IEnumerator AffixesDoWhatTheirWordsSay()
        {
            yield return Stage();var taren=Body("Taren");
            taren.thrust=0;float from=Time.time;yield return new WaitForSeconds(1);float plain=taren.thrust/(Time.time-from);
            Assert.AreEqual(CombatActor.ThrustRest,plain,2,"fixture: Thrust returns at its own pace with no gear on");
            // Worn, an affix reaches the hero.
            Wear("Taren","EdgesT2","Keen","Siphon");Wear("Taren","DrivesT2","Fleet","Flashbound");Wear("Taren","ModulesT2","Overcharged","Quickcycle");Wear("Taren","FramesT2","Bulwark");
            Assert.AreEqual(.05f,taren.mods.critChance,.0001f);Assert.AreEqual(.02f,taren.mods.lifeOnKill,.0001f);Assert.AreEqual(-.5f,taren.mods.mashCost,.0001f);
            Assert.AreEqual(CombatActor.MashCost*.5f,taren.MashPrice,.01f,"Fleet: a rushed roll costs half the Thrust");
            // Overcharged and Quickcycle, through a real release.
            var skill=SkillBook.Equipped("Taren",0);taren.charge=100;float cost=SkillBook.Cost("Taren",skill);
            Assert.IsTrue(taren.Skill(0),"fixture: the skill goes");
            Assert.AreEqual(100-(cost-2),taren.charge,.5f,"Overcharged: skills cost 2 less Charge");Assert.AreEqual(skill.cooldown*.9f,taren.cooldowns[0],.05f,"Quickcycle: skill cooldowns 10% shorter");
            yield return new WaitForSecondsRealtime(1.2f);
            // Siphon: a kill gives Integrity back.
            taren.Health.integrity=50;taren.AwardKill(false);Assert.AreEqual(50+taren.Health.maximum*.02f,taren.Health.integrity,.05f,"Siphon: a kill restores 2% Integrity");
            // Keen and Vicious, by the packet a swing sends: with the chance at certainty every hit crits, and harder.
            taren.mods=new Mods{critChance=1};var sure=taren.Packet(10,DamageType.Beam,0);Assert.IsTrue(sure.isCrit,"crit chance from gear isn't added to the roll");
            taren.mods=new Mods{critChance=1,critDamage=.25f};var hard=taren.Packet(10,DamageType.Beam,0);
            Assert.AreEqual(sure.amount+10*.25f,hard.amount,.01f,"Vicious: crits hit 25% harder");
            // Thrust comes back faster with Fleet on the drive. Over a second, so the gear's own refresh (twice a second) has put the worn parts' effects back in place of the two above.
            yield return new WaitForSecondsRealtime(.7f);Assert.AreEqual(.2f,taren.mods.thrustRegen,.0001f,"fixture: the worn parts' effects are back");
            taren.thrust=0;from=Time.time;yield return new WaitForSeconds(1);float fleet=taren.thrust/(Time.time-from);
            Assert.AreEqual(plain*1.2f,fleet,plain*.07f,"Fleet: Thrust returns 20% faster");
        }

        // ------------------------------------------------------------------ item 4: what a kill pays
        [UnityTest] public IEnumerator AnEnemyPaysItsOwnScripAndScrapAndCanDropARolledPart()
        {
            var table=ScriptableObject.CreateInstance<LootTable>();
            table.entries=new[]{new LootEntry{partTier=3,partSlot=GearSlot.Drive,count=1,chance=1},new LootEntry{id="EdgesT3",count=1,chance=1},new LootEntry{id="Rootfibre",count=2,chance=1}};
            var def=Object.Instantiate(Def<EnemyDef>("Scrapmite"));def.id="Scrapmite";def.lootTable=table;def.scrip=40;def.scrap=5;
            Party.Active.Health.InvulnerableUntil=GameTime.Now+60;
            int purse=State.scrip,scrap=Held("ScrapAlloy"),parts=State.parts.Count;Loot.Seed(9);
            const int kills=10;
            for(int i=0;i<kills;i++)
            {
                var enemy=ActorFactory.Enemy(def,Party.Active.transform.position+new Vector3(12+i*2,0,12));enemy.Passive=true;yield return null;yield return null;
                enemy.Health.Receive(new DamagePacket{source=Party.Active.Health,amount=100000,type=DamageType.Pulse});yield return null;
            }
            Assert.AreEqual(purse+kills*40,State.scrip,"an enemy pays the flat 8 scrip every enemy used to, whatever its definition says");
            Assert.AreEqual(scrap+kills*5,Held("ScrapAlloy"),"an enemy pays the flat 2 Scrap Alloy");Assert.AreEqual(kills*2,Held("Rootfibre"));
            var dropped=State.parts.Skip(parts).ToArray();Assert.AreEqual(kills*2,dropped.Length,"a loot table's parts never drop: its entries only knew materials and consumables");
            var drives=dropped.Where(p=>Def<TechPartDef>(p.definitionId).slot==GearSlot.Drive).ToArray();
            Assert.AreEqual(kills,drives.Length,"'any Prime drive' gave another slot");Assert.IsTrue(drives.All(p=>{var d=Def<TechPartDef>(p.definitionId);return d.tier==3&&d.drops;}),"'any Prime drive' gave a part of another tier, or one that doesn't drop");
            Assert.AreEqual(kills,dropped.Count(p=>p.definitionId=="EdgesT3"),"a part named in a loot table doesn't drop");
            Assert.IsTrue(dropped.Any(p=>p.affixes.Count>0),"twenty dropped parts and not one rolled an affix");
            // The first two chapters' enemies pay what they always did.
            Assert.AreEqual(8,LootDrop.Scrip(Def<EnemyDef>("Scrapmite")));Assert.AreEqual(2,LootDrop.Scrap(Def<EnemyDef>("Scrapmite")));
            Assert.AreEqual(150,LootDrop.Scrip(Def<EnemyDef>("Cantor")));Assert.AreEqual(12,LootDrop.Scrap(Def<EnemyDef>("Cantor")));
            Object.Destroy(table);Object.Destroy(def);
        }

        // ------------------------------------------------------------------ which card goes first
        static readonly FieldInfo CardTimer=typeof(QuestToast).GetField("showing",BindingFlags.NonPublic|BindingFlags.Instance);
        /// <summary>What a kill or a quest paid is not what the player just did. A card for it (POUCH FULL for loot over a carry cap, NEW PART for a part out of a drop) used to be shown
        /// next, as the answer to a press is, so it went up ahead of the quest step the same kill finished: the scan finds that a moment after the loot is in the bag. The quest's card
        /// goes first, then the level that came with it, then what it paid. QaUiFlowTests.AQuestFinishedInTheFieldShowsItsCardBeforeItsLevelUp holds the same for a quest's own reward.</summary>
        [UnityTest] public IEnumerator AKillsQuestStepAndItsLevelUpComeUpAheadOfWhatItsLootPaid()
        {
            var toast=Object.FindFirstObjectByType<QuestToast>();Assert.IsNotNull(CardTimer,"fixture: the card's own timer has moved");
            // A quest waiting on this kill, its own first card come and gone.
            State.questSteps["Sorrel"]=2;
            float deadline=Time.unscaledTime+3;while(!toast.Showing&&Time.unscaledTime<deadline)yield return null;
            deadline=Time.unscaledTime+12;while(toast.Showing&&Time.unscaledTime<deadline){CardTimer.SetValue(toast,0f);yield return null;}
            Assert.IsFalse(toast.Showing,"fixture: the quest's first card has gone");
            // A level a kill away, a pouch with no room for the gels it drops, and a part in the drop as well.
            foreach(var member in State.party)member.xp=Levels.RequiredXp(member.level)-1;
            var gel=Def<ConsumableDef>("RepairGel");State.consumables["RepairGel"]=Inventory.Cap(gel);
            var table=ScriptableObject.CreateInstance<LootTable>();table.entries=new[]{new LootEntry{id="RepairGel",count=2,chance=1},new LootEntry{id="EdgesT3",count=1,chance=1}};
            var def=Object.Instantiate(Def<EnemyDef>("Scrapmite"));def.id="Burrower";def.boss=false;def.lootTable=table;
            // The drop alone, on a plain body: the Burrower's own would bring its whole fight with it.
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="QaProgressionKill";body.transform.position=Party.Active.transform.position+new Vector3(30,0,30);
            var health=body.AddComponent<Health>();health.id="Burrower";body.AddComponent<LootDrop>().definition=def;yield return null;yield return null;
            int purse=State.scrip,level=State.party[0].level;
            health.Receive(new DamagePacket{source=Party.Active.Health,amount=1000000,type=DamageType.Pulse});
            Assert.AreEqual(3,State.questSteps["Sorrel"],"fixture: the kill moved the quest on");Assert.AreEqual(level+1,State.party[0].level,"fixture: the kill raised a level");
            Assert.AreEqual(Inventory.Cap(gel),State.consumables["RepairGel"],"fixture: the gels had nowhere to go");
            Assert.AreEqual(purse+LootDrop.Scrip(def)+2*gel.value,State.scrip,"the gels that didn't fit were lost, not paid for");
            // Each card as it comes up, sent on after a frame.
            var order=new List<string>();deadline=Time.unscaledTime+12;
            while(order.Count<4&&Time.unscaledTime<deadline)
            {
                yield return null;
                if(toast.Showing&&(order.Count==0||order[order.Count-1]!=toast.Heading))order.Add(toast.Heading);
                CardTimer.SetValue(toast,0f);
            }
            string came=string.Join(", ",order);
            Assert.AreEqual(4,order.Count,"fixture: the quest step, the level, the full pouch and the new part each get a card. Came up: "+came);
            Assert.AreEqual("QUEST UPDATED",order[0],"what the kill paid goes up ahead of the quest step it finished. Came up: "+came);
            StringAssert.StartsWith("SYNC",order[1],"what the kill paid goes up ahead of the level it brought. Came up: "+came);
            Assert.AreEqual("POUCH FULL",order[2],"the gels that didn't fit get no card after the level's. Came up: "+came);Assert.AreEqual("NEW PART",order[3],"the dropped part gets no card. Came up: "+came);
            // The answer to a press still goes next, ahead of anything waiting. (Two frames first, for the last card above to leave the screen.)
            yield return null;yield return null;Assert.IsFalse(toast.Showing,"fixture: the cards above have all gone");
            RpgServices.Inventory.Grant("EdgesT3",2,false);Notices.Post("SAVED","Your progress is saved.");
            deadline=Time.unscaledTime+3;while(!(toast.Showing&&toast.Heading=="SAVED")&&Time.unscaledTime<deadline){if(toast.Showing&&toast.Heading!="SAVED")Assert.Fail("a card for a part went up ahead of the answer to a press: "+toast.Heading);yield return null;}
            Assert.AreEqual("SAVED",toast.Heading,"a press's answer waits behind what a drop paid");
            Object.Destroy(body);Object.Destroy(table);Object.Destroy(def);
        }

        // ------------------------------------------------------------------ item 10: a cache
        [UnityTest] public IEnumerator ACacheGivesWhatItHoldsOnceAndStaysOpen()
        {
            var hero=Party.Active;var toast=Object.FindFirstObjectByType<QuestToast>();
            var body=new GameObject("QaProgressionCache");body.transform.position=hero.transform.position+Vector3.right*2;
            var lid=new GameObject("Lid");lid.transform.SetParent(body.transform,false);
            var cache=LootCache.On(body,"qa.progression","Growers' cache",75,new LootEntry{id="EdgesT3",count=1},new LootEntry{partTier=3,anySlot=true,count=1},new LootEntry{id="Rootfibre",count=4},new LootEntry{id="RepairSalve",count=1});
            cache.visual=lid;yield return null;
            Assert.IsTrue(cache.Available,"a cache with nothing awake near it can't be opened");StringAssert.Contains("cache",cache.prompt.ToLowerInvariant(),"the prompt doesn't say what it opens");
            Assert.Contains(cache,InteractionPrompt.Active.ToList(),"a cache isn't something the interact button can find");
            int purse=State.scrip,parts=State.parts.Count;Loot.Seed(4);
            cache.Interact();
            Assert.AreEqual(purse+75,State.scrip,"the cache's scrip");Assert.AreEqual(parts+2,State.parts.Count,"the cache's two parts");Assert.AreEqual("EdgesT3",State.parts[parts].definitionId);
            Assert.AreEqual(3,Def<TechPartDef>(State.parts[parts+1].definitionId).tier,"'any Prime part' in a cache");
            Assert.AreEqual(4,Held("Rootfibre"));Assert.AreEqual(1,State.consumables["RepairSalve"]);
            Assert.IsTrue(GameServices.Current.Flags.GetBool("cache.qa.progression"),"an opened cache isn't in the save: it comes back on a reload");
            Assert.IsFalse(lid.activeSelf,"an opened cache looks shut");Assert.IsFalse(cache.Available,"an opened cache still has a prompt");
            float until=Time.unscaledTime+6;while(!(toast.Showing&&toast.Heading.Contains("CACHE"))&&Time.unscaledTime<until)yield return null;
            StringAssert.Contains("CACHE OPENED",toast.Heading,"no card says what the cache held");StringAssert.Contains("Growers' cache",toast.Body);StringAssert.Contains("Rootfibre ×4",toast.Body);StringAssert.Contains("75 scrip",toast.Body);
            cache.Interact();Assert.AreEqual(purse+75,State.scrip,"a cache pays twice");Assert.AreEqual(parts+2,State.parts.Count);
            // The same cache met again (a reload, a return to the zone) starts open.
            var again=new GameObject("QaProgressionCacheAgain");LootCache.On(again,"qa.progression","Growers' cache",75,new LootEntry{id="Rootfibre",count=4});yield return null;yield return null;
            Assert.IsFalse(again.activeSelf,"a cache opened on an earlier visit is shut again");
            // And it waits for a fight around it to end, as a bench and a crystal do.
            hero.Health.InvulnerableUntil=GameTime.Now+60;
            var fresh=new GameObject("QaProgressionCacheFresh");fresh.transform.position=hero.transform.position+Vector3.left*2;var waiting=LootCache.On(fresh,"qa.progression.second","Cache",0,new LootEntry{id="Rootfibre",count=1});
            var hound=ActorFactory.Enemy(Def<EnemyDef>("Ridgehound"),fresh.transform.position+Vector3.forward*6);yield return Frames(3);
            Assert.IsFalse(waiting.Available,"a cache opens with an enemy awake beside it");waiting.Interact();Assert.AreEqual(4,Held("Rootfibre"),"a cache was opened in the middle of a fight");
            Object.Destroy(hound.gameObject);Object.Destroy(body);Object.Destroy(again);Object.Destroy(fresh);
        }
    }
}
