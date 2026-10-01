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
        readonly string[] tabs={"Party","Gear","Fabricate","Items","Quests","Settings"};
        Canvas canvas;
        GameObject panel;
        int page,offset,gearMode;
        bool keyboardSettings;
        string feedback="";
        public bool IsOpen=>panel!=null;
        public bool AtBench;
        InputActionRebindingExtensions.RebindingOperation rebind;
        public void Open(int selected=0,bool bench=false){page=selected;offset=0;keyboardSettings=false;feedback="";AtBench=bench;GameServices.Current.Input.Blocked=true;GameTime.Paused=true;Rebuild(false);}
        public void Close(){if(AudioMix.Current!=null)AudioMix.Current.Flush();rebind?.Dispose();rebind=null;if(panel!=null)Destroy(panel);panel=null;if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;GameTime.Paused=false;}
        void Start(){canvas=UiKit.CreateCanvas("Menus",30,transform);PromptService.Changed+=Restate;}
        // A rebind listens for the very key that flips the device; never redraw under it.
        void Restate(){if(IsOpen&&rebind==null)Rebuild();}
        void Update()
        {
            var input=GameServices.Current.Input;
            if(rebind!=null){if(UiActions.Cancel.WasPressedThisFrame()){UiSounds.Cancel();rebind.Cancel();}return;}
            bool pause=input.Find("Pause").WasPressedThisFrame();
            if(!IsOpen){if(input.Blocked)return;if(pause)Open();else if(input.Find("Log").WasPressedThisFrame())Open(4);return;}
            if(pause||UiActions.Cancel.WasPressedThisFrame()){UiSounds.Cancel();Close();return;}
            if(page==1&&input.Find("Swap").WasPressedThisFrame()){PartyController.Current.Swap();Rebuild();}
            if(input.Find("MenuNext").WasPressedThisFrame()){page=(page+1)%tabs.Length;offset=0;Rebuild(false);}
            if(input.Find("MenuPrev").WasPressedThisFrame()){page=(page+tabs.Length-1)%tabs.Length;offset=0;Rebuild(false);}
        }
        void Rebuild(bool rememberFocus=true)
        {
            string focus=rememberFocus&&EventSystem.current.currentSelectedGameObject!=null?EventSystem.current.currentSelectedGameObject.name:null;
            if(panel!=null){panel.SetActive(false);Destroy(panel);}panel=UiKit.Dim(canvas.transform,.95f).gameObject;
            var title=UiKit.Heading(panel.transform,"Heading",tabs[page].ToUpperInvariant(),46,UiKit.TextColor,TextAlignmentOptions.Left);
            UiKit.Rect(title.gameObject,new(.5f,.5f),new(.5f,.5f),new(-150,400),new(1100,75));
            var list=new List<Selectable>();var tabButtons=new List<Selectable>();
            for(int i=0;i<tabs.Length;i++)
            {int n=i;var b=UiKit.Button(panel.transform,"Tab_"+tabs[i],tabs[i],()=>{page=n;offset=0;Rebuild(false);});UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(-690,270-i*95),new(290,65));tabButtons.Add(b);}
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
                foreach(var m in state.party)Add($"{m.id}   Sync {m.level}   XP {m.xp}/{Levels.RequiredXp(m.level)}   Integrity {m.integrity:0}");
                Add("Swap active member",()=>{PartyController.Current.Swap();Rebuild();});
                for(int i=1;i<=3;i++){string slot="slot"+i;Add("Save to "+slot,()=>{GameServices.Current.Saves.Save(slot,state);Rebuild();});}
                Note($"Scrip {state.scrip}  ·  Autosave on docking, warp and repair");
            }
            else if(page==1)
            {
                var member=state.party[state.activeMember];Add("Equip for "+member.id+" — Y swaps member",()=>{PartyController.Current.Swap();Rebuild();});
                string[] modes={"Equip","Equip module 2","Upgrade","Salvage"};
                Add("Action: "+modes[gearMode]+" (select to change)",()=>{gearMode=(gearMode+1)%modes.Length;Rebuild();});
                foreach(var item in state.parts.Skip(offset).Take(4))
                {
                    var def=GameCatalog.Find<TechPartDef>(item.definitionId);if(def==null)continue;
                    bool equipped=member.equipped.Values.Contains(item.instanceId)||member.module2==item.instanceId;
                    Add($"{(equipped?"[E] ":"")}{def.displayName}  T{def.tier} +{item.upgrade}  {def.slot}",()=>
                    {
                        bool ok=gearMode==3?RpgServices.Inventory.Salvage(item,def):gearMode==2?Gear.Upgrade(state,item):RpgServices.Inventory.Equip(member,item,def,gearMode==1);
                        feedback=ok?modes[gearMode]+" complete.":"Cannot do that: check equipped parts, materials and upgrade cap.";if(!ok)UiSounds.Error();
                        PartyController.Current.RefreshStats();Rebuild();
                    },def.slot.ToString());
                }
                Pager(state.parts.Count,4);
                Note("Upgrades cost 3 x (next level) Scrap Alloy. Equipped parts cannot be salvaged.");
            }
            else if(page==2)
            {
                var fabricator=new Fabrication(state);
                var recipes=GameCatalog.All<RecipeDef>().OrderBy(r=>r.discipline).ThenBy(r=>r.tier).ToArray();
                foreach(var recipe in recipes.Skip(offset).Take(6))
                {var r=recipe;Add($"{r.displayName}  T{r.tier}   {string.Join(", ",r.inputs.Select(x=>x.count+" "+x.id))}",()=>{bool ok=fabricator.Craft(r,AtBench,UnityEngine.Random.value,out var made);if(!ok)UiSounds.Error();feedback=ok?(made!=null&&made.affixes.Count>0?"Overclock! ":"Fabricated: ")+r.displayName:"Needs materials, discipline level "+r.unlockLevel+(r.output!=null?" and a bench.":".");Rebuild();},r.output!=null?r.output.slot.ToString():r.consumableOutput);}
                Pager(recipes.Length,6);
                Note("Disciplines: "+string.Join("  ·  ",System.Enum.GetValues(typeof(Discipline)).Cast<Discipline>().Select(d=>d+" "+fabricator.Level(d)))+(AtBench?"\nLattice bench linked":"\nParts require a bench. Consumables can be made anywhere."));
            }
            else if(page==3)
            {
                var entries=state.consumables.Keys.Concat(state.materials.Keys).Distinct().ToArray();
                foreach(var id in entries.Skip(offset).Take(6))
                {
                    bool usable=state.consumables.TryGetValue(id,out int count);if(!usable)count=state.materials[id];
                    Add(id+"  ×"+count,usable?()=>{PartyController.Current.Active.GetComponent<PlayerBrain>().UseItem(id);Rebuild();}:null,id);
                }
                Pager(entries.Length,6);Note("Select a consumable to use it on the active member.");
            }
            else if(page==4)
            {
                foreach(var q in GameCatalog.All<QuestDef>())if(state.questSteps.TryGetValue(q.id,out var step))Add(q.title+"  "+(step>=q.steps.Length?"COMPLETE":$"{step+1}/{q.steps.Length}"));
                Note("Orrin at the Dock Office knows why Sorrel went quiet.");
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
                    Note("Select a binding, then press a keyboard key. B cancels listening.");
                }
            }
            var close=UiKit.Button(panel.transform,"Close","B  Back",()=>{UiSounds.Cancel();Close();});UiKit.Rect(close.gameObject,new(.5f,.5f),new(.5f,.5f),new(650,-435),new(270,55));list.Add(close);
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
