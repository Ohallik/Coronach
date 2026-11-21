using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A lamp that keeps a count. Wendmire's people speak across water in light, and the whole of chapter 4 turns on telling a kept lamp from a lure: Driftreed's own lamps pulse two
    /// short and one long (<c>SSL</c>), the pilots' call is long, long, short (<c>LLS</c>, "still afloat"), a slipped beacon is short, long, short (<c>SLS</c>), and a lure flickers shorts
    /// and never holds a long (an empty <see cref="count"/>). It drives one point light and the emission of any renderers given, through a property block it sets every frame (a
    /// block isn't saved with a scene, so the lamp is drawn dark in an editor still and keeps its count in play). Each count is spoken aloud by someone in the script as well, so
    /// nothing depends on seeing a pulse.
    /// </summary>
    public sealed class RhythmLamp:MonoBehaviour
    {
        /// <summary>One letter a pulse, S short and L long; the dark between pulses is <see cref="gap"/> and the rest after the count <see cref="rest"/>. Empty: a lure's flicker.</summary>
        public string count="SSL";
        public Light glow;
        /// <summary>Whose emission pulses with the light (the lamp's glass).</summary>
        public Renderer[] lamps=System.Array.Empty<Renderer>();
        public Color colour=new(1,.78f,.42f);
        /// <summary>The light's intensity, and the emission's strength, at the top of a pulse.</summary>
        public float intensity=2.4f,glowStrength=2.2f;
        /// <summary>What is left lit between pulses: a kept lamp never goes quite dark.</summary>
        public float dim=.12f;
        public float shortOn=.22f,longOn=.95f,gap=.3f,rest=1.5f;
        /// <summary>Seconds into its count it starts at, so two lamps keeping the same count don't pulse as one.</summary>
        public float offset;
        /// <summary>Dark for good once this is set (a lure whose bloom has died). Empty: never.</summary>
        public string darkWhen;
        /// <summary>Dark until this is set (a beacon that has to be relit). Empty: lit from the start.</summary>
        public string litWhen;
        MaterialPropertyBlock block;
        static readonly int Emission=Shader.PropertyToID("_EmissionColor");

        /// <summary>The count's length in seconds.</summary>
        public float Period
        {
            get
            {
                if(string.IsNullOrEmpty(count))return 1;
                float total=rest;foreach(char pulse in count)total+=(pulse=='L'?longOn:shortOn)+gap;return total;
            }
        }
        /// <summary>How lit the lamp is at a time, nought to one. A lure's flicker is a hash of the time: shorts of two brightnesses with ragged dark between, never one as long as a long.</summary>
        public float Brightness(float time)
        {
            if(string.IsNullOrEmpty(count))
            {
                // Every other slot is dark, so no flash outlasts one slot: a lure's longest flash is a short.
                int slot=Mathf.FloorToInt(time/.13f);if((slot&1)==1)return dim;
                uint h=(uint)(slot*374761393+GetInstanceID()*668265263);h=(h^(h>>13))*1274126177;h^=h>>16;
                return (h&3)==0?dim:(h&4)!=0?1:.55f;
            }
            float t=Mathf.Repeat(time,Period);
            foreach(char pulse in count)
            {
                float on=pulse=='L'?longOn:shortOn;
                if(t<on)return 1;t-=on;
                if(t<gap)return dim;t-=gap;
            }
            return dim;
        }
        public bool Lit
        {
            get
            {
                var services=GameServices.Current;if(services==null)return string.IsNullOrEmpty(litWhen);
                if(!string.IsNullOrEmpty(darkWhen)&&services.Flags.GetBool(darkWhen))return false;
                return string.IsNullOrEmpty(litWhen)||services.Flags.GetBool(litWhen);
            }
        }
        void Update()
        {
            float level=Lit?Brightness(Time.time+offset):0;
            if(glow!=null){glow.enabled=level>0;glow.intensity=intensity*level;}
            if(lamps.Length==0)return;
            block??=new MaterialPropertyBlock();
            foreach(var lamp in lamps)
            {
                if(lamp==null)continue;
                lamp.GetPropertyBlock(block);block.SetColor(Emission,colour*(glowStrength*Mathf.Max(level,0)));lamp.SetPropertyBlock(block);
            }
        }
    }
}
