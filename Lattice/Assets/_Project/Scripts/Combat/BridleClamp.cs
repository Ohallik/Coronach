using System;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the four clamps of the first Bridle, in the Glass Choir's engine hall (chapter 7): what the Last Warden fight is about. It is opened, never broken: it has no integrity
    /// and takes no blow. It stands on its <b>seat</b> (this object: the floor a hero stands on, which is where its prompt is), and is worked in the release's own two beats. Once its
    /// boss's hold on the road has come down to its line the boss offers it (<see cref="Offer"/>): its seat lights as a pale ring. A hero in the seat takes its weight
    /// (<c>Take the clamp's weight</c>, the Interact button, which answers in this fight: <see cref="InFight"/>); the clamp pulls while its weight is taken, and eases only while
    /// the boss's staff is down (<see cref="Slack"/>); held through the ease, it opens onto its catch. A hero who rolls, strikes, is staggered, goes down or steps out of the seat lets
    /// go, and what he had taken drains away; a Clamp Tender at work on it (<see cref="Tended"/>) closes it as fast, and it never eases while one is there. Opened: its fibres
    /// (<see cref="ForceLine.clamp"/>) go slack, its flag is set, it reports <c>Clamp</c> to the quests and <see cref="Opened"/> tells the boss. With every clamp open they let go together
    /// (<see cref="LetGoTogether"/>), which its boss does as it stands down. A clamp loads let go once <c>bossdown.&lt;settledBy&gt;</c> is set, and shut on any other load: a lost fight
    /// starts again with all four. The room it stands in: docs/maps/GlassChoir.md, "Boss arena contract".
    /// </summary>
    [RequireComponent(typeof(Tended))]
    public sealed class BridleClamp:InteractionPrompt
    {
        public static readonly List<BridleClamp> All=new();
        /// <summary>A clamp has just opened onto its catch, and <see cref="OpenCount"/> already counts it.</summary>
        public static event Action<BridleClamp> Opened;
        /// <summary>Every clamp has let go, together.</summary>
        public static event Action LetGo;
        public enum Hold{Shut,Open,Released}
        /// <summary>Which clamp this is (1 NE, 2 NW, 3 SW, 4 SE): its fibres name it, and nothing depends on the order the scene woke them in.</summary>
        public int index;
        /// <summary>The boss whose fall lets every clamp go, and who offers them.</summary>
        public string settledBy="LastWarden";
        /// <summary>Set as it opens (oriel.clamp1 to 4).</summary>
        public string openedFlag;
        /// <summary>The clamp shut, opened onto its catch, and let go: one is shown at a time, and any can be missing.</summary>
        public GameObject whole,onCatch,released;
        /// <summary>How far from the seat's middle a hero may stand and still have his hands on it.</summary>
        public float seatRadius=1.6f;
        /// <summary>Loads open onto its catch once its <see cref="openedFlag"/> is set, not shut: a clamp of a boss whose bar is its clamps (chapter 10's Mother of Doors, D285: nobody opens
        /// a clamp twice). Chapter 7's stay as they were: shut on any load before the fight is won.</summary>
        public bool keepsOpened;
        /// <summary>Its boss takes its clamps in order (Mother of Doors: each clamp's guns wait on the one before, D292), set by that boss's fight as it wakes: only the lowest-numbered of
        /// them still shut is the one to work, and a berth whose shelf holds any other says which comes first (World's Berth).</summary>
        [NonSerialized] public bool inOrder;
        /// <summary>How many of its boss's slacks it is held through before it opens: each eases it a notch, kept while nobody holds it and wound back by a tender at work on it.
        /// One for chapter 7's; Mother of Doors sets her own (D364: her room was four clamps of one slack each, and over in 101 to 104 s against a bar from 120).</summary>
        [NonSerialized] public int slacks=1;
        /// <summary>The notches it has eased so far (of <see cref="slacks"/>).</summary>
        public int Eased=>eased;
        /// <summary>The clamp of this boss's to work next, when its clamps are taken in order: the lowest-numbered still shut. Null when they aren't, or none is shut.</summary>
        public static BridleClamp NextOf(string settledBy)
        {
            BridleClamp next=null;
            foreach(var clamp in All)if(clamp!=null&&clamp.inOrder&&clamp.settledBy==settledBy&&clamp.StillShut&&(next==null||clamp.index<next.index))next=clamp;
            return next;
        }
        /// <summary>Still to be opened: shut, and not kept open on the disk (D285: a flag set before its Start read the save, or since, is a clamp opened, as its boss counts it).</summary>
        public bool StillShut=>State==Hold.Shut&&!(keepsOpened&&!string.IsNullOrEmpty(openedFlag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(openedFlag));
        /// <summary>The release's two beats: how long a hero holds to take its weight, and how long it must be held through the slack before it opens; and how fast what was taken drains
        /// once nobody holds it, or a tender is at it, as a share of the rate it was taken.</summary>
        public const float TakeWeight=1.2f,EaseHold=.8f,Drains=1.5f;
        /// <summary>The seat: a pale ring in the party's cream while it can be worked, and the hold filling it in cyan, then amber through the ease. Boundaries, never a danger's colour (D217).</summary>
        static readonly Color SeatTone=new(1f,.93f,.74f,.42f),WeightTone=new(.55f,.9f,1f,.45f),EaseTone=new(1f,.74f,.38f,.45f);
        /// <summary>The staff is down: set every frame by the boss that holds the clamps (its opening, its break). What a clamp eases on.</summary>
        public static bool Slack;
        public Hold State{get;private set;}=Hold.Shut;
        /// <summary>Its boss's hold is down to its line: it can be worked.</summary>
        public bool Offered{get;private set;}
        public CombatActor Holder{get;private set;}
        /// <summary>Whose hands opened it: the release that ends the fight is theirs.</summary>
        public CombatActor OpenedBy{get;private set;}
        /// <summary>How much of its weight is taken (0 to 1), and how far through the ease it is (0 to 1).</summary>
        public float Weight=>Mathf.Clamp01(weight/TakeWeight);
        public float Ease=>Mathf.Clamp01(ease/EaseHold);
        public bool Carried=>weight>=TakeWeight-.0001f;
        public static int OpenCount{get{int n=0;foreach(var clamp in All)if(clamp.State!=Hold.Shut)n++;return n;}}
        public static int ShutCount=>All.Count-OpenCount;
        /// <summary>A hero has his hands on one of them.</summary>
        public static BridleClamp Held{get{foreach(var clamp in All)if(clamp.Holder!=null)return clamp;return null;}}
        public override bool InFight=>true;
        public override bool Available=>isActiveAndEnabled&&Offered&&State==Hold.Shut&&Holder==null&&!GameTime.Paused;
        public Tended Tended{get;private set;}
        float weight,ease;bool easedSaid,notched;int eased;Telegraph seatRing,fill;int fillPhase;
        void Awake()
        {
            Tended=GetComponent<Tended>();Tended.seat=transform;
            prompt="Take the clamp's weight";range=seatRadius;
        }
        void Start()
        {
            var services=GameServices.Current;
            // A loaded save is a quiet state: no burst, no sound, no event.
            if(services!=null&&!string.IsNullOrEmpty(settledBy)&&services.Flags.GetBool("bossdown."+settledBy))State=Hold.Released;
            else if(keepsOpened&&services!=null&&!string.IsNullOrEmpty(openedFlag)&&services.Flags.GetBool(openedFlag))State=Hold.Open;
            Show();
        }
        /// <summary>The boss offers the clamps still shut (its hold is down to their line), or takes the offer back.</summary>
        public static void Offer(bool open){foreach(var clamp in All)clamp.OfferThis(open);}
        void OfferThis(bool open)
        {
            open&=State==Hold.Shut;if(Offered==open)return;Offered=open;
            if(!open){Drop("withdrawn");weight=ease=0;}
        }
        public override void Interact()
        {
            if(!Available||PartyController.Current==null)return;
            Holder=PartyController.Current.Active;easedSaid=false;
            Debug.Log($"CLAMP_TAKEN {index} by={Holder.character}");
            BarkService.Play(Holder.character,"clamp",true);
        }
        void Drop(string why)
        {
            if(Holder==null)return;
            Debug.Log($"CLAMP_DROPPED {index} {why}");Holder=null;
        }
        /// <summary>Its hands are still on it: alive, the one the player is playing, in the seat, and not rolling, striking or staggered.</summary>
        bool Keeps(CombatActor hero)
        {
            if(hero==null||!hero.Health.Alive||hero.Recovering||PartyController.Current==null||PartyController.Current.Active!=hero)return false;
            var o=hero.transform.position-transform.position;o.y=0;if(o.magnitude>seatRadius+.4f)return false;
            var state=hero.State;return state!=ActorState.Dodge&&state!=ActorState.Stagger&&state!=ActorState.Down&&state!=ActorState.Attack&&state!=ActorState.Skill;
        }
        void Update()
        {
            if(State!=Hold.Shut){Paint(false,0);return;}
            Tended.wanted=Offered;
            if(Holder!=null&&!Keeps(Holder))Drop(Holder.Health.Alive?"let go":"fell");
            Tended.contested=Holder!=null;
            if(GameTime.Paused)return;
            float dt=Time.deltaTime;bool tended=Tended.Tending;
            // A notch a slack: once one is eased in a slack, the next waits for the next slack.
            if(!Slack)notched=false;
            // A tender at work on it winds back what was eased.
            if(tended&&eased>0){eased=0;Debug.Log("CLAMP_WOUND_BACK "+index);}
            if(Holder!=null&&!tended)
            {
                weight=Mathf.Min(TakeWeight,weight+dt);
                if(Carried&&Slack&&!notched)
                {
                    if(!easedSaid){easedSaid=true;Debug.Log("CLAMP_EASED "+index);}
                    ease+=dt;
                    if(ease>=EaseHold)
                    {
                        if(++eased>=Mathf.Max(1,slacks)){Open(Holder);return;}
                        notched=true;ease=0;easedSaid=false;Debug.Log($"CLAMP_NOTCH {index} {eased}/{slacks}");
                    }
                }
                else ease=Mathf.Max(0,ease-dt*Drains);
            }
            else{weight=Mathf.Max(0,weight-dt*Drains);ease=0;}
            Paint(Offered,Holder==null?0:Carried&&ease>0?2:1);
        }
        /// <summary>The seat's ring while it can be worked, and the fill of the hold over it: its weight, then its ease, each filling over the time it takes.</summary>
        void Paint(bool offered,int phase)
        {
            if(offered&&seatRing==null){seatRing=Telegraph.Circle(transform.position,seatRadius,9999,SeatTone);seatRing.Steady=true;seatRing.owner=this;}
            if(!offered&&seatRing!=null){seatRing.Cancel();seatRing=null;}
            if(phase!=fillPhase||fill==null&&phase>0)
            {
                if(fill!=null){fill.Cancel();fill=null;}
                fillPhase=phase;
                if(phase==1)fill=Telegraph.Circle(transform.position,seatRadius*.8f,Mathf.Max(.05f,TakeWeight-weight),WeightTone);
                else if(phase==2)fill=Telegraph.Circle(transform.position,seatRadius*.8f,Mathf.Max(.05f,EaseHold-ease),EaseTone);
                if(fill!=null)fill.owner=this;
            }
        }
        /// <summary>Held through the ease: open onto its catch. Its fibres let go of their pull, its flag is set, the quest hears of it, and its boss is told.</summary>
        void Open(CombatActor by)
        {
            State=Hold.Open;OpenedBy=by;Holder=null;Offered=false;weight=ease=0;Tended.wanted=Tended.contested=false;Tended.Clear();Paint(false,0);
            var services=GameServices.Current;
            if(services!=null&&!string.IsNullOrEmpty(openedFlag))services.Flags.SetBool(openedFlag,true);
            if(services!=null)Lattice.Rpg.RpgServices.Quests.Report(ObjectiveKind.Interact,"Clamp");
            foreach(var line in ForceLine.All.ToArray())if(line!=null&&line.clamp==index&&index>0)line.GoSlack();
            CombatVfx.Burst(transform.position+Vector3.up*1.6f,new Color(1f,.86f,.5f),"crack");
            AudioManager.PlayAt("doorOpen_000",transform,transform.position+Vector3.up,.32f,AudioBus.SFX,50,.05f,.55f);
            Show();Debug.Log($"CLAMP_OPEN {index} opened={OpenCount}");
            Opened?.Invoke(this);
        }
        /// <summary>Every clamp lets go at once, on one call: what was opened onto its catch is released, and anything still shut with it (its boss stood down another way).</summary>
        public static void LetGoTogether()
        {
            bool any=false;
            foreach(var clamp in All)
            {
                if(clamp.State==Hold.Released)continue;any=true;
                clamp.State=Hold.Released;clamp.Holder=null;clamp.Offered=false;clamp.Tended.wanted=clamp.Tended.contested=false;clamp.Paint(false,0);clamp.Show();
                foreach(var line in ForceLine.All.ToArray())if(line!=null&&line.clamp==clamp.index&&clamp.index>0)line.GoSlack();
            }
            Slack=false;
            if(!any)return;
            Debug.Log("CLAMPS_LET_GO");LetGo?.Invoke();
        }
        void Show()
        {
            if(whole!=null)whole.SetActive(State==Hold.Shut);
            if(onCatch!=null)onCatch.SetActive(State==Hold.Open);
            if(released!=null)released.SetActive(State==Hold.Released);
        }
        protected override void OnEnable(){base.OnEnable();if(!All.Contains(this))All.Add(this);}
        protected override void OnDisable(){base.OnDisable();All.Remove(this);if(seatRing!=null)seatRing.Cancel();if(fill!=null)fill.Cancel();}
    }
}
