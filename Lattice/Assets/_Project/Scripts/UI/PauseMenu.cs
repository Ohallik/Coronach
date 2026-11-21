using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Lattice.UI
{
    public sealed class PauseMenu:MonoBehaviour
    {
        public const int PartyPage=0,SkillsPage=1,GearPage=2,FabricatePage=3,ItemsPage=4,QuestsPage=5,SettingsPage=6;
        /// <summary>Sela's chart (RouteChart), once chapter 3 has drawn it. Its number comes after Settings', so every page before it keeps the number it always had; its tab stands after Quests.</summary>
        public const int ChartPage=7;
        readonly string[] tabs={"Party","Skills","Gear","Fabricate","Items","Quests","Settings","Chart"};
        static readonly int[] Plain={PartyPage,SkillsPage,GearPage,FabricatePage,ItemsPage,QuestsPage,SettingsPage},Charted={PartyPage,SkillsPage,GearPage,FabricatePage,ItemsPage,QuestsPage,ChartPage,SettingsPage};
        /// <summary>The pages in the order their tabs stand, which is the order LB and RB walk. Until the chart exists it's the seven there always were.</summary>
        static int[] Order=>RouteChart.Available?Charted:Plain;
        int Beside(int step){var order=Order;return order[(System.Array.IndexOf(order,page)+step+order.Length)%order.Length];}
        public int PageCount=>Order.Length;
        readonly RouteChart chart=new();
        int skillSlot=-1;
        /// <summary>Skill bindings name the active map's face actions (Fire/Roll/Lunge in flight).</summary>
        static bool InFlight=>ZoneController.Current!=null&&ZoneController.Current.Flight;
        /// <summary>The story's quests, in story order: the log leads with whichever is active.</summary>
        /// <summary>Read from the definitions (QuestDef.main; the first three whatever theirs say): a chapter adds its own by defining it.</summary>
        internal static string[] MainQuests=>StoryQuests.Ordered;
        /// <summary>A rebind moves a role, not an action: each form has its own map, and flight calls the same buttons by other names.</summary>
        static readonly (string ground,string flight,string label)[] Roles=
        {
            ("Attack","Fire","Attack / Fire"),("Dodge","Roll","Dodge / Roll"),("Guard","Lunge","Guard / Lunge"),("Sprint","Boost","Sprint / Boost"),("QuickItem","Brake","Quick item / Brake"),
            ("Swap","Swap","Swap"),("Interact","Interact","Interact"),("LockOn","LockOn","Lock on"),("Pause","Pause","Pause"),("Log","Log","Quest log"),
            ("Skill1","Skill1","Skill 1"),("Skill2","Skill2","Skill 2"),("Skill3","Skill3","Skill 3"),("Skill4","Skill4","Skill 4"),
            ("Extra","Extra","EXTRA move"),
        };
        Canvas canvas;
        GameObject panel;
        TMP_Text note;
        int page,offset,gearMode;
        /// <summary>Whose skills the Skills page shows. Swap flips it and the party stays as it is: seeing the partner's loadout used to mean really swapping, and with the partner down it couldn't be seen at all.
        /// With three in the party it is also whose parts the Gear page puts on (PartyPairUi.GearOf).</summary>
        int skillsOf;
        bool keyboardSettings,leaveAsked,closing;
        /// <summary>The Settings page's second sub-page: how a hit lands (ImpactSettingsUi).</summary>
        bool feelSettings;
        string feedback="",salvageAsked,listeningFor;
        PromptDevice feedbackDevice;
        FirstPress first;
        /// <summary>A part that has just changed hands: the Gear list moves it, and the cursor goes with it.</summary>
        string followPart;
        /// <summary>The Items page's rows by name, for the Swap button: what's under the cursor when it's pressed.</summary>
        readonly Dictionary<string,string> itemRows=new();
        /// <summary>What each save slot holds, in the Party page's words, read once a visit to the page. Every press redraws the page, and each redraw read all three
        /// slot files from the disk, so a manual save wrote one file and then read three on the frame of the press (QA-034). A save made here says what it wrote.</summary>
        readonly Dictionary<string,string> slotHolds=new();
        /// <summary>The Fabricate page shows only what can be made right now. Its row is the page's first and spends nothing, so a bench that opens at the end of
        /// Hal's talk lands there: a thumb still tapping through his lines used to make the first recipe and spend 2 Scrap Alloy (QA-036).</summary>
        bool onlyMakeable;
        /// <summary>The Gear page's slot filter and the Fabricate page's kind filter: -1 shows everything, then the slots and ladders in their own order (and, last, consumables).</summary>
        int gearSlot=-1,craftKind=-1;
        public bool IsOpen=>panel!=null;
        public bool AtBench;
        InputActionRebindingExtensions.RebindingOperation rebind;
        InputAction listening;
        /// <summary>A rebind is waiting for a key.</summary>
        public bool Listening=>rebind!=null;
        public void Open(int selected=0,bool bench=false)
        {
            page=selected;offset=gearMode=0;skillSlot=-1;keyboardSettings=feelSettings=leaveAsked=onlyMakeable=false;feedback="";salvageAsked=null;feedbackDevice=PromptService.Device;AtBench=bench;skillsOf=GameServices.Current.State.activeMember;slotHolds.Clear();
            gearSlot=craftKind=-1;
            GameServices.Current.Input.Blocked=true;GameTime.Paused=true;
            // The bench opens from a conversation on a row that spends materials, and the A that ended the talk is often pressed twice.
            if(bench)first.Begin();
            Rebuild(false);
        }
        public void Close()
        {
            // Disposing a listen raises neither of its callbacks: it has to be cancelled, or its action stays dead.
            closing=true;rebind?.Cancel();closing=false;
            first.End();Navigation(true);
            if(AudioMix.Current!=null)AudioMix.Current.Flush();if(panel!=null)Destroy(panel);panel=null;if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;GameTime.Paused=false;
        }
        /// <summary>Back one level: out of a listen, a sub-page or, at the top of a page, the menu.</summary>
        public void StepBack()
        {
            UiSounds.Cancel();
            if(rebind!=null){rebind.Cancel();return;}
            feedback="";
            if(skillSlot>=0){int slot=skillSlot;skillSlot=-1;offset=0;Rebuild(false,"Row_"+(slot+1));}
            else if(keyboardSettings){keyboardSettings=false;offset=0;Rebuild(false,"Row_5");}
            else if(feelSettings){feelSettings=false;offset=0;Rebuild(false,"Row_6");}
            else if(leaveAsked){leaveAsked=false;Rebuild(false,"Row_7");}
            else if(page==ChartPage&&chart.Back(out string stop))Rebuild(false,stop);
            else Close();
        }
        void Start(){canvas=UiKit.CreateCanvas("Menus",30,transform);PromptService.Changed+=Restate;}
        // A rebind listens for the very key that flips the device; never redraw under it.
        void Restate()
        {
            if(!IsOpen||rebind!=null)return;
            // A stored result can name the other device's buttons. A question goes with its words, or the next press answers one nobody can see.
            // A refusal just earned stays: the press that earned it is what flips the device, a poll later, and refusals don't name buttons.
            if(PromptService.Device!=feedbackDevice){feedbackDevice=PromptService.Device;if(Time.unscaledTime-refusedAt>.3f)feedback="";salvageAsked=null;}
            Rebuild();
        }
        void Update()
        {
            var input=GameServices.Current.Input;
            first.Step(rebind==null);
            if(rebind!=null){if(UiActions.Cancel.WasPressedThisFrame())StepBack();return;}
            bool pause=input.Find("Pause").WasPressedThisFrame();
            if(!IsOpen){if(input.Blocked)return;if(pause)Open();else if(input.Find("Log").WasPressedThisFrame())Open(QuestsPage);return;}
            if(UiActions.Cancel.WasPressedThisFrame()){StepBack();return;}
            if(pause){UiSounds.Cancel();Close();return;}
            // With three in the party the Gear page's Swap looks at the next hero, the one resting too, and swaps nobody (PartyPairUi.GearByLook). With two it really swaps.
            if(page==GearPage&&input.Find("Swap").WasPressedThisFrame()){if(PartyPairUi.GearByLook(GameServices.Current.State)){salvageAsked=null;ShowOtherHero();}else TrySwap();}
            if(page==SkillsPage&&input.Find("Swap").WasPressedThisFrame())ShowOtherHero();
            if(page==ItemsPage&&input.Find("Swap").WasPressedThisFrame())SetQuickItem();
            if(input.Find("MenuNext").WasPressedThisFrame())GoTo(Beside(1));
            if(input.Find("MenuPrev").WasPressedThisFrame())GoTo(Beside(-1));
        }
        void GoTo(int target){rebind?.Cancel();page=target;offset=0;skillSlot=-1;keyboardSettings=feelSettings=leaveAsked=false;feedback="";salvageAsked=null;skillsOf=GameServices.Current.State.activeMember;slotHolds.Clear();Rebuild(false);}
        static void Navigation(bool on){var system=EventSystem.current;if(system!=null)system.sendNavigationEvents=on;}
        void TrySwap()
        {
            var party=PartyController.Current;if(party.members.Length<2)return;
            salvageAsked=null;
            if(party.Swap())feedback="";
            else Refuse(DisplayNames.Speaker(party.members[(party.index+1)%party.members.Length].character)+" is down.");
            Rebuild();
        }
        /// <summary>The other hero's skills, in this page's place. Nobody swaps, so it asks nothing of the partner: one who's down can still be looked at, and given a loadout for when they're up.
        /// From inside the chooser it comes out on the same slot, as a swap did.</summary>
        void ShowOtherHero()
        {
            int count=GameServices.Current.State.party.Count;if(count<2)return;
            string want=skillSlot>=0?"Row_"+(skillSlot+1):null;
            skillsOf=(skillsOf+1)%count;skillSlot=-1;offset=0;feedback="";UiSounds.Move();
            Rebuild(true,want);
        }
        void Rebuild(bool rememberFocus=true,string want=null)
        {
            var selected=EventSystem.current.currentSelectedGameObject;
            string focus=want??(rememberFocus&&selected!=null?selected.name:null);
            // The tabs in the order they stand. A page that isn't among them (the chart, asked for before it exists) is the first page.
            var order=Order;if(System.Array.IndexOf(order,page)<0)page=PartyPage;
            if(panel!=null){panel.SetActive(false);Destroy(panel);}panel=UiKit.Dim(canvas.transform,UiKit.MenuDim).gameObject;
            UiKit.SolidFrame(panel.transform,"MenuPanel",new(.5f,.5f),new(-5,-13),new(1660,980));
            var title=UiKit.Heading(panel.transform,"Heading",tabs[page].ToUpperInvariant(),46,UiKit.TextColor,TextAlignmentOptions.Left);
            UiKit.Rect(title.gameObject,new(.5f,.5f),new(.5f,.5f),new(-150,400),new(1100,75));
            var list=new List<Selectable>();var tabButtons=new List<Selectable>();
            for(int i=0;i<order.Length;i++)
            {
                int n=order[i];var b=UiKit.Button(panel.transform,"Tab_"+tabs[n],tabs[n],()=>GoTo(n));UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(-645,290-i*86),new(290,62));tabButtons.Add(b);
                if(n==page)MarkOpenTab(b);
            }
            var state=GameServices.Current.State;int row=0,rowFont=26,shift=0;
            Button Add(string text,System.Action action=null,string icon=null,string name=null)
            {
                var b=UiKit.Row(panel.transform,name??"Row_"+row,text,action,rowFont);UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(170,290-(row+shift)*68),new(1220,56));list.Add(b);row++;
                // The icon stands clear of the selection bar, which is 16 to 22 in: on top of it, a selected blade and the bar merged into an L.
                if(icon!=null){var image=UiKit.Icon(b.transform,"Item",icon,Color.white);UiKit.Rect(image.gameObject,new(0,.5f),new(0,.5f),new(48,0),new(48,48));b.GetComponentInChildren<TMP_Text>().margin=new Vector4(78,0,20,0);}
                return b;
            }
            // A row of the page's own, a filter: it takes a place on the page without taking a number, so the rows under it keep the names they're focused and found by.
            Button Extra(string text,System.Action action,string name,string icon=null){var b=Add(text,action,icon,name);row--;shift++;return b;}
            // A page's title, in a row's place: it can't take focus, so no stray A lands on it.
            void Header(string text)
            {
                var t=UiKit.Text(panel.transform,"Header",text,26,UiKit.FrostColor,TextAlignmentOptions.Left);t.margin=new Vector4(42,0,42,0);
                UiKit.Rect(t.gameObject,new(.5f,.5f),new(.5f,.5f),new(170,290-(row+shift)*68),new(1220,56));row++;
            }
            void Note(string text){note=UiKit.Text(panel.transform,"Note",text,24,UiKit.DimTextColor,TextAlignmentOptions.TopLeft);UiKit.Rect(note.gameObject,new(.5f,.5f),new(.5f,.5f),new(170,-360),new(1220,90));}
            // A list can shrink under the page it was on: stay on its last page rather than show nothing.
            int Window(int count,int perPage){if(offset>=count)offset=count>0?(count-1)/perPage*perPage:0;return offset;}
            void Pager(int count,int perPage)
            {
                if(count<=perPage)return;
                // Focus goes by role here: an index would land the next A on whatever took it, a buy, an equip or a salvage.
                var pager=Add(UiKit.PagerLabel(offset,perPage,count),()=>{rebind?.Cancel();offset=offset+perPage<count?offset+perPage:0;salvageAsked=null;Rebuild(true,"Pager");},null,"Pager");
                // In line with the rows above: a page of icon rows starts its words further in, and this one sat hard against the row's left chevron.
                if(list.Count>1){var above=list[list.Count-2].GetComponentInChildren<TMP_Text>();if(above!=null)pager.GetComponentInChildren<TMP_Text>().margin=above.margin;}
            }
            if(page==PartyPage)
            {
                // With three in the party each hero's row chooses who is out (PartyPairUi, PartyField.Choose). It goes by the hero's name, and the third takes a place
                // without taking a number, so Swap is still Row_2 and the slots Row_3 to Row_5 for every route and check that finds them so. With two, the rows are as ever.
                bool choosing=Deployment.Choosing(state);
                foreach(var m in state.party)
                {
                    if(!choosing){Add(PartyLine(state,m));continue;}
                    var hero=m;string named=PartyPairUi.RowName(m.id),words=PartyPairUi.Line(state,m,PartyLine(state,m));
                    System.Action press=()=>{if(PartyPairUi.Press(hero,AtBench,out string said))feedback=said;else Refuse(said);Rebuild(true,named);};
                    var line=state.party.IndexOf(m)<Deployment.Size?Add(words,press,null,named):Extra(words,press,named);
                    PartText.Narrow(line,words,PartyPairUi.Short(words));
                    line.gameObject.AddComponent<SelectNote>().selected=()=>Tell(PartyTotals(state,hero),PartyPairUi.Says(state,hero,AtBench),$"{state.scrip} scrip");
                }
                if(state.party.Count>1)Add("Swap active member",()=>TrySwap());
                for(int i=1;i<=3;i++)
                {
                    int number=i;string slot="slot"+i;
                    if(!slotHolds.TryGetValue(slot,out string holds))slotHolds[slot]=holds=Holding(GameServices.Current.Saves.Load(slot));
                    Add($"Save to slot {number}"+holds,()=>{GameServices.Current.Saves.Save(slot,state);slotHolds[slot]=Holding(state);feedback=$"Saved to slot {number}.";Rebuild();});
                }
                // The note holds three lines: the two heroes out and the scrip. The one resting says theirs while the cursor is on their row.
                Note(string.Join("\n",Deployment.Of(state).Select(m=>PartyTotals(state,m)))+$"\n{state.scrip} scrip  ·  The game autosaves when you dock, warp, repair or finish a quest");
            }
            else if(page==SkillsPage)
            {
                var member=state.party[skillsOf];string who=DisplayNames.Speaker(member.id);var all=SkillBook.All(member.id);
                // Back and all eight skills fit above the note: no paging until a hero has more.
                int perPage=all.Length>8?7:8;
                string Binding(int at)=>GameHud.SkillBinding(at,InFlight);
                int SlotOf(SkillDef of){for(int i=0;i<SkillBook.Slots;i++)if(SkillBook.Equipped(member.id,i)==of)return i;return -1;}
                if(skillSlot<0)
                {
                    Header(who+"'s skills"+(state.party.Count>1?$"  ·  {PromptService.Tag("Swap")} shows {DisplayNames.Speaker(state.party[(skillsOf+1)%state.party.Count].id)}'s skills":""));
                    for(int i=0;i<SkillBook.Slots;i++)
                    {
                        int slot=i;var skill=SkillBook.Equipped(member.id,i);if(skill==null)continue;
                        Add($"{Binding(i)}  {skill.displayName}  ·  {SkillBook.Cost(member.id,skill):0} Charge  ·  {MasteryLine(member.id,skill)}  ·  {skill.cooldown:0} s",
                            ()=>{skillSlot=slot;int at=System.Array.IndexOf(all,skill);offset=at>=0?at/perPage*perPage:0;feedback="";Rebuild(false,"Row_"+(at-offset+1));});
                    }
                    // The hero's EXTRA move (S3), under the four slots once the story has unlocked the gauge. It's no slot: selecting it says what it does and changes nothing.
                    var definition=GameCatalog.Find<Lattice.Data.CharacterDef>(member.id);var extra=definition!=null&&ExtraGauge.Unlocked?definition.extra:null;
                    if(extra!=null)
                        Add($"EXTRA  ·  {extra.displayName}  ·  {ExtraGauge.Binding(InFlight)} with a full gauge  ·  gauge {ExtraGauge.Of(member.id):0} / 100",
                            ()=>{feedback=$"{extra.displayName}: {extra.description}";Rebuild(true,"ExtraRow");},null,"ExtraRow");
                    var next=all.Where(s=>!SkillBook.Knows(member,s)).OrderBy(s=>s.unlockLevel).FirstOrDefault();
                    Note($"{all.Count(s=>SkillBook.Knows(member,s))} of {all.Length} skills learned"+(next!=null?$"  ·  next: {next.displayName} at Sync {next.unlockLevel}":"")+
                        ".\nSelect a slot to change its skill. Each use raises a skill's mastery: more damage, less Charge.");
                }
                else
                {
                    var current=SkillBook.Equipped(member.id,skillSlot);
                    string choosing=$"Choosing for {Binding(skillSlot)} (now {current?.displayName}). A skill already in another slot trades places.";
                    // The rows hold what decides between skills, as the slot rows do. What a skill does follows the focus: with mastery on the row as well it ran past the row's 1136.
                    string Said(SkillDef of)=>$"{of.displayName}: {of.description}\n{choosing}";
                    Add("Back to "+who+"'s slots",StepBack).gameObject.AddComponent<SelectNote>().selected=()=>note.text=choosing;
                    foreach(var skill in all.Skip(Window(all.Length,perPage)).Take(perPage))
                    {
                        var s=skill;bool known=SkillBook.Knows(member,s);int worn=SlotOf(s);
                        string mark=s==current?"In use · ":worn>=0?$"On {Binding(worn)} · ":"";
                        var line=Add(known?$"{mark}{s.displayName}  ·  {SkillBook.Cost(member.id,s):0} Charge  ·  {MasteryLine(member.id,s)}  ·  {s.cooldown:0} s":$"Locked  ·  {s.displayName}  ·  learned at Sync {s.unlockLevel}",
                            known?()=>
                            {
                                int slot=skillSlot;var displaced=SkillBook.Equipped(member.id,slot);int from=SlotOf(s);SkillBook.Equip(member,slot,s);
                                feedback=$"{s.displayName} is on {Binding(slot)}."+(from>=0&&from!=slot&&displaced!=null?$" {displaced.displayName} moved to {Binding(from)}.":"");
                                skillSlot=-1;offset=0;Rebuild(false,"Row_"+(slot+1));
                            }:()=>{Refuse($"{s.displayName} is learned at Sync {s.unlockLevel}.");Rebuild();});
                        line.gameObject.AddComponent<SelectNote>().selected=()=>note.text=Said(s);
                    }
                    Pager(all.Length,perPage);
                    Note(current!=null?Said(current):choosing);
                }
            }
            else if(page==GearPage)
            {
                var member=state.party[PartyPairUi.GearOf(state,skillsOf)];var inventory=RpgServices.Inventory;
                Header("Equipping "+DisplayNames.Speaker(member.id)+PartyPairUi.GearClause(state,skillsOf,SwapClause(state)));
                // A re-tune is offered only where it can be done, at a bench: in the field the action still turns through the four it always had.
                string[] modes=AtBench?new[]{"Equip","Equip in module slot 2","Upgrade","Salvage","Re-tune"}:new[]{"Equip","Equip in module slot 2","Upgrade","Salvage"};
                const string benchWork="a Lattice Anvil or Hal's bench";
                string[] hints={"Edges drive melee and the lunge, Emitters drive beams and flight fire. Select a worn part to take it off.","Select a module for the second module slot.",
                    AtBench?"Each level costs Scrap Alloy and scrip; a Prime part or better takes its region's material as well.":"Upgrades are bench work: "+benchWork+".",
                    "Select a spare part twice to break it down. Parts in use can't be salvaged.",
                    AtBench?"Select an affix twice to roll it again. It becomes another one the part can carry.":"Re-tuning is bench work: "+benchWork+"."};
                gearMode=Mathf.Clamp(gearMode,0,modes.Length-1);
                string standing=PartText.Wearing(state,member)+"\n"+hints[gearMode];
                Add("Action: "+modes[gearMode]+" (select to change)",()=>{gearMode=(gearMode+1)%modes.Length;salvageAsked=null;feedback="";Rebuild();}).gameObject.AddComponent<SelectNote>().selected=()=>Tell(standing);
                // One list held every part the party owns, six to a page. The filter stands on the title's line, a press up from the action, so no row moves down to make room for it.
                string[] slots={"every slot","Emitters","Edges","Frames","Drives","Modules"};
                // It ends where the rows end and starts clear of the title's own box (which runs to 400): a select turns it, as it does the action.
                var filter=UiKit.Row(panel.transform,"Filter",$"Showing: {slots[gearSlot+1]}",()=>{gearSlot=gearSlot>=slots.Length-2?-1:gearSlot+1;offset=0;salvageAsked=null;feedback="";Rebuild(true,"Filter");},24);
                UiKit.Rect(filter.gameObject,new(.5f,.5f),new(.5f,.5f),new(595,400),new(370,56));list.Add(filter);filter.gameObject.AddComponent<SelectNote>().selected=()=>Tell(standing);
                // Now and next share a row: a worn two-stat module with an affix ran past it at 26, and at the type floor it stands at 1060 of 1122.
                if(gearMode==2||gearMode==4)rowFont=24;
                const int perPage=6;
                var parts=GearOrder(state,member).Where(p=>{var d=GameCatalog.Find<TechPartDef>(p.definitionId);return d!=null&&(gearSlot<0||(int)d.slot==gearSlot);}).ToList();
                // A re-tune is asked of one affix, so that mode lists each affix on its own row; every other mode lists each part once.
                var entries=gearMode==4?parts.SelectMany(p=>Enumerable.Range(0,Mathf.Min(2,p.affixes.Count)).Select(i=>(part:p,affix:i))).ToList():parts.Select(p=>(part:p,affix:-1)).ToList();
                // Putting a part on or taking it off moves it in this list: the cursor goes with it, page and all, or the next A lands on whatever slid under it.
                if(followPart!=null){int at=entries.FindIndex(e=>e.part.instanceId==followPart);if(at>=0)offset=at/perPage*perPage;}
                foreach(var entry in entries.Skip(Window(entries.Count,perPage)).Take(perPage))
                {
                    var item=entry.part;int affixAt=entry.affix;var def=GameCatalog.Find<TechPartDef>(item.definitionId);
                    var wearer=inventory.Wearer(item);
                    string head=$"{PartText.Mark(wearer,member)}{def.displayName}  T{def.tier} +{item.upgrade}",effects=PartText.Effects(def,item);
                    Button line;string under;
                    if(gearMode==4)
                    {
                        var affix=GameCatalog.Find<AffixDef>(item.affixes[affixAt]);var (materials,scrip)=Gear.RetuneCost(def);
                        line=Add($"{PartText.Mark(wearer,member)}{def.displayName}  T{def.tier}  ·  {(affix!=null?PartText.AffixSays(affix,def):item.affixes[affixAt])}",()=>Retune(item,def,affixAt),def.slot.ToString());
                        under=AtBench?$"A re-tune costs {PartText.Price(state,materials,scrip)}.":hints[4];
                    }
                    else
                    {
                        // What the part is against the slot decides an equip. What it becomes decides an upgrade: shown only as it stands, that was seen after paying.
                        string says=gearMode==2?PartText.NextUpgrade(def,item):PartText.Effect(def,item),against=gearMode==2||wearer==member?"":PartText.Against(state,member,def,item);
                        line=Add(head+PartText.Affixes(item)+"  ·  "+says+against,()=>UsePart(item,def,member),def.slot.ToString());
                        // Two affixes' names and a comparison don't all fit a row: the row gives way in that order, and the note under the list says the rest.
                        PartText.Narrow(line,head+PartText.Affixes(item)+"  ·  "+says+against,head+PartText.Affixes(item)+"  ·  "+says,head+"  ·  "+says);
                        under=gearMode==2?(AtBench?PartText.UpgradePrice(state,def,item):hints[2]):gearMode==3?$"Salvage gives {PartText.Yield(def,item)}.":PartText.Compare(state,member,def,item,gearMode==1&&def.slot==GearSlot.Module);
                    }
                    // What a row has no room for: a module's passive, each affix's effect, the whole comparison, the price.
                    line.gameObject.AddComponent<SelectNote>().selected=()=>Tell(effects,under);
                    if(item.instanceId==followPart)focus=line.name;
                }
                followPart=null;
                Pager(entries.Count,perPage);
                Note(standing);
            }
            else if(page==FabricatePage)
            {
                var fabricator=new Fabrication(state);var inventory=RpgServices.Inventory;
                // What the party has never held a later region's material for stays off the list (Fabrication.Known): a bench on Sorrel isn't ten pages of things made from plants nobody has seen.
                bool hostile=Hostile();
                bool OfKind(RecipeDef r)=>craftKind<0||(craftKind==5?r.output==null:r.output!=null&&(int)r.discipline==craftKind);
                var recipes=GameCatalog.All<RecipeDef>().Where(fabricator.Known).OrderBy(r=>r.discipline).ThenBy(r=>r.tier).ToArray();
                const int perPage=6;
                // One gate, the bench's own: this page kept a copy of it that stopped at tier 3.
                bool Ranked(RecipeDef r)=>fabricator.Ranked(r);
                bool Makeable(RecipeDef r)=>fabricator.Blocked(r,AtBench,hostile)==Fabrication.Block.None;
                // First, a row that spends nothing, with the first recipe one press down, as a shop a talk opens lands on its page toggle (D222). It's the bench's
                // own: with every recipe listed, the ones that can be made are spread over pages among the grey ones.
                Add(onlyMakeable?"Showing what can be made now  ·  select to show every recipe":"Showing every recipe  ·  select to show only what can be made now",
                    ()=>{onlyMakeable=!onlyMakeable;offset=0;feedback="";Rebuild(false,"Row_0");},"Anvil");
                string Trained(Discipline d){int level=fabricator.Level(d);return level>=10?$"{d} {level}":$"{d} {level} ({fabricator.Xp(d)%30}/30)";}
                string ladders=string.Join("  ·  ",System.Enum.GetValues(typeof(Discipline)).Cast<Discipline>().Select(d=>Trained(d)));
                string where=AtBench?"Parts and consumables can both be made here.":hostile?"Parts need a Lattice Anvil or Hal's bench. Consumables wait until the fight is over.":"Parts need a Lattice Anvil or Hal's bench. Consumables can be made anywhere.";
                list[list.Count-1].gameObject.AddComponent<SelectNote>().selected=()=>Tell(ladders,where);
                // By ladder, or the consumables alone: on the title's line, a press up from the row the bench opens on, so the first recipe stays one press down from it (QA-036).
                string[] kinds={"every kind","Emitters","Edges","Frames","Drives","Modules","Consumables"};
                var kind=UiKit.Row(panel.transform,"Kind",$"Showing: {kinds[craftKind+1]}",()=>{craftKind=craftKind>=kinds.Length-2?-1:craftKind+1;offset=0;feedback="";Rebuild(true,"Kind");},24);
                UiKit.Rect(kind.gameObject,new(.5f,.5f),new(.5f,.5f),new(595,400),new(370,56));list.Add(kind);kind.gameObject.AddComponent<SelectNote>().selected=()=>Tell(ladders,where);
                var shown=recipes.Where(OfKind).Where(r=>!onlyMakeable||Makeable(r)).ToArray();
                foreach(var recipe in shown.Skip(Window(shown.Length,perPage)).Take(perPage))
                {
                    var r=recipe;var made=r.output==null?GameCatalog.Find<ConsumableDef>(r.consumableOutput):null;var block=fabricator.Blocked(r,AtBench,hostile);
                    // A rank it hasn't reached takes the place of what it would make, which won't fit beside it on a row. Grey with every material met looked like a fault: the reason was only in the footer.
                    // So does a full pouch, and a fight.
                    string does=!Ranked(r)?$"<color=#FFB08A>Needs {r.discipline} {Mathf.Max(r.unlockLevel,r.tier)}</color>":block==Fabrication.Block.Hostile?"<color=#FFB08A>Not in a fight</color>":
                        block==Fabrication.Block.Full?$"<color=#FFB08A>Carrying {PartText.Carried(state,made)}</color>":r.output!=null?PartText.Effect(r.output):PartText.Restores(made);
                    string needs=string.Join(", ",r.inputs.Select(x=>{string held=$"{DisplayNames.Item(x.id)} {inventory.Count(x.id)}/{x.count}";return inventory.Count(x.id)<x.count?$"<color=#FFB08A>{held}</color>":held;}));
                    var line=Add($"{r.displayName}  T{r.tier}  ·  {does}  ·  {needs}",()=>Fabricate(r,fabricator),r.output!=null?r.output.slot.ToString():r.consumableOutput);
                    // Three materials and a stat line don't share a row: what it makes gives way to what it takes, and the note under the list says what it makes.
                    PartText.Narrow(line,$"{r.displayName}  T{r.tier}  ·  {does}  ·  {needs}",$"{r.displayName}  T{r.tier}  ·  {needs}");
                    if(block!=Fabrication.Block.None)Dim(line);
                    string makes=r.output!=null?$"{r.output.displayName}: {string.Join("  ·  ",new[]{PartText.Effect(r.output),PartText.Passive(r.output)}.Where(s=>s!=""))}":made!=null?$"{made.displayName} {PartText.Restores(made)}. You carry {PartText.Carried(state,made)}.":"";
                    line.gameObject.AddComponent<SelectNote>().selected=()=>Tell(makes,ladders,where);
                }
                Pager(shown.Length,perPage);
                Note((onlyMakeable&&shown.Length==0?"Nothing can be made with what you hold right now.\n":"")+ladders+"\n"+where);
            }
            else if(page==ItemsPage)
            {
                var hero=PartyController.Current.Active;string who=DisplayNames.Speaker(hero.character);var recipes=GameCatalog.All<RecipeDef>();
                var entries=state.consumables.Where(p=>p.Value>0).Select(p=>p.Key).Concat(state.materials.Where(p=>p.Value>0).Select(p=>p.Key)).Distinct().ToArray();
                const int perPage=8;itemRows.Clear();
                string vitals=$"{who}  ·  Integrity {hero.Health.integrity:0}/{hero.Health.maximum:0}  ·  Charge {hero.charge:0}/100",how=$"Select a consumable to use it on {who}. Press {PromptService.Tag("Swap")} on one to make it the quick item.";
                // One item, then a wait before the next (prog159). The wait runs on the fight's clock, which this menu stops: it says so, or the page looks stuck.
                string last=ItemUse.Wait(hero)>0?$"The next item is {ItemUse.Wait(hero):0.0} s of the fight away: the wait doesn't run in this menu.":$"Quick item: {DisplayNames.Item(state.quickItem)}. In a fight, {DeviceHints.CycleItem()} changes it.";
                foreach(var id in entries.Skip(Window(entries.Length,perPage)).Take(perPage))
                {
                    bool usable=state.consumables.TryGetValue(id,out int count)&&count>0;if(!usable)count=state.materials[id];
                    // A consumable's count stands against the most the party carries: "×3 of 9".
                    var carried=usable?GameCatalog.Find<ConsumableDef>(id):null;string most=carried!=null&&Inventory.Cap(carried)!=int.MaxValue?" of "+Inventory.Cap(carried):"";
                    string head=$"{DisplayNames.Item(id)} ×{count}{most}",quick=usable&&id==state.quickItem?"  ·  Quick item":"",full=head+ItemSays(id,recipes)+quick;
                    var line=Add(full,usable?()=>UseItem(id):()=>NoUse(id,recipes),id);
                    // A draught's or a meal's words can outrun the row: then the note says them while the cursor is on it.
                    PartText.Narrow(line,full,head+quick);
                    string says=carried!=null&&line.GetComponentInChildren<TMP_Text>().text!=full?$"{carried.displayName} {PartText.Restores(carried)}.":how;
                    line.gameObject.AddComponent<SelectNote>().selected=()=>Tell(vitals,says,last);
                    itemRows[line.name]=id;
                }
                Pager(entries.Length,perPage);
                Note(vitals+"\n"+how+"\n"+last);
            }
            else if(page==QuestsPage)
            {
                // A step can run long: at the type floor it takes two lines of a row rather than crossing its edge.
                rowFont=24;
                bool Main(QuestDef q)=>System.Array.IndexOf(MainQuests,q.id)>=0;
                bool Done(QuestDef q)=>state.questSteps[q.id]>=q.steps.Length;
                var quests=GameCatalog.All<QuestDef>().Where(q=>state.questSteps.ContainsKey(q.id)).OrderBy(q=>Done(q)?2:Main(q)?0:1).ThenBy(q=>Main(q)?0:1).ThenBy(q=>System.Array.IndexOf(MainQuests,q.id)).ToArray();
                // No quest covers the Gullet: with no main quest open, the log says what the objective panel says instead of "all done".
                string objective=quests.Any(q=>Main(q)&&!Done(q))?"":ObjectiveHud.CurrentLabel;
                string Says(QuestDef q)=>$"{q.title}: {q.description}"+(objective!=""?"\nNext: "+objective:"");
                const int perPage=8;
                foreach(var quest in quests.Skip(Window(quests.Length,perPage)).Take(perPage))
                {
                    var q=quest;
                    var line=Add(Done(q)?q.title+"  ·  COMPLETE":(Main(q)?"Main  ·  ":"")+q.title+"  ·  "+QuestText.Step(q,state.questSteps[q.id],state));
                    line.gameObject.AddComponent<SelectNote>().selected=()=>note.text=Says(q);
                }
                Pager(quests.Length,perPage);
                Note(quests.Length>0?Says(quests[offset]):"Orrin at the dock office wants to see you about Sorrel.");
            }
            // The chart draws itself (RouteChart) and links its own stops once this page's controls are linked, below.
            else if(page==ChartPage)Note(chart.Build(panel.transform,focus,stop=>Rebuild(false,stop),Close));
            else
            {
                if(leaveAsked)
                {
                    Add("Stay",StepBack);
                    Add("Return to title",()=>{Close();GameTime.Reset();GameServices.Current.Input.Blocked=false;SceneFlow.Current.LoadZone("Title");});
                    Add("Quit to desktop",()=>OrderlyQuit.Quit());
                    Note("Progress since your last save is lost. The game autosaves when you dock, warp, repair or finish a quest.");
                }
                else if(feelSettings)
                {
                    // Each press steps a choice to its next value and says so in the row: the cursor stays where it is.
                    Add("Back to sound controls",StepBack);
                    foreach(var choice in ImpactSettingsUi.Rows){var c=choice;Add(c.says(),()=>{c.next();Rebuild();});}
                    Note(ImpactSettingsUi.Note);
                }
                else if(!keyboardSettings)
                {
                    AudioSettingsUi.Add(panel.transform,new Vector2(170,290),1150,68,list);row=5;
                    // StepBack names these three rows by their places: Row_5, Row_6 and Row_7.
                    Add("Keyboard bindings",()=>{keyboardSettings=true;offset=0;feedback="";Rebuild(false);});
                    Add("Screen and impact",()=>{feelSettings=true;offset=0;feedback="";Rebuild(false);});
                    // A place on the page under its own name and no number (Extra), so Leave the game is still Row_7 beneath it. A press steps it and restates the row where the cursor is.
                    Extra(DifficultyUi.Row(),()=>{DifficultyUi.Next();Rebuild();},DifficultyUi.RowName);
                    Add("Leave the game",()=>{leaveAsked=true;Rebuild(false);});
                    Note(DeviceHints.AudioControls());
                }
                else
                {
                    Add("Back to sound controls",StepBack);
                    bool flying=GameServices.Current.Input.FlightMode;const int perPage=7;
                    foreach(var role in Roles.Skip(Window(Roles.Length,perPage)).Take(perPage))
                    {
                        var r=role;
                        Add($"{r.label}  ·  {(Listening&&listeningFor==r.label?"press a key":PromptService.TagFor(flying?r.flight:r.ground,PromptDevice.Keyboard))}",()=>BeginRebind(r));
                    }
                    Pager(Roles.Length,perPage);
                    Note(Listening?$"Press a key for {listeningFor}. {PromptService.EscTag()}{(PromptService.PadConnected?" or "+PromptService.CancelTagFor(PromptDevice.Gamepad):"")} cancels.":$"Select an action, then press its new key. {PromptService.EscTag()} cancels. On foot and in flight share one key.");
                }
            }
            var close=UiKit.Button(panel.transform,"Close","Back",StepBack);UiKit.Rect(close.gameObject,new(.5f,.5f),new(.5f,.5f),new(650,-435),new(270,55));list.Add(close);
            var footer=UiKit.Text(panel.transform,"Footer",$"{DeviceHints.Pages()}  TABS  ·  {PromptService.SubmitTag()}  SELECT  ·  {PromptService.CancelTagFor(PromptService.Device)}  BACK",24,UiKit.DimTextColor,TextAlignmentOptions.MidlineLeft);
            UiKit.Rect(footer.gameObject,new(.5f,.5f),new(.5f,.5f),new(32.5f,-435),new(945,55));
            var said=UiKit.Text(panel.transform,"Feedback",feedback,24,UiKit.TextColor,TextAlignmentOptions.TopLeft);UiKit.Rect(said.gameObject,new(.5f,.5f),new(.5f,.5f),new(170,-300),new(1220,30));
            UiKit.LinkVertical(tabButtons.ToArray());UiKit.LinkGrid(list);
            int tabAt=System.Array.IndexOf(order,page);
            foreach(var b in list)if(b is not Slider){var nav=b.navigation;nav.selectOnLeft=tabButtons[tabAt];b.navigation=nav;}
            for(int i=0;i<tabButtons.Count;i++){var nav=tabButtons[i].navigation;nav.selectOnRight=list[0];tabButtons[i].navigation=nav;}
            Selectable target=focus!=null?list.Concat(tabButtons).FirstOrDefault(s=>s.name==focus):null;
            // The list got shorter under its last row: the nearest one above it, not the top of the page. Any other row just takes the next item.
            if(target==null&&focus!=null&&focus.StartsWith("Row_")&&int.TryParse(focus.Substring(4),out int gone))
                for(int i=gone-1;i>=0&&target==null;i--){string above="Row_"+i;target=list.FirstOrDefault(s=>s.name==above);}
            EventSystem.current.SetSelectedGameObject((target!=null?target:list[0]).gameObject);
            if(page==ChartPage)chart.Link(tabButtons,tabAt,close);
        }
        /// <summary>What this hero wears first, the second module last of it; then spares by slot, the better part first; then what the other hero wears, which takes a move to use.
        /// In the order they came, it mixed all three: what's on Taren was fifth of six, between Sela's part and a spare. Like parts keep their order, so nothing shuffles under the cursor for no reason.</summary>
        static List<PartInstance> GearOrder(GameState state,MemberState member)
        {
            var inventory=RpgServices.Inventory;
            int Group(PartInstance p){var wearer=inventory.Wearer(p);return wearer==member?0:wearer==null?1:2;}
            return state.parts.Select(p=>(part:p,def:GameCatalog.Find<TechPartDef>(p.definitionId))).OrderBy(x=>Group(x.part)).ThenBy(x=>x.def!=null?(int)x.def.slot:int.MaxValue).ThenBy(x=>x.part.instanceId==member.module2?1:0).ThenByDescending(x=>x.def!=null?x.def.tier:0).Select(x=>x.part).ToList();
        }
        /// <summary>A row a press can only refuse: the player shouldn't have to read every count to find what can be made. It keeps its focus, so the refusal can still say why.</summary>
        static void Dim(Button row)
        {
            row.GetComponentInChildren<TMP_Text>().color=UiKit.DimTextColor;
            var state=row.colors;state.normalColor=new Color(.46f,.52f,.58f);row.colors=state;
            var icon=row.transform.Find("Item");if(icon!=null&&icon.TryGetComponent<Image>(out var image))image.color=new Color(.55f,.55f,.55f);
        }
        /// <summary>The open tab stays lit wherever focus is: the page's heading was the only sign of it.</summary>
        static void MarkOpenTab(Button tab)
        {
            var mark=UiKit.Panel(tab.transform,"ActiveTab",UiKit.GoldColor);mark.raycastTarget=false;
            var rect=mark.rectTransform;rect.anchorMin=new(0,.1f);rect.anchorMax=new(0,.9f);rect.pivot=new(0,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new(8,0);
            tab.GetComponentInChildren<TMP_Text>().color=UiKit.GoldColor;
        }
        static string SwapClause(GameState state)=>state.party.Count>1?"  ·  "+PromptService.Tag("Swap")+" switches member":"";
        static string PartyLine(GameState state,MemberState m)
        {
            // The weapon the hero fights with on foot, read off their definition (DevLoadout.Weapon): Idra's is an Edge, and by name she read as Sela, by her Emitter's Output.
            var stats=Gear.Calculate(state,m,GameCatalog.Find<CharacterDef>(m.id),DevLoadout.Weapon(m.id),GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
            return $"{DisplayNames.Speaker(m.id)}   Sync {m.level}   XP {m.xp}/{Levels.RequiredXp(m.level)}   Integrity {m.integrity:0}/{Levels.MaxIntegrity(m.level):0}   Output {stats.output:0}   Plating {stats.plating:0}";
        }
        /// <summary>The three stats the rows have no room for, in the words Gear and the bench use. The fight reads all three off the Edge's sum (PartyController.RefreshStats), whichever weapon the row above
        /// goes by, so that's the sum here. The two sums agree on these three now (a weapon not in hand keeps only its Output to itself, Gear.Calculate), so this is the fight's own line whichever is read.</summary>
        static string PartyTotals(GameState state,MemberState m)
        {
            var stats=Gear.Calculate(state,m,GameCatalog.Find<CharacterDef>(m.id),GearSlot.Edge,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
            return $"{DisplayNames.Speaker(m.id)}  ·  Response {stats.response:0}  ·  Resonance {stats.resonance:0}  ·  Fortune {stats.fortune:0}";
        }
        static string MasteryLine(string character,SkillDef skill)
        {
            int uses=SkillBook.Uses(character,skill.id),next=SkillBook.NextMasteryAt(uses);
            return $"Mastery {SkillBook.MasteryFor(uses)}"+(next>0?$" ({uses}/{next})":"");
        }
        static string ItemSays(string id,RecipeDef[] recipes)
        {
            var consumable=GameCatalog.Find<ConsumableDef>(id);if(consumable!=null)return "  ·  "+PartText.Restores(consumable);
            var material=GameCatalog.Find<MaterialDef>(id);if(material==null)return "";
            if(recipes.Any(r=>r.inputs!=null&&r.inputs.Any(i=>i.id==id)))return "  ·  used to fabricate";
            // A region's material may be for upgrades alone, and anything with a value has a buyer.
            return ForUpgrades(id)?"  ·  used to upgrade":material.value>0?"  ·  a shop will buy it":"  ·  keep this: no use yet";
        }
        static bool ForUpgrades(string id)=>GameCatalog.All<TechPartDef>().Any(p=>p!=null&&(p.upgradeMaterial==id||p.upgradeRare==id));
        static string SavedWhen(GameState saved){string at=DisplayNames.SavedAt(saved.savedAtUtc);return at==""?"":"  ·  "+at;}
        /// <summary>What a slot's row says it holds.</summary>
        static string Holding(GameState saved)=>saved!=null&&saved.party.Count>0?$"  ·  now {DisplayNames.Zone(saved.zone)}, Sync {saved.party[0].level}{SavedWhen(saved)}":"  ·  empty";
        /// <summary>The Swap button on a consumable's row makes it the quick item. Cycling in a fight was the only way, one press at a time, blind.</summary>
        void SetQuickItem()
        {
            var state=GameServices.Current.State;var row=EventSystem.current.currentSelectedGameObject;
            if(row==null||!itemRows.TryGetValue(row.name,out string id))return;
            UiSounds.Run(()=>
            {
                if(GameCatalog.Find<ConsumableDef>(id)==null)Refuse("Only a consumable can be the quick item.");
                else{state.quickItem=id;feedback=$"{DisplayNames.Item(id)} is now the quick item.";}
            });
            Rebuild();
        }
        void Refuse(string why){feedback=why;refusedAt=Time.unscaledTime;UiSounds.Error();}
        /// <summary>What the row under the cursor has to say, in the note under the list, which holds three lines. If it all runs over, the first line gives way from its end,
        /// a piece at a time, and the line under it never does: a price or a comparison is what the press is decided on.</summary>
        void Tell(params string[] lines)=>PartText.Fit(note,lines);
        /// <summary>Whether something awake and hostile is near the hero: a consumable isn't fabricated in the middle of a fight (prog159).</summary>
        static bool Hostile(){var party=PartyController.Current;return party!=null&&party.Active!=null&&GearEffects.InFight(party.Active.transform.position);}
        /// <summary>Rolls one affix again, at a bench, on the second press: the first says what it costs.</summary>
        void Retune(PartInstance item,TechPartDef def,int index)
        {
            var state=GameServices.Current.State;string name=def.displayName,key=item.instanceId+"#"+index;
            if(index<0||index>=item.affixes.Count){Rebuild();return;}
            var old=GameCatalog.Find<AffixDef>(item.affixes[index]);string was=old!=null?old.displayName:item.affixes[index];
            var (materials,scrip)=Gear.RetuneCost(def);
            bool other=(def.affixPool??System.Array.Empty<AffixDef>()).Any(a=>a!=null&&!item.affixes.Contains(a.id));
            if(!AtBench)Refuse("Re-tuning is bench work: take it to a Lattice Anvil or Hal's bench.");
            else if(!other)Refuse($"{name} can't carry anything but what it has: there's nothing for {was} to become.");
            else if(state.scrip<scrip||!RpgServices.Inventory.Has(materials))Refuse($"Re-tuning {was} on {name} takes {PartText.Price(state,materials,scrip)}.");
            else if(salvageAsked!=key){salvageAsked=key;feedback=$"Press again to re-tune {was} on {name} for {PartText.Price(state,materials,scrip)}. What it becomes is the roll's.";}
            else if(Gear.Retune(state,item,def,index,null,true))
            {
                salvageAsked=null;var now=GameCatalog.Find<AffixDef>(item.affixes[index]);
                feedback=$"Re-tuned {name}: {was} became {(now!=null?PartText.AffixSays(now,def):item.affixes[index])}.";
                PartyController.Current.RefreshStats();
            }
            Rebuild();
        }
        float refusedAt=-9;
        /// <summary>A material's row takes focus like any other, so a press on it answers like any other refused row.</summary>
        void NoUse(string id,RecipeDef[] recipes)
        {
            string name=DisplayNames.Item(id);
            var material=GameCatalog.Find<MaterialDef>(id);
            Refuse(recipes.Any(r=>r.inputs!=null&&r.inputs.Any(i=>i.id==id))?$"{name} is for fabricating. It can't be used from here.":
                ForUpgrades(id)?$"{name} is for upgrades at a bench. It can't be used from here.":
                material!=null&&material.value>0?$"{name} can't be used from here. A shop will buy it.":$"{name} has no use yet. Keep it.");
            Rebuild();
        }
        void UseItem(string id)
        {
            var (used,message)=PartyController.Current.Active.GetComponent<PlayerBrain>().UseItem(id);
            if(used)feedback=message;else Refuse(message);
            // Rows go by place: with the stack gone the next item is under the cursor, and a repeat press would spend it.
            if(used&&GameServices.Current.State.consumables[id]<=0)first.Begin();
            Rebuild();
        }
        void UsePart(PartInstance item,TechPartDef def,MemberState member)
        {
            var state=GameServices.Current.State;var inventory=RpgServices.Inventory;var wearer=inventory.Wearer(item);
            string name=def.displayName,who=DisplayNames.Speaker(member.id);
            if(gearMode<=1)followPart=item.instanceId;
            if(gearMode==3)
            {
                if(wearer!=null)Refuse($"{name} is on {DisplayNames.Speaker(wearer.id)}. Take it off with Action: Equip first.");
                else if(salvageAsked!=item.instanceId){salvageAsked=item.instanceId;feedback=$"Press again to salvage {name} for {PartText.Yield(def,item)}.";}
                else{string gave=PartText.Yield(def,item);if(inventory.Salvage(item,def)){salvageAsked=null;feedback=$"Salvaged {name} for {gave}.";}}
            }
            else if(gearMode==2)
            {
                // Bench work, and it costs scrip as well as Scrap Alloy now (prog160): from the pause menu in the field a part can be put on, taken off or broken down.
                if(item.upgrade>=Gear.MaxUpgrade)Refuse($"{name} is already at +{Gear.MaxUpgrade}.");
                else if(!AtBench)Refuse("Upgrades are bench work: take it to a Lattice Anvil or Hal's bench.");
                else if(Gear.Upgrade(state,item,def,true))feedback=$"Upgraded {name} to +{item.upgrade}: {PartText.Upgraded(def,item)}.";
                else{var (materials,scrip)=Gear.UpgradeCost(def,item.upgrade+1);Refuse($"Upgrading {name} to +{item.upgrade+1} takes {PartText.Price(state,materials,scrip)}.");}
            }
            else if(gearMode==4)Refuse("Select one of the part's affixes to re-tune it.");
            else if(gearMode==1&&def.slot!=GearSlot.Module)Refuse("Only a module fits module slot 2. Set Action to Equip for this part.");
            else if(wearer==member&&(gearMode==0||member.module2==item.instanceId)){inventory.Unequip(item);feedback=$"Took {name} off {who}.";}
            else if(inventory.Equip(member,item,def,gearMode==1))
                feedback=wearer!=null&&wearer!=member?$"Moved from {DisplayNames.Speaker(wearer.id)} to {who}.":gearMode==1?$"{name} is in {who}'s module slot 2.":$"Equipped {name} on {who}.";
            PartyController.Current.RefreshStats();Rebuild();
        }
        void Fabricate(RecipeDef r,Fabrication fabricator)
        {
            var inventory=RpgServices.Inventory;bool hostile=Hostile();
            var block=fabricator.Blocked(r,AtBench,hostile);var consumable=r.output==null?GameCatalog.Find<ConsumableDef>(r.consumableOutput):null;
            if(block!=Fabrication.Block.None||!fabricator.Craft(r,AtBench,UnityEngine.Random.value,out var made,hostile))
            {
                int rank=Mathf.Max(r.unlockLevel,r.tier);made=null;
                Refuse(block==Fabrication.Block.Rank?$"Needs {r.discipline} level {rank}. Making {r.discipline} trains it.":
                    block==Fabrication.Block.Bench?"Parts can only be fabricated at a Lattice Anvil or Hal's bench.":
                    block==Fabrication.Block.Hostile?$"Not in a fight: {r.displayName} waits until nothing nearby is awake.":
                    block==Fabrication.Block.Full?$"You can't carry more {r.displayName}: {Inventory.Cap(consumable)} is the most.":
                    "Not enough materials: "+string.Join(", ",r.inputs.Where(x=>inventory.Count(x.id)<x.count).Select(x=>$"{DisplayNames.Item(x.id)} {inventory.Count(x.id)}/{x.count}"))+".");
            }
            else if(made==null)feedback=$"Fabricated: {r.displayName}. You have {(consumable!=null?PartText.Carried(GameServices.Current.State,consumable):GameServices.Current.State.consumables[r.consumableOutput].ToString())}.";
            else
            {
                var wearer=inventory.EquipIfEmpty(made,r.output);string place=wearer!=null?$"Equipped on {DisplayNames.Speaker(wearer.id)}.":"Now in Gear.";
                // An overclock can be any affix the part can carry, and two of them from ladder 6: each said with what it does on this part.
                var rolled=made.affixes.Select(id=>GameCatalog.Find<AffixDef>(id)).Where(a=>a!=null).ToArray();
                feedback=rolled.Length>0?$"Overclocked: {r.displayName} is {string.Join(" and ",rolled.Select(a=>$"{a.displayName} ({PartText.AffixSays(a,r.output).Substring(a.displayName.Length+2)})"))}. {place}":$"Fabricated: {r.displayName}. {place}";
                PartyController.Current.RefreshStats();
            }
            Rebuild();
        }
        static int KeyBinding(InputAction action)
        {
            if(action!=null)for(int i=0;i<action.bindings.Count;i++)if(action.bindings[i].path.StartsWith("<Keyboard>"))return i;
            return -1;
        }
        void BeginRebind((string ground,string flight,string label) role)
        {
            rebind?.Cancel();
            var input=GameServices.Current.Input;var action=input.Active.FindAction(input.FlightMode?role.flight:role.ground);int binding=KeyBinding(action);
            if(binding<0)return;
            action.Disable();listening=action;listeningFor=role.label;Navigation(false);feedback="";
            rebind=action.PerformInteractiveRebinding(binding).WithControlsHavingToMatchPath("<Keyboard>").WithCancelingThrough("<Keyboard>/escape").OnComplete(_=>KeyChosen(role,action,binding)).OnCancel(_=>{EndListen();if(!closing)Rebuild();});rebind.Start();
            Rebuild();
        }
        /// <summary>Whatever ends a listen gives its action back and lets the pad navigate again.</summary>
        void EndListen()
        {
            rebind?.Dispose();rebind=null;listening?.Enable();listening=null;listeningFor=null;Navigation(true);
        }
        void KeyChosen((string ground,string flight,string label) role,InputAction action,int binding)
        {
            var input=GameServices.Current.Input;string path=action.bindings[binding].effectivePath;
            var twin=input.FlightMode?input.Ground.FindAction(role.ground):input.Flight.FindAction(role.flight);int twinKey=KeyBinding(twin);
            if(twinKey>=0)twin.ApplyBindingOverride(twinKey,path);
            EndListen();
            KeyBindingStore.Save(input.Asset.SaveBindingOverridesAsJson());
            var clash=Roles.FirstOrDefault(r=>r.label!=role.label&&string.Equals(input.Ground.FindAction(r.ground).bindings[KeyBinding(input.Ground.FindAction(r.ground))].effectivePath,path,System.StringComparison.OrdinalIgnoreCase));
            string key=PromptService.TagFor(input.FlightMode?role.flight:role.ground,PromptDevice.Keyboard);
            feedback=$"{role.label} is now {key}."+(clash.label!=null?$" {clash.label} uses {key} too.":"");feedbackDevice=PromptService.Device;
            PromptService.NotifyBindingsChanged();
        }
        void OnDestroy(){PromptService.Changed-=Restate;if(IsOpen)Close();}
    }
}
