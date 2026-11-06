using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>What a part or an item does, in the words Gear, the bench and the shop all use. The numbers are what the hero
    /// actually gets: tier, upgrades and affixes counted the way Gear.Calculate counts them.</summary>
    public static class PartText
    {
        static IEnumerable<(string name,float value)> Lines(Stats stats)
        {
            yield return ("Output",stats.output);yield return ("Plating",stats.plating);yield return ("Response",stats.response);yield return ("Resonance",stats.resonance);yield return ("Fortune",stats.fortune);
        }
        static bool Counts((string name,float value) line)=>Mathf.Abs(line.value)>=.5f;
        static Stats Total(TechPartDef def,PartInstance item)
        {
            if(item==null)return def.baseStats;
            var total=Levels.PartStats(def,item);
            foreach(string name in item.affixes.Take(2)){var affix=GameCatalog.Find<AffixDef>(name);if(affix!=null)total+=affix.bonus;}
            return total;
        }
        public static string Says(Stats stats)=>string.Join(", ",Lines(stats).Where(Counts).Select(l=>$"{l.name} +{l.value:0}"));
        public static string Effect(TechPartDef def,PartInstance item=null)=>Says(Total(def,item));
        /// <summary>"  ·  now +8": what the hero has in that slot, in the stat this part leads with, or that the slot is free.</summary>
        public static string Against(GameState state,MemberState member,TechPartDef def,PartInstance item=null)
        {
            var lead=Lines(Total(def,item)).FirstOrDefault(Counts);if(lead.name==null)return "";
            if(!member.equipped.TryGetValue(def.slot,out string id))return "  ·  slot empty";
            var worn=state.parts.Find(p=>p.instanceId==id);var wornDef=worn!=null?GameCatalog.Find<TechPartDef>(worn.definitionId):null;
            if(wornDef==null)return "  ·  slot empty";
            if(worn==item)return "";
            return $"  ·  now +{Lines(Total(wornDef,worn)).First(l=>l.name==lead.name).value:0}";
        }
        /// <summary>Who has it on, in words: the bracketed E read as a key prompt, and a partner's parts looked free.</summary>
        public static string Mark(MemberState wearer,MemberState viewer)=>wearer==null?"":wearer==viewer?"Equipped · ":DisplayNames.Speaker(wearer.id)+" · ";
        public static string Affixes(PartInstance item)=>string.Concat(item.affixes.Take(2).Select(name=>"  ·  "+(GameCatalog.Find<AffixDef>(name)?.displayName??name)));
        public static string Yield(TechPartDef def)=>string.Join(", ",(def.salvage??System.Array.Empty<Ingredient>()).Select(i=>$"{i.count} {DisplayNames.Item(i.id)}"));
        public static string Restores(ConsumableDef def)
        {
            if(def==null)return "";
            if(def.heal>0&&def.charge>0)return $"restores {def.heal:0} Integrity and {def.charge:0} Charge";
            return def.heal>0?$"restores {def.heal:0} Integrity":$"restores {def.charge:0} Charge";
        }
        /// <summary>A hero's slots at a glance, the empty ones named.</summary>
        public static string Wearing(GameState state,MemberState member)
        {
            TechPartDef Def(string id){var item=state.parts.Find(p=>p.instanceId==id);return item!=null?GameCatalog.Find<TechPartDef>(item.definitionId):null;}
            var worn=new List<string>();var empty=new List<string>();
            foreach(GearSlot slot in System.Enum.GetValues(typeof(GearSlot)))
            {
                var def=member.equipped.TryGetValue(slot,out string id)?Def(id):null;
                if(def!=null)worn.Add(def.displayName);else empty.Add(slot.ToString());
            }
            if(!string.IsNullOrEmpty(member.module2)){var second=Def(member.module2);if(second!=null)worn.Add(second.displayName);}
            string who=DisplayNames.Speaker(member.id);
            return (worn.Count>0?$"{who} wears {string.Join(", ",worn)}.":$"{who} wears no parts yet.")+(empty.Count>0?$" Empty: {string.Join(", ",empty)}.":"");
        }
    }
}
