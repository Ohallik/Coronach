# The Picket of Teeth - the Muster's border

2026-11-12. Builder: `HarrowgateBuilder.Picket.cs` on `ZoneKit` (chapter 8's first engineer, who also owns the chapter's definitions and dialogue wiring in `HarrowgateBuilder.cs`). Chapter brief: [ch08-harrowgate.md](../chapters/ch08-harrowgate.md). Script wiring: [ch08-manifest.md](../writing/ch08-manifest.md). Mechanism: `S/World/PicketAlarm.cs`, `S/World/SensorSweep.cs`, `S/World/RouteLock.cs`, `S/Combat/SensorShade.cs`, the gauge `S/UI/PicketGauge.cs`. Tests: `T/PlayMode/HarrowgateWorldTests.cs`. **MAP_EYE_TEST: blockout written before construction (this page); editor stills and what they found are under Evidence.**

## What the place is for

The Vaskar Muster's border, between the convoy's lane from Many Hands and the volcanic world Scoria. A band of rock teeth, sensor buoys sweeping it, a carrier standing off its east side, and a wall of spires with flak gates in it. The one clean lane through the wall is shut by a Compact **route lock**, the one Taren installed eight years ago as a job. The zone's rule is **staged detection**: the picket wakes in three stages, and each shows itself before it comes.

- **What the picket sees** is drawn on the plane: each buoy's cone is a pale beam sweeping its arc, and the whole arc it can sweep is a fainter fan under it. Both are cut where a rock stands between the buoy and the plane, so a rock's **shadow** is a dark wedge in the light: a ship there can't be seen by that buoy, whatever the beam does.
- **What raises the alarm**: the lead ship inside a lit beam. The gauge under the vitals fills while a beam is on the ship ("SEEN") and drains a few seconds after it leaves; full, the picket goes up a stage. A buoy shot down gets its last call off: one stage. Taking the lock apart trips its own alarm: at least the hunter wing.
- **What each stage brings**, said on the gauge in words and shown in the world:
  1. **Amber.** Every buoy's lamp pulses amber and the beams sweep a third faster. Unseen for eight seconds, the picket goes quiet again.
  2. **Hunter wing.** A wing of Muster Lancers launches off the carrier's deck and hunts the ship. Break the wing, or stay out of every beam for twelve seconds with nothing awake near the ship, and the picket falls back to amber.
  3. **Carrier.** The carrier swings broadside, its running lights go red, a second wing launches, the flak gates in the wall wake, and its guns lay marked bursts round the lead ship every few seconds while it is in the field. The third stage holds until the party leaves the zone.
- **Slipping through** is shadow to shadow, waiting in a rock's shadow for the beam to swing away. **Fighting through** is shooting buoys and breaking wings and running the carrier's bursts. Both reach the lock; the quiet way reaches it with nothing awake.

Users: the party (Idra on the radio of their own ships: she gives the orders at the lock), the Muster's buoys, hunter wings and flak gates, the carrier (the Muster's picket ship, a hull on station). Nobody lives here.

## Rules of construction

- Everything flies at the flight plane (y 1). Backdrop is drawn below it and kept clear of what must stay readable by where it shows on screen (`ZoneKit.ClearOnScreen`).
- The lane runs straight north, 104 m wide (`FlightCorridor`, half 52). Its edges are rock teeth standing through the plane on both sides, so the stop is visible; the wall across it at z 640 is spires and two flak gates, with the lock in its one gap.
- **Every rock in the lane is a sensor shadow** (`SensorShade`, a circle at the plane as wide as the rock), solid to a hull, and drawn as rock made in code that fills that circle (`HarrowgateBuilder.Rock`, six builds, every face flat), tumbling slowly. What a beam is cut by is that circle. Its body stays the sphere it always was.
- **Every gap takes Idra's hull** (2.17 m): no two solid things in the lane are closer than 5.2 m, the lock's clean lane is 11 m between its posts, the quiet route keeps 2.8 m from every rock.
- The rule is drawn, not written: beams, their arcs and their shadows on the plane, buoy lamps (cold white, then amber), hunters launching from a carrier that is in frame from the field's east side, the carrier turning, marked bursts. The gauge's words name the stage and what it brings.

## Plan

North up; x east, z north.

```text
 z 852  ------------------------------ lane end ---------------------------------
 z 790        LAND - THE SCORIA FLOWS (-18)                LAND - HARROWGATE (22)
 z 762        spawn Scoria (-14)                           spawn Harrowgate (18)
 z 640  ##### spires #### FLAK GATE (-24) ### [ROUTE LOCK x -7..7] ### FLAK GATE (24) #### spires #####
 z 600  - - - - - - - - IdraLockReached crossing (vask.picketCrossed) - - - - spawn Lock (0, 594)
 z 570                        B4 on a tooth (0), sweeping south
 z 470                                                                    B3 (50), sweeping west
 z 360  B2 (-50), sweeping east                         carrier, 40 m down, under the east side (x 30, z 520)
 z 300  - - - - - - - - - - - - - - field begins - - - - - - - - - - - - - - - - - -
 z 200                                                                    B1 (50), sweeping west
 z 78   - - - - - - - - - IdraPicketEdge crossing (vask.picketRead) - - - - - - - - - -
 z 6    warp home (-14, -4)   ARRIVAL (0, 6)
```

| Stretch | z | What | Answer |
|---|---|---|---|
| The approach | 0-110 | Arrival, the way home, Idra's crossing at 78: the cone, the shadows, the three stages | |
| The first tooth | 110-290 | One buoy (B1) on the east edge sweeping the lane west, three rocks in it | Pocket to pocket behind the rocks as the beam swings north and south |
| The teeth | 300-600 | B2 on the west edge sweeping east and B3 on the east edge sweeping west, out of step; B4 on a tooth in the middle at 570 sweeping south; eight rocks | Pocket to pocket, waiting each beam out; round B4's west flank, which its arc never reaches |
| The wall | 600-640 | The lock in the one gap; flak gates either side; the carrier's deck under the east side | Take the lock apart in Idra's order between the flak curtain's volleys, with a hunter wing on you |
| Beyond | 640-852 | The way down to Scoria (and, once the party has a parley flag, to Harrowgate's quay) | |

## The buoys (built exactly so: the quiet route's test reads these)

Yaw is degrees from north toward east. A beam sweeps from one yaw to the other and back once a period, eased at each end; its half-width is the cone's half angle.

| Buoy | Where (x, z) | Sweeps (yaw) | Half angle | Reach | Period | Phase |
|---|---|---|---|---|---|---|
| B1 | (50, 200) | 222 to 318 | 11 | 108 | 10 s | 0 |
| B2 | (-50, 360) | 50 to 130 | 11 | 108 | 10 s | .35 |
| B3 | (50, 470) | 230 to 310 | 11 | 108 | 10 s | .85 |
| B4 | (0, 570) | 140 to 220 | 13 | 62 | 8 s | .15 |

At amber and above a beam sweeps 1.35 times as fast.

## The rocks that shade the quiet route

(x, z, radius at the plane). More rocks stand in the lane for its look; every one of them is a shadow too, and none comes within 2.8 m of the quiet route's legs.

(10, 172, 6), (-14, 196, 5.5), (8, 226, 6), (-14.1, 348.6, 5), (5.6, 370.4, 5), (-10.6, 392.6, 5), (-12.9, 428.7, 5), (7.8, 451.4, 5), (28.4, 472.9, 5), (9.2, 495.5, 5), (-18.2, 533.6, 5).

## The quiet route

Pockets (P) are hidden from every buoy at every angle of its sweep; the legs between them are flown at 10 m/s when the beams allow. Designed with a simulation of the sweeps (the numbers are each leg's window: how long, in each cycle, a ship can set off and never be lit) and flown by `HarrowgateWorldTests.TheQuietRouteRunsShadowToShadowWithAWindowOnEveryLeg`, which measures the same thing on the built buoys.

| Leg | From | Via | To | Window |
|---|---|---|---|---|
| 1 | (0, 110) | | P (2, 160) | 4.6 s of 10 |
| 2 | P (2, 160) | | P (-26, 190) | 2.9 s |
| 3 | P (-26, 190) | (-21.7, 200) | P (-6, 228) | 2.4 s |
| 4 | P (-6, 228) | (0, 300) | P (-22, 322) | 4.2 s |
| 5 | P (-22, 322) | | P (-6, 346) | 5.7 s |
| 6 | P (-6, 346) | (12.1, 365.4), (13.5, 368.1) | P (14, 372) | 2.3 s |
| 7 | P (14, 372) | | P (-4, 398) | 4.3 s |
| 8 | P (-4, 398) | | P (-20, 424) | 3.6 s |
| 9 | P (-20, 424) | (-21, 429.4), (-19.2, 433.9) | P (0, 448) | always |
| 10 | P (0, 448) | (-0.3, 452.6), (1.3, 456.4) | P (20, 474) | 3.4 s |
| 11 | P (20, 474) | (2.5, 490.8), (1, 495.1) | P (2, 500) | 5.1 s |
| 12 | P (2, 500) | | P (-22, 526) | 2.3 s |
| 13 | P (-22, 526) | (-24.7, 528.7), (-26, 531.3) | P (-36, 574) | 4.7 s |
| 14 | P (-36, 574) | | (0, 606) | always |

A straight run up the lane's middle at 12 m/s has no window: it is lit for certain.

## What happens along it

| Where | What | Flags, ids |
|---|---|---|
| (0, 6) | Arrival (spawn `Arrival`), `Warp — The Hull of Many Hands` (-14, 1, -4), open | arrives at Many Hands' hull, spawn `Vaskar` |
| z 78 | `IdraPicketEdge` on a crossing across the lane | `vask.picketRead` |
| the field | Buoys B1 to B4, each an encounter of one `SensorBuoy` on show and asleep until the lead comes within 15 m (`Buoy_1..4`) | `clear.Buoy_N`: its beam is dark for good, and the alarm went up a stage as it died |
| on the alarm | Hunter wings `Hunters_1..3`, launched in turn off the carrier's deck at (44, 1, 520): the first at stage 2, the next at stage 3 or the next time stage 2 is reached | `clear.Hunters_N` |
| z 604 | `IdraLockReached` on a crossing across the lane; also the main quest's step (report `PicketCrossed`); a checkpoint (spawn `Lock` at (0, 1, 594)) | `vask.picketCrossed` |
| the lock | Idra calls each piece (`hail.lock`), the lead answers as it comes off (`lock`); the four pieces in order: `Take the collar off` (port, (-5.5, 1, 633)), (starboard, (5.5, 1, 633)), (keel, (0, 1, 631)), then `Take the keeper bar out` ((0, 1, 634)); each saves at spawn `Lock`. The first trips the lock's own alarm (at least stage 2) and wakes the flak gates (`Lock_Flak`, needs `vask.lock1`) | `vask.lock1..4`, report `LockPiece` |
| the lock | The keeper bar out: the frame swings off the lane and the gap is open; `IdraLockOff` once nothing is awake near the lead (`AfterTalk`) | `vask.lockRemoved` |
| (-18, 1, 790) | `Land — The Scoria Flows`, locked on `vask.lockRemoved` | arrives at the Flows' `Arrival` |
| (22, 1, 790) | `Land — Harrowgate`, locked on `vask.parley` ("walk in under a parley flag first, by the road from the Unsworn camp") | arrives at Harrowgate's `Arrival` |
| (-14, 1, 762) | spawn `Scoria` (back from the Flows) | |
| (18, 1, 762) | spawn `Harrowgate` (back from the town's pad) | |

## Coordinates for routes

| Place | World (x, y, z) | Id / flag |
|---|---|---|
| Arrival | (0, 1, 6) | spawn `Arrival` |
| Warp home | (-14, 1, -4) | `Warp — The Hull of Many Hands` -> Many Hands' hull, spawn `Vaskar` |
| Idra's crossing | line across the lane at z 78 | `IdraPicketEdge`, `vask.picketRead` |
| B1, B2, B3, B4 | (50, 1, 200), (-50, 1, 360), (50, 1, 470), (0, 1, 570) | `Buoy_1..4` |
| Quiet route | the table above | |
| Carrier's deck (hunters launch) | (44, 1, 520) | `Hunters_1..3` |
| Lock crossing | line across the lane at z 604 | `IdraLockReached`, `vask.picketCrossed`, report `PicketCrossed` |
| Lock checkpoint | (0, 1, 594) | spawn `Lock` |
| Port collar | (-5.5, 1, 633) | `vask.lock1` |
| Starboard collar | (5.5, 1, 633) | `vask.lock2` |
| Keel collar | (0, 1, 631) | `vask.lock3` |
| Keeper bar | (0, 1, 634) | `vask.lock4`; then `vask.lockRemoved` (`IdraLockOff`) |
| The clean lane through the wall | x -5.5 to 5.5 at z 640 | open from `vask.lock4` |
| Flak gates | (-24, 1, 636), (24, 1, 636), just in front of the wall, facing south | `Lock_Flak` |
| From the Flows | (-14, 1, 762) | spawn `Scoria` |
| From Harrowgate | (18, 1, 762) | spawn `Harrowgate` |
| Down to the Flows | (-18, 1, 790) | `Land — The Scoria Flows`, needs `vask.lockRemoved` |
| Down to Harrowgate | (22, 1, 790) | `Land — Harrowgate`, needs `vask.parley` |

## Flight fights contract

*What the zone needs of the flight fights, written by the zone's engineer; the first combat engineer writes what is built and its numbers in the second column. Until it is written, each id below stands in as the existing enemy named (`HarrowgateBuilder.Foe`).*

| Thing | What the zone needs | As built (combat engineer) |
|---|---|---|
| `SensorBuoy` | A body that stands still where it is set (it never moves: the beam is the zone's, drawn from where it stands), on show and asleep from load (`previewPoint`), woken by the lead within 15 m. Shootable: Pulse is its weakness. The zone listens for its death (the encounter's `clear.Buoy_N`). Its own attack is the combat engineer's; it must not be a cone sweep of its own (the zone draws the sweep), and a quiet ship passing 16 m off must not wake it. Stand-in: none; until it exists the buoy is a drawn `SensorBuoy` prop and can't be shot | Anchored all round (`EnemyAnchored`, `arc` 360): never moves, nothing drags it, **no sweep of its own**. Its attack is a **flare inside 8 m**: a marked ring all round it for 1.0 s, then a Pulse burst of 108 (about a tenth of a Sync 22 bar) to every ship in the ring, every 3.2 s while one is there. Your 15 m wake is outside its ring, so a ship that wakes it and keeps off is never struck. Integrity 700, Pulse weak, Beam resists (every enemy resists something: the shared test), capsule r 1; XP 20. Its death raises `EnemyAnchored.Freed` as well as clearing its encounter |
| `MusterLancer` | The hunter wing: launched by the zone (`EncounterVolume.Begin`) at (44, 1, 520), two to a wing for the first two wings, three for the third. Hunts the lead ship from there. **To be lost in shadow**: a Lancer that has not been able to see the ship it hunts for 2 s (`SensorShade.Blocks(lancer, ship)`, or the ship more than 40 m off) should break off and circle where it is; the zone counts the wing lost when the lead has been unseen for 12 s with nothing awake within 25 m of it. Check the body's facing: the intake turned it -90 so the lance should lead. Stand-in: `MusterSkiff` | A strafer that **comes about** and **hunts** (`EnemyStrafer`, `tune.pace`, `tune.linger`). It circles the lead at its 16 m run-in, marks a lane **26 m by 2.6 m for 1.0 s**, runs it at 26 m/s (114 Kinetic to what it touches in the lane), then comes about through a slow half turn for 1.8 s, open (a blow lands at half again). **Lost in shadow as asked**: with the ship it hunts unseen for 2 s (`SensorShade.Blocks(lancer, ship)`, or past 40 m) it breaks off and circles 8 m round where it lost him (`STRAFER_LOST`), is not walked home, and takes him up again the moment it sees him (`STRAFER_FOUND`). When one dies every strafer within 24 m scatters. Facing: `HarrowgateCombatTests.TheLancerIsDrawnLongAlongItsRunWithItsLanceLeading`. Integrity 950; XP 60 |
| `FlakGate` | Two, just in front of the wall at (-24, 1, 636) and (24, 1, 636), set facing south (yaw 180, `faceAsPlaced`), so their curtains fill the lock's approach. Asleep until the first lock piece comes off (`Lock_Flak`, `requiredFlag vask.lock1`) or the carrier turns. Its curtain fires across the lock's approach (z 618 to 636) in a timed pattern with gaps a player can work a piece in: each piece's prompt is 6.5 m round its point (above). Kinetic weak, Beam resists. Stand-in: none; until it exists the gates are drawn `FlakGate` props and the curtain doesn't fire | Anchored with a **curtain** (`EnemyAnchored.Curtain.cs`). From its own line, **three rows across the way it faces, 8 m deep each (24 m in all), each 52 m long and centred on it**. Set at z 636 facing south: row 0 is z 636 to 628 (all four lock pieces stand in it), row 1 628 to 620, row 2 620 to 612; the west gate's rows run x -50 to 2, the east's -2 to 50. While any ship is within 40 m: beat 0 marks rows 0 and 2 for 1.0 s and bursts (110 Kinetic to each ship in a marked row, once a volley however many gates' rows it is in), beat 1 marks row 1, a beat every 3.2 s. So row 0, where the lock is worked, is quiet **5.4 s** between bursts and marked 1 s before each. Two gates woken together (`Lock_Flak`) keep one clock. A row never follows a ship. Integrity 2,400, capsule r 2; XP 80. Logs `CURTAIN_MARKED`, `CURTAIN_BURST` |
| Damage | Authored at its real weight at a zone threat of 1, 9 to 13% of a hero's bar at Sync 22 in the parts the party has (DevLoadout chapter8) | Every ordinary hit of the chapter is 108 to 116: 12.0 to 12.9% of Taren's or Sela's bar at 22, 9.1 to 9.7% of Idra's at 25, measured on all three through `Health.Receive` (`HarrowgateCombatTests.EveryOrdinaryHitTakesANinthToAnEighthOfAnyHerosBarAndVessasAnElitesShare`). The returning skiff is `ReachSkiff` ("Muster Skiff", chapter 2's shooter at this weight); chapter 2's `MusterSkiff` is tuned for Sync 9: use `ReachSkiff` in any wing that mixes one in |
| The carrier's bursts | The zone's (not an enemy): `PicketAlarm` marks a 4.5 m circle (`Telegraph.Threat`) round where the lead will be in 1.4 s, every 3.2 s at stage 3 while the lead is between z 260 and 660, and strikes inside it on landing for 10% of a hero's Integrity (the place's damage, no source) | |
| Barks | `picket.amber`, `picket.hunters`, `picket.carrier` (the lead, as each stage comes), `shadow` (into a shadow with a beam swinging toward it), `lancer` (a wing launches), `flak` (the flak gates wake), `lock`, `hail.lock` (the zone's) | Nothing of the combat side barks in the Picket: every line in this row is the zone's to play |

## What is in frame

The flight frame is about 45 m wide and 25 m deep at the plane (LookDev at the flight profile).

- **The beams and their shadows**: the live beam a pale cold-blue sweep with brighter edges; under it, fainter, the whole fan the buoy can sweep, both cut short behind each rock, so the shadows are dark rounded lanes running away from the buoy. Additive: where two beams cross it is brighter, and nothing under one is hidden. Never orange, red or purple at any stage (D217): the stage is the lamps' and the gauge's.
- **The rocks** (November 14, the second look): jagged basalt made in code (`HarrowgateBuilder.PicketLook`), in six builds (a lump, a lump with spurs, a split boulder, a slab, a lump with a blade through it, a heap), toned in their vertices, each a solid circle and a shadow; more under the lane as debris, 4 to 30 m down, with a few dead hulls and, under the approach, dead buoys and two wrecks. The teeth along both edges and across the wall are fangs in five builds (a lone fang, a pair, a crown, a broken stump with a shard against it, a leaning blade) with rubble between them at the plane. The generated ridge rocks they replace read from above as squared blocks.
- **The buoys**: on a stub of rock each, a lamp and a halo the flight camera reads from its height.
- **The carrier**: a long hull 38 m under the field's east side; its running lights go red when it turns.
- **The wall**: spires either side of the lock, kept low enough that the camera sees a ship just past them; the lock's frame across the clean lane between two anchor rocks.
- **Scoria** (November 14): a ball 96 m in radius 420 m down, west of the lane, its middle in mid-frame with the lead ship at z 450: in frame from the first tooth to the wall, crust, obsidian lowlands, pale ash and thin dull-red melt. No deeper, because the game's camera draws nothing past 1000 m (LookDev draws to 1500, so a still can show a planet a player never sees). Its moon, 30 m, 300 m down east of the lane, is in the approach's frames; faint bands of ash lie under the whole lane.

## Evidence

Built in the editor (`HarrowgateBuilder.BuildZones -harrowgate-zones PicketOfTeeth`, then `ZoneLook.Apply`) and looked at through the flight camera (`LookDev.Render`, 20 views: `HarrowgateBuilder.Picket.Views`). The editor stills show each beam where it stands at the zone's first moment and its whole fan (`SensorSweep.Still`); in play each buoy draws its own every frame. `PropTint` and the lamps' colours apply in play only, so the rocks show the ridge rock's own brown in these stills.

| Pass | What was wrong | What changed |
|---|---|---|
| `harrow-picket-01` | Scoria, a 150 m ball of bright orange melt in big regular cells, filled every frame from the field on (the flight camera's top edge looks hundreds of metres north at that depth) and was the colour of a danger marking. The beams were flat grey sheets that washed whole frames grey. The rocks were tinted warm and read as one brown. The approach's frames were bare stars | Scoria is 70 m, set far ahead, basalt with thin deep-red cracks, and shows only at the top of the frames from the wall on. The beams are cold blue, fainter, with bright edges (a texture across the fan), and stay cold at every stage. Rocks are basalt and iron. Debris is three times as dense, and dead buoys drift under the approach |
| `harrow-picket-02` | Every rock was one of three ridge rocks: the field read as one block turned many ways | The asteroids and the debris draw from six pieces (the ridge rocks, a strata outcrop, a boulder heap, rubble) |
| `harrow-picket-03` | | Read as a place: in every still a rock or debris and the lane's light; in the wide stills of the first tooth and the field the fan and its dark shadow lanes behind each rock read at a glance |
| `harrow-picket-04` (the last) | | As 03; the way home's label now names its zone ("Warp — The Hull of Many Hands") |

What is weak, plainly:

- **The ridge rocks are stratified blocks**: from above they read as squared masonry more than as tumbling asteroids. They are generated, textured and tumble, and they are the only rocks in the kit; a rounder asteroid piece would read better.
- **The rule's stages are seen only in play**: the amber lamps, the hunter wing launching, the carrier turning, the bursts and the gauge are all runtime. They are tested (`HarrowgateWorldTests`), not looked at in a player.
- **Flown only by tests**: the quiet route is held by a measurement of its windows on the built buoys, and a ship is held in pockets and in beams; nobody has flown it whole in a player.
- **The carrier** is the Hauler's hull at ninety metres under a tint; it is a big shape under the east side, not a carrier of its own make.
