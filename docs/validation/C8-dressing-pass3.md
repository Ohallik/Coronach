# Dressing pass 3: Hushwell and the Gullet (D193)

2026-11-03. Status: **Hushwell and the Gullet dressed; builder refusals all clear in all five zones; in-player MAP_EYE_TEST pending.**

## Before

Editor stills `Builds/quality/look/hg-before-01` (13 Hushwell and 13 Gullet gameplay-angle views, all opened). Hushwell: one dark uniform brown floor on all three levels; the breach had a cart and one device; galleries almost empty; vent glows threw no light on the floor; five identical cradles in a ring. Cause found: the whole cave was one renderer, so Forward's six-lights-per-object limit let only six of its ~20 lights reach the floor. Gullet: every organ showed the same tiled cell membrane edge to edge; nothing distinguished canal, feeding chamber, eddy, gate or coil from the flight camera.

## What changed

`RoomDressing.Apply` (isolated, D191) now dresses `Hushwell` and `Gullet_Tunnel` too (`RoomDressing.Caves.cs`).

- **Hushwell (48 pieces, 68 floor details, 200 lit chunks):** rails from the breach up the haul tunnel to both ore galleries (a spur to gallery A), the two loose carts coupled on the line, a buffer stop at the scaffold; work lamps (warm pools) at the breach camp and along the haul tunnels; power cell, cable drum, bench, core rack and barrels at the breach; survey beacons and a core rack at the survey-grid wall; a lit cache camp in the east pocket; 23 crystal spars with cyan light on the far walls from gallery A down to the nursery approach; a scald crust 6.4 m across around each of the six vents (the plume's 3.2 m radius); fissures radiating around the Bellows slab; dust, basalt and fissure patches by level; floor vertex colour by level and use; seven nursery cradles of three kinds and sizes; Hushwell's fill raised slightly (`ZoneLook`).
- **Gullet (141 floor-detail batches, no solid pieces):** vessels along both sides of every organ; pools and pores in the feeding chamber; scar tissue at the throat folds; silt in front of the eddy wreck; egg clusters toward the nursery gate; scour arcs in the coil; vessels converging on the exit.

Art: 6 generated models and 12 decal textures, 138 Meshy credits (3,289 -> 3,151); [ledger](../art/C8-dressing3.json).

## Checks

- Builder, final run on all five zones (`-dress-final`): 0 problems everywhere. Decks 108 pieces / 255 details / 7,712 standing points / 9 of 9 prompts, Tallow 22 / 21 / 878 / 3 of 3 (both identical to pass 2), Sorrel 711 / 283 / 41,021 / 5 of 5, Hushwell 48 / 68 / 17,211 / 4 of 4, all 0 hidden; Gullet 141 renderers, all below y=-0.6, no colliders.
- Refusals during iteration, each fixed by moving the piece: carts in the chapter-08 corridor (the route cuts to (-11,40), off the layout line), four cradles across the nursery replay paths, camp pieces in rock or overlapping the scaffold, crates and grooved-wall boxes. The sightline audit pruned crystals that would have hidden floor (13-16 standing points per run).
- Builder defects found and fixed: prompts behind Hushwell's closed seals were never protected and the sweep skipped the nursery (1 -> 4 prompts, 11,044 -> 17,211 points); the reachability radius went negative below y=0; refused patches still reserved floor; square crusts beside walls were refused for their transparent corners; rails broke under the grooved walls' bounding boxes; Gullet bands were refused by Sorrel's step rule on a smooth bed; the coil's inner scour arcs sat on the fold's steep inner face and were moved to the flat bottom and crest.
- `RoomDressingDetailTests` (EditMode): extended to Hushwell and the Gullet, plus `TheGulletBedStaysFarBelowTheFlightPlane`, `TheFlightCheckCatchesAPieceAtShipHeight` (negative control) and `HushwellIsDrawnInLitChunksThatCoverTheWholeCave` (with a dropped-chunk control). **Red on the undressed scenes first** (7 of 12 failed for the right reasons; kept as `Builds/quality/C8/dressing-3/editmode-detail-on-undressed-scenes-RED.xml`), then 12/12 on the dressed scenes. `RoomDressingTests.DressingNeverHidesAHeroFromTheFixedCamera` now includes Hushwell.

## Editor stills (opened)

`Builds/quality/look/hg-after-01` ... `hg-after-06`. Rejected along the way, on sight: hard orange lamp discs (softened), dark basalt blocks reading as stickers (fainter, rarer), fragmented rails, missing crusts, blob-like vein patches with dark halos (now vessels with the tissue faded), a hard striped scour ribbon (now feathered grooves). Current: the breach reads as the miners' working end of a rail line; lamps pool warm light along the haul tunnel; each vent's danger radius is visible on the floor; crystals light the galleries; the nursery colony is varied; the Gullet's organs differ from the flight camera. Still weak: the lower ramp to the nursery stays dark; the coil's scour arcs end square; the Gullet's exit valve carries little.
