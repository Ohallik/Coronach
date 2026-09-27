# Station spatial review

2026-09-26. Reviewer: Codex. Agent-directed ordinary-input release routes, actual unlabelled editor renders and opened player stills. No Nathan or independent reviewer approval is implied. Normal/slow video observation, audio audition and physical Logitech feel remain UNVERIFIED; this review concerns spatial construction and circulation.

## Rejections and corrections

The C0 station failed: separated ring ornaments, floating docks, interchangeable tiled bays, blocked public routes and no credible housing/service circulation. The redesigned [blockout](station-blockout.md) establishes three attached vessel hulls, enclosed public and service bridges, receiving/repair, market stock, office/waiting, four bounded cabins and a shared galley. Companion navigation and door-jamb collision have independent red/green regressions.

Final-art review then rejected grille holes open to space, protruding backing, hanging keel supports, a malformed Tallow roof patch, and a deck section below the lower hull. Recessed generated pressure backing, closed lower hull casings, a fitted inspection cover and the corrected occupied-deck level address those defects. Raw rejected views remain under `Builds/quality/station/maps-after-04` through `maps-after-09`.

The final-02 routes all completed, but the launch prompt beside a plain interior bulkhead remained ambiguous. A first added-frame attempt still showed wall across the hatch and was rejected in `maps-after-10`. Coordinate comparison also exposed a 4.5 m mismatch between Tallow's exterior tower and lower arrival bay. The final rebuild leaves actual hatch openings, aligns the lift footprint, extends the shaft into the occupied hull, and encloses adjoining life-support cells. `station-final02.json` preserves the preceding routes and opened images.

## Tallow Drift and approach

Opened `maps-after-11/TallowDrift-{arrival,overhead}.png` and `TallowApproach-{arrival,structure}.png` without labels/HUD, then release `final-tallow-03` arrival/service and flight views 000, 001, 035, 041, 043, 044 and 046.

The lower cabin is a bounded five-metre compartment with a closed south pressure hatch and a north exit straight into the public refuge. Its footprint maps to the exterior shaft, rather than arriving in an unrelated room. Closed service hatches flank it; machinery has separate operator space. Waiting seats lie west of the clear public floor, repair and stores east, and rear cabin hatches open onto the shared service strip. The delivery/service route reaches stores without crossing a private room. The cutaway convention retains visible boundaries.

Outside, the apron is attached to the tower with supports meeting the generated shell. The tower visibly enters that shell; its lower section reaches the enclosed occupied deck. The separate small dark panel is the fitted roof inspection cover, seated on the hull. Launch leaves both ships clear of the tower; the approach retains braking room and leads to the same closed upper hatch.

`final-tallow-03` completes all 47 checkpoints in 177.594 s, 10,652 focused samples and 208.07 m of traversal, including both heroes, Keeper conversation, repair/save, departure and redocking. CPU-side Keeper response is 2.848 ms and repair response 5.805 ms. These captured timings are not C1 performance evidence. The saved fixture reaches `sliceComplete`, and the restored arrival appears in 046.

The corrected shared-layout fit and final integrated spatial review pass for **TallowApproach and TallowDrift**. The earlier blockout remains the circulation baseline; the final generated geometry was used for the corrected envelope/lift fit review. No new greybox capture is claimed.

## Cinder Halo and Decks

Final corrected-player traversal and dock checks are in progress. `final-walk-03` is rejected: 971 samples lose focus from 108.401498 s to the end, missing the dock return, swap and Sela boundary/reversal checkpoints. Its raw video/CSV and failure report remain; a same-route retry is required. The full unchanged retry `final-walk-04` passes all checkpoints in 124.695 s / 7,483 samples, including Sela. Extended `final-docks-03` walks to and presses against all three closed hatches, then returns to the launch control; all pairs complete in 119.547 s / 7,160 samples. Opened 003/004/005 show an actual closed panel, but also expose the lower-casing seam: its top ends at -0.5 below the floor; the bridge underside ends at -1. Cinder spatial acceptance remains OPEN until those joins overlap the floor and are recaptured. The companion/player occlusion when pressed directly against the tall closed panel is an edge-case presentation limitation; ordinary launch control remains before that panel.

## Regression and packaging

`Builds/quality/station/pressure-integrated-01` contains fresh 50/50 PlayMode and 39/39 EditMode results, zero skips, scene-rebuild/render logs and successful development/release build logs. The rebuild changes only the maintained Hub_Decks, TallowApproach and TallowDrift scenes. Zone/spawn IDs, quests and normal saves are preserved. Both packaged players contain the corrected geometry. C1 must still be repeated after these changes; C2-C6 and the rest of the workshop remain open.

Post-rejection editor views: `maps-after-12/Hub_Decks-arrival.png` and `Hub_CinderHalo-arrival.png` show the raised lower casing meeting the deck; the previous black opening beneath the cutaway wall is closed. The unchanged final-docks-03 boundary input stops at z=-19.43 at every hull, with y=0.08. This is functional boundary evidence, retained separately from the seam correction's final player recapture.

`final-docks-04` is rejected for lost focus: 6,223 samples are unfocused from 0.000271 to 104.105835 s, producing wrong-scene and missed navigation checks. A foreign Unity test job overlapped; the exact cause of focus ownership is not established. The route, collision expectations and timing thresholds are unchanged for retry.

`final-docks-05` also rejects focus/input loss: 1746 unfocused samples, first 16.677897 s, last 46.049169 s. It docks into the office first, then misses the departure and later checkpoints. Further capture/performance retries are deferred during continuing foreign Unity activity; independent combat implementation proceeds.

After the final casing/bridge adjustment, `pressure-integrated-02` passes all three station regressions (zero skips), and both development/release players rebuild with BUILD_OK. The earlier 50/50 PlayMode and 39/39 EditMode suite predates that geometry-only adjustment. Exact current binaries, assemblies, content, source and unchanged-save hashes are in [station-pressure.json](station-pressure.json).

`final-docks-06` starts with no competing Unity process and completes the office and market pairs, but is rejected after losing focus from 101.917050 s through 119.564017 s (1,038 samples). The repair hatch/return checkpoints fail. A foreign Frostbound release build is running by the end; its causal role in focus ownership is not established. Opened 003/004 show the corrected casing meeting the deck and the closed pressure panel; 008 shows the attached exterior apron and launch clearance. These stills verify the visible join, not completion of the full traversal. Further player capture/timing retries remain deferred during external activity. The packaged player is still the `742e79c` station candidate; unbuilt ranged-combat source changes are not part of this recording.
