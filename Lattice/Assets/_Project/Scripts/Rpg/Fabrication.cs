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
        public bool Craft(RecipeDef recipe,bool atBench,double roll,out PartInstance item)
        {
            item=null;
            if(recipe==null||recipe.unlockLevel>Level(recipe.discipline)||recipe.tier>Mathf.Min(3,Level(recipe.discipline))||
                (recipe.output!=null&&!atBench))return false;
            if(recipe.output==null&&string.IsNullOrEmpty(recipe.consumableOutput))return false;
            var inv=new Inventory(state);if(!inv.Spend(recipe.inputs))return false;
            if(recipe.output!=null)
            {
                item=inv.GivePart(recipe.output);
                if(roll<OverclockChance(recipe.discipline)&&recipe.output.affixPool?.Length>0)item.affixes.Add(recipe.output.affixPool[0].id);
            }
            else {state.consumables.TryGetValue(recipe.consumableOutput,out var n);state.consumables[recipe.consumableOutput]=n+1;}
            state.craftingXp[recipe.discipline.ToString()]=Xp(recipe.discipline)+recipe.tier*10;
            return true;
        }
    }
}
