# Many Hands - the ship the convoy became

2026-11-12. Builder: `ManyHandsBuilder.cs` (definitions, dialogue, the way in) and `ManyHandsBuilder.City.cs` (the zone) on `ZoneKit` and `GroundKit` (world engineer A, chapter 5). Chapter brief: [ch05-many-hands.md](../chapters/ch05-many-hands.md). Script wiring: [ch05-manifest.md](../writing/ch05-manifest.md). The outside of the same ship: [ManyHandsHull.md](ManyHandsHull.md) (world engineer B). **MAP_EYE_TEST: blockout written before construction (this page); editor stills and what is still weak are under Evidence.**

## What the place is for

Tallow's cabins, Teo's seed carriers, Driftreed's rafts and an old liner's passenger arcade, lashed to a freight hauler's keel and given a name nobody chose. It is the party's home for the rest of the game, so it has to read as a town people live in across districts, each built by the people who brought it: a Vael station's cabins and lamp gallery, an Okhari garden under carrier ribs, an Ilu raft bay on its own water, a liner arcade where Corra sells kettles, and the hauler's freight spine where everything arrives. Chapter 5 asks four things of it: a first circuit of three small jobs (the mooring, the vent, the chart), Hulljack's second part in the freight hall, the breach at the cabins, and a dinner of seven talks in the galley. Later chapters bring more people aboard (Idra's cutters at the dock, Hask's herd in the freight hall, Ishen's archive, Brekk and Tovra's forge, Bryn at the chart table, Orrin's board) and expect the same districts.

Users: the four peoples who live here (Vael from Tallow, Okhari growers, Ilu weavers and pilots, Wick-folk from Lanternwick), children of two of them running the spine, the queue of settlements' delegates at the dock, and the named: Keeper Abri, Pell, Ruva, Teo, Garo, Dabb, Jask, Binna, Hadda, Corra, Dov, Maru, Ona, Pim, Ilo, and in this chapter Dessik Varn, the *Steady Hand*'s captain and Meret Venn.

## Rules of construction

- **North is the back of every view.** The fixed camera (pitch 40, yaw 20, 19.5 m) stands south-south-west of the party and sees about 9 m north and 5 m south. Everything tall is on the north side of what it stands beside: the lamp gallery (a terrace 3.6 m up) is the town's north end, the garden's carrier ribs and the cabins' rows rise toward the north edge, the freight hall's tall walls are its north and east. South and west faces are low (a counter, a kerb, a rail), never a wall the hero stands in front of.
- **One deck, four makes.** The floor is one landform; what it is underfoot changes at every join, as the brief asks ("hull colours and deck plates change at every join"): riveted hauler plate on the spine and the dock, cream liner tile in the arcade and galley, teal station plate in the cabins and on the gallery, boards in the garden, woven mat on the rafts. The ground kit's three layers carry the three that are not the base plate.
- **The edge of the ship is the edge of the world, and it is drawn.** The deck stops at the rectangle's sides with space beyond: every edge a party can reach has a rail on a `Block`, and under the rail a drawn hull skirt runs down out of sight. Nothing invisible stands anywhere; the raft bay's hold and the garden's channel are walled by knee-high kerbs a body stops at (Rootmarket's rim rule).
- **Space is in the frame.** From the gallery's rail, the garden's east glass, the cabins' north windows and the dock's apron the camera looks past the deck into space: the convoy's other ships and the settlements' queue holding off, a moon below, the patrol's cutters once they latch on. Built and lit, not left to the sky panorama.
- **Townspeople live here.** Four peoples in their own bodies, at least three looks each, never two alike side by side; standing ones in static boxes off every way, walking ones on loops between districts (growers with trays to the galley, weavers with mats to the cabins, children of both peoples down the spine).
- **Places are named constants** in `ManyHandsBuilder.City.cs` (`Arrival`, `Crossing`, `HallDoor`, `ChartTableAt`, ...): the scatter's keep-off, the bake's must-reach list, LookDev's views, this page, the tests and a route all read the same numbers. A check refuses anything placed on a kerb, in water or over the edge before anything is built.

## Plan

North up, metres, deck at y 0 unless said. The deck runs x -78..78, z -66..58.

```text
 z 58 ---------------- space (rail) ------------------------------------------------------------- space ---
      CABINS (Tallow's)            LAMP GALLERY (terrace, y 3.6)              GARDEN (three carriers, ribs)
      x -78..-28, z 18..58         x -24..24, z 38..58                        x 26..78, z 18..58
      rows of cabin doors,         Pell's lamps, Abri's desk, Ilo's desk,     Marrow-1, -2, -3 beds under
      the breach row at the        CHART TABLE; stair up the spine's          their ribs, the channel, the
      north (sealed later)         north end (x -3..3, z 26..38)              seed racks off Marrow-3
                                         |  spine north
 z 14 -------------------------------- ARCADE (the liner) x -36..51, z -8..14 ------------- GALLEY x 53..78
      raft bay's north bulkhead    Corra's stall, Dov's counter,              z -18..16: Binna's hatch,
                                   Hadda's pump-and-press, the clock          the long tables, the vent
 z -8 ------- RAFT BAY (flooded hold) -------- | SPINE | ---- FREIGHT HALL --------- COLD STORES
      x -78..-12, z -50..-10: three rafts,     |x -7..7| x 13..51, z -52..-17         (raised roofs,
      rope walks, mats drying, Maru's tide     | cont- | F (32, -34.5); door on       tanks, vents)
      lines on the north bulkhead              | ainers| its south wall, x 20..28
 z -52 ------- container yard ------------- DOCK (the dock door at z -53) ---- FREIGHT YARD ----------
      x -78..-36                    landing pads, Abri and the queue           FreightHall spawn
 z -66 ---------------- space (rail) -----------------------------------------------------------------
```

Landmarks, one a district, each nameable from one still: the **dock door** (a slid cargo door, "MANY HANDS" chalked across it and a laddered gantry over the pads); the **gantry crane** straddling the freight spine; the **freight hall's** cargo stacks and its torn roof light; the **arcade clock** (the liner's promenade clock on its column at the crossing) and the copper **pump-and-press**; the **galley hood** and its vent stacks over Binna's hatch; the **carrier ribs** arching over the garden beds; the **drying racks** of dyed mats and Maru's tide lines in the raft bay; **Pell's great lamp** on the gallery; the **cabin rows** with laundry strung between them and, after the breach, a welded patch.

## What happens where

| Place | What | Flags, ids |
|---|---|---|
| Dock | Arrival (spawn `Arrival`); `Launch — Tallow Approach` (pad); Abri's welcome on a crossing (`AbriHandsArrival`, starts `ManyHands`); the queue of delegates; `Look at the dock door` (`SelaDockDoor`) | `hands.aboard`, `hands.doorRead` |
| Dock | Ilo from `hands.jobsDone` (a `FlagAll` of the three jobs) until `hands.iloAboard` (`IloArrives`); Dessik's claim on the radio straight after (`AfterTalk`, `DessikClaim`); `Launch — The Hull of Many Hands` from `hands.claimRead` | `hands.jobsDone`, `hands.iloAboard`, `hands.claimRead` |
| Dock | Meret by her cutter from `hands.dinnerDone` (`MeretParting`), gone at `hands.partingHeard` or `chapter5.complete` | `hands.partingHeard` |
| Freight spine | containers, the gantry; the riggers' cradle (free `RepairBay`, saves at `FreightHall`) | |
| Freight yard | Spawn `FreightHall` (part two's checkpoint; the hull's `Follow Varn in` lands here) | |
| Freight hall | See the contract below. Shut (a seal under a `FlagGate`) until `bossdown.Hulljack`. `DessikHall` on a crossing inside the door; the fight (`DessikVarn`); `DessikYields` (`YieldedCaptain`); Dessik in custody by the door after | `hands.hallEntered`, `bossdown.DessikVarn`, `hands.dessikYielded` |
| Arcade | Corra's stall (`ManyHands_Corra`; `CorraHands`, kettle beats), Dov's counter (`ManyHands_Dov`), Hadda at the pump-and-press (`Bench`, paid `RepairBay`; `HaddaHands`), the arcade clock | `kettle.*` |
| Galley | Binna at the hatch (`BinnaVent`, then the dinner's `BinnaDinner`); the long tables: from `hands.meretMet` to `hands.dinnerDone` Meret (seated, then standing at the galley door), Ruva, Ilo, Abri, Teo and Jask at table, Maru standing by the wall | `hands.ventFixed`, the dinner's seven flags |
| Garden | Ruva (`RuvaMooring`), Teo and Garo on the garden deck, Dabb at the pumps; `Look behind the seed racks` off Marrow-3 (`JaskFound`) | `hands.ruvaHeard`, `hands.jaskFound` |
| Raft bay | Maru (`MaruMooring`), Ona at the weaving bench, Pim; `Fit the running rings` at the bay's north-east hold wall once both are heard (`SelaMooringRail`) | `hands.maruHeard`, `hands.mooringSettled` |
| Lamp gallery | Pell (`PellChartLamps`); three `Sight the gallery lamp` points from the chart table once she is heard; `Open the route chart` (`SelaChartOpen`); Abri's desk and Ilo's beside it; `Read the chart` after the dinner (`SelaLampGallery`) | `hands.lamp1..3`, `hands.lampsSighted`, `hands.chartOpen`, `hands.chartRead`, `chapter5.complete` |
| Cabins | The breach row: sealed and patched from `bossdown.DessikVarn`, the *Steady Hand*'s cutters latched beyond the rail; the captain from `hands.dessikYielded` (`PatrolBreachSealed`), Meret behind him (`MeretArrives`) | `hands.breachSealed`, `hands.meretMet` |
| Jask | In the racks once found; then where `TeoStowawayDecide` puts him (garden, the raft bay ferry, the gallery); at table during the dinner if found | `jask.works*`, `jask.thanked` |

Who says what is routed by chapter 5's beats at the head of `SideStories.Beats`, each scoped to this zone, because chapters 3 and 4's beats for the same people (Dov's smokehouse order, Maru's beacons) would otherwise still be true aboard. A first talk aboard that sets no flag of its own is marked heard by `SideStories.HeardFlagFor` (`hands.<id>Aboard`).

## Freight hall contract

*Copied from [ManyHandsHull.md](ManyHandsHull.md), "Freight hall contract (part two)", written by the combat engineer; the room is built to it. Positions are from **F**, the boss's spawner in the hall, +z north. In this zone F = (32, -34.5).*

| Thing | What the fight needs | As built here |
|---|---|---|
| Combat in the hall | While the fight is on, the hall is a combat place: Shaped forms, enemies awake, skills live. The rest of the ship stays safe | A `FightPocket` (`S/World/FightPocket.cs`) over the hall: the zone is `GroundSafe`, and inside the box from `hands.hallEntered` until `bossdown.DessikVarn` it is `GroundCombat` (`ZoneController.FightPocket`) |
| Floor | Level and walkable over at least 34 x 26 m with F in its middle; nothing solid within r 6 of F | Hall floor x 13..51, z -52..-17 |
| Fixed cover | Four static cargo stacks 2.2 to 3 m tall, 2.5 to 4 m across, 9 to 13 m from F, none due south nor due north within 15 m | `HallStacks` |
| Loose crates | Six `LooseCargo` crates 1.4 to 1.8 m a side, 7 to 14 m from F, 3 m or more apart and from the stacks | `HallCrates` |
| Walls | North and east 16 m or more from F; south and west low | North wall z -17 (17.5 m), east x 51 (19 m); south and west 1.1 m |
| Light | Enough for every marking; six lights at most on the floor | four lamps |
| Boss | Spawner at F, yaw 180, `faceAsPlaced`, radius 0; `previewPoint` at the crossing, radius taking in F | |
| Door | Encounter `DessikVarn`, trigger over the floor, `requiredFlag="hands.hallEntered"`, `checkpointSpawn="FreightHall"`; `DessikHall` crossing just inside the door; one `Membrane` seal across the door (strands, carving while shut); a free cradle within a minute of the checkpoint | Door gap x 28..36 on the south wall, straight in front of F (the shared no-way-out test reads "through" along the line from the seal to the fight's middle); the riggers' cradle on the spine |
| Checkpoint | Spawn `FreightHall` outside the seal, within 60 m of the door, on navigation, nearer the door than `Arrival` | (32, -59): 6.5 m from the door; `Arrival` is 37 m |
| Ending | `Ending.Yields`; `YieldedCaptain` to Dessik's `Npc` (first node `DessikYields`), `heardFlag="hands.dessikYielded"`, Abri's people as guards | |
| The breach | The fight sounds the alarm; the breached cabins, patch and latched cutters are the world's, shown on `bossdown.DessikVarn` | |

### The way in from the hull, what breaks, and the staging (world engineer B)

*What the hall has to be for part one to end in it and part two to start in it, and how it is switched from a working freight hall to the fight and back. The hull side is built (`ManyHandsBuilder.Hull.cs`, [ManyHandsHull.md](ManyHandsHull.md)); the rows here are what the city builds to meet it.*

| Thing | What the city builds | Why |
|---|---|---|
| The way in | Spawn **`FreightHall`** as above. The hull's `Follow Varn in — Many Hands freight hall` (at the torn roof door, shown from `bossdown.Hulljack` until `bossdown.DessikVarn`) lands there; the load's autosave is the checkpoint between the parts. Nothing else of the hull's names a spawn here but `Arrival` (`Dock — Many Hands`) | Part one ends with the party following the frame in; a defeat in part two retries at `FreightHall` with part one won |
| The way out to the hull | `Launch — The Hull of Many Hands` lands at the hull's spawn **`Arrival`** (by the stern dock pad) | The only arrival the hull keeps for a launch; `Hulljack` is its fight's checkpoint |
| What breaks | Only the roof door, and from above: the frame comes down through it at F. In the hall that is the light falling through the hole onto F and the door's two torn leaves (drawn only, against the north and east walls, beyond r 16 of F, so the floor and the crate lanes stay clear). The loose crates move in the fight and a reload puts them back | The hull shows the same door shut, torn and patched by the same flags (`Roof door shut/torn/patched` in the hull's scene) |
| The window | One `FightPocket` over the hall (as built: box centred (31.8, 3, -34.8), 38.4 x 8 x 35.4, so x 12.6..51, z -52.5..-17.1, y -1..7, `isTrigger` set by the builder; its south face is the door's line, so the seal at z -52.1 is just inside it and the `FreightHall` spawn at (32, -59) outside): `from="bossdown.Hulljack"`, `until="bossdown.DessikVarn"`, `alarmBoss="DessikVarn"`, `alarmPhase=2`. From the frame's arrival, not from `hands.hallEntered`: the hall has to show the frame come through its roof from the moment it has (the hull's follow way is open then), and a party walking in is in its fighting form before Dessik speaks. The encounter still waits for `hands.hallEntered`, so the fight starts at the talk | The hall's three states and the combat place follow the same two flags, so they can't disagree |
| `before` | The hall at work: two hands at the stacks (townsfolk, off the walk from the door to F), a hand truck, a pallet hanging from the roof hoist over F (drawn only), the roof door's two lamps lighting F | A working freight hall until the frame arrives |
| `during` | The light through the torn roof: a soft bell of light over F and a pool round it (one of the four lamps moved under the hole), the torn leaves, scorch round F outside r 6. The seal and the frame are the encounter's, not the staging's | The fight's room |
| `alarm` | Blue-white strobes on the north and east walls, a light each at most (the floor renderer's six). **Never orange, red or purple light on the floor** (D217: those colours there mean a marking) | The cabin alarm at Dessik's half-way shift, which the fight sounds and the party can't answer |
| `after` | The hall at work again: the hands back, a work lamp over F instead of the bell, plates over the hole | What the fight left |
| Cover and the bake | The four stacks static and the same in all three states, in the navigation bake; the six crates `LooseCargo`, not static, not in the bake | A stack hidden outside the fight would leave a hole in the paths |
| **The seal (open finding, for the city engineer)** | A shared-test run on the tree of November 12 walked a body through `Freight hall seal` with the fight on. It is the city's piece (`ManyHandsBuilder.CityLife.cs`, `FreightHallFight`: a `Membrane` made open at (32, 2, -52.1) since the door moved to face F (it was at x 24), 8.4 wide, 4 tall, its renderers removed, a carving obstacle added disabled). While `DessikVarn` is begun and not cleared it must stop a body from **both** sides across the whole door gap (x 28..36, the floor to 4 m): its `BoxCollider` solid (not a trigger), on the `Membrane`'s own object or a child so `SetOpen` switches it, not reached by any `FlagGate` or the cargo door's gate, and its obstacle carving. Once the fight is over it must be gone: `EncounterVolume` opens it at the last death and, from a save with `clear.DessikVarn`, at load (`FinishSaved`) | "The party cannot leave the hall" (manifest 8.9), and the shared door tests (no way out mid-fight, retry at the door) |

## Sightlines and what is in frame

- A frame is about 19 x 15 m of floor. Every one holds a district's own furniture, at least one person, and something of its make underfoot; the measure is `python tools/look_density.py` at or under .22 on every still (the brief asks .15).
- The gallery is the back of the town: from the crossing the stair and the great lamp stand at the head of the spine. The gallery's rail looks north into space.
- Nothing 7 m across and over 3 m tall stands between the camera and a named person, a prompt or the hall's floor.

## Coordinates for routes

Plan x, z; the deck is at y 0 (the gallery 3.6, the rafts about 0). Filled from the builder's constants and checked against the build's `MANYHANDS_COORDS ManyHands` line.

| Place | Plan (x, z) | Id, prompt, flag |
|---|---|---|
| **Arrival** (the dock) | (-4, -60) | spawn `Arrival`; `Launch — Tallow Approach` pad (-16, -61.5) to its spawn `ManyHands` |
| Abri's welcome (crossing, 30 m wide across the dock) | (-2, -57.4) | `AbriHandsArrival`; `hands.aboard` (the crossing's own `hands.welcomeHeard`) |
| Keeper Abri on the dock | (4, -56.4) | walks up the spine to her desk once `hands.aboard` |
| The dock door | (11.5, -56.6) | `Look at the dock door`, `SelaDockDoor`, `hands.doorRead` |
| Hull pad | (8, -62) | `Launch — The Hull of Many Hands`, shown from `hands.claimRead`; to the hull's `Arrival` |
| Ilo on the dock | (-10, -57.6) | from `hands.jobsDone`; `IloArrives`, `hands.iloAboard`; then `DessikClaim` on the radio, `hands.claimRead` |
| Meret by her cutter | (-22, -58.6); cutter (-29, -62) | from `hands.dinnerDone`; `MeretParting`, `hands.partingHeard` |
| Freight yard (checkpoint) | (32, -59) | spawn `FreightHall` |
| Freight hall door | (32, -52.5), gap x 28..36 | shut (`Freight hall cargo door`) until `bossdown.Hulljack` |
| Dessik's talk (crossing, 8 m wide, inside the door) | (32, -50.6) | `DessikHall`, `hands.hallEntered` (needs `bossdown.Hulljack`) |
| **F**, the boss's spawner | (32, -34.5) | encounter `DessikVarn`; `bossdown.DessikVarn`; `DessikYields`, `hands.dessikYielded` |
| Dessik in custody (later visits) | (16.2, -48.2) | `DessikHeld`, `DessikHeldPost` |
| The riggers' cradle (free) | (-10.6, -44.6) | `The riggers' cradle — repair and save`, saves at `FreightHall` |
| The gantry (spine) | (0, -32) | |
| **The crossing** (arcade, the spine's middle) | (0, 3) | spawn `Arcade` at (26, 4) |
| Corra at her stall | (-24, 7.6) | counter `ManyHands_Corra`; `CorraHands`, kettle beats |
| Dov at his counter | (-12, 7.6) | counter `ManyHands_Dov`; `DovHands` |
| Hadda at the pump-and-press | (18, 7.6); bench point (20.6, 9.2) | `Fabricate — The Pump-and-Press` |
| Hadda's mending frame (paid) | (30, 11), stand at (30, 9.8) | `Hadda's mending frame — repair and save`, saves at `Arcade` |
| The arcade clock | (10.5, 10.6) | |
| The mooring rail | (-19, -9.6) | `Fit the running rings` (needs `hands.bothHeard`), `SelaMooringRail`, `hands.mooringSettled` |
| Galley door / Meret standing | (54.8, 6.6) | `MeretDrawings`, `hands.dinnerDone` (from `hands.sorrelNamed`) |
| Binna at the hatch | (66, 11.6) | `BinnaVent` (`hands.ventFixed`), `BinnaDinner` (`hands.dinnerSat`) |
| The dinner table (middle row) | Meret (61.8, 0.9), Ruva (59.4, 0.9), Jask (64.2, 0.9), Teo (66.6, 0.9), Ilo (61, -3), Abri (63.6, -3); Maru standing (75.6, 0) | `MeretStillwater`, `RuvaDinnerNumber`, `IloFootnotes`, `MeretSorrel` |
| Ruva in the garden | (42.6, 31) | `RuvaMooring`, `hands.ruvaHeard` |
| Teo on the garden deck | (57.6, 40) | `TeoHands`; stowaway beats |
| Garo; Dabb at the pumps | (62, 28); (74.6, 25.6) | `GaroHands`, `DabbHands` |
| Behind the seed racks | (73.6, 49.2) | `Look behind the seed racks`, `JaskFound`, `hands.jaskFound` |
| Maru on the ferry raft | (-25, -21.4) | `MaruMooring`, `hands.maruHeard` |
| Ona at her bench; Pim | (-50, -35.2); (-56, -18.6) | `OnaHands`, `PimHands` |
| Raft walks | quay (-14.5, -24) to (-19.5, -24); ferry raft to weavers' raft (-29, -29) to (-42.5, -32); to the mat raft (-32.5, -22.5) to (-49, -21); north quay (-58, -11.5) to (-58, -15.5); arcade (-24, -11.5) to (-24, -19) | |
| Stair foot; stair top | (0, 23); (0, 38.6) at y 3.6 | |
| Pell on the gallery | (8, 47) | `PellChartLamps`, `hands.pellAboard` |
| The three scopes (sight the lamps) | (-2.9, 47.2), (0, 49.5), (2.9, 47.2) | `Sight the gallery lamp`, `hands.lamp1..3`, `hands.lampsSighted` |
| The chart table | (0, 45.2) | `Open the route chart` (`SelaChartOpen`, `hands.chartOpen`); after the dinner `Read the chart` (`SelaLampGallery`, `chapter5.complete`) |
| Abri's desk; Ilo's desk | (-11, 43.6); (-17.4, 43.6) | |
| The captain at the sealed cabins | (-60, 50); Meret behind him (-61.6, 51.8) | from `hands.dessikYielded`; `PatrolBreachSealed`, `MeretArrives` |

A first circuit from the dock: (-4, -60) -> (0, -55) -> (0, -40) -> (0, -20) -> (0, 3) -> (0, 20) -> (0, 23) -> (0, 38.6) -> (0, 45.2). The garden: (0, 20) -> (16, 20.4) -> (27, 24.2) -> (44, 24.2) -> (44, 52). The raft bay: (0, -24) -> (-14.5, -24) -> (-24, -24) -> (-31, -23) -> (-32.5, -22.5) -> (-49, -21); to Ona over the weavers' walk, (-24, -24) -> (-29, -29) -> (-42.5, -32) -> (-44.75, -32.5) -> (-44.5, -33.6) -> (-48, -34.3). The galley: (0, 3) -> (50, 3) -> (56, 2.4) -> (66, 3.6); to Binna round the long tables' west end, (56, 2.4) -> (55.6, 4.6) -> (55.6, 10.4) -> (64.8, 10.4); to the south table (56, 2.4) -> (56.3, -3.4) -> (58.4, -5.2) -> (74, -5.2). The cabins: (0, 19.6) -> (-16, 20.4) -> (-30, 20) -> (-52, 20) -> (-52, 50.2) -> (-58, 50.2). The freight hall: (0, -57) -> (12, -59.4) -> (32, -59) -> (32, -50) -> (31.4, -47.6). These are the builder's `Ways`; the build checks every end and every named person for a whole path from the arrival (`MANYHANDS_ROUTES_OK points=72`).

## Room for later arrivals

*For whoever builds chapters 6 to 10's people aboard (from each chapter's brief and manifest). Plan x, z; sizes are the free floor as built on November 12, clear of every way (`Ways`), sign and named person. Each arrival is one `Npc` (or body) under a `Whereabouts` from its chapter's flag; nothing here is built yet. The city's ways must stay clear (the builder refuses anything within 1.8 m of one), so a new piece goes inside these rectangles or a way moves with it.*

| Chapter | Who or what | Where | Free floor |
|---|---|---|---|
| 6 | Idra's three rescue cutters lash on (`IdraLiftFigures` on a crossing at the dock) | Berths beyond the south rail, x -60..-34, z -68..-80 (drawn only, off the deck, as the latched patrol cutters are); the visitors' pad (-29, -62) is free once Meret's cutter has gone; the talk on a crossing across the dock at z -57.4, as Abri's welcome is | Apron x -36..-20, z -65..-57.5: 16 x 7.5 m, less the parked loader (-29.6, -55.6) |
| 6 | Hask, the herders and the rime-beasts ("I've put them in the freight hall") | The freight hall's west half: a pen x 14..30, z -50..-20 behind a low rail, Hask at its gate (24, -46). The hall is the fight's only while it is on (`FightPocket` `after` set); its four stacks can go under the same flag | 16 x 30 m (Dessik's custody corner at (16, -48) stays) |
| 6 | Iru with Pell on the lamp gallery | (13, 47), east of Pell | Gallery east, x 8..22, z 40..50: 14 x 10 m, less the lamp rack at (21, 44.6) |
| 6 | Idra's nineteen crew, eating standing with their backs to a wall; Kott and Sibb | The galley's east window wall, x 74.5..77, z -13..11 (Maru's dinner place at (75.6, 0) is free after the dinner); Kott and Sibb at the south table (59..72, -8.4) | 2.5 x 24 m standing room |
| 7 | Ishen and his handcart of books at the dock, then his corridor ("the archive") | Dock (-20, -61); the archive in the cabins' lane B-C, east block, x -48..-30, z 37..43.5, two cabin doors in row B east made the archive's | 18 x 6.5 m |
| 7 | The gardener working a planted bed | Marrow-1's north end: the jar on the bed at (36, 50), the gardener in the aisle at (40.6, 50) | Aisle x 39.5..48.5, z 46..53 |
| 7 | The release posted in the galley; Oriel's lantern on the gallery | A notice on the hatch wall's front, x 58..62, z 16.1, 1.5 m up; the lantern at the gallery's west end (-17, 55) | 4 x 3 m by the north rail |
| 8 | Brekk off the dock, then at Tovra's forge; Tovra and sixty smiths in the freight hall | Brekk's arrival at the dock (-12, -57); the forge against the hall's east wall, x 40..50, z -40..-20, Tovra at (44, -30), Brekk at (40, -26) | 10 x 20 m (the herd's pen is the hall's west half) |
| 8 | Drask in custody ("where he can't give an order") | The freight yard's east end, a brig at (48, -60); apart from Dessik in the hall | Yard x 36..51, z -65..-56: 15 x 9 m, less the loader (44.4, -57.2) and the pallets (40.4, -57.4) |
| 9 | Oda and her mother in a cabin "with a wall" | Row C, east block, the cabin at (-40, 45); Oda in the lane at (-40, 42) | Lane B-C east (shared with the archive's doors) |
| 9 | Bryn and her navigators at the chart table | Round the compass rose at radius 4 from (0, 47.2), between the scopes: (-3.6, 44.4), (3.6, 44.4), (-4, 49.6), (4, 49.6), (0, 51.6) | The rose's ring, 10.6 m across |
| 9 | The tug Welcome Back, "lashed where the cutters were" | The cutters' berths beyond the south rail | |
| 9 | Ket's portable bench | The arcade's east end in front of a frontage bay, (40, 10) | 4 x 3 m |
| 9 | Abri's wall of names ("start at the door and go round") | The dock door's leaf (x 8.2..15.8, z -55.3), then round the yard along the freight hall's south wall, x 16..28 and 36..52, its outer face at z -54.1 | 36 m of wall |
| 9 | Orrin aboard, then (chapter 10) his board with twelve standing before it | The east forecourt: the board against the gallery's south face at (16, 37.6), the herb stall's place (it moves under the same flag); standing room x 9..24, z 27..35 | 15 x 8 m |
| 9 | Idra in the raft bay | The north quay (-46, 0) | Quay x -76..-38, z -9..15, less the racks and goods |

## Arrivals as built

2026-11-12. `ManyHandsBuilder.Arrivals.cs` (class `Hands.Arrivals`), called from the city's build twice: `Arrivals.Build(kit)` beside the school raft in `CityDetail.cs`, and `Arrivals.Settle(kit)` straight after `Story(kit)` in `City.cs` (it needs the city's people and the freight hall's staging to stand). Its views are appended to the city's own (`Hands.Views` in `CityLife.cs`), so `LookDev.Render` takes them; `ManyHandsBuilder.ArrivalStills` (`-arrivals-run <name>`, `-arrivals-stages ch5,ch6,...`) renders the city at each stage of the story without saving anything. Sounds: `tools/manyhands_arrivals_ambience.py` (synthesised) writes `AMB_HandsHerd.ogg` and `AMB_HandsForge.ogg`.

**How an arrival comes.** Each is one root named `Arrivals: <what>` under a `Whereabouts` decided at load (`live` off) from the flag of the talk that brings it, so it is there when the party next docks and never appears in view or on anybody. Roots are saved switched off, so nothing of them is in the navigation bake; every solid box in one carries a carving `NavMeshObstacle`, so the paths change once it is there. What stands in the freight hall is also away from `bossdown.Hulljack` until `bossdown.DessikVarn` (the fight's window, D252), whatever later flags say. The build refuses (`Plan`, then `Settle`) anybody without a name within 2 m of a way, on a sign or a player's spot, between the camera and someone with a name, on a walker's loop, beside a townsperson in the same body; anything standing in what the city built (a scattered kit piece in the way is moved off from the arrival's flag on, logged `MANYHANDS_ARRIVAL_MOVED`); a walker's loop running into an arrival; anything 3 m wide and over 3 m tall on the camera's line to a talk or a spot.

**The ways, held (`Arrivals.NothingStandsOnTheWays`, the last step of `Settle`).** As `XylosBuilder.Rootmarket.cs` holds its streets: a solid box within 0.9 m of a way's line (its footprint, not its centre), a townsperson within 2 m, a walker's loop within 1.6 m, each refused by name, with every arrival switched on. Walk decks and rails, triggers, moving bodies and the walkers themselves are skipped; a scattered kit piece the check finds is dropped and logged (`MANYHANDS_WAYS_DROPPED`: today the planter at (-2, 8.7), the solar arrays at (-44.5, 22) and (-56.1, 52.2), the fan shrubs at (62.4, 45.9) and (41.8, 47), a pallet at (-8.3, -26)). What the route writer found in the city with it, and what changed: Binna's way now goes round the north table ((55.6, 4.6), (55.6, 10.4), (64.8, 10.4)); the galley's south way bends off the middle table's corner ((56.3, -3.4), (58.4, -5.2)); the garden's way runs at z 24.2, off the beds' fronts, and Dabb's west pump stands at (40.6, 22.85) south of it; the raft bay's pulse lamp moved out of the weavers' landing to (-55.6, -36.4) and the raft's way runs on the walk's axis, not inside its rim; the arcade's line posts at x -5.2 and the spine's west lamps 1.2 m south of their old places; Ilo's dock spot (-10, -57.6), off the way; the seedling grower's loop at x 47.6 and the tray run round (6.6, 10.6), off Jask's post; the errand check now counts the named people, and every named person (switched off or not) is cut out of the bake by a carving obstacle, so a person's place is never a hole in the paths while they are elsewhere. In `SideStories`, Corra's first meeting (`CorraHands`) is ahead of her after-dinner beat, so `KettleTrade` is reachable whichever comes first.

| Ch. | Arrival (root) | Where (plan x, z) | Comes with (gone at) | Who is there, what changes |
|---|---|---|---|---|
| 6 | Idra's cutter on the visitors' pad | (-29, -62), gear at (-34.6, -58.9) | `hoar.sleepLearned` | Her own cutter (IdraFlight, 4.8 m) on chocks where Meret's stood; a winch drum, a folded stretcher cage, coils of line, a striped crate; a rescue hand coiling line |
| 6 | Idra's cutters lashed off the stern | (-51, -69.4), (-41, -69.8), drawn under the stars | `hoar.sleepLearned` (`home.tugFound`) | Two more of her hull, lines over the south rail to bollards |
| 6 | The herd in the freight hall | Pen round the hall's west half: fence x 12.9..27, z -22.2..-48.2, Dessik's custody corner and the 3.8 m along the south wall to it left out; gate x 27, z -45.8..-48.2 | `chapter6.complete`, away in the fight's window | Bone-post fence with hides on the camera's side, a shut gate with horn lamps, strewn straw, a rib trough on the west wall, a moss rack, tied bales under a hide on the two west cargo stacks, a herders' horn lamp and its light; six rime-beasts walking the pen (`RimeHerd`, never startled); three herders (two Kept coats); the herd's sound. The two loose crates in the pen's half go from the same flag |
| 6 | Iru's lamps on the gallery | (17.8, 48.8), east of Pell | `chapter6.complete` | Lastlight's shrine lamps on bone stands in an arc, a low bowl lamp, two hide mats, a post of lamp-hooks; a light |
| 6 | Idra's crew in the galley | Window wall x 75.9..76.1, z -10.4..6.6; Kott (64.4, -7.4) and Sibb (67.4, -7.4) at the south table | `chapter6.complete` | Five rescue hands eating standing (two still in their frames), Kott with his four bowls, Sibb; their helmets and line at the wall |
| 7 | Ishen's archive in the cabins | Row C's two east cabins, fronts x -34.8..-30, z 43.6; desk (-31.3, 40.6) | `chapter7.complete` | Bookcases over the two cabins' windows, ARCHIVE on a board at the roof's edge, crates of books, a reading desk with its lamp (and light), someone reading |
| 7 | The gardener's bed by Marrow-1 | Planter (40.7, 51.7) in the aisle's north end | `chapter7.complete` and `quest.EmptyGardens.complete` | A pale stone planter, raked rings, the jar of seed half buried, the gardener's low light |
| 7 | The release posted in the galley | Hatch wall's front, centre (57.2, 16.1), 1.7 m up | `chapter7.complete` | "TAKE ITS WEIGHT. / WAIT FOR THE SLACK. / LET GO TOGETHER. / Then give it room." in chalk (one of Ishen's four copies); someone reading it |
| 7 | Oriel's lantern on the gallery | (-17.6, 56.3), the fourth of the gallery's row of lamps | `chapter7.complete` and `quest.LumesLantern.complete` | A Lanternwick post with the far end's glass; a cold light |
| 8 | Tovra's forge in the freight hall | East wall: hearth (49.6, -30), anvil (46.2, -29.6), quench (46.9, -34.6), mending table (43, -34.2), bar rack (50.2, -36.6) | `chapter8.complete`, away in the fight's window | Brick hearth with coals breathing (`LampRhythm` on its light and coals), hood and flue up the wall, bellows, anvil with a bar on it, sparks, smoke, steam off the quench, racks of cage bars and ribs, release gear on a pallet; three smiths; a queue of four with a kettle, a pot, a valve; two more smiths with `quest.QuietHands.complete`; the forge's sound. The loose crate on its floor and the freight crew's hand there go from the same flag |
| 8 | Drask's cell on the freight yard | Bars x 39.6..45, z -60.85..-58.25 against the north face of the container at (42, -62.2) | `chapter8.complete` | Barred on three sides and over, locked; a cot and a stool with his bowl; a lamp on a post; one of Abri's watch |
| 9 | Oda's cabin | Row C cabin x -42.7..-39.5 (front z 43.6) | `home.odaFound` | "904" framed over its window, the crack through the nine; bundles strapped as on a hauler, a trunk |
| 9 | Oda and her mother at their cabin | Oda (-41.4, 42.4), her mother (-39.7, 43) | `home.odaFound` (`home.returned`) | Oda's body (her talk waits on the pending script) |
| 9 | Bryn's navigators at the chart table | Round the compass rose: (-5.6, 46.4), (-2.6, 50.6), (0, 52), (3, 50.3), (1.8, 53.6); Bryn (4.4, 47.2) from `home.tugFound` | `home.navigatorsFound` | Five navigators with charts, facing the table; Bryn's body |
| 9 | The tug Welcome Back off the stern | (-46, -71.6), drawn under the stars | `home.tugFound` | A squat green working hull with fenders, bridge and tow boom, its line to a bollard on the rail (Idra's lashed pair go at the same flag) |
| 9 | Keeper Abri's wall of names | The dock door's leaf (z -55.34) under and over the chalk already there, then the hall's south wall (z -54.1) x 16.8..27.6 and 36.4..46.4 | `home.noticeHeard` | Thirty-six names in chalk, read at 26 px or more; a chalk bucket by the door; someone writing; three standing by theirs |
| 9 | Nacre's families in the freight yard | Strip x 14.6..30, z -63.2..-61.6 | `home.noticeHeard` | Bundles and bedrolls, three families |
| 9 | Orrin's board under the gallery | Board (14.8, 37.6) between the banners; Orrin (14.8, 36.1); the herb stall moved to (21.6, 33.6) | `home.launched` (SORREL on it from `chapter9.complete`) | "CINDER HALO / DEPARTED, / ALL SECTIONS" in chalk; Orrin's body |
| 9 | Ket's bench in the arcade | (40, 10.2), Ket (40, 11.5) | `quest.PortableBenches.complete` | Hal's folding bench opened out with its tools and lamp; Ket |
| 7, 8, 9 | Someone else in the bodies of the people who came | Where the city's own townsfolk stood | `chapter7.complete` (Ishen's), `chapter8.complete` (Brekk's), `home.launched` (Orrin's) | The townsfolk wearing Ishen's, Brekk's and Orrin's bodies (City.cs: "Ishen and Brekk until their chapters") stand down, and Kept, Unsworn and Vael townsfolk take the same jobs in the same places |

**Where the chapters' people stand in these places (contract).** The named people who come aboard are each chapter's own staging (`HoarfellBuilder`, `OrielBuilder`, `HarrowgateBuilder` `.StageManyHands`), which stands them today where this table's last column says. The places publish where each belongs (`Hands.Arrivals.HaskAt` and the rest); the stagings should read those and stand their people there. `ManyHandsArrivalsTests.TheNamedPeopleWhoCameAboardStandInTheirPlacesAndSayTheirChaptersLines` holds them to it.

| Who (node) | Their place (constant) | Staged today |
|---|---|---|
| Hask (`HaskAboard`) | (28.5, -48) at the pen's gate (`HaskAt`) | (28.5, -48): in place |
| Iru (`IruAboard`) | (15.4, 46.4) beside her lamps (`IruAt`) | (15.4, 46.4): in place |
| Ishen (`IshenAboard`) | (-33.2, 42.3) in front of the archive's shelves (`IshenAt`) | In place: `OrielBuilder` reads `IshenAt` (it stood at (-20.4, 51.6) on the lamp trimmer's walk) |
| The gardener (body; `IdraGardenerAboard` on a point) | (41.8, 49.7) by its planter (`GardenerAt`); look point (42.7, 50.5) (`GardenerLook`) | In place (it stood at (50.4, 38.6), on Marrow-2's bed) |
| Tovra (`TovraAboard`) | (46.2, -27.9) at her anvil (`TovraAt`) | `HarrowgateBuilder` reads `TovraAt` from 2026-11-13 (it stood at (28, -37), a metre outside the herd's fence); in place once chapter 8's staging is run again |
| Brekk (`BrekkAboard`) | (48.6, -26.2) at the bellows (`BrekkAt`) | `HarrowgateBuilder` reads `BrekkAt` (it stood at (35.5, -31), 4.9 m from F in the open hall) |
| Drask (`DraskAboard`) | (42.2, -58.8) in his cell (`DraskAt`); talked to through the bars from (41.9, -57.5) | `HarrowgateBuilder` reads `DraskAt` (it stood at (70, -1.6), on the galley's middle table's edge). From `home.odaFound` he says `DraskCount` (chapter 9's) |

**What waits (chapters 9 and 10 are paused).** Chapter 9's definitions now set every flag these arrivals come with (`HouseLeaves`: `home.odaFound` to `chapter9.complete`; `CoronachLaunch`: `home.launched`; the `PortableBenches` quest), and its staging (`HomeBuilder.Approach`, restage step `chapter9.approach`) already builds on the hooks here: Oda's talk goes on `Oda, aboard`, its own Ket waits at `KetAt` until the bench, Bryn's lesson and Meret's notice are its points. Still bodies here: Bryn (her talk is the lesson point), Orrin (no node until chapter 10's `OrrinRoll`), Ket after the bench (Hal's apprentice, no lines). Ket's bench works as a bench only when chapter 9 makes it one (a second `Bench` aboard breaks `ManyHandsWorldTests`' single bench). Chapter 10's twelve before Orrin's board are its staging; the board and the standing room are here.

**When the Halo's districts join the convoy (chapter 10), the hub will need to show it:** the districts' hulls off the stern and past the gallery rail (ManyHandsHull's sky and the view from the scopes), not only the tug; the board under the gallery adding each district as it comes in (the board holds "CINDER HALO / DEPARTED, / ALL SECTIONS" and Sorrel today); the Halo's people aboard in their own coats, filling the cabins' lane and the raft bay rather than standing in the ways (the build's ways check refuses anyone within 2 m of one); traffic between the hulls at the dock (a ferry line and the visitors' pad busy); and Nacre's families moved off the freight yard into berths. Each goes in as one more `Arrivals: ...` root from the chapter's flag, held to the same checks.

**What is owed in the hull (2026-11-13, not the arrivals').** `QaChapterTwoLookTests.NothingDrawsAMeshWithNoUvsOverATexture("ManyHandsHull")`: a `Roofscape` piece is built without uvs and drawn with a plating texture coplanar with the textured roof; which one trips it changes with each build (liner east, then raft bay rim), so the fix is in `ManyHandsBuilder.Hull.cs`'s `Roofscape` (give its shapes uvs, or draw them untextured). `QaPlanWorldTests.EveryLockedExitStaysShutAndSaysWhatOpensIt("ManyHandsHull")`: chapter 9's warp to Nacre (`HomeBuilder.Approach.WarpToNacre`) is switched off by its `FlagGate` until `chapter8.complete`, and `WarpBeacon` restores its prompt only in `Update`, so pressed while hidden it keeps its refusal after `home.turnedForNacre`; chapter 9's to settle (show it, or name its lock words for a hidden warp).

**What is owed.** A Rauk crew body: Idra's crew wear her own two bodies (natural and braced) in other greys and sizes, which is two looks where the bar asks three. The Kept have two coats for townsfolk (`KeptHerder`, Tern's), since Hask's and Iru's are aboard as themselves.

## Evidence

Built in the editor (`ManyHandsBuilder.BuildZones -manyhands-zones ManyHands`, then `ZoneLook.Apply`, then `LookDev.Render`, 39 views), measured with `python tools/look_density.py` and looked at as contact sheets.

| Pass | What was wrong | What changed |
|---|---|---|
| plan check | The builder's own `Refuse` stopped the first build: the delegates' queue stood on the way to the freight yard, the RAFT BAY sign in the hold's water | The queue moved along the yard's north side; the sign onto the quay. The same check ported offline found nothing more |
| errands | Six walkers' loops ran into the arcade's frontage, scatter, a pulse lamp, a cabin row | The grower's tray run goes round by the spine; the walkers' loops joined the scatter's keep-off; the laundry walk stays in its lane |
| bake | The freight hall's talk point unreachable: the shut cargo door's carving obstacle cut the editor's own path check | Every seal is opened for the bake and put back; the bake now names every unreachable point at once |
| `hands-01` | Five stills over .22: the cabins' lane .51 and the breach .35 (flat grey roofs), the spine's foot .33 (the dock door's header beam across the camera's line), the gallery's west .31, the hall .24. The moon read as an orange cliff; the carrier ribs toy orange; the liner checker harsh | The header beam gone and the door posts and gantry fade; roofs each their own colour with a rim and things on them; Pell's lamp racks and a compass rose on the gallery; painted cargo bays in the hall, the dock and the yard; a teal runner and lantern lines down the arcade; the moon further off and smaller; softer checker |
| `hands-03` | Every still at or under .22 (worst .19: the spine's foot and the north quay) | |
| `hands-07` (the lead's second look) | The cabins rows of plain boxes; the arcade a kitchen checker; the freight floors the plainest (.15 to .19); one water barrel scattered everywhere; the school raft missing | Each cabin a door in a frame with its step, a striped awning, a lit framed window with a sill and a pot; the liner's own floor (cream slabs, dark diamonds where four meet, a mosaic band every fourth course, a medallion at the crossing and smaller ones down the promenade, brass inlay lines); lane paint, tie-down rings, oil and scuff, pallet stacks and two parked loaders on the freight deck; Ilu goods along the north quay; the kits' other pieces scattered; the school raft with Iri and three children, from `dov.bellReported`. Every still at or under .15 |
| `hands-arrivals-01`..`06` (`ManyHandsBuilder.ArrivalStills`, the city at chapters 5, 6, 7, 8, 9 with Nacre's families, and 10; 58 views a stage, 19 of them the arrivals') | The herd's bedding read as lily pads (radial discs) and its fodder as loose straw; Idra's cutter stood at the world's origin (a static read before its place was set); with nothing arrived, the pen-gate view of the hall's bare west half measured .23 | Straw in slabs and tied bales under a hide; the cutter's place a literal; the pen-gate view's point 2.6 m south, taking in the hall's south wall. `hands-arrivals-06` (the pen's south run at z -48.2, chapters 6 to 9 restaged): worst .21 at chapter 5 (the pen-gate view, the hall before the herd), .17 at 6 and 7, .16 at 8, 9 and 10 |

What is weak, plainly:

- **The freight districts are still the plainest** (the hall's door .15, the hall and the gallery .14): plate, paint and cargo, with fewer people than the arcade. The hall's tie-down rings are too small to read at play distance.
- **The moon past the gallery's rail** still reads as an orange mass more than a planet's limb; the stars below the deck are crisp points but few show in a play frame.
- **The arcade's tile** is a regular check: busy enough to measure well, but it reads as tile and not as a liner's mosaic.
- **No still has been taken in a player**, and none with `-local-biomes`; nobody has walked the circuit or timed it.
- **Built pieces** (stalls, cabin rows, the galley's tables) are boxes with painted textures beside the generated props; the cabins' fronts are flat panels with painted doors.
