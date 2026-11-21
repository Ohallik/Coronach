using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using UnityEngine;

namespace Lattice.UI
{
    // The driver's reading of chapter 9 (docs/quality routes chapter-53 to 61, tools/home_routes.py): a prompt that has to come before it is pressed (someone walking to
    // their place), a tread-hall coupling whose jack is held with the hands still, a wreck track's wrecks on the way a ship is about to fly, and the Foundry Saint's collars
    // and the climb to its back. Ordinary pad presses and what a player reads off the screen, as everywhere in the driver: the game still does to the party whatever it
    // would do to a person who misjudged it.
    public sealed partial class ContinuousReview
    {
        /// <summary>The driver as it was before these hands, for a test's broken state (HomeReplayHandsTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool homeHandsOff;

        // ---------- a prompt that has to come: "until":"prompt:<words>" ----------
        string promptSaid;
        /// <summary>The screen offers the lead a prompt whose words begin with these: the one the interact button would answer to now (PromptService's own choice, the nearest
        /// in reach). A step that waits on it holds the pad still and ends the moment it is up; the press after it is made at it. Orrin's door at his board is offered only once he
        /// has walked there from his call, and the close only once he stands at the walkway joint: a route that pressed on arriving pressed at nothing.</summary>
        bool Offered(string words)
        {
            var offered = PromptService.Interaction;
            bool up = offered != null && offered.isActiveAndEnabled && offered.Available && !string.IsNullOrEmpty(offered.prompt) && offered.prompt.StartsWith(words, StringComparison.Ordinal);
            if (up && promptSaid != words) Debug.Log("QUALITY_PROMPT " + words + " at " + offered.name);
            promptSaid = up ? words : null;
            return up;
        }

        // ---------- a coupling's jack: the hands kept still on it ----------
        /// <summary>The lead's hands are on a tread-hall coupling's jack (TreadCoupling.Holder): nothing is pressed and the stick is let go, whatever the step says, until it is mated
        /// or let go. A swing, a skill, a roll or a step out of the seat lets it go, and the step's own presses (a press_for pressing on, a dodge, the pouch) would be each of
        /// those: one press takes the jack, and the next would have been a swing. A coupling let go by a stagger is pressed for again by the step.</summary>
        static bool AtACoupling(PartyController party)
        {
            if (homeHandsOff || party == null || party.Active == null) return false;
            foreach (var coupling in TreadCoupling.All) if (coupling != null && coupling.Holder == party.Active) return true;
            return false;
        }

        // ---------- a wreck track: part of "until":"clear:x,z;x,z" in flight ----------
        static string tracksZone; static WreckTrack[] trackList;
        static WreckTrack[] Tracks()
        {
            string zone = SceneFlow.Current != null ? SceneFlow.Current.Zone : "";
            if (trackList == null || zone != tracksZone || Array.Exists(trackList, t => t == null)) { tracksZone = zone; trackList = UnityEngine.Object.FindObjectsByType<WreckTrack>(FindObjectsSortMode.None); }
            return trackList;
        }
        /// <summary>A wreck riding a track will be on this point of the plane that many seconds from now, within its own reach and <paramref name="room"/> more (the ship's hull and a
        /// margin): the track's name, or null. Read off what the lamps say, a track's wrecks on its own clock at its own speeds (WreckTrack.PieceAt); a wreck still sunk at either end
        /// of an open track is under the plane and hits nothing. ReplayWays asks it for every sample of a way in flight (Nacre Grave's tow line and berth ring, the Ring's channel).</summary>
        public static string WreckOn(Vector2 p, float ahead, float room)
        {
            if (homeHandsOff) return null;
            foreach (var track in Tracks())
            {
                if (track == null || !track.isActiveAndEnabled) continue;
                int count = Mathf.Max(1, track.pieces.Length);
                float time = track.Clock + ahead;
                for (int i = 0; i < count; i++)
                {
                    var at = track.PieceAt(i, time);
                    if (at.y < track.plane - .5f) continue;
                    float reach = track.pieceRadius + room;
                    if ((new Vector2(at.x, at.z) - p).sqrMagnitude < reach * reach) return string.IsNullOrEmpty(track.trackName) ? track.name : track.trackName;
                }
            }
            return null;
        }
        /// <summary>How far round a ship a wreck must pass: its hull and a margin for the turn it makes.</summary>
        public static float WreckRoom(CombatActor lead) => (lead != null && lead.TryGetComponent<CharacterController>(out var hull) ? hull.radius : 2) + .6f;

        // ---------- the Foundry Saint: a planted arm's collar, then the climb ----------
        /// <summary>Where the lead stands to strike a collar: this far out from the press along its arm, on the floor; how near that spot it is before it swings; and how far up
        /// a fallen arm's road it goes before it strikes the press's back from it (RoadAt: 0 the foot, 1 the collar).</summary>
        public const float CollarStand = 1.3f, CollarNear = .6f, RoadTop = .86f;
        bool saintPress; double saintPressAt = -1;
        static BossController Saint(Vector3 near)
        {
            foreach (var body in Health.All)
                if (body != null && body.Alive && !body.friendly && body.id == "FoundrySaint" && (body.transform.position - near).sqrMagnitude < 60 * 60) return body.GetComponent<BossController>();
            return null;
        }
        /// <summary>In a fight with the Foundry Saint the room says what to do: its arms are raised and bear the press, so its body takes nothing, and a slam or the head's lift
        /// plants an arm, its collar burning amber for a few seconds (a blow there lands); with three arms down, the press is bare to a hero on its back, up a fallen arm's road.
        /// The arm to work: the nearest whose collar is open, or (bare) the nearest fallen at rest. None while nothing is open and the press isn't bare.</summary>
        static SaintArm SaintArmToWork(CombatActor lead)
        {
            if (homeHandsOff || SaintArm.All.Count == 0) return null;
            var saint = Saint(lead.transform.position); if (saint == null) return null;
            bool bare = saint.SaintBare;
            SaintArm best = null; float least = float.PositiveInfinity;
            foreach (var arm in SaintArm.All)
            {
                if (arm == null || !arm.isActiveAndEnabled || arm.settledBy != "FoundrySaint") continue;
                if (bare ? !(arm.IsReleased && arm.Resting) : !arm.Unlocked) continue;
                var spot = bare ? arm.RoadPoint(0) : CollarSpot(arm);
                float gap = (spot - lead.transform.position).sqrMagnitude;
                if (gap < least) { least = gap; best = arm; }
            }
            return best;
        }
        static Vector3 CollarSpot(SaintArm arm) => arm.CollarFloor + arm.Outward * CollarStand;
        /// <summary>To the collar by the floor's own way, then still before it, the attack pressed as a thumb does (the lock on the Saint turns each swing to the press, and the collar
        /// stands between); or, bare, to the fallen arm's foot by the floor's way, up its road (which isn't in the bake) straight, and still near its top, striking. Nothing of the
        /// step's own buttons is pressed meanwhile.</summary>
        Vector2 WorkSaint(CombatActor lead, SaintArm arm)
        {
            saintPress = false;
            var at = lead.transform.position; float yaw = ZoneController.Current.CameraProfile.yaw;
            Vector2 Push(Vector3 heading, float speed = 1) { var local = Quaternion.Euler(0, -yaw, 0) * heading.normalized; return new Vector2(local.x, local.z) * speed; }
            bool Strike() { double now = clock.Elapsed.TotalSeconds; if (now - saintPressAt > .46) saintPressAt = now; return now - saintPressAt < .1; }
            if (!arm.IsReleased)
            {
                var spot = CollarSpot(arm); var gap = spot - at; gap.y = 0;
                if (gap.magnitude > CollarNear) return Push(ReviveHeading(lead, spot, gap, false), Mathf.Clamp(gap.magnitude, .35f, 1));
                saintPress = Strike(); return Vector2.zero;
            }
            bool up = at.y - arm.Floor >= BossController.SaintTune.ClimbHeight * .8f || arm.OnRoad(at);
            if (!up)
            {
                var foot = arm.RoadPoint(0) + arm.Outward * .8f; var gap = foot - at; gap.y = 0;
                if (gap.magnitude > .7f) return Push(ReviveHeading(lead, foot, gap, false));
                var along = arm.RoadPoint(RoadTop) - at; along.y = 0; return Push(along);
            }
            if (arm.RoadAt(at) < RoadTop) { var along = arm.RoadPoint(RoadTop) - at; along.y = 0; return Push(along, .8f); }
            saintPress = Strike(); return Vector2.zero;
        }

        // ---------- the Debris Cutter: flown after, and fired on wherever its shots reach ----------
        /// <summary>How near the Cutter the lead flies before he brakes and holds his nose on it, and how far off his Fire is held: the shots fly 72 m, and its hull takes a blow at any
        /// time (its pods only choose which goes, D281); inside the lock's thirty metres a person keeps shooting it.</summary>
        public const float CutterNear = 11, CutterFire = 28;
        /// <summary>The Cutter keeps 18 m off the lead and flies to another of its lanes at 13 m/s whenever he comes nearer (BossController.CutterTune.KeepWhole, CutterLane). The
        /// fight steps hold the brake, the flight driver's way of keeping a firing position (a braked ship tops out at 2.1 m/s), and fired only from inside the step's 5 m: in
        /// chapter-61's first play (nov14-recon31) the lead crept about the south-west quadrant at 2.1 m/s for sixteen rounds, fired in 7% of them, and left the Cutter at 60% of its bar
        /// after 471 s. A person flies after it with the brake off, brakes once near enough to keep the nose on it, and holds Fire whenever it is in reach. Null for any other
        /// target, so every other flight fight keeps the hands it was timed with; the stick to push otherwise, and whether the brake comes off.</summary>
        static Vector2? CutterHeading(CombatActor lead, Health chased, Vector3 difference, out bool unbraked, out bool firing)
        {
            unbraked = firing = false;
            if (homeHandsOff || !lead.flight || chased == null || !chased.Alive) return null;
            var boss = chased.GetComponentInParent<BossController>(); if (boss == null || boss.Health == null || boss.Health.id != "DebrisCutter") return null;
            difference.y = 0; float away = difference.magnitude; firing = away <= CutterFire;
            var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * (difference.sqrMagnitude > .01f ? difference.normalized : lead.motor.Facing);
            if (away > CutterNear) { unbraked = true; return new Vector2(local.x, local.z); }
            // Near: braked, the nose kept on it with a touch of the stick (the flight chase's own .25, clear of the dead zone).
            return Vector3.Angle(lead.motor.Facing, difference) > 5 ? new Vector2(local.x, local.z) * .25f : Vector2.zero;
        }
    }
}
