# Dressing pass 2: walkway centres, lamps, Sorrel basins (D192)

2026-11-03. Status: **Decks, Tallow and Sorrel dressed; editor stills opened; automated checks pass; in-player MAP_EYE_TEST below.**

## What changed

`RoomDressing.Apply` (isolated, D191) gained a detail layer (`RoomDressing.Detail.cs`) and pass-2 placements (`RoomDressing.Places.cs`).

- **Decks (108 pieces, 255 floor details):** amber guide studs on both edges of every runner (each hull's spine, the promenade, the service gallery); pictograms inside each room's threshold (dock ×3, office, cabins, market, galley, stores); hazard bands at the three dock doors and the eight hull-joint collars, and outlines around the receiving bay, fuel store, the anvil's work zone and the stores bay; grates, drains, hatches, tread plates (repair hull) and gallery trench covers on the plate grid. All 22 stick lamps became bollards with a real warm point light (range 4.6 m, no shadows). A kiosk at each promenade junction, pipe runs on the market side walls, the workshop and stores hull wall and the gallery back wall, cable drums at receiving and in the workshop.
- **Tallow (22 pieces, 21 floor details):** studs on the runner, the lift lobby painted as the launch square (outline, threshold band, launch pictogram), mess/repair/cabins pictograms, grates, drains and a trench cover, lit bollards, plumbing in both life-support rooms, a cable drum by repair.
- **Sorrel (713 pieces, 281 floor details):** tyre tracks on Chaikin-smoothed haul lanes (both lanes on the outpost stretch, one to the excavation) and the rover's spur; soft ground patches (gravel, pebbles, clay in the clay-tinted zones, wind ripples in the dust zones, mineral stains by the ore); 593 plants in 146 clusters within 30 m of the roads; 35 boulder groups and 19 survey beacons around the ore bodies and along the seam road (counts before the builder's clearance/sightline pruning); core racks at the outpost bench and the hopper; unlit bollards on the outpost haul road.

Art: 9 generated models and 20 decal textures, 192 Meshy credits (3,481 → 3,289); [ledger](../art/C8-dressing2.json).

## Checks

- Builder (refuses to save on any problem): final run 0 problems in all three zones. Sightline sweeps Decks 7,712 / Tallow 878 / Sorrel 40,948 standing points, **0 hidden**; prompt approaches preserved (9/9, 3/3, 5/5).
- Refusals during iteration (kept in the run log until overwritten): a bollard crowding a cabin doorway; Tallow's kiosk on three replay corridors in turn (dropped) and a cable drum on one (moved); tyre tracks and a plant hiding the hero because they had draped over a rock and a roof at 2–4 m. That last one exposed the fault: new Sorrel pieces and decals now stand on the terrain only and skip ground under structures.
- `RoomDressingDetailTests` (EditMode, new): every floor-detail vertex lies within 0.15 m above the real ground (deck floor, or Sorrel's terrain itself) in all three zones; **negative control** in two zones: a decal floating 2.5 m up is reported. 5/5.
- Existing `RoomDressingTests` (PlayMode): 0 hidden heroes in all three zones, stack and cabin controls pass.
- Full suites: **181/181 EditMode**; PlayMode below.

## Editor stills (opened)

`Builds/quality/look/dress2-01` … `dress2-05`; before/after sheets in `Builds/quality/C8/dressing-2`. Observations: the walkway centres now read as designed deck (lit studs lead along each route, grates and hatches sit on plates, dock thresholds and hull joints are marked); bollards read as lamps with warm pools on the floor; pictograms make each room's purpose legible from the doorway. The first Sorrel build was rejected on sight: hard-edged ground patches read as stickers, tracks were saturated rust and kinked at road vertices, plants were 0.5 m specks and too sparse to see. All four were fixed (soft blended patches, muted tracks on smoothed lanes, plants 0.65–0.95 m in denser clusters). Plants stay out of encounter arenas, so view centres at encounter spots remain open by design. Gallery bollards wash the back wall in banded pools (stylised; acceptable).
