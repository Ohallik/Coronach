using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/TechPartDef")]
    public sealed class TechPartDef:ScriptableObject
    {
        public string id,displayName; public GearSlot slot; public DamageType damageType; public int tier=1; public Stats baseStats; public AffixDef[] affixPool; public GameObject model; public Sprite icon; public Ingredient[] salvage;
    }
}
