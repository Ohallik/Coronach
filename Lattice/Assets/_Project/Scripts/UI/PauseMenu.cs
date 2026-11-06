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
        readonly string[] tabs={"Party","Skills","Gear","Fabricate","Items","Quests","Settings"};
        public int PageCount=>tabs.Length;
        int skillSlot=-1;
        /// <summary>Skill bindings name the active map's face actions (Fire/Roll/Lunge in flight).</summary>
        static bool InFlight=>ZoneController.Current!=null&&ZoneController.Current.Flight;
        /// <summary>The story's quests, in story order: the log leads with whichever is active.</summary>
        internal static readonly string[] MainQuests={"Sorrel","Hushwell","LanternRoad"};
        /// <summary>A rebind moves a role, not an action: each form has its own map, and flight calls the same buttons by other names.</summary>
        static readonly (string ground,string flight,string label)[] Roles=
        {
            ("Attack","Fire","Attack / Fire"),("Dodge","Roll","Dodge / Roll"),("Guard","Lunge","Guard / Lunge"),("Sprint","Boost","Sprint / Boost"),("QuickItem","Brake","Quick item / Brake"),
            ("Swap","Swap","Swap"),("Interact","Interact","Interact"),("LockOn","LockOn","Lock on"),("Pause","Pause","Pause"),("Log","Log","Quest log"),
            ("Skill1","Skill1","Skill 1"),("Skill2","Skill2","Skill 2"),("Skill3","Skill3","Skill 3"),("Skill4","Skill4","Skill 4"),
        };
        Canvas canvas;
        GameObject panel;
        TMP_Text note;
        int page,offset,gearMode;
        bool keyboardSettings,leaveAsked,closing;
        string feedback="",salvageAsked,listeningFor;
        PromptDevice feedbackDevice;
        float submitHeldUntil;
        /// <summary>The Items page's rows by name, for the Swap button: what's under the cursor when it's pressed.</summary>
        readonly Dictionary<string,string> itemRows=new();
        public bool IsOpen=>panel!=null;
        public bool AtBench;
        InputActionRebindingExtensions.RebindingOperation rebind;
        InputAction listening;
        /// <summary>A rebind is waiting for a key.</summary>
        public bool Listening=>rebind!=null;
        public void Open(int selected=0,bool bench=false)
        {
            page=selected;offset=gearMode=0;skillSlot=-1;keyboardSettings=leaveAsked=false;feedback="";salvageAsked=null;feedbackDevice=PromptService.Device;AtBench=bench;
            GameServices.Current.Input.Blocked=true;GameTime.Paused=true;
            // The bench opens from a conversation on a row that spends materials, and the A that ended the talk is often pressed twice.
            if(bench)HoldSubmit();
            Rebuild(false);
        }
        public void Close()
        {
            // Disposing a listen raises neither of its callbacks: it has to be cancelled, or its action stays dead.
            closing=true;rebind?.Cancel();closing=false;
            submitHeldUntil=0;Navigation(true);
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
            else if(leaveAsked){leaveAsked=false;Rebuild(false,"Row_6");}
            else Close();
        }
        void Start(){canvas=UiKit.CreateCanvas("Menus",30,transform);PromptService.Changed+=Restate;}
        // A rebind listens for the very key that flips the device; never redraw under it.
        void Restate()
        {
            if(!IsOpen||rebind!=null)return;
            // A stored result can name the other device's buttons. A question goes with its words, or the next press answers one nobody can see.
            if(PromptService.Device!=feedbackDevice){feedbackDevice=PromptService.Device;feedback="";salvageAsked=null;}
            Rebuild();
        }
        void Update()
        {
            var input=GameServices.Current.Input;
            if(submitHeldUntil>0&&Time.unscaledTime>=submitHeldUntil){submitHeldUntil=0;if(rebind==null)Navigation(true);}
            if(rebind!=null){if(UiActions.Cancel.WasPressedThisFrame())StepBack();return;}
            bool pause=input.Find("Pause").WasPressedThisFrame();
            if(!IsOpen){if(input.Blocked)return;if(pause)Open();else if(input.Find("Log").WasPressedThisFrame())Open(QuestsPage);return;}
            if(UiActions.Cancel.WasPressedThisFrame()){StepBack();return;}
            if(pause){UiSounds.Cancel();Close();return;}
            if((page==GearPage||page==SkillsPage)&&input.Find("Swap").WasPressedThisFrame())TrySwap(skillSlot>=0?"Row_"+(skillSlot+1):null);
            if(page==ItemsPage&&input.Find("Swap").WasPressedThisFrame())SetQuickItem();
            if(input.Find("MenuNext").WasPressedThisFrame())GoTo((page+1)%tabs.Length);
            if(input.Find("MenuPrev").WasPressedThisFrame())GoTo((page+tabs.Length-1)%tabs.Length);
        }
        void GoTo(int target){rebind?.Cancel();page=target;offset=0;skillSlot=-1;keyboardSettings=leaveAsked=false;feedback="";salvageAsked=null;Rebuild(false);}
        static void Navigation(bool on){var system=EventSystem.current;if(system!=null)system.sendNavigationEvents=on;}
        void HoldSubmit(){Navigation(false);submitHeldUntil=Time.unscaledTime+.3f;}
        void TrySwap(string want=null)
        {
            var party=PartyController.Current;if(party.members.Length<2)return;
            skillSlot=-1;salvageAsked=null;
            if(party.Swap())feedback="";
            else Refuse(DisplayNames.Speaker(party.members[(party.index+1)%party.members.Length].character)+" is down.");
            Rebuild(true,want);
        }
        void Rebuild(bool rememberFocus=true,string want=null)
        {
            var selected=EventSystem.current.currentSelectedGameObject;
            string focus=want??(rememberFocus&&selected!=null?selected.name:null);
            if(panel!=null){panel.SetActive(false);Destroy(panel);}panel=UiKit.Dim(canvas.transform,.95f).gameObject;
            var title=UiKit.Heading(panel.transform,"Heading",tabs[page].ToUpperInvariant(),46,UiKit.TextColor,TextAlignmentOptions.Left);
            UiKit.Rect(title.gameObject,new(.5f,.5f),new(.5f,.5f),new(-150,400),new(1100,75));
            var list=new List<Selectable>();var tabButtons=new List<Selectable>();
            for(int i=0;i<tabs.Length;i++)
            {
                int n=i;var b=UiKit.Button(panel.transform,"Tab_"+tabs[i],tabs[i],()=>GoTo(n));UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(-690,290-i*86),new(290,62));tabButtons.Add(b);
                if(i==page)MarkOpenTab(b);
            }
            var state=GameServices.Current.State;int row=0,rowFont=26;
            Button Add(string text,System.Action action=null,string icon=null,string name=null)
            {
                var b=UiKit.Row(panel.transform,name??"Row_"+row,text,action,rowFont);UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(170,290-row*68),new(1220,56));list.Add(b);row++;
                if(icon!=null){var image=UiKit.Icon(b.transform,"Item",icon,Color.white);UiKit.Rect(image.gameObject,new(0,.5f),new(0,.5f),new(38,0),new(48,48));b.GetComponentInChildren<TMP_Text>().margin=new Vector4(78,0,20,0);}
                return b;
            }
            // A page's title, in a row's place: it can't take focus, so no stray A lands on it.
            void Header(string text)
            {
                var t=UiKit.Text(panel.transform,"Header",text,26,UiKit.FrostColor,TextAlignmentOptions.Left);t.margin=new Vector4(42,0,42,0);
                UiKit.Rect(t.gameObject,new(.5f,.5f),new(.5f,.5f),new(170,290-row*68),new(1220,56));row++;
            }
            void Note(string text){note=UiKit.Text(panel.transform,"Note",text,24,UiKit.DimTextColor,TextAlignmentOptions.TopLeft);UiKit.Rect(note.gameObject,new(.5f,.5f),new(.5f,.5f),new(170,-360),new(1220,90));}
            // A list can shrink under the page it was on: stay on its last page rather than show nothing.
            int Window(int count,int perPage){if(offset>=count)offset=count>0?(count-1)/perPage*perPage:0;return offset;}
            void Pager(int count,int perPage)
            {
                if(count<=perPage)return;
                // Focus goes by role here: an index would land the next A on whatever took it, a buy, an equip or a salvage.
                Add($"Next page  ({offset/perPage+1}/{Mathf.CeilToInt(count/(float)perPage)})",()=>{rebind?.Cancel();offset=offset+perPage<count?offset+perPage:0;salvageAsked=null;Rebuild(true,"Pager");},null,"Pager");
            }
            if(page==PartyPage)
            {
                foreach(var m in state.party)Add(PartyLine(state,m));
                if(state.party.Count>1)Add("Swap active member",()=>TrySwap());
                for(int i=1;i<=3;i++)
                {
                    int number=i;string slot="slot"+i;var saved=GameServices.Current.Saves.Load(slot);
                    Add($"Save to slot {number}"+(saved!=null&&saved.party.Count>0?$"  ·  now {DisplayNames.Zone(saved.zone)}, Sync {saved.party[0].level}{SavedWhen(saved)}":"  ·  empty"),()=>{GameServices.Current.Saves.Save(slot,state);feedback=$"Saved to slot {number}.";Rebuild();});
                }
                Note($"{state.scrip} scrip  ·  The game autosaves when you dock, warp or repair");
            }
            else if(page==SkillsPage)
            {
                var member=state.party[state.activeMember];string who=DisplayNames.Speaker(member.id);var all=SkillBook.All(member.id);
                // Back and all eight skills fit above the note: no paging until a hero has more.
                int perPage=all.Length>8?7:8;
                string Binding(int at)=>GameHud.SkillBinding(at,InFlight);
                int SlotOf(SkillDef of){for(int i=0;i<SkillBook.Slots;i++)if(SkillBook.Equipped(member.id,i)==of)return i;return -1;}
                if(skillSlot<0)
                {
                    Header(who+"'s skills"+SwapClause(state));
                    for(int i=0;i<SkillBook.Slots;i++)
                    {
                        int slot=i;var skill=SkillBook.Equipped(member.id,i);if(skill==null)continue;
                        Add($"{Binding(i)}  {skill.displayName}  ·  {SkillBook.Cost(member.id,skill):0} Charge  ·  {MasteryLine(member.id,skill)}  ·  {skill.cooldown:0} s",
                            ()=>{skillSlot=slot;int at=System.Array.IndexOf(all,skill);offset=at>=0?at/perPage*perPage:0;feedback="";Rebuild(false,"Row_"+(at-offset+1));});
                    }
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
                            }:null);
                        line.gameObject.AddComponent<SelectNote>().selected=()=>note.text=Said(s);
                    }
                    Pager(all.Length,perPage);
                    Note(current!=null?Said(current):choosing);
                }
            }
            else if(page==GearPage)
            {
                var member=state.party[state.activeMember];var inventory=RpgServices.Inventory;
                Header("Equipping "+DisplayNames.Speaker(member.id)+SwapClause(state));
                string[] modes={"Equip","Equip in module slot 2","Upgrade","Salvage"};
                string[] hints={"Edges drive melee and the lunge, Emitters drive beams and flight fire. Select a worn part to take it off.","Select a module for the second module slot.","Each upgrade costs Scrap Alloy: three times the next level.","Select a spare part twice to break it down. Parts in use can't be salvaged."};
                Add("Action: "+modes[gearMode]+" (select to change)",()=>{gearMode=(gearMode+1)%modes.Length;salvageAsked=null;feedback="";Rebuild();});
                const int perPage=6;
                foreach(var item in state.parts.Skip(Window(state.parts.Count,perPage)).Take(perPage))
                {
                    var def=GameCatalog.Find<TechPartDef>(item.definitionId);if(def==null)continue;
                    var wearer=inventory.Wearer(item);
                    Add($"{PartText.Mark(wearer,member)}{def.displayName}  T{def.tier} +{item.upgrade}{PartText.Affixes(item)}  ·  {PartText.Effect(def,item)}{(wearer==member?"":PartText.Against(state,member,def,item))}",
                        ()=>UsePart(item,def,member),def.slot.ToString());
                }
                Pager(state.parts.Count,perPage);
                Note(PartText.Wearing(state,member)+"\n"+hints[gearMode]);
            }
            else if(page==FabricatePage)
            {
                var fabricator=new Fabrication(state);var inventory=RpgServices.Inventory;
                var recipes=GameCatalog.All<RecipeDef>().OrderBy(r=>r.discipline).ThenBy(r=>r.tier).ToArray();
                const int perPage=6;
                foreach(var recipe in recipes.Skip(Window(recipes.Length,perPage)).Take(perPage))
                {
                    var r=recipe;
                    string does=r.output!=null?PartText.Effect(r.output):PartText.Restores(GameCatalog.Find<ConsumableDef>(r.consumableOutput));
                    string needs=string.Join(", ",r.inputs.Select(x=>$"{DisplayNames.Item(x.id)} {inventory.Count(x.id)}/{x.count}"));
                    Add($"{r.displayName}  T{r.tier}  ·  {does}  ·  {needs}",()=>Fabricate(r,fabricator),r.output!=null?r.output.slot.ToString():r.consumableOutput);
                }
                Pager(recipes.Length,perPage);
                string Trained(Discipline d){int level=fabricator.Level(d);return level>=10?$"{d} {level}":$"{d} {level} ({fabricator.Xp(d)%30}/30)";}
                Note(string.Join("  ·  ",System.Enum.GetValues(typeof(Discipline)).Cast<Discipline>().Select(d=>Trained(d)))+(AtBench?"\nParts and consumables can both be made here.":"\nParts need a Lattice Anvil or Hal's bench. Consumables can be made anywhere."));
            }
            else if(page==ItemsPage)
            {
                var hero=PartyController.Current.Active;string who=DisplayNames.Speaker(hero.character);var recipes=GameCatalog.All<RecipeDef>();
                var entries=state.consumables.Where(p=>p.Value>0).Select(p=>p.Key).Concat(state.materials.Where(p=>p.Value>0).Select(p=>p.Key)).Distinct().ToArray();
                const int perPage=8;itemRows.Clear();
                foreach(var id in entries.Skip(Window(entries.Length,perPage)).Take(perPage))
                {
                    bool usable=state.consumables.TryGetValue(id,out int count)&&count>0;if(!usable)count=state.materials[id];
                    var line=Add($"{DisplayNames.Item(id)} ×{count}{ItemSays(id,recipes)}{(usable&&id==state.quickItem?"  ·  Quick item":"")}",usable?()=>UseItem(id):null,id);
                    itemRows[line.name]=id;
                }
                Pager(entries.Length,perPage);
                Note($"{who}  ·  Integrity {hero.Health.integrity:0}/{hero.Health.maximum:0}  ·  Charge {hero.charge:0}/100\nSelect a consumable to use it on {who}. Press {PromptService.Tag("Swap")} on one to make it the quick item.\nQuick item: {DisplayNames.Item(state.quickItem)}. In a fight, {DeviceHints.CycleItem()} changes it.");
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
            else
            {
                if(leaveAsked)
                {
                    Add("Stay",StepBack);
                    Add("Return to title",()=>{Close();GameTime.Reset();GameServices.Current.Input.Blocked=false;SceneFlow.Current.LoadZone("Title");});
                    Add("Quit to desktop",Application.Quit);
                    Note("Progress since your last save is lost. The game autosaves when you dock, warp or repair.");
                }
                else if(!keyboardSettings)
                {
                    AudioSettingsUi.Add(panel.transform,new Vector2(170,290),1150,68,list);row=5;
                    Add("Keyboard bindings",()=>{keyboardSettings=true;offset=0;feedback="";Rebuild(false);});
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
            foreach(var b in list)if(b is not Slider){var nav=b.navigation;nav.selectOnLeft=tabButtons[page];b.navigation=nav;}
            for(int i=0;i<tabButtons.Count;i++){var nav=tabButtons[i].navigation;nav.selectOnRight=list[0];tabButtons[i].navigation=nav;}
            Selectable target=focus!=null?list.Concat(tabButtons).FirstOrDefault(s=>s.name==focus):null;
            // The list got shorter under its last row: the nearest one above it, not the top of the page. Any other row just takes the next item.
            if(target==null&&focus!=null&&focus.StartsWith("Row_")&&int.TryParse(focus.Substring(4),out int gone))
                for(int i=gone-1;i>=0&&target==null;i--){string above="Row_"+i;target=list.FirstOrDefault(s=>s.name==above);}
            EventSystem.current.SetSelectedGameObject((target!=null?target:list[0]).gameObject);
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
            var stats=Gear.Calculate(state,m,GameCatalog.Find<CharacterDef>(m.id),m.id=="Taren"?GearSlot.Edge:GearSlot.Emitter,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
            return $"{DisplayNames.Speaker(m.id)}   Sync {m.level}   XP {m.xp}/{Levels.RequiredXp(m.level)}   Integrity {m.integrity:0}/{Levels.MaxIntegrity(m.level):0}   Output {stats.output:0}   Plating {stats.plating:0}";
        }
        static string MasteryLine(string character,SkillDef skill)
        {
            int uses=SkillBook.Uses(character,skill.id),next=SkillBook.NextMasteryAt(uses);
            return $"Mastery {SkillBook.MasteryFor(uses)}"+(next>0?$" ({uses}/{next})":"");
        }
        static string ItemSays(string id,RecipeDef[] recipes)
        {
            var consumable=GameCatalog.Find<ConsumableDef>(id);if(consumable!=null)return "  ·  "+PartText.Restores(consumable);
            if(GameCatalog.Find<MaterialDef>(id)==null)return "";
            return recipes.Any(r=>r.inputs!=null&&r.inputs.Any(i=>i.id==id))?"  ·  used to fabricate":"  ·  keep this: no use yet";
        }
        static string SavedWhen(GameState saved){string at=DisplayNames.SavedAt(saved.savedAtUtc);return at==""?"":"  ·  "+at;}
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
        void Refuse(string why){feedback=why;UiSounds.Error();}
        void UseItem(string id)
        {
            var (used,message)=PartyController.Current.Active.GetComponent<PlayerBrain>().UseItem(id);
            if(used)feedback=message;else Refuse(message);
            // Rows go by place: with the stack gone the next item is under the cursor, and a repeat press would spend it.
            if(used&&GameServices.Current.State.consumables[id]<=0)HoldSubmit();
            Rebuild();
        }
        void UsePart(PartInstance item,TechPartDef def,MemberState member)
        {
            var state=GameServices.Current.State;var inventory=RpgServices.Inventory;var wearer=inventory.Wearer(item);
            string name=def.displayName,who=DisplayNames.Speaker(member.id);
            if(gearMode==3)
            {
                if(wearer!=null)Refuse($"{name} is on {DisplayNames.Speaker(wearer.id)}. Take it off with Action: Equip first.");
                else if(salvageAsked!=item.instanceId){salvageAsked=item.instanceId;feedback=$"Press again to salvage {name} for {PartText.Yield(def)}.";}
                else if(inventory.Salvage(item,def)){salvageAsked=null;feedback=$"Salvaged {name} for {PartText.Yield(def)}.";}
            }
            else if(gearMode==2)
            {
                int cost=3*(item.upgrade+1);
                if(item.upgrade>=5)Refuse($"{name} is already at +5.");
                else if(Gear.Upgrade(state,item))feedback=$"Upgraded {name} to +{item.upgrade}: {PartText.Effect(def,item)}.";
                else Refuse($"Upgrading {name} takes {cost} Scrap Alloy. You have {inventory.Count("ScrapAlloy")}. Salvage a spare part for more.");
            }
            else if(gearMode==1&&def.slot!=GearSlot.Module)Refuse("Only a module fits module slot 2. Set Action to Equip for this part.");
            else if(wearer==member&&(gearMode==0||member.module2==item.instanceId)){inventory.Unequip(item);feedback=$"Took {name} off {who}.";}
            else if(inventory.Equip(member,item,def,gearMode==1))
                feedback=wearer!=null&&wearer!=member?$"Moved from {DisplayNames.Speaker(wearer.id)} to {who}.":gearMode==1?$"{name} is in {who}'s module slot 2.":$"Equipped {name} on {who}.";
            PartyController.Current.RefreshStats();Rebuild();
        }
        void Fabricate(RecipeDef r,Fabrication fabricator)
        {
            var inventory=RpgServices.Inventory;
            if(!fabricator.Craft(r,AtBench,UnityEngine.Random.value,out var made))
            {
                Refuse(fabricator.Level(r.discipline)<r.unlockLevel?$"Needs {r.discipline} level {r.unlockLevel}. Each fabrication trains it.":
                    r.output!=null&&!AtBench?"Parts can only be fabricated at a Lattice Anvil or Hal's bench.":
                    "Not enough materials: "+string.Join(", ",r.inputs.Where(x=>inventory.Count(x.id)<x.count).Select(x=>$"{DisplayNames.Item(x.id)} {inventory.Count(x.id)}/{x.count}"))+".");
            }
            else if(made==null)feedback=$"Fabricated: {r.displayName}. You have {GameServices.Current.State.consumables[r.consumableOutput]}.";
            else
            {
                var wearer=inventory.EquipIfEmpty(made,r.output);string place=wearer!=null?$"Equipped on {DisplayNames.Speaker(wearer.id)}.":"Now in Gear.";
                var affix=made.affixes.Count>0?GameCatalog.Find<AffixDef>(made.affixes[0]):null;
                feedback=affix!=null?$"Overclocked: {r.displayName} is {affix.displayName} ({PartText.Says(affix.bonus)}). {place}":$"Fabricated: {r.displayName}. {place}";
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
            PlayerPrefs.SetString("keyboardBindings",input.Asset.SaveBindingOverridesAsJson());
            var clash=Roles.FirstOrDefault(r=>r.label!=role.label&&string.Equals(input.Ground.FindAction(r.ground).bindings[KeyBinding(input.Ground.FindAction(r.ground))].effectivePath,path,System.StringComparison.OrdinalIgnoreCase));
            string key=PromptService.TagFor(input.FlightMode?role.flight:role.ground,PromptDevice.Keyboard);
            feedback=$"{role.label} is now {key}."+(clash.label!=null?$" {clash.label} uses {key} too.":"");feedbackDevice=PromptService.Device;
            PromptService.NotifyBindingsChanged();
        }
        void OnDestroy(){PromptService.Changed-=Restate;if(IsOpen)Close();}
    }
}
