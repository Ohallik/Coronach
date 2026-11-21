using System;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
namespace Lattice.Rpg
{
    /// <summary>The draught and the meal a party is under. Both lend the same kind of thing a part does (<see cref="Mods"/>), to both heroes, for a time; they live in the save,
    /// so a zone change or a reload doesn't eat them. One of each runs at a time: a second draught takes the first one's place, and a meal doesn't disturb a draught.</summary>
    public static class PartyBuffs
    {
        static ConsumableDef Def(string id)=>string.IsNullOrEmpty(id)?null:GameCatalog.Find<ConsumableDef>(id);
        /// <summary>What's running, in the order the HUD shows it: the draught, then the meal.</summary>
        public static IEnumerable<(ConsumableDef def,float left)> Running(GameState state)
        {
            if(state.draughtLeft>0){var draught=Def(state.draught);if(draught!=null)yield return (draught,state.draughtLeft);}
            if(state.mealLeft>0){var meal=Def(state.meal);if(meal!=null)yield return (meal,state.mealLeft);}
        }
        /// <summary>Everything the party's draught and meal lend right now, summed as a hero's parts are.</summary>
        public static Mods Current(GameState state)
        {
            var total=new Mods();foreach(var (def,_) in Running(state))total+=def.buff;return total;
        }
        public static void Begin(GameState state,ConsumableDef def)
        {
            if(def==null||def.seconds<=0)return;
            if(def.meal){state.meal=def.id;state.mealLeft=def.seconds;}else{state.draught=def.id;state.draughtLeft=def.seconds;}
        }
        /// <summary>Runs both down by the fight's own clock: a menu, a conversation or a load doesn't spend them.</summary>
        public static void Tick(GameState state,float seconds)
        {
            if(seconds<=0)return;
            if(state.draughtLeft>0){state.draughtLeft=Math.Max(0,state.draughtLeft-seconds);if(state.draughtLeft<=0)state.draught=null;}
            if(state.mealLeft>0){state.mealLeft=Math.Max(0,state.mealLeft-seconds);if(state.mealLeft<=0)state.meal=null;}
        }
    }
}
