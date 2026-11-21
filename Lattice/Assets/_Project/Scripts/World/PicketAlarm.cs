using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A picket that wakes in stages (chapter 8's Picket of Teeth: staged detection), one to a zone. The lead ship inside a buoy's lit beam (<see cref="SensorSweep"/>) is heard: the
    /// <see cref="Heard"/> meter fills over <see cref="rise"/> seconds of being seen and drains a few seconds after the beam has gone, and full, the picket goes up a <see cref="Stage"/>.
    /// What each stage brings is shown before it is said: 1, <b>amber</b>, every buoy's lamp pulsing amber and its beam sweeping faster, back to quiet after <see cref="calm"/> seconds unseen;
    /// 2, a <b>hunter wing</b> off the carrier's deck (the next of <see cref="wings"/>), back to amber once that wing is broken, or lost (<see cref="lose"/> seconds unseen with nothing awake
    /// within <see cref="loseNear"/>); 3, the <b>carrier</b> swinging broadside with its lights gone red, another wing, the wall's flak gates awake (<see cref="flak"/>), and its guns laying
    /// marked bursts round the lead while it is in the field, for as long as the party stays in the zone. A buoy shot down gets its last call off (a stage), and the route lock's
    /// tamper alarm (<see cref="RouteLock"/>) brings at least the hunter wing.
    /// <para>Only the ship the player flies is watched: the partner flies by its own brain, and a picket woken by where it chose to fly would be the player's fault for nothing they did
    /// (the Spore Veil's lesson). Nothing is saved: a load starts quiet, and a wing broken stays broken (its encounter's own clear flag).</para>
    /// Logs <c>PICKET_SEEN</c>, <c>PICKET_STAGE n why</c>, <c>PICKET_WING id</c>, <c>PICKET_CARRIER</c>, <c>PICKET_BURST</c>.
    /// </summary>
    public sealed class PicketAlarm:MonoBehaviour
    {
        public static PicketAlarm Current{get;private set;}
        public const int Quiet=0,Amber=1,Hunters=2,Carrier=3;
        /// <summary>Seconds of being seen that fill the meter at each stage (quiet, amber, hunters).</summary>
        public float[] rise={1.4f,1.2f,1.6f};
        /// <summary>Seconds after the beam has gone that the meter holds before it drains, and how fast it drains.</summary>
        public float hold=2.5f,drain=.3f;
        /// <summary>Unseen this long, amber goes quiet; unseen this long with nothing awake this near, a hunter wing is lost.</summary>
        public float calm=8,lose=12,loseNear=25;
        /// <summary>How much faster a beam sweeps once the picket has half heard.</summary>
        public float alarmedPace=1.35f;
        /// <summary>The hunter wings, launched in turn (each is an encounter whose trigger nobody can fly into).</summary>
        public EncounterVolume[] wings=System.Array.Empty<EncounterVolume>();
        /// <summary>The wall's flak gates, woken by the carrier.</summary>
        public EncounterVolume flak;
        /// <summary>The carrier's drawn hull, the yaw it swings to at the third stage and how long it takes; its running lights, which go from <see cref="calmLight"/> to red.</summary>
        public Transform carrier;public float carrierTurn=-80,turnSeconds=9;public Light[] carrierLights=System.Array.Empty<Light>();public Color calmLight=new(.7f,.86f,1f,1);
        /// <summary>The carrier's bursts: where in z the lead must be for its guns to reach, how often, how wide, how long marked, and what a hit takes of a hero's bar.</summary>
        public float fieldFrom=260,fieldTo=660,burstEvery=3.2f,burstRadius=4.5f,burstWarn=1.4f,burstShare=.18f;
        public int Stage{get;private set;}
        /// <summary>0 to 1: how near the next stage being seen has brought the picket.</summary>
        public float Heard{get;private set;}
        /// <summary>A beam is on the lead ship this frame.</summary>
        public bool Seen{get;private set;}
        public float LastSeen{get;private set;}=float.NegativeInfinity;
        /// <summary>The wing out now (null with none).</summary>
        public EncounterVolume Wing=>wing;
        public static event System.Action<int> StageChanged;
        EncounterVolume wing;int launched;float turnFrom,turned=-1,nextBurst,changedAt;Quaternion carrierRest;bool seenBefore,wasHidden;float nextShadowBark;
        readonly List<(Telegraph mark,float at,Vector3 centre)> bursts=new();

        void OnEnable(){Current=this;SensorSweep.Silenced+=OnSilenced;}
        void OnDisable(){if(Current==this)Current=null;SensorSweep.Silenced-=OnSilenced;}
        void Start(){if(carrier!=null)carrierRest=carrier.rotation;foreach(var l in carrierLights)if(l!=null)l.color=calmLight;}
        void OnSilenced(SensorSweep sweep)=>Raise(Stage+1,"buoy down");
        /// <summary>The picket up to at least this stage, each stage between brought in turn.</summary>
        public void Raise(int atLeast,string why)
        {
            atLeast=Mathf.Min(Carrier,atLeast);
            while(Stage<atLeast)
            {
                Stage++;Heard=0;changedAt=Time.time;Debug.Log("PICKET_STAGE "+Stage+" "+why);
                var lead=Lead();string who=lead!=null?lead.character:"Taren";
                switch(Stage)
                {
                    case Amber:BarkService.Play(who,"picket.amber",true);AudioManager.Play("computerNoise_000",.3f);break;
                    case Hunters:Launch();BarkService.Play(who,"picket.hunters",true);break;
                    case Carrier:
                        Launch();if(flak!=null&&!flak.Started){flak.Begin();flakSaid=true;BarkService.Play(who,"flak");}
                        turned=0;turnFrom=Time.time;nextBurst=Time.time+burstWarn+1;Debug.Log("PICKET_CARRIER");
                        foreach(var l in carrierLights)if(l!=null){l.color=new Color(1,.18f,.1f);l.intensity*=1.4f;}
                        BarkService.Play(who,"picket.carrier",true);break;
                }
                StageChanged?.Invoke(Stage);
            }
        }
        void Lower(int to,string why)
        {
            if(to>=Stage)return;
            Stage=to;Heard=0;changedAt=Time.time;Debug.Log("PICKET_STAGE "+Stage+" "+why);StageChanged?.Invoke(Stage);
        }
        void Launch()
        {
            while(launched<wings.Length)
            {
                var next=wings[launched++];if(next==null)continue;
                // A wing broken in an earlier visit stays broken: its own clear flag answers Begin.
                bool broken=GameServices.Current!=null&&GameServices.Current.Flags.GetBool("clear."+next.encounterId);
                if(broken||next.Started)continue;
                next.Begin();wing=next;Debug.Log("PICKET_WING "+next.encounterId);
                AudioManager.PlayAt("thrusterFire_000",null,next.spawners.Length>0?next.spawners[0].transform.position:transform.position,.5f,AudioBus.SFX,60);
                return;
            }
        }
        static CombatActor Lead(){var party=PartyController.Current;return party!=null?party.Active:null;}
        void Update()
        {
            var lead=Lead();
            if(lead==null||!lead.flight||!lead.Health.Alive||GameTime.Paused){foreach(var s in SensorSweep.All)if(s!=null)s.Holding=false;return;}
            var at=lead.transform.position;Seen=false;
            foreach(var sweep in SensorSweep.All)
            {
                if(sweep==null)continue;
                sweep.Stage=Stage;sweep.Pace=Stage>=Amber?alarmedPace:1;
                sweep.Holding=sweep.Sees(at);if(sweep.Holding)Seen=true;
            }
            // Nothing is heard through a conversation: the hails and the lock's orders are said with the ship still in a beam.
            bool talking=GameServices.Current!=null&&GameServices.Current.Input.Blocked;
            if(Seen&&!talking)
            {
                if(!seenBefore)Debug.Log("PICKET_SEEN");seenBefore=true;
                LastSeen=Time.time;
                if(Stage<Carrier){Heard+=Time.deltaTime/Mathf.Max(.1f,rise[Mathf.Min(Stage,rise.Length-1)]);if(Heard>=1)Raise(Stage+1,"seen");}
            }
            else
            {
                seenBefore=false;
                if(Time.time-LastSeen>hold)Heard=Mathf.Max(0,Heard-drain*Time.deltaTime);
            }
            // Unseen since the beam last left the ship, or since the stage came, whichever is later: a stage brought by a buoy's last call or the lock, with the ship never in a
            // beam, holds as long as any other.
            float unseen=Time.time-Mathf.Max(LastSeen,changedAt);
            if(Stage==Amber&&unseen>calm&&Heard<=0)Lower(Quiet,"quiet");
            else if(Stage==Hunters)
            {
                if(wing==null||wing.Cleared)Lower(Amber,"wing broken");
                else if(unseen>lose&&!EnemyBrain.AwakeNear(at,loseNear))Lower(Amber,"wing lost");
            }
            // Into a shadow with the picket stirring or a beam just off the ship: the lead says so, now and then.
            bool hidden=!Seen&&SensorSweep.HiddenFromAll(at);
            if(hidden&&!wasHidden&&(Stage>=Amber||Time.time-LastSeen<5)&&Time.time>=nextShadowBark){BarkService.Play(lead.character,"shadow");nextShadowBark=Time.time+15;}
            wasHidden=hidden;
            Calls(lead);
            TurnCarrier();
            if(Stage==Carrier)Bursts(lead);
        }
        /// <summary>The lead's word as the flak gates wake (by the carrier or the lock), and as a Lancer lines up on the ship: the combat side says nothing here, every line is the zone's.</summary>
        void Calls(CombatActor lead)
        {
            if(flak!=null&&flak.Started&&!flakSaid){flakSaid=true;if(Stage<Carrier)BarkService.Play(lead.character,"flak");}
            if(Time.time<nextLancerLook)return;nextLancerLook=Time.time+.5f;
            if(Time.time<nextLancerBark)return;
            foreach(var brain in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                if(brain==null||!brain.enabled||brain.definition==null||brain.definition.id!="MusterLancer"||!brain.Telegraphing)continue;
                var gap=brain.transform.position-lead.transform.position;gap.y=0;if(gap.sqrMagnitude>30*30)continue;
                BarkService.Play(lead.character,"lancer");nextLancerBark=Time.time+12;break;
            }
        }
        bool flakSaid;float nextLancerLook,nextLancerBark;
        void TurnCarrier()
        {
            if(carrier==null||turned<0||turned>=1)return;
            turned=Mathf.Clamp01((Time.time-turnFrom)/Mathf.Max(.1f,turnSeconds));
            carrier.rotation=Quaternion.Euler(0,carrierTurn*Mathf.SmoothStep(0,1,turned),0)*carrierRest;
        }
        void Bursts(CombatActor lead)
        {
            var at=lead.transform.position;
            for(int i=bursts.Count-1;i>=0;i--)
            {
                var (mark,when,centre)=bursts[i];if(mark==null){bursts.RemoveAt(i);continue;}
                if(Time.time<when)continue;
                bursts.RemoveAt(i);mark.Release();Land(mark,centre);
            }
            if(at.z<fieldFrom||at.z>fieldTo||Time.time<nextBurst)return;
            nextBurst=Time.time+burstEvery;
            // Where the ship will be when it lands, as the guns lead it, a little off so a ship that keeps turning is missed.
            var ahead=at;if(lead.TryGetComponent<FlightMotor>(out var motor))ahead+=new Vector3(motor.Velocity.x,0,motor.Velocity.z)*burstWarn;
            ahead+=new Vector3(Random.Range(-1.5f,1.5f),0,Random.Range(-1.5f,1.5f));ahead.y=1;
            var marking=Telegraph.Circle(ahead,burstRadius,burstWarn,Telegraph.Threat);bursts.Add((marking,Time.time+burstWarn,ahead));
            Debug.Log("PICKET_BURST "+ahead.ToString("0.0"));
        }
        void Land(Telegraph mark,Vector3 centre)
        {
            AudioManager.PlayAt("explosionCrunch_000",null,centre,.45f,AudioBus.SFX,60);CombatVfx.Burst(centre,new Color(1,.6f,.3f,.8f),"spark");
            var party=PartyController.Current;if(party==null)return;
            foreach(var hero in party.members)
            {
                if(hero==null||!hero.Health.Alive||!mark.Contains(hero.transform.position))continue;
                // The place's damage, as a share of the bar and never the last of it: the carrier sets the party back, it doesn't end the crossing.
                float amount=Mathf.Min(hero.Health.maximum*burstShare,hero.Health.integrity-1);
                if(amount>0)hero.Health.Receive(new DamagePacket{amount=amount,type=DamageType.Kinetic});
            }
        }
    }
}
