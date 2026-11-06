using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/ShopInventory")]
    public sealed class ShopInventory:ScriptableObject
    {
        /// <summary>The shop's heading, when it isn't the outfitter's.</summary>
        public string title;
        public TechPartDef[] parts; public ConsumableDef[] consumables; public MaterialDef[] materials; public int[] prices;
    }
}
