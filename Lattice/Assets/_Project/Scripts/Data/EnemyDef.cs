using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/EnemyDef")]
    public sealed class EnemyDef:ScriptableObject
    {
        /// <summary>A person, not a monster: at zero she gives up rather than dies, and hands over to the person standing there.</summary>
        public bool yieldsOnDefeat;
        public GameObject collarPlate;
        public string id; public GameObject prefab,bodySegment,tailSegment; public Stats stats; public float integrity=100,breakThreshold=80,speed=3.2f; public AttackDef[] attacks; public DamageType weakness,resistance; public EnemyArchetype archetype; public LootTable lootTable; public int xp=24; public bool boss;
    }
}
