# MAP_EYE_TEST — Arena_Ground and Arena_Flight, blockout and final build

2026-09-29, Claude Code. Briefs: [Arena_Ground](../maps/Arena_Ground.md) and [Arena_Flight](../maps/Arena_Flight.md). The originals were written by Codex on September 26; the redesign plans are recorded in each brief.

**Blockout PASS for both (agent review). Final in-player pass: PASS for both** (ordinary virtual-gamepad routes, full focus, September 30; see below).

## What changed

`ArenaRedesign` (isolated; `GroundBlockout/Final`, `FlightBlockout/Final`) replaces the practice squares with places. Both keep these contracts:

- `Arrival` at (0, 0/1, −4);
- the three z = 5 opponent spawns;
- Hal's `ArenaGuide` node;
- the working core the tests fly and fight across. The core is |x| < 10, −18 < z < 10; the old arena's rocks began at |x| ≈ 11.

**Proving yard.**

- The yard is graded against a west rock outcrop and a north rock backstop. An obstacle and target area (crate stacks, a low cover ridge, rubble, a drill bit) stands in front of the backstop.
- An installed deck-panel barrier runs along the east side.
- A low crate barrier runs along the camera side, with a service entrance leading to a landing pad and a parked skiff.
- The operator's sheltered repair pad (hab, console, bench, crate fence) sits in the south-east corner. Hal stands at its open corner, out of the firing path.
- Collision boxes sit on those visible edges. A 110 m outpost apron continues past them, so the yard is no longer a floating tray.

**Proving berth.**

- Posts rise from a lower dock deck 13 m below the flight plane.
- Rails at the flight plane are the collision boundary on every side, including a railed entry lane closed at its outer end.
- A generated docking pod on posts outside the west rail is the control cabin. Hal speaks from it through a lit call pad at the berth edge, with his standing body hidden.
- Vorun lies far below. The old membrane walls, organic floor and floating rocks are gone.

## Checks, red before green

`ArenaLayoutTests`, two play tests:

- **Yard.** The core is free of static scenery at body height, and Hal is outside the firing path. The three opponent posts exist. The west, north, east and south edges each stop a walker.
- **Berth.** The core is clear at the flight plane, and every boundary ray meets the berth's visible rails or posts. Hal's body is hidden and his call point is at the control cabin.

| Run | Result | Meaning |
|---|---|---|
| Old arenas | 0/2 | Red: `RidgeRock2` and `CrystalClusterB` inside the working core in both scenes |
| First final build | 0/2 | The south crate line intruded into the core box: the edge strip is now excluded, and the box still contains the old crystal. A north post stood in front of the rail: posts are visible berth structure too, so both names are accepted |
| Final | **2/2** | Accepted |

Full suites on the rebuilt arenas: **56/56 EditMode and 173/173 PlayMode**, zero skips. Every combat, motion, audio and flight test that uses the arenas still passes.

## Opened views

`Builds/quality/workshop/arena-blockout-01` and `arena-final-01` were opened. Each has an overhead; the flight berth also has a section. Perspective renders use each form's gameplay camera (ground 40°/20°/19.5 m, flight 48°/0°/26 m, 30° field of view) at the arrival, operator, targets, each boundary and the service entrance or entry lane.

- **Ground blockout.** The overhead reads as the intended yard: outcrop west, backstop and targets north, barrier east, the operator pad south-east, and the crate line and entrance south, with the pad and skiff beyond.
- **Ground final.**
  - The operator corner reads as a working repair station: hab, console and crates, with Hal at its opening. The east barrier reads as installed panels, and the crate barrier as a low service fence.
  - Weakness: the large generated rocks read as a single cliff mass.
  - The low cover ridge first stood end-on to the camera and read as a pillar. It was turned across the view.
- **Flight blockout.** The section locates the cabin on its posts, the rails at the flight plane and the lower deck. The blockout's single cyan material hides everything else, so the final review carries the reading.
- **Flight final.**
  - The berth reads as a railed dock frame on posts over a deck far below. Vorun shows past the lane's end, and the cabin stands outside the west rail with the call pad inside.
  - Weakness in `arena-final-01`: the lower deck was the dominant surface in every view and read as dockyard floor under the flight plane, not a distant backdrop.
  - Fixed in `arena-final-02` (opened): the solid deck is replaced by an open gantry, strips under both rails and the centre crossed at the ends and middle. Its bays show space and Vorun far below, so the berth now reads as hanging in orbit. The arena-dependent classes pass 33/33.

## Open

- **Ordinary in-player passes**, awaiting a quiet machine. Needed on the yard: the service edge and opposing corners. Needed on the berth: boosting, braking, roll, lunge, both end boundaries and the call to Hal.
- **Routes.** The flight lunge routes stay inside the core. The ground motion route drives eight directions from the arrival and may now meet the operator fence earlier than the old rocks; it will be rechecked in that pass.

## Ordinary in-player passes (September 30)

Release player rebuilt from the integrated source; each capture ran alone.

**Proving yard, `arena-yard-walk`.** The route fights the three opponents with ordinary melee, a skill and a gel, then walks the service entrance and lane to the landing pad, all four corners, the east barrier and the operator corner beside Hal.

- `arena-yard-walk-01` was **rejected**, and the rejection found a layout defect. The low cover ridge stood 4 m in front of the backstop, whose rock mass reaches into the yard. That left a pinch barely wider than a hero, and Taren was trapped in it for the rest of the route (opened stills). The drill bit also blocked the west walkway.
- The target props now stand between the opponents' posts and the backstop at z 12–13.5, leaving a clear lane along the rock at z≈17 and a clear walkway beside the outcrop. `ArenaLayoutTests` still pass.
- `-02` missed only a west waypoint set inside the outcrop's real edge; it was moved onto the edge.
- `arena-yard-walk-03`: **PASS**, 92.9 s, 5,577/5,577 frames focused, both heroes at full health.

**Proving berth, `arena-berth-flight`.** The route clears the targets with ordinary fire, then boosts, brakes, contacts the north rail, rolls, holds at Hal's call pad, flies the railed lane and touches its closed end.

- `-01` was rejected: one leg's time budget was too short for braked navigation.
- `-02` passed, but its rail approaches stopped short of contact.
- `arena-berth-flight-03`: **PASS**, 110.7 s, 6,645/6,645 focused.
  - North-rail contact by Sela's hull at z 26.05, and lane-end contacts by both hulls at z≈−38. Both are logged as `WALL_CONTACT` on the rail.
  - "Call control — Hal" was the live prompt for 484 frames at the call pad.
- The ordinary lunge routes `c4-lunge-{taren,sela}-ordinary` also pass in the new berth (48.3 s and 48.7 s, full focus).

**`c0-motion-arena_ground` was rejected, and it is a fixture problem, not a yard problem.** This eight-direction motion route runs in the live combat yard with an 8-second settle, during which the Ridgehounds attack the idle party. Taren began at 12 of 120 health and went down, forcing a swap; the September C0 baseline had also begun at 30 health and survived by chance. Motion measurement belongs in a quiet scene or with opponents held, which is a C2 fixture decision. The route was left unchanged, and the yard's pass rests on `arena-yard-walk`.
