"""Writes chapter 8's replay routes (chapter-44 to chapter-52), Harrowgate, from the builders' own places.

Places are read out of the builders (HarrowgateBuilder.cs: what chapter 8 stands aboard Many Hands; .Picket.cs: the lane, its buoys, its shading rocks, the quiet route,
the lock's pieces and the ways down; .Scoria.cs: the Flows' named places and their declared Ways; .Foundry.cs: the three floors' rooms, the Ways, the wheels, the
cradles, the heart; .Town.cs: the town's places, its Ways, its folk and walkers; .Harbour.cs: the pursuit's water, its cranes and the way back) and checked against the
coordinates tables of their map docs (docs/maps/PicketOfTeeth.md, ScoriaFlows.md, BridleFoundry.md, Harrowgate.md, whose "Pursuit contract" is the harbour's)
before anything is written: a place a route uses that has drifted from its doc stops the run. Every straight leg is checked here before it is written: in the
Picket inside its walls and clear of every rock (the scene's own shadows) and solid by Idra's hull; aboard Many Hands and off its stern by manyhands_routes' checks
(Idra's hull); on the Flows, the Foundry and in Harrowgate along their declared ways (or on the floor before a prompt the ways stop short of), off what the built
scene stands solid and the people standing there; over the war harbour inside its line and off the cranes' legs. HarrowgateRouteTests then checks every leg on the
built zones' navigation and colliders.

What a room does on a clock is waited on, never timed from a load: the Picket's beams, the Flows' crust, the Foundry's ladles and press. A step "until":"clear:x,z;x,z"
holds the pad still until the way from where the lead stands through those points is clear for as long as the lead will be on it (ReplayWays, ContinuousReview.Rooms),
and the walk after it goes. The Slag Colossus is fought as its room says: the driver turns a sluice whose seat ring shows and flicks the lock to the plate it darkens
(the same file). HarrowgateReplayTests plays each through the driver's own pad.

The way through the Picket: shadow to shadow, the map doc's quiet route, each leg set off on once the way to the next pocket is clear of every beam, the nose first
turned down the leg so that Idra, behind it, is never across it. Then B4 shot down from its north side (out of its own beam and B3's reach): the stage it raises is
amber and no more, and the lock's approach has nothing watching it. The lock is worked from its line, north of the flak curtain's rows and a hull short of the seal:
the first piece off brings the hunter wing (the lock's own alarm), which comes off the carrier's deck far out of sight and loses the ship, and wakes the flak gates,
whose curtain never reaches the line; the other three pieces, Idra's word, and through the gap to the Flows.

The chain. Chapter 7's routes end saved in OrielSkyfields after chapter7.complete (Lume's reading at Ishen's camp); chapter-44 takes Sela's chart from wherever that is.
  chapter-44-harrowgate-way-in   the chart from the Skyfields to Many Hands, Taren and Idra out, Brekk's word on the dock (starts Harrowgate), Idra's plan at the
                                 chart table, the launch, the hull's warp to the Picket
  chapter-45-picket-of-teeth     Idra at the edge, the quiet route pocket to pocket, B4 shot down, the lock in sight, the four pieces from the lock's line, Idra's
                                 word, through the gap and down to the Flows
  chapter-46-scoria-flows        Brekk on the crust, the first tongue, the causeway's pikes, the delta and its crawlers, the north shelf's squall, the battery,
                                 the camp: Tovra, Brekk's stall (gel to the cap), a save at the camp cradle
  chapter-47-scoria-errands      side content: Tovra's cages and her offer (Quiet Hands), the pike-head, the three kettle stones (the gate garrison fought on
                                 the way to the third), Tovra told, a save at the camp cradle
  chapter-48-bridle-foundry      the Foundry gate, the yard, the first wheel, the gangway's ladle, the charging floor, the press run and the second wheel, the press
                                 bay's hounds, the stair, the gallery's drones, the gallery cradle, Vessa Korl (boss, her door retried) and her squad, the heart cradle
  chapter-49-slag-colossus       the caged road, the Slag Colossus (boss, its door retried, worked by its sluices), the release, Brekk by the anvil, the way back up
                                 and out, Tovra's road, the town road to Harrowgate
  chapter-50-harrowgate          the sisters, Hessa's parley, Pip, the field with Pip, the trophy gallery and Pip's case, Mother Odrun, a save at the armourer's frame
  chapter-51-harrowgate-errands  side content: Odrun's sleepless and her kitchen (salt, flour, kettle; the back table, gel to the cap), the three glass cases, Pip's
                                 places (the washing, the stair game, the forges, the harbour wall, the wall of names), a save at the armourer's frame
  chapter-52-the-warmaster       Drask at his table, the field, the duel (boss, its door retried), his yield, the pursuit over the war harbour (boss, its door
                                 retried), the verdict, Pip after the duel, the armoury gate, the stair, the cell, every radio, Meret (chapter8.complete), and Abri
                                 turning the convoy for Nacre (chapter 9 begins, and saves)

  python tools/harrowgate_routes.py
"""
import math
import re
from pathlib import Path

import manyhands_routes as mh
from lantern_routes import COMBAT_SPEED, FLIGHT_SPEED
from manyhands_routes import BODY, NUMBER, block, points, scene_solids, vectors
from xylos_routes import box_distance, segment_distance

ROOT = Path(__file__).resolve().parent.parent
WORLD = ROOT / "Lattice" / "Assets" / "_Project" / "Scripts" / "Editor" / "World"
SCENES = ROOT / "Lattice" / "Assets" / "_Project" / "Scenes"
MAPS = ROOT / "docs" / "maps"
PREFIX = "chapter-"
HULL = 2.17  # HeroCollision.HullRadius("Idra"): the larger of the pair chapter 8 is played with (Taren and Idra, FlightSocketTests)
WALK = 4.5  # civil pace in a town
PAIR = ("Taren", "Idra")
TALK = 3.2  # WorldBuilder.Npc's reach for a talk
INSIDE = .15  # RouteFightTests.Inside: how far inside a reach a route must stand
POCKET_BRAKE = 4.0  # how far short of a pocket the leg's full thrust ends and the braked settle begins (a ship at 12 m/s braked runs on some 1.3 m)
FALL_IN = 2.0  # seconds braked in a pocket, the nose turned down the next leg, for Idra to come round behind it (half a turn round the lead is some 11 m at her pace)

STAGE_SOURCE = (WORLD / "HarrowgateBuilder.cs").read_text(encoding="utf-8")
PICKET_SOURCE = (WORLD / "HarrowgateBuilder.Picket.cs").read_text(encoding="utf-8")
SCORIA_SOURCE = (WORLD / "HarrowgateBuilder.Scoria.cs").read_text(encoding="utf-8")
FOUNDRY_SOURCE = (WORLD / "HarrowgateBuilder.Foundry.cs").read_text(encoding="utf-8")
TOWN_SOURCE = (WORLD / "HarrowgateBuilder.Town.cs").read_text(encoding="utf-8")
HARBOUR_SOURCE = (WORLD / "HarrowgateBuilder.Harbour.cs").read_text(encoding="utf-8")
HOME_SOURCE = (WORLD / "HomeBuilder.Approach.cs").read_text(encoding="utf-8")


def vec3(source, name):
    """A `Name=new(x,y,z)` of a builder, as its plan point (x, z)."""
    m = re.search(r"\b" + name + r"=new\(" + NUMBER + "," + NUMBER + "," + NUMBER + r"\)", source)
    if not m:
        raise SystemExit(f"no {name} in its builder\nHARROWGATE_ROUTES_FAILED")
    return float(m.group(1)), float(m.group(3))


def const(source, name):
    m = re.search(r"\b" + name + "=" + NUMBER, source)
    if not m:
        raise SystemExit(f"no {name} in its builder\nHARROWGATE_ROUTES_FAILED")
    return float(m.group(1))


def triples(text):
    return [(float(a), float(c)) for a, b, c in re.findall(r"new\(" + NUMBER + "," + NUMBER + "," + NUMBER + r"\)", text)]


def ways(text, named, numbers=None):
    """A Vector2[][] initializer as point lists, whether each is written new[]{...} or new Vector2[]{...}; `numbers` stands literals for the builder's float names."""
    for name, value in (numbers or {}).items():
        text = re.sub(r"\b" + name + r"\b", f"{value:g}", text)
    # A one-letter name (the town's F, the heart's C) is no name points() reads: written out as its literal.
    for name, (x, z) in named.items():
        if len(name) == 1:
            text = re.sub(r"(?<![\w.])" + name + r"(?![\w(])", f"new Vector2({x:g},{z:g})", text)
    text = text.replace("--", "")
    return [points(part, named) for part in re.findall(r"new(?: Vector2)?\[\]\{(.*?)\}", text[1:-1])]


def reach(where, stand, tolerance, prompt, distance_):
    """A press made at a prompt from a stand: the walk's end, plus its tolerance and RouteFightTests' fifteen centimetres, inside the prompt's reach on the plan."""
    gap = math.dist(stand, prompt) + tolerance + INSIDE
    if gap > distance_:
        raise SystemExit(f"{where}: standing within {tolerance} of {stand} is {gap:.2f} m from the prompt at {prompt}, which reaches {distance_}\nHARROWGATE_ROUTES_FAILED")


# ------------------------------------------------------------------ the Picket of Teeth, as HarrowgateBuilder.Picket lays it
_PICKET = block(PICKET_SOURCE, "internal static class Picket")
P = {k: vec3(_PICKET, k) for k in ("Arrival", "HomeWarp", "LockSpawn", "FromScoria", "FromTown", "ScoriaWay", "TownWay", "WingLaunch")}
PICKET_HALF = const(_PICKET, "Half")
EDGE_Z, LOCK_SEEN_Z, WALL_Z = const(_PICKET, "EdgeZ"), const(_PICKET, "LockSeenZ"), const(_PICKET, "WallZ")
GAP = const(_PICKET, "Gap")
BUOYS = [(m.group(1), (float(m.group(2)), float(m.group(3))), *map(float, m.groups()[3:])) for m in re.finditer(
    r'\("(B\d)",new\(' + NUMBER + "," + NUMBER + r"\)," + ",".join([NUMBER] * 6) + r"\)", block(_PICKET, "Buoys="))]
SHADES = [((float(a), float(b)), float(r)) for a, b, r in re.findall(r"\(new\(" + NUMBER + "," + NUMBER + r"\)," + NUMBER + r"\)", block(_PICKET, "Shades="))]
QUIET = [((float(a), float(b)), p == "true") for a, b, p in re.findall(r"\(new\(" + NUMBER + "," + NUMBER + r"\),(true|false)\)", block(_PICKET, "Quiet="))]
PIECES = [(m.group(1), (float(m.group(2)), float(m.group(4))), m.group(5), m.group(6)) for m in re.finditer(
    r'\("([^"]+)",new\(' + NUMBER + "," + NUMBER + "," + NUMBER + r'\),"([^"]+)","([^"]+)"\)', block(_PICKET, "Pieces="))]
FLAK = triples(block(_PICKET, "Flak="))
_seal = re.search(r"box\.size=new Vector3\(Picket\.Gap\*2," + NUMBER + "," + NUMBER + r"\)", block(PICKET_SOURCE, "static RouteLock"))
SEAL_FACE = WALL_Z - float(_seal.group(2)) / 2  # the lock seal's south face: what a hull at the lock meets until the keeper bar is out
DOCK_RANGE = 6.5  # WorldBuilder.FlightDockRange: the ways down and every piece of the lock
PICKET_WARP_RANGE = 5  # WorldBuilder.Warp's own reach: the way home
_nacre = re.search(r"PicketWarp=new\(" + NUMBER + r",ZoneKit\.FlightPlane," + NUMBER + r"\)", HOME_SOURCE)
NACRE_WARP = (float(_nacre.group(1)), float(_nacre.group(2)))  # HomeBuilder.Approach: hidden until Abri has turned the convoy (chapter 9)


def picket_rocks():
    """Every rock in the lane as the built scene has it: each "Rock N body" sphere (the eleven the quiet route was designed on, and the rest the builder drew), with
    its radius. Their shadows are the beams' business, at run time; here they are what a hull meets."""
    text = (SCENES / "PicketOfTeeth.unity").read_text(encoding="utf-8")
    docs = {m.group(2): (m.group(1), m.group(3)) for m in re.finditer(r"^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- |\Z)", text, flags=re.M | re.S)}
    rocks = []
    for kind, body in docs.values():
        if kind != "1" or not re.search(r"m_Name: Rock \d+ body\s*$", body, flags=re.M):
            continue
        pos = radius = None
        for c in re.findall(r"component: \{fileID: (\d+)\}", body):
            k, b = docs.get(c, (None, ""))
            if k == "4":
                m = re.search(r"m_LocalPosition: \{x: ([^,]+), y: [^,]+, z: ([^}]+)\}", b)
                pos = (float(m.group(1)), float(m.group(2)))
            elif k == "135":
                radius = float(re.search(r"m_Radius: ([^\n]+)", b).group(1))
        if pos and radius:
            rocks.append((pos, radius))
    return rocks


ROCKS = picket_rocks()

# ------------------------------------------------------------------ the pair in flight, the curtain, the lock's line (the game's own numbers)
SCRIPTS = WORLD.parent.parent
_follow = re.search(r"active\.transform\.position-active\.motor\.Facing\*" + NUMBER + r"\+followingRight\*" + NUMBER, (SCRIPTS / "Combat" / "PartnerBrain.cs").read_text(encoding="utf-8"))
BEHIND, BESIDE = float(_follow.group(1)), float(_follow.group(2))  # PartnerBrain: the partner's place is this far behind the lead's nose and this far to its right
LEAD_HULL = float(re.search(r'hero=="Taren"\?' + NUMBER, (SCRIPTS / "Combat" / "HeroCollision.cs").read_text(encoding="utf-8")).group(1))  # Taren leads the Picket
_motor = (SCRIPTS / "Combat" / "FlightMotor.cs").read_text(encoding="utf-8")
# Braked with the stick held, a ship closes at thrust over the brake's damping; let go, it runs on that over the damping again before it is still.
BRAKED_DRIFT = const(_motor, "acceleration") / float(re.search(r"damping=brake\?" + NUMBER, _motor).group(1)) ** 2
ALLOWANCE = const((SCRIPTS / "Combat" / "Telegraph.cs").read_text(encoding="utf-8"), "BodyAllowance")  # how far past its edge a marking still holds a body
# The flak gates' curtain lies in rows from the gates' own line out ahead of them (south, EnemyAnchored.Curtain): north of that line, short of the lock's seal, a ship is in
# no row. That band is where the lock is worked once the gates are awake: the lead's middle in it, its hull clear of the seal, approached from the south and braked.
CURTAIN_FRONT = max(z for x, z in FLAK) + ALLOWANCE
LOCK_TOLERANCE = .15
LOCK_LINE = round(CURTAIN_FRONT + .25, 2)
if LOCK_LINE - LOCK_TOLERANCE <= CURTAIN_FRONT or LOCK_LINE + LOCK_TOLERANCE + BRAKED_DRIFT + LEAD_HULL >= SEAL_FACE:
    raise SystemExit(f"the lock's line z {LOCK_LINE} isn't both out of the curtain (z > {CURTAIN_FRONT}) and a hull ({LEAD_HULL}) and a drift ({BRAKED_DRIFT:.2f}) short of the seal (z {SEAL_FACE})"
                     "\nHARROWGATE_ROUTES_FAILED")
CAMERA_YAW = float(re.search(r"yaw: (\S+)", (SCRIPTS.parent / "Resources" / "Definitions" / "CameraProfile" / "Flight.asset").read_text(encoding="utf-8")).group(1))


def stick(direction, magnitude):
    """The stick, as the driver writes it (camera-local, ContinuousReview.InputUpdate), for a plan direction in the flight camera's view."""
    yaw = math.radians(-CAMERA_YAW)
    dx, dz = direction
    length = math.hypot(dx, dz)
    dx, dz = dx / length, dz / length
    return round((dx * math.cos(yaw) + dz * math.sin(yaw)) * magnitude, 3), round((-dx * math.sin(yaw) + dz * math.cos(yaw)) * magnitude, 3)


def picket_solids(lock_open=False):
    """What a hull meets at the plane in the Picket, as (label, centre, yaw, half x, half z): the wall's two bodies either side of the gap, the lock's seal across it
    while the keeper bar is in, the warp pieces (2 m), the flak gates' and the buoys' bodies (their capsules: 2 and 1 m)."""
    inner, outer = GAP, PICKET_HALF + 4
    s = [(f"the wall's {side} body", ((inner + outer) / 2 * sign, WALL_Z), 0, (outer - inner) / 2, 1.5) for side, sign in (("west", -1), ("east", 1))]
    if not lock_open:
        s.append(("the lock's seal", (0, WALL_Z), 0, GAP, 1.25))
    s += [("the warp home", P["HomeWarp"], 0, 1, 1), ("the way down to Scoria", P["ScoriaWay"], 0, 1, 1), ("the way down to Harrowgate", P["TownWay"], 0, 1, 1)]
    s += [(f"the flak gate at {f}", f, 0, 2, 2) for f in FLAK] + [(f"buoy {name}", at, 0, 1, 1) for name, at, *_ in BUOYS]
    return s


# ------------------------------------------------------------------ Many Hands: what chapter 8 stands there (HarrowgateBuilder.StageManyHands)
_brekk = re.search(r"var brekkAt=new Vector2\(" + NUMBER + "," + NUMBER + r"\)", STAGE_SOURCE)
BREKK_DOCK = (float(_brekk.group(1)), float(_brekk.group(2)))
_word = re.search(r'word\.transform\.position=Floor\(new Vector2\(' + NUMBER + "," + NUMBER + r"\)\)", STAGE_SOURCE)
BREKK_WORD = (float(_word.group(1)), float(_word.group(2)))
WORD_BOX = tuple(float(v) for v in re.search(r"mouth\.size=new Vector3\(" + NUMBER + "," + NUMBER + "," + NUMBER + r"\)", STAGE_SOURCE).groups())
_warp = re.search(r"var warpAt=new Vector3\(" + NUMBER + r",ZoneKit\.FlightPlane," + NUMBER + r"\)", STAGE_SOURCE)
WARP_TO_PICKET = (float(_warp.group(1)), float(_warp.group(2)))
_from = re.search(r"WorldBuilder\.Spawn\(HomeSpawn,new Vector3\(" + NUMBER + r",ZoneKit\.FlightPlane," + NUMBER + r"\)\)", STAGE_SOURCE)
FROM_PICKET = (float(_from.group(1)), float(_from.group(2)))
PLAN_RANGE = float(re.search(r"lay\.range=" + NUMBER, STAGE_SOURCE).group(1))
# The other warps off the stern (chapter 6's, 7's: their staging), and chapter 9's, which is hidden until Abri turns the convoy.
STERN_WARPS = [("Hoarfell's warp", (-90.0, -176.0)), ("Oriel's warp", (-120.0, -176.0)), ("the warp to the Picket", WARP_TO_PICKET)]


# ------------------------------------------------------------------ the Scoria Flows, as HarrowgateBuilder.Scoria lays it
_SCORIA = block(SCORIA_SOURCE, "internal static class Scoria")
S = vectors(_SCORIA[:_SCORIA.index("internal static readonly Vector3[] Islands")])
SCORIA_WAYS = ways(block(_SCORIA, "internal static readonly Vector2[][] Ways="), S)
ISLANDS = [(S[n], float(r)) for n, r in re.findall(r"new\((\w+)\.x,\w+\.y," + NUMBER + r"\)", block(_SCORIA, "Islands="))]
SCORIA_LAUNCH_RANGE = float(re.search(r"launch\.range=" + NUMBER, SCORIA_SOURCE).group(1))
FOUNDRY_ROAD_RANGE = float(re.search(r'"Foundry road — The Bridle Foundry",FoundryZone,"Arrival","met\.Tovra","[^"]*",' + NUMBER, SCORIA_SOURCE).group(1))
TOWN_ROAD_RANGE = float(re.search(r'"Town road — Harrowgate",TownZone,"Arrival","vask\.roadSaid","[^"]*",' + NUMBER, SCORIA_SOURCE).group(1))


def kettle_hand(at, yaw):
    """Where a cookfire's kettle stone is worked from (Scoria.Cookfire): beside the fire on its stone's side, a step back from the stone."""
    r = math.radians(yaw)
    turn = lambda x, z: (x * math.cos(r) + z * math.sin(r), -x * math.sin(r) + z * math.cos(r))
    stone, back = turn(1.05, .15), turn(0, -.9)
    return (round(at[0] + stone[0] + back[0], 2), round(at[1] + stone[1] + back[1], 2))


KETTLES = {int(m.group(3)): kettle_hand(S[m.group(1)], float(m.group(2))) for m in re.finditer(r'Cookfire\(kit,"[^"]+",(\w+),' + NUMBER + r',"vask\.kettle(\d)"\)', SCORIA_SOURCE)}
KETTLE_RANGE = 2.6
# The Unsworn camp's yard: a safe pocket of open setts (36 by 26 on (47, 206)), walked anywhere between its tents, its fire and its stall; and where its people are spoken to.
CAMP_YARD, CAMP_YARD_ROUND = (47.0, 206.0), 12.5
STALL_STAND = (S["BrekkStall"][0] - .4, S["BrekkStall"][1] - 2.2)
CAMP_CRADLE_STAND = (S["CampCradle"][0] + 2.1, S["CampCradle"][1] + .1)
# Into the camp south of its water barrels, and round its fire ring by the south: the builder's way from (28, 208) passes the barrels at (34, 207.5) within a body.
INTO_CAMP = [(30, 206.2), (35, 204.6), S["CampSpawn"]]
CAMP_SOUTH = [(42, 203.2), (46.8, 203.6)]
# The camp's standing people: since November 21 (D336) each wears a body of nobody named here, placed from a list of (x, z, yaw, tint).
SCORIA_FOLK = [(float(a), float(b)) for a, b, _ in re.findall(r"\(" + NUMBER + "," + NUMBER + "," + NUMBER + r",new Color", block(SCORIA_SOURCE, "foreach(var (x,z,yaw,tint) in new[]"))]
assert len(SCORIA_FOLK) >= 4, "the Scoria camp's people were not read from HarrowgateBuilder.Scoria.cs"
BUILT_SCORIA = [(l, c, yaw, hx, hz) for l, c, yaw, hx, hz, y0, y1 in scene_solids("ScoriaFlows", ("Brekk leading", "Generated civilian", "Townsperson", "Walker"))
                if y0 < 1.8 + 1 and y1 > .3 - .4 and hx < 30 and hz < 30]


# ------------------------------------------------------------------ the Bridle Foundry, as HarrowgateBuilder.Foundry lays it
_FOUNDRY = block(FOUNDRY_SOURCE, "internal static class Foundry")
FD = vectors(_FOUNDRY[:_FOUNDRY.index("/// <summary>The four mould racks")])
FOUNDRY_LEVELS = {k: const(_FOUNDRY, k) for k in ("Charge", "Cast", "Deep", "TankFloor")}
ARENA_RADIUS = const(_FOUNDRY, "ArenaRadius")
ROOMS = [(m.group(1), *(float(v) for v in m.groups()[1:])) for m in re.finditer(r"\(Room\.(\w+)," + ",".join([NUMBER] * 5) + r"\)", block(_FOUNDRY, "float round)[] Boxes="))]
FOUNDRY_WAYS = ways(block(_FOUNDRY, "internal static readonly Vector2[][] Ways="), FD)
TROUGHS = {m.group(1): ((float(m.group(2)), float(m.group(3))), (float(m.group(4)), float(m.group(5)))) for m in re.finditer(
    r'\("(\w+)",new\(' + NUMBER + "," + NUMBER + r"\),new\(" + NUMBER + "," + NUMBER + r"\)\)", block(_FOUNDRY, "Troughs="))}
SEAT_OUT = const(_FOUNDRY, "SeatOut")
WHEEL_RANGE = 2.6  # Foundry.Sluice: kit.Discovery(...,2.6f)
FOUNDRY_HOME_RANGE = float(re.search(r'"Foundry mouth — The Scoria Flows",ScoriaZone,ScoriaFromFoundry,null,null,' + NUMBER, FOUNDRY_SOURCE).group(1))
ANVIL_STAND = (FD["AnvilAt"][0] + 1.4, FD["AnvilAt"][1] - 2.6)
ANVIL_RANGE = 3
C_HEART, G_GALLERY = FD["C"], FD["G"]
COVER_STACKS = [(round(C_HEART[0] + 9 * math.sin(math.radians(b)), 2), round(C_HEART[1] + 9 * math.cos(math.radians(b)), 2)) for b in (140, 260, 20)]


def round_box(p, x0, x1, z0, z1, r):
    dx, dz = abs(p[0] - (x0 + x1) / 2) - (x1 - x0) / 2 + r, abs(p[1] - (z0 + z1) / 2) - (z1 - z0) / 2 + r
    return min(max(dx, dz), 0) + math.hypot(max(dx, 0), max(dz, 0)) - r


def foundry_room(p):
    best, room = math.dist(p, C_HEART) - ARENA_RADIUS, "Arena"
    for which, x0, x1, z0, z1, r in ROOMS:
        clear = round_box(p, x0, x1, z0, z1, r)
        if clear < best:
            best, room = clear, which
    return best, room


def foundry_y(p):
    """A floor's level by its room (Foundry.Level), without its centimetres of unevenness."""
    _, room = foundry_room(p)
    lerp = lambda a, b, t: a + (b - a) * max(0.0, min(1.0, t))
    L = FOUNDRY_LEVELS
    if room == "TankStair":
        return round(lerp(L["Charge"], L["TankFloor"], (p[1] - 106) / 4), 2)
    if room == "Tank":
        return L["TankFloor"]
    if room == "ChargeStair":
        return round(lerp(L["Charge"], L["Cast"], (p[1] - 121) / 14), 2)
    if room in ("GallerySouth", "LoftPass", "Loft", "GalleryDoor", "Gallery"):
        return L["Cast"]
    if room == "HeartStair":
        return round(lerp(L["Cast"], L["Deep"], (p[1] - 203) / 14), 2)
    if room in ("Landing", "HeartDoor", "Dais", "SmithsDoor", "Arena"):
        return L["Deep"]
    return L["Charge"]


# The Foundry's solid things a body meets on its floors, as built; less the seals the fights open and the young road's smiths and walkers, who move.
BUILT_FOUNDRY = [(l, c, yaw, hx, hz) for l, c, yaw, hx, hz, y0, y1 in scene_solids("BridleFoundry", ("Gallery south seal", "Heart stair seal", "Heart door seal", "Tap run", "Smiths door grille",
                 "The smiths", "Brekk", "Tovra", "Vessa", "Generated civilian", "Townsperson",
                 # The dressing's props read here as boxes many metres wider than they are drawn (an 8 m square for an ore hopper by the yard's way): the navigation the props
                 # were baked round is the judge of those (HarrowgateRouteTests).
                 "Foundry clutter"))
                 if y0 < foundry_y(c) + 1.8 and y1 > foundry_y(c) + .3 and hx < 30 and hz < 30]


# ------------------------------------------------------------------ Harrowgate, as HarrowgateBuilder.Town lays it
# The town's class is partial since November 21 (its look lives in HarrowgateBuilder.TownLook.cs); the plan's tables stayed in Town.cs.
_TOWN = block(TOWN_SOURCE, "internal static partial class Town" if "internal static partial class Town" in TOWN_SOURCE else "internal static class Town")
T = vectors(_TOWN[:_TOWN.index("/// <summary>The muster field inside its kerb")])
TOWN_LEVELS = {k: const(_TOWN, k) for k in ("Quay", "Lower", "Upper", "Podium", "Cells")}
RAMPS = [(float(x), float(h), float(a), float(b), TOWN_LEVELS[lo], TOWN_LEVELS[hi]) for x, h, a, b, lo, hi in re.findall(
    NUMBER + "," + NUMBER + "," + NUMBER + "," + NUMBER + r",(Quay|Lower|Upper|Podium),(Quay|Lower|Upper|Podium)\)", block(_TOWN, "Ramps="))]
FIELD_HALF_X, FIELD_SOUTH, FIELD_NORTH = const(_TOWN, "FieldHalfX"), const(_TOWN, "FieldSouth"), const(_TOWN, "FieldNorth")
T["FieldSouthAt"] = (0, FIELD_SOUTH)
TOWN_WAYS = ways(block(_TOWN, "internal static readonly Vector2[][] Ways="), T, {"FieldSouth": FIELD_SOUTH, "FieldHalfX": FIELD_HALF_X})
TOWN_FOLK = [(float(a), float(b)) for a, b in re.findall(r'\("\w+",new\(' + NUMBER + "," + NUMBER + r"\)", block(TOWN_SOURCE, "string doing,float scale)[] Folk="))]
TOWN_FOLK += [(float(a), float(b)) for a, b in re.findall(r'\("\w+",new\(' + NUMBER + "," + NUMBER + r"\)", block(TOWN_SOURCE, "Color tint,float scale)[] Children="))]
TOWN_LOOPS = [points(m.group(1), T) for m in re.finditer(r"new\[\]\{(.*?)\}", block(TOWN_SOURCE, "string name)[] Errands="))]
_TOWN_WALKERS = tuple(m.group(1) for m in re.finditer(r'(?:true|false),"([^"]+)"\)', block(TOWN_SOURCE, "string name)[] Errands=")))
TOWN_RANGES = {"Pip": 2.6, "case": 2.4, "thing": 2.4, "kitchen": 2.4}  # Town.Story: Pip's places, the glass cases, Odrun's three things, the kitchen table
PAD_RANGE = float(re.search(r'kit\.Way\(holder,"Launch — The War Harbour","HarrowgateHarbour","Launch","vask\.duelWon","[^"]*",' + NUMBER, TOWN_SOURCE).group(1))
CELL_RANGE = float(re.search(r"cell\.range=" + NUMBER, TOWN_SOURCE).group(1))
_grille = re.search(r'WorldBuilder\.Membrane\("Armoury gate grille",new Vector3\(' + NUMBER + r",Upper\+" + NUMBER + "," + NUMBER + r"\)", TOWN_SOURCE)
ARMOURY_GRILLE = (float(_grille.group(1)), float(_grille.group(3)))


def town_crossings():
    """The town's talk lines (kit.Crossing in HarrowgateBuilder.Town): name, centre, half-size on the plan, the flag it sets and the one it waits for. A centre may be a named place
    (Town.PipFirstAt) or literal; a size literal. One whose numbers aren't literal is left out here and said so."""
    lines = []
    for m in re.finditer(r'kit\.Crossing\("([^"]+)",new Vector3\(([^,]+),0,([^)]+)\),new Vector3\(([^,]+),[^,]+,([^)]+)\),"([^"]+)","[^"]+","[^"]+"(?:,"([^"]*)")?\)', TOWN_SOURCE):
        name, cx, cz, sx, sz, flag, needs = m.groups()
        def value(text, axis):
            text = text.strip().rstrip("f")
            if re.fullmatch(NUMBER.replace("f?", ""), text):
                return float(text)
            place = re.fullmatch(r"(\w+)\.([xy])", text)
            if place and place.group(1) in T:
                return T[place.group(1)][0 if place.group(2) == "x" else 1]
            return None
        c = (value(cx, "x"), value(cz, "z"))
        size = (value(sx, "x"), value(sz, "z"))
        if None in c or None in size:
            print(f"(the town's crossing '{name}' isn't written in literals or named places: not checked)")
            continue
        lines.append((name, c, (size[0] / 2, size[1] / 2), flag, needs or ""))
    return lines


TOWN_CROSSINGS = town_crossings()


def _smooth(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def town_y(p):
    """The floor's height at a plan point of Harrowgate, as Town.Height makes it where the town is walked (its crag and its harbour's bed left out): the two risers, the
    oath hall's podium, the ramps through them, and the stair down outside the harbour wall to the cells' landing. A fixed height once put the forecourt, the trophy
    gallery and the harbour wall on the quay or the landing (HarrowgateRouteTests, November 14)."""
    L = TOWN_LEVELS
    x, z = p
    inside = lambda x0, x1, z0, z1: min(min(x - x0, x1 - x), min(z - z0, z1 - z))
    riser = lambda line: _smooth(line - .7, line + .7, z)
    y = (L["Lower"] - L["Quay"]) * riser(22.2) + (L["Upper"] - L["Lower"]) * riser(58.2)
    lerp = lambda a, b, t: a + (b - a) * t
    y = lerp(y, L["Podium"], _smooth(-.3, .3, inside(-16, 16, 105.5, 400)))
    for cx, half, z0, z1, lo, hi in RAMPS:
        across = 1 - _smooth(half - .3, half + .3, abs(x - cx))
        if across <= 0:
            continue
        along = _smooth(z0 - .6, z0, z) * (1 - _smooth(z1, z1 + .6, z))
        if along <= 0:
            continue
        y = lerp(y, lerp(lo, hi, max(0.0, min(1.0, (z - z0) / (z1 - z0)))), across * along)
    outside = _smooth(47.7, 48.3, x) * _smooth(78.2, 78.8, z) * (1 - _smooth(111.7, 112.3, z))
    if outside > 0:
        stair = L["Upper"] if z < 82 else lerp(L["Upper"], L["Cells"], (z - 82) / 12) if z < 94 else L["Cells"]
        y = lerp(y, stair, outside)
    return round(y, 2)


# What a body meets in the built town: everything solid between ankle and head over the floor there, less the walkers (who move), the seals the story opens (the gate's
# leaves from the parley, the armoury grille from the verdict, the field's four seals outside the duel), the folk who stand only for a scene and move with it.
_TOWN_SKIP = _TOWN_WALKERS + ("Harbour gate leaves", "Armoury gate grille", "Muster field seal", "A cadet at drill", "Pip", "Vessa at the harbour gate", "Drask", "The field after the pursuit",
                              "The town watching", "The town waits on the steps", "The guard wing", "Townsperson")
BUILT_TOWN = [(l, c, yaw, hx, hz) for l, c, yaw, hx, hz, y0, y1 in scene_solids("Harrowgate", _TOWN_SKIP) if y0 < town_y(c) + 1.8 and y1 > town_y(c) + .3 and hx < 30 and hz < 30]


# ------------------------------------------------------------------ the war harbour, as HarrowgateBuilder.Harbour lays it
_HARBOUR = block(HARBOUR_SOURCE, "internal static class Harbour")
HB = {k: vec3(_HARBOUR, k) for k in ("Launch", "HomeWay", "Exit")}
HB_LEGS = triples(block(_HARBOUR, "Legs="))
HB_LIMITS = tuple(const(_HARBOUR, k) for k in ("XMin", "XMax", "ZMin", "ZMax"))
CRANES = [(m.group(1), (float(m.group(2)), float(m.group(3))), float(m.group(4)), float(m.group(5))) for m in re.finditer(
    r'\("([^"]+)",new\(' + NUMBER + "," + NUMBER + r"\)," + NUMBER + "," + NUMBER + "," + NUMBER + r"\)", block(_HARBOUR, "Cranes="))]
HARBOUR_SOLIDS = [(f"{name}'s leg", (at[0] + sx * hx, at[1] + sz * hz), 0, .8, .8) for name, at, hx, hz in CRANES for sx in (-1, 1) for sz in (-1, 1)] + [("the way back to the field", HB["HomeWay"], 0, 1, 1)]


# ------------------------------------------------------------------ the map docs
def doc_rows(name, section="## Coordinates"):
    """Each table row under a doc's coordinates heading: its cells, and every place in it as (x, z): from (x, y, z), (x, z) or a bare 'x, z'."""
    text = (MAPS / name).read_text(encoding="utf-8")
    start = text.index(section)
    rest = text[start + 4:]
    body = text[start:start + 4 + rest.index("\n## ")] if "\n## " in rest else text[start:]
    rows = []
    for line in body.splitlines():
        if not line.strip().startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if not cells or set(cells[0]) <= set("-: "):
            continue
        joined = " ".join(cells[1:])
        found = [(float(a), float(c)) for a, b, c in re.findall(r"\((-?[\d.]+), (-?[\d.]+), (-?[\d.]+)\)", joined)]
        joined = re.sub(r"\((-?[\d.]+), (-?[\d.]+), (-?[\d.]+)\)", " ", joined)
        found += [(float(a), float(b)) for a, b in re.findall(r"(?<![\d.])(-?\d+(?:\.\d+)?), (-?\d+(?:\.\d+)?)(?![\d.])", joined)]
        rows.append((cells, found))
    return rows


def check_docs():
    wrong = []

    def expect(doc, label, *places, section="## Coordinates"):
        rows = [r for r in doc_rows(doc, section) if label in r[0][0] or len(r[0]) > 1 and label in r[0][1]]
        if not rows:
            wrong.append(f"{doc}: no row '{label}'")
            return
        given = [p for r in rows for p in r[1]]
        for place in places:
            if not any(math.dist(place, g) <= .25 for g in given):
                wrong.append(f"{doc} '{label}': the builder has {place}, the doc {given}")

    pk = "PicketOfTeeth.md"
    for label, key in (("Arrival", "Arrival"), ("Warp home", "HomeWarp"), ("Carrier's deck", "WingLaunch"), ("Lock checkpoint", "LockSpawn"), ("From the Flows", "FromScoria"),
                       ("From Harrowgate", "FromTown"), ("Down to the Flows", "ScoriaWay"), ("Down to Harrowgate", "TownWay")):
        expect(pk, label, P[key], section="## Coordinates for routes")
    expect(pk, "B1, B2, B3, B4", *[at for _, at, *_ in BUOYS], section="## Coordinates for routes")
    for (name, at, _, _), label in zip(PIECES, ("Port collar", "Starboard collar", "Keel collar", "Keeper bar")):
        expect(pk, label, at, section="## Coordinates for routes")
    expect(pk, "Flak gates", *FLAK, section="## Coordinates for routes")
    # The quiet route's legs, as its table gives them (From, Via, To), and the rocks that shade it.
    quiet_rows = [r for r in doc_rows(pk, "## The quiet route") if r[0][0].isdigit()]
    doc_quiet = [quiet_rows[0][1][0]] + [p for r in quiet_rows for p in r[1][1:]]
    if [tuple(round(v, 1) for v in p) for p in doc_quiet] != [tuple(round(v, 1) for v in p) for p, _ in QUIET]:
        wrong.append(f"{pk}: the quiet route's table isn't HarrowgateBuilder.Picket.Quiet: {doc_quiet} against {[p for p, _ in QUIET]}")
    shade_text = (MAPS / pk).read_text(encoding="utf-8").split("## The rocks that shade the quiet route")[1].split("## ")[0]
    doc_shades = [((float(a), float(b)), float(c)) for a, b, c in re.findall(r"\((-?[\d.]+), (-?[\d.]+), (-?[\d.]+)\)", shade_text)]
    if doc_shades != SHADES:
        wrong.append(f"{pk}: the shading rocks aren't HarrowgateBuilder.Picket.Shades")
    sf = "ScoriaFlows.md"
    for label, place in (("`Arrival`", S["Arrival"]), ("`Camp`", S["CampSpawn"]), ("`Foundry`", S["FoundrySpawn"]), ("`Harrowgate`", S["HarrowgateSpawn"]),
                         ("`Launch — The Picket of Teeth`", S["Pad"]), ("`Foundry road — The Bridle Foundry`", S["FoundryGate"]), ("`Town road — Harrowgate`", S["HarrowgateRoad"]),
                         ("`Tovra`", S["TovraAt"]), ("`Brekk` (stall)", S["BrekkStall"]), ("`Camp cradle — repair and save`", S["CampCradle"]), ("`Scoria_Crossing`", S["CrossingFight"]),
                         ("`Scoria_Delta`", S["DeltaFight"]), ("`Scoria_Battery`", S["BatteryFight"]), ("`Scoria_Gate`", S["GateFight"]), ("`Look at the pike-head`", S["PikeHead"]),
                         ("Kettle stone 1", S["KettleOne"]), ("Kettle stone 2", S["KettleTwo"]), ("Kettle stone 3", S["KettleThree"])):
        expect(sf, label, place, section="## Route coordinates")
    for label, at in (("`Brekk on the crust`", (1, 19.5)), ("`The garrison's causeway`", (0, 67)), ("`Ash coming down the valley`", (2, 157.5)), ("`Over the first tongue`", (3, 53.5))):
        expect(sf, label, at, section="## Route coordinates")
    bf = "BridleFoundry.md"
    for label, place in (("Arrival spawn", FD["Arrival"]), ("Way home", FD["WayHome"]), ("Mouth cradle", FD["MouthCradle"]), ("Wheel 1", FD["WheelOneHand"]), ("Wheel 2", FD["WheelTwoHand"]),
                         ("Gallery checkpoint", FD["GallerySpawn"]), ("Gallery checkpoint", FD["GalleryCradle"]), ("Heart checkpoint", FD["HeartSpawn"]), ("Heart checkpoint", FD["HeartCradle"]),
                         ("Brekk by the anvil", ANVIL_STAND), ("First wheel talk", (0, 46)), ("Gallery talk", (0, 161)), ("Heart door talk", (0, 225.2))):
        expect(bf, label, place)
    hg = "Harrowgate.md"
    for label, place in (("Arrival (quay)", T["Arrival"]), ("Vessa, Hessa at the gate", T["HessaGate"]), ("Pip waiting", T["PipWaits"]), ("Pip waiting", T["PipFirstAt"]),
                         ("Washing lines", T["WashingAt"]), ("Family stair", T["StairGameAt"]), ("Odrun's kitchen", T["KitchenDoor"]), ("Odrun's kitchen", T["OdrunAt"]),
                         ("Pip after the verdict", T["PipAfterAt"]), ("Armourers' street", T["ForgesAt"]), ("Armourer's frame", T["ArmourersSpawn"]), ("Regulation kettle", T["KettleAt"]),
                         ("Garrison salt", T["SaltAt"]), ("Ash-root flour", T["FlourAt"]), ("Pip at the field's kerb", T["PipFieldAt"]), ("Checkpoint; launch", T["FieldSpawn"]),
                         ("Checkpoint; launch", T["LaunchPad"]), ("Back from the pursuit", T["VerdictSpawn"]), ("Drask at his table", T["DraskAt"]), ("Wall of names", T["NamesAt"]),
                         ("Glass case 3", T["Case3"]), ("Glass case 1", T["Case1"]), ("Glass case 1", T["DoorCase"]), ("Glass case 2", T["Case2"]), ("Wall walk", T["Lookout"]),
                         ("Armoury gate", T["ArmouryGate"]), ("Armoury gate", T["HessaArmoury"]), ("Stair down", T["StairMid"]), ("Drask in his cell", T["CellAt"]),
                         ("Drask in his cell", T["CellFront"]), ("Muster field", T["F"])):
        expect(hg, label, place, section="## Coordinates for routes")
    # The pursuit's places, as the harbour's "As built" paragraph names them.
    as_built = (MAPS / hg).read_text(encoding="utf-8").split("**As built (November 13")[1].split("\n")[0]
    for what, place in (("Launch", HB["Launch"]), ("Land — Harrowgate", HB["HomeWay"])):
        if f"({place[0]:g}, 1, {place[1]:g})" not in as_built:
            wrong.append(f"{hg}: the harbour's {what} isn't at {place} in its 'As built'")
    for leg in HB_LEGS:
        if f"({leg[0]:g}, {leg[1]:g})" not in as_built:
            wrong.append(f"{hg}: the harbour's leg {leg} isn't in its 'As built'")
    # What the routes stand on that only the builders say: Brekk's word, the warp to the Picket, the lock's checkpoint.
    if BREKK_WORD != (0.0, -44.4):
        wrong.append(f"HarrowgateBuilder.StageManyHands: Brekk's word is at {BREKK_WORD}, the brief's (0, -44.4)")
    if wrong:
        raise SystemExit("a place here isn't where its map doc says:\n" + "\n".join(wrong) + "\nHARROWGATE_ROUTES_FAILED")
    missing = [s for s in SHADES if not any(math.dist(s[0], p) < .05 and abs(r - s[1]) < .05 for p, r in ROCKS)]
    if missing or len(ROCKS) < len(SHADES):
        raise SystemExit(f"the built PicketOfTeeth has {len(ROCKS)} rocks and misses the quiet route's {missing} (a stale scene?)\nHARROWGATE_ROUTES_FAILED")


# ------------------------------------------------------------------ the checks
class Checked(mh.Checked):
    """manyhands_routes' checks for the ship and its hull (at Idra's hull, the pair's larger), and chapter 8's: the Picket's walls, rocks and solids by a hull; the
    Flows', the Foundry's and the town's declared ways (or the floor in front of something pressed), what each built scene stands solid and the people standing on it;
    the war harbour's line and its cranes' legs. `stands` are the places off the ways a route may step to, each with how far round it."""

    def __init__(self, *args, **kw):
        super().__init__(*args, **kw)
        self.hull = HULL
        self.lock_open = False
        self.on_lock_line = False  # a walk to or along the lock's line (LOCK_LINE)
        self.flak_awake = False  # the first piece is off: the gates fire their curtain
        self.stands = []
        self.town_people = []
        self.deployed = PAIR
        self.brekk_on_dock = False
        self.across_loop = False  # a leg allowed across a townsperson's errand, straight over it (a case behind an errand: a finding)
        self.seen = set()  # every flag the route has asked for so far: what the world it walks has opened
        self.walk_stop = None  # the walk being checked ends on this condition (a talk it walks into)

    def checks(self):
        c = super().checks()
        c.update({"ManyHandsHull": self.check_stern, "PicketOfTeeth": self.check_picket, "ScoriaFlows": self.check_scoria, "BridleFoundry": self.check_foundry,
                  "Harrowgate": self.check_town, "HarrowgateHarbour": self.check_harbour})
        return c

    def walk(self, name, x, y, z, tolerance=0.8, seconds=None, stop=None, buttons=None):
        # On the Foundry's three floors and the town's four levels the height is always the floor's own (the builder's function): a literal a walk was written with
        # stood a forecourt walk on the quay and a lookout on the cells' landing (HarrowgateRouteTests, November 14).
        self.walk_stop = stop
        floors = {"BridleFoundry": foundry_y, "Harrowgate": town_y}
        if self.scene in floors:
            y = floors[self.scene]((x, z))
        elif y is None:
            y = 0
        super().walk(name, x, y, z, tolerance, seconds, stop, buttons)

    def fail(self, text):
        self.problems.append(f"{self.name}: {text}")

    def check_stern(self, name, to):
        x0, x1, z0, z1 = mh.LIMITS
        for p in self.legs(to):
            # The harbour line is a 2 m wall on each limit: the hull's middle keeps its radius and a metre in from the wall's inner face.
            if not (x0 + 1 + self.hull + .3 < p[0] < x1 - 1 - self.hull - .3 and z0 + 1 + self.hull + .3 < p[1] < z1 - 1 - self.hull - .3):
                return self.fail(f"'{name}' flies into the harbour line at ({p[0]:.1f}, {p[1]:.1f})")
            hit = next((label for label, c, yaw, hx, hz in mh.HULL_SOLIDS + [(n, at, 0, 1, 1) for n, at in STERN_WARPS] if box_distance(p, c, yaw, hx, hz) < self.hull + .5), None)
            if hit:
                return self.fail(f"'{name}' flies within a hull ({self.hull} m) of {hit} at ({p[0]:.1f}, {p[1]:.1f})")

    def check_city(self, name, to):
        super().check_city(name, to)
        if not self.problems:
            for p in self.legs(to):
                if self.brekk_on_dock and math.dist(p, BREKK_DOCK) < .4 + BODY + .15:
                    return self.fail(f"'{name}' walks through Brekk on the dock at ({p[0]:.1f}, {p[1]:.1f})")

    def check_picket(self, name, to):
        solids = picket_solids(self.lock_open)
        # On the lock's line the lead (Taren) is held braked a hull short of the seal: his own hull, the walk's tolerance and the drift after it, against the seal; and never
        # south of the line once the gates are awake, which is in their curtain's front row.
        hull, room = (LEAD_HULL, LOCK_TOLERANCE + BRAKED_DRIFT) if self.on_lock_line else (self.hull, .5)
        for p in self.legs(to):
            if abs(p[0]) > PICKET_HALF - 1 - self.hull - 1:
                return self.fail(f"'{name}' flies {abs(p[0]):.1f} m off the lane's middle, into its teeth")
            rock = next(((c, r) for c, r in ROCKS if math.dist(p, c) < r + self.hull + .3), None)
            if rock:
                return self.fail(f"'{name}' flies within a hull of the rock at {rock[0]} (r {rock[1]}) at ({p[0]:.1f}, {p[1]:.1f})")
            hit = next((label for label, c, yaw, hx, hz in solids if box_distance(p, c, yaw, hx, hz) < hull + room), None)
            if hit:
                return self.fail(f"'{name}' flies within a hull of {hit} at ({p[0]:.1f}, {p[1]:.1f})")
            if self.on_lock_line and self.flak_awake and p[1] - LOCK_TOLERANCE <= CURTAIN_FRONT:
                return self.fail(f"'{name}' works the lock at ({p[0]:.1f}, {p[1]:.1f}), in the flak curtain's front row (north edge z {CURTAIN_FRONT})")

    def face(self, name, toward, magnitude=.3):
        """Turn the ship's nose toward a plan point where it stands, braked: a tap of the stick (FlightMotor faces the way it is pushed), a few centimetres of way."""
        x, y = stick((toward[0] - self.at[0], toward[1] - self.at[1]), magnitude)
        self.add(name, 0.12, x=x, y=y, leftTrigger=1)

    def work_at_lock(self, name, flag, nose=None):
        """A piece of the lock worked from its line: still, braked, the nose turned (`nose`: "south" or "west") so that Idra's place behind it (PartnerBrain) is against the
        seal, out of the curtain too and off the way the lead goes next, then the press (lantern_routes' press_for), braked throughout: let go, the ship ran on into the seal
        or back into the curtain's front row."""
        self.add("hold still before " + name, 0.6, leftTrigger=1)
        if nose:
            x, y = stick({"south": (0, -1), "west": (-1, 0)}[nose], .3)
            self.add(f"turn the nose {nose} before " + name, 0.12, x=x, y=y, leftTrigger=1)
        self.add("settle before " + name, 0.5, leftTrigger=1)
        self.add(name, 4, buttons=["South"], pulseSeconds=0.6, until="flag:" + flag, expectedFlag=flag, leftTrigger=1)
        self.add("release " + name, 0.5, leftTrigger=1)

    def ground(self, name, to, ways, built, people, off=1.2):
        for p in self.legs(to):
            gap = min(min(segment_distance(p, w[i - 1], w[i]) for i in range(1, len(w))) for w in ways)
            if gap > off and not any(math.dist(p, at) <= round_ for at, round_ in self.stands):
                return self.fail(f"'{name}' leaves {self.scene}'s ways by {gap:.1f} m at ({p[0]:.1f}, {p[1]:.1f})")
            hit = next((label for label, c, yaw, hx, hz in built if box_distance(p, c, yaw, hx, hz) < BODY + .15), None)
            if hit:
                return self.fail(f"'{name}' walks into {hit} at ({p[0]:.1f}, {p[1]:.1f})")
            who = next((at for at in people if math.dist(p, at) < .35 + BODY + .15), None)
            if who:
                return self.fail(f"'{name}' walks through someone standing at {who}")

    def check_scoria(self, name, to):
        people = SCORIA_FOLK + [S["TovraAt"], S["BrekkStall"]]
        self.ground(name, to, SCORIA_WAYS, BUILT_SCORIA, people)

    def check_foundry(self, name, to):
        for p in self.legs(to):
            clear, room = foundry_room(p)
            # A body's width all round on some floor: where two rooms join (a stair's foot in the gallery) each is shallow and the union is not.
            rim = [foundry_room((p[0] + BODY * math.cos(a), p[1] + BODY * math.sin(a)))[0] for a in [k * math.pi / 4 for k in range(8)]]
            if clear > 0 or max(rim) > 0:
                return self.fail(f"'{name}' leaves the Foundry's floors at ({p[0]:.1f}, {p[1]:.1f}): {clear:.1f} m from {room}'s edge")
            hit = next((label for label, c, yaw, hx, hz in [("a cover stack", s, 0, 1.4, 1.4) for s in COVER_STACKS] if box_distance(p, c, yaw, hx, hz) < BODY + .15), None)
            if hit:
                return self.fail(f"'{name}' walks into {hit} at ({p[0]:.1f}, {p[1]:.1f})")
        # Off the walked lines only where the floor is a fight's or the heart's, which the builder keeps clear, or before what a hero works.
        self.ground(name, to, FOUNDRY_WAYS + [[C_HEART, (0, 229)], [C_HEART, (0, 272)]], BUILT_FOUNDRY, [], off=2.5)

    def add(self, name, seconds, **fields):
        if fields.get("expectedFlag"):
            self.seen.add(fields["expectedFlag"])
        super().add(name, seconds, **fields)

    def check_town(self, name, to):
        # The armoury gate's grille is shut, solid and carving, until the town has answered (Town.StairAndCells: hidden from vask.deposed); the scene is built with it shut.
        grille = [] if "vask.deposed" in self.seen else [("the armoury gate's grille", ARMOURY_GRILLE, 0, .3, 1.7)]
        self.ground(name, to, TOWN_WAYS, BUILT_TOWN + grille, TOWN_FOLK + self.town_people)
        # A walk that goes into a talk's line before its talk has been heard (and once what it waits for has been) must stop when the talk opens: walked on, it stood the
        # talk's length short of its point and timed out (route 50 into the trophy gallery, whose line begins at z 99, reconnaissance s50, November 14).
        if not self.problems and self.walk_stop != "ui:dialogue":
            for p in self.legs(to):
                talk = next((t for t in TOWN_CROSSINGS if t[3] not in self.seen and (not t[4] or t[4] in self.seen)
                             and abs(p[0] - t[1][0]) <= t[2][0] + BODY and abs(p[1] - t[1][1]) <= t[2][1] + BODY), None)
                if talk:
                    return self.fail(f"'{name}' walks into {talk[0]}'s line at ({p[0]:.1f}, {p[1]:.1f}) without stopping for its talk")
        if not self.problems and not self.across_loop:
            for p in self.legs(to):
                loop = next((l for l in TOWN_LOOPS if segment_distance(p, l[0], l[1]) < .45 + BODY), None)
                if loop:
                    return self.fail(f"'{name}' crosses a townsperson's errand {loop} at ({p[0]:.1f}, {p[1]:.1f})")

    def check_harbour(self, name, to):
        x0, x1, z0, z1 = HB_LIMITS
        for p in self.legs(to):
            if not (x0 + 1 + self.hull + .3 < p[0] < x1 - 1 - self.hull - .3 and z0 + 1 + self.hull + .3 < p[1] < z1 - 1 - self.hull - .3):
                return self.fail(f"'{name}' flies into the harbour line at ({p[0]:.1f}, {p[1]:.1f})")
            hit = next((label for label, c, yaw, hx, hz in HARBOUR_SOLIDS if box_distance(p, c, yaw, hx, hz) < self.hull + .5), None)
            if hit:
                return self.fail(f"'{name}' flies within a hull of {hit} at ({p[0]:.1f}, {p[1]:.1f})")

    def fight_ground(self, id_, rounds=4, seconds=30, skills=True):
        """As every chapter's: and the party is wherever the fight left it, so the next walk finds its way and is checked from its end only."""
        super().fight_ground(id_, rounds, seconds, skills)
        self.fought()

    def fight_flight(self, id_, rounds=3, seconds=30):
        super().fight_flight(id_, rounds, seconds)
        self.fought()

    # -------------------------------------------------------------- the driver's hands for chapter 8
    def wait_clear(self, name, way, seconds=40, brake=None):
        """Hold still until the way from where the lead stands through these plan points is clear of what the room times (ReplayWays): a beam, the crust, a pour."""
        text = ";".join(f"{x:g},{z:g}" for x, z in way)
        flying = self.speed >= FLIGHT_SPEED or self.brake
        self.add(name, seconds, until="clear:" + text, leftTrigger=1 if (flying if brake is None else brake) else None)

    def go(self, name, points_, **kw):
        """Walk a list of plan points (x, z), each at the floor's own height."""
        for i, (x, z) in enumerate(points_):
            self.walk(f"{name} {i + 1}/{len(points_)}", x, None, z, **kw)

    def cross(self, name, way, wait=40, **kw):
        """A way across what a room times: the wait for it to be clear, then the walk, point by point."""
        self.wait_clear(f"wait for {name}", way, wait)
        self.go(name, way, **kw)

    def fight_on(self, id_, rounds=4, seconds=30):
        """The rest of a fight whose boss has gone (Vessa Korl's squad fights on after she leaves): no door, no enter, rounds until the encounter's own flag."""
        stop = f"flag:clear.{id_}"
        for r in range(rounds):
            self.rotation(f"{id_} squad {r + 1}", stop)
            self.add(f"fight {id_}'s squad round {r + 1}", seconds, buttons=["South"], pulseSeconds=0.46, navigate=True, approachTarget=True, tolerance=2.3, rangedTolerance=12,
                     evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=0, z=0))
        self.add(f"{id_}'s squad is down", 4, until=stop, expectedFlag=f"clear.{id_}")
        self.recover(id_ + " squad")
        self.drifted = True

    def stand_for(self, what, stand, prompt, range_, tolerance=.4, **kw):
        """Walk to a stand off the ways to press a prompt there, its reach checked here (and again by RouteFightTests on the built zone)."""
        reach(f"{self.name} '{what}'", stand, tolerance, prompt, range_)
        # The floor between the way and the stand: a step off the way to what is pressed (the ways stop short of most prompts), and back.
        here = self.at if self.at is not None else stand
        self.stands.append((stand, max(1.0, math.dist(stand, here) + .5)))
        self.walk(f"to {what}", stand[0], None, stand[1], tolerance=tolerance, **kw)

    def shop(self, name, lines, row="Repair Gel"):
        """A talk that ends at a counter: read it, fill the pouch to the cap with what fits, leave."""
        self.add("start " + name, 0.2, buttons=["South"], expectedUi="dialogue")
        self.add("release start " + name, 0.3, expectedUi="dialogue")
        for i in range(lines):
            self.add(f"read {name} line {i + 1}", 1.9, stopWhen="ui:shop")
            self.add(f"next {name} line {i + 1}", 0.15, buttons=["South"], stopWhen="ui:shop")
            self.add(f"release {name} line {i + 1}", 0.2, stopWhen="ui:shop")
        self.add(f"finish {name}", 40, buttons=["South"], pulseSeconds=1.0, until="ui:shop", expectedUi="shop")
        self.add(f"find the {row.lower()}", 3, pickRow=row, until="row:" + row, expectedUi="shop")
        self.stock(f"buy {row.lower()} up to the cap", row)
        self.add(f"leave the counter after {name}", 0.5, buttons=["East"], expectedUi="world")
        self.add(f"release the counter after {name}", 0.3)

    def write(self):
        if self.problems:
            raise SystemExit("\n".join(self.problems) + "\nHARROWGATE_ROUTES_FAILED")
        mh.Route.write(self)


def boss(r, id_, door, x, y, z, spawn, rounds=10):
    """A boss on foot from its door (CHAPTER_GUIDE section 10): manyhands_routes' boss, with the door's checks at Idra's hull."""
    mh.boss(r, id_, door, x, y, z, spawn, rounds)


# ------------------------------------------------------------------ the routes
H = mh.H


def way_in():
    r = Checked(PREFIX + "44-harrowgate-way-in", "Chapter 8: the chart from the Skyfields to Many Hands, Taren and Idra out, Brekk's word on the dock, Idra's plan, the hull's warp to the Picket", chapter=8)
    # Chapter 7 ends saved somewhere in the Skyfields (Ishen's camp): nothing here walks from there, the chart takes the party to the dock. Every chapter 7 route is
    # played with Taren and Idra out (their "deployed"), so the save it leaves has them out, and this route names the same pair: a save with any other pair fails at
    # the first step with a party, saying both. Taren is put in the lead with lead() alone, a hero already out. It brought Idra out with deploy() as well, and with
    # her already out that step is one Swap: it ends the frame she takes the lead, so the only frame on its record is the one before, and the analysis read
    # Taren there and rejected the run ("Idra comes out and leads: wrong hero", reconnaissance s44, November 14) though the game had done just what was asked.
    r.resume("OrielSkyfields", "chapter7.complete", speed=COMBAT_SPEED)
    r.chart("ManyHands", "ManyHands", spawn=H["Arrival"], form="Natural")
    r.speed = 4.5
    r.lead("Taren")
    r.people = {"Corra", "Dov", "Hadda", "Binna", "Ruva", "Teo", "Garo", "Dabb", "Maru", "Ona", "Pim", "Pell", "Abri", "Ilo", "DessikHeld", "JaskGarden"}
    r.brekk_on_dock = True
    # Brekk's word is a crossing a step north of him on the dock, (0, -44.4), 16 by 3: walking up off the pads meets it.
    r.walk("up off the pads", 0, 0, -55, tolerance=0.5)
    r.walk("up the dock until Brekk speaks", 0, 0, -40, stop="ui:dialogue")
    r.crossing("Brekk's word from Scoria", 6, "vask.brekkWord")  # BrekkScoriaWord: starts Harrowgate
    r.walk("on up the dock", 0, 0, -40)
    r.brekk_on_dock = False  # gone from the dock once Idra has laid the Picket out (vask.lockAdmitted): not before the walk up
    reach(f"{r.name} 'Idra's plan'", H["ChartTalk"], 0.4, H["ChartTalk"], PLAN_RANGE)
    r.go("up to the chart table", [(0, -20), H["Crossing"]] + mh.SPINE_UP + [H["StairFoot"], H["StairTop"], H["ChartTalk"]], tolerance=0.4)
    r.look("hear Idra's plan for the Picket", 12, "vask.lockAdmitted")  # IdraPicketPlan (staged by StageManyHands): the quest's first step; opens the hull's warp
    r.go("down to the hull pad", [H["StairTop"], H["StairFoot"], (0, 20)] + mh.SPINE_DOWN + [(0, -20), (0, -40), (0, -55), (0, -57), (5.2, -58.2), (5, -61)], tolerance=0.5)
    r.scene = "ManyHandsHull"
    r.add("launch to the hull", 4, buttons=["South"], expectedForm="Flight")
    r.add("release launch", 0.5)
    r.at, r.speed = mh.O["ArrivalAt"], FLIGHT_SPEED
    # Off the stern dock south, then west along the stern clear of chapter 6's and 7's warps, to chapter 8's beyond Oriel's (StageManyHands).
    r.walk("south off the stern dock", -26, 1, -148, tolerance=3)
    r.walk("west along the stern", -80, 1, -164, tolerance=3)
    r.walk("on west past Oriel's warp", -132, 1, -166, tolerance=3)
    r.brake = True
    # Its reach is 5 and its piece (2 m) is solid: four metres east of its middle keeps Idra's hull clear of the piece and the walk's end well inside the reach.
    stand = (WARP_TO_PICKET[0] + 4.0, WARP_TO_PICKET[1] + .4)
    reach(f"{r.name} 'the warp to the Picket'", stand, 0.6, WARP_TO_PICKET, 5)
    r.walk("settle at the warp to the Picket", stand[0], 1, stand[1], tolerance=0.6)
    r.brake = False
    r.scene = "PicketOfTeeth"
    r.add("take the warp to the Picket of Teeth", 4, buttons=["South"], expectedForm="Flight")
    r.add("release warp", 0.5)
    r.at = P["Arrival"]
    r.wait("first look at the border", 2, expectedFlag="vask.lockAdmitted")
    r.write()


def picket():
    r = Checked(PREFIX + "45-picket-of-teeth", "Chapter 8: Idra at the edge, the quiet route shadow to shadow, B4 shot down, the lock in sight, the four pieces worked from the curtain's front line while the hunter wing hunts and loses the ship, down to the Flows", chapter=8)
    r.resume("PicketOfTeeth", "vask.lockAdmitted", speed=FLIGHT_SPEED, spawn=P["Arrival"], form="Flight")
    r.lead("Taren")
    # Idra's lesson is a line across the lane at z 78; out of every beam's reach until z 100.
    r.walk("up the approach", 0, 1, 60, tolerance=3)
    r.walk("on until Idra speaks", 0, 1, EDGE_Z + 14, stop="ui:dialogue")
    r.crossing("Idra at the picket's edge", 5, "vask.picketRead")  # IdraPicketEdge
    start = QUIET[0][0]
    r.walk("to the first tooth's edge", start[0], 1, start[1], tolerance=2)
    r.brake = True
    r.walk("settle at the first tooth's edge", start[0], 1, start[1], tolerance=0.8, seconds=6)
    r.brake = False
    # Pocket to pocket: each leg set off on only once its way is clear of every beam (the wait), flown at speed through its bends, then settled into its pocket braked.
    # In each pocket the nose is first turned down the next leg and Idra given a moment to fall in behind it. Her place is behind the lead's nose (PartnerBrain), and a
    # settle that runs past its pocket and turns back to it leaves the nose the wrong way: in pocket 9 it pointed south with leg 10 going north, she sat across the leg,
    # and the lead slid round her hull into Rock 8 and stuck there, scraped every 0.6 s, until both of the leg's bends timed out (reconnaissance s45, November 14).
    leg, n = [], 0
    for at, pocket in QUIET[1:]:
        leg.append(at)
        if not pocket:
            continue
        n += 1
        r.face(f"turn the nose down leg {n}", leg[0])
        r.add(f"let Idra fall in behind for leg {n}", FALL_IN, leftTrigger=1)
        r.wait_clear(f"wait in the shadow for leg {n}", leg, 40, brake=True)
        for k, bend in enumerate(leg[:-1]):
            r.walk(f"leg {n} round the rock {k + 1}", bend[0], 1, bend[1], tolerance=2)
        # The last few metres into a pocket braked: at speed the settle ran on past pocket 10 into Rock 9 behind it (HarrowgateReplayTests, November 14).
        r.walk(f"leg {n} to its pocket", leg[-1][0], 1, leg[-1][1], tolerance=POCKET_BRAKE)
        r.brake = True
        r.walk(f"settle into pocket {n}", leg[-1][0], 1, leg[-1][1], tolerance=0.8, seconds=6)
        r.brake = False
        leg = []
    # B4 watches the lock's approach from the south (its arc is 140 to 220): from its north side, out of its arc and B3's reach, it is shot down, and its last call
    # brings amber and no more. Its fight's box is 30 by 30 on it; 10 m north is inside it and outside its 8 m flare.
    b4 = next(at for name, at, *_ in BUOYS if name == "B4")
    north = (b4[0], b4[1] + 11)
    over = (-30, 590)  # north of the last pocket first, round the rock east of it: everything north of z 570 is out of every beam (B4 looks south, B3 is out of reach)
    r.face("turn the nose toward B4's north side", over)
    r.add("let Idra fall in behind for B4's north side", FALL_IN, leftTrigger=1)
    r.wait_clear("wait for the way to B4's north side", [over, north], 30, brake=True)
    r.walk("north out of the last pocket", over[0], 1, over[1], tolerance=2.5)
    r.walk("to B4's north side", north[0], 1, north[1], tolerance=2)
    r.brake = True
    r.walk("settle north of B4", north[0], 1, north[1], tolerance=1, seconds=6)
    r.brake = False
    r.fight_flight("Buoy_4", rounds=3)
    r.fought()
    # The lock in sight: a line across the lane at z 604, the main quest's step and a checkpoint (spawn Lock).
    r.walk("toward the lock until Idra speaks", 0, 1, LOCK_SEEN_Z + 4, stop="ui:dialogue")
    r.crossing("the lock in sight", 4, "vask.picketCrossed")  # IdraLockReached
    # The lock is worked from its line (LOCK_LINE): north of the flak curtain's rows and a hull short of the seal, the one place in front of the lock no volley reaches. The first
    # piece off trips the lock's own alarm and wakes the gates; from then on the ship never goes south of the line. It was worked from z 629.4, in the curtain's front row:
    # the walks to the other three were braked from end to end, 2 m/s against the 11 the steps were timed for, every volley rolled the ship 4 m back, none of them was
    # reached, and the flak wore the lead down to a defeat (reconnaissance s45, November 14).
    # Two stands: the port collar from the west one; the starboard collar, the keel collar and the keeper bar all from the east one, so that the lead moves along the line
    # once, east, away from where Idra is. At the west stand the nose is turned south (her place behind it is west of him, against the seal); at the east one west (her place
    # is east of him, against the seal, and once the seal is gone in the open gap short of the wall, so out of the curtain either way), and the way out through the gap is
    # north-west, away from her again. Behind a west-facing nose her place is BEHIND east of the lead (and BESIDE north): the east stand is far enough west that her hull there
    # is clear of the wall's body beyond the gap.
    west, east = (-1.5, LOCK_LINE), (round(GAP - HULL - BEHIND - .2, 1), LOCK_LINE)
    stands = {"vask.lock1": west, "vask.lock2": east, "vask.lock3": east, "vask.lock4": east}
    for name, at, prompt, flag in PIECES:
        reach(f"{r.name} '{name}'", stands[flag], LOCK_TOLERANCE, at, DOCK_RANGE)
    first = PIECES[0]
    r.walk("toward the port collar", west[0], 1, west[1] - 4, tolerance=1.5)
    r.brake, r.on_lock_line = True, True
    r.walk("up to the lock's line for the port collar", west[0], 1, west[1], tolerance=LOCK_TOLERANCE, seconds=8)
    r.work_at_lock("take the port collar off", first[3], nose="south")
    # The hunter wing the first piece launches comes off the carrier's deck (44, 520), a hundred and twenty metres off: a Lancer loses a ship it hasn't seen inside 40 m for
    # 2 s (EnemyStrafer.LosesAt, its linger), so the wing loses this one at once and circles where it lost it, and the alarm comes back down on its own ("wing lost",
    # PicketAlarm). It is never fought: chasing it out over the field is into the buoys' beams.
    r.flak_awake = True
    r.walk("along the lock's line to the starboard collar", east[0], 1, east[1], tolerance=LOCK_TOLERANCE)
    for i, (name, at, prompt, flag) in enumerate(PIECES[1:]):
        r.work_at_lock(f"take the {name.lower()} {'out' if 'bar' in name else 'off'}", flag, nose="west" if i == 0 else None)
    r.lock_open = True
    # Idra's word waits for nothing awake within 15 m of the ship: the gates stand 24 m off the lane's middle. It is heard here, on the line; the world holds still for it.
    r.crossing("the lock is off", 5, "vask.lockRemoved", wait=12)  # IdraLockOff (an AfterTalk, 2.5 s after the keeper bar)
    r.brake, r.on_lock_line = False, False
    r.walk("through the gap", -1, 1, WALL_Z + 12, tolerance=3)
    way = P["ScoriaWay"]
    r.walk("north toward Scoria", way[0] + 2, 1, way[1] - 16, tolerance=3)
    r.brake = True
    stand = (way[0], way[1] - 4.4)
    reach(f"{r.name} 'Land — The Scoria Flows'", stand, 1, way, DOCK_RANGE)
    r.walk("settle over the way down to Scoria", stand[0], 1, stand[1], tolerance=1)
    r.brake = False
    r.scene = "ScoriaFlows"
    r.add("land on the Scoria Flows", 6, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release landing", 0.5)
    r.at, r.speed = S["Arrival"], COMBAT_SPEED
    r.wait("first look at the Flows", 2, expectedFlag="vask.lockRemoved")
    r.write()


# The battery's field (F7): sixteen plates each on its own clock (HarrowgateBuilder.Scoria), three rows between the north shelf and the terrace's ramp. The declared way runs
# down the seam between two columns of plates, so a body on it stands on two plates at a time and both must hold: southward from the ramp that way was clear 3% of the time,
# for 111 s together never, and its 40 s wait ran out (reconnaissance s47, November 14). Down the middle of one column it is three plates: the east column southward (clear
# 17% of the time, the longest wait 51 s), the west one northward (18%, 28 s), read over fifteen minutes of the field's own clocks by the driver's own sums (ReplayWays.Underfoot).
# The columns' middles are a metre or two off the declared way, on the same open crust: allowed as stands for the crossing alone.
BATTERY_SOUTH = [(1.8, 185), (1.8, 169), (1.2, 166)]
BATTERY_NORTH = [(-1, 169), (-1, 185), (0, 189)]
BATTERY_WAIT = 75


def battery_field(r, name, way):
    """Across the battery's field by one column of its plates, waiting for the three on the way (BATTERY_SOUTH, BATTERY_NORTH)."""
    allowed = [((x, 176.5), 10) for x in sorted({p[0] for p in way[:2]})]
    r.stands += allowed
    r.cross(name, way, wait=BATTERY_WAIT, tolerance=0.8)
    for a in allowed:
        r.stands.remove(a)


def flows():
    r = Checked(PREFIX + "46-scoria-flows", "Chapter 8: Brekk on the crust, the first tongue, the causeway, the delta, the squall, the battery, the camp: Tovra and Brekk's stall", chapter=8)
    r.resume("ScoriaFlows", "vask.lockRemoved", speed=COMBAT_SPEED, spawn=S["Arrival"])
    r.lead("Taren")
    # Brekk's crust: a crossing across the landing's edge, (1, 19.5), 30 wide.
    r.walk("up from the landing", 2.6, 0, 15)
    r.walk("on until Brekk speaks", 6, 0, 30, stop="ui:dialogue")
    r.crossing("Brekk on the crust", 4, "vask.crustRead")  # BrekkCrust
    r.walk("on past the lesson tongue", 6, 0, 30)
    r.walk("to the first tongue's south bank", 5, 0, 40, tolerance=0.6)
    # F2, a wave from the east: wait on the bank until the plates on the line will hold for the crossing (ReplayWays), then cross.
    r.cross("the first tongue", [(4, 52)], tolerance=0.6)
    r.walk("on toward the causeway", 0.4, 0, 62)
    r.walk("on until Brekk names the pikes", 0, 0, 72, stop="ui:dialogue")
    r.crossing("the garrison's causeway", 4, "vask.garrisonSeen")  # BrekkCrossing
    r.walk("onto the causeway", 0, 0, 84, tolerance=0.6)
    r.fight_ground("Scoria_Crossing", rounds=5)
    r.walk("on to the delta's south shelf", 0, 0, 96)
    r.walk("to the delta's bank", -1, 0, 104, tolerance=0.6)
    # The delta: a tide from the north-east, island to island behind it; its crawlers wake as the party walks out onto it.
    r.cross("the delta to island A", [S["IslandA"]], tolerance=0.8)
    r.fight_ground("Scoria_Delta", rounds=6)
    # After the fight the party is wherever it ended, on an island, a plate or the bank: the wait is from there.
    r.wait_clear("wait for the way to island B", [S["IslandB"]])
    r.walk("to island B", S["IslandB"][0], 0, S["IslandB"][1], tolerance=0.8)
    r.cross("the delta to the north shelf", [(2, 152)], tolerance=0.8)
    r.walk("onto the north shelf", 2, 0, 160)
    # The battery's field: plates each on its own clock, under the mortars; across to the ramp's foot, then the fight on the terrace above.
    r.walk("to the battery field's south bank", 1.2, 0, 166, tolerance=0.6)
    battery_field(r, "the battery's field", BATTERY_NORTH)
    r.fight_ground("Scoria_Battery", rounds=5)
    r.walk("up onto the battery terrace", 1, 0, 198)
    r.go("to the camp", [S["Junction"], (16, 209), (28, 208)] + INTO_CAMP, tolerance=0.6)
    r.stands.append((CAMP_YARD, CAMP_YARD_ROUND))
    tovra = (S["TovraAt"][0], S["TovraAt"][1] - 2.2)
    r.go("round the camp fire's south side", CAMP_SOUTH, tolerance=0.5)
    r.stand_for("Tovra", tovra, S["TovraAt"], TALK, tolerance=0.4)
    r.talk("Tovra Kesh", 12, flag="vask.unswornMet")  # TovraFirst
    # Brekk stands before his stall's counter, facing the yard: back round the fire's east side to him.
    r.go("round the camp fire to Brekk's stall", [CAMP_SOUTH[1], (47.2, 199.6)], tolerance=0.5)
    r.stand_for("Brekk's stall", STALL_STAND, S["BrekkStall"], TALK, tolerance=0.4)
    r.shop("Brekk's stall", 1)  # BrekkCampStall: its stall, gel to the cap before the Foundry
    r.go("back across the yard, north of the anvil", [(44, 201.6)], tolerance=0.5)
    r.stand_for("the camp cradle", CAMP_CRADLE_STAND, S["CampCradle"], 3, tolerance=0.4)
    r.save_at("repair and save at the camp cradle", "release the cradle")
    r.write()


def flows_errands():
    r = Checked(PREFIX + "47-scoria-errands", "Chapter 8: Tovra's cages and her offer, the pike-head, the three kettle stones and the gate garrison, Tovra told, a save at the camp", chapter=8)
    r.resume("ScoriaFlows", "vask.unswornMet", speed=COMBAT_SPEED, spawn=S["CampSpawn"])
    r.lead("Taren")
    # The yards a kettle stone's fire stands in, off the trodden line: the causeway garrison's north of it, the gate garrison's by the Foundry road.
    r.stands += [(CAMP_YARD, CAMP_YARD_ROUND), ((4, 96.5), 4.5), ((4, 245), 6)]
    tovra = (S["TovraAt"][0], S["TovraAt"][1] - 2.2)
    r.go("round the camp fire's south side", CAMP_SOUTH, tolerance=0.5)
    r.stand_for("Tovra", tovra, S["TovraAt"], TALK, tolerance=0.4)
    r.talk("how well the cages work", 4, flag="tovra.cagesHeard")  # TovraCages
    r.talk("Tovra's quiet hands", 4)  # TovraQuietOffer: starts Quiet Hands
    pike = (S["PikeHead"][0] - 1.4, S["PikeHead"][1] - .8)
    r.stand_for("the pike-head", pike, S["PikeHead"], 2.6, tolerance=0.4)
    r.look("look at the pike-head", 5, "brekk.pikeDressed")  # BrekkPikeHead
    # North first: the last tongue, the gate garrison, the third kettle stone behind it.
    r.go("back across the camp", [(46.8, 206.8), CAMP_SOUTH[1], CAMP_SOUTH[0]] + INTO_CAMP[::-1][1:] + [(28, 208), (16, 209), S["Junction"]], tolerance=0.6)
    r.walk("to the last tongue's south bank", 3, 0, 214, tolerance=0.6)
    r.cross("the last tongue", [(1, 222), (0, 232)], tolerance=0.8)
    r.walk("up the Foundry road", 0, 0, 236)
    r.fight_ground("Scoria_Gate", rounds=5)
    third = KETTLES[3]
    r.stand_for("the third kettle stone", (third[0] - 1.2, third[1] - .9), third, KETTLE_RANGE, tolerance=0.4)
    r.press_for("leave the third message under the kettle stone", "vask.kettle3")
    r.go("back to the Foundry road", [(0.5, 240)], tolerance=0.6)
    r.walk("to the last tongue's north bank", .5, 0, 229, tolerance=0.6)
    r.cross("the last tongue back", [(4, 210)], tolerance=0.8)
    # South: the battery terrace's fire, then across the battery's field, the shelf, the delta and down to the causeway's.
    r.go("down to the battery terrace", [(1, 198), (0, 196), (-12, 197)], tolerance=0.6)
    second = KETTLES[2]
    r.stand_for("the second kettle stone", (second[0] + .4, second[1] - .6), second, KETTLE_RANGE, tolerance=0.4)
    r.press_for("leave the second message under the kettle stone", "vask.kettle2")
    r.go("back along the terrace to the ramp", [(-12, 197), (0, 196), (0, 189.5)], tolerance=0.6)
    battery_field(r, "the battery's field southward", BATTERY_SOUTH)
    r.go("over the north shelf", [(2, 160), (2, 152)], tolerance=0.8)
    r.cross("the delta to island B", [S["IslandB"]], tolerance=0.8)
    r.cross("the delta to island A", [S["IslandA"]], tolerance=0.8)
    r.cross("the delta to its south shelf", [(-1, 104)], tolerance=0.8)
    first = KETTLES[1]
    r.walk("to the causeway garrison's fire", 0, 0, 96)
    r.stand_for("the first kettle stone", (first[0] - 1.6, first[1] - .5), first, KETTLE_RANGE, tolerance=0.4)
    r.press_for("leave the first message under the kettle stone", "vask.kettle1")
    # And back up the valley to Tovra.
    r.go("back to the delta's bank", [(0, 96), (-1, 104)], tolerance=0.6)
    r.cross("the delta north to island A", [S["IslandA"]], tolerance=0.8)
    r.cross("the delta north to island B", [S["IslandB"]], tolerance=0.8)
    r.cross("the delta to the north shelf again", [(2, 152)], tolerance=0.8)
    r.go("over the north shelf again", [(2, 160), (1.2, 166)], tolerance=0.6)
    battery_field(r, "the battery's field northward", BATTERY_NORTH)
    r.go("up the terrace and on to the camp", [(1, 198), S["Junction"], (16, 209), (28, 208)] + INTO_CAMP + CAMP_SOUTH, tolerance=0.6)
    r.stand_for("Tovra again", tovra, S["TovraAt"], TALK, tolerance=0.4)
    r.talk("the messages are left", 2, flag="tovra.messagesLeft")  # TovraQuietReturn
    r.go("to the camp cradle", CAMP_SOUTH[::-1], tolerance=0.5)
    r.stand_for("the camp cradle", CAMP_CRADLE_STAND, S["CampCradle"], 3, tolerance=0.4)
    r.save_at("repair and save at the camp cradle", "release the cradle")
    r.write()


def foundry():
    r = Checked(PREFIX + "48-bridle-foundry", "Chapter 8: the Foundry gate, the yard, two wheels, the ladle and the press, the charging floor, the gallery, Vessa Korl and her squad, the heart cradle", chapter=8)
    r.resume("ScoriaFlows", "tovra.messagesLeft", speed=COMBAT_SPEED, spawn=S["CampSpawn"])
    r.lead("Taren")
    r.go("to the junction", INTO_CAMP[::-1][1:] + [(28, 208), (16, 209), S["Junction"]], tolerance=0.6)
    r.walk("to the last tongue's south bank", 3, 0, 214, tolerance=0.6)
    r.cross("the last tongue", [(1, 222), (0, 232)], tolerance=0.8)
    gate = S["FoundryGate"]
    stand = (gate[0], gate[1] - 3.6)
    r.go("up the Foundry road past the gate garrison", [(0, 240), S["FoundrySpawn"]], tolerance=0.6)
    r.stand_for("the Foundry gate", stand, gate, FOUNDRY_ROAD_RANGE, tolerance=0.4)
    r.scene = "BridleFoundry"
    r.add("in at the Foundry gate", 5, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the gate", 0.5)
    r.at = FD["Arrival"]
    # The yard: two Forge Hounds on open floor.
    r.walk("into the yard", 0, None, 12)
    r.fight_ground("Foundry_Yard", rounds=5)
    r.walk("across the yard", 0, None, 30)
    r.walk("on into the wheel hall until Idra speaks", 0, None, 48, stop="ui:dialogue")
    r.crossing("the first sluice wheel", 3, "vask.sluiceRead")  # IdraFirstSluice
    hand = FD["WheelOneHand"]
    r.walk("toward the first wheel", -10, None, 50, tolerance=0.6)
    r.stand_for("the first wheel", (hand[0] + .4, hand[1] + .2), hand, WHEEL_RANGE, tolerance=0.4)
    r.press_for("turn the first sluice wheel", "vask.sluice1")  # floods tap run A, arms ladle line L
    r.wait("the coolant floods tap run A", 4)
    r.go("over tap run A's crust", [(-10, 50), (0, 56), (0, 66)], tolerance=0.6)
    # The gangway: ladle line L pours across it on its ten-second timetable.
    r.cross("the gangway under the ladle", [(0, 80)], tolerance=0.6)
    r.walk("onto the charging floor", 0, None, 88)
    r.fight_ground("Charging_Floor", rounds=5)
    # The press run east to the second wheel: the press is quenched and still until the wheel turns.
    r.walk("back to the charging floor's middle", 0, None, 96)
    r.go("east along the press run", [(20, 96), (34, 96), (42, 99)], tolerance=0.6)
    hand = FD["WheelTwoHand"]
    r.stand_for("the second wheel", (hand[0] - .4, hand[1] - .2), hand, WHEEL_RANGE, tolerance=0.4)
    r.press_for("turn the second sluice wheel", "vask.sluice2")  # floods tap run B, drains the tank, arms the press
    r.fight_ground("Press_Bay", rounds=5)
    r.walk("back to the press run's east end", 34, None, 96, tolerance=0.6)
    r.cross("the press run under the press", [(20, 96)], tolerance=0.6)
    r.go("across the charging floor to the stair head", [(0, 96), (-13, 108), (-13, 121)], tolerance=0.6)
    r.walk("down the stair to the gallery", -13, None, 134, tolerance=0.6)
    # Gallery south: ladle line L2 across the whole room; its fight wakes once over it.
    r.cross("the gallery's ladle line", [(-6, 142), (-3, 146)], tolerance=0.6)
    r.fight_ground("Gallery_Drones", rounds=5)
    cradle = FD["GalleryCradle"]
    r.walk("to the gallery's north side", 0, None, 150)
    r.stand_for("the gallery cradle", (cradle[0] - 1.6, cradle[1]), cradle, 3, tolerance=0.4)
    r.save_at("repair and save at the gallery cradle", "release the cradle")
    # The casting gallery: Vessa's word as the party comes in at its door (saves at Gallery), then her floor.
    r.walk("to the gallery door", 0, None, 154)
    r.walk("in through the door until Vessa speaks", 0, None, 163, stop="ui:dialogue")
    r.crossing("Vessa on the casting gallery", 4, "vask.vessaMet")  # VessaGallery
    r.stands.append((FD["GallerySpawn"], 3))
    # Her door: from the checkpoint (Gallery, (-3, 149)) through the south door into her floor, well inside her fight's volume (z 159 to 201) and short of her racks.
    boss(r, "VessaKorl", "onto the casting gallery's floor", 0, FOUNDRY_LEVELS["Cast"], 167, spawn=FD["GallerySpawn"], rounds=10)
    # She leaves at nought by the gantry door (bossdown.VessaKorl); her pikeguard pair fights on.
    r.fight_on("VessaKorl", rounds=4)
    r.walk("across the gallery to the heart stair", 0, None, 198)
    r.walk("down the heart stair", 0, None, 216)
    cradle = FD["HeartCradle"]
    r.stand_for("the heart cradle", (cradle[0] - 1.6, cradle[1]), cradle, 3, tolerance=0.4)
    r.save_at("repair and save at the heart cradle", "release the cradle")
    r.write()


def colossus():
    r = Checked(PREFIX + "49-slag-colossus", "Chapter 8: the caged road, the Slag Colossus at its sluices, the release, Brekk by the anvil, the way out, Tovra's road, the town road", chapter=8)
    r.resume("BridleFoundry", "vask.vessaBeaten", speed=COMBAT_SPEED, spawn=FD["HeartSpawn"])
    r.lead("Taren")
    r.stands.append((FD["HeartSpawn"], 3.5))
    r.walk("toward the heart's door until Sela speaks", 0, None, 226.6, stop="ui:dialogue")
    r.crossing("the caged road", 4, "vask.cageSeen")  # SelaCagedRoad: saves at Heart
    # The door: from the checkpoint (Heart, (-3, 220)) through the door at z 227 to well inside its volume (z 229 to 273). The driver works the sluices whose seat rings
    # show while nothing of it can be hurt, and flicks the lock to a plate that is dark (ContinuousReview.Rooms.cs).
    boss(r, "SlagColossus", "into the heart", 0, FOUNDRY_LEVELS["Deep"], 236, spawn=FD["HeartSpawn"], rounds=12)
    r.add("reach the fallen hero after the Colossus", 16, navigate=True, approachPartner=True, tolerance=1.2, stopWhen="partyAlive", point=dict(x=0, y=0, z=0))
    r.add("both heroes ready after the Colossus", 6, until="partyAlive")
    r.crossing("the release", 13, "vask.juvenileFreed", wait=14)  # TovraRelease (an AfterTalk, 7.5 s after the fall)
    r.lead("Taren")
    r.fought()
    anvil = (ANVIL_STAND[0] + 1.4, ANVIL_STAND[1] - 1.6)
    r.walk("across the heart toward the anvil", 0, None, 262)
    r.stand_for("Brekk by the anvil", anvil, ANVIL_STAND, ANVIL_RANGE, tolerance=0.4)
    r.look("stand by the anvil", 1, "vask.anvilSeen")  # BrekkAnvil
    # Back up and out: the heart, the stair, the gallery, the second ladle line, the stair, the charging floor, the gangway's ladle, the wheel hall, the yard, the mouth.
    r.go("back across the heart to its door", [(0, 262), (0, 240), (0, 229)], tolerance=0.6)
    r.go("up the heart stair and across the gallery", [(0, 218), (0, 198), (0, 160), (0, 150)], tolerance=0.6)
    r.cross("the gallery's ladle line back", [(-6, 142), (-13, 134)], tolerance=0.6)
    r.go("up the stair and across the charging floor", [(-13, 121), (-13, 108), (0, 92), (0, 82)], tolerance=0.6)
    r.cross("the gangway back", [(0, 66)], tolerance=0.6)
    home = FD["WayHome"]
    stand = (home[0], home[1] + 2.6)
    reach(f"{r.name} 'the Foundry mouth'", stand, 0.4, home, FOUNDRY_HOME_RANGE)
    r.go("through the wheel hall and the yard to the mouth", [(0, 56), (-10, 50), (0, 46), (0, 30), (0, 12)], tolerance=0.6)
    r.walk("to the Foundry mouth", stand[0], None, stand[1], tolerance=0.4)
    r.scene = "ScoriaFlows"
    r.add("out by the Foundry mouth", 5, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the mouth", 0.5)
    r.at = S["FoundrySpawn"]
    r.go("down the Foundry road", [(0, 240)], tolerance=0.6)
    r.walk("to the last tongue's north bank", .5, 0, 229, tolerance=0.6)
    r.cross("the last tongue", [(4, 210)], tolerance=0.8)
    r.stands.append((CAMP_YARD, CAMP_YARD_ROUND))
    r.go("to the camp", [(16, 209), (28, 208)] + INTO_CAMP + CAMP_SOUTH, tolerance=0.6)
    tovra = (S["TovraAt"][0], S["TovraAt"][1] - 2.2)
    r.stand_for("Tovra", tovra, S["TovraAt"], TALK, tolerance=0.4)
    r.talk("the road to Harrowgate", 4, flag="vask.roadSaid")  # TovraHarrowgateRoad
    road = S["HarrowgateRoad"]
    # East out of the camp north of the stall and south of the forge, to the road's posts.
    r.go("east out of the camp", [(52, 207.6), (56.6, 206), S["HarrowgateSpawn"]], tolerance=0.6)
    r.stand_for("the town road", (road[0] - 3, road[1]), road, TOWN_ROAD_RANGE, tolerance=0.4)
    r.scene = "Harrowgate"
    r.add("take the road to Harrowgate", 5, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release the road", 0.5)
    r.at, r.speed = T["Arrival"], WALK
    r.wait("first look at the quay", 2, expectedFlag="vask.roadSaid")
    r.write()


# Harrowgate's walked pieces, along its declared ways (Town.Ways).
QUAY_EAST = [(-26, 7.6), (-12, 8.4), (2, 10.4)]
UP_THE_GATE = [T["GateFoot"], (8, 27), (8, 32)]
TO_FORECOURT = [(8, 35), (0, 35), (0, 52), (0, 56), (0, 66)]
FROM_FORECOURT = [(0, 56), (0, 52), (0, 35)]
# Up the field's middle between the cadets' drill lines (x 4.2 to 14 either side) to its cross walk at z 85, which runs out of its west and east openings.
FIELD_UP = [(0, 66), T["FieldSouthAt"], (0, 85)]
FIELD_TO_GALLERY = [(-FIELD_HALF_X, 85), (-24, 85), (-24, 104)]
# The way round the Warmaster on his post at F (the Ways entry that starts at (0, 85)), from the field's cross walk to the hall steps' line at (0, 90).
ROUND_HIS_POST = [p for p in next(w for w in TOWN_WAYS if w[0] == (0, 85) and len(w) == 4)[1:]]
GALLERY_TO_FIELD = [(-24, 85), (-FIELD_HALF_X, 85), (0, 85), T["FieldSouthAt"], (0, 66)]
TO_KITCHEN = [(-25, 35), T["KitchenDoor"], (-25, 44.4)]
FROM_KITCHEN = [T["KitchenDoor"], (-25, 35)]


def town():
    r = Checked(PREFIX + "50-harrowgate", "Chapter 8: the sisters, Hessa's parley, Pip, the field with Pip, the trophy gallery, Pip's case, Mother Odrun, a save at the armourer's frame", chapter=8)
    r.resume("Harrowgate", "vask.roadSaid", spawn=T["Arrival"])
    r.speed = WALK
    r.lead("Taren")
    # The sisters: a crossing across the quay's whole depth, x -14 to -10.
    r.walk("east along the quay", -26, 0, 7.6, tolerance=0.6)
    r.walk("on until the sisters are heard", -12, 0, 8.4, stop="ui:dialogue")
    r.crossing("the sisters at the harbour gate", 5, "vask.sistersHeard")  # HessaSisters
    r.walk("on along the quay", 2, 0, 10.4, tolerance=0.5)
    hessa = (T["HessaGate"][0] - 1.6, T["HessaGate"][1] - 1.4)
    r.walk("to the gate's foot", T["GateFoot"][0], 0, T["GateFoot"][1], tolerance=0.5)
    r.stand_for("Hessa at the harbour gate", hessa, T["HessaGate"], TALK, tolerance=0.3)
    r.talk("Hessa's parley", 7, flag="vask.parley")  # HessaParley: the gate's leaves open
    r.town_people += [T["HessaGate"]]
    r.go("back to the gate's foot and up through it", [T["GateFoot"], (8, 27)], tolerance=0.5)
    r.walk("onto the gate's landing until Pip speaks", 8, 0, 32, stop="ui:dialogue")
    r.crossing("Pip at the gate", 4, "vask.pipMet")  # PipFirst: starts The Oath; he follows from here
    r.walk("on onto the landing", 8, 0, 32, tolerance=0.5)
    r.go("up to the forecourt", TO_FORECOURT, tolerance=0.5)
    pip_field = (T["PipFieldAt"][0], T["PipFieldAt"][1] - 1.6)
    r.walk("along the forecourt", -8, 0, 66, tolerance=0.5)
    r.stand_for("the field's wall with Pip", pip_field, T["PipFieldAt"], TOWN_RANGES["Pip"], tolerance=0.3)
    r.look("look out over the muster field", 5, "pip.fieldTalked")  # PipField
    # The gallery's talk line begins at z 99, across the walk up from the field's corner: that walk is the one that stops for Sela.
    r.go("across the field to the cadet yard", [(-8, 66), (0, 66)] + FIELD_UP[1:] + FIELD_TO_GALLERY[:-1], tolerance=0.5)
    r.walk("into the trophy gallery until Sela speaks", FIELD_TO_GALLERY[-1][0], 0, FIELD_TO_GALLERY[-1][1], stop="ui:dialogue")
    r.crossing("the trophy gallery", 7, "vask.gallerySeen")  # SelaTrophyGallery: starts What Was Taken
    door_case = (T["DoorCase"][0], T["DoorCase"][1] - 1.8)
    r.walk("along the gallery", door_case[0], 0, 104, tolerance=0.5)
    r.stand_for("the case with the door", door_case, T["DoorCase"], TOWN_RANGES["Pip"], tolerance=0.3)
    r.look("look at the case with the door", 5, "pip.galleryTalked")  # PipGallery: opens the cases
    r.go("back out of the gallery and down to the lower terrace", [(door_case[0], 104), (-24, 104)] + GALLERY_TO_FIELD + FROM_FORECOURT, tolerance=0.5)
    odrun = (T["OdrunAt"][0] - 1.8, T["OdrunAt"][1] - .9)
    r.go("along the lower terrace to Odrun's kitchen", TO_KITCHEN, tolerance=0.5)
    r.stand_for("Mother Odrun", odrun, T["OdrunAt"], TALK, tolerance=0.3)
    r.talk("Mother Odrun", 10, flag="vask.odrunMet")  # OdrunFirst: the town is seen, and the Warmaster comes to his table
    r.town_people += [T["OdrunAt"]]
    frame = (T["MenderAt"][0], T["MenderAt"][1] - .6)
    r.go("out of the kitchen and east to the armourers' street", [(-25, 44.4)] + FROM_KITCHEN + [(16, 35), (16, 42.4)], tolerance=0.5)
    r.stand_for("the armourer's frame", frame, (T["MenderAt"][0], T["MenderAt"][1] + 1.2), 3, tolerance=0.3)
    r.save_at("repair and save at the armourer's frame", "release the frame")
    r.write()


def town_errands():
    r = Checked(PREFIX + "51-harrowgate-errands", "Chapter 8: Odrun's kitchen and back table, the three glass cases, Pip's places, a save at the armourer's frame", chapter=8)
    r.resume("Harrowgate", "vask.odrunMet", spawn=T["ArmourersSpawn"])
    r.speed = WALK
    r.lead("Taren")
    r.town_people = [T["HessaGate"], T["OdrunAt"]]
    r.stands.append((T["ArmourersSpawn"], 4.5))
    # Pip's armourers' street first, then the kitchen: Odrun's sleepless night and her offer.
    forges = (T["ForgesAt"][0], T["ForgesAt"][1] - 1.6)
    r.go("onto the lower terrace walk", [(19, 35), (23, 35)], tolerance=0.5)
    r.stand_for("the forges with Pip", forges, T["ForgesAt"], TOWN_RANGES["Pip"], tolerance=0.3)
    r.look("look along the forges", 3, "pip.forgesSeen")  # PipArmourers
    kettle = (T["KettleAt"][0], T["KettleAt"][1] - 1.6)
    r.go("west along the terrace to the washing", [(23, 35), (8, 35), (-11, 35)], tolerance=0.5)
    washing = (T["WashingAt"][0], T["WashingAt"][1] - 1.8)
    r.stand_for("the washing with Pip", washing, T["WashingAt"], TOWN_RANGES["Pip"], tolerance=0.3)
    r.look("look up at the washing", 4, "pip.washingSeen")  # PipWashing
    r.go("on to the family stair", [(-11, 35), (-25, 35), (-38.5, 35), (-38.5, 46.4)], tolerance=0.5)
    stair = (T["StairGameAt"][0], T["StairGameAt"][1] - 1.8)
    r.stand_for("the stair game with Pip", stair, T["StairGameAt"], TOWN_RANGES["Pip"], tolerance=0.3)
    r.look("watch the stair game", 3, "pip.gameSeen")  # PipStairGame
    odrun = (T["OdrunAt"][0] - 1.8, T["OdrunAt"][1] - .9)
    r.go("back along the terrace to the kitchen", [(-38.5, 35)] + TO_KITCHEN, tolerance=0.5)
    r.stand_for("Mother Odrun", odrun, T["OdrunAt"], TALK, tolerance=0.3)
    r.talk("Odrun's sleepless night", 3, flag="odrun.sleeplessHeard")  # OdrunSleepless
    r.talk("Odrun's kitchen", 4)  # OdrunKitchenOffer: starts Odrun's Kitchen
    # Her three things: the kettle on the armourers' street, the salt at the quartermaster's stair, the flour at the harbour scales.
    r.go("out to the armourers' street", [(-25, 44.4)] + FROM_KITCHEN + [(29.5, 35), (29.5, 42.4)], tolerance=0.5)
    r.stand_for("the regulation kettle", kettle, T["KettleAt"], TOWN_RANGES["thing"], tolerance=0.3)
    r.press_for("take the kettle", "odrun.kettle")
    salt = (T["SaltAt"][0] - 1.2, T["SaltAt"][1] - .8)
    r.go("east to the quartermaster's stair", [(29.5, 35), (42.5, 35), (42.5, 46)], tolerance=0.5)
    r.stand_for("the garrison salt", salt, T["SaltAt"], TOWN_RANGES["thing"], tolerance=0.3)
    r.press_for("take the salt", "odrun.salt")
    # Up the stair to the wall walk and the harbour lookout with Pip; the east gallery's case on the way back.
    r.go("up the quartermaster's stair to the wall walk", [(42.5, 52), (42.5, 66), (43.6, 66), (43.6, 99), (44, 113.6)], tolerance=0.5)
    lookout = (T["Lookout"][0] - 1.4, T["Lookout"][1] - 1.2)
    r.stand_for("the harbour wall with Pip", lookout, T["Lookout"], TOWN_RANGES["Pip"], tolerance=0.3)
    r.look("look down at the harbour", 4, "pip.harbourSeen")  # PipHarbourWall
    case2 = (T["Case2"][0] - .4, T["Case2"][1] - 1.6)  # a gallery post stands 0.8 m before it, a hand east
    r.go("back along the wall walk to the east gallery", [(44, 113.6), (43.6, 99), (38, 104), (case2[0], 104)], tolerance=0.5)
    r.stand_for("the east gallery's case", case2, T["Case2"], TOWN_RANGES["case"], tolerance=0.3)
    r.press_for("take the relay glass from the east gallery's case", "vask.glass2")
    r.walk("back to the gallery's walk", case2[0], None, 104, tolerance=0.5)
    # The oath hall: down the wall walk to the field, across it, up the hall steps to the third case and Pip's wall of names.
    case3 = (T["Case3"][0], T["Case3"][1] - 1.4)
    # Round the Warmaster on his post at F (he stops a hero until the duel; Town.Duel), east of him by the way the builder declares for it.
    r.go("down to the field and up the hall steps", [(38, 104), (43.6, 99), (43.6, 85), (FIELD_HALF_X, 85), (0, 85)] + ROUND_HIS_POST + [(0, 98), (0, 107.8), (case3[0], 108.2)], tolerance=0.5)
    r.stand_for("the oath hall's case", case3, T["Case3"], TOWN_RANGES["case"], tolerance=0.3)
    r.press_for("take the relay glass from the oath hall's case", "vask.glass3")
    names = (T["NamesAt"][0], T["NamesAt"][1] - 1.8)
    r.go("along the portico to the wall of names", [(case3[0], 108.2), (0, 107.6), (names[0], 108.2)], tolerance=0.5)
    r.stand_for("the wall of names with Pip", names, T["NamesAt"], TOWN_RANGES["Pip"], tolerance=0.3)
    r.look("look at the wall of names", 4, "pip.hallSeen")  # PipOathHall
    case1 = (T["Case1"][0], T["Case1"][1] - 1.6)
    r.go("down the steps and across to the trophy gallery", [(names[0], 108.2), (0, 107.6), (0, 98)] + ROUND_HIS_POST[::-1] + [(0, 85)] + FIELD_TO_GALLERY + [(case1[0], 104)], tolerance=0.5)
    r.stand_for("the trophy gallery's case", case1, T["Case1"], TOWN_RANGES["case"], tolerance=0.3)
    r.press_for("take the relay glass from the gallery's case", "vask.glass1")
    r.wait("the glass is all back", 0.5, expectedFlag="quest.RelayGlass.complete")
    # Down to the quay for the flour at the harbour scales, then back to Odrun.
    flour = (T["FlourAt"][0] - .8, T["FlourAt"][1] - 1.4)
    r.go("down through the town to the quay", [(case1[0], 104), (-24, 104)] + GALLERY_TO_FIELD + FROM_FORECOURT + [(8, 35), (8, 32), (8, 27), T["GateFoot"], (2, 10.4), (20, 9.2), (31, 9.2)], tolerance=0.5)
    r.stand_for("the ash-root flour", flour, T["FlourAt"], TOWN_RANGES["thing"], tolerance=0.3)
    r.press_for("take the flour", "odrun.flour")
    r.go("back up through the gate to the kitchen", [(31, 9.2), (20, 9.2), (2, 10.4), T["GateFoot"], (8, 27), (8, 32), (8, 35)] + TO_KITCHEN, tolerance=0.5)
    r.stand_for("Mother Odrun again", odrun, T["OdrunAt"], TALK, tolerance=0.3)
    r.shop("the kitchen stocked", 2)  # OdrunKitchenReturn: the back table opens; gel to the cap before the field
    frame = (T["MenderAt"][0], T["MenderAt"][1] - .6)
    r.go("back to the armourer's frame", [(-25, 44.4)] + FROM_KITCHEN + [(16, 35), (16, 42.4)], tolerance=0.5)
    r.stand_for("the armourer's frame", frame, (T["MenderAt"][0], T["MenderAt"][1] + 1.2), 3, tolerance=0.3)
    r.save_at("repair and save at the armourer's frame", "release the frame")
    r.write()


def warmaster():
    r = Checked(PREFIX + "52-the-warmaster", "Chapter 8: Drask's table, the duel, his yield, the pursuit, the verdict, Pip, the armoury gate, the cells, every radio, Meret, Abri turns for Nacre", chapter=8)
    r.resume("Harrowgate", "odrun.kitchenStocked", spawn=T["ArmourersSpawn"])
    r.speed = WALK
    r.lead("Taren")
    r.town_people = [T["HessaGate"], T["OdrunAt"]]
    # The muster field inside its kerb is open floor (the duel's: nothing stands on it), and the forecourt before its south opening, where its checkpoint is.
    r.stands += [(T["ArmourersSpawn"], 4.5), ((0, 84), 13), ((0, 66), 6)]
    table = (T["DraskAt"][0], T["DraskAt"][1] - 1.6)
    r.go("up to the oath hall", [(19, 35), (8, 35)] + TO_FORECOURT[1:] + FIELD_UP[1:] + [(0, 98), table], tolerance=0.4)
    reach(f"{r.name} 'the Warmaster at his table'", table, 0.4, T["DraskAt"], TALK)
    r.talk("the Warmaster receives them", 15, flag="vask.draskMet")  # DraskReceives: he goes out to the field
    # Down the hall steps onto the field: his word as the party steps on (a crossing over the whole floor; saves at Field).
    r.walk("down the hall steps until he speaks", 0, None, 94, stop="ui:dialogue")
    r.crossing("the Warmaster on the muster field", 3, "vask.fieldTaken")  # DraskOnTheField
    r.speed = COMBAT_SPEED
    # His door: from the checkpoint (Field, (0, 64)) in at the south opening, or from where his word ends, to well inside the field (z 72.5 to 97.5) off his line at F.
    boss(r, "DraskVaal", "out onto the muster field", -5, TOWN_LEVELS["Upper"], 90, spawn=T["FieldSpawn"], rounds=10)
    r.add("reach the fallen hero after the duel", 16, navigate=True, approachPartner=True, tolerance=1.2, stopWhen="partyAlive", point=dict(x=0, y=0, z=0))
    r.add("both heroes ready after the duel", 6, until="partyAlive")
    r.crossing("the Warmaster yields", 4, "vask.duelWon", wait=12)  # DraskLeavesField: his own person takes over where he yielded
    r.lead("Taren")
    r.fought()
    pad = T["LaunchPad"]
    stand = (pad[0] + 2.6, pad[1] + 1)
    r.walk("off the field by its south opening", 0, None, 74)
    r.go("onto the forecourt and west of the ramp's parapet", [(0, 66), (-8, 66)], tolerance=0.5)
    r.stand_for("the forecourt's pad", stand, pad, PAD_RANGE, tolerance=0.4)
    r.scene = "HarrowgateHarbour"
    r.add("lift off after him", 5, buttons=["South"], expectedUi="world", expectedForm="Flight")
    r.add("release lift-off", 0.5)
    r.at, r.speed = HB["Launch"], FLIGHT_SPEED
    # His door over leg 0's water: from the launch (also its checkpoint) north into the fight's box (60 by 60 on (0, 30)).
    r.door("DraskSkiff", "out over the harbour after the skiff", 0, 1, 14, spawn=HB["Launch"], tolerance=3)
    mh.flight_boss(r, "DraskSkiff", rounds=12)
    r.wait("the skiff goes down toward the field", 5)
    r.fought()
    home = HB["HomeWay"]
    r.walk("back toward the way to the field", home[0] + 2, 1, home[1] + 14, tolerance=3)
    r.brake = True
    stand = (home[0], home[1] + 4.4)
    reach(f"{r.name} 'Land — Harrowgate'", stand, 1, home, DOCK_RANGE)
    r.walk("settle over the way back to the field", stand[0], 1, stand[1], tolerance=1)
    r.brake = False
    r.scene = "Harrowgate"
    # The town's answer (an AfterTalk) opens 1.5 s after the party is down: the landing ends as it arrives, and the talk is waited for after it.
    r.add("land on the muster field", 5, buttons=["South"], until="scene:Harrowgate", expectedUi="world", expectedForm="Natural")
    r.add("release landing", 0.5)
    r.at, r.speed = T["VerdictSpawn"], WALK
    r.crossing("Harrowgate answers", 7, "vask.deposed", wait=10)  # OdrunVerdict (an AfterTalk, 1.5 s after arriving)
    # Pip at Odrun's door, after the verdict: his oath.
    pip = (T["PipAfterAt"][0] + .2, T["PipAfterAt"][1] - 2.2)
    r.go("off the field and down to the lower terrace", [T["FieldSouthAt"], (0, 66)] + FROM_FORECOURT + [(-21.8, 35)], tolerance=0.5)
    r.stand_for("Pip at Odrun's door", pip, T["PipAfterAt"], TALK, tolerance=0.3)
    r.talk("Pip after the duel", 6, flag="pip.oathLeft")  # PipAfterDuel
    # The end, without a break: the armoury gate, the stair down, the cell, and every radio in the harbour.
    r.go("east along the terrace and up the quartermaster's stair", [(-21.8, 35), (42.5, 35), (42.5, 52), (42.5, 66), (43.6, 66)], tolerance=0.5)
    r.town_people += [T["HessaArmoury"]]
    r.walk("along the wall walk to the armoury gate", 43.6, None, 80.25, tolerance=0.5)
    r.walk("in at the armoury gate until Hessa speaks", 48.4, None, 80.25, stop="ui:dialogue")
    r.crossing("Hessa at the armoury gate", 4, "vask.gateDenied")  # HessaArmouryGate
    r.walk("through the armoury gate", 48.4, None, 80.25, tolerance=0.5)
    r.walk("to the stair's head", 50.8, None, 81.4, tolerance=0.5)
    r.walk("down the stair until Idra speaks", 50.8, None, 92, stop="ui:dialogue")
    r.crossing("Idra on the stair down", 4, "idra.lockCounted")  # IdraLockCounted
    cell = (T["CellAt"][0], T["CellAt"][1] - 2.6)
    reach(f"{r.name} 'Drask in his cell'", cell, 0.3, T["CellAt"], CELL_RANGE)
    r.go("down to the cells' landing", [T["StairFootAt"], cell], tolerance=0.3)
    r.talk("the Warmaster in his cell", 5, flag="vask.orderSaid")  # DraskCustody
    r.crossing("every radio in the harbour", 7, "vask.nacreHeard", wait=8)  # BrynNacreTraffic
    r.crossing("all stations", 5, "chapter8.complete", wait=8)  # MeretAllStations: the chapter's card
    # Chapter 9 begins by itself: Abri turns the convoy for Nacre ten seconds after the card (HomeBuilder.Approach), and her talk saves.
    r.crossing("Abri turns the convoy for Nacre", 4, "home.turnedForNacre", wait=30)  # AbriTurnsForNacre
    r.wait("chapter eight closes and nine begins", 3, expectedFlag="home.turnedForNacre")
    r.write()


if __name__ == "__main__":
    check_docs()
    way_in()
    picket()
    flows()
    flows_errands()
    foundry()
    colossus()
    town()
    town_errands()
    warmaster()
    print("HARROWGATE_ROUTES_OK")
