# C1 station performance — implementation in progress

**Latest status, October 7:** [current-runtime reconciliation](C1-oct7-reconciliation.md) records new Tallow first/warm/stability passes, two rejected Decks stability loops, invalid Tallow development GPU evidence, and the native-profiler false-acceptance repair. C1 remains OPEN; the historical measurements below are preserved, not substituted for current acceptance.

2026-09-26. Implementation **OPEN**, automated gate **FAILED** at baseline, continuous observation **UNVERIFIED**, physical controller feel **UNVERIFIED**. Native 1920 × 1080, Ultra, 60 fps cap, VSync 0, reported display 60 Hz; Ryzen 7 9800X3D / RTX 5070. VRR and GPU work timing are unavailable/unverified. No capture, encoder, editor or build ran during the four clean baseline players.

| Clean baseline under `Builds/quality/C1/` | p95 / p99 (ms) | Worst (ms) | >33.3 / >50 ms | Camera stopped during walking |
|---|---:|---:|---:|---:|
| `before-development-decks-1` | 16.981 / 17.118 | 63.883 | 1 / 1 | 939 / 2,956 |
| `before-development-tallow-1` | 16.954 / 17.099 | 62.443 | 1 / 1 | 702 / 2,589 |
| `before-release-decks-1` | 16.920 / 17.080 | 66.309 | 1 / 1 | 939 / 2,958 |
| `before-release-tallow-1` | 16.912 / 17.035 | 59.985 | 1 / 1 | 702 / 2,588 |

All four complete their ordinary-input route and UI checkpoints. All four fail the existing hitch budget, without weakening a target. The largest frame lands on first dialogue opening: Mira at about 22.1 seconds and Keeper at about 72.1 seconds. Decks additionally shows 28–33 ms frames at first options/shop. Per-frame CSV, checkpoints, build assembly hashes, settings and plots are retained with each run. Development allocation median is 4,204 bytes/frame, 9 collections/120 seconds; release allocation counter is unsupported, with 8 collections. Those figures do not by themselves establish HUD work as the cause.

The camera is independently reproduced with a constant-speed 2.6 m/s target: `C0/preflight/station-motion-red.xml` fails at 0.3303 stopped-frame fraction versus the 0.05 limit. The correction moves toward the nearest edge of the dead zone instead of repeatedly stepping across its threshold. Velocity filtering also uses elapsed time and resets on target initialization. The unchanged regression passes in `C1/camera-green.xml` (1 test, zero skips). Real traversal after correction remains required.

CPU/UI diagnosis is separate from clean timing. Opt-in `-quality-profile` records a bounded 45-second Unity binary trace and HUD elapsed/allocation counters. The offline analyzer rejects profiled or captured runs for clean performance acceptance. Inclusive profiler samples overlap and retain waits; they cannot be summed as active CPU/GPU work. The profiler file is closed by disabling capture and clearing `Profiler.logFile`, following the [Unity profiler API](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/profiling/profiler/enabled).

Seventeen analyzer controls currently pass, including duplicate/missing measurement windows and retained first-use hitches. First-visit/warmed fixtures now keep consecutive measured laps in one player process, and ten-minute fixtures remain moving through town services. Each lap is adjudicated separately; earlier hitches are not averaged away. These fixtures have not yet established final acceptance.

The `dialogue-profile-before` diagnostic reproduces first dialogue at 43.478 ms: PlayerBrain takes 38.1 ms inclusive, `Dialogue.StartNode` 26.759 ms, SetProject 8.910 ms, LoadProject 1.431 ms, with Mono JIT samples. These nested timings must not be added. HUD averages about 0.106 ms, peaks at 2.45 ms; Canvas peaks at 0.78 ms and collection at 2.225 ms. This supports preparing Yarn's VM and presenter paths during the scene fade; it does not support replacing the HUD. The HUD byte probe returned zero on this Mono runtime and is now calibrated to report unsupported rather than claiming zero allocation.

Each new dialogue runner now loads/sets its project once, executes an inert line/options node during the scene preparation hold, clears only that fresh runner's preparation variables, and releases player control afterward. Story flags, rewards, visible lines and speaker state stay untouched. Forced Yarn import is part of building so a newly added preparation node cannot silently be absent from compiled dialogue. `dialogue-preparation-green.xml` passes the actual load/interaction/unload regression (1 test); earlier missing-node and teardown failures remain preserved. The teardown failure also led to ignoring conversation completion during scene destruction.

Still required: rebuild the paired station, repeat first/warm release and development runs, complete the ten-minute stability loop, verify interaction latency and reopen final traversal evidence. Normal/slow-motion playback remains unavailable through the native pipe, so that observation cannot be called passed.

The recorder now timestamps queued input to the first visible dialogue/shop state. This is a CPU-side response measurement, not a monitor-presentation latency measurement. The successful Decks blockout route reports Mira at 4.169 ms, shop at 9.889 ms and dismissal at 1.220 ms. Heavy recording remains excluded from frame-pacing acceptance. The available-counter catalog confirms that this Unity player exposes scene/object counts but no `Audio Voices` counter; unsupported voices remain -1. Scene/object counters were added for the forthcoming stability runs.

Clean timing launch now inventories other Unity/build/encoder processes, refuses a contaminated start and checks for interference during traversal. Another task is actively running Frostbound; it is left untouched. Captures and independent implementation can continue, but overlapping clean timing cannot pass.

The generated station rebuild now passes both development first/warm comparisons. Decks: first p95/p99/worst 16.984/17.122/25.206 ms; warm 16.964/17.102/19.178 ms. Tallow: first 16.956/17.082/19.883 ms; warm 16.955/17.066/18.883 ms. Neither town has a frame above 33.3 ms. First CPU-side dialogue responses are 4.402 ms (Mira) and 3.528 ms (Keeper); first shop response is 7.444 ms. Runs are under `station-after-01-development-{decks,tallow}-1`. Release and stability comparison are still in progress.

The available-counter inventory also exposes `CPU Main Thread Frame Time`, `CPU Render Thread Frame Time` and `GPU Frame Time`. The earlier recorder did not enable these work counters; its UNAVAILABLE GPU field must not be read as proof that the hardware cannot provide timing. An opt-in uncapped headroom path is being added and remains unverified. Unity documents these separately from the wait-inclusive Main Thread marker: [counter definitions](https://docs.unity.com/en-us/engine/6000.7/manual/analysis/graphics-performance-profiling/profile-rendering/frame-timing-manager/counter-reference) and [selective recording](https://docs.unity.com/en-us/engine/6000.7/manual/analysis/graphics-performance-profiling/profile-rendering/frame-timing-manager/record-timing-data). Missing/zero work samples remain unsupported, and a missing active CPU measure cannot establish a headroom pass.

All four first/warm comparisons now pass in `station-after-01-{development,release}-{decks,tallow}-1` (eight adjudicated laps). Release Decks first p95/p99/worst is 16.946/17.053/25.342 ms; warm 16.939/17.048/18.531 ms. Release Tallow first is 16.911/17.017/19.568 ms; warm 16.918/17.031/20.300 ms. Across all eight laps, no frame exceeds 33.3 ms. The original 60-66 ms first-dialogue hitch is absent. Twenty-two offline analyzer controls pass, including missing/slow work-counter rejection. Ten-minute stability, active CPU/GPU headroom and observed continuous smoothness remain open.


## Work-counter validity and uncapped results

`headroom-01-development-tallow` completes 120.018 seconds / 105,704 samples uncapped, VSync off, without capture/profiling. Active CPU p95 is 0.9523 ms, active render-thread p95 1.1810 ms and GPU p95 1.243392 ms; maximum GPU duration is 4.085759 ms. This run passes headroom validation.

The corresponding Decks run completes 124.537 seconds / 77,779 samples. Active CPU p95 is 0.9925 ms and active render-thread p95 1.9073 ms. **Its GPU evidence is rejected**: two values at 31.671624 and 73.594834 seconds resemble absolute timestamps, reaching 1,790,456,079,119.5764 ms. A completed frame cannot exceed the whole recorded visit. The original analyzer incorrectly accepted the percentile; `analysis-counter-validated.json` now rejects it, preserving all raw samples and the original analysis. No trimming or relabeling of those values as fast frames is allowed. The underlying Unity/platform cause has not been established. Decks GPU headroom remains UNVERIFIED.

Twenty-four analyzer controls pass, including rejection of a single impossible timestamp even when p95 is fast, and retention of a genuine 250 ms GPU duration. The rejection bound is the entire measured visit plus recorded load/settle; it is not a percentile filter or a relaxed hitch budget. Zero/pending/unsupported sample counts are disclosed. The ten-minute release stability runs are in progress; continuous visual observation remains unavailable.


The initial release stability run `station-stability-01-release-decks-1` completes all five separately adjudicated laps in 628.922 s / 37,734 samples. p95/p99/worst: 16.9305/17.0363/25.3704 ms; >33.3/>50 ms: 0/0. Minute memory medians are 138.56, 138.69, 138.91, 138.78, 139.02, 139.06, 139.43, 138.99, 139.10 and 138.97 MB, settling after initial caches. Every interaction is below 7 ms CPU-side. This does not establish object/voice stability: the release strips those native counters despite their development availability. An opt-in five-second object/audio-source census is being added for explicit growth diagnostics, retaining its sampling cost. Playing AudioSources are not simultaneous one-shot voice counts and will not be labeled as voices.


`station-stability-01-release-tallow-1` completed 600.421 s / 36,024 samples but is **REJECTED**: one sample at 17.802033 s lost focus. Raw p95/p99/worst are 16.8961/16.9954/20.8729 ms, with no interval above 25 ms; those figures do not override the invalid run. All raw evidence remains. The queued census rerun must retain the focus requirement. The analyzer now has 29 passing controls, including absent/incomplete/invalid object-source census rejection and visible sustained-growth reporting.


## Final-layout retry after 48bdff9

The September 26 final-layout retry starts without competitors, but another Frostbound batch begins during Decks headroom. The unchanged harness rejects concurrent Unity/build work and focus loss: only 52,064/70,041 samples retain focus, and twelve navigation/dialogue/shop checks fail in 124.530706 seconds. The seven remaining headroom/first-warm/census starts refuse the contaminated environment. No clean performance conclusion is drawn from this run; no unrelated process was stopped. [Exact rejected evidence](C1-final-timing-rejected.json) retains environment/process records and hashes. C1 remains OPEN; current integrated packages contain the 85/39-tested buff checkpoint. The offline analyzer controls still pass 29/29.

## Clean integrated runs (September 30, Claude Code)

With the other session's Frostbound work finished and no competing process, every run started clean on players rebuilt from `0e5e9c2`, and each ran alone.

| Run | Result |
|---|---|
| First/warm, `c1-clean-20260930-{development,release}-{decks,tallow}-1` | All four **valid**. p95 16.95–17.02 ms, p99 17.05–17.13 ms. Worst: Decks 25.5/26.1 ms, Tallow 19.6/19.7 ms. No interval above 33.3 ms |
| Headroom, `headroom-03-development-tallow` (uncapped, VSync off, no capture) | **Valid**: 120.0 s / 103,531 samples. p95 active CPU 1.00 ms, render thread 1.20 ms, GPU 1.25 ms |
| Headroom, `headroom-03-development-decks` | CPU **valid**: 124.5 s / 78,493 samples, p95 active CPU 1.03 ms, render thread 1.89 ms. GPU **rejected**: exactly two impossible "GPU Frame Time" samples (≈1.79 × 10¹² ms, timestamp-like), both at the moments the route opens a dialogue (steps 18 and 23). The remaining GPU p95 is 1.87 ms, but the counter evidence stays rejected as the analyzer requires |
| `headroom-02-*` | Rejected by the analyzer: run with the capped first/warm routes by mistake. The uncapped headroom routes were then used for `headroom-03` |
| Stability, `station-stability-probe` (Decks, release, census) | **Valid**: 628.8 s / 37,724 samples, all focused. p95/p99/worst 16.97/17.08/25.9 ms; none above 33.3 ms. Memory medians about 150 MB, flat. Census: about 2,960 scene objects start to end (max 2,976), 4–7 audio sources, 2–3 looping. No growth |
| Stability, `station-stability-03-release-tallow` (release, census) | **Valid**: 600.4 s / 36,024 samples, all focused. p95/p99/worst 16.95/17.06/19.75 ms; none above 25 ms. Memory 140.5–140.9 MB. Census: 563→564 objects, 6–7 audio sources, 2 looping. No growth |

The first `quality-timing -RouteSet Stability` attempt refused to start both runs, probably because the last headroom player had not yet exited. The stability routes were then run directly, one at a time. Nothing was trimmed or re-labelled.

**C1 status:** every timing, hitch, stability and census criterion passes on both stations in both builds, and Tallow's headroom passes in full. Two items remain:

- **Decks GPU headroom** is UNVERIFIED. Unity's GPU Frame Time counter reproducibly returns two timestamp-like values when that route opens a dialogue. An independent GPU source would be needed to verify it; its CPU headroom passes with about 13× margin.
- **Continuous viewed traversal** by a person remains unavailable.


## FrameTimingManager cross-check (October 3, Codex)

**Decks GPU headroom PASS** under D126/D129, in both rebuilt players. Each ordinary
virtual-gamepad run was isolated and blocking: 1920 x 1080, Ultra, uncapped, VSync
off, no capture/profile/census, no competing process, every recorded frame focused.
Frame Timing Stats is enabled in Player Settings; the recorder retains direct API
durations (milliseconds), original CPU frame-start ticks, timer frequency and the
profiler GPU counter (nanoseconds). Unity's API returns asynchronous completed
frames, so rows are not attributed to the current input frame. The API and counter
share Unity's timing backend; agreement is not a second hardware-clock validation.
[Unity API](https://docs.unity.com/en-us/engine/6000.7/script-reference/unityengine/frametimingmanager),
[feature enablement](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/frametimingmanager/isfeatureenabled).

| Under `Builds/quality/workshop/` | Release `decks-ftm-release-01` | Development `decks-ftm-development-01` |
|---|---:|---:|
| Seconds / focused frames | 124.524 / 79,376 | 124.527 / 76,655 |
| Direct API distinct positive GPU samples | 71,801 (90.46%) | 72,397 (94.45%) |
| API pending/unavailable rows | 7,575 | 4,258 |
| GPU p95, API and profiler | **1.855232 ms** | **1.934848 ms** |
| GPU p99 / worst, API | 1.984512 / 3.494912 ms | 2.053376 / 2.908160 ms |
| Active CPU / render p95 | 0.8086 / 1.8809 ms | 1.0607 / 1.9454 ms |
| Whole-frame p95 / p99 / worst | 1.9562 / 2.1261 / 10.8305 ms | 2.0224 / 2.1921 / 9.8947 ms |
| Analyzer | valid, AGREE | valid, AGREE |

Neither API returns impossible, nonfinite or repeated-frame readings in these
runs. The profiler has one fewer positive row per run because the access paths
are sampled asynchronously. No positive sample is removed for being slow.
The earlier timestamp-like anomalies were not reproduced with this configuration;
this is not proof of their underlying cause. Eight new analyzer negative cases
(disabled, missing, zero, cached, impossible, nonfinite, disagreement and slow API)
all failed against the old analyzer before implementation. The corrected analyzer
passes 37 controls including existing traversal/headroom cases. Raw red/green
output: `Builds/quality/session-2026-10-03/frame-timing-{red,green}.txt`.

The complete player, assembly and evidence hashes are indexed in
[C1-frametiming-evidence.json](C1-frametiming-evidence.json). Nathan's original save
SHA-256 values match the pre-session manifest after both runs. Prior capped and
stability evidence is historical and retained; continuous visual/audio observation
is still UNVERIFIED, and final material/layout changes require new performance
evidence before accepting the integrated workshop.

## Optional landscape pipeline (October 3, Codex)

The final private renderer and water/grass settings were measured again in both
builds. These are isolated ordinary-input, uncapped runs at 1920 x 1080 with no
capture, profile or census. All samples retain focus and all environments have
no competing Unity/player/encoder work. API and profiler agree at p95 in all four.

| Under `Builds/quality/workshop/biomes/` | Frames | GPU p95 ms | Frame p95 / p99 / worst ms |
|---|---:|---:|---:|
| `decks-headroom-release-01` | 73,678 | 1.896960 | 2.0992 / 2.2758 / 10.6904 |
| `decks-headroom-development-01` | 70,659 | 1.991424 | 2.1893 / 2.3785 / 10.1804 |
| `tallow-headroom-release-01` | 97,248 | 1.273344 | 1.4771 / 1.6213 / 9.2335 |
| `tallow-headroom-development-01` | 92,830 | 1.330432 | 1.5372 / 1.6797 / 9.4345 |

Both the unchanged 14 ms GPU budget and distinct-positive API coverage pass;
raw pending samples remain in the recordings. These measurements cover the
global renderer and Tallow water. They do not substitute for dense Sorrel combat
performance, capped first/warm routes, long stability or continuous observation.

The subsequent clean release Sorrel service-combat run,
`biomes/sorrel-headroom-release-01`, covers the densest grass placement: 259.946 s,
221,548 focused frames, active CPU p95 0.9178 ms, render 1.2474 ms, and GPU
**1.247744 ms** from both timing sources. API coverage is 96.18%; its 8,464 pending
rows are retained. No impossible values or repeated API frames. Whole-frame
p95/p99/worst is 1.4232/1.5637/15.7015 ms, and all route outcomes pass. Exact hashes
are in [the landscape evidence index](C8-landscape-evidence.json).
