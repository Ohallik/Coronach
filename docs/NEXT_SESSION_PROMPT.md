# Coronach - continuation prompt (November 3, 2026)

You're continuing development of **Coronach**, an HD-2D action RPG in Unity 6 (URP) about Vael heroes who sync with technology and shift between ground, flight and civilian forms. Project: `C:\Users\natem\Projects\SpaceRPG` (Unity project `Lattice/`); public repo `github.com/Ohallik/Coronach`, branch `main`. Nathan is the creator. The goal is a genuinely great game, not green tests. If something looks or feels poor while tests pass, keep iterating. Make routine decisions yourself (D157) and record consequential ones in `docs/DECISIONS.md`.

## Read first

1. `docs/PROGRESS.md`, newest entry first (November 3), then `docs/WORKSHOP_NOV03.md`.
2. `docs/DECISIONS.md` D186-D190 (and D060 no-blur, D119 fixed-camera sightline rule, D127/D131 paid packs, D154 display-off stall).
3. `docs/PRODUCTION_PLAN.md`: milestones C1-C10, evidence rules, MAP_EYE_TEST.
4. As needed: `docs/STORY_CAMPAIGN.md`, `docs/WORLD_ATLAS.md`, `docs/maps/*.md`, `docs/validation/*.md`. The previous, much longer prompt is in git history if you need older detail.

## Session-start safety (do this before any Unity run)

- **Saves:** Nathan's saves live in `%USERPROFILE%\AppData\LocalLow\Nathan\Lattice\Saves` (`autosave.json` sha256 `65b2ca12…`, `autosave.json.bak` `a9b131c2…` as of November 3). If any `Builds/logs/playmode-human-saves-backup-*` folder exists, a previous run was interrupted. With no Unity/Coronach process alive, compare hashes and move it back. This happened on November 3. Hash the live files before and after every big run.
- **Processes:** only one Unity may own `Lattice/`. Never stop Frostbound or other unrelated Unity processes.
- **Git:** Unity re-adds the embedded grass entry to `Lattice/Packages/packages-lock.json` on every run; revert that file before committing. Never commit `Builds/`, `Library/`, `art-src/`, `tools/meshy/.env`, `Lattice/Assets/Stylized Water 3/`, `Lattice/Packages/xyz.staggart-creations.stylized-grass/` or `Lattice/Assets/_Project/LocalStaggart/`. Check staged paths.

## Where things stand

- The playable chapter runs Title → Decks → Sorrel → Hushwell (nursery discovery, required for the Gullet) → Halo → Gullet (Cantor collar fight and release) → Tallow (moving-refuge line discovery, Keeper acknowledgement). On November 3 an agent-directed virtual-gamepad chain completed New Game → Tallow in 41:32 (`docs/validation/C7-nov03-chain.md`). 175 EditMode / 455 PlayMode tests passed in full runs on `729aae0`; the later practical-light change passed its 3 look checks and 93 affected PlayMode checks.
- **Nathan's current build:** `Builds/Workshop-Nov03/Play Coronach.cmd` (source `a7409db`, own profile). The frozen October 4 chapter `Builds/Workshop-Chapter` (`a3aaad6`) must stay byte-identical (243 files).
- Paid Staggart water and grass are a private, ignored layer with full public fallbacks; a fresh clone opens, builds and plays without them (`docs/OPTIONAL_ART_PACKS.md`, D131).
- Every zone has authored light and a colour grade (`ZoneLook.Specs`, D189), and warm practical lights sit at Decks and Tallow speakers.

## What to do next, in order

1. **Make the places worth walking through. This is the biggest gap.** Gameplay-camera stills show the Decks and Tallow as large empty grids of the same cross-pattern floor plate, and Sorrel as wide flat ochre plains with tufts and smooth slopes. Furnish each interior room for its purpose (market stalls and goods, Orrin's office, galley, residences, workshop benches, signage, visible lamp fixtures for the practical pools, floor wear and runner strips along real circulation). Give Sorrel ridge strata and rock outcrops, ground variation, spoil heaps and outpost life. Obey D119: nothing may rise into the sightline from the ground camera (pitch 40°, yaw 20°, 19.5 m, FOV 30°) to a hero on any walkable floor, so dress slopes and backs, not the camera side, and keep vegetation under 0.6 m. Keep collision, navigation, encounter/save IDs and replay routes valid, and use isolated builders, never `WorldBuilder.Prepare`/`BuildAll`. Reuse generated kits first; Meshy has about 2,383 credits (verify live before spending). Judge with before/after `LookDev.Render` stills, then the in-player `scripts/look.ps1`, then the relevant MAP_EYE_TEST questions.
2. **Remaining C8 staging:** opening walkway and Sela's arrival scene; resident interiors and town/service loops; repetitive nursery cradles; a reeling animation and a less flat cable for Tallow's line; the Gullet coil's quiet pre-fight composition.
3. **Feel and presentation defects still open:** Sela's high stepping on slopes; the awake-display Decks hitches (34/42/62 ms, unexplained); combat balance (fights currently 50-80 s by agent input). Timing evidence is only valid with displays awake. Check display state first (D154); with displays asleep, use `-DiagnosticVSync 0` for progression runs only.
4. After a meaningful batch: full suites, rebuild both players, rerun the chain, publish `Builds/Workshop-<date>` with a short note for Nathan, commit and push.
5. Then C9A (Xylos / Many Hands) per the atlas, with blockouts before art.

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
```

Chain order: chapter-01-new-game-decks (no ResumeFrom), 02-continue-sorrel, 03-defeat-retry, 04-haul-preparation, 05-drill-equipment, 06-burrower, 07-enter-hushwell, 08-hushwell-nursery, 09-calibrated-gullet, 10-gullet-evasive (~13 min: it outlives the 10-minute tool limit and moves to the background; issue nothing until it reports), 11-tallow-reading-return. `python tools/quality_chain_summary.py <folders>` prints a one-line verdict per run.

## Ground rules

- Player-window runs (replays, look, smokes) reject on focus loss: run each alone as one blocking call, with nothing else issued during it.
- Full PlayMode needs `-TimeoutSec 3600`; the default kills it and strands the saves.
- Use isolated builder entry points only.
- Commit and push as each gate completes; keep PROGRESS newest-first and validation notes current.
