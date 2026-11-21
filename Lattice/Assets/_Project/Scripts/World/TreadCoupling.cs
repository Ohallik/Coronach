using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// One of tread-hall four's three couplings (chapter 9's Terminator, docs/maps/Terminator.md): where a car's drag cable has parted from its drawbar socket, heavy town machinery lashed
    /// and chocked by people who expect it to shake loose. It stands on its <b>seat</b> (this object: the floor a hero stands on at the jack, which is where its prompt is). A hero in the
    /// seat takes it (<c>Re-mate the coupling</c>, the Interact button) and works the jack while his hands stay on it: the jack's wheel winds, the drawbar's eye comes round and in to the
    /// socket. A roll, a swing, a skill, a stagger, a fall or a step out of the seat lets go, and what was wound runs back. Wound all the way (<see cref="HoldSeconds"/>) it is mated: the
    /// pin drops, the chocks go in, the lashings come taut, its lamp goes from amber to white, its flag is set (a world flag, told to the quests), its spawn becomes the run's checkpoint and
    /// the game saves; the lead says the coupling's line (<c>remate</c>, by its number) and Dorrow answers on the radio (<c>hail.remate</c>). While anyone's hands are on one, the dawn holds
    /// where it stands (<see cref="HandsOn"/>; <see cref="DawnLine"/>). Offered only once the coupling behind it is mated (<see cref="requiredFlag"/>), so they go in order and the checkpoint is
    /// always the furthest one. A load with its flag set has it mated, with nothing played. Logs <c>COUPLING_TAKEN &lt;n&gt;</c>, <c>COUPLING_LET_GO &lt;n&gt; &lt;why&gt;</c>,
    /// <c>COUPLING_MATED &lt;n&gt;</c>.
    /// </summary>
    public sealed class TreadCoupling:InteractionPrompt
    {
        public static readonly List<TreadCoupling> All=new();
        /// <summary>A coupling has just been mated.</summary>
        public static event Action<TreadCoupling> Mated;
        /// <summary>Which coupling this is, from the back of the hall forward (1 the schoolroom car's).</summary>
        public int index=1;
        /// <summary>The flag it sets, the one it waits on (the coupling behind it; empty for the first), and the spawn its mating makes the checkpoint.</summary>
        public string flag="home.coupling1",requiredFlag="",spawn="";
        /// <summary>How far from the seat's middle a hero may stand and still have his hands on the jack.</summary>
        public float seatRadius=1.8f;
        /// <summary>How long the jack takes to wind with hands on it; how fast what was wound runs back with none, as a share of the rate it was wound; how long the pin, chocks and lashings
        /// take once it is wound (nobody's hands needed).</summary>
        public const float HoldSeconds=3.2f,Drains=1.5f,GiveSeconds=1.2f;
        /// <summary>The drawbar's eye as it lies parted, from where it sits mated: a slide and a slew about its own up. Mated is where the builder stood it.</summary>
        public Transform drawbar;public Vector3 parted=new(.9f,0,-1.1f);public float slew=-14;
        /// <summary>The pin over the socket (dropped by <see cref="pinDrop"/>), the jack's wheel (turned about its own forward), the chocks (each driven in along its own forward).</summary>
        public Transform pin,wheel;public float pinDrop=.7f,wheelTurns=900;
        public Transform[] chocks=Array.Empty<Transform>();public float chockTravel=.45f;
        /// <summary>The lashings slack until it is mated, and taut after.</summary>
        public GameObject slack,taut;
        /// <summary>Its lamp: amber while it waits, white once mated (never a danger's colour), and its light.</summary>
        public Renderer[] lamp=Array.Empty<Renderer>();public Light lampLight;
        [ColorUsage(false,true)] public Color waiting=new(1.6f,1.05f,.35f),mated=new(1.5f,1.6f,1.7f);
        /// <summary>The seat: a pale ring in the party's cream while it can be taken, the wind filling it in coolant blue. Boundaries, never a danger's colour (D217).</summary>
        static readonly Color SeatTone=new(1f,.93f,.74f,.42f),WindTone=new(.55f,.9f,1f,.45f);
        static readonly int EmissionId=Shader.PropertyToID("_EmissionColor");
        public CombatActor Holder{get;private set;}
        public bool IsMated{get;private set;}
        /// <summary>How far the jack is wound (0 to 1).</summary>
        public float Wound=>Mathf.Clamp01(wound/HoldSeconds);
        /// <summary>Anyone's hands are on a coupling: the dawn holds.</summary>
        public static bool HandsOn{get{foreach(var c in All)if(c!=null&&c.Holder!=null)return true;return false;}}
        /// <summary>The next coupling to mate (the first not mated), or null once all three are.</summary>
        public static TreadCoupling Next{get{TreadCoupling best=null;foreach(var c in All)if(c!=null&&!c.IsMated&&(best==null||c.index<best.index))best=c;return best;}}
        public static int MatedCount{get{int n=0;foreach(var c in All)if(c!=null&&c.IsMated)n++;return n;}}
        public override bool Available=>isActiveAndEnabled&&!IsMated&&Holder==null&&!GameTime.Paused&&Ready;
        bool Ready=>GameServices.Current==null||string.IsNullOrEmpty(requiredFlag)||GameServices.Current.Flags.GetBool(requiredFlag);
        float wound,given=-1,answerAt=-1;Vector3 barRest,pinRest;Quaternion barTurn,wheelRest;Vector3[] chockRest=Array.Empty<Vector3>();bool measured;Telegraph seatRing,fill;int fillPhase;MaterialPropertyBlock block;
        void Awake(){prompt="Re-mate the coupling";range=seatRadius;Measure();}
        void Measure()
        {
            if(measured)return;measured=true;
            // In each part's parent's space: the ride carries the whole hall south, and a load after it has every car where the ride left it before this is posed.
            if(drawbar!=null){barRest=drawbar.localPosition;barTurn=drawbar.localRotation;}
            if(pin!=null)pinRest=pin.localPosition;if(wheel!=null)wheelRest=wheel.localRotation;
            chockRest=new Vector3[chocks.Length];for(int i=0;i<chocks.Length;i++)if(chocks[i]!=null)chockRest[i]=chocks[i].localPosition;
        }
        void Start()
        {
            var services=GameServices.Current;
            // A loaded save is a quiet state: mated, with nothing played.
            IsMated=services!=null&&services.Flags.GetBool(flag);given=IsMated?GiveSeconds:-1;Pose();
        }
        public override void Interact()
        {
            if(!Available||PartyController.Current==null)return;
            Holder=PartyController.Current.Active;
            Debug.Log($"COUPLING_TAKEN {index} by={Holder.character}");
            AudioManager.PlayAt("doorOpen_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,40,.05f,.6f);
        }
        void Drop(string why){if(Holder==null)return;Debug.Log($"COUPLING_LET_GO {index} {why}");Holder=null;}
        /// <summary>His hands are still on it: alive, the one the player is playing, in the seat, and not rolling, striking, staggered or down.</summary>
        bool Keeps(CombatActor hero)
        {
            if(hero==null||!hero.Health.Alive||hero.Recovering||PartyController.Current==null||PartyController.Current.Active!=hero)return false;
            var o=hero.transform.position-transform.position;o.y=0;if(o.magnitude>seatRadius+.4f)return false;
            var state=hero.State;return state!=ActorState.Dodge&&state!=ActorState.Stagger&&state!=ActorState.Down&&state!=ActorState.Attack&&state!=ActorState.Skill;
        }
        void Update()
        {
            if(answerAt>0&&Time.unscaledTime>=answerAt){answerAt=-1;BarkService.PlayAt("Dorrow","hail.remate",index-1,true);}
            if(IsMated)
            {
                if(given>=0&&given<GiveSeconds){given+=Time.deltaTime;Pose();}
                return;
            }
            if(Holder!=null&&!Keeps(Holder))Drop(Holder.Health.Alive?"let go":"fell");
            if(GameTime.CombatHeld)return;
            float dt=Time.deltaTime;
            if(Holder!=null){wound+=dt;if(wound>=HoldSeconds){Mate(Holder);return;}}
            else wound=Mathf.Max(0,wound-dt*Drains);
            Pose();Paint(Available||Holder!=null,Holder!=null?1:0);
        }
        /// <summary>Wound all the way: the pin, the chocks, the lashings; the flag, the checkpoint and the save; the lines.</summary>
        void Mate(CombatActor by)
        {
            IsMated=true;Holder=null;wound=HoldSeconds;given=0;Paint(false,0);
            var services=GameServices.Current;
            if(services!=null)
            {
                services.Flags.SetBool(flag,true);RpgServices.Quests.Report(ObjectiveKind.Flag,flag);
                if(!string.IsNullOrEmpty(spawn))services.State.spawn=spawn;
                services.Saves.Save("autosave",services.State);
            }
            AudioManager.PlayAt("explosionCrunch_000",transform,(pin!=null?pin.position:transform.position)+Vector3.up*.3f,.34f,AudioBus.SFX,40,.05f,.7f);
            CombatVfx.Burst((pin!=null?pin.position:transform.position)+Vector3.up*.4f,new Color(.75f,.72f,.7f,.6f),"steam");
            if(by!=null)BarkService.PlayAt(by.character,"remate",index-1,true);
            answerAt=Time.unscaledTime+3;
            Debug.Log($"COUPLING_MATED {index}");
            Mated?.Invoke(this);
        }
        /// <summary>The machinery as the wind and the give have it.</summary>
        void Pose()
        {
            Measure();
            float wind=IsMated?1:Wound,give=IsMated?(given<0?1:Mathf.Clamp01(given/GiveSeconds)):0;
            float bring=Mathf.SmoothStep(0,1,wind);
            if(drawbar!=null)
            {
                var offset=barTurn*parted*(1-bring);
                drawbar.localPosition=barRest+offset;drawbar.localRotation=Quaternion.AngleAxis(slew*(1-bring),Vector3.up)*barTurn;
            }
            if(wheel!=null)wheel.localRotation=wheelRest*Quaternion.AngleAxis(wheelTurns*wind,Vector3.forward);
            if(pin!=null)pin.localPosition=pinRest+Up(pin)*pinDrop*(1-Mathf.Clamp01(give*2));
            for(int i=0;i<chocks.Length;i++)if(chocks[i]!=null)chocks[i].localPosition=chockRest[i]-chocks[i].localRotation*Vector3.forward*chockTravel*(1-Mathf.Clamp01(give*2-.6f));
            if(slack!=null)slack.SetActive(give<1);if(taut!=null)taut.SetActive(give>=1);
            block??=new MaterialPropertyBlock();var colour=IsMated?Color.Lerp(waiting,mated,give):waiting*(.75f+.25f*Mathf.Sin(Time.time*3.2f));
            foreach(var r in lamp){if(r==null)continue;r.GetPropertyBlock(block);block.SetColor(EmissionId,colour);r.SetPropertyBlock(block);}
            if(lampLight!=null)lampLight.color=IsMated?Color.Lerp(new Color(1,.7f,.35f),new Color(.9f,.95f,1),give):new Color(1,.7f,.35f);
        }
        static Vector3 Up(Transform t)=>t.parent!=null?t.parent.InverseTransformDirection(Vector3.up):Vector3.up;
        /// <summary>The seat's ring while it can be taken, and the wind filling it over the time it takes.</summary>
        void Paint(bool offered,int phase)
        {
            if(offered&&seatRing==null){seatRing=Telegraph.Circle(transform.position,seatRadius,9999,SeatTone);seatRing.Steady=true;seatRing.owner=this;}
            if(!offered&&seatRing!=null){seatRing.Cancel();seatRing=null;}
            if(phase!=fillPhase||fill==null&&phase>0)
            {
                if(fill!=null){fill.Cancel();fill=null;}
                fillPhase=phase;
                if(phase==1){fill=Telegraph.Circle(transform.position,seatRadius*.8f,Mathf.Max(.05f,HoldSeconds-wound),WindTone);fill.owner=this;}
            }
        }
        protected override void OnEnable(){base.OnEnable();if(!All.Contains(this))All.Add(this);}
        protected override void OnDisable(){base.OnDisable();All.Remove(this);Holder=null;if(seatRing!=null){seatRing.Cancel();seatRing=null;}if(fill!=null){fill.Cancel();fill=null;}}
    }
}
