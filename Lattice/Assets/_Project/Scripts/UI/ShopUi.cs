using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Lattice.UI
{
    public sealed class ShopUi:MonoBehaviour
    {
        const int PerPage=6;
        /// <summary>Where the list stands with someone selling it: to the right of them, in a frame no wider than the screen's margins allow. The rows keep their width.</summary>
        const float SellerListAt=206;
        Canvas canvas;GameObject panel;int page,offset;ShopInventory inventory;string feedback="",sellAsked,seller;FirstPress first;
        public bool IsOpen=>panel!=null;
        void Start(){canvas=UiKit.CreateCanvas("Shop",35,transform);PromptService.Changed+=Restate;}
        void OnDestroy(){PromptService.Changed-=Restate;}
        void Restate(){if(IsOpen)Rebuild();}
        /// <param name="from">Who's selling, the speaker of the conversation that opened it: their stock with their face on it, as the dialogue box draws them. It was a list on a black screen headed only 'Arch Market'.</param>
        public void Open(ShopInventory source,string from=null)
        {
            inventory=source;seller=from;page=offset=0;feedback="";sellAsked=null;GameServices.Current.Input.Blocked=true;MusicDirector.Shop(this,true);
            // The A that ended the conversation is often pressed twice: submit waits a moment.
            first.Begin();
            // A moment doesn't cover a thumb still tapping through someone's lines, and its next A bought a 240-scrip part nobody had read. So a shop a conversation opens lands on the page's toggle, which spends nothing: another tap only turns the page, with the cursor left on the toggle, and the first offer is one press down.
            Rebuild(string.IsNullOrEmpty(from)?"ShopRow1":"ShopRow0");
        }
        public void Close(){first.End();if(panel!=null)Destroy(panel);panel=null;GameServices.Current.Input.Blocked=false;MusicDirector.Shop(this,false);}
        void OnDisable(){MusicDirector.Shop(this,false);}
        // The toggle, not an index: the same row on the other page would be a buy or a sale.
        void Switch(){page=1-page;offset=0;feedback="";sellAsked=null;Rebuild("ShopRow0");}
        void Update()
        {
            if(!IsOpen)return;first.Step();
            if(UiActions.Cancel.WasPressedThisFrame()){UiSounds.Cancel();Close();return;}
            if(GameServices.Current.Input.Find("MenuNext").WasPressedThisFrame()||GameServices.Current.Input.Find("MenuPrev").WasPressedThisFrame())Switch();
        }
        void Rebuild(string want=null)
        {
            var selected=EventSystem.current.currentSelectedGameObject;string focus=want??(selected!=null?selected.name:null);
            if(panel!=null){panel.SetActive(false);Destroy(panel);}panel=UiKit.Dim(canvas.transform,UiKit.MenuDim).gameObject;
            var face=string.IsNullOrEmpty(seller)?null:PortraitLookup.Get(seller,BodyForm.Natural,PortraitEmotion.Happy);float listAt=face!=null?SellerListAt:0;
            UiKit.SolidFrame(panel.transform,"ShopPanel",new(.5f,.5f),new(0,-13),new(face!=null?1680:1290,980));
            if(face!=null)
            {
                UiKit.Bust(panel.transform,"SellerPortrait",new(.5f,.5f),new(-806,-472),500).sprite=face;
                var who=UiKit.Heading(panel.transform,"SellerName",DisplayNames.Speaker(seller),32,UiKit.GoldColor);UiKit.Rect(who.gameObject,new(.5f,.5f),new(.5f,.5f),new(-596,14),new(400,50));
            }
            var state=GameServices.Current.State;var active=state.party[state.activeMember];
            string trade=inventory!=null&&!string.IsNullOrEmpty(inventory.title)?inventory.title:"OUTFITTER";
            var heading=UiKit.Heading(panel.transform,"Heading",trade+(page==0?" · BUY":" · SELL")+"   "+state.scrip+" SCRIP",42,UiKit.TextColor);
            UiKit.Rect(heading.gameObject,new(.5f,.5f),new(.5f,.5f),new(listAt,390),new(1180,80));
            var buttons=new List<Selectable>();int row=0;
            void Add(string label,System.Action action,string name=null)
            {
                var b=UiKit.Button(panel.transform,name??"ShopRow"+row,label,action);b.GetComponentInChildren<TMP_Text>().fontSize=30;
                UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(listAt,260-row++*76),new(1180,62));buttons.Add(b);
            }
            Add((page==0?"Sell parts":"Buy supplies")+"  ·  "+DeviceHints.Pages(),Switch);
            var offers=new List<(string label,Action action)>();
            int priceIndex=0;
            int Price(int fallback){int i=priceIndex++;return inventory!=null&&inventory.prices!=null&&i<inventory.prices.Length?Mathf.Max(0,inventory.prices[i]):fallback;}
            void Buy(string name,string says,string against,int cost,Func<string> give)
            {
                offers.Add(($"{name}{says}  —  {cost} scrip{against}",()=>
                {
                    if(state.scrip<cost){feedback=$"Not enough scrip: {name} costs {cost}, you have {state.scrip}.";UiSounds.Error();}
                    else{state.scrip-=cost;feedback=$"Purchased {name}.{give()}";}
                    Rebuild();
                }));
            }
            if(page==0)
            {
                foreach(var part in (inventory!=null?inventory.parts??Array.Empty<TechPartDef>():GameCatalog.All<TechPartDef>().Where(p=>p.tier==1)).OrderBy(p=>p.slot))
                {
                    var p=part;
                    Buy(p.displayName,"  ·  "+PartText.Effect(p),PartText.Against(state,active,p),Price(60),()=>
                    {
                        var bag=RpgServices.Inventory;var wearer=bag.EquipIfEmpty(bag.GivePart(p),p);
                        return wearer!=null?$" Equipped on {DisplayNames.Speaker(wearer.id)}.":" Equip it from Gear.";
                    });
                }
                foreach(var consumable in inventory!=null?inventory.consumables??Array.Empty<ConsumableDef>():GameCatalog.All<ConsumableDef>())
                {var c=consumable;Buy(c.displayName,"  ·  "+PartText.Restores(c),"",Price(20),()=>{state.consumables.TryGetValue(c.id,out int n);state.consumables[c.id]=n+1;return "";});}
                foreach(var material in inventory!=null?inventory.materials??Array.Empty<MaterialDef>():GameCatalog.All<MaterialDef>().Where(m=>m.id=="ScrapAlloy"||m.id=="LatticeFilament"))
                {var m=material;Buy(m.displayName+" ×3","","",Price(12),()=>{RpgServices.Inventory.Give(m.id,3);return "";});}
            }
            else foreach(var item in state.parts.ToArray())
            {
                var p=item;var wearer=RpgServices.Inventory.Wearer(p);
                var def=GameCatalog.Find<TechPartDef>(p.definitionId);int value=20+(def!=null?def.tier-1:0)*20+p.upgrade*5;string name=def!=null?def.displayName:p.definitionId;
                string says=def!=null?$"  ·  {PartText.Effect(def,p)}":"";
                offers.Add(($"{PartText.Mark(wearer,active)}{name} +{p.upgrade}{PartText.Affixes(p)}{says}  —  {value} scrip",()=>
                {
                    if(wearer!=null){feedback=$"{name} is on {DisplayNames.Speaker(wearer.id)}. Take it off in Gear before selling it.";UiSounds.Error();}
                    else if((p.upgrade>0||def!=null&&def.tier>1)&&sellAsked!=p.instanceId){sellAsked=p.instanceId;feedback=$"Press again to sell {name} for {value} scrip.";}
                    // The next part slides under the cursor: a repeat press mustn't sell that too.
                    else if(state.parts.Remove(p)){sellAsked=null;state.scrip+=value;feedback=$"Sold {name} for {value} scrip.";first.Begin();}
                    Rebuild();
                }));
            }
            if(offset>=offers.Count)offset=offers.Count>0?(offers.Count-1)/PerPage*PerPage:0;
            foreach(var offer in offers.Skip(offset).Take(PerPage))Add(offer.label,offer.action);
            // Focus goes by role: an index would land the next A on a buy.
            if(offers.Count>PerPage)Add(UiKit.PagerLabel(offset,PerPage,offers.Count),()=>{offset=offset+PerPage<offers.Count?offset+PerPage:0;sellAsked=null;Rebuild("Pager");},"Pager");
            Add("Back",()=>{UiSounds.Cancel();Close();},"ShopBack");
            string said=feedback!=""?feedback:page==1?"Only parts can be sold here. Anything worn comes off in Gear first.":"";
            var note=UiKit.Text(panel.transform,"Feedback",said,25,UiKit.TextColor);UiKit.Rect(note.gameObject,new(.5f,.5f),new(.5f,.5f),new(listAt,-430),new(1180,70));
            UiKit.LinkVertical(buttons.ToArray());
            Selectable target=focus!=null?buttons.FirstOrDefault(b=>b.name==focus):null;
            // The last row went away with a sale: the nearest one above it.
            if(target==null&&focus!=null&&focus.StartsWith("ShopRow")&&int.TryParse(focus.Substring(7),out int at))
                for(int i=at-1;i>=0&&target==null;i--){string above="ShopRow"+i;target=buttons.FirstOrDefault(b=>b.name==above);}
            EventSystem.current.SetSelectedGameObject((target!=null?target:buttons[0]).gameObject);
        }
    }
}
