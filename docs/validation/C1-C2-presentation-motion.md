# Screen presentation and leg motion — October 6

**Latest October 7 comparison:** unchanged candidate-45 packages pass clean synchronized Decks first/warm timing with Windows display-on snapshots before and after; the combat recapture also passes motion, with live-PNG capture overhead isolated separately. [Current evidence](C1-C2-awake-comparison.md). Physical tearing and continuous movement acceptance remain open. The rejection below is preserved historical evidence.

The later [terrain follow-up](C2-terrain-followup.md) records the edge/shoulder repair candidate, original-source controls and additional verification. This report preserves the earlier failures; they are not erased by later passes. The frozen chapter remains unchanged.

Nathan reported lag/tearing across the middle of the screen during movement and wonky legs. This is a renewed presentation rejection despite previous timing/contact passes. The knee-snap repair passes its regressions and flat-ground player diagnostics. This report preserves the original steep-ground and pad failures; the later terrain candidate passes their measured regressions. Synchronized presentation remains REJECTED, and C1/C2/C7/C8 acceptance stays open. The October 7 follow-up records fresh release/development failures with independently observed Windows display-off state; it does not establish a general driver cause or a tear-free result.

## Presentation

The previous D140 startup explicitly disabled VSync and used a software 60 fps cap. That removed the documented quarter-second waits but allowed unsynchronized presentation. Desktop startup now requests VSync 1 with `targetFrameRate=-1`; display refresh owns presentation. Batch tests retain their explicit 60 Hz clock. Alternate-rate/headroom replays still explicitly disable synchronization and remain separate diagnostics. Ordinary reference routes preserve the actual startup policy.

Unity recommends hardware synchronization for smooth desktop pacing: [targetFrameRate documentation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html). This change does not establish the cause of the October 4 driver/compositor stalls, nor does a Unity frame trace prove tear-free physical scan-out.

Two isolated release comparisons on this machine's 60 Hz display completed the ordinary Decks traversal/dialogue/shop route with synchronization enabled:

| Diagnostic | Seconds / focused frames | p95 / p99 ms | Worst ms | Frames >33.3 ms |
| --- | ---: | ---: | ---: | ---: |
| D3D12, VSync 1, cap -1 | 124.717 / 7,484 | 17.044 / 17.188 | 25.637 | 0 |
| D3D11, VSync 1, cap -1 | 124.717 / 7,484 | 17.068 / 17.207 | 26.959 | 0 |

Raw runs: `Builds/quality/2026-10-06-presentation/d3d12-sync1` and `d3d11-sync1`. These were diagnostic builds from dirty source based on 49df94f; packaged DLL/content hashes are retained. Neither run captured/profiled frames or lost focus. These early passes did not reproduce in final players. The candidate retains automatic API selection; no tested alternate backend resolved the later failure.

The boot regression rejects the actual old startup (`boot-red.xml`). The offline performance contract now requires synchronization, no competing software cap and the 60 Hz reference display, while preserving all frame-time/hitch/input-response limits. Changed tests reject the old analyzer (8 failures); all 38 analyzer tests pass after implementation, including unsynchronized, wrong-cap, wrong/missing-refresh and missing-policy controls. Higher-refresh displays use their refresh rate in ordinary play; these are not silently called measurements of the 60 Hz reference configuration.

A signed Intel PresentMon 2.6.0 portable tracer was obtained from the official release. Windows denied its ETW trace session; no scan-out CSV was produced, and no permission/group/driver change was made. Its probe stdout/stderr and executable hash remain under the presentation evidence folder. Physical tearing is consequently **UNVERIFIED**, including VRR/driver overrides. Do not describe this as a measured tear-free result.

## Final presentation rejection

The ordinary-startup final development run records p95 **265.406 ms**, p99 **276.471 ms**, worst **278.521 ms**; the release records **265.735 / 267.244 / 279.472 ms**. Both miss interactions/checkpoints. Per-frame Unity focus stays true and the runner finds no concurrent editor/player workload. Do not mistake the short probes' runtime `valid=true` (goals reached) for performance acceptance; the unchanged offline validator rejects their timing/sample count and short duration.

The development CPU profile at `Builds/quality/C1/oct6-development-sync-profile` localizes approximately 240–259 ms to `TimeUpdate.WaitForLastPresentationAndUpdateTime → WaitForTargetFPS → DXGI.WaitOnSwapChain`. Main-thread gameplay and submission work is a few milliseconds. This identifies the wait site, not its driver/display root cause. [Full comparison metrics and raw hashes](C1-presentation-diagnostics.json).

Tested and rejected: final D3D11 and D3D12; explicit same-value synchronization override; multithreaded rendering (log confirms threaded mode); direct rendering; queued-frame values 0/1; borderless and D3D11 exclusive modes; a one-Awake off/on re-arm. D3D11 BitBlt plus VSync 1 avoids quarter-second waits but runs around 32 ms/frame, outside the 60 fps budget. BitBlt with VSync off runs smoothly but remains unsynchronized. A diagnostic DwmFlush compositor clock also stalls and was removed from source (archived under the ignored evidence folder). No custom compositor workaround was accepted.

A read-only desktop check found conflicting monitor telemetry: generic WMI Availability=8 (offline), two physical monitor records Active=true, and WTS session 1 active/unlocked. A temporary thread-scoped display-awake request did not fix the wait and was restored in `finally`. The actual physical monitor state is unanswered. Computer-use failed with `Computer Use native pipe is unavailable` after its documented retries; no window input was attempted. None of these observations proves a monitor/driver cause. Do not repeat broad backend probes without a new hypothesis; establish physical display/desktop state and compare the same ordinary route first.

Microsoft documents [display idle behavior](https://learn.microsoft.com/en-us/windows/win32/power/system-sleep-criteria) and [WTS session flags](https://learn.microsoft.com/en-us/windows/win32/api/wtsapi32/ns-wtsapi32-wtsinfoex_level1_w); the state readings above are machine observations, not conclusions from those documents. Power plans, device drivers and security permissions were not changed. The frozen a3aaad6 chapter remains intact.

## Legs

Central planted-foot checks missed the landing/release portions of a step. The new audit samples every phase at 240 Hz over three cycles, both legs of both heroes, civilian walk/run and combat run/sprint. It compares the owned take and final correction with root clearance of 8 cm. A separate no-Mecanim-IK comparison did not remove the defect; built-in foot IK remains enabled.

The old solver approached full extension within 1 mm, inferred the knee pole from an already warped ankle axis, and released running contacts in 0.025 of a cycle. This produced unstable knee direction and very fast extension. The repair uses an anatomical pole that follows the lower body, leaves 2 cm of chain reach for knee bend, blends running landing/toe-off across the authored stance, and damps pelvis support on the unscaled clock (25 ms walk/run, 10 ms sprint). Maximum support drop is 16 cm. Pause reapplies the same correction without advancing smoothing. Motor speed, movement/camera controls, donor clips and generated bodies are unchanged.

The most severe walking defect was a **394.65 mm knee displacement in one 240 Hz interval** as the bend plane flipped. The candidate's largest walking knee displacement is **20.02 mm**. These root-relative measurements are separate from angle changes and expose why a stationary sole alone could falsely suggest acceptable motion.

All eight new whole-step regressions fail on the original runtime (`2026-10-06-legs/red.xml`, 0/8). They inspect knee-angle speed, knee displacement and non-frozen swing rather than the solver's own target. The candidate passes within the original bounds: 1,200 deg/s walk, 3,000 deg/s run/sprint; knee speed 6/15/18 m/s respectively; more than 25 degrees of swing per knee. Existing foot-contact assertions are retained.

| Maximum angle change per 240 Hz sample | Original correction | Candidate |
| --- | ---: | ---: |
| Civilian walk | 12.29° | 3.63° |
| Civilian run | 15.50° | 8.00° |
| Combat run | 17.36° | 10.45° |
| Combat sprint | 19.85° | 10.28° |

These are diagnostic measurements, not a visual quality score. [Per-body comparison and source CSV hashes](C1-C2-leg-comparison.json) retain the exact values. The original abrupt poses, intermediate knee-plane experiments and rejected slope/contact attempts remain under `Builds/quality/2026-10-06-legs/` and `Builds/quality/C2/oct6-*`.

The editor contact audit initially failed because its old edit-mode fixture invoked the animation driver without initializing CombatActor's cached Health. The fixture now invokes that lifecycle initialization before measurement; contact, reach, penetration, separation and sample-count assertions were not relaxed. The final 96-case directional/target-facing reverse matrix and the +10°/8 cm-clearance matrix pass. An earlier wrong audit invocation omitted reverse playback; its rejected evidence is retained separately.

Codex opened both heroes' candidate baked-pose contact sheets, a full-resolution Sela walking frame and matched original/candidate Taren walking stills. The candidate knees bend forward and clothing remains attached in those stills. This does not constitute watching continuous animation.

## Integrated status

Full EditMode: **97/97 pass**; full PlayMode: **230/230 pass**; offline controls: **119/119 pass**, zero skipped. The final EditMode repeat follows diagnostic assertion aggregation and removal of an unused solver parameter; no runtime behavior changed after the full PlayMode pass. `all-bounds-red.xml` rejects both excessive angle speed and knee displacement in all eight original cases. The live animation slope probe covers both heroes/forms uphill and downhill; worst uphill rendered-sole drift is 24.64 mm, within the unchanged 50 mm limit. The existing pause and moving-fire stride tests also pass.

Raw suite artifacts are in `Builds/quality/2026-10-06-legs/`. Nathan's two current LocalLow saves match the pre-run hashes and count (`2026-10-06-presentation/save-hashes-after-suite.json`). Both players build; final synchronized presentation is rejected as above, and the frozen chapter remains a3aaad6. Final save hashes are retained in `2026-10-06-presentation/save-hashes-final.json`.

Continuation instructions: [NEXT_SESSION_PROMPT.md](../NEXT_SESSION_PROMPT.md). C1–C6 observation gaps, C7 acceptance, C8 map/story polish and C9–C10 production remain explicit there and in the production plan.

## Ordinary movement and remaining terrain failures

These release captures use the actual virtual gamepad with `-DiagnosticVSync 0`, 60 fps, capture and motion telemetry. They are motion-only diagnostics, not synchronized C1 evidence. The first synchronized Natural run remains rejected for missing samples and a missed hero swap.

| Route | Seconds / samples | Worst central foot drift | Result |
| --- | ---: | ---: | --- |
| Civilian, both heroes | 222.180 / 13,331 | 15.62 mm | Motion diagnostic PASS |
| Combat, both heroes, clear graded ground | 232.283 / 13,937 | 13.90 mm | Motion diagnostic PASS |
| Combat, southwest basin shoulder | 232.545 / 13,953 | 65.86 mm | REJECTED: exceeds unchanged 50 mm limit |
| Combat, landing-pad crossing | 232.420 / 13,945 | 22.19 mm | REJECTED: 173.18 mm sole penetration |

[Exact summaries, failures and hashes](C2-oct6-player-motion.json). The original Sorrel fixture also rejects form coverage because the redesigned civilian pocket now begins at z=-7.5. Updated `motion_routes.py` regenerates the 30/60/120 flat-ground fixtures without altering any motion or form assertion. Only 60 fps was recaptured after this repair; 30/120 remain due for this runtime.

Keep `docs/quality/routes/c2-shaped-sorrel-slope-60.json` and `c2-shaped-sorrel-pad-60.json` as ordinary-input reproduction cases. The shoulder trace shows roughly 39° local ascent in a failing walking contact; the existing ±10° suite does not cover it. The pad case needs support sampling/foot placement investigation. Their failure on the previous pre-repair solver has not yet been measured, so do not label either a new regression or pre-existing defect without a comparison.

Opened final player stills: `player-natural60-diagnostic/087.png` and `player-shaped60-clear/124.png`, plus the rejected pad run's `071.png`. The 60 fps capture directories retain `replay.mp4`, raw motion and actual AudioListener mix. Continuous normal/slow video observation remains UNVERIFIED; opened stills do not establish convincing motion through all transitions. No replacement workshop was published.
