"""Writes chapter 3's replay routes for Xylos Nearspace and Rootmarket (chapter-17 to chapter-19) from the builder's own places.

Nearspace places are worked out from the lane the builder lays (XylosBuilder.Nearspace.cs: Lane, Beside, Barges, Nests), and checked against
the coordinates the build logs (docs/maps/XylosNearspace.md, Evidence) before anything is written. Rootmarket's are the builder's literals
(XylosBuilder.Rootmarket.cs: RootArrival, RopeAt, the bake's must-reach points, RootWays). Nobody has run these: they are made right by
construction, so before writing, every leg the driver flies or walks in a straight line is checked here against what stands solid there
(the barges where they lie and where they rest, the mirror's loose panes, the beacons, the lane's limits; in town the floor the builder's
height function makes, the named people and the townsfolk). XylosWorldTests.EveryWalkOnTheChapterThreeRoutesFollowsFloorABodyCanCross then
checks the same legs on each zone's real navigation and colliders.

The routes chain from chapter-16-lantern-home's autosave: aboard Tallow with chapter 2 told.
  chapter-17-xylos-nearspace   Abri and Pell aboard Tallow, the Xylos warp, Teo's hail, three barges cut free, the patrol, the claim drones, the delta beacon
  chapter-18-rootmarket        the arrival talk, three pulls on the rope, Ruva's terms, a save at the mender's cradle
  chapter-19-rootmarket-errands  side content: Hadda's press, Binna's stall and baskets, the seed house and the notice, the upper walk's cache, Jask,
                               Senn and his island's box, Teo, then the river gate to the Braided River

  python tools/xylos_routes.py
"""
import math

from lantern_routes import COMBAT_SPEED, FLIGHT_SPEED, Route

# Chapter routes are numbered on from chapter-16-lantern-home (CHAPTER_GUIDE section 10; route_lint's FIRST_EXTRA_ROUTE is 17).
PREFIX = "chapter-"

# ------------------------------------------------------------------ Nearspace: the lane, as XylosBuilder.Nearspace.cs lays it
LANE = [(0, 0), (30, 240), (130, 520), (300, 800), (520, 1000), (760, 1120), (980, 1180)]
LANE_HALF = 62
HULL = 1.78  # HeroCollision.HullRadius("Taren"), the larger of the two
BARGES = [(252, 25, 38, 296, 7, -30), (952, -28, -48, 990, 6, 38), (1438, -31, 24, 1474, 8, -34)]  # s, side, yaw, rest s, rest side, turn
BEACON_S = 1650
PANES = [(-20, -16, 8, 20), (15, -22, 10, -50), (24, 9, 7, 70), (-9, 22, 9, -15), (-30, 6, 6.5, 110), (6, 34, 8.5, 40), (36, -9, 6, -80)]  # dx, dz, size, yaw


def along(s):
    heading = (0.0, 1.0)
    for i in range(1, len(LANE)):
        a, b = LANE[i - 1], LANE[i]
        leg = math.dist(a, b)
        heading = ((b[0] - a[0]) / leg, (b[1] - a[1]) / leg)
        if s <= leg or i == len(LANE) - 1:
            return (a[0] + heading[0] * s, a[1] + heading[1] * s), heading
        s -= leg
    return LANE[0], heading


def beside(s, starboard):
    (x, z), (hx, hz) = along(s)
    return (round(x + hz * starboard, 1), round(z - hx * starboard, 1))


def from_lane(p):
    best = 1e9
    for i in range(1, len(LANE)):
        best = min(best, segment_distance(p, LANE[i - 1], LANE[i]))
    return best


def segment_distance(p, a, b):
    dx, dz = b[0] - a[0], b[1] - a[1]
    length = dx * dx + dz * dz
    t = 0 if length == 0 else max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / length))
    return math.dist(p, (a[0] + dx * t, a[1] + dz * t))


def box_distance(p, centre, yaw, half_x, half_z):
    """How far a point is outside a box on the plan (negative inside), the box turned by yaw degrees as Unity turns it."""
    r = math.radians(yaw)
    dx, dz = p[0] - centre[0], p[1] - centre[1]
    local_x = dx * math.cos(r) - dz * math.sin(r)
    local_z = dx * math.sin(r) + dz * math.cos(r)
    ox, oz = abs(local_x) - half_x, abs(local_z) - half_z
    return math.hypot(max(ox, 0), max(oz, 0)) + min(max(ox, oz), 0)


def solids_in_flight():
    """What is solid at the flight plane, as boxes: each barge where it lies snagged and where it rests (16.4 m by 4.6), the seven loose panes round
    barge two (made wider than their face for the roll they lie at), and the two warp beacons (WorldBuilder.Warp's 2 m piece)."""
    boxes = []
    for n, (s, side, yaw, rs, rside, turn) in enumerate(BARGES):
        boxes.append((f"barge {n + 1} snagged", beside(s, side), yaw, 2.3, 8.2))
        boxes.append((f"barge {n + 1} at rest", beside(rs, rside), yaw + turn, 2.3, 8.2))
    two = beside(BARGES[1][0], BARGES[1][1])
    for dx, dz, size, yaw in PANES:
        boxes.append((f"loose pane {dx} {dz}", (two[0] + dx, two[1] + dz), yaw, size / 2 + 1, size * 0.31 + 1.5))
    boxes.append(("warp home", (-16, -2), 0, 1, 1))
    boxes.append(("delta beacon", beside(BEACON_S, 0), 0, 1, 1))
    return boxes


# The places the build logs (docs/maps/XylosNearspace.md, Evidence): what the lane above must reproduce before a route is written from it.
LOGGED = {"barge 1": ((56.9, 241.1), beside(252, 25)), "barge 1 rest": ((54.8, 288.6), beside(296, 7)), "barge 2": ((344.2, 878.1), beside(952, -28)),
          "barge 2 rest": ((395.2, 878.5), beside(990, 6)), "barge 3": ((757.2, 1151.4), beside(1438, -31)), "barge 3 rest": ((802.2, 1123.2), beside(1474, 8)),
          "delta beacon": ((969.9, 1177.3), beside(1650, 0)), "spawn from Rootmarket": ((945.7, 1174.8), beside(1626, -4)), "Teo's hail": ((5.7, 45.6), beside(46, 0)),
          "patrol's hail": ((106.7, 454.8), beside(470, 0)), "nest A": ((128.5, 606.1), beside(612, -46)), "nest B": ((604.5, 991.9), beside(1236, 45)),
          "first fight": ((16.1, 129), beside(130, 0)), "second fight": ((223.8, 674.6), beside(720, 0)), "third fight": ((534.2, 1007.1), beside(1180, 0))}

# ------------------------------------------------------------------ Rootmarket: the builder's literals and its floor
POOL, POOL_A, POOL_B, RING = (0, 44), 20, 14, 11
LOBES = [((0, 20), 13, 9), ((0, 74), 17, 12.5), ((26, 65), 12, 10.5), ((37, 43), 11, 10.5), ((26, 23), 10.5, 10), ((-46, 13), 8, 7), ((-30, 44), 7.5, 7.5)]
CAUSEWAY = ((46, 43), (61, 43), 3.6)
WALKWAY = [(-24.5, 56.5), (-30, 68), (-25, 85), (-11, 97.5), (0, 101), (11, 97.5), (25, 88), (33.5, 78), (32, 70.5)]
WALK_HALF, RIM_WIDE, BODY = 2.9, 1.1, 0.55  # the ridge's half width, the rims' width, the bake's agent radius
ROPE_WALK = ((-23.4, 28.4), (-39.8, 17))
OPENINGS = [((-19.6, 44), (-22, 44), 1.5), ((19.6, 44), (22, 44), 1.5), ((-24.3, 27.8), (-22.5, 29.05), 1.15), ((-38.9, 17.6), (-40.7, 16.35), 1.15)]
# Where the line that starts Ruva's arrival talk is (its middle), as XylosBuilder.ArrivalLine and the build's logged "crossing=".
ARRIVAL_LINE = (23.5, 24)
PEOPLE = {"Ruva": (28.5, 21), "Ruva at the seed house": (5.5, 68.5), "Teo": (-8.5, 24.6), "Jask": (-25, 47.6), "Binna": (24.6, 68), "Hadda": (35.4, 46.6), "Senn": (-46.4, 13.6)}
FOLK = [(29.8, 71.9), (36.2, 65.2), (-24, 41.6), (-24.2, 39.8), (-24.1, 38), (18, 30.2), (19.6, 31), (-8.2, 75.2), (8.6, 76), (44.6, 46.6), (-33.4, 40),
        (29.6, 16.6), (27.2, 15.2), (24.8, 16.6)]  # RootFolk, then the three on the mooring lines
FIXED = [("press bed", (41, 48.6), -35, 1.4, 0.8), ("drying rack", (42.79, 50.22), -35, 0.75, 0.55), ("mender's cradle", (41.6, 40.4), 30, 1.3, 0.6),
         ("mooring post", (33.7, 17.9), 0, 0.3, 0.3), ("second mooring post", (30.0, 15.4), 0, 0.25, 0.25)]
# What the builder stands about the town, each as the block the bake reads (XylosBuilder.Rootmarket: Timber's lantern posts and stacks, Kitchen's tables).
FIXED += [(f"lantern post at ({x}, {z})", (x, z), 0, 0.15, 0.15) for x, z in [(4.6, 27.2), (-5.2, 14.6), (16.4, 28.6), (-21.4, 50.6), (-6.4, 64.2), (10.6, 66.6), (16.4, 60.8),
          (31.2, 50.2), (30.6, 36.4), (46.6, 45), (-41, 12.6), (27.8, 28.6)]]
FIXED += [(f"tray stack at ({x}, {z})", (x, z), yaw, 0.65, 0.45) for x, z, yaw in [(-9.8, 26.8, 20), (-11.2, 28.2, -30), (-10.6, 22.6, 70), (-6.6, 71.2, 10), (-9.6, 71.8, -20),
          (9.6, 71.4, 30), (11.6, 70.4, 80), (31.5, 26.5, 40), (40.6, 36.2, -10)]]
FIXED += [(f"kitchen table at ({x}, {z})", (x, z), 0, r * 0.8, r * 0.8) for x, z, r in [(27.4, 58, 1.05), (29.4, 65.6, 1.2), (30.8, 58.6, 0.95), (19.4, 69, 0.9)]]
# The kitchen's works at its front, where its sign is read (XylosBuilder.Rootmarket: KitchenWorks and their blocks, D358): the clay oven, the pot on its tripod, the woodpile, the fish rack.
FIXED += [("clay oven", (21, 61.3), 0, 1.1, 1.1), ("cook pot", (27, 63), 0, 0.8, 0.8), ("woodpile", (29.7, 61.6), 0, 1.3, 0.55), ("kitchen drying rack", (18.8, 61), 0, 0.95, 0.25)]
FIXED += [(f"basket at ({x}, {z})", (x, z), 0, size * 0.6, size * 0.6) for x, z, size in [(-7.2, 27.6, 0.7), (8.4, 69.4, 0.8), (-12.6, 68.4, 0.7), (30.6, 25.4, 0.7), (-44.2, 16.8, 0.6)]]


def oval(p, c, a, b):
    qx, qz = p[0] - c[0], p[1] - c[1]
    e = math.sqrt(qx * qx / (a * a) + qz * qz / (b * b))
    if e < 1e-4:
        return -min(a, b)
    return (e - 1) * e / math.sqrt(qx * qx / a ** 4 + qz * qz / b ** 4)


def low_land(p):
    pool = oval(p, POOL, POOL_A, POOL_B)
    d = min(max(-pool, oval(p, POOL, POOL_A + RING, POOL_B + RING)), segment_distance(p, CAUSEWAY[0], CAUSEWAY[1]) - CAUSEWAY[2])
    for c, a, b in LOBES:
        d = min(d, oval(p, c, a, b))
    return max(d, -pool)


def from_walk(p):
    return min(segment_distance(p, WALKWAY[i - 1], WALKWAY[i]) for i in range(1, len(WALKWAY)))


def town_floor(p):
    """Whether a body's middle can stand here: a body's width inside a platform's rim, on the ridge's top, or on one of the two decks."""
    # The builder's own rule for a place to stand (XylosBuilder.OnTheFloor): this far inside a platform's rim, or anywhere in one of the rim's openings.
    if -low_land(p) >= RIM_WIDE + 0.15:
        return True
    if low_land(p) < 0 and any(segment_distance(p, a, b) <= half for a, b, half in OPENINGS):
        return True
    if from_walk(p) <= WALK_HALF - 0.9 - BODY:
        return True
    if segment_distance(p, *ROPE_WALK) <= 1.1 - 0.2:
        return True
    return -20 <= p[0] <= 20 and abs(p[1] - 44) <= 1.65 - BODY


# ------------------------------------------------------------------ the legs, checked as they are written
class Checked(Route):
    """A Route that refuses a straight leg the driver couldn't cross. `flying` and `standing` say what is solid now; a fight's chase leaves the
    party somewhere unknown, so the leg after it is checked from its end only."""

    def __init__(self, *args, **kw):
        super().__init__(*args, **kw)
        self.flying = False
        self.people = set()
        self.problems = []

    def walk(self, name, x, y, z, tolerance=0.8, seconds=None, stop=None, buttons=None):
        if self.scene == "XylosNearspace":
            self.check_flight(name, (x, z))
        elif self.scene == "Rootmarket":
            self.check_town(name, (x, z))
        super().walk(name, x, y, z, tolerance, seconds, stop, buttons)

    def legs(self, to):
        start = self.at
        if start is None:
            return [to]
        steps = max(1, int(math.dist(start, to) / 0.5))
        return [(start[0] + (to[0] - start[0]) * i / steps, start[1] + (to[1] - start[1]) * i / steps) for i in range(steps + 1)]

    def check_flight(self, name, to):
        for p in self.legs(to):
            if from_lane(p) > LANE_HALF - 1 - HULL:
                self.problems.append(f"{self.name}: '{name}' flies {from_lane(p):.1f} m off the lane's middle, into its limit")
                return
            for label, centre, yaw, hx, hz in solids_in_flight():
                if box_distance(p, centre, yaw, hx, hz) < HULL + 0.8:
                    self.problems.append(f"{self.name}: '{name}' flies within a hull of {label} at ({p[0]:.1f}, {p[1]:.1f})")
                    return

    def check_town(self, name, to):
        for p in self.legs(to):
            if not town_floor(p):
                self.problems.append(f"{self.name}: '{name}' walks off the floor at ({p[0]:.1f}, {p[1]:.1f}): rim or water")
                return
            for who in self.people:
                if math.dist(p, PEOPLE[who]) < 1.3:
                    self.problems.append(f"{self.name}: '{name}' walks through {who} at ({p[0]:.1f}, {p[1]:.1f})")
                    return
            for at in FOLK:
                if math.dist(p, at) < 0.5 + BODY + 0.15:
                    self.problems.append(f"{self.name}: '{name}' walks through a townsperson at {at}")
                    return
            for label, centre, yaw, hx, hz in FIXED:
                if box_distance(p, centre, yaw, hx, hz) < BODY:
                    self.problems.append(f"{self.name}: '{name}' walks into {label} at ({p[0]:.1f}, {p[1]:.1f})")
                    return

    def fly(self, name, s, side, tolerance=3, stop=None):
        x, z = beside(s, side)
        self.walk(name, x, 1, z, tolerance=tolerance, stop=stop)

    def hail(self, name, s, lines, flag):
        """A crossing line across the whole lane: fly on past it, and the talk starts as the ship crosses. Read it, and the flag says it was heard."""
        self.fly(f"fly on until {name}", s + 25, 0, stop="ui:dialogue")
        # The talk stops the ship as it crosses the line (a few metres early: the hull has a radius), not at the point flown toward: the next leg starts from here.
        self.at = beside(s - 4, 0)
        self.add(f"{name} on the radio", 3, until="ui:dialogue", expectedUi="dialogue")
        self.read(name, lines, flag)

    def fought(self):
        """After a chase the party is wherever the fight left it."""
        self.at = None

    def write(self):
        if self.problems:
            raise SystemExit("\n".join(self.problems) + "\nXYLOS_ROUTES_FAILED")
        super().write()


def barge_geometry(n):
    """Where the builder puts a barge's two fights (XylosBuilder.Nearspace: LaneSide, Knots, SeedBarge): the barge, the flank its builder calls its lane side
    (whichever of its two flanks leans toward the lane's middle, which for a barge lying across the lane is nearly along it), and its skimmers' middle, 16 m off
    that flank. Worked out from the lane's sides instead, the waypoints for barges two and three lay outside both fights and nothing woke (first player run)."""
    s, side, yaw = BARGES[n][:3]
    at, mid = beside(s, side), beside(s, 0)
    turn = math.radians(yaw)
    right = (math.cos(turn), -math.sin(turn))
    flank = right if right[0] * (mid[0] - at[0]) + right[1] * (mid[1] - at[1]) >= 0 else (-right[0], -right[1])
    return at, flank, (at[0] + flank[0] * 16, at[1] + flank[1] * 16)


def cut_free(r, n, word):
    """A barge's skimmers, then its knots. Into the skimmers' fight from the side the ship comes from, 12 m short of its middle (the fight's volume is 46 by 40 m);
    then beside the flank knot, 6 m off it and outside its lash (5.5 m), where the bow and stern knots are in sight past the hull's ends."""
    at, flank, swarm = barge_geometry(n)
    back = (r.at[0] - swarm[0], r.at[1] - swarm[1])
    length = math.hypot(*back) or 1.0
    r.walk(f"into barge {word}'s skimmers", round(swarm[0] + back[0] / length * 12, 1), 1, round(swarm[1] + back[1] / length * 12, 1), tolerance=3)
    r.fight_flight(f"Barge{n + 1}_Skimmers", rounds=3)
    r.fought()
    r.walk(f"beside barge {word}'s flank knot", round(at[0] + flank[0] * 10, 1), 1, round(at[1] + flank[1] * 10, 1), tolerance=2)
    r.fight_flight(f"Barge{n + 1}_Knots", rounds=3)
    r.wait(f"barge {word} comes free", 2, expectedFlag=f"xylos.barge{n + 1}")
    r.fought()


def nearspace():
    r = Checked(PREFIX + "17-xylos-nearspace", "Chapter 3: Abri and Pell aboard Tallow, the Xylos warp, Teo's hail, three barges cut free, the patrol, the claim drones, the delta beacon", chapter=3)
    # Where chapter-16-lantern-home left the party: aboard Tallow, chapter 2 told and saved as Abri's homecoming closed.
    r.resume("TallowDrift", "chapter2.complete", spawn=(0, -10))
    r.lead("Taren")
    r.walk("through the pressure cabin", 0, 0, 1)
    r.walk("stand beside Keeper Abri", 0, 0, 2, tolerance=0.3)
    # Chapter 2 closes on Abri's own last word (AbriLanternAfter). Her offer waits for it (SideStories.AfterwordHeard), so a party fresh from the homecoming
    # hears that line first and asks again; the route asked once, read the afterword as the offer, and Pell had nothing to teach (first player run, November 13).
    r.talk("Abri's last word on the lanterns", 1, flag="chapter2.afterword")
    r.talk("Abri's dinner problem", 9)  # AbriSeedRoad: starts the Seed Road
    r.walk("to Pell at her lamp", -4, 0, -1.2, tolerance=0.5)  # Pell stands at (-5.5, -1.5) aboard (ChapterOneStaging)
    r.talk("Pell teaches her lantern the lane", 7, flag="xylos.charted")  # PellLaneLantern
    # Back the way the party came: a galley table stands between Pell's lamp and the lift.
    r.walk("back past Abri", 0, 0, 2, tolerance=0.5)
    r.walk("to the pressure lift", 0, 0, -8)
    r.scene = "TallowApproach"
    r.add("launch from Tallow", 3, buttons=["South"], expectedForm="CivilFlight")
    r.add("release launch", 0.5)
    r.at, r.speed, r.brake = (0, -8.5), FLIGHT_SPEED, False
    # The Xylos warp stands south-east of where a ship comes out, at (11, -37), reach 5 (XylosBuilder.StageTallowApproach).
    r.walk("toward the Xylos warp", 7, 1, -27, tolerance=3)
    r.brake = True
    r.walk("settle at the Xylos warp", 9.5, 1, -33.5, tolerance=1)
    r.brake = False
    r.scene = "XylosNearspace"
    r.add("take the Xylos warp", 4, buttons=["South"], expectedForm="Flight")
    r.add("release warp", 0.5)
    r.at, r.flying = (0, 6), True
    r.hail("Teo's hail from the first barge", 46, 8, "xylos.teoHailed")
    r.fly("toward the first swarm", 108, 0)
    r.fly("into the first swarm", 124, 0)
    r.fight_flight("Lane_0", rounds=3)
    r.fought()
    # Barge one lies 25 m to starboard at 252; its skimmers wait on its lane side, its knots on its flank, bow and stern.
    r.fly("toward barge one", 200, 6)
    cut_free(r, 0, "one")
    # Out of its way to port while it drifts in to rest 7 m to starboard at 296.
    r.fly("clear of barge one's drift", 330, -14)
    r.hail("the patrol's hail", 470, 9, "xylos.patrolHeard")
    r.fly("toward the second swarm", 696, 0)
    r.fly("into the second swarm", 714, 0)
    r.fight_flight("Lane_1", rounds=4)
    r.fought()
    # Barge two lies 28 m to port at 952 over the mirror's dark half, among loose panes at the plane: in from the lane, not round them.
    r.fly("over the weather mirror", 900, 4)
    cut_free(r, 1, "two")
    r.fly("back to the lane past the panes", 962, 22)
    r.fly("past barge two's rest", 1040, 14)
    r.fly("toward the third swarm", 1150, 0)
    r.fly("into the third swarm", 1172, 0)
    r.fight_flight("Lane_2", rounds=4)
    r.fought()
    r.hail("Teo's sight of the claim drones", 1360, 6, "xylos.claimSeen")
    # Barge three lies 31 m to port at 1438; its claim drones wait 26 m beyond its far side. The knots are cut from the lane side, which doesn't wake them.
    cut_free(r, 2, "three")
    r.wait("the delta beacon lights", 2, expectedFlag="xylos.bargesFree")
    # It drifts in to lie across the lane's starboard half at 1474 (sides 0 to 16): pass it to port.
    r.fly("ahead of barge three, to port of where it rests", 1500, -14)
    r.fly("toward the delta beacon", 1615, 0)
    r.brake = True
    r.fly("settle at the delta beacon", 1645, 0, tolerance=1)  # the beacon's reach is 6.5 (WorldBuilder.FlightDockRange)
    r.brake = False
    r.scene = "Rootmarket"
    r.add("land at Rootmarket", 6, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release landing", 0.5)
    r.wait("first look at the landing", 2, expectedFlag="xylos.landed")
    r.write()


def rootmarket():
    r = Checked(PREFIX + "18-rootmarket", "Chapter 3: the arrival talk, three pulls on the rope, Ruva's terms, a save at the mender's cradle", chapter=3)
    r.resume("Rootmarket", "xylos.landed", spawn=(0, 17))
    r.people = {"Ruva", "Ruva at the seed house", "Binna", "Hadda", "Senn", "Jask"}
    r.lead("Taren")
    # The talk starts on a line across the way from the pad to the mooring, ARRIVAL_LINE (XylosBuilder.ArrivalLine: x 22.5 to 24.5, 22 m across), about seven metres short of Ruva (D358).
    r.walk("toward the commotion at the mooring", 13, 0, 25.5, stop="ui:dialogue")
    # A lantern post stood on the builder's own way from here to the mooring, at (16.2, 26.6), and the walk bent south of it; the post is now at (16.4, 28.6), off the way, and the bend stays.
    r.walk("past the lantern", 16, 0, 24.4, stop="ui:dialogue")
    r.walk("on toward the mooring", 20, 0, 26.5, stop="ui:dialogue")
    # Through the line: the talk stops the walk as the party steps onto it, at x 22.5.
    r.walk("up to where Ruva calls out", ARRIVAL_LINE[0] - 0.3, 0, 25.1, stop="ui:dialogue")
    r.add("Ruva calls out", 3, until="ui:dialogue", expectedUi="dialogue")
    r.read("the arrival on the rope", 8, "xylos.arrivalSeen")
    r.walk("round behind Ruva", 25.5, 0, 23.8, tolerance=0.6)
    r.walk("past her to the line", 29.5, 0, 23.6, tolerance=0.6)
    r.walk("take up the line", 31, 0, 19.2, tolerance=0.4)  # the rope's reach is 2.8 from (32.4, 18.8); Ruva, at 2.9, is farther
    for pull in range(1, 4):
        r.add(f"pull {pull}", 0.2, buttons=["South"])
        r.add(f"release pull {pull}", 0.3)
        r.wait(f"the island comes in after pull {pull}", 1.9)  # each pull brings it a third of the way in 1.6 s; the next isn't offered until then
    r.wait("she's seated", 4, until="flag:xylos.ropeHauled", expectedFlag="xylos.ropeHauled")
    r.walk("to Ruva", 30.2, 0, 22.6, tolerance=0.4)
    r.talk("Ruva's terms", 10, flag="xylos.termsHeard")
    r.people.add("Teo")  # he comes down to the pool the moment the terms are heard
    # The mooring meets the ring at a waist round (25, 32) where a straight leg grazes the rim: through its wide western part instead.
    # The cradle's hand reaches about 2.85 m from (41.6, 40.4). The path's last point was (39.3, 40.7) at the path's .8 m: the lead stopped 2.82 m off for weeks and
    # 2.87 m off on November 15, when nothing was offered, nothing was saved and chapter 19 loaded a save from before Ruva's terms. So a last short leg, close.
    r.path("to the mender's cradle", [(25, 0, 27), (21.5, 0, 31.5), (23, 0, 35), (25.5, 0, 39), (29, 0, 40), (34, 0, 41.2), (38.6, 0, 40.8)])
    r.walk("up to the cradle's hand", 39.4, 0, 40.8, tolerance=0.3)
    r.save_at("repair and save at the cradle")
    r.write()


def errands():
    r = Checked(PREFIX + "19-rootmarket-errands", "Chapter 3: Hadda's press, Binna's stall and baskets, the seed house, the notice, the upper walk, Jask, Senn, Teo, the river gate", chapter=3)
    r.resume("Rootmarket", "xylos.termsHeard", spawn=(34, 41.2))  # the cradle's save puts the party at the spawn "Press"
    r.people = {"Ruva", "Ruva at the seed house", "Binna", "Hadda", "Senn", "Jask", "Teo"}
    r.lead("Taren")
    r.walk("to Hadda at the press", 33.8, 0, 46.2, tolerance=0.5)
    # Her first meeting ends on three choices; the first is the press.
    r.add("greet Hadda", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release greet Hadda", 0.3, expectedUi="dialogue")
    r.add("hear Hadda out", 15, buttons=["South"], pulseSeconds=0.55, until="ui:options", expectedUi="options")
    r.add("release before her choices", 0.5)
    r.add("use the press", 1.5, buttons=["South"], until="ui:bench", pauseUi="bench", expectedUi="bench")
    r.add("look over the press", 1, pauseUi="bench", expectedUi="bench")
    r.add("leave the press", 0.5, buttons=["East"], pauseUi="bench", expectedUi="world")
    r.add("release the press", 0.4)
    r.path("to Binna's kitchen", [(28.5, 0, 45), (27.5, 0, 50), (23.5, 0, 58), (23.2, 0, 66.6)], tolerance=0.5)
    r.add("greet Binna", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release greet Binna", 0.3, expectedUi="dialogue")
    r.add("hear Binna out", 15, buttons=["South"], pulseSeconds=0.55, until="ui:options", expectedUi="options")
    r.add("release before her choices", 0.5)
    r.add("ask what she's selling", 0.2, buttons=["South"])
    r.add("release the ask", 0.3)
    r.add("her stall opens", 10, buttons=["South"], pulseSeconds=1.0, until="ui:shop", expectedUi="shop")
    r.add("find the repair gel", 3, pickRow="Repair Gel", until="row:Repair Gel", expectedUi="shop")
    r.stock("buy repair gel up to the cap", "Repair Gel")
    r.add("leave her stall", 0.5, buttons=["East"], expectedUi="world")
    r.add("release her stall", 0.3)
    r.talk_to_shop("Binna's three baskets", 6)  # BinnaBasketsOffer: starts Three Baskets and ends at her counter
    # Ruva's later spot at the door, (5.5, 68.5), is a hole in the town's navigation whether or not she stands there yet: round it.
    # Between the kitchen's works and round where a lantern post stood at (8.8, 64.8): both stood on the builder's own way from Binna to the seed house, and both are off it now
    # (the oven at (21, 61.3) and the rack at (18.8, 61) a metre or more south of this walk, the post at (10.6, 66.6)); the walk keeps its old line.
    r.path("to the seed house", [(16.5, 0, 65.2), (17, 0, 62.6), (11, 0, 63.5), (9.5, 0, 62.4), (5.5, 0, 65.8), (3, 0, 68), (2.5, 0, 71.1)], tolerance=0.5)
    r.add("look into the seed house", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release the look", 0.3, expectedUi="dialogue")
    r.read("the seed house", 3, flag="xylos.seedHouseSeen")
    r.walk("to the Compact's notice", -2.7, 0, 71.1, tolerance=0.4)
    r.add("read the notice", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release the notice", 0.3, expectedUi="dialogue")
    r.read("the Compact notice", 6, flag="xylos.noticeRead")
    r.path("up the dry walk", [(-6, 0, 68), (-17, 0, 62), (-24.5, 0, 56.5), (-30, 1, 68), (-25, 2.4, 85), (-11, 2.4, 97.5), (0, 2.4, 101.1)], tolerance=0.6)
    r.add("open the growers' cache", 0.3, buttons=["South"], expectedFlag="cache.xylos.walkway")
    r.add("release the cache", 0.5)
    r.path("back down the dry walk", [(-11, 2.4, 97.5), (-25, 2.4, 85), (-30, 1, 68), (-24.5, 0, 56.5), (-26, 0, 52), (-23.4, 0, 47.6)], tolerance=0.5)
    r.talk("Jask at the ferry stage", 10, flag="met.Jask")  # JaskFirst runs on into JaskAsksMore, which starts Questions
    # Down the ring's inner side east of the ferry queue, which now stands in a line between this walk and the way the builder lists from Teo to Jask (two of it stood on that way).
    r.path("to the rope walk", [(-22.5, 0, 42.5), (-21.5, 0, 36), (-19, 0, 27), (-21.5, 0, 28.6), (-23.4, 0, 28.4), (-39.2, 0, 17.4), (-44.8, 0, 13.6)], tolerance=0.5)
    r.talk("Old Senn", 6, flag="met.Senn")
    r.talk("Senn's mooring post", 5)  # SennPostOffer: starts The Mooring Post
    r.path("to the old mooring box", [(-45.6, 0, 11.6), (-49.6, 0, 11.4)], tolerance=0.4)
    r.add("open the old mooring box", 0.3, buttons=["South"], expectedFlag="cache.xylos.sennIsland")
    r.add("release the box", 0.5)
    # One of Teo's tray stacks stood on the builder's own way from the rope walk to him (it is at (-11.2, 28.2) now, off it); the walk still goes over the raft crossing and round
    # by the landing, to his east side.
    r.path("back across the rope walk", [(-45.6, 0, 11.6), (-39.2, 0, 17.4), (-23.4, 0, 28.4), (-21.5, 0, 28.6), (-19, 0, 27), (-22, 0, 36), (-22, 0, 41), (-21, 0, 44)], tolerance=0.5)
    r.path("over the raft crossing and round to the landing", [(21, 0, 44), (29, 0, 40), (25.5, 0, 39), (23, 0, 35), (21.5, 0, 31.5), (20, 0, 26.5), (16, 0, 24.4), (13, 0, 25.5), (0, 0, 17),
           (-6.7, 0, 23.2)], tolerance=0.5)
    r.talk("Teo by the landing pool", 7, flag="met.Teo")
    r.path("back round the ring's east side", [(0, 0, 17), (13, 0, 25.5), (16, 0, 24.4), (20, 0, 26.5), (21.5, 0, 31.5), (23, 0, 35), (25.5, 0, 39), (29, 0, 40), (33.8, 0, 44.8), (45, 0, 43)])
    r.walk("to the river gate", 56.8, 0, 43, tolerance=0.5)
    r.scene = "BraidedRiver"
    r.add("through the river gate", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the river gate", 0.5)
    r.speed = COMBAT_SPEED
    r.write()


def read_after(r, name, lines, flag, wait=12):
    """A talk that comes by itself (an AfterTalk, a crossing already walked into): wait for it, then read it."""
    r.add(name + " begins", wait, until="ui:dialogue", expectedUi="dialogue")
    r.read(name, lines, flag)


def boss(r, id_, door, x, y, z, spawn, rounds=10, door_tolerance=1.5):
    """A boss from its door (CHAPTER_GUIDE section 10): the door step the driver retries from, the lock, rounds of skills and the fight until it is down."""
    r.lead("Taren")
    r.door(id_, door, x, y, z, spawn=spawn, tolerance=door_tolerance)
    r.press("lock the " + id_, "LeftShoulder", 0.2)
    stop = "flag:bossdown." + id_
    for i in range(rounds):
        r.rotation(f"{id_} {i + 1}", stop)
        r.add(f"fight the {id_} round {i + 1}", 35, buttons=["South"], pulseSeconds=0.46, navigate=True, approachTarget=True, tolerance=2.3, rangedTolerance=12,
              evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=0, z=0))
    r.add(f"the {id_} is down", 4, until=stop, expectedFlag="bossdown." + id_)
    r.recover(id_)


def river():
    """The Braided River (docs/maps/BraidedRiver.md, Route coordinates): the north bank, Sluice A, island one, the cut, island two, Sluice B, Seedfall."""
    r = Checked(PREFIX + "24-braided-river", "Chapter 3: the first arch, Sluice A, Marrow-1 and Garo, Marrow-2 and Dabb, Sluice B, the Seedthane, Teo, the mooring roots", chapter=3)
    r.resume("BraidedRiver", "xylos.termsHeard", speed=COMBAT_SPEED, spawn=(0, 122.6))
    r.lead("Taren")
    r.walk("east along the north bank", 20, 0.6, 122.5)
    r.walk("under the first arch", 45, 0.6, 122.5, stop="ui:dialogue")
    read_after(r, "the first arch", 3, "xylos.archSeen", wait=3)
    r.walk("into the first fight", 70, 0.6, 122)
    r.fight_ground("River_0")
    r.walk("on to the reedstalkers", 126, 0.6, 122)
    r.fight_ground("River_1")
    r.path("to the middle sluice's wheel", [(160, 0.6, 122.5), (170.6, 0.6, 116.2)], tolerance=0.5)
    r.add("set a hand to the middle sluice", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release the wheel", 0.3, expectedUi="dialogue")
    r.read("the middle sluice", 4, flag="xylos.sluiceA")
    r.wait("the middle channel goes down", 4)
    # Across the drained bed to island one's camp, clear of Jask on the ferry stage at (103.2, 94.4).
    r.path("across the drained middle channel", [(160, -0.7, 108), (125, -0.7, 99), (116.8, -0.7, 99.3), (110, -0.7, 96), (100, 0.6, 90)], tolerance=0.8)
    r.walk("to the ferry cradle", 97.5, 0.6, 84.6, tolerance=0.5)
    r.save_at("repair and save at the ferry cradle", "release the cradle")
    r.path("west to Marrow-1", [(90, 0.6, 90), (60, 0.6, 90)])
    r.fight_ground("River_Marrow1")
    r.walk("to Garo at Marrow-1's hatch", 36.8, 0.6, 90.4, tolerance=0.5)
    r.talk("Garo's relief", 5, flag="xylos.marrow1")
    # South of the camp's seed crates.
    r.path("east along island one", [(60, 0.6, 90), (100, 0.6, 90), (120.8, 0.6, 88.6), (140, 0.6, 89)])
    r.fight_ground("River_3")
    r.path("over the drained cut to island two", [(158, 0.6, 92), (175, -0.7, 94), (195, -0.7, 98.5), (206, 0.6, 98.5)], tolerance=0.8)
    r.fight_ground("River_Marrow2")
    r.walk("to Dabb at Marrow-2's hatch", 210.4, 0.6, 99.6, tolerance=0.5)
    r.talk("Dabb's relief", 5, flag="xylos.marrow2")
    r.path("east along island two", [(240, 0.6, 100), (252, 0.6, 99.5)])
    r.fight_ground("River_5")
    r.walk("on along the north shore", 288, 0.6, 99.5)
    r.fight_ground("River_6")
    r.path("to the south sluice's wheel", [(315, 0.6, 101), (321.1, 0.6, 101.3)], tolerance=0.5)
    r.add("set a hand to the south sluice", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release the wheel", 0.3, expectedUi="dialogue")
    r.read("the south sluice", 3, flag="xylos.sluiceB")
    r.wait("the south channel goes down", 4)
    r.walk("over the south channel's head", 336.3, 0.6, 99)
    r.walk("onto Seedfall's fan", 345, 0.6, 97, stop="ui:dialogue")
    read_after(r, "what's on Marrow-3", 4, "xylos.seedfallSeen", wait=4)
    # The Seedthane is already awake as the party walks on: on November 22 it struck the lead 3.6 m short of the door point and threw him 8 m back, and the walk
    # timed out. Within 5 m of the point the fight has begun, and the next steps lock and close on it by themselves.
    boss(r, "Seedthane", "onto Seedfall's fan", 355, 0.6, 92, spawn=(343, 90), rounds=8, door_tolerance=5)
    read_after(r, "the Seedthane goes upriver", 3, "xylos.seedthaneSeenOff")
    # Teo walks up from the root bridge to Marrow-3 once the Seedthane has gone, at 2.4 m/s (37 m): his talk is there when he is.
    r.wait("Teo walks up from the bridge", 12)
    r.walk("to Teo beside Marrow-3", 355, 0.6, 99.4, tolerance=0.5)
    r.talk("Teo at Seedfall", 10, flag="xylos.seedthaneGone")
    r.path("east to the mooring roots", [(375, 0.6, 93), (389, 0.6, 90)], tolerance=0.5)
    r.scene = "WalkingCanopy"
    r.add("through the mooring roots", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the mooring roots", 0.5)
    r.write()


def canopy():
    """The Walking Canopy (docs/maps/WalkingCanopy.md, Route coordinates): the root hall, anchors A, B and C in order, the basin door, the Floodcrown."""
    r = Checked(PREFIX + "25-walking-canopy", "Chapter 3: the root hall, anchor A, the flooding gallery and anchor B, the vault, anchor C, the basin, the Floodcrown", chapter=3)
    r.resume("WalkingCanopy", "xylos.seedthaneGone", speed=COMBAT_SPEED, spawn=(2, 2))
    r.lead("Taren")
    r.walk("into the root hall", 2, -2.8, 10, stop="ui:dialogue")
    read_after(r, "the root hall", 3, "xylos.canopyEntered", wait=3)
    r.walk("among the swarms", 2, -2.8, 12)
    r.fight_ground("Canopy_0")
    r.walk("to anchor root A", 8.6, -2.8, 13, tolerance=0.5)
    r.add("cut anchor root A", 0.3, buttons=["South"], expectedFlag="xylos.anchor1")
    r.add("release the root", 0.5)
    r.wait("the island lurches", 3)
    read_after(r, "the doors have changed", 3, "xylos.anchorHeard")
    r.path("through the gallery door", [(-12, -2.8, 10), (-22, -4.5, 10), (-37, -6.8, 15)], tolerance=0.8)
    r.walk("up the gallery to the swarms", -37.5, -6, 22)
    r.fight_ground("Canopy_1")
    r.walk("on up the gallery", -37.5, -4.8, 36)
    r.fight_ground("Canopy_2")
    r.walk("to anchor root B's platform", -36.5, -3.8, 50)
    r.fight_ground("Canopy_3")
    r.walk("to anchor root B", -36.4, -3.6, 57, tolerance=0.5)
    r.add("cut anchor root B", 0.3, buttons=["South"], expectedFlag="xylos.anchor2")
    r.add("release the root", 0.5)
    r.wait("the flood drains and the vault opens", 4)
    r.path("through the vault to the anchor chamber", [(-30, -3.6, 57.6), (-22.2, 0, 57.5), (-21, 0, 58.8), (-14.4, 0, 59.6), (-14, 0, 60.4), (-14, 0, 61.6), (-12, 0, 68)], tolerance=0.8)
    r.fight_ground("Canopy_4")
    r.walk("to anchor root C", -10.6, 0, 76.4, tolerance=0.5)
    r.add("cut anchor root C", 0.3, buttons=["South"], expectedFlag="xylos.anchor3")
    r.add("release the root", 0.5)
    r.wait("the basin door opens", 3)
    r.path("down past the porch to the hall", [(-11, 0, 70), (-2.7, 0, 62.3), (-2.7, 0, 59.6), (-10, 0, 45), (-9.6, 0, 39), (-11.2, 0, 38.3), (-12, -1.4, 30), (-12, -2.8, 22), (-8, -2.8, 16)], tolerance=0.8)
    r.fight_ground("Canopy_Return")
    r.path("to the root cradle", [(6, -2.8, 18), (12, -2.8, 21.9)], tolerance=0.5)
    r.save_at("repair and save at the root cradle", "release the cradle")
    # The last point stops a metre short of the door: at z 26 arriving (z 25.5) and the basin talk's trigger (the body's front at 25.85) were the same frame,
    # and whichever came first decided the run (a retry on November 21 and again on the 22nd).
    r.path("to the basin door", [(14.3, -2.8, 21.8), (22, -2.8, 21), (22, -3.3, 25)], tolerance=0.5)
    r.walk("into the passage to the basin", 22, -4.2, 30, stop="ui:dialogue")
    read_after(r, "what holds the basin", 4, "xylos.basinSeen", wait=4)
    boss(r, "Floodcrown", "down into the basin", 24, -5.2, 40, spawn=(19.5, 18.5), rounds=10)
    read_after(r, "the Floodcrown leaves with the water", 5, "xylos.floodcrownGone")
    r.at = None
    r.path("back up through the hall", [(24, -5.2, 40), (22, -4.2, 30), (22, -2.8, 21), (16, -2.8, 12), (11.5, -2.8, 1.8)], tolerance=0.5)
    r.scene = "BraidedRiver"
    r.add("out by the mooring roots", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the mooring roots", 0.5)
    r.write()


def vote():
    """Back to Rootmarket by the root bridge, the vote at the seed house, Abri's call on the way to the pad, and home to Tallow by the lane, where chapter 4's route begins."""
    r = Checked(PREFIX + "26-rootmarket-vote", "Chapter 3: the root bridge to town, the vote, Abri's call, and home to Tallow Drift", chapter=3)
    r.resume("BraidedRiver", "xylos.floodcrownGone", speed=COMBAT_SPEED, spawn=(383, 90))
    r.lead("Taren")
    r.path("to the root bridge", [(365, 0.6, 84), (350.4, 0.6, 66.6)], tolerance=0.5)   # 2.1 m from the way at (350, 64.5), which reaches 3.8: at 68.4 it stood a tenth outside
    r.scene = "Rootmarket"
    r.add("over the root bridge to town", 4, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release the root bridge", 0.5)
    r.at, r.speed = (54, 43), 4.5
    r.people = {"Ruva at the seed house", "Binna", "Hadda", "Senn", "Teo"}
    r.path("up past the kitchen to the seed house", [(45, 0, 43), (33.8, 0, 44.8), (28.5, 0, 45), (27.5, 0, 50), (23.5, 0, 58), (23.2, 0, 64.5), (17, 0, 62.6), (11, 0, 63.5), (9.5, 0, 62.4),
           (5.5, 0, 65.8), (5.5, 0, 66.6)], tolerance=0.5)
    r.talk("the vote", 10, flag="xylos.voteHeard")
    r.path("down the ring's east side toward the pad", [(5.5, 0, 65.8), (9.5, 0, 62.4), (11, 0, 63.5), (17, 0, 62.6), (23.2, 0, 64.5), (23.5, 0, 58), (27.5, 0, 50), (28.5, 0, 45), (29, 0, 40),
           (25.5, 0, 39), (23, 0, 35), (21.5, 0, 31.5), (20, 0, 26.5)], tolerance=0.5)
    r.walk("onto the landing", 16, 0, 24.4, stop="ui:dialogue")
    read_after(r, "Abri's dinner call", 11, "chapter3.complete", wait=4)
    r.path("to the pad", [(16, 0, 24.4), (13, 0, 25.5), (0, 0, 17), (0, 0, 21.4)], tolerance=0.5)   # the pad at (0, 24.2) reaches 4
    r.scene = "XylosNearspace"
    r.add("launch from Rootmarket", 4, buttons=["South"], expectedForm="Flight")
    r.add("release launch", 0.5)
    r.at, r.speed, r.brake = (945.7, 1174.8), FLIGHT_SPEED, False
    # Home down the lane, round where each barge now rests across it: barges three and one to port, barge two to starboard, clear of the mirror's loose panes on the port side.
    for s, side in [(1560, -14), (1474, -16), (1300, -10), (1100, 0), (1040, 20), (990, 20), (900, 18), (800, 0), (500, 0), (296, -14), (100, 0), (20, -6)]:
        r.fly(f"home down the lane at {s}", s, side)
    r.brake = True
    r.walk("settle at the warp home", -12.2, 1, -1.4, tolerance=0.8)   # 3.85 m from the warp at (-16, -2): its piece keeps a hull 2.8 m off and its prompt reaches 5; at 4.7 m and a metre's tolerance the ship stood outside it
    r.brake = False
    r.scene = "TallowApproach"
    r.add("warp home to Tallow", 4, buttons=["South"], expectedForm="CivilFlight")
    r.add("release warp", 0.5)
    r.at = (6, -30)
    r.walk("clear of the Xylos warp", 3, 1, -12, tolerance=3)
    r.brake = True
    r.walk("to Tallow's dock", 0, 1, -4.2, tolerance=0.65)
    r.brake = False
    r.scene = "TallowDrift"
    r.add("dock at Tallow Drift", 3, buttons=["South"], expectedForm="Natural")
    r.add("release dock", 0.5)
    r.at, r.speed = (0, -10), 4.5
    r.wait("home aboard Tallow", 2, expectedFlag="chapter3.complete")
    r.write()


def check_lane():
    wrong = [f"{name}: the lane puts it at {made}, the build logged {logged}" for name, (logged, made) in LOGGED.items() if math.dist(logged, made) > 0.25]
    if wrong:
        raise SystemExit("the lane here isn't the builder's:\n" + "\n".join(wrong) + "\nXYLOS_ROUTES_FAILED")


if __name__ == "__main__":
    check_lane()
    nearspace()
    rootmarket()
    errands()
    river()
    canopy()
    vote()
    print("XYLOS_ROUTES_OK")
