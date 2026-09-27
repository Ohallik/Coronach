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
        Vector3 previous;
        string state;
        int sequence = -1;
        int emitterLayer=-1,emitterSequence=-1;
        int recoilLayer=-1,recoilSequence=-1;
        bool upperShot;
        float emitterWeight;
        readonly Dictionary<string, float> lengths = new();
        public GroundStrideProfile strideProfile;
        public string CurrentAnimation => upperShot?"Shoot":state;
        public string LocomotionAnimation => state;
        public int ActionLayer => upperShot?emitterLayer:0;
        public float StrideScale {get;private set;}=1;
        public bool ReverseLocomotion {get;private set;}
        public bool IsLocomotion => state=="Idle"||state=="Walk"||state=="Run"||state=="Sprint";

        void OnEnable() { previous = transform.position; state = null; sequence = emitterSequence = recoilSequence = -1;upperShot=false;emitterWeight=0;GameTime.PauseChanged+=OnPause; }
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
                var feet=GetComponent<GroundFeet>()??gameObject.AddComponent<GroundFeet>();
                feet.Initialize(animator,actor.transform,strideProfile);
            }
        }
        void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            if (form != null && form.Shaping) { animator.speed = 0; previous = transform.position; return; }
            if (GameTime.Paused) { animator.speed = 0; previous = transform.position; return; }
            float speed = actor?.motor != null ? actor.motor.Velocity.magnitude :
                Vector3.Distance(transform.position, previous) / Mathf.Max(.001f, Time.deltaTime);
            previous = transform.position;
            string wanted = speed > .12f ? "Walk" : "Idle";
            float rate = speed > .12f ? Mathf.Clamp(speed / 2.1f, .7f, 1.5f) : 1;
            bool visual = false;
            upperShot=false;
            if (actor != null)
            {
                if (speed > 7.5f) { wanted = "Sprint"; rate = Mathf.Clamp(speed / 7.4f, .8f, 1.55f); }
                else if (speed > 3.1f) { wanted = "Run"; rate = Mathf.Clamp(speed / 4.5f, .8f, 1.6f); }
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
            }
            bool down=actor!=null&&!actor.Health.Alive||enemy!=null&&!enemy.Health.Alive;
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
            StrideScale=1;ReverseLocomotion=false;
            if(strideProfile!=null&&actor?.motor!=null)
            {
                if(travelling)
                {
                    rate=strideProfile.Cadence(wanted,speed);
                    StrideScale=strideProfile.Stride(wanted,speed,rate);
                    ReverseLocomotion=actor.TargetLocked&&Vector3.Dot(actor.transform.forward,actor.motor.Velocity.normalized)<-.15f;
                }
                // A state speed multiplier supports reverse locomotion while
                // the Animator's global speed and the action clocks stay positive.
                animator.SetFloat("TravelSign",ReverseLocomotion?-1:1);
            }
            bool fullAction=visual&&!upperShot;
            bool nextAction = fullAction && sequence != actor.AttackSequence;
            if (state != wanted || nextAction)
            {
                state = wanted;
                if (actor != null) sequence = actor.AttackSequence;
                var ground=actor?.motor as GroundMotor;
                float blend=ground!=null&&ground.Phase==GroundMotor.TravelPhase.Starting?.075f:
                    wanted=="Idle"?.14f:.1f;
                animator.CrossFadeInFixedTime(wanted, fullAction ? .045f : blend, 0, 0);
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
