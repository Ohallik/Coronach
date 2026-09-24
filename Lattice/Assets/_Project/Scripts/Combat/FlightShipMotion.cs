using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Presentation only: bank and roll the generated ship without rotating its collider.</summary>
    public sealed class FlightShipMotion : MonoBehaviour
    {
        CombatActor actor;
        float previousYaw, bank, rollStart = -10;
        ActorState previousState;
        void OnEnable()
        {
            actor = GetComponentInParent<CombatActor>();
            previousYaw = actor != null ? actor.transform.eulerAngles.y : 0;
            bank = 0; previousState = ActorState.Idle; rollStart = -10;
        }
        void LateUpdate()
        {
            if (actor == null || GameTime.Paused) return;
            float yaw = actor.transform.eulerAngles.y;
            float turn = Mathf.DeltaAngle(previousYaw, yaw) / Mathf.Max(.001f, Time.unscaledDeltaTime);
            previousYaw = yaw;
            bank = Mathf.Lerp(bank, Mathf.Clamp(-turn * .12f, -28, 28), 1 - Mathf.Exp(-9 * Time.unscaledDeltaTime));
            if (actor.State == ActorState.Dodge && previousState != ActorState.Dodge) rollStart = GameTime.Now;
            previousState = actor.State;
            float phase = Mathf.Clamp01((GameTime.Now - rollStart) / .3f);
            float roll = phase < 1 ? Mathf.SmoothStep(0, 360, phase) : 0;
            float speed = actor.motor != null ? actor.motor.Velocity.magnitude : 0;
            transform.localRotation = Quaternion.Euler(-Mathf.Clamp(speed / 5, 0, 5), 0, bank + roll);
        }
    }
}
