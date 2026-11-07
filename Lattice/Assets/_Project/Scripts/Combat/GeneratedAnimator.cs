using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    public sealed class GeneratedAnimator : MonoBehaviour
    {
        Animator animator;
        CombatActor actor;
        EnemyBrain enemy;
        FormController form;
        GroundFeet feet;
        Vector3 previous;
        string state;
        string pendingGait;
        float pendingGaitSince;
        int sequence = -1;
        int emitterLayer=-1,emitterSequence=-1;
        int recoilLayer=-1,recoilSequence=-1;
        bool upperShot;
        float emitterWeight;
        /// <summary>A shaped hero's Idle is a guard stance, Sword_Idle or Pistol_Idle_Loop, played empty-handed: out of a fight it read as a mannequin and as an arm stuck above her head. CalmIdle
        /// is the plain Idle_Loop. <see cref="state"/> stays "Idle" for both, so the feet and everything reading it don't change; this is what the Animator was asked to show.</summary>
        const string CalmState="CalmIdle";
        string shown;
        bool hasCalm,calm;
        float standingSince,nextCalmCheck;
        /// <summary>Long enough that a step and a stop doesn't change stance, and past any brake.</summary>
        const float CalmAfter=.6f;
        /// <summary>The retargeted idle holds a person's arms out from the sides. In idle only each upper arm is turned toward hanging, by this much, easing in and out so walking and swinging are untouched.</summary>
        const float ArmHang=.85f,ArmEaseSeconds=.2f,ArmLetGoSeconds=.06f;
        Transform leftArm,rightArm,leftFore,rightFore,neck,head;
        readonly Transform[] back=new Transform[3];
        float armHang,slump;
        bool beaten;
        /// <summary>The frame a captain gives up in: the first buckle of her fall, knees bent and both feet down, which reads from every side a talk can turn her to. No clip has a kneel of its own
        /// (tools/blender/clip_poses.py). The slump laid over it does the rest.</summary>
        public const string YieldState="Down";public const float YieldPhase=.29f;
        /// <summary>How far a beaten body bows at each joint, degrees, from the hips up.</summary>
        static readonly float[] BackBow={12,16,10};
        const float NeckBow=12,HeadBow=22;
        readonly Dictionary<string, float> lengths = new();
        public GroundStrideProfile strideProfile;
        public string CurrentAnimation => upperShot?"Shoot":state;
        public string LocomotionAnimation => state;
        public int ActionLayer => upperShot?emitterLayer:0;
        public float StrideScale {get;private set;}=1;
        public bool ReverseLocomotion {get;private set;}
        public bool IsLocomotion => state=="Idle"||state=="Walk"||state=="Run"||state=="Sprint";
        /// <summary>Travelling gait for a hero's measured planar speed. The owned walk stretches cleanly to about 2.5 m/s;
        /// above that a short-stride jog reads better than an over-reaching walk (D197).</summary>
        public const float RunAbove=2.5f,SprintAbove=7.5f;
        public static string Gait(float speed)=>speed>SprintAbove?"Sprint":speed>RunAbove?"Run":"Walk";

        void OnEnable() { previous = transform.position; state = pendingGait = null; sequence = emitterSequence = recoilSequence = -1;upperShot=false;emitterWeight=0;GameTime.PauseChanged+=OnPause; }
        void OnDisable(){GameTime.PauseChanged-=OnPause;}
        void OnPause(bool paused){if(paused&&animator!=null)animator.speed=0;}
        void Start()
        {
            animator = GetComponentInChildren<Animator>();
            actor = GetComponentInParent<CombatActor>();
            enemy = GetComponentInParent<EnemyBrain>();
            form = GetComponentInParent<FormController>();
            previous = transform.position;
            if (animator != null && actor != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            if(animator!=null)emitterLayer=animator.GetLayerIndex("Emitter");
            if(animator!=null)recoilLayer=animator.GetLayerIndex("Recoil");
            if(animator!=null&&actor!=null&&strideProfile!=null)
            {
                feet=GetComponent<GroundFeet>()??gameObject.AddComponent<GroundFeet>();
                feet.Initialize(animator,actor.transform,strideProfile);
            }
            if(animator!=null)hasCalm=animator.HasState(0,Animator.StringToHash(CalmState));
            // People: heroes, townsfolk and whoever gives up in the end. A Husk's arms were built to stand off its body.
            if(animator!=null&&animator.isHuman&&(enemy==null||enemy.definition.yieldsOnDefeat))
            {
                leftArm=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);rightArm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                leftFore=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);rightFore=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                back[0]=animator.GetBoneTransform(HumanBodyBones.Spine);back[1]=animator.GetBoneTransform(HumanBodyBones.Chest);back[2]=animator.GetBoneTransform(HumanBodyBones.UpperChest);
                neck=animator.GetBoneTransform(HumanBodyBones.Neck);head=animator.GetBoneTransform(HumanBodyBones.Head);
            }
        }
        /// <summary>The person who takes over from a fighter who gave up: the caller holds the recoil, this lays the slump over it from the first frame on.</summary>
        public void HoldBeaten(){beaten=true;slump=1;}
        bool Calm()
        {
            // A body with no party around it is a fixture holding its stance; the stride tests are all of those.
            if(actor==null||PartyController.Current==null||Time.unscaledTime-standingSince<CalmAfter)return false;
            if(Time.unscaledTime>=nextCalmCheck)
            {
                nextCalmCheck=Time.unscaledTime+.4f;
                calm=!actor.TargetLocked&&GameTime.Now-actor.VisualAttackUntil>CalmAfter&&!BossController.AnyFightUnderway&&!EnemyBrain.AwakeNear(actor.transform.position,EnemyBrain.InteractionCalm);
            }
            return calm;
        }
        void LateUpdate()
        {
            if(animator==null||leftArm==null||rightArm==null||leftFore==null||rightFore==null||!animator.isActiveAndEnabled)return;
            float dt=GameTime.Paused?0:Time.unscaledDeltaTime;
            if(beaten||enemy!=null)
            {
                slump=Mathf.MoveTowards(slump,beaten||!enemy.Health.Alive?1:0,dt/.35f);
                if(slump>0)Slump(slump);
                return;
            }
            bool resting=state=="Idle"&&(shown==CalmState||!hasCalm)&&!upperShot&&emitterWeight<.01f&&(form==null||!form.Shaping);
            // Quick to let go: a swing's edge lands .15 s in, and the blade is read from this same arm.
            armHang=Mathf.MoveTowards(armHang,resting?1:0,dt/(resting?ArmEaseSeconds:ArmLetGoSeconds));
            if(armHang>0){Hang(leftArm,leftFore,-1,armHang*ArmHang);Hang(rightArm,rightFore,1,armHang*ArmHang);}
        }
        void Hang(Transform arm,Transform fore,float side,float weight)
        {
            var along=fore.position-arm.position;if(along.sqrMagnitude<1e-6f)return;
            // A hand's width out from straight down, or it hangs through a wide hip.
            arm.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(along,Vector3.down+transform.right*(side*.2f)),weight)*arm.rotation;
        }
        /// <summary>Beaten: the back rounds, the head drops, and whatever the clip left in the air comes down.</summary>
        void Slump(float weight)
        {
            var bow=transform.right;
            for(int i=0;i<back.Length;i++)if(back[i]!=null)back[i].rotation=Quaternion.AngleAxis(BackBow[i]*weight,bow)*back[i].rotation;
            if(neck!=null)neck.rotation=Quaternion.AngleAxis(NeckBow*weight,bow)*neck.rotation;
            if(head!=null)head.rotation=Quaternion.AngleAxis(HeadBow*weight,bow)*head.rotation;
            Hang(leftArm,leftFore,-1,weight*.9f);Hang(rightArm,rightFore,1,weight*.9f);
        }
        void Update()
        {
            if (beaten || animator == null || !animator.isActiveAndEnabled) return;
            if (form != null && form.Shaping) { animator.speed = 0; previous = transform.position; return; }
            if (GameTime.Paused) { animator.speed = 0; previous = transform.position; return; }
            float speed = actor?.motor != null ? actor.motor.Velocity.magnitude :
                Vector3.Distance(transform.position, previous) / Mathf.Max(.001f, Time.deltaTime);
            previous = transform.position;
            string wanted = speed > .12f ? "Walk" : "Idle";
            // A walking civilian with a measured stride profile plays its walk at that profile's speed, so its feet
            // keep pace with the ground (D195); bodies without one keep the original estimate.
            float rate = speed > .12f ? (actor == null && strideProfile != null ? Mathf.Clamp(speed / strideProfile.walkSpeed, .5f, 1.6f) : Mathf.Clamp(speed / 2.1f, .7f, 1.5f)) : 1;
            bool visual = false;
            upperShot=false;
            if (actor != null)
            {
                if (speed > SprintAbove) { wanted = "Sprint"; rate = Mathf.Clamp(speed / 7.4f, .8f, 1.55f); }
                else if (speed > RunAbove) { wanted = "Run"; rate = Mathf.Clamp(speed / 4.5f, .8f, 1.6f); }
                if (actor.State == ActorState.Guard) { wanted = "Guard"; rate = 1; }
                if (actor.State == ActorState.Stagger) { wanted = "Stagger"; rate = 1; }
                visual = GameTime.Now < actor.VisualAttackUntil && actor.State != ActorState.Stagger && actor.State != ActorState.Guard;
                upperShot=visual&&actor.character=="Sela"&&!actor.flight&&actor.VisualAction=="Shoot"&&emitterLayer>=0;
                if (visual&&!upperShot) wanted = actor.VisualAction;
                if (form != null && (form.Current == BodyForm.Flight || form.Current == BodyForm.CivilFlight)) { wanted = "Idle"; visual = false; }
            }
            else if(enemy!=null)
            {
                if(enemy.Health.Broken&&animator.HasState(0,Animator.StringToHash("Stagger")))
                {
                    wanted="Stagger";
                    if(!lengths.TryGetValue(wanted,out float length))
                    {
                        foreach(var clip in animator.runtimeAnimatorController.animationClips)if(clip.name==wanted){length=clip.length;break;}
                        if(length>0)lengths[wanted]=length;
                    }
                    // Reach the recoiled pose in 0.22 seconds, then hold the
                    // authored body until gameplay break recovery begins.
                    rate=length>0?length*.25f/.22f:1;
                    if(state==wanted&&!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=.25f)rate=0;
                }
                else if(enemy.Attacking)wanted="Attack";
                else if(enemy.Telegraphing&&animator.HasState(0,Animator.StringToHash("Attack")))
                {
                    // The tell is the clip's wind-up, drawn out over the marking: the swing carries straight on from there at the strike.
                    wanted="Attack";
                    if(!lengths.TryGetValue(wanted,out float length))
                    {
                        // A clip keeps its take's name (Armature|...|Sword_Attack), whatever the state that plays it is called.
                        foreach(var clip in animator.runtimeAnimatorController.animationClips)if(clip.name.EndsWith("Attack")){length=clip.length;break;}
                        if(length>0)lengths[wanted]=length;
                    }
                    rate=GameTime.CombatHeld?0:length>0?length*EnemyBrain.TellShare/Mathf.Max(.1f,enemy.TellDuration):1;
                }
            }
            bool down=actor!=null&&!actor.Health.Alive||enemy!=null&&!enemy.Health.Alive&&!enemy.definition.yieldsOnDefeat;
            // She gives up low, and the person who takes over holds the same frame.
            if(enemy!=null&&!enemy.Health.Alive&&enemy.definition.yieldsOnDefeat)
            {
                wanted=YieldState;rate=1;
                if(state==wanted&&!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=YieldPhase)rate=0;
            }
            bool gettingUp=actor!=null&&actor.Recovering;
            if(down||gettingUp)
            {
                wanted=down?"Down":"Revive";visual=false;upperShot=false;
                if(!lengths.TryGetValue(wanted,out float length))
                {
                    foreach(var clip in animator.runtimeAnimatorController.animationClips)if(clip.name==wanted){length=clip.length;break;}
                    if(length>0)lengths[wanted]=length;
                }
                float duration=gettingUp?CombatActor.ReviveDuration:actor!=null?CombatActor.DownDuration:1.15f;
                rate=length>0?length/duration:1;
                if(down&&state=="Down"&&!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1)rate=0;
            }
            if (!animator.HasState(0, Animator.StringToHash(wanted))) wanted = speed > .12f ? "Walk" : "Idle";
            bool travelling=wanted=="Walk"||wanted=="Run"||wanted=="Sprint";
            if(actor!=null&&strideProfile!=null&&travelling&&(state=="Walk"||state=="Run"||state=="Sprint"))
            {
                if(wanted!=state)
                {
                    if(pendingGait!=wanted){pendingGait=wanted;pendingGaitSince=Time.unscaledTime;}
                    // Hold brief slowdowns so every shot does not interrupt
                    // the legs twice. Restored speed needs the faster gait
                    // immediately: a stretched walk cannot hold its planted
                    // sole through a collision rebound at running speed.
                    bool accelerating=strideProfile.NativeSpeed(wanted)>strideProfile.NativeSpeed(state);
                    if(!accelerating&&Time.unscaledTime-pendingGaitSince<.12f)wanted=state;
                }
                else pendingGait=null;
            }
            else pendingGait=null;
            StrideScale=1;ReverseLocomotion=false;
            if(strideProfile!=null&&actor?.motor!=null)
            {
                if(travelling)
                {
                    rate=strideProfile.Cadence(wanted,speed,feet!=null?feet.TerrainGrade:0);
                    StrideScale=strideProfile.Stride(wanted,speed,rate,feet!=null?feet.TerrainGrade:0);
                    ReverseLocomotion=actor.TargetLocked&&Vector3.Dot(actor.transform.forward,actor.motor.Velocity.normalized)<-.15f;
                }
                // A state speed multiplier supports reverse locomotion while
                // the Animator's global speed and the action clocks stay positive.
                animator.SetFloat("TravelSign",ReverseLocomotion?-1:1);
            }
            bool fullAction=visual&&!upperShot;
            bool nextAction = fullAction && sequence != actor.AttackSequence;
            if(wanted!="Idle"||upperShot)standingSince=Time.unscaledTime;
            string pose=wanted=="Idle"&&hasCalm&&Calm()?CalmState:wanted;
            bool settling=state==wanted&&shown!=pose;
            if (state != wanted || nextAction || settling)
            {
                // A pace change continues the same left/right step. Repeated
                // shots can cross Walk/Run thresholds every recovery; restarting
                // each take pinned the legs to the start of their gait forever.
                float phase=-1,priorDuration=1;
                if(travelling&&(state=="Walk"||state=="Run"||state=="Sprint"))
                {
                    var prior=animator.IsInTransition(0)?animator.GetNextAnimatorStateInfo(0):animator.GetCurrentAnimatorStateInfo(0);
                    phase=Mathf.Repeat(prior.normalizedTime,1);priorDuration=Mathf.Max(.01f,prior.length);
                }
                bool slopedStartStop=state=="Idle"&&wanted=="Walk"||state=="Walk"&&wanted=="Idle";
                state = wanted;shown=pose;
                if (actor != null) sequence = actor.AttackSequence;
                var ground=actor?.motor as GroundMotor;
                float blend=ground!=null&&ground.Phase==GroundMotor.TravelPhase.Starting?.075f:
                    wanted=="Idle"?.14f:.1f;
                // A steep support stance needs time to transfer weight into
                // or out of the donor walk. Root response stays immediate;
                // lengthen only this pose blend, leaving other gaits/actions.
                if(slopedStartStop&&feet!=null)
                    blend=Mathf.Lerp(blend,wanted=="Idle"?.3f:.2f,
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.45f,feet.TerrainGrade)));
                // The cycle offset stays normalized while cadence changes.
                // Converting it to fixed seconds produced phase jumps when a
                // crossfade was interrupted by the next shot's speed change.
                if(phase>=0)animator.CrossFade(wanted,blend*rate/priorDuration,0,phase);
                else animator.CrossFadeInFixedTime(pose, fullAction ? .045f : settling ? .35f : blend, 0, 0);
            }
            if (fullAction)
            {
                if (!lengths.TryGetValue(wanted, out float length))
                {
                    foreach (var clip in animator.runtimeAnimatorController.animationClips)
                        if (clip.name == wanted) { length = clip.length; break; }
                    if (length > 0) lengths[wanted] = length;
                }
                rate = length > 0 ? length / Mathf.Max(.1f, actor.VisualDuration) : 1;
            }
            animator.speed = rate;
            if(recoilLayer>=0&&enemy!=null)
            {
                float elapsed=Time.time-enemy.ImpactStarted;
                bool recoil=enemy.Health.Alive&&!enemy.Health.Broken&&elapsed>=0&&elapsed<EnemyBrain.ImpactDuration;
                float weight=recoil?Mathf.Min(Mathf.Clamp01(elapsed/.035f),Mathf.Clamp01((EnemyBrain.ImpactDuration-elapsed)/.12f)):0;
                animator.SetLayerWeight(recoilLayer,weight*(enemy.ArmoredImpact?.45f:1));
                if(recoil)
                {
                    if(!lengths.TryGetValue("Stagger",out float length))
                    {
                        foreach(var clip in animator.runtimeAnimatorController.animationClips)if(clip.name=="Stagger"){length=clip.length;break;}
                        if(length>0)lengths["Stagger"]=length;
                    }
                    // The full owned take contains the chest recoil near its
                    // end. Reach it in 160 ms, then let the layer fade away.
                    animator.SetFloat("RecoilRate",length/.16f/Mathf.Max(.01f,rate));
                    if(recoilSequence!=enemy.ImpactSequence)
                    {recoilSequence=enemy.ImpactSequence;animator.Play("Recoil",recoilLayer,0);}
                }
            }
            if(emitterLayer>=0)
            {
                bool interrupted=actor==null||!actor.Health.Alive||actor.Recovering||actor.State==ActorState.Guard||actor.State==ActorState.Stagger||actor.State==ActorState.Dodge||actor.flight;
                emitterWeight=interrupted?0:Mathf.MoveTowards(emitterWeight,upperShot?1:0,Time.unscaledDeltaTime/.045f);
                animator.SetLayerWeight(emitterLayer,emitterWeight);
                if(upperShot)
                {
                    if(!lengths.TryGetValue("Shoot",out float length))
                    {
                        foreach(var clip in animator.runtimeAnimatorController.animationClips)if(clip.name=="Shoot"){length=clip.length;break;}
                        if(length>0)lengths["Shoot"]=length;
                    }
                    // Global speed belongs to stride; the shot layer retains
                    // its own action duration through a relative multiplier.
                    animator.SetFloat("EmitterRate",length/Mathf.Max(.1f,actor.VisualDuration)/Mathf.Max(.01f,rate));
                    if(emitterSequence!=actor.AttackSequence)
                    {emitterSequence=actor.AttackSequence;animator.Play("Shoot",emitterLayer,0);}
                }
            }
        }
    }
}
