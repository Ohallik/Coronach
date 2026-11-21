using UnityEngine;
namespace Lattice.World
{
    /// <summary>Moving shade over a zone's ground: cloud shadow drifting down a canyon, or light through a canopy swaying where it falls. A fixed camera shows mostly floor, and a floor
    /// nothing moves across is a painted backdrop. One tiling map is drifted across the sun and cut at a threshold (LatticeToon's FbGroundShade), so its edge is as crisp as a cast shadow's;
    /// it only takes sunlight away, never past what a real shadow takes, and lamps burn through it. Danger markings and floor signs have their own shaders and don't take it.
    /// GroundKit.Shade adds one to a zone; a zone without one has none (the globals are zero).</summary>
    [ExecuteAlways]
    public sealed class GroundShade:MonoBehaviour
    {
        public Texture2D map;
        /// <summary>Metres one repeat of the map covers: 60 to 90 for cloud, 8 to 14 for a canopy.</summary>
        public float size=70;
        /// <summary>Metres a second the shade travels.</summary>
        public Vector2 drift=new(1.1f,.45f);
        /// <summary>How much of the sun a shaded spot loses: 1 is as dark as a cast shadow.</summary>
        [Range(0,1)]public float strength=.5f;
        /// <summary>Where the map is cut: higher shades more of the ground.</summary>
        [Range(0,1)]public float threshold=.5f;
        /// <summary>Half the width of the cut, in map value: .01 is a hard edge, .08 a soft one.</summary>
        public float edge=.02f;
        /// <summary>A second, finer reading of the same map breaks up the first's edge. A whole number of times finer, so the drift can wrap without a jump.</summary>
        public int octaveScale=3;[Range(0,1)]public float octaveWeight=.25f;
        /// <summary>A canopy sways rather than travels: metres to and fro, and how fast.</summary>
        public Vector2 sway;public float swayRate=.6f;
        /// <summary>Where the shade lies at time zero, so a still can be composed.</summary>
        public Vector2 phase;
        static readonly int Map=Shader.PropertyToID("_GroundShadeMap"),A=Shader.PropertyToID("_GroundShadeA"),B=Shader.PropertyToID("_GroundShadeB");
        void OnEnable()=>Apply(Application.isPlaying?Time.time:0);
        void LateUpdate()=>Apply(Application.isPlaying?Time.time:0);
        void OnDisable()=>Shader.SetGlobalVector(A,Vector4.zero);
        public void Apply(float time)
        {
            if(map==null||size<=0||strength<=0){Shader.SetGlobalVector(A,Vector4.zero);return;}
            var at=phase+drift*time+sway*Mathf.Sin(time*swayRate);
            Shader.SetGlobalTexture(Map,map);
            Shader.SetGlobalVector(A,new Vector4(Mathf.Repeat(at.x,size),Mathf.Repeat(at.y,size),1/size,strength));
            Shader.SetGlobalVector(B,new Vector4(threshold,Mathf.Max(.002f,edge),octaveScale,octaveWeight));
        }
    }
}
