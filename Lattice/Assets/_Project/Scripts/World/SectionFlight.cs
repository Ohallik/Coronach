using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// One of Cinder Halo's districts flying out through the Broken Ring (chapter 9; rule: keep beside a district, its lit water is calm and its bulk is cover; docs/maps/BrokenRing.md).
    /// On the district's root, which carries its drawn body, its cover (solid at the plane in the shape it is drawn there, kinematic: <see cref="body"/>) and the contract's <c>RingDistrict</c>.
    /// <list type="bullet">
    /// <item><b>Lifting.</b> Unless it is <see cref="launched"/>, it waits in its socket in the ring (the first point of <see cref="path"/>, far under the plane) until the leading ship's z
    /// passes <see cref="liftAt"/>, then climbs out along the path at <see cref="climb"/> m/s. Its cover is off while it is under the plane and comes on as it reaches it.</item>
    /// <item><b>Keeping with its escort.</b> At the plane it flies north on its column at <see cref="cruise"/>, easing to <see cref="easy"/> once it is <see cref="ahead"/> metres ahead
    /// of the lead, so the party flies beside it; never faster than 4 m/s.</item>
    /// <item><b>Never trapping a ship.</b> Before every step it looks where its cover would stand: a hero ship, an enemy or another district (closer than <see cref="spacing"/> ahead of
    /// it) there, and it holds where it is until the way is clear. It never pushes into anything.</item>
    /// <item><b>On station.</b> At the path's end it holds its place in the formation, swaying <see cref="bob"/> metres. A save with <see cref="stationFlag"/> loads it there.</item>
    /// <item><b>Leaving.</b> Once <see cref="releaseFlag"/> is set it flies <see cref="exit"/> out and down to the convoy and stays there; a save with the flag loads it there.</item>
    /// </list>
    /// What it says: <see cref="liftSpeaker"/>'s line as it leaves the ring, <see cref="planeSpeaker"/>'s as it reaches the plane (radio lines, by their place in the pool), and during a
    /// boss fight the lead's <c>section</c> line the first time the lead passes within <see cref="barkRange"/> of it (the line by hero: <see cref="barkHeroes"/>, <see cref="barkLines"/>).
    /// Nothing is moved while the game is paused or a conversation holds the party. Logs <c>SECTION_LIFTS id</c>, <c>SECTION_AT_PLANE id</c>, <c>SECTION_ON_STATION id</c>,
    /// <c>SECTION_HOLDS id why</c> (once a hold), <c>SECTION_PASSES id</c>, <c>SECTION_LEAVES id</c>.
    /// </summary>
    public sealed class SectionFlight:MonoBehaviour
    {
        public static readonly List<SectionFlight> All=new();
        public enum Stage{Docked,Flying,OnStation,Leaving,Gone}

        /// <summary>The district's id (<c>market</c>, <c>repair</c>, <c>crescent</c>, <c>dock</c>) and how a player says it.</summary>
        public string districtId="",displayName="";
        /// <summary>Its way from its socket in the ring to its station, as world points of its root (y is the root's: under the plane while it climbs, the plane after).</summary>
        public Vector3[] path=Array.Empty<Vector3>();
        /// <summary>Its way from its station to the convoy, once released.</summary>
        public Vector3[] exit=Array.Empty<Vector3>();
        /// <summary>Already off the ring at load (the market, which lifted on the Decks).</summary>
        public bool launched;
        /// <summary>The lead's z it lifts at.</summary>
        public float liftAt;
        public float cruise=3.8f,easy=2.2f,climb=3.6f,ahead=30,spacing=14,clearance=.8f,plane=1;
        /// <summary>A <see cref="launched"/> district waits where it is for its escort: it sets off once the lead has come level with it within <see cref="escortReach"/> of it, or is
        /// <see cref="escortAhead"/> metres past it anywhere. Flown off at once, the market left the arrival before the party had moved, and the zone's first frame lost its subject.</summary>
        public float escortReach=30,escortAhead=40;
        /// <summary>Its cover: solid from 2 m under the plane to 4 m over (contract), on a child of the root, and the shape the district is drawn at the plane (a convex hull fitted to it:
        /// D272, a box over an irregular outline stops shots in clear space).</summary>
        public Collider body;
        /// <summary>Its footprint on the plan, about the root: what it is spaced by, holds for and is measured from (the fitted cover's box, as the builder took it).</summary>
        public Vector3 footCentre=new(0,1,0),footSize=new(12,6,17);
        public string stationFlag="ring.formed",releaseFlag="home.cutterDisabled";
        public string liftSpeaker="",liftSituation="",planeSpeaker="",planeSituation="";public int liftLine=-1,planeLine=-1;
        public string[] barkHeroes=Array.Empty<string>();public int[] barkLines=Array.Empty<int>();public float barkRange=12;
        public float bob=.9f,bobPeriod=23;
        /// <summary>The engines' flames (scaled by their burn) and their light.</summary>
        public Transform[] flames=Array.Empty<Transform>();public Light engineLight;public float engineIntensity=2.4f;

        Stage stage;float along,total,clock,heldSince=-1;float[] arcs;Vector3 station;bool atPlane,barked,spoke,setOff;string holdWhy;Vector3[] flameScale;
        /// <summary>Whether a launched district has set off with its escort.</summary>
        public bool SetOff=>!launched||setOff;
        public Stage State=>stage;
        public float Along=>along;
        public float Total{get{Prepare();return total;}}
        public bool OnStation=>stage==Stage.OnStation;
        public bool AtPlane=>atPlane;
        /// <summary>The way it last held for, or null while it moves.</summary>
        public string Held=>holdWhy;
        public float Speed{get;private set;}
        /// <summary>Its station: the path's last point.</summary>
        public Vector3 Station=>path.Length>0?path[path.Length-1]:transform.position;
        /// <summary>Its footprint on the plan where its root stands at <paramref name="at"/>.</summary>
        public Rect FootprintAt(Vector3 at)
        {
            var c=at+footCentre;var s=footSize;
            return new Rect(c.x-s.x*.5f,c.z-s.z*.5f,s.x,s.z);
        }
        public Rect Footprint=>FootprintAt(transform.position);
        /// <summary>The nearest two footprints come to each other on the plan (nought if they overlap).</summary>
        public static float Gap(Rect a,Rect b){float dx=Mathf.Max(0,Mathf.Max(a.xMin-b.xMax,b.xMin-a.xMax)),dz=Mathf.Max(0,Mathf.Max(a.yMin-b.yMax,b.yMin-a.yMax));return Mathf.Sqrt(dx*dx+dz*dz);}
        /// <summary>How far a point of the plan is from a footprint (nought inside it).</summary>
        public static float From(Rect r,Vector2 p){float dx=Mathf.Max(0,Mathf.Max(r.xMin-p.x,p.x-r.xMax)),dz=Mathf.Max(0,Mathf.Max(r.yMin-p.y,p.y-r.yMax));return Mathf.Sqrt(dx*dx+dz*dz);}

        void OnEnable(){if(!All.Contains(this))All.Add(this);BarkService.Spoken+=Heard;}
        void OnDisable(){All.Remove(this);BarkService.Spoken-=Heard;}
        void Heard(string who,string line)=>spoke=true;

        void Prepare()
        {
            if(arcs!=null&&arcs.Length==path.Length)return;
            arcs=new float[path.Length];for(int i=1;i<path.Length;i++)arcs[i]=arcs[i-1]+Vector3.Distance(path[i-1],path[i]);total=path.Length>0?arcs[path.Length-1]:0;
        }
        /// <summary>The point of its path at this arc length.</summary>
        public Vector3 PointAt(float arc)
        {
            Prepare();if(path.Length==0)return transform.position;if(path.Length==1)return path[0];
            arc=Mathf.Clamp(arc,0,total);int i=Array.BinarySearch(arcs,arc);if(i<0)i=~i-1;i=Mathf.Clamp(i,0,path.Length-2);
            return Vector3.Lerp(path[i],path[i+1],(arc-arcs[i])/Mathf.Max(.0001f,arcs[i+1]-arcs[i]));
        }

        void Start()
        {
            Prepare();station=Station;flameScale=new Vector3[flames.Length];for(int i=0;i<flames.Length;i++)if(flames[i]!=null)flameScale[i]=flames[i].localScale;
            var flags=GameServices.Current!=null?GameServices.Current.Flags:null;
            if(flags!=null&&!string.IsNullOrEmpty(releaseFlag)&&flags.GetBool(releaseFlag)){stage=Stage.Gone;Place(exit.Length>0?exit[exit.Length-1]:station);}
            else if(flags!=null&&!string.IsNullOrEmpty(stationFlag)&&flags.GetBool(stationFlag)){stage=Stage.OnStation;along=total;Place(station);}
            else{stage=launched?Stage.Flying:Stage.Docked;along=0;Place(PointAt(0));}
            atPlane=transform.position.y>=plane-1;if(body!=null)body.enabled=atPlane&&stage!=Stage.Gone;
        }
        void Place(Vector3 at){transform.position=at;Physics.SyncTransforms();}

        void Update()
        {
            var services=GameServices.Current;if(services==null)return;
            Burn();
            if(GameTime.CombatHeld)return;
            float dt=Time.deltaTime;if(dt<=0)return;clock+=dt;
            var party=PartyController.Current;var lead=party!=null&&party.members!=null&&party.members.Length>0?party.Active:null;
            switch(stage)
            {
                case Stage.Docked:
                    if(lead!=null&&lead.transform.position.z>=liftAt){stage=Stage.Flying;Debug.Log("SECTION_LIFTS "+districtId);Say(liftSpeaker,liftSituation,liftLine);}
                    break;
                case Stage.Flying:
                {
                    var here=transform.position;bool under=here.y<plane-1.5f;
                    if(launched&&!setOff)
                    {
                        if(lead==null)break;var at=lead.transform.position;
                        if(!(at.z>=here.z&&From(Footprint,new Vector2(at.x,at.z))<=escortReach||at.z>=here.z+escortAhead))break;
                        setOff=true;Debug.Log("SECTION_SETS_OFF "+districtId);
                    }
                    float speed=under?climb:lead!=null&&here.z-lead.transform.position.z>ahead?easy:cruise;
                    float to=Mathf.Min(total,along+Mathf.Min(speed,4)*dt);var next=PointAt(to);
                    if(Move(here,next,dt)){along=to;if(along>=total-.01f){stage=Stage.OnStation;Debug.Log("SECTION_ON_STATION "+districtId);}}
                    break;
                }
                case Stage.OnStation:
                {
                    if(!string.IsNullOrEmpty(releaseFlag)&&services.Flags.GetBool(releaseFlag)){stage=Stage.Leaving;along=0;exitFrom=transform.position;Debug.Log("SECTION_LEAVES "+districtId);break;}
                    float a=clock*Mathf.PI*2/bobPeriod;var next=station+new Vector3(Mathf.Sin(a)*bob,0,Mathf.Sin(a*.73f+1.1f)*bob*.6f);
                    Move(transform.position,next,dt);
                    break;
                }
                case Stage.Leaving:
                {
                    // At the plane it holds for whatever is in its way, as it does flying in; once under it, nothing is.
                    float to=along+cruise*dt;var next=ExitAt(to,out bool end);
                    if(next.y>=plane-1.5f){if(!Move(transform.position,next,dt))break;}else Place(next);
                    along=to;
                    if(body!=null&&body.enabled&&next.y<plane-1.5f)body.enabled=false;atPlane=next.y>=plane-1;
                    if(end){stage=Stage.Gone;Debug.Log("SECTION_WITH_THE_CONVOY "+districtId);}
                    break;
                }
            }
            SectionLine(lead);
        }
        Vector3 exitFrom;
        Vector3 ExitAt(float arc,out bool end)
        {
            var from=exitFrom;end=false;
            foreach(var p in exit){float leg=Vector3.Distance(from,p);if(arc<=leg)return Vector3.Lerp(from,p,arc/Mathf.Max(.0001f,leg));arc-=leg;from=p;}
            end=true;return from;
        }
        /// <summary>Steps toward <paramref name="next"/> unless something is where its cover would stand: true if it moved.</summary>
        bool Move(Vector3 here,Vector3 next,float dt)
        {
            var why=Blocked(here,next);
            if(why!=null){Speed=0;if(holdWhy==null){Debug.Log("SECTION_HOLDS "+districtId+" "+why);heldSince=Time.time;}holdWhy=why;return false;}
            holdWhy=null;Speed=Vector3.Distance(here,next)/dt;Place(next);
            bool nowAtPlane=next.y>=plane-1;
            if(nowAtPlane&&!atPlane){atPlane=true;if(body!=null)body.enabled=true;Debug.Log("SECTION_AT_PLANE "+districtId);Say(planeSpeaker,planeSituation,planeLine);}
            return true;
        }
        /// <summary>What would be in the way of its cover at <paramref name="next"/>, or null: a ship or an enemy anywhere in it (and a margin), or another district closer than
        /// <see cref="spacing"/> ahead. Deep under the plane nothing is in its way.</summary>
        string Blocked(Vector3 here,Vector3 next)
        {
            if(body==null||next.y+4<plane-2.5f)return null;
            var centre=next+footCentre;var half=footSize*.5f+Vector3.one*clearance;
            foreach(var hit in Physics.OverlapBox(centre,half,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
            {
                // A hero ship and an enemy body are each a CharacterController (ActorFactory): nothing else that flies is.
                if(hit.transform.IsChildOf(transform)||hit is not CharacterController)continue;
                return hit.name;
            }
            if(next.y<plane-1.5f)return null;
            var way=next-here;way.y=0;var mine=FootprintAt(next);
            foreach(var other in All)
            {
                if(other==this||other==null||!other.atPlane||other.stage==Stage.Gone)continue;
                var off=other.transform.position-next;off.y=0;
                // Only a district ahead holds it: one behind is the one that holds.
                if(way.sqrMagnitude>1e-6f&&Vector3.Dot(off,way)<=0)continue;
                if(Gap(mine,other.Footprint)<spacing&&Gap(mine,other.Footprint)<Gap(FootprintAt(here),other.Footprint)+.001f)return other.districtId;
            }
            return null;
        }
        void Say(string speaker,string situation,int line)
        {
            if(string.IsNullOrEmpty(speaker)||string.IsNullOrEmpty(situation))return;
            if(line>=0)BarkService.PlayAt(speaker,situation,line,true);else BarkService.Play(speaker,situation,true);
        }
        /// <summary>The lead's word as a district goes by in the Cutter's fight: once a visit, and only if it was said (a line not given priority can wait its turn and be lost).</summary>
        void SectionLine(CombatActor lead)
        {
            if(barked||lead==null||!atPlane||!BossController.AnyFightUnderway||body==null||!body.enabled)return;
            var p=lead.transform.position;if(From(Footprint,new Vector2(p.x,p.z))>barkRange)return;
            int index=-1;for(int i=0;i<barkHeroes.Length&&i<barkLines.Length;i++)if(barkHeroes[i]==lead.character){index=barkLines[i];break;}
            if(index<0){barked=true;return;}
            spoke=false;BarkService.PlayAt(lead.character,"section",index);
            if(spoke){barked=true;Debug.Log("SECTION_PASSES "+districtId);}
        }
        /// <summary>The engines burn hard climbing and flying, idle on station, and are out once it is with the convoy.</summary>
        void Burn()
        {
            float level=stage==Stage.Flying||stage==Stage.Leaving?1:stage==Stage.OnStation?.45f:stage==Stage.Docked?0:.2f;
            float flicker=1+.12f*(Mathf.PerlinNoise(Time.time*7.3f,districtId.Length)-.5f)*2;
            for(int i=0;i<flames.Length;i++)if(flames[i]!=null){var s=flameScale!=null&&i<flameScale.Length?flameScale[i]:flames[i].localScale;flames[i].localScale=new Vector3(s.x,s.y,s.z*Mathf.Max(.05f,level*flicker));flames[i].gameObject.SetActive(level>0);}
            if(engineLight!=null){engineLight.intensity=engineIntensity*level*flicker;engineLight.enabled=level>0;}
        }
    }
}
