# Coronach test plan: fun and professional

Written November 5, 2026, for the build after the Lantern Road batch (chapter 1 plus chapter 2). It has two readers: a QA session that plays the game through the replay driver, and Nathan, who plays it by hand. Its question is whether the game is fun and finished-looking, not only whether it runs. Anything only a person can judge is marked **[Human]**.

## 1. What is under test

| Area | Content |
|---|---|
| Chapter 1, *A Repair with Nobody Home* | Title, first morning on the Decks, Cinder Halo, Sorrel Ridges (Burrower), Hushwell (Bellows Below), the Gullet (Cantor), Tallow Approach, Tallow Drift and the Compact's call |
| Chapter 2, *The Lantern Road* | Abri's lantern tale, the Wick Reach (space combat, lantern gate), Lanternwick (town), Lantern Gorge (open ground), the Sunken Relay (dungeon, Hessa Korl), Aumi's chart and the homecoming |
| Combat | 16 skills (8 per hero, 12 learned with Sync level), mastery, the companion's skills, 14 enemy types, 4 bosses, ground and flight |
| Systems | Sync levels (cap 40), gear and upgrades, fabrication, shops (Mira, Corra), 10 quests, objective HUD, quest log and cards, saves |
| Presentation | 18 portrait sets, about 320 lines of dialogue, barks, music by zone, generated art in every zone |

**Builds.** Nathan plays `Builds/Workshop-<date>/Play Coronach.cmd`, which uses its own profile. The QA session uses the release and development players (`Builds/Windows`, `Builds/WindowsDev`) through `scripts/quality-replay.ps1`.

**Save safety.** Nathan's saves live in `%USERPROFILE%\AppData\LocalLow\Nathan\Lattice\Saves`. Hash them before and after every run. A leftover `Builds/logs/playmode-human-saves-backup-*` folder means an interrupted run: restore it before anything else.

## 2. The quality bar

A build passes this plan only when every **Blocker** and **Major** item below is clear and each section's bar is met.

| Quality | What it means here | How it is checked |
|---|---|---|
| Responsive | Input to visible action within 2 frames at 60 fps; no input is eaten during dialogue, menus or zone loads, but for two holds on a thumb still tapping: Interact waits .35 s after any screen hands the world back, and a shop, a bench or a choice holds its first press .3 s | Replay traces, `frames.csv`, PadBridge log lines |
| Readable | Every enemy attack is marked before it lands; every objective names a place or person; no text overflows; no raw ids on screen | Eye sheets, `ReadableTypeTests`, `DialogueLayoutTests`, this plan's UI checks |
| Fair | Any hit that lands was marked and dodgeable; a boss can be beaten without taking an unavoidable hit; defeat costs at most 2 minutes of replay | Boss checklists (§4.4), defeat-retry routes |
| Paced | A new skill, zone, boss or story beat roughly every 15–25 minutes; no stretch of walking longer than 30 s with nothing to see or do | Chain timings, the pacing log (§4.3) |
| Coherent | Characters sound like themselves; the story's cause and effect is clear without a codex; side quests leave something behind | Script read-through (§4.6) **[Human]** for tone |
| Polished | No placeholder geometry (`ART_PENDING`), no floating or sunken props, no visible seams, no clipping through walls, lighting that suits the place | Look captures, eye sheets, `SightlineAudit` |
| Stable | No exceptions in `Player.log`; no soft lock; no frame over 33 ms outside loads in a full chain | `quality_analyze.py`, log scan |

**Severity.**
- **Blocker:** a crash, soft lock, lost save, or a main path that can't be finished.
- **Major:** wrong or confusing behaviour on the main path; an unfair or unreadable attack; text or art that looks broken; a broken side quest.
- **Minor:** polish (timing, wording, a prop slightly off).
- **Idea:** a change that would make the game more fun but isn't a defect.

## 3. How testing runs

1. **Smoke** (10 minutes, every build): Title → New Game → first repair → Orrin; Continue → the Halo → a dock; open every pause tab; quit. No exceptions in the log.
2. **Chain** (about 45 minutes for chapter 1): the eleven `chapter-*` routes in order, with displays awake (D154). Chapter 2 routes `chapter-12` onward are added during the QA pass (§5).
3. **Focused passes** (§4): one area at a time, with the replay driver plus `-Capture` and `tools/eye_sheet.py` contact sheets.
4. **Human pass** **[Human]**: Nathan plays the workshop build start to finish on his Logitech Precision and answers the questions in §6.

Every bug follows the loop in §5: reproduce, write a failing check, fix, rerun, commit.

## 4. Test areas

### 4.1 First minutes
- [ ] The title screen shows the logo, New Game, Continue, Settings and Quit, with its hints in the pad's words when a pad is connected. Save slots read "Autosave — The Decks — Sync 3", never raw ids, and the picker opens on the newest.
- [ ] The first morning: the player can reach the walkway job without a tutorial wall; Orrin, Mira and Sela each answer when talked to; the repair prompt appears once Sela sets off.
- [ ] The first combat (Sorrel) teaches attack, dodge, Flash Move and swap through play, not text.
- [ ] Ten minutes in, the player knows who they are, what went wrong at Sorrel, and where to go. **[Human]**

### 4.2 Controls and feel
- [ ] Digital pad (Precision) and analog pad: walk, jog and sprint speeds match D197. Turning at speed keeps pace. Sprint in combat rests when Thrust runs out.
- [ ] Attack, dodge, guard, swap, lock-on and quick item respond on the first press. Skills fire with RB plus a face button, or with the number keys. On a Precision, where LB is also the cycle modifier, lock-on answers LB's release, and only if no direction was pressed while it was held.
- [ ] With an enemy on the hero, A is an attack: no bench, crystal, repair bay, cache, survey key, quest pick-up, dock or warp answers it (`WithAnEnemyOnTheHeroAIsAnAttackNotAPrompt`, `WithAnEnemyOnTheHeroACacheAndTheKeyWait`). Story prompts and relays stay open: relighting a relay mid-fight is designed.
- [ ] Flash Move triggers within its window and slows the world. A roll started within .45 s of the last one still evades, but it costs 30 Thrust and earns no Flash; short of that Thrust it doesn't go, the Thrust bar flashes with the error sound, and the .45 s starts again from that press. Flash Guard gives Charge and a crit. A guard pressed within .45 s of the last press still blocks but isn't a Flash Guard.
- [ ] The camera never hides the party behind a cliff or wall in Sorrel, the gorge, the relay or Lanternwick (`cameraClearsScenery`).
- [ ] Flight: inertia, boost, brake, roll and lunge all respond; walls shove without trapping; docks are reachable from every approach.
- [ ] Prompt glyphs follow the active device and change on swap; no hard-coded "B" or "Y" appears on keyboard.

### 4.3 Progression and pacing
Keep a pacing log during the chain: the time of each level-up, skill learned, boss, new zone and story beat.
- [ ] Skills unlock at Sync 3, 5, 8 and 12. Chapter 1 should end near Sync 7 (Anchor Slam, Tether Hook, Survey Drone and Waypoint Blink learned), and chapter 2's main path should end at Sync 8 to 10, or 11 with every side quest. Sync 12 and its skills stay for chapter 3.
- [ ] A level-up card names the skill learned and says where to equip it. Mastery cards appear at 6, 18, 36 and 60 uses.
- [ ] Gear: the Lanternwick shop sells Refined (T2) parts; the starter parts are clearly outclassed by chapter 2.
- [ ] Scrip: the player can afford at least one T2 part by Lanternwick without grinding.
- [ ] No stretch of the chain goes 25 minutes without a new skill, zone, boss or story beat.

### 4.4 Combat and bosses
**Skills.** Release all 16 skills on the ground and in flight, at Sync 12 with each skill equipped in turn.
- [ ] Each skill does its job:
  - Anchor Slam closes distance.
  - Tether Hook reels in small enemies and pulls Taren to big ones.
  - Forge Spiral hits several times.
  - Faultline reaches 10 m and stops at walls.
  - Survey Drone keeps firing after Sela moves.
  - Waypoint Blink escapes and its charge bursts.
  - Gravity Fold gathers enemies.
  - Starfall waits for its warning.
- [ ] No skill damages through walls. No skill can be spammed to trivialise a boss (check Charge cost against Charge gain).
- [ ] The companion uses skills sensibly: at most every 6–9 s, and only in range.

**Enemies.** For each of the 14 types:
- [ ] Its tell is visible before it attacks.
- [ ] Its weakness and resistance show on hit.
- [ ] It dies with a fitting presentation.
- [ ] Grazers slow and back out of a lit relay's light; a ring on the ground shows where it ends.

**Bosses.** For each of the Burrower, the Bellows Below, the Cantor and Hessa Korl:

| Check | Bar |
|---|---|
| Title card and bar | Name, epithet, phase notches |
| Every special is marked | Telegraph markings before every hit; nothing lands outside its marking |
| Openings | At least one move per phase leaves a clear punish window (wedged charge, dodged chain, eruption) |
| Phase shifts | Readable brace, push-back, then a new move |
| Duration | 2–5 minutes at the expected level (Hessa: Sync 8–10). `python tools/boss_durations.py <run>...` reads what a replay chain took; a person's time on a pad is still the real test **[Human]** |
| Retry | Defeat returns to the checkpoint just before the arena; a second attempt is shorter |
| Hessa specifically | The last Hook Lance thrust is held a beat; a dodged Grapple Chain leaves her open; the Lampbreaker flash can be outrun or rolled through; she yields, and once Aumi has been told stands under guard |

- [ ] **[Human]** Each boss feels hard but fair on the Precision pad.

### 4.5 World and zones
For each zone (Decks, Halo, Sorrel, Hushwell, Gullet, Tallow Approach, Tallow Drift, Wick Reach, Lanternwick, Lantern Gorge, Sunken Relay):
- [ ] **Arrival:** the spawn point faces somewhere interesting, and the objective HUD names the next step with distance and direction.
- [ ] **Navigation:** no invisible walls in the open; no gaps to fall through; no spot where the party sticks (try every wall foot and prop); the companion keeps up.
- [ ] **Exits:** every dock and warp leads where its label says, and its locked text names what unlocks it.
- [ ] **Art:**
  - No `ART_PENDING` objects, and no floating or half-buried props.
  - No stretched textures: no kit piece stands 5 times longer one way than its texture. (The Decks' low cutaway walls, at 3 to 4.3, nobody can see on screen.) Cliff strata run horizontally.
  - Dead relays look dark; lit relays glow; lights suit the time of day.
- [ ] **Density:** walk the main route and count the stretches over 30 s with nothing new on screen (target: none).
- [ ] **Lanternwick:** every named NPC is where the story says and can be talked to; the shop is Corra's own stock; the lantern stair, archive and orchard are readable as places.
- [ ] **Wick Reach:** two relays open the amber light curtain; the dock is reachable; the warp home works after the chart.
- [ ] **Cinder Halo:** the Gullet warp's prompt and sign say it runs one way until you're through; a free repair waits beside it.
- [ ] **Gorge and relay:**
  - The relay door stays shut until all three relays burn.
  - Pell's lamp and the ration crate appear only once their quests are given.
  - The record plates and their lights only after Ishen's request; the old mirror stands they rest on are always there, and stay when a plate is taken.
  - The checkpoint cradle is less than a minute from Hessa.

### 4.6 Story, dialogue and script
- [ ] Read every conversation in order, using Yarn node names. Each character sounds like the cast notes in `GAME_DESIGN.md` and `STORY_CAMPAIGN.md`:
  - Taren is dry and steady; Sela quick and teasing; Orrin gruff and fond; Mira brisk.
  - Hal is slow and kind; Neve restless.
  - Aumi is wary, then warm; Pell eager; Ishen precise and delighted.
  - Corra is brisk and kind; Brekk quiet; Hessa proud and sardonic.
- [ ] **Writing quality:**
  - No line reads as machine-written or translated: no stiff missing contractions, no generic uplift, no explaining a joke.
  - Each compulsory conversation is under 90 seconds.
  - Every quest and objective states what to do and why.
- [ ] **Emotion tags** match the line's mood; portraits change with them.
- [ ] **Cause and effect:** Sorrel's signal → the nursery → the Cantor's collar → Tallow moving → the Compact's three conditions → Lanternwick's route → the hook to Xylos (food). **[Human]** Is the chapter 2 mystery (who strips relay glass, and why) engaging?
- [ ] **Text layout:** no typos, no overflow (`DialogueLayoutTests`), no raw ids, and the same term for the same thing everywhere ("survey key", "Keeper Abri", "Charge").

### 4.7 Quests
For each of the 10 quests (Silent Outpost, What the Drill Found, Six Bright Answers, An Overdue Parcel, Quiet Ridges, A Berth for Two, The Lantern Road, Light for a Lamp-Keeper, Ishen's Missing Record, Rations for the Unsworn):
- [ ] It starts where its giver says and appears in the log with a description.
- [ ] Each step's text is accurate, its count advances, and the card announces it.
- [ ] It completes, grants its reward (XP, scrip and items, named readably) and changes something visible (a line, a light, a person, a stock).
- [ ] It survives a save and Continue at every step.
- [ ] It can't be completed out of order or broken by leaving and returning.

### 4.8 UI and UX
- [ ] All seven pause tabs (Party, Skills, Gear, Fabricate, Items, Quests, Settings) open, navigate by pad and mouse, and keep focus sensibly.
- [ ] Skills page: four slots show the binding, cost and mastery; choosing a slot lists learned skills with their cost, mastery and cooldown, the focused skill's description in the note, and locked ones with their level; equipping one trades places with its old slot.
- [ ] The HUD shows integrity, Charge, Thrust, partner state, target with break, skill icons and costs, and the zone name. The boss bar hides the interaction prompt.
- [ ] Shops show names and prices, refuse unaffordable purchases with a reason, and sell back with confirmation text.
- [ ] Item names are consistent everywhere ("Scrap Alloy", "Repair Gel").

### 4.9 Audio
- [ ] Music by zone:
  - Hub Town Groove in towns and the Halo.
  - Old Village in Lanternwick.
  - Adventure Awaits in combat ground.
  - Moon Caverns in Hushwell and the relay.
  - Starfight in the Gullet and the Wick Reach.
  - Alien Boss Battle during bosses.
  - The shop theme while shopping.
- [ ] Transitions crossfade, and no track restarts on every zone load.
- [ ] Every skill has a sound, and new skills don't drown out combat. Volume sliders work.
- [ ] **[Human]** The mix sounds balanced on speakers and on headphones.

### 4.10 Performance and stability
- [ ] A full chain with displays awake: p95 at or under 17.1 ms, and no frame over 33 ms outside 1.5 s of a scene load.
- [ ] Zone loads under 5 s on Nathan's PC.
- [ ] No exceptions or error logs in `Player.log` over a full chain.
- [ ] Survey Drone, Starfall and Gravity Fold spam doesn't stack effects or leak objects (count `Skill ring` and `Starfall` objects after a fight).

### 4.11 Saves
- [ ] Autosave on dock, warp, repair and discovery, and when a conversation completes a quest or ends a chapter: once it closes, in a safe place only (no fight on, nobody down, not the proving grounds). Nothing it paid is paid again after a Continue. Continue resumes in the right zone and spawn, with the party, gear, quests, flags, skill loadout and mastery.
- [ ] Old saves (pre-skill-loadout, pre-chapter 2) load and get the starting loadout. One with no parts at all comes back armed from the title. A damaged save never stops the title coming up.
- [ ] Defeat offers "Return to last autosave" and doesn't wipe progress.

### 4.12 Accessibility
- [ ] Markings are shaped (circle, ring, cone, lane) as well as coloured; the Choir rule holds (every audio cue has a visual one).
- [ ] Text never renders below 16 px at 1280×720, which is 24 units at the 1080p reference (`ReadableTypeTests`).
- [ ] Keyboard rebinding works for the core actions.
- [ ] **Idea:** a difficulty option and a "hold to repeat attack" option.

### 4.13 Breaking things
- [ ] Spam interact during a dialogue, a scene load, a zone transition or a boss intro. An arrival beside an exit doesn't bounce the party back out, and a choice can't be answered before it has been up .3 s.
- [ ] Open the pause menu during every kind of dialogue and during a skill.
- [ ] Swap characters mid-skill, mid-dodge and while downed.
- [ ] Try to leave a boss arena mid-fight: pads and warps must wait.
- [ ] Walk into every corner of every new zone looking for stuck spots and falls.
- [ ] Sequence breaks: reach the gorge before Aumi; the relay before the relays; Lanternwick after the chart. Every step must still make sense.

## 5. Finding and fixing

Every finding goes into `docs/qa/ISSUES.md`, one line each (create the file if it doesn't exist):

```
| ID | Sev | Area | Zone | Summary | Repro (route/step or steps) | Status | Fix commit |
```

The loop for each Blocker or Major:
1. Reproduce it with a replay route or a PlayMode test.
2. Write the check so it **fails on the current build** and keep that red evidence (§ evidence rules in `NEXT_SESSION_PROMPT.md`).
3. Fix the cause. Never loosen an assertion to pass.
4. Rerun the focused suite, then the area's routes.
5. Commit with a message saying what the player will notice, via a message file; mark the issue Fixed with the commit.

Minor issues are batched by area. Ideas go in a separate list at the bottom of `ISSUES.md`, for Nathan to choose from.

**Routes to add during QA.** The chapter 2 chain needs routes that continue from `chapter-11-tallow-reading-return`, each run alone and validated:
- `chapter-12-wick-reach`: the warp, two relays, the gate, the dock.
- `chapter-13-lanternwick`: Aumi, Pell, Ishen and Corra.
- `chapter-14-lantern-gorge`: three relays, the camp, the den.
- `chapter-15-sunken-relay`: the plates, the checkpoint and Hessa.
- `chapter-16-lantern-home`: the chart, then Abri.

## 6. Questions for the human pass **[Human]**

Nathan answers these after playing the workshop build:
1. Which moment was the most fun? Which was the most boring?
2. Did any attack ever feel unfair? Which, and where?
3. Which skill did you use most, which never, and why?
4. Did any line of dialogue sound wrong for its character?
5. Was it ever unclear what to do next?
6. Did anything look unfinished or out of place?
7. Did Lanternwick feel like a town people live in?
8. How long did chapter 2 take, and did it feel the right length?

## 7. Exit criteria

| Milestone | Required |
|---|---|
| Workshop build for Nathan | Smoke passes; full EditMode and PlayMode suites green; chain passes; no open Blocker |
| Chapter 2 complete | All §4 checks for chapter 2 pass; the chapter 2 chain passes; no open Major; Nathan's §6 answers addressed |
| Release candidate | All of the above for every chapter; performance bar met; a full human playthrough with no Major findings |
