# Room and outpost dressing (D191)

2026-11-03. Status: **Decks, Tallow and Sorrel dressed; editor stills opened; automated sightline/route/reachability checks pass; in-player review and final MAP_EYE_TEST pending (see end).**

## What changed

`Lattice.EditorTools.RoomDressing.Apply` (isolated; never `WorldBuilder`) opens each saved scene, rebuilds only its `Room dressing` root, rebakes that zone's navigation and saves. Stand-ins it replaces (galley crate-tables and benches, cabin benches/crates, Mira's six back-stock crates, Tallow's rest-cabin benches/crates) are switched off and renamed, never deleted, and restored on the next run.

- **Decks (96 pieces):** arrival vestibules with benches, planters, luggage pallets and locker shelving; Orrin's office desk, records shelves and visitors' rug; produce stalls and stock pallets in Mira's market with herb planters behind them in the galley; four galley tables with stools on rugs; Hal's workbench, parts shelving and cart; stores shelving, barrels and tank; receiving pallets and tank; service-gallery shelving against the full-height back wall. Walkway runners follow each hull's dock-to-gallery spine, the promenade and the service gallery. Floor finishes get world-space wear patches.
- **Construction fix:** the 16 m cross walls left 3.7 m side gaps, so the four "private" cabins behind closed hatches were open to the office and service gallery. Four generated low-wall modules close them; each cabin is now a home (bunk, rug, and one of shelf/table/workbench/planter).
- **Tallow (19 pieces):** mess table and waiting rug/planter to port, help-desk shelving and lamp, the Keeper's bench and lamp, starboard shelving and pallet, life-support tank/barrels/shelf behind the closed hatches, bunks in both rest cabins, a runner from the lift to the Keeper.
- **Sorrel (81 pieces):** fuel tank, comms mast, solar arrays, barrels and a parked rover at the habs; receiving/stores/repair-pad equipment beside the chapter tour path; an ore hopper backed onto the excavation face; generated strata ledges/outcrops on far slopes; spoil near ore and the excavation; low rock and spoil verges; bollard lamps on the outpost haul road. Ground: terrain vertex colour (paler packed haul road, clay and dust patches, darker slopes) and slope-masked strata on steep faces (new opt-in `_StrataSlope` in `LatticeToon`; default off, so no other material changes).

Art: 18 generated pieces plus runner/rug textures, 312 Meshy credits, balance 3,769 → 3,481 ([ledger](../art/C8-room-dressing.json)).

## Checks

The builder refuses to save a zone if any hand-placed piece blocks a replay-route corridor (all `docs/quality/routes` navigate segments, ±(0.35 + tolerance + 0.3) m; routes already running through walls are reported obsolete and skipped), crowds a door, speaker or spawn, intrudes on an encounter arena, intersects existing solid geometry, removes a prompt approach that existed before dressing, or hides a hero. Sorrel's slope scatter and verges are generated: pieces that fail clearance or sightline are pruned and logged.

`SightlineAudit` (runtime, shared with tests) sweeps every reachable standing point in each dressed mesh's sightline shadow from the D119 camera (40°/20°/19.5 m, chest at 1 m, Hushwell's 0.6 m near stop) using the exact rendered meshes. Final: Decks 6,538 points, Tallow 529, Sorrel 7,942; **0 hidden**.

Refused layouts during iteration (kept as notes; logs were overwritten per run): retire matched wrapper and body (46 problems), nominal sizes intersected walls (22), route corridors crossed by outpost pieces (13, then 1), prompts beyond the closed Burrower seal flagged until the before/after baseline rule.

`RoomDressingTests` (PlayMode): no hidden hero in all three zones; **negative control** — a 2.4 m stack on open promenade floor is reported; cabins closed, with a **negative control** that disabling the closing walls reopens all four gaps. The first run of the cabin test failed because its control probe ran through a shelf (`Builds/quality/C8/room-dressing/affected-01-control-probe-blocked.xml`); probes were moved to clear lines, not loosened. 68/69 then 5/5 affected/new checks; full suites **176/176 EditMode and 460/460 PlayMode** with graphics, zero skipped.

## Review

Opened before/after gameplay-angle stills (`Builds/quality/look/dress-before-01`, `dress-after-02` Decks/Tallow, `dress-after-05` and `dress-final-01` Sorrel; sheets in `Builds/quality/C8/room-dressing`). Each Decks room now reads as what it is (arrivals, office, market, galley, workshop, stores, homes), runners make the circulation legible, and Tallow reads as an occupied refuge. Sorrel's outpost reads as a working camp. Remaining weaknesses: the open centres of rooms are still bare cross-plate floor (they are the circulation); 1.3 m lamps read small; Sorrel's wide basin floors between paths still look plain, because most generated verge pieces fall inside route corridors and are pruned. These are editor stills with static heroes, not play.
