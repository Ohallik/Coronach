using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/ConsumableDef")]
    public sealed class ConsumableDef:ScriptableObject
    {
        /// <summary><see cref="stack"/> is the most a party carries (prog159): past it a shop and a bench refuse, and loot pays its <see cref="value"/> in scrip instead.</summary>
        public string id,displayName; public Sprite icon; public int stack=99; public float heal=60,charge;
        /// <summary>Integrity restored as a share of the hero's own bar, so a gel is worth using at Sync 30: the heal is this or <see cref="heal"/>, whichever is more.</summary>
        public float healPercent;
        /// <summary>Raises the partner who is down, to <see cref="healPercent"/> of their bar, and does nothing else.</summary>
        public bool revive;
        /// <summary>Restores both heroes, not only the one who used it.</summary>
        public bool party;
        /// <summary>What it lends both heroes for <see cref="seconds"/>. One draught and one meal run at a time: another of the same kind takes the first one's place.</summary>
        public Mods buff; public float seconds;
        /// <summary>Cooked food: the longer, milder kind, bought from a cook and never fabricated.</summary>
        public bool meal;
        /// <summary>Scrip a spare one is worth, to a shop's price list and to loot found with the pouch full.</summary>
        public int value=10;
        /// <summary>A word for the HUD while its buff runs ("SURGE").</summary>
        public string mark;
    }
}
