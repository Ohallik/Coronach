# The Walking Canopy - a root island on the crossing

Functional brief, written with the blockout (November 11, world engineer B, Claude). **MAP_EYE_TEST: blockout PASSED with repairs (agent review, editor stills and plans); final in-player pass OPEN.** Chapter brief: [ch03-carrying-a-place.md](../chapters/ch03-carrying-a-place.md); script wiring: [ch03-manifest.md](../writing/ch03-manifest.md). Builder: `S/Editor/World/XylosBuilder.Canopy.cs`. Mechanics: [CHAPTER_GUIDE.md, "World mechanics"](../CHAPTER_GUIDE.md). Review status is at the end, and it is an agent's review: nobody else has looked.

## Purpose and formation

A root island: a raft of living root big enough to carry a grove, with halls inside it where the roots have grown round hollows. Islands like it are moored along the river (Old Senn lives on a small one). This one was torn off its mooring by the storm and walked, on the flood, onto the one stretch of the east channel a seed carrier can cross. It has tied itself there with its own mooring roots.

It is held in its shape by three **anchor roots**, each drawn taut from floor to roof. The zone's one rule: **cut an anchor and the island lurches, and the doors change.** A door here is a wall of root across a way through; when the island shifts, some draw back and others draw across. Nothing else opens a door.

Its growers kept a **seed vault** in it, dry and racked, and a **basin**: a round pond under the island with three flood gates on its rim that let the river in and out. Something came in on the storm and has taken the basin for its own.

## Users and access

Nobody is aboard. The growers who kept the vault left baskets and lanterns. The party comes in by the mooring roots from the Braided River (the east end of Seedfall) into the root hall, and leaves the same way.

## Layout

About 100 m square, with roughly 300 m of route, on two levels and two lower places. x runs east, z north. The **vault level** (the seed vault and the anchor chamber behind it) is at height 0. The **root hall** is the level below, at -2.8. The **flooding gallery** runs down the island's west side from -7 at its low end up to -3.6 at the second anchor's platform. The **basin** lies north of the hall, its floor a bowl from -5.2 at the rim to -6 in the middle.

```text
z 84   +---------------------+
       | ANCHOR CHAMBER      |      (vault level, 0)
       |   anchor C (3)      |
z 62   +...... racks ........+            . - ~ ~ ~ - .
  B's  | SEED VAULT          |window    .' gate N       '.
  platform  chest            |slot >   /   platform        \        THE BASIN  (floor -5.2 .. -6)
 +----+ back                 |        | platform   platform  |      34 m across; water at -5.0 while
 | B  |=ramp=>[door]         |        |  Floodcrown           |      three gates stand
 |(2) |      +---[door]------+ z 40    \ gate W      gate E  /=> drain
 +----+          | porch |              '.  platform S    .'
 | G  |          | ROOT  |                 ' - .[  ]. - '  z 35
 | A  | ledge    | LADDER|                      |seal|
 | L  |          | (stair)                      | passage (checkpoint talk)
 | L  | ledge    +-[door]-+----------------------+[door]+--+ z 23
 | E  |          |                                        |
 | R  | ledge    |   ROOT HALL  (-2.8)      anchor A (1)  | cradle
 | Y  |          |                                        |
 |    |=ramp=[door]   arrival           mooring roots >   |
 +----+ z 1      +----------------------------------------+ z -3
   x -43  -30   -20                                       24
```

| Area | What it is for | Size | Level | Combat |
|---|---|---|---|---|
| Root hall | The hub: the way in and out, anchor A in plain sight, the three doors, the cradle | 44 x 26 m | -2.8 | `Canopy_0`; `Canopy_Return` once B is cut |
| West ramp | Down from the hall's west door into the gallery's low end | 11 x 8 m | -2.8 to -7 | none |
| Flooding gallery | The pressure: a 12 m wide hall rising north, four dry ledges on its east side, the water climbing behind | 13 x 50 m | -7 to -3.6 | `Canopy_1`, `Canopy_2` |
| Anchor B's platform | The gallery's head, above the highest water; anchor B, and the back ramp up into the vault | 13 x 11 m | -3.6 | `Canopy_3` |
| Seed vault | The breather: racks, a chest, the window slot down into the basin | 24 x 22 m | 0 | none |
| Anchor chamber | Behind the vault's last row of racks: anchor C | 22 x 22 m | 0 | `Canopy_4` |
| Root ladder | The stair from the hall's north-west up to the vault's porch: the shortcut once B is cut | 8 x 17 m | -2.8 to 0 | none |
| Basin passage | The door checkpoint: down from the hall to the basin's south rim | 8 x 13 m | -2.8 to -5.2 | none |
| Basin | The Floodcrown's arena | 34 m across | -5.2 to -6 | `Floodcrown` |

## The three anchors and their door sets

| State | Gallery door (hall west) | Stair foot (hall north-west) | Vault doors (porch, and the back ramp's head) | Basin door (hall north) | What the party can do |
|---|---|---|---|---|---|
| Arrival | shut | **open** | shut | shut | Climb the stair to the vault's porch (a basket, the shut door) and come back. Cut A. |
| A cut (`xylos.anchor1`) | **open** | **shut** | shut | shut | Down the west ramp into the gallery; the water starts when they step into it. "The gallery's open, and the stair we wanted just shut." |
| B cut (`xylos.anchor2`) | open | **open** (the ladder is let down) | **open** | shut | The flood drains. Up the back ramp into the vault; on to anchor C; or down the ladder to the hall and its cradle. |
| C cut (`xylos.anchor3`) | open | open | open | **open** | Down the ladder, across the hall, into the basin passage. |

So the hall is crossed three times, each time with different doors: out west and down (A), back in from the north-west by the ladder (B), and out north to the basin (C). Each door is a solid wall of root with its own hole in the paths (a carving obstacle); a `FlagGate` on an anchor's flag switches it. Nothing is baked at runtime. A cut anchor is in the save, so the doors load as they were left.

**Travel** (walking pace 6.7 m/s; about 265 m of route, 40 seconds without a fight or the water):

| Leg | Metres | Seconds |
|---|---|---|
| arrival to anchor A | 13 | 2 |
| anchor A to the gallery door, and down the west ramp | 46 | 7 |
| the gallery, low end to anchor B (two fights; the waterline follows at 1.4 m/s) | 50 | 8 |
| anchor B, up the back ramp, through the vault to anchor C | 41 | 6 |
| anchor C back through the vault and down the ladder to the hall | 53 | 8 |
| the ladder's foot to the cradle and the basin door | 34 | 5 |
| basin door to the basin's middle | 28 | 4 |

## Route coordinates

Read from the built scene (`WalkingCanopy.unity`, November 12 rebuild) with the builder's constants beside them. x, z on the plan and the floor's height y, since the island has several levels: hall -2.8, gallery -7 rising to -3.6 (z 15 to 50), vault and anchor chamber 0, basin rim -5.2 to centre -6 (platform tops -4.7). Crossings start on walking through their box; everything else is pressed (A) within its range. Doors are walls of root switched by the anchors' flags (the door table above); a route must cut the anchors in order A, B, C.

| Kind | Id / name | x, y, z | Range or box | Flag it sets / needs | Notes |
|---|---|---|---|---|---|
| Spawn | `Arrival` | 2, -2.8, 2 | | | From the Braided River's mooring roots |
| Spawn | `Basin` | 19.5, -2.8, 18.5 | | | The door checkpoint: the cradle saves here, a lost Floodcrown fight returns here |
| Way out | `Mooring roots — The Braided River` | 11.5, -2.8, -1.4 | 4.4 | open | To BraidedRiver spawn `Canopy`; its sign at (11.5, 1.4) |
| Crossing | `Look round the root hall` (`SelaCanopyMooring`) | 2, -2.8, 7.6 | box 44 x 3 (x -20 to 24) | sets `xylos.canopyEntered` | Crossed walking north from the arrival |
| Switch | `Anchor root 1 cut point` | 8.6, -2.8, 13 | 3.2 | sets `xylos.anchor1` | Report `AnchorRoot`; opens the gallery door (-20.2, 6.2 to 13.8), shuts the stair foot (z 23.6, x -15.8 to -8.2). Then `SelaCanopyAnchor` (sets `xylos.anchorHeard`) once nothing fights |
| Hazard | `Gallery flood` | trigger 14 x 6 at (-36.5, -5.5, 14) | | starts on `xylos.anchor1`, drains for good on `xylos.anchor2` | Rises from -7.08 to -3.76 at 0.14 m/s once a hero walks into the trigger |
| Ledges | `Dry ledge 1..4` | -31.9, z 21.2 / 30.2 / 39.2 / 47.6 | | | Each 1.25 m over the gallery floor beside it; a caught party is set back on the last reached |
| Switch | `Anchor root 2 cut point` | -36.4, -3.6, 57.6 | 3.2 | sets `xylos.anchor2` | Opens the vault's front (z 39.6) and back (x -22.2) doors and the stair foot, lets the root ladder down, stops and drains the flood |
| Prompt | `Vault window slot` (`SelaSeedVault`) | 0.2, 0, 52 | 3 | sets `xylos.vaultSeen` | Shows the dormant Floodcrown (`previewPoint`) |
| Switch | `Anchor root 3 cut point` | -10.6, 0, 76.4 | 3.2 | sets `xylos.anchor3` | Opens the basin door (z 23.6, x 18.2 to 25.8) |
| Service | `Root cradle — repair and save` | 13.2, -2.8, 20.4 | | | Free; saves at `Basin` |
| Crossing | `Look at what holds the basin` (`SelaFloodcrownBasin`) | 22, -4.0, 28.6 | box 9 x 3 | sets `xylos.basinSeen` | In the passage down to the basin; saves at `Basin` |
| Boss door | `Floodcrown` | 24, -5.2, 52 | 36 x 34 | needs `xylos.basinSeen`; checkpoint `Basin` | Seal `Basin seal` across the passage at z 31.8 (x 17.6 to 26.4) while it fights; spawner (14.5, 53) facing east; `BossExit` (38.6, -5.3, 58) |
| Gates | `Flood gate north/west/east` | (24, 68.4), (8.6, 46.4), (39.4, 46.4), y -5.26 | | `xylos.gate.north/west/east` | `FloodGate` index 0, 1, 2 |
| Talk | `SelaFloodcrownGone` | anywhere | | after `bossdown.Floodcrown`, sets `xylos.floodcrownGone` | `AfterTalk`, 3 s once nothing fights |
| Cache | `xylos.canopy.porch` "Keepers' basket" | -9.6, 0, 37.4 | 2.6 | | On the porch at the stair's head |
| Cache | `xylos.canopy.gallery` "Drowned basket" | -41.4, -7, 3.6 | 2.6 | | The gallery's low end |
| Cache | `xylos.canopy.vault` "Seed vault chest" | -17.5, 0, 57.5 | 2.6 | | |
| Fight | `Canopy_0` | 0, -2.8, 14 | 30 x 12 | | 2 Mirefly Swarm, 1 Sporecaster |
| Fight | `Canopy_1` | -37.5, -6.0, 25 | 11 x 10 | | 3 Mirefly Swarm |
| Fight | `Canopy_2` | -37.5, -4.6, 40 | 11 x 10 | | 2 Sporecaster, 2 Mirefly Swarm |
| Fight | `Canopy_3` | -36.5, -3.6, 57 | 12 x 10 | | 1 Rootwarden, on anchor B's platform |
| Fight | `Canopy_4` | -11, 0, 74 | 20 x 16 | | 2 Rootwarden, 2 Mirefly Swarm, in the anchor chamber |
| Fight | `Canopy_Return` | -6, -2.8, 14 | 18 x 14 | needs `xylos.anchor2` | 2 Sporecaster, 1 Rootwarden, by the ladder's foot |

## The flooding gallery

Dry at rest but for its low end, a hand of water over knuckles of root (some stand out of it), with thick roots along its west and south wall feet. With A cut, the first step into it starts the flood: the water climbs from the low end at 0.14 m a second, which on this floor is a waterline walking north at about 1.4 m/s, a little slower than a wade. It stops just under anchor B's platform. Cutting B drains it for good.

- **Marked.** The floor the water is about to take, from where it is already deep enough to catch someone to 3.5 m past the waterline, is one lane in the house's danger orange, moving up the hall with the water.
- **Ledges.** Four dry shelves on the east side, each 1.25 m above the floor beside it, with a ramp up from the south, a pale cream plate on it and a seed lantern over it. The water reaches a shelf about nine seconds after the floor beside it.
- **Caught.** A hero the water closes over by 0.75 m loses 12% of integrity (never the last of it), and the party is stood on the last ledge it reached, with the water fallen back 1.3 m below that ledge and 2.5 seconds before it climbs again. Nobody dies of the water and nothing is restarted.
- The water slows whatever wades it, enemies too (the Mirefly Swarms fly over it).

## The basin (for the combat engineer)

All positions are world x, z; heights are world y.

| Thing | Where | Notes |
|---|---|---|
| Centre, radius | **(24, 52)**, **17 m** | Floor is a bowl: y = -6 + 0.8 (r/17)^2, so -6 at the centre and -5.2 at the rim |
| Way in | Passage x 18 to 26, from the hall (z 23, y -2.8) down to the south rim (z 35, y -5.2) | The seal (`Basin seal`, a Membrane with roots drawn across) is at z 31.8 |
| Rim | South and west: low (1.5 m at the edge, rising away from the floor under the camera's sightline). North and east: the island's wall, 8 m | West of the west rim, 5 m back, is the vault's window |
| Platforms (dry while full) | Bearings 30, 150, 270 at 9.5 m: **(32.2, 56.75), (15.8, 56.75), (24, 42.5)** | Each a mound: flat top radius 2.4 m at y -4.7, sides falling at 1 in 2 to the bowl. Walkable from any side |
| Flood gates | Bearings 90, 200, 340 at 16.4 m: north **(24, 68.4)**, west **(8.6, 46.4)**, east **(39.4, 46.4)** | `FloodGate`, index 0, 1, 2: north, then west, then east, which is anticlockwise round the rim as the camera sees it, so the next gate is always the next one round; integrity 2400; hurtbox a sphere radius 1.7 at 1.6 m; flags `xylos.gate.north/west/east`; settled by `bossdown.Floodcrown` |
| Water | Full **-5.0**; empty **-6.15** | `FloodBasin` on object `Basin flood`: the arena's `IArenaWater` (`ArenaWater.Current`). Follows `FloodGate.Held`: -5.0, -5.38, -5.77, -6.15 with 3, 2, 1, 0 gates standing, 2.6 s a third |
| What is under water | Full: everything but the platforms (depth 0.2 at the rim to 1.0 at the centre). Two gates: inside r 15. One gate: inside r 9.1. None: dry | Wading is the water's own rule (`WaterVolume`, pace 0.62 once feet are 0.12 under) |
| Drain | East wall at (39.6, 58.4), north of the east gate: a low mouth at floor level | Where the Floodcrown goes: its `BossExit` (boss `Floodcrown`, `sinks`) is on the floor in front of it at **(38.6, 58)** |
| Boss | Spawner at **(14.5, 53)**, facing east, shown and inert before the fight | `previewPoint` is the vault window (0.2, 52), radius 5 |
| Encounter | `Floodcrown`, trigger 36 x 34 m on the centre, needs `xylos.basinSeen` (the talk in the passage), checkpoint spawn `Basin` (19.5, 18.5, in the hall) | Cradle `Root cradle` at (13.2, 20.4) |

The boss drives the water through `ArenaWater.SetLevel(fraction, seconds)` if a move wants it; the next gate to break takes the level back to what the gates hold. `FloodGate.Expose(false/true)` shuts the gates to blows and opens them.

## Identities

Scene `WalkingCanopy` (ZoneKind GroundCombat, profile Cave, no sky, threat 1.1). Music: Riverbed Stomp (ZoneDef). Ambience: `AMB_RootHall` (made by `tools/water_sounds.py`). Light: `CanopyLooks` in the builder (root-filtered green-gold, lifted so it is never murky; seed lanterns are the warm thing in each room).

| Kind | Ids |
|---|---|
| Spawns | `Arrival` (2, 2), `Basin` (19.5, 18.5) |
| Way out | `Mooring roots — The Braided River` (11.5, -1.4) to spawn `Canopy` |
| Anchors | Points `Anchor root 1 cut point` (8.6, 13), `2` (-36.4, 57.6), `3` (-10.6, 76.4): prompt "Cut the anchor root", report `AnchorRoot`, flags `xylos.anchor1..3`; B and C bark `anchor`. `ShiftAnchor` on each root; log `ANCHOR_CUT <flag>` |
| Talks | `SelaCanopyMooring` crossing at (2, 7.6) (`xylos.canopyEntered`); `SelaCanopyAnchor` after A's lurch (`xylos.anchorHeard`); `SelaSeedVault` at the window slot (0.2, 52) (`xylos.vaultSeen`); `SelaFloodcrownBasin` crossing at (22, 28.6) (`xylos.basinSeen`); `SelaFloodcrownGone` after `bossdown.Floodcrown` (sets `xylos.floodcrownGone`) |
| Fights | `Canopy_0` (0, 14), `Canopy_1` (-37.5, 25), `Canopy_2` (-37.5, 40), `Canopy_3` (-36.5, 57), `Canopy_4` (-11, 74), `Canopy_Return` (-6, 14; needs `xylos.anchor2`), `Floodcrown` |
| Water | `Gallery water` (rest -7.08, top -3.76; `RisingWater` on `Gallery flood`), `Basin water` |
| Service | `Root cradle — repair and save` (free, spawn `Basin`) |
| Caches | `xylos.canopy.porch` (-9.6, 37.4), `xylos.canopy.gallery` (-41.4, 3.6), `xylos.canopy.vault` (-17.5, 57.5) |

## Encounter plan

| Fight | Where | Pack | Why here |
|---|---|---|---|
| `Canopy_0` | Root hall | 2 Mirefly Swarm, 1 Sporecaster | The island's own vermin, in the first room, with room to fight |
| `Canopy_1` | Gallery, first third | 3 Mirefly Swarm | Something to stop for while the water comes |
| `Canopy_2` | Gallery, second third | 2 Sporecaster, 2 Mirefly Swarm | Casters that keep their distance up the hall, flies in the way |
| `Canopy_3` | Anchor B's platform | 1 Rootwarden | A guardian at the thing to cut, on dry ground |
| `Canopy_4` | Anchor chamber | 2 Rootwarden, 2 Mirefly Swarm | The last anchor is the best guarded |
| `Canopy_Return` | Root hall, by the ladder's foot | 2 Sporecaster, 1 Rootwarden | The hall is not the same room the second time through |
| `Floodcrown` | The basin | The Floodcrown | Boss |

## Camera

The fixed ground camera looks north-north-east from above. Every floor here has its high ground on its north and east; root mass between the camera and a floor stays under the line of sight to a hero standing at that floor's edge, measured from the lower of the two levels (the rule Hushwell was built to, D119), and climbs away at a pitch nobody can stand on. So the hall's back wall is the vault's retaining face, the gallery's tall side is its east wall with the ledges against it, and the basin is entered from the south with its low rim behind the party and its wall and the far gate in front. Root columns, hanging roots and lanterns stand only on back walls. The two trunks in the hall are the exception that the rule allows for: they stand in its back third, each is its own piece, and the view opens a hole in the one a hero is behind.

## What says "root island"

- **The floor is root woven flat**, brown in the hall, wet green down the gallery, paler in the vault, silt in the basin; drifts of leaf, moss and damp lie over it, and daylight comes down through the roof in broken pieces that sway.
- **Roots run out over the floor from each anchor**, half buried: every line in a room leads to the thing that holds the island there, which is the thing to cut.
- **Every back wall is root**: columns from floor to roof with creeper on them, an arch over each way through, a door that is a wall of root tied shut.
- **Seed lanterns** are the warm thing in each room: one at each ledge of the gallery, one on each platform of the basin, one over each anchor's knot.

## Review

Reviewer: Claude (world engineer B, chapter 3), from editor stills and plans, November 11 and 12. Nobody else has looked, and nothing here has been walked in a player. **MAP_EYE_TEST: blockout PASSED with repairs; final in-player pass OPEN.**

### Blockout

Evidence: 21 grey stills in `Builds/quality/look/ch3b-blockout-01/` (`WalkingCanopy-*`), plans in `Builds/quality/chapter3/canopy-blockout-01/` (`WalkingCanopy-plan`, `-basin`).

| Check | What the stills showed |
|---|---|
| Purpose and adjacency | Passed. The hall is a hub with the first anchor in sight of the way in; the gallery runs up to the second; the vault is above and behind the hall with the third behind its racks; the basin is the lowest place and is seen from the vault before it is entered. |
| Circulation and scale | Passed on the plan: the hall is crossed three times with different doors each time. The navigation baked with 31 must-reach points, every door open. Not walked. |
| Construction and enclosure | Passed. Four levels on one ground; rock between the camera and a floor stays under the sightline to it, so no room was hidden by its own south wall. |
| Terrain and arenas | Passed. The basin is the island's own pond with gates on its rim; draining it by thirds is what its gates are for. |
| Immediate visual understanding | Passed for the rooms, with three faults repaired before dressing: the floor was one loud yellow weave; the gallery's resting water lay as a pale slab; the basin's drain was drawn on top of its east gate (moved north of it). |

### Dressed (editor)

Evidence: 21 stills in `Builds/quality/look/ch3b-final/` (`WalkingCanopy-*`; local biomes; the same views without them are in `ch3b-dress-10-public/`), plans in `Builds/quality/chapter3/canopy-final/`. `python tools/look_density.py Builds/quality/look/ch3b-final --match WalkingCanopy`: every play frame passes (worst `gallery-mid` and `vault-window` 0.13, `arrival` 0.12, `basin-south` 0.10). The whole-island overview measures 0.29: it is the island's mass seen from far off, not a view anyone plays in.

| Check | What the stills show |
|---|---|
| Purpose and adjacency | Passes. An anchor is the thing every root on a room's floor runs to; a door is a wall of root under an arch; the vault has racks and a slot that looks down on the basin. |
| Construction and enclosure | Passes. Walls are columns of root with roots lying along their tops; nothing stands or lies between the camera and a floor (a column there is cut under the sightline or left out). |
| Immediate visual understanding | Passes. Woven-root floor with moss, drifts of leaf and lantern pools; the gallery is wet and green with pale plates on its ledges; the basin is dark water with pads and three lit platforms. |

What was found and repaired while dressing: the damp layer lay on every floor as black holes (its texture is dark and it had been tinted down as well); root columns on the wall south of the back ramp hid the party on the ramp (the sightline rule now applies to what stands on a wall as well as to the wall); the gallery's east wall and the island's tops were plain dark faces (roots now lie along them, and the root's material keeps more light in shade).

What is still weak:

- **The floor may read as matting rather than root.** I take it for woven root because the anchors' runners and the walls say root; a second pair of eyes may not.
- **The hall is large for what happens in it** (44 x 26 m, two fights). Its two trunks and the anchor's runners give it a middle, and it is crossed three times, but it has not been timed in play.
- **The rising water has not been seen rising.** Its marking, the ledges' plates and the duckweed riding up with it are built and tested in code only (`XylosMechanicsTests`, not yet run).
- The leaf light that moves over the floor is a runtime effect and is not in an editor still.
- The sounds are synthesised and unheard.

### Dressed (editor), November 12 rebuild

Taken over by a second engineer (Claude) and rebuilt. Changed: the gallery's low end is knuckles of root with standing water in the dips (it was a flat film that read as a rectangular pool), with roots along its west and south wall feet; its east wall, behind the ledges, has roots and creeper down it; the island's walls take the bark at a finer grain (a separate material, `xylos-canopy-wall`); the flood gates' panels are drawn on both faces (the west gate read as an empty frame); the water sheets are drawn at 0.4 and 0.35 m so a falling waterline is a curve; the water's glint is small and faint (it lay on the basin as grey camouflage). Evidence: stills in `Builds/quality/look/rc-07/` and `rc-07-public/`; the basin at one gate standing with the Floodcrown's body stood at its spawner in `rc-02-stills/` (`-xylos-stills -xylos-basin 0.34`, never kept); plans in `Builds/quality/chapter3/canopy-rc-06/`. `python tools/look_density.py Builds/quality/look/rc-07 --match WalkingCanopy`: every play still at or under 0.22 (`basin-east-gate` 0.22); the two overviews are 0.31 and 0.36 (the basin's water from above).

| Check | What the stills show |
|---|---|
| The basin from the fixed camera | Its three gates read as framed panels in the rim (north straight ahead, west and east at the sides), the platforms as lit mounds, the water dark with pads. At one gate standing (`rc-02-stills/WalkingCanopy-basin-*.png`) the water has drawn back to the middle and the platforms' flanks and the bowl's rising floor stand out of it; the Floodcrown (dark blue, gold antlers) reads against both. |
| The gallery | A flooded root floor with dry knuckles and roots, the ledges' cream rings plain against it. |

Still weak: the exposed basin floor and the hall are the same woven floor, which reads as large square tiles; the gallery's puddles have a bright rim that looks pasted on; the walls are still the plainest thing in most frames; only one water level (one gate) has been rendered, not two; nothing has been seen in a player.

### Finishing pass, November 12 afternoon

- **A wet edge, not a rim.** The water's edge is thin, darker than the water and broken up, with no lap line, and shallow water is dark (`_FoamParams` (0.035, 0.9, 0.03, 0), `_FoamColor` (0.15, 0.21, 0.17)). The gallery's puddles and the basin's edge were a pale mint band, the brightest thing in the frame. The indoor glint is all but gone (0.018): thresholded noise at any grain lay on the basin's dark water as pale blots.
- **Every water level of the fight rendered**, with the Floodcrown stood at its spawner (`-xylos-stills -xylos-basin <share>`, never kept): `Builds/quality/look/rc-20-full/`, `rc-20-two/`, `rc-20-one/`, `rc-20-dry/` (`WalkingCanopy-basin*`, `-overview-basin`). Full, the water is dark with pads and the three platforms stand out of it; with one and two gates down it draws back to the bowl's middle and the platforms' flanks and the rising floor stand clear; drained, the bowl is damp floor with the pads lying on it. No still can show a danger marking (they are made in play), so this is by draw order: the water (Transparent-30) and the pads (Transparent-25) write no depth and draw before every marking (Transparent-10), and no root lies on the basin's floor (the wall ribs keep to the faces and 2.5 m off each gate, the runners to the hall and the anchors).
- **The walls:** roots run up every face the camera looks at, from the floor's edge to as much as four metres up (the basin's east wall all the way between the east gate and the drain), with creeper hung from them; daylight comes through nine gaps at the walls' tops as slanting shafts. The island's mooring roots run in across the arrival.
- **The floor:** the weave is fainter (0.46) and the litter and damp lie in smaller drifts over it, so no stretch of bare weave is wide enough to read as a grid of tiles.
- **The vault's last row of racks** stands at x -17.2, -11.2 and -5.2 (z 60.9), with gaps of 2.9 m between: at 1.5 m the route writer's walk through to the anchor chamber brushed a rack.
- **The Floodcrown's after-talk after a load** plays. A save made between the anchors and the win, then Continue, plays any after-talk still owed first (the first anchor's, if the party never stood still after cutting it), and the Floodcrown's two seconds after; the chapter test waited for the first conversation and asserted, and now waits for that talk by name.
