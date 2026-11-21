# The Returning Passage - the last flight, the convoy run, the tug and the rendezvous

2026-11-14. Builder: `LastMooringBuilder.Passage.cs` on `ZoneKit` (the passage engineer, stream J). Chapter brief: [ch10-a-route-with-room-to-turn-back.md](../chapters/ch10-a-route-with-room-to-turn-back.md), "ReturningPassage". Who places what: [ch10-wiring.md](../chapters/ch10-wiring.md) sections 1, 2 "The Returning Passage", 4 (steps 10 to 12), 5 (`Passage_Tug`), 6.4, 13 (D286). Script: [ch10-manifest.md](../writing/ch10-manifest.md) (`IloFirstName`, `BrynFallsBehind`, `BrynOnTheLine`, `OrrinRendezvous`; barks `passage`, `youngchoir`, `hail.*`, `wake`, `tugdrones`). Tests: `T/PlayMode/ReturningPassageTests.cs`. **MAP_EYE_TEST: plan written before construction (this page); stills under "Evidence".** Every number below is an `internal static readonly` (or `const`) in `LastMooringBuilder.Passage`; read it from there, never copy it.

## What the place is for

The anchor has let go and the road moves. The Returning Passage is the bore of the adult Choir that Stillwater pinned, open again: a living passage hundreds of metres across, and the whole convoy running it home together. It is the game's reward flight. The party flies escort down a lit road through the middle of it, every hull the player has known flowing down the bore around and under them (the Halo's districts, Many Hands, Tallow's hull, the seed barges, the raft bay, the Crawl's treads, the ring under tow, Lastlight's longhouses, Nacre's cutters, Oda's hauler, Dace's outpost, the Compact's cutters now helping), the Wick-folk's lantern chain marking the road's edges and pulsing ahead toward the mouth, the young Choir swimming alongside, and Mother of Doors far off below, going the same way. Ilo reads the names off the radio as each household comes through. Near the far end Bryn's tug falls behind; Sela turns back with Taren, the convoy closes to the lanterns and leaves her the middle of the road, they fly back up against the wake, clear the tow-drones off the *Welcome Back*, take her line and bring her through the narrows to the far mouth, where Orrin's board says "all through" and the game cuts to SEVEN WEEKS ON.

**The rule: keep to the lanterns.** The road is the lit middle of the bore; its two edges are the lantern line (the Wick-folk's towers on floats under the plane, and their count, two short and one long, lit along the edge at the plane); the edge is solid where the lanterns are. The count pulses run ahead along the line toward the mouth, so the way on is always the way the light goes.

## Rules of construction

- Everything flies at the plane (y 1). Nothing is drawn above y 1.5 anywhere a ship can be: the bore's upper half is cut away for the camera (the Gullet's rule). The lantern towers stand on floats under the plane, their tops at y -1.5 or lower; the count along the edge is beads at y 0.6 to 1.1, beyond the solid edge. Down the road's middle the Wick-folk's lantern buoys, in threes every 22 m (two small, one long), lie a hand under the plane: from the road's middle the edges are out of frame (the road is 60 m across, the frame 40), so these are the lanterns ahead in every frame, and every third keeps the count with a light whose pulse runs north along the road at 38 m/s, as the edges' do.
- The bore: its floor 178 m down (rising to 62 in the narrows), its ribs a hoop of light every 70 m, three faint streams of light flowing north 50 to 110 m down; past the far mouth's lip, open sky.
- The convoy runs in six lanes: two under the road (±10, one pace, their groups laid so no footprint meets another), two outside it (±50) and two deep (±44, ±46, 24 m and more down: Tallow, the ring, the Crawl's treads; at ±34 a pair of ring segments 37 m long still reached under the road's middle once the convoy had closed). Under the road run no cutters and no docking pods (sharp-nosed or wide, close under the heroes they read as fighters or filled the frame).
- **The convoy is drawn only and flows on its own paths** (`S/World/ConvoyFlow.cs`): every hull's top is at least 3 m under the plane (y -2 or lower), it has no collider, nothing of it is between the camera and a ship, and it never moves a hero. It flows north along the passage's line at 5 to 7.5 m/s, each group at its own pace, comes round again south of the arrival out of sight, closes to the lanterns from `end.passageRun`, and **holds where it is while a conversation holds the controls** (D275).
- **No current carries a ship** (a decision for the lead, below). The passage's flow is drawn: the convoy, the deep streams of light, the young, Mother of Doors, the lantern pulses all go north; ships fly free at their own speed. Nothing at the plane is carried, so the tug's fight plays as in still space (D282 does not arise: nothing here carries a body, a marking or a shot).
- The road is 60 m wide (half 30) from the arrival to the far end, narrows to 28 m (half 14) over the last 70 m before the far mouth ("as the passage narrows"), and opens past the mouth into the new sky (half 44). Its line bends three times (Pell's count is at each bend).
- The camera's side: nothing over the plane within 18 m south of the tug's fight (nothing over the plane anywhere in the road, in fact); the tug herself is cover at the plane (solid while she waits; drawn 2.8 m tall, her top at y 2.8).
- Every place is published by distance along the passage's line (`Passage.On(s, side)`): `s` from the line's start, `side` metres to starboard (east, positive) of it.

## Plan

North is up, x east, z north. The line runs from (0, -40) through bends at (0, 170), (-36, 450) and (20, 760) to (0, 1050), straight on through the narrows to the mouth at (0, 1240) and out to (0, 1345). `s` is distance along it (the bends at s 210, 492, 807; the far end at 1080; the mouth at 1288).

```text
  z 1345 ..... north limit, the new sky (half 44)                              s 1393
  z 1300       the rendezvous: Many Hands and the first hulls through, in open space
  z 1240  ))((  THE FAR MOUTH: OrrinRendezvous's crossing (needs end.tugFetched)    s 1288
  z 1170  )  (  the narrows (half 30 -> 14), great lanterns on the lip               s 1218
  z 1032  ====  THE FAR END: BrynFallsBehind's crossing (report FarEnd)              s 1080
  z 1012        spawn Turn (after the turn; facing south)                            s 1060
  z  760   \    bend three (Pell: "Still afloat")                                    s  807
  z  752   |    checkpoint TugReach (the fight's door; facing south)                 s  800
  z  713   |    THE TUG Welcome Back on the road's middle (from end.passageRun)       s  760
  z  690   |    Passage_Tug: three tow-drones 16 to 22 m south of her, facing south   s  738-744
  z  450  /     bend two (Pell: "Lanterns are lit to the bend")                       s  492
  z  170  |     bend one (Pell: "Here. This way.")                                    s  210
  z  130  |     IloFirstName's crossing (the young alongside)                         s  170
  z    0  *     ARRIVAL (0, 1, 0), facing north: the launch from the anchor chamber   s   40
  z  -40 ..... south limit                                                           s    0
```

## Coordinates (as built; `LastMooringBuilder.Passage`)

| Place | s, side | World (x, y, z) | Id / flag |
|---|---|---|---|
| Arrival | 40, 0 | (0, 1, 0), facing north | spawn `Arrival` (the chamber's `Launch — The Returning Passage` lands here) |
| The run begins | 50 | | the lead's `passage` |
| The young come alongside | 120 | | the lead's `youngchoir` |
| IloFirstName | 170, across the road (64 x 8 x 6) | (0, 1, 130) | crossing, flag `end.namesBegun`, node `IloFirstName`, speaker `Ilo` |
| Radio through the run | 190 to 1030 | | `PassageCalls` (below) |
| BrynFallsBehind (the far end) | 1080, across the road | about (0, 1, 1032) | crossing, flag `end.farEnd`, **report `FarEnd`**, node `BrynFallsBehind`, speaker `Sela`; its last line sets `end.passageRun` |
| The turn | | | from `end.passageRun`, once the talk closes: the party held 4 s while the convoy closes, then the pair out becomes Taren and Sela (D286) and the zone reloads at `Turn` (`end.turnedBack`) |
| Turn | 1060, 0 | about (0, 1, 1012), facing south | spawn `Turn`: the Back section's checkpoint (the reload saves there) |
| The tug | 760, 0, bow along the road | about (11.6, 1, 713) | drawn from `end.passageRun`; solid at the plane until her line is on |
| `Passage_Tug` | box on s 752, 60 x 8 x 44, turned to the road | | three `StillwaterTowDrone` (stand-in `TowDrone`), spawned at s 742 side -10, s 738 side 0, s 742 side 10, each facing south (away from her, down the road); `requiredFlag` `end.passageRun`; `checkpointSpawn` `TugReach` |
| TugReach | 800, 0 | about (19, 1, 752), facing south | spawn: the fight's door (a fall in the fight retries here) |
| Take the tug's line | 768, 0 (her bow, 3 m clear) | about (13, 1, 721) | point `Take the tug's line`, flag `end.lineOn`, **report `TugLine`**, needs `clear.Passage_Tug`, saves at `Tow`; 23 m and more from where the drones wake |
| Tow | 785, 0 | about (16, 1, 738), facing north | spawn: the Tow section's checkpoint |
| BrynOnTheLine | | | after-talk on `end.lineOn`, delay 1.2 s, speaker `Sela`, heard `end.tugFetched` (its own line) |
| OrrinRendezvous (the far mouth) | 1288, across the narrows (32 x 8 x 6) | (0, 1, 1240) | crossing, flag `end.mouthReached`, **report `FarMouth`**, needs `end.tugFetched`, node `OrrinRendezvous`, speaker `Orrin`; its last line sets `end.rendezvous` |
| The cut | | | from `end.rendezvous`, once the talk closes: the party held 2.5 s, the plate SEVEN WEEKS ON (3.5 s on black), then `NeighbourhoodAfloat` at `Arrival` |
| The edges | half 30 (s 0 to 1218), 30 -> 14 (to 1288), 14 -> 44 (to 1320), 44 (to 1393) | | solid boxes at the plane along both edges (`Passage limit ...`), and across both ends |

## Run contract (wiring 6.4)

**Sections and checkpoints.** A defeat is the defeat screen's Retry at the last autosave, with every flag kept.

| Section | From | To | Checkpoint (where a fall retries) | What can fall |
|---|---|---|---|---|
| Run | `Arrival` | the far end (`end.passageRun`) | `Arrival` (entering saves; the crossings save there too) | nothing fights in the run (wiring 5); a scrape on the edge is a scrape |
| Back | `Turn` (the turn's reload saves there) | the tug's fight | `Turn`, then `TugReach` once the fight is entered (its door saves there) | the tug's fight |
| Tow | `Tow` (taking the line saves there) | the far mouth (`end.rendezvous`) | `Tow`: loaded with the line on, the tug starts on the line behind the lead | nothing fights; a scrape |

**The convoy's flow** (`ConvoyFlow`, one root `The convoy, running`): every group of hulls is its own root with its place along the line (`s`), its side, its top and its pace. It moves north along the line (following the bends, squeezing through the narrows by the road's width there), comes round from s 1480 to s -100 (out of every frame), and from `end.passageRun` every group under the road's middle closes to the lantern line at its own edge within 8 s, leaving the middle 44 m (28 m in the narrows) empty; a zone opened with the flag set opens with them closed. It does nothing while the game is paused or **while a conversation holds the controls** (`GameInput.Blocked`), and never touches a hero: drawn only, three metres and more under the plane.

**What else flows** (`S/World/Alongside.cs`, held through a conversation like the convoy): the young (seven, the Cantor's segments small in their cold glow) keep station to starboard of the lead under the plane through the run, and from `end.passageRun` go on ahead to the mouth; Mother of Doors keeps station far off to starboard, deep and ahead (in the top of every frame of the run), and from `end.passageRun` goes on out of the mouth; from `end.passageRun` Idra's cutter (she is resting: D286) flies the convoy's west flank from the far end on toward the narrows at the convoy's pace, under the plane.

**The turn (D286)**: `S/World/CutAway.cs` on `end.passageRun`. Once `BrynFallsBehind` closes the party is held four seconds while the convoy closes to the lanterns; then the pair out becomes Taren and Sela, whoever was out (a lead who is one of them keeps the lead, otherwise Sela leads), `end.turnedBack` is set and the zone reloads at `Turn`, which saves. Idra is seen holding the flank from then on. The choice of pair returns after the rendezvous (the neighbourhood is a safe zone: the Party page is open there).

**The tow** (`S/World/TugTow.cs` on the tug): while she waits she is solid at the plane (cover: a harpoon line breaks on her). From `end.lineOn` her body stops being solid and she follows the lead on an 11 m line, a little to port of his wake, turning to face him; the line is drawn from the lead's stern to her bow. She slows nobody. She holds where she is while a conversation holds the controls, and a zone loaded with the line on starts her on it behind the lead. While her fight is on, the panel reads "Clear the tow-drones off her (n/3)" (`ZoneLine`), and each drone cleared is the lead's `tugdrones`, in turn.

**The cut** (`CutAway` on `end.rendezvous`): once `OrrinRendezvous` closes, the party is held two and a half seconds (the convoy goes on through the mouth), the screen goes to the plate SEVEN WEEKS ON (`S/World/TimePlate.cs`, drawn by `S/UI/ScreenFade.cs` over everything, through the load) and `NeighbourhoodAfloat` loads at `Arrival`. A save made after the talk loads into the cut again.

**Radio and barks** (`S/World/PassageCalls.cs`, each said once a load, at least 4.2 s apart, held through a conversation, by its place along the line; a pool said in turn is said by its own place in the pool, so a reload keeps the count in step):

| s | Who | Key (line) |
|---|---|---|
| 50 | the lead | `passage` |
| 120 | the lead | `youngchoir` |
| 190 | Pell | `hail.count` 1 ("Two short, one long. Here. This way.") at bend one |
| 245 | Ilo | `hail.names` 1 (Oda's hauler) |
| 300 | Dorrow | `hail.step` 1 |
| 360 | Ilo | `hail.names` 2 (the pear stall) |
| 415 | Mira | `hail.manifest` 1 |
| 470 | Pell | `hail.count` 2 at bend two |
| 530 | Orrin | `hail.board` 1 ("half the hulls are through") |
| 585 | Ilo | `hail.names` 3 (Sorrel outpost) |
| 640 | Hal | `hail.cradle` |
| 695 | Neve | `hail.rear` 1 |
| 750 | Ilo | `hail.names` 4 (Lastlight) |
| 790 | Pell | `hail.count` 3 at bend three |
| 850 | Orrin | `hail.board` 2 ("three in four") |
| 905 | Dorrow | `hail.step` 2 |
| 960 | Mira | `hail.manifest` 2 |
| 1015 | Bryn | `hail.lead` (before her tug fails) |
| back, 1040 | the lead | `wake` (flying back up the lane, from `end.turnedBack`) |
| tow, 820 | Ilo | `hail.names` 5 (Rootmarket) |
| tow, 900 | Neve | `hail.rear` 2 ("Last hull but one") |
| tow, 980 | Ilo | `hail.names` 6 (the apprentices) |
| tow, 1150 | Orrin | `hail.board` 3 ("one to come") |

## Tug fight contract (`Passage_Tug`; wiring 5)

*What the zone gives the fight; the first combat engineer writes what is built in the second column. Until `StillwaterTowDrone` is defined the fight stands in as `TowDrone` (`MOORING_FOE_STANDIN`).*

| Thing | What the zone gives it | As built (combat engineer) |
|---|---|---|
| The box | at the plane round the tug, on s 752 (her middle 8 m inside it), 60 x 8 x 44 turned to the road: the whole road's width, its north face open (the party comes back from the far end) | |
| The drones | three spawners of one each, at s 742 side -10, s 738 side 0, s 742 side 10 (16 to 22 m south of her middle), `faceAsPlaced`, each facing south down the road, away from her: a caught ship is hauled off the tug, down the road | |
| Cover | the tug, solid at the plane while her line is off (a box 5.6 x 4 x 10.6 on her hull; drawn with her fenders and boom she is 6.9 x 2.8 x 14) | |
| The line | `Take the tug's line` at her bow, 23 m and more from every drone's wake point, offered from `clear.Passage_Tug` | |
| The camera's side | nothing over the plane anywhere in the box or south of it but the drones and the tug | |
| The count | the panel's "Clear the tow-drones off her (n/3)" counts the fight's bodies down (`TugTow`) | |

## For route writers

- **Starts**: `Arrival` (0, 1, 0) facing north, from the anchor chamber's launch; `Turn` (about (0, 1, 1012)) facing south, after the turn; `TugReach` (about (19, 1, 752)) facing south, a fall in the tug's fight; `Tow` (about (16, 1, 738)) facing north, a fall after the line.
- **The run**: fly north along the road's middle (the line through (0, 170), (-36, 450), (20, 760), (0, 1050); `Passage.On(s, 0)` gives every point). `IloFirstName` plays crossing z 130 (four lines; `end.namesBegun`); radio says nothing that needs an answer. At the far end (s 1080, about z 1032) `BrynFallsBehind` (nine lines) sets `end.passageRun`; wait for the turn's reload (`ZONE_ENTER ReturningPassage` at `Turn`), `until` it.
- **Back**: fly south along the road's middle to the tug; the fight wakes entering its box at s 774 or less (about z 735); `Passage_Tug` is three tow-drones south of her: `evadeTelegraphs` (roll a harpoon line), shoot them (Pulse) until `clear.Passage_Tug`.
- **The line**: fly to her bow (about (13, 1, 721); `Passage.LineAt`) and press South `until` `end.lineOn`; wait for `BrynOnTheLine` (four lines, `end.tugFetched`).
- **The tow**: fly north along the road to the far mouth (s 1288, z 1240); `OrrinRendezvous` (four lines) sets `end.rendezvous`; the cut loads `NeighbourhoodAfloat` at `Arrival` about 7 s after the talk closes.
- **Fights**: `Passage_Tug` only.
- **Flags it ends with**: `end.namesBegun`, `end.farEnd`, `end.passageRun`, `end.turnedBack`, `clear.Passage_Tug`, `end.lineOn`, `end.tugFetched`, `end.mouthReached`, `end.rendezvous`.
- **How long**: about five minutes: the run 1,080 m at about 12 m/s (90 s) and two talks (13 lines), the turn (6 s), 300 m back (25 s), the fight (30 to 60 s), the line and a talk, 530 m of tow (45 s), a talk, the cut (7 s).

## Shared pieces

- **The tug Welcome Back** is `LastMooringBuilder.TugKit.WelcomeBack(kit, middle, yaw, parent)`: HomeBuilder.Nacre's `TugRaft` hull lifted out (the cutter in the Nacre approach's green, fenders on both flanks, the tow boom, her beacon), drawn only, and `TugKit.Paint` for a piece in a colour of its own. Nacre Grave, the Broken Ring and Many Hands' arrivals still build their own; each can call this at its owner's next edit (not changed here: their files).
- **The convoy's hulls** are Last Mooring's `ConvoyKit` (`Hull`, `Body`, `CopyOut`, `ManyHands`, `Districts`) as it stands; nothing of it is copied.
- **The plate** (SEVEN WEEKS ON) is `S/World/TimePlate.cs`, drawn by a small append to `S/UI/ScreenFade.cs` (the fade's canvas outlives the load it covers).

## For the lead to decide (a decision number wanted)

- **No current carries the ships** (wiring 6.4 leaves it open: "if ships are carried by it"). Carried, the run back up the road to the tug would crawl at .7 of a ship's speed with its Thrust draining into the tug's fight, the heroes' own `current` barks ("Wrong way. It's bleeding us. Cross to the bright lane.") would tell the player to leave the road Orrin has just emptied for them, and a fight in the current would be carried down the road (D282). The flow is drawn instead: everything else in the passage goes north, and ships fly free.
- **The turn is a reload** at `Turn` (D286's "the pair out becomes Sela and Taren"): a party's pair can't be changed under a running zone without rebuilding its bodies, and the reload's own save is the Back section's checkpoint.

## Evidence

Built in the editor (`LastMooringBuilder.BuildZones -mooring-zones ReturningPassage`, then `ZoneLook.Apply`) and looked at through the game's flight camera (`LookDev.Render -look-far 1000`, 27 views: `LastMooringBuilder.Passage.Views`), as met and (`-passage-pose turned`) as it stands after the turn.

| Pass | What was wrong | What changed |
|---|---|---|
| `passage-01` | Not a dark tube: the opposite. The bore's floor was bright magenta with its veins stretched into silk stripes, the ribs broad textured bands like stripes on a carpet, the deep streams fine vertical lines and rings like a hologram's grid over everything, the road's chevrons wide gold smears, the running lights metre-wide white blocks. The convoy ran at ±14, so the road's middle was empty, and three metres under the plane, so what was in frame sat under the ships like a deck they had landed on; the Compact's cutters beside the heroes read as enemy craft | Floor dark plum with veins both ways; ribs thin hoops of light; streams soft and faint; small chevrons; small coloured lights; the convoy in two lanes at ±10 laid so no footprint meets another, four to six metres deeper; the cutters only outside the road |
| `passage-02` | The bore reads as a living interior and the wide views as a river of the convoy between lantern lines, ribs curving under it. Still: haulers and docking pods filled every frame they were in; Nacre's cutters in their stock dark paint read as fighters three times a hero's size; Mother of Doors at 96 m down and the convoy's scale read as one more thing among the hulls; from the road's middle no lantern was ever in frame (the edges are 30 m off, the frame 40 m across); the Crawl's treads (23 m tall) reached down through the deep hulls | Every hull smaller; Nacre's cutters in the Nacre approach's green; the treads with the deep hulls; Mother 128 m down at three times the size, in her glow; the road's line is the Wick-folk's lanterns in threes down its middle (the chevrons gone) |
| `passage-turned-03` | (after the turn, `-passage-pose turned`) The convoy closed to the lanterns, the middle of the road empty but for its lanterns and the ribs under it: Orrin's "leave the middle empty" reads. The tug is the Nacre-green cutter with her fenders and boom (as Nacre Grave and the Ring have her); her drones spawn in play, so no still has the fight | |
| `passage-03` | The convoy fills the frames as a river of places at last (the Halo's districts with lit windows, rafts, seed barges, the tread hall's "9", haulers, the ring) with the road's lanterns between. Every group's glow quad read from above as a pale square round it; Mother of Doors, 128 m down at full colour, still read as near; the mouth's lip as a great red wing | No glow quads over groups; Mother 148 m down over a deeper floor, painted into the haze; the lip slimmer and darker; docking pods outside the road |
| `passage-04` | Nacre's cutters (now green) clustered beside the heroes at the arrival, still read as an escort of fighters | Nacre's cutters smaller and outside the road |
| `passage-05`, `passage-06` | **The stills judged are `passage-06`'s** (05 with the deep lanes at ±34, then moved out) (and `passage-turned-03` for the zone after the turn). Every frame of the run has places the player knows under the ships (Lastlight's longhouses and rib arches, the seed barges, the raft bays, the Halo's districts with lit windows, the tread hall's "9", the ring, haulers), the road's lanterns in threes between them, the young swimming alongside to starboard, the ribs of light curving under it all; the wide views read as a river of the convoy between two lantern lines; the narrows close in and the mouth opens on the new sky's stars | |

What is weak, plainly:

- **Mother of Doors reads only faintly**: a dark segmented shape deep under the convoy at a frame's edge. Painted into the haze and 148 m down she no longer reads as one more thing among the hulls, but from this camera depth is hard to sell, and she does not yet read as huge and far off.
- **A still has no flow.** The convoy, the young, Mother and the streams move only in play; nothing here has been seen moving (no player window).
- **Teo's carriers (skiffs) read as small fighters** among the hulls; the convoy's only craft at a hero's own size.
- **Lastlight's rib arches read as white bones** from above (the piece as taken in; Last Mooring has the same).
- **The tug's fight is in no still** (its drones spawn in play); the tug herself is the Nacre-green cutter of chapter 9, not a squat working hull.
- **The middle of the road after the turn is sparse** by design (Orrin empties it): the road's lanterns, the ribs and the convoy at the edges.
- No ambience bed of its own (guide 14: the lead's).
