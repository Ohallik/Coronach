# Coronach - continuation prompt (November 3, 2026, after dressing pass 2)

You're continuing development of **Coronach**, an HD-2D action RPG in Unity 6 (URP) about Vael heroes who sync with technology and shift between ground, flight and civilian forms. Project: `C:\Users\natem\Projects\SpaceRPG` (Unity project `Lattice/`); public repo `github.com/Ohallik/Coronach`, branch `main`. Nathan is the creator. The goal is a genuinely great game, not green tests. If something looks or feels poor while tests pass, keep iterating. Make routine decisions yourself (D157) and record consequential ones in `docs/DECISIONS.md`.

## Read first

1. `docs/PROGRESS.md`, newest entry first (November 4: Hushwell lit and dressed, the Gullet's floor), then `docs/WORKSHOP_NOV03D.md` and `docs/validation/C8-dressing-pass3.md`.
2. `docs/DECISIONS.md` D186-D193 (D191 room dressing and `SightlineAudit`, D192 detail layer, D193 Hushwell/Gullet dressing, lit cave chunks, flight ceiling) (and D060 no-blur, D119 fixed-camera sightline rule, D127/D131 paid packs, D154 display-off stall).
3. `docs/PRODUCTION_PLAN.md`: milestones C1-C10, evidence rules, MAP_EYE_TEST.
4. As needed: `docs/STORY_CAMPAIGN.md`, `docs/WORLD_ATLAS.md`, `docs/maps/*.md`, `docs/validation/*.md`. The previous, much longer prompt is in git history if you need older detail.

## Session-start safety (do this before any Unity run)

- **Saves:** Nathan's saves live in `%USERPROFILE%\AppData\LocalLow\Nathan\Lattice\Saves` (`autosave.json` sha256 `65b2ca12…`, `autosave.json.bak` `a9b131c2…` as of November 4). If any `Builds/logs/playmode-human-saves-backup-*` folder exists, a previous run was interrupted. With no Unity/Coronach process alive, compare hashes and move it back. This happened on November 3. Hash the live files before and after every big run.
- **Processes:** only one Unity may own `Lattice/`. Never stop Frostbound or other unrelated Unity processes.
- **Git:** Unity re-adds the embedded grass entry to `Lattice/Packages/packages-lock.json` on every run; revert that file before committing. Never commit `Builds/`, `Library/`, `art-src/`, `tools/meshy/.env`, `Lattice/Assets/Stylized Water 3/`, `Lattice/Packages/xyz.staggart-creations.stylized-grass/` or `Lattice/Assets/_Project/LocalStaggart/`. Check staged paths.

## Where things stand

- The playable chapter runs Title → Decks → Sorrel → Hushwell → Halo → Gullet → Tallow. An agent-directed virtual-gamepad chain completed New Game → Tallow in 41:26 (`docs/validation/C7-nov03d-chain.md`; segments 1-8 on `d6643fb`, 9-11 on `34c6d30`; displays asleep, `-DiagnosticVSync 0`, progression only). **188 EditMode / 461 PlayMode** pass.
- **Nathan's current build:** `Builds/Workshop-Nov03d/Play Coronach.cmd` (source `34c6d30`, own profile). `Builds/Workshop-Nov03c` (`5a656ce`), `Workshop-Nov03b` (`f019bcf`), `Workshop-Nov03` (`a7409db`) and the frozen `Builds/Workshop-Chapter` (`a3aaad6`, 243 files) must stay byte-identical.
- The Decks, Tallow, Sorrel, Hushwell and the Gullet are dressed by `Lattice.EditorTools.RoomDressing.Apply` (D191-D193): it rebuilds only each scene's `Room dressing` root, rebakes navigation and refuses layouts that block replay routes/doors/speakers/spawns/encounters, remove a prompt approach, or hide a hero (`Lattice.World.SightlineAudit`). **Rerun it after any rebuild of those scenes** (`-dress-zones`, `-dress-floors` for deck wear). D192 adds a flat detail layer (`RoomDressing.Detail.cs`/`Places.cs`: studs, pictograms, hazard paint, inset plates, Sorrel ground patches and tyre tracks, plants, boulders, beacons) and lit bollards; `RoomDressingDetailTests` (EditMode) keep every detail on the real ground. Route protection is per segment: a segment into an interactable is an approach; one through a wall marks the route obsolete. Generated `Dress*` kit: 27 pieces plus the D193 six (work lamp, rail buffer, power cell, wall crystal, hatched and single-egg cradles) in `Prefabs/Environment`; decal textures in `Art/Generated/Dressing` (D193 sources cut by `tools/dressing_decals3.py`). Hushwell: the builder also draws the cave in 16 m `Lit render chunks` (the whole-cave renderer is switched off; collider untouched), opens its seals for the run and restores them before saving. Gullet: no solid pieces; every renderer below y=-0.6 (`FlightAudit`). Meshy live balance 3,151.
- Paid Staggart water and grass stay a private, ignored layer with public fallbacks (D131). Every zone has authored light and grade (D189).

## What to do next, in order

1. **Places, remaining.** MAP_EYE_TEST_OK now holds for the Decks interior/Halo pairing, Tallow, Sorrel (blockout recorded November 4 from retained evidence), and Hushwell, all by agent review. Hushwell's lower ramp into the nursery is still dark (the miners never went below; more seam light, not lamps). The Gullet floor passes in player; the coil's dormant-preview composition and release presentation stay open in its brief, and its scour arcs end square. Next places: the Halo exterior and TallowApproach never had a dressing pass; look at them with the same eyes. Keep D119 via `SightlineAudit`; collidable pieces on open floor stay ≤ ~1.4 m.
2. **Remaining C8 staging:** opening walkway and Sela's arrival scene; resident interiors and town/service loops; a reeling animation and a less flat cable for Tallow's line; the Gullet coil's quiet pre-fight composition.
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
# eye-test: add -Capture to a replay, then labelled contact sheets of every step
python tools/eye_sheet.py Builds/quality/<run> --per-sheet 16 --every 2
```

Dressing: `& .\scripts\exec.ps1 -Method Lattice.EditorTools.RoomDressing.Apply -Marker ROOM_DRESSING_OK -Graphics -ExtraArgs @('-dress-zones','Hub_Decks,TallowDrift,Sorrel_Ridges,Hushwell,Gullet_Tunnel','-dress-final')` (a refused zone is left unsaved and the run fails; read `DRESSING_PROBLEM` lines in the exec log).

Chain order: chapter-01-new-game-decks (no ResumeFrom), 02-continue-sorrel, 03-defeat-retry, 04-haul-preparation, 05-drill-equipment, 06-burrower, 07-enter-hushwell, 08-hushwell-nursery, 09-calibrated-gullet, 10-gullet-evasive (~13 min: it outlives the 10-minute tool limit and moves to the background; issue nothing until it reports), 11-tallow-reading-return. `python tools/quality_chain_summary.py <folders>` prints a one-line verdict per run.

## Ground rules

- Player-window runs (replays, look, smokes) reject on focus loss: run each alone as one blocking call, with nothing else issued during it.
- Full PlayMode needs `-TimeoutSec 3600`; the default kills it and strands the saves.
- Use isolated builder entry points only.
- Run Meshy image tasks one at a time: parallel tasks race on `docs/art/gen-manifest.json` and drop rows.
- `-Capture` runs write a PNG per step; their frame times are not comparable with uncaptured runs.
- Commit and push as each gate completes; keep PROGRESS newest-first and validation notes current.
