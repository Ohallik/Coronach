# New Game → Tallow chain after the script pass, skills and chapter 2 (`009018d`)

2026-11-05. **Agent-directed, continuous virtual-gamepad replay; not human play.**
- Progression: **PASSED**, 44:51, every segment fully focused.
- C7 acceptance, continuous audiovisual review and physical-controller feel stay **OPEN/UNVERIFIED**.
- Chapter 2 is not in this chain; its routes are the QA pass's first task (TEST_PLAN §5).

Conditions:
- Release player built from `009018d`, 1920×1080 windowed.
- Each segment ran alone as one blocking call, resuming from the previous segment's validated saves.
- **Displays awake** under ordinary VSync, held by a display-required execution state (D154). The timings count.
- Raw folders: `Builds/quality/C7/nov05/`.

## Rejected runs (kept) and route changes

Routes changed by insertion only, except the two husk legs noted below.
- **`s01`: the displays were already off.** Frames alternated between 2 and 265 ms (the D154 present stall); the walkway conversation timed out. The keep-awake state cannot wake displays that are already dark, so a synthetic one-pixel mouse move woke them. `s01b` then ran at p95 17.05 ms.
- **`s01b` and `s02`: the new Skills tab sits between Party and Gear.** Segment 1 pressed "previous tab" twice to reach Party and landed on Skills, so slot 1 was never saved and segment 2 found nothing to load. Segments 1 and 5 now pass the Skills tab with one more press. `s01c` passed.
- **`s03` and `s03b`: the companion now fights with its skills (D201).** Standing Taren undefended in the first clearing and the Scrapmite wash no longer produced a defeat; Sela's Static Net cleared the swarm.
  - The route now walks the undefended party on up the east branch to a second wash, where the defeat comes.
  - `s03b` showed the two further legs, toward the husks, end early once the party falls, so they were removed. `s03c` passed.
- **`s06`: both heroes fell in the Burrower's last phase**, 73 s into the fight, and the route had no defeat in it.
  - The route is unchanged: `s06b` passed with the Burrower down in 80.8 s.
  - This is the second chain in a row where the last phase beats the replay driver at least once. It is a difficulty edge to watch, not a defect proven.
- **`s11`: the lantern tale.** After the Compact's call, the Keeper's next words are now Abri's lantern tale (D202), which starts chapter 2. The route reads it to `lantern.routeKnown`. The first attempt omitted the press that starts the conversation; `s11b` passed.

## Results

| Segment | Route | Seconds | p95 ms | Boss |
|---|---|---:|---:|---|
| s01c | chapter-01-new-game-decks | 137.3 | 17.04 | |
| s02b | chapter-02-continue-sorrel | 132.6 | 17.05 | |
| s03c | chapter-03-defeat-retry | 147.2 | 17.11 | |
| s04 | chapter-04-haul-preparation | 230.8 | 17.08 | |
| s05 | chapter-05-drill-equipment | 112.4 | 17.05 | |
| s06b | chapter-06-burrower | 212.8 | 17.08 | Burrower 80.8 s (s06: party defeated) |
| s07 | chapter-07-enter-hushwell | 163.2 | 17.05 | |
| s08 | chapter-08-hushwell-nursery | 457.4 | 17.11 | Bellows 87.2 s |
| s09 | chapter-09-calibrated-gullet | 43.9 | 17.08 | |
| s10 | chapter-10-gullet-evasive | 807.7 | 17.06 | Cantor 88.2 s |
| s11b | chapter-11-tallow-reading-return | 246.2 | 17.05 | |
| **Total** | | **2,691.5 (44:51)** | | |

**Frame times.** Thirty-six frames over 33 ms, all within 1.5 s of a scene change. None outside loads; the previous chain's 45 ms Scrapmite-wash frame did not recur. Every accepted process exited 0 with `QUALITY_REPLAY_OK`.

**Companion.** The companion visibly uses its skills (`SKILL_KIND Sela …` throughout the Burrower and Bellows fights).

**Package.** `Builds/Workshop-Nov05` is packaged from `009018d`: 264 files, verified.
