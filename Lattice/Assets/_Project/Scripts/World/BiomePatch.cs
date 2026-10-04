using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.World
{
    /// <summary>A complete public visual, optionally replaced by licensed local art.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class BiomePatch : MonoBehaviour
    {
        public string resourceKey;
        public string palette;
        public bool water;
        // Local root position and blade height; also used by the optional art baker.
        public Vector4[] sprouts = System.Array.Empty<Vector4>();
        public bool UsingLocalArt { get; private set; }

        void Awake() => ApplyLocalArt();

        public bool ApplyLocalArt()
        {
            var art = Resources.Load<BiomeVisualAsset>("LocalBiomes/" + resourceKey);
            if (art == null || art.mesh == null || art.material == null || art.material.shader == null || !art.material.shader.isSupported)
                return false;
            GetComponent<MeshFilter>().sharedMesh = art.mesh;
            GetComponent<MeshRenderer>().sharedMaterial = art.material;
            UsingLocalArt = true;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InstallLocalPipeline()
        {
            var pipeline = Resources.Load<RenderPipelineAsset>("LocalBiomes/LocalPipeline");
            if (pipeline == null) return;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            Debug.Log("BIOME_LOCAL_PIPELINE " + pipeline.name);
        }
    }
}
