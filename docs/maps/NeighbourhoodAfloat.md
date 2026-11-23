# Neighbourhood Afloat - the game's last place

Chapter 10's epilogue town and its last scene (brief: docs/chapters/ch10-a-route-with-room-to-turn-back.md, "NeighbourhoodAfloat"; script: docs/writing/LastMooring.yarn and docs/writing/ch10-manifest.md, section 2 "Neighbourhood Afloat, seven weeks on" and the two Homecomings tables; who wires what: docs/chapters/ch10-wiring.md, row K). Builder: `Lattice/Assets/_Project/Scripts/Editor/World/LastMooringBuilder.Neighbourhood.cs` (`LastMooringBuilder.Neighbourhood`), on `ZoneKit`. Zone kind `GroundSafe`, ground camera, music Hub Town Groove (the stand-in for the creator's "Neighbourhood Afloat" cue), sky `neighbourhood-afloat-sky`. Rebuild: `LastMooringBuilder.BuildZones -mooring-zones NeighbourhoodAfloat`, then `ZoneLook.Apply -look-zones NeighbourhoodAfloat`, `Restage.All -restage-scenes NeighbourhoodAfloat`, `RouteFacts.Encounters`.

## What the place is for

Seven weeks after the convoy turned back, the districts the player has known through ten chapters have moored together under a new sky and become one town. Nothing fights here and nothing runs on a clock. The player walks it, sees everyone who mattered, finishes the last two side stories, and the game's last scene plays at Jun's fruit stall; afterwards the town stays walkable.

The rule a player reads: **each district is still the place it came from** (the floor it was built with, the landmark it was known by), **and the gaps between them are open sky, crossed on gangways with handrails**. A player should be able to say from one still which district they stand in.

## Rules of construction

- **Hulls, not a landform.** Each district is a hull's deck at its own make, with open space between: one heightfield (`Height`) at the deck's level on a district and 30 m down in a gap, whose floor in the gaps and whose walls under 7 m are cut away afterwards (`Trim`), so a gap shows the sky below and the hull sides end like hulls. Every edge a body can reach has a rail (`Rail`, drawn 1.12 m, blocked 2.6 m so no stride stands on its top; nothing under 2 m tall within 2.6 m of a rail over the sky, and the build walks every reachable edge itself, `AFLOAT_EDGES_OK`); every gap is crossed by a gangway (planks on two stringers, a handrail each side, a static walk box under it for the bake).
- **The fixed camera looks north-north-east** (yaw 20, pitch 40, 19.5 m). Tall things (the rib arch, lantern towers, the moored hulls of the convoy, cabins) stand north and east of where people stand, or are faded (`SceneryOcclusion`). South and west of every speaker the ground stays low. Ships moored at the quay hang below the quay's level, so the camera looks down on them.
- **Districts and their makes** (`MakeAt`): the moorings (hauler plate, rust), the market (the Halo's station plating, blue-grey, with yellow safety lines), Many Hands' arcade (the liner's cream tile and teal runner), the garden (carrier boards, raised 1.8 m), the square (Lastlight's flagstones), the low street (Driftreed's boards, round a raft basin), the east terrace (Tallow's deck plate, cabins), the lantern gallery (Lanternwick's paving, lamp towers), the sky-garden (the Crawl's riveted pavers, trees in rings).
- **People**: every named person is an `Npc` with their own body, standing where the manifest's table puts them; nobody unnamed stands within 2 m of a way; townsfolk wear the bodies of people who stayed behind (Senn, Quill, Aumi, Tern, Lume, Odrun, the Okhari growers, the Kept herders), tinted.

## Plan

x east, z north, metres; deck level 0 unless said. Districts (rectangles with rounded corners):

| District | Came from | x | z | Level | Landmark |
|---|---|---|---|---|---|
| The moorings (arrival) | the Halo's quay plate | -26..26 | -14..8 | 0 | forty cleats; Idra's cutter and Sela's ship on one; the travel terminal |
| The market | the Decks' market district | -24..8 | 14..42 | 0 | Jun's fruit stall and pear tree, Mira's pitch eleven, yellow lines |
| The arcade | Many Hands' arcade | 8..44 | 14..42 | 0 | shopfronts and the teal runner; Xylos's islands moored off its east rail |
| The garden | Many Hands' and Xylos's garden | -48..-24 | 16..46 | 1.8 | beds under hoops; the garden stair up from the market |
| The square | Lastlight's rib over flagstones | -18..18 | 50..76 | 0 | the rib arch; Ruva's table, the message board, Orrin's board, Meret's room |
| The low street | Driftreed's raft bay | -58..-24 | 50..78 | 0 | the basin with reed rafts, the harbour lamp |
| The east terrace | Tallow's cabins | 24..56 | 48..74 | 0 | cabin rows, window boxes |
| The lantern gallery | Lanternwick | -48..-6 | 84..100 | 0 | lantern towers and the gallery lantern |
| The sky-garden | the Crawl's deck | 8..50 | 82..102 | 0 | trees in gimbal rings, raised beds |

Gangways (each 3 m wide, planks, two handrails): moorings to market (x 0, z 8..14) and to arcade (x 18); market to square (x -6, z 42..50) and arcade to square (x 14); square to low street (z 62, x -24..-18); square to east terrace (z 60, x 18..24); square to gallery (x -10, z 76..84); low street to gallery (x -40, z 78..84); square to sky-garden (x 12, z 76..82); east terrace to sky-garden (x 40, z 74..82). The garden is docked to the market's west side: its stair (x -26..-20, z 27..30) climbs west.

## What happens where

The epilogue's nodes (wiring section 2). Coordinates are the published constants in `LastMooringBuilder.Neighbourhood` (the builder logs `AFLOAT_COORDS`).

| Node | How it starts | Where | Placed as |
|---|---|---|---|
| (arrival) | the cut from the passage, or the chart | `Arrival` (0, -4) on the moorings | `FlagAll{visited.NeighbourhoodAfloat}` sets `end.epilogue` |
| `RuvaPassageRules` | Ruva's Npc (a beat in the neighbourhood; "!" until heard once, `end.rulesHeard`) | Ruva at the end of her table in the square (-9.4, 57) | starts `Homecomings` |
| `IruTernsMessage`, `RuvaSennsMessage`, `PimQuillsMessage` | points `Read the message` at the message board (cord, seed tag, valve drawings) | the board (7, 56.6), read from x 4.8, 7, 9.2 at z 54.9 | flags `end.msgTern`, `end.msgSenn`, `end.msgQuill` |
| `PellAumisMessage` | point `Read the message` at the gallery lantern | (-20, 95) | `end.msgAumi` |
| `MeretAccount` | Meret's Npc, every time | in her plain room off the square, the door open (15.2, 58.4) | (no flag) |
| `DraskWatches` | Drask's Npc | at the cabin window over the square's north side (5, 74.5) | |
| `OrrinDirectorship` | a beat on Orrin until `end.handrail`; after it he is at the board, chalking, with no prompt | the new board (-12, 70.4) | `end.handrail` (step 13) |
| `SelaReturnLeg` | point `Stop and listen` at the travel terminal, Bryn beside it | (9.6, -2.8) | `end.routeFiled` |
| `SelaLastSurveyOffer` | point `Look at the nursery on the chart`, same flag as Many Hands' | (14.6, 0) | `sela.surveyOffered`, starts `LastSurvey` |
| `SelaSurveyHome` | point `Put the road on the chart` (report `SurveyHome`, `LastSurvey` step 1, needs `end.surveyFlown`) | (12.1, -2.9) | `sela.surveyFiled` |
| `IdraTiedUp` (+ `IdraHomeCrew`) | point `Look at the moorings` at the cleat; Idra's extra follows if `CutterDown` was done | (-8, -11.6) | `end.cleatSeen` |
| `JaskNewSky`, `PipRota` | Npcs on the gallery | Jask (-36, 92), Pip (-13, 91.6) | |
| `TamSkyGarden` / `TamHomeFrame` | Tam's Npc on the sky-garden (Tam stayed / back for a week) | (36, 94) | |
| (the handrail) | point `Mend the handrail` at the garden stair, from `end.handrail` | (-20.6, 26.2) | `end.railMended`, report `Handrail` |
| (the line) | point `Repaint the line` at the fruit stall, from `end.railMended` | (-13.4, 23) | `end.linePainted`, report `Handrail`; the new wet line shows and the pears stand one crate past it |
| `JunLastLine` | `AfterTalk` on `end.linePainted`, speaker Taren, delay 2.5 s, within 14 m of the stall | the fruit stall | `game.complete`; barks hushed from `end.linePainted` (`QuietAfter`, for good) |
| the nine visits | a point `Talk to <name>` on each speaker (report `Homecoming`, `Homecomings` step 0, flag `end.visit<Speaker>`); while it is wanted it is the person's only prompt (`Homecoming`), afterwards their Npc says the visit again | see "People" | |
| the 32 extras | said straight after their speaker's own line in the same box (`Npc.SayNext`), there only if their story was finished (`Homecoming.extras`) | beside their speaker | uncounted |

## People

Every named person is an `Npc` under a `Homecoming` (S/World/Homecoming.cs): their own line every time (a SideStories beat in the neighbourhood), their visit point while it is wanted, their extras after it. Someone whose only lines are extras stands there only if one of their stories was finished.

| Who | Where | Lines |
|---|---|---|
| Keeper Abri | the east terrace, before cabin nine's window box (35.8, 67.4) | `AbriHomecoming`; `AbriHomeGlass` |
| Teo | the arcade's east end, by the islands (40.6, 25.2) | `TeoHomecoming`; `TeoHomeTags` |
| Maru | the low street, at the raft basin (-38, 64) | `MaruHomecoming`; `MaruHomeHarbourLamp` |
| Hask | under the rib arch (-1.6, 63.4) | `HaskHomecoming`; `HaskHomeStrays` |
| Pell | the lantern gallery (-26, 91.2) | `PellHomecoming`; `PellHomeMothersLamp`, `PellHomeOriel` |
| Tovra | the forge by the garden stair (-16.8, 33.2) | `TovraHomecoming`; `TovraHomePikes` |
| Oda | her door with the empty frame (48.4, 58) | `OdaHomecoming`; `OdaHomeStamp` |
| Dorrow | the sky-garden (24, 91) | `DorrowHomecoming` |
| Mira | pitch eleven, beside Jun's stall (-18.8, 18.4), behind her counter | `MiraHomecoming`; `MiraHomeCrystals`, `MiraHomeTable`; her counter is the shop (`Trade — Pitch Eleven`) |
| Ruva | at the end of her table in the square (-9.4, 57) | `RuvaPassageRules`; `RuvaHomePost`, `RuvaHomeShoot` |
| Iru | by the rib (3, 61) | `IruHomeLamps` (three lamps on the rib) |
| Pim | by the message board (11.6, 55.2) | `PimHomeKey` |
| Meret | her room (15.2, 58.4) | `MeretAccount` |
| Drask | behind the sill of a window on the square's north side (5, 74.5) | `DraskWatches` |
| Orrin | the new board (-12.4, 70.4) | `OrrinDirectorship` |
| Neve | the east terrace (31, 55.4) | `NeveHomeParcels`, `NeveHomeSocks` |
| Dace | the east terrace, by the four blue lamps on its outer rail (46, 51.6) | `SurvivorHomeLamps` |
| Ishen | his shelves on the east terrace (47.6, 66.6) | `IshenHomeShelves`, `IshenHomeMoons` |
| Corra and Brekk | Corra's stall under Brekk's awning, the low street (-30, 73), (-33.4, 70.4) | `CorraHomeKettles`; `BrekkHomeAwning`, `BrekkHomeKettle` |
| Dov | behind the smokehouse's counter on the low street (-31.9, 56) | `DovHomeSmokehouse`, `DovHomeBell` |
| Binna | her window in the arcade (26.6, 37.6) | `BinnaHomeBaskets` |
| Hal | one of his benches, in the arcade (33.8, 35.4) | `HalHomeBenches` |
| Ilo | the registry door, arcade (39.6, 37.6) | `IloHomeBook` |
| Jask, Pip | the gallery | `JaskNewSky` (+ `JaskHomeThree`, `JaskHomeFare`), `PipRota` (+ `PipHomeFile`) |
| Tam | the sky-garden | `TamSkyGarden` or `TamHomeFrame` |
| Jun (body) | behind her pears (-18.7, 23) | the last scene |
| Bryn (body), the navigator with the pencil (body) | the travel terminal | `SelaReturnLeg` |

## Services (the chart stop)

`NeighbourhoodStop` lists shop, bench and repair, and each stands: `Trade — Pitch Eleven` (Mira's counter, `ShopInventory/NeighbourhoodAfloat`), `Fabricate — Tovra's Forge` (the forge's anvil, a `Bench`), `Mooring cradle — repair and save` (the moorings, `RepairBay`, saves at `Arrival`). The nursery's road: `Launch — The nursery's road, Sorrel's Second Sky` on the moorings' pad (20, -6), shown from `sela.surveyOffered`, to SorrelSecondSky `Road`.

## For the credits (stream L)

The last scene sets `game.complete` with the last line of `JunLastLine` (`<<setFlag game.complete>>`); the talk's close is the hand-over. Nothing barks from `end.linePainted` on (`QuietAfter` in the scene). `TurnBack.cardFlag` is `game.complete` (the credits' rule replaces the card, D288). A save made after `game.complete` loads at `Arrival` with the town as it is (the line painted, the pears past it, Orrin chalking).

*Written back by stream L (November 14; `S/UI/Credits.cs`)*: the roll starts in the frame `JunLastLine` closes with `game.complete` newly up (a flag put up any other way, or a talk that finds it already up, rolls nothing), holds the party where it stands at the stall, hushes barks itself (a load finds `QuietAfter` off), and frames the town from this map's LookDev view `overview` (the fixed angle, 4.4 distances back), then drifts north to (0, 96) at 4 back, where Tallow's station hull and the hauler stand in the top corners with the giant between them (from `overview-north` itself both are just out of frame): keep those hulls and the giant there. At its end the party is let go at the stall, `credits.rolled` is set and the autosave written (spawn as the state has it, `Arrival`). Entering with `game.complete` up and `credits.rolled` not rolls it again. Your `OrrinsOfferThenTheHandrail...` test now waits out its 40 s `Settle` under the roll (it still passes).

## Sightlines and what is in frame

Judged from the `LookDev` stills (`Builds/quality/look/afloat-07`, the game camera's own height and pitch):

- **Overview (over the market, looking north):** the market's striped stalls and painted bays, the arcade's tiled floor and shopfronts east of it, the square with the rib and the longhouse beyond, the gallery's lantern towers at the top; the banded giant shows through every gap between hulls, gangways with handrails cross each gap. This is the frame that says "the districts moored together".
- **Arrival:** the quay's riveted plates, the harbour office and the waiting shelter to the east, the painted landing ring, cargo by the cleats; the market gangway is straight ahead (north). The middle of the quay is the emptiest floor in the zone: busy at its edges, plain in the middle.
- **Stall:** Jun's stall (green-and-white awning), the pear tree behind it, the stair to the garden on the left, the old line and the pears in front. The line's spot (-14.3, 23.4) is in frame with the stall, the tree and Jun.
- **Square / rib / Orrin:** the rib's two legs frame the square; Ruva's table, the message board and Orrin's board on the west edge, the longhouse north. The rib's feet are let 1.6 m into the paving to hide Lastlight's snow; its crown's snow cap and icicles (baked into the model) are warmed to weathered bone by its own material (`afloat-rib-bone`), but their shape still reads as a cap from the overviews.
- **Low street / basin:** reed-tinted rafts and cargo, the smokehouse roof in the foreground of the low-street frame (a plank block from above), the basin's water to the west.
- **Terrace / sky-garden / gallery:** cabins with window boxes, planters and tables; the sky-garden's trees in gimbal tubs, benches and beds; the gallery's lantern towers and the great lantern with the giant below the rail.

## Coordinates for routes

Every constant is in `LastMooringBuilder.Neighbourhood` and logged by the build (`AFLOAT_COORDS`). A person is spoken to from `Stand(at,yaw)`, 2.4 m in front of them (behind a counter or a sill that is the near side of it). The walked ways (`Neighbourhood.Ways`, nothing stands within 2 m of one):

- Arrival (0, -4) north up the quay to the market gangway (0, 8)-(0, 14), (0, 20), (-6, 30), the square gangway (-6, 42)-(-6, 50), (-6, 58), (-2, 66), (-10, 72), the gallery gangway (-10, 76)-(-10, 84), (-10, 86.6), along the gallery (-24, 87), (-38, 87.4).
- The arcade: (0, 4), (18, 6), its gangway (18, 8)-(18, 14), (18, 24), (30, 27), Teo (39.6, 25.6); and north (14, 32), the square gangway (14, 42)-(14, 50), (12, 58), (12, 76), the sky-garden gangway (12, 76)-(12, 82), (16, 88), (30, 91.6), (40, 90).
- The stall and the stair: (-6, 30), (-10.6, 24.6), the line's spot (-14.3, 23.4); the stair (-12, 28.6), (-17.4, 28.5), up to (-24, 28.5), the garden (-30, 28.5), (-30, 40).
- The low street: (-6, 58), (-6, 62), its gangway (-18, 62)-(-24, 62), (-34, 62), (-36, 64), (-36, 74), the gallery gangway (-40, 78)-(-40, 84), (-40, 87.2).
- The east terrace: (12, 62), (14, 66), its gangway (18, 66)-(24, 66), (36, 62), (40, 62), (40, 74), the sky-garden gangway (40, 74)-(40, 82), (40, 90).

## For route writers

**The main path (compulsory), from the passage's cut:** load `NeighbourhoodAfloat` at `Arrival` with `TurnBack` at step 13 (`end.rendezvous` set). The objective reads "See the new neighbourhood, and then Orrin" and its arrow is on Orrin.

1. Walk Arrival (0, -4) > (0, 14) > (0, 20) > (-6, 30) > (-6, 50) > (-6, 58) > (-11.98, 68.04) (Orrin's spot, `Stand(OrrinAt,170)`). Press A: `OrrinDirectorship`, 5 lines, press through. It sets `end.handrail`; step 14 ("Mend the handrail on the garden stair, and repaint the line"). Orrin offers nothing after it.
2. Walk back (-6, 58) > (-6, 42) > (-6, 30) > (-12, 28.6) > (-20.6, 26.1). Press A: `Mend the handrail` (no talk). Sets `end.railMended`.
3. Walk (-16, 24.6) > (-14.3, 23.4). Press A: `Repaint the line` (no talk). Sets `end.linePainted`; the wet line shows, the pears move one crate past it; barks stop for good.
4. Wait about 2.5 s (stay within 14 m of the stall, nothing else held): `JunLastLine` (Taren, Mira, Jun, Taren; 4 lines), press through. Its last line sets `game.complete`: the credits' hand-over.

**Flags that end it:** `end.handrail` (step 13), `game.complete` (step 14 and the chapter; `TurnBack.cardFlag`).

**Optional, in any order:** Ruva (-9.4, 57), spoken to from (-7.32, 55.8): `RuvaPassageRules` starts `Homecomings`. The nine visits (each a point `Talk to <name>` on the person, report `Homecoming`, flag `end.visit<Speaker>`; Abri's is `end.visitAbri`): Abri (35.8, 67.4), Teo (40.6, 25.2), Maru (-39.4, 64), Hask (-1.6, 63.4), Pell (-26, 91.2), Tovra (-16.8, 33.2), Oda (48.4, 58), Dorrow (24, 91), Mira (-18.8, 18.4). Each visit's extras follow it in the same box: press through until the box closes. The board's three `Read the message` at (4.8, 54.9), (7, 54.9), (9.2, 54.9); Aumi's at the gallery lantern (-20, 93.4); `Stop and listen` at the terminal (9.6, -2.8); `Look at the moorings` (-8, -11.4). The survey: `Look at the nursery on the chart` (14.6, -2.8) if not heard at Many Hands; `Launch — The nursery's road, Sorrel's Second Sky` on the pad (20.4, -7) to SorrelSecondSky `Road`; back with `end.surveyFlown`, `Put the road on the chart` (12.1, -2.9).

## Evidence

- Build: `AFLOAT_ROUTES_OK points=56` (every spot and every way's end reached on the bake from `Arrival`), `AFLOAT_EDGES_OK` (the shared edge walk over every reachable rim edge and round every person; it found the two holes the mirror's `NoWalkableEdgeDropsIntoNothing` reported, (8, 42) and (-36, 100), before the rails' blocks were raised), `AFLOAT_OK`.
- The build walks every leg of every way on the bake and refuses one whose path is more than half again its length (plus 6 m): with the north row's last cabin at x 40.4 it said "(40, 62)>(40, 74) walks 80 m for 12" (the cabin shut the sky-garden gangway's foot off from the terrace); the cabin now stands at x 45.2.
- The build refuses a hand-placed townsperson standing inside anything (the sky-garden's child stood in a bed: `NothingFloatsOrSinksIn`, "sunk 0.77 m"), and the crowd's spots are checked from the feet up.
- Stills: `Builds/quality/look/afloat-07` (28 views).
- Tests: `NeighbourhoodAfloatTests` (10) and the zone's shared cases, label `afloat5` in a test mirror (Builds/quality/focused/afloat5-playmode.xml).

## Look pass (November 21, the chapter 10 zone engineer)

After the whole-game capture (`Builds/quality/nov20-whole/s69`):

- **Orrin's board** carries his chalk, painted (`tools/afloat_boards.py`, `Art/Generated/Afloat/afloat-chalkboard.png`: three columns under underlined headings in a hand, a rota's tallies, a line struck through, the convoy drawn as beads); his line after the handrail is a line of the same hand (`afloat-chalkline.png`, cut out). It was rows of plain white bars (s69/014, 031).
- **The message board** carries fourteen notes pinned at a slant, each its own paper and ink (`afloat-noticeboard.png`); the three messages a player reads stay in front of it as they were.
- **The stalls' goods** were drawn in the town's dark iron (`hands-dark`), so every counter showed grey lumps and Mira's black cubes on a pink cloth (s69/052, 064). Each stall now lays out what it sells in its own colours (`Neighbourhood.Wares`): Jun's crates of pears and greens, the greengrocer's, the cloth stall's bolts and folds, the tinker's tins and coils (and Corra's), fish on ice, bowls of spice, loaves in baskets, bottles of oil; Mira's counter has open crates of the Halo's crystals in straw, parcels in brown paper tied with string and a tray of lamp glass.
- **Orrin** wears his own body as before; `tools/orrin_grade.py` gives it his portrait's silver fronds and his reference's charcoal trousers (Meshy had painted them slate and tan, and under this sun he read as a tan man with a dark head).
