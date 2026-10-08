# C2 stationary slope support and gait recovery — October 7

**C2 remains OPEN. Candidate 63 passes full integration and four fresh player motion checks; selected uphill views retain a crouching concern.** Candidate 61 remains REJECTED by its fresh pad motion check and candidate 62 by full integration. This follows the ordinary-player posture rejection in [candidate 59](C2-uphill-balance.md), rather than treating numerical passes as acceptance. Candidate 59 is committed as `c768f33`; its five runtime source hashes match `Builds/quality/2026-10-07-bounded-trace/committed-source-binding59.json`. The exact candidate-59/61/62 packages are archived and verified. Both standard players now contain candidate 63, bound to the source/package inventories in the accompanying JSON.

## Reproduced defect

The fresh shoulder recording shows companion Taren in a rearward, seated-looking pose across several selected frames. Candidate 59 applies horizontal gravity support while walking, then disengages it at Idle. Four new independently baked-mesh checks reject all heroes/forms on a 39-degree uphill stance: the hips/chest midpoint remains **541–601 mm behind both feet**. The same live Animator start/stop fixtures reject the original runtime at **700–820 mm behind the rear forefoot centroid**. Those references differ; neither is a physical centre-of-mass calculation.

The new `TerrainPoseAudit` re-evaluates retained player root/heading/phase/stride rows against the actual Sorrel scene and records posture/support values with a selected side render. Its first attempt fails because the `Idle` state references `RangedIdle`, not a clip named `Idle`; the tool now reads the actual controller state-to-clip mapping. The failed output/log remains. The recorded Sela walking pose has roughly 0.93 m hip clearance at the selected instant, with 0.84–1.14 m across the later ascent. This narrows the sustained companion problem to stationary support while retaining Sela's continuous-motion review gap. The tool samples one clip, **not the Animator's transition mixture**; its scope file and transition/shot metadata explicitly state that limit. The selected comparison shots are outside transitions. The stationary fixture uses the retained shoulder position, not a claimed reconstruction of the companion's trajectory.

Actually opened so far: the candidate-59 shoulder stills at steps 78/166, additional step-166 offsets 0.75/1.75 s and step-78 offset 0.75 s; the re-evaluated Sela walking side; and Taren's original stationary side on the retained shoulder position. The last view clearly shows the hips behind the feet. These are selected images, not watched motion.

## Candidate and checks

Candidate 61 retains an 85% horizontal gravity correction at Idle, blending through the same pause-safe support state. It preserves candidate 59's short-walk correction, the `e93bf13` knee solving/terrain heading/contact repair, all motor speeds and donor clips. Steep Idle/Walk pose transfers blend over 0.20 s into walking and 0.24 s into Idle; ordinary flat blends and gameplay response are unchanged.

The isolated stationary posture metric falls to **0–26 mm behind the rear visible edge**. The first candidate's new live transitions still exceed the existing 6 m/s walking knee bound; retain that rejection. After the slope-specific blend, both heroes/forms pass uphill and downhill starts/stops, all transition frames included, with 1,200 degrees/s and 6 m/s bounds, rendered sole penetration and held-pose/pause checks intact.

The first two live-fixture attempts are also retained: an uncapped run and an explicitly clocked run both used an incorrect incremental ramp-height equation, subtracting the old Y coordinate again. The corrected fixture adds the height adjustment, maintains the intended 22 cm clearance and keeps every original assertion. These failed fixture outputs are not runtime acceptance or discarded rows. The corrected candidate-60 rejection and original-runtime red controls are separate evidence.

Four new active-Idle pause cases reject a deliberate Idle-only removal of the balance pause guard. Both downhill live cases reject a deliberate near-instant slope blend. Both mutated runtime files are restored byte-for-byte before integrated tests. Raw source snapshots, XML, logs and pose diagnostics remain under `Builds/quality/2026-10-07-bounded-trace/` and `Builds/quality/C2/oct7-*`.

The integrated candidate passes **155 EditMode / 235 PlayMode**, with zero skips. Live transition maxima are 5.285 m/s knee speed, 1,095.682 degrees/s and 0.332 mm sole penetration. The candidate-59 384-case fixed-gait contact matrix is retained as prior evidence; it is not relabeled as a fresh candidate-61 run. Nathan's two saves and all 243 frozen chapter files remain unchanged.

Actually opened controlled side images: `oct7-idle-taren61/side.png` and `oct7-idle-sela61/side.png`, alongside `oct7-idle-taren59/side.png`. Taren's hips move forward from the obvious seated pose and his knee bends beneath the torso. This is a selected-pose improvement, not acceptance of his full stance or either hero's motion.

## Fresh player rejection and gait recovery

Both candidate-61 packages build and retain exact source/package manifests. The fresh 60 fps shoulder diagnostic passes all checkpoints and motion bounds in 232.480 seconds / 13,949 focused samples, with 11.757 mm worst planted-marker drift. Opened gameplay frames include step 78 at +1.25 s and step 166 at +0.75/+1.25/+1.75 s. Companion Taren's stance changes but remains crouched. Sela's sampled stride phases vary; these images cannot accept her full movement.

The 60 fps pad route completes its checkpoints with full focus, but **fails motion** at step 118, `Sela ordinary travel 45 reverse`: one walking contact moves **451.197 mm** against the unchanged 50 mm limit. Keep the entire failed recording, all 13,950 rows, source/package hashes and analysis. The result is not erased by the prior full-suite pass. Both recordings explicitly use VSync off because Windows reports displays off; neither accepts C1 timing or synchronized presentation.

The retained root trajectory shows a collision slowdown/rebound, followed by walking at restored running speed while the 0.12-second gait-change hold expires. Opened failure frames at recorded 153.840 / 154.007 / 154.107 seconds place the slowdown beside the companion near the pad; this does not establish the raised edge as its cause. Two new generated-sole PlayMode checks reject candidate 61 and the previous committed animator, isolating a **latent** delay problem rather than proving the idle repair introduced it. Restored-run transfer takes about 167 ms, dragging the walking sole 249–281 mm. Candidate 62 retains the slowdown hold used by moving fire but starts a faster gait immediately. Both regressions and the existing moving-emitter stride test pass (3/3), followed by all 11 focused locomotion PlayMode checks. Transfer starts in about 33 ms; the new checks no longer strand the walking contact at restored running speed.

Both candidate-62 builds succeed. The unchanged pad route passes all checkpoints and motion checks in 232.422 seconds / 13,945 focused samples; worst planted-marker drift is **19.504 mm** and the former failure step uses Run. Its single **34.353 ms** frame remains in the diagnostic timing report. The exact full route trajectory can vary with collision timing; the deterministic red/green foot and transfer checks complement this ordinary-input comparison. All 155 EditMode tests pass. Full PlayMode **REJECTS candidate 62: 236/237**, with Taren's Shaped downhill start/stop reaching **1,209.161 degrees/s** against the unchanged 1,200 bound. Earlier focused passes do not erase that result. The remaining player captures are deferred while angular-peak instrumentation diagnoses this transition. Nathan's saves and all frozen chapter files still match.

## Evaluated-frame clocks and braking-phase coverage

The new angular log narrows the integrated failure to the first frames of braking. The initial 12-phase sweep also reports 1,232 degrees/s during an unusually short frame. A late observer then proves the coroutine reads the previous frame's corrected pose while its denominator comes from the next frame. Record the pose's own delta and frame number instead, and assert the association on every sample. No speed, penetration, coverage or pause bound is relaxed. Unity's test runner rejects `WaitForEndOfFrame` in batch mode, so the observer executes after the terrain correction without relying on a render callback.

The first clock calibration uses tiny rotations that `Quaternion.Angle` rounds to zero; that failure remains. A directly measured 30 m/s displacement then has 0.002918 m/s worst error with the evaluated-frame clock, versus 6,227.939 m/s error with the next frame's denominator. This is an intentionally uneven clock calibration, not a player-performance measurement.

Clock repair does **not** erase the runtime defect: a denser 24-phase sweep reproduces **1,209.354 degrees/s** for Taren at phase 0.630273. Candidate 63 extends only steep Walk-to-Idle blending from 0.24 to 0.30 s, retaining the 0.20 s start blend and unchanged motor response. The focused clock/phase/downhill group passes **5/5**; the reproduced phase falls to **1,157.695 degrees/s**, with 4.740 m/s maximum knee speed across all 48 phase cases. All **3/3 deliberate controls reject** the wrong-frame denominator and near-instant steep stop blend; both files restore byte-for-byte. Full integration passes **155 EditMode / 240 PlayMode, zero skips**. Both players build. Nathan's saves and all 243 frozen chapter files still match after integration, builds and every replay.

## Candidate 63 player evidence and remaining observations

All four unchanged gameplay routes pass their checkpoints and motion checks with every sample focused. Windows reports session and console displays off before the runs; these are explicit VSync-off diagnostics, not synchronized C1 evidence. The 60 fps runs retain video/audio without live PNG overhead; all frames and original thresholds remain.

| Diagnostic | Seconds / samples | Worst contact drift | Minimum central sole | p95 / worst frame |
| --- | --- | --- | --- | --- |
| Shoulder 60 fps | 232.420 / 13,945 | 37.670 mm | 2.996 mm | 17.158 / 20.531 ms |
| Pad 60 fps | 232.363 / 13,942 | 19.609 mm | 5.999 mm | 17.156 / 19.923 ms |
| Shoulder 30 fps | 233.676 / 7,011 | 5.068 mm | 2.998 mm | 33.709 / 36.955 ms |
| Shoulder 120 fps | 231.588 / 27,786 | 34.526 mm | 2.998 mm | 8.706 / 11.669 ms |

Actually opened twelve fresh frames: shoulder steps 78 and 166 at +0.75/+1.25/+1.75 s, plus step 165 +4.50 s, step 166 +4.50 s and step 168 +2.00 s; pad step 118 at +0.90/+1.07/+1.17 s. Taren's sampled walking poses are upright. Sela varies from upright to deeply bent, especially late in step 166; **the uphill crouching concern remains**. Companion Taren is still moving in the selected new shoulder frames, so they do not establish repaired stationary quality. The controlled stationary comparison above remains narrower evidence. The pad samples show a running stride after recovery; changed collision timing means they do not duplicate the original failed trajectory exactly.

Raw capture timestamps map each image to the source recording. `Builds/quality/2026-10-07-bounded-trace/Review candidate 63.html` links normal/quarter-speed recordings and the retained failed candidate-61 comparison. The [machine-readable inventory](C2-idle-support.json) binds source, packages, tests, recordings and opened images. The 149 offline checks and candidate-59 contact matrix remain prior unchanged evidence. No failed result is replaced by this checkpoint.

Continuous normal/slow video, audio, physical scan-out and controller feel remain **UNVERIFIED**. C1's awake Decks failures and invalid Tallow development GPU evidence remain open; the frozen chapter is not replaced.
