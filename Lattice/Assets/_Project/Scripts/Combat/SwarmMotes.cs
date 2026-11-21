using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A swarm drawn as what it is: one enemy, many motes. Each mote keeps its own small orbit inside the cloud, and the cloud as a whole draws in tight through a tell, shivers
    /// when it is struck and flies apart as it bursts. It needs no model: a swarm-cloud definition with no prefab gets this as its body (ActorFactory). Every mote's place comes
    /// from its number and the clock, so nothing here draws on the random stream the fight's own rolls come off.
    /// </summary>
    public sealed class SwarmMotes:MonoBehaviour
    {
        public int count=18;
        public float radius=.9f;
        public Color tone=new(1.3f,1.05f,.4f,1);
        /// <summary>How wide the cloud is asked to stand, as a share of its radius: 1 at rest, well under for a tell, over for a latch round a hero.</summary>
        public float Gather=1;
        float spread=1,kick,gone=-1;
        ParticleSystem system;ParticleSystem.Particle[] motes;
        /// <summary>How wide it is drawn now: what a check reads.</summary>
        public float Spread=>spread;
        public bool Bursting=>gone>=0;
        public static GameObject Body(Transform root,EnemyDef definition,CharacterController capsule)
        {
            var go=new GameObject("Motes");go.transform.SetParent(root,false);go.transform.localPosition=capsule.center;
            var cloud=go.AddComponent<SwarmMotes>();cloud.radius=capsule.radius*1.6f;if(definition.tune.count>0)cloud.count=definition.tune.count;
            return go;
        }
        void Start()
        {
            var go=new GameObject("Swarm motes",typeof(ParticleSystem));go.transform.SetParent(transform,false);
            system=go.GetComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            main.maxParticles=count;main.simulationSpeed=0;main.startSpeed=0;
            var emission=system.emission;emission.enabled=false;var shape=system.shape;shape.enabled=false;
            var view=system.GetComponent<ParticleSystemRenderer>();view.sharedMaterial=Resources.Load<Material>("Effects/flare_01");
            view.renderMode=ParticleSystemRenderMode.Billboard;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
            motes=new ParticleSystem.Particle[count];
        }
        /// <summary>Struck: the cloud shivers apart and closes again.</summary>
        public void Jolt()=>kick=1;
        /// <summary>Broken: every mote flies out and goes dark.</summary>
        public void Burst(){if(gone<0)gone=0;}
        static float Part(float value)=>value-Mathf.Floor(value);
        void LateUpdate()
        {
            if(system==null||GameTime.Paused)return;
            float dt=Time.deltaTime,t=Time.time;
            spread=Mathf.MoveTowards(spread,Gather+kick*.7f,5*dt);kick=Mathf.MoveTowards(kick,0,3.5f*dt);
            if(gone>=0)gone=Mathf.Min(1,gone+dt/.45f);
            float wide=radius*(gone>=0?spread+3*gone:spread),fade=gone>=0?1-gone:1;
            for(int i=0;i<count;i++)
            {
                float a=i*2.399963f,pace=1.6f+1.9f*Part(i*.618f),reach=.4f+.6f*Part(i*.754f);
                var at=new Vector3(Mathf.Sin(t*pace+a),.7f*Mathf.Sin(t*pace*1.31f+a*1.7f),Mathf.Cos(t*pace*.87f+a*2.3f))*(wide*reach);
                motes[i].position=at;motes[i].startSize=(.2f+.08f*Mathf.Sin(t*7+a))*fade;
                motes[i].startColor=new Color(tone.r,tone.g,tone.b,tone.a*fade);motes[i].startLifetime=motes[i].remainingLifetime=1000;
            }
            system.SetParticles(motes,count);
        }
    }
}
