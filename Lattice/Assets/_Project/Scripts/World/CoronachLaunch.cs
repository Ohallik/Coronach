using System;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Chapter 9's launch (docs/maps/DecksLeaving.md, "The launch"), one to the zone: once the close's talk has set <see cref="walkedFlag"/> and ended, the party is held for as long as it lasts
    /// and no longer. Anyone on the market side of the line, or near it, walks back beside Orrin (<see cref="watch"/>; set there if the step can't be walked); the chalked joints in view
    /// brighten in turn, farthest first, and the walkway's lets go under the yellow line (a deep release, a short hold of the clock, the view's tremor, the pad's low motor); then
    /// <see cref="node"/> plays, and a beat into it the market district lifts away on its own clock (<see cref="district"/> drawn up and along by <see cref="lift"/>), its cut edge and
    /// engines lit, space showing where it stood, with the view drawn back above the dialogue box to take it in. When the talk has set <see cref="launchedFlag"/>, Orrin's first radio line,
    /// a moment more of the district going, and the load into the Broken Ring, whose arrival saves. Its own music plays from the close (<see cref="music"/>, one of the zone's cueMusicIds).
    /// A save made before the talk's end replays it from the start on loading (the party set at its marks behind the load's black); the zone entered with <see cref="launchedFlag"/> already set
    /// goes straight on to the Ring. Logs <c>LAUNCH_BEGIN</c>, <c>LAUNCH_JOINT</c>, <c>LAUNCH_LIFT</c>, <c>LAUNCH_CUT</c>.
    /// </summary>
    public sealed class CoronachLaunch:MonoBehaviour
    {
        public enum Stage{Waiting,Stepping,Opening,Talking,Going,Gone}
        public string walkedFlag="home.coronachWalked",launchedFlag="home.launched";
        public string node="NeveFirstOut",speaker="Neve";
        /// <summary>The launch's own track, by title from docs/music/tracks.json.</summary>
        public string music;
        public string ring="BrokenRing",ringSpawn="Arrival";
        /// <summary>The radio line after the talk: the first of a pool the Ring goes on with.</summary>
        public string hailBy="Orrin",hail="hail.section";
        /// <summary>What lifts (everything standing in the district's footprint, each moved by the same offset: none is under another), how far (world metres, up and along) and over how
        /// long; how long into the talk it starts.</summary>
        public Transform[] district=Array.Empty<Transform>();public Vector3 lift=new(14,30,8);public float liftSeconds=18,liftDelay=1.8f;
        /// <summary>Flat things laid across the parting (a runner, a strip of studs): torn as the joint lets go, hidden then, since half of one can't go with the district.</summary>
        public GameObject[] torn=Array.Empty<GameObject>();
        /// <summary>The line it parts along, and a point on the side that stays.</summary>
        public Vector3 lineA,lineB,stays;
        /// <summary>Where the lead and the partner watch from, and what they look at.</summary>
        public Vector3[] watch=Array.Empty<Vector3>();public Vector3 lookAt;
        /// <summary>The chalk on the joints in view, farthest first, and the light each takes.</summary>
        public Renderer[] chalk=Array.Empty<Renderer>();public Color chalkGlow=new(1.4f,1.3f,1.1f);
        /// <summary>Where the walkway joint vents as it lets go.</summary>
        public Vector3[] vents=Array.Empty<Vector3>();
        /// <summary>Shown from the joint's release: the district's cut edge and engines, under its root.</summary>
        public GameObject[] released=Array.Empty<GameObject>();
        /// <summary>What the view draws back to take in, kept above the dialogue box.</summary>
        public Bounds shows;public float farthest=36;
        /// <summary>Seconds: walking back from the line at most, the joints, and the district going after the talk.</summary>
        public float stepSeconds=3,openSeconds=4.2f,goingSeconds=2.6f;
        /// <summary>When in <see cref="openSeconds"/> the walkway joint lets go.</summary>
        public float releaseAt=2.6f;
        static readonly Rect Above=Rect.MinMaxRect(.06f,.4f,.94f,.96f),HeroAbove=Rect.MinMaxRect(.1f,.4f,.9f,.9f);
        /// <summary>Above a conversation's own framing (1), so the talk doesn't take the view off the district.</summary>
        const int FramingRank=2;
        public Stage Now{get;private set;}
        public float LiftShare{get;private set;}
        public static CoronachLaunch Current{get;private set;}
        bool settled,talking,joint,hailed,cued,lifting;float clock,liftClock;int lit;Vector3[] home=Array.Empty<Vector3>();Vector3 centre;
        MaterialPropertyBlock block;CameraRig rig;AudioManager.Loop engines;
        static bool Flag(string key)=>!string.IsNullOrEmpty(key)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        void OnEnable()=>Current=this;
        void OnDisable(){if(Current==this)Current=null;Release();}
        void OnDestroy()=>Release();
        void Release(){if(cued){MusicDirector.Encounter(this,null,false);cued=false;}engines?.Dispose();engines=null;}
        void Start()
        {
            home=district.Select(t=>t!=null?t.position:Vector3.zero).ToArray();centre=(lineA+lineB)*.5f;
            block=new MaterialPropertyBlock();foreach(var thing in released)if(thing!=null)thing.SetActive(false);
        }

        void Update()
        {
            var services=GameServices.Current;if(services==null)return;
            var flow=SceneFlow.Current;bool loading=flow!=null&&flow.Loading;
            var party=PartyController.Current;bool hasParty=party!=null&&party.members!=null&&party.members.Length>0&&party.Active!=null;
            if(!settled)
            {
                if(!hasParty)return;settled=true;
                // The Decks as a place is gone once the market has flown: nothing here is to be walked again.
                if(Flag(launchedFlag)){Debug.Log("LAUNCH_ALREADY");Now=Stage.Gone;Cut();return;}
                // A save made between the close and the talk's end: the party stands at its marks behind the load's black, and it plays from the start.
                if(Flag(walkedFlag)&&loading){Place(party);Begin();}
            }
            switch(Now)
            {
                case Stage.Waiting:
                    if(Flag(walkedFlag)&&!services.Input.Blocked&&!loading)Begin();
                    return;
                case Stage.Stepping:
                    clock+=Time.deltaTime;Hold();Frame();
                    if(StepBack(party,clock>=stepSeconds)){Now=Stage.Opening;clock=0;}
                    return;
                case Stage.Opening:
                    clock+=Time.deltaTime;Hold();Frame();Face(party);Open();
                    if(clock>=openSeconds){Now=Stage.Talking;clock=0;}
                    return;
                case Stage.Talking:
                    Frame();Lift();
                    if(!talking&&!Flag(launchedFlag))Talk();
                    if(Flag(launchedFlag)&&!talking){Now=Stage.Going;clock=0;}
                    return;
                case Stage.Going:
                    clock+=Time.deltaTime;Hold();Frame();Lift();
                    if(!hailed&&clock>=.5f){hailed=true;BarkService.PlayAt(hailBy,hail,0,true);}
                    if(clock>=goingSeconds){Now=Stage.Gone;Cut();}
                    return;
                default:Lift();return;
            }
        }
        void Begin()
        {
            Now=Stage.Stepping;clock=0;Debug.Log("LAUNCH_BEGIN");
            if(!string.IsNullOrEmpty(music)){MusicDirector.Encounter(this,music,true);cued=true;}
            GameServices.Current.Input.Blocked=true;
        }
        /// <summary>The party is held through the launch: no step, no menu, no swap. The talk holds it itself, and lets go as it ends, so this asks again each frame outside it.</summary>
        void Hold(){if(!talking)GameServices.Current.Input.Blocked=true;}
        void Frame()
        {
            if(shows.size.sqrMagnitude<=0)return;
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig==null)return;
            // A fixed stretch of the town, not the district: the district rises out of it, past the top of the frame, as a district lifting past should. Framed on the district, the view chased it out to its farthest.
            rig.FrameGround(shows,Above,HeroAbove,farthest,FramingRank);
        }
        /// <summary>Which side of the parting line a point is on: above nought, the side that stays.</summary>
        public float Side(Vector3 at)
        {
            var along=lineB-lineA;along.y=0;var across=new Vector3(-along.z,0,along.x).normalized;
            float sign=Mathf.Sign(Vector3.Dot(stays-lineA,across));return Vector3.Dot(at-lineA,across)*sign;
        }
        /// <summary>Walks anyone on the district's side or within two metres of the line back to their mark, a stroll's pace, the way a talk walks the partner; past the time allowed, sets them there.</summary>
        bool StepBack(PartyController party,bool late)
        {
            if(party==null||party.members==null)return true;bool all=true;
            for(int i=0;i<party.members.Length&&i<watch.Length;i++)
            {
                var hero=party.members[i==0?party.index:Other(party)];if(hero==null)continue;
                var mark=watch[i];var to=mark-hero.transform.position;to.y=0;
                // A save loaded into the launch gets here a frame before the party has its motors (PartyController gives them with the zone): it threw once a frame
                // until they came (whole suite, November 15). Wait for them, or set the hero on the mark if the time allowed has run out.
                if(hero.motor==null){if(late)Set(hero,mark);else all=false;continue;}
                // Already well back on the side that stays: nobody is walked anywhere they needn't go.
                if(Side(hero.transform.position)>=2||to.magnitude<=.35f){hero.motor.Move(Vector2.zero,false,false);continue;}
                if(late){Set(hero,mark);continue;}
                all=false;hero.motor.Move(new Vector2(to.x,to.z).normalized*.45f,false,false);
            }
            return all||late;
        }
        static int Other(PartyController party){for(int i=0;i<party.members.Length;i++)if(i!=party.index)return i;return party.index;}
        void Place(PartyController party){for(int i=0;i<party.members.Length&&i<watch.Length;i++){var hero=party.members[i==0?party.index:Other(party)];if(hero!=null)Set(hero,watch[i]);}Face(party);}
        static void Set(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();if(body!=null)body.enabled=false;
            hero.transform.position=at;if(body!=null)body.enabled=true;Physics.SyncTransforms();
        }
        void Face(PartyController party)
        {
            if(party==null||party.members==null)return;
            foreach(var hero in party.members)
            {
                if(hero==null)continue;var look=lookAt-hero.transform.position;look.y=0;if(look.sqrMagnitude<.01f)continue;
                hero.transform.rotation=Quaternion.RotateTowards(hero.transform.rotation,Quaternion.LookRotation(look),360*Time.unscaledDeltaTime);
                if(hero.TryGetComponent<GroundMotor>(out var motor))motor.KeepFacing(hero.transform.forward);
            }
        }
        /// <summary>The joints, farthest first, each brightening with a breath of vented air; then the walkway's lets go.</summary>
        void Open()
        {
            float step=releaseAt/Mathf.Max(1,chalk.Length+1);
            while(lit<chalk.Length&&clock>=step*(lit+1))
            {
                var line=chalk[lit++];if(line==null)continue;
                line.GetPropertyBlock(block);block.SetColor("_EmissionColor",chalkGlow);line.SetPropertyBlock(block);
                AudioManager.PlayAt("joint_hiss",null,line.bounds.center,.18f,AudioBus.SFX,60,.05f,UnityEngine.Random.Range(.92f,1.08f));
            }
            if(joint||clock<releaseAt)return;
            joint=true;Debug.Log("LAUNCH_JOINT");
            AudioManager.PlayAt("joint_release",null,(lineA+lineB)*.5f,.42f,AudioBus.SFX,20);
            AudioManager.PlayAt("impactMetal_000",null,(lineA+lineB)*.5f,.3f,AudioBus.SFX,30,.06f,.55f);
            foreach(var vent in vents)CombatVfx.Burst(vent+Vector3.up*.3f,new Color(.86f,.9f,.94f,.55f),"steam");
            foreach(var thing in released)if(thing!=null)thing.SetActive(true);
            foreach(var thing in torn)if(thing!=null)thing.SetActive(false);
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();
            if(rig!=null){rig.Tremor(1.8f,.11f);rig.Impulse(Vector3.down,.009f,.45f);}
            Impact.Current?.Hold(.15f,true);PadRumble.Pulse(.8f,.35f,.6f);
        }
        void Talk()
        {
            bool inside=true,refused=false;talking=true;
            bool sent=Npc.Say(node,speaker,completed=>
            {
                if(inside&&!completed){refused=true;return;}
                talking=false;
            });
            inside=false;
            if(!sent||refused){talking=false;return;}
            if(!lifting){lifting=true;liftClock=-liftDelay;}
        }
        void Lift()
        {
            if(!lifting)return;
            liftClock+=Time.deltaTime;if(liftClock<0||district.Length==0)return;
            if(LiftShare<=0)
            {
                Debug.Log("LAUNCH_LIFT");var first=district.FirstOrDefault(t=>t!=null);if(first!=null)engines=AudioManager.StartLoop("spaceEngineLow_000",first,0);
                AudioManager.PlayAt("district_lift",null,centre,.4f,AudioBus.SFX,24);
                if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Tremor(2.4f,.06f);
            }
            float u=Mathf.Clamp01(liftClock/liftSeconds);
            // Engines take the weight before anything moves much: slow off the deck, then away.
            LiftShare=Mathf.Max(.0001f,u*u*(3-2*u)*.55f+u*u*.45f);
            for(int i=0;i<district.Length;i++)if(district[i]!=null)district[i].position=home[i]+lift*LiftShare;
            engines?.Set(Mathf.Lerp(.05f,.32f,Mathf.Clamp01(liftClock/3)));
        }
        void Cut()
        {
            var flow=SceneFlow.Current;if(flow==null)return;
            Debug.Log("LAUNCH_CUT "+ring);flow.LoadZone(ring,ringSpawn);
        }
    }
}
