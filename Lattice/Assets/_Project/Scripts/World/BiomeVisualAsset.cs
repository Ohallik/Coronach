using UnityEngine;

namespace Lattice.World
{
    /// <summary>Optional locally baked art. Public scenes never reference this asset.</summary>
    public sealed class BiomeVisualAsset : ScriptableObject
    {
        public Mesh mesh;
        public Material material;
    }
}
