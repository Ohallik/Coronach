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
        Canvas canvas;
        GameObject panel;
        int page,offset,gearMode;
        bool keyboardSettings;
        string feedback="";
        public bool IsOpen=>panel!=null;
        public bool AtBench;
        InputActionRebindingExtensions.RebindingOperation rebind;
        public void Open(int selected=0,bool bench=false){page=selected;offset=0;skillSlot=-1;keyboardSettings=false;feedback="";AtBench=bench;GameServices.Current.Input.Blocked=true;GameTime.Paused=true;Rebuild(false);}
        public void Close(){if(AudioMix.Current!=null)AudioMix.Current.Flush();rebind?.Dispose();rebind=null;if(panel!=null)Destroy(panel);panel=null;if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;GameTime.Paused=false;}
        void Start(){canvas=UiKit.CreateCanvas("Menus",30,transform);PromptService.Changed+=Restate;}
        // A rebind listens for the very key that flips the device; never redraw under it.
        void Restate(){if(IsOpen&&rebind==null)Rebuild();}
        void Update()
        {
            var input=GameServices.Current.Input;
            if(rebind!=null){if(UiActions.Cancel.WasPressedThisFrame()){UiSounds.Cancel();rebind.Cancel();}return;}
            bool pause=input.Find("Pause").WasPressedThisFrame();
            if(!IsOpen){if(input.Blocked)return;if(pause)Open();else if(input.Find("Log").WasPressedThisFrame())Open(QuestsPage);return;}
            if(pause||UiActions.Cancel.WasPressedThisFrame()){UiSounds.Cancel();Close();return;}
            if((page==GearPage||page==SkillsPage)&&input.Find("Swap").WasPressedThisFrame()){PartyController.Current.Swap();skillSlot=-1;Rebuild();}
            if(input.Find("MenuNext").WasPressedThisFrame()){page=(page+1)%tabs.Length;offset=0;skillSlot=-1;Rebuild(false);}
            if(input.Find("MenuPrev").WasPressedThisFrame()){page=(page+tabs.Length-1)%tabs.Length;offset=0;skillSlot=-1;Rebuild(false);}
        }
        void Rebuild(bool rememberFocus=true)
        {
            string focus=rememberFocus&&EventSystem.current.currentSelectedGameObject!=null?EventSystem.current.currentSelectedGameObject.name:null;
            if(panel!=null){panel.SetActive(false);Destroy(panel);}panel=UiKit.Dim(canvas.transform,.95f).gameObject;
            var title=UiKit.Heading(panel.transform,"Heading",tabs[page].ToUpperInvariant(),46,UiKit.TextColor,TextAlignmentOptions.Left);
            UiKit.Rect(title.gameObject,new(.5f,.5f),new(.5f,.5f),new(-150,400),new(1100,75));
            var list=new List<Selectable>();var tabButtons=new List<Selectable>();
            for(int i=0;i<tabs.Length;i++)
            {int n=i;var b=UiKit.Button(panel.transform,"Tab_"+tabs[i],tabs[i],()=>{page=n;offset=0;skillSlot=-1;Rebuild(false);});UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(-690,290-i*86),new(290,62));tabButtons.Add(b);}
            var state=GameServices.Current.State;int row=0;
            void Add(string text,System.Action action=null,string icon=null)
            {
                var b=UiKit.Row(panel.transform,"Row_"+row,text,action,26);UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(170,290-row*68),new(1220,56));list.Add(b);row++;
                if(icon!=null){var image=UiKit.Icon(b.transform,"Item",icon,Color.white);UiKit.Rect(image.gameObject,new(0,.5f),new(0,.5f),new(38,0),new(48,48));b.GetComponentInChildren<TMP_Text>().margin=new Vector4(78,0,20,0);}
            }
            void Note(string text){var t=UiKit.Text(panel.transform,"Note",(feedback==""?"":feedback+"\n")+text,24,UiKit.DimTextColor,TextAlignmentOptions.TopLeft);UiKit.Rect(t.gameObject,new(.5f,.5f),new(.5f,.5f),new(170,-335),new(1220,100));}
            void Pager(int count,int perPage)
            {
                if(count<=perPage)return;
                Add($"Next page  ({offset/perPage+1}/{Mathf.CeilToInt(count/(float)perPage)})",()=>{offset=offset+perPage<count?offset+perPage:0;Rebuild();});
            }
            if(page==0)
            {
                foreach(var m in state.party)Add($"{DisplayNames.Speaker(m.id)}   Sync {m.level}   XP {m.xp}/{Levels.RequiredXp(m.level)}   Integrity {m.integrity:0}");
                Add("Swap active member",()=>{PartyController.Current.Swap();Rebuild();});
                for(int i=1;i<=3;i++){string slot="slot"+i;Add("Save to slot "+i,()=>{GameServices.Current.Saves.Save(slot,state);Rebuild();});}
                Note($"{state.scrip} scrip  ·  The game autosaves when you dock, warp or repair");
            }
            else if(page==SkillsPage)
            {
                var member=state.party[state.activeMember];string who=DisplayNames.Speaker(member.id);var all=SkillBook.All(member.id);
                if(skillSlot<0)
                {
                    Add(who+"'s skills — "+PromptService.Tag("Swap")+" switches member",()=>{PartyController.Current.Swap();Rebuild();});
                    for(int i=0;i<SkillBook.Slots;i++)
                    {
                        int slot=i;var skill=SkillBook.Equipped(member.id,i);if(skill==null)continue;
                        Add($"{GameHud.SkillBinding(i,false)}  {skill.displayName}  ·  {SkillBook.Cost(member.id,skill):0} Charge  ·  Mastery {SkillBook.Mastery(member.id,skill.id)}",
                            ()=>{skillSlot=slot;offset=0;Rebuild(false);});
                    }
                    var next=all.Where(s=>!SkillBook.Knows(member,s)).OrderBy(s=>s.unlockLevel).FirstOrDefault();
                    Note($"{all.Count(s=>SkillBook.Knows(member,s))} of {all.Length} skills learned"+(next!=null?$"  ·  next: {next.displayName} at Sync {next.unlockLevel}":"")+
                        ".\nSelect a slot to change its skill. Each use raises a skill's mastery: more damage, less Charge.");
                }
                else
                {
                    var current=SkillBook.Equipped(member.id,skillSlot);
                    Add("Back to "+who+"'s slots",()=>{UiSounds.Cancel();skillSlot=-1;Rebuild(false);});
                    foreach(var skill in all.Skip(offset).Take(5))
                    {
                        var s=skill;bool known=SkillBook.Knows(member,s);
                        Add(known?$"{(s==current?"[E] ":"")}{s.displayName}  ·  {SkillBook.Cost(member.id,s):0} Charge  ·  {s.description}":$"Locked  ·  {s.displayName}  ·  learned at Sync {s.unlockLevel}",
                            known?()=>{int slot=skillSlot;SkillBook.Equip(member,slot,s);feedback=$"{s.displayName} is on {GameHud.SkillBinding(slot,false)}.";skillSlot=-1;Rebuild(false);}:null);
                    }
                    Pager(all.Length,5);
                    Note($"Choosing for {GameHud.SkillBinding(skillSlot,false)} (now {current?.displayName}). A skill already in another slot trades places.");
                }
            }
            else if(page==GearPage)
            {
                var member=state.party[state.activeMember];Add("Equipping "+DisplayNames.Speaker(member.id)+" — "+PromptService.Tag("Swap")+" switches member",()=>{PartyController.Current.Swap();Rebuild();});
                string[] modes={"Equip","Equip in module slot 2","Upgrade","Salvage"};string[] done={"Equipped.","Equipped in module slot 2.","Upgraded.","Salvaged for materials."};
                Add("Action: "+modes[gearMode]+" (select to change)",()=>{gearMode=(gearMode+1)%modes.Length;Rebuild();});
                foreach(var item in state.parts.Skip(offset).Take(4))
                {
                    var def=GameCatalog.Find<TechPartDef>(item.definitionId);if(def==null)continue;
                    bool equipped=member.equipped.Values.Contains(item.instanceId)||member.module2==item.instanceId;
                    Add($"{(equipped?"[E] ":"")}{def.displayName}  T{def.tier} +{item.upgrade}  {def.slot}",()=>
                    {
                        bool ok=gearMode==3?RpgServices.Inventory.Salvage(item,def):gearMode==2?Gear.Upgrade(state,item):RpgServices.Inventory.Equip(member,item,def,gearMode==1);
                        feedback=ok?done[gearMode]:gearMode==3?"Unequip this part before salvaging it.":gearMode==2?"Not enough Scrap Alloy, or the part is already +5.":"That part can't go in that slot.";if(!ok)UiSounds.Error();
                        PartyController.Current.RefreshStats();Rebuild();
                    },def.slot.ToString());
                }
                Pager(state.parts.Count,4);
                Note("Each upgrade costs Scrap Alloy: three times the next level. Equipped parts can't be salvaged.");
            }
            else if(page==FabricatePage)
            {
                var fabricator=new Fabrication(state);
                var recipes=GameCatalog.All<RecipeDef>().OrderBy(r=>r.discipline).ThenBy(r=>r.tier).ToArray();
                foreach(var recipe in recipes.Skip(offset).Take(6))
                {var r=recipe;Add($"{r.displayName}  T{r.tier}   {string.Join(", ",r.inputs.Select(x=>x.count+" "+DisplayNames.Item(x.id)))}",()=>{bool ok=fabricator.Craft(r,AtBench,UnityEngine.Random.value,out var made);if(!ok)UiSounds.Error();feedback=ok?(made!=null&&made.affixes.Count>0?"Overclocked: ":"Fabricated: ")+r.displayName:fabricator.Level(r.discipline)<r.unlockLevel?$"Needs {r.discipline} level {r.unlockLevel}.":r.output!=null&&!AtBench?"Parts can only be fabricated at a bench.":"Not enough materials.";Rebuild();},r.output!=null?r.output.slot.ToString():r.consumableOutput);}
                Pager(recipes.Length,6);
                Note("Disciplines: "+string.Join("  ·  ",System.Enum.GetValues(typeof(Discipline)).Cast<Discipline>().Select(d=>d+" "+fabricator.Level(d)))+(AtBench?"\nLattice bench linked":"\nParts require a bench. Consumables can be made anywhere."));
            }
            else if(page==ItemsPage)
            {
                var entries=state.consumables.Keys.Concat(state.materials.Keys).Distinct().ToArray();
                foreach(var id in entries.Skip(offset).Take(6))
                {
                    bool usable=state.consumables.TryGetValue(id,out int count);if(!usable)count=state.materials[id];
                    Add(DisplayNames.Item(id)+"  ×"+count,usable?()=>{PartyController.Current.Active.GetComponent<PlayerBrain>().UseItem(id);Rebuild();}:null,id);
                }
                Pager(entries.Length,6);Note("Select a consumable to use it on the active member.");
            }
            else if(page==QuestsPage)
            {
                string lead=null;
                foreach(var q in GameCatalog.All<QuestDef>())if(state.questSteps.TryGetValue(q.id,out var step))
                {
                    bool done=step>=q.steps.Length;
                    Add(done?q.title+"  ·  COMPLETE":q.title+"  ·  "+QuestText.Step(q,step,state));
                    if(!done&&lead==null&&!string.IsNullOrEmpty(q.description))lead=q.description;
                }
                Note(lead??(state.questSteps.Count==0?"Orrin at the dock office knows why Sorrel went quiet.":"Every quest here is finished. People around the Halo may still need a hand."));
            }
            else
            {
                if(!keyboardSettings)
                {
                    AudioSettingsUi.Add(panel.transform,new Vector2(170,290),1150,68,list);row=5;
                    Add("Keyboard bindings",()=>{keyboardSettings=true;Rebuild(false);});
                    Note(DeviceHints.AudioControls());
                }
                else
                {
                    Add("Back to sound controls",()=>{UiSounds.Cancel();keyboardSettings=false;Rebuild(false);});
                    foreach(string name in new[]{"Attack","Dodge","Guard","Swap","Interact","Pause"})
                    {string actionName=name;Add("Rebind keyboard: "+name,()=>BeginRebind(actionName));}
                    Note("Select a binding, then press a key. "+PromptService.EscTag()+" cancels.");
                }
            }
            var close=UiKit.Button(panel.transform,"Close","Back",()=>{UiSounds.Cancel();Close();});UiKit.Rect(close.gameObject,new(.5f,.5f),new(.5f,.5f),new(650,-435),new(270,55));list.Add(close);
            UiKit.LinkVertical(tabButtons.ToArray());UiKit.LinkGrid(list);
            foreach(var b in list)if(b is not Slider){var nav=b.navigation;nav.selectOnLeft=tabButtons[page];b.navigation=nav;}
            for(int i=0;i<tabButtons.Count;i++){var nav=tabButtons[i].navigation;nav.selectOnRight=list[0];tabButtons[i].navigation=nav;}
            var remembered=list.Concat(tabButtons).FirstOrDefault(s=>s.name==focus);
            EventSystem.current.SetSelectedGameObject((remembered!=null?remembered:list[0]).gameObject);
        }
        void BeginRebind(string name)
        {
            var input=GameServices.Current.Input;var action=input.Find(name);if(action==null)return;
            int binding=-1;for(int i=0;i<action.bindings.Count;i++)if(action.bindings[i].path.StartsWith("<Keyboard>")){binding=i;break;}
            if(binding<0)return;action.Disable();
            void Finish(){rebind?.Dispose();rebind=null;action.Enable();PlayerPrefs.SetString("keyboardBindings",input.Asset.SaveBindingOverridesAsJson());PromptService.NotifyBindingsChanged();Rebuild();}
            rebind=action.PerformInteractiveRebinding(binding).WithControlsHavingToMatchPath("<Keyboard>").WithCancelingThrough("<Keyboard>/escape").OnComplete(_=>Finish()).OnCancel(_=>Finish());rebind.Start();
        }
        void OnDestroy(){PromptService.Changed-=Restate;if(IsOpen)Close();}
    }
}
