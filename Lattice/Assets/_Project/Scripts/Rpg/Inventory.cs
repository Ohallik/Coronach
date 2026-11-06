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
        public void Grant(string id,int amount,bool announce=true)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));if(amount==0)return;
            var part=GameCatalog.Find<TechPartDef>(id);
            if(part!=null){for(int i=0;i<amount;i++)ReceivePart(part);}
            else if(GameCatalog.Find<ConsumableDef>(id)!=null){state.consumables.TryGetValue(id,out int held);state.consumables[id]=held+amount;}
            else Give(id,amount);
            if(announce)Announce(id,amount);
        }
        public void Give(string id,int amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            state.materials[id]=Count(id)+amount;
            state.collectedMaterials.TryGetValue(id,out int total);state.collectedMaterials[id]=total+amount;
            // Every source counts toward a gathering quest: a crystal from the Bellows is as bright as one from a ridge.
            if(GameServices.Current!=null&&ReferenceEquals(GameServices.Current.State,state))RpgServices.Quests.Report(ObjectiveKind.Collect,id,amount);
        }
        public bool Spend(Ingredient[] ingredients)
        {
            var totals=(ingredients??Array.Empty<Ingredient>()).GroupBy(i=>i.id).ToDictionary(g=>g.Key,g=>g.Sum(i=>i.count));
            if(totals.Any(p=>p.Value<0||Count(p.Key)<p.Value))return false;
            foreach(var p in totals)state.materials[p.Key]=Count(p.Key)-p.Value;return true;
        }
        public PartInstance GivePart(TechPartDef definition)
        {var item=new PartInstance{definitionId=definition.id};state.parts.Add(item);return item;}
        /// <summary>A part from the world, for a player who isn't in a menu: it finds a free slot if there is one, and a card says where it went.</summary>
        public PartInstance ReceivePart(TechPartDef definition)
        {
            var item=GivePart(definition);var wearer=EquipIfEmpty(item,definition);
            Notices.Post("NEW PART",wearer!=null?$"{definition.displayName} equipped on {DisplayNames.Speaker(wearer.id)}.":$"{definition.displayName}. Equip it from Gear.");
            return item;
        }
        /// <summary>Puts a new part on whoever has that slot free, and returns who (null: every hero is covered). A weapon tries the hero
        /// who fights with it first, then the active hero, so a part never waits in a list beside an empty slot.</summary>
        public MemberState EquipIfEmpty(PartInstance item,TechPartDef definition)
        {
            string user=definition.slot==GearSlot.Edge?"Taren":definition.slot==GearSlot.Emitter?"Sela":null;
            int active=state.activeMember;
            var wearer=state.party.OrderByDescending(m=>m.id==user).ThenByDescending(m=>state.party.IndexOf(m)==active).FirstOrDefault(m=>!m.equipped.ContainsKey(definition.slot));
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
        public bool Salvage(PartInstance part,TechPartDef definition)
        {
            if(Wearer(part)!=null||!state.parts.Remove(part))return false;
            foreach(var material in definition.salvage??Array.Empty<Ingredient>())Give(material.id,material.count);return true;
        }
    }
}
