using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.UI
{
    // The driver's hands for cover in flight (route 17 at barge three, tools/xylos_routes.py; BargeSkimmerReplayTests): a lock held on something behind a hull, and a walk ground against
    // one. Ordinary pad presses and what a player reads off the screen, as everywhere in the driver: the d-pad's lock flick and the stick.
    public sealed partial class ContinuousReview
    {
        /// <summary>The driver as it was before these hands, for a test's broken state (BargeSkimmerReplayTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool coverHandsOff;

        /// <summary>How long the line to what is locked stays shut, with the ship in reach and firing, before a person does something about it: a few shots sparking on the hull.</summary>
        const float ShutFor = 1f;
        /// <summary>How far aside the ship looks for a line, a step at a time, each way.</summary>
        const float AsideStep = 2f, AsideMost = 16f;
        double coverShutSince = -1, coverFlickAt = -1, coverWayAt = -1; Health coverFor; Vector3? coverWay; GamepadButton? coverFlick;

        /// <summary>Where the ship's guns are, and where a shot at this body goes (FlightEmitter: from the hull to the muzzle, then at the body's middle).</summary>
        static Vector3 Guns(CombatActor lead) => lead.transform.position + Vector3.up * .9f;
        static Vector3 AimAt(Health body)
        {
            var capsule = body.GetComponent<CapsuleCollider>();
            return capsule != null ? capsule.bounds.center : body.transform.position + Vector3.up * .75f;
        }
        /// <summary>Something solid (a barge's hull, a pane) stands between the ship's guns and this body: what is fired at it sparks off that.</summary>
        static bool LineShut(Vector3 from, Health body) => !CombatCover.Clear(from, AimAt(body), .09f);

        /// <summary>In flight, a fight step, the lock on something in reach and every shot at it sparking off a hull: at barge three the last skimmer sat behind the bow, 11.8 m off and inside
        /// the step's reach of 12, and the lead held the brake and fired four hundred shots into the hull for ninety seconds (BargeSkimmerReplayTests; nov15-recon38 and nov20-recon40 s17).
        /// A person whose shots do nothing flicks the lock to a body in the open, if one is awake within the lock's reach, or else flies aside, the brake off, to the nearest place the
        /// line is clear and fires from there; on the way there, out of the step's reach or not, it keeps going until the line is open. Null while the line is open, the lock is elsewhere or the
        /// hands are off; the stick otherwise. A flick is pressed by <see cref="CoverFlick"/>.</summary>
        Vector2? CoverHeading(CombatActor lead, Health chased, bool inReach)
        {
            coverFlick = null;
            // A boss's fight keeps the hands it was sized with (D319 to D327).
            if (coverHandsOff || lead == null || !lead.flight || chased == null || !chased.Alive || chased.GetComponentInParent<BossController>() != null ||
                BossController.Focus(lead.transform.position, LostBossWithin) != null) { coverShutSince = -1; coverWay = null; return null; }
            double now = clock.Elapsed.TotalSeconds; var guns = Guns(lead);
            if (chased != coverFor) { coverFor = chased; coverShutSince = -1; coverWay = null; }
            if (!LineShut(guns, chased)) { coverShutSince = -1; coverWay = null; return null; }
            // Out of reach and not on the way to a line, the chase is the step's own.
            if (!inReach && !coverWay.HasValue) { coverShutSince = -1; return null; }
            if (coverShutSince < 0) { coverShutSince = now; return null; }
            if (now - coverShutSince < ShutFor) return null;
            // A body of a fight in the open, within the lock's reach: the lock flicked to it, the nearer way round, a tap every .4 s.
            var open = OpenBody(lead, chased);
            if (open != null)
            {
                coverFlick = FlickToward(lead, open);
                if (coverFlick.HasValue && now - coverFlickAt > 2) { coverFlickAt = now; Debug.Log("QUALITY_COVER_FLICK " + chased.name + " behind cover, to " + open.name); }
                return null;
            }
            // Nothing in the open: aside to the nearest place a shot reaches it, looked for again every half second as it moves.
            if (!coverWay.HasValue || now - coverWayAt > .5)
            {
                coverWayAt = now; var found = WayToALine(lead, chased);
                if (found.HasValue && !coverWay.HasValue) Debug.Log("QUALITY_COVER_ASIDE " + chased.name + " behind cover, to (" + found.Value.x.ToString("0.0") + ", " + found.Value.z.ToString("0.0") + ")");
                coverWay = found;
            }
            if (!coverWay.HasValue) return null;
            var heading = coverWay.Value - lead.transform.position; heading.y = 0;
            if (heading.sqrMagnitude < .25f) { coverWay = null; return null; }
            var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * heading.normalized;
            return new Vector2(local.x, local.z);
        }
        /// <summary>The d-pad press the cover hands want this frame, if any.</summary>
        GamepadButton? CoverFlick => coverHandsOff ? null : coverFlick;

        /// <summary>The nearest awake body the lock could take with its line from the ship's guns open.</summary>
        static Health OpenBody(CombatActor lead, Health behind)
        {
            Health best = null; float least = float.PositiveInfinity; var guns = Guns(lead);
            foreach (var body in Health.All)
            {
                if (body == null || body == behind || !Lockable(lead, body)) continue;
                var owner = body.reactionOwner != null ? body.reactionOwner : body;
                if (!owner.TryGetComponent<EnemyBrain>(out var brain) || !brain.enabled || brain.Passive) continue;
                var gap = body.transform.position - lead.transform.position; gap.y = 0;
                if (gap.sqrMagnitude >= least || LineShut(guns, body)) continue;
                least = gap.sqrMagnitude; best = body;
            }
            return best;
        }
        /// <summary>The d-pad press that brings the lock round to this, in the game's own order (PlayerBrain.CycleTarget: by angle from where the ship faces), the shorter way; one tap
        /// every .4 s until the lock is on it.</summary>
        GamepadButton? FlickToward(CombatActor lead, Health want)
        {
            var targets = new List<Health>();
            foreach (var h in Health.All) if (h != null && h.Targetable && !h.friendly && !EnemyBrain.OtherForm(lead, h) && (h.transform.position - lead.transform.position).sqrMagnitude < LockRange * LockRange) targets.Add(h);
            var forward = lead.transform.forward; var from = lead.transform.position;
            targets.Sort((a, b) => Vector3.SignedAngle(forward, a.transform.position - from, Vector3.up).CompareTo(Vector3.SignedAngle(forward, b.transform.position - from, Vector3.up)));
            int n = targets.Count, goal = targets.IndexOf(want), here = targets.IndexOf(lead.target);
            if (goal < 0 || n == 0 || goal == here) return null;
            bool rightward = here < 0 || ((goal - here) % n + n) % n <= ((here - goal) % n + n) % n;
            double now = clock.Elapsed.TotalSeconds;
            if (now - coverTapAt > .4) coverTapAt = now;
            return now - coverTapAt < .12 ? rightward ? GamepadButton.DpadRight : GamepadButton.DpadLeft : null;
        }
        double coverTapAt = -1;
        /// <summary>Where a shot reaches the body from, with the flight there open for the hull: the nearest point aside of the line to it, either way, within <see cref="AsideMost"/>; failing
        /// that, the nearest by the flight there of the points round the body at the reach the ship stands at now (round the end of what is in the way). Null when there is none.</summary>
        static Vector3? WayToALine(CombatActor lead, Health body)
        {
            var at = lead.transform.position; var to = body.transform.position - at; to.y = 0; if (to.sqrMagnitude < .01f) return null;
            var across = Vector3.Cross(Vector3.up, to.normalized); float room = Room(lead);
            bool Fires(Vector3 spot) => OpenFlight(at, spot, room) && !LineShut(spot + Vector3.up * .9f, body);
            for (float aside = AsideStep; aside <= AsideMost + .01f; aside += AsideStep)
                foreach (float way in new[] { 1f, -1f })
                {
                    var spot = at + across * (aside * way);
                    if (Fires(spot)) return spot;
                }
            Vector3? best = null; float least = float.PositiveInfinity; var middle = body.transform.position; middle.y = at.y;
            foreach (float reach in new[] { to.magnitude, to.magnitude * .7f })
                for (int k = 0; k < RoundPoints; k++)
                {
                    var spot = middle + Quaternion.Euler(0, k * 360f / RoundPoints, 0) * Vector3.forward * reach;
                    float flight = Vector3.Distance(at, spot);
                    if (flight >= least || !Fires(spot)) continue;
                    least = flight; best = spot;
                }
            return best;
        }
        /// <summary>How many points round the body are looked at.</summary>
        const int RoundPoints = 24;

        // ---------- a walk in flight ground against a hull ----------
        /// <summary>The walk as it was before this hand, for a test's broken state (BargeSkimmerReplayTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool roundHandsOff;
        /// <summary>How long a walk in flight may go without gaining a metre on its point, with something solid on the straight line, before it goes round; and the rings it looks round on.</summary>
        const float RoundAfter = 1f; static readonly float[] RoundRings = { 5, 8, 11, 15 };
        QualityStep roundFor; Vector3? roundVia; Vector3 roundGainFrom; double roundGainAt = -1, roundViaAt = -1, roundLookedAt = -1;
        /// <summary>A walk in flight runs straight at its point, and a chase straight at what it chases. After a fight the party is wherever the fight left it, and a route's walk from there to
        /// its next point can lie through a barge: route 17's walk to barge three's flank knot from the hull's far side (where the chase had left Sela, Taren down at barge two) held her
        /// against the hull, a scrape every 0.6 s at 12 apiece, until she fell (nov15-recon38 s17; BargeSkimmerReplayTests); and a chase after the bow's knot from the stern slid Taren
        /// down the hull's side, seven scrapes in four seconds. A pilot flies round. With something solid on the straight line, a scrape felt or a second gone and not a metre
        /// gained, it flies by the nearest point (on rings round the ship) from which both legs are open for the hull, then on. The stick to push: the straight heading unchanged while
        /// the way is open, in a boss's fight (its hands were sized as they are) or with the hand off.</summary>
        Vector3 WalkRound(CombatActor lead, Vector3 goal, Vector3 straight, float reach)
        {
            // A chase ends where what it chases is in reach, not on it: the bow's knot hangs 1.4 m off the bow, so no way to its middle keeps a hull's width off the barge.
            bool chasing = reach > 0;
            if (roundHandsOff || lead == null || !lead.flight || BossController.Focus(lead.transform.position, LostBossWithin) != null) { roundVia = null; return straight; }
            double now = clock.Elapsed.TotalSeconds; var at = lead.transform.position; goal.y = at.y;
            if (step != roundFor) { roundFor = step; roundVia = null; roundGainFrom = at; roundGainAt = now; }
            if (roundVia is Vector3 via)
            {
                var leg = via - at; leg.y = 0;
                // What is chased moves: the way round it is looked at again sooner.
                if (leg.magnitude > 1.5f && now - roundViaAt < (chasing ? 3 : 6)) return leg;
                roundVia = null; roundGainFrom = at; roundGainAt = now;
            }
            float before = Flat(roundGainFrom, goal), still = Flat(at, goal), room = Room(lead);
            bool gaining = before - still > 1;
            if (gaining) { roundGainFrom = at; roundGainAt = now; }
            // A walk goes round only once it has stalled: a leg flown close by cover on purpose (the Picket's quiet route, rock to rock and pocket to pocket) is the route's, and going
            // round early took the ship into a buoy's sight (HarrowgateReplayTests). A chase also at a scrape felt this moment: it slides down a hull's side gaining all the while.
            bool touching = chasing && lead.motor is FlightMotor ship && ship.LastScrape > 0 && Time.unscaledTime - ship.LastScrape < .3f;
            if (!touching && (gaining || now - roundGainAt < RoundAfter)) return straight;
            // The hull's whole width, and a little: a line that clears its middle by less is a scrape down the side of what it passes. A first leg off a ship already against something
            // keeps a little less, or nothing would ever be open from there.
            float whole = Room(lead) / .85f + .2f;
            if (now - roundLookedAt < .5 || OpenFlight(at, Short(at, goal, reach), whole)) return straight;
            roundLookedAt = now; roundGainFrom = at; roundGainAt = now;
            Vector3? best = null;
            foreach (float off in new[] { whole, room })
            {
                float least = float.PositiveInfinity;
                foreach (float ring in RoundRings)
                    for (int k = 0; k < RoundPoints; k++)
                    {
                        var spot = at + Quaternion.Euler(0, k * 360f / RoundPoints, 0) * Vector3.forward * ring;
                        float way = ring + Flat(spot, goal);
                        if (way >= least || !OpenFlight(at, spot, off) || !OpenFlight(spot, Short(spot, goal, reach), whole)) continue;
                        least = way; best = spot;
                    }
                if (best.HasValue) break;
            }
            if (!best.HasValue) return straight;
            roundVia = best; roundViaAt = now;
            Debug.Log("QUALITY_FLIES_ROUND " + step.name + " by (" + best.Value.x.ToString("0.0") + ", " + best.Value.z.ToString("0.0") + ")");
            var first = best.Value - at; first.y = 0; return first;
        }
        static float Flat(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        /// <summary>Where a way from here toward a point ends: on it, or for a chase a metre inside the step's reach of it.</summary>
        static Vector3 Short(Vector3 from, Vector3 to, float reach)
        {
            if (reach <= 0) return to;
            var way = to - from; way.y = 0; float length = way.magnitude, stop = Mathf.Max(0, length - Mathf.Max(0, reach - 1));
            return length < .01f ? from : from + way / length * stop;
        }
        /// <summary>Nothing solid between two points on the flight plane, the ship's width either side: three thin sweeps at the guns' height, down the middle and at each side, so what lies
        /// under the plane (the debris a lane is strung over) is never in the way.</summary>
        static bool OpenFlight(Vector3 from, Vector3 to, float room)
        {
            var way = to - from; way.y = 0; var up = Vector3.up * .9f;
            if (way.sqrMagnitude < .0001f) return true;
            var side = Vector3.Cross(Vector3.up, way.normalized) * Mathf.Max(0, room - Thin);
            return CombatCover.Clear(from + up, to + up, Thin) && CombatCover.Clear(from + up + side, to + up + side, Thin) && CombatCover.Clear(from + up - side, to + up - side, Thin);
        }
        const float Thin = .5f;
        /// <summary>The width a way has to keep for this ship: its hull, a little inside so a ship already against something can still be asked about a way off it.</summary>
        static float Room(CombatActor lead) => HeroCollision.HullRadius(lead.character) * .85f;
    }
}
