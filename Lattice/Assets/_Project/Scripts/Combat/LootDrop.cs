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
            int scrip=definition.boss?150:8;state.scrip+=scrip;Inventory.Announce("scrip",scrip);
            RpgServices.Inventory.Grant("ScrapAlloy",definition.boss?12:2);
            float luck=packet.source!=null&&packet.source.TryGetComponent<CombatActor>(out var actor)?Mathf.Clamp(actor.fortune*.005f,0,.15f):0;
            if(definition.lootTable!=null)foreach(var loot in definition.lootTable.entries)if(Random.value<=Mathf.Clamp01(loot.chance+luck))RpgServices.Inventory.Grant(loot.id,loot.count);
            RpgServices.Quests.Report(ObjectiveKind.Kill,definition.id);
        }
    }
}
