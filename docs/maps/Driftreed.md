# Driftreed - the raft village on the mire

2026-11-12. Builder: `WendmireBuilder.Driftreed.cs` on `ZoneKit` and `GroundKit` (world engineer A, chapter 4). Chapter brief: [ch04-breathing-room.md](../chapters/ch04-breathing-room.md). Script wiring: [ch04-manifest.md](../writing/ch04-manifest.md). **MAP_EYE_TEST: blockout written before construction (this page); editor stills and the in-player pass are under Evidence.**

## What the place is for

Woven-reed rafts on Wendmire's mire, lashed round a market on stilts, lived on by the Ilu for longer than anyone remembers. The weavers who make the air-mats the convoy needs live here, and so does the grief that keeps them home: lights in the mire have taken three people this season, Ona Sedge's apprentice Pim the latest. The party lands on the pilots' stage, holds a stern line it is not allowed to tie, hears Ona's terms at the weaving hall, and goes into the mire with Maru once the tide turns.

Users: the pilots (Maru Wend and two who coil line on the stage), the weavers (Ona and three at the racks), Dov Hale at his smokehouse, Quill at his pump raft, Iri and the children on the school raft, somebody always fishing, market-goers.

## Rules of construction

- The fixed camera stands south-west of the party and never turns (D119, D212). The tall things are north and east: the weaving hall and its drying racks at the back are the landmark; the mangroves stand beyond the hall and past the mire gate. West and south (the camera's side) are open water with low reed beds.
- **The water between rafts is the floor's relief.** Each raft is a rounded rectangle of floor in water with a knee-high rim of bundled reed along every edge (`ReedHeight`), steep both sides: the hero's step is .35 m, so the rim stops a walk with something drawn, and no `Boundary` box stands anywhere. A rim is open only where a rope walk lands (`Openings`); each walk's deck has a drawn rail on a `BlockAlong`.
- **The tide turns once** (`wend.tideTurned`, set by `MaruTide`): two rope walks swing away and two swing across (`S/World/TideSwing.cs`). Each swinging walk's ends are seals (`Membrane`: a rope strung across the gap, a box a body stops at, a carving obstacle) that stand while the walk is away. The bake lays floor under every walk as if it were across, and the seals' obstacles cut what isn't. Every place but the mire gate is reachable before the turn and every place after it.
- Townspeople are Ilu in five coats (Maru's, Ona's, Dov's, Quill's and Pim's, each body tinted apart from the named person it borrows), never two alike side by side, standing in static boxes off every way between the named, or walking an authored loop (`WalkLoop`).
- Places are named constants in the builder (`StageAt`, `OnaAt`, `DovAt`, ...): the scatter's keep-off, the bake's must-reach list, LookDev's views, this page and the route generator all read the same numbers. `OnTheFloor` refuses anything placed on a rim or in the water.

## Plan

North up, metres. Rafts (centre, half-size x by z):

```text
                         mangroves                         mangroves
          reed store  [ WEAVING HALL  (0,86) 20 x 11 ]                 [GATE RAFT (46,96)]
                       drying racks along the back           .-S4-.    MIRE GATE (47,100)
                       Ona at the door (-0.5,82.2)          S3          |  (arrives with the tide)
                                 |F2                     .'            |
  [PUMP RAFT (-40,44)]  -F3-  [ STILT MARKET (0,46) 16 x 12 ] -S1-  [SCHOOL RAFT (40,58)]
   Quill (-34.5,46)            market post (-6,39.5)         (leaves)   Iri and the children
   press, mending frame        DOV'S SMOKEHOUSE east lobe (25,41)
        .                               |F1
          S2 (leaves)                   |
             '.             [ PILOTS' STAGE (0,12) 15 x 6 ]   the stern-line raft adrift (31,4) -> lying easy (22,9.5)
    open water, reed beds    arrival (0,8); pad (-8,11); Maru (9,12.5); the line (13.5,10.6)
                                  (camera's side: low)
```

Rope walks: F1 stage-market, F2 market-hall, F3 market-pump raft (fixed); S1 market-school and S2 stage-pump raft (swing away at the turn); S3 hall-school and S4 school-gate raft (swing across).

## What happens where

| Place | What | Flags, ids |
|---|---|---|
| Pilots' stage | Arrival (spawn `Arrival`); `Launch — The Spore Veil` (pad); `MaruArrival` on a crossing between the pad and the line; `Hold the stern line` (`MooringHaul`, three holds, Maru's `line` barks); Maru's lamp on the stage mast, long, long, short until `wend.decided` | `wend.arrived` (on landing), `wend.arrivalSeen`, `wend.lineHeld`; Maru and Ona answer with `MaruLineHold`, `OnaLineHold` until it is held |
| Pilots' stage | Maru: `MaruFirst`, `MaruRepeat`; `MaruTide` (beat) turns the tide; away in the mire from `wend.tideTurned` until `wend.rotbloomDead`; then `MaruOnaWaits`, `MaruComes`, `MaruBeaconsOffer`/`Return`, `MaruPost` | `wend.tideTurned`, `maru.comesHeard`, `maru.beaconsReported` |
| Rope walk F1 | Abri's call on the walk back to the stage once `wend.decided` (`AbriManyHandsCall`, crossing "Keeper Abri calls about the name", standing on the walk's deck) | `chapter4.complete` |
| Stilt market | stalls, the market post: `Read the Compact notice` (`SelaWendNotice`) | `wend.noticeRead` |
| Smokehouse | Dov: `DovFirst`, `DovRepeat`, `DovPost`; beats `DovOrderOffer`/`Return`, `DovBellOffer`/`Return`; his counter `Driftreed_Dov` | `dov.orderFilled`, `dov.bellReported` |
| Pump raft | Quill: `QuillFirst`, `QuillRepeat`, `QuillPost`; beats `QuillPumpOffer`/`Return`; the press (`Bench`), the mending frame (`RepairBay`, saves at spawn `Pump`); his counter `Driftreed_Quill` | `quill.markRead` |
| Weaving hall | Ona: `OnaTerms` (first), `OnaRepeat`, beats `OnaPimBack`, `OnaDecides`, final `OnaAfter`; the reed store (eleven poles locked, open from `wend.decided`); Pim at the bench from `wend.rotbloomDead` (`PimHome`) | `wend.onaHeard`, `wend.decided` |
| School raft | `Watch the school raft` (`SelaSchoolRaft`); the bell frame with no bell | `wend.schoolSeen` |
| Gate raft | `Mire gate — The Drowned Waystation`, locked on `wend.tideTurned`; spawn `Mire` (back from the mire) | |

## Sightlines and what is in frame

- The camera sees about 7 to 9 m north of the party and 5 south: every frame on a raft is raft floor, its rim and water beyond. Water is never left bare: lily rafts, reed beds, moored canoes, and (`TideLife`) five lines strung raft to raft over the gaps (stage-market east at x 12, market-hall at x -10 and x 11, market-pump at z 50.5, hall-gate at z 93) hung with lanterns, drying nets, floats and fish; fourteen skiffs moored along the rims on lines to rim posts, some loaded, some with a stern lantern; and 220 drifts of floating weed lying the way the tide runs. Each of the ten lanterns over the water keeps the town's count and lies on the water as a streak of its light toward the camera, blinking with it.
- The floor is mats laid edge to edge (16 m to a repeat): plain half-metre checks, twill, herringbone and long lashed stalks, each mat its own size, age and bound edge.
- People with somewhere to be (`Errands`, seven `WalkLoop`s, each checked clear of anything solid as built): a weaver carrying a mat round the hall's east side; a weaver walking the hall's racks; a weaver bringing reed along the hall's west side; a weaver turning the mats laid out to dry in the market's south-east; the smokehouse's hand with a creel along the lobe's south side; a pilot pacing the stage's west end with a coil; a child running the school raft's rim. Nine more stand at their work and three children face Iri.
- The hall stands at the back of the north raft with its door to the market; its racks (7 to 9 m) stand behind it, north of every walked line, so nothing tall stands between the camera and the party.
- Every floor still is held to the ground kit's measure (`python tools/look_density.py`, at or under .22).

## Coordinates for routes

Plan x, z (the floor is at y 0 to .05; a route walks on it). From the builder's constants, logged by the build as `WENDMIRE_COORDS Driftreed`.

| Place | Plan (x, z) | Id / flag |
|---|---|---|
| Arrival (pilots' stage) | (0, 8.5) | spawn `Arrival` |
| Landing pad | (-8, 11) | `Launch — The Spore Veil` -> the Veil's spawn `Driftreed` |
| Maru's call (crossing, across the stage) | (6, 12), 13 m deep | `MaruArrival`, `wend.arrivalSeen` |
| Maru on the stage | (9.5, 13.5) | `Maru` (away from `wend.tideTurned` until `wend.rotbloomDead`) |
| The stern line | (13.4, 10.6) | `Hold the stern line` x3, `wend.lineHeld` |
| Stage walk (F1), Abri's call on it | (0, 18) -> (0, 34); call at (0, 26) | `AbriManyHandsCall`, `chapter4.complete` (needs `wend.decided`) |
| Market middle | (0, 46) | |
| The Compact notice | (-6, 39.5) | `Read the Compact notice`, `wend.noticeRead` |
| Dov at the smokehouse | (19.5, 45) | `Dov`, counter `Driftreed_Dov` |
| Pump walk (F3) | (-16, 46) -> (-30.2, 46) | |
| Quill on the pump raft | (-34.5, 46) | `Quill`, counter `Driftreed_Quill` |
| The pump-and-press | (-43.5, 49.5); press from (-42.2, 48.2) | `Fabricate — Quill's Pump-and-Press` |
| The mending frame | (-42.5, 39.6) | `Quill's mending frame — repair and save`, saves at spawn `Pump` (-36, 41.5) |
| Hall walk (F2) | (0, 58) -> (0, 75) | |
| Ona at the hall door | (-0.5, 82.2) | `Ona` |
| Reed store | (-15.5, 84.5) | poles out from `wend.decided` |
| Pim's bench | (10.5, 82) | `Pim` (from `wend.rotbloomDead`) |
| School walk (S1, leaves) | (16, 54) -> (29.3, 55) | |
| School raft | (36.5, 55.5) | `Watch the school raft`, `wend.schoolSeen` |
| Stage-pump walk (S2, leaves) | (-10, 18) -> (-33.8, 35.8) | |
| Hall-school walk (S3, arrives) | (20, 80) -> (37.45, 66.6) | |
| Gate walk (S4, arrives) | (45.3, 65.2) -> (45.3, 88.6) | |
| Back from the mire | (45.5, 92.2) | spawn `Mire` |
| **The mire gate** | (48.2, 99.4) | `Mire gate — The Drowned Waystation` -> its `Arrival`; locked on `wend.tideTurned` |
| The `MIRE GATE` floor sign | (45, 95.6), on the camera's side of the arch, 5 m from it | was (42.6, 92) beside the spawn, 9.3 m off: the arch read as an unlabelled way out (`QaStoryWorldTests.EveryChapterTwoExitIsSigned`, a sign within 7 m), November 14 |
| Errand loops (people walking, keep routes off them) | weaver with a mat (7, 85.6) (15.5, 86) (16.5, 88.4) (8, 88.2); weaver at the racks (11.4, 89.4) <-> (15, 89.4); reed carrier (-14.4, 77.3) <-> (-6.6, 77.2); mat turner (4.6, 38.2) (10.8, 38.4) (10.6, 43.1) (4.8, 42.9); smokehouse hand (21.4, 37) <-> (29.2, 37.2); pilot (-10.8, 15.8) <-> (-12.2, 11.8); child (34.2, 57.4) (35.8, 62.4) (38.6, 61.8) (36.4, 58.2) | `Errands` |

A route from the arrival to the mire gate after the tide: (0, 8.5) -> (0, 18) -> (0, 34) -> (0, 58) -> (0, 75) -> (1, 78.5) -> (12, 80.5) -> (20, 80) -> (37.45, 66.6) -> (40, 63) -> (45.3, 65.2) -> (45.3, 88.6) -> (45.5, 92.2) -> (48.2, 99.4). Before it: to the school by (0, 46) -> (12, 52) -> (16, 54) -> (29.3, 55) -> (36.5, 55.5).

## Evidence

Built six times in the editor (`WendmireBuilder.BuildZones -wendmire-zones Driftreed`, then `ZoneLook.Apply`), each followed by stills through the ground camera (`LookDev.Render`, 20 views: each raft and walk as first walked, two wide), measured with `python tools/look_density.py` and looked at.

| Pass | What was wrong | What changed |
|---|---|---|
| first builds | The bake refused the gate raft: a shut tide seal's carving obstacle cut the editor's own path check at the gate walk's foot (found by a diagnostic that logs where the route stops and what stands there) | Every seal is opened for the bake and put back; the bake's stand-in decks run two metres into each raft |
| `wend-02` | The floor read as wide planking under sand dunes (bundles a metre wide; the kit's sand layer on the ways); the market measured .24; the station's planters and steel barrels stood about | The floor is a mat woven in half-metre squares, each its stalks the other way; the ways are trodden mat (a layer of Driftreed's own, `ground-wend-reedpath`), not sand; raft goods are woven baskets, earthen jars, standing reed bundles |
| `wend-04` | The market's crossroads still a plain plaza (.26) | Mats laid out to dry on the boards, two more people, fish traps and coils, and pennant lines with lanterns strung mast to mast over the square: their shadows cross the floor |
| tests | `WendmireWorldTests` found the mire gate's prompt answering at the world's origin (a built mesh stands at the origin, and the way was put on it) | The way stands on an object at the arch (`wend-14`); Ona moved to the hall's door, where the hall's front is |
| `wend-05` | Every still at or under .11 (market .11, the worst); the water read as camouflage: the mire's bed and the shade moving over it showed through translucent water | The bed is dark peat and the water nearly opaque past a metre (`wend-06`) |

| shared tests (`wend-base-a`) and review of `wend-14` | `NoInvisibleWallStandsInTheOpen`: the racks' boxes hung under a bare holder, so they read as invisible walls across open deck. `TheBodyNeverSticksAtAWallFootOrAProp` and `TheCompanionKeepsUpOnFoot`: with every seal opened, an arriving walk's ends opened onto water where the walk wasn't, and a body walked out onto the away walk or off it into the mire. `EveryPromptCanBeReachedOnFoot` and `NoRawIdOrWrongWord...`: Abri's call stood on the mire's bed under the walk, two metres down, and named her "Abri". The review: plain water between the rafts, a floor that read as square tile, and most people standing still. The build's own errand check found the pilot's loop starting inside a lamp's collider, so he had never walked | The racks' boxes hang under the drawn frames. `TideSwing` keeps an open end only where the walk is: anything that opens one brings the walk across. Abri's call stands on the walk's deck and reads "Keeper Abri calls about the name". `TideLife`: lines raft to raft hung with lanterns, nets, floats and fish; fourteen moored skiffs on lines to rim posts; weed lying in drifts the tide's way; each lantern's light lies on the water as a streak that keeps the count. The floor is mats of four weaves, sizes and ages with bound edges, sixteen metres to a repeat. Seven people walk errands (`Errands`), each loop checked clear of anything solid as built. The leaf dapple over the town went (no canopy throws it) for a wide, faint cloud shadow (`wend-15` to `wend-17`) |

What is weak, plainly:

- **The water's own pattern** is the project's water look (the same blotched ripple as the Braided River's), and on the mire's dark tea it still reads a little like camouflage between the things now lying on it.
- **The weed drifts are flat rosettes**; from the fixed camera some read as brown lily pads.

- **Maru's lamp can't be in frame from the hall door.** The fixed camera looks north-east and the stage is south of the hall; the lamp is built and blinks long, long, short at the stage, and Sela says what she sees, but the player never sees it from where Ona speaks.
- **The tide's swing has not been watched.** `TideSwing` turns each walk over seven seconds; the stills show the walks before the tide only.
- **Nine townspeople still stand at their work**, and the children and Iri are scaled bodies of the same rig. Nobody poles a boat between the rafts: the skiffs are moored.
- **Not walked in a player**: the four-minute circuit, the stern line's surges, the camera at each landing.
