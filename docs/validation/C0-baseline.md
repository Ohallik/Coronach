# C0 baseline — recorded, 2026-09-26

Source at entry: `6084c88`; clean working tree. Unity 6000.4.7f1.
No Lattice editor/player owned the project at entry. Frostbound's unrelated batch editor was left untouched. Normal saves were hashed before any player launch; all replay saves are below `Builds/quality/`. The prior development package is preserved at `Builds/quality/C0/original-player-dev/`.

Implementation **PASSED** for the baseline recorder; automated recording routes **PASSED** as detailed below; observed still-image defects **FAILED**; continuous motion/audio audition **UNVERIFIED**; physical controller feel **UNVERIFIED**. **C0_BASELINE_RECORDED** records defects, not their acceptance. C1–C10 remain open.

## Defects to reproduce before correction

| ID | Baseline evidence and unknowns | Priority |
|---|---|---|
| Q01 | Existing benchmark holds nearly still in Gullet. Ground camera only moves when target distance exceeds 0.4 m, then interpolates toward the target; moving traces must distinguish this threshold from frame stalls. HUD formats text each frame. Neither is yet an established cause. | First: record moving station intervals and camera displacement. |
| Q02 | Motor rotates the actor to travel, but imported pelvis/chest may disagree. Locomotion donor imports bake orientation from body direction. No unexplained global yaw correction is authorized by this evidence. | Measure bilateral hip/shoulder directions independently of actor forward, compare clips/forms. |
| Q03 | `GroundMotor.Velocity` is assigned before collision. Animation uses that requested speed; blocked motion can still animate a run. Transition/contact quality remains to be observed. | Measure actual displacement and blocked travel before changing stride. |
| Q04 | `GeneratedAnimator` has no Down/Dead handling; enemy cleanup is 0.25 seconds. Hero down state can retain the last motor speed. | Capture ordinary defeat/revive plus targeted lifecycle regression later. |
| Q05 | Eight staged generic science-fiction cues; music originals and assignments preserved. No footfall palette or distinct death layers. | Preserve actual listener mix, inventory missing families. |
| Q06 | Previous routes use dialogue auto-advance. Continuous fixtures keep it off, use distinct press/release edges and check shop/dialogue/world outcomes. | Inspect actual lines/options and input ownership. |
| Q07 | Decks builder fills a 64 × 28 m rectangle with three repetitive bays. Exterior's three isolated dock pods have no visible connectors to the ring or common deck envelope. No residential branch or service spine. Tallow has three invisible edge barriers with no visible hull boundary. | Capture before views, rebuild first station as one place; review Tallow separately. |

New evidence path: `scripts/quality-replay.ps1` uses a timed virtual gamepad through `GameInput`/`PlayerBrain`; navigation goals only calculate stick input. No direct movement, attack, teleport, invulnerability or dialogue completion APIs. Frame CSV includes raw elapsed intervals, actor/camera position, requested speed, skeletal direction, animation state, GC and supported profiler counters. Video and actual listener audio are opt-in and kept separate from clean timing.

Skeletal directions currently use the left-to-right upper-leg/upper-arm span crossed with world up. These are independent measurements, **not yet a visually calibrated acceptance metric**. Twisting poses and small bone separation require inspection before applying C2 tolerances.

Computer-use inspection failed after two attempts and a fresh-session recovery: `Computer Use native pipe is unavailable ... os error 2`. The available image reader can inspect captured stills, but no continuous-video or subjective audio audition has been established. Preserve video/mix artifacts and label those observations UNVERIFIED; neither static frames nor audio signal statistics substitute for them.

Meshy ledger recomputed: 795 consumed / 1,200 cap, 405 remaining. No generation was submitted. Existing UAL1/UAL2 skeleton-only animation donors and owned Kenney/Sonniss sounds are available. The required generated-character skill and import/tooling references were read before the read-only rig audit.

## Recorded evidence and findings

Raw artifacts live under `Builds/quality/C0/`; [C0-recordings.json](C0-recordings.json) records hashes, hardware/settings, exact routes and per-form bone diagnostics. Input authorship: Codex-directed virtual gamepad through the ordinary game input stack. Recordings include music and the actual AudioListener mix; no subjective sound pass is claimed.

| Run | Result and scope |
|---|---|
| `decks-capture-02` | 120.22 s, 7,090 samples. Office–market–repair, both heroes, doorway, wall contact, return, real Mira lines/options and shop open/close. Runtime and offline route checks pass. |
| `c0-tallow-capture-01` | 120.19 s, 7,095 samples. Both heroes, walk/sprint/turns, boundary contact, actual Keeper dialogue and return. Route checks pass. |
| `c0-cinder-dock-capture-01` | 47.81 s, 2,821 samples. Civil approach, ordinary dock, interior launch/return, boost/brake, swap and roll. Route checks pass. |
| `natural-diagnostic-02` | 62.42 s, 3,397 samples. Both Natural heroes, eight directions and sprint directions with independent facing arrows. Route checks pass. |
| `c0-motion-arena_ground-capture-01` | 93.80 s, 5,203 samples. Both Shaped heroes, eight directions, sprint, lock, attacks, dodge, guard and swap. Route checks pass. |
| `c0-motion-sorrel_ridges-capture-01` | 108.70 s, 6,076 samples. Shaped matrix repeated in the real moon map. Route checks pass. |
| `down-revive-01` | 14.74 s, 851 samples. Ordinary enemy damage downs Taren; active Sela approaches and guards through the normal proximity revive, then swaps and attacks. Runtime state conditions pass. This short lifecycle diagnostic travels only 6 m, so the station traversal analyzer correctly rejects its minimum-distance criterion; it is not a performance route. |
| `facing-audit` | Actual imported generated prefabs sampled across 60 phases of each locomotion clip in both forms; root/import settings retained. Read-only editor renders, not watched motion. |
| `maps-before` | Overhead, structural and arrival views of both Cinder scenes and both Tallow scenes. First-station views inspected together; acceptance rejected. |

The captured walking traces show about one third of moving samples with a stopped camera (894/2,697 in Decks; 683/2,061 in the Natural diagnostic). Checkpoint PNGs and 30 fps readback introduce large allocations/hitches, so these recordings **cannot establish clean frame pacing**. C1 must separately measure release/development first-visit and warm timing.

The Shaped running pelvis has a persistent approximately 27–29° rightward offset relative to actor heading; the imported Walk averages about 7°, Sprint about 20°. Both heroes show the same clip-dependent pattern. Root import currently has `keepOriginalOrientation=false`, `rotationOffset=0`, root rotation baked. These measurements require visible-rig calibration and correction at the actual source in C2. No blanket actor offset has been applied.

Opened actual Mira dialogue/shop frames show readable text and successful input handoff. The repair-arrival frame hides the hero behind a foreground wall. The down/revive capture and code inspection expose the missing down animation selection. Sound families and mix quality remain unverified pending audition.

The first station fails spatial review: disconnected arc pieces do not make a continuous ring; floating docks lack a believable shared pressure envelope; its interior is three repetitive bays without housing or separate delivery/service circulation. Functional redesign briefs are in `docs/maps/Hub_Decks.md` and `Hub_CinderHalo.md`. They are design inputs, **not MAP_EYE_TEST passes**.

Rejected recordings are preserved: `decks-capture-01` was blocked by benches and missed dialogue checkpoints; its initial video orientation was also wrong and was corrected in the recorder. `c0-motion-hub_decks-capture-01` accidentally launched when attack/confirm was pressed near a dock, and correctly failed its scene expectations. The new Natural fixture excludes combat actions unavailable in that safe zone. Ten analyzer control tests pass, including rejection of injected stalls, stationary input, focus loss, pause, wrong scene/resolution, missing samples, missed checkpoints and capture-contaminated timing.

Priority now: clean timing and independent camera/blocked-motion red regressions (C1/C2), shared first-station blockout, clip/import orientation audit, then combat/death/audio/dialogue corrections. The larger per-hero/form 60-second calibrated C2 matrix and normal/slow-motion audition remain required; these C0 clips do not close that gate.
