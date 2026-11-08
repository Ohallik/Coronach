# New Game → homecoming chain after the QA pass (`a8247e6`)

2026-11-07. **Agent-directed, continuous virtual-gamepad replay; not human play.**
- Progression: **PASSED**, 16 routes from New Game to Abri's homecoming (chapters 1 and 2), 61:13 of play, every accepted segment fully focused.
- Frame times: median 16.7 ms throughout, p95 17.09 ms over the whole chain and 17.06 to 17.12 ms by segment, so four segments (`s06`, `s09`, `s12`, `s14`) are just over TEST_PLAN 4.10's 17.1 ms. Seven single frames of 34 to 46 ms fall outside 1.5 s of a scene change, all at a menu opening or a save (below), so 4.10's "no frame over 33 ms outside loads" is **not** met either.
- Feel, listening and physical-controller play stay **UNVERIFIED**.

Conditions:
- Release player built from `a8247e6`, the source of `Builds/Workshop-Nov07` (262 files, verified), 1920×1080 windowed, uncaptured.
- Each segment ran alone, resuming from the previous segment's validated saves. The chain ran as one call with nothing else issued during it, resumed once at chapter 15.
- **Displays awake** under ordinary VSync (D154). The chain's own PowerShell call held a display-required execution state after a one-pixel mouse move, because the detached `keep-displays-awake.ps1` was found dead twice that day. Every segment's median was 16.7 ms, so the timings count.
- Raw folders: `Builds/quality/QA/final/`.

## Rejected runs (kept)

- **`s15-rejected-revive-step`: Hessa yielded with Taren down** (135.9 s fight), and the route's "reach the fallen hero" step didn't get Sela within 1.2 m of him in 16 s, so nobody revived him and Hessa's talk, which waits for that, never started. The step steers straight at the downed hero with no pathfinding, so a column, a crew body or Hessa between them stops it; a player walks round (a revive needs 2 m for 2 s). Logged as a driver limit, QA-035.
- **`s15b-rejected-wipe`: the party fell to Hessa.** On this build the driver won two of her three fights; since her November 6 retune it has lost four of ten. Only a person on a pad can judge her difficulty.
- The rerun from `s14` passed (`s15`, Hessa 131.5 s), and `s16` followed.

## Results

| Segment | Route | Seconds | p95 ms | Over 33 ms | Outside loads | Boss |
|---|---|---:|---:|---:|---:|---|
| s01 | chapter-01-new-game-decks | 135.1 | 17.07 | 5 | 1 | |
| s02 | chapter-02-continue-sorrel | 132.6 | 17.07 | 5 | 1 | |
| s03 | chapter-03-defeat-retry | 134.8 | 17.09 | 4 | 0 | |
| s04 | chapter-04-haul-preparation | 229.6 | 17.08 | 4 | 0 | |
| s05 | chapter-05-drill-equipment | 102.2 | 17.07 | 5 | 1 | |
| s06 | chapter-06-burrower | 274.4 | 17.12 | 4 | 2 | Burrower 139.9 s |
| s07 | chapter-07-enter-hushwell | 97.1 | 17.09 | 5 | 1 | |
| s08 | chapter-08-hushwell-nursery | 494.9 | 17.08 | 6 | 1 | Bellows 125.8 s |
| s09 | chapter-09-calibrated-gullet | 43.9 | 17.11 | 5 | 0 | |
| s10 | chapter-10-gullet-evasive | 872.9 | 17.10 | 3 | 0 | Cantor 160.3 s |
| s11 | chapter-11-tallow-reading-return | 246.1 | 17.06 | 4 | 0 | |
| s12 | chapter-12-wick-reach | 126.8 | 17.11 | 5 | 0 | |
| s13 | chapter-13-lanternwick | 183.3 | 17.10 | 4 | 0 | |
| s14 | chapter-14-lantern-gorge | 134.9 | 17.11 | 4 | 0 | |
| s15 | chapter-15-sunken-relay | 252.7 | 17.10 | 4 | 0 | Hessa 131.5 s |
| s16 | chapter-16-lantern-home | 211.8 | 17.08 | 5 | 0 | |
| **Total** | | **3,673.1 (61:13)** | | **72** | **7** | |

**The seven frames outside loads** (QA-034): opening the save picker (45.7 and 42.5 ms, in `s02` and `s06`, 1.6 s after the title loads), a manual save (40.5 ms, `s01`), opening the fabrication bench (36.9 and 34.4 ms, `s05` and `s07`), entering the Burrower's excavation (40.0 ms, `s06`), and the nursery line that ends a conversation and autosaves (40.4 ms, `s08`). Each is one frame at a menu or a save, and the features that cost it arrived in this pass: save rows that say what they hold, bench rows with stats, and autosaves after a conversation that completes something. The November 5 chain had none.

**Bosses.** Every fight is inside TEST_PLAN's 120 to 300 s for the replay party (`python tools/boss_durations.py Builds/quality/QA/final`). A person's time on a pad is still the real test.

**Package.** `Builds/Workshop-Nov07` is packaged from `a8247e6`: 262 files, verified. `Builds/WindowsDev` is built from the same source.
