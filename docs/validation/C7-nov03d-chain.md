# New Game → Tallow chain on dressing pass 3 (`d6643fb` / `34c6d30`)

2026-11-04. **Agent-directed continuous virtual-gamepad replay, not human play.** Progression **PASSED**. C7 acceptance, continuous audiovisual review, physical-controller feel and clean C1 timing remain **OPEN/UNVERIFIED**.

Same conditions as [C7-nov03c-chain](C7-nov03c-chain.md): release player, 1920×1080 windowed, each segment alone as one blocking call, resuming from the previous segment's earned saves after `quality_analyze.py` validated it. Displays were asleep (last OS input 49 min before the first run, 20-minute display timeout), so every segment used explicit `-DiagnosticVSync 0`: **progression evidence only**. Raw folders: `Builds/quality/C7/nov03d/`.

Segments 1-8 ran on the `d6643fb` player. Its captured Gullet review then showed the vessel decals blooming brighter than the ships; `34c6d30` only quiets that one material, and segments 9-11 were rerun on the `34c6d30` player that is published as `Workshop-Nov03d`. The rejected `d6643fb` players and their 9-11 runs (`v1-09`..`v1-11`, all OK) are kept.

| Segment | Route | Player | Seconds | Unfocused | p95 ms | Result |
|---|---|---|---:|---:|---:|---|
| v1-01 | chapter-01-new-game-decks | d6643fb | 102.9 | 0 | 16.98 | OK |
| v1-02 | chapter-02-continue-sorrel | d6643fb | 132.5 | 0 | 16.96 | OK |
| v1-03 | chapter-03-defeat-retry | d6643fb | 59.3 | 0 | 16.99 | OK |
| v1-04 | chapter-04-haul-preparation | d6643fb | 231.3 | 0 | 16.99 | OK |
| v1-05 | chapter-05-drill-equipment | d6643fb | 111.8 | 0 | 16.96 | OK |
| v1-06 | chapter-06-burrower | d6643fb | 185.1 | 0 | 17.00 | OK |
| v1-07 | chapter-07-enter-hushwell | d6643fb | 163.1 | 0 | 16.98 | OK |
| v1-08 | chapter-08-hushwell-nursery | d6643fb | 441.2 | 0 | 17.00 | OK |
| v2-09 | chapter-09-calibrated-gullet | 34c6d30 | 43.9 | 0 | 16.97 | OK |
| v2-10 | chapter-10-gullet-evasive | 34c6d30 | 782.2 | 0 | 16.98 | OK |
| v2-11 | chapter-11-tallow-reading-return | 34c6d30 | 233.0 | 0 | 16.94 | OK |
| **Total** | | | **2,486.3 (41:26)** | **0** | | |

Every process exited 0 with `QUALITY_REPLAY_OK`; every analysis is valid. The final save is at TallowDrift; both heroes are level 7 at 228 integrity. Worst frames are scene loads (220-235 ms), as before.

Against the nov03c chain under the same capped conditions (v1 run, all 11 segments on `d6643fb`): 51 intervals over 25 ms in both chains, medians 16.67 ms in every segment, p99 within 0.21 ms. Hushwell now draws 200 lit chunks and about 25 more point lights; segments 7-8 show no change (5/5 and 4/4 intervals over 25 ms). This is not C1 timing evidence.

Not established: human play, audio, controller feel, clean awake-display timing, combat balance.
