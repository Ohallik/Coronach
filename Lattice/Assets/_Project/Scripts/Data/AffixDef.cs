using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/AffixDef")]
    public sealed class AffixDef:ScriptableObject
    {
        /// <summary>The bonus is for a tier 1 part and scales with the part's tier (Gear.AffixStats): a flat +4 that mattered on Scrap is noise on a Choirwrought part.</summary>
        public string id,displayName; public Stats bonus;
        /// <summary>What it does beyond the stats. These don't scale with tier: five points of crit are five points on any part.</summary>
        public Mods mods;
    }
}
