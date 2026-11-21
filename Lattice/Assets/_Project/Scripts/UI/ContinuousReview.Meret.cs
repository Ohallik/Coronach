using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.UI
{
    // The driver's reading of Meret Venn's three fights (docs/quality routes chapter-66 and chapter-67, tools/mooring_routes.py; T/PlayMode/MeretReplayTests.cs plays each through this pad):
    // what her markings say that her moves' own tells don't. Ordinary pad presses and what a player reads off the screen, as everywhere in the driver: the game still does to the party
    // whatever it would do to a person who misjudged it.
    public sealed partial class ContinuousReview
    {
        /// <summary>The driver as it was before these hands, for a test's broken state (MeretReplayTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool meretHandsOff;

        // ---------- her craft: fought from the open lane in front of her shelter ----------
        /// <summary>Where a person fights her craft from: this far out in front of her along her shelter's mouth, at most this far to either side of it, within this of that stand before he brakes,
        /// and how far off his Fire is held (her shield answers only what reaches it while it stands: HoldFire takes the thumb off while it does).</summary>
        public const float ShipStandOff = 14, ShipStandSide = 6, ShipStandNear = 2.5f, ShipFire = 30;
        /// <summary>Her craft holds station six metres out of its shelter's mouth, between two armoured wings, and sways five metres toward the lead. The fight steps chase her braked to
        /// twelve metres, straight: played through the pad (MeretReplayTests) the lead was rolled out round her broadsides, chased her straight at her across the west wing and scraped along its far
        /// side for most of fourteen rounds with the wing between them, and left her at 62% after 367 s. A person keeps to the open lane in front of her mouth, where every lane of hers is
        /// marked and nothing stands between him and her: a stand fourteen metres out, on his side of her middle, with a clear line to her (a clamp risen in it moves him along); flown to
        /// brake off, held braked with the nose on her, and fired from whenever she is in reach and in sight. Out behind a wing, he first flies out along her mouth's way. Null for anything
        /// but her craft; whether the brake comes off, and whether Fire may be held.</summary>
        static Vector2? MeretShipHeading(CombatActor lead, Health chased, out bool unbraked, out bool firing)
        {
            unbraked = firing = false;
            if (meretHandsOff || lead == null || !lead.flight || chased == null || !chased.Alive) return null;
            // A relay of hers locked (her dome up): the same lane, on the relay's side of her middle, so the shot at it passes nowhere near her dome.
            var relay = chased.GetComponent<MeretRelay>();
            var boss = relay != null ? relay.boss : chased.GetComponentInParent<BossController>(); if (boss == null || boss.Health == null || boss.Health.id != "MeretShip") return null;
            var at = lead.transform.position; var her = boss.transform.position; var facing = boss.ShipFacing; facing.y = 0;
            if (facing.sqrMagnitude < .01f) return null; facing.Normalize();
            var across = Vector3.Cross(Vector3.up, facing);
            float side = relay != null ? Mathf.Sign(Vector3.Dot(relay.transform.position - her, across)) * ShipStandSide : Mathf.Clamp(Vector3.Dot(at - her, across), -ShipStandSide, ShipStandSide);
            Vector3 Stand(float s) { var p = her + facing * ShipStandOff + across * s; p.y = at.y; return p; }
            var stand = Stand(side);
            // A clamp risen across the line to her: along the lane, a few metres at a time, to where she can be seen.
            for (int k = 1; k <= 4 && !CombatCover.Clear(stand, her, .4f); k++)
            {
                var right = Stand(Mathf.Clamp(side + 4 * k, -ShipStandSide - 6, ShipStandSide + 6)); var left = Stand(Mathf.Clamp(side - 4 * k, -ShipStandSide - 6, ShipStandSide + 6));
                bool r = CombatCover.Clear(right, her, .4f), l = CombatCover.Clear(left, her, .4f);
                if (r || l) { stand = r && (!l || (right - at).sqrMagnitude < (left - at).sqrMagnitude) ? right : left; break; }
            }
            var toHer = her - at; toHer.y = 0;
            var aim = relay != null ? relay.transform.position : her; var toAim = aim - at; toAim.y = 0;
            bool seen = CombatCover.Clear(at, aim, .3f) && (relay == null || ClearOfHerDome(at, aim, her));
            firing = toAim.magnitude <= ShipFire && seen;
            var gap = stand - at; gap.y = 0;
            // At a stand whose own line is clear but with his own cut (a wing's corner across it: played through the pad the lead sat so for forty seconds, Fire held off, her bar under
            // half a percent): in toward the middle of her lane.
            if (!seen && gap.magnitude <= ShipStandNear) { stand = Stand(0); gap = stand - at; gap.y = 0; }
            float yaw = ZoneController.Current.CameraProfile.yaw;
            Vector2 Stick(Vector3 heading, float speed) { var local = Quaternion.Euler(0, -yaw, 0) * heading.normalized; return new Vector2(local.x, local.z) * speed; }
            if (gap.magnitude > ShipStandNear)
            {
                unbraked = gap.magnitude > 6;
                float hull = lead.TryGetComponent<CharacterController>(out var shape) ? shape.radius : 2;
                // Behind something solid (a wing, a clamp): out along her mouth's way first, toward the stand's side.
                if (CombatCover.Sweep(at, stand, hull * .9f, out _)) { unbraked = true; return Stick(facing + across * Mathf.Sign(Vector3.Dot(gap, across)) * .35f, 1); }
                return Stick(gap, Mathf.Clamp(gap.magnitude / 6, .35f, 1));
            }
            // At the stand: braked, the nose kept on what is aimed at with a touch of the stick (the flight chase's own .25, clear of the dead zone).
            return Vector3.Angle(lead.motor.Facing, toAim) > 5 ? Stick(toAim, .25f) : Vector2.zero;
        }
        /// <summary>A shot from here to there passes outside her dome (and a hull's width more): one that crosses it is spent on it, and given back.</summary>
        static bool ClearOfHerDome(Vector3 from, Vector3 to, Vector3 her)
        {
            from.y = to.y = her.y = 0; var d = to - from; float t = d.sqrMagnitude > .01f ? Mathf.Clamp01(Vector3.Dot(her - from, d) / d.sqrMagnitude) : 0;
            return (from + d * t - her).magnitude > BossController.ShipTune.Shield + 1.2f;
        }
        /// <summary>The lead's lock is on a lit relay of hers: Fire at it is never given back, so the dome hand leaves the thumb on it.</summary>
        static bool AimsAtRelay(CombatActor lead) => !meretHandsOff && lead != null && lead.target != null && lead.target.TryGetComponent<MeretRelay>(out var relay) && relay.Lit;
        double relayFlickAt = -1;
        /// <summary>Her dome up and a relay of hers lit (<c>HER SHIELD IS UP · SHOOT A RELAY</c>): the lock flicked round with the d-pad, as a thumb does, until it is on the lit relay nearest
        /// the lead. With her dome down the game's own lock finds her again (a dark relay can't be locked).</summary>
        GamepadState MeretHands(GamepadState pad, PartyController party, bool evading)
        {
            var lead = party != null ? party.Active : null;
            if (meretHandsOff || lead == null || !lead.flight || evading) return pad;
            var boss = BossController.Focus(lead.transform.position, 80); if (boss == null || boss.Health == null || boss.Health.id != "MeretShip" || !boss.ShipShieldUp) return pad;
            MeretRelay want = null; float least = float.PositiveInfinity;
            foreach (var r in boss.ShipRelays) { if (r == null || !r.Lit) continue; float d = (r.transform.position - lead.transform.position).sqrMagnitude; if (d < least) { least = d; want = r; } }
            if (want == null || lead.target == want.health) return pad;
            double now = clock.Elapsed.TotalSeconds; if (now - relayFlickAt > .45) relayFlickAt = now;
            if (now - relayFlickAt < .12) pad = pad.WithButton(GamepadButton.DpadRight);
            return pad;
        }

        static bool IsMeret(BossController boss) => boss != null && boss.Health != null && boss.Health.id != null && boss.Health.id.StartsWith("Meret", System.StringComparison.Ordinal);
        Telegraph meretSaid;
        /// <summary>The way out of a marking of Meret's over the lead as it is about to land, in the stick's own terms; null while none is. The rule that reads the locked boss's tells reads
        /// only what her moves count down (a broadside, a lance, a slam), and two of her markings come from no move: the lane her shield gives a shot back down, and the clamp that closes under
        /// a hero who has stood still. Both are drawn on the floor under the lead for as long as the rest. A person reads the marking under him and leaves it the short way as it lands: aside
        /// of a lane, out of a circle. On foot the roll keeps to the ground the lead stands on.</summary>
        Vector2? OutOfMeretsMarking(CombatActor lead)
        {
            if (meretHandsOff || lead == null || !lead.Health.Alive || lead.Recovering || GameTime.Paused || GameInput.Current == null || GameInput.Current.Blocked) return null;
            var here = lead.transform.position; float soon = (lead.flight ? .3f : .16f) * Time.timeScale;
            foreach (var marking in Telegraph.Live)
            {
                if (marking == null || marking.Released || marking.Steady || !(marking.owner is BossController boss) || !IsMeret(boss)) continue;
                if (marking.Remaining > soon || !marking.Contains(here)) continue;
                // Her guard's pale ring round her strikes nobody: it is the counter's boundary, and the guard hand answers it.
                if (boss.WellGuarding && marking.Shape == Telegraph.Kind.Circle && (marking.Origin - boss.transform.position).sqrMagnitude < 1) continue;
                var way = Aside(marking, here); way.y = 0; if (way.sqrMagnitude < .0001f) continue;
                if (!lead.flight) way = RollWay(lead, way.normalized);
                if (meretSaid != marking) { meretSaid = marking; Debug.Log("QUALITY_MERET_ROLL " + boss.Health.id + " " + marking.Shape + " at " + here.ToString("F1")); }
                var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * way.normalized;
                return new Vector2(local.x, local.z);
            }
            return null;
        }
    }
}
