using System;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// The anchor chamber's lift (chapter 10, docs/maps/StillwaterChamber.md, "The lift"): a cage on an inclined rail from the frame gantry's deck down through the hull to the control
    /// well's door. Its prompt stands on the cage at the top (<c>Ride down — The Control Well</c>) and is refused, saying why, until <see cref="requiredFlag"/> (her frame is down). Pressed,
    /// both heroes out step into the cage and are carried down <see cref="path"/> with it, side by side, off their own controls; at the foot they are set down, the way's spawn becomes
    /// the checkpoint and the game saves. <b>A conversation that holds the controls holds the lift</b> (D275): the ride doesn't block the controls itself, it takes the heroes off them, and
    /// its clock and the cage stand still for as long as anything else holds them (a talk, a menu, a pause), then go on. The shaft's floor beside the rail is a walked way too (the bake's,
    /// so the partner and every reach test know the way), shut at its head by <see cref="gate"/> until the lift may run. When her frame is beaten on the gantry the cage goes down with
    /// her (her BossExit stands on it and she sinks: <see cref="sinkAfter"/>) and comes back up empty a few seconds later; a load with the flag finds it at the top.
    /// Logs <c>CHAMBER_LIFT_REFUSED</c>, <c>CHAMBER_LIFT_RIDE</c>, <c>CHAMBER_LIFT_HELD</c>, <c>CHAMBER_LIFT_DOWN</c>, <c>CHAMBER_LIFT_TAKES_HER</c>.
    /// </summary>
    public sealed class ChamberLift:InteractionPrompt
    {
        public string requiredFlag="bossdown.MeretGantry",lockedPrompt="The lift won't run while her frame is still up",spawn="Well";
        /// <summary>What the prompt says while the cage is away, gone down with her and coming back up empty: the lift's head was offered nothing for those fourteen seconds, and a party
        /// stood at it with no word of why (MeretReplayTests: the route waited them out).</summary>
        public string awayPrompt="The cage is on its way back up";
        /// <summary>The cage's floor, top first: the deck, the shaft's head, down the incline, the foot.</summary>
        public Vector3[] path=Array.Empty<Vector3>();
        public float seconds=8;
        /// <summary>The cage (drawn only: the deck and the shaft's floor are what bodies stand on).</summary>
        public Transform car;
        /// <summary>The shaft's head, shut until the lift may run (opened by this, never by a fight).</summary>
        public Membrane gate;
        /// <summary>Her going down with it: from the flag, how long until the cage starts down, how far along the path it goes (out of sight in the shaft), how long that takes, and how
        /// long after the flag it comes back up empty and how long the climb takes.</summary>
        public float sinkAfter=2.6f,sinkTo=.28f,sinkSeconds=2.2f,backAfter=9,backSeconds=5;
        /// <summary>The ride is over: for a test, or anything that wants the moment.</summary>
        public static event Action<ChamberLift> Carried;
        public bool Riding{get;private set;}
        /// <summary>The ride is standing still because something else holds the controls.</summary>
        public bool Held{get;private set;}
        /// <summary>How far down the path the cage stands (0 the top, 1 the foot).</summary>
        public float Travel{get;private set;}
        /// <summary>A test's switch for the old fault (a lift that ran on through a conversation): never set in a scene.</summary>
        [NonSerialized] public bool runsThroughTalks;
        bool Locked=>!string.IsNullOrEmpty(requiredFlag)&&GameServices.Current!=null&&!GameServices.Current.Flags.GetBool(requiredFlag);
        public override bool Available=>!Riding&&!BossController.AnyFightUnderway&&!EnemyBrain.AwakeNear(transform.position,EnemyBrain.ExitCalm)&&(Locked||!taking||Away)&&!(silentWhileAway&&Away);
        /// <summary>A test's switch for the old fault (nothing offered at the lift's head while the cage was away): never set in a scene.</summary>
        [NonSerialized] public bool silentWhileAway;
        /// <summary>The cage has left the top with her (from <see cref="sinkAfter"/>) and is down the shaft or coming back up: the prompt says so and a press is refused. While she steps
        /// onto it nothing is offered, as before.</summary>
        public bool Away=>taking&&!Locked&&taken>=sinkAfter;
        string openPrompt;float clock,taken=-1;bool unsetSeen,taking,known;int gateWas=-1;
        CombatActor[] bodies=Array.Empty<CombatActor>();CharacterController[] controllers=Array.Empty<CharacterController>();bool[] partnerOn=Array.Empty<bool>(),pilotWas=Array.Empty<bool>();Vector3[] offsets=Array.Empty<Vector3>();
        void Awake(){openPrompt=prompt;}
        void Start(){Pose(0);}
        public override void Interact()
        {
            if(Locked){prompt=lockedPrompt;Debug.Log("CHAMBER_LIFT_REFUSED "+requiredFlag);return;}
            if(Away){Debug.Log("CHAMBER_LIFT_AWAY");return;}
            if(!Available||path.Length<2||PartyController.Current==null||PartyController.Current.Active==null)return;
            var party=PartyController.Current;Riding=true;clock=0;Held=false;
            bodies=(CombatActor[])party.members.Clone();controllers=new CharacterController[bodies.Length];partnerOn=new bool[bodies.Length];pilotWas=new bool[bodies.Length];offsets=new Vector3[bodies.Length];
            var across=Across();int other=0;
            for(int i=0;i<bodies.Length;i++)
            {
                var hero=bodies[i];if(hero==null)continue;
                controllers[i]=hero.GetComponent<CharacterController>();if(controllers[i]!=null)controllers[i].enabled=false;
                if(hero.TryGetComponent<PartnerBrain>(out var partner)){partnerOn[i]=partner.enabled;partner.enabled=false;}
                if(hero.TryGetComponent<PlayerBrain>(out var pilot)){pilotWas[i]=pilot.AutoPilot;pilot.AutoPilot=true;}
                (hero.motor as GroundMotor)?.Halt();
                // The lead in the middle of the cage, whoever else beside him: as two people step into a lift.
                offsets[i]=hero==party.Active?Vector3.zero:across*(1.4f+.3f*other++);
            }
            AudioManager.PlayAt("doorOpen_000",transform,path[0]+Vector3.up,.3f,AudioBus.SFX,40,.05f,.7f);
            Debug.Log("CHAMBER_LIFT_RIDE "+name);
        }
        /// <summary>Across the cage, to the lead's right as he rides down it.</summary>
        Vector3 Across(){var along=path.Length>1?path[^1]-path[0]:Vector3.forward;along.y=0;return along.sqrMagnitude<.01f?Vector3.right:new Vector3(along.z,0,-along.x).normalized;}
        /// <summary>Anything but the ride holds the controls: a conversation, a menu, a pause.</summary>
        static bool HeldElsewhere=>GameTime.Paused||GameServices.Current!=null&&GameServices.Current.Input.Blocked;
        void Update()
        {
            var services=GameServices.Current;if(services==null)return;
            bool locked=Locked;
            // The gate follows the lock as it changes (shut at the first frame while locked), and is otherwise left as it stands.
            if(gate!=null&&(gateWas<0||(gateWas==1)!=locked)){gate.SetOpen(!locked);gateWas=locked?1:0;}
            if(!locked){string says=Away?awayPrompt:openPrompt;if(prompt!=says)prompt=says;}
            if(Riding){Ride();return;}
            // Her going down with it: once, in play, a beat after her frame is beaten.
            bool down=!string.IsNullOrEmpty(requiredFlag)&&services.Flags.GetBool(requiredFlag);
            if(!known){known=true;unsetSeen=!down;}
            // Unset again (a test, a load), her going is owed again from the start.
            if(!down){unsetSeen=true;if(!taking)taken=-1;return;}
            if(!unsetSeen)return;
            if(taken<0){taken=0;taking=true;}
            if(!GameTime.Paused)taken+=Time.deltaTime;
            float t=taken<sinkAfter?0:taken<sinkAfter+sinkSeconds?Mathf.SmoothStep(0,sinkTo,(taken-sinkAfter)/sinkSeconds):taken<backAfter?sinkTo:Mathf.SmoothStep(sinkTo,0,(taken-backAfter)/backSeconds);
            if(taken>=sinkAfter&&Travel==0&&t>0)Debug.Log("CHAMBER_LIFT_TAKES_HER");
            Pose(t);
            if(taken>=backAfter+backSeconds){taking=false;unsetSeen=false;Pose(0);}
            // Its words in the frame the cage leaves the top or comes back, not the frame after.
            if(!locked){string says=Away?awayPrompt:openPrompt;if(prompt!=says)prompt=says;}
        }
        void Ride()
        {
            Held=!runsThroughTalks&&HeldElsewhere;
            if(Held){if(!heldSaid){heldSaid=true;Debug.Log("CHAMBER_LIFT_HELD");}Carry();return;}
            heldSaid=false;
            clock+=Time.deltaTime;float u=Mathf.Clamp01(clock/Mathf.Max(.1f,seconds));
            // Eased like a cage taking its weight and setting down: slow off the deck, steady down the shaft, slow at the foot.
            Pose(u*u*(3-2*u));Carry();
            if(u>=1)Arrive();
        }
        bool heldSaid;
        void Carry()
        {
            var floor=At(Travel,out var along);
            for(int i=0;i<bodies.Length;i++)
            {
                var hero=bodies[i];if(hero==null)continue;
                hero.transform.position=floor+offsets[i]+Vector3.up*.04f;
                var face=along;face.y=0;if(face.sqrMagnitude>.01f)hero.transform.rotation=Quaternion.RotateTowards(hero.transform.rotation,Quaternion.LookRotation(face),360*Time.unscaledDeltaTime);
            }
            Physics.SyncTransforms();
        }
        void Arrive()
        {
            Riding=false;Held=false;
            for(int i=0;i<bodies.Length;i++)
            {
                var hero=bodies[i];if(hero==null)continue;
                if(controllers[i]!=null)controllers[i].enabled=true;
                if(hero.TryGetComponent<PartnerBrain>(out var partner))partner.enabled=partnerOn[i];
                if(hero.TryGetComponent<PlayerBrain>(out var pilot))pilot.AutoPilot=pilotWas[i];
                if(hero.TryGetComponent<GroundMotor>(out var feet)){feet.Halt();feet.KeepFacing(hero.transform.forward);}
            }
            Physics.SyncTransforms();
            var services=GameServices.Current;
            if(services!=null&&!string.IsNullOrEmpty(spawn)){services.State.spawn=spawn;services.Saves.Save("autosave",services.State);}
            AudioManager.PlayAt("impactMetal_000",transform,path[^1]+Vector3.up,.22f,AudioBus.SFX,40,.05f,.6f);
            Debug.Log("CHAMBER_LIFT_DOWN "+name);
            Carried?.Invoke(this);
        }
        /// <summary>The cage a share of the way down.</summary>
        void Pose(float t)
        {
            Travel=t;if(car==null||path.Length<2)return;
            var at=At(t,out _);car.position=at;
        }
        /// <summary>The point a share of the way along the path by length, and the way it runs there.</summary>
        public Vector3 At(float t,out Vector3 along)
        {
            along=Vector3.forward;if(path.Length==0)return transform.position;if(path.Length==1)return path[0];
            float total=0;for(int i=1;i<path.Length;i++)total+=Vector3.Distance(path[i-1],path[i]);
            float metres=Mathf.Clamp01(t)*total;
            for(int i=1;i<path.Length;i++)
            {
                float leg=Vector3.Distance(path[i-1],path[i]);along=leg>0?(path[i]-path[i-1])/leg:along;
                if(metres<=leg||i==path.Length-1)return Vector3.Lerp(path[i-1],path[i],leg>0?Mathf.Clamp01(metres/leg):1);
                metres-=leg;
            }
            return path[^1];
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            if(!Riding)return;
            // Gone mid-ride (a load): nobody is left off their own controls.
            for(int i=0;i<bodies.Length;i++)
            {
                var hero=bodies[i];if(hero==null)continue;
                if(controllers[i]!=null)controllers[i].enabled=true;
                if(hero.TryGetComponent<PartnerBrain>(out var partner))partner.enabled=partnerOn[i];
                if(hero.TryGetComponent<PlayerBrain>(out var pilot))pilot.AutoPilot=pilotWas[i];
            }
            Riding=false;
        }
    }
}
