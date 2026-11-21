using System;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// One of the Bridle's stays in the Glass Choir (chapter 7): a fibre of the old road drawn taut from a bronze seat to a piece of the glass body, holding it where the court wanted it (a rib pressed
    /// across the gallery, a fold of glass shut over a niche). The hand is a <see cref="DiscoveryPoint"/> at the seat ("Take the stay's weight"), which sets <see cref="WorldSwitch.flag"/>; this is
    /// the stay answering, in the release's own two beats. It <b>pulls</b>: the fibre goes hard white, the rib dips and the glass groans, and what the strain cracks cracks (<see cref="frees"/>).
    /// Then it <b>eases</b>: the light runs out of the fibre, the fibre lets go of the rib and drops slack and amber by its seat, and the rib swings back into the body, out of the way, taking
    /// <see cref="blocks"/> with it. A save with the flag loads eased, with nothing played. Standing at the seat, the view draws back over <see cref="shows"/>, as a pump's does over its water,
    /// so a player sees what a stay holds before taking its weight. Logs <c>STAY_EASED flag</c> (the press) and <c>STAY_SLACK flag</c> (the rib at rest).
    /// </summary>
    public sealed class BridleStay:WorldSwitch
    {
        static readonly Rect FramedIn=new(.06f,.18f,.88f,.74f);
        protected override string Kind=>"STAY_EASED";
        /// <summary>What the stay holds, turned about its own position (the rib's root in the body) from <see cref="held"/> to <see cref="eased"/> (world rotations).</summary>
        public Transform rib;public Quaternion held=Quaternion.identity,eased=Quaternion.identity;
        /// <summary>The fibre: drawn from <see cref="anchor"/> (its seat) to <see cref="grip"/> (a point on the rib, which moves with it) while taut, and lying slack along the floor from the seat
        /// toward <see cref="rest"/> once eased.</summary>
        public LineRenderer cord;public Transform anchor,grip,rest;
        /// <summary>The stay's light at its middle: white while it holds, gone to a low amber once it has let go.</summary>
        public Light glow;public float glowTaut=2.4f,glowSlack=.5f;
        public Color taut=new(.86f,.94f,1f),slack=new(.42f,.24f,.1f),pulse=Color.white;
        /// <summary>The two beats: how long it pulls, how long it takes the rib to settle; how many degrees the rib dips under the pull.</summary>
        public float pull=1.1f,ease=2.8f,dip=3;
        /// <summary>What the strain cracks as the stay pulls (glass that held something still): hidden with a burst of shards. What the rib stops while it is held: switched off once it is at rest.</summary>
        public GameObject[] frees=Array.Empty<GameObject>();public GameObject blocks;
        /// <summary>What a hero standing at the seat should see the stay change, as a world box. No size: the view stays where it is.</summary>
        public Bounds shows;public float showsFrom=2.5f;
        public string strain="root_groan",release="break_1";public float volume=.5f;
        /// <summary>A Clamp Tender's work, if the stay has keepers (the fibre gallery's): wanted once the stay has eased. While a tender works it the stay is drawn taut again over
        /// <see cref="closing"/> seconds, the rib sinking back across the way, which shuts once it is half down; stopped, the tender lets it ease again by itself. Nothing is saved: a load
        /// finds the stay as its flag left it.</summary>
        public Tended keeper;public float closing=4;
        const int Points=14;
        readonly Vector3[] line=new Vector3[Points];
        float working=-1,shut;CameraRig rig;bool slackLogged,tendedLogged;EnemyBrain setter;
        /// <summary>How far a tender has drawn the stay taut again, 0 to 1.</summary>
        public float Shut=>shut;
        /// <summary>Where the stay is between held (0) and at rest (1), for a test: the rib's turn.</summary>
        public float Eased{get;private set;}
        public bool Working=>working>=0;
        void Start(){if(!Worked)Pose(0,0,false);}
        protected override void Settle()
        {
            Pose(1,1,true);foreach(var glass in frees)if(glass!=null)glass.SetActive(false);if(blocks!=null)blocks.SetActive(false);slackLogged=true;
        }
        protected override void Work()
        {
            working=0;
            AudioManager.PlayAt(strain,transform,(anchor!=null?anchor.position:transform.position)+Vector3.up,volume,AudioBus.SFX,40,.06f,.82f);
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Tremor(pull*.8f,.05f);
            // The strain is what cracks the glass round whatever the stay held: it goes as the stay pulls, not as it lets go.
            foreach(var glass in frees)
            {
                if(glass==null||!glass.activeSelf)continue;
                var at=glass.transform.position;var drawn=glass.GetComponentInChildren<Renderer>();if(drawn!=null)at=drawn.bounds.center;
                CombatVfx.Burst(at,new Color(.8f,.95f,1f,.8f),"crack");CombatVfx.Burst(at,new Color(.86f,.96f,1f,.4f),"shape");glass.SetActive(false);
            }
        }
        /// <summary>The rib's turn (0 held, 1 eased, with the pull's dip below 0), the fibre's give (0 taut, 1 lying slack), and whether its light has gone.</summary>
        void Pose(float turn,float give,bool spent)
        {
            Eased=Mathf.Clamp01(turn);
            if(rib!=null)rib.rotation=turn<0?Quaternion.SlerpUnclamped(held,eased,turn):Quaternion.Slerp(held,eased,turn);
            if(cord!=null&&anchor!=null&&grip!=null)
            {
                Vector3 from=anchor.position,to=grip.position,lie=rest!=null?rest.position:from+(new Vector3(to.x-from.x,0,to.z-from.z).normalized*1.6f);
                for(int i=0;i<Points;i++)
                {
                    float t=i/(Points-1f);
                    // Taut: a straight line to the rib. Slack: lying along the floor from the seat, a little coiled at its end.
                    var straight=Vector3.Lerp(from,to,t);
                    var lying=Vector3.Lerp(from,lie,t)+new Vector3(Mathf.Sin(t*9)*.18f*t,.04f,Mathf.Cos(t*7)*.12f*t);
                    line[i]=Vector3.Lerp(straight,lying,Mathf.SmoothStep(0,1,give))+Vector3.down*Mathf.Sin(t*Mathf.PI)*give*(1-give)*1.2f;
                }
                cord.positionCount=Points;cord.useWorldSpace=true;cord.SetPositions(line);
            }
            if(glow!=null){glow.color=Color.Lerp(taut,new Color(1,.62f,.3f),spent?1:give);glow.intensity=spent?glowSlack:Mathf.Lerp(glowTaut,glowSlack,give);}
        }
        protected override void Update()
        {
            base.Update();
            float now=Time.time;
            if(working>=0&&!GameTime.Paused)
            {
                float before=working;working+=Time.deltaTime;
                if(working<pull)
                {
                    float p=working/pull;Pose(-Mathf.Sin(p*Mathf.PI)*dip/90f,0,false);
                    if(cord!=null)BridleHold.Pulse(cord,p*3,Color.Lerp(taut,pulse,p),pulse,1);
                }
                else
                {
                    if(before<pull)AudioManager.PlayAt(release,transform,(grip!=null?grip.position:transform.position),volume*.8f,AudioBus.SFX,40,.06f,1.6f);
                    float e=Mathf.Clamp01((working-pull)/ease);
                    Pose(Mathf.SmoothStep(0,1,e),Mathf.Clamp01(e*1.6f),false);
                    if(cord!=null)BridleHold.Pulse(cord,0,Color.Lerp(taut,slack,Mathf.Clamp01(e*2)),slack,0);
                    if(e>=1)
                    {
                        working=-1;Pose(1,1,true);if(blocks!=null)blocks.SetActive(false);
                        if(!slackLogged){slackLogged=true;Debug.Log("STAY_SLACK "+flag);}
                    }
                }
            }
            else if(!Worked&&cord!=null)BridleHold.Pulse(cord,now*.45f+transform.position.x*.07f,taut,pulse,.7f);
            else if(Worked&&working<0)Kept();
            Show();
        }
        /// <summary>At rest and eased: whether a tender is drawing it taut again, and the stay as far as it has got.</summary>
        void Kept()
        {
            if(keeper==null){if(cord!=null)BridleHold.Pulse(cord,0,slack,slack,0);return;}
            // Set taut again, it stays so while the tender that set it lives; stop that one and it eases again by itself.
            bool held=shut>=1&&setter!=null&&setter.Health!=null&&setter.Health.Alive;
            keeper.wanted=!held;
            bool tending=!held&&keeper.Tending&&!GameTime.Paused;
            if(tending&&!tendedLogged){tendedLogged=true;Debug.Log("STAY_TENDED "+flag);}
            if(!tending)tendedLogged=false;
            float before=shut;
            if(!held&&!GameTime.Paused)shut=tending?Mathf.Min(1,shut+Time.deltaTime/Mathf.Max(.1f,closing)):Mathf.Max(0,shut-Time.deltaTime/Mathf.Max(.1f,ease));
            if(shut>=1&&before<1){setter=keeper.By;keeper.Clear();Debug.Log("STAY_SET "+flag);}
            if(shut>0||before>0)
            {
                Pose(1-Mathf.SmoothStep(0,1,shut),1-shut,shut<.01f);
                if(blocks!=null)blocks.SetActive(shut>.5f);
                if(shut<=0&&before>0){setter=null;Debug.Log("STAY_SLACK "+flag);}
            }
            if(cord!=null)BridleHold.Pulse(cord,Time.time*.45f,Color.Lerp(slack,taut,shut),pulse,shut*.7f);
        }
        void Show()
        {
            if(shows.size.sqrMagnitude<=0||GameServices.Current==null||GameServices.Current.Input.Blocked)return;
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var at=party.Active.transform.position;
            if(working<0)
            {
                // Not yet eased: only for someone standing at its seat, and only while there is still a stay to ease.
                if(Worked||point!=null&&!point.Wanted)return;
                var offset=at-(point!=null?point.transform.position:transform.position);offset.y=0;
                if(offset.magnitude>(point!=null?point.range:0)+showsFrom)return;
            }
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig==null)return;
            var view=new Bounds(at+Vector3.up,Vector3.one*3);view.Encapsulate(shows);
            rig.FrameSceneInterest(this,view,FramedIn);
        }
    }
}
