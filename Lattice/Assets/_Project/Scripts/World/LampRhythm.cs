using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A lamp that speaks in light, as Wendmire's do (chapter 4; manifest section 1, "The two light signals"). Driftreed's own count is two short and one long: somebody keeps this lamp.
    /// The pilots' "still afloat" is long, long, short. A beacon that has slipped its count blinks short, long, short; a lure flickers shorts and never holds a long. Every one of these is
    /// also said aloud by someone who sees it, so nothing depends on reading a pulse; this is the world drawing what they say.
    /// Drives its lights and the emission of its lamp renderers (a property block, set every frame, so nothing about it is saved with the scene). With <see cref="litFlag"/> it is dark until
    /// that flag is set (the dead beacon); with <see cref="retunedFlag"/> it keeps <see cref="count"/> until that flag is set, then <see cref="retuned"/> (an off-lane beacon set right);
    /// with <see cref="darkFlag"/> it goes out for good once that flag is set (Maru's lamp once Ona has answered).
    /// </summary>
    public sealed class LampRhythm:MonoBehaviour
    {
        public enum Count{TwoShortOneLong,LongLongShort,ShortLongShort,Flicker,Steady}
        public Count count=Count.TwoShortOneLong,retuned=Count.TwoShortOneLong;
        public string litFlag,retunedFlag,darkFlag;
        public Light[] lights=System.Array.Empty<Light>();
        public Renderer[] lamps=System.Array.Empty<Renderer>();
        /// <summary>Additive halos round the lamp (a beacon read from the flight camera's height): their colour goes with the count.</summary>
        public Renderer[] halos=System.Array.Empty<Renderer>();
        public Color colour=new(1,.78f,.42f);
        public float intensity=2.4f,emission=2.2f;
        /// <summary>Where in its count it starts, in seconds: two lamps side by side keep time apart.</summary>
        public float offset;
        /// <summary>How bright it sits between flashes (0 is out): a lamp between its pulses is a coal, not a hole.</summary>
        public float rest=.08f;
        const float Short=.22f,Long=.95f,Gap=.28f,Pause=1.35f;
        MaterialPropertyBlock block;static readonly int Emission=Shader.PropertyToID("_EmissionColor"),Base=Shader.PropertyToID("_BaseColor");
        /// <summary>The count it is keeping now (for a test; Steady when dark).</summary>
        public Count Keeping{get;private set;}
        public bool Dark{get;private set;}
        /// <summary>On (1) or off (0) at a time into a count.</summary>
        public static float At(Count count,float t,int seed=0)
        {
            float[] beats=count switch{Count.TwoShortOneLong=>new[]{Short,Short,Long},Count.LongLongShort=>new[]{Long,Long,Short},Count.ShortLongShort=>new[]{Short,Long,Short},_=>null};
            if(count==Count.Steady)return 1;
            if(beats==null)
            {
                // A lure: shorts at no rhythm, and never one held: a flash is one slot and the slot after it is always dark.
                float slot=Mathf.Floor(t/.14f);float roll=Mathf.Abs(Mathf.Sin(slot*12.9898f+seed*78.233f)*43758.5453f)%1;return slot%2==0&&roll>.3f?1:0;
            }
            float cycle=Pause;foreach(var beat in beats)cycle+=beat+Gap;
            float at=Mathf.Repeat(t,cycle);
            foreach(var beat in beats){if(at<beat)return 1;at-=beat;if(at<Gap)return 0;at-=Gap;}
            return 0;
        }
        void Update()
        {
            var flags=GameServices.Current!=null?GameServices.Current.Flags:null;
            bool dark=flags!=null&&(!string.IsNullOrEmpty(litFlag)&&!flags.GetBool(litFlag)||!string.IsNullOrEmpty(darkFlag)&&flags.GetBool(darkFlag));
            Keeping=dark?Count.Steady:flags!=null&&!string.IsNullOrEmpty(retunedFlag)&&flags.GetBool(retunedFlag)?retuned:count;Dark=dark;
            float on=dark?0:Mathf.Lerp(rest,1,At(Keeping,Time.time+offset,GetInstanceID()));
            foreach(var light in lights)if(light!=null){light.enabled=on>.01f;light.intensity=intensity*on;light.color=colour;}
            block??=new MaterialPropertyBlock();
            foreach(var lamp in lamps)
            {
                if(lamp==null)continue;
                lamp.GetPropertyBlock(block);block.SetColor(Emission,colour*(emission*Mathf.Max(on,dark?0:.04f)));lamp.SetPropertyBlock(block);
            }
            foreach(var halo in halos)
            {
                if(halo==null)continue;
                halo.enabled=on>.02f;if(!halo.enabled)continue;
                halo.GetPropertyBlock(block);block.SetColor(Base,new Color(colour.r*1.6f,colour.g*1.6f,colour.b*1.6f,on));halo.SetPropertyBlock(block);
            }
        }
    }
}
