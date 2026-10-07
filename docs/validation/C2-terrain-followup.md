# Terrain contact and reversal follow-up — October 6

Candidate 45 is committed and pushed as `e93bf13`, following `8685dca`; its three exact runtime source hashes accompany every current player recording. The three captured runtime files match `e93bf13` after Git line-ending normalization (`committed-source-binding.json`); historical build manifests remain unchanged. [Machine-readable evidence](C2-terrain-results.json). `07eaf9f` remains the original defect control. **C1/C2 acceptance remains OPEN.** The frozen `a3aaad6` chapter has not been replaced. Continuous normal/slow-motion viewing, physical scan-out and controller feel remain UNVERIFIED; opened poses and numerical passes do not establish those results.

## Reproduction and original-source controls

The retained Sorrel failures now have committed root/heading/clip/phase fixtures. They sample the actual generated soles by baking the mesh, independently of the solver's contact targets. The fixtures include the preceding second and the complete failing route step, including reversals; the existing half-second contact warm-up is explicit. Joint continuity is measured throughout each non-transitioning clip after that warm-up, not only in central foot stance. Motor heading is removed from these joint measurements so a legitimate whole-body turn is not mistaken for knee motion.

| Recorded case | Original `07eaf9f` | Current engineering candidate |
|---|---:|---:|
| Sela pad penetration | 172.761 mm | 0 measured penetration |
| Taren shoulder contact drift | 66.014 mm | 2.928 mm |
| Pad knee angular-speed / allowed limit | 1.345 | 0.963 |
| Pad knee movement-speed / allowed limit | 3.009 | 0.717 |
| Shoulder knee angular-speed / allowed limit | 1.270 | 0.814 |
| Shoulder knee movement-speed / allowed limit | 7.318 | 0.804 |

The candidate's pad drift is 22.838 mm. The unchanged thresholds are 50 mm drift and 30 mm penetration, plus the existing whole-step knee limits: 1,200/3,000 deg/s and 6/15/18 m/s for walk/run/sprint. The original eight flat-ground knee checks still run unchanged. Sixteen new steep cases run both ±39° directions at 8 cm and 22 cm root clearance, with the same sample-count, speed and non-frozen-swing assertions. The higher clearance covers the capsule standing above a ramp, which an 8 cm-only fixture missed.

All eighteen new EditMode cases fail on the original source; all eight previously validated flat-ground cases pass on that control. The original cadence/stride expressions were preserved for the control, with ignored optional grade parameters only to compile the new fixture. Candidate files were restored in `finally`. A separate original live-Animator test rejects six of eight ±39° hero/form cases. A comparison against the pre-knee `49df94f` solver passes the pad fixture but fails the shoulder at 66.139 mm: the shoulder defect predates the knee repair, while the demonstrated pad regression does not.

Raw controls: `Builds/quality/2026-10-06-terrain-followup/{terrain-replay-red,pre-knee-terrain,steep-red,original-final-checks}.xml` and associated logs. Fixture sources are the retained `2026-10-06-legs/player-shaped60-graded` step 118 and `player-shaped60-southwest` step 78. Their original captures and failed analyses remain intact.

## Repair and remaining review

The pad failure occurred after the requested foot position had been sampled: bounded reach moved the toe from outside the pad across its raised edge. The correction now queries both final heel/toe footprints, including the reached and rotated foot after solving. It does not extrapolate one lower ground plane through the platform.

Steep support uses a terrain-aligned leg pose, a bounded correction for the capsule's extra clearance, and a torso balance correction. Cadence accounts for travel along the support surface; the walking increase is shared with stride length. Body support, terrain alignment and stride changes use the unscaled clock and hold during pause. The anatomical knee direction and extension reserve remain bounded; the reserve is 25 mm on flat ground with up to 40 mm more on steep ground. Gameplay motor speed, donor clips, generated bodies and collision are unchanged.

The recorded reversals exposed a separate discontinuity: the motor was already turning the actor, but the foot solver rotated free-travel hips another 180° when residual velocity changed sign through zero. Free-travel facing smooths measured direction only during a motor-heading turn and its 0.2-second braking recovery. Once that turn settles, collision-redirected travel is followed directly; the globally damped candidate was rejected in the player. Deliberate target-facing/reverse locomotion retains its directional treatment. Walking heel release and toe-off now blend over longer intervals without changing the measured central stance windows.

The recorded pad fixture also logs a maximum reach correction of 83.983 mm, including its transient trajectory; this is not a passed 80 mm controlled-matrix result. Its asserted contact and knee bounds pass, while the separate final controlled matrices retain and pass the existing 80 mm reach limit. Continuous entry/exit review remains necessary.

The existing ranged/skill readers execute after the foot pose (`GroundFeet` order 500, `GroundSkillContact` 620, `RangedContact` 700), so they read the corrected wrist. The terrain correction applies to locomotion and moving fire; full action clips retain their authored poses. Steep-terrain action blends still need continuous review and are not accepted by the locomotion measurements.

Rejected experiments are retained as `GroundFeet-candidate*.cs.txt`, corresponding XML/logs, and `Builds/quality/C2/oct6-steep-*`. Notable rejections include a contact-only pass with excessive knee speeds, a higher-clearance live failure, and a terrain pose that leaned backward like water-skiing despite numerical passes. Opened side renders `oct6-steep-side28` exposed that posture. `oct6-steep-side31` shows the lower-spine balance iteration; these are intermediate stills, not final movement acceptance. The final matrices and current player results below are engineering evidence; the movement gate is not accepted.

## Integrated verification

- Full candidate-45 EditMode: **131/131 pass**, zero skipped (`editmode-final.xml/log`), including all original eight flat-knee assertions and 34 new terrain/replay/pause/facing/reverse cases. Focused checks also pass **42/42**.
- Full candidate-45 PlayMode passes **231/231**, zero skipped (`playmode-final.xml/log`), including all five live RuntimeStride cases. The prior candidate-39 suite is preserved separately (`playmode-candidate39.xml/log`). The earlier **229/231 rejection** remains in `playmode-first.xml/log`: root-heading-only facing broke the existing collision contract and a Shaped sideways contact case reached 134.254 mm drift. Both assertions are preserved.
- Offline replay/performance/resume/audio/timing/package controls: **119/119 pass**, zero skipped. Their implementation has not changed during this follow-up.
- Candidate-45 flat, +10 degree, +39 degree and -39 degree directional/reverse contact matrices: **384/384 pass** (`oct6-{flat,mild,steep-up,steep-down}-final45`). All original drift, penetration, contact-lift, reach, shin-separation and sample-count limits are unchanged.
- Four pause cases pass and all fail with pause guards deliberately removed (`pause-mutant-red.xml/log`). The initial malformed mutant compilation is retained separately and is not a behavioral rejection.
- Both release and development players build from the same candidate-45 runtime source. The players were built from candidate-45 working source based on `8685dca`; their historical manifests retain that HEAD value, which alone is not the runtime identity. `source-state.json` supplies the exact runtime hashes. `final-build-provenance.json` verifies both current packaged assembly/content sets against their captured manifests and the candidate source hashes.
- The frozen chapter still matches all **243 files / 616,521,326 bytes** in its `a3aaad6` manifest (`frozen-chapter-verified.json` and the final repeat `frozen-chapter-verified-final.json`). No missing, extra or changed player files were found.
- Nathan's two LocalLow saves match their exact original hashes and file count after the full candidate-45 suite (`saves-after-suite-final45.json`). The existing save shield restored both files; `saves-after-final.json` confirms the same hashes/count after every player run.

## Fresh-player rejection and scoped facing correction

The first `8685dca` release pad capture (`pad60-final`) is **REJECTED** despite passing route completion and foot contact. It records 232.799 seconds / 13,970 focused frames with ordinary VSync 1 and no software cap, 23.755 mm maximum contact drift and 5.999 mm minimum central clearance. The unchanged motion analyzer rejects 26 steady-facing cases, reaching 79.605 degrees of pelvis/travel error. The global direction smoothing delayed contact redirection occurring partway through an already established step; the old constant-direction runtime fixture started already redirected and missed this condition. The recording, audio, frames and failed analysis are retained. Opened player stills `078.png` and `124.png` do not constitute continuous review.

Four new `TerrainFacingTests` fail on `8685dca`, then pass when direction smoothing is scoped to motor turns and braking. Candidate 40's pad replay (`pad60-turn40-final`) passes: 232.701 seconds / 13,964 focused frames, 176 central contacts, 15.789 mm worst drift and 5.999 mm minimum clearance. Its shoulder replay (`slope60-turn40-final`) remains **REJECTED** for ten facing cases, reaching 14.680 degrees despite passing contact. The slope rotation skewed the visible hip span's horizontal heading.

Four further terrain-facing cases reject candidate 40 on both 39-degree slope orientations and all eight headings. Candidate 41 preserves the horizontal right axis while lifting it onto the terrain plane. Facing maxima fall from 10.597 / 11.005 / 16.462 / 16.736 to 3.941 / 4.402 / 9.249 / 9.371 degrees. However, the downhill contact matrix rejects four cases. Candidate 42 carries foot travel along the actual slope direction in that new basis; six downhill contact cases still reject it.

A faster 15 ms steep-running support response passes contacts but is **REJECTED** by complete reverse-step knee checks: Taren Natural reaches 15.517 m/s and Taren Shaped reaches 15.109 m/s, beyond the unchanged 15 m/s bound. Candidate 44 restores the original 25 ms response and begins support descent earlier with 25 mm of additional steep-running margin. It passes contact but one reverse-knee case remains rejected. Candidate 45 widens the reverse exit ramp by 0.02 cycle on steep running; full central support still begins at phase 0.075. All 42 focused checks and the four complete contact matrices pass together. Review found the terrain-facing fixture omitted grade from its stride calculation; it now uses the runtime calculation. All four facing cases still reject candidate 40 in `terrain-facing-grade-control.xml/log`.

The four new reverse-diagonal cases exercise both slope signs and both rear diagonals. **Three fail on original `07eaf9f`; Taren Natural passes that original control.** Taren Natural does reject the failed faster-response candidate 43. Thus every new case has a demonstrated bad-state rejection, without pretending all four reject the original. `original-reverse-checks.xml/log/console` retain both the actual 3/4 rejection and the helper's incorrect expectation of four failures; source restoration ran in `finally`. All intermediate sources, XMLs, logs and failed matrices remain available.

## Current player evidence and observation limits

All captures use ordinary virtual gamepad input, both heroes, fresh isolated saves, and exclusive player focus. Ordinary 60 fps routes retain startup VSync 1 / cap -1. Capture/motion instrumentation makes them C2 diagnostics, not clean C1 performance evidence. Explicit 30/120 fps VSync-off runs, when present, are alternate-clock diagnostics only.

| Candidate-45 recording | Seconds / focused frames | Central contacts | Worst foot drift | Minimum sole clearance | Result |
|---|---:|---:|---:|---:|---|
| Steep shoulder, synchronized | 232.684 / 13,963 | 180 | 7.869 mm | 2.998 mm | Measured pass |
| Raised pad, synchronized | 232.882 / 13,975 | 173 | 26.053 mm | 5.999 mm | Measured pass |
| Civilian 60 fps, synchronized | 222.349 / 13,343 | 150 | 12.849 mm | 5.999 mm | Measured pass |
| Combat 60 fps, synchronized | 251.500 / 1,596 | 65 | 20.924 mm | 3.938 mm | **REJECTED: missing samples** |
| Civilian 30 fps, diagnostic | 223.228 / 6,698 | 134 | 14.095 mm | 5.998 mm | Measured pass |
| Combat 30 fps, diagnostic | 233.391 / 7,003 | 132 | 5.608 mm | 3.943 mm | Measured pass |
| Civilian 120 fps, diagnostic | 221.188 / 26,537 | 148 | 13.204 mm | 5.998 mm | Measured pass |
| Combat 120 fps, diagnostic | 231.547 / 27,780 | 185 | 24.606 mm | 5.995 mm | Measured pass |

All four explicit 30/120 fps diagnostics pass the unchanged analyzer. The synchronized combat-form 60 fps capture is **REJECTED**, with p95 266.182 ms and worst 279.825 ms while Windows reported the display off before launch. Its 1,596 samples are insufficient; the surviving foot metrics do not accept that run. Repeat it on a confirmed awake display after C1 is resolved. The raw recordings live under `Builds/quality/2026-10-06-terrain-followup/*-candidate45-final` with source/build hashes, full-rate motion/frames, captured video/audio and unchanged analyzer output.

Opened candidate-45 player images: `slope60-candidate45-final/078.png`, `pad60-candidate45-final/124.png`, and `natural60-candidate45-final/078.png`. Extracted 15 fps frame sequences actually opened: slope steps 78 and 166 (both heroes' slow uphill returns), and pad step 118 including its following transition. They show knee progression and supported feet at the pad, but the steep walk's weight/balance still requires continuous normal-speed review. The sequences retain source timestamps and are not a substitute for watching motion. Final flat-ground images also opened: `shaped120-candidate45-final/{078,166}.png` and `natural30-candidate45-final/078.png`; these show selected poses only.

`Builds/quality/2026-10-06-terrain-followup/Review terrain candidate.html` offers normal, half and quarter speed, named route-step jumps and direct recordings. Candidate-45 clips appear first; rejected comparisons remain selectable. A viewer can mark exact moments and save timestamped observation notes. Continuous viewing/listening, physical screen output and controller feel remain **UNVERIFIED** with the available viewer. A specific human review request is pending. No screenshot or captured-video timing claim accepts tearing.

## Presentation remains unresolved

Before runtime edits, the unchanged synchronized release player completed `station-decks` in 124.667 seconds / 7,481 focused frames: p95 17.033 ms, p99 17.184 ms, worst 25.135 ms, no frame above 33.3 ms. The unchanged pad route also passed on a different realized trajectory. Neither repeat erases the retained quarter-second swap-chain stalls or the exact pad-edge failure.

A read-only physical-monitor query returned DDC/CI D6 value 1 from both monitors while WMI still reported generic Availability=8. The console was active/unlocked. This adds telemetry; it does not establish the cause of the earlier waits or prove tear-free physical output. The desktop helper again failed with `Computer Use native pipe is unavailable`; no driver, power-plan, security or monitor setting was changed. The current candidate-45 clean release first/warm run is **REJECTED**: 259.567 seconds / 1,644 focused frames, p95 265.582 ms, p99 267.494 ms, worst 279.445 ms, and 987 frames above 100 ms. Navigation and shop interactions also fail. No capture/profiler or competing Unity workload was present. `release-first-warm-final/{analysis.json,frame-times.svg,frames.csv,run.json,build.json}` preserve the complete failure.

Before and after that run, DDC/CI changed from the earlier two value-1 replies to one failed read and one value-2 reply. Value 2 denotes DPMS standby in the [MCCS feature definitions documented by ddcutil](https://www.ddcutil.com/vcpinfo_output/). A new read-only Windows power-notification query independently reports **session display 0 and console display 0 (off)**, in session 1 (`display-os-state45.json`). Microsoft defines these states in [Power Setting GUIDs](https://learn.microsoft.com/en-us/windows/win32/power/power-setting-guids) and documents the immediate current-state callback in [PowerSettingRegisterNotification](https://learn.microsoft.com/en-us/windows/win32/api/powersetting/nf-powersetting-powersettingregisternotification). This establishes the observed display-off condition for this run; it does not prove the cause of every historical wait or physical tearing.

A bounded `ES_DISPLAY_REQUIRED | ES_CONTINUOUS` probe successfully set and restored the original state on the same native thread, but did not restore DDC replies or produce a display-on notification. All request/restore records are retained under `display-lease45/`. A read-only policy query reports a 1,200-second AC display timeout; `GetLastInputInfo` reports 5,210 seconds since desktop input. No power plan, monitor setting, driver, security permission or gameplay pacing policy was changed. A physical-display check and same-build powered-display comparison are pending. Do not repeat broad backend probes or assume an awake lease fixes the fault. The current development first/warm comparison is also **REJECTED**: 260.097 seconds / 1,666 focused frames, p95 265.627 ms, p99 273.995 ms, worst 278.191 ms, and 987 frames over 100 ms. Its full report and failed checkpoint list remain in `development-first-warm-final/`. The Windows session/console display notifications still return 0 afterward (`display-after-development45.json`) and after all final captures (`display-final-state.json`). C1, C2, C7 and C8 are not accepted, and C9–C10 campaign production has not advanced during this defect repair.

## Required next acceptance work

1. Obtain a visibly awake-display state and repeat the same packaged release/development first/warm route, recording OS display state before and after. Resolve any synchronized pacing/input failures without a VSync-off acceptance shortcut. Retain the display-off failures and earlier unsuccessful probes.
2. Recapture the rejected synchronized combat-form 60 fps route. Review ordinary movement continuously at normal/slow speed, including the steep walk's balance and terrain entry/exit, and inspect physical scan-out/controller feel when available. The local review page is ready; neither human observation request has been answered.
3. Only then close the appropriate C1/C2 gates and resume the production-plan order. C1?C10 and the complete game have not been completed. No current presentation candidate has replaced the frozen chapter.
