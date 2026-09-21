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
        public void Give(string id,int amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            state.materials[id]=Count(id)+amount;
            state.collectedMaterials.TryGetValue(id,out int total);state.collectedMaterials[id]=total+amount;
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
