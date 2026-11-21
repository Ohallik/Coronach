using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Rpg
{
    public static class Gear
    {
        public const int MaxUpgrade=5;
        /// <summary>What an affix adds on this part. Its bonus is written for a tier 1 part and grows with the tier it's on: Resonant is +2 on Scrap, +4 on Refined, +10 on Choirwrought.</summary>
        public static Stats AffixStats(AffixDef affix,TechPartDef part)=>affix.bonus*Mathf.Max(1,part.tier);
        /// <summary>What one part gives its wearer: its base stats at its upgrade level and its affixes at its tier. What the rows, the comparison and the fight all go by.</summary>
        public static Stats PartTotal(TechPartDef def,PartInstance item,Func<string,AffixDef> affixes)
        {
            var total=Levels.PartStats(def,item);
            foreach(string name in item.affixes.Take(2)){var affix=affixes(name);if(affix!=null)total+=AffixStats(affix,def);}
            return total;
        }
        /// <summary>The parts a hero has on, with their definitions; a part whose definition is gone counts for nothing.</summary>
        static IEnumerable<(PartInstance item,TechPartDef def)> Worn(GameState state,MemberState member,Func<string,TechPartDef> parts)
        {
            var ids=new HashSet<string>(member.equipped.Values);
            if(!string.IsNullOrEmpty(member.module2))ids.Add(member.module2);
            foreach(string id in ids)
            {
                var item=state.parts.Find(p=>p.instanceId==id);if(item==null)continue;
                var def=parts(item.definitionId);if(def==null)continue;
                yield return (item,def);
            }
        }
        public static Stats Calculate(GameState state,MemberState member,CharacterDef character,GearSlot weapon,
            Func<string,TechPartDef> parts,Func<string,AffixDef> affixes)
        {
            var total=Levels.Calculate(character,member.level);
            foreach(var (item,def) in Worn(state,member,parts))
            {
                var stats=PartTotal(def,item,affixes);
                // The weapon not in hand keeps its Output to its own attack; everything else it carries reaches both, affix or base (D008). Its base stats used to be dropped whole with the
                // Output: a Choir Emitter's Resonance +4 stood on its row and reached no fight, since the fight reads every stat but Output off the Edge's sum (PartyController.RefreshStats).
                if((def.slot==GearSlot.Edge||def.slot==GearSlot.Emitter)&&def.slot!=weapon)stats.output=0;
                total+=stats;
            }
            return total;
        }
        /// <summary>Everything a hero's parts do beyond the stats: each part's own passive and its affixes'. Both weapons count, whichever is in hand, as their stats do.</summary>
        public static Mods Modifiers(GameState state,MemberState member,Func<string,TechPartDef> parts,Func<string,AffixDef> affixes)
        {
            var total=new Mods();
            foreach(var (item,def) in Worn(state,member,parts))
            {
                total+=def.mods.On(def.damageType);
                foreach(string name in item.affixes.Take(2)){var affix=affixes(name);if(affix!=null)total+=affix.mods.On(def.damageType);}
            }
            return total;
        }
        /// <summary>A part by its name and what it rolled: "Prime Edge (Keen, Vicious)".</summary>
        public static string Name(TechPartDef def,PartInstance item)
        {
            var rolled=item==null?new List<string>():item.affixes.Take(2).Select(id=>GameCatalog.Find<AffixDef>(id)).Where(a=>a!=null).Select(a=>a.displayName).ToList();
            return rolled.Count>0?$"{def.displayName} ({string.Join(", ",rolled)})":def.displayName;
        }
        public static DamageType WeaponType(GameState state,MemberState member,GearSlot slot,DamageType fallback)
        {
            if(!member.equipped.TryGetValue(slot,out string id))return fallback;
            var item=state.parts.Find(p=>p.instanceId==id);var def=item!=null?GameCatalog.Find<TechPartDef>(item.definitionId):null;
            return def!=null?def.damageType:fallback;
        }
        /// <summary>What upgrade <paramref name="level"/> (1 to 5) costs on this part: Scrap Alloy and scrip by level and tier, and from tier 3 its region's common for every level and its
        /// rare for the last. It used to be 3 Scrap a level on any part, from anywhere, and scrip had nothing to do after Corra's stall (prog160): a Refined part now takes 750 scrip to +5.</summary>
        public static (Ingredient[] materials,int scrip) UpgradeCost(TechPartDef def,int level)
        {
            int tier=Mathf.Max(1,def.tier);level=Mathf.Clamp(level,1,MaxUpgrade);
            var materials=new List<Ingredient>{new Ingredient{id="ScrapAlloy",count=3*level*tier}};
            if(tier>=3&&!string.IsNullOrEmpty(def.upgradeMaterial))materials.Add(new Ingredient{id=def.upgradeMaterial,count=level});
            if(tier>=3&&level>=MaxUpgrade&&!string.IsNullOrEmpty(def.upgradeRare))materials.Add(new Ingredient{id=def.upgradeRare,count=1});
            return (materials.ToArray(),25*level*tier);
        }
        /// <summary>Half the materials a part's upgrades took, rounded down by kind: what salvage gives back beside the part's own yield. The scrip stays spent.</summary>
        public static Ingredient[] UpgradeRefund(TechPartDef def,PartInstance part)
        {
            var spent=new Dictionary<string,int>();
            for(int level=1;level<=Mathf.Clamp(part.upgrade,0,MaxUpgrade);level++)
                foreach(var material in UpgradeCost(def,level).materials){spent.TryGetValue(material.id,out int n);spent[material.id]=n+material.count;}
            return spent.Where(p=>p.Value/2>0).Select(p=>new Ingredient{id=p.Key,count=p.Value/2}).ToArray();
        }
        /// <summary>Upgrades are bench work: from the pause menu in the field a part can only be put on, taken off or broken down.</summary>
        public static bool Upgrade(GameState state,PartInstance part,bool atBench=true)=>Upgrade(state,part,GameCatalog.Find<TechPartDef>(part.definitionId),atBench);
        public static bool Upgrade(GameState state,PartInstance part,TechPartDef def,bool atBench=true)
        {
            if(!atBench||def==null||!state.parts.Contains(part)||part.upgrade>=MaxUpgrade)return false;
            var (materials,scrip)=UpgradeCost(def,part.upgrade+1);var inventory=new Inventory(state);
            if(state.scrip<scrip||!inventory.Spend(materials))return false;
            state.scrip-=scrip;part.upgrade++;return true;
        }
        /// <summary>What it costs to roll one of a part's affixes again: scrip by tier, and from tier 3 its region's rare.</summary>
        public static (Ingredient[] materials,int scrip) RetuneCost(TechPartDef def)
        {
            int tier=Mathf.Max(1,def.tier);
            var materials=tier>=3&&!string.IsNullOrEmpty(def.upgradeRare)?new[]{new Ingredient{id=def.upgradeRare,count=1}}:Array.Empty<Ingredient>();
            return (materials,75*tier);
        }
        /// <summary>Rolls the affix at <paramref name="index"/> again, at a bench: it becomes another from the part's pool, never the one it was nor the other it carries.
        /// Nothing is spent when the pool has nothing else to offer.</summary>
        public static bool Retune(GameState state,PartInstance part,int index,System.Random rng=null,bool atBench=true)=>Retune(state,part,GameCatalog.Find<TechPartDef>(part.definitionId),index,rng,atBench);
        public static bool Retune(GameState state,PartInstance part,TechPartDef def,int index,System.Random rng=null,bool atBench=true)
        {
            if(!atBench||def==null||!state.parts.Contains(part)||index<0||index>=Mathf.Min(2,part.affixes.Count))return false;
            var (materials,scrip)=RetuneCost(def);var inventory=new Inventory(state);
            if(state.scrip<scrip||!inventory.Has(materials))return false;
            string next=Loot.PickAffix(def,part.affixes,rng);if(next==null)return false;
            inventory.Spend(materials);state.scrip-=scrip;part.affixes[index]=next;return true;
        }
    }
}
