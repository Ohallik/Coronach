using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/AttackDef")]
    public sealed class AttackDef:ScriptableObject
    {
        public float telegraph=0.6f,range=2.5f,radius=1,damage=18,breakPower=15,cooldown=1.6f; public DamageType type; public bool projectile;
    }
}
