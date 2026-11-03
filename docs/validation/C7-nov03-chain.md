# New Game → Tallow chain on the November 3 source (C7 candidate)

2026-11-03. **Agent-directed continuous virtual-gamepad replay, not human play.** Status: progression **PASSED** on release players built from `729aae0` (full chain) and `a7409db` (re-run of the two segments whose scenes changed); C7 acceptance, continuous audiovisual review, physical-controller feel and clean C1 timing remain **OPEN/UNVERIFIED**.

## Conditions

Release player, 1920×1080 windowed, Ultra. Windows had the displays asleep, which reproduces the known quarter-second synchronized-present stall (D154): the first attempt with startup VSync (`chain-01`) completed its route at ~4 fps (881 samples / 136 s, p95 266 ms) and is retained but not used. Every segment below used the replay's explicit `-DiagnosticVSync 0` (60 fps cap), so this is **progression evidence only, excluded from clean C1 timing**. Each segment ran alone as one blocking call; each later segment resumes from the previous segment's exact earned saves (`tools/quality_resume.py`), after `tools/quality_analyze.py` validated the prior run. Raw folders: `Builds/quality/C7/nov03/`.

| Segment | Route | Seconds | Samples | Unfocused | p95 ms | Result |
|---|---|---:|---:|---:|---:|---|
| v0-01 | chapter-01-new-game-decks | 102.9 | 6,161 | 0 | 16.98 | OK |
| v0-02 | chapter-02-continue-sorrel | 132.6 | 7,935 | 0 | 16.98 | OK |
| v0-03 | chapter-03-defeat-retry | 59.3 | 3,544 | 0 | 16.99 | OK |
| v0-04 | chapter-04-haul-preparation | 231.2 | 13,860 | 0 | 17.00 | OK |
| v0-05 | chapter-05-drill-equipment | 111.6 | 6,682 | 0 | 16.98 | OK |
| v0-06 | chapter-06-burrower | 182.5 | 10,934 | 0 | 16.99 | OK (Burrower 50.6 s) |
| v0-07 | chapter-07-enter-hushwell | 163.1 | 9,774 | 0 | 16.97 | OK |
| v0-08 | chapter-08-hushwell-nursery | 447.2 | 26,818 | 0 | 16.98 | OK (Bellows 80.4 s) |
| v0-09 | chapter-09-calibrated-gullet | 43.9 | 2,619 | 0 | 16.99 | OK |
| v0-10 | chapter-10-gullet-evasive | 784.4 | 47,052 | 0 | 16.98 | OK (Cantor 72.4 s) |
| v0-11 | chapter-11-tallow-reading-return | 233.2 | 13,976 | 0 | 16.94 | OK |
| **Total** | | **2,491.9 (41:32)** | | **0** | | |
| final-01 | chapter-01 on `a7409db` | 102.9 | 6,157 | 0 | 16.99 | OK |
| final-11 | chapter-11 on `a7409db` (from v0-10 saves) | 233.1 | 13,975 | 0 | 16.94 | OK |

All runtime `run.json` and offline `analysis.json` verdicts are valid with no failures; every process exited 0 with `QUALITY_REPLAY_OK`. The 126-step Tallow route (`chapter-11-tallow-reading-return`) had never been executed before; it passes here. The final save is at TallowDrift with `hushwell.nursery`, both organ breaks, `sliceComplete`, `tallow.lineObserved` and `tallow.driftDiscussed`; both heroes are level 7 (Taren 123 / Sela 181 integrity after Cantor).

Each segment's worst frame (221–235 ms) is the Continue scene load (step 5, UI `loading`), followed by a few 28–38 ms frames at load and arrival. These are recorded, not explained away.

## Not established

No video, audio audition or human play. The fights' durations are agent-input outcomes, not accepted balance. Display-off/VSync-0 conditions cannot accept C1 presentation. The station's original awake 34–62 ms Decks failures remain open.
