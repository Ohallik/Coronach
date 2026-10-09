# Tallow dialogue-reading camera and navigation diagnosis - October 9

**The reading sightline has a verified controlled repair; final presentation remains unvalidated.** Both standard players bind 827 inputs. [Machine-readable evidence](C8-tallow-reading.json) retains original failures, fault controls, source/package hashes, images and preservation records. Raw evidence: `Builds/quality/C8/tallow-reading/`.

## Reproduction and repair

The preceding mooring/player02 capture rejects the detached marker moving behind the real dialogue panel and below the screen. The new fixture uses the actual scene, both generated ships, actual observation/dialogue ownership and actual panel/portrait bounds. It waits twelve seconds before opening the line, then measures every frame for thirty-two seconds. A 1920x1080 render target includes the real UI; this is controlled rendering, not ordinary input.

| Run | Result | Evidence |
|---|---|---|
| original01 | 0/2 | Original 640x480 runner: both heroes crop the marker and overlap the overlay; retained |
| original02 | 0/2 | Explicit 1920x1080 target: 1,913 / 1,914 frames, 7,307 / 7,364 intersections, 176 / 35 crops |
| candidate01 | 0/2 | Same effective failure: the component's Start lookup precedes ArenaRuntime's camera creation |
| candidate02 | 2/2 | Retry camera lookup until available: 1,912 / 1,913 frames, zero intersections and crops; minimum marker span about 0.122 |
| lifecycle01 | 14/15 | Existing five camera-return and two mooring-view checks pass; new pause fixture wrongly requires motion from an already settled envelope |
| lifecycle02 | REJECTED crash | Null-graphics target allocation fails; process -1073741819, no result XML; no test acceptance |
| lifecycle03 | 1/1 | Pause during an actual transition; unchanged freeze/resume limits pass |

The local Tallow component frames the actual assembly and nearby craft through a generic CameraRig scene-interest lease. The fixed profile angles stay unchanged. A reserved upper viewport leaves space for dialogue. Combat retains priority; completion, departure, disable, save-owner change and stale leases return the camera to hero follow. Swap preserves the view and pause holds easing. The per-frame flag lookup avoids constructing a new FlagService wrapper. No marker speed/travel, terrain, knee, ship motor or existing assertion changes.

The scene audit compares every serialized object: only one component and its reference on the existing mooring root are added. Existing geometry, supports, discovery point, marker and navigation stay unchanged. Trailing-space normalization has an archived equivalence record.

## Verification and observations

Full suites: **173/173 EditMode**, **450/450 PlayMode**, zero skips. Full PlayMode uses `-Graphics`; this fixture explicitly rejects a null device before allocation. The retained crash is followed by two fresh-XML guard rejections. Eight separate camera mutations reject disable/expiry, completion, save ownership, departure, paused easing, swap and combat-priority defects. Exact runtime source is restored before the full suites and builds.

All six original02 images and all six candidate02 images were individually opened. Original images visibly lose the marker under the panel and offscreen. Revised start/middle/end views keep the marker, loop, free fitting and both ships readable above it; the view pulls back as the marker falls behind. Start captures show the newly opened empty text field; middle/end hold the real first line. All six candidate01 images remain UNOPENED. The near ship still overlaps part of the reel/attached line; the fitting remains a generated crate blockout. No reeling animation or final-art acceptance. No continuous motion/audio or physical presentation was observed.

## Return-route evidence and tooling

The new `chapter-11-tallow-reading-return.json` inserts an eight-second intermediate waypoint at (-12,1,-8). All 125 original step objects remain identical and ordered, including the earlier ten-second return. The standard validator and five removed/changed-request controls prove the route contract. No loadout or progression flag is seeded; the runner resumes the exact earned Drifter-fan/player02 saves.

`tools/quality_navigation.py` is read-only with respect to the capture and writes only a new explicit output. It preserves runtime validity/failures, discontinuities and focus/blocking observations. It does not change the acceptance analyzer or extrapolate across hero/scene changes, moving targets, pause or lost focus. Nine checks pass, twelve broken variants reject, and an isolated overwrite fault proves the output guard.

Applied to the retained failed mooring/player02: step 74 spans 9.983356 seconds, travels 20.841791 m from 25.995332 m away and ends 5.153541 m short of the target. All 597 samples are focused, unpaused and unblocked; full brake is an authored request. The estimated remaining time at its observed average speed is 2.157226 seconds. The 66.9843 ms maximum interval remains. Focus first drops later at 188.040930 seconds. This supports an input-budget diagnosis without proving the absence or cause of a runtime movement defect.

Current ordinary confirmation is **UNVERIFIED**: the read-only preflight at 10:00:40 UTC found VS Code (PID 20408) in the foreground, so no competing visible replay was started. No foreground was changed. The earlier rejected mooring/player02 remains the latest ordinary evidence.

## Preserved acceptance gaps

Both standard release/development players are rebuilt; exact 61da16f players are archived under this evidence root. Source/package and full-suite input manifests are checked. Nathan saves and every frozen a3aaad6 file/profile remain exact. Known Unity import/private-package changes are archived/restored, never committed. Paid exclusions and e93bf13 remain intact.

Original Decks stability hitches, invalid Tallow development GPU values, Sela's high stepping, continuous uphill/flight/audio/physical quality, battle balance and ordinary Cantor release review remain unresolved. All unmet C1-C10 and changed-map final gates stay OPEN. Continue dependency-safe production; no presentation candidate is published as validated.
