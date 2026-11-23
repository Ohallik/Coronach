# The Bridle Foundry - the charging floor, the casting gallery and the heart

Functional brief, written before construction (November 12, world engineer C, chapter 8). **MAP_EYE_TEST: blockout PASSED with repairs; dressed (editor) PASSED with repairs, the gangway and the tank's pit still over the plain-floor limit (see Review); look pass (heat, machinery, three floors, the release walked) PASSED with repairs in editor stills and the test's frames; final in-player pass OPEN** (an agent's review; nobody else has looked). Chapter brief: [ch08-harrowgate.md](../chapters/ch08-harrowgate.md); script wiring: [ch08-manifest.md](../writing/pending/ch08-manifest.md). Builder: `S/Editor/World/HarrowgateBuilder.Foundry.cs`. Mechanics: [CHAPTER_GUIDE.md, "World mechanics"](../CHAPTER_GUIDE.md) (15.27 is this zone's; it also uses 15.3, 15.6, 15.8, 15.17 and 15.26).

## Purpose and formation

The Muster's forge on Scoria, cut into the basalt where a magma vein runs close under the surface: "the great work", where Bridles are made. It is built on three floors, one under the next, and walked down: the **charging floor** (ore, scrap and flux go into the furnaces; the furnaces' tapholes run molten iron in channels across the floor, and ladles on overhead rails carry it), the **casting gallery** (cage ribs are poured into long moulds from ladles and from pour drones on rails), and the **heart** (the furnace's root over the vein, where a finished Bridle hangs with a juvenile Choir in it and the Slag Colossus walks).

**The route's shape is a descent.** In by the mouth from the Flows, across the charging floor, down a stair to the gallery, down a stair to the heart. The one rule: **hot metal runs on a clock, and a marking always says where it will be; coolant stops it.** Metal is only ever dangerous inside a marking (orange hatch with a rim on dark iron), and the marking is laid before any pour. Two kinds of pour: a **tap run** (a furnace's molten run in a floor channel, always bright: a wall nobody can cross) and a **ladle line** (a ladle on an overhead rail that runs out, tips over a floor trough and pours on a fixed timetable a player can read by watching the ladle come). A **coolant sluice** (a red wheel) floods one channel and drains another: "every wheel opens one way and arms another" (Idra, `IdraFirstSluice`). The rule is introduced with nothing awake (the first wheel and the first ladle line), tested (the second wheel stands behind a press, and arms it: the way back is under a drop hammer on its timetable), then combined with enemies (hounds through the live press; the gallery's ladle line across a fight with pour drones).

## Users and access

The Muster's guard and its machines; the Unsworn smiths were sent out when the party was seen on the Flows. Nobody friendly is here until the Colossus is down: then Tovra's sixty smiths file in by the door behind the dais, Brekk with them, and Vessa comes out onto the heart's gantry to watch. The party arrives on foot from the Scoria Flows at the Foundry's mouth (spawn `Arrival`) and leaves the same way (`Foundry mouth — The Scoria Flows`, to the Flows' spawn `Foundry`: **the Flows' engineer owns that spawn**).

## Layout

About 84 x 295 m; about 340 m of route from the mouth to the heart's door, plus the press detour (50 m there and back) and three pockets. x runs east, z north; heights are world y. Floors: the charging floor 0, the casting gallery -5, the heart -10.

```text
 z 287      [ cage dais ]  the Bridle and its juvenile; the gantry along the north wall (-5)
 z 273  .-''''''''''''''''-.
        |     THE HEART     |    floor -10, C = (0, 251), r 22 (contract below)
        |        (C)        |
 z 229  '-.____[door]____.-'     seal 227
 z 217   [ LANDING -10 ]  checkpoint Heart, cradle, SelaCagedRoad
 z 202       | stair |
        +-----------------------------+
        |  CASTING GALLERY (-5)       |  Vessa Korl's floor (contract below)
        |                             |
 z 157  +----------[door]-------------+  checkpoint Gallery, cradle (south of the door)
        |  GALLERY SOUTH  ~~ladle L2~~ |== passage ~~ladle L3~~ == PATTERN LOFT (cache)
 z 135  +-----+-----------------------+
              | stair |
 z 120  [stair head ~~tap B~~]
        +-----------------------------+-------- press run --[PRESS]--+ WHEEL BAY (wheel 2)
        |  CHARGING FLOOR (0)         |                              | QUENCH TANK (cache)
 z  81  +--------+  gangway  +--------+
                 | ~ladle L~ |
 z  63  +-----------------------------+
        |  WHEEL HALL  ~~~tap A~~~    |  wheel 1 (west side)
 z  40  +-----------------------------+
 SKIP   |  YARD (hounds)              |
 STORE==+                             |
 z   8  +-------+  mouth  +-----------+
                 [arrival]
```

| Area | What it is for | Size | Level | Combat |
|---|---|---|---|---|
| Mouth | The arrival from the Flows: the way home, a free cradle | 14 x 17 m | 0 | none |
| Yard | The Forge Hound met: open floor with walls to end its leaps | 36 x 32 m | 0 | `Foundry_Yard` |
| Skip store (optional) | Off the yard's west side: ore skips on their rails, a pair of pikeguards, a footlocker | 17 x 20 m | 0 | `Foundry_Skips` |
| Wheel hall | The rule introduced, with nothing awake: tap run A across the whole hall, the first wheel on this side of it, ladle line L beyond | 36 x 24 m | 0 | none |
| Gangway | Between the ore bunkers, 10 m wide: ladle line L pours across it on its timetable | 10 x 19 m | 0 | none |
| Charging floor | The furnaces' mouths along its north wall; ore heaps; a fight | 44 x 33 m | 0 | `Charging_Floor` |
| Press run | A passage east to the second wheel, under a drop press (quenched and still until the second wheel; then on its timetable) | 14 x 8 m | 0 | `Press_Bay` (after wheel 2) |
| Wheel bay | The second wheel ("behind the press") | 12 x 20 m | 0 | `Press_Bay` |
| Quench tank (optional) | The press's coolant tank, deep until wheel 2 drains it; a stair down; a cache on its floor | 10 x 12 m | -1.8 | none |
| Stair head | Tap run B across the head of the stair down: the way on, shut until wheel 2 | 12 x 8 m | 0 | none |
| Charging stair | Down to the gallery | 8 x 16 m | 0 to -5 | none |
| Gallery south | Moulds in the floor, ladle line L2 right across the room, two pour drones on rails | 52 x 19 m | -5 | `Gallery_Drones` |
| Pattern loft (optional) | Through a passage that ladle line L3 pours across: the patternmaker's store and a chest | 12 x 20 m | -5 | none |
| Casting gallery | Vessa Korl's floor (contract below) | 52 x 46 m | -5 | `VessaKorl` |
| Heart stair | Down to the heart | 10 x 16 m | -5 to -10 | none |
| Landing | The heart's door: the checkpoint, a free cradle, the talk | 24 x 8 m | -10 | none |
| The heart | The Slag Colossus's arena (contract below) and the cage on its dais | r 22 | -10 | `SlagColossus` |

**Travel** (about 6.7 m/s): about 390 m with the press detour: a minute of walking. With seven fights, the two timed lines, two wheels, three pockets, four talks and the boss it is about 25 minutes, the chapter's dungeon share.

## The pours, the wheels and the press (the zone's mechanic)

| Thing | Where | What it does | Before | After |
|---|---|---|---|---|
| Tap run A | Wheel hall, a trough z 56.6 to 59.4 from wall to wall, fed from a taphole in the east wall | Always pouring: marked steady, a waist-high seal over it (the paths can't cross it) | bright | wheel 1 (`vask.sluice1`): coolant floods the trough, the run crusts dark, the seal goes |
| Wheel 1 | Wheel hall, west side (-12.5, 50.5); worked from (-11, 49.4) | "Turn the sluice wheel", `vask.sluice1`, bark `sluice`; `IdraFirstSluice` (crossing at z 46) teaches it first; Tovra's `hail.sluice` from the speaking tube | | floods A, drains L's trough (L's ladle arms) |
| Ladle line L | Gangway, a trough z 71.6 to 74.4 across its 10 m, ladle rail at z 73 overhead | A ladle runs out from its bay, tips and pours on a 10 s timetable; marked 1.4 s before the pour and through it | quenched: coolant stands in the trough, the ladle hangs at rest | pours on its clock |
| Press | Press run, a stamp 4 x 8 m on (28, 96) | A drop hammer: the ram climbs, the square is marked, it slams; every 6 s | quenched: the ram held up, steam at its seals | wheel 2 (`vask.sluice2`): stamps on its clock |
| Wheel 2 | Wheel bay (43, 100); worked from (41.4, 99.2) | `vask.sluice2`, bark `sluice` | | floods B (the way down), drains the quench tank (the press arms; the tank's floor and cache come out) |
| Tap run B | Stair head, a trough z 115.6 to 118.4 across it | As A | bright | wheel 2: crusts, its seal goes |
| Ladle lines L2, L3 | Gallery south (across the whole room, z 139.6 to 142.4); the loft passage (x 26.6 to 29.4) | As L, offset clocks | | always on |

- A pour strikes a hero inside its marking while it pours: a fifth of his Integrity, never the last of it (the place's damage: no enemy is its source), and throws him out of the near side; once a pour. Outside the marking, nothing. The metal crusts dark within a second of the pour stopping, and the marking goes with it: **marked means hot**.
- **The marking wins over the glow**: a pour's marking is the trough plus a margin on each side, so its rim always lies on the dark iron kerb, never on bright metal; the metal is drawn under the marking (lower in the floor and earlier in the queue) and its glow is held under the bloom threshold's reach at the rim. Nothing else on the floor is orange, red or purple: the furnaces' light is on the north walls.
- `vask.sluice3` is not used here: the heart's sluices are the boss contract's.
- A save loads every pour, the press and every channel in the state its flags say, with nothing played.

## Identities

Scene `BridleFoundry` (GroundCombat, its own profile `Foundry`, no sky: under a roof, chapter 8, threat 1). Music: Alien Pulse; the Colossus: Glitch Boss (its definition). Light: `FoundryLooks` in the builder. Ambience: `AMB_BridleFoundry` when it exists.

## Coordinates (for routes and tests)

All world x, z (y is the floor there: 0 on the charging floor, -5 in the gallery, -10 in the heart). As built: the numbers are `HarrowgateBuilder.Foundry`'s constants. Spawns are `SpawnPoint` ids; a point to press is where a hero stands.

| What | Id or prompt | x, z | Flag / note |
|---|---|---|---|
| Arrival spawn | `Arrival` | 0, 1 | |
| Way home | `Foundry mouth — The Scoria Flows` | gate at 0, -6.6 (stand 0, -4) | to ScoriaFlows spawn `Foundry`; open |
| Mouth cradle | `Mouth cradle — repair and save` | 5.5, 4 (stand 3.9, 4) | free, saves at `Arrival` |
| Yard | `Foundry_Yard` | 0, 26 (34 x 28) | 2 ForgeHound from -7, 35 and 7, 35.5 |
| Skip store (optional) | `Foundry_Skips` | -30, 24 (15 x 17) | 2 MusterPikeguard (pair) at -31, 27; cache `vask.foundry.skips` "Charging-floor footlocker" at -35, 30 |
| First wheel talk (crossing) | `IdraFirstSluice` | box at 0, 46 (36 x 3) | sets `vask.sluiceRead` |
| Wheel 1 | `Turn the sluice wheel` | housing -13.2, 50.4; stand -11.6, 49.4 | `vask.sluice1`; bark `sluice`; marker `COOLANT_OPEN vask.sluice1`; Tovra's `hail.sluice` from the wheel (range 7, after `vask.sluiceRead`, heard `vask.sluiceTube1`) |
| Tap run A | | trough z 57 to 59 across the hall (x -17.6 to 17.6); marked strip z 56.4 to 59.6 | sealed until `vask.sluice1` |
| Ladle line L | | trough z 72 to 74 across the gangway (x -5 to 5); strip z 71.4 to 74.6; rail z 71.7, ladle's bay at x 10 | armed by `vask.sluice1`; 10 s: out 1.6, marked 1.4, pours 2.4, back 1.6, rests 3 (safe to cross for 6.2 s of every 10) |
| Charging floor | `Charging_Floor` | 0, 97 (42 x 30) | 2 MusterPikeguard (pair) at -4, 106; 1 ForgeHound at 12, 104 |
| Press | | square x 26 to 30, z 92 to 100 | armed by `vask.sluice2`; 5 s: marked 1.2, slams, holds .4, climbs 1.1 |
| Wheel 2 | `Turn the sluice wheel` | housing 44.2, 100; stand 42.4, 99.2 | `vask.sluice2`; Tovra's second `hail.sluice` at the stair head (-13, 115, range 9) or the press run's mouth (20, 96, range 7), whichever comes first, after `vask.sluice1`, not after `vask.sluice2` (heard `vask.sluiceTube2`) |
| Press bay fight | `Press_Bay` | 40, 96 (14 x 22) | needs `vask.sluice2`; 2 ForgeHound from 16.5, 94 and 16.5, 98.5 (through the press run) |
| Quench tank (optional) | | floor -1.8 at x 35 to 45, z 110 to 119; stair x 41 to 45, z 104 to 111 | coolant -0.4 (deep) until `vask.sluice2`, then -2.4; cache `vask.foundry.tank` "Quench-tank strongbox" at 38, 116 |
| Tap run B | | trough z 116 to 118 across the stair head (x -18.6 to -7.4) | sealed until `vask.sluice2` |
| Stair down | | x -17 to -9, z 119 to 136 | 0 to -5 |
| Gallery south | `Gallery_Drones` | 0, 146 (50 x 19) | 2 PourDrone at -13, 151 and 13, 151 (yaw 180, on rails), 1 MusterPikeguard at 0, 152 |
| Ladle line L2 | | trough z 140 to 142 across the room; strip z 139.4 to 142.6; two ladles on one clock (bays x -21 and 21, pour at x -7 and 7) | 9 s, phase 3 |
| Drone rails | `ForceLine` | x -13 and 13, z 144 to 154.5 (width 1.8); girders 3.4 m over | |
| Loft (optional) | | x 30 to 42, z 134 to 154, through the passage x 25 to 31, z 141 to 147 | ladle line L3 across the passage (trough x 27.7 to 29.7; 8 s, phase 1.5; bay north of it); cache `vask.foundry.loft` "Patternmaker's chest" at 39, 150.5 |
| Gallery checkpoint | spawn `Gallery` | -3, 149 | cradle `Gallery cradle — repair and save` at 6.5, 149 (stand 4.9, 149), saves at `Gallery` |
| Gallery talk (crossing) | `VessaGallery` | box at 0, 161 (50 x 3) | sets `vask.vessaMet`; saves at `Gallery`; previews `VessaKorl` |
| Casting gallery | `VessaKorl` | G 0, 180 | contract below |
| Heart stair | | x -4.5 to 4.5, z 202 to 218 | -5 to -10 |
| Heart checkpoint | spawn `Heart` | -3, 220 | cradle `Heart cradle — repair and save` at 7.5, 220 (stand 5.9, 220), saves at `Heart` |
| Heart door talk (crossing) | `SelaCagedRoad` | box at 0, 225.2 (8.8 x 2.2) | sets `vask.cageSeen`; saves at `Heart`; previews `SlagColossus` |
| The heart | `SlagColossus` | C 0, 251 (r 22) | contract below |
| The Bridle | | 0, 279 on the dais (x -12 to 12, z 270 to 287) | the young road inside (`CagedChoir`, pull 9, ease 4; freed by `vask.juvenileFreed`) |
| The release | `AfterTalk` `TovraRelease` | | on `bossdown.SlagColossus`; heard `vask.juvenileFreed` (its last line) |
| Brekk by the anvil | `Stand by the anvil` | anvil at -7.5, 275.5; stand -6.1, 272.9 | needs `vask.juvenileFreed`; node `BrekkAnvil`; sets `vask.anvilSeen`; no bark |

## Encounter plan

| Fight | Where | Pack | Where the player stands to answer it |
|---|---|---|---|
| `Foundry_Yard` | Yard | 2 Forge Hound | The yard is 36 m across with walls on three sides: a hound's 7 m lane always ends in a wall a step from it, and there is room to roll its lane, strike it in the air, or brace |
| `Foundry_Skips` | Skip store (optional) | 2 Muster Pikeguard | Round the end of the pair: the store is 17 m wide, the skips on their rails along its west wall are cover |
| `Charging_Floor` | Charging floor | 2 Muster Pikeguard, 1 Forge Hound | The floor is 44 x 33 m and open: break the pair from its end while the hound's lanes run east-west to the walls |
| `Press_Bay` | Press run and wheel bay, after wheel 2 | 2 Forge Hound | They come through the press run from the charging floor while the press stamps: fight them in the bay, or let the press's square stand between |
| `Gallery_Drones` | Gallery south | 2 Pour Drone (rails north-south at x -13 and 13), 1 Muster Pikeguard | Off a drone's rail and off the ladle line's strip when it is marked; the room is 52 m wide |
| `VessaKorl` | Casting gallery | Vessa Korl and her squad | Contract below |
| `SlagColossus` | The heart | The Slag Colossus | Contract below |

## Camera

The fixed camera looks north-north-east from the south-south-west. Every room is walked roughly north, its south and west walls kept under the sightline to the floor beyond (the Waystation's rule), its north and east walls tall: the furnaces' mouths, the pipes, the gantries and the moulds' racks stand there. Ladle rails run 4.9 m over the trough, so the ladle running out is in the frame of a hero waiting at the strip's edge (at 6.6 m it ran above the top of the frame and only its stream showed); each rail stands on a post at a wall's top or its bay's ledge, never on the floor.

## Review

Reviewer: Claude (world engineer C, chapter 8), from editor stills, from frames the zone's own test takes from the game's camera in a mirror, and from plans, November 12. Nothing here has been walked in a player.

**Blockout** (`Builds/quality/look/foundry-greybox-01/`). The route reads as a descent: the mouth and the yard, the wheel hall with its run across it, the gangway between the bunkers, the charging floor, the stair down, the gallery's two rooms, the stair down, the heart. Every fight's floor has room for its answer. Repaired before dressing: the ladles hung above the frame's top (only their streams showed) and three of them rested inside wall masses (rails lowered to 4.9 m, the ladles shortened, ledges cut where they wait); the gallery's ladles crossed the drones' girders (the girders start north of the ladle line); the press bay's fight box took in the press's own square (it is the bay's floor now); the coolant's ripple read as camouflage (finer, fainter).

**Dressed** (`foundry-dressed-01` to `-05`; `foundry-drained-01` with both wheels' work stood in the water). Found and repaired while dressing:
- the ground kit's brown grit and crazy paving made the forge a garden path: dark basalt slabs now, about one in seven an iron tread plate and one in seven cracked, worn through to the bedrock in patches, black slag grit only at the wall feet and kerbs, moulding sand only round the gallery's moulds;
- the ground kit painted a pool of cold lamp light under every light, the pours' own included, which lay as white blobs in the troughs (the pours', furnaces', vents' and cage's lights are named apart from the work lamps);
- a crate stood with its corner in a pour's strip (clutter is kept off a trough by its own size);
- the press's held coolant pipe was a white rod (a dull teal pipe); the ingot stacks read first as crates, then as marble (a sand-cast skin of their own); the skips' load was the moon's cyan crystal (dulled to ore); Tovra's anvil was the Lattice bench with its cyan top (a smith's anvil built in code); two station consoles with glowing screens were in the clutter (taken out of its lists);
- the ladles, the ram, the drones' girders and the loft's patterns were drawn hanging in the air (`QaPlanWorldTests.NothingFloatsOrSinksIn`): each ladle line is one piece with its rail on posts, the ram hangs in the press's frame, the girders stand on posts, the patterns stand on the floor;
- clutter stood inside the heart's floor (kept 2.5 m outside its rim).
- a spoil heap's solid was the box round its whole drawn bounds, an 8 m square for a mound fitted by its 2 m height, whose corners stood across the yard's way and the wheel hall's (chapter 8's replay routes' leg test, November 14): every heap and rubble pile now carries a prism fitted to its outline above ankle height (`Foundry.FitHeap`, as `XylosBuilder.FitHull` fits a barge), its mesh saved beside the scene. The two heaps by the main way stand 1.3 and 1.9 m off its line; what is drawn is unchanged.

`python tools/look_density.py Builds/quality/look/foundry-dressed-05 --match BridleFoundry --max-blob 0.22`: every play still at or under .22 (worst wheel-two .22, skips .19, landing and loft .18; the charging floor, the gallery and the heart .04 to .09) but two: the gangway (`ladle-l`, .23: its east bunker's masonry in shade) and the quench tank photographed flooded (`tank`, .29: a state nobody stands in; drained, in `foundry-drained-01`, its pit walls make it .38). The two overviews are over, as wide views are.

**The pour seen from the game's camera** (`BridleFoundryTests.APoursMarkingReadsOverItsGlowFromTheFixedCamera`, frames in the test mirror's `Builds/quality/focused/foundry-pour/`): the marking's rim lies on dark ground beyond the kerbs and reads as the danger's orange through the warning and the pour (rim about .93 to .96 red against the floor's .19 to .22 luminance), its hatch over the lit metal; the metal is pale yellow and only lit under the marking.

**Look pass** (November 12, after the coordinator's review: the heat, machinery in frame, three floors three rooms, stills of the runs as in play, the cage's and the ore's glow, the smiths appearing). Stills: `foundry-hot-03` (built with `-foundry-hot`: the runs at their ember with their streams falling, the gangway's and the gallery's west ladles tipped mid-pour), `foundry-dressed-09` (the zone as saved); contact sheets `sheet-1-charging.png`, `sheet-2-gallery.png`, `sheet-3-heart.png` in each. `foundry-hot-01` and `foundry-dressed-06` are the first try, kept for what they showed:
- **Heat, off the floor.** The charging floor's three furnaces are round iron shells standing half out of its north wall (ring main and tuyeres, bands, a hood), their mouths grated and breathing (`Breathing`), each with its light, lit smoke and embers going up; charge buckets of ore and a slag pot between them. A crucible hangs over the charging floor's north strip and another over the gallery's, each on a travelling crane, their open tops full of metal and breathing, embers and lit smoke off them. The heart's vents breathe slower, with embers climbing the rim wall, smoke with its light in it under the vault, and a fire door at each duct's root. The newest finished rib against the gallery's north wall and the newest ring in its rack keep a dull red in them. The tap runs glow ember, deep red, and warm a little in a surge every seven seconds; a ladle's trough warms to ember under its marking as the ladle tips and is the melt's pale yellow-white only while it pours. Where the forge's light reaches the floor (before each furnace, under each crucible) a soft pale pool is painted.
- **Repaired while doing it:** the cranes first ran across the rooms' middles at seven and a half metres, and from the fixed camera the girder crossed the frame as a band that hid the heroes, the fight and the furnaces behind it (`foundry-hot-01`, `furnaces`): the girders are at ten and a half metres now, above the frame from anywhere on the floor, and the crucibles hang near the back walls where nothing the camera looks past them at is a fight's floor. The runs' first ember rendered as a marking's orange (`foundry-hot-01`, `tap-a`): it is a deep red now, about half as bright. The heart's slag and the gallery's sand first lay in tight patches that read as camouflage: broad drifts in the heart, and sand over most of the gallery with a slab walkway left down its middle. A cooling ring read orange a metre off the floor (dimmer and redder now). The quench troughs' water drew white (a still dark teal). A fuel tank's copper bands stood in front of the gallery's half-made Bridle (out of the clutter). The camera test's first run found the floor before the middle furnace's mouth lit to within .11 of a marking's rim (the furnaces' floor light and pool are held lower now). The gantry's east door, drawn apart, read as sunk in the wall it is set in (it is the gantry's own piece).
- **Machinery in frame:** the ore bunkers' faces in the wheel hall's north wall either side of the gangway, chutes over every head and ore spilt under them; built ore skips on the yard's, the store's and a new charging-floor rail (the kit's cart, with its cyan crystal, is gone); ore heaps in the skip store; a chain hoist in the yard's north-east corner; a quench trough by the press run's mouth and another in the gallery by a rack of rings cooling; clamps for fitting and a drum of Bridle wire along the gallery's east wall; a Bridle half made in its jig in each of the gallery's north corners (the finished ribs moved east of the north-west one); in the heart two winches on the dais with their chains taut up to the cage's shoulders, and the ribs of broken Bridles thrown down against its west wall.
- **Three floors:** the charging floor's stone a little brown, ore and slag spilt round its machines; the gallery's paler, under moulding sand but for its walkway; the heart's dark, slag in broad drifts and scorch round the cage.
- **The Bridle's orange** was painted into its colour map, not its emission (whose map is black): the Foundry draws it with that map's orange taken out, to a pale cold grey-blue on the cage the young road is in and to iron grey on the half-made ones and the clamps.
- **The release** is walked, not placed (see "After the Colossus").
- `BridleFoundryTests.AMarkingReadsWhereverTheForgeIsHottest` lays a danger marking at every hot place a hero can stand (before two furnace mouths, under both crucibles, by the charge buckets, by the cooling rings, under the heart's vents) and reads tap run B's own marking at its ember and at its surge, each measured as the pour's is; frames in the test mirror's `Builds/quality/focused/foundry-heat/`. Before the furnace repair, rims .47 to .57 against floors .20 to .41 (the gallery's sand the brightest floor, still .15 under its rim) but the middle mouth's.

What is still weak:
- **Nothing here has been seen in a player**: the pours, the press, the young road's cadence and its going, the wheels turning and the coolant moving, the drones on their rails, the furnaces' breathing, the embers and smoke, the smiths' walk in and both fights are runtime and only in the test's frames or not at all.
- **The release:** Tovra stands by her anvil with Brekk rather than sitting on it (there is no sitting clip); the young road goes after `TovraRelease`'s last line, not under "There." and "Gone."
- **The young road is the Cantor's head and links**, small and pale: a stand-in, as the brief allows.
- **The tap runs' coolant and the drained tank** are seen only in `foundry-drained-01`.
- **The work lamps' cold pools** have never been painted (`GROUND_LAMP_POOLS placed=0 of 7` since the first dressing): each lamp stands at a room's edge and the ground kit lays a pool only where the whole of it is floor. The lamps' own light still reaches what is near them.

## Casting gallery contract

*The first combat engineer's section: what the Vessa Korl fight needs from the room (floor, cover, rails for her pour drones, where her squad stands, where she goes at half Integrity, the door). The world engineer builds to it and writes the coordinates as built underneath.*

Written November 12 by the first combat engineer, to the plan above (the gallery 52 x 46 m at -5, door at z 157, the heart stair at z 202; centre **G = (0, 180)**). The fight's moves are `S/Combat/BossController.VessaKorl.cs` and its numbers that file's `VessaTune`; her definition and her squad's are `HarrowgateBuilder.Enemies.cs`. Its tests (`T/PlayMode/HarrowgateCombatTests.cs`) build a stand-in of this room on a bare floor in the proving yard, so nothing here waits on the Foundry. Positions are from **G**, +z north.

**How the fight uses the room.** Vessa stands at the far end of the gallery with her squad, a pikeguard pair, before her. She fights at lance's length, 6 to 10 m off: a heat beam down a marked line, a marked fan her beam sweeps across (then the lance vents: her first opening), and her call to the pikes (her own braced thrust, and every pikeguard of hers thrusting at the same hero). **At half her bar she falls back** (her shift, and the writer's `bossphase.VessaKorl`: "She's pulling back to the heart. Don't follow her blind."): she sets the flag her talk plays on, writes off her squad in front of them (`VessaFallsBack`), runs back to hold 5 m before the gantry door to the heart, and lays the floor across the way to her ("Pour drones, tip on my mark. I've costed the floor.": three marked strips that burn after they land). From the door, the lance hot: the beam leaves its line burning, the strips again, and she brings the lance down overhead and wedges it (her second opening). When her bar runs out she walks out through the gantry door, and her squad fights on. The beam is light and heat: **solid cover between her and a hero takes it**, which is what the moulds' racks are for.

*Changed November 12, within the hour, after reading the barks the writer gave her: she falls back at half (the shift), not at nought. Her talk's flag and the gantry door's place below are the ones that matter.*

*Checked against the builder, November 12, 17:00: `HarrowgateBuilder.Foundry.cs` stands everything here as asked (her post, the pair, the four racks, both seals, the `Gallery` checkpoint, `vask.vessaMet`, the gantry door's `BossExit` at (16, 201)) except her talk, which still waits on `bossdown.VessaKorl` with `delay` 1.2: make it `flag=BossController.FallingBackFlag` (`vask.vessaFallsBack`), `delay=1.6`. As it stands the talk plays at nought, as she leaves, instead of at half as the barks say.*

| Thing | What the fight needs | Why |
|---|---|---|
| Floor | Walkable and near level over the whole 52 x 46 m; no step over .3 m inside **r 16** of G; nothing solid inside **r 5** of G or within 3 m of her post | Her beam is 16 m long and her fan reaches 9.5 m; a dodge sideways out of a 1.8 m line must never meet a step |
| Cover | **Four** mould racks (the cage-rib moulds stood on end, or their racks): solid static boxes **1.8 to 2.4 m tall**, about **1.2 x 4 m**, long side east-west, centred at **(-9, 174)**, **(9, 174)**, **(-11, 188)**, **(11, 188)** (G-relative: (-9,-6), (9,-6), (-11,8), (11,8)). Occlusion fade on (they are under 2.5 m, so `SceneryOcclusion` is optional); in the navigation bake | Behind a rack a hero is out of her beam and her sweep. None in the middle: the pair and the party need the middle to turn in |
| Her post | `Spawner` for **`VessaKorl`** (count 1) at **(0, 190)**, yaw **180** (facing the door), `faceAsPlaced=true` | The far end: the party crosses the floor toward her and the camera has her in frame with the north wall behind |
| Her squad | A second `Spawner` child of the same encounter, **`MusterPikeguard`**, count **2**, radius **2.1** (so they stand **4.2 m apart**, the pair's own spacing), at **(0, 185)**, yaw 180, `faceAsPlaced=true`; the encounter's `spawners` holds both | Two woken within 7 m of each other pair up by themselves (`EnemyGuardian`'s pair). They are the encounter's own, not her summons, so they stay when she leaves and the door waits for them |
| Door | Encounter id **`VessaKorl`** (the main quest's step 6 points at it), trigger covering the floor (about **50 x 6 x 42** on G), `requiredFlag` **`vask.vessaMet`** (the `VessaGallery` crossing just inside the south door, at about z 161), `checkpointSpawn` **`Gallery`** (your (-3, 149), on navigation, the cradle beside it). Two `Membrane` seals: the **south door** (z 157) and the **heart stair** (z 202), both shut through the fight, strands drawn instead of the cube. `previewPoint` just inside the south door with a radius that takes in her post, so she and the pair stand shown and inert until the talk | The shared door tests (checkpoint, retry, no way out mid-fight). Her boss sheet names zone `BridleFoundry`, door `VessaKorl`, door flag `vask.vessaMet`: write back any other flag the door waits on |
| Where she goes | A **`BossExit{boss="VessaKorl"}`** at the **gantry door** in the north wall, **(16, 201)**, on the floor at -5; `sinks=false`. The door is a drawn doorway onto the gantry that runs to the heart (the "gantry along the north wall" of your layout), **barred to the party** (solid, not a way out). At half she runs back to hold **5 m before it, on the line from where she stands** (from her post, about (11.9, 198.2)): keep that ground walkable, clear of racks, and 7 m of open floor south of it for her strips. At nought she walks through it over the 3.6 s of her leaving (`BossDeparture`), turning smaller as she goes | Falling back and leaving for the heart under her own power (`BossController.Ending.Leaves`): no body on the floor. She is on the heart's gantry for the release (your After the Colossus) |
| Her talk | `VessaFallsBack` **at half her bar**, the moment she falls back, **with her squad still standing** (she writes them off in front of them). Her shift sets **`vask.vessaFallsBack`** (`BossController.FallingBackFlag`); an `AfterTalk` as it was waits for every fight to end, so `AfterTalk` gains **`duringFight`** (mine, `S/World/AfterTalk.cs`, done): stand `AfterTalk{flag="vask.vessaFallsBack", node="VessaFallsBack", speaker="Vessa", heardFlag="vask.vessaBeaten", delay=1.6, duringFight=true}` in the gallery. A conversation stops every enemy's clock, so nothing strikes through it, and she waits it out at her door before she lays the floor. A defeat after it reloads at the door's checkpoint, saved before the fight, so the talk comes round again on the next attempt | The script's order (manifest 2: "At half Integrity (`Npc.Say`)... She writes off her squad aloud and wakes the Colossus"); `vask.vessaBeaten` is the main quest's step 6 and is set by the talk's own last line. The objective panel says "Drive Vessa Korl out of the gallery" for her second half (her `fightGoal`), whatever the main quest's step says |
| Her squad after | Nothing to build: the pair fights on; the door opens and `clear.VessaKorl` is set when both are down | |
| Music | Nothing to build: her fight plays the shared boss track (her definition names none: "Alien Boss Battle", as her sister's did), and the zone's own (Alien Pulse) comes back the moment she leaves | |
| Light | Her lance is drawn in code with its own glow (white-amber, going white-hot at her shift): nothing on the floor of hers is orange, red or purple but her markings. Keep the gallery lit from fixtures so the north half (her post) reads | D217 |
| After the fight | On a load with `bossdown.VessaKorl` set nothing of her stands in the gallery; her squad, if cleared, is gone (`clear.VessaKorl`) | She is on the gantry later, staged by you from `bossdown.SlagColossus` |

What the fight sets and logs, for the builder's own checks: `vask.vessaFallsBack` (at half), `bossdown.VessaKorl` (as she leaves, before her squad is down), `clear.VessaKorl` (with the squad), `VESSA_BEAM`, `VESSA_SWEEP`, `VESSA_VENTS`, `VESSA_PIKES <n>` (how many of her squad answered), `VESSA_HOT` (the shift), `VESSA_FALLS_BACK`, `VESSA_POUR`, `VESSA_WEDGED`, `BOSS_SHIFT VessaKorl 2`, `BOSS_LEAVES VessaKorl`. Barks: her `hail.gallery` ("Gallery guard, close up..." at her first move, "Pour drones, tip on my mark..." as she lays the floor) and the party's `bossphase.VessaKorl`, `bossopening.VessaKorl` (automatic); nothing at nought (her talk has been said).

**Ordinary enemy ids for the Foundry's encounters** (`HarrowgateBuilder.Enemies.cs`): `ForgeHound`, `MusterPikeguard` (two woken within 7 m pair up by themselves; one alone fights as a lone guardian), `PourDrone` (rides the nearest rail within 16 m: lay each overhead rail as a `ForceLine` on the floor under it, as chapter 7's fibres; with none it makes its own along the way it was set facing, so stand it facing along its rail), and the returning Muster at chapter 8's weight: **`ReachScav`**, **`ReachSergeant`** (chapter 2's `MusterScav` and `MusterSergeant` are tuned for Sync 9 and must not be placed here). The XP the chapter is sized to assumes your encounter plan above (yard 2 hounds, charging floor a pair and a hound, press bay 2 hounds, gallery south 2 drones and a pikeguard, the skip store's pair optional).

### Coordinates as built (casting gallery)

World engineer C, November 12, from `HarrowgateBuilder.Foundry` (`CastingGallery`); `BridleFoundryTests.TheCastingGalleryIsBuiltToItsContract` checks them in the built scene.

| Thing | As built |
|---|---|
| Floor | x -26 to 26, z 157 to 203 at **-5** (noise under 2.5 cm); nothing solid inside r 16 of G but the four racks (the test samples every metre) |
| Racks | **(-9, 174), (9, 174), (-11, 188), (11, 188)**: iron frames 4.1 x 1.25 with four sand cores between their uprights, **2.1 m** tall, one solid static box each (4 x 2.1 x 1.2, long side east-west), in the bake |
| Her post | Spawner `VessaKorl` at **(0, 190)**, yaw 180, `faceAsPlaced`, radius 0 |
| Her squad | Second spawner `MusterPikeguard` x2, radius **2.1**, at **(0, 185)**, yaw 180, `faceAsPlaced`; `spawners` holds both |
| Door | Encounter `VessaKorl`, trigger **50 x 6 x 42** on G, `requiredFlag` `vask.vessaMet`, `checkpointSpawn` `Gallery`; seals `Gallery south seal` at **(0, 157)** and `Heart stair seal` at **(0, 202.6)**, both 8.8 wide (the existing material), cube removed, iron bars drawn only while shut, a carving obstacle while shut. `previewPoint` `Vessa preview (the south door)` at (0, 160), radius **31** |
| Her talk at the door | `VessaGallery` crossing, box at (0, 161), 50 x 3: sets `vask.vessaMet`, saves at `Gallery`, `previewEncounter` the duel |
| Where she goes | `BossExit` `Vessa's way out (the gantry door)` at **(16, 201)**, floor -5, `boss` VessaKorl, `sinks` false. The gantry door: an iron frame and bars in the north wall at z 203.4, one solid static box (3.7 x 3.8 x .7) |
| Her fall back | `AfterTalk` `Vessa falls back`: flag **`vask.vessaFallsBack`** (`BossController.FallingBackFlag`, set by her shift at half her bar), node `VessaFallsBack`, speaker Vessa, `heardFlag` `vask.vessaFellBack` (`BossController.FallBackHeardFlag`; until November 21 `vask.vessaBeaten`, the main quest's step, which moved the card on to the Colossus with a third of her bar to go: whole-game capture s48/221), delay **1.6**, **`duringFight`**; offered at half and not again at nought (`BridleFoundryTests.VessasTalkIsOfferedAsSheFallsBackAndNotAgainAsSheLeaves`). Until November 12's look pass it waited on `bossdown.VessaKorl` and played at nought, after she had left |
| Checkpoint | Spawn `Gallery` **(-3, 149)** in the south room (on the paths, 8 m outside the seal), free cradle `Gallery cradle — repair and save` at (6.5, 149), saves at `Gallery` |
| Light | Two work lamps lit on its walls (-24.2, 170) and (24.2, 182), cold white; pillars on its east and north walls; finished ribs against its north wall; rib moulds along its east and west walls (low sand beds, under 0.3 m) |

## Boss arena contract

*The second combat engineer's section: what the Slag Colossus needs from the heart (where the sluices are, what a hero does to open one, where the boss stands and walks, its pour-arms, the slag floor, the door). The world engineer builds to it and writes the coordinates as built underneath.*

Written November 12 by the second combat engineer, to the plan above (the heart r 22 round **C = (0, 251)** at floor **-10**, door seal at z 227, checkpoint `Heart` at (-3, 220)). The fight's moves are `S/Combat/BossController.SlagColossus.cs` (every number in its `ColossusTune`); a sluice is `S/Combat/CoolantSluice.cs` (one scene MonoBehaviour, guide section 15); the definition is `HarrowgateBuilder.Bosses.cs`. Its tests (`T/PlayMode/HarrowgateBossTests.cs`) build a stand-in of this room on a bare floor in the proving yard, so nothing here waits on the Foundry. Positions are from **C**, +z north; a bearing is degrees clockwise from north.

**How the fight uses the room.** The Colossus stands at C, the cage behind it on its dais, and never walks more than 6 m from C. It wears three **heat plates** (drawn on its body by the fight: its front and both flanks), and while any plate still glows its whole body is **too hot to hurt**: every blow on it lands at nothing and reads as resisted, the bar says TOO HOT TO HURT · OPEN A SLUICE, and the first blow says so on screen. Round the rim stand **three coolant sluices**. A hero who stands in a sluice's seat and turns its red wheel (1.6 s, hands on it: a roll, a swing, a skill, a stagger or a step out lets go) throws coolant from its spout onto the Colossus: one plate goes **dark for 9 s**, and a dark plate is the thing to hit (a break target). A dark plate broken is shed for good and takes an eighth of the bar; one not broken in time glows again, keeping its wounds. A sluice then refills for 12 s before it can be turned again, so the party works the room: wheel, plate, next wheel. With all three plates shed the Colossus is cooling iron: its body can be hurt, and its **two pour-arms** open as break targets; its body is fought from five eighths down to half and held there until both arms are off (BREAK THE POUR-ARMS), and the second arm off with its bar at the line tears loose and is its shift. Then it comes apart on the cooling slag floor: it leaks bright slag as it walks (a danger for 2.5 s, then dark crust that only slows), its pours are gone, and it spills its crucible round itself. It is destroyed, not left standing.

| Thing | What the fight needs | Why |
|---|---|---|
| Floor | Round, **r 22** from C at -10 (your plan), level inside **r 20** (no step over .3 m). Nothing solid inside r 20 but the boss and the three cover stacks below; the sluices' housings stand outside r 16 | Its moves reach 12 to 18 m from its body's edge; a hero rolling out of a sector or a lane must never meet a step |
| Boss | Its `Spawner` at **C**, yaw **180** (facing the door), `faceAsPlaced=true`, radius 0. `previewPoint` at the landing just inside the door, **(0, 229)**, with a radius that takes in C (about 26), so it stands there glowing and inert (`DormantEnemy`) through `SelaCagedRoad` | It is in the room at the first look, between the party and the cage. Its plates, arms and the sluices' offers are made only as it wakes: nothing can be hit or worked from the door |
| The three sluices | One **`CoolantSluice`** each, on an empty object **at its seat** (the floor a hero stands on: the prompt's place), **r 15** from C at the floor, its forward toward C. **1** on bearing **200**: **(-5.13, 236.90)**, the red wheel nearest the door; **2** on bearing **80**: **(14.77, 253.60)**; **3** on bearing **320**: **(-9.64, 262.49)**. Fields: `index` 1 to 3 as listed; `settledBy="SlagColossus"`; **`wheel`** = the wheel's own Transform (it is turned about its own forward axis, a turn and a half as the hero works it), on a housing **1.4 m behind the seat** (r 16.4), its hub 1.2 m up, facing C; **`spout`** = an empty at the coolant pipe's mouth on the rim, **r 18.5**, **2.6 m** up, facing C. Leave its prompt and range alone: the sluice sets them (`Turn the sluice wheel`, 1.6 m) | The seat is what the player stands on and the prompt measures to. It draws its own seat ring (pale cream: a boundary, never a danger's colour) while it can be turned and fills it as the wheel turns; the coolant's jet from the spout to the plate is drawn by the fight |
| Each sluice's housing | A solid static box round the wheel's housing (about 1.6 x 2 x 1.2, in the bake), a pipe run from the housing up to its spout (drawn only, above 2.4 m or outside r 17); nothing solid within 2 m of the seat; the seat on the paths. Fade on the south housing (sluice 1: the camera's side) | It is worked under fire: the hero must be able to step into the seat, and roll out of it, from any side but the wall |
| Cover | **Three** stacks (cast ingots, or a quench trough on end): solid static boxes **2.2 m tall**, about **2.8 x 1.6 m**, long side across the line to C, at **r 9** on bearings **140, 260, 20** (between the sluices' lines from C): **(5.79, 244.11)**, **(-8.86, 249.44)**, **(3.08, 259.46)**. In the bake; fade on. Nothing on the straight line from C to a seat or from a spout to C | Its furnace breath stops at solid cover; its bombs and pours don't. The lines from C to the seats are the jets' paths |
| The plates, the arms, the slag | Nothing to build: the heat plates are drawn on its body by the fight, the pour-arms are knots on its shoulders (`BossParts`, `Pour-arm`), and the slag it pours, spills and leaks is the fight's `LingeringArea`s on the floor | |
| Door | Encounter id **`SlagColossus`** (the main quest's step 7 points at it), trigger covering the floor (about **44 x 8 x 44** on C), `requiredFlag` **`vask.cageSeen`** (your `SelaCagedRoad` crossing), `checkpointSpawn` **`Heart`**. One `Membrane` seal across the door at z 227, strands drawn instead of the cube | The shared door tests (checkpoint, retry, no way out mid-fight). Its boss sheet names zone `BridleFoundry`, door `SlagColossus`, door flags `vask.vessaBeaten`, `vask.cageSeen`: write back any other flag the door waits on |
| Rim | South and west low (the camera's foreground); the north is the cage on its dais beyond r 22 and the gantry, which is where they belong. Nothing over 3 m and 7 m across between the camera and C | Fixed three-quarter camera. The fight draws the view back over a sluice's seat and the Colossus while a hero stands at the wheel, so the plate it darkens is seen |
| Leaving | None, and no `BossExit`: it is destroyed (`Ending.Destroyed`) | The brief |
| After the fall | Every sluice loads drained and inert once `bossdown.SlagColossus` is set (no ring, no prompt). The Colossus plays no down bark, so nothing is said over `TovraRelease` (your `AfterTalk`) | Manifest 8.5 |

What the fight sets and logs, for the builder's own checks: `bossdown.SlagColossus`, and `BOSS_MOVE SlagColossus <move>`, `COLOSSUS_HOT` (the first blow on glowing armour), `SLUICE_TURNING <index>`, `SLUICE_LET_GO <index> <why>`, `SLUICE_OPEN <index>` (the jet), `PLATE_DARK <plate>`, `PLATE_SHED <plate> shed=<n>`, `PLATE_REHEATS <plate>`, `COLOSSUS_COOLED`, `PART_BROKEN Pour-arm standing=<n>`, `COLOSSUS_ARM_OFF 1`, `COLOSSUS_ARMS_OFF`, `COLOSSUS_TEARS_LOOSE`, `BOSS_SHIFT SlagColossus 2`, `COLOSSUS_LEAKS`, `COLOSSUS_LODGED`, `COLOSSUS_VENTS`, `COLOSSUS_EMPTIED`, `BOSS_DOWN SlagColossus`. Barks: Tovra's third `hail.sluice` ("Third. That plate's going dark. Hit it.") the first time a plate goes dark in the heart (her first two are your wheels 1 and 2); the party's `bossopening.SlagColossus` at each later dark plate and at each opening, `arm` as each pour-arm breaks, `bossphase.SlagColossus` at the shift. Its music is **Glitch Boss** (its definition); the Foundry's Alien Pulse comes back at its fall.

### Coordinates as built (the heart)

World engineer C, November 12, from `HarrowgateBuilder.Foundry` (`Heart`, `HeartSluices`, `CoverStacks`); `BridleFoundryTests.TheHeartIsBuiltToTheBossContract` checks them in the built scene.

| Thing | As built |
|---|---|
| Floor | Round, r 22 on **C (0, 251)** at **-10** (a 6 cm bowl to C, noise under 2.5 cm); the test samples every metre inside r 20: level, and nothing solid but the three stacks and the housings' front faces (see below) |
| Boss | Spawner `SlagColossus` at **C**, yaw 180, `faceAsPlaced`, radius 0. `previewPoint` `Colossus preview (the landing inside the door)` at **(0, 229)**, radius **26** |
| Sluices | `CoolantSluice` on `Heart sluice N seat` at r 15, facing C: **1 (-5.13, 236.90)**, **2 (14.77, 253.60)**, **3 (-9.64, 262.49)**; `index` 1 to 3, `settledBy` SlagColossus. `wheel`: a red hand wheel (r .62) hub **1.2 m** up, 0.66 m in front of its housing, facing C. Housing: solid static box **1.6 x 2 x 1.2** centred at **r 16.4** (so its front face is at r 15.8: two tenths inside the contract's r 16, which its own 1.2 m depth and r 16.4 centre require), in the bake; sluice 1's fades. `spout`: an empty at **r 18.5, 2.6 m** up, facing C, at the mouth of a pipe that climbs from the housing's back up the rim wall (drawn only, over 2.6 m or outside r 17). Nothing else solid within 2 m of a seat |
| Cover | Ingot stacks (five courses of cast ingots, a little off true), one solid static box each **2.8 x 2.2 x 1.6**, long side across the line to C, fade on, in the bake: **(5.79, 244.11)**, **(-8.86, 249.44)**, **(3.08, 259.46)** |
| Door | Encounter `SlagColossus`, trigger **44 x 8 x 44** on C, `requiredFlag` `vask.cageSeen`, `checkpointSpawn` `Heart`; one seal `Heart door seal` at **(0, 227)**, 8.8 wide, bars drawn only while shut, carving obstacle while shut |
| Checkpoint | Spawn `Heart` **(-3, 220)** on the landing (-10), 7 m outside the seal, on the paths; free cradle `Heart cradle — repair and save` at (7.5, 220), saves at `Heart`. The door's talk `SelaCagedRoad`: crossing box (0, 225.2), 8.8 x 2.2, saves at `Heart`, `previewEncounter` the duel |
| Rim | South and west low (the sightline rule); north: the dais (z 270 to 287) with the Bridle at (0, 279), the gantry (z 285.8, five metres up) and two ducts at x -15.5 and 15.5; vents of the vein lit from behind high in the north and east rim wall (bearings -52, -28, 28, 52, 100, 122; 2.85 to 4.55 m up: on the wall, never the floor) |
| Leaving | No `BossExit` for the Colossus |
| After the fall | `AfterTalk` `The release` (`TovraRelease`, see below); the sluices settle themselves |

## After the Colossus (the release)

The world engineer's, from the manifest (section 2, 8.5 to 8.7): `TovraRelease` plays by `AfterTalk` once `bossdown.SlagColossus` is set (it sets `vask.juvenileFreed` itself). The smiths stand at the cage's four clamps (fifteen to a clamp in the script; a few each in the room), Brekk at the fourth, Tovra before the cage, Vessa on the gantry. The juvenile strains in its cage, pulling nine seconds and easing four, from the moment the party can see it (`SelaCagedRoad`) until it is freed; freed, it rises through the vent over the dais and is gone. After it, Tovra sits on an anvil with Brekk beside her (`BrekkAnvil`, prompt `Stand by the anvil`, sets `vask.anvilSeen`), and Vessa is gone.

**Nobody appears in view** (the look review, November 12: "the smiths appear" is a fault a player sees). As built:

| Who | Through the fight (from `vask.cageSeen`) | At the fall (`bossdown.SlagColossus`) | At the release's end (`vask.juvenileFreed`) | On a load |
|---|---|---|---|---|
| Eleven smiths (`The smiths at the clamps`, `Townsperson` bodies) | Waiting in the passage behind the **smiths' door** in the dais's north wall (x -2.4 to 2.4, z 286.5 to 293.5, at -10), its bars down (`Smiths door grille`, solid) | The bars go (the gates stand hooked back) and they walk in, three abreast, round the cage's sides to their clamps (r 6.4 to 7.3 from the cage) | Stay | Where the flags put them |
| Brekk (`Brekk`) | In the passage | Walks to the fourth clamp (bearing 299, r 6.4) | Walks to the anvil's side, (-5.9, 273.7) | Same |
| Tovra (`Tovra`) | In the passage | Walks (brisker: the longest way) round the cage's east side to stand before it, (0, 271.6) | Walks to her anvil, (-7.3, 274.6) | Same |
| Vessa (`Vessa on the gantry`) | Beyond the gantry's east door, (17.4, -4.98, 285.8), inside the wall | Comes out of it and along the gantry to (9.5, 285.6), five metres up | Walks back off it by that door | Not there after the release |

Each walks as a `Guide` (guide 15.17: a leg on the fall, one on the release; a load stands each at the end of the last leg its flags have set, so nobody walks a road twice). Out of frame because of the camera, not because of the walls: from anywhere on the heart's floor the frame's top edge meets the floor short of the dais's north edge, and the passage and the gantry's east end are above it (`BridleFoundryTests.TheReleaseIsStagedFromTheColossussFall` checks every walker against the camera's frustum from seven places on the floor, and that nobody is put anywhere at the fall). The talk waits 7.5 s after the fall, the longest walk in. The young road still goes after `TovraRelease`'s last line, not under "There." and "Gone."
