# The Spore Veil - Driftreed's beacon lane

2026-11-12. Builder: `WendmireBuilder.SporeVeil.cs` on `ZoneKit` (world engineer A, chapter 4). Chapter brief: [ch04-breathing-room.md](../chapters/ch04-breathing-room.md). Script wiring: [ch04-manifest.md](../writing/ch04-manifest.md). **MAP_EYE_TEST: blockout written before construction (this page); editor stills and the in-player pass are under Evidence.**

## What the place is for

A bioluminescent spore nebula hung over Wendmire and its three moons, on the only way down to the mire that Driftreed's pilots keep: a lane of their pulse beacons, each blinking the town's own count, two short and one long. The zone's rule is **visibility is the resource**. Spores come to light: every shot, every skill and a held boost raise the ship's glow, a bright ship draws the swarms hanging in the lane from thirty metres off and is mobbed by them, and a ship that flies dark for two seconds sheds whatever is riding it. Maru Wend, who set the beacons, is on the radio the whole way and rude about ship lights.

Users: Maru (a voice), Driftreed's beacons, spore swarms and drifting spore pods, one Veil Ray on a circuit round the dead beacon, and whoever is coming down to Driftreed by the tide-gate.

## Rules of construction

- Everything flies at the flight plane (y 1). What is drawn below is backdrop and never an obstacle, kept clear of what must be read by where it shows on screen (`ZoneKit.ClearOnScreen`, `VeilKeep`).
- The lane bends, so every place is so many metres along it and so many to starboard (`WendmireBuilder.VeilBeside`); it is walled 72 m either side by `FlightCorridor`, wide enough for the two off-lane pockets to be inside the walls and well off the beacons' line.
- **No backdrop is a stretched texture.** The chapter 3 lane's planet was one 1024 px map over a kilometre and read soft at the flight camera. Here every backdrop piece is built geometry with its colour in its vertices (spore pods, mats, strands, the moons' faces, the mire under the gate), or a painted texture only where its texels are denser than the screen's pixels at that depth (a moon 420 m down needs about five texels a metre; it has more).
- The rule is drawn, not written: the glow is a light and a halo on the hull and a gauge under the vitals (`ShipGlow`, `GlowGauge`); the swarms of leg two hang in the lane on show and asleep, so a player sees the clouds before they wake; the beacons blink the count Maru teaches (`LampRhythm`); the slipped beacons blink it wrong.

## Plan

North up. The lane runs about 1,560 m from the arrival (south) to the tide-gate (north). "P" is port (left of travel), "S" starboard.

```text
                       TIDE-GATE 1560  (ring of pulse beacons on the mire's surface, 34 m down; land - Driftreed,
                            .           locked until the dead beacon is relit)
                     spawn Driftreed 1530
                  DEAD BEACON 1380 ......... Veil Ray's loop beside it (leg three)
                 hail: the ray 1020         pocket B 1150, 58 S: off-lane beacon 2, cache
          BEACON 3  1000
                .          swarm field 590-950: nine swarms on show, asleep, three rows of three across the lane
        hail: swarms 540
     BEACON 2  520
         .          pocket A 300, 56 P: off-lane beacon 1 (short, long, short), cache; Maru's stray call at its mouth
     pods 150, 260, 400 (leg one: clusters to thread)
   hail: pods 120
   Maru's hail 52
  BEACON 1  40
 warp home  ARRIVAL 0
```

## What happens along it

| Along | What | Flags, ids |
|---|---|---|
| 0 | Arrival (spawn `Arrival`), `Warp — Tallow Approach` (open) | arrives at Tallow Approach's spawn `Wendmire` |
| 40 | Beacon 1: two short, one long | crossing sets `wend.beacon1` |
| 52 | Maru's hail (`MaruVeilHail`, a line across the lane) | `wend.maruHailed` |
| 120 | `hail.pods` (radio) | leg one |
| 150, 260, 400 | Spore pods in clusters across the lane (`Pods_0..2`, on show) | shot or rammed, a pod bursts into a blinding cloud |
| 300, 30 P | Maru's stray call at the mouth of pocket A (`MaruVeilStray`, then `MaruBeaconsAsk`: starts `BeaconKeeper`) | `wend.strayed`; hidden once `maru.comesHeard` |
| 300, 56 P | Pocket A: off-lane beacon 1 (short, long, short until retuned), a cache | `Retune the beacon` (BeaconKeeper step 0, report `OffLaneBeacon`, `wend.offBeacon1`), `cache.wend.pocketA` |
| 460 | Two swarms, the first to meet (`Lane_0`) | |
| 520 | Beacon 2 | `wend.beacon2` |
| 540 | `hail.swarm` | leg two |
| 590-950 | **The swarm field**: nine swarms on show and asleep in three rows of three across the lane (`Field_0..8`, rows at 615, 760 and 905). In a row they stand 48 m apart and the outermost 20 to 28 m in from the edge: no line across a row is more than 28 m from a swarm, and between two swarms there is 36 m of open lane. A dark ship wakes one only by flying into it; a lit lead ship wakes every one within 30 m (the partner's own catch-up boosts don't count) | the zone's rule |
| 1000 | Beacon 3 | `wend.beacon3` |
| 1020 | `hail.ray` | leg three |
| 1150, 58 S | Pocket B: off-lane beacon 2, a cache | `wend.offBeacon2`, `cache.wend.pocketB` |
| 1360-1400 | The Veil Ray's circuit, a loop of about 22 m set 20 m to starboard of the dead beacon, so its near pass goes by the lamp and its far pass is 40 m off (`Veil_Ray`) | relight on its far pass, or fight it |
| 1380 | **The dead beacon**: `Relight the dark beacon` (`MaruDeadBeacon`) | sets `wend.beaconRelit`; `hail.relit`, `hail.gate` follow on the radio |
| 1530 | spawn `Driftreed` (back from town) | |
| 1560 | **The tide-gate**: `Land — Driftreed`, locked on `wend.beaconRelit` | arrives at Driftreed's `Arrival` |

## What is in frame

The flight frame is about 25 m wide and 19 m deep at the plane. Nothing of the lane may be bare sky for more than a moment:

- **The drift**: ribbed spore pods in four builds and six colourings, lit a little from inside, with a glowing tip or band where the spores come out; colonies of lace in stacked shelves with rolled, beaded rims; and trailing strands strung with spores, 4 to 13 m below the plane the whole length of the lane, piled toward its middle and thickest round the beacons, the swarm field and the dead beacon. Built as one mesh to each 170 m stretch.
- **The nebula**: soft bands of green and violet far under the lane (additive strands; they are haze on purpose, never the subject).
- **The three moons**: one large moon under leg one (420 m down), a small one under leg two, the third under leg three, each a dome with a crisp painted face and its own light, dim enough that ships and creatures stay the brightest things.
- **Wendmire**: under the last 200 m the mire's own surface, 34 m down: dark water, reed islands, mangrove crowns and the ring of the tide-gate's lamps.
- **The subject of each stretch**: a beacon (every leg starts and ends on one), a cluster of pods, a hanging swarm, the ray's circuit, the gate.

## Coordinates for routes

World x, y, z from the build's `WENDMIRE_COORDS SporeVeil` line (November 12). The lane is 1,673 m from the arrival to its far wall; the gate stands 1,560 m along it.

| Place | Along, side | World (x, y, z) | Id / flag |
|---|---|---|---|
| Arrival | 0 | (0, 1, 6) | spawn `Arrival` |
| Warp home | | (-14, 1, -4) | `Warp — Tallow Approach` -> spawn `Wendmire` |
| Beacon 1 (and its crossing) | 40, 12 P | (-17.9, 1, 37.7); the line spans the lane at 40 | `wend.beacon1` |
| Maru's hail line | 52, 0 | (-7.9, 1, 51.4) | `MaruVeilHail`, `wend.maruHailed` |
| Pod banks | 150 / 260 / 400 | (-22.8, 1, 148.3) / (-39.5, 1, 257.0) / (-8.9, 1, 393.4) | `Pods_0..2` |
| Maru's stray call (pocket A's mouth) | 300, 32 P | (-62.8, 1, 303.2) | `MaruVeilStray`, `wend.strayed` |
| Pocket A: off-lane beacon 1, cache | 300, 56 P | (-86.1, 1, 308.7); cache 6 m on | `wend.offBeacon1` (report `OffLaneBeacon`), `cache.wend.pocketA` |
| First swarms | 460, 0 | (4.8, 1, 451.8) | `Lane_0` |
| Beacon 2 | 520, 12 P | (6.7, 1, 512.9) | `wend.beacon2` |
| Swarm field, row one | 611-619 | (6.6, 1, 619.1), (48.5, 1, 595.4), (95.5, 1, 582.5) | `Field_0..2` |
| Swarm field, row two | 756-764 | (61.9, 1, 753.3), (103.8, 1, 729.5), (150.8, 1, 716.7) | `Field_3..5` |
| Swarm field, row three | 901-909 | (104.2, 1, 865.8), (152.3, 1, 863.6), (200.0, 1, 873.5) | `Field_6..8` |
| A dark way through it | | along 575 to 960 at 22 to 28 m to port: (575, -22), (615, -22), (690, -25), (760, -28), (830, -24), (905, -20), (960, -20) in (along, starboard) | flown by `WendmireWorldTests` |
| Beacon 3 | 1000, 12 P | (132.5, 1, 962.0) | `wend.beacon3` |
| Pocket B: off-lane beacon 2, cache, two swarms | 1150, 58 S | (183.5, 1, 1130.2) | `wend.offBeacon2`, `cache.wend.pocketB`, `Pocket_B` |
| The Veil Ray's circuit centre | 1380, 8 S | (65.8, 1, 1328.8) | `Veil_Ray` |
| **The dead beacon** | 1380, 12 P | (45.9, 1, 1327.2) | `Relight the dark beacon`, `wend.beaconRelit` (report `DeadBeacon`) |
| Back from town | 1530, 4 S | (50.3, 1, 1478.0) | spawn `Driftreed` |
| **The tide-gate** | 1560, 0 | (44.0, 1, 1507.6) | `Land — Driftreed` -> Driftreed's `Arrival`; locked on `wend.beaconRelit` |

## Evidence

Built nine times in the editor (`WendmireBuilder.BuildZones -wendmire-zones SporeVeil,Driftreed`, then `ZoneLook.Apply`), each followed by stills through the flight camera (`LookDev.Render`, 23 views: the named places, the lane every 150 m or so, two wide). The floor measure doesn't apply to a sky; each still was looked at.

| Pass | What was wrong | What changed |
|---|---|---|
| `wend-veil-01` | The pods were grey veined lumps with white dots: an asteroid field, flat-shaded rock. The pale moon was a flattened dome read as a lens, its craters chalk rings, its face soft. The mire under the gate was one scum texture stamped every 5 m: wallpaper | Pods became long ribbed seeds in lilac, mint and peach, lit from inside a little, a green glowing tip at one end; strands carry beads of spore; lace colonies got rounder. Moons became whole balls with hard-shored seas and shaded craters, their texels denser than the screen at that depth. The water's repeat went to 30 m and lost its blobs |
| `wend-02` | The moon's pole pinched in the middle of its face; the lilies round the gate were a solid carpet of flat bright green | Poles turned to the limbs; lilies thinned and darkened |
| `wend-07` to `wend-12` | The arrival's frame was half bare sky; each nebula band began at full strength and cut a straight edge across the sky | The arrival keeps a hull's width clear, not eight metres; the bands are soft across their width (a texture of their own) and fade in and out along their length |
| `wend-12` (review) | The pale moon, in the middle of the arrival frame, was a low-detail ball with flat crater blotches; the lace colonies read as flat green coins; every pod was one pale ellipsoid; the nebula was a smooth two-colour wash | `wend-15` to `wend-17`: the moon's face is laid once over the half the camera sees (four times the texels there) and painted with seas of wandering shores, wrinkle ridges, and 3,200 craters whose count falls off with the square of their size (each bowl lit on one wall and shadowed on the other, a hard lip, the big ones terraced with a central peak, three young ones throwing rays). Drawn evenly between sizes, the first repaint was all big bowls and read as foam. Pods come in four builds (spindle, teardrop, waisted gourd, banded horn) in six stem-to-tip colourings; lace colonies are two to four stacked shelves with a thickness, a rolled rim and beads of spore, lit a little from inside. The bands carry long threads of glow, finer ones, and dust lanes, with a third hue through their middle. A pod's profile went below zero at its tip once and took a whole stretch of drift out of every frame (the build now refuses a stretch whose bounds aren't numbers) |
| `wend-03`, `wend-04` | | Read as a place: in every still of the lane there is drift (pods, lace, beaded strands) over a nebula sky, a moon under each leg, a beacon at each leg's ends, the tide-gate's ring of ten lamps on the mire among lilies and peat islands |

What is weak, plainly:

- **The rule can't be seen in an editor still.** The swarms and pods are encounter bodies that only appear in play (on show and asleep, `previewPoint`), and the ship's glow and the GLOW gauge exist only in play. Nothing of "visibility is the resource" has been seen on screen yet; it is tested (`WendmireWorldTests.ALitShipDrawsTheSleepingSwarmsNearItAndADarkOneSlipsBetweenThem`), not looked at.
- **The beacons blink only in play** (`LampRhythm` runs at runtime); a still shows each lamp lit.
- **Flown only by tests**: `WendmireWorldTests` flies the lead dark through the field's gaps with the partner under her own brain (nothing wakes), and watches the ray's loop from out of its reach (clear of a ship at the lamp for most of each loop) before relighting the beacon on the far pass. Nobody has flown the 7 to 9 minutes, threaded the pods, or flown the field lit, in a player.
- **The nebula's threads run with the lane**, so they read a little as aurora curtains; they stay behind the drift and the beacons.
