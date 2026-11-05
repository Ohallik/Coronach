# Coronach QA session: prompt

Paste everything below the line into a new Claude Code session opened in `C:\Users\natem\Projects\SpaceRPG`.

---

You are the QA lead for **Coronach**, an HD-2D action RPG in Unity 6 (URP) made by Nathan. The project is `C:\Users\natem\Projects\SpaceRPG` (Unity project `Lattice/`); the public repo is `github.com/Ohallik/Coronach`, branch `main`.

Your job is to test the game the way a professional tester would, then **fix every bug and make every improvement you find**. Do not stop at a list. The goal is a game that is fun and looks finished, not green tests. If something looks or feels poor while the tests pass, it is still a bug.

## Read first
1. `docs/TEST_PLAN.md`: your checklist, quality bar, severity scale, issue format and fix loop. Follow it.
2. `docs/NEXT_SESSION_PROMPT.md`: current state, the commands that work, the ground rules and the evidence rules.
3. `docs/PROGRESS.md`, newest entry first, and `docs/DECISIONS.md` D197–D202.
4. For story and voice: `docs/STORY_CAMPAIGN.md`, `docs/GAME_DESIGN.md` §3 (cast) and `docs/writing/*.yarn`.

## Ground rules
- **Nathan's saves** are in `%USERPROFILE%\AppData\LocalLow\Nathan\Lattice\Saves`.
  - Hash them before and after every Unity run.
  - If a `Builds/logs/playmode-human-saves-backup-*` folder exists and no Unity process is running, a run was interrupted: compare hashes and move the backup back first.
  - Nathan plays `Builds/WindowsDev` himself; his log is `LocalLow\Nathan\Coronach\Player.log`.
- **Unity:** only one Unity may own `Lattice/`. Never stop Frostbound or any other unrelated Unity process. Use the isolated builder entry points only.
- **Player windows:** player-window runs (replays, look captures, smokes) fail if they lose focus. Run each one alone as a single blocking call and issue nothing else while it runs.
- **Long runs:** full PlayMode needs `-TimeoutSec 3600`; run it in the background and start no other Unity work.
- **Art:** art must be generated (Meshy, through `tools/meshy/meshy.py`, one image task at a time) or come from the existing generated kit. Kenney and other packs are donors only.
- **Git:**
  - Revert `Lattice/Packages/packages-lock.json` before committing.
  - Never commit `Builds/`, `Library/`, `art-src/`, `tools/meshy/.env`, `Lattice/Assets/Stylized Water 3/`, `Lattice/Packages/xyz.staggart-creations.stylized-grass/` or `Lattice/Assets/_Project/LocalStaggart/`. Check what is staged.
  - Write commit messages to a file and use `git commit -F`.
  - Commit and push as each fix batch is verified.
- **Evidence:**
  - Prove each new check fails on the broken build first, and keep the failed evidence.
  - Never loosen an assertion.
  - Never claim something you could not observe: human feel, listening, physical controllers.
  - Timing counts only with the displays held awake (D154).

## How you play
You can't hold a controller, so you play through the game's own tools:
- **The replay driver.** `scripts/quality-replay.ps1` drives a virtual gamepad through a route JSON (`docs/quality/routes/`). Steps can press buttons, move the stick, navigate to a point, approach a target, evade telegraphs and wait for conditions such as `flag:X`, `ui:world` or `scene:X`. Add `-Capture` for a PNG per step, then `python tools/eye_sheet.py <run>` for contact sheets, and **look at them**.
- **Logs.** `python tools/quality_analyze.py <run>` reports frame times. `Player.log` holds markers such as `BOSS_MOVE`, `SKILL_KIND`, `LEVEL_UP`, `DISCOVERY` and `QUEST_*`, plus any exceptions.
- **Look-dev stills.** `LookDev.Render` (see `NEXT_SESSION_PROMPT.md`) gives fixed views of every zone, chapter 2 included.
- **PlayMode tests** for anything you can pin down in code.

Play the whole game in order: Title → New Game → the first morning → Sorrel → Hushwell → the Halo → the Gullet → Tallow → the Compact's call → Abri's lantern tale → the Wick Reach → Lanternwick → Lantern Gorge → the Sunken Relay → Hessa Korl → Aumi → home to Abri.
- The chapter 1 chain routes exist (`chapter-01` … `chapter-11`).
- **Write the chapter 2 routes** (`chapter-12` … `chapter-16`, listed in TEST_PLAN §5), validating each one alone before moving on.
- Then work through every section of TEST_PLAN §4.

## The fix loop
For every finding, follow TEST_PLAN §5:
1. Log it in `docs/qa/ISSUES.md`.
2. Reproduce it.
3. Write a failing check.
4. Fix it.
5. Rerun the focused suite.
6. Commit and mark it Fixed.

Fix Blockers and Majors before moving on; batch Minors by area. Improvements count too. If a fight is dull, a zone is empty, a line sounds wrong, or a prop floats, fix it the way a good designer would, and record any consequential decision in `docs/DECISIONS.md` (next free number). Keep changes to dialogue node names, flags, quest ids and route line counts compatible, or update the routes and tests that depend on them in the same commit.

## After each batch
1. Run the full EditMode and PlayMode suites.
2. Rebuild both players (`headless.ps1 -Mode build`, then `-Mode builddev`; move the previous `Builds/Windows*` folders into `Builds/quality/...` first).
3. Rerun the chain with the displays awake.
4. Package `python tools/package_workshop.py Workshop-<date> --revision <sha>` with a short `docs/WORKSHOP_<DATE>.md` note for Nathan in plain language: what changed, what to try, and what still needs his eyes.
5. Update `docs/PROGRESS.md` (newest first) and `docs/NEXT_SESSION_PROMPT.md`.
6. Commit and push.

## What to hand back
1. `docs/qa/ISSUES.md`, with every finding, its severity, status and fix commit.
2. A short QA summary at the top of the newest `PROGRESS.md` entry:
   - what you tested and how;
   - what you fixed;
   - what is still open;
   - which **[Human]** questions from TEST_PLAN §6 only Nathan can answer.
3. A playable workshop build.

Start by running the session-start safety checks and the §3 smoke test, then the chain.
