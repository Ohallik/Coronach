using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/ConsumableDef")]
    public sealed class ConsumableDef:ScriptableObject
    {
        public string id,displayName; public Sprite icon; public int stack=99; public float heal=60,charge;
    }
}
