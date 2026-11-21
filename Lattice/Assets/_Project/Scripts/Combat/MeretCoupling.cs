using System;
using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the two arm couplings on Meret Venn's heavy frame (chapter 10, phase two: <c>BossController.MeretGantry.cs</c>): where her press arm or her welding arm joins the frame at the
    /// shoulder. It is opened, never broken ("You opened them. You didn't break them."), with the release the party has done since Oriel, in its two beats: while its arm is
    /// <b>overreached</b> (planted at the end of a slam that met nobody, or at full stretch at the end of a weld) the boss offers it, and its seat lights on the floor beside her shoulder as a pale
    /// ring. A hero in the seat takes its weight (<c>Take the collar's weight</c>, the Interact button, which answers in a fight), holds while the weight is taken, and eases it while the arm is
    /// still overreached; held through the ease, it opens and its arm hangs dead. A hero who rolls, strikes, is staggered, goes down or steps out of the seat lets go, and what he had taken
    /// drains away; an arm drawn back in before the ease is done takes the offer back with it. Opened, it tells the boss (<see cref="Opened"/>).
    /// <para>It stands on the frame's own transform (the frame doesn't walk while an arm is planted): its seat beside the shoulder, on her side, a hand's breadth in front of her.</para>
    /// Logs <c>COUPLING_TAKEN</c>, <c>COUPLING_DROPPED</c>, <c>COUPLING_EASED</c>, <c>COUPLING_OPEN &lt;arm&gt;</c>.
    /// </summary>
    public sealed class MeretCoupling:InteractionPrompt
    {
        public static readonly List<MeretCoupling> All=new();
        /// <summary>A coupling has just opened: its arm hangs.</summary>
        public static event Action<MeretCoupling> Opened;
        /// <summary>Which arm it holds: "press" (her right) or "welder" (her left).</summary>
        public string arm="press";
        /// <summary>The release's two beats (Oriel's), and how fast what was taken drains once nobody holds it.</summary>
        public const float TakeWeight=1.2f,EaseHold=.8f,Drains=1.5f,SeatRadius=1.5f;
        static readonly Color SeatTone=new(1f,.93f,.74f,.42f),WeightTone=new(.55f,.9f,1f,.45f),EaseTone=new(1f,.74f,.38f,.45f);
        public bool IsOpen{get;private set;}
        /// <summary>Its arm is overreached now: it can be worked, and it eases.</summary>
        public bool Offered{get;private set;}
        public CombatActor Holder{get;private set;}
        public CombatActor OpenedBy{get;private set;}
        public float Weight=>Mathf.Clamp01(weight/TakeWeight);
        public bool Carried=>weight>=TakeWeight-.0001f;
        public override bool InFight=>true;
        public override bool Available=>isActiveAndEnabled&&Offered&&!IsOpen&&Holder==null&&!GameTime.Paused;
        float weight,ease;bool easedSaid;Telegraph seatRing,fill;int fillPhase;
        void Awake(){prompt="Take the collar's weight";range=SeatRadius;}
        /// <summary>The boss offers it (its arm overreached) or takes the offer back (drawn in).</summary>
        public void Offer(bool open)
        {
            open&=!IsOpen;if(Offered==open)return;Offered=open;
            if(!open){Drop("withdrawn");weight=ease=0;}
        }
        public override void Interact()
        {
            if(!Available||PartyController.Current==null)return;
            Holder=PartyController.Current.Active;easedSaid=false;
            Debug.Log($"COUPLING_TAKEN {arm} by={Holder.character}");
            BarkService.Play(Holder.character,"clamp",true);
        }
        void Drop(string why)
        {
            if(Holder==null)return;
            Debug.Log($"COUPLING_DROPPED {arm} {why}");Holder=null;
        }
        bool Keeps(CombatActor hero)
        {
            if(hero==null||!hero.Health.Alive||hero.Recovering||PartyController.Current==null||PartyController.Current.Active!=hero)return false;
            var o=hero.transform.position-transform.position;o.y=0;if(o.magnitude>SeatRadius+.4f)return false;
            var state=hero.State;return state!=Lattice.Data.ActorState.Dodge&&state!=Lattice.Data.ActorState.Stagger&&state!=Lattice.Data.ActorState.Down&&state!=Lattice.Data.ActorState.Attack&&state!=Lattice.Data.ActorState.Skill;
        }
        void Update()
        {
            if(IsOpen){Paint(false,0);return;}
            if(Holder!=null&&!Keeps(Holder))Drop(Holder.Health.Alive?"let go":"fell");
            if(GameTime.CombatHeld)return;
            float dt=Time.deltaTime;
            if(Holder!=null&&Offered)
            {
                weight=Mathf.Min(TakeWeight,weight+dt);
                if(Carried)
                {
                    if(!easedSaid){easedSaid=true;Debug.Log("COUPLING_EASED "+arm);}
                    ease+=dt;if(ease>=EaseHold){Open(Holder);return;}
                }
            }
            else{weight=Mathf.Max(0,weight-dt*Drains);ease=0;}
            Paint(Offered,Holder==null?0:Carried&&ease>0?2:1);
        }
        void Paint(bool offered,int phase)
        {
            if(offered&&seatRing==null){seatRing=Telegraph.Circle(transform.position,SeatRadius,9999,SeatTone);seatRing.Steady=true;seatRing.owner=this;}
            if(!offered&&seatRing!=null){seatRing.Cancel();seatRing=null;}
            if(phase!=fillPhase||fill==null&&phase>0)
            {
                if(fill!=null){fill.Cancel();fill=null;}
                fillPhase=phase;
                if(phase==1)fill=Telegraph.Circle(transform.position,SeatRadius*.8f,Mathf.Max(.05f,TakeWeight-weight),WeightTone);
                else if(phase==2)fill=Telegraph.Circle(transform.position,SeatRadius*.8f,Mathf.Max(.05f,EaseHold-ease),EaseTone);
                if(fill!=null)fill.owner=this;
            }
        }
        void Open(CombatActor by)
        {
            IsOpen=true;OpenedBy=by;Holder=null;Offered=false;weight=ease=0;Paint(false,0);
            CombatVfx.Burst(transform.position+Vector3.up*1.6f,new Color(1f,.86f,.5f),"crack");
            AudioManager.PlayAt("doorOpen_000",transform,transform.position+Vector3.up,.32f,AudioBus.SFX,50,.05f,.55f);
            Debug.Log("COUPLING_OPEN "+arm);
            Opened?.Invoke(this);
        }
        /// <summary>Its frame is gone: nothing to offer, nothing drawn.</summary>
        public void Retire(){Offered=false;Holder=null;Paint(false,0);}
        protected override void OnEnable(){base.OnEnable();if(!All.Contains(this))All.Add(this);}
        protected override void OnDisable(){base.OnDisable();All.Remove(this);if(seatRing!=null)seatRing.Cancel();if(fill!=null)fill.Cancel();}
    }
}
