using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class LootDrop:MonoBehaviour
    {
        public EnemyDef definition;
        void Start(){GetComponent<Health>().Died+=Drop;}
        void Drop(Health _,DamagePacket packet)
        {
            // A mine that went off by itself was no victory: no XP for flying close to it.
            if(packet.tag==EnemyBrain.SelfDetonation)return;
            var state=GameServices.Current.State;
            foreach(var member in state.party)Levels.Grant(member,definition.xp);
            // Both heroes earn the same, so the feed says it once. A quest's XP stays on its own card.
            Inventory.Announce("xp",definition.xp);
            // Each enemy pays its own scrip and Scrap Alloy; one that names neither pays what they all used to.
            int scrip=Scrip(definition),scrap=Scrap(definition);
            if(scrip>0){state.scrip+=scrip;Inventory.Announce("scrip",scrip);}
            if(scrap>0)RpgServices.Inventory.Grant("ScrapAlloy",scrap);
            float luck=packet.source!=null&&packet.source.TryGetComponent<CombatActor>(out var actor)?Luck(actor):0;
            // An entry is a material, a consumable, a part by name or "any part of a tier": a part comes rolled (Inventory.GrantLoot).
            if(definition.lootTable!=null&&definition.lootTable.entries!=null)foreach(var loot in definition.lootTable.entries)if(Random.value<=Mathf.Clamp01(loot.chance+luck))RpgServices.Inventory.GrantLoot(loot);
            RpgServices.Quests.Report(ObjectiveKind.Kill,definition.id);
        }
        public static int Scrip(EnemyDef definition)=>definition.scrip>=0?definition.scrip:definition.boss?150:8;
        public static int Scrap(EnemyDef definition)=>definition.scrap>=0?definition.scrap:definition.boss?12:2;
        /// <summary>What the killer's Fortune adds to every drop's chance: half a point each, up to 15, and what gear adds to the cap (Fortune Compass) and past it (Scavenger).</summary>
        public static float Luck(CombatActor killer)=>Mathf.Clamp(killer.fortune*.005f,0,.15f+Mathf.Max(0,killer.mods.luckCap))+killer.mods.dropLuck;
    }
}
