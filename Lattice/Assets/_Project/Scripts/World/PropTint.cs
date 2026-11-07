using UnityEngine;
namespace Lattice.World
{
    /// <summary>One kit piece weathered apart from its fellows, without a material of its own. A property block isn't saved with a scene, so this has to happen as the scene loads,
    /// and in the editor too, or LookDev's stills show every piece in its catalogue colours.</summary>
    [ExecuteAlways]
    public sealed class PropTint:MonoBehaviour
    {
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
        public Color tint=Color.white;
        void Awake()
        {
            var block=new MaterialPropertyBlock();
            foreach(var part in GetComponentsInChildren<Renderer>(true)){part.GetPropertyBlock(block);block.SetColor(BaseColor,tint);part.SetPropertyBlock(block);}
        }
    }
}
