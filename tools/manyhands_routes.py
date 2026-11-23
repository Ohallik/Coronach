"""Writes chapter 5's replay routes (chapter-27 to chapter-31) for Many Hands and its hull from the builders' own places, with chapter 4's close before them.

Places are read out of the builders themselves (ManyHandsBuilder.City.cs: the named places, the scopes, the folk, the walkers and the declared Ways;
ManyHandsBuilder.Hull.cs: the joins, the door, the dock, the checkpoint, the drones and the cover; WendmireBuilder.Driftreed.cs: the town's places and its
TownWays) and checked against the coordinates tables of their map docs (docs/maps/ManyHands.md, ManyHandsHull.md, Driftreed.md) before anything is
written: a place that has drifted from its doc stops the run. Nobody has run these. Every straight leg is checked here before it is written: on the deck
against the floor the city's own height function makes (the gallery's terrace, the hold, its rims and walks, the hall's walls), the furniture the builder
stands solid, the named and the townsfolk; in flight against the hull's cover, its dock pad and the harbour line; in Driftreed against the declared ways
and the people on them. ManyHandsRouteTests.EveryWalkOnTheChapterFiveRoutesFollowsFloorABodyCanCross then checks every leg on the built zones' navigation
and colliders.

The chain. Chapter 4's routes stop at chapter-23-drowned-waystation, saved under Maru's lamp at the portico (wend.lampCarried, spawn Door): nothing played
chapter 4's close (Ona's decision, Abri's call, chapter4.complete), and Many Hands' dock off Tallow is hidden until it is told. chapter-27 plays it.
  chapter-27-driftreed-decision  chapter 4's close: Sela's chart from the Waystation to Driftreed, Ona decides, Abri's call on the stage walk, the chart to
                                 Tallow, the launch, and the new dock off the approach into Many Hands
  chapter-28-many-hands          Abri's welcome, Binna's vent, Ruva, Pell and the three gallery lamps, the chart table, Maru and the running rings, Corra and
                                 Dov (gel to the cap), Ilo off the courier, Dessik's claim on the radio, the launch to the hull
  chapter-29-hull-of-many-hands  the claim haulers over the raft bay, Hulljack's four feet (boss, part one, its door retried), the way in after it
  chapter-30-freight-hall        Dessik in the hall, Dessik Varn (boss, part two, its door retried) and his yield, the breach and the captain, Meret, the
                                 dinner's six talks, Meret at the galley door, the chart read on the lamp gallery (chapter5.complete)
  chapter-31-many-hands-errands  side content: the dock door, the kettles, the claims ledger, the stowaway's fare, and a save at the riggers' cradle

  python tools/manyhands_routes.py
"""
import math
import re
from pathlib import Path

from lantern_routes import COMBAT_SPEED, FLIGHT_SPEED, Route
from xylos_routes import box_distance, segment_distance

ROOT = Path(__file__).resolve().parent.parent
WORLD = ROOT / "Lattice" / "Assets" / "_Project" / "Scripts" / "Editor" / "World"
MAPS = ROOT / "docs" / "maps"
PREFIX = "chapter-"
HULL = 1.78  # HeroCollision.HullRadius("Taren"), the larger of chapter 5's two
BODY = 0.55  # the bake's agent radius
NUMBER = r"(-?\d*\.?\d+)f?"


# ------------------------------------------------------------------ reading the builders
def vectors(source):
    """Every `Name=new(x,z)` in a builder's source, by name: the named places a builder declares on one line or many."""
    return {m.group(1): (float(m.group(2)), float(m.group(3))) for m in re.finditer(r"\b([A-Z]\w*)=new\(" + NUMBER + "," + NUMBER + r"\)", source)}


def block(source, start):
    """The text of an initializer that begins at `start` (an array or a table), to its matching close."""
    i = source.index(start) + len(start)
    depth, j = 0, i
    while True:
        c = source[j]
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                return source[i:j + 1]
        j += 1


def points(text, named):
    """The points of a `new[]{...}` written with literals and the builder's own names, in order."""
    out = []
    for m in re.finditer(r"new(?: Vector2)?\(\s*" + NUMBER + r"\s*,\s*" + NUMBER + r"\s*\)|\b([A-Z][A-Za-z]+)\b(?!\s*[(.])", text):
        if m.group(3):
            if m.group(3) in named:
                out.append(named[m.group(3)])
        else:
            out.append((float(m.group(1)), float(m.group(2))))
    return out


def arrays(text, named):
    """A Vector2[][] initializer as a list of point lists."""
    inner = text[1:-1]
    return [points(part, named) for part in re.findall(r"new\[\]\{(.*?)\}", inner)]


CITY_SOURCE = (WORLD / "ManyHandsBuilder.City.cs").read_text(encoding="utf-8")
HULL_SOURCE = (WORLD / "ManyHandsBuilder.Hull.cs").read_text(encoding="utf-8")
REED_SOURCE = (WORLD / "WendmireBuilder.Driftreed.cs").read_text(encoding="utf-8")
H = vectors(CITY_SOURCE)
SCOPES = points(block(CITY_SOURCE, "Scopes="), H)
WAYS = arrays(block(CITY_SOURCE, "internal static readonly Vector2[][] Ways="), H)
FOLK = [(float(m.group(2)), float(m.group(3))) for m in re.finditer(r'\("(\w+)",' + NUMBER + "," + NUMBER + "," + NUMBER + r",new\(", block(CITY_SOURCE, "string doing)[] Folk="))]
CHILDREN = [(float(m.group(2)), float(m.group(3))) for m in re.finditer(r'\("(\w+)",' + NUMBER + "," + NUMBER + "," + NUMBER + r",new\(", block(CITY_SOURCE, "Color tint)[] Children="))]
ERRANDS = [points(m.group(1), H) for m in re.finditer(r"new\[\]\{(.*?)\}", block(CITY_SOURCE, "string name)[] Errands="))]
O = vectors(HULL_SOURCE)
JOINS = points(block(HULL_SOURCE, "Joins="), O)
COVER = [(int(m.group(1)), m.group(2), (float(m.group(3)), float(m.group(4))), (float(m.group(5)), float(m.group(6))) if m.group(5) else None)
         for m in re.finditer(r'\((\d),"(\w+)",new\(' + NUMBER + "," + NUMBER + r"\),(?:new\(" + NUMBER + "," + NUMBER + r"\)|default)\)", block(HULL_SOURCE, "Vector2 to)[] Cover="))]
LIMITS = tuple(float(re.search(r"\b" + k + "=" + NUMBER, HULL_SOURCE).group(1)) for k in ("XMin", "XMax", "ZMin", "ZMax"))
R = vectors(REED_SOURCE)
TOWN_WAYS = arrays(block(REED_SOURCE, "static readonly Vector2[][] TownWays="), R)


# ------------------------------------------------------------------ the map docs: what each place is said to be
def doc_rows(name):
    """Each row of a map doc's coordinates table: its first cell and every (x, z) or (x, y, z) in its second, as (x, z)."""
    text = (MAPS / name).read_text(encoding="utf-8")
    section = text[text.index("## Coordinates"):]
    section = section[:section.index("\n## ", 4)] if "\n## " in section[4:] else section
    rows = []
    for line in section.splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 2 or cells[0].startswith("---") or cells[0] == "Place":
            continue
        found = re.findall(r"\((-?[\d.]+), (-?[\d.]+)(?:, (-?[\d.]+))?\)", " ".join(cells[1:]))
        rows.append((cells[0], [(float(a), float(c if c else b)) for a, b, c in found]))
    return rows


def drifted(doc, expected):
    """Every place whose builder's number isn't its doc's: (row label prefix, the places in the order the row gives them)."""
    rows = doc_rows(doc)
    wrong = []
    for label, places in expected:
        row = next((given for first, given in rows if first.startswith(label)), None)
        if row is None:
            wrong.append(f"{doc}: no row '{label}' in its coordinates table")
            continue
        for i, place in enumerate(places):
            if place is None:
                continue
            if i >= len(row) or math.dist(row[i], place) > 0.25:
                wrong.append(f"{doc} '{label}': the builder has {place}, the doc {row[i] if i < len(row) else 'nothing'}")
    return wrong


def check_docs():
    a = lambda n: H[n]
    welcome = (H["Arrival"][0] + 2, H["Arrival"][1] + 2.6)  # ManyHandsBuilder.Hands.Story: the crossing a step from the arrival
    walks = [p for w in points(block(CITY_SOURCE, "Vector2 to)[] Walks="), H) for p in [w]]
    city = [("**Arrival**", [a("Arrival"), a("TallowPad")]), ("Abri's welcome", [welcome]), ("Keeper Abri on the dock", [a("AbriDock")]), ("The dock door", [a("DoorChalk")]),
            ("Hull pad", [a("HullPad")]), ("Ilo on the dock", [a("IloDock")]), ("Meret by her cutter", [a("MeretDock"), a("MeretCutter")]),
            ("Freight yard", [a("FreightHallSpawn")]), ("Freight hall door", [a("HallDoor")]), ("Dessik's talk", [a("HallTalk")]), ("**F**", [a("F")]),
            ("Dessik in custody", [a("DessikHeld")]), ("The riggers' cradle", [a("Cradle")]), ("The gantry", [a("Gantry")]), ("**The crossing**", [a("Crossing"), a("ArcadeSpawn")]),
            ("Corra at her stall", [a("Corra")]), ("Dov at his counter", [a("Dov")]), ("Hadda at the pump-and-press", [a("Hadda"), a("BenchAt")]),
            ("Hadda's mending frame", [a("ArcadeCradle")]), ("The arcade clock", [a("Clock")]), ("The mooring rail", [a("RingsAt")]), ("Galley door", [a("GalleyDoor")]),
            ("Binna at the hatch", [a("Binna")]), ("The dinner table", [a(n) for n in ("MeretSeat", "RuvaSeat", "JaskSeat", "TeoSeat", "IloSeat", "AbriSeat", "MaruWall")]),
            ("Ruva in the garden", [a("Ruva")]), ("Teo on the garden deck", [a("Teo")]), ("Garo; Dabb", [a("Garo"), a("Dabb")]), ("Behind the seed racks", [a("JaskLook")]),
            ("Maru on the ferry raft", [a("Maru")]), ("Ona at her bench", [a("Ona"), a("Pim")]), ("Raft walks", walks), ("Stair foot", [a("StairFoot"), a("StairTop")]),
            ("Pell on the gallery", [a("Pell")]), ("The three scopes", SCOPES), ("The chart table", [a("ChartTalk")]), ("Abri's desk", [a("Abri"), a("Ilo")]),
            ("The captain at the sealed cabins", [a("Patrol"), a("MeretCabins")])]
    hull = [("Arrival", [O["ArrivalAt"]]), ("Dock home", [O["DockAt"]]), ("Claim haulers", [O["DronesAt"]]), ("Checkpoint", [O["CheckpointAt"]]), ("The frame on show", [None]),
            ("J1 raft bay join", [JOINS[0]]), ("J2 cabins join", [JOINS[1]]), ("J3 garden join", [JOINS[2]]), ("J4 freight hall join", [JOINS[3]]), ("Roof door", [O["DoorAt"]])]
    reed = [("Arrival", [R["ReedArrival"]]), ("Landing pad", [R["ReedPad"]]), ("Stage walk (F1)", [(0, 18), (0, 34), R["AbriCallAt"]]), ("Market middle", [R["MarketAt"]]),
            ("Hall walk (F2)", [(0, 58), (0, 75)]), ("Ona at the hall door", [R["OnaAt"]]), ("Pim's bench", [R["PimAt"]])]
    wrong = drifted("ManyHands.md", city) + drifted("ManyHandsHull.md", hull) + drifted("Driftreed.md", reed)
    if wrong:
        raise SystemExit("a place here isn't where its map doc says:\n" + "\n".join(wrong) + "\nMANYHANDS_ROUTES_FAILED")


# ------------------------------------------------------------------ Many Hands' deck, as ManyHandsBuilder.Hands.Height makes it
GALLERY_TOP, HOLD_BED, CHANNEL_BED, RIM_TALL, RIM_WIDE, WALK_HALF, HALL_WALL, LOW_WALL, STORES_ROOF = 3.6, -2.6, -1, .5, .9, 1.2, 5.5, 1.1, 4.6
DOOR_FROM, DOOR_TO = 28, 36
RAFTS = [((-26, -24), (6.5, 5), 1.5), ((-50, -38), (10, 6), 2), ((-58, -20), (9, 4.5), 2)]
WALKS = points(block(CITY_SOURCE, "Vector2 to)[] Walks="), H)
WALKS = [(WALKS[i], WALKS[i + 1]) for i in range(0, len(WALKS), 2)]


def smooth(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def outside(p, x0, x1, z0, z1):
    return max(max(x0 - p[0], p[0] - x1), max(z0 - p[1], p[1] - z1))


def inside(p, x0, x1, z0, z1, soft):
    return 1 - smooth(-soft, 0, outside(p, x0, x1, z0, z1))


def round_box(p, c, half, r):
    dx, dz = abs(p[0] - c[0]) - half[0] + r, abs(p[1] - c[1]) - half[1] + r
    return math.hypot(max(dx, 0), max(dz, 0)) + min(max(dx, dz), 0) - r


def hold(p):
    return round_box(p, (-45, -30.5), (30.5, 19), 4)


def raft_land(p):
    return min(round_box(p, c, half, r) for c, half, r in RAFTS)


def opening(p):
    best = 0
    for a, b in WALKS:
        length = math.dist(a, b)
        d = ((b[0] - a[0]) / length, (b[1] - a[1]) / length)
        reach = RIM_WIDE + 1.6
        for s, e in (((a[0] + d[0] * .5, a[1] + d[1] * .5), (a[0] - d[0] * reach, a[1] - d[1] * reach)), ((b[0] - d[0] * .5, b[1] - d[1] * .5), (b[0] + d[0] * reach, b[1] + d[1] * reach))):
            best = max(best, 1 - smooth(WALK_HALF, WALK_HALF + .25, segment_distance(p, s, e)))
    return best


def height(p):
    y = GALLERY_TOP * inside(p, -24, 24, 38, 70, .3)
    if -3.4 < p[0] < 3.4 and 23 < p[1] < 39:
        y = max(y, GALLERY_TOP * max(0.0, min(1.0, (p[1] - 24) / 14)) * inside(p, -3, 3, 18, 42, .3))
    y = max(y, HALL_WALL * inside(p, 11, 53, -17, -15.5, .25), HALL_WALL * inside(p, 51, 53, -54, -15.5, .25))
    if not DOOR_FROM < p[0] < DOOR_TO:
        y = max(y, LOW_WALL * inside(p, 11, 53, -54, -52.5, .25))
    y = max(y, LOW_WALL * inside(p, 11, 12.5, -54, -15.5, .25), STORES_ROOF * inside(p, 53, 90, -80, -24, .3))
    h = hold(p)
    if h < RIM_WIDE + .2:
        if h >= 0:
            y = max(y, RIM_TALL * smooth(0, .25, h) * (1 - smooth(RIM_WIDE - .3, RIM_WIDE, h)) * (1 - opening(p)))
        else:
            raft = raft_land(p)
            bank = HOLD_BED * smooth(0, .7, -h)
            on_raft = RIM_TALL * smooth(-RIM_WIDE, -RIM_WIDE + .3, raft) * (1 - opening(p)) if raft < 0 else HOLD_BED * smooth(0, .7, raft)
            y = max(bank, on_raft)
    channel = outside(p, 30, 76, 19.5, 22.5)
    if channel < 0:
        y = y + (CHANNEL_BED - y) * smooth(0, .5, -channel)
    elif channel < .7:
        y = max(y, .45 * smooth(0, .2, channel) * (1 - smooth(.45, .7, channel)))
    return y


def on_walk(p, room=0.0):
    return any(segment_distance(p, a, b) <= WALK_HALF - room for a, b in WALKS)


def deck_level(p):
    """The floor a body's middle stands on here, or None: the deck (0), the gallery (3.6), the stair between, or a walk's plank over the hold. A body's
    width all round must be the same floor (a rim, a wall or the hold's edge within it is no floor)."""
    if on_walk(p, BODY):
        return 0.0
    y = height(p)
    stair = -3 + BODY < p[0] < 3 - BODY and 23 < p[1] < 39
    if stair:
        return y
    level = 0.0 if abs(y) < .12 else GALLERY_TOP if abs(y - GALLERY_TOP) < .12 else None
    if level is None:
        return None
    for dx, dz in ((BODY, 0), (-BODY, 0), (0, BODY), (0, -BODY), (BODY * .7, BODY * .7), (-BODY * .7, BODY * .7), (BODY * .7, -BODY * .7), (-BODY * .7, -BODY * .7)):
        q = (p[0] + dx, p[1] + dz)
        if not (on_walk(q) or abs(height(q) - level) < .3 or (-3 < q[0] < 3 and 23 < q[1] < 39)):
            return None
    return level


# What the city builder stands solid that a body meets (ManyHandsBuilder.CityParts/CityDetail: Solid and Block), as (label, centre, yaw, half x, half z).
def city_solids():
    s = []
    for z in (7.5, -1, -9.5):
        s.append((f"the galley's long table at z {z}", (65.5, z), 0, 8, .6))
        for side in (-1.05, 1.05):
            s.append((f"a bench of the galley's table at z {z}", (65.5, z + side), 0, 7.7, .18))
    s += [("Binna's counter", (66, 14), 0, 6.5, .55), ("the galley's hatch wall", (65.5, 17.2), 0, 12.5, 1), ("the galley's south counter", (65.5, -15.4), 0, 12.5, .7),
          ("the galley's west wall, south", (53.6, -10.4), 0, .6, 5.7), ("the galley's west wall, north", (53.6, 13.6), 0, .6, 3)]
    s += [("the chart table", H["ChartTable"], 0, 1.7, 1.1)] + [(f"scope {i + 1}", at, 0, .25, .25) for i, at in enumerate(SCOPES)]
    s += [("Abri's desk", H["AbriDesk"], 0, 1, .45), ("Ilo's desk", H["IloDesk"], 0, 1, .45)]
    for z, x0, x1 in ((25.1, -76, -55), (25.1, -49, -30), (35.1, -76, -55), (35.1, -49, -30), (45.1, -76, -55), (45.1, -49, -30), (55.6, -76, -55), (55.6, -49, -30)):
        s.append((f"the cabin row at z {z}", ((x0 + x1) / 2, z), 0, (x1 - x0) / 2, 2 if z > 50 else 1.5))
    s += [("the dock door's west post", (-8.1, -53.5), 0, .6, .7), ("the dock door's east post", (8.1, -53.5), 0, .6, .7), ("the dock door's leaf", (12, -55.1), 0, 3.8, .2)]
    for x, z, yaw, high in ((-73, -60, 0, 3), (-69.6, -60, 0, 2), (-66.2, -60, 0, 1), (-73, -53, 90, 2), (-58, -62.6, 90, 2), (-50, -62.6, 90, 1), (-44, -56, 0, 2), (-40.6, -56, 0, 3),
                            (39.8, -63, 90, 2), (46.4, -63.6, 90, 1), (48.6, -58, 0, 3), (9.2, -47, 0, 3), (9.2, -40.6, 0, 3), (9.2, -26.6, 0, 2), (9.2, -20.2, 0, 2)):
        s.append((f"a container stack at ({x}, {z})", (x, z), yaw, 1.25, 3.1))
    for x, z, yaw, high in ((16.4, -20.6, 0, 3), (47.6, -20.4, 90, 2), (47.6, -49.4, 0, 3), (-11.6, -38.2, 90, 2), (-11.8, -17.4, 0, 2), (40.4, -57.4, 0, 2), (-33.6, -55.2, 20, 3), (-70, -54.4, 0, 2)):
        s.append((f"a pallet stack at ({x}, {z})", (x, z), yaw, .65, .55))
    s += [("the loader at (44.4, -57.2)", (44.4, -57.2), -90, .8, 1.6), ("the loader at (-29.6, -55.6)", (-29.6, -55.6), 70, .8, 1.6)]
    s += [("the gantry's west leg", (-11.2, H["Gantry"][1]), 0, .45, .45), ("the gantry's east leg", (9.4, H["Gantry"][1]), 0, .45, .45)]
    s += [(f"Marrow-{b + 1}'s bed", (36 + b * 16, 40), 0, 3, 13.5) for b in range(3)]
    s += [("the arcade's west frontage", ((-36 - 8.6) / 2, 14.6), 0, (36 - 8.6) / 2, .7), ("the arcade's east frontage", ((8.6 + 53) / 2, 14.6), 0, (53 - 8.6) / 2, .7)]
    s += [("Ona's weaving bench", (H["Ona"][0], H["Ona"][1] - 2.3), 0, 1.4, .5), ("Maru's tide board", (H["TideBoard"][0], H["TideBoard"][1] + .3), 0, 5, .15)]
    for x, z, mats in ((-61, -17.4, 5), (-54.6, -22.2, 3), (-70, 2.6, 6), (-50, 9.6, 4), (-40, -3.6, 3)):
        s.append((f"a drying rack at ({x}, {z})", (x, z), 0, mats * 1.3 / 2, .2))
    for x, z, yaw in ((-22.6, 49, 90), (-22.6, 43.6, 90), (21, 44.6, -90), (-7.6, 56.6, 0), (6.6, 56.6, 0)):
        s.append((f"a lamp rack at ({x}, {z})", (x, z), yaw, 1.8, .3))
    posts = [(-6.4, z) for z in (-49, -37, -25)] + [(6.6, z + 6) for z in (-49, -37, -25)] + [(15, -19.6), (48.4, -19.6), (48.4, -50), (15, -50)]
    posts += [(x, 12.4) for x in range(-30, 49, 13)] + [(55.2, 14.8), (75.6, 12.4), (56.6, -6), (75.6, -12.6)] + list(points(block(CITY_SOURCE, "GalleryLamps="), H))
    s += [(f"a lamp post at {p}", p, 0, .15, .15) for p in posts]
    # Driftreed's pulse lamps, keeping their count over the water: the 3.2 m PulseBeacon kit piece (landed), its foot taken as a metre across.
    s += [(f"a pulse lamp at {p}", p, 0, .5, .5) for p in [(-21, -20.2), (-55.6, -36.4), (-64.6, -17.6), (-50.4, 4.6), (-66, 10.6), (-30.4, -6.6)]]
    return s


CITY_SOLIDS = city_solids()


def scene_solids(scene, skip=()):
    """Every solid box a body meets in a built scene (Assets/_Project/Scenes/<scene>.unity), as (label, centre, yaw, half x, half z): each BoxCollider that isn't a
    trigger and stands between ankle and head over the floor, with its object's name. The scatter's props are drawn from a seeded generator whose keep-off rule
    measures a prop's centre only, so a wide one reaches a way: what was built is the judge. Read, never written. Objects named in `skip` (and their children)
    are left out: seals that open, walkers who move."""
    text = (ROOT / "Lattice" / "Assets" / "_Project" / "Scenes" / (scene + ".unity")).read_text(encoding="utf-8")
    docs = {m.group(2): (m.group(1), m.group(3)) for m in re.finditer(r"^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- |\Z)", text, flags=re.M | re.S)}
    tr = {}
    for fid, (kind, body) in docs.items():
        if kind != "4":
            continue
        found = [re.search(pattern, body) for pattern in (r"m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", r"m_LocalRotation: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}",
                                                         r"m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", r"m_Father: \{fileID: (\d+)\}", r"m_GameObject: \{fileID: (\d+)\}")]
        if all(found):
            tr[fid] = (found[3].group(1), tuple(map(float, found[0].groups())), tuple(map(float, found[1].groups())), tuple(map(float, found[2].groups())), found[4].group(1))
    # A name with a colon in it is saved quoted ('Arrivals: ...', a quote inside doubled): unquote it, or a skip by prefix never matches.
    names = {fid: re.sub(r"^'(.*)'$", lambda q: q.group(1).replace("''", "'"), re.search(r"m_Name: (.*)", body).group(1).strip())
             for fid, (kind, body) in docs.items() if kind == "1" and "m_Name: " in body}
    by_object = {v[4]: t for t, v in tr.items()}

    def world(fid):
        parent, pos, rot, scale, _ = tr[fid]
        yaw = math.degrees(2 * math.atan2(rot[1], rot[3]))
        if parent not in tr:
            return (pos[0], pos[1], pos[2]), yaw, scale
        (px, py, pz), pyaw, ps = world(parent)
        r = math.radians(pyaw)
        lx, ly, lz = pos[0] * ps[0], pos[1] * ps[1], pos[2] * ps[2]
        return (px + lx * math.cos(r) + lz * math.sin(r), py + ly, pz - lx * math.sin(r) + lz * math.cos(r)), pyaw + yaw, (ps[0] * scale[0], ps[1] * scale[1], ps[2] * scale[2])

    out = []
    for fid, (kind, body) in docs.items():
        if kind != "65" or re.search(r"m_IsTrigger: 1", body) or re.search(r"m_Enabled: 0", body):
            continue
        holder = by_object.get(re.search(r"m_GameObject: \{fileID: (\d+)\}", body).group(1))
        chain, label, skipped = holder, None, False
        while chain in tr:
            name = names.get(tr[chain][4], "")
            label = label or name
            if any(name.startswith(k) for k in skip):
                skipped = True
            chain = tr[chain][0]
        if holder is None or skipped:
            continue
        size = tuple(map(float, re.search(r"m_Size: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", body).groups()))
        centre = tuple(map(float, re.search(r"m_Center: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", body).groups()))
        (x, y, z), yaw, sc = world(holder)
        r = math.radians(yaw)
        cx, cz = x + centre[0] * sc[0] * math.cos(r) + centre[2] * sc[2] * math.sin(r), z - centre[0] * sc[0] * math.sin(r) + centre[2] * sc[2] * math.cos(r)
        cy, hy = y + centre[1] * sc[1], abs(size[1] * sc[1]) / 2
        out.append((f"{label} at ({cx:.1f}, {cz:.1f})", (cx, cz), yaw, abs(size[0] * sc[0]) / 2, abs(size[2] * sc[2]) / 2, cy - hy, cy + hy))
    return out


# What a body meets on the deck as built: everything solid between ankle and head over the floor there, less the seals the story opens (the cargo door from the
# frame's arrival, the hall's seal outside the fight) and the walkers, who move.
# What comes aboard after chapter 5 (ManyHandsBuilder.Arrivals.cs: every root named "Arrivals: ...", saved switched off and shown from a later chapter's flag) is never on these routes.
_SKIP = ("Freight hall cargo door", "Freight hall seal", "Arrivals: ") + tuple(m.group(1) for m in re.finditer(r'(?:true|false),"([^"]+)"\)', block(CITY_SOURCE, "string name)[] Errands=")))
BUILT_CITY = [(label, c, yaw, hx, hz) for label, c, yaw, hx, hz, y0, y1 in scene_solids("ManyHands", _SKIP) if y0 < height(c) + 1.8 and y1 > height(c) + 0.3 and hx < 30 and hz < 30]
NAMED = {k: H[k] for k in ("AbriDock", "IloDock", "MeretDock", "DessikHeld", "Corra", "Dov", "Hadda", "Binna", "MaruWall", "Ruva", "Teo", "Garo", "Dabb", "JaskGarden", "Maru", "Ona",
                           "Pim", "JaskFerry", "Patrol", "MeretCabins", "Pell", "Abri", "Ilo", "JaskLamps", "MeretSeat", "RuvaSeat", "JaskSeat", "TeoSeat", "IloSeat", "AbriSeat",
                           "GalleyDoor", "JaskHid")}


# ------------------------------------------------------------------ the hull, at the plane
def hull_solids():
    """What stands solid at the flight plane round the ship (ManyHandsBuilder.Outside.CoverPiece, the dock): each mast a 1.6 m block, each boom a 1.6 m girder
    from its pedestal (3 m) to its tip, each cargo tower 3.2 by 13, and the dock's 5 m pad, its box run up through the plane."""
    boxes = [("the dock pad", O["DockAt"], 0, 2.5, 2.5)]
    for join, kind, at, to in COVER:
        if kind == "mast":
            boxes.append((f"J{join + 1}'s mast", at, 0, .8, .8))
        elif kind == "boom":
            mid = ((at[0] + to[0]) / 2, (at[1] + to[1]) / 2)
            yaw = math.degrees(math.atan2(to[0] - at[0], to[1] - at[1]))
            boxes += [(f"J{join + 1}'s boom", mid, yaw, .8, math.dist(at, to) / 2), (f"J{join + 1}'s boom pedestal", at, 0, 1.5, 1.5)]
        else:
            boxes.append((f"J{join + 1}'s cargo tower", at, 0, 1.6, 6.5))
    return boxes


HULL_SOLIDS = hull_solids()
# Tallow Approach at the plane: the warps that stand in it (WorldBuilder.Warp's 2 m piece) and the dock off it that takes a ship in to Many Hands
# (ManyHandsBuilder.StageTallowDock, (0, -44), reach 5).
APPROACH_SOLIDS = [("the Xylos warp", (11, -37), 0, 1, 1), ("the Wendmire warp", (-11, -37), 0, 1, 1), ("the dock to Many Hands", (0, -44), 0, 1, 1)]


class Checked(Route):
    """A Route that refuses a straight leg the driver couldn't cross: off the deck or into what the city builder stands solid, through someone, into the
    hull's cover or its harbour line, off Driftreed's declared ways. A fight's chase leaves the party somewhere unknown, so the leg after it is checked from
    its end only. `people` are the named who stand at their places while the leg is walked."""

    def __init__(self, *args, **kw):
        super().__init__(*args, **kw)
        self.problems = []
        self.people = set()

    def legs(self, to):
        start = self.at
        if start is None:
            return [to]
        steps = max(1, int(math.dist(start, to) / 0.4))
        return [(start[0] + (to[0] - start[0]) * i / steps, start[1] + (to[1] - start[1]) * i / steps) for i in range(steps + 1)]

    def checks(self):
        """The leg check for each scene a route here walks or flies."""
        return {"ManyHands": self.check_city, "ManyHandsHull": self.check_hull, "TallowApproach": self.check_approach, "Driftreed": self.check_reed}

    def walk(self, name, x, y, z, tolerance=0.8, seconds=None, stop=None, buttons=None):
        check = self.checks().get(self.scene)
        if check:
            check(name, (x, z))
        if self.scene == "ManyHands" and y == 0:
            level = deck_level((x, z))
            y = round(level if level is not None else height((x, z)), 2)
        super().walk(name, x, y, z, tolerance, seconds, stop, buttons)

    def check_city(self, name, to):
        for p in self.legs(to):
            if deck_level(p) is None:
                self.problems.append(f"{self.name}: '{name}' leaves the deck at ({p[0]:.1f}, {p[1]:.1f}): floor {height(p):.2f} (a rim, a wall, the hold or the gallery's edge)")
                return
            hit = next((label for label, c, yaw, hx, hz in CITY_SOLIDS if box_distance(p, c, yaw, hx, hz) < BODY), None)
            hit = hit or next((label for label, c, yaw, hx, hz in BUILT_CITY if box_distance(p, c, yaw, hx, hz) < BODY + .15), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' walks into {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return
            # Every named person's place is a hole in the baked floor whether or not they stand there at the time (Abri's place on the dock is one long after she
            # has gone up to her desk): the bake reads their bodies where the builder stood them.
            hole = next((n for n, at in NAMED.items() if math.dist(p, at) < .35 + BODY + .1), None)
            if hole:
                self.problems.append(f"{self.name}: '{name}' walks over {hole}'s place at ({p[0]:.1f}, {p[1]:.1f}), a hole in the floor's navigation")
                return
            who = next((n for n in self.people if math.dist(p, NAMED[n]) < 1.3), None)
            if who:
                self.problems.append(f"{self.name}: '{name}' walks through {who} at ({p[0]:.1f}, {p[1]:.1f})")
                return
            folk = next((f for f in FOLK + CHILDREN if math.dist(p, f) < .35 + BODY + .15), None)
            if folk:
                self.problems.append(f"{self.name}: '{name}' walks through a townsperson at {folk}")
                return

    def check_hull(self, name, to):
        x0, x1, z0, z1 = LIMITS
        for p in self.legs(to):
            if not (x0 + HULL + 1 < p[0] < x1 - HULL - 1 and z0 + HULL + 1 < p[1] < z1 - HULL - 1):
                self.problems.append(f"{self.name}: '{name}' flies into the harbour line at ({p[0]:.1f}, {p[1]:.1f})")
                return
            hit = next((label for label, c, yaw, hx, hz in HULL_SOLIDS if box_distance(p, c, yaw, hx, hz) < HULL + .8), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' flies within a hull of {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def check_approach(self, name, to):
        for p in self.legs(to):
            hit = next((label for label, c, yaw, hx, hz in APPROACH_SOLIDS if box_distance(p, c, yaw, hx, hz) < HULL + .8), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' flies within a hull of {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def check_reed(self, name, to):
        """Driftreed's rafts are rimmed and walked by their declared ways (TownWays): a leg keeps to them, and off the people who stand on them."""
        for p in self.legs(to):
            off = min(min(segment_distance(p, w[i - 1], w[i]) for i in range(1, len(w))) for w in TOWN_WAYS)
            if off > 0.9:
                self.problems.append(f"{self.name}: '{name}' leaves Driftreed's ways by {off:.1f} m at ({p[0]:.1f}, {p[1]:.1f})")
                return
            who = next((n for n in ("MaruAtStage", "DovAt", "QuillAt", "PimAt") if math.dist(p, R[n]) < 1.3), None)
            if who:
                self.problems.append(f"{self.name}: '{name}' walks through {who} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def door(self, boss, name, x, y, z, spawn, tolerance=0.8):
        """A boss's door step, checked from where a retry starts it too: its checkpoint spawn."""
        check = self.checks().get(self.scene)
        if check:
            here, self.at = self.at, spawn
            check(name + " (retried from the checkpoint)", (x, z))
            self.at = here
        super().door(boss, name, x, y, z, spawn, tolerance)

    def go(self, name, points_, **kw):
        """Walk a list of plan points (x, z), each the floor's own height."""
        for i, (x, z) in enumerate(points_):
            self.walk(f"{name} {i + 1}/{len(points_)}", x, 0, z, **kw)

    def crossing(self, name, lines, flag, wait=3):
        """A talk that starts on a crossing (or by itself: an AfterTalk, a captain who yields): wait for it, then read it."""
        self.add(f"{name} begins", wait, until="ui:dialogue", expectedUi="dialogue")
        self.read(name, lines, flag)

    def look(self, name, lines, flag):
        """A prompt that opens a talk: press it, read it."""
        self.add(name, 0.2, buttons=["South"], expectedUi="dialogue")
        self.add("release " + name, 0.3, expectedUi="dialogue")
        self.read(name, lines, flag)

    def fought(self):
        self.at = None

    def write(self):
        if self.problems:
            raise SystemExit("\n".join(self.problems) + "\nMANYHANDS_ROUTES_FAILED")
        super().write()


# The walked pieces the routes share, bent round what stands on or against the builder's own ways as built (each a finding): a planter by the spine's north
# end, (-2, 8.7); the three garden beds' fronts, (36|52|68, 25.6), and Dabb's pump, (40.6, 24.2), along the garden's way at z 25; a planter and a fan shrub in
# the east aisle at z 35.3; the solar arrays on the cabins' ways, (-44.5, 22) and (-56.1, 52.2); the crates round the spine's foot on the dock.
SPINE_UP = [(0.9, 8.6), (0, 20)]                                                                   # from the crossing
SPINE_DOWN = [(0.9, 8.6), H["Crossing"]]                                                           # from (0, 20)
GARDEN_IN = [(16, 20.4), (27, 24.2), (33.5, 24.2), (38.6, 24.2), (39.4, 25.6), (44, 25.5)]           # from (0, 20) to the foot of Marrow-1's aisle
GARDEN_OUT = [(39.4, 25.6), (38.6, 24.2), (33.5, 24.2), (27, 24.2), (16, 20.4), (0, 20)]            # from (44, 25.5)
GARDEN_EAST = [(48.6, 24.2), (55.4, 24.2), (59.8, 24.2), (60.5, 26.4)]                             # from (44, 25.5) to the foot of the east aisle (x 60)
GARDEN_WEST = [(59.8, 24.2), (55.4, 24.2), (48.6, 24.2), (44, 25.5)]                               # from (60.5, 26.4)
TO_TEO = [(60.5, 37.6), (59.4, 39.2)]                                                              # up the east aisle, between the planter and the shrub, past Garo
FROM_TEO = [(60.5, 37.6), (60.5, 26.4)]
TO_RACKS = [(62.2, 24.2), (72.4, 24.2), (73, 26.4), (73.1, 47.9)]                                   # from (60.5, 26.4), past bed 3's front and Dabb
FROM_RACKS = [(73, 26.4), (72.4, 24.2), (62.2, 24.2), (60.5, 26.4)]
TO_CABINS = [(0.9, 8.6), (0, 19.6), (-16, 20.4), (-30, 20), (-36, 19.4), (-50, 19.4), (-52, 20), (-52, 30.6), (-52, 40.6), (-52, 49.2), (-58, 49.4)]  # from the crossing
FROM_CABINS = [(-52, 49.2), (-52, 40.6), (-52, 30.6), (-52, 20), (-50, 19.4), (-36, 19.4), (-30, 20), (-16, 20.4), (0, 19.6), (0.9, 8.6), H["Crossing"]]  # from (-58, 49.4)


def boss(r, id_, door, x, y, z, spawn, rounds=10):
    """A boss on foot from its door (CHAPTER_GUIDE section 10): the door step the driver retries from, the lock, rounds of skills and the fight until it is
    down. As xylos_routes.boss."""
    r.lead("Taren")
    r.door(id_, door, x, y, z, spawn=spawn, tolerance=1.5)
    r.press("lock " + id_, "LeftShoulder", 0.2)
    stop = "flag:bossdown." + id_
    for i in range(rounds):
        r.rotation(f"{id_} {i + 1}", stop)
        r.add(f"fight {id_} round {i + 1}", 35, buttons=["South"], pulseSeconds=0.46, navigate=True, approachTarget=True, tolerance=2.3, rangedTolerance=12,
              evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=0, z=0))
    r.add(f"{id_} is down", 4, until=stop, expectedFlag="bossdown." + id_)


def flight_boss(r, id_, rounds=14, seconds=20):
    """A boss in flight after its door: the lock, then rounds of pursuit at firing range with the ship's shots, a Pulse, and the dodges the driver reads off the
    marks and the shots (as the Cantor's in chapter-10-gullet-evasive), until it is down."""
    stop = "flag:bossdown." + id_
    r.press("lock " + id_, "LeftShoulder", 0.2, leftTrigger=1)
    for i in range(rounds):
        r.add(f"pursue {id_} round {i + 1}", seconds, buttons=["South"], leftTrigger=1, navigate=True, approachTarget=True, tolerance=12, rangedTolerance=12,
              evadeProjectiles=True, evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=1, z=0))
        r.add(f"pulse {id_} round {i + 1}", 0.18, buttons=["RightShoulder", "West"], leftTrigger=1, stopWhen=stop)
        r.add(f"release pulse {id_} round {i + 1}", 0.45, leftTrigger=1)
    r.add(f"{id_} is down", 4, until=stop, expectedFlag="bossdown." + id_)


# ------------------------------------------------------------------ the routes
def decision():
    """Chapter 4's close, which no route played: from the portico's save the chart to Driftreed, Ona's decision, Abri's call, the chart to Tallow, the launch,
    and the dock off the approach that Abri's call opens, into Many Hands."""
    r = Checked(PREFIX + "27-driftreed-decision", "Chapter 4's close: the chart to Driftreed, Ona decides, Abri's call on the stage walk, the chart to Tallow, the new dock into Many Hands", chapter=4)
    # chapter-23 ends saved under Maru's lamp at the portico; the save's spawn is the great hall's door (the Rotbloom's checkpoint, the last one set).
    r.resume("DrownedWaystation", "wend.lampCarried", speed=COMBAT_SPEED, spawn=(-2.5, 261.5))
    r.lead("Taren")
    # Nothing is awake in the Waystation once the Rotbloom is dead: Sela's chart takes the party home (RouteChart.Blocked).
    r.chart("Driftreed", "Driftreed", spawn=R["ReedArrival"], form="Natural")
    r.speed = 4.5
    r.lead("Taren")
    hall = [(0, 18), (0, 34), R["MarketAt"], (0, 58), (0, 75), (1, 78.5), (0.5, 80.1)]
    for i, (x, z) in enumerate(hall):
        r.walk(f"up the stage walk to Ona {i + 1}/{len(hall)}", x, 0, z, tolerance=0.5)
    r.talk("Ona decides", 11, flag="wend.decided")  # OnaDecides (wend.rotbloomDead, not wend.decided)
    back = [(1, 78.5), (0, 75), (0, 58), R["MarketAt"], (0, 34)]
    for i, (x, z) in enumerate(back):
        r.walk(f"back down to the stage {i + 1}/{len(back)}", x, 0, z, tolerance=0.5)
    # Abri's call is a crossing on the stage walk, (0, 26), 6 m wide and 3 deep (WendmireBuilder.Driftreed: AbriCallAt), open from wend.decided.
    r.walk("down the stage walk until Abri calls", 0, 0, 18, stop="ui:dialogue")
    r.crossing("Abri's call about the name", 10, "chapter4.complete")  # AbriManyHandsCall
    # The chart to Tallow: the Veil flown back would be 1,600 m of lane for a dock the chart reaches in one press.
    r.chart("TallowDrift", "TallowDrift", spawn=(0, -10), form="Natural")
    r.lead("Taren")
    r.walk("to the pressure lift", 0, 0, -8)
    r.scene = "TallowApproach"
    r.add("launch from Tallow", 3, buttons=["South"], expectedForm="CivilFlight")
    r.add("release launch", 0.5)
    r.at, r.speed, r.brake = (0, -8.5), FLIGHT_SPEED, False
    # Many Hands is moored due south of the arrival, its dock at (0, -44), reach 5, between the Xylos warp (11, -37) and Wendmire's (-11, -37)
    # (ManyHandsBuilder.StageTallowDock), shown once chapter 4 is told.
    r.walk("toward the convoy", 0, 1, -34, tolerance=3)
    r.brake = True
    r.walk("settle at the dock to Many Hands", 0, 1, -40.4, tolerance=1)   # the dock at (0, -44) reaches 5
    r.brake = False
    r.scene = "ManyHands"
    r.add("dock at Many Hands", 4, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release dock", 0.5)
    r.at, r.speed = H["Arrival"], 4.5
    r.wait("first look at the dock", 2, expectedFlag="chapter4.complete")
    r.write()


def city():
    r = Checked(PREFIX + "28-many-hands", "Chapter 5: Abri's welcome, Binna's vent, Ruva, Pell's three lamps, the chart table, Maru and the running rings, Corra and Dov, Ilo, the claim, the launch", chapter=5)
    r.resume("ManyHands", "chapter4.complete", spawn=H["Arrival"])
    r.people = {"AbriDock", "Corra", "Dov", "Hadda", "Binna", "Ruva", "Teo", "Garo", "Dabb", "Maru", "Ona", "Pim", "Pell"}
    r.lead("Taren")
    # Abri's welcome is a crossing a step north of the arrival, 30 m wide across the dock: walking up the spine meets it.
    r.walk("up from the pads until Abri speaks", 0, 0, -55, stop="ui:dialogue")
    r.crossing("Abri's welcome", 10, "hands.aboard")  # AbriHandsArrival: starts Many Hands
    r.people -= {"AbriDock"}  # she walks up the spine to her desk on the gallery
    r.people |= {"Abri"}
    r.walk("on to the spine", 0, 0, -55)
    r.go("up the freight spine to the crossing", [(0, -40), (0, -20), H["Crossing"]])
    # The galley: along the arcade to its door, then round the east end of the long tables. The builder's own way from the door to Binna's hatch,
    # (66, 3.6) to (66, 10.2), runs through the north table and both its benches (finding); the west end is the grower's errand loop.
    r.go("along the arcade to the galley", [(50, 3), H["GalleyDoorWay"], (66, 3.6), (74.4, 4), (74.4, 10.2), (67.4, 11)], tolerance=0.5)
    r.talk("Binna's vent", 9, flag="hands.ventFixed")  # BinnaVent
    r.go("back round the tables", [(74.4, 10.2), (74.4, 4), (66, 3.6), H["GalleyDoorWay"], (50, 3), H["Crossing"]], tolerance=0.5)
    # The garden: up the spine's north end, under the carriers' ribs, and up Marrow-1's aisle to Ruva.
    r.go("to the garden", SPINE_UP + GARDEN_IN + [(44, 29.6)], tolerance=0.5)
    r.talk("Ruva's case for the roots", 6, flag="hands.ruvaHeard")  # RuvaMooring
    # The lamp gallery: up the stair at the spine's north end to Pell, then the three scopes round the chart table, each from its own side (the bake's points).
    r.go("to the lamp gallery", [(44, 25.5)] + GARDEN_OUT + [H["StairFoot"], H["StairTop"], (0, 40.4), (7, 45.4)], tolerance=0.5)
    r.talk("Pell's three lamps", 7, flag="hands.pellAboard")  # PellChartLamps
    # A scope's prompt reaches 1.3 m: nine tenths off it and two tenths of tolerance is surely inside; a metre and three tenths was not (RouteFightTests).
    east, north, west = (SCOPES[2][0] + 0.9, SCOPES[2][1]), (SCOPES[1][0], SCOPES[1][1] + 0.9), (SCOPES[0][0] - 0.9, SCOPES[0][1])
    r.walk("to the east scope", east[0], 0, east[1], tolerance=0.2)
    r.press_for("sight the east gallery lamp", "hands.lamp3")
    r.go("round to the north scope", [(3.6, 49.6), north], tolerance=0.2)
    r.press_for("sight the north gallery lamp", "hands.lamp2")
    r.go("round to the west scope", [(-3.6, 49.6), west], tolerance=0.2)
    r.press_for("sight the west gallery lamp", "hands.lamp1")
    r.wait("the three lamps are sighted", 0.5, expectedFlag="hands.lampsSighted")
    r.go("round to the chart table", [(-3.6, 45), H["ChartTalk"]], tolerance=0.3)
    r.look("open the route chart", 4, "hands.chartOpen")  # SelaChartOpen
    # The raft bay: down the stair and the spine, over the quay walk onto the ferry raft to Maru.
    r.go("down to the raft bay", [H["StairTop"], H["StairFoot"], (0, 20)] + SPINE_DOWN + [(0, -20), (0, -24), (-14.5, -24), (-19.5, -24), (-24, -24)], tolerance=0.5)
    r.talk("Maru's case for the tides", 6, flag="hands.maruHeard")  # MaruMooring
    # The running rings, at the hold's north-east wall: off the raft by the arcade walk, and along the quay to the end of the builder's way from the arcade.
    # Maru stands a metre off the straight line from the raft's middle to the walk: round her east side, short of the pulse lamp at (-21, -20.2).
    r.go("up the arcade walk to the hold wall", [(-22.8, -21.4), (-23.8, -19), (-24, -11.5), (-24, -8.6), (-19.4, -8.4)], tolerance=0.4)
    r.look("fit the running rings", 7, "hands.mooringSettled")  # SelaMooringRail: the first choice, Ruva's word (rings)
    r.wait("the three jobs are done", 0.5, expectedFlag="hands.jobsDone")
    # Corra's first talk aboard before the dinner: after it her first talk is her post line, which never marks her met aboard, and her kettles are never offered (finding).
    r.go("to Corra's stall", [(-19, 3), (-24, 3), (-24, 6)], tolerance=0.5)
    r.talk_to_shop("Corra's stall", 5)  # CorraHands: "Show us what you have."
    # Gel to the cap at Dov's counter before the hull and the freight hall.
    r.go("to Dov's counter", [(-24, 3), (-12, 3), (-12, 6)], tolerance=0.5)
    r.add("greet Dov", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release greet Dov", 0.3, expectedUi="dialogue")
    r.add("hear Dov out", 12, buttons=["South"], pulseSeconds=0.55, until="ui:options", expectedUi="options")
    r.add("release before his choices", 0.5)
    r.add("see his stock", 10, buttons=["South"], pulseSeconds=1.0, until="ui:shop", expectedUi="shop")
    r.add("find the repair gel", 3, pickRow="Repair Gel", until="row:Repair Gel", expectedUi="shop")
    r.stock("buy repair gel up to the cap", "Repair Gel")
    r.add("leave Dov's counter", 0.5, buttons=["East"], expectedUi="world")
    r.add("release Dov's counter", 0.3)
    # Ilo is off the courier on the dock from the three jobs' end; his talk brings Dessik's claim over the radio (an AfterTalk, 2.5 s on).
    r.people |= {"IloDock"}
    # Down to Ilo on the builder's way west from the arrival, which passes 2.1 m south of him (he stood 3.5 m off it, out of a talk's reach, until he was moved).
    r.go("down to the dock", [(-12, 3), H["Crossing"], (0, -20), (0, -40), (0, -55), H["Arrival"], (-10, -59.7)], tolerance=0.5)
    r.talk("Ilo off the courier", 8, flag="hands.iloAboard")  # IloArrives
    r.crossing("Dessik's claim on the radio", 8, "hands.claimRead", wait=8)  # DessikClaim
    r.people -= {"IloDock"}
    # The hull pad opens with the claim: (8, -62), reach 4, stood at from its west side.
    r.go("to the hull pad", [H["Arrival"], (0, -55), (0, -57), (5.2, -58.2), (5, -61)], tolerance=0.5)
    r.scene = "ManyHandsHull"
    r.add("launch to the hull", 4, buttons=["South"], expectedForm="Flight")
    r.add("release launch", 0.5)
    r.at, r.speed = O["ArrivalAt"], FLIGHT_SPEED
    r.write()


def hull():
    r = Checked(PREFIX + "29-hull-of-many-hands", "Chapter 5: the claim haulers over the raft bay, Hulljack's four feet, the way in after it through the freight hall's roof door", chapter=5)
    r.resume("ManyHandsHull", "hands.claimRead", speed=FLIGHT_SPEED, spawn=O["ArrivalAt"], form="Flight")
    r.lead("Taren")
    # The claim haulers tag the raft bay's hatch, waking as a ship comes over the hold (their trigger x -80 to -36, z -100 to -68): the tether before the frame's own.
    r.walk("north over the stern dock's yards", -32, 1, -104, tolerance=3)
    r.walk("over the raft bay's hold", -46, 1, -90, tolerance=3)
    r.fight_flight("Hull_Drones", rounds=3)
    r.fought()
    # The fight's door is the box over the four joins (z -62 to 42); its checkpoint, spawn Hulljack, is 4 m south of it. From there, or from the drones, a
    # straight flight north-west into the frame's room, short of J1's boom (-24 to -8 at z -40) and its cargo tower (-58, -47).
    r.door("Hulljack", "into the frame's reach at the raft bay join", -36, 1, -52, spawn=O["CheckpointAt"], tolerance=3)
    flight_boss(r, "Hulljack", rounds=14)
    # Its fourth foot cut, the frame goes down through the freight hall's roof door (BossExit, 12 m/s for about 3 s); the way after it opens there.
    r.wait("the frame tears through the roof door", 5)
    r.fought()
    door = O["DoorAt"]
    r.walk("toward the torn roof door", door[0], 1, door[1] + 11, tolerance=3)
    r.brake = True
    r.walk("settle over the roof door", door[0], 1, door[1] + 3.5, tolerance=1)  # the way's reach is 6.5 (WorldBuilder.FlightDockRange); nothing solid stands there
    r.brake = False
    r.scene = "ManyHands"
    r.add("follow Varn in", 5, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release follow", 0.5)
    r.at, r.speed = H["FreightHallSpawn"], 4.5
    r.wait("in the freight yard", 1.5, expectedFlag="bossdown.Hulljack")
    r.write()


def hall():
    r = Checked(PREFIX + "30-freight-hall", "Chapter 5: Dessik in the hall, Dessik Varn, his yield, the breach, Meret, the dinner, the chart read on the lamp gallery", chapter=5)
    r.resume("ManyHands", "bossdown.Hulljack", speed=COMBAT_SPEED, spawn=H["FreightHallSpawn"])
    r.people = {"Corra", "Dov", "Hadda", "Binna", "Ruva", "Teo", "Garo", "Dabb", "Maru", "Ona", "Pim", "Pell", "Abri", "Ilo"}
    r.lead("Taren")
    # The cargo door is open from the frame's arrival; Dessik's talk is a crossing just inside it, (32, -50.6), 8 by 2, and saves at FreightHall.
    r.walk("through the cargo door until Dessik speaks", H["HallDoor"][0], 0, -50, stop="ui:dialogue")
    r.crossing("Dessik in the freight hall", 5, "hands.hallEntered")  # DessikHall
    # The fight's box takes in the floor from z -51: the talk's end starts it, and the seal shuts behind. Lost, the driver retries from the door step:
    # a straight walk from FreightHall (32, -59) through the door gap (x 28 to 36) to clear floor 11.5 m in front of F.
    boss(r, "DessikVarn", "onto the freight hall's floor", H["F"][0], 0, -46, spawn=H["FreightHallSpawn"], rounds=10)
    r.add("reach the fallen hero after Dessik", 16, navigate=True, approachPartner=True, tolerance=1.2, stopWhen="partyAlive", point=dict(x=0, y=0, z=0))
    r.add("both heroes ready after Dessik", 6, until="partyAlive")
    r.crossing("Dessik yields", 7, "hands.dessikYielded", wait=10)  # DessikYields: his own person takes over where the frame kneels and speaks unasked
    r.lead("Taren")
    r.fought()
    r.people |= {"Patrol"}
    out = [(31.4, -47.6), (32, -50), H["FreightHallSpawn"], (12, -59.4), (0, -57), (0, -40), (0, -20), H["Crossing"]] + TO_CABINS
    r.go("out of the hall and up to the cabins", out)
    r.speed = 4.5
    r.talk("the captain at the seal", 8, flag="hands.breachSealed")  # PatrolBreachSealed
    # Meret stands behind him once he has spoken: round his east side to her, so she is nearer than he is.
    r.people |= {"MeretCabins"}
    r.go("round the captain to Meret", [(-58.2, 52.6), (-60.4, 52.7)], tolerance=0.3)
    r.talk("Meret Venn", 9, flag="hands.meretMet")  # MeretArrives
    r.people -= {"MeretCabins"}
    # The dinner: those at table are at their seats from hands.meretMet; Maru stands by the galley's east wall.
    r.people = (r.people - {"Ruva", "Teo", "Ilo", "Abri", "Maru"}) | {"RuvaSeat", "TeoSeat", "IloSeat", "AbriSeat", "MaruWall", "MeretSeat"}
    back = [(-58.2, 52.6), (-58, 49.4)] + FROM_CABINS + [(50, 3), H["GalleyDoorWay"], (66, 3.6), (74.4, 4), (74.4, 10.2), (67.4, 11)]
    r.go("down to the galley and round to Binna", back, tolerance=0.5)
    r.talk("Binna's dinner", 12, flag="hands.dinnerSat")  # BinnaDinner
    at_meret = (H["MeretSeat"][0], 3.1)
    r.go("to Meret at the table", [(74.4, 10.2), (74.4, 4), (66, 3.6), at_meret], tolerance=0.3)
    r.talk("Stillwater, explained to a cook", 8, flag="hands.stillwaterHeard")  # MeretStillwater
    r.walk("along the table to Ruva", H["RuvaSeat"][0], 0, 3, tolerance=0.3)
    r.talk("Ruva's number", 11, flag="hands.numberHeard")  # RuvaDinnerNumber
    # Round the middle table's west end to Ilo across it: the builder's way from the galley door, (56, 2.4) to (58, -5.2), passes .45 m from the table's corner.
    r.go("round the table to Ilo", [H["GalleyDoorWay"], (56.2, -3.4), (58.6, -5), (H["IloSeat"][0], -5.2)], tolerance=0.3)
    r.talk("Ilo's footnotes", 10, flag="hands.footnotesTabled")  # IloFootnotes
    r.go("back round to Meret", [(58.6, -5), (56.2, -3.4), H["GalleyDoorWay"], at_meret], tolerance=0.3)
    r.talk("what the anchor costs", 10, flag="hands.sorrelNamed")  # MeretSorrel
    # She gets up and walks to the galley door (4.6 s, then she stands there); her way out after the drawings is west along the arcade, so wait for her on its north side.
    r.wait("Meret goes to the galley door", 6)
    r.people = (r.people - {"MeretSeat"}) | {"GalleyDoor"}
    r.go("to Meret at the galley door", [H["GalleyDoorWay"], (52.6, 3), (53.2, 8.4)], tolerance=0.4)
    r.talk("Meret's drawings", 7, flag="hands.dinnerDone")  # MeretDrawings
    r.people -= {"GalleyDoor"}
    r.go("up to the lamp gallery", [(52.6, 3), (50, 3), H["Crossing"]] + SPINE_UP + [H["StairFoot"], H["StairTop"], H["ChartTalk"]], tolerance=0.4)
    r.look("read the chart", 9, "chapter5.complete")  # SelaLampGallery
    r.wait("chapter five closes", 3, expectedFlag="hands.chartRead")
    r.write()


def errands():
    r = Checked(PREFIX + "31-many-hands-errands", "Chapter 5: the dock door, the kettles, the claims ledger, the stowaway's fare, a save at the riggers' cradle", chapter=5)
    # chapter-30's last checkpoint was the freight hall's: Continue puts the party in the freight yard.
    r.resume("ManyHands", "chapter5.complete", spawn=H["FreightHallSpawn"])
    r.people = {"Corra", "Dov", "Hadda", "Binna", "Ruva", "Teo", "Garo", "Dabb", "Maru", "Ona", "Pim", "Pell", "Abri", "Ilo", "DessikHeld"}
    r.lead("Taren")
    # First along the yard from the checkpoint, so the leg check's first walk starts where the save does.
    r.go("to the dock door", [(24, -59.2), (12, -59.4), (10.6, -57.6)], tolerance=0.4)
    r.look("look at the dock door", 3, "hands.doorRead")  # SelaDockDoor
    r.go("up the spine to Corra", [(12, -59.4), (0, -57), (0, -40), (0, -20), H["Crossing"], (-24, 3), (-24, 6)], tolerance=0.5)
    r.talk_to_shop("Corra's kettles", 4)  # CorraKettleOffer: starts Somebody Has to Sell a Kettle, then her stall
    gallery = [(-24, 3), H["Crossing"]] + SPINE_UP + [H["StairFoot"], H["StairTop"], (0, 40.4), (-11, 42), (-17.4, 42)]
    r.go("up to Ilo's desk", gallery, tolerance=0.4)
    r.talk("Ilo's ledger", 5)  # IloLedgerOffer: starts The Claims Ledger
    r.walk("to Abri's desk", -11, 0, 42, tolerance=0.4)
    r.talk("Abri and Tallow's line in the ledger", 2, flag="ledger.abri")  # AbriLedger
    r.talk("Abri takes cabin nine's kettle", 2, flag="kettle.cabins")  # AbriKettle
    garden = [(0, 40.4), H["StairTop"], H["StairFoot"], (0, 20)] + GARDEN_IN + GARDEN_EAST
    r.go("down to the garden deck", garden, tolerance=0.4)
    r.talk("Garo's kettle", 1, flag="kettle.garden")  # GaroKettle
    r.go("to Teo", TO_TEO, tolerance=0.4)
    r.talk("Teo and the barges in the ledger", 1, flag="ledger.teo")  # TeoLedger
    # Behind the seed racks off Marrow-3, up the east aisle.
    r.go("up the east aisle to the seed racks", FROM_TEO + TO_RACKS, tolerance=0.4)
    r.look("look behind the seed racks", 8, "hands.jaskFound")  # JaskFound
    r.people |= {"JaskHid"}
    r.go("back down to Teo", FROM_RACKS + TO_TEO, tolerance=0.4)
    r.talk("Teo and the stowaway", 4)  # TeoStowawayOffer: starts The Stowaway's Fare
    r.go("to Ruva", FROM_TEO + GARDEN_WEST + [(44, 29.6)], tolerance=0.4)
    r.talk("Ruva on Jask", 4, flag="hands.ruvaOnJask")  # RuvaStowaway
    r.go("back to Teo", [(44, 25.5)] + GARDEN_EAST + TO_TEO, tolerance=0.4)
    r.talk("where Jask works", 5, flag="hands.jaskPlaced")  # TeoStowawayDecide: the first choice, the garden
    r.people = (r.people - {"JaskHid"}) | {"JaskGarden"}
    r.go("to Jask in Marrow-1's aisle", FROM_TEO + GARDEN_WEST + [(44, 47)], tolerance=0.4)
    r.talk("Jask's thanks", 1, flag="jask.thanked")  # JaskThanksGarden
    bay = [(44, 25.5)] + GARDEN_OUT + SPINE_DOWN + [(0, -20), (0, -24), (-14.5, -24), (-19.5, -24), (-24, -24)]
    r.go("down to the raft bay", bay, tolerance=0.4)
    r.talk("Maru and her raft in the ledger", 2, flag="ledger.maru")  # MaruLedger
    # The builder's way on from the walk's landing, (-42.5, -32) to (-48, -33.4), runs inside the weavers' raft's north rim within half a metre of it: into
    # the raft first, then along to Ona.
    # The pulse lamp at (-44.6, -33) stands in the walk's landing on the weavers' raft, half a metre off the builder's way on to Ona (finding): off the
    # walk's end before it, then round its south side.
    r.go("over the weavers' walk to Ona", [(-29, -29), (-42.5, -32), (-43.1, -32.4), (-43, -35), (-48, -35)], tolerance=0.4)
    r.talk("Ona's kettle", 2, flag="kettle.bay")  # OnaKettle
    up = [(-43, -35), (-43.1, -32.4), (-42.5, -32), (-29, -29), (-24, -24), (-19.5, -24), (-14.5, -24), (0, -24), (0, -20), H["Crossing"]] + SPINE_UP + [H["StairFoot"], H["StairTop"], (0, 40.4), (-11, 42), (-17.4, 42)]
    r.go("back up to Ilo", up, tolerance=0.4)
    r.talk("the three statements", 2, flag="ledger.returned")  # IloLedgerReturn
    r.go("down to Corra", [(-11, 42), (0, 40.4), H["StairTop"], H["StairFoot"], (0, 20)] + SPINE_DOWN + [(-24, 3), (-24, 6)], tolerance=0.4)
    r.talk_to_shop("the kettles delivered", 2)  # CorraKettleReturn
    r.wait("the kettles are paid for", 0.5, expectedFlag="kettle.reported")
    r.go("down the spine to the riggers' cradle", [(-24, 3), H["Crossing"], (0, -20), (0, -40), (0, -45), (-8.6, -45)], tolerance=0.4)
    r.save_at("repair and save at the riggers' cradle", "release the cradle")
    r.write()


def survey():
    """Findings, not refusals: everything the city builder stands solid, and every townsperson, measured against its own declared ways (the shared test's
    rule, 'a townsperson or a prop on a way people must walk'). Printed for the report."""
    for k, way in enumerate(WAYS):
        for i in range(1, len(way)):
            a, b = way[i - 1], way[i]
            steps = max(1, int(math.dist(a, b) / 0.25))
            seen = set()
            for j in range(steps + 1):
                p = (a[0] + (b[0] - a[0]) * j / steps, a[1] + (b[1] - a[1]) * j / steps)
                for label, c, yaw, hx, hz in CITY_SOLIDS:
                    if label not in seen and box_distance(p, c, yaw, hx, hz) < 0.35:
                        seen.add(label)
                        print(f"FINDING way {k} {a}->{b} runs into {label} at ({p[0]:.1f}, {p[1]:.1f})")
                for f in FOLK:
                    if f not in seen and math.dist(p, f) < 1.6:
                        seen.add(f)
                        print(f"FINDING way {k} {a}->{b} passes {math.dist(p, f):.1f} m from a townsperson at {f}")
    for loop in ERRANDS:
        for n, who in NAMED.items():
            for i in range(len(loop) - 1):
                if segment_distance(who, loop[i], loop[i + 1]) < 1.0:
                    print(f"FINDING the walker's loop {loop[i]}->{loop[i + 1]} passes {segment_distance(who, loop[i], loop[i + 1]):.1f} m from {n} at {who}")


if __name__ == "__main__":
    check_docs()
    decision()
    city()
    hull()
    hall()
    errands()
    survey()
    print("MANYHANDS_ROUTES_OK")
