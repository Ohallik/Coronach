using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    // The Burrower is disabled equipment. Its powered seams wind down while
    // the existing supported collapse completes; gameplay resolution is owned
    // by Health/EnemyBrain and is never delayed by this presentation.
    [DefaultExecutionOrder(650)]
    public sealed class BurrowerShutdown : MonoBehaviour
    {
        struct Surface {public Renderer renderer;public Color emission;}
        Surface[] surfaces;
        Health health;
        bool shuttingDown;
        float started;
        MaterialPropertyBlock block;
        static readonly int Emission=Shader.PropertyToID("_EmissionColor");
        void Awake()
        {
            health=GetComponent<Health>();health.Died+=Begin;health.Revived+=RestorePower;
            block=new MaterialPropertyBlock();
            var visual=GetComponent<DefeatPresentation>().visual;
            var list=new System.Collections.Generic.List<Surface>();
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                var material=renderer.sharedMaterial;
                if(material!=null&&material.HasProperty(Emission))list.Add(new Surface{renderer=renderer,emission=material.GetVector(Emission)});
            }
            surfaces=list.ToArray();
        }
        void OnEnable(){if(health!=null&&health.Alive&&surfaces!=null)RestorePower(health);}
        void Begin(Health _,DamagePacket __){if(shuttingDown)return;shuttingDown=true;started=GameTime.Now;age=0;}
        /// <summary>D419: its glow on the frame it is beaten (a share of its own) and how long until it is out, in real time through the kill's stop (only a pause holds it). Over
        /// 1.15 s of the game's clock, which the kill's stop holds, its head still glowed cyan half a second on (nov25-r5-chain s06/045): every other beaten boss's lights are
        /// out on its end's frame (BodyGoesDark).</summary>
        public const float FirstLight=.4f,OutIn=.45f;
        float age;
        void RestorePower(Health _)
        {
            shuttingDown=false;started=0;
            foreach(var surface in surfaces)
            {
                if(surface.renderer==null)continue;
                surface.renderer.GetPropertyBlock(block);block.SetVector(Emission,surface.emission);surface.renderer.SetPropertyBlock(block);
            }
        }
        void OnDestroy(){if(health!=null){health.Died-=Begin;health.Revived-=RestorePower;}}
        void LateUpdate()
        {
            if(!shuttingDown)return;
            if(!GameTime.Paused)age+=Time.unscaledDeltaTime;
            // Its power drops at once and the rest goes out within half a second. No new flash, blast or
            // geometry obscures the supported boss finish or nearby heroes.
            float power=FirstLight*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(age/OutIn)));
            foreach(var surface in surfaces)
            {
                if(surface.renderer==null)continue;
                // Preserve the ground-boss occlusion ellipse in the same block.
                // HDR emission is already a shader-space vector. SetColor would
                // convert it again and brighten the rig even before shutdown.
                surface.renderer.GetPropertyBlock(block);
                var color=surface.emission*power;color.a=1;
                block.SetVector(Emission,color);surface.renderer.SetPropertyBlock(block);
            }
        }
    }
}
