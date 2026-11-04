# Coronach - continuation prompt (November 4, 2026, after the first-morning batch)

You're continuing development of **Coronach**, an HD-2D action RPG in Unity 6 (URP) about Vael heroes who sync with technology and shift between ground, flight and civilian forms. Project: `C:\Users\natem\Projects\SpaceRPG` (Unity project `Lattice/`); public repo `github.com/Ohallik/Coronach`, branch `main`. Nathan is the creator. The goal is a genuinely great game, not green tests. If something looks or feels poor while tests pass, keep iterating. Make routine decisions yourself (D157) and record consequential ones in `docs/DECISIONS.md`.

## Read first

1. `docs/PROGRESS.md`, newest entry first (November 4: the first morning, dressed exteriors, coiled Cantor, reeling line), then `docs/WORKSHOP_NOV04.md`, `docs/validation/C8-nov04-batch.md`, `docs/validation/C7-nov04-chain.md` and `docs/validation/C2-uphill-fold.md`.
2. `docs/DECISIONS.md` D186-D196 (D191 room dressing and `SightlineAudit`, D192 detail layer, D193 Hushwell/Gullet dressing, D194 skills and zone threat, D195 the first morning, D196 exteriors, coil, reel) (and D060 no-blur, D119 fixed-camera sightline rule, D127/D131 paid packs, D154 display-off stall).
3. `docs/PRODUCTION_PLAN.md`: milestones C1-C10, evidence rules, MAP_EYE_TEST.
4. As needed: `docs/STORY_CAMPAIGN.md`, `docs/WORLD_ATLAS.md`, `docs/maps/*.md`, `docs/validation/*.md`. The previous, much longer prompt is in git history if you need older detail.

## Session-start safety (do this before any Unity run)

- **Saves:** Nathan's saves live in `%USERPROFILE%\AppData\LocalLow\Nathan\Lattice\Saves` (`autosave.json` sha256 `65b2ca12…`, `autosave.json.bak` `a9b131c2…` as of November 4). If any `Builds/logs/playmode-human-saves-backup-*` folder exists, a previous run was interrupted. With no Unity/Coronach process alive, compare hashes and move it back. This happened on November 3. Hash the live files before and after every big run.
- **Processes:** only one Unity may own `Lattice/`. Never stop Frostbound or other unrelated Unity processes.
- **Git:** Unity re-adds the embedded grass entry to `Lattice/Packages/packages-lock.json` on every run; revert that file before committing. Never commit `Builds/`, `Library/`, `art-src/`, `tools/meshy/.env`, `Lattice/Assets/Stylized Water 3/`, `Lattice/Packages/xyz.staggart-creations.stylized-grass/` or `Lattice/Assets/_Project/LocalStaggart/`. Check staged paths.

## Where things stand

- The playable chapter runs Title → the first morning on the Decks → Sorrel → Hushwell → Halo → Gullet → Tallow.
- The New Game → Tallow chain passes in 42:14 (`docs/validation/C7-nov04-chain.md`).
  - **The displays were awake under ordinary VSync**: a display-required execution state held them on, so the timings count.
  - Outside scene loads there was one frame over 33 ms (Tallow dialogue, unexplained).
  - Two awake ten-minute Decks stability runs are clean; October's hitches did not reproduce and are not diagnosed.
- **188 EditMode / 470 PlayMode** pass.
- **Nathan's current build:** `Builds/Workshop-Nov04/Play Coronach.cmd` (source `f931c48`, own profile).
  - `Workshop-Nov03d` (`34c6d30`), `Nov03c`, `Nov03b`, `Nov03` and the frozen `Builds/Workshop-Chapter` (`a3aaad6`) must stay byte-identical to their `player-manifest.json` (hashes are of the files under `Player/`).
- **The first morning (D195).** Only a Title New Game sets `opening.marketRequested`; Continue, old saves and fixtures skip the scene.
  - `MarketOpeningStaging` (isolated) builds the gates, cover, cart and repair point on the Decks bridge.
  - `OpeningMarketSequence` presents the saved state and holds Orrin until `opening.walkwayRepaired`.
  - `ScriptedWalk` moves Sela and Mira.
  - `<<stationShift>>` gives the deck tremor.
  - Rerun `MarketOpeningStaging.Refresh` after any Decks rebuild; it sweeps both walks against the real scene.
- **Dressing.**
  - `RoomDressing.Apply` dresses the Decks, Tallow, Sorrel, Hushwell, the Gullet, **the Halo roofs and the Tallow refuge deck** (D191-D196). Rerun it after any rebuild of those scenes (`-dress-zones`).
  - Exterior roofs: collider-free, on a roof above the flight plane, 2 m back from every edge (`ExteriorAudit`).
  - Flight zones: details only, below y=-0.6 (`FlightAudit`).
  - Hushwell's floor material uses 6 toon ramp steps (the rock 3).
- **Actors.** The dormant Cantor rests coiled (`SerpentSegments.Rest`). Tallow's line is a generated rope (`TallowMooringStaging.RefreshCable`) that reels in after `tallow.lineObserved` (`TallowLineReel`).
- **Balance (D194).** Burst skills deal 2.6× output (`CombatActor.SkillBurst`). `ZoneDef.enemyThreat`: Gullet 1.2, Sorrel and Hushwell 1. At 1.25 in Hushwell, Taren fell in the chain.
- Meshy live balance 3,142.
- Paid Staggart water and grass stay a private, ignored layer with public fallbacks (D131).

## What to do next, in order

1. **Sela's uphill fold (both heroes fold).**
   - Re-pose uphill walking in `GroundFeet` in the gravity frame: an upright torso, slope-aware foot targets, and a trailing heel that lifts at toe-off.
   - Do not retune parameters; five variants broke the planted-sole and knee-continuity checks (`docs/validation/C2-uphill-fold.md`).
   - `Lattice.EditorTools.UphillStepAudit.Run` is the acceptance check; it is OPEN at 8 of 8 on the committed walk.
2. **Town life.**
   - Resident interiors and town/service loops need ordinary resident bodies. Generate 3-4 Decks residents with the Meshy character pipeline (rigged to the donor, a measured stride profile each), then give them `ScriptedWalk`-style loops on the public and service routes.
   - Mira's walk back to her bay has no measured stride profile.
3. **Places.**
   - The Cantor preview's in-player eye test (the coil now rests coiled; the moorings keep their rectangle).
   - The refuge hull's own texture is soft in the closest dock view.
4. **Presentation.**
   - The one 50 ms frame on a Tallow dialogue line.
   - C1's remaining evidence: release first/warm laps, development stability, uncapped headroom, a person watching the motion.
   - Check that the displays are awake before timing runs (D154).
5. After a meaningful batch: full suites, rebuild both players, rerun the chain, publish `Builds/Workshop-<date>` with a short note for Nathan, commit and push.
6. Then C9A (Xylos / Many Hands) per the atlas, with blockouts before art.

**Evidence proportionality.** Keep the hard rules: prove a new check fails on the broken state first, never loosen an assertion, keep failed evidence, and never claim unobserved quality (human play, controller feel, listening, continuous motion). But keep validation notes short, and spend time on what a player sees and feels. Earlier sessions spent whole days on single props and evidence bookkeeping while the maps stayed empty.

## Commands that work

```powershell
# compile / suites (PlayMode full takes about an hour: run it in the background and start no other Unity work)
.\scripts\headless.ps1 -Mode verify
.\scripts\headless.ps1 -Mode tests -TimeoutSec 1800
.\scripts\headless.ps1 -Mode playtests -Graphics -TimeoutSec 3600
.\scripts\headless.ps1 -Mode playtests -Graphics -TestFilter "Lattice.Tests.PlayMode.A;Lattice.Tests.PlayMode.B"
# editor methods with arguments: call with & from PowerShell (powershell -File cannot pass arrays)
& .\scripts\exec.ps1 -Method Lattice.EditorTools.LookDev.Render -Marker LOOKDEV_OK -Graphics -ExtraArgs @('-look-run','<new-name>','-local-biomes','-look-preview','-look-zones','Hub_Decks,Sorrel_Ridges')
& .\scripts\exec.ps1 -Method Lattice.EditorTools.ZoneLook.Apply -Marker ZONE_LOOK_OK -ExtraArgs @('-look-zones','Hub_Decks')
.\scripts\look.ps1 -Folder Builds/logs/look/<new-name>          # in-player stills, dev player
# players (move the previous Builds/Windows* into Builds/quality/... first)
.\scripts\headless.ps1 -Mode build -TimeoutSec 1800 ; .\scripts\headless.ps1 -Mode builddev -TimeoutSec 1800
# chain segment (one blocking call, nothing else while it runs), then analyse before the next -ResumeFrom
.\scripts\quality-replay.ps1 -Route docs/quality/routes/chapter-02-continue-sorrel.json -ResumeFrom Builds/quality/<prev> -Output Builds/quality/<new> -Build Release [-DiagnosticVSync 0] -TimeoutSec 780
python tools/quality_analyze.py Builds/quality/<new>
python tools/package_workshop.py Workshop-<name> --revision <sha>
# eye-test: add -Capture to a replay, then labelled contact sheets of every step
python tools/eye_sheet.py Builds/quality/<run> --per-sheet 16 --every 2
```

Dressing: `& .\scripts\exec.ps1 -Method Lattice.EditorTools.RoomDressing.Apply -Marker ROOM_DRESSING_OK -Graphics -ExtraArgs @('-dress-zones','Hub_Decks,TallowDrift,Sorrel_Ridges,Hushwell,Gullet_Tunnel','-dress-final')` (a refused zone is left unsaved and the run fails; read `DRESSING_PROBLEM` lines in the exec log).

Opening staging: `& .\scripts\exec.ps1 -Method Lattice.EditorTools.MarketOpeningStaging.Refresh -Marker MARKET_OPENING_STAGING_OK -Graphics`. Tallow rope/reel: `-Method Lattice.EditorTools.TallowMooringStaging.RefreshCable -Marker TALLOW_CABLE_OK`. Uphill fold audit: `-Method Lattice.EditorTools.UphillStepAudit.Run -Marker UPHILL_STEP_AUDIT -Graphics` (prints `UPHILL_STEP_AUDIT OPEN|PASS`).

Chain order: chapter-01-new-game-decks (no ResumeFrom), 02-continue-sorrel, 03-defeat-retry, 04-haul-preparation, 05-drill-equipment, 06-burrower, 07-enter-hushwell, 08-hushwell-nursery, 09-calibrated-gullet, 10-gullet-evasive (~13 min: it outlives the 10-minute tool limit and moves to the background; issue nothing until it reports), 11-tallow-reading-return. `python tools/quality_chain_summary.py <folders>` prints a one-line verdict per run.

## Ground rules

- Player-window runs (replays, look, smokes) reject on focus loss: run each alone as one blocking call, with nothing else issued during it.
- Full PlayMode needs `-TimeoutSec 3600`; the default kills it and strands the saves.
- Use isolated builder entry points only.
- Run Meshy image tasks one at a time: parallel tasks race on `docs/art/gen-manifest.json` and drop rows.
- `-Capture` runs write a PNG per step; their frame times are not comparable with uncaptured runs.
- Commit and push as each gate completes; keep PROGRESS newest-first and validation notes current.
