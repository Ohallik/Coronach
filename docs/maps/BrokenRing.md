# The Broken Ring

*This file was started by the second combat engineer with the Cutter's section alone, before the zone's own brief (November 12). The Broken Ring's world engineer writes the rest of the brief above it and owns the file; the contract below stays under its own heading.*

2026-11-12. Builder: `HomeBuilder.Ring.cs` on `ZoneKit` (chapter 9's space-zone engineer). Chapter brief: [ch09-the-house-that-can-leave.md](../chapters/ch09-the-house-that-can-leave.md), "BrokenRing" and beats 9 to 11. Script: [Home.yarn](../writing/Home.yarn), [manifest](../writing/ch09-manifest.md); who places what: [ch09-wiring.md](../chapters/ch09-wiring.md). Mechanism: `S/World/SectionFlight.cs` (a district flying out), `S/World/RingFormation.cs` (the formation the Cutter is fought in), `S/World/WreckTrack.cs` (the first engineer's marked track, reused for the debris channel). Tests: `T/PlayMode/BrokenRingTests.cs`. **MAP_EYE_TEST: blockout written before construction (this page, the sections down to "Coordinates for routes"); editor stills and what they found are under Evidence.**

## What the place is for

Story beats 9 to 11. The launch on the Decks cuts straight into this zone (`SceneFlow.LoadZone("BrokenRing","Arrival")` from `NeveFirstOut`): Cinder Halo is coming apart on purpose. Its districts lift off the ring one after another on engines the Crawl's engineers rigged, each a place the player has walked in for two chapters: **the market** (Mira's pitch nine and the promenade), **the repair quarter** (Hal's shop front), **the residential crescent** (the cabins) and **the tug dock** (lock nine). The party flies out beside them, through Compact interceptors, to where Bryn holds the sections in formation, and fights the **Debris Cutter** among them. There is no escort bar and no civilian-loss score: the districts are landmarks and cover, never a number. The tug *Welcome Back* tows the disabled Cutter off (`BrynTowsCutter`), the districts break formation and fly down to the convoy, and the way home to Many Hands opens. The chapter's last talk (`OrrinChalk`, at Many Hands' chart table) is staged there by the first engineer's `Approach` (wiring section 2).

**The rule, as a player would say it: "Keep beside a district. The lit water next to it is calm, and its bulk is cover. The danger runs down the marked channel between the districts: cross it where its lamps are cold."**

- **Beside a district is calm** (its *lee*). Each district's lit windows face the camera, and the light from them lies on the plane along its inner side as a warm pool: that strip is where nothing travels. Its bulk is solid at the plane (2 m under it to 4 m over), so a strafing run, a tractor line or the Cutter's beam stops at it (`CombatCover`, D207).
- **Between the districts is the channel.** The ring's debris streams outward down it: torn deck plate, ring fascia, crates, a cabin's door. Its banks are two rows of pale lamps (a boundary, never a danger's colour), and down its middle a line of lamps **stands amber** over the stretch each piece will cover in the next 2.5 seconds and **runs cold** everywhere else (the same language as Nacre's tracks, earlier in the chapter). With a ship near, the amber stretch is marked on the plane as a danger (`Telegraph.Threat`); a piece that touches a ship inside its marking takes 10% of its Integrity (the place's damage) and throws it clear.
- **The market waits for its escort** (November 14): it sets off once the lead has come level with it within 30 m, or is 40 m past it anywhere (`SectionFlight.escortReach`, `escortAhead`; `SECTION_SETS_OFF`). Flown off at load, it left the arrival before the party had moved.
- **The districts move**: slowly (never over 4 m/s; 3.8 cruising, 2.2 once they are 30 m ahead of the lead ship, which is what "keeping with the escort" looks like), outward, each on a column of its own; they **hold for anything in their way** (a hero ship, an enemy, another district closer than 14 m ahead) instead of pushing into it, so they can never trap a ship. Nothing of a district stands more than 3 m over the plane but a mast, so it never hides a ship or a marking beside it from the fixed camera.
- **Taught by the first district before the first fight.** The party arrives in the market's lee with the channel 15 m to starboard; within the first seconds a piece of the ring comes down the channel past it, its amber running ahead of it. Bryn's radio (`hail.ring`) asks the sections to hold their spacing. The first interceptor pair is 120 m on.

Users: the party; the four districts and everyone aboard them (a few figures on the open decks); Bryn's tug; Compact interceptors (two pairs on the way) and the Debris Cutter.

## Rules of construction

- Everything flies at the flight plane (y 1). Backdrop is drawn below it and kept clear of what must be read by where it shows on screen (`ZoneKit.ClearOnScreen`): the arrival, the channel, both pairs' centres, the checkpoint, the Cutter's lanes and C, the way home.
- **Walls are short static boxes laid along the outline** (the approach's sides and its south end, the fight's circle, the exit lane), each a little longer than its piece so neighbours overlap. **`ZoneKit.FlightCorridor` is not used** (it runs each leg's walls 72 m past its ends: chapter 6). Every edge has a lamp-buoy line on it and drifting ring wreckage beyond it, so the stop is something seen. `BrokenRingTests` flies a hull's sphere through all the open water and finds no wall in it, and finds a wall within reach out from every edge.
- **Every district is built from the station's kit**, the generated `DeckFloor`, `DeckWall` and `DeckDoorway` pieces at near-native proportions, as `StationRedesign` built the Halo in chapter 1, with the roof dressing chapter 1 gave it (solar arrays, tanks, pipe runs, a comms mast, deck lamps) and the Decks' own things (`MarketAwning`, `DressMarketStall`, `LatticeAnvil`, `LandingPad`, `DockingPod*`). Each has a clean cut edge at its stern, chalked where it parted, and a Crawl-rigged engine bolted under it burning warm white (never a danger's orange).
- **A district's footprint is 12 m across and 17 m long**, heading north always (its lit face toward the camera), solid from 2 m under the plane to 4 m over it, with a `RingDistrict` on its root (contract). Nothing of it but a thin mast stands more than 3 m over the plane.
- **Every gap takes Idra's hull** (2.17 m): the lee is 18 m wide, the channel 12, the districts in the fight never closer than 14 m to each other.

## Plan

North up; x east, z north. **C = (0, 280)**, the Cutter's centre.

```text
 z 382  ------------------------ exit lane end -------------------------
 z 368                      WARP HOME (0) (home.cutterDisabled)
 z 338  ============== the fight's circle, r 60, opens north to x ±14 ==============
 z 296   MARKET station (-42)          lane 0 (0, 310)         REPAIR station (42)
 z 280   tug (-64) |                     C (0, 280)                      |
 z 264   CRESCENT station (-42)                                 TUG DOCK station (42)
 z 240                          spawn Ring (0, 240)
 z 241  - - - - - - - - - - - - - the fight's circle meets the approach - - - - - - - - - -
 z 188                                                 Ring_Pair2 (16)
 z 172                                          cover wreck (20)
 z 134         cover wreck (-20)
 z 118         Ring_Pair1 (-16)
        | west wall -46 | west column -30 | west lee | CHANNEL 0 | east lee | east column 30 | east wall 46 |
 z 10          MARKET at load (-30, 10)
 z 2         ARRIVAL (-15, 2)
 z -18  ---------------------------- south wall ------------------------------
        deep below and behind: what is left of the ring (r 62, 74 m down), its four sockets empty
```

| Stretch | z | What | Answer |
|---|---|---|---|
| The parting | -18 to 70 | Arrival in the market's lee; the channel to starboard, a piece of the ring coming down it within seconds; the ring far below; the repair quarter rising out of it to port of the channel as the lead moves (Orrin: "Repair quarter, lifting.") and clearing the ring at the plane (Hal's `hail.repair`) | Keep beside the market; watch the channel's lamps |
| The first pair | 70 to 150 | The crescent lifts behind the party as the lead passes z 70 (Orrin's third); `Ring_Pair1` in the west lee at z 118, a wreck of the ring's deck as cover 16 m from it | Break the tractor line behind the wreck or a district; keep out of the channel when pinned |
| The second pair | 150 to 230 | The tug dock lifts as the lead passes z 130 (Orrin's last: "That's the last of us."); `Ring_Pair2` in the east lee at z 188, a second wreck as cover | As above; the channel sinks away at z 200 |
| The formation | 230 to 340 | The columns bend out to x ±42 and each district takes its station either side of the fight; once two are on station (`ring.formed`) the Cutter's door opens; the spawn `Ring` is the checkpoint | The Cutter, among the districts |
| The way home | 340 to 382 | The exit lane north; the warp opens with `home.cutterDisabled` | |

## The districts (built exactly so; the tests read these)

Each flies a path of its own: at load (or once the lead's z passes its lift line) it climbs out of its socket in the ring 74 m below and comes up to the plane on its column, flies north on it, bends out to its station and holds there, swaying a metre (0.2 m/s). Its station is its place in the formation. On `home.cutterDisabled` it leaves its station north and down, to the convoy; a save with that flag has it there.

| District | Look (what a player names it by) | Column | Lifts | Station | Lift line said |
|---|---|---|---|---|---|
| The market | Mira's pitch nine and the promenade: a row of stalls under striped awnings along the open deck, the **yellow line** of the promenade running down it to the clean chalked edge where it parted, the market hull's lit shop windows behind | west, x -30 | already up at load, at (-30, 10) | (-42, 296), north-west | (Orrin's first, on the Decks, before the cut) |
| The repair quarter | **Hal's shop front**: the workshop's wide lit doorway and its sign plate, the receiving bay's crates, the anvil, a gantry crane over the bay | east, x 30 | as the lead passes z 10 | (42, 296), north-east | Orrin's second; Hal's `hail.repair` as it reaches the plane |
| The residential crescent | **Rows of lit cabin windows in family colours**, two stepped terraces in an arc, laundry lines strung across its court | west, x -30 | as the lead passes z 70 | (-42, 264), south-west | Orrin's third |
| The tug dock | **Cradles**: two tug cradles on the open deck, one with a tug in it, landing pads, the dock office's lit windows and **lock nine's number** painted large | east, x 30 | as the lead passes z 130 | (42, 264), south-east | Orrin's fourth |

The **lee** of each is its inner side (toward the channel in the approach, toward C at its station), 12 m wide along its length, drawn as the warm pool of its windows' light on the plane. During the Cutter's fight, the first time the lead passes within 12 m of a district, the lead says its `section` line (by its place in each hero's pool: Taren's and Idra's are the repair quarter, the crescent, the tug dock; Sela's the market, the crescent, the tug dock).

**What each district shows the flight camera (November 14, a look engineer, on the lead's note to `ch9-wr-02`):** the market's hall roofed in striped canvas with strings of lights over its stalls and a striped awning on its outer side; the repair quarter's sooty roof with four lit skylights and a yellow edge; the crescent's cabins roofed in their families' colours round a garden court (grass, planters, a tree); the tug dock's roof a blue-grey pad ringed in chalk, lit at its edges; every open deck lit along both edges with a lamp on each corner; and under the fight, pieces of homes adrift with a window still lit. All inside the footprint and under 3.25 m; the cover meshes are unchanged. Stills `Builds/quality/look/ring-streets-02` (the stills pose).

## The channel (built exactly so)

| Track | Line | Speed | Pieces | A piece's reach, its blow | Lamps |
|---|---|---|---|---|---|
| The channel | x 0, from z -26 to z 216, open: each piece comes up out of the field (the ring's direction) over its first 16 m and goes down over its last | 9 m/s | 5 ring fragments, about 48 m apart | 3 m; 10% of a hull's Integrity, thrown 6 m | every 3 m down its middle (`WreckTrack`); banks of pale lamps at x ±6.5 every 3 m |

## What happens along it

| Where | What | Flags, ids |
|---|---|---|
| (-15, 1, 2) | Arrival (spawn `Arrival`, facing north): from the launch on the Decks; arriving saves | |
| the arrival | Bryn on the radio, `hail.ring` (two in turn) | `ring.brynHailed` |
| lift lines z 10, 70, 130 | the repair quarter, the crescent, the tug dock leave the ring; Orrin's `hail.section` lines 2, 3, 4 (by index) | `SECTION_LIFTS <district>` |
| the repair quarter at the plane | Hal's `hail.repair` | `SECTION_AT_PLANE repair` |
| (-16, 1, 118) | `Ring_Pair1`, two Interceptors; the lead's `interceptor` on its wake (`FirstSight`) | `clear.Ring_Pair1`, `ring.interceptorSeen` |
| (16, 1, 188) | `Ring_Pair2`, two Interceptors | `clear.Ring_Pair2` |
| two districts on station within 45 m of C | `RingFormation` sets the door's flag | `ring.formed` |
| C (0, 1, 280) | `DebrisCutter`, the Cutter's door: trigger 100 x 8 x 100, needs `ring.formed`, checkpoint spawn `Ring` (0, 1, 240); the Cutter on show at lane 0 before it wakes | `bossdown.DebrisCutter` |
| the tug (-64, 1, 280) | `TowedOff`: comes in and tows the Cutter off west | `TOW_TAKES DebrisCutter` |
| anywhere, after the fall | `BrynTowsCutter` (`AfterTalk`, speaker Bryn) | `home.cutterDisabled` |
| the districts | leave their stations for the convoy | `SECTION_LEAVES <district>` |
| (0, 1, 368) | `Warp — The Hull of Many Hands`, locked on `home.cutterDisabled` | to `ManyHandsHull`, spawn `Ring` (the first engineer's `Approach` adds it) |

## Coordinates for routes

| Place | World (x, y, z) | Id / flag |
|---|---|---|
| Arrival | (-15, 1, 2) | spawn `Arrival` |
| The west lee, flown north | (-15, 1, 2) -> (-15, 1, 100) -> (-10, 1, 150) | |
| Crossing the channel | from (-12, 1, z) to (12, 1, z) when its lamps at z are cold | the channel x -6 to 6 |
| The east lee, flown north | (15, 1, 150) -> (15, 1, 230) | |
| `Ring_Pair1` | (-16, 1, 118), trigger 40 x 8 x 40 | `clear.Ring_Pair1` |
| Its cover wreck | (-20, 1, 134), a 7 x 5 m deck plate turned 20 degrees, solid as it is drawn at the plane | |
| `Ring_Pair2` | (16, 1, 188), trigger 40 x 8 x 40 | `clear.Ring_Pair2` |
| Its cover wreck | (20, 1, 172), the same turned -25 degrees, solid as it is drawn at the plane | |
| Checkpoint | (0, 1, 240), facing north | spawn `Ring` |
| C | (0, 1, 280) | encounter `DebrisCutter`, needs `ring.formed` |
| The Cutter's lanes | r 30 round C at bearings 0, 45, ... 315: (0, 310), (21.2, 301.2), (30, 280), (21.2, 258.8), (0, 250), (-21.2, 258.8), (-30, 280), (-21.2, 301.2) | `CutterLane` 0 to 7 |
| Stations | market (-42, 1, 296), repair (42, 1, 296), crescent (-42, 1, 264), tug dock (42, 1, 264) | `RingDistrict` `market`, `repair`, `crescent`, `dock` |
| Bryn's tug | (-64, 1, 280), facing east; tows to (-130, 1, 280) | `TowedOff` |
| Way home | (0, 1, 368) | `Warp — The Hull of Many Hands`, needs `home.cutterDisabled` |

## What is in frame

The flight frame is about 45 m wide and 25 m deep at the plane.

- **In the approach**, a district fills a quarter of the frame's width on one side: its stern's chalked cut edge and engine glow nearest the camera, then its open deck (the market's awnings, the repair quarter's bay, the crescent's court, the tug dock's cradles), then its lit windows; its warm pool on the plane beside it. On the other side, the channel: two pale banks, the lamps down its middle cold with an amber run ahead of each piece, a piece of the ring tumbling down it.
- **Below**, in the first frames: what is left of the ring, 74 m down, a band of deck plate and fascia with its four sockets empty and chalked, and wreckage between. Further on: ring fragments drifting under the lane.
- **In the fight**, two or three districts at a time at the frame's edges, each with its name and its lit face; the Cutter's lanes open between them; the tug off the west side.
- **Beyond, at the top of the frames from the second pair on**: Many Hands, far down and ahead, turning to take the districts in, its decks lit; past it the convoy's lights waiting; Vorun's limb and Sorrel, small and grey.

## Cutter arena contract

*The second combat engineer's section: what the Debris Cutter needs from the Broken Ring (the open space it is fought in, the districts as cover and landmarks, the lanes it weaves, where Bryn's tug waits, the door). The world engineer builds to it and writes the coordinates as built underneath.*

Written November 12 by the second combat engineer, before the zone has a layout: positions are given from the fight's centre **C**, which the zone's engineer places (+z north, at the flight plane, y 1). The fight's moves are `S/Combat/BossController.DebrisCutter.cs` (every number in its `CutterTune`); a lane point is `S/Combat/CutterLane.cs`, a district's cover `S/Combat/RingDistrict.cs` and the tug `S/Combat/TowedOff.cs` (one scene MonoBehaviour each, guide section 15); the definition is `HomeBuilder.Bosses.cs`. Its tests (`T/PlayMode/HomeBossTests.cs`) stand lanes, two districts and the tug in the flight yard, so nothing here waits on the Ring.

**How the fight uses the Ring.** The Cutter is a Compact interceptor with **three weapon pods** on its hull, each a target of its own: the **cutting beam** under its nose (a marked lane, stopped by anything solid), the **mine-layer** at its stern (marked circles laid across the lane a ship is flying, each a fuse and a burst) and the **tractor** on its starboard flank (a marked line; a ship caught is reeled toward it, off its line, until it rolls, breaks the line behind cover, or the line runs out). A pod is shut while it is cold (a cyan knot; a blow passes to the hull) and **open while it vents** (amber), just after it has fired, and while it vents the Cutter keeps it turned on the ship it fired at: that is the moment to strike it, and the first vent says so on screen. **The player chooses which pod to go for first**, and each pod gone changes how it hunts: with no beam it closes and rams; with no mines it stands off and cuts twice; with no tractor it boxes a ship in with mines. It weaves among the districts between moves, and after a run it **turns wide round a district**, open (the party's line: "It's turning wide round the market. Now."). **Its bar is paid in blows** (November 14, after D272), on the hull or on a venting pod alike, and each third the bar passes disables the pod struck most: the pods choose, they never take bar with them. With its bar gone it is **dead in space and whole** (`Ending.ShutDown`): nothing explodes, its crew stay aboard, and Bryn's tug comes in and tows it off. There is **no escort bar and no civilian-loss score**: the districts are never at risk in numbers. They are landmarks a player has walked in, and cover.

| Thing | What the fight needs | Why |
|---|---|---|
| The fight space | Open flying at the plane **50 m round C**, bounded at **r 60** (the zone's lamp-buoy line or `FlightLimits`). Nothing solid in it but the districts below | The Cutter weaves at 30 m; a beam runs 34 m; a ship rolling out of a lane must have room |
| The districts | The four named ones flying in formation through the Ring: **the market**, **the repair quarter**, **the residential crescent**, **the tug dock** (each a place the party has walked in on the Decks: its own lit windows, family colours and one thing a player would know it by from above, Mira's awning, Hal's crane, the crescent's laundry lines, lock nine's numbers). At any moment of the fight **at least two are within 45 m of C**; none moves faster than **4 m/s**; two are never closer than **14 m** (the gap is a channel a ship flies) | Landmarks and cover, never a score |
| A district's cover | On each district, at the plane, a **solid collider** matching its drawn footprint there, from **2 m under the plane to 4 m over it** (static if the district stands still, kinematic on its own moving root if it flies), no `Health` above it. On the same object a **`RingDistrict`**: `id` `market`, `repair`, `crescent` or `dock`, and `displayName` "the market", "the repair quarter", "the residential crescent", "the tug dock" | The beam and the tractor's line stop at it (`CombatCover`); mines are never laid inside one. The fight names the district the beam hit (Mira's `hail.market` when it is hers) and the one it turned wide round |
| The lanes it weaves | **Eight `CutterLane`s**, each on an empty at the plane, `index` **0 to 7** clockwise, on a ring **r 30 round C** at bearings 0, 45, ... 315, each with **8 m of open space** round it (move a point along the ring, not inward, if a district's path crosses it). It flies between neighbours (never across C) and holds off whichever are at its distance from the party | Its pursuit has somewhere to be; with none (the yard) it circles the lead |
| Encounter | Id **`DebrisCutter`** (the main quest's step 13 points at it), trigger covering the fight space at the plane (about **100 x 8 x 100** on C), no `requiredFlag` (the zone is reached only after `home.launched`), `checkpointSpawn` **`Ring`**; its `Spawner` at **lane 0** (C + (0, 30)), facing C, `faceAsPlaced`, radius 0 | The shared door tests. Its boss sheet names zone `BrokenRing`, door `DebrisCutter`, `flight`, door flag `home.launched`: write back any other flag the door waits on |
| Arrival and checkpoint | Spawn **`Ring`** at the plane, about **40 m south of C** on the party's way in, facing C, clear of every district's path | A retry flies back in from here, the Cutter whole |
| Bryn's tug | Her tug's hull (the `Welcome Back`; a hauler hull as its stand-in) drawn at the plane **55 m from C on the south-west** (bearing 225), facing C, with a **`TowedOff`** on its root: `boss="DebrisCutter"`, `away` an empty **130 m** out on the same side. It waits there through the fight, in sight from the fight's south edge | Once `bossdown.DebrisCutter` is set it comes in (12 m/s), puts a line on the disabled Cutter and tows it off to `away`, and both are gone; a save with the flag loads with both gone. The tug is the brief's ending |
| Rim | Everything drawn below the plane (the districts' undersides, the debris) kept by `ClearOnScreen` off the screen-shadow of the lanes and of C, as the Wick Reach does | The flight camera looks down at 48°: what is under the plane shows south of where it is |
| After the fight | `BrynTowsCutter` is your `AfterTalk` on `bossdown.DebrisCutter` (its last line sets `home.cutterDisabled`). The Cutter plays no down bark, so nothing is said over it; it stays where it went dead until the tug takes it | Manifest section 2 |

What the fight sets and logs, for the builder's own checks: `bossdown.DebrisCutter`; `BOSS_MOVE DebrisCutter <move>`, `CUTTER_WAKES pods=3 lanes=<n>`, `CUTTER_FIRST_VENT <pod>` (the first pod to open, said on screen), `CUTTER_VENTS <pod>`, `CUTTER_POD_DOWN <pod> left=<n>`, `CUTTER_HUNTS <mode>` (its pursuit as the first pod goes: `close`, `standoff` or `boxing`), `CUTTER_TURNS_WIDE <district>`, `CUTTER_BEAM_STOPPED <district>`, `CUTTER_TRACTOR_CAUGHT <hero>`, `CUTTER_TRACTOR_BROKEN <why>`, `BOSS_SHIFT DebrisCutter 2`, `CUTTER_DEAD_IN_SPACE`, `TOW_TAKES DebrisCutter`, `BOSS_DOWN DebrisCutter`. Barks the fight plays: the party's `pod` at each pod disabled (three in turn), `bossopening.DebrisCutter` as it turns wide, `bossphase.DebrisCutter` at its shift (the first pod gone), Mira's `hail.market` the first time its beam is stopped by the market, Neve's `hail.lane` as the first and second pods go, Idra's `hail.ring` (three in turn) when she is out but not leading, at the start and as each of the first two pods goes. **Yours**: Orrin's `hail.section`, Bryn's `hail.ring` at the launch, the party's `section` as a district passes, Hal's `hail.repair` as the repair quarter clears the ring. Its music is **Alien Boss Battle** (its definition); the Ring's Starfight comes back at its fall.

## Flight fights contract

*The first combat engineer's section: what the Ring's ordinary fights need, in metres, written November 12 before the zone has a layout, so places are given from the Cutter's centre **C** and from the party's way in. The world engineer places them and writes the coordinates as built underneath. The enemy is `HomeBuilder.Enemies.cs`; a pair's two roles are `S/Combat/EnemyStrafer.Pair.cs`; tests in `T/PlayMode/HomeCombatTests.cs`, in the flight yard.*

**The Interceptor** (a `Strafer` **in a pair**: integrity 1,050 each; 60 XP each). Two woken together within 24 m are a pair: one **pins** and one **strikes**. The pinner holds 14 m off the lead and marks a tractor line to him (15 m long, 1.2 wide, .9 s); a ship caught in it flies at .3 of its speed for up to 3.5 s, a cyan line drawn between them, and the catch itself does no damage. The striker circles at its 14 m run-in and runs a marked lane through the ship (22 m long, 2.6 wide, .9 s, at 24 m/s), a hit of 130 (Kinetic), then breaks away. While its partner pins someone the striker runs its lane through **him**. The line breaks when the pinned ship rolls, when anything hits the pinner, when solid cover (a district) comes between them, or at 22.5 m. **Two answers, both drawn: break the line (roll, or put a district between you), or shoot the one holding it**; then cross out of the striker's lane. Killed, either one's partner scatters for a beat (every strafer does) and then fights alone as a striker. Beam is their weakness, Kinetic their resistance.

| Thing | What the pairs need |
|---|---|
| Encounters | **`Ring_Pair1`** and **`Ring_Pair2`** on the party's way from the zone's arrival to the Cutter (each one encounter, one `Spawner`, **count 2, radius 4**, so the two are 8 m apart and wake as a pair), at the flight plane (y 1); and an optional **`Ring_Pair3`** off that way. Trigger about **40 by 8 by 40** on each |
| Where | Each pair's centre **outside the Cutter's bound (r 60 round C)**, at least **75 m from C** and **70 m from any other pair**, so no pair is ever awake in the Cutter's fight and two pairs never join. On the way in: the first about 150 m from C, the second about 95 m (so `Ring_Pair2` is the last thing before the checkpoint spawn `Ring`, 40 m south of C) |
| Room | Open flying **30 m round each pair's centre** at the plane: the pinner holds 14 m off, the striker's run is 22 m and its break-away carries it 10 m more, and a pinned ship (slowed) must still be able to roll clear of a lane |
| Cover | A district, or a big piece of debris with a solid collider at the plane, **within 20 m of each pair's centre**, at least one: it is the second way to break a tractor line, and a lane stops at it. Nothing else solid within 8 m of the centre |
| Debris channels | The zone's marked channels (the place's damage) may cross a pair's room, but never its centre's 12 m, so a pinned ship is never held on a channel's marking |
| Barks | `interceptor` on the first pair's wake (`FirstSight` with the encounter): the zone's |

Damage, as the chapter's: the strike's 130 is 12.7% of Taren's or Sela's bar at Sync 25 in Tempered parts and 9.4% of Idra's at 27 in Choirwrought (a ship's bar and Plating are its hero's). Two pairs pay 240 XP on the way to the Cutter, the third 120 more (the ledger is in `HomeBuilder.Enemies.cs`).

## Both contracts as built (the world engineer, November 14)

`HomeBuilder.Ring` (its numbers are the `Ring` class's constants; the build logs them as `HOME_COORDS BrokenRing ...`); `BrokenRingTests` reads them in the built scene.

**Cover is the shape it looks (D272).** Every district's cover and both wrecks' are one convex collider fitted to what is drawn of them at the plane (`Ring.FitCover`: the outline on the plan of their vertices from 1 m under the plane to 1.5 m over, light, flames, names and the lee left out), solid from 2 m under the plane to 4 m over. The first build's boxes stood the crescent's cabin ends a metre out of its cover and a wreck's box four metres over a plate a hand thick. A district's footprint (what it is spaced by and holds for: `SectionFlight.footCentre`, `footSize`) is that shape's box. `EveryPieceOfCoverIsTheShapeItIsDrawnAtThePlane` checks it.

| Contract item | As built |
|---|---|
| The fight space | C = (0, 1, 280). Open flying at the plane to r 60, walled at r 60 by short boxes under "Ring limits" with a lamp-buoy every 10 m; the circle opens south to the approach (x ±46) and north to the exit lane (x ±14). Nothing solid inside it but the districts at their stations |
| The districts | Stations: market (-42, 296), repair (42, 296), crescent (-42, 264), dock (42, 264): every one within 45 m of C, 14 m and more apart, 8 m and more from every lane, swaying 0.9 m. Cruise 3.8 m/s, 2.2 once 30 m ahead of the lead, never over 4 |
| A district's cover | "Its cover" on each district's root (kinematic `Rigidbody`, nothing static under it), the fitted convex collider above; `RingDistrict` on the root with the contract's ids and names. Off while the district is under the plane |
| The lanes | Eight `CutterLane` empties "Cutter lane 0" to "7" at r 30 round C, bearings 0 to 315, y 1 |
| Encounter | `DebrisCutter`, trigger 100 x 8 x 100 on C, spawner at lane 0 (0, 1, 310) facing C, `faceAsPlaced`, radius 0, `checkpointSpawn` `Ring`. **It waits on `ring.formed`** (a flag the boss sheet's door list wants): `RingFormation` at C sets it once two districts hold their stations within 45 m of C, so the fight never begins with fewer about it; a save at the door has it and loads every district on station |
| Arrival and checkpoint | `Arrival` (-15, 1, 2) in the market's lee; `Ring` (0, 1, 240), facing north, 40 m south of C, clear of every district's way |
| Bryn's tug | "The tug Welcome Back" at (-64, 1, 280) facing C (64 m out at bearing 270, not the contract's 55 m at 225, which stands on the crescent's bend out to its station), a `TowedOff` (`boss` DebrisCutter, `away` (-130, 1, 280), 130 m from C). Drawn only |
| Rim | Wreckage below the plane is laid only where `ZoneKit.ClearOnScreen` keeps it off the screen-shadow of the arrival, the channel, both pairs and their cover, the checkpoint, C (14 m), every lane (6 m), every station (10 m), the tug and the way home |
| After the fight | "Bryn tows the Cutter", an `AfterTalk` on `bossdown.DebrisCutter`: `BrynTowsCutter`, speaker Bryn, heard `home.cutterDisabled`, after 2.5 s. The districts leave their stations for the convoy on `home.cutterDisabled`, and `Warp — The Hull of Many Hands` at (0, 1, 368) opens on it (to `ManyHandsHull` spawn `Ring`) |
| The pairs | `Ring_Pair1` (-16, 1, 118) and `Ring_Pair2` (16, 1, 188): one spawner each, two Interceptors, radius 4, trigger 40 x 8 x 40; 163 and 93 m from C, 77 m apart; the channel's marking 12.5 m off each centre. Cover: "Cover wreck 1" (-20, 1, 134), 16.5 m from Pair1's centre, and "Cover wreck 2" (20, 1, 172), 16.5 m from Pair2's; nothing else solid within 8 m of either centre. `interceptor` is the lead's on Pair1's wake (`FirstSight`, once: `ring.interceptorSeen`). No `Ring_Pair3` |

**The Cutter's run and a roll.** What the Ring gives a roll out of the run's lane (3.6 m wide): open plane round every lane point and C, the nearest district 8 m and more off any lane, the bound at r 60 with 30 m of open plane inside it from every lane. A ship pinned against a district's side or the bound always has the plane open on its other side, and a district's cover is now its drawn shape, so neither a lane nor a roll meets solid in clear space beside one.

## For the replay route (the world engineer, November 14)

Start: `BrokenRing` spawn `Arrival` (-15, 1, 2), cut to from the launch on the Decks (`home.launched`). Everything at the plane, y 1. Bryn's radio plays at the arrival by itself (`ring.brynHailed`); nothing is pressed for it.

1. Fly north in the market's lee, x -15, to z 100. The repair quarter lifts as the lead passes z 10, the crescent at z 70, the tug dock at z 130 (`SECTION_LIFTS`); nothing to press.
2. `Ring_Pair1`: its trigger is 40 x 40 on (-16, 118); fly to (-16, 112) and lock the Interceptors (two). Cover: the wreck at (-20, 134). Ends at `clear.Ring_Pair1`.
3. Cross the channel (x -6 to 6) at z 150 to 160, from (-10, 150) to (15, 160), when its middle lamps there are cold (the danger is the amber run ahead of each piece: `WreckTrack` marks it on the plane).
4. `Ring_Pair2`: trigger 40 x 40 on (16, 188). The second wreck lies across the way at (20, 172) (a 7 x 5 m plate turned -25 degrees, solid as it is drawn: about x 16 to 24, z 168 to 176): keep a hull's width west of it, x 11 or less from z 165 to 179, e.g. (11, 160) to (11, 182), and lock there. Ends at `clear.Ring_Pair2`.
5. Wait at the checkpoint `Ring` (0, 1, 240) until `ring.formed` (two districts on their stations within 45 m of C, about 80 to 100 s after the arrival if the party flies straight on): the Cutter's door waits on it. The checkpoint already lies inside the door's trigger (100 x 100 on C (0, 1, 280)), so the fight begins there the moment the flag is set.
6. Lock "the Debris Cutter" (encounter `DebrisCutter`). It ends at `bossdown.DebrisCutter`; the tug tows it off and `BrynTowsCutter` follows 2.5 s later (advance it): `home.cutterDisabled`.
7. Fly north up the exit lane (x 0) to (0, 1, 362) and press "Warp — The Hull of Many Hands" (at (0, 1, 368), reach 6.5): `ManyHandsHull` spawn `Ring`.
