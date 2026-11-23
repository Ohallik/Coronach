# The Orrery - Oriel's moons on their rails

2026-11-12. Builder: `OrielBuilder.Orrery.cs` on `ZoneKit`, the machine itself `S/World/OrreryRail.cs` (world engineer A, chapter 7). Chapter brief: [ch07-the-cage-makers.md](../chapters/ch07-the-cage-makers.md). Script wiring: [ch07-manifest.md](../writing/ch07-manifest.md). **MAP_EYE_TEST: blockout written before construction (this page); editor stills and what was flown are under Evidence.**

## What the place is for

The way down to Oriel. The Ancelane built an orrery round their world and it is still running: nine broken moons, each threaded on its own light-rail, each rail a ring round the planet, every moon on one clock. A lane in to the planet has to cross all nine rails, and each rail has a row of bronze rail-gates on its outer side that open and shut as its moon comes round. The zone's rule is **one readable machine**: a player should be able to say after one pass what it does and how its parts move, and use that to get through.

Users: the party in two ships, Lume flying Ishen behind them in the courier pod (a voice, and a small ship trailing the party), Rail Warden drones asleep on the rails, and the Clerk of Roads, the broken moon, which sheds.

## The machine, as a player reads it

Everything below is drawn, and the same rule is what the code does (`OrreryRail`):

1. **A rail crosses the lane**, east to west: a bronze light-rail with a line of lamps along its top, curving away at both ends as a ring round the planet. **A moon rides it**, threaded on it through a bronze collar, turning on the rail as it goes, always the same way along it.
2. **The lamps run ahead of the moon.** For a stretch ahead of the moon (2.6 seconds of its travel) they stand **amber and steady**: that stretch is about to be swept. For a stretch behind it (3 seconds of its travel, `OrreryRail.lee`) they stand **green, steady and full bright**: its lee, where a ship crosses after it. Everywhere else they run **green**: dimmer light chasing along the rail the way the moon is going. (Drawn only, November 14: the gates' rule is unchanged.)
3. **A row of three rail-gates** stands just outside each rail (7.5 to 9 m, a moon's reach and a little more), across the lane: bronze rings standing on their plinths, a lamp band round each, drawn **slim** (everything of the ring past its opening drawn 0.4 of its distance nearer it: `Slim`) and **open at the top** (41 degrees either side of straight up, cut off the ring's own mesh short of where its band meets the plane: `OpenAbove`, logged as `ORRERY_RING_OPEN`), the gap closed by a lattice arch of two slim chords; so a ship waiting at a gate sees round the ring and through its top to the moon going by. The opening at the plane, the solid rims and the gates' rule are the whole ring's, as measured before (`ORRERY_RING`); the row's truss is drawn out to the slim band, so every solid has bronze over it. The rail's lamps and the gates are one signal. **A gate the moon is coming to, or is passing, is amber; a gate it has passed is green.** Between the rings, and from the outer rings to the lane's edges, is solid truss: a ship crosses a rail only through a gate.
4. **So: wait in a moon's lee, and go on green.** Three gates in a row go amber one after another as the moon comes along behind them, and green again in the same order once it has passed: a ship that tucks in behind the moon and takes the gate it has just passed is through, and the moon is going the other way.
5. **The moon strikes what is left on its rail.** While a moon is in the lane, the stretch from its trailing edge to the end of its amber reach is marked on the plane as a danger (`Telegraph.Line`, Threat, the moon's width). A ship touching the moon inside that marking loses a fifth of its Integrity before its frame's plating (about 11.5% of Taren's on arrival in Tempered parts, less of Idra's; never the last of it) and is thrown clear off the rail. Not through a roll's cover; not during a conversation.
6. **Keep its time and nothing minds you.** Each rail has two Rail Wardens asleep and on show just inside the rail, at the lane's edges. A lead ship that goes through a gate while it is amber has **forced** it: that rail's wardens wake (`RAIL_FORCED`, the lead's `rail.forced` bark). A ship that only ever takes green gates wakes nothing.
7. **The rails counter-turn, like gears**, and every moon goes round in the same twenty seconds. The moons cross the lane one after another inward, about eleven metres a second apart, so a party flying in at the stick's pace behind the first moon meets each next gate just as it goes green: the green wave. Missed, it comes round again.
8. **The Clerk of Roads is broken.** As it comes into the lane it calves a shard off its leading face (the combat engineer's `Moonshard`, below), which runs ahead of it on a marked track: ahead of a moon is doubly where not to be.

The lights are taught in words once, by Ishen, before the first gate (`IshenMoonNames`: "Amber and standing, the gate is shut. Green and running, it's open. Wait in a moon's lee, and go on green."), and as he says it the view draws back over the Steward's gates, so its lights are on screen while he names the moons, with the ship still a ship and not a speck (D359: drawn back over the first two rails it stood 337 m off and the ship was 28 pixels long). Everything after that is the same machine, a little faster each rail.

## Rules of construction

- Everything flies at the flight plane (y 1). The rails' beams lie 1 m under it (ships pass over them); the moons are centred 1 m under it and reach 5 to 7 m above. The gates' ring centres are 2.6 m under the plane and each ring leans back 28 degrees over its rail, so a ship passes through the upper half of a ring, over the gate's own bar, and the moon passes under the ring's arch.
- **Every gap takes Idra's hull** (2.17 m against 1.8): a ring's opening at the plane is 10.5 m, measured off the ring's own mesh where the plane cuts it as the build lays it (`ORRERY_RING`; two hulls side by side, 4.15 m apart, need 8.1); between a moon's inner edge and the next rail's gates there are at least 19 m of clear plane. Nothing in a lane is narrower than a ring's opening.
- **The machine is one set of numbers**: the rails are circles about one centre (the planet's, plan (0, 1300)), each given its radius, its moon's speed, radius, direction and phase, and one period for all of them (20 s). The builder lays everything from those numbers and the component runs the same ones, so a still and a frame of play show the same machine.
- Backdrop is below the plane and never an obstacle: the gates' plinths on a girder along each gate row, the orrery's radial spokes and its gear hubs, broken moon debris, the rails' arcs going on round past the lane walls, and Oriel itself. Kept clear by where it shows on screen (`ZoneKit.ClearOnScreen`), not by where it is.
- **Oriel is inside the camera's far plane.** The game's camera draws nothing past 1000 m. The first Oriel (radius 380, 800 m down under the rails' centre) was wholly past it: in play the planet was never drawn, and the stills (rendered at 1500) showed something no player saw. Now it is a globe of radius 165 centred at (80, -360, 1300): its rim comes into the top of the frame between rails 4 and 5, every point of it in frame is inside 960 m from then to the landing (the camera drawn back to 60 m included, checked ray by ray), and it grows to half the frame at the far gate, up and east of the lane, the ships at the frame's middle over its night side. Render the zone's stills with `-look-far 1000`.
- **No stretched texture, no flat lump.** Moons are built spheres with a painted crater face (texels denser than the screen at their size) and a detail layer. Oriel is a built globe whose map (2048, `OrielFace`) is painted for the one face the camera sees (one sheet across the disc, poles at its top and bottom limbs), lit from the scene's own sun by its vertices (the toon ramp's six steps lay six flat rings over a sphere), with a night map that shines on the dark side; its severed fibres are lines of light laid on it as geometry, crisp at any distance and brighter by night, meeting at the Glass Choir, a knot of glass shards.

## Plan

North up, toward the planet. The lane is 72 m wide (x -36 to 36), from the arrival (z 0) to the landing (z 880). Rails are named for their moons, outermost first.

```text
 z 880  LAND - THE SKYFIELDS (to Ishen's camp)      spawn OrielSkyfields 866 (back from the camp)
 z 850  FAR GATE (crossing: oriel.crossed, report FarGate)
 z 820  rail 9  THE WARDEN        E->W  13 m/s, the biggest moon, "rides last"   gates z 811   hail.moon 3 at 785
 z 730  rail 8  THE ALMONER       W->E  12.5                                       gates z 723   hail.moon 2 at 697
 z 640  rail 7  THE BELLFOUNDER   E->W  12                                         gates z 632   hail.moon 1 at 606
 z 552  rail 6  THE SECOND GARDENER W->E 10   } a double: rail 5's strip ends at 525,
 z 520  rail 5  THE REEVE         E->W  10    } rail 6's gates stand at 545
 z 430  rail 4  THE LAMPLIGHTER   W->E  10.5    named gate (middle ring)           gates z 423
 z 340  rail 3  THE CLERK OF ROADS E->W  9, broken: calves Moonshards              gates z 333
 z 250  rail 2  THE WEIGHER       W->E  9.5     named gate (middle ring)           gates z 243
 z 160  rail 1  THE STEWARD       E->W  8, the slowest: the first to read           gates z 153   named gate (middle ring)
 z 128  IshenMoonNames (crossing across the lane); the view draws back over the Steward's gates from z 88
 z  20  Lume's hail (hail.pod); the courier pod falls in behind the party
 z   6  ARRIVAL     warp home (-14, -4) - The Hull of Many Hands
```

## What happens along it

| z | What | Flags, ids |
|---|---|---|
| 0 | Arrival (spawn `Arrival`), `Warp — The Hull of Many Hands` (open) | to ManyHandsHull spawn `Oriel` |
| 20 | Lume on the radio (`hail.pod`), once; the courier pod trails the party from here to the landing | `oriel.podHailed` |
| 88-128 | The view draws back over the Steward's gates (`VantageView`) until Ishen has taught the lights | |
| 128 | **Ishen names the moons** (`IshenMoonNames`, a line across the lane) | `oriel.lightsRead` |
| 153 / 160 | Rail 1, the Steward: three gates, the moon, two wardens (`Rail_1`). Middle ring: `THE STEWARD` on its keystone | MoonNames: `Pass the named gate on green` (report `NamedGate`, `oriel.stewardRead`) |
| 243 / 250 | Rail 2, the Weigher (`Rail_2`). Middle ring `THE WEIGHER` | `oriel.weigherRead` (offered once the Steward's is read) |
| 333 / 340 | Rail 3, the Clerk of Roads (`Rail_3`), calving into `Shards_0..2` in turn as it comes into the lane | `moonshard` bark |
| 423 / 430 | Rail 4, the Lamplighter (`Rail_4`). Middle ring `THE LAMPLIGHTER` | `oriel.lamplighterRead` (offered once the Weigher's is read) |
| 513-552 | Rails 5 and 6, the Reeve and the Second Gardener, counter-turning 32 m apart: wait between them | `Rail_5`, `Rail_6` |
| 606 / 632 / 640 | Ishen names the seventh (`hail.moon`); rail 7, the Bellfounder (`Rail_7`) | `oriel.moon7Named` |
| 697 / 723 / 730 | The eighth (`hail.moon`); rail 8, the Almoner (`Rail_8`) | `oriel.moon8Named` |
| 785 / 811 / 820 | The ninth (`hail.moon`); rail 9, the Warden (`Rail_9`) | `oriel.moon9Named` |
| 850 | **The far gate**: a line across the lane | `oriel.crossed` (report `FarGate`: CageMakers step 1) |
| 866 | spawn `OrielSkyfields` (back up from the camp) | |
| 880 | `Land — The Skyfields` (open) | to OrielSkyfields `Arrival` (Ishen's camp) |

**The Names of the Moons** (MoonNames, offered at the camp): the three named gates are read in Ishen's order (each point waits for the one before it, so the objective's dim "Also" arrow goes to the Steward's first), by passing through the named ring while it is green at no more than 8 m/s (coast, or brake). Each keystone's point is only what the arrow finds: it has no reach and is never offered, so no button reads it; the rail reads it as the ship passes. Ishen reads each over the radio (`hail.survey`). Coming back up from the camp the party arrives at the inner end, so the quest is a lap: out to the Steward, back in by the Weigher and the Lamplighter. Once Ishen has read them (`ishen.moonsRead`) every gate shows its turn ahead of time: a ring about to go amber flickers amber for a second and a half first.

## What is in frame

The flight frame is about 25 m wide and 19 m deep at the plane.

- **At a gate** (where the routes wait in the lee, 6 m short of the middle gate: LookDev's `lee-N` stills): the slim ring round the ships, its lamp band green or amber, the row's truss out to either side and space beyond it; through the ring's open top, the moon going by on its rail and the rail's lamps across the top edge (amber ahead of it, steady green in its lee); from rail 7 on, Oriel behind the ring. Before November 14 one whole ring filled this frame and the moon was behind its top (`oriel-orrery-05-before/Orrery-lee-3.png`).
- **Between rails** (80 to 90 m): the orrery's armature below the lane (a radial spoke under the lane's middle 30 m down, the gate rows' girders, gear hubs where spokes meet rails), broken moon debris drifting at 6 to 25 m down in several shapes (cratered fragments, a split half-moon, bronze collar pieces and rail lengths), thickest in the Clerk of Roads' wake between rails 3 and 4.
- **The inner end**: Oriel rising into the top of the frame from between rails 4 and 5 (its night limb first), up and east of the lane, half the frame by the far gate and the landing: gold and olive land, deep blue sea, its severed road fibres as fine lines of light meeting at the Glass Choir's glint.

## Coordinates for routes

World x, y, z. The builder logs every number below as `ORIEL_COORDS Orrery ...`; after each build the table is checked against it (Evidence). Gates: the three rings of a rail at x -24, 0 and 24 (each 12 m across at the plane); a route goes through the middle of one.

| Place | World (x, y, z) | Id / flag |
|---|---|---|
| Arrival | (0, 1, 6) | spawn `Arrival` |
| Warp home | (-14, 1, -4) | `Warp — The Hull of Many Hands` -> ManyHandsHull spawn `Oriel` |
| Lume's hail | (0, 1, 20), range 30 | `hail.pod`, `oriel.podHailed` |
| Ishen's talk (line across the lane) | (0, 1, 128) | `IshenMoonNames`, `oriel.lightsRead` |
| Rail 1, The Steward: gates | (-24.0, 1, 152.5), (0.0, 1, 152.3), (24.0, 1, 152.5); rail z 160 | `Rail_1`: wardens (+-30, 1, 168.9) on the warden track |
| Rail 2, The Weigher: gates | (-24.0, 1, 242.6), (0.0, 1, 242.3), (24.0, 1, 242.6); rail z 250 | `Rail_2`: wardens (+-30, 1, 258.9) on the warden track |
| Rail 3, The Clerk of Roads: gates | (-24.0, 1, 333.2), (0.0, 1, 332.9), (24.0, 1, 333.2); rail z 340 | `Rail_3`: wardens (+-30, 1, 349.0) on the warden track; `Shards_0..2` |
| Rail 4, The Lamplighter: gates | (-24.0, 1, 422.6), (0.0, 1, 422.3), (24.0, 1, 422.6); rail z 430 | `Rail_4`: wardens (+-30, 1, 439.0) on the warden track |
| Rail 5, The Reeve: gates | (-24.0, 1, 513.2), (0.0, 1, 512.9), (24.0, 1, 513.2); rail z 520 | `Rail_5`: wardens (+-30, 1, 529.1) on the warden track |
| Between rails 5 and 6 (wait here) | (0, 1, 532) | |
| Rail 6, The Second Gardener: gates | (-24.0, 1, 545.2), (0.0, 1, 544.9), (24.0, 1, 545.2); rail z 552 | `Rail_6`: wardens (+-30, 1, 561.1) on the warden track |
| Rail 7, The Bellfounder: gates | (-24.0, 1, 632.2), (0.0, 1, 631.7), (24.0, 1, 632.2); rail z 640 | `Rail_7`: wardens (+-30, 1, 649.2) on the warden track |
| Rail 8, The Almoner: gates | (-24.0, 1, 722.8), (0.0, 1, 722.3), (24.0, 1, 722.8); rail z 730 | `Rail_8`: wardens (+-30, 1, 739.3) on the warden track |
| Rail 9, The Warden: gates | (-24.0, 1, 811.2), (0.0, 1, 810.6), (24.0, 1, 811.2); rail z 820 | `Rail_9`: wardens (+-30, 1, 829.5) on the warden track |
| The far gate (line) | (0, 1, 850) | `oriel.crossed`, report `FarGate` |
| Back from the camp | (0, 1, 866) | spawn `OrielSkyfields` |
| The landing | (0, 1, 880) | `Land — The Skyfields` -> OrielSkyfields `Arrival` |

**The way in, staged in chapter 5's scenes** (`OrielBuilder.StageApproach`; every piece named "Oriel: ...", rerun after `ManyHandsBuilder.BuildZones` and after `HoarfellBuilder.StageApproach`):

| Place | Where | Id / flag |
|---|---|---|
| Ishen and his handcart of books (from `chapter6.complete` until `oriel.charted`) | ManyHands (-20, -61), the dock's west end | body only, no talk of his own |
| Ishen arrives (a crossing 16 m across the spine, just past chapter 6's lift figures) | ManyHands (0, -49.4), box 16 x 5 x 3 | `IshenArrives`, needs `chapter6.complete`, sets `oriel.charted`, starts `CageMakers` |
| The warp to the Orrery, beside Hoarfell's off the stern | ManyHandsHull (-120, 1, -176) | `Warp — The Orrery` -> Orrery `Arrival`, locked on `oriel.charted` ("Ishen is waiting on the dock with his books") |
| Back from the Orrery | ManyHandsHull (-112, 1, -166) | spawn `Oriel` |
| Ishen in the archive (from `chapter7.complete`) | ManyHands `Hands.Arrivals.IshenAt` (-33.2, 42.3) | Npc `Ishen`, first and repeat `IshenAboard` |
| The gardener at the planted bed, and Idra's look at it (with `quest.EmptyGardens.complete` and `chapter7.complete`) | ManyHands `Hands.Arrivals.GardenerAt` (41.8, 49.7); the look at `GardenerLook` (42.7, 50.5) | `IdraGardenerAboard`, sets `oriel.gardenerHome` |

**Timing, for a route.** The clock starts when the zone loads (`GameTime.Now` at the rail's `Start`). Rail i's moon centre crosses x 0 at load + `phase` + k x 20 s; a gate is green except from 2.6 s before the moon's leading edge reaches its near rim until its trailing edge is a hull clear of its far rim. Phases (s): 4.0, 12.2, 0.4, 8.5, 16.7, 19.6, 7.6, 15.8, 4.0. A route can wait on the machine itself: `OrreryRail.Open(gate)` is what a test asks.

## For the combat engineer (Rail Warden, Moonshard)

Written by world engineer A, who needs these; the enemies are yours. Until each `EnemyDef` exists the Orrery flies a stand-in of the same shape (`Foe`): `RailWarden` -> `ChoristerDrifter`, `Moonshard` -> `Shellmine`.

| What | How the Orrery stands it |
|---|---|
| `RailWarden` | Two to a rail, one at each lane edge, **8.5 m inside the rail** (clear of its moon), at the plane, **facing along the rail toward the lane's middle**, spawner radius 0, `faceAsPlaced`. **Its rail is the warden track**: a straight `ForceLine` (width 3) across the lane 8.5 m inside the moon's rail, from x -42 to 42, drawn as a thin bronze rod with a white light line, a lesser rail beside the moon's, so `EnemyRider` finds it within its `wake`. Asleep and on show (`previewPoint` on itself, radius 0). Encounter `Rail_<n>`, trigger 6 x 6 x 6 on the drone (flying into one wakes its rail). The rail's own component **begins** the encounter when the lead forces one of that rail's gates. The brief's tell: "fires along its rail only": whatever it does awake, the answer the Orrery teaches is "get off the rail" (Idra's bark: "Don't sit on one."). Damage at threat 1 |
| `Moonshard` | Three encounters on rail 3, `Shards_0..2`, one spawner each, **no trigger the party can fly into** and no preview. As the Clerk of Roads' leading edge comes into the lane with the lead within 80 m of the rail, the rail moves the next unspent encounter's spawner to 2 m off the moon's leading face, on the rail, **facing along the rail in the moon's direction of travel**, and begins it (`MOON_CALVED`, the lead's `moonshard` bark). It should run ahead of the moon on a marked track and be spent, so the stretch ahead of a moon is where the debris flies and its lee is calm |

Nothing else in the Orrery is an enemy: the moons are the place's damage (no `source`, as `RisingWater` and a wall flown into are).

## Evidence

Built with `OrielBuilder.BuildZones -oriel-zones Orrery`, then `ZoneLook.Apply`, then `LookDev.Render ... -look-far 1000` (38 views since November 14, the nine `lee-N` where the routes wait added to the 29: the arrival, the hail and the talk, each rail's middle and east gates as a ship waits 3 to 4 m short of them, the double rail, the far gate, the landing, three wide views). Stills in `Builds/quality/look/oriel-orrery-NN/`, contact sheets `sheet-a.png` (and `sheet-b.png`) in each.

| Pass | What was wrong | What changed |
|---|---|---|
| `oriel-orrery-01` | A solid bronze spoke straight under the lane read as a road the ships were flying along. Each ring stood upright 27 m tall and filled the whole frame, hiding most of the rail and the moon behind it. The rings' lamp panels bloomed to cream: green and amber could not be told apart. Oriel was a soft, washed-out cream disc with long streaks behind the last three rails (a 1024 face over a globe that big on screen). A whole moon was drawn twice with the split's amber (a mesh with fewer parts than materials); caught before the stills by reading the code | Spokes became open girders (two chords, braced) off the lane's middle and deeper; gears under each gate row turn (`Turning`); each ring leans back 28 degrees over its rail, so it arches over the moon's path and its lamp band faces the camera; panel albedo darkened and the light made a saturated green and amber; Oriel made smaller and farther (radius 380, 800 m down) with a 2048 face, darker; every moon's mesh declares all three slots |
| `oriel-orrery-02` | Gates read at a glance: green or amber, the moon seen past the ring as it comes. Oriel crisp, but its cloud covered a third of the face in flat cream blobs and read as camouflage | Cloud thinned to a few curls, half see-through |
| `oriel-orrery-03` | Oriel reads as a small globe of gold and sage land and green-blue seas with its fibres across it, under the last rails. At full size the gates' lit panels bled green speckle over the bronze rims: single texels of the coat that passed the panel colour test | A texel of the panel mask lights only where seven of its nine neighbours are panel |
| `oriel-orrery-05-before` (November 14, the same scene rendered with `-look-far 1000`, the game camera's far plane; 9 `lee-N` views added) | **No planet at all**: every Oriel still before this was rendered at 1500 m and showed what no player sees. Its face where it did show (`-04`) was flat camouflage blotches. At every waiting place one ring filled the frame and hid the moon going by (`lee-3`) | Oriel rebuilt inside the far plane (above, Rules of construction); its map repainted (sharp coasts, a fine mosaic on land, ranges and rivers, no cloud streaks); its light laid in its vertices from the scene's sun; fibres finer, meeting at a glass knot. Each ring drawn slim and open at the top with a lattice arch; the truss drawn out to it; the plinths' girder made a rod. The rail's lamps keep the moon's lee steady green |
| `oriel-orrery-07` | Oriel half the frame at the far gate and behind the last rails' gates; the moon passing the middle gate is seen whole through the ring's open top (`lee-3`). The face read a little muddy, and the plinths' girder lay across the bottom of every waiting frame like a plank | Land brighter and paler; girder thinned |

What is weak, plainly:

- **The machine moves only in play.** A still shows every moon, lamp and gate as it stands the moment the zone opens; the running lamps, the sequence of a row going amber and green, the moon sweeping and the gears turning are tested (`OrielWorldTests`), not seen in a still.
- **At the waiting place the rail is at the frame's top edge.** The camera sees about 12 m north of the ship and the rail is 13 to 15 m north of where the routes wait, so the moon is seen passing across the top of the frame (its lower half) and the rail's lamps sit on the edge; the gate's own colour is still the nearest signal. Nothing drawn can move the rail nearer (the routes and the rule hold it); only a camera that drew back at gates could.
- **Oriel's limb comes in on its night side.** The scene's sun is high in the south-east, so the part of the planet that rises into the frame first faces down and away from it; its lit face fills in as the lane goes on.
- **Nobody has flown it in a player.** The tests fly the lead through gates and wait on the clock; the nine minutes, the green wave, a forced rail's fight and the shards have not been played.

## The Rail Warden and the Moonshard as defined (combat engineer)

*The combat engineer's answer to the section above (November 12). Definitions: `OrielBuilder.Enemies.cs`; behaviours: CHAPTER_GUIDE section 5 (chapter 7's rows) and 15.23; tested on a bare flight plane in `T/PlayMode/OrielCombatTests.cs`.*

- **`RailWarden`** is a `Rider` on a rail: placed as written above (facing along the rail toward the lane's middle, `faceAsPlaced`), it rides a line from where it stands **ahead along its heading** (18 m) and fires only down that line: a ship on it, ahead of the drone within 16 m, sees the line marked 3 m wide for .9 s, then a bolt comes down it (96, Beam). A ship beside the line is never fired at, however near. It glides along its line toward the ship's place on it. Asleep on show it does nothing; woken it fights as above. If the rails themselves are wanted as its line, lay a `ForceLine` along the rail at the plane within 20 m of each warden and it rides that instead (`end` the far end; the rail's curve is fine as a chord a lane wide). Integrity 950, weak Pulse, resists Beam, 90 XP.
- **`Moonshard`** is a `Charger` on a track (`tune.count` 1): begun as written above (2 m off the moon's leading face, facing the moon's way along the rail), it marks a lane **ahead along its heading**, 30 m long and 3.4 m wide, for 1.1 s, runs it at 20 m/s whoever is about, strikes what it touches inside the lane (98), and is spent at the end: it breaks up and pays nothing. A wall in the lane ends it there. Shot down before it runs (180 integrity, a few shots), it is a kill (20 XP). So the stretch ahead of a moon is the marked danger and its lee is calm, as the section above wants, provided the shard (20 m/s) outruns its moon.

## The gate names (labels engineer's request, D356, November 22)

From the play camera (Flight profile: 26 m, 48 degrees, fov 30) the frame at the plane runs from about 7.6 m behind the ship to 12.3 m ahead of it. THE STEWARD, THE WEIGHER and THE LAMPLIGHTER hang 10.8 m over the middle gate's keystone (`keystone+Vector3.up*1.2f`), so from 14, 8 and 3 m short of their gates they stand above the top of the frame (a probe in run nov22-hud1, since removed); they are only on screen in the wide view over Ishen's talk, small and behind the trusses (nov22-whole s38/080, s38/087). Asked of the zone's engineer: lay each name on the plane again, centred on the lane, 5 m before its middle gate: `WorldBuilder.Label(rail.name.ToUpperInvariant(),At(LaneGates[1],rail.Radius+rail.GateOut+5,Plane-.7f),2.4f)`. The two reasons it was moved up on November 21 are answered at run time: a ship over a word lying under the plane now takes it to its trace (`SignFade`, D356), and at 5 m out the ring's lit rim is no longer behind its letters (at 2.4 m it read 2 to 1); `SignStyle` gives it the full plate if what is behind it is bright. It then reads from about 17 m short of the gate until the ship crosses it.
