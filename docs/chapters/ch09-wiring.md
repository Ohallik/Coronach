# Chapter 9 - who wires what, and where

**State on November 13 (chapter 9 paused; read from each engineer's files, nothing here was played).** *Defined* (`HomeBuilder.cs`, `HOME_DEFS_OK`): the six ZoneDefs (chapter 9, their music, threat 1), the five QuestDefs as manifest section 4 (the closing card's body cut from 370 characters to 195), Dorrow, Tam, Jun and Oda (who had no definition) with their sheets, two chart stops (the Crawl, charted by `home.crawlNamed`; "Cinder Halo", closed on `home.launched`). The script is live: `docs/writing/Home.yarn`, all 104 nodes compiled (`HOME_DIALOGUE_OK`); `PeopleMarks.Repeats` and Many Hands' two beats are in; the expectations (`HalCrated`'s branch, the Nacre warps' lock) are in `HomeWorldTests`. *Placeholders*: `BuildZones` now stands an empty loadable scene (a floor or an open plane, `Arrival`, walls, light; logged `HOME_ZONE_PLACEHOLDER`) for any chapter 9 zone defined and not built, so the build list holds every ZoneDef; a zone's own build replaces it. MeridianCrawl and Terminator are placeholders today. *Placed*: Nacre Grave is built and lit (two look passes, `Builds/quality/look/home-nacre-02`; a third build is lit and unseen); `StageApproach` (`chapter9.approach`) has run once into Harrowgate, the Picket, Many Hands and its hull. *The others*: the Crawl's builder is 1,337 lines (looks, shop, chart hook) and has never built its zone; the Terminator has its map doc and `DawnLine`, `DawnTouched`, `TreadCoupling`, `TreadRide`, and its builder is still the skeleton's stub; the Decks (1,185 lines) and the Coronach (walk and launch, `CoronachTests`, D269) build `DecksLeaving` together (07:44); the Works (1,172 lines, `SeparationWorksTests`) and the Ring (794 lines, `BrokenRingTests`) are built; all eight ordinary enemies and both bosses are defined, with `BossController.FoundrySaint.cs` and `.DebrisCutter.cs` (`HomeCombatTests`, `HomeBossTests`). No zone but Nacre Grave has been looked at by this engineer, and no chapter 9 class but `HomeWorldTests` run by him.

Made from [the manifest](../writing/ch09-manifest.md) (sections 2 to 7) on November 12 by the chapter's first engineer, before any zone was built. Brief: [ch09-the-house-that-can-leave.md](ch09-the-house-that-can-leave.md). Builder: `S/Editor/World/HomeBuilder.cs` and its partial files. **Every engineer builds from this table and corrects it with exact edits**: change the owner, the place or the state of a row in place, never rewrite the file. A row nobody owns is a row that falls between zones; if you find one, put your name on it or say whose it is.

**Owners** (the file each engineer owns; nobody edits another's):

| Short | File | Who |
|---|---|---|
| Defs | `HomeBuilder.cs` (zones, people, quests, chart, dialogue list) | first engineer |
| Nacre | `HomeBuilder.Nacre.cs` (zone `NacreGrave`, map docs/maps/NacreGrave.md) | first engineer |
| Approach | `HomeBuilder.Approach.cs` (`StageApproach`: Harrowgate, the Picket, Many Hands, Many Hands' hull) | first engineer |
| Crawl | `HomeBuilder.Crawl.cs` (zone `MeridianCrawl`) | town engineer |
| Terminator | `HomeBuilder.Terminator.cs` (zone `Terminator`) | wild-run engineer |
| Decks | `HomeBuilder.Decks.cs` (zone `DecksLeaving`, a second scene; chapter 1's `Hub_Decks` is untouched) | Decks engineer |
| Coronach | `HomeBuilder.Coronach.cs` (the walk and the launch, staged into `DecksLeaving` by the Decks' build) | Coronach engineer |
| Works | `HomeBuilder.Works.cs` (zone `SeparationWorks`) | dungeon engineer |
| Ring | `HomeBuilder.Ring.cs` (zone `BrokenRing`) | space-zone engineer |
| Enemies | `HomeBuilder.Enemies.cs` (ordinary enemies, flight fights) | first combat engineer |
| Bosses | `HomeBuilder.Bosses.cs`, `S/Combat/BossController.FoundrySaint.cs`, `BossController.DebrisCutter.cs` | second combat engineer |

**State**: *owed* (nobody has placed it yet), *placed* (built and in a scene), *tested* (a test asserts it). Update your rows as you go.

## 0. Contracts between files

- **One build call per zone.** `HomeBuilder.BuildZone` calls `Nacre.Build()`, `Crawl.Build()`, `Terminator.Build()`, `Decks.Build(Coronach.Stage)`, `Works.Build()`, `Ring.Build()`. Each starts with `WorldBuilder.Begin(<zone>)` and ends with `WorldBuilder.Save`. Rebuild with `-home-zones <Zone>`.
- **The Decks and the Coronach share one scene.** `Decks.Build(Action<ZoneKit> coronach)` calls `coronach(kit)` once the Decks' own places stand and **before** its navigation bake and ground pass, so the Coronach's people and props are in both. The Decks publishes its places (doors, the walkway joint, lock nine, gate six, pitch nine, the board, the notice board, the dock office) as `internal static readonly` constants in its nested class `Decks`; the Coronach reads them, never repeats them. Rebuild either with `-home-zones DecksLeaving`.
- **Each zone's definition hook** (`<Zone>Definition(ZoneDef z)`) runs last inside `Define`'s lambda for its `ZoneDef`: an ambience bed, a bright floor, a pocket's music, a different profile. `CrawlStop(ChartStop s)` and `DecksStop(ChartStop s)` set what really stands there (`shop`, `bench`, `repair`: `RouteChartTests` loads the zone and looks for a `Bench` and a `RepairBay`).
- **Enemies by id** (brief, "Enemies"): `TowDrone`, `BridleFragment`, `HeatScavenger`, `SlagGull`, `OverrideRig`, `WeldArm`, `CompactWarden`, `Interceptor`; bosses `FoundrySaint`, `DebrisCutter` (exactly: their barks are found by id). A zone names them through `HomeBuilder.Foe(id, standIn)`: until the definition exists it gets the stand-in and the log says `HOME_FOE_STANDIN`. Each zone's map doc has a "contract" section the combat engineer fills in. **All eight are defined** (November 12, `HomeBuilder.Enemies.cs`): no zone needs its stand-in now; the contracts are in docs/maps/NacreGrave.md and BrokenRing.md ("Flight fights contract"), Terminator.md ("Run fights contract", the combat engineer's answer) and SeparationWorks.md ("Works fights contract").
- **Boss encounters' ids** are `FoundrySaint` (in the Works' press hall) and `DebrisCutter` (in the Ring): the main quest's arrow finds them by those ids (steps 10 and 13).
- **`SideStories.Beats`** (`S/World/SideStories.cs`, shared): chapter 9's block is at the top of the list, under the comment `// The House That Can Leave (chapter 9`. Each engineer appends the beats for the people in their own zone **inside that block**, with an exact edit, each guarded by its zone (`s.zone=="DecksLeaving"`) or a flag only chapter 9 sets, and each requiring its speaker met (a beat never preempts a first meeting). The rows below say which beats there are; the first engineer wrote Many Hands' and Nacre's.
- **`PeopleMarks.Repeats`** (`S/UI/PeopleMarks.cs`): every repeat line of the manifest's section 8.2 is in, written by the first engineer. A new repeat you add is yours to append.
- **`FirstSight`** (`S/World/FirstSight.cs`, the first engineer's): a world object that has the lead say a bark once, when an encounter begins or when the lead first comes within range of it. Use it for the "first sight of each" barks below.
- **Restaging.** A staging step that reaches into another chapter's scene is registered with the restage runner (`S/Editor/World/Restage.cs`) by its `[Restage.Step]` attribute. The first engineer's is `chapter9.approach` (`HomeBuilder.Approach.cs`: Harrowgate, PicketOfTeeth, ManyHands, ManyHandsHull, after `chapter8.many-hands`), every piece named `Home: ...`. **The script is live** (`docs/writing/Home.yarn`, `docs/writing/ch09-manifest.md`, moved out of `pending/` under D240; `HomeBuilder.Dialogue` compiled all 104 nodes): fix any link to `pending/` in your own docs.

## 1. The ways between zones

| Way | From | To (spawn) | Open when | Placed by | State |
|---|---|---|---|---|---|
| `Warp — Nacre Grave` | PicketOfTeeth, north end at (40, 1, 776), by the spawn `Harrowgate` (a ship off Harrowgate's quay is beside it) | NacreGrave (`Arrival`) | drawn, and open, from `home.turnedForNacre` (Abri's word); before it nothing stands there (November 14: it was drawn from `chapter8.complete` and shut until her word, and a press while hidden left it refusing once open) | Approach | placed, tested (`HomeWorldTests.TheWarpToNacreIsDrawnAndOpenOnlyOnceAbriHasTurnedTheConvoy`) |
| `Warp — Nacre Grave` | ManyHandsHull, off the stern at (-60, 1, -176), beside chapter 6's, 7's and 8's | NacreGrave (`Arrival`) | as above | Approach | placed, tested |
| `Warp — The Hull of Many Hands` | NacreGrave, at (-21.7, 1, 154.6), in the arrival's frame | ManyHandsHull (`Nacre` at (-52, 1, -166), a spawn Approach adds) | always | Nacre (warp), Approach (spawn) | placed |
| Sela's chart stop `MeridianCrawl` | anywhere | MeridianCrawl (`Arrival`) | `home.crawlNamed` (Idra charts it: `IdraTownMovers`) | Defs (stop), Crawl (spawn and what `CrawlStop` says) | stop defined |
| The way to the run | MeridianCrawl, the drop cage at the tail (58.4, 5.4): `Ride the drop cage down — The Terminator`, locked "Ask Chief Engineer Dorrow at the helm first"; gone from `home.treadRecovered` | Terminator (`Arrival`) | `home.crawlAsked` (`DorrowFirst`) | Crawl | placed (`MeridianCrawlTests`) |
| The way back from the run | Terminator, Dorrow's crawler at the ride's end (8, 45.5): `Climb aboard — The Meridian Crawl`, locked "Tread-hall four is still out there: re-mate its three couplings and ride it in" | MeridianCrawl (`Gap`) | `home.rideDone` (the ride's end) | Terminator (way), Crawl (spawn) | placed (`TerminatorTests`) |
| Home | MeridianCrawl, the yard's pad (2, 6): `Launch — The Decks`, locked "Bring tread-hall four in for Chief Engineer Dorrow first" | DecksLeaving (`Arrival`) | `home.treadRecovered` | Crawl (way), Decks (spawn) | placed |
| Sela's chart stop `DecksLeaving` ("Cinder Halo", region Home, position (.03, .98)) | anywhere | DecksLeaving (`Arrival`) | visited; **closed** once `home.launched` | Defs (stop), Decks (spawn and what `DecksStop` says) | stop defined |
| Beneath the promenade | DecksLeaving, the gate over the stairwell in the market vestibule at (6, -10.15): `Go down — The Separation Works`, locked "The town meets first: Orrin has called it in the dock office" | SeparationWorks (`Arrival`) | `home.meetingHeld` | Decks | placed |
| Back up | SeparationWorks, the hatch stair at the arrival (`Climb to the promenade — The Decks`) | DecksLeaving (`Works`, a spawn the Decks adds at the top of its way down: (6, -12.4)) | always open | Works (way), Decks (spawn) | way built; spawn placed |
| After the Saint (the short way home, for Hal's scene) | SeparationWorks, the lift at the press hall's north-east corner (`Ride the lift up — The Decks`) | DecksLeaving (`Lift`, a spawn the Decks adds by Hal's workshop) | `bossdown.FoundrySaint` (the lock names it) | Works (way), Decks (spawn) | way built; spawn owed |
| The launch | DecksLeaving | BrokenRing (`Arrival`) | after `NeveFirstOut` (`home.launched`) | Coronach | owed |
| `Warp — The Hull of Many Hands` | BrokenRing | ManyHandsHull (`Ring` at (-34, 1, -166), a spawn Approach adds: `HomeBuilder.RingSpawn`) | `home.cutterDisabled` | Ring (warp), Approach (spawn) | spawn written |
| Chapter 1's stop `Decks` (and the Halo's three docks, and a save) | anywhere | `Hub_Decks`, which **is** DecksLeaving from `home.treadRecovered` until `home.launched` (`ZoneDef.standsFor`, `Lattice.Core.StandIns` in `SceneFlow`); chapter 1's Decks before and after | the stop closes at `home.launched` (`lockedFlag`, set from `DecksStop`) | Decks | answered (was the open question for the lead); the Halo's docks after the launch are the Ring's to close |

## 2. Nodes (all 104), where each plays and who stands it

### Harrowgate and Nacre Grave

| Node | Zone | How it starts | Sets | Placed by | State |
|---|---|---|---|---|---|
| `AbriTurnsForNacre` | Harrowgate, at the cells after `MeretAllStations` | `AfterTalk` on `chapter8.complete`, speaker `Keeper`, heard `home.turnedForNacre`; a second, the same, in Many Hands for a save that left Harrowgate first | starts `HouseLeaves`; `home.turnedForNacre` (the talk's own `<<setFlag>>`, so the shared wiring test sees who sets it) | Approach | placed, tested (`ChapterEightHandsOnByItselfAndTheFirstArrowLeadsToAnOpenWarpToNacre`, from both places) |
| `BrynNacreApproach` | NacreGrave, the outer marker | crossing across the lane; the point's flag `home.nacreReached` | `home.nacreSpoken` | Nacre | owed |
| `OdaFound` | NacreGrave, Oda's hauler | point `Hail — Line nine hundred and four`, report `OdaHauler`, needs `home.nacreSpoken` | `home.odaFound` | Nacre | owed |
| `BrynWelcomeBack` | NacreGrave, the tug | point `Hail — The tug Welcome Back`, report `WelcomeBack`, needs `home.nacreSpoken` | `home.tugFound` | Nacre | owed |
| `BrynNavigators` | NacreGrave, the navigators' pod | point `Hail — The navigators' pod`, report `NavigatorsPod`, needs `home.nacreSpoken` | `home.navigatorsFound` | Nacre | owed |
| `NeveReturnOffer` | NacreGrave, Neve's skiff in the outer spiral | point `Hail — Neve`, flag `neve.offered`, needs `home.nacreSpoken` (a point, not an Npc: she has nothing else to say at Nacre) | starts `ReturnAddress` | Nacre | owed |

### Many Hands (StageApproach)

| Node | Zone | How it starts | Sets | Placed by | State |
|---|---|---|---|---|---|
| `MeretBerthNotice` | ManyHands, the dock | crossing walked into off the dock, needs `home.nacreDone` (a `FlagAll` over the three beacons, in Nacre and in the city); the point's flag `home.noticeHeard` | `home.noticeHeard` | Approach | owed |
| `BrynChartLesson` | ManyHands, the chart table | point `Ask Bryn about her chart`, needs `home.noticeHeard`, speaker `Bryn` | `home.chartLent` | Approach | owed |
| `IdraTownMovers` | ManyHands, the raft bay by Maru | point `Ask Maru who has moved a town`, report `TownMovers`, needs `home.chartLent` (a point, not an Npc: Idra is in the party) | `home.crawlNamed` | Approach | owed |
| `OdaAboard` | ManyHands, Oda's cabin | Oda's Npc at the place Many Hands' arrivals stand her (`Arrivals.OdaAt`), first and repeat, from `home.odaFound` until `home.returned` | | Approach | owed |
| `DraskCount` | ManyHands, Drask's cell | beat on `Drask`: aboard, `home.odaFound`, not `drask.asked` | `drask.asked` | Approach (beat) | owed |
| `AbriNamesOnTheWall` | ManyHands | beat on `Keeper`: aboard, `home.noticeHeard`; a repeat | | Approach (beat) | owed |
| `OrrinChalk` | ManyHands, the chart table | point `See where the roads run`, report `FleetChart`, needs `home.cutterDisabled` | `home.chartRead`, `chapter9.complete` | Approach | owed |

### Duskline

| Node | Zone | How it starts | Sets | Placed by | State |
|---|---|---|---|---|---|
| `DorrowFirst` | MeridianCrawl, the helm (-79, .6) | Dorrow's Npc, first node. The run opens on it | `home.crawlAsked` | Crawl | placed |
| `DorrowRepeat` | MeridianCrawl | Dorrow's repeat at the helm until `home.rideDone` (then he is at the lip) | | Crawl | placed |
| `TamFirst` | MeridianCrawl, the market street's south rail (-41, -6.8) | Tam's Npc, first and repeat (optional), until `home.returned` | | Crawl | placed |
| `DorrowTreadHome` | **MeridianCrawl**, as tread-hall four rolls in (Dorrow at the lip) | `AfterTalk` on `home.rideDone` (the Terminator's ride), speaker Dorrow, heard `home.treadRecovered` | `home.treadRecovered` | Crawl | placed (`MeridianCrawlTests`) |

### The Decks on separation day (DecksLeaving)

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `OrrinHomecoming` | beat on Orrin (met since chapter 1): `home.treadRecovered`, not `home.returned` | `home.returned` | Decks | owed |
| `JunFirst`, `JunRepeat` | Jun's Npc at the walkway end of Mira's stall | | Decks | owed |
| `MiraInventoryOffer` | beat on Mira: `home.returned`, `SecondInventory` not started | starts `SecondInventory`, shop | Decks | owed |
| `MiraCratedRepeat` | Mira's repeat on the day | shop | Decks | owed |
| `MiraInventoryReturn` | beat on Mira: `SecondInventory` at step 1 | `mira.inventorySettled`, shop | Decks | owed |
| `HalCrated` | Hal's Npc, first and repeat until `home.halChose`; three choices (bench, repair, just visiting) | | Decks | owed |
| `HalBenchesOffer` | beat on Hal: `home.returned`, `PortableBenches` not started | starts `PortableBenches` | Decks | owed |
| `HalBenchesReturn` | beat on Hal: `PortableBenches` at step 1 | `hal.benchesLit` | Decks | owed |
| `NeveFlyingAStreet` | Neve's Npc at lock nine, from `home.returned` until `home.launched` | | Decks | owed |
| `NeveReturnAnswer` | beat on Neve: `ReturnAddress` at step 3 | `neve.letterAnswered` | Decks | owed |
| `SurvivorDecks` | Dace's Npc on the dock with his crew (met since chapter 1: give it as his repeat, or a beat) | | Decks | owed |
| `SurvivorNevesLetter` | beat on `Survivor`: `ReturnAddress` at step 2 | `dace.letterRead` | Decks | owed |
| `OdaDecks`, `IloDecks` | Oda at the dock office, Ilo at the notice board, until `home.meetingHeld` | | Decks | owed |
| `DorrowListens` | Dorrow in a service hatch, ear to the deck | | Decks | owed |
| `TamStillOffer` | Tam on the promenade, `home.returned`, `StillGround` not started (Tam may not have been met on the Crawl: first node, and a beat for when met) | starts `StillGround` | Decks | owed |
| `TamDoorMarks`, `TamLeaningTree`, `TamWornStep` | points `Show Tam` at a marked doorframe, Jun's pear tree, a worn stair; report `StillThing`, `StillGround` at step 0; flags `tam.door`, `tam.tree`, `tam.step` (the talks set nothing) | | Decks | owed |
| `TamGoesBack` | beat on Tam: `StillGround` at step 1 | `tam.goingBack` | Decks | owed |
| `HalLastMachine` | beat on Hal: `home.saintReleased`, not `home.halChose`. No bark, no prompt for Taren | `home.halChose` | Decks | owed |
| The meeting, in order: `OrrinMeetingOpens`, `MeretBerthOffer`, `HalApprentices`, `MiraManifest`, `IloReadsNames`, `OdaStands`, `OrrinPostponed`, `MeretWithdraws` | eight walks in one room (manifest section 1); each offered once the one before has set its flag; **report `Meeting`** on whatever starts each (the main quest's step 8 arrow looks for it) | `home.meetingOpened`, `home.offerMade`, `home.halAsked`, `home.manifestShown`, `home.namesRead`, `home.odaStood`, `home.orrinSaid`, then `home.berthRefused` and `home.meetingHeld` | Decks | owed |

### The Separation Works

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `DorrowWorks` | crossing at the first joint under the promenade | `home.worksRead` | Works | tested (`SeparationWorksTests`) |
| `SelaWeddingTable` | point `Look at the table` in a store bay, report `Consignment`, `SecondInventory` at step 0, flag `mira.lotTable` (the talk sets nothing) | | Works | tested (`SeparationWorksTests`) |
| `OrrinSaintSpeaks` | after the override rig is destroyed: `AfterTalk` on `bossdown.FoundrySaint`, speaker `Orrin`, heard `home.saintReleased` (the talk sets it too) | `home.saintReleased` | Works (the room), Bosses (the down) | placed at (0, 193), 3 m up; tested |

### The Coronach (DecksLeaving, staged by the Coronach engineer)

No combat, no timer, no bark, no prompt to hurry (manifest section 1). Every door and guest is `Stop and listen`; Taren's, Sela's and Idra's are `Say yours`. **No report on any of them**: step 12 targets the zone and nothing in it, so no arrow points at a door (the Coronach engineer's decision; design and contract in docs/maps/DecksLeaving.md, "The Coronach" and "The launch").

| Node | Where | Point's flag | Needs | Placed by | State |
|---|---|---|---|---|---|
| `OrrinCallsCoronach` | a crossing on the promenade just outside Hal's workshop door; Orrin is the Coronach's own body there from `home.halChose` (the Decks' Orrin goes at `home.halChose`) | `home.coronachCalled` | `home.halChose` | Coronach | owed |
| `MiraStallDoor` | pitch nine, crated | `coronach.mira` | `home.coronachCalled` | Coronach | owed |
| `OrrinBoardDoor` | the dock office board | `coronach.orrin` | same | Coronach | owed |
| `HalBenchDoor` | four bolt holes in his workshop floor | `coronach.hal` | same | Coronach | owed |
| `NeveLockDoor` | lock nine | `coronach.neve` | same | Coronach | owed |
| `OdaOfficeDoor` | the dock office's stamping window | `coronach.oda` | same | Coronach | owed |
| `JunLineDoor` | the walkway end of the stall | `coronach.jun` | same | Coronach | owed |
| `IloNoticeDoor` | where the capacity line was pinned | `coronach.ilo` | same | Coronach | owed |
| `SurvivorDockDoor` (not needed) | the dock rail | `coronach.dace` | same | Coronach | owed |
| `PellEdge`, `RuvaEdge`, `MaruEdge`, `HaskEdge`, `TovraEdge`, `DorrowEdge`, `AbriEdge` (guests, optional) | manifest section 1's places | `coronach.pell` ... `coronach.abri` | same | Coronach | owed |
| `coronach.doorsHeard` | `FlagAll` over the seven needed doors' flags | | | Coronach | owed |
| `TarenWalkway`, `SelaGateSix`, `IdraNoDoor` | the walkway joint from the first scene; gate six; wherever Idra stands | `coronach.taren`, `coronach.sela`, `coronach.idra` | `coronach.doorsHeard` | Coronach | owed |
| `coronach.lastHeard` | `FlagAll` over those three | | | Coronach | owed |
| `OrrinGiveItRoom` | Orrin at the walkway joint, office side: point `Stand with Orrin`, only when the player chooses (never an `AfterTalk`) | `home.coronachWalked` (the talk sets it) | `coronach.lastHeard` | Coronach | owed |
| The 32 extras (`MiraDoorCrystals` ... `TamDoorEdge`) | each beside its speaker's door or guest place, straight after that speaker's line in the same stop; each needs `home.coronachCalled` and its quest's flag (manifest table "The extras") | none: each plays with its door, whose flag saves | | Coronach | owed |
| `NeveFirstOut` | as the market district lifts, after `home.coronachWalked` | `home.launched` | | Coronach | owed |

### The Broken Ring

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `BrynTowsCutter` | `AfterTalk` on `bossdown.DebrisCutter`, speaker `Bryn`, heard `home.cutterDisabled` (the talk sets it) | `home.cutterDisabled` | Ring | tested (`BrokenRingTests`) |

## 3. Flags

The brief's, then the script's, then the world's. "Talk" means the node's own `<<setFlag>>`.

| Flag | Set by | Zone | Placed by |
|---|---|---|---|
| `home.turnedForNacre` | talk `AbriTurnsForNacre` (its AfterTalk's heard flag too); shows the two warps to Nacre | Harrowgate (Many Hands) | Approach |
| `home.nacreReached` | the outer marker's crossing (its own flag) | NacreGrave | Nacre |
| `home.nacreSpoken` | talk `BrynNacreApproach` | NacreGrave | Nacre |
| `home.odaFound`, `home.tugFound`, `home.navigatorsFound` | talks at the three beacons | NacreGrave | Nacre |
| `home.nacreDone` | `FlagAll` over the three | NacreGrave, ManyHands | Nacre, Approach |
| `neve.offered` | Neve's point (its own flag) | NacreGrave | Nacre |
| `mira.lotNacre` | the Nacre lot's point | NacreGrave | Nacre |
| `neve.parcelTug` | the tug parcel's point | NacreGrave | Nacre |
| `home.noticeHeard`, `home.chartLent`, `home.crawlNamed` | talks `MeretBerthNotice`, `BrynChartLesson`, `IdraTownMovers` | ManyHands | Approach |
| `drask.asked` | talk `DraskCount` | ManyHands | Approach |
| `bench.ket` | Ket's bench point | ManyHands | Approach |
| `home.crawlReached` | a `FlagAll` on `visited.MeridianCrawl` (set the moment the party arrives) | MeridianCrawl | Crawl |
| `home.crawlAsked` | talk `DorrowFirst` | MeridianCrawl | Crawl |
| `mira.lotCrawl`, `neve.parcelCrawl`, `bench.lo` | the Crawl's three points | MeridianCrawl | Crawl |
| `home.coupling1`, `home.coupling2`, `home.coupling3` | each coupling re-mated (prompt `Re-mate the coupling`); each a checkpoint: failure restarts from the last coupling, never the start | Terminator | Terminator |
| `home.rideDone` | the ride out's end (`TreadRide`): the hall is out ahead of the light; the Crawl shows it home from this; in `storyFlags` | Terminator | Terminator |
| `home.treadRecovered` | talk `DorrowTreadHome` (an `AfterTalk` on `home.rideDone`) | MeridianCrawl | Crawl |
| `home.returned` | talk `OrrinHomecoming` | DecksLeaving | Decks |
| `home.meetingOpened`, `home.offerMade`, `home.halAsked`, `home.manifestShown`, `home.namesRead`, `home.odaStood`, `home.orrinSaid`, `home.berthRefused`, `home.meetingHeld` | the meeting's talks | DecksLeaving | Decks |
| `mira.inventorySettled`, `hal.benchesLit`, `dace.letterRead`, `neve.letterAnswered`, `tam.goingBack` | the side stories' return talks | DecksLeaving | Decks |
| `tam.door`, `tam.tree`, `tam.step`, `bench.marn` | the Decks' points | DecksLeaving | Decks |
| `home.worksRead` | talk `DorrowWorks` | SeparationWorks | Works |
| `home.joint1` ... `home.joint6` | each joint reopened (prompt `Take the joint's weight`). **On the critical path: `home.joint1`, `home.joint2`, `home.joint3`** (their hands report `Joint`; the Saint's door waits on `home.joint3`); `home.joint4` to `home.joint6` open the three optional pockets and **report nothing**, so they never count toward step 9 (docs/maps/SeparationWorks.md, "The six joints") | SeparationWorks | Works |
| `mira.lotTable` | the table's point | SeparationWorks | Works |
| `bossdown.FoundrySaint` | the game | SeparationWorks | Bosses |
| `home.saintReleased` | talk `OrrinSaintSpeaks` | SeparationWorks | Works |
| `home.halChose` | talk `HalLastMachine` | DecksLeaving | Decks |
| `home.coronachCalled` | talk `OrrinCallsCoronach` | DecksLeaving | Coronach |
| `coronach.*` (doors, guests, last words, `doorsHeard`, `lastHeard`) | points and `FlagAll`s | DecksLeaving | Coronach |
| `home.coronachWalked` | talk `OrrinGiveItRoom` | DecksLeaving | Coronach |
| `home.launched` | talk `NeveFirstOut` | DecksLeaving | Coronach |
| `bossdown.DebrisCutter` | the game | BrokenRing | Bosses |
| `home.cutterDisabled` | talk `BrynTowsCutter` | BrokenRing | Ring |
| `home.chartRead`, `chapter9.complete` | talk `OrrinChalk` | ManyHands | Approach |

## 4. Quest steps

Defined in `HomeBuilder.Quests` exactly as manifest section 4 (rewards: materials, parts and scrip; never a capped consumable). What each step's arrow finds is the thing in the "Thing" column: it must exist under that name.

### HouseLeaves (main, chapter 9)

| # | Kind, target | Arrow (`stepTargets`) | Thing, in its zone | Placed by | State |
|---|---|---|---|---|---|
| 0 | Reach `NacreGrave` | the way to Nacre, via the Picket or the hull | the two `Warp — Nacre Grave` | Approach | placed, tested (the arrow followed from Harrowgate and from Many Hands) |
| 1 | Flag `home.odaFound` | report `OdaHauler` in NacreGrave | Oda's hauler's point | Nacre | owed |
| 2 | Flag `home.tugFound` | report `WelcomeBack` | the tug's point | Nacre | owed |
| 3 | Flag `home.navigatorsFound` | report `NavigatorsPod` | the pod's point | Nacre | owed |
| 4 | Flag `home.crawlNamed` | report `TownMovers` in ManyHands, via the hull | the raft bay point | Approach | owed |
| 5 | Reach `MeridianCrawl` | the chart's diamond on the Crawl stop | the stop, the Crawl's `Arrival` | Defs, Crawl | placed |
| 6 | Flag `home.treadRecovered` | the way to the Terminator, via the Crawl; in the Terminator its `ZoneLine` (couplings, gallery, the way home) | the Crawl's drop cage; the Terminator's couplings and crawler | Crawl, Terminator | placed |
| 7 | Flag `home.returned` | speaker `Orrin` in DecksLeaving | Orrin's Npc | Decks | owed |
| 8 | Flag `home.meetingHeld` | report `Meeting` in DecksLeaving | whatever starts each meeting talk | Decks | owed |
| 9 | Interact `Joint` x3 | report `Joint` in SeparationWorks, via DecksLeaving | the joints' points | Works | tested |
| 10 | Flag `home.saintReleased` | encounter `FoundrySaint`, via DecksLeaving | the press hall's encounter | Works, Bosses | placed (Works), tested |
| 11 | Flag `home.halChose` | speaker `Hal` in DecksLeaving | Hal's Npc | Decks | owed |
| 12 | Flag `home.coronachWalked` | the zone only (`At(DecksZone)`): no arrow inside the town, by design | nothing | Coronach | defined |
| 13 | Flag `home.cutterDisabled` | encounter `DebrisCutter` in BrokenRing | the Cutter's encounter | Ring, Bosses | placed (Ring), tested |
| 14 | Flag `home.chartRead` | report `FleetChart` in ManyHands, via the hull | the chart table's point | Approach | owed |

Card: `cardFlag chapter9.complete`; the body is cut to two lines of the plate (the manifest's ran to 370 characters; `ChapterCardTests` measures it). Closing line: "Chapter 9 complete. The Halo is a fleet, and every road out runs past Sorrel." `storyFlags` (what chapter 10's fixture starts with) is in `HomeBuilder.Quests`: correct it there by exact edit if your zone sets another flag on the main path.

### The side quests

Each crosses three zones. Steps advance on flags the side talks set, never on `TalkTo`.

| Quest | Step | Thing | Zone | Placed by | State |
|---|---|---|---|---|---|
| `SecondInventory` (Mira) | start | `MiraInventoryOffer` | DecksLeaving | Decks | owed |
| | 0: Interact `Consignment` x3 | the Nacre lot: a crate lashed to a wreck, point `Read Mira's tag`, flag `mira.lotNacre`, bark `consign.nacre` | NacreGrave | Nacre | owed |
| | | the Crawl lot, lent: point `Read Mira's tag` on the sky-garden (47, 8.6), flag `mira.lotCrawl`, bark `consign.crawl` | MeridianCrawl | Crawl | placed (`MeridianCrawlTests`) |
| | | the wedding table: `SelaWeddingTable`, flag `mira.lotTable` | SeparationWorks | Works | tested |
| | 1: Flag `mira.inventorySettled` | `MiraInventoryReturn` | DecksLeaving | Decks | owed |
| `PortableBenches` (Hal) | start | `HalBenchesOffer` | DecksLeaving | Decks | owed |
| | 0: Interact `PortableBench` x3 | Ket on Many Hands, in the arcade at `Arrivals.KetBench`: point `Hand over the bench`, flag `bench.ket`, bark `bench.ket` | ManyHands | Approach | owed |
| | | Lo on tread-hall four's porch from `home.returned`: `Hand over the bench`, flag `bench.lo`, bark `bench.lo` | MeridianCrawl (the tread-hall, back with the Crawl) | Crawl | placed (`MeridianCrawlTests`) |
| | | Marn at the tug dock: flag `bench.marn`, bark `bench.marn` | DecksLeaving | Decks | owed |
| | 1: Flag `hal.benchesLit` | `HalBenchesReturn` | DecksLeaving | Decks | owed |
| `ReturnAddress` (Neve) | start | `NeveReturnOffer` | NacreGrave | Nacre | owed |
| | 0: Interact `TugParcel` | at the tug: point `Hand over the parcel`, flag `neve.parcelTug`, bark `parcel.tug` | NacreGrave | Nacre | owed |
| | 1: Interact `CrawlParcel` | on the Crawl, the yard's post rack (12.5, -8.9): `Hand over the parcel`, flag `neve.parcelCrawl`, bark `parcel.crawl` | MeridianCrawl | Crawl | placed (`MeridianCrawlTests`) |
| | 2: Flag `dace.letterRead` | `SurvivorNevesLetter` | DecksLeaving | Decks | owed |
| | 3: Flag `neve.letterAnswered` | `NeveReturnAnswer` | DecksLeaving | Decks | owed |
| `StillGround` (Tam) | start | `TamStillOffer` | DecksLeaving | Decks | owed |
| | 0: Interact `StillThing` x3 | the doorframe, the pear tree, the stair | DecksLeaving | Decks | owed |
| | 1: Flag `tam.goingBack` | `TamGoesBack` | DecksLeaving | Decks | owed |

Rewards: `SecondInventory` a Masterwork module, `PortableBenches` a Masterwork edge ("Ket made it"), `ReturnAddress` a Masterwork drive, `StillGround` a Ring Keystone ("a token"), each with 200 XP and 160 scrip; the main quest 1,000 XP, 1,400 scrip, a Masterwork frame (rolled), four Halo Rivets and four Nacre Shards. The combat ledger (`HomeBuilder.Enemies.cs`) counts these 1,800 XP against Sync 25 to 27.

## 5. Discovery points, by report

| Report | Points | Zone | Placed by |
|---|---|---|---|
| `OdaHauler`, `WelcomeBack`, `NavigatorsPod` | one each | NacreGrave | Nacre |
| `TownMovers`, `FleetChart` | one each | ManyHands | Approach |
| `Consignment` | three: Nacre, the Crawl, the Works | three zones | Nacre, Crawl, Works |
| `PortableBench` | three: Ket, Lo, Marn | ManyHands, MeridianCrawl, DecksLeaving | Approach, Crawl, Decks |
| `TugParcel`, `CrawlParcel` | one each | NacreGrave, MeridianCrawl | Nacre, Crawl |
| `StillThing` | three | DecksLeaving | Decks |
| `Meeting` | one per meeting talk | DecksLeaving | Decks |
| `Joint` | three, the critical joints 1 to 3 (the optional 4 to 6 report nothing) | SeparationWorks | Works |
| (none for the Coronach: its doors carry no report, so nothing points at them) | | DecksLeaving | Coronach |

## 6. Objective strings (manifest section 6)

The panel's line is the main quest's step text (section 4) everywhere; nothing in `ObjectiveHud` is edited. A boss's line with its bar up is its `Kit.fightGoal` (the combat engineer's). **There is no mechanism today for a zone to show a line of its own while the lead is somewhere** (the Picket's four rows in chapter 8 were never wired either): the rows marked "none" are owed to whoever writes one, or said by barks instead.

| Zone | When | Line | Mechanism | Owner | State |
|---|---|---|---|---|---|
| NacreGrave | in the inner spiral | Watch the marked tracks: inward is quick and full of wrecks | none; the `spiral` barks say it | Nacre | owed |
| NacreGrave | all three lamps found | Go back to Many Hands | step 4's text says it ("Go back to Many Hands, and find somebody who has moved a town") | Defs | defined |
| Terminator | the dawn line in view | Keep ahead of the dawn | `ZoneLine` (`DawnLine`) | Terminator | placed (`TerminatorTests`) |
| Terminator | at a coupling | Re-mate the coupling (1/3) | `ZoneLine` (`DawnLine`), within 16 m of the next | Terminator | placed (`TerminatorTests`) |
| Terminator | the third coupling mated | Ride the tread-hall out | `ZoneLine` (`DawnLine`) | Terminator | placed (`TerminatorTests`) |
| The Decks | `home.returned`, before the meeting | See your neighbours, then go to the dock office | step 8's text ("Attend the town meeting") | Defs / Decks | step text only |
| SeparationWorks | the Saint, arms left | Release an arm at its shoulder collar (1/3) | `Kit.fightGoal` | Bosses | owed |
| SeparationWorks | three arms down | Climb the fallen arms to the rig on its back | `Kit.fightGoal` | Bosses | owed |
| The Decks | `home.coronachCalled` | Walk the town. There is no hurry. | step 12's text, now exactly this line (an exact edit in `HomeBuilder.Quests` by the Coronach engineer) | Defs / Coronach | defined |
| BrokenRing | the Cutter, pods left | Disable a weapon pod: your choice which first (1/3) | `Kit.fightGoal` | Bosses | owed |
| any | `chapter9.complete` | Chapter 9 complete. The Halo is a fleet, and every road out runs past Sorrel. | `closingLine` | Defs | defined |

## 7. Bark triggers (manifest section 7; the lines are in docs/writing/barks.json)

Nothing barks during the meeting, Hal's scene or the Coronach.

| Key | Speaker | Trigger | Zone | Placed by | State |
|---|---|---|---|---|---|
| `bossphase.FoundrySaint`, `bossopening.FoundrySaint`, `bossphase.DebrisCutter`, `bossopening.DebrisCutter` | heroes | `BossController.Situation` | Works, Ring | automatic for those ids | |
| `spiral` | heroes | crossing into the inner spiral, and back out (two in turn) | NacreGrave | Nacre | owed |
| `towdrone`, `fragment` | heroes | first sight of each (`FirstSight`) | NacreGrave | Nacre | owed |
| `wreck` | heroes | passing a wreck with its hull colours showing (three in turn, quiet) | NacreGrave | Nacre | owed |
| `hail.spiral` | Bryn | as the party flies the spiral (`RadioHail`, two in turn) | NacreGrave | Nacre | owed |
| `consign.nacre`, `parcel.tug` | heroes | the points' `situation` | NacreGrave | Nacre | owed |
| `bench.ket` | heroes | the point's `situation` | ManyHands | Approach | owed |
| `consign.crawl`, `parcel.crawl`, `bench.lo` | heroes | the points' `situation` | MeridianCrawl | Crawl | placed |
| `dawn` | heroes | the dawn line closes: far, near, on them (three in turn: the run's script) | Terminator | Terminator | placed (`DawnLine`) |
| `hail.dawn` | Dorrow | the same three moments (`DawnLine`, 2.6 s after each) | Terminator | Terminator | placed |
| `remate`, `hail.remate` | heroes, Dorrow | each coupling re-mated | Terminator | Terminator | placed (`TreadCoupling`) |
| `scavenger`, `gull` | heroes | first sight of each (`FirstSight`: the lesson's burrower by range; `Terminator_Gull` as it wakes) | Terminator | Terminator | placed |
| `bench.marn` | heroes | the point's `situation` | DecksLeaving | Decks | owed |
| `joint` | heroes | a joint reopened (three in turn, round again) | SeparationWorks | Works | placed (each hand's `situation`) |
| `override`, `weldarm`, `warden` | heroes | first sight of each; `warden` when they give ground | SeparationWorks (and the Ring for wardens) | Works (sight), Enemies (`warden` on giving ground: the body's `tune.bark`) | Enemies' half defined: `CompactWarden` has `tune.bark="warden"`, said by the lead as a Warden stands aside at nought (`EnemyGuardian.Ground.cs`) |
| `saint.arm` | heroes | an arm released at its collar | SeparationWorks | Bosses | owed |
| `hail.section` | Orrin | the four districts lifting, market first after `NeveFirstOut` | DecksLeaving / BrokenRing | Coronach (the first), Ring (the rest) | Ring's three placed and tested (`SectionFlight`, by index) |
| `section` | heroes | a district passes during the Cutter fight | BrokenRing | Ring | placed (`SectionFlight`) |
| `hail.lane` | Neve | during the Cutter fight | BrokenRing | Ring | owed |
| `hail.market` | Mira | when the Cutter's beam crosses the market district | BrokenRing | Bosses (the beam), Ring | owed |
| `hail.repair` | Hal | when the repair quarter clears the ring | BrokenRing | Ring | tested |
| `hail.ring` | Bryn; Idra | the launch; Idra when she is not the active ship | BrokenRing | Ring (Bryn's, tested), Bosses (Idra's) | Bryn's placed |
| `interceptor` | heroes | first pair | BrokenRing | Ring | placed (`FirstSight` on `Ring_Pair1`) |
| `pod` | heroes | a weapon pod disabled | BrokenRing | Bosses | owed |

## 8. People, by zone

| Who | Where | Body | Placed by |
|---|---|---|---|
| Bryn Asta | radio at Nacre; at Many Hands' chart table (`Arrivals.BrynAt`, from `home.tugFound`); radio in the Ring | `BrynBody` | Nacre (voice), Approach, Ring |
| Oda | Nacre (voice, her hauler); Many Hands (`OdaAboard`); the Decks | `OdaBody` (Many Hands' stand-in already wears it) | Nacre, Approach, Decks |
| Neve | Nacre (her skiff, a point); the Decks (lock nine) | | Nacre, Decks |
| Meret | radio in Many Hands; in person at the meeting; radio at the launch | | Approach, Decks, Coronach |
| Maru | Many Hands' raft bay (chapter 4's own, already there); the Coronach's guest | | Approach (the point by her), Coronach |
| Drask | Many Hands' cell (chapter 8's staging stands him) | | Approach (the beat) |
| Dorrow | the Crawl's helm; the Decks' service hatch; the Works (`DorrowWorks`); the Coronach guest | `DorrowBody` | Crawl, Decks, Works, Coronach |
| Tam | the Crawl's market street; the Decks' promenade; the Coronach (`TamDoorEdge`) | `TamBody` | Crawl, Decks, Coronach |
| Jun | the Decks, the walkway end of Mira's stall | `JunBody` | Decks |
| Orrin, Mira, Hal, Ilo, Dace and his crew | the Decks | chapter 1's | Decks |
| Ket | Many Hands' arcade (Many Hands' arrivals stand his body) | | Approach (the point) |
| Lo, Marn | the Crawl's tread-hall four; the Decks' tug dock | townsperson bodies | Crawl, Decks |

## 9. Asked of other files by the first engineer (November 14)

- **Decks engineer, `DecksLeavingTests.cs`**: `DialogueInputTests.EveryCompiledStoryNodeAndChoiceAcceptsRapidInputAndRoundTripsItsConsequences("Home")` fails on `MiraInventoryOffer` (suite `nov13a`, parts 4 and 5: `AdvanceToChoice`, "Expected: True", the talk ended at Mira's counter waiting for a choice). Its `[ContentRegistry.Expectations]` declares `book.Branch("MiraInventoryOffer","shop")`, and the same for `MiraCratedRepeat` and `MiraInventoryReturn`. A `Branch` says the node **has choices**, and these three have none: the test waits for one that never comes. Take the three lines out: a choice-less node of a later chapter is read from its own lines (`DialogueInputTests.EndingOf`), and each of the three ends on `<<shop>>`, so it is already checked as ending at her stall.
- **Space-zone engineer (the Ring)**: `ZoneLookTests.EveryZoneVolumeIsSharpAndGraded` (EditMode) fails on `Assets/_Project/Settings/BrokenRingVolume.asset`, which has no `ColorAdjustments` at all ("has no authored grade"): run `ZoneLook.Apply -look-zones BrokenRing` after the Ring's last rebuild. NacreGrave's volume is graded (contrast 12).

## 10. Where chapter 9 ends (for chapter 10's brief)

In **ManyHands** (the city, not the hull), at the chart table: the point "See where the roads run" (`Home: the fleet's chart`, report `FleetChart`, at plan (1.4, 45.2), offered once `home.cutterDisabled` is set). The party comes to it from the Broken Ring by its warp home to the hull (spawn `Ring`, (-34, 1, -166)) and the hull's way into the city. The talk is `OrrinChalk` (Sela, Bryn, Taren, Orrin), whose last lines set `home.chartRead` (the main quest's last step) and `chapter9.complete` (the closing card's flag). Set by then besides: `home.launched` (the Halo's districts have flown; the chart stop "Cinder Halo" is shut), `bossdown.DebrisCutter`, `bossdown.FoundrySaint` and the rest of `HouseLeaves.storyFlags`. Owed when chapter 10's start is fixed: `HouseLeaves`' closing line must end with what to do next, and `q.next` must name where chapter 10 begins (D273), with its hand-off's cases in `ContentFrameworkTests.HandOffs`.
