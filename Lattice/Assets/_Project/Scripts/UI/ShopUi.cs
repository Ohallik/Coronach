using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Lattice.UI
{
    public sealed class ShopUi:MonoBehaviour
    {
        Canvas canvas;GameObject panel;int page,offset;ShopInventory inventory;string feedback="";
        public bool IsOpen=>panel!=null;
        void Start(){canvas=UiKit.CreateCanvas("Shop",35,transform);}
        public void Open(ShopInventory source){inventory=source;page=offset=0;feedback="";GameServices.Current.Input.Blocked=true;MusicDirector.Shop(this,true);Rebuild();}
        public void Close(){if(panel!=null)Destroy(panel);panel=null;GameServices.Current.Input.Blocked=false;MusicDirector.Shop(this,false);}
        void OnDisable(){MusicDirector.Shop(this,false);}
        void Switch(){page=1-page;offset=0;feedback="";Rebuild();}
        void Update()
        {
            if(!IsOpen)return;if(UiActions.Cancel.WasPressedThisFrame()){UiSounds.Cancel();Close();return;}
            if(GameServices.Current.Input.Find("MenuNext").WasPressedThisFrame()||GameServices.Current.Input.Find("MenuPrev").WasPressedThisFrame())Switch();
        }
        void Rebuild()
        {
            string focus=EventSystem.current.currentSelectedGameObject!=null?EventSystem.current.currentSelectedGameObject.name:null;
            if(panel!=null){panel.SetActive(false);Destroy(panel);}panel=UiKit.Dim(canvas.transform,.95f).gameObject;
            var state=GameServices.Current.State;
            var heading=UiKit.Heading(panel.transform,"Heading",(page==0?"OUTFITTER · BUY":"OUTFITTER · SELL")+"   "+state.scrip+" SCRIP",42,UiKit.TextColor);
            UiKit.Rect(heading.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,390),new(1400,80));
            var buttons=new List<Selectable>();int row=0;
            void Add(string label,System.Action action){var b=UiKit.Button(panel.transform,"ShopRow"+row,label,action);UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,260-row++*76),new(1180,62));buttons.Add(b);}
            Add(page==0?"Sell parts  ·  LB / RB":"Buy supplies  ·  LB / RB",Switch);
            var offers=new List<(string label,Action action)>();
            int priceIndex=0;
            int Price(int fallback){int i=priceIndex++;return inventory!=null&&inventory.prices!=null&&i<inventory.prices.Length?Mathf.Max(0,inventory.prices[i]):fallback;}
            void Buy(string name,int cost,Action give)
            {
                offers.Add(($"{name}  —  {cost} scrip",()=>{if(state.scrip<cost){feedback="Not enough scrip.";UiSounds.Error();}else{state.scrip-=cost;give();feedback="Purchased "+name+".";}Rebuild();}));
            }
            if(page==0)
            {
                foreach(var part in (inventory!=null?inventory.parts??Array.Empty<TechPartDef>():GameCatalog.All<TechPartDef>().Where(p=>p.tier==1)).OrderBy(p=>p.slot))
                {var p=part;Buy(p.displayName,Price(60),()=>RpgServices.Inventory.GivePart(p));}
                foreach(var consumable in inventory!=null?inventory.consumables??Array.Empty<ConsumableDef>():GameCatalog.All<ConsumableDef>())
                {var c=consumable;Buy(c.displayName,Price(20),()=>{state.consumables.TryGetValue(c.id,out int n);state.consumables[c.id]=n+1;});}
                foreach(var material in inventory!=null?inventory.materials??Array.Empty<MaterialDef>():GameCatalog.All<MaterialDef>().Where(m=>m.id=="ScrapAlloy"||m.id=="LatticeFilament"))
                {var m=material;Buy(m.displayName+" ×3",Price(12),()=>RpgServices.Inventory.Give(m.id,3));}
            }
            else foreach(var item in state.parts.ToArray())
            {
                var p=item;bool equipped=state.party.Any(m=>m.equipped.Values.Contains(p.instanceId)||m.module2==p.instanceId);
                var def=GameCatalog.Find<TechPartDef>(p.definitionId);int value=20+(def!=null?def.tier-1:0)*20+p.upgrade*5;
                offers.Add(($"{(equipped?"[E] ":"")}{(def!=null?def.displayName:p.definitionId)} +{p.upgrade}  —  {value} scrip",()=>
                {if(equipped){feedback="Unequip this part before selling it.";UiSounds.Error();}else if(state.parts.Remove(p)){state.scrip+=value;feedback="Part sold.";}Rebuild();}));
            }
            if(offset>=offers.Count)offset=0;
            foreach(var offer in offers.Skip(offset).Take(4))Add(offer.label,offer.action);
            if(offers.Count>4)Add($"Next page  ({offset/4+1}/{Mathf.CeilToInt(offers.Count/4f)})",()=>{offset=offset+4<offers.Count?offset+4:0;Rebuild();});
            Add("Back",()=>{UiSounds.Cancel();Close();});
            var note=UiKit.Text(panel.transform,"Feedback",feedback,25,UiKit.TextColor);UiKit.Rect(note.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-360),new(1200,70));
            UiKit.LinkVertical(buttons.ToArray());var remembered=buttons.FirstOrDefault(b=>b.name==focus);EventSystem.current.SetSelectedGameObject((remembered!=null?remembered:buttons[0]).gameObject);
        }
    }
}
