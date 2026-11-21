using System;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// The last collar on Stillwater's anchor (chapter 10, docs/maps/StillwaterChamber.md, "The anchor"; wiring 6.6): Taren's, because a lock is a lock. Meret's page says it in three lines and the
    /// hold does them in order. <b>Take up the load</b>: a hero in the seat takes it (<c>Take the last collar</c>, Interact) and his hands draw its weight over <see cref="TakeSeconds"/>.
    /// <b>Hold for the slack</b>: the anchor pulls for twelve seconds and eases for three on its own clock (<see cref="AnchorRelease"/>, shown on the collar's lamps and the chain's strain;
    /// Idra calls it), and the collar waits in his hands through the pull. <b>Release all clamps as one</b>: held through a whole ease, from its first moment to its last, it opens: its flag is
    /// set (told to the quests, saved), and the anchor lets go. A roll, a swing, a skill, a stagger, a fall or a step out of the seat lets go, and what was taken drains; it can be taken
    /// again at once. An ease that began before the load was taken doesn't count: the next one does. Offered from <see cref="requiredFlag"/> (Idra's "Ease.") until it opens; a load with
    /// its flag finds it open, with nothing played. Logs <c>COLLAR_TAKEN</c>, <c>COLLAR_LOADED</c>, <c>COLLAR_RIDES_THE_EASE</c>, <c>COLLAR_LET_GO &lt;why&gt;</c>, <c>COLLAR_OPEN</c>.
    /// </summary>
    public sealed class LastCollar:InteractionPrompt
    {
        public string flag="end.collarTaken",requiredFlag="end.sequenceRead",report="LastCollar";
        public AnchorRelease anchor;
        /// <summary>How far from the seat's middle a hero may stand and still have his hands on the collar.</summary>
        public float seatRadius=1.6f;
        public const float TakeSeconds=1.5f,Drains=1.5f;
        /// <summary>The collar's own lamps: dark while it isn't taken, warming as the load comes on, white through the ease he holds it through (never a danger's colour).</summary>
        public Renderer[] lamp=Array.Empty<Renderer>();public Light lampLight;
        [ColorUsage(false,true)] public Color idle=new(.1f,.24f,.3f),loaded=new(.4f,1.3f,1.5f),riding=new(1.7f,1.7f,1.75f);
        static readonly Color SeatTone=new(1f,.93f,.74f,.42f),TakeTone=new(.55f,.9f,1f,.45f),EaseTone=new(.92f,.96f,1f,.5f);
        static readonly int EmissionId=Shader.PropertyToID("_EmissionColor");
        public static event Action<LastCollar> Opened;
        public CombatActor Holder{get;private set;}
        public bool IsOpen{get;private set;}
        /// <summary>His hands are on it with its load taken, waiting for the slack.</summary>
        public bool Loaded=>Holder!=null&&taken>=TakeSeconds;
        /// <summary>He has held it from the first moment of the ease running now.</summary>
        public bool RidingTheEase{get;private set;}
        public float Taken=>Mathf.Clamp01(taken/TakeSeconds);
        public override bool Available=>isActiveAndEnabled&&!IsOpen&&Holder==null&&!GameTime.Paused&&Ready;
        bool Ready=>GameServices.Current==null||string.IsNullOrEmpty(requiredFlag)||GameServices.Current.Flags.GetBool(requiredFlag);
        float taken;int easeSeen=-1;Telegraph seatRing,fill;int fillPhase;MaterialPropertyBlock block;
        void Awake(){prompt="Take the last collar";range=seatRadius;}
        void Start()
        {
            var services=GameServices.Current;IsOpen=services!=null&&services.Flags.GetBool(flag);Paint(false,0);Light(0);
        }
        public override void Interact()
        {
            if(!Available||PartyController.Current==null)return;
            Holder=PartyController.Current.Active;RidingTheEase=false;easeSeen=anchor!=null?anchor.Eases:-1;
            Debug.Log($"COLLAR_TAKEN by={Holder.character}");
            AudioManager.PlayAt("doorOpen_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,40,.05f,.55f);
        }
        void Drop(string why){if(Holder==null)return;Debug.Log("COLLAR_LET_GO "+why);Holder=null;RidingTheEase=false;}
        /// <summary>His hands are still on it: alive, the one the player is playing, in the seat, and not rolling, striking, staggered or down.</summary>
        bool Keeps(CombatActor hero)
        {
            if(hero==null||!hero.Health.Alive||hero.Recovering||PartyController.Current==null||PartyController.Current.Active!=hero)return false;
            var o=hero.transform.position-transform.position;o.y=0;if(o.magnitude>seatRadius+.4f)return false;
            var state=hero.State;return state!=ActorState.Dodge&&state!=ActorState.Stagger&&state!=ActorState.Down&&state!=ActorState.Attack&&state!=ActorState.Skill;
        }
        void Update()
        {
            if(IsOpen){Paint(false,0);return;}
            if(Holder!=null&&!Keeps(Holder))Drop(Holder.Health.Alive?"let go":"fell");
            // Nothing moves while a conversation, a menu or a pause holds the controls: the anchor's own clock waits with it.
            if(GameTime.CombatHeld){Paint(Available||Holder!=null,fillPhase);return;}
            float dt=Time.deltaTime;bool easing=anchor!=null&&anchor.Easing;int eases=anchor!=null?anchor.Eases:0;
            if(Holder!=null)
            {
                bool was=Loaded;taken=Mathf.Min(TakeSeconds,taken+dt);if(!was&&Loaded)Debug.Log("COLLAR_LOADED");
                // An ease is ridden only from its first moment, with the load already taken.
                if(eases!=easeSeen){easeSeen=eases;RidingTheEase=easing&&Loaded;if(RidingTheEase)Debug.Log("COLLAR_RIDES_THE_EASE "+eases);}
                if(RidingTheEase&&!easing){Open(Holder);return;}
            }
            else{taken=Mathf.Max(0,taken-dt*Drains);RidingTheEase=false;}
            Paint(Available||Holder!=null,Holder==null?0:RidingTheEase?2:1);
            Light(Holder==null?0:RidingTheEase?2:Loaded?1:Taken);
        }
        void Open(CombatActor by)
        {
            IsOpen=true;Holder=null;RidingTheEase=false;Paint(false,0);Light(2);
            var services=GameServices.Current;
            if(services!=null)
            {
                services.Flags.SetBool(flag,true);RpgServices.Quests.Report(ObjectiveKind.Interact,report);RpgServices.Quests.Report(ObjectiveKind.Flag,flag);
                services.Saves.Save("autosave",services.State);
            }
            AudioManager.PlayAt("joint_release",transform,transform.position+Vector3.up,.3f,AudioBus.SFX,40);
            Debug.Log("COLLAR_OPEN by="+(by!=null?by.character:"nobody"));
            Opened?.Invoke(this);
        }
        void Light(float level)
        {
            block??=new MaterialPropertyBlock();
            var colour=level<=1?Color.Lerp(idle,loaded,Mathf.Clamp01(level)):riding;
            foreach(var r in lamp){if(r==null)continue;r.GetPropertyBlock(block);block.SetColor(EmissionId,colour);r.SetPropertyBlock(block);}
            if(lampLight!=null){lampLight.color=level<=1?new Color(.55f,.9f,1):new Color(.95f,.97f,1);lampLight.intensity=Mathf.Lerp(.3f,2.2f,Mathf.Clamp01(level*.6f));}
        }
        /// <summary>The seat's ring while it can be taken; the load's fill while he takes it; the ease's while he rides one out.</summary>
        void Paint(bool offered,int phase)
        {
            if(offered&&seatRing==null){seatRing=Telegraph.Circle(transform.position,seatRadius,9999,SeatTone);seatRing.Steady=true;seatRing.owner=this;}
            if(!offered&&seatRing!=null){seatRing.Cancel();seatRing=null;}
            if(phase==fillPhase&&(fill!=null||phase==0))return;
            if(fill!=null){fill.Cancel();fill=null;}
            fillPhase=phase;
            if(phase==1){fill=Telegraph.Circle(transform.position,seatRadius*.8f,Mathf.Max(.05f,TakeSeconds-taken),TakeTone);fill.owner=this;}
            if(phase==2){fill=Telegraph.Circle(transform.position,seatRadius*.8f,Mathf.Max(.05f,anchor!=null?anchor.EaseLeft:.1f),EaseTone);fill.owner=this;}
        }
        protected override void OnDisable(){base.OnDisable();Holder=null;if(seatRing!=null){seatRing.Cancel();seatRing=null;}if(fill!=null){fill.Cancel();fill=null;}}
    }
}
