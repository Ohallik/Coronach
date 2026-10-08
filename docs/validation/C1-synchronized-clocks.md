# C1 synchronized clock diagnostic — October 7

**C1 remains OPEN. No presentation or GPU repair is established.** This extends the [bounded trace](C1-bounded-trace.md) without repeating its unchanged ten-minute run. [Exact rows, statistics, source and artifact hashes](C1-synchronized-clocks.json).

The earlier sidecar's `qpc` column is actually `Stopwatch.GetTimestamp()`. In this Mono player its epoch is process-relative: the first Tallow value is 125,566,534 while Unity's CPU frame timestamp is 19,445,735,117,900. Both report 10 MHz; equal frequency does not establish an equal epoch. Preserve the old label and raw values, explicitly document the mismatch, and never invent an alignment offset.

The optional `-quality-frame-clocks` / `-FrameClocks` mode now records native Windows `QueryPerformanceCounter`, its frequency, Unity frame-start/present/completion timestamps, UTC, main/render work and main-thread present wait. Unlike headroom mode, it preserves ordinary startup VSync and route interaction. Native calls are Windows-guarded; unavailable counters are -1. This is a shared Unity timing diagnostic, not an independent GPU clock or clean performance evidence. The analyzer rejects it, and also rejects missing launch manifests for clean timing/headroom. All three new controls fail on the prior analyzer; **146 offline checks** and **7 affected replay PlayMode checks** pass.

## Changed probe and finding

`decks-display-off-clocks59` uses only the first four authored Decks steps, development candidate 59, VSync 1, no software cap, no capture/native profiler and no headroom override. Windows session/console callbacks report display state 0 before and after. It completes the checkpoints with focus, but its timing is **REJECTED**: 144 samples / 22.389 seconds, median 261.059 ms, p95 265.187 ms, p99 265.849 ms and worst 266.601 ms. All 85 frames over 25/33.3 ms remain. Its short duration, missing expected sample density, timing limits and instrumentation also disqualify clean acceptance.

| Shared timing field | Median | Maximum |
|---|---:|---:|
| Main-thread frame work | 2.221 ms | 9.598 ms |
| Render-thread frame work | 2.824 ms | 10.424 ms |
| Main-thread present wait | 259.184 ms | 264.334 ms |

This supports the current display-off stall being dominated by waiting rather than heavy game work. It does **not** explain the original awake 34/42/62 ms Decks failures or identify the underlying driver/compositor cause. No system setting or production pacing policy changed.

Native QPC and Unity CPU stamps now occupy the same apparent range at 10 MHz. The API's latest completed frame is still asynchronous: median age relative to the current native read is **532.932 ms**, approximately two stalled frames here. Row-index alignment remains invalid. Unity documents the timing fields and asynchronous retrieval in its [FrameTiming API](https://docs.unity3d.com/cn/6000.0/ScriptReference/FrameTiming.html); [present wait](https://docs.unity3d.com/cn/6000.0/ScriptReference/FrameTiming-cpuMainThreadPresentWaitTime.html) can include presentation/target-rate waiting. Do not sum inclusive costs or replace the original invalid GPU durations with clock-derived values.

The three Unix-timestamp-shaped GPU failures remain intact and invalid, including both Tallow development attempts. The next useful investigation needs a reproduced bad duration or awake stability hitch with attributable, sufficiently low-overhead evidence. Do not rerun unchanged loops until green. Physical scan-out, continuous audiovisual review and controller feel remain **UNVERIFIED**. Saves and all 243 frozen chapter files retain their hashes.
