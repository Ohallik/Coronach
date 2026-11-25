using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using UnityEngine;

namespace Lattice.UI
{
    // The driver's reading of chapter 7's two machines: the Orrery's rail-gates, which open in a moon's lee, and the Bridle's clamps in the Last Warden's
    // engine hall, which a hero opens with his own hands while the Warden's staff is down. Ordinary pad presses and what a player reads off the screen, as
    // everywhere in the driver: the game still does to the party whatever it would do to a person who misjudged it.
    public sealed partial class ContinuousReview
    {
        // ---------- the Orrery: "until":"lee:<rail>" ----------
        /// <summary>How far off a gate row the lead may be for its gate to be read: a ship waiting short of the row, or still braking toward it.</summary>
        public const float LeeWithin = 30;
        OrreryRail[] rails; string railsIn, leeSaid;
        /// <summary>Rail n of the zone's machine (1 the outermost, as docs/maps/Orrery.md numbers them) has its gate nearest the lead open in its moon's lee. A step that waits
        /// on it holds the ship braked short of the row and ends the moment it is true; the walk after it goes through. Nothing counts seconds: the routes wait on the machine.</summary>
        bool InTheLee(string which)
        {
            var lead = PartyController.Current != null ? PartyController.Current.Active : null;
            if (lead == null || !int.TryParse(which, out int n)) return false;
            var rail = RailNumbered(n);
            bool lee = rail != null && (leeAsGreen ? Green(rail, lead.transform.position) : InLee(rail, lead.transform.position, rail.Moon));
            // Said once each time it comes: the step it ends is the record of the wait, this is the record of what the machine showed.
            if (lee && leeSaid != which) Debug.Log("QUALITY_LEE " + which + " " + rail.railName + " gate " + rail.NearestGate(rail.Along(lead.transform.position, rail.GateRadius)));
            if (lee) leeSaid = which; else if (leeSaid == which) leeSaid = null;
            return lee;
        }
        /// <summary>The driver reading the lights as green alone, for a test's broken state (OrielReplayHandsTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool leeAsGreen;
        /// <summary>A wait on the machine ("until":"lee:n") that the lee ended: its point is only where to hold the ship if the lee hasn't come, and a lee that comes while the ship
        /// is still braking toward it is the step done, not a checkpoint missed. On every rail after the first, the green wave brought the lee eight metres short of the
        /// point, and each of those eight waits was counted a miss (route 38's first two runs in a player, November 14). And the driver as it was, for a test's broken state.</summary>
        static bool WaitedOut(QualityStep step) => !leeWaitsMissed && !string.IsNullOrEmpty(step.until) && step.until.StartsWith("lee:");
        static bool leeWaitsMissed;
        static bool Green(OrreryRail rail, Vector3 at)
        {
            if (Mathf.Abs(rail.FromCentre(at) - rail.GateRadius) > LeeWithin) return false;
            int gate = rail.NearestGate(rail.Along(at, rail.GateRadius));
            return gate >= 0 && !rail.GateShut(gate, rail.Moon);
        }
        /// <summary>The zone's rails, outermost first: the order a ship meets them flying in from the arrival.</summary>
        OrreryRail RailNumbered(int n)
        {
            string scene = ActualScene;
            if (rails == null || railsIn != scene || rails.Length > 0 && rails[0] == null)
            {
                railsIn = scene; rails = FindObjectsByType<OrreryRail>(FindObjectsSortMode.None);
                System.Array.Sort(rails, (a, b) => b.radius.CompareTo(a.radius));
            }
            return n >= 1 && n <= rails.Length ? rails[n - 1] : null;
        }
        /// <summary>The zone's rule as Ishen teaches it ("wait in a moon's lee, and go on green"), read off what the screen shows: within <see cref="LeeWithin"/> of the gate row and
        /// in the lane, the ring nearest the lead is green and the moon has gone past it, no further on than a quarter of its loop. Green alone is not enough: a ring goes green a
        /// few seconds before the moon comes round to it again (the lamps on the rail stand amber ahead of the moon to say so). In the lee every rail's gate stays open eleven
        /// seconds or more, and the moon is going away from where the ship crosses its rail.</summary>
        public static bool InLee(OrreryRail rail, Vector3 at, float moonAt)
        {
            float row = rail.GateRadius;
            if (Mathf.Abs(rail.FromCentre(at) - row) > LeeWithin) return false;
            float along = rail.Along(at, row);
            if (Mathf.Abs(along) > rail.laneHalf) return false;
            int gate = rail.NearestGate(along);
            if (gate < 0 || rail.GateShut(gate, moonAt)) return false;
            float ahead = rail.AheadOfMoon(rail.GateAlongRail(gate), moonAt);
            return ahead < 0 && ahead > -rail.Loop * .25f;
        }

        // ---------- the Bridle's clamps, in the Last Warden's fight ----------
        /// <summary>Where the lead stands in a clamp's seat: this far to one side of the clamp's own fibre, which runs out from the engine under the seat. The seat answers to 1.6 m
        /// from its middle, and a strike down the fibre is a lane 1.15 m either side of it (its 1.8 m and a body's allowance): from here the hands are on the clamp and a Pluck,
        /// a Chord or a Toll down that fibre goes by. And how near the spot the lead is before it presses.</summary>
        public const float ClampAside = 1.38f, ClampStand = .2f;
        bool clampPress; double clampPressAt = -1, tenderPressAt = -1;
        /// <summary>The driver as it was before this hand, for a test's broken state (OrielReplayHandsTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool clampHandsOff;
        /// <summary>A Clamp Tender up in the hall (known by what it is, <see cref="Tended.IsTender"/>: chapter 10's keeps Mother of Doors' clamps): while one is, every clamp offered is one it keeps (a clamp with a tender at it never eases), and a person stops it first.</summary>
        static Health TenderNear(Vector3 at)
        {
            foreach (var body in Health.All)
                if (body != null && !body.friendly && body.Alive && Tended.IsTender(body) && (body.transform.position - at).sqrMagnitude < 30 * 30) return body;
            return null;
        }
        /// <summary>At a held line the Warden's clamps are offered, each seat lit as a pale ring (THE ROAD IS HELD · OPEN A CLAMP): the one the lead already has his hands on, or the
        /// nearest offered. None while a tender is up.</summary>
        static BridleClamp ClampToWork(CombatActor lead)
        {
            if (clampHandsOff || BridleClamp.All.Count == 0 || TenderNear(lead.transform.position) != null) return null;
            BridleClamp best = null; float least = float.PositiveInfinity;
            foreach (var clamp in BridleClamp.All)
            {
                if (clamp == null || !clamp.isActiveAndEnabled) continue;
                if (clamp.Holder == lead) return clamp;
                if (!clamp.Available) continue;
                var gap = ClampSpot(clamp, lead.transform.position) - lead.transform.position; gap.y = 0;
                if (gap.sqrMagnitude < least) { least = gap.sqrMagnitude; best = clamp; }
            }
            return best;
        }
        /// <summary>The spot in a clamp's seat beside its fibre, on the side nearer the lead. A seat faces the hall's middle, so its own right is across its fibre.</summary>
        public static Vector3 ClampSpot(BridleClamp clamp, Vector3 from)
        {
            // Mother of Doors' clamps have no fibre, and their works stand at the seat (ContinuousReview.Mother.cs).
            if (MotherStand(clamp) is Vector3 hers) return hers;
            var seat = clamp.transform.position; var aside = clamp.transform.right; aside.y = 0; aside.Normalize();
            var off = from - seat; off.y = 0;
            if (Vector3.Dot(off, aside) < 0) aside = -aside;
            return seat + aside * ClampAside;
        }
        /// <summary>To the seat by the floor's own way, slowing over the last metre; then still on the spot with the interact button pressed as a thumb does until the hands are on
        /// the clamp; then still with nothing pressed until it opens (<see cref="AtAClamp"/>): a roll, a swing, a skill or a step out of the seat would let it go.</summary>
        Vector2 WorkClamp(CombatActor lead, BridleClamp clamp)
        {
            clampPress = false;
            if (clamp.Holder == lead) return Vector2.zero;
            var spot = ClampSpot(clamp, lead.transform.position); var gap = spot - lead.transform.position; gap.y = 0;
            // Mother of Doors' clamps: walked to straight across the deck, and taken as soon as the game offers them (ContinuousReview.Mother.cs).
            if (gap.magnitude > ClampStand && !MotherOffered(clamp))
            {
                var heading = MotherWalksStraight(clamp) ? gap : ReviveHeading(lead, spot, gap, false);
                var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * heading.normalized;
                return new Vector2(local.x, local.z) * Mathf.Clamp(gap.magnitude, .3f, 1);
            }
            double now = clock.Elapsed.TotalSeconds;
            if (now - clampPressAt > .6) clampPressAt = now;
            clampPress = now - clampPressAt < .12;
            return Vector2.zero;
        }
        /// <summary>The lead's hands are on a clamp and no tender is up to close it: nothing is pressed and the stick is let go, whatever the step says (a skill's RB, a swing, a
        /// roll from a tell, an item would each let it go). The clamp pulls, and eases while the Warden's staff is down: a Draw closing on nobody, a Toll's ringing, a break.</summary>
        static bool AtAClamp(PartyController party) => !clampHandsOff && party != null && party.Active != null && BridleClamp.Held != null && BridleClamp.Held.Holder == party.Active &&
            TenderNear(party.Active.transform.position) == null;
        /// <summary>How near a blade is taken to a Clamp Tender before it swings: half a person tall, a step aside of its seat. From the chase's 2.3 m the lead stood still and swung
        /// in front of it (2.13 m off the Stillwater Tender at Mother of Doors' first seat: twenty seconds of swings, none landed; from 1.9 m about one in three), and in the player's run he
        /// did so for five rounds (nov24-recon-g-63 s63, D406). A person sees the blade pass short and steps in. Her shots (Sela) keep the step's ranged reach.</summary>
        public const float TenderBlade = 1.5f;
        /// <summary>The chase as it was, swinging at a tender from the step's reach, for a test's broken state (MooringBossReplayTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool tenderReachAsItWas;
        static float TenderReach(CombatActor actor, Health chased, float reach) =>
            tenderReachAsItWas || chased == null || actor == null || actor.flight || ShootsOnFoot(actor) || !Tended.IsTender(chased) ? reach : Mathf.Min(reach, TenderBlade);
        /// <summary>A Clamp Tender is up and the lock is on something else (the Warden, whose held line takes nothing): a person flicks the lock across with the d-pad, once in a
        /// while until it is on a tender, and the chase goes after it. Tenders are fragile.</summary>
        bool TenderToStop(CombatActor lead)
        {
            if (clampHandsOff || BridleClamp.All.Count == 0 || TenderNear(lead.transform.position) == null) return false;
            if (lead.target != null && lead.target.Alive && Tended.IsTender(lead.target)) return false;
            double now = clock.Elapsed.TotalSeconds;
            if (now - tenderPressAt > .4) tenderPressAt = now;
            return now - tenderPressAt < .12;
        }
    }
}
