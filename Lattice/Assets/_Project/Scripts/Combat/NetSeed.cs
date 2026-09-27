using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    public sealed class NetSeed:MonoBehaviour
    {
        Vector3 origin,destination;DamagePacket packet;float age;
        LineRenderer trail;Material material;
        public static NetSeed Throw(Vector3 origin,Vector3 destination,DamagePacket packet)
        {
            var go=new GameObject("Static Net seed",typeof(NetSeed));go.transform.position=origin;
            var seed=go.GetComponent<NetSeed>();seed.origin=origin;seed.destination=CombatCover.Ground(destination+Vector3.up*2)+Vector3.up*.12f;seed.packet=packet;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.startSpeed=0;main.startSize=.35f;main.startColor=Color.cyan;main.maxParticles=1;main.simulationSpeed=0;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("Effects/flare_01");
            ps.SetParticles(new[]{new ParticleSystem.Particle{position=Vector3.zero,startSize=.38f,startColor=new Color(.3f,1.5f,2),startLifetime=1000,remainingLifetime=1000}},1);
            seed.trail=go.AddComponent<LineRenderer>();seed.trail.positionCount=2;seed.trail.useWorldSpace=true;
            seed.trail.startWidth=.04f;seed.trail.endWidth=.2f;seed.trail.numCapVertices=4;
            seed.material=new Material(Resources.Load<Material>("Effects/flare_01"));seed.material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            seed.trail.sharedMaterial=seed.material;seed.trail.startColor=new Color(.1f,.6f,1,.3f);seed.trail.endColor=new Color(.7f,1.5f,2,1);
            seed.trail.SetPosition(0,origin-(destination-origin).normalized*.3f);seed.trail.SetPosition(1,origin);
            seed.trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return seed;
        }
        void Update()
        {
            if(GameTime.Paused)return;
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/.34f);
            var next=Vector3.Lerp(origin,destination,t)+Vector3.up*(.8f*4*t*(1-t));
            bool blocked=CombatCover.Sweep(transform.position,next,.12f,out var hit);
            if(blocked)next=transform.position+(next-transform.position).normalized*Mathf.Max(0,hit.distance-.025f);
            var heading=(next-transform.position).normalized;transform.position=next;
            trail.SetPosition(0,next-heading*.65f);trail.SetPosition(1,next);
            if(!blocked&&t<1)return;
            Land(next,packet);
            Destroy(gameObject);
        }
        public static void Land(Vector3 position,DamagePacket packet)
        {
            var field=new GameObject("StaticNet",typeof(PulseField)).GetComponent<PulseField>();
            field.transform.position=CombatCover.Ground(position);field.packet=packet;
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
