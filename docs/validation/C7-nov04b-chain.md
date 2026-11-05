# New Game → Tallow chain with the boss move sets and side stories (`1bd102c` → `a2929dd`)

2026-11-04 (evening). **Agent-directed, continuous virtual-gamepad replay; not human play.**

- Progression: **PASSED**, 43:51, every segment fully focused.
- C7 acceptance, continuous audiovisual review and physical-controller feel stay **OPEN/UNVERIFIED**.

Conditions:
- Release player, 1920×1080 windowed, each segment run alone as one blocking call, resuming from the previous segment's validated saves.
- **Displays awake under ordinary VSync**, held by a display-required execution state for the whole chain (D154). The timings count.
- Raw folders: `Builds/quality/C7/nov04b/`.

## Which player and route ran which segment

- Segments 1-5 ran on players built from `1bd102c`. Segments 6-11 ran on `a2929dd`, which differs only in the replay driver (`ensureCharacter`).
- Routes changed by insertion only:
  - **Segment 3.** Sela now wins the first hound clearing alone while Taren stands undefended. Run `s03` (kept) cleared it with no defeat. The route now waits there, then walks on into the Scrapmite wash; `s03b` (kept) was a first attempt whose walk never moved. `s03c` passed.
  - **Segment 6.** In `s06` (kept, passed) Taren went down in the Burrower's last phase and Sela finished the fight, so the earned save left Sela leading and segment 7 rejected at load (`s07`, kept). The route now presses Swap until Taren leads before the key autosave. `s06b` passed; Taren had kept the lead, so no press was needed.
  - **Segment 11.** The Keeper's second talk is now the Compact's call (D199). `s11` (kept) rejected on it, and the route now reads the call before the repeat line. `s11b` passed.

| Segment | Route | Seconds | p95 ms | >33 ms | Outside loads | Boss |
|---|---|---:|---:|---:|---:|---|
| s01 | chapter-01-new-game-decks | 136.8 | 16.99 | 2 | 0 | |
| s02 | chapter-02-continue-sorrel | 132.6 | 16.98 | 5 | 0 | |
| s03c | chapter-03-defeat-retry | 114.2 | 17.01 | 4 | 1 | |
| s04 | chapter-04-haul-preparation | 231.7 | 17.03 | 3 | 0 | |
| s05 | chapter-05-drill-equipment | 111.8 | 17.01 | 3 | 0 | |
| s06b | chapter-06-burrower | 188.4 | 17.01 | 3 | 0 | Burrower 55.1 s (s06: 83.0 s, three phases) |
| s07b | chapter-07-enter-hushwell | 163.1 | 16.99 | 3 | 0 | |
| s08 | chapter-08-hushwell-nursery | 460.5 | 17.01 | 4 | 0 | Bellows 95.0 s, three phases (was 84.0) |
| s09 | chapter-09-calibrated-gullet | 43.9 | 16.97 | 4 | 0 | |
| s10 | chapter-10-gullet-evasive | 805.7 | 17.01 | 3 | 0 | Cantor 86.3 s, three phases, spiral song (was 80.9) |
| s11b | chapter-11-tallow-reading-return | 242.6 | 16.98 | 3 | 0 | |
| **Total** | | **2,631.2 (43:51)** | | | **1** | |

Every accepted process exited 0 with `QUALITY_REPLAY_OK`.

**Frame times.** Every interval over 33 ms but one falls within 1.5 s of a scene change. The one exception is 45.2 ms in segment 3, as the undefended party walked into the six-Scrapmite wash, probably that encounter's first spawn. The unexplained 50 ms Tallow dialogue frame from the previous chain did not recur.

**Packages.** `Workshop-Nov04b` is packaged from `a2929dd` (248 files). `Nov04`, `Nov03d`, `Nov03c`, `Nov03b`, `Nov03` and the frozen chapter all verify against their manifests with 0 mismatches.

**Saves.** Nathan's LocalLow saves are unchanged by the run: `a87ab0b0…`, `5f7218db…`, from his own November 4 session. No backup folders remain.
