"""Writes chapter 10's replay routes (chapter-62 on), A Route With Room To Turn Back, from the builders' own places and the built scenes' prompts.

Places are read out of the builders (LastMooringBuilder.Approach.cs: the stern's warps to Sorrel and Last Mooring and the spawns ships come home to; LastMooringBuilder.Sorrel.cs:
the valley, the apertures, the crater and the lane east as its own Air() lays them, the four shelves worked out as its Shelf does, the cover; LastMooringBuilder.Mooring.cs: the chain
lines and their gaps, the pylons, the islands, the arm) and from docs/quality/encounters.json (every fight's box, RouteFacts.Encounters), and every prompt a route presses is read out
of the built scene it stands in (home_routes.scene_prompts: where it is, its reach, the flag it sets, the zone a way goes to); a builder's number that isn't what the scene has stops
the run. A stand is checked against the prompt as built: in reach with its walk's tolerance and RouteFightTests' fifteen centimetres to spare. Every straight leg is checked before
it is written: Many Hands and its hull by chapter 9's checks (Idra's hull); Sorrel inside its air by Idra's hull and a metre, off the cover and Mother of Doors' ring; Last Mooring
off every chain, pylon and gap's edge; on a shelf or an island, inside its deck by a body and off its works. MooringRouteTests then checks every leg, press, dock and launch on the
built zones.

The driver's hands for chapter 10 (ContinuousReview.Mooring.cs): a step that ends docked at a berth, or launched from it, goes to that berth's pad or launch where it stands now (the
fourth shelf at Sorrel swings on her strain: a person flies at the pad they see); and the markings of a boss nobody strikes (Mother of Doors, the Tollkeeper, in their rooms) are
rolled out of as they land, which the older rule never read because nobody can lock either. The berth's own reading is ContinuousReview.Berth.cs ("until":"docked:<id>" and
"launched:<id>"); a clamp on a shelf is worked by chapter 7's clamp hand (ContinuousReview.Oriel.cs: the Clamp Tender stopped first, then the hold with the pad let go).

The chain. Chapter 9's last route (chapter-61-the-broken-ring) ends at Many Hands' chart table on chapter9.complete, saved at the dock's arrival; its closing line sends the party to
Idra's plan at the same table.
  chapter-62-one-plan          Idra's plan at the chart table, Orrin's roll (TurnBack begins), the hull pad, the stern to the warp to Sorrel's second sky, the valley and its two
                               interceptor pairs (saved at Sorrel's arrival, the lanes cleared)
  chapter-63-mother-of-doors   up the valley and through the east aperture, Dace over his ridge, Mother of Doors (her door retried): four times the clamp's guns flown down, the
                               dock, the Clamp Tender stopped and the clamp opened on foot, the launch (the fourth shelf swings); Dace as she goes (end.motherFreed, saved at the
                               fourth shelf's own spawn)
  chapter-64-last-mooring      the lane east to the warp on, Last Mooring: chain A, the first pair, chain B, the section struck and Maru's weavers, the west island's cradle
                               (saved there), the guns at chain C's gap, the second pair (saved at the island's spawn, every fight before the Tollkeeper's door cleared)

  python tools/mooring_routes.py          all of them
  python tools/mooring_routes.py 62 63    a subset, by number (a zone still being finished can be left out)
"""
import math
import re
import sys

import harrowgate_routes as hr
import home_routes as hm
import manyhands_routes as mh
from lantern_routes import COMBAT_SPEED, FLIGHT_SPEED, encounters
from manyhands_routes import BODY, NUMBER, block
from xylos_routes import box_distance, segment_distance

PREFIX = "chapter-"
WALK = hm.WALK
INSIDE = hm.INSIDE
HULL = hm.HULL  # Idra's, the larger of the pair chapter 10 is played with
FAILED = "MOORING_ROUTES_FAILED"


def fail(text):
    raise SystemExit(text + "\n" + FAILED)


def source(name):
    return (hm.WORLD / name).read_text(encoding="utf-8")


APPROACH = source("LastMooringBuilder.Approach.cs")
SORREL_SOURCE = source("LastMooringBuilder.Sorrel.cs")
MOORING_SOURCE = source("LastMooringBuilder.Mooring.cs")


def const(text, name):
    m = re.search(r"\b" + name + "=" + NUMBER, text)
    if not m:
        fail(f"no {name} in its builder")
    return float(m.group(1))


def vec3(text, name, names):
    """`Name=new(x,y,z)` of a builder, each part a number or one of its own constants (Plane, ShelfY): the plan point and the height."""
    m = re.search(r"\b" + name + r"=new\(([^()]*)\)", text)
    if not m:
        fail(f"no {name}=new(x,y,z) in its builder")
    parts = [p.strip() for p in m.group(1).split(",")]

    def value(p):
        p = re.sub(r"(\d)f\b", r"\1", p)
        return float(eval(p, {"__builtins__": {}}, names))
    x, y, z = (value(p) for p in parts)
    return (round(x, 3), round(z, 3)), y


# ------------------------------------------------------------------ chapter 10's places in older scenes (LastMooringBuilder.Approach)
_ap = hm.vec3s(APPROACH)
SORREL_WARP, MOORING_WARP, HULL_FROM_SORREL, HULL_FROM_MOORING = (_ap[k] for k in ("SorrelWarp", "MooringWarp", "HullFromSorrel", "HullFromMooring"))
SURVEY_AT = hm.vec2s(APPROACH)["SurveyAt"]
# The two warps stand off the stern on chapters 6 to 9's row: what a hull meets there (the Last Mooring one only from end.motherFreed: a solid it may meet is counted always).
hr.STERN_WARPS += [("the warp to Sorrel", SORREL_WARP), ("the warp to Last Mooring", MOORING_WARP)]
H = mh.H
MANY_HANDS_PEOPLE = {"Corra", "Dov", "Hadda", "Binna", "Ruva", "Teo", "Garo", "Dabb", "Maru", "Ona", "Pim", "Pell"}

# ------------------------------------------------------------------ Sorrel's second sky, as LastMooringBuilder.Sorrel lays it
_SORREL = block(SORREL_SOURCE, "internal static class Sorrel")
SK = {k: const(_SORREL, k) for k in ("ValleyHalf", "Half", "CanyonSouth", "FunnelFrom", "FunnelTo", "RimLine", "ApertureInner", "CraterR", "ChainZ", "SeatR", "ShelfR", "AnchorR",
                                     "CoilIn", "CoilOut", "EastZ", "EastHalf", "EastEnd", "RoadHalf", "RoadSouth", "RoadTurn1", "RoadNorth", "RoadTurn2", "RoadEnd", "CoverRadius", "ShelfY")}
SK["Plane"] = 1.0
ROAD_LEGS = [float(v) for v in re.findall(NUMBER, block(_SORREL, "RoadLegs="))]
SORREL_CENTRE = hm.vec2s(_SORREL)["Centre"]
EAST_FROM = float(re.search(r"Take\(Box\(p," + NUMBER + r",EastEnd", _SORREL).group(1))
S_ARRIVAL, S_CHECKPOINT, S_HEAD, S_CROSSING = (vec3(_SORREL, k, SK)[0] for k in ("Arrival", "Checkpoint", "ValleyHead", "Crossing"))
SORREL_COVER = [(float(a), float(b)) for a, b in re.findall(r"new\(" + NUMBER + r",Plane," + NUMBER + r"\)", block(_SORREL, "Vector3[] Cover="))]
SORREL_WARP_ON = (SK["EastEnd"] - 10, SK["EastZ"])
_wo = re.search(r"WarpOn=new\(EastEnd" + NUMBER + r",Plane,EastZ\)", _SORREL)
if not _wo or float(_wo.group(1)) != -10:
    fail("LastMooringBuilder.Sorrel: WarpOn is no longer new(EastEnd-10,Plane,EastZ)")


def clamp01(t):
    return max(0.0, min(1.0, t))


def half_at(z):
    return SK["ValleyHalf"] + (SK["Half"] - SK["ValleyHalf"]) * clamp01((z - SK["FunnelFrom"]) / (SK["FunnelTo"] - SK["FunnelFrom"]))


def box_sd(p, x0, x1, z0, z1):
    dx, dz = max(x0 - p[0], p[0] - x1), max(z0 - p[1], p[1] - z1)
    if dx <= 0 and dz <= 0:
        return min(-dx, -dz)
    return -math.hypot(max(dx, 0), max(dz, 0))


ROAD_PARTS = [(-SK["RoadHalf"], SK["RoadHalf"], SK["CraterR"] + SORREL_CENTRE[1] - 8, SK["RoadNorth"]), (-SK["RoadHalf"], ROAD_LEGS[1] + SK["RoadHalf"], SK["RoadTurn1"], SK["RoadNorth"]),
              (ROAD_LEGS[1] - SK["RoadHalf"], ROAD_LEGS[1] + SK["RoadHalf"], SK["RoadSouth"], SK["RoadNorth"]), (ROAD_LEGS[1] - SK["RoadHalf"], ROAD_LEGS[2] + SK["RoadHalf"], SK["RoadSouth"], SK["RoadTurn2"]),
              (ROAD_LEGS[2] - SK["RoadHalf"], ROAD_LEGS[2] + SK["RoadHalf"], SK["RoadSouth"], SK["RoadEnd"])]


def sorrel_air(p):
    """Sorrel.Air: the signed distance on the plan into the air a ship flies in (positive inside), the largest of its parts."""
    c = SK
    best = -math.inf
    h = half_at(p[1])
    slant = math.cos(math.atan2(c["ValleyHalf"] - c["Half"], c["FunnelTo"] - c["FunnelFrom"])) if c["FunnelFrom"] < p[1] < c["FunnelTo"] else 1
    side, ends = (h - abs(p[0])) * slant, min(p[1] - c["CanyonSouth"], c["RimLine"] - p[1])
    best = max(best, min(side, ends) if side > 0 and ends > 0 else -math.hypot(max(0, -side), max(0, -ends)))
    best = max(best, box_sd(p, -c["Half"], -c["ApertureInner"], c["RimLine"] - 2, c["RimLine"] + 20), box_sd(p, c["ApertureInner"], c["Half"], c["RimLine"] - 2, c["RimLine"] + 20))
    best = max(best, c["CraterR"] - math.dist(p, SORREL_CENTRE))
    best = max(best, box_sd(p, EAST_FROM, c["EastEnd"], c["EastZ"] - c["EastHalf"], c["EastZ"] + c["EastHalf"]))
    for x0, x1, z0, z1 in ROAD_PARTS:
        best = max(best, box_sd(p, x0, x1, z0, z1))
    return best


def sorrel_clear(p, margin):
    """A hull of this radius round the point is all in Sorrel's air: the middle and sixteen points round it each inside some part (the parts' union, which Air's largest-of
    underrates where two parts join, as the valley's end and an aperture do)."""
    if sorrel_air(p) <= 0:
        return False
    return all(sorrel_air((p[0] + margin * math.cos(a), p[1] + margin * math.sin(a))) > 0 for a in (k * math.pi / 8 for k in range(16)))


class Shelf:
    """Sorrel.Shelf: a shelf turned to face the crater's middle, every point worked out as the builder works it."""

    def __init__(self, index, name, bearing):
        self.index, self.name, self.bearing = index, name, bearing
        self.berth, self.flag, self.guns, self.spawn = f"Clamp{index}", f"end.clamp{index}", f"Sorrel_Clamp{index}", f"Shelf{index}"
        a = math.radians(bearing)
        self.outward = (math.sin(a), math.cos(a))
        self.across = (self.outward[1], -self.outward[0])
        self.seat, self.middle, self.foot = self.at(SK["SeatR"]), self.at(SK["ShelfR"]), self.at(SK["ShelfR"] + 3.5)
        self.launch, self.works = self.at(SK["ShelfR"] + 2.5, 4.5), self.at(SK["SeatR"] - .9)
        self.pad, self.flight, self.back = self.at(SK["ShelfR"]), self.at(SK["ShelfR"] + 3), self.at(40)

    def at(self, r, side=0.0):
        c = SORREL_CENTRE
        return (round(c[0] + self.outward[0] * r + self.across[0] * side, 2), round(c[1] + self.outward[1] * r + self.across[1] * side, 2))

    def local(self, p):
        """A plan point in the shelf's own frame: along its inner edge, and out from her middle (r)."""
        d = (p[0] - SORREL_CENTRE[0], p[1] - SORREL_CENTRE[1])
        return d[0] * self.across[0] + d[1] * self.across[1], d[0] * self.outward[0] + d[1] * self.outward[1]


SHELVES = [Shelf(int(i), n, float(b)) for i, n, b in re.findall(r'new\((\d),"([^"]+)","[^"]+",' + NUMBER + r"\)", block(_SORREL, "Shelf[] Shelves="))]
SHELF_WIDE, SHELF_DEEP = 14.0, 10.0
WORKS_HALF = (1.6, .45)  # the winch housing: 3.2 wide, 0.9 deep, on the shelf's inner edge between the seat and her
MOTHER_BOX = next((b for b in encounters().get("SorrelSecondSky", []) if b["id"] == "MotherOfDoors"), None)
STRAIN_BEARING = 92.0  # Mother.StandStrain's default: the fourth shelf swings from its bearing out to 92 and back (MotherStrain.toBearing)


def check_sorrel_scene():
    """The built scene against the builder's numbers: each shelf's pad and launch where Shelf puts them, the warp home and the warp on."""
    prompts = hm.scene_prompts("SorrelSecondSky")
    wrong = []
    for s in SHELVES:
        for kind, where in (("Pad", s.pad), ("Launch", s.launch)):
            if not any(p["name"] == kind and math.dist(p["at"], where) < .1 for p in prompts):
                wrong.append(f"shelf {s.index}'s {kind.lower()} at {where}")
    if not any(p["to"] == "LastMooring" and math.dist(p["at"], SORREL_WARP_ON) < .1 for p in prompts):
        wrong.append(f"the warp on at {SORREL_WARP_ON}")
    if MOTHER_BOX is None:
        wrong.append("Mother of Doors' door in docs/quality/encounters.json")
    if wrong:
        fail("SorrelSecondSky as built has nothing at " + ", ".join(wrong) + " (a stale scene, or the builder moved it: rebuild, RouteFacts.Encounters, then this)")


# ------------------------------------------------------------------ Last Mooring, as LastMooringBuilder.Mooring lays it
_MOORING = block(MOORING_SOURCE, "internal static class Mooring")
MK = {k: const(_MOORING, k) for k in ("OuterX", "SouthZ", "GateZ", "NorthZ", "BeyondX", "GateHalf", "ShelfY", "ChainWide", "PylonRadius", "ArenaChainFrom", "ArenaChainTo")}
MK["Plane"] = 1.0
LINES = [(i, float(z), float(a), float(b)) for i, z, a, b in re.findall(r'new\("(\w)",' + NUMBER + "," + NUMBER + "," + NUMBER + r"\)", block(_MOORING, "Line[] Lines="))]
ARENA_CHAIN_X = [float(v) for v in re.findall(NUMBER, block(_MOORING, "ArenaChainX="))]
M_ARRIVAL, M_CHECKPOINT, M_DOCK = (vec3(_MOORING, k, MK)[0] for k in ("Arrival", "Checkpoint", "DockAt"))
_names_m = dict(MK)
M_ARM_PAD, M_ARM_LAUNCH, M_ARM_FOOT, M_ARM_FLIGHT, M_ARM_RETURN, M_ARM_MOTOR = (vec3(_MOORING, k, _names_m)[0] for k in ("ArmPad", "ArmLaunch", "ArmFoot", "ArmFlight", "ArmReturnAt", "ArmMotor"))


class Island:
    """Mooring.Island: a maintenance island between the chains, axis-aligned."""

    def __init__(self, index, name, at, wide, deep, side):
        self.index, self.name, self.at_, self.wide, self.deep = index, name, at, wide, deep
        self.berth, self.spawn = f"Island{index}", f"Island{index}Pad"
        self.pad = at
        self.foot = (at[0] + 1, at[1] - deep * .5 + 1.6)
        self.launch = (at[0] - wide * .5 + 2.2, at[1] - deep * .5 + 1.6)
        self.flight = (at[0], at[1] - deep * .5 - 6)
        self.back = (at[0] + side * 8, at[1] - deep * .5 - 10)

    def point(self, x, z):
        return (self.at_[0] + x, self.at_[1] + z)


ISLANDS = [Island(int(i), n, (float(x), float(z)), float(w), float(d), float(s)) for i, n, x, z, w, d, s in re.findall(
    r'new\((\d),"([^"]+)",new Vector2\(' + NUMBER + "," + NUMBER + r"\)," + NUMBER + "," + NUMBER + "," + NUMBER + r"\)", block(_MOORING, "Island[] Islands="))]
_cradle = re.search(r"Cradle=>Islands\[0\]\.At\(" + NUMBER + "," + NUMBER + r"\)", _MOORING)
CRADLE = ISLANDS[0].point(float(_cradle.group(1)), float(_cradle.group(2))) if _cradle else fail("LastMooringBuilder.Mooring has no Cradle=>Islands[0].At(x,z)")


def mooring_runs(gate_open):
    """Mooring.Runs: every chain run at the plane (and the gate's own, solid until the Tollkeeper's motor dies)."""
    c = MK
    ox, sz, gz, nz, bx, gh = c["OuterX"], c["SouthZ"], c["GateZ"], c["NorthZ"], c["BeyondX"], c["GateHalf"]
    runs = [((-ox, sz), (-ox, gz)), ((ox, sz), (ox, gz)), ((-ox, sz), (ox, sz)), ((-ox, gz), (-gh, gz)), ((gh, gz), (ox, gz)), ((-bx, gz), (-bx, nz)), ((bx, gz), (bx, nz)), ((-bx, nz), (bx, nz))]
    for _, z, a, b in LINES:
        runs += [((-ox, z), (a, z)), ((b, z), (ox, z))]
    runs += [((x, c["ArenaChainFrom"]), (x, c["ArenaChainTo"])) for x in ARENA_CHAIN_X]
    if not gate_open:
        runs.append(((-gh, gz), (gh, gz)))
    return runs


def mooring_pylons():
    at = []
    for a, b in mooring_runs(True):
        for p in (a, b):
            if all(math.dist(p, q) > 4 for q in at):
                at.append(p)
    return at


PYLONS = mooring_pylons()


def check_mooring_scene():
    prompts = hm.scene_prompts("LastMooring")
    wrong = []
    for i in ISLANDS:
        for kind, where in (("Pad", i.pad), ("Launch", i.launch)):
            if not any(p["name"] == kind and math.dist(p["at"], where) < .1 for p in prompts):
                wrong.append(f"island {i.index}'s {kind.lower()} at {where}")
    if not any(p["prompt"].startswith("Cradle") and math.dist(p["at"], CRADLE) < .1 for p in prompts):
        wrong.append(f"the cradle at {CRADLE}")
    if wrong:
        fail("LastMooring as built has nothing at " + ", ".join(wrong) + " (a stale scene, or the builder moved it)")


# ------------------------------------------------------------------ the checks
class Checked(hm.Checked):
    """Chapter 9's checks for Many Hands and its hull (Idra's hull), and chapter 10's: Sorrel's air, cover and her ring; Last Mooring's chains, pylons and gaps; on a shelf or an
    island the deck it is. `berth` is the shelf or island the party stands on (a Shelf or an Island), None in the zone's own form."""

    def __init__(self, *args, **kw):
        super().__init__(*args, **kw)
        self.berth = None
        self.freed = False  # end.motherFreed: Sorrel's chain line and east notch are gone
        self.gate_open = False

    def checks(self):
        c = super().checks()
        c.update({"SorrelSecondSky": self.check_sorrel, "LastMooring": self.check_mooring, "ReturningPassage": self.check_passage})
        return c

    def walk(self, name, x, y, z, tolerance=0.8, seconds=None, stop=None, buttons=None):
        if self.scene in ("SorrelSecondSky", "LastMooring"):
            y = SK["ShelfY"] if self.berth is not None else 1
        elif self.scene == "Continuance" and self.berth is not None:
            y = CONT_PLANE  # the flight berth round the hull flies at the hull's own plane
        super().walk(name, x, y, z, tolerance, seconds, stop, buttons)

    def check_deck(self, name, to):
        b = self.berth
        for p in self.legs(to):
            if isinstance(b, Shelf):
                along, out = b.local(p)
                off = max(abs(along) - (SHELF_WIDE / 2 - BODY - .5), abs(out - SK["ShelfR"]) - (SHELF_DEEP / 2 - BODY - .5))
                works = max(abs(along) - WORKS_HALF[0], abs(out - (SK["SeatR"] - .9)) - WORKS_HALF[1])
                if off > 0 or works < BODY + .15:
                    return self.fail(f"'{name}' leaves shelf {b.index}'s deck or walks into its works at ({p[0]:.1f}, {p[1]:.1f})")
            else:
                if abs(p[0] - b.at_[0]) > b.wide / 2 - BODY - .5 or abs(p[1] - b.at_[1]) > b.deep / 2 - BODY - .5:
                    return self.fail(f"'{name}' leaves island {b.index}'s deck at ({p[0]:.1f}, {p[1]:.1f})")

    def check_sorrel(self, name, to):
        if self.berth is not None:
            return self.check_deck(name, to)
        for p in self.legs(to):
            if not sorrel_clear(p, self.hull + 1):
                return self.fail(f"'{name}' flies out of Sorrel's air (a hull and a metre in) at ({p[0]:.1f}, {p[1]:.1f})")
            rock = next((c for c in SORREL_COVER if math.dist(p, c) < SK["CoverRadius"] + self.hull + .5), None)
            if rock:
                return self.fail(f"'{name}' flies within a hull of the cover rock at {rock} at ({p[0]:.1f}, {p[1]:.1f})")
            r = math.dist(p, SORREL_CENTRE)
            if r < SK["SeatR"]:
                return self.fail(f"'{name}' flies inside her ring (r {r:.1f}, the seats are on r {SK['SeatR']:g}: her head, her coil, the anchor clamps) at ({p[0]:.1f}, {p[1]:.1f})")
            if not self.freed and r < SK["CraterR"] and (p[1] > SK["ChainZ"] - 1.3 - self.hull - .5 or p[0] > 70 - self.hull - 1.8):
                return self.fail(f"'{name}' flies into the chain line (solid until end.motherFreed) at ({p[0]:.1f}, {p[1]:.1f})")

    def check_mooring(self, name, to):
        if self.berth is not None:
            return self.check_deck(name, to)
        c = MK
        for p in self.legs(to):
            run = next(((a, b) for a, b in mooring_runs(self.gate_open) if segment_distance(p, a, b) < c["ChainWide"] / 2 + self.hull + .5), None)
            if run:
                return self.fail(f"'{name}' flies within a hull of the chain {run} at ({p[0]:.1f}, {p[1]:.1f})")
            pylon = next((q for q in PYLONS if box_distance(p, q, 0, c["PylonRadius"], c["PylonRadius"]) < self.hull + .5), None)
            if pylon:
                return self.fail(f"'{name}' flies within a hull of the pylon at {pylon} at ({p[0]:.1f}, {p[1]:.1f})")

    def check_passage(self, name, to):
        """Inside the road's walls (a metre past its half-width) by a hull and a metre."""
        for p in self.legs(to):
            off, half = road_off(p)
            if off > half + 1 - self.hull - 1:
                return self.fail(f"'{name}' flies {off:.1f} m off the road's middle (its walls at {half + 1:.1f}) at ({p[0]:.1f}, {p[1]:.1f})")

    # -------------------------------------------------------------- the berth: in by the pad, out by the launch
    def dock(self, what, berth, seconds=None):
        """Fly to a berth's pad and press A until the party stands on it (ContinuousReview.Berth.cs, "docked:<id>"): the driver goes to the pad where it is now (the fourth shelf swings),
        every press in flight a shot until the pad is in reach. The pad is the built scene's, its reach checked from the walk's end."""
        prompts = hm.scene_prompts(self.scene)
        pad = next((p for p in prompts if p["name"] == "Pad" and math.dist(p["at"], berth.pad) < .1), None) or fail(f"{self.name}: {self.scene} has no pad at {berth.pad}")
        hm.reach(f"{self.name} '{what}'", berth.pad, 1.2, pad)
        check = self.checks().get(self.scene)
        if check:
            check("dock at " + what, berth.pad)
        far = 150.0 if self.at is None else math.dist(self.at, berth.pad) + 30
        self.add(f"dock at {what}", seconds or min(170.0, far / FLIGHT_SPEED * 1.6 + 6), navigate=True, point=dict(x=berth.pad[0], y=1, z=berth.pad[1]), tolerance=1.2,
                 until="docked:" + berth.berth, stopWhen="docked:" + berth.berth, buttons=["South"], pulseSeconds=0.3, evadeTelegraphs=True, evadeProjectiles=True)
        # The bodies fold into their new form over .28 s from the change's middle, a frame or two after "docked:" is true: the form is the next step's to say.
        self.add(f"stand on {what}", 0.6, expectedForm="Shaped")
        self.at, self.berth, self.speed, self.brake, self.cut = berth.foot, berth, COMBAT_SPEED, False, False

    def launch(self, what, seconds=12):
        """Walk to the berth's launch (where it stands now: the driver's hand) and press A until the party is back in flight ("launched:<id>")."""
        b = self.berth
        prompts = hm.scene_prompts(self.scene)
        out = next((p for p in prompts if p["name"] == "Launch" and math.dist(p["at"], b.launch) < .1), None) or fail(f"{self.name}: {self.scene} has no launch at {b.launch}")
        hm.reach(f"{self.name} '{what}'", b.launch, .4, out)
        self.check_deck("to the launch of " + what, b.launch)
        # Straight on the deck, never by the floor's bake: the fourth shelf's is baked where it was built, and docked it may stand thirteen metres round her ring from there.
        self.add(f"to the launch of {what}", seconds, navigate=True, point=dict(x=b.launch[0], y=SK["ShelfY"], z=b.launch[1]), tolerance=0.4, until="arrived", stopWhen="launched:" + b.berth,
                 evadeTelegraphs=True)
        self.add(f"settle before the launch of {what}", 0.5)
        self.add(f"launch from {what}", 6, buttons=["South"], pulseSeconds=0.6, until="launched:" + b.berth)
        self.add(f"release the launch of {what}", 0.6, expectedForm="Flight")
        self.at, self.berth, self.speed, self.cut = b.flight, None, FLIGHT_SPEED, False

    def ready(self, label):
        """After work on a deck: the partner raised (the driver's own feet, or a Spike), both standing, Taren leading."""
        self.add(f"reach the fallen hero after {label}", 16, navigate=True, approachPartner=True, tolerance=1.2, stopWhen="partyAlive", point=dict(x=0, y=0, z=0))
        self.add(f"both heroes ready after {label}", 6, until="partyAlive")
        self.lead("Taren")

    def work_clamp(self, shelf, rounds=5, seconds=30):
        """On a shelf: rounds of the fight's own steps that end when the clamp is open. The driver locks the Clamp Tender stepping out beside the seat and stops it, then walks to the
        seat, takes the clamp's weight with A and lets the pad go until it opens in her slack (chapter 7's clamp hand). Nothing here presses a skill: a press with the hands on the
        clamp would let it go (the hand keeps the pad still, but a skill step isn't a fight step)."""
        stop = "flag:" + shelf.flag
        # No lock step of its own: with nothing locked a fight step presses LB once a second (the tender is the only body of the lead's form on the shelf).
        for i in range(rounds):
            self.add(f"work clamp {shelf.index} round {i + 1}", seconds, buttons=["South"], pulseSeconds=0.46, navigate=True, approachTarget=True, tolerance=2.3, rangedTolerance=12,
                     evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=0, z=0))
        self.add(f"clamp {shelf.index} is open", 4, until=stop, expectedFlag=shelf.flag)
        self.at = None

    def leave(self, name, scene, spawn, form, seconds=6, flying=False):
        super().leave(name, scene, spawn, form, seconds, flying)
        self.berth = None


def ways_out(scene, to, near=None):
    return hm.prompt_of(scene, f"the way from {scene} to {to}", to=to, near=near)


def warp(r, what, scene, to, spawn, east=-4.0, tolerance=0.6):
    """Settle braked beside a warp (its 2 m piece is solid: four metres along x keeps Idra's hull clear of it) and take it."""
    w = ways_out(scene, to)
    stand = (round(w["at"][0] + east, 2), round(w["at"][1], 2))
    if box_distance(stand, w["at"], 0, 1, 1) < r.hull + .5:
        fail(f"{r.name}: the stand for {what} is within a hull of its piece")
    r.stand_at(what, stand, w, tolerance=tolerance, flying=True)
    r.leave(f"take {what}", to, spawn, "Flight", flying=True)


# ------------------------------------------------------------------ the routes
def one_plan():
    r = Checked(PREFIX + "62-one-plan", "Chapter 10: Idra's plan at the chart table, Orrin's roll, the hull pad, the warp to Sorrel's second sky, the valley's two pairs", chapter=10)
    # Chapter 9 ends at the fleet's chart on chapter9.complete, saved at the dock's arrival (its last zone load: the stern dock): the closing line and the arrow point at Idra's plan at
    # the same table (HouseLeaves.next, report OnePlan).
    r.resume("ManyHands", "chapter9.complete", spawn=H["Arrival"])
    r.speed = WALK
    r.lead("Taren")
    r.people = set(MANY_HANDS_PEOPLE)
    plan = hm.prompt_of("ManyHands", "Idra's plan", flag="end.planHeard")
    r.walk("up off the pads", 0, 0, -55, tolerance=0.5)
    r.go("up the dock and the spine to the chart table", [(0, -40), (0, -20), H["Crossing"]] + mh.SPINE_UP + [H["StairFoot"], H["StairTop"]], tolerance=0.4)
    # The chart table's other points are heard (Bryn's lesson, the fleet's chart) or not yet offered (the nursery, from end.planSaid): Idra's is the one in reach.
    r.stand_at("Idra's plan at the chart table", (plan["at"][0], plan["at"][1] - .6), plan, tolerance=0.4, y=0)
    r.hail("hear Idra's plan", 6, "end.planHeard")  # IdraOnePlan: five places, said once
    # Orrin's roll at his board, an after-talk .6 s after the plan's flag, wherever the party stands: thirteen lines, then TurnBack begins (end.planSaid) and the chapter's card.
    r.crossing("Orrin's roll", 13, "end.planSaid", wait=8)  # OrrinRoll
    r.wait("chapter ten begins", 3, expectedFlag="end.planSaid")
    pad = ways_out("ManyHands", "ManyHandsHull")
    # Corra's stall on the way down: the convoy's Choirwrought is on her counter from the fleet's chart (D326), and chapter 10 is sized on it. The weapon first, then the module
    # (the chain's pair: a Pulse Edge on Taren, Tempered modules on Idra); what the pair already holds of each counts. About 14,000 scrip in hand; no gels before the bosses.
    r.go("to Corra's stall", [H["StairTop"], H["StairFoot"], (0, 20)] + mh.SPINE_DOWN + [(-24, 3), (-24, 6)], tolerance=0.5)
    r.talk_to_shop("Corra's stall", 1, buys=(("Choirwrought Edge", 2), ("Choirwrought Module", 2)))
    r.go("down to the hull pad", [(-24, 3), H["Crossing"], (0, -20), (0, -40), (0, -55), (0, -57), (5.2, -58.2)], tolerance=0.5)
    r.stand_at("the hull pad", (5, -61), pad, tolerance=0.4, y=0)
    r.leave("launch to the hull", "ManyHandsHull", mh.O["ArrivalAt"], "Flight")
    r.speed = FLIGHT_SPEED
    # Off the stern dock south, then east of chapter 9's warp to Nacre along the stern's row to Sorrel's (-6, -176), stood west of it.
    r.walk("south off the stern dock", -26, 1, -148, tolerance=3)
    r.walk("toward the warp to Sorrel", -14, 1, -168, tolerance=3)
    warp(r, "the warp to Sorrel's Second Sky", "ManyHandsHull", "SorrelSecondSky", S_ARRIVAL)
    r.wait("first look at Sorrel from its sky", 2, expectedFlag="end.planSaid")
    # The haul road north between the spines, down its middle: two pairs of interceptors keep the clamp's approach (80 m boxes across the valley).
    r.lead("Taren")
    r.walk("north up the haul road", 0, 1, 60, tolerance=3)
    r.fight_flight("Sorrel_Lane1", rounds=4)
    r.walk("on north up the valley", 0, 1, 140, tolerance=3)
    r.fight_flight("Sorrel_Lane2", rounds=4)
    r.wait("the valley is clear", 2, expectedFlag="clear.Sorrel_Lane2")  # the clear saves (at the arrival: the zone's last spawn)
    r.write()


def mother():
    r = Checked(PREFIX + "63-mother-of-doors", "Chapter 10: the valley, Dace over his ridge, Mother of Doors' four clamps (her door retried), Dace as she goes", chapter=10)
    r.resume("SorrelSecondSky", "clear.Sorrel_Lane2", speed=FLIGHT_SPEED, spawn=S_ARRIVAL, form="Flight")
    r.lead("Taren")
    check_sorrel_scene()
    for z in (60, 140, 200):
        r.walk(f"north up the valley, z {z}", 0, 1, z, tolerance=3)
    # From the valley's head (where she is first seen) the east aperture is flown, down its middle (x 9.5; a hull and a metre in from both its walls).
    mouth = ((SK["ApertureInner"] + SK["Half"]) / 2, SK["RimLine"] - 2)
    r.walk("to the valley's head", S_HEAD[0], 1, S_HEAD[1], tolerance=3)
    r.walk("to the east aperture's mouth", mouth[0], 1, mouth[1], tolerance=2)
    # Dace's crossing lies across both apertures at z 250 (30 x 4): braked into it, so the ship stands in the aperture through his four lines and is nowhere near her door.
    r.brake = True
    r.walk("into the aperture until Dace speaks", mouth[0], 1, S_CROSSING[1] + 1, tolerance=1, stop="ui:dialogue")
    r.brake = False
    r.crossing("Dace over his ridge", 4, "end.sorrelSeen")  # SurvivorSecondSky: her door opens on it
    # Her door: the first step of the approach, a straight flight up the aperture from her checkpoint (Clamps, in the east aperture short of Dace's crossing) as from where
    # his lines left the ship, to just short of her box; then on through into it, ten metres in.
    r.door("MotherOfDoors", "up the east aperture to her door", mouth[0], 1, S_CROSSING[1] + 2, spawn=S_CHECKPOINT, tolerance=2)
    inside = (mouth[0], MOTHER_BOX["z"] - MOTHER_BOX["depth"] / 2 + 10)
    r.walk("through the aperture into her crater", inside[0], 1, inside[1], tolerance=2.5)
    # West round her ring to the first clamp (bearing 240), wide of the cover rock at bearing 222.5.
    r.walk("west round her ring, outside the cover", -34, 1, 300, tolerance=3)
    for n, shelf in enumerate(SHELVES):
        if n == 2:
            # From the second shelf's launch to the third clamp, south round the cover rock at bearing 180.
            r.walk("south round the middle rock", -8, 1, 290, tolerance=3)
            r.walk("east under the middle rock", 8, 1, 290, tolerance=3)
        label = shelf.name[0].lower() + shelf.name[1:]
        r.fight_flight(shelf.guns, rounds=4)
        r.dock(label, shelf)
        r.work_clamp(shelf)
        if shelf.index < len(SHELVES):
            r.ready(f"clamp {shelf.index}")
            r.launch(label)
    # The fourth opened, she goes up her road and the young after her; Dace's one line six seconds after (an after-talk: the party is still on the fourth shelf).
    r.wait("she lets go", 2, expectedFlag="bossdown.MotherOfDoors")
    r.crossing("Dace as she goes", 1, "end.motherFreed", wait=20)  # SurvivorMotherGoes
    r.ready("her release")
    r.launch(SHELVES[-1].name[0].lower() + SHELVES[-1].name[1:])
    r.wait("over her empty crater", 2, expectedFlag="end.motherFreed")
    r.write()


def last_mooring():
    r = Checked(PREFIX + "64-last-mooring", "Chapter 10: the lane east, Last Mooring's chains, the first pair, Many Hands struck and held, the west island's cradle, the guns at chain C, the second pair", chapter=10)
    # Saved after her release on the fourth shelf: a save on a berth names its own spawn in flight, off its pad (wiring 6.1 rule 2).
    fourth = SHELVES[-1]
    r.resume("SorrelSecondSky", "end.motherFreed", speed=FLIGHT_SPEED, spawn=fourth.back, form="Flight")
    r.freed = True
    r.lead("Taren")
    check_mooring_scene()
    r.walk("east across the crater to the notch", 50, 1, SK["EastZ"], tolerance=3)
    r.walk("east along the lane to the warp on", SORREL_WARP_ON[0] - 12, 1, SK["EastZ"], tolerance=3)
    warp(r, "the warp to Last Mooring", "SorrelSecondSky", "LastMooring", M_ARRIVAL)
    r.wait("first look at the anchorage", 2, expectedFlag="end.motherFreed")
    r.lead("Taren")
    a, b, c = (next(l for l in LINES if l[0] == k) for k in "ABC")
    r.walk("north through chain A's gap", (a[2] + a[3]) / 2, 1, a[1] + 15, tolerance=3)
    r.fight_flight("Mooring_Lane1", rounds=4)
    gap_b = (b[2] + b[3]) / 2
    r.walk("to chain B's gap", gap_b, 1, b[1] - 12, tolerance=3)
    # Through the gap and the section's crossing at z 172 (the whole arena, x 14 to 28 is its frame): end.sectionStruck and the lead's bark, then Maru's after-talk 3.8 s later.
    r.walk("through chain B's gap and over Many Hands", gap_b, 1, b[1] + 20, tolerance=3)
    r.crossing("Many Hands is struck and Maru's weavers hold it", 3, "end.sectionHeld", wait=12)  # MaruSectionHeld
    # The west island's free cradle: dock, repair and save (its spawn Island1Pad from here), launch.
    west = ISLANDS[0]
    r.dock("the west maintenance island", west)
    cradle = hm.prompt_of("LastMooring", "the west island's cradle", prompt="Cradle")
    stand = (CRADLE[0], round(CRADLE[1] - 1.6, 2))
    hm.reach(f"{r.name} 'the cradle'", stand, .4, cradle)
    r.check_deck("to the cradle", stand)
    r.walk("to the island's cradle", stand[0], SK["ShelfY"], stand[1], tolerance=0.4)
    r.save_at("repair and save at the island's cradle", "release the cradle")
    r.launch("the west maintenance island")
    # Chain C's gap is held by two crossed clamp guns on its chain either side (Mooring_Gap): flown down, then through the gap's middle.
    r.fight_flight("Mooring_Gap", rounds=4)
    gap_c = (c[2] + c[3]) / 2
    r.walk("to chain C's gap", gap_c, 1, c[1] - 10, tolerance=2.5)
    r.walk("through chain C's gap", gap_c, 1, c[1] + 12, tolerance=3)
    r.fight_flight("Mooring_Lane2", rounds=4)
    r.wait("the moorings are clear to the gate", 2, expectedFlag="clear.Mooring_Lane2")  # the clear saves, at the island's spawn
    r.write()


# ------------------------------------------------------------------ the Tollkeeper's lane (LastMooring.md, "For route writers, the fight")
BRACES = [(-27.7, 434.2), (27.7, 434.2), (-31.1, 443.6)]  # the couplings at rest, one a cycle: west, east, west
ARM = Island("arm", "the Tollkeeper's arm", M_ARM_PAD, 6, 12, 1)
ARM.berth, ARM.foot, ARM.launch, ARM.flight = "Arm", M_ARM_FOOT, M_ARM_LAUNCH, M_ARM_FLIGHT
MOTOR_STAND = (M_ARM_MOTOR[0], round(M_ARM_MOTOR[1] - 1.2, 2))


def tollkeeper():
    r = Checked(PREFIX + "65-the-tollkeeper", "Chapter 10: chain D, the Tollkeeper (its door retried): three braces in flight, three docks on its arm and the gate motor stopped a stage at a time, the gate open, the dock to the Continuance", chapter=10)
    r.resume("LastMooring", "clear.Mooring_Lane2", speed=FLIGHT_SPEED, spawn=ISLANDS[0].back, form="Flight")
    r.lead("Taren")
    c, d = (next(l for l in LINES if l[0] == k) for k in "CD")
    gap_c = (c[2] + c[3]) / 2
    r.walk("back to chain C's gap", gap_c, 1, c[1] - 10, tolerance=2.5)
    r.walk("north through chain C's gap", gap_c, 1, c[1] + 12, tolerance=3)
    r.walk("north to the gate checkpoint", M_CHECKPOINT[0], 1, M_CHECKPOINT[1], tolerance=3)
    door = next(b for b in encounters()["LastMooring"] if b["id"] == "Tollkeeper")
    # Its door: through chain D's gap from the checkpoint, eight metres into its box (the partner's ship behind the lead's is through too).
    r.door("Tollkeeper", "through chain D's gap into its lane", 0, 1, door["z"] - door["depth"] / 2 + 8, spawn=M_CHECKPOINT, tolerance=2)
    r.walk("up the middle lane past the dividing chains", 0, 1, 420, tolerance=3)
    for n in (1, 2, 3):
        bx, bz = BRACES[n - 1]
        near = (round(bx * .78, 1), round(bz - 7, 1))
        r.walk(f"toward a brace on the {'west' if bx < 0 else 'east'} outrigger", near[0], 1, near[1], tolerance=3)
        stop = f"flag:end.brace{n}"
        r.press(f"take aim at a brace {n}", "LeftShoulder", 0.2, leftTrigger=1)
        for i in range(4):
            r.add(f"break brace {n} round {i + 1}", 25, buttons=["South"], leftTrigger=1, navigate=True, approachTarget=True, tolerance=12, rangedTolerance=12, evadeProjectiles=True,
                  evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=1, z=0))
            r.add(f"pulse brace {n} round {i + 1}", 0.18, buttons=["RightShoulder", "West"], leftTrigger=1, stopWhen=stop)
            r.add(f"release pulse brace {n} round {i + 1}", 0.45, leftTrigger=1)
        r.add(f"brace {n} is broken", 4, until=stop, expectedFlag=f"end.brace{n}")
        r.at = None
        # About three seconds later the arm runs out of the gate with the motor on it: the driver flies at its pad where it is (in, behind the gate, it comes out to it).
        r.dock("the Tollkeeper's arm", ARM, seconds=30)
        r.walk("to the gate motor", MOTOR_STAND[0], SK["ShelfY"], MOTOR_STAND[1], tolerance=0.5)
        stop = "flag:" + (f"end.motor{n}" if n < 3 else "bossdown.Tollkeeper")
        r.press(f"take aim at the gate motor {n}", "LeftShoulder", 0.2)
        for i in range(4):
            r.rotation(f"motor {n} {i + 1}", stop)
            r.add(f"stop the gate motor's stage {n} round {i + 1}", 12, buttons=["South"], pulseSeconds=0.46, navigate=True, approachTarget=True, tolerance=2.3, rangedTolerance=12,
                  evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=0, z=0))
        r.add(f"stage {n} is stopped", 3, until=stop, expectedFlag=stop[5:])
        r.at = None
        if n < 3:
            r.launch("the Tollkeeper's arm", seconds=5)
            r.walk("back south of the cage", 0, 1, 420, tolerance=3)
        else:
            # The motor dies and the gate with it: the party is put off the arm by itself.
            r.add("put off the arm as the gate opens", 10, until="launched:" + ARM.berth)
            r.add("in flight over the open gate", 0.6, expectedForm="Flight")
            r.at, r.berth, r.speed = ARM.flight, None, FLIGHT_SPEED
    r.gate_open = True
    r.crossing("Orrin: the gate's hanging open", 2, "end.gateOpen", wait=12)  # OrrinGateOpen
    dock = ways_out("LastMooring", "Continuance")
    r.walk("north over the carrier", dock["at"][0], 1, dock["at"][1] - 20, tolerance=3)
    r.stand_at("the dock on the Continuance's spine", (dock["at"][0], dock["at"][1] - 4), dock, tolerance=0.8, flying=True)
    r.leave("dock at the Continuance", "Continuance", (0, -76), "Shaped", flying=True)
    r.speed = COMBAT_SPEED
    r.wait("at the spine's foot", 2, expectedFlag="end.gateOpen")
    r.write()


# ------------------------------------------------------------------ the Continuance (Continuance.md, "For route writers")
_CONT = block(source("LastMooringBuilder.Continuance.cs"), "internal static class Continuance")
CN = hm.vec2s(_CONT)
CONT_WAYS = [[(float(a), float(b)) for a, b in re.findall(r"new\(" + NUMBER + "," + NUMBER + r"\)", part)] for part in
             re.findall(r"new Vector2\[\]\{(.*?)\}", block(_CONT, "internal static readonly Vector2[][] Ways=")[1:-1])]
CONT_PLANE = const(_CONT, "Plane")


def cont_y(p):
    """The decks by stretch: the spine from its foot (y -5) up to the hull's face (0), the crew deck (0) to the stair at z 53, then the lower decks (-3)."""
    z = p[1]
    if z < -68:
        return -5.0
    if z < -16:
        return round(-5 + 5 * (z + 68) / 52, 2)
    return 0.0 if z < 53 else -3.0


class FlightBerth:
    """The Continuance's flight berth round its hull: in by the bay's pad on foot, out by its 'Dock' in flight."""
    berth, pad, launch, flight, foot = "HullBerth", CN["PadAt"], (-8.0, 196.0), (-8.0, 182.0), CN["FootAt"]


def continuance():
    r = Checked(PREFIX + "66-the-continuance", "Chapter 10: the boarding spine, the mess, the captain of the Steady Hand, the works, the flight berth, Meret's craft (her door retried), the dock to the gantry", chapter=10)
    r.resume("Continuance", "end.gateOpen", speed=COMBAT_SPEED, spawn=(0, -76))
    r.lead("Taren")
    main = CONT_WAYS[0]

    def go(name, points, **kw):
        for i, p in enumerate(points):
            r.walk(f"{name} {i + 1}/{len(points)}", p[0], cont_y(p), p[1], **kw)
    r.walk("up the boarding spine", 0, cont_y((0, -40)), -40, tolerance=0.6)
    r.walk("on up the spine until Idra speaks", 0, 0, main[0][1], stop="ui:dialogue")
    r.crossing("Idra's crews brace the spine", 3, "end.boarded")  # IdraSpineBraced
    go("aboard, to the mess", main[:3], tolerance=0.6)
    r.fight_ground("Continuance_Mess", rounds=5)
    go("to the stair and deck four", [(4, 44), (0, 50), (0, 60), (0, 72)], tolerance=0.6)
    r.walk("across deck four until the captain speaks", 0, -3, 88, stop="ui:dialogue")
    r.crossing("The captain of the Steady Hand lets them pass", 3, "end.captainPassed")  # PatrolLetsPass: he steps aside on his third line
    r.wait("the captain stands aside", 3)
    go("on to the works", [(0, 104), (0, 110)], tolerance=0.6)
    r.fight_ground("Continuance_Works", rounds=7)
    go("through the works' north door to the bay", [(0, 170), (0, 190), (-2, 214), (2, 236)], tolerance=0.4)
    berth = FlightBerth()

    def dock_on_foot(what):
        pad = next(p for p in hm.scene_prompts("Continuance") if p["name"] == "Pad" and math.dist(p["at"], berth.pad) < .1)
        hm.reach(f"{r.name} '{what}'", r.at, .4, pad)
        r.add(f"settle before {what}", 0.5)
        r.add(what, 6, buttons=["South"], pulseSeconds=0.6, until="docked:" + berth.berth, stopWhen="docked:" + berth.berth)
        r.add("flying round the hull", 0.6, expectedForm="Flight")
        r.at, r.berth, r.speed = berth.flight, berth, FLIGHT_SPEED
    dock_on_foot("launch from the bay round the hull")
    # Her hail across the berth at z 203: then back down to the bay, so that her door is a walk on foot from the bay as a retry (her checkpoint, LaunchBay) has it.
    r.walk("north over the hull until Meret hails", berth.flight[0], CONT_PLANE, 204, tolerance=1.5, stop="ui:dialogue")
    r.crossing("Meret hails them from her craft", 6, "end.meretHailed")  # MeretShipToShip
    back = next(p for p in hm.scene_prompts("Continuance") if p["name"] == "Launch" and math.dist(p["at"], berth.launch) < .1)
    r.stand_at("the bay's way back in", (berth.launch[0], berth.launch[1] - 3), back, tolerance=0.8, flying=True)
    r.add("back in to the bay", 6, buttons=["South"], pulseSeconds=0.6, until="launched:" + berth.berth, leftTrigger=1)
    r.add("on foot in the bay", 0.6, expectedForm="Shaped")
    r.at, r.berth, r.speed = berth.foot, None, COMBAT_SPEED
    r.door("MeretShip", "to the bay's pad", 2, -3, 236, spawn=(5, 230), tolerance=0.4)
    dock_on_foot("launch round the hull after her craft")
    r.walk("into her lane", -4, CONT_PLANE, 214, tolerance=2.5)
    mh.flight_boss(r, "MeretShip", rounds=14)
    r.crossing("Meret at the gantry's rail", 4, "end.meretShip", wait=12)  # MeretGantry
    gantry = ways_out("Continuance", "StillwaterChamber")
    r.at = None
    r.stand_at("the dock at the frame gantry", (gantry["at"][0], gantry["at"][1] - 5), gantry, tolerance=0.8, flying=True)
    r.leave("dock at the frame gantry", "StillwaterChamber", (0, -21), "Shaped", flying=True)
    r.speed = COMBAT_SPEED
    r.wait("on the gangway to the gantry", 2, expectedFlag="end.meretShip")
    r.write()


def chamber():
    r = Checked(PREFIX + "67-the-anchor-chamber", "Chapter 10: Meret at the gantry (her door retried, her couplings worked), the lift, Meret in the well (her door retried), her yield, the sequence, the last collar, the release, the launch", chapter=10)
    r.resume("StillwaterChamber", "end.meretShip", speed=COMBAT_SPEED, spawn=(0, -21))
    hr.boss(r, "MeretGantry", "north through the gantry's seal", 0, 0, -9, spawn=(0, -21), rounds=12)
    r.recover("the gantry")
    r.fought()
    lift = hm.prompt_of("StillwaterChamber", "the lift", prompt="Ride down")
    r.stand_at("the lift's cage", (lift["at"][0], lift["at"][1] - 2.1), lift, tolerance=0.4, y=0)
    r.offered("wait for the cage to come back up", "Ride down", seconds=20)
    r.add("ride down to the control well", 25, buttons=["South"], pulseSeconds=30, until="saved")
    r.at = (0, 46.4)
    r.walk("into the well's door until Meret speaks", 0, -16, 54, tolerance=0.6, stop="ui:dialogue")
    r.crossing("Meret in the control well", 6, "end.meretGantry")  # MeretWell
    hr.boss(r, "MeretWell", "into the well", 0, -16, 59.5, spawn=(0, 47.6), rounds=12)
    r.crossing("Meret yields", 5, "end.crewsOrdered", wait=8)  # MeretYields
    r.crossing("Tovra reads the sequence", 6, "end.sequenceRead", wait=25)  # IshenAnchorSequence: once the crews stand
    r.fought()
    # The seat's own hold, not step nine's arrow standing on it (a never-offered point with the same flag and a reach of nothing): which of the two a built scene lists first changes
    # with every rebuild of the chamber, and the arrow found first failed the reach check (November 14, the look pass's rebuild).
    seat = max((p for p in hm.scene_prompts("StillwaterChamber") if p["flag"] == "end.collarTaken"), key=lambda p: p["range"], default=None) \
        or hm.prompt_of("StillwaterChamber", "the last collar", flag="end.collarTaken")
    r.stand_at("the last collar", (seat["at"][0], seat["at"][1] - .8), seat, tolerance=0.4, y=-16)
    # One press and the pad still through a pull and an ease (a roll or a swing lets go): the step presses once at its start, and nothing is drunk from the pouch.
    r.add("settle before the last collar", 0.6)
    r.add("take the last collar and hold", 30, buttons=["South"], pulseSeconds=40, until="flag:end.collarTaken", useItem="none", expectedFlag="end.collarTaken")
    r.hands_off("the four clamps let go", 3)
    r.crossing("Meret sees it open", 5, "end.anchorReleased", wait=15)  # MeretSeesItOpen
    r.crossing("Ilo takes her into custody", 4, "end.custody", wait=12)  # IloCustody
    out = ways_out("StillwaterChamber", "ReturningPassage")
    r.walk("back south through the well", 2.0, -16, 56.6, tolerance=0.5)
    r.stand_at("the launch to the passage", (out["at"][0], out["at"][1] + 1.4), out, tolerance=0.4, y=-16)
    r.leave("launch into the Returning Passage", "ReturningPassage", (0, 0), "Flight")
    r.speed = FLIGHT_SPEED
    r.wait("the road ahead", 2, expectedFlag="end.custody")
    r.write()


# ------------------------------------------------------------------ the Returning Passage (ReturningPassage.md, "For route writers")
_PASS = block(source("LastMooringBuilder.Passage.cs"), "internal static class Passage")
PASS_LINE = [(float(x), float(z)) for x, z in re.findall(r"new\(" + NUMBER + r",Plane," + NUMBER + r"\)", block(_PASS, "Vector3[] Line="))]
PASS_HALVES = [float(v) for v in re.findall(NUMBER, block(_PASS, "float[] Halves="))]
PS = {k: const(_PASS, k) for k in ("IloS", "TugS", "LineS", "TurnS", "FarEndS", "MouthS")}


def on_road(s, side=0.0):
    """Passage.On: s metres along the line, side metres to starboard (ConvoyFlow.At)."""
    run = 0.0
    for i in range(1, len(PASS_LINE)):
        a, b = PASS_LINE[i - 1], PASS_LINE[i]
        leg = math.dist(a, b)
        if s <= run + leg or i == len(PASS_LINE) - 1:
            t = (s - run) / max(.01, leg)
            ahead = ((b[0] - a[0]) / leg, (b[1] - a[1]) / leg)
            return (round(a[0] + (b[0] - a[0]) * t + ahead[1] * side, 2), round(a[1] + (b[1] - a[1]) * t - ahead[0] * side, 2))
        run += leg


def road_half(s):
    run = 0.0
    for i in range(1, len(PASS_LINE)):
        leg = math.dist(PASS_LINE[i - 1], PASS_LINE[i])
        if s <= run + leg or i == len(PASS_LINE) - 1:
            return PASS_HALVES[i - 1] + (PASS_HALVES[i] - PASS_HALVES[i - 1]) * clamp01((s - run) / leg)
        run += leg


def road_off(p):
    """How far a plan point is off the road's middle, and the road's half-width there (its walls stand a metre beyond it)."""
    best, run, half = math.inf, 0.0, 30.0
    for i in range(1, len(PASS_LINE)):
        a, b = PASS_LINE[i - 1], PASS_LINE[i]
        leg = math.dist(a, b)
        d = segment_distance(p, a, b)
        if d < best:
            t = clamp01(((p[0] - a[0]) * (b[0] - a[0]) + (p[1] - a[1]) * (b[1] - a[1])) / (leg * leg))
            best, half = d, road_half(run + t * leg)
        run += leg
    return best, half


def passage():
    r = Checked(PREFIX + "68-the-returning-passage", "Chapter 10: the run north with Ilo's names, Bryn falls behind, Sela turns back, the tug's drones, the line, the tow to the far mouth, the rendezvous, the cut", chapter=10)
    r.resume("ReturningPassage", "end.custody", speed=FLIGHT_SPEED, spawn=on_road(40), form="Flight")
    r.lead("Taren")
    s_ilo = PS["IloS"]
    ilo = on_road(s_ilo + 15)
    r.walk("north along the road until Ilo reads", ilo[0], 1, ilo[1], tolerance=3, stop="ui:dialogue")
    r.crossing("Ilo reads the first name", 4, "end.namesBegun")  # IloFirstName
    # The tug waits on the road's middle at s 760 (solid, cover in her fight): the run passes her twelve metres to port.
    for s, side in ((330, 0), (492, 0), (640, 0), (720, -12), (800, -12), (900, 0), (1000, 0)):
        p = on_road(s, side)
        r.walk(f"along the road, {s:g} m in", p[0], 1, p[1], tolerance=3)
    far = on_road(PS["FarEndS"] + 8)
    r.walk("to the far end until Bryn falls behind", far[0], 1, far[1], tolerance=3, stop="ui:dialogue")
    r.crossing("Bryn falls behind", 9, "end.passageRun")  # BrynFallsBehind
    # Sela turns back: the zone loads again at Turn with Sela and Taren out (D286).
    r.add("Sela turns back", 40, until="flag:end.turnedBack", expectedFlag="end.turnedBack")
    r.wait("facing back down the road", 2)
    r.at = on_road(PS["TurnS"])
    r.lead("Taren")
    south = on_road(880)
    r.walk("back south down the road", south[0], 1, south[1], tolerance=3)
    r.fight_flight("Passage_Tug", rounds=5)
    line = hm.prompt_of("ReturningPassage", "the tug's line", flag="end.lineOn")
    r.stand_at("the tug's bow", (line["at"][0], line["at"][1] + 4), line, tolerance=1, flying=True)
    r.press_for("take the tug's line", "end.lineOn", seconds=8)
    r.crossing("Bryn on the line", 4, "end.tugFetched", wait=8)  # BrynOnTheLine
    for s in (860, 960, 1060, 1160, 1240):
        p = on_road(s)
        r.walk(f"towing north, {s:g} m in", p[0], 1, p[1], tolerance=3)
    mouth = on_road(PS["MouthS"] + 8)
    r.walk("to the far mouth until Orrin speaks", mouth[0], 1, mouth[1], tolerance=3, stop="ui:dialogue")
    r.crossing("The rendezvous", 4, "end.rendezvous")  # OrrinRendezvous
    r.scene = "NeighbourhoodAfloat"
    r.add("seven weeks on", 40, until="scene:NeighbourhoodAfloat", expectedUi="world", expectedForm="Natural")
    r.add("release after the cut", 0.5)
    r.at, r.speed = (0, -4), WALK
    r.wait("the new sky", 2, expectedFlag="end.rendezvous")
    r.write()


def homecoming():
    r = Checked(PREFIX + "69-neighbourhood-afloat", "Chapter 10: seven weeks on, Orrin at the new board, the handrail, the line, Jun's last line, the credits, the town after them", chapter=10)
    # After the passage's turn Sela and Taren are out (D286): the pair is the save's, and nothing here fights.
    r.deployed = None
    r.resume("NeighbourhoodAfloat", "end.rendezvous", spawn=(0, -4))
    r.speed = WALK
    r.lead("Taren")
    orrin = hm.prompt_of("NeighbourhoodAfloat", "Orrin", prompt="Talk to Orrin")
    r.go("up the quay and the gangways to the square", [(0, 14), (0, 20), (-6, 30), (-6, 50), (-6, 58)], tolerance=0.4)
    # The straight line from the square's foot to Orrin's spot is cut (something stands at about (-9.8, 64.4)): the floor's own way, as a person walks round it.
    r.at = None
    r.stand_at("Orrin at the new board", (-11.98, 68.04), orrin, tolerance=0.3, y=0)
    r.hail("Orrin's directorship", 5, "end.handrail")  # OrrinDirectorship
    rail = hm.prompt_of("NeighbourhoodAfloat", "the handrail", flag="end.railMended")
    r.at = None
    r.go("back down to the garden stair", [(-6, 58), (-6, 42), (-6, 30), (-12, 28.6)], tolerance=0.4)
    r.stand_at("the loose handrail", rail["at"], rail, tolerance=0.3, y=0)
    r.press_for("mend the handrail", "end.railMended")
    line = hm.prompt_of("NeighbourhoodAfloat", "the line", flag="end.linePainted")
    r.at = None  # round the stall's corner by the floor's own way (the straight line is cut at (-19.2, 25.7))
    r.stand_at("the old yellow line", line["at"], line, tolerance=0.3, y=0)
    r.press_for("repaint the line", "end.linePainted")
    r.hands_off("look at the pears past the line", 2.5)
    # JunLastLine: four lines, its last press puts game.complete up and the credits begin by themselves; nothing is pressed after it but A held to hurry them (Start is ignored
    # on a profile that has never seen them to their end).
    r.add("Jun's last line begins", 8, until="ui:dialogue", expectedUi="dialogue")
    for i in range(4):
        r.add(f"read Jun's last line line {i + 1}", 1.9, stopWhen="flag:game.complete")
        r.add(f"next Jun's last line line {i + 1}", 0.15, buttons=["South"], stopWhen="flag:game.complete")
        r.add(f"release Jun's last line line {i + 1}", 0.2, stopWhen="flag:game.complete")
    r.add("the last word", 20, buttons=["South"], pulseSeconds=1.0, until="flag:game.complete", expectedFlag="game.complete")
    r.add("let the last word go", 1.5, useItem="none")
    for i in range(3):
        r.add(f"the credits, held to hurry {i + 1}", 170, buttons=["South"], stopWhen="flag:credits.rolled", useItem="none")
    r.add("the credits end", 30, until="flag:credits.rolled", useItem="none", expectedFlag="credits.rolled")
    r.wait("back in the town under the new sky", 4, expectedFlag="game.complete", expectedUi="world")
    r.write()


ROUTES = {62: one_plan, 63: mother, 64: last_mooring, 65: tollkeeper, 66: continuance, 67: chamber, 68: passage, 69: homecoming}

if __name__ == "__main__":
    wanted = [int(a) for a in sys.argv[1:]] or sorted(ROUTES)
    for number in wanted:
        if number not in ROUTES:
            fail(f"no chapter 10 route numbered {number} ({', '.join(map(str, sorted(ROUTES)))})")
        ROUTES[number]()
    print("MOORING_ROUTES_OK")
