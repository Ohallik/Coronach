# The Meridian Crawl - a town on treads that has never stopped

2026-11-12. Builder: `HomeBuilder.Crawl.cs` (nested class `HomeBuilder.Crawl`) on `ZoneKit` and `GroundKit` (the town engineer, chapter 9). Definitions, people, quests and the chart stop: `HomeBuilder.cs` (the first engineer); what this zone sets on them: the hooks `CrawlDefinition` and `CrawlStop` in this zone's file. Chapter brief: [ch09-the-house-that-can-leave.md](../chapters/ch09-the-house-that-can-leave.md) ("MeridianCrawl (town, safe) and Terminator"). Script wiring: [ch09-manifest.md](../writing/ch09-manifest.md), [ch09-wiring.md](../chapters/ch09-wiring.md). **MAP_EYE_TEST: this page was written before construction (the blockout); the stills and what is still weak are under Evidence.**

## What the place is for

Duskline is tidally locked: one side cooks, the other freezes, and a band of twilight runs between. The Crawlers' town walks that band on enormous treads, west, at the pace the dawn comes on behind it, and has done for two hundred years. It is a chain of tread-halls coupled end to end, each a deck on its own pair of treads: the helm at the head, a market street, the yard where ships land and treads are mended, the sky-garden, and at the tail the gap where tread-hall four was coupled until it threw a link and dropped behind. The party comes to ask how a town is moved; Dorrow asks them to fetch his kitchens first. The run to the stalled hall (the Terminator) starts at the gap and comes home to it.

Users: Crawlers (small, goggled, soot-dark, cheerful about catastrophe) at jobs that only exist on a town that moves: oilers on the treads, a hammer-man walking the couplings, dawn-readers at the tail rail with a brass glass, a mail runner who hands things over without stopping, gardeners lashing trees, children chasing whatever has rolled. The named: Chief Engineer Dorrow (the helm, then the gap), Tam (the market street), and later Lo, Hal's apprentice (tread-hall four).

**The one thing a player must feel within five seconds: the town is moving and always has been.** Everything that says so is in the frame, not in words:

- **The ground slides past.** Outside every deck edge and down every coupling gap the plain of Duskline moves east under the town at its walking pace (1.4 m/s): its own texture and its rocks and scrub, which wrap round out of sight. The deck the party stands on never moves in the frame; the world goes past it.
- **The treads turn.** Every tread-hall rides on two great belts, 8.6 m from the ground to the top run, outboard of the deck's north and south rails, the top run rising just above the deck (1.1 m): its plates run forward, its sprockets and idlers (8.6 m across) turn, dust trails off behind it. Seen at the bottom of the frame along the south rail, at the top past the north rail, and end-on down every coupling gap.
- **Everything that hangs sways together.** Lamps on chains, the market's signs and awnings, laundry, the garden's trees in their gimbal tubs: one swing for the whole hall, in step, each hall a beat off the next (each has its own treads), with a jolt now and then as a link goes over a rock. A still shows them all leaning the same way.
- **The light sits low and never changes.** The dawn is always behind the town, low in the east-south-east: a warm key at 16 degrees, so every shadow is long and points the same way (west-north-west, up and left on screen), with a cold blue fill from the night ahead. The ground glows warmer toward the tail and darkens toward the head.
- **The dawn is a glow everybody keeps half an eye on.** Dawn-readers at the tail with a brass glass and a chalk slate of readings; a dawn mirror at the helm; Crawlers here and there turned to look east.
- **Nothing makes a player queasy.** The camera never moves with the town; the deck is still; only what is beyond it moves, at a walk.

## Rules of construction

- **The town runs west to east, the camera looks north-north-east.** The town travels west (screen left); its tail and the dawn are east (screen right). The fixed camera (pitch 40, yaw 20, distance 19.5, fov 30) sees about 6 m south and 12 m north of the party at deck level, and much further where the ground falls away (the plain is 7.5 m below the deck: past a north rail it is seen out to about 28 m). So the **walked spine runs along z -3**, south of each deck's middle; **buildings stand on the north half** of each deck with their fronts to the south (the backdrop), and **lanes between them open onto the north rail**, where the moving plain, the north tread and the tread-halls alongside are in frame. South of the spine there is nothing taller than 2.5 m: the south rail and the south tread's top run are the bottom of the frame.
- **The deck is a built floor**: riveted iron plate laid in courses by the Crawlers, patched, worn bright where feet go, soot and grit in its corners, chalked with tread numbers; boards over the plate on the market street; slatted walks and soil beds on the sky-garden. The landform is one height function (decks at 0, the helm platform and the sky-garden at 2.4, the plain at -7.5); the plain's own floor is cut out of the drawn landform and drawn instead by the moving plain, so only the decks are the ground kit's.
- **The edges are drawn.** Every deck edge has a rail (posts, two rails, a kick plate) on a `Block`; every coupling has a bridge with rails; nothing stands open over the drop and no `Boundary` stands in the open. Past the rails, the treads and the plain.
- **Everything is lashed, chocked or hung on gimbals**, because everything shakes loose: crates under straps, barrels in racks, carts with chocks, stoves and the garden's tubs on gimbal rings, lamps on chains, tools on lanyards.
- **People** are Crawlers, in Dorrow's body (the only Crawler body there is; Tam's is the same body in another grade) dyed in five coats (rust, olive, slate, soot, faded teal), at three builds (adult, a size down, child), and dressed in code (packs, scarves, goggles pushed up, hoods, tool racks, aprons) so no two neighbours are alike; each at a job only a moving town has, standing at it or walking it (`WalkLoop`). Nobody on a way, a sign, or between the camera and anyone named (checked as built).
- **Places are named constants** in `HomeBuilder.Crawl` (`Arrival`, `DorrowAt`, `TamAt`, `Pad`, `Cradle`, `TreadShop`, `Stall`, `PostRack`, `MiraLot`, `Lip`, `DropCage`, `LoAt`, ...): the ways, the keep-offs, the bake's must-reach list, LookDev's views, this page, the tests and a route writer all read the same numbers. `Ways` are declared, and `NothingStandsOnTheWays` (every town's check) refuses the built town if anything solid within .9 m of a way's line, anyone within 2 m, or a walker's loop within 1.6 m.

## Plan

North up, metres. The deck is y 0 unless said. The plain is y -7.5 everywhere outside the decks.

```text
 z 44  .  .  .  tread-halls alongside (generated CrawlTread, three, x -66 / -22 / 22, z 24..44): lit windows, their treads on the plain  .  .  .
 z 24  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~ the plain, sliding east under everything at 1.4 m/s ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 z 13  ==== north treads (top run y 1.1, z 10..13 on each hall) ====
 z 10  ---rail--- | ----------rail------------ | ---------rail---------- | ------------------rail------------------ |
       HELM HOUSE | houses, backs to the rail;  | OIL SHOP   PAD  TREAD   | SKY-GARDEN (y 2.4) x 28..50, z 2.5..10   | THE GAP
       helm plat- | the rail walk (x -28..-21)  | (cradle)  (2,6) SHOP    | trees in gimbal tubs, frames, beds;      | x 60..92
       form y 2.4 | STALLS z 0..3, awnings,     | (-9,5.2)  ARR. (bench)  | Mira's lot (47,8.4)                      | (tread-hall
       x -88..-74 | hanging signs; THE LASHED   |           (2,1.2)(11.5,5.4)| stair x 36..40 up from z -2.5 to 2.5    |  four from
       Dorrow     | STALL (-37,1.4)             |                         |           THE TAIL x 50..60: dawn glass,|  the run)
       (-79,.6)   |                             |                         |           slate, drop cage (60,5.5)    |  porch
 z -3  ramp >>>>==B===== SPINE (market street) ====B=========== SPINE ======B=================== SPINE ============ LIP (60) =B= 66..72
       (-74..-66)| Tam at the south rail       | post rack (12.5,-7.8)   |                                          |
 z -10 ---rail---| (-41,-6.8); oilers; carts   | oilers, lashing         | gardeners' barrows                       |
 z -13 ==== south treads (seen at the bottom of the frame from the spine's south side) ====
       x -88..-60     x -54..-20                    x -14..18                 x 24..60
       B = coupling bridge, 6 m wide (z -6..0), over a 6 m gap open to the plain: x -60..-54, -20..-14, 18..24; and the tail's, 60..66, which comes with tread-hall four
```

## What happens where

| Place | What | Flags, ids |
|---|---|---|
| The yard: the pad | Arrival (spawn `Arrival`, the chart's) beside the landing pad; `Launch — The Decks` on the pad, locked until tread-hall four is in ("Bring tread-hall four in for Chief Engineer Dorrow first"); a Crawler chocking the pad's clamps | `home.crawlReached` (a `FlagAll` on `visited.MeridianCrawl`: set the moment the party arrives) |
| The yard: the oil shop | The cradle, `Oil-shop cradle — repair and save` (paid; saves at spawn `Yard`) | |
| The yard: the tread shop | `Fabricate — The Tread Shop` (a `Bench` on the Lattice anvil); a spare tread link on a stand, the yard's landmark; oilers on the south tread | |
| The yard: the post rack | Neve's second parcel: `Hand over the parcel` at the rack, offered while `ReturnAddress` is at step 1; the mail runner takes it at a walk | `neve.parcelCrawl`, report `CrawlParcel`, situation `parcel.crawl` |
| Market street | The swaying street: stalls with awnings, signs on brackets and lamps on chains over the street; `Trade — The Lashed Stall` (a `Shop`, the zone's own stall `MeridianCrawl`); Tam at the south rail with a notebook: `TamFirst`, first and repeat, until `home.returned`; the rail walk at the north-east corner, looking out at the tread-halls alongside | |
| The helm | Up the ramp to the helm platform: the great wheel (a Crawler holding it), the speaking tubes, the forward lamp aimed west into the night, the dawn mirror; Dorrow: `DorrowFirst` (the run opens on it), `DorrowRepeat` until he goes to the gap (`home.rideDone`) | `home.crawlAsked` (`DorrowFirst`'s own `<<setFlag>>`) |
| The sky-garden | Up the stair from the spine: beds, trees in gimbal tubs, cold frames, the water tank, laundry; Mira's lot, lent nine years ago and still wrapped, lashed to the north rail: `Read Mira's tag`, offered while `SecondInventory` is at step 0 | `mira.lotCrawl`, report `Consignment`, situation `consign.crawl` |
| The tail | Dawn-readers with the brass dawn glass and the slate; the broken coupling (its knuckle hanging, a snapped link on a chain); the lip rail across the gap, its gate chained until tread-hall four is in; the drop cage on its davit over the gap: `Ride the drop cage down — The Terminator`, locked until Dorrow has asked ("Ask Chief Engineer Dorrow at the helm first") | lock on `home.crawlAsked` |
| The gap / tread-hall four | Before the run: open; the plain going away east, warm under the dawn's glow. From `home.rideDone` (the Terminator's ride): tread-hall four (the generated `CrawlTread`) rolls in and couples, its gangway lowered onto the tail; Dorrow at the lip watching it; `DorrowTreadHome` plays (an `AfterTalk`); the lip's gate opens once it is said. From `home.treadRecovered`: its porch walkable, a Crawler with Hesk's pot. From `home.returned`: Lo, Hal's apprentice, on the porch; `Hand over the bench` while `PortableBenches` is at step 0; once handed over, `Fabricate — Lo's folding bench` | `home.treadRecovered` (`DorrowTreadHome`'s own `<<setFlag>>`), `bench.lo`, report `PortableBench`, situation `bench.lo` |

Dorrow and Tam go to the Halo with the party: neither is on the Crawl once `home.returned` is set (Dorrow's place is the Decks' service hatch, Tam's the promenade: the Decks engineer's).

## For the Terminator

*The town engineer's half of the contract with the Terminator's world engineer (written November 12, before either zone was built). The one who needs something writes it here; the other writes the coordinates back underneath. Changes to a number are made by both, never one.*

**Where the dawn is.** Both zones keep Duskline's compass: the dawn is east (+x), low in the east-south-east; the Crawl travels west (-x) at 1.4 m/s; long shadows point west-north-west. The Crawl's light (`ZoneLook`, sun pitch 16, from about 105 degrees) is this page's; the Terminator may want the same key so the two read as one place.

**Where the party leaves for the run.** At the tail, over the gap: `Ride the drop cage down — The Terminator` at plan (58.4, 5.4) on the tail deck (the cage hangs on its davit just east of the lip at (60.8, 5.4)), to the Terminator's spawn **`Arrival`**. Locked until `home.crawlAsked` (`DorrowFirst`), its words naming Dorrow; open for good after. In the Crawl the party drops off the town's tail onto the plain east of it, with the town rolling away west: the Terminator's arrival is best on open ground with the Crawl's tail going away to the west and the dawn's glow to the east.

**How and where the recovered tread-hall arrives (proposed: say here if the Terminator wants it otherwise).**

- **The way back from the run, whenever it is taken** (a retreat before the couplings, or the way home after the third), arrives at the Crawl's spawn **`Gap`**: plan (55.5, -1), on the tail deck by the lip, facing east over the gap.
- **Tread-hall four rejoins in the Crawl.** From the flag the Terminator sets at the third coupling, **`home.coupling3`**, the Crawl stands tread-hall four (the generated `CrawlTread`) in the gap, coupled to the tail: its west end at x 66, its gangway lowered across x 60 to 66 on the spine (z -6 to 0), its porch x 66 to 72. On arrival from the run with `home.coupling3` set and `home.treadRecovered` not yet, it is seen coming: it rolls in from 30 m east over about seven seconds and couples (`CouplesIn`, a new world component), with Dorrow standing at the lip.
- **`DorrowTreadHome` plays in the Crawl**, as the hall comes in ("There she comes"): an `AfterTalk` on `home.coupling3`, speaker Dorrow, heard `home.treadRecovered` (the node's own last line sets it), placed by the Crawl. So the Terminator stages no `DorrowTreadHome`, and its end is the third coupling and the ride: the objective line "Ride the tread-hall out" and a way out onto the hall (for example `Ride the tread-hall home — The Meridian Crawl`) to the Crawl's `Gap`. If the Terminator would rather play the talk in its own zone (the Crawl's tail in sight at the end of the run), write it here: the Crawl then drops its `AfterTalk` and keeps the rest (the hall coupled from `home.coupling3`, the gate opening on `home.treadRecovered`).
- **What the Crawl reads from the Terminator**: `home.coupling3` (the hall is back) and `home.treadRecovered`. Nothing else.
- The wiring table's row for `DorrowTreadHome` is corrected to the Crawl once this is agreed.

As built in the Terminator (November 14; one engineer holds both places now, so this is agreed):

- **The hall comes back on the ride, not on the third coupling.** The Terminator's end is the ride: the engine car pulls the hall 110 m out ahead of the light, sets the party down and sets **`home.rideDone`**; `Climb aboard — The Meridian Crawl` on Dorrow's crawler then arrives at **`Gap`**. Everything above that said `home.coupling3` reads `home.rideDone` (a party that mated the third coupling and left by the chart hasn't brought anything home): tread-hall four stands in the gap from it, rolls in on that first arrival, Dorrow is at the lip from it (and off the helm), and `DorrowTreadHome` is the `AfterTalk` on it, heard `home.treadRecovered`. The Terminator stages no talk of its own.
- **The lip opens once the hall has coupled and Dorrow has said so** (`CouplesIn.shut`, `opensWith home.treadRecovered`): with a flag alone, a quick talk opened it onto a porch still rolling in.
- **The cage stays up once number four is home**: from `home.treadRecovered` the drop cage's way and its sign are gone (the ground behind the town is in daylight).

## Sightlines and what is in frame

- The arrival opens on the pad, the oil shop and the tread shop, and past the pad the north rail, the north tread's top run going forward, the plain going back and a tread-hall alongside with its lit windows (camera 15 m and more from every spawn).
- Every place has its landmark in the frame where a player stops: the helm's wheel and forward lamp (at Dorrow), the market's signs and awnings over the street (at the stall and at Tam), the spare link on its stand and the pad (in the yard), the gimbal trees and the cold frames (on the sky-garden), the dawn glass and the broken knuckle (at the tail), tread-hall four's houses (at its porch).
- The floor: riveted plate in courses (each plate its own size and tone, rivet rows, patches), worn bright down the spine and to every door, soot and grit in the corners and along the rails' feet, oil under the treads' greasing points and the carts, chalk marks at the couplings; boards on the market street; slats and soil in the garden. Every play-zoom still at or under .22 on `tools/look_density.py`.
- Long rooms get beats: the spine is 145 m from the helm to the lip, and every 35 m a coupling (the bridge, the gap, the plain and the treads end-on) with lamps on posts between.

## Coordinates for routes

Plan x, z (a route walks on the floor at its height). From the builder's constants; the build logs them as `HOME_CRAWL_COORDS`.

| Place | Plan (x, z) | Height | Id / flag |
|---|---|---|---|
| Arrival | (2, 1.2) | 0 | spawn `Arrival` (the chart stop's) |
| Landing pad | (2, 6), radius 3.4 | 0 | `Launch — The Decks` -> DecksLeaving `Arrival`; locked on `home.treadRecovered` |
| Oil-shop cradle | (-9, 5.2); saves at (-9, 1.6) | 0 | `Oil-shop cradle — repair and save`, spawn `Yard` |
| Tread shop bench | (11.5, 5.4) | 0 | `Fabricate — The Tread Shop` |
| Post rack (Neve's parcel) | (12.5, -7.8); stand at (12.5, -5.6) | 0 | `Hand over the parcel`, `neve.parcelCrawl`, report `CrawlParcel` (`ReturnAddress` step 1) |
| The Lashed Stall | (-37, 1.4); stand at (-37, -0.6) | 0 | `Trade — The Lashed Stall` (`ShopInventory/MeridianCrawl`) |
| Tam | (-41, -6.8); stand at (-41, -5) | 0 | `Tam` (`TamFirst`) until `home.returned` |
| Helm ramp | foot (-66, 0) to head (-74, 0) | 0 to 2.4 | |
| Dorrow at the helm | (-79, 0.6); stand at (-77, 0.2) | 2.4 | `Dorrow` (`DorrowFirst`, `DorrowRepeat`) until `home.rideDone`; sets `home.crawlAsked` |
| Sky-garden stair | foot (38, -2.5) to head (38, 2.5) | 0 to 2.4 | |
| Mira's lot | (47, 8.4); stand at (46.5, 6.4) | 2.4 | `Read Mira's tag`, `mira.lotCrawl`, report `Consignment` (`SecondInventory` step 0) |
| The tail | (56, -3) | 0 | |
| Drop cage | (60.8, 5.4); stand at (58.4, 5.4) | 0 | `Ride the drop cage down — The Terminator` -> Terminator `Arrival`; locked on `home.crawlAsked`; gone from `home.treadRecovered` |
| Back from the run | (55.5, -1), facing east | 0 | spawn `Gap` |
| Dorrow at the lip | (58.4, 0.8) | 0 | from `home.rideDone` until `home.returned` (no talk of his own; `DorrowTreadHome` is an `AfterTalk` on `home.rideDone`) |
| Lip gate | x 60, z -6 to 0 | 0 | shut until tread-hall four has coupled and `home.treadRecovered` is set |
| Tread-hall four's porch | x 66 to 72, z -7 to 1 | 0 | from `home.rideDone` |
| Lo and the bench | Lo (70, -1.2); hand at (68.6, -3) | 0 | `Hand over the bench`, `bench.lo`, report `PortableBench` (`PortableBenches` step 0); from `home.returned`; after it `Fabricate — Lo's folding bench` |

The declared ways (`HomeBuilder.Crawl.Ways`): what the bake must join, what a route writer walks, and what the build refuses anything to stand on.

| Way | Plan points |
|---|---|
| The spine, helm to lip | (-64.5, -3) > (-60, -3) > (-54, -3) > (-20, -3) > (-14, -3) > (18, -3) > (24, -3) > (58, -3) |
| Up to the helm | (-64.5, -3) > (-64.5, 0) > (-66, 0) > (-74, 0) > (-77, 0.2) (onto the ramp from in front: along x -66 it ran down the ramp's side rail) |
| To the stall | (-37, -3) > (-37, -0.6) |
| To Tam | (-41, -3) > (-41, -5) |
| To the pad | (2, -3) > (2, 1.2) > (2, 3) |
| To the cradle | (-9, -3) > (-9, 1.6) > (-9, 3.4) |
| To the bench | (11.5, -3) > (11.5, 3.6) |
| To the post rack | (12.5, -3) > (12.5, -5.6) |
| Up to the sky-garden and Mira's lot | (38, -3) > (38, 2.5) > (38, 4.6) > (46.5, 6.4) |
| To the drop cage | (55, -3) > (55, 2) > (58.4, 5.4) |
| Onto tread-hall four (from `home.treadRecovered`) | (58, -3) > (66, -3) > (68.6, -3) |

## For a replay route

1. **In** by Sela's chart (the stop is charted by `home.crawlNamed`) at `Arrival` (2, 1.2); `home.crawlReached` is set on arrival (main step 5).
2. **Dorrow**: the spine west, (2, -3) to (-64.5, -3), (-64.5, 0), up the ramp (-66, 0) to (-74, 0), stand at (-77, 0.2) and press Interact: `DorrowFirst` (8 lines, compulsory) sets `home.crawlAsked`.
3. **Optional points**, only at their quests' steps: Tam (-41, -5), `TamFirst`; Neve's parcel at the post rack (12.5, -5.6) while `ReturnAddress` is at step 1; Mira's tag on the sky-garden (up the stair (38, -2.5) to (38, 2.5), then (46.5, 6.4)) while `SecondInventory` is at step 0.
4. **Down to the run**: the spine east to (55, -3), then (55, 2), stand at (58.4, 5.4) and press Interact (`Ride the drop cage down — The Terminator`).
5. **Back from the run** (`Climb aboard` in the Terminator) at `Gap` (55.5, -1): tread-hall four rolls in for 7 s and `DorrowTreadHome` (4 lines) starts 2.5 s after arriving: advance it. It sets `home.treadRecovered` (main step 6). The lip opens once the hall has coupled and the talk is over.
6. **Home**: the spine west to (2, -3), then (2, 3), press Interact (`Launch — The Decks`): DecksLeaving `Arrival`. Lo's bench (tread-hall four's porch, (68.6, -3) from (58, -3) and (66, -3)) is offered only from `home.returned`, on a later visit.

## Evidence

November 14 (built; no test run yet: Unity's licence lapsed mid-pass).
- Look pass 01 (`Builds/quality/look/ct9-crawl-01`, 28 stills): the overview reads as a town on treads crossing a plain, tread-halls alongside, tracks behind. Faults found: the plain's cracks drawn as a paved square; a halo over every goggled Crawler (the strap ring catching the rim light); big plates of one tone, six stills over .22 on `look_density` (gap .35, helm-north .46, pad .26, tail .38, tread-shop .24, yard-rail .30).
- Look pass 02 (`ct9-crawl-02`): smaller plates in a wider spread of tone; tread plate laid wall to wall in the yard read as one plain grey and made it worse (nine over .22). Since then: tread plate in patches with oil-black grit worked in, the strap ring removed, the plain's cells doubled and its cracks thinned, the young in Tam's body, the strip behind the helm platform railed off, the bed (`tools/duskline_ambience.py`). Built (04:13), not yet lit, rendered or measured.
- Look passes 04 and 06 (`ct9-both-04`, `ct9-both-06`): 27 of 28 stills under .22 in pass 04; the one over (helm-north, .47) was the wheelhouse standing across the helm's west end, its ridge 13 m up, hiding anyone on the platform's north half from the camera. It now stands along the north side, its lit face to the camera, and the forward lamp is on its own standard at the bow: pass 06 has all 28 under .22 (worst .14). The halos are gone. The plates read as a patched iron deck (a little busy); the overviews read as a town on treads crossing a plain with halls alongside.
