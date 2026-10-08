# C2 slow uphill balance repair — October 7

**C2 remains OPEN. Selected poses improve; continuous motion and uphill balance are not visually accepted.** Candidate 59 extends `e93bf13`'s terrain repair without changing motor speeds, donor clips, collision, the knee solver, turn/facing corrections or any existing assertion. The [C1 stability and GPU problems](C1-bounded-trace.md) remain separate open gates. The frozen `a3aaad6` chapter is untouched.

## Defect and repair

Reopened candidate-45 uphill sequences and side renders show a seated pose. The existing correction rotates the pelvis around the ground root with the terrain normal, displacing the hips downhill while the feet remain uphill. Counter-rotating the spine changes torso pitch but does not restore its horizontal support.

A new editor diagnostic projects the hips/chest midpoint vertically onto planar travel and compares it with the rearmost visible heel/toe centroid. At 1.2 m/s on a 39-degree ascent, after the first cycle and during central stance, the midpoint stays as much as **44–50 cm behind both soles**. This is a posture proxy, not a physical centre-of-mass calculation or a visual acceptance test. The regression uses independently baked bottom-foot vertices rather than solver targets; its baseline maxima are 37–44 cm, reflecting its different rear-edge reference.

The repair restores up to 65% of the pelvis's horizontal displacement toward a gravity-aligned position **after** calculating the existing foot targets. Support descent blends toward vertical with that correction. It applies to uphill walking with a short stride, fades as the extended walk exhausts the donor's reach, and follows changes over 0.2 seconds. The unchanged running/sprinting solution remains in place once the walking transition settles. Heel and toe landing ramps lengthen only with this balance correction; the central toe plateau and release endpoints remain intact. A small support-response adjustment avoids the newly exposed knee-speed peaks during acceleration/deceleration.

Full gravity restoration across every gait was rejected: it improved the posture number but broke contact and knee continuity. A render that looks more upright cannot waive those failures. The final bounded correction is an incremental repair, not acceptance of brisk uphill movement, running balance or the whole C2 gate.

## Controlled evidence

| Hero / form, 1.2 m/s uphill | Candidate 45 torso behind rear sole | Candidate 59 | Candidate 59 planted drift |
|---|---:|---:|---:|
| Taren Natural | 443.348 mm | 46.589 mm | 20.660 mm |
| Taren Shaped | 500.964 mm | 87.366 mm | 19.246 mm |
| Sela Natural | 469.394 mm | 74.547 mm | 19.756 mm |
| Sela Shaped | 486.546 mm | 85.162 mm | 21.067 mm |

All **384** flat, +10-degree and ±39-degree directional/reverse contact cases pass. Across the complete matrix: worst drift **42.883 mm**, penetration **0 mm**, contact lift **37.073 mm**, reach correction **64.628 mm**, minimum shin separation **148.042 mm**. The original limits remain 50 / 30 / 40 / 80 / 70 mm respectively, with contact-count checks intact. Raw directories are `Builds/quality/C2/oct7-{flat,mild,steep-up,steep-down}-final59/`.

Sixteen new EditMode cases cover four independently baked balance checks, four slow/extended-walk continuity matrices, four repeated speed-change matrices and four pauses during active uphill balance adaptation. The speed changes traverse 1.2–2.6 m/s without resetting phase, with straight and diagonal uphill travel. All four balance cases fail on the untouched `e93bf13` solver. Six of the eight added continuity cases also fail there: the old coverage had missed slow Shaped steps and changing walking speeds. The other two pass on the baseline and reject failed intermediate candidates. All four new pause checks reject deliberate removal of the new balance pause guard; the original four running-pause cases remain. Existing whole-step limits remain **1,200/3,000 degrees/s**, **6/15/18 m/s** by gait and more than **25 degrees** of knee motion; no frozen-knee workaround is accepted.

Candidate 59 passes **147/147 EditMode**, **231/231 full PlayMode**, **7/7 affected replay PlayMode** after the native-clock extension, and **146/146 offline checks**, zero skipped. Both release and development players build. [Machine-readable results, source snapshot and artifact hashes](C2-uphill-balance.json) bind the candidate to its packages; replay manifests identify the earlier base `d7536a2`, so that base alone is not the candidate source.

## Fresh player evidence

All four runs use the rebuilt **development** player, full-rate motion samples and ordinary virtual gamepad input. Windows reports session and console displays off before/after. The initial synchronized 60 fps preflight therefore rejects **before launching**. The explicit VSync-off diagnostics below retain every gameplay step/assertion; captured routes defer live PNGs to avoid the previously isolated screenshot cost. Neither capture nor an alternate-rate motion pass accepts C1 or physical presentation.

| Diagnostic | Duration / samples | p95 / worst frame | Worst contact drift | Result |
|---|---:|---:|---:|---|
| Shoulder, 60 fps video/audio | 232.432 s / 13,946 | 17.129 / 20.511 ms | 12.046 mm | Motion PASS |
| Pad, 60 fps video/audio | 232.333 s / 13,940 | 17.164 / 20.054 ms | 24.056 mm | Motion PASS |
| Shoulder, 30 fps | 233.746 s / 7,013 | 33.711 / 36.565 ms | 6.060 mm | Motion PASS |
| Shoulder, 120 fps | 231.546 s / 27,781 | 8.701 / 11.768 ms | 34.972 mm | Motion PASS |

Minimum central sole clearance is approximately 3 mm in all four. The 30 fps run naturally exceeds the 25/33.3 ms counts; those rows remain in its report. Raw results and `Review candidate 59.html` are under `Builds/quality/2026-10-07-bounded-trace/`. Normal/quarter-speed controls and preserved candidate-45 comparisons are available, but the videos have **not** been continuously watched or heard.

## Retained rejected approaches

Raw source snapshots, logs, XML, CSV and renders are under `Builds/quality/2026-10-07-bounded-trace/` and `Builds/quality/C2/oct7-balance-*`.

| Candidate | Rejection / finding |
|---|---|
| 46 | Full horizontal restoration: 19/46 affected tests fail; all four slow contact captures fail, including 249 mm reach correction and 125 mm slip. |
| 47 | Vertical support and a different knee pole: slow contact captures pass, but 21/46 tests fail. Rejected. |
| 48–51 | Walking scope, alternative pole, landing reserve and partial restoration are explored separately; each retains five failed tests. |
| 52 | 50/50 affected checks pass. Additional acceleration/deceleration coverage then exposes missing cases. This intermediate pass is not the final result. |
| 53–55 | Two added speed-change failures remain in each version; full 143-case runs for 53/54 are rejected. |
| 56 | One speed-change failure remains. Additional peak diagnostics identify the bent support-leg rise. |
| 57 | Slower support recovery worsens that motion: four failures. Rejected. |
| 58 | Faster response removes the knee-speed peaks but violates three knee-angle bounds. Rejected. |
| 59 | Bounded response, gradual landing and retained extension reserve pass the unchanged limits and complete contact matrix. Visual acceptance remains open. |

The first diagnostic/test compile mistakes, the original solver's newly measured failures and every failed experiment are retained. No assertion or failed row was deleted to produce the final pass.

## Actual observation and remaining gates

Actually opened in this work: the prior candidate-45 uphill sequence; earlier Taren Natural/Shaped steep side renders; new baseline Taren Natural left and Sela Shaped right views; candidate-47 Taren Natural left; candidate-53 Taren Natural left and Sela Shaped right; final candidate-59 Taren Natural/Shaped left and Sela Natural/Shaped right. Those controlled 1.2 m/s stills show the hips closer to the feet and a less seated stance.

**Fresh gameplay stills retain a visual rejection.** Opened `slope60-unsynced59/step78-1.25.png` and `step166-1.25.png`, extracted by recorded capture index after the player exited. Taren's selected pose is more upright, but Sela's slow shoulder return still looks deeply crouched and her companion Taren visibly rearward. The controlled improvement does not establish convincing ordinary movement. Investigate the actual route's slope, stride and support drop before extending the correction. Keep these images and candidate 59 as evidence; numerical passes do not waive the remaining posture problem.

The native desktop pipe remains unavailable after reset. Continuous motion, normal/slow audiovisual quality, controller feel and physical scan-out remain **UNVERIFIED**. Fresh synchronized shoulder/pad recordings remain due when displays are on; do not repeat the completed unsynchronized diagnostics unchanged. Nathan's saves and the frozen `a3aaad6` chapter/profile remain byte-for-byte preserved. Both previous candidate-45 packages are archived and verified against all 106 recorded hashes; the current standard packages remain **unvalidated**.
