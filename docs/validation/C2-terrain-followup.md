# Terrain contact and reversal follow-up — October 6

Engineering candidate based on `07eaf9f`. **C1/C2 acceptance remains OPEN.** The frozen `a3aaad6` chapter has not been replaced. Continuous normal/slow-motion viewing, physical scan-out and controller feel remain UNVERIFIED; opened poses and numerical passes do not establish those results.

## Reproduction and original-source controls

The retained Sorrel failures now have committed root/heading/clip/phase fixtures. They sample the actual generated soles by baking the mesh, independently of the solver's contact targets. The fixtures include the preceding second and the complete failing route step, including reversals; the existing half-second contact warm-up is explicit. Joint continuity is measured throughout each non-transitioning clip after that warm-up, not only in central foot stance. Motor heading is removed from these joint measurements so a legitimate whole-body turn is not mistaken for knee motion.

| Recorded case | Original `07eaf9f` | Current engineering candidate |
|---|---:|---:|
| Sela pad penetration | 172.761 mm | 0 measured penetration |
| Taren shoulder contact drift | 66.014 mm | 2.923 mm |
| Pad knee angular-speed / allowed limit | 1.345 | 0.963 |
| Pad knee movement-speed / allowed limit | 3.009 | 0.717 |
| Shoulder knee angular-speed / allowed limit | 1.270 | 0.814 |
| Shoulder knee movement-speed / allowed limit | 7.318 | 0.796 |

The candidate's pad drift is 22.840 mm. The unchanged thresholds are 50 mm drift and 30 mm penetration, plus the existing whole-step knee limits: 1,200/3,000 deg/s and 6/15/18 m/s for walk/run/sprint. The original eight flat-ground knee checks still run unchanged. Sixteen new steep cases run both ±39° directions at 8 cm and 22 cm root clearance, with the same sample-count, speed and non-frozen-swing assertions. The higher clearance covers the capsule standing above a ramp, which an 8 cm-only fixture missed.

All eighteen new EditMode cases fail on the original source; all eight previously validated flat-ground cases pass on that control. The original cadence/stride expressions were preserved for the control, with ignored optional grade parameters only to compile the new fixture. Candidate files were restored in `finally`. A separate original live-Animator test rejects six of eight ±39° hero/form cases. A comparison against the pre-knee `49df94f` solver passes the pad fixture but fails the shoulder at 66.139 mm: the shoulder defect predates the knee repair, while the demonstrated pad regression does not.

Raw controls: `Builds/quality/2026-10-06-terrain-followup/{terrain-replay-red,pre-knee-terrain,steep-red,original-final-checks}.xml` and associated logs. Fixture sources are the retained `2026-10-06-legs/player-shaped60-graded` step 118 and `player-shaped60-southwest` step 78. Their original captures and failed analyses remain intact.

## Repair and remaining review

The pad failure occurred after the requested foot position had been sampled: bounded reach moved the toe from outside the pad across its raised edge. The correction now queries both final heel/toe footprints, including the reached and rotated foot after solving. It does not extrapolate one lower ground plane through the platform.

Steep support uses a terrain-aligned leg pose, a bounded correction for the capsule's extra clearance, and a torso balance correction. Cadence accounts for travel along the support surface; the walking increase is shared with stride length. Body support, terrain alignment and stride changes use the unscaled clock and hold during pause. The anatomical knee direction and extension reserve remain bounded; the reserve is 25 mm on flat ground with up to 40 mm more on steep ground. Gameplay motor speed, donor clips, generated bodies and collision are unchanged.

The recorded reversals exposed a separate discontinuity: the motor was already turning the actor, but the foot solver rotated free-travel hips another 180° when residual velocity changed sign through zero. Free-travel facing now turns gradually toward measured travel, preserving collision redirection without that one-frame flip. Deliberate target-facing/reverse locomotion retains its directional treatment. Walking heel release and toe-off now blend over longer intervals without changing the measured central stance windows.

The recorded pad fixture also logs a maximum reach correction of 83.983 mm, including its transient trajectory; this is not a passed 80 mm controlled-matrix result. Its asserted contact and knee bounds pass, while the separate final controlled matrices retain and pass the existing 80 mm reach limit. Continuous entry/exit review remains necessary.

The existing ranged/skill readers execute after the foot pose (`GroundFeet` order 500, `GroundSkillContact` 620, `RangedContact` 700), so they read the corrected wrist. The terrain correction applies to locomotion and moving fire; full action clips retain their authored poses. Steep-terrain action blends still need continuous review and are not accepted by the locomotion measurements.

Rejected experiments are retained as `GroundFeet-candidate*.cs.txt`, corresponding XML/logs, and `Builds/quality/C2/oct6-steep-*`. Notable rejections include a contact-only pass with excessive knee speeds, a higher-clearance live failure, and a terrain pose that leaned backward like water-skiing despite numerical passes. Opened side renders `oct6-steep-side28` exposed that posture. `oct6-steep-side31` shows the lower-spine balance iteration; these are intermediate stills, not final movement acceptance. Final player and matrix results must be recorded before a handoff is described as validated.

## Integrated verification

- Full EditMode: **119/119 pass**, zero skipped (`editmode-final.xml/log`).
- First full PlayMode: **229/231 REJECTED** (`playmode-first.xml/log`). The root-heading-only experiment failed the existing collision-redirected facing contract and a Shaped sideways contact case (134.254 mm drift). Both assertions are preserved. The revised gradual measured-direction correction passes all five live RuntimeStride cases; the final full suite passes **231/231**, zero skipped (`playmode-final.xml/log`).
- Offline replay/performance/resume/audio/timing/package controls: **119/119 pass**, zero skipped.
- Final flat, +10 degree, +39 degree and -39 degree directional/reverse contact matrices: **384/384 pass**. All original drift, penetration, contact-lift, reach, shin-separation and sample-count limits are unchanged. CSV hashes and maxima are in `C2-terrain-results.json`.
- Four new pause cases pass on the candidate and all fail when the pause guards are deliberately removed (`pause-mutant-red.xml/log`); the candidate is restored in `finally`. The initial malformed mutant compilation is retained separately and is not counted as a behavioral rejection.
- Final side poses actually opened: `Builds/quality/C2/oct6-steep-side-final39/{TarenNatural-2.600-0-left,SelaNatural-2.600-0-right,TarenShaped-6.700-0-left,SelaShaped-6.700-0-right}.png`. The torso balance differs from the rejected intermediate. These remain selected static poses, not a continuous-movement pass.
- Release/development builds and ordinary-player recaptures: pending in this working report.
- The frozen chapter still matches all **243 files / 616,521,326 bytes** in its `a3aaad6` manifest (`frozen-chapter-verified.json`). No missing, extra or changed player files were found.
- Nathan's two LocalLow saves retain their exact initial hashes and file count after the full suites (`saves-before.json`, `saves-after-suite.json`).

## Presentation remains unresolved

Before runtime edits, the unchanged synchronized release player completed `station-decks` in 124.667 seconds / 7,481 focused frames: p95 17.033 ms, p99 17.184 ms, worst 25.135 ms, no frame above 33.3 ms. The unchanged pad route also passed on a different realized trajectory. Neither repeat erases the retained quarter-second swap-chain stalls or the exact pad-edge failure.

A read-only physical-monitor query returned DDC/CI D6 value 1 from both monitors while WMI still reported generic Availability=8. The console was active/unlocked. This adds telemetry; it does not establish the cause of the earlier waits or prove tear-free physical output. The desktop helper again failed with `Computer Use native pipe is unavailable`; no driver, power-plan, security or monitor setting was changed. Final synchronized release/development first/warm runs remain due. C1, C2, C7 and C8 are not accepted, and C9–C10 campaign production has not advanced during this defect repair.
