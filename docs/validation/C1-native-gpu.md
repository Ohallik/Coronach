# C1 — independent D3D12 camera timestamps, October 7

**C1 remains OPEN. Both standard players contain the opt-in diagnostic; no presentation candidate is validated.** The original awake Decks 34.197/42.504/62.284 ms failures and all three impossible Unity GPU values remain rejected and unchanged. This pass adds an independent query path, repairs a reproduced defect in its first prototype, and retains one changed Decks diagnostic. [Machine-readable results and hashes](C1-native-gpu.json).

## What changed

`-NativeGpu` uses owned C++ D3D12 timestamp queries around the main-camera callbacks. Each of 64 slots has a GPU-written frame identity and a private queue fence. The reader requires every replay sample, exact frame/step/time identities, positive raw integer spans, the right tag and completed fences. Subtraction occurs before float conversion because the endpoints exceed the double-precision exact-integer range. Unity's raw API/profiler readings are retained independently, including invalid values.

The end event flushes the timestamp commands before signaling the private fence. This adds submissions/synchronization, can include CPU submission gaps, and excludes later overlays and presentation. **It measures a diagnostic camera envelope, not GPU busy time, clean headroom or scan-out.** Launch flags and metadata exclude it from clean timing/headroom. Launch manifests now hash packaged native DLLs as well as managed code and scene content. Ordinary play does not instantiate the recorder.

The implementation follows [Microsoft's timestamp-query contract](https://learn.microsoft.com/en-us/windows/win32/direct3d12/timing), [Unity's native rendering interface](https://github.com/Unity-Technologies/NativeRenderingPlugin), and the installed Unity 6000.4.7f1 plugin headers. Queue-access callbacks run on the submission thread; recording callbacks use Unity's active command list. No stable-power-state request, driver change or machine-wide compiler installation was made. The pinned official Zig 0.17.0 archive hash and every source/header/compiler/binary hash are recorded. Compiler runtime notices ship beside both players; Unity headers are consumed in place rather than vendored.

## Rejected prototype and controls

The first version used Unity's next-frame fence. Its short synchronized pilot passed 1,082 frames, and an intentionally wrong GPU tag rejected correctly. The uncapped Tallow run then **REJECTED**: frame 6698 returned frame 6634's tag, exactly 64 slots earlier, even though the sampled Unity frame fence had reached 13077. Only 496 native results were produced; all 91,986 replay samples remain. The preceding frame shared that Unity fence value. This is a reproduced completion-identity bug in the new diagnostic, **not proof of the old Unity timing corruption**.

The private queue fence fixes this producer path. Schema 2 explicitly rejects the old framework-fence provenance. Its short pilot has 1,083 complete focused samples; a new wrong-tag DLL rejects runtime and independent analysis with status 15 across an 18.011 s / 1,082-sample focused run. The wrapper restores the exact canonical DLL. Original source/binaries, both wrong-tag controls, the failed full prototype and the old passing reports all remain under `Builds/quality/C1/native-gpu/`.

Two launch-exclusion checks fail on the original analyzer. A deliberately accepting coverage reader causes 14 failures; the old provenance reader fails the new fence check. **188 offline checks and 3 affected ContinuousReview PlayMode tests pass, zero skipped.** Both final standard builds succeed. This is not a fresh full C7 suite. Whitespace-only source cleanup was verified and rebuilt; the pre-cleanup source and packages are archived too.

## Tallow development, changed independent diagnostic

The unchanged authored two-minute uncapped route completes **120.021 s / 91,093 focused samples**, with 91,093 matching private-fence results and process exit 0. Both display-state endpoints report on; the headroom route explicitly uses VSync 0. Native camera envelopes: median **1.129152 ms**, p95 **1.311904 ms**, p99 **1.443296 ms**, maximum **11.180192 ms**. All Unity GPU values are retained; none exceeds 1,000 ms in this run.

Raw native endpoints are around 1.7914e18 ticks at 1 GHz. Expressed in milliseconds, their magnitude resembles the original impossible ~1.7914e12 ms values. This supports the endpoint/duration or missing-start hypothesis, but establishes neither a Unix clock epoch nor the Unity backend cause. Do not correct, replace or filter any original duration. Tallow development headroom remains invalid/unaccepted.

## Decks release, changed ten-minute diagnostic

The same authored ten-minute route runs once with native queries and frame-clock records, without binary profiling or audiovisual capture: **629.363 s / 37,762 focused samples**, every native identity/fence complete, process exit 0 and VSync 1. Both display-state endpoints report on. The output folder is about 23.6 MB, compared with the earlier 4.827 GB segmented trace; artifact size does not prove zero timing disturbance.

The largest intervals are **29.3425 ms at sample 29329 / step 122 (port wall contact)** and **25.2760 ms at sample 2107 / step 9 (first choices opening)**. No sample exceeds 33.3 ms. Native envelopes are median 1.694912 / p95 2.016928 / maximum 6.492576 ms. Collection counts do not change around the 29.3425 ms interval. All samples remain; the quiet original failure intervals do not explain their earlier rejections.

Raw Windows QPC gives a unique enclosing Unity API interval for the 29.3425 ms sample: frame start **19692105032863**, previous replay QPC **19692105037604**, current replay QPC **19692105331212**, present call **19692105364192** (10 MHz). That API record reports main work **1.0501 ms**, render work **2.4003 ms** and present wait **24.7429 ms**. Adjacent explicitly tagged native camera spans are **1.693728 / 2.143424 ms**. This supports a wait-dominated interpretation of this smaller observed event; it does not identify the OS/driver cause or establish that the original 34–62 ms failures share it. The association uses clock enclosure, not an invented CSV row offset. Both long-interval brackets and raw neighbours are retained.

## Preservation and continuation

`packages.json` binds the final players to 25 exact build-input source/notice/DLL hashes. The final public C++/C# line-ending cleanup and notice trailing-space cleanup are separately bound in `commit-format-equivalence.json`; archived build inputs and committed source have identical non-whitespace bytes. No runtime or package bytes changed in that final formatting cleanup. Compiler and launch inventories additionally retain the native DLL's raw SHA-256. The preceding `df7edfa` C5 players are archived at `prior-Windows*`; the intermediate formatting packages at `before-format-Windows*`. The isolated native player has its own full inventory. The evidence inventory binds the captures, tests, controls, display reports and provenance files without committing ignored recordings or toolchain archives.

Nathan's current two saves and all 243 frozen `a3aaad6` files/profile match. Nine known Unity import/private-package side effects were archived and restored; no paid asset source is staged. `e93bf13` knees/terrain, later support/gait fixes and every earlier assertion/failure remain intact.

No new images were opened, continuous video watched, audio heard or physical controller/scan-out quality observed. Those observations stay **UNVERIFIED**, and all unmet C1–C10 gates stay open. Do not repeat these unchanged diagnostics for acceptance. Use the new clocks when a concrete failure recurs, then continue the remaining movement, combat/camera, first-chapter and campaign work independently.
