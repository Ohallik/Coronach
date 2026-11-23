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
        /// <summary>Offers a page holds. Under them, whether or not anyone stands beside the list, the strip about the row under the cursor has the rest of the list's column: with the
        /// strip over the seller a stall of six left the lower half of the list empty, and nothing at all was said while the cursor was on the page toggle (nov22 s25 053).</summary>
        const int SellerPage=6,PlainPage=6;
        /// <summary>The strip under the list: its top, and the foot it stops short of (the pager and Back stand at -420, 54 tall).</summary>
        const float DetailTop=-166,DetailFoot=-386;
        /// <summary>The panel's size with a seller beside the list and without.</summary>
        static readonly Vector2 SellerPanel=new(1680,980),PlainPanel=new(1290,980);
        /// <summary>The list's column beside a seller: right of her portrait, whose seat ends at -387, to the panel's inner edge.</summary>
        const float SellerListAt=225,SellerListWide=1150,PlainListWide=1180;
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
        /// <summary>An offer or a sale, as its row and the strip say it: a cell for each column, the words it's found by, its kind, and what the strip says while it's under the cursor.</summary>
        sealed class Offer{public string key,name;public string[][] cells;public ItemMarks.Kind kind;public Action press;public string[] about;public TechPartDef part;public PartInstance owned;}
        /// <summary>The buy page's columns and the sell page's, on a row this wide: a kind mark, the name, what it does, the price ending on one edge, and what the party has.</summary>
        static MenuList.Column[] Columns(bool buying,float wide)=>new[]
        {
            new MenuList.Column("Name",MenuList.FirstColumn,344,"ITEM",TextAlignmentOptions.MidlineLeft,26),
            new MenuList.Column("Does",440,344,buying?"DOES":"WHAT IT IS",TextAlignmentOptions.MidlineLeft,24,UiKit.FrostColor),
            new MenuList.Column("Price",790,150,buying?"PRICE":"PAYS",TextAlignmentOptions.MidlineRight,24),
            new MenuList.Column("Have",962,wide-962-26,buying?"YOU HAVE":"ON",TextAlignmentOptions.MidlineLeft,24,UiKit.FrostColor),
        };
        void Rebuild(string want=null)
        {
            var selected=EventSystem.current.currentSelectedGameObject;string focus=want??(selected!=null?selected.name:null);
            if(panel!=null){panel.SetActive(false);Destroy(panel);}panel=UiKit.Dim(canvas.transform,UiKit.MenuDim).gameObject;
            var face=string.IsNullOrEmpty(seller)?null:PortraitLookup.Get(seller,BodyForm.Natural,PortraitEmotion.Happy);bool beside=face!=null;
            // Words stand clear of the frame's inner bevel, which takes 45 units of each side: the name and the purse were drawn against it.
            var size=beside?SellerPanel:PlainPanel;float left=-size.x*.5f+72,right=size.x*.5f-72;
            float listAt=beside?SellerListAt:0,listWide=beside?SellerListWide:PlainListWide,listLeft=listAt-listWide*.5f,listRight=listAt+listWide*.5f;int perPage=beside?SellerPage:PlainPage;
            UiKit.SolidFrame(panel.transform,"ShopPanel",new(.5f,.5f),new(0,-13),size);
            if(beside)
            {
                UiKit.Bust(panel.transform,"SellerPortrait",new(.5f,.5f),new(-806,-472),500).sprite=face;
                var who=UiKit.Heading(panel.transform,"SellerName",DisplayNames.Speaker(seller),32,UiKit.GoldColor);UiKit.Rect(who.gameObject,new(.5f,.5f),new(.5f,.5f),new(-596,14),new(400,50));
            }
            var state=GameServices.Current.State;var active=state.party[state.activeMember];
            // The stall's name heads the page from its left edge, over the seller, and the purse stands at the other end. Centred over the list it sat off to the right, with an empty block above her.
            string trade=inventory!=null&&!string.IsNullOrEmpty(inventory.title)?inventory.title:"OUTFITTER";
            var heading=UiKit.Heading(panel.transform,"Heading",trade+(page==0?" · BUY":" · SELL"),40,UiKit.TextColor,TextAlignmentOptions.MidlineLeft);heading.textWrappingMode=TextWrappingModes.NoWrap;
            // A long name in the panel with nobody beside the list ("THE MENDER'S PRESS · SELL") comes down a little rather than run under the purse.
            heading.enableAutoSizing=true;heading.fontSizeMax=40;heading.fontSizeMin=28;
            UiKit.Rect(heading.gameObject,new(.5f,.5f),new(.5f,.5f),new((left+right-420)*.5f,410),new(right-left-420,60));
            var purse=UiKit.Heading(panel.transform,"Scrip",state.scrip+" SCRIP",40,UiKit.GoldColor,TextAlignmentOptions.MidlineRight);purse.textWrappingMode=TextWrappingModes.NoWrap;
            UiKit.Rect(purse.gameObject,new(.5f,.5f),new(.5f,.5f),new(right-200,410),new(400,60));
            // A seller's own price, said on the stall so a player sees why the prices changed, and what the story has brought to the counter (ShopInventory.note): one line under the name,
            // the panel's whole width. Under the list's width Corra's two broke, and left "· CHOIRWROUGHT" alone on a second line.
            string stallSays=string.Join("  ·  ",new[]{Discounted(inventory,state)?inventory.discountNote:null,Noted(inventory,state)?inventory.note:null}.Where(s=>!string.IsNullOrEmpty(s)));
            if(page==0&&stallSays!="")
            {
                // In the body's face: Cinzel's capitals at the type floor ran Corra's two to 1,850 units, past the panel.
                var priceNote=UiKit.Text(panel.transform,"Discount",stallSays,24,UiKit.GoldColor,TextAlignmentOptions.MidlineLeft);priceNote.characterSpacing=1;priceNote.textWrappingMode=TextWrappingModes.NoWrap;
                UiKit.Rect(priceNote.gameObject,new(.5f,.5f),new(.5f,.5f),new((left+right)*.5f,362),new(right-left,34));
            }
            var buttons=new List<Selectable>();
            bool takesMaterials=inventory==null||inventory.buysMaterials;
            // The page toggle is the list's own tab strip, both pages named and the open one lit, its shoulder buttons at its two ends. It was a row among the goods.
            string sellWords=takesMaterials?"Sell parts and materials":"Sell parts";
            string Lit(string words,bool open)=>open?$"<color=#FFD873>{words}</color>":$"<color=#8CA1BD>{words}</color>";
            var toggle=MenuList.Control(panel.transform,"ShopRow0",$"{PromptService.Tag("MenuPrev")}      {Lit("Buy",page==0)}      ·      {Lit(sellWords,page==1)}      {PromptService.Tag("MenuNext")}",Switch,new(listAt,300),new(660,52));
            buttons.Add(toggle);
            var columns=Columns(page==0,listWide);MenuList.Headings(panel.transform,new(listAt,258),listWide,columns);
            // Each offer with what the row has no room to say: a module's passive, the whole comparison, how many are carried.
            var offers=new List<Offer>();
            void Buy(string name,ItemMarks.Kind kind,string[] does,int cost,string[] have,string[] about,Func<string> refuse,Func<string> give,TechPartDef part=null)
            {
                offers.Add(new Offer{key=name,name=name,kind=kind,about=about,part=part,cells=new[]{MenuList.Say(name),does,MenuList.Say($"{cost} scrip"),have},press=()=>
                {
                    string why=refuse?.Invoke();
                    if(why!=null){feedback=why;UiSounds.Error();}
                    else if(state.scrip<cost){feedback=$"Not enough scrip: {name} costs {cost}, you have {state.scrip}.";UiSounds.Error();}
                    else{state.scrip-=cost;feedback=$"Purchased {name}.{give()}";}
                    Rebuild();
                }});
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
                        // What it would change on the hero who leads, in the stat it leads with: "+22 for Taren". The whole comparison, stat by stat in colour, is the strip's.
                        string versus=PartText.VersusWorn(state,active,part),who=DisplayNames.Speaker(active.id);
                        int owned=state.parts.Count(p=>p.definitionId==part.id);
                        Buy(part.displayName,ItemMarks.Of(part.slot),MenuList.Say(PartText.Effect(part)),cost,MenuList.Say(versus.Contains("slot free")?$"{who}: {versus}":versus!=""?$"{versus} for {who}":"",versus),
                            new[]{PartText.SlotSays(part.slot),PartText.Effects(part),PartText.CompareInk(state,active,part),owned>0?$"You own {owned}.":"You own none."},null,()=>
                        {
                            // A shop's part comes clean: what's found or made can roll affixes, what's bought never does.
                            var bag=RpgServices.Inventory;var wearer=bag.EquipIfEmpty(bag.GivePart(part),part);
                            return wearer!=null?$" Equipped on {DisplayNames.Speaker(wearer.id)}.":" Equip it from Gear.";
                        },part);
                    }
                    else if(consumable!=null)
                    {
                        string restores=PartText.Restores(consumable),carried=PartText.Carried(state,consumable);
                        // A draught's or a meal's words outrun the column: then its kind and its seconds, and the strip says the rest.
                        string lean=consumable.seconds>0?$"{(consumable.meal?"a meal":"a draught")}, {consumable.seconds:0} s":consumable.revive?"raises a partner":restores.Replace("restores ","");
                        Buy(consumable.displayName,ItemMarks.Of(consumable),MenuList.Say(restores,lean,PartText.Brief(consumable)),cost,MenuList.Say(carried),new[]{PartText.About(consumable),$"You carry {carried}.",Needs(state,consumable)},
                            ()=>RpgServices.Inventory.Full(consumable)?$"You can't carry more {consumable.displayName}: {Inventory.Cap(consumable)} is the most.":null,
                            ()=>{state.consumables[consumable.id]=RpgServices.Inventory.Held(consumable.id)+1;return $" You carry {PartText.Carried(state,consumable)}.";});
                    }
                    else if(material!=null)
                    {
                        int lot=o.lot>0?o.lot:3;int held=RpgServices.Inventory.Count(material.id);
                        Buy(material.displayName+" ×"+lot,ItemMarks.Kind.Material,MenuList.Say(lot+" at a press"),cost,MenuList.Say("holding "+held),new[]{PartText.About(material),$"You hold {held} {material.displayName}."},null,()=>{RpgServices.Inventory.Give(material.id,lot);return "";});
                    }
                }
            }
            else
            {
                foreach(var item in state.parts.ToArray())
                {
                    var p=item;var wearer=RpgServices.Inventory.Wearer(p);
                    var def=GameCatalog.Find<TechPartDef>(p.definitionId);int value=20+(def!=null?def.tier-1:0)*20+p.upgrade*5;string name=def!=null?def.displayName:p.definitionId;
                    string affixes=PartText.Affixes(p);string plain=$"{name} +{p.upgrade}";
                    offers.Add(new Offer{key=PartText.Mark(wearer,active)+name,name=name,kind=def!=null?ItemMarks.Of(def.slot):ItemMarks.Kind.None,part=def,owned=p,
                        cells=new[]{MenuList.Say(plain+affixes,plain,name),MenuList.Say(def!=null?PartText.Effect(def,p):""),MenuList.Say($"{value} scrip"),MenuList.Say(wearer!=null?DisplayNames.Speaker(wearer.id):"spare")},
                        about=new[]{p.upgrade>0?$"Upgraded to +{p.upgrade}.":"",def!=null?PartText.Effects(def,p):"",wearer!=null?$"{DisplayNames.Speaker(wearer.id)} has it on: it comes off in Gear first.":""},press=()=>
                    {
                        if(wearer!=null){feedback=$"{name} is on {DisplayNames.Speaker(wearer.id)}. Take it off in Gear before selling it.";UiSounds.Error();}
                        else if((p.upgrade>0||def!=null&&def.tier>1||p.affixes.Count>0)&&sellAsked!=p.instanceId){sellAsked=p.instanceId;feedback=$"Press again to sell {name} for {value} scrip.";}
                        // The next part slides under the cursor: a repeat press mustn't sell that too.
                        else if(state.parts.Remove(p)){sellAsked=null;state.scrip+=value;feedback=$"Sold {name} for {value} scrip.";first.Begin();}
                        Rebuild();
                    }});
                }
                // Spare materials sell too (prog160): five at a time, since they come by the dozen; a boss's or an elite's one at a time, and asked twice.
                if(takesMaterials)foreach(var held in state.materials.Where(m=>m.Value>0).Select(m=>m.Key).ToArray())
                {
                    var material=GameCatalog.Find<MaterialDef>(held);if(material==null||material.value<=0)continue;
                    string id=held,name=material.displayName;int count=RpgServices.Inventory.Count(id),lot=material.rare?1:Mathf.Min(MaterialLot,count),pay=lot*material.value;
                    offers.Add(new Offer{key=$"{name} ×{count}",name=name,kind=ItemMarks.Kind.Material,cells=new[]{MenuList.Say($"{name} ×{count}"),MenuList.Say(material.rare?"rare: one at a press":$"{lot} at a press"),MenuList.Say($"{pay} scrip"),MenuList.Say("held")},
                        about=new[]{PartText.About(material)},press=()=>
                    {
                        if(material.rare&&sellAsked!=id){sellAsked=id;feedback=$"Press again to sell {name} for {pay} scrip. It's rare: a recipe, a last upgrade or a re-tune may want it.";}
                        else{int paid=RpgServices.Inventory.SellMaterial(id,lot);sellAsked=null;feedback=$"Sold {lot} {name} for {paid} scrip.";if(RpgServices.Inventory.Count(id)<=0)first.Begin();}
                        Rebuild();
                    }});
                }
            }
            if(offset>=offers.Count)offset=offers.Count>0?(offers.Count-1)/perPage*perPage:0;
            // The strip about the row under the cursor, under the list in the list's own column: a rule, the thing's mark and name, then what it does in a sentence, what's carried
            // and what it means to the party. Beside a seller it stood over her and left a short stall's lower half empty.
            var rule=UiKit.Divider(panel.transform,"DetailRule");UiKit.Rect(rule.gameObject,new(.5f,.5f),new(.5f,.5f),new(listAt,DetailTop),new(listWide,4));
            var markHolder=new GameObject("DetailMark",typeof(RectTransform)).GetComponent<RectTransform>();markHolder.SetParent(panel.transform,false);
            UiKit.Rect(markHolder.gameObject,new(.5f,.5f),new(.5f,.5f),new(listLeft+MenuList.MarkAt,DetailTop-34),new(48,48));
            var named=UiKit.Text(panel.transform,"DetailName","",28,UiKit.GoldColor,TextAlignmentOptions.MidlineLeft);named.textWrappingMode=TextWrappingModes.NoWrap;named.overflowMode=TextOverflowModes.Ellipsis;
            UiKit.Rect(named.gameObject,new(.5f,.5f),new(.5f,.5f),new(listAt+MenuList.FirstColumn*.5f,DetailTop-34),new(listWide-MenuList.FirstColumn,44));
            var note=UiKit.Text(panel.transform,"Feedback","",24,UiKit.TextColor,TextAlignmentOptions.TopLeft);
            float noteTop=DetailTop-62;UiKit.Rect(note.gameObject,new(.5f,.5f),new(.5f,.5f),new(listAt+MenuList.FirstColumn*.5f,(noteTop+DetailFoot)*.5f),new(listWide-MenuList.FirstColumn,noteTop-DetailFoot));
            // Beside a seller, over her: the party as the thing under the cursor finds them, each hero's Integrity and Charge, or for a part what it would change on each.
            var heroes=beside?PartyLines(panel.transform):null;
            string toggleSays=page==0?$"{PromptService.SubmitTag()} here turns to {sellWords}. Down the list, {PromptService.SubmitTag()} buys the row that's lit.":
                $"{PromptService.SubmitTag()} here turns back to Buy. Down the list, {PromptService.SubmitTag()} sells the row that's lit.";
            void Speak(Offer o)
            {
                // A long name ("Choirwrought Ember Emitter") comes down to the type floor rather than lose its end.
                named.text=o!=null?o.name:page==0?"Buying":"Selling";named.fontSize=28;
                foreach(Transform old in markHolder)Destroy(old.gameObject);
                if(o!=null&&o.kind!=ItemMarks.Kind.None)MenuList.Mark(markHolder,o.kind,Vector2.zero,48,false);
                if(heroes!=null)Party(heroes,state,o);
                if(o!=null)PartText.Fit(note,o.about);
                else PartText.Fit(note,page==1?(takesMaterials?"Parts and spare materials can be sold here.":"Only parts can be sold here.")+" Anything worn comes off in Gear first.":offers.Count==0?"Nothing on the counter today.":"",toggleSays);
            }
            var shown=new Dictionary<string,Offer>();int row=1;
            foreach(var offer in offers.Skip(offset).Take(perPage))
            {
                var o=offer;string rowName="ShopRow"+row;shown[rowName]=o;
                var b=MenuList.Row(panel.transform,rowName,new(listAt,205-(row-1)*66),new(listWide,56),columns,o.cells,o.press,o.key,o.kind);buttons.Add(b);row++;
                // Moving onto a row lets it speak; the answer to the last press stays until then.
                b.gameObject.AddComponent<SelectNote>().selected=()=>{if(settling)return;feedback="";Speak(o);};
            }
            // The panel's own controls, at its foot: the pager and Back, and what the pad's buttons do here. Focus goes by role: an index would land the next A on a buy.
            float foot=-420;
            if(offers.Count>perPage)buttons.Add(MenuList.Control(panel.transform,"Pager",UiKit.PagerLabel(offset,perPage,offers.Count),()=>{offset=offset+perPage<offers.Count?offset+perPage:0;sellAsked=null;Rebuild("Pager");},new(listRight-420,foot),new(330,54)));
            buttons.Add(MenuList.Control(panel.transform,"ShopBack","Back",()=>{UiSounds.Cancel();Close();},new(listRight-125,foot),new(230,54),26));
            var hints=UiKit.Text(panel.transform,"Footer",MenuList.Hints((DeviceHints.Pages(),page==0?"SELL":"BUY"),(PromptService.SubmitTag(),page==0?"BUY":"SELL"),(PromptService.CancelTagFor(PromptService.Device),"LEAVE")),24,UiKit.DimTextColor,TextAlignmentOptions.MidlineLeft);
            hints.textWrappingMode=TextWrappingModes.NoWrap;UiKit.Rect(hints.gameObject,new(.5f,.5f),new(.5f,.5f),new(listLeft+280,foot),new(560,54));
            // Back on the toggle, the strip says what a press does there and what the lit row below means.
            toggle.gameObject.AddComponent<SelectNote>().selected=()=>{if(settling)return;feedback="";Speak(null);};
            UiKit.LinkVertical(buttons.ToArray());
            Selectable target=focus!=null?buttons.FirstOrDefault(b=>b.name==focus):null;
            // The last row went away with a sale: the nearest one above it.
            if(target==null&&focus!=null&&focus.StartsWith("ShopRow")&&int.TryParse(focus.Substring(7),out int at))
                for(int i=at-1;i>=0&&target==null;i--){string above="ShopRow"+i;target=buttons.FirstOrDefault(b=>b.name==above);}
            var landing=(target!=null?target:buttons[0]).gameObject;
            settling=true;EventSystem.current.SetSelectedGameObject(landing);settling=false;
            // With nothing just said, what the cursor lands on says its piece at once, the page toggle included; with something said, the strip's head and the party still
            // follow the cursor and the answer to the press stands under them.
            shown.TryGetValue(landing.name,out var under);
            Speak(under);if(feedback!="")note.text=feedback;
        }
        /// <summary>The party over the seller: a heading, then each hero's name and a line under it that the row under the cursor fills (Party).</summary>
        static TMP_Text[] PartyLines(Transform panel)
        {
            var state=GameServices.Current.State;var members=state.party.Take(3).ToArray();var lines=new TMP_Text[members.Length];
            var head=UiKit.Text(panel,"PartyHead","THE PARTY",24,UiKit.DimTextColor,TextAlignmentOptions.MidlineLeft);head.characterSpacing=4;
            UiKit.Rect(head.gameObject,new(.5f,.5f),new(.5f,.5f),new(PartyAt,318),new(PartyWide,34));
            for(int i=0;i<members.Length;i++)
            {
                var name=UiKit.Text(panel,"PartyName"+i,DisplayNames.Speaker(members[i].id),26,i==state.activeMember?UiKit.GoldColor:UiKit.TextColor,TextAlignmentOptions.MidlineLeft);
                name.textWrappingMode=TextWrappingModes.NoWrap;UiKit.Rect(name.gameObject,new(.5f,.5f),new(.5f,.5f),new(PartyAt,276-i*86),new(PartyWide,34));
                lines[i]=UiKit.Text(panel,"PartyState"+i,"",24,UiKit.FrostColor,TextAlignmentOptions.MidlineLeft);lines[i].textWrappingMode=TextWrappingModes.NoWrap;lines[i].overflowMode=TextOverflowModes.Ellipsis;
                UiKit.Rect(lines[i].gameObject,new(.5f,.5f),new(.5f,.5f),new(PartyAt,244-i*86),new(PartyWide,32));
            }
            return lines;
        }
        /// <summary>The party's column over the seller: from inside the frame's bevel to short of the list.</summary>
        const float PartyAt=-588,PartyWide=400;
        /// <summary>Each hero as the thing under the cursor finds them: for a part, what it would change on them in its leading stat; for anything else, their Integrity and Charge.</summary>
        static void Party(TMP_Text[] lines,GameState state,Offer o)
        {
            var bodies=Bodies(state);
            for(int i=0;i<lines.Length&&i<bodies.Length;i++)
            {
                var (member,integrity,most,charge,down)=bodies[i];
                lines[i].text=o!=null&&o.part!=null?PartText.VersusLine(state,member,o.part,o.owned):
                    down?MenuList.Ink("Down",MenuList.Worse)+"  ·  Charge "+charge.ToString("0"):$"Integrity {integrity:0}/{most:0}  ·  Charge {charge:0}";
            }
        }
        /// <summary>The party as a fight would find them: each hero's Integrity and Charge off their body where there is one, or off the save.</summary>
        static (MemberState member,float integrity,float most,float charge,bool down)[] Bodies(GameState state)
        {
            var party=Lattice.Combat.PartyController.Current;
            return state.party.Take(3).Select(m=>
            {
                var body=party!=null?party.members.FirstOrDefault(a=>a!=null&&a.character==m.id):null;
                return body!=null&&body.Health!=null?(m,body.Health.integrity,body.Health.maximum,body.charge,!body.Health.Alive):(m,m.integrity,Levels.MaxIntegrity(m.level),m.charge,m.integrity<=0);
            }).ToArray();
        }
        /// <summary>What the party's state makes of a consumable, in a sentence: who it would help now.</summary>
        static string Needs(GameState state,ConsumableDef item)
        {
            var bodies=Bodies(state);if(item==null||bodies.Length==0)return "";
            string Named(MemberState m)=>DisplayNames.Speaker(m.id);
            if(item.revive){var down=bodies.Where(b=>b.down).Select(b=>Named(b.member)).ToArray();return down.Length==0?"Nobody is down.":$"{string.Join(" and ",down)} {(down.Length>1?"are":"is")} down: this raises a partner.";}
            if(item.seconds>0)return "";
            bool heals=item.heal>0||item.healPercent>0;
            var said=new List<string>();
            foreach(var (member,integrity,most,charge,down) in bodies)
            {
                if(down){said.Add($"{Named(member)} is down");continue;}
                if(heals)said.Add(integrity>=most-.5f?$"{Named(member)} is at full Integrity":$"{Named(member)} is at {integrity:0}/{most:0}");
                else if(item.charge>0)said.Add(charge>=99.5f?$"{Named(member)} has full Charge":$"{Named(member)} has {charge:0} Charge");
            }
            return said.Count==0?"":string.Join(", ",said)+".";
        }
        /// <summary>The cursor is being put where it belongs after a redraw, not moved by the player: the answer to the last press isn't to be talked over.</summary>
        bool settling;
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
        /// <summary>Whether the stall's story line stands now (ShopInventory.noteFlag set).</summary>
        public static bool Noted(ShopInventory inventory,GameState state)=>inventory!=null&&!string.IsNullOrEmpty(inventory.noteFlag)&&!string.IsNullOrEmpty(inventory.note)&&state.flags.TryGetValue(inventory.noteFlag,out bool set)&&set;
        /// <summary>Whether this seller's own price holds now (ShopInventory.discountFlag set).</summary>
        public static bool Discounted(ShopInventory inventory,GameState state)=>inventory!=null&&!string.IsNullOrEmpty(inventory.discountFlag)&&state.flags.TryGetValue(inventory.discountFlag,out bool set)&&set;
        /// <summary>What an offer costs to buy here now: its price, times the seller's discount while it holds.</summary>
        public static int Price(ShopInventory inventory,ShopOffer offer,GameState state)=>Mathf.Max(0,Discounted(inventory,state)?Mathf.RoundToInt(offer.price*inventory.discount):offer.price);
        /// <summary>The stall a conversation opens: the seller's own in this zone ("Rootmarket_Binna") where a zone has more than one, or the zone's.
        /// A talk that opens a shop where nobody has stocked one gets an empty counter, not the outfitter's: a new town's missing stall shows as missing.</summary>
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
