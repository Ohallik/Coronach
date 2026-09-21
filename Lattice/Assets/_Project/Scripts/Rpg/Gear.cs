using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
namespace Lattice.Rpg
{
    public static class Gear
    {
        public static Stats Calculate(GameState state,MemberState member,CharacterDef character,GearSlot weapon,
            Func<string,TechPartDef> parts,Func<string,AffixDef> affixes)
        {
            var total=Levels.Calculate(character,member.level);
            var ids=new HashSet<string>(member.equipped.Values);
            if(!string.IsNullOrEmpty(member.module2))ids.Add(member.module2);
            foreach(string id in ids)
            {
                var item=state.parts.Find(p=>p.instanceId==id);if(item==null)continue;
                var def=parts(item.definitionId);if(def==null)continue;
                // Only the weapon being used contributes; armour and modules affect both attacks.
                if((def.slot==GearSlot.Edge||def.slot==GearSlot.Emitter)&&def.slot!=weapon)continue;
                total+=Levels.PartStats(def,item);
                foreach(string name in item.affixes.Take(2)){var affix=affixes(name);if(affix!=null)total+=affix.bonus;}
            }
            return total;
        }
        public static DamageType WeaponType(GameState state,MemberState member,GearSlot slot,DamageType fallback)
        {
            if(!member.equipped.TryGetValue(slot,out string id))return fallback;
            var item=state.parts.Find(p=>p.instanceId==id);var def=item!=null?GameCatalog.Find<TechPartDef>(item.definitionId):null;
            return def!=null?def.damageType:fallback;
        }
        public static bool Upgrade(GameState state,PartInstance part)
        {
            if(!state.parts.Contains(part)||part.upgrade>=5)return false;
            var cost=new[]{new Ingredient{id="ScrapAlloy",count=3*(part.upgrade+1)}};
            if(!new Inventory(state).Spend(cost))return false;part.upgrade++;return true;
        }
    }
}
