"""Writes chapter 4's replay routes (chapter-20 to chapter-23) for the Spore Veil, Driftreed and the Drowned Waystation from the builders' own places.

The Veil's places come from the lane its builder lays (WendmireBuilder.SporeVeil.cs: VeilLane, VeilBeside, Pockets, Field, PodBanks), checked against the
coordinates the build logs (docs/maps/SporeVeil.md) before anything is written. Driftreed's and the Waystation's walks follow the ways their builders
declare and keep clear (WendmireBuilder.Driftreed.cs TownWays, WendmireBuilder.Waystation.cs Ways) to the places in docs/maps/Driftreed.md and
DrownedWaystation.md. Nobody has run these: every straight leg is checked here against what is solid in flight, and
WendmireRouteTests.EveryWalkOnTheChapterFourRoutesFollowsFloorABodyCanCross checks every leg on the built zones' navigation and colliders.

The routes chain from the autosave chapter 3's last route leaves aboard Tallow, with chapter 3 told (chapter3.complete). That route isn't written yet:
it has to end docked at Tallow Drift.
  chapter-20-spore-veil         Abri and Pell aboard Tallow, the Wendmire warp, the beacons, both pockets, the swarm field flown dark, the Veil Ray,
                                the dead beacon relit, the tide-gate
  chapter-21-driftreed          the arrival talk, three holds on the stern line, Ona's terms, Dov, Quill's press, Maru's tide, the mire gate
  chapter-22-driftreed-errands  side content: back by the way home, the school raft, Dov's order and bell, the Compact notice, Quill's fourth pump,
                                and out through the mire gate again
  chapter-23-drowned-waystation the mire and the lure lesson, the portico, pumps one and two, Pim, the vault and pump four, pump three, the door,
                                the Rotbloom, and the walk out with Maru's lamp

  python tools/wendmire_routes.py
"""
import math

from lantern_routes import COMBAT_SPEED, FLIGHT_SPEED, Route
from xylos_routes import box_distance, segment_distance

PREFIX = "chapter-"
HULL = 1.78  # HeroCollision.HullRadius("Taren"), the larger of the two

# ------------------------------------------------------------------ the Spore Veil, as WendmireBuilder.SporeVeil.cs lays it
VEIL = [(0, 0), (-40, 260), (30, 560), (150, 820), (140, 1080), (60, 1300), (40, 1560), (36, 1620)]
VEIL_HALF = 72
BEACON1, BEACON2, BEACON3, DEAD, GATE, FROM_TOWN = 40, 520, 1000, 1380, 1560, 1530
POCKETS = [(300, -56), (1150, 58)]
FIELD = [(600, -22), (665, 24), (730, -6), (800, 28), (870, -26), (935, 12)]


def along(s):
    heading = (0.0, 1.0)
    for i in range(1, len(VEIL)):
        a, b = VEIL[i - 1], VEIL[i]
        leg = math.dist(a, b)
        heading = ((b[0] - a[0]) / leg, (b[1] - a[1]) / leg)
        if s <= leg or i == len(VEIL) - 1:
            return (a[0] + heading[0] * s, a[1] + heading[1] * s), heading
        s -= leg
    return VEIL[0], heading


def beside(s, starboard):
    (x, z), (hx, hz) = along(s)
    return (round(x + hz * starboard, 1), round(z - hx * starboard, 1))


def from_lane(p):
    return min(segment_distance(p, VEIL[i - 1], VEIL[i]) for i in range(1, len(VEIL)))


# What the build logs (docs/maps/SporeVeil.md, Coordinates for routes): the lane here must put each where it is.
LOGGED = {"beacon 1": ((-17.9, 37.7), beside(BEACON1, -12)), "Maru's hail": ((-7.9, 51.4), beside(52, 0)), "beacon 2": ((6.7, 512.9), beside(BEACON2, -12)),
          "beacon 3": ((132.5, 962.0), beside(BEACON3, -12)), "pocket A": ((-86.1, 308.7), beside(300, -56)), "stray call": ((-62.8, 303.2), beside(300, -32)),
          "first swarms": ((4.8, 451.8), beside(460, 0)), "pocket B": ((183.5, 1130.2), beside(1150, 58)), "the ray": ((65.8, 1328.8), beside(DEAD, 8)),
          "dead beacon": ((45.9, 1327.2), beside(DEAD, -12)), "from town": ((50.3, 1478.0), beside(FROM_TOWN, 4)), "tide-gate": ((44.0, 1507.6), beside(GATE, 0)),
          "field 0": ((22.1, 595.4), beside(*FIELD[0])), "field 5": ((159.0, 897.9), beside(*FIELD[5]))}


def veil_solids():
    """At the plane: the two warp pieces (WorldBuilder.Warp, 2 m), and each sleeping swarm's cloud, which a dark ship wakes only by flying into it: kept a
    hull and eight metres off its 12 m box."""
    boxes = [("warp home", (-14, -4), 0, 1, 1), ("tide-gate", beside(GATE, 0), 0, 1, 1)]
    boxes += [(f"sleeping swarm {k}", beside(s, side), 0, 6 + 8, 6 + 8) for k, (s, side) in enumerate(FIELD)]
    return boxes


# What the Waystation's builder stands on a floor the route walks, as the block a body meets (WendmireBuilder.Waystation): the low floor's lamp on the pier between
# its two north alcoves, moved there from the mouth of the western one when the low floor's sentries were stood in those alcoves (D358).
STATION_FIXED = [("the low floor's north lamp", (-13, 249.4), 0, 0.3, 0.3)]
# The low floor's two Drowned Sentries, in its north wall's alcoves facing the room (Waystation.FloorSentries, D358); until November 22 in the west wall's, at (-23.2, 209.5) and (-23.2, 237.5).
FLOOR_SENTRIES = [(-16, 250.6), (-10, 250.6)]
BODY = 0.55  # the bake's agent radius


def station_y(x, z):
    """The floor's height at a plan point of the Drowned Waystation, from its map's levels (docs/maps/DrownedWaystation.md, Layout): the mire at 0, the hall's
    dry walk +0.3, the stair down to the terrace at -2.4, the low floor -4.0 with Pim's pocket -2.0 up its west stair, the vault passage -2.4 and its sump
    -4.2, the great stair up to the landing -0.6, and the great hall -1.6. The station's floors are 2 to 4 m under the mire's, and a waypoint written at
    the mire's height isn't on them."""
    if z < 168:
        return 0
    if z < 192:
        return 0.3
    if z < 206:
        return round(0.3 + (z - 192) / 14 * (-2.7), 2)
    if z < 245.5:
        if x >= 22:
            return -3.4 if 27 <= x <= 35 else -2.4
        if x >= 8:
            return -2.4
        if x <= -24:
            return -2.0
        return -3.2 if x > 2 else -4.0
    if z < 259:
        return -2.4 if x >= 8 else round(-4.0 + (z - 245) / 14 * 3.4, 2)
    if z < 267:
        return -0.6
    return -1.6


class Checked(Route):
    """A Route that refuses a straight leg flown into the Veil's limits, a warp piece or a sleeping swarm. Walks on foot follow the builders' declared
    ways, and the PlayMode check judges them on the built navigation."""

    def __init__(self, *args, **kw):
        super().__init__(*args, **kw)
        self.problems = []

    def walk(self, name, x, y, z, tolerance=0.8, seconds=None, stop=None, buttons=None):
        if self.scene == "DrownedWaystation" and y == 0:
            y = station_y(x, z)
        if self.scene == "DrownedWaystation" and self.at is not None:
            start = self.at
            steps = max(1, int(math.dist(start, (x, z)) / 0.25))
            for i in range(steps + 1):
                p = (start[0] + (x - start[0]) * i / steps, start[1] + (z - start[1]) * i / steps)
                hit = next((label for label, c, yaw, hx, hz in STATION_FIXED if box_distance(p, c, yaw, hx, hz) < BODY), None)
                if hit:
                    self.problems.append(f"{self.name}: '{name}' walks into {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                    break
        if self.scene == "SporeVeil":
            start = self.at
            steps = 1 if start is None else max(1, int(math.dist(start, (x, z)) / 0.5))
            for i in range(steps + 1):
                p = (x, z) if start is None else (start[0] + (x - start[0]) * i / steps, start[1] + (z - start[1]) * i / steps)
                if from_lane(p) > VEIL_HALF - 1 - HULL:
                    self.problems.append(f"{self.name}: '{name}' flies {from_lane(p):.1f} m off the lane's middle, into its limit")
                    break
                hit = next((label for label, c, yaw, hx, hz in veil_solids() if box_distance(p, c, yaw, hx, hz) < HULL + 0.8), None)
                if hit:
                    self.problems.append(f"{self.name}: '{name}' flies within a hull of {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                    break
        super().walk(name, x, y, z, tolerance, seconds, stop, buttons)

    def fly(self, name, s, side, tolerance=3, stop=None):
        x, z = beside(s, side)
        self.walk(name, x, 1, z, tolerance=tolerance, stop=stop)

    def settle(self, name, s, side, tolerance=1):
        self.brake = True
        self.fly(name, s, side, tolerance=tolerance)
        self.brake = False

    def crossing(self, name, lines, flag, stop_at, how="walk"):
        """A talk that starts on a crossing: move on until it starts, then read it."""
        self.add(f"{name} begins", 3, until="ui:dialogue", expectedUi="dialogue")
        self.read(name, lines, flag)

    def fought(self):
        self.at = None

    def write(self):
        if self.problems:
            raise SystemExit("\n".join(self.problems) + "\nWENDMIRE_ROUTES_FAILED")
        super().write()


def tallow(r):
    """Aboard Tallow: Abri's dinner gives way to the air, and Pell's lantern learns the lane. The same floor chapter-17 walks."""
    r.resume("TallowDrift", "chapter3.complete", spawn=(0, -10))
    r.lead("Taren")
    r.walk("through the pressure cabin", 0, 0, 1)
    r.walk("stand beside Keeper Abri", 0, 0, 2, tolerance=0.3)
    # Chapter 3 closes on Abri's own last word (AbriDinnerServed); her offer waits for it (SideStories.ChapterThreeAfterword), as chapter 3's waits for chapter 2's.
    r.talk("Abri's word on the first barge", 1, flag="chapter3.afterword")
    r.talk("Abri's stale air", 9)  # AbriReedRoad: starts the Reed Road
    r.walk("to Pell at her lamp", -4, 0, -1.2, tolerance=0.5)
    r.talk("Pell's lantern learns the Veil", 6, flag="wend.charted")  # PellVeilLantern
    r.walk("back past Abri", 0, 0, 2, tolerance=0.5)
    r.walk("to the pressure lift", 0, 0, -8)
    r.scene = "TallowApproach"
    r.add("launch from Tallow", 3, buttons=["South"], expectedForm="CivilFlight")
    r.add("release launch", 0.5)
    r.at, r.speed, r.brake = (0, -8.5), FLIGHT_SPEED, False
    # The Wendmire warp stands south-west of where a ship comes out, at (-11, -37), reach 5 (WendmireBuilder.StageTallowApproach).
    r.walk("toward the Wendmire warp", -6, 1, -27, tolerance=3)
    r.brake = True
    r.walk("settle at the Wendmire warp", -8.85, 1, -34.0, tolerance=1)   # 3.7 m from the warp at (-11, -37), which reaches 5
    r.brake = False


def veil():
    r = Checked(PREFIX + "20-spore-veil", "Chapter 4: Abri and Pell aboard Tallow, the Wendmire warp, the beacons, both pockets, the swarm field flown dark, the Veil Ray, the dead beacon, the tide-gate", chapter=4)
    tallow(r)
    r.scene = "SporeVeil"
    r.add("take the Wendmire warp", 4, buttons=["South"], expectedForm="Flight")
    r.add("release warp", 0.5)
    r.at = (0, 6)
    # The first beacon's line (40) sets its flag as the ship crosses it; Maru's hail is the line just inside it (52).
    r.fly("past the first beacon", 46, 0)
    r.wait("the first beacon passed", 0.5, expectedFlag="wend.beacon1")
    r.fly("fly on until Maru hails", 78, 0, stop="ui:dialogue")
    r.crossing("Maru's hail on the Veil", 9, "wend.maruHailed", 52)
    # Leg one: three banks of pods across the lane, each with its gap where the builder leaves no pod within 8 m of side (bank - 1) x 10.
    r.fly("toward the first pods", 130, -10)
    r.fly("through the first pods' gap", 172, -10)
    r.fly("toward the second pods", 240, 0)
    r.fly("through the second pods' gap", 276, 0)
    # Pocket A, 56 m to port at 300: Maru's call at its mouth starts Two Short, One Long; the slipped beacon is retuned and its cache taken.
    r.fly("into pocket A's mouth", 300, -38, stop="ui:dialogue")
    r.crossing("Maru calls a stray ship", 5, "wend.strayed", 300)  # MaruVeilStray runs on into MaruBeaconsAsk
    r.settle("to the slipped beacon", 298, -51)
    r.press_for("retune the first slipped beacon", "wend.offBeacon1")
    r.settle("to the pilots' cache", 306, -44)
    r.press_for("open the pilots' cache", "cache.wend.pocketA")
    r.fly("back to the lane", 360, 6)
    r.fly("through the third pods' gap", 418, 10)
    r.fly("into the first swarms", 456, 0)
    r.fight_flight("Lane_0", rounds=3)
    r.fought()
    # Shots and boost light the ship; dark for two seconds sheds what rides it, and a dark ship wakes a sleeping swarm only by flying into it.
    r.wait("go dark and count two", 3)
    r.fly("past the second beacon", 526, 0)
    r.wait("the second beacon passed", 0.5, expectedFlag="wend.beacon2")
    for k, (s, side) in enumerate([(600, 22), (665, -18), (730, 26), (800, -12), (870, 16), (935, -22)]):
        r.fly(f"dark between the sleeping swarms {k + 1}/6", s, side)
    r.fly("out of the swarm field", 975, 0)
    r.fly("past the third beacon", 1006, 0)
    r.wait("the third beacon passed", 0.5, expectedFlag="wend.beacon3")
    # Pocket B, 58 m to starboard at 1150, with two swarms of its own in front of the slipped beacon.
    r.fly("toward pocket B", 1120, 30)
    r.fight_flight("Pocket_B", rounds=3)
    r.fought()
    r.wait("go dark again", 3)
    r.settle("to the second slipped beacon", 1148, 53)   # the lamp is at (1150, 58) and reaches 7: from (1146, 53) and a metre's tolerance it could stand outside
    r.press_for("retune the second slipped beacon", "wend.offBeacon2")
    r.settle("to the second pilots' cache", 1156, 46)   # the cache is at (1156, 50) and reaches 6.5: (1160, 44) is 7.2 m from it, and only a press made on the way past ever opened it
    r.press_for("open the second pilots' cache", "cache.wend.pocketB")
    r.fly("back to the lane", 1240, 0)
    # The Veil Ray circles 20 m to starboard of the dead beacon; its circuit is the whole fight.
    r.fly("toward the dead beacon", 1320, -8)
    r.fight_flight("Veil_Ray", rounds=5)
    r.fought()
    r.settle("to the dead beacon", 1380, -6.8)
    r.add("reach for the dark lamp", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release the lamp", 0.3, expectedUi="dialogue")
    r.read("the dead beacon", 3, "wend.beaconRelit")  # MaruDeadBeacon
    r.fly("on to the tide-gate", 1500, 0)
    r.settle("over the tide-gate", 1555, 0)  # the gate's reach is 6.5 (WorldBuilder.FlightDockRange); its piece is solid
    r.scene = "Driftreed"
    r.add("come down through the tide-gate", 6, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release the gate", 0.5)
    r.wait("first look at Driftreed", 2, expectedFlag="wend.arrived")
    r.write()


# Driftreed's walked ways, by name (WendmireBuilder.Driftreed.cs TownWays).
STAGE_TO_HALL = [(0, 0, 18), (0, 0, 34), (0, 0, 46), (0, 0, 58), (0, 0, 75), (1, 0, 78.5)]
# The gate walk is entered along its own line: the builder's way from (40, 63) to its south end at (45.3, 65.2) clips the walk's rail.
HALL_TO_GATE = [(12, 0, 80.5), (20, 0, 80), (37.45, 0, 66.6), (40, 0, 63), (45.3, 0, 62.2), (45.3, 0, 65.2), (45.3, 0, 88.6), (45.5, 0, 92.2), (46.6, 0, 96.9)]   # the last is 3 m from the mire gate at (48.2, 99.4), which reaches 4.4: at (46.2, 96.2) it stood two centimetres outside being sure


def driftreed():
    r = Checked(PREFIX + "21-driftreed", "Chapter 4: the arrival talk, the stern line, Ona's terms, Dov, Quill's press, Maru's tide, the mire gate", chapter=4)
    r.resume("Driftreed", "wend.arrived", spawn=(0, 8.5))
    r.lead("Taren")
    # Maru's call is a crossing across the stage between the pad and the line (at x 6, 13 m deep).
    r.walk("toward Maru and the line", 7, 0, 10.5, stop="ui:dialogue")
    r.crossing("Maru's arrival on the stage", 9, "wend.arrivalSeen", 6)
    r.walk("on to the line", 7, 0, 10.5)
    r.walk("take the stern line", 12.2, 0, 10.6, tolerance=0.4)
    for hold in range(1, 4):
        r.add(f"hold {hold}", 0.2, buttons=["South"])
        r.add(f"release hold {hold}", 0.3)
        r.wait(f"the raft eases in after hold {hold}", 1.9)
    r.wait("the line is held", 4, until="flag:wend.lineHeld", expectedFlag="wend.lineHeld")
    r.path("to the weaving hall", [(7, 0, 10.5), (0, 0, 8.5)] + STAGE_TO_HALL + [(0.5, 0, 80.1)], tolerance=0.5)
    r.talk("Ona's terms", 11, flag="wend.onaHeard")
    r.path("down to the market", [(1, 0, 78.5), (0, 0, 75), (0, 0, 58), (0, 0, 46), (10, 0, 45.5), (18, 0, 45.2)], tolerance=0.5)
    r.add("greet Dov", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release greet Dov", 0.3, expectedUi="dialogue")
    r.add("hear Dov out", 18, buttons=["South"], pulseSeconds=0.55, until="ui:options", expectedUi="options")
    r.add("release before his choices", 0.5)
    r.add("see his smokehouse stock", 10, buttons=["South"], pulseSeconds=1.0, until="ui:shop", expectedUi="shop")   # he says a line before the counter opens: one held press chose the answer and stood on that line
    r.add("look over Dov's counter", 1, expectedUi="shop")
    r.add("leave Dov's counter", 0.5, buttons=["East"], expectedUi="world")
    r.add("release Dov's counter", 0.3)
    r.path("over the pump walk to Quill", [(10, 0, 45.5), (0, 0, 46), (-16, 0, 46), (-30.2, 0, 46), (-32.9, 0, 46)], tolerance=0.5)
    r.add("greet Quill", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release greet Quill", 0.3, expectedUi="dialogue")
    r.add("hear Quill out", 15, buttons=["South"], pulseSeconds=0.55, until="ui:options", expectedUi="options")
    r.add("release before his choices", 0.5)
    # His third answer, his parts: the Rotbloom is sized on Prime parts in every slot (DevLoadout chapter4), and chapter 3's drops leave one Prime Module for the
    # two (the chain's saves replayed with wearBest). About 3,300 scrip in hand here, after chapter 2's Refined set; no gels are bought before Many Hands.
    r.add("find his parts among his answers", 3, pickRow="See the parts", until="row:See the parts", expectedUi="options")
    r.add("see his parts", 10, buttons=["South"], pulseSeconds=1.0, until="ui:shop", expectedUi="shop")  # he says a line before the counter opens
    r.stock_part("Prime Module", 2)
    r.add("leave Quill's counter", 0.5, buttons=["East"], expectedUi="world")
    r.add("release Quill's counter", 0.3)
    r.path("back to Maru on the stage", [(-30.2, 0, 46), (-16, 0, 46), (0, 0, 46), (0, 0, 34), (0, 0, 18), (0, 0, 13), (8, 0, 13.4)], tolerance=0.5)
    r.talk("Maru turns the tide", 10, flag="wend.tideTurned")
    r.wait("the rope walks swing", 4)
    r.path("to the mire gate", [(0, 0, 13)] + STAGE_TO_HALL + HALL_TO_GATE, tolerance=0.5)
    r.scene = "DrownedWaystation"
    r.add("through the mire gate", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the mire gate", 0.5)
    r.write()


def errands():
    r = Checked(PREFIX + "22-driftreed-errands", "Chapter 4: back by the way home, the school raft, Dov's order and bell, the notice, Quill's fourth pump, the mire gate again", chapter=4)
    r.resume("DrownedWaystation", "wend.tideTurned", speed=COMBAT_SPEED, spawn=(-4, 2.5))
    r.lead("Taren")
    r.walk("to the way home", -7.2, 0, 0.4, tolerance=0.5)  # Mire gate — Driftreed at (-9.2, -1.6), reach 3.6
    r.scene = "Driftreed"
    r.add("home through the mire gate", 4, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release the way home", 0.5)
    r.at = (45.5, 92.2)
    # Off the gate walk along its line, and round the children clapping the count in the middle of the raft to watch from its west side.
    r.path("over the gate walk to the school raft", [(45.3, 0, 88.6), (45.3, 0, 65.2), (45.3, 0, 62.2), (36, 0, 61), (35.9, 0, 57.0)], tolerance=0.5)
    r.add("watch the school raft", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release the watch", 0.3, expectedUi="dialogue")
    r.read("the school raft", 3, flag="wend.schoolSeen")
    # The school walk to the market left with the tide: round by the hall walk.
    r.path("round by the hall to the market", [(36, 0, 61), (40, 0, 63), (37.45, 0, 66.6), (20, 0, 80), (12, 0, 80.5), (1, 0, 78.5), (0, 0, 75), (0, 0, 58), (0, 0, 46), (10, 0, 45.5), (18, 0, 45.2)], tolerance=0.5)
    r.talk_to_shop("Dov's order", 4)  # DovOrderOffer: starts The Smokehouse Order
    r.talk_to_shop("Dov's bell", 5)  # DovBellOffer: starts The School Bell
    r.path("to the Compact notice", [(10, 0, 45.5), (0, 0, 46), (-1, 0, 42), (-4.8, 0, 40.4)], tolerance=0.4)
    r.add("read the notice", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release the notice", 0.3, expectedUi="dialogue")
    r.read("the Compact notice", 2, flag="wend.noticeRead")
    r.path("over the pump walk to Quill", [(-1, 0, 42), (0, 0, 46), (-16, 0, 46), (-30.2, 0, 46), (-32.9, 0, 46)], tolerance=0.5)
    r.add("greet Quill", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release greet Quill", 0.3, expectedUi="dialogue")
    r.add("hear Quill's fourth pump", 12, buttons=["South"], pulseSeconds=0.55, until="ui:options", expectedUi="options")  # QuillPumpOffer: starts The Fourth Pump
    r.add("release before his choices", 0.5)
    r.add("use the pump-and-press", 10, buttons=["South"], pulseSeconds=1.0, until="ui:bench", pauseUi="bench", expectedUi="bench")
    r.add("leave the press", 0.5, buttons=["East"], pauseUi="bench", expectedUi="world")
    r.add("release the press", 0.4)
    r.path("back to the mire gate", [(-30.2, 0, 46), (-16, 0, 46), (0, 0, 46)] + STAGE_TO_HALL[3:] + HALL_TO_GATE, tolerance=0.5)
    r.scene = "DrownedWaystation"
    r.add("back through the mire gate", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the mire gate", 0.5)
    r.write()


def waystation():
    r = Checked(PREFIX + "23-drowned-waystation", "Chapter 4: the mire, the lure lesson, the portico, pumps one and two, Pim, the vault and pump four, pump three, the door, the Rotbloom", chapter=4)
    r.resume("DrownedWaystation", "wend.schoolSeen", speed=COMBAT_SPEED, spawn=(-4, 2.5))
    r.lead("Taren")
    # The mire's walked line (WendmireBuilder.Waystation.cs Ways[0]), fight by fight.
    r.walk("toward Maru's lamp", 2, 0, 16, stop="ui:dialogue")
    r.walk("onto the first island", 4, 0, 30, stop="ui:dialogue")
    r.crossing("the mire gate's island", 4, "wend.mireEntered", 17.6)  # MaruMireGate
    r.walk("into the striders' ground", 4, 0, 30)
    r.fight_ground("Mire_Strider")
    r.path("over the ford", [(14, 0, 38), (20, 0, 52)])
    r.walk("toward the lure posts", 27, 0, 62, stop="ui:dialogue")
    r.crossing("the lure lesson", 9, "wend.lureLearned", 60)  # MaruLureLesson
    r.walk("among the lure posts", 28, 0, 72)
    r.fight_ground("Mire_Lures")
    r.path("to the leech channel", [(22, 0, 82), (10, 0, 92)])
    r.fight_ground("Mire_Leeches")
    r.path("to the third island", [(-3, 0, 101), (-4, 0, 118)])
    r.fight_ground("Mire_Third")
    r.path("over the causeway", [(1, 0, 136), (0, 0, 154)])
    r.walk("to the portico", 0, 0, 166, stop="ui:dialogue")
    r.crossing("the portico", 5, "wend.porticoReached", 160.4)  # MaruPortico
    # Through the portico's opening before turning east: the builder's way from (0, 168) to (4, 174) runs through its east leg.
    r.walk("under the portico", 0, 0, 170, stop="ui:dialogue")
    r.walk("into the entry hall", 4, 0, 174, stop="ui:dialogue")
    r.crossing("the entry hall", 4, "wend.hallSeen", 173.6)  # SelaWaystationHall
    r.path("along the dry walk to pump one", [(4, 0, 174), (11, 0, 180), (12.1, 0, 182.9)], tolerance=0.5)
    r.add("set a hand to pump one", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release pump one", 0.3, expectedUi="dialogue")
    r.read("pump one", 4, flag="wend.pump1")  # SelaPumpOne; the water goes down over four seconds
    r.wait("the hall drains", 5)
    r.fight_ground("Hall_Sentries")
    r.path("down the stair to the terrace", [(12, 0, 190), (12, 0, 206), (14, 0, 226)])
    r.fight_ground("Gallery_Terrace")
    r.path("to pump two", [(16, 0, 244), (17, 0, 246.2)], tolerance=0.5)
    r.press_for("run pump two", "wend.pump2")
    r.wait("the low floor drains", 5)
    # Round the north sump's edge, which the straight line from the ramp foot grazes.
    r.path("down onto the low floor", [(16, 0, 244), (6, 0, 245), (-2, 0, 245), (-10.1, 0, 233.6), (-12, 0, 230)])
    # Its sentries stand in the north wall's alcoves: across the floor to face them from the south, 7.6 m short of each and south of the lamp on the pier between them,
    # so the chase to either goes up one side of the lamp (D358).
    north = (round((FLOOR_SENTRIES[0][0] + FLOOR_SENTRIES[1][0]) / 2, 1), 243.5)
    r.walk("across the low floor to the sentries' alcoves", north[0], 0, north[1], tolerance=0.6)
    r.fight_ground("Gallery_Floor")
    r.path("to Pim's grille", [(-12, 0, 230), (-22, 0, 221.5), (-26.5, 0, 221.5)], tolerance=0.5)
    r.talk("Pim behind the grille", 10, flag="wend.pimFound")  # PimFound
    # The vault and pump four: The Fourth Pump, started at Quill's in chapter-22.
    r.path("back to the terrace's vault passage", [(-22, 0, 221.5), (-12, 0, 230), (-10.1, 0, 233.6), (-2, 0, 245), (6, 0, 245), (16, 0, 244), (14, 0, 226), (14, 0, 217.5), (25.3, 0, 217.9)], tolerance=0.5)
    r.press_for("run the fourth pump", "wend.pump4")
    r.wait("the vault's sump drains", 5)
    r.path("through the vault's sump", [(28, 0, 217.5), (33.3, 0, 218), (42, 0, 218), (45.8, 0, 217.6)], tolerance=0.5)
    r.add("take a rubbing of the marks", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release the rubbing", 0.3, expectedUi="dialogue")
    r.read("the wall of marks", 4, flag="wend.vaultRead")  # SelaVaultMark
    r.walk("to the vault's coffer", 45.8, 0, 213.4, tolerance=0.4)
    r.press_for("open the vault's coffer", "cache.wend.station.vault")
    r.path("back to the great stair", [(46, 0, 214), (42, 0, 218), (33.3, 0, 218), (28, 0, 217.5), (14, 0, 217.5), (14, 0, 226), (16, 0, 244), (6, 0, 245), (-2, 0, 245), (0, 0, 252), (0, 0, 262)], tolerance=0.5)
    r.walk("to the station cradle", 5.6, 0, 261.8, tolerance=0.4)
    r.save_at("repair and save at the station cradle", "release the cradle")
    r.path("to pump three", [(0, 0, 262), (-9, 0, 262.5)], tolerance=0.4)
    r.press_for("run pump three", "wend.pump3")
    r.wait("the grille lifts", 2)
    r.walk("back to the door", 0, 0, 262, tolerance=0.5)
    r.walk("up to the great hall's door", 0, 0, 266.5, stop="ui:dialogue")
    r.crossing("the great hall's door", 5, "wend.bloomSeen", 265.6)  # SelaBloomDoor; saves at the spawn Door
    r.lead("Taren")
    # The door: walking onto the floor starts the fight and writes its checkpoint (spawn Door, -2.5, 261.5). Lost, the driver retries from here.
    # Past the east side of the old engineers' lamp at the door, (0, 270.4): a straight walk from the landing and from the checkpoint spawn. Ending at x 2.6 the walk from the
    # landing passed 1.0 m from the lamp's post and the one from the spawn 0.4 m, inside the floor the post carves (WendmireRouteTests found the first); at x 4 they pass 1.5 and 1.25.
    r.door("Rotbloom", "onto the great hall's floor", 4, -1.6, 277, spawn=(-2.5, 261.5), tolerance=1.5)   # 7 m into its volume (z from 270): at 272 the lead was in, Sela was in the doorway, and the fight waits for both
    r.press("lock the Rotbloom", "LeftShoulder", 0.2)
    stop = "flag:bossdown.Rotbloom"
    for i in range(10):
        r.rotation(f"Rotbloom {i + 1}", stop)
        r.add(f"fight the Rotbloom round {i + 1}", 35, buttons=["South"], pulseSeconds=0.46, navigate=True, approachTarget=True, tolerance=2.3, rangedTolerance=12,
              evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=0, z=0))
    r.add("the Rotbloom dies", 4, until=stop, expectedFlag="bossdown.Rotbloom")
    r.recover("the Rotbloom")
    r.add("the hall after the Rotbloom", 12, until="ui:dialogue", expectedUi="dialogue")
    r.read("the hall after the Rotbloom", 4, flag="wend.rotbloomDead")  # SelaBloomDead (AfterTalk)
    r.at = None
    r.path("out of the great hall", [(0, 0, 262), (0, 0, 252), (-2, 0, 245), (6, 0, 245), (16, 0, 244), (14, 0, 226), (12, 0, 206), (12, 0, 190), (11, 0, 180), (4, 0, 174), (0, 0, 170), (0, 0, 168)])
    r.walk("out to the portico", 0, 0, 163, stop="ui:dialogue")
    r.crossing("Maru's lamp at the portico", 5, "wend.lampCarried", 165.2)  # MaruAfterBloom
    r.write()


def check_lane():
    wrong = [f"{name}: the lane puts it at {made}, the build logged {logged}" for name, (logged, made) in LOGGED.items() if math.dist(logged, made) > 0.25]
    if wrong:
        raise SystemExit("the Veil here isn't the builder's:\n" + "\n".join(wrong) + "\nWENDMIRE_ROUTES_FAILED")


if __name__ == "__main__":
    check_lane()
    veil()
    driftreed()
    errands()
    waystation()
    print("WENDMIRE_ROUTES_OK")
