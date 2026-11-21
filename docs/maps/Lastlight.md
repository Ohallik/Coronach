# Lastlight - the herders' town inside a frozen road

2026-11-12. Builder: `HoarfellBuilder.Lastlight.cs` on `ZoneKit` and `GroundKit` (world engineer A, chapter 6). Chapter brief: [ch06-the-long-sleep.md](../chapters/ch06-the-long-sleep.md). Script wiring: [ch06-manifest.md](../writing/ch06-manifest.md). **MAP_EYE_TEST: blockout written before construction (this page); editor stills and what is still weak are under Evidence.**

## What the place is for

An adult Choir froze on Hoarfell in the last Severance, held still by the old rulers until it died, and the Kept built their town inside its ribcage: the ribs arch over the back of the town, vents under the ice keep the rib-halls warm, and the lamp shrine stands at the skull end in the scar of the clamps that held it. The herders keep rime-beasts in pens on the west side and their dead in the ice. Old Tern, who knows how a young road sleeps, sits under the fourth rib and won't tell it while the Muster's thaw is sweating her roof. Idra Sorn is already here, arguing with Hask about the pens. The Reach gate on the west opens on the White Reach and the dig.

Users: the Kept (Tern, Hask, Iru, herders, carvers, lamp-keepers, children), their beasts, Idra, the party.

## Rules of construction

- The fixed camera stands south-south-west of the party and never turns (D119, D212). **The tall things are north and east**: the ribs stand along the back of the town, each a generated `RibArch` 12 m tall (about 16 m across) spanning north to south, its front foot at z 60 and its back foot at z 76, so every frame at the back of the town has a rib's lower curve rising out of the top of it; the skull is the east end. South and west, the camera's side, are open snow with low drifts.
- **Warm ground and cold ground read apart** (brief): snow in blue shadow over most of the floor; round each vent bare dark stone, wet and steaming, with a ring of slush; trodden paths grey-brown. The vents' steam and lamps' pools are warm against the snow's blue.
- **The edge of the town is drawn**: snow drifts rising at the south and west (low, the camera's side), the spine's snow slope to the north, the skull and an ice wall to the east. No invisible wall stands in the open.
- **The climb to the high lamps** is a raised way of packed snow over bone up the rib-halls on the east (part of the ground's own height, so it is walked and baked like the rest), with a knee-high rim along each side: three platforms, each a lamp.
- People: the Kept in four coats (`KeptHerder`, and Tern's, Hask's and Iru's coats, each tinted apart from the named person it borrows), never two alike side by side; standing at their work in static boxes, or walking an errand (`WalkLoop`) carrying ladders, hides, lamps and buckets. Nobody stands on a way, a sign, or between the camera and anyone with a name (checked as built).
- Places are named constants in the builder (`LastArrival`, `TernAt`, `HaskAt`, ...): the scatter's keep-off, the bake's must-reach list, LookDev's views, this page and a route generator all read the same numbers. `OnTheSnow` refuses anything placed on the climb's rim or in a vent.

## Plan

North up, metres.

```text
   z 76  ---- back feet of the ribs; the spine's snow slope beyond ----------------------------------------------- the skull
   z 60  rib10 rib9   rib8   rib7   rib6   rib5   rib4   rib3   rib2   rib1      (front feet; ribs numbered from the skull)
          -58   -46    -34    -22    -10     2     14     26     38     50        climb: L1 (47.5,53.8) L2 (52,63) L3 (60,68)
               store  [hall]  racks [hall] [TERN'S HALL (8,68)] [hall]           the skull (64,52), east end
                                         Tern seated (8,60.5)  FOURTH RIB tally (12,58.4)
  REACH GATE        CARVING YARD (-12,46)                                    LAMP SHRINE: Iru (44,44), low lamp (48.5,48),
  (-63.5,42)        bone map, Idra (-12,48)                                   the four cut seats (55.4,44.2)
  spawn Reach (-57,42)  bench (-18.5,46)              VENT BATH-HOUSE (28,46), cradle (25.6,37.6), spawn Bath (23.4,34.6)
       BEAST PENS (-48..-29, 14..34)        vents (-4,21) (7,28) (16,17) (23,30.5) (36,29); one in the yard (-30,48)
       Hask (-25,26.5), Idra arguing (-24,24.2)
                                  arrival crossing across z 12
                         pad (-7,7)   ARRIVAL (0,4)
   z -8  ---- low drift (camera's side) ----
```

## What happens where

| Place | What | Flags, ids |
|---|---|---|
| Landing | Arrival (spawn `Arrival`); `Launch — The Aurora Shear` (pad) -> the Shear's spawn `Lastlight`; `SelaLastlightArrival` on a crossing across the way north of the landing; herders carrying ladders | `hoar.arrived` (on landing, by the crossing's own arrival flag), `hoar.ribsSeen` |
| Tern's hall, under the fourth rib | Tern, seated in three hides by her door: `TernFirst`, `TernRepeat`, beat `TernLongSleep` (`hoar.marshalAsleep`), final `TernAfter` (`hoar.sleepLearned`); `Look at the cuts in the rib` (`SelaRibTally`) at the rib's foot, open from arrival | `hoar.ternHeard`, `hoar.sleepLearned`, `hoar.tallySeen` |
| Beast pens | Hask: `HaskFirst` (choice: trade or leave), `HaskRepeat` (his counter), beats `HaskStraysOffer`/`Waiting`/`Return`, `HaskOwes`, post `HaskGoes` (`hoar.sleepLearned`); his counter is the zone's own (`ShopInventory/Lastlight`); Idra arguing with him until the pens talk; `IdraPens` on a crossing by the pen gate, open once Tern has spoken | `hoar.pensSeen`; starts `CutterDown`; `StrayBeasts` |
| Carving yard | Idra (from `hoar.pensSeen` until `hoar.reachEntered`, and never once she has joined): `IdraPlan`, `IdraRepeat`; the herders' bone map; the bench (`Fabricate — The Carving Yard`); `Ask Idra about the scratches` (`IdraTally`) once the Marshal sleeps | `hoar.planTaken`, `idra.tallyShown` |
| Vent bath-house | The cradle (`Vent bath-house — repair and save`, saves at spawn `Bath`); `Sit by the vent` (`SelaVentBath`) once the Marshal sleeps | `hoar.bathSeen` |
| Lamp shrine | Iru: `IruFirst`, `IruRepeat`, beats `IruLampsOffer`/`Return`, post `IruGoes` (`hoar.sleepLearned`), final `IruRepeat` once she has spoken up; the low lamp; `Look at the cut seats` (`SelaClampScars`) once Tern has told the long sleep; the climb: `Light the high lamp` at L1, L2, L3, offered while `ShrineLamps` is at step 0 | `iru.lampsLit`, `hoar.scarsSeen`, `hoar.skullLamp1..3` (report `SkullLamp`) |
| Tern's hall | Iru standing by Tern for `IruSpeaksUp` once the lamps are lit and the long sleep told, until she has spoken | `iru.spokeUp` |
| Reach gate | `Reach gate — The Frozen Host`, locked on `hoar.planTaken` ("... hear Idra Sorn's plan at the carving yard"); spawn `Reach` (back from the Reach) | |

## For the Frozen Host (contract with world engineer B)

What Lastlight needs from the Frozen Host, and what it gives (the one who needs something writes it here; the other writes the coordinates back):

- **Ways.** Lastlight's `Reach gate — The Frozen Host` arrives at the Frozen Host's spawn **`Arrival`**. The Frozen Host's way back must arrive at Lastlight's spawn **`Reach`** (plan (-57, 42)).
- **Flags the Frozen Host sets that Lastlight reads.** `hoar.reachEntered` (`SelaReachGate`'s crossing: Idra leaves the carving yard for the Reach), `hoar.marshalAsleep` (`IdraMarshalSleeps`), `hoar.idraJoined` (`IdraJoins`).
- **The story's arrows** (`LongSleep` in `HoarfellBuilder.Quests`): step 5 (`hoar.idraJoined`) points at the Frozen Host's point with **report `BottomGallery`**: give the `IdraJoins` crossing that report (`point.report="BottomGallery"` after `kit.Crossing`). Step 6 is **`Kill HostMarshal`** at the encounter **`HostMarshal`**: the couplings are the Marshal's break targets (brief), so the manifest's optional "Interact x3 Coupling" step is left out.
- **`StrayBeasts`**: three points on the Reach with **report `StrayBeast`**, offered while `StrayBeasts` is at step 0 (`kit.Discovery(..., "StrayBeast", null, "Sela", "StrayBeasts", 0)`), prompt `Walk the stray in`, situation `stray`.
- **The join** (`IdraJoins`, `<<joinParty Idra>>`): the conversation adds her and brings her out in the partner's place when it closes (`DialogueSystem` -> `PartyField.Join`). Nothing more is staged for it; `HoarfellWorldTests.IdraJoinsAsTheThirdHeroAndTheGameStillPlays` plays the node itself.

As built in the Frozen Host (world engineer B, November 12; `HoarfellBuilder.FrozenHost.cs`, coordinates in docs/maps/FrozenHost.md): spawn `Arrival` at (0, 2), the way back `Reach gate — Lastlight` under a rib at (0, -10.5) and, once `hoar.marshalAsleep` is set, `Ride the winch up — Lastlight` at (176, 228.5), both to Lastlight's `Reach`. `SelaReachGate` is a crossing at (0, 12) setting `hoar.reachEntered`; `IdraJoins` a crossing at (182, 218) setting `hoar.idraJoined` with `report="BottomGallery"`; `IdraMarshalSleeps` an `AfterTalk` on `bossdown.HostMarshal` whose script sets `hoar.marshalAsleep`. The strays: `Walk the stray in` at (-36, 63.3), (-33.5, 105.8), (36.5, 119.8), report `StrayBeast`, `StrayBeasts` step 0, situation `stray`, flags `hoar.stray1..3`; each beast walks off and is gone once walked in. Idra stands ahead of the party on the Reach and down the dig from load (a `Guide`, never someone to talk to) until `hoar.idraJoined`.

## Sightlines and what is in frame

- The camera sees about 7 to 9 m north of the party and 5 south. At the back of the town every frame has a rib's front foot and curve; in the middle, vents steaming, beasts, longhouse doors; on the east, the skull and the climb.
- The floor: snow painted with wind-cut ripples (sastrugi) and blue hollows; trodden grey-brown paths; bare wet stone round the vents; bone litter, hide scraps, tracks of feet and hooves (ground kit).
- People with somewhere to be: two herders carrying a ladder across the commons, a herder walking the pens' fence, a carver bringing bone to the yard, a bather carrying buckets to the bath-house, a lamp-keeper's helper on the shrine floor, a child running between the vents.
- Every floor still is held to the ground kit's measure (`python tools/look_density.py`, at or under .22).

## Coordinates for routes

Plan x, z (a route walks on the floor at its height). From the builder's constants, logged by the build as `HOARFELL_COORDS Lastlight`.

| Place | Plan (x, z) | Id / flag |
|---|---|---|
| Arrival | (0, 4) | spawn `Arrival` |
| Landing pad | (-7, 7) | `Launch — The Aurora Shear` -> the Shear's spawn `Lastlight` |
| Arrival crossing | across z 12, x -12 to 12 | `SelaLastlightArrival`, `hoar.ribsSeen` |
| Hask at the pens | (-25, 26.5) | `Hask`, counter `Lastlight` |
| Idra arguing at the pens (until the talk; she walks to the yard as it closes) | (-24, 24.2) | |
| Pens crossing | (-20, 25), 5 by 10 | `IdraPens`, `hoar.pensSeen` (needs `hoar.ternHeard`); starts `CutterDown` |
| Idra at the carving yard | (-12, 48) | `Idra` (`IdraPlan`, `IdraRepeat`) |
| Carving bench | (-18.5, 46) | `Fabricate — The Carving Yard` |
| Idra's tally | (-10, 47.6) | `Ask Idra about the scratches`, `idra.tallyShown` (needs `hoar.marshalAsleep`) |
| Bath-house cradle | (25.6, 37.6); saves at (23.4, 34.6) | `Vent bath-house — repair and save`, spawn `Bath` |
| Sit by the vent | (31.8, 38.6) | `SelaVentBath`, `hoar.bathSeen` (needs `hoar.marshalAsleep`) |
| Tern | (8, 60.5) | `Tern` |
| The fourth rib's tally | (12, 58.4) | `Look at the cuts in the rib`, `hoar.tallySeen` |
| Iru at the shrine | (44, 44) | `Iru` |
| The low lamp | (48.5, 48) | |
| The cut seats | (55.4, 44.2) | `Look at the cut seats`, `hoar.scarsSeen` (needs `hoar.sleepLearned`) |
| Climb foot | (40, 46) | |
| Climb's ledge | (38, 51.5) -> (43, 53.2) -> L1 -> (52.5, 54.4) -> (57, 55.2) -> (58.5, 57.8), rising 0 to 4.8 m, 4.2 m wide | |
| High lamp 1 (the ledge's landing) | (47.5, 53.8), 2.4 m up | `Light the high lamp`, `hoar.skullLamp1` |
| High lamp 2 (the high rib-hall, under the first rib) | (52, 63), 4.8 m up | `hoar.skullLamp2` |
| High lamp 3 (the knoll by the skull) | (60, 68), 6.2 m up | `hoar.skullLamp3` |
| Iru in Tern's hall | (5.2, 59.6) | `IruSpeaksUp` |
| Reach gate | (-63.5, 42) | `Reach gate — The Frozen Host` -> the Frozen Host's `Arrival`; locked on `hoar.planTaken` |
| Back from the Reach | (-57, 42) | spawn `Reach` |

The declared ways (`TownZone.Ways`, with the climb's ledge as one more): what the bake must join, what the route writer walks, and what the build refuses anything to stand on (`NothingStandsOnTheWays`: nothing solid within .9 m of a way's line, nobody standing within 2 m, no walker's loop within 1.6 m; it logs `HOARFELL_WAYS_CLEAR Lastlight`).

| Way | Plan points |
|---|---|
| Up the commons to the carving yard | (0, 4) > (0, 12) > (0, 22) > (-2, 34) > (-8, 42) > (-12, 45.6) |
| To Hask at the pens | (0, 22) > (-12, 26) > (-20, 25.8) > (-23.4, 26.4) |
| To the bath-house cradle | (-2, 34) > (8, 38) > (18, 37) > (23.4, 34.6) > (25.6, 38) |
| On to the shrine, south of the bath's basin | (23.4, 34.6) > (31.5, 34.4) > (37.2, 36.6) > (39.6, 41) > (42.4, 44) |
| Up to Tern's hall | (-2, 34) > (2, 46) > (6, 54) > (8, 58.6) |
| West to the Reach gate | (-8, 42) > (-24, 42) > (-40, 42) > (-57, 42) > (-60.8, 42), inside the gate's reach |
| To the pad | (0, 4) > (-7, 7) |
| Shrine to the climb's foot | (42.4, 44) > (38, 51.5) |
| Across the shrine floor | (39.6, 41) > (45, 42.6) > (48.5, 46.4) > (55.4, 44.6) |
| The high rib-hall | (58.5, 57.8) > (55, 60.5) > L2 (52, 63) > (56, 64.8) > L3 (60, 68) |

## Evidence

Built nine times in the editor (`HoarfellBuilder.BuildZones -hoarfell-zones Lastlight`, then `ZoneLook.Apply`), each followed by stills at the play camera (`LookDev.Render`, 19 views: each place as it is first walked, at its floor's height, and two wide) and the floor measure (`python tools/look_density.py`, at or under .22).

| Pass | What was wrong | What changed |
|---|---|---|
| builds 1-3 | The bake couldn't reach the first high lamp. The ledge's points were written (x, z, height) and read (x, height, z), so the ledge stood 50 m high along the south edge and nowhere near the shrine; and the first lamp's landing was a flat disc set into the ramp with a step all round it. A height model in Python (`climb_model.py` in the session's scratchpad: the ground's height on a quarter-metre grid, the agent's .55 m radius taken off every steep edge, a walk from the ledge's foot) reproduced both failures and passed the fix before Unity ran it | The ledge's points are (x, height, z); the landing widens the ledge and keeps the ramp's height; where the ledge meets the high rib-hall at its own height it has no rim and nothing falls from one |
| `hoar-01` | The ribs were turned 90 degrees: the generated arch spans along its own z, so each rib stood as two pillars east and west of its foot boxes (invisible walls). The vents were warm-lit discs of bare stone with seven dark stones in a ring: cookies. The snow walls were level courses of blue and white: a layered cake. The skull's jaw was a raised log, cut off flat, its bone painted with a grain that read as wood. Grey stain decals over the snow read as dirt; the tracks strip laid dashes down every way like road markings; melt laid over the pens drew them as camouflage | Ribs unturned or turned round, so each is an arch rising from the back of the town; vents heaped with stones round a dark mouth, unlit, bare stone ragged at its edge; walls in cells of packed snow and old ice with soft fractures; bone porous, no grain; no stain decals or strips; melt only in a ring round each vent |
| `hoar-03` to `hoar-05` | The jaw still a raised log; the drift decals (sand-coloured in the atlas) read as beige paper; Old Tern lost in her hides (a fur on her lap and the heap in front of her, between her and the camera); the snow's wind ridges ended at every tile's edge in a straight seam | The jaws lie low and taper to points, half under drifted snow; no drift decals; Tern lowered less, the hides heaped behind and beside her, a small fur on her lap: she reads as an old woman seated by her brazier; whole numbers of ridges across the sheet |
| `hoar-07` | The shrine still measured at the limit (.22): its plain stretch was the trodden ways where three meet, one flat grey | Worn ways mottled, the trodden layer's prints deeper and doubled, tracks scattered along the ways; carved posts hung with hide strips and charms beside the low lamp, pelts where the keepers kneel |
| route writer, November 12 | The way to the shrine turned at (30, 40.6), 0.4 m off the bath-house's wall; the high hall's way from the second lamp to the knoll was cut by that lamp's post and the knoll's steep side (a floor path of 9.8 m for 5.5 straight); the ways stopped 6.5 m short of the Reach gate, whose prompt reaches 4.4; this page had the gate at (-64, 42) and Iru in Tern's hall at (10.4, 59.6) | The shrine's way runs south of the bath's basin; the second lamp's post stands behind it to the north-west; the knoll rises over five metres, not three; the Reach gate's way runs on to (-60.8, 42); the page has the builder's numbers; and the build refuses anything standing on a way (`NothingStandsOnTheWays`, chapter 3's town's check) |
| `hoar-08` | | Every play-zoom still at or under .14 (the shrine .10, the yard .12, the knoll .14). Read as a place: the ribs arching over the back of the town, the longhouses in the rib-halls, beasts in the pens behind their fence, vents heaped with stone and steaming, Tern by her brazier, the climb up the high rib-hall's bank to the lamps |

What is weak, plainly:

- **The skull is the weakest piece.** It is built in code (a cranium in the ice wall with a brow and an empty eye, two jaws with teeth, the four cut seats), and from the play camera it reads as a great tusk lying by the shrine more than as a skull; its tubes show their facets in bands. The landmark you can name from a still is the ribs. A generated skull (one image-to-3D piece) would fix it; it was not ordered (no credits spent).
- **The walls' cells are graphic close to**: packed snow in a cell pattern with soft fractures. Better than the striped first pass; still clearly a texture.
- **Steam, snowfall, the brazier and the walkers move only in play**; a still shows them stopped or not at all. The Kept walking errands, Idra walking from the pens to the yard and Iru moving to Tern's hall are tested (`HoarfellWorldTests`), not seen.
- **Old Tern is the standing body lowered into her hides** (she has no seated clip); her idle plays from the waist up. ConversationStage frames her as it frames anyone; that framing has not been looked at.
- Nobody has walked the town in a player.

