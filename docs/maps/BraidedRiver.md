# The Braided River - Xylos, between Rootmarket and the Walking Canopy

Functional brief, written with the blockout and re-laid after it (November 11 and 12, world engineer B, Claude). **MAP_EYE_TEST: blockout FAILED on the first layout, PASSED on this one (agent review, editor stills and plans); final in-player pass OPEN.** Chapter brief: [ch03-carrying-a-place.md](../chapters/ch03-carrying-a-place.md); script wiring: [ch03-manifest.md](../writing/ch03-manifest.md). Builder: `S/Editor/World/XylosBuilder.River.cs`. Mechanics: [CHAPTER_GUIDE.md, "World mechanics"](../CHAPTER_GUIDE.md). Review status is at the end, and it is an agent's review: nobody else has looked.

## Purpose and formation

The river that Rootmarket lives on, a morning's poling upstream of the town. It runs **from the east to the west**: out from under the Walking Canopy, which the storm has parked on the crossing, past Seedfall bank and two long islands, and on down to the town's landing pool. Where it meets an island it splits, so there are three channels, and they rejoin under root arches.

The growers have always moved this river about. Each channel has a **sluice** at its upstream (east) end: a wall of posts and woven root across the channel's head, with a hand wheel on the bank. Shut a sluice and the channel below it runs down to a wade. Root weirs laid across the cuts (dams, not ways across) are why one channel can stand nearly empty beside a full one. That is the zone's one rule: **the sluices decide which channel is ground.**

- **North channel**: the main stem, deep and fast, along the foot of the jungle, behind the north bank and the second island. Never waded. Reedstalkers hunt its edge.
- **Middle channel**: between the north bank and the first island, with the cut that runs south between the two islands. **Sluice A** (the "middle sluice") stands at its head, on the east tip of the north bank.
- **South channel**: leaves the main stem at the east tip of the second island, runs south past Seedfall and then west along the foot of both islands. **Sluice B** (the "south sluice") stands at its head.

## Users and access

Seed-carrier crews and ferry hands. Three carriers went aground in the storm: *Marrow-1* jammed on the riffle off the first island's west tip, *Marrow-2* on the second island's north shore, *Marrow-3* thrown up on Seedfall's gravel, where a Seedthane has nested on her. Jask poles the town ferry out to a stage on the first island. Old Senn's wife, Ulma, kept a small island off the second one; its mooring ring is under water now.

The party walks in from Rootmarket's river gate at the west end of the north bank, and leaves by the Canopy's mooring roots at the east end (shut until Teo has been heard at Marrow-3) or by the root bridge at Seedfall, back to town.

## Layout

About 450 m west to east and 115 m south to north. x runs east, z north. **Every piece of land is a strip 16 to 20 m wide with a channel along its north shore**, because the fixed camera sees about twelve metres north of the party and five south (and nothing beyond that, however tall): the river is the back of each view and the walked line keeps within a few strides of it. The jungle wall stands across the main stem and is only ever seen in an overview; the trees a play frame does show stand along each north shore with their feet at the water's edge, and snags lie in the main stem's margin beyond them. The south side is the camera's foreground and stays low: the south channel's far bank is a bluff about 3 m high, under the sightline to anyone at its foot, with old forest floor on top of it that nobody reaches. It is the edge of the world on that side, and it is drawn: there are no invisible walls along it. The west end is the same: behind the town gate the ground stands straight up into a bank of root and earth (from x -21.4, about 4 m high) with the jungle on it, and both channels' heads end against it. The north is the jungle's bank across the main stem and the east the Canopy's flank, both far too steep to climb; the boxes along the north, east and west stand behind those banks, where nobody can reach them.

```text
z 150  ///////////////////// JUNGLE (far wall, overviews only) ////////////////////////////////////////////////////////////
z 138  ~~~~~~~~~~~~~~~~ NORTH CHANNEL (main stem, deep, never waded; shore trees along its bank) ~~~~~~~~~~~~~~~<-- from under
z 121  [gate] arrival . FIRST ARCH . Thornback . reedstalkers . keepers' house ]A                                   the Canopy
       ======== NORTH BANK (a levee 16 m wide) ============== SLUICE A (wheel) \~~~~~~ north channel ~~~~~~~~/ SLUICE B
z 105  #riffle ~~~~~~~ MIDDLE CHANNEL (drains on A) ~~ ferry raft ~~~~~~~~~~~~+ Marrow-2 ~~~~~~~~~~~~~~~~~~~+B~+ pod trees
z  95  Marrow-1 >      ferry stage                                         |c| ISLAND TWO: Dabb . pods(blue)  |h|  Marrow-3 . Teo
z  87  ISLAND ONE: Garo . fight . CAMP (cradle, anvil, cache) . pods(red)  |u|  fights . north-shore pack     |e| SEEDFALL fan      ==> CANOPY
z  76  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ # weir # ==+t|  ford                          |a|  Seedthane, 4 pods    MOORING
z  66  ~~~~~~~~ SOUTH CHANNEL (drains on B) ~~~ gravel bar (cache) ~~~~~~~~~~~~ ULMA'S ISLAND ~~~~~~~~~~~~~~~~+d|  root bridge > town   pods(yellow)
z  56  ############################## bluff (3 m, not climbed; forest floor above, never reached) #############+#########
       x 0            50            100            150   168  192        250             319  346        392
```

| Area | What it is for | Size | Combat |
|---|---|---|---|
| North bank | The way in, a levee between the main stem and the middle channel: the first arch thrown over the main stem (the three channels and the Canopy named under it), the first Thornback, the reedstalker reach, the sluice keepers' house, Sluice A on its east tip | 210 x 17 m | `River_0`, `River_1` |
| Middle channel | Deep until A; then a 14 m wade to the first island; root arches thrown over it | 145 x 14 m | none |
| Island one | *Marrow-1* jammed on the riffle off its west tip, her mate Garo on the tip; the **mid-river camp** (ferry stage out over the north shore, cradle, anvil, a cache; nothing fights inside it); red spice pods; a Sporecaster pocket at the east end | 145 x 20 m | `River_Marrow1`, `River_3` |
| The cut and its weir | The middle channel's arm between the islands, drained by A too: the way from island one to island two. The weir across its south end is a dam of root and is not crossed | 16 x 25 m | none |
| Island two | *Marrow-2* on its north shore with Dabb beside her and blue spice pods in her shade; a Reedstalker pack on the north shore; the ford to Ulma's island; Sluice B on the east tip | 135 x 21 m | `River_Marrow2`, `River_5`, `River_6` |
| Ulma's island | Off island two's south shore, across a ford waded from the start: the drowned ring off its east end, the fork facing the town where the post goes, a cache | 22 x 13 m | none |
| South channel | Deep until B; then a wade, with a gravel bar and its cache for whoever comes back along the bed | 380 x 18 m | `River_Bar` |
| Seedfall bank | The gravel fan: *Marrow-3* across its back with the nest on her, pod trees behind, four hanging pods over the floor, the Seedthane's ground in front; the root bridge back to town | 50 x 60 m | `Seedthane` |
| Canopy mooring | The neck of land under the Canopy's flank: the mooring roots, yellow spice pods | 25 x 20 m | none |

**Circulation.** One line west to east, and each sluice is the only way on:

1. North bank, from the town gate east along the main stem's shore: the first arch, `River_0`, `River_1`, the keepers' house, the wheel of **Sluice A** on the bank's east tip.
2. The middle channel goes down. Across its bed to island one: the ferry stage and the camp are straight across; *Marrow-1* and Garo are west along the island; the red pods and `River_3` east.
3. From island one's east tip across the drained cut to island two: *Marrow-2*, Dabb, the blue pods, then east along the north shore (`River_5`, `River_6`). The ford to Ulma's island leaves the south shore half way along and is waded from the start.
4. The wheel of **Sluice B** on island two's east tip. The south channel goes down; across its head to Seedfall.
5. The fan's edge (the talk that shows the Seedthane), the fight, Teo beside *Marrow-3*, east to the mooring roots; or the root bridge back to town.

Optional: Ulma's island, the south channel's bar and its bed (after B), the caches, the harvest nodes. The riffle at island one's west tip and the weir between the islands are dams and are never ways across. Steps 5 to 8 of the main quest are flags, so they complete in whatever order the water allows: with this layout that is A, then Marrow-1 and Marrow-2 in either order, then B.

**Travel** (walking pace 6.7 m/s; wading 0.62 of it):

| Leg | Metres | Seconds |
|---|---|---|
| town gate to the first arch | 55 | 8 |
| first arch to Sluice A's wheel (through `River_0`, `River_1`) | 138 | 21 |
| wheel to the ferry stage, by the drained bed | 72 | 11 |
| camp to Garo at Marrow-1 | 66 | 10 |
| camp to island one's east tip | 58 | 9 |
| across the cut to Dabb at Marrow-2 | 54 | 9 |
| Marrow-2 to Sluice B's wheel | 112 | 17 |
| island two to Ulma's fork, by the ford | 34 | 7 (the ford is waded) |
| Sluice B's wheel to Seedfall's edge | 22 | 4 |
| Seedfall's edge to the fan's centre | 22 | 3 |
| fan's centre to the mooring roots | 31 | 5 |
| fan's centre to the root bridge | 26 | 4 |
| end to end without a fight | about 500 | about 80 |

**Camera and scale.** The fixed ground camera (pitch 40, yaw 20, 19.5 m) looks north-north-east. Each carrier lies on a north shore, behind her own fight. The sluice keepers' house and the camp's awning stand on the north side of their paths. Roots thrown over a channel land beside the paths and each is its own piece, so the view opens a hole in the one a hero is behind. The shore trees stand on each strip's north edge, north of its walked line: their trunks and flared roots are the back of the frame, their crowns are over the water, and a crown north of the party never hides ground the camera needs. Banks run down at about 27 degrees, so a wader is never behind one. Deep water is stopped by walls that reach a wader's waist and no higher: a shot crosses a channel as it crosses open floor.

## What the player has to be able to see

- **Which water a sluice changes, before turning it.** A sluice is a wall across the head of its own channel, with its wheel on the bank end: the water it holds is the water in front of it. Standing at the wheel draws the view back over the stretch that will go (`FlowSwitch.shows`), and it holds there while the water falls. A floor sign names each (`MIDDLE SLUICE`, `SOUTH SLUICE`).
- **Deep against shallow.** The water's colour is its depth: clear over a ford or a drained bed, with the bed showing through, and opaque dark green where it is over the knee. Foam lies where the ground comes up through it.
- **Where the Seedthane is.** The talk at the fan's edge shows it standing in front of Marrow-3 (`previewPoint`), and is where a lost fight starts again.

## Identities

Scene `BraidedRiver` (ZoneKind GroundCombat, profile Jungle, threat 1: chapter 3's enemies are authored at their real weight). Music: Jungle Planet Groove, fights Riverbed Stomp (ZoneDef). Ambience: `AMB_BraidedRiver` (river and insects, made by `tools/water_sounds.py`). Light: `RiverLooks` in the builder (a bright humid green-gold day).

| Kind | Ids |
|---|---|
| Spawns | `Arrival` (0, 122.6), `Camp` (100, 88), `Seedfall` (343, 90), `Canopy` (383, 90), `Bridge` (350, 72) |
| Ways out | `River gate — Rootmarket` (-16, 121) to spawn `River`; `Mooring roots — The Walking Canopy` (392.5, 90), needs `xylos.seedthaneGone`; `Root bridge — Rootmarket` (350, 64.5) to spawn `River` |
| Water | `North channel` (level 0, never drains), `Middle channel` (0 to -0.7 on `xylos.sluiceA`), `South channel` (-0.3 to -1.0 on `xylos.sluiceB`) |
| Sluices | Points `Middle sluice wheel` at (172.4, 114.5) (prompt "Open the middle sluice", node `SelaSluiceA`, report `SluiceA`) and `South sluice wheel` at (323.5, 102.0) ("Open the south sluice", `SelaSluiceB`, `SluiceB`); each a `FlowSwitch` on its gate; log `SLUICE_OPEN <flag>`, then `WATER_DRAINING`, `WATER_DRAINED`. The points are found from the ground by the builder (`Sluice` returns them): read them from the built scene if the banks are ever moved |
| People | `Jask` on the ferry stage (103.2, 94.4), from `xylos.archSeen` until `xylos.voteHeard` (`Whereabouts`, the complement of the town's ferry Jask) (first `JaskFerryFirst`; `JaskFerry` once `xylos.sluiceA`, until `jask.ferryTalk`; then `JaskFerryRepeat`); `Garo` (35, 91.6) once `clear.River_Marrow1` (`GaroRelief` sets `xylos.marrow1`); `Dabb` (212, 100.8) once `clear.River_Marrow2` (`DabbRelief` sets `xylos.marrow2`); `Teo` (355, 101) from `bossdown.Seedthane` until `xylos.voteHeard` (`Whereabouts`, live; the complement of the town's Teo) (`TeoSeedfall` sets `xylos.seedthaneGone`) |
| Crossings and talks | `SelaRiverArch` (`xylos.archSeen`) crossing the bank at x 39; `SelaSeedfall` (`xylos.seedfallSeen`) crossing x 339.6 at the fan's edge, or z 72.6 coming up from the south (the root bridge, the drained bed); `SelaSeedthaneGone` after `bossdown.Seedthane` (`xylos.seedthaneSeenOff`); Jask's hail `hail.ferry` from (104, 104), 30 m |
| Quest things | Spice pods red (133, 80.4), blue (223, 99.6), yellow (383, 96.6), each picked from 1.1 m south of the plant: report `SpicePods`, flags `xylos.pods.<colour>`, barks `pods.<colour>`; Ulma's fork (244.6, 70.8): "Set Senn's mooring post", node `SelaMooringPost`, report `SennPost`, flag `senn.postSet` |
| Fights | `River_0`, `River_1`, `River_Marrow1`, `River_3`, `River_Marrow2`, `River_5`, `River_6`, `River_Bar`, `Seedthane` (needs `xylos.seedfallSeen`; checkpoint spawn `Seedfall`) |
| Services | `Ferry cradle — repair and save` (96, 83) (free, spawn `Camp`), `Fabricate — Lattice Anvil` (112.5, 84), both inside the camp's safe pocket (30 x 20 m on (104, 87)) |
| Caches | `xylos.river.arch` (41, 125.4), `xylos.river.camp` (110, 79.6), `xylos.river.marrow2` (225, 102), `xylos.river.ulma` (256.4, 67.8), `xylos.river.bar` (151.5, 67.2; after B) |
| Harvest | Amber Sap `river_sap_0..3` at (54, 125), (118, 125), (206, 99.6), (374, 117.6); Rootfibre `river_fibre_0..3` at (18, 117.4), (88, 91.6), (282, 90.8), (349, 73) |

## Route coordinates

Read from the built scene (`BraidedRiver.unity`, November 12 rebuild) with the builder's constants beside them; x, z on the plan, the floor about y 0.6 everywhere a body stands except the drained beds (middle -0.7, south -1.0 once its sluice is open). A route's `walk`/`path` points should keep to the trodden lines: north bank z 121 to 124.5 from x -13 to 166; island one z 88 to 92 from x 40 to 158; the drained cut x 158 to 200, z 88.5 to 99.6; island two z 97 to 103 from x 200 to 317; Seedfall (322, 101) to (389, 90). Crossings start on walking through their box; everything else is pressed (A) within its range.

| Kind | Id / name | x, z | Range or box | Flag it sets / needs | Notes |
|---|---|---|---|---|---|
| Spawn | `Arrival` | 0, 122.6 | | | From Rootmarket's river gate |
| Spawn | `Camp` | 100, 88 | | | The camp cradle saves here |
| Spawn | `Seedfall` | 343, 90 | | | A lost Seedthane fight returns here |
| Spawn | `Bridge` | 350, 72 | | | Rootmarket's root bridge lands here |
| Spawn | `Canopy` | 383, 90 | | | From the Walking Canopy |
| Way out | `River gate — Rootmarket` | -16, 121 | 4.2 | open | To Rootmarket spawn `River`; the arch is at (-17, 121), the west bank rises behind it from x -21.4 |
| Way out | `Mooring roots — The Walking Canopy` | 392.5, 90 | 4.4 | needs `xylos.seedthaneGone` | To WalkingCanopy `Arrival`; refused: "The mooring roots are drawn shut: ask Teo Marrow at Marrow-3 what holds them"; its sign at (387.8, 86.6) |
| Way out | `Root bridge — Rootmarket` | 350, 64.5 | 3.8 | open | To Rootmarket spawn `River` |
| Crossing | `Look down the river` (`SelaRiverArch`) | 39, 121 | box 3 x 18 (z 112 to 130) | sets `xylos.archSeen` | Crossed by anyone walking east on the bank; Jask comes to the ferry stage on this flag |
| Switch | `Middle sluice wheel` (`SelaSluiceA`) | 172.4, 114.5 | 3.4 | sets `xylos.sluiceA` | Report `SluiceA`; drains the middle channel (0 to -0.7) over 3 s |
| Switch | `South sluice wheel` (`SelaSluiceB`) | 323.5, 102 | 3.4 | sets `xylos.sluiceB` | Report `SluiceB`; drains the south channel (-0.3 to -1.0) over 3.4 s |
| Crossing | `Look at what's on Marrow-3` (`SelaSeedfall`) | 339.6, 88 | box 3 x 60 (z 58 to 118) | sets `xylos.seedfallSeen` | Wakes the Seedthane's door; saves at `Seedfall` |
| Crossing | `... from the south` (`SelaSeedfall`) | 359, 72.6 | box 38 x 3 (x 340 to 378) | sets `xylos.seedfallSeen` | For whoever comes up from the root bridge or the drained bed |
| Prompt | `Red spice pods` | 133, 79.3 | 2.8 | sets `xylos.pods.red` | Quest `ThreeBaskets`, report `SpicePods`; the plant is at (133, 80.4) |
| Prompt | `Blue spice pods` | 223, 98.5 | 2.8 | sets `xylos.pods.blue` | As above; plant (223, 99.6) |
| Prompt | `Yellow spice pods` | 383, 95.5 | 2.8 | sets `xylos.pods.yellow` | As above; plant (383, 96.6) |
| Prompt | `Ulma's mooring fork` (`SelaMooringPost`) | 244.6, 70.8 | 3 | sets `senn.postSet` | Quest `MooringPost` step 0 (report `SennPost`); reached by the ford from (250, 88) to (250, 73), waded from the start |
| Person | `Jask` | 103.2, 94.4 (on the stage deck, y 0.76) | | here from `xylos.archSeen`, gone at `xylos.voteHeard` | First `JaskFerryFirst`; `JaskFerry` once `xylos.sluiceA` (until `jask.ferryTalk`); then `JaskFerryRepeat`. His hail (`hail.ferry`, 30 m from (104, 104)) needs `xylos.sluiceA` |
| Person | `Garo` | 35, 91.6 | | appears on `clear.River_Marrow1` | `GaroRelief` sets `xylos.marrow1` |
| Person | `Dabb` | 212, 100.8 | | appears on `clear.River_Marrow2` | `DabbRelief` sets `xylos.marrow2` |
| Person | `Teo` | 355, 101 | | here from `bossdown.Seedthane`, gone at `xylos.voteHeard` | Walks in (`ScriptedWalk`, 2.4 m/s) from the root bridge's foot (348.5, 65.5) by (351.4, 72.5), (351.6, 80), (351.6, 91), (352.2, 98.6) to the carrier (east of the Rootfibre hank at (349, 73), which his first leg used to run into; the build now sweeps every walker's legs against everything solid, `WALKERS_CLEAR`), about 15 s, through Sela's line; a load after his talk finds him at the carrier. `TeoSeedfall` sets `xylos.seedthaneGone` (opens the mooring roots); then `TeoSeedfallRepeat` |
| Talk | `SelaSeedthaneGone` | anywhere | | after `bossdown.Seedthane`, sets `xylos.seedthaneSeenOff` | `AfterTalk`, 2.5 s once nothing is fighting |
| Service | `Ferry cradle — repair and save` | 96, 83 | | | Free; saves at `Camp`; inside the camp's safe pocket (30 x 20 m on (104, 87)) |
| Service | `Fabricate — Lattice Anvil` | 112.4, 84 | | | In the safe pocket |
| Cache | `xylos.river.arch` "Growers' basket" | 41, 125.4 | 2.6 | | |
| Cache | `xylos.river.camp` "Ferry hands' basket" | 110, 79.6 | 2.6 | | |
| Cache | `xylos.river.marrow2` "Marrow-2's stores" | 225, 102 | 2.6 | | |
| Cache | `xylos.river.ulma` "Ulma's basket" | 256.4, 67.8 | 2.6 | | |
| Cache | `xylos.river.bar` "Drowned basket" | 151.5, 67.2 (y 0) | 2.6 | after `xylos.sluiceB` in practice | On the bar in the south channel |
| Harvest | `river_sap_0..3` (Amber Sap) | (54, 125), (118, 125), (206, 99.6), (374, 117.6) | 2.6 | | "Gather Amber Sap" |
| Harvest | `river_fibre_0..3` (Rootfibre) | (18, 117.4), (88, 91.6), (282, 90.8), (349, 73) | 2.6 | | "Cut Rootfibre" |
| Fight | `River_0` | 76, 121 | 24 x 16 | sets `clear.River_0` | 1 Thornback, 2 Mirefly Swarm |
| Fight | `River_1` | 132, 121 | 24 x 16 | | 3 Reedstalker |
| Fight | `River_Marrow1` | 48, 88 | 26 x 16 | | 2 Thornback, 2 Reedstalker; clearing it brings Garo out. Main-quest step 6 points here until Garo is out, then at him |
| Fight | `River_3` | 146, 86 | 22 x 16 | | 2 Sporecaster, 2 Mirefly Swarm |
| Fight | `River_Marrow2` | 214, 95 | 26 x 16 | | 3 Sporecaster, 1 Thornback; brings Dabb out. Main-quest step 7 points here until Dabb is out, then at him |
| Fight | `River_5` | 260, 98 | 26 x 16 | | 4 Reedstalker |
| Fight | `River_6` | 296, 98 | 24 x 16 | | 2 Thornback, 2 Sporecaster |
| Fight | `River_Bar` | 150, 66 | 16 x 12 | | 3 Mirefly Swarm, 2 Reedstalker; on the south channel's bar |
| Boss door | `Seedthane` | 362, 88 | 30 x 28 | needs `xylos.seedfallSeen`; checkpoint `Seedfall` | No door: the fan is open. Spawner (362, 97.5) facing south; preview (341, 88), radius 6; way out `BossExit` (374, 105.5) |

## Encounter plan

| Fight | Where | Pack | Why here |
|---|---|---|---|
| `River_0` | North bank (76, 121) | 1 Thornback, 2 Mirefly Swarm | The first Thornback, on a strip with water either side: its charge has two ways to end in the river |
| `River_1` | North bank, east (132, 121) | 3 Reedstalker | The main stem's edge, on the way to the wheel |
| `River_Marrow1` | Island one, west (48, 88) | 2 Thornback, 2 Reedstalker | Clearing it brings Garo out |
| `River_3` | Island one, east (146, 86) | 2 Sporecaster, 2 Mirefly Swarm | Beside the red pods and the way to the cut |
| `River_Marrow2` | Island two, west (214, 95) | 3 Sporecaster, 1 Thornback | "Sporecasters in the wreck's lee"; clearing it brings Dabb out |
| `River_5` | Island two, north shore (260, 98) | 4 Reedstalker | The deep channel's edge again, a full pack |
| `River_6` | Island two, east (296, 98) | 2 Thornback, 2 Sporecaster | The last before Sluice B: both kinds together |
| `River_Bar` | The south channel's bar (150, 66) | 3 Mirefly Swarm, 2 Reedstalker | Optional, only once B is open |
| `Seedthane` | Seedfall (362, 88) | The Seedthane | Mini-boss: a 30 m open fan, four hanging pods to knock down for cover, no door |

Every pocket is 16 m deep, the width of its strip, so the shallows either side are part of the fight: a body in them wades. A Mirefly Swarm is one cloud, so two or three is a pocket's worth.

**Reed hides** (`AmbushSpot`, radius 3.2, each with a bed of reeds standing in it): three or four to each Reedstalker pocket, at the water's edge. `River_1`: the bank's north shore at x 120, 134.5, 141.5 (z about 130). `River_Marrow1`: the middle channel's bed off island one at x 41, 49, 57 (z about 98 to 99; under water until A is open). `River_5`: island two's north shore at x 250.5, 258, 265.5, 273 (z about 107 to 109). `River_Bar`: round the bar, 8.5 m east, west and north of its middle. The shore trees keep off them.

## The Seedthane's ground (for the combat engineer)

- Floor: gravel fan, level (about +0.6), centred **(362, 88)**, clear to about **15 m** in every direction (the fan is a 25 m disc); the encounter's trigger is 30 x 28 m on that centre.
- *Marrow-3* lies across the back: hull centred (363, 107.5), bow to the east, a solid box about 15.6 x 4.4 m, 3.2 m high. The Seedthane's spawner is on her near side at **(362, 97.5)**, facing south, shown before the fight (`previewPoint` at (341, 88), radius 6).
- **Hanging pods** (`CoverPod`, four): over the floor at **(354.5, 95), (370.5, 94.5), (354, 80.5), (371, 81)**, each on a stalk of its own planted 6 to 7 m outside it (four separate pieces: one mesh of the four read as a single wide prop across the camera's side of the fight). Hanging, a pod is a body (`Health` id `SeedPod`, 260 integrity, hurtbox a capsule reaching from 0.3 to 3.2 m). Killed, or told to `Drop()`, it lands where it hung as cover 2 x 1.5 x 2.2 m with a carving obstacle. `CoverPod.NearestHanging(point)`, `CoverPod.Fell`, `CoverPod.All`. Pods hang again on every load; once `bossdown.Seedthane` they are gone.
- **Way out** (`BossExit`, boss `Seedthane`): **(374, 105.5)**, north-east off the fan toward the main stem, 21 m from the centre: where it goes when it is driven off.
- Open on every side: west to the south channel's head (drained), south-west to the root bridge, east to the mooring. A lost fight returns to spawn `Seedfall` (343, 90), 19 m from the centre.

## Review

Reviewer: Claude (world engineer B, chapter 3), from editor stills and plans, November 11 and 12. Nobody else has looked, and nothing here has been walked in a player. **MAP_EYE_TEST: blockout FAILED on the first layout and PASSED on the layout above; final in-player pass OPEN.**

### Blockout

Evidence: 21 grey stills in `Builds/quality/look/ch3b-blockout-01/` (`BraidedRiver-*`), plans in `Builds/quality/chapter3/river-blockout-01/`.

The first layout had a 30 m north bank with the main stem beyond it and the jungle beyond that, and islands 30 to 35 m across.

| Check | What the stills showed |
|---|---|
| Purpose and adjacency | Passed. Each sluice stood at the head of its own channel, the camp was across from where the middle channel is waded, each carrier lay by her own fight. |
| Circulation and scale | **Failed.** The riffle at island one's west tip and the weir between the islands were walkable from the start, so Sluice A opened nothing the party needed. |
| Construction | Passed. Three levels of water over one ground, sills where two meet. The navigation baked with 41 must-reach points. |
| Terrain and arenas | Passed on the plan. |
| Immediate visual understanding | **Failed.** The fixed camera sees twelve metres north and five south. From the bank and from the middle of either island the frame was floor: 20 of 21 stills measured a largest plain area between 0.24 and 0.91 against a limit of 0.22, and the river was in almost none of them. The first arch's near foot stood between the camera and the party. The floor was also far too bright. |

Repairs, all in the layout above: every piece of land became a strip 16 to 20 m wide with water along its north shore and its path near that shore; the main stem was brought round behind the north bank; the first arch was thrown over the main stem, beside the path; the riffle and the weir became dams of root that are never crossed, so each sluice is the only way on. The re-laid plan was not rendered grey again: its blockout questions were answered on the dressed build's plans and stills below.

### Dressed (editor)

Evidence: 25 stills in `Builds/quality/look/ch3b-final4-river/` (`BraidedRiver-*`; local biomes; the same views without them, from one build earlier, are in `ch3b-dress-10-public/`), plans in `Builds/quality/chapter3/river-final4/` (`BraidedRiver-west`, `-east`, `-whole`). `python tools/look_density.py Builds/quality/look/ch3b-final4-river --match BraidedRiver`: every still passes; the worst play frames are `marrow-2` 0.14, `marrow-1` 0.13 and `north-shore` 0.11, the rest 0.08 or under; the east overview is 0.11.

| Check | What the stills show |
|---|---|
| Purpose and adjacency | Passes. A levee with a path, a keepers' house beside the gate it keeps, a ferry stage where the channel is narrowest, carriers aground on the shores the flood would leave them on. |
| Circulation and scale | Passes on the plan: one line west to east, each sluice the only way on, the ford and the bar as side trips. Not walked. |
| Construction | Passes. Banks run into the water without a gap; the sluices are kit gates in a row across a sill; the weirs are dams that stand out of the water. |
| Terrain, ecology and arenas | Passes. The channels follow the ground (one height function, three levels). Seedfall is a gravel fan with a wreck across its back and spilt seed on it, which is why something nested there. |
| Immediate visual understanding | Passes, with reservations below. From every bank and island frame there is water at the back, buttressed trunks standing in its edge, pads on it, and a trodden line to follow; a sluice reads as a gate with a wheel; the ford reads as a ford. |

What is still weak, and would be the first things to look at in a player:

- **Seedfall's middle** (`seedfall`) is gravel, seed and one hanging pod until the Seedthane is standing in it. It is the one frame with no water.
- **The first arch does not read as an arch from the play camera**: it is a great root coming down at the back of the frame. Its span is only seen in an overview. The talk under it is about the channels and what sits at the river's far end, neither of which is in that frame; whether that is enough has not been tried.
- **The land is evenly lit in the stills.** The leaf shade that moves over it is a runtime effect and is not in an editor still, so its strength (0.42) is unjudged.
- *(Fixed November 12.)* The water's surface was mottled in broad pale pieces. It was not the bed showing through, as this said: it was the water's glint, a metre across at a grain of 1.7. The glint is now fine and faint (grain 3.4, strength 0.1).
- **A root thrown over the middle channel looks like a bridge** from an overview. It is solid only at its feet.
- The sounds (`water_splash`, `water_rush`, `root_groan`, the ambience) are synthesised and unheard.

### Dressed (editor), November 12 rebuild

Taken over from the first engineer by a second (Claude) and rebuilt: the west end made a visible bank, the town's spawn and the two people tied to the town's flags, the trees crowned in fronds, roots run out over the ground from every shore tree, the floor moss-and-earth instead of lime, seed spilt round the carriers, the water's glint made small. Evidence: 27 stills in `Builds/quality/look/rc-07/` (local biomes) and `rc-07-public/` (without), boss and drained stills in `rc-02-stills/` (both sluices open, the Seedthane's body stood at its spawner; built with `-xylos-drained -xylos-stills` and never kept), plans in `Builds/quality/chapter3/river-rc-06/`. `python tools/look_density.py Builds/quality/look/rc-07 --match BraidedRiver`: every play still at or under 0.22 except `marrow-2` at 0.23 (the open middle of island two west of Dabb); the same with and without local biomes.

| Check | What the stills show |
|---|---|
| Edges | Every edge is drawn: the jungle's bank across the main stem (north), the Canopy's flank (east), the bluff (south) and now a bank of root and earth with trees on it behind the town gate (west). No walkable floor reaches an invisible box. |
| Immediate visual understanding | Better than before: trunks stand in the water's edge with flared roots, and roots run back across the strips from them, so the banks read as jungle floor and not lawn; the overviews show palms and tree-ferns in fronds rather than green balls. |
| The Seedthane's ground | Reads from the fixed camera with the body stood in it (`rc-02-stills/BraidedRiver-marrow-3.png`, `-overview-east.png`): the red thorn-fans, the pale legs, the four hanging pods on their stalks and Marrow-3 behind. From the fan's middle (`seedfall`) its legs are at the top edge of the frame. |

Still weak: island two's west end (`marrow-2`) is open moss with litter, just over the plain-floor limit; the bluff's top and the west bank's top are one flat tone seen only in overviews; the south channel meets the west bank in a square corner (an overview flaw); Teo appears at Marrow-3 in the same frame as the Seedthane's fall (followed live, so the talk that opens the mooring roots is there at once); none of it has been seen in a player.

### Finishing pass, November 12 afternoon

- **Marrow-2's lee** has what her crew got off her before she settled: two stacks of seed trays with seedlings in them (solid, waist-low, cover in `River_Marrow2`) at (219.4, 94.4) and (221.9, 96.5), two baskets at (217.4, 93.2) and (223.6, 95.1), and her sail furled on its spar along the shore from (209.4, 92.6) to (215.6, 91.6). `marrow-2` measures at or under 0.22 now (`Builds/quality/look/rc-14/`).
- **Teo arrives; he never appears.** He walks up from the root bridge's foot, off the frame of anyone fighting on the fan, across it to Marrow-3.
- **Floating leaf no longer hides a danger marking.** Lily pads and duckweed (`xylos-pads`, `xylos-pad-flowers`) draw after the water and write no depth, so a marking on the bed under them (`Telegraph`, Transparent-10) draws over them whole; the water itself never wrote depth.
- **The leaf shade is lighter and soft-edged** (0.24, edge 0.09): crisp, it lay on the channels as pale-and-dark camouflage.
