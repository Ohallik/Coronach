using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/ShopInventory")]
    public sealed class ShopInventory:ScriptableObject
    {
        public TechPartDef[] parts; public ConsumableDef[] consumables; public MaterialDef[] materials; public int[] prices;
    }
}
