# C2 locomotion — implementation in progress

2026-09-26. Implementation **OPEN**. Targeted facing and collision-speed regressions **PASSED**; continuous motion observation and physical-controller feel **UNVERIFIED**. This is not C2_MOTION_OK.

The donor's baked root orientation leaves the visible hips about 28 degrees right of travel during Run, about 20 during Sprint and 7 during Walk. Independent bilateral hip/shoulder spans, calibrated against actual baked mesh poses, establish the bias. Keeping original root orientation was tested and rejected: it turns the visible body backward and produces inconsistent cycle measurements. No actor or whole-prefab yaw offset was applied.

Clip import rotation offsets are now Walk 7°, Run 28°, Sprint 20°, with root orientation still baked. `PresentationUpgrade.AlignLocomotion` changes only those clips, and the intake specs preserve the values on rebuild. The four hero/form regressions sample each clip at 60 phases facing an independently specified 73° travel direction. `Builds/quality/C2/facing-red.xml` rejects all four original combinations, including Taren Shaped Run's 27.873° mean. The unchanged test passes 4/4 with zero skips in `facing-green.xml`. The comparison audit puts corrected mean pelvis yaw between -0.26° and 1.04°, with peaks below 3.6° across the twelve hero/form/clip cases.

Editor screenshot capture initially reused a cached GPU skin pose. The corrected audit bakes the currently evaluated skinned mesh into a temporary render mesh before each still. Opened corrected front/top views of both Shaped runs under `clip-offset-comparison`; original renders remain available but do not establish pose acceptance. Foot-to-toe axes are not yet calibrated as visible contact measurements and cannot prove stride quality.

GroundMotor now measures planar displacement after CharacterController collision, on the animator's unscaled clock. Desired motion remains separate. Disabled/dead/paused/stale motors report zero; external teleports do not become stride speed. The original wall regression reported 6.7 m/s while physically blocked and failed. `station-motion-green.xml` now passes both that unchanged collision test and the camera constant-speed test (2/2, zero skips). Normal saves are shielded by the launcher.

Still required: directional aiming/strafe/backpedal, start/stop/turn treatment, calibrated planted-foot drift, attachments and 30/60/120 clock checks; rebuilt ordinary-input hero/form loops and observed normal/slow-motion review. The native computer-use pipe is unavailable, so saved video has not been watched through that tool.

`foot-trajectories/feet.csv` samples each retargeted Walk/Run/Sprint at 240 phases for all four generated bodies, after alignment. Raw ankle/toe heights and displacement are diagnostic, not a calibrated sole-contact pass. Low-ankle backward speeds differ substantially from the driver's hardcoded 2.1/4.5/7.4 m/s values; cadence/stance work remains required.
