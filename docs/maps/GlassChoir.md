# The Glass Choir - the nave, the stays and the engine hall

Functional brief, written before construction (November 12, world engineer C, chapter 7). **MAP_EYE_TEST: blockout PASSED with repairs; dressed (editor) PASSED with repairs; final in-player pass OPEN** (review below; an agent's review, nobody else has looked). Chapter brief: [ch07-the-cage-makers.md](../chapters/ch07-the-cage-makers.md); script wiring: [ch07-manifest.md](../writing/pending/ch07-manifest.md). Builder: `S/Editor/World/OrielBuilder.GlassChoir.cs` (everything in its nested class `OrielBuilder.Choir`). Mechanics: [CHAPTER_GUIDE.md, "World mechanics"](../CHAPTER_GUIDE.md), 15.18 and 15.19 (`BridleStay`, `BridleHold`).

## Purpose and formation

A cathedral the court of Oriel built round the adult Choir it bridled, a thousand years ago. The Choir died held: its body went to glass where it lay, vast and translucent, and the cathedral stands along its flank like a gallery along a ship. The Bridle that holds it is still working. Four clamps grip the body; from each clamp's works the court strung **stays**, fibres of the old road drawn taut, and every stay holds a piece of the body still: a rib pressed flat across the gallery, a fold of glass shut over a niche. The light inside the glass has not moved since.

The court's builders were the same hands as Lanternwick's relays and the Drowned Waystation (pale carved stone, bronze gone green, the ring-and-bar works mark), here at their grandest: a nave of pale gold stone and bronze, the body on its east side the whole way down.

**The route's shape is a descent along the body.** In at its tail by the great door, along the nave beside it, down past the first clamp, down three flights between ribs, to the landing over the heart and the engine hall under it. North is always on, and down; the body is always on the right.

**The one rule: a stay held taut pins the glass, and a stay eased lets it go.** The hand is a bronze ratchet seat beside each stay: *Take the stay's weight*. The stay pulls (its fibre goes hard white and the rib it holds dips and groans), then eases (the light runs out of it, it drops slack and amber) and the rib it held lifts back into the body, out of the way. It is the first two lines of the release, done with the hands, three times before the engine hall asks for the third. Introduced with nothing awake (the rib walk, straight after the release is read at the first clamp); tested (the sentinel bay: the stay that opens its far side also cracks the glass that holds two Glass Sentinels, and they wake); combined (the fibre gallery: Fibre Wisps riding the stays and a sentinel awake while the stay is worked, and the stay's slack wakes the Clamp Tenders that keep it). A fourth stay, optional, pins a niche in the body shut over a cache.

## Users and access

Nobody lives here. The gardener that follows the party sits down at the door outside and will not come in (`IdraChoirDoor`, the Skyfields'). Ishen comes in with the party and keeps at its heels the whole way down, holding back while anything is awake (`KeepsPace`, as in the Skyfields: never in a fight); he stays on the stair through the Warden's fight and walks to the record wall once it has stood down. The Last Warden is in the engine hall, as it has been for a thousand years.

The party arrives from the Skyfields through the great door (spawn `Arrival`) and leaves the same way (`Choir door — The Skyfields`, to the Skyfields' spawn `ChoirDoor`). There is no shortcut out of the engine hall, on purpose: the walk back up is the payoff: once the Bridle has let go, every stay lies slack, every rib stands in the body, and the light through the glass moves the whole way up.

## The silences (manifest section 8, item 3)

- **The nave.** No zone bark (the zone's definition sets `quietArrival`, which `BarkService.PlayArrival` honours: D262), no discovery bark, no enemy and no sign but THE NAVE for the first 95 m. The body is in view on the right from the first step. Then `SelaGlassChoir` at z 96, and nothing after it until the first clamp.
- **When the Bridle opens.** `WardenAsks` ends on Ishen; then the light moves through the glass and nobody says so (`BridleHold`: every stay's pulse stops, the fibres go slack and amber, the shafts of light through the body begin to move). No bark.
- **The gardener** is not in this zone.

## Layout

About 60 x 330 m. x runs east, z north; heights are world y (the door's threshold is 0). The body lies east of the route the whole way, its spine about 18 m east of the gallery's middle and descending with it; its chest curls west over the engine hall's north rim with the heart in it.

```text
 z 312   .-''''''''''''-.   the road's knot (cool, in the body's chest), the Bridle engine's works on the north rim
         | NW          NE|
 z 292   |     (C)       |  ENGINE HALL  floor -10, C = (0, 292), radius 18      record wall (west rim)
         | SW          SE|  four clamp places (the combat engineer's contract)
 z 274   '-.____[ ]___.-'   seal 272.6; WardenFirst at the stair's foot
 z 264        |stair|       -6.6 to -10
 z 252   [ HEART LANDING -6.6 ]  checkpoint: spawn Door, cradle; the heart (warm) in the body beside it   BODY
 z 248   ======rib 3======  stay 3                                              (glass,
         | FIBRE GALLERY  |== niche (rib 4, optional stay, cache)               east of
 z 214   |  -6.6          |                                                      the route
 z 208        |stair|        -4.4 to -6.6                                        all the
 z 205   ======rib 2======  stay 2                                              way down)
         | SENTINEL BAY   |== breach into the body (cache, Singing Glass)
         | -4.4  casings  |
 z 172   |   rib walk     |
 z 166   ======rib 1======  stay 1 (introduced: nothing awake)
 z 150        |stair|        -2.4 to -4.4
         | FIRST CLAMP    |  the clamp grips the body on the east side; the release cut on its housing
 z 118   |   -2.4         |
         |   NAVE  -1.2   |== OVERLOOK (z 96: Sela's line, the place to stop)
 z 60    |   NAVE  0      |-- arch -- CLERKS' CHAPEL (cache)
 z 12    [  NARTHEX  0    ]  cradle
 z -5        great door      to the Skyfields (spawn ChoirDoor there); the body's tail tapers to its end beside it
```

| Area | What it is for | Size | Level | Combat |
|---|---|---|---|---|
| Narthex | The arrival inside the great door: the way out, a free cradle, the first sight of the body's tail on the right | 18 x 18 m | 0 | none |
| Nave | The silence: 105 m of gallery beside the body, two shallow flights down. Its beat: the colonnade's stumps every 10 m on the west (some whole to the capital, some broken off) with what stood between them (a lamp, a clerk's stall, a fallen capital), and the bands of shade the body's ribs lay across the floor on the east | 11 x 106 m | 0, then -1.2 | none |
| Overlook | Where Sela speaks (z 96) the nave steps out over the trench toward the body: a railed bay, a bronze ring in its floor, its far rail a stride from the glass. The place to stop | 3.4 x 9.2 m | -1.2 | none |
| Clerks' chapel (optional) | Off the nave's west side through an arch: the court's clerks' desks, their ledgers, a coffer | 15 x 18 m | 0 | none |
| First clamp | The release read: the first clamp gripping the body on the room's east side, its housing turned to the gallery with the maker's lines cut in it | 17 x 32 m | -2.4 | none |
| Rib walk | The rule introduced: a stair down, then rib 1 pressed across the gallery, its stay's seat on the near side; nothing awake | 10 x 22 m | -4.4 | none |
| Sentinel bay | The rule tested: a wide bay with two Glass Sentinels held in glass casings on its west side; stay 2 opens its far side and cracks the casings | 18 x 36 m | -4.4 | `Choir_Sentinels` (after stay 2) |
| Breach (optional) | Off the bay's east side, a hollow where the body's glass broke open: Singing Glass, a glass-cutter's chest | 10 x 14 m | -4.4 | none |
| Fibre gallery | The rule combined: stays strung across overhead, Fibre Wisps riding them and a sentinel awake; stay 3 opens the way to the heart, and its slack wakes the Clamp Tenders | 16 x 36 m | -6.6 | `Choir_Fibres`, then `Choir_Tenders` (after stay 3) |
| Niche (optional) | A fold of the body pinned shut by rib 4; its stay is optional; a coffer of the court's | 6 x 8 m | -6.6 | none |
| Heart landing | The boss door: checkpoint spawn, free cradle, the stair down | 16 x 12 m | -6.6 | none |
| Engine hall | The Last Warden's arena (contract below), the record wall on its west rim | 36 m across | -10 | `LastWarden` |

**Travel** (about 6.7 m/s): door to the first clamp 135 m (20 s); the clamp to the landing 120 m; the stair and the hall 40 m. About 45 seconds of walking, three stays, three fights, two pockets and four talks, then the boss: the brief's share of 55 to 70 minutes is 20 to 25.

## The stays (the zone's mechanic)

| Stay | Seat (stand here) | Holds | Flag | What easing it changes |
|---|---|---|---|---|
| 1 | Rib walk, west side, south of the rib | Rib 1, across the gallery at z 166 | `oriel.stay1` | The way down to the bay opens. Nothing wakes |
| 2 | Sentinel bay's north-west, south of the rib | Rib 2, across the bay's north exit at z 205.5; and the two casings | `oriel.stay2` | The way down to the fibre gallery opens; the casings crack and the two sentinels wake (`Choir_Sentinels` takes the flag as its `requiredFlag`) |
| 3 | Fibre gallery's north-west, south of the rib | Rib 3, across the way to the landing at z 248.5 | `oriel.stay3` | The way to the heart landing opens; the stay's slack wakes two Clamp Tenders at the gallery's north (`Choir_Tenders`) |
| 4 (optional) | Fibre gallery's east side, south of the niche | Rib 4, across the niche's mouth | `oriel.stay4` | The niche opens: the coffer inside |

- Each stay is a `BridleStay` (new, `S/World/BridleStay.cs`, a `WorldSwitch`): its hand is a `DiscoveryPoint` beside it ("Take the stay's weight", report `Stay`), and the switch plays the pull and the ease (marker `STAY_EASED <flag>`, then `STAY_SLACK <flag>`). What it blocks is a `Membrane` box with a carving obstacle under the held rib, hidden by a `FlagGate` on the same flag (a door is never the switch's business). A save with the flag loads with the rib up and nothing played.
- Standing at a seat, the view draws back over the rib and whatever it holds (`shows`), as a pump's does over its water.
- The Bridle's held fibres (each stay's, and the lines strung through the gallery and the engine hall) carry a slow white pulse running toward the engine: the hold, still pulling. `BridleHold` (new, `S/World/BridleHold.cs`) draws it, and on `oriel.wardenStoodDown` stops it, lets every fibre go slack and amber, and sets the light through the body moving.

## Identities

Scene `GlassChoir` (GroundCombat, profile Oriel, threat 1, chapter 7). Music: Glass Ruans; the Last Warden's own track is the combat engineer's (Boss of the Ages). Ambience: `AMB_GlassChoir` when tools/oriel_ambience.py makes it.

Light (`GlassChoirLooks`, and the key's direction set by the builder, `Choir.SunYaw`/`SunPitch`): **the body is the light in the room.** One low cool key (24 degrees) comes from the east, through the glass, so everything in the gallery has a lit side toward the body and a long shadow away from it; the glass is lit through (no dark side) and carries its own light, so it is the brightest broad thing in every frame; the ambient is low and cool, and the stone's shade is a deep blue band, not a tint. The body's ribs lay bands of shade across the floor (shadow-only casters inside the glass, `The body's rib shadows`). The court lamps are cool; **the heart by the landing is the one warm light in the building.** The court's floor is its own large dressed slabs in courses (no crazy paving), worn smooth down the middle, sand only in corners and against the west walls, glass dust only in the hollows; a bronze line runs the whole walk, a border along the balustrade, a ring in the overlook, and in the hall a rim ring, a ring round each clamp's seat and the twelve spokes inlaid under the fibres. The cradles stand on plinths of the court's pale stone banded in bronze, the house's spanner mark kept.

## Coordinates (for routes and tests)

Written before the build; the builder's named constants (`OrielBuilder.Choir`) are the truth, and this table is updated from them as built.

| What | Id or prompt | x, z | Flag / note |
|---|---|---|---|
| Arrival spawn | `Arrival` | 0, 3 | |
| Way out | `Choir door — The Skyfields` | 0, -4 | to the Skyfields' spawn `ChoirDoor` |
| Narthex cradle | `Choir cradle — repair and save` | -6, 6 | free, saves at `Arrival` |
| Nave talk (crossing) | `SelaGlassChoir`, report `Nave` | box at -0.2, 96 (14.4 x 3, across the nave and the overlook) | sets `oriel.choirSeen` |
| Overlook | (no prompt) | floor x 3.2 to 6.6, z 91.4 to 100.6; its ring at 5, 96 | the place to stop beside Sela's line |
| The tail's end | (scenery) | from the skin at about 8.2, 1.5 to its tip at 5.4, -5 beside the great door, on the trench's shallow floor (-1.4) | the Cantor's tail mesh in the core's milky glass |
| The heart | (scenery) | 10.6, 258, a metre and a half under the landing's floor, in the body's core | the one warm light (`The heart's warmth`) |
| Clerks' coffer | cache `oriel.choir.clerks` | -22, 51 | |
| First clamp talk (crossing) | `IshenFirstClamp`, report `FirstClamp` | box at -4.2, 137.5 (17 x 3), beside the clamp (1.4, 137) | sets `oriel.releaseLearned` |
| Stay 1 | `Take the stay's weight` | stand -5.4, 162.4 (rib 1's root at 5.8, 166) | `oriel.stay1` |
| Sentinel casings | `Choir_Sentinels` (centre -5.5, 189.5; 19 x 32) | -11.2, 183 and -11.2, 197 | 2 Glass Sentinel, need `oriel.stay2`; shown in their glass before (preview at -6, 190, radius 16) |
| Stay 2 | `Take the stay's weight` | stand -8.2, 201.8 (rib 2's root at 5.8, 205.6) | `oriel.stay2` |
| Breach chest | cache `oriel.choir.breach` | 11, 192 | Singing Glass `oriel_glass_0`, `oriel_glass_1` beside it |
| Fibre gallery | `Choir_Fibres` | centre -4.2, 229.5 (16.5 x 30): wisps from -6, 224 and 0, 234, the sentinel from -4, 236.5 | 2 Fibre Wisp, 1 Glass Sentinel |
| Stay 3 | `Take the stay's weight` | stand -7.4, 245.4 (rib 3's root at 5.8, 249.2) | `oriel.stay3` |
| Tenders | `Choir_Tenders` | centre -4.2, 242.5 (16.5 x 15); from -10.2, 240.5 and 1.6, 239 | 2 Clamp Tender, need `oriel.stay3` |
| Stay 4 (optional) | `Take the stay's weight` | stand 2.2, 222.6 (rib 4 lies along the niche's mouth from 4.9, 235.4) | `oriel.stay4` |
| Niche coffer | cache `oriel.choir.niche` | 7.5, 230 | |
| Door spawn | `Door` | -3, 257.5 | the boss checkpoint |
| Landing cradle | `Heart cradle — repair and save` | -8.2, 255.8 (the landing's west side) | free, saves at `Door` |
| Warden talk (crossing) | `WardenFirst` | box at 0, 273 (9 x 2.2), needs `oriel.releaseLearned`, saves at `Door` | sets `oriel.wardenMet` |
| Record wall | `Look at the record wall`, report `RecordWall` | -13.9, 300 (the slab at -16.9, 301.8 on the west rim) | talk `IshenReads` (speaker Ishen), needs `oriel.wardenStoodDown`, sets `oriel.coronachHeard` |

## Encounter plan

| Fight | Where | Pack | Where the player stands to answer it |
|---|---|---|---|
| `Choir_Sentinels` | Sentinel bay, after stay 2 | 2 Glass Sentinel, out of their casings on the west side | Behind them: the bay is 18 m wide and 30 deep with nothing standing in it, so a guard turning at its pace can be walked round; neither casing is within 6 m of a wall it could pin a hero to |
| `Choir_Fibres` | Fibre gallery | 2 Fibre Wisp, 1 Glass Sentinel | Off the lit fibre: the three stays strung across the gallery are 9 m apart, so there is always unlit floor between two; the sentinel holds the gallery's middle and is walked round as in the bay |
| `Choir_Tenders` | Fibre gallery's north, after stay 3 | 2 Clamp Tender, from the hatches either side of the stay | Stop them first: they come out 6 m from the seat and walk to it, fragile, across open floor |
| `LastWarden` | Engine hall | The Last Warden | Contract below |

## Camera

The fixed camera looks north-north-east from the south-south-west. The body is east of every walked floor and lower than its crest only beyond the gallery's balustrade, so no glass stands between the camera and a hero: what is see-through is beside or beyond the party, never on the camera's side of it. A rib lifted by its stay stands over the body, east of the walk. Walls follow the Drowned Waystation's rule (the sightline to the floor's edge, D119): every wall with a floor beyond it along the camera's line stays under it; the west side of every room is low, the north and east may be tall. Nothing see-through lies over a fight's floor, and the body's own light is kept under the brightness of a danger marking where it shows beside a fight.

## Review

Reviewer: Claude (world engineer C, chapter 7), from editor stills and plans, November 12. Nothing here has been walked in a player.

**Blockout** (`Builds/quality/look/choir-blockout-01/`, `-choir-greybox`). The route reads as a descent beside a body: a pale gallery with a bronze balustrade, the glass on the right the whole way, the first clamp and its tablet (the four lines read at the play camera; the tablet was later taken away, see "For OrielBuilder.cs"), the ribs across the way with their seats, the bay, the gallery's three fibres, the landing, the stair and the round hall with its twelve spokes. The navigation bake reached all 38 must-reach points first time. Repaired before dressing: the body read as a greenhouse (regular arches over a row of white pearls: ribs now sweep down over the flank at uneven spacing, some broken, the spine knuckled and dull, plates of clear glass along the crest, and a milky core inside the skin so it reads as a mass of glass with light in it rather than a pane over a pit); walls the sightline rule lowered rose as pointed brick pyramids (walls now stand their full height with a level top where the rule allows and are a hip-high parapet with a level aisle behind where it doesn't); the heart was a white blowout; the hall's clamps didn't face C; the record wall stood on top of the wall instead of in it.

**Dressed** (`choir-dressed-01` to `-09`; the last is the scene as it stands). `python tools/look_density.py <folder> --max-blob 0.22`: every still passes (worst .09 on the first dressed pass, .10 on `-09`, 23 stills). Repaired while dressing: the kit's terracotta columns stood as huge orange drums in the nave's foreground (now the court's own pale fluted columns, banded in bronze, set into the wall); light shafts built as quads read as solid white blades (now soft bands drawn by a line turned to the camera); the fibres were glaring white lines (thinner, in their held colour, a pulse running in at runtime); the held ribs read as flat ribbons floating over the floor (faceted, a lit marrow down the middle, a shadow, and heaped broken paving where each presses the floor); the breach was a thicket of white: first the skeleton's bones in it, then the crest's plates and the skin on the camera's side of it (every piece of the body that stands on the camera's line to the breach or the niche floor is cut away by the walls' sightline rule, so the breach is an open wound in the flank), and lumps of broken glass built on its floor, which drew as long white blades across it from the fixed camera (gone: the breach keeps its Singing Glass and small crumbs of glass; found by rendering the breach with each suspect switched off in turn); the gallery's north end was wider than rib 3, so the way to the landing went round it (`GlassChoirTests` found it; the way out is now a passage the rib closes); the landing's cradle stood past the balustrade in the trench, and of the two columns on the hall's west rim one was sunk to its capital in the wall (a disc on the wall's top) and the other stood over the trench (the shared zone tests found both: the cradle is on the landing now and the hall has no columns); the first clamp's plaque (see "For OrielBuilder.cs").

**Look pass** (November 13, after the coordinator's review of `choir-dressed-09`: "a sunlit sand-coloured courtyard with a garden path"; stills `choir-look-01` to `-03`, the clamps open in `choir-open-01`). Light: the low cool key from the east through the body, a low cool fill, the glass lit through with its own light, deep blue shade, the ribs' bands of shade across the floor. Floor: the crazy paving, the lichen, the grass tufts and the loose-slab decals are gone; the court's slabs, worn pale down the walk, sand only in corners and against the west walls. The nave's beat (stumps every 10 m, a lamp, a stall or a fallen capital between) and the overlook at z 96. The tail tapering to its end beside the great door (the Cantor's tail mesh in the core's glass); the warm heart by the landing. The cradles' plinths. The breach's grey field was the trench's floor seven metres down showing through the cut flank: the core now stays on the hollows' far side and a back of dense glass rises there. The hall's clamps open on a hinged jaw. Density: worst .21 of .22 (`choir-look-03`, 25 stills), higher than before because the floor now carries bands of shade.

What is still weak:
- **The tail reads as the body thinning, not yet as a tail**: from the arrival the glass narrows toward the door, but the plated end lies under the frame's bottom edge and its plates are lost in glass.
- **The stone is cool grey-gold**, the price of a cool key; it is dimmer than the body in every still, but the court's gold now shows only in the lit bands.
- **The body reads as glass, not yet unmistakably as a creature** in a play frame: the camera sees its near flank and the inside of its ribs, never its shape. The overviews show the spine, the ribs and the crest's plates; a play frame shows a glass slope with bone in it. A head or a limb end somewhere a frame can take it in whole would help; none is built.
- **The stood-down Warden is the boss prefab in its own rest pose**: there is no staff-lowered clip.
- **Nothing here has been seen in a player**: the stays' pull and ease, the pulses along the fibres, the shafts and lights moving once the Bridle lets go, the gel's drifting veins, the casings cracking, Ishen at the party's heels, the tenders setting a stay again: all runtime, in no still.

## For OrielBuilder.cs

What the main quest's steps find here (`stepTargets`, `CageMakers`):

| Step | Report or speaker | What carries it |
|---|---|---|
| 4 Walk the nave | report `Nave` | the `SelaGlassChoir` crossing (sets `oriel.choirSeen` by its node) |
| 5 Follow the body down to the first clamp | report `FirstClamp` | the `IshenFirstClamp` crossing (sets `oriel.releaseLearned` by its node) |
| 6 Open the four clamps (Interact x4 `Clamp`) | report `Clamp` | the four clamps' hands in the engine hall (the combat engineer's contract says what they are; the world stands them) |
| 7 Answer the Warden | speaker `Warden` | the combat engineer's (contract) |
| 8 Hear Ishen read | report `RecordWall` | the record wall's point (node `IshenReads`, speaker `Ishen`) |

World flags this zone sets that the story keeps: `oriel.stay1`, `oriel.stay2`, `oriel.stay3` (and `oriel.stay4`, optional), `oriel.wardenMet`. For `q.storyFlags`: the first three.

**The release is first spoken here** *(from world engineer A, November 12)*. `IshenFirstClamp` is where the principle the rest of the game turns on is read aloud for the first time ("Take its weight. Wait for the slack. Let go together." and, smaller, "Then give it room."), and it is said back only once more, by Taren at the end of the chapter. So the first clamp's room is the chapter's quietest frame and has to carry the lines without help:
- **The clamp is the subject of the frame as the talk opens.** From the crossing (-3.5, 129) the fixed camera sees 7 to 9 m north and a little east: the clamp's housing, with the maker's lines on it, stands there, turned to the gallery, with nothing tall between it and the camera. If the crossing has to move to make that true, move it and write the new box here; the quest's arrow follows the report, not the number.
- **What Taren says is visibly true of it**: "There's a seat for a hand here, and here." Two hand seats at standing height on the housing's gallery side, and the lines cut above them, drawn (lit grooves or inlay, not a floor sign or a label: a screen never prints the words, Ishen does).
- **Quiet**: no encounter awake within earshot, no bark, nothing moving but the body's light. The talk is 8 lines, about 40 s, all compulsory.
- **The clamp grips the body** (the dead Choir east of the gallery): the cage holding what it holds is what the words are about.
- *Seen in `Builds/quality/look/choir-blockout-01/GlassChoir-clamp.png` (blockout):* the lines are on a wooden plaque on a post standing on the floor in front of the clamp, and the clamp itself is cut off by the frame's top edge, behind the plaque. As the script has it the release is on the housing ("a line cut on the housing", "a seat for a hand here, and here"), so the plaque reads as a museum label and the clamp as scenery. Asked: the lines and the two seats on the clamp's gallery face, the clamp whole in the frame from the talk's crossing, and no plaque.
- *Done (world engineer C, `choir-dressed-08` on):* the plaque is gone. The kit's clamp already carries both on the face it turns to the gallery: the maker's ring-and-bar lines cut in gold along its arch, and two hand seats, basins at its foot either side of the pillar. The crossing moved north to (-4.2, 137.5), level with the clamp, which is where the play camera has the whole clamp in the frame (`GlassChoir-clamp.png`); at (-3.5, 129) the frame's top edge cut off the arch with the lines on it.

## For OrielSkyfields

The Choir's door is a way between the two zones: the Skyfields' gate into the Choir names spawn `Arrival` here, and both ways out of the Choir name the Skyfields' spawn **`ChoirDoor`** (a spawn on the Skyfields' side of the door, clear of the gardener's place).

## Boss arena contract

*The combat engineer's section: what the Last Warden fight needs from the room (components, sizes, anchors, the four clamps and their hands, what must be breakable or reachable, how the Warden stands down and where it goes). The world engineer builds to it and writes the coordinates as built underneath.*

Written November 12 by the combat engineer. The fight's moves are `S/Combat/BossController.LastWarden.cs` (every number in its `WardenTune`); the clamp is `S/Combat/BridleClamp.cs`, a fibre the force runs down is `S/Combat/ForceLine.cs`, and what a Clamp Tender keeps is `S/Combat/Tended.cs`; the definitions are `OrielBuilder.Enemies.cs`. Its tests (`T/PlayMode/OrielCombatTests.cs`) build a stand-in of this room on a bare floor, so nothing here waits on the dungeon to be tested. Positions are from the hall's centre **C = (0, 292)** at floor **-10**, +z north; a bearing is degrees clockwise from north.

**How the fight uses the room.** The Warden holds the engine at C and never strays more than 3 m from it. Every attack it has runs down a fibre: a fibre brightens (a marked lane along it), then force runs down it. The hall's twelve fibres run out from the engine like spokes, three to each clamp. The Warden's hold on the road is its bar, and the bar stops at four lines, one for each clamp still shut (**three quarters, half, a quarter, nought**): fought down to the next line, it holds there (THE ROAD IS HELD · OPEN A CLAMP) and the clamps' seats light. A hero stands in a lit seat and takes the clamp's weight (`Take the clamp's weight`, the Interact button, which answers inside this fight); the clamp pulls, and eases only while the Warden's staff is down (an opening: its Draw closing on nobody, or a break). Held through the ease, the clamp opens onto its catch, its three fibres go slack (fewer lines, more floor), the Warden counts it (`clamp.open`), and the bar goes over its line. The second is the shift (two Clamp Tenders step out beside the two clamps still shut). The fourth ends the fight: all four let go together, the Warden lowers its staff and stands where it is. It is not beaten down and it does not leave.

| Thing | What the fight needs | Why |
|---|---|---|
| Floor | Round, **radius 18 m** walkable from C, near level (no step over .3 m inside r 17). Nothing solid inside **r 12** but the boss; the four clamps' bodies stand outside r 12.5 | Its lanes run 15 m from the engine and a Chord marks three of them at once: a column inside that stops a sidestep between two lines. The partner covers from anywhere on this floor |
| The four clamps | One **`BridleClamp`** each, on an empty object **at its seat** (the floor a hero stands on to work it, the prompt's place), **r 11** from C on bearings **45, 315, 225, 135**, its forward toward C. Fields: `index` **1 NE, 2 NW, 3 SW, 4 SE** (anticlockwise as the camera sees it, so nothing depends on the order the scene woke them); `settledBy="LastWarden"`; `openedFlag` **`oriel.clamp1`** to **`oriel.clamp4`** by index; `whole` = the clamp shut (the `BridleClamp` prefab, 4.2 m, its jaw closed, standing **3 m behind the seat**, at r 14, facing C); `onCatch` = the same clamp opened onto its catch (jaw swung back), hidden until it opens; `released` = the same let go (jaw open, fibres hanging), hidden until all four let go. Any of the three may be missing. Leave its prompt and range alone: the clamp sets them | The seat is what the player stands on and what the prompt measures to. The clamp draws its own seat ring (a pale cream boundary, never a danger's colour) while it can be worked, and fills it as its weight is taken. The fight sets each clamp's flag as it opens and reports `Clamp` to the quests (step 6, Interact x4); a clamp loads let go once `bossdown.LastWarden` is set, and shut on any other load (a lost fight starts with four) |
| The clamp's body | A solid static box round each clamp's body (in the navigation bake), nothing over the seat itself; the seat on the paths. `SceneryOcclusion` and the fade on the two south clamps (SW, SE: the camera's side) | It is scenery the party fights round; the seat must be reachable from anywhere on the floor |
| The fibres the force runs down | **Twelve `ForceLine`s**, each on an object at its inner end **(r 2.6 from C, on the floor)** with `end` its outer end **(r 17.5, on the floor)**, on bearings **15, 45, 75, ..., 345** (every 30 degrees). `clamp` is the index of the clamp whose sector it lies in: 15/45/75 clamp 1 (NE), 285/315/345 clamp 2 (NW), 195/225/255 clamp 3 (SW), 105/135/165 clamp 4 (SE). The middle one of each three runs under its clamp's seat. `width` left at 1.8 | Where the force strikes is the strip of floor under the fibre: a fibre strung above the floor gets its `ForceLine` laid on the floor beneath it. A clamp opened makes its three slack: they carry nothing from then on. Anything that draws the fibre (`BridleHold`) may read `ForceLine.Glow` (0 to 1 while it is lit for a strike) and `ForceLine.Slack` from the same object, to draw it white-hot and then amber; the marking on the floor is the tell either way |
| Boss | Its `Spawner` at **C**, facing the door (yaw 180), `faceAsPlaced=true`, radius 0. `previewPoint` on the stair with a radius that takes in C, so it stands there shown and inert (`DormantEnemy`) until `WardenFirst` | It is in the room at the first look, and has held the engine for a thousand years |
| Door | South, encounter id **`LastWarden`**, trigger covering the floor (about 38 x 8 x 38 on C), `requiredFlag="oriel.wardenMet"` (the `WardenFirst` crossing), `checkpointSpawn` **`Door`** on the heart landing (within 60 m, on the paths, the free cradle beside it). One `Membrane` seal across the stair's foot, strands drawn instead of its cube | The shared door tests: checkpoint, retry, no way out mid-fight. The boss sheet in `OrielCombatTests` names zone `GlassChoir`, door `LastWarden`, door flags `oriel.releaseLearned`, `oriel.wardenMet` |
| Clamp Tenders | Open floor **2.5 m either side of each seat** (toward the clamp's flanks), clear of props and on the paths: at the shift the Warden calls a `ClampTender` out beside each clamp still shut, on a pale marked ring | A tender walks to the seat a hero is working and closes the clamp while it stands there: it has to be reachable to be stopped |
| Rim | South and west low (the camera's foreground); north and east may be the hall's tall wall and the engine's works. Nothing over 3 m and 7 m across between the camera and C; nothing important behind the Warden from the camera (the heart is north, above it, which is where it belongs) | Fixed three-quarter camera |
| Leaving | None. No `BossExit`: at the fourth clamp it stands down where it stands (`Ending.ShutDown`: its body stays, staff lowered, and is not cleared away) | The brief: shown the release, it stands down |
| After the fall | `WardenAsks` by `AfterTalk` on `bossdown.LastWarden` (speaker `Warden`, `heardFlag` `oriel.wardenStoodDown`, delay about 1.2 s, so the four letting go is seen first). The Warden plays no down bark and nobody else's plays (manifest 8.3). On every later load the Warden's body is the room's: a standing `Prefabs/Enemies/LastWarden` at C facing south, staff lowered, under a `FlagGate` on `bossdown.LastWarden` (shown when set), solid, in the bake. **Staff lowered** is a `StandsDown` (`S/Combat/StandsDown.cs`, `id="LastWarden"`, `seconds=0`) added to that body after its behaviours are switched off: its staff's point down before it, its head bowed. The fight's Warden takes the same pose over 1.2 s where it stands, turning back to the way it was set facing (south, as the room's), and while it stands the room's hides itself (renderers and its holder's box), so the flag showing the room's at once never puts two Wardens in the hall | `IshenReads` follows at the record wall; the Warden is there for it and for every visit after. No staff-lowered clip exists and none was bought: the pose is made on the rig |
| The stays' tenders | Each `BridleStay` the `Choir_Tenders` keep carries a **`Tended`** (`S/Combat/Tended.cs`) on its own object, `seat` = its seat's floor point. Set its `wanted` true while a tender should come (the stay gone slack, say). A Clamp Tender walks to the nearest wanted `Tended` within 24 m that no other tender is at and works it beside its seat (`Tending` true while it does, `Worked` the seconds worked, `By` the tender, the static event `Tended.Begun` as it sets to work; `Clear()` starts the count over); what tending does to the stay is the stay's to say. With none wanted it fights with its torch | The same body keeps the hall's clamps (each `BridleClamp` carries a `Tended`, wanted while it is offered) and the gallery's stays, without Combat having to know a stay |
| Fibre Wisps | In the fibre gallery, a `ForceLine` on the floor under each stay strung across it (its ends where the stay's shadow meets the walls). A wisp rides the nearest within 16 m and jumps between them; with none near it makes its own, so it fights anywhere | The Skyfields lay theirs the same way where fibres run along the ground |

What the fight sets and logs, for the builder's own checks: `oriel.clamp1` to `oriel.clamp4` as the clamps open, `Clamp` reported for each, `bossdown.LastWarden` with the fourth, then `BOSS_SHIFT LastWarden 2`, `CLAMP_TAKEN <index>`, `CLAMP_EASED <index>`, `CLAMP_OPEN <index> opened=<n>`, `WARDEN_HOLDS <line>`, `WARDEN_RELEASED`, `WARDEN_STOOD_DOWN`. Its music is "Boss of the Ages" (the zone's Glass Ruans comes back when it stands down).

### Coordinates as built

World engineer, November 12, from `OrielBuilder.GlassChoir.cs` (the numbers are the builder's constants in `OrielBuilder.Choir`; `GlassChoirTests.TheEngineHallIsBuiltToTheBossContract` checks them in the built scene).

| Thing | As built |
|---|---|
| C | **(0, 292)**, floor **-10**: a worn bowl of 3 cm and noise under 1.5 cm, nothing else; no step anywhere inside r 18. The engine's ring at C is drawn only (a bronze disc r 2.75, flush, 12 sockets at r 2.6) |
| The four clamps | `BridleClamp` on `Clamp N seat` (empty object at the seat, facing C): **1 NE (7.78, 299.78)**, **2 NW (-7.78, 299.78)**, **3 SW (-7.78, 284.22)**, **4 SE (7.78, 284.22)** (r 11 on bearings 45, 315, 225, 135). `settledBy` LastWarden, `openedFlag` `oriel.clamp1` to `4`. Bodies 3 m behind at r 14: NE (9.90, 301.90), NW (-9.90, 301.90), SW (-9.90, 282.10), SE (9.90, 282.10), the kit's `BridleClamp` at 4.2 m, its seats turned to C. `whole` = shut; `onCatch` = the clamp with its jaw swung up 58 degrees on its hinge at the pillar's head; `released` = the jaw thrown back 96 degrees. The jaw is the kit clamp cut in two by `tools/blender/split_clamp_jaw.py` (`Art/WorldMeshes/ChoirClamp/ChoirClamp_base.fbx` and `_jaw.fbx`, the same UVs and material); the builder lays both on the whole clamp's transform, checks that together they make it again, and swings the jaw about the hinge it finds from the pieces (`OpenJaw`). Without the pieces it falls back to the whole clamp leaning back. Each state has a static box 2.6 x 4 x 1.8 round the body (in the bake); all four clamps' three states carry the fade, not only the camera-side SW and SE the contract names (a hero on the floor behind a north clamp, r 15 to 18, is behind 4 m of bronze too). Nothing over a seat |
| Fibres | Twelve `ForceLine`s `Fibre <bearing>` on bearings 15, 45, ..., 345: near end at r 2.6, `end` at r 17.5, both 2 cm over the floor, `clamp` by sector (15/45/75 → 1, 105/135/165 → 4, 195/225/255 → 3, 285/315/345 → 2), `width` left at 1.8. Drawn by `BridleHold` as lines a hand over the floor, from the engine out: a slow white pulse running in while the Bridle holds, each three slack and amber once its clamp's flag is set, all slack on `oriel.wardenStoodDown` |
| The gallery's fibres (wisps) | Three `ForceLine`s `Gallery fibre line <z>` (`clamp` 0) under the three stays strung across the fibre gallery: from (-11.8, z) to (3.6, z+.25) for z = 220.5, 229.5, 238.5, on the floor at -6.6 |
| Boss | Spawner at **C**, yaw 180, `faceAsPlaced`, radius 0. `previewPoint` `Last Warden preview (the stair)` at **(0, 270)** on the stair, radius 24 |
| Door | Encounter `LastWarden`, trigger **38 x 8 x 38** on C, `requiredFlag` `oriel.wardenMet`, `checkpointSpawn` `Door`, one membrane: `Engine hall seal` at **(0, 275.2)** across the stair's foot, 9.2 wide and 3.8 high, cube removed, nine taut fibres drawn while shut, a carving obstacle while shut. `WardenFirst` is a crossing at (0, 273), 9 x 2.2, needing `oriel.releaseLearned`, saving at `Door`, its `previewEncounter` the fight |
| Checkpoint | Spawn `Door` **(-3, 257.5)** on the heart landing (-6.6), 17.7 m outside the seal, on the paths. Free cradle `Heart cradle — repair and save` at **(-8.2, 255.8)** on the landing's west side, saves at `Door` (first built at (5, 258.5), which is past the balustrade, down in the trench: the shared reach test caught it) |
| Tenders' floor | Open floor 2.5 m either side of every seat, on the paths (checked) |
| Rim | A balustrade (stone plinth, bronze rail at 1.05 m, solid) round bearings -62 to 128 at r 17.75: beyond it the trench under the body's chest (-19), the engine's works (five bronze arms out of the trench into the glass) and the road's knot (cool, once called the heart; the heart is now the warm one beside the landing) at (1, -8.6, 317). South and west, the hall's masonry under the sightline rule: a parapet at the floor's edge, the wall's full height further out. The stair comes in at the south, 9 m wide. Nothing stands between the camera and C (`GlassChoirTests.NoWallGlassOrTallPieceStandsBetweenTheCameraAndAFight` samples every fight's floor, glass included) |
| Light as clamps open | A shaft of light through the glass onto each clamp, under a `FlagGate` on its `oriel.clampN` (the brief's "a shaft of light through the glass"); every shaft swings once the Bridle has let go (`BridleHold`) |
| Leaving | None, no `BossExit` |
| After the fall | `AfterTalk` `The Warden stands down`: flag `bossdown.LastWarden`, node `WardenAsks`, speaker `Warden`, `heardFlag` `oriel.wardenStoodDown`, delay 1.2. `The Warden, stood down`: the `Prefabs/Enemies/LastWarden` body at C facing south (its behaviours off; it stood in the prefab's own rest pose, there being no staff-lowered clip, until the combat engineer's `StandsDown` was added to it, November 12: staff lowered and head bowed, hidden while the fought Warden stands), a static box 1.4 x 3.2 x 1.4 (in the bake), under a `FlagGate` on `bossdown.LastWarden` shown when set |
| The stays' tenders | A `Tended` on `Stay 3` (the third stay's own object), `seat` its hand at **(-7.4, 245.4)**. Once the stay has eased it is `wanted`; a tender at work draws the stay taut again over 4 s (the way shuts once the rib is half down) and it stays set while that tender lives; stopped, it eases again by itself (`GlassChoirTests.AClampTenderDrawsTheThirdStayTautAgainAndStoppedItEasesAgain`). `Choir_Tenders` (2, from (-10.2, 240.5) and (1.6, 239), needing `oriel.stay3`) |
| Record wall | The slab set into the west rim at bearing 299, r 18.5, at the floor; its point `Look at the record wall` at **(-13.9, 300)**, needing `oriel.wardenStoodDown`, node `IshenReads`, report `RecordWall`. Ishen's place there (-12.2, 299.4) |
