using System;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Rpg
{
    public sealed class Fabrication
    {
        readonly GameState state;
        public Fabrication(GameState state){this.state=state;}
        public int Xp(Discipline d)=>state.craftingXp.TryGetValue(d.ToString(),out var n)?n:0;
        public int Level(Discipline d)=>Mathf.Clamp(1+Xp(d)/30,1,10);
        public float OverclockChance(Discipline d)=>Mathf.Clamp(.04f+.02f*(Level(d)-1),.04f,.22f);
        /// <summary>The highest tier a ladder ever opens: Scrap, Refined, Prime, Tempered, Choirwrought. A ladder's level opens the tier of the same number.</summary>
        public const int TopTier=5;
        /// <summary>From this ladder level an overclock rolls twice, and a part can come off the bench with two affixes.</summary>
        public const int SecondOverclockLevel=6;
        /// <summary>Why a recipe can't be made right now, first reason first.</summary>
        public enum Block{None,Unknown,Rank,Bench,Hostile,Full,Materials}
        public bool Ranked(RecipeDef recipe)=>recipe!=null&&recipe.unlockLevel<=Level(recipe.discipline)&&recipe.tier<=Mathf.Min(TopTier,Level(recipe.discipline));
        /// <summary>Whether the bench lists it at all. A recipe that takes a later region's material stays off the list until the party has held that material once:
        /// a bench on Sorrel doesn't show ten pages of things made from plants nobody has seen.</summary>
        public bool Known(RecipeDef recipe)
        {
            foreach(var input in recipe.inputs??Array.Empty<Ingredient>())
            {
                var material=GameCatalog.Find<MaterialDef>(input.id);if(material==null||material.chapter<3)continue;
                state.collectedMaterials.TryGetValue(input.id,out int seen);state.materials.TryGetValue(input.id,out int held);
                if(seen<=0&&held<=0)return false;
            }
            return true;
        }
        /// <param name="enemiesAwake">Something nearby is awake and hostile. A part was always bench work; a consumable could be made from the pause menu with a boss frozen mid-swing (prog159).</param>
        public Block Blocked(RecipeDef recipe,bool atBench,bool enemiesAwake=false)
        {
            if(recipe==null||recipe.output==null&&string.IsNullOrEmpty(recipe.consumableOutput))return Block.Unknown;
            if(!Ranked(recipe))return Block.Rank;
            if(recipe.output!=null&&!atBench)return Block.Bench;
            var inv=new Inventory(state);
            if(recipe.output==null)
            {
                if(enemiesAwake)return Block.Hostile;
                if(inv.Full(GameCatalog.Find<ConsumableDef>(recipe.consumableOutput)))return Block.Full;
            }
            return inv.Has(recipe.inputs)?Block.None:Block.Materials;
        }
        public bool Craft(RecipeDef recipe,bool atBench,double roll,out PartInstance item,bool enemiesAwake=false,System.Random rng=null)
        {
            item=null;
            if(Blocked(recipe,atBench,enemiesAwake)!=Block.None)return false;
            var inv=new Inventory(state);if(!inv.Spend(recipe.inputs))return false;
            if(recipe.output!=null)
            {
                item=inv.GivePart(recipe.output);rng??=Loot.Rng;
                // An overclock is one affix out of the part's own pool, any of them: it was the pool's first, every time, so a second could never happen. From ladder 6 it rolls twice.
                float chance=OverclockChance(recipe.discipline);
                if(roll<chance){string first=Loot.PickAffix(recipe.output,item.affixes,rng);if(first!=null)item.affixes.Add(first);}
                if(Level(recipe.discipline)>=SecondOverclockLevel&&rng.NextDouble()<chance){string second=Loot.PickAffix(recipe.output,item.affixes,rng);if(second!=null)item.affixes.Add(second);}
                // Only a part trains a ladder. A consumable's recipe names no discipline and the field's default is Emitters: twelve Repair Gels took Emitters from 2 to 6 for 24 Scrap Alloy.
                state.craftingXp[recipe.discipline.ToString()]=Xp(recipe.discipline)+recipe.tier*10;
            }
            else {state.consumables.TryGetValue(recipe.consumableOutput,out var n);state.consumables[recipe.consumableOutput]=n+1;}
            return true;
        }
    }
}
