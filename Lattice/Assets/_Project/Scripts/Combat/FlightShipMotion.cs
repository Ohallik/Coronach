using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Presentation only: bank and roll the generated ship without rotating its collider.</summary>
    public sealed class FlightShipMotion : MonoBehaviour
    {
        CombatActor actor;FormController form;
        float previousYaw, bank, rollStart = -10;
        ActorState previousState;
        void OnEnable()
        {
            actor = GetComponentInParent<CombatActor>();form=GetComponentInParent<FormController>();
            previousYaw = actor != null ? actor.transform.eulerAngles.y : 0;
            bank = 0; previousState = ActorState.Idle; rollStart = -10;
        }
        float joltPitch, joltRoll, joltAge = 1, joltLife = 1;
        /// <summary>A hit on the hull: it rolls and pitches away from the blow and rocks back, on the drawn ship only. A shot used to take integrity off a craft that flew on as if nothing had touched it.</summary>
        public void Jolt(Vector3 along, float degrees, float seconds)
        {
            along.y = 0; if (actor == null || along.sqrMagnitude < .0001f || degrees <= 0 || seconds <= 0) return;
            if (joltAge < joltLife) { float left = 1 - joltAge / joltLife; if (Mathf.Max(Mathf.Abs(joltPitch), Mathf.Abs(joltRoll)) * left * left > degrees) return; }
            along.Normalize();
            // From the side it rolls, from ahead or astern it pitches.
            joltRoll = -Vector3.Dot(along, actor.transform.right) * degrees; joltPitch = Vector3.Dot(along, actor.transform.forward) * degrees * .6f;
            joltLife = seconds; joltAge = 0;
        }
        void LateUpdate()
        {
            if (actor == null || (form!=null&&form.Shaping) || GameTime.Paused || !actor.Health.Alive || actor.Recovering) return;
            float dt = GameTime.Delta;
            float yaw = actor.transform.eulerAngles.y;
            // A hit-stop holds the bank where it was: with no real time counted, the turn that made it would read as none at all.
            if (dt > 0)
            {
                float turn = Mathf.DeltaAngle(previousYaw, yaw) / Mathf.Max(.001f, dt);
                bank = Mathf.Lerp(bank, Mathf.Clamp(-turn * .12f, -28, 28), 1 - Mathf.Exp(-9 * dt));
            }
            previousYaw = yaw;
            if (actor.State == ActorState.Dodge && previousState != ActorState.Dodge) rollStart = GameTime.Now;
            previousState = actor.State;
            float phase = Mathf.Clamp01((GameTime.Now - rollStart) / .3f);
            float roll = phase < 1 ? Mathf.SmoothStep(0, 360, phase) : 0;
            float speed = actor.motor != null ? actor.motor.Velocity.magnitude : 0;
            float rock = 0;
            if (joltAge < joltLife)
            {
                // Deepest on the frame of the hit, which a stop then holds; rocked back and once over on the pilot's own clock.
                float left = 1 - joltAge / joltLife; rock = left * left * Mathf.Cos(joltAge / joltLife * Mathf.PI * 3); joltAge += dt;
            }
            transform.localRotation = Quaternion.Euler(-Mathf.Clamp(speed / 5, 0, 5) + joltPitch * rock, 0, bank + roll + joltRoll * rock);
        }
    }
}
