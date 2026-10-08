# C2 — evaluated uphill poses, October 7

**C2 remains OPEN. The changed diagnostic confirms a stationary companion crouch; it does not repair or accept movement.** Both standard players now support `-GroundPose`, which also enables the existing motion records. All `GroundFeet` and `GeneratedAnimator` bytes, motor settings, donor clips, knee/contact assertions and earlier failures remain unchanged. [Results and exact inventories](C2-evaluated-pose.json).

## Actual pose recording

The new opt-in recorder streams world-space roots, headings, velocities, hips, chest and both leg chains for the active and partner roles after `GroundFeet.LateUpdate`. It retains sample/frame/step/time, evaluated delta, actor state, base animation phase, transition state and support diagnostics. It records the real Animator mixture rather than reconstructing it from one clip. Missing actors/joints and nonfinite data remain in the raw file and reject completion.

The independent reader requires both roles for every replay sample, matching identities, finite geometry/clocks, complete producer/launch evidence and full focus. Known straight/right-angle geometry calibrates its knee measurement. It reports ranges including transitions, without assigning a visual acceptance threshold. Launch flags and metadata exclude the extra work from clean timing/headroom. `TerrainPoseAudit` separately gains donor hip-height and knee-angle fields while retaining its single-clip scope warning.

**202 offline checks and five affected PlayMode checks pass, zero skipped; both builds succeed.** Deliberate wrong-knee and false-completion producers each fail one of the two new PlayMode checks, then restore byte-for-byte. Deliberately accepting coverage produces 17 failures; a wrong angle calculation fails; two native-launch exclusions reproduce false passes in the original checker. The first relative-path audit failure and a floating-point calibration-fixture failure remain. The latter uses exactly representable orthogonal geometry after repair; its assertion was not loosened. No fresh full C7 suite is claimed.

## Fresh synchronized shoulder recording

The unchanged full shoulder route completes **232.943 s / 13,978 focused samples**, with **27,956** complete active/partner pose rows and process exit 0. Every original route step, facing/contact assertion and failed earlier recording remains. Both display endpoints report on and VSync 1 is retained. The new capture's worst frame is 20.2563 ms, with no >25 ms samples; this instrumented recording cannot accept C1 timing or physical presentation.

The bounded listener writer retains all 22,366,208 floats in 89,464,832 data bytes, peak queue 2/32. Signal/file checks do not establish audible quality. Normal/slow playback remains available in `Builds/quality/C2/uphill-clearance/player01/replay.mp4`; it has not been continuously watched or heard.

Actually opened four new gameplay frames: Taren step 78 +1.25 s and Sela step 166 +1.25/+1.75/+4.50 s. Four corresponding older Sela views and one controlled side reconstruction were also opened. The new late Sela frame still shows a high, deeply bent stepping leg; the retained live angles are about **135.64/60.88 degrees**. This is not accepted as convincing continuous motion.

The new recording also catches **stationary companion Taren** clearly crouched at step 166, unlike the earlier selected frames where he was still moving. He remains in non-transition Idle with zero motor velocity. Hips stay **0.631–0.646 m above the actor root**, knees about **73–106 degrees**, while `RequestedSupportDrop` is **zero**. This supports testing an idle-only vertical support correction rather than weakening foot/knee bounds or assuming the drop is necessary for reach. No such correction is included in this checkpoint.

For 300 non-transition Sela ascent frames, the separate single-clip audit agrees with the live skeleton within **0.710 mm at the hips** and **0.149 degrees at the knees**. This narrows the reconstruction uncertainty for those frames; it does not validate transition mixtures or the companion's earlier history. The actual image and live-pose rows remain the observation record.

## Preservation and next work

The exact prior `324a6db` players are archived under `Builds/quality/C2/uphill-clearance/prior-Windows*`. Current source/package and raw-evidence inventories are alongside them. Nathan's two saves and all 243 frozen `a3aaad6` chapter files/profile match. Known Unity import/private-package side effects are archived and restored; no paid asset source is staged.

Test the recorded idle-height case while preserving the established targets, anatomical knee solution, pause behavior and all contact/continuity assertions. Recheck both heroes/forms and actual player images after any supported repair. Keep Sela's stepping concern, continuous audiovisual/physical observations and every unmet C1–C10 gate OPEN/UNVERIFIED, and continue independent production work.
