"""Writes chapter 9's replay routes (chapter-53 to chapter-61), The House That Can Leave, from the builders' own places and the built scenes' prompts.

Places are read out of the builders (HomeBuilder.Approach.cs: the two warps to Nacre and Many Hands' chapter 9 points; HomeBuilder.Nacre.cs: the spiral, its hails,
solids and fights; HomeBuilder.Crawl.cs, .Terminator.cs, .Works.cs, .Ring.cs: each zone's named places and declared ways; HomeBuilder.Decks.cs: the day's places
and people, the meeting's room) and every prompt a route presses is read out of the built scene it stands in (its position, its reach, the flag it sets, the
zone a way goes to). A route's stand for a prompt is checked here: in reach with its walk's tolerance and RouteFightTests' fifteen centimetres to spare, and the
prompt the one meant. Every straight leg is checked before it is written: Harrowgate, the Picket, Many Hands and its hull by harrowgate_routes' and
manyhands_routes' checks (Idra's hull); Nacre Grave inside the lane, the breach or the basin by Idra's hull, off its hulls and solids, and off its tracks except
where the leg was waited for; the Decks, the Crawl, the Terminator and the Works along their declared ways (or on the floor before a prompt the ways pass at a
distance); the Ring inside its walls and off its cover. HomeRouteTests then checks every leg, press and fight on the built zones.

The driver's hands for chapter 9 (ContinuousReview.Home.cs): a press that waits for the prompt the screen offers ("until":"prompt:<words>": Orrin at his board in
the Coronach walk, the close at the walkway joint); the pad let go while the lead's hands are on a tread-hall coupling (the Terminator); a wait on a wreck track
("until":"clear:x,z;..." in flight now reads the tracks too: Nacre's tow line and berth ring, the Ring's channel); and the Foundry Saint's collars and the climb
to its back.

The chain. Chapter 8's last route (chapter-52-the-warmaster) ends in Harrowgate's cells with Abri's word (home.turnedForNacre), saved at spawn Verdict.
  chapter-53-the-way-to-nacre       Harrowgate's field to the quay's pad, the Picket's north end, the warp to Nacre Grave; Bryn at the outer marker, the outer tows,
                                    the tug Welcome Back (saves)
  chapter-54-nacre-grave            the long way round from the arrival: past the tug, the inner tows, the inner pass, across the tow line to Oda's hauler, round the
                                    basin, across the berth ring to the navigators' pod; Sela's chart to Many Hands
  chapter-55-many-hands-and-the-crawl  Meret's notice on the dock, Bryn's chart lesson, Maru names the Crawl; the chart to the Meridian Crawl, Dorrow at the helm, the
                                    drop cage to the Terminator
  chapter-56-the-terminator         the run south ahead of the dawn, the three couplings (the driver's hands still on each), the ride, Dorrow's crawler; the hall comes
                                    home in the Crawl; the launch to the Decks
  chapter-57-the-decks-on-the-day   Orrin at his board, the town meeting's eight talks, the way down to the Works
  chapter-58-separation-works       Dorrow under the promenade, joints 1 to 3 and the rigs and crews between them, the press hall's cradle (saves at PressHall)
  chapter-59-the-foundry-saint      the Foundry Saint (boss, its door retried, worked at its collars and climbed), Orrin on the speaker, the lift up to the Decks
  chapter-60-the-coronach           Hal's last machine, the call, the seven doors, the three last words, Orrin at the joint, the launch, the cut to the Ring
  chapter-61-the-broken-ring        the market's lee, two interceptor pairs, the channel waited for, the formation, the Debris Cutter (boss, its door retried), the
                                    tow, the warp to the hull, the dock, the fleet's chart at the table (chapter9.complete)

  python tools/home_routes.py          all of them
  python tools/home_routes.py 53 54    a subset, by number (a zone still being finished can be left out)
"""
import ast
import json
import math
import re
import sys
from pathlib import Path

import harrowgate_routes as hr
import manyhands_routes as mh
from lantern_routes import COMBAT_SPEED, FLIGHT_SPEED, encounters
from manyhands_routes import BODY, NUMBER, block
from xylos_routes import box_distance, segment_distance

ROOT = Path(__file__).resolve().parent.parent
WORLD = ROOT / "Lattice" / "Assets" / "_Project" / "Scripts" / "Editor" / "World"
SCENES = ROOT / "Lattice" / "Assets" / "_Project" / "Scenes"
MAPS = ROOT / "docs" / "maps"
PREFIX = "chapter-"
HULL = hr.HULL  # Idra's, the larger of the pair chapter 9 is played with
PAIR = ("Taren", "Idra")
WALK = 4.5
INSIDE = hr.INSIDE
TALK = hr.TALK
FAILED = "HOME_ROUTES_FAILED"


def fail(text):
    raise SystemExit(text + "\n" + FAILED)


def source(name):
    return (WORLD / name).read_text(encoding="utf-8")


APPROACH = source("HomeBuilder.Approach.cs")
NACRE_SOURCE = source("HomeBuilder.Nacre.cs")
DECKS_SOURCE = source("HomeBuilder.Decks.cs")
CRAWL_SOURCE = source("HomeBuilder.Crawl.cs")
RUN_SOURCE = source("HomeBuilder.Terminator.cs")
WORKS_SOURCE = source("HomeBuilder.Works.cs")
RING_SOURCE = source("HomeBuilder.Ring.cs")


def const(text, name):
    m = re.search(r"\b" + name + "=" + NUMBER, text)
    if not m:
        fail(f"no {name} in its builder")
    return float(m.group(1))


def vec2s(text):
    """Every `Name=new(x,z)` of a builder's block, by name."""
    return {m.group(1): (float(m.group(2)), float(m.group(3))) for m in re.finditer(r"\b([A-Z]\w*)=new\(" + NUMBER + "," + NUMBER + r"\)", text)}


def vec3s(text):
    """Every `Name=new(x,y,z)` (a ZoneKit.FlightPlane or Plane for y) as its plan point."""
    return {m.group(1): (float(m.group(2)), float(m.group(3))) for m in re.finditer(r"\b([A-Z]\w*)=new\(" + NUMBER + r",(?:ZoneKit\.FlightPlane|Plane|" + NUMBER[1:-3] + r"f?)," + NUMBER + r"\)", text)}


# ------------------------------------------------------------------ the built scenes' prompts
def unquote(value):
    value = value.strip()
    if value.startswith('"') and value.endswith('"'):
        return json.loads(value)
    if value.startswith("'") and value.endswith("'"):
        return value[1:-1].replace("''", "'")
    return value


_PROMPTS = {}


def scene_prompts(scene):
    """Every prompt a built scene stands (Assets/_Project/Scenes/<scene>.unity): each MonoBehaviour with a prompt and a reach (an InteractionPrompt: a talk, a point,
    a way, a door, a hand), as dicts of its object's name, plan point, height, reach, words, the flag it sets and the zone a way goes to. Read, never written."""
    if scene in _PROMPTS:
        return _PROMPTS[scene]
    text = (SCENES / (scene + ".unity")).read_text(encoding="utf-8")
    docs = {m.group(2): (m.group(1), m.group(3)) for m in re.finditer(r"^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- |\Z)", text, flags=re.M | re.S)}
    tr, of_object = {}, {}
    for fid, (kind, body) in docs.items():
        if kind != "4":
            continue
        found = [re.search(p, body) for p in (r"m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", r"m_LocalRotation: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}",
                                              r"m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", r"m_Father: \{fileID: (\d+)\}", r"m_GameObject: \{fileID: (\d+)\}")]
        if all(found):
            tr[fid] = (found[3].group(1), tuple(map(float, found[0].groups())), tuple(map(float, found[1].groups())), tuple(map(float, found[2].groups())))
            of_object[found[4].group(1)] = fid
    names = {fid: unquote(re.search(r"m_Name: (.*)", body).group(1)) for fid, (kind, body) in docs.items() if kind == "1" and "m_Name: " in body}

    def rotate(q, v):
        x, y, z, w = q
        ux, uy, uz = 2 * (y * v[2] - z * v[1]), 2 * (z * v[0] - x * v[2]), 2 * (x * v[1] - y * v[0])
        return (v[0] + w * ux + y * uz - z * uy, v[1] + w * uy + z * ux - x * uz, v[2] + w * uz + x * uy - y * ux)

    def mul(a, b):
        ax, ay, az, aw = a
        bx, by, bz, bw = b
        return (aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx, aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz)

    def world(fid):
        parent, pos, rot, scale = tr[fid]
        if parent not in tr:
            return pos, rot, scale
        ppos, prot, pscale = world(parent)
        local = rotate(prot, (pos[0] * pscale[0], pos[1] * pscale[1], pos[2] * pscale[2]))
        return (ppos[0] + local[0], ppos[1] + local[1], ppos[2] + local[2]), mul(prot, rot), (pscale[0] * scale[0], pscale[1] * scale[1], pscale[2] * scale[2])

    out = []
    for fid, (kind, body) in docs.items():
        if kind != "114" or "\n  prompt: " not in body or "\n  range: " not in body:
            continue
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", body).group(1)
        holder = of_object.get(go)
        if holder is None:
            continue
        (x, y, z), _, _ = world(holder)
        field = lambda key: unquote(m.group(1)) if (m := re.search(r"^  " + key + r": (.*)$", body, flags=re.M)) else ""
        out.append(dict(name=names.get(go, ""), at=(x, z), y=y, range=float(field("range")),
                        prompt=field("prompt"), flag=field("flag"), to=field("scene"), node=field("dialogueNode") or field("firstNode"), requires=field("requiredFlag")))
    _PROMPTS[scene] = out
    return out


def prompt_of(scene, where, **want):
    """The one prompt of a scene that answers to every given field (flag=, prompt=, to=, node=; a prompt's words may be given as their start), nearest `near`."""
    near = want.pop("near", None)
    found = [p for p in scene_prompts(scene) if all(p[k].startswith(v) if k == "prompt" else p[k] == v for k, v in want.items())]
    if near is not None:
        found.sort(key=lambda p: math.dist(p["at"], near))
    if not found:
        fail(f"{where}: {scene} has no prompt with {want} (a stale scene, or the builder renamed it)")
    return found[0]


def reach(where, stand, tolerance, prompt):
    """A press made at a prompt from a stand: the walk's end, plus its tolerance and RouteFightTests' fifteen centimetres, inside the prompt's reach on the plan."""
    gap = math.dist(stand, prompt["at"]) + tolerance + INSIDE
    if gap > prompt["range"]:
        fail(f"{where}: standing within {tolerance} of {stand} is {gap:.2f} m from '{prompt['prompt']}' at ({prompt['at'][0]:.2f}, {prompt['at'][1]:.2f}), which reaches {prompt['range']}")


# ------------------------------------------------------------------ chapter 9's places in older scenes (HomeBuilder.Approach)
HULL_WARP, HULL_FROM_NACRE, HULL_FROM_RING, PICKET_WARP = (vec3s(APPROACH)[k] for k in ("HullWarp", "HullFromNacre", "HullFromRing", "PicketWarp"))
_ap = vec2s(APPROACH)
NOTICE_LINE, LESSON_AT, CHART_READ_AT, MARU_TALK = (_ap[k] for k in ("NoticeLine", "LessonAt", "ChartReadAt", "MaruTalk"))
# The warp to Nacre off the stern stands beside chapter 6's, 7's and 8's: what a hull meets there.
hr.STERN_WARPS.append(("the warp to Nacre", HULL_WARP))
H = mh.H
T = hr.T

# ------------------------------------------------------------------ Nacre Grave, as HomeBuilder.Nacre lays it
_NACRE = block(NACRE_SOURCE, "internal static class Nacre")
N_CONST = {k: const(_NACRE, k) for k in ("R0", "Pitch", "Half", "StartTurn", "EndTurn", "BasinRim", "BerthSolid", "MouthTurn", "BreachTurn", "BreachHalf", "TowOffset", "TowFrom",
                                         "TowTo", "RaceRadius", "RaceFrom", "RaceTo", "RingRadius", "MarkerTurn", "TugTurn")}


def nacre_r(turn):
    return N_CONST["R0"] - N_CONST["Pitch"] * turn / 360


def nacre_on(turn, radius):
    a = math.radians(turn)
    return (round(math.sin(a) * radius, 2), round(math.cos(a) * radius, 2))


def lane(turn, offset=0):
    """A point of the lane (Nacre.At): `offset` metres out from the lane's middle at that turn."""
    return nacre_on(turn, nacre_r(turn) + offset)


def basin(turn, radius):
    return nacre_on(turn, radius)


def _nacre_places():
    """The builder's At(...) and In(...) places, worked out as it works them: the hails, the hulls, the fights."""
    names = {k: v for k, v in N_CONST.items()}
    out = {}
    for m in re.finditer(r"\b([A-Z]\w*)=(At|In)\(([^()]*)\)", _NACRE):
        args = [a.strip().rstrip("f") for a in m.group(3).split(",")]
        try:
            values = [eval(compile(ast.parse(re.sub(r"(\d)f\b", r"\1", a), mode="eval"), "<builder>", "eval"), {"__builtins__": {}}, names) for a in args]
        except Exception:
            continue
        out[m.group(1)] = lane(*values[:2]) if m.group(2) == "At" else basin(*values[:2])
    return out


NP = _nacre_places()
NACRE_ARRIVAL, NACRE_HOME = NP["Arrival"], NP["HomeWarp"]
# What a hull meets in the field, at the plane, as (label, centre, radius): the hulls the prompts stand by and the warp home's piece (WorldBuilder.Warp: 2 m).
NACRE_SOLIDS = [("the tug Welcome Back", NP["Tug"], 3.8), ("Oda's hauler", NP["Hauler"], 4.6), ("the navigators' pod", NP["Pod"], 2.4), ("Neve's skiff", NP["NeveSkiff"], 2.6),
                ("Mira's lot", NP["Lot"], 1.6), ("the outer marker", NP["MarkerBuoy"], 1.8), ("the warp home", NACRE_HOME, 1.42)]
NACRE_SOLIDS += [(f"the family wreck at turn {t:g}", lane(t, 12.6), 2.8) for t in (float(v) for v in re.findall(NUMBER, block(_NACRE, "FamilyWrecks=")))]
# The tug's raft (Nacre.TugRaft): hulls on lines from her cleats, each solid at .36 of its size where it lies in the lane ("Raft hull").
for _m in re.finditer(r"\(At\(TugTurn([+-]\d+),(\d+(?:\.\d+)?)f?\),\"\w+\"," + NUMBER + r",\d\)", block(NACRE_SOURCE, "var raft=new[]")):
    _at = lane(N_CONST["TugTurn"] + float(_m.group(1)), float(_m.group(2)))
    if math.hypot(*_at) < nacre_r(N_CONST["TugTurn"]) + N_CONST["Half"] - 1:
        NACRE_SOLIDS.append((f"a raft hull at turn {N_CONST['TugTurn'] + float(_m.group(1)):g}", _at, float(_m.group(3)) * .36))
WRECK_REACH = {"the tow line": 3.5, "the breach race": 4, "the berth ring": 4}


def nacre_open(p, margin):
    """Nacre.Open with a margin: in the lane, the mouth's strip, the breach, or the basin outside the berth."""
    c = N_CONST
    r = math.hypot(*p)
    theta = math.degrees(math.atan2(p[0], p[1])) % 360
    for k in (-1, 0, 1):
        turn = theta + 360 * k
        if c["StartTurn"] <= turn <= c["EndTurn"] and abs(r - nacre_r(turn)) <= c["Half"] - margin:
            return True
    turn = theta + 360
    if c["MouthTurn"] + margin / 2 <= turn <= c["EndTurn"] and c["BasinRim"] - margin <= r <= nacre_r(turn) - c["Half"] + margin:
        return True
    if c["BerthSolid"] + margin <= r <= c["BasinRim"] - margin:
        return True
    ray = (math.sin(math.radians(c["BreachTurn"])), math.cos(math.radians(c["BreachTurn"])))
    out, side = p[0] * ray[0] + p[1] * ray[1], p[0] * ray[1] - p[1] * ray[0]
    return abs(side) <= c["BreachHalf"] - margin and c["BasinRim"] - 1 <= out <= nacre_r(c["BreachTurn"]) - c["Half"] + 1


def wreck_tracks_near(p, room):
    """The track whose wrecks would pass within `room` of a point: the tow line (5 m inside the lane's inner wall), the breach race, the berth ring."""
    c = N_CONST
    r = math.hypot(*p)
    theta = math.degrees(math.atan2(p[0], p[1])) % 360
    for k in (-1, 0, 1):
        turn = theta + 360 * k
        if c["TowFrom"] <= turn <= c["TowTo"] and abs(r - (nacre_r(turn) + c["TowOffset"])) < room + WRECK_REACH["the tow line"]:
            return "the tow line"
    if c["RaceFrom"] <= theta <= c["RaceTo"] and abs(r - c["RaceRadius"]) < room + WRECK_REACH["the breach race"]:
        return "the breach race"
    if abs(r - c["RingRadius"]) < room + WRECK_REACH["the berth ring"]:
        return "the berth ring"
    return None


# ------------------------------------------------------------------ the Decks on the day, as HomeBuilder.Decks lays them
_DECKS = block(DECKS_SOURCE, "internal static class Decks")
DK = vec2s(_DECKS[:_DECKS.index("internal static readonly Vector2[][] Ways=")])
for _name in ("PitchNine", "Board", "StampingWindow", "NoticeBoard", "GateSix", "LockNine", "DockRail", "BoltHoles"):
    _m = re.search(_name + r"=\(new\(" + NUMBER + "," + NUMBER + r"\),new\(" + NUMBER + "," + NUMBER + r"\)\)", _DECKS)
    if not _m:
        fail(f"HomeBuilder.Decks has no {_name}=(at,stand)")
    DK[_name], DK[_name + "Stand"] = (float(_m.group(1)), float(_m.group(2))), (float(_m.group(3)), float(_m.group(4)))
DECKS_WAYS = [mh.points(part, DK) for part in re.findall(r"new\[\]\{(.*?)\}", block(_DECKS, "internal static readonly Vector2[][] Ways=")[1:-1])]
# The meeting's people, where the builder stands them (Decks.Meeting): Person(id, at, ...) under the meeting's root.
MEETING = {m.group(1): (float(m.group(2)), float(m.group(3))) for m in re.finditer(r'Person\("(\w+)",new Vector2\(' + NUMBER + "," + NUMBER + r"\),\d+,\"\w+\",\"\w+\",meeting", _DECKS)}
MEETING["Orrin"] = DK["BoardStand"]
MEETING_BENCHES = [(float(x), float(z)) for x in re.findall(NUMBER, re.search(r"foreach\(float x in new\[\]\{([^}]*)\}\)foreach\(float z in new\[\]\{([^}]*)\}\)Bench", _DECKS).group(1))
                   for z in re.findall(NUMBER, re.search(r"foreach\(float x in new\[\]\{([^}]*)\}\)foreach\(float z in new\[\]\{([^}]*)\}\)Bench", _DECKS).group(2))]
BENCH_HALF = (2.1, .21)  # Decks.Bench: 4.2 by .42
MEETING_SPAWN, LIFT_SPAWN = DK["MeetingSpawn"], DK["LiftSpawn"]
# The day's arrival is chapter 1's (StationRedesign's Hub_Decks spawns, copied with the scene): its map doc's coordinates table gives it.
_spawns = re.search(r"\| Spawns \| `Arrival` and `Office` \((-?[\d.]+), (-?[\d.]+)\)", (MAPS / "DecksLeaving.md").read_text(encoding="utf-8"))
DECKS_ARRIVAL = (float(_spawns.group(1)), float(_spawns.group(2))) if _spawns else fail("docs/maps/DecksLeaving.md has no Spawns row for Arrival")


def check_decks_docs():
    """The Coronach walk's stands, as its engineer wrote them for route writers (docs/maps/DecksLeaving.md, "For route writers"): each with its door's flag."""
    text = (MAPS / "DecksLeaving.md").read_text(encoding="utf-8").split("## For route writers: the Coronach walk, press by press")[1]
    rows = {}
    for line in text.splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 6 or not cells[0].isdigit():
            continue
        stand = re.search(r"\((-?[\d.]+), (-?[\d.]+)\)", cells[3])
        flag = re.search(r"`([\w.]+)`", cells[5])
        if stand and flag:
            rows[flag.group(1)] = (float(stand.group(1)), float(stand.group(2)))
    return rows


# ------------------------------------------------------------------ the Meridian Crawl, as HomeBuilder.Crawl lays it
_CRAWL = block(CRAWL_SOURCE, "internal static class Crawl")
CR = vec2s(_CRAWL)
CRAWL_WAYS = [mh.points(part, CR) for part in re.findall(r"new\[\]\{(.*?)\}", block(_CRAWL, "internal static readonly Vector2[][] Ways=")[1:-1])]
CRAWL_RAISED = const(_CRAWL, "Raised")
# The way up to the helm as the builder declares it, from the spine's west end onto the ramp's foot (Crawl.Ways[1], before HelmFoot); and round the garden stair's rail ends,
# which stand on the spine's line at x 35.25 and 40.8 (a finding: the floor's own way round is at z -3.2, HomeRouteTests, November 14).
HELM_RAIL = [p for p in CRAWL_WAYS[1] if p not in (CR["HelmFoot"], CR["HelmHead"], CR["DorrowHand"])]
STAIR_RAILS = [(34.6, -3.35), (41.4, -3.35)]


def crawl_y(p):
    helm = (-88, -74, -7, 7)
    if helm[0] <= p[0] <= helm[1] and helm[2] <= p[1] <= helm[3]:
        return CRAWL_RAISED
    if -74 < p[0] < -66 and abs(p[1]) <= 2.4:
        return round(CRAWL_RAISED * (-66 - p[0]) / 8, 2)
    return 0.0


# ------------------------------------------------------------------ the Terminator, as HomeBuilder.Terminator lays it
_RUN = block(RUN_SOURCE, "internal static class Terminator")
RN = vec2s(_RUN)
_front = {}
for _m in re.finditer(r"internal static Vector2 (Front|Socket|Seat|Checkpoint)\(int car\)=>(.*?);", _RUN):
    _front[_m.group(1)] = _m.group(2)


COUPLINGS = sorted([p for p in scene_prompts("Terminator") if p["flag"].startswith("home.coupling")], key=lambda p: p["flag"])
RIDE = next((p for p in scene_prompts("Terminator") if p["prompt"].startswith("Ride the tread-hall out")), None)


def run_points():
    """Terminator.Way as plan points, its Seat(n) entries taken from the built couplings (seat n is coupling n + 1) and Board from the builder."""
    text = block(_RUN, "internal static readonly Vector2[] Way=")[1:-1]
    out = []
    for m in re.finditer(r"new\(" + NUMBER + "," + NUMBER + r"\)|Seat\((\d)\)|\bBoard\b", text):
        if m.group(3) is not None:
            out.append(("seat", int(m.group(3))))
        elif m.group(0) == "Board":
            out.append(("board", RN["Board"]))
        else:
            out.append(("way", (float(m.group(1)), float(m.group(2)))))
    return out


RUN_WAY = run_points()
RUN_HOME = mh.points(block(_RUN, "internal static readonly Vector2[] Home=")[1:-1], RN)
_g = re.search(r"Gallery=Rect\.MinMaxRect\(" + ",".join([NUMBER] * 4) + r"\)", _RUN)
RUN_GALLERY = tuple(float(v) for v in _g.groups())  # x min, z min, x max, z max: the engine car's front gallery, 1.6 m up

# ------------------------------------------------------------------ the Separation Works, as HomeBuilder.Works lays it
_WORKS = block(WORKS_SOURCE, "internal static class Works")
WK = vec2s(_WORKS[:_WORKS.index("internal const string DecksFromWorks")])
WORKS_WAYS = [[(float(a), float(b)) for a, b in re.findall(r"new\(" + NUMBER + "," + NUMBER + r"\)", part)] for part in
              re.findall(r"new Vector2\[\]\{(.*?)\}", block(_WORKS, "internal static readonly Vector2[][] Ways=")[1:-1])]
WORKS_FLOORS = {k: const(_WORKS, k) for k in ("TugFloor", "HaulerFloor", "LinerFloor", "BargeFloor", "ControlFloor")}

# ------------------------------------------------------------------ the Broken Ring, as HomeBuilder.Ring lays it
_RING = block(RING_SOURCE, "internal static class Ring")
RG = vec3s(_RING)
RING_WALL_X, RING_SOUTH, RING_EXIT_HALF, RING_EXIT_END = (const(_RING, k) for k in ("WallX", "SouthZ", "ExitHalf", "ExitEnd"))
RING_CHANNEL_HALF, RING_CHANNEL_FROM, RING_CHANNEL_TO, RING_BANK = (const(_RING, k) for k in ("ChannelHalf", "ChannelFrom", "ChannelTo", "BankX"))
RING_CENTRE = vec2s(_RING)["Centre"]
RING_BOUND = const(_RING, "Bound")


# ------------------------------------------------------------------ the checks
class Checked(hr.Checked):
    """harrowgate_routes' checks for Harrowgate, the Picket, Many Hands and its hull (at Idra's hull), and chapter 9's: Nacre Grave's lane, breach and basin, its
    hulls and its tracks; the Decks', the Crawl's, the Terminator's and the Works' declared ways (or the floor before a prompt), off the people standing there;
    the Ring's walls. `waited` is the way the last wait on a clock was for: a leg along it may cross a track."""

    def __init__(self, *args, **kw):
        super().__init__(*args, **kw)
        self.waited = []
        self.decks_people = []

    def checks(self):
        c = super().checks()
        picket = c["PicketOfTeeth"]

        def picket9(name, to):
            picket(name, to)
            if not self.problems:
                for p in self.legs(to):
                    if box_distance(p, PICKET_WARP, 0, 1, 1) < self.hull + .5:
                        return self.fail(f"'{name}' flies within a hull of the warp to Nacre at ({p[0]:.1f}, {p[1]:.1f})")
        c.update({"PicketOfTeeth": picket9, "NacreGrave": self.check_nacre, "DecksLeaving": self.check_decks, "MeridianCrawl": self.check_crawl, "Terminator": self.check_run,
                  "SeparationWorks": self.check_works, "BrokenRing": self.check_ring})
        return c

    def walk(self, name, x, y, z, tolerance=0.8, seconds=None, stop=None, buttons=None):
        if y is None:
            y = {"MeridianCrawl": crawl_y, "SeparationWorks": works_y, "NacreGrave": lambda p: 1, "BrokenRing": lambda p: 1, "PicketOfTeeth": lambda p: 1,
                 "ManyHandsHull": lambda p: 1}.get(self.scene, lambda p: None)((x, z))
        before = (x, z)
        super().walk(name, x, y, z, tolerance, seconds, stop, buttons)
        if self.waited and math.dist(before, self.waited[0]) < .01:
            self.waited.pop(0)

    # -------------------------------------------------------------- the leg checks
    def check_nacre(self, name, to):
        waited = bool(self.waited) and math.dist(to, self.waited[0]) < .01
        for p in self.legs(to):
            if not nacre_open(p, self.hull + 1):
                return self.fail(f"'{name}' flies out of Nacre's lane, breach and basin (a hull and a metre in) at ({p[0]:.1f}, {p[1]:.1f}), r {math.hypot(*p):.1f}")
            hit = next((label for label, c, r in NACRE_SOLIDS if math.dist(p, c) < r + self.hull + .5), None)
            if hit:
                return self.fail(f"'{name}' flies within a hull of {hit} at ({p[0]:.1f}, {p[1]:.1f})")
            track = wreck_tracks_near(p, self.hull + .5)
            if track and not waited:
                return self.fail(f"'{name}' flies onto {track} at ({p[0]:.1f}, {p[1]:.1f}) without a wait for it to be clear")

    def on_ways(self, name, to, ways, people=(), off=1.2):
        for p in self.legs(to):
            gap = min(min(segment_distance(p, w[i - 1], w[i]) for i in range(1, len(w))) for w in ways if len(w) > 1)
            if gap > off and not any(math.dist(p, at) <= round_ for at, round_ in self.stands):
                return self.fail(f"'{name}' leaves {self.scene}'s ways by {gap:.1f} m at ({p[0]:.1f}, {p[1]:.1f})")
            who = next((at for at in people if math.dist(p, at) < .35 + BODY + .15), None)
            if who:
                return self.fail(f"'{name}' walks through someone standing at {who}")

    def check_decks(self, name, to):
        self.on_ways(name, to, DECKS_WAYS + getattr(self, "walk_ways", []), self.decks_people)
        if not self.problems:
            for p in self.legs(to):
                bench = next((b for b in self.benches if box_distance(p, b, 0, *BENCH_HALF) < BODY + .15), None) if getattr(self, "benches", None) else None
                if bench:
                    return self.fail(f"'{name}' walks into the meeting's bench at {bench} at ({p[0]:.1f}, {p[1]:.1f})")
                # The Coronach walk's people and things as built (coronach_solids): a hero's body and a tenth clear of each.
                hit = next((label for label, c, yaw, hx, hz in getattr(self, "walk_solids", []) if box_distance(p, c, yaw, hx, hz) < HERO + .1), None)
                if hit:
                    return self.fail(f"'{name}' walks into {hit}, standing in the Coronach walk, at ({p[0]:.1f}, {p[1]:.1f})")

    def check_crawl(self, name, to):
        self.on_ways(name, to, CRAWL_WAYS, [CR["DorrowAt"], CR["TamAt"]])

    def check_run(self, name, to):
        way = [p for kind, p in RUN_WAY if kind == "way"] + [c["at"] for c in COUPLINGS]
        ordered = [p if kind != "seat" else COUPLINGS[p]["at"] for kind, p in RUN_WAY]
        self.on_ways(name, to, [ordered, RUN_HOME], off=2.2)

    def check_works(self, name, to):
        self.on_ways(name, to, WORKS_WAYS, off=1.6)

    def check_ring(self, name, to):
        for p in self.legs(to):
            if p[1] < RING_SOUTH + self.hull + 1 or (p[1] < RING_EXIT_END - self.hull - 1 and abs(p[0]) > RING_WALL_X - self.hull - 1) or p[1] > RING_EXIT_END - self.hull - 1:
                return self.fail(f"'{name}' flies into the Ring's walls at ({p[0]:.1f}, {p[1]:.1f})")
            if p[1] > RING_CENTRE[1] + RING_BOUND and abs(p[0]) > RING_EXIT_HALF - self.hull - 1:
                return self.fail(f"'{name}' flies into the exit lane's side at ({p[0]:.1f}, {p[1]:.1f})")
            in_channel = abs(p[0]) < RING_CHANNEL_HALF + self.hull and RING_CHANNEL_FROM < p[1] < RING_CHANNEL_TO
            if in_channel and not (self.waited and math.dist(to, self.waited[0]) < .01):
                return self.fail(f"'{name}' flies into the channel at ({p[0]:.1f}, {p[1]:.1f}) without a wait for it to be clear")

    # -------------------------------------------------------------- presses, at the built scenes' prompts
    def stand_at(self, what, stand, prompt, tolerance=.4, y=None, flying=False, **kw):
        """Walk (or, flying, settle braked) to a stand for a prompt of the built scene, its reach checked from there. The stand is floor off the ways a route may step to."""
        reach(f"{self.name} '{what}'", stand, tolerance, prompt)
        here = self.at if self.at is not None else stand
        if not flying:
            self.stands.append((stand, max(1.0, math.dist(stand, here) + .5)))
            self.walk(f"to {what}", stand[0], y, stand[1], tolerance=tolerance, **kw)
        else:
            self.brake = True
            self.walk(f"settle at {what}", stand[0], 1, stand[1], tolerance=tolerance, **kw)
            self.brake = False

    def hail(self, name, lines, flag):
        """A prompt that opens a talk (a hail, a person, a point): a beat to settle, then the interact button every six tenths until the talk is up (a press made as the
        body arrives can land before the prompt is offered, and in flight the same button fires a shot), then the talk read."""
        flying = self.speed >= FLIGHT_SPEED
        self.add("settle before " + name, 0.5, leftTrigger=1 if flying else None)
        self.add(name, 5, buttons=["South"], pulseSeconds=0.6, until="ui:dialogue", expectedUi="dialogue", leftTrigger=1 if flying else None)
        self.add("release " + name, 0.3, expectedUi="dialogue")
        self.read(name, lines, flag)

    def offered(self, name, words, seconds=60):
        """Hold still until the screen offers this prompt to the lead (the driver reads PromptService's offer): someone who has to arrive first (Orrin at his board)."""
        self.add(name, seconds, until="prompt:" + words, useItem="none")

    def leave(self, name, scene, spawn, form, seconds=6, flying=False):
        self.scene = scene
        self.add(name, seconds, buttons=["South"], pulseSeconds=1.0, until="scene:" + scene, expectedUi="world", expectedForm=form, leftTrigger=1 if flying else None)
        self.add("release " + name, 0.5)
        self.at = spawn

    def wait_clear(self, name, way, seconds=40, brake=None):
        super().wait_clear(name, way, seconds, brake)
        self.waited = list(way)

    # -------------------------------------------------------------- a fight is locked from inside its box, never from wherever the last one ended
    def into(self, id_, across=0.0, along=0.0, seconds=None):
        """On into a fight's volume (docs/quality/encounters.json) from wherever the fight before it was cleared: its middle moved `across` and `along` in its own frame, inside
        it by more than the leg's tolerance. Route.enter walks into a box only from a known place; after a fight the party is anywhere (D293). A fight woken in the same rounds
        as the last may be cleared already: the leg ends then."""
        box = next((b for b in encounters().get(self.scene, []) if b["id"] == id_), None)
        if box is None:
            return self.fail(f"no fight {id_} in {self.scene}'s volumes (docs/quality/encounters.json)")
        flying = self.speed >= FLIGHT_SPEED
        tolerance = 3 if flying else 0.8
        if abs(across) > box["width"] / 2 - tolerance - 1 or abs(along) > box["depth"] / 2 - tolerance - 1:
            return self.fail(f"the leg into {id_} aims {across}, {along} from its middle: not inside it by the leg's tolerance")
        turn = math.radians(box["yaw"])
        x = box["x"] + math.cos(turn) * across + math.sin(turn) * along
        z = box["z"] - math.sin(turn) * across + math.cos(turn) * along
        self.walk(f"on into {id_}", round(x, 1), 1 if flying else None, round(z, 1), tolerance=tolerance, stop=f"flag:clear.{id_}", seconds=seconds)

    def refuse_unknown(self, id_):
        if self.at is None:
            self.fail(f"'lock {id_}' is pressed from wherever the fight before it ended: walk or fly into its volume first (into)")

    def fight_ground(self, id_, rounds=4, seconds=30, skills=True):
        self.refuse_unknown(id_)
        super().fight_ground(id_, rounds, seconds, skills)

    def fight_flight(self, id_, rounds=3, seconds=30):
        self.refuse_unknown(id_)
        super().fight_flight(id_, rounds, seconds)

    def write(self):
        if self.problems:
            fail("\n".join(self.problems))
        mh.Route.write(self)


def works_y(p):
    """The Works' floors by stretch (docs/maps/SeparationWorks.md): the tug and the control room at 0, the hauler -1.5, the liner -3, the landing and the hall -6."""
    z = p[1]
    if p[0] > 9 and 74 < z < 94:
        return WORKS_FLOORS["ControlFloor"]
    if z < 44:
        return WORKS_FLOORS["TugFloor"]
    if z < 100:
        return WORKS_FLOORS["HaulerFloor"]
    if z < 152:
        return WORKS_FLOORS["LinerFloor"]
    if z < 162:
        return round(WORKS_FLOORS["LinerFloor"] + (WORKS_FLOORS["BargeFloor"] - WORKS_FLOORS["LinerFloor"]) * (z - 152) / 10, 2)
    return WORKS_FLOORS["BargeFloor"]


# ------------------------------------------------------------------ the routes
def ways_out(scene, to, near=None):
    return prompt_of(scene, f"the way from {scene} to {to}", to=to, near=near)


def way_in():
    r = Checked(PREFIX + "53-the-way-to-nacre", "Chapter 9: Harrowgate's field to the quay, the Picket's north end, the warp to Nacre Grave, Bryn at the outer marker, the outer tows, the tug", chapter=9)
    # Chapter 8 ends in the cells with Abri's word (home.turnedForNacre), saved at the field's Verdict spawn: the arrow points at the warp to Nacre off the Picket's north end, a
    # launch from the quay away (D273, D283). The verdict is given: the harbour gate's leaves are open, Hessa stands at the armoury gate, Odrun in her kitchen.
    r.resume("Harrowgate", "home.turnedForNacre", spawn=T["VerdictSpawn"])
    r.speed = WALK
    r.lead("Taren")
    r.town_people = [T["HessaGate"], T["OdrunAt"], T["HessaArmoury"]]
    r.stands += [((0, 84), 13), ((0, 66), 6)]
    r.go("off the field and down through the town to the quay", [T["FieldSouthAt"], (0, 66)] + hr.FROM_FORECOURT + [(8, 35), (8, 32), (8, 27), T["GateFoot"]] + hr.QUAY_EAST[::-1], tolerance=0.5)
    pad = ways_out("Harrowgate", "PicketOfTeeth")
    r.stand_at("the quay's landing pad", (pad["at"][0], pad["at"][1] + 2.6), pad, tolerance=0.4)
    r.leave("launch for the Picket of Teeth", "PicketOfTeeth", hr.P["FromTown"], "Flight")
    r.speed = FLIGHT_SPEED
    warp = ways_out("PicketOfTeeth", "NacreGrave")
    r.walk("toward the warp to Nacre", warp["at"][0] - 8, 1, warp["at"][1] - 4, tolerance=3)
    r.stand_at("the warp to Nacre Grave", (warp["at"][0] - 3.9, warp["at"][1] + .4), warp, tolerance=0.6, flying=True)
    r.leave("take the warp to Nacre Grave", "NacreGrave", NACRE_ARRIVAL, "Flight", flying=True)
    r.wait("first look at the wreck field", 2, expectedFlag="home.turnedForNacre")
    nacre_outer_pass(r, first=True)
    tug = prompt_of("NacreGrave", "the tug", flag="home.tugFound")
    # Her raft lies either side of her in the lane (turns 163 to 189): round its outer side to her hail.
    r.walk("out round the tug's raft", lane(163, 12.5)[0], 1, lane(163, 12.5)[1], tolerance=2)
    r.stand_at("the tug Welcome Back", tug["at"], tug, tolerance=1, flying=True)
    r.hail("hail the tug Welcome Back", 5, "home.tugFound")  # BrynWelcomeBack: the point saves
    r.wait("the tug is found", 1, expectedFlag="home.tugFound")
    r.write()


def nacre_outer_pass(r, first):
    """From the arrival east and south down the outer pass, on its quiet side (seven metres out from the lane's middle): Bryn's line across the lane at turn 8 the first
    time, then the outer tows' fight (its box takes in the quiet side at turn 95)."""
    r.lead("Taren")
    # The outer marker's buoy stands seven metres outward at turn -1.5: the first leg keeps to the lane's middle past it.
    first_leg = lane(14, -1)
    if first:
        r.walk("east past the outer marker until Bryn speaks", first_leg[0], 1, first_leg[1], tolerance=3, stop="ui:dialogue")
        r.crossing("Bryn at the outer marker", 7, "home.nacreSpoken")  # BrynNacreApproach: the rule, said once
    r.walk("east past the outer marker", first_leg[0], 1, first_leg[1], tolerance=3)
    for turn in (20, 50, 80):
        p = lane(turn, 7)
        r.walk(f"along the outer pass's quiet side, turn {turn}", p[0], 1, p[1], tolerance=3)
    into = lane(95, 7)
    r.walk("into the outer tows", into[0], 1, into[1], tolerance=3)
    if first:
        r.fight_flight("Nacre_TowsOuter", rounds=4)
        r.fought()
    for turn in (110, 140):
        p = lane(turn, 7)
        r.walk(f"along the outer pass's quiet side, turn {turn}", p[0], 1, p[1], tolerance=3)


def nacre():
    r = Checked(PREFIX + "54-nacre-grave", "Chapter 9: the long way round from the arrival, the inner tows, the inner pass, Oda's hauler, the basin, the berth ring, the navigators' pod, the chart to Many Hands", chapter=9)
    r.resume("NacreGrave", "home.tugFound", speed=FLIGHT_SPEED, spawn=NACRE_ARRIVAL, form="Flight")
    nacre_outer_pass(r, first=False)
    # Past the tug on her outer side, by her hail: her hull stands four metres out from the lane's middle, and the quiet side is seven.
    for name, p in (("out round the tug's raft", lane(163, 12.5)), ("past the tug by her hail", NP["TugHail"]), ("on past the raft", lane(183, 12.8)), ("on along the outer edge", lane(190, 12.7)), ("back to the quiet side", lane(198, 9))):
        r.walk(name, p[0], 1, p[1], tolerance=2)
    for turn in (200, 230):
        p = lane(turn, 7)
        r.walk(f"along the outer pass's quiet side, turn {turn}", p[0], 1, p[1], tolerance=3)
    into = lane(255, 7)
    r.walk("into the inner tows", into[0], 1, into[1], tolerance=3)
    r.fight_flight("Nacre_TowsInner", rounds=4)
    r.fought()
    for turn in (290, 320, 350, 380, 410, 425):
        p = lane(turn, 7 if turn < 425 else 1)
        r.walk(f"round the inner pass, turn {turn}", p[0], 1, p[1], tolerance=3)
    # Oda's hauler stands just inside the mouth (turn 62, r 61), her hail five metres out from her hull: a hull is stood further out, in the mouth's open strip, and the
    # tow line runs between (its tows at eight metres a second here): waited for, then crossed.
    oda = prompt_of("NacreGrave", "Oda's hauler", flag="home.odaFound")
    stand = basin(62, 68.6)
    r.brake = True
    r.walk("settle on the inner pass short of the tow line", r.at[0], 1, r.at[1], tolerance=1.5, seconds=6)
    r.brake = False
    r.wait_clear("wait for the tow line to be clear", [stand], 40, brake=True)
    r.stand_at("Oda's hauler", stand, oda, tolerance=0.8, flying=True)
    r.hail("hail line nine hundred and four", 8, "home.odaFound")  # OdaFound
    # Round the basin outside the berth ring (r 48, its wrecks at fifteen metres a second), north and west, to where its crossing toward the pod begins.
    for turn, radius in ((50, 67.5), (35, 61), (10, 60), (340, 60), (310, 58)):
        p = basin(turn, radius)
        r.walk(f"round the basin, turn {turn}", p[0], 1, p[1], tolerance=2.5)
    start, ring, inside = basin(300, 56), basin(300, 48), basin(300, 40)
    r.walk("to the berth ring's edge", start[0], 1, start[1], tolerance=2)
    r.brake = True
    r.walk("settle at the berth ring's edge", start[0], 1, start[1], tolerance=1, seconds=6)
    r.brake = False
    pod = prompt_of("NacreGrave", "the navigators' pod", flag="home.navigatorsFound")
    # The pod's hail is four metres from its hull: a hull is stood five metres off it, along the line out from the pod through its hail.
    away = (pod["at"][0] - NP["Pod"][0], pod["at"][1] - NP["Pod"][1])
    stand = (round(NP["Pod"][0] + away[0] / math.hypot(*away) * 5.2, 2), round(NP["Pod"][1] + away[1] / math.hypot(*away) * 5.2, 2))
    r.wait_clear("wait for the berth ring to be clear", [ring, inside], 40, brake=True)
    r.walk("across the berth ring", ring[0], 1, ring[1], tolerance=2)
    r.walk("inside the berth ring", inside[0], 1, inside[1], tolerance=2)
    r.stand_at("the navigators' pod", stand, pod, tolerance=0.8, flying=True)
    r.hail("hail the navigators' pod", 4, "home.navigatorsFound")  # BrynNavigators: with the hauler and the tug, home.nacreDone
    r.wait("all three lamps are found", 1, expectedFlag="home.nacreDone")
    # Home by Sela's chart, nothing awake near: the arrow's way (the warp off the arrival, the hull, the dock) is the whole spiral back out.
    r.chart("ManyHands", "ManyHands", spawn=H["Arrival"], form="Natural")
    r.speed = WALK
    r.wait("first look at the dock", 2, expectedFlag="home.nacreDone")
    r.write()


def town_movers():
    r = Checked(PREFIX + "55-many-hands-and-the-crawl", "Chapter 9: Meret's notice, Bryn's chart lesson, Maru names the Crawl, the chart to the Meridian Crawl, Dorrow at the helm, the drop cage", chapter=9)
    r.resume("ManyHands", "home.nacreDone", spawn=H["Arrival"])
    r.speed = WALK
    r.lead("Taren")
    r.people = {"Corra", "Dov", "Hadda", "Binna", "Ruva", "Teo", "Garo", "Dabb", "Maru", "Ona", "Pim", "Pell"}
    # Meret's notice is a crossing across the dock a step north of the pads, 16 by 3 on (0, -50.5), once all three lamps are in.
    r.walk("up off the pads", 0, 0, -55, tolerance=0.5)
    r.walk("up the dock until Meret's notice", 0, 0, -40, stop="ui:dialogue")
    r.crossing("Meret's notice to all stations", 5, "home.noticeHeard")  # MeretBerthNotice
    r.walk("on up the dock", 0, 0, -40)
    lesson = prompt_of("ManyHands", "Bryn's chart lesson", flag="home.chartLent")
    r.go("up to the chart table", [(0, -20), H["Crossing"]] + mh.SPINE_UP + [H["StairFoot"], H["StairTop"]], tolerance=0.4)
    r.stand_at("Bryn at the chart table", (lesson["at"][0], lesson["at"][1] - .4), lesson, tolerance=0.4, y=0)
    r.hail("ask Bryn about her chart", 7, "home.chartLent")  # BrynChartLesson: the pencil
    maru = prompt_of("ManyHands", "Maru names the Crawl", flag="home.crawlNamed")
    r.go("down to the raft bay", [H["StairTop"], H["StairFoot"], (0, 20)] + mh.SPINE_DOWN + [(0, -20), (0, -24), (-14.5, -24), (-19.5, -24)], tolerance=0.5)
    r.stand_at("Maru in the raft bay", (-24, -24), maru, tolerance=0.5, y=0)
    r.hail("ask Maru who has moved a town", 5, "home.crawlNamed")  # IdraTownMovers: charts the Crawl
    r.chart("MeridianCrawl", "MeridianCrawl", spawn=CR["Arrival"], form="Natural")
    r.wait("first look at the Crawl", 2, expectedFlag="home.crawlNamed")
    dorrow = prompt_of("MeridianCrawl", "Dorrow at the helm", prompt="Talk to Chief Engineer Dorrow")
    r.go("along the spine to the helm", [(2, -3), (-14, -3), (-20, -3), (-54, -3), (-60, -3)] + HELM_RAIL + [CR["HelmFoot"], CR["HelmHead"]], tolerance=0.5)
    r.stand_at("Dorrow at the helm", CR["DorrowHand"], dorrow, tolerance=0.4)
    r.talk("Dorrow at the helm", 8, flag="home.crawlAsked")  # DorrowFirst: the run opens
    cage = ways_out("MeridianCrawl", "Terminator")
    r.go("back along the spine to the tail", [CR["HelmHead"], CR["HelmFoot"]] + HELM_RAIL[::-1] + [(-54, -3), (-20, -3), (-14, -3), (18, -3), (24, -3)] + STAIR_RAILS + [(55, -3), (55, 2)], tolerance=0.5)
    r.stand_at("the drop cage", CR["CageHand"], cage, tolerance=0.4)
    r.leave("ride the drop cage down", "Terminator", RN["Arrival"], "Shaped")
    r.speed = COMBAT_SPEED
    r.wait("first look at the band", 1, expectedFlag="home.crawlAsked")
    r.write()


def terminator():
    r = Checked(PREFIX + "56-the-terminator", "Chapter 9: the run south ahead of the dawn, three couplings, the ride, Dorrow's crawler, the hall home in the Crawl, the launch to the Decks", chapter=9)
    r.resume("Terminator", "home.crawlAsked", speed=COMBAT_SPEED, spawn=RN["Arrival"])
    r.lead("Taren")
    # The line stands 11 m north and moves once the lead has walked 6 m or 8 s have gone: set off at once, and keep moving. The fights are no gates (the line takes what it
    # passes); a coupling is taken with one press and the pad let go until it is mated (the driver's coupling hand), pressed again if a stagger lets it go.
    leg = 0
    for kind, p in RUN_WAY:
        if kind == "way":
            leg += 1
            if r.at is not None and math.dist(r.at, p) < .05:
                continue
            r.walk(f"south along the trodden way {leg}", p[0], 0, p[1], tolerance=1.2)
        elif kind == "seat":
            c = COUPLINGS[p]
            # The way's leg to the engine car's seat crosses the car's front gallery (Terminator.Way: a finding): round its east end, as the floor goes.
            x0, z0, x1, z1 = RUN_GALLERY
            if r.at is not None and any(x0 - BODY < q[0] < x1 + BODY and z0 - BODY < q[1] < z1 + BODY for q in r.legs(c["at"])):
                r.walk("round the engine car's gallery", x1 + .3, 0, z0 - .6, tolerance=0.5)
            r.stand_at(f"coupling {p + 1}'s seat", c["at"], c, tolerance=0.5, y=0)
            r.press_for(f"re-mate coupling {p + 1}", c["flag"], seconds=24)
        else:
            if RIDE is None:
                fail("the Terminator has no 'Ride the tread-hall out' (a stale scene?)")
            r.stand_at("the engine car's board", (p[0] + .6, p[1]), RIDE, tolerance=0.5, y=0)
            # The ride: input is the car's for about fourteen seconds, and the party is set down at the tail with home.rideDone.
            r.press_for("ride the tread-hall out", "home.rideDone", seconds=40)
    r.at = RN["Tail"]
    r.wait("set down at the tail", 1.5, expectedFlag="home.rideDone")
    crawler = ways_out("Terminator", "MeridianCrawl")
    r.go("to Dorrow's crawler", RUN_HOME[1:-1], tolerance=0.5)
    # The doc's stand is 3.04 m from the way's 3.4: in reach, with room for a body that stops two tenths short of it.
    r.stand_at("Dorrow's crawler", RUN_HOME[-1], crawler, tolerance=0.2)
    r.leave("climb aboard the Meridian Crawl", "MeridianCrawl", CR["GapSpawn"], "Natural")
    r.speed = WALK
    r.crossing("Dorrow brings tread-hall four home", 4, "home.treadRecovered", wait=14)  # DorrowTreadHome: an AfterTalk as the hall rolls in
    pad = ways_out("MeridianCrawl", "DecksLeaving")
    r.go("along the spine to the yard", [(55, -3)] + STAIR_RAILS[::-1] + [(24, -3), (18, -3), (2, -3)], tolerance=0.5)
    r.stand_at("the yard's pad", (CR["Pad"][0], CR["Pad"][1] - 3), pad, tolerance=0.4)
    r.leave("launch for the Decks", "DecksLeaving", DECKS_ARRIVAL, "Natural")
    r.wait("home", 2, expectedFlag="home.treadRecovered")
    r.write()


def decks():
    r = Checked(PREFIX + "57-the-decks-on-the-day", "Chapter 9: Orrin at his board, the town meeting's eight talks, the way down to the Separation Works", chapter=9)
    r.resume("DecksLeaving", "home.treadRecovered", spawn=DECKS_ARRIVAL)
    r.speed = WALK
    r.lead("Taren")
    r.decks_people = [DK["NoticeBoardStand"], DK["LockNineStand"], DK["StampingWindowStand"]]
    # Orrin at his board in the dock office, talkable until home.returned: up gate six's way to the office's front.
    r.go("up into the dock office", [(-28, -8.6), (-28, -3), (-28, 3), (-28, 6.55)], tolerance=0.4)
    orrin = MEETING["Orrin"]
    stand = (-29.6, 6.55)
    if math.dist(stand, orrin) + .4 + INSIDE > TALK:
        fail(f"{r.name}: Orrin at {orrin} is out of a talk's reach from {stand}")
    r.stands.append((stand, 2))
    r.walk("to Orrin at his board", stand[0], 0, stand[1], tolerance=0.4)
    r.talk("Orrin at his board", 5, flag="home.returned")  # OrrinHomecoming
    join = prompt_of("DecksLeaving", "the meeting", flag="home.meetingGathered")
    r.go("back to the office's front", [(-28, 6.55), (-28, 3)], tolerance=0.4)
    r.stand_at("the town meeting", (join["at"][0], join["at"][1] + .9), join, tolerance=0.4, y=0)
    # The town gathers: the zone loads again at the back of the room, the day's people at the meeting (Decks.Meeting).
    r.press_for("join the town meeting", "home.meetingGathered", seconds=6)
    r.wait("the town gathers", 3)
    r.add("the room is full", 25, until="ui:world", expectedUi="world")
    r.at = MEETING_SPAWN
    r.benches = MEETING_BENCHES
    # Each talk in its turn (TurnToSpeak): Orrin, Meret, Hal, Mira, Ilo, Oda, Orrin twice. The front bench row ends at z 5.81, the dock office's front is walked along
    # z 6.55 (Orrin stands at his board 1.15 m north of it), and Mira stands on the front's way, so the walk east goes round her north side.
    front_w, front_e = (-29.6, 6.55), (-33.4, 6.55)
    turns = [("Orrin opens the meeting", "Orrin", [(-28, 1.2), (-28, 6.55), front_w], front_w, 3, "home.meetingOpened"),
             ("Meret's berth offer", "Meret", [front_e], front_e, 5, "home.offerMade"),
             ("Hal and his apprentices", "Hal", [(-28, 6.55), (-26, 7.9), (-21.2, 7.9)], (-21.2, 7.6), 5, "home.halAsked"),
             ("Mira's manifest", "Mira", [(-23.6, 7.9)], (-23.6, 8.0), 5, "home.manifestShown"),
             ("Ilo reads the names", "Ilo", [(-26, 7.9), (-28, 6.55), (-33.8, 6.55)], (-33.8, 6.55), 4, "home.namesRead"),
             ("Oda stands", "Oda", [(-28.6, 6.55)], (-28.6, 2.6), 4, "home.odaStood"),
             ("Orrin postpones nothing", "Orrin", [(-28.6, 6.0)], front_w, 5, "home.orrinSaid"),
             ("Meret withdraws the offer", "Orrin", [], front_w, 6, "home.meetingHeld")]
    for what, who, via, stand, lines, flag in turns:
        at = MEETING[who]
        if math.dist(stand, at) + .3 + INSIDE > TALK:
            fail(f"{r.name}: {who} at {at} is out of a talk's reach from {stand}")
        r.decks_people = [p for k, p in MEETING.items() if k != who]
        r.stands += [(p, 1.6) for p in via + [stand]]
        if via:
            r.go(f"to {who} for {what.lower()}", via, tolerance=0.3)
        if r.at is None or math.dist(r.at, stand) > .05:
            r.walk(f"stand for {what.lower()}", stand[0], 0, stand[1], tolerance=0.3)
        r.talk(what, lines, flag=flag)
    r.wait("the meeting is held", 1, expectedFlag="home.meetingHeld")
    r.decks_people = list(MEETING.values())
    gate = ways_out("DecksLeaving", "SeparationWorks")
    r.go("out of the office and along the promenade to the market", [(-28, 6.55), (-28, 1.2), (-28, -3), (-12.6, -3), (-11.3, -3.75), (-7.2, -3.75), (-6, -3), (0, -3), (0, -11)], tolerance=0.4)
    r.benches = []
    r.stand_at("the gate down to the Works", (6, -11.2), gate, tolerance=0.4, y=0)
    r.leave("go down to the Separation Works", "SeparationWorks", WK["Arrival"], "Shaped")
    r.speed = COMBAT_SPEED
    r.wait("first look under the promenade", 1, expectedFlag="home.meetingHeld")
    r.write()


def works():
    r = Checked(PREFIX + "58-separation-works", "Chapter 9: Dorrow under the promenade, joints 1 to 3 and the rigs and crews between them, the press hall's cradle", chapter=9)
    r.resume("SeparationWorks", "home.meetingHeld", speed=COMBAT_SPEED, spawn=WK["Arrival"])
    r.lead("Taren")
    r.walk("north up the tug's middle", 0, None, 10)
    r.walk("on until Dorrow speaks", 0, None, 31, stop="ui:dialogue")
    r.crossing("Dorrow under the promenade", 6, "home.worksRead")  # DorrowWorks
    j1 = prompt_of("SeparationWorks", "joint 1", flag="home.joint1")
    r.walk("on up the tug", 0, None, 33, tolerance=0.6)
    r.stand_at("joint 1's jack", (j1["at"][0] - 1.4, j1["at"][1]), j1, tolerance=0.4)
    r.press_for("take joint 1's weight", "home.joint1")
    r.hands_off("joint 1 gives", 5)
    # The throat: the rig alone, locked from the throat's south floor (z 55 to 59), never from a press bay (z 62.4 to 67.4).
    r.go("back to the way and up into the throat", [(0, 34), (0, 40), (0, 56)], tolerance=0.6)
    r.fight_ground("Works_Throat", rounds=4)
    r.walk("through the press bays", 0, None, 72)
    r.go("up the stair to the control room", [(0, 84), (12, 82), (18, 82), (24, 84)], tolerance=0.6)
    r.fight_ground("Works_ControlRig", rounds=4)
    r.into("Works_Control")
    r.fight_ground("Works_Control", rounds=4)
    j2 = prompt_of("SeparationWorks", "joint 2", flag="home.joint2")
    r.go("back down from the control room", [(24, 84), (18, 82), (12, 82), (0, 84), (0, 94), (0, 96)], tolerance=0.6)
    r.stand_at("joint 2's jack", (j2["at"][0] - 1.6, j2["at"][1] + .4), j2, tolerance=0.4)
    r.press_for("take joint 2's weight", "home.joint2")
    r.hands_off("joint 2 gives", 5)
    r.go("north into the gallery", [(0, 96), (0, 104)], tolerance=0.6)
    r.fight_ground("Works_GalleryRig", rounds=4)
    r.into("Works_Gallery")
    r.fight_ground("Works_Gallery", rounds=5)
    j3 = prompt_of("SeparationWorks", "joint 3", flag="home.joint3")
    r.walk("on up the gallery", 0, None, 144, tolerance=0.6)
    r.stand_at("joint 3's jack", (j3["at"][0] - 1.4, j3["at"][1]), j3, tolerance=0.4)
    r.press_for("take joint 3's weight", "home.joint3")
    r.hands_off("joint 3 gives", 5)
    cradle = prompt_of("SeparationWorks", "the press hall's cradle", prompt="Press hall cradle")
    r.go("down the stair to the landing", [(0, 144), (0, 152), (0, 162)], tolerance=0.6)
    r.stand_at("the press hall's cradle", (cradle["at"][0] - 1.6, cradle["at"][1]), cradle, tolerance=0.4)
    r.save_at("repair and save at the press hall's cradle", "release the cradle")
    r.write()


def saint():
    r = Checked(PREFIX + "59-the-foundry-saint", "Chapter 9: the Foundry Saint at its collars and on its back, Orrin on the speaker, the lift up to the Decks", chapter=9)
    r.resume("SeparationWorks", "home.joint3", speed=COMBAT_SPEED, spawn=WK["Checkpoint"])
    r.lead("Taren")
    r.stands += [(WK["Checkpoint"], 3.5), ((-1.5, 169), 3.5), ((0, 186), 13)]
    # The door: from the checkpoint (PressHall, (-3, 165)) through the door at z 172 to well inside the fight's volume (z 174 to 200), so the partner is through too. The
    # driver strikes a collar its arm has planted, and with three arms down climbs one to the press's back (ContinuousReview.Home.cs).
    hr.boss(r, "FoundrySaint", "through the press hall's door", 0, WORKS_FLOORS["BargeFloor"], 179, spawn=WK["Checkpoint"], rounds=12)
    r.add("reach the fallen hero after the Saint", 16, navigate=True, approachPartner=True, tolerance=1.2, stopWhen="partyAlive", point=dict(x=0, y=0, z=0))
    r.add("both heroes ready after the Saint", 6, until="partyAlive")
    r.crossing("Orrin on the press's speaker", 3, "home.saintReleased", wait=10)  # OrrinSaintSpeaks: an AfterTalk 2.2 s after the fall
    r.lead("Taren")
    r.fought()
    lift = ways_out("SeparationWorks", "DecksLeaving", near=WK["LiftAt"])
    r.stands.append((lift["at"], 4))
    r.stand_at("the lift home", (lift["at"][0] - 1.2, lift["at"][1] - 1.2), lift, tolerance=0.4)
    r.leave("ride the lift up to the Decks", "DecksLeaving", LIFT_SPAWN, "Natural")
    r.speed = WALK
    r.wait("up on the Decks", 2, expectedFlag="home.saintReleased")
    r.write()


HERO = 0.35  # a hero's body on foot (ActorFactory's capsule): what a walk's legs keep off a solid by, and a tenth more
# The Coronach walk's own ways to the doors the Decks' ways don't reach (HomeBuilder.Coronach.DoorWays): its people keep off them, and the route walks them.
DOOR_WAYS = [mh.points(part, {}) for part in re.findall(r"new\[\]\{(.*?)\}", block(source("HomeBuilder.Coronach.cs"), "internal static readonly Vector2[][] DoorWays=")[1:-1])]


def door_way(stand):
    """The walk's own way that ends at this door's stand, or None (a door the Decks' ways pass)."""
    return next((w for w in DOOR_WAYS if math.dist(w[-1], stand) < .05), None)


def coronach_solids(scene="DecksLeaving", root="The Coronach"):
    """Everything solid the Coronach walk stands, as built (every box collider under its root in the scene: its people, the crates set beside them), as (label, plan centre,
    yaw, half x, half z): every side story done, so everyone who can stand in the walk does; a hero the route plays (PAIR) walks with the party and never stands at their place.
    Read, never written. A walk across the dock office's front to Oda's window went through Jask, set there by the build 4.4 m from the stair he stands for (route 60, the
    first chain to play the Coronach, November 14): the walk's people are read as built, never assumed off the legs."""
    text = (SCENES / (scene + ".unity")).read_text(encoding="utf-8")
    docs = {m.group(2): (m.group(1), m.group(3)) for m in re.finditer(r"^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- |\Z)", text, flags=re.M | re.S)}
    tr, of_object = {}, {}
    for fid, (kind, body) in docs.items():
        if kind != "4":
            continue
        found = [re.search(p, body) for p in (r"m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", r"m_LocalRotation: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}",
                                              r"m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", r"m_Father: \{fileID: (\d+)\}", r"m_GameObject: \{fileID: (\d+)\}")]
        if all(found):
            tr[fid] = (found[3].group(1), tuple(map(float, found[0].groups())), tuple(map(float, found[1].groups())), tuple(map(float, found[2].groups())), found[4].group(1))
            of_object[found[4].group(1)] = fid
    names = {fid: unquote(re.search(r"m_Name: (.*)", body).group(1)) for fid, (kind, body) in docs.items() if kind == "1" and "m_Name: " in body}

    def rotate(q, v):
        x, y, z, w = q
        ux, uy, uz = 2 * (y * v[2] - z * v[1]), 2 * (z * v[0] - x * v[2]), 2 * (x * v[1] - y * v[0])
        return (v[0] + w * ux + y * uz - z * uy, v[1] + w * uy + z * ux - x * uz, v[2] + w * uz + x * uy - y * ux)

    def mul(a, b):
        ax, ay, az, aw = a
        bx, by, bz, bw = b
        return (aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx, aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz)

    def world(fid):
        parent, pos, rot, scale, _ = tr[fid]
        if parent not in tr:
            return pos, rot, scale
        ppos, prot, pscale = world(parent)
        local = rotate(prot, (pos[0] * pscale[0], pos[1] * pscale[1], pos[2] * pscale[2]))
        return (ppos[0] + local[0], ppos[1] + local[1], ppos[2] + local[2]), mul(prot, rot), (pscale[0] * scale[0], pscale[1] * scale[1], pscale[2] * scale[2])

    def chain(fid):
        while fid in tr:
            yield fid
            fid = tr[fid][0]
    top = next((fid for fid, t in tr.items() if t[0] == "0" and names.get(t[4]) == root), None)
    if top is None:
        fail(f"{scene} has no '{root}' (a stale scene, or the walk isn't staged in it)")
    # The heroes' own places (CoronachDoor.hero, its body): never standing for the pair the route plays.
    out_of_it = set()
    for kind, body in docs.values():
        hero, held = re.search(r"^  hero: (\w+)$", body, flags=re.M), re.search(r"^  body: \{fileID: (\d+)\}", body, flags=re.M)
        if kind == "114" and hero and held and hero.group(1) in PAIR and held.group(1) in of_object:
            out_of_it.add(of_object[held.group(1)])
    solids = []
    for fid, (kind, body) in docs.items():
        if kind != "65" or "m_IsTrigger: 0" not in body or "m_Enabled: 1" not in body:
            continue
        holder = of_object.get(re.search(r"m_GameObject: \{fileID: (\d+)\}", body).group(1))
        if holder is None or top not in chain(holder) or out_of_it & set(chain(holder)):
            continue
        size, centre = (tuple(map(float, re.search(p, body).groups())) for p in (r"m_Size: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", r"m_Center: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}"))
        pos, rot, scale = world(holder)
        c = rotate(rot, (centre[0] * scale[0], centre[1] * scale[1], centre[2] * scale[2]))
        yaw = math.degrees(2 * math.atan2(rot[1], rot[3]))
        label = "/".join(names.get(tr[f][4], "?") for f in list(chain(holder))[:2][::-1])
        solids.append((label, (pos[0] + c[0], pos[2] + c[2]), yaw, abs(size[0] * scale[0]) / 2, abs(size[2] * scale[2]) / 2))
    return solids


# What the walk's last prompts open with (CoronachDoor.opensWith, as the route table says): the three last words once the seven doors are heard, the close once they are said.
OPENS_WITH = {"coronach.taren": "coronach.doorsHeard", "coronach.sela": "coronach.doorsHeard", "coronach.idra": "coronach.doorsHeard", "home.coronachWalked": "coronach.lastHeard"}
# Points of side stories these routes never start, so never shown on them: Tam's three things (StillGround, step 0) and Marn's bench (PortableBenches). A route that takes them up
# must stand clear of them too: the marked doorframe's "Show Tam" (-29.4, 8.9) is nearer Orrin's board stand than Orrin is.
UNSTARTED = ("tam.", "bench.marn")


def coronach():
    r = Checked(PREFIX + "60-the-coronach", "Chapter 9: Hal's last machine, the call, the seven doors, the three last words, Orrin at the joint, the launch, the cut to the Ring", chapter=9)
    r.resume("DecksLeaving", "home.saintReleased", spawn=LIFT_SPAWN)
    r.speed = WALK
    r.lead("Taren")
    stands = check_decks_docs()
    doors = {p["flag"]: p for p in scene_prompts("DecksLeaving") if p["flag"].startswith("coronach.") or p["flag"] == "home.coronachWalked"}
    heard = {"home.coronachCalled"}

    def door(what, flag, via):
        tolerance = 0.3
        stand = stands.get(flag)
        if stand is None:
            fail(f"docs/maps/DecksLeaving.md's route table has no stand for {flag}")
        prompt = doors.get(flag) or fail(f"DecksLeaving has no prompt that sets {flag}")
        # The prompt meant has to be the one the game offers wherever within the walk's tolerance the body stops: nearer by twice the tolerance than every other prompt in reach
        # there (the walk's doors, guests and stories, every story finished being the worst case, the day's docks and ways), but those already heard.
        here = math.dist(stand, prompt["at"])

        def opened(p):
            needs = OPENS_WITH.get(p["flag"]) or p["requires"]
            return not needs or needs in heard
        # The day's people (each a "Talk to ..." Npc) have gone at the call (their Whereabouts, goneAt home.coronachCalled; the day's Orrin at home.halChose).
        rivals = [p for p in scene_prompts("DecksLeaving") if p is not prompt and p["range"] > 0 and p["flag"] not in heard and not p["prompt"].startswith("Talk to")
                  and not p["flag"].startswith(UNSTARTED) and opened(p) and math.dist(stand, p["at"]) < p["range"] + tolerance]
        nearer = [p for p in rivals if math.dist(stand, p["at"]) - tolerance <= here + tolerance]
        if nearer:
            fail(f"{r.name} '{what}': from within {tolerance} of {stand}, '{nearer[0]['name']}' at {math.dist(stand, nearer[0]['at']):.2f} m can be as near as '{prompt['name']}' at {here:.2f} m")
        # A door off the Decks' ways is walked to along the walk's own way to it (DoorWays), the one its people are kept off.
        way = door_way(stand)
        if way and (not via or math.dist(via[-1], way[-2]) > .05 or not any(math.dist(q, way[0]) < .05 for q in via)):
            fail(f"{r.name} '{what}': the walk to {stand} doesn't come along the walk's own way to it, {way} (HomeBuilder.Coronach.DoorWays)")
        if via:
            r.go(f"to {what}", via, tolerance=0.4)
        r.stand_at(what, stand, prompt, tolerance=tolerance, y=0)

    # Hal's scene: Hal at his bench once the Saint is released (HalLastMachine, a beat on him), at the end of the tug dock's way to the bench.
    hal = DK["BoltHolesStand"]
    hal_stand = DECKS_WAYS[3][-1]
    if math.dist(hal_stand, hal) + .3 + INSIDE > TALK:
        fail(f"{r.name}: Hal at {hal} is out of a talk's reach from {hal_stand}")
    r.stands.append((LIFT_SPAWN, 3))
    r.go("to Hal at his bench", [DECKS_WAYS[3][-2], hal_stand], tolerance=0.3)
    r.talk("Hal's last machine", 7, flag="home.halChose")  # HalLastMachine
    # The call: a crossing along the workshop's whole open front (z 0); Orrin is there.
    r.stands += [((31, 2.2), 2), ((31, -1), 1.5)]
    r.walk("back into the workshop", 31, 0, 2.2, tolerance=0.4)
    r.walk("out across the workshop's front until Orrin speaks", 31, 0, -1, stop="ui:dialogue")
    r.crossing("Orrin calls the Coronach", 3, "home.coronachCalled", wait=4)  # OrrinCallsCoronach
    # From the call the walk's people stand at their places: every leg keeps a body clear of them.
    r.walk_solids = coronach_solids()
    r.walk("out onto the promenade", 31, 0, -1, tolerance=0.4)
    # The doors off the Decks' ways are walked to along the walk's own ways (HomeBuilder.Coronach.DoorWays), each from a Decks way to the door's stand and back the same way:
    # across the dock office's front south of its crated benches to Oda, up the office's east side to the board, off gate six's way to Sela, off lock nine's to Neve.
    r.walk_ways = DOOR_WAYS
    OFFICE, BOARD, GATE, LOCK = (door_way(stands[f]) or fail(f"HomeBuilder.Coronach.DoorWays has no way to {f}'s stand {stands[f]}")
                                 for f in ("coronach.oda", "coronach.orrin", "coronach.sela", "coronach.neve"))
    walked = [("Hal at the bolt holes", "coronach.hal", [(31, 2.2)], 2),
              ("Neve at lock nine", "coronach.neve", [(31, -1), (28, -3), (6, -3), (0, -3)] + LOCK[:-1], 2),
              ("Jun at the yellow line", "coronach.jun", LOCK[-2::-1] + [(0, -3), (-6, -3), (-7.2, -3.75), (-11.3, -3.75)], 3),
              ("Mira at pitch nine", "coronach.mira", [(-11.3, -3.75)], 3),
              ("Ilo at the notice board", "coronach.ilo", [(-11.3, -3.75), (-12.6, -3), (-28, -3), (-28, -8.6)], 2),
              ("Oda at the stamping window", "coronach.oda", [(-28, -8.6), (-28, -3)] + OFFICE[:-1], 3)]
    for what, flag, via, lines in walked:
        door(what, flag, via)
        r.hail(f"stop and listen: {what.lower()}", lines, flag)
        heard.add(flag)
    # Orrin at his board: offered only once he stands there, about forty seconds after his call (he walks to it along the town's ways). Held for, never timed.
    door("Orrin at his board", "coronach.orrin", OFFICE[-2::-1] + [(-28, 6.8)] + BOARD[:-1])
    r.offered("wait until Orrin stands at his board", "Stop and listen", seconds=90)
    r.hail("stop and listen: orrin at his board", 2, "coronach.orrin")
    heard.add("coronach.orrin")
    r.wait("the seven doors are heard", 1, expectedFlag="coronach.doorsHeard")
    heard.add("coronach.doorsHeard")
    for what, flag, via, lines in (("Taren's last word", "coronach.taren", BOARD[-2::-1] + [(-28, 6.8), (-28, -3), (-13.6, -3)], 2),
                                   ("Sela's last word", "coronach.sela", [(-13.6, -3), (-28, -3)] + GATE[:-1], 2),
                                   ("Idra's last word", "coronach.idra", GATE[-2::-1] + [(-28, -3), (-32.44, -3)], 1)):
        door(what, flag, via)
        r.hail(f"say yours: {what.lower()}", lines, flag)
        heard.add(flag)
    r.wait("the three last words are said", 1, expectedFlag="coronach.lastHeard")
    heard.add("coronach.lastHeard")
    close = doors["home.coronachWalked"]
    stand = stands["home.coronachWalked"]
    r.go("to Orrin at the walkway joint", [(-32.44, -3), (-13.6, -3)], tolerance=0.3)
    r.stand_at("Orrin at the walkway joint", stand, close, tolerance=0.3, y=0)
    r.offered("wait until Orrin stands at the joint", "Stand with Orrin", seconds=60)
    r.hail("stand with Orrin", 2, "home.coronachWalked")  # OrrinGiveItRoom
    # The launch: the party is held about seven seconds as the joints open, then NeveFirstOut, then the cut to the Ring.
    r.crossing("the market district lifts", 6, "home.launched", wait=20)  # NeveFirstOut
    r.scene = "BrokenRing"
    r.add("the cut to the Broken Ring", 40, until="scene:BrokenRing", expectedUi="world", expectedForm="Flight")
    r.add("release after the cut", 0.5)
    r.at, r.speed = RG["Arrival"], FLIGHT_SPEED
    r.wait("first look at the Ring", 2, expectedFlag="home.launched")
    r.write()


def ring():
    r = Checked(PREFIX + "61-the-broken-ring", "Chapter 9: the market's lee, two interceptor pairs, the channel, the formation, the Debris Cutter, the tow, the warp home, the fleet's chart", chapter=9)
    r.resume("BrokenRing", "home.launched", speed=FLIGHT_SPEED, spawn=RG["Arrival"], form="Flight")
    r.lead("Taren")
    # North in the market's lee, x -15: the districts lift as the lead passes z 10, 70 and 130 (SECTION_LIFTS); nothing to press.
    r.walk("north in the market's lee", RG["Arrival"][0], 1, 40, tolerance=3)
    r.walk("on north in the lee", RG["Arrival"][0], 1, 100, tolerance=3)
    r.walk("toward the first pair", RG["Pair1"][0], 1, RG["Pair1"][1] - 6, tolerance=3)
    r.fight_flight("Ring_Pair1", rounds=4)
    r.fought()
    # The channel (x -6 to 6) at z 150 to 160, crossed when its lamps there are cold: the driver waits on its wrecks (ContinuousReview.Home.cs).
    # The east bank at x 11, where the world engineer's way north past the second wreck runs (docs/maps/BrokenRing.md, "For the replay route", step 4). Crossed to x 15
    # at full speed the ship was still doing 15.9 m/s east when the leg turned it north-west, ran on 5 m into the wreck at (20, 172) and stuck on its south corner
    # until "past the second pair's cover" timed out (chapter-61's first play, November 14). So: across to x 11, and braked there before the turn north.
    west, east = (-10, 150), (11, 160)
    r.walk("to the channel's west bank", west[0], 1, west[1], tolerance=2)
    r.brake = True
    r.walk("settle at the channel's west bank", west[0], 1, west[1], tolerance=1, seconds=6)
    r.brake = False
    r.wait_clear("wait for the channel to be clear", [east], 40, brake=True)
    r.walk("across the channel", east[0], 1, east[1], tolerance=2)
    r.brake = True
    r.walk("settle on the channel's east bank", east[0], 1, east[1], tolerance=1.5, seconds=3)
    r.brake = False
    # West of the cover wreck at (20, 172) on the way: a hull flew into it straight.
    r.walk("past the second pair's cover", 10, 1, 168, tolerance=2.5)
    r.walk("toward the second pair", RG["Pair2"][0] - 2, 1, RG["Pair2"][1] - 6, tolerance=3)
    r.fight_flight("Ring_Pair2", rounds=4)
    r.fought()
    # The Cutter's door: the checkpoint Ring (0, 240) lies inside its trigger (100 by 100 on C); the fight waits on the formation (ring.formed, two districts on station).
    # A door's walk is timed from its checkpoint, where a retry starts. The first time the lead comes from wherever the second pair fell: it ended 73 m short on its
    # first play, three seconds into the leg, and sat outside the ring through sixteen rounds with nothing to chase (November 14). So a leg north to the ring first.
    r.walk("north to the Cutter's ring", RG["Checkpoint"][0], 1, RG["Checkpoint"][1] - 6, tolerance=3)
    door = (RG["Checkpoint"][0], RG["Checkpoint"][1] + 4)
    r.door("DebrisCutter", "into the Cutter's ring", door[0], 1, door[1], spawn=RG["Checkpoint"], tolerance=3)
    r.add("wait for the districts to form", 150, until="flag:ring.formed", leftTrigger=1)
    # The Cutter keeps about 18 m off and flies to another lane whenever the lead comes nearer. Braked and held to 5 m, the pursuit crept at 2.1 m/s and fired in 7% of
    # sixteen rounds (chapter-61's first play, November 14). The driver's own hands fly after it brake off and fire wherever it is in reach (ContinuousReview.Home.cs,
    # CutterHeading); these rounds are only their frame. (Shots don't weaken with range: it was Idra's prow cutter, a 6 m arc, that dealt nothing from 8 m.)
    close_boss(r, "DebrisCutter", rounds=16)
    r.crossing("the tug takes the Cutter", 3, "home.cutterDisabled", wait=10)  # BrynTowsCutter: an AfterTalk 2.5 s after the fall
    r.fought()
    warp = ways_out("BrokenRing", "ManyHandsHull")
    r.walk("north up the exit lane", warp["at"][0], 1, warp["at"][1] - 20, tolerance=3)
    r.stand_at("the warp home", (warp["at"][0], warp["at"][1] - 4.5), warp, tolerance=0.8, flying=True)
    r.leave("take the warp to Many Hands' hull", "ManyHandsHull", HULL_FROM_RING, "Flight", flying=True)
    dock = ways_out("ManyHandsHull", "ManyHands", near=mh.O["DockAt"])
    r.walk("north toward the stern dock", dock["at"][0] + 2, 1, dock["at"][1] - 14, tolerance=3)
    r.stand_at("the stern dock", (dock["at"][0], dock["at"][1] - 5.4), dock, tolerance=0.6, flying=True)
    r.leave("dock at Many Hands", "ManyHands", H["Arrival"], "Natural", flying=True)
    r.speed = WALK
    r.people = {"Corra", "Dov", "Hadda", "Binna", "Ruva", "Teo", "Garo", "Dabb", "Maru", "Ona", "Pim", "Pell"}
    chart = prompt_of("ManyHands", "the fleet's chart", flag="home.chartRead")
    r.walk("up off the pads", 0, 0, -55, tolerance=0.5)
    r.go("up the dock and the spine to the chart table", [(0, -40), (0, -20), H["Crossing"]] + mh.SPINE_UP + [H["StairFoot"], H["StairTop"]], tolerance=0.4)
    r.stand_at("the fleet's chart", (chart["at"][0], chart["at"][1] - .6), chart, tolerance=0.4, y=0)
    r.hail("see where the roads run", 8, "home.chartRead")  # OrrinChalk: home.chartRead (the point's), chapter9.complete (its last lines)
    r.wait("chapter nine closes", 3, expectedFlag="chapter9.complete")
    r.write()


def close_boss(r, id_, rounds=16, seconds=20, reach=5):
    """manyhands_routes.flight_boss with the pursuit held close: rounds of pursuit within `reach` metres, the ship's shots, a Pulse, and the dodges, until it is down."""
    stop = "flag:bossdown." + id_
    r.press("lock " + id_, "LeftShoulder", 0.2, leftTrigger=1)
    for i in range(rounds):
        r.add(f"pursue {id_} round {i + 1}", seconds, buttons=["South"], leftTrigger=1, navigate=True, approachTarget=True, tolerance=reach, rangedTolerance=reach,
              evadeProjectiles=True, evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=1, z=0))
        r.add(f"pulse {id_} round {i + 1}", 0.18, buttons=["RightShoulder", "West"], leftTrigger=1, stopWhen=stop)
        r.add(f"release pulse {id_} round {i + 1}", 0.45, leftTrigger=1)
    r.add(f"{id_} is down", 4, until=stop, expectedFlag="bossdown." + id_)


ROUTES = {53: way_in, 54: nacre, 55: town_movers, 56: terminator, 57: decks, 58: works, 59: saint, 60: coronach, 61: ring}

if __name__ == "__main__":
    wanted = [int(a) for a in sys.argv[1:]] or sorted(ROUTES)
    for number in wanted:
        if number not in ROUTES:
            fail(f"no chapter 9 route numbered {number} ({', '.join(map(str, sorted(ROUTES)))})")
        ROUTES[number]()
    print("HOME_ROUTES_OK")
