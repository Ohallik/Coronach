using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/EnemyDef")]
    public sealed class EnemyDef:ScriptableObject
    {
        public string id; public GameObject prefab,bodySegment,tailSegment; public Stats stats; public float integrity=100,breakThreshold=80,speed=3.2f; public AttackDef[] attacks; public DamageType weakness,resistance; public EnemyArchetype archetype; public LootTable lootTable; public int xp=24; public bool boss;
    }
}
