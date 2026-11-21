using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class CombatVfx:MonoBehaviour
    {
        static CombatVfx current;
        readonly List<ParticleSystem> pool=new();
        Material flare,slash;
        void Awake()
        {
            current=this;flare=Resources.Load<Material>("Effects/flare_01");slash=Resources.Load<Material>("Effects/slash_01");
            Health.Impacted+=OnHit;Lattice.Core.GameTime.PauseChanged+=OnPause;
        }
        void OnDestroy(){Health.Impacted-=OnHit;Lattice.Core.GameTime.PauseChanged-=OnPause;if(current==this)current=null;}
        /// <summary>A hit's sparks run on real time, so they fly on through the hit-stop they belong to. A pause still holds them, as it holds everything else in the air.</summary>
        void OnPause(bool paused)
        {
            foreach(var ps in pool)
            {
                if(ps==null||!ps.main.useUnscaledTime)continue;
                if(paused)ps.Pause();else if(ps.isPaused)ps.Play();
            }
        }
        /// <summary>What a hit throws off a hero. It was the cyan of the party's own hits on everything else, so taking damage looked like dealing it.</summary>
        public static readonly Color Hurt=new(1.5f,.4f,.2f);
        public static Color Tint(HitKind kind)=>kind==HitKind.Weak?new Color(1,.6f,.15f):kind==HitKind.WeakSpot?new Color(1,.9f,.4f):kind==HitKind.Resisted?new Color(.6f,.62f,.66f):Color.cyan;
        void OnHit(Health victim,DamagePacket packet,float amount,HitKind kind,bool broke,bool killed)
        {
            if(amount<=0)return;
            // A hit somebody swung throws its sparks off the side it struck, by its weight (Impact). This is every other hit: a wall, a field's tick, a fixture's packet.
            if(Impact.Answers(packet))return;
            Burst(victim.transform.position+Vector3.up*.9f,victim.friendly?Hurt:Tint(kind),kind==HitKind.Resisted?"spark":"hit");
        }
        /// <summary>Sparks off a struck body: a cone thrown along the blow from the side it landed on, not a ball in the middle of the chest.</summary>
        /// <param name="gravity">Above 0 they fall: bits of what was hit, not light.</param>
        public static void Spark(Vector3 point,Vector3 along,Color color,float size,int count,float speed,float spread=26,float life=.22f,float gravity=0)
        {
            if(current==null||current.flare==null||count<=0)return;
            var ps=current.Take();if(ps==null)return;
            ps.transform.SetPositionAndRotation(point,Quaternion.LookRotation(along.sqrMagnitude>.0001f?along:Vector3.up));
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=spread;shape.radius=.05f;
            var settings=ps.main;settings.useUnscaledTime=true;settings.gravityModifier=gravity;settings.startColor=color;
            settings.startLifetime=new ParticleSystem.MinMaxCurve(life*.55f,life);settings.startSpeed=new ParticleSystem.MinMaxCurve(speed*.4f,speed);
            settings.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=current.flare;
            ps.Play();ps.Emit(Mathf.Min(count,64));
            if(Lattice.Core.GameTime.Paused)ps.Pause();
        }
        public static void Burst(Vector3 position,Color color,string kind)
        {
            if(current==null||current.flare==null)return;current.Emit(position,color,kind);
        }
        ParticleSystem Take()
        {
            ParticleSystem ps=null;foreach(var candidate in pool)if(!candidate.IsAlive()){ps=candidate;break;}
            if(ps==null)
            {
                if(pool.Count>=32)return null;
                var go=new GameObject("Lattice particles",typeof(ParticleSystem));go.transform.SetParent(transform);
                ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);pool.Add(ps);
                var main=ps.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=64;main.gravityModifier=0;
                var emission=ps.emission;emission.enabled=false;
                var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.18f;
                var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
            }
            return ps;
        }
        void Emit(Vector3 position,Color color,string kind)
        {
            var ps=Take();if(ps==null)return;
            // A pooled system may last have been a spark's cone on its own clock.
            ps.transform.SetPositionAndRotation(position,Quaternion.identity);var settings=ps.main;settings.useUnscaledTime=false;settings.gravityModifier=0;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.18f;
            // "flash" is the bright star of a crit or a perfect guard, "crack" the ring of shards a break throws.
            settings.startLifetime=kind=="steam"?.95f:kind=="spark"?.16f:kind=="failure"?.28f:kind=="form"?.13f:kind=="shape"?.6f:kind=="kill"?.45f:kind=="flash"?.12f:kind=="crack"?.36f:.24f;
            settings.startSpeed=kind=="steam"?1.1f:kind=="failure"?.6f:kind=="form"?1:kind=="kill"?6:kind=="shape"?1.8f:kind=="boost"?1:kind=="flash"?.4f:kind=="crack"?8:3;
            settings.startSize=kind=="steam"?1.25f:kind=="spark"?.12f:kind=="failure"?.09f:kind=="form"?2.4f:kind=="shape"?.8f:kind=="lunge"?.7f:kind=="flash"?1.5f:kind=="crack"?.2f:.3f;settings.startColor=color;
            // A shape's burst is a soft puff. Forty thin outline rings looked like debug gizmos, and they hung round the dying Bellows.
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=kind=="lunge"?slash:flare;
            ps.Play();ps.Emit(kind=="steam"?14:kind=="spark"?5:kind=="failure"?6:kind=="form"?3:kind=="kill"?30:kind=="shape"?14:kind=="boost"?3:kind=="flash"?2:kind=="crack"?24:10);
        }
    }
}
