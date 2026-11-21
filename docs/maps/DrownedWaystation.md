# The Drowned Waystation - the mire and the half-sunk station

Functional brief, written before construction (November 12, world engineer B, chapter 4). **MAP_EYE_TEST: blockout PASSED with repairs; dressed (editor) PASSED; final in-player pass OPEN** (review at the end; an agent's review, nobody else has looked). Chapter brief: [ch04-breathing-room.md](../chapters/ch04-breathing-room.md); script wiring: [ch04-manifest.md](../writing/pending/ch04-manifest.md). Builder: `S/Editor/World/WendmireBuilder.Waystation.cs`. Mechanics: [CHAPTER_GUIDE.md, "World mechanics"](../CHAPTER_GUIDE.md).

## Purpose and formation

Wendmire's mire at dusk, and the half-sunk Ancelane route station at its north end. The station was built by the same hands as Lanternwick's relays (carved terracotta courses, bronze collars and grilles, the ring-and-bar works mark) and has been sinking a hand a season ever since; its roof fell in, so it is open to the sky, and the mire's water stands in it. Three pumps of the old engineers' drain it in stages. The Rotbloom has made the flooded great hall at its back its home and fishes the mire with lures that copy Driftreed's lamps.

**The route's shape is a drain.** The mire is walked south to north across three peat islands; the station is walked down into, one stage of water at a time, and then up out of the bottom to the great hall's door. The one rule: **a pump takes the water down a stage, and what the water leaves wakes.** It is introduced with nothing awake (pump one, in an empty hall; the sentries it uncovers wait in their alcoves until the party walks to them), tested on the terrace (a sentry and two rot-pods on ground the first stage gave back), and combined on the low floor (two sentries and leeches in the sumps the second stage leaves wet). The great hall drains the same way mid-fight.

## Users and access

Nobody lives here. Maru Wend leads the party from Driftreed's mire gate to the portico and goes no further ("I don't walk under a roof that doesn't float"). Pim, Ona's apprentice, followed a lure in two nights ago and is shut in a dry pocket off the lower gallery. The party arrives by raft on the first island's south shore (spawn `Arrival`) and leaves the same way (`Mire gate — Driftreed`, to Driftreed's spawn `Mire`).

## Layout

About 140 x 330 m; about 390 m of route from the raft to the boss, plus the bell pocket, the dry spit, Pim's pocket and the vault. x runs east, z north; heights are world y. The mire's water stands at 0. The station's water starts at 0 and goes to -2.7 (pump one) and -4.5 (pump two).

```text
 z 307  .-'''''''''''''-.        GREAT HALL (floor -1.6, water -1.15 until the Rotbloom drains it)
        |  NW         NE |       four plinths (tops -0.85), C = (0, 289), radius 18
        |       (C)      |
        |  SW         SE |
 z 271  '-.____[]____.-'   seal 267.6, grille 266.6
 z 259   [ LANDING -0.5 ]  pump three, cradle, spawn Door        checkpoint
 z 250       | stair |
        +---------------------------+--------+
        | LOW FLOOR -4.0            | TERRACE|-- pump two (north end)
 Pim's  |   sump        ramp ->     | -2.4   |
 pocket |                           | alcoves|== passage == sump == THE VAULT (-2.4)
 -2.0 [grille]  sump                |        |   pump four
        +---------------------------+--------+  z 206
                                    | stair  |
        +-----------------------+---+--------+  z 192
        | ENTRY HALL  (sunk -1.2)| walk +0.3 | pump one
        |  alcoves at the back   |           |
        +---------[ portico ]----+-----------+  z 170
                    causeway (wade)                          z 140-154
          THIRD ISLAND (strider and lure)
     dry spit ~~ LEECH CHANNEL (wade, leeches)                z 85-101
                        SECOND ISLAND (lure lesson) ~ ford ~ bell hummock
                 ford (wade)                                  z 32-52
          FIRST ISLAND (raft, cradle, Maru, Bogstriders)      z -5-42
```

| Area | What it is for | Size | Level | Combat |
|---|---|---|---|---|
| First island | The arrival: the raft home, a free cradle, Maru with her lamp, the talk that starts the mire, then a strider pocket on the far side of her | 38 x 46 m | +0.4 to +0.7 | `Mire_Strider` |
| Ford | A wade to the second island past a kept lamp | 5 x 22 m | -0.3 | none |
| Second island | The lure lesson: the far kept lamp counting two short, one long, and four lure posts, the nearest flickering shorts with a Lurelight on it | 40 x 34 m | +0.5 | `Mire_Lures` |
| Bell pocket (optional) | The school raft's bell-buoy fouled in a root (SchoolBell), a fisher's tin | 10 x 12 m | +0.5 | none |
| Leech channel | The direct way: 30 m of wading in scummed, bubbling water | 7 x 30 m | -0.34 | `Mire_Leeches` |
| Dry spit (optional way) | The way round the leeches: a peat spit, longer and dry, with a cache | 8 x 40 m | +0.5 | none |
| Third island | Strider and lure together; the first fallen stones and a toppled column of the station | 30 x 40 m | +0.5 | `Mire_Third` |
| Causeway | The old road's paving under the water, bronze bollards either side, two kept lamps | 5 x 14 m | -0.24 | none |
| Portico shore | The station's door: the broken portico arch, Maru's lamp, a bench | 46 x 12 m | +0.5 | none |
| Entry hall | The pump introduced: a dry walk on the east side with pump one; the sunk floor west of it under the water with two sentries in alcoves at the back | 30 x 22 m | +0.3 walk, -1.2 sunk | `Hall_Sentries` (after pump one) |
| Stair | Down from the hall's north-east corner to the gallery's terrace | 6 x 14 m | +0.3 to -2.4 | none |
| Terrace | The gallery's east side, dry from pump one: three alcoves in its east wall, pump two at its north end, the vault passage | 14 x 44 m | -2.4 | `Gallery_Terrace` (after pump one) |
| Low floor | The gallery's floor, dry from pump two: two sumps that stay wet, alcoves on its west and north | 30 x 44 m | -4.0, sumps -4.85 | `Gallery_Floor` (after pump two) |
| Pim's pocket (optional cache) | Up a stair off the low floor's west side, behind a grille Pim tied shut; Pim inside until found, a basket after | 9 x 13 m | -2.0 | none |
| The vault (optional) | Off the terrace's east side: a passage, pump four, a flooded sump it drains (a stair down and a stair up, 3.2 m each), the dry vault with the wall of marks and a coffer | 28 x 20 m | -2.4, sump -4.2 | none |
| Landing | The great hall's door: the checkpoint, pump three (after Pim), the cradle, the grille | 22 x 6 m | -0.5 (at -0.6 the station's first deep-water boxes took it in, and the checkpoint had no walkable floor in a station loaded before its pumps) | none |
| Great hall | The Rotbloom's arena (contract below) | 36 m across | -1.6 | `Rotbloom` |

**Travel** (about 6.7 m/s walking, 0.62 of that wading): mire about 230 m (60 m of it wading), station about 160 m: roughly 70 seconds of walking and 25 of wading without a fight; with eight fights, two pockets, five talks and the boss it is the brief's 25 to 30 minutes.

## The pumps and the water (the zone's mechanic)

| Pump | Where | Prompt, report, flag | Gated by | What it changes |
|---|---|---|---|---|
| One | Entry hall, the walk's north end (13.6, 184.5); worked from (12.1, 182.9) | "Run the pump", `Pump`, `wend.pump1`, talk `SelaPumpOne` first | nothing | Station water 0 to -2.7 over 4 s: the hall's sunk floor, the stair and the terrace come out; the hall's two sentries can wake |
| Two | Terrace north end (18.5, 248); worked from (17, 246.2) | `Pump`, `wend.pump2`, bark `pump` | pump one (it stands on the terrace) | Station water -2.7 to -4.5: the low floor, the ramp, the great stair and Pim's stair come out; the sumps stay 0.4 m deep |
| Three | Landing, west of the door (-8.4, 264.2); worked from (-9, 262.5) | `LastPump` (its own step's report), `wend.pump3`, bark `pump` | `wend.pimFound` (the quest's step 8 sits between pumps two and three) | Lifts the great hall's grille. The hall stays flooded until the Rotbloom's shift, when "the last pump runs" |
| Four (FourthPump) | Vault passage (26.5, 219.6); worked from (25.3, 217.9) | `FourthPump`, `wend.pump4`; offered only at FourthPump step 0 | the quest | The vault's sump -2.6 to -4.6: the way into the vault |

- One `WaterVolume` (`Station water`) with a `StagedWater` on it (new, `S/World/StagedWater.cs`, guide 15.8): flags `wend.pump1`, `wend.pump2`, levels -2.7, -4.5. Its deep blockers are two holders, one a stage: what is deep at rest but not at -2.7, and what is deep at -2.7. A save loads at its stage with nothing played.
- Each pump is a `FlowSwitch` (marker `PUMP_RUN`) with a bronze crank on its head that turns, and `shows` the water it takes down, so a hero standing at a pump sees what it will change.
- The sentries a stage uncovers stand in their alcoves before it, shown and inert (`previewPoint`), and wake when the party walks into their room with the stage's flag set.

## Identities

Scene `DrownedWaystation` (GroundCombat, profile Mire, sky dusk, threat 1, chapter 4). Music: Swamp Sprint. Light: `WaystationLooks` in the builder. Ambience: `AMB_Mire` when it exists (none yet: the Braided River's bed stands in).

## Coordinates (for routes and tests)

All world x, z (y is the ground there). Spawns are `SpawnPoint` ids; points are where a hero stands to press.

| What | Id or prompt | x, z | Flag / note |
|---|---|---|---|
| Arrival spawn | `Arrival` | -4, 2.5 | |
| Way home | `Mire gate — Driftreed` | -9.2, -1.6 (raft) | to Driftreed spawn `Mire` |
| Cradle | `Pilots' cradle — repair and save` | -12.5, 7 (stand at -10.9, 7) | free, saves at `Arrival` |
| Mire talk (crossing) | `MaruMireGate` | box at 2, 17.6 (40 x 3) | `wend.mireEntered` |
| Maru, first island | Npc `Maru` | 8.5, 22 | from `wend.tideTurned` until `wend.lureLearned` |
| Strider pocket | `Mire_Strider` | 2, 32 (30 x 18) | 2 Bogstrider |
| Ford lamp | kept lamp | 9, 34 | two short, one long |
| Lure lesson (crossing) | `MaruLureLesson` | box at 28, 60 (32 x 3) | `wend.lureLearned`; report `LureLesson` |
| Lure posts | `AmbushSpot` x4 | 24,67 / 17,74 / 37,67 / 27,81 | the first flickers until `clear.Mire_Lures` |
| Kept lamp | | 33, 75.5 | two short, one long |
| Lures | `Mire_Lures` | 27, 72.5 (32 x 22) | 2 Lurelight (shown before, from the lesson) |
| Bell-buoy | `Ring the bell-buoy` | stand 55.6, 69.6 | SchoolBell step 0, report `BellBuoy`, `wend.bellRung`, talk `SelaBellBuoy` |
| Fisher's tin | cache `wend.mire.bell` | 53.6, 74.6 | |
| Leech channel | `Mire_Leeches` | 10.5, 93 (24 x 16) | 2 Mire Leeches; Maru's `hail.leech` from 20, 84 |
| Dry spit cache | `wend.mire.spit` | -19.6, 93.2 | |
| Third island | `Mire_Third` | -4, 120 (26 x 24) | 1 Bogstrider, 1 Lurelight; posts -12,116 / 4,127 |
| Glowspore | `wend_glow_0..4` | -9,37.5 / 33.5,83.5 / -15,91.5 / -26,111 / 54.5,65.6 | 2 each (SmokehouseOrder wants 6) |
| Bench | `Fabricate — Lattice Anvil` | -10, 157.5 | |
| Portico talk (crossing) | `MaruPortico` | box at 0, 160.4 (34 x 3) | `wend.porticoReached` |
| Maru, portico | Npc `Maru` | -6.5, 162.5 | from `wend.lureLearned` until `wend.lampCarried` |
| Pim, portico | Npc `Pim` | -4, 161.4 | from `wend.pimFound` until `wend.rotbloomDead` |
| Walk out (crossing) | `MaruAfterBloom` | box at 0, 165.2 (9 x 2.4) | needs `wend.rotbloomDead`; sets `wend.lampCarried` |
| Portico spawn | `Portico` | 2.5, 158 | |
| Hall talk (crossing) | `SelaWaystationHall` | box at 0, 173.6 (28 x 3) | `wend.hallSeen` |
| Pump one | see the pump table | 12.1, 182.9 | `wend.pump1` |
| Hall sentries | `Hall_Sentries` | -1, 190.2 (32 x 7.6) | 2 Drowned Sentry from alcoves -9,193.2 and -1.5,193.2; needs `wend.pump1` |
| Terrace | `Gallery_Terrace` | 15, 229 (14 x 40) | 1 Drowned Sentry (alcove 23.2,226.5), 2 Rot-pod (17.5,237.5 and 11.5,219); needs `wend.pump1` |
| Pump two | | 17, 246.2 | `wend.pump2` |
| Low floor | `Gallery_Floor` | -7, 228 (30 x 44) | 2 Drowned Sentry (alcoves -23.2,209.5 and -23.2,237.5), 2 Mire Leeches (sumps -12,214.5 and -6.5,232); needs `wend.pump2` |
| Pim | Npc `Pim` (first and only node `PimFound`) | -31.5, 221.5 (talk from the grille's east side, -26.5, 221.5) | `wend.pimFound` (the script sets it): the grille at x -27.6 opens, Pim stops being someone to talk to at once and, once the talk closes, walks out (-28.4,219.8 / -21.4,219.6 / -12,227 / -2,239 / 5,244.6) and is gone at the ramp (`LeavesAfter`) |
| Pim's basket | cache `wend.station.pim` | -35, 226 | behind the grille |
| Pump four | | 25.3, 217.9 | FourthPump step 0, report `FourthPump`, `wend.pump4` |
| Wall of marks | `Take a rubbing of the marks` | 47.4, 218 | FourthPump step 1, report `VaultMark`, `wend.vaultRead`, talk `SelaVaultMark` |
| Vault coffer | cache `wend.station.vault` | 45.5, 211.6 | |
| Door spawn | `Door` | -2.5, 261.5 | the boss checkpoint |
| Station cradle | `Station cradle — repair and save` | 7.5, 261.6 | free, saves at `Door` |
| Pump three | | -9, 262.5 | `wend.pump3`, needs `wend.pimFound` |
| Bloom door (crossing) | `SelaBloomDoor` | box at 0, 265.6 (8.8 x 2.2) | needs `wend.pump3`; sets `wend.bloomSeen`; saves at `Door` |

## Encounter plan

| Fight | Where | Pack | Where the player stands to answer it |
|---|---|---|---|
| `Mire_Strider` | First island, north half | 2 Bogstrider | Behind it: the island's north half is 26 m of open peat, so a slow turner can be circled on every side |
| `Mire_Lures` | Second island | 2 Lurelight, 4 posts | Outside the ring; each post stands 9 m or more from the next on dry ground, so the spent beat after a bite is in the open |
| `Mire_Leeches` | Leech channel | 2 Mire Leeches | A step back from the burst: the channel is 7 m wide, or the dry spit avoids them altogether |
| `Mire_Third` | Third island | 1 Bogstrider, 1 Lurelight | Both lessons on one island: circle the strider without walking into the ring |
| `Hall_Sentries` | Entry hall's drained floor | 2 Drowned Sentry | Behind them: the sunk floor is 22 m wide once drained, and they come out of their alcoves to it |
| `Gallery_Terrace` | The terrace | 1 Drowned Sentry, 2 Rot-pod | Toward the pods along the terrace (14 m wide); the sentry's line runs north-south, the pods sit at its ends |
| `Gallery_Floor` | The low floor | 2 Drowned Sentry, 2 Mire Leeches | The floor is 30 m across; the sumps are the leeches', and wading them is the cost |
| `Rotbloom` | Great hall | The Rotbloom | Contract below |

## Camera

The fixed camera looks north-north-east from the south-south-west. In the mire every island's water is the back of its frames, and the mangroves stand only where nothing walked lies in their shadow along the camera's line (`ShadowsAFloor`). In the station every wall that has a floor beyond it along the camera's line stays under the sightline to a hero at that floor's edge, as the Walking Canopy's root does (D119): the south and west faces of every room are low, the north and east tall. Columns, lamps and pipes stand on north and east walls, the columns half set into them: free-standing on the hall's walk, beside the terrace's alcoves and at the great hall's door, each hid a hero standing a few metres north of it, which the camera test found. A kept lamp is solid only as wide as its post. Nothing over 3 m and broader than a post stands on the camera's side of a fight.

## Review

Reviewer: Claude (world engineer B, chapter 4), from editor stills and plans, November 12. Nothing here has been walked in a player.

**Blockout** (`Builds/quality/look/wend-way-blockout-03/`, greybox at rest). The route reads as a drain: islands linked by a pale ford and a channel, then the station's hall with its dry walk and the drowned floor beside it. Pump one's view holds the water it takes and the alcoves at the back, so the pump changes something seen from where it is worked. Every island and room has room for its enemy's answer. Repaired before dressing: the peat's long strands read as stripes across every island (repainted as wandering fine roots); the water's deep was navy with grey blotches and the grade pushed it toward purple (olive-teal, finer glints, less blue in the shadows).

**Dressed** (`wend-way-final-2/` at rest, `wend-way-drained-03/` with both stages run and the hall drained). `python tools/look_density.py <folder> --match DrownedWaystation --max-blob 0.22`, each room measured in the state a player stands in it: every mire view, the portico, the hall's walk, pump one, the stair, Pim's pocket and the vault at rest (worst: ford .18, first island .09, dry spit .04); the hall's floor, terrace, low floor, vault passage and landing drained (worst: stair .20); the great hall flooded (phase one: .22, .10, plinth .05) and drained (phase two: .12, .07). Over .22, and why: the same rooms photographed while flooded at rest (nobody stands in them then) and the two overviews.

Found and repaired while dressing:
- the ground kit's puddles took the dusk sky and lay on the peat as lilac with white blotches (removed); the way-poles' cloth strips were lilac (ochre now);
- floor details under the station's water drew on top of it (the station's waters now draw after the floor's details and before danger markings);
- opaque lily pads over the great hall would have hidden a marking on the floor under them (pads and scum now draw without depth, after the water, before markings);
- the old engineers' lamps were the chapter 2 lantern shard, which reads as a blue barrel lying about (a built lamp: stone foot, bronze post and arm, caged amber globe);
- mangrove crowns read as palms (rounded domes of short leaves);
- the islands were one tone of leaf litter (sedge tussocks, moss cushions, straw drifts, peat stacks, stumps, way-poles, a drying rack, trodden paths);
- the great hall at knee depth was one sheet of water (the Rotbloom's lily pond: thick pads, algae beds and silt under the water, a fallen column, roots, a fallen rib);
- columns free-standing on the hall's walk, beside the terrace's alcoves and at the great hall's door hid a hero a few metres north of each (`WaystationTests.NoWallOrTallPieceStandsBetweenTheCameraAndAFight`; set into the walls or removed); the great hall's roots and rib drawn as one piece had a box over the whole hall (`QaWorldSecondTests.NothingWideStandsBetweenTheCameraAndAFight`; each its own piece).

What is still weak:
- **The wall masses the sightline rule lowers read as stepped brick slopes**, not walls (the stair, the portico's sides, the great hall's south-west rim). Coping, creeper and fern break the flat tops; the slopes themselves are plain masonry.
- **The bright white lily flowers** in the great hall are many and may read as sparkle rather than flowers.
- **Nothing here has been seen in a player**: the pulsing lamps (RhythmLamp sets them by property block, so every still shows them unlit), the moving cloud shade, the water stepping down, Pim walking out, the lure flicker and the dormant sentries and Rotbloom are runtime and in no still.
- The Ilu's tussocks, way-poles and peat are built in code; they read at play distance in the stills, not up close.

## Boss arena contract

*The combat engineer's section: what the Rotbloom fight needs from the room (components, sizes, anchors, what must be breakable or climbable, where the boss leaves). The world engineer builds to it and writes the coordinates as built underneath.*

Written November 12 by the combat engineer. The fight's moves are `S/Combat/BossController.Rotbloom.cs`; its numbers are in that file's `RotbloomTune` and in `WendmireBuilder.Enemies.cs`; its tests (`T/PlayMode/WendmireCombatTests.cs`) build a stand-in of this room on a bare floor, so nothing here waits on the dungeon to be tested. Positions are given from the hall's centre **C** (world x, z of the boss's spawner), with +z north, the way the fixed camera looks.

**How the fight uses the room.** Phase one, the hall is flooded knee-deep and the Rotbloom floats near the middle. Its root lashes strike only who stands in the water; the four plinths at the rim are the only dry ground, and each carries a lure-stalk. Burning a stalk makes one of its mouths gape (a five-second opening on the body) and takes a share of its bar; the second stalk burned carries it over half. At that shift the boss sends the water out (the last pump runs), the two stalks still standing go dark, it grounds where it is and seeds two rot-pods at the rim. Phase two is on the bare floor.

| Thing | What the fight needs | Why |
|---|---|---|
| Floor | Round, **radius 18 m** walkable from C, near level (a shallow bowl is fine: no step over .3 m anywhere inside r 17). Nothing solid on it inside **r 12.5** but the boss | The boss drifts inside r 6 and its lanes, cone and drag run 15 to 18 m from its middle: a column in there stops a dodge and hides the party behind a 9 m body |
| Water | One `WaterVolume` over the whole floor, **wading depth only** (feet .25 to .6 m under at full; no `WaterKit.Deep` anywhere a hero can stand, so every point stays reachable), with a `FloodBasin` on it, **`followsGates=false`** (it loads full and moves only when told). Its empty level is under the whole floor | The boss calls `ArenaWater.SetLevel(0, 3)` at its shift; `ArenaWater.Submerged(point)` is how a lash knows who is in the water. Wading (pace .62) is the flooded phase's cost; the boss itself floats (`motion` Hover) and isn't slowed |
| After the win | The hall loads **drained** once `bossdown.Rotbloom` is set: e.g. `drainFlag="bossdown.Rotbloom"` on the `WaterVolume`, and a `FlagGate` (flag `bossdown.Rotbloom`, hidden when set) on the `FloodBasin`'s object, since a basin with `followsGates` off refills on its first reading | A won hall must not load flooded |
| Plinths | **Four**, at bearings **45, 135, 225, 315** (NE, SE, SW, NW) with centres **15 m** from C. Flat dry tops of **radius 2.6 m**, standing **.15 m or more above the full water** (about .7 to .9 m over the floor beside them); a ramp or steps on the side facing C (walkable: the hero's step is .35 m, or a slope under 35 degrees); the outer side may meet the hall wall | The only dry ground in phase one: a lash can't reach a hero on one, and the stalk on it is what he came to burn. Room on top for a hero to stand on any side of the stalk |
| Lure-stalks | On each plinth's top, centred: a **`LureStalk`** (`S/Combat/LureStalk.cs`) on an object with a `Health` (id **`Lure-stalk`**: what the target card says), a trigger `SphereCollider` (radius **1.3**, centre **2.2 m** up) and a `Hurtbox` owned by that `Health`. Fields: `index` **0 NE, 1 NW, 2 SW, 3 SE** (anticlockwise round the rim as the camera sees it, so the boss's pattern never depends on the order the scene woke them); `settledBy="Rotbloom"`; `defeatedFlag` `wend.stalk.ne`, `wend.stalk.nw`, `wend.stalk.sw`, `wend.stalk.se`; `whole` = the `LureStalk` prefab (4.2 m, `Prefabs/Enemies/LureStalk`) and its light; `burned` = a scorched stump, hidden until it burns. Leave `Health.maximum` alone: the boss sets each stalk's integrity, weakness (Plasma) and resistance (Beam) as the fight begins | A break target that answers to the boss is shut, untargetable and unlit as a target until the boss is awake, so nobody burns a stalk from the door (chapter 3's fault). A burned stalk stays burned in the save; all four load settled once the boss is down |
| Light | Each stalk's bulb is a light source (a point light under `whole`, so it goes out when the stalk burns), plus fixed lamps that keep the hall readable with all four out: at the door and at two places on the rim between plinths (six lights at most on the floor renderer) | The lures are the room's lamps, which is the chapter's story; the fight must still read once they are burned and the water is gone |
| Boss | Its `Spawner` at **C**, facing the door, `faceAsPlaced=true`. `previewPoint` at the door with a radius that takes in C, so it floats there shown and inert (`DormantEnemy`) until the fight | It is in the room from the first look through the door and never leaves it |
| Door | South side (the camera's side), encounter id **`Rotbloom`** (the main quest's step 10 names it), trigger covering the floor (about 38 x 38 m on C), `requiredFlag="wend.bloomSeen"` (the `SelaBloomDoor` crossing), `checkpointSpawn` a spawn **outside** the seal, within 60 m of the door and on navigation, with a free cradle under a minute's walk (pump three's landing does both). One `Membrane` seal across the door, strands drawn instead of its cube | The shared door tests: the checkpoint, the retry, no way out mid-fight. The boss sheet in `WendmireCombatTests` names zone `DrownedWaystation`, door `Rotbloom`, door flags `wend.pimFound`, `wend.pump3`, `wend.bloomSeen` |
| Rot-pods | The floor at **12 m** from C on bearings **0, 90, 180, 270** (between the plinths) stays clear of props: at the shift the boss seeds a `RotPod` on the two of those nearest the lead | Summons need open floor within 1.2 m of the boss's level |
| Rim | South and west rim low (the camera's foreground); north and east may be the hall's tall wall. Nothing over 3 m and 7 m across between the camera and C | Fixed three-quarter camera: nothing important behind the boss, and nothing between the camera and the fight |
| Leaving | None: the Rotbloom is killed where it lies (`Ending.Destroyed`). No `BossExit` | It is a predator and it dies (brief) |
| After the fall | `SelaBloomDead` by `AfterTalk` on `bossdown.Rotbloom` (sets `wend.rotbloomDead`). The boss plays no down bark of its own, so nothing talks over that scene (manifest 8.8) | |

What the fight sets and logs, for the builder's own checks: `bossdown.Rotbloom`, the four `wend.stalk.*` flags as stalks burn, `BOSS_SHIFT Rotbloom 2`, `ROTBLOOM_STALK_BURNED`, `ROTBLOOM_DRAINS`, `ROTBLOOM_GAPES`.

### Coordinates as built

World engineer, November 12, from `WendmireBuilder.Waystation.cs` (the numbers are the builder's constants; `WaystationTests.TheGreatHallIsBuiltToTheBossContract` checks them in the built scene).

| Thing | As built |
|---|---|
| C | **(0, 289)**; floor -1.6 at the rim, -1.68 at C (a bowl of 8 cm), noise under 2 cm. No step inside r 17 but the plinths |
| Water | `Great hall water` (`WaterVolume`, reach 40 x 45 on (0, 288.5)): full **-1.15** (feet .45 to .53 under), `drainFlag="bossdown.Rotbloom"`, `drainedLevel` **-2.3** (70 cm under the floor), 3 s. `FloodBasin` on `Great hall flood`: `fullLevel` -1.15, `emptyLevel` -2.3, `seconds` 3, `followsGates=false`. `FlagGate` on its own object (`Great hall flood (gone once the Rotbloom is dead)`): flag `bossdown.Rotbloom`, hides the basin. No `WaterKit.Deep` in the hall |
| Plinths | Centres 15 m from C: NE **(10.61, 299.61)**, SE **(10.61, 278.39)**, SW **(-10.61, 278.39)**, NW **(-10.61, 299.61)**. Tops flat to r 2.6 at **-0.85** (.3 over full water, .75 over the floor); steep (4 in 1) all round but a wedge facing C, which is a ramp at .32 in 1 (18 degrees) out to about r 4.9, with five stone treads on it. Their outer edges reach r 17.6 |
| Lure-stalks | `Lure-stalk NE/NW/SW/SE` on the plinth tops at **(x, -0.85, z)** of each, facing C: `Health` id `Lure-stalk` (maximum left alone), trigger `SphereCollider` r 1.3 at 2.2 up, `Hurtbox`, `LureStalk` index **0 NE, 1 NW, 2 SW, 3 SE**, `settledBy` Rotbloom, `defeatedFlag` `wend.stalk.ne/nw/sw/se`; `whole` = the `LureStalk` prefab and a point light (`Lure bulb light`, range 9, 2.6, pale green) at (0, 3.4, .5) local; `burned` = a charred stump and ash ring, hidden |
| Fixed lamps | Two, each a lantern shard of the old engineers' with a cool point light (range 12, 2.4): at the door **(0, 270.4)** and on the east rim **(17.6, 289)**. With the four bulbs that is six lights in the hall while every stalk stands, and the two fixed ones once all four have burned |
| Boss | Spawner at **C**, facing south (yaw 180), `faceAsPlaced`, radius 0. `previewPoint` `Rotbloom preview (the door)` at **(0, 266)**, radius 26 |
| Door | Encounter `Rotbloom`, trigger **38 x 8 x 38** on C, `requiredFlag` `wend.bloomSeen`, `checkpointSpawn` `Door`, one membrane: `Great hall seal` at **(0, 267.6)**, 8.8 wide, cube removed, bronze bars drawn while shut, carving obstacle while shut. South of it the bronze grille (`Great hall grille`, z 266.6) stands until `wend.pump3` |
| Checkpoint | Spawn `Door` **(-2.5, 261.5)** on the landing (-0.5), 6 m outside the seal and on the paths. Free cradle `Station cradle — repair and save` at **(7.5, 261.6)**, saves at `Door`. Pump three at (-8.4, 264.2), worked from (-9, 262.5) |
| Rot-pod places | N (0, 301), E (12, 289), S (0, 277), W (-12, 289): open floor, nothing drawn or solid on them |
| Rim | South and south-west: the hall's wall stays under the sightline to the floor's edge (the builder's camera rule), about 1.4 m over the floor at the passage. North and east: 4 to 6 m of masonry with six columns at bearings -20, 0, 20, 70, 90, 110, each walked back from r 19.4 until nothing of it overhangs r 18.6, and roots hanging off the top. Nothing stands between the camera and C (`WaystationTests.NoWallOrTallPieceStandsBetweenTheCameraAndAFight` samples every fight's floor) |
| After the fall | `AfterTalk` `The hall after the Rotbloom`: flag `bossdown.Rotbloom`, node `SelaBloomDead`, speaker Sela, `heardFlag` `wend.rotbloomDead`, delay 3. No `BossExit` |
