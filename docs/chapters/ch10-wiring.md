# Chapter 10 - who wires what, and where

**State on November 14 (chapter 10's first engineer; nothing here has been played).** **Unity's licence lapsed at 04:14, after the definitions ran and before anything else could:** `LastMooringBuilder.Definitions` (`MOORING_DEFS_OK`) and `HomeBuilder.Definitions` (`HOME_DEFS_OK`, chapter 9's hand-off) have run; the dialogue, music staging, placeholders, Many Hands' restage, route facts and every test below are written and type-checked and **not yet run** (the commands, in order, are at the end of section 12). Until `BuildZones` runs, the six ZoneDefs have no scenes and `ContentFrameworkTests`' build-list case fails on them. *Defined* (`LastMooringBuilder.cs`, `MOORING_DEFS_OK`): the six ZoneDefs (chapter 10, their music, threat 1), the three QuestDefs as manifest section 4 with step text and step targets, the neighbourhood's chart stop. Nobody new speaks, so no person is defined (every one of the 33 speakers has a definition and a sheet; Meret's has Determined and Shocked). The script is live: `docs/writing/LastMooring.yarn` and `ch10-manifest.md` moved up out of `pending/`, all 78 nodes in `LastMooringBuilder.Nodes`, which `LastMooringBuilder.Dialogue` compiles and checks (unrun). *Placeholders*: `BuildZones` stands an empty loadable scene for each of the six (D270; unrun). *Staged, written and unrun*: chapter 10's way in, in Many Hands and its hull (`chapter10.many-hands`), and chapter 9's hand-off to it (`HouseLeaves.closingLine` and `next`, the one edit in `HomeBuilder.cs`). *Empty and waiting for their owners*: one partial file per place and per fight, each with its entry point. Nothing of any place or fight is built.

Made from [the manifest](../writing/ch10-manifest.md) (sections 1 to 10) and [the brief](ch10-a-route-with-room-to-turn-back.md), on the model of [chapter 9's](ch09-wiring.md). Builder: `S/Editor/World/LastMooringBuilder.cs` and its partial files (`S/` = `Lattice/Assets/_Project/Scripts/`). **Every engineer builds from this file and corrects it with exact edits**: change the owner, the place or the state of a row in place, never rewrite the file. A row nobody owns falls between zones; if you find one, put your name on it or say whose it is.

## Owners

Each engineer owns their files and edits nobody else's. A shared file (`SideStories.cs`, `PeopleMarks.cs`, `barks.json`, `ContentFrameworkTests.cs`, a registration list) gets a small append by exact edit.

| Short | File | Who |
|---|---|---|
| Defs | `LastMooringBuilder.cs` (zones, quests, chart, dialogue list, placeholders) | first engineer |
| Approach | `LastMooringBuilder.Approach.cs` (`StageApproach`: Many Hands' chart table and Orrin's board, the hull's stern) | first engineer |
| **Berth** | a new `S/World/Berth.cs` (one MonoBehaviour), small edits in `S/Core/ZoneController.cs`, `S/Combat/PartyController.cs`, `S/Combat/PlayerBrain.cs`, `S/UI/ContinuousReview.cs`, the save path; its test class `T/PlayMode/BerthTests.cs` | systems engineer (section 6.1) |
| Sorrel | `LastMooringBuilder.Sorrel.cs` (zone `SorrelSecondSky`, map docs/maps/SorrelSecondSky.md) | Sorrel engineer |
| Mooring | `LastMooringBuilder.Mooring.cs` (zone `LastMooring`, map docs/maps/LastMooring.md) | Last Mooring engineer |
| Continuance | `LastMooringBuilder.Continuance.cs` (zone `Continuance`, map docs/maps/Continuance.md) | Continuance engineer |
| Chamber | `LastMooringBuilder.Chamber.cs` (zone `StillwaterChamber`, map docs/maps/StillwaterChamber.md; the anchor's release is this file's) | chamber engineer |
| Passage | `LastMooringBuilder.Passage.cs` (zone `ReturningPassage`, map docs/maps/ReturningPassage.md) | passage engineer |
| Afloat | `LastMooringBuilder.Neighbourhood.cs` (zone `NeighbourhoodAfloat`, map docs/maps/NeighbourhoodAfloat.md; `NeighbourhoodStop`) | neighbourhood engineer |
| Enemies | `LastMooringBuilder.Enemies.cs` (ordinary enemies and flight rosters, the XP ledger; tells in `T/PlayMode/LastMooringCombatTests.cs`) | first combat engineer |
| Mother | `LastMooringBuilder.Mother.cs`, `S/Combat/BossController.MotherOfDoors.cs` (and any world part it reads, in `S/Combat/`) | Mother of Doors engineer |
| Tollkeeper | `LastMooringBuilder.Tollkeeper.cs`, `S/Combat/BossController.Tollkeeper.cs` | Tollkeeper engineer |
| Meret | `LastMooringBuilder.Meret.cs`, `S/Combat/BossController.MeretShip.cs`, `.MeretGantry.cs`, `.MeretWell.cs`, and their world parts `S/Combat/MeretClamp.cs`, `MeretCoupling.cs`, `MeretFrame.cs`; her sheets and tests in `T/PlayMode/LastMooringBossTests.cs` (Meret region) | Meret engineer (stream H): defined and tested in the yards, November 14 (6.5) |
| Credits | the credits roll and the last chapter's end (`S/UI/`; section 8) | UI engineer |
| Art | `tools/meshy/c17_stillwater_batch.py`, `tools/c17_intake.py` (section 11) | art runner |

**State**: *owed* (nobody has placed it), *placed* (built and in a scene), *tested* (a test asserts it). Update your rows as you go.

## 0. Contracts between files

- **One build call per zone.** `LastMooringBuilder.BuildZone` calls `Sorrel.Build()`, `Mooring.Build()`, `Continuance.Build()`, `Chamber.Build()`, `Passage.Build()`, `Neighbourhood.Build()`. Each returns false until it is written (the zone keeps its placeholder); written, it starts `WorldBuilder.Begin(<zone>)`, ends `WorldBuilder.Save` and returns true. Rebuild with `-mooring-zones <Zone>`. Its light is a `[ZoneLook.Source]` in its own file.
- **Each zone's definition hook** (`<Place>Definition(ZoneDef z)`) runs last inside `Define`'s lambda for its `ZoneDef`: an ambience bed, a pocket's music, `cueMusicIds`, `quietArrival`. `NeighbourhoodStop(ChartStop s)` sets what really stands in the town (`shop`, `bench`, `repair`: `RouteChartTests` loads the zone and looks).
- **Places are published, not repeated.** Every point another file needs (a spawn, a pad, a boss's floor, a seat) is an `internal static readonly` constant in its zone's nested class; the fight reads it from there. A contract between two engineers lives in the zone's map doc under its own heading: the one who needs something writes it, the one who builds it writes the coordinates back (guide 0.1).
- **Bosses by id, exactly** (their barks are found by it): `MotherOfDoors`, `Tollkeeper`, `MeretShip`, `MeretGantry`, `MeretWell`. **Each boss's encounter id is its own id**: the main quest's arrows find them by it (steps 4, 6, 7, 8). The tug's fight is `Passage_Tug` (step 11). A zone names an enemy through `LastMooringBuilder.Foe(id, standIn)`: until the definition exists it gets the stand-in and the log says `MOORING_FOE_STANDIN`. **A boss `EnemyDef` is defined in the same change as its `Kit`**: defined alone, it throws `BOSS_UNREGISTERED` in every test that spawns it.
- **`SideStories.Beats`**: chapter 10's block is at the top of the list, under `// A Route With Room To Turn Back (chapter 10`, ahead of chapter 9's (Abri's and Drask's chapter 9 lines hold aboard and the first match wins). Append your people's beats inside it, each guarded by your zone (`s.zone=="NeighbourhoodAfloat"`) or a flag only chapter 10 sets (`end.*`), each with its speaker met. **`PeopleMarks.Repeats`** already holds every repeat of manifest 8.2 (the aboard lines, the epilogue's people, the four messages, the nine visits and the 32 extras).
- **Restaging.** `chapter10.many-hands` (`LastMooringBuilder.Approach.cs`, after `chapter9.approach`) stages ManyHands and ManyHandsHull; every piece is named `End: ...`. A zone of this chapter that reaches into another scene registers its own `[Restage.Step]` (guide 12.1). No chapter 10 zone needs another's scene today.
- **Music** is named once, in `LastMooringBuilder.cs`: the zones' tracks (`SecondSkyTheme` Adventure Awaits, `MooringTheme` Starfight, `CompactTheme` Regal Alien Town, `DepartureTheme` Endless Flight, `AfloatTheme` Hub Town Groove, `FightTheme` Starfight). Three are stand-ins for the creator's cues (docs/music/NEW_TRACKS.md 4 to 6: "Last Mooring", "Room To Turn Back", "Neighbourhood Afloat"); each swap is one line there. The bosses name theirs on their definitions: Boss of the Ages (Mother of Doors), Glitch Boss (the Tollkeeper), Final Stand (all three of Meret's). A moment's own track (the anchor letting go, the turn back) is a `cueMusicIds` entry on its zone, by the zone's hook, played as chapter 9's Coronach plays its own.
- **The script is live** (`docs/writing/LastMooring.yarn`). A new or changed `.yarn` line needs `LastMooringBuilder.Dialogue` (Yarn project reimport, guide 12); `barks.json` changes need it too (it copies the Resources twin).

## 1. The ways between zones

The chapter is one road: Many Hands' hull, Sorrel, Last Mooring, the Continuance, the anchor chamber, the passage, the neighbourhood. Every zone before the passage has a way back toward the hull, so a party short of gel can go home to its shops; the chart's travel works from anywhere outside a fight.

| Way | From | To (spawn) | Open when | Placed by | State |
|---|---|---|---|---|---|
| `Warp — Sorrel's Second Sky` | ManyHandsHull, off the stern at (-6, 1, -176), east of chapter 9's to Nacre on the same row | SorrelSecondSky (`Arrival`) | drawn, and open, from `end.planSaid`; nothing stands there before | Approach | written, unrun |
| `Warp — Last Mooring` | ManyHandsHull, off the stern at (24, 1, -176) | LastMooring (`Arrival`) | drawn, and open, from `end.motherFreed` (the convoy has moved up) | Approach | written, unrun |
| `Warp — The Hull of Many Hands` | SorrelSecondSky, in the arrival's frame | ManyHandsHull (`Sorrel` at (2, 1, -166), a spawn Approach adds: `LastMooringBuilder.SorrelSpawn`) | always (refused mid-fight by the beacon itself) | Sorrel (warp), Approach (spawn) | warp placed (Sorrel engineer, November 14; `SorrelSecondSkyTests`) at (-10, 1, -4); spawn written |
| `Warp — Last Mooring` | SorrelSecondSky, at the lane's far end | LastMooring (`Arrival`) | drawn from `end.motherFreed` (a `FlagGate`, as the hull's) | Sorrel | placed (Sorrel engineer, November 14; `SorrelSecondSkyTests`) at (158, 1, 322), the lane east's far end |
| `Warp — The Hull of Many Hands` | LastMooring, in the arrival's frame (the convoy's lee) | ManyHandsHull (`Mooring` at (32, 1, -166): `LastMooringBuilder.MooringSpawn`) | always | Mooring (warp), Approach (spawn) | warp placed (Last Mooring engineer, November 14; `LastMooringZoneTests`) at (-10, 1, -4); spawn written |
| `Dock — The Continuance` | LastMooring, a pad on the Continuance's boarding spine, past the Tollkeeper's gate | Continuance (`Spine`, at the spine's foot) | drawn from `end.gateOpen` | Mooring (pad), Continuance (spawn) | spawn `Spine` placed (0, -5, -76); pad placed (Last Mooring engineer, November 14; `LastMooringZoneTests`) at (0, 1, 524) on the spine's head, drawn from `end.gateOpen` |
| `Launch — Last Mooring` | Continuance, the spine's foot | LastMooring (`Continuance`, beside the dock) | always, until `end.meretShip` | Continuance (pad), Mooring (spawn) | pad placed and tested (4.2, -5, -81); spawn `Continuance` placed (Last Mooring engineer, November 14; `LastMooringZoneTests`) at (10, 1, 512), facing south, 12 m from the dock |
| The flight berth round the hull | Continuance, the anchor works' launch bay (`Launch — after the director's craft`) | the same zone, flying (section 6.1); coming out of the bay is `MeretShipToShip`'s crossing | once the works are passed: the works' north door opens on **`clear.Continuance_Works`**, the last door before the bay | Continuance (berth), Berth (system) | placed and tested: `HullBerth`, docs/maps/Continuance.md "Berth contract" |
| `Dock — The Anchor Chamber's frame gantry` (it names the zone it leads to, as every way to a later zone must) | Continuance's flight berth, at the gantry's rail | StillwaterChamber (`Gantry`, the phase two checkpoint) | from `end.meretShip` (MeretGantry heard) | Continuance (pad), Chamber (spawn) | pad placed and tested (24, 19, 247.5); spawn `Gantry` placed (chamber engineer, November 14; `StillwaterChamberTests`) at (0, 0, -21) on the gangway (her door's checkpoint), nearer her than the zone's `Arrival` (0, 0, -30.5) on the landing |
| `Ride down — The Control Well` | StillwaterChamber, the gantry's lift | the same zone, the well's door (`Well`, the phase three checkpoint) | from `bossdown.MeretGantry` (a lock on a `bossdown.` flag names itself; its refusal says the frame is still up) | Chamber | placed and tested (chamber engineer, November 14; `StillwaterChamberTests`): `ChamberLift` (S/World) on the cage at (0, 0, 12.5), refused "The lift won't run while her frame is still up"; carries both down to (0, -16, 46.4) in 8 s, standing still while a conversation holds the controls (D275); the walk beside it opens with it |
| `Launch — The Returning Passage` | StillwaterChamber, the well's door, once Ilo has her | ReturningPassage (`Arrival`) | from `end.custody` | Chamber (pad), Passage (spawn) | pad placed (chamber engineer, November 14; `StillwaterChamberTests`) at (5.6, -16, 47.6) on the landing outside the well's door, shown from `end.custody`; spawn `Arrival` placed (passage engineer, November 14; `ReturningPassageTests`) at (0, 1, 0) facing north |
| The cut to the epilogue | ReturningPassage, at the far mouth | NeighbourhoodAfloat (`Arrival`) | `OrrinRendezvous`'s talk closes (`end.rendezvous`): the party is held, a plate "SEVEN WEEKS ON", the load (section 6.4) | Passage | the arrival placed (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`): `Arrival` (0, -4) on the moorings; the cut placed (passage engineer, November 14; `ReturningPassageTests`): `S/World/CutAway.cs` on `end.rendezvous` once the talk closes, the party held 2.5 s, the plate on black (`S/World/TimePlate.cs`, drawn by `S/UI/ScreenFade.cs`), the load |
| Sela's chart stop `NeighbourhoodAfloat` (region "The new sky", (1, .97)) | anywhere | NeighbourhoodAfloat (`Arrival`) | visited | Defs (stop), Afloat (spawn and `NeighbourhoodStop`) | placed: `NeighbourhoodStop` lists shop (Mira's counter, `Trade — Pitch Eleven`), bench (`Fabricate — Tovra's Forge`) and repair (`Mooring cradle — repair and save`), each standing |
| The nursery's road (LastSurvey) | NeighbourhoodAfloat, the moorings (`Launch — The nursery's road, Sorrel's Second Sky`: the zone's name is in it, as every way to a later zone must say) | SorrelSecondSky (`Road`, the course's start) | from `sela.surveyOffered` and `end.epilogue` | Afloat (pad), Sorrel (spawn and course) | Sorrel's spawn and course placed (Sorrel engineer, November 14; `SorrelSecondSkyTests`) (D287: open from `end.motherFreed`); the pad placed (neighbourhood engineer, November 14) at (20.4, -7), shown from `sela.surveyOffered` (`end.epilogue` holds in the zone by arriving) |

The Continuance and the chamber have no way back but the one they came in by and the chart. A party that falls at Meret is never sent back further than her door (section 6.5).

## 2. Nodes (all 78), where each plays and who stands it

"Point" is a `DiscoveryPoint` (its own flag, never the one its talk sets); "crossing" is a point started by walking through its box (`startsOnCrossing`, `ZoneKit.Crossing`); "after-talk" is an `AfterTalk{flag,node,speaker,heardFlag,delay,range}` (guide 15.6), which plays once nothing is fighting and never twice.

### Many Hands, among the Halo's districts (Approach)

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `IdraOnePlan` | point `Hear Idra's plan` at the chart table (the city's `ChartTalk`, (0, 45.2)), needs `chapter9.complete`, flag `end.planHeard`, **report `OnePlan`** (what chapter 9's `q.next` points at); speaker `Idra` (a point: she is in the party) | | Approach | written, unrun |
| `OrrinRoll` | after-talk on `end.planHeard`, speaker `Orrin`, at his board (14.8, 36.1), delay .6, heard `end.planSaid` (its own line sets it) | `startQuest TurnBack`, `end.planSaid` | Approach | written, unrun. *Owed (polish, Approach):* "each crew stands as it is pointed at" (manifest 8.3); the people already stand where Many Hands' arrivals put them |
| `AbriFortiethDay` | beat on `Keeper`: aboard, `end.planSaid`, not `visited.SorrelSecondSky` | | Approach (beat) | written |
| `DraskBeforeTheField` | beat on `Drask`: aboard, `end.planSaid`, met | | Approach (beat) | written |
| `SelaLastSurveyOffer` | point `Look at the nursery on the chart` at the table's west end (-2.8, 45.2), needs `end.planSaid`, flag `sela.surveyOffered`; the neighbourhood's travel terminal carries a second point with the **same flag** (offered once, heard once) | `startQuest LastSurvey` | Approach, Afloat | Many Hands' written, unrun ; the terminal's placed at (14.6, -2.8) (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `SelaSurveyHome` | point at the route chart (Many Hands' table, or the travel terminal), report `SurveyHome`, needs `end.surveyFlown`, `LastSurvey` at step 1 | `sela.surveyFiled` | Afloat (and Approach: the table's, if the course can be flown before the passage) | the terminal's placed at (12.1, -2.9) (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |

### Sorrel's second sky

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `SurvivorSecondSky` | crossing across the lane where the ridge and the clamp come into view, flag `end.sorrelSeen`, speaker `Survivor` (Dace, by radio) | | Sorrel | placed (Sorrel engineer, November 14; `SorrelSecondSkyTests`): across both apertures at z 250 |
| `SurvivorMotherGoes` | after-talk on `bossdown.MotherOfDoors` (she goes, the young follow), speaker `Survivor`, heard `end.motherFreed`; **nothing barks after it for a while** (`BarkService.Hush` for 8 s from its close) | `end.motherFreed` | Sorrel (the talk), Mother (the going) | the talk placed (Sorrel engineer, November 14; `SorrelSecondSkyTests`): delay 6 s; the hush is `S/World/QuietAfter.cs`; the young leave by `S/World/LeavesAlong.cs` |

### Last Mooring

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `MaruSectionHeld` | crossing in the lane where Many Hands' galley quarter is struck in view, flag `end.sectionStruck`, speaker `Maru` (by radio); the `sectionhit` bark first, as the section is struck | `end.sectionHeld` | Mooring | placed (Last Mooring engineer, November 14; `LastMooringZoneTests`): the crossing across the whole arena at z 172, 12 m past chain B's gap, sets `end.sectionStruck` and says the lead's `sectionhit` (the point's own situation); the talk is an after-talk 3.8 s later (the bark first), heard `end.sectionHeld`; Many Hands' galley quarter is in the crossing's frame, its breach shown from `end.sectionStruck`, Driftreed's mats from `end.sectionHeld`; `hail.mats` (Maru) on flying back through chain B's gap |
| `OrrinGateOpen` | after-talk on `bossdown.Tollkeeper` (the gate motor dies), speaker `Orrin`, heard `end.gateOpen`; the first convoy hulls come through behind the party as it plays (moving scenery, not moving the party) | `end.gateOpen` | Mooring (the talk and the hulls), Tollkeeper (the down) | the talk and the hulls placed (Last Mooring engineer, November 14; `LastMooringZoneTests`): after-talk delay 4 s; the market district, a seed barge and two rafts queued at chain D go through under the party from `bossdown.Tollkeeper` (`LeavesAlong`); the down is the Tollkeeper's |

### The Continuance

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `IdraSpineBraced` | crossing at the top of the boarding spine, flag `end.spineTop`, **report `Spine`** (step 5's arrow), speaker `Idra` | `end.boarded` | Continuance | placed and tested (0, 0, -12.5) |
| `PatrolLetsPass` | crossing across maintenance deck four in front of the captain of the *Steady Hand* and his crew, flag `end.captainPassed`, speaker `Patrol`; he steps aside on his third line (the crew walk off by `LeavesAfter`, guide 15.10) | | Continuance | placed and tested (box z 86 across deck four; he steps to (-15, -3, 96)) |
| `MeretShipToShip` | crossing as the party comes out of the launch bay into the flight berth, flag `end.meretHailed`, speaker `Meret` (her voice from her craft). **The MeretShip encounter's `requiredFlag`** | | Continuance (crossing), Meret (door) | crossing and door placed and tested (the door's boss stands in as an Interceptor until Meret's definition exists) |
| `MeretGantry` | after-talk on `bossdown.MeretShip`, speaker `Meret`, heard `end.meretShip` (its line sets it); she is seen at the gantry's rail from the berth (her body drawn there from `bossdown.MeretShip`) | `end.meretShip` | Continuance | placed and tested |

### The anchor chamber (StillwaterChamber)

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `MeretWell` | crossing just inside the control well's door, at the lift's foot (**within 10 m of the lift**: the main quest's step 7 still reads "Open the two arm couplings" until it plays), flag `end.wellEntered`, speaker `Meret`. **The MeretWell encounter's `requiredFlag`** | `end.meretGantry` | Chamber | placed and tested (chamber engineer, November 14; `StillwaterChamberTests`): box 9 x 2.2 at (0, -16, 52.8), 6.4 m from the lift's foot, needs `bossdown.MeretGantry`, saves `Well` |
| `MeretYields` | after-talk on `bossdown.MeretWell` (she yields standing: `yieldsOnDefeat`, `Ending.Yields`), speaker `Meret`, heard `end.crewsOrdered` | `end.meretYielded`, `end.crewsOrdered` | Chamber (talk), Meret (the yield) | placed (chamber engineer, November 14): after-talk delay 2.4 s; she stands by `YieldsStanding` (S/World), upright, from the frame she yields |
| `IshenAnchorSequence` | after-talk on `end.crewsOrdered`, delay long enough for the crews to walk in (Tovra's smiths to the four clamps, Idra's people to the braces, Ishen with the page: `Guide` legs, guide 15.17), speaker `Tovra`, heard `end.sequenceRead`. Ends on Idra's "Ease." | | Chamber | placed and tested (chamber engineer, November 14): delay 13.5 s; the crews walk in by `Guide` from her yield's hatches and the landing; Tovra's `hail.gear` (heard `end.gearHeard`) |
| (the hold) | `Take the last collar`, Taren's (whoever leads), from `end.sequenceRead`, flag `end.collarTaken`, **report `LastCollar`** (step 9's arrow): one hold, the anchor pulling twelve seconds and easing three, held through the ease on Idra's call (section 6.6) | `end.collarTaken` | Chamber | placed and tested (chamber engineer, November 14): `LastCollar` (S/World) at (0, -16, 78.4); the arrow is a never-offered point (range 0) on the seat |
| `MeretSeesItOpen` | after-talk on `end.collarTaken` as the four clamps let go and the road moves, speaker `Meret`, heard `end.anchorReleased`. **Nothing barks from her "Yes." to `IloCustody`**: `BarkService.Hush` from `end.collarTaken` to `end.custody` | `end.anchorReleased` | Chamber | placed and tested (chamber engineer, November 14): played by `AnchorRelease` itself 9.5 s into the release (the view still drawn back, the party held into it), not an `AfterTalk`; a load between plays it once settled; hushed to `end.custody` |
| `IloCustody` | after-talk on `end.anchorReleased`, speaker `Ilo`, heard `end.custody` (the talk sets nothing); civilian crews at the well's door | | Chamber | placed and tested (chamber engineer, November 14): after-talk delay 5.5 s; Ilo and two civilian crews walk in at the door, and all leave with her by the shaft (`LeavesAfter`) |

### The Returning Passage

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `IloFirstName` | crossing as the run begins, the young road alongside, flag `end.namesBegun`, speaker `Ilo` (radio) | | Passage | placed (passage engineer, November 14; `ReturningPassageTests`): across the road at s 170, (0, 1, 130); the young keep station to starboard (`S/World/Alongside.cs`) |
| `BrynFallsBehind` | crossing near the run's far end, flag `end.farEnd`, **report `FarEnd`** (step 10's arrow), speaker `Sela`. On Orrin's line the convoy closes to the lanterns at the lane's edges and leaves the middle empty | `end.passageRun` | Passage | placed (passage engineer, November 14; `ReturningPassageTests`): across the road at s 1080, about (1, 1, 1032); the convoy closes from `end.passageRun` (`S/World/ConvoyFlow.cs`); the turn (D286) is `S/World/CutAway.cs`: held 4 s, Taren and Sela out, the zone reloads at `Turn` |
| `BrynOnTheLine` | after-talk on `end.lineOn` (the tug's line taken: a flight prompt `Take the tug's line` at the tug, needs `clear.Passage_Tug`, **report `TugLine`**), speaker `Sela` | `end.tugFetched` | Passage | placed (passage engineer, November 14; `ReturningPassageTests`): the line's point at her bow, about (13, 1, 722), saves at `Tow`; after-talk delay 1.2 s; the tow is `S/World/TugTow.cs` |
| `OrrinRendezvous` | crossing at the far mouth with the tug in tow, needs `end.tugFetched`, flag `end.mouthReached`, **report `FarMouth`**, speaker `Orrin` | `end.rendezvous` | Passage | placed (passage engineer, November 14; `ReturningPassageTests`): across the narrows at s 1288, (0, 1, 1240); then the cut (section 1) |

### Neighbourhood Afloat, seven weeks on

No combat, no timer; the order is the player's, except that `JunLastLine` waits on the handrail and the line. `end.epilogue` is set on arriving (`FlagAll{flags={"visited.NeighbourhoodAfloat"},result="end.epilogue"}`).

| Node | How it starts | Sets | Placed by | State |
|---|---|---|---|---|
| `RuvaPassageRules` | Ruva's Npc in the square, chairing (first and repeat until `Homecomings` begun; then her visit tier) | `startQuest Homecomings` | Afloat | placed: Ruva at her table in the square (-9.4, 57), a beat in the zone every time, news until heard once (`end.rulesHeard`); her finished stories follow (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `IruTernsMessage`, `RuvaSennsMessage`, `PimQuillsMessage` | points `Read the message` at the message board (a knotted cord, a seed tag, a page of valve drawings), flags `end.msgTern`, `end.msgSenn`, `end.msgQuill` | | Afloat | placed: the board (7, 56.6), read from (4.8, 54.9), (7, 54.9), (9.2, 54.9) (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `PellAumisMessage` | point `Read the message` at the gallery lantern, flag `end.msgAumi` | | Afloat | placed: read from (-20, 93.4), before the gallery lantern (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `MeretAccount` | Meret's Npc in a plain room off the square, the door open, standing; every time | | Afloat | placed: (15.2, 58.4), the room's door open on its south side (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `DraskWatches` | Drask's Npc at a window over the square | | Afloat | placed: (5, 74.5) behind the sill of a window on the square's north side, spoken to from (5, 72.1) (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `OrrinDirectorship` | **beat** on Orrin (he has been met since chapter 1): in the neighbourhood, not `end.handrail`; at the new board. **Compulsory**: step 13's arrow is on him | `end.handrail` | Afloat | placed: (-12.4, 70.4) at the new board; from `end.handrail` he offers nothing (`Homecoming.quietFrom`) and the board has his chalked line (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `SelaReturnLeg` | point at the travel terminal, Bryn beside it, flag `end.routeFiled`, speaker `Sela` | | Afloat | placed: `Stop and listen` at (9.6, -2.8); Bryn (a body) at (9.8, 0.4), the navigator with the pencil at (14.4, 0.5) (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `IdraTiedUp` | point `Look at the moorings` (her cutter and Sela's ship on one cleat; there are forty), flag `end.cleatSeen` | | Afloat | placed: (-8, -11.4) at the cleat, the two ships moored below it; `IdraHomeCrew` follows if `CutterDown` was done (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `JaskNewSky`, `PipRota` | Npcs on the gallery | | Afloat | placed: Jask (-36, 92), Pip (-13, 91.6) drilling three small ones (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `TamSkyGarden` | Tam's Npc on the sky-garden deck, only while `quest.StillGround.complete` is unset (Tam stayed); otherwise Tam is "back for a week" (`TamHomeFrame`) | | Afloat | placed: (36, 94.4); the zone's beat says `TamHomeFrame` once `StillGround` is done (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| (the handrail and the line) | point `Mend the handrail` at the garden stair, needs `end.handrail`, flag `end.railMended`; then point `Repaint the line` at the fruit stall, needs `end.railMended`, flag `end.linePainted`; **both report `Handrail`** (step 14's arrow goes to the one still to do). From `end.linePainted` the wet new yellow line shows, and the pears stand one crate past it (`FlagGate`) | | Afloat | placed: the rail's point (-20.6, 26.1), the line's (-14.3, 23.4); the loose rail, the old line and the pears behind it give way by `FlagGate` (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `JunLastLine` | after-talk on `end.linePainted` at the fruit stall, speaker `Taren`, delay long enough to see the crate past the line (about 2.5 s); **nothing is heard after Taren's last word** (barks hushed from `end.linePainted` for good); the credits follow (section 8) | `game.complete` | Afloat (talk), Credits (after) | placed: `AfterTalk` at the stall (range 14, delay 2.5 s); `QuietAfter` on `end.linePainted` hushes every bark for good (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| The nine visits (`AbriHomecoming` ... `MiraHomecoming`) | each speaker's Npc in their community's street (manifest's table "Homecomings: nine visits"), offered at `Homecomings` step 0 with a point **report `Homecoming`** beside each (nine points, one flag each, `home.<speaker>` free to name: `end.visit<Speaker>`), in any order | | Afloat | placed: a point `Talk to <name>` on each person, flag `end.visit<Speaker>` (Abri's `end.visitAbri`); while it is wanted it is the person's only prompt (`S/World/Homecoming.cs`), afterwards their Npc says the visit again uncounted (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| The 32 extras (`MiraHomeCrystals` ... `TamHomeFrame`) | each beside its speaker's visit, there only if its quest was finished (manifest's table, "Needs"), said straight after the visit in the same stop (`Npc.SayNext`, as chapter 9's Coronach does), uncounted | none | Afloat | placed: `Homecoming.extras` asks each finished one to follow the person's own line or visit (`Npc.SayNext`); what stands with one (Dace's lamps, Binna's baskets, Senn's post, the shoot) is there only with it, and someone whose only lines are extras only if one was finished (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |

## 3. Flags

The brief's, then the script's, then the world's (the manifest's italic flags and the ones this file adds). "Talk" means the node's own `<<setFlag>>`. Every flag a main-quest step waits on is set by a line of the script (`LastMooringWorldTests.TheChapterIsDefinedAsItsScriptAsks` checks), because a flag set by a point or by code moves a step only at the next load.

| Flag | Set by | Zone | Placed by |
|---|---|---|---|
| `end.planHeard` | Idra's plan's point | ManyHands | Approach |
| `end.planSaid` | talk `OrrinRoll` (also its after-talk's heard flag) | ManyHands | Approach |
| `sela.surveyOffered` | the survey offer's two points | ManyHands, NeighbourhoodAfloat | Approach, Afloat |
| `end.sorrelSeen` | SurvivorSecondSky's crossing | SorrelSecondSky | Sorrel |
| `end.clamp1` ... `end.clamp4` | each clamp opened on foot (the hold's `openedFlag`; section 6.2), **saved at once** (6.1, rule 3) | SorrelSecondSky | Mother (the clamp), Sorrel (the seat) |
| `bossdown.MotherOfDoors` | the game, as she goes after the fourth | SorrelSecondSky | Mother |
| `end.motherFreed` | talk `SurvivorMotherGoes` | SorrelSecondSky | Sorrel |
| `end.surveyFlown` | the nursery's road's far point (report `NurseryRoad`), from `end.motherFreed` | SorrelSecondSky | Sorrel |
| `end.sectionStruck`, `end.sectionHeld` | the section's crossing; talk `MaruSectionHeld` | LastMooring | Mooring |
| `end.brace1`, `end.brace2`, `end.brace3` | the Tollkeeper's braces, counted as they break (any three of the cage's), **saved at once** | LastMooring | Tollkeeper |
| `end.motorStopped` | the gate motor disabled on foot on the arm | LastMooring | Tollkeeper (the motor), Mooring (the arm's berth) |
| `bossdown.Tollkeeper` | the game, as the motor dies (`Ending.ShutDown`: the carrier stays, its cage hangs open) | LastMooring | Tollkeeper |
| `end.gateOpen` | talk `OrrinGateOpen` | LastMooring | Mooring |
| `end.spineTop`, `end.boarded` | the spine's top crossing; talk `IdraSpineBraced` | Continuance | Continuance |
| `end.captainPassed` | PatrolLetsPass's crossing | Continuance | Continuance |
| `end.meretHailed` | MeretShipToShip's crossing | Continuance | Continuance |
| `bossdown.MeretShip`, `end.meretShip` | the game; talk `MeretGantry` | Continuance | Meret, Continuance |
| `bossdown.MeretGantry` | the game | StillwaterChamber | Meret |
| `end.wellEntered`, `end.meretGantry` | the well's door crossing; talk `MeretWell` | StillwaterChamber | Chamber |
| `bossdown.MeretWell`, `end.meretYielded`, `end.crewsOrdered` | the game (she yields); talk `MeretYields` | StillwaterChamber | Meret, Chamber |
| `end.sequenceRead` | IshenAnchorSequence's after-talk | StillwaterChamber | Chamber |
| `end.collarTaken` | the last collar's hold | StillwaterChamber | Chamber |
| `end.anchorReleased` | talk `MeretSeesItOpen` | StillwaterChamber | Chamber |
| `end.custody` | IloCustody's after-talk | StillwaterChamber | Chamber |
| `end.namesBegun` | IloFirstName's crossing | ReturningPassage | Passage |
| `end.farEnd`, `end.passageRun` | the far end's crossing; talk `BrynFallsBehind` | ReturningPassage | Passage |
| `clear.Passage_Tug`, `end.lineOn`, `end.tugFetched` | the game; the line's prompt; talk `BrynOnTheLine` | ReturningPassage | Enemies, Passage |
| `end.mouthReached`, `end.rendezvous` | the far mouth's crossing; talk `OrrinRendezvous` | ReturningPassage | Passage |
| `end.epilogue` | `FlagAll` on `visited.NeighbourhoodAfloat` | NeighbourhoodAfloat | Afloat |
| `end.msgTern`, `end.msgAumi`, `end.msgSenn`, `end.msgQuill`, `end.accountHeard`, `end.routeFiled`, `end.cleatSeen` | their points (Meret's account: her Npc's first hearing, if a flag is wanted at all) | NeighbourhoodAfloat | Afloat |
| `end.visit<Speaker>` (nine) | the visits' points | NeighbourhoodAfloat | Afloat |
| `sela.surveyFiled` | talk `SelaSurveyHome` | NeighbourhoodAfloat (or ManyHands) | Afloat |
| `end.handrail` | talk `OrrinDirectorship` | NeighbourhoodAfloat | Afloat |
| `end.railMended`, `end.linePainted` | the handrail's and the line's points | NeighbourhoodAfloat | Afloat |
| `game.complete` | talk `JunLastLine` | NeighbourhoodAfloat | Afloat |

`TurnBack.storyFlags` (in `LastMooringBuilder.Quests`) lists the world's flags on the main path; correct it there by exact edit if your zone sets another or names one differently.

## 4. Quest steps

Defined in `LastMooringBuilder.Quests` exactly as manifest section 4. What each step's arrow finds is in the "Thing" column: it must exist under that name. Every `via` runs back to `ManyHandsHull`, so from anywhere behind the party the arrow finds the way on, and the chart's diamond stands on Many Hands until the place has been stood in (no chart stop reaches the operation's five places).

### TurnBack (main, chapter 10)

| # | Kind, target | Arrow (`stepTargets`) | Thing, in its zone | Placed by | State |
|---|---|---|---|---|---|
| 0 | Flag `end.planSaid` | speaker `Orrin` in ManyHands | done in the same talk that starts the quest | Approach | written, unrun |
| 1 | Reach `SorrelSecondSky` | via the hull | Many Hands' way to the hull; the hull's `Warp — Sorrel's Second Sky` | Approach | written, unrun |
| 2 | Flag `end.motherFreed` | report `Clamp` in SorrelSecondSky, via the hull | four points with report `Clamp`, one at each clamp's berth pad, each with its clamp's flag (`end.clampN`) and never offered (a `previewEncounter`, as `XylosBuilder.SeedBarge`'s): the arrow goes to the nearest clamp still shut | Sorrel | placed (Sorrel engineer, November 14; `SorrelSecondSkyTests`); each wanted only once the clamp before is open (Mother's order) |
| 3 | Reach `LastMooring` | via Sorrel, the hull | Sorrel's warp on; the hull's | Sorrel, Approach | hull's written, unrun |
| 4 | Flag `end.gateOpen` | encounter `Tollkeeper` in LastMooring | the Tollkeeper's encounter (the boss once its bar is up) | Mooring, Tollkeeper | owed |
| 5 | Flag `end.boarded` | report `Spine` in Continuance, via LastMooring | the spine's top crossing | Mooring (dock), Continuance | owed |
| 6 | Flag `end.meretShip` | encounter `MeretShip` in Continuance | phase one's encounter, in the flight berth | Continuance, Meret | owed |
| 7 | Flag `end.meretGantry` | encounter `MeretGantry` in StillwaterChamber, via Continuance | phase two's encounter on the gantry | Chamber, Meret | placed (chamber engineer, November 14) |
| 8 | Flag `end.crewsOrdered` | encounter `MeretWell` in StillwaterChamber | phase three's encounter in the well | Chamber, Meret | placed (chamber engineer, November 14) |
| 9 | Flag `end.anchorReleased` | report `LastCollar` in StillwaterChamber | the hold's point | Chamber | placed (chamber engineer, November 14) |
| 10 | Flag `end.passageRun` | report `FarEnd` in ReturningPassage | the far end's crossing | Passage | placed (passage engineer, November 14; `ReturningPassageTests`) |
| 11 | Flag `end.tugFetched` | report `TugLine`, else encounter `Passage_Tug` | the tug's fight, then the line's point | Passage, Enemies | placed (passage engineer, November 14; `ReturningPassageTests`): `Passage_Tug` round the tug at s 752, three `StillwaterTowDrone`; the panel's "Clear the tow-drones off her (n/3)" from `TugTow` (`ZoneLine`) |
| 12 | Flag `end.rendezvous` | report `FarMouth` | the far mouth's crossing | Passage | placed (passage engineer, November 14; `ReturningPassageTests`) |
| 13 | Flag `end.handrail` | speaker `Orrin` in NeighbourhoodAfloat | Orrin's Npc at the new board | Afloat | placed: Orrin at (-12.4, 70.4) (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| 14 | Flag `game.complete` | report `Handrail` | the handrail's point, then the line's | Afloat | placed: `Mend the handrail` (-20.6, 26.1) from `end.handrail`, `Repaint the line` (-14.3, 23.4) from `end.railMended`, then `JunLastLine` sets `game.complete` (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |

The opening card is "CHAPTER 10" over "A ROUTE WITH ROOM TO TURN BACK", as `OrrinRoll` closes. The closing card waits on `game.complete` (`cardFlag`); the manifest asks for the credits in its place (section 8). Closing line, for a Continue after the credits: "The convoy is through, and everyone who came is here. There is no hurry." No `next`: there is no chapter after it. Reward: none (it pays after the last scene); the climb from Sync 27 to 30 is the fights' and the side quests' (section 5).

**Chapter 9's hand-off (D273)**: `HouseLeaves.closingLine` is now "Chapter 9 complete. The Halo is a fleet, and every road out runs past Sorrel: hear Idra's plan at the chart table." and `HouseLeaves.next` is `At(ManyHands, report OnePlan, via ManyHandsHull)`. `ContentFrameworkTests.HandOffs` has `10 ManyHands node:IdraOnePlan ManyHands` and `10 ManyHandsHull to:ManyHands ManyHands`.

### The side quests

| Quest | Step | Thing | Zone | Placed by | State |
|---|---|---|---|---|---|
| `LastSurvey` (Sela's chart) | start | `SelaLastSurveyOffer` | ManyHands; NeighbourhoodAfloat | Approach; Afloat | Many Hands' written, unrun |
| | 0: Interact `NurseryRoad` | the young's road through the drilled voids, from `end.motherFreed`: a narrow course that turns twice on itself; its far end a point (report `NurseryRoad`, flag `end.surveyFlown`); flown out and back. An expert course: marked walls, no enemies, a fall restarts at its start (`Road`) | SorrelSecondSky | Sorrel | placed (Sorrel engineer, November 14; `SorrelSecondSkyTests`) |
| | 1: Flag `sela.surveyFiled` | `SelaSurveyHome` at the route chart (report `SurveyHome`) | NeighbourhoodAfloat (and Many Hands' table) | Afloat (Approach) | the terminal's placed at (12.1, -2.9) (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| `Homecomings` (the square) | start | `RuvaPassageRules` | NeighbourhoodAfloat | Afloat | placed: Ruva (-9.4, 57) (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |
| | 0: Interact `Homecoming` x9 | the nine visits' points | NeighbourhoodAfloat | Afloat | placed: `Homecoming: <Name>` on each of the nine, report `Homecoming` (neighbourhood engineer, November 14; `NeighbourhoodAfloatTests`) |

Rewards: `LastSurvey` 200 XP, 160 scrip and a Masterwork scout drive (`DrivesT5Scout`, rolled), and the prospect on the chart (section 13: a stop needs a zone to arrive in, so it is the lead's to decide); `Homecomings` 200 XP, 600 scrip, a Masterwork emitter (`EmittersT5`, rolled: the slot chapter 9's side stories didn't fill) and six Halo Rivets ("the game's last cache").

### Discovery points, by report

| Report | Points | Zone | Placed by |
|---|---|---|---|
| `OnePlan` | Idra's plan | ManyHands | Approach (written, unrun) |
| `Clamp` | four, one at each clamp's berth pad, never offered | SorrelSecondSky | Sorrel |
| `NurseryRoad` | the road's far end | SorrelSecondSky | Sorrel |
| `Spine` | the spine's top crossing | Continuance | Continuance |
| `LastCollar` | the hold | StillwaterChamber | Chamber |
| `FarEnd`, `TugLine`, `FarMouth` | one each | ReturningPassage | Passage |
| `Homecoming` | nine | NeighbourhoodAfloat | Afloat |
| `Handrail` | two: the handrail, the line | NeighbourhoodAfloat | Afloat |
| `SurveyHome` | the route chart's (one or two) | NeighbourhoodAfloat (ManyHands) | Afloat (Approach) |

## 5. Enemies

"Nothing new is introduced": the Compact's crews and machines are chapter 9's, the clamps' and the cage's guns are chapter 8's families, the tug's drones are chapter 9's. The first combat engineer decides each; this is the plan's starting point. An existing enemy is named by its own id where it already fights at this weight; a new id is a new weight or a mode, tuned by data on an existing family.

| Who | Where | Family (as defined) | Proposed |
|---|---|---|---|
| Compact Warden (`CompactWarden`, chapter 9) | the Continuance's decks; the well's door after the yield (stands aside) | `Guardian` that gives ground; stands aside at nought (nobody of Meret's is killed on screen) | **Decided (first combat engineer, November 14): `StillwaterWarden`**, "Compact Warden", in the `CompactWarden` body, chapter 9's guardian that gives ground **in a pair** (chapter 8's mode: one braces its shield drive, the other covers it, neither strikes across the other). Chapter 9's 128 is 8.8% of Idra's bar at Sync 28: baton 146, drive 150. Defined, tested (`LastMooringCombatTests`) |
| Weld Arm, Override Rig (chapter 9) | the anchor works ("the game's earlier machinery, better made") | `Rider` that stands down; `Anchored` | **Decided: `StillwaterWeldArm`** ("Weld Arm", weld 148, seam 44 a second) and **`StillwaterRig`** ("Override Rig", discharge 144), in their chapter 9 bodies. Defined, tested |
| Interceptor (chapter 9) | Last Mooring's lanes between the chains; the Continuance's flight berth | `Strafer` pair | **Decided: `StillwaterInterceptor`** ("Interceptor", run 150, the tractor line breaks on a chain between). Sorrel's valley and Last Mooring's lanes; **none in the Continuance's flight berth** (it is Meret's arena: her Kit may call a pair). Defined, tested |
| Tow-drone (`TowDrone`, chapter 9) | the tug, `Passage_Tug` (three) | `Tetherer` that tows | **Decided: `StillwaterTowDrone`** ("Tow-drone", harpoon 146), three, set facing away from the tug; three in turn the `tugdrones` bark. Defined, tested |
| Flak Gate / an anchor gun (chapter 8's `Anchored` curtain) | each clamp's guns at Sorrel ("fly its defences down"), the Tollkeeper's outriggers | `Anchored` curtain or line | **Decided: `ClampGun`** ("Clamp Gun", in the `FlakGate` body): a curtain of three rows 6 m deep to 18 m, 30 m across, burst 144, firing while a ship is within 24 m. **Set in pairs that cross** (each turned 35 degrees toward the other): no lee behind either, and their quiet rows meet in a cell on the double beat. Defined, tested |
| Clamp Tender (chapter 7) | at a clamp's works on foot, working the clamp shut while the lead holds it (the partner's job is to stop it) | `Tender` | **Decided: `StillwaterTender`** ("Clamp Tender", in the `ClampTender` body, torch 142, integrity 600): chapter 7's is 96, 8% of Taren's bar at 27. Mother of Doors' file steps it out: **one word there (`"ClampTender"` to `"StillwaterTender"`, `BossController.MotherOfDoors.cs` `MotherTenders`), the Mother engineer's**. Defined, tested |
| Helpers | Compact crews who help: no enemy; people (`LeavesAfter`, `Guide`) with the `helpers` bark | | Continuance |

Every ordinary enemy has one tell and one answer, different from its neighbours'; a hit takes 9 to 13% of a hero's bar at Sync 27 to 29 with the chapter's parts (tier 5 from `DevLoadout chapter10`), measured through `Health.Receive` (for scale: chapter 9's Warden's baton, 128, is 11% at Sync 25 to 27). The ledger from Sync 27 to about 30 is `LastMooringBuilder.Enemies.cs`'s: the main quest pays nothing, the two side quests 400 between them.

**As decided (first combat engineer, November 14; defined by `LastMooringBuilder.Definitions`, tested in `LastMooringCombatTests`).** Every hit is authored at 142 to 150: 9.0 to 12.5% of any hero's bar from Sync 27 to 30 in Choirwrought. A zone names each through `Foe(id, standIn)`; the ids are also `LastMooringBuilder.Enemies.Warden`, `.WeldArm`, `.Rig`, `.Interceptor`, `.TowDrone`, `.Gun`, `.Tender`.

**The ledger** (every kill pays the whole party; 10,500 from Sync 27 to 30). Bosses as assumed (each boss engineer sets its own `xp`; a different number moves every later Sync by the difference over about 3,500): **Mother of Doors 1,500** (defined), **the Tollkeeper 1,600, Meret's ship 1,200, her gantry 1,300, her well 1,400**. Ordinary pay: Interceptor 90, Clamp Gun 110, Tender 60, Warden 140, Weld Arm 140, Rig 70, Tow-drone 80. On the main path the party meets **Mother of Doors at Sync 27.1, the Tollkeeper at 28.1, `MeretShip` at 28.8, `MeretGantry` at 29.1, `MeretWell` at 29.5**, ends the main path at 29.9 and with both side quests at 30.0. **Each boss's sheet should say `sync` = 27, 28, 29, 29, 29** (`LastMooringCombatTests.TheLedgerBringsThePartyToEachBossAtTheSyncItsSheetAssumes` fails on a sheet more than .6 off the road).

**Rosters by zone** (what each zone's engineer places; counts are the ledger's):

| Zone | Encounter (id is the zone's) | Who, how many | What it needs of the room |
|---|---|---|---|
| SorrelSecondSky | `Sorrel_Lane1`, `Sorrel_Lane2` | `StillwaterInterceptor` x2 each | as built (80 x 28 boxes across the valley); see docs/maps/SorrelSecondSky.md, "Flight fights contract" |
| SorrelSecondSky | `Sorrel_Clamp1` to `_4` (Mother's fight) | `ClampGun` x2 each, **crossed** | as built, the two spawners turned 35 degrees toward each other (map doc) |
| SorrelSecondSky | (Mother's file steps them out on each dock) | `StillwaterTender` 1, 1, 2, 2 | 3 m clear each side of a seat (her contract) |
| LastMooring | `Mooring_Lane1`, `Mooring_Lane2`: one in each of the first two arenas between the chains, on the way to the convoy's lee | `StillwaterInterceptor` x2 each | each arena at least 40 m across and 28 deep at the plane, a chain (solid at the plane) along at least one side of it: a chain between the pinner and its catch breaks the tractor line, which is how the chains are cover here. Box like Sorrel's lanes, the pair spawned at its far side, no checkpoint of its own |
| LastMooring | `Mooring_Gap`: the gap between two chains the party flies through to the Tollkeeper's lane | `ClampGun` x2, crossed, one on each chain's foot either side of the gap, each turned 35 degrees in from facing the approach | the gap at least 12 m wide (a quiet cell is 6 m and a hull needs 4.4); nothing solid between the guns and the gap; the box round both guns, 30 x 8 x 30. Optional, by the Mooring engineer: an island pad held by a `StillwaterWarden` pair on its shelf (a berth fight on foot, 280 more) |
| LastMooring | (the Tollkeeper's own) | the cage's guns are its moves (6.3). If its engineer wants guns on the outriggers as ordinary bodies, `ClampGun`, crossed in pairs, set on the outrigger at the plane | the Tollkeeper engineer's |
| Continuance | `Continuance_Mess`, `Continuance_Works` | the mess: `StillwaterWarden` x2; the works: `StillwaterWarden` x2, `StillwaterWeldArm` x1, `StillwaterRig` x1 | see docs/maps/Continuance.md, "Crews contract" |
| Continuance | the flight berth | nothing ordinary: it is `MeretShip`'s arena. Her Kit may call a `StillwaterInterceptor` pair | the Meret engineer's |
| StillwaterChamber | none | the gantry and the well are Meret's; the crews that walk in after are people | |
| ReturningPassage | `Passage_Tug` | `StillwaterTowDrone` x3, all at once, round the tug, each set facing away from it along the passage (so a caught ship is hauled off the tug, down the wake) | the box round the tug at the plane, open on the side the party comes from, at least 40 x 30; the tug solid at the plane (it is cover: it breaks a harpoon line); the tug's line prompt more than 15 m from where the drones wake, so the pad-like `TugLine` isn't held by a drone the party is still fighting. The objective's (1/3) counts `StillwaterTowDrone` kills. **The run itself has no ordinary enemies**: its danger is the passage and keeping to the lanterns |
| ReturningPassage | the run | none | |
| NeighbourhoodAfloat | none | | |

## 6. The berth, and the fights' contracts

Chapter 10 changes form inside a zone, inside a fight: a ship docks on a shelf at Sorrel or on the Tollkeeper's arm and the party fights on foot while the boss is still up, then launches. Nothing in the game does that today (a zone's kind, camera and form are its definition's; chapter 5's Hulljack crossed between forms by a scene load *between* two bosses). The brief's six places, the Tollkeeper's "launch before it retracts" and Mother of Doors' "the partner covers" all need the world to keep running while the party is on foot, so the change is made in the zone. **That is the one new system of the chapter, and the place a player will get stuck**: its rules are written here first, and every fight that uses it follows them.

### 6.1 The berth (systems engineer; `S/World/Berth.cs`, `T/PlayMode/BerthTests.cs`)

A `Berth{pad,launch,box,kind,footSpawn,flightSpawn,returnSpawn,camera,inFight,refuseWhileAwake}` is a part of a zone that is of the other form: a shelf of ground in a flight zone (Sorrel's four clamps' works, the Tollkeeper's arm, Last Mooring's maintenance islands), or a stretch of open space beside a ground zone (the Continuance's flight berth round its hull). It is entered and left only by a prompt, never walked or flown into.

| Part | What it is |
|---|---|
| `pad` | the way in: a `DockingPad`-like prompt in the zone's own form, **range on the plan** (D272: `FlightDockRange` 6.5 in flight), "Dock — <the shelf's name>". Answers with a boss bar up when `inFight` is set (as `BridleClamp` and `CoolantSluice` do, `InteractionPrompt.InFight`); refused while any enemy is awake within 15 m of the lead (`EnemyBrain.AwakeNear`), so a ship docks once a clamp's guns are flown down, never under them |
| `launch` | the way out: a prompt in the berth's form at its own place ("Launch — <the zone>"), always offered except mid-hold (a hold being worked is let go first, as a roll lets go of a `BridleClamp`) |
| `box` | the berth's floor (or space): `ZoneController.Berth(kind)` sets the zone's `Kind` to the berth's while the party is berthed, as `FightPocket` and `SafePocket` do; `Apply` runs (input mode, music, form) |
| `camera` | the camera profile the berth is seen with (`Ground` or `Flight`): `PartyController.Apply` and `PlayerBrain`'s stick rotation and `ContinuousReview`'s headings read the **active** profile (today all three read `zone.definition.cameraProfile`) |
| `footSpawn`, `flightSpawn` | where the two heroes are set down, side by side (`HeroCollision.Apart`), on docking and on launching: on foot within 1.5 m of navigation, in flight at the plane, clear of cover by a hull's width |
| `returnSpawn` | a `SpawnPoint` in the zone's own form beside the pad: **the only spawn a save made while berthed may name** |

The rules, each with the fault it prevents:

1. **Docking and launching hold the party** for the change (a fade of about .4 s, input blocked, both heroes moved), then give it back. Nothing is offered or barked in that time.
2. **A save never stands on the other form's ground.** Every autosave made while berthed (a cradle on a maintenance island, a flag the fight saves) writes `returnSpawn`, or, while a boss fight is begun, keeps `state.spawn` as the encounter's `checkpointSpawn`. A load always arrives in the zone's own kind. So a **Continue** after quitting on a shelf comes back in flight beside that shelf's pad (or at the fight's door), with what the flags say done: a clamp opened stays open; a clamp half-held is shut again.
3. **What a berth's work has done is saved when it is done**, through a save that keeps the fight's door (`SaveSystem.KeepCheckpoint()`, new: the flags as they stand, `state.spawn` untouched). Sorrel's `end.clampN` and the Tollkeeper's `end.braceN` are kept this way, so a defeat or a Continue never undoes them. This is a deliberate departure from D204's "the boss at full integrity" for the two bosses whose bar is made of parts (6.2, 6.3), and needs a decision number.
4. **A defeat on foot in a berth is the fight's defeat** (the two out down): the defeat screen, Retry to the encounter's `checkpointSpawn` (in flight, outside the fight's box), the party whole, the boss made again from its saved parts. Never a retry onto the shelf.
5. **A conversation that holds the controls holds everything that moves the heroes** (D275): a berth that moves (the Tollkeeper's arm) stands still while input is blocked, and so does its clock. No berth floor moves under a hero at any time: what moves is drawn round it (the backdrop, the boss's body), never the floor the party stands on.
6. **Navigation for a shelf** is baked in the flight scene (`kit.BakeNavigation` over the shelf's bounds, its seat, its spawn, its launch in `mustReach`): the companion and every reach test need it. Ground enemies on a shelf stay on it (their spawners on it, `wake` within it).
7. **Bodies**: the heroes' Shaped bodies on foot and their ships in flight, exactly as a zone load makes them; the partner comes with the lead. A hero resting stays resting.
8. Logs `BERTH_DOCK <name>`, `BERTH_LAUNCH <name>`, `BERTH_SAVE <spawn>`, `BERTH_REFUSED <why>`; event `Berth.Changed`. A test docks and launches on a bare shelf in `Arena_Flight` (and in reverse in `Arena_Ground`), saves on the shelf and loads (arrives in flight at `returnSpawn`), falls on the shelf mid-fight (retries at the door), and holds a moving berth through a conversation.

Until it lands, every berth is a pad that says why it is refused, and nothing a fight needs waits on it but the dock itself: the fights' engineers build and test their moves in the yards (`Arena_Flight`, `Arena_Ground`) with stand-ins, as chapters 7 to 9 did.

#### 6.1.1 The berth's contract, as built (berth engineer, stream A)

*State (November 14): built and **tested in the yards**, `S/World/Berth.cs`, `S/World/BerthPrompt.cs`, `S/Core/IBerth.cs`, with small edits in `ZoneController`, `SaveSystem` (`SafeSpawn`, `KeepCheckpoint`), `SceneFlow.Fade`, `PartyController`, `PlayerBrain`, `EnemyBrain`/`Health` (forms apart), the driver (`ContinuousReview.Berth.cs`, `docked:`/`launched:`, also in `tools/route_lint.py`) and `QaPlanWorldTests`' two prompt-reach cases (a prompt in a berth is judged in the berth's form). `BerthTests` 7 of 7 in a mirror: dock and launch with no load, forms apart, a save on a shelf continued, a fall on a shelf mid-fight retried at the door, a conversation holding the berth and a moving one, nothing walling a hero in, and the reverse in the ground yard. No zone declares a berth yet, and none has been played.*

**Declaring a berth** (a zone builder, inside its `Build()`; a test, at run time: the same call):

```csharp
// Lattice.World.Berth (S/World/Berth.cs)
public static Berth Declare(string id,string shelf,ZoneKind kind,Bounds box,Vector3 pad,Vector3 launch,Vector3 footSpawn,Vector3 flightSpawn,
    string returnSpawn,Vector3 returnAt,bool inFight=false,bool refuseWhileAwake=true,Transform parent=null)
```

| Argument | What it is |
|---|---|
| `id` | the berth's name in code, logs and routes: `Clamp1` to `Clamp4`, `Arm`, `Island1`, `HullBerth`. Unique in its zone |
| `shelf` | what the pad says: `"Dock — "+shelf` ("Dock — The first clamp's works"). The launch says `"Launch — <the zone's display name>"` by itself. A flight berth beside a ground zone reads the other way round: its pad (on foot) `"Launch — "+shelf`, its way out `"Dock — <the zone>"`. In code and logs "dock" is always going onto the berth and "launch" coming off it |
| `kind` | the berth's form: `ZoneKind.GroundCombat` for a shelf in a flight zone, `ZoneKind.SpaceCombat` for the space beside a ground zone. While the party is berthed `ZoneController.Current.Kind` is this |
| `box` | the berth's floor (or space), world bounds (no collider: `berth.Contains(point)`). A shelf: its bottom at the floor's top, 1 m high, so its top is **under** the flight plane (a ship over the shelf is not "in" it, and a body is read as a flier or not by whether it woke inside a box). A flight berth: the plane, ±1.5 m. Every hero is set down inside it; its four edges are made solid (2 m walls round a shelf) on docking and let go on launching, and nothing else is |
| `pad` | the way in, in the zone's own form: in flight at the plane (`y`=the plane), answered within 6.5 m (`Berth.FlightReach`, as `FlightDockRange`); on foot on a floor in the bake, within 3 m (`Berth.FootReach`) |
| `launch` | the way out, in the berth's form, at its own place inside `box`: on a shelf on its floor (in the bake). The launch answers A with a boss bar up, so stand it at least 5 m from where the shelf's own fight is (the works, the tenders): within its 3 m, A is the launch, not a swing |
| `footSpawn`, `flightSpawn` | where the lead is set down on docking and on launching; the partner goes beside him along the berth's right (world +X unless `parent` turns it; 2 m on foot, `HeroCollision.Apart` in flight). A flight berth's plane is `flightSpawn.y`; a shelf's `flightSpawn.y` is the zone's plane. Each hero is set down clear of every solid collider (moved round the spot in half-metre rings out to 8 m until his body fits, on a floor and inside the box when it is the berth's: D289). Keep `footSpawn` more than 3.5 m from the launch and `flightSpawn` more than 7 m from the pad, so the change's own press is never the next one |
| `returnSpawn`, `returnAt` | the zone's own-form `SpawnPoint` beside the pad that a save made while berthed names (rule 2). The call makes it at `returnAt` unless the scene already has a spawn with that id. In flight: at the plane, outside every boss marking |
| `inFight` | the pad answers with a boss bar up (`InteractionPrompt.InFight`). The launch always does |
| `refuseWhileAwake` | the pad is not offered (log `BERTH_REFUSED <id> awake`) while an enemy **of the lead's own form** is awake within 15 m of the lead (`EnemyBrain.InteractionCalm`): a clamp's guns hold it, the tenders on the shelf under it don't, and at an `inFight` pad the boss doesn't. A pad that isn't `inFight` is also refused in a boss fight (`BERTH_REFUSED <id> boss`), and either prompt while the controls are held |
| `parent` | where the berth's objects go (a zone's root, a moving arm's transform); null: the scene root |

It returns the `Berth` (a GameObject `Berth <id>` at the box's centre, children `Pad` and `Launch` (`BerthPrompt`s), `Foot`, `Flight` and `Edges`; the return spawn goes under `parent`). In a builder, call it after the shelf's floor stands; the shelf's floor itself is not the berth's (put it under the same `parent` if the berth moves). What the builder still builds itself: the shelf's floor and its look, its navigation (rule 6: `footSpawn`, `launch` and every hold in `mustReach`), the pad's and the launch's props. **Constants it publishes** in its zone's nested class (section 0), for the fight and the routes: `internal const string <Id>Berth="<id>"` and `internal static readonly Vector3 <Id>Pad, <Id>Launch, <Id>Foot, <Id>Flight, <Id>Return`, `internal static readonly Bounds <Id>Box`.

```csharp
// LastMooringBuilder.Sorrel.cs (illustrative numbers): the first clamp's shelf, 14 x 10 m, its floor 1.2 m under the plane, south of the clamp.
// Floor top at y -.2 (1.2 m under the plane); the box from -.2 to .8; foot 7 m from the launch; flight place 9 m from the pad.
internal const string Clamp1Berth="Clamp1",Clamp1Return="Clamp1Pad";
internal static readonly Bounds Clamp1Box=new(new Vector3(-40,.3f,212),new Vector3(14,1,10));
internal static readonly Vector3 Clamp1Pad=new(-40,1,203),Clamp1Launch=new(-45,-.2f,214),Clamp1Foot=new(-40,-.2f,209),Clamp1Flight=new(-40,1,194),Clamp1ReturnAt=new(-48,1,196);
Berth.Declare(Clamp1Berth,"The first clamp's works",ZoneKind.GroundCombat,Clamp1Box,Clamp1Pad,Clamp1Launch,Clamp1Foot,Clamp1Flight,Clamp1Return,Clamp1ReturnAt,inFight:true);
```

**What a fight reads.** Combat can't see World, so a boss's file (and any world part it keeps in `S/Combat/`) reads the berth through Core: `ZoneController.Current.Berthed`, `.BerthId`, `.BerthOn` (an `IBerth`, `S/Core/IBerth.cs`), `ZoneController.FindBerth("Clamp1")`, and `ZoneController.Current.Kind`/`.Flight`/`.CameraProfile`/`.FlightPlane`, which are the berth's while the party is on it. An `IBerth` has `Id`, `Kind`, `Contains(p)`, `Occupied`, `MayMove`, `Travel`, `DockedSeconds`, `Drive(position,rotation)`, `Launch(why)`. World-side code has the same on `Lattice.World.Berth` and more:

- `Berth.Current` (the berth the party is on, or null), `Berth.Docked`, `Berth.Changing` (the .4 s change: nothing is offered, nothing moves), `Berth.Find("Clamp1")`, `berth.Occupied`; event `Berth.Changed(Berth,bool docked)`, raised once the party stands in the new form and the controls are back (Combat polls `Berthed` instead). `Berth.LastLaunched` is the id the party last came off.
- A fight's own hold on a shelf that isn't a `BridleClamp` (which already counts) keeps the launch shut while a hand is on it with `Berth.Holds.Add(()=>myHold.Held)`.
- **A berth's work done is kept** with `SaveSystem.KeepCheckpoint()` (Core, rule 3): the flags as they stand, `state.spawn` untouched (log `CHECKPOINT_KEPT <spawn>`). Call it the moment a clamp opens or a brace breaks, after setting its flag.
- **Saves while berthed** (rule 2) need nothing from anybody (`SaveSystem.SafeSpawn`, set by the berth): every save the game makes while the party is berthed names `returnSpawn`, except that while a boss fight is begun (an encounter `Started` and not cleared whose `checkpointSpawn` the state holds, or `BossController.AnyFightUnderway`) a spawn of the zone's own form is kept: the door's (log `BERTH_SAVE <spawn>`).
- **Forms apart.** A body fights only heroes of its own form, and its form is read once, from where it woke (`EnemyBrain.Flier`: inside a berth's box the berth's form, anywhere else the zone's own), so a flier no longer sinks off the plane while the zone is on foot. A flier never marks or fires at a hero on a shelf: it goes home and waits, as for a safe pocket; a ground body on a shelf never fights a ship flying past. A shot or strike already on its way when the party docks or launches **passes through a hero of the other form** (`Health.Receive` gives it nothing). A boss's move that is meant to reach a shelf (marked onto it) says so with `packet.tag=Health.AnyForm`; nothing else crosses. The lead's lock drops a target of the other form at the change, and lock-on and the partner pick only bodies of the hero's own form. A boss's own moves decide for themselves: `ZoneController.Current.Flight` is the party's form while it is berthed, so a move that branches on it (as Floodcrown's do) should ask its own `EnemyBrain.Flier` instead.
- **A berth that moves** (the Tollkeeper's arm, Mother's fourth pad) is moved only through `berth.Drive(position,rotation)`, which does nothing while the party is on it, while the controls are held (a conversation, a menu, the change) or mid-change (rules 5 and D275); `berth.MayMove` says which, and `berth.Travel` is a clock that runs only while it may move. `berth.DockedSeconds` runs only while the party is on it and the controls are free (the arm's retract clock). `berth.Launch("why")` sends the party out to `flightSpawn` as if the launch were pressed (the arm drawn in); log `BERTH_LAUNCH <id> <why>`.
- A defeat on a shelf is the fight's (rule 4): nothing to do; the defeat screen's Retry loads the autosave, which names the door.

**A replay route step** (the driver presses what a player presses, `UI/ContinuousReview.Berth.cs`): fly or walk to the prompt as any other step, then press A until the change is over.

```json
{"name":"fly to the first clamp's pad","seconds":20,"navigate":true,"point":{"x":-40,"y":1,"z":203},"tolerance":2.5,"expectedForm":"Flight"},
{"name":"dock at the first clamp","seconds":6,"buttons":["South"],"pulseSeconds":0.3,"until":"docked:Clamp1","expectedForm":"Shaped"},
{"name":"walk to the launch","seconds":12,"findWay":true,"point":{"x":-43,"y":-0.2,"z":209},"tolerance":1.2},
{"name":"launch from the first clamp","seconds":6,"buttons":["South"],"pulseSeconds":0.3,"until":"launched:Clamp1","expectedForm":"Flight"}
```

`docked:<id>` is true once the party stands on that berth with the change finished; `launched:<id>` once it is back in the zone's own form, that berth the last it came off (pressed, or put out by `Launch`), with the change finished. For the Continuance's flight berth the words are the same: `docked:HullBerth` is flying round the hull, `launched:HullBerth` is back on foot at the bay. Headings in every step read the camera the party is seen with (`ZoneController.Current.CameraProfile`), so a walk on a shelf in a flight zone steers by the ground camera.

### 6.2 Mother of Doors (boss arena contract; SorrelSecondSky)

*The Mother of Doors engineer's section, written by the first engineer as the plan's starting point: what the fight needs of the room. The engineer owns it and corrects it; the Sorrel engineer builds to it and writes the coordinates as built underneath, in docs/maps/SorrelSecondSky.md.*

**How the fight uses the room.** She is coiled round the nursery's crater north of the lane (the camera's far side), her head (`MotherOfDoors`, 16 m) and her segments (`MotherBody`, `MotherTail`: the Cantor's in her grade; `EnemyArchetype.Serpent`'s segmented body) all below the flight plane or north of every lane a ship flies in the fight. Four clamps (`AnchorClamp`) pin her to Stillwater's clamp (`StillwaterClamp`, 40 m) over the crater. Each clamp has guns (an ordinary encounter of its own, `Sorrel_Clamp1` to `_4`) and works on a fixed shelf beside it (a berth). Her sweeps and pressure waves are huge and marked, with lanes wider than two hulls between them; each clamp opened narrows them (her `Kit` reads `end.clampN`), and Hask's hum runs under all of it (`hail.hum`). The fourth clamp's shelf is reached as she strains against it: its pad swings on a slow marked arc in flight; docked, the arc stops (rule 5), and on launch it moves again. At the fourth she goes along her road (a `BossExit` with id `MotherOfDoors`) and the young follow; `bossdown.MotherOfDoors`, then `SurvivorMotherGoes`.

| Thing | What the fight needs | Why |
|---|---|---|
| Boss | Its `Spawner` at the crater's middle C, on the plane, `faceAsPlaced`; `previewPoint` from the lane's entry with a radius that takes C in, so she is on show, coiled and inert, from `end.sorrelSeen` | Her bar is the four clamps (`Health.Holds` at each line until its clamp opens), not damage. **As built (stream C, November 14):** in her room she can't be struck or targeted (nobody shoots at her) and each clamp opened takes a quarter; the clamps open in order 1 to 4; everything she uses lies within 34 m of C (the bar's and her specials' reach), so the seats are on r 23, not 50 (docs/maps/SorrelSecondSky.md, "Boss arena contract", built to by Sorrel). Her sheet's bounds are 120 to 300 s: four real docks with a near-perfect hand took 47 s of her own and 143 s with each clamp's guns allowed 24 s (`LastMooringBossTests`); in a yard with no clamps she fights on integrity (80,000) so the shared length test measures her as any boss |
| The four clamps' guns | four encounters `Sorrel_Clamp1` to `_4`, each its box round its clamp at the plane, each spawner on the clamp's body, `requiredFlag` the door's, cleared before its berth's pad answers | "Fly its defences down, dock": the pad refuses with anything awake within 15 m |
| The four shelves | a berth per clamp (6.1): a shelf of level ground about 14 x 10 m beside the clamp, south of it (on the camera's side), its floor at least 1 m under the plane; `footSpawn` at its south edge, the clamp's works on its north edge; `returnSpawn` beside its pad, outside every marking she lays | The release is done on foot with the partner covering; the shelf is the camera's foreground and nothing on it stands between the camera and the seat |
| The clamp's works | on each shelf, at its north edge: the hold, as at Oriel (`BridleClamp`'s rules: `Take the clamp's weight`, pull 1.2 s, ease .8 s held while she is slack), `openedFlag` `end.clampN`, reporting `Clamp`; a Clamp Tender (or two) working it shut, the partner's to stop | Manifest 8.4 and section 7: the `clamp` bark is chapter 7's; nothing new is introduced |
| Door | Encounter **`MotherOfDoors`**, its box over the four clamps and the lane between them at the plane (open on the side the party comes from, at least four hull widths, 9 m, wide at its mouth: a fight's volume is entered by every hero, and in flight nothing solid stands across it), `requiredFlag` `end.sorrelSeen`, `checkpointSpawn` **`Clamps`** in flight on the approach, outside the box, within 60 m | Shared door tests; the arrow before the bar is up |
| Cover | three rocks (Sorrel's `RidgeRock`s) across the lanes, each solid at the plane, that survive the fight | Her pressure wave stops at cover |
| Camera | nothing drawn above the plane within 18 m south of any lane she sweeps or any pad; her coil and the clamp north of the lanes or under the plane | The flight camera sits 19 m over and 17 m south of a ship |
| Leaving | `BossExit` `MotherOfDoors` at the road's mouth, north | A boss that survives leaves by a placed exit |
| After | the clamps open, the clamp hanging broken (`FlagGate` on `bossdown.MotherOfDoors`), the young gone, the road open (LastSurvey's course) | |

**At each dock:** *checkpoint*: the door (`Clamps`) is the fight's one checkpoint; each opened clamp is saved the moment it opens (6.1 rule 3). *Defeat* anywhere (in flight or on a shelf): Retry at `Clamps`, the party whole, her clamps as saved (the opened ones stay open, her bar held at the clamps left), and the guns of a clamp already flown down still down (their encounter's own clear is saved). The gun encounters carry no `checkpointSpawn` of their own, so their saves keep the door's (`EncounterVolume.Begin` with an empty one leaves `state.spawn` alone and saves nothing; its clear saves with the door's spawn). *Continue* after quitting mid-fight: the save at `Clamps` (or the last clamp kept), in flight, the fight not begun until the party enters the box. *Quitting on a shelf*: the same: never on foot.

### 6.3 The Tollkeeper (boss arena contract; LastMooring)

*The Tollkeeper engineer's section, the plan's starting point; the Last Mooring engineer builds to it and writes coordinates back in docs/maps/LastMooring.md.* **As built (Last Mooring engineer, November 14):** the room stands to this table; every number (the carrier's middle T, the cage's hinges, the dividing chains and the guns' lanes, the arm's root, deck, motor seat and berth `Arm`, the door, `Gate`) is in docs/maps/LastMooring.md, "Boss arena contract", and in `LastMooringBuilder.Mooring`. The door is built once `EnemyDef` `Tollkeeper` exists: **rebuild LastMooring after the Tollkeeper's definition lands.**

**How the fight uses the room.** The carrier (`Tollkeeper`, 90 m) lies across the lane's far end, north of it and under the plane; its outriggers (`TollkeeperOutrigger`, 40 m) form a cage across the lane that contracts and opens on a clock (marked: a cage closing is a danger, never a surprise). Braces on the outriggers are break targets (untargetable before the fight); any three broken open the gap the arm's berth needs (`end.brace1..3`, counted, kept). Its guns fire marked lanes between the anchor chains (`AnchorChain`), which divide the arena and are cover. The arm extends into the lane with its gate motor's berth: dock, stop the motor on foot (a hold or a break target with the partner covering), launch before the arm draws in. The motor stopped is the bar's end: `Ending.ShutDown`, the carrier stays (its file stops its `DefeatPresentation`, and the arena stands a solid stand-in where its colliders were, as chapter 9's Saint), the cage hangs open, the first convoy hulls come through behind the party, `OrrinGateOpen`.

| Thing | What the fight needs | Why |
|---|---|---|
| Boss | Spawner at the carrier's middle T, under the plane, north of the lane, `faceAsPlaced`; `previewPoint` from the convoy's lee | A 90 m body: nothing of it above the plane south of the lane |
| The cage | each outrigger's moving path in the lane, marked as it closes; a ship it closes on is shoved clear and scraped (the place's damage, never the last of it), never crushed; **its clock and its motion stop while input is blocked** (D275) | "Weave out as the cage contracts" |
| The braces | six on the outriggers (any three open the gap), each a break target with `Health`, untargetable until the fight wakes, kept by `end.braceN` once three are counted | "Destroy chosen braces" |
| The arm's berth | 6.1: a deck on the arm about 10 x 6 m, the motor at its north end, `footSpawn` at its south; its retract clock shown (a marked band along the arm) and paused in a conversation; run out, the party is launched to `flightSpawn` (a scrape, never the last of it), the motor's progress lost, the arm out again on its next cycle | "Launch before it retracts": a player who is slow loses a try, never the fight |
| Door | Encounter **`Tollkeeper`** over the lane at the plane, open on the approach side at least 9 m wide, `requiredFlag` `end.sectionHeld`, `checkpointSpawn` **`Gate`** in the lee, outside the box, within 60 m | Step 4's arrow; shared door tests |
| Camera | as 6.2: the carrier and the cage's bulk north of or under the plane within 18 m south of a lane | |
| After | the cage open and still (`FlagGate` on `bossdown.Tollkeeper`), the convoy's hulls moving through (scenery on paths, not moving the party), the dock to the Continuance drawn from `end.gateOpen` | |

**As built (Tollkeeper engineer, stream G, November 14):** `S/Combat/BossController.Tollkeeper.cs` (Kit, `TollTune`), `S/Combat/TollkeeperCage.cs` (the cage: outriggers, braces, the clock's lamps, the motor, the arm's drive, the gate opening), `LastMooringBuilder.Tollkeeper` (the `EnemyDef`, 68,000 for the yards, XP 1,600, Glitch Boss; `StandCage`, which the zone calls; `TollkeeperLook`), sheet and tests in `LastMooringBossTests` (region "The Tollkeeper"). The bar is **three stages of the gate motor**, not the braces: each cycle is one brace broken in flight (six, the player's choice; `end.toll.brace1..6` which, `end.brace1..3` counted), the arm run out, a dock, one stage stopped on foot (`end.motor1`, `end.motor2`; the third is the fall, `end.motorStopped`), a launch. All kept at once (`KeepCheckpoint`). The motor is **not** whole after a defeat: a stage stopped stays stopped (D285); only a stage the arm's clock ran out on is lost. The bar, the panel and the framing reach the whole lane through two new `Kit` fields (`centre`, `reach`, read by `BossController.Focus` and `AnyFightUnderway`; unset for every other boss). The room's needs: docs/maps/LastMooring.md, "What the fight needs of the room".

**At the arm:** *checkpoint*: the door (`Gate`); braces counted are kept. *Defeat* on the arm or in the lane: Retry at `Gate`, braces kept, the motor whole, the cage at its first clock. *Continue*: at `Gate` (or the last kept), in flight. *Running out of arm*: not a defeat: launched, try again.

### 6.4 The Returning Passage (run contract; ReturningPassage)

"Demanding, forgiving: failure restarts the local section and never removes a town from the ending." Three sections, each starting at a checkpoint that saves where it stands (a crossing with `saveSpawn`): **Run** (the arrival to the far end), **Back** (the turn to the tug), **Tow** (the line on to the far mouth). A fall (both ships down) is the defeat screen's Retry at the section's spawn, with every earlier section's flags kept; a Continue the same. The passage's flow, if ships are carried by it, is chapter 6's `AuroraCurrent` (which already holds a ship where it is through a conversation, D275); the convoy's hulls move along their own paths and never push a hero. **The tow**: the tug follows the lead on a line (`end.lineOn`); it slows nobody, and it holds where it is while input is blocked. The nine-line `BrynFallsBehind` and the four-line `IloFirstName` play mid-flight: nothing moves the party during them. **The turn back is Sela's** ("Sela's ship is the one that turns, whoever the player is flying"): with Sela resting, she says it by portrait, and the party's two ships turn (section 13 asks the lead whether the passage brings her out).

**As built (passage engineer, November 14; docs/maps/ReturningPassage.md, "Run contract")**: sections Run (`Arrival`), Back (`Turn`, then `TugReach` once the tug's fight is entered), Tow (`Tow`, where the line is taken). **No current carries a ship** (the flow is drawn: the convoy, the young, Mother of Doors, the streams; a decision asked of the lead in the map doc). Everything that moves (`ConvoyFlow`, `Alongside`, `TugTow`) does nothing while `GameInput.Blocked` holds the controls (D275; `ReturningPassageTests.AConversationHoldsTheConvoyTheYoungMotherOfDoorsIdraAndTheTow`, which shows each moving the old way first). D286 is a reload: once `BrynFallsBehind` closes the party is held 4 s, the pair out becomes Taren and Sela (a lead who is one of them keeps it, else Sela), `end.turnedBack`, and the zone loads again at `Turn`; Idra's cutter (`IdraFlight`, drawn) holds the convoy's west flank from then on.

### 6.5 Meret Venn (three boss arena contracts; Continuance, StillwaterChamber)

Three bosses, three bars, three doors, a checkpoint between each (brief: "no fourth health bar"). Each sheet states its own bounds (about 80 to 150 s each; the three together about five to six minutes), `sync=27`, `driverShare=.3` until a chain has fought it.

| Phase | Zone, room | Door (encounter id, needs, checkpoint) | What it needs of the room | After |
|---|---|---|---|---|
| 1, ship to ship | Continuance, the flight berth round its hull (6.1, in reverse: a ground zone's space) | **`MeretShip`**, needs `end.meretHailed`, checkpoint **`LaunchBay`** (on foot, at the launch bay, outside the berth: a defeat in the berth retries on foot at the bay, which is the zone's own form, rule 4) | the hull's anchor clamps reshaping lanes with visible warnings, tractor lines, a broadside of marked lanes, her craft (`MeretShip`, 9 m) fighting from its shelter; her shield gives back a shot fired at the wrong moment (marked: "Hold your fire: her shield gives it back"); three shield projectors (break targets, untargetable before the fight); her craft leaves by a `BossExit` `MeretShip` at the gantry's dock | `MeretGantry` after-talk; the dock to the gantry |
| 2, the gantry | StillwaterChamber, the frame gantry (the Continuance's moving exterior: the motion is the backdrop's, never the floor's) | **`MeretGantry`**, needs `end.meretShip`, checkpoint **`Gantry`** (the dock's arrival, whose autosave is the checkpoint) | the frame (`MeretGantry`, 3.2 m, rigged) with the ship's maintenance arms: a press (the Saint's slam), a welding line (the Works'), a coolant flood (the Foundry's sluices), each marked as before; two arm couplings to open; three cover anchors that survive every phase; she leaves the frame for the well by a `BossExit` `MeretGantry` at the lift | the lift to the well |
| 3, the well | StillwaterChamber, the control well (compact, circular) | **`MeretWell`**, needs `end.wellEntered`, checkpoint **`Well`** (at the lift's foot, outside the well's seal) | a duel: her counter stance punishes mashing, her clamp-traps punish standing still, every attack marked and none new; three cover anchors; she talks during it (`duel` barks, the third a question with quiet after it); `yieldsOnDefeat`, `Ending.Yields`: at nought she stands | `MeretYields`, then the anchor (6.6) |

A Continue between phases arrives at the next phase's checkpoint with the earlier phases won (their `bossdown` flags and talks saved). Nothing barks over a scripted talk; she does not fall, kneel or shout.

**As built (Meret engineer, stream H, November 14).** All three defined with their Kits (`LastMooringBuilder.Meret`, `MOORING_DEFS_OK`; Final Stand staged), sheets `sync=29` (D296's ledger: 28.8, 29.1, 29.5), `driverShare=.3`, bounds 80 to 150 s, XP 1,200 / 1,300 / 1,400; fought and tested in the yards (`LastMooringBossTests`, Meret region). Every blow 192 to 198, her phases adding nothing. *Ship*: her shield is up unless she fires through it (lance) or her tractor pulls; while up a blow on her is turned and the player's shot that reaches it comes back down a marked lane (the partner's only burns out; she stays locked and framed); three projectors, a third of her bar each, the one struck most going at each third (D281); her clamps are her own, made at the Continuance's three seats (`MeretClamp`); she leaves by `BossExit` `MeretShip`. *Gantry*: press slam, weld, coolant flood, charge, stomp; two couplings (`MeretCoupling`) opened by Oriel's hold while an arm is overreached; the bar held at each third until a coupling opens (each opened takes the bar to its line); she takes the lift down (`BossExit` `MeretGantry`, `sinks`). *Well*: thrust, cut, counter stance (a marked riposte, then her counter is spent), clamp-traps (a ring round the lead, and one under anyone who stands still), lunge; into her last third she asks her question and listens (`duel` line three, four seconds' quiet); at nought she yields standing and `DefeatPresentation.Yielded("MeretWell")` hands over to the chamber's person. Rooms: docs/maps/Continuance.md (built; **rebuild it now**, its door still names the stand-in) and docs/maps/StillwaterChamber.md (the gantry's and the well's contracts, for stream I).

### 6.6 The anchor (the chamber's own)

The crews walk in after `MeretYields` (each a `Guide` leg on `end.crewsOrdered`; the after-talk waits until they stand). `IshenAnchorSequence` plays; on its last word ("Ease.") the hold is offered: `Take the last collar`, one hold through the anchor's cycle, pull twelve seconds and ease three (its own clock, shown on the collar's lamp; Idra calls it), held through an ease to open (as a `BridleStay` eases and a `BridleClamp` opens, guide 15.18, 15.23), let go by a roll and taken again. Opened: `end.collarTaken`, the four `AnchorClamp`s let go in turn and the road moves (a `BridleHold`-like release of light through the chamber, guide 15.19), `MeretSeesItOpen`, `IloCustody`, the launch to the passage. Nothing barks from her "Yes." to Ilo. A save during it loads the anchor as its flags say, with nothing played.

## 7. Objective strings (manifest section 6)

The panel's line is the main quest's step text (section 4) everywhere; a boss's line with its bar up is its `Kit.fightGoal` or its state line (D274: a long one stands as two). **There is still no mechanism for a zone to show a line of its own while the lead is somewhere** (chapter 9 found the same): the rows marked "none" are owed to whoever writes one, or said by barks.

| Zone | When | Line | Mechanism | Owner |
|---|---|---|---|---|
| SorrelSecondSky | a clamp's guns up | Fly the clamp's guns down, then dock | `Kit.fightGoal` (Mother's, by the nearest shut clamp's state) | Mother |
| SorrelSecondSky | on a clamp's works | Take its weight and wait for the slack (1/4) | `Kit.fightGoal` while berthed | Mother |
| SorrelSecondSky | the fourth clamp | Open the last clamp alongside her as she moves | `Kit.fightGoal` | Mother |
| LastMooring | the cage closing | Weave out before the cage closes | `Kit.fightGoal` / state line | Tollkeeper |
| LastMooring | on the arm | Stop the gate motor before the arm draws in | `Kit.fightGoal` while berthed | Tollkeeper |
| StillwaterChamber | her shield up | Hold your fire: her shield gives it back | MeretShip's state line (it is fought in the Continuance) | Meret |
| StillwaterChamber | the anchor | Wait for Idra's call | none (the hold's prompt and Idra's line say it) | Chamber |
| ReturningPassage | the run | Keep to the lanterns: two short, one long | step 10's text says the run; the count is `hail.count` | Passage |
| ReturningPassage | turned back | Fly against the wake to the tug | step 11's text | Defs |
| ReturningPassage | at the tug | Clear the tow-drones off her (1/3) | none | Passage |
| NeighbourhoodAfloat | `end.epilogue` | There is no hurry | step 13's text names Orrin; the closing line says it after the credits | Defs |

## 8. Barks (manifest section 7; the lines are in docs/writing/barks.json) and the credits

Nothing barks during the roll, between "Yes." and `IloCustody`, or after Taren's last word.

| Key | Speaker | Trigger | Zone | Placed by |
|---|---|---|---|---|
| `bossphase.*`, `bossopening.*` for the five bosses | heroes | `BossController.Situation` | | automatic for those ids |
| `mother` (wide, then narrower), `young` | heroes | her sweep; the young moving in the bore (`FirstSight`) | Sorrel | Mother, Sorrel |
| `hail.hum` | Hask (three in turn), Iru (after the first and the third clamp) | `RadioHail` over the fight; Iru's on `end.clamp1`, `end.clamp3` | Sorrel | Mother |
| `clamp` | heroes | each release (chapter 7's) | Sorrel | Mother |
| `sectionhit`, `hail.mats` | heroes; Maru | the section struck; after it, looking back | Mooring | Mooring |
| `tollbrace` | heroes | a brace broken (three in turn) | Mooring | Tollkeeper |
| `standaside`, `helpers`, `warden` | heroes | crews who step back, one who helps; a Warden giving ground (chapter 9's) | Continuance | Continuance, Enemies |
| `hail.aside` | Patrol | after `PatrolLetsPass` | Continuance | Continuance |
| `hail.spine` | Idra | when she isn't the active hero (two in turn) | Continuance | Continuance |
| `ship`, `gantry`, `duel` | Meret | one set a phase (`BarkService.Play("Meret", …, true)`) | Continuance, Chamber | Meret |
| `returnfire` | heroes | her shield up | Continuance | Meret |
| `oldmachines` | heroes | the press, the welding line, the coolant, as each is first used | Chamber | Meret |
| `hail.gear` | Tovra | as the smiths fit the gear, before `IshenAnchorSequence` (three in turn) | Chamber | Chamber |
| `passage`, `youngchoir` | heroes | the run begins; the young come alongside | Passage | Passage |
| `hail.count`, `hail.names`, `hail.board`, `hail.manifest`, `hail.step`, `hail.cradle`, `hail.rear`, `hail.lead` | Pell, Ilo, Orrin, Mira, Dorrow, Hal, Neve, Bryn | the run's turns and moments (manifest's table) | Passage | Passage |
| `wake`, `tugdrones` | heroes | flying back up the lane; a tow-drone cleared (three in turn) | Passage | Passage, Enemies |

**The credits** (UI engineer; manifest section 10): after `JunLastLine` closes (`game.complete`), no "chapter complete" plate (chapter 10's closing card is replaced, not shown: `ChapterCards` needs a rule for the last chapter), the two lines over the convoy under its new sky, the roll from `docs/CREDITS.md` under the manifest's headings, "The pears keep a schedule." alone, then the title. Title Theme plays (`Credits` names it). A save made after `game.complete` loads into the neighbourhood with the closing line. **The neighbourhood's side, placed (neighbourhood engineer, November 14; docs/maps/NeighbourhoodAfloat.md, "For the credits")**: `JunLastLine` is an `AfterTalk` (heard flag `game.complete`, which its own last line sets with `<<setFlag>>`, so the main quest's step 14 moves in the talk); the credits start when that talk closes with `game.complete` set. From `end.linePainted` a `QuietAfter` hushes every bark for good, so nothing is heard between Taren's last word and the roll. The town after it is walkable as it stands (Orrin quiet at his board, the line painted, the pears past it), arriving at `Arrival`. **The credits' side, built (UI engineer, stream L, November 14; `S/UI/Credits.cs`, `CreditsRoll.cs`, `T/PlayMode/CreditsTests.cs`)**: only in NeighbourhoodAfloat, and only when a conversation closes having put `game.complete` up (or the town is entered with it up and `credits.rolled` not, a quit mid-roll); a flag set any other way rolls nothing. The party is held and the HUD gone for the roll; Title Theme from its top (`Credits.Music`, one line); a camera of its own draws back from the party to the town's LookDev "overview" and drifts north past "overview-north" to the gallery rail (0, 96), where the convoy's two big hulls and the giant share the frame; the two lines, the roll (`CreditsRoll`), the player's line held, "The pears keep a schedule." alone, the wordmark; then the camera comes home and the hold lets go at the stall. Hold A hurries (x5); Start skips only once a roll has been seen on that machine. At the end `credits.rolled` is set and the autosave written; the last quest's QUEST COMPLETE card is dropped; the panel reads `TurnBack.closingLine`. The title's subtitle reads "THE CONVOY IS THROUGH." once any save holds `game.complete`. Logs `CREDITS_BEGIN`, `CREDITS_SKIP`, `CREDITS_RETURN`, `CREDITS_END`.

## 9. People, by zone

| Who | Where | Body | Placed by |
|---|---|---|---|
| Orrin | Many Hands' board (Arrivals' stand-in, from `home.launched`); radio through the operation; the neighbourhood's new board | `Orrin` | Approach (after-talk), Afloat |
| Idra | in the party; her crews on the spine (Kott forward, Sibb aft: Many Hands' arrivals have their bodies) and at the anchor's braces | | Continuance, Chamber |
| Dace (`Survivor`), Hask, Iru | radio at Sorrel; the neighbourhood | chapter 1's, chapter 6's | Sorrel (voices), Afloat |
| Maru, Ruva, Teo, Pell, Tovra, Brekk, Mira, Hal, Neve, Ilo, Bryn, Dorrow, Oda, Abri | radio in the operation; the neighbourhood (each in their community's street) | their own | Afloat |
| The captain of the *Steady Hand* (`Patrol`) and his crew | the Continuance's maintenance deck four | **`Patrol` has no body**: owed (section 11) | Continuance |
| Meret | her craft (phase 1, voice); the gantry's rail (seen); the gantry (frame); the well (emergency frame); standing after; the neighbourhood's plain room | `MeretShip`, `MeretGantry`, `MeretWell`, `MeretBody` | Meret, Continuance, Chamber, Afloat |
| Ishen, Tovra's smiths, Idra's people, civilian crews | the well, after the yield | their own; smiths and crews as townsfolk | Chamber |
| Jun, Jask, Pip, Pim, Dov, Corra, Binna, Tam, Drask | the neighbourhood | their own | Afloat |
| The navigator with the pencil | leading the convoy in the passage (a ship), and at the travel terminal | a Nacre navigator (Many Hands' arrivals) | Passage, Afloat |

**Three heroes, two out**: `DevLoadout chapter10` arrives with Taren and Idra out and Sela resting. Every scene where a hero must be out (Sela's turn in the passage, Taren at the last collar) says so in its zone's map doc and either works with whoever is out or brings that hero out where a player could (section 13).

## 10. The faults chapters 3 to 6 made, applied here (D272 to D275)

Each engineer checks their own rows against this before reporting; the lead looks at all of it.

- **A prompt is reached on the floor plan from where a body can stand** (D272). Every point, pad, berth and hold names the floor a hero stands on to press it, and that floor is in the bake (on foot) or clear at the plane (in flight); flight pads use `FlightDockRange`. `RouteFightTests` and `EveryPromptCanBeReachedOnFoot` check it per zone; a talk through a grille or a rail reaches from where a body stands (Pim, D272).
- **A fight's volume can be entered by every hero.** In flight a fight's box spans the plane and its mouth is at least 9 m (two `HeroCollision.Apart`) wide with nothing solid across it; on foot a door's seal opens at least 3 m and its box starts three metres inside it (the partner is through before it shuts, `EncounterVolume.TryEnter`).
- **A boss is sized from what the party really deals, and its sheet says its bounds.** Each of the five sheets states `minSeconds`/`maxSeconds`, `sync=27`, and `driverShare=.3` until a chain; once a chain fights it, the bar is resized from `BOSS_RATE` and `BOSS_DURATION` (guide 10). Mother of Doors' and the Tollkeeper's bars are made of parts: their bounds are cycles of fly, dock, work, launch, measured in a test that docks.
- **Nothing drawn stands between the fixed camera and a fight.** Flight: nothing above the plane within 18 m south of a lane a fight uses (the camera is 19 m over and 17 m south of a ship); the Tollkeeper's 90 m, the Continuance's 180 m and Stillwater's 40 m bodies lie north of the lanes or under the plane. On foot: the shelves' and the gantry's south and west edges are low (guide 3.5); the well's rim on the camera's side is under the sightline (`CanopySeen`'s rule, guide 15.2).
- **A conversation that holds the controls holds everything that moves the heroes** (D275): the Tollkeeper's arm and its clock, the cage, Mother's fourth-clamp arc, the passage's flow, the tow, the convoy's hulls (they never push a hero), any lift between the gantry and the well. Each has a test that holds it through a conversation.
- **A chapter's end says where the next thing is** (D273): chapter 9's now does (section 4). Inside the chapter every step's text names the place and its arrow finds the way (section 4's "Thing" column); the two short gaps where a step's text runs a few seconds behind the world (step 6 to `MeretGantry`, step 7 to the well's door) are closed by placing the talk at once (an after-talk on the boss's fall; the well's crossing within 10 m of the lift).
- **A boss's state line too long for its room stands as two** (D274): any new long line is added to `QaNov10HudTests.ABossStateTooLongForItsRoomStandsAsTwoLinesClearOfTheSkillRowAndTheHints`.

## 11. Art (for the lead: nothing ordered)

**Ledger.** Chapters 7 to 10 share a cap of 720 credits (SHIPPING_PLAN section 7). Spent, per docs/ASSET_SOURCES.md (November 12): chapter 7 176, chapter 8 181, chapter 9 164, and **chapter 10 already 164** (batch `C17-Stillwater`, `tools/meshy/c17_stillwater_batch.py`, intake `tools/c17_intake.py`): 685 of 720. **35 credits are left for chapter 10.** (The live balance after the last orders was 206, less 54 spent since for chapters 4 and 6.)

**Already made, taken in and prefabbed** (`A/Prefabs/...`; none yet seen in a chapter 10 scene):

| Piece | What it is | Used for |
|---|---|---|
| `MotherOfDoors` (16 m head), `MotherBody` (9 m), `MotherTail` (11 m) | her head (generated) and the Cantor's segments in her grade | Mother of Doors, coiled; at a distance in the passage |
| `StillwaterClamp` (40 m), `AnchorClamp` (6 m), `AnchorChain` (30 m) | Stillwater's clamp over Sorrel; Oriel's Bridle clamp in Compact white; an anchor chain | the clamp and its four clamps; Last Mooring's dividing chains; the anchor's four clamps in the chamber |
| `Tollkeeper` (90 m), `TollkeeperOutrigger` (40 m) | the carrier; one outrigger the fight places and moves | the cage |
| `Continuance` (180 m) | the Tollkeeper's hull in Meret's gold | the Continuance outside (Last Mooring's far end, its own flight berth, the gantry's backdrop) |
| `MeretShip` (9 m) | her craft | phase one |
| `MeretGantry` (3.2 m, rigged), `MeretWell` (1.85 m, rigged), `MeretBody` (chapter 5) | her heavy frame with press and welder arms; her emergency frame (a coat on her own body); herself | phases two and three; the epilogue |
| `CompactWarden` (rigged), `CompactCutter` (chapter 5) | the Compact's crews and rescue craft | the Continuance; Last Mooring's Compact ships |

**What chapters 1 to 9 built that stands in, honestly** ("nothing new is introduced"):

| Need | Stand-in | Gap |
|---|---|---|
| Sorrel's ridge skyline, drill mast, crater | Sorrel's kit: `RidgeRock1-3`, `MoonGroundA/B`, `OutpostDrill`, `OutpostHab`, `CrystalClusterA/B`; Hushwell's (`HushwellWall`, `Column`, `Shells`, `ChoirGrowth`, `EggCradle`, `CradleHatched`) for the apertures and the nursery | none |
| The young in the voids and in the passage | the Cantor's segments (`CantorHead/Body/Tail`) at a small scale in a grade of their own (as chapter 8's young road) | a grade at intake (0) |
| The shelves and the clamps' works | `AnchorClamp`, Hushwell's `Scaffold`/`Lift`/`Crates`, station kit, code-built machinery | none |
| The clamps' guns, the Tollkeeper's guns | `FlakGate` (chapter 8) | none |
| Braces, gate motor, shield projectors, the arm's deck | code (`ZoneKit.Shape`), `PowerCell`, `CoreRack`, `DeckConsole`; projectors as `Lattice/EnergyBubble` domes | none |
| The convoy (every hull a place the player knows) | `TallowStationHull`, `Hauler`, `Skiff`, `PatrolCutter`, `CompactCutter`, `SeedBarge`, `ReedRaft`, `CrawlTread`, `RingSegmentA/B`, `RibArch`, `GreatLantern`/`LanternTower` on floats; Many Hands' and the Halo's districts as built in code (lift their builders' pieces into a shared helper rather than copying) | none |
| The Continuance inside ("the game's earlier machinery, better made") | the Works' (`SaintArm`, `WeldArm`, `OverrideRig`, `FoundrySaint`), the Foundry's, Oriel's `BridleClamp`, the station kit (`DeckFloor`/`Wall`/`Doorway`/`Console`/`Crate`/`Bench`) in Compact white and teal (`PropTint`), floor signs in its neat hand | none |
| The gantry and the well | code-built decks, rails and a round well (the Frozen Host's and the Glass Choir's round rooms), `AnchorClamp` x4, three cover anchors | none |
| The tug *Welcome Back* | code-built twice already (Many Hands' arrivals, Nacre Grave): lift one into a shared helper | none |
| The lanterns ahead | `LanternShard`, `GreatLantern`, `LampPost`, `RhythmLamp` counts (chapter 4) | none |
| The neighbourhood's streets | the Decks' market (`DressMarketStall`, `DressKiosk`, ...), Many Hands' arcade and garden (built), Driftreed's `ReedRaft`, Lastlight's `RibArch` and `KeptLonghouse`, Lanternwick's `LampPost`/`GreatLantern`/`AncelaneArch`, the Crawl's sky-garden deck (built), Hal's benches (`LatticeAnvil`, Ket's bench as built), the pear tree and stall (DecksLeaving's) | none |
| A new sky | `ZoneKit.Sky` turned from the panorama (`neighbourhood-afloat-sky`, defined), `WorldArt.Planet` | none |
| Every person's body | their own (every speaker but one has a body) | **Patrol** (the captain of the *Steady Hand*) |

**What must be generated** (the lead's to order):

| Piece | How | Credits |
|---|---|---|
| The captain of the *Steady Hand*'s body (`PatrolBody`) | first a grade at intake of an existing Vael man's coat (Dessik's or Orrin's) in the Compact's white and teal: 0. Only if that reads as somebody else: an original-UV retexture of the same body (D214: a named person needs his own body, not another's coat) | 0, or 10 |
| Held back | for a coat or a piece the first look pass rejects (one retexture or one board cell) | 25 |

Nothing else needs generating. Chapter 10 would end at 685 to 695 of 720.

## 12. Order of building, and the fan-out

### The order things must be built in

1. **Now (done):** definitions, placeholders, the script, Many Hands' staging, chapter 9's hand-off.
2. **First, in parallel, because everything else waits on them:** the **berth** (6.1: Sorrel's and Last Mooring's fights, Last Mooring's islands and the Continuance's flight berth all dock through it); the **Sorrel zone** with Mother of Doors' contract (the chapter's first place, and the berth's first real use); the **Continuance zone** with the crews' contract (the biggest place, and phase one's room).
3. **As each zone's map doc and its fight's contract are agreed:** the zone builds its rooms to the contract and writes the coordinates back; the fight builds its moves in the yards with stand-ins and moves into the room once it stands.
4. **Then:** Last Mooring and the Tollkeeper (after the berth); the chamber and Meret's phases two and three; the passage (no new system; after Last Mooring only for the convoy's shared hull pieces); the neighbourhood (independent of everything but the people; last in story, but can start now).
5. **Last:** the credits (needs `game.complete` only), the routes (`tools/mooring_routes.py`, after each zone's `RouteFacts.Encounters`), the look passes.

### The fan-out (streams to start)

| Stream | Owns | Can start today on | Waits for |
|---|---|---|---|
| **A. Berth (systems)** - start first | `S/World/Berth.cs`, the small edits in `ZoneController`, `PartyController`, `PlayerBrain`, `ContinuousReview`, the save that keeps a door (`SaveSystem.KeepCheckpoint`), `T/PlayMode/BerthTests.cs` | all of it: section 6.1 is the contract; test on bare shelves in the yards | nothing; it answers the lead's decision on 6.1 rule 3 |
| **B. Sorrel (world)** - start first | `LastMooringBuilder.Sorrel.cs`, docs/maps/SorrelSecondSky.md | the map brief, the lane, the ridges, the crater, the four shelves as level ground with their pads drawn and refusing, the crossing, the warps, the young's road | the berth for its pads to dock; the Mother engineer's contract for exact positions |
| **C. Mother of Doors (combat)** - start first | `LastMooringBuilder.Mother.cs`, `S/Combat/BossController.MotherOfDoors.cs`, her sheet in `T/PlayMode/LastMooringBossTests.cs` | the definition and the Kit together, her segmented coil (the Cantor's `Serpent`), sweeps and pressure waves narrowing by clamps, the hold at a clamp on a bare yard floor, the contract in 6.2 | the berth (for the dock in the real zone); Sorrel's shelves |
| D. Continuance (world) | `LastMooringBuilder.Continuance.cs`, docs/maps/Continuance.md | the spine, the decks, the works, the captain's deck, the launch bay, the crossings, the dock in | the berth for the flight berth; the crews' contract (E) |
| E. Enemies (combat) | `LastMooringBuilder.Enemies.cs`, `T/PlayMode/LastMooringCombatTests.cs` (tells, the ledger) | the roster (section 5), the contracts for the Continuance's crews, the clamps' and the cage's guns, the tug's drones | nothing |
| F. Last Mooring (world) | `LastMooringBuilder.Mooring.cs`, docs/maps/LastMooring.md | the arenas and chains, the convoy in the lee, the section struck, the islands' shelves, the dock to the Continuance | the berth (islands, the arm); the Tollkeeper's contract |
| G. Tollkeeper (combat) | `LastMooringBuilder.Tollkeeper.cs`, `S/Combat/BossController.Tollkeeper.cs` | the definition and the Kit, the cage's clock, the braces, the guns' lanes, in the flight yard | the berth (the arm) |
| H. Meret (combat; may be two: flight and foot) | `LastMooringBuilder.Meret.cs`, `S/Combat/BossController.MeretShip.cs`, `.MeretGantry.cs`, `.MeretWell.cs` | all three Kits in the yards; the contracts in 6.5 | the berth (phase one's room only); the chamber's rooms |
| I. Chamber (world) | `LastMooringBuilder.Chamber.cs`, docs/maps/StillwaterChamber.md | the gantry, the lift, the well, the anchor's release (6.6), the after-talks, the launch | Meret's contracts for exact positions |
| J. Passage (world) | `LastMooringBuilder.Passage.cs`, docs/maps/ReturningPassage.md | the passage, its sections and checkpoints, the convoy's flow, the tug and the tow, the cut to the epilogue | E's tug fight (a stand-in meanwhile); the lead's answer on Sela (section 13) |
| K. Neighbourhood (world) | `LastMooringBuilder.Neighbourhood.cs`, docs/maps/NeighbourhoodAfloat.md, `NeighbourhoodStop` | all of it: the streets, the people, the visits and extras, the handrail and the line, the last scene | nothing (the credits follow it) |
| L. Credits (UI) | `S/UI/` credits roll, `ChapterCards`' last-chapter rule | the roll, the two lines, the headings, the title's return | `docs/CREDITS.md` |
| M. Art runner | the intake grades (Patrol, the young), the batch only if the lead orders | the grades (0 credits) | the lead, for anything that costs |

### What runs first once Unity is back (the first engineer's, in this order)

```powershell
& .\scripts\exec-when-green.ps1 -Method Lattice.EditorTools.LastMooringBuilder.Dialogue -Marker MOORING_DIALOGUE_OK       # the Yarn reimport: 78 nodes
& .\scripts\exec-when-green.ps1 -Method Lattice.EditorTools.MusicIntake.Stage -Marker MUSIC_STAGED_OK                     # Regal Alien Town is named for the first time
& .\scripts\exec-when-green.ps1 -Method Lattice.EditorTools.LastMooringBuilder.BuildZones -Marker MOORING_ZONES_OK -Graphics -TimeoutSec 2400   # six placeholders
& .\scripts\exec-when-green.ps1 -Method Lattice.EditorTools.Restage.All -Marker RESTAGE_OK -Graphics -TimeoutSec 3000 -ExtraArgs @('-restage-scenes','ManyHands,ManyHandsHull')
& .\scripts\exec-when-green.ps1 -Method Lattice.EditorTools.RouteFacts.Encounters -Marker ROUTE_FACTS_OK
git checkout -- Lattice/Packages/packages-lock.json Lattice/Assets/_Project/Resources/WorldMaterials/staged-light-line.mat
.\scripts\focused.ps1 -SkipVerify -Label ch10first -Classes 'LastMooringWorldTests|ContentFrameworkTests|ChapterCardTests|RouteChartTests|Lattice.Tests.PlayMode.QaPlanWorldTests.EveryExitNamesWhereItGoesAndItsArrivalIsThereAcrossZones|Lattice.Tests.PlayMode.QaPlanWorldTests.EveryConversationLockAndQuestStepIsWiredToSomethingInTheWorldAcrossZones|Lattice.Tests.PlayMode.QaPlanWorldTests.NoQuestMovesOnOutOfTurn|Lattice.Tests.PlayMode.QaPlanWorldTests.EveryQuestsCardsFollowItsStepsAndItsRewardIsPaidOnce' -Zones 'SorrelSecondSky,LastMooring,Continuance,StillwaterChamber,ReturningPassage,NeighbourhoodAfloat,ManyHands,ManyHandsHull'
```

`-Zones LastMooring` also runs `DialogueInputTests`' case for LastMooring.yarn, and `ManyHands`/`ManyHandsHull` their `RestageTests` cases. Expected red until their places are built: `EveryConversationLockAndQuestStepIsWired...AcrossZones` on `LastSurvey` (report `NurseryRoad`) and `Homecomings` (report `Homecoming`), which nothing in the world answers yet; and the placeholders' look cases.

**Start A, B and C first.** The berth is the one system every flight fight and three places dock through, and it carries the save rule a player gets stuck on if it is wrong; Sorrel and Mother of Doors are the chapter's first place and fight and the berth's first real use, so they find its faults while the rest is still being blocked out. D, E and K can start the same day without waiting on anyone.

## 13. For the lead to decide (decision numbers wanted)

1. **The berth** (6.1): the in-zone change of form, and rule 3 (a boss whose bar is made of parts keeps the parts through a defeat and a Continue, a departure from D204 for Mother of Doors and the Tollkeeper). The alternative is a scene load at every dock, which keeps D204 whole but freezes the world while the party is on foot, resets the boss at every dock and adds four to six scenes.
2. **Sela in the passage**: "Sela's ship is the one that turns, whoever the player is flying". Either the passage brings her out where a player could, on arrival, or the turn is spoken by portrait with whoever is out.
3. **LastSurvey's course and prospect**: the course in Sorrel from `end.motherFreed`, reached before the passage and from the neighbourhood's moorings; the prospect as a chart stop needs a zone to arrive in (the course's start would do).
4. **Chapter 10's closing card**: suppressed for the credits (section 8), which needs a `ChapterCards` rule for the last chapter. *Decided (D288) and built*: `ChapterCards.EndsOnCredits` (the quest whose `cardFlag` is `game.complete`) never ends on a card; it still opens on one (stream L; `CreditsTests`).
