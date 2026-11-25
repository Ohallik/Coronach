using System;
using System.Collections;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A berth (chapter 10, ch10-wiring 6.1, D285): a part of a zone of the other form, a shelf of ground in a flight zone (Sorrel's clamps' works, the Tollkeeper's arm) or a
    /// stretch of space beside a ground zone (the Continuance's flight berth), entered and left only by a prompt, with the world still running. Its <see cref="pad"/> is the way in,
    /// in the zone's own form; its <see cref="launch"/> the way out, in the berth's. A change holds the party for a fade of <see cref="Change"/> seconds: input blocked, both heroes
    /// moved to the berth's place for that form side by side and clear of everything solid (D289), the zone's Kind, camera and plane the berth's
    /// (<see cref="ZoneController.Berth"/>), then the controls given back. While the party is on it its edges are solid, and nothing else is; every save names
    /// <see cref="returnSpawn"/> (or a boss door's spawn while its fight is begun), so a load always arrives in the zone's own form. It moves only through <see cref="Drive"/>,
    /// which does nothing while the party stands on it or a conversation holds the controls (D275). Made by <see cref="Declare"/>, from a zone's builder or a test.
    /// </summary>
    public sealed class Berth:MonoBehaviour,IBerth
    {
        /// <summary>Its name in code, logs and routes (Clamp1, Arm), and what the pad says (the shelf's name).</summary>
        public string id,shelf;
        /// <summary>The berth's form: the zone's Kind while the party is on it.</summary>
        public ZoneKind kind=ZoneKind.GroundCombat;
        /// <summary>Its floor (or space), about this object: centred on it, this big. A shelf's top stays under the flight plane, so a ship over it is not in it.</summary>
        public Vector3 boxSize=new(10,1.2f,8);
        public BerthPrompt pad,launch;
        /// <summary>Where the lead is set down on foot and in flight; the partner goes beside him along this object's right.</summary>
        public Transform foot,flight;
        /// <summary>The zone's own-form spawn beside the pad: the only spawn a save made on the berth may name.</summary>
        public string returnSpawn;
        /// <summary>The camera the berth is seen with (Ground or Flight).</summary>
        public CameraProfile view;
        /// <summary>The pad answers with a boss bar up; and it is refused while an enemy is awake within <see cref="EnemyBrain.InteractionCalm"/> of the lead.</summary>
        public bool inFight,refuseWhileAwake=true;
        /// <summary>The berth's edges, solid only while the party is on it: what keeps a hero from walking off a shelf, and never in a ship's way.</summary>
        public GameObject edges;
        public const float FlightReach=6.5f,FootReach=3,Change=.4f,EdgeThickness=.5f;
        public static readonly List<Berth> All=new();
        /// <summary>The berth the party stands on, or null.</summary>
        public static Berth Current{get;private set;}
        public static bool Docked=>Current!=null;
        /// <summary>A change is under way: nothing is offered, nothing moves.</summary>
        public static bool Changing=>changing!=null;
        static Berth changing;
        /// <summary>Raised once the party stands in its new form: the berth, and whether it is now on it.</summary>
        public static event Action<Berth,bool> Changed;
        /// <summary>The berth the party last launched from (or was put out of), until it docks again: what a replay step's "launched:" reads.</summary>
        public static string LastLaunched{get;private set;}
        /// <summary>Anything else a hero holds with his hands on a berth (a fight's own hold): while one answers true the launch waits, as it does for a Bridle clamp held.</summary>
        public static readonly List<Func<bool>> Holds=new();
        float travel,docked;string pendingLaunch;
        public string Id=>id;
        public ZoneKind Kind=>kind;
        public bool Flight=>ZoneController.IsFlight(kind);
        public bool Occupied=>Current==this||changing==this;
        static bool Held=>GameServices.Current==null||GameServices.Current.Input.Blocked||GameTime.Paused;
        public bool MayMove=>!Occupied&&!Changing&&!Held;
        public float Travel=>travel;
        public float DockedSeconds=>docked;
        public static Berth Find(string id){foreach(var b in All)if(b!=null&&b.id==id)return b;return null;}
        public bool Contains(Vector3 point)
        {
            var local=transform.InverseTransformPoint(point);
            return Mathf.Abs(local.x)<=boxSize.x*.5f&&Mathf.Abs(local.y)<=boxSize.y*.5f&&Mathf.Abs(local.z)<=boxSize.z*.5f;
        }
        /// <summary>The place the party is set down entering the berth (its own form), and leaving it (the zone's).</summary>
        Transform Inside=>Flight?flight:foot;
        Transform Outside=>Flight?foot:flight;

        // ---------- declaring one ----------
        /// <summary>
        /// A berth, from a zone's builder or a test (ch10-wiring 6.1.1). <paramref name="box"/> is its floor or space in world bounds; <paramref name="pad"/> the way in, in the zone's
        /// own form (in flight at the plane, answered within <see cref="FlightReach"/>; on foot within <see cref="FootReach"/>); <paramref name="launch"/> the way out, in the berth's,
        /// inside the box; <paramref name="footSpawn"/> and <paramref name="flightSpawn"/> where the lead is set down (a flight berth's plane is flightSpawn's height). The spawn
        /// <paramref name="returnSpawn"/> is made at <paramref name="returnAt"/> unless the scene has one by that id.
        /// </summary>
        public static Berth Declare(string id,string shelf,ZoneKind kind,Bounds box,Vector3 pad,Vector3 launch,Vector3 footSpawn,Vector3 flightSpawn,
            string returnSpawn,Vector3 returnAt,bool inFight=false,bool refuseWhileAwake=true,Transform parent=null)
        {
            var root=new GameObject("Berth "+id);if(parent!=null)root.transform.SetParent(parent,false);root.transform.position=box.center;
            // Its children go in before the berth itself, so a berth declared in play wakes with its parts there.
            root.SetActive(false);
            var berth=root.AddComponent<Berth>();
            berth.id=id;berth.shelf=shelf;berth.kind=kind;berth.boxSize=box.size;berth.returnSpawn=returnSpawn;berth.inFight=inFight;berth.refuseWhileAwake=refuseWhileAwake;
            bool flightBerth=ZoneController.IsFlight(kind);
            berth.view=GameCatalog.Find<CameraProfile>(flightBerth?"Flight":"Ground");
            berth.pad=Prompt(berth,"Pad",pad,false,flightBerth?FootReach:FlightReach);
            berth.launch=Prompt(berth,"Launch",launch,true,flightBerth?FlightReach:FootReach);
            berth.foot=Point(root.transform,"Foot",footSpawn);berth.flight=Point(root.transform,"Flight",flightSpawn);
            // Four walls round the box, from its floor to a metre over the shelf's top (a flight berth's: its whole height and more), off until the party docks.
            var edges=new GameObject("Edges");edges.transform.SetParent(root.transform,false);
            float rise=flightBerth?1.5f:1,height=box.size.y+rise,y=-box.size.y*.5f+height*.5f,t=EdgeThickness;var s=box.size;
            foreach(var (centre,size) in new[]{(new Vector3(0,y,s.z*.5f+t*.5f),new Vector3(s.x+t*2,height,t)),(new Vector3(0,y,-s.z*.5f-t*.5f),new Vector3(s.x+t*2,height,t)),
                (new Vector3(s.x*.5f+t*.5f,y,0),new Vector3(t,height,s.z)),(new Vector3(-s.x*.5f-t*.5f,y,0),new Vector3(t,height,s.z))})
            {
                var wall=new GameObject("Edge",typeof(BoxCollider));wall.transform.SetParent(edges.transform,false);wall.transform.localPosition=centre;wall.GetComponent<BoxCollider>().size=size;
            }
            edges.SetActive(false);berth.edges=edges;
            SpawnPoint existing=null;
            foreach(var spawn in FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(spawn.id==returnSpawn)existing=spawn;
            if(existing==null)
            {
                var back=new GameObject(returnSpawn,typeof(SpawnPoint));if(parent!=null)back.transform.SetParent(parent,false);
                back.transform.position=returnAt;back.GetComponent<SpawnPoint>().id=returnSpawn;
            }
            root.SetActive(true);return berth;
        }
        static BerthPrompt Prompt(Berth berth,string name,Vector3 at,bool wayOut,float range)
        {
            var go=new GameObject(name);go.transform.SetParent(berth.transform,false);go.transform.position=at;
            var prompt=go.AddComponent<BerthPrompt>();prompt.berth=berth;prompt.wayOut=wayOut;prompt.range=range;prompt.prompt=berth.Words(wayOut,null);return prompt;
        }
        static Transform Point(Transform root,string name,Vector3 at){var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=at;return go.transform;}
        /// <summary>What its prompts say: in from flight "Dock — the shelf", in from the ground "Launch — the berth"; out to flight "Launch — the zone", out to the ground "Dock — the zone".</summary>
        internal string Words(bool wayOut,string zone)
        {
            bool toFlight=wayOut?!Flight:Flight;string place=wayOut?(string.IsNullOrEmpty(zone)?"":zone):shelf;
            return (toFlight?"Launch":"Dock")+(string.IsNullOrEmpty(place)?"":" — "+place);
        }

        // ---------- in play ----------
        void OnEnable()
        {
            if(!All.Contains(this))All.Add(this);if(!ZoneController.Berths.Contains(this))ZoneController.Berths.Add(this);
            SaveSystem.SafeSpawn=Guard;
        }
        void Start()
        {
            var zone=ZoneController.Current;if(launch!=null&&zone!=null&&zone.definition!=null)launch.prompt=Words(true,DisplayNames.Zone(zone.definition.id));
            if(view==null)view=GameCatalog.Find<CameraProfile>(Flight?"Flight":"Ground");
        }
        /// <summary>On a fight's ground berth the launch waits until the lead's hands have been off the fight this long (no swing, skill, roll or stagger): on foot the interact button
        /// is the attack button, and a blow meant for the Tollkeeper's motor, struck by a lead a surge had shoved within the launch's reach, put the party out with the stage running
        /// (MooringBossReplayTests, November 15). A person who means to launch stops striking and presses at the console.</summary>
        public const float FightLaunchCalm=.8f;
        float fightingAt=-99;
        bool Fighting=>inFight&&!Flight&&Current==this&&(Time.time-fightingAt<FightLaunchCalm||FoeOnDeck());
        /// <summary>An awake foe stands on this deck, so the button is the attack's and the launch waits (D404). The calm above was not enough: with one Clamp Tender down the lead
        /// walked more than its .8 s toward the second, pressed to strike within the console's reach, and launched off the Tollkeeper's arm with the third stage running; no step
        /// could bring the party back (<c>d404</c>, MooringBossReplayTests). A boss's own body is no foe here (the motor and the clamps are worked from the deck).</summary>
        bool FoeOnDeck()
        {
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!body.Alive||!Contains(body.transform.position))continue;
                if(!body.TryGetComponent<EnemyBrain>(out var brain)||!brain.enabled||brain.Passive||brain.Flier)continue;
                if(body.GetComponent<BossController>()!=null)continue;
                return true;
            }
            return false;
        }
        void NoteFighting()
        {
            if(!inFight||Flight||Current!=this)return;
            var lead=PartyController.Current!=null?PartyController.Current.Active:null;
            if(lead!=null&&(lead.State==ActorState.Attack||lead.State==ActorState.Skill||lead.State==ActorState.Stagger||lead.State==ActorState.Dodge))fightingAt=Time.time;
        }
        void OnDisable()
        {
            All.Remove(this);ZoneController.Berths.Remove(this);
            if(All.Count==0&&SaveSystem.SafeSpawn==(Func<string,string>)Guard)SaveSystem.SafeSpawn=null;
            if(Current==this)Current=null;if(changing==this)changing=null;
        }
        void Update()
        {
            if(Current==this&&!Changing&&!Held)docked+=Time.deltaTime;
            NoteFighting();
            if(MayMove)travel+=Time.deltaTime;
            if(pendingLaunch!=null&&Current==this&&!Changing&&!Held){var why=pendingLaunch;pendingLaunch=null;StartCoroutine(Shift(false,why));}
        }
        /// <summary>Moves the berth and everything under it, when nobody is on it, no change is under way and the controls are free.</summary>
        public bool Drive(Vector3 position,Quaternion rotation){if(!MayMove)return false;transform.SetPositionAndRotation(position,rotation);return true;}
        public void Launch(string why){if(Current!=this)return;pendingLaunch=string.IsNullOrEmpty(why)?"forced":why;}
        /// <summary>A hero has his hands on a hold: the launch waits until he lets go.</summary>
        public static bool HandsBusy
        {
            get{if(BridleClamp.Held!=null)return true;foreach(var hold in Holds)if(hold!=null&&hold())return true;return false;}
        }
        /// <summary>A conversation or a menu that holds the controls holds the berth too (D275): neither prompt is offered under it.</summary>
        internal bool CanLaunch=>Current==this&&!Changing&&!Held&&!HandsBusy&&pendingLaunch==null&&!Fighting;
        /// <summary>Whether the pad would take the party now, and if not why ("awake": an enemy awake near the lead; "boss": a boss fight under way at a pad that isn't the fight's).</summary>
        internal bool CanDock(out string why)
        {
            why=null;var party=PartyController.Current;var zone=ZoneController.Current;
            if(Changing||Held||Current!=null||party==null||party.Active==null||zone==null||zone.Berthed||!party.Active.Health.Alive||party.Active.flight!=ZoneController.IsFlight(zone.definition.kind))return false;
            if(!inFight&&BossController.AnyFightUnderway){why="boss";return false;}
            if(refuseWhileAwake&&AwakeNear(party.Active.transform.position,party.Active.flight)){why="awake";return false;}
            if(OutOfTurn(out _)){why=Turn;return false;}
            return true;
        }
        /// <summary>The refusal of a shelf out of its fight's turn: its pad is still offered, with <see cref="TurnWords"/>, and takes nobody.</summary>
        public const string Turn="turn";
        /// <summary>The shelf holds a clamp of a boss that takes its clamps in order (Mother of Doors) that isn't the one to work now: shut behind an earlier one, or already open while
        /// others are shut. Docked there out of turn, a party stood with nothing to do and no word why (MooringBossReplayTests, November 15). <paramref name="words"/>: what its pad says
        /// instead, which shelf comes first, or next.</summary>
        public bool OutOfTurn(out string words)
        {
            words=null;
            foreach(var clamp in BridleClamp.All)
            {
                if(clamp==null||!clamp.inOrder||!Contains(clamp.transform.position+Vector3.up*.5f))continue;
                var next=BridleClamp.NextOf(clamp.settledBy);if(next==null||next==clamp)continue;
                Berth at=null;foreach(var b in All)if(b!=null&&b.Contains(next.transform.position+Vector3.up*.5f))at=b;
                string shelfName=at!=null&&!string.IsNullOrEmpty(at.shelf)?at.shelf:"Another clamp";
                words=shelfName+(clamp.StillShut?" first":" next");
                return true;
            }
            return false;
        }
        /// <summary>What the pad says when it would take the party.</summary>
        internal string PadWords=>Words(false,null);
        /// <summary>An enemy awake within <see cref="EnemyBrain.InteractionCalm"/> of the lead, of the lead's own form: a clamp's guns still up hold the pad, the tenders at work on
        /// the shelf below it don't (they are what the party docks to stop), and at the fight's own pad the boss doesn't either.</summary>
        bool AwakeNear(Vector3 at,bool flying)
        {
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!body.Alive)continue;
                var gap=body.transform.position-at;gap.y=0;if(gap.sqrMagnitude>EnemyBrain.InteractionCalm*EnemyBrain.InteractionCalm)continue;
                if(!body.TryGetComponent<EnemyBrain>(out var brain)||!brain.enabled||brain.Passive||brain.Flier!=flying)continue;
                if(inFight&&body.GetComponent<BossController>()!=null)continue;
                return true;
            }
            return false;
        }
        internal void DockPressed()
        {
            if(!CanDock(out var why)){if(why!=null)Debug.Log("BERTH_REFUSED "+id+" "+why);return;}
            StartCoroutine(Shift(true,null));
        }
        internal void LaunchPressed(){if(CanLaunch)StartCoroutine(Shift(false,null));}
        IEnumerator Shift(bool onto,string why)
        {
            var party=PartyController.Current;var zone=ZoneController.Current;var services=GameServices.Current;
            if(party==null||zone==null||services==null)yield break;
            changing=this;services.Input.Blocked=true;
            Debug.Log((onto?"BERTH_DOCK ":"BERTH_LAUNCH ")+id+(why!=null?" "+why:""));
            foreach(var member in party.members)if(member!=null){member.GetComponent<GroundMotor>().Halt();member.GetComponent<FlightMotor>().Halt();}
            var flow=SceneFlow.Current;float half=Change*.5f;
            for(float t=0;t<half;t+=Time.unscaledDeltaTime){if(flow!=null)flow.Fade(t/half);yield return null;}
            if(flow!=null)flow.Fade(1);
            if(party==null||zone==null){changing=null;yield break;}
            if(onto){Current=this;docked=0;edges.SetActive(true);zone.Berth(this,kind,view,flight.position.y);}
            else{edges.SetActive(false);Current=null;zone.Berth(null);}
            Place(party,(onto?Inside:Outside).position,zone.Flight);
            // D407: the forms change here, under the black. Begun now and left to run, each ship folded after the screen cleared, two hulls set down a person's 2 m apart.
            foreach(var member in party.members)if(member!=null&&member.TryGetComponent<FormController>(out var form))form.Cover();
            // The lead's lock on a body of the other form lets go now, not on his next frame of play: the frame between showed a reticle on a flier over the shelf.
            foreach(var member in party.members)if(member!=null&&member.target!=null&&EnemyBrain.OtherForm(member,member.target)){member.TargetLocked=false;member.target=null;}
            for(float t=0;t<half;t+=Time.unscaledDeltaTime){if(flow!=null)flow.Fade(1-t/half);yield return null;}
            if(flow!=null)flow.Fade(0);
            changing=null;services.Input.Blocked=false;LastLaunched=onto?null:id;
            Debug.Log((onto?"BERTH_DOCKED ":"BERTH_LAUNCHED ")+id+" "+zone.Kind);
            Changed?.Invoke(this,onto);
        }

        // ---------- setting the party down ----------
        /// <summary>The lead at the berth's place, the others beside him along the berth's right (2 m on foot, their hulls' width apart in flight), each moved along the row and then
        /// outward until his body fits clear of everything solid: a hull set down in a rock, or a hero in a shelf's edge, was a cell he couldn't leave (D289).</summary>
        void Place(PartyController party,Vector3 at,bool flying)
        {
            var lead=party.Active;var placed=new List<CombatActor>();
            Set(lead,Clear(lead,at,flying,placed));placed.Add(lead);
            int n=1;
            foreach(var member in party.members)
            {
                if(member==null||member==lead||!member.gameObject.activeInHierarchy)continue;
                float gap=flying?HeroCollision.Apart(lead.character,member.character):2;
                Set(member,Clear(member,at+transform.right*gap*n,flying,placed));placed.Add(member);n++;
            }
            Physics.SyncTransforms();
        }
        static void Set(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();bool on=body.enabled;body.enabled=false;hero.transform.position=at;body.enabled=on;
            hero.GetComponent<GroundMotor>().Halt();hero.GetComponent<FlightMotor>().Halt();
        }
        static readonly Collider[] Touching=new Collider[16];
        /// <summary>The nearest place to <paramref name="wanted"/> where the hero's body fits: on foot on a floor (inside the berth, when it is the berth's), in flight at the plane.</summary>
        Vector3 Clear(CombatActor hero,Vector3 wanted,bool flying,List<CombatActor> placed)
        {
            bool inside=flying==Flight;
            for(float ring=0;ring<=8.01f;ring+=.5f)
                for(int i=0,steps=ring<.01f?1:16;i<steps;i++)
                {
                    float a=i*Mathf.PI*2/steps;var at=wanted+(transform.right*Mathf.Cos(a)+transform.forward*Mathf.Sin(a))*ring;
                    if(!flying){if(!Floor(at,out at))continue;}else at.y=wanted.y;
                    if(inside&&!Contains(at+Vector3.up*(flying?0:.5f)))continue;
                    if(Fits(hero,at,placed))return at;
                }
            Debug.Log("BERTH_CRAMPED "+id+" "+hero.character);return wanted;
        }
        static bool Floor(Vector3 at,out Vector3 floor)
        {
            floor=at;var hits=Physics.RaycastAll(at+Vector3.up*1.5f,Vector3.down,4,~0,QueryTriggerInteraction.Ignore);float best=float.MaxValue;bool any=false;
            foreach(var hit in hits)
            {
                if(hit.collider.GetComponentInParent<CombatActor>()!=null||hit.collider.GetComponentInParent<EnemyBrain>()!=null)continue;
                if(hit.distance<best){best=hit.distance;floor=hit.point;any=true;}
            }
            return any;
        }
        static bool Fits(CombatActor hero,Vector3 at,List<CombatActor> placed)
        {
            var body=hero.GetComponent<CharacterController>();float r=body.radius;
            foreach(var other in placed)
            {
                var gap=other.transform.position-at;gap.y=0;float clear=r+other.GetComponent<CharacterController>().radius+.2f;
                if(gap.sqrMagnitude<clear*clear)return false;
            }
            var centre=at+body.center;float half=Mathf.Max(r,body.height*.5f)-r;
            var low=centre-Vector3.up*half+Vector3.up*.08f;var high=centre+Vector3.up*half;
            int count=Physics.OverlapCapsuleNonAlloc(low,high,r-.02f,Touching,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var other=Touching[i];if(other==null)continue;
                var actor=other.GetComponentInParent<CombatActor>();if(actor!=null&&PartyController.Current!=null&&Array.IndexOf(PartyController.Current.members,actor)>=0)continue;
                return false;
            }
            return true;
        }

        // ---------- saves (rule 2) ----------
        /// <summary>A save made while the party is on a berth names its own-form spawn beside the pad, or, while a boss fight is begun, the door's spawn the state already holds.
        /// Loaded, the party arrives in the zone's own form, never on a shelf in flight.</summary>
        static string Guard(string spawn)
        {
            var on=Current;if(on==null||string.IsNullOrEmpty(on.returnSpawn))return spawn;
            string kept=(AtADoor(spawn)||BossController.AnyFightUnderway)&&OwnForm(spawn)?spawn:on.returnSpawn;
            Debug.Log("BERTH_SAVE "+kept);return kept;
        }
        static bool AtADoor(string spawn)
        {
            if(string.IsNullOrEmpty(spawn))return false;
            foreach(var door in FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))if(door.Started&&!door.Cleared&&door.checkpointSpawn==spawn)return true;
            return false;
        }
        /// <summary>A spawn in the scene that stands in no berth.</summary>
        static bool OwnForm(string spawn)
        {
            if(string.IsNullOrEmpty(spawn))return false;
            foreach(var point in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            {
                if(point.id!=spawn)continue;
                foreach(var berth in All)if(berth!=null&&berth.Contains(point.transform.position))return false;
                return true;
            }
            return false;
        }
    }
}
