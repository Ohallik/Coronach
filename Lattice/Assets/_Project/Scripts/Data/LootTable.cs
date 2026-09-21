using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/LootTable")]
    public sealed class LootTable:ScriptableObject
    {
        public LootEntry[] entries;
    }
}
