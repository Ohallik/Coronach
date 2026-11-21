# The Separation Works - the keel, the press galleries, the control room and the press hall

Functional brief, written before construction (November 12, the Works' world engineer, chapter 9). **MAP_EYE_TEST: blockout OPEN; dressed OPEN; in-player OPEN.** Chapter brief: [ch09-the-house-that-can-leave.md](../chapters/ch09-the-house-that-can-leave.md); script wiring: [ch09-manifest.md](../writing/ch09-manifest.md) and docs/chapters/ch09-wiring.md. Builder: `S/Editor/World/HomeBuilder.Works.cs` (`HomeBuilder.Works`). Mechanics: [CHAPTER_GUIDE.md, "World mechanics"](../CHAPTER_GUIDE.md) (15.28 is this zone's: `SeparationJoint`, `DrivenPress`; it also uses 15.3's hand, 15.6 and 15.17).

## Purpose and formation

Cinder Halo's original separation plant, under the promenade of the Decks. The station was put together from vessels a hundred years ago, and the plant is the keel they were joined along: walk it and you walk through the ships themselves, one hull after another, each in its own make of plate, joined by the **separation collars** Taren has been tightening every year for eleven years. It is the underside of chapter 1's Decks and is built of the same things: blue-grey deck plate, hull plating, the station's warm work lamps and its yellow-and-black joint paint.

**The route's shape is a walk through the hulls**, north from the hatch under the promenade: the **tug** (where the hatch comes down), the **hauler**, the **liner**, and down into the **ore barge**'s hold, where the plant's press hall is and the Foundry Saint stands. A collar stands between each hull and the next, and each is a joint to reopen by hand. A player can say it in one breath: *through the tug, the hauler and the liner, a collar between each, to the press hall*. Off each hull, a collar in its side wall opens onto a pocket (optional, each with a cache).

**The rule** (the zone's one): *a Compact rig is clamped to anything that's been turned on you, and a cyan cable runs from the rig to the machine. Cut the rig and the machine is home's again.* A driven press hunts: it marks the floor under whoever comes into its bay and drops on it; a pinning press holds its ram down on a joint so nobody can work it. Cut the rig (an Override Rig, the white Compact drone clamped at the end of the cable) and every machine on its cable lets go: the cable drops slack and goes dark, the cyan lamps on the machine go out and the plant's warm lamps come on, and the ram climbs to rest under local control. Iron is iron: a hunting press drops on whatever is under it when it falls, a Compact crew too.

- **Taught** in the hauler's throat, with nothing awake but the rig: a line of three presses across the only way north, their rig clamped on a post at the throat's east jamb, its three cables in plain sight. The presses hunt whoever steps into a bay; cut the rig and they climb to rest.
- **Used to get somewhere**: the second joint, at the hauler's north end, has a press standing on it. Its cable climbs the east wall and runs to the **control room**, where its rig is clamped to the plant's board. Follow the cable, cut the rig, and the press lifts off the joint.
- **Used in a fight**: the liner's press gallery, five hunting presses on one rig, with a Compact crew and a Weld Arm in the room. Fight under the hammers and let them fall on the crew, or go for the rig on its post first and fight on quiet floor.

**A joint is opened with the hands** (Oriel's release, on home's own iron; manifest prompt *Take the joint's weight*). At each collar's foot stands a screw jack with a red hand wheel; taking the joint's weight is working the jack. The joint answers in two beats: it **takes** (the wheel turns, the collar groans, its eight locking dogs draw back one after another with a knock each), then it **gives** (the collar ring turns a few degrees, the seam opens, the two hull leaves part and slide into the walls, dust falls from the seam, the collar's lamp goes from dim to the plant's warm white). Dorrow says it first at the first joint: *take the weight off each one, and she'll show you which way she wants to go*.

## The six joints, and what each shows

| Joint | Flag | Where | Path | What it shows of what the station is made of |
|---|---|---|---|---|
| 1 | `home.joint1` | The tug's bow into the hauler, north wall of the tug's hold | critical | **A tug's bow still carrying its name**: the tug's red-oxide bow plating, its name *STAY A WHILE* across it in faded white, married to the hauler's ochre stern at the collar. Dorrow is here with his ear to it (`DorrowWorks`) |
| 2 | `home.joint2` | The hauler into the liner, north end of the hauler | critical | **A hauler's ribs**: the bulk hauler's great frames bolted to the liner's flat teal plate; the joint is set through the last rib. A press stands on it, driven from the control room |
| 3 | `home.joint3` | The liner into the ore barge, north end of the press gallery | critical | **A collar with a repair of Taren's own on it**: a bright patch plate across a crack in the collar, his weld bead round it, and eleven scratches under it, one a year. Beyond it the stair down to the press hall |
| 4 | `home.joint4` | The tug into the Compact module, east wall of the tug's hold | optional | **The one hull the Compact built**: white plate on a bolt pattern that never matched the station's, a shim in every hole. Two Compact crew inside; a cache |
| 5 | `home.joint5` | The hauler into the liner's baggage bay, east wall of the hauler's south half (first planned on its west wall, where the camera saw the collar edge-on) | optional | **Where people lived in the joins**: the Arrens' wedding table, built inside the bay forty years ago and wider than any hatch (Mira's third consignment, `SelaWeddingTable`); a cache |
| 6 | `home.joint6` | The liner into the courier skiff, east wall of the press gallery | optional | **A courier skiff's nose**, the first lock nine, its lock number still stencilled on it, grown into the liner's side; a Weld Arm in it; a cache |

Six hull standards in all, each in its own plate: the tug (red oxide), the hauler (ochre, ribbed), the liner (pale teal panels), the ore barge (dark iron, the press hall), the Compact module (white), the courier skiff (cream). Each room's floor and walls take its hull's colour, so no two rooms read as one.

The three critical joints' hands report **`Joint`** (the main quest's step 9 counts three); the three optional joints' report nothing, so they never count toward it. Every joint's state is its flag and a save loads it open with nothing played. The control room's board shows all six: a lamp for each joint, dim while shut, warm once open.

## Users and access

Nobody lives here. The Compact's crews and rigs came down the service hatches while the town met. Dorrow is at the first joint until the Saint is released. The party arrives down the hatch from the Decks' promenade (spawn `Arrival`; the Decks' way down is the Decks engineer's, to this spawn) and leaves by the same stair (`Climb to the promenade — The Decks`, to `DecksLeaving` spawn `Works`). **After the Saint the way home is short**: a lift at the press hall's north-east corner, `Ride the lift up — The Decks`, locked until `bossdown.FoundrySaint`, to `DecksLeaving` spawn `Lift` (by Hal's workshop, where the next scene is). Both spawns are the Decks engineer's to stand.

## Layout

About 62 x 230 m; about 210 m of route from the hatch to the press hall, plus the control room (40 m there and back) and three pockets. x runs east, z north; heights are world y. Floors: the tug 0, the hauler -1.5, the control room 0, the liner -3, the ore barge (the landing and the press hall) -6.

```text
 z 214   +------------------- PRESS HALL (-6) ------------------+ lift (NE corner)
         |                 the Foundry Saint at P                |   contract below
 z 174   +------------------------[door]-------------------------+
 z 166         [ LANDING -6 ]  checkpoint PressHall, cradle
 z 152            | stair |
 z 147     ======[ JOINT 3 ]======   Taren's repair
         +--------------------------------+
         |  THE LINER: PRESS GALLERY (-3) |==[ JOINT 6 ]== COURIER SKIFF (cache)
         |  five hunting presses, one rig |
 z 101   +--------[ JOINT 2 ]---------+
         |  pinning press on joint 2  |
         |  THE HAULER (-1.5)         |==stair==  CONTROL ROOM (0): the rig on the board
 z  70   +---+  THROAT: 3 presses +---+
 z  59   +---+  and their rig     +---+
         |                            |==[ JOINT 5 ]== BAGGAGE BAY (the wedding table, cache)
 z  41   +--------[ JOINT 1 ]---------+   STAY A WHILE | hauler plate
         |  THE TUG'S HOLD (0)            |==[ JOINT 4 ]== COMPACT MODULE (cache)
 z   4   +-----+  hatch  +----------------+
               [arrival]  stair up
```

| Area | What it is for | Size | Level | Combat |
|---|---|---|---|---|
| Hatch | The arrival from the promenade: the stair up, a free cradle | 14 x 17 m | 0 | none |
| The tug's hold | Joint 1 taught by hand with nothing awake; Dorrow | 22 x 32 m | 0 | none |
| Compact module (optional) | Through joint 4: the white hull, a crew, a cache | 15 x 16 m | 0 | `Works_Module` |
| Collar 1 | Down to the hauler | 5 x 6 m | 0 to -1.5 | none |
| The hauler, south | The rule seen: the throat's press line and its rig ahead | 22 x 19 m (narrowed from 26: at 26 a still was all floor) | -1.5 | none |
| The throat | The rule taught: three hunting presses across the way, their rig on the east jamb | 15 x 12 m | -1.5 | `Works_Throat` (the rig alone) |
| The hauler, north | Joint 2 under its pinning press; the cable to the control room | 22 x 27 m | -1.5 | none |
| Baggage bay (optional) | Through joint 5, off the hauler's south half: the wedding table, a cache | 15 x 16 m | -1.5 | none |
| Control room | Up a stair off the hauler's east side: the plant's board, the rig that drives joint 2's press | 15 x 20 m | 0 | `Works_Control` (rig, two crew) |
| Collar 2 | Down to the liner | 5 x 7 m | -1.5 to -3 | none |
| The liner: press gallery | The fight under the hammers | 32 x 45 m | -3 | `Works_Gallery` (crew, Weld Arm), `Works_GalleryRig` |
| Courier skiff (optional) | Through joint 6: a Weld Arm, a cache | 14 x 16 m | -3 | `Works_Skiff` |
| Collar 3 and the stair | Joint 3, then down to the ore barge | 6 x 18 m | -3 to -6 | none |
| Landing | The press hall's door: the checkpoint, a free cradle | 20 x 8 m | -6 | none |
| Press hall | The Foundry Saint (contract below) | 48 x 40 m | -6 | `FoundrySaint` |

**Travel** (about 6.7 m/s): about 290 m with the control room: under a minute of walking. With five fights and three optional, six joints, the talk and the boss it is about 20 to 25 minutes, the chapter's dungeon share.

## The machines (the zone's mechanic)

| Thing | Where | What it does while its rig lives | Once its rig is cut |
|---|---|---|---|
| Throat presses (3) | Across the throat, three bays 4.8 wide by 5 deep at z 62.4 to 67.4, x -7.3 to 7.3, under one gantry wall to wall | Hunt: a hero stepping into a bay has it marked for .9 s, then the ram drops (an ordinary hit's share, never the last of it; thrown out of the near side). Anything else under it is struck too | Climb to rest, warm lamps, cable slack: any floor |
| Joint 2's press | Over joint 2's jack and hand, x 2.6 to 6.2, z 92.65 to 97.05 (its frame runs north-south over the bay, clear of the collar's way) | Pins: its ram stands down on the collar's jack, a solid block; the joint's hand is refused | Climbs to rest: the hand is offered |
| Gallery presses (5) | Two rows in the liner, bays 5 x 5: z 112 to 117 (centres x -10, 0, 10) and z 127.5 to 132.5 (x -5, 5); each its own frame, two columns at the bay's ends and a crown running north-south (a gantry across the room stood between the camera and the fight) | Hunt, as the throat's | Rest |
| Rigs | The throat's east jamb; the control room's board; the gallery's east post | An Override Rig (the first combat engineer's) clamped at the cable's end, its own encounter of one | Its encounter's `clear.` flag is what every press on its cable reads |

## Identities

Scene `SeparationWorks` (GroundCombat, chapter 9, threat 1, no sky). Music: Moon Caverns; the Saint: Glitch Boss (its definition). Light: `WorksLooks` in the builder. Profile: its own, `Works` (cool, low bloom: the marking's orange must win over every lamp).

## Coordinates (for routes and tests)

All world x, z (y is the floor there: 0 in the tug and the control room, -1.5 in the hauler and the baggage bay, -3 in the liner and the skiff, -6 on the landing and in the press hall). **As built**: the numbers are `HomeBuilder.Works`'s constants, and `SeparationWorksTests` checks the hands, the rigs, the ways and the hall against them in the built scene. A point to press is where a hero stands.

| What | Id or prompt | x, z | Flag / note |
|---|---|---|---|
| Arrival spawn | `Arrival` | 0, -5 | |
| Way up | `Climb to the promenade — The Decks` | prompt at the stair's foot 4.2, -10; the stair climbs north along the hatch's east wall to the promenade's hatch | to `DecksLeaving` spawn `Works`; open |
| Hatch cradle | `Hatch cradle — repair and save` | -4.6, -1.4 | free, saves at `Arrival` |
| Dorrow's talk (crossing) | `DorrowWorks` | box at 0, 29.6 (23 x 3) | sets `home.worksRead`; Dorrow stands at -4.2, 32.2, his ear to the collar, until `home.saintReleased` |
| Joint 1 | `Take the joint's weight` | collar 0, 36.3; jack 4.4, 35.35; hand 4.4, 34.0 | `home.joint1`, report `Joint` |
| Joint 4 | `Take the joint's weight` | collar 11.1, 20 (east wall); jack 10.15, 16.4; hand 8.8, 16.4 | `home.joint4` |
| Compact module | `Works_Module` (needs `home.joint4`) | 20.2, 20 | 2 CompactWarden at 21, 23.5; cache `home.works.module` "Compact module locker" at 25.6, 15 |
| Throat rig | `Works_Throat` | post 9.8, 57.4; the rig 9.0, 56.3 (yaw 220); trigger 24 x 9 on 1.5, 57; shown inert from 0, 47 (r 18) | 1 OverrideRig; frees the three throat presses on `clear.Works_Throat`; first sight `override` (once: `home.works.sawRig`) |
| Joint 5 | `Take the joint's weight` | collar 11.1, 50 (east wall of the hauler's south half); jack 10.15, 46.4; hand 8.8, 46.4 | `home.joint5` |
| Wedding table | `Look at the table` | table 20.4, 52.6; hand 20.4, 49.4 | flag `mira.lotTable`, `SecondInventory` step 0, report `Consignment`, node `SelaWeddingTable` (Sela) |
| Baggage-bay cache | "Baggage-bay trunk" | 25.8, 44.4 | `home.works.baggage` |
| Control room | `Works_ControlRig`, `Works_Control` | the rig 25, 90.2 (yaw 180) at its post 25, 91.6, before the board on the north wall (24.6, 93.4); room x 17.4 to 32, z 74 to 94, up the stair x 10.2 to 18.2, z 79.4 to 84.6 | the rig frees joint 2's press on `clear.Works_ControlRig`; 2 CompactWarden at 24.6, 86; floor sign `CONTROL ROOM` at 19.6, 80.4 |
| Joint 2 | `Take the joint's weight` | collar 0, 97.3; jack 4.4, 96.35; hand 4.4, 95.0, under its press | `home.joint2`, report `Joint`; the hand's `requiredFlag` is `clear.Works_ControlRig` |
| Gallery | `Works_Gallery`, `Works_GalleryRig` | trigger 31 x 40 on 0, 123; the rig 13.1, 121.2 (yaw 270) at its post 14.2, 121.2 | 3 CompactWarden at 0, 122.5; 1 WeldArm at -11, 140.5 (yaw 160); the rig frees the five presses; first sight `weldarm` (once: `home.works.sawArm`) |
| Joint 6 | `Take the joint's weight` | collar 16.1, 134 (east wall); jack 15.15, 130.4; hand 13.8, 130.4 | `home.joint6` |
| Courier skiff | `Works_Skiff` (needs `home.joint6`) | 25, 134 | 1 WeldArm at 29.6, 138.6; cache `home.works.skiff` "Courier's lockbox" at 29.4, 129.8; `LOCK 9` on its nose |
| Joint 3 | `Take the joint's weight` | collar 0, 146.3; jack 4.4, 145.35; hand 4.4, 144.0 | `home.joint3`, report `Joint`; Taren's patch on the collar's west side |
| Checkpoint | spawn `PressHall` | -3, 165 | cradle `Press hall cradle — repair and save` at 6.6, 165; floor sign `PRESS HALL` at -6.4, 167.4 |
| Press hall | `FoundrySaint` | P 0, 196; the door's trigger 46 x 26 on 0, 187 (z 174 to 200) | contract below, and its as-built table |
| Lift up | `Ride the lift up — The Decks` | cage 20.8, 209.8; prompt 19.6, 208.6 | to `DecksLeaving` spawn `Lift`; locked until `bossdown.FoundrySaint` ("The lift answers the press hall's board: release the Foundry Saint first") |

## Encounter plan

| Fight | Where | Pack | Where the player stands to answer it |
|---|---|---|---|
| `Works_Module` | Compact module (optional) | 2 Compact Warden | The module is 15 m across and open: break one and it gives ground; the other comes on |
| `Works_Throat` | The throat | 1 Override Rig | On the throat's south floor, out of the bays: the rig is on its post at the east jamb |
| `Works_ControlRig`, `Works_Control` | Control room | 1 Override Rig on the board; 2 Compact Warden | Between the desks: the rig first, or the crew |
| `Works_GalleryRig`, `Works_Gallery` | The liner | 1 Override Rig on the east post; 3 Compact Warden, 1 Weld Arm on the north wall | In the gaps between the bays; draw the crew into a marked bay and step out; or cut the rig first |
| `Works_Skiff` | Courier skiff (optional) | 1 Weld Arm | Off its strip |
| `FoundrySaint` | Press hall | The Foundry Saint | Contract below |

## Camera

The fixed camera looks north-north-east from the south-south-west. Every hull is walked north, its south and west walls kept under the sightline to the floor beyond (the Waystation's rule), its north and east walls tall: the collars, the bows, the ribs and the presses' frames stand there. A press's frame stands across its bay with its crown five metres up, so a hero waiting at a bay's edge has the ram and the crown in frame; a rig's post stands a stride from the walked floor on the east side, so a hero at the bays sees rig, cable and press in one frame.

**The keel as it is drawn (D272, November 14).** Bodies and the bake meet the keel as `Works.Height` has it, which on the camera's side of a floor climbs at the camera's own pitch from six tenths over the line to a hero's feet. That is the rule the Walking Canopy's near walls were built to, and they hid a boss fight's whole aftermath. What the camera sees is `Works.Seen`: on the camera's side of any floor a kerb of six tenths, then a slope four tenths and more under the real line to the feet (the camera hangs 12.1 m over the hero and 15 m back, 20.7 degrees off north), and nothing within five metres of the lens of anyone on any floor. The hull plate, the pipes and the work lamps stand on the wall as it is drawn: a lamp or a pipe where the drawn wall is too low for it isn't hung (`WORKS_LAMP_UNMOUNTED`, `WORKS_PIPE_UNLAID` in the build's log). Three fixtures that hung in the air went: a lamp over the hatch where no wall is, one before the bow's west wall, one on the throat's west jamb; the tug's steam main on its west wall is gone, and the landing's lamp is on the landing's east wall (its north wall is the press hall's foreground). `SeparationWorksTests.WhatIsDrawnOnTheCamerasSideOfAFloorStaysUnderItsRealLineOfSight` measures it.

**What each fixture is part of.** A collar's dogs and its number are part of the collar ring (the joint still sets each dog's place as it draws back); the collar's lamp sits on the jack's head, where the hand works it; Taren's patch is on joint 3's flange and turns with it; the tug's name, lock nine's number and the Compact's mark are painted on their hull's plate; every pipe run is fixed to the plate it runs along; each press's ram and control box hang from its own frame, and the gallery's five bays are five frames; the control room's board stands on its console; the Saint's arms hang from the press, which stands on its bed (a flush iron plate 11.4 x 6.4 m at P).

## Review

(Written as the zone is built and seen.)

**The keel's cut faces (November 14, a look engineer, on the lead's note to `ch9-wr-02`):** `Works.KeelWork` dresses the keel as drawn (`Seen`) in the band a frame shows round every floor (.9 to 9 m off it): frames every 1.5 m and longitudinals every 3 m in pale plant iron, bays plated in the nearest hull's paint or stripped dark with the next hull's frames inside, torch cuts with scorched rims, cable runs on the camera's side of every big room, and cuts in progress on the keel beside the walk (white-gold, sparks, a glare turned to the camera, bottles and hose; never within four metres of a floor along the camera's line). The keel's own texture is a hull's framing too: plates with rivets, a frame at each edge, plates cut away to the dark, torch cuts, a cable run. Laid along the keel's normal, so its ridges' faces carry it. Never more than a hand over the drawn keel, lower by the kerb; drawn only. Log line `WORKS_KEEL`. Stills `Builds/quality/look/works-keel-04`.

## Boss arena contract

*The second combat engineer's section: what the Foundry Saint needs from the press hall (floor, where it stands, its arms and their collars, the climb to the rig on its back, the door, the way out after). The world engineer builds to it and writes the coordinates as built underneath.*

(Planned room, for the contract to be written against: the press hall is x -24 to 24, z 174 to 214 at floor -6, centre **P = (0, 196)**; its door from the landing at z 172, 6 m wide; checkpoint `PressHall` at (-3, 165), on the landing; the lift at the north-east corner, (21, 210), locked until `bossdown.FoundrySaint`. Anything here can move to the contract.)

Written November 12 by the second combat engineer, to the planned room above (P = (0, 196) at floor -6, door at z 172, checkpoint `PressHall` at (-3, 165), lift at (21, 210)). The fight's moves are `S/Combat/BossController.FoundrySaint.cs` (every number in its `SaintTune`); an arm is `S/Combat/SaintArm.cs` (one scene MonoBehaviour, guide section 15); the definition is `HomeBuilder.Bosses.cs`. Its tests (`T/PlayMode/HomeBossTests.cs`) stand the six arms on a bare floor in the proving yard, so nothing here waits on the Works. Positions are world x, z (y is world height; the hall floor is -6). A bearing is degrees clockwise from north.

**How the fight uses the room.** The Saint stands at P **facing north**: its back, where the Compact's override rig is clamped, faces the door and the camera, so everything the fight asks of a player (the collars, the fallen arms, the climb, the rig) is on the camera's side of the press and never behind it. Six arms are socketed along the press's back at hero height, **raised** like a crane's booms while the override holds them. **Phase one**: arms slam down onto marked strips (two at a time), a third drags a welding line across the floor, the press drops its head on a marked circle round itself. A **collar** (the gold ring where an arm meets the press, at chest height) is shut while its arm bears weight: a blow on it is resisted, and the first one says so on screen. It **unlocks** while its arm lies planted after a slam (4 s), and every collar unlocks while the head lifts after a drop (5 s, the opening). While its arms are raised the press itself takes nothing (their weight takes the blow; said on screen once) except under the head's lift or a break. **The bar is paid in blows** (November 14, after D272): a blow on an unlocked collar comes off it as one on the open press does, and each fifth the bar passes **releases** the arm whose collar was struck most: the ring turns, the cyan goes out of it, and the arm comes down and **lies still**, a road from the floor up to the press's back. **Phase two** (three arms down): the rig is bare. A blow from the floor on the press is resisted except in the head's opening; **a blow from up on a fallen arm lands**, and twice over on the rig. The three arms still raised **sweep** across the roads (the road a sweep will cross is marked along its top half; its lower half and the floor are safe), the welding line still runs on the floor, the head still drops. At nothing the rig breaks and falls, the three raised arms come down and lie still too, the cyan goes out of the press and its own warm lamps come on: it **settles** (`Ending.ShutDown`: it is not destroyed and does not leave), and nothing barks over `OrrinSaintSpeaks`.

| Thing | What the fight needs | Why |
|---|---|---|
| Boss | Its `Spawner` at **P (0, 196)** on the floor (-6), **yaw 0** (facing north), `faceAsPlaced=true`, radius 0. `previewPoint` on the landing at **(0, 168)** with a radius that takes in P (about 30), so it stands there inert (`DormantEnemy`) from the landing | Its capsule is **2.6 m** round and **12 m** tall (the press's own height: headroom **13 m** over P). The drawn press is about 10 m across and 5 m deep; the fight stands two solid blocks in its legs (x ±2.6 to ±5.05, z 193.55 to 198.45), so nobody walks into its frame |
| The six arms | One **`SaintArm`** each, on an empty object **at its collar** (the shoulder, where the arm meets the press), **2.0 m over the floor (y -4)**, on the press's back at **z 193.2**, its forward along the arm's bearing (outward, level). From west to east: **1** at (-4.3, 193.2) bearing **225**; **2** at (-2.6, 193.2) bearing **205**; **3** at (-0.9, 193.2) bearing **188**; **4** at (0.9, 193.2) bearing **172**; **5** at (2.6, 193.2) bearing **155**; **6** at (4.3, 193.2) bearing **135**. Fields: `index` 1 to 6 as listed, `settledBy="FoundrySaint"`. Nothing else: the component draws its own arm (the `SaintArm` model, from the Saint's definition), poses it raised from the moment the scene loads, and makes its own collar target, road and cable | The arms are in the room at the first look, raised, before anything wakes. Their collars can't be struck or targeted until the Saint wakes and claims them |
| Where each arm stands (raised) | From its collar, pitched **50°** up along its bearing: its hand end about 7.4 m over the floor and 4.5 m out. Keep **9 m** clear over the floor within 8 m of each collar on its bearing (no ceiling, duct or gantry in its way) | Raised is alive; down and still is a road |
| Where each arm falls (and slams) | Along the floor on its bearing, from its collar out **7.0 m**: its foot on the floor at **(-9.11, 188.39)**, **(-5.47, 187.04)**, **(-1.85, 186.47)**, **(1.85, 186.47)**, **(5.47, 187.04)**, **(9.11, 188.39)** for arms 1 to 6. A slam's marked strip is that footprint, **7.4 m by 2.4 m** from the collar. Keep each strip and **1.5 m either side of it** level floor with nothing solid on it | A slam lands there; a released arm lies there for good; a climber thrown off one lands beside it |
| What a fallen arm is | The fight makes it: a **road 1.8 m wide** on top, **7.0 m** long, rising from the floor at its foot to **2.3 m** at its collar end (**18.7°**: under the bake's 35° and the controller's 45°), drawn as the arm lying still, solid all the way down (a box 1.2 m deep) so nobody stands under its high end. **Not in the navigation bake** (it isn't there until it falls): the partner holds the floor (Idra's `bossphase` line says so). Anyone standing where it comes down is moved clear before it turns solid | A road on the camera's side of the press: a hero on it is never behind anything, and the slope reads as a climb up the screen toward the rig |
| The rig, and reaching it | Drawn by the fight: the Compact's override rig clamped on the press's back over the collars (2.6 to 4.6 m over the floor), a cyan bar across the back and a cable from it to each arm still raised. Its target in phase two is the back over the collars, x -4.8 to 4.8, 2.8 to 4.6 m over the floor, z 192.35 to 193.35: **from the top of any fallen road a blade reaches it**; from the floor nothing does (a hero's chest is 1.8 m short). Nothing to build | D258. A released arm's cable drops slack and goes dark: the Works' own rule, a machine on a cut cable is home's again |
| The climb under the sweeps | Nothing to build. A sweep marks the **top half** of each road it will cross (from about 1 m up to the collar end), in the danger's orange, laid on the road's own surface; the lower half, the other roads and the floor are safe; a roll off the side of a road drops to the floor | What a climber must see: where he stands (on a road, on the camera's side), where the sweep will be (orange on his road), where safe is (down the road, or off it) |
| Floor | Level (no step over .3 m) within **r 14 of P on the south half and r 10 on the north** | The head's drop is a circle **7.5 m** round P; a weld runs 16 m; a roll out of either must never meet a step |
| Cover | **Two** stacks (crated separation bolts, or a jack on end): solid static boxes about **2.4 m tall, 2.6 x 1.6 m**, long side across the line to P, at **(-8.0, 184.5)** and **(8.0, 184.5)** (r 14, bearings 215 and 145), in the bake, fade on. Nothing else solid within r 14 of P | A welding line stops at solid cover. The stacks stand between the arms' feet, never on a strip |
| Door | Encounter id **`FoundrySaint`** (the main quest's step 10 points at it), trigger over the hall floor (about **46 x 8 x 38** on (0, 194)), `requiredFlag` **`home.joint3`** (the third critical joint), `checkpointSpawn` **`PressHall`**. One `Membrane` seal across the door at z 172, strands drawn instead of the cube | The shared door tests (checkpoint, retry, no way out mid-fight). Its boss sheet names zone `SeparationWorks`, door `FoundrySaint`, door flags `home.joint1`, `home.joint2`, `home.joint3`: write back any other flag the door waits on |
| Rim | South and west low (the camera's foreground). Nothing over 3 m and 7 m across between the camera and P, or between the camera and any arm's strip | Fixed three-quarter camera: the whole back fan of arms is the fight |
| Where a player who falls resumes | Nothing to build: a climber thrown off a road lands on the floor beside it and climbs again from its foot; a party that falls starts again at the door (`PressHall`, the landing's cradle) with the Saint whole | Shared retry rule (D204) |
| Leaving | No `BossExit`: it settles where it stands | The brief |
| After the fall | Once `bossdown.FoundrySaint` is set, every `SaintArm` loads **down and still** (all six: it lowers the last three as it settles) with its road solid. The press itself is yours to stand as scenery from that flag (the `FoundrySaint` model at P, yaw 0, a solid box round its frame and legs, warm-lit; a `FlagGate` on the flag), since the body goes with the encounter on a reload. `OrrinSaintSpeaks` is your `AfterTalk` on `bossdown.FoundrySaint` (the node sets `home.saintReleased`); its voice is the press's speaker, about (0, 193) at 3 m. The Saint plays no down bark | Manifest section 2: Orrin's voice on its speaker |

What the fight sets and logs, for the builder's own checks: `bossdown.FoundrySaint`; `BOSS_MOVE FoundrySaint <move>`, `SAINT_WAKES arms=<n>`, `SAINT_COLLAR_SHUT <index>` (the first blow on a shut collar), `SAINT_UNLOCKED <indices>`, `SAINT_ARM_RELEASED <index> released=<n>`, `SAINT_HEAD_UP`, `BOSS_SHIFT FoundrySaint 2`, `SAINT_RIG_BARE`, `SAINT_SWEEP <arm> roads=<indices>`, `SAINT_SETTLES`, `BOSS_DOWN FoundrySaint`. Barks: the party's `saint.arm` at each arm released (three in turn), `bossopening.FoundrySaint` as the head lifts, `bossphase.FoundrySaint` at the shift. Its music is **Glitch Boss** (its definition); the Works' Moon Caverns comes back at its fall.

### The press hall as built (the world engineer, November 14)

`HomeBuilder.Works.PressHall`; `SeparationWorksTests.ThePressHallIsBuiltToItsContract` and `TheSaintsFightBeginsOnceBothHeroesAreThroughTheDoor` read it in the built scene.

**One departure from the contract, for the combat engineer (D278):** the door's trigger is **46 x 8 x 26 on (0, 187)**, z 174 to 200, not 46 x 8 x 38 on (0, 194). Everything else in the contract is as asked. Nothing in the fight reads the trigger's size (`EncounterVolume` only begins the fight with it), so the Saint, its spawner at P and its arms are unchanged; a test or a sheet that measures the box should read these numbers. The reason is in the Door row below.

| Thing | As built |
|---|---|
| Boss | Spawner at P (0, 196), y -6, yaw 0, `faceAsPlaced`, radius 0. Preview point (0, -6, 168) on the landing, radius 30 |
| The six arms | Empties "Saint arm 1" to "6" at the contract's collars, y -4, yaw = bearing, each a `SaintArm` with `index` and `settledBy="FoundrySaint"`, under "The Saint's arms", which hangs from "The press's bed": a flush drawn iron plate 11.4 x 6.4 m at P (no collider; it is what the press stands on, so the arms are measured with it and never as six things hanging in the air) |
| Headroom, strips, floor | The hall has no ceiling; nothing hangs over any arm within 8 m of its collar. The floor is level (-6, a hand of unevenness) everywhere in the hall; nothing solid stands on any strip or 1.5 m either side but the two stacks, which stand between the feet. The two separation rails across the south half (z 177.6 and 179.4) are flush, drawn only |
| Cover | "Separation bolts 0" and "1": crated bolts at (-8, 184.5) and (8, 184.5), solid 2.6 x 2.4 x 1.6 with the long side across the line to P, static (in the bake), fade on |
| Door | Encounter `FoundrySaint`, trigger **46 x 8 x 26 on (0, -3, 187)**: z 174 (the hall's south wall) to 200. *A departure from the contract's 38 deep on (0, 194)*: that box's middle was 2 m inside the Saint's capsule, and the shared camera check (`QaWorldSecondTests.NothingWideStandsBetweenTheCameraAndAFight`) stands a hero there and found the press's own raised arms between it and the camera. Its middle is now the floor the fight is fought on. It still takes in P and the press; nobody can come in by the hall's north half. `requiredFlag` `home.joint3` (no other flag), `checkpointSpawn` `PressHall` (-3, 165). One seal, "Press hall door seal", across the door at z 172, 6.4 wide: iron bars drawn only while shut, a carving obstacle, no cube. The fight begins once both heroes are through it (`EncounterVolume.TryEnter`); a door walk for a route ends at z 177 or further in (three metres inside the trigger) |
| Rim | The hall's south and west walls are drawn under the camera's real line (`Works.Seen`, "The keel as it is drawn" above): the landing's north wall is about a metre high either side of the door, and nothing of the keel within five metres of the lens |
| After the fall | "The Foundry Saint, settled", a `FlagGate` on `bossdown.FoundrySaint`: the `FoundrySaint` model at P fitted to 12 m, yaw 0, a solid 10.1 x 12 x 4.9 box under it (cut out of the paths by a carving obstacle, D316), a warm light. "Orrin on the press's speaker", an `AfterTalk` at (0, -3, 193): `OrrinSaintSpeaks`, speaker Orrin, heard `home.saintReleased`, after 2.2 s |
| Leaving | No `BossExit`. The lift home at the north-east corner: cage (20.8, 209.8), its prompt on a holder at (19.6, 208.6), range 3.6, `Ride the lift up — The Decks` to `DecksLeaving` spawn `Lift`, locked on `bossdown.FoundrySaint` ("The lift answers the press hall's board: release the Foundry Saint first"; `book.Lock` in `SeparationWorksTests`) |

**Amended after route 59 (D316; November 14, evening; nov14-recon25 s59).** The chain's lead stood in the press's frame for the fight's last two minutes and was then wedged for good, an autosave just written. Taren's Extra carried him between the legs behind collars 2 and 3, where the round capsule left a pocket inside the drawn frame; arms 3 and 2 came down 3 s later and their high ends (a metre clear of the floor: too low to pass under, too high to step onto) shut its mouth. When the press settled, its box pushed him out under arm 2's high end, where he never moved again. Now, in combat (`BossController.FoundrySaint`, `SaintArm`), nothing in the room changed: (1) the fight stands the press's whole frame solid (`Saint frame`, 10.1 x 12 x 4.9 on P, the settled box's footprint; its own capsule passes through it), so nobody stands behind the collars; (2) an arm coming to rest steps anyone it would hold (on its plan, under its high end, or touching where it will lie, a climber on the next road's top too) to the nearest clear floor on the camera's side of the collars, and the settling press does the same for anyone inside its frame (`SaintArm.StandClear`, log `SAINT_STOOD_CLEAR`); (3) a fallen arm's plan is cut out of the paths (a carving obstacle, `Road footprint (off the paths)`), so the partner and anything else that walks by the bake goes round its foot and high end; so is the press's footprint, by the fight's frame while its body stands and, after a reload, by the room's "Settled press" box (a carving obstacle on it, `HomeBuilder.Works.PressHall`): the bake has floor under the press, and the partner's way to the lift ran through it. The contract's "not in the navigation bake" stands: neither is baked, each carves while it stands. `FoundrySaintFallTests` stands the heroes where the fight leaves them (behind the collars, against the back under each raised arm, up on and at the foot of the fallen arms, in the wedge between two, against the seal), fells it, and walks each to the lift or out through the door.

**Owed by `SaintArm` (combat, not the room):** its collar's knot is Unity's sphere primitive under an unpainted material, which `QaPlanWorldTests.NoPlaceholderIsDrawnInAnyZone("SeparationWorks")` rejects six times ("Knot is a bare Sphere"). It wants a mesh of its own (a `ZoneKit.Shape` disc or lump, or the arm model's own collar), and the zone's case of that test stays red until it has one.

## Works fights contract

*The first combat engineer's section: what the Works' ordinary fights need from the place, in metres, written November 12 against the plan and the builder as they stood (`HomeBuilder.Works`: the rigs at their posts, the crews and the arms in their rooms). The world engineer builds to it and writes the coordinates as built underneath. The enemies are `HomeBuilder.Enemies.cs`; tests in `T/PlayMode/HomeCombatTests.cs`, on a bare floor.*

**Agreed: the cable is the zone's.** The zone draws every cable from a rig's clamp to its machine (`RigPost`, `DrivenPress.cable`), lit and pulsing while the rig lives and slack and dark once `clear.<the rig's encounter>` is set; the rig's body stands at the clamp, so the cable visibly ends at it. Combat draws nothing between them. Killing the rig is the whole of freeing the machine: its encounter of one clears, and the machines read the flag. What a press does to a Compact crew under it (`DrivenPress.enemyShare`) is the zone's too: "lead a crew under a hammer" is the gallery's answer as much as any enemy's.

| Who | How it fights (as defined) | What it needs from the place |
|---|---|---|
| **Override Rig** (`Anchored`, all round; integrity 900; 35 XP) | Never moves. While a hero is within **2.8 m** it discharges: a marked ring 2.8 m round it for 1 s, then a hit of 126 (Pulse), every 3 s. A blade lunge mid-ring takes the discharge from it. Answer: step out of the ring, strike it between discharges (Pulse is its weakness, so a shot does it from outside the ring). Killed, it bursts off its clamp | Its spawner at the clamp's face, as now (throat: 0.8 m west and 1.1 south of the clamp, yaw 220; control room: on the board, yaw 180; gallery: 1.1 m west of the post, yaw 270), radius 0, `faceAsPlaced`. **Its ring overlaps no press bay's marking** (as placed: the throat rig's ring is 5.6 m from the nearest bay, the gallery's 4.0); walkable floor on its open side out to 4 m, so a hero can stand at its flank. Solid behind it (the cabinet) is fine: its discharge stops at cover like any hit |
| **Compact Warden** (`Guardian` that **gives ground**: integrity 1,700, break 300; 80 XP) | A shield to the front (140°; a blow from inside it lands at a fifth, read as resisted) and a stun-baton: a marked circle 2.1 m round, 1.6 m before it (.8 s, 128, Pulse); from 3 to 7 m a shield drive down a marked line 7 by 1.8 (1 s, 131, Kinetic), after which its shield is down 1.6 s. Won't be led more than 9 m from where it was set. **Its guard broken** (its Break meter), it gives ground: backs off 3 m in half a second and stands open for the break's 3 s. **At nought it stands aside**: it lowers its baton, turns from the party and walks out of the fight, 9 m, and is gone. No body falls, nothing explodes, no "one less": the party's `warden` bark instead. Nobody of Meret's is killed on screen | Each crew's spawner on level floor, 2.1 to 2.6 m ring (as placed), with **4 m clear round each Warden** and its 9 m post walkable. **Behind each crew (away from the way the party comes in) 9 m of floor to walk off along**; or, better, a `BossExit` with `id` **`CompactWarden`** at a hatch or a door in that room (`S/Combat/BossExit.cs`): every Warden there leaves by it, the way they came down. In the gallery the crew stands between the press rows (the 10.5 m between z 117 and 127.5), so a Warden can be led under a hunting press |
| **Weld Arm** (a `Rider` on its own rail that never moves: integrity 1,400; 80 XP) | Lays a weld down **a marked strip 11 m long and 2.4 wide** along the way it was set facing, from its foot: a hero on the strip within 11 m sees it lit for 1 s, then the weld runs down it, a hit of 130 (Plasma), and the strip is left **burning for 3 s** (38 a second; drawn as a danger). Every 3.4 s. A hero off the strip is never touched. Answer: step off the strip, don't step back onto the burning seam, and strike the arm from beside it (Kinetic). At nought its override is cut and it stands down where it is | Its spawner at its foot, `faceAsPlaced`, radius 0, facing along **a strip of level floor 11 m by 2.4, plus half a metre each side, that crosses no press bay** and has **3 m of open floor on at least one side**. As placed, the gallery's arm (-11, 140.5) at yaw 160 lays its strip across the north row's west bay (x -7.5 to -2.5): **turn it to yaw 200**, so its strip ends at (-14.8, 130.2), west of both rows. The skiff's (29.6, 138.6) at yaw 225 ends at (21.8, 130.8), inside the skiff: as it is. Its own rail also runs 5.5 m behind it, into the wall it is mounted on, where it can reach nobody |

**The fights as the plan has them** (the doc's encounter plan, kept): `Works_Throat` (the throat's rig alone), `Works_ControlRig` and `Works_Control` (a rig and two Wardens), `Works_GalleryRig` and `Works_Gallery` (a rig; three Wardens and a Weld Arm), and the optional `Works_Module` (two Wardens) and `Works_Skiff` (a Weld Arm). Main path 585 XP, the optional rooms 240 more (the ledger is in `HomeBuilder.Enemies.cs`). Every hit above is 9 to 13% of a hero's bar at Sync 25 to 27; the gallery's three Wardens with the arm are its hardest room, about 30 s for a Sync 25 pair with the presses still hunting.

### The fights as built (the world engineer, November 14)

`HomeBuilder.Works.Build`. Every spawner `faceAsPlaced`; a body of one has radius 0.

| Fight | Trigger (x, z, size) | Who, where | Waits on |
|---|---|---|---|
| `Works_Throat` | (1.5, 57), 24 x 9 | 1 OverrideRig at (9.0, 56.3), yaw 220, at its post (9.8, 57.4); shown inert from (0, 47), r 18 | nothing |
| `Works_ControlRig` | (24.6, 84), 15 x 19 | 1 OverrideRig at (25, 90.2), yaw 180, at its post (25, 91.6) before the board | nothing |
| `Works_Control` | (24.6, 84), 15 x 19 | 2 CompactWarden at (22.6, 84), yaw 180, radius 2.4 | nothing |
| `Works_GalleryRig` | (0, 123), 31 x 40 | 1 OverrideRig at (13.1, 121.2), yaw 270, at its post (14.2, 121.2) | nothing |
| `Works_Gallery` | (0, 123), 31 x 40 | 3 CompactWarden at (0, 122.5), yaw 180, radius 2.6, between the press rows; 1 WeldArm at (-11, 140.5), **yaw 200** as asked | nothing |
| `Works_Module` | (20.2, 20), 15 x 16.5 | 2 CompactWarden at (19, 22), yaw 270, radius 2.1 | `home.joint4` |
| `Works_Skiff` | (25, 134), 14 x 16 | 1 WeldArm at (29.6, 138.6), yaw 225 | `home.joint6` |

No `BossExit` for the Wardens: `BossExit.For` takes the first exit of an id in the scene, so a hatch in each room would send every crew to one room's hatch. Each crew walks off on the floor behind it, away from the way the party comes in.

## For the replay route (the world engineer, November 14)

Start: `SeparationWorks` spawn `Arrival` (0, -5), down the Decks' stair (`home.meetingHeld`). Floors: tug 0, hauler -1.5, control room 0, liner -3, landing and hall -6.

1. Walk north up the tug's middle; the crossing at z 29.6 (23 m wide) plays `DorrowWorks` (advance it): `home.worksRead`.
2. Joint 1: stand at its hand (4.4, 34.0), press: `home.joint1`. Wait for it to give (`JOINT_GIVEN home.joint1`, about 4 s): until then its held seam blocks the collar, and the walk north through it, (0, 33) to (0, 42), is cut.
3. The throat: `Works_Throat` is the rig alone at (9.0, 56.3) (trigger on (1.5, 57)). Lock onto it from the throat's south floor (z 55 to 59), never inside a bay (z 62.4 to 67.4) until `clear.Works_Throat`; then walk through the bays to z 75.
4. The control room: up the stair (x 10.2 to 18.2 on z 82) to (24.6, 84). `Works_ControlRig` (the rig at (25, 90.2)) and `Works_Control` (two Wardens at (22.6, 84)) wake together: lock the rig first; both cleared (`clear.Works_ControlRig`, `clear.Works_Control`) lifts joint 2's press.
5. Joint 2: only once `clear.Works_ControlRig` is set, its press lifts (about 1.5 s) and the floor under it is walkable again; then stand at (4.4, 95.0) and press: `home.joint2`. The collar's held seam goes once the joint has given (`JOINT_GIVEN home.joint2`, about 4 s): only then walk north through it, (0, 96) to (0, 104).
6. The gallery: `Works_GalleryRig` (rig at (13.1, 121.2)) and `Works_Gallery` (three Wardens at (0, 122.5), a Weld Arm at (-11, 140.5)) wake as the party comes in past z 103. Lock the rig first from (11, 121) (its presses stop hunting once it is cut), then the crew between the press rows (z 118 to 127); keep off the Weld Arm's strip (from (-11, 140.5) toward (-14.8, 130.2)).
7. Joint 3: stand at (4.4, 144.0), press: `home.joint3`.
8. Down the stair to the landing: the stair runs from (0, -3, 152.5) at the liner's level down to (0, -6, 162); walk (0, 150) to (0, 158) to (0, 165). The cradle at (6.6, 165) is a `RepairBay` ("Press hall cradle — repair and save", no talk): stand at (5.0, 165) and press; it saves at `PressHall` (-3, 165).
9. The Saint: through the door at (0, 172) to (0, 180) (a walk with 1.5 m of tolerance has to end three metres inside the trigger, which starts at z 174: so z 178.5 at the least), and the partner through too; lock "the Foundry Saint" (encounter `FoundrySaint`). It ends at `bossdown.FoundrySaint`; `OrrinSaintSpeaks` follows 2.2 s later (advance it): `home.saintReleased`.
10. Leave by the lift: stand at (19.6, 208.6) or within 3 m of it (its reach is 3.6 on the plan), press "Ride the lift up — The Decks": `DecksLeaving` spawn `Lift`.

Optional on the way: joint 4 (hand (8.8, 16.4), `home.joint4`, then `Works_Module`: two Wardens at (19, 22), and the locker at (25.6, 15)); joint 5 (hand (8.8, 46.4), `home.joint5`, then the wedding table, "Look at the table" from (20.4, 49.4) with `SecondInventory` at step 0: `mira.lotTable`, and the trunk at (25.8, 44.4)); joint 6 (hand (13.8, 130.4), `home.joint6`, then `Works_Skiff`: a Weld Arm at (29.6, 138.6), and the lockbox at (29.4, 129.8)). A cache is opened by standing within 2.6 m and pressing.
