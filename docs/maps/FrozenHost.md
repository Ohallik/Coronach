# The Frozen Host - the White Reach and the dig

Functional brief, written before construction (November 12, world engineer B, chapter 6). **MAP_EYE_TEST: blockout PASSED with repairs; dressed (editor) PASSED with repairs; final in-player pass OPEN** (review at the end; an agent's review, nobody else has looked). Chapter brief: [ch06-the-long-sleep.md](../chapters/ch06-the-long-sleep.md); script wiring: [ch06-manifest.md](../writing/pending/ch06-manifest.md). Builder: `S/Editor/World/HoarfellBuilder.FrozenHost.cs` (everything of the zone's own is in its nested `FrozenHost` class). Mechanics: [CHAPTER_GUIDE.md, "World mechanics"](../CHAPTER_GUIDE.md).

## Purpose and formation

North of Lastlight's Reach gate the White Reach runs out over the glacier: wind-packed snow, crevasses with snow bridges, a ridge of seracs with one gap in it that the rime-beast herd uses, vents whose steam keeps a few hollows clear when the wind drops. Under it, an Ancelane army stands where it was marching when the last Severance froze it: the tips of its spears and standards come up through the snow in rows. At the Reach's north end the Muster have cut a trench into the glacier and dug down after the army: the **dig**, three levels stepping down eastward, with the host frozen in ranks in its north walls. The Muster's thaw-rigs are waking it a rank at a time. The Host Marshal stands in the bottom gallery.

**The route's shape is an L.** North across the Reach to the dig's head; then east and down the dig, a level at a time, to the bottom gallery. The one rule in the dig: **a thaw-rig turns the ice amber, and amber ice lets go of what is in it.** Each level's way down is shut by a plug of ice; the rig on that level, worked by a hero, thaws the plug, and whatever its heat reaches thaws with it. It is introduced safely on the upper gallery (rig one's heat reaches nothing but the plug), tested on the second level (rig two's heat reaches a rank of three, who step out as the party walks in), and combined on the third (rig three's rank comes out with a Bridle-hound let loose from behind the plug). Amber ice already in the walls is the Muster's own thaw: a rank in it wakes if the party walks up to it, so the party goes round amber.

The Reach has two beats of its own, each introduced safely and then tested: the herd (walk through it, don't run: a dodge, a sprint, a blow or a skill within 7 m sets it off, and a bolting beast tramples on a marked lane), and the whiteout (the first comes during Idra's lesson with nothing about, and the vent pocket is forty paces west; the second closes as the party walks into the stalkers' field, with a second vent to the east).

## Users and access

Nobody lives here. Idra Sorn leads the party out of Lastlight and walks ahead of it across the Reach and down the dig (her braced body, waiting at each of her talks, never someone to talk to), until she joins at the bottom gallery. Vessa Korl is a voice on the rig's loudhailer. The party arrives through the Reach gate (spawn `Arrival`) and leaves either back through it (`Reach gate — Lastlight`, to Lastlight's spawn named in docs/maps/Lastlight.md) or, once the Marshal sleeps, up the loading winch (`Ride the winch up — Lastlight`, the same spawn).

## Layout

About 280 x 265 m. x runs east, z north; heights are world y. The Reach's snow lies at about 0; the dig's floors at -4, -8.5, -13 and -17.5. The glacier north of the dig stands at +4 to +7.

```text
 z 250                                    glacier (+4..+7), the host in ranks in every north wall
        +-----------------------+  +--------------------+  .-'''-.
 z 232  |  L3  third level -13  |rig3                   |  /       \
        |  hounds, motes        |==plug3== ramp3 ==> ANTE| | ARENA  |   C = (208, 218), r 18
 z 204  +---ramp2---------------+          winch, door  |=|  -17.5 |
        | left gallery  |rig2                           |  \       /
 z 196  +--==plug2==----+------+----------------+       +--'-...-'
        |                      |  the Excavators' cut   |
        |   L2  second level -8.5                       |  (dead end, cache)
 z 170  +----------------------+------------------------+
   +--------------------------------+
   |  L1  upper gallery -4    rig1  |==plug1== ramp1 ==> L2
   +----[mouth ramp]----------------+     z 176-198, x 14-64
 z 160   camp: bench, sledges, boiler, loudhailer mast
 z 136   ~~~~~~~~ crevasse 2 ~~~~ bridge ~~~~~~~~
         stalkers' field (second whiteout)     vent pocket 2 (34, 118)
 z 100   vent pocket 1 (-30, 104) <- first whiteout (Idra's lesson)
 z 88  ^^^^^^^^^^^ serac ridge ^^^ gap ^^^ ridge ^^^^^^^^^^^   herd streams south through the gap and away west
 z 54    west pocket (sledge, a stray)        herd field
 z 50    ~~~~~~~~ crevasse 1 ~ west bridge ~~~~ main bridge ~~~~~~~~
 z 15    spear field (stalkers)
 z 0     Arrival, cradle         Reach gate (rib arch) z -11
```

| Area | What it is for | Size | Level | Combat |
|---|---|---|---|---|
| Reach gate and arrival | The way home under a rib arch, a free cradle, Sela and Taren's talk | 30 x 14 m | 0 | none |
| Spear field | The army under the snow: rows of spear tips and standards; the first Rime-stalkers | 70 x 32 m | 0 | `Reach_Stalkers` |
| Crevasse 1 | Two snow bridges over a crevasse that crosses the Reach: the main bridge (6 m) and the west bridge (4 m) to the west pocket | 100 x 5 m | to -7 | none |
| West pocket (optional) | A Muster sledge gone through the crust, its cargo spilled; a steam vent with a stray | 24 x 18 m | 0 | none |
| Herd field and gap | The herd streams south through the gap in the serac ridge and away west; the party walks north through it | 14 m gap | 0 | none (the herd) |
| Whiteout field | Idra's lesson: the first whiteout closes, vent pocket 1 to the west; then the stalkers' field, where the second closes as the party walks in | 100 x 46 m | 0 | `Reach_Whiteout` |
| Crevasse 2 | One natural ice bridge | 100 x 4 m | to -7 | none |
| Camp | The dig's head: a bench, the Muster's sledges and stacks, the thaw boiler, the loudhailer mast; Vessa's first words | 60 x 24 m | 0 | none |
| Mouth ramp | Down into the dig | 12 x 16 m | 0 to -4 | none |
| Upper gallery (L1) | The rule introduced: ranks frozen blue in the north wall, one amber (go round it); rig one thaws the plug over the ramp down, and nothing else | 50 x 22 m | -4 | `Dig_Motes`; `Dig_Amber` (optional) |
| Ramp 1 | Down east to the second level, plugged at its head | 14 x 18 m | -4 to -8.5 | none |
| Second level (L2) | Vessa's offer; the left gallery (the way) and the Excavators' cut (straight on) | 34 x 28 m | -8.5 | none |
| Left gallery | Rig two thaws the plug across its mouth and the rank in its west wall; the rank walks out; the gap it leaves is the short way down | 12 x 20 m | -8.5 | `Dig_Rank` |
| The Excavators' cut (optional) | A Muster Excavator at work; the Muster's tool chest | 22 x 20 m | -8.5 | `Dig_Excavator` |
| Ramp 2 | Down east from the left gallery's north end | 18 x 12 m | -8.5 to -13 | none |
| Third level (L3) | Motes and a hound on arrival; rig three thaws the plug over the last ramp and the rank beside it, and lets out the hound penned behind it | 40 x 32 m | -13 | `Dig_Third`, `Dig_Rank3` |
| Ramp 3 | Down east to the bottom | 16 x 18 m | -13 to -17.5 | none |
| Bottom gallery's mouth (ante) | The loading winch and Vessa's slate; where the gap lands; the checkpoint, a free cradle, a bench; Idra joins | 22 x 32 m | -17.5 | none |
| Bottom gallery (arena) | The Host Marshal (contract below) | 36 m across | -17.5 | `HostMarshal` |

**Travel** (about 6.7 m/s): the Reach about 175 m of walking, the dig about 200 m: about 60 seconds of walking without a fight. With seven fights, two whiteouts (about 25 s each), the herd, three rigs, the optional pockets and nine talks it is about 25 to 30 minutes before the boss.

## The host where the camera looks (look pass, November 12)

The place is named for the army in the ice, so the army is what the fixed camera sees, not a strip at the top of the frame.

- **Under the floor** (`HostBeneath`, `Beneath` in the builder; one atlas `Art/Generated/Hoarfell/host-below.png` painted in code, material `host-below`). Ranks of the host stand in the ice under every dig floor and the bottom gallery, and under the Reach wherever the wind has scoured the snow to ice (the herd's line, six windows over the spear field and the stalkers' field). Each rank is ten metres of soldiers abreast (cuirass, crested helm, round shield, spear), laid flat with "up" running away from the camera, so it stands upright in the frame as a body under clear ice does. Blocks of three ranks with lanes between, in a brick bond. Far below the upper gallery (soft and faint), nearer under the second level, clearest under the third level and the bottom, with deeper ranks between the blocks. Clearer where a rig or an amber rank has warmed the ice; never under snow. They are dim, grey-blue shapes drawn just before the ground kit's own flat details (queue `DetailQueue - 1`), so a crack lies over them and every danger marking lies well over them. Batches are per 12 m cell and floor, named `Floor detail host beneath ...`, one mesh asset `Scenes/FrozenHost-Beneath.asset`.
- **In the walls, at full height**: where a gallery is deeper than the camera's reach from the road, the face the Muster haven't cut yet stands out from the wall in clear ice, so the host in it stands 8 to 10 m from the walked line (`ForwardRank`): the upper gallery west of the Muster's amber (x 13.6-30.25) and east of it (x 50.2-58.4), front faces at z 193.45. Two more stand in the bottom gallery's mouth's east wall either side of the door (x 185.6-188.6, z 203.6-212.4 and 223.4-227.8). Every blue rank, these and the old ones, is lit from within: a soft cold glow on a card behind the host inside the ice (material `host-ice-light`, no light of its own), so the soldiers stand dark against it.

## The three levels and the muster ground

- **The upper gallery is the Muster's worksite**: their lean-to, shear legs with a cut block hanging over the one beside it, two tool racks (ice saws, picks, tongs, a bar), two sledges loaded with cut blocks, spoil heaps, three lamps on tripods, the cable from the boiler lying in easy bends round the walls to the lamps and rig one, sledge ruts to the road, and the lines they have scribed on the ice where the next blocks come out (a 5 x 2 grid of survey lines).
- **The second level is where their thaw went over the ice and went cold**: a duller floor with less snow, the frozen stains its meltwater left, and the thaw lines the Muster ran along its walls from rig two to its ranks, frosted and dead (pale, their hazard paint gone dull, rime crusted along them). Its ranks (the north wall's, x 103.4-114.4; the cut's, x 114.6-128.4; a pair at the west edge, x 80.3-84.1) stand out of the ice to the hips in mounds that slumped while they ran and froze again where they lay: blue-white, cloudy, rimed over, rime on the bodies' shoulders and helms, no glow and no steam (material `host-ice-refrozen`; solid as a 0.75 m box). They are drawn only and no flag wakes them. Amber is kept for what wakes, and only for it (the coordinator's decision, November 12): `Dig_Amber`, the left gallery's rank and rig three's rank, the bottom gallery's thawing wall. A first version stood them in amber, which would have taught a player that the rule sometimes lies.
- **The third level is the oldest**: Ancelane columns stand at its way in from the ramp (113.4, 206 and 113.4, 222.2) and its way out (149.8, 207.9), drums of its colonnade lie half in the ice (116.4-118, 205; 127-128.6, 209.2; 140.2-141.1, 213; solid, 0.8 m), the Ancelane paving shows through the ice in grey patches, and the host's standards (bronze, a stride and more over a head, a wheel of spokes at the top, faded ochre cloth) stand in front of its ranks (x 120.5, 127.5, 134.5 at z 232.6) and lean up out of the floor's ice along the road (119, 211.2; 124.6, 224.6; 131.2, 221.6; 143.4, 217.2) and at its edges (113.6, 229 and 141.5, 206.2). Each standard is solid at its foot (0.35 m), a post the camera rule passes. Footings flush in the floor were tried and taken out: on a fight's floor a ring in a square reads as something to step on.
- **The bottom gallery is the host's muster ground**: under its ice the Ancelane paving (rings at r 6.5, 11.5 and 16.5 about C, lines out from it every 30 degrees) and ranks drawn up on it; at the Marshal's post (bearing 0, r 10) its sign under the ice and, inlaid flush in the floor where it stands, a disc of stone in two bronze rings with eight bronze bars between (`Floor detail the Marshal's post`, no collider: the floor is the contract's). In the niche behind the shelf, the Marshal's dais of two stone steps with the host's great standard over it (6.4 m, its ring 1.3 m across) between two cold bronze braziers, seen over the shelf's ranks.

## The Reach at a larger scale

- The crust is laid in fields about a dozen metres across (`Noise(13)`), with the snow between polished into soft dimples (the floor's own texture, `host-snow`; a first try with rimmed scallops read as a honeycomb of tiles and was taken out), so the Reach is two surfaces and not one. The crust's ridges themselves wander and run three ways in fields that meet in smooth seams over its 12 m repeat (the first try's seams zig-zagged like herringbone).
- Blue scoured ice down the herd's line (about 4 m either side, ragged), in the six windows over the host, in the hollows and down the crevasses.
- Snow drifted against everything that stands: a bank on its east face, a tail drawn out west (the seracs near the floor, the camp, the sledges, every standard and shield of the spear field).
- Dark rock (14 outcrops at most 1.6 m) and old blue ice pushed up in ridges (knee to chest high, solid) come through the snow off the walked lines, the herd's line, the fights and the pockets.
- **The spear field reads at play distance**: shafts 1.5 to 2.6 m showing, thicker, three in ten pushed over 15 to 28 degrees by the ice, one in five snapped; standards 3.2 to 4.3 m with a ring at the top and cloth no more than a metre across; round shields tipped up out of the crust; crested helms.

## The vent pockets

Somewhere a herder is glad to reach in a whiteout: dark wet ground with moss and lichen, a pool steaming at the heart, steam rising in four columns, the herders' windbreak of dry-stacked stone (three courses, 1.1 m, solid) with a hide stretched between two poles over it, round the side away from the way in and from the pocket's stray (pocket 1: bearings -20 to 60; pocket 2: 95 to 175), and their cairn on the rim with a pole of rime-beast-hair tassels (pocket 1 at bearing 250, pocket 2 at 300). The west pocket (not a shelter) has loose stones, the pool, moss and a cairn at 330.

## The thaw (the dig's mechanic)

| Rig | Where | Prompt, flag | Its heat reaches | What changes |
|---|---|---|---|---|
| One | Upper gallery's east end (60, 193); worked from (58.5, 189.5) | "Run the thaw rig", `hoar.rig1` | plug 1 only | Plug 1 (across ramp 1's head, x 65-67) goes amber and melts: the way down to the second level. Nothing wakes |
| Two | Second level's north wall, east of the left gallery (100, 197); worked from (99, 192.5) | `hoar.rig2` | plug 2 and the rank in the left gallery's west wall | Plug 2 (across the left gallery's mouth, z 199-201.5) melts; the rank's ice goes amber and melts, and the three step out as the party walks into the gallery (`Dig_Rank`, `requiredFlag hoar.rig2`) |
| Three | Third level's north wall by ramp 3's head (147, 233); worked from (145.5, 228.5) | `hoar.rig3` | plug 3, the rank in the north wall beside it, and the hound pen behind the plug | Plug 3 (across ramp 3's head, x 151-153) melts; the rank and the hound come out as the party walks toward the ramp (`Dig_Rank3`, `requiredFlag hoar.rig3`) |

- Each rig is a `FlowSwitch` (marker `RIG_RUN`) on the Muster's `ThawRig` with a valve wheel that turns; its `shows` is the ice it will thaw, so a hero at the rig sees the plug and the rank before working it. Each body of ice that answers is an `IceThaw` (new, `S/World/IceThaw.cs`, guide 15.12) on the same flag: blue to amber, then what is set to melt slumps and goes, and its light warms. A save loads thawed with nothing played.
- Each plug is a `Membrane` with a carving obstacle (a seal like any other, so the world tests that open every seal open it), baked open.
- The Muster's own thaw: ranks already amber (upper gallery x 40; two in the bottom gallery's mouth) wake when walked up to. Their trigger stops 7 m short of the walked line.
- Once the Marshal sleeps (`hoar.marshalAsleep`) every amber body of ice goes blue again (`IceThaw.refreezeFlag`): the cold comes in behind the party, as Idra planned. What melted stays melted.

## The Reach's beats

| Beat | Where | What it does | Logs |
|---|---|---|---|
| The herd (`RimeHerd`, new, `S/World/RimeHerd.cs`) | Through the gap (0, 81), from (52, 98) to (-56, 60) | 22 rime-beasts walk the line and are put back at its start out of sight. Each stops for anyone in its way. A dodge, a sprint, a blow or a skill within 7 m of any beast startles the herd: every beast marks the lane ahead of it for .8 s (danger colour), then bolts down it for 2.4 s; a hero in a bolting beast's lane is knocked aside and loses 6% of integrity (the place's damage, never the last of it). `IdraHerd` plays at (0, 57) as the herd comes into view; `hoar.herdPassed` is a silent crossing north of the gap (0, 93) | `HERD_STARTLED`, `HERD_TRAMPLED`, `HERD_SETTLED` |
| Whiteouts (`Whiteout`, new, `S/World/Whiteout.cs`) | The field north of the ridge | The first closes once `hoar.whiteoutMet` is set and Idra's talk has closed; later ones as the lead walks into a front (the stalkers' field). The wind drops (warning, 3.5 s), visibility closes (fog from 14 m past the hero, blown snow) for 22 s, then lifts. Outside a vent pocket a hero walks at .7; inside one (steam marks it) the whiteout doesn't reach. Danger markings don't take fog | `WHITEOUT_WARNING`, `WHITEOUT_CLOSED`, `WHITEOUT_LIFTED` |
| The gap (`LeapDown`, new, `S/World/LeapDown.cs`) | Left gallery, where rig two's rank stood (85.6, 206.5) | Offered once `clear.Dig_Rank` is set: "Take the gap down" plays `SelaDigShortcut`, sets `hoar.gapTaken`, and the party jumps: the zone is entered again at spawn `Gap` in the bottom gallery's mouth (autosaved on arrival) | `LEAP_DOWN Gap` |
| Idra ahead (`Guide`, new, `S/World/Guide.cs`) | Each of her talks | Her braced body waits at her next talk; once it is had she walks on to the one after, and at load stands at the last one reached. Gone once she joins (`hoar.idraJoined`) | `GUIDE_LEG`, `GUIDE_AT` |

## Identities

Scene `FrozenHost` (GroundCombat, profile Rime, sky day, threat 1, chapter 6; the ZoneDef is `HoarfellBuilder.Define`'s). Music: March of the Frozen Host. Light: `FrozenHostLooks` in the builder. Ambience: `AMB_FrozenHost` (open wind and spindrift, the glacier groaning and cracking, the rigs thumping below, the herd far off), synthesised by `tools/frozen_host_ambience.py` and set on the definition by `FrozenHost.Air` at volume .377 (its measured RMS .0594). Nobody has listened to it.

## Coordinates (for routes and tests)

All world x, z (y is the ground there: the Reach about 0, the upper gallery -4, the second level -8.5, the third -13, the bottom -17.5). Spawns are `SpawnPoint` ids; points are where a hero stands to press; boxes are centre and size. The numbers are the builder's (`HoarfellBuilder.FrozenHost`'s named constants).

| What | Id or prompt | x, z | Flag / note |
|---|---|---|---|
| Arrival spawn | `Arrival` | 0, 2 | |
| Way home | `Reach gate — Lastlight` | 0, -10.5 (under the rib, z -11.5) | to Lastlight's spawn `Reach` |
| Cradle | `Herders' cradle — repair and save` | -9, 4 (stand at -7.4, 4) | free, saves at `Arrival` |
| Reach talk (crossing) | `SelaReachGate` | box 0, 12 (36 x 3) | `hoar.reachEntered` (Lastlight reads it) |
| Spear field | `Reach_Stalkers` | 0, 32 (36 x 20) | 2 Rime-stalker; hides -10,30 / 9,35 / -1,40 |
| Idra on the radio | hail `hail.reach` | from 2, 44 (12 m) | `hoar.reachHailed` |
| Main bridge | crevasse 1 | 2, 50 (6.4 wide, 4.4 walkable) | lips are solid knee-high boxes under the cornice |
| West bridge | crevasse 1 | -30, 51 (4.4 wide) | to the west pocket |
| West pocket | `Reach_West` | -37, 61 (20 x 16) | 2 Frost Motes |
| Muster sledge | cache `hoar.reach.sledge` | -40, 60.5 | |
| Stray one | `Walk the stray in` | -36, 63.3 (the beast at -36, 64.5) | StrayBeasts step 0, report `StrayBeast`, `hoar.stray1` |
| Herd talk (crossing) | `IdraHerd` | box 0, 57 (40 x 3) | `hoar.herdMet` |
| The herd's line | | -48,91 / -26,92 / -9,89 / -5.5,81 / -4,72 / 4,66 / 24,62 / 48,61 | 22 beasts; the walked line crosses it at about 4, 66 and goes up the gap's east half |
| The gap | | 0, 81 (20 wide) | |
| Past the herd (crossing) | silent | box 8, 95 (24 x 3) | `hoar.herdPassed` |
| Whiteout talk (crossing) | `IdraWhiteout` | box 0, 100 (44 x 3) | `hoar.whiteoutMet`; the first whiteout closes when the talk ends |
| Vent pocket 1 | sign `VENT POCKET` (-24.5, 99.5) | -30, 104 (shelter 13 x 13) | |
| Stray two | `Walk the stray in` | -33.5, 105.8 | `hoar.stray2` |
| Stalkers' field | `Reach_Whiteout` | 6, 120 (40 x 22) | needs `hoar.whiteoutMet`; 2 Rime-stalker, 2 Frost Motes; hides 0,116 / 13,121 / -6,125; walking into it brings the second whiteout |
| Vent pocket 2 | | 34, 118 (shelter 12 x 12) | |
| Stray three | `Walk the stray in` | 36.5, 119.8 | `hoar.stray3` |
| Crevasse 2 bridge | | 6, 136 (6.4 wide) | |
| Rime glass | `Gather Rime Glass` | -46,40 / 46,108 / -44,140 | `hoar_glass_0..2`, 2 each |
| Bench | `Fabricate — Lattice Anvil` | -10, 151 | |
| Camp | `Reach_Camp` | 26, 152 (30 x 16) | 2 Frost Motes on the trodden snow south of the thaw boiler (43, 163) and its tank, which stand at the rim, north of the fight; the box takes in the walked line through the camp (in at about 11, 148, out at 22, 160), so walking the route wakes it |
| Vessa's first words (crossing) | `VessaLoudhailer` | box 24, 163 (16 x 3) | `hoar.vessaHeard` |
| First rank (crossing) | `IdraDigGalleries` | box 24, 181 (14 x 3) | `hoar.ranksSeen`; sign `UPPER GALLERY` |
| Vessa, rank one | hail `hail.rank1` | from 40, 186 (12 m) | needs `hoar.ranksSeen` |
| Upper gallery fight | `Dig_Motes` | 52, 183.5 (16 x 11) | 2 Frost Motes |
| The amber rank (optional) | `Dig_Amber` | 40.2, 194.5 (14 x 7) | 3 Thawed Guard shown inert at 40.2, 196.4 until walked up to |
| Host alloy | `Prise out Host Alloy` | 56, 178.5 / 138, 207 | `hoar_alloy_0..1`, 2 each |
| Rig one | `Run the thaw rig` | 58.5, 189.5 (the rig at 60.5, 194.2) | `hoar.rig1`: plug one (x 64.8-67.2) |
| Rig one back ice | solid, 3.7 x 3 x 4.1 | 60.15, 196.65 | packs the 1.4 to 2.2 m wedge between the rig's north-west face, the upper rank's ice (x 58.4) and the north wall: the same pocket as rig three's (below) |
| Vessa's offer (crossing) | `VessaSecondLevel` | box 86, 184 (3 x 26) | `hoar.offerHeard` |
| Second level fight | `Dig_Second` | 104, 181 (20 x 22) | needs `hoar.offerHeard`; 1 Muster Excavator, 2 Bridle-hound |
| Vessa, excavators | hail `hail.excavator` | from 98, 184 (12 m) | needs `hoar.offerHeard` |
| Rig two | `Run the thaw rig` | 99, 192.5 (the rig at 100.5, 197.4) | `hoar.rig2`: plug two (z 199.1-201.5) and the rank's ice (x 84.1-86.5) |
| The rank (crossing, bark `thaw`) | | box 90, 202.6 (12 x 3) | needs `hoar.rig2`; `hoar.rankStepped` |
| Left gallery fight | `Dig_Rank` | 90, 207.5 (12 x 13) | needs `hoar.rig2`; 3 Thawed Guard from 85.3, 206.5 facing east, shown inert until walked up to |
| Vessa, rank two | hail `hail.rank2` | from 90, 203 (9 m) | needs `hoar.rig2` |
| The gap | `Take the gap down` | 85.6, 206.5 | needs `clear.Dig_Rank`; talk `SelaDigShortcut`; `hoar.gapTaken`; lands at `Gap` |
| The cut | `Dig_Excavator` | 124, 184 (20 x 18) | 1 Muster Excavator at 128.5, 184 facing west |
| Tool chest | cache `hoar.dig.tools` | 131, 190.5 | |
| Third level fight | `Dig_Third` | 124, 219 (22 x 26) | 1 Muster Excavator, 2 Bridle-hound, 2 Frost Motes |
| Ancelane coffer | cache `hoar.dig.third` | 115.5, 232 | |
| Rig three | `Run the thaw rig` | 145.5, 228.5 (the rig at 147.5, 233.2) | `hoar.rig3`: plug three (x 150.8-153.2), the rank's ice (x 135.2-141.8) and the hounds' pen behind the plug |
| Rig three back ice | solid, 6.8 x 3 x 2.2 | 149.2, 235.7 | packs the 1.2 to 1.8 m gap between the rig's back and the north wall, on to the east wall: both of the rank's hounds wedged themselves in it on the 17:17 build of November 14 and the fight never cleared (nov14-recon24 s35). No rig or coupling may leave a gap a hound fits into (1.2 m) and a hero can't fight in (2.2 m): `HoarfellWorldTests.NoThawRigLeavesAGapAHoundFitsInAndAHeroCantFightIn` (melting ice aside) |
| The thaw comes (crossing, bark `thaw`) | | box 145.5, 228.5 (6 x 6) | needs `hoar.rig3`; `hoar.rank3Stepped` |
| The rank and the pen | `Dig_Rank3` | 146, 221 (18 x 22) | needs `hoar.rig3` (the rig stands inside it: the thaw comes straight out); 3 Thawed Guard from 138.5, 234.4 facing south, 2 Bridle-hound from 158, 215 and 158, 223 |
| Gap spawn | `Gap` | 168.5, 210 | |
| Winch talk (crossing) | `SelaMusterSlate` | box 172, 218 (3 x 30) | `hoar.slateRead`; sign `LOADING WINCH` |
| Door spawn | `Door` | 177, 211 | the boss checkpoint |
| Dig cradle | `Dig cradle — repair and save` | 173.5, 206 | free, saves at `Door` |
| Bench | `Fabricate — Lattice Anvil` | 184, 229.5 | |
| Idra joins (crossing) | `IdraJoins` | box 182, 218 (3 x 30) | `hoar.idraJoined` (Lastlight reads it); report `BottomGallery`; sign `BOTTOM GALLERY` |
| The winch up | `Ride the winch up — Lastlight` | 176, 228.5 | needs `hoar.marshalAsleep` ("Put the Host Marshal to sleep first"); to Lastlight's `Reach` |
| Marshal talk (crossing) | `IdraMarshalWakes` | box 187.5, 218 (3 x 8) | needs `hoar.idraJoined`; `hoar.marshalSeen`; saves at `Door` |
| After the Marshal | `IdraMarshalSleeps` | `AfterTalk` on `bossdown.HostMarshal`, delay 3 | sets `hoar.marshalAsleep` (Lastlight reads it) |

Idra ahead (`Guide`), where she waits: 7,60.5 (from the start) / 9,102 (`hoar.herdMet`) / -26,104.5 (`hoar.whiteoutMet`) / 18.5,161 (`hoar.whiteoutLifted`) / 31.5,186.6 (`hoar.vessaHeard`) / 57,184 (`hoar.ranksSeen`) / 84,189.5 (`hoar.rig1`) / 93.4,193.4 (`hoar.offerHeard`) / 110,216.2 (`clear.Dig_Rank`) / 178,224 (`clear.Dig_Rank3` or `hoar.gapTaken`); gone at `hoar.idraJoined`. Each wait is a step or two off the walked line, and her body never blocks anyone: it has no collider and no agent and is in no bake (a skinned body is drawn only), and it waits for a body in its way rather than walk through it.

## Encounter plan

| Fight | Where | Pack | Where the player stands to answer it |
|---|---|---|---|
| `Reach_Stalkers` | Spear field | 2 Rime-stalker | Outside the rings: the hides are 12 m or more apart on open snow, so the spent beat after a pounce is in the open and the next ring is visible |
| `Reach_West` (optional) | West pocket | 2 Frost Motes | A step back from the burst on open snow round the sledge |
| `Reach_Whiteout` | Stalkers' field, in the second whiteout | 2 Rime-stalker, 2 Frost Motes | In the open with markings unfogged, or back at vent pocket 2 (east) to wait out the white |
| `Reach_Camp` | The camp | 2 Frost Motes | 30 x 16 of trodden snow south of the boiler; the one thing standing in it is a sledge of cut ice at 15.1, 148.6 (solid), lying along the walked line 2.8 m clear of it |
| `Dig_Motes` | Upper gallery | 2 Frost Motes | The gallery is 22 m deep; keep south of z 190 or the amber rank wakes too |
| `Dig_Amber` (optional) | Upper gallery's north wall | 3 Thawed Guard | Behind the rank: they come out to a floor 22 m deep and 14 m of it free either side |
| `Dig_Second` | Second level | 1 Muster Excavator, 2 Bridle-hound | 34 x 28 m of floor: behind the excavator's front, the hounds taken mid-leap |
| `Dig_Rank` | Left gallery | 3 Thawed Guard | They step out east into a gallery 12 m wide with the second level's floor behind the party: round the end of the rank, not into its front |
| `Dig_Excavator` (optional) | The Excavators' cut | 1 Muster Excavator | Behind it: the cut is 22 x 20, and it works facing west |
| `Dig_Third` | Third level | 1 Muster Excavator, 2 Bridle-hound, 2 Frost Motes | 40 x 32 m of floor |
| `Dig_Rank3` | Third level's east end | 3 Thawed Guard, 2 Bridle-hound | The rank comes south out of the north wall, the hounds west through the melted plug: stay out of the rank's front with the hounds on you |
| `HostMarshal` | Bottom gallery | The Host Marshal | Contract below |

Counted as the combat engineer sized the chapter: on the Reach 4 stalkers and 6 mote swarms; in the dig 3 ranks (9 guards), 3 excavators, 6 hounds and 4 swarms (`FrozenHostTests.TheTalksRigsAndStraysSetTheFlagsTheScriptExpects` counts them).

## Camera

The fixed camera looks north-north-east from the south-south-west. Everything a fight or a talk needs is on the north side of what is walked: the serac ridge stands steep on its south face and slopes away on its north, under the sightline; in the dig every north wall is the glacier's full height (the ranks are in it) and every south side slopes under the sightline to the floor beyond it (the Drowned Waystation's rule, D119). Seracs, the boiler and the loudhailer mast stand only where nothing walked lies in their shadow along the camera's line. Nothing over 3 m and broader than a post stands on the camera's side of a fight.

The bottom gallery's camera side follows D119's rule, which D272 found sits just over the real line of sight: with the fixed camera a hero 1.5 m inside the south rim is seen whole (stills south-rim-150 to -210, `HoarfellWorldTests.AHeroAtAThawRigCanBeSeenWithTheWallThawingOrAsleep`, November 14), and one at the very edge, by the work lamps, loses his feet behind the ice. Drawing that rim lower as the Walking Canopy's is (a ground of its own, `CanopySeen`) was tried the same day and left the floor's detail cover on the south slope hanging over nothing and the floor-wall seam out of shade (`NothingFloatsOrSinksIn`, `FloorAndWallMeetInShade`): it needs the dressing laid on the drawn ground, and is left for the look pass.

## Review

Reviewer: Claude (world engineer B, chapter 6), from editor stills and plans, November 12. Nothing here has been walked in a player.

**Blockout** (`Builds/quality/look/host-blockout-01/`, greybox at rest; the bake's reach check passed with all 75 must-reach points on the first run). The route reads as an L: north across the Reach between the bridges and through the ridge's gap, then east and down the dig in steps. The ridge stands steep on its south face (the herd field's back) and slopes away under the camera on its north. The dig's north walls are the glacier's full height with the host standing in clear blocks at their feet, readable as soldiers in the ice from the play camera; the amber at the bottom gallery's mouth reads from the overview. Each level's floor has room for its fight's answer. Repaired before dressing: the herd and Idra stood at the origin in a still (both are now stood where they start at build, not only at runtime); the vent hollows were flat dark blots; the crevasses' lips were rows of snowballs; the glacier walls were a saturated cobalt with a wave pattern like water (now pale banded ice with thin blue year-lines and fracture planes).

**Dressed** (`Builds/quality/look/host-dressed-06/` at rest, `host-thawed-02/` with every plug and rank's ice melted, as a player walks the dig). `python tools/look_density.py <folder> --match FrozenHost --max-blob 0.22`: at rest every still but one passes (the Reach .01 to .12, the dig .03 to .15, the bottom gallery .11 to .19); the one over is the left gallery shot from behind its plug (.29), where nobody stands until rig two has melted it. Thawed, every still passes (worst .19, the left gallery .12).

Found and repaired while dressing:
- the snow went brown: moraine grit everywhere and the ground kit's sand drifts lay on it as beige (grit now only in thin streaks and at the vents' hearts; drifts are blue shade, the crust layer brighter than the snow under it);
- the dig's cut ice read as bathroom tiles, then as flagstones in a grid, and the same tiles lay in the Reach's scoured hollows (now plates of fractured ice, each its own tone, a crack between);
- the snow painted into the dig never showed, because the ice layer above it covered it (the ice is now left off where snow lies);
- white "snow patch" decals came out as dark blots (the sprite is dark and a tint only multiplies: they are meltwater now, fewer);
- built wind drifts read as flat-shaded slabs lying on the snow (removed: the ground's own relief and the crust ridges carry it);
- the spear tips were left out of the spear field itself and too thin to read (thicker, longer, denser, snow banked on their windward side);
- the bottom gallery would have had eight lights on its floor while the couplings run (the amber stretches glow by their own emission; five lights in each state);
- plug two stood 4.4 m tall between the camera and the left gallery's fight (all plugs are 2.9 m: nobody climbs that, and it is under the 3 m rule);
- each coupling's solid box had nothing drawn over it (an invisible wall to the shared check): it hangs under an iron skid the rig stands on;
- the lamp pools came out pink on the blue ice (a pale warm white now).

What was weak after dressing (and the coordinator's look notes on it, November 12): the dig's levels looked alike; the host stood at the top edge of most dig frames; the Reach was one ripple texture in every frame; the vent pockets were a dark disc in a ring of stones.

**Look pass** (`Builds/quality/look/host-look-04/`, at rest; contact sheets in the report). What it did is in "The host where the camera looks", "The three levels and the muster ground", "The Reach at a larger scale" and "The vent pockets" above; no route coordinate or contract row moved. Floor measure: every still at or under .22; the left gallery shot from behind its plug is the worst, at .22 (it was .29). Found and repaired on the way (`host-look-01` to `-03`):
- the polished snow's first texture had rims to its scallops and read as a honeycomb of tiles, then as a regular grid of dots (now two fields of dimples, centres thrown anywhere);
- the crust's three ridge fields met in zig-zag seams like herringbone (the ridges now die out into smooth snow where two fields meet, and wander more);
- the second level's half-out ranks were first flat glowing yellow slabs with the bodies standing on them like trays of pieces, then mounds of duller amber; amber is kept for what wakes, so they are now thawed-and-refrozen blue-white mounds, rimed, their hoses frosted and dead, with no glow and no steam;
- the cairns were stacks of round stones and read as snowmen (now flat slabs, each set off and turned); the windbreaks' round stones read as sandbags (now four courses of flat stones in a rock material, dark);
- the vents' pools never laid: a strong decal (pebbles) had already claimed the spot (pools are laid first now);
- meltwater pools on the second level read as holes in the ice (the shared water is dark): taken out;
- flush column footings on the third level read as pads to step on: taken out for standards leaning out of the ice and drums half in it;
- the host under the Reach's scoured ice didn't show (the block grid seldom fell in a window): ranks are laid under each window and along the herd's line by name;
- softening the cut ice's joints made the bottom gallery and the cut plain to the floor measure (.42, .28): the joints are back as they were.

What is still weak:
- **The host beneath is soft**: on purpose (it must lose to every danger marking), but on the second level its shapes sit among the frozen meltwater stains and can read as stains; under the Reach it is faint.
- **The east-wall ranks in the bottom gallery's mouth** are seen mostly through their blocks' tops: the figures read as dark shapes in ice, not as soldiers.
- **The third level's stonework is at its edges**: the columns and fallen drums are out of most frames of the walked line; what a player sees there is the standards, the grey paving and the clearest host beneath.
- **The bottom gallery's shelf ranks and the Marshal's dais** are 18 to 22 m from its centre, beyond the frame from most of the floor (the contract's size); the Marshal's post inlay and the host beneath carry the muster ground.
- **Nothing runtime is in a still**: the herd walking and bolting, the whiteouts, the ice warming and melting, Idra walking ahead, the dormant ranks inside their ice (they are spawned at load), steam and spindrift.
- **The overviews are washed out** by the zone's fog at four times the play distance.

## Boss arena contract

*The combat engineer's section: what the Host Marshal fight needs from the room (components, sizes, anchors, what must be breakable or climbable, where the boss ends). The world engineer builds to it and writes the coordinates as built underneath.*

What the world engineer has planned, for the contract to change: the bottom gallery is a round floor of radius 18 m on **C = (208, 218)**, at -17.5, entered from the west through a passage 6 m wide (x 186-192, z 213-223) off the bottom gallery's mouth, where the checkpoint (`Door`, (177, 211)), a free cradle and a bench are. The north and east walls are the glacier's full height (about 22 m of blue ice) with the frozen host in ranks at their foot; the south and south-west rim stay under the sightline. The encounter is `HostMarshal` (the main quest's step 6 names it), the door crossing `IdraMarshalWakes` (`hoar.marshalSeen`). Write here what the fight needs: where the three couplings stand and what they are, the walls in thirds and where the ranks step out, the closing ring of phase two, where the Marshal kneels.

Written November 12 by the combat engineer, to that plan. The fight's moves are `S/Combat/BossController.HostMarshal.cs` and its numbers that file's `MarshalTune`; the couplings are `S/Combat/ThawCoupling.cs`, the closing frost `S/Combat/FrostRing.cs`; the definitions are `HoarfellBuilder.Enemies.cs`. Its tests (`T/PlayMode/HoarfellCombatTests.cs`) build a stand-in of this room on a bare floor in the proving yard, so nothing here waits on the dig. Positions are from **C**, +z north; a bearing is degrees from north toward east.

**How the fight uses the room.** The Marshal wakes in the north wall and musters its host. Three thaw couplings stand round the gallery, each feeding one stretch of the host's wall (the tall north and east wall, in three). While a coupling runs, its stretch is amber and the rank of three standing in it can be called out: the Marshal's rallying call wakes the rank of the running coupling nearest the lead, once per stretch (the fight opens on it, so the couplings are taught first). Breaking a coupling refreezes its stretch: its rank never wakes, and any of its soldiers already out freeze where they stand and stay as ice (cover, and what the Marshal's spear lodges in for longer). While any coupling runs the rig keeps the Marshal warm (a blow on it lands at three tenths, read as resisted; an opening its own moves give lands whole; and nothing breaks it, its break meter held at nought: November 14, after the replay driver broke it seven times a fight and beat it past half with two couplings still running, 94.8 s in all); each coupling takes a sixth of its bar, the first two never carry it over half, and the third always does: that is the shift. Phase two is a duel in the cold: no ranks, the Marshal slower and heavier, frost closing in from the rim, its sweeps cracking the floor. At nothing it kneels where it stands and the frost takes it: put back to sleep, not destroyed.

| Thing | What the fight needs | Why |
|---|---|---|
| Floor | As planned: round, radius 18 on C. Walkable and near level to **r 17** (no step over .3 m inside r 16). Nothing solid inside **r 12.5** but the boss and the couplings | The lunge runs 14 m, the sweep reaches 8.5 m from the Marshal's middle and its cracks 11.5, and phase two's frost closes from r 17 to r 9: a column or a pit in there stops a dodge or hides the party behind a 4.8 m body |
| Couplings | **Three**, centres **13.5 m** from C, at bearings **330, 30, 90** (in front of the north-west, north and east stretches of the tall wall), standing on the floor, not in the wall: within reach of a blade and a shot from every side. Each is an object with a `ThawCoupling`, a `Health` (id **`Thaw coupling`**: what the target card says), a trigger `SphereCollider` (radius **1.3**, centre **1.5 m** up) and a `Hurtbox` owned by that `Health`. Fields: `index` **1** (bearing 330, nearest the door), **2** (30), **3** (90); `settledBy="HostMarshal"`; `defeatedFlag` **`hoar.coupling1`**, **`hoar.coupling2`**, **`hoar.coupling3`** by index (the main quest's `storyFlags` name them); `whole` = the `ThawRig` prefab (3 m) with an amber point light under it; `broken` = the rig iced over and dark, hidden until it breaks. Leave `Health.maximum` alone: the boss sets each coupling's integrity, weakness (Kinetic) and resistance (Plasma) as the fight begins | A break target that answers to a boss is shut and untargetable until that boss is awake, so nobody breaks one from the door (chapter 3's fault). A broken coupling is kept in the save; all three load broken once the Marshal sleeps. The frost ring centres on the circle the three stand on, so keep the three at one distance from C (that circle's centre is C wherever they stand round it) |
| Its stretch of wall | On each `ThawCoupling`: `thawing` = the amber of its stretch (the lit ice and the soldiers in it, its point light), `refrozen` = the same stretch in blue ice, hidden until it breaks. The coupling swaps them; nothing else needs to. The stretches: bearings 300-0 (index 1), 0-60 (2), 60-120 (3) | "Each one broken refreezes a third of the wall": the player sees which third went to sleep |
| Its rank | On each `ThawCoupling`: `ranks` = **three** empty transforms on the floor where its soldiers step out of the wall, at **r 16**, bearings the coupling's **-16, 0, +16**, each with 1.2 m of open floor round it | The rally calls them out there; the three ranks are the guards' first stand. With no `ranks` the boss calls them out beside itself (the proving yard) |
| Boss | Its `Spawner` at bearing **0**, **r 10** (in front of the niche in the north wall it steps out of), facing the door (yaw about 250), `faceAsPlaced=true`. `previewPoint` in the passage with a radius that takes it in, so it stands shown and inert (`DormantEnemy`) until the fight | In the room from the first look in; behind it from the camera there is only the north wall |
| Door | As planned: the west passage, encounter **`HostMarshal`**, trigger covering the floor (about **36 x 8 x 36** on C), `requiredFlag` **`hoar.marshalSeen`** (the `IdraMarshalWakes` crossing), `checkpointSpawn` **`Door`** (outside the seal, 31 m from C, on navigation, the cradle beside it). One `Membrane` seal across the passage, strands drawn instead of its cube | The shared door tests (checkpoint, retry, no way out mid-fight). The boss sheet in `HoarfellCombatTests` names zone `FrozenHost`, door `HostMarshal`, door flags `hoar.idraJoined`, `hoar.marshalSeen`: write back any other flag the door waits on |
| Rim | As planned: south and south-west under the sightline; nothing over 3 m and 7 m across between the camera and C. Nothing of the fight stands on the south half, so the party always has open floor there | Fixed three-quarter camera |
| Frost ring | Nothing to build: phase two draws it (`FrostRing`: a pale boundary ring from r 17 closing to r 9 over 70 s; a hero on it walks at .6). The floor must be walkable everywhere from r 9 to r 17, the passage's mouth included | The closing ring of ice the brief asks for; a boundary and not a danger (D217), so no orange, red or purple decal under it |
| Light | Each coupling's amber light (three; out when it breaks), plus fixed cold lamps that keep the gallery readable with all three out (six lights at most on the floor renderer) | The thaw is the dig's light and the cold is its end |
| Leaving | None: no `BossExit`. It kneels where it falls (`Ending.ShutDown`) and the fight leaves its body there as ice for the rest of the visit | Put back to sleep, not destroyed (brief) |
| After the fall | `IdraMarshalSleeps` by `AfterTalk` on `bossdown.HostMarshal` (sets `hoar.marshalAsleep`). The boss plays no down bark, so nothing talks over that scene (manifest 8.9). On a later load with `bossdown.HostMarshal` set, stand the `HostMarshal` prefab kneeling at its spawner, drawn only, under a `FlagGate` shown when set; the couplings load broken and their stretches refrozen by themselves | A won gallery must not load thawing, nor empty of what the party put to sleep |

What the fight sets and logs, for the builder's own checks: `bossdown.HostMarshal`; `hoar.coupling1`, `2`, `3` as each breaks; `COUPLING_BROKEN <index>`, `MARSHAL_RALLY <index>`, `MARSHAL_LODGED`, `MARSHAL_FALTERS`, `BOSS_SHIFT HostMarshal 2`, `FROST_RING`, `MARSHAL_ASLEEP`. Its track is `Boss of the Ages` (its `EnemyDef.bossMusicId`).

**Ordinary enemy ids for the zone's encounters** (`HoarfellBuilder.Enemies.cs`): `RimeStalker`, `FrostMotes`, `ThawedGuard` (a rank of three forms by itself from three woken within 7 m of each other), `MusterExcavator`, `BridleHound`. The XP the chapter is sized to assumes about the plan above: 4 stalkers, 6 mote swarms on the Reach; three ranks, 3 excavators, 6 hounds and 4 mote swarms in the dig (the optional pockets included).

### Coordinates as built

World engineer, November 12, from `HoarfellBuilder.FrozenHost.cs` (its constants `C`, `CouplingOut`, `RankOut`, `MarshalOut`, `ShelfRadius`; `FrozenHostTests.TheBottomGalleryIsBuiltToTheBossContract` checks them in the built scene).

| Thing | As built |
|---|---|
| C | **(208, 218)**, floor **-17.5**, flat (no step anywhere inside r 18). The floor reaches r 18 on the south and west and r 20.5 under the tall wall (bearings 292 to 128), where the outer 2.3 m is the rank shelf: clear ice standing on it, solid (static boxes every 6 degrees at r 19.3, in the bake), so the walkable floor ends at about r 17.6 there |
| Couplings | `Thaw coupling 1` at bearing **330** (201.25, 229.69), `2` at **30** (214.75, 229.69), `3` at **90** (221.5, 218): r 13.5, each facing C. On the root: `Health` id `Thaw coupling` (maximum left alone), trigger `SphereCollider` r 1.3 at 1.5 up, `Hurtbox` owned by it, `ThawCoupling` index 1/2/3, `settledBy` HostMarshal, `defeatedFlag` `hoar.coupling1/2/3`. `whole` = the `ThawRig` prefab (2.8 m, on the skid) and an amber point light (range 10, 2.6) at 2.6 up; `broken` = the same rig tinted to ice with a crust of rime, hidden until it breaks. Under the root an iron skid 2.3 m square, always drawn, carrying the solid box (1.8 x 2.6 x 1.8) and a carving `NavMeshObstacle`, so it is solid and walked round in every state |
| Its stretch | `thawing` = a curved band of amber ice on the shelf (r 18.3 to 20.4, 3.8 to 4.3 m tall) over bearings **300-360** (1), **0-60** (2), **60-120** (3), glowing by its emission; `refrozen` = the same band in clear blue ice with a cold light (range 12), hidden until the coupling breaks. The host stands in the shelf in both states: a rank of three every 10 degrees from 304 to 116, at r 19.4, facing C (drawn only) |
| Its rank | three empty transforms per coupling at **r 16**, bearings the coupling's **-16, 0, +16**, facing C, on the paths and 2.2 m clear of the shelf's face |
| Boss | Spawner at bearing 0, r 10: **(208, 228)**, yaw **250**, `faceAsPlaced`, radius 0. `previewPoint` `Host Marshal preview (the door)` at the passage **(190, 218)**, radius 22.6 (takes in the spawner). The Marshal's niche in the north wall behind it: x 203-213, z 236-242 |
| Door | The west passage x 186-193, z 214-222 at -17.5. Encounter `HostMarshal`, trigger **36 x 8 x 36** on C, `requiredFlag` `hoar.marshalSeen`, `checkpointSpawn` `Door`, one membrane: `Bottom gallery seal` at **(191, 218)**, 8.8 wide turned to span the passage, its cube removed; a wall of ice posts drawn while it is shut, a carving obstacle while it is shut. The crossing `IdraMarshalWakes` (box (187.5, 218), 3 x 8) needs `hoar.idraJoined`, sets `hoar.marshalSeen`, saves at `Door`: the door flags are `hoar.idraJoined` and `hoar.marshalSeen`, nothing else |
| Checkpoint | Spawn `Door` **(177, 211)** in the bottom gallery's mouth, 31.8 m from C, on the paths, outside the seal. Free cradle `Dig cradle — repair and save` at (173.5, 206), saves at `Door`; a bench at (184, 229.5) (the Party page, to choose who rests before the fight) |
| Rim | South and south-west: the gallery's wall slopes under the camera's line to its floor (the builder's sightline rule). North and east: the glacier's full height, about 22 m. Nothing over 3 m and 7 m across stands between the camera and C |
| Frost ring | Walkable from r 9 to r 17 everywhere but within 1.6 m of a coupling (its skid), the passage's mouth included |
| Light | While it runs: the three couplings' amber lights and two cold work lamps at the south rim's foot (bearings 150 and 215, r 17.6): five. Once all three are broken: the three refrozen stretches' cold lights and the two lamps: five |
| After the fall | `AfterTalk` `The gallery after the Marshal`: flag `bossdown.HostMarshal`, node `IdraMarshalSleeps`, speaker Idra, `heardFlag` `hoar.marshalAsleep`, delay 3. On a later load the `HostMarshal` prefab kneels at the spawner (its Down clip held at .28, iced, drawn only) under a `Whereabouts` from `bossdown.HostMarshal`, decided at load only so it never stands beside the body the fight leaves. Every amber body of ice in the dig goes blue on `hoar.marshalAsleep` (`IceThaw.refreezeFlag`). No `BossExit`. The way out is the loading winch (`Ride the winch up — Lastlight`, on `hoar.marshalAsleep`) |
