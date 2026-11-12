# Coronach - continuation prompt (November 7, 2026, after the QA pass)

> **For another QA pass, use [TESTER_PROMPT.md](TESTER_PROMPT.md)**, which hands the game to a tester session that plays through the replay driver and fixes what it finds, following [TEST_PLAN.md](TEST_PLAN.md). The November pass's log is [qa/ISSUES.md](qa/ISSUES.md). Take commands and state from this file.

You're continuing development of **Coronach**, an HD-2D action RPG in Unity 6 (URP) about Vael heroes who sync with technology and shift between ground, flight and civilian forms. Project: `C:\Users\natem\Projects\SpaceRPG` (Unity project `Lattice/`); public repo `github.com/Ohallik/Coronach`, branch `main`. Nathan is the creator. The goal is a genuinely great game, not green tests. If something looks or feels poor while tests pass, keep iterating. Make routine decisions yourself (D157) and record consequential ones in `docs/DECISIONS.md` (next free number: D232).

## Read first

1. `docs/PROGRESS.md`, newest entry first (November 10-11: the open Minors and the plan to finish; then November 6-7: the QA pass, with its summary), then `docs/WORKSHOP_NOV07.md`, `docs/qa/ISSUES.md` (open items first among the Minor rows), `docs/TEST_PLAN.md`, `docs/qa/PLAN_COVERAGE.md` and `docs/validation/QA-nov07-chain.md`.
2. `docs/DECISIONS.md` D223 (the shipping scope) and D224-D231 (the open Minors), D203-D222 (the QA pass), then D200-D202 (script, skills, chapter 2) and D197-D199 (gait, bosses, side stories). For a new zone or chapter: D119 (fixed-camera sightlines), D191 (room dressing and `SightlineAudit`), D196 (exteriors), D060 (no blur), D131 (paid packs stay private), D154 (display-off stall).
3. `docs/PRODUCTION_PLAN.md`: milestones, evidence rules, MAP_EYE_TEST.
4. As needed: `docs/STORY_CAMPAIGN.md`, `docs/WORLD_ATLAS.md`, `docs/maps/*.md`, `docs/validation/*.md`. Older detail is in git history.

## Session-start safety (do this before any Unity run)

- **Saves:** Nathan's saves live in `%USERPROFILE%\AppData\LocalLow\Nathan\Lattice\Saves` (`autosave.json` sha256 `a87ab0b0…`, `autosave.json.bak` `5f7218db…`, unchanged through the QA pass). Hash them before and after every big run.
- **Has Nathan played?** Nathan plays either `Builds/WindowsDev` (those saves; log `LocalLow\Nathan\Coronach\Player.log`) or the Workshop-Nov07 package, which keeps its own saves in `Builds/Workshop-Nov07/Profile` and logs to `Builds/Workshop-Nov07/Logs/play-*.log`. If those hashes change or those folders appear, Nathan has played: read that log first.
- **Interrupted runs:** if a `Builds/logs/playmode-human-saves-backup-*` folder exists and no Unity or Coronach process is alive, a run was interrupted. Move what the run left in the live `Saves` folder aside first (it holds test saves), then move the stash back as `Saves` and check the hashes.
- **Processes:** only one Unity may own `Lattice/`. Never stop Frostbound or other unrelated Unity processes.
- **Git:** Unity re-adds the embedded grass entry to `Lattice/Packages/packages-lock.json` on every run; revert it before committing. A test run also rewrites `Resources/WorldMaterials/staged-light-line.mat`'s `_Color`: revert that too. Never commit `Builds/`, `Library/`, `art-src/`, `tools/meshy/.env`, `Lattice/Assets/Stylized Water 3/`, `Lattice/Packages/xyz.staggart-creations.stylized-grass/`, `Lattice/Assets/_Project/LocalStaggart/`, the `Lattice/Assets/InitTestScene*` an interrupted test run leaves (not ignored), or a throwaway render test (name such tests `QaDebug*` and delete them after use). Check the staged paths before every commit, write the message to a file and use `git commit -F`, and commit and push as each gate completes.

## Where things stand

- The whole game, Title to Abri's homecoming (chapters 1 and 2), plays through on the replay driver: 16 routes, `chapter-01-new-game-decks` to `chapter-16-lantern-home`. Routes 12 to 16 are written by `python tools/lantern_routes.py`: change the generator and rerun it, never the JSON by hand.
- **196 EditMode / 1,449 PlayMode** checks. The last full run was November 7 (195 and 1,413; 1,412 passed). On November 11, EditMode passed 196 of 196, and the six new `QaNov10*` PlayMode classes (36 cases) with the existing classes that work touched passed 147 of 147; the full suite hasn't been rerun since. `CompanionTravelTests.TarenRegroupsAroundTheWholeAnimal` is intermittent: it failed the same way on November 6 and in the final full run (its fixture flies the leader straight through the Cantor, and the leader can stick on the body), and passed when its class was rerun alone. If it recurs, steer the fixture's leader round the animal; never loosen it.
- **The QA pass's log** has 655 findings: 649 fixed (26 of the 191 Majors much better rather than gone), 5 Minor open, one by design, and 34 ideas (26 built). `ff99a7a` (November 11) closed 34 of the 39 that were open or partial; nothing in it has been seen in a capture yet.
- **Nathan's current build:** `Builds/Workshop-Nov07/Play Coronach.cmd` (source `a8247e6`, own profile), and `Builds/WindowsDev` from the same source. Earlier packages must stay byte-identical to their `player-manifest.json`.
- **The final uncaptured chain** (16 routes, displays awake) took 61:13 of accepted segments, resumed once at chapter 15 ([chain](validation/QA-nov07-chain.md)). Median frame 16.7 ms, p95 17.09 ms over the chain (four segments at 17.11 to 17.12 ms), and seven 34 to 46 ms frames at menus and saves (QA-034). Boss fights in that chain: Burrower 140 s, Bellows 126 s, Cantor 160 s, Hessa 132 s (`python tools/boss_durations.py Builds/quality/QA/final`).
- **Waiting on Nathan:** answers to TEST_PLAN §6, how fights feel on the Precision (now with what a guard costs, D224), listening, and the design calls: a cap on Repair Gels (prog159) and a chapter 2 scrip sink (prog160).
- Meshy live balance 2,372.

### Systems at a glance

- **Skills (D201).** `SkillBook` (Core) owns learned skills, the four-slot loadout and mastery; `CombatActor.Skill` dispatches on `SkillDef.kind`; learned kinds live in `HeroSkills` (Combat).
- **Names (D200).** `DisplayNames` turns speaker, item, zone and target ids into words; never print a raw id.
- **Bosses (D198, D206).** `BossController` is partial per boss; `Telegraph` markings (shader `Resources/Effects/Telegraph.shader`) decide every strike; a marking under alpha .99 is a boundary (D222). Boss integrity: Burrower 42000, Bellows 35000, Cantor 55000, Hessa 48000.
- **Saves and losses (D204).** A boss door is a checkpoint and saves; "Return to last autosave" brings the party back whole.
- **Camera (D212, D216, D221).** The fixed camera moves only its look-at and distance; the flight view's pull and come-in speeds are capped.
- **The first morning (D195).** Only a Title New Game sets `opening.marketRequested`; `MarketOpeningStaging` builds the Decks bridge scene.
- **Side stories (D199).** Steps advance on `ObjectiveKind.Flag` from `<<setFlag>>`; Sorrel's beacons are staged by `SideStoryStaging.Refresh`.
- **Balance (D208).** 125 XP a level; the main path ends chapter 1 near Sync 7 and chapter 2 at Sync 8 to 10.

## What to do next, in order

0. **Read Nathan's verdict on Workshop-Nov07** (the log of whichever build Nathan played, and anything Nathan writes) and tune from Nathan's words before adding more. Hessa: the replay driver lost 1 of 3 in the final chain and 4 of 10 since her November 6 retune, which only a person on a pad can judge.
1. **The five Minors still open in ISSUES.md**, each with what `ff99a7a` tried: the canyon's cliff-foot teeth, halved by half-metre cells and not gone (E1-lanternwick-13); the Burrower's wreck, which got smoke and nothing else (E2-hushwell-16); and three that only a chain can close. Those are the menu and save hitches (QA-034: the next timing chain must show no frame over 33 ms at those steps), the replay's revive step (QA-035: watch chapter 15), and the shutdown crash (item 3). The next capture is also the first look at everything `ff99a7a` changed, and the place to re-measure boss lengths and Hessa, since the partner now fires less (D227) and a guard has a price (D224). In `ff99a7a` the Choir Emitter's Resonance +4 doesn't reach a fight (D225). The haul road's sameness is an open idea.
2. **Still open from before the QA pass:**
   - **Sela's uphill fold (both heroes fold).** Re-pose uphill walking in `GroundFeet` in the gravity frame: an upright torso, slope-aware foot targets, a trailing heel that lifts at toe-off. Don't retune parameters: five variants broke the planted-sole and knee-continuity checks (`docs/validation/C2-uphill-fold.md`). `Lattice.EditorTools.UphillStepAudit.Run` (marker `UPHILL_STEP_AUDIT`) is the acceptance check, OPEN at 8 of 8.
   - **Town life.** Generated Decks residents on `ScriptedWalk` loops, resident interiors, and a measured stride profile for Mira's walk. Lanternwick's townsfolk came in the QA pass.
   - **An Ys-style EXTRA move.** None exists yet.
3. **The engine's shutdown crash** (QA-031): an access violation in `D3D12Core.dll` while quitting, twice in about 160 exits. Every quit now goes through `OrderlyQuit` (D230), which should make it rarer and can't cure it: count the exits of the next chains. If it reaches Nathan, try the next Unity 6000.4 patch before changing graphics API.
4. **Chapter 3:** Xylos and Many Hands (food, Venn's first condition), per the atlas, with blockouts before art. Chapter 2 ends with Abri pointing there.
5. After a meaningful batch: the full suites, rebuild both players, rerun the chain, publish `Builds/Workshop-<date>` with a short note for Nathan, update PROGRESS and this file, commit and push.

**Evidence proportionality.** Keep the hard rules: prove a new check fails on the broken state first, never loosen an assertion, keep failed evidence, and never claim unobserved quality (human play, controller feel, listening, continuous motion). But keep validation notes short, and spend time on what a player sees and feels. Earlier sessions spent whole days on single props and evidence bookkeeping while the maps stayed empty.

## Commands that work

```powershell
# compile check
.\scripts\headless.ps1 -Mode verify
# type-check the ten assemblies with Unity's own compiler without starting Unity, in about nine seconds (--player for the player's defines); the compile check above is still the gate
python tools/typecheck.py
# the whole suite: EditMode, then about 1,400 PlayMode tests in three parts by class (about 2.5 hours). Run it in the background
# and start no other Unity work. Never run PlayMode unfiltered under one time limit: a timeout loses every result and strands the saves.
.\scripts\full-suite.ps1 -Label <name>
python tools/suite_results.py <name>
# focused runs
.\scripts\headless.ps1 -Mode tests -TimeoutSec 900
.\scripts\headless.ps1 -Mode playtests -Graphics -TestFilter "Lattice.Tests.PlayMode.A|Lattice.Tests.PlayMode.B" -TimeoutSec 2400
# editor methods: call with & from PowerShell (powershell -File cannot pass arrays); -Marker is the method's final log line,
# and the method's own arguments go through -ExtraArgs
& .\scripts\exec.ps1 -Method Lattice.EditorTools.ZoneLook.Apply -Marker ZONE_LOOK_OK -ExtraArgs @('-look-zones','WickReach,Lanternwick')
& .\scripts\exec.ps1 -Method Lattice.EditorTools.LookDev.Render -Marker LOOKDEV_OK -Graphics -ExtraArgs @('-look-run','<new-name>','-local-biomes','-look-preview','-look-zones','Hub_Decks,Sorrel_Ridges')
.\scripts\look.ps1 -Folder Builds/logs/look/<new-name>          # in-player stills, dev player
# players (move the previous Builds/Windows* into Builds/quality/... first)
.\scripts\headless.ps1 -Mode build -TimeoutSec 3600 ; .\scripts\headless.ps1 -Mode builddev -TimeoutSec 3600
# the whole-game chain. Without -Routes it runs chapter 1 only. It takes about an hour, far past the 10-minute tool limit: run it in
# the background and issue nothing at all until it reports (player windows reject on focus loss).
.\scripts\quality-chain.ps1 -Output Builds/quality/<run> -Routes chapter-01-new-game-decks,chapter-02-continue-sorrel,chapter-03-defeat-retry,chapter-04-haul-preparation,chapter-05-drill-equipment,chapter-06-burrower,chapter-07-enter-hushwell,chapter-08-hushwell-nursery,chapter-09-calibrated-gullet,chapter-10-gullet-evasive,chapter-11-tallow-reading-return,chapter-12-wick-reach,chapter-13-lanternwick,chapter-14-lantern-gorge,chapter-15-sunken-relay,chapter-16-lantern-home [-Capture] -Build Release
# to resume: -ResumeFrom Builds/quality/<run>/sNN -FirstNumber <NN+1> -Routes <only the routes after sNN>
python tools/boss_durations.py Builds/quality/<run>
python tools/quality_analyze.py Builds/quality/<run>/sNN
python tools/eye_sheet.py Builds/quality/<run>/sNN
python tools/package_workshop.py Workshop-<name> --revision <sha>
```

**Rebuild order after builder changes** (each `& .\scripts\exec.ps1 -Method Lattice.EditorTools.<method> -Marker <marker> ...`):

| Step | Method | Marker | Arguments |
|---|---|---|---|
| after any `.yarn` change | `LanternRoadBuilder.Dialogue` | `LANTERN_DIALOGUE_OK` | |
| new or repainted generated art | `GenIntake.BuildPrefix` | `GEN_INTAKE_OK` | `-ExtraArgs @('-intake-prefix','<ids>')` |
| chapter 2 zones | `LanternRoadBuilder.BuildZones` | `LANTERN_ZONES_OK` | `-Graphics -ExtraArgs @('-lantern-zones','<zones>')` |
| their light | `ZoneLook.Apply` | `ZONE_LOOK_OK` | `-Graphics -ExtraArgs @('-look-zones','<zones>')` |
| chapter 1 staging | `ChapterOneStaging.Apply` | `CHAPTER_ONE_STAGED_OK` | `-Graphics` |
| chapter 1 dressing | `RoomDressing.Apply` | `ROOM_DRESSING_OK` | `-Graphics -ExtraArgs @('-dress-zones','<chapter 1 zones>','-dress-final')`; it knows chapter 1's seven zones only, and a refused zone is left unsaved (read `DRESSING_PROBLEM`) |
| staging again, on the final dressing | `ChapterOneStaging.Apply` | `CHAPTER_ONE_STAGED_OK` | `-Graphics` |
| the November 10 looks, after staging (D231) | `QaNov10Staging.Apply` | `QA_NOV10_STAGED_OK` | `-Graphics`; `-ExtraArgs @('-qa-zones','<zones>')` limits it to Hushwell, Gullet_Tunnel, Sorrel_Ridges or TallowApproach |

After a rebuild of the Decks scene itself, rerun `MarketOpeningStaging.Refresh` (`MARKET_OPENING_STAGING_OK`, `-Graphics`); after a rebuild of Sorrel itself, `SideStoryStaging.Refresh` (`SIDE_STORY_STAGING_OK`). Then delete `Resources/WorldMaterials/gullet-membrane-23-1.mat*` (building the relay's seal makes it, and nothing uses it) and revert `packages-lock.json`.

## Ground rules

- **Displays:** with the displays dark the player runs at about 250 ms a frame and routes fail. For a timing or capture chain, hold them awake from the chain's own PowerShell call (a one-pixel mouse move, then `SetThreadExecutionState([uint32]2147483651)`, before `quality-chain.ps1`); the detached `keep-displays-awake.ps1` has died mid-chain more than once. Check `frames.csv`: the median should be 16.7 ms.
- **Rejections that aren't the game:** "Listener capture queue overflow" is the capture tool's audio writer falling behind (QA-033), and an exit code of -1073741819 after `QUALITY_REPLAY_OK` is the engine's shutdown crash (QA-031). Set the folder aside and resume the chain from the last good segment.
- Player-window runs (replays, look, smokes) reject on focus loss: run each alone, with nothing else issued during it, not even a trivial shell command.
- Use isolated builder entry points only.
- Run Meshy image tasks one at a time: parallel tasks race on `docs/art/gen-manifest.json` and drop rows.
- `-Capture` runs write a PNG per step; their frame times aren't comparable with uncaptured runs.
- Keep PROGRESS newest first and validation notes current.
