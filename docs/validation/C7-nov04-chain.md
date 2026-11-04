# New Game → Tallow chain with the opening scene, displays awake (`d23d238` → `f931c48`)

2026-11-04. **Agent-directed, continuous virtual-gamepad replay; not human play.**

- Progression: **PASSED**, 42:14, every segment fully focused.
- C7 acceptance, continuous audiovisual review and physical-controller feel stay **OPEN/UNVERIFIED**.

Conditions:
- Release player, 1920×1080 windowed, each segment run alone as one blocking call.
- Each segment resumes from the previous segment's earned saves once `quality_analyze.py` has validated them.
- **Displays were awake for the whole run, under ordinary VSync** (no `-DiagnosticVSync 0`), unlike the November 3 chains. An awake request held them on: a continuous display-required execution state, the same thing a video player uses, with no power-setting change. That makes these timings valid under D154; a person still has to watch the motion.
- Raw folders: `Builds/quality/C7/nov04/`.

## Which player ran which segment

- Segments 2-6 ran on `d23d238`. Segment 2 resumed from `s01b`, a passing segment 1 on that same player.
- At threat 1.25, segment 8 rejected: Taren fell in Hushwell's pressure gallery. That run is kept as `s08`. Hushwell's threat returned to 1 (`51cdd87`, D194), and segments 7-11 ran on `51cdd87`.
- The opening eye test then moved the safety line above the deck runners (`f931c48`). Segment 1 was rerun on that published player.
- The players differ from `f931c48` only in Hushwell's threat (segments 2-6 never enter Hushwell) and that decal's height.
- Also kept: the first segment 1 (`s01`), rejected because its route gave the two walks around the walkway job 4-5 s at route speed (about 2.1 m/s). The opening itself played correctly in it.

| Segment | Route | Player | Seconds | p95 ms | >33 ms | Result |
|---|---|---|---:|---:|---:|---|
| v3-s01 | chapter-01-new-game-decks (with the opening) | f931c48 | 136.7 | 16.99 | 2 | OK |
| s02 | chapter-02-continue-sorrel | d23d238 | 132.6 | 17.06 | 6 | OK |
| s03 | chapter-03-defeat-retry | d23d238 | 59.2 | 17.07 | 3 | OK |
| s04 | chapter-04-haul-preparation | d23d238 | 229.5 | 17.06 | 3 | OK |
| s05 | chapter-05-drill-equipment | d23d238 | 111.7 | 16.99 | 3 | OK |
| s06 | chapter-06-burrower | d23d238 | 186.6 | 17.00 | 3 | OK |
| v2-s07 | chapter-07-enter-hushwell | 51cdd87 | 163.2 | 16.99 | 3 | OK |
| v2-s08 | chapter-08-hushwell-nursery | 51cdd87 | 437.4 | 17.04 | 3 | OK |
| v2-s09 | chapter-09-calibrated-gullet | 51cdd87 | 43.9 | 17.04 | 4 | OK |
| v2-s10 | chapter-10-gullet-evasive | 51cdd87 | 800.2 | 17.01 | 3 | OK |
| v2-s11 | chapter-11-tallow-reading-return | 51cdd87 | 233.1 | 16.96 | 2 | OK |
| **Total** | | | **2,534.0 (42:14)** | | | |

**Every process exited 0 with `QUALITY_REPLAY_OK`.**
- The final save is at TallowDrift. Both heroes are level 7 at 228 integrity.
- The save carries `opening.marketRequested` and `opening.walkwayRepaired`.

**Frame times outside scene loads.**
- Every interval over 33 ms except one falls within 1.5 s of a scene change. Those are loads, 220-290 ms as before.
- The one exception is 50.1 ms at Tallow, while a line of the free-line observation was showing. No collection ran, and it is unexplained.

## Decks stability with displays awake (C1)

Route: `station-decks-ten-minute`, release player, `-Census`, VSync on, focused throughout.

| Run | Player | Seconds | p95 / p99 / worst ms | >33.3 / >50 ms |
|---|---|---:|---|---|
| `C1/nov04-awake-decks-stability-release-1` | 34c6d30 | 629.2 | 17.03 / 17.16 / 25.07 | 0 / 0 |
| `C1/nov04-awake-decks-stability-release-2` | 51cdd87 | 629.3 | 17.02 / 17.15 / 26.30 | 0 / 0 |

In both runs the worst frame is the first shop menu (`show choices`).

On October 7 the same route failed twice out of twice: 34/42 ms on warm lap 1, then 62 ms on warm lap 3. Those hitches **did not reproduce** in these two runs, nor anywhere on the Decks in the 42-minute awake chain.

The cause was never identified. Since October the build has changed (lighting, dressing, URP chunks) and so has the machine's state. This is the absence of a reproduction, not a diagnosed fix. Release first/warm laps, development stability, uncapped headroom and a person watching the motion are still needed before C1 can close.
