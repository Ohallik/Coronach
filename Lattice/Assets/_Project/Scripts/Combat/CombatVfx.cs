using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class CombatVfx:MonoBehaviour
    {
        static CombatVfx current;
        readonly List<ParticleSystem> pool=new();
        Material flare,circle,slash;
        void Awake()
        {
            current=this;flare=Resources.Load<Material>("Effects/flare_01");circle=Resources.Load<Material>("Effects/circle_02");slash=Resources.Load<Material>("Effects/slash_01");
            Health.DamageNumber+=OnHit;
        }
        void OnDestroy(){Health.DamageNumber-=OnHit;if(current==this)current=null;}
        void OnHit(Health victim,float amount,bool weak){if(amount>0)Burst(victim.transform.position+Vector3.up*.9f,weak?new Color(1,.6f,.15f):Color.cyan,"hit");}
        public static void Burst(Vector3 position,Color color,string kind)
        {
            if(current==null||current.flare==null)return;current.Emit(position,color,kind);
        }
        void Emit(Vector3 position,Color color,string kind)
        {
            ParticleSystem ps=null;foreach(var candidate in pool)if(!candidate.IsAlive()){ps=candidate;break;}
            if(ps==null)
            {
                if(pool.Count>=32)return;
                var go=new GameObject("Lattice particles",typeof(ParticleSystem));go.transform.SetParent(transform);
                ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);pool.Add(ps);
                var main=ps.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=64;main.gravityModifier=0;
                var emission=ps.emission;emission.enabled=false;
                var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.18f;
                var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
            }
            ps.transform.position=position;var settings=ps.main;
            settings.startLifetime=kind=="shape"?.6f:kind=="kill"?.45f:.24f;
            settings.startSpeed=kind=="kill"?6:kind=="shape"?3:kind=="boost"?1:3;
            settings.startSize=kind=="shape"?.45f:kind=="lunge"?.7f:.3f;settings.startColor=color;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=kind=="lunge"?slash:kind=="shape"?circle:flare;
            ps.Play();ps.Emit(kind=="kill"?30:kind=="shape"?40:kind=="boost"?3:10);
        }
    }
}
