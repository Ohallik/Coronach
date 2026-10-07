# Presentation with displays awake — October 7

**Subsequent requirement reconciliation:** [the later October 7 report](C1-oct7-reconciliation.md) preserves these short-run passes and adds two rejected Decks stability loops plus invalid Tallow development GPU evidence. C1/C2 remain OPEN.

**The same candidate-45 packages pass the clean timing comparisons while Windows reports both displays on. Presentation remains unvalidated.** No runtime, startup policy, assertion, system setting or frozen chapter changed. Continuous movement/audio, physical tearing and controller feel remain UNVERIFIED. The prior display-off failures are retained in [the terrain report](C2-terrain-followup.md).

Evidence: `Builds/quality/2026-10-07-awake-comparison/`. [Machine-readable results](C1-C2-awake-results.json) retain package verification, raw artifact hashes, display snapshots, all measured outcomes and save/package checks. Current runtime is `e93bf13`; the replay HEAD is documentation-only `d6c6910`. Both current packages match the earlier candidate-45 executable, assembly and content hashes. The earlier working-source manifests remain unchanged; their binding to committed runtime source is preserved.

## Same packages, different observed display state

Read-only session and console power callbacks report value 1 before and after each new run. The prior failure condition reported value 0. These are snapshots, not continuous physical observation. No awake lease, synthetic input, power-plan, monitor, driver or security change was made. All replays ran exclusively, with isolated saves and zero focus losses. No editor/player/encoder competitor contaminated the clean runs.

The ordinary `station-decks-first-warm.json` route used native 1920×1080, 60 Hz, VSync 1 and cap -1. Neither new clean run used capture, profiling or motion instrumentation.

| Run | Focused samples / seconds | p95 / p99 ms | Worst ms | Frames >100 ms | Result |
|---|---:|---:|---:|---:|---|
| Prior release, display-off condition | 1,644 / 259.567 | 265.582 / 267.494 | 279.445 | 987 | Rejected; missed navigation/shop inputs |
| Prior development, display-off condition | 1,666 / 260.097 | 265.627 / 273.995 | 278.191 | 987 | Rejected; missed navigation/shop inputs |
| Release, displays on before/after | 15,054 / 250.887 | 17.061 / 17.215 | 24.732 | 0 | Timing and checkpoints pass |
| Development, displays on before/after | 15,056 / 250.918 | 17.096 / 17.249 | 25.053 | 0 | Timing and checkpoints pass |

Neither new run has a frame over 33.3 ms. Maximum recorded UI response is 22.814 ms in release and 23.800 ms in development. The same-build comparison associates the severe stall condition with display state; it does not establish every historical stall's cause or physical scan-out quality. Retain all earlier backend/thread/queue/window-mode/re-arm/DwmFlush failures. No further rendering workaround is justified by these passes alone.

## Combat recapture and recording overhead

The previously rejected synchronized combat route now passes the unchanged motion analyzer. Its first new capture, `shaped60/`, nevertheless contains 25 frames over 50 ms, including 15 over 100 ms. **All 25 occur immediately after a route step that wrote a live PNG screenshot.** Preserve this recording and all its frames; its motion pass is not a performance pass.

Two bounded comparisons use the same development package, ordinary synchronization, identical gameplay steps and full motion sampling:

| Run | Samples / seconds | Contacts | Worst drift | Minimum sole clearance | p95 / worst ms | Frames >25 ms |
|---|---:|---:|---:|---:|---:|---:|
| `shaped60`: video/audio plus live PNGs | 13,849 / 232.787 | 176 | 15.134 mm | 5.382 mm | 17.222 / 113.468 | 25 |
| `shaped60-uncaptured`: recording off | 13,956 / 232.583 | 167 | 21.654 mm | 5.683 mm | 17.083 / 19.497 | 0 |
| `shaped60-video-only`: video/audio, live PNGs deferred | 13,965 / 232.730 | 166 | 23.534 mm | 4.752 mm | 17.238 / 19.813 | 0 |

All three pass motion. The uncaptured run also passes the unchanged performance analyzer. The final diagnostic changes **only** `screenshotInterval` from 8 to 10,000 in an ignored route copy; it changes no gameplay, thresholds, sampling or synchronization. Native-resolution video and actual listener audio remain recorded at 30 fps, with full-rate motion/frames and the recorder's wall-clock stall retention. Its exact route diff and hashes are in the machine summary. These controls support live PNG capture overhead as the cause of the new capture-only spikes under these conditions. They do not turn a recorded run into clean C1 evidence, and the original route/capture remains intact.

## Observation and preservation

Actually opened: `shaped60/078.png` and `shaped60/166.png`, selected poses for Taren and Sela. They do not establish motion quality. The existing steep/pad frame-sequence observations remain in the terrain report; perceived uphill balance still needs continuous review. No new continuous video/audio, physical display or controller observation has been claimed.

`Builds/quality/2026-10-06-terrain-followup/Review terrain candidate.html` now includes 13 recordings. The new combat video with deferred live PNGs appears first. The current steep shoulder and pad recordings and rejected comparisons remain selectable, with normal/half/quarter-speed controls and timestamped notes. A human review request is pending.

Nathan's current two LocalLow save files were hashed before these runs and match afterward. The frozen `a3aaad6` chapter again matches **243 files / 616,521,326 bytes**, with no changed, extra or missing files; its profile was not touched. Runtime, tests and assertions are unchanged, so the already completed **131/131 EditMode, 231/231 PlayMode, 119/119 offline checks and 384/384 matrices** were not needlessly repeated for this evidence update. No replacement chapter was packaged or published as validated.

## Next work

1. Review both heroes and ground forms continuously at normal/slow speed, particularly knee continuity, pad entry/exit and uphill balance. Investigate observed defects before C2 acceptance. Review the new combat recording without mistaking recording overhead for gameplay stutter.
2. Observe ordinary synchronized movement on the physical display and review controller response. Reconcile C1's full station, first/warm, headroom and stability evidence with the final runtime; a successful Decks comparison alone does not close the complete gate. Record display state around future runs; do not repeat completed comparisons without a new change or concern.
3. Continue the remaining C1–C10 work in [the production plan](../PRODUCTION_PLAN.md), preserving its map/story, save, asset and evidence rules. The complete game and C7/C8 acceptance remain unfinished. Use [the full continuation prompt](../NEXT_SESSION_PROMPT.md).
