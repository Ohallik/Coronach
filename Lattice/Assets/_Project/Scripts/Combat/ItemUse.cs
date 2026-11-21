using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>What using a consumable does: a heal as a share of the hero's own bar, Charge, both heroes at once, a partner raised, or a draught or a meal begun.
    /// Every use starts one shared wait before the next (prog159): gels used to go down one after another, from the menu, with a boss frozen in front of them.
    /// The wait runs on the fight's clock, which a menu stops, so pausing doesn't serve it.</summary>
    public static class ItemUse
    {
        /// <summary>Seconds between one item and the next, whichever items and whichever hero.</summary>
        public const float Cooldown=4;
        static float readyAt=-99;
        /// <summary>Seconds of the wait left since the last item, whether or not anything is holding the hero to it.</summary>
        public static float ReadyIn=>Mathf.Max(0,readyAt-GameTime.Now);
        /// <summary>Seconds this hero has to wait before another item: the wait only binds in a fight. Between fights there's nothing to pace, and a hurt party at a campfire
        /// shouldn't have to close the menu between two gels.</summary>
        public static float Wait(CombatActor actor)=>ReadyIn>0&&actor!=null&&GearEffects.InFight(actor.transform.position)?ReadyIn:0;
        /// <summary>A new party starts free to use one: the wait belongs to a fight, not to a save or a zone.</summary>
        public static void Reset()=>readyAt=-99;
        /// <summary>What a heal is worth to this hero: the share of their bar, or the flat amount where that's more (a gel on a new hero).</summary>
        public static float Heal(ConsumableDef def,CombatActor who)=>Mathf.Max(def.heal,def.healPercent*who.Health.maximum);
        /// <summary>Uses one on this hero and says what happened, in words for the player. A refusal spends nothing and starts no wait.</summary>
        public static (bool used,string message) Use(CombatActor actor,string id)
        {
            var state=GameServices.Current.State;id??=state.quickItem;
            var def=GameCatalog.Find<ConsumableDef>(id);if(def==null)return (false,"That can't be used.");
            if(!state.consumables.TryGetValue(id,out int held)||held<=0)return (false,$"No {def.displayName} left.");
            float wait=Wait(actor);if(wait>0)return (false,$"Not yet: another item in {Mathf.Max(.1f,wait):0.0} s.");
            var crew=PartyController.Current!=null&&PartyController.Current.members!=null&&System.Array.IndexOf(PartyController.Current.members,actor)>=0?PartyController.Current.members:new[]{actor};
            string said;
            if(def.revive)
            {
                var down=System.Array.Find(crew,m=>m!=null&&m!=actor&&!m.Health.Alive);
                if(down==null)return (false,"Nobody is down.");
                down.Health.Heal(down.Health.maximum*Mathf.Max(.05f,def.healPercent));
                said=$"{def.displayName} raised {DisplayNames.Speaker(down.character)}.";
            }
            else if(def.seconds>0)
            {
                PartyBuffs.Begin(state,def);
                if(PartyController.Current!=null)PartyController.Current.RefreshStats();
                said=$"{def.displayName} is running: {def.seconds:0} s.";
            }
            else
            {
                bool heals=def.heal>0||def.healPercent>0;
                var targets=def.party?System.Array.FindAll(crew,m=>m!=null&&m.Health.Alive):new[]{actor};
                bool Wants(CombatActor who)=>heals&&who.Health.integrity<who.Health.maximum||def.charge>0&&who.charge<100;
                // A Medic Filament passes part of a heal to the partner, and a full hero can still use one for a hurt partner's sake.
                var partner=!def.party&&heals&&actor.mods.sharedHeal>0?System.Array.Find(crew,m=>m!=null&&m!=actor&&m.Health.Alive):null;
                bool shared=partner!=null&&partner.Health.integrity<partner.Health.maximum;
                if(!System.Array.Exists(targets,Wants)&&!shared)return (false,heals?"Integrity is already full.":"Charge is already full.");
                foreach(var who in targets){if(heals)who.Health.Heal(Heal(def,who));who.charge=Mathf.Min(100,who.charge+def.charge);}
                if(partner!=null)partner.Health.Heal(Heal(def,partner)*actor.mods.sharedHeal);
                said=def.party?$"Used {def.displayName} on the party.":$"Used {def.displayName} on {DisplayNames.Speaker(actor.character)}.";
            }
            state.consumables[id]=held-1;readyAt=GameTime.Now+Cooldown;
            return (true,said);
        }
    }
}
