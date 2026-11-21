# Nacre Grave - the wreck field round the broken berth

2026-11-12. Builder: `HomeBuilder.Nacre.cs` on `ZoneKit` (chapter 9's first engineer, who also owns the chapter's definitions, dialogue list and the beats it stands in older scenes: `HomeBuilder.cs`, `HomeBuilder.Approach.cs`). Chapter brief: [ch09-the-house-that-can-leave.md](../chapters/ch09-the-house-that-can-leave.md), "NacreGrave". Script wiring: [ch09-manifest.md](../writing/ch09-manifest.md); who places what: [ch09-wiring.md](../chapters/ch09-wiring.md). Mechanism: `S/World/WreckTrack.cs` (the tracks), `S/World/FirstSight.cs` (first-sight barks), `S/World/SignalBeacon.cs` (the three lamps). Tests: `T/PlayMode/HomeWorldTests.cs`. **MAP_EYE_TEST: blockout written before construction (this page); editor stills and what they found are under Evidence.**

## What the place is for

The Nacre berth was the one door every hauler line queued at. When the bridled Choir thrashed through it (chapter 8's end), it broke the berth open and threw the queue into an older salvage spiral that already turned round it. Bryn Asta's tug has been holding four hundred hulls together since. The party comes to pull out three: **Oda's hauler** (line nine hundred and four), **the tug Welcome Back** itself, and **the navigators' pod**. It is a rescue, and the reunion with Oda is the emotional start of the chapter.

**The rule: a wreck field with a spiral.** The field turns round the broken berth, quicker the further in. The lane is the spiral itself, a turn and a quarter of open water cut through the old salvage, winding inward to the basin round the berth.

- **Outward is slow and safe.** On the lane's outer side nothing moves but old hulks drifting at the reef's edge; the tow line on its inner side creeps at 2.6 m/s on the outer pass. Following the spiral all the way round is the long way in, and nothing hits you.
- **Inward is quick and dangerous.** The breach, a channel blown through the inner salvage halfway round, cuts three quarters of a turn: it drops straight into the basin, but the **breach race** crosses it (fresh wreckage at 13 m/s) and a Bridle fragment thrashes on its tether in it. In the basin the **berth ring** circles the berth at 15 m/s, and the navigators' pod is lodged inside it. On the tow line itself the tows run quicker as they go in (2.6 m/s outside, 8 at the mouth).
- **What is drawn**: every track is a line of lamps on the plane. Each lamp **stands amber** over the stretch a wreck will cover in the next 2.5 seconds, so a quicker wreck throws a longer amber ahead of it, and everywhere else the lamps **run cold**: a chase of pale light travelling the way the field turns, at its speed there, so the chase crowds and creeps outward and stretches and races inward. With a ship near, the amber stretch is also **marked on the plane as a danger** (`Telegraph.Threat`), stretch by stretch along the curve, and a wreck that touches a ship inside its marking takes 8 to 14% of its Integrity (the place's damage) and throws it clear off the track.
- **What tells the passes apart in a still**: the outer pass is wide and dark, its tow line's amber short; the inner tracks are mostly amber with long lit stretches; the berth sits in the middle of the basin with its gash toward the breach; the families' paint (teal, ochre, cream, sky, green) is on the fresh wrecks and the tows' hulls, and the old salvage of the walls is grey and rust.
- **Beyond**: the Choir's passage, a bruise of light (green-yellow at its edge, blue-violet in it), far under the field behind the basin and the inner pass.

Users: the party (Bryn and Neve on the radio and at their hulls; Oda at her door), Bryn's tug and its raft, the tow-drones gone wrong (still hauling hulls toward a berth that isn't there), one fragment of the Bridle. Nobody lives here any more; four hundred families are waiting.

## Rules of construction

- Everything flies at the flight plane (y 1). Backdrop is drawn below it and kept clear of what must stay readable by where it shows on screen (`ZoneKit.ClearOnScreen`).
- **The lane** is the spiral `r = 158 - 52 * turn / 360` round the centre (0, 0), turn in degrees clockwise from north, from turn -18 to turn 452, 32 m wide (16 either side: the flight frame is 25 m deep, so a lane much wider never shows its walls). Between its two passes (turns -18 to 92 against 342 to 452) the old salvage is 20 m thick.
- **The basin** is the open circle of radius 70 round the berth; the berth is solid inside 30. The lane opens into it along its last 52 degrees (turns 400 to 452, the **mouth**). The **breach** is a channel 22 m wide along the line out from the centre at turn 200, from the basin's rim to the lane's inner wall (radius 70 to 113).
- **Walls** are short static boxes laid along each curve (the outer wall, the inner wall, the basin's rim, the breach's sides, the two ends and the mouth's jamb), each half a metre longer than its piece so neighbours overlap. **None runs on past a bend**: `ZoneKit.FlightCorridor` runs each leg's walls the corridor's half width past its ends, which stood a wall across a current in chapter 6, and isn't used here. `HomeWorldTests` flies a hull's sphere along the whole lane, the breach and the basin and finds nothing solid in open water.
- **Every wall is drawn as reef**: old hulks (twelve hull and station pieces of the kit, turned any way, in the salvage's greys and rust) packed three rows deep outside it, each standing a third of its height over the plane, so the wall reads as a bank of wreckage at the frame's edges. A hull is stopped 3 m short of a wall's line (the wall's own thickness and its hull), so a hulk may lean out over the water by up to 2.5 m; one that would reach further is dropped, not moved (seeded, so the reef stands the same every build).
- **Every gap takes Idra's hull** (2.17 m): the quiet side of the lane is 22 m clear of the tow line's marking; every prompt has room for a hull within its reach; nothing solid in open water is closer to another than 6 m.
- The rule is drawn, not written: lamps, their amber and their cold chase, the markings, the wrecks riding them, the families' colours, the berth. Bryn says it once (`BrynNacreApproach`); the barks only follow it.

## Plan

North up; x east, z north. The lane winds clockwise (east from the arrival, then south, west, north and in).

```text
                         z 160   ARRIVAL (-14) -> lane runs east;  outer marker, Bryn's line (turn 8)
          warp home (-22, 155)                                     Neve's skiff (102, 122)
    the outer pass                                                  \
   (west, z 0..90)      inner pass (turn 342-452)                     the outer pass (east), x 140
    lot (-116, 47)         |  lane's mouth (85, 31) -> basin         tows (143, -25)
                           |   Oda's hauler (58, 31)
    tows (-121, -17)    BASIN r 70: berth (0, 0) solid r 30, berth ring r 48, pod (-26, 29)
                           |   tows (-53, -28)
                         breach: turn 200, from (-38, -103) down to (-24, -66); race at r 91; fragment (-34, -92)
                         z -134   the tug Welcome Back (9, -134) and her raft
```

| Stretch | Turns | What | Answer |
|---|---|---|---|
| The approach | -18 to 20 | Arrival, the way home, the outer marker, Bryn's line across the lane at turn 8 | |
| The outer pass, east | 20 to 150 | Neve's skiff (turn 40); the tow line on the inner side, slow; the first tows break off the line (turn 100) | Keep to the outer side: nothing hits you there |
| The outer pass, south | 150 to 250 | The tug and her raft (turn 176); the breach's mouth on the inner side (turn 200) | The long way: on round the lane. The quick way: down the breach, across the race, past the fragment |
| The outer pass, west | 250 to 360 | Tows again (turn 262); Mira's lot on the outer wall (turn 292) | |
| The inner pass | 342 to 452 | Between the outer pass and the basin; the tow line quicker here; the lane opens into the basin from turn 400 | |
| The basin | inside r 70 | Oda's hauler just inside the mouth (turn 62, r 61); the berth ring at r 48; the pod inside it against the berth (turn 318, r 35.5); tows (turn 242) | Oda from the mouth without crossing anything; the pod across the ring, between two wrecks |

## The tracks (built exactly so; the tests read these)

| Track | Line | Speed | Wrecks | A wreck's reach, its blow | Lamps |
|---|---|---|---|---|---|
| The tow line | 5 m inside the lane's inner wall (offset -11 from the lane's middle), turn 16 to 446, open: each tow comes up out of the field over its first 16 m and goes down into it over its last | 2.6 x (140 / r)^2 m/s, 2.6 at least (the outer pass) and 8 at most (the mouth) | 8 tows: a hull in a family's colour, hauled by a tow-drone on a line | 3.5 m; 8% of a hull's Integrity, thrown 5 m | every 3 m |
| The breach race | radius 91, turn 150 to 250, open (it rises over the salvage and goes down into it either side of the breach) | 13 m/s | 2 fresh wrecks | 4 m; 14%, thrown 7 m | every 2.5 m |
| The berth ring | radius 48, a loop | 15 m/s | 4 fresh wrecks | 4 m; 14%, thrown 7 m | every 2.5 m |

Every track's amber is 2.5 seconds of its wreck's travel plus its reach; a stretch (6 m of track) is marked while any of it is under amber and the lead ship is within 55 m; a wreck strikes only inside a marking that has stood half a second. The figures for each, as built, are in the coordinates log line `HOME_COORDS NacreGrave ...` (traverse times and the tow line's length).

## What happens along it

| Where | What | Flags, ids |
|---|---|---|
| (-14, 1, 160) | Arrival (spawn `Arrival`, facing east down the lane); `Warp — The Hull of Many Hands` at (-21.7, 1, 154.6), open, lower left in the arrival's frame; the outer marker's buoy upper right | arrives at Many Hands' hull, spawn `Nacre` |
| turn 8 | `BrynNacreApproach` on a crossing across the lane past the outer marker | the point's flag `home.nacreReached`; the talk sets `home.nacreSpoken` |
| turn 70, turn 236 | Bryn on the radio, `hail.spiral` (two in turn), once `home.nacreSpoken` | `nacre.hailOuter`, `nacre.hailInner` |
| (101, 1, 120) | `Hail — Neve` at her skiff, a pod of survivors in tow; needs `home.nacreSpoken` | `NeveReturnOffer`, starts `ReturnAddress`; point's flag `neve.offered` |
| (10.1, 1, -145.0) | `Hail — The tug Welcome Back`; needs `home.nacreSpoken` | `BrynWelcomeBack`, `home.tugFound`; report `WelcomeBack` |
| (-1, 1, -142) | `Hand over the parcel` at the tug's rail, while `ReturnAddress` is at step 0 | `neve.parcelTug`, report `TugParcel`, bark `parcel.tug` |
| (-114, 1, 46) | `Read Mira's tag` at a pale crate lashed to an old hull, while `SecondInventory` is at step 0 (shown only then) | `mira.lotNacre`, report `Consignment`, bark `consign.nacre` |
| (58, 1, 31) | `Hail — Line nine hundred and four` at Oda's hauler; needs `home.nacreSpoken` | `OdaFound`, `home.odaFound`; report `OdaHauler` |
| (-26, 1, 29) | `Hail — The navigators' pod`, inside the berth ring; needs `home.nacreSpoken` | `BrynNavigators`, `home.navigatorsFound`; report `NavigatorsPod` |
| anywhere | `FlagAll` over the three | `home.nacreDone` (Many Hands' notice waits on it) |
| first wake of `Nacre_TowsOuter` | the lead's `towdrone` | `nacre.towdroneSeen` |
| within 34 m of the fragment | the lead's `fragment` | `nacre.fragmentSeen` |
| turns 64, 150, 334, on the lane's outer edge (offset 12.6, solid 2.8 m) | wrecks in the families' colours, each said once (`wreck`) | `nacre.wreck1..3` |
| the breach's mouth; the lane's mouth once Oda is found | the lead's `spiral`, in and back out | `nacre.spiralIn`, `nacre.spiralOut` |

## Coordinates for routes

| Place | World (x, y, z) | Id / flag |
|---|---|---|
| Arrival | (-14.0, 1, 160.1) | spawn `Arrival` |
| Warp home | (-21.7, 1, 154.6) (turn -8, 3 m inward) | `Warp — The Hull of Many Hands` -> `ManyHandsHull`, spawn `Nacre`; 9.4 m from the arrival, outside its 6.5 m reach |
| Outer marker (buoy, solid 1.8 m) | (-4.3, 1, 165.2) (turn -1.5, 7 m outward) | |
| Bryn's crossing | the line across the lane at turn 8, from (19.6, 1, 139.5) to (24.1, 1, 171.2) | `home.nacreReached`, `BrynNacreApproach` |
| Neve | hail (101.1, 1, 120.4); skiff (104.3, 1, 124.3), solid 2.6 m | `neve.offered` |
| The tug | hail (10.1, 1, -145.0), 12.8 m out on her outer side, at the outer edge of open water for a hull; tug (9.7, 1, -138.4), 6.1 m out, solid 3.8 m; her raft in line with her either side, 6.5 to 7 m out (solids 4.3 to 9.5 m out), and two more past the outer wall. Inside them the quiet side is open from her solid (2.3 m out) to the tow line's marked stretch (7 m in): 9.3 m with nothing across it (`HomeWorldTests.TheLaneIsOpenByMoreThanTwoHullsAllTheWayRound`); outside them a strip to her hail, which chapter 9's routes fly (`tools/home_routes.py` holds a route a hull and half a metre off her 3.8 m sphere, so she can't move further out). Before November 14 she lay 4 m out with her raft 2 to 7.5 m out, and a ship passed only at the outer edge | `home.tugFound` |
| The tug's parcel | (-1.0, 1, -142.4) | `neve.parcelTug` |
| Mira's lot | hand (-113.9, 1, 46.0); crate (-118.5, 1, 47.9), solid 1.6 m | `mira.lotNacre` |
| Oda's hauler | hail (58.3, 1, 31.0); hauler (53.9, 1, 28.6), solid 4.6 m | `home.odaFound` |
| The navigators' pod | hail (-26.4, 1, 29.4); pod (-23.8, 1, 26.4), solid 2.4 m | `home.navigatorsFound` |
| Breach mouth (lane side) | (-40.1, 1, -110.0) | |
| The breach, down its middle | (-38.7, 1, -106.3) (r 113, the inner wall's gap) -> (-31.1, 1, -85.5) (the race at r 91) -> (-23.9, 1, -65.8) (the rim) | |
| Lane's mouth (into the basin) | (84.5, 1, 30.7) | |
| Crossing the berth ring toward the pod | (-48.5, 1, 28.0) (r 56) -> (-41.6, 1, 24.0) (on the ring) -> (-34.6, 1, 20.0) (r 40) | |
| Fights | `Nacre_TowsOuter` (143.3, 1, -25.3), `Nacre_TowsInner` (-121.0, 1, -17.0), `Nacre_TowsBasin` (-53.0, 1, -28.2), `Nacre_Fragment` (-33.5, 1, -92.1) | `clear.<id>` |

The lane's middle and its quiet outer side (7 m out from the middle), every 30 degrees, for a route that keeps to the safe side all the way round (straight legs between these stay in the lane):

| Turn | Middle | Quiet side (+7) | Middle's radius |
|---|---|---|---|
| -10 | (-27.7, 1, 157.0) | (-28.9, 1, 163.9) | 159.4 |
| 20 | (53.1, 1, 145.8) | (55.4, 1, 152.3) | 155.1 |
| 50 | (115.5, 1, 96.9) | (120.9, 1, 101.4) | 150.8 |
| 80 | (144.2, 1, 25.4) | (151.1, 1, 26.6) | 146.4 |
| 110 | (133.5, 1, -48.6) | (140.1, 1, -51.0) | 142.1 |
| 140 | (88.6, 1, -105.5) | (93.1, 1, -110.9) | 137.8 |
| 170 | (23.2, 1, -131.4) | (24.4, 1, -138.3) | 133.4 |
| 200 | (-44.2, 1, -121.3) | (-46.6, 1, -127.9) | 129.1 |
| 230 | (-95.6, 1, -80.2) | (-100.9, 1, -84.7) | 124.8 |
| 260 | (-118.6, 1, -20.9) | (-125.5, 1, -22.1) | 120.4 |
| 290 | (-109.1, 1, 39.7) | (-115.7, 1, 42.1) | 116.1 |
| 320 | (-71.8, 1, 85.6) | (-76.3, 1, 91.0) | 111.8 |
| 350 | (-18.7, 1, 105.8) | (-19.9, 1, 112.7) | 107.4 |
| 380 | (35.3, 1, 96.9) | (37.7, 1, 103.5) | 103.1 |
| 410 | (75.7, 1, 63.5) | (81.0, 1, 68.0) | 98.8 |
| 440 | (93.0, 1, 16.4) | (99.9, 1, 17.6) | 94.4 |

A straight leg between two of these 30 degrees apart cuts the curve's inside by up to 2.6 m on the outer pass: on the quiet side that is still 11 m clear of the tow line's marking.

## Flight fights contract

*What the zone needs of the fights, written by the zone's engineer; the first combat engineer writes what is built and its numbers in the second column. Until an id is defined, each stands in as the enemy named (`HomeBuilder.Foe`, which logs `HOME_FOE_STANDIN`).*

| Thing | What the zone needs | As built (combat engineer) |
|---|---|---|
| `TowDrone` | Three encounters of two (`Nacre_TowsOuter` on the outer pass east, `Nacre_TowsInner` on the outer pass west, `Nacre_TowsBasin` in the basin), each a 30 to 36 m trigger box across the lane, spawned on the plane at its middle. "A rescue drone dragging toward a berth that no longer exists; harpoons and reels": the brief's `Tetherer` (the guide lists it as the Tow-drone's family), Pulse weak, Kinetic resists. Its reel must not drag a ship onto a track's marked stretch faster than a ship can roll out of it, or across the lane's inner wall. The drawn tows on the tow line are scenery (`TowDrone`'s body, no brain), not this. Stand-in: `ClaimDrone` | `Tetherer` that **tows** (`tune.tow`, a mode of chapter 9's). It holds off 12 m from a ship and marks a harpoon line 15 m long and 1.2 wide for .9 s; the catch is a marked hit of 128 (Kinetic). Caught, a ship flies at .45 of its speed, is reeled toward the drone at 3.5 m/s (never closer than 3.5 m), and the drone **hauls it the way it was set facing** at 3 m/s: it takes a ship where it takes hulls. It lets go when the ship rolls, when anything hits the drone, when cover comes between them, at 22.5 m, or after 3.5 s. Answer: roll the line, or one shot on the drone. So in 3.5 s a catch moves at most 10.5 m along the tow and 12 toward the drone, at under half a ship's speed, and a roll ends it at once; a controller stops at a wall, so nothing drags a ship through one. Integrity 900, break 200, flies at 6.5 m/s, capsule .9 by 1.8 (Hover). Pays 40 XP. **Needs from the zone**: each spawner set **facing along the lane inward** (the way the tows go: `Along(turn)` at its turn; in the basin, tangent to the berth ring the way the field turns, so it hauls round the basin and not into the berth), `faceAsPlaced`, ring radius 3 (the default); 14 m of open water round each spawner on the plane |
| `BridleFragment` | One, `Nacre_Fragment`, in the breach at (-33.5, 1, -92.1), 6 m inside the race (r 91) on the basin side of it, on show and asleep from load (`previewPoint`), woken within its 22 m box. "A broken cage segment thrashing on its tether; sweeps a marked arc": anchored (it never moves off its tether's end; the zone draws the tether from (-44, 1, -97)), its sweep a marked fan across the breach. The answer the zone gives it: the breach is 22 m wide, so a sweep that covers it all leaves the gaps between sweeps; don't make its arc reach the race's line (r 91) or the rim (r 70) so the danger reads as two things, not one. Kinetic weak, Plasma resists. Stand-in: none; until it exists it is its drawn body on the tether, tumbling | `Anchored` **on a tether** (`tune.tethered`, a mode of chapter 9's). It never moves and **never turns**: every 2.6 s while a ship is within 14 m it thrashes, a marked fan of **90° out to 9.5 m** laid along the way it was set facing, 1 s, then a lash of 130 (Kinetic) inside it. It can't swing over its own tether: the half of the breach between it and the tether's wall is never struck. Answer: **come in along the tether** and break it from there, or cross its side of the breach between thrashes. Integrity 2,000, break 380, capsule 1.6 by 4.4. Pays 120 XP. **Needs from the zone**: make the tether fast **due west of it across the breach**, on the breach's west wall at about **(-43.4, 1, -88.5)** (bearing 290 from it), and set its spawner **facing bearing 110** (across the breach, away from the tether: yaw 110), `faceAsPlaced`, radius 0. Then its fan spans bearings 65 to 155: at its innermost edge it reaches r 91.5 and at its outermost r 104.9, so it touches neither the race (r 91) nor the lane's inner wall (r 110), and the rim not at all; across, it reaches 9.5 of the breach's 11 m east half. Its lee, the west half along the tether, is 11 m wide and must stay open water (the tether drawn, not solid) |
| Damage | Authored at its real weight at a zone threat of 1: 9 to 13% of a hero's bar at the Sync the party arrives at (about 25, `DevLoadout chapter9`), measured through `Health.Receive` | Every ordinary hit of the chapter is authored at 126 to 132 (`HomeBuilder.Enemies.cs`, its summary has the arithmetic): 12.2 to 12.8% of Taren's or Sela's bar at Sync 25 in Tempered parts, 9.1 to 9.5% of Idra's at 27 in Choirwrought, measured on all three heroes by `HomeCombatTests` |
| The tracks | The zone's, not an enemy's: 8% (the tow line) and 14% (the race, the berth ring) of a hull's Integrity, no source, never the last of it (`WreckTrack.strike`) | |
| Barks | `towdrone` (first wake of `Nacre_TowsOuter`) and `fragment` (within 34 m of it) are the zone's (`FirstSight`); nothing of the combat side barks here | |

## What is in frame

The flight frame is about 45 m wide and 25 m deep at the plane.

- **On the outer pass** the lane fills the frame's width: the reef of grey hulks along one or both edges, the tow line's lamps along the inner side (mostly cold, short amber ahead of each tow), a tow with its drone and its line, wreckage deep below.
- **At the breach's mouth** the inner wall breaks, the channel runs down toward the basin, and the race's amber crosses it.
- **In the basin** the berth's ring of station segments, its hub and arms, its gash toward the breach; the berth ring's lamps (mostly amber: 15 m/s); Oda's hauler in ochre with the family's bundles strapped to it, a lamp in its window and the evacuation lamp blinking on its spine; the pod's grease-drawn hatch.
- **Beyond**, under everything, the Choir's passage: a bruise of light.

## Evidence

Built in the editor (`HomeBuilder.BuildZones -home-zones NacreGrave`, then `ZoneLook.Apply`) and looked at through the flight camera (`LookDev.Render`, 18 views: `HomeBuilder.Nacre.Views`). Sheets: `Builds/quality/look/home-nacre-01/sheet.png` to `home-nacre-05/sheet.png` (the last with its eighteen stills).

| Pass | What was wrong | What changed |
|---|---|---|
| `home-nacre-01` | Most frames were bare stars: the lane was 38 m wide (the frame is 25 deep) and 286 reef hulks were dropped for reaching a metre into the water, the rest sunk under the plane. The sky, tinted rose-violet, was one saturated purple. The "bruise" quads drew the shared flare's spikes: lens flares and extra stars. The tug was code-built boxes in tiled grey. Built shapes' colours came out chalky (vertex colour isn't converted) | Lane 32 m; the reef three rows deep, a third of each hulk over the plane, leaning up to 2.5 m over the water; a lane of small wreckage 2 to 9 m under the plane; the sky dulled to steel; every glow on a soft round texture; the tug a generated cutter in her green; shapes' colours written linear |
| `home-nacre-02` | Read as a place: the reef at the frames' edges, wreckage under every ship, the tracks' lamps (cold chases, amber ahead of each wreck), the berth ring mostly amber beside the pod, the spiral and its passes in the wide view. Still wrong: Oda's hauler twenty metres long with its bundles as pastel balls on sticks; the tug cut off at the frame's foot; the arrival with no landmark in it | The hauler fitted to 4.2 m tall, its belongings rows of sacks, crates and rolls under lines across the hull; the tug's hail on her outer side so she stands in the frame's upper half; the warp home and the outer marker moved into the arrival's frame. Built and lit November 13 |
| `home-nacre-03` (November 14, the pass 2 changes seen) | The arrival was still two ships in the dark: the warp home stood 17 m west and the buoy 17 m east and 10 south, both just past the frame (it is about 25 m wide and 19 deep at the plane). The tug's eight fenders were black drums standing off her hull (laid out in the lane's frame from fixed numbers, on a hull longer and narrower than that frame), her boom a plank in the air, and two raft lines ran through the hail point where the ship stops. The pod's charted hatch sank into its own hull and showed as two broken arcs. Oda's belongings were pale code-built blocks and flat lumps beside a generated hull | Warp home at turn -8, 3 m inward (lower left); the buoy at turn -1.5, 7 m outward (upper right). Hull, fenders and boom laid unturned, measured, then turned to the lane together; the two far raft hulls moved off to either side. The hatch plate laid over the curve. The belongings are the kit's crates, netted pallets and drums, lashed down, with bedding rolls |
| `home-nacre-04`, `-05` | The fenders still stood at the edge of a box that takes in the cutter's fins; the cargo, at a metre and a half, hid the hull | Fenders at .62 of the box's half width; cargo a metre and a bit. Seen: the arrival has the warp home and the approach's lit buoy in frame, the tug reads as a working hull holding a raft, Oda's hauler as a household's things lashed to a hull with its lamp calling, the pod's hatch as a chart |

What is weak, plainly:

- **The lane's walls are off the frame on most of the passes**: the lane is 32 m and the frame about 25 by 19 at the plane, so on the outer pass a still shows wreckage scattered round the ship rather than a bank on either side; the tracks' lamps are what say where the lane runs. The inner pass's still shows its tow line only at the frame's foot.
- **Oda's hull is mostly hidden under her household**, which is the point, but the family's ochre shows only at the edges.
- **The rule is clearest in motion**: in a still, inward reads only as more amber on the inner tracks; the chase quickening inward is play only, and nobody has flown it in a player.
- **The tracks' wrecks pass over the old salvage** where a track runs through it (the race either side of the breach), drawn and not hidden.
- **No ambience bed** of its own; the berth is kit station segments round a code-built hub.
