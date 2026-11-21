using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/TechPartDef")]
    public sealed class TechPartDef:ScriptableObject
    {
        public string id,displayName; public GearSlot slot; public DamageType damageType; public int tier=1; public Stats baseStats; public AffixDef[] affixPool; public GameObject model; public Sprite icon; public Ingredient[] salvage;
        /// <summary>A passive the part always has: what makes a module a build, not a number.</summary>
        public Mods mods;
        /// <summary>What an upgrade costs beside Scrap Alloy and scrip, from tier 3 up: its region's common for every level, and its rare for the last (Gear.UpgradeCost). A re-tune takes the rare too.</summary>
        public string upgradeMaterial,upgradeRare;
        /// <summary>Whether a drop or a cache that asks for "any part of this tier" may give this one. Trophy parts and modules with a passive are made or bought, never found at random.</summary>
        public bool drops;
    }
}
