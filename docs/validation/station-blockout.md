# Station blockout spatial review

2026-09-26, reviewer Codex. **Spatial blockout PASSED for Hub_CinderHalo, Hub_Decks, TallowApproach and TallowDrift; final MAP_EYE_TEST OPEN.** This is an implementation checkpoint before final generated-art fitting, not MAP_EYE_TEST_OK, C1_STATION_PERF_OK or workshop acceptance.

Subsequent final-art inspection reopened Tallow's exterior/interior envelope judgment: the generated body's center differs from its mesh bounding center. The exterior has been enlarged/recentered and given an attached pressure-transfer tower; see [Tallow Approach](../maps/TallowApproach.md). The first generated Decks route also exposed a companion clipping through an uncollided door jamb; passing route coordinates alone did not catch it. `door-jamb-red.xml` rejects the old post (character reaches z=-5.901 through it); separate post/lintel colliders and rebaked navigation pass the unchanged test and companion route, 2/2 in `door-jamb-green.xml`. The opening remains walkable. The current clean traversal includes these colliders; recorded visual reruns remain pending.

The functional briefs are [Decks](../maps/Hub_Decks.md), [paired exterior](../maps/Hub_CinderHalo.md) and [Tallow](../maps/TallowDrift.md). Actual unlabelled overhead, structure and arrival PNGs were opened under `Builds/quality/station/blockout-03`. In-player stills were opened for office, galley, arrival, public/service paths, swapped-hero wall contact, Keeper dialogue and repair stores. Continuous ordinary-input route artifacts and hashes are listed in [station-blockout-evidence.json](station-blockout-evidence.json).

| Evidence under `Builds/quality/station/` | Result and concrete finding |
|---|---|
| C0 station comparison (under C0/maps-before) | FAILED: ring gaps, floating pods, repeated bays, obstructing furniture and unexplained open boundaries. |
| blockout-walk-01 | FAILED: furniture/people obstructed central circulation. Work alcoves clear that route. |
| blockout-walk-02 | FAILED: companion stranded by the service wall; swapping moved control about 30 m backward. |
| companion-red.xml / companion-green.xml | 30.730 m old separation rejected; corrected path-following passes the same test, 1/1 zero skips. An invalid constructor and empty stale-transform bake were also rejected and retained. |
| blockout-walk-03 | PASSED: 124.85 s, full party/public/service/housing loop, Mira/shop, swap and dock return. |
| blockout-dock-01 / -02 | First fixture held confirm through loading and braked too briefly. Explicit release and a longer cross-station leg preserve the normal input guard; corrected run passes all three dock pairs in 103.08 s. |
| tallow-blockout-walk-01 / -02 | First service leg cut through the console; explicit corner fixes that route. All 47 final checkpoints pass in 177.72 s, including actual repair/save, launch and redock. |

The current first-station hulls match the interior extents and dock IDs. Public bridges connect arrivals, office, market and repair; the rear gallery serves stores, galley and bounded private cabins without routing freight through a home. Casing now encloses the lower beams and joins the ring. Tallow reads as a small refuge with separate waiting, service and repair access. Major circulation/construction contradictions found in the blockout were corrected. Generated surfaces and furnishings may still expose new silhouette, scale, enclosure or clearance problems; the final pass must reopen those questions and repeat the routes.

The traffic-clearance correction is present in the latest blockout scene renders; the passing dock movie predates that radius change. Its final-art flight rerun remains required. Still review and scripted continuous traversal are distinct from watched continuous motion: normal/slow playback, subjective audio and physical-controller feel remain UNVERIFIED because native observation is unavailable. Capture/readback allocations and checkpoint hitches disqualify these recordings from clean performance acceptance. Do not quote their frame times as C1 results.


## Floor finish and pressure backing review

Codex opened `maps-after-07/TallowDrift-arrival.png`: reducing generated texture contrast exposed actual cross-shaped grille openings onto black space. `maps-after-08` rejected an incorrectly scaled backing plate that protruded through the deck. The corrected identity-wrapper/rotated-child fit places reused generated DeckWall geometry below the walking panels. Opened `maps-after-09/TallowDrift-arrival.png` shows recessed steel beneath the slots and no protruding plate. Walking height/collision and room dimensions are unchanged. These are editor stills, not final player or traversal acceptance. No new paid assets were commissioned.

The same `maps-after-09` Cinder exterior structure, Decks overhead and arrival views were reopened after the lifecycle integration. The paired three hulls and front docks agree, public/rear bridges span the actual gaps, and the arrival grille slots show recessed backing. Arrival vestibules are still visually sparse; their operational purpose and the size of furnishings must be judged again at ordinary gameplay scale. No final MAP_EYE_TEST acceptance follows from these stills.
