"""Writes chapter 6's replay routes (chapter-32 to chapter-37) for the Aurora Shear, Lastlight and the Frozen Host from the builders' own places.

Places are read out of the builders (HoarfellBuilder.AuroraShear.cs: the lane, its currents and the named places along it; HoarfellBuilder.Lastlight.cs: the
town's places, its declared Ways, its folk and walkers; HoarfellBuilder.FrozenHost.cs: the dig's places, its rooms and their floors, its declared Ways;
HoarfellBuilder.StageManyHands: what chapter 6 stands aboard Many Hands) and checked against the coordinates tables of their map docs (docs/maps/AuroraShear.md,
Lastlight.md, FrozenHost.md) before anything is written: a place a route uses that has drifted from its doc stops the run, unless it is listed in KNOWN_DRIFT
with the doc's exact wrong number (each is a finding in the report, printed on every run). The Shear's ice shards are the builder's own seeded scatter
(System.Random(61), ported here and compared with the built scene's colliders). Nobody has run these. Every straight leg is checked here before it is written:
in the Shear inside its walls and clear of every shard and warp piece by a hull; in Lastlight and the Frozen Host along the declared ways, on the floor its own
rooms make, off the people; aboard Many Hands by manyhands_routes' city check. HoarfellRouteTests.EveryWalkOnTheChapterSixRoutesFollowsFloorABodyCanCross then
checks every leg on the built zones' navigation and colliders.

The chain: chapter-31-many-hands-errands ends saved at the riggers' cradle (spawn FreightHall), chapter 5 told.
  chapter-32-aurora-shear       Many Hands: the chart for Hoarfell (starts The Long Sleep), the launch, the hull's warp to the Shear; the first current,
                                Idra's hail and tow, the short lane and its tender, the fast lane's two fights, the descent's, and the way down to Lastlight.
                                Its fights ride the currents with the party, so after each it heads only on down the lane, from wherever it was carried,
                                into the next lane fight's own volume (ride_into), each such leg checked from wherever the fight before it can have ended.
  chapter-33-lastlight          the ribs, Old Tern, Idra and Hask at the pens, Hask's counter (gel to the cap), Idra's plan, the Reach gate
  chapter-34-lastlight-errands  side content: back by the Reach gate, Iru and the skull lamps (the climb, three high lamps), the fourth rib's tally, out again
  chapter-35-frozen-host        the White Reach (the spear field, the herd, the whiteouts, the stalkers' field, the camp) and the dig (three levels, three rigs,
                                five fights) to the dig cradle by the bottom gallery
  chapter-36-host-marshal       Idra joins, the Marshal wakes, the Host Marshal (boss, its door retried), it sleeps, the winch up to Lastlight
  chapter-37-long-sleep         with Taren and Idra out: Old Tern's long sleep, Sela's chart home, Idra's cutters at the dock (chapter6.complete)

Waits on the staging aboard Many Hands (HoarfellBuilder.StageManyHands, not in the scenes as built on November 12): chapter-32's chart for Hoarfell and the
hull's warp to the Shear, and chapter-37's lift figures on the dock. The routes are written to the builder's numbers for them.

  python tools/hoarfell_routes.py
"""
import math
import re
import subprocess
import sys
from pathlib import Path

import lantern_routes as lt
import manyhands_routes as mh
from lantern_routes import COMBAT_SPEED, FLIGHT_SPEED
from manyhands_routes import BODY, NUMBER, arrays, block, boss, points, vectors
from xylos_routes import box_distance, segment_distance

ROOT = Path(__file__).resolve().parent.parent
WORLD = ROOT / "Lattice" / "Assets" / "_Project" / "Scripts" / "Editor" / "World"
SCENES = ROOT / "Lattice" / "Assets" / "_Project" / "Scenes"
MAPS = ROOT / "docs" / "maps"
PREFIX = "chapter-"
HULL, HULL_IDRA = 1.78, 2.17  # HeroCollision.HullRadius: Taren's (the larger of chapter 5's two), and Idra's, once she is out (FlightSocketTests)

SHEAR_SOURCE = (WORLD / "HoarfellBuilder.AuroraShear.cs").read_text(encoding="utf-8")
TOWN_SOURCE = (WORLD / "HoarfellBuilder.Lastlight.cs").read_text(encoding="utf-8")
HOST_SOURCE = (WORLD / "HoarfellBuilder.FrozenHost.cs").read_text(encoding="utf-8")
STAGE_SOURCE = (WORLD / "HoarfellBuilder.cs").read_text(encoding="utf-8")


# ------------------------------------------------------------------ the Aurora Shear, as HoarfellBuilder.ShearZone lays it
def consts(source, start):
    """The `Name=value` floats of one `internal const float ...;` statement that begins at `start`."""
    i = source.index(start)
    text = source[i:source.index(";", i)]
    return {m.group(1): float(m.group(2)) for m in re.finditer(r"\b([A-Z]\w*)=" + NUMBER, text)}


LANE = points(block(SHEAR_SOURCE, "internal static readonly Vector2[] Lane="), {})
HALF = float(re.search(r"internal const float Half=" + NUMBER, SHEAR_SOURCE).group(1))
S = consts(SHEAR_SOURCE, "internal const float HailS=")
PAST = S.get("CorridorOverrun", HALF)  # how far each stretch's walls run past its ends (ShearZone.CorridorOverrun; FlightCorridor's default is the half width)
CURRENTS = [(m.group(1), [(float(a), float(b)) for a, b in re.findall(r"\((-?[\d.]+)f?,(-?[\d.]+)f?\)", m.group(2))], float(m.group(3)))
            for m in re.finditer(r'\("([^"]+)",new\[\]\{(.*?)\},' + NUMBER + "," + NUMBER + r',"', block(SHEAR_SOURCE, "Color tone)[] Currents="))]
LANDS_AT = None  # set below, once beside() exists


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
    return (round(x + hz * starboard, 2), round(z - hx * starboard, 2))


def from_lane(p):
    return min(segment_distance(p, LANE[i - 1], LANE[i]) for i in range(1, len(LANE)))


def from_currents(p):
    """How far a plan point is outside the nearest current (negative inside one), as ShearZone.FromCurrents."""
    best = 1e9
    for _, line, half in CURRENTS:
        pts = [beside(s, side) for s, side in line]
        best = min(best, min(segment_distance(p, pts[i - 1], pts[i]) for i in range(1, len(pts))) - half)
    return best


class DotNetRandom:
    """System.Random(seed), the subtractive generator Unity's runtime and .NET Framework share: what a builder's seeded scatter draws."""
    MBIG, MSEED = 2147483647, 161803398

    def __init__(self, seed):
        a = [0] * 56
        mj = self.MSEED - abs(seed)
        a[55], mk = mj, 1
        for i in range(1, 55):
            ii = (21 * i) % 55
            a[ii] = mk
            mk = mj - mk
            if mk < 0:
                mk += self.MBIG
            mj = a[ii]
        for _ in range(4):
            for i in range(1, 56):
                a[i] -= a[1 + (i + 30) % 55]
                if a[i] < 0:
                    a[i] += self.MBIG
        self.a, self.inext, self.inextp = a, 0, 21

    def next_double(self):
        self.inext = 1 if self.inext + 1 >= 56 else self.inext + 1
        self.inextp = 1 if self.inextp + 1 >= 56 else self.inextp + 1
        r = self.a[self.inext] - self.a[self.inextp]
        if r == self.MBIG:
            r -= 1
        if r < 0:
            r += self.MBIG
        self.a[self.inext] = r
        return r / self.MBIG

    def next(self, n):
        return int(self.next_double() * n)


def _tow():
    """Idra's tow as ShearZone.Tow lays it: (along, starboard) points, the builder's names read as their constants."""
    text = block(SHEAR_SOURCE, "internal static readonly (float s,float side)[] Tow=")
    out = []
    for a, b in re.findall(r"\(\s*([\w.\-]+)f?\s*,\s*([\w.\-]+)f?\s*\)", text):
        out.append(tuple(S[v] if v in S else float(v) for v in (a, b)))
    return out


TOW = _tow()
TOW_ROOM = float(re.search(r"internal const float TowRoom=" + NUMBER, SHEAR_SOURCE).group(1))


def from_tow(p):
    """How far a plan point is from the way Idra's line takes the ships (ShearZone.FromTow): the first current's width across her hail (-20 to 0 starboard), from
    each end of it to the cutter's wait, and her tow."""
    held = [beside(S["HailS"], -20), beside(S["HailS"], 0)]
    wait = beside(*TOW[0])
    tow = [beside(s, side) for s, side in TOW]
    ways = [(held[0], held[1]), (held[0], wait), (held[1], wait)] + list(zip(tow, tow[1:]))
    return min(segment_distance(p, a, b) for a, b in ways)


def shards():
    """The ring's shards at the plane (ShearZone.Shards): fourteen draws a 150 m stretch, each dropped if it would stand within its size and 3 m of a current,
    of a kept place, or of the hail's middle, or within its size and TowRoom of the way Idra's line takes the ships. Each solid is a block size x 1.3 square,
    turned by its yaw."""
    keep = [((0, 6), 5), ((-14, -4), 7), (beside(S["CutterS"], S["CutterSide"]), 7), (beside(S["PodS"], S["PodSide"]), 10), (beside(S["CacheS"], S["CacheSide"]), 7),
            (beside(S["LandS"], S["LandSide"]), 16), (beside(S["FromTownS"], S["FromTownSide"]), 5)]
    length = sum(math.dist(LANE[i - 1], LANE[i]) for i in range(1, len(LANE)))
    rng, out, start = DotNetRandom(61), [], 0.0
    while start < length:
        for _ in range(14):
            s = start + rng.next_double() * 150
            side = (rng.next_double() * 2 - 1) * (HALF - 6)
            size = 1.6 + rng.next_double() * 2.6
            rng.next(3), rng.next(9999)
            yaw = rng.next_double() * 360
            p = beside(s, side)
            if from_currents(p) < size + 3 or any(math.dist(c, p) < clear + size + 3 for c, clear in keep) or math.dist(p, beside(S["HailS"], 0)) < 6 \
                    or from_tow(p) < size + TOW_ROOM:
                continue
            out.append((f"the ice shard at ({p[0]:.1f}, {p[1]:.1f})", p, yaw, size * .65, size * .65))
        start += 150
    return out


SHARDS = shards()


def lane_walls():
    """The Shear's limits as ZoneKit.FlightCorridor builds them: for each stretch of the lane a 2 m wall either side at the lane's half width, run PAST metres
    past both of the stretch's ends. At a bend the inside walls run on across the next or the last stretch's open space."""
    walls = []
    for i in range(1, len(LANE)):
        a, b = LANE[i - 1], LANE[i]
        length = math.dist(a, b)
        along = ((b[0] - a[0]) / length, (b[1] - a[1]) / length)
        across = (along[1] * HALF, -along[0] * HALF)
        for side, word in ((-1, "port"), (1, "starboard")):
            before, after = (HALF if i == 1 else PAST), (HALF if i == len(LANE) - 1 else PAST)  # the lane's own ends meet the walls across them
            f = (a[0] - along[0] * before + across[0] * side, a[1] - along[1] * before + across[1] * side)
            t = (b[0] + along[0] * after + across[0] * side, b[1] + along[1] * after + across[1] * side)
            walls.append((f"Lane limit {i} {word}", ((f[0] + t[0]) / 2, (f[1] + t[1]) / 2), math.degrees(math.atan2(t[0] - f[0], t[1] - f[1])), 1, math.dist(f, t) / 2))
    return walls


SHEAR_SOLIDS = SHARDS + lane_walls() + [("the warp home", (-14, -4), 0, 1, 1), ("the way down", beside(S["LandS"], S["LandSide"]), 0, 1, 1)]


def station(p):
    """How far along the lane a plan point lies, and how far to starboard of its middle (the inverse of beside)."""
    best, walked = None, 0.0
    for i in range(1, len(LANE)):
        a, b = LANE[i - 1], LANE[i]
        leg = math.dist(a, b)
        hx, hz = (b[0] - a[0]) / leg, (b[1] - a[1]) / leg
        t = max(0.0, min(leg, (p[0] - a[0]) * hx + (p[1] - a[1]) * hz))
        q = (a[0] + hx * t, a[1] + hz * t)
        if best is None or math.dist(p, q) < best[0]:
            best = (math.dist(p, q), walked + t, (p[0] - q[0]) * hz - (p[1] - q[1]) * hx)
        walked += leg
    return best[1], best[2]


# How far off its current's middle a lane fight may leave the party, for the leg after it: every leg flown from where a lane fight was cleared is checked from
# each place this near the middle (the current's own half width and a little: the fast lane's first fight was cleared 12.0 m to starboard of it in the lead's
# first player run, November 14). A party left further out than this may meet a shard on the leg: nothing here steers round one.
ENDS_WITHIN = 12


def built_shards():
    """The Ice shard colliders in the built AuroraShear scene, (x, z) and side: a stale scene or a changed builder shows as a difference."""
    text = (SCENES / "AuroraShear.unity").read_text(encoding="utf-8")
    docs = {m.group(2): (m.group(1), m.group(3)) for m in re.finditer(r"^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- |\Z)", text, flags=re.M | re.S)}
    found = []
    for kind, body in docs.values():
        if kind != "1" or not re.search(r"m_Name: Ice shard\s*$", body, flags=re.M):
            continue
        pos = size = None
        for c in re.findall(r"component: \{fileID: (\d+)\}", body):
            k, b = docs.get(c, (None, ""))
            if k == "4":
                m = re.search(r"m_LocalPosition: \{x: ([^,]+), y: [^,]+, z: ([^}]+)\}", b)
                pos = (float(m.group(1)), float(m.group(2)))
            elif k == "65":
                size = float(re.search(r"m_Size: \{x: ([^,]+),", b).group(1))
        found.append((pos, size))
    return found


# ------------------------------------------------------------------ Lastlight, as HoarfellBuilder.TownZone lays it
T = vectors(block(TOWN_SOURCE, "internal static class TownZone"))
HIGH_LAMPS = points(block(TOWN_SOURCE, "HighLamps="), T)
CLIMB = [(float(a), float(b), float(c)) for a, b, c in re.findall(r"new\((-?[\d.]+)f?,(-?[\d.]+)f?,(-?[\d.]+)f?\)", block(TOWN_SOURCE, "Vector3[] Climb="))]
T["HighLamp1"], T["HighLamp2"], T["HighLamp3"] = HIGH_LAMPS
_ways = block(TOWN_SOURCE, "internal static readonly Vector2[][] Ways=")
for _n, (_x, _z) in enumerate(HIGH_LAMPS):
    _ways = _ways.replace(f"HighLamps[{_n}]", f"new Vector2({_x},{_z})")
TOWN_WAYS = arrays(_ways, T)
TOWN_FOLK = [(float(m.group(2)), float(m.group(3))) for m in re.finditer(r'\("(\w+)",' + NUMBER + "," + NUMBER + "," + NUMBER + r",new\(", block(TOWN_SOURCE, "Color tint)[] Folk="))]
TOWN_FOLK += [(float(m.group(2)), float(m.group(3))) for m in re.finditer(r'\("(\w+)",' + NUMBER + "," + NUMBER + "," + NUMBER + r",new\(", block(TOWN_SOURCE, "Color tint)[] Children="))]
TOWN_WALKS = [points(m.group(1), T) for m in re.finditer(r"new\[\]\{(.*?)\}", block(TOWN_SOURCE, "string name)[] Errands="))]
LEDGE_HALF = float(re.search(r"LedgeHalf=" + NUMBER, TOWN_SOURCE).group(1))


def town_y(p):
    """The floor's height at a plan point of Lastlight: the climb's ledge (its points' heights, between them), the high rib-hall's bank (4.8) with the knoll by the
    skull (6.2), else the snow at about 0."""
    for i in range(1, len(CLIMB)):
        a, b = CLIMB[i - 1], CLIMB[i]
        d = (b[0] - a[0], b[2] - a[2])
        t = max(0.0, min(1.0, ((p[0] - a[0]) * d[0] + (p[1] - a[2]) * d[1]) / (d[0] ** 2 + d[1] ** 2)))
        if math.dist(p, (a[0] + d[0] * t, a[2] + d[1] * t)) < LEDGE_HALF:
            return round(a[1] + (b[1] - a[1]) * t, 2)
    if p[0] > 44 and p[1] > 57.5:
        return round(4.8 + 1.4 * max(0.0, 1 - max(0.0, math.dist(p, T["KnollAt"]) - 2.5) / 5), 2)
    return 0


# ------------------------------------------------------------------ the Frozen Host, as HoarfellBuilder.FrozenHost lays it
F = vectors(HOST_SOURCE[HOST_SOURCE.index("internal static readonly Vector2 C=new("):HOST_SOURCE.index("/// <summary>The ambusher's hides")])
FIGHTS = [((float(a), float(b)), (float(c), float(d))) for a, b, c, d in re.findall(r"\(new\(" + NUMBER + "," + NUMBER + r"\),new\(" + NUMBER + "," + NUMBER + r"\)\)", block(HOST_SOURCE, "Vector2 size)[] Fights="))]
HOST_LEVELS = consts(HOST_SOURCE, "internal const float Snow=")
ROOMS = [(m.group(1), *(float(v) for v in m.groups()[1:])) for m in re.finditer(r"\(Room\.(\w+)," + ",".join([NUMBER] * 5) + r"\)", block(HOST_SOURCE, "float round)[] Boxes="))]
HOST_WAYS = [points(w, {}) for w in re.findall(r"new Vector2\[\]\{(.*?)\}", block(HOST_SOURCE, "internal static readonly Vector2[][] Ways="))]
CREVASSES = [(50, 2, .06, .4, 2.2, [(2, 6.4), (-30, 4.4)]), (136, 1.5, .08, 2.1, 2, [(6, 6.4)])]  # FrozenHost.Crevasses, checked against the source below
C = F["C"]
ARENA_RADIUS, SHELF_RADIUS = 18, 20.5


def round_box(p, x0, x1, z0, z1, r):
    dx, dz = abs(p[0] - (x0 + x1) / 2) - (x1 - x0) / 2 + r, abs(p[1] - (z0 + z1) / 2) - (z1 - z0) / 2 + r
    return min(max(dx, dz), 0) + math.hypot(max(dx, 0), max(dz, 0)) - r


def bearing_of(p):
    return math.degrees(math.atan2(p[0] - C[0], p[1] - C[1])) % 360


def arena_reach(b):
    b = (b + 68) % 360
    return ARENA_RADIUS + (SHELF_RADIUS - ARENA_RADIUS) * mh.smooth(0, 10, b) * (1 - mh.smooth(186, 196, b))


def nearest_room(p):
    best, room = math.dist(p, C) - arena_reach(bearing_of(p)), "Arena"
    for which, x0, x1, z0, z1, r in ROOMS:
        clear = round_box(p, x0, x1, z0, z1, r)
        if clear < best:
            best, room = clear, which
    return best, room


def crevasse(p):
    best = 1e9
    for z, amp, freq, phase, half, bridges in CREVASSES:
        if p[0] < -54 or p[0] > 54:
            continue
        band = abs(p[1] - (z + amp * math.sin(p[0] * freq + phase))) - half
        bridge = min(abs(p[0] - x) - wide / 2 for x, wide in bridges)
        best = min(best, max(band, -bridge))
    return best


def host_y(p):
    """A floor's level by its room (FrozenHost.Level): the Reach's snow at 0, the mouth ramp down to the upper gallery (-4), the three ramps between the levels,
    the bottom -17.5."""
    _, room = nearest_room(p)
    L1, L2, L3, BOTTOM = HOST_LEVELS["L1"], HOST_LEVELS["L2"], HOST_LEVELS["L3"], HOST_LEVELS["Bottom"]
    lerp = lambda a, b, t: a + (b - a) * max(0.0, min(1.0, t))
    return round({"Reach": 0, "Gap": 0, "Gate": 0, "Mouth": lerp(0, L1, (p[1] - 166) / 10), "L1": L1, "Ramp1": lerp(L1, L2, (p[0] - 66) / 12), "L2": L2, "Left": L2, "Cut": L2,
                  "Ramp2": lerp(L2, L3, (p[0] - 97) / 14), "L3": L3, "Ramp3": lerp(L3, BOTTOM, (p[0] - 153) / 12)}.get(room, BOTTOM), 2)


# ------------------------------------------------------------------ what stands solid in the built town and dig (manyhands_routes.scene_solids): the scatter's draws
# and the builders' blocks between ankle and head over the floor, less the walkers, who move, and the dig's plugs, rank ice and seal, which the rigs melt and the
# fight opens.
_TOWN_WALKERS = tuple(m.group(1) for m in re.finditer(r'(?:true|false),"([^"]+)"\)', block(TOWN_SOURCE, "string name)[] Errands=")))
BUILT_TOWN = [(l, c, yaw, hx, hz) for l, c, yaw, hx, hz, y0, y1 in mh.scene_solids("Lastlight", _TOWN_WALKERS) if y0 < town_y(c) + 1.8 and y1 > town_y(c) + .3 and hx < 30 and hz < 30]
BUILT_HOST = [(l, c, yaw, hx, hz) for l, c, yaw, hx, hz, y0, y1 in mh.scene_solids("FrozenHost", ("Plug one", "Plug two", "Plug three", "Left gallery rank ice", "Third rank ice", "Bottom gallery seal", "Rime-beast"))
              if y0 < host_y(c) + 1.8 and y1 > host_y(c) + .3 and hx < 30 and hz < 30]
TOWN_NAMED = {k: T[k] for k in ("Tern", "Hask", "IruShrine", "IruHall", "IdraYard")}


# ------------------------------------------------------------------ the map docs
# Places a route here stands at whose doc gives another number than its builder: the doc's own (wrong) value, so any further drift still stops the run.
KNOWN_DRIFT = {}


def doc_table(name):
    """Each row of a map doc's coordinates table: its cells and every number pair in its other cells, as (x, z) (from (x, z), (x, y, z) or a bare 'x, z'). The
    Frozen Host's table gives its places in its third column ("x, z") and signs and notes beside them in the others: there, that column only."""
    text = (MAPS / name).read_text(encoding="utf-8")
    section = text[text.index("## Coordinates"):]
    rest = section[4:]
    section = section[:4 + rest.index("\n## ")] if "\n## " in rest else section
    rows = []
    for line in section.splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 3 or set(cells[0]) <= set("-: "):
            continue
        text_ = cells[2] if name == "FrozenHost.md" else " ".join(cells[1:])
        triples = re.findall(r"\((-?[\d.]+), (-?[\d.]+), (-?[\d.]+)\)", text_)
        pairs = [(float(a), float(c)) for a, b, c in triples] if triples else [(float(a), float(b)) for a, b in re.findall(r"(-?\d+(?:\.\d+)?), (-?\d+(?:\.\d+)?)", text_)]
        rows.append((cells, pairs))
    return rows


def check_docs():
    wrong, known = [], []

    def expect(doc, label, places, contains=None):
        rows = [r for r in doc_table(doc) if r[0][0].startswith(label) and (contains is None or contains in " ".join(r[0]))]
        if not rows:
            wrong.append(f"{doc}: no row '{label}'")
            return
        given = rows[0][1]
        for i, place in enumerate(places):
            if i >= len(given) or math.dist(given[i], place) > 0.25:
                if KNOWN_DRIFT.get((doc, label)) == (given[i] if i < len(given) else None):
                    known.append(f"{doc} '{label}': the builder has {place}, the doc {given[i]}")
                    continue
                wrong.append(f"{doc} '{label}': the builder has {place}, the doc {given[i] if i < len(given) else 'nothing'}")

    b = lambda s, side: beside(s, side)
    expect("AuroraShear.md", "Arrival", [(0, 6)])
    expect("AuroraShear.md", "Warp home", [(-14, -4)])
    expect("AuroraShear.md", "Idra's hail line", [b(S["HailS"], 0)])
    expect("AuroraShear.md", "Cutter waiting", [b(S["CutterS"], S["CutterSide"])])
    expect("AuroraShear.md", "Tow ends", [b(S["TowEndS"], S["TowEndSide"])])
    expect("AuroraShear.md", "Skiffs on Idra's lane", [b(600, 44)])
    expect("AuroraShear.md", "Fast lane tender and skiffs", [b(860, 4)], "Fast_0")
    expect("AuroraShear.md", "Fast lane tender and skiffs", [b(1080, 2)], "Fast_1")
    expect("AuroraShear.md", "Descent tender and skiffs", [b(S["DescentFightS"], S["DescentFightSide"])])
    expect("AuroraShear.md", "Back from town", [b(S["FromTownS"], S["FromTownSide"])])
    expect("AuroraShear.md", "**The way down**", [b(S["LandS"], S["LandSide"])])
    for name, line, half in CURRENTS:
        label = {"The first current": "C1", "Idra's lane": "C2", "The short lane": "C3", "The fast lane": "C4", "The counter-current": "C5", "The descent": "C6"}[name]
        expect("AuroraShear.md", label, [b(s, side) for s, side in line])
    expect("Lastlight.md", "Arrival", [T["Arrival"]])
    expect("Lastlight.md", "Landing pad", [T["Pad"]])
    expect("Lastlight.md", "Hask at the pens", [T["Hask"]])
    expect("Lastlight.md", "Idra arguing", [T["IdraArguing"]])
    expect("Lastlight.md", "Pens crossing", [T["PensCrossing"]])
    expect("Lastlight.md", "Idra at the carving yard", [T["IdraYard"]])
    expect("Lastlight.md", "Tern", [T["Tern"]])
    expect("Lastlight.md", "The fourth rib's tally", [T["RibTally"]])
    expect("Lastlight.md", "Iru at the shrine", [T["IruShrine"]])
    for n in range(3):
        expect("Lastlight.md", f"High lamp {n + 1}", [HIGH_LAMPS[n]])
    expect("Lastlight.md", "Reach gate", [T["ReachGate"]])
    expect("Lastlight.md", "Back from the Reach", [T["ReachSpawn"]])
    for label, place in (("Arrival spawn", F["Arrival"]), ("Way home", F["GateWay"]), ("Main bridge", F["MainBridge"]), ("Vent pocket 1", F["VentOne"]), ("Vent pocket 2", F["VentTwo"]),
                         ("Rig one", F["RigOneHand"]), ("Rig two", F["RigTwoHand"]), ("Rig three", F["RigThreeHand"]), ("Door spawn", F["DoorSpawn"]), ("Dig cradle", F["DoorCradle"]),
                         ("The winch up", F["WinchHand"]), ("Marshal talk", (F["Passage"][0] - 2.5, F["Passage"][1]))):
        expect("FrozenHost.md", label, [place])
    for label, place in (("Reach talk", (0, 12)), ("Herd talk", (0, 57)), ("Whiteout talk", (0, 100)), ("Vessa's first words", (24, 163)), ("First rank", (24, 181)),
                         ("Vessa's offer", (86, 184)), ("Winch talk", (172, 218)), ("Idra joins", (182, 218))):
        expect("FrozenHost.md", label, [place])
    # The crossings above are the builder's literals (FrozenHost: kit.Crossing): each must still be there.
    for at in ("new Vector3(0,0,12)", "new Vector3(0,0,57)", "new Vector3(0,0,100)", "new Vector3(24,0,163)", "new Vector3(24,0,181)", "new Vector3(86,0,184)", "new Vector3(172,0,218)",
               "new Vector3(182,0,218)"):
        if at not in HOST_SOURCE:
            wrong.append(f"HoarfellBuilder.FrozenHost.cs has no crossing at {at} any more")
    if "(50,2,.06f,.4f,2.2f,new[]{(2f,6.4f),(-30f,4.4f)})" not in HOST_SOURCE or "(136,1.5f,.08f,2.1f,2f,new[]{(6f,6.4f)})" not in HOST_SOURCE:
        wrong.append("HoarfellBuilder.FrozenHost.cs: the crevasses aren't the ones this models")
    staged = re.search(r"var warpAt=new Vector3\(" + NUMBER + r",ZoneKit\.FlightPlane," + NUMBER + r"\)", STAGE_SOURCE)
    if not staged or (float(staged.group(1)), float(staged.group(2))) != WARP_TO_SHEAR:
        wrong.append(f"HoarfellBuilder.StageManyHands: the hull's warp to the Shear isn't at {WARP_TO_SHEAR} any more")
    if "Floor(new Vector2(0,-54.6f))" not in STAGE_SOURCE:
        wrong.append("HoarfellBuilder.StageManyHands: the lift figures' crossing isn't at (0, -54.6) any more")
    for line in known:
        print("KNOWN_DRIFT " + line)
    built = built_shards()
    differ = [p for p, size in built if not any(math.dist(p, s[1]) < .05 and abs(size - s[3] * 2) < .05 for s in SHARDS)]
    if differ or len(built) != len(SHARDS):
        print(f"WARNING the built AuroraShear has {len(built)} shards, the builder makes {len(SHARDS)}; {len(differ)} built ones aren't the builder's (a stale scene?)")
    if wrong:
        raise SystemExit("a place here isn't where its map doc says:\n" + "\n".join(wrong) + "\nHOARFELL_ROUTES_FAILED")


WARP_TO_SHEAR, FROM_SHEAR = (-90.0, -176.0), (-82, -166)  # HoarfellBuilder.StageManyHands: the hull's warp and its spawn Hoarfell
LIFT_FIGURES = (0, -54.6)  # the lift figures' crossing on the dock, 16 x 3 (StageManyHands)


class Checked(mh.Checked):
    """manyhands_routes' checks for the ship and the hull, and Hoarfell's: the Shear's walls, shards and warp pieces by a hull (Idra's once she is out); Lastlight's
    and the Frozen Host's declared ways, their rooms' floors and the people on them."""

    def __init__(self, *args, **kw):
        super().__init__(*args, **kw)
        self.hull = HULL
        self.town_people = set()
        self.ended = None  # (current, from, to): where the last lane fight may have been cleared, for the leg after it (fought_in)

    def checks(self):
        c = super().checks()
        c.update({"AuroraShear": self.check_shear, "Lastlight": self.check_town, "FrozenHost": self.check_host})
        if self.hull > HULL:
            c["ManyHandsHull"] = lambda name, to: self.check_hull_as(name, to, self.hull)
        return c

    def walk(self, name, x, y, z, tolerance=0.8, seconds=None, stop=None, buttons=None):
        if y == 0 and self.scene == "Lastlight":
            y = town_y((x, z))
        elif y == 0 and self.scene == "FrozenHost":
            y = host_y((x, z))
        if self.ended and self.at is None and self.scene == "AuroraShear":
            self.check_from_ends(name, (x, z))
        self.ended = None
        super().walk(name, x, y, z, tolerance, seconds, stop, buttons)

    def fought_in(self, id_, until, *currents):
        """A lane fight is over: the party is wherever the current and the chase left it (manyhands' fought), somewhere from the fight's volume on down the lane to
        `until`, in the currents named. The next leg is checked from every place there within ENDS_WITHIN of a current's middle, not from its end only."""
        self.fought()
        self.ended = (currents, span(self.scene, id_)[0], until)

    def check_from_ends(self, name, to):
        currents, since, until = self.ended
        for s in range(int(since), int(until) + 1, 4):
            middle = next((side_on(c, s) for c in currents if covers(c, s)), None)
            if middle is None:
                continue
            current = next(c for c in currents if covers(c, s))
            for off in range(-ENDS_WITHIN, ENDS_WITHIN + 1, 3):
                start = beside(s, middle + off)
                if from_lane(start) > HALF - 1 - self.hull or any(box_distance(start, c, yaw, hx, hz) < self.hull for _, c, yaw, hx, hz in SHARDS):
                    continue  # nowhere a ship can be
                hit = self.crossing_shear(start, to)
                if hit:
                    self.problems.append(f"{self.name}: '{name}' flies within a hull ({self.hull} m) of {hit} from ({start[0]:.1f}, {start[1]:.1f}), {off:+d} m off "
                                         f"{current}'s middle at {s} along, where the fight before it may have been cleared")
                    return

    def crossing_shear(self, a, b):
        """What a straight leg from a to b in the Shear flies within a hull of (its walls, a shard, a warp piece), or None: check_shear's test, for a leg from anywhere."""
        lo, hi = (min(a[0], b[0]) - 10, min(a[1], b[1]) - 10), (max(a[0], b[0]) + 10, max(a[1], b[1]) + 10)
        near = [s for s in SHEAR_SOLIDS if "Lane limit" in s[0] or lo[0] <= s[1][0] <= hi[0] and lo[1] <= s[1][1] <= hi[1]]
        steps = max(1, int(math.dist(a, b) / 0.4))
        for i in range(steps + 1):
            p = (a[0] + (b[0] - a[0]) * i / steps, a[1] + (b[1] - a[1]) * i / steps)
            if from_lane(p) > HALF - 1 - self.hull:
                return "the lane's wall"
            hit = next((label for label, c, yaw, hx, hz in near if box_distance(p, c, yaw, hx, hz) < self.hull + .8), None)
            if hit:
                return hit
        return None

    def ride_into(self, id_, across=0.0, along=0.0, seconds=45):
        """On down the lane into a lane fight's volume from wherever the fight before it was cleared. Route.enter walks into a box only from a known place, and a
        fight in a current ends where the current and the chase took it: in the lead's first player run the fast lane's first fight was cleared 12 m to starboard
        of the current's middle, a metre out of it, and the party sat there through both fights after it, locking at nothing, with nothing to carry it on
        (November 14). The point is the box's middle moved `across` and `along` in its own frame, inside it by more than the leg's tolerance (RouteFightTests), and
        chosen so the legs from fought_in's places clear every shard. A fight begun in the same rounds as the last one may be cleared already: the leg ends then."""
        box = next(b for b in lt.encounters()[self.scene] if b["id"] == id_)
        turn = math.radians(box["yaw"])
        right, fore = (math.cos(turn), -math.sin(turn)), (math.sin(turn), math.cos(turn))
        if abs(across) > box["width"] / 2 - 4 or abs(along) > box["depth"] / 2 - 4:
            self.problems.append(f"{self.name}: the leg into {id_} aims {across}, {along} from its middle: not inside it by the leg's tolerance")
        x, z = box["x"] + right[0] * across + fore[0] * along, box["z"] + right[1] * across + fore[1] * along
        self.walk(f"on down the lane into {id_}", round(x, 1), 1, round(z, 1), tolerance=3, stop=f"flag:clear.{id_}", seconds=seconds)

    def fight_flight(self, id_, rounds=3, seconds=30):
        # A lock pressed from wherever the last fight left the party is judged by nobody (RouteFightTests): the Shear's two lane fights after the fast lane's first
        # were locked that way, from 190 m short of the first of them.
        if self.scene == "AuroraShear" and self.at is None:
            self.problems.append(f"{self.name}: 'lock {id_}' is pressed from wherever the fight before it ended: fly into its volume first (ride_into)")
        super().fight_flight(id_, rounds, seconds)

    def check_hull_as(self, name, to, hull):
        for p in self.legs(to):
            hit = next((label for label, c, yaw, hx, hz in mh.HULL_SOLIDS if box_distance(p, c, yaw, hx, hz) < hull + .8), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' flies within {hull} m of {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def check_shear(self, name, to):
        for p in self.legs(to):
            if from_lane(p) > HALF - 1 - self.hull:
                self.problems.append(f"{self.name}: '{name}' flies {from_lane(p):.1f} m off the lane's middle, into its wall")
                return
            hit = next((label for label, c, yaw, hx, hz in SHEAR_SOLIDS if box_distance(p, c, yaw, hx, hz) < self.hull + .8), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' flies within a hull ({self.hull} m) of {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def check_town(self, name, to):
        # The ways, the climb's ledge (TownZone.Climb, a way of its own), and the floor in front of a prompt the ways stop short of (the Reach gate's arch, the rib's cuts).
        ways = TOWN_WAYS + [[(x, z) for x, _, z in CLIMB]]
        stands = [(T["ReachGate"], 7), (T["RibTally"], 4)]  # the floor before the gate's arch and the rib's cuts (the gate's way now runs on to (-60.8, 42))
        for p in self.legs(to):
            off = min(min(segment_distance(p, w[i - 1], w[i]) for i in range(1, len(w))) for w in ways)
            on_bank = p[0] > 50 and 57.5 < p[1] < 70 and p[0] < 61  # the high rib-hall's top, between the ledge and the knoll: legs here were asked of the baked floor
            if off > 1.2 and not on_bank and not any(math.dist(p, at) <= reach for at, reach in stands):
                self.problems.append(f"{self.name}: '{name}' leaves Lastlight's ways by {off:.1f} m at ({p[0]:.1f}, {p[1]:.1f})")
                return
            who = next((n for n in self.town_people if math.dist(p, T[n]) < 1.3), None)
            if who:
                self.problems.append(f"{self.name}: '{name}' walks through {who} at ({p[0]:.1f}, {p[1]:.1f})")
                return
            folk = next((f for f in TOWN_FOLK if math.dist(p, f) < .35 + BODY + .15), None)
            if folk:
                self.problems.append(f"{self.name}: '{name}' walks through a townsperson at {folk}")
                return
            hit = next((label for label, c, yaw, hx, hz in BUILT_TOWN if box_distance(p, c, yaw, hx, hz) < BODY + .15), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' walks into {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return
            hole = next((n for n, at in TOWN_NAMED.items() if math.dist(p, at) < .35 + BODY + .1), None)
            if hole:
                self.problems.append(f"{self.name}: '{name}' walks over {hole}'s place at ({p[0]:.1f}, {p[1]:.1f}), a hole in the floor's navigation")
                return

    def check_host(self, name, to):
        for p in self.legs(to):
            clear, room = nearest_room(p)
            if clear > -BODY:
                self.problems.append(f"{self.name}: '{name}' leaves the floor at ({p[0]:.1f}, {p[1]:.1f}): {clear:.1f} m from {room}'s edge")
                return
            if room in ("Reach", "Gap") and crevasse(p) < BODY:
                self.problems.append(f"{self.name}: '{name}' walks into a crevasse at ({p[0]:.1f}, {p[1]:.1f})")
                return
            off = min(min(segment_distance(p, w[i - 1], w[i]) for i in range(1, len(w))) for w in HOST_WAYS)
            # Off the walked lines only inside a fight's own ground (FrozenHost.Fights, the bottom gallery), which the builder keeps clear for it.
            in_fight = any(abs(p[0] - c[0]) < size[0] / 2 - 1 and abs(p[1] - c[1]) < size[1] / 2 - 1 for c, size in FIGHTS) or math.dist(p, C) < 16
            # And the floor in front of what a hero works that the walked lines pass at a distance: the gate's arch home (the Reach's line starts at the
            # arrival) and each rig's valve; and the bottom gallery's mouth and passage, the room the checkpoint, the cradle, the bench and the winch stand in.
            stands = [(F["GateWay"], 10), (F["RigOneHand"], 4), (F["RigTwoHand"], 4), (F["RigThreeHand"], 4)]
            if off > 2.5 and not in_fight and room not in ("Ante", "Door") and not any(math.dist(p, at) <= reach for at, reach in stands):
                self.problems.append(f"{self.name}: '{name}' leaves the Frozen Host's walked lines by {off:.1f} m at ({p[0]:.1f}, {p[1]:.1f})")
                return
            hit = next((label for label, c, yaw, hx, hz in BUILT_HOST if box_distance(p, c, yaw, hx, hz) < BODY + .15), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' walks into {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def fly(self, name, s, side, tolerance=3, stop=None, seconds=None):
        x, z = beside(s, side)
        self.walk(name, x, 1, z, tolerance=tolerance, stop=stop, seconds=seconds)

    def settle(self, name, s, side, tolerance=1):
        self.brake = True
        self.fly(name, s, side, tolerance=tolerance)
        self.brake = False

    def write(self):
        if self.problems:
            raise SystemExit("\n".join(self.problems) + "\nHOARFELL_ROUTES_FAILED")
        mh.Route.write(self)


def covers(name, s):
    line = next(l for n, l, _ in CURRENTS if n == name)
    return min(p[0] for p in line) <= s <= max(p[0] for p in line)


def span(scene, id_):
    """How far along the lane a fight's volume begins and ends (docs/quality/encounters.json): its middle, less and plus half its depth."""
    box = next(b for b in lt.encounters()[scene] if b["id"] == id_)
    middle = station((box["x"], box["z"]))[0]
    return middle - box["depth"] / 2, middle + box["depth"] / 2


def side_on(name, s):
    """A current's middle at a point along the lane: its own line's side there."""
    line = next(l for n, l, _ in CURRENTS if n == name)
    for (s0, d0), (s1, d1) in zip(line, line[1:]):
        lo, hi = min(s0, s1), max(s0, s1)
        if lo <= s <= hi:
            return round(d0 + (d1 - d0) * (s - s0) / (s1 - s0), 1)
    raise ValueError(f"{name} doesn't run past {s}")


# ------------------------------------------------------------------ the routes
def shear():
    r = Checked(PREFIX + "32-aurora-shear", "Chapter 6: the chart for Hoarfell, the hull's warp, the first current, Idra's hail and tow, her lane, the fast lane, the descent, the way down", chapter=6)
    H = mh.H
    r.resume("ManyHands", "chapter5.complete", spawn=H["FreightHallSpawn"])
    r.people = {"Corra", "Dov", "Hadda", "Binna", "Ruva", "Teo", "Garo", "Dabb", "Maru", "Ona", "Pim", "Pell", "Abri", "Ilo", "DessikHeld", "JaskGarden"}
    r.lead("Taren")
    r.go("up to the chart table", [(24, -59.2), (12, -59.4), (0, -57), (0, -40), (0, -20), H["Crossing"]] + mh.SPINE_UP + [H["StairFoot"], H["StairTop"], H["ChartTalk"]], tolerance=0.4)
    r.look("read the chart for Hoarfell", 8, "hoar.charted")  # SelaHoarfellChart (staged by StageManyHands): starts The Long Sleep
    r.go("down to the hull pad", [H["StairTop"], H["StairFoot"], (0, 20)] + mh.SPINE_DOWN + [(0, -20), (0, -40), (0, -55), (0, -57), (5.2, -58.2), (5, -61)], tolerance=0.5)
    r.scene = "ManyHandsHull"
    r.add("launch to the hull", 4, buttons=["South"], expectedForm="Flight")
    r.add("release launch", 0.5)
    r.at, r.speed = mh.O["ArrivalAt"], FLIGHT_SPEED
    # Off the stern, south-west of the dock (StageManyHands): round the dock's pad, not over it.
    r.walk("south off the stern dock", -26, 1, -148, tolerance=3)
    r.walk("toward the warp to the Shear", -84, 1, -170, tolerance=3)
    r.brake = True
    r.walk("settle at the warp to the Shear", WARP_TO_SHEAR[0] + 2.6, 1, WARP_TO_SHEAR[1] + 2.6, tolerance=1)  # its reach is 5; its piece is solid
    r.brake = False
    r.scene = "AuroraShear"
    r.add("take the warp to the Aurora Shear", 4, buttons=["South"], expectedForm="Flight")
    r.add("release warp", 0.5)
    r.at = (0, 6)
    # C1 runs north from 26 along, 10 m to port, until Idra hails: ride it to the hail's line across the whole lane at 200.
    for s in (30, 70, 120, 170):
        r.fly(f"up the first current at {s}", s, side_on("The first current", s))
    r.fly("on up the first current until Idra hails", S["HailS"] + 20, side_on("The first current", S["HailS"] + 20), stop="ui:dialogue")
    r.add("Idra's hail", 3, until="ui:dialogue", expectedUi="dialogue")
    r.read("Idra's hail", 9, "hoar.idraHailed")  # IdraShearHail: the current turns, her line goes on
    # The tow carries the ship across to her lane by itself (CutterTow): hands off the stick until it lets go.
    r.wait("Idra's cutter tows the party across to her lane", 25, until="flag:hoar.towed", expectedFlag="hoar.towed")
    # Where the tow lets go: a step there, so a leg check knows the ship was carried and not flown.
    r.at = None
    r.fly("where the tow lets go", S["TowEndS"], S["TowEndSide"], tolerance=4)
    # Over to the short lane, which keeps to the middle, past the tender and Idra's call there. (Idra's lane once ran into the next stretch's starboard wall at the
    # bend at 560, ZoneKit.FlightCorridor running it 72 m back; the Shear's walls now run 15 m past each stretch and her lane swings out to 50 m: it is clear too.)
    for s, side in ((345, 10), (370, -9)):
        r.fly(f"across to the short lane at {s}", s, side)
    for s in (420, 470, 510):
        r.fly(f"down the short lane at {s}", s, side_on("The short lane", s))
    r.fly("on down the short lane until Idra calls it", S["LanesS"] + 12, side_on("The short lane", S["LanesS"] + 12), stop="ui:dialogue")
    r.add("Idra calls the short lane", 3, until="ui:dialogue", expectedUi="dialogue")
    r.read("Idra calls the short lane", 5, "hoar.tenderSeen")  # IdraShearLanes: across the whole lane
    r.fly("toward the tender", S["TenderS"] - 14, side_on("The short lane", S["TenderS"] - 14))
    # A fight in a current is a running fight down the lane (AuroraCurrent.Float: the Muster's craft, their markings and the shots all ride it as the ships do), so
    # every fight from here on ends wherever the current has carried it, and no leg after one may go back up a lane: against the fast lane a ship at the stick's
    # pace is carried backward. The first player run walked back up to waypoints a fight had carried it past, and timed out at each (November 14).
    r.fight_flight("Shear_Tender", rounds=4)  # carried down the short lane toward its foot (720), where still water lies before the fast lane's head
    r.fought_in("Shear_Tender", 720, "The short lane")
    # On down from wherever the tender fell (somewhere between its box and the short lane's foot): to a point in the fast lane no fight on the short lane reaches.
    r.fly("on to the fast lane", 790, side_on("The fast lane", 790))
    r.fly("down the fast lane at 815", 815, side_on("The fast lane", 815))
    r.fight_flight("Fast_0", rounds=4)
    # The fast lane's second wing and the descent's are each flown into from wherever the one before was cleared. Nothing carries a party that a chase has left out
    # of the current: the first player run sat 12 m to starboard of the fast lane's middle, a metre out of it, through both, locking at nothing (November 14).
    # A wing the current carried the last fight into is fought in the same rounds, and once cleared its leg ends at once; one already begun is flown into all
    # the same (its box's middle is at most 18 m away), against the current if it has to be: the leg has the seconds for it.
    r.fought_in("Fast_0", span("AuroraShear", "Fast_1")[1], "The fast lane")
    r.ride_into("Fast_1", across=6, along=-12)
    r.fight_flight("Fast_1", rounds=5)
    r.fought_in("Fast_1", span("AuroraShear", "Descent_0")[1], "The fast lane", "The descent")
    r.ride_into("Descent_0", across=12, along=-12)
    r.fight_flight("Descent_0", rounds=5)  # the descent ends at 1438, short of the way down: still water from there
    r.fought_in("Descent_0", 1438, "The descent")
    r.fly("on to the way down", S["LandS"] - 14, S["LandSide"])
    r.settle("over the way down", S["LandS"] - 4, S["LandSide"])  # its reach is 6.5; its piece is solid
    r.scene = "Lastlight"
    r.add("land at Lastlight", 6, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release landing", 0.5)
    r.at, r.speed = T["Arrival"], 4.5
    r.wait("first look at the ribs", 2, expectedFlag="hoar.idraHailed")
    r.write()


def town():
    r = Checked(PREFIX + "33-lastlight", "Chapter 6: the ribs, Old Tern, Idra and Hask at the pens, Hask's counter, Idra's plan, the Reach gate", chapter=6)
    r.resume("Lastlight", "hoar.idraHailed", spawn=T["Arrival"])
    r.town_people = {"Tern", "Hask", "IruShrine"}
    r.lead("Taren")
    # The arrival's crossing runs across the way north of the landing, z 12, 24 wide.
    r.walk("up from the landing until Sela looks up", 0, 0, 22, stop="ui:dialogue")
    r.crossing("the ribs overhead", 4, "hoar.ribsSeen")  # SelaLastlightArrival
    r.walk("on up the commons", 0, 0, 22)
    r.go("up to Tern's hall", [(-2, 34), (2, 46), (6, 54), (8, 58.6)], tolerance=0.4)
    r.talk("Old Tern", 12, flag="hoar.ternHeard")  # TernFirst
    # Idra and Hask at the pens: a crossing at the pen gate, 5 by 10, open once Tern has spoken, on the way from the commons to Hask.
    r.go("back down to the commons", [(6, 54), (2, 46), (-2, 34), (0, 22), (-12, 26)], tolerance=0.4)
    r.walk("toward the pens until Idra and Hask are heard", -20, 0, 25.8, stop="ui:dialogue")
    r.crossing("Idra and Hask at the pens", 9, "hoar.pensSeen")  # IdraPens: starts Cutter Down; Idra walks to the carving yard
    r.walk("on to Hask at the pen gate", -23.4, 0, 26.4, tolerance=0.4)
    # His first word ends on two choices; the first is his counter. Gel to the cap before the Reach.
    r.add("greet Hask", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release greet Hask", 0.3, expectedUi="dialogue")
    r.add("hear Hask out", 12, buttons=["South"], pulseSeconds=0.55, until="ui:options", expectedUi="options")  # HaskFirst
    r.add("release before his choices", 0.5)
    r.add("see what he trades", 10, buttons=["South"], pulseSeconds=1.0, until="ui:shop", expectedUi="shop")
    r.add("find the repair gel", 3, pickRow="Repair Gel", until="row:Repair Gel", expectedUi="shop")
    r.stock("buy repair gel up to the cap", "Repair Gel")
    r.add("leave Hask's counter", 0.5, buttons=["East"], expectedUi="world")
    r.add("release Hask's counter", 0.3)
    r.town_people |= {"IdraYard"}
    r.go("up to the carving yard", [(-20, 25.8), (-12, 26), (0, 22), (-2, 34), (-8, 42), (-12, 45.6)], tolerance=0.4)
    r.talk("Idra's plan", 11, flag="hoar.planTaken")  # IdraPlan
    gate = T["ReachGate"]
    r.go("west to the Reach gate", [(-8, 42), (-24, 42), (-40, 42), T["ReachSpawn"], (gate[0] + 3.5, gate[1])], tolerance=0.4)
    r.scene = "FrozenHost"
    r.add("through the Reach gate", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the Reach gate", 0.5)
    r.speed = COMBAT_SPEED
    r.write()


def errands():
    r = Checked(PREFIX + "34-lastlight-errands", "Chapter 6: back by the Reach gate, Iru and the skull lamps, the fourth rib's tally, out to the Reach again", chapter=6)
    r.resume("FrozenHost", "hoar.planTaken", speed=COMBAT_SPEED, spawn=F["Arrival"])
    r.lead("Taren")
    way = F["GateWay"]
    r.walk("back to the Reach gate", way[0], 0, way[1] + 3, tolerance=0.4)  # Reach gate — Lastlight, reach 3.6
    r.scene = "Lastlight"
    r.add("home through the Reach gate", 4, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release the Reach gate", 0.5)
    r.at, r.speed = T["ReachSpawn"], 4.5
    r.town_people = {"Tern", "Hask", "IruShrine", "IdraYard"}
    # The way to the shrine runs by the bath-house's cradle and south of its basin (TownZone.Ways; it once turned 0.4 m off the bath-house's south wall).
    shrine_way = [(18, 37), T["BathSpawn"], (31.5, 34.4), (37.2, 36.6), (39.6, 41), (42.4, 44)]
    r.go("east to the lamp shrine", [(-40, 42), (-24, 42), (-8, 42), (-2, 34), (8, 38)] + shrine_way, tolerance=0.4)
    r.talk("Iru at the shrine", 4)  # IruFirst
    r.talk("Iru's three lamps", 4)  # IruLampsOffer: starts The Skull Lamps
    # The climb: the ledge up the high rib-hall's south face (its points), the first lamp on its landing, the second under the first rib, the third on the knoll.
    ledge = [(x, z) for x, _, z in CLIMB]
    r.go("up the ledge to the first high lamp", [ledge[0], ledge[1], ledge[2]], tolerance=0.5)
    r.press_for("light the first high lamp", "hoar.skullLamp1")
    # The high hall's way (TownZone.Ways): through the second lamp's spot and on up the knoll to the third. (It was cut past the second lamp by its post and the
    # knoll's steep side, a floor path of 9.8 m from (52, 63) to (56.5, 65.5); the post now stands behind the lamp and the knoll rises over five metres.)
    r.go("up the ledge to the high rib-hall", [ledge[3], ledge[4], ledge[5], (55, 60.5), HIGH_LAMPS[1]], tolerance=0.5)
    r.press_for("light the second high lamp", "hoar.skullLamp2")
    r.go("across the high hall to the knoll", [(56, 64.8), HIGH_LAMPS[2]], tolerance=0.5)
    r.press_for("light the third high lamp", "hoar.skullLamp3")
    r.go("back down the climb", [ledge[5], ledge[4], ledge[3], ledge[2], ledge[1], ledge[0], (42.4, 44)], tolerance=0.5)
    r.talk("the shrine is lit", 2, flag="iru.lampsLit")  # IruLampsReturn
    r.go("to the fourth rib", shrine_way[::-1][1:] + [(8, 38), (-2, 34), (2, 46), (6, 54), (8, 58.6), (10.6, 58.2)], tolerance=0.4)
    r.look("look at the cuts in the rib", 2, "hoar.tallySeen")  # SelaRibTally
    gate = T["ReachGate"]
    r.go("west to the Reach gate", [(8, 58.6), (6, 54), (2, 46), (-2, 34), (-8, 42), (-24, 42), (-40, 42), T["ReachSpawn"], (gate[0] + 3.5, gate[1])], tolerance=0.4)
    r.scene = "FrozenHost"
    r.add("back through the Reach gate", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the Reach gate", 0.5)
    r.speed = COMBAT_SPEED
    r.write()


def host():
    r = Checked(PREFIX + "35-frozen-host", "Chapter 6: the White Reach, the herd, the whiteouts, the camp, the dig's three levels and rigs, to the dig cradle", chapter=6)
    r.resume("FrozenHost", "iru.lampsLit", speed=COMBAT_SPEED, spawn=F["Arrival"])
    r.lead("Taren")
    reach = HOST_WAYS[0]
    # Out of the gate's arch: the Reach's crossing, (0, 12), 36 wide.
    r.walk("out onto the Reach until Sela speaks", 0, 0, 20, stop="ui:dialogue")
    r.crossing("out on the Reach", 4, "hoar.reachEntered")  # SelaReachGate
    r.walk("into the spear field", 1, 0, 30)
    r.fight_ground("Reach_Stalkers")
    r.fought()
    r.go("over the main bridge", [(2, 46), (2, 50), (2, 54)], tolerance=0.6)
    # The herd comes through the gap as the party reaches it: walk, never run or dodge, within 7 m of a beast (RimeHerd startles).
    r.walk("toward the gap until Idra speaks", 2, 0, 60, stop="ui:dialogue")
    r.crossing("the herd in the gap", 5, "hoar.herdMet")  # IdraHerd
    r.hands_off("let the herd's head go by", 4)
    for i, (x, z) in enumerate([(4, 66), (4.5, 82), (5, 93), (5, 96.5)]):
        r.walk(f"walk through the herd {i + 1}/4", x, 0, z, tolerance=0.6)
    r.wait("past the herd", 0.5, expectedFlag="hoar.herdPassed")
    # The wind drops at the field north of the ridge: Idra's lesson, then the first whiteout. Into vent pocket 1 to wait it out.
    r.walk("on until the wind drops", 3, 0, 102, stop="ui:dialogue")
    r.crossing("the wind drops", 4, "hoar.whiteoutMet")  # IdraWhiteout
    r.go("into the first vent pocket", [(3, 100), (-12, 102.2), (-26, 104)], tolerance=0.8)
    r.wait("the first whiteout passes", 30, until="flag:hoar.whiteoutLifted", expectedFlag="hoar.whiteoutLifted")
    r.go("back to the walked line", [(-12, 102.2), (3, 100)], tolerance=0.8)
    # The stalkers' field brings the second whiteout as the party walks in.
    r.walk("into the stalkers' field", 5, 0, 114)
    r.fight_ground("Reach_Whiteout", rounds=5)
    r.fought()
    r.go("over the second bridge to the camp", [(6, 118), (6, 134), (6, 138), (8, 145)], tolerance=0.6)
    # The walked line only grazes the camp's north-west corner at (19, 157): step off it onto the trodden snow, where the motes are.
    r.walk("along the line to the camp", 18, 0, 156)
    r.walk("onto the camp's trodden snow", 21, 0, 154)
    r.fight_ground("Reach_Camp")
    r.fought()
    r.go("back to the line at the dig's head", [(18, 156)], tolerance=0.6)
    r.walk("past the rig's glass until Vessa speaks", 24, 0, 166, stop="ui:dialogue")
    r.crossing("Vessa on the loudhailer", 6, "hoar.vessaHeard")  # VessaLoudhailer
    r.walk("down the mouth ramp", 24, 0, 178)
    r.walk("into the upper gallery until Idra speaks", 30, 0, 184, stop="ui:dialogue")
    r.crossing("the first rank", 3, "hoar.ranksSeen")  # IdraDigGalleries
    # The upper gallery: the motes south of z 190 (the amber rank north of it wakes too), the rig at its east end.
    r.go("east along the upper gallery", [(30, 184), (47, 185.4)], tolerance=0.6)
    r.fight_ground("Dig_Motes")
    r.fought()
    r.go("to rig one", [(56, 185.9), F["RigOneHand"]], tolerance=0.4)
    r.press_for("run thaw rig one", "hoar.rig1")
    r.wait("plug one melts", 4)
    # Down ramp one to the second level: Vessa's offer is a crossing across the ramp's foot.
    r.go("down ramp one", [(58, 186), (72, 187)], tolerance=0.6)
    r.walk("onto the second level until Vessa speaks", 88, 0, 184, stop="ui:dialogue")
    r.crossing("Vessa's offer", 7, "hoar.offerHeard")  # VessaSecondLevel
    r.walk("into the second level", 96, 0, 186.6)
    r.fight_ground("Dig_Second")
    r.fought()
    r.go("to rig two", [(100, 188), F["RigTwoHand"]], tolerance=0.4)
    r.press_for("run thaw rig two", "hoar.rig2")
    r.wait("plug two and the rank's ice melt", 4)
    r.go("into the left gallery", [(100, 188), (92, 198), (91, 206)], tolerance=0.6)
    r.fight_ground("Dig_Rank")
    r.fought()
    r.go("down ramp two to the third level", [(90, 212), (100, 214), (112, 214), (118, 216)], tolerance=0.6)
    r.fight_ground("Dig_Third", rounds=5)
    r.fought()
    r.go("east along the third level", [(124, 218), (140, 222.4), (146, 224)], tolerance=0.6)
    r.walk("to rig three", F["RigThreeHand"][0], 0, F["RigThreeHand"][1] - 0.6, tolerance=0.4)
    r.press_for("run thaw rig three", "hoar.rig3")
    r.fight_ground("Dig_Rank3", rounds=5)  # the rank out of the north wall, the hounds out of their pen through the melted plug
    r.fought()
    r.go("down ramp three", [(146, 224), (158, 219), (166, 218.2)], tolerance=0.6)
    r.walk("on to the winch until Sela reads the slate", 174, 0, 218, stop="ui:dialogue")
    r.crossing("Vessa's slate", 6, "hoar.slateRead")  # SelaMusterSlate: walked into at x 170.5
    r.walk("to the dig cradle", F["DoorCradle"][0] + 1.6, 0, F["DoorCradle"][1] + 0.8, tolerance=0.4)
    r.save_at("repair and save at the dig cradle", "release the cradle")
    r.write()


def marshal():
    r = Checked(PREFIX + "36-host-marshal", "Chapter 6: Idra joins, the Marshal wakes, the Host Marshal, it sleeps, the winch up to Lastlight", chapter=6)
    door = F["DoorSpawn"]
    r.resume("FrozenHost", "hoar.slateRead", speed=COMBAT_SPEED, spawn=door)
    r.lead("Taren")
    # The bottom gallery's crossing (182, 218), 3 by 30, across the mouth: Idra joins as it closes, out in the partner's place (Sela rests).
    r.walk("toward the bottom gallery until Idra speaks", 184, 0, 218, stop="ui:dialogue")
    r.crossing("Idra joins", 6, "hoar.idraJoined")  # IdraJoins: <<joinParty Idra>>
    r.add("Idra is out", 1.5, expectedCharacter="Taren")
    r.lead("Taren")
    r.walk("into the passage until the Marshal steps out", 187.6, 0, 218, stop="ui:dialogue")
    r.crossing("the Marshal steps out", 4, "hoar.marshalSeen")  # IdraMarshalWakes: saves at Door
    # The door: the line from the checkpoint (177, 211) through the passage's middle (189.5, 218) into the gallery, 12.5 m from C, short of the first coupling.
    r.at = (187.6, 218)
    boss(r, "HostMarshal", "into the bottom gallery", 196, HOST_LEVELS["Bottom"], 221.64, spawn=door, rounds=10)
    r.add("reach the fallen hero after the Marshal", 16, navigate=True, approachPartner=True, tolerance=1.2, stopWhen="partyAlive", point=dict(x=0, y=0, z=0))
    r.add("both heroes ready after the Marshal", 6, until="partyAlive")
    r.crossing("the Marshal sleeps", 6, "hoar.marshalAsleep", wait=12)  # IdraMarshalSleeps (an AfterTalk, 3 s on)
    r.lead("Taren")
    r.fought()
    winch = F["WinchHand"]
    r.go("out to the loading winch", [(192, 218), (184, 218), (winch[0] + 1, winch[1] - 1.4)], tolerance=0.4)
    r.scene = "Lastlight"
    r.add("ride the winch up", 6, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release the winch", 0.5)
    r.at, r.speed = T["ReachSpawn"], 4.5
    r.wait("back in Lastlight", 1.5, expectedFlag="hoar.marshalAsleep")
    r.write()


def sleep():
    r = Checked(PREFIX + "37-long-sleep", "Chapter 6 with Taren and Idra out: Old Tern's long sleep, Sela's chart home, Idra's cutters at the dock", chapter=6)
    r.deployed = ("Taren", "Idra")
    r.hull = HULL_IDRA
    r.resume("Lastlight", "hoar.marshalAsleep", spawn=T["ReachSpawn"])
    r.town_people = {"Tern", "Hask", "IruShrine"}
    r.lead("Taren")
    r.go("to Tern's hall", [(-40, 42), (-24, 42), (-8, 42), (-2, 34), (2, 46), (6, 54), (8, 58.6)], tolerance=0.4)
    r.talk("how the young ones sleep", 16, flag="hoar.sleepLearned")  # TernLongSleep
    r.chart("ManyHands", "ManyHands", spawn=mh.H["Arrival"], form="Natural")
    r.lead("Taren")
    # The lift figures: a crossing on the dock, (0, -54.6), 16 by 3, walked into stepping off the pads (StageManyHands).
    r.walk("up off the pads until Idra reads the figures", 0, 0, -55, stop="ui:dialogue")
    r.crossing("Idra's cutters come home", 12, "chapter6.complete")  # IdraLiftFigures
    r.wait("chapter six closes", 3, expectedFlag="hoar.liftFixed")
    r.write()


if __name__ == "__main__":
    check_docs()
    shear()
    town()
    errands()
    host()
    marshal()
    sleep()
    print("HOARFELL_ROUTES_OK")
