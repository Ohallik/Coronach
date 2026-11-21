using System;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the three coolant sluices round the Bridle Foundry's heart (chapter 8): what the Slag Colossus fight is worked with. A world switch with a fight's rules: it has no integrity and
    /// takes no blow, and it is worked with the hands, under fire. It stands on its <b>seat</b> (this object: the floor a hero stands on, which is where its prompt is), its red
    /// <see cref="wheel"/> on a housing behind it and its <see cref="spout"/> on the rim. While its boss still glows it is offered (<see cref="Offer"/>): its seat shows a pale ring. A hero in the
    /// seat turns the wheel (<c>Turn the sluice wheel</c>, the Interact button, which answers in this fight: <see cref="InFight"/>); it turns while his hands stay on it, and a roll, a swing, a
    /// skill, a stagger, a fall or a step out of the seat lets go, and what was turned runs back. Turned all the way (<see cref="TurnSeconds"/>) it opens: coolant leaves the spout,
    /// <see cref="Opened"/> tells the boss (which throws the jet onto a plate and darkens it), and the sluice refills for <see cref="Refill"/> seconds before it can be turned again. Nothing of it
    /// is saved: a lost fight starts with all three full. Once <c>bossdown.&lt;settledBy&gt;</c> is set it loads drained and inert. The room it stands in: docs/maps/BridleFoundry.md, "Boss
    /// arena contract".
    /// </summary>
    public sealed class CoolantSluice:InteractionPrompt
    {
        public static readonly List<CoolantSluice> All=new();
        /// <summary>A sluice has just been turned all the way open: its coolant is leaving the spout.</summary>
        public static event Action<CoolantSluice> Opened;
        public enum Hold{Ready,Refilling,Settled}
        /// <summary>Which sluice this is (1 the red wheel by the door, 2 east, 3 north-west): what the room's numbering says, never the order the scene woke them in.</summary>
        public int index;
        /// <summary>The boss whose fall drains every sluice, and who offers them.</summary>
        public string settledBy="SlagColossus";
        /// <summary>The wheel, turned about its own forward axis as the hero works it; and the pipe's mouth on the rim the coolant leaves from.</summary>
        public Transform wheel,spout;
        /// <summary>How far from the seat's middle a hero may stand and still have his hands on the wheel.</summary>
        public float seatRadius=1.6f;
        /// <summary>How long the wheel takes to turn open with hands on it; how fast what was turned runs back once nobody holds it, as a share of the rate it was turned; how long it takes
        /// to refill once it has opened; and how far the wheel turns, in degrees.</summary>
        public const float TurnSeconds=1.6f,Drains=1.5f,Refill=12,WheelTurns=540;
        /// <summary>The seat: a pale ring in the party's cream while it can be turned, and the turn filling it in coolant blue. Boundaries, never a danger's colour (D217).</summary>
        static readonly Color SeatTone=new(1f,.93f,.74f,.42f),TurnTone=new(.55f,.9f,1f,.45f);
        public Hold State{get;private set;}=Hold.Ready;
        /// <summary>Its boss still glows: it can be turned.</summary>
        public bool Offered{get;private set;}
        public CombatActor Holder{get;private set;}
        /// <summary>Whose hands opened it last: the plate it darkens is theirs to strike.</summary>
        public CombatActor OpenedBy{get;private set;}
        /// <summary>How far the wheel is turned (0 to 1).</summary>
        public float Turned=>Mathf.Clamp01(turned/TurnSeconds);
        public float RefilledAt{get;private set;}
        public int TimesOpened{get;private set;}
        /// <summary>Where its coolant leaves: the spout, or with none a pipe's height over the rim behind the seat.</summary>
        public Vector3 Spout=>spout!=null?spout.position:transform.position+Vector3.up*2.6f-transform.forward*3.5f;
        /// <summary>A hero has his hands on one of them.</summary>
        public static CoolantSluice Held{get{foreach(var sluice in All)if(sluice!=null&&sluice.Holder!=null)return sluice;return null;}}
        public override bool InFight=>true;
        public override bool Available=>isActiveAndEnabled&&Offered&&State==Hold.Ready&&Holder==null&&!GameTime.Paused;
        float turned,angle;Quaternion rest;bool measured;Telegraph seatRing,fill;int fillPhase;
        void Awake(){prompt="Turn the sluice wheel";range=seatRadius;Measure();}
        void Measure(){if(measured)return;measured=true;if(wheel!=null)rest=wheel.localRotation;}
        void Start()
        {
            var services=GameServices.Current;
            // A loaded save is a quiet state: no jet, no sound, no event.
            if(services!=null&&!string.IsNullOrEmpty(settledBy)&&services.Flags.GetBool("bossdown."+settledBy))Settle();
        }
        /// <summary>The boss offers the sluices (it still glows), or takes the offer back (it has cooled, it is down, or it is shifting).</summary>
        public static void Offer(bool open){foreach(var sluice in All)if(sluice!=null)sluice.OfferThis(open);}
        void OfferThis(bool open)
        {
            open&=State!=Hold.Settled;if(Offered==open)return;Offered=open;
            if(!open)Drop("withdrawn");
        }
        /// <summary>Every sluice drained and inert: its boss is down.</summary>
        public static void SettleAll(){foreach(var sluice in All)if(sluice!=null)sluice.Settle();}
        void Settle(){State=Hold.Settled;Offered=false;Holder=null;turned=0;angle=0;Paint(false,0);Spin();}
        public override void Interact()
        {
            if(!Available||PartyController.Current==null)return;
            Holder=PartyController.Current.Active;
            Debug.Log($"SLUICE_TURNING {index} by={Holder.character}");
            AudioManager.PlayAt("doorOpen_000",transform,transform.position+Vector3.up,.22f,AudioBus.SFX,40,.05f,.7f);
        }
        void Drop(string why)
        {
            if(Holder==null)return;
            Debug.Log($"SLUICE_LET_GO {index} {why}");Holder=null;
        }
        /// <summary>His hands are still on it: alive, the one the player is playing, in the seat, and not rolling, striking, staggered or down.</summary>
        bool Keeps(CombatActor hero)
        {
            if(hero==null||!hero.Health.Alive||hero.Recovering||PartyController.Current==null||PartyController.Current.Active!=hero)return false;
            var o=hero.transform.position-transform.position;o.y=0;if(o.magnitude>seatRadius+.4f)return false;
            var state=hero.State;return state!=ActorState.Dodge&&state!=ActorState.Stagger&&state!=ActorState.Down&&state!=ActorState.Attack&&state!=ActorState.Skill;
        }
        void Update()
        {
            if(State==Hold.Settled)return;
            if(Holder!=null&&!Keeps(Holder))Drop(Holder.Health.Alive?"let go":"fell");
            if(GameTime.Paused)return;
            float dt=Time.deltaTime;
            if(State==Hold.Refilling)
            {
                // The wheel unwinds as the channel fills again behind it.
                angle=WheelTurns*Mathf.Clamp01((RefilledAt-Time.time)/Refill);
                if(Time.time>=RefilledAt){State=Hold.Ready;angle=0;Debug.Log($"SLUICE_FULL {index}");}
            }
            else if(Holder!=null)
            {
                turned+=dt;angle=WheelTurns*Turned;
                if(turned>=TurnSeconds){Open(Holder);return;}
            }
            else{turned=Mathf.Max(0,turned-dt*Drains);angle=WheelTurns*Turned;}
            Spin();
            Paint(Offered&&State==Hold.Ready,Holder!=null?1:0);
        }
        void Spin(){Measure();if(wheel!=null)wheel.localRotation=rest*Quaternion.AngleAxis(angle,Vector3.forward);}
        /// <summary>Turned all the way: the coolant goes, the boss is told, and it refills.</summary>
        void Open(CombatActor by)
        {
            State=Hold.Refilling;RefilledAt=Time.time+Refill;OpenedBy=by;Holder=null;turned=0;TimesOpened++;angle=WheelTurns;Spin();Paint(false,0);
            AudioManager.PlayAt("water_rush",transform,Spout,.45f,AudioBus.SFX,40,.1f,1.1f);
            CombatVfx.Burst(Spout,new Color(.75f,.95f,1f,.6f),"steam");
            Debug.Log($"SLUICE_OPEN {index}");
            Opened?.Invoke(this);
        }
        /// <summary>The seat's ring while it can be turned, and the turn filling it over the time it takes.</summary>
        void Paint(bool offered,int phase)
        {
            if(offered&&seatRing==null){seatRing=Telegraph.Circle(transform.position,seatRadius,9999,SeatTone);seatRing.Steady=true;seatRing.owner=this;}
            if(!offered&&seatRing!=null){seatRing.Cancel();seatRing=null;}
            if(phase!=fillPhase||fill==null&&phase>0)
            {
                if(fill!=null){fill.Cancel();fill=null;}
                fillPhase=phase;
                if(phase==1){fill=Telegraph.Circle(transform.position,seatRadius*.8f,Mathf.Max(.05f,TurnSeconds-turned),TurnTone);fill.owner=this;}
            }
        }
        protected override void OnEnable(){base.OnEnable();if(!All.Contains(this))All.Add(this);}
        protected override void OnDisable(){base.OnDisable();All.Remove(this);if(seatRing!=null){seatRing.Cancel();seatRing=null;}if(fill!=null){fill.Cancel();fill=null;}}
    }
}
