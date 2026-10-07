# C1 evidence reconciliation — October 7

**C1 and C2 remain OPEN. The presentation candidate is NOT validated.** Two current Decks stability runs fail their per-lap hitch limits. Tallow's current development GPU evidence is invalid. Continuous motion/audio, uphill balance, physical scan-out and controller feel remain UNVERIFIED. No C3–C10 milestone was accepted or new chapter published during this work.

Runtime remains `e93bf138cddccc9fb6d992a819040a646ce9f7ea` (candidate 45); replay source HEAD was `f930af9`. The 106 release/development package files checked in the [earlier awake comparison](C1-C2-awake-comparison.md) still match, and the three repaired runtime sources match their committed hashes after line-ending normalization. Historical build HEAD values remain unchanged. [Machine-readable results and artifact hashes](C1-oct7-reconciliation.json) bind all eleven new runs, the diagnostic trace, rejected controls and preservation checks.

## Coverage against the production gate

| Requirement | Current evidence | Status |
|---|---|---|
| Decks release/development first and warm visits | Earlier October 7 awake comparison, same packages; p95 17.061 / 17.096 ms, no frame >33.3 ms | Measured pass retained; not repeated |
| Tallow release/development first and warm visits | New uninterrupted two-lap runs below | Measured pass |
| Uncapped CPU/GPU headroom | Decks release repeat/development and Tallow release pass; initial Decks release and both Tallow development attempts contain impossible GPU durations | Tallow development GPU UNVERIFIED; retain all failures |
| Ten-minute release traversal | Tallow passes; Decks fails twice on different warmed movement steps | Decks REJECTED |
| Memory, object and source stability | Both towns' minute windows and five-second census retained, including rejected timing runs | No sustained object/source growth observed; audio voices unsupported |
| Accepted-input response | Maximum 23.324 ms CPU-side across these runs; every interaction checkpoint completes | Measured pass; physical latency/feel UNVERIFIED |
| Continuous movement, mix and physical presentation | Native desktop pipe unavailable before and after helper reset; no continuous review obtained | UNVERIFIED |

All new players run alone, with ordinary virtual gamepad input, no competing Unity/build/encoder process, and every recorded frame focused. Windows session and console display callbacks report **on before and after** every run. These are endpoint observations, not a continuous display-state log. No awake request, power-plan change, rendering workaround or system-setting change was made.

Hardware/settings: Ryzen 7 9800X3D, RTX 5070, Unity 6000.4.7f1, Ultra, 1920×1080, reported refresh 60 Hz; VRR remains unverified. Ordinary timing retains startup VSync 1 / software cap -1. Headroom deliberately uses VSync 0 / uncapped and cannot accept physical presentation. Load and eight-second settle durations remain in each `run.json`. Census sampling costs remain in the stability timings.

## New ordinary timing

Raw evidence root: `Builds/quality/2026-10-07-c1-reconciliation/`. Nothing is trimmed, including short frames after stalls and first-use UI work. Each first/warm or stability lap is adjudicated independently.

| Run | Seconds / frames | p95 / p99 / worst (ms) | >33.3 / >50 ms | Result |
|---|---:|---:|---:|---|
| `tallow-release-first-warm-02` | 240.404 / 14,425 | 17.033 / 17.162 / 19.576 | 0 / 0 | PASS, both laps |
| `tallow-development-first-warm` | 240.368 / 14,423 | 17.040 / 17.197 / 20.453 | 0 / 0 | PASS, both laps |
| `decks-release-stability` | 629.431 / 37,764 | 17.045 / 17.185 / 42.504 | 2 / 0 | REJECTED, warm lap 1 |
| `tallow-release-stability` | 600.963 / 36,058 | 17.027 / 17.153 / 19.577 | 0 / 0 | PASS, all five laps |
| `decks-release-stability-repeat` | 629.531 / 37,770 | 17.037 / 17.183 / 62.284 | 1 / 1 | REJECTED, warm lap 3 |

The first Decks rejection has **34.1971 and 42.5038 ms** frames at **192.220123 and 192.278790 seconds**, step 49, `service bridge to provisions`. Collection count stays 34 across those frames. Main-thread counters are 34.4301 / 42.8166 ms but include waits; active-work/cap-wait counters are unsupported in this ordinary run. Nearest censuses are 190.219263 / 195.219392 seconds, costing 0.3128 / 0.3365 ms. Neither collection nor simultaneous census work explains the observed frames.

One unchanged repeat checks recurrence rather than retrying until green. It has **62.2836 ms** at **474.005613 seconds**, step 117, `office to promenade`. Collection count remains 48; main-thread time is 61.6495 ms, again wait-inclusive. Nearest census samples are 470.576834 / 475.593317 seconds and cost 0.2858 / 0.2846 ms. Focus remains true. The subsequent 2.4716 ms interval is retained. This is another failed movement interval, not an explained or repaired fault.

All other Decks laps pass their unchanged timing limits. First options opening is retained at 25.4165 / 25.8000 ms in the two runs; neither is silently removed as warmup. Passing percentiles and later laps cannot accept either failed loop.

Decks' first ten-minute memory medians range **155.710–156.256 MB**, with scene-object medians 2,959. The repeat ranges **155.637–156.796 MB**, with scene medians 2,958 initially then 2,959; that small late heap increase is reported, not labeled a proven leak. Temporary UI objects reach 2,976 and return. Tallow's medians range **148.310–148.627 MB**, with 579 initial objects settling at 582. Both towns settle at seven audio sources. Two looping sources are usual, with a transient third in Decks. Playing sources are not audible one-shot voice counts; the voice counter remains unsupported. These inventories cannot accept the sound mix.

## Headroom and rejected GPU evidence

All six headroom attempts complete their routes. The table includes the failed attempts' reported percentiles solely to show why a low p95 is insufficient; their GPU evidence remains rejected.

| Run | p95 active CPU / GPU (ms) | Verdict |
|---|---:|---|
| `decks-release-headroom` | 0.881 / 1.901 | REJECTED: one impossible API duration and its profiler counterpart |
| `decks-development-headroom` | 1.108 / 1.969 | PASS |
| `decks-release-headroom-repeat` | 0.874 / 1.895 | PASS; original rejection retained |
| `tallow-release-headroom` | 0.773 / 1.273 | PASS |
| `tallow-development-headroom` | 1.028 / 1.327 | REJECTED: impossible GPU duration |
| `tallow-development-headroom-repeat` | 1.025 / 1.331 | REJECTED: impossible GPU duration |

The three invalid API samples occur at **56.723196**, **28.484444**, and **111.442621** seconds respectively, reporting approximately **1.7914×10¹² ms**. The following profiler samples contain matching invalid values from Unity's shared timing backend. They are not independent hardware clocks or legitimate frame durations. Every raw value, pending count, source comparison and failure remains in the machine-readable evidence. No sample is dropped or substituted, and no further Tallow retries were used to hunt for a pass.

## Diagnostic trace and repaired false acceptance

`Builds/quality/C1/oct7-decks-native-profile/` records the unchanged development first/warm Decks route with native binary profiling and census, separately from clean timing. Native profiling is enabled through Unity's [documented command-line switches](https://docs.unity.com/en-us/engine/6000.5/manual/analysis/profiler/command-line-arguments). It produces a **2,020,953,468-byte** trace and 15,055 focused recorded frames over 250.899 seconds. Warm step 49's recorded maximum is **17.3436 ms**: the first failed release interval does not reproduce in this diagnostic. It does not cover the repeat's later warm lap 3.

The original report imports only frames **13,572–15,571**. That tail does not cover the first failed route interval and cannot explain it. A temporary full-history reader is **REJECTED**: Unity reported memory pressure and discarded early frames, then the reader's coverage assertion failed. The entire log and candidate reader are retained. A bounded 5,000-frame import succeeds with frames **10,572–15,571**, spanning 83.319 seconds between first and last frame starts. Its only >25 ms profiler frame is the final **416.6921 ms** interval containing evidence serialization and application teardown. Do not attribute that shutdown interval to movement. An attempted exact alignment of CSV main-thread counters with profiler frame durations fails its assertion and is also retained; its proposed offset is not accepted.

The bounded trace has maximum collected GC and Canvas markers of 2.0935 / 0.6294 ms, but those measurements describe this diagnostic window and do not explain the failed release loops. Marker times are inclusive and overlap. Process-local import helpers use Unity's [profiler frame-data API](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/master/Modules/ProfilerEditor/Public/FrameDataView.bindings.cs); their editor-source edits were restored exactly. Automatic texture-meta formatting and private package-lock additions were archived and restored, not committed.

The native diagnostic exposes a separate, reproducible validation bug: `run.json.profiled` is false because it records the replay's own profiling switch, while Unity's native profiler is active. The original offline checker **falsely accepts this actual recording as clean performance**. `quality_analyze.py` now also inspects `build.json` launch arguments and explicit diagnostic exclusions. Native/deep/harness profiling cannot pass clean timing or headroom even if the runtime flag disagrees. Raw runtime metadata is never rewritten.

**129/129 offline checks pass**, zero skipped. Ten new CLI contract checks include eight negative controls that fail against the original analyzer and two positive controls that still pass. The actual trace likewise exits 0 under the old checker and 1 under the repair; both analyses are retained. All existing 119 checks and thresholds are preserved. No runtime changed, so prior **131 EditMode / 231 PlayMode / 384 contact-matrix passes** remain the integrated runtime evidence and were not repeated.

## C2 review and preservation

Actually reopened: `slope60-candidate45-final/078.png`, its timestamped sequences for steps 78 and 166, and `pad60-candidate45-final/sequence-step118.png`. The sequences show the two heroes' uphill strides and the pad transition. The uphill pose still raises a balance concern; these selected frames do not establish convincing movement at normal speed. The native desktop pipe remains unavailable after reset, so no continuous motion or audio was watched/heard through it. The human observation request remains pending.

Continue review with `Builds/quality/2026-10-06-terrain-followup/Review terrain candidate.html`: the October 7 combat clip with live PNG capture deferred appears first, followed by the existing slope/pad comparisons. Preserve the failed PNG-overhead capture, display-off runs, original knee/terrain failures and every assertion. Do not replace visual judgment with contact thresholds.

Fresh before/after inventories preserve Nathan's two LocalLow saves byte-for-byte. The frozen chapter still matches **243 files / 616,521,326 bytes**, runtime `a3aaad6`; its profile remains exactly as found (no files present in this session's snapshot). Its launcher is `Builds/Workshop-Chapter/Play Coronach Chapter.cmd`. No package was replaced, and no paid source/private derivative was staged.

Next work remains at C1/C2: obtain continuous normal/slow review; diagnose the retained Decks long-run hitches using a bounded trace that actually covers a reproduced failure; establish valid Tallow development GPU evidence without filtering bad samples; and complete physical presentation/input/audio observations. Do not repeat the successful short awake comparison or full runtime suites without a new affected change or failure. Only accept gates when their measurements and required observations agree, then continue C3–C10 in production order.
