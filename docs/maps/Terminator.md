# The Terminator - Duskline, the run against the dawn to tread-hall four

Functional brief, written before construction (November 12, chapter 9's wild-run engineer, Claude); built and corrected to the built zone on November 14 (the engineer who took over both chapter 9 places on Duskline). Chapter brief: [ch09-the-house-that-can-leave.md](../chapters/ch09-the-house-that-can-leave.md), "MeridianCrawl and Terminator"; script wiring: [ch09-manifest.md](../writing/ch09-manifest.md); who places what: [ch09-wiring.md](../chapters/ch09-wiring.md). Builder: `S/Editor/World/HomeBuilder.Terminator.cs` (everything nested in `HomeBuilder.Terminator`). The zone's mechanism: `S/World/DawnLine.cs`, `DawnTouched.cs`, `TreadCoupling.cs`, `TreadRide.cs` and the shader `Resources/Effects/Dawn.shader` (guide section 15.31); combat reads the dawn and its shade through `S/Combat/Dawn.cs` (registered by `DawnLine`; see "Run fights contract"); the objective panel reads `S/World/ZoneLine.cs`. Tests: `T/PlayMode/TerminatorTests.cs`. Review status is at the end, and it is an agent's review.

## Purpose and formation

Duskline is tidally locked; its twilight band creeps across the ground, and the Meridian Crawl walks ahead of the sunrise and never stops. Tread-hall four is three tread cars on one drag: the **engine car** ahead (its driver is aboard, waiting), the **kitchens car** (nine chimneys), the **schoolroom car** behind (its bell frame). Each is the generated `CrawlTread`, 14 m along its treads (13.3 wide, 12.3 tall). The cars run in file on long drag cables carried on iron sleds, so a ridge under one car doesn't lift the next. When the hall threw its link to hall three, the snatch ran back down the drag and parted three couplings; every car's brake caught where it stood, and the hall stopped strung out across eight hundred metres of the band. Everyone walked out. Nine kitchens and a schoolroom couldn't. The run is for a place, not for lives.

**Re-mate the three couplings from the back forward, and ride the engine car out ahead of the light.** Each coupling takes the drive a step further down the drag: the schoolroom car's, then the kitchens car's, then the engine car's own.

The zone's one rule is **the dawn**: a hard edge of light on the ground, coming. Everything behind it burns white; everything ahead of it is long blue dusk.

## The dawn, as a player meets it

### Which way the run goes, and why

The ground camera is fixed (pitch 40, yaw 20, 19.5 m, a 30 degree lens): on the ground it sees about 11.9 m north of the hero, 6.2 m south, 9.3 m either side at the hero and 13.6 m at the top of the frame. A dawn behind a party that ran north would be off the bottom of the frame. Three layouts were worked out on the frame before anything was built, measuring how much of a line 10 m behind the hero is on screen and how far ahead the hero can see:

| Run heading | Line 10 m behind: length on screen | Ahead of the hero on screen | Notes |
|---|---|---|---|
| due west, dawn from the east | 10.7 m, in the top-right corner | 8.7 m | the line clips a corner; at 12 m it is 6.8 m long |
| west-south-west (250), dawn from the corner | 11.4 m | 9.4 m | the frame's far corner is 18 m away, but at the gaps that matter it is no better than west, and the band would be diagonal to the grid |
| **due south, dawn from the north** | **16 m, across the top of the frame** | **6.6 m** | the line comes down the screen behind the party, tilted 20 degrees by the camera's yaw |

**The run goes due south, toward the camera, and the dawn comes down the screen from the north.** The line is a band across the top of the frame, the party below it. What that costs is the view ahead (6.6 m), and the layout pays for it: the way is a broad trodden band with nothing tall on it, the hall's drag cable runs beside it on its sleds the whole way as a line to follow, the arrow points at the next coupling, and enemies come from behind (out of the light) and from the sides, where the frame is widest. Nothing tall stands south of where a hero is meant to stand. The stills at each coupling (below, "Review") are the proof.

The dawn never lags more than **11 m** behind the lead (its **reach**, the same on every difficulty): at 11 m, 13 m of line is in frame across its top. A party that runs on keeps the line at 11 m; a party that stops, fights, or turns aside sees it come down the screen.

### What it looks like

- **The edge**: a crisp line of pale gold-white on the ground, a hand wide, never orange or red (those mean a marking, D217).
- **Behind it**: the ground lit hot (its own texture brightened three and a half times, with a heat shimmer through it) and blooming; the hills and cliffs behind it the same; every rock, wreck and car it has reached warmed to a hot glow on its sunward faces; a low veil of light standing over the edge; steam and sparks along it.
- **Ahead of it**: long blue dusk. The key light is the low sun beyond the line, behind everything the camera sees; shadows run south, toward the camera. **As the line nears, the sun climbs and the shadows shorten** (the key light's pitch goes from 9 degrees with the line 26 m off to 24 at the edge, and it warms and brightens), so a stop is felt in the light before the line arrives.
- **Things it reaches change, so the rule is learned by watching** (`DawnTouched`): frost on the ground flashes to steam and is gone; an old crawler buggy's paint blisters and smokes; a cable sled's lamp pops; the cars' back plates go hot. In the opening stretch the line catches a heat-scavenger hunting along it, which flips and burns black, with Taren's "Burrower, along the light. It lunges. It hates shade." said as the party first sees it. All of this happens in the top half of the frame before the line can reach the party.
- **The danger marking**: the strip the line is about to take (from its edge to 1.6 m ahead) is a steady `Telegraph.Line` in the threat colour, laid on the dusk side only, so its rim is always on dark ground.

### The rule

- **Standing in it is a fast burn, then a fall.** A hero behind the edge (more than a quarter of a metre: a roll across the line is not caught) loses integrity at a rate that climbs the longer they stay: 10 % of their bar a second at first, 38 % a second after one second. A clip of half a second costs about 8 %; a second about 24 %; from whole to down is about 2.3 s. The heat in a hero cools over a second out of the light, so a second clip soon after costs more. It is the place's damage (no source): the same on every difficulty, and nothing through a dodge's cover. The lead's `dawn` bark ("on them", the third of its pool) the first time.
- **A partner caught alone is moved, never burnt.** It doesn't choose where it stands: a partner behind the edge while the lead is ahead of it is set down beside the lead.
- **An enemy it reaches burns** at 45 % of its integrity a second (dead in about two seconds) with sparks: a heat-scavenger caught by it dies. A hidden body and a boss are left alone (there are no bosses here).
- **It holds while a hero's hands are on a coupling.** Our numbers need it: at the reach (11 m) and the last stretch's speed (2.7 m/s on Standard) the line is on a hero four seconds after the party stops, and a coupling takes 3.2 s of hands with scavengers coming at the hand. Without the hold, every mating on the last stretch would be a burn. The hold is lost the moment the hands come off (a roll, a swing, a stagger, a step out), and the line moves on.
- **It starts when the party does.** At the arrival and at every restart it stands at its reach, still, until the lead has walked 6 m or eight seconds have passed. A conversation, the pause menu and a held fight stop it.
- **Ahead of it, shade** is real ground: the long shadows the low sun throws south from every rock, wreck and car. **Shade is cover from scavengers and never from the dawn**: they flee it (`ShadeCover`), and the line crosses it like anything else. It shortens as the line nears, because the sun climbs.

### The timetable

Speeds are the dawn's closing speed in metres a second when the party is not moving away from it; the reach caps the gap, so a party that runs south at walking speed or better keeps the line at 11 m. The slowest pair a player can field moves at 6.7 m/s (a hero with no Response past 10, no sprint); the route's legs are within 20 degrees of south, so its southward pace is 6.3 m/s at worst, twice the fastest stretch's speed on the hardest setting.

| Stretch | From, to (z) | Route length | Standard speed | Slack standing still from the reach: Gentle (x0.7) / Standard / Demanding (x1.15) | What holds a party there |
|---|---|---|---|---|---|
| 0, the lesson | arrival (740) to the schoolroom car's coupling (587) | 160 m | 1.6 | 9.8 / 6.9 / 6.0 s | nothing: the line kills a scavenger and burns the buggy where the party can see it |
| 1 | schoolroom coupling (587) to the kitchens car's (397) | 200 m | 2.2 | 7.1 / 5.0 / 4.3 s | a scavenger pair (z 505), a gull (z 450) |
| 2 | kitchens coupling (397) to the engine car's (157) | 250 m | 2.7 | 5.8 / 4.1 / 3.5 s | gulls (z 330), a scavenger pair in the spines (z 255) |
| the couplings | at each | | held while hands are on | 3.2 s of hands, then free; the line moves while hands are off | 1: nothing (the safe lesson); 2: a scavenger pair; 3: a scavenger pair |
| the ride | the engine car, 157 to 47 | 110 m | the car outruns it | the car runs 1.5 m/s slower than the line for its first four seconds (the line comes to 7 m), then pulls away at up to 9 m/s | nothing: input is the car's |

Run without stopping, every stretch keeps the line at its reach on every setting: always possible without taking damage. The slack is how long a fight, a gull's circle or a missed turn can hold the party before the line is on it; after a hold the party wins the gap back at its own pace less the line's (3.6 m/s at worst), so a hold of four seconds is paid back in three of running.

### A fall, a load, a way back in

- **A fall** (both heroes out down: the defeat screen, D260) restarts from the last coupling mated, never the start: each coupling, as it is mated, makes its own spawn the checkpoint and autosaves. Before the first, the run's start (`Arrival`) is the checkpoint. **Retry puts the dawn back at its reach behind that coupling**, 11 m north of its spawn: where it stood when that coupling was mated by a party that came in running, and never closer. (A party that held the line off at the coupling and mated it with the line nearer comes back to the 11 m it would have had; the save holds flags and a spawn, not a line, and a restart that put the line on a hero's back would leave a stretch with no slack.)
- **A save made mid-run** (a cleared fight's autosave, a coupling's) holds the last coupling's spawn, so it loads exactly as a retry does. Nothing a player does leaves a save that can't be finished: the couplings are mated in order (each offered only once the one behind it is), so the checkpoint is always the furthest one mated and the next one is always ahead of it.
- **Back in from the Crawl** at `Arrival` with couplings mated (left by Sela's chart mid-run): the party is set down at the last coupling's spawn as the zone opens, with the line at its reach.
- **After the run** (`home.treadRecovered`): the cars have gone with the Crawl, the line stands far behind and does not advance, and the way aboard the Crawl is open.

### The music

Both tracks are named on the zone's definition (`musicId` "Dventure at Dusk", `fightMusicId` "Starfight"), so `MusicIntake.Stage` stages both. The dawn drives the change itself through `MusicDirector.Fight(owner, on)`: **the pursuit track comes in the first time the line closes within 8 m of the lead**, and stays for the rest of the run (once a run is on, the line is never far enough off to go back to the dusk track, and a track that flapped with every stride would be worse). The dusk track plays at the arrival, before the line has moved, and again once the hall is home. A retry or a load starts on the dusk track until the line closes again. Encounters play the same "Starfight" as their fight cue.

## Users and access

The party comes from the Meridian Crawl by Dorrow's crawler and is set down at the run's start, behind the schoolroom car, the line already in view to the north. Heat-scavengers (native burrowers that hunt the warm edge) and Slag Gulls (fliers that drop molten spit) work the band ahead of the line. The engine car's driver is the only other person here, aboard in the cab.

Ways in and out:
- **In**: from the Crawl (its way to the run, the Crawl engineer's) at spawn `Arrival`.
- **Out**: the ride (the engine car carries the party 110 m south and sets it down at spawn `Tail`), then `Climb aboard — The Meridian Crawl` on Dorrow's crawler waiting there, to MeridianCrawl spawn `Gap`; it stays shut until `home.rideDone` ("Tread-hall four is still out there: re-mate its three couplings and ride it in"). `DorrowTreadHome` plays in the Crawl as the hall rolls in (agreed with the Crawl: docs/maps/MeridianCrawl.md, "For the Terminator").
- Sela's chart can take the party out at any time a fight isn't awake near it.

## Layout

About 56 m west to east inside its banks (x -29 to 27) and 740 m north to south (z 32 to 770). x runs east, z north. The band is a twilight plain of dark regolith and frost: low swells, one dry wash (z 655), two fields of wind-cut rock spines (z 500 to 562 and z 240 to 302), a frost flat (z 452 to 488). The camera's sides stay low: a 2.7 m bank to the west (x -29 to -32) and to the south (z 35 to 32); the far sides are the band's broken edge, a 6.5 m scarp to the east (x 27 to 32) and a ridge to the north (z 767 to 775), the burning plain beyond. **The drag lane** (x -17 to -1, z 47 to 620) is flattened: the cars stand and run on it, and nothing else does.

```text
z 770   ~~~~~~ the ridge; the burning plain beyond (the light starts at z 751) ~~~~~~
z 747   Dorrow's crawler (-7.5, 747)    ARRIVAL (0, 740)   tool cart (6.5, 744.5): repair and save
z 716     lesson burrower (-7, 716)
z 690     old crawler buggy (-12.5, 690)          frost in the hollows (z 636 to 736)
z 655   ===== the dry wash (crossed) =====
z 613   [SCHOOLROOM CAR (-9, 606), x -15.6..-2.4, z 599..613]     the way passes 7 m east (x 6..7)
z 594.5   C1 jaw (-9, 594.5), seat (-6, 594.5); checkpoint Schoolroom (-3.5, 591.5)
z 593..423   drag cable on sleds, x -9 (a low block: stepped round)
z 562..500   rock spines (shade), east of the way and beyond the lane    SCAVENGERS (1, 520)
z 488..452   the frost flat     GULL (5, 462)
z 423   [KITCHENS CAR (-9, 416), z 409..423]   nine chimneys
z 404.5   C2 seat (-6, 404.5); checkpoint Kitchens (-3.5, 401.5)    SCAVENGERS (1, 400)
z 402..179   drag cable, x -9
z 330   GULLS (1, 330)
z 302..240   the second spines    SCAVENGERS (5, 262)
z 179   [ENGINE CAR (-9, 172), z 165..179]   stack; the front gallery (x -13.4..-2.8, z 162.6..165, 1.6 m up)
z 162.4   board: Ride the tread-hall out (-1.4, 162.4)
z 160.5   C3 seat (-6, 160.5); checkpoint Engine (-3.5, 157.5)     SCAVENGERS (1, 161.5)
z 165..55   the ride, south, 110 m
z 51    Tail (-3.5, 51)     Dorrow's crawler (8, 45.5): Climb aboard - The Meridian Crawl (from (5.8, 47.6))
        x -30      -10     0     10      30
```

| Area | What it is for | Combat |
|---|---|---|
| The arrival | Where the crawler sets the party down: a hard pan, its tool cart (free repair), the line in view 11 m north | none |
| The lesson (z 740 to 600) | The line is slow here (1.6 m/s). It takes the burrower hunting along it, blisters the buggy's paint and flashes the frost off, west of the way where the frame is widest, before it can reach the party. The dry wash is the stretch's landform | none |
| The schoolroom car (z 613 to 594) | The first coupling, the safe lesson of a hold: nothing comes. The car's bell frame and painted number; the gantry and its amber lamp mark the jaw | none |
| Stretch 1 (z 594 to 423) | Spines with long shadows south over the way's edge (the first shade the party runs through), then the frost flat. The drag on its sleds to the west | scavenger pair, a gull |
| The kitchens car (z 423 to 401) | The second coupling, worked under scavengers while the partner covers. Nine chimneys | scavenger pair |
| Stretch 2 (z 401 to 179) | The fastest line. Gulls over open ground, then the second spines | gulls, scavenger pair and a gull |
| The engine car (z 179 to 157) | The last coupling under a scavenger pair, then the gallery steps | scavenger pair |
| The ride (z 165 to 55) | The engine car pulls the hall out south with the party on its gallery; the line falls behind it. Set down at the tail; the way home is Dorrow's crawler | none |

## Identities

Scene `Terminator` (ZoneKind GroundCombat, profile Duskline, threat 1, the first engineer's definition). Music: Dventure at Dusk, then Starfight (above). Light: `TerminatorLooks` in the builder (long blue dusk; the key light is the dawn behind the line, turned by `DawnLine`). Ambience: none set (guide 14: a hand step for the lead).

## Route coordinates

The builder's constants (`HomeBuilder.Terminator`), as built (the log line `TERMINATOR_COORDS`). x, z on the plan; the plain stands at about y 0 with swells of up to .4 m, flat on the drag lane. A route's `walk`/`path` points keep to the trodden way (`HomeBuilder.Terminator.Way`): (0, 740) (2, 722) (-2, 704) (3, 686) (6, 668) (2, 652) (5, 636) (7, 620) (6, 606) (1, 598) **C1 (-6, 594.5)** (-3, 588) (1, 572) (4, 556) (2, 540) (-1, 522) (1, 506) (4, 490) (6, 474) (3, 458) (5, 442) (7, 428) (6, 416) (1, 408) **C2 (-6, 404.5)** (-3, 398) (1, 382) (5, 366) (3, 350) (0, 334) (-2, 318) (1, 302) (5, 286) (7, 270) (4, 254) (1, 238) (3, 222) (6, 206) (7, 190) (6, 178) (2, 166) (-1, 161.4) **C3 (-6, 160.5)**, board at (-1.4, 162.4); after the ride (-3.5, 51) (1, 50) (5.8, 47.6). **A route against the line has to keep moving**: hold a coupling's prompt (South held through the hold) only once the panel says "Re-mate the coupling", and expect the line 11 m behind on arriving at each.

| Kind | Id / name | x, z | Range or box | Flag it sets / needs | Notes |
|---|---|---|---|---|---|
| Spawn | `Arrival` | 0, 740 | | | From the Crawl's drop cage; the run's first checkpoint |
| Spawn | `Schoolroom` | -3.5, 591.5 | | | Checkpoint once `home.coupling1` is set |
| Spawn | `Kitchens` | -3.5, 401.5 | | | Checkpoint once `home.coupling2` |
| Spawn | `Engine` | -3.5, 157.5 | | | Checkpoint once `home.coupling3` |
| Spawn | `Tail` | -3.5, 51 | | | Where the ride sets the party down (the partner at (-1.5, 51)) |
| Service | `Crawler's tool cart — repair and save` | 6.5, 744.5 | 2.8 | | Free; saves at `Arrival` |
| Hand | `Re-mate the coupling` (schoolroom car) | -6, 594.5 | seat 1.8 | sets `home.coupling1` | No enemies; 3.2 s of hands |
| Hand | `Re-mate the coupling` (kitchens car) | -6, 404.5 | seat 1.8 | needs `home.coupling1`, sets `home.coupling2` | |
| Hand | `Re-mate the coupling` (engine car) | -6, 160.5 | seat 1.8 | needs `home.coupling2`, sets `home.coupling3` | |
| Hand | `Ride the tread-hall out` | -1.4, 162.4 | 2.6 | needs `home.coupling3`, sets `home.rideDone` | The ride: 110 m south, about 14 s |
| Way out | `Climb aboard — The Meridian Crawl` | 8, 45.5 (stand at 5.8, 47.6) | 3.4 | needs `home.rideDone` | To MeridianCrawl spawn `Gap` |
| Fight | `Terminator_Burrowers` | 1, 520 | 30 x 24 | | 2 `HeatScavenger` from (-5, 532) and (12, 530) |
| Fight | `Terminator_Gull` | 5, 462 | 32 x 22 | | 1 `SlagGull` from (-4, 450); `gull` said as it wakes |
| Fight | `Terminator_Kitchens` | 1, 405.5 | 30 x 35 (north past the line at its reach, so a fight framed keeps the dawn in view) | | 2 `HeatScavenger` from (12, 413) and (14, 401) |
| Fight | `Terminator_Gulls` | 1, 330 | 32 x 22 | | 2 `SlagGull` from (-5, 320) and (14, 322) |
| Fight | `Terminator_Spines` | 5, 262 | 30 x 24 | | 2 `HeatScavenger` from (-4, 274) and (16, 272), 1 `SlagGull` from (15, 252) |
| Fight | `Terminator_Engine` | 1, 161.5 | 30 x 35 (likewise) | | 2 `HeatScavenger` from (12, 173) and (15, 167) (nothing from the south, and no gull: a lobber's spit is framed as it flies, and one here drew the view back past the dawn while the hands were on; it hunts the second spines instead) |

## For a replay route

What a route through the run must do (load `chapter9` with `home.crawlAsked` and the earlier main steps told; it comes in from the Crawl's drop cage at `Arrival`):

1. **Start** at `Arrival` (0, 740), facing south. The line stands 11 m north and doesn't move until the lead has walked 6 m or 8 s have passed: set off at once.
2. **Keep moving south along the way** (Route coordinates above). The line closes at 1.6 m/s, then 2.2, then 2.7 on Standard; a route that stops loses its slack in 6.9, 5.0 and 4.1 s, and a hero more than a quarter-metre behind the edge burns (about 2.3 s from whole to down). A fall is the defeat screen: Retry restarts at the last coupling's spawn with the line 11 m behind.
3. **The fights are not gates**: no box shuts. `Terminator_Burrowers` (1, 520), `Terminator_Gull` (5, 462), `Terminator_Kitchens` (1, 405.5), `Terminator_Gulls` (1, 330), `Terminator_Spines` (5, 262), `Terminator_Engine` (1, 161.5). A route can run through them; the line takes any enemy it passes. Fighting while standing costs the stretch's slack.
4. **Each coupling**: walk onto its seat ((-6, 594.5), (-6, 404.5), (-6, 160.5)), then press Interact once and **stand still with nothing else pressed** until `COUPLING_MATED n` (3.2 s). Don't roll, swing, use a skill or step more than 2.2 m from the seat's middle: any of those lets go (`COUPLING_LET_GO`), what was wound runs back, and the line moves again. The line holds while the hands are on. A coupling is offered only once the one behind it is mated (`home.coupling1` to `3`). At the kitchens and engine couplings a scavenger pair are awake in the box: a staggering hit lets go, so clear them first or press again after.
5. **The ride**: after `COUPLING_MATED 3`, walk to (-1.4, 162.4) and press Interact (`Ride the tread-hall out`). Input is blocked for about 14 s (`RIDE_BEGUN` to `RIDE_DONE`); the party is set down at `Tail` (-3.5, 51) with `home.rideDone` set and saved.
6. **Out**: walk (1, 50) to (5.8, 47.6) and press Interact (`Climb aboard — The Meridian Crawl`): MeridianCrawl, spawn `Gap`.

Logs a driver can wait on: `DAWN_LIVE`, `COUPLING_TAKEN n`, `COUPLING_MATED n`, `COUPLING_LET_GO n`, `DAWN_BURNS_HERO`, `RIDE_BEGUN`, `RIDE_DONE`.

## Contracts

### With the first engineer (shared definitions, dialogue, quests)

- Flags set here: `home.coupling1`, `home.coupling2`, `home.coupling3` (by the couplings themselves, as the world: each also reports `Flag` to the quests) and `home.rideDone` (the ride's end, the world). `home.rideDone` is on the main path and is in `HouseLeaves.storyFlags` (added by exact edit, November 14). `home.treadRecovered` is set in the Crawl, by `DorrowTreadHome`'s own last line, played there as an `AfterTalk` on `home.rideDone` as the hall rolls in.
- Step 6 (Flag `home.treadRecovered`): its arrow in this zone is this zone's own (`ZoneLine`): the next coupling, then the gallery, then the way home.
- Barks used, all as the manifest has them: `dawn` (the lead: far as the line first stands in view, near the first time it is within 5 m, on them the first time a hero is burnt), `hail.dawn` (Dorrow, 2.6 s after each), `remate` (the lead, the coupling's own index), `hail.remate` (Dorrow, 3 s after), `scavenger` (`FirstSight` within 15 m of the lesson's burrower), `gull` (`FirstSight` as `Terminator_Gull` wakes).
- The objective rows of manifest section 6 are shown through `S/World/ZoneLine.cs` (read by `ObjectiveHud`'s default branch): "Keep ahead of the dawn" while the line is moving; "Re-mate the coupling (n/3)" within 16 m of the next coupling (n is how many are mated); "Ride the tread-hall out" once the third is mated; after the ride, the arrow alone on Dorrow's crawler.

### With the Crawl (agreed: the same engineer holds both places since November 14)

- The Crawl's drop cage (`Ride the drop cage down — The Terminator`, locked on `home.crawlAsked`, naming Dorrow) arrives at this zone's `Arrival`. Once `home.treadRecovered` is set it is taken away with its sign (the ground behind the town is in daylight).
- `Climb aboard — The Meridian Crawl` arrives at the Crawl's `Gap`. The Crawl shows tread-hall four from `home.rideDone`, rolling in on that first arrival (`CouplesIn`), with Dorrow at the lip and `DorrowTreadHome` played as it comes; the lip stays shut until it has coupled and he has said so.
- What the Crawl reads from here: `home.rideDone`. Nothing else.

### Run fights contract (with the first combat engineer)

What the run asks of `HeatScavenger` and `SlagGull`, and what the ground gives them. Encounters name the ids (`HomeBuilder.Foe`): until the definitions exist they stand in as `Glassback` (a charger) and `MusterMortar` (a lobber) and log `HOME_FOE_STANDIN`.

**What the ground gives:**
- `Lattice.Combat.GroundHeat` is the dawn while the zone is open: `GroundHeat.Of(p)` is 1 behind the line (it burns), rising from 0 to 0.5 over the 6 m ahead of it (the warm edge scavengers hunt), 0 further ahead. `Hot(p)` is the edge band and the light; `Bright(p)` is the light.
- `Lattice.Combat.ShadeCover` (new, `S/Combat/ShadeCover.cs`): `ShadeCover.In(p)` is whether a point lies in the long shadow of a rock, a wreck or a car, as it is drawn now (shadows shorten as the line nears). Registered by the dawn; anywhere else nothing is shade.
- `DawnLine.Current` is World's; combat only needs the two above.
- The line burns an ordinary enemy at 45 % of its integrity a second: anything left behind it dies in about two seconds.
- The ground is open plain with low swells; rock spines (2.5 to 6 m) stand east and west of the way in two fields (z 560 to 500 and z 300 to 240); nothing walls a fight. Each encounter's box is the stretch of way it covers; its spawners are where the brief's burrowers come up (behind and to the side of the party, between it and the line) or, for gulls, ahead and to the side.

**What the run needs from the enemies:**
- **Heat-scavenger**: hunts along the line (keeps to `GroundHeat` about 0.5 when it can), lunges at a hero within its reach with a marked lane, flees shade (won't enter `ShadeCover.In`, breaks off a lunge whose lane runs into it, and leaves shade it finds itself in). The answer a player has is to step aside and keep running south, or to run through shade. It should be killable on the move: a lunge, a miss, an opening a hero can hit once or twice in passing.
- **Slag Gull**: a hovering lobber that drops its spit on marked circles ahead of the party (Burn), indifferent to shade; the answer is to sidestep and keep going, or to shoot it down from under it.
- **How long a fight may hold the party** (stand it still, or turn it back north) before the line is on it, from the reach, on each setting (Gentle / Standard / Demanding): stretch 1, 7.1 / 5.0 / 4.3 s; stretch 2, 5.8 / 4.1 / 3.5 s. At the couplings the line holds while the hands are on, so a pair there may take as long as it takes, provided the partner keeps them off the hand: a scavenger that lands a staggering hit on the hand (or makes it roll) lets go of the coupling, and from then the line moves. Tune a pair so a party fighting it while still moving south loses no more than those seconds; a fight the party stands and wins should cost it about the slack and no more.
- Counts on the path, for the ledger: 8 `HeatScavenger`, 4 `SlagGull` (no optional fights).
- `book.Tell` for each is the combat engineer's (where a hero stands for its tell), in their own test class.

**The combat engineer's answer** (November 12, the first combat engineer; the enemies are `HomeBuilder.Enemies.cs`, tested on a stand-in line in `T/PlayMode/HomeCombatTests.cs`):

- **One call instead of two.** The dawn as combat reads it already exists: `Lattice.Combat.Dawn` (`S/Combat/Dawn.cs`). `DawnLine` registers it in `OnEnable` and takes it back in `OnDisable`: `Dawn.Register(this,ahead:p=>/* metres p stands ahead of the line, negative behind it */,shade:p=>/* ShadeCover.In(p): the shadows as drawn now */);` and `Dawn.Unregister(this);`. Please **don't** register `GroundHeat` here (an ambusher reads `Dawn` first wherever one is registered, and nothing else on the run reads heat) and **don't** write `ShadeCover.cs`: `Dawn.Shade` is it, and the line's shortening shadows are whatever your `shade` answers at the moment.
- **The dawn and an enemy: one rule, not two.** Registering makes `DawnTakes`: any ordinary enemy the line has passed by 1.5 m flares white-gold where it stands and falls, in sight, and **pays nothing** (the party didn't win it; it carries the tag a self-detonating mine does). So `DawnLine` shouldn't burn enemies itself, or a body is killed twice. If you would rather keep your 45%-a-second burn with sparks (a two-second death reads well too), register with `takes:false` and give your packet `tag=EnemyBrain.SelfDetonation`, so it pays nothing either; your call, say which here.
- **Heat-scavenger** (an `Ambusher` that lunges; integrity 1,000, 45 XP). It hunts the **warm band, 1.5 to 14 m ahead of the line** and out of shade: it goes under only there, making its hides across from the lead (your 11 m reach keeps a moving party inside the band, so it is always in reach of a party that dawdles and always chasing one that runs). A hero inside its 3.6 m ring draws it up; it lunges down a marked lane 5 m by 1.6 (.8 s), a hit of 129, then stands **spent in the open 2 s**: one or two blows in passing, killable on the move. **Shade**: it won't come up after a hero standing in shade, it **breaks off a lunge whose lane runs into shade** and runs for the band, a hide that falls into shade or behind the line is left at once, and spent in shade it runs. Answer: step aside from the lane and keep south, or run through shade.
- **Slag Gull** (a hovering `Lobber`; integrity 1,050, 50 XP): from up to 12 m it spits onto a marked 2.2 m circle where the lead is as it throws (1.2 s in the air), a hit of 127, which burns 2.4 m round for 3.5 s; a party moving south has left the circle by the time it lands. Indifferent to shade. Answer: sidestep and go on, or shoot it down (Pulse).
- **Holds**: two scavengers are 2,000 integrity and land at most one lunge each before a moving pair is past; stood and fought, a Sync 25 pair takes about the stretch's slack (4 to 7 s). The gulls hold nobody who keeps walking. Your counts are accepted (8 scavengers, 4 gulls, no optional fights): 560 XP, in the ledger.
- `book.Tell` for both is in `HomeCombatTests` (2.4 m inside the scavenger's ring, 7 m from a gull). Your stand-ins (`Glassback`, `MusterMortar`) give way by themselves: both definitions exist.

### The Terminator's answer (November 14: coordinates written back)

- **One call.** `DawnLine` registers `Lattice.Combat.Dawn` in `OnEnable` with `takes:true` (`Dawn.Register(this,Ahead,Shade,true)`) and takes it back in `OnDisable`. Nothing here registers `GroundHeat` and there is no `ShadeCover.cs`: `Dawn.Shade(p)` is `DawnLine.Shade(p)`, the long shadow of the zone's casters swept south at the sun's pitch now (about three times a caster's height with the line at its reach, shorter as it nears). `DawnLine` burns only heroes; enemies are `DawnTakes`'s.
- **Where shade is**: the rock spines in two fields, z 500 to 562 and z 240 to 302 (east of the way at x 4 to 24, a few short ones west of it, the rest beyond the drag lane at x -27 to -19); each car (x -15.6 to -2.4, its z span), the crawlers, the buggy and each coupling's gantry. Every shadow falls south, toward the camera.
- **The fights' coordinates** are the table above (Route coordinates): every spawner east of the drag cable (x -9), which is a low block a body steps round, so nothing spawns in a pocket west of it. The warm band is open plain on every stretch.
- Counts on the path, as accepted: 8 `HeatScavenger`, 4 `SlagGull`.

## Review

**The road the halls have driven (November 14, a look engineer, on the lead's note to `ct9-term-08`):** generational tread ruts down the band (six pairs 13 m apart, trodden pale and tracked dark), tread-hall four's own fresh tracks down the drag lane, cairns every 12 to 21 m east of the way and west of the drag lane (every second a marker pole with its cloth, every fourth a lamp; they warm as the light reaches them), about two hundred pieces the halls have shed (links, a sled's runner, an axle, a coil of drag cable, fuel cans), four tread units of an old hall east of the way, the crew's dump of spares round the run's end, rime in the shade south of every rock, wreck and car (the Dawn shader's frost: it burns off as the line comes), and steam rising off the ground just behind the edge, carried with the line. Drawn only, off the way and the drag lane (shed iron, a hand high, may lie in a fight's floor; cairns and hulks may not); nothing throws shade. Rime along the ruts was tried and read as rain. Stills `Builds/quality/look/term-road-06`.

November 14, after Unity came back: built, lit and looked at in four passes (`Builds/quality/look/ct9-both-04`, `-05`, `ct9-both-06`, `ct9-term-07`, `ct9-term-08`).
- Pass 04 (the first still of the run anyone saw): no sunrise at all behind the line (the light multiplied into a dusk floor a few hundredths bright), the frost a sheet of fog over the opening stretch, grit like oil stains everywhere, the bank rocks orange. The light is now added (pale gold-white, `Blend One One`), the frost a dusting, the grit sparse and pale, the rocks blue-grey.
- Pass 06 to 08: the dawn reads in every still: a burning pale-gold field across the top of the frame with a hard white edge, diagonal on screen, long shadows running down the screen toward the camera, the dusk below. At each coupling the car's front and its tread fill the top-left, the gantry and lamp mark the jaw, the drag runs off south; the run's own regolith (frost-heaved plates, dark cracks rimmed pale) replaced the Crawl's plain. The ride's end has Dorrow's crawler, the crew's marker post and links the Crawl threw.
- `look_density` (`ct9-term-08`): 12 of 18 under .22; still over: the kitchens and engine couplings (.33 to .37, the cars' long shadow over plain plate), the gulls stretch (.25), the middle wide view (.24), and the ride's end and the crawler (.73 to .80: the still shows the end without the engine car the ride brings there).
- Not seen: anything moving (stills pose the line by each view's gap; the veil, the sparks, the burrower's death, the ride itself and the heat on the cars are runtime only).