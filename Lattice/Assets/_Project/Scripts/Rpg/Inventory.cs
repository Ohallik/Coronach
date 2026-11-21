using System;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
namespace Lattice.Rpg
{
    public sealed class Inventory
    {
        readonly GameState state;
        public Inventory(GameState state){this.state=state;}
        public int Count(string id)=>state.materials.TryGetValue(id,out var value)?value:0;
        /// <summary>An item arrived from the world: (id, amount), for the pickup feed.</summary>
        public static event Action<string,int> Gained;
        public static void Announce(string id,int amount){if(amount>0)Gained?.Invoke(id,amount);}
        /// <summary>Gives anything by id, to where its kind lives: parts to gear, gels and cells to the item bag, the rest to
        /// materials. Materials can't use a gel, and a looted gel used to vanish there (D199 fixed this for quest rewards only).</summary>
        /// <param name="rolled">A part from the world rolls its affixes (Loot); one a shop or a plain quest hands over comes clean.</param>
        public void Grant(string id,int amount,bool announce=true,bool rolled=false)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));if(amount==0)return;
            var part=GameCatalog.Find<TechPartDef>(id);var consumable=part==null?GameCatalog.Find<ConsumableDef>(id):null;
            if(part!=null){for(int i=0;i<amount;i++)ReceivePart(part,rolled);}
            else if(consumable!=null)amount=Stock(consumable,amount,announce);
            else Give(id,amount);
            if(announce)Announce(id,amount);
        }
        /// <summary>The most of a consumable a party carries (prog159): gels were made by the dozen from the pause menu and drunk one after another with a boss frozen in front of them.</summary>
        public static int Cap(ConsumableDef def)=>def!=null&&def.stack>0?def.stack:int.MaxValue;
        public int Held(string id)=>state.consumables.TryGetValue(id??"",out int n)?n:0;
        /// <summary>Whether one more would be over the cap: a shop and a bench refuse then.</summary>
        public bool Full(ConsumableDef def)=>def!=null&&Held(def.id)>=Cap(def);
        /// <summary>Puts found consumables in the pouch up to its cap and returns how many went in. What's over is paid in scrip at its value, with a card to say so: loot isn't lost, and the cap holds.</summary>
        int Stock(ConsumableDef def,int amount,bool announce)
        {
            int held=Held(def.id),kept=Math.Min(amount,Math.Max(0,Cap(def)-held)),over=amount-kept;
            if(kept>0)state.consumables[def.id]=held+kept;
            if(over>0)
            {
                int paid=over*Math.Max(0,def.value);state.scrip+=paid;
                // Paid, not pressed: the card waits behind the quest or the kill it came from (Notices.Pay).
                Notices.Pay("POUCH FULL",$"{def.displayName}: {Cap(def)} is the most you can carry. {over} spare sold for {paid} scrip.");
                if(announce)Announce("scrip",paid);
            }
            return kept;
        }
        /// <summary>Gives what a drop or a cache holds and says what it was, for a card ("Prime Edge (Keen)", "Rootfibre ×4"); null when it names nothing that exists.
        /// A part comes rolled. "Any part of tier N" gives nothing while no part of that tier drops.</summary>
        public string GrantLoot(LootEntry loot,bool announce=true)
        {
            int count=Math.Max(1,loot.count);
            var part=string.IsNullOrEmpty(loot.id)?(loot.partTier>0?Loot.PickPart(loot.partTier,loot.partSlot,loot.anySlot):null):GameCatalog.Find<TechPartDef>(loot.id);
            if(part!=null)
            {
                var names=new System.Collections.Generic.List<string>();
                for(int i=0;i<count;i++)names.Add(Gear.Name(part,ReceivePart(part,true)));
                if(announce)Announce(part.id,count);
                return string.Join(", ",names);
            }
            if(string.IsNullOrEmpty(loot.id))return null;
            Grant(loot.id,count,announce);return DisplayNames.Item(loot.id)+(count>1?" ×"+count:"");
        }
        public void Give(string id,int amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            state.materials[id]=Count(id)+amount;
            state.collectedMaterials.TryGetValue(id,out int total);state.collectedMaterials[id]=total+amount;
            // Every source counts toward a gathering quest: a crystal from the Bellows is as bright as one from a ridge.
            if(GameServices.Current!=null&&ReferenceEquals(GameServices.Current.State,state))RpgServices.Quests.Report(ObjectiveKind.Collect,id,amount);
        }
        static System.Collections.Generic.Dictionary<string,int> Totals(Ingredient[] ingredients)=>(ingredients??Array.Empty<Ingredient>()).GroupBy(i=>i.id).ToDictionary(g=>g.Key,g=>g.Sum(i=>i.count));
        /// <summary>Whether everything asked for is held, without spending it.</summary>
        public bool Has(Ingredient[] ingredients)=>!Totals(ingredients).Any(p=>p.Value<0||Count(p.Key)<p.Value);
        public bool Spend(Ingredient[] ingredients)
        {
            var totals=Totals(ingredients);
            if(totals.Any(p=>p.Value<0||Count(p.Key)<p.Value))return false;
            foreach(var p in totals)state.materials[p.Key]=Count(p.Key)-p.Value;return true;
        }
        /// <summary>Sells up to <paramref name="count"/> of a material a shop takes, at its value each, and returns the scrip paid. What a quest counted as gathered stays counted.</summary>
        public int SellMaterial(string id,int count)
        {
            var def=GameCatalog.Find<MaterialDef>(id);int sold=def!=null&&def.value>0?Math.Min(Math.Max(0,count),Count(id)):0;
            if(sold<=0)return 0;
            state.materials[id]=Count(id)-sold;state.scrip+=sold*def.value;return sold*def.value;
        }
        public PartInstance GivePart(TechPartDef definition)
        {var item=new PartInstance{definitionId=definition.id};state.parts.Add(item);return item;}
        /// <summary>A part from the world, for a player who isn't in a menu: it finds a free slot if there is one, and a card says where it went and what it rolled.</summary>
        public PartInstance ReceivePart(TechPartDef definition,bool rolled=false)
        {
            var item=GivePart(definition);if(rolled)Loot.RollAffixes(item,definition);
            var wearer=EquipIfEmpty(item,definition);string name=Gear.Name(definition,item);
            // A part is always given (a drop, a cache, a reward), never the answer to a press: its card waits its turn behind the quest or the kill that gave it.
            Notices.Pay("NEW PART",wearer!=null?$"{name} equipped on {DisplayNames.Speaker(wearer.id)}.":$"{name}. Equip it from Gear.");
            return item;
        }
        /// <summary>Puts a new part on whoever has that slot free, and returns who (null: every hero is covered). A weapon tries the hero
        /// who fights with it first, then the active hero, so a part never waits in a list beside an empty slot.</summary>
        public MemberState EquipIfEmpty(PartInstance item,TechPartDef definition)
        {
            // Who fights with it is read off each hero's definition (HeroWeapon): by name an Edge was Taren's alone, and Idra's hammer is an Edge. Of two who wield it, the lead
            // first, then whoever is out; a party of two orders exactly as it did.
            int active=state.activeMember;
            var wearer=state.party.OrderByDescending(m=>HeroWeapon.Wields(m.id,definition.slot)).ThenByDescending(m=>state.party.IndexOf(m)==active).ThenByDescending(m=>Deployment.Out(state,m.id))
                .FirstOrDefault(m=>!m.equipped.ContainsKey(definition.slot));
            return wearer!=null&&Equip(wearer,item,definition)?wearer:null;
        }
        public MemberState Wearer(PartInstance item)=>state.party.Find(m=>m.equipped.ContainsValue(item.instanceId)||m.module2==item.instanceId);
        /// <summary>Takes a part off whoever wears it; false if nobody did.</summary>
        public bool Unequip(PartInstance item)
        {
            bool worn=false;
            foreach(var m in state.party)
            {
                foreach(var key in m.equipped.Where(p=>p.Value==item.instanceId).Select(p=>p.Key).ToArray()){m.equipped.Remove(key);worn=true;}
                if(m.module2==item.instanceId){m.module2=null;worn=true;}
            }
            return worn;
        }
        public bool Equip(MemberState member,PartInstance item,TechPartDef definition,bool secondModule=false)
        {
            if(!state.party.Contains(member)||!state.parts.Contains(item)||item.definitionId!=definition.id||secondModule&&definition.slot!=GearSlot.Module)return false;
            Unequip(item);
            if(secondModule&&definition.slot==GearSlot.Module)member.module2=item.instanceId;
            else member.equipped[definition.slot]=item.instanceId;
            return true;
        }
        /// <summary>What breaking a part down gives: its own yield, and half the materials its upgrades took.</summary>
        public static Ingredient[] SalvageYield(TechPartDef definition,PartInstance part)
        {
            var all=(definition.salvage??Array.Empty<Ingredient>()).Concat(part!=null?Gear.UpgradeRefund(definition,part):Array.Empty<Ingredient>());
            return all.GroupBy(i=>i.id).Select(g=>new Ingredient{id=g.Key,count=g.Sum(i=>i.count)}).Where(i=>i.count>0).ToArray();
        }
        public bool Salvage(PartInstance part,TechPartDef definition)
        {
            if(Wearer(part)!=null||!state.parts.Remove(part))return false;
            foreach(var material in SalvageYield(definition,part))Give(material.id,material.count);return true;
        }
    }
}
