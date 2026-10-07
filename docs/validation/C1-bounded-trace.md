# C1 bounded trace and GPU-clock diagnosis — October 7

**C1 remains OPEN. Neither Decks stability nor Tallow development GPU evidence is repaired or accepted.** This work makes complete native profiling practical and adds clocks for investigating the corrupt GPU samples. It does not replace the [two rejected Decks loops or invalid GPU runs](C1-oct7-reconciliation.md). [Machine-readable findings](C1-bounded-trace.json) retain their raw readings and the new diagnostic results.

## Complete native coverage

The earlier full import exhausted memory and the retained tail could not explain warmed movement. `QualityTrace` now rotates development-player binary traces every 120–1,800 replay samples. Each frame contains the replay sample index, Unity frame, step and elapsed time as native frame metadata, plus a named `Coronach.Replay.Sample` marker. The editor imports one segment at a time with bounded history. The offline reader requires exact, contiguous coverage, matching step/time metadata, a real marker and a nonempty raw trace. Missing, duplicate, shifted, nonfinite or tail-only data rejects the report. Boundary and flush frames remain explicit; rotation costs remain in the CSV.

The isolated diagnostic player is built with `BatchTools.BuildTracePlayer` under `Builds/quality/TracePlayer`. The initial player contains candidate-45 locomotion plus diagnostic instrumentation. It leaves the ordinary packages and frozen chapter intact. A four-step pilot produces 12 small raw files and matches all **1,323** replay samples. The unchanged authored `station-decks-ten-minute.json` then produces **21 raw files / 4,827,536,910 bytes**, covering all **37,763** samples over **629.378 seconds**. All segments import without the earlier history truncation. No CSV row is omitted.

The failed release movement intervals do **not** recur in this diagnostic: step 49 peaks at **17.3416 ms**, step 117 at **17.2133 ms**, and no replay frame exceeds 33.3 ms. This does not explain or erase the release failures. Profiling can change scheduling and costs.

The sole >25 ms CSV interval is first options opening: sample **2,104**, step **9**, **35.071149 s**, **25.1538 ms**. Its adjacent native main-thread frames last approximately **16.598 / 16.730 ms**, while the explicit sample-marker interval is **25.156 ms**. A replay interval spans portions of two native frames; matching it to one main-thread counter would repeat the earlier false alignment. The second frame includes `Awaitable.OnUpdate` at 8.589 ms, loading/integration markers around 1 ms, and a 5.985 ms swap-chain wait; the preceding frame's swap-chain wait is 14.411 ms. These inclusive costs overlap. This options interval is not a reproduction of the failed service-bridge or office traversal.

Raw root: `Builds/quality/2026-10-07-bounded-trace/`. Preserve `pilot/`, `decks-stability-trace-02/`, `import-stability.log`, the rejected launch with the incorrect route filename, and every prior import/alignment failure. Launches have focused frames and display-on callbacks before/after; this is endpoint telemetry, not continuous display observation.

Example diagnostic workflow, with no overlapping player or Unity owner:

```powershell
scripts/exec.ps1 -Method Lattice.EditorTools.BatchTools.BuildTracePlayer -Marker BUILD_OK
scripts/quality-replay.ps1 -Route <authored-route> -Output Builds/quality/<new-run> -Build Development -DiagnosticPlayer Builds/quality/TracePlayer/Coronach.exe -ProfileSegmentFrames 1800
scripts/exec.ps1 -Method Lattice.EditorTools.QualityProfileReport.Segments -Marker QUALITY_TRACE_REPORT_OK -ExtraArgs @('-quality-profile-folder','Builds/quality/<new-run>')
python tools/quality_trace.py Builds/quality/<new-run>
```

Unity's [frame metadata API](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/master/Runtime/Profiler/ScriptBindings/Profiler.bindings.cs) and [raw frame data API](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/master/Modules/ProfilerEditor/Public/RawFrameDataView.bindings.cs) provide the correspondence. The report accepts diagnostic coverage only. Native profiling, segmented profiling and GPU-clock instrumentation remain excluded from clean timing/headroom, including when the runtime flag disagrees with launch arguments.

## GPU timestamp hypothesis

All three original impossible GPU durations decode as Unix milliseconds during their respective runs:

| Rejected run | Replay sample / elapsed | Raw API GPU ms | Interpreted UTC timestamp |
|---|---:|---:|---|
| Decks release headroom | 31,363 / 56.723196 s | 1,791,401,928,741.87 | 2026-10-07 19:38:48.741870 |
| Tallow development headroom | 22,166 / 28.484444 s | 1,791,402,511,958.00 | 2026-10-07 19:48:31.958000 |
| Tallow development repeat | 86,018 / 111.442621 s | 1,791,402,763,112.81 | 2026-10-07 19:52:43.112810 |

**Inference:** an absolute timestamp may be escaping through a duration field in the shared Unity/D3D12 timing path. The native backend code path is not established, and no matching vendor defect was verified. The corresponding profiler values on the next CSV rows are shared-backend corroboration, not independent GPU measurements. Never subtract an inferred epoch, filter these rows or substitute another counter to turn them into passes.

`-GpuClocks` now records UTC ticks, process QPC, FrameTimingManager present/completion clocks and clock frequencies beside each headroom row. This has diagnostic overhead and cannot accept clean headroom. A single changed-instrumentation Tallow run records **92,331 timing rows and 92,331 clock rows** over **120.018 seconds**, with no impossible duration reproduced. Its p95/p99/worst are **1.5564 / 1.7146 / 10.0581 ms**, reported only as diagnostic observations. Tallow development GPU acceptance stays **UNVERIFIED**. No unchanged retry follows this result.

The run reports Direct3D12, QPC and CPU frequencies of 10 MHz, GPU frequency 1 GHz. See Unity's [FrameTiming fields](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/frametiming) for clock semantics. The added clocks are never used to correct raw GPU durations. The initial analysis with an imprecise “profiling” exclusion label is preserved separately; the current analyzer names GPU-clock overhead explicitly.

## Validation and remaining work

The two new launch-exclusion controls fail against `e20f036`'s analyzer, which falsely accepts segmented profiling and clock diagnostics. Twelve trace-reader controls include deliberately broken coverage, identity and metadata; the nonfinite-time control first failed against the new reader and is now repaired. **143/143 offline checks pass**, zero skipped. Existing assertions and raw results are retained. The 7 affected replay PlayMode checks also passed before the clock extension; the actual Tallow run exercises the clock extension.

The native desktop pipe failed before and after reset; no continuous audiovisual or physical review was obtained. Failed node-shell launch attempts started no player and are retained. Nathan's two current saves and all 243 frozen chapter files matched their fresh before/after inventories throughout these diagnostics. No paid source was staged and no system setting changed.

Next C1 work needs a trace of an actual recurring warmed movement failure, or a specifically changed, lower-overhead diagnostic that separates CPU work from presentation waits. The current complete trace supplies that capability but not a root cause. Preserve the timestamp hypothesis until a corrupt event is captured with its clocks. Do not rerun unchanged routes to obtain a green result, repeat the successful short awake comparison, or call physical presentation validated.
