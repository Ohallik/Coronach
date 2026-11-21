using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.UI
{
    // The driver's reading of a room that runs on a clock or is worked by hand (chapter 8): a way it waits on until it is clear, a wheel a
    // boss's fight is worked with, and a part of a boss that can be hurt while its body can't. Ordinary pad presses and what a player reads off
    // the screen, as everywhere in the driver: the game still does to the party whatever it would do to a person who misjudged it.
    public sealed partial class ContinuousReview
    {
        // ---------- a way the room times: "until":"clear:x,z;x,z" ----------
        /// <summary>How often the way is looked at while the party waits for it: a buoy's beam moves a degree or two in a tenth of a second.</summary>
        const double ClearLook = .1;
        string clearFor; double clearNext = -1; bool clearWas; string clearBlocked;
        /// <summary>The way from where the lead stands through these points is clear of every beam, crust, pour and press, setting off now (ReplayWays). A step that waits on it holds
        /// the pad still (and the brake, in flight, if it says so) and ends the moment it is true; the walk after it goes.</summary>
        bool ClearAhead(string points)
        {
            double now = clock.Elapsed.TotalSeconds;
            if (points == clearFor && now < clearNext) return clearWas;
            clearFor = points; clearNext = now + ClearLook;
            var lead = PartyController.Current != null ? PartyController.Current.Active : null;
            string blocked = lead != null && lead.Health.Alive && !GameTime.Paused ? ReplayWays.Blocked(lead, ReplayWays.Points(points)) : "nobody to go";
            if (blocked != clearBlocked && blocked != null) Debug.Log("QUALITY_WAIT " + blocked + " on the way to " + points);
            if (blocked == null && clearBlocked != null) Debug.Log("QUALITY_CLEAR " + points);
            clearBlocked = blocked; clearWas = blocked == null;
            return clearWas;
        }

        // ---------- a wheel the boss's fight is worked with (the Foundry's heart) ----------
        /// <summary>How near the seat's middle the lead stands to turn its wheel: well inside its 1.6 m, with room for a shove.</summary>
        const float WheelStand = .8f;
        bool wheelPress;
        /// <summary>The driver as it was before these two hands, for a test's broken state (HarrowgateReplayTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool roomHandsOff;
        /// <summary>In a fight with nothing of the boss that can be hurt, a sluice's seat ring is what the room says to do: the nearest one offered, or the one the lead already has
        /// his hands on. Its rings show only while the Colossus glows and no plate is dark (CoolantSluice.Offer), so this is that and nothing else.</summary>
        static CoolantSluice WheelToWork(CombatActor lead)
        {
            if (roomHandsOff) return null;
            CoolantSluice best = null; float least = float.PositiveInfinity;
            foreach (var sluice in CoolantSluice.All)
            {
                if (sluice == null || !sluice.isActiveAndEnabled) continue;
                if (sluice.Holder == lead) return sluice;
                if (!sluice.Available) continue;
                var gap = sluice.transform.position - lead.transform.position; gap.y = 0;
                if (gap.sqrMagnitude < least) { least = gap.sqrMagnitude; best = sluice; }
            }
            return best;
        }
        /// <summary>To the seat by the floor's own way, then still in it with the interact button pressed as a thumb does until the hands are on the wheel, then still with nothing
        /// pressed until it opens: a swing, a skill or a step out lets it go. Nothing of the step's own buttons is pressed meanwhile.</summary>
        Vector2 WorkWheel(CombatActor lead, CoolantSluice wheel)
        {
            wheelPress = false;
            if (wheel.Holder == lead) return Vector2.zero;
            var gap = wheel.transform.position - lead.transform.position; gap.y = 0;
            if (gap.magnitude > WheelStand)
            {
                var heading = ReviveHeading(lead, wheel.transform.position, gap, false);
                var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * heading.normalized;
                return new Vector2(local.x, local.z);
            }
            wheelPress = (clock.Elapsed.TotalSeconds - stepStart) % .5 < .12;
            return Vector2.zero;
        }
        /// <summary>The lead's hands are on a wheel: the driver's own presses (an item, the EXTRA) would let it go.</summary>
        static bool AtTheWheel(PartyController party) => party != null && party.Active != null && CoolantSluice.Held != null && CoolantSluice.Held.Holder == party.Active;

        // ---------- a part of a boss to strike while its body takes nothing ----------
        double partPressAt = -1;
        /// <summary>The locked target takes nothing now (its plate turns every blow, or its bar is held at a line) and a part of the same boss can be hurt (a quenched heat plate, a
        /// pour-arm): a person flicks the lock across to it with the d-pad, once in a while until it is there. Bosses whose armour only lessens a blow are left as they were.</summary>
        bool SwitchToPart(CombatActor lead)
        {
            var target = lead != null ? lead.target : null;
            if (roomHandsOff || target == null || !target.Alive || Hurtable(target, lead)) return false;
            var boss = target.GetComponentInParent<BossController>(); if (boss == null) return false;
            bool part = false;
            foreach (var body in boss.GetComponentsInChildren<Health>())
            {
                if (body == null || body == target || body.friendly) continue;
                var gap = body.transform.position - lead.transform.position; gap.y = 0;
                if (gap.sqrMagnitude < 30 * 30 && Hurtable(body, lead)) { part = true; break; }
            }
            if (!part) return false;
            double now = clock.Elapsed.TotalSeconds;
            if (now - partPressAt > .4) partPressAt = now;
            return now - partPressAt < .12;
        }
        static bool Hurtable(Health body, CombatActor lead)
        {
            if (body == null || !body.Targetable || GameTime.Now < body.InvulnerableUntil) return false;
            if (body.Plate != null && body.Plate(new DamagePacket { amount = 1, type = DamageType.Kinetic, source = lead.Health }) <= 0) return false;
            if (body.Holds != null && body.Holds() >= body.integrity - .01f) return false;
            return true;
        }
        /// <summary>A part with no brain or boss of its own (a quenched heat plate) is chased for its blows, and its boss's tells are still the ones a person reads.</summary>
        static Health TellsOf(Health carrier)
        {
            if (carrier == null || carrier.GetComponent<BossController>() != null || carrier.GetComponent<EnemyBrain>() != null) return carrier;
            var boss = carrier.GetComponentInParent<BossController>();
            return boss != null && boss.TryGetComponent<Health>(out var body) ? body : carrier;
        }
    }
}
