# The Scoria Flows - Scoria, between the Picket's landing and the Bridle Foundry

Functional brief, written before construction (November 12, chapter 8's second world engineer, Claude). Chapter brief: [ch08-harrowgate.md](../chapters/ch08-harrowgate.md); script wiring: [ch08-manifest.md](../writing/pending/ch08-manifest.md). Builder: `S/Editor/World/HarrowgateBuilder.Scoria.cs` (everything nested in `HarrowgateBuilder.Scoria`). The zone's mechanism: `S/World/CrustField.cs` (guide section 15.24), with its one hook for combat in `S/Combat/GroundHeat.cs`. Tests: `T/PlayMode/ScoriaTests.cs`. Review status is at the end, and it is an agent's review.

## Purpose and formation

The lava deltas below the Muster's foundry. Scoria's flows come out from under the scoria hills on the east and run west and south-west across a valley of basalt shelves. Where a flow spreads thin it crusts over, and the crust breaks and re-forms on a cycle as the melt under it surges: that is the zone's one rule, **the crust tells you**.

- **Dark crust is safe.** Near-black slabs, fitted tight, their joins dark.
- **Glowing seams are about to go.** The joins between the slabs light, deep red to hot yellow, over about two and a half seconds; the slabs tremble in the last second; the crust groans.
- **Bright flow hurts.** The slabs sink and the melt is open, pale yellow-white and moving. A hero who stands on it is burnt (a share of integrity, never the last of it) and thrown back to the last ground that doesn't cycle. Then the crust rises again, dark, with a hiss of steam.
- **What just moved under it.** Where an Ash Crawler has gone under, the crust over it is heaved up into a blister that breathes a dull glow and puffs ash. A ring (the ambusher's own) says how near is too near.

The Muster holds the valley's crossings with garrisons of conscripted clan soldiers: a basalt causeway the garrison built over the first flow, a mortar battery on the shelf above the second field, and the gate garrison on the Foundry road. Between the second and third crossings, up a side valley to the east, the **Unsworn camp**: deserters and the conscripted smiths' families, under Tovra Kesh. Ash squalls come down the valley without warning and close the view for a few seconds.

## Users and access

The party lands from the Picket of Teeth on a basalt shelf at the valley's south end. Brekk Tamar, who learned to walk here, leads them across the first two flows and stops at the garrison's causeway; after it he goes on ahead to the camp, where he is put on the stall. The garrisons are clan conscripts in Muster gunmetal (pikeguards, mortar crews, a sergeant). Ash Crawlers, the native heat-scavengers, live in the crust of the delta and the cinder hollow.

Ways in and out:
- **The landing** (south): the party's ships on a landing pad; `Launch — The Picket of Teeth` goes back up.
- **The Foundry road** (north): the gate in the north wall, past the gate garrison; shut until the party has met Tovra (`met.Tovra`).
- **The Harrowgate road** (east, from the camp): shut until Tovra has sent them to the town (`vask.roadSaid`).

## Layout

About 100 m west to east and 270 m south to north. x runs east, z north. The fixed ground camera (pitch 40, yaw 20, 19.5 m) looks north-north-east and sees about twelve metres north of the party and five south: **every flow is crossed going north**, so the plates a hero is about to step on are the top half of the frame, and every crossing is read from its south bank. The south and west walls are low (the camera's foreground); the hills to the north and east stand tall, and the flows come out from under them.

```text
z 262  ######## north wall ######## [FOUNDRY GATE] ##################
z 250              Foundry spawn . gate garrison (Scoria_Gate) . kettle stone 3
z 222  ~~~~~~~~~~~~~~~ F9 the last tongue (a wave, east to west) ~~~~~~~~~~~
z 210  junction ===== camp road =====> UNSWORN CAMP (safe pocket)  ==> [HARROWGATE ROAD]
z 196      the sled (cache) .. mortar shelf: battery, kettle stone 2        Tovra, stall, bench, cradle, forge
z 180  ~~~~~~~~~~ F7 the battery's field (plates on their own clocks) ~~~~~
z 158                 north shelf (squall front)
z 150  ~~~ F6 THE DELTA: a tide from north-east to south-west ~~~~~~~~
z 138    glass island (cache)        . island B .
z 124                       . island A .         crawler hides on the plates
z 102                 south shelf (squall starts here)       F4 > CINDER HOLLOW (crawlers, cache)
z  84  ~~~~~ F3 ~~~~~ [causeway] ~~~~~ F3 ~~~~~  garrison: pikeguard pair, kettle stone 1
z  66                 Brekk stops; BrekkCrossing
z  46  ~~~~~~~~~ F2 the first crossing (a wave, east to west) ~~~~~~~~~~
z  31                 path ....  F1 the lesson tongue (beside the path, east)
z  19  -------------- BrekkCrust crossing --------------
z   8  [LANDING PAD: launch]  arrival   landing cradle
       x -30        -10    0    10          30         50        70
```

| Area | What it is for | Size | Combat |
|---|---|---|---|
| The landing | The way in from the Picket: a basalt shelf with the landing pad, a free cradle, Brekk waiting | 26 x 22 m | none |
| The lesson | F1, a tongue of crust lying beside the path, close enough to watch a whole cycle while walking past it. Nothing fights here; stepping onto it is the safe way to learn what bright flow does | 30 x 22 m | none |
| The first crossing | F2, a 6 m tongue across the valley: the first flow that has to be crossed. Its plates break in a wave from east to west, so the bright band walks past whoever waits on the south bank | 32 x 6 m | none |
| The garrison causeway | F3 across the valley, with a basalt causeway the garrison laid over it: a pikeguard pair holds the causeway's north end; the crust either side is the flank. Their cookfire and kettle stone are behind them | 44 x 26 m | `Scoria_Crossing` |
| The cinder hollow | Optional, east of the causeway's north end, across a narrow fast tongue (F4): a crawler den in a pan of crust (F5) and a clan cache at the back | 22 x 18 m | `Scoria_Hollow` |
| The delta | F6, the widest crust: a tide breaks it in bands from the north-east to the south-west. Two basalt islands in it are the stepping places; Ash Crawlers hide under its plates. The glass island off its west side holds a cache, across plates on a faster clock of their own | 50 x 50 m | `Scoria_Delta` |
| The north shelf | Rest after the delta; an ash squall comes down the valley here | 22 x 14 m | none |
| The battery's field | F7, plates each on its own clock, crossed under the mortar battery on the shelf above: shells land on marked circles and leave burning ground, on top of whatever the crust is doing. The sled (a wrecked Muster supply sled, a cache) is at the shelf's west end | 36 x 34 m | `Scoria_Battery` |
| The junction and the camp | The camp road climbs east to the Unsworn camp: tents of Muster canvas over basalt, a forge, the stall, a bench, a cradle, Tovra. Safe | 34 x 26 m | none (safe pocket) |
| The last tongue and the gate | F9, a wave like F2's, then the gate garrison on the Foundry road and the gate itself | 26 x 40 m | `Scoria_Gate` |

**Circulation.** One line south to north, with the camp up a side valley to the east of the junction:

1. Land; Brekk's talk at the shelf's edge; walk past the lesson tongue (F1) and watch it.
2. Cross the first tongue (F2) behind its wave. Brekk stops before the causeway; his talk names the pikeguards.
3. The causeway fight, or round it across F3's crust when it is dark. The cinder hollow off to the east.
4. The delta: south shelf to island A to island B to the north shelf, behind the tide, with crawlers under the plates. The glass island to the west.
5. The battery's field, under the mortars, to the shelf above. The sled to the west.
6. The junction: east to the camp (Tovra; the quest), or north across the last tongue (F9) to the gate garrison and the Foundry gate.
7. Back from the Foundry to the camp; then the Harrowgate road out of the camp's east end.

**Travel** (walking pace 6.7 m/s): landing to the first crossing 40 m (6 s), to the causeway 40 m, causeway to the delta's south shelf 20 m, across the delta 50 m (8 s plus waiting on the tide), delta to the battery 30 m, battery to the junction 25 m, junction to the camp 40 m, junction to the gate 45 m. End to end about 270 m, 40 s without a fight or a wait.

**Camera and scale.** Everything tall stands on the north side of what it belongs to: the garrison's tents behind the causeway, the battery's breastwork behind the field, the camp's tents north of its yard. Walls on the camera's side (south and west of a floor) stay under the sightline. Plates are 5 to 7 m across: a body crosses one in a second, so a plate is a decision, not a corridor.

## The crust (what the player has to be able to see)

- **Every plate says what it is about to do, before it does it**: two and a half seconds of seams brightening, a tremble, a groan. A plate's clock is its own; in a field the clocks are laid as a wave (F2, F9: east to west; F6: north-east to south-west) or scattered (F7: a fight floor), so a player waiting at a bank can see the bright band coming and where it has just been.
- **A danger marking always wins over the lava's own glow.** Bright flow is a pale yellow-white, not the orange or red of a marking (Telegraph.Threat, Telegraph.Heavy), and its light is kept under the bloom's threshold; the melt and the slabs are opaque and draw before every marking; a seam's light is at most a quarter of the plate. Checked by a render test (`AMarkingOnBrightFlowStillReadsAsAMarking`) that lays a mortar's circle and a pikeguard's line on an open plate and on dark crust and measures both against the same frame without them.
- **The rule is the same for everyone.** A hero thrown back is set down on the last ground that doesn't cycle, the partner with the lead (the partner is moved, never burnt: it doesn't choose where it stands); an enemy left standing on open melt burns. A hero rolling through a seam is not caught: the throw comes after a fifth of a second on open melt.
- **Nothing cycles in a conversation or with the game paused**, and nothing is saved: every load starts the clocks at nought.

## Identities

Scene `ScoriaFlows` (ZoneKind GroundCombat, profile Scoria, threat 1). Music: Alien Pulse (ZoneDef, the first engineer's); the camp keeps the zone's track. Ambience: `AMB_ScoriaFlows` when it exists. Light: `ScoriaLooks` in the builder.

## Route coordinates

The builder's constants (`HarrowgateBuilder.Scoria`), checked against the built scene. x, z on the plan; shelves stand at about y 0 (±0.07), the battery terrace at y 0.9 (its ramp at x -3.2 to 3.2, z 187.6 to 191.8), every flow's bed and its plates at y -0.32. A route's `walk`/`path` points should keep to the trodden line: (2, 9) (3, 18) (6, 30) (5, 40) [F2 z 44 to 50] (4, 52) (0, 64) (0, 84 causeway) (0, 96) (-1, 104) [F6] island A (-5, 121) [F6] island B (4, 136.5) [F6] (2, 152) (2, 160) (0, 176 F7) (0, 190 ramp) (1, 198) junction (4, 210); east (16, 209) (28, 208) camp (39, 204) (48, 204) (61, 205); north (1, 222 F9) (0, 240) (0, 251). Crossings start on walking through their box; everything else is pressed (South) within its range. A crossing of a flow should wait on the south bank for the plates on the line to be dark (`CrustField.Current.PhaseOf`), or be timed from the zone's load: every clock starts at nought on load and stops in conversations.

| Kind | Id / name | x, z | Range or box | Flag it sets / needs | Notes |
|---|---|---|---|---|---|
| Spawn | `Arrival` | 2, 9 | | | From the Picket's landing |
| Spawn | `Camp` | 39, 204 | | | The camp cradle saves here |
| Spawn | `Foundry` | 0, 251 | | | The Foundry's way back arrives here |
| Spawn | `Harrowgate` | 61, 205 | | | The town's way back arrives here |
| Way out | `Launch — The Picket of Teeth` | -8, 5 (the landing pad) | 4 | open | `DockingPad` to PicketOfTeeth spawn `Scoria` |
| Way out | `Foundry road — The Bridle Foundry` | 0, 259 | 4.4 | needs `met.Tovra` | To BridleFoundry `Arrival`; refused: "The Foundry gate is the garrison's: talk to Tovra Kesh at the Unsworn camp first"; sign at (3.6, 254.6) |
| Way out | `Town road — Harrowgate` | 66.5, 205 | 4.2 | needs `vask.roadSaid` | To Harrowgate `Arrival`; refused: "Tovra Kesh hasn't said where the road goes yet: talk to her at the camp" |
| Crossing | `Brekk on the crust` (`BrekkCrust`, Brekk) | 1, 19.5 | box 30 x 3 | sets `vask.crustRead` | Brekk's guide body walks on to the first tongue |
| Crossing | `Over the first tongue` (no talk) | 3, 53.5 | box 30 x 2.4 | sets `vask.tongueCrossed` | Brekk follows over F2 and on to the causeway |
| Crossing | `The garrison's causeway` (`BrekkCrossing`, Brekk) | 0, 67 | box 26 x 3 | sets `vask.garrisonSeen` | |
| Crossing | `Two pikes` (bark `pikeguard`) | 0, 78.5 | box 6 x 2.4 | sets `vask.pikesSeen` | The causeway's south end |
| Crossing | `Ash coming down the valley` | 2, 157.5 | box 22 x 2.4 | sets `vask.squallMet` | The first ash squall closes at once |
| Crossing | `The battery` (bark `mortar`) | 1, 165.5 | box 24 x 2.4 | sets `vask.mortarsSeen` | |
| Radio | Brekk `hail.crust` | (4, 36), (-1, 104), (2, 155) | 9 m each | `vask.brekkHail1`..`3` | Once per save each, in that order |
| Prompt | Kettle stone 1 "Leave it under the kettle stone" | by the fire at (6.5, 98) | 2.6 | sets `vask.kettle1` | Report `KettleStone`, `QuietHands` step 0, bark `message` |
| Prompt | Kettle stone 2 | by the fire at (-12.5, 198.5) | 2.6 | sets `vask.kettle2` | On the battery terrace |
| Prompt | Kettle stone 3 | by the fire at (8.5, 248) | 2.6 | sets `vask.kettle3` | Behind the gate garrison |
| Prompt | `Look at the pike-head` (`BrekkPikeHead`, Brekk) | 54, 208.4 | 2.6 | needs `vask.unswornMet`, sets `brekk.pikeDressed` | The camp forge |
| Person | `Tovra` | 46, 210.5 | | until `chapter8.complete` | First `TovraFirst` (sets `vask.unswornMet`), repeat `TovraRepeat`; beats are the first engineer's |
| Person | `Brekk` (stall) | 49.5, 200.6 | | from `clear.Scoria_Crossing` until `chapter8.complete` | `BrekkCampStall` once (completion `brekk.stallTalked`), then `BrekkCampRepeat`; both open the stall |
| Guide | Brekk's body | (5, 16), then (7, 38.5), then (-5, 64); waits at a bank while the crust ahead is lit or will light within 3.2 s (`CrustWait`) | | legs on "", `vask.crustRead`, `vask.tongueCrossed`; gone at `clear.Scoria_Crossing` | Never talked to |
| Service | `Landing cradle — repair and save` | 9, 4 | | | Free; saves at `Arrival` |
| Service | `Camp cradle — repair and save` | 35.5, 200.5 | | | Free; saves at `Camp`; in the safe pocket (36 x 26 on (47, 206)) |
| Service | `Fabricate — Lattice Anvil` | 41.5, 198.5 | | | In the safe pocket |
| Cache | `vask.scoria.hollow` "Clan cache" | 32, 114.5 | 2.6 | | Across F4; a frame part (tier 4), Scoria Glass, gel |
| Cache | `vask.scoria.glass` "Smith's chest" | -22.5, 142.5 | 2.6 | | The glass island, across F6's fast plates; an emitter (tier 4) |
| Cache | `vask.scoria.sled` "Wrecked supply sled" | -21, 196.5 | 2.6 | | The terrace's west end; a module (tier 4) |
| Harvest | `scoria_glass_0..4` (Scoria Glass, 2 each) | (-9, 60), (18.5, 111.5), (-24.5, 138.5), (-24, 193), (9, 236) | 2.6 | | "Gather Scoria Glass" |
| Fight | `Scoria_Crossing` | 0, 93 | 30 x 18 | | A `MusterPikeguard` pair on the causeway's north end (one spawner, count 2, radius 2.1, facing south), a `ReachScav` at (6, 98.5) |
| Fight | `Scoria_Hollow` | 26, 108 | 18 x 14 | | 2 `AshCrawler`, hides on F5 |
| Fight | `Scoria_Delta` | -4, 128 | 40 x 40 | | 3 `AshCrawler`, hides on F6 |
| Fight | `Scoria_Battery` | 0, 184 | 36 x 32 | | 2 `MusterMortar` on the terrace at (-8, 196.5) and (8.5, 196), a `ReachScav` at the ramp's head (0, 194) |
| Fight | `Scoria_Gate` | 0, 243 | 26 x 18 | | A `MusterPikeguard` pair at (0, 247), a `ReachSergeant` at (-7, 251) |
| Hides | `AmbushSpot` radius 3.6 | on the plates nearest (21, 104) and (30, 111) (F5); (-13, 115), (3, 127), (-9, 140) (F6) | | | Each with a blister of crust over it |

**The flows** (each plate a convex quad; offsets are the head start of its clock, so on load a plate is `offset` seconds into its cycle):

| Flow | Where | Plates | Clock (dark, seam, open) | Pattern |
|---|---|---|---|---|
| F1 the lesson | from (25, 31.5) to (8.5, 31), 6.6 wide | 3 | 5, 2.5, 3.5 | a wave from the east, 1.4 s a plate |
| F2 the first crossing | from (17, 47.5) to (-16, 46.5), 6.2 wide | 5 | 6, 2.5, 3.5 | a wave from the east, 1.5 s a plate |
| F3E, F3W under the causeway | (23, 84) to (2.7, 84) and (-2.7, 84) to (-21, 84), 8 wide | 3 and 3 | 7, 2.5, 3 | each plate on its own clock |
| F4 to the hollow | (11.5, 113) to (11.5, 97), 4.8 wide | 3 | 3.6, 1.8, 3 | a fast wave from the north |
| F5 the hollow's pan | (19.5, 103.5) to (29, 109.5), 6.8 wide | 2 | 8, 2.5, 3 | slow |
| F6 the delta | a grid of 6 m cells, x -29 to 19, z 106 to 154, inside the valley | about 30 | 7, 2.5, 3.5 | a tide from the north-east at 5.2 m/s; round the glass island 3.2, 1.6, 2.4 on scattered clocks |
| F7 the battery's floor | a grid, x -17 to 19, z 168 to 186 | about 15 | 6 to 8, 2.5, 3 | each on its own clock |
| F9 the last tongue | from (15, 222.5) to (-14, 221.5), 6 wide | 5 | 5.5, 2.5, 3.5 | a wave from the east |

## Contracts

### With the first engineer (shared definitions, dialogue, quests)

What this zone stands in its scene, for the main quest's `stepTargets` and the side quest's:
- Step 4 (`vask.unswornMet`): speaker `Tovra` in `ScoriaFlows`, at the camp.
- Step 5 (Reach `BridleFoundry`): the Foundry gate in `ScoriaFlows` leads to `BridleFoundry` spawn `Arrival`, locked on `met.Tovra`.
- Step 9 (`vask.roadSaid`): speaker `Tovra` again.
- Step 10: the Harrowgate road (camp east end) leads to `Harrowgate` spawn `Arrival`, locked on `vask.roadSaid`.
- `QuietHands` step 0: three `DiscoveryPoint`s, report `KettleStone`, prompt "Leave it under the kettle stone", quest `QuietHands` step 0, flags `vask.kettle1`..`3`, situation `message`.
- Brekk's stall: an `Npc` "Brekk" whose first talk there is `BrekkCampStall` and every later one `BrekkCampRepeat` (Npc's final tiers: final flag `met.Brekk`, completion `brekk.stallTalked`); the stall `ScoriaFlows_Brekk` is yours.
- Tovra: an `Npc` "Tovra" (first `TovraFirst`, repeat `TovraRepeat`). Her beats (`TovraHarrowgateRoad`, `TovraQuietReturn`, `TovraCages`, `TovraQuietOffer`) are `SideStories.Beats`, yours.
- `BrekkPikeHead`: a `DiscoveryPoint` at the camp forge, prompt "Look at the pike-head", needs `vask.unswornMet`, sets `brekk.pikeDressed`.
- Crossings: `BrekkCrust` (sets `vask.crustRead`), `BrekkCrossing` (sets `vask.garrisonSeen`).
- Barks used: `crust` (the lead, as a seam lights under them or they step back onto dark crust), `flow` (thrown back), `squall` (an ash squall closes), `message` (a kettle stone), `pikeguard`, `mortar` (first sight, by crossing), Brekk's `hail.crust` (three radio hails before the camp).
- The objective row "Get onto dark crust: the seam is about to go" (manifest section 6) needs a hook in `ObjectiveHud`, which takes no new cases: not done; the `crust` bark carries it.

### With the combat engineers

- Encounters reference `MusterPikeguard`, `MusterMortar`, `AshCrawler`, `ReachScav` and `ReachSergeant` by id (the counts are the ledger's below); an id not yet defined stands in as `ThawedGuard`, `Sporecaster`, `RimeStalker`, `MusterScav`, `MusterSergeant` and logs `SCORIA_STAND_IN`.
- **Ash Crawler and the crust.** `Lattice.Combat.GroundHeat` (`S/Combat/GroundHeat.cs`) answers, for any point, how hot the ground is there: `GroundHeat.Of(p)` 0 for ground that never cycles or dark crust, 0.5 for a lit seam, 1 for open melt; `GroundHeat.Bright(p)`, `GroundHeat.Hot(p)` (lit or open). The Flows' `CrustField` registers it; anywhere else it answers 0. For "flees bright flow", the crawler can leave a hide whose ground is `Hot`, and pick a hide that isn't. Hides (`AmbushSpot`, radius 3.6) stand on plates of F5 and F6; the builder writes their places in the table below.
- **The Mortar's burning ground** is the ordinary `LingeringArea` Burn: it lies over the crust and is drawn before nothing of the crust's (markings draw last).
- An enemy left on open melt (not hidden, not a boss) loses a tenth of its integrity a second.

**The first combat engineer's answer (November 12, `HarrowgateBuilder.Enemies.cs`):**
- **Use `ReachScav` and `ReachSergeant`, not `MusterScav` and `MusterSergeant`.** Chapter 2's two are tuned for a Sync 9 party (a 14-point shot at Sync 22 is under 2% of a bar) and their numbers are pinned by chapter 2's tests; the Reach's are the same soldiers, fighting as they always did, at chapter 8's weight (9 to 13% of a bar a hit). Display names are the same: "Muster Scavenger", "Muster Sergeant".
- `GroundHeat` is used as written: an Ash Crawler never takes a hide whose ground is `Hot`, and leaves one that turns `Hot` under it (out of its hide early, spent, before it goes for a cold one). It comes up with a **lunge** down a marked lane 4.5 m long (one bite, not two: the Rime-stalker's pair was chapter 6's), then stands spent 2 s. Its ring is 3.6 m (`tune.wake`): a crawler that takes an `AmbushSpot` sets the spot's ring to its own 3.6 m whatever radius the builder gave it, so size the plates for that.
- `MusterPikeguard`s pair by themselves: two woken within 7 m of each other stand 4.2 m apart, face the party together, one braces (a long marked thrust, 9 m) while the other covers it, and neither strikes at a hero standing between them (Brekk's "get between them"). A third or a lone one fights as a lone guardian. Place a pair's two within 7 m of each other (a `Spawner` of count 2, radius 2.1, does it).
- `MusterMortar`: lobs from up to 13 m onto a marked 2.4 m circle (1.3 s in the air) that burns 2.8 m across for 4 s; it walks back from a hero who closes, at a walk. Its battery is best on a shelf a hero can climb to: stepping out of the circle toward it is its answer.
- The XP ledger assumes, on the main path: the causeway a pikeguard pair and a Scav; the delta 3 crawlers; the battery 2 mortars and a Scav; the gate a pair and a Sergeant (about 1,040 XP); and the cinder hollow's 2 crawlers optional. Write your counts here if they differ and I will move the numbers.

### With the Foundry and town engineers

- The Foundry's way back to the Flows arrives at spawn `Foundry` (in front of the Foundry gate, inside the north wall).
- Harrowgate's way back to the Flows arrives at spawn `Harrowgate` (the camp's east end).
- The Picket's landing (its `Land — The Scoria Flows`) arrives at spawn `Arrival`; this zone's launch arrives at the Picket's `Scoria` (`HarrowgateBuilder.FromScoriaSpawn`).

## Review

Reviewer: Claude (chapter 8's second world engineer), from editor stills and plans, November 12. Nobody else has looked, and nothing here has been walked in a player. **MAP_EYE_TEST: blockout PASSED (agent review, plans and stills); final in-player pass OPEN.**

### Blockout

Evidence: plans `Builds/quality/chapter8/scoria-blockout-01/` (`ScoriaFlows-whole`, `-south`, `-middle`, `-north`), 22 stills `Builds/quality/look/scoria-blockout-01/`. The navigation bake reached all 45 must-reach points; 84 plates.

| Check | What the plans and stills showed |
|---|---|
| Purpose and adjacency | Passed. The landing at the valley's foot, a garrison on each crossing with its fire and tents behind it, the camp up its own side valley off the junction, the Foundry gate at the head. |
| Circulation and scale | Passed on the plan. One spine south to north; each flow crossed going north; the camp and the three pockets off it; nothing blocks the spine but crust on its clock and the garrisons. |
| Construction | Passed. Flows lie in beds a third of a metre down with a bank up to the shelves; the causeway is laid across F3; the islands stand out of the delta. |
| Terrain and arenas | Passed. Every fight on shelf or crust with room round it; the battery on its terrace above its field, reached by a ramp. |
| Immediate visual understanding | **Failed in part**, and repaired: the three states read (dark crust, lit seams, open melt, an island, a blister over a hide: `island-a`), but lit seams were crisp orange bands, a marking's look (`first-crossing`); the open melt was saturated amber, near a marking's orange; slabs read as paving; the basalt's ash blotches read as camouflage; built lean-tos read as boxes. |

Repairs: the slabs are a mesh of their own whose joins' light fades into the slab and whose plate edge carries a third of it (no glowing square per plate); slabs come in several sizes and casts and the crust has fine cracks; the seams run cherry red to white heat; the open melt is pale yellow-white with red veins and its light was taken down a step; the basalt's ash is finer; the Muster's own lean-to and the market awning replace built ones; island B moved off the east wall.

### Dressed (editor), after the lead's look pass (November 13)

Evidence: 22 stills `Builds/quality/look/scoria-look-10/` (earlier passes: `scoria-dress-01` to `-06`, `scoria-look-07` to `-09`).

What the look pass changed: the open melt is rafts of dark cooling skin with the glow in thin cracks between them, red at a crack's edge and yellow at its middle, mostly dark (it was cream with red veins, read as marble); every plate is drawn along its cracks, from one crack pattern laid across the whole flow, so neighbouring plates interlock and what lights is a slab, not a tile (the burn is shrunk to where the drawn plate and its footprint agree); Brekk waits at a bank until the crust ahead will hold (`CrustWait`); Brekk's stall is Vaskar make (iron posts, a roof of sooted hide and iron, a smith's red cloth) in place of the kit's leaf-roofed awning; the pale ash-drift decal is gone.

The marking test (`AMarkingOnBrightFlowStillReadsAsAMarking`), the rim's change in the frame: shelf 0.363, dark crust 0.382, open melt 0.392; the fault (the plate lit as a plain sheet of a marking's orange) 0.149 to 0.165 across runs, failing as it must.

`python tools/look_density.py Builds/quality/look/scoria-look-10 --max-blob 0.22` (largest plain area): passing `battery-terrace` 0.04, `camp` 0.11, `sled` 0.11, `gate` 0.11, `delta-south` 0.09, `camp-forge` 0.15, `first-crossing` 0.17, `causeway` 0.18, `last-tongue` 0.19, `lesson` 0.22, `hollow` 0.22, `junction` 0.22; over: `island-a` 0.56, `island-b` 0.56, `battery` 0.39, `glass-island` 0.35, `causeway-fight` 0.27, `arrival` 0.25, `north-shelf` 0.24, overviews 0.27 and 0.31. The crust and melt frames are busy to the eye (rafts, cracks, slabs of several tones); the tool reads their dark fields as one plain tone. The arrival and north shelf are ash-and-cinder shelves just over the line.

What is still weak, and would be the first things to look at in a player:

- **A field's own bank edge is straight** where a tongue or the delta meets the shelf (`delta-south`): the plates interlock with each other, not with the bank.
- **Lit seams are orange** (the melt's own language), close in hue to a danger marking where a marking crosses a lit plate; the render test measures open melt and dark crust, not a lit seam.
- **The cinder patches** read as a red-brown spatter on the shelves (`camp`, `arrival`): greyer, the shelves measured plain.
- **The squall, the blisters' breathing, the crust's cycle, the melt's drift and Brekk's wait are runtime** and not in an editor still (a still is posed at one clock, `CrustField.stillClock`).
- **Sounds** are existing clips (`root_groan`, `thrusterFire_000`, `explosionCrunch_000`), unheard.
