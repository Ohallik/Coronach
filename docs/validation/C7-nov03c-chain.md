# New Game → Tallow chain on dressing pass 2 (`5a656ce`)

2026-11-03. **Agent-directed continuous virtual-gamepad replay, not human play.** Progression **PASSED** on the release player built from `5a656ce` (dressing pass 2, D192). C7 acceptance, continuous audiovisual review, physical-controller feel and clean C1 timing remain **OPEN/UNVERIFIED**.

Same conditions as [C7-nov03b-chain](C7-nov03b-chain.md): release player, 1920×1080 windowed, each segment alone as one blocking call, resuming from the previous segment's earned saves after `quality_analyze.py` validated it. Displays were asleep (last OS input ~93 min before the first run, 20-minute display timeout), so every segment used explicit `-DiagnosticVSync 0`: **progression evidence only**. Raw folders: `Builds/quality/C7/nov03c/v1-*`.

| Segment | Route | Seconds | Unfocused | p95 ms | Result |
|---|---|---:|---:|---:|---|
| v1-01 | chapter-01-new-game-decks | 103.0 | 0 | 16.98 | OK |
| v1-02 | chapter-02-continue-sorrel | 132.6 | 0 | 16.97 | OK |
| v1-03 | chapter-03-defeat-retry | 59.3 | 0 | 17.00 | OK |
| v1-04 | chapter-04-haul-preparation | 231.2 | 0 | 16.99 | OK |
| v1-05 | chapter-05-drill-equipment | 111.7 | 0 | 16.96 | OK |
| v1-06 | chapter-06-burrower | 184.3 | 0 | 17.00 | OK |
| v1-07 | chapter-07-enter-hushwell | 163.2 | 0 | 16.97 | OK |
| v1-08 | chapter-08-hushwell-nursery | 438.5 | 0 | 16.99 | OK |
| v1-09 | chapter-09-calibrated-gullet | 43.9 | 0 | 16.95 | OK |
| v1-10 | chapter-10-gullet-evasive | 783.4 | 0 | 16.97 | OK |
| v1-11 | chapter-11-tallow-reading-return | 233.2 | 0 | 16.93 | OK |
| **Total** | | **2,484.3 (41:24)** | **0** | | |

Every process exited 0 with `QUALITY_REPLAY_OK`; every analysis is valid. The final save is at TallowDrift; both heroes are level 7 at 228 integrity. Worst frames are scene loads (226–253 ms), as before.

Against the nov03b chain under the same capped conditions: 148,882 frames with 51 intervals over 25 ms (nov03b: 149,586 / 48). Medians are 16.67 ms in every dressed segment and p99 within 0.07 ms. The three extra intervals are 25.1–25.4 ms frames at the New Game and Decks scene loads; the Decks walking frames are unchanged (one ~30 ms at step 35 in both chains). This is not C1 timing evidence.

Not established: human play, audio, controller feel, clean awake-display timing, combat balance.
