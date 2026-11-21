using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the Continuance's anchor clamps on its hull's back (chapter 10, <c>BossController.MeretShip.cs</c>): a seat in the plating under the flight plane, and a clamp the director raises
    /// through the plane to reshape the berth's lanes. She marks it first (a ring at the seat, filling: the strike is the boss's), then it <b>rises</b>, stands solid at the plane (cover: her
    /// broadside's lanes and her tractor's line stop at it, and a hull flies round it) for as long as she holds it up, and <b>sinks</b> back into its seat. Nothing here strikes or moves a hero:
    /// whoever stood where it rises has been struck and set beside it by the boss first (D289), and a sinking clamp is let go of at once.
    /// <para>Drawn from her definition's <c>bodySegment</c> (the <c>AnchorClamp</c> model, Oriel's Bridle clamp in Compact white); hidden under the hull while it is down. A room stands its own
    /// at the hull's seats (docs/maps/Continuance.md, "Boss arena contract"); a room with none (the flight yard) gets three the boss makes where the room's contract stands them.</para>
    /// Logs <c>MERET_CLAMP_UP &lt;index&gt;</c>, <c>MERET_CLAMP_DOWN &lt;index&gt;</c>.
    /// </summary>
    public sealed class MeretClamp:MonoBehaviour
    {
        public static readonly List<MeretClamp> All=new();
        /// <summary>1 to 3, west to east as the room's contract numbers its seats.</summary>
        public int index;
        /// <summary>The boss that raises it, by EnemyDef id.</summary>
        public string settledBy="MeretShip";
        /// <summary>The flight plane it rises through (the boss's own height, set as it claims the clamp).</summary>
        public float plane=19;
        public static class Tune
        {
            /// <summary>How wide it stands solid at the plane, how far over the plane its drawn top stands (low enough that nothing much can hide behind it from the fixed camera), how long
            /// it takes to come up through the plane and to go down again.</summary>
            public const float Solid=3,TopOver=1.1f,RiseSeconds=.3f,SinkSeconds=1.1f,DrawnWidth=6.4f;
        }
        Transform drawn;CapsuleCollider solid;Renderer[] parts;float risen,goal,until=-1,drawnTall=4,drawnBase;bool claimed;
        /// <summary>Standing solid at the plane now.</summary>
        public bool Up=>solid!=null&&solid.enabled;
        /// <summary>How far up it is, 0 (in its seat) to 1 (risen).</summary>
        public float Risen=>risen;
        /// <summary>Its seat on the plan, at the plane.</summary>
        public Vector3 AtPlane{get{var p=transform.position;p.y=plane;return p;}}
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        void Awake()
        {
            solid=gameObject.AddComponent<CapsuleCollider>();solid.direction=1;solid.radius=Tune.Solid;solid.enabled=false;
        }
        /// <summary>The boss has woken: it draws the clamp from her definition (a stand-in ring if she has none) and its plane is hers.</summary>
        public void Claim(EnemyDef definition,float atPlane)
        {
            plane=atPlane;claimed=true;
            if(drawn==null)
            {
                var prefab=definition!=null?definition.bodySegment:null;
                if(prefab!=null){drawn=Instantiate(prefab,transform,false).transform;foreach(var c in drawn.GetComponentsInChildren<Collider>())Destroy(c);}
                else{var stand=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(stand.GetComponent<Collider>());stand.name="Clamp stand-in";drawn=stand.transform;drawn.SetParent(transform,false);}
                drawn.name="Clamp";parts=drawn.GetComponentsInChildren<Renderer>();
                // Stood on end in its seat, as wide as its contract says, its top a little over the plane once risen.
                var bounds=new Bounds(drawn.position,Vector3.zero);bool first=true;
                foreach(var r in parts){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
                float wide=Mathf.Max(.1f,Mathf.Max(bounds.size.x,bounds.size.z)),scale=Tune.DrawnWidth/wide;drawn.localScale*=scale;
                // How tall it is drawn and where its foot is, from its own origin: risen, its top stands just over the plane whatever the model's pivot.
                drawnTall=Mathf.Max(.5f,bounds.size.y*scale);drawnBase=(bounds.min.y-drawn.position.y)*scale;
            }
            Apply();
        }
        float Height=>Mathf.Max(1,plane+Tune.TopOver-transform.position.y);
        /// <summary>Up through the plane now (its marking has run and its strike has landed): solid at once.</summary>
        public void Rise(float stands)
        {
            goal=1;until=Time.time+Mathf.Max(.5f,stands);
            // Straight-sided for 3 m either side of the plane: its rounded ends anywhere near the plane, a ship rode up over one (a character controller climbs a slope).
            solid.height=Tune.Solid*2+6;solid.center=Vector3.up*(plane-transform.position.y);solid.enabled=true;Physics.SyncTransforms();
            AudioManager.PlayAt("impactMetal_000",transform,AtPlane,.3f,AudioBus.SFX,50,.05f,.55f);
            Debug.Log("MERET_CLAMP_UP "+index);
        }
        /// <summary>Back into its seat: let go of at once (nothing it held walls anyone in), drawn going down.</summary>
        public void Sink()
        {
            if(goal<=0&&!solid.enabled)return;
            goal=0;until=-1;solid.enabled=false;Physics.SyncTransforms();
            AudioManager.PlayAt("doorOpen_000",transform,AtPlane,.24f,AudioBus.SFX,50,.05f,.6f);
            Debug.Log("MERET_CLAMP_DOWN "+index);
        }
        void Update()
        {
            if(!claimed)return;
            if(until>=0&&Time.time>=until&&!GameTime.CombatHeld)Sink();
            // A clamp held up stands as long as the fight's clock runs: a conversation holds it with everything else.
            if(GameTime.CombatHeld&&until>=0)until+=Time.deltaTime;
            float rate=1/(goal>risen?Tune.RiseSeconds:Tune.SinkSeconds);
            risen=Mathf.MoveTowards(risen,goal,rate*Time.deltaTime);Apply();
        }
        void Apply()
        {
            if(drawn==null)return;
            // Risen: its top at the plane and a little over. Down: its top under its seat (and not drawn at all).
            float up=plane+Tune.TopOver-transform.position.y-drawnTall-drawnBase;drawn.localPosition=Vector3.up*(up-(1-risen)*(drawnTall+Mathf.Max(0,up)+.5f));
            bool shown=risen>.02f;foreach(var r in parts)if(r!=null)r.enabled=shown;
        }
        /// <summary>The clamps that answer to a boss, by index.</summary>
        public static List<MeretClamp> For(string boss)
        {
            var list=new List<MeretClamp>();foreach(var c in All)if(c!=null&&(string.IsNullOrEmpty(c.settledBy)||c.settledBy==boss))list.Add(c);
            list.Sort((a,b)=>a.index.CompareTo(b.index));return list;
        }
        /// <summary>For a room with none of its own (the flight yard): one made where the boss stands it, which goes when <paramref name="maker"/> does.</summary>
        public static MeretClamp Make(string boss,int index,Vector3 seat,Object maker)
        {
            var go=new GameObject("Anchor clamp "+index);go.SetActive(false);go.transform.position=seat;
            var clamp=go.AddComponent<MeretClamp>();clamp.index=index;clamp.settledBy=boss;clamp.maker=maker;clamp.made=maker!=null;go.SetActive(true);return clamp;
        }
        Object maker;bool made;
        void LateUpdate(){if(made&&maker==null)Destroy(gameObject);}
    }
}
