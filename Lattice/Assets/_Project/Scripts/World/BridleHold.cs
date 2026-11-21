using System;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// The first Bridle's hold on the Glass Choir, seen (chapter 7). While it holds, a slow white pulse runs along every fibre in <see cref="fibres"/> toward the engine (each line runs from its far end
    /// to its first point), and the light through the body is still: the shafts in <see cref="shafts"/> stand where they have stood for a thousand years. Once <see cref="flag"/> is set (the Warden has
    /// stood down and the four clamps have let go together) the pulse stops, every fibre goes slack and amber over a few seconds, and the light moves for the first time in an age: each shaft
    /// swings slowly, each light in <see cref="lights"/> drifts and breathes. Nobody says so; the script leaves it to the screen. A save with the flag loads released, with nothing played but the
    /// moving light itself. Logs <c>BRIDLE_RELEASED flag</c> when it lets go in play. <see cref="Pulse"/> is the fibre's look, shared with each <see cref="BridleStay"/>'s own.
    /// </summary>
    public sealed class BridleHold:MonoBehaviour
    {
        public string flag;
        public LineRenderer[] fibres=Array.Empty<LineRenderer>();
        /// <summary>For each fibre, a flag that lets that one go before the rest (the engine hall's three to a clamp, as the clamp opens). Empty, or shorter than the fibres: none.</summary>
        public string[] slackFlags=Array.Empty<string>();
        public Color held=new(.62f,.7f,.8f),band=Color.white,slack=new(.42f,.24f,.1f);
        /// <summary>Seconds for one pulse to run a fibre's length; a fibre's own offset keeps two from pulsing as one.</summary>
        public float period=3.4f;
        /// <summary>The light through the glass: shafts that are still while the Bridle holds and swing once it has let go, by <see cref="sway"/> degrees over <see cref="swaySeconds"/>.</summary>
        public Transform[] shafts=Array.Empty<Transform>();public float sway=7,swaySeconds=16;
        public Light[] lights=Array.Empty<Light>();
        bool known,released;float since=-1;Quaternion[] rest;Vector3[] home;float[] bright;
        /// <summary>0 while the Bridle holds, rising to 1 over the few seconds it takes to let go; 1 in a save that has it.</summary>
        public float Released{get;private set;}
        void Awake()
        {
            rest=new Quaternion[shafts.Length];for(int i=0;i<shafts.Length;i++)if(shafts[i]!=null)rest[i]=shafts[i].rotation;
            home=new Vector3[lights.Length];bright=new float[lights.Length];for(int i=0;i<lights.Length;i++)if(lights[i]!=null){home[i]=lights[i].transform.position;bright[i]=lights[i].intensity;}
        }
        void Update()
        {
            var services=GameServices.Current;
            bool now=services!=null&&!string.IsNullOrEmpty(flag)&&services.Flags.GetBool(flag);
            if(!known){known=true;released=now;if(now){since=float.PositiveInfinity;Released=1;}}
            else if(now&&!released){released=true;since=0;Debug.Log("BRIDLE_RELEASED "+flag);}
            if(released&&since<float.PositiveInfinity){since+=Time.deltaTime;Released=Mathf.Clamp01(since/3.5f);if(since>3.5f)since=float.PositiveInfinity;}
            float t=Time.time;
            for(int i=0;i<fibres.Length;i++)
            {
                var line=fibres[i];if(line==null)continue;
                bool own=i<slackFlags.Length&&!string.IsNullOrEmpty(slackFlags[i])&&services!=null&&services.Flags.GetBool(slackFlags[i]);
                if(Released<=0&&!own)Pulse(line,t/period+i*.37f,held,band,.85f);
                else Pulse(line,0,own?slack:Color.Lerp(held,slack,Released),slack,0);
            }
            if(Released<=0)return;
            float s=Mathf.SmoothStep(0,1,Released);
            for(int i=0;i<shafts.Length;i++)
            {
                if(shafts[i]==null)continue;
                float swing=Mathf.Sin(t*Mathf.PI*2/swaySeconds+i*1.9f)*sway*s;
                shafts[i].rotation=Quaternion.AngleAxis(swing,Vector3.up)*rest[i];
            }
            for(int i=0;i<lights.Length;i++)
            {
                if(lights[i]==null)continue;
                var drift=new Vector3(Mathf.Sin(t*.21f+i*2.3f),Mathf.Sin(t*.17f+i)*.4f,Mathf.Cos(t*.19f+i*1.7f))*2.2f*s;
                lights[i].transform.position=home[i]+drift;lights[i].intensity=bright[i]*(1+.25f*s*Mathf.Sin(t*.6f+i));
            }
        }

        static readonly Gradient shared=new();static readonly GradientColorKey[] five=new GradientColorKey[5],two=new GradientColorKey[2];
        static readonly GradientAlphaKey[] solid={new(1,0),new(1,1)};
        /// <summary>A fibre's colour along its length: <paramref name="body"/> all along, and, with <paramref name="strength"/> over nought, a band of <paramref name="bright"/> at the place
        /// <paramref name="phase"/> puts it, running from the line's last point toward its first as the phase grows.</summary>
        public static void Pulse(LineRenderer line,float phase,Color body,Color bright,float strength)
        {
            if(line==null)return;
            if(strength<=0)
            {
                two[0]=new GradientColorKey(body,0);two[1]=new GradientColorKey(body,1);shared.SetKeys(two,solid);
            }
            else
            {
                float p=Mathf.Clamp(1-Mathf.Repeat(phase,1),.01f,.99f);const float wide=.11f;
                float before=Mathf.Clamp(p-wide,.001f,p-.002f),after=Mathf.Clamp(p+wide,p+.002f,.999f);
                five[0]=new GradientColorKey(body,0);five[1]=new GradientColorKey(body,before);five[2]=new GradientColorKey(Color.Lerp(body,bright,strength),p);
                five[3]=new GradientColorKey(body,after);five[4]=new GradientColorKey(body,1);shared.SetKeys(five,solid);
            }
            line.colorGradient=shared;
        }
    }
}
