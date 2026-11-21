# Rootmarket - a river town on root platforms

2026-11-11, revised 2026-11-12 (second engineer on the town: townsfolk and props off the ways, a check over everything solid, decks, lily pads, two bodies for the townsfolk). Reviewer: Claude (world engineers A and B, chapter 3). Builder: `XylosBuilder.Rootmarket.cs` on `ZoneKit`. Chapter brief: [ch03-carrying-a-place.md](../chapters/ch03-carrying-a-place.md). Script wiring: [ch03-manifest.md](../writing/ch03-manifest.md). **MAP_EYE_TEST: blockout PASSED (agent review, editor stills and plans); final in-player pass OPEN** - nothing here has been walked in a player.

## What the place is for

The Okhari growers' town on the Xylos delta: where seed is kept between floods, where the carriers load, where everyone eats and everything is voted on. It is not built on a bank, because the bank moves. It is platforms of packed earth held in woven root, standing in slack river water round a pool the town's boats (and a visitor's ship) come down to, and what can't be a platform is tied to one.

Users: growers and seed-house hands, the carrier captain and his crews, the cook and whoever she is feeding, the mender, the ferry's pole-hand and his queue, one old man who is not leaving, and two Shapers who came to buy a harvest and are handed a rope.

## Rules of construction

- **A platform is an oval of floor with a rim.** Every edge of every platform has a knee-high rim of woven root, steep on both faces (the landform's own collision, drawn in bark): it is what holds the earth in, and it is why nobody walks off into the water. There is no invisible wall anywhere in the town. A rim is left out only where something built crosses: the two ends of the raft crossing and the two ends of the rope walk.
- **Everything hangs off the ring.** The ring of floor round the pool is the street. The landing, the mooring, the ferry stage, the seed house's forecourt, the kitchen and the press are each a lobe grown off it; Senn's island is the one platform that touches nothing and is reached by a slung walk of boards.
- **Two ways across.** The lower way is the ferry's rafts lashed end to end across the pool since the storm, railed with rope. The upper way is a root ridge that leaves the ring's north-west, climbs 2.4 m, passes behind the seed house over the lagoon and comes down into the kitchen: dry in any flood.
- **Tall things stand at the back** (D119, D191). The fixed camera looks north-north-east from above and never sees higher than itself, so the seed house is at the north of the ring, the jungle's bank and trunks are along the north and east, and the south and west are open water with only knees of root, reeds and lily pads. No crown hangs over floor: the trees in the lagoon are far enough south of the upper walk (`ZoneKit.Shadowed`) that nobody walking it is hidden.
- The river leaves east through a gap in the bank, which is where the river gate is.

## Plan

North up, metres. `~` water, `=` built deck, `#` the bank the jungle stands on.

```text
 z
110 ################################################################################ #####
100 ~~~~~~~~~~~~~~~~~~~~~~~~.-- upper dry walk (ridge, +2.4 m) --.~~~~~~~~~~~~~~~~~~ #####
 90 ~~~~~~~~~~~~~~~~~~.-----'   cache          lagoon             '----.~~~~~~~~~~~~ #####
 80 ~~~~~~~~~~~~~~~~~/~~~~~~~  [ SEED HOUSE ]  (door south:        ~~~~\~~~~~~~~~~~~ #####
 70 ~~~~~~~~~~~~~~~~|~~~~~~~    notice, Ruva*)        BINNA'S KITCHEN ~|~~~~~~~~~~~~ #####
 60 ~~~~~~~~~~~~~~~~ \  .----- ring ------.  stalls, pit, tables ------'~~~~~~~~~~~~ #####
 50 ~~~~~~ FERRY    .-'   .------------.   '-.     THE PRESS                         ~ gap
 44 ~~~~~~ STAGE Jask ====|= raft crossing =|====  Hadda, press, cradle ---- RIVER GATE ->
 40 ~~~~~~ queue    '-.   '---- pool --'   .-'                                       ~ gap
 30 ~~~~~~~~~~~~~~~~~~ '-.              .-'    MOORING: Ruva, crew, the rope         #####
 20 ~~~~~~~ rope walk =/  LANDING: pad, Teo*   '--- (the island is hauled in  ~~~~~~ #####
 10 ~ SENN'S ISLAND ==/   ARRIVAL                    from the south-east)     ~~~~~~ #####
  0 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ #####
   x -60      -40       -20        0        20        40        60        70
```

`*` Ruva stands at the mooring until the party has been out on the river, and at the seed house's door from then on (the vote is held there). Teo arrives by the landing once the terms are heard, is away at Seedfall from the Seedthane's fall to the vote, and is back after it. Jask leaves the town ferry stage when the party first goes out on the river; after the vote his pole is there and he isn't. Each is one `Npc` per place with a `Whereabouts`, so there is only ever one of anyone.

## Functional areas and how they connect

| Area | Where | What is there | Why there |
|---|---|---|---|
| Landing | south of the pool, (0, 20) | the pad (`Launch — Xylos Nearspace`), the arrival, Teo and his trays, a lantern | a ship comes down to the pool; the first thing a visitor sees is the pool and the ring going both ways |
| Mooring | south-east, (26, 23) | two mooring posts, the island on its lines, the hauling crew, Ruva, the line that's short a back | the open water to the south-east is where a drifting island can be brought in |
| Ferry stage | west, (-30, 44) | the stage head and its post, the ferry (a small seed pod of a boat) and a canoe, the queue up the ring's inner side, Jask | the west end of the raft crossing |
| Raft crossing | across the pool at z 44 | five rafts lashed end to end, rope rails | the lower way between the stage and the press |
| Seed house | north, (0, 78) | the hall, its door and lantern, the Compact's notice, trays and hands, a child after a pod | the tallest thing, at the back; the forecourt is where the town meets |
| Binna's kitchen | north-east, (26, 65) | two stalls under leaf awnings, the cooking pit, four tables | beside the seed house and at the foot of the upper walk: everyone passes it |
| The press | east, (37, 43) | the screw press (`Fabricate — The Press`), the mender's cradle (`repair and save`), a drying rack, Hadda | the east end of the raft crossing and the way to the river gate: the last stop before the river |
| River gate | far east, (60, 43) | a root arch on a causeway into the gap in the bank; `River gate — The Braided River` written on the causeway four metres short of it, at (56.6, 45), and the floor sign RIVER GATE at (51.5, 41.2) | where the river leaves |
| Senn's island | south-west, (-46, 13) | his hut, his seat, the post he carved, an old mooring box | apart, on purpose: he isn't going |
| Upper dry walk | north, round behind the seed house | a ridge with its own rims, a cache at its top | the way that stays dry; from it the lagoon and the back of the seed house |

Services: Binna sells provisions from her conversation (`Rootmarket_Binna`); Hadda offers the press, the cradle and her parts (`Rootmarket_Hadda`) from hers; the press and the cradle also have prompts of their own, a few steps apart, so neither needs her to be asked twice.

## Circulation and travel

Walking pace is 6.7 m/s. Lengths are the straight legs the builder keeps clear (`RootWays`). Nothing solid may stand nearer a way's line than .9 m at its edge (the bake's .55 m agent and a cell), nor anyone with no name nearer than 2 m, nor a walker's loop nearer than 1.6 m: `NothingStandsOnTheWays` reads the finished scene before the bake and refuses the build by name, whatever placed the thing. The way to Senn's island enters the rope walk by its mouth, (-21.5, 28.6) then (-23.4, 28.4); cutting that corner ran across the end of the walk's south rail.

| Leg | Metres | Seconds |
|---|---|---|
| arrival to the rope (the crossing that starts `RuvaArrival` is on the way, at x 14) | 36 | 5 |
| mooring back to Teo by the landing | 43 | 6 |
| Teo to Jask at the ferry stage | 34 | 5 |
| Jask to the seed house door | 42 | 6 |
| seed house to Binna | 23 | 3 |
| Binna to Hadda | 28 | 4 |
| Hadda to the river gate | 25 | 4 |
| the raft crossing, end to end | 40 | 6 |
| the ring to Senn, by the rope walk | 32 | 5 |
| the upper walk, foot to foot | 110 | 16 |

The first circuit (arrival, the rope, the terms, then clockwise round to the gate) is 231 m of walking, about 35 seconds on foot; with the arrival scene, three pulls on the rope, the terms and a first word with each of six people it is the brief's four to five minutes. The circuit is a ring, so nothing has to be walked twice: the raft crossing and the upper walk are both shortcuts across it.

## Sightlines

- **Arrival.** The party stands on the landing with the pad and the pool ahead of it and the ring leading off both ways. The seed house is 60 m north and not in the first frame; the commotion at the mooring is 30 m east, and the objective names it.
- **The mooring.** Ruva faces the island; the crew stand south of the line a visitor walks, hauling toward the south-east; the island is in frame beyond the rim from the moment the rope is.
- **The seed house** is 10.6 m tall with its door to the south, so everyone who talks at it stands in front of it and it is never between them and the camera. A hero within about 11 m north of it would be behind it; there is no floor there.
- **The upper walk** is 22 m behind the seed house at its nearest, so the roof never comes under the lens for someone on it. The bank and the feet of the jungle fill the top of the frame from up there.
- Nothing taller than a person stands on the south or west side of any floor.

## Evidence (November 12)

Rebuilt nine times in the editor (`XylosBuilder.BuildZones -xylos-zones Rootmarket`, then `ZoneLook.Apply`), each followed by stills through the game's own ground camera (`LookDev.Render`, 19 views) and looked at as contact sheets.

- **Plan:** `Builds/quality/chapter3/rootmarket-b03/Rootmarket-overhead.png` and `Rootmarket-pool.png` (`MapEvidence.Zone`; `rootmarket-03` is the first build's). The ring, the pool and its raft crossing, the six lobes, the upper walk's arc behind the seed house, Senn's island on its rope walk and the gap in the east bank all read from above without labels.
- **Stills:** `Builds/quality/look/xylos-b-05/Rootmarket-*.png` (public water, the current build; `xylos-b-03` and `-04` are the same less the gate's written name and the last lantern post and tray stack moved), `xylos-b-03-local/Rootmarket-*.png` (after `LocalBiomeBake.BuildPatches -biome-zones Rootmarket`: the licensed water through the `BiomePatch` named `Rootmarket-river`; both paths draw; not rendered again after those three small changes). `xylos-a-01` to `-06` are the first engineer's passes.
- **Build log** (`xylos-b-05`): landform 277 x 245 points, the drawn floor 60,654 vertices with a tooth share of .005 (the rims are drawn as roots, `XYLOS_ROOT_RIMS runs=8 metres=703`; the collider keeps them); `XYLOS_WAYS_CLEAR Rootmarket`; navigation `ZONE_ROUTE_BAKE_OK Rootmarket points=20`; ground kit `layers=3 details=219 standing=263 batches=132`; `XYLOS_MESHES_OK Rootmarket`.

**Density** (`python tools/look_density.py Builds/quality/look/xylos-b-05 --match Rootmarket`; largest plain area, limit .22): worst **.19**, 19 of 19 under the limit, and the same worst, **.19**, with the licensed water (`xylos-b-03-local`).

| View | Public | Licensed | View | Public | Licensed | View | Public | Licensed |
|---|---|---|---|---|---|---|---|---|
| arrival | .16 | .16 | seed-house | .12 | .12 | rope-walk | .06 | .16 |
| mooring | .09 | .11 | seed-house-door | .19 | .19 | senn | .01 | .16 |
| landing-west | .11 | .11 | kitchen | .17 | .14 | upper-walk-west | .02 | .09 |
| ferry-stage | .08 | .08 | press | .16 | .16 | upper-walk-top | .13 | .13 |
| raft-crossing | .14 | .16 | river-gate | .03 | .10 | upper-walk-east | .06 | .17 |
| ring-northwest | .02 | .04 | ring-southeast | .14 | .14 | overview / overview-north | .03 / .01 | .04 / .02 |

The licensed water is plainer than ours: under it the lagoon east of the upper walk measured .29 (`xylos-b-02-local`) until a second, thicker scatter of pads and reeds was laid within ten metres of the walk on its own draws.

**Places** (plan x, z; the builder's constants, logged as `XYLOS_COORDS Rootmarket`). Routes and tests read these.

| Place | At | Place | At |
|---|---|---|---|
| spawn `Arrival` | (0, 17) | the pad (`Launch — Xylos Nearspace`) | (0, 24.2) |
| the crossing that starts `RuvaArrival` | (14, 24), 3 x 22 m | the mooring line (`Haul the mooring line`) | (32.4, 18.8) |
| Ruva at the mooring | (28.5, 21) | Ruva at the seed house | (5.5, 68.5) |
| Teo | (-8.5, 24.6) | Jask / his pole after the vote | (-25, 47.6) / (-22.3, 46.4) |
| the Compact notice / the seed house door | (-2.7, 72.3) / (2.5, 72.3) | Binna | (24.6, 68) |
| Hadda / the press / the cradle | (35.4, 46.6) / (41, 48.6) / (41.6, 40.4) | spawn `Press` (the cradle's save) | (34, 41.2) |
| river gate / spawn `River` | (60.2, 43) / (54, 43) | Senn | (-46.4, 13.6) |
| growers' cache (upper walk) / old mooring box | (0, 102.3) / (-51.2, 11) | Abri's call (crossing, after the vote) | (0, 22), 34 x 12 m |

Moved on November 12 because they stood on the builder's own ways: the ferry queue to (-24, 41.6), (-24.2, 39.8), (-24.1, 38) (two of it were 0.55 and 1.02 m from the way from Teo to Jask); the hand at the press to (44.6, 46.6) (1.01 m from the way from Hadda to the gate); lantern posts from (16.2, 26.6), (8.8, 64.8), (-42.2, 16.6) and (25.6, 29.4) to (16.4, 28.6), (10.6, 66.6), (-41, 12.6) and (27.8, 28.6), and the one south of the kitchen from (19.6, 62.4) to (16.4, 60.8) (0.77 m from the chapter-26 vote route's walk from (23.2, 64.5) to (17, 62.6), which the navigation cut); kitchen table 1 from (21.4, 63.2) to (20.2, 60.2); Teo's second tray stack from (-11.4, 25.6) to (-11.2, 28.2); and a tray stack by the seed house from (-5.4, 70.6) to (-6.6, 71.2) (0.69 m from the errands route's walk up to the dry walk: inside what the navigation needs, with nothing to spare). The other townsfolk stand where they stood: the cooks (29.8, 71.9), (36.2, 65.2); the two arguing over a coil (18, 30.2), (19.6, 31); the seed-house hands (-8.2, 75.2), (8.6, 76); the angler (-33.4, 40); the three on the mooring lines from (29.6, 16.6), (27.2, 15.2), (24.8, 16.6).

### MAP_EYE_TEST at blockout (first build, plans and first stills)

| Check | What was seen |
|---|---|
| Purpose and adjacency | Passes. The landing is on the pool; the mooring faces the open water an island could come in from; the ferry stage and the press are the two ends of the crossing; the kitchen is beside the seed house at the foot of the upper walk; the gate is where the bank opens. |
| Circulation and scale | Passes after two faults were built out. The mooring lobe first touched nothing (the bake refused it), and the rims' openings were round gaps that closed to less than a body's width at the rim's inner face (the bake refused Senn's island); both are now checked by the builder itself (`OnTheFloor`, openings through the rim's whole depth). Twenty must-reach points join. |
| Construction and enclosure | Passes as a rule: every floor has a drawn rim, every deck a drawn rail, and nothing stops a walker that isn't drawn. |
| Terrain and ecology | Passes: platforms in slack water, the jungle on a bank behind, the river leaving east. |
| Immediate understanding | Partly. The arrival frame shows the landing and the pool's edge, not the seed house: the fixed camera sees 14 m north and the landmark is 60 m off. The objective line carries the first move. |

### MAP_EYE_TEST after dressing (`xylos-b-05`, editor stills only)

It reads as a river town someone lives in: trodden earth where feet go and moss and litter where they don't, lily pads (round, slit, veined) and reeds in every frame with water in it, trays of seedlings stacked where they'd be carried from, a kitchen with its pit and tables, a carpentered press and a slung cradle, lanterns, a crew on their lines with the island on the ropes beyond the rim, and decks of single boards with the grain along each (they read as square tiles until November 12). The rims are roots laid along every edge; the crowns behind are fronds; the generated seed house, awnings, arches and the small seed house that is Senn's hut carry the frames they are in. The townsfolk wear the chapter's two unnamed Okhari bodies (the grower's green and rust; the plain body's sand wrap, teal shawl and red sash) under leaf hats, panniers, yokes with baskets, poles, seedling trays and coils of line, each a little taller or shorter than the next, and no two who stand together share a look. **Passed for the editor stage; not passed for the player**, which nobody has run. What is still weak, plainly:

- **One key of colour.** Floor, rims, water and leaves are all between olive and teal; the kitchen's produce, the seed lanterns, the sashes and the party break it, not much else.
- **The bank behind the upper walk is a ten-metre wall of basket weave** (`RootWall`): it holds the earth in, and from the walk's top it reads as a woven wall more than as a jungle bank.
- **The public water's pale blotches** read as camouflage in the wide frames; the licensed water is plainer and needs the second scatter of pads to measure.
- **The seed house's forecourt is the plainest floor in town** (.19): one trodden sweep in front of the door.
- **Nothing moves that hasn't been seen moving**: the walk loops (`WalkLoop`), the rope's three pulls, the island coming in and the hold lines are built and tested in code, never watched in a player. Signs and prompts are hidden in stills.
- The stalls' fronts (`MarketAwning` at yaw 180 and 250) look right in the kitchen still but were not checked from the counter side.

Reviewer: Claude (agent review; no independent viewer and no approval by Nathan is implied).
