using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// One marked debris track of Nacre Grave (chapter 9; rule: a wreck field with a spiral; docs/maps/NacreGrave.md). The field turns round the broken berth at <see cref="centre"/>, and a
    /// track is a line on the plane that wrecks ride round it, quicker the further in: a piece's speed at a point is <see cref="speed"/> times (<see cref="refRadius"/> over the point's
    /// distance from the centre) to the power <see cref="steep"/>, held between <see cref="minSpeed"/> and <see cref="maxSpeed"/>. Everything a player reads is worked out from where the
    /// pieces are, every frame, by one rule, the same for the builder's still and a frame of play:
    /// <list type="bullet">
    /// <item>The track's lamps <b>stand amber</b> over the stretch each piece will cover in the next <see cref="lead"/> seconds (so a quicker piece throws a longer amber ahead of it), and
    /// everywhere else <b>run cold</b>: a chase of light travelling the way the field turns, at its speed there, so it runs visibly quicker inward.</item>
    /// <item>With the lead ship near, every stretch of the track under a piece's amber is <b>marked on the plane</b> as a danger, stretch by stretch along the track (fixed stretches, so a
    /// bend is marked as it bends), and a hero ship a piece touches inside a marking that has stood half a second loses <see cref="strike"/> of its Integrity before its frame's plating
    /// (never the last of it) and is thrown clear off the track. It is the place's damage: no enemy is its source.</item>
    /// </list>
    /// A <see cref="closed"/> track is a loop. An open one brings each piece up out of the field at its start and takes it down into the field at its end (<see cref="sink"/> metres under the
    /// plane over <see cref="sinkLength"/>), so a piece is only ever met at the plane. Logs <c>WRECK_STRUCK hero track</c>.
    /// </summary>
    public sealed class WreckTrack:MonoBehaviour
    {
        static readonly Color Amber=new(1f,.46f,.08f,1),Cold=new(.5f,.82f,1f,1);
        static readonly int BaseMapId=Shader.PropertyToID("_BaseMap"),EmissionMapId=Shader.PropertyToID("_EmissionMap");

        public string trackName="";
        /// <summary>The track on the plan, in the order the wrecks go, as world points (their y is ignored: the plane's).</summary>
        public Vector3[] path=Array.Empty<Vector3>();public bool closed;
        public Vector2 centre;public float speed=4,refRadius=100,steep=1,minSpeed=2,maxSpeed=16;
        /// <summary>A piece's reach from its middle, the seconds of its travel its amber covers ahead of it, the plane, and how an open track's ends go down into the field.</summary>
        public float pieceRadius=3.5f,lead=2.5f,plane=1,sink=7,sinkLength=16;
        /// <summary>Seconds into the traverse the first piece is at when the zone opens; the rest follow evenly spaced in time.</summary>
        public float phase;
        /// <summary>The pieces as drawn: each faces the way it goes; a child named "Body" tumbles slowly at <see cref="spin"/> degrees a second.</summary>
        public Transform[] pieces=Array.Empty<Transform>();public float spin=8;
        /// <summary>The lamps: one mesh whose lamp k has its uv's x at (k + .5) / <see cref="lampCount"/>, standing at arc length <see cref="lampFrom"/> + k x <see cref="lampSpacing"/>.</summary>
        public Renderer lampStrip;public float lampFrom,lampSpacing=2.5f;public int lampCount;
        /// <summary>A piece's blow (a share of a hull's Integrity before plating) and how far it throws the hull; how near the lead must be for a stretch to be marked; a stretch's length and width.</summary>
        public float strike=.1f,throwClear=6,markRange=55,markStep=6,markWidth;

        float[] along;float total;float[] timeAt;float traverse;float started;
        Texture2D lamps;Color32[] lampColours;
        readonly Dictionary<int,(Telegraph mark,float at)> marked=new();
        readonly List<(CombatActor hero,Vector3 way,float left)> throws=new();
        readonly HashSet<(int hero,int piece)> struck=new();

        // ---------------------------------------------------------------- the track's arithmetic
        public float Total{get{Prepare();return total;}}
        /// <summary>Seconds a piece takes over the whole track (one loop of a closed one).</summary>
        public float Traverse{get{Prepare();return traverse;}}
        public float Clock=>GameTime.Now-started;
        public int Marked=>marked.Count;
        void Prepare()
        {
            if(along!=null&&along.Length==path.Length+(closed?1:0))return;
            int n=path.Length+(closed&&path.Length>1?1:0);along=new float[Mathf.Max(1,n)];
            for(int i=1;i<n;i++)along[i]=along[i-1]+Flat(path[i%path.Length]-path[i-1]).magnitude;
            total=along[along.Length-1];
            // The time a piece takes to reach each metre of the track, so where every piece is at any time is one look-up and never drifts.
            int steps=Mathf.Max(2,Mathf.CeilToInt(total)+1);timeAt=new float[steps];
            for(int k=1;k<steps;k++){float a=total*(k-1)/(steps-1),b=total*k/(steps-1);timeAt[k]=timeAt[k-1]+(b-a)/SpeedAt((a+b)*.5f);}
            traverse=timeAt[steps-1];
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        /// <summary>The point of the track at this arc length, at the plane.</summary>
        public Vector3 PointAt(float arc)
        {
            Prepare();if(path.Length==0)return transform.position;
            arc=closed?Mathf.Repeat(arc,total):Mathf.Clamp(arc,0,total);
            int i=Array.BinarySearch(along,arc);if(i<0)i=~i-1;i=Mathf.Clamp(i,0,along.Length-2);
            float t=(arc-along[i])/Mathf.Max(.0001f,along[i+1]-along[i]);var p=Vector3.Lerp(path[i%path.Length],path[(i+1)%path.Length],t);p.y=plane;return p;
        }
        /// <summary>The way along the track, the way the wrecks go, at this arc length.</summary>
        public Vector3 TangentAt(float arc){var a=PointAt(arc-1);var b=PointAt(arc+1);var d=Flat(b-a);return d.sqrMagnitude<1e-6f?Vector3.forward:d.normalized;}
        public float RadiusAt(float arc){var p=PointAt(arc);return Vector2.Distance(new Vector2(p.x,p.z),centre);}
        /// <summary>How quickly the field turns here: quicker inward.</summary>
        public float SpeedAt(float arc)=>Mathf.Clamp(speed*Mathf.Pow(refRadius/Mathf.Max(1,RadiusAt(arc)),steep),minSpeed,maxSpeed);
        /// <summary>The seconds from the track's start to this arc length.</summary>
        public float TimeTo(float arc){Prepare();float x=Mathf.Clamp(arc,0,total)/Mathf.Max(.0001f,total)*(timeAt.Length-1);int i=Mathf.Min((int)x,timeAt.Length-2);return Mathf.Lerp(timeAt[i],timeAt[i+1],x-i);}
        /// <summary>The arc length a piece has reached this many seconds after the track's start.</summary>
        public float ArcAfter(float seconds)
        {
            Prepare();float t=closed?Mathf.Repeat(seconds,traverse):Mathf.Clamp(seconds,0,traverse);
            int i=Array.BinarySearch(timeAt,t);if(i<0)i=~i-1;i=Mathf.Clamp(i,0,timeAt.Length-2);
            float f=(t-timeAt[i])/Mathf.Max(.0001f,timeAt[i+1]-timeAt[i]);return total*(i+f)/(timeAt.Length-1);
        }
        /// <summary>Where piece <paramref name="index"/> is along the track at this clock time: the pieces are spaced evenly in time.</summary>
        public float ArcOf(int index,float time){Prepare();int count=Mathf.Max(1,pieces.Length);return ArcAfter(Mathf.Repeat(time+phase+index*traverse/count,traverse));}
        /// <summary>How far under the plane an open track has a piece at this arc length (nought on a closed one, and along its middle).</summary>
        public float Depth(float arc){if(closed)return 0;Prepare();float edge=Mathf.Min(arc,total-arc);return sink*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/Mathf.Max(.01f,sinkLength))));}
        public bool AtPlane(float arc)=>Depth(arc)<.5f;
        public Vector3 PieceAt(int index,float time){float a=ArcOf(index,time);var p=PointAt(a);p.y=plane-Depth(a);return p;}
        /// <summary>How far ahead of a piece a point of the track lies, the way the wrecks go (a closed track the shorter way round).</summary>
        public float Ahead(float arc,float pieceArc){Prepare();float d=arc-pieceArc;return closed?Mathf.Repeat(d+total*.5f,total)-total*.5f:d;}
        /// <summary>The far end of a piece's amber: where it will be in <see cref="lead"/> seconds, plus its own reach.</summary>
        public float ReachAhead(float pieceArc)
        {
            float t=TimeTo(closed?Mathf.Repeat(pieceArc,Total):pieceArc)+lead;float to=closed?ArcAfter(Mathf.Repeat(t,Traverse)):ArcAfter(t);
            float ahead=closed?Mathf.Repeat(to-pieceArc,Total):to-pieceArc;return ahead+pieceRadius;
        }
        /// <summary>Whether this arc length is under a piece's amber at this clock time: what a lamp shows, and what is marked.</summary>
        public bool AmberAt(float arc,float time)
        {
            for(int i=0;i<pieces.Length;i++){float p=ArcOf(i,time);if(!AtPlane(p)&&!AtPlane(arc))continue;float ahead=Ahead(arc,p);if(ahead>=-pieceRadius&&ahead<=ReachAhead(p))return true;}
            return false;
        }
        /// <summary>The colour of lamp k at this clock time.</summary>
        public Color32 Lamp(int k,float time)
        {
            float arc=lampFrom+k*lampSpacing;
            if(AmberAt(arc,time))return Amber;
            // The chase: lit points spaced a second and a fifth apart in the field's own time, travelling with it half again as quick, so outward they crowd and creep and inward
            // they stretch and race.
            float run=Mathf.Repeat((TimeTo(arc)-1.5f*time)/1.2f,1),bright=.14f+.86f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(run*Mathf.PI*2)),10);
            return Cold*bright;
        }
        /// <summary>Lays every piece where it is at this clock time: what the builder does for a still, and what play does every frame.</summary>
        public void Pose(float time)
        {
            for(int i=0;i<pieces.Length;i++)
            {
                if(pieces[i]==null)continue;float a=ArcOf(i,time);var p=PointAt(a);p.y=plane-Depth(a);
                pieces[i].SetPositionAndRotation(p,Quaternion.LookRotation(TangentAt(a)));
                var body=pieces[i].Find("Body");if(body!=null)body.localRotation=Quaternion.AngleAxis(time*spin+i*47,new Vector3(.3f,1,.2f).normalized);
            }
        }

        // ---------------------------------------------------------------- play
        void Start()
        {
            started=GameTime.Now;Prepare();
            if(lampStrip!=null&&lampCount>0)
            {
                lamps=new Texture2D(lampCount,1,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name=trackName+" lamps"};lampColours=new Color32[lampCount];
                var material=lampStrip.material;material.SetTexture(BaseMapId,lamps);material.SetTexture(EmissionMapId,lamps);
            }
            Pose(0);
        }
        void OnDisable(){foreach(var (mark,_) in marked.Values)if(mark!=null)mark.Clear();marked.Clear();}
        void OnDestroy(){if(lamps!=null)Destroy(lamps);}
        void Update()
        {
            float time=Clock;Pose(time);
            if(lamps!=null){for(int k=0;k<lampCount;k++)lampColours[k]=Lamp(k,time);lamps.SetPixels32(lampColours);lamps.Apply(false);}
            Throw();
            var party=PartyController.Current;var lead=party!=null?party.Active:null;
            if(lead==null||!lead.flight){Unmark();return;}
            Mark(time,lead);Strike(time,party);
        }
        void Unmark(){foreach(var (mark,_) in marked.Values)if(mark!=null)mark.Release();marked.Clear();}
        /// <summary>The danger on the plane: each fixed stretch of the track near the lead that a piece's amber lies over.</summary>
        void Mark(float time,CombatActor lead)
        {
            int stretches=Mathf.CeilToInt(Total/markStep);float width=markWidth>0?markWidth:pieceRadius*2;var here=lead.transform.position;
            for(int j=0;j<stretches;j++)
            {
                float a=j*markStep,b=Mathf.Min(Total,a+markStep);var from=PointAt(a);var to=PointAt(b);
                bool near=Flat((from+to)*.5f-here).sqrMagnitude<markRange*markRange&&AtPlane(a)&&AtPlane(b);
                bool hot=near&&(AmberAt(a,time)||AmberAt(b,time)||AmberAt((a+b)*.5f,time));
                if(hot&&!marked.ContainsKey(j))
                {
                    var run=Flat(to-from);if(run.sqrMagnitude<.01f)continue;
                    // Each stretch a little over its own length, so the stretches of a bend overlap and nothing between them is unmarked.
                    var mark=Telegraph.Line(from-run.normalized*.4f,run,run.magnitude+.8f,width,1,Telegraph.Threat,false,Total+.8f);mark.Steady=true;mark.owner=this;
                    marked[j]=(mark,GameTime.Now);
                }
                else if(!hot&&marked.TryGetValue(j,out var gone)){if(gone.mark!=null)gone.mark.Release();marked.Remove(j);}
            }
        }
        void Strike(float time,PartyController party)
        {
            if(GameServices.Current==null||GameServices.Current.Input.Blocked)return;
            for(int k=0;k<party.members.Length;k++)
            {
                var hero=party.members[k];if(hero==null||!hero.flight||!hero.Health.Alive)continue;
                for(int i=0;i<pieces.Length;i++)
                {
                    float a=ArcOf(i,time);if(!AtPlane(a))continue;
                    var gap=Flat(hero.transform.position-PointAt(a));float reach=pieceRadius+HeroCollision.HullRadius(hero.character)*.8f;
                    if(gap.magnitude>reach+6){struck.Remove((k,i));continue;}
                    if(gap.magnitude>reach||struck.Contains((k,i))||GameTime.Now<hero.Health.InvulnerableUntil||!InsideAStoodMarking(hero.transform.position))continue;
                    struck.Add((k,i));
                    float share=Mathf.Min(hero.Health.maximum*strike,Mathf.Max(0,hero.Health.integrity-1));
                    if(share>0)hero.Health.Receive(new DamagePacket{amount=share,type=DamageType.Kinetic,tag="wreck"});
                    // Off the track on the side the hull is on, a little the wrecks' way: out of the marking, never back under the piece.
                    var tangent=TangentAt(a);var side=Vector3.Cross(Vector3.up,tangent);float sign=Mathf.Sign(Vector3.Dot(gap,side));if(sign==0)sign=1;
                    if(hero.motor is FlightMotor ship)ship.Halt();
                    throws.Add((hero,(side*sign+tangent*.35f).normalized,throwClear));
                    Debug.Log("WRECK_STRUCK "+hero.character+" "+trackName);
                }
            }
        }
        bool InsideAStoodMarking(Vector3 point){foreach(var (mark,at) in marked.Values)if(mark!=null&&GameTime.Now-at>=.5f&&mark.Contains(point))return true;return false;}
        /// <summary>A struck hull goes off the track over a fifth of a second, along the plane, stopped by anything solid as a flown hull is.</summary>
        void Throw()
        {
            if(GameTime.Paused)return;
            for(int i=throws.Count-1;i>=0;i--)
            {
                var (hero,way,left)=throws[i];
                if(hero==null||!hero.TryGetComponent<CharacterController>(out var body)||!body.enabled){throws.RemoveAt(i);continue;}
                float step=Mathf.Min(left,throwClear/.2f*Time.deltaTime);body.Move(way*step+Vector3.up*(plane-hero.transform.position.y));
                left-=step;if(left<=.001f)throws.RemoveAt(i);else throws[i]=(hero,way,left);
            }
        }
    }
}
