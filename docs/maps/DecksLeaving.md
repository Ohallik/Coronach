# The Decks on separation day (DecksLeaving)

A second scene of the Decks for chapter 9 (chapter 1's `Hub_Decks` is never touched). **The place is the Decks engineer's** (`S/Editor/World/HomeBuilder.Decks.cs`): its brief, layout and coordinates belong above "For the Coronach", and "For the Coronach" is where the Decks engineer writes back what the Coronach asks for below. **The last two sections, "The Coronach" and "The launch", are the Coronach engineer's** (`HomeBuilder.Coronach.cs`, `S/World/CoronachDoor.cs`, `CoronachWalk.cs`, `CoronachLaunch.cs`, `T/PlayMode/CoronachTests.cs`), staged into this scene by the Decks' own build (`Decks.Build(Coronach.Stage)`, before its bake). Brief: [ch09-the-house-that-can-leave.md](../chapters/ch09-the-house-that-can-leave.md), beats 8 and 9. Script: [Home.yarn](../writing/pending/Home.yarn) and its [manifest](../writing/pending/ch09-manifest.md), section 1 and section 8 items 6, 7, 10 and 12. Who wires what: [ch09-wiring.md](../chapters/ch09-wiring.md).

## How it is made: a copy of Hub_Decks, changed (decision)

**Decision (November 12, the Decks engineer): DecksLeaving is a file copy of the finished Hub_Decks, changed by `HomeBuilder.Decks.Build`, not a second run of the builders.** Hub_Decks is the product of nine staging steps that each know it by name and only by name (`StationRedesign.BuildDecks`, its surfaces, `ChapterOneStaging` twice, `RoomDressing`, `MarketOpeningStaging`, `TownLife`, chapter 1's November 10 pass): running them for a second scene would mean changing every one of those shared files, and any drift between the two runs would give the player a Decks that is *almost* the one they knew. A copy is the same streets to the centimetre by construction, and every rebuild of Hub_Decks reaches the day for free: the build copies `Hub_Decks.unity` over `DecksLeaving.unity` (file bytes, so the copy keeps its own GUID), opens it, points its `ZoneController` at `DecksLeaving` and its volume at its own `Settings/DecksLeavingVolume.asset`, takes out every chapter 1 piece that talks, walks, opens or fires, and puts the day in.

- **One command rebuilds it:** `exec-when-green.ps1 -Method Lattice.EditorTools.HomeBuilder.BuildZones -Marker HOME_ZONES_OK -Graphics -TimeoutSec 1500 -ExtraArgs @('-home-zones','DecksLeaving')`. It applies its own light (`ZoneLook.Configure`), so `ZoneLook.Apply -look-zones DecksLeaving` afterwards changes nothing but is safe.
- **It is rebuilt whenever Hub_Decks is:** `[Restage.Step("chapter9.decks-leaving","DecksLeaving",After="chapter1.town-life",Chapter=9)]` on `HomeBuilder.RestageDecksLeaving` (the restage runner, `Restage.All`), after chapter 1's last Hub_Decks step.
- **Hub_Decks is read, never written.** The build fingerprints Hub_Decks before it starts and after it has saved the day (object count, a hash of every object's path, active state, local pose and components, and the file's own sha256), writes both to `Assets/_Project/Scenes/DecksLeaving-Source.txt`, and **refuses** (`DECKS_SOURCE_CHANGED`) if any of the three differ. `DecksLeavingTests` reads that record, rereads Hub_Decks.unity and fails if the file today isn't the file the build left alone.
- **What of chapter 1 goes from the copy:** the four people (Orrin, Mira, Hal, Sela), the market morning's root (`OpeningMarketSequence`, its gates, its walkway repair point, its guard box), the service job's kit and its three `FlagGate`s, `TownLife`'s whole root (its walkers and its chapter 1 and 2 `Whereabouts`), chapter 1's baked navigation, and the `Bench` on Hal's anvil. The build throws `CHAPTER_ONE_LEFT` if any `Npc`, `ScriptedWalk`, `OpeningMarketSequence`, `DiscoveryPoint`, `FlagGate`, `FlagAll`, `Whereabouts`, `WalkLoop`, `QuestStarter` or `Bench` of chapter 1's survives. **What stays:** every wall, floor, door, sign and piece of dressing; the three docks to the Halo; the four spawns; the fruit stall where the first morning leaves it (moved back the .9 m the morning moves it); Taren's yellow line; the walkway hatch's collar and its closed lid; Hal's repair console (its prompt moves to a child object so the Coronach can put it away).
- **No chapter 1 trigger can fire here:** nothing of the opening is left to fire, and every person on the day is the day's own `Npc` with its own tiers (below).
- If chapter 1's dressing moves a piece the day changes, the build stops with `DECKS_SOURCE_MOVED` naming it.

## The way in, the window, and after the launch

- **Every way to the Decks leads here from `home.treadRecovered` until `home.launched`** (`ZoneDef.standsFor="Hub_Decks"`, `standsFrom`, `standsUntil`, read by `Lattice.Core.StandIns` in `SceneFlow.Load`: one question covers the Halo's three docks, chapter 1's chart stop, any warp and a save continued from the title; logs `ZONE_STANDS_IN Hub_Decks=>DecksLeaving`). Before the window and after it, `Hub_Decks` is `Hub_Decks`. **Why `home.treadRecovered` and not `home.returned`:** `home.returned` is set by `OrrinHomecoming`, which is Orrin at his board on the day (manifest section 2): the party has to be standing in this scene to set it. `home.treadRecovered` is the flag on which the main quest's step 7 says "Go home".
- **Chart:** the day's own stop (`DecksLeaving`, "Cinder Halo", (.03, .98)) is charted from `home.treadRecovered` (`unlockFlag`), so the story's diamond never sits on an uncharted stop, and closes at `home.launched` ("Cinder Halo came apart on purpose..."). **Chapter 1's stop `Decks`** travels here during the window (by the stand-in) and from `home.launched` shows CLOSED, "The Decks came apart on purpose: their districts fly with the convoy now." (`lockedFlag`, set from `DecksStop`). Travel to a closed stop is refused with that reason.
- **The Halo's docks** (in `Hub_CinderHalo`) still name `Hub_Decks`: before the window they land in chapter 1's Decks, in it they land here. After `home.launched` they would land in chapter 1's Hub_Decks: the Halo exterior after the launch is the Ring engineer's to close (a `standsFor` on another zone, or the Halo's stop being unreachable). Open item, written to the wiring table.
- **A save made here** is a save in `DecksLeaving` at the spawn the arrival or the last cradle wrote; continued, it loads here (`DecksLeaving` is never stood in for). The Coronach engineer's launch loads the Ring on its own; a save with `home.launched` set that names DecksLeaving goes straight on to the Ring (their section, "The launch", step 5).

## The place on the day

The same three hulls (office x=-28, market x=0, repair quarter x=28), the same promenade along z=-3, the same docks to the south and service gallery to the north, deliberately changed:

- **Crates on the promenade**, low (under 1.3 m) along its camera side and stacked higher against the workshop's open front: the station's crates, Muster crates and miners' crates in each household's colour band, a cloth bundle on top, the family's number chalked on the front. None within .9 m of a declared way (`NothingStandsOnTheWays`, refusing at build time).
- **Shutters down** on the market's two stalls and the galley's counter ("GONE ABOARD" chalked on each); crates on the galley tables.
- **Chalk on the joints:** the eight separation collars Taren tightens (both ends of both bridges, promenade and service gallery; chapter 1 paints their hazard bands at x = -15.45, -12.55, 12.55, 15.45), each chalked twice along its hull side and numbered 1 to 8, and a chalk ring round the walkway hatch where the first morning's job was. Renderers are named `Chalked joint`.
- **Crawl engineers in the service hatches:** three hatches opened (lid up, striped collar, a work lamp, a cable going down): Dorrow bent over the one by the walkway's bridge, two Crawlers at the market's, one in the service gallery.
- **Families at the airlocks with what they could carry:** at gate six a grandmother sitting on the family trunk, a parent holding their place, a child with a pack; at lock nine a family with bedding and crates.
- **The fruit stall** where it always was, **Jun behind the pears**, her crate one crate past the yellow line; Mira at pitch nine among her crated lots; Jun's pear tree leaning toward the market's lamp.
- **The tug dock:** Dace and nine of his Sorrel crew at the dock rail, sitting on their kit; Marn waiting for a folding bench.
- **Lock nine:** Neve at the market's dock.
- **The way down** to the Separation Works: one floor plate of the market vestibule taken up (x 4 to 8, z -10 to -7), a lit stair going down north under the promenade, striped and railed, a gate with a lit plate and "SEPARATION WORKS" over it, chalk arrows from the market door. `Go down — The Separation Works`, locked until `home.meetingHeld` ("The town meets first: Orrin has called it in the dock office").
- **People with somewhere to be** (`The Decks carrying things`): crates carried to gate six, the galley's crates stacked, bedding carried from the cabins, parts carried out of the workshop, crates hauled at the tug dock; two neighbours arguing over a crate on the promenade; a child chalking the family's door in the cabins; the grandmother sitting on what she owns. Bodies: chapter 1 and 2's own (Aumi, Brekk, Corra, Ishen, Pell, Survivor) in their own dyes and builds, the Crawl's (Tam's and Dorrow's grades), and for Hal's six apprentices off the convoy, the convoy's (Teo, Jask, Dov, Pim, Garo, Quill).
- **Music:** `Hub Town Groove` (the zone's `musicId`). "Thinned" (the brief) has no track of its own: the Coronach engineer measured Old Village as the sparest track and plays it from the call; the day's thinning is left to the music pass (melody seeds).

## Coordinates (plan x,z; y is the deck, 0)

| What | Where | Notes |
|---|---|---|
| Spawns | `Arrival` and `Office` (-28, -11), `Shop` (0, -11), `Repair` (28, -11) | chapter 1's, unchanged |
| | `Meeting` (-28, -1.6) | the back of the meeting, at the office's front |
| | `Works` (6, -12.4) | the stair up from the Works arrives beside the gate |
| | `Lift` (28.6, 1.4) | the Saint's lift arrives by Hal's workshop |
| Docks to the Halo | (-28, -14) gate six, (0, -14) lock nine, (28, -14) tug dock | `Launch — Cinder Halo`, chapter 1's |
| Way down | gate prompt at (6, -10.15), range 3; well x 4..8, z -10..-7 | to `SeparationWorks` `Arrival`, from `home.meetingHeld` |
| Orrin | (-31.6, 7.7), facing the room | at his board; talkable until `home.returned`, then in the meeting's turns; gone at `home.halChose` (live) |
| The board | (-33.2, 9.35), slate 3.2 x 1.7 facing south | the meeting's front |
| Dock office booth | x -39.7..-36.2, z .4..7, .5 m high | stamping window on its east face at (-36.25, 2.2); two steps at x -36.2..-35.4, z 4.2..6.6; the worn step (-35.6, 5.4) |
| Oda | (-35.2, 2.2) | at the stamping window |
| Notice board | (-24.4, -7.45), facing south in the arrivals vestibule | Ilo at (-24.4, -8.9) |
| Pitch nine | stall (-9.7, -1.5); Mira (-7.7, -.9) | `PITCH NINE` sign (-7, -4.9) |
| Yellow line | x -10.6, z -5.6..-.4 | chapter 1's `Walkway safety line` |
| Jun | (-10.9, -.2) | behind her pears at (-11.25, -1.35) |
| Pear tree | (-6.4, 2.6), on a planter | `Show Tam` at (-6.4, 1.3) |
| Marked doorframe | the cabins' door, (-28, 10); marks on its west post | `Show Tam` at (-29.4, 8.9) |
| Lock nine | dock door (0, -16.2); Neve (-3.4, -12.6) | `LOCK NINE` sign (-4.4, -14.4) |
| Gate six | dock door (-28, -16.2); family at (-30.8..-34.4, -13.2..-14.5) | `GATE SIX` sign (-25, -12.6) |
| Tug dock rail | x 31.4, z -15.6..-12.4; Dace (32.3, -13.2) | crew x 33.4..39, z -15.2..-9.4; `TUG DOCK` sign (23.2, -14.6) |
| Marn's bench | `Hand over the bench` at (22.6, -10.9) | Marn at (22.6, -12.2); `PortableBenches` step 0 |
| Hal | (34.4, 3.6) | bench (32, 6); repair console (24, 6); `Fabricate — Hal's folding bench` at (38.2, 4.4) |
| Dorrow | (-19.6, -5.7) | at the hatch (-18.4, -5.3), its work lamp on the bridge side; away from `home.meetingHeld` until `home.saintReleased` |
| Tam | (14, -.9) on the east bridge | talkable from `home.returned` until `tam.goingBack` |
| Open hatches | (-18.4, -5.3), (9.8, -5.3), (10.2, 22.9) | |
| Walkway hatch | (-13.6, -3); first morning's repair point (-15, -3) | chalk ring r 1.25 |
| Join the town meeting | (-28, -.8), range 2.8 | from `home.returned`; sets `home.meetingGathered`, loads at `Meeting` |
| Ways declared | the promenade (z -3, bending to z -3.75 round the stall from x -12.6 to -6); x=-28 from -14 to 9.4; x=0 from -14 to 15; (28,-14)>(28,-3)>(30.2,1.4)>(33,2.4); (0,-11)>(6,-11.2); (-28,-8.6)>(-25.2,-8.6), ending short of Ilo, its goal; (28,-11)>(30.4,-11.2); the meeting's front (-34.4,6.8)>(-21,6.8) | `Decks.Ways`. A person a way is walked to stands at its end and past it, and that way alone is no fault of theirs (the builder's `OffTheWays` and `CoronachTests` say the same) |

## For the Coronach

*The Decks engineer writes this section.* What the Coronach reads from the nested class `Decks`, as `internal static readonly` constants (wiring section 0: the Coronach reads them and never repeats them). Plan points are x,z on the floor. For each **door**, two points: the thing itself (`at`, what its neighbour looks at) and a spot beside it where one person can stand still without touching a way (`stand`). Where a name below doesn't fit what you built, change the name here and say so; where a thing doesn't exist (the promenade has no stair), say what stands nearest.

**Written back (Decks engineer, November 12).** Every constant below is published in `HomeBuilder.Decks.cs` under these names and shapes: doors are `(Vector2 at,Vector2 stand)`, lines `(Vector2 a,Vector2 b)`, points `Vector2`.

| Constant | Value | Notes |
|---|---|---|
| `PitchNine` | at (-9.7, -1.5), stand (-8.9, .35) | the stall's north side: she faces south over it, toward the camera |
| `Board` | at (-33.2, 9.35), stand (-31.6, 7.7) | the stand is where the day's Orrin stands (he goes at `home.halChose`, live) |
| `BoltHoles` | at (32, 6), stand (34.4, 3.6) | four holes at (32±.7, 6±.45), shown from `home.halChose` (load); the nine pieces lie in a 3x3 row at x 35..37.4, z 5.4..7.8 straight after Hal's scene, and in a crate at (36.2, 6.6) from the next load |
| `HalsDoor`, `HalsDoorWidth` | (31, 0), 8 | the workshop has no door: its whole front is open to the promenade along z 0 |
| `LockNine` | at (0, -16.2), stand (-3.4, -12.6) | the market's dock (the district Neve flies first) |
| `StampingWindow` | at (-36.25, 2.2), stand (-35.2, 2.2) | on the dock office booth's east face; the booth is new on the day (a raised dock office), with two steps up beside it |
| `YellowLine` | a (-10.6, -5.6), b (-10.6, -.4) | |
| `PearCrate` | (-11.25, -1.35) | |
| `NoticeBoard` | at (-24.4, -7.45), stand (-24.4, -8.9) | in the arrivals vestibule, on the promenade wall's south face |
| `DockRail` | at (31.4, -14), stand (32.3, -13.2) | the rail's ends are `DockRailEnds` (31.4, -15.6) to (31.4, -12.4); it is on the **tug dock** (the repair quarter's), where Dace's crew are |
| `GateSix` | at (-28, -16.2), stand (-30.6, -14.2) | the dock office's dock |
| `WalkwayHatch` | (-13.6, -3) | and `WalkwayRepair` (-15, -3) |
| `DockOfficeDoor` | (-38, .2) | the booth's south-west corner, facing the promenade (the office has no door: it is open to the promenade along z 0) |
| `WornStep` | (-35.6, 5.4) | the stair Tam is shown: the dock office booth's lower step. The promenade has no stair; this is the only one on the Decks |
| `Ways` | `Vector2[][]`, as the coordinates table above | |
| `ChalkedJoints` | `(Vector2 a,Vector2 b)[]`, eight: x ±12.55, ±15.45 at z -5.7..-.3 and 19.3..23.7 | each drawn as two `Chalked joint` lines .3 and .48 m to its hull side |
| Spawns | `Arrival`, `Office`, `Shop`, `Repair`, `Meeting`, `Works`, `Lift` | |

- **Who goes at the call: done.** Each of the day's Npcs for Mira, Hal, Neve, Oda, Jun, Ilo, Dace, Dorrow and Tam has a live `Whereabouts` with `goneAt="home.coronachCalled"`; Orrin's has `goneAt="home.halChose"` (live). Hal's folding bench (`Bench`) and his repair console's prompt (`RepairBay`, on its own child object) go at `home.coronachCalled`, live; Mira's shop is her talk, so it goes with her.
- **The day's walkers: done**, under `The Decks carrying things` (five errands).
- **`Market district` and the yellow line in halves: not yet** (see the end of this section when it is).

| Constant (suggested) | What | Why the Coronach needs it |
|---|---|---|
| `PitchNine` (at, stand) | Mira's stall, crated. She stands behind it if there is room, so the camera sees her face | Mira's door |
| `Board` (at, stand) | Orrin's board at the dock office, where the town's news is chalked | Orrin's door |
| `BoltHoles` (at, stand) | the four bolt holes in Hal's workshop floor where the bench stood (shown from `home.halChose`) | Hal's door |
| `HalsDoor` (centre, width) | the workshop's doorway onto the promenade | the call is a crossing just outside it |
| `LockNine` (at, stand) | the airlock Neve flies from | Neve's door |
| `StampingWindow` (at, stand) | the dock office's window where berths were stamped | Oda's door |
| `YellowLine` (a, b) | the two ends of the painted safety line at the walkway end of the stall: the line chapter 1 opens on | Jun's door; the line the promenade parts under at the launch |
| `PearCrate` (at) | Jun's crate of pears, one crate past the line | Jun's door |
| `NoticeBoard` (at, stand) | where Ilo pinned the capacity line | Ilo's door |
| `DockRail` (at, stand) | the dock rail on Dace's side | Dace's door |
| `GateSix` (at, stand) | the departure gate Sela missed | Sela's last word |
| `WalkwayHatch` (at) | the walkway joint's service hatch from the first scene (chapter 1: the cover at (-13.6, -3), the repair point (-15, -3)) | Taren's last word; where Orrin waits to close the walk |
| `DockOfficeDoor` (at) | the dock office's door to the promenade | Pell lights a lamp over it |
| `Ways` (polylines) | every way the Decks declares clear (the ones its scatter and its own people keep off) | the Coronach's people keep off them too, and the test checks it |
| `ChalkedJoints` (a, b pairs) | the chalk lines on the joints the brief puts on the day ("chalk lines along the joints"), with their renderers named `Chalked joint` | at the launch each brightens in turn, farthest first, before the walkway's lets go |
| `Arrival` and every other spawn | as built | a save mid-walk loads at one of them |

And two things in the scene itself, by name:

- **`Market district`: no longer needed** (Coronach engineer, November 12, after reading the copy). Chapter 1's hulls are separate pieces, so the Coronach's `Stage` takes the market as everything whose whole extent stands between the market's two collars (the chalked joints at x -12.55 and 12.55): its hull's floors, walls, keel and casing, its dressing and lamps, and whoever stands in it. The walkway bridges reach past the collars and stay. It takes `BatchingStatic` off those (their other static flags stay, so they are still in the bake) and moves them together at the launch; anything flat laid across the parting is torn (hidden as the joint lets go). **The parting is the market's west collar, 1.4 m short of the yellow line, so the line Taren painted lifts away with the market**; no halves. Please keep anything of the day that should stay with the office side from reaching past x -12.5.
- **Who goes at the call.** From `home.coronachCalled` the neighbours stand at their doors as the Coronach's own people, not as Npcs: please give each of the Decks' own Npcs for Mira, Hal, Neve, Oda, Jun, Ilo, Dace, Dorrow and Tam a `Whereabouts` with `goneAt="home.coronachCalled"` (live). **Orrin's goes at `home.saintReleased`** (changed: the Coronach's Orrin waits outside Hal's workshop from the Saint's release, giving Hal room, and calls the walk as the party comes out; one exact edit in `HomeBuilder.Decks.cs`, `HalChose` to `SaintFreed` on "Orrin at his board"). Anything that opens a shop, a bench or a repair goes with them: nothing jingles during the walk. The call is made in front of Hal's workshop, so the Coronach's Hal takes over from the day's Hal where he stands, in view, and turns to his bolt holes; everyone else arrives at their door out of the party's view.
- **The day's walkers.** The people carrying crates to the airlocks are what the walk thins out as doors are said. Put them under one root named `The Decks carrying things`; the Coronach takes them away a few at a time, out of sight, as the doors are heard. Nothing else of yours changes.

## The Coronach

*Design, written before building (November 12). The Coronach engineer's. Places are named by the constants above until the Decks has built them; coordinates follow once they exist.*

**What it is.** Story beat 8: before the last joint opens, the neighbours walk the town and say, out loud and to their own door, what the place did for them. It is the scene the game is named for. **No combat, no timer, no counter, no enemy, no bark, no shop, no arrow.** The panel's line is the main quest's step 12, "Walk the town. There is no hurry." (an exact edit in `HomeBuilder.cs`: the step targets the zone and nothing in it, so no arrow points at a door; wiring rows corrected to match). The music is its own.

**The one visual rule: a lit lantern set down at someone's feet means somebody here still has something to say.** Every door's neighbour, and each hero's last word when its time comes, has a small hand lantern (a Wick lantern: the atlas makes Lanternwick the custom's origin) set on the deck between them and what they are facing, with a warm pool of light round it. The person stands still and looks at their door, never at the party; everyone else in town is carrying things toward the airlocks. When their words are said, they put the lantern out. Later, out of the party's sight, they pick it up and go aboard, and what they faced is shut behind them. So: **lit lantern, someone waiting; dark lantern, said, still standing with it; nothing, gone aboard.** Guests (people from elsewhere) have no door and no lantern: they stand still at the edge, watching, and say a line if the party stops by them.

**How a door is heard.** Coming within about three metres puts up the prompt `Stop and listen` (every door and guest; `Say yours` for the heroes' three). Pressing it plays the door's node; the hero turns to the door, the partner comes to stand beside, and the neighbour does not turn round. Nothing starts by walking past: a player who never stops hears nothing, and loses nothing. Each door is a `DiscoveryPoint` whose flag is the world's (`coronach.<who>`: manifest section 3), with no report and no bark, so every one saves when heard.

### The call

`OrrinCallsCoronach` is a crossing along the whole open front of Hal's workshop, offered from `home.halChose`: Hal's scene ends, the party steps out onto the promenade, and Orrin is standing there, where he has waited since the Saint was let go, giving Hal room. He calls it (`home.coronachCalled`), then walks to his board along the town's ways. The Decks' own Orrin goes at `home.saintReleased` (decided at load: the party comes up from the Works), so there is only ever one of him.

### The doors, and why each is that person's

| Who | Door | Stands | Faces | Why this one | Afterwards (out of sight) |
|---|---|---|---|---|---|
| Mira | the stall, pitch nine, crated (`PitchNine`) | behind it, where she sold from | the stall | four planks and an awning she never paid for: the place she argued over money for eleven years | gone aboard the market district; the stall's awning goes with her ("I'm keeping the awning") |
| Orrin | the board (`Board`) | in front of it, chalk in hand | the board | everything the town knew, it knew off his board; his last line on it is "departed, all sections" | he does not go aboard: he walks to the walkway joint to wait for the last of it |
| Hal | the four bolt holes (`BoltHoles`) | over them, looking down | the floor where the bench stood | the bench was the one thing that made people hold still; it is in nine pieces in a crate | gone aboard the repair quarter; the workshop's shutter comes down |
| Neve | lock nine (`LockNine`) | at the lock's inner door | the door | four thousand times out and four thousand times back in: a lane to come home down | gone through it |
| Oda | the dock office's stamping window (`StampingWindow`) | at the window | the window | line nine hundred and four was stamped there, and is being framed | gone; the window's shutter comes down |
| Jun | the yellow line (`YellowLine`), her crate of pears one crate past it (`PearCrate`) | at the line, on the market side | the line on the floor | thirty-one years of being a little over; chapter 1's first scene, answered | gone aboard the market, with the crate |
| Ilo | the notice board (`NoticeBoard`) | in front of it | the board, four pin holes | he pinned the worst thing he ever posted there, and is keeping the pins | gone |
| Dace (not needed) | the dock rail (`DockRail`) | at the rail | out through the dock | he never lived here, and says it anyway | gone |

Seven are needed; Dace's is not (manifest). The order is the player's: any door, any time, none of them waits on another.

### The guests

Pell under a lamp she has lit over the dock office (`DockOfficeDoor`); Ruva by the fruit stall (Jun's end of the stall); Maru at the walkway's end; Hask across from the board; Tovra by Hal's workshop door; Dorrow by a chalked joint, goggles pushed up, listening to it; Abri by the notice board, writing. One line each (`PellEdge` ... `AbriEdge`), all optional. A guest whose side story was never done is still there, saying their own line.

### The extras

Thirty-two one-line nodes, one for each side quest finished since chapter 1 (manifest table). Each plays **straight after its speaker's own line, in the same stop**: the door's node ends and the extra begins in the same frame, so there is no gap, no second prompt and no list. Someone whose only lines are extras (Ishen, Brekk, Binna, Jask, Teo, Dov, Corra, Iru, Pip, Tam) stands at their place **only if at least one of their stories was finished**, and is then a person like any other there: `Stop and listen` plays their first, and the rest follow. Where they would stand is ordinary town without them (a crate is a crate, a rail is a rail), so an unfinished story leaves nothing standing empty and nothing set out for nobody.

Three extras are things seen or heard that somebody else reads (manifest 8.7):

- **The gardener** (`RuvaDoorGardener`): the Skyfields' gardener stands beside Ruva by the fruit stall, its light turned low. Without `EmptyGardens` it isn't there.
- **The bell** (`DovDoorBell`): one stroke of the school raft's bell is heard, once, the first time the party comes within about seven metres of Dov; Dov's line says what it was. Never twice. Without `SchoolBell` it never rings.
- **The table** (`MiraDoorTable`): as Mira's stop begins, the Arrens come out of a doorway carrying the wedding table's top in two halves, one each, and walk past the stall toward the market's airlock and are gone. Mira's line names them as they go by. Without `SecondInventory` they never come out.

Beside the people: Mira's lantern has six Ridge Crystals in it (`MiraCrystals`); Pell's lamp over the dock office is her mother's (`PellsLamp`: the same lamp either way, so nothing is missing without it), with the Oriel flame beside it (`LumesLantern`) and Iru with one of her own (`ShrineLamps`); Maru's two lamps at each end of the walkway count two short, one long (`BeaconKeeper`); Kott and Sibb stand in view of Idra (`CutterDown`); three of Tovra's smiths stand with their young pikeguards (`QuietHands`); Brekk stands with Tovra (`UnswornRations`, `OdrunsKitchen`), with a kettle if Odrun sent one; Tam stands with Dorrow (`StillGround`).

### The last three, and a resting hero

When the seventh needed door has been heard (`coronach.doorsHeard`, a `FlagAll`), three more lanterns are lit: Taren's at the walkway joint from the first scene (`WalkwayHatch`), Sela's at gate six (`GateSix`), and Idra's in the open street where she has been standing, because she has no door. `Say yours` at each plays `TarenWalkway`, `SelaGateSix` or `IdraNoDoor`, in any order.

**All three heroes are in the street for it.** The two the player has out walk it as the party; the one resting stands at their own place from the call onward (their natural body, still, facing their door, with no lantern until the seventh door is heard), so the town sees all three. When a hero who is out says theirs, the partner comes to stand beside the lead and both face the door; when the hero resting says theirs, they say it where they stand, by portrait. If the player changes who rests during the walk, a hero brought out leaves their place at once (there are never two of them) and a hero sent to rest appears at their place only when it is out of sight.

### What changes as the doors are said

The street at the call is busy: the day's walkers carrying crates to the airlocks, a lit lantern at every door. Each door heard puts its lantern out; when the party has moved on and the person is out of sight, they and their lantern are gone and what they faced is shut (a shutter down, an awning gone). The walkers thin out in step with the doors heard, also only out of sight. By the seventh door the streets are empty but for the guests nobody stopped for and the three heroes' lanterns; by the close it is the heroes, Orrin, and whoever is still to be stood beside. Nobody vanishes in front of the party.

### The close

Orrin, after his own door, walks to the walkway joint and waits on the office side of the line. Once the three last words are heard (`coronach.lastHeard`, a `FlagAll`), his lantern is lit there; `Stand with Orrin` plays `OrrinGiveItRoom` ("Give it room"), which sets `home.coronachWalked` and saves. **The walk ends only when the player chooses**, standing with Orrin at the line: never on a timer, never by itself as the last word ends, never with a door still in sight unheard by the player's choice. No reaction to "Give it room" is staged (manifest).

### Music

The Coronach's own piece (`coronach-walk-a`, docs/music/NEW_TRACKS.md cue 1) doesn't exist yet. The walk plays **Old Village** until it does, over everything, from the call to the close; the launch plays **Title Theme** (cue 2, "The House That Can Leave", is its own and also not made yet). Both are named on the zone's `ZoneDef.cueMusicIds` (so `MusicIntake.Stage` stages them) and on the components, from **one line** in `HomeBuilder.Coronach.cs` (`WalkMusic`, `LaunchMusic`): the swap is that line, once the new track's row is in `tracks.json`.

Why Old Village: it is the sparest track the game has, measured (the MP3 masters decoded and counted, `tools/` not committed: scratch): 2.9 onsets a second against Hub Town Groove's 5.1, the least high-frequency attack of the twelve calm tracks (no hats or shaker driving it), a third of Hub Town Groove's bass energy, and the darkest spectrum. The day's music is Hub Town Groove, so the walk gets quieter when it begins, as it should. And it is Lanternwick's tune, the town the atlas makes the custom's origin. Why Title Theme for the launch: it is the game's own theme, heard only on the title and at the end, and this is the moment the game is named for opening into motion; it is steady and full where Old Village is sparse.

### Silence

From the call until the cut to the Ring nothing barks (`BarkService.Hush`: an arrival on loading a save mid-walk, a swap, a place's quip), no shop can be opened (the Decks' own people with counters have gone at the call), and the music cue outranks everything but a shop. After `NeveFirstOut`, Orrin's first `hail.section` ("Market district is away. Mira wants a receipt.") is the one radio line before the cut; the Ring plays the rest.

### Saves

Every door, guest and last word is a discovery point: hearing one saves. A save made mid-walk loads into the walk at the zone's spawn with every door heard still heard (its person gone aboard, its lantern gone, its door shut), every door unheard still lit, the three last lanterns lit if the seven are heard, and the music and the hush on. Extras are not saved separately: they play with their door, and a door heard has had its extras.

### State on November 13 (paused with chapter 9)

**Designed:** everything above and "The launch" below (D269). **Built:** `CoronachDoor`, `CoronachWalk`, `CoronachLaunch` (S/World, guide 15.29); `BarkService.Hush`, `ZoneDef.cueMusicIds` and its `MusicIntake` line; the four synthesised sounds (`tools/coronach_sounds.py`, unheard); `HomeBuilder.Coronach.Stage`, which stages the whole walk and launch into DecksLeaving from the Decks' published places. DecksLeaving builds with it (`HOME_ZONES_OK`, November 13 07:44: 30 places, the town's 5 walkers, the Decks' `Market district` plus the walk's 7 market places to lift, `CORONACH_WAYS_CLEAR`, the bake reaches every point); the tree compiles. Exact edits outside this file: step 12's text and target in `HomeBuilder.Quests`; the Decks' Orrin goes at `home.saintReleased`. **Tested** (`T/PlayMode/CoronachTests.cs`, mirror C, November 13 08:56, `Builds/quality/focused/home-coronach-playmode.xml`): 16 run, 12 passed. Passed in the yard: the lantern rule, nobody set out for an unfinished story (and the bell once), a resting hero at their place, the walk's music and hush, ten idle minutes, the launch to the next zone's load. Passed in the zone: the call at Hal's front, the stories exactly when finished, no bark and only the walk's music, a save mid-walk, ten idle minutes, a save before the launch's talk. **Failed:** (1) the dialogue box closes for 3 frames between a door's line and its extra (the extra starts a frame or more after the door's conversation lets go; fixing it needs a follow-on node in `DialogueSystem`, a shared file); (2) Oda's door at the stamping window has no walkable floor within its reach in the bake (a needed door: must be fixed first); (3) Ilo stands 0.3 m from the end of the way to the notice board (the Decks' own stand; decide whether a door at a way's end is allowed, and make the builder's check and the test's agree: the builder passed it); (4) the launch-to-Ring test threw a `MissingReferenceException` (a lifted part destroyed mid-launch; not yet read). The Broken Ring isn't built, so the cut itself is untested in the zone. **Not seen:** the stills the zone tests wrote (call-before, call-after, launch-1-joint, launch-2-lift, launch-3-away, in `..\CoronachTest\C\Builds\quality\focused\coronach\`) have not been opened; the door and street stills weren't written because that test failed at Oda. **Next:** fix (2) and (4), decide (3), propose (1) to the lead; open the stills (each door with its speaker, the street at the start and near the end, three moments of the launch) and judge them against CHAPTER_GUIDE 0.1; render LookDev for DecksLeaving to check the walk's switched-off people don't show in the day's stills; read the walk aloud at walking pace; then the Ring's arrival when it exists.

### State on November 14 (both engineers' parts, one engineer)

**Fixed since November 13.** (1) *The box between a door and its extras*: a door's extras are asked, as its talk opens, to follow it in the same box (`Npc.SayNext`, `DialogueSystem.Follow`: the end of a followed talk leaves the box up and the party held until the next one's first line; refused with nothing running; dropped with a talk cut short; nothing changes for a talk nobody follows). (2) *Oda's door*: the Decks' bake had the meeting standing, so its forty people and six benches stayed holes in the dock office's floor all day; the meeting is out of the bake and carves its own while it stands (`NavMeshObstacle`, carving). (3) *Ilo at the notice board*: a person a way is walked to stands at its end and past it, and that way is no fault of theirs (`OffTheWays` and `CoronachTests` read the same rule, and the test's ways are now the builder's, point for point). (4) *The launch to the Ring*: the test read the market's parts after the cut had unloaded the scene; it now holds Neve's talk on its first line for the stills, reads the rise while the market is there, then lets the talk go to the Ring. (5) Mira's three talks on the day are not branched (no choice; `DialogueInputTests` reads their shop ending from the lines). (6) The restage step is `From="Hub_Decks"`. **From the zone's first run of the shared cases** (mirror A, 03:41, on the 03:36 build): chalk under the type floor (the board's order is five short lines at 24 px, its answer "BERTH: DECLINED", GONE ABOARD and ARREN at the floor, the front bench's crate "HOLD"); the day's name labels built at 26 px (Jun's and Tam's stood at 16); Dorrow inside a crate stack and his hatch's lamp (the stack is gone, its seed still drawn, and the lamp is on the bridge side); the walk's crates set beside their people clear of anyone (Binna's stood round the day's Jun); the halves of the yellow line not static-batched (a batched quad read as 34 m of stretched paint); the way down's plate and lamps hung on its gate; Mira's crystals taken out of the bag in the let-go test (with six aboard her job finished and paid inside the measure); the walk's people kept off floor signs (Abri stood on NOTICE BOARD) and in the same room as what they stand by, the heroes placed before the guests (a guest took Taren's room at the joint and pushed him past the line); and three exemptions for a station deck (`FloorAndWallMeetInShade`, `CliffBandsRunLevelAndTheCanyonIsNotStretched`, `EachNaturalPlaceHasItsOwnAir`). A build that fails now puts the last built day back (one left DecksLeaving.unity a byte copy of Hub_Decks).

**Built:** 04:09 (`HOME_ZONES_OK`, 30 places, only Iru placed far; the bake reaches all 23 points; Hub_Decks unchanged), and `Restage.All -restage-scenes DecksLeaving` after it (`RESTAGE_OK`, the scene unchanged by it). **Not run, Unity's licence lapsed at 04:14:** `RouteFacts.Encounters`, `ZoneLook.Apply`, `LookDev.Render` of this build. **Tested:** on the 03:36 build, CoronachTests 17 of 17 and DecksLeavingTests 9 of 10, and 7 of the zone's shared cases failed. On the 04:09 build (mirror A, label ch9-dc-2, 85 cases): CoronachTests 17 of 17, DecksLeavingTests 9 of 9, DialogueInputTests 26 of 26, every DecksLeaving shared case but one. **Still wrong:** `NoWalkableEdgeDropsIntoNothing("DecksLeaving")` finds walkable edges with no floor beyond at (-13, -7) and (-13, 1), new in this build, on the walkway bridge where Taren's and Ruva's places now stand. Not yet read; the likely cause is that the walk's bodies, hidden until the call, are baked as holes in the paths (as the meeting's were): bake them out and let them carve while shown. `EveryConversationLockAndQuestStepIsWiredToSomethingInTheWorldAcrossZones` fails on other zones' content only (`home.rideDone`, nodes not yet in the script). **Seen:** LookDev `Builds/quality/look/ch9-dc-1` (03:36 build) and the walk's stills from mirror A (`..\CoronachTest\A\Builds\quality\focused\coronach`). **Unheard:** the four synthesised sounds. **Music** is still the stand-ins (Old Village, Title Theme): the list is the creator's.

## The launch

*The Coronach engineer's. Story beat 9: the districts lift, the promenade parts, Neve flies the first one out. On the fixed three-quarter camera, with the game's own camera, clock and effects.*

1. **"Give it room."** `OrrinGiveItRoom` ends (`home.coronachWalked`). The party is held (input blocked; no menu, no swap) for the launch's length, which is short: anyone standing on the market side of the line, or within two metres of it, walks back to the office side beside Orrin and turns to face the line (three seconds at most; anyone the step back can't reach is set there).
2. **The joints open** (about four seconds). The chalk lines on the joints in view brighten in turn from the farthest to the nearest, each with a short hiss of vented air; then the walkway joint itself lets go, the market's own collar at the walkway's end, a step short of the yellow line: a deep release, steam along the seam, a 150 ms hold of the clock (`Impact.Hold`), a heavy tremor of the view (`CameraRig.Tremor`) and the pad's low motor. Anything flat laid across the seam tears (is hidden).
3. **`NeveFirstOut`** begins: "Market district. On my word. Now." On its own clock, from a beat after the talk opens, **the promenade parts at the yellow line** and the market district lifts: its floor with the line on it, its stall bays and its people (everyone who went aboard is aboard it; a guest still standing in the market goes up with it) rise and draw away to the east, the lit ports along its casing rising past, its keel and three engines burning under it, and space showing through where it stood. The view draws back to take it in above the dialogue box (`CameraRig.FrameGround`, outranking the conversation's framing) and holds there. A low engine swell under it.
4. Mira, from aboard: "I felt that! Did the walkway just move?" Taren: "All of it. On purpose." Meret's one radio line of the launch, then Neve's last (`home.launched`).
5. **The cut.** Orrin's radio line ("Market district is away..."), two and a half seconds more of the district going, and the load into the Broken Ring (`SceneFlow.LoadZone("BrokenRing","Arrival")`), whose own arrival saves. A save made before step 5 replays the launch from step 1 on loading; the Decks entered with `home.launched` set (it can't be from the chart: the stop closes) goes straight on to the Ring.

Nothing in it is longer than it has to be: about four seconds before the talk, the talk at the player's own pace, two and a half seconds after it.

## For route writers: the Coronach walk, press by press

*November 14, from the built scene (prompt positions read from DecksLeaving.unity; each prompt is offered within its range on the floor plan, D272, and the nearest offered prompt wins). Through the walk the Halo's three docks, Hal's folding bench and console and Tam's still things are shut (`CoronachWalk.shut`), and from anywhere 1 to 1.75 m off a door, a last word or the close nothing else can be pressed within a metre of it (`CoronachTests.EachDoorLastWordAndTheCloseIsThePromptOfferedAllRoundIt`; the build refuses a layout that breaks it, `CORONACH_PROMPTS_CROWDED`). The guests and the people there for their stories are spoken to where they stand, within 2.2 m. From each stand below nothing else is in reach at all.*

**Starts** in DecksLeaving after Hal's scene: `home.saintReleased` and `home.halChose` set (the party comes up the Saint's lift, spawn `Lift` (28.6, 1.4), and hears Hal at his bench), main quest `HouseLeaves` at step 12 ("Walk the town. There is no hurry.", no arrow). The flags up to here are `CoronachTests.UpToHal`. Orrin stands at (35, -1) from the load.

| # | What | Press | Stand at (x, z) | Prompt at, range | Sets | Note |
|---|---|---|---|---|---|---|
| 0 | The call | none: walk out of the workshop across its open front (any x 16.5 to 39.5, z from 1.1 to -1.1), e.g. (31, 2.2) to (31, -1) | | trigger along z 0 | `home.coronachCalled` | `OrrinCallsCoronach` plays by itself; Orrin then walks to his board, about 40 s |
| 1 | Hal | Interact "Stop and listen" | (32.0, 3.2) | (33.27, 4.73), 3 | `coronach.hal` | Hal takes over from the day's Hal at once |
| 2 | Neve | Interact | (-1.7, -12.4) | (-2.30, -13.76), 3 | `coronach.neve` | lock nine's dock is shut through the walk |
| 3 | Jun | Interact | (-11.0, -4.3) | (-11.06, -5.94), 3 | `coronach.jun` | she stands at the line's south end, by the promenade's wall, and is spoken to where she stands |
| 4 | Mira | Interact | (-9.85, -3.8) | (-9.70, -1.50), 3 | `coronach.mira` | the Arrens carry the table past if `SecondInventory` is done |
| 5 | Ilo | Interact | (-25.4, -8.0) | (-24.40, -7.45), 3 | `coronach.ilo` | |
| 6 | Oda | Interact | (-34.4, 3.0) | (-36.25, 2.20), 3 | `coronach.oda` | |
| 7 | Orrin at his board | Interact | (-31.0, 9.0) | (-32.71, 8.85), 3 | `coronach.orrin`; with the six above, `coronach.doorsHeard` | offered only once he stands at his board (`stopWhen` his prompt); he then walks to the last joint |
| 8 | Taren's last word | Interact "Say yours" | (-13.6, -3.6) | (-13.60, -3.00), 3 | `coronach.taren` | the three last words open on `coronach.doorsHeard`, any order |
| 9 | Sela's | Interact | (-29.9, -14.9) | (-29.33, -15.18), 3 | `coronach.sela` | gate six's dock is shut through the walk |
| 10 | Idra's | Interact | (-32.44, -2.8) | (-32.44, -3.43), 3 | `coronach.idra`; with 8 and 9, `coronach.lastHeard` | she stands across the street from the office, at (-32.44, -5.03) when resting |
| 11 | The close | Interact "Stand with Orrin" | (-13.6, -3.6) | (-13.97, -4.24), 2.4 | `home.coronachWalked` | offered once `coronach.lastHeard` is set and Orrin stands at (-15.42, -4.77) |
| 12 | The launch | none for about 7 s (the party is held), then press through `NeveFirstOut` | | | `home.launched` | about 2.6 s later the zone loads `BrokenRing` at `Arrival` (`ZONE_ENTER BrokenRing`) |

**The walk's own ways** (`HomeBuilder.Coronach.DoorWays`, November 14): four doors stand off the Decks' ways, and each is walked to along a way of the walk's own, from a Decks way to the stand above and back the same way: across the dock office's front south of its crated benches, (-28, 1.6) > (-32.7, 1.6) > (-34.4, 3.0), to Oda; up the office's east side, (-30.6, 6.8) > (-30.6, 8.3) > (-31.0, 9.0), to the board; (-28, -13.8) > (-29.9, -14.9) to Sela at gate six; (0, -11) > (-1.7, -12.4) to Neve. Nobody of the walk stands within 1.7 m of them, as of the Decks' own (the door's own person, at the end and past it, is its goal); the build stops (`CORONACH_DOOR_WAY`) if one doesn't leave a Decks way or doesn't end inside its door's reach on clear floor, and `tools/home_routes.py` reads them and walks them. Without them the office's front was nobody's way: the doors' prompts pushed Jask 4.4 m off his stair onto it, at (-31.12, 1.52), and the first chain to play the walk stopped against him there on its way to Oda and for good on its way back to the board (route 60, November 14). He stands by the stair now, at its top end past the booth's north-east corner, (-36.7, 8.0), facing it: the nearest floor to it four metres off Oda's window and Orrin's board. `HomeRouteTests.EveryWalkOnFootEndsWhereAHeroHasRoomAndMeetsNothingSolidOnTheWay` feels every leg of the chapter 9 routes with a hero's capsule, the walk loaded with every side story done (everyone who can stand in it does), and `home_routes.py` keeps every leg a body clear of the walk's people and things as built (`coronach_solids`).

Dace's door (31.40, -14.00), the guests and the people there for their stories are optional; none of them is needed for a flag above. Every door saves when heard (autosave), and a talk's extras follow in the same box with no press of their own beyond their lines. Logs a route can wait on: `CORONACH_HEARD <name>`, `GUIDE_LEG 1` (Orrin sets off for his board), `LAUNCH_BEGIN`, `LAUNCH_JOINT`, `LAUNCH_LIFT`, `LAUNCH_CUT BrokenRing`.
