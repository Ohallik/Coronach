# Screen presentation and leg motion — October 6

Nathan reported lag/tearing across the middle of the screen during movement and wonky legs. This is a renewed presentation rejection despite previous timing/contact passes. The repair is being validated; C2/C7/C8 acceptance remains open.

## Presentation

The previous D140 startup explicitly disabled VSync and used a software 60 fps cap. That removed the documented quarter-second waits but allowed unsynchronized presentation. Desktop startup now requests VSync 1 with `targetFrameRate=-1`; display refresh owns presentation. Batch tests retain their explicit 60 Hz clock. Alternate-rate/headroom replays still explicitly disable synchronization and remain separate diagnostics. Ordinary reference routes preserve the actual startup policy.

Unity recommends hardware synchronization for smooth desktop pacing: [targetFrameRate documentation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html). This change does not establish the cause of the October 4 driver/compositor stalls, nor does a Unity frame trace prove tear-free physical scan-out.

Two isolated release comparisons on this machine's 60 Hz display completed the ordinary Decks traversal/dialogue/shop route with synchronization enabled:

| Diagnostic | Seconds / focused frames | p95 / p99 ms | Worst ms | Frames >33.3 ms |
| --- | ---: | ---: | ---: | ---: |
| D3D12, VSync 1, cap -1 | 124.717 / 7,484 | 17.044 / 17.188 | 25.637 | 0 |
| D3D11, VSync 1, cap -1 | 124.717 / 7,484 | 17.068 / 17.207 | 26.959 | 0 |

Raw runs: `Builds/quality/2026-10-06-presentation/d3d12-sync1` and `d3d11-sync1`. These were diagnostic builds from dirty source based on 49df94f; packaged DLL/content hashes are retained. Neither run captured/profiled frames or lost focus. No graphics-backend change is necessary from this comparison. The production player retains its existing automatic API selection.

The boot regression rejects the actual old startup (`boot-red.xml`). The offline performance contract now requires synchronization, no competing software cap and the 60 Hz reference display, while preserving all frame-time/hitch/input-response limits. Changed tests reject the old analyzer (8 failures); all 38 analyzer tests pass after implementation, including unsynchronized, wrong-cap, wrong/missing-refresh and missing-policy controls. Higher-refresh displays use their refresh rate in ordinary play; these are not silently called measurements of the 60 Hz reference configuration.

A signed Intel PresentMon 2.6.0 portable tracer was obtained from the official release. Windows denied its ETW trace session; no scan-out CSV was produced, and no permission/group/driver change was made. Its probe stdout/stderr and executable hash remain under the presentation evidence folder. Physical tearing is consequently **UNVERIFIED**, including VRR/driver overrides. Do not describe this as a measured tear-free result.

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

Raw suite artifacts are in `Builds/quality/2026-10-06-legs/`. Nathan's two current LocalLow saves match the pre-run hashes and count (`2026-10-06-presentation/save-hashes-after-suite.json`). Final built-player checks and package replacement are next; the frozen chapter remains a3aaad6.

Continuation instructions: [NEXT_SESSION_PROMPT.md](../NEXT_SESSION_PROMPT.md). C1–C6 observation gaps, C7 acceptance, C8 map/story polish and C9–C10 production remain explicit there and in the production plan.
