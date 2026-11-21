using Lattice.Combat;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A fibre's light in play. Lying across a fight's floor (<see cref="line"/> set), it shows the rule in the fight: while a Fibre Wisp tells a strike down its <see cref="ForceLine"/>,
    /// the thread brightens with the tell (<see cref="ForceLine.Glow"/>, 0 to 1, rising until the strike lands), its halo thickens and a light runs on the stone under it; slack, it is
    /// dim for good. Fraying (<see cref="gutter"/> above 0, no line), it gutters: its core, its halo and its light dip and catch unevenly, now and then going nearly out.
    /// Set by property block only when it changes, so an editor still shows it as its materials draw it at rest.
    /// </summary>
    public sealed class FibreGlow:MonoBehaviour
    {
        public ForceLine line;
        /// <summary>The thread's core and its soft halo, each its own renderer.</summary>
        public Renderer core,halo;
        public Light shine;
        public Color rest=new(1.9f,2.3f,3.2f,1),lit=new(4.4f,5f,6.4f,1),restBase=new(.86f,.92f,1,1),haloTint=new(.42f,.62f,1,.42f);
        public float haloRest=.42f,haloLit=.95f,shineLit=3.2f;
        /// <summary>How hard a frayed fibre gutters (0: steady). Its core and halo are drawn in vertex-lit materials, so the block scales their base colour.</summary>
        [Range(0,1)] public float gutter;
        public float shineRest=1.2f;
        MaterialPropertyBlock block;float shown=-1;Color coreBase=Color.white,haloBase=Color.white;float seed;
        void Awake()
        {
            block=new MaterialPropertyBlock();seed=Random.value*100;
            if(core!=null&&core.sharedMaterial!=null&&core.sharedMaterial.HasProperty("_BaseColor"))coreBase=core.sharedMaterial.GetColor("_BaseColor");
            if(halo!=null&&halo.sharedMaterial!=null&&halo.sharedMaterial.HasProperty("_BaseColor"))haloBase=halo.sharedMaterial.GetColor("_BaseColor");
        }
        void LateUpdate()
        {
            if(line==null&&gutter>0){Gutter();return;}
            float glow=line==null?0:line.Slack?-1:line.Glow;if(Mathf.Abs(glow-shown)<.01f)return;shown=glow;
            float g=Mathf.Clamp01(glow);var colour=glow<0?rest*.35f:Color.Lerp(rest,lit,g);
            if(core!=null){core.GetPropertyBlock(block);block.SetColor("_EmissionColor",colour);block.SetColor("_BaseColor",Color.Lerp(restBase,Color.white,g));core.SetPropertyBlock(block);}
            if(halo!=null){halo.GetPropertyBlock(block);block.SetColor("_BaseColor",new Color(haloTint.r,haloTint.g,haloTint.b,glow<0?.08f:Mathf.Lerp(haloRest,haloLit,g)));halo.SetPropertyBlock(block);}
            if(shine!=null){shine.enabled=g>.02f;shine.intensity=shineLit*g;}
        }
        /// <summary>A guttering light: two slow waverings and a fast flutter, with a dip near to dark now and then.</summary>
        void Gutter()
        {
            float t=Time.time;float slow=Mathf.PerlinNoise(t*1.3f,seed),fast=Mathf.PerlinNoise(t*9f,seed+7),dip=Mathf.PerlinNoise(t*.7f,seed+13);
            float level=Mathf.Clamp01(.55f+.45f*slow+.25f*(fast-.5f))*(dip<.22f?.25f:1);level=Mathf.Lerp(1,level,gutter);
            if(Mathf.Abs(level-shown)<.02f)return;shown=level;
            if(core!=null){core.GetPropertyBlock(block);block.SetColor("_BaseColor",new Color(coreBase.r*level,coreBase.g*level,coreBase.b*level,coreBase.a));core.SetPropertyBlock(block);}
            if(halo!=null){halo.GetPropertyBlock(block);block.SetColor("_BaseColor",new Color(haloBase.r,haloBase.g,haloBase.b,haloBase.a*level));halo.SetPropertyBlock(block);}
            if(shine!=null){shine.enabled=level>.05f;shine.intensity=shineRest*level;}
        }
    }
}
