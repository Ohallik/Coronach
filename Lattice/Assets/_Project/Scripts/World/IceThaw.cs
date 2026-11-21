using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Ice answering heat: the Frozen Host's dig, where a thaw-rig worked by a hero turns a plug of ice and the rank standing in the ice beside it from blue to amber, and what is set
    /// to melt slumps and goes. The rig is a <see cref="FlowSwitch"/> on the same flag (the wheel, the burner's roar, the view drawn back over the ice); this is the ice. Like every
    /// <see cref="WorldSwitch"/> it plays when its flag goes from unset to set while the zone is open, and a save that has the flag loads thawed with nothing played.
    /// <see cref="ice"/> warms by property block, set every frame it changes and again at load, so a still taken in the editor shows the material as built: ice that starts amber (the
    /// Muster's own thaw, an empty <see cref="WorldSwitch.flag"/>) is built with the amber material. Each of <see cref="melts"/> is a <see cref="Membrane"/> with a carving obstacle (a seal
    /// like any other, so the world tests that open every seal open it); it sinks and dissolves after the ice has warmed, and is opened. Once <see cref="refreezeFlag"/> is set every
    /// piece of ice here goes blue again (the cold coming in behind the party once the Host Marshal sleeps); what melted stays gone.
    /// Logs <c>ICE_THAW flag</c> (or <see cref="WorldSwitch.marker"/>), <c>ICE_MELTED flag</c> and <c>ICE_REFROZEN name</c>.
    /// </summary>
    public sealed class IceThaw:WorldSwitch
    {
        protected override string Kind=>"ICE_THAW";
        public Renderer[] ice=System.Array.Empty<Renderer>();
        public Membrane[] melts=System.Array.Empty<Membrane>();
        /// <summary>Lights that warm with the ice (a rank's glow in its block, the rig's spill).</summary>
        public Light[] glow=System.Array.Empty<Light>();
        public Color cold=new(.62f,.82f,1f,.6f),warm=new(1f,.62f,.3f,.62f);
        [ColorUsage(false,true)] public Color coldGlow=new(.05f,.16f,.3f),warmGlow=new(1.4f,.55f,.12f);
        public Color coldLight=new(.5f,.75f,1f),warmLight=new(1f,.6f,.28f);
        public float coldIntensity=.6f,warmIntensity=2.4f;
        /// <summary>How long the ice takes to go amber, then how long what melts takes to go.</summary>
        public float seconds=2.6f,meltSeconds=1.6f;
        /// <summary>Once set, the ice goes blue again (live: over a few seconds; at load: at once).</summary>
        public string refreezeFlag;
        public string sound="water_rush";public float volume=.4f;
        /// <summary>0 blue, 1 amber.</summary>
        public float Heat{get;private set;}
        public bool Melted{get;private set;}
        public bool Refrozen{get;private set;}
        float warming=-1,melting=-1,cooling=-1;bool started;
        Vector3[] rest;MaterialPropertyBlock block;
        bool Set(string key)=>!string.IsNullOrEmpty(key)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        /// <summary>Thawing already, with no hand to start it: the Muster's rig is on it.</summary>
        bool Running=>string.IsNullOrEmpty(flag);
        void Start()
        {
            started=true;rest=new Vector3[melts.Length];
            for(int i=0;i<melts.Length;i++)if(melts[i]!=null)rest[i]=melts[i].transform.localScale;
            if(Running)Heat=1;
            if(Set(refreezeFlag)){Refrozen=true;Heat=0;}
            Paint();
        }
        protected override void Settle()
        {
            if(!started)Start();
            Heat=Refrozen?0:1;Melted=true;warming=melting=-1;
            foreach(var plug in melts)if(plug!=null)plug.gameObject.SetActive(false);
            Paint();
        }
        protected override void Work()
        {
            if(!started)Start();
            warming=0;
            AudioManager.PlayAt(sound,transform,transform.position+Vector3.up,volume,AudioBus.SFX,40);
            foreach(var plug in melts)if(plug!=null)CombatVfx.Burst(plug.transform.position+Vector3.up,new Color(1,.82f,.6f,.45f),"steam");
        }
        protected override void Update()
        {
            if(GameServices.Current==null)return;
            if(!Running)base.Update();
            if(!Refrozen&&Set(refreezeFlag)){Refrozen=true;cooling=0;}
            if(GameTime.Paused)return;
            float dt=Time.deltaTime;
            if(warming>=0)
            {
                warming+=dt;Heat=Mathf.SmoothStep(0,1,Mathf.Clamp01(warming/Mathf.Max(.01f,seconds)));Paint();
                if(warming>=seconds){warming=-1;melting=0;}
            }
            if(melting>=0)
            {
                melting+=dt;float t=Mathf.Clamp01(melting/Mathf.Max(.01f,meltSeconds));
                for(int i=0;i<melts.Length;i++)
                {
                    var plug=melts[i];if(plug==null||!plug.gameObject.activeSelf)continue;
                    // It slumps: shorter and a little wider, as soft ice goes; then the seal opens (its colliders and obstacle off, its body dissolved).
                    plug.transform.localScale=new Vector3(rest[i].x*(1+.12f*t),rest[i].y*Mathf.Lerp(1,.35f,t*t),rest[i].z*(1+.12f*t));
                    if(t>=1&&!plug.Open)plug.SetOpen(true);
                }
                if(t>=1){melting=-1;Melted=true;Debug.Log("ICE_MELTED "+(Running?name:flag));}
            }
            if(cooling>=0)
            {
                cooling+=dt;Heat=Mathf.Min(Heat,1-Mathf.SmoothStep(0,1,Mathf.Clamp01(cooling/4f)));Paint();
                if(cooling>=4){cooling=-1;Debug.Log("ICE_REFROZEN "+name);}
            }
        }
        void Paint()
        {
            block??=new MaterialPropertyBlock();
            var colour=Color.Lerp(cold,warm,Heat);var emission=Color.Lerp(coldGlow,warmGlow,Heat);
            foreach(var piece in ice)
            {
                if(piece==null)continue;
                piece.GetPropertyBlock(block);block.SetColor("_BaseColor",colour);block.SetColor("_EmissionColor",emission);piece.SetPropertyBlock(block);
            }
            foreach(var light in glow)
            {
                if(light==null)continue;
                light.color=Color.Lerp(coldLight,warmLight,Heat);light.intensity=Mathf.Lerp(coldIntensity,warmIntensity,Heat);
            }
        }
    }
}
