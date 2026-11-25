using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The screens a player opens, measured as the November 20 capture saw them: Mira's stall read as a debug list (one centred string a row, the page toggle,
    /// the pager and Back among the goods, the title off over the list with an empty block above her), the quest log's longest step ran out of its row and its
    /// description onto the footer, the bench's costs read as fractions, Corra's note broke onto a second line, and the chart's question covered the map's names.
    /// Every string the definitions can put on these screens is put there and measured against the box it stands in.</summary>
    public sealed class MenuLayoutTests
    {
        static GameState State=>GameServices.Current.State;
        static PauseMenu Menu=>Object.FindFirstObjectByType<PauseMenu>();
        static ShopUi Shop=>Object.FindFirstObjectByType<ShopUi>();
        static MemberState Taren=>State.party.Find(m=>m.id=="Taren");
        static MemberState Sela=>State.party.Find(m=>m.id=="Sela");
        static TechPartDef Part(string id)=>GameCatalog.Find<TechPartDef>(id);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();PromptService.ForceDevice(null);SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Load("Arena_Ground");
        }
        UnityEngine.InputSystem.Gamepad pad;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);pad=null;
            var menu=Menu;if(menu!=null&&menu.IsOpen)menu.Close();var shop=Shop;if(shop!=null&&shop.IsOpen)shop.Close();
            PromptService.ForceDevice(PromptDevice.Keyboard);PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return null;yield return null;
        }

        // ------------------------------------------------------------------ what's on screen
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static TMP_Text Label(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name&&t.isActiveAndEnabled);
        static IEnumerable<TMP_Text> Texts(Component row)=>row.GetComponentsInChildren<TMP_Text>().Where(t=>t.isActiveAndEnabled&&!string.IsNullOrEmpty(t.text));
        static string Words(Component row)=>string.Join("  ·  ",Texts(row).Select(t=>t.GetParsedText()));
        static Button[] Numbered(string prefix,int from)=>Buttons().Where(b=>b.name.StartsWith(prefix)&&int.TryParse(b.name.Substring(prefix.Length),out int n)&&n>=from)
            .OrderBy(b=>int.Parse(b.name.Substring(prefix.Length))).ToArray();
        static Rect Box(Component c){var corners=new Vector3[4];((RectTransform)c.transform).GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);}
        static float Units(Component c)=>((RectTransform)c.transform).lossyScale.x;
        /// <summary>Where these words are drawn, on the screen, in whichever of the row's texts holds them: from the pen's place at the first letter to its place after the last,
        /// so two columns of names line up whatever their first letters' side bearings.</summary>
        static Rect? Ink(Component row,string part)
        {
            foreach(var text in Texts(row))
            {
                text.ForceMeshUpdate();var info=text.textInfo;string parsed=text.GetParsedText();int at=parsed.IndexOf(part,System.StringComparison.Ordinal);if(at<0)continue;
                float l=float.MaxValue,r=float.MinValue,b=float.MaxValue,t=float.MinValue;
                for(int i=at;i<at+part.Length&&i<info.characterCount;i++){var c=info.characterInfo[i];if(!c.isVisible)continue;l=Mathf.Min(l,c.origin);r=Mathf.Max(r,c.xAdvance);b=Mathf.Min(b,c.bottomLeft.y);t=Mathf.Max(t,c.topRight.y);}
                if(l>r)continue;
                Vector3 low=text.transform.TransformPoint(new Vector3(l,b)),high=text.transform.TransformPoint(new Vector3(r,t));return Rect.MinMaxRect(low.x,low.y,high.x,high.y);
            }
            return null;
        }
        /// <summary>A piece of type that leaves its box: on a line of its own it runs past the box's ends, or it wraps to more lines than the box is tall; or it's under the type floor.</summary>
        static string Spills(TMP_Text text)
        {
            if(text==null||!text.isActiveAndEnabled||string.IsNullOrEmpty(text.text))return null;
            var box=text.rectTransform.rect;float room=box.width-text.margin.x-text.margin.z;
            if(text.fontSize<24)return $"\"{text.GetParsedText()}\" is set at {text.fontSize:0}, under the type floor";
            if(text.overflowMode==TextOverflowModes.Ellipsis||text.overflowMode==TextOverflowModes.Truncate)
            {text.ForceMeshUpdate();if(text.isTextTruncated)return $"\"{text.text}\" is cut short in its {box.width:0} wide box";}
            float line=text.fontSize*1.3f;
            if(box.height<line*1.9f||text.textWrappingMode==TextWrappingModes.NoWrap)
            {float wide=text.GetPreferredValues(text.text).x;if(wide>room+1)return $"\"{text.GetParsedText()}\" needs {wide:0} on its line and its box has {room:0}";}
            float tall=text.GetPreferredValues(text.text,room,0).y;
            return tall>box.height+1?$"\"{text.GetParsedText()}\" needs {tall:0} of height and its box has {box.height:0}":null;
        }
        static void AllFit(string surface,IEnumerable<Component> rows,List<string> failures)
        {foreach(var row in rows)foreach(var text in row.GetComponentsInChildren<TMP_Text>()){string spill=Spills(text);if(spill!=null)failures.Add($"{surface}, {row.name}: {spill}");}}
        /// <summary>The same left edge on every row (a column of names), or the same right edge (a column of prices), to two units.</summary>
        static void Lined(string surface,string what,IList<(string row,Rect ink)> found,bool right,List<string> failures,float units)
        {
            if(found.Count<2){failures.Add($"{surface}: fixture: fewer than two rows say their {what}");return;}
            float first=right?found[0].ink.xMax:found[0].ink.xMin;
            foreach(var (row,ink) in found.Skip(1))
            {float edge=right?ink.xMax:ink.xMin;if(Mathf.Abs(edge-first)>2*units)failures.Add($"{surface}: {row}'s {what} {(right?"ends":"starts")} {(edge-first)/units:0} units from {found[0].row}'s: the rows aren't in columns");}
        }

        // ------------------------------------------------------------------ the stalls
        static readonly (string inventory,string seller)[] Stalls=
        {
            ("Hub_Decks","Mira"),("Lanternwick","Corra"),("Rootmarket_Binna","Binna"),("Rootmarket_Hadda","Hadda"),("Driftreed_Dov","Dov"),("Driftreed_Quill","Quill"),
            ("Harrowgate_Odrun","Odrun"),("ManyHands_Corra","Corra"),("ManyHands","Corra"),("ManyHands_Dov","Dov"),("MeridianCrawl","Corra"),("NeighbourhoodAfloat","Corra"),
            ("OrielSkyfields","Lume"),("ScoriaFlows_Brekk","Brekk"),("Lastlight","Hask"),("DecksLeaving_Mira","Mira"),
        };
        /// <summary>Every offer on its counter, whatever flag it waits on, and every note the stall can say.</summary>
        static void Everything(ShopInventory stall)
        {
            foreach(var offer in stall.offers??new ShopOffer[0])if(!string.IsNullOrEmpty(offer.requires))GameServices.Current.Flags.SetBool(offer.requires,true);
            if(!string.IsNullOrEmpty(stall.discountFlag))GameServices.Current.Flags.SetBool(stall.discountFlag,true);
            if(!string.IsNullOrEmpty(stall.noteFlag))GameServices.Current.Flags.SetBool(stall.noteFlag,true);
        }
        static (string name,int price) Offered(ShopInventory stall,ShopOffer offer)
        {
            var part=GameCatalog.Find<TechPartDef>(offer.id);var item=GameCatalog.Find<ConsumableDef>(offer.id);var material=GameCatalog.Find<MaterialDef>(offer.id);
            return (part!=null?part.displayName:item!=null?item.displayName:material!=null?material.displayName:offer.id,ShopUi.Price(stall,offer,State));
        }
        /// <summary>A stall's offers stand in columns: every name starts where the others do and every price ends where the others do. The page toggle names both of its
        /// pages, and the pager and Back are the panel's controls, not rows the size of an offer.</summary>
        [UnityTest] public IEnumerator EveryStallsOffersStandInColumnsWithItsControlsBesideThem()
        {
            State.scrip=99999;var failures=new List<string>();var shop=Shop;
            foreach(var (id,seller) in Stalls)
            {
                var stall=GameCatalog.Find<ShopInventory>(id);if(stall==null){failures.Add($"fixture: no stall {id}");continue;}
                Everything(stall);var offers=ShopUi.Stock(stall,State).Select(o=>Offered(stall,o)).ToArray();
                shop.Open(stall,seller);yield return null;
                for(int page=0;page<6;page++)
                {
                    var rows=Numbered("ShopRow",1);string surface=$"{id}, page {page+1}";
                    var names=new List<(string,Rect)>();var prices=new List<(string,Rect)>();
                    foreach(var row in rows)
                    {
                        var offer=offers.Where(o=>Words(row).Contains(o.name)).OrderByDescending(o=>o.name.Length).FirstOrDefault();if(offer.name==null)continue;
                        var name=Ink(row,offer.name);var price=Ink(row,offer.price+" scrip")??Ink(row,offer.price.ToString());
                        if(name!=null)names.Add((row.name,name.Value));if(price!=null)prices.Add((row.name,price.Value));else failures.Add($"{surface}: {row.name} doesn't say its price, {offer.price}");
                    }
                    // A last page of one offer has nothing to line up with.
                    if(rows.Length>1){Lined(surface,"name",names,false,failures,Units(rows[0]));Lined(surface,"price",prices,true,failures,Units(rows[0]));}
                    AllFit(surface,rows,failures);
                    float offerWidth=Box(rows[0]).width;
                    foreach(string control in new[]{"Pager","ShopBack"}){var c=Row(control);if(c!=null&&Box(c).width>offerWidth*.5f)failures.Add($"{surface}: {control} is a row as wide as an offer, among the goods");}
                    var pager=Row("Pager");if(pager==null)break;pager.onClick.Invoke();yield return null;
                    if(Words(Row("Pager")).Contains("(1/"))break;
                }
                var toggle=Row("ShopRow0");string says=Words(toggle).ToLowerInvariant();
                if(!says.Contains("buy")||!says.Contains("sell"))failures.Add($"{id}: the page toggle says \"{Words(toggle)}\", not both of its pages");
                shop.Close();yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        /// <summary>The stall's name heads the page from its left edge, over the seller, where it sat off over the list with an empty block above her; what the story has
        /// brought to the counter is one line under it, where Corra's broke and left "· CHOIRWROUGHT" alone.</summary>
        [UnityTest] public IEnumerator AStallsNameAndNoteStandOverTheWholePanelOnALineEach()
        {
            var failures=new List<string>();var shop=Shop;
            foreach(var (id,seller) in Stalls)
            {
                var stall=GameCatalog.Find<ShopInventory>(id);if(stall==null)continue;Everything(stall);
                shop.Open(stall,seller);yield return null;
                var frame=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).First(i=>i.name=="ShopPanel");var panel=Box(frame);float units=Units(frame);
                var heading=Label("Heading");var ink=Ink(heading,heading.GetParsedText().Substring(0,1));
                if(ink==null||ink.Value.xMin-panel.xMin>90*units)failures.Add($"{id}: the stall's name starts {(ink==null?-1:(ink.Value.xMin-panel.xMin)/units):0} units into the panel: it stands off over the list, not at the head of the page");
                string spill=Spills(heading);if(spill!=null)failures.Add($"{id}: {spill}");
                var note=Label("Discount");
                if(note!=null)
                {
                    spill=Spills(note);if(spill!=null)failures.Add($"{id}: {spill}");
                    if(note.GetPreferredValues(note.text,note.rectTransform.rect.width-note.margin.x-note.margin.z,0).y>note.fontSize*1.6f)failures.Add($"{id}: the note \"{note.text}\" takes more than one line");
                    var box=Box(note);if(box.xMin<panel.xMin||box.xMax>panel.xMax)failures.Add($"{id}: the note runs out of the panel");
                }
                shop.Close();yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>Every item a stall sells and every part or material the party can sell back, on its row, measured against the box it stands in.</summary>
        [UnityTest] public IEnumerator EveryOfferAndEverySaleFitsItsBox()
        {
            State.scrip=99999;var failures=new List<string>();var shop=Shop;var inventory=RpgServices.Inventory;
            foreach(var def in GameCatalog.All<TechPartDef>()){var item=inventory.GivePart(def);item.upgrade=5;foreach(var affix in (def.affixPool??new AffixDef[0]).Take(2))item.affixes.Add(affix.id);}
            foreach(var material in GameCatalog.All<MaterialDef>())State.materials[material.id]=99;
            foreach(var (id,seller) in Stalls)
            {
                var stall=GameCatalog.Find<ShopInventory>(id);if(stall==null)continue;Everything(stall);
                shop.Open(stall,seller);yield return null;
                for(int side=0;side<2;side++)
                {
                    for(int page=0;page<20;page++)
                    {
                        var rows=Numbered("ShopRow",1);AllFit($"{id} {(side==0?"buy":"sell")} page {page+1}",rows.Concat(new[]{Row("ShopRow0")}).Where(r=>r!=null),failures);
                        foreach(var row in rows){EventSystem.current.SetSelectedGameObject(row.gameObject);yield return null;foreach(string name in new[]{"Feedback","Detail","DetailName","DetailSays","DetailAgainst"}){string spill=Spills(Label(name));if(spill!=null)failures.Add($"{id}, {row.name} under the cursor: {spill}");}}
                        var pager=Row("Pager");if(pager==null)break;pager.onClick.Invoke();yield return null;if(Words(Row("Pager")).Contains("(1/"))break;
                    }
                    Row("ShopRow0").onClick.Invoke();yield return null;
                }
                shop.Close();yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }

        // ------------------------------------------------------------------ the pause menu's lists
        /// <summary>Gear's parts and the bench's recipes stand in columns: names start together and what each does starts together, whoever wears it; the filters and the pager
        /// are controls, not rows; and nothing on the bench reads as a fraction (a cost read "Scrap Alloy 45/6", the ladders "Edges 2 (0/30)").</summary>
        [UnityTest] public IEnumerator GearAndTheBenchStandInColumnsAndTheBenchSaysWhatItTakesInWords()
        {
            var inventory=RpgServices.Inventory;var failures=new List<string>();
            var emitter=Part("EmittersT2");inventory.Equip(Sela,inventory.GivePart(emitter),emitter);var edge=Part("EdgesT2");inventory.Equip(Taren,inventory.GivePart(edge),edge);
            foreach(string id in new[]{"EdgesT1","FramesT1","DrivesT1","ModulesT1","FramesT2"})inventory.GivePart(Part(id));
            State.materials["ScrapAlloy"]=45;State.materials["RidgeCrystal"]=2;
            var menu=Menu;menu.Open(PauseMenu.GearPage);yield return null;
            {
                var names=new List<(string,Rect)>();var does=new List<(string,Rect)>();var rows=Numbered("Row_",2);
                foreach(var row in rows)
                {
                    var part=State.parts.Select(p=>(p,d:GameCatalog.Find<TechPartDef>(p.definitionId))).Where(x=>x.d!=null&&Words(row).Contains(x.d.displayName)).OrderByDescending(x=>x.d.displayName.Length).FirstOrDefault();if(part.d==null)continue;
                    var name=Ink(row,part.d.displayName);var stat=Ink(row,PartText.Effect(part.d,part.p));if(name!=null)names.Add((row.name,name.Value));if(stat!=null)does.Add((row.name,stat.Value));
                }
                Lined("Gear","name",names,false,failures,Units(rows[0]));Lined("Gear","stats",does,false,failures,Units(rows[0]));AllFit("Gear",rows,failures);
                foreach(string control in new[]{"Pager","Filter"}){var c=Row(control);if(c!=null&&Box(c).width>Box(rows[0]).width*.5f)failures.Add($"Gear: {control} is a row as wide as a part");}
            }
            menu.Close();yield return null;
            menu.Open(PauseMenu.FabricatePage,true);yield return null;
            for(int page=0;page<6;page++)
            {
                var fabricator=new Fabrication(State);var rows=Numbered("Row_",1);string surface="the bench, page "+(page+1);
                var names=new List<(string,Rect)>();var does=new List<(string,Rect)>();
                foreach(var row in rows)
                {
                    var recipe=GameCatalog.All<RecipeDef>().Where(r=>Words(row).Contains(r.displayName)).OrderByDescending(r=>r.displayName.Length).FirstOrDefault();if(recipe==null)continue;
                    var name=Ink(row,recipe.displayName);if(name!=null)names.Add((row.name,name.Value));
                    if(!fabricator.Ranked(recipe))continue;
                    string makes=recipe.output!=null?PartText.Effect(recipe.output):PartText.Restores(GameCatalog.Find<ConsumableDef>(recipe.consumableOutput));
                    var stat=Ink(row,makes);if(stat!=null)does.Add((row.name,stat.Value));
                    EventSystem.current.SetSelectedGameObject(row.gameObject);yield return null;
                    foreach(var text in rows.SelectMany(r=>r.GetComponentsInChildren<TMP_Text>()).Concat(new[]{Label("Note")}).Where(t=>t!=null))
                        if(Regex.IsMatch(text.GetParsedText(),@"\d+/\d+"))failures.Add($"{surface}: \"{text.GetParsedText()}\" reads as a fraction");
                }
                Lined(surface,"name",names,false,failures,Units(rows[0]));if(does.Count>1)Lined(surface,"what it makes",does,false,failures,Units(rows[0]));AllFit(surface,rows,failures);
                foreach(string control in new[]{"Pager","Kind","Row_0"}){var c=Row(control);if(c!=null&&Box(c).width>Box(rows[0]).width*.5f)failures.Add($"{surface}: {control} is a row as wide as a recipe");}
                var pager=Row("Pager");if(pager==null)break;pager.onClick.Invoke();yield return null;if(Words(Row("Pager")).Contains("(1/"))break;
            }
            menu.Close();
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        /// <summary>Every part the game defines, at its last upgrade with two affixes, worn and spare, in every mode of the Gear page, and every recipe on the bench: each on its row,
        /// measured against its box, and the note under the list too.</summary>
        [UnityTest] public IEnumerator EveryPartAndEveryRecipeFitsItsBox()
        {
            var inventory=RpgServices.Inventory;var failures=new List<string>();
            foreach(var def in GameCatalog.All<TechPartDef>())
            {
                for(int copy=0;copy<2;copy++){var item=inventory.GivePart(def);item.upgrade=copy==0?5:0;foreach(var affix in (def.affixPool??new AffixDef[0]).Take(2))item.affixes.Add(affix.id);}
            }
            foreach(var slot in new[]{"ModulesT2","EmittersT2","EdgesT2","FramesT2","DrivesT2"}){var def=Part(slot);var worn=inventory.GivePart(def);worn.upgrade=5;foreach(var affix in (def.affixPool??new AffixDef[0]).Take(2))worn.affixes.Add(affix.id);inventory.Equip(Taren,worn,def);}
            foreach(var material in GameCatalog.All<MaterialDef>()){State.materials[material.id]=999;State.collectedMaterials[material.id]=999;}
            foreach(var discipline in System.Enum.GetValues(typeof(Discipline)).Cast<Discipline>())State.craftingXp[discipline.ToString()]=135;
            var menu=Menu;menu.Open(PauseMenu.GearPage,true);yield return null;
            for(int mode=0;mode<5;mode++)
            {
                for(int page=0;page<60;page++)
                {
                    var rows=Numbered("Row_",1);AllFit($"Gear mode {mode} page {page+1}",rows,failures);
                    foreach(var row in rows.Skip(1).Take(2)){EventSystem.current.SetSelectedGameObject(row.gameObject);yield return null;string spill=Spills(Label("Note"));if(spill!=null)failures.Add($"Gear mode {mode}, {row.name}: {spill}");}
                    var pager=Row("Pager");if(pager==null)break;pager.onClick.Invoke();yield return null;if(Words(Row("Pager")).Contains("(1/"))break;
                }
                Row("Row_1").onClick.Invoke();yield return null;
            }
            menu.Close();yield return null;
            menu.Open(PauseMenu.FabricatePage,true);yield return null;
            for(int page=0;page<40;page++)
            {
                var rows=Numbered("Row_",0);AllFit($"the bench, page {page+1}",rows,failures);
                foreach(var row in rows){EventSystem.current.SetSelectedGameObject(row.gameObject);yield return null;string spill=Spills(Label("Note"));if(spill!=null)failures.Add($"the bench, {row.name}: {spill}");}
                var pager=Row("Pager");if(pager==null)break;pager.onClick.Invoke();yield return null;if(Words(Row("Pager")).Contains("(1/"))break;
            }
            menu.Close();
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        /// <summary>The two heroes' rows on the Party page stand in columns under headings: each number starts where its heading does, on both rows, whatever the names' lengths.
        /// The people stand apart from what can be done: Swap is a control, not a row among the heroes, and the save slots stand under a heading of their own, below the heroes
        /// (nov22 s05 041: no headings, and "Swap active member" and "Save to slot 1" were rows in the heroes' list).</summary>
        [UnityTest] public IEnumerator ThePartysRowsStandInColumnsUnderHeadingsApartFromWhatCanBeDone()
        {
            Taren.level=12;Taren.xp=40;var failures=new List<string>();
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            var heroes=new[]{Row("Row_0"),Row("Row_1")};Assert.IsTrue(heroes.All(h=>h!=null),"fixture: two heroes' rows");float units=Units(heroes[0]);
            var heads=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.name=="ColumnHead"&&t.isActiveAndEnabled).ToList();
            foreach(var (column,heading) in new[]{("Sync","SYNC"),("XP","XP"),("Integrity","INTEGRITY"),("Output","OUTPUT"),("Plating","PLATING")})
            {
                var head=heads.FirstOrDefault(h=>h.text==heading);if(head==null){failures.Add($"the Party page has no {heading} heading over its numbers");continue;}
                var over=InkOf(head);
                foreach(var hero in heroes)
                {
                    var cell=hero.GetComponent<ListRow>()?.Cell(column);if(cell==null||cell.text==""){failures.Add($"{hero.name} says nothing under {heading}");continue;}
                    var ink=InkOf(cell);if(ink==null||over==null)continue;
                    if(Mathf.Abs(ink.Value.xMin-over.Value.xMin)>2*units)failures.Add($"{hero.name}'s {column} starts {(ink.Value.xMin-over.Value.xMin)/units:0} units from its heading");
                    if(System.Text.RegularExpressions.Regex.IsMatch(cell.GetParsedText(),"[A-Za-z]{3}"))failures.Add($"{hero.name}'s {column} says its own name under its heading: \"{cell.GetParsedText()}\"");
                }
            }
            var swap=Row("Row_2");var heroBox=Box(heroes[1]);
            if(swap==null||!Words(swap).Contains("Swap active member"))failures.Add("fixture: Swap is Row_2");
            else if(Box(swap).width>heroBox.width*.5f)failures.Add("Swap active member is a row as wide as a hero's, in the heroes' list");
            var saveHead=Label("SaveHead");if(saveHead==null)failures.Add("the save slots have no heading of their own");
            foreach(var slot in new[]{"Row_3","Row_4","Row_5"}.Select(Row))
            {
                if(slot==null){failures.Add("fixture: the save slots are Row_3 to Row_5");continue;}
                if(Box(slot).yMax>heroBox.yMin)failures.Add($"{Words(slot)} stands among the heroes");
                if(saveHead!=null&&Box(slot).yMax>Box(saveHead).yMin+1*units)failures.Add($"{Words(slot)} stands over its heading");
            }
            AllFit("Party",Numbered("Row_",0),failures);Clear("Party",failures);
            menu.Close();
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ nothing crosses a border, nothing overlaps (D357)
        /// <summary>Where a piece of type's letters are drawn on the screen: from the first letter's pen to the last's, top of the tallest to the foot of the lowest.</summary>
        static Rect? InkOf(TMP_Text text)
        {
            if(text==null||!text.isActiveAndEnabled||string.IsNullOrEmpty(text.text))return null;
            text.ForceMeshUpdate();var info=text.textInfo;float l=float.MaxValue,r=float.MinValue,b=float.MaxValue,t=float.MinValue;
            for(int i=0;i<info.characterCount;i++){var c=info.characterInfo[i];if(!c.isVisible)continue;l=Mathf.Min(l,c.origin);r=Mathf.Max(r,c.xAdvance);b=Mathf.Min(b,c.bottomLeft.y);t=Mathf.Max(t,c.topRight.y);}
            if(l>r)return null;
            Vector3 low=text.transform.TransformPoint(new Vector3(l,b)),high=text.transform.TransformPoint(new Vector3(r,t));
            return Rect.MinMaxRect(low.x,low.y,high.x,high.y);
        }
        /// <summary>Where every word on the open menu or stall is drawn.</summary>
        static List<(string name,Rect ink)> AllInk(Canvas canvas)
        {
            var found=new List<(string,Rect)>();
            foreach(var text in canvas.GetComponentsInChildren<TMP_Text>())
            {
                if(text.color.a<.05f)continue;var ink=InkOf(text);if(ink==null)continue;
                string owner=text.GetComponentInParent<Button>()!=null?text.GetComponentInParent<Button>().name+"/":"";string words=text.GetParsedText().Replace("\n"," ");
                found.Add((owner+text.name+" \""+words.Substring(0,Mathf.Min(30,words.Length))+"\"",ink.Value));
            }
            return found;
        }
        /// <summary>The open screen's words each stand inside the panel's opening, and no two of them are drawn over each other; no two of its rows or controls overlap either.</summary>
        static void Clear(string surface,List<string> failures)
        {
            var frame=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).FirstOrDefault(i=>(i.name=="MenuPanel"||i.name=="ShopPanel")&&i.isActiveAndEnabled);
            if(frame==null){failures.Add($"{surface}: fixture: no panel open");return;}
            float units=Units(frame);var panel=Box(frame);var opening=Rect.MinMaxRect(panel.xMin+40*units,panel.yMin+30*units,panel.xMax-40*units,panel.yMax-30*units);
            var inks=AllInk(frame.canvas);
            foreach(var (name,ink) in inks)if(ink.xMin<opening.xMin||ink.xMax>opening.xMax||ink.yMin<opening.yMin||ink.yMax>opening.yMax)failures.Add($"{surface}: {name} crosses the panel's border");
            float slack=2*units;
            for(int i=0;i<inks.Count;i++)for(int j=i+1;j<inks.Count;j++)
            {
                var a=inks[i].ink;var b=inks[j].ink;float across=Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin),up=Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin);
                if(across>slack&&up>slack)failures.Add($"{surface}: {inks[i].name} and {inks[j].name} are drawn over each other");
                // On one line, two pieces of type with no space between them read as one word: "COOLDOWNMASTERY".
                else if(up>Mathf.Min(a.height,b.height)*.5f&&across>-4*units)failures.Add($"{surface}: {inks[i].name} and {inks[j].name} run into each other");
            }
            var boxes=frame.canvas.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled).Select(b=>(b.name,box:Box(b))).ToList();
            for(int i=0;i<boxes.Count;i++)for(int j=i+1;j<boxes.Count;j++)
            {
                var a=boxes[i].box;var b=boxes[j].box;
                if(Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin)>slack&&Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin)>slack)failures.Add($"{surface}: {boxes[i].name} and {boxes[j].name} overlap");
            }
        }
        /// <summary>The instrument can fail: the page's note moved onto its first row is two pieces of type over each other.</summary>
        static IEnumerator ClearCanFail(string row)
        {
            var note=Label("Note")??Label("Feedback");var line=Row(row);Assert.IsNotNull(note,"fixture: the page has a note");Assert.IsNotNull(line,"fixture: "+row);
            if(string.IsNullOrEmpty(note.text))note.text="The instrument's own words";
            var was=note.transform.position;note.transform.position=line.GetComponentInChildren<TMP_Text>().transform.position;yield return null;
            var caught=new List<string>();Clear("fixture",caught);note.transform.position=was;yield return null;
            Assert.IsTrue(caught.Any(c=>c.Contains("drawn over each other")),"the overlap check passes a note moved onto a row: it cannot fail");
        }

        // ------------------------------------------------------------------ the pause pages that were one-string rows (D357)
        /// <summary>The Skills page stands in columns like Gear and the bench (nov22 s01 054, s05 039: one dot-separated string a row, the lower half empty): each slot's button,
        /// the skill's icon as the HUD draws it, its name, cost, cooldown and mastery, names and costs starting together; and under the list a card for the skill under the cursor,
        /// its icon, its name and what it does. The chooser the same. Items, the bindings and the bench the same, and nothing on any of them crosses a border or another.</summary>
        [UnityTest] public IEnumerator TheSkillsItemsBindingsAndBenchStandInColumnsWithTheirCardsClear()
        {
            Taren.level=12;var failures=new List<string>();var menu=Menu;
            menu.Open(PauseMenu.SkillsPage);yield return null;yield return ClearCanFail("Row_1");
            {
                var rows=Numbered("Row_",1);Assert.AreEqual(SkillBook.Slots,rows.Length,"fixture: a row a slot");float units=Units(rows[0]);
                var names=new List<(string,Rect)>();var costs=new List<(string,Rect)>();
                for(int i=0;i<rows.Length;i++)
                {
                    var skill=SkillBook.Equipped("Taren",i);var row=rows[i].GetComponent<ListRow>();
                    if(row==null){failures.Add($"slot {i+1} is one string: \"{Words(rows[i])}\"");continue;}
                    var icon=rows[i].transform.Find("Item")?.GetComponent<Image>();if(icon==null||icon.sprite!=UiSkin.Skill(skill))failures.Add($"slot {i+1} doesn't show {skill.displayName}'s icon as the HUD draws it");
                    if(row.Cell("Button")?.text!=GameHud.SkillBinding(i,false))failures.Add($"slot {i+1}'s button isn't in a column of its own");
                    var name=Ink(rows[i],skill.displayName);var cost=Ink(rows[i],$"{SkillBook.Cost("Taren",skill):0} Charge");if(name!=null)names.Add((rows[i].name,name.Value));if(cost!=null)costs.Add((rows[i].name,cost.Value));
                    EventSystem.current.SetSelectedGameObject(rows[i].gameObject);yield return null;
                    if(Label("SkillName")?.text!=skill.displayName)failures.Add($"with slot {i+1} under the cursor the card names \"{Label("SkillName")?.text}\"");
                    if(!(Label("Note")?.text??"").Contains(skill.description))failures.Add($"with slot {i+1} under the cursor the card doesn't say what {skill.displayName} does");
                    var card=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).First(m=>m.name=="SkillIcon");if(card.sprite!=UiSkin.Skill(skill))failures.Add($"the card's icon isn't {skill.displayName}'s");
                    if(Box(card).yMax>Box(rows[rows.Length-1]).yMin)failures.Add("the card stands among the slots, not under them");
                }
                Lined("Skills","name",names,false,failures,units);Lined("Skills","cost",costs,false,failures,units);
                AllFit("Skills",rows,failures);Clear("Skills",failures);
                rows[0].onClick.Invoke();yield return null;
                var chooser=Numbered("Row_",1);AllFit("the chooser",chooser,failures);Clear("the chooser",failures);
                foreach(var line in chooser)if(line.GetComponent<ListRow>()==null)failures.Add($"the chooser's {line.name} is one string: \"{Words(line)}\"");
                menu.Close();yield return null;
            }
            State.consumables["RepairGel"]=3;State.consumables["ChargeCell"]=2;State.materials["ScrapAlloy"]=12;State.materials["HuskCore"]=1;
            menu.Open(PauseMenu.ItemsPage);yield return null;
            {
                var rows=Numbered("Row_",1);
                foreach(var row in rows){if(row.GetComponent<ListRow>()==null)failures.Add($"Items' {row.name} is one string");var mark=row.transform.Find("Item")?.GetComponent<Image>();if(mark==null||mark.sprite==null)failures.Add($"Items' {Words(row)} has no mark of its kind");}
                AllFit("Items",rows,failures);Clear("Items",failures);
                menu.Close();yield return null;
            }
            menu.Open(PauseMenu.SettingsPage);yield return null;Buttons().First(b=>Words(b)=="Keyboard bindings").onClick.Invoke();yield return null;
            {
                var rows=Numbered("Row_",1);
                foreach(var row in rows){var list=row.GetComponent<ListRow>();if(list==null||string.IsNullOrEmpty(list.Cell("Key")?.text))failures.Add($"the bindings' {Words(row)} doesn't give its key a column");}
                AllFit("the bindings",rows,failures);Clear("the bindings",failures);
                menu.Close();yield return null;
            }
            // The bench: every recipe says what it takes against what's held on its own row, and every ladder's progress in the same words.
            State.materials["ScrapAlloy"]=45;State.materials["RidgeCrystal"]=2;State.craftingXp["Edges"]=50;
            menu.Open(PauseMenu.FabricatePage,true);yield return null;
            for(int page=0;page<8;page++)
            {
                var rows=Numbered("Row_",1);var inventory=RpgServices.Inventory;
                foreach(var row in rows)
                {
                    var recipe=GameCatalog.All<RecipeDef>().Where(r=>Words(row).Contains(r.displayName)).OrderByDescending(r=>r.displayName.Length).FirstOrDefault();if(recipe==null)continue;
                    string takes=ListRow.Cell(row,"Takes");
                    foreach(var input in recipe.inputs)
                    {
                        string held=$"{DisplayNames.Item(input.id)}"+(input.count>1?$" ×{input.count}":"");int have=inventory.Count(input.id);
                        if(!System.Text.RegularExpressions.Regex.Replace(takes,"<[^>]+>","").Contains($"(have {have})"))failures.Add($"the bench's {recipe.displayName} doesn't say how much {DisplayNames.Item(input.id)} is held: \"{takes}\"");
                        if(have<input.count&&!System.Text.RegularExpressions.Regex.IsMatch(takes,$"<color={MenuList.Worse}>[^<]*{System.Text.RegularExpressions.Regex.Escape(DisplayNames.Item(input.id))}"))failures.Add($"the bench's {recipe.displayName} doesn't mark the {DisplayNames.Item(input.id)} it lacks");
                    }
                }
                AllFit("the bench, page "+(page+1),rows,failures);Clear("the bench, page "+(page+1),failures);
                var pager=Row("Pager");if(pager==null)break;pager.onClick.Invoke();yield return null;if(Words(Row("Pager")).Contains("(1/"))break;
            }
            EventSystem.current.SetSelectedGameObject(Row("Row_0").gameObject);yield return null;
            foreach(var discipline in System.Enum.GetValues(typeof(Discipline)).Cast<Discipline>())
                if(!System.Text.RegularExpressions.Regex.IsMatch(Label("Note").GetParsedText(),$@"{discipline} \d+ \((\d+ of 30|top)\)"))failures.Add($"the bench's ladders don't give {discipline}'s progress as they give the others': \"{Label("Note").GetParsedText()}\"");
            // D389: a bracketed count stays on one line ("Frames 1 (20" / "of 30)" in s07 013).
            {
                var ladderNote=Label("Note");ladderNote.ForceMeshUpdate();var ladderInfo=ladderNote.textInfo;
                for(int l=0;l<ladderInfo.lineCount;l++)
                {
                    var row=ladderInfo.lineInfo[l];int open=0;
                    for(int c=row.firstCharacterIndex;c<=row.lastCharacterIndex&&c<ladderInfo.characterCount;c++){char ch=ladderInfo.characterInfo[c].character;if(ch=='(')open++;else if(ch==')')open--;}
                    if(open!=0)failures.Add($"the bench's ladder note breaks inside a bracket on line {l+1}: \"{ladderNote.GetParsedText()}\"");
                }
            }
            menu.Close();yield return null;
            // Gear's comparison says what wearing a part would change.
            var inventoryGear=RpgServices.Inventory;var scrap=Part("EdgesT1");if(!Taren.equipped.ContainsKey(GearSlot.Edge))inventoryGear.Equip(Taren,inventoryGear.GivePart(scrap),scrap);inventoryGear.GivePart(Part("EdgesT2"));
            menu.Open(PauseMenu.GearPage);yield return null;
            {
                var heads=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.name=="ColumnHead").Select(t=>t.text).ToArray();
                if(!heads.Contains("VS WORN"))failures.Add("Gear's comparison column isn't headed for what it compares: "+string.Join(", ",heads));
                foreach(var row in Numbered("Row_",2)){string now=ListRow.Cell(row,"Now");if(now.Contains("now +"))failures.Add($"Gear's {Words(row)} still says \"now +\"");}
                var spare=Numbered("Row_",2).FirstOrDefault(r=>ListRow.Key(r.gameObject)=="Refined Edge");
                if(spare==null)failures.Add("fixture: a spare Refined Edge");else if(!System.Text.RegularExpressions.Regex.IsMatch(System.Text.RegularExpressions.Regex.Replace(ListRow.Cell(spare,"Now"),"<[^>]+>",""),@"^\+\d+$"))failures.Add($"a better spare part doesn't say what it would add: \"{ListRow.Cell(spare,"Now")}\"");
                AllFit("Gear",Numbered("Row_",2),failures);Clear("Gear",failures);
                menu.Close();
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct().Take(60)));
        }

        // ------------------------------------------------------------------ the stall's strip (D357)
        /// <summary>The strip about what's under the cursor stands under the stall's list, in its column, and always says something: with the cursor on the page toggle it says
        /// what a press does there (nov22 s25 053: the cursor on the toggle, nothing said, the lower half of six rows empty). Over the seller the party, each hero as the thing
        /// under the cursor finds them. Nothing on the stall crosses its border or another.</summary>
        [UnityTest] public IEnumerator AStallsStripStandsUnderItsListAndSaysWhatTheCursorIsOn()
        {
            State.scrip=99999;var failures=new List<string>();var shop=Shop;
            foreach(var (id,seller) in Stalls)
            {
                var stall=GameCatalog.Find<ShopInventory>(id);if(stall==null)continue;Everything(stall);
                shop.Open(stall,seller);yield return null;
                var named=Label("DetailName");var said=Label("Feedback");
                if(named==null||said==null){failures.Add($"{id}: fixture: no strip");shop.Close();yield return null;continue;}
                // Opened from a talk the cursor is on the toggle, which spends nothing, and the strip says so.
                if(EventSystem.current.currentSelectedGameObject?.name!="ShopRow0")failures.Add($"{id}: a stall a talk opens doesn't land on its toggle");
                if(string.IsNullOrEmpty(said.text)||!said.text.Contains(PromptService.SubmitTag()))failures.Add($"{id}: with the cursor on the toggle the strip doesn't say what a press does there: \"{said.text}\"");
                var rows=Numbered("ShopRow",1);
                var lowest=rows.Length>0?rows.Min(r=>Box(r).yMin):Box(Row("ShopRow0")).yMin;var list=rows.Length>0?Box(rows[0]):Box(Row("ShopRow0"));
                foreach(var part in new Component[]{named,said})
                {
                    var box=Box(part);
                    if(box.yMax>lowest)failures.Add($"{id}: the strip's {part.name} stands beside the list or over it, not under it");
                    if(box.xMin<list.xMin-1||box.xMax>list.xMax+1)failures.Add($"{id}: the strip's {part.name} leaves the list's column");
                }
                foreach(var line in Enumerable.Range(0,3).Select(i=>Label("PartyState"+i)).Where(l=>l!=null))if(string.IsNullOrEmpty(line.text))failures.Add($"{id}: {line.name} over the seller says nothing");
                Clear(id+" on the toggle",failures);
                foreach(var row in rows)
                {
                    EventSystem.current.SetSelectedGameObject(row.gameObject);yield return null;
                    string key=ListRow.Key(row.gameObject);
                    if(string.IsNullOrEmpty(named.text)||!key.EndsWith(named.text)&&!key.StartsWith(named.text))failures.Add($"{id}: with {key} under the cursor the strip is headed \"{named.text}\"");
                    if(string.IsNullOrEmpty(said.text))failures.Add($"{id}: with {key} under the cursor the strip says nothing");
                    Clear($"{id}, {key} under the cursor",failures);
                }
                shop.Close();yield return null;
            }
            // The instrument can fail: the strip where it stood, over the seller, isn't under the list.
            {
                var stall=GameCatalog.Find<ShopInventory>("Driftreed_Dov");shop.Open(stall,"Dov");yield return null;
                var said=Label("Feedback");var rows=Numbered("ShopRow",1);said.rectTransform.anchoredPosition=new Vector2(-578,170);yield return null;
                Assert.Greater(Box(said).yMax,rows.Min(r=>Box(r).yMin),"fixture: the strip over the seller stands beside the list, and the check must see it");
                shop.Close();yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct().Take(60)));
        }

        // ------------------------------------------------------------------ the replay's pad on the lists (D357)
        IEnumerator Press(UnityEngine.InputSystem.LowLevel.GamepadButton button)
        {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(button));yield return null;yield return null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());yield return null;yield return null;yield return new WaitForSecondsRealtime(.1f);
        }
        static string OnNow{get{var at=EventSystem.current.currentSelectedGameObject;return at!=null?at.name+" \""+ListRow.Key(at)+"\"":"nothing";}}
        /// <summary>The replay driver's pickRow, by pad presses as it makes them (ContinuousReview): down until the row it names is under the cursor, by its object's name or the
        /// words it's found by (ListRow.Found), turning the page once on the pager.</summary>
        IEnumerator Pick(string want,List<string> failures,string where)
        {
            bool paged=false;
            for(int presses=0;presses<40;presses++)
            {
                var at=EventSystem.current.currentSelectedGameObject;
                if(ListRow.Found(at,want))yield break;
                bool pager=at!=null&&at.name=="Pager";
                if(pager&&!paged){paged=true;yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.South);}
                else{if(!pager)paged=false;yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.DpadDown);}
            }
            failures.Add($"{where}: forty presses down never reached \"{want}\"; the cursor ends on {OnNow}");
        }
        /// <summary>Every list a route picks a row from by name, driven by the pad as the replay drives it, and each press doing what it says: the title's save picker loads slot 1
        /// (route 2 stopped on the title on November 22: route 1's three presses down the Party page had saved to slot 2 once Swap stood on the header's line), the Party page
        /// saves to slot 1 by those three presses and by its row, a stall buys by its row, the bench makes by its row, and the quest log reaches a quest on its second page.</summary>
        [UnityTest] public IEnumerator TheReplaysPadReachesEveryRowItPicksByNameAndThePressDoesWhatItSays()
        {
            pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();pad.MakeCurrent();PromptService.ForceDevice(PromptDevice.Gamepad);
            yield return Load("Hub_Decks");var failures=new List<string>();var saves=GameServices.Current.Saves;var menu=Menu;
            string[] slots={"slot1","slot2","slot3"};
            void Clean(){if(Application.isBatchMode)foreach(string slot in slots)foreach(string suffix in new[]{"",".bak",".tmp"}){var path=saves.PathForSlot(slot)+suffix;if(System.IO.File.Exists(path))System.IO.File.Delete(path);}}
            Clean();
            try
            {
                // The Party page as route 1 reaches it at Hal's bench: three presses down from where it lands, then A.
                menu.Open(PauseMenu.PartyPage,true);yield return new WaitForSecondsRealtime(.5f);
                for(int i=0;i<3;i++)yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.DpadDown);
                if(EventSystem.current.currentSelectedGameObject?.name!="Row_3")failures.Add($"three presses down the Party page land on {OnNow}, not Save to slot 1");
                yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.South);
                if(saves.Load("slot1")==null)failures.Add("route 1's three presses and A on the Party page saved nothing to slot 1");
                if(saves.Load("slot2")!=null)failures.Add("route 1's three presses and A on the Party page saved to slot 2");
                menu.Close();yield return null;
                // And by its row's words, as a pickRow would.
                menu.Open(PauseMenu.PartyPage);yield return new WaitForSecondsRealtime(.4f);yield return Pick("Save to slot 3",failures,"the Party page");
                yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.South);if(saves.Load("slot3")==null)failures.Add("A on Save to slot 3 saved nothing");
                menu.Close();yield return null;
                // A stall's buy rows, from the toggle a talk lands on.
                State.scrip=500;int parts=State.parts.Count;var shop=Shop;shop.Open(GameCatalog.Find<ShopInventory>("Hub_Decks"),"Mira");yield return new WaitForSecondsRealtime(.5f);
                yield return Pick("Scrap Module",failures,"Mira's stall");yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.South);
                if(State.parts.Count!=parts+1||State.scrip>=500)failures.Add($"A on Scrap Module at Mira's bought nothing (scrip {State.scrip})");
                shop.Close();yield return null;
                // The bench's rows, past its filter.
                State.materials["ScrapAlloy"]=30;parts=State.parts.Count;menu.Open(PauseMenu.FabricatePage,true);yield return new WaitForSecondsRealtime(.5f);
                yield return Pick("Scrap Edge",failures,"the bench");yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.South);
                if(State.parts.Count!=parts+1)failures.Add("A on Scrap Edge at the bench made nothing");
                menu.Close();yield return null;
                // The quest log, to a quest on its second page.
                foreach(var quest in GameCatalog.All<QuestDef>())State.questSteps[quest.id]=0;
                menu.Open(PauseMenu.QuestsPage);yield return new WaitForSecondsRealtime(.4f);
                var later=GameCatalog.All<QuestDef>().Select(q=>q.title).Distinct().Skip(10).FirstOrDefault();
                if(later!=null)yield return Pick(later,failures,"the quest log");
                menu.Close();yield return null;
                // The title's save picker: Continue, the picker, slot 1 by its row, A, and the world.
                SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
                while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
                yield return new WaitForSecondsRealtime(.5f);
                if(EventSystem.current.currentSelectedGameObject?.name!="Continue")yield return Pick("Continue",failures,"the title");
                yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.South);yield return new WaitForSecondsRealtime(.3f);
                yield return Pick("slot1",failures,"the save picker");yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.South);
                bool In(string scene)=>SceneManager.GetSceneByName(scene).isLoaded;
                deadline=Time.unscaledTime+25;while(Time.unscaledTime<deadline&&(SceneFlow.Current.Loading||In("Title")||!In("Hub_Decks")))yield return null;
                if(In("Title")||!In("Hub_Decks"))failures.Add($"slot 1 picked and confirmed on the title doesn't bring up the Decks (title still up: {In("Title")}); the cursor was on {OnNow}");
            }
            finally{Clean();}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ a conversation's choices (D357)
        /// <summary>Whether any of a standing body is drawn under the choices.</summary>
        static bool UnderChoices(Vector3 feet,Rect choices)
        {
            var camera=Camera.main;float l=float.MaxValue,b=float.MaxValue,r=float.MinValue,t=float.MinValue;
            var box=new Bounds(feet+Vector3.up*.95f,new Vector3(1,1.9f,1));
            for(int corner=0;corner<8;corner++)
            {
                var p=camera.WorldToViewportPoint(box.center+Vector3.Scale(box.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                if(p.z<=0)continue;l=Mathf.Min(l,p.x);b=Mathf.Min(b,p.y);r=Mathf.Max(r,p.x);t=Mathf.Max(t,p.y);
            }
            return l<=r&&Rect.MinMaxRect(l,b,r,t).Overlaps(choices);
        }
        /// <summary>A conversation's choices stand on its box at the right end, the last just over the box's top edge (nov22 s01 025: Mira's two sat over Sela in the middle of the
        /// right side), and while they're up nobody in the talk stands under them: the partner put under them as they come up is clear of them once the view has moved. First
        /// the check sees her under them, as she was.</summary>
        [UnityTest] public IEnumerator AConversationsChoicesStandOnItsBoxClearOfThePeopleTalking()
        {
            yield return Load("Hub_Decks");
            var mira=Object.FindObjectsByType<Lattice.World.Npc>(FindObjectsSortMode.None).FirstOrDefault(n=>n.speaker=="Mira");Assert.IsNotNull(mira,"fixture: Mira on the Decks");
            var party=PartyController.Current;var hero=party.Active;var partner=party.members.First(m=>m!=hero);
            void Put(CombatActor who,Vector3 at){var body=who.GetComponent<CharacterController>();body.enabled=false;who.transform.position=at;body.enabled=true;Physics.SyncTransforms();}
            Put(hero,mira.transform.position+new Vector3(-.6f,0,-2.6f));Put(partner,hero.transform.position+new Vector3(1.6f,0,.2f));yield return new WaitForSecondsRealtime(.5f);
            var dialogue=Lattice.Dialogue.DialogueSystem.Current;dialogue.AutoAdvance=false;Assert.IsTrue(dialogue.StartNode("MiraFirst","Mira"),"fixture: Mira's first talk starts");
            var panel=Object.FindFirstObjectByType<DialoguePanel>();var click=panel.GetComponentsInChildren<Button>(true).First(b=>b.name=="DialoguePanel");
            for(float end=Time.unscaledTime+20;!panel.OptionsVisible&&Time.unscaledTime<end;){click.onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);}
            Assert.IsTrue(panel.OptionsVisible,"fixture: Mira's choices came up");
            var labels=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith("Option_")).OrderBy(b=>b.name).Select(b=>b.GetComponentInChildren<TMP_Text>().text).ToList();
            var frame=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).First(i=>i.name=="DialoguePanel");var box=Box(frame);float units=Units(frame);
            foreach(var option in Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith("Option_")))
            {
                var at=Box(option);
                Assert.LessOrEqual(at.xMax,box.xMax+1,$"\"{Words(option)}\" runs past the box's right end");Assert.Greater(at.center.x,box.center.x,$"\"{Words(option)}\" doesn't stand at the box's right end");
                Assert.GreaterOrEqual(at.yMin,box.yMax-1,$"\"{Words(option)}\" stands on the box's words");
            }
            float lowest=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith("Option_")).Min(b=>Box(b).yMin);
            Assert.LessOrEqual(lowest-box.yMax,30*units,"the choices stand off the box, over the people, not on it");
            // She stays where her walk ended (the stage's walk gives up after four seconds); then she's put under the choices as a new set comes up.
            yield return new WaitForSecondsRealtime(4.5f);
            var area=panel.ChoiceArea.Value;var camera=Camera.main;var ray=camera.ViewportPointToRay(new Vector3(area.center.x,area.yMin+.02f,0));
            var ground=new Plane(Vector3.up,new Vector3(0,hero.transform.position.y,0));Assert.IsTrue(ground.Raycast(ray,out float along),"fixture: the floor under the choices");
            Put(partner,ray.GetPoint(along));panel.Options(labels,_=>{});yield return null;
            Assert.IsTrue(UnderChoices(partner.transform.position,panel.ChoiceArea.Value),"fixture: the partner stands under the choices, and the check must see her there");
            yield return new WaitForSecondsRealtime(3f);
            var choices=panel.ChoiceArea.Value;var failures=new List<string>();
            TestContext.WriteLine($"CHOICES screen {Screen.width}x{Screen.height} aspect {camera.aspect:0.000} choices {choices} partner at {camera.WorldToViewportPoint(partner.transform.position)} hero at {camera.WorldToViewportPoint(hero.transform.position)}");
            foreach(var (who,feet) in new[]{("the partner",partner.transform.position),("the hero",hero.transform.position),("Mira",mira.transform.position)})if(UnderChoices(feet,choices))failures.Add($"{who} stands under the choices while they're up");
            // Rendered at the shape of the screen the view was framed for: at 16:9 the same camera shows a wider strip and the box's choices stand higher.
            Still("s01-025-choices",Mathf.RoundToInt(1080f*Screen.width/Mathf.Max(1,Screen.height)),1080);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the row under the cursor (D357)
        /// <summary>The mean light of a set of boxes on the screen, as the game's camera draws it with every canvas over it, inside each box by a share of its size (the bevel and the
        /// chevrons at its ends stay out). Read in one render, with the canvases laid out at 1920 by 1080; the render is kept for a person to look at.</summary>
        static float[] Light(string still,params RectTransform[] boxes)
        {
            const int W=1920,H=1080;var camera=Camera.main;Assert.IsNotNull(camera,"fixture: no game camera");
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var image=new Texture2D(W,H,TextureFormat.RGB24,false);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;
                foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.1f;}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                var light=new float[boxes.Length];var corners=new Vector3[4];
                for(int k=0;k<boxes.Length;k++)
                {
                    boxes[k].GetWorldCorners(corners);Vector2 low=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),high=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
                    float w=high.x-low.x,h=high.y-low.y;int x0=Mathf.Clamp(Mathf.RoundToInt(low.x+w*.06f),0,W-1),x1=Mathf.Clamp(Mathf.RoundToInt(high.x-w*.06f),0,W-1),y0=Mathf.Clamp(Mathf.RoundToInt(low.y+h*.28f),0,H-1),y1=Mathf.Clamp(Mathf.RoundToInt(high.y-h*.28f),0,H-1);
                    double sum=0;int n=0;
                    for(int y=y0;y<=y1;y+=2)for(int x=x0;x<=x1;x+=2){var p=image.GetPixel(x,y);sum+=.2126f*p.r+.7152f*p.g+.0722f*p.b;n++;}
                    light[k]=n>0?(float)(sum/n):0;
                }
                if(!string.IsNullOrEmpty(still))
                {
                    string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/menus"));System.IO.Directory.CreateDirectory(folder);
                    string file=System.IO.Path.Combine(folder,still+".png");System.IO.File.WriteAllBytes(file,image.EncodeToPNG());TestContext.WriteLine("MENU_STILL "+file);
                }
                return light;
            }
            finally
            {
                foreach(var c in canvases)c.renderMode=RenderMode.ScreenSpaceOverlay;
                camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);
            }
        }
        /// <summary>How much brighter a control is drawn with the cursor on it than with the cursor on another: what a player sees of the selection, read off the render.</summary>
        static IEnumerator Lift(string surface,Selectable on,Selectable other,System.Action<float> said)
        {
            EventSystem.current.SetSelectedGameObject(other.gameObject);yield return null;yield return null;float off=Light(null,(RectTransform)on.transform)[0];
            EventSystem.current.SetSelectedGameObject(on.gameObject);yield return null;yield return null;float lit=Light(surface,(RectTransform)on.transform)[0];
            TestContext.WriteLine($"SELECTION {surface}: {off:0.000} -> {lit:0.000}");said(lit-off);
        }
        /// <summary>Its face's light must rise by this much under the cursor: a twelfth of the way from black to white. The gold words and the six-unit bar raised a stall's row by
        /// about a fortieth (nov22 s25 053), which a reviewer couldn't see.</summary>
        const float SelectedLift=1/12f;
        /// <summary>The row under the cursor is unmistakable on every list in the game: the pause pages, a stall (its rows and its page toggle), the save picker and the title. Each is
        /// rendered with the cursor on it and on another, and its face must be drawn brighter by a measured amount. First the selection as it was drawn (gold words and a thin bar,
        /// no wash), which the measure must reject.</summary>
        [UnityTest] public IEnumerator TheRowUnderTheCursorIsUnmistakableOnEveryList()
        {
            var failures=new List<string>();var menu=Menu;var shop=Shop;
            void Check(string surface,float lift){if(lift<SelectedLift)failures.Add($"{surface}: the row under the cursor is drawn {lift:0.000} brighter than without it, under {SelectedLift:0.000}");}
            // As it was: the wash taken off, the bar six units wide at 16 in.
            shop.Open(GameCatalog.Find<ShopInventory>("Driftreed_Dov"),"Dov");yield return null;
            {
                var on=Row("ShopRow1");var other=Row("ShopRow2");float old=0;
                EventSystem.current.SetSelectedGameObject(on.gameObject);yield return null;
                var wash=on.transform.Find("SelectionWash");var bar=on.transform.Find("SelectionBar");Assert.IsNotNull(wash,"fixture: the selection draws its wash");Assert.IsNotNull(bar,"fixture: the selection draws its bar");
                EventSystem.current.SetSelectedGameObject(other.gameObject);yield return null;float off=Light(null,(RectTransform)on.transform)[0];
                EventSystem.current.SetSelectedGameObject(on.gameObject);yield return null;
                wash.GetComponent<Image>().enabled=false;var rect=(RectTransform)bar;rect.anchorMin=new(0,.22f);rect.anchorMax=new(0,.78f);rect.anchoredPosition=new(16,0);rect.sizeDelta=new(6,0);
                old=Light("selection-as-it-was",(RectTransform)on.transform)[0]-off;TestContext.WriteLine($"SELECTION as it was: {old:0.000}");
                Assert.Less(old,SelectedLift,"the measure passes the selection the capture couldn't see: it cannot fail");
                shop.Close();yield return null;
            }
            foreach(var (id,seller,name) in new[]{("Driftreed_Dov","Dov","shop-dov"),("Hub_Decks","Mira","shop-mira")})
            {
                shop.Open(GameCatalog.Find<ShopInventory>(id),seller);yield return null;
                yield return Lift(name+" row",Row("ShopRow1"),Row("ShopRow2"),l=>Check(name+" row",l));
                yield return Lift(name+" toggle",Row("ShopRow0"),Row("ShopRow1"),l=>Check(name+" toggle",l));
                shop.Close();yield return null;
            }
            State.consumables["RepairGel"]=3;State.consumables["ChargeCell"]=2;
            foreach(var quest in GameCatalog.All<QuestDef>().Take(3))State.questSteps[quest.id]=0;
            foreach(var (page,first,second,name) in new[]{(PauseMenu.PartyPage,"Row_0","Row_1","party"),(PauseMenu.SkillsPage,"Row_1","Row_2","skills"),(PauseMenu.FabricatePage,"Row_1","Row_2","bench"),
                (PauseMenu.ItemsPage,"Row_1","Row_2","items"),(PauseMenu.QuestsPage,"Row_0","Row_1","quests"),(PauseMenu.PartyPage,"Row_3","Row_4","save-slots"),(PauseMenu.PartyPage,"Tab_Gear","Tab_Items","tabs")})
            {
                menu.Open(page,page==PauseMenu.FabricatePage);yield return null;
                var on=Row(first);var other=Row(second);if(on==null||other==null){failures.Add($"{name}: fixture: {first} and {second}");menu.Close();yield return null;continue;}
                yield return Lift(name,on,other,l=>Check(name,l));menu.Close();yield return null;
            }
            // The save picker and the title.
            var saves=GameServices.Current.Saves;saves.Save("slot1",State);saves.Save("slot2",State);
            try
            {
                SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
                while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
                yield return null;yield return null;
                var titled=Buttons().Where(b=>b.isActiveAndEnabled&&b.interactable).OrderByDescending(b=>Box(b).yMax).Take(2).ToArray();
                if(titled.Length<2)failures.Add("the title: fixture: two of its buttons");else yield return Lift("title",titled[0],titled[1],l=>Check("title",l));
                Row("Continue").onClick.Invoke();yield return null;yield return null;
                var slot1=Row("slot1");var slot2=Row("slot2");
                if(slot1==null||slot2==null)failures.Add("the save picker: fixture: slot1 and slot2");else yield return Lift("save picker",slot1,slot2,l=>Check("save picker",l));
            }
            finally
            {
                if(Application.isBatchMode)foreach(string slot in new[]{"slot1","slot2"})foreach(string suffix in new[]{"",".bak",".tmp"}){var path=saves.PathForSlot(slot)+suffix;if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the quest log
        /// <summary>Every quest the game defines, at every step it has and done, in the log: no row's words leave it (the Three Baskets' step ran under its row's border,
        /// its count below the box), and the note under the list holds the quest's whole description and the next line clear of the footer (the last word landed on "LB / RB TABS").
        /// Titles start together on every row and steps start together, the main quest's mark beside them, not in front.</summary>
        [UnityTest] public IEnumerator EveryQuestsRowAndDescriptionStayInTheirBoxesAtEveryStep()
        {
            var quests=GameCatalog.All<QuestDef>().ToArray();var failures=new List<string>();int most=quests.Max(q=>q.steps.Length);
            var menu=Menu;
            for(int step=0;step<=most;step++)
            {
                State.questSteps.Clear();foreach(var q in quests)State.questSteps[q.id]=Mathf.Min(step,q.steps.Length);
                menu.Open(PauseMenu.QuestsPage);yield return null;
                for(int page=0;page<12;page++)
                {
                    var rows=Numbered("Row_",0);string surface=$"step {step}, page {page+1}";
                    AllFit("the quest log, "+surface,rows,failures);
                    var titles=new List<(string,Rect)>();
                    foreach(var row in rows)
                    {
                        var quest=quests.Where(q=>Words(row).Contains(q.title)).OrderByDescending(q=>q.title.Length).FirstOrDefault();if(quest==null){failures.Add($"{surface}: {row.name} names no quest: {Words(row)}");continue;}
                        var title=Ink(row,quest.title);if(title!=null)titles.Add((row.name,title.Value));
                        EventSystem.current.SetSelectedGameObject(row.gameObject);yield return null;
                        var note=Label("Note");string spill=Spills(note);if(spill!=null)failures.Add($"{quest.id} at {surface}: {spill}");
                        if(!note.GetParsedText().Replace("\n"," ").Contains((quest.description??"").Replace("\n"," ")))failures.Add($"{quest.id} at {surface}: the note doesn't hold the whole description: \"{note.GetParsedText()}\"");
                        var footer=Label("Footer");if(footer!=null&&note.textBounds.size.y>0)
                        {
                            note.ForceMeshUpdate();var ink=Ink(note,note.GetParsedText().Trim().Split('\n').Last().Trim());var under=Ink(footer,footer.GetParsedText().Trim());
                            if(ink!=null&&under!=null&&ink.Value.yMin<under.Value.yMax)failures.Add($"{quest.id} at {surface}: the note's last line comes down onto the footer");
                        }
                    }
                    if(titles.Count>1)Lined("the quest log, "+surface,"title",titles,false,failures,Units(rows[0]));
                    var pager=Row("Pager");if(pager!=null&&Box(pager).width>Box(rows[0]).width*.5f)failures.Add($"{surface}: the pager is a row as wide as a quest");
                    if(pager==null)break;pager.onClick.Invoke();yield return null;if(Words(Row("Pager")).Contains("(1/"))break;
                }
                menu.Close();yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct().Take(60)));
        }

        // ------------------------------------------------------------------ a two-line row's plate (D366)
        /// <summary>The row plate's cyan strokes stand 14 units in from the row's top and bottom (measured off nov22b-whole s05/028 at 1080 tall: strokes 24.5 units from the middle of a 74
        /// row, three thick). The earlier checks measured each text against its own box, never against the plate, so a recipe's two lines passed while the strokes cut the first.
        /// The ink of every letter of a two-line row (the bench's, the quest log's) must stand inside that. Canvas units, so the screen's size doesn't change it.</summary>
        static string PlateCuts(string surface,IEnumerable<Button> rows)
        {
            const float Stroke=14;var cuts=new List<string>();
            foreach(var row in rows)
            {
                var rect=(RectTransform)row.transform;float half=rect.rect.height*.5f;float scale=Units(row);var listed=row.GetComponent<ListRow>();if(listed==null)continue;
                foreach(var text in listed.cells)
                {
                    if(text==null||!text.gameObject.activeSelf||string.IsNullOrEmpty(text.text))continue;
                    text.ForceMeshUpdate();var info=text.textInfo;float low=float.MaxValue,high=float.MinValue;
                    for(int i=0;i<info.characterCount;i++){var c=info.characterInfo[i];if(!c.isVisible)continue;low=Mathf.Min(low,c.bottomLeft.y);high=Mathf.Max(high,c.topRight.y);}
                    if(low>high)continue;
                    float top=rect.InverseTransformPoint(text.transform.TransformPoint(new Vector3(0,high))).y,bottom=rect.InverseTransformPoint(text.transform.TransformPoint(new Vector3(0,low))).y;
                    if(top>half-Stroke+.01f||bottom<-half+Stroke-.01f)cuts.Add($"{surface}, {row.name}: \"{text.GetParsedText()}\" is inked from {bottom:0.0} to {top:0.0} in a row {half*2:0} tall whose strokes stand at +-{half-Stroke:0.0}");
                }
            }
            return cuts.Count==0?null:string.Join("\n",cuts);
        }
        [UnityTest] public IEnumerator ATwoLineRowsStrokesCutNoneOfItsLetters()
        {
            var failures=new List<string>();var menu=Menu;
            foreach(var material in GameCatalog.All<MaterialDef>()){State.materials[material.id]=2;State.collectedMaterials[material.id]=2;}
            menu.Open(PauseMenu.FabricatePage,true);yield return null;
            for(int page=0;page<8;page++)
            {
                var rows=Numbered("Row_",1);var cut=PlateCuts("the bench, page "+(page+1),rows);if(cut!=null)failures.Add(cut);
                var pager=Row("Pager");if(pager==null)break;pager.onClick.Invoke();yield return null;if(Words(Row("Pager")).Contains("(1/"))break;
            }
            menu.Close();yield return null;
            State.questSteps.Clear();foreach(var q in GameCatalog.All<QuestDef>())State.questSteps[q.id]=0;
            menu.Open(PauseMenu.QuestsPage);yield return null;
            for(int page=0;page<12;page++)
            {
                var rows=Numbered("Row_",0);var cut=PlateCuts("the quest log, page "+(page+1),rows);if(cut!=null)failures.Add(cut);
                var pager=Row("Pager");if(pager==null)break;pager.onClick.Invoke();yield return null;if(Words(Row("Pager")).Contains("(1/"))break;
            }
            menu.Close();
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct().Take(40)));
        }

        /// <summary>The Party page's Response, Resonance and Fortune stand in columns under headings (one dotted line a hero in the footer in nov22b s05 042): a column's cells start together,
        /// no cell holds more than its one number, and the card clears the save slots above and the note and footer below.</summary>
        [UnityTest] public IEnumerator ThePartysOtherThreeStatsStandInColumnsClearOfTheSlotsAndTheNote()
        {
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;var failures=new List<string>();
            var cells=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Totals_")).ToArray();
            if(cells.Length<4)failures.Add($"fixture: the card holds {cells.Length} cells");
            foreach(string stat in new[]{"Response","Resonance","Fortune"})
            {
                var column=cells.Where(t=>t.name.EndsWith("_"+stat)).ToArray();if(column.Length<1)failures.Add($"no {stat} column");
                foreach(var c in column){if(c.GetParsedText().Contains("·")||!float.TryParse(c.GetParsedText(),out _))failures.Add($"{c.name} says \"{c.GetParsedText()}\", not one number");}
                if(column.Select(c=>Mathf.Round(Box(c).xMin*10)).Distinct().Count()>1)failures.Add($"the {stat} cells don't start together");
            }
            if(!Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t=>t.name=="ColumnHead"&&t.text=="FORTUNE"))failures.Add("no FORTUNE heading");
            float top=cells.Length>0?cells.Max(c=>Box(c).yMax):0,bottom=cells.Length>0?cells.Min(c=>Box(c).yMin):0;
            foreach(var slot in Numbered("Row_",3))if(Box(slot).yMin<top&&Box(slot).yMax>bottom)failures.Add($"{slot.name} stands over the card");
            var note=Label("Note");var footer=Label("Footer");
            if(note!=null&&note.GetParsedText().Contains("Resonance"))failures.Add("the note still carries the stats");
            if(note!=null&&Box(note).yMax>bottom+.01f&&Box(note).yMin<top)failures.Add("the note's box overlaps the card");
            if(note!=null&&footer!=null){var n=Ink(note,note.GetParsedText().Trim());var f=Ink(footer,footer.GetParsedText().Trim());if(n!=null&&f!=null&&n.Value.yMin<f.Value.yMax)failures.Add("the note comes down onto the footer");}
            menu.Close();
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ stills
        /// <summary>What the game's camera draws with every canvas over it, as a 1920 by 1080 still in the mirror's Builds/quality/focused/menus (CreditsTests.Still's way).</summary>
        static string Still(string name)=>Still(name,1920,1080);
        /// <summary>A still of another shape: the shape of the screen a test ran on, where what it measured was framed for that screen (the mirror's is 4:3).</summary>
        static string Still(string name,int W,int H)
        {
            var camera=Camera.main;Assert.IsNotNull(camera,"fixture: no game camera");
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var image=new Texture2D(W,H,TextureFormat.RGB24,false);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;
                foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.1f;}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/menus"));System.IO.Directory.CreateDirectory(folder);
                string file=System.IO.Path.Combine(folder,name+".png");System.IO.File.WriteAllBytes(file,image.EncodeToPNG());TestContext.WriteLine("MENU_STILL "+file);return file;
            }
            finally
            {
                foreach(var c in canvases)c.renderMode=RenderMode.ScreenSpaceOverlay;
                camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);
            }
        }
        /// <summary>The screens the November 20 capture saw, in states like the ones it caught them in, for a person to look at beside its stills.</summary>
        [UnityTest] public IEnumerator StillsOfTheScreensTheCaptureSaw()
        {
            PromptService.ForceDevice(PromptDevice.Gamepad);yield return null;
            var inventory=RpgServices.Inventory;var files=new List<string>();State.scrip=120;
            var edge=Part("EdgesT1");if(!Taren.equipped.ContainsKey(GearSlot.Edge))inventory.Equip(Taren,inventory.GivePart(edge),edge);
            var shop=Shop;var menu=Menu;
            IEnumerator Stall(string id,string seller,string name,string cursor=null,bool toggle=false)
            {
                var stall=GameCatalog.Find<ShopInventory>(id);Everything(stall);shop.Open(stall,seller);yield return null;yield return new WaitForSecondsRealtime(.4f);
                if(toggle){}
                else if(cursor!=null){var row=Numbered("ShopRow",1).FirstOrDefault(r=>Words(r).Contains(cursor));if(row!=null)EventSystem.current.SetSelectedGameObject(row.gameObject);}
                else{var first=Row("ShopRow1");if(first!=null)EventSystem.current.SetSelectedGameObject(first.gameObject);}
                yield return null;yield return null;files.Add(Still(name));shop.Close();yield return null;
            }
            // As a talk opens a stall: the cursor on its page toggle (nov22 s25 053, s01 026).
            State.scrip=3609;yield return Stall("Driftreed_Dov","Dov","s25-053-smokehouse-toggle",null,true);yield return Stall("Driftreed_Dov","Dov","s25-053-smokehouse-row");
            State.scrip=120;yield return Stall("Hub_Decks","Mira","s01-026-mira-toggle",null,true);
            yield return Stall("Hub_Decks","Mira","s01-026-mira");
            State.scrip=1200;yield return Stall("Hub_Decks","Mira","s01-026-mira-module","Scrap Module");
            State.scrip=11179;yield return Stall("Harrowgate_Odrun","Odrun","s51-230-back-table");
            State.scrip=16135;yield return Stall("ManyHands_Corra","Corra","s62-094-corra");
            State.scrip=3551;yield return Stall("Driftreed_Dov","Dov","s24-107-smokehouse");yield return Stall("Driftreed_Quill","Quill","s24-121-quill","Prime Module");
            shop.Open(GameCatalog.Find<ShopInventory>("Hub_Decks"),"Mira");yield return null;inventory.GivePart(Part("FramesT2"));Row("ShopRow0").onClick.Invoke();yield return null;
            var sale=Row("ShopRow1");if(sale!=null)EventSystem.current.SetSelectedGameObject(sale.gameObject);yield return null;files.Add(Still("shop-sell"));shop.Close();yield return null;
            shop.Open(GameCatalog.Find<ShopInventory>("Hub_Decks"));yield return null;yield return null;files.Add(Still("shop-no-seller"));shop.Close();yield return null;
            foreach(string id in new[]{"EdgesT1","EdgesT2"})inventory.GivePart(Part(id));var emitter=Part("EmittersT1");if(!Sela.equipped.ContainsKey(GearSlot.Emitter))inventory.Equip(Sela,inventory.GivePart(emitter),emitter);
            menu.Open(PauseMenu.GearPage);yield return null;var part=Row("Row_3");if(part!=null)EventSystem.current.SetSelectedGameObject(part.gameObject);yield return null;files.Add(Still("s01-049-gear"));
            Row("Row_1").onClick.Invoke();yield return null;Row("Row_1").onClick.Invoke();yield return null;files.Add(Still("gear-upgrade"));menu.Close();yield return null;
            menu.Open(PauseMenu.PartyPage);yield return null;files.Add(Still("s05-041-party"));menu.Close();yield return null;
            // The pages that were one-string rows (nov22 s01 054, s05 039), with a little mastery to show.
            for(int i=0;i<2;i++)SkillBook.RecordUse("Taren",SkillBook.Equipped("Taren",0));Taren.level=3;
            menu.Open(PauseMenu.SkillsPage);yield return null;files.Add(Still("s01-054-skills"));Row("Row_1").onClick.Invoke();yield return null;files.Add(Still("skills-chooser"));menu.Close();yield return null;
            State.consumables["RepairGel"]=8;State.consumables["ChargeCell"]=4;State.materials["ScrapAlloy"]=45;State.materials["RidgeCrystal"]=2;State.materials["HuskCore"]=1;
            menu.Open(PauseMenu.ItemsPage);yield return null;files.Add(Still("items"));menu.Close();yield return null;
            menu.Open(PauseMenu.SettingsPage);yield return null;Buttons().First(b=>Words(b)=="Keyboard bindings").onClick.Invoke();yield return null;files.Add(Still("bindings"));menu.Close();yield return null;
            menu.Open(PauseMenu.GearPage);yield return null;files.Add(Still("s05-034-gear"));menu.Close();yield return null;
            State.materials["ScrapAlloy"]=45;State.materials["RidgeCrystal"]=2;State.craftingXp["Edges"]=40;
            menu.Open(PauseMenu.FabricatePage,true);yield return null;var recipe=Row("Row_2");if(recipe!=null)EventSystem.current.SetSelectedGameObject(recipe.gameObject);yield return null;files.Add(Still("s05-026-fabricate"));menu.Close();yield return null;
            State.questSteps.Clear();foreach(var q in GameCatalog.All<QuestDef>().Where(q=>q.chapter==4||q.id=="ThreeBaskets"||q.id=="MooringPost"))State.questSteps[q.id]=0;
            menu.Open(PauseMenu.QuestsPage);yield return null;files.Add(Still("s27-007-quests"));
            var baskets=Numbered("Row_",0).FirstOrDefault(r=>Words(r).Contains("Three Baskets"));if(baskets!=null){EventSystem.current.SetSelectedGameObject(baskets.gameObject);yield return null;files.Add(Still("s27-099-quests-baskets"));}
            menu.Close();yield return null;
            DevLoadout.Apply("chapter3");GameServices.Current.Flags.SetBool(RouteChart.ChartedFlag,true);yield return Load("TallowDrift");
            menu=Menu;menu.Open(PauseMenu.ChartPage);yield return null;yield return null;var stop=Row(RouteChart.StopPrefix+"Lanternwick");if(stop!=null){stop.onClick.Invoke();yield return null;}
            files.Add(Still("s42-148-chart"));menu.Close();
            Assert.IsTrue(files.All(System.IO.File.Exists),"a still wasn't written");
        }

        // ------------------------------------------------------------------ the chart
        /// <summary>Asked whether to travel, the chart still shows its places: the question covered "Sorrel Out…" and "…ncharted" and the names under it.</summary>
        [UnityTest] public IEnumerator TheChartsQuestionCoversNoPlacesName()
        {
            DevLoadout.Apply("chapter3");GameServices.Current.Flags.SetBool(RouteChart.ChartedFlag,true);yield return Load("TallowDrift");
            var failures=new List<string>();
            foreach(var ask in new[]{"Lanternwick","Decks"})
            {
                var menu=Menu;menu.Open(PauseMenu.ChartPage);yield return null;yield return null;
                var stop=Row(RouteChart.StopPrefix+ask);Assert.IsNotNull(stop,"fixture: the chart has "+ask);stop.onClick.Invoke();yield return null;
                Assert.IsNotNull(Row("Travel"),"fixture: the chart asks about "+ask);
                var question=Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None).Where(g=>g.name=="ChartConfirm"||g.name=="Travel"||g.name=="Stay"||g.name=="ChartConfirmTitle"||g.name=="ChartConfirmLine").ToArray();
                foreach(var place in RouteChart.Stops)
                {
                    var holder=Row(RouteChart.StopPrefix+place.id);if(holder==null)continue;var name=holder.GetComponentInChildren<TMP_Text>();var ink=Ink(holder,name.GetParsedText());if(ink==null)continue;
                    foreach(var part in question)if(Box(part).Overlaps(ink.Value))failures.Add($"asking about {ask}: {part.name} covers \"{name.GetParsedText()}\"");
                }
                menu.Close();yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
    }
}
