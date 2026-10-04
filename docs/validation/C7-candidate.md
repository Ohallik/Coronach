# Workshop candidate — October 4

Runtime source **06b0686**. **Technically playable candidate; C7 acceptance OPEN.**
The launch instructions and Q01–Q07 report are in [WORKSHOP](../WORKSHOP.md).
No Nathan play, physical-controller feel, continuous footage viewing or sound
audition is claimed. Generated scenery and licensed local surfaces are included;
the public source retains its generated fallback.

The final checks exposed and fixed the ordinary-startup pacing defect in
[D140](C1-startup-pacing.md). This matters more than the previous green timing
results: ordinary 60 fps replays now preserve the actual startup policy.

| Clean first/warm run | Whole-run p95 / p99 | Worst | Frames over 33.3 ms |
| --- | --- | --- | --- |
| Decks release, 250.661 s | 16.958 / 17.079 ms | 26.402 ms | 0 |
| Decks development, 250.656 s | 17.006 / 17.135 ms | 26.090 ms | 0 |
| Tallow release, 240.171 s | 16.937 / 17.046 ms | 20.173 ms | 0 |
| Tallow development, 240.145 s | 16.988 / 17.111 ms | 19.632 ms | 0 |

Every first and warmed segment independently passes the unchanged performance
thresholds. All frames retain focus; no capture, profiler or competing process
is present. Every required dialogue/shop response passes the 100 ms gate.

| Uncapped run | Active CPU p95 | GPU p95, profiler / FrameTimingManager |
| --- | --- | --- |
| Decks release | 0.865 ms | 1.986816 / 1.986816 ms |
| Decks development | 1.074 ms | 2.039807 / 2.039808 ms |
| Tallow release | 0.755 ms | 1.363712 / 1.363712 ms |
| Tallow development repeat | 1.052 ms | 1.420800 / 1.420800 ms |

The first Tallow development headroom run is **rejected**. Both Unity access
paths contain two impossible, epoch-sized durations around 1.81 and 88.57 s.
The unchanged repeat passes coverage, plausibility and agreement checks. Both
runs remain intact; this does not resolve Unity's intermittent shared-backend
fault. D129's distinction between separate access paths and independent hardware
clocks still applies. Earlier ten-minute stability evidence remains conditional
on its documented VSync-off settings, now matching the startup policy.

Both ordinary flight circuits pass clean release timing and navigation:
Taren **393.517 s / 23,610 focused frames**, p95 16.959 ms, worst 22.076 ms;
Sela **393.635 s / 23,617 focused frames**, p95 16.955 ms, worst 21.800 ms.
These cover the mouth, chambers, throat and eddy. The separately recorded
[earned coil/Cantor/exit recapture](C7-gullet-coil.md) covers the late tunnel;
it is not clean timing evidence.

Both development arena smokes pass kills/swap/Flash. The ordered slice smoke
passes all 24 markers in **496.2 s**, reaching level six. It uses the development
driver and is kept separate from the **30:04 ordinary New Game-to-Tallow chain**
in [the workshop route report](C7-workshop-flow.md). All 24 portrait and eight UI
captures pass. Codex opened the dialogue panel, Survivor Neutral and Keeper
Shocked; the distinct portraits and frame layout remain intact. These fixtures
are not new story-writing or continuous-motion evidence.

Full EditMode: **69/69**, zero skipped. Offline validation: **113 checks pass**.
The final full PlayMode run passes **218/218**, zero skipped, in **1,210.979 s**
with `-TimeoutSec 3600`. After the runner restores its shielded originals,
both Nathan save files match their original SHA-256 values exactly.

The frozen copy itself passes the ordinary opening route: **102.854 s / 6,156
focused frames**, New Game through docking, Sela joining, shop, bench, equipment
and manual save. This is captured gameplay with menus and loading, not clean
performance evidence. The preceding stationary Title check and 6.616 m dock-only
check pass runtime scene/UI/focus assertions but are **rejected by the offline
traversal minimum**. That was a mismatch between the shortened route and the
chosen analyzer; both remain intact. The full opening repeat passes both.

The first Taren flight launch was interrupted by a PowerShell path error while
inspecting process executables. It has no final gameplay verdict. The same
`Join-Path` exception reproduces on an extended Windows path (`\\?\C:\...`);
the transient offending process was not captured, so its identity is unknown.
Native IO path handling fixes that class of failure. The contract is red on the
old code and green for ordinary/extended Unity paths, absent libraries, null
paths and bare executable names. The launcher still rejects competing players;
it never stops an unrelated process. Rejections and interrupted artifacts remain
in `candidate-06b0686/flight-taren-release` and `process-path-{red,green}`.

The player copy contains 243 files / 614,873,502 bytes. Its SHA-256 manifest is
`Builds/Workshop/player-manifest.json`. The launcher isolates saves and audio
preferences under `Builds/Workshop/Profile`. Legacy Lattice files and Unity's
DoNotShip folders are excluded. The original `57147a1` copy is preserved as
`Player-57147a1-rejected-pacing`; it must not be offered as the final candidate.

Still open: sparse Gullet approach, preview/collar work, remaining first-chapter
presentation and unavailable audiovisual/physical observation. The nursery
connection is [planned](../C8_OPENING_INTEGRATION.md), not implemented in this
candidate. [Artifact/source hashes](C7-candidate-evidence.json).
