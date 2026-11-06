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
            if(part!=null){for(int i=0;i<amount;i++)GivePart(part);}
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
        public bool Equip(MemberState member,PartInstance item,TechPartDef definition,bool secondModule=false)
        {
            if(!state.party.Contains(member)||!state.parts.Contains(item)||item.definitionId!=definition.id||secondModule&&definition.slot!=GearSlot.Module)return false;
            foreach(var m in state.party)
            {
                foreach(var key in m.equipped.Where(p=>p.Value==item.instanceId).Select(p=>p.Key).ToArray())m.equipped.Remove(key);
                if(m.module2==item.instanceId)m.module2=null;
            }
            if(secondModule&&definition.slot==GearSlot.Module)member.module2=item.instanceId;
            else member.equipped[definition.slot]=item.instanceId;
            return true;
        }
        public bool Salvage(PartInstance part,TechPartDef definition)
        {
            if(state.party.Any(m=>m.equipped.Values.Contains(part.instanceId)||m.module2==part.instanceId)||!state.parts.Remove(part))return false;
            foreach(var material in definition.salvage??Array.Empty<Ingredient>())Give(material.id,material.count);return true;
        }
    }
}
