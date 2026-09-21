using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/MaterialDef")]
    public sealed class MaterialDef:ScriptableObject
    {
        public string id,displayName; public Sprite icon; public int stack=999;
    }
}
