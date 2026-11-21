using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.UI
{
    // The driver's reading of chapter 10 (docs/quality/routes chapter-62 on, tools/mooring_routes.py): a berth's pad or launch walked or flown to where it stands now (Mother of Doors'
    // fourth shelf swings on her strain, its pad, its launch and its spawns with it), and the markings of a boss nobody strikes read off the floor and rolled out of (Mother of Doors in
    // her room, the Tollkeeper in its: neither can be locked, so the older rule, which watches the locked boss's own tells, never looked at theirs). Ordinary pad presses and what a
    // player reads off the screen, as everywhere in the driver: the game still does to the party whatever it would do to a person who misjudged it.
    public sealed partial class ContinuousReview
    {
        /// <summary>The driver as it was before these hands, for a test's broken state (MooringReplayHandsTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool mooringHandsOff;

        // ---------- a berth's prompt, where it stands now ----------
        InteractionPrompt berthSaid;
        /// <summary>Where a walk goes. A step that ends docked at a berth (<c>"until":"docked:&lt;id&gt;"</c>, or its stopWhen) goes to that berth's pad, and one that ends launched from it
        /// to its launch while the party stands on it, wherever each stands now: the fourth shelf at Sorrel swings out round her middle and back every sixteen seconds from her third
        /// clamp on, its pad over it, and once the party has docked it stops wherever it was, the launch with it. A person flies at the pad they can see and walks to the launch they
        /// can see; a route can only write where they were built. Any other step goes to its point, as it always did.</summary>
        Vector3 BerthGoal(QualityStep s)
        {
            if (s == null) return default;
            string condition = Berthing(s.until) ? s.until : Berthing(s.stopWhen) ? s.stopWhen : null;
            if (mooringHandsOff || condition == null) return s.point;
            bool dock = condition.StartsWith("docked:");
            var berth = Berth.Find(condition.Substring(dock ? 7 : 9));
            if (berth == null || (!dock && Berth.Current != berth)) return s.point;
            var prompt = dock ? berth.pad : berth.launch;
            if (prompt == null) return s.point;
            var at = prompt.transform.position;
            if (berthSaid != prompt) { berthSaid = prompt; Debug.Log("QUALITY_BERTH_GOAL " + berth.id + (dock ? " pad at " : " launch at ") + at.ToString("F1") + " (built at " + s.point.ToString("F1") + ")"); }
            return new Vector3(at.x, s.point.y, at.z);
        }
        static bool Berthing(string condition) => !string.IsNullOrEmpty(condition) && (condition.StartsWith("docked:") || condition.StartsWith("launched:"));

        // ---------- a boss nobody strikes: its markings read off the floor ----------
        Telegraph unstruckSaid;
        /// <summary>The way out of a marking laid by a boss nobody strikes, as its tell runs out over the lead, in the stick's own terms; null while none is about to land on him. Mother of
        /// Doors in her room and the Tollkeeper in its take no blow and no lock (their bars are their clamps and their motor), so the rule that watches the locked boss's tells never
        /// looked at hers: her sweeps and waves landed on every ship flying from clamp to clamp. A person reads the wedge or the band over their ship and rolls out of it as it lands:
        /// across a wedge toward its nearer edge, out of a band or a circle the shorter way, aside of a lane. On foot the roll keeps to the ground the lead stands on. Only those two
        /// bosses in their own rooms: every other fight keeps the rule it was timed with.</summary>
        Vector2? OutOfAnUnstruckBoss(CombatActor lead)
        {
            if (mooringHandsOff || lead == null || !lead.Health.Alive || lead.Recovering || GameTime.Paused || GameInput.Current == null || GameInput.Current.Blocked) return null;
            var here = lead.transform.position; float soon = (lead.flight ? .3f : .16f) * Time.timeScale;
            foreach (var marking in Telegraph.Live)
            {
                if (marking == null || marking.Released || !(marking.owner is BossController boss) || boss == null || !(boss.MotherRoom || boss.TollRoom)) continue;
                if (!boss.TryGetComponent<Health>(out var body) || !body.Alive || body.Damageable) continue;
                // Whether it lands on him now, and the way out: her wave read as it runs (ContinuousReview.Mother.cs), the Tollkeeper's own (ContinuousReview.Tollkeeper.cs), else as it was.
                if (!UnstruckLands(marking, boss, lead, soon, out var way)) continue;
                way.y = 0; if (way.sqrMagnitude < .0001f) continue;
                if (!lead.flight) way = RollWay(lead, way.normalized);
                if (unstruckSaid != marking) { unstruckSaid = marking; Debug.Log("QUALITY_UNSTRUCK_ROLL " + body.id + " " + marking.Shape + " at " + here.ToString("F1")); }
                var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * way.normalized;
                return new Vector2(local.x, local.z);
            }
            return null;
        }
        // ---------- what the fight's own words say about the pad: hold fire, a collar, a guard, a tender on the arm ----------
        bool couplingPress; double couplingPressAt = -1, guardPressAt = -1, armFlickAt = -1;
        /// <summary>Meret's craft with its dome up (<c>HER SHIELD IS UP · HOLD YOUR FIRE</c>): a shot of the lead's that reaches it comes back down a marked lane, so a person takes the
        /// thumb off Fire (and the Pulse, and the EXTRA) until her lance or her tractor drops it. The fight steps press Fire constantly.</summary>
        static bool HoldFire(CombatActor lead)
        {
            if (mooringHandsOff || lead == null || !lead.flight) return false;
            var boss = BossController.Focus(lead.transform.position, 80);
            // A lit relay locked is what to shoot while her dome stands (ContinuousReview.Meret.cs).
            return boss != null && boss.ShipShieldUp && !AimsAtRelay(lead);
        }
        /// <summary>Meret's gantry with an arm overreached (<c>AN ARM IS OVERREACHED · TAKE ITS COLLAR</c>): the coupling the lead's hands are on, or the nearest offered.</summary>
        static MeretCoupling CouplingToWork(CombatActor lead)
        {
            if (mooringHandsOff || lead == null || lead.flight) return null;
            MeretCoupling best = null; float least = float.PositiveInfinity;
            foreach (var c in MeretCoupling.All)
            {
                if (c == null || !c.isActiveAndEnabled || c.IsOpen) continue;
                if (c.Holder == lead) return c;
                if (!c.Available) continue;
                var gap = c.transform.position - lead.transform.position; gap.y = 0;
                if (gap.sqrMagnitude < least) { least = gap.sqrMagnitude; best = c; }
            }
            return best;
        }
        /// <summary>To the lit seat by the floor's way, slowing over the last metre; there, the interact button as a thumb presses it until the hands are on the collar; then nothing.</summary>
        Vector2 WorkCoupling(CombatActor lead, MeretCoupling coupling)
        {
            couplingPress = false;
            if (coupling.Holder == lead) return Vector2.zero;
            var seat = coupling.transform.position; var gap = seat - lead.transform.position; gap.y = 0;
            if (gap.magnitude > MeretCoupling.SeatRadius * .5f)
            {
                var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * ReviveHeading(lead, seat, gap, false).normalized;
                return new Vector2(local.x, local.z) * Mathf.Clamp(gap.magnitude, .35f, 1);
            }
            double now = clock.Elapsed.TotalSeconds; if (now - couplingPressAt > .6) couplingPressAt = now;
            couplingPress = now - couplingPressAt < .12; return Vector2.zero;
        }
        /// <summary>The direction a step on foot goes in: to a gantry coupling to be worked, or as it was (<paramref name="was"/>), and whether the chase's own presses still apply.</summary>
        Vector2 MooringHeading(CombatActor actor, QualityStep s, Vector2 was, ref bool inReach)
        {
            var coupling = s.approachTarget ? CouplingToWork(actor) : null;
            if (coupling == null) { couplingPress = false; return was; }
            inReach = false; return WorkCoupling(actor, coupling);
        }
        static GamepadState Without(GamepadState pad, params GamepadButton[] buttons) { foreach (var b in buttons) pad.buttons &= ~(1u << (int)b); return pad; }
        /// <summary>The pad as the step and the driver's other hands left it, with what these fights' words ask of a thumb: no Fire, Pulse or EXTRA under Meret's dome; the interact
        /// button at a gantry coupling's seat and the pad let go while the hands are on it (a roll or a swing lets go); one blow into Meret's guard, not a string of them (her counter
        /// punishes mashing), and nothing pressed while she listens; on the Tollkeeper's arm the lock flicked across to a Clamp Tender holding the motor.</summary>
        GamepadState MooringHands(GamepadState pad, PartyController party, bool evading, bool raising)
        {
            var lead = party != null ? party.Active : null;
            if (mooringHandsOff || lead == null) return pad;
            // A dock step's A only at its own pad, never at another berth's offered on the way (ContinuousReview.Mother.cs).
            if (WrongPad()) pad = Without(pad, GamepadButton.South);
            // Without RB the EXTRA's RB+LT is only the brake (LT), which a ship under her dome wants anyway.
            // A berth step's A answers the pad that is offered, not the guns: after her hail the dome stands over the bay's way back in, and the hold swallowed the press that
            // docks (route 66 stopped there twice in the player, November 15: six seconds at "Dock: The Continuance" with nothing pressed).
            bool berthPress = step != null && (Berthing(step.until) || Berthing(step.stopWhen)) && PromptService.Interaction is BerthPrompt;
            if (HoldFire(lead) && !berthPress) pad = Without(pad, GamepadButton.South, GamepadButton.RightShoulder, GamepadButton.West);
            bool press = couplingPress && !evading && !raising; couplingPress = false;
            if (press) pad = pad.WithButton(GamepadButton.South);
            foreach (var c in MeretCoupling.All) if (c != null && c.Holder == lead) return default;
            double now = clock.Elapsed.TotalSeconds;
            var meret = lead.target != null ? lead.target.GetComponent<BossController>() : null;
            if (meret != null && meret.WellListening) pad = Without(pad, GamepadButton.South, GamepadButton.RightShoulder, GamepadButton.East, GamepadButton.West, GamepadButton.North);
            else if (meret != null && meret.WellGuarding)
            {
                // The first press into her guard goes through (a tenth of a second of it); every later one waits for her riposte to be rolled and her guard to drop.
                if ((pad.buttons & (1u << (int)GamepadButton.South)) != 0) { if (guardPressAt < 0) guardPressAt = now; if (now - guardPressAt > .12) pad = Without(pad, GamepadButton.South); }
            }
            else guardPressAt = -1;
            if (ArmTender(lead) && now - armFlickAt > .4) armFlickAt = now;
            if (ArmTender(lead) && now - armFlickAt < .12) pad = pad.WithButton(GamepadButton.DpadRight);
            return pad;
        }
        /// <summary>On a berth with a Clamp Tender up beside the lead and the lock on something else (the Tollkeeper's motor, which takes a quarter of a blow while one holds it):
        /// chapter 7's tender hand reads only rooms with Bridle clamps, so this flicks the lock across here.</summary>
        static bool ArmTender(CombatActor lead)
        {
            var zone = ZoneController.Current;
            if (mooringHandsOff || zone == null || !zone.Berthed || lead.flight || BridleClamp.All.Count > 0) return false;
            if (lead.target != null && lead.target.Alive && Tended.IsTender(lead.target)) return false;
            foreach (var body in Health.All)
                if (body != null && !body.friendly && body.Alive && Tended.IsTender(body) && (body.transform.position - lead.transform.position).sqrMagnitude < 12 * 12) return true;
            return false;
        }

        /// <summary>The short way out of a marking: across a wedge (round its origin, toward the nearer of its two edges: a wedge forty metres long is left sideways, not by its far end),
        /// and the marking's own way out for everything else.</summary>
        static Vector3 Aside(Telegraph marking, Vector3 at)
        {
            var offset = at - marking.Origin; offset.y = 0;
            if (marking.Shape == Telegraph.Kind.Cone && marking.Angle < 359 && offset.sqrMagnitude > .25f)
            {
                var round = Vector3.Cross(Vector3.up, offset.normalized);
                return Vector3.Dot(offset, Vector3.Cross(Vector3.up, marking.Forward)) >= 0 ? round : -round;
            }
            return marking.WayOut(at);
        }
    }
}
