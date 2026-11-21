# The Hull of Many Hands - the outside of the ship, and Hulljack's first part

2026-11-12. Builder: `ManyHandsBuilder.Hull.cs` on `ZoneKit` (world engineer B, chapter 5). Chapter brief: [ch05-many-hands.md](../chapters/ch05-many-hands.md). Script wiring: [ch05-manifest.md](../writing/pending/ch05-manifest.md). The city inside: [ManyHands.md](ManyHands.md) (world engineer A). **MAP_EYE_TEST: blockout written before construction (this page); editor stills and what is still weak are under Evidence.**

## What the place is for

The outside of the convoy that became a town: a freight hauler's keel down the middle with the vessels the convoy picked up lashed along it, every one a different make and colour, joined by collars, cables and patch plates. The party flies over it the first time they leave its dock with a reason to, and what they fly over is the town they have just walked through, from outside: the garden's glass, the raft bay's open hold with the rafts floating in it, Tallow's cabins, the old liner's arcade with its portholes lit, the galley's vents, Pell's lamps at the bow, the freight spine's containers and cranes, ships parked at the stern dock and a queue of settlements' ships holding off the port side, waiting to be let in.

A salvage frame, Hulljack, has clamped on at one of the joins and is cutting. Its operator, Dessik Varn, has a filed claim on the hull and a tug standing off the stern.

Users: the party (flying), Hulljack and the claim drones from its tug, traffic, and, later in the game, anyone coming back out to the hull (it is the hub's outside for the rest of the game).

## The rule, and how it reads on screen

**The frame can only hold on at a join, and the hull's own gear is the cover.** The brief's fight is the zone: each foot is exposed as it plants, each foot cut makes the frame lurch to another join, and which booms and masts are cover depends on the join it is on.

- A **join** is where two vessels of different make meet, and it looks it: the plating changes colour and pattern across a raised collar with clamp bands, lashing cables and patch plates, and a line of cool seam lamps along it. Of the many joins on the hull, four are the fight's, and Hulljack's claim tags are bolted along each (lit, below the plane): Dessik has filed on exactly those four, and he told the party to stand clear of the joins.
- **Cover** at each fight join is the hull's working gear standing up through the flight plane: a cargo boom out of a crane pedestal, an antenna mast, a stack of containers. Each piece is solid at the ships' height and drawn down to the hull it stands on, so its foot shows where it is. Each join's set stands round it, outside the room the frame needs, so as the frame moves the cover that matters moves with it.
- Nothing taller than 4 m over the plane stands anywhere the camera looks at a fight across, and nothing to aim at lies just north of a join (behind the frame, from the fixed camera).

## Construction rules

- Everything flies at the flight plane (y 1). The hull is under it: roofs 3.5 to 8 m down, the vessels' south faces showing between them (the camera looks north at 48 degrees, so a south face and a roof are what it sees; north faces are never seen). Backdrop is kept readable by where it shows (`ZoneKit.ShowsAt`).
- No stretched texture, no flat-shaded lump. The hull plating is built geometry (each plate its own box with a seam round it, tinted in its vertices per vessel) over a plate texture painted in code at about 150 texels a metre at the roofs' depth; windows are built quads on an emissive material; the vessels' paint is vertex colour. Kit pieces are used where the city has them (Tallow's station hull for the cabins, Teo's seed barges for the garden's carriers, Driftreed's rafts, the dock's pods and landing pads, parked ships, masts and tanks), each regraded to sit on its vessel.
- Limits: the **harbour line**, a ring of lamp buoys round the ship, 300 x 420 m, each a warm lamp on a float with a lit line strung to the next, solid at the plane. A ship flown at it stops at the line it can see.
- Every way out says where it goes; the locked one says what opens it.

## Plan

North up; the ship's bow is north, its stern (and the tug) south. "y" is a roof's height; the plane is y 1.

```text
                         z 170  LAMP GALLERY (bow tower, roof -2.5): Pell's lamps in its south windows
                          |
   CABINS (Tallow's       |  SPINE (keel, roof -5,      GARDEN (three seed carriers under a glass
   station hull, roof -6) |  x -16..16): containers,    vault on carrier ribs, roof -3.8)
   x -104..-22, z 32..128 |  cranes, the ridge walk     x 22..98, z 32..124
                          |
     J2 (-44, 29) ========+================ J3 (44, 29)
   ---------- ARCADE: the old liner, x -72..72, z -26..26, roof -6.5, glazed promenade -4.6 ---------- GALLEY (x 72..102, vents)
     J1 (-44,-29) ========+================ J4 (44,-33)
                          |                       |
   RAFT BAY (open hold,   |                FREIGHT HALL (roof -3.5), x 22..66, z -100..-36
   rafts on water at -9)  |                roof cargo door (44, -68): Hulljack's way in
   x -104..-22, z -84..-32|
   claim drones (-50,-74) |
                          |
   STERN DOCK (x -66..-16)|  container yard       STERN APRON: tanks, a gantry
   DOCK - MANY HANDS pad  |
   (-40, -134)  ARRIVAL   |
                         z -158  stern
                  HULLJACK'S TUG standing off (55, -196)
   queue of settlements' ships holding off the port side, x -150, z -60..100 (below the plane)
   harbour line: x -150..150, z -215..205
```

## What happens here

| Where | What | Flags, ids |
|---|---|---|
| Stern dock (-40, -134) | `Arrival` beside the pad, where a launch from the city's dock comes out. `Dock — Many Hands` (open) takes the party back in | to ManyHands spawn `Arrival` |
| Over the raft bay (-58, -84) | Three claim haulers tagging the raft bay's hatch, on show and inert from the dock, waking as a ship comes over the hold (`Hull_Drones`, needs `hands.claimRead`): the tether before the frame's own haulers use it | `clear.Hull_Drones` |
| Raft bay join J1 (-44, -29) | Hulljack clamped on and cutting, on show and inert from the approach (`previewPoint`); the fight's door is the box over the four joins (`Hulljack`, needs `hands.claimRead`) | `bossdown.Hulljack`; `hands.feet1..4` as feet are cut (the fight's) |
| Checkpoint (-28, -66) | Spawn `Hulljack`, just outside the door's box, 40 m from the frame | |
| J2 (-44, 29), J3 (44, 29), J4 (44, -30) | The joins the frame leaps to | |
| Freight hall roof door (44, -67) | The frame tears it open and goes in (`BossExit`). Then `Follow Varn in — Many Hands freight hall` stands over the torn door; before that the same beacon is only "The freight hall's roof door" and a press says "The roof door holds until the salvage frame tears through it" | open from `bossdown.Hulljack`, gone at `bossdown.DessikVarn`; to ManyHands spawn `FreightHall` (the second part's checkpoint); `hands.hullCleared` set by a `FlagAll` on `bossdown.Hulljack` |
| The cabins' north-west row (-93, 119) | After part two, the breach the alarm was about: a patch on the cabins and the *Steady Hand*'s two rescue cutters latched beside it | shown once `bossdown.DessikVarn` |

No talk is scripted out here (manifest section 2: the claim is read on the radio in the city, before the launch); the barks the fight plays (`bossphase.Hulljack`, `bossopening.Hulljack`, `foot`) are the combat engineer's.

## What is in frame

The flight frame is about 25 m wide and 19 m deep at the plane, 32 x 24 m at a roof 6 m down; a fight pulls the view back to frame the frame and the party, 60 to 90 m. Every stretch has a subject:

- **The spine**: container stacks in four lengths and a dozen paints, some open, some under tarps, two gantry cranes over them, the ridge walk with its lamps.
- **The liner**: cream and teal, rows of lit portholes on its south flank, the promenade's glass roof with the arcade's light under it, deck lamps.
- **The garden**: three of Teo's seed carriers side by side under a vault of glass on bronze ribs, green under the glass, grow-lamps.
- **The raft bay**: an open hold, water in it, Driftreed's rafts moored inside, mats drying on lines across the hatch.
- **The cabins**: Tallow's station hull, the convoy's first home, its windows lit.
- **The galley**: vent stacks with warm light round their skirts.
- **The bow**: the lamp gallery's south windows with Pell's lamps in them, and the gallery's own lamps along its rail.
- **The stern**: the dock's pads and parked ships, cranes, tanks.
- **Traffic**: skiffs and cutters moving on loops under the plane round the ship; the queue of settlements' ships to port.
- **Signage**: each district's name on a sign lying on its own roof (`GARDEN`, `ARCADE`, `RAFT BAY`, `CABINS`, `GALLEY`, `LAMP GALLERY`, `FREIGHT HALL`, `DOCK`, `FREIGHT SPINE`), `MANY HANDS` painted large across the dock's deck where every launch comes out, and `HULLJACK` on the tug's.
- **The joins**: seam channels a step down between the vessels, a collar along each with clamp bands, cables and patch plates, seam lamps; at the four fight joins a coupling drum up to just under the plane, chevroned white and teal, Hulljack's claim tags on its side.
- **The roofs**: every vessel's roof dressed in its own make (`Roofscapes`): deckhouses with their south windows lit, vents, pipe runs, hatches (some open on lit rooms), skylights, tanks, aerials, deck lights, painted lanes, solar racks, crates. At the crossing the arcade's clock on a short tower.
- **Off the ship**: a cargo park tethered to starboard, approach lights off the stern dock, Pell's lamps on floats off the bow.

**The night side** (look pass, `ManyHandsBuilder.HullLight.cs`). The hull is seen from its night side: a far cold star low across the decks (pitch 34) so every tall piece throws a long shadow down the next roof, a dim neutral fill, and the city's own light doing the rest. All of it is drawn only and stays under the plane; no contract coordinate moved (the `MANYHANDS_COORDS` line is the same before and after).
- **Tall pieces**: the liner's upper decks, two tiers stepping up either side of the promenade (cabin deck with portholes, lounges set back with a long row of lit windows, deck chairs, a wheelhouse on one, `ARCADE` and `TEA ROOM` lit signs), each kept 11 m from a drum's middle and 4.5 m from cover; the hauler's bridge across the spine's stern (accommodation block, the bridge set back with its wheelhouse band lit cool, wing cabs, its roof dressed flat because it sits at the top of the room); two trussed gantry cranes across the spine at z -100 and 92 with floodlights under their girders (no containers under them); the clock tower at the crossing on a plinth with a lit lantern stage and a pyramid cap; the dock's tower (lit shaft, `DOCK` down its face, a glazed cab); stacked cabins up to three floors with balconies, washing and roof planters.
- **Light**: pools of lamp light lying on the decks under every lamp (an additive soft disc drawn before any danger marking, warm under the city's lamps, pale under work lights, cool under floods, green under the grow lamps, never orange, red or purple); running lights along every edge that is the ship's outside and along the lower hull's foot; the arcade's promenade glass and the garden's vault lit from inside; lit signs (`ARCADE`, `TEA ROOM`, `GALLEY`, `LAMPS`, `DOCK`); the freight hall and the seams left dark.
- **Wear on the broad decks**: blots of grime and oil multiplied into one plate in five, plates let in over old ones with a raised weld bead, repainted plates; on the dock thruster scorch round the pylon's foot and on both pads, a painted ring and the berth numbers 1 to 3, teal taxi lines, skid marks, floodlit masts, stowed gear (pallets, cable drums, a fuel tank, a power cell); the hauler's hold numbers on every hatch band.
- **The bow**: the chart room's dome raised to 2.6 m on twelve brass ribs, glazed warm and lit from inside by Pell's lamp shop under it (a ring of shelves with lamps of every make, lamps hung from the ribs, her bench, the great lamp), a lamp on its crown, a compass rose painted round it.
- **Each district's own house**: the liner's rounded deckhouses with portholes and a teal band, café corners under umbrellas and a lido; the cabins' stacks; the raft folk's shacks under pitched reed roofs with a lamp on a pole and mats drying on racks; the dock's kiosks glazed under lit canopies, tool carts, hose reels, drums; the bow's lamp stalls under striped awnings, coiled lines, crates of lamps.

## Coordinates for routes

World x, y, z (y 1 is the plane; everything a route flies to is on it). The builder's constants (`ManyHandsBuilder.Outside`), printed by each build's `MANYHANDS_COORDS ManyHandsHull` line.

| Place | World (x, y, z) | Id / flag |
|---|---|---|
| Arrival (a launch from the city comes out here) | (-30, 1, -126) | spawn `Arrival` |
| Dock home | pad at (-40, 1, -134); fly in from the north, east or west, range 6.5 | `Dock — Many Hands` -> ManyHands `Arrival` |
| Claim haulers over the raft bay | (-58, 1, -84); trigger 44 x 32, so a ship crossing z -100..-68 between x -80 and -36 wakes them | `Hull_Drones` (3 `ClaimHauler`), needs `hands.claimRead`; `clear.Hull_Drones` |
| Checkpoint | (-28, 1, -66) | spawn `Hulljack` |
| The fight's door | box x -75..75, z -62..42 (150 x 8 x 104 centred on (0, 1, -10)); the encounter itself at J1, so the objective's arrow points at the frame | `Hulljack`, needs `hands.claimRead`, checkpoint `Hulljack` |
| The frame on show | at J1, seen from the approach point (-40, 1, -53), radius 26 | `previewPoint` |
| J1 raft bay join | (-44, 1, -29) | `HullJoin` index 1 |
| J2 cabins join | (-44, 1, 29) | `HullJoin` index 2 |
| J3 garden join | (44, 1, 29) | `HullJoin` index 3 |
| J4 freight hall join | (44, 1, -30) | `HullJoin` index 4 |
| Roof door (where the frame goes; the way after it) | (44, 1, -67) | `BossExit` Hulljack; `Follow Varn in — Many Hands freight hall` -> ManyHands `FreightHall`, needs `bossdown.Hulljack` |
| Tug's drone rack | (48, 1, -188) | `ClaimTug` |
| Harbour line (the limits) | x -150..150, z -215..205 | |

## Boss arena contract

*What the Hulljack fight (part one) needs from the hull. Proposed by the world engineer from the brief; amended and agreed by the combat engineer (November 12, 08:00): the table below is the contract, and the builder follows it. "Coordinates as built" underneath says what was built.*

The fight's moves are `S/Combat/BossController.Hulljack.cs` (numbers in its `HulljackTune`), its definitions `ManyHandsBuilder.Enemies.cs`, its tests `T/PlayMode/ManyHandsCombatTests.cs`, which build a stand-in of this room in the flight yard, so nothing here waits on the hull being built. Every position is at the flight plane (y 1) unless it says otherwise.

**How the fight uses the hull.** The frame stands on a join with its four feet clamped, cutting. Clamped to a join, its body shrugs off blows (a hit lands at .3, read as resisted; with no join under it, as in the proving yard, it has nothing to clamp to). Every so often it re-plants a foot: a marked slam round that foot, and then the foot glows and stands exposed, the frame open, for five seconds. A foot cut takes a quarter of its bar; it reels, then lets go and leaps to the next join (a marked lane and a marked landing circle) and calls two claim haulers from its tug. The fourth foot cut, it has nothing left to hold on by: it goes down through the freight hall's roof door. Between plants: a torch sweep (a marked fan), a grapple shot (a marked line; a ship caught is reeled toward the frame and crushed unless it rolls free), and from the second join on, magnetised debris flung down five marked lanes with gaps between. Torch, grapple and debris all stop at anything solid at the plane: that is what the booms are for.

| Thing | What the fight needs | Why |
|---|---|---|
| Joins | **Four** fight joins, J1 to J4, at the plan's places. At each, the join's raised collar stands up to just under the plane: its drawn top at **y .2 to .6** over a disc of **r 9 m** round the join's centre (drawn only, not solid). Elsewhere the roofs keep their depths | The frame stands on the plane, where the ships fly and shoot: its feet (the four cyan rings) stand within r 4 of the join's centre at y 1 and are the targets. On a roof 6 m down the rings would show 5 to 6 m south of where they are hit (the 48 degree camera) |
| Room | Nothing solid at the plane within **r 14 m** of a join's centre, and none of the other three joins' cover within r 14 of it either | A planted foot's slam reaches 8.5 m from the centre; the grapple's crush and the ship's roll need the rest |
| Cover | At each join, **two to three** pieces solid at the plane (a boom, a mast, a container stack) between **r 16 and r 26**, each solid from **y .2 or lower to y 2.2 or higher** and **1.2 m or more thick**, none due south of the centre (the camera's side) or due north within 15 m (hidden behind the frame) | The torch fan reaches 18 m from the frame's middle, the grapple line and the debris lanes 26 m: a ship behind a boom inside those is untouched. Thinner than 1.2 m and a line slips past it |
| Order | J1 (start, south-west) - J2 (north-west) - J3 (north-east) - J4 (south-east), then the freight hall's roof door | The fight circles the crossing and ends beside the door it tears through |
| The leaps | Nothing needed: the frame leaps from join to join over whatever lies between, in a marked lane 5 m wide with a marked landing circle r 7 round the next join. Keep the landing circles free of solid (the Room rule does) | Each leap is a move: marked, dodgeable, and the party chases it |
| Anchors | A **`HullJoin`** (`S/Combat/HullJoin.cs`) on an empty object at each fight join's centre, at the plane, `index` **1 to 4** in the order above. A **`ClaimTug`** (`S/Combat/ClaimTug.cs`) on an empty object at the tug's drone rack, at the plane. A **`BossExit`** (`boss="Hulljack"`, `sinks=true`) at the plane over the roof door's middle | Typed anchors the boss finds (`HullJoin.All`, `ClaimTug.All`, `BossExit.For`) rather than names. The tug gives only the side the drones come from: they are launched 30 m from the frame toward it, never 160 m off at the tug itself |
| Door | Encounter `Hulljack` (the main quest's step 5 names it), trigger over the four joins, `requiredFlag="hands.claimRead"`, `checkpointSpawn="Hulljack"` with the spawn within 60 m and outside the trigger. Boss spawner at J1, **yaw 180**, `faceAsPlaced`, radius 0, `previewPoint` on the approach with a radius that takes in J1, so the frame is on show and inert from the approach | The shared door tests. Its feet are made as it wakes and stay shut but for the one it plants, so nothing on it can be cut before the fight |
| Drones | The fight launches **`ClaimHauler`**s itself (two at each leap, never more than three alive): the claim drone's heavier sister, on the same body, whose tether reels a ship in. The `Hull_Drones` encounter over the raft bay uses `ClaimHauler` too, not chapter 3's `ClaimDrone`, whose hits are sized for Sync 10 (`TowDrone` is chapter 9's) | One enemy, one weight: `ClaimHauler` is sized for this chapter's party |
| Ending | `Ending.Leaves`: at nothing it turns for the `BossExit` and goes down through the roof door (it runs 12 m/s for about 3 s: the door is 35 m from J4). The way after it opens on `bossdown.Hulljack` | "With four gone it tears through a cargo door into the freight hall". With no `BossExit` it would leave straight away from wherever the party stood |
| After | The fight sets `bossdown.Hulljack` and `hands.feet1` to `hands.feet4` as each foot is cut. It sets nothing else: `hands.hullCleared` is the world's (a `FlagAll` on `bossdown.Hulljack`, or the follow prompt). It plays no bark as it goes, so the last foot's lines are the last word | |
| Music | `Glitch Boss` (the `EnemyDef`'s `bossMusicId`), both parts | Brief, Music |
| Leaving mid-fight | The dock and the follow are refused mid-fight by the components themselves | |

What the fight logs, for the builder's own checks: `BOSS_SHIFT Hulljack 2` (its one shift, at half: the second foot), `HULLJACK_PLANT <foot>`, `HULLJACK_FOOT_CUT <n>`, `HULLJACK_FOOT_TORN <n>`, `HULLJACK_LEAP <join>`, `HULLJACK_GRAPPLE caught|rolled|missed`, `BOSS_SUMMON Hulljack <n>`, `BOSS_LEAVES Hulljack`.

### Freight hall contract (part two, for docs/maps/ManyHands.md)

*Handed to the world engineer who stages the freight hall, to copy into docs/maps/ManyHands.md as "Freight hall contract". Written by the combat engineer, November 12. The fight's moves are `S/Combat/BossController.DessikVarn.cs` (`DessikTune`); positions are from **F**, the boss's spawner in the hall, +z north (the way the fixed camera looks).*

**How the fight uses the hall.** Dessik fights from the same frame with no feet: it can't walk. It turns where it sits and moves only by its claw. The claw is a marked line: a hero caught is hauled in and slammed (roll or guard it); dodged, the claw bites whatever solid it meets behind him (a stack, a crate, a wall) and the frame hauls itself there and arrives winded (an opening); guarded, the claw clamps on the guard and the frame is yanked open. The torch sweeps a marked fan that stops at cover and leaves three burning lines on the floor for four seconds. The magnet pulse (its four rings, which held the feet to the hull) strikes a marked disc round the frame and drags every loose crate in reach along the floor to it, each down a marked lane, striking whoever stands in the lane; crates that slam into the frame jolt it open. From half, Dessik throws crates back out onto marked circles at the party, where each lands as cover; and at half the cabin alarm sounds. Break the frame and it kneels: its cockpit is the target until it rises. At nothing he yields.

| Thing | What the fight needs | Why |
|---|---|---|
| Combat in the hall | While the fight is on, the hall is a combat place: Shaped forms, enemies awake, the party's skills live. The rest of the ship stays safe. ManyHands' own `ZoneKind` and how the hall differs from the town are the world's to choose (a `SafePocket` flips the zone to safe inside one box, so several of them would fight each other; a hall-shaped way to be a combat place inside a safe town is a world piece) | The fight is on foot with the party's combat kit |
| Floor | Level and walkable (no step over .3 m) over at least **34 x 26 m** (x by z) with F in its middle; nothing solid within **r 6** of F | The frame (capsule r 1.8) turns in place; its slam, disc and swipes reach 6 m from its middle |
| Fixed cover | **Four** cargo stacks, static, solid (`BoxCollider`, in the navigation bake), **2.2 to 3 m** tall and 2.5 to 4 m across, centred **9 to 13 m** from F, none due south of F (the camera's side) or due north within 15 m (hidden behind the frame) | The torch and the claw stop at them; a dodged claw bites one and the frame hauls itself to it |
| Loose crates | **Six** `LooseCargo` (`S/Combat/LooseCargo.cs`) crates, each an object with a solid `BoxCollider` of **1.4 to 1.8 m** a side (the component adds its carving `NavMeshObstacle` if there isn't one; not static, not in the bake) and its drawn crate under it, standing on open floor **7 to 14 m** from F, **3 m or more** from each other and from the stacks. The floor between each crate and F as clear as you can make it | The pulse drags them straight to the frame down marked lanes; a crate that meets a stack on the way stops there. The throw puts them back on the floor within 16 m of F, which therefore has to be walkable everywhere a crate can land |
| Walls | North and east walls **16 m or more** from F (tall is fine); south and west low, the camera's side | The claw reaches 16 m and the frame hauls itself toward what it bites (never more than 13 m from F: the fight keeps it there) |
| Light | Lamps enough that the floor reads with every marking on it (six lights at most on the floor renderer) | Burning lines, crate lanes and landing circles are the fight |
| Boss | Spawner at F, **yaw 180** (facing the hall door), `faceAsPlaced`, radius 0. `previewPoint` at the crossing, radius taking in F, so the frame is on show and inert from the door | It is in the hall from the first look |
| Door | Encounter **`DessikVarn`** (the main quest's step 6 names it), trigger over the floor, `requiredFlag="hands.hallEntered"`, `checkpointSpawn="FreightHall"`. The `DessikHall` talk as a walk-through `Crossing` just inside the hall door that sets `hands.hallEntered`; one `Membrane` seal across the hall door behind it (strands drawn, carving while shut); a free repair cradle within a minute's walk of the checkpoint | The shared door tests: checkpoint, retry, no way out mid-fight. Nothing of the fight barks before the encounter begins, so nothing talks over the crossing |
| Checkpoint | Spawn **`FreightHall`** outside the seal, within 60 m of the hall door, on navigation, and **nearer the hall door than `Arrival`** (the retry test measures it). The hull's `Follow Varn in` lands here, and that load's autosave is the checkpoint between the parts | A party that falls in part two retries at `FreightHall` with part one won (`bossdown.Hulljack` and `hands.feet1..4` stay set) and the frame whole. Part one is never fought again |
| Ending | `Ending.Yields`: he gives up, the bar goes, the seal opens. A `YieldedCaptain` hands over to Dessik's own `Npc` (first node `DessikYields`, which sets `hands.dessikYielded`; `DessikHeld` after), `bossId="DessikVarn"`, `downFlag="bossdown.DessikVarn"`, `heardFlag="hands.dessikYielded"`, and Abri's people as its guards if wanted. The fight plays no bark at its end, so nothing talks over `DessikYields` (manifest 8.10). No `BossExit` | "At zero he yields, and is taken into custody by Abri's people" |
| The breach | Nothing: the fight sounds the cabin alarm itself at its half-way shift and plays Keeper's `hail.breach`, the party's `breach` and the Patrol's `hail.latched` in turn. It sets no flag. The breached cabins, the patch and the latched cutters are the world's, shown on `bossdown.DessikVarn` | "The party cannot leave the hall": the seal already sees to that |

What the fight logs: `BOSS_SHIFT DessikVarn 2`, `DESSIK_CLAW caught|guarded|hauled|empty`, `DESSIK_PULSE <crates>`, `DESSIK_THROW <crates>`, `DESSIK_KNEELS`, `BREACH_ALARM`.

### Coordinates as built

World engineer B, November 12, from `ManyHandsBuilder.Hull.cs` (the numbers are the builder's constants in `ManyHandsBuilder.Outside`; `ManyHandsHullTests.TheFourJoinsAreBuiltToTheArenaContract` and `TheFrameWaitsOnShowAtTheFirstJoinAndItsDoorWaitsForTheClaim` check them in the built scene).

| Thing | As built |
|---|---|
| Joins | `HullJoin` index 1 to 4 on empty objects `Hulljack join 1..4` at the plane: **J1 (-44, 1, -29)** raft bay, **J2 (-44, 1, 29)** cabins, **J3 (44, 1, 29)** garden, **J4 (44, 1, -30)** freight hall. Each stands over a coupling drum: drawn only (no collider), radius 9.4, its deck at **y .3** (rim to .58), in rings of plates with four clamp sockets on the diagonals at r 3.6, chevrons round its rim, from the lower roof beside it (-6.5 to -10.5) up |
| Room | Nothing solid at y .2 to 2.2 within r 14 of any join; the nearest solid is J1's cargo tower at 16.9 m |
| Cover | Three pieces a join, each a drawn piece with box colliders under it, solid from its roof (-6.5) to at least y 2.5, 1.6 m or more thick: J1 mast (-64, -20), boom from (-24, -40) to (-8, -40), cargo tower (-58, -47); J2 mast (-26, 41), boom (-64, 40) to (-64, 56), tower (-60, 12); J3 mast (64, 38), boom (28, 43) to (14, 55), tower (62, 10); J4 mast (24, -41), boom (64, -42) to (64, -58), tower (64, -13). Masts 3.5 m over the plane (the tallest thing in the arena), booms a 1.3 m box girder at the plane, towers of containers to y 2.6. None due south of its join, none within 15 m north |
| Order | J1 - J2 - J3 - J4, then the roof door (44, -67), 37 m south of J4 |
| Tug | `ClaimTug` on `Hulljack tug rack` at **(48, 1, -188)**, by the drone rack of the tug standing off the stern at (62, -198) |
| Exit | `BossExit` (`boss="Hulljack"`, `sinks=true`) on `Hulljack goes through the roof door` at **(44, 1, -67)** |
| Door | Encounter `Hulljack` standing at J1 (the objective's arrow points at an encounter's place), its box 150 x 8 x 104 centred on (0, 1, -10) (covers x -75..75, z -62..42, all four joins), `requiredFlag="hands.claimRead"`, `checkpointSpawn="Hulljack"`. Spawner at J1 (-44, 1, -29), yaw 180, `faceAsPlaced`, radius 0, the body `Hulljack` (a `ClaimDrone` stands in only if the definition is missing at build time) |
| Preview | `Hulljack preview (the approach)` at (-40, 1, -53), radius 26: J1 is 24.3 m from it |
| Checkpoint | Spawn `Hulljack` at **(-28, 1, -66)**: 4 m south of the door's box, 40 m from the encounter |
| Drones | `Hull_Drones` at (-58, 1, -84), trigger 44 x 6 x 32, three `ClaimHauler` on show (`previewPoint`, radius 0), `requiredFlag="hands.claimRead"` |
| After | `hands.hullCleared` by a `FlagAll` on `bossdown.Hulljack`; the roof door shut until `bossdown.Hulljack`, torn from then until `bossdown.DessikVarn`, patched after; the follow way's beacon is in the scene from the start (a `WarpBeacon` takes its open words as it wakes, so one kept hidden until it opened woke with its refusal for a name: `QaPlanWorldTests.EveryLockedExitStaysShutAndSaysWhatOpensIt` found it), named only the roof door while shut (`unnamedPrompt`), locked on `bossdown.Hulljack`, hidden at `bossdown.DessikVarn`; the dock pad's box runs from below a hull to above it, so a hull flown into it meets a wall rather than riding the lip of a thin slab (`QaPlanEarlyTests.AHullDrivenIntoARealWallGetsBackOff` found it); the cut at J1 glows until `bossdown.Hulljack`, cold after |
| Leaving mid-fight | `Dock — Many Hands` and the follow way are a `DockingPad` and a `WarpBeacon`, which refuse while a boss fight is under way or an enemy is awake near |

## Evidence

Built in the editor with `ManyHandsBuilder.BuildZones -manyhands-zones ManyHandsHull`, then `ZoneLook.Apply`, each pass followed by stills through the flight camera (`LookDev.Render`, 27 views in `Builds/quality/look/hands-hull-NN/`, 28 from `hands-hull-09` with the stern: the launch, the approach, each join at the play view and drawn back as a fight draws it, the roof door, each district, the hauler's bridge, the tug, the queue, the four edges, the whole ship). The floor measure doesn't apply to a flight zone; each still was looked at.

| Pass | What was wrong | What changed |
|---|---|---|
| `hands-hull-01` | Every play-view frame was bare plating: the vessels were plain slabs with their windows on walls the camera barely sees. The raft bay's hold was roofed over. The seed carriers were scaled to 80 m and the garden's vault stood up to the plane. The roof door was grey on grey. Space showed through the gaps between vessels under the collars | Roofscapes on every roof (deckhouses with their south windows lit, vents, pipes, hatches, skylights, tanks, aerials, deck lights, lanes, solar racks, crates); the hold cut open; three seed barges at their own size under a vault from -6 to -3.6; the roof door's leaves pale with a lit frame; every gap between vessels a seam channel with its collar on the floor; the drums plated in rings with chevrons; MANY HANDS painted across the dock's deck; the arcade's clock on a tower at the crossing |
| `hands-hull-02` | Read as a place in the wide views; the raft bay, the garden, the crossing and the roof door read at the play view. Still bare: the cabins (the frame sat on Tallow's flat top), the spine's edges, the galley, the bow; the tug was plain boxes and a bar; the queue was two ships in a dark sky; the bow's south windows were under the spine's roof. Several drawn-only things stood up through the plane (three-high containers, the gantries, the bow's lantern tower and dome, dock pods, the tug's boom), where a ship would fly through them | Everything drawn-only cut to under the plane (two-high stacks, jib cranes instead of gantries, a low dome, roof furniture fitted to the room over each roof); the harbour line pulled in to 300 x 420 m so the margins hold the cargo park, the queue, the approach lights and the lamp chain; denser roofscapes; Tallow's hull smaller in the cabins' corner; the tug rebuilt (hazard band, bridge with lit windows, a drone rack with drones docked, seized cargo, a lattice tow boom, HULLJACK on its deck); twelve ships and towed habitats in the queue with its lane lit |
| `hands-hull-03` | The cabins read as a district (deckhouses with lit windows round Tallow's hull in its corner), the arrival has its lanes and cargo, the queue and the tug have subjects. Still bare: the spine's broad roof round its stacks and at the crossing, the bow; the harbour's buoys read as faceted eggs | The hauler's cargo hatches down both sides of the spine (ribbed lids with coamings and lugs, the stacks standing on them); marker buoys (a float drum with a red band, a cage up to the lamp); the bow lowered to -3.2 so its lamps fit under the plane, its south windows in the band the camera sees |
| `hands-hull-04`, `-05` | The spine reads as a hauler's deck; the buoys as buoys. The bow's chart-room dome reads only as a ring of ribs | The dome glazed on brass ribs with Pell's great lamp under it and a ring of her small lamps round its foot; hatch rows carried through the crossing round the clock and the skylights |
| `hands-hull-06` (the lead's look) | Accepted as built, but it read as a roof plan: large flat tiled planes, evenly lit, seen from above; nothing tall enough to show a side or throw a shadow; the bow's dome a ring of ribs on planks; the same deckhouse the length of the liner | The look pass (`ManyHandsBuilder.HullLight.cs`, listed under "What is in frame"): the night side's light, the liner's upper decks, the hauler's bridge, two gantries over the spine, the clock tower, the dock's tower and floods, stacked cabins; pools of lamp light, running lights, lit glass and signs; wear, patch plates, scorch, numbers and gear on the broad decks; Pell's lamp shop under a warm glazed dome; each district's own house |
| `hands-hull-07` | Depth, pools and signs read, but the night grade turned every paint blue-violet (a cool sky, blue split-toning and blue shadow tints on the plating), the glow material's boost blew windows and skylights to white, and the north gantry lay as a pale plank under the ships | A neutral grade (pale star 0.95, a dark neutral fill, light floors 0.09, neutral shadow tints), glow boost 1.15 and the skylights dimmed; the gantries lowered, darkened and trussed, with no stacks under them; the clock tower's lantern given a pyramid cap; the dock's floods shortened and moved off the launch's middle; skid marks drawn as streaks; roofs on the stacked cabins |
| `hands-hull-08`, `-09` | Each paint reads at night (oxide spine, cream and teal liner, green garden, lilac cabins, slate galley) and the overview reads as a lit city. The new stern still showed the hauler's bridge as a block with a bare grey roof and a mauve container standing in front of its windows | The bridge's roof dressed flat (it sits at the top of the room): oxide band, five skylights lit cool, hatches, corner lamps, wear; the stacks kept clear of its south face; the mauve container paint changed to a brown |

Before and after, for the lead to open side by side: `hands-hull-06` and `hands-hull-10`, `ManyHandsHull-overview.png`, `-fight-wide.png`, `-crossing.png`, `-cabins.png`, `-bow.png`, `-arrival.png`, `-spine-north.png`; the stern only in `hands-hull-10`.

Tests (November 12, in test mirrors): `ManyHandsHullTests` 11 of 11 (`Builds/quality/focused/hands-hull-2-playmode.xml`); in `QaPlanWorldTests`, `QaWorldSecondTests` and `QaPlanEarlyTests` every hull case and every all-zones loop passes with the hull in it (`hands-hull-shared3-playmode.xml`). Those classes found two faults the first time round, both fixed: a hull pressed into the dock pad rode its lip and backed off 3.9 m (the pad's box now runs from below a hull to above it), and the way after the frame, kept asleep until it opened, woke with its refusal for a name (it is in the scene from the start, called only the roof door while shut). Each of the class's measures is shown failing on a broken state inside its own test: a box planted in a join's room, the east limit switched off, a mast planted on the camera's line, a hull set down on the dock pad; and the first run against the scene before the fix failed `OnceTheFrameIsThroughTheRoofDoor...` on the shut door offering to follow. After the look pass (`hands-hull-10`, same day, mirror S): `focused.ps1 -Classes 'ManyHandsHullTests|<the zoneless shared list of guide 0.1>' -Zones 'ManyHandsHull'`, 45 of 45 (`Builds/quality/focused/hands-hull-3-playmode.xml`: `ManyHandsHullTests` 11, the shared cases that name the hull, among them the wall, dock, exit and flight-prompt checks, and the zoneless prompt, lock, quest, item-spelling and dialogue cases).

What is weak, plainly (after `hands-hull-10`):

- **Tall is at most six metres.** Everything drawn stays under the plane, so the tallest pieces (the liner's upper decks, the hauler's bridge, the dock's tower) are five to six metres over their roofs. They throw shadows and show their south faces, but from the flight camera the ship still reads more as a lit townscape seen from above than as towers.
- **Big flat roofs remain** where the room is short: the bridge's roof and the liner's upper roofs are dressed flat (bands, skylights, lamps let in), and the freight hall's graphite roof is kept dark and plain so the roof door and its chevrons read.
- **The bow's dome** now glows and shows the shop's lamps through it, but the shelves under the glass read as warm shapes rather than as things one could name.
- **The spine's north gantry** crosses the frame right under the ships in the `spine-north` still; it sits five metres below them, but at the flight camera's pitch it reads close.
- **The water in the raft bay is a flat teal** with a painted swell and lamp light lying on it; it doesn't move.
- **The water in the raft bay is a flat teal** with a painted swell; it doesn't move.
- **Nothing here has been seen in a player or flown.** The frame on its drum, its leaps, the haulers waking, the traffic moving and the harbour line's stop are play; the stills hold the place, not the fight.
