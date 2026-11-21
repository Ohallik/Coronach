# Sorrel's Second Sky - the first moon from above, under Stillwater's clamp

2026-11-14. Builder: `LastMooringBuilder.Sorrel.cs` on `ZoneKit` (the Sorrel engineer, stream B). Chapter brief: [ch10-a-route-with-room-to-turn-back.md](../chapters/ch10-a-route-with-room-to-turn-back.md), "SorrelSecondSky". Who places what: [ch10-wiring.md](../chapters/ch10-wiring.md) sections 1, 2, 6.1, 6.2. Script: [ch10-manifest.md](../writing/ch10-manifest.md) (`SurvivorSecondSky`, `SurvivorMotherGoes`). Tests: `T/PlayMode/SorrelSecondSkyTests.cs`. **MAP_EYE_TEST: blockout written before construction (this page); editor stills under Evidence.** Every number below is an `internal static readonly` in `LastMooringBuilder.Sorrel`: read it from there, never copy it.

## What the place is for

Sorrel, chapter 1's moon, seen again from its own sky. Dace's outpost is empty (his ten are aboard Many Hands); the haul road still runs north between the two ridge spines to the drill mast; past the drill the bore broke into Hushwell, and the ground there has fallen into a crater over the nursery. Stillwater's clamp, a white Compact machine seventy metres tall, stands in that crater with four anchor clamps chained out from it, and Mother of Doors is coiled round the nursery under it. The party flies the haul road north, through the two apertures the drill cut in the crater's rim, frees her clamp by clamp (each a dock onto a shelf, a release on foot, a launch), and after she goes the young's own road opens north and the lane east to Last Mooring is drawn.

**The rule: fly the ridge tops.** Every lane is at the ridge crests' height; the valley's floor is 27 m under the ships, the crown under her 12, the moat round the clamp 87. The edges are the rock you can see: the spines' crests, the crater's rim and the teeth on them. Nothing stands above the plane south of any fight lane.

## Rules of construction

- Everything flies at the plane (y 1). The moon's surface is drawn below it (one heightfield, `GroundTerrain`, Sorrel's own ground and strata, **no collider**: nothing walks on it). Its crests stand at the plane on every lane's edge and fall away outside; the stop is an invisible box on the crest line with code-built rock teeth (`HarrowgateBuilder.Crag`/`Fang`, in Sorrel's red) standing through the plane just outside it, **except on the crater's south half**, where no tooth rises above y 1 (the camera's side of the arena).
- No collider lies between y -0.5 and 4.2 over open air (Idra's hull reaches 2.17 m from a centre 0.8 over the plane, so to y -0.37): the shelves' floors are at **y -2.0**, so their works (1.25 m) and rails (0.5 m) stay under every hull that flies over them.
- Backdrop is kept clear of what must stay readable by where it shows on screen (`ZoneKit.ClearOnScreen`).
- The flight frame is about 25 m wide and 20 deep at the plane, and about 70 m wide on the moon's floor: the floor, not the plane, is what fills a frame.

## Plan

North up; x east, z north.

```text
                       z 520  the young's road, far end (44, 512)  [from end.motherFreed]
                              legs: x 0 (north), x 22 (south), x 44 (north), 12 m wide (two hulls side by side)
                       z 404  the road's mouth in the north rim; Road spawn (-2.2, 410)
          chain across the crater at z 352 (closed until end.motherFreed); Stillwater's clamp (0, 356) in the moat, under the plane
   CRATER  centre C (0, 330): the nursery's crown r < 21 (y -11, her coil on it), the moat r 31-41 (y -86), the rim r 74 at the plane
          her fight's ring r 21-36: shelves 1-4 at bearings 240, 205, 155, 120 (middles on r 27), cover at bearings 222.5, 180, 137.5 (r 32)
          the east notch -> lane z 322 -> Warp — Last Mooring (158, 322)
                       z 242-258  the two apertures (x -15..-4, 4..15), the pillar between; Dace's crossing z 250; checkpoint Clamps (7.45, 245) in the east one
                       z 232  the valley's head (where she is first seen from)
   VALLEY  x -40..40 to z 190, narrowing to -15..15 at z 232 (the haul road between the spines); fights at z 95 and z 175; the drill mast (-6, 212)
                       z 40   Dace's empty outpost below (habs, pad, rover)
                       z 0    ARRIVAL; warp home (-10, -4)
```

## Coordinates (as built; `LastMooringBuilder.Sorrel`)

| Place | World (x, y, z) | Id / flag |
|---|---|---|
| Arrival | (0, 1, 0), facing north | spawn `Arrival` |
| Warp home | (-10, 1, -4) | `Warp — The Hull of Many Hands` -> `ManyHandsHull`, spawn `Sorrel`; open, except while Mother of Doors' fight is under way: from her door's start to her fall it waits on `bossdown` and says "Free Mother of Doors first: she is still chained in the crater" (`FightHoldsTheWay`, November 14) |
| The valley | x -40..40 from z -14 to 190, its sides slanting in to x -15..15 at z 232 and running on at that to the rim (the apertures). Floor y -26, slopes terraced over 16 m up to the crests at the plane | |
| Lane fights | `Sorrel_Lane1` (0, 1, 95), `Sorrel_Lane2` (0, 1, 175); boxes 80 x 8 x 28 across the whole valley | `clear.<id>` |
| Checkpoint | (7.45, 1, 245): in the east aperture, south of Dace's crossing, 57.5 m from the door's middle (under the shared 60); both ships arrive between the aperture's walls. It stood at the valley's head (0, 1, 232), 70 m from the door and facing the pillar (November 14) | spawn `Clamps` |
| The valley's head | (0, 1, 232) | where she is on show from (the door's `previewPoint`) |
| Dace's crossing | box (0, 1, 250), 30 x 8 x 4, across both apertures | flag `end.sorrelSeen`, node `SurvivorSecondSky`, speaker `Survivor` |
| Crater middle C | (0, 1, 330) on the plane. The crown under her: y -10.6 inside r 11, the ledge y -11.5 to r 21 (her coil's, nothing of the zone's from r 11 to 21 over y -9); the crown's cliff to r 31; the moat y -86 to r 41; the outer wall in terraces to the rim at r 74 | |
| The door's box | centre (0, 1, 302), 148 x 8 x 92 (x -74..74, z 256..348); mouth the whole south face | encounter `MotherOfDoors` |
| Stillwater's clamp | standing in the moat at (0, -86, 356), 72 m tall, top about y -14, jaws south over the crown; drawn only. Its chains run from its jaws (0, -26, 344) to each anchor clamp | |
| The chain line | z 352 from the west rim to the east rim at the plane, with the east notch's chain at x 70: solid until `end.motherFreed` | |
| Shelves (`Sorrel.Shelves[i]`) | see the table below | |
| Cover | three rocks solid at the plane (sphere radius 3), drawn to y 2.5 on pillars out of the moat: (0, 1, 298), (-21.6, 1, 306.4), (21.6, 1, 306.4) | |
| Her way out | (0, 1, 400), the road's mouth | `BossExit` `MotherOfDoors` |
| The young's road | legs 12 m wide (x -6..6, 16..28, 38..50), turns at z 480..494 and 416..430, its far end at z 524; Road (-2.2, 1, 410) facing north (the partner arrives 4 m east of the lead, clear of both walls); crossing across it at z 418 (saves `Road`, needs `end.motherFreed`); far end point (44, 0, 514), reach 6.5 | `end.surveyFlown`, report `NurseryRoad` (LastSurvey step 0, needs `end.motherFreed`) |
| The lane east | z 306..338 from the east notch to x 168, floor y -12 | |
| Warp on | (158, 1, 322), drawn from `end.motherFreed` | `Warp — Last Mooring` -> `LastMooring`, `Arrival` |

### The four shelves (berths)

To her contract: each shelf is turned to face C, its works on the inner edge (the seat a metre in from it, on **r 23**), its middle on **r 27**, its foot spawn on the outer edge, 14 m along the ring by 10 m deep, its floor at **y -2.0**, a 0.5 m rail on all four edges. Its `AnchorClamp` stands on her coil on **r 17** on the shelf's bearing, top at y 0.9, chained to Stillwater's clamp. Shelves 1 to 3 stand on buttresses of the crown's rock (kept outside r 21.5); shelf 4 hangs off its anchor clamp's arm under one root at C (`Shelf 4: The east clamp's works`, its transform at (0, -2, 330), nothing in it static) for her strain to swing. Every number is `LastMooringBuilder.Sorrel.Shelves[i]` (`seat`, `middle`, `foot`, `launch`, `pad`, `flight`, `back`, `anchor`, `Box`, `Turn`); the build logs them (`MOORING_COORDS SorrelSecondSky`).

| i | Bearing | Seat (the hold) | Middle | Foot spawn | Launch (on the floor) | Pad (flight, over the middle) | Return `ShelfN` (flight, r 40) | Anchor clamp (r 17) |
|---|---|---|---|---|---|---|---|---|
| 1 | 240 | (-19.9, -2, 318.5) | (-23.4, -2, 316.5) | (-26.4, -2, 314.8) | (-27.8, -2, 319.2) | (-23.4, 1, 316.5) | (-34.6, 1, 310.0) | (-14.7, 321.5) |
| 2 | 205 | (-9.7, -2, 309.2) | (-11.4, -2, 305.5) | (-12.9, -2, 302.4) | (-16.6, -2, 305.2) | (-11.4, 1, 305.5) | (-16.9, 1, 293.7) | (-7.2, 314.6) |
| 3 | 155 | (9.7, -2, 309.2) | (11.4, -2, 305.5) | (12.9, -2, 302.4) | (8.4, -2, 301.4) | (11.4, 1, 305.5) | (16.9, 1, 293.7) | (7.2, 314.6) |
| 4 | 120 | (19.9, -2, 318.5) | (23.4, -2, 316.5) | (26.4, -2, 314.8) | (23.3, -2, 311.4) | (23.4, 1, 316.5) | (34.6, 1, 310.0) | (14.7, 321.5) |

Flight spawn on launching: over the shelf's outer part (r 30, the plane). The works: a winch housing 3.2 m wide, 1.25 m tall and 0.9 deep on the inner edge between the seat and her, solid (`The works`), its drum toward the seat; the seat itself is an empty `Clamp seat N` at the seat, facing C, for `Mother.StandClamp`. Nothing within 3 m either side of a seat at shelf level. Navigation: one bake of all four (`Walkable paths`), each checked from its foot spawn to its seat, its launch and its middle (`ZONE_ROUTE_BAKE_OK SorrelSecondSky points=12`).

The clamps are taken in order: `Sorrel_ClampN`'s `requiredFlag` and each `Clamp` arrow point's are `end.sorrelSeen` for the first and `end.clamp(N-1)` after. Each arrow point stands at its pad, never offered (`previewEncounter` its guns, whose box holds the pad). From her fight's start (November 15) a shelf out of its turn takes nobody: its pad is still offered, as a locked warp is, saying "The west clamp's works first" (a later shelf, its guns asleep) or "The south-west clamp's works next" (a shelf whose clamp is open), and a press refuses (`BERTH_REFUSED ClampN turn`; `Berth.OutOfTurn`, `BridleClamp.inOrder`, set by her fight). Docked out of turn, a party had stood on a shelf with nothing to do and no word why.

## Berth contract

Wired to stream A's `Berth.Declare` (wiring 6.1.1), one call per shelf inside the shelf's frame (turned to face C, so the berth's box and its edges lie along the deck): id `Clamp1` to `Clamp4`, shelf "The west clamp's works", "The south-west clamp's works", "The south-east clamp's works", "The east clamp's works" (the pad reads "Dock — The west clamp's works"), kind `GroundCombat`, box the floor to 1.2 m over it (top y -0.8, under every hull), pad, launch, foot and flight spawns and return as in the table, `inFight` true, `refuseWhileAwake` true. The fourth's berth is under the swinging root, so `berth.Drive` and the root move together.

## Boss arena contract (Mother of Doors)

*The Mother of Doors engineer writes what the fight needs here (wiring 6.2 is the starting point); the Sorrel engineer builds it and writes the coordinates back under it.*

### What the fight needs (Mother of Doors engineer, stream C, November 14, 11:00)

Positions are from the crater's middle **C** = her `Spawner`, on the plane (y 1), `faceAsPlaced` facing **south** (yaw 180): +z north, +x east, as the plan above. Her moves are `S/Combat/BossController.MotherOfDoors.cs` (every number in `BossController.MotherTune`, public: read it, never copy it); her body is `S/Combat/MotherCoil.cs`, the fourth shelf's swing `S/Combat/MotherStrain.cs`; her definition and the two calls the zone makes are `LastMooringBuilder.Mother.cs`. Her tests stand all of this in the yards (`T/PlayMode/LastMooringBossTests.cs`), so nothing here waits on the zone.

**Why the fight is small (the one thing that changes your plan).** The boss bar, the panel's fight line and the ground camera's framing show only within **34 m of her root** (`BossController.Focus`), and a boss's specials start only within 30 m of it. Her root is her head at C. So **everything the fight uses lies within 32 m of C**: the four seats on a ring of **r 23**, the shelves' middles on **r 27**, their pads, and the lanes between **r 21 and r 34**. The seats at r 50 would leave the party with no bar, no line and no attacks at every shelf. The flyable crater can stay r 74; the fight is its inner ring.

**How she lies.** Her head (`MotherOfDoors`, 16 m) is raised over the nursery at C, facing south toward the party: her snout reaches about C + (0, 0, -8), her capsule is r 4 round C. Her neck runs north from the back of her head to her coil, which lies round the nursery once and a little over: a ring of eleven `MotherBody` drums and a `MotherTail`, centreline **r 16 round C, 4.5 m under the plane** (each drum about 7 m thick: the body fills r 12.5 to r 19.5, y -7.5 to y -1). She is laid by her own code, at load, dormant or awake: **build nothing of yours inside that tube** (no rock, rim, spire or chain through r 11 to r 21 between y -9 and y 1), and nothing above the plane inside r 21 but her. Under her the crater and the nursery are yours as planned; Stillwater's clamp at (0, -86, 362) is below her and fine.

| Thing | What the fight needs | Why |
|---|---|---|
| Boss | As built: Spawner at C, south, `faceAsPlaced`; `previewPoint` the valley's head, radius 100. No `BossController.Focus` reaches beyond 34 m: keep the spawner at C | Her root is the fight's middle |
| Order | The clamps are opened **in order, 1 to 4** (west to east as you numbered them). `Sorrel_Clamp1`'s `requiredFlag` is `end.sorrelSeen`; `Sorrel_Clamp2`'s `end.clamp1`, `_3`'s `end.clamp2`, `_4`'s `end.clamp3`. Each `Clamp` arrow point is wanted only once the clamp before it is open (the first from `end.sorrelSeen`) | One arrow, one next clamp; Iru speaks after the first and the third; the fourth is the one that swings |
| The four shelves | Bearings as you have them (240, 205, 155, 120), pulled in: **seat on r 23, shelf middle r 27, pad over the shelf middle**, each shelf 14 x 10 at y -0.8 as planned, turned so its **works stand on the edge facing C** and its foot spawn on the edge away from her. Seats: 1 (-19.9, -0.8, 318.5), 2 (-9.7, -0.8, 309.2), 3 (9.7, -0.8, 309.2), 4 (19.9, -0.8, 318.5) | Within her reach and her bar; the works between her and the seat shelter it from her pressure wave (below) |
| The clamp's works and the hold | At each seat, standing on the shelf: the works' machinery (solid, at least 1.2 m tall and 3 m wide, between the seat and C) and **the hold, by one call**: `LastMooringBuilder.Mother.StandClamp(seat, i)` on an empty at the seat facing C. It adds the `BridleClamp` (index i, `settledBy` `MotherOfDoors`, `openedFlag` `end.clampI`, `keepsOpened`, reporting `Clamp`) and its `Tended` | Chapter 7's hold, nothing new: take its weight 1.2 s, ease .8 s held while she is slack. An opened clamp loads open (D285) |
| The fourth shelf | Built as a moving berth (stream A's `IBerth.Drive`): shelf, works, seat, pad and spawns all under one root, the shelf hung off its `AnchorClamp`'s arm (no spire: a spire can't swing), and one call on that root: `LastMooringBuilder.Mother.StandStrain(root)` (it pivots round the root's own position, which you stand at C; its other arguments default to bearing 120 out to 92 with the way marked at r 27). Both calls exist from 11:40. From the third clamp open until she goes, it swings on a marked arc round C from its bearing 120 out to **bearing 92** and back, every 16 s, at r 27 (about 13 m each way), and stands still whenever the party is on it or a conversation holds the controls (wiring 6.1 rule 5, D275). Keep **r 22 to r 33 from bearing 85 to 125 clear** of rock and of shelf 3 | "The fourth clamp is opened racing alongside her as she starts to move" |
| The anchor clamps | Each `AnchorClamp` stood on her coil just inside its seat, at **r 17 on the shelf's bearing**, its top at y 1 at most, chained to Stillwater's clamp as you planned; the fourth's on the fourth shelf's root, so it swings with it | They pin her coil |
| Clamp Tenders | Nothing of yours: the fight steps one out beside the seat on each dock (two from the third clamp), on the shelf, from `StillwaterTender` (D296; a tender is known by its Tender behaviour, `Tended.IsTender`, so it holds a clamp as chapter 7's `ClampTender` does and the replay driver's clamp hands stop it). Leave **3 m clear on each side of every seat** at shelf level | The partner's to stop: it works the clamp shut |
| Cover | Three rocks solid at the plane in the fight's ring, not at r 55: **(0, 1, 298), (-21.6, 1, 306.4), (21.6, 1, 306.4)** (r 32, between the shelves: bearings 180, 222.5, 137.5), each radius 2.8 to 3.5, drawn to y 2.5, kept clear of every shelf by 4 m | Her pressure wave stops at cover: a ship behind one is not struck |
| Lanes | Open flying at the plane from **r 21 to r 36** round the south half (bearings 80 to 280), nothing solid there but the three rocks and the anchor clamps; the shelves' floors are under it (y -0.8) and fly-over | Her sweep's gaps are 9 m or more at r 21 and wider beyond; a ship rolling out of one needs room |
| Door | As built (`MotherOfDoors`, the 148 x 92 box, `requiredFlag` `end.sorrelSeen`, `checkpointSpawn` `Clamps`) | Shared door tests |
| Camera | Nothing above the plane between C and the camera's side within **r 54** south of C but her head, the anchor clamps (top y 1) and the rocks (top y 2.5) | The flight camera is 19 m over and 17 m south of a ship at r 34 |
| Her way out | `BossExit` `MotherOfDoors` as built (0, 1, 400). On her release she lifts her head, turns north and goes up her road, her coil unwinding after her through the chain line, gone once her tail is past the road's mouth, about **12 s** (16 at most: `MotherTune.ReleaseSeconds`); the young (yours) follow her, see below | "She goes, and the young follow her out" |
| After | `SurvivorMotherGoes` is your `AfterTalk` on `bossdown.MotherOfDoors`, **delay 6 s** (she is half way up the road; nothing else is said, and the music stays out until the talk closes: the fight holds it quiet). The chains slack and the clamp open as planned | Manifest 8.4: "When she goes, SurvivorMotherGoes, and nothing else for a while" |
| The young | The young in the bore are yours (`FirstSight`, the `young` bark). On `bossdown.MotherOfDoors` move them up and out after her along her road (0, 1, 400) from about 3 s in, and gone by 12 s; a save with the flag loads them gone | Her release is mine, the nursery yours |

**For the Sorrel engineer (12:20):** `SorrelSecondSkyTests.NothingStandsAboveThePlaneOnTheCamerasSideOfHerFight` (line 193 on) fails on her own head ("Body at (-2.24, 4.49, 327.04)"): it walks every MeshFilter, and her head, raised at C as this contract has it, is the one thing meant to stand over the plane there (it is north of every lane, never between the camera and a ship). Leave out what is under an `EnemyBrain` (her head and coil), as the contract's Camera row does.

What she reads of the zone: every `BridleClamp` with `settledBy` `MotherOfDoors` (its seat, its index, its flag), the berth the party stands on (`ZoneController.BerthOn`) and which seat lies inside it, `clear.Sorrel_ClampN`, and every solid collider at the plane as cover. What she sets and logs, for your checks: `end.clamp1` to `4` (each saved at once, `CHECKPOINT_KEPT`), `bossdown.MotherOfDoors`; `MOTHER_WAKES opened=<n>`, `MOTHER_CLAMP_OPEN <i>`, `MOTHER_STRAINS`, `MOTHER_GOES`, `MOTHER_GONE`, `BOSS_MOVE MotherOfDoors <Sweep|Wave|Lash|Heave>`, `BOSS_SHIFT MotherOfDoors <2..4>`. Barks she plays: the party's `mother` (wide, then narrower), `bossopening.MotherOfDoors` as she first draws back with the next clamp's guns down, `bossphase.MotherOfDoors` at the first clamp, Hask's `hail.hum` through the fight, Iru's `hail.hum` after the first and third clamps; `clamp` is the hold's own. **Yours**: `young`, `SurvivorSecondSky`, `SurvivorMotherGoes`. Her music is **Boss of the Ages** (her definition); Sorrel's own comes back when `SurvivorMotherGoes` closes.

### As built (Sorrel engineer, to the contract above)

| Thing | As built |
|---|---|
| Boss | Spawner at C (0, 1, 330), yaw 180, `faceAsPlaced`, radius 0; `previewPoint` the valley's head (0, 1, 232), radius 100. Built only once `EnemyDef` `MotherOfDoors` exists (`Foe`); until then the log says `MOORING_FOE_STANDIN MotherOfDoors` and the zone has no door |
| Order | `Sorrel_Clamp1` needs `end.sorrelSeen`, `_2` `end.clamp1`, `_3` `end.clamp2`, `_4` `end.clamp3`; each `Clamp` arrow point needs the same |
| The four shelves | the table above (seats r 23, middles r 27, floor y -2.0, turned to C, works on the inner edge, foot spawn on the outer) |
| The clamp's works and the hold | the winch housing (solid, 3.2 x 1.25 x 0.9) between each seat and C; an empty `Clamp seat N` at each seat facing C. `Mother.StandClamp(seat, i)` is called on it (the hold, Mother's) |
| The fourth shelf | everything of it under `Shelf 4: The east clamp's works` (root at C, y -2), hung off its anchor clamp's arm, no spire, nothing static; its berth's frame inside the root. `Mother.StandStrain(root)` is called on it (pivot C, bearing 120 to 92). Nothing of the zone's from r 22 to 33 between bearings 85 and 125 at or near the plane: the crater's air there is open to its rim |
| The anchor clamps | `AnchorClamp`, 6.5 m, on r 17 on each shelf's bearing, top at y 0.9; the fourth's under its swinging root; each chained to Stillwater's clamp's jaws |
| The guns | `Sorrel_Clamp1` to `_4`, each box 26 x 8 x 26 at the plane round its seat (it holds the pad), two spawners on its anchor clamp's body 4.5 m either side along the ring, at the plane, `faceAsPlaced` facing out along the shelf's bearing; no checkpoint of their own. `ClampGun`, standing in as `FlakGate` |
| Cover | three rocks, sphere radius 3, at (0, 1, 298), (-21.6, 1, 306.4), (21.6, 1, 306.4), drawn to y 2.5 |
| Lanes | open air at the plane from the crown's edge to the rim (r 21 to 74) round the south half; nothing solid there but the cover and her |
| Camera | nothing above y 1.6 south of C within r 54, nor on the crater's south rim, but the cover rocks (`SorrelSecondSkyTests.NothingStandsAboveThePlaneOnTheCamerasSideOfHerFight`); the rim there is at the plane and carries no teeth |
| Her way out | `BossExit` `MotherOfDoors` at (0, 1, 400), facing north |
| After | `SurvivorMotherGoes`: an `AfterTalk` on `bossdown.MotherOfDoors`, delay 6 s, heard `end.motherFreed`. From `end.motherFreed`: nothing barks for 8 s (`QuietAfter`), the chain line and the east notch's chain are gone (walls and drawn), the slack chains, the young's road's marks and the warp on are drawn |
| The young | `The young in the voids` (root at (0, -9.5, 330); three on the crown under her, four in the moat, circling) carry a `LeavesAlong` on `bossdown.MotherOfDoors`: from 3 s they rise and go north up her road past (0, 1, 400), gone at 12 s; a zone opened with the flag has them gone |
| Door | `MotherOfDoors`, the 148 x 92 box, `requiredFlag` `end.sorrelSeen`, `checkpointSpawn` `Clamps` |

## Flight fights contract

*What the zone needs of the fights; the first combat engineer writes what is built in the second column. Until an id is defined each stands in as named (`LastMooringBuilder.Foe`, `MOORING_FOE_STANDIN`).*

| Thing | What the zone needs | As built (combat engineer) |
|---|---|---|
| Lane fights | Two encounters of two in the valley (`Sorrel_Lane1` at z 95, `Sorrel_Lane2` at z 175), each an 80 x 8 x 28 box spanning the valley (80 m wide at the plane), spawned at its north part. Compact interceptors keeping the clamp's approach: `Interceptor` (chapter 9's Strafer pair) by its own id | **`StillwaterInterceptor`** (defined November 14, `LastMooringBuilder.Enemies.Interceptor`): chapter 9's pair at this chapter's weight (run 150; chapter 9's 130 is under 9% of Idra's bar past Sync 27). **Your one change: `Foe("StillwaterInterceptor","Interceptor")`** at line 243. The boxes as built suit them: a pair wants about 40 x 28 m at the plane (the pinner holds 14 m off, the striker's lane is 22 m), spawned together within 24 m so they pair; nothing needed for cover, though a ridge rock between pinner and ship breaks the tractor line. Two at once per box, no checkpoint of their own |
| Clamp guns | `ClampGun` (stand-in `FlakGate`), two per clamp, anchored on the clamp's body, firing across the crater; must be cleared before the pad answers | **`ClampGun`** (defined): a curtain of three rows, each 6 m deep, out to 18 m ahead and 30 m across, the first and third then the second on a 3.2 s beat (1 s marked), burst 144, firing while a ship is within 24 m; integrity 3,300 (November 14, played through the replay driver's pad: at 2,200 a pair fell in about 16 s and her whole fight in 117 to 132 s, under her sheet's floor; 3,300 is her length model's 24 s a pair). Your `Foe("ClampGun","FlakGate")` already finds it. **Your one change: cross each pair.** Today both spawners face `s.bearing`, so the two curtains lie one over the other and behind both guns is a lee (the clamp's body side). Turn the gun at `s.anchor - side` to yaw `s.bearing + 35` and the one at `s.anchor + side` to `s.bearing - 35` (`across` is the shelf's right as it faces out, so each turns toward the other): their curtains then cross over the shelf, the pad stands in the second row of both (10.8 m ahead of each, 2 m off its middle), which is the quiet cell on the double beat, and behind either gun is in the other's first row, so there is no lee (`LastMooringCombatTests.TwoClampGunsCrossTheirCurtainsSoNeitherHasALeeAndTheirQuietRowsMeetInACell`). Room: nothing solid at the plane within 18 m in front of either gun but the cover rocks, and the box (26 x 26, as built) holding the pad. Docked, the guns go quiet (`...AClampsGunsGoQuietOverAHeroDockedOnItsShelf...`), but they are flown down before the pad answers anyway |
| Clamp Tenders | (Mother of Doors' fight steps them out on each dock) | **`StillwaterTender`** (defined): the Mother engineer's one word (`"ClampTender"` to `"StillwaterTender"` in `MotherTenders`). Needs of the shelf as her contract says: 3 m clear each side of a seat |

## What is in frame

- **Arrival**: the warp home lower left; below, Dace's empty outpost (two habs, the landing pad, the rover) on the haul road between the spines.
- **The canyon**: the red strata of both spines at the frame's edges, the haul road and the olive scrub 35 m down, his old beacons in the east branch.
- **The apertures**: two notches in the crater's rim, the drill mast on the ledge below them, and past them the crater opening.
- **The crater**: the clamp in the middle of the frame, its four chains running to the anchor clamps on their shelves, the nursery's glow 85 m down where the young move in the bore.
- **The young's road**: a narrow cut through the drilled voids, its walls lit along their edges.

## Evidence

Built in the editor (`LastMooringBuilder.BuildZones -mooring-zones SorrelSecondSky`, then `ZoneLook.Apply`) and looked at through the game's cameras (`LookDev.Render`, 27 views: `LastMooringBuilder.Sorrel.Views`, 25 from the flight camera and two on foot on a shelf).

| Pass | What was wrong | What changed |
|---|---|---|
| `sorrel-look-01` | It read as Sorrel's red ground, the young's road read (lit walls, rock between legs) and the clamp read as one white machine; but the canyon was a slot 30 m wide and 35 deep, so every approach frame was two dark cliff faces and a strip of floor with the outpost as specks; the shelves stood on stacked-crag columns out of an 85 m pit (red bead strings); the moat was black | The valley 80 m wide with slopes terraced over 16 m; the crown's ledge carried on under her ring on the south half so the shelves stand on legs 10 m over rock; rock tones lightened |
| `sorrel-look-02` | The valley read (ochre floor, scrub, the spines' shadows, the outpost and the drill from above); the floor was a plain ochre sheet; the narrow lanes (east, the apertures) and the crater's south side were deep slots, all wall | The floor dressed as chapter 1's (boulders, fallen ridge rock, strata, Sorrel's cyan ore; none on the haul road or the outpost, none in her coil's ring); scrub and the haul road stronger; the south moat at y -30, the east lane 32 m wide at y -12, the apertures at y -11 |
| `sorrel-look-03`, `-04` | The valley dressed and readable end to end (habs, rover, mast, drill, boulders, ore, scrub, the spines' shadows); the crater reads as one place (the raised crown under her, the four decks on legs round it, Stillwater's clamp behind, the terraced bowl); the way on and the lane east have floor in frame. The apertures stay mostly rock | Mother's hold and strain wired (`StandClamp`, `StandStrain`); points and lamps stood at the plane (the kit had lifted them onto the moon); the road 12 m wide so two hulls arrive side by side. Tests: `SorrelSecondSkyTests` (7) and the zone's shared cases, `focused sorrel-b3`: 23 passed, 1 ignored (no `DockingPad`) |

**Look pass (November 14, evening; the look engineer, stills `Builds/quality/look/sorrel-polish-03`, `-look-far 1000`).** Drawn ground and drawn rock only: the moon has no collider, and `Air`, the walls' stops, the cover's spheres, the shelves and every constant are as they were; routes 62 to 69 regenerated byte for byte the same. The valley climbs north from the second fight to a bench at y -12 where the drill stands over its bore, and on up to y -7 at the apertures, its walls narrowing to 7 m across its head: from the checkpoint the end of the valley is ground under the ships, not a 27 m wall. The apertures are cut to y -7 with near-vertical sides (floor 7 m wide of 11), the lane east to y -8.6 with walls 4 m across (floor about 24 m of 32). The cover rocks are Sorrel's own sandstone (`RidgeRock2/3`, generated): a broken ledge for each head, its top at 2.5, on a column of the same down to the crown's ledge, every mesh named `Cover ...`.

What is weak, plainly:

- **The apertures** are still notches 11 m wide in the rim: their frames show walls as much as floor, which only a wider `Air` would change.
- **The lane east's** north wall still fills the top of its frame (the camera looks at it).
- The strata texture on the tall walls reads as bands, not as chapter 1's broken ledges.
- **Her coil isn't in any still**: it is laid by her own code at load, and LookDev stills don't spawn her (`LastMooringBuilder.MotherLook` does).
- **The fourth shelf's navigation is baked where it stands**: when her strain has swung it, the partner's paths on it are where it was (the berth's to answer: the party never docks while it moves).
- No ambience bed of its own.
