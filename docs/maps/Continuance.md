# The Continuance - the Compact director's great ship, on foot, with a flight berth round its hull

2026-11-14. Builder: `LastMooringBuilder.Continuance.cs` on `ZoneKit` (the Continuance engineer, stream D). Chapter brief: [ch10-a-route-with-room-to-turn-back.md](../chapters/ch10-a-route-with-room-to-turn-back.md), "Continuance". Who places what: [ch10-wiring.md](../chapters/ch10-wiring.md) sections 0 to 6.5, 9 and 10. Script: [ch10-manifest.md](../writing/ch10-manifest.md) (`IdraSpineBraced`, `PatrolLetsPass`, `MeretShipToShip`, `MeretGantry`; barks `hail.spine`, `hail.aside`, `standaside`, `helpers`). Tests: `T/PlayMode/ContinuanceTests.cs`. **MAP_EYE_TEST: plan written before construction (this page); editor stills under "Evidence".** Every number below is an `internal static readonly` (or `const`) in `LastMooringBuilder.Continuance`: read it from there, never copy it. The build logs them all on one line (`MOORING_COORDS Continuance ...`).

## What the place is for

Meret's engineering vessel, carrying the last anchor: clean, well kept, every panel signed and dated in the same neat hand, built by people who meant well. The party docks at the foot of its boarding spine (Idra's crews hold the spine's rings open with their braces), walks up it into the ship, through the crew deck where Compact crews fight, stand aside or hold a hatch, across maintenance deck four where the captain of the *Steady Hand* lets them pass, through the anchor works (the game's earlier machinery, built properly: the Saint's press, the Works' welding line, the Foundry's coolant sluices, Oriel's clamp, on the anchor's own chain), past the command deck, into the launch bay. From the bay's empty cradle (the director's craft has gone out) the party launches into the flight berth over the hull's back, where Meret hails them and phase one is fought ship to ship. Afterwards she is at the frame gantry's rail and the dock to it is drawn.

**Each deck has one job you can read from one still:**

| Deck | Its job | What says so | Its light |
|---|---|---|---|
| The boarding spine | a gangway across open space into the hull | rings over a white deck, two rings jacked open by orange rescue braces with Idra's crews at them, the convoy below, the hull's flank ahead with its name | open starlight from the side, amber work lamps at the braces |
| The crew deck | where three hundred people live | the mess's long tables under warm pendants, bunk rows, a locker wall with names, the duty board, windows onto the convoy | warm pools over the tables, cool window light |
| Maintenance deck four | where the cutters are kept | a Compact cutter on its service cradle with its panels off, benches, tool walls, hoists, the captain's crew across the deck | hard white work lights on the cutter |
| The anchor works | where the anchor's gear is made | two presses, a welding line, the coolant sluices, a clamp on its test stand, a link of the anchor's chain on rollers down the middle | the press beds' heat, the weld's blue-white, the coolant's cyan |
| The command deck | where the director was | the chart table showing the convoy and the anchor, a crescent of consoles, an empty chair, windows onto the anchor | the chart's teal, the anchor's window |
| The launch bay | where the craft go out | two cutters on cradles, the empty cradle with its marks, the open mouth onto space | floodlights on the cradles, the mouth's cold field |

## Rules of construction

- The decks are one heightfield (`GroundTerrain`, Works-style): each room a rounded box at its level; between rooms the hull's own inner plating (`Height`), drawn on the camera's side of every floor under the real line of sight with room to spare and nothing within 5 m of any lens (`Seen`, D272, the Works' rule). North and east walls (the ones the camera sees) carry each deck's own plate, trim and fixtures; south and west walls stay low.
- The spine is built geometry outside the heightfield (south of it), over open space, its deck a ramp of box colliders and its edges rails a body can't pass (rails 1.1 m and the rings; nothing walks off it). The height function answers on the spine too, so `kit.On` works there.
- Windows are openings in the outer edge of the heightfield: a 0.9 m sill, mullions and a head beam built on it, nothing beyond but space (and what is put out there to be seen: the convoy, the anchor). A stop box stands in each opening (there is no ground beyond it).
- The flight berth is the space over the hull's back: the plane at **y 19**, the hull's top drawn at **y 16** (no collider, no shadow), every deck at or under y 0 and its plating under y 8, so the ground camera (13.5 m over a floor at most) never sees the hull's top and the flight camera never sees a deck. Nothing of the hull stands above the plane south of the lanes; the superstructure, the shelter's wings and the gantry stand north of them.
- Colours: the Compact's white, teal and a little gold. Words are painted in the Compact's neat hand (a stencil of the ship's own, `Painted`).

## Plan

North up; x east, z north. Floors: the spine from y -5 at its foot to y 0 at its top; the hatch and the crew deck at y 0; everything from maintenance deck four on at y -3.

```text
 z 264 ----- berth's north edge: the superstructure (bridge block) above the plane, north of every lane
 z 246-262   Meret's shelter (two wings) at x -14 and x 6; her craft at (-4, 19, 252); the frame gantry x 16..32 with its rail and dock (24, 19, 247)
 z 206-262   FLIGHT BERTH arena over the hull's back (door MeretShip), plane y 19, hull top y 16
 z 203       MeretShipToShip crossing, across the whole berth
 z 196       the open launch hatch on the hull's top; the berth's way back ("Dock — The Continuance")
 z 182       flight spawn (where the party comes out)
 z 160 ----- berth's south edge (box x -48..40, z 160..264)
 ------------------------------------------------------------------------------- on foot, under all of the above:
 z 252       the bay's mouth onto space (a field across it)
 z 195-252   LAUNCH BAY x -28..12 (y -3): cutters on cradles x -18; the empty cradle (2, -3, 242) and the pad (2, -3, 238); LaunchBay spawn (5, -3, 230)
 z 176-206   COMMAND DECK x 13..31.4 (y -3), east windows onto the anchor; joined by a corridor at z 182-189
 z 171-196   the passage north (x -3..3)
 z 172       the works' north door: shut until clear.Continuance_Works (the last door before the bay)
 z 112-172   ANCHOR WORKS x -30..26 (y -3): presses west, welding line east, sluices north, chain link down the middle
 z 112       the works' south door (seals behind the partner)
 z 57-104    MAINTENANCE DECK FOUR x -24..24 (y -3): the cutter's cradle west, the captain's line across at z 92, his crossing at z 86
 z 50-57     the stair down (y 0 to -3)
 z 8-50      CREW DECK x -18..31.4 (y 0): mess east by the windows, bunks and lockers west, the duty board north
 z -8-9      the hatch hall (y 0)
 z -8        the hull's south flank and the hatch
 z -16..-8   the spine's top landing (y 0): IdraSpineBraced
 z -68..-16  THE BOARDING SPINE, rising y -5 to 0; rings every 8 m; Sibb's brace at the aft ring z -54, Kott's at the forward ring z -30
 z -86..-68  the spine's foot (y -5): Spine/Arrival spawn (0, -5, -76), the pad home (4.2, -5, -81), the cradle (-4.4, -5, -80)
```

## Coordinates (as built; `LastMooringBuilder.Continuance`)

| Place | World (x, y, z) | Id / flag |
|---|---|---|
| Arrival, and Last Mooring's way in | (0, -5, -76), facing north | spawns `Arrival` and `Spine` (`Continuance.SpineSpawn`) |
| The pad home | (4.2, -5, -81) | `Launch — Last Mooring` -> `LastMooring`, spawn **`Continuance`** (`Continuance.MooringBack`, stream F stands it beside its dock); drawn until `end.meretShip` |
| The spine's cradle | (-4.4, -5, -80) | free, saves `Spine` |
| The spine | x -2.6..2.6 from z -68 (y -5) to z -16 (y 0); rings at z -62, -54, -46, -38, -30, -22; alcoves (x to ±4.4) at the braced rings | |
| Sibb (aft ring) / Kott (forward ring) | (-3.4, ·, -54) / (3.4, ·, -30), facing the walk | bodies (Idra's crew) |
| Idra's radio on the spine | at the foot, range 30 | RadioHail `hail.spine` twice, heard `end.spineHailed`, silenced by `end.spineTop` |
| The spine's top | landing x -4..4, z -16..-8, y 0; crossing box (0, 0, -12.5) 8 x 4 x 3 | flag `end.spineTop`, node `IdraSpineBraced`, speaker `Idra`, **report `Spine`** |
| The crew deck's fight | box centre (12, 0, 30), 34 x 7 x 36; two wardens at (14, 0, 36), facing south | `Continuance_Mess`, needs `end.boarded` |
| Crews standing aside | the west bunk rows and the locker wall (see People) | FirstSight `standaside` |
| The crew who holds the hatch | at the stair head (4.4, 0, 48.6), facing west | FirstSight `helpers` |
| Deck four's cradle | (-19.4, -3, 62) | free, saves `DeckFour` |
| The captain's crossing | box (0, -3, 86) 48 x 5 x 3, the whole deck's width | flag `end.captainPassed`, node `PatrolLetsPass`, speaker `Patrol` |
| The captain / where he steps to | (0, -3, 92), facing south / (-16, -3, 94) | `Guide`, leg on `end.captainPassed` |
| His crew | four across z 92 at x -7.5, -3.8, 3.8, 7.5 | `LeavesAfter` `end.captainPassed`, out by deck four's east door (27, -3, 77) |
| His radio | deck four, range 30 | RadioHail `hail.aside`, needs `end.captainPassed`, heard `end.asideHeard` |
| The works' south door | seal (0, -3, 112.5), 6 m wide, open until the fight begins | |
| The works' fight | box centre (-2, -3, 142.5), 52 x 7 x 54 (z 115.5..169.5, starts 3 m inside the south door) | `Continuance_Works` |
| The works' north door | seal (0, -3, 171.5), 6 m wide, **shut until `clear.Continuance_Works`** | the last door before the bay (`Continuance.WorksPassed`) |
| The command deck | x 13..31.4, z 176..206; the chart table (22, -3, 190); a cache at (29.6, -3, 202) | |
| The bay's cradle | (9, -3, 216) | free, saves `LaunchBay` |
| LaunchBay | (5, -3, 230) | spawn `LaunchBay`: the berth's return spawn **and** the MeretShip door's checkpoint |
| The berth's pad | (2, -3, 238), at the empty cradle | `Launch — After the director's craft` |
| Coming back on foot | (-6, -3, 229) | the berth's foot spawn |

## Berth contract (the flight berth round the hull)

One call to stream A's `Berth.Declare` (wiring 6.1.1), the reverse of a shelf: a flight berth beside a ground zone.

| Argument | As built |
|---|---|
| id | `HullBerth` (`Continuance.HullBerth`); `docked:HullBerth` is flying over the hull, `launched:HullBerth` is back on foot in the bay |
| shelf | "After the director's craft" (the pad reads "Launch — After the director's craft"; the way back "Dock — The Continuance") |
| kind | `ZoneKind.SpaceCombat` |
| box | `HullBerthBox`: centre (-4, 19, 212), size 88 x 3 x 104 (x -48..40, z 160..264, y 17.5..20.5) |
| pad | `HullBerthPad` (2, -3, 238), on foot, in the bake |
| launch | `HullBerthLaunch` (-8, 19, 196), over the open launch hatch |
| footSpawn | `HullBerthFoot` (-6, -3, 229), 8.5 m from the pad |
| flightSpawn | `HullBerthFlight` (-8, 19, 182), 14 m from the launch |
| returnSpawn | `LaunchBay` at (5, -3, 230) |
| inFight / refuseWhileAwake | **true** / true: the berth is her fight's own, so its pad takes a party back up into her fight as its launch takes them down out of it (it was false until November 14, and a party that launched back into the bay mid-fight was refused the way up for the rest of her fight); not offered with a crew awake near |

Constants: `HullBerth`, `HullBerthBox`, `HullBerthPad`, `HullBerthLaunch`, `HullBerthFoot`, `HullBerthFlight`, `BaySpawn`, `LaunchBayAt`, `Plane` (19), `HullTop` (16).

## Boss arena contract (MeretShip, phase one)

*Stream H writes here what the fight needs; the Continuance engineer builds it and writes the coordinates back under it. Until then this is the room as built, to start from.*

### The room as built (Continuance engineer, November 14)

| Thing | As built | Why |
|---|---|---|
| Door | encounter **`MeretShip`**, box centre `DoorCentre` (-4, 19, 234), size `DoorSize` 80 x 8 x 56 (x -44..36, z 206..262), its whole south face (80 m) the mouth the party flies in by; `requiredFlag` `end.meretHailed`; `checkpointSpawn` **`LaunchBay`** (on foot, 24 m from the door's middle, on navigation); its one seal is **the works' north door** (the works' own seal too), shut from her fight's start to her craft's fall, so the bay can't be walked out of mid-fight (November 14: `QaPlanSystemsTests.NoWayOutAnswersInEveryLaterBossFight`) | wiring 6.5; a defeat in flight retries on foot at the bay (rule 4) |
| Boss | its spawner at `MeretAt` (-4, 19, 252), yaw 180 (facing the party), `faceAsPlaced`, radius 0; `previewPoint` at the hail crossing, radius 52 (she is on show in her shelter from the crossing). Named through `Foe("MeretShip","Interceptor")`: **until stream H defines `MeretShip` with its Kit, an Interceptor stands in** (log `MOORING_FOE_STANDIN`) and nothing sets `bossdown.MeretShip` | |
| Her shelter | two armoured wings on the hull's back, solid at the plane: west wing x -16..-12, east wing x 4..8, both z 246..260, drawn to y 24; behind her the superstructure's foot, solid from z 262 | "her craft fighting from its shelter" |
| The lanes | open air at the plane over the whole arena, x -44..36, z 206..246, and between the wings; nothing above the plane south of z 246 but the ships | the flight camera is 19 m over and 17 m south of a ship |
| The hull's anchor clamps | three seats on the hull's back for her moving clamps: `ClampSeats` (-30, 16, 228), (-4, 16, 222), (22, 16, 228); drawn there are only the seats (rings in the plating). The clamps that reshape the lanes are the fight's to stand and move (an `AnchorClamp` reaches the plane from a seat if it stands 3 m tall) | "anchor clamps that reshape the lanes with visible warnings" |
| Cover | none placed: say where you want it (a mast housing solid at the plane is the house piece) | |
| Her way out | `BossExit` `MeretShip` at `MeretExit` (24, 19, 258), behind the gantry's rail | "her craft leaves by a BossExit at the gantry's dock" |
| After | `MeretGantry` is an `AfterTalk` on `bossdown.MeretShip` at the gantry, delay 2.5 s, heard `end.meretShip` (its own line); her body stands at the gantry's rail at `MeretAtRail` (27, 19, 251) from `bossdown.MeretShip`; `Dock — The Anchor Chamber's frame gantry` at `GantryDock` (24, 19, 247) -> `StillwaterChamber` spawn **`Gantry`** (`Continuance.GantrySpawn`, the chamber engineer stands it), drawn from `end.meretShip` | wiring 1, 2 |
| The hail | `MeretShipToShip` crossing box `HailAt` (-4, 19, 203), 88 x 8 x 4, across the whole berth between the flight spawn and the door | the door's `requiredFlag` |

### What the fight needs (Meret engineer, stream H)

*November 14. `MeretShip` is defined with its Kit (`LastMooringBuilder.Meret`, `S/Combat/BossController.MeretShip.cs`, the clamps `S/Combat/MeretClamp.cs`); tested in the flight yard (`LastMooringBossTests`, Meret region). The room as built above fits it as it stands: nothing needs moving.* **Rebuild the Continuance now** (`-mooring-zones Continuance`, then `Restage.All`, `RouteFacts.Encounters`): until then the door's spawner still names the Interceptor stand-in and nothing sets `bossdown.MeretShip`.

| Thing | What the fight needs | Why |
|---|---|---|
| Her station | She holds 6 m out of her shelter's mouth along her facing (z 246), swaying across it toward the lead up to 5 m either way (x -9..1, clear of both wings by her 3 m capsule), nose on the lead. Keep the gap between the wings and 8 m in front of it free of anything solid at the plane | "Fighting from its shelter"; the boss bar reaches 34 m from her (to about z 212) |
| Her clamps | She stands her own three anchor clamps at exactly `ClampSeats` (-30, 16, 228), (-4, 16, 222), (22, 16, 228): the Kit makes them from her spawn and facing (`ShipTune.Seats`), so **if a seat moves, say so here** and the numbers move with it. Nothing to place. Raised, a clamp is solid at the plane 3 m round and its top stands 1.1 m over it. **Keep 5.6 m round each seat clear of anything solid at the plane** (a clamp's 3 m, Idra's 2.17 m hull, .4): a ship on a seat as it rises is set beside it, into open air (D289) | "Anchor clamps that reshape the lanes with visible warnings" |
| Her lanes | Broadside lanes start 4 m in front of her and run 40 m along her aim (south to about z 202); her lance 40 m; her tractor 36 m; what her shield gives back runs to the shooter and 6 m past. A risen clamp and the shelter's wings stop every one of them (cover); nothing else may stand at the plane in z 206 to 246 | Gaps between lanes are 9.2 m, wider than two of Idra's hulls |
| Camera | As built: nothing above the plane south of z 246 but her clamps' tops (1.1 m) | The flight camera is 19 m over and 17 m south of a ship |
| Her way out | `BossExit` `MeretShip` at (24, 19, 258), as built: at nought she flies to it (16 m/s, 3.2 s) and is gone; the after-talk `MeretGantry` and her body at the rail are yours | `Ending.Leaves` |
| Door and checkpoint | As built: `MeretShip`, `end.meretHailed`, `LaunchBay` | A defeat retries on foot at the bay (wiring 6.1 rule 4) |
| Adds | None. The Kit calls nothing in | |

**For the route writer (phase one).** Fly in through the door's mouth and keep within about 30 m of her. **Hold fire while her shield is up** (a dome round her, the bar reads `HER SHIELD IS UP · HOLD YOUR FIRE`; she stays locked): a shot of the lead's that reaches it comes back down a marked lane (roll out of it); the partner's only burn out. Her shield drops after her **lance** (a marked lane from her nose; about 3 s down) and while her **tractor** pulls (roll out of its line, or let it pull you in: a projector is then a lunge away). While it is down (`HER SHIELD IS DOWN · STRIKE A PROJECTOR`) lock and fire: her three projectors glow amber and take blows, as does her hull. Roll out of broadside lanes (marked, middle first) and clamp rings. She is down after about three windows' worth of blows per projector; `bossdown.MeretShip`, then `MeretGantry` plays by itself.

**Played through the pad (November 15, `MeretReplayTests`).** Fight her from the open lane in front of her shelter's mouth, about 14 m out and on your side of her middle, never from behind a wing: the wings are solid at the plane and stop every shot (the driver's braked chase went round the west wing and scraped its far side for most of fourteen rounds). A shot fired while her shield was down, or within .3 s of its rising, only burns out on the dome (`ShipTune.ReturnGrace`): what she gives back is what was fired at the raised dome. Her shield is down 4.2, 3.8, 3.4 s after a lance (it was 3.4, 3.1, 2.8), her lance is a little likelier, and her dome never stands more than 8 s before she fires through it (`ShipTune.MostUp`: on the picks alone it stood half a minute at a time, and one bar lasted 83 s in one fight and 181 s in the next). **Her shield's two relays** (`MeretRelay`, made by her Kit at `ShipTune.Relays`: about (-18, 19, 242) and (10, 19, 242), a mast in front of each shelter wing, its head at the plane and well outside her dome, nothing solid): while her dome stands each is lit cyan with a beam into it, takes blows and can be locked (the d-pad), and a shot at one is never given back. Shot through (`RelayShare`, about three seconds of one ship's fire), it goes dark for 12 s and her dome is down for her lance's time. The bar says `HER SHIELD IS UP · SHOOT A RELAY` while one is lit (and `HOLD YOUR FIRE` only while both are dark), the panel `Shoot a shield relay: her shield gives back what reaches it`. Played through the pad without them a player held fire about seven tenths of the fight and the same bar lasted 80 to 170 s; with them the lead acts nineteen twentieths of it. With the relays her lance is never rolled: it is her answer to a dome left standing 8 s (`MostUp`), so the windows come at the rhythm the player makes. A window gives up at most a twelfth of her bar (`WindowShare`, about four windows a projector): a relay's window ends there with her dome back up, and a lance's or a tractor's holds her bar (`HER SHIELD IS COMING BACK`). Her bar is 40,000. Keep 3 m round each relay's head clear of anything solid at the plane, and fly to it from the lane in front of her mouth, on its side of her middle: a shot that crosses her dome is spent on it.

## Crews contract (stream E)

*The fights' rooms as built; stream E says here which enemy stands in each and the Continuance engineer names it in the builder (`Foe(id, standIn)`).*

| Encounter | Room | Size, floor, cover | Entrances | Named today |
|---|---|---|---|---|
| `Continuance_Mess` (needs `end.boarded`) | the crew deck's mess, y 0 | box 34 x 36 on (12, 30); open floor between three long tables (solid, 1 m tall: cover for a body, not for a Warden's sweep); bunk rows west and the windows east as its sides | from the hatch (south, 10 m wide), on to the stair (north, 6 m); no seals | `CompactWarden` x2 at (14, 0, 36), radius 2.2, facing south |
| `Continuance_Works` | the anchor works, y -3 | box 52 x 54 on (-2, 142.5); the chain link on its rollers down the middle (x -3..3, z 128..156, 1.6 m tall, solid), the clamp's test stand (10, 132) and the presses' beds (west) as cover; open floor 14 m wide either side of the chain | the south door (6 m, seals as the fight begins once both heroes are 3 m in), the north door (6 m, shut until the fight is won) | `CompactWarden` x2 at (-6, -3, 152), `WeldArm` x1 at (16, -3, 158), `OverrideRig` x1 at (-20, -3, 160) |

Crews who stand aside and the one who helps are people, not enemies (`standaside`, `helpers` barks; the Warden's own `warden` bark is its definition's). If a retune gets its own id (`StillwaterWarden`), say so here and it is one word in the builder.

### What the crews need (stream E, first combat engineer, November 14)

Defined (`LastMooringBuilder.Definitions`) and tested in the yards (`LastMooringCombatTests`). Chapter 9's three are authored at 126 to 131 and fall under 9% of Idra's bar from Sync 28, so each comes back under its own id at this chapter's weight, in the same body and the same name on screen. **Name them so** (each is also `LastMooringBuilder.Enemies.<Name>`): `Foe("StillwaterWarden","CompactWarden")`, `Foe("StillwaterWeldArm","WeldArm")`, `Foe("StillwaterRig","OverrideRig")`.

| Encounter | Who | What it needs of the room |
|---|---|---|
| `Continuance_Mess` | `StillwaterWarden` x2, as named (radius 2.2 puts them 4.4 m apart: they pair, within 7 m) | **A Warden pair** braces and covers: one drives its shield down a marked line 7 m by 1.8 at a hero 2.4 to 7 m before it while the other keeps its plate to him; neither strikes across the other, so a hero **between them** is struck by neither for 1.6 s, then they step 2 m further apart. So: 10 m of open floor before the pair (the drive's 7 m and a stride), and the tables either side of that, not across it; between the pair and the hatch nothing solid within 3 m of their line, so a hero can get between them. Both stand aside at nought and walk out (a `BossExit` with id `StillwaterWarden` by the north stair would give them a door to go by; without one they walk away from the party and are gone in 3.4 s). Two at once |
| `Continuance_Works` | `StillwaterWarden` x2 (one spawner, radius 2.2), `StillwaterWeldArm` x1, `StillwaterRig` x1 | **The pair** holds the west floor as above. **The Weld Arm** never moves and welds down a strip 11 m by 2.4 **along the way it is set facing**: set its spawner `faceAsPlaced` facing south (yaw 180) down the open floor east of the chain link, so its strip lies across the east way north (the party crosses it or waits it out) and nothing solid stands on the strip's first 11 m (the seam burns 3 s after, a marked danger). At nought it stands down where it is, a machine at rest (scenery, no collider). **The Rig** discharges a 2.8 m ring round itself: set it beside a press's bed where its machine stands (no solid within 1 m of it, so a blade reaches it). So the west way is the pair's and the east the arm's, and the chain link between them is the cover. Four at once |

The flight berth has no ordinary enemies: it is `MeretShip`'s arena (her Kit may call a `StillwaterInterceptor` pair in).

## Ways in and out (wiring section 1)

| Way | As built |
|---|---|
| `Dock — The Continuance` (Last Mooring's pad, stream F) | arrives at `Spine` |
| `Launch — Last Mooring` | the pad at the spine's foot, always, drawn until `end.meretShip`; arrives at Last Mooring's `Continuance` (stream F's spawn: **owed by stream F**, so the shared exit test is red on this way until it stands) |
| The flight berth | the bay's pad, behind the works' north door (`clear.Continuance_Works`); `Dock — The Continuance` back |
| `Dock — The Anchor Chamber's frame gantry` (a way to a later zone names it: `QaPlanWorldTests`) | in the berth at the gantry's rail, drawn from `end.meretShip`; arrives at the chamber's `Gantry` (stream I's spawn: owed by stream I) |

## People (wiring section 9)

| Who | Where | Body |
|---|---|---|
| Kott, Sibb and two rescue hands | the spine's forward and aft rings, at their braces | Idra's crew bodies (`IdraNatural`, `IdraBraced`, tinted greys), as Many Hands' arrivals |
| Compact crews who stand aside | the crew deck's bunks and lockers (three), the works' west gallery (two), the command deck (one at the chart) | `CompactWarden` bodies, unarmed, drawn only, in a static box |
| The one who holds the hatch | the crew deck's stair head | a Compact body |
| A cook and a hand at work | the mess's galley counter; deck four's cutter | walkers |
| The captain of the *Steady Hand* | deck four, across the way, then aside | **`PatrolBody` owed** (wiring 11): stands in as `DessikBody` in Compact white (log `CONTINUANCE_PATROL_STANDIN`) |
| His crew (four) | deck four, beside him | Compact bodies, leaving by the east door |
| Deck hands | the launch bay, at the cutters | Compact bodies |
| Meret | at the gantry's rail, from `bossdown.MeretShip` | `MeretBody`, drawn only |

## For route writers

- **Start**: `"loadout":"chapter10"` with the main quest at step 5 (`end.gateOpen` set), zone `Continuance`, spawn `Spine` (or `Arrival`, the same place).
- **Presses on foot**: none to board: walk the spine (the crossing at (0, 0, -12.5) plays `IdraSpineBraced`, sets `end.spineTop` then `end.boarded`); the mess fight `Continuance_Mess` (two wardens) can be fought or walked past; the stair (0, ·, 53); deck four's crossing at z 86 (`PatrolLetsPass`, `end.captainPassed`; wait for the captain to step aside); the works' fight `Continuance_Works` (enter 3 m past (0, -3, 112.5); the north door opens on `clear.Continuance_Works`); walk to the pad (2, -3, 238) and press South until `docked:HullBerth`.
- **In flight**: from (-8, 19, 182) fly north through z 203 (`MeretShipToShip`, `end.meretHailed`), into the door's box (`MeretShip`, its south face at z 206); after her fall the after-talk `MeretGantry` plays by itself (`end.meretShip`); fly to the gantry's dock (24, 19, 247), press South (`Dock — The Anchor Chamber's frame gantry`, scene `StillwaterChamber`, spawn `Gantry`).
- **Fights' ids**: `Continuance_Mess`, `Continuance_Works`, `MeretShip`.
- **Flags that end it**: `end.boarded` (step 5), `end.meretShip` (step 6). The quest's step 7 is the chamber's.
- Cradles: `Spine` (spine's foot), `DeckFour` (deck four), `LaunchBay` (the bay).

## Evidence

Stills: `Builds/quality/look/continuance-04` (37 editor stills, `LookDev.Render -look-zones Continuance`, after `ZoneLook.Apply`). Four rounds were rendered and looked at; what each fixed: round 2 took the ground kit's wet patches off every deck but the working ones, lowered the key and ambient so each deck's own lamps lead, made the bunks open frames with blankets and pillows, warmed the mess tables, gave the hull's back plates of their own tone, frames, walkways, skylights and chevrons; round 3 thinned the access covers and grilles, put hazard bands before every moving machine, work cells, deck four's bays and the bay's launch lanes on the floors; round 4 a plate-by-plate patchwork in the decks' tone and a layout grid on the works' and the bay's floors.

What reads: the spine (a white gangway across open space, its rings, the two braced open with orange jacks and Idra's crews at them, the convoy under it, the hull's flank ahead with its name and its windows); the crew deck (beds in rows, the mess's warm tables and lamps, the duty board, the windows onto the convoy); deck four (the cutter up on its cradle under the hoist, its bays, the captain's line across the way); the works' presses and coolant sluices and the anchor's chain on its rollers; the command deck's chart and the anchor through its windows; the bay's empty cradle with her craft's outline; over the hull, the open launch hatch, the shelter's wings and the superstructure north of the lanes.

**Look pass (November 14, evening; the look engineer, stills `Builds/quality/look/continuance-polish-04`, all 37 under `look_density --max-blob 0.22`, worst .21).** Drawn things only: no collider, way, prompt, fight, spawn, berth or constant moved; routes 62 to 69 regenerated byte for byte the same. The floors are painted by what is done on them (`Floors`, in the deck's vertices, written as the shader takes them: linear, so the first values, a fifth darker on paper, didn't show): slate laydown bays L1 to L5 with gold corners, a deep slate press bay, the welding bay's dark steel, the sluices' wet teal, the chain's runway with its rails, a dark cross aisle to the welding line, a pale walk; teal rugs under the mess tables, the mess's warm runner, a commons rug, the muster marks before the duty board, a mat before each bunk, the bunk aisles' runners; the console well dark along the command deck's windows. The welding line is four carriages on steel rails, each with its torch down to a link section clamped on a dark steel jig, the seams hot, a cable festoon, four welders behind it. People, drawn only and where no hero walks into them: six at the crew deck's windows watching the convoy, two still at their meal (the meals on all three tables left half eaten), a second cook behind the galley counter, three asleep in the bunks, techs at deck four's benches and drive stand, an operator at each press, a hand at the sluices, officers at two consoles. Crews at work wear the Compact's coat (`PatrolBody`) or a hand's suit, crews off watch their own clothes (the civilians other zones use); the Warden's body is kept for the captain's detail. Over the hull: the lanes' working deck in dark teal-grey plates with dashed lane lines, the white hull round it in a patchwork of white, teal-grey and grey, radiator banks either side of the launch hatch, the Compact's roundel before it.

**What still reads badly** (said plainly):
- The sleepers on the upper bunks read as a lump with a head on the pillow: plausible, not lovely.
- The crews off watch are the civilians other zones use (Neve, Hal, Mira, Lume, Dace's suit), so a player who knows them may see a face from elsewhere; the Compact has no civilian grade of its own.
- The welding line's jig is still one long bed under its four sections.
- The flight berth's edges are lane lamps and buoys; nothing but those marks the berth's south, west and east edges.

## Look pass (November 21, the chapter 10 zone engineer)

After the whole-game capture of November 20 (`Builds/quality/nov20-whole/s66`; stills re-rendered from that capture's own camera positions by `LastMooringBuilder.EndingLook`, `Builds/quality/look/ch10-after*`):

- **Her shield relays** were drawn (s66/157 shows the east one locked) but as a 1.25 m ball on a thin grey pole, under half the 2.8 m their shots are judged against (`ShipTune.RelayTarget`), grey on the grey deck. The room now stands both relays' **masts** (`MeretRelay.MastName`: "Her west shield relay's mast", "Her east shield relay's mast"), at the heads' own places (`Continuance.Relays`, from `ShipTune.Relays` and her station: (-18, 19, 242) and (10, 19, 242)): a plinth in a painted gold-and-teal seat as her clamps' are, a white column banded in teal, a gold cradle under the plane, four teal lamps; nothing solid, seen before she wakes. The fight finds them by name and draws only the head (2.8 m lit, as big as its target; 2.1 m dark), a ring at the plane (2.4 m, cyan lit, amber dark) and the beam (`MeretRelay.Draw`); with no mast (a yard) it stands a plain one. What can be hit is unchanged (the 1.4 m trigger).
- **The working deck** under her lanes: every 8 m plate has a dark seam with a pale lip, a panel line, rows of fixings and here and there a service hatch or gold chevrons; the skylights are teal panes in dark frames every 6.8 m (they were bare white boxes every 3.4 m, the brightest thing over the deck, s66/154).
- **The hull's south flank** behind the spine's head (s66/013): its plates in courses of whites and the Compact's pale teal-grey, darker toward the dark under the spine, a rib every four strips; one window in three lit, the rest dark glass, each in a dark frame.
