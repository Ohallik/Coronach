using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A swarm drawn as what it is: one enemy, many motes. Each mote keeps its own small orbit inside the cloud, and the cloud as a whole draws in tight through a tell, shivers
    /// when it is struck and flies apart as it bursts. It needs no model: a swarm-cloud definition with no prefab gets this as its body (ActorFactory). Every mote's place comes
    /// from its number and the clock, so nothing here draws on the random stream the fight's own rolls come off. Each mote is a bead of its tone with a dark edge, half a metre
    /// across, and the mass casts a soft shadow on the ground under it, so a swarm reads from the game's camera on snow, pale stone, water and a dark deck alike.
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
            var view=system.GetComponent<ParticleSystemRenderer>();view.sharedMaterial=Look();
            view.renderMode=ParticleSystemRenderMode.Billboard;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
            motes=new ParticleSystem.Particle[count];
            // Its shadow: a soft dark pool on the ground under the mass, so the cloud stands on the floor it hunts over (snow and pale stone above all).
            var pool=new GameObject("Swarm shadow",typeof(MeshFilter),typeof(MeshRenderer));pool.transform.SetParent(transform,false);shadow=pool.transform;
            pool.GetComponent<MeshFilter>().sharedMesh=Pool();var under=pool.GetComponent<MeshRenderer>();under.sharedMaterial=Shade();
            under.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;under.receiveShadows=false;pool.SetActive(false);
        }
        Transform shadow;
        static Mesh pool;static Material shade;
        /// <summary>A flat square on the ground, facing up, its corners white with the shadow's alpha in its texture.</summary>
        static Mesh Pool()
        {
            if(pool!=null)return pool;
            pool=new Mesh{name="swarm shadow",hideFlags=HideFlags.HideAndDontSave};
            pool.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};
            pool.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};pool.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            pool.triangles=new[]{0,1,2,0,2,3};pool.RecalculateNormals();pool.RecalculateBounds();return pool;
        }
        /// <summary>The shadow's look: the flare's own material (a variant the build carries) on a soft round dark texture made here. Drawn before the danger markings
        /// (TelegraphFill's queue less some), so a marking under a swarm is never darkened by it.</summary>
        static Material Shade()
        {
            if(shade!=null)return shade;
            var flare=Resources.Load<Material>("Effects/flare_01");if(flare==null)return null;
            const int N=64;var soft=new Texture2D(N,N,TextureFormat.RGBA32,true){name="swarm shadow (runtime)",wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color32[N*N];
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {float u=(x+.5f)/N*2-1,v=(y+.5f)/N*2-1,r=Mathf.Sqrt(u*u+v*v);pixels[y*N+x]=new Color32(8,8,12,(byte)Mathf.RoundToInt(255*.42f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.98f,r)))));}
            soft.SetPixels32(pixels);soft.Apply(true);
            shade=new Material(flare){name="swarm shadow (runtime)",hideFlags=HideFlags.HideAndDontSave};shade.SetTexture("_BaseMap",soft);shade.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-20;
            return shade;
        }
        static readonly RaycastHit[] below=new RaycastHit[8];
        /// <summary>Lays the shadow on the first ground under the cloud within reach, as wide as the cloud; none over a drop, open water or the void a flight zone flies over.</summary>
        void Lay(float wide,float fade)
        {
            if(shadow==null)return;
            int hits=Physics.RaycastNonAlloc(transform.position+Vector3.up*.3f,Vector3.down,below,4.5f,~0,QueryTriggerInteraction.Ignore);
            float nearest=float.MaxValue;var ground=default(RaycastHit);var root=transform.root;
            for(int i=0;i<hits;i++){var hit=below[i];if(hit.collider==null||hit.collider.transform.IsChildOf(root)||hit.distance>=nearest)continue;nearest=hit.distance;ground=hit;}
            bool on=nearest<float.MaxValue&&fade>.05f;if(shadow.gameObject.activeSelf!=on)shadow.gameObject.SetActive(on);if(!on)return;
            shadow.SetPositionAndRotation(ground.point+ground.normal*.05f,Quaternion.FromToRotation(Vector3.up,ground.normal));
            var parent=shadow.parent!=null?shadow.parent.lossyScale:Vector3.one;float across=Mathf.Max(1.2f,wide*2.4f)*fade;
            shadow.localScale=new Vector3(across/Mathf.Max(.01f,parent.x),1,across/Mathf.Max(.01f,parent.z));
        }
        static Material look;
        /// <summary>A mote: a bead of its tone with a darker edge, so it reads on pale stone and snow as on a dark deck. The motes were the shared flare, a spark whose bright core is
        /// under a hundredth of its square: a fifth of a metre each, two or three pixels at the game's framing, in the editor as in the player (the Fibre Wisp undrawn on the
        /// Skyfields' stone, s39/153; the Frost Motes flecks on snow, s35/101). Its texture is made here, so a build can't be without it; its material is the flare's own, so its
        /// shader and keywords are a variant the build carries.</summary>
        static Material Look()
        {
            if(look!=null)return look;
            var flare=Resources.Load<Material>("Effects/flare_01");if(flare==null)return null;
            const int N=64;var bead=new Texture2D(N,N,TextureFormat.RGBA32,true){name="swarm mote (runtime)",wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color32[N*N];
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {
                float u=(x+.5f)/N*2-1,v=(y+.5f)/N*2-1,r=Mathf.Sqrt(u*u+v*v);
                float core=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.58f,r)),edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,.97f,r));
                byte value=(byte)Mathf.RoundToInt(255*Mathf.Lerp(.16f,1,core)),alpha=(byte)Mathf.RoundToInt(255*Mathf.Max(core,.75f*edge));
                pixels[y*N+x]=new Color32(value,value,value,alpha);
            }
            bead.SetPixels32(pixels);bead.Apply(true);
            look=new Material(flare){name="swarm motes (runtime)",hideFlags=HideFlags.HideAndDontSave};look.SetTexture("_BaseMap",bead);return look;
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
                motes[i].position=at;motes[i].startSize=(.46f+.06f*Mathf.Sin(t*7+a))*fade;
                motes[i].startColor=new Color(tone.r,tone.g,tone.b,tone.a*fade);motes[i].startLifetime=motes[i].remainingLifetime=1000;
            }
            system.SetParticles(motes,count);
            Lay(wide,fade);
        }
    }
}
