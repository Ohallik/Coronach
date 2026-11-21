"""Reject a replay route the driver would misread without a word.

The driver (Lattice.UI.ContinuousReview) parses a route with JsonUtility, which ignores a key it doesn't know: a misspelt
"stopWhen" or "expectedFlag" is a check that silently never runs. This reads the schema from the C# it is parsed into
(Lattice/Assets/_Project/Scripts/UI/QualityRoute.cs), so a new field is known here the moment it exists there, and checks:

  - every key of the route, of each step and of each segment is a field of its class, with a value of that field's type;
  - every scene named (the route's, a step's expectedScene, a "scene:" condition) is "Title" or a ZoneDef asset's file name;
  - every "until" and "stopWhen" is a form the driver's Condition() knows (it throws on any other, mid-chain);
  - a row named as a stop on Sela's chart ("Stop_<id>", in pickRow or after "row:") is a ChartStop, and a step that picks one holds the pause menu's pause;
  - UI states, forms, heroes, buttons, the loadout and the difficulty are ones that exist; a step has a name and 0 < seconds <= 180;
  - "deployed", the pair a route is played with once the party is three, names two different heroes; "lead:<hero>" ends a step when that hero leads;
  - the driver's own hands are asked for in words it knows: "useItem" is a ConsumableDef's id or "none"; "stock" is "cap" or a count, on a step
    whose pickRow is a consumable's row (or a count, on a part's row) and which ends on "stocked"; "bossDoor" is a boss EnemyDef's id, once a boss; a route of
    chapter 3 onward (chapter-17 and later) says "useExtra" one way or the other; and a route after the Gear page's drill (chapter-06 and later)
    says "wearBest" one way or the other, which no route up to the drill may set.

    python tools/route_lint.py                          every route in docs/quality/routes
    python tools/route_lint.py docs/quality/routes/chapter-17-xylos-nearspace.json [more.json ...]
    python tools/route_lint.py --scenes Rootmarket,BraidedRiver route.json     also accept zones not defined yet
    python tools/route_lint.py --stops Rootmarket route.json                   also accept a chart stop not defined yet
    python tools/route_lint.py --heroes Idra route.json                        also accept a hero whose CharacterDef isn't written yet

tools/quality_route.py, the preflight every replay runs, calls lint() too. Exit status 1 with any problem; last line ROUTE_LINT_OK.
"""
import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCHEMA = ROOT / "Lattice" / "Assets" / "_Project" / "Scripts" / "UI" / "QualityRoute.cs"
DEFINITIONS = ROOT / "Lattice" / "Assets" / "_Project" / "Resources" / "Definitions"
ROUTES = ROOT / "docs" / "quality" / "routes"

# ContinuousReview.Condition: the bare names, and the forms that take an argument after the colon.
CONDITIONS = {"targetDown", "rowReached", "arrived", "stocked", "saved", "partnerDown", "partyAlive"}
CONDITION_FORMS = ("flag:", "ui:", "scene:", "row:", "lead:", "clear:", "lee:", "docked:", "launched:", "prompt:")
# "prompt:<words>": the screen offers the lead a prompt whose words begin with these (chapter 9, ContinuousReview.Home.cs: Orrin walking to his board).
# "docked:<id>" / "launched:<id>": chapter 10's berth (ContinuousReview.Berth.cs, ch10-wiring 6.1.1): the party stands on that berth, or is back off it, the change over.
BERTH_ID = re.compile(r"^[A-Za-z][A-Za-z0-9]*$")
# "clear:x,z;x,z": the way from the lead through these plan points is clear of what the room does on a clock (ContinuousReview.ClearAhead, ReplayWays.Points).
CLEAR_WAY = re.compile(r"^-?\d+(\.\d+)?,-?\d+(\.\d+)?(;-?\d+(\.\d+)?,-?\d+(\.\d+)?)*$")
# Lattice.Core.Deployment.Size: how many heroes are out on the field, which is how many a route's "deployed" names.
PAIR = 2
# ContinuousReview.ObserveUi.
UI_STATES = {"loading", "unavailable", "saves", "settings", "title", "defeat", "bench", "pause", "shop", "options", "dialogue", "world"}
PAUSE_UI = {"bench", "pause", "defeat"}
FORMS = {"Natural", "Shaped", "CivilFlight", "Flight"}  # Lattice.Data.BodyForm
# UnityEngine.InputSystem.LowLevel.GamepadButton, which the driver parses a button into, ignoring case.
BUTTONS = {b.lower() for b in (
    "DpadUp", "DpadDown", "DpadLeft", "DpadRight", "North", "East", "South", "West", "LeftStick", "RightStick", "LeftShoulder", "RightShoulder",
    "Start", "Select", "LeftTrigger", "RightTrigger", "X", "Y", "A", "B", "Cross", "Square", "Triangle", "Circle")}
# Lattice.UI.DevLoadout: and JoinedLoadout, chapter 6 once Idra has joined (its "chapter6" is the arrival, before she is met).
LOADOUTS = {"starter", "moon", "hushwell", "gullet", "chapter6joined"} | {"chapter%d" % n for n in range(2, 11)}
# Lattice.Core.Difficulty.TryNamed, exact case: the driver refuses any other name at boot. Left out, a route plays at Standard.
DIFFICULTIES = {"Gentle", "Standard", "Demanding"}
# QualityStep.useItem beside a ConsumableDef's id: the driver keeps its hands off the pouch for the step.
NO_ITEM = "none"
# QualityStep.stock beside a count: all the party may carry of it (ConsumableDef.stack).
TO_CAP = "cap"
# The first route of chapter 3, where the story unlocks the EXTRA gauge: from here a route says whether the driver uses it.
FIRST_EXTRA_ROUTE = 17
# The first route after chapter-05-drill-equipment, which teaches the Gear page with the pad: from here a route says whether the driver wears the best it holds
# (QualityRoute.wearBest), and no route before it may, or the drill's own presses would meet parts the driver had already moved.
FIRST_WEAR_ROUTE = 6


def schema():
    """{class: {field: C# type}} for QualityRoute, QualitySegment and QualityStep, read from the source."""
    text = SCHEMA.read_text(encoding="utf-8-sig")
    classes = {}
    for found in re.finditer(r"public sealed class (\w+)\s*\{(.*?)\n    \}", text, re.S):
        body = re.sub(r"//[^\n]*", "", found.group(2))
        fields = {}
        for declared in re.finditer(r"public\s+([\w\[\]<>.]+)\s+([^;(){}]+);", body):
            for part in declared.group(2).split(","):
                fields[part.split("=")[0].strip()] = declared.group(1)
        classes[found.group(1)] = fields
    for needed in ("QualityRoute", "QualitySegment", "QualityStep"):
        if not classes.get(needed):
            raise SystemExit("route_lint: could not read %s from %s" % (needed, SCHEMA))
    return classes


def names(kind):
    """The ids of one kind of definition: its assets' file names."""
    return {p.stem for p in (DEFINITIONS / kind).glob("*.asset")}


def fields(kind, field):
    """{id: what one serialized field of each asset of a kind says} (a consumable's displayName, an enemy's boss)."""
    found = {}
    for path in (DEFINITIONS / kind).glob("*.asset"):
        said = re.search(r"^  %s: *(.*)$" % re.escape(field), path.read_text(encoding="utf-8-sig"), re.M)
        if said:
            found[path.stem] = said.group(1).strip().strip("'\"")
    return found


EDITOR = ROOT / "Lattice" / "Assets" / "_Project" / "Scripts" / "Editor"
STOP = "Stop_"  # Lattice.UI.RouteChart.StopPrefix: a stop on Sela's chart, as pickRow and "row:" name it
_stops = None


def stops():
    """The ids of the stops on Sela's chart: the ChartStop assets, and those a builder defines that Unity hasn't written yet
    (ArenaBuilder.Asset<ChartStop>("id", ...) anywhere in the editor scripts, and ContentScaffold's table of the first four)."""
    global _stops
    if _stops is None:
        _stops = names("ChartStop")
        for source in EDITOR.rglob("*.cs"):
            text = source.read_text(encoding="utf-8-sig")
            _stops |= set(re.findall(r'Asset<ChartStop>\(\s*"(\w+)"', text))
            table = re.search(r"\]\s*Stops\s*=\s*\{(.*?)\n\s*\};", text, re.S)
            if table:
                _stops |= set(re.findall(r'^\s*\("(\w+)"', table.group(1), re.M))
    return _stops


def fits(value, kind):
    if kind == "string":
        return isinstance(value, str)
    if kind == "bool":
        return isinstance(value, bool)
    if kind in ("float", "int"):
        return isinstance(value, (int, float)) and not isinstance(value, bool) and (kind == "float" or float(value).is_integer())
    if kind == "string[]":
        return isinstance(value, list) and all(isinstance(v, str) for v in value)
    if kind == "Vector3":
        return isinstance(value, dict) and set(value) <= {"x", "y", "z"} and all(fits(v, "float") for v in value.values())
    return isinstance(value, list) and all(isinstance(v, dict) for v in value)  # QualityStep[], QualitySegment[]


def keys(found, fields, where, problems):
    for key, value in found.items():
        if key not in fields:
            near = [f for f in fields if f.lower() == key.lower()] or [f for f in fields if f.lower().startswith(key.lower()[:6])]
            problems.append("%s: unknown key '%s'%s: the driver would ignore it" % (where, key, " (did you mean '%s'?)" % near[0] if near else ""))
        elif value is not None and not fits(value, fields[key]):
            problems.append("%s: '%s' should be a %s, not %s" % (where, key, fields[key], json.dumps(value)[:40]))


def lint(route, extra_scenes=(), extra_stops=(), extra_heroes=()):
    """Every problem in one parsed route, as sentences. An empty list is a route the driver reads as written."""
    problems = []
    classes = schema()
    scenes = names("ZoneDef") | {"Title"} | set(extra_scenes)
    heroes = names("CharacterDef") | set(extra_heroes)
    # A consumable's row at a stall or a bench starts with its displayName; a boss is an EnemyDef that says so.
    consumables = fields("ConsumableDef", "displayName")
    parts = fields("TechPartDef", "displayName")
    bosses ={enemy for enemy, said in fields("EnemyDef", "boss").items() if said == "1"}
    doors = {}
    if not isinstance(route, dict):
        return ["the route is not a JSON object"]
    keys(route, classes["QualityRoute"], "route", problems)
    # Chapter 3's story unlocks the EXTRA gauge. A route that leaves the key out plays every fight after it as if the gauge weren't there, without a word.
    numbered = re.match(r"chapter-(\d+)-", route.get("name") or "")
    if numbered and int(numbered.group(1)) >= FIRST_EXTRA_ROUTE and not isinstance(route.get("useExtra"), bool):
        problems.append("route: from chapter 3 (chapter-%d) a route says \"useExtra\": true, or false if it is about leaving the gauge alone" % FIRST_EXTRA_ROUTE)
    # A route that leaves "wearBest" out plays on in whatever the game dropped into an empty slot: chapter 10 was reached in a Refined Edge with two Choirwrought ones in the bag.
    if numbered and int(numbered.group(1)) >= FIRST_WEAR_ROUTE and not isinstance(route.get("wearBest"), bool):
        problems.append("route: after the Gear page's drill (chapter-%02d on) a route says \"wearBest\": true, or false if it is about the Gear page itself" % FIRST_WEAR_ROUTE)
    if numbered and int(numbered.group(1)) < FIRST_WEAR_ROUTE and route.get("wearBest") is True:
        problems.append("route: \"wearBest\" is for the routes after the Gear page's drill (chapter-%02d on): up to the drill the Gear page is the route's own" % FIRST_WEAR_ROUTE)

    def scene(value, where):
        if isinstance(value, str) and value and value not in scenes:
            problems.append("%s: unknown scene '%s' (no ZoneDef asset by that name)" % (where, value))

    def stop(value, where):
        # The driver steers the d-pad at a stop by its name, and at a name nothing on the chart answers to it would press until the step ran out.
        if isinstance(value, str) and value.startswith(STOP) and value[len(STOP):] not in stops() | set(extra_stops):
            problems.append("%s: unknown stop '%s' (no ChartStop by that id; the chart has %s)" % (where, value, ", ".join(sorted(stops())) or "none"))

    def condition(value, where):
        if not isinstance(value, str) or not value:
            return
        if value in CONDITIONS:
            return
        form = next((f for f in CONDITION_FORMS if value.startswith(f)), None)
        if form is None or not value[len(form):]:
            problems.append("%s: unknown condition '%s' (the driver knows %s and %s<...>)" % (where, value, ", ".join(sorted(CONDITIONS)), "<...>, ".join(CONDITION_FORMS)))
        elif form == "scene:":
            scene(value[len(form):], where)
        elif form == "ui:" and value[len(form):] not in UI_STATES:
            problems.append("%s: unknown UI state in '%s' (one of %s)" % (where, value, ", ".join(sorted(UI_STATES))))
        elif form == "row:":
            stop(value[len(form):], where)
        elif form == "lead:" and value[len(form):] not in heroes:
            problems.append("%s: unknown hero in '%s' (no CharacterDef asset by that name)" % (where, value))
        elif form == "clear:" and not CLEAR_WAY.match(value[len(form):]):
            problems.append("%s: '%s' should be plan points 'x,z;x,z' (the driver reads no way at all from it, and waits until the step runs out)" % (where, value))
        # "lee:n": the Orrery's rail n (1 the outermost) has the gate nearest the lead open in its moon's lee (ContinuousReview.InTheLee). A rail the zone hasn't is never true.
        elif form == "lee:" and not (value[len(form):].isdigit() and 1 <= int(value[len(form):]) <= 9):
            problems.append("%s: '%s' should name one of the Orrery's nine rails by its number, 1 the outermost" % (where, value))
        elif form in ("docked:", "launched:") and not BERTH_ID.match(value[len(form):]):
            problems.append("%s: '%s' should name a berth by its id (Clamp1, Arm), as Berth.Declare was given it" % (where, value))

    if not isinstance(route.get("scene"), str) or not route.get("scene"):
        problems.append("route: no scene")
    scene(route.get("scene"), "route")
    if route.get("loadout") and route["loadout"] not in LOADOUTS:
        problems.append("route: unknown loadout '%s'" % route["loadout"])
    # The two heroes a route is played with, once the party is three. The driver puts them out with a loadout and rejects a run whose save has another pair
    # out, so a name it can't match would fail every run of the route at its first step.
    pair = route.get("deployed")
    if isinstance(pair, list) and pair and all(isinstance(hero, str) for hero in pair):
        if len(pair) != PAIR or len(set(pair)) != PAIR:
            problems.append("route: 'deployed' names the %d heroes out on the field, each once, not %s" % (PAIR, json.dumps(pair)))
        for hero in pair:
            if hero not in heroes:
                problems.append("route: unknown hero '%s' in 'deployed' (no CharacterDef asset by that name)" % hero)
    if isinstance(route.get("difficulty"), str) and route["difficulty"] and route["difficulty"] not in DIFFICULTIES:
        problems.append("route: unknown difficulty '%s' (%s; leave it out for Standard)" % (route["difficulty"], ", ".join(sorted(DIFFICULTIES))))
    steps = route.get("steps")
    if not isinstance(steps, list) or not steps:
        problems.append("route: no steps")
        steps = []
    for index, step in enumerate(steps):
        if not isinstance(step, dict):
            problems.append("step %d is not an object" % index)
            continue
        where = "step %d (%s)" % (index, step.get("name", "unnamed"))
        keys(step, classes["QualityStep"], where, problems)
        if not isinstance(step.get("name"), str) or not step.get("name"):
            problems.append("%s: no name" % where)
        seconds = step.get("seconds", 1)
        if not fits(seconds, "float") or not 0 < seconds <= 180:
            problems.append("%s: seconds must be over 0 and at most 180" % where)
        scene(step.get("expectedScene"), where)
        condition(step.get("until"), where)
        condition(step.get("stopWhen"), where)
        stop(step.get("pickRow"), where)
        # The chart is a page of the pause menu, which stops the simulation: a step on it that doesn't say so is rejected after the run, as "simulation paused".
        if isinstance(step.get("pickRow"), str) and step["pickRow"].startswith(STOP) and step.get("pauseUi") != "pause":
            problems.append("%s: picks a stop on the chart without \"pauseUi\": \"pause\" (the chart is a page of the pause menu)" % where)
        if step.get("expectedUi") and step["expectedUi"] not in UI_STATES:
            problems.append("%s: unknown expectedUi '%s'" % (where, step["expectedUi"]))
        if step.get("pauseUi") and step["pauseUi"] not in PAUSE_UI:
            problems.append("%s: unknown pauseUi '%s' (bench, pause or defeat)" % (where, step["pauseUi"]))
        if step.get("expectedForm") and step["expectedForm"] not in FORMS:
            problems.append("%s: unknown expectedForm '%s'" % (where, step["expectedForm"]))
        for key in ("expectedCharacter", "ensureCharacter"):
            if step.get(key) and step[key] not in heroes:
                problems.append("%s: unknown %s '%s' (no CharacterDef asset by that name)" % (where, key, step[key]))
        for button in step.get("buttons") or []:
            if isinstance(button, str) and button.lower() not in BUTTONS:
                problems.append("%s: unknown button '%s'" % (where, button))
        # The driver's own hands. It cycles the quick item to an id and would press the d-pad at one the game doesn't have until the step ran out.
        item = step.get("useItem")
        if isinstance(item, str) and item and item != NO_ITEM and item not in consumables:
            problems.append("%s: unknown useItem '%s' (a ConsumableDef's id, or \"%s\" to keep the driver off the pouch)" % (where, item, NO_ITEM))
        ends = (step.get("until"), step.get("stopWhen"))
        stock = step.get("stock")
        if isinstance(stock, str) and stock:
            if stock != TO_CAP and not (stock.isdigit() and int(stock) > 0):
                problems.append("%s: stock '%s' should be \"%s\" or a count" % (where, stock, TO_CAP))
            row = step.get("pickRow")
            # A part's row is stocked to a count of that part (ContinuousReview.BeginPartStock), never to a cap: a part has none.
            if row in parts or row in parts.values():
                if stock == TO_CAP:
                    problems.append("%s: a part (%s) is stocked to a count of it, not \"%s\"" % (where, row, TO_CAP))
            elif row not in consumables and row not in consumables.values():
                problems.append("%s: a stock step's pickRow is a consumable's or a part's row (%s, or a TechPartDef's displayName), not %s" % (where, ", ".join(sorted(consumables.values())), json.dumps(row)))
            if "stocked" not in ends:
                problems.append("%s: a stock step ends on \"until\": \"stocked\" (or \"stopWhen\"): its presses stop there, and its seconds are only the most it may take" % where)
        elif "stocked" in ends:
            problems.append("%s: 'stocked' is never true on a step with no \"stock\"" % where)
        # A retry from a boss's door goes back to one step: the driver asks which by the boss's id.
        door = step.get("bossDoor")
        if isinstance(door, str) and door:
            if door not in bosses:
                problems.append("%s: unknown bossDoor '%s' (a boss EnemyDef's id: %s)" % (where, door, ", ".join(sorted(bosses)) or "none defined"))
            elif door in doors:
                problems.append("%s: %s already has its door at step %d: a retry picks the route up in one place" % (where, door, doors[door]))
            doors.setdefault(door, index)
    for index, segment in enumerate(route.get("segments") or []):
        if not isinstance(segment, dict):
            problems.append("segment %d is not an object" % index)
            continue
        where = "segment %d (%s)" % (index, segment.get("name", "unnamed"))
        keys(segment, classes["QualitySegment"], where, problems)
        first, count = segment.get("firstStep", 0), segment.get("stepCount", 0)
        if fits(first, "int") and fits(count, "int") and (first < 0 or count <= 0 or first + count > len(steps)):
            problems.append("%s: steps %s to %s are outside the route's %d" % (where, first, first + count - 1, len(steps)))
    return problems


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("routes", nargs="*", type=Path)
    parser.add_argument("--scenes", default="", help="comma-separated scene names to accept beside the ZoneDef assets (zones not defined yet)")
    parser.add_argument("--stops", default="", help="comma-separated ChartStop ids to accept beside the ones defined (stops not defined yet)")
    parser.add_argument("--heroes", default="", help="comma-separated hero ids to accept beside the CharacterDef assets (a hero whose definition Unity hasn't written yet)")
    args = parser.parse_args()
    extra = [s for s in args.scenes.split(",") if s]
    routes = args.routes or sorted(ROUTES.glob("*.json"))
    bad = 0
    for path in routes:
        try:
            problems = lint(json.loads(path.read_text(encoding="utf-8-sig")), extra, [s for s in args.stops.split(",") if s], [s for s in args.heroes.split(",") if s])
        except (OSError, ValueError) as error:
            problems = ["cannot be read as JSON: %s" % error]
        if problems:
            bad += 1
            print("%s: %d problem%s" % (path.name, len(problems), "" if len(problems) == 1 else "s"))
            for problem in problems[:40]:
                print("  " + problem)
            if len(problems) > 40:
                print("  ...and %d more" % (len(problems) - 40))
    if bad:
        print("ROUTE_LINT_FAILED %d of %d routes" % (bad, len(routes)))
        sys.exit(1)
    print("ROUTE_LINT_OK %d routes" % len(routes))


if __name__ == "__main__":
    main()
