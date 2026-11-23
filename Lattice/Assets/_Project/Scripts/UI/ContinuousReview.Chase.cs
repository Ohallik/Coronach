using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.UI
{
    // The driver's chase on foot when a straight line won't do, and what it locks in a fight whose bar says how to open the boss (the Drowned Waystation's great hall,
    // November 14). The Rotbloom's lure-stalks stand on plinths whose only way up is a ramp on the side facing the hall's middle. The chase pushed straight at the
    // nearest stalk from the door, slid round the plinth's steep side into the gap between it and the wall, and stood there for half a minute (first whole chain and the
    // reconnaissance after it): the way ChaseBlocked asked for began where the navigation's nearest point to the lead was, and the nearest point to a body in that gap
    // is the plinth's top, 0.6 m over his head, so the "way" was the straight push again. And with one stalk burned the lock stayed on the boss, shut, and the fight
    // ground through its 30% blows for five minutes without burning a second. Ordinary pad presses and what a player reads off the screen, as everywhere in the driver.
    public sealed partial class ContinuousReview
    {
        /// <summary>The driver as it was before these hands, for a test's broken state (RotbloomDriverTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool chaseHandsOff;

        // ---------- the floor's way to what is chased ----------
        /// <summary>A rise a hero walks onto without a ramp: the controller's .35 m step and a little. Ground further above or below is other ground.</summary>
        const float Climb = .45f;
        /// <summary>The floor's way is walked when it is this much longer than the straight line (it goes round something), or when it ends on other ground.</summary>
        const float WayBends = .75f;
        /// <summary>Stalled this long on the way as well: a side-step, a side at a time. Stalled this long all told: the lock goes to something that can be reached.</summary>
        const float SideAfter = 1.2f, GiveUpAfter = 5f;
        NavMeshPath wayPath; Vector3[] wayCorners; double wayAt = -1, pushedAt = -1, gainAt = -1, sideUntil = -1; Health wayFor, gainFor; Vector3 gainFrom; float side = 1;
        /// <summary>Which way the stick goes after what is chased on foot. Straight at it, as ever, while the floor between is open; along the baked floor's way when that way
        /// goes round something or climbs to other ground (a stalk on its plinth, up the ramp); and, if even that stops gaining, a side-step a side at a time. The way starts
        /// from the floor the lead stands on, never from the nearest navigation in the air above him.</summary>
        Vector3 ChaseHeading(CombatActor actor, Health chased, Vector3 straight)
        {
            if (chaseHandsOff) return ChaseBlocked(actor, chased) ? ReviveHeading(actor, chased.transform.position, straight, false) : straight;
            double now = clock.Elapsed.TotalSeconds; var at = actor.transform.position;
            // A chase taken up again (after swinging, a roll, a step that didn't chase) starts its count afresh.
            if (chased != gainFor || now - pushedAt > .3) { gainFor = chased; gainFrom = at; gainAt = now; sideUntil = -1; }
            pushedAt = now;
            // Another hand has the stick (a wheel, a clamp, a collar, the walk to a fallen partner): standing still for it is no stalled chase.
            if (OtherHands(actor)) { gainFrom = at; gainAt = now; sideUntil = -1; }
            var moved = at - gainFrom; moved.y = 0;
            if (moved.magnitude > ChaseGain) { gainFrom = at; gainAt = now; }
            double stalled = now - gainAt;
            if (chased != wayFor || now - wayAt > .25) { wayFor = chased; wayAt = now; wayCorners = FloorWay(at, chased, stalled > ChaseStall); }
            var heading = straight;
            if (wayCorners != null) foreach (var corner in wayCorners) { var gap = corner - at; gap.y = 0; if (gap.magnitude > .6f) { heading = gap; break; } }
            heading.y = 0;
            // Not gaining on the floor's way either (somebody standing on the ramp, a corner cut against a wall): aside for a moment, then on, the other side next time.
            if (stalled > SideAfter && now >= sideUntil + .6) { side = -side; sideUntil = now + .8; }
            if (now < sideUntil && heading.sqrMagnitude > .0001f) heading = Vector3.Cross(Vector3.up, heading.normalized) * side + heading.normalized * .3f;
            return heading;
        }
        bool OtherHands(CombatActor actor)
        {
            var party = PartyController.Current;
            // A talk being read in the fight holds everyone still: no stalled chase either.
            return raise != null && !raise.Health.Alive || AtTheWheel(party) || AtAClamp(party) || AtACoupling(party) || talkFrom >= 0 ||
                WheelToWork(actor) != null || ClampToWork(actor) != null || SaintArmToWork(actor) != null;
        }

        // ---------- an ordinary fight with nothing in the lock ----------
        /// <summary>A short walk, as a person takes one to something in a fight: the lock's reach and a half. Further off, an enemy is only gone after inside the fight the lead is in
        /// (its volume, started and not cleared), and a fallen partner further off is left where she lies.</summary>
        const float ShortWalk = LockRange * 1.5f;
        /// <summary>The two walks below as they were first written, without their bounds, for a test's broken state (DriverBoundsTests sets it by reflection and puts it back).</summary>
        static bool boundsOff;
        Health fightFar, fightLogged; double fightAt = -1;
        static string fightsZone; static World.EncounterVolume[] fightList;
        static World.EncounterVolume[] Fights()
        {
            string zone = SceneFlow.Current != null ? SceneFlow.Current.Zone : "";
            if (fightList == null || zone != fightsZone || Array.Exists(fightList, f => f == null)) { fightsZone = zone; fightList = UnityEngine.Object.FindObjectsByType<World.EncounterVolume>(FindObjectsSortMode.None); }
            return fightList;
        }
        /// <summary>The body is one of the fight the lead stands in: a volume started and not cleared holds both of them.</summary>
        static bool InTheSameFight(Vector3 lead, Vector3 body)
        {
            foreach (var fight in Fights())
            {
                if (fight == null || !fight.Started || fight.Cleared || !fight.TryGetComponent<Collider>(out var box)) continue;
                var bounds = box.bounds; bounds.Expand(new Vector3(2, 6, 2));
                if (bounds.Contains(lead) && bounds.Contains(body)) return true;
            }
            return false;
        }
        /// <summary>The stick toward the nearest awake enemy, by the floor's way, while the lock has nothing. A body that keeps its place (a pour drone on its rail across the gallery)
        /// neither comes nor is ever in front of a lead who stands still, and the lock press takes nothing behind him further than 21 m (PlayerBrain.FindTarget halves what is behind):
        /// the chase stood two minutes beside the gallery's east rail with the west drone alive (reconnaissance s48, November 14). A person turns and walks to it, and the lock
        /// takes it as he comes. Only to an enemy within a short walk, or one of the fight the lead stands in; never while another hand has the pad (a wheel, a clamp, a coupling,
        /// a collar, a fallen partner, a talk). Null with something locked, with a boss fighting about (its own rules: what its bar names, a boss gone beyond the lock), or in flight.</summary>
        Vector2? TowardFight(CombatActor lead)
        {
            if (chaseHandsOff || lead == null || lead.flight || lead.target != null && lead.target.Alive) return null;
            if (BossController.Focus(lead.transform.position, LostBossWithin) != null) return null;
            if (!boundsOff && OtherHands(lead)) return null;
            double now = clock.Elapsed.TotalSeconds; var at = lead.transform.position;
            if (now - fightAt > .5)
            {
                fightAt = now; fightFar = null; float least = float.PositiveInfinity;
                foreach (var body in Health.All)
                {
                    if (body == null || body.friendly || !body.Alive || !body.Targetable || EnemyBrain.OtherForm(lead, body)) continue;
                    var offset = body.transform.position - at; offset.y = 0;
                    if (boundsOff ? offset.sqrMagnitude > 3600 : offset.sqrMagnitude > ShortWalk * ShortWalk && !InTheSameFight(at, body.transform.position)) continue;
                    // Awake, as the game counts it (EnemyBrain.AwakeNear): a body on show before its fight, or one put to rest, is no fight.
                    var owner = body.reactionOwner != null ? body.reactionOwner : body;
                    if (!owner.TryGetComponent<EnemyBrain>(out var brain) || !brain.enabled || brain.Passive) continue;
                    float way = WayLength(at, body); if (way < least) { least = way; fightFar = body; }
                }
                if (fightFar != null && fightFar != fightLogged) { fightLogged = fightFar; Debug.Log("QUALITY_TOWARD_FIGHT " + fightFar.name); }
            }
            if (fightFar == null || !fightFar.Alive) return null;
            var heading = fightFar.transform.position - at; heading.y = 0;
            wayPath ??= new NavMeshPath();
            if (FloorPath(at, fightFar.transform.position, wayPath, out _, out _)) foreach (var corner in wayPath.corners) { var gap = corner - at; gap.y = 0; if (gap.magnitude > .6f) { heading = gap; break; } }
            if (heading.sqrMagnitude < .0001f) return null;
            var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * heading.normalized;
            return new Vector2(local.x, local.z);
        }

        /// <summary>Whether the lead goes to raise the fallen partner now: only within a short walk, and never from where something is offered on a step that isn't a fight's (a coupling's seat,
        /// a cradle, a collar): there the press comes first. Route 56's lead walked off coupling 3's seat after Idra, down since the run's twenty-ninth walk and 160 m back up it, into
        /// the dawn (reconnaissance s56, November 14). Further off she is left where she lies, as a person carries on and raises her when the way comes back to her or a Spike is
        /// in hand.</summary>
        bool RaiseWithin(CombatActor lead, CombatActor fallen)
        {
            if (boundsOff || lead == null || fallen == null) return true;
            var gap = fallen.transform.position - lead.transform.position; gap.y = 0;
            if (gap.magnitude > ShortWalk) return false;
            // At something offered (the seat's prompt is up) on a step that isn't a fight's: the press it is there for comes first.
            return !(step != null && !step.approachTarget && PromptService.Interaction != null);
        }

        // ---------- a talk in the middle of a fight ----------
        /// <summary>A line of a conversation as a route's own read steps take it (lantern_routes.read): held this long, then A pressed for the rest of the beat.</summary>
        const double TalkLine = 1.9, TalkBeat = 2.25;
        double talkFrom = -1;
        /// <summary>The pad while a conversation is up in a fight step with a boss's fight underway (Vessa Korl writes her squad off as she falls back: an AfterTalk that plays during the
        /// fight): read as a person reads it, each line held and then A pressed, and nothing else. A conversation stops every enemy's clock, so the fight waits under it: it stood open
        /// for two and a half rounds, 108 s, the lead out of reach and so pressing nothing (reconnaissance s48, November 14).</summary>
        bool TalkInAFight(out GamepadState reading)
        {
            reading = default;
            if (chaseHandsOff || step == null || !step.approachTarget || ObserveUi() != "dialogue" || !BossController.AnyFightUnderway) { talkFrom = -1; return false; }
            double now = clock.Elapsed.TotalSeconds;
            if (talkFrom < 0) { talkFrom = now; Debug.Log("QUALITY_READS_TALK in " + step.name); }
            double into = (now - talkFrom) % TalkBeat;
            if (into > TalkLine && into < TalkLine + .15) reading = reading.WithButton(GamepadButton.South);
            return true;
        }
        /// <summary>The baked floor's way from the lead to where a hero stands to strike this, if the straight line won't do (or <paramref name="always"/>): null to go straight.</summary>
        Vector3[] FloorWay(Vector3 feet, Health chased, bool always)
        {
            wayPath ??= new NavMeshPath();
            if (!FloorPath(feet, chased.transform.position, wayPath, out var start, out var end)) return null;
            var corners = wayPath.corners; float length = 0;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            var line = end - start; line.y = 0;
            return always || Mathf.Abs(end.y - start.y) > Climb || length > line.magnitude + WayBends ? corners : null;
        }
        /// <summary>A whole way on the baked floor between two points, each taken on its own ground: the navigation nearest a point on its own level (within a step), and only
        /// failing that the nearest at all. The nearest at all to a hero in the gap behind a plinth is the plinth's top.</summary>
        /// <remarks>Only a body's own position is a height to keep to (<paramref name="toIsABody"/>: what is chased, a fallen hero). A route's point is a point on the plan whose height
        /// is the route's own (a walk's y is 0 wherever the floor is), and is taken as it always was, the nearest navigation within 1.5 m: kept to its "own level" on the mire's
        /// third island, whose bank stands 0.7 m over the point, the way ended on lower ground 1.5 m off it and a 5 s walk ran about for 17 s and never arrived (November 14).</remarks>
        static bool FloorPath(Vector3 from, Vector3 to, NavMeshPath path, out Vector3 start, out Vector3 end, bool toIsABody = true)
        {
            start = from; end = to;
            if (GroundNavigation.Current == null || !OnTheFloor(from, 1.2f, out start)) return false;
            bool ownLevel = toIsABody || planGoalsOnTheirLevel;
            if (!(ownLevel ? OnTheFloor(to, 1.5f, out end) : Nearest(to, out end))) return false;
            return NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }
        /// <summary>The way GroundNavigation.FindPath takes a goal: the nearest navigation within 1.5 m, or within 1.2.</summary>
        static bool Nearest(Vector3 point, out Vector3 floor)
        {
            floor = point;
            if (!NavMesh.SamplePosition(point, out var hit, 1.5f, NavMesh.AllAreas) && !NavMesh.SamplePosition(point, out hit, 1.2f, NavMesh.AllAreas)) return false;
            floor = hit.position; return true;
        }
        /// <summary>The driver of 14:26 on November 14, for a test's broken state (RotbloomDriverTests sets it by reflection and puts it back): a route's point taken on its own
        /// level like a body's. No route sets it.</summary>
        static bool planGoalsOnTheirLevel;
        static bool OnTheFloor(Vector3 point, float anyWithin, out Vector3 floor)
        {
            if (FloorNear(point, out floor)) return true;
            if (!NavMesh.SamplePosition(point, out var any, anyWithin, NavMesh.AllAreas)) return false;
            floor = any.position; return true;
        }
        /// <summary>The baked floor nearest a point on that point's own level: straight under it, or the nearest found on rings round it out to three metres.</summary>
        static bool FloorNear(Vector3 feet, out Vector3 floor)
        {
            floor = feet;
            if (NavMesh.SamplePosition(feet, out var hit, .6f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - feet.y) <= Climb) { floor = hit.position; return true; }
            for (float r = .5f; r <= 3.01f; r += .5f)
            {
                float least = float.PositiveInfinity;
                for (int k = 0; k < 12; k++)
                {
                    var p = feet + Quaternion.Euler(0, k * 30, 0) * Vector3.forward * r;
                    if (!NavMesh.SamplePosition(p, out hit, .45f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - feet.y) > Climb) continue;
                    var gap = hit.position - feet; gap.y = 0;
                    if (gap.magnitude < least) { least = gap.magnitude; floor = hit.position; }
                }
                if (least < float.PositiveInfinity) return true;
            }
            return false;
        }
        /// <summary>How far a hero walks to stand where it can strike this: along the floor's way, or a straight line and half again where there is none.</summary>
        float WayLength(Vector3 from, Health to)
        {
            wayPath ??= new NavMeshPath();
            var line = to.transform.position - from; line.y = 0;
            if (!FloorPath(from, to.transform.position, wayPath, out _, out _)) return line.magnitude * 1.5f;
            var corners = wayPath.corners; float length = 0;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            return length;
        }

        // ---------- what the bar says to strike ----------
        /// <summary>How far the game's lock reaches (PlayerBrain: thirty metres), and how much nearer another named thing must be before the lock leaves the one it is on.</summary>
        const float LockRange = 30, Keep = 3;
        double wantAt = -1, flickAt = -1; Health wanted;
        /// <summary>What a person would have the lock on now, read off the boss's bar and the objective as the screen shows them, or null to leave the lock where it is.
        /// A boss whose mouths, plates or shell are open (its break: "A MOUTH GAPES · STRIKE IT") is struck, if the lead can be at it before it shuts. A boss that is shut (a
        /// blow on it lands at a share) whose bar or objective names what opens it ("BURN A LURE-STALK") has those struck first, the nearest by the floor's way. A chase that
        /// has stood without gaining for <see cref="GiveUpAfter"/> seconds goes to the next thing it can reach.</summary>
        Health WantedTarget(CombatActor lead)
        {
            double now = clock.Elapsed.TotalSeconds;
            if (now - wantAt < .5) return wanted != null && wanted.Targetable ? wanted : null;
            wantAt = now; wanted = null; namedFar = null;
            var at = lead.transform.position; var current = lead.target != null && lead.target.Targetable ? lead.target : null;
            // A step that ends when the thing it locked falls keeps to that thing: the route chose it (an organ the boss is braced on), and a boss that breaks
            // meanwhile is not what the step is for. The Bellows broke twice while its east organ stood, the lock went to its body each time, and the organ was
            // never struck in thirty seconds (November 22, two builds).
            if (step != null && step.until == "targetDown" && stepTarget != null && stepTarget.Alive && stepTarget.Targetable && !EnemyBrain.OtherForm(lead, stepTarget)) { wanted = stepTarget; return wanted; }
            var boss = BossController.Focus(at);
            if (boss != null && boss.TryGetComponent<Health>(out var body) && body.Alive && body.Targetable && !EnemyBrain.OtherForm(lead, body))
            {
                if (body.Broken) { if (InTime(lead, body)) wanted = body; return wanted; }
                if (Shut(body, lead))
                {
                    float least = float.PositiveInfinity, keep = float.PositiveInfinity, farthest = float.PositiveInfinity;
                    foreach (var thing in Health.All)
                    {
                        if (thing == null || thing.friendly || !thing.Targetable || EnemyBrain.OtherForm(lead, thing) || thing == body || thing.reactionOwner == body || !Named(boss, thing)) continue;
                        float way = WayLength(at, thing);
                        // Named and standing, but further than the lock reaches: walked toward until it can be locked.
                        if (!Lockable(lead, thing)) { if (way < farthest) { farthest = way; namedFar = thing; } continue; }
                        if (thing == current) keep = way;
                        if (way < least) { least = way; wanted = thing; }
                    }
                    if (current != null && keep <= least + Keep) wanted = current;
                    if (wanted != null) { namedFar = null; return wanted; }
                    if (namedFar != null) return null;
                }
            }
            // Stood still out of reach, pushing, for seconds on end: whatever else can be reached, the nearest by the floor's way.
            if (current != null && gainFor == current && now - pushedAt < .3 && now - gainAt > GiveUpAfter)
            {
                float least = float.PositiveInfinity;
                foreach (var thing in Health.All)
                {
                    if (!Lockable(lead, thing) || thing == current) continue;
                    float way = WayLength(at, thing); if (way < least) { least = way; wanted = thing; }
                }
                if (wanted != null) { gainAt = now; Debug.Log("QUALITY_CHASE_GAVE_UP " + current.name + " for " + wanted.name); }
            }
            return wanted;
        }
        /// <summary>The nearest standing thing of the enemy's that is not a boss's own body, within fifteen metres: the part a route has just walked the lead up to
        /// (ContinuousReview.BeginStep, a targetDown step whose lock press took nothing or took the boss).</summary>
        static Health NearestPart(CombatActor lead)
        {
            Health best = null; float least = 15f * 15f; var at = lead.transform.position;
            foreach (var thing in Health.All)
            {
                if (thing == null || thing.friendly || !thing.Alive || !thing.Targetable || EnemyBrain.OtherForm(lead, thing) || thing.GetComponent<BossController>() != null) continue;
                float d = (thing.transform.position - at).sqrMagnitude;
                if (d < least) { least = d; best = thing; }
            }
            return best;
        }
        /// <summary>What the bar names that stands further off than the lock reaches, with nothing named within it.</summary>
        Health namedFar;
        /// <summary>The stick toward what the bar names beyond the lock's reach, by the floor's way, until the lock can take it; null while there is no such thing. The lock press
        /// takes nothing beyond thirty metres, and a chase with nothing it is told to strike in reach would grind a shut boss at its share.</summary>
        Vector2? TowardNamed(CombatActor lead)
        {
            if (chaseHandsOff || lead == null || lead.flight) return null;
            WantedTarget(lead);
            var far = namedFar;
            if (far == null || !far.Targetable || Lockable(lead, far)) return null;
            var at = lead.transform.position; var heading = far.transform.position - at; heading.y = 0;
            wayPath ??= new NavMeshPath();
            if (FloorPath(at, far.transform.position, wayPath, out _, out _)) foreach (var corner in wayPath.corners) { var gap = corner - at; gap.y = 0; if (gap.magnitude > .6f) { heading = gap; break; } }
            if (heading.sqrMagnitude < .0001f) return null;
            var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * heading.normalized;
            return new Vector2(local.x, local.z);
        }
        /// <summary>The boss's state line under its bar, or the objective panel's line while its bar is up, names this by the name its target card shows ("BURN A LURE-STALK").</summary>
        static bool Named(BossController boss, Health thing)
        {
            string name = DisplayNames.Target(thing.id);
            if (string.IsNullOrWhiteSpace(name)) return false;
            string said = (boss.StatusText + "  " + boss.FightGoal("")).ToUpperInvariant();
            return said.IndexOf(name.ToUpperInvariant(), StringComparison.Ordinal) >= 0;
        }
        /// <summary>Whose tells a person watches while striking this: something with no body of its own that the fighting boss's bar names (a lure-stalk on its plinth) is struck under
        /// that boss's moves. The chase never rolled at one while it burned a stalk (its tells were looked for on the stalk, which has none), and every move the bloom made landed.</summary>
        Health BarTells(CombatActor lead, Health carrier)
        {
            if (chaseHandsOff || carrier == null || carrier.GetComponent<BossController>() != null || carrier.GetComponent<EnemyBrain>() != null) return carrier;
            var boss = BossController.Focus(carrier.transform.position);
            return boss != null && boss.TryGetComponent<Health>(out var body) && body.Alive && Named(boss, carrier) ? body : carrier;
        }
        /// <summary>Which way a roll goes: aside as ever, unless that leaves the ground the lead stands on (off a plinth's top, into a wall): then the other side, then through what he
        /// is striking (a lure-stalk stands in the middle of its top), as a person rolls through a flash. Untouchable for the whole roll either way.</summary>
        Vector3 RollWay(CombatActor lead, Vector3 aside)
        {
            if (chaseHandsOff || lead == null || lead.flight || GroundNavigation.Current == null || !FloorNear(lead.transform.position, out var from)) return aside;
            float hop = lead.Style != null && lead.Style.hop > 0 ? lead.Style.hop : 3.3f;
            // How much of the roll stays on the baked floor. The bake stops a hero's half-width short of a drop (.55 m), so a roll that ends that little past it ends on the top.
            float Room(Vector3 way) { way.y = 0; if (way.sqrMagnitude < .0001f) return -1; var end = from + way.normalized * hop; return NavMesh.Raycast(from, end, out var hit, NavMesh.AllAreas) ? Vector3.Distance(from, hit.position) : hop; }
            const float Edge = .6f;
            var through = lead.target != null ? lead.target.transform.position - lead.transform.position : Vector3.zero; through.y = 0;
            float side = Room(aside), other = Room(-aside), past = Room(through);
            if (side >= hop - Edge) return aside;
            if (other >= hop - Edge) return -aside;
            if (past >= hop - Edge) return through.normalized;
            // None stays: the one that keeps most of the roll on the ground. Falling back to the roll aside put the lead in the gap behind a plinth.
            return past >= side && past >= other ? through.normalized : other > side ? -aside : aside;
        }
        /// <summary>Something the game's lock would take: an enemy's, standing, within its reach, of the lead's own form.</summary>
        static bool Lockable(CombatActor lead, Health thing) => thing != null && !thing.friendly && thing.Targetable && !EnemyBrain.OtherForm(lead, thing) &&
            (thing.transform.position - lead.transform.position).sqrMagnitude < LockRange * LockRange;
        /// <summary>A blow on it lands at a share now (shut mouths, a warm shell): neither whole nor nothing.</summary>
        static bool Shut(Health body, CombatActor lead)
        {
            if (body.Plate == null) return false;
            float share = body.Plate(new DamagePacket { amount = 1, type = DamageType.Kinetic, source = lead.Health });
            return share > 0 && share < 1;
        }
        /// <summary>The lead can walk to the open body, by the floor's way, and be at it with a moment to strike before its opening ends.</summary>
        bool InTime(CombatActor lead, Health body)
        {
            float radius = body.TryGetComponent<CharacterController>(out var capsule) ? capsule.radius * Mathf.Max(Mathf.Abs(body.transform.lossyScale.x), Mathf.Abs(body.transform.lossyScale.z)) : 0;
            float walk = Mathf.Max(0, WayLength(lead.transform.position, body) - radius - 1.5f);
            float pace = (lead.motor is GroundMotor ground ? ground.speed : 6.7f) * lead.Health.Pace * .8f;
            return walk / Mathf.Max(1, pace) + .5f < body.BrokenUntil - Time.time;
        }
        /// <summary>The d-pad press that brings the lock round to what is wanted, in the game's own order (PlayerBrain.CycleTarget: by angle from where the lead faces), the
        /// shorter way round; one tap every .4 s until the lock is on it. Null while nothing is wanted or the lock is already there.</summary>
        GamepadButton? ChaseFlick(CombatActor lead)
        {
            if (chaseHandsOff || lead == null || lead.flight) return null;
            var want = WantedTarget(lead);
            if (want == null || lead.target == want) return null;
            var targets = new List<Health>();
            foreach (var h in Health.All) if (h != null && h.Targetable && !h.friendly && !EnemyBrain.OtherForm(lead, h) && (h.transform.position - lead.transform.position).sqrMagnitude < 900) targets.Add(h);
            var forward = lead.transform.forward; var from = lead.transform.position;
            targets.Sort((a, b) => Vector3.SignedAngle(forward, a.transform.position - from, Vector3.up).CompareTo(Vector3.SignedAngle(forward, b.transform.position - from, Vector3.up)));
            int n = targets.Count, goal = targets.IndexOf(want), here = targets.IndexOf(lead.target);
            if (goal < 0 || n == 0) return null;
            // From nothing in the list the game's first press right lands on the first: right is the only way that counts from there.
            bool rightward = here < 0 || ((goal - here) % n + n) % n <= ((here - goal) % n + n) % n;
            double now = clock.Elapsed.TotalSeconds;
            if (now - flickAt > .4)
            {
                flickAt = now;
                if (want != flickedFor) { flickedFor = want; Debug.Log("QUALITY_LOCK_FOR " + want.name + " (on " + (lead.target != null ? lead.target.name : "nothing") + ")"); }
            }
            return now - flickAt < .12 ? rightward ? GamepadButton.DpadRight : GamepadButton.DpadLeft : null;
        }
        Health flickedFor;
    }
}
