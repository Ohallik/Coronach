using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// One of the Sorrel survey crew's evacuation beacons (D199, "Quiet ridges"). It blinks its call until its flag is set,
    /// then stays dark, including on every later visit. The light is the beacon's state, so a player can see from the
    /// trail which ones still call.
    /// </summary>
    public sealed class SignalBeacon:MonoBehaviour
    {
        public string flag;
        public Light glow;
        public Renderer lamp;
        public float intensity=2.2f;
        MaterialPropertyBlock block;
        static readonly int Emission=Shader.PropertyToID("_EmissionColor");
        public bool Quiet=>GameServices.Current!=null&&!string.IsNullOrEmpty(flag)&&GameServices.Current.Flags.GetBool(flag);
        void Update()
        {
            bool quiet=Quiet;
            // A slow double pulse: an evacuation call, not a decoration.
            float t=Mathf.Repeat(Time.time,1.6f);float pulse=t<.12f||t>.3f&&t<.42f?1:.08f;
            if(glow!=null){glow.enabled=!quiet;glow.intensity=intensity*pulse;}
            if(lamp!=null)
            {
                block??=new MaterialPropertyBlock();lamp.GetPropertyBlock(block);
                block.SetColor(Emission,quiet?Color.black:new Color(.35f,.75f,1f)*(2.5f*pulse));lamp.SetPropertyBlock(block);
            }
        }
    }
}
