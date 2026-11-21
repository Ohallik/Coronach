using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
using UnityEngine.AI;
namespace Lattice.World
{
    /// <summary>
    /// A body of water with a surface that can move: a river's shallows, a deep channel a sluice drains, a hall that floods, a boss's basin. One rule does the work: a body on foot whose feet are under the
    /// surface is wading, and wades at a share of its pace (heroes and enemies alike, through <see cref="Health.Hinder"/>; a ship overhead isn't in it). Where the water is comes from the ground itself, since
    /// only ground lower than the surface is under it, so <see cref="reaches"/> are loose rectangles over the channel and its banks and never have to follow a shoreline.
    /// Deep water is water with something in the way: <see cref="deep"/> holds solid, navigation-carving blockers that stand while the water does. Setting <see cref="drainFlag"/> takes the surface down to
    /// <see cref="drainedLevel"/> over <see cref="drainSeconds"/>, then removes the blockers (the navigation opens as a membrane's does: nothing is baked again) and shows <see cref="revealed"/>. The flag is in
    /// the save, so a drained channel loads drained, with no ceremony. Anything else that moves water (<see cref="RisingWater"/>, <see cref="FloodBasin"/>, a later chapter's pump) calls <see cref="MoveTo"/>.
    /// The surface's mesh carries each vertex's ground height in uv2.x; as the level moves, its depth (uv2.y) is written again, which is what the toon shader's water reads for its shore foam and its
    /// shallow-to-deep tint, so the shoreline is right at every level. A mesh without that channel is simply moved.
    /// </summary>
    public sealed class WaterVolume:MonoBehaviour
    {
        public static readonly List<WaterVolume> All=new();
        /// <summary>The surface's height in the world as built.</summary>
        public float level;
        /// <summary>Feet this far under the surface are wading, and at this share of their pace: ankle-deep is a splash, not a slog.</summary>
        public float wadeFrom=.12f,wadePace=.62f;
        /// <summary>What's drawn. It is moved to the level, and nothing else about it changes.</summary>
        public Transform surface;
        /// <summary>Unit squares on the ground plane (scaled and turned to fit) that the water lies within. None: nothing wades here.</summary>
        public Transform[] reaches=System.Array.Empty<Transform>();
        public string drainFlag;public float drainedLevel,drainSeconds=2.6f;
        /// <summary>What stops a walker while the water is deep: solid boxes with a carving NavMeshObstacle each (WaterKit.Deep hangs them under one holder that is a Membrane, so deep
        /// water is a seal like any other). Switched off once drained.</summary>
        public GameObject[] deep=System.Array.Empty<GameObject>();
        /// <summary>What the water leaves showing when it has gone down: a wet bed's puddles, a ford's stones.</summary>
        public GameObject[] revealed=System.Array.Empty<GameObject>();
        /// <summary>The ring a wader leaves. Unset, the owned circle is used.</summary>
        public Material ripple;
        public string splash="water_splash";
        public Color rippleTone=new(.86f,.96f,.92f,.5f);
        public float Level{get;private set;}
        public bool Drained{get;private set;}
        public bool Moving=>moveLeft>0;

        struct Wader{public float next;public Vector3 last;public int seen;}
        readonly Dictionary<Health,Wader> waders=new();readonly List<Health> gone=new();
        float moveFrom,moveTo,moveSeconds,moveLeft;bool draining;
        Mesh depthMesh;readonly List<Vector2> depths=new();float depthsFor=float.NaN;
        ParticleSystem rings;int frame;

        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        void Awake(){Level=level;}
        void Start()
        {
            bool drained=!string.IsNullOrEmpty(drainFlag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(drainFlag);
            if(drained){Level=drainedLevel;Drained=true;}
            Settle();Place();
        }
        /// <summary>The blockers and what the water uncovers, as the state says, at once.</summary>
        void Settle()
        {
            foreach(var block in deep)if(block!=null&&block.activeSelf==Drained)block.SetActive(!Drained);
            foreach(var shown in revealed)if(shown!=null&&shown.activeSelf!=Drained)shown.SetActive(Drained);
        }
        /// <summary>Whether a point is over this water's ground, at any height.</summary>
        public bool Over(Vector3 point)
        {
            foreach(var reach in reaches)
            {
                if(reach==null)continue;var local=reach.InverseTransformPoint(point);
                if(Mathf.Abs(local.x)<=.5f&&Mathf.Abs(local.z)<=.5f)return true;
            }
            return false;
        }
        /// <summary>How far under the surface a point is: negative above it, and far above for a point that isn't over this water at all.</summary>
        public float Depth(Vector3 point)=>Over(point)?Level-point.y:float.NegativeInfinity;
        /// <summary>The deepest any water stands over a point (negative: dry).</summary>
        public static float DepthAt(Vector3 point)
        {
            float deepest=float.NegativeInfinity;
            foreach(var water in All)if(water!=null)deepest=Mathf.Max(deepest,water.Depth(point));
            return deepest;
        }
        /// <summary>Takes the surface to a height over a time (zero: at once). Whoever calls last wins: a flood that is told to drain drains from where it stood.</summary>
        public void MoveTo(float height,float seconds=0)
        {
            draining=false;
            if(seconds<=0){moveLeft=0;Level=height;Place();return;}
            moveFrom=Level;moveTo=height;moveSeconds=moveLeft=seconds;
        }
        void Update()
        {
            if(GameServices.Current==null)return;
            if(!Drained&&!draining&&!string.IsNullOrEmpty(drainFlag)&&GameServices.Current.Flags.GetBool(drainFlag))
            {
                MoveTo(drainedLevel,drainSeconds);draining=true;Debug.Log("WATER_DRAINING "+drainFlag);
            }
            if(moveLeft>0&&!GameTime.Paused)
            {
                moveLeft=Mathf.Max(0,moveLeft-Time.deltaTime);
                Level=Mathf.Lerp(moveFrom,moveTo,Mathf.SmoothStep(0,1,1-moveLeft/moveSeconds));Place();
                if(moveLeft<=0&&draining){draining=false;Drained=true;Settle();Debug.Log("WATER_DRAINED "+drainFlag);}
            }
            Wade();
        }
        /// <summary>The surface where the level is, and its shore where the ground now meets it.</summary>
        void Place()
        {
            if(surface==null)return;
            var at=surface.position;if(!Mathf.Approximately(at.y,Level))surface.position=new Vector3(at.x,Level,at.z);
            if(Mathf.Approximately(Level,level)&&depthMesh==null||Mathf.Approximately(Level,depthsFor))return;
            if(depthMesh==null)
            {
                if(!Application.isPlaying||!surface.TryGetComponent<MeshFilter>(out var filter)||filter.sharedMesh==null)return;
                filter.sharedMesh.GetUVs(1,depths);if(depths.Count!=filter.sharedMesh.vertexCount){depths.Clear();return;}
                // Its own copy: the saved mesh is the water at rest, for every load.
                depthMesh=filter.mesh;
            }
            if(depths.Count==0)return;
            for(int i=0;i<depths.Count;i++)depths[i]=new Vector2(depths[i].x,Level-depths[i].x);
            depthMesh.SetUVs(1,depths);depthsFor=Level;
        }
        void Wade()
        {
            var zone=ZoneController.Current;if(zone!=null&&zone.Flight||reaches.Length==0||GameTime.Paused)return;
            frame++;
            foreach(var body in Health.All)
            {
                if(body==null||!body.Alive)continue;
                var at=body.transform.position;if(Depth(at)<wadeFrom)continue;
                if(body.TryGetComponent<CombatActor>(out var actor)&&actor.flight)continue;
                // What hovers is over the water, not in it: a cloud of flies crosses a channel as it crosses a floor.
                if(body.TryGetComponent<EnemyBrain>(out var brain)&&brain.definition!=null&&brain.definition.motion==Lattice.Data.EnemyMotion.Hover)continue;
                body.Hinder(wadePace);
                bool entered=!waders.TryGetValue(body,out var wader);
                if(entered)
                {
                    wader=new Wader{last=at};
                    AudioManager.PlayAt(splash,body.transform,at,body.friendly?.3f:.2f,AudioBus.SFX,90,.12f,Random.Range(.92f,1.08f));
                    Ring(at,1.7f);Ring(at,1.1f);
                }
                else if(Time.time>=wader.next)
                {
                    // A wader on the move trails rings; one standing still breathes one out now and then.
                    bool moving=(at-wader.last).sqrMagnitude>.04f;wader.last=at;
                    wader.next=Time.time+(moving?.3f:1.3f);Ring(at,moving?.9f:.7f);
                }
                wader.seen=frame;waders[body]=wader;
            }
            foreach(var pair in waders)if(pair.Value.seen!=frame)gone.Add(pair.Key);
            foreach(var body in gone)waders.Remove(body);gone.Clear();
        }
        void Ring(Vector3 at,float size)
        {
            if(rings==null)
            {
                var material=ripple!=null?ripple:Resources.Load<Material>("Effects/circle_02");if(material==null)return;
                var go=new GameObject("Wader rings",typeof(ParticleSystem));go.transform.SetParent(transform,false);
                rings=go.GetComponent<ParticleSystem>();rings.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=rings.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=96;main.startSpeed=0;main.startLifetime=1.1f;
                var emission=rings.emission;emission.enabled=false;var shape=rings.shape;shape.enabled=false;
                var grow=rings.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(1,1)));
                var fade=rings.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
                gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
                var view=go.GetComponent<ParticleSystemRenderer>();view.sharedMaterial=material;view.renderMode=ParticleSystemRenderMode.HorizontalBillboard;
                view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
                rings.Play();
            }
            rings.Emit(new ParticleSystem.EmitParams{position=new Vector3(at.x,Level+.03f,at.z),startSize=size*2.2f,startColor=rippleTone,startLifetime=1.1f},1);
        }
    }
}
