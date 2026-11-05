# Content audit (November 5)

Nathan asked whether the game has enough towns, dungeons, open areas and space combat; enough distinct skills tied to progression; and a script that reads as professional and in character. This note records what was there, what changed, and what is still short. Decisions D200–D202 hold the detail.

## Zones

| Type | Before (chapter 1) | Added (chapter 2) | Now |
|---|---|---|---|
| Town | The Decks, Tallow Drift (a small refuge) | **Lanternwick**, a canyon town | 3 |
| Open ground | Sorrel Ridges | **Lantern Gorge** | 2 |
| Dungeon | Hushwell | **The Sunken Relay** | 2 |
| Space, combat | The Gullet (a warp tunnel) | **The Wick Reach**, an open lane with a relay gate | 2 |
| Space, safe | Cinder Halo, Tallow Approach | (a warp out of the approach) | 2 |
| Bosses | Burrower, Bellows Below, Cantor | **Hessa Korl**, the first duel against a person | 4 |
| Enemy types | 9 | Gloomgrazer, Muster Scav, Muster Sergeant, Muster Skiff (+ Hessa) | 14 |

**Verdict.** Chapter 1 alone was too thin for a long game: one of each zone type and about 45 minutes of chain play. Chapter 2 adds one of each again, and a boss of a new kind. The atlas plans 52 main maps across six movements, and 11 are now built, so the game is still short of its target.

Each new place was chosen for a job the existing ones don't do:
- **The Wick Reach** is open-space combat with a light mechanic.
- **Lanternwick** is an old town rather than a station.
- **The gorge** is a night canyon whose light matters in combat.
- **The relay** is a carved ruin rather than a cave.

**Next, by the atlas:** Xylos and Many Hands (food), then Wendmire and Veyr.

## Combat skills and progression

**Before:**
- Eight skills, four per hero, all available from the start.
- All eight had identical numbers (20 Charge, 4 s cooldown, 40 damage).
- Nothing to learn while levelling, and a level cap of 10 that chapter 2 would hit almost at once.

**Now (D201):**
- Sixteen skills. Each hero learns one at Sync 3, 5, 8 and 12, and each new skill does a job the others don't:
  - Taren gets a gap-closer, a hookshot, a spin and a ranged rupture.
  - Sela gets a turret, an evasive blink, a gathering well and a delayed strike.
- Four are equipped from a Skills page.
- Mastery rises with use, Ys style.
- The companion uses its skills.
- The level cap is 40.

The inspirations, named so the choices can be judged:

| Game | What it contributed |
|---|---|
| Ys VIII/IX | Skills learned by level, skill mastery, a partner using skills |
| Elden Ring | A leaping, guard-breaking slam; a held-beat combo finisher for Hessa |
| Zelda | The hookshot (Tether Hook) and the spin attack (Forge Spiral) |
| Twin-stick space shooters | The drone turret, the area denial well and the marked orbital strike |

**Still open:** an EXTRA-style super move, skills for later chapters' heroes, and the companion's choice of skills (it uses whatever is equipped).

## Story, dialogue and script

**Before:**
- The older chapter 1 scenes avoided contractions ("I am coming", "Let us see the stock"), so the same characters sounded stiff in one scene and natural in the next.
- A few lines were cryptic ("The key names the route…"), the Tallow line scene was tagged entirely Neutral, and the outpost foreman and Tallow's keeper had no names.
- On screen there were raw ids ("ScrapAlloy", "Hub CinderHalo", "slot1"), literal "B"/"Y" keys on keyboard, two names for the warp key, "task" in one place and "quest" in another, and a locked warp that kept its locked text after opening.

**Now (D200):**
- Every chapter 1 node is rewritten in its speaker's voice from the cast notes, keeping nodes, flags, options and line counts.
- Dace and Keeper Abri are named in the story and on screen.
- All screens use readable names and device glyphs, with one term per thing.
- Chapter 2 adds about 130 lines for six new characters, each with a defined voice:
  - Aumi, wary and then warm;
  - Pell, eager;
  - Ishen, precise and delighted;
  - Corra, brisk and kind;
  - Brekk, quiet and ashamed;
  - Hessa, proud and sardonic.
- Chapter 2 also has three side quests that foreshadow the Muster, the Bridles and Oriel.

**Still to judge [Human]:**
- Whether each voice lands when read in play.
- Whether chapter 2's question (who strips relay glass, and why) pulls the player forward.

The QA pass in [TEST_PLAN.md](TEST_PLAN.md) §4.6 reads every node in order.
