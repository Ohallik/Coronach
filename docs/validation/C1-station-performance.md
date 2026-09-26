# C1 station performance — implementation in progress

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
