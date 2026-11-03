# New Game → Tallow chain on the dressed maps (`f019bcf`)

2026-11-03. **Agent-directed continuous virtual-gamepad replay, not human play.** Progression **PASSED** on the release player built from `f019bcf` (room/outpost dressing, D191). C7 acceptance, continuous audiovisual review, physical-controller feel and clean C1 timing remain **OPEN/UNVERIFIED**.

Conditions match the earlier November 3 chain ([C7-nov03-chain](C7-nov03-chain.md)): release player, 1920×1080 windowed, each segment alone as one blocking call, resuming from the previous segment's earned saves after `quality_analyze.py` validated it. Displays were asleep (last OS input ~58 min earlier, 20-minute display timeout), so every segment used explicit `-DiagnosticVSync 0`: **progression evidence only**. Raw folders: `Builds/quality/C7/nov03b/v1-*`.

| Segment | Route | Seconds | Unfocused | p95 ms | Result |
|---|---|---:|---:|---:|---|
| v1-01 | chapter-01-new-game-decks | 103.0 | 0 | 16.97 | OK |
| v1-02 | chapter-02-continue-sorrel | 132.6 | 0 | 16.97 | OK |
| v1-03 | chapter-03-defeat-retry | 59.3 | 0 | 16.99 | OK |
| v1-04 | chapter-04-haul-preparation | 231.2 | 0 | 16.99 | OK |
| v1-05 | chapter-05-drill-equipment | 111.7 | 0 | 16.97 | OK |
| v1-06 | chapter-06-burrower | 183.7 | 0 | 17.00 | OK |
| v1-07 | chapter-07-enter-hushwell | 163.2 | 0 | 16.97 | OK |
| v1-08 | chapter-08-hushwell-nursery | 446.4 | 0 | 17.00 | OK |
| v1-09 | chapter-09-calibrated-gullet | 43.9 | 0 | 16.96 | OK |
| v1-10 | chapter-10-gullet-evasive | 787.6 | 0 | 16.97 | OK |
| v1-11 | chapter-11-tallow-reading-return | 233.2 | 0 | 16.94 | OK |
| **Total** | | **2,495.7 (41:36)** | **0** | | |

Every process exited 0 with `QUALITY_REPLAY_OK`; every analysis is valid. The final save is at TallowDrift with `hushwell.nursery`, `sliceComplete`, `tallow.lineObserved` and `tallow.driftDiscussed`; both heroes are level 7 at 228 integrity. Each segment's worst frame is a scene load (New Game 226 ms, Gullet arrival 282 ms, Tallow approach 227 ms). Against the earlier chain under the same capped conditions, the dressed segments (Decks, Sorrel, Tallow) show unchanged median/p95/p99 and equal or fewer >25 ms intervals; this is not C1 timing evidence.

Not established: human play, video, audio, controller feel, clean awake-display timing, combat balance.
