using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/AffixDef")]
    public sealed class AffixDef:ScriptableObject
    {
        public string id,displayName; public Stats bonus;
    }
}
