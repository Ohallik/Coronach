# Last Mooring - the convoy on Stillwater's chains, and the Tollkeeper's gate

2026-11-14. Builder: `LastMooringBuilder.Mooring.cs` on `ZoneKit` (the Last Mooring engineer, stream F); the convoy's shared pieces are `LastMooringBuilder.Convoy` (same file, for the passage too). Chapter brief: [ch10-a-route-with-room-to-turn-back.md](../chapters/ch10-a-route-with-room-to-turn-back.md), "LastMooring". Who places what: [ch10-wiring.md](../chapters/ch10-wiring.md) sections 1, 2, 5, 6.1, 6.3. Script: [ch10-manifest.md](../writing/ch10-manifest.md) (`MaruSectionHeld`, `OrrinGateOpen`; barks `sectionhit`, `hail.mats`). Tests: `T/PlayMode/LastMooringZoneTests.cs`. **MAP_EYE_TEST: plan written before construction (this page); editor stills under "Evidence".** Every number below is an `internal static readonly` (or `const`) in `LastMooringBuilder.Mooring`: read it from there, never copy it. The build logs them on one line (`MOORING_COORDS LastMooring ...`).

## What the place is for

The last anchorage before the shared passage. Stillwater's anchor chains are strung taut at the flight plane between white pylons that rise out of the Compact's anchor works far below, and they divide the anchorage into broad arenas. The whole convoy is moored on them, every hull a place the player has been: the Halo's districts with their windows lit and Crawl engines bolted under their sterns, Tallow's station hull, Xylos's seed barges and their gardens, Driftreed's reed rafts lashed into a raft bay, the Crawl's treads, the Halo's ring segments under tow, Lastlight's rib-arched longhouses, Nacre's navigators' cutters, and Many Hands itself, the convoy's hub, in the middle of it. Lanternwick's lantern towers mark every gap through the chains. At the north end the Tollkeeper, an anchor-fleet carrier, lies across the way with its outriggers closed into a cage; past it the Continuance, Meret's ship, holds the passage's mouth.

The party flies north from the convoy's lee through four chain lines, each crossed at a gap: a pair of interceptors in the first arena, Many Hands' galley quarter struck as they pass it (Maru's weavers get it breathing), the maintenance islands to dock at (a free cradle on one, a Compact pair holding the other), a gap held by two crossed clamp guns, a second pair, then the Tollkeeper. When its gate motor dies the cage hangs open, the first hulls of the convoy go through under the party, and the dock at the Continuance's boarding spine is drawn.

**The rule: arenas divided by anchor chains.** Every edge a ship meets is a chain it can see (the outer chains are the zone's walls too); every way on is a gap in a chain line, marked by a Wick-folk lantern tower either side; the chains are cover (an interceptor's tractor line and a gun's shot stop at them). Nothing is drawn above the plane but the chains' links, the pylons' clamps on them and the guns.

## Rules of construction

- Everything flies at the plane (y 1). The convoy hangs below it on mooring lines from the chain over it, its tops at least 2.5 m under the plane (district roofs 3 m), drawn only: nothing of the convoy is solid and nothing of it moves a hero.
- The anchor works are the floor of every frame: a lattice of white girders and plate about 105 m under the plane, fogged a little, with the pylons rising out of it to the chains. They are drawn only.
- Every chain line is solid at the plane (a box 2.6 m wide and 12 tall along it) and drawn as the `AnchorChain` piece end to end, its middle a little under the plane, its top about 1.3 m over it. Pylons stand at the ends of every run and every gap, solid at the plane (radius 2.4).
- A shelf (an island, the arm) is a berth (wiring 6.1.1): floor at **y -2.0**, 3 m under the plane, so its works and rails stay under every hull that flies over it.
- Every gap takes two hulls side by side (a chain gap is at least 16 m; the guns' gap 18 m with the guns outside it); every fight's box spans the plane and its mouth is a whole arena wide.
- The camera's side: nothing above the plane within 18 m south of any lane a fight uses, but the chains (to 1.3 m over) and the guns. The Tollkeeper's carrier and the Continuance lie north of every lane and under the plane.

## Plan

North up; x east, z north. Chain lines are `====`, gaps `[  ]`.

```text
 z 548  ------------------- north limit (after the gate) ---------------------------------
 z 524           Dock — The Continuance (0, 524) on the boarding spine's head; spawn Continuance (10, 512)   [from end.gateOpen]
 z 520-700       THE CONTINUANCE, 180 m, bow south, under the plane (drawn)
 z 492           THE TOLLKEEPER, the carrier, 90 m across the way, under the plane (stand-in drawn after its fall)
 z 470  ######## THE CAGE: the gate line (solid until bossdown.Tollkeeper) ########################
 z 444-456       the arm's deck (berth Arm) at (0, -2, 450), the gate motor at its north end
 z 400-440       dividing chains at x -18 and 18 (the guns' lanes between them)
 z 392-464       TOLLKEEPER's door (box 110 x 8 x 72)
 z 386  ==================================[  -8..8  ]==================================  chain D
 z 372           checkpoint Gate (0, 372); the market district, a seed barge and rafts queued to go through first
 z 326           Mooring_Lane2 (two interceptors)
 z 270  =========[ -30..-12 ]=========================================================  chain C; Mooring_Gap: two clamp guns at x -35 and -7
 z 246           Island2 (44, 246): the Compact pair (optional)            Lastlight's longhouses, Nacre's cutters below
 z 228   Island1 (-44, 228): the free cradle
 z 172           the section struck crossing (end.sectionStruck): MANY HANDS below, its galley quarter at about (21, 186)
 z 160  ===============================================[ 12..30 ]=====================  chain B; seed barges moored under it
 z 110           Mooring_Lane1 (two interceptors); the Crawl's treads, the ring segments below
 z 60   =================================[  -8..8  ]==================================  chain A
 z 10            the Halo's three districts moored side by side under the arrival; Tallow's hull west
 z 0             ARRIVAL (0, 0); warp home (-10, -4)
 z -18  ------------------- south limit ----------------------------------------------------
        x -58 and 58: the outer chains, from z -18 to 470; after the gate x -44..44 to 548
```

## Coordinates (as built; `LastMooringBuilder.Mooring`)

| Place | World (x, y, z) | Id / flag |
|---|---|---|
| Arrival | (0, 1, 0), facing north | spawn `Arrival` |
| Warp home | (-10, 1, -4) | `Warp — The Hull of Many Hands` -> `ManyHandsHull`, spawn `Mooring` (`LastMooringBuilder.MooringSpawn`); open, except while the Tollkeeper's fight is under way: from its door's start to its fall it waits on `bossdown` and says "Stop the Tollkeeper's gate motor first: the convoy is still under its guns" (`FightHoldsTheWay`, November 14) |
| The outer chains | x -58 and 58 from z -18 to 470; the south chain z -18 | solid, drawn |
| Chain lines | A z 60, gap x -8..8; B z 160, gap 12..30; C z 270, gap -30..-12; D z 386, gap -8..8 (`Mooring.Lines`) | solid but the gap |
| Pylons | at the ends of every run and no further apart than 32 m between (`Mooring.Pylons()`), solid at the plane (box 4.8 x 12 x 4.8) | |
| `Mooring_Lane1` | box (0, 1, 110), 112 x 8 x 44; spawner (0, 1, 124), radius 4 | two `StillwaterInterceptor` |
| The section's crossing | box (0, 1, 172), 116 x 8 x 6 (the whole arena, 12 m past chain B's gap) | flag `end.sectionStruck`; the lead's `sectionhit` as it is crossed |
| Maru's talk | an after-talk 3.8 s after `end.sectionStruck` (the bark is read first) | node `MaruSectionHeld`, speaker `Maru`, heard `end.sectionHeld` (its own line) |
| Maru on the mats | radio hail at (21, 1, 150), radius 14, needs `end.sectionHeld` (flying back south through chain B's gap) | `hail.mats`, heard `end.matsHeard` |
| Many Hands | its hull copied out of `ManyHandsHull` at 0.3, its origin at (-4.8, -12, 186.6), bow north, roofs about y -12 | |
| Its galley quarter | (21, -12, 186): the breach (from `end.sectionStruck`), the air venting (until `end.sectionHeld`), Driftreed's mats and a weaver (from `end.sectionHeld`) | |
| Island 1 (west) | deck 18 x 12 round (-47, -2, 228); the free cradle at (-44, -2, 232) (`Cradle — repair and save`, saves `Island1Pad`) | berth `Island1` |
| Island 2 (east) | deck 20 x 14 round (44, -2, 246); its pair: box (44, -1.3, 248.6), 19 x 1.6 x 8 (4 m in from the south edge, under every hull), spawner (44, -2, 250.6), radius 2.2 | berth `Island2`; `Mooring_Island`, two `StillwaterWarden` (optional) |
| `Mooring_Gap` | box (-21, 1, 270), 44 x 8 x 36; guns at (-35, 1, 267) facing east and (-7, 1, 267) facing west, `faceAsPlaced`: hung on chain C's south face. On the chain's line (z 270, until November 15) each 2 m body lay wholly inside the chain's 5 m solid, which stops every shot, and nobody could break them (route 64 in the player) | two `ClampGun`, crossed |
| `Mooring_Lane2` | box (0, 1, 326), 112 x 8 x 44; spawner (0, 1, 340), radius 4 | two `StillwaterInterceptor` |
| The queue | the market district (-16, -8, 372), a seed barge (20, 370), two reed rafts (32, 368 and 378): each goes through the gate from `bossdown.Tollkeeper` (after 1.5, 5, 8, 10 s, over 34 to 38 s), at its own height, and is gone | `LeavesAlong` |
| Checkpoint | (0, 1, 372), facing north | spawn `Gate` |
| The Tollkeeper's arena | see "Boss arena contract" | |
| The gate | the line z 470: its flanks x ±40..±58 always solid (chain); its opening x -40..40 solid until `bossdown.Tollkeeper` (`The gate (until the Tollkeeper's motor dies)`) | |
| Past the gate | x -44..44 from z 470 to 548 (chains) | |
| The Continuance | drawn, bow south at z 528, its top at y -17; its boarding spine from a platform under the dock (y -2) down to its bow | |
| Dock | (0, 1, 524), drawn from `end.gateOpen` | `Dock — The Continuance` -> `Continuance`, spawn `Spine` (`Continuance.SpineSpawn`) |
| Back from the Continuance | (10, 1, 512), facing south | spawn `Continuance` (`Continuance.MooringBack`) |

## Berth contract

Wired to stream A's `Berth.Declare` (wiring 6.1.1), axis-aligned, floors at **y -2.0**, boxes the floor to 1.2 m over it (top y -0.8, under every hull), `refuseWhileAwake` true. Constants: `Mooring.Islands[i]` (`pad`, `foot`, `launch`, `flight`, `back`, `Box`, `berth`, `returnSpawn`) and `Mooring.Arm*`.

| Berth | Shelf (the pad says "Dock — ...") | Pad (flight) | Foot | Launch (on the floor) | Flight (on launching) | Return spawn | In a fight |
|---|---|---|---|---|---|---|---|
| `Island1` | The west maintenance island | (-47, 1, 228) | (-46, -2, 223.6) | (-53.8, -2, 223.6) | (-47, 1, 216) | `Island1Pad` (-39, 1, 212) | no |
| `Island2` | The east maintenance island | (44, 1, 246) | (45, -2, 240.6) | (36.2, -2, 240.6) | (44, 1, 233) | `Island2Pad` (36, 1, 229) | no |
| `Arm` | The Tollkeeper's arm | (0, 1, 450) | (1.4, -2, 445.4) | (-1.6, -2, 449.5) | (10, 1, 440) | `ArmReturn` (0, 1, 382) | yes |

Navigation: one bake of the three decks (`Walkable paths`), each checked from its foot spawn to its launch and its middle, island 1's to its cradle and the arm's to its motor (`ZONE_ROUTE_BAKE_OK LastMooring`). The arm's is baked where it stands out (`ArmOut`): the party only docks while it is out and still.

## Boss arena contract (the Tollkeeper)

*The Tollkeeper engineer's section (wiring 6.3 is the starting point): the Last Mooring engineer has built the room to 6.3 as it stands and published every number under "As built"; the Tollkeeper engineer corrects what the fight needs here and the room is rebuilt to it.*

### As built (Last Mooring engineer, to wiring 6.3, November 14)

Every number is a constant of `LastMooringBuilder.Mooring` (the carrier's middle is measured from its body at build: `TollkeeperAt`, logged `MOORING_CARRIER`).

| Thing | As built | What the fight does with it |
|---|---|---|
| The arena | the lane from chain D (z 386, gap x -8..8) to the gate line (z 470), x -58..58 between the outer chains; two dividing chains at **x -18 and 18 from z 396 to 416** (`ArenaChainX`, `ArenaChainFrom`/`To`), solid at the plane (5 m wide), so the south part of the arena is three lanes: **x middles -38, 0, 38** (`GunLanes`); from z 416 open to the cage, and nothing over the plane inside x -40..40 from z 420 to the gate (the fight's own row below) | Its guns fire marked lanes down those three (between the chains, which are cover) |
| Door | encounter **`Tollkeeper`** (built only once `EnemyDef` `Tollkeeper` exists: `Foe("Tollkeeper", null)`; until then the zone has no door and logs `MOORING_FOE_STANDIN Tollkeeper`): box **(0, 1, 428), 110 x 8 x 72** (x -55..55, z 392..464), its mouth the whole south face behind chain D's 16 m gap; `requiredFlag` `end.sectionHeld`; `checkpointSpawn` **`Gate`** (0, 1, 372), 56 m from the box's middle, outside it; `previewPoint` `Gate`, radius to the carrier | |
| Boss | spawner at **T = the carrier's middle, (0, -29.6, 492)**, yaw 180, `faceAsPlaced`, radius 0. The carrier as taken in is 90 x 31 x 29 m, laid across the way (length east-west), its top at **y -14**, spanning z 478 to 506: all of it north of the lane and under the plane | Its body is the boss's. The zone draws a stand-in of the same body there (`The Tollkeeper, as it stays`): **with the boss defined it shows only from `bossdown.Tollkeeper`** (the arena's solid stand-in after `Ending.ShutDown`; it has no collider, being under the plane); with no boss defined it always stands, so the gate is never an empty frame |
| The cage | hinges at **(-40, 1, 468) and (40, 1, 468)** (`HingeWest`, `HingeEast`): the gate's opening is x -40..40 (`GateHalf`); x 40 to 58 either side is chain, always solid. The gate's stop, a box x -40.3..40.3 at z 470 (`The gate (until the Tollkeeper's motor dies)`), is solid until `bossdown.Tollkeeper` | The fight places and moves its outriggers (40 m each) from those hinges; with the boss defined the zone calls `Tollkeeper.StandCage(root)` with the gate's root (`The Tollkeeper's gate`) right after building it (found by name: until that call exists the log says `MOORING_CAGE_UNSTOOD` and the fight stands its cage as it wakes). With no boss defined the zone stands the two shut across the opening (`The cage, closed`) and, from `bossdown.Tollkeeper`, hanging open (`The cage, hanging open`), drawn only; with the boss defined it draws neither |
| The braces | nothing of the zone's | Six on the fight's outriggers |
| The arm | its root **`The Tollkeeper's arm`** at **`ArmOut` (0, -2, 450)**, everything of it under the root and nothing static: the deck 6 x 12 (x -3..3, z 444..456), floor y -2, amber edges, a rail on the west, east and south edges; the gate motor's housing on its north end (3.6 x 1.4 x 1.2, solid) and an empty **`Gate motor seat` at (0, -2, 454.6) facing south** for the fight's motor (a hold or a break target); the boom back north under the carrier. Drawn in it would be at `ArmIn` (0, -8, 486) | The fight drives the root through `berth.Drive` (out, in, out on its cycle), stands the motor on the seat, shows the retract clock along the arm. Until it does, the arm stands out and its berth is live: if the fight wants the arm shut away before its bar is up, it hides or draws in the root |
| The arm's berth | `Arm` (table above): pad (0, 1, 450), foot (1.4, -2, 445.4), launch (-1.6, -2, 449.5) (5.1 m from the motor), flight (10, 1, 440), return `ArmReturn` (0, 1, 382), `inFight` true | `berth.Launch(why)` when the arm draws in; `berth.DockedSeconds` for the clock |
| Camera | the carrier and everything of the arm under the plane; the outriggers at the gate line; nothing over the plane south of the gate but chains (`LastMooringZoneTests.NothingStandsOverThePlaneSouthOfTheGateButTheChainsAndTheConvoyHangsUnderIt`) | |
| After | `OrrinGateOpen`: an after-talk on `bossdown.Tollkeeper`, delay 4 s, speaker Orrin, heard `end.gateOpen`. From `bossdown.Tollkeeper` the market district, a seed barge and two rafts leave chain D's queue and go through under the party; the gate's stop goes; the cage hangs open. From `end.gateOpen` the dock to the Continuance is drawn | |

### What the fight needs of the room (Tollkeeper engineer, November 14)

*The Tollkeeper engineer's (stream G); the Last Mooring engineer builds to it. Everything below reads the constants as built above; nothing in the room moves.*

**How the fight goes.** Its bar is three stages of the gate motor, a third each, and nobody strikes the carrier. Three times: break **one brace** (in flight; six, the player's choice) and the arm **runs out** of the gate (`ArmIn` to `ArmOut`, about 3 s) with the motor on its seat; **dock** at `Arm`; **stop one stage** of the motor on foot (a break target on `Gate motor seat`; from the second stage a `StillwaterTender` steps out to hold it, the partner's to stop); **launch** before the arm's clock runs out (a marked band along the deck: run out, the party is put out with a scrape and that stage's progress is lost, never the brace). From the second stage the cage **contracts on a clock**: both outriggers swing in from where they lie (20 degrees in from straight down the lanes) toward the gate, each sweep a marked fan from its hinge; a broken brace shortens its outrigger's sweep (the gap the bark names). The guns fire marked lanes down the lanes the dividing chains make. The third stage stopped is the bar's end.

| Thing | What the fight needs | Why |
|---|---|---|
| The cage | **One call in `Mooring.Build`, after `TheGate`, when the boss is defined:** `Tollkeeper.StandCage(root)` (`LastMooringBuilder.Tollkeeper`, this engineer's file): it stands a `TollkeeperCage` (Combat world part) at the gate with the two hinges, the arm's root and the motor's seat, and draws the two outriggers lying open toward the lanes under the plane (their bars' tops under y 0, the braces' couplings at the plane). Until the call is there the fight stands one itself as it wakes, and the cage is missing from the preview | The outriggers are the fight's, and must be there while the boss is on show |
| The cage hanging open | **With the boss defined, don't draw `The cage, hanging open`**: the fight's own outriggers swing to that pose at its fall and stay (a load after `bossdown.Tollkeeper` stands them so) | Two cages, one swinging through the other |
| The arm | As built. The fight moves its root `The Tollkeeper's arm` between `ArmIn` and `ArmOut` (only while its berth may move: nobody on it, no change, no conversation) and draws it **in** as it wakes; keep the bake where it stands out | "Launch before it retracts" |
| Over the plane | Nothing over the plane inside x -40..40 from z 420 to the gate but the outriggers' braces | The fans and the guns' lanes are read on it |
| The guns' lanes | As built: three lanes between the outer and dividing chains, the middle lane on to the gate | "Marked lanes between the chains" |

**Seen (Tollkeeper engineer, 14:06, stills `Builds/quality/look/toll-look-01`, `LastMooringBuilder.TollkeeperLook`):** the carrier reads; **the cage doesn't**. From the flight camera the carrier's two anchor chains (from about (±26, -24, 480) down to (±44, -129, 380)) and the cutters at (-40, -6, 448) and (40, -6, 452) lie over the outriggers' bars and braces in every still: white links the size of a district across the white bars, so the bars, the couplings a player breaks and the lamps of the cage's clock are lost under them. **Asked:** the carrier's chains from its two ends (x ±45) running outward past x ±60, and nothing drawn (chains, cutters, pylons' drums) within 8 m of x ±40 from z 420 to 470 or inside x -40..40 from z 420 to the gate, down to y -30. The carrier's body itself needs nothing from the zone: the cage lays it as it wakes (its pivot is its keel, so made at T it stood 15 m high), length across the way as the zone's own, its middle on T, and gives way to `The Tollkeeper, as it stays` the frame that shows.

**For route writers, the fight (Tollkeeper engineer):** three cycles, each: fly to a brace (at rest the couplings stand at the plane at (±34.5, 453), (±31.1, 443.6) and (±27.7, 434.2) (the bars lie 70 degrees off the gate line); from the second cycle the cage swings in and back on its clock, the braces with it), lock it and fire until `end.brace<n>` (n 1 to 3); about 3 s later the arm is out: fly to the pad (0, 1, 450) and press South `until docked:Arm`; walk to (0, -2, 453.1), lock the `Gate motor` (from the second cycle a Clamp Tender steps out beside it: lock and stop it first), strike until `end.motor1`, `end.motor2`, then `bossdown.Tollkeeper`; after each of the first two, walk to the launch (-1.6, -2, 449.5) and press South `until launched:Arm` within 5 s (run out, the party is put out with a scrape: still fine, nothing is lost). After the third the party is put off the arm by itself. `evadeTelegraphs` throughout: the guns' lanes and the cage's fans in flight, the motor's surge and the rake on the deck. The boss can't be struck, so the driver's own "lock what the bar names" doesn't apply: the route locks. Reconnaissance first: nothing here has been driven.

**Built to it (Last Mooring engineer, 14:00):** the dividing chains are cut back to z 396..416 (their clamp heads at z 416), so nothing stands over the plane inside x -40..40 from z 420 to the gate; `StandCage` is called with the gate's root once the boss is defined (by name, so either file can land first: `MOORING_CAGE_STOOD`); with the boss defined the zone draws no cage of its own, shut or open; the arm as built. The arena's cutters and the carrier's anchor chains are all under y -5.

**Built to the 14:06 request (Last Mooring engineer, `mooring-look-06`):** the carrier's two anchor chains now run out from its ends, (±47, -22, 492), past the outer chains to (±118, -129, 508), each with its anchor clamp half way; the arena's cutters stand at (-26, 404), (26, 410), (-53, 430) and (53, 444), tops y -6; the two pylons at the cage's hinges (±40, 470) are solid and drawn by nothing of the zone's. So nothing of the zone's is drawn inside x -48..48 from z 420 to the gate above y -30 but the arm (the fight's own, as built) and the gate flanks' chain, which ends at the hinges.

## Flight fights contract

*What the zone gives the fights; the first combat engineer writes what is built in the second column. Until an id is defined each stands in as named (`LastMooringBuilder.Foe`, `MOORING_FOE_STANDIN`).*

| Thing | What the zone gives it | As built (combat engineer) |
|---|---|---|
| `Mooring_Lane1`, `Mooring_Lane2` | a pair of `StillwaterInterceptor` each (stand-in `Interceptor`), spawned 14 m north of the box's middle, radius 4; boxes 112 x 8 x 44 across the whole arena at the plane. The chain lines 50 m north and south of each are cover a tractor line breaks on; no other cover inside an arena | |
| `Mooring_Gap` | two `ClampGun` (stand-in `FlakGate`) on chain C either side of its gap, hung on its south face at (-35, 1, 267) facing east and (-7, 1, 267) facing west (crossed; a ship south of the chain strikes them); the gap is x -30..-12, its edges clamp heads (solid, radius 2.4); box 44 x 8 x 36 round the gap | |
| `Mooring_Island` | two `StillwaterWarden` (stand-in `CompactWarden`) on island 2's deck (20 x 14 at y -2), spawned at (44, -2, 250.6), radius 2.2, facing south; their box covers the deck from 4 m in from the south edge the party lands on, under every hull. Optional (the ledger's 280) | |

## For route writers

- **Starts**: `Arrival` (0, 1, 0) facing north (from the hull or from Sorrel); `Gate` (0, 1, 372) (the Tollkeeper's checkpoint); `Continuance` (10, 1, 512) facing south (back from the Continuance); a save on an island or the arm loads at its `IslandNPad`/`ArmReturn`.
- **The way north**: chain A's gap (0, 1, 60) -> `Mooring_Lane1` (enter its box at z 92 or more) -> chain B's gap (21, 1, 160) -> the section's crossing at z 172 (fly through it at x 14 to 28: `end.sectionStruck`, the lead's bark, then `MaruSectionHeld` 3.8 s later, three lines: `end.sectionHeld`) -> chain C's gap (-21, 1, 270) through `Mooring_Gap` -> `Mooring_Lane2` -> `Gate` -> chain D's gap (0, 1, 386) -> the Tollkeeper's door (enter 3 m in, z 395 or more).
- **Docks**: island 1: fly to (-47, 1, 228) and press South until `docked:Island1`; the cradle is pressed from (-44, -2, 230.4); the launch at (-53.8, -2, 223.6) (`launched:Island1`). Island 2: (44, 1, 246), `docked:Island2`, its pair wakes 4 m in; launch (36.2, -2, 240.6). The arm: (0, 1, 450) while its fight lets it be out, `docked:Arm`; the motor's seat (0, -2, 454.6) is walked to at (0, -2, 453.4); launch (-1.6, -2, 449.5).
- **After the motor**: wait for `OrrinGateOpen` (4 s after `bossdown.Tollkeeper`, two lines: `end.gateOpen`), then fly north over the carrier to the dock (0, 1, 524) and press South: `Dock — The Continuance` -> `Continuance` at `Spine`.
- **Fights**: `Mooring_Lane1`, `Mooring_Gap`, `Mooring_Lane2`, `Mooring_Island` (optional), `Tollkeeper`.
- **Flags it ends with**: `end.sectionStruck`, `end.sectionHeld`, `clear.Mooring_Lane1`, `clear.Mooring_Gap`, `clear.Mooring_Lane2` (`clear.Mooring_Island`), `bossdown.Tollkeeper` and the fight's own (`end.brace1..3`, `end.motorStopped`), `end.gateOpen`; `end.matsHeard` if the party flies back south through chain B's gap after the section holds.

## The convoy's pieces, and where they come from

`LastMooringBuilder.ConvoyKit` (the Returning Passage may use it as it stands): `Hull(key, x, z, top, yaw, scale)` stands a taken-in hull (docs/art/intake.json's size) drawn only with its top at a height; `CopyOut(zone, lights, names...)` copies named objects out of another built scene as they stand there, only what is drawn; `ManyHands(at, scale, yaw)` is Many Hands' own hull (`The ship` of `ManyHandsHull`), as the Broken Ring's distant copy; `Districts` are the Halo's four districts by their chapter 9 names (copied out of `BrokenRing`). The copies are taken when Last Mooring is built: **a rebuild of `ManyHandsHull` or `BrokenRing` reaches Last Mooring only at its own next rebuild** (their meshes are saved in place, so a copy never loses its mesh; it can only be out of date).

| Place the player knows | As moored here |
|---|---|
| The Halo's districts | the repair quarter, the residential crescent and the tug dock side by side under the arrival (roofs about y -6); the market at the head of the queue at chain D |
| Tallow | its station hull along the west chain in the lee |
| The Halo's ring | three segments in an arc on the lee's east side, one deep under the first fight, a tug on it |
| The Crawl | three treads coupled in a line along arena one's west side; a fourth in arena three |
| Xylos | seed barges under chain B (five), either side of chain C's gap, in arena three and in the queue |
| Driftreed | the raft bay in arena three (eleven rafts lashed in rows) and two in the queue |
| Many Hands | the hub, under the second arena (copied at 0.3), its galley quarter under the section's crossing |
| Nacre | Bryn's navigators' cutters in a line on arena two's east side |
| Lastlight | three longhouses under their rib arches on arena three's east side |
| Lanternwick | a tower either side of every gap, and a float of three under the second pair's fight |
| The Compact | the anchor works far below, the pylons, the chains, the carrier and its anchor chains, cutters on the arena's chains, the Continuance |

## Evidence

Built in the editor (`LastMooringBuilder.BuildZones -mooring-zones LastMooring`, then `ZoneLook.Apply`) and looked at through the game's cameras (`LookDev.Render -look-far 1000`, 33 views: `LastMooringBuilder.Mooring.Views`, 30 from the flight camera and three on foot on the shelves).

| Pass | What was wrong | What changed |
|---|---|---|
| `mooring-look-01` | The convoy read where it was (Many Hands with its garden, arcade, tea room and galley sign; the seed barges; the districts; Tallow), and the chains read as a chain. But every frame was a forest of white pylons (one every 32 m on every run, each 106 m tall, converging on the works) over a lit white grid floor, and the convoy was a few hulls lost in it; the chain was laid at its taken-in 14 m across, so a ship would fly over links far wider than the wall it meets | Pylons only where runs meet the outer chains; gap edges a clamp head with a short drum; the works dark, deeper (y -135), a quarter of the plates left out, the haze darker; the chain at 0.38 (5.3 m) with its solid as wide; the convoy moored in groups of a kind, bigger, tops 4 to 9 m down |
| `mooring-look-02` | The convoy now fills the lee, arena one and the moorings; dark works with their lamps under it; the chains, lanterns and districts read. Still wrong: the works' girders ruled every frame in pale lines; the gap posts were white columns across the frame; arena three's middle and the Tollkeeper's lanes were empty works | Girders dark and every other line; pylon shafts dark; Lanternwick's float under the second fight, a raft row more; the carrier's two anchor chains down under its side lanes with an anchor clamp each, and Compact cutters on the arena's chains |
| `mooring-look-03`, `-04` | Arena three reads (the lantern float lit under the fight, the raft bay, the Crawl's tread, Lastlight's longhouse); the Tollkeeper's lanes have the carrier's chains going down under them. Lastlight's rib arches over the longhouses' roofs read from above as humps of ice. `LastMooringZoneTests` found both islands' work huts standing to y 0.2, where a ship flying over meets them | The arches at the longhouses' ends; the huts a 1.4 m tool locker (top y -0.6) |
| `mooring-look-05` | As built to the Tollkeeper engineer's first needs (dividing chains z 396..416, no cage of the zone's with the fight defined) | Their 14:06 stills: the carrier's chains and two cutters lay over the cage's bars |
| `mooring-look-06` | The carrier's chains run out from its ends past the outer chains; the cage's ground (x -48..48, z 420 to the gate) is clear works under the plane for its fans and lanes; the side lanes keep a cutter and a chain each. **The stills judged are this run's** | |

Tests (`T/PlayMode/LastMooringZoneTests.cs`, 8; each builds its broken case first and requires the check to find it): `focused mooring-f2`, 8 of 8, and the zone's shared cases 17 passed, 1 ignored (no dock a hull flies into until the gate is open); `EveryLockedExitStaysShut...AcrossZones` and `EveryConversationLockAndQuestStepIsWired...AcrossZones` passed. `mooring-f1` found both islands' huts in a hull's way (fixed).

**Look pass (November 14, evening; the look engineer, stills `Builds/quality/look/mooring-polish-01` (`-look-far 1000`) and the Tollkeeper's `Builds/quality/look/toll-polish-02`).** Drawn things only: no collider, way, prompt, fight, spawn, berth or constant moved (the pylons' solids, `Pylons()` and `PylonRadius` are as they were); routes 62 to 69 regenerated byte for byte the same. The islands are kept decks: plates of their own tones between the seams, a dark steel walk inside the rail with its tie-downs, a dark landing square under the pad's ring with white corner marks, the ground worn before the locker; on island 1 two of the Compact's hands at work (at Hal's anvil, at the open locker), drawn only and bent so no head comes near a hull flying over. The gap posts (every clamp head that isn't a pylon out of the works) are dark iron with a teal band, bolted straps and a ring of cold light round the rim, their drums dark: the way through is marked by the light, not by a white column. The Tollkeeper's couplings (`TollkeeperCage.Dress`, drawn children of each brace's node, gone with it when it breaks; the knot, its colours, its target and its hit shape unchanged) sit in a socket with a bolted collar round their middle, a cap on the crown and two jaws gripping them across the bar.

What is weak, plainly:

- The islands' crates and work lamps are the Decks' kit, a little small beside the deck.
- The gap posts' drums under the heads still widen the frame's dark mass at each gap.
- The couplings' dressing is grey on the outriggers' white and reads best open (amber): shut, at a distance, they are still mostly the ball.
- **The carrier's two anchor chains** pass under the arena's side lanes 25 to 60 m down, links 14 m across: they make the machine huge, and in the side lanes they fill the frame under the ships.
- **The Tollkeeper's arena has no boss in any still** (its body is its fight's, spawned at load); the stills show the zone's stand-in of the carrier and, with no fight defined, its cage shut.
- **The dock** is a white square platform over the Continuance's bow; the spine down to it is short and hardly seen.
- The north half of the overview is darker and emptier than the south (the convoy is moored in the lee; past chain D is the Compact's).
- No ambience bed of its own (guide 14: the lead's).
