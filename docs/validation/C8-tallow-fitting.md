# Tallow loose-line coupler and reel supports (C8)

2026-10-09 to 2026-11-03. Status: **integrated; controlled views opened; ordinary-player review UNVERIFIED; final MAP_EYE_TEST for Tallow OPEN.**

## Change

- The generated crate standing in for the free docking-line fitting is replaced by the generated `TallowMooringCoupler` (one Meshy T2 task, 15 credits, balance 2,383; [art record](../art/C8-tallow-fitting.json)). The ferrule rim is measured from the imported mesh and seated on the existing cable endpoint; uniform intake scale is kept.
- `TallowMooringStaging.RefreshHardware` is the isolated scene refresh. It moves the reel and its platform to (-24, y, 8), translates both existing cantilevers by their evaluated geometry so they meet the platform, and reroutes the slack line so it leaves the reel and loops away from the reading pocket. It throws if a cantilever misses the platform or the cable start leaves the reel.
- Marker drift, discovery flag, Keeper acknowledgement and the reading-camera lease are unchanged.

## Checks

| Check | Before | After |
|---|---|---|
| `TallowMooringTests.BothCantileversActuallyReachTheReelPlatform` | **Failed** on the original scene (`support-original01.xml`) | Passed |
| `TallowReelVisibilityTests` reel separation (Taren, Sela) | **Failed** 2/2 on candidate01 | Passed, 0/181 overlapping samples each |
| `TallowReelVisibilityTests` attached line vs ships (Taren, Sela) | **Failed** 2/2 on candidate02 | Passed, 0/180-181 crossing samples each |
| Remaining Tallow mooring + view checks | — | Passed |

Final filtered run: 11/11 PlayMode, zero skipped, `-Graphics` (2026-11-03). Raw logs, XML and images are under `Builds/quality/C8/tallow-fitting/` (`candidate03-images/`). Nathan's saves were restored from an interrupted October 9 stash first and re-hashed identical after the run.

## Opened images and limits

`Taren-attached-line`, `Sela-overhead` and the coupler close-up were opened. The supports now visibly bridge the platform to the hull, the cable leaves the reel drum, and the coupler reads as an intact shackle with an empty ferrule. The line is still a flat-coloured `LineRenderer`; there is no reeling animation; the reel/platform remain kit pieces. These are controlled background-graphics renders, not ordinary traversal or continuous review.
