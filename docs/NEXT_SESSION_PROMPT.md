# Coronach - continuation prompt (November 5, 2026, after the script pass, the skill system and chapter 2)

> **For a QA pass, use [TESTER_PROMPT.md](TESTER_PROMPT.md) instead.** It hands the game to a tester session that plays through the replay driver and fixes what it finds, following [TEST_PLAN.md](TEST_PLAN.md).

You're continuing development of **Coronach**, an HD-2D action RPG in Unity 6 (URP) about Vael heroes who sync with technology and shift between ground, flight and civilian forms. Project: `C:\Users\natem\Projects\SpaceRPG` (Unity project `Lattice/`); public repo `github.com/Ohallik/Coronach`, branch `main`. Nathan is the creator. The goal is a genuinely great game, not green tests. If something looks or feels poor while tests pass, keep iterating. Make routine decisions yourself (D157) and record consequential ones in `docs/DECISIONS.md`.

## Read first

1. `docs/PROGRESS.md`, newest entry first (November 5: script pass, skills, chapter 2), then `docs/WORKSHOP_NOV05.md`, `docs/CONTENT_AUDIT_NOV05.md`, `docs/TEST_PLAN.md`, `docs/validation/C7-nov05-chain.md`; older context in `docs/WORKSHOP_NOV04B.md` and `docs/validation/C8-nov04b-bosses-sides.md`.
2. `docs/DECISIONS.md` D200 (script and display names), D201 (skills learned by level, loadout, mastery), D202 (chapter 2, the Lantern Road), D197-D199 (gait, bosses, side stories), then D186-D196 (D191 room dressing and `SightlineAudit`, D192 detail layer, D193 Hushwell/Gullet dressing, D194 skills and zone threat, D195 the first morning, D196 exteriors, coil, reel) (and D060 no-blur, D119 fixed-camera sightline rule, D127/D131 paid packs, D154 display-off stall).
3. `docs/PRODUCTION_PLAN.md`: milestones C1-C10, evidence rules, MAP_EYE_TEST.
4. As needed: `docs/STORY_CAMPAIGN.md`, `docs/WORLD_ATLAS.md`, `docs/maps/*.md`, `docs/validation/*.md`. The previous, much longer prompt is in git history if you need older detail.

## Session-start safety (do this before any Unity run)

- **Saves:** Nathan's saves live in `%USERPROFILE%\AppData\LocalLow\Nathan\Lattice\Saves` (`autosave.json` sha256 `a87ab0b0…`, `autosave.json.bak` `5f7218db…` since Nathan's own play on November 4; he plays `Builds/WindowsDev` directly and his log is `LocalLow\Nathan\Coronach\Player.log`). If any `Builds/logs/playmode-human-saves-backup-*` folder exists, a previous run was interrupted. With no Unity/Coronach process alive, compare hashes and move it back. This happened on November 3. Hash the live files before and after every big run.
- **Processes:** only one Unity may own `Lattice/`. Never stop Frostbound or other unrelated Unity processes.
- **Git:** Unity re-adds the embedded grass entry to `Lattice/Packages/packages-lock.json` on every run; revert that file before committing. Never commit `Builds/`, `Library/`, `art-src/`, `tools/meshy/.env`, `Lattice/Assets/Stylized Water 3/`, `Lattice/Packages/xyz.staggart-creations.stylized-grass/` or `Lattice/Assets/_Project/LocalStaggart/`. Check staged paths.

## Where things stand

- The playable game runs Title → the first morning on the Decks → Sorrel → Hushwell → Halo → Gullet → Tallow (chapter 1), then Abri's lantern tale → the Wick Reach → Lanternwick → Lantern Gorge → the Sunken Relay (Hessa Korl) → Aumi's chart → home to Abri (chapter 2, D202).
- **Chapter 2 has no replay routes yet** (`chapter-12` … `chapter-16` are planned in TEST_PLAN §5). Its zones are built by the isolated `Lattice.EditorTools.LanternRoadBuilder` (`Build` for all four plus the Tallow approach warp and dialogue refresh; `BuildZones -lantern-zones Lanternwick,...`; `Dialogue` after any new `.yarn`), then `ZoneLook.Apply -look-zones WickReach,Lanternwick,LanternGorge,SunkenRelay`. Generated pieces come from `tools/meshy/c9_lantern_batch.py` and `tools/c9_intake.py`, staged with `GenIntake.BuildPrefix`.
- **Skills (D201).** `SkillBook` (Core) owns learned skills, the four-slot loadout (`MemberState.skillLoadout`) and mastery (`skillUses`); `CombatActor.Skill` dispatches on `SkillDef.kind`; the eight learned kinds live in `HeroSkills` (Combat). The pause menu's page constants are `PauseMenu.PartyPage` … `SettingsPage`.
- **Names (D200).** `DisplayNames` turns speaker, item, zone and target ids into words; never print a raw id.
- The New Game → Tallow chain passes in 44:51 with the displays awake (`docs/validation/C7-nov05-chain.md`); no frame over 33 ms outside loads.
  - Hold the displays awake for timing runs with a display-required execution state (a small PowerShell `SetThreadExecutionState(0x80000003)` loop; stop it afterwards). It cannot wake displays that are already off: frames alternating 2/265 ms mean they are; a one-pixel `SendInput` mouse move wakes them (November 5).
- **194 EditMode / 516 PlayMode** pass (November 5).
- **Nathan's current build:** `Builds/Workshop-Nov05/Play Coronach.cmd` (source `009018d`, own profile).
  - `Workshop-Nov04b` (`a2929dd`), `Nov04` (`f931c48`), `Nov03d`, `Nov03c`, `Nov03b`, `Nov03` and the frozen `Builds/Workshop-Chapter` (`a3aaad6`) must stay byte-identical to their `player-manifest.json` (hashes are of the files under `Player/`).
- **Feel (D197).** Civil movement: jog 4.5 m/s at full tilt, walk 1.5 under half tilt, sprint 6.6 without Thrust (`GroundMotor.Natural*`). `GroundMotor.Steer` carries pace around turns. `GroundStrideProfile.RunCadence` shortens the jog's step instead of slowing it. Gait threshold `GeneratedAnimator.RunAbove` is 2.5 m/s. The walking bend reserve fades on steep support. `GroundFeet.Lean` leans and banks free running.
- **The opening answers (D197).** `Npc.holdNode` gives Orrin, Mira and Sela holding lines until the walkway is repaired. The repair is offered once Sela sets off (`ScriptedWalk.walkThroughDialogue`).
- **Bosses (D198).**
  - `BossController` is partial per boss (`.Burrower/.Bellows/.Cantor.cs`); `Perform(move)` starts a named special for tests.
  - `Telegraph` markings (circle, ring, cone, lane; shader `Resources/Effects/Telegraph.shader`) decide every strike.
  - Phase shifts, `Health.Expose`, a boss bar and title card in `GameHud`.
  - Pads and warps wait during a nearby engaged fight. `ZoneDef.cameraClearsScenery` (Sorrel, Hushwell) slides the ground camera past blocking scenery.
  - Cantor's new moves follow integrity only, because its collar tests pin the choir rhythm.
- **Side stories (D199).**
  - `SideStories` routes beats; steps advance on `ObjectiveKind.Flag` from `<<setFlag>>`.
  - Quests: `NeveParcel`, `SorrelBeacons` (beacons staged by isolated `SideStoryStaging.Refresh`; rerun after any Sorrel rebuild), `OdaBerth`, and the Keeper's `CompactHail`.
  - Rewards route by kind. `QuestDef.stepText` feeds the quest log and `QuestToast`.
- **Replay driver.** `QualityStep.ensureCharacter` presses Swap until that hero leads.
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
- Meshy live balance 2,635 (November 5).
- Paid Staggart water and grass stay a private, ignored layer with public fallbacks (D131).

## What to do next, in order

0. **Watch for Nathan's verdict on Workshop-Nov05** (chapter 2, Hessa's difficulty, the new skills, the rewritten lines) and on Nov04b's gait and Precision feel. Tune from his words before adding more.
0a. **QA pass ([TESTER_PROMPT.md](TESTER_PROMPT.md)).** Write the chapter 2 replay routes, then work TEST_PLAN §4 and fix what it finds. The Burrower's last phase wiped the replay party once on November 5 (`Builds/quality/C7/nov05/s06`, kept); `BossFightTests.TheWoundedBurrowerFightsWithItsWholeSet` is random and failed once.
0b. **Chapter 2 polish.** Ordinary Lanternwick residents; an Ys-style EXTRA move; Hessa's own animation; a second look at Lanternwick's market density once a person has walked it.
1. **Bosses, next layer.** Boss animation is still staged with existing bodies (the Burrower rears and lunges procedurally). Consider an optional chapter-1 boss reusing generated art (for example, a Scrapmite matriarch) and Bellows/Cantor in-player eye tests like the Burrower's three.
2. **Sela's uphill fold (both heroes fold).**
   - Re-pose uphill walking in `GroundFeet` in the gravity frame: an upright torso, slope-aware foot targets, and a trailing heel that lifts at toe-off.
   - Do not retune parameters; five variants broke the planted-sole and knee-continuity checks (`docs/validation/C2-uphill-fold.md`).
   - `Lattice.EditorTools.UphillStepAudit.Run` is the acceptance check; it is OPEN at 8 of 8 on the committed walk.
3. **Town life.**
   - Resident interiors and town/service loops need ordinary resident bodies. Generate 3-4 Decks residents with the Meshy character pipeline (rigged to the donor, a measured stride profile each), then give them `ScriptedWalk`-style loops on the public and service routes.
   - Mira's walk back to her bay has no measured stride profile.
4. **Places.**
   - The Cantor preview's in-player eye test (the coil now rests coiled; the moorings keep their rectangle).
   - The refuge hull's own texture is soft in the closest dock view.
5. **Presentation.**
   - The one 45 ms frame entering Sorrel's Scrapmite wash (likely first spawn); the 50 ms Tallow dialogue frame did not recur.
   - C1's remaining evidence: release first/warm laps, development stability, uncapped headroom, a person watching the motion.
   - Check that the displays are awake before timing runs (D154).
6. After a meaningful batch: full suites, rebuild both players, rerun the chain, publish `Builds/Workshop-<date>` with a short note for Nathan, commit and push.
7. Then chapter 3: Xylos and Many Hands (food, Venn's first condition), per the atlas, with blockouts before art. Chapter 2 ends with Abri pointing there.

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
