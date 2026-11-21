# Xylos Nearspace - the seed barges' lane

2026-11-11, revised 2026-11-12 (second engineer on the lane: the debris stream's broken meshes, soft clouds, the lit pane, the beacon's bole, the knots' colour, a subject at the arrival). Reviewer: Claude (world engineers A and B, chapter 3). Builder: `XylosBuilder.Nearspace.cs` on `ZoneKit`. Chapter brief: [ch03-carrying-a-place.md](../chapters/ch03-carrying-a-place.md). Script wiring: [ch03-manifest.md](../writing/ch03-manifest.md). **MAP_EYE_TEST: blockout PASSED (agent review, editor stills); final in-player pass OPEN** - nothing here has been flown in a player.

## What the place is for

Low orbit over Xylos's day side, where the growers' seed barges are towed up and down between the delta and wherever they trade. A storm has thrown a stream of debris up through the lane (pod husks, loose seed, root, glass from the weather mirror) and orbital weed, snagvine, has come with it: three of Teo Marrow's barges are held out of the lane by knots of it, and podskimmers are stripping them. The zone's rule is **moving cargo**: the party does not kill its way down a corridor, it puts three large things back where they belong, and each one then stays in the lane as cover.

Users: Teo's barge crews (one voice on the radio), the Compact's patrol *Steady Hand* (a voice, at the edge of the shelter corridor), Hulljack's claim drones (tagging what has "come off its registered mooring"), podskimmers and their nests, and whoever is coming down to Rootmarket by the delta beacon.

## Rules of construction

- Everything flies at the flight plane (y 1). What is drawn below it is backdrop and never an obstacle; because the flight camera looks down at 48 degrees, backdrop shows south of where it stands, and is kept clear of everything that must be read by where it shows (`ZoneKit.ClearOnScreen`, `NearspaceKeep`).
- The lane bends, so nothing is placed by x and z: every place is so many metres along the lane and so many to starboard (`XylosBuilder.Beside`). The lane is walled 62 m either side by `FlightCorridor`; the far reaches stay visible.
- Solid things at the plane are only what the story says is there: the three barges (snagged, then resting in the lane), seven loose mirror panes round the second barge, and the two beacons.
- A barge is held by three knots that are an ordinary encounter's enemies (`Barge<N>_Knots`), on show from the start. When that encounter is cleared the barge sets `xylos.barge<N>`, reports one `SeedBarge` to the main quest, drifts along an authored path into the lane and stays (`S/World/SnaggedBarge.cs`). All three set `xylos.bargesFree`, which lights the delta beacon and unlocks its landing.

## Plan

North up; the lane runs 1,660 m from the arrival beacon (south-west) to the delta beacon (north-east). Distances are metres along the lane; "P" is port (left of travel), "S" starboard.

```text
                                                              [3] barge three, 31 P ......... DELTA BEACON  1650
                                                         claim drones 26 m beyond its far side   (land - Rootmarket;
                                              claim talk 1386 .                                   dark until 3 free)
                                    fight 1180 .        nest B 1236, 45 S
            WEATHER MIRROR      . . '
          (190 m disc, 34 m     [2] barge two 952, 28 P   <- rests at 990
           under the lane,      mirror point 905, 14 P; seven loose panes at the plane
           centre 62 P)      .
                          fight 720
              nest A 612, 46 P
                        .
                 patrol hail 470      <- the planet's limb passes under the lane here
                      .
                [1] barge one 252, 25 S   <- rests at 296
                   .
               fight 130
              Teo's hail 46
   warp home  ARRIVAL 0
```

World coordinates of every place a route needs are logged by the build (`XYLOS_COORDS XylosNearspace ...`) and listed under Evidence.

## What happens along it

| Along | What | Notes |
|---|---|---|
| 0 | Arrival (spawn `Arrival`), `Warp — Tallow Approach` | open both ways; arrives at Tallow Approach's spawn `Xylos` |
| 46 | Teo's hail (`TeoBargeHail`, crossing) | teaches the knots: a blade or steady fire |
| 130 | `Lane_0`: three podskimmers | the first fight, small on purpose |
| 252 | **Barge one**, 25 m to starboard: three knots, five skimmers on its lane side, its claim tag on the far flank | freed, it rests at 296, 7 m starboard of the centre line |
| 380-500 | the planet's limb comes up under the lane | nothing to do: the first sight of Xylos |
| 470 | The patrol's hail (`PatrolHail`, crossing) | once |
| 612 | Nest A, 46 m to port: five skimmers and a hoard (`cache.xylos.nest1`) | optional |
| 720 | `Lane_1`: five podskimmers | |
| 870-1000 | **The weather mirror** under the lane; `Look at the weather mirror` at 905 | the landmark |
| 952 | **Barge two**, 28 m to port over the mirror's dark half, among seven loose panes at the plane | tighter flying; rests at 990 |
| 1180 | `Lane_2`: four podskimmers and a claim drone | the first sight of a drone |
| 1236 | Nest B, 45 m to starboard: five skimmers and a hoard (`cache.xylos.nest2`) | optional |
| 1386 | Teo sees the claim drones (`TeoClaimDrones`, crossing) | starts `ClaimTags` |
| 1438 | **Barge three**, 31 m to port: three knots and four skimmers on the lane side; three claim drones (`Barge3_Claim`) 26 m beyond its far side, where its tag is | the knots can be cut from the lane without waking the drones; going round for the tag is going to them |
| 1650 | The delta beacon: `Land — Rootmarket`, locked on `xylos.bargesFree` | returning from town arrives at spawn `Rootmarket`, 24 m short of it |

Travel: the ships cruise at 12 m/s, so the lane is 2 minutes 20 of flying end to end. The longest stretch with nothing to do is barge two's rest to the fight at 1180 and on to Teo's call at 1386 (190 m and 206 m, 16 and 17 s each). With the three barges, four swarms on the way and the talks, a first pass should take the brief's 8 to 10 minutes; that is an estimate from distances and enemy counts, not a measured run.

## Sightlines and what is in frame

- The flight frame is about 25 m wide and 19 m deep at the plane. A screen of bare starfield for more than a few seconds was the fault to avoid, so the debris stream is laid the whole length of the lane (about two pieces to every metre of it, 3.6 to 12.6 m below the plane and piled toward the lane's middle, thicker round the barges and the mirror), six soft bands of nebula lie far under the first and last stretches, and from 400 m on the planet is under everything.
- Each barge is visibly held before the party reaches it: its knots are on show from the start, each with two thorned snagvine cables (down into the weed's mass, and back round the hull; deep green going to umber, a blue-violet bud of light in every knot) that vanish with their knot (`SnaggedBarge` answers `EnemyAnchored.Freed` for each root).
- The mirror is 34 m below the plane and 190 m across, tilted toward the camera; one pane is lit and throws a light up at the lane.
- Enemies are read against the dark sky early and against the planet later. The planet is drawn dim and unlit by the sun on purpose (its own emission, no toon bands) so the creatures' colours stay the brightest things in frame. Until their own bodies are staged the enemies wear chapter 1 and 2 stand-ins.

## Evidence (November 12)

Rebuilt eight times in the editor (`XylosBuilder.BuildZones -xylos-zones XylosNearspace,Rootmarket`, then `ZoneLook.Apply`), each followed by stills through the game's own flight camera (`LookDev.Render`, 22 views: eleven named places, eight points along the lane, three wide) and looked at as contact sheets.

- **Plans:** `Builds/quality/chapter3/nearspace-b02/XylosNearspace-lane.png` (the whole lane over the planet), `-barge1.png`, `-mirror.png`, `-barge3.png`, `-beacon.png` (`MapEvidence.Zone`; `nearspace-03` is the first build's). The delta's fan of channels, the mirror's cracked disc with its one lit pane, each barge in its tangle of weed and the stream of debris along the lane read from above.
- **Stills:** `Builds/quality/look/xylos-b-02/XylosNearspace-*.png` (current); `xylos-b-01` is the same build before the last look fixes; `xylos-a-01` to `-06` are the first engineer's passes.
- **The debris stream was not drawn in `xylos-a-05` and `-06`.** A husk's horn took a fractional power of the sine of pi, which in floats is a hair under zero, so every horn put points that were not numbers into its stretch of the stream; Unity logged "abnormal mesh bounds" for two stretches on every load (eleven tests failed on the log) and culled or spiked the rest. Clamped; and the builder now refuses any zone holding a mesh with a point or normal that isn't a number (`NoBrokenMeshes`, every point of every mesh, since bounds miss a bad point that isn't first). `XYLOS_MESHES_OK XylosNearspace`, and no "abnormal mesh bounds" in the build log since.
- **Places** (world x, y, z; from the build's `XYLOS_COORDS` line): arrival (0, 1, 6); warp home (-16, 1, -2); Teo's hail line (5.7, 1, 45.6); first fight (16.1, 1, 129); barge one (56.9, 1, 241.1), resting at (54.8, 1, 288.6), knots at (53.9, 243.5), (62.2, 249.2), (50.4, 234.1); patrol's hail line (106.7, 1, 454.8); nest A (128.5, 1, 606.1); second fight (223.8, 1, 674.6); mirror point (318.9, 1, 836.1); barge two (344.2, 1, 878.1), resting at (395.2, 1, 878.5), knots at (341.6, 875.2), (336.6, 883.9), (350.8, 871.0); third fight (534.2, 1, 1007.1); nest B (604.5, 1, 991.9); barge three (757.2, 1, 1151.4), resting at (802.2, 1, 1123.2), knots at (760.8, 1149.8), (761.9, 1159.8), (754.1, 1142.3); delta beacon (969.9, 1, 1177.3); the spawn from Rootmarket (945.7, 1, 1174.8).

- **The way in from Tallow** (`XylosBuilder.StageApproach`): `Warp — Xylos Nearspace` at (11, 1, -37) in Tallow Approach, locked on `xylos.charted`, a 2.5 m box with a 5 m reach; the spawn `Xylos` a party coming home lands at is (6, 1, -30), 8.6 m from it (outside its reach, inside the 12 m the world tests allow). Checked against that zone's flight limits by the numbers: it has none. Its scene holds no boundary or limit object and no box collider wider than six metres but the 6 by 5 m deck floor at (0, -0.2, 0.5); the hull has no collider, so ships pass over it. The other warps of the arc stand 22 m (the Spore Veil's, (-11, -37)), 23 m (the Wick's, (24, -18)) and 38 m (the Halo's, (-22, -20)) away, so no two reaches overlap; the chapter-17 route flies to it from the launch point (0, -8.5) by (7, -27) and settles at (9.5, -33.5).

**What is in frame.** The floor measure doesn't apply to a sky (run on these stills it calls 19 of 22 "empty", which is what open space is), so this was judged by eye, still by still, against the brief's own rule: something other than starfield in every screen of the lane.

| Stretch | In frame |
|---|---|
| arrival, 60, first fight (0-130) | green and gold bands of nebula across the whole frame, eight to fifteen pieces of the stream (husks, seed, boughs, a rag of weed, a length of deck, a shard of glass); at the arrival itself, which the keep round the spawn leaves otherwise to haze, a root arch the storm tore off the town turning ahead and to starboard (placed by hand after the stream's draws) |
| barge one (252) | the barge on its side of the lane, loaded with seed and lashed in green, the weed's magenta tangle and cables beyond it; the planet's limb entering at the right |
| limb, patrol (380-470) | the limb as one long curve, sea and cloud under it |
| 560, nest, 720 | the planet under everything (sea, cloud bands, the first coast), the stream over it; the nest's hung husks and its hoard |
| mirror, barge two (820-1000) | the mirror's panes filling the frame under the lane, its frame's spokes, the lit pane; the second barge in its tangle among loose panes at the plane |
| 1060-1300 | forest and gold lowland with the delta's channels, the stream over it |
| barge three, claim (1438) | the barge and its tangle against open sky, the planet's far limb below |
| 1560, beacon (1650) | the limb falling away, gold nebula, the beacon's root pylons round the landing mark |

No still of the lane is bare starfield. The first build's were: two husks and a white ball in the arrival frame, and the nebula too faint to see. The emptiest frame left is off the lane: the claim drones' side of barge three, 50 m to port, which the third build showed as stars and a sliver of limb. It now has the far half of the stream round the barge, Hulljack's four claim buoys under the plane and the limb below, and is still the thinnest place in the zone.

### MAP_EYE_TEST at blockout (first build)

| Check | What was seen |
|---|---|
| Purpose and adjacency | Passes. A lane with cargo caught beside it, a landmark the storm broke, a beacon at the end where the lane goes down. Each barge lies to the side it was pushed and comes back to the middle. |
| Circulation and scale | Passes on the plan: nothing solid in the lane but what the story puts there, 62 m of room either side, the nests far enough off (45 m) to be a choice. Not flown. |
| Construction | Passes. What holds a barge is drawn (knots, cables, a mass of weed below); what lets it go is the knots' deaths; where it goes is authored. |
| Ecology | Passes: the debris is what a delta storm would lift (pod husks, seed, root, weed) plus the mirror's glass, thickest where things were caught. |
| Immediate understanding | Failed at first build: empty frames along the first 250 m, a planet as bright as the ships and flat as a poster, weed that was six purple boulders. |

### MAP_EYE_TEST after dressing (`xylos-b-02`, editor stills only)

**Passed for the editor stage; not passed for the player.** The lane reads as a place: a stream to follow, three barges visibly held, a broken mirror the size of a town under the middle of it, a world below. Since the first engineer's report: the planet's coasts are cut from a smooth field (crisp at the camera's scale), its fields are parcels with hedges and its forest and shore are the ground kit's layers; the clouds over it thin out over their last few metres and are lit on the sunward side and blue-grey on the other (they were white cut-outs); the stream has husks, seed, root balls, boughs, deck, folded rags of thatch, thorned weed and glass, plus 79 of the town's own pieces turning in it (`XYLOS_WRECKAGE pieces=79`); the snagvine is round, thorned, green to umber, with blue-violet buds (they were flat pink discs); the lit mirror pane is warm and shows its cells (it was a white blank); the delta beacon's bole is a mound of roots (it was a squashed lump that read as a table top). What is still weak, plainly:

- **The planet's fields are a pattern.** Parcels of four crops in one cell size, every one hedged the same: from the lane they read as fields, and also as a tiled floor.
- **Nothing in the stream moves** except the dust and the barges once free.
- **The nebula's bands are flat washes** of green and gold; they answer "is the screen empty" and not much more.
- **The knots' buds are small flat-lit dots** at the flight camera's distance: violet now, not magenta, but not yet anything more than a mark.
- **Enemies have not been seen against any of this.** The brief's test (chapter 2's first space enemy "read as soft clay on a dark sky") can only be judged with the chapter's creatures spawned, in the player. Their bodies exist; the review does not.
- **The delta beacon is a root bole with a station's beacon standing on it.** The lit and dark states are built and unseen.
- **Nothing has been flown**: the barges' drift, the knots' cables going one by one, the two hails, the claim drones' side of barge three and the 8 to 10 minute estimate are all untested in play.

Reviewer: Claude (agent review; no independent viewer and no approval by Nathan is implied).
