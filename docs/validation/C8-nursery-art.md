# Nursery floor art

October 4, 2026. **Art increment, ordinary traversal and clean timing PASS.**
C8 and full map acceptance remain OPEN. Runtime source is `56a013e`.
The [artifact index](C8-nursery-art-evidence.json) records 314 retained files,
including the rejected first model, budget control, captures and frozen package.

The later shoreline/revisit update is packaged separately as a3aaad6; see
[the current chapter report](../WORKSHOP_CHAPTER.md). This report's 56a013e player
and manifest are retained under `Player-56a013e` and `player-manifest-56a013e.json`.
The artifact index now points to that exact archived manifest, with its original hash.

The nursery's precise concentric slab is replaced with a generated mineral sweep.
Three irregular arms and luminous grooves make the growing passage described by
Sela visible in the room. The fixed camera, egg cradles, discovery, lift, gameplay
objects, collision and baked navigation remain in place. The relief is only six
centimetres thick, starting one centimetre above the existing floor, so walking
feet agree with the surface. The isolated `RefreshNurseryFloor` entry point updates
only this decoration. Saving the scene also serializes the already tested
`SelaNursery`/`Sela` defaults on its discovery point.

[P46](../art/P46-nursery-groove.json) records the reference prompts, hashes and
generation ledger. Two Meshy attempts cost 30 credits; 2,778 remain. The first is
rejected in `Builds/quality/C8/nursery-model-01`: it made flower-like paving stones
and lost the continuous luminous channels. A simplified three-arm reference
produced the accepted shape. Its initial upright import is retained separately;
the intake records a -90-degree pitch for the horizontal floor.

The second raw model has 6,359 triangles. The 6,000-triangle check fails on that
original before the reproducible reduction in `tools/blender/p46_nursery_relief.py`
produces 5,949 triangles with UVs intact. The original, red/green reports and logs
remain under `nursery-model-02`. Opened isolated and in-cave renders establish the
shape and placement; they do not replace normal traversal evidence.

Both players build. The release recapture in `Builds/quality/C8/chapter-08-art`
loads the accepted earned preparation save and passes the complete cave, both
organs, Bellows, seven discovery lines and lift return: **446.782 seconds,
26,793/26,793 focused frames**. Opened capture `118.png` shows the new growing
grooves behind the discovery, with fitting dialogue, unobstructed heroes and
consistent floor contact. No outcome assertion or gameplay tuning changed.

The separate clean release fixture in `nursery-performance-release` starts with
the cave cleared, walks its main route and repeats eight nursery circuits. It is
explicitly performance evidence, not earned progression. Runtime and offline
`--performance` checks pass, with no capture, pause, focus loss or competing process:

| Segment | Seconds | p95 / p99 ms | Worst ms | Frames over 25 ms |
| --- | ---: | ---: | ---: | ---: |
| First approach | 250.038 | 16.979 / 17.099 | 21.171 | 0 |
| Warm nursery circuits | 128.017 | 16.971 / 17.082 | 19.999 | 0 |
| Whole run | 378.055 | 16.976 / 17.093 | 21.171 | 0 |

All 22,682 frames retain focus. This is capped frame timing; GPU headroom was not
requested for this run. The station FrameTimingManager measurements remain in the
[C7 candidate report](C7-candidate.md).

After the scene replacement, full EditMode passes **84/84** and the Hushwell,
opening-chapter and development-loadout PlayMode checks pass **15/15**, zero skipped.
Nathan's save hashes are restored exactly. These complement the full **226/226**
PlayMode pass and fresh **38:37 New Game-to-Tallow chain** immediately before the
decorative change, documented in [the integration report](C8-nursery-integration.md).
The packaged chapter copy also passes the full ordinary opening: **102.992 seconds,
6,164 focused frames**. Its 243 files match their source hashes. Nathan's original
save hashes and file count still match after all player runs.
[Chapter launch instructions](../WORKSHOP_CHAPTER.md).

The first chained render/refresh launcher stopped because its owned render process
was still exiting. The one-owner guard prevented another Unity instance from
opening Lattice. After the process exited, the isolated refresh completed.

Still open: the cradles' repetitive construction, sparse Sorrel stretches and the
broader opening/ending story work. This improves the discovery's visual meaning;
it does not establish `MAP_EYE_TEST_OK` for the whole chapter.
