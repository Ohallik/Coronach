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
        readonly Dictionary<string, float> lengths = new();
        public string CurrentAnimation => state;

        void OnEnable() { previous = transform.position; state = null; sequence = -1; }
        void Start()
        {
            animator = GetComponentInChildren<Animator>();
            actor = GetComponentInParent<CombatActor>();
            enemy = GetComponentInParent<EnemyBrain>();
            form = GetComponentInParent<FormController>();
            previous = transform.position;
            if (animator != null && actor != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
        void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            if (GameTime.Paused) { animator.speed = 0; previous = transform.position; return; }
            float speed = actor?.motor != null ? actor.motor.Velocity.magnitude :
                Vector3.Distance(transform.position, previous) / Mathf.Max(.001f, Time.deltaTime);
            previous = transform.position;
            string wanted = speed > .12f ? "Walk" : "Idle";
            float rate = speed > .12f ? Mathf.Clamp(speed / 2.1f, .7f, 1.5f) : 1;
            bool visual = false;
            if (actor != null)
            {
                if (speed > 7.5f) { wanted = "Sprint"; rate = Mathf.Clamp(speed / 7.4f, .8f, 1.55f); }
                else if (speed > 3.1f) { wanted = "Run"; rate = Mathf.Clamp(speed / 4.5f, .8f, 1.6f); }
                if (actor.State == ActorState.Guard) { wanted = "Guard"; rate = 1; }
                if (actor.State == ActorState.Stagger) { wanted = "Stagger"; rate = 1; }
                visual = GameTime.Now < actor.VisualAttackUntil && actor.State != ActorState.Stagger && actor.State != ActorState.Guard;
                if (visual) wanted = actor.VisualAction;
                if (form != null && (form.Current == BodyForm.Flight || form.Current == BodyForm.CivilFlight)) { wanted = "Idle"; visual = false; }
            }
            else if (enemy != null && enemy.Attacking) wanted = "Attack";
            if (!animator.HasState(0, Animator.StringToHash(wanted))) wanted = speed > .12f ? "Walk" : "Idle";
            bool nextAction = visual && sequence != actor.AttackSequence;
            if (state != wanted || nextAction)
            {
                state = wanted;
                if (actor != null) sequence = actor.AttackSequence;
                animator.CrossFadeInFixedTime(wanted, visual ? .045f : .12f, 0, 0);
            }
            if (visual)
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
        }
    }
}
