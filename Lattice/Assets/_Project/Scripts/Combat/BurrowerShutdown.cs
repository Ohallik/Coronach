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
        void Begin(Health _,DamagePacket __){if(shuttingDown)return;shuttingDown=true;started=GameTime.Now;}
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
            float elapsed=GameTime.Now-started;
            // A short power sag precedes the final fade. No new flash, blast or
            // geometry obscures the supported boss finish or nearby heroes.
            float power=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/1.15f));
            power*=1-.3f*Mathf.Sin(Mathf.Clamp01(elapsed/.28f)*Mathf.PI);
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
