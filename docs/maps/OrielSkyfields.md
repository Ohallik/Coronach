# The Skyfields - Oriel, from Ishen's camp to the Glass Choir's door

Functional brief, written before construction (November 12, world engineer B, chapter 7). **MAP_EYE_TEST: blockout FAILED on the first layout, PASSED with repairs; dressed (editor) PASSED with reservations; final in-player pass OPEN** (review at the end; an agent's review, nobody else has looked). Chapter brief: [ch07-the-cage-makers.md](../chapters/ch07-the-cage-makers.md); script wiring: [ch07-manifest.md](../writing/pending/ch07-manifest.md). Builder: `S/Editor/World/OrielBuilder.Skyfields.cs` (everything in its nested class `OrielBuilder.Skyfields`). Mechanics: [CHAPTER_GUIDE.md, "World mechanics"](../CHAPTER_GUIDE.md) (`FibreLift`, `FibreJunction`, `KeepsPace`, `FallsAlong`).

## Purpose and formation

Oriel's high gardens: islands of pale gold stone hanging over a sea of cloud, each held where it is by **fibres**, severed strands of the old road that still run and still glow. The court's gardeners laid walled beds and stairs across them a thousand years ago; the automatons still keep them, though nothing grows. Where a fibre frayed, glass grew (the glass forests); where a stair's island was moved, the stair now ends in air.

**The one rule: weight and light follow the fibres.** Near a fibre, down is along the thread. In a player's words, after the first island: *things fall toward the glowing threads, not to the ground; a thread across a gap carries you; pull a thread tight and its island comes up, cut it and the island drops; when a thread lights up, something is about to run down it.*

- **Taught, safely** (the camp, nothing awake): Ishen's book slides off his crate and falls sideways onto the old wall, where the camp's fibre runs (`FallsAlong`, as `IshenLanding` starts). Before that is said, the screen has already shown it: leaves, a cup and pebbles lie flat on the wall's face, not at its foot, and the camp's water runnel climbs the wall and runs away up the thread.
- **Used to get somewhere**: lift nodes (a taut fibre across a gap is a road of light: *Ride the lift node* and it carries the party up it, `FibreLift`), then junctions (an island hangs off a frayed collar across a gap: *Seat the junction* and it rises flush; *Cut the junction* and it drops into the cloud and the freed fibre springs taut across the gap as a road. Either one crosses: `FibreJunction`).
- **Used in a fight**: the Fibre Wisps' fights stand on floors crossed by fibres, and a wisp's strike runs down the fibre it sits on, which lights first (the combat engineer's behaviour; the fibres are this zone's). The second junction carries a garden: cut it and the garden is wrecked, and the gardeners tending its far end turn (`Pruner`). The third carries a gardener's chest: seat it and the chest comes up with it; cut it and the chest goes into the cloud.

**The route's shape is a switchback stair of islands.** Six long islands, each a row across the map at its own height, climbing north: walk the camp east, ride up, walk the terrace west, cross the first junction, walk the fibre fields east, ride up, walk the sagging gardens west, cross the second junction, walk the stair gardens east, cross the third, and the Choir's door is in front of you. Every row has the void and the next island's face at the back of its frames.

## Users and access

Nobody lives here but the gardeners. Ishen's camp at the landing is the safe pocket: the party's ships, Ishen and his crates of books, Lume at her stall beside the chain's last lantern, a bench and a mending cradle. A gardener with a rake stands by the camp's old wall and has not moved since the party landed; from the landing it follows twelve paces behind (`KeepsPace`). Ishen walks with the party from the landing (`KeepsPace`, never in a fight) and is at his crates again once he has read the record.

The party lands from the Orrery at the camp's west end (spawn `Arrival`) and can launch from the same pad. The Choir's door is at the north end (`Choir door — The Glass Choir`, to the Glass Choir's `Arrival`); both ways out of the Choir bring the party back to spawn `ChoirDoor`. From the forecourt the long fibre runs down to the camp (`Ride the long fibre — Ishen's Camp`): a ride with no road under it, one way, a shortcut home.

## Layout

About 110 x 170 m of islands, plus the Choir's front and body behind the north end. x runs east, z north, y up; the camp's floor is 0. The cloud sea lies at -24 and the abyss's floor (nobody's, never seen through the cloud) at -30.

**Every island is a strip 16 m deep** (the camp 18, the forecourt 20) with its walked line four to six metres in from its north rim, because the fixed camera sees about twelve metres north of the party and five south: the void, the fibres across it and the next island's face are the back of every frame, as the Braided River's channels are. The first layout had rows 22 to 26 m deep with the line down their middles, and every frame was floor (see the review).

```text
 z 172-260        ~ ~ the Glass Choir: the court's front in pale stone behind the door, the bridled Choir's body in glass beyond it, ribs out of the cloud ~ ~
 z 169                        [ CHOIR DOOR ] -> GlassChoir             (the gardener sits on the step)
 z 149-169   R6 FORECOURT  y12        . Sky_Door .   long fibre down ->  (one way to the camp)
 z 149-163  HI  y18  (lantern seat)            ^J3 (a chest rides on it)
              \  L3                            |
 z 121-137   R5 STAIR GARDENS  y12  sealed garden . GJ2 . raised beds . GJ3 . J3 dock  ==> east       (Sky_Pruners at J2's landing, if J2 is cut)
                                     ^J2 (a garden rides on it)
 z  93-109   R4 SAGGING GARDENS y12  <== west .. J2 dock . bed and pump . GJ1 (the gardener waits) .. Sky_Mixed .. follower talk .. L2 top
                                                                                                         ^L2
 z  63-79    R3 FIBRE FIELDS  y6   J1 landing ==> east .. bed . a gate to nowhere .. Sky_Wisps (three floor fibres) .. swept stair .. L2 foot
                                     ^J1 (nothing on it: the lesson)
 z  35-51    R2 GARDEN TERRACE y6  glass forest (Sky_Grove, a chest) . J1 dock . junction talk . walled garden . colonnade . Sky_Glassback . L1 top
                                                                                                                                  ^L1
 z   4-22    R1 THE CAMP  y0     pad and pod (Arrival) . cradle . bench . Ishen's crates . the old wall + gardener . Lume . LAST LANTERN . L1 foot
             x -58                                         0                                                    36
```

| Area | What it is for | Size | Level | Combat |
|---|---|---|---|---|
| R1 The camp | The landing and the safe pocket: the courier pod on its pad, cradle, bench, Ishen under his awning, Lume's stall, the last lantern, the old wall with its fibre and the gardener. The rule shown and said | 80 x 18 m | 0 | none (safe pocket) |
| L1 First lift | The first ride: a road of light up from the camp's east end to the terrace, nothing awake at either end | 13 m, rises 6 | 0 to 6 | none |
| R2 Garden terrace | The first Glassback on open stone with a colonnade along its rim; the walled garden of bare grit with two gardeners in it; the junction talk; the glass forest at the west end (optional: its own Glassback and a chest) | 94 x 16 m | 6 | `Sky_Glassback`, `Sky_Grove` |
| J1 First junction | The lesson: an island hanging off a frayed collar between the terrace and the fibre fields, nothing on it. Seat or cut | 6 x 12 m gap | 6 | none |
| R3 Fibre fields | Three fibres run across the floor from rim to rim; the Fibre Wisps ride them. A gate standing alone; the swept stair on the north rim | 80 x 16 m | 6 | `Sky_Wisps` |
| L2 Second lift | Up from the fibre fields' east end to the sagging gardens | 14 m, rises 6 | 6 to 12 | none |
| R4 Sagging gardens | The follower talk; a Glassback with wisps; a bed, its pump and its keepers; the first frayed garden, where the gardener waits (EmptyGardens) | 84 x 16 m | 12 | `Sky_Mixed` |
| J2 Second junction | An island with a garden bed on it, its keepers at the far end. Seat keeps the garden; cut wrecks it and they turn | 6 x 12 m gap | 12 | `Sky_Pruners` (cut only) |
| R5 Stair gardens | Raised beds and stairs that end in air; the sealed garden at the west end; the second and third frayed gardens; the way up to the highest island; the third junction's dock | 86 x 16 m | 12 | `Sky_Pruners` (cut only) |
| L3 Third lift (optional) | Up from the stair gardens to the highest island | 15 m, rises 6 | 12 to 18 | none |
| HI Highest island | The worn seat where the chain's last lantern stood (LumesLantern); a chest; the whole Skyfields and the Choir from above | 28 x 14 m | 18 | none |
| J3 Third junction | An island with a gardener's chest on it, between the stair gardens and the forecourt | 6 x 12 m gap | 12 | none |
| R6 Forecourt | The Choir's door between two columns, the gardener's step, Pruners set on the Warden's order, the long fibre home | 48 x 20 m | 12 | `Sky_Door` |

**Each island's ground is its own.** Every island is the court's paving, laid in courses by the court (8 m to the floor's texture); the ground kit's three layers are the same flags worn (the camp), raked grit laid over them (the gardens), and the same flags grown through with glass (the fibre fields), the two paving layers laid at the floor's own 8 m so their joints fall on its joints. No wild path layer: none of this ground is wild.
- the camp is trodden and lived in: its flags worn and dusty where it is lived on and along the walk, footfall, a scorched round under the kettle, rugs under the awnings, litter, dust against the rims, the washing out;
- the garden terrace and the sagging gardens are raked grit with a swept path of the court's flags through them, a crisp edge of dark stone between; every kept stone and lone glass stem in the grit has raked rings round it, and the gardeners kneel at them or stand with their rakes;
- the stair gardens are raked in their own pattern, rows of fans; the sealed garden's one bed is raked in a spiral round a single jar;
- the fibre fields are the court's flags grown through with glass: every joint and crack run full of it, some flags gone wholly to it, shards through the paving, knots of glass, glass roots running over the floor from every tree and collar, lit from inside, and their light on the stone;
- the glass grove at the terrace's west end is the fibre fields' ground;
- the forecourt is the court's oldest, grandest stone: its flags in their richest cream, a great bronze medallion let into them between the door's two fibres and a processional band of dark stone edged in bronze to the door;
- the highest island is the same old stone, weathered: lichen, grass, white flowers.

**Circulation.** One line, back and forth, climbing:

1. Camp, west to east: the pad, `IshenLanding` at Ishen's crates, Lume, the lift node.
2. `L1` up. Terrace, east to west: `Sky_Glassback`, the walled garden (`IdraEmptyGarden`), the junction talk (`IshenJunction`), `J1`. Past J1, the glass forest (optional: `IdraGlassForest`, `Sky_Grove`, a chest).
3. Fibre fields, west to east: `Sky_Wisps`, the swept stair (optional: `IdraSweptStair`), the lift node.
4. `L2` up. Sagging gardens, east to west: `IdraGardenerFollows`, `Sky_Mixed`, the first frayed garden where the gardener has gone ahead (optional: `IdraGardenerWaits`, starts EmptyGardens), `J2`.
5. Stair gardens, west to east: the sealed garden (optional), the second and third frayed gardens, `L3` to the highest island (optional), `J3`.
6. Forecourt: `Sky_Door`, `IdraChoirDoor`, the door. Afterwards the long fibre takes the party home: `MeretOrielCall` on coming into the camp, `LumeBrekksWord` at the last lantern.

**Travel** (walking 6.7 m/s; a lift ride 2.6 s, the long fibre 7 s):

| Leg | Metres | Seconds |
|---|---|---|
| pad to Ishen's crates | 28 | 4 |
| crates to the lift node | 36 | 5 |
| terrace, lift top to J1 | 60 | 9 |
| fibre fields, J1 landing to the lift node | 66 | 10 |
| sagging gardens, lift top to J2 | 74 | 11 |
| stair gardens, J2 landing to J3 | 62 | 9 |
| forecourt to the door | 22 | 3 |
| end to end without a fight | about 350 | about 55, plus two rides and three junctions |

## Mechanics (guide section 15)

| Thing | Where | Prompt, flag | What it does |
|---|---|---|---|
| `FibreLift` (L1, L2, L3) | a node at each end of a road of light | `Ride the lift node`, no flag | Carries the party (both heroes out) along the road to the other node in about 3 s, eased like a fall and a catch; bark `lift`. The road is solid and can be walked too (it is under 27 degrees): weight follows the fibre either way |
| `FibreLift` (long fibre) | forecourt to camp | `Ride the long fibre — Ishen's Camp` | The same with no road under it: a one-way ride through the air over the islands, a shortcut home |
| `FibreJunction` (J1, J2, J3) | a frayed collar on the near rim | `Seat the junction` (`oriel.junctionN.seat`), `Cut the junction` (`oriel.junctionN.cut`); either sets `oriel.junctionN` | Seat: the island rises flush over 3.5 s and its deck takes the party (what is on it stays). Cut: the fibre snaps, the island drops into the cloud, the fibre springs taut across the gap as a road of light. Either way the same deck crosses; the other hand goes. What rides on it (J3's chest) can't be reached while it hangs. Barks `junction.seat`, `junction.cut` |
| Garden junctions GJ1-3 | frayed collars under gardens hanging off the north rims of R4 and R5 | `Seat the junction` (`oriel.garden1..3`), report `GardenJunction`, EmptyGardens step 0, bark `mend` | The garden rises level and the gardener's light comes up. Seat only: the quest can't be lost |
| `FallsAlong` | Ishen's book, at the camp's old wall | starts with `IshenLanding` | Slides off the crate and falls sideways onto the wall, and lies on its face. A save with `oriel.ruleSeen` has it there already |
| `KeepsPace` | the gardener, Ishen | | Follows at twelve paces (Ishen at four), stops when the party stops, holds back from any awake enemy, goes ahead or sits where its stages say |

## What the player has to be able to see

- **A fibre.** A hand's width of light anchored in bronze collars on the rims, in a colour nothing else in the zone has: taut, blue-white with a blue halo; fraying, amber, broken along its length and guttering (the Choir's fibres speak the same: held, pale steel; slack, amber). It throws its light on the stone and walls near it. Every island hangs on some, and every north rim has one every dozen metres or so, so every frame has one.
- **Down is along the thread, in an empty frame.** Round the foot of every fibre: grit and dust lying in lines that all run in to it, like filings round a magnet; grass leaning to it; petals in a trail toward it and stuck flat to the thread; water beaded along it; in play, motes drifting in. At the camp, before a word is said: things lying on the wall's face, water climbing the thread, the camp's washing and the awnings' cords and the lantern's tassel all hanging toward the wall's fibre, not down.
- **Which way a junction goes.** The island hangs crooked well below both rims, rolled down on its free side so the cloud shows past its low edge, its shade on the cloud under it; at the near collar the fibre visibly parts into strands, most snapped and sparking, its amber light guttering on the rim; the seat hand is at the collar, the cut hand three metres along the fibre. The deck it will make is the gap between the two rims' openings, each closed by a bronze rail.
- **A taut fibre is a road.** Every lift and every cut junction is the same image: a ribbon of pale light across a gap with the thread down its middle and a glowing kerb each side.
- **Where the fight's fibres are.** Each wisp fight's floor fibres are drawn the same as every other fibre, low over the stone, and a wisp's strike runs only down one of them.

## Identities

Scene `OrielSkyfields` (ZoneKind GroundCombat, profile Oriel, sky `oriel-day-sky`, threat 1, chapter 7). Display "The Skyfields". Music: Glass Ruans; the camp's safe pocket keeps it (the chapter is the quiet one). Light: `SkyfieldsLooks` in the builder. Ambience: `AMB_OrielSkyfields` when it exists.

## Coordinates (for routes and tests)

From the builder's constants as built: see "Coordinates as built" under the review, which a route writer can use.

## Encounter plan

Enemies by id from the brief; the combat engineer defines them (`OrielBuilder.Enemies.cs`). Until an id is defined the zone stands a stand-in of the same family and logs `SKYFIELDS_STAND_IN <stand-in> for <id>` (Glassback: Thornback; Fibre Wisp: Frost Motes; Pruner: Thawed Guard).

| Fight | Where (centre, box) | Pack | Where the player stands to answer it |
|---|---|---|---|
| `Sky_Glassback` | R2 east (10, 43), 26 x 16 | 1 Glassback from (6, 47.5) | The terrace is 16 m deep with a rim .55 m high on both sides and 26 m of open stone east-west: sidestep the charge with the rim behind you and it ends in the rim, stuck. Its plates lie as a ring that cuts: there is room to fight round it |
| `Sky_Grove` (optional) | R2 west, the glass forest (-48, 43), 18 x 14 | 1 Glassback from (-51, 47) | The same lesson among glass trees: the trees stand on the rim and at the west end, the floor between them open |
| `Sky_Wisps` | R3 middle (0, 71), 24 x 16 | 2 Fibre Wisp from (-6, 75) and (6, 67) | Three fibres run north-south across the floor 7 m apart (x -7, 0, 7): a strike runs down one, lit first, so stand between them |
| `Sky_Mixed` | R4 east (6, 101), 26 x 16 | 1 Glassback from (2, 105.5), 2 Fibre Wisp from (12, 97) | Two fibres slant across the floor 12 m apart: the charge across the fibres, two answers on one floor |
| `Sky_Pruners` | R5 at J2's landing (-34, 129), 20 x 16, needs `oriel.junction2.cut` | 2 Pruner from where the garden's keepers stood, (-42.6, 124.4) and (-33.6, 124.8) | The keepers turn when their garden is wrecked; 5 m of open stone round each |
| `Sky_Door` | R6 (4, 155.5), 24 x 13 | 2 Pruner from (4, 159), 1 Fibre Wisp from (4, 152) | Pruners on the Warden's order before its door, and a wisp on the forecourt's two fibres (x -2, 10) |

## For OrielBuilder.cs

What `OrielBuilder.cs` (the chapter's definitions, quests and dialogue: world engineer A) needs from this zone, and what this zone needs from it.

- **Spawns**: `Arrival` (from the Orrery; the chart stop), `Camp`, `ChoirDoor`.
- **Reports** the main quest's arrows find: `Landing` (the `IshenLanding` crossing, step 2) and `CampReturn` (the `MeretOrielCall` crossing, step 9). Step 3 (Reach `GlassChoir`) points at `Choir door — The Glass Choir`; step 10 at Lume.
- **Side quest points**: `GardenJunction` x3 (EmptyGardens step 0), `SealedGarden` (step 1), `LanternSeat` (LumesLantern step 0). `IdraGardenerWaits` is a point beside the gardener whose talk starts EmptyGardens (it sets `oriel.gardenerAsked`).
- **People placed here**: `Ishen` (Npc: repeat `IshenCamp`, post `IshenOrielPost` on `oriel.coronachHeard`; beats `IshenMoonsReturn`, `IshenMoonsOffer` from SideStories), `Lume` (first `LumeFirst`, repeat `LumeRepeat`, post `LumePost` on `oriel.coronachHeard`; beats `LumeBrekksWord`, `LumeLanternReturn`, `LumeLanternOffer`). Her stall is the zone's own `ShopInventory` (`OrielSkyfields`), already defined.
- **World flags** this zone sets: `oriel.landed` (a crossing at the ships, no talk), `oriel.junction1..3` (and `.seat`/`.cut` for each), `oriel.garden1..3`, `oriel.gardensSeated` (all three, by `FlagAll`), `oriel.sealedGardenSeen`, `oriel.lanternSet`, `oriel.doorReached`, and the talks' own: `oriel.gardenSeen`, `oriel.junctionRead`, `oriel.followerSeen`, `oriel.gardenerAsked`, `oriel.forestHeard`, `oriel.stairSeen`. For `q.storyFlags` (what the main path always sets): `oriel.landed`, `oriel.ruleSeen`, `oriel.gardenSeen`, `oriel.junctionRead`, `oriel.junction1`, `oriel.followerSeen`, `oriel.junction2`, `oriel.junction3`, `oriel.doorReached`.
- **A lock this zone names**: none. Every way out is open.
- *(From world engineer A, November 12.)* `Launch — The Orrery` names the Orrery's spawn **`OrielSkyfields`** (0, 1, 866), at the lane's inner end just south of `Land — The Skyfields`, which arrives here at `Arrival`. The flags above for `q.storyFlags` are in `OrielBuilder.Quests`. `Ishen` and `Lume`'s beats are in `SideStories.Beats` under "Chapter 7"; `Lume`'s first node is `LumeFirst`, and Ishen at the camp is met already (chapter 2), so his first node there should be `IshenCamp` as well as his repeat.

## For the combat engineer

The ground each fight stands on (as planned; coordinates as built below once built). Every fight floor is the island's own level stone with a rim .55 m high at its edges, which stops a body and ends a charge.

- **Glassback** (`Sky_Glassback`, `Sky_Grove`, `Sky_Mixed`): 24 to 26 m of open floor east-west, 18 north-south, rims on both long sides.
- **Fibre Wisp** (`Sky_Wisps`, `Sky_Mixed`, `Sky_Door`): every floor fibre is a **`ForceLine`** on its own object (`FibreStrand <fight> <i>`), its place at the fibre's south end on the stone and `end` its north end on the stone, `width` 1.8, `clamp` 0, as the section below asks. The drawn thread (.28 m over the stone) is a `FibreGlow` that reads the line's `Glow`: it brightens with a wisp's tell and a light runs on the stone under it. Sky_Wisps: x -7, 0, 7 from z 63.8 to 78.2. Sky_Mixed: (-4, 93.8) to (2, 108.2) and (8, 93.8) to (14, 108.2). Sky_Door: x -2 and 10 from z 149.8 to 161.
- **Pruner** (`Sky_Pruners`, `Sky_Door`): 20 x 16 m of open floor round its spawner.

## Review

Reviewer: Claude (world engineer B, chapter 7), from editor stills and plans, November 12; the chapter's coordinator reviewed the dressed sheet and asked for the look pass below. Nothing here has been walked in a player. **MAP_EYE_TEST: blockout FAILED on the first layout and PASSED with repairs; dressed (editor) PASSED with reservations, then a look pass after review; final in-player pass OPEN.**

### Blockout

Evidence: 23 grey stills in `Builds/quality/look/sky-grey-01/` (`OrielSkyfields-*`, `sheet.png`). The route built and baked first time (48 must-reach points, `ZONE_ROUTE_BAKE_OK`).

| Check | What the stills showed |
|---|---|
| Purpose and adjacency | Passed. The camp at the landing, the lesson at its wall, the first lift at its far end, each junction at a row's end where the next row starts. |
| Circulation and scale | Passed on the plan: one switchback line, each lift and junction the only way on, the grove, the stair, the sealed garden and the highest island as side trips. |
| Construction | Passed. Rows of stone at their levels, rims that stop a walk, roads and decks as solid spans the bake walks. |
| Immediate visual understanding | **Failed.** The rows were 22 to 26 m deep with the walked line down their middles, so the fixed camera saw only floor: the void, the fibres and the next island were in almost no play frame. The floor fibres were heavy white bars, the flagstones a giant regular grid, and the islands' faces read as wooden planking. |

Repairs: every row re-laid as a 16 m strip with its walk four to six metres in from its north rim (the River's rule); threads made thin; the stone repainted as smaller, irregular flags; the faces repainted.

### Dressed (editor)

Evidence: four dressing passes, `Builds/quality/look/sky-dress-02/` to `sky-dress-04/` (each with `sheet.png`); the last is the zone as built.

Found and repaired while dressing:
- the heightfield can only draw an island as a mesa standing on the floor of the void, so the rows read as terraces of one hill with brick faces: its faces are no longer drawn, and each island has a keel (a tapering underside of rock, light under the rim and darkening into the haze) with fibres running down from its rim into the cloud, which now lies 24 m down: the islands hang in the air;
- the cloud sheet the kit makes is cut away wherever ground stands over it, and under a floating island that hole showed the night panorama: the cloud is one unbroken sheet of its own;
- the glass trees read as snow: their glass is now clear aqua with a deep teal inside, a hard rim, sharp glints, and the stone seen through it;
- the borrowed column and arch pieces are the waystation's terracotta: the court's columns, the gate to nowhere and the Choir's door are built in the Skyfields' pale gold stone with bronze;
- every column is its own piece, so no box forty metres long stands on a rim;
- sand drifts and scree were orange: paler and greyer now.

Measured (`python tools/look_density.py Builds/quality/look/sky-dress-04 --match OrielSkyfields --max-blob 0.22`): every one of the 23 stills passes; the largest plain floor is 0.15 (`door`), the rest 0.10 or under. No stand-in enemies: the combat engineer's Glassback, Fibre Wisp and Pruner are defined.

| Check | What the stills show |
|---|---|
| Purpose and adjacency | Passes. A camp at the landing with its lesson at the wall; gardens of bare grit with their keepers on every island; the junctions where one island's row ends and the next begins; the Choir's door at the top. |
| Circulation and scale | Passes on the plan and in the bake (48 must-reach points): one switchback, the lifts and junctions the only ways on, the grove, the stair, the sealed garden and the highest island as side trips. Not walked. |
| Construction | Passes. Islands of paving on keels of bedded rock, held by fibres that run from collars on their rims down into the cloud; kerbs on every rim; roads of light and decks as solid spans; columns, beds, walls and stairs of the court's pale stone with bronze. |
| Terrain, ecology and arenas | Passes. Glass grows where fibres fray (on the rims, at the collars, on the keels); pale grass and lichen where the paving has broken; every fight floor is open stone with rims to end a charge and fibres to strike down. |
| Immediate visual understanding | Passes, with reservations below. Every play frame has the void and the next island's keel and threads at its back; the lifts and the cut junctions are the same road of light; the overviews read as islands floating over cloud. |

### Look pass (editor, after review)

The coordinator's review of `sky-dress-04`: twenty of twenty-three frames were the same tan flagstones, and the thing the zone is about was a thin white line. Six passes, `Builds/quality/look/sky-look-05/` to `sky-look-10/` (each with `sheet.png`); the last is the zone as built. The last pass took out the ground kit's wild path layer (irregular pavers), which the camp had used, by guide 0.1: the camp's wear and the fibre fields' glass are the court's own coursed flags. No route coordinate moved.

- **The fibres are the zone.** A hand's width of light, in their own colour: taut, blue-white with a blue halo; fraying (the junctions, the frayed gardens), amber, broken along their length and guttering in play (`FibreGlow.gutter`). They throw their light on the stone and walls near them: a band under every floor fibre and lift fibre, a pool round every collar, the camp wall's top and face lit by its fibre, two blue lamps there and an amber one at each fraying collar.
- **The pull shows where nothing is happening.** Every north rim has a fibre every dozen metres or so, and round the foot of every fibre (rims, fights, lifts, junctions, the long fibre's ends) grit and dust lie in lines that run in to it like filings round a magnet, sand drifts toward it and heaps against the collar, grass leans to it, petals trail toward it and stick to the thread, and water beads along it. In play a few motes drift in to each foot near a walk. At the camp the washing, the awnings' cords and the lantern's tassel all hang toward the fibre.
- **Each island has its own ground** (see Layout): the camp's worn paving, footfall, scorch and things lying about; the gardens' raked grit with ringed stones and stems and a swept path; the stair gardens' fans; the fibre fields' glass-grown plates, glass roots lit from inside and knots of glass; the forecourt's medallion, band, border and inlaid rings; the highest island's lifted slabs, broken ring, lichen and grass.
- **The Choir's front** has the Choir's own glass (`choir-glass`, `choir-rib`, the dungeon engineer's materials, so the two sides are one body) bursting through it between the buttresses and round the door, with its light along the cracks it opened; a window of that light over the door; glass massed on the cornice and hanging down the face; three ribs reaching over the top and gripping it; the doorway full of cold light with glass standing in it, and the light lying out across the threshold.
- **A sagging junction** hangs below both rims, rolled down on its free side, turned, its far end dropped; its top is broken ground (rubble, grit) between broken copings, not a deck. At the near collar the fibre parts into strands, most snapped and sparking. The cloud shows past its low edge. Its shade, and every island's, lies on the cloud where the sun throws it.
- **The keels** carry roots of old fibre, faintly lit, glass grown down the rock in veins and breaking out of it, and blocks and drums of the court's stone held in the rock or hung under it on old fibres.
- **The gardeners** kneel at a ringed stone in their beds (three, posed from their idle with no animator, so they stay down) with the rake laid by, or stand with a rake held upright (the follower carries its own); the sweeper has a broom. Plumb lines on the beds hang toward the nearest fibre.

Each island's shade lies on the cloud where the walk's camera sees cloud past a rim (about one and a half times its height on, away from the camera), not where the sun, standing off to the side, would throw it under the next island: the look's licence.

What is still weak, and would be the first things to look at in a player:
- **The rule's best moments are runtime and in no still**: the book falling to the wall, the water climbing the thread and the motes drifting in to each fibre (particle streams the editor doesn't run), a lift's ride, an island rising or dropping, a floor fibre lighting under a wisp's tell and a frayed fibre guttering (`FibreGlow`), the gardener and Ishen walking with the party. Each is tested or set by property block; none has been seen moving.
- **The first junction still reads partly as a slab sloping away from the rail** from the walk's own camera: the camera looks down past the near rim, so the near half of anything hanging in a 12 m gap is hidden behind it. The third junction, the overviews and the second junction read as hanging. Its shade on the cloud falls where the sun throws it, which is not always in the junction's own frame.
- **The stair gardens' fan raking is laid in quarter-metre cells**, so its edge along the swept path steps.
- **The keels' rock** is still one texture; what hangs from them carries the overviews.
- **The courier pod is the kit's Skiff**, a machine on the pad.
- **The kneeling gardeners are posed by hand**, so they do not breathe or shift; the standing ones idle with their rakes; none rakes.

### Tests

After the look pass (November 13, `Builds/quality/focused/oriel-sky-final-playmode.xml`): `SkyfieldsTests` 11 of 11 and all 27 of the zone's shared cases pass; of the zone-less checks, `EveryConversationLockAndQuestStepIsWiredToSomethingInTheWorldAcrossZones` lists this zone's `Landed on Oriel` (it waits on `visited.OrielSkyfields`, which `RouteChart` sets on arrival under a name it builds at runtime, so the check can't see it; Lastlight's landing is listed the same way) beside other zones' entries, and `DialogueInputTests("Home")` fails (not this zone). `ProgressionTests` (EditMode) passes with Lume's stall at three times what a shop pays per lot (Fibre Thread 99, Singing Glass 108 for 3).

`T/PlayMode/SkyfieldsTests.cs` (the built scene; 11 of 11 pass, `Builds/quality/focused/oriel-sky-2-playmode.xml`): the book falls sideways onto the wall as `IshenLanding` opens and a save with the rule seen has it there; a lift carries both heroes to the far node and gives back their hands and feet; the long fibre takes the party home and Meret's call waits there once the record is read; a junction seated or cut crosses the same gap, keeps the chest or loses it, and wrecks the garden and turns its keepers; a rail stops a walker where an island sags, and a junction's state loads with nothing played; the route is walked island to island once the junctions are worked and not before; the gardener keeps twelve paces, goes ahead to the frayed garden, sits at the door and leads to the sealed garden once three gardens are seated; Ishen walks with the party and holds back from a fight; every talk and point sets the flag, report and step the script and the quests expect; no wall or tall piece stands between the camera and a fight. The shared zone checks on this zone (`focused.ps1 -Zones OrielSkyfields`, November 12, `Builds/quality/focused/oriel-sky-playmode.xml`: 47 of 50, the three failures being two of this class's own, fixed and passing in the rerun above, and one of Harrowgate's dialogue) pass: prompts reachable on foot, no edge into nothing, no invisible wall, nothing floats or sinks, the body never sticks, the companion keeps up, nobody inside a prop, cliff bands, the camera at a wall foot; and the checks that take no zone (words on prompts and signs, locks, quest order and cards, item spelling). `FloorAndWallMeetInShade` is exempted by name for this zone (its faces are not drawn: the keels are).

### Coordinates as built

From `OrielBuilder.Skyfields` (its constants; `SkyfieldsTests` loads the built scene against the same numbers). x, z on the plan; y is the floor there: the camp 0, the terrace and the fibre fields 6, the sagging gardens, the stair gardens and the forecourt 12, the highest island 18. Every island's rim is a kerb .55 m high that stops a walk; a route's points keep to the walked lines below. Crossings start on walking through their box; everything else is pressed within its range.

**Walked lines** (the ground kit keeps them clear; a route's `walk`/`path` points can follow them): camp (-36, 13) (-15, 14) (0, 14) (12, 14) (20, 13) (26, 17.5); first lift to (26, 39.5); terrace (18, 43) (0, 43) (-22, 43) (-30, 47) (-30, 49), the grove (-30, 43) to (-48, 43); first junction (-30, 49) to (-30, 65); fibre fields (-20, 71) (0, 71) (20, 71) (32, 74.5); second lift to (32, 97.5); sagging gardens (22, 101) (6, 101) (-12, 103) (-30, 104) (-38, 107); second junction to (-38, 123); stair gardens (-31.5, 123) (-30, 129) (-10, 130) (10, 131) (20, 135), the sealed garden (-38, 123) (-43.8, 122.9) (-43.8, 126.4) (-43, 128) (both round the second junction's far bed and its keepers, which stand across the landing: November 14, chapter 7's routes; they ran through the bed's south wall before), the third lift's node (-36, 130) to (-40, 133); third junction (20, 135) to (20, 151); forecourt (10, 157) (4, 162) (4, 167), the long fibre (4, 162) to (-12, 159.5); the highest island (-56, 154.5) to (-62, 157.2).

| Kind | Id / name | x, z | Range or box | Flag it sets / needs | Notes |
|---|---|---|---|---|---|
| Spawn | `Arrival` | -36, 13 | | | From the Orrery's `Land — The Skyfields`; the chart's stop (Ishen's Camp) arrives here |
| Spawn | `Camp` | -20, 12 | | | The camp cradle saves here |
| Spawn | `ChoirDoor` | 10, 166.6 | | | From the Glass Choir (both its ways out) |
| Way out | `Launch — The Orrery` | -44, 10 (the pad) | 4 | open | To the Orrery's spawn `OrielSkyfields` |
| Way out | `Choir door — The Glass Choir` | 4, 168.2 | 3.6 | open | To GlassChoir `Arrival`; its sign at (8, 165.8) |
| Service | `Camp cradle — repair and save` | -25, 8.6 | 3 | | Free; saves at `Camp`; in the camp's safe pocket (80 x 20 m on (-8, 13)) |
| Service | `Fabricate — Lattice Anvil` | -22, 17.6 | | | In the safe pocket |
| Person | `Ishen` | -9, 15.6 at his crates | 3.2 | | First and repeat `IshenCamp`, post `IshenOrielPost` on `oriel.coronachHeard`; walks with the party from `oriel.ruleSeen` (`KeepsPace`, gap 3.6 m; his capsule is a trigger, so he never stands in the party's way) and is at his crates again from `oriel.coronachHeard` |
| Person | `Lume` | 9, 15.6 at her stall | 3.2 | | First `LumeFirst`, repeat `LumeRepeat`, post `LumePost` on `oriel.coronachHeard`; the zone's stall (`ShopInventory` OrielSkyfields) |
| Body | the gardener that follows | home -2, 19.2 by the old wall | | | No `Npc`. Twelve paces behind from `oriel.ruleSeen`; at (-10.6, 106.4) from `oriel.followerSeen` until `oriel.gardenerAsked` or `oriel.junction2`; in front to (-42.6, 130.4) once `oriel.gardensSeated`; on the Choir's step (-2, 166.6) from `oriel.doorReached` |
| Crossing | `Ishen at the landing` (`IshenLanding`) | x -15 | box 3 x 18 (z 4 to 22) | sets `oriel.ruleSeen`; report `Landing` | The book falls to the wall as it opens |
| Crossing | `Two gardeners in a walled bed` (`IdraEmptyGarden`) | x -6 | box 3 x 16 (z 35 to 51) | sets `oriel.gardenSeen` | The walled garden at (-14, 47.4) |
| Crossing | `The first junction` (`IshenJunction`, Taren) | x -22 | box 3 x 16 (z 35 to 51) | sets `oriel.junctionRead` | J1 at x -30 |
| Crossing | `Twelve paces behind` (`IdraGardenerFollows`) | x 22 | box 3 x 16 (z 93 to 109) | sets `oriel.followerSeen` | Two islands on from the walled garden |
| Crossing | `The Choir's doorstep` (`IdraChoirDoor`) | z 164 | box 48 x 3 (x -18 to 30) | sets `oriel.doorReached` | The gardener stops and sits on the step |
| Crossing | `Back into the camp` (`MeretOrielCall`, Meret) | x 19 | box 4 x 18 (z 4 to 22) | needs `oriel.coronachHeard`; sets `oriel.meretHeard`; report `CampReturn` | The long fibre sets down inside it; the first lift's walk back crosses it |
| Prompt | `Tap the glass` (`IdraGlassForest`, Taren) | -38, 45.6 | 3 | sets `oriel.forestHeard` | Optional |
| Prompt | `Look at the stair` (`IdraSweptStair`, Idra) | 18, 73.4 | 3 | needs `oriel.gardenSeen`; sets `oriel.stairSeen` | Optional |
| Prompt | `Look at the gardener` (`IdraGardenerWaits`, Idra) | -9.4, 104.8 | 3.2 | needs `oriel.followerSeen`; sets `oriel.gardenerAsked` | Starts EmptyGardens |
| Prompt | `Seat the junction` (frayed gardens 1 to 3) | (-14, 106.4), (-24, 134.4), (6, 134.4) | 2.8 | `oriel.garden1..3`; report `GardenJunction`; EmptyGardens step 0 | Bark `mend`. All three set `oriel.gardensSeated` (`FlagAll`) |
| Prompt | `Go in after the gardener` (`IdraSealedGarden`, Idra) | -43, 128 | 3 | report `SealedGarden`; sets `oriel.sealedGardenSeen`; EmptyGardens step 1 | The door opens on the flag |
| Prompt | `Set the lantern in the seat` (`IdraFarLantern`, Taren) | -62.4, 157.2 | 3 | report `LanternSeat`; sets `oriel.lanternSet`; LumesLantern step 0 | The lantern stands lit in the seat on the flag |
| Junction | `First junction` | span x -30 from z 51 to 63, level 6 | seat hand (-34.5, 48.8), cut hand (-25.7, 48.8), 2.6 | `oriel.junction1.seat` / `.cut`, either sets `oriel.junction1` | Nothing on it |
| Junction | `Second junction` | span x -38 from z 109 to 121, level 12 | seat (-42.5, 106.8), cut (-33.7, 106.8) | `oriel.junction2.seat` / `.cut`, `oriel.junction2` | A garden: the cut barks `garden` and turns its keepers (`Sky_Pruners`) |
| Junction | `Third junction` | span x 20 from z 137 to 149, level 12 | seat (15.5, 134.8), cut (24.3, 134.8) | `oriel.junction3.seat` / `.cut`, `oriel.junction3` | A chest (`oriel.sky.junction`): seated, it is on the deck at (22.1, 145.4); cut, it is gone |
| Lift | `First lift` | (26, 17.5) at 0 and (26, 39.5) at 6 | 2.4 | | `Ride the lift node` at each node; 2.6 s; the road between the rims (z 22 to 35) can be walked |
| Lift | `Second lift` | (32, 74.5) at 6 and (32, 97.5) at 12 | 2.4 | | Road z 79 to 93 |
| Lift | `Lift to the highest island` | (-40, 133) at 12 and (-56, 154.5) at 18 | 2.4 | | Road (-43, 137) to (-51.9, 149) |
| Lift | `The long fibre's node` | -12, 159.5 at 12 | 2.4 | | `Ride the long fibre — Ishen's Camp`: one way, 7 s, sets down at (20, 10) in the camp |
| Cache | `oriel.sky.grove` "A gardener's chest" | -55, 47.6 | 2.6 | | The glass forest |
| Cache | `oriel.sky.junction` "A gardener's tool chest" | on J3's island | 2.6 | after `oriel.junction3.seat` | Lost if the junction is cut |
| Cache | `oriel.sky.high` "The chain's old stores" | -68, 153 | 2.6 | | The highest island |
| Fight | `Sky_Glassback` | 10, 43 | 26 x 16 | | 1 Glassback from (6, 47.5) |
| Fight | `Sky_Grove` | -48, 43 | 18 x 14 | | 1 Glassback from (-51, 47) |
| Fight | `Sky_Wisps` | 0, 71 | 24 x 16 | | 2 Fibre Wisp from (-6, 75), (6, 67); fibres x -7, 0, 7 |
| Fight | `Sky_Mixed` | 6, 101 | 26 x 16 | | 1 Glassback from (2, 105.5), 2 Fibre Wisp from (12, 97); fibres (-4, 93.8)-(2, 108.2), (8, 93.8)-(14, 108.2) |
| Fight | `Sky_Pruners` | -34, 129 | 20 x 16 | needs `oriel.junction2.cut` | 2 Pruner from (-42.6, 124.4), (-33.6, 124.8) |
| Fight | `Sky_Door` | 4, 155.5 | 24 x 13 | | 2 Pruner from (4, 159), 1 Fibre Wisp from (4, 152); fibres x -2 and 10, z 149.8 to 161 |
| Sign | floor signs | ISHEN'S CAMP (-12, 13.2), LUME'S STALL (3.8, 13), LAST LANTERN (14.3, 13.8), LIFT NODE by each lift's lower node, WALLED GARDEN (-6.6, 42.8), SEALED GARDEN (-40.8, 124.4), HIGHEST ISLAND (-60, 152.6) | | | The manifest's list; nobody is left to put up more |

## For the combat engineer's enemies (Fibre Wisp, Glassback, Pruner)

*The combat engineer's section (November 12): what the Skyfields' fights need from the floor. The definitions are `OrielBuilder.Enemies.cs`; the pieces are in CHAPTER_GUIDE 15.23; the behaviours are tested on a bare floor in `T/PlayMode/OrielCombatTests.cs`.*

| Thing | What it needs | Why |
|---|---|---|
| A fibre a wisp rides | One **`ForceLine`** (`S/Combat/ForceLine.cs`) per fibre in a wisp fight, on an empty object at one end of the fibre, `end` its other end, both **on the floor** (a fibre drawn "low over the stone" gets its line on the stone beneath it), `width` left at 1.8, `clamp` 0. In `Sky_Wisps` the three north-south fibres 7 m apart; in `Sky_Mixed` and `Sky_Door` the fibres those floors carry | A wisp rides the nearest `ForceLine` within 16 m, lights it and strikes down it, and hops to the one a hero stands on. With none near, it makes a line of its own through the hero, so a fight with no fibres laid still works but no longer teaches the zone's rule. Anything drawing a fibre may read `ForceLine.Glow` (0 to 1 while lit for a strike) to brighten it |
| Between fibres | At least 4 m of clear floor between two fibres of one fight (7 m is good) | A strike covers 1.8 m: the answer is the floor between the lines |
| A Glassback's floor | A rim or rock within the 12 m of its run on at least one side of where it is met | Its charge ends stuck in a wall (open 2.2 s); its plates lie where it stops as a 2.6 m ring that cuts (5 s), so leave room to fight round it |
| A Pruner's garden | Its spawner where it tends; open floor 5 m round it | It turns a 4.4 m ring of blades with a hole 1.7 m across in the middle, and keeps to 12 m round where it was set |
| Barks | Nothing to wire: the wisp says `wisp` as it lights a fibre, the Glassback `glassback` as it sheds its plates (the party's lines, manifest section 7). `garden` is the zone's (the garden wrecked) | |
