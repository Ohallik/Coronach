# Coronach - continuation prompt (November 12, 2026, during the finishing run)

> **For another QA pass, use [TESTER_PROMPT.md](TESTER_PROMPT.md)**, which hands the game to a tester session that plays through the replay driver and fixes what it finds, following [TEST_PLAN.md](TEST_PLAN.md). The November pass's log is [qa/ISSUES.md](qa/ISSUES.md). Take commands and state from this file.

You're continuing development of **Coronach**, an HD-2D action RPG in Unity 6 (URP) about Vael heroes who sync with technology and shift between ground, flight and civilian forms. Project: `C:\Users\natem\Projects\SpaceRPG` (Unity project `Lattice/`); public repo `github.com/Ohallik/Coronach`, branch `main`. Nathan is the creator. The goal is a genuinely great game, not green tests. If something looks or feels poor while tests pass, keep iterating. Make routine decisions yourself (D157) and record consequential ones in `docs/DECISIONS.md` (next free number: D248).

## Read first

0. `docs/SHIPPING_PLAN.md` (the ten-chapter scope, D223, and its progress table) and `docs/CHAPTER_GUIDE.md` (how a chapter is built, and section 0: how several engineers share the tree and Unity). Each chapter's brief is in `docs/chapters/`, its script in `docs/writing/` or `docs/writing/pending/`.
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

### The finishing run (November 11 onward)

Nathan's instruction on November 11: finish the game without check-ins, fix rather than defer, and leave only music for Nathan (six cues, `docs/music/NEW_TRACKS.md`). The work is run by a lead with engineers as subagents, all in one working tree. State lives in `docs/SHIPPING_PLAN.md` section 7; read that table first.

- **Committed:** the plan, the eight chapter briefs, every chapter's script (chapter 3 live, 4 to 10 in `docs/writing/pending/` until their chapter is defined), every speaker's portrait sheet, the six music seeds, the shared-Unity tools and the test mirrors (D247).
- **In the working tree, uncommitted** (snapshots of it are kept under `refs/snapshots/*`; `git show-ref | findstr snapshots`): the systems pass (impact, loot, late skills and EXTRA moves, route chart, difficulty, chapter cards, ground kit, content-by-data framework, the replay driver's new hands), chapter 3's zones and combat, chapters 4 to 6's art, Idra's bodies. None of it is committed until a whole suite passes on a frozen copy and the regressions it found are closed.
- **A killed or interrupted session** leaves engineers stopped mid-task with their work on disk. Resume each by its agent id with a message rather than starting it again, check `Builds/quality/focused/` and `Builds/quality/suite/` for results that finished meanwhile, and take a new snapshot before changing anything.
- **Not yet done in this run:** no player-window run (no replay chain, no in-player stills) has been made since November 7, so nothing built in this run has been seen in a capture; boss durations for the new fights are estimates from tests.

### The last committed build

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

1. **Close the systems pass.** Read the latest whole-suite result (`python tools/suite_results.py <label>`, newest label in `Builds/quality/suite/`), send each real regression to the code that caused it, rerun, and commit the systems pass when a frozen-copy suite is clean apart from content a chapter in progress still owes.
2. **Chapter 3** (Xylos): finish the look pass on the approach and the town, the river and the Canopy with their boss arenas, tests and routes; then a sync point (below).
3. **Chapters 4 to 10 in story order**, each by the guide: definitions, art bind, zones, combat, tests, routes, a look review of stills by the lead, and its script moved up from `docs/writing/pending/`. Chapters may be built side by side; they are played and committed in order.
4. **A sync point after each chapter:** every engineer idle, then alone on the machine with the displays held awake: rebuild both players, run the whole-game chain through that chapter, look at the captures frame by frame, re-baseline boss durations, fix what the captures show, commit, push, snapshot.
5. **Systems still owed:** Idra as the third hero (kit, party of three, chapter 6), town life and the uphill gait (S5), the four Hushwell stills still over the floor limit and the decal draw order over danger markings, and the Minors still open in `docs/qa/ISSUES.md`.
6. **The end:** a whole-game QA pass on the replay driver (TESTER_PROMPT), a Workshop build with a note for Nathan, and the six tracks wired in once Nathan has made them in Suno.

**Evidence proportionality.** Keep the hard rules: prove a new check fails on the broken state first, never loosen an assertion, keep failed evidence, and never claim unobserved quality (human play, controller feel, listening, continuous motion). But keep validation notes short, and spend time on what a player sees and feels. Earlier sessions spent whole days on single props and evidence bookkeeping while the maps stayed empty.

## Commands that work

```powershell
# compile check
.\scripts\headless.ps1 -Mode verify
# type-check the ten assemblies with Unity's own compiler without starting Unity, in about nine seconds (--player for the player's defines); the compile check above is still the gate
python tools/typecheck.py
# the whole suite: EditMode, then about 1,600 PlayMode tests in eight parts, on one frozen copy of the tree in test mirror S
# (..\CoronachTest\S, D247). About 2.5 hours, in the background, beside other work. -From n resumes at part n.
.\scripts\suite-shared.ps1 -Label <name>
python tools/suite_results.py <name>
# focused runs: copies the tree to a free test mirror (A or C) and runs there; results in Builds/quality/focused/<label>-*.xml
.\scripts\focused.ps1 -SkipVerify -Label <label> -Classes 'ATests|BTests' [-EditMode]
# a test mirror is made or refreshed with .\scripts\mirror-sync.ps1 -Name A (about 6 GB the first time). Never run PlayMode in the
# main project unless you must (-Main): that path moves the human saves aside and a timeout strands them.
# builders, renders and intake, in the main project: waits for a tree that compiles and for its turn at Unity
& .\scripts\exec-when-green.ps1 -Method Lattice.EditorTools.<Method> -Marker <MARKER> [-Graphics] [-TimeoutSec n] [-ExtraArgs @(...)]
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
| the ground (D246), always last | `GroundStaging.Apply` | `GROUND_STAGED_OK` | `-Graphics`; see the guide's "Ground kit" for its zone argument and `GroundStaging.Audit` |
| local biomes, after the ground | `LocalBiomeBake.BuildPatches` | `LOCAL_BIOMES_OK` | `-Graphics -ExtraArgs @('-biome-zones','<zones>')`; needs the licensed packs on this machine |
| chapter 3 | `XylosBuilder.Dialogue`, `.BuildZones`, `.StageApproach` | `XYLOS_DIALOGUE_OK`, `XYLOS_ZONES_OK`, `XYLOS_APPROACH_OK` | the order is in the guide, section 12; later chapters follow the same names |

After a rebuild of the Decks scene itself, rerun `MarketOpeningStaging.Refresh` (`MARKET_OPENING_STAGING_OK`, `-Graphics`); after a rebuild of Sorrel itself, `SideStoryStaging.Refresh` (`SIDE_STORY_STAGING_OK`). Then delete `Resources/WorldMaterials/gullet-membrane-23-1.mat*` (building the relay's seal makes it, and nothing uses it) and revert `packages-lock.json`.

## Ground rules

- **Displays:** with the displays dark the player runs at about 250 ms a frame and routes fail. For a timing or capture chain, hold them awake from the chain's own PowerShell call (a one-pixel mouse move, then `SetThreadExecutionState([uint32]2147483651)`, before `quality-chain.ps1`); the detached `keep-displays-awake.ps1` has died mid-chain more than once. Check `frames.csv`: the median should be 16.7 ms.
- **Rejections that aren't the game:** "Listener capture queue overflow" is the capture tool's audio writer falling behind (QA-033), and an exit code of -1073741819 after `QUALITY_REPLAY_OK` is the engine's shutdown crash (QA-031). Set the folder aside and resume the chain from the last good segment.
- Player-window runs (replays, look, smokes) reject on focus loss: run each alone, with nothing else issued during it, not even a trivial shell command.
- Use isolated builder entry points only.
- Run Meshy image tasks one at a time: parallel tasks race on `docs/art/gen-manifest.json` and drop rows.
- `-Capture` runs write a PNG per step; their frame times aren't comparable with uncaptured runs.
- Keep PROGRESS newest first and validation notes current.
