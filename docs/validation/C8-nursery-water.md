# Nursery shoreline refinement

October 4, 2026. **Shoreline geometry and opened player views pass; one first-visit hitch remains unresolved.** This follows the [generated nursery floor](C8-nursery-art.md), without claiming whole-map or C8 acceptance.

[Raw evidence hashes](C8-nursery-water-evidence.json) cover the rejected and accepted views, tests, player captures and timing runs.

Opened fixed-camera images exposed hard blue pool edges and a cracked surface. Two causes were distinct: the licensed shader's former 0.18 setting produced only a 1.8 mm depth fade, and portions of the water mesh ended above the wet basin floor. The original geometry check fails at pool zero: surface -16.085 m versus bed -16.119 m. A wider fade alone could not fix the exposed mesh boundary.

The isolated `BiomeLandscapeUpgrade.RefreshNurseryWater` refreshes three public mesh assets and their generated fallback material. The meshes extend beneath the dry banks, with no added vertices, colliders, terrain or navigation edits. Every boundary vertex and midpoint is checked against the actual basin surface. The fallback uses the existing toon shader's transparency and baked shoreline blend. Scenes retain no licensed dependencies.

`LocalBiomeBake.Build` gives the optional licensed nursery water a 20 cm fade, 0.22 normal strength and 0.16 animation speed, without foam across its centre. Bank detail, refraction and reflections remain. The source material's obsolete `_AnimationSpeed` and `_FoamOpacity` names are removed from the recipe; current animation uses `_Speed`. Tallow retains its previous material values and contained cistern appearance. All licensed derivatives remain ignored.

Raw evidence is under `Builds/quality/C8/`:

- `nursery-floor-02`: original final-floor views, rejected for the hard/cracked water treatment.
- `nursery-water-01`: wider fade and quieter normals; cracked foam remains.
- `nursery-water-02`: surface foam removed; the exposed mesh border still cuts part of the bank.
- `nursery-water-03` and `nursery-water-final`: corrected submerged boundary; final also has the corrected slow animation parameter.
- `nursery-water-public`: rejected opaque fallback, whose polygonal basin intersection remained conspicuous.
- `nursery-water-public-02`: softened public fallback, opened at the same fixed camera.
- `nursery-water-checks/rim-original-red.*`: new boundary check rejects the original assets. `surfaces-green.*` passes all ten surface checks; `final-editmode-green.*` passes all 85 EditMode checks, zero skipped.

Runtime **b1df12b** builds in both configurations and passes 85/85 EditMode and seven affected Hushwell PlayMode checks. Separate standalone runs at 1920x1080, Ultra, VSync off and 60 fps:

| Run under `Builds/quality/C8/` | Result |
|---|---|
| `nursery-water-performance-release` | REJECTED by offline timing: 378.052 s / 22,675 focused frames; p95 16.972 ms, p99 17.080 ms, one 152.800 ms hitch at 231.288 s, first nursery approach |
| `nursery-water-headroom-development` | Capped timing PASS: 378.027 s / 22,681 focused frames; p95 17.012 ms, p99 17.143 ms, worst 20.653 ms; zero over 25 ms |
| `nursery-water-performance-release-repeat` | Unchanged-build timing PASS: 378.071 s / 22,683 focused frames; p95 16.971 ms, p99 17.074 ms, worst 19.824 ms; zero over 25 ms |
| `nursery-water-player-views` | Captured ordinary traversal PASS: 378.059 s / 22,683 focused frames; opened frames 024/030/036 show the banks, floor and unobscured heroes |

The development folder's name reflects requested counters: its route remained capped, so this is **not an uncapped headroom gate**. `counter-diagnostic.json` retains that policy rejection. Its observed active CPU p95 is 1.562 ms, and GPU p95 is 1.330176 ms through both APIs, with no impossible samples. The initial release hitch had no lost focus or detected competing process. It did not recur in development or the unchanged release repeat. Its cause is unknown; retain the rejection and do not infer that a shader-cache or GC fault has been fixed.

The views also exposed a live-organ defect in the supposedly completed cave. These runs used the recorded `gullet` fixture and ordinary virtual-gamepad walking; they are not earned progression or proof of a quiet completed cave. [The resulting save/revisit repair](C8-organ-persistence.md) now passes full suites, an earned cave recapture and a separate corrected-fixture timing run. The latter records p95 16.968 ms and one 37.782 ms frame, preserving that result alongside the original unresolved hitch. Nathan's original save hashes and count still match the session manifest. The [current chapter package](../WORKSHOP_CHAPTER.md) is runtime a3aaad6; 56a013e is archived.

Continuous video and mixed audio are retained as `nursery-water-player-views/replay.mp4`. Static images were opened; continuous audiovisual quality and physical-controller feel remain UNVERIFIED.
