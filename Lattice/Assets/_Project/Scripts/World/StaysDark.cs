using UnityEngine;
namespace Lattice.World
{
    /// <summary>A dead relay: its crystal unlit, so a relit one stands out. The model's own emission is what says a relay burns, and a property
    /// block isn't saved with a scene, so this has to happen as the scene loads and not when the builder runs. In the editor too: LookDev's
    /// stills open the scene there, and would show every dead relay burning.</summary>
    [ExecuteAlways]
    public sealed class StaysDark:MonoBehaviour
    {
        static readonly int Emission=Shader.PropertyToID("_EmissionColor"),BaseMap=Shader.PropertyToID("_BaseMap");
        /// <summary>The tower's texture with its amber glass gone smoky. Dead means dark, and unlit amber in full sun still reads as lit.</summary>
        public Texture glass;
        void Awake()
        {
            var block=new MaterialPropertyBlock();
            foreach(var part in GetComponentsInChildren<Renderer>(true)){part.GetPropertyBlock(block);block.SetVector(Emission,Vector4.zero);if(glass!=null)block.SetTexture(BaseMap,glass);part.SetPropertyBlock(block);}
        }
    }
}
