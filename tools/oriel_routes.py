"""Writes chapter 7's replay routes (chapter-38 to chapter-43) for the Orrery, the Skyfields and the Glass Choir from the builders' own places.

Places are read out of the builders (OrielBuilder.Orrery.cs: the machine's numbers, its rails, gates and ways; OrielBuilder.Skyfields.cs: the islands, lifts, junctions,
named places and declared Ways; OrielBuilder.GlassChoir.cs: the rooms and their floors, the stays, the hall and its Ways; OrielBuilder.StageManyHands: Ishen's crossing on
Many Hands' dock and the hull's warp to the Orrery) and checked against the coordinates tables of their map docs (docs/maps/Orrery.md, OrielSkyfields.md, GlassChoir.md)
before anything is written: a place a route uses that has drifted from its doc stops the run, unless KNOWN_DRIFT lists the doc's exact wrong number (each is a finding,
printed on every run). Nobody has run these. Every straight leg is checked here before it is written: in the Orrery inside the lane's walls and through a ring's opening by
Idra's hull, never across a gate row's truss; in the Skyfields along the declared Ways (or onto the floor before a prompt they pass at a distance), off the people and what
the built scene stands solid; in the Glass Choir on its rooms' floors and off what the built scene stands solid; aboard Many Hands and round its hull by manyhands_routes'
checks. OrielRouteTests.EveryWalkOnTheChapterSevenRoutesFollowsFloorABodyCanCross then checks every leg on the built zones' navigation and colliders.

The Orrery's rule is the machine's time (docs/maps/Orrery.md): each rail is crossed by waiting short of its middle gate, braked, until the driver reads the gate as open in
its moon's lee ("until": "lee:<rail>", ContinuousReview.Oriel.cs), then going through. Nothing counts seconds. The Last Warden is fought with the driver's own clamp hand
(the same file): at a held line it walks to a lit seat, beside its fibre, takes the clamp's weight with the pad and keeps still until the staff is down, and it stops the
Clamp Tenders first. The routes only fight it.

The chain: chapter-37-long-sleep ends saved aboard Many Hands at the dock (spawn Arrival, chapter6.complete), Taren and Idra out, Sela resting. Every route here is
played with that pair.
  chapter-38-orrery          Many Hands: Ishen arrives on the dock (starts The Cage Makers), the launch, the hull's warp to the Orrery; Lume's hail, Ishen's lesson, the
                             nine rails each in its moon's lee, the far gate, the landing at Ishen's camp
  chapter-39-skyfields       the camp (Ishen at the landing, Lume's stall: gel to the cap, and her lantern), the first lift, the terrace (its Glassback, the walled garden,
                             the junction), the fibre fields (the wisps), the second lift, the sagging gardens (the follower, the mixed fight), the stair gardens, the
                             forecourt (the Warden's pruners), the doorstep, the Choir's door. Every junction is seated
  chapter-40-glass-choir     the nave in silence and Sela's line, the first clamp's release, the three stays and their fights, to the heart cradle
  chapter-41-last-warden     the Warden's office, the Last Warden (boss, its door retried), it stands down, the court's record read (oriel.coronachHeard)
  chapter-42-oriel-errands   side content: up out of the Choir, the gardener's three gardens and the sealed garden (The Empty Gardens), the lantern set on the highest
                             island, back into the Choir and the chart to the camp, Lume's lantern answered (The Far End), Ishen's moons, the Orrery's lap out to the
                             Steward and in by the Weigher and the Lamplighter, slowly and on green (The Names of the Moons), a save at the camp cradle
  chapter-43-the-far-end     Meret's call coming into the camp, Lume reads Brekk's word (chapter7.complete), a save at the camp cradle

Where chapter 8 begins: chapter-43 ends saved at Ishen's camp in the Skyfields, by the camp cradle (spawn Camp, (-20, 12)), Taren and Idra out, chapter7.complete.

Lume's lantern is offered in chapter-39: her second word at the stall, the only time the party stands there before the climb (once the record is read, the camp's way up
is through Meret's call, after which Lume's next word is the chapter's end). The rest of that side story is chapter-42's.

  python tools/oriel_routes.py
"""
import math
import re
from pathlib import Path

import manyhands_routes as mh
from lantern_routes import COMBAT_SPEED, FLIGHT_SPEED, encounters
from manyhands_routes import BODY, NUMBER, arrays, block, points, vectors
from xylos_routes import box_distance, segment_distance

ROOT = Path(__file__).resolve().parent.parent
WORLD = ROOT / "Lattice" / "Assets" / "_Project" / "Scripts" / "Editor" / "World"
MAPS = ROOT / "docs" / "maps"
PREFIX = "chapter-"
HULL_IDRA = 2.17  # HeroCollision.HullRadius("Idra"): the larger hull of the pair these routes are played with
PAIR = ("Taren", "Idra")

ORRERY_SOURCE = (WORLD / "OrielBuilder.Orrery.cs").read_text(encoding="utf-8")
SKY_SOURCE = (WORLD / "OrielBuilder.Skyfields.cs").read_text(encoding="utf-8")
CHOIR_SOURCE = (WORLD / "OrielBuilder.GlassChoir.cs").read_text(encoding="utf-8")
ORIEL_SOURCE = (WORLD / "OrielBuilder.cs").read_text(encoding="utf-8")
HOARFELL_SOURCE = (WORLD / "HoarfellBuilder.cs").read_text(encoding="utf-8")


def const(source, name):
    return float(re.search(r"\b" + name + "=" + NUMBER, source).group(1))


# ------------------------------------------------------------------ the Orrery, as OrielBuilder.Orrery lays it
CENTRE = tuple(float(v) for v in re.search(r"Vector2 Centre=new\(" + NUMBER + "," + NUMBER + r"\)", ORRERY_SOURCE).groups())
HALF, RING_DROP, TILT = const(ORRERY_SOURCE, "Half"), const(ORRERY_SOURCE, "RingDrop"), const(ORRERY_SOURCE, "Tilt")
TALK_Z, FAR_GATE_Z, FROM_CAMP_Z, LAND_Z = (const(ORRERY_SOURCE, k) for k in ("TalkZ", "FarGateZ", "FromCampZ", "LandZ"))
LANE_GATES = [float(v) for v in re.findall(NUMBER, block(ORRERY_SOURCE, "LaneGates="))]
# The ring's opening at the plane, half of it, as the build measured it off the ring's mesh (ORRERY_RING opening-half=5.25; the map doc's "10.5 m").
RING_OPENING = 5.25
# Each gate row's solid (Truss: ZoneKit.BlockAlong, 1.4 m thick) across the plane.
ROW_SOLID = 0.7


class Rail:
    """One rail of the machine (OrielBuilder.Orrery.Rail): where it crosses the lane, its moon's speed, radius and direction, and the gate row its leaning rings make."""

    def __init__(self, name, z, speed, moon, direction):
        self.name, self.z, self.speed, self.moon, self.direction = name, z, speed, moon, direction
        s, c = math.sin(math.radians(TILT)), math.cos(math.radians(TILT))
        self.radius = CENTRE[1] - z
        ring_out = (moon + 1.3 + RING_DROP * s) / c
        self.gate_out = ring_out - RING_DROP * math.tan(math.radians(TILT))
        self.row = self.radius + self.gate_out
        self.gate_z = round(z - self.gate_out, 2)  # the middle ring, where the lane's middle crosses the row


RAILS = [Rail(m.group(1), float(m.group(2)), float(m.group(3)), float(m.group(4)), int(m.group(5)))
         for m in re.finditer(r'new\("([^"]+)",' + NUMBER + "," + NUMBER + "," + NUMBER + r",(-?1),new\(", block(ORRERY_SOURCE, "internal static readonly Rail[] Rails="))]
NAMED_RAILS = [i + 1 for i, m in enumerate(re.finditer(r'new\("([^"]+)",[^\n]*', block(ORRERY_SOURCE, "internal static readonly Rail[] Rails="))) if '"oriel.' in m.group(0)]
WARP_HOME, LANDING = (-14.0, -4.0), (0.0, LAND_Z)


def lane_along(p, radius):
    return radius * math.atan2(p[0] - CENTRE[0], CENTRE[1] - p[1])


# ------------------------------------------------------------------ Many Hands, as OrielBuilder.StageManyHands stages chapter 7 in it
_warp = re.search(r"var warpAt=new Vector3\(" + NUMBER + r",ZoneKit\.FlightPlane," + NUMBER + r"\)", ORIEL_SOURCE)
WARP_TO_ORRERY = (float(_warp.group(1)), float(_warp.group(2)))
_warp = re.search(r"var warpAt=new Vector3\(" + NUMBER + r",ZoneKit\.FlightPlane," + NUMBER + r"\)", HOARFELL_SOURCE)
WARP_TO_SHEAR = (float(_warp.group(1)), float(_warp.group(2)))
ISHEN_ARRIVES = tuple(float(v) for v in re.search(r'arrives\.transform\.position=Floor\(new Vector2\(' + NUMBER + "," + NUMBER + r"\)\)", ORIEL_SOURCE).groups())
ISHEN_BOX = tuple(float(v) for v in re.search(r"mouth\.size=new Vector3\(" + NUMBER + "," + NUMBER + "," + NUMBER + r"\)", ORIEL_SOURCE).groups())
# The warps that stand at the plane off the hull's stern: WorldBuilder.Warp's 2 m piece each.
HULL_EXTRA = [("the warp to the Shear", WARP_TO_SHEAR, 0, 1, 1), ("the warp to the Orrery", WARP_TO_ORRERY, 0, 1, 1)]


# ------------------------------------------------------------------ the Skyfields, as OrielBuilder.Skyfields lays them
SK = vectors(SKY_SOURCE[SKY_SOURCE.index("internal static readonly Vector2 Arrival=new("):SKY_SOURCE.index("/// <summary>The frayed garden junctions")])
LEVELS = {k: const(SKY_SOURCE, k) for k in ("Camp", "Terrace", "Upper", "High")}
ISLES = [(n, x0, x1, z0, z1, LEVELS[level]) for n, x0, x1, z0, z1, level in
         [(m.group(1), float(m.group(2)), float(m.group(3)), float(m.group(4)), float(m.group(5)), m.group(6)) for m in
          re.finditer(r'\("([^"]+)",' + ",".join([NUMBER] * 4) + r",(Camp|Terrace|Upper|High)\)", block(SKY_SOURCE, "float level)[] Isles="))]]
CORNER, RIM_WIDE, DOCK_HALF = const(SKY_SOURCE, "Corner"), const(SKY_SOURCE, "RimWide"), const(SKY_SOURCE, "DockHalf")
LIFTS = []
for _m in re.finditer(r'\("([^"]+)",new\(' + NUMBER + "," + NUMBER + r"\),new\(" + NUMBER + "," + NUMBER + r"\),new\(" + NUMBER + "," + NUMBER + r"\),new\(" + NUMBER + "," + NUMBER
                      + r"\),(Camp|Terrace|Upper|High),(Camp|Terrace|Upper|High)\)", block(SKY_SOURCE, "float from,float to)[] Lifts=")):
    _g = _m.groups()
    LIFTS.append((_g[0], (float(_g[1]), float(_g[2])), (float(_g[3]), float(_g[4])), (float(_g[5]), float(_g[6])), (float(_g[7]), float(_g[8])), LEVELS[_g[9]], LEVELS[_g[10]]))
JUNCTIONS = [(m.group(1), float(m.group(2)), float(m.group(3)), float(m.group(4)), LEVELS[m.group(5)])
             for m in re.finditer(r'\("([^"]+)",' + ",".join([NUMBER] * 3) + r",(Camp|Terrace|Upper|High),Load\.\w+\)", block(SKY_SOURCE, "Load load)[] Junctions="))]
GARDENS = [((float(a), float(b)), flag) for a, b, _, _, flag in re.findall(r"\(new\(" + NUMBER + "," + NUMBER + r"\),new\(" + NUMBER + "," + NUMBER + r'\),"([\w.]+)"\)',
                                                                               block(SKY_SOURCE, "string flag)[] GardenJunctions="))]
SKY_WAYS = [points(w, SK) for w in re.findall(r"new Vector2\[\]\{(.*?)\}", block(SKY_SOURCE, "internal static readonly Vector2[][] Ways="))]
# The junctions' hands (Junction: seat at the collar, cut along the fibre's other side), and the gardener's own prompt beside where it waits.
SEAT_HANDS = [(x - DOCK_HALF - 1.1, rim_a - 2.2) for _, x, rim_a, _, _ in JUNCTIONS]
CUT_HANDS = [(x + DOCK_HALF + .9, rim_a - 2.2) for _, x, rim_a, _, _ in JUNCTIONS]
ASKS = (SK["GardenerWait"][0] + 1.2, SK["GardenerWait"][1] - 1.6)
# The Skyfields' crossings as the builder lays them (kit.Crossing): name, centre (x, z), box (x by z). Checked against the source below.
SKY_CROSSINGS = {"IshenLanding": ((-15, 13), (3, 18)), "IdraEmptyGarden": ((-6, 43), (3, 16)), "IshenJunction": ((-22, 43), (3, 16)), "IdraGardenerFollows": ((22, 101), (3, 16)),
                 "IdraChoirDoor": ((6, 164), (48, 3)), "MeretOrielCall": ((19, 13), (4, 18))}
LIFT_RANGE, PROMPT_REACH, TALK_REACH = 2.4, 2.6, 3.2
# Ishen's camp is the Skyfields' safe pocket (TheCamp's "Ishen's camp (safe)", 80 by 20 on (-8, 13): the pad, the landing's spawn and both their walks are in it), and there the
# heroes stand in their own form, though the zone is one of fights. Route 38's landing expected Shaped and saw Natural (its first two runs in a player, November 14).
_pocket = re.search(r"\"Ishen's camp \(safe\)\".*?position=new Vector3\(" + NUMBER + "," + NUMBER + "," + NUMBER + r"\).*?size=new Vector3\(" + NUMBER + "," + NUMBER + "," + NUMBER + r"\)", SKY_SOURCE, re.S)
CAMP_POCKET = (float(_pocket.group(1)), float(_pocket.group(3)), float(_pocket.group(4)), float(_pocket.group(6)))  # centre x, z and size x, z
CAMP_FORM = "Natural"


def round_box(p, x0, x1, z0, z1, r):
    dx, dz = abs(p[0] - (x0 + x1) / 2) - (x1 - x0) / 2 + r, abs(p[1] - (z0 + z1) / 2) - (z1 - z0) / 2 + r
    return min(max(dx, dz), 0) + math.hypot(max(dx, 0), max(dz, 0)) - r


def sky_y(p):
    """The floor's height at a plan point of the Skyfields: a lift's road between its rims (and its nodes), a junction's deck across its gap, else the nearest island's level."""
    for _, a, rim_a, rim_b, b, lo, hi in LIFTS:
        length = math.dist(a, b)
        t = max(0.0, min(1.0, ((p[0] - a[0]) * (b[0] - a[0]) + (p[1] - a[1]) * (b[1] - a[1])) / length ** 2))
        if segment_distance(p, a, b) < 2.2 and math.dist(a, rim_a) / length - .05 < t < math.dist(a, rim_b) / length + .05:
            u = (t * length - math.dist(a, rim_a)) / math.dist(rim_a, rim_b)
            return round(lo + (hi - lo) * max(0.0, min(1.0, u)), 2)
    for _, x, rim_a, rim_b, level in JUNCTIONS:
        if abs(p[0] - x) < 3.2 and rim_a - .5 < p[1] < rim_b + .5:
            return level
    best = min(ISLES, key=lambda i: round_box(p, i[1], i[2], i[3], i[4], CORNER))
    return best[5]


# ------------------------------------------------------------------ the Glass Choir, as OrielBuilder.Choir lays it
CH = vectors(CHOIR_SOURCE[CHOIR_SOURCE.index("internal static readonly Vector2 Arrival=new("):CHOIR_SOURCE.index("/// <summary>The four stays:")])
CH["C"] = tuple(float(v) for v in re.search(r"Vector2 C=new\(" + NUMBER + "," + NUMBER + r"\)", CHOIR_SOURCE).groups())
CH_LEVELS = {k: const(CHOIR_SOURCE, k) for k in ("Door", "NaveLow", "ClampFloor", "RibFloor", "GalleryFloor", "HallFloor", "EastEdge", "HallRadius", "ClampSeat")}
STAYS = [((float(a), float(b)), flag) for a, b, flag in re.findall(r"new\(" + NUMBER + "," + NUMBER + r'\),"(oriel\.stay\d)"\)', block(CHOIR_SOURCE, "string flag)[] Stays="))]
CLAMP_BEARINGS = [float(v) for v in re.findall(NUMBER, block(CHOIR_SOURCE, "ClampBearings="))]
CHOIR_WAYS = [points(w, {"C": CH["C"]}) for w in re.findall(r"new Vector2\[\]\{(.*?)\}", block(CHOIR_SOURCE, "internal static readonly Vector2[][] Ways="))]
_nearest = block(CHOIR_SOURCE, "internal static float Nearest(Vector2 p,out Room room)")
ROOMS = []
for _m in re.finditer(r"Try\(RoundBox\(p," + r",".join([r"([-\w.]+)"] * 4) + r"(?:," + NUMBER + r")?\),Room\.(\w+)\)", _nearest):
    if _m.group(6) == "Overlook":
        continue  # its own bounds below
    # A bound is a literal or the balustrade's line less a little ("EastEdge-.6f").
    _vals = [CH_LEVELS["EastEdge"] + (float(v[len("EastEdge"):].rstrip("f")) if len(v) > len("EastEdge") else 0) if v.startswith("EastEdge") else float(v.rstrip("f")) for v in _m.groups()[:4]]
    ROOMS.append((_m.group(6), *_vals, float(_m.group(5)) if _m.group(5) else .5))
OVERLOOK = (const(CHOIR_SOURCE, "OverlookEdge"), const(CHOIR_SOURCE, "OverlookFrom"), const(CHOIR_SOURCE, "OverlookTo"))
if len(ROOMS) != 19:
    raise SystemExit(f"OrielBuilder.Choir.Nearest: {len(ROOMS)} rooms read, not the nineteen boxes this models\nORIEL_ROUTES_FAILED")


def choir_nearest(p):
    """Metres outside the nearest floor of the Choir (negative on one) and which room (Choir.Nearest): its boxes, the hall's round, the overlook."""
    best, room = math.dist(p, CH["C"]) - CH_LEVELS["HallRadius"], "Hall"
    for name, x0, x1, z0, z1, r in ROOMS:
        clear = round_box(p, x0, x1, z0, z1, r)
        if clear < best:
            best, room = clear, name
    clear = round_box(p, CH_LEVELS["EastEdge"] - .8, OVERLOOK[0], OVERLOOK[1], OVERLOOK[2], 1.4)
    if clear < best:
        best, room = clear, "Overlook"
    return best, room


def choir_y(p):
    """The floor's level by its room (Choir.Level): the narthex and the upper nave at the door's 0, the flights between the levels, the engine hall at -10."""
    L = CH_LEVELS
    ramp = lambda z, z0, z1, a, b: a + (b - a) * max(0.0, min(1.0, (z - z0) / (z1 - z0)))
    _, room = choir_nearest(p)
    z = p[1]
    level = {"Narthex": L["Door"], "Porch": L["Door"], "NaveA": L["Door"], "Passage": L["Door"], "Chapel": L["Door"], "FlightA": ramp(z, 58.6, 61.4, L["Door"], L["NaveLow"]),
             "NaveB": L["NaveLow"], "FlightB": ramp(z, 114.6, 117.4, L["NaveLow"], L["ClampFloor"]), "Overlook": L["NaveLow"], "ClampRoom": L["ClampFloor"],
             "StairA": ramp(z, 150.6, 155.4, L["ClampFloor"], L["RibFloor"]), "RibWalk": L["RibFloor"], "Bay": L["RibFloor"], "Breach": L["RibFloor"],
             "StairB": ramp(z, 206.4, 213.4, L["RibFloor"], L["GalleryFloor"]), "Gallery": L["GalleryFloor"], "Niche": L["GalleryFloor"], "Landing": L["GalleryFloor"],
             "RibPass": L["GalleryFloor"], "StairC": ramp(z, 264.6, 274.6, L["GalleryFloor"], L["HallFloor"]), "Hall": L["HallFloor"]}.get(room, L["Door"])
    return round(level, 2)


def polar(bearing, r):
    c = CH["C"]
    return (c[0] + math.sin(math.radians(bearing)) * r, c[1] + math.cos(math.radians(bearing)) * r)


CLAMP_SEATS = [polar(b, CH_LEVELS["ClampSeat"]) for b in CLAMP_BEARINGS]
# The engine hall's seal across the stair's foot (EngineHall: WorldBuilder.Membrane("Engine hall seal", kit.On(0, 275.2f, ...))). The Warden's fight is gated by it:
# EncounterVolume.TryEnter begins it only once every hero out stands a body's radius and half a metre past its line, and Idra follows three metres behind the lead
# (PartnerBrain: the lead less three of its facing). A door walk that stopped the lead 4.7 m past the seal left her on it, and the Warden stood at its engine, its fight
# unbegun, through all twelve rounds (route 41's first run in a player, November 14). So the door walk ends where the partner behind is surely through.
SEAL_Z = float(re.search(r'Membrane\("Engine hall seal",kit\.On\(0,' + NUMBER, CHOIR_SOURCE).group(1))
PARTNER_BEHIND, GATE_ALLOWANCE, PARTNER_SLACK = 3.0, 1.1, 2.0  # PartnerBrain's follow; a body's radius (.6 at most) and TryEnter's half metre; where she stops short of her place
WARDEN_DOOR_TOLERANCE = 1.0
WARDEN_DOOR_Z = SEAL_Z + PARTNER_BEHIND + GATE_ALLOWANCE + PARTNER_SLACK + WARDEN_DOOR_TOLERANCE + 1.7  # 284: the lead stops a metre short of it at the most
STAY_PIVOTS = [((float(a), float(b)), (float(c), float(d)), float(e)) for a, b, c, d, e in re.findall(r"\(new\(" + NUMBER + "," + NUMBER + r"\),new\(" + NUMBER + "," + NUMBER + r"\)," + NUMBER,
                                                                                                block(CHOIR_SOURCE, "string flag)[] Stays="))]


def stay_stand(index, away=.9):
    """Where to stand to take a stay's weight: on its hand's floor, on the far side from its ratchet post (Stay: the post stands 1.2 m from the seat toward the rib's end,
    solid, .9 m square), so the hand is in reach and the post is not walked into."""
    (pivot, lies, length), seat = STAY_PIVOTS[index], STAYS[index][0]
    end = (pivot[0] + lies[0] * length * .9, pivot[1] + lies[1] * length * .9)
    d = math.dist(end, seat)
    return (round(seat[0] - (end[0] - seat[0]) / d * away, 2), round(seat[1] - (end[1] - seat[1]) / d * away, 2))


# ------------------------------------------------------------------ what stands solid in the built scenes (manyhands_routes.scene_solids)
# The Skyfields: less the junctions' rails across the rims (gone once a junction is worked), the sealed garden's door (open on its flag) and the walkers (who move).
BUILT_SKY = [(l, c, yaw, hx, hz) for l, c, yaw, hx, hz, y0, y1 in mh.scene_solids("OrielSkyfields", tuple(j[0] + " shut" for j in JUNCTIONS) + ("Sealed garden door",))
             if y0 < sky_y(c) + 1.8 and y1 > sky_y(c) + .3 and hx < 30 and hz < 30]
# The Choir: less what the stays hold (each rib's membrane, gone on its flag), the sentinels' casings (cracked by stay 2) and the hall's seal (shut only in the fight).
BUILT_CHOIR = [(l, c, yaw, hx, hz) for l, c, yaw, hx, hz, y0, y1 in mh.scene_solids("GlassChoir", ("Rib 1 held", "Rib 2 held", "Rib 3 held", "Rib 4 held", "Sentinel casing", "Engine hall seal"))
               if y0 < choir_y(c) + 1.8 and y1 > choir_y(c) + .3 and hx < 30 and hz < 30]
# The people who stand at their places: an Npc's body is a hole in the bake whether or not the leg meets them.
SKY_NAMED = {"Lume": SK["LumeAt"], "IshenPost": SK["IshenPost"]}


# ------------------------------------------------------------------ the map docs
# Places a route here uses whose doc gives another number than its builder: the doc's own (wrong) value, so any further drift still stops the run.
KNOWN_DRIFT = {("GlassChoir.md", "Way out"): (0.0, -4.0), ("GlassChoir.md", "Narthex cradle"): (-6.0, 6.0)}


def doc_rows(name, heading, column):
    """Each row of one table in a map doc (the one under `heading`): its cells, and the number pairs of its `column`th cell (x, z, from (x, z), (x, y, z) or a bare 'x, z')."""
    text = (MAPS / name).read_text(encoding="utf-8")
    section = text[text.index(heading):]
    rest = section[len(heading):]
    nxt = re.search(r"\n#{2,3} ", rest)
    section = section[:len(heading) + nxt.start()] if nxt else section
    rows = []
    for line in section.splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) <= column or set(cells[0]) <= set("-: "):
            continue
        cell = cells[column]
        triples = re.findall(r"\((-?[\d.]+), (-?[\d.]+), (-?[\d.]+)\)", cell)
        pairs = [(float(a), float(c)) for a, b, c in triples] if triples else [(float(a), float(b)) for a, b in re.findall(r"(-?\d+(?:\.\d+)?), (-?\d+(?:\.\d+)?)", cell)]
        rows.append((cells, pairs))
    return rows


def check_docs():
    wrong, known = [], []

    def expect(doc, heading, column, label, places, contains=None, cell=1):
        rows = [r for r in doc_rows(doc, heading, column) if label in r[0][cell] and (contains is None or contains in " ".join(r[0]))]
        if not rows:
            wrong.append(f"{doc}: no row '{label}' under '{heading}'")
            return
        given = rows[0][1]
        for i, place in enumerate(places):
            if i < len(given) and math.dist(given[i], place) <= 0.25:
                continue
            if KNOWN_DRIFT.get((doc, label)) == (given[i] if i < len(given) else None):
                known.append(f"{doc} '{label}': the builder has {place}, the doc {given[i]}")
                continue
            wrong.append(f"{doc} '{label}': the builder has {place}, the doc {given[i] if i < len(given) else 'nothing'}")

    orrery = "## Coordinates for routes"
    expect("Orrery.md", orrery, 1, "Arrival", [(0, 6)], cell=0)
    expect("Orrery.md", orrery, 1, "Warp home", [WARP_HOME], cell=0)
    expect("Orrery.md", orrery, 1, "Ishen's talk", [(0, TALK_Z)], cell=0)
    expect("Orrery.md", orrery, 1, "The far gate", [(0, FAR_GATE_Z)], cell=0)
    expect("Orrery.md", orrery, 1, "Back from the camp", [(0, FROM_CAMP_Z)], cell=0)
    expect("Orrery.md", orrery, 1, "The landing", [LANDING], cell=0)
    for n, rail in enumerate(RAILS, 1):
        rows = [r for r in doc_rows("Orrery.md", orrery, 1) if r[0][0].startswith(f"Rail {n},")]
        if not rows or len(rows[0][1]) < 2 or abs(rows[0][1][1][1] - rail.gate_z) > .25 or abs(rows[0][1][1][0]) > .25:
            wrong.append(f"Orrery.md 'Rail {n}': the builder's middle gate is at (0, {rail.gate_z}), the doc {rows[0][1][1] if rows and len(rows[0][1]) > 1 else 'nothing'}")
        elif rail.name not in rows[0][0][0]:
            wrong.append(f"Orrery.md 'Rail {n}' isn't {rail.name}")
    way_in = "**The way in, staged in chapter 5's scenes**"
    expect("Orrery.md", way_in, 1, "Ishen arrives", [ISHEN_ARRIVES], cell=0)
    expect("Orrery.md", way_in, 1, "The warp to the Orrery", [WARP_TO_ORRERY], cell=0)
    sky = "### Coordinates as built"
    for label, place in (("`Arrival`", SK["Arrival"]), ("`Camp`", SK["CampSpawn"]), ("`ChoirDoor`", SK["ChoirSpawn"]), ("`Launch — The Orrery`", SK["Pad"]),
                         ("`Choir door — The Glass Choir`", (SK["DoorAt"][0], SK["DoorAt"][1] - .8)), ("`Camp cradle — repair and save`", SK["Cradle"]),
                         ("`Ishen`", SK["IshenPost"]), ("`Lume`", SK["LumeAt"]), ("`Look at the gardener`", ASKS), ("`Go in after the gardener`", SK["SealedHand"]),
                         ("`Set the lantern in the seat`", SK["SeatHand"]), ("`The long fibre's node`", SK["LongFrom"])):
        expect("OrielSkyfields.md", sky, 2, label, [place])
    expect("OrielSkyfields.md", sky, 2, "`Seat the junction` (frayed gardens 1 to 3)", [g for g, _ in GARDENS])
    for (name, x, rim_a, rim_b, _), seat in zip(JUNCTIONS, SEAT_HANDS):
        rows = [r for r in doc_rows("OrielSkyfields.md", sky, 3) if f"`{name}`" in r[0][1]]
        hands = rows[0][1] if rows else []
        if not hands or math.dist(hands[0], seat) > .25:
            wrong.append(f"OrielSkyfields.md '{name}': the builder's seat hand is at {seat}, the doc {hands[0] if hands else 'nothing'}")
    for name, a, _, _, b, _, _ in LIFTS:
        rows = [r for r in doc_rows("OrielSkyfields.md", sky, 2) if f"`{name}`" in r[0][1]]
        if not rows or len(rows[0][1]) < 2 or math.dist(rows[0][1][0], a) > .25 or math.dist(rows[0][1][1], b) > .25:
            wrong.append(f"OrielSkyfields.md '{name}': the builder's nodes are {a} and {b}, the doc {rows[0][1] if rows else 'nothing'}")
    for node, ((cx, cz), (w, d)) in SKY_CROSSINGS.items():
        literal = f"new Vector3({cx},0,{cz}),new Vector3({w},5,{d})"
        if literal not in SKY_SOURCE:
            wrong.append(f"OrielBuilder.Skyfields.cs has no crossing at {literal} any more ({node})")
    choir = "## Coordinates (for routes and tests)"
    for label, place in (("Arrival spawn", CH["Arrival"]), ("Way out", CH["DoorWay"]), ("Narthex cradle", CH["Cradle"]), ("Nave talk", (-.2, CH["NaveTalk"][1])),
                         ("First clamp talk", CH["ClampTalk"]), ("Stay 1", STAYS[0][0]), ("Stay 2", STAYS[1][0]), ("Stay 3", STAYS[2][0]), ("Door spawn", CH["DoorSpawn"]),
                         ("Landing cradle", CH["DoorCradle"]), ("Warden talk", CH["WardenTalk"]), ("Record wall", CH["RecordWall"])):
        expect("GlassChoir.md", choir, 2, label, [place], cell=0)
    built = "### Coordinates as built"
    rows = [r for r in doc_rows("GlassChoir.md", built, 1) if r[0][0] == "The four clamps"]
    given = re.findall(r"\*\*\d (?:NE|NW|SW|SE) \((-?[\d.]+), (-?[\d.]+)\)\*\*", rows[0][0][1]) if rows else []
    for i, seat in enumerate(CLAMP_SEATS):
        if i >= len(given) or math.dist((float(given[i][0]), float(given[i][1])), seat) > .25:
            wrong.append(f"GlassChoir.md 'The four clamps': the builder's seat {i + 1} is at ({seat[0]:.2f}, {seat[1]:.2f}), the doc {given[i] if i < len(given) else 'nothing'}")
    for literal in ('kit.Crossing("The body, in view",new Vector3(-.2f,0,NaveTalk.y),new Vector3(14.4f,5,3)', 'kit.Crossing("The first clamp",new Vector3(ClampTalk.x,0,ClampTalk.y),new Vector3(17,5,3)',
                    'kit.Crossing("The engine hall",new Vector3(WardenTalk.x,0,WardenTalk.y),new Vector3(9,5,2.2f)'):
        if literal not in CHOIR_SOURCE:
            wrong.append("OrielBuilder.GlassChoir.cs: no " + literal + " any more")
    if "mouth.size=new Vector3(16,5,3)" not in ORIEL_SOURCE or ISHEN_ARRIVES != (0.0, -49.4):
        wrong.append(f"OrielBuilder.StageManyHands: Ishen's crossing isn't (0, -49.4), 16 x 3 any more ({ISHEN_ARRIVES}, {ISHEN_BOX})")
    cx, cz, sx, sz = CAMP_POCKET
    for label, p in (("the landing's spawn", SK["Arrival"]), ("the pad", SK["Pad"])):
        if not (abs(p[0] - cx) < sx / 2 - 1 and abs(p[1] - cz) < sz / 2 - 1):
            wrong.append(f"OrielBuilder.Skyfields: {label} {p} is no longer in the camp's safe pocket, where the routes expect the heroes' own form ({CAMP_FORM})")
    if len(RAILS) != 9 or NAMED_RAILS != [1, 2, 4]:
        wrong.append(f"OrielBuilder.Orrery: the machine isn't nine rails with the Steward's, the Weigher's and the Lamplighter's named ({len(RAILS)}, {NAMED_RAILS})")
    for line in known:
        print("KNOWN_DRIFT " + line)
    if wrong:
        raise SystemExit("a place here isn't where its map doc says:\n" + "\n".join(wrong) + "\nORIEL_ROUTES_FAILED")


# ------------------------------------------------------------------ the route, checked
class Checked(mh.Checked):
    """manyhands_routes' checks aboard Many Hands, and chapter 7's: round the hull at Idra's hull and clear of the two warps off the stern; in the Orrery inside the lane and
    through a ring's opening at every gate row; in the Skyfields along the Ways, off the people and the built scene's solids; in the Glass Choir on its rooms' floors and off
    its solids. Every route here is played with Taren and Idra out."""

    def __init__(self, *args, **kw):
        super().__init__(*args, **kw)
        self.hull = HULL_IDRA
        self.deployed = PAIR
        self.sky_people = set()
        self.stands = []  # floor in front of a prompt the Ways pass at a distance: (place, how far round it)

    def checks(self):
        c = super().checks()
        c.update({"ManyHandsHull": self.check_hull, "Orrery": self.check_orrery, "OrielSkyfields": self.check_sky, "GlassChoir": self.check_choir})
        return c

    def walk(self, name, x, y, z, tolerance=0.8, seconds=None, stop=None, buttons=None):
        if y == 0 and self.scene == "OrielSkyfields":
            y = sky_y((x, z))
        elif y == 0 and self.scene == "GlassChoir":
            y = choir_y((x, z))
        super().walk(name, x, y, z, tolerance, seconds, stop, buttons)

    # -------------------------------------------------------------- the leg checks
    def check_hull(self, name, to):
        x0, x1, z0, z1 = mh.LIMITS
        for p in self.legs(to):
            if not (x0 + self.hull + 1 < p[0] < x1 - self.hull - 1 and z0 + self.hull + 1 < p[1] < z1 - self.hull - 1):
                self.problems.append(f"{self.name}: '{name}' flies into the harbour line at ({p[0]:.1f}, {p[1]:.1f})")
                return
            hit = next((label for label, c, yaw, hx, hz in mh.HULL_SOLIDS + HULL_EXTRA if box_distance(p, c, yaw, hx, hz) < self.hull + .8), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' flies within {self.hull} m of {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def check_orrery(self, name, to):
        for p in self.legs(to):
            if abs(p[0]) > HALF - 1 - self.hull:
                self.problems.append(f"{self.name}: '{name}' flies {abs(p[0]):.1f} m off the lane's middle, into its wall")
                return
            for rail in RAILS:
                if abs(math.dist(p, CENTRE) - rail.row) < ROW_SOLID + self.hull:
                    along = lane_along(p, rail.row)
                    if min(abs(along - g) for g in LANE_GATES) > RING_OPENING - self.hull - .2:
                        self.problems.append(f"{self.name}: '{name}' meets {rail.name}'s gate row at ({p[0]:.1f}, {p[1]:.1f}), {min(abs(along - g) for g in LANE_GATES):.1f} m off a ring's middle: its truss")
                        return
            hit = next((label for label, c in (("the warp home", WARP_HOME), ("the landing", LANDING)) if box_distance(p, c, 0, 1, 1) < self.hull + .8), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' flies within a hull ({self.hull} m) of {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def check_sky(self, name, to):
        fights = [b for b in encounters().get("OrielSkyfields", [])]
        for p in self.legs(to):
            off = min(min(segment_distance(p, w[i - 1], w[i]) for i in range(1, len(w))) for w in SKY_WAYS)
            in_fight = any(abs(p[0] - b["x"]) < b["width"] / 2 - 1 and abs(p[1] - b["z"]) < b["depth"] / 2 - 1 for b in fights)
            if off > 1.4 and not in_fight and not any(math.dist(p, at) <= reach for at, reach in self.stands):
                self.problems.append(f"{self.name}: '{name}' leaves the Skyfields' ways by {off:.1f} m at ({p[0]:.1f}, {p[1]:.1f})")
                return
            hole = next((n for n, at in SKY_NAMED.items() if math.dist(p, at) < .35 + BODY + .1), None)
            if hole:
                self.problems.append(f"{self.name}: '{name}' walks over {hole}'s place at ({p[0]:.1f}, {p[1]:.1f}), a hole in the floor's navigation")
                return
            who = next((n for n in self.sky_people if math.dist(p, SKY_NAMED[n]) < 1.3), None)
            if who:
                self.problems.append(f"{self.name}: '{name}' walks through {who} at ({p[0]:.1f}, {p[1]:.1f})")
                return
            hit = next((label for label, c, yaw, hx, hz in BUILT_SKY if box_distance(p, c, yaw, hx, hz) < BODY + .15), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' walks into {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def check_choir(self, name, to):
        for p in self.legs(to):
            clear, room = choir_nearest(p)
            # Where two rooms meet a body stands on both: its width all round has to be floor, whichever room's.
            if clear > -BODY and not all(choir_nearest((p[0] + BODY * math.cos(a), p[1] + BODY * math.sin(a)))[0] < 0 for a in [k * math.pi / 4 for k in range(8)]):
                self.problems.append(f"{self.name}: '{name}' leaves the Choir's floor at ({p[0]:.1f}, {p[1]:.1f}): {clear:.1f} m from {room}'s edge")
                return
            hit = next((label for label, c, yaw, hx, hz in BUILT_CHOIR if box_distance(p, c, yaw, hx, hz) < BODY + .15), None)
            if hit:
                self.problems.append(f"{self.name}: '{name}' walks into {hit} at ({p[0]:.1f}, {p[1]:.1f})")
                return

    def reach(self, name, at, place, reach, tolerance):
        """A press made where a walk ended has its prompt in reach on the plan with fifteen centimetres to spare (RouteFightTests asks the same of every route)."""
        if math.dist(at, place) + tolerance + .15 > reach:
            self.problems.append(f"{self.name}: '{name}' stands {math.dist(at, place):.2f} m (and {tolerance} of tolerance) from a prompt at {place} that reaches {reach}")

    # -------------------------------------------------------------- the Orrery's rule
    def lee(self, n, name, x, z, tolerance=1.5, seconds=45):
        """Braked, to the waiting place short of rail n's middle gate, and held there until the driver reads that gate open in its moon's lee (ContinuousReview.InTheLee):
        the moon has passed it and is going away. The lee ends the wait wherever the ship is, from where the wait began to its point (ContinuousReview.WaitedOut), so the leg
        after it is budgeted from where it began: from the waiting place, the Lamplighter's slow ring had 8 s for 19.5 m at 2 m/s, and wasn't read (route 42 in a player,
        November 14). Every leg after it is along the same straight line."""
        check = self.checks().get(self.scene)
        if check:
            check(name, (x, z))
        began = self.at
        self.add(name, seconds, navigate=True, point=dict(x=x, y=1, z=z), tolerance=tolerance, until=f"lee:{n}", leftTrigger=1)
        self.at, self.height = began if began is not None else (x, z), 1

    def cross(self, n, inward=True, slowly=False, here=False):
        """Rail n crossed on the lane's middle: up to its gate row's waiting place, braked; wait in the moon's lee; through on green. Inward the gate row comes first, then
        the rail; outward the rail, then the row. Slowly (the Names of the Moons), the gate row is crossed braked, under the 8 m/s the rail reads a named ring at."""
        rail = RAILS[n - 1]
        its = rail.name + ("'" if rail.name.endswith("s") else "'s")
        if inward:
            # Before a named ring passed slowly the approach ends nearer the ring: from where the wait may end, the braked way through it is 13.5 m (under 7 s at 2 m/s),
            # well inside the eleven seconds a lee keeps the gate open, not 19.5.
            wait, before, row, past = rail.gate_z - 6, rail.gate_z - (12 if slowly else 18), rail.gate_z + 1.5, rail.z + rail.moon + 6
        else:
            wait, before, row, past = rail.z + rail.moon + 6, rail.z + rail.moon + 18, rail.gate_z + 6, rail.gate_z - 7
        if not here:
            self.walk(f"up to rail {n}, {rail.name}", 0, 1, before, tolerance=3)
        self.brake = True
        self.lee(n, f"wait in {its} lee at its middle gate", 0, wait)
        if slowly and inward:
            # Braked through the ring itself (2 m/s), then on at speed: the rail is crossed well before the moon comes round.
            self.walk(f"through {its} named ring on green, slowly", 0, 1, row, tolerance=1)
            self.brake = False
            self.walk(f"on past {its} rail", 0, 1, past, tolerance=3)
        elif slowly:
            # At speed over the rail behind its moon, then braked before the ring, so the ring is passed at 2 m/s.
            self.brake = False
            self.walk(f"over {its} rail behind its moon", 0, 1, row, tolerance=1.5)
            self.brake = True
            self.walk(f"through {its} named ring on green, slowly", 0, 1, past, tolerance=1.5)
            self.brake = False
        else:
            self.brake = False
            self.walk(f"through {its} middle gate on green", 0, 1, past, tolerance=3)

    # -------------------------------------------------------------- the Skyfields' own hands
    def ride(self, name, lift, up=True):
        """A lift node (FibreLift): stand on it, a beat for its prompt, one press (a second, at the far node, would ride back), the ride. The walk after it is to the far
        node with the baked floor's way: there already if the lift carried the party, up the road (which is walked like any floor) if it didn't."""
        _, a, _, _, b, lo, hi = lift
        start, end = (a, b) if up else (b, a)
        self.reach("ride " + name, self.at, start, LIFT_RANGE, .4)
        self.add("settle on " + name + "'s node", 0.6)
        self.add("ride " + name, 0.15, buttons=["South"])
        self.add("release and ride " + name, 4.5)
        self.at = None
        self.walk(f"step off {name}", end[0], hi if up else lo, end[1], tolerance=1)

    def press_at(self, name, flag, place, reach=PROMPT_REACH):
        self.reach(name, self.at, place, reach, self.last_tolerance())
        self.press_for(name, flag)

    def look_at(self, name, lines, flag, place, reach=PROMPT_REACH):
        self.reach(name, self.at, place, reach, self.last_tolerance())
        self.look(name, lines, flag)

    def talk_at(self, name, lines, place, flag=None, shop=False):
        self.reach(name, self.at, place, TALK_REACH, self.last_tolerance())
        if shop:
            self.talk_to_shop(name, lines)
        else:
            self.talk(name, lines, flag=flag)

    def last_tolerance(self):
        walked = [s for s in self.steps if s.get("navigate")]
        return walked[-1]["tolerance"] if walked else .8

    def write(self):
        if self.problems:
            raise SystemExit("\n".join(self.problems) + "\nORIEL_ROUTES_FAILED")
        mh.Route.write(self)


def gate_count(r):
    return sum(1 for s in r.steps if str(s.get("until", "")).startswith("lee:"))


# Off the second junction's landing, (-38, 123), the far bed of its garden stands across the way north with a keeper at each end (SecondGardenKeepers): the builder's
# ways go round them (OrielBuilder.Skyfields.Ways), east along the rim below the bed, and west between its west keeper and the garden house to the sealed garden's door.
# They ran straight through the bed's south wall until November 14. The routes walk those ways' bends.
_LANDING = (JUNCTIONS[1][1], JUNCTIONS[1][3] + 2)
_EAST = next(w for w in SKY_WAYS if w[0] == _LANDING and w[-1] == (20.0, 135.0))
_WEST = next(w for w in SKY_WAYS if w[0] == _LANDING and math.dist(w[-1], SK["SealedHand"]) < .01)
ROUND_THE_BED = _EAST[1:_EAST.index((-30.0, 129.0))]
ROUND_THE_KEEPER = _WEST[1:-2]
SEALED_STAND = _WEST[-2]
TO_THE_HIGH_LIFT = (-42, 129.5)


# ------------------------------------------------------------------ the routes
def orrery():
    r = Checked(PREFIX + "38-orrery", "Chapter 7: Ishen on the dock, the launch, the warp to the Orrery, Lume's hail, Ishen's lesson, the nine rails in their moons' lee, the far gate, the landing", chapter=7)
    H, O = mh.H, mh.O
    r.resume("ManyHands", "chapter6.complete", spawn=H["Arrival"])
    r.lead("Taren")
    # Up off the pads through the lift figures (told: it only saves now), and on up the spine into Ishen's crossing, (0, -49.4), 16 by 3 (StageManyHands).
    r.walk("up off the pads", 0, 0, -55)
    r.walk("up the spine until Ishen speaks", 0, 0, -45, stop="ui:dialogue")
    r.crossing("Ishen arrives", 9, "oriel.charted")  # IshenArrives: starts The Cage Makers; the hull's warp to the Orrery opens
    r.go("down to the hull pad", [(0, -55), (0, -57), (5.2, -58.2), (5, -61)], tolerance=0.5)
    r.reach("launch to the hull", r.at, H["HullPad"], 4, .5)
    r.scene = "ManyHandsHull"
    r.add("launch to the hull", 4, buttons=["South"], expectedForm="Flight")
    r.add("release launch", 0.5)
    r.at, r.speed = O["ArrivalAt"], FLIGHT_SPEED
    # Off the stern, round the dock's pad, then west past Hoarfell's warp (-90, -176) a lane's width north of it, to the Orrery's beside it (-120, -176).
    r.walk("south off the stern dock", -26, 1, -148, tolerance=3)
    r.walk("west past Hoarfell's warp", -108, 1, -164, tolerance=3)
    r.brake = True
    r.walk("settle at the warp to the Orrery", WARP_TO_ORRERY[0], 1, WARP_TO_ORRERY[1] + 4, tolerance=0.8)  # its reach is 5; its piece is solid
    r.brake = False
    r.reach("take the warp to the Orrery", r.at, WARP_TO_ORRERY, 5, .8)
    r.scene = "Orrery"
    r.add("take the warp to the Orrery", 4, buttons=["South"], expectedForm="Flight")
    r.add("release warp", 0.5)
    r.at = (0, 6)
    # Lume's hail (a bark, z 20) and the courier pod falling in behind; the view draws back over the first two rails from z 50; Ishen's line across the lane at z 90.
    r.walk("up the lane past Lume's hail", 0, 1, 50, tolerance=3)
    r.walk("on until Ishen names the moons", 0, 1, TALK_Z + 10, tolerance=3, stop="ui:dialogue")
    r.crossing("Ishen names the moons", 9, "oriel.lightsRead")  # IshenMoonNames: the lights taught
    for n in range(1, len(RAILS) + 1):
        r.cross(n)
    # The far gate is a line across the lane (oriel.crossed, report FarGate), then the landing.
    r.walk("through the far gate", 0, 1, FAR_GATE_Z + 10, tolerance=3)
    r.brake = True
    r.walk("settle over the landing", LANDING[0], 1, LANDING[1] - 4, tolerance=1)  # its reach is 6.5; its piece is solid
    r.brake = False
    r.reach("land at the Skyfields", r.at, LANDING, 6.5, 1)
    r.scene = "OrielSkyfields"
    r.add("land at the Skyfields", 6, buttons=["South"], expectedUi="world", expectedForm=CAMP_FORM)
    r.add("release landing", 0.5)
    r.at, r.speed = SK["Arrival"], COMBAT_SPEED
    r.wait("first look at the camp", 2, expectedFlag="oriel.crossed")
    assert gate_count(r) == 9
    r.write()


def skyfields():
    r = Checked(PREFIX + "39-skyfields", "Chapter 7: the camp, Lume's stall and her lantern, the first lift, the terrace, the first junction, the fibre fields, the second lift, the sagging gardens, the stair gardens, the forecourt, the Choir's door", chapter=7)
    r.resume("OrielSkyfields", "oriel.crossed", speed=COMBAT_SPEED, spawn=SK["Arrival"])
    r.sky_people = {"Lume", "IshenPost"}
    r.lead("Taren")
    # Ishen's crossing is across the whole camp at x -15 (z 4 to 22): walking east from the pad meets it. The book falls to the wall as it opens.
    r.walk("east from the pad until Ishen speaks", -10, 0, 14, stop="ui:dialogue")
    r.crossing("Ishen at the landing", 9, "oriel.ruleSeen")  # IshenLanding
    r.sky_people -= {"IshenPost"}  # he walks with the party from here (KeepsPace), his capsule a trigger
    lume_stand = (SK["LumeAt"][0], 14)
    r.walk("on to Lume's stall", lume_stand[0], 0, lume_stand[1], tolerance=0.4)
    # Her first word ends on two choices; the first is her stall. Gel to the cap before the islands.
    r.reach("greet Lume", r.at, SK["LumeAt"], TALK_REACH, .4)
    r.add("greet Lume", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release greet Lume", 0.3, expectedUi="dialogue")
    r.add("hear Lume out", 12, buttons=["South"], pulseSeconds=0.55, until="ui:options", expectedUi="options")  # LumeFirst
    r.add("release before her choices", 0.5)
    r.add("see her stall", 10, buttons=["South"], pulseSeconds=1.0, until="ui:shop", expectedUi="shop")
    r.add("find the repair gel", 3, pickRow="Repair Gel", until="row:Repair Gel", expectedUi="shop")
    r.stock("buy repair gel up to the cap", "Repair Gel")
    # Chapter 7 is sized on Tempered parts in every slot (DevLoadout). Taren and Idra reach the camp with Prime modules at best (a Reservoir, a Twin Thread), and Sela,
    # resting, keeps the Tempered one she wore: two Tempered Modules for the pair out, 2,080 of the 8,500 or so in hand after the gels (the chain's saves: 9,819 after
    # the Orrery, less chapter 2's Refined set).
    r.stock_part("Tempered Module", 2)
    r.add("leave Lume's stall", 0.5, buttons=["East"], expectedUi="world")
    r.add("release Lume's stall", 0.3)
    # Her second word is the lantern (LumeLanternOffer: starts The Far End and opens the stall again). Taken here, where a player takes it: the rest is chapter-42's.
    r.talk_at("Lume's lantern", 4, SK["LumeAt"], shop=True)
    lift1, lift2, lift3 = LIFTS
    r.go("east to the first lift", [(12, 14), (20, 13), lift1[1]], tolerance=0.4)
    r.ride("the first lift", lift1)
    # The terrace, east to west: the Glassback's open stone, the walled garden's crossing at x -6, the junction's at x -22, the first junction's collar.
    r.walk("onto the terrace's open stone", 18, 0, 43)
    r.fight_ground("Sky_Glassback")
    r.fought()
    r.walk("west along the terrace until Idra speaks", -10, 0, 43, stop="ui:dialogue")
    r.crossing("two gardeners in a walled bed", 5, "oriel.gardenSeen")  # IdraEmptyGarden
    r.walk("on until Taren reads the junction", -24, 0, 43, stop="ui:dialogue")
    r.crossing("the first junction", 6, "oriel.junctionRead")  # IshenJunction
    j1, j2, j3 = JUNCTIONS
    seat1 = (SEAT_HANDS[0][0] + 1, SEAT_HANDS[0][1])
    r.stands += [(seat1, 3)]
    r.go("to the first junction's collar", [(-30, 47), seat1], tolerance=0.4)
    r.press_at("seat the first junction", "oriel.junction1", SEAT_HANDS[0])
    r.wait("the island rises flush", 4)
    r.go("over the first junction", [(j1[1], j1[2] - 2), (j1[1], j1[3] + 2)], tolerance=0.5)
    # The fibre fields, west to east: the wisps between the three floor fibres (x -7, 0, 7), the second lift.
    r.walk("onto the fibre fields", -20, 0, 71)
    r.walk("onto the wisps' floor between two fibres", -3.5, 0, 71)
    r.fight_ground("Sky_Wisps")
    r.fought()
    r.go("east to the second lift", [(20, 71), lift2[1]], tolerance=0.4)
    r.ride("the second lift", lift2)
    # The sagging gardens, east to west: Idra's count at x 22 (short of the mixed fight's floor, which begins at x 19), the fight, the second junction's collar.
    r.walk("west until Idra counts the paces", 22, 0, 101, stop="ui:dialogue")
    r.crossing("twelve paces behind", 5, "oriel.followerSeen")  # IdraGardenerFollows
    r.walk("onto the sagging gardens' fight floor", 14, 0, 101)
    r.fight_ground("Sky_Mixed")
    r.fought()
    seat2 = (SEAT_HANDS[1][0] + 1, SEAT_HANDS[1][1])
    r.stands += [(seat2, 3)]
    # Past the gardener waiting at its frayed junction (optional: chapter-42 asks it), to the second junction's collar.
    r.go("west along the sagging gardens", [(-12, 103), (-30, 104), seat2], tolerance=0.5)
    r.press_at("seat the second junction", "oriel.junction2", SEAT_HANDS[1])  # seated: the garden stays and its keepers don't turn
    r.wait("the garden rises flush", 4)
    r.go("over the second junction", [(j2[1], j2[2] - 2), (j2[1], j2[3] + 2)], tolerance=0.5)
    # The stair gardens, west to east, to the third junction's collar.
    seat3 = (SEAT_HANDS[2][0] + 1, SEAT_HANDS[2][1])
    r.stands += [(seat3, 3)]
    r.go("east along the stair gardens", ROUND_THE_BED + [(-30, 129), (-10, 130), (10, 131), seat3], tolerance=0.5)
    r.press_at("seat the third junction", "oriel.junction3", SEAT_HANDS[2])  # seated: the chest rides up with it
    r.wait("the chest's island rises flush", 4)
    r.go("over the third junction", [(j3[1], j3[2] - 2), (j3[1], j3[3] + 2)], tolerance=0.5)
    # The forecourt: the Warden's pruners and a wisp, the doorstep (a crossing 48 wide at z 164), the door.
    r.walk("onto the forecourt", 12, 0, 155.8)
    r.fight_ground("Sky_Door")
    r.fought()
    r.walk("up to the Choir's step until Idra speaks", 4, 0, 163.6, stop="ui:dialogue")
    r.crossing("the Choir's doorstep", 2, "oriel.doorReached")  # IdraChoirDoor: the gardener sits on the step
    door = (SK["DoorAt"][0], SK["DoorAt"][1] - .8)
    r.walk("to the Choir's door", door[0], 0, door[1] - 1.8, tolerance=0.4)
    r.reach("into the Glass Choir", r.at, door, 3.6, .4)
    r.scene = "GlassChoir"
    r.add("into the Glass Choir", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the Choir's door", 0.5)
    r.at = CH["Arrival"]
    r.wait("inside the great door", 1.5, expectedFlag="oriel.doorReached")
    r.write()


def choir():
    r = Checked(PREFIX + "40-glass-choir", "Chapter 7: the nave in silence, Sela's line, the first clamp's release, the three stays and their fights, the heart cradle", chapter=7)
    r.resume("GlassChoir", "oriel.doorReached", speed=COMBAT_SPEED, spawn=CH["Arrival"])
    r.lead("Taren")
    # The nave's silence: 95 m with nothing said, then Sela's line across the nave and the overlook at z 96.
    r.go("down the nave in silence", [(-1, 14), (-1.5, 58)], tolerance=0.6)
    r.walk("on down the nave until Sela speaks", -1.5, 0, 100, stop="ui:dialogue")
    r.crossing("a held road", 1, "oriel.choirSeen")  # SelaGlassChoir
    r.go("down to the first clamp", [(-1.5, 118), (-3, 129)], tolerance=0.6)
    r.walk("on until Taren reads the lock", -3, 0, 141, stop="ui:dialogue")  # the crossing level with the clamp, z 136 to 139
    r.crossing("the first clamp", 8, "oriel.releaseLearned")  # IshenFirstClamp: the release read
    # The rule introduced, nothing awake: stay 1's seat on the rib walk, south of rib 1.
    stay1 = stay_stand(0)
    r.go("down the stair to the rib walk", [(-2, 152), (-2, 158), stay1], tolerance=0.4)
    r.press_at("take the first stay's weight", "oriel.stay1", STAYS[0][0])
    r.wait("the stay pulls, then eases, and rib 1 lifts", 4.5)
    # Tested: stay 2 opens the bay's far side and cracks the casings; the two sentinels wake.
    stay2 = stay_stand(1)
    r.go("under rib 1 into the sentinel bay", [(-2, stay1[1]), (-2, 170), (-4, 190), stay2], tolerance=0.4)
    r.press_at("take the second stay's weight", "oriel.stay2", STAYS[1][0])
    r.fight_ground("Choir_Sentinels", rounds=5)
    r.fought()
    # Combined: the fibre gallery's wisps and its sentinel, then stay 3, whose slack wakes the Clamp Tenders that keep it.
    r.go("down the stair to the fibre gallery", [(-2, 208), (-3, 224)], tolerance=0.6)
    r.fight_ground("Choir_Fibres", rounds=5)
    r.fought()
    stay3 = stay_stand(2)
    r.walk("to the third stay's seat", stay3[0], 0, stay3[1], tolerance=0.4)
    r.press_at("take the third stay's weight", "oriel.stay3", STAYS[2][0])
    r.fight_ground("Choir_Tenders", rounds=4)
    r.fought()
    r.wait("with its tenders stopped, the third stay eases again", 5)
    cradle = (CH["DoorCradle"][0] + 1.8, CH["DoorCradle"][1] - .4)
    r.go("through rib 3's pass to the heart landing", [(-2, 252), cradle], tolerance=0.4)
    r.reach("repair and save at the heart cradle", r.at, CH["DoorCradle"], 3, .4)
    r.save_at("repair and save at the heart cradle", "release the cradle")
    r.write()


def warden():
    r = Checked(PREFIX + "41-last-warden", "Chapter 7: the Warden's office, the Last Warden (its door retried), the four clamps, it stands down, the court's record read", chapter=7)
    door = CH["DoorSpawn"]
    r.resume("GlassChoir", "oriel.stay3", speed=COMBAT_SPEED, spawn=door)
    r.lead("Taren")
    # The Warden's office: a crossing at the stair's foot, (0, 273), 9 by 2.2, needing the release read; it saves at Door.
    r.walk("to the stair's head", -2, 0, 264)
    r.walk("down the stair until the Warden speaks", 0, 0, CH["WardenTalk"][1] + .5, stop="ui:dialogue")
    r.crossing("the Warden's office", 5, "oriel.wardenMet")  # WardenFirst
    # The door: straight from the checkpoint (Door, on the heart landing) down the stair's middle onto the hall's floor, far enough past the seal at the stair's foot that
    # the partner three metres behind is through it too, or the fight never begins (SEAL_Z, above). Eight metres short of the Warden at its engine.
    r.lead("Taren")
    r.door("LastWarden", "onto the engine hall's floor", 0, CH_LEVELS["HallFloor"], WARDEN_DOOR_Z, spawn=door, tolerance=WARDEN_DOOR_TOLERANCE)
    r.press("lock the Last Warden", "LeftShoulder", 0.2)
    stop = "flag:bossdown.LastWarden"
    # The fight is the driver's own clamp hand (ContinuousReview.Oriel.cs): at each held line it walks to a lit seat beside its fibre, takes the weight with the pad, keeps
    # still until the staff is down, and stops a Clamp Tender first. These rounds are the fight between: swings, skills, dodges, mends.
    for i in range(12):
        r.rotation(f"LastWarden {i + 1}", stop)
        r.add(f"fight LastWarden round {i + 1}", 35, buttons=["South"], pulseSeconds=0.46, navigate=True, approachTarget=True, tolerance=2.3, rangedTolerance=12,
              evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=0, z=0))
    r.add("the four let go together", 4, until=stop, expectedFlag="bossdown.LastWarden")
    # Nothing is said as the Bridle opens; the Warden's last words follow by themselves (AfterTalk, 1.2 s on).
    r.crossing("the Warden stands down", 6, "oriel.wardenStoodDown", wait=12)  # WardenAsks
    r.add("reach the fallen hero after the Warden", 16, navigate=True, approachPartner=True, tolerance=1.2, stopWhen="partyAlive", point=dict(x=0, y=0, z=0))
    r.add("both heroes ready after the Warden", 6, until="partyAlive")
    r.lead("Taren")
    r.fought()
    wall = (CH["RecordWall"][0] + 1.3, CH["RecordWall"][1] + 1.4)  # beside Ishen's place there (-12.2, 299.4), not on it
    r.walk("to the record wall", wall[0], 0, wall[1], tolerance=0.4)
    r.look_at("look at the record wall", 9, "oriel.coronachHeard", CH["RecordWall"], 3)  # IshenReads: the Coronach named
    r.wait("Ishen has read the record", 1.5, expectedFlag="oriel.wardenStoodDown")
    r.write()


def errands():
    r = Checked(PREFIX + "42-oriel-errands", "Chapter 7: up out of the Choir, the gardener's gardens, the lantern on the highest island, the chart to the camp, Lume's answer, Ishen's moons and the Orrery's lap", chapter=7)
    r.resume("GlassChoir", "oriel.coronachHeard", speed=COMBAT_SPEED, spawn=CH["DoorSpawn"])
    r.lead("Taren")
    # Up out of the Choir the way it came, every stay slack and the light moving in the glass (the walk back up is the payoff: docs/maps/GlassChoir.md).
    r.go("up out of the Choir", [(-2, 252), (-3, 232), (-2, 208), (-4, 190), (-2, 170), (-2, 152), (-3, 129), (-1.5, 118), (-1.5, 58), (-1, 14), (0, -2.2)], tolerance=0.6)
    r.reach("out by the great door", r.at, CH["DoorWay"], 3.4, .6)
    r.scene = "OrielSkyfields"
    r.add("out by the great door", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the great door", 0.5)
    r.at = SK["ChoirSpawn"]
    r.sky_people = {"Lume", "IshenPost"}
    r.stands += [(SK["ChoirSpawn"], 6)]  # the forecourt's open stone before the door
    j1, j2, j3 = JUNCTIONS
    # The Empty Gardens. The gardener sits on the Choir's step; where it asked is the sagging gardens' frayed junction, two islands down.
    r.go("down the forecourt to the third junction", [(10, 157), (j3[1], j3[3] + 2), (j3[1], j3[2] - 2)], tolerance=0.5)
    r.go("west along the stair gardens", [(10, 131), (-10, 130), (-30, 129)] + ROUND_THE_BED[::-1] + [(j2[1], j2[3] + 2)], tolerance=0.5)
    ask = (ASKS[0] - 1.1, ASKS[1] - 1.6)
    garden1 = (GARDENS[0][0][0], GARDENS[0][0][1] - 1.2)
    r.stands += [(ask, 3), (garden1, 3)]
    r.go("over the second junction to the frayed garden", [(j2[1], j2[2] - 2), (-30, 104), (-12, 103), ask], tolerance=0.4)
    r.look_at("look at the gardener's frayed junction", 6, "oriel.gardenerAsked", ASKS, 3.2)  # IdraGardenerWaits: starts The Empty Gardens
    r.walk("to the first frayed garden's collar", garden1[0], 0, garden1[1], tolerance=0.4)
    r.press_at("seat the first frayed garden", "oriel.garden1", GARDENS[0][0], 2.8)
    garden2, garden3 = (GARDENS[1][0][0], GARDENS[1][0][1] - 1.2), (GARDENS[2][0][0], GARDENS[2][0][1] - 1.2)
    r.stands += [(garden2, 3), (garden3, 3)]
    r.go("back over the second junction", [(-12, 103), (-30, 104), (j2[1], j2[2] - 2), (j2[1], j2[3] + 2)] + ROUND_THE_BED + [(-30, 129), (garden2[0], 129.6), garden2], tolerance=0.4)
    r.press_at("seat the second frayed garden", "oriel.garden2", GARDENS[1][0], 2.8)
    r.go("east to the third frayed garden", [(garden2[0], 129.6), (-10, 130), (garden3[0], 130.8), garden3], tolerance=0.4)
    r.press_at("seat the third frayed garden", "oriel.garden3", GARDENS[2][0], 2.8)
    r.wait("three gardens seated: the gardener goes ahead", 1, expectedFlag="oriel.gardensSeated")
    sealed = SEALED_STAND  # the sealed garden's way's last bend, 1.8 m from the door's hand
    r.stands += [(TO_THE_HIGH_LIFT, 3.5)]
    r.go("west after the gardener to the sealed garden", [(garden3[0], 130.8), (-10, 130), (-30, 129)] + ROUND_THE_BED[::-1] + [(j2[1], j2[3] + 2)] + ROUND_THE_KEEPER + [sealed], tolerance=0.4)
    r.look_at("go in after the gardener", 7, "oriel.sealedGardenSeen", SK["SealedHand"], 3)  # IdraSealedGarden: The Empty Gardens done
    # The Far End: the lift to the highest island, the worn seat, and down again.
    lift3 = LIFTS[2]
    r.go("to the lift to the highest island", [lift3[1]], tolerance=0.4)  # west of the bed, by the sealed garden's door
    r.ride("the lift to the highest island", lift3)
    seat = (SK["SeatHand"][0] + 1.4, SK["SeatHand"][1] - .6)
    r.walk("to the worn seat", seat[0], 0, seat[1], tolerance=0.4)
    r.look_at("set the lantern in the seat", 3, "oriel.lanternSet", SK["SeatHand"], 3)  # IdraFarLantern
    r.walk("back to the lift's high node", lift3[4][0], 0, lift3[4][1], tolerance=0.4)
    r.ride("the lift down from the highest island", lift3, up=False)
    # Back up to the forecourt and into the Choir: the camp's only way up from here crosses Meret's call, after which Lume's next word is the chapter's end, and the chart
    # won't travel within the zone it is read in. From the Choir it takes the party to the camp's west end.
    door = (SK["DoorAt"][0], SK["DoorAt"][1] - .8)
    r.go("back to the forecourt", [(-36, 130), (-30, 129), (-10, 130), (10, 131), (j3[1], j3[2] - 2), (j3[1], j3[3] + 2), (10, 157), (4, 162), (door[0], door[1] - 1.8)], tolerance=0.4)
    r.reach("into the Glass Choir", r.at, door, 3.6, .4)
    r.scene = "GlassChoir"
    r.add("into the Glass Choir", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release the Choir's door", 0.5)
    r.at = CH["Arrival"]
    r.chart("OrielSkyfields", "OrielSkyfields", spawn=SK["Arrival"], form=CAMP_FORM)
    r.speed = COMBAT_SPEED
    r.lead("Taren")
    ishen = (SK["IshenPost"][0], 14)
    r.walk("east to Ishen's crates", ishen[0], 0, ishen[1], tolerance=0.4)  # through the landing's crossing, told: it only saves
    r.talk_at("Ishen and the three named gates", 4, SK["IshenPost"])  # IshenMoonsOffer: starts The Names of the Moons
    r.walk("on to Lume's stall", SK["LumeAt"][0], 0, 14, tolerance=0.4)  # x 9: short of Meret's call at x 17 to 21
    r.talk_at("the far lantern is lit", 3, SK["LumeAt"], shop=True)  # LumeLanternReturn, then her stall
    r.wait("The Far End is done", 0.5, expectedFlag="lume.lanternLit")
    pad = (SK["Pad"][0] + 2.4, SK["Pad"][1])
    r.stands += [(pad, 6)]  # the landing pad's open paving
    r.go("back west to the landing pad", [(0, 14), (-15, 14), SK["Arrival"], pad], tolerance=0.4)
    r.reach("launch to the Orrery", r.at, SK["Pad"], 4, .4)
    r.scene = "Orrery"
    r.add("launch to the Orrery", 4, buttons=["South"], expectedForm="Flight")
    r.add("release launch", 0.5)
    r.at, r.speed = (0, FROM_CAMP_Z), FLIGHT_SPEED
    # The lap: out across rails 9 to 1, each in its moon's lee (the Weigher's and the Lamplighter's rings wait on the Steward's, so passing them now reads nothing), the
    # Steward's ring passed slowly; then back in, the Weigher's and the Lamplighter's slowly, and home.
    for n in range(len(RAILS), 0, -1):
        r.cross(n, inward=False, slowly=n == 1)
    r.cross(1, inward=True, here=True)
    for n in range(2, len(RAILS) + 1):
        r.cross(n, inward=True, slowly=n in NAMED_RAILS)
    r.wait("the three names read", 0.5, expectedFlag="oriel.lamplighterRead")
    r.walk("through the far gate", 0, 1, FAR_GATE_Z + 10, tolerance=3)
    r.brake = True
    r.walk("settle over the landing", LANDING[0], 1, LANDING[1] - 4, tolerance=1)
    r.brake = False
    r.reach("land at the Skyfields", r.at, LANDING, 6.5, 1)
    r.scene = "OrielSkyfields"
    r.add("land at the Skyfields", 6, buttons=["South"], expectedUi="world", expectedForm=CAMP_FORM)
    r.add("release landing", 0.5)
    r.at, r.speed = SK["Arrival"], COMBAT_SPEED
    r.walk("east to Ishen's crates", ishen[0], 0, ishen[1], tolerance=0.4)
    r.talk_at("what Ishen took down", 3, SK["IshenPost"], flag="ishen.moonsRead")  # IshenMoonsReturn: The Names of the Moons done
    cradle = (SK["Cradle"][0] + 1.6, SK["Cradle"][1] + 1)
    r.stands += [(cradle, 6)]  # the camp's open paving round its cradle
    r.go("back to the camp cradle", [(-15, 14), cradle], tolerance=0.4)
    r.reach("repair and save at the camp cradle", r.at, SK["Cradle"], 3, .4)
    r.save_at("repair and save at the camp cradle", "release the cradle")
    assert gate_count(r) == 18
    r.write()


def far_end():
    r = Checked(PREFIX + "43-the-far-end", "Chapter 7's close: Meret's call coming into the camp, Lume reads Brekk's word up the chain, a save at the camp cradle", chapter=7)
    r.resume("OrielSkyfields", "ishen.moonsRead", speed=COMBAT_SPEED, spawn=SK["CampSpawn"])
    r.sky_people = {"Lume", "IshenPost"}
    r.stands += [(SK["CampSpawn"], 4)]  # where the camp cradle's save stands the party, on the camp's open paving
    r.lead("Taren")
    # Meret's call is a crossing across the whole camp at x 19 (x 17 to 21), waiting on the record read: walking east past the last lantern meets it.
    r.go("east through the camp", [(-15, 14), (0, 14), (12, 14)], tolerance=0.5)
    r.walk("on east until Meret calls", 22, 0, 13.5, stop="ui:dialogue")
    r.crossing("Meret on the radio", 10, "oriel.meretHeard")  # MeretOrielCall
    r.walk("back to the last lantern", 12, 0, 14)
    r.walk("to Lume", SK["LumeAt"][0], 0, 14, tolerance=0.4)
    r.talk_at("Brekk's word up the chain", 7, SK["LumeAt"], flag="chapter7.complete")  # LumeBrekksWord: the chapter's last talk
    r.wait("chapter seven closes", 9.5, expectedFlag="oriel.brekkHeard")  # the chapter's card stays nine seconds
    cradle = (SK["Cradle"][0] + 1.6, SK["Cradle"][1] + 1)
    r.stands += [(cradle, 6)]  # the camp's open paving round its cradle
    r.go("back to the camp cradle", [(0, 14), (-15, 14), cradle], tolerance=0.4)
    r.reach("repair and save at the camp cradle", r.at, SK["Cradle"], 3, .4)
    r.save_at("repair and save at the camp cradle", "release the cradle")
    r.write()


if __name__ == "__main__":
    check_docs()
    orrery()
    skyfields()
    choir()
    warden()
    errands()
    far_end()
    print("ORIEL_ROUTES_OK")
