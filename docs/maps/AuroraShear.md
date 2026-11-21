# The Aurora Shear - the lane down through Hoarfell's ice ring

2026-11-12. Builder: `HoarfellBuilder.AuroraShear.cs` on `ZoneKit` (world engineer A, chapter 6). Chapter brief: [ch06-the-long-sleep.md](../chapters/ch06-the-long-sleep.md). Script wiring: [ch06-manifest.md](../writing/ch06-manifest.md). **MAP_EYE_TEST: blockout written before construction (this page); editor stills and what is still weak are under Evidence.**

## What the place is for

Hoarfell's magnetic storms pull curtains of aurora through a shattered ring of ice, and where the storm runs along the ring it runs as currents: ribbons of light a ship can ride. The convoy's lane to Lastlight comes down through the ring. The zone's rule is **riding the currents**: a ship flying with a current is carried, faster than its engines alone, and its Thrust fills; a ship flying against one crawls and its Thrust drains. The lane is a sequence of currents to hop between, with ice shards as cover and Muster tenders and skiffs hauling thaw-rig parts along the fast lane. Idra Sorn is met here as a voice and a hazard-striped cutter that tows the party out of the first current as it turns.

Users: the party, Idra (a voice and a cutter), Muster tenders and skiffs, and Idra's third cutter lying wrecked under an ice shelf with two of her crew in its pod.

The Muster on the lanes (chapter 6's roster, `HoarfellBuilder.Enemies.cs`): four `MusterTender` (a slow hovering lobber whose charge burns on a 3.2 m circle for four seconds) and eight `CurrentSkiff` (a strafer that marks an 18 m lane for .8 s and runs it at 22 m/s), in five fights, each in a current 18 to 22 m wide with no ice in it, so there is room to fly out of a tender's circle toward it and out of a skiff's lane.

**A fight in a current is a running fight down the lane** (November 14, after the first player run). A current carries everything at the plane inside it as it carries the ships (`AuroraCurrent.Float`): the Muster's craft while they fight the party (within 35 m of the lead, where an enemy gives up and goes home anyway), the circles and lanes they mark, the burning melt a tender's charge leaves, and every shot in the air. So between the bodies in it a fight plays as it would in still space: a tender's circle comes down on the ship it was thrown at, a skiff's lane runs through the ship it was laid through, the party's shots go where they were aimed. What the current changes is where the fight is going (on down the lane, toward the next fight's box and the lane's end) and what riding it does to a hull: with it, Thrust for the rolls the fight asks for; against it, a crawl, and the fight carried on without you. A body that has lost the party is not carried: it holds and goes home. What a hero's skill lays in the world (a Gravity Fold's well, a waypoint charge) stays where it is laid. Before this only the ships rode: every tell landed where a ship had been, two runs took no blow from any fight, and a fight begun in the fast lane was left a hundred metres behind in ten seconds.

What a player has to do in each fight:

| Fight | Where | What it is | What a player does |
|---|---|---|---|
| `Shear_Tender` | the short lane at 560 (push 7) | a tender | the tender holds 6 to 12 m off and lobs onto a marked 2.6 m circle on the ship, 1.3 s in the air: fly out of the circle (toward it), keep clear of the 3.2 m of melt it leaves burning beside you as both ride, close and break it. Carried toward the short lane's foot (720), still water |
| `Lane_0` | Idra's lane at 600 (push 6) | two skiffs | each circles about 10 m off, marks an 18 m lane through the ship for .8 s and runs it: step out of the lane sideways as the paint fills, hit it as it breaks off. A kill scatters the other for a beat |
| `Fast_0`, `Fast_1` | the fast lane at 860 and 1080 (push 9) | a tender and two skiffs | both answers at once, riding at 9 m/s: a circle and lanes, the melt riding beside you. A skiff broken out of the current falls behind and comes back. A fight still going at 1062 carries the party into `Fast_1`'s box, and both wings are fought together |
| `Descent_0` | the descent at 1360 (push 7) | a tender and two skiffs | as the fast lane's; the descent ends at 1438, short of the way down, so the fight ends in still water there at the latest |

## Rules of construction

- Everything flies at the flight plane (y 1). What is drawn below is backdrop, kept clear of what must be read by where it shows on screen (`ZoneKit.ClearOnScreen`, `ShearKeep`).
- The lane bends; every place is so many metres along it and so many to starboard (`HoarfellBuilder.ShearBeside`), walled 72 m either side by `FlightCorridor`, each stretch's walls running 15 m past its ends (`CorridorOverrun`). Run 72 m past (the half width, `FlightCorridor`'s default), the starboard wall of the stretch after the right-hand bend at 560 reached back across Idra's lane from (26.5, 475.3); 15 m still closes the outside of every bend (the sharpest, 21 degrees at 560, needs 13.2).
- **The rule is drawn, not written** (`S/World/AuroraCurrent.cs`): each current is a ribbon of light at the plane whose chevrons run the way it flows, with streaks of light carried along it; when a current turns round its chevrons slow, stop and run back. The HUD's current gauge (`S/UI/CurrentGauge.cs`) fills right in teal while the lead ship rides with a current and left in amber against one; the Thrust bar fills or drains with it.
- A current is 18 to 22 m wide: most of a flight frame's 25 m, so the frame always shows which way it runs. Nothing solid stands in a current; the ice shards that are cover stand between them.
- No backdrop is a stretched texture: the ring's ice, the shelf and the shards are built geometry with their colour in their vertices; the planet's face is painted denser than the screen's pixels at its depth.

## Plan

North up. The lane runs about 1,510 m from the arrival (south) to the way down (north). "P" is port, "S" starboard. Currents are named C1 to C6 and flow north (with travel) unless marked.

```text
                         LAND - LASTLIGHT 1470, 20 P  (the ring's gap over Hoarfell; locked until Idra has hailed)
                       spawn Lastlight 1440
                C6 the descent  1310 -> 1438
       C4 THE FAST LANE 760 -> 1320 (push 9)         C5 the counter-current, flowing SOUTH, 840-1240 at 40-52 S
            Fast_1: a tender and skiffs 1080                 the downed cutter under the ice shelf 1020, 60 S (CutterDown)
            Fast_0: a tender and skiffs 860
            hail.fastlane 760
   C3 the short lane 360 -> 720, 0-12 P          C2 IDRA'S LANE 250 -> 745, swinging out to 50 S
     IdraShearLanes 520 (across the whole lane)    hail.tender 480
     a tender on C3 560
                       the tow 215 -> 320: C1 to C2
     IDRA'S HAIL 200 (line across the lane)    her cutter waits 215, 16 S
   C1 the first current 26 -> 340, 10-12 P  (turns SOUTH once she has hailed)
 warp home  ARRIVAL 0
```

## What happens along it

| Along | What | Flags, ids |
|---|---|---|
| 0 | Arrival (spawn `Arrival`); `Warp — The Hull of Many Hands` (open) | arrives at Many Hands' hull, spawn `Hoarfell` (staged by `HoarfellBuilder.StageApproach`) |
| 26-340 | **C1, the first current**, 10 to 12 m to port, flowing north, beginning just past the arrival (so a party coming back once it has turned doesn't arrive inside it) | `turnFlag` `hoar.idraHailed` |
| 200 | **Idra's hail** (`IdraShearHail`): a line across the whole lane. As it closes, C1 turns round and her cutter puts a line on the lead ship and tows the party east across to C2 (`S/World/CutterTow.cs`) | `hoar.idraHailed` (report `CurrentTurns` for the step's arrow); `hoar.towed` when the line lets go |
| 215, 16 S | Idra's striped cutter, waiting (the `IdraFlight` body, drawn only) | after the tow it leads along her lane, waiting for the lead ship at each point, and waits at the way down |
| 250-745 | **C2, Idra's lane**, swinging out to 50 m to starboard and back (at 56 its edge met the next stretch's wall at the bend) | |
| 360-720 | **C3, the short lane**, straight up the middle to port, past a tender | |
| 480 | `hail.tender` (Idra on the radio) | |
| 520 | `IdraShearLanes`: a line across the whole lane, both currents (compulsory: whichever lane the party took, Idra calls the two lanes here) | `hoar.tenderSeen` |
| 560, 4 P | A Muster tender hauling on C3 (`Shear_Tender`) | its wake burns |
| 640, 50 P | A Muster haul cache, off both lanes | `cache.hoar.haul` |
| 760 | `hail.fastlane` | |
| 760-1320 | **C4, the fast lane**, 22 m wide, push 9 | |
| 860 | A tender and two skiffs riding the fast lane (`Fast_0`) | |
| 840-1240, 40-52 S | **C5, the counter-current**, flowing south: the wrong lane, between the fast lane and the shelf | |
| 1020, 60 S | **Idra's downed cutter** under the ice shelf: `Tow the crew pod` (`IdraCutterFound`), offered while `CutterDown` is at step 0 | `hoar.podFound` (report `CrewPod`) |
| 1080 | A tender and two skiffs on the fast lane (`Fast_1`) | |
| 1360, 14 P | A tender and two skiffs on the descent (`Descent_0`) | |
| 1310-1438 | **C6, the descent**, ending short of the way down | |
| 1440, 12 P | spawn `Lastlight` (back from town) | |
| 1470, 20 P | **`Land — Lastlight`**, locked on `hoar.idraHailed` ("Follow the striped cutter: she knows the way down through the ring") | arrives at Lastlight's `Arrival` |

## What is in frame

The flight frame is about 25 m wide and 19 m deep at the plane.

- **The currents**: in nearly every frame of the lane a ribbon of green-to-teal light (C5, the counter-current, violet) with chevrons and streaks running along it. The ribbon is the subject of the frame and says the rule.
- **The ring**: ice shards at the plane between the currents (cover, solid: three to six a stretch, several silhouettes: slabs, blades, clusters), and the ring's broken band under the lane from 380 to 980 at 15 to 90 m down: thousands of ice pieces, blue-white, faceted.
- **The aurora**: curtains hanging from just under the plane far down into the dark, green and violet, behind everything.
- **Hoarfell ahead**: the planet 760 m down and beyond the way down, its ice cap and storm bands. The flight camera sees that deep a long way ahead, so it stands at the top of the frame the whole way down the lane, small at first and nearer at the end: where the lane is going. At 300 m across and 560 down (the first build) it filled the frame behind every current and read as a pale wall.
- **The subject of each stretch**: a current, the cutter, a tender, the shelf and the wreck, the ring's gap at the way down.

## Coordinates for routes

World x, y, z from the build's `HOARFELL_COORDS AuroraShear` line, which the builder prints from the same constants (`ShearLane`, `ShearBeside`). The lane points are (0, 0), (20, 260), (-30, 560), (30, 860), (0, 1160), (-20, 1420), (-20, 1500).

| Place | Along, side | World (x, y, z) | Id / flag |
|---|---|---|---|
| Arrival | 0 | (0, 1, 6) | spawn `Arrival` |
| Warp home | | (-14, 1, -4) | `Warp — The Hull of Many Hands` -> spawn `Hoarfell` |
| C1, the first current | 26 -> 340, 10-12 P | (-8.0, 1, 26.7) > (-0.8, 1, 120.4) > (5.7, 1, 230.2) > (-2.9, 1, 336.5); half 9, push 6 | turns on `hoar.idraHailed` |
| Idra's hail line (across the lane) | 200, 0 | (15.3, 1, 199.4) | `IdraShearHail`, `hoar.idraHailed` (report `CurrentTurns`) |
| Cutter waiting | 215, 16 S | (32.4, 1, 213.1) | |
| Tow ends (in C2) | 320, 34 S | (43.8, 1, 324.0) | `hoar.towed` |
| C2, Idra's lane | 250 -> 745 | (53.1, 1, 246.7) > (49.7, 1, 385.8) > (26.7, 1, 523.9) > (24.0, 1, 625.8) > (15.1, 1, 734.6); half 9, push 6 | |
| C3, the short lane | 350 -> 720 | (-6.5, 1, 346.0) > (-22.0, 1, 475.3) > (-27.0, 1, 595.2) > (0.4, 1, 712.1); half 9, push 7 | |
| Idra's lanes line (across the whole lane) | 520, 0 | (-22.6, 1, 515.7), 148 wide | `IdraShearLanes`, `hoar.tenderSeen` |
| Tender on the short lane | 560, 4 P | (-33.1, 1, 554.5) | `Shear_Tender` |
| Skiffs on Idra's lane | 600, 44 S | (20.0, 1, 585.8) | `Lane_0` |
| Haul cache | 640, 50 P | (-64.3, 1, 643.4) | `cache.hoar.haul` |
| C4, the fast lane | 750 -> 1320 | (8.3, 1, 741.1) > (35.1, 1, 889.8) > (17.1, 1, 1028.7) > (-4.6, 1, 1167.3) > (-21.3, 1, 1306.5); half 11, push 9 | |
| Fast lane tender and skiffs | 860, 4 S | (31.8, 1, 848.6) | `Fast_0` |
| C5, the counter-current (flows south) | 1240 -> 840, 40-52 S | (42.7, 1, 1231.1) > (58.9, 1, 1093.2) > (64.9, 1, 953.1) > (63.2, 1, 821.9); half 8, push 6 | |
| Downed cutter | 1020, 60 S | (74.9, 1, 1014.4) | `Tow the crew pod`, `hoar.podFound` (report `CrewPod`, `CutterDown` step 0) |
| Fast lane tender and skiffs | 1080, 2 S | (11.2, 1, 1068.3) | `Fast_1` |
| Descent tender and skiffs | 1360, 14 P | (-28.4, 1, 1346.0) | `Descent_0` |
| C6, the descent | 1310 -> 1438 | (-20.5, 1, 1296.5) > (-30.9, 1, 1366.0) > (-38.0, 1, 1424.9); half 10, push 7. It ends 30 m short of the way down, so a hull reaches the warp from any side | |
| Back from town | 1440, 12 P | (-32.0, 1, 1426.9) | spawn `Lastlight` |
| **The way down** | 1470, 20 P | (-40.0, 1, 1456.9) | `Land — Lastlight` -> Lastlight `Arrival`; locked on `hoar.idraHailed` |

A route rides C1 north from the arrival, crosses the hail line at (15.3, 1, 199.4) (the tow then carries it to (43.8, 1, 324.0) by itself: give that step 25 s and `stopWhen` `flag:hoar.towed`), rides C2 or cuts west to C3, joins C4 at about (8, 1, 741) and rides it to C6 and the way down. Flying against a current, a route crawls: at push 9 (the fast lane) a ship at the stick's own pace is carried backward, and must boost or cross out of it. **So after a fight in a current a route never flies back up the lane**: the fight has carried it on, how far depends on how the fight went, and the next leg is to a point no fight can have been carried past: the fast lane at 790 after the short lane's tender, 14 m short of the way down after the descent's, and **into the next lane fight's own volume** after each of the fast lane's fights (`Checked.ride_into`: `Fast_1` at (18.3, 1, 1057.0), `Descent_0` at (-15.5, 1, 1335.0), each with `stopWhen` `flag:clear.<id>` and 45 s). A leg into a fight a current carried the last one into, and already cleared, ends at once. Nothing carries a party that a chase has left outside the current: the lead's first player run of the route had no leg between the fast lane's fights, and sat 12 m to starboard of the current's middle (a metre out of it) through both fights after the first, locking at nothing (November 14). The generator refuses a lane-fight lock pressed from wherever the last fight ended, and checks every leg after a lane fight from each place within 12 m of the current's middle from the fight's box to where the next leg goes (`ENDS_WITHIN`), not from its end only. **Further out than that a straight leg can meet a shard**: from 15 m off, the shard at (7.6, 955.7) stands in the way into `Fast_1` (and at 18 m, the ones at (9.8, 869.6), (-21.9, 1123.5) and (-0.5, 615.5)); the replay driver doesn't steer round shards, and no fixed point clears a party that stopped right behind one (`hoarfell_routes.py`, `shear()`).

**Keep clear: Idra's line.** No shard stands within its size and 8.2 m (`ShearZone.TowRoom`: the partner held 4 m to starboard of the lead, a hull up to 2.2 m, 2 to spare) of the way her line takes the ships: the first current's width across her hail (-20 to 0 starboard at 200), from each end of it to her cutter's wait, and her tow (`ShearZone.FromTow`; the route generator's `from_tow`). As built on November 12 no shard stood there, so the rule drops nothing. The first player run's scrapes on the shard at (19.1, 255.9) came from the currents carrying the party 160 m on through her call (fixed by D275), so the tow had to drag both hulls back south to reach her line.



## Evidence

Built six times in the editor (`HoarfellBuilder.BuildZones -hoarfell-zones AuroraShear`, then `ZoneLook.Apply`), each followed by stills through the flight camera (`LookDev.Render`, 19 views: the named places, the lane between them, two wide). The floor measure doesn't apply to a sky; each still was looked at.

| Pass | What was wrong | What changed |
|---|---|---|
| `hoar-01` | Hoarfell (300 m across, 560 m down) filled the frame behind every current from a hundred metres along and read as a pale wall; its sea-ice leads were drawn as dark lines and read as a map's contours. The ice shelf was the ring's ice laid over forty metres: a stamped pattern of bubbles. The crew pod wore the hazard stripes all over and read as a beach ball | Hoarfell is 170 m across and 760 m down, beyond the way down; the leads are faint veins. The shelf has its own quiet ice (broad drifts of tone, a few long cracks); the pod is plain plating lit by Kott's lamp |
| route writer, November 12 | An invisible wall stood across C2 at its widest, from (26.5, 475.3) north (the next stretch's starboard wall, run 72 m back past the bend at 560; found from the builder's numbers, not flown); `IdraShearLanes` spanned only C3, though compulsory, so a party that kept to Idra's lane never heard it | Each stretch's walls run 15 m past its ends; C2 swings out to 50 m, not 56 (11.3 m clear of every wall), and `Lane_0` sits on it at 44; Idra's lanes line spans the lane |
| `hoar-06` | | Read as a place: in every still a current's ribbon with its chevrons running the way it flows (the rule, from the screen), crystals of the ring between the currents and its broken band below, curtains of aurora in the dark, Hoarfell small at the top of the frame ahead, the cutter, the wreck in the shelf's lip, the ring of shards and lamps round the way down |

What is weak, plainly:

- **The currents move only in play.** `AuroraCurrent` scrolls the ribbons' chevrons and carries the streaks at runtime; a still shows them standing. That a current carries a ship, fills its Thrust with it and drains it against it is tested (`HoarfellWorldTests.ACurrentCarriesAShipWithItAndFillsItsThrustAndHoldsItBackAndDrainsItAgainstIt`), not seen.
- **The tow and the cutter that leads** (`CutterTow`) are tested (`IdrasHailTurnsTheFirstCurrentAndHerCutterTowsThePartyIntoHerLane`) and never seen moving; the tow line is a plain orange line.
- **Enemies appear only in play** (`Shear_Tender`, `Lane_0`, `Fast_0`, `Fast_1` are the combat engineer's tenders and skiffs, with stand-ins until those land); nobody has flown the lane's fights.
- **Against the fast lane at the stick's own pace a ship is carried backward** (push 9 against a top speed held to .7 of 12). That is the rule at its hardest. Since fights ride the currents (November 14) nobody has to hold against a lane to fight in it; a ship that does is left by the fight.
- **The running fights are written and type-checked, not run** (Unity's licence lapsed the day they were written). `HoarfellCombatTests.AShearFightRidesTheCurrentWithTheShipAndHitsOneThatDoesNothingButNotOneThatAnswersItsTells` measures them; a skiff that breaks away out of a current falls behind until it is back in one, which that test's 35 m bound is there to catch.
- The ribbons are bright and broad; with the ships small at the flight camera's height they may read as the thing to look at more than the ships are.

