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
            // No inventory named is the outfitter's, as it always was: Mira's own stock now (Outfitter), where it used to be every tier 1 part and every consumable in the catalogue.
            inventory=source!=null?source:Outfitter;seller=from;page=offset=0;feedback="";sellAsked=null;GameServices.Current.Input.Blocked=true;MusicDirector.Shop(this,true);
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
            // A seller's own price, said on the stall so a player sees why the prices changed.
            // And what the story has brought to the counter (ShopInventory.note), on the same line: the two together at Corra's once the convoy is in.
            string stallSays=string.Join("  ·  ",new[]{Discounted(inventory,state)?inventory.discountNote:null,Noted(inventory,state)?inventory.note:null}.Where(s=>!string.IsNullOrEmpty(s)));
            if(page==0&&stallSays!=""){var priceNote=UiKit.Heading(panel.transform,"Discount",stallSays,26,UiKit.GoldColor);UiKit.Rect(priceNote.gameObject,new(.5f,.5f),new(.5f,.5f),new(listAt,334),new(1180,40));}
            var buttons=new List<Selectable>();int row=0;
            void Add(string label,System.Action action,string name=null)
            {
                var b=UiKit.Button(panel.transform,name??"ShopRow"+row,label,action);b.GetComponentInChildren<TMP_Text>().fontSize=30;
                UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),new(listAt,260-row++*76),new(1180,62));buttons.Add(b);
            }
            bool takesMaterials=inventory==null||inventory.buysMaterials;
            Add((page==0?takesMaterials?"Sell parts and materials":"Sell parts":"Buy supplies")+"  ·  "+DeviceHints.Pages(),Switch);
            // Each offer with what the row under the cursor has no room to say: a module's passive, the whole comparison, how many are carried.
            // A row's leaner wording, for when its full one outruns it: the price is never what gives way.
            var offers=new List<(string label,Action action,string detail,string lean)>();
            void Buy(string name,string says,string against,int cost,string detail,Func<string> refuse,Func<string> give)
            {
                offers.Add(($"{name}{says}  —  {cost} scrip{against}",()=>
                {
                    string why=refuse?.Invoke();
                    if(why!=null){feedback=why;UiSounds.Error();}
                    else if(state.scrip<cost){feedback=$"Not enough scrip: {name} costs {cost}, you have {state.scrip}.";UiSounds.Error();}
                    else{state.scrip-=cost;feedback=$"Purchased {name}.{give()}";}
                    Rebuild();
                },detail,$"{name}  —  {cost} scrip{against}"));
            }
            if(page==0)
            {
                // Every offer has its own price, in the order the stall keeps them (ShopInventory.offers). The prices were one list read by position after the parts were sorted by slot,
                // and a zone with no inventory sold the whole catalogue of consumables at 20: a stall with nothing authored now has nothing on the counter.
                foreach(var offer in Stock(inventory,state))
                {
                    var o=offer;int cost=Price(inventory,o,state);
                    var part=GameCatalog.Find<TechPartDef>(o.id);var consumable=part==null?GameCatalog.Find<ConsumableDef>(o.id):null;var material=part==null&&consumable==null?GameCatalog.Find<MaterialDef>(o.id):null;
                    if(part!=null)
                    {
                        string does=PartText.Effects(part),against=PartText.Compare(state,active,part);
                        Buy(part.displayName,"  ·  "+PartText.Effect(part),PartText.Against(state,active,part),cost,does!=""?does+"\n"+against:against,null,()=>
                        {
                            // A shop's part comes clean: what's found or made can roll affixes, what's bought never does.
                            var bag=RpgServices.Inventory;var wearer=bag.EquipIfEmpty(bag.GivePart(part),part);
                            return wearer!=null?$" Equipped on {DisplayNames.Speaker(wearer.id)}.":" Equip it from Gear.";
                        });
                    }
                    else if(consumable!=null)
                    {
                        string restores=PartText.Restores(consumable),carried=PartText.Carried(state,consumable);
                        // A draught's or a meal's words are the note's: with them on the row a price ran off its end.
                        Buy(consumable.displayName,restores.Length<=RowWords?"  ·  "+restores:"","  ·  "+carried,cost,$"{consumable.displayName} {restores}.\nYou carry {carried}.",
                            ()=>RpgServices.Inventory.Full(consumable)?$"You can't carry more {consumable.displayName}: {Inventory.Cap(consumable)} is the most.":null,
                            ()=>{state.consumables[consumable.id]=RpgServices.Inventory.Held(consumable.id)+1;return $" You carry {PartText.Carried(state,consumable)}.";});
                    }
                    else if(material!=null)
                    {
                        int lot=o.lot>0?o.lot:3;
                        Buy(material.displayName+" ×"+lot,"","",cost,$"You hold {RpgServices.Inventory.Count(material.id)} {material.displayName}.",null,()=>{RpgServices.Inventory.Give(material.id,lot);return "";});
                    }
                }
            }
            else
            {
                foreach(var item in state.parts.ToArray())
                {
                    var p=item;var wearer=RpgServices.Inventory.Wearer(p);
                    var def=GameCatalog.Find<TechPartDef>(p.definitionId);int value=20+(def!=null?def.tier-1:0)*20+p.upgrade*5;string name=def!=null?def.displayName:p.definitionId;
                    string says=def!=null?$"  ·  {PartText.Effect(def,p)}":"";
                    offers.Add(($"{PartText.Mark(wearer,active)}{name} +{p.upgrade}{PartText.Affixes(p)}{says}  —  {value} scrip",()=>
                    {
                        if(wearer!=null){feedback=$"{name} is on {DisplayNames.Speaker(wearer.id)}. Take it off in Gear before selling it.";UiSounds.Error();}
                        else if((p.upgrade>0||def!=null&&def.tier>1||p.affixes.Count>0)&&sellAsked!=p.instanceId){sellAsked=p.instanceId;feedback=$"Press again to sell {name} for {value} scrip.";}
                        // The next part slides under the cursor: a repeat press mustn't sell that too.
                        else if(state.parts.Remove(p)){sellAsked=null;state.scrip+=value;feedback=$"Sold {name} for {value} scrip.";first.Begin();}
                        Rebuild();
                    },def!=null?PartText.Effects(def,p):"",$"{PartText.Mark(wearer,active)}{name} +{p.upgrade}{says}  —  {value} scrip"));
                }
                // Spare materials sell too (prog160): five at a time, since they come by the dozen; a boss's or an elite's one at a time, and asked twice.
                if(takesMaterials)foreach(var held in state.materials.Where(m=>m.Value>0).Select(m=>m.Key).ToArray())
                {
                    var material=GameCatalog.Find<MaterialDef>(held);if(material==null||material.value<=0)continue;
                    string id=held,name=material.displayName;int count=RpgServices.Inventory.Count(id),lot=material.rare?1:Mathf.Min(MaterialLot,count),pay=lot*material.value;
                    offers.Add(($"{name} ×{count}  —  sell {lot} for {pay} scrip",()=>
                    {
                        if(material.rare&&sellAsked!=id){sellAsked=id;feedback=$"Press again to sell {name} for {pay} scrip. It's rare: a recipe, a last upgrade or a re-tune may want it.";}
                        else{int paid=RpgServices.Inventory.SellMaterial(id,lot);sellAsked=null;feedback=$"Sold {lot} {name} for {paid} scrip.";if(RpgServices.Inventory.Count(id)<=0)first.Begin();}
                        Rebuild();
                    },$"{material.value} scrip each."+(material.rare?" Rare.":""),null));
                }
            }
            if(offset>=offers.Count)offset=offers.Count>0?(offers.Count-1)/PerPage*PerPage:0;
            var details=new Dictionary<string,string>();TMP_Text note=null;
            foreach(var offer in offers.Skip(offset).Take(PerPage))
            {
                string detail=offer.detail,rowName="ShopRow"+row;details[rowName]=detail;Add(offer.label,offer.action);
                if(offer.lean!=null)PartText.Narrow((Button)buttons[buttons.Count-1],offer.label,offer.lean);
                // Moving onto a row lets it speak; the answer to the last press stays until then.
                buttons[buttons.Count-1].gameObject.AddComponent<SelectNote>().selected=()=>{if(settling||note==null)return;feedback="";PartText.Fit(note,(detail??"").Split('\n'));};
            }
            // Focus goes by role: an index would land the next A on a buy.
            if(offers.Count>PerPage)Add(UiKit.PagerLabel(offset,PerPage,offers.Count),()=>{offset=offset+PerPage<offers.Count?offset+PerPage:0;sellAsked=null;Rebuild("Pager");},"Pager");
            Add("Back",()=>{UiSounds.Cancel();Close();},"ShopBack");
            string said=feedback!=""?feedback:page==1?(takesMaterials?"Parts and spare materials can be sold here.":"Only parts can be sold here.")+" Anything worn comes off in Gear first.":offers.Count==0?"Nothing on the counter today.":"";
            note=UiKit.Text(panel.transform,"Feedback",said,25,UiKit.TextColor);UiKit.Rect(note.gameObject,new(.5f,.5f),new(.5f,.5f),new(listAt,-430),new(1180,70));
            UiKit.LinkVertical(buttons.ToArray());
            Selectable target=focus!=null?buttons.FirstOrDefault(b=>b.name==focus):null;
            // The last row went away with a sale: the nearest one above it.
            if(target==null&&focus!=null&&focus.StartsWith("ShopRow")&&int.TryParse(focus.Substring(7),out int at))
                for(int i=at-1;i>=0&&target==null;i--){string above="ShopRow"+i;target=buttons.FirstOrDefault(b=>b.name==above);}
            var landing=(target!=null?target:buttons[0]).gameObject;
            settling=true;EventSystem.current.SetSelectedGameObject(landing);settling=false;
            // With nothing just said, the row the cursor lands on says its piece at once.
            if(feedback==""&&page==0&&details.TryGetValue(landing.name,out string first0)&&!string.IsNullOrEmpty(first0))PartText.Fit(note,first0.Split('\n'));
        }
        /// <summary>The cursor is being put where it belongs after a redraw, not moved by the player: the answer to the last press isn't to be talked over.</summary>
        bool settling;
        /// <summary>How long a consumable's effect can be and still share its row with a price.</summary>
        const int RowWords=34;
        /// <summary>How many of a common material one press sells.</summary>
        public const int MaterialLot=5;
        /// <summary>What a stall has on its counter now: its offers, less any still waiting on a flag (a cook's dishes, until her baskets are full).</summary>
        public static IEnumerable<ShopOffer> Stock(ShopInventory inventory,GameState state)
        {
            if(inventory==null||inventory.offers==null)yield break;
            foreach(var offer in inventory.offers)
                if(!string.IsNullOrEmpty(offer.id)&&(string.IsNullOrEmpty(offer.requires)||state.flags.TryGetValue(offer.requires,out bool met)&&met))yield return offer;
        }
        /// <summary>The parts a stall sells, each with its price, whatever flag an offer waits on: what a check or a ledger asks of a stall.</summary>
        public static IEnumerable<(TechPartDef part,int price)> Parts(ShopInventory inventory)
        {
            if(inventory==null||inventory.offers==null)yield break;
            foreach(var offer in inventory.offers){var part=string.IsNullOrEmpty(offer.id)?null:GameCatalog.Find<TechPartDef>(offer.id);if(part!=null)yield return (part,offer.price);}
        }
        /// <summary>The stall a conversation opens: the seller's own in this zone ("Rootmarket_Binna") where a zone has more than one, or the zone's.
        /// A talk that opens a shop where nobody has stocked one gets an empty counter, not the outfitter's: a new town's missing stall shows as missing.</summary>
        /// <summary>Whether this seller's own price holds now (ShopInventory.discountFlag set).</summary>
        /// <summary>Whether the stall's story line stands now (ShopInventory.noteFlag set).</summary>
        public static bool Noted(ShopInventory inventory,GameState state)=>inventory!=null&&!string.IsNullOrEmpty(inventory.noteFlag)&&!string.IsNullOrEmpty(inventory.note)&&state.flags.TryGetValue(inventory.noteFlag,out bool set)&&set;
        public static bool Discounted(ShopInventory inventory,GameState state)=>inventory!=null&&!string.IsNullOrEmpty(inventory.discountFlag)&&state.flags.TryGetValue(inventory.discountFlag,out bool set)&&set;
        /// <summary>What an offer costs to buy here now: its price, times the seller's discount while it holds.</summary>
        public static int Price(ShopInventory inventory,ShopOffer offer,GameState state)=>Mathf.Max(0,Discounted(inventory,state)?Mathf.RoundToInt(offer.price*inventory.discount):offer.price);
        public static ShopInventory For(string zone,string seller)
        {
            var own=string.IsNullOrEmpty(zone)||string.IsNullOrEmpty(seller)?null:GameCatalog.Find<ShopInventory>(zone+"_"+seller);
            if(own==null&&!string.IsNullOrEmpty(zone))own=GameCatalog.Find<ShopInventory>(zone);
            return own!=null?own:Unstocked;
        }
        /// <summary>The outfitter's stock, Mira's on the Decks: what a shop opened with no inventory sells.</summary>
        public static ShopInventory Outfitter=>GameCatalog.Find<ShopInventory>("Hub_Decks");
        static ShopInventory unstocked;
        /// <summary>A stall with nothing on its counter, which still buys.</summary>
        public static ShopInventory Unstocked
        {
            get
            {
                if(unstocked==null){unstocked=ScriptableObject.CreateInstance<ShopInventory>();unstocked.name="Unstocked";unstocked.title="";unstocked.offers=Array.Empty<ShopOffer>();unstocked.hideFlags=HideFlags.HideAndDontSave;}
                return unstocked;
            }
        }
    }
}
