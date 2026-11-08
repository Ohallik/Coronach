# Coronach workshop: November 7 (after the QA pass)

**To play:** double-click `Builds/Workshop-Nov07/Play Coronach.cmd`, then choose **New Game**. It keeps its own profile in `Builds/Workshop-Nov07/Profile`, which starts empty, so Continue works once you've saved in it; your own saves and the earlier packages are untouched. Your usual `Builds/WindowsDev` player is rebuilt from the same source, if you'd rather carry on with your own saves.

## What happened

A tester session spent two days playing the whole game, both chapters from New Game to Abri's homecoming, through the replay driver with a virtual pad. Readers then went through four fresh captures of the whole game, frame by frame. It also ran an audit of every system and went through every line of the test plan's section 4, adding a check wherever nothing tested it properly. It fixed **615 of 655 findings**, including every Blocker and Major (26 of the Majors are much better rather than gone). It also logged 34 ideas: 24 are built, and 10 are left for you to choose from. The full list, with each fix's commit, is in [qa/ISSUES.md](qa/ISSUES.md).

## What you'll notice

**Saves and losing.** With a save on disk the title starts on Continue, and New Game asks before it replaces anything. Losing a boss fight sends you back to its door, whole, and any other loss returns you, whole, to the latest autosave. Old saves that had lost Neve's cache, Hal's starting materials or Pell's move to Tallow are put right when they load.

**Fights.** Every attack is marked on the floor where it will really hit, and a hit outside its marking doesn't land. Walls stop blades, bites and shots, yours and theirs. Each boss now has openings to punish and shows its whole set of moves. Mashing dodge still gets you out but costs Thrust and earns no Flash. A plain swing turns to the enemy beside you. Bosses last about two to three minutes for the replay party.

**People.** Hessa, Brekk, Corra, Ishen and Pell have their own bodies, made from their portraits. Hessa yields instead of falling, then talks to you. Lanternwick has townsfolk, stalls and a built stair up to Aumi.

**The screen.** Menus sit over the dimmed world on a framed plate. Panels fade out of the way of whatever is behind them, and their words stay on screen, though over bright art they can be hard to read. The objective has an arrow, and in flight a mark at the screen's edge points the way when the goal is off screen. The game has its own type (Cinzel and Nunito) and a real logo on the title.

**Places.** The drill site, the cradles, the gorge's den and the Sunken Relay were dressed so they look like what they're called. Signs stand by what they name. The stars in the Halo, the Wick Reach and Tallow's approach now drift past as you fly.

**The last small batch.** The Burrower is plain steel instead of a spotted grub. Making Repair Gels no longer levels your Emitters. A shop that opens at the end of a talk starts on the Buy/Sell toggle, so tapping through Corra's lines can't buy a 240-scrip part by accident. Sela's shots have short tails instead of long beams.

## What to try

1. **A new game through to Tallow, then chapter 2.** Note where it drags and where you didn't know what to do.
2. **The four bosses on your Precision**, timed: the Burrower, the Bellows, the Cantor and Hessa. Are they hard but fair, and does each take two to five minutes?
3. **Flying:** let go of boost (it should coast), and Waypoint Blink toward and away from an enemy.
4. **Losing a boss fight on purpose:** you should be back at its door, ready to go again.
5. **Shopping at Mira's and Corra's** after their conversations.

## What only you can judge

- **The test plan's eight questions** ([TEST_PLAN.md](TEST_PLAN.md) section 6): the most fun and most boring moment, any attack that felt unfair, which skills you used, any line that sounded wrong, when you didn't know what to do next, anything that looked unfinished, whether Lanternwick feels lived in, and how long chapter 2 took.
- **Feel on the pad:** boss length, Hessa's difficulty (the replay party lost to her once in three fights on this build), boost release, Blink, and whether a guard that costs nothing makes fights too safe.
- **Listening:** each place's new ambience, and the mix. Nobody has listened to it.
- **Words to approve:** Orrin's hail at the outer warp, the line that teaches the swap, "Get Taren back up" when a hero is down, and the chapter card.
- **Looks in motion:** Hessa's yield pose, the townsfolk, the Burrower's steel, and the camera easing in and out to keep a fight in frame.
- **Your call:** a meter or facing rule for guard; a use for Husk Core, Choir Resin and Cantor Pearl; a cap on Repair Gels; something to spend scrip on in chapter 2; whether the apprentice Pell or the planned companion Idra Pell keeps the name; whether Hal's bench should open on something that spends nothing, as the shops now do; and whether floor signs in flight should stop growing past a size as the camera pulls back.

## Known rough edges

None of these block play. All are in ISSUES.md, as open findings, partly fixed ones or ideas:
- Tallow's hull is soft up close, and the prompt bar at the bottom doesn't fade for what's behind it as the panels do.
- The haul road up to the drill site is six similar views. The Gullet's arrival shows no mouth. Sorrel's slope beside the Burrower has soft, airbrushed bands.
- The first job's gate boards look like stand-ins, the Bellows' door seal has no frame, and the wrecked rig at Sorrel's drill site still doesn't read as a wreck.
- Hal's bench, opened from his talk, starts on a recipe, so tapping through his last lines can make one (2 Scrap Alloy).
- A short row of faint shadow dashes lies beside the gorge's first fight.
- Sela sometimes flies under the skill row, fires at things off screen, or crosses a prompt.
- The small target card can sit over an enemy at the top of the screen, and the Cantor's boss card can hide Sela.
- A few menus stutter for a single frame as they open (the save picker, the fabrication bench), and so does saving.
- Very rarely the game crashes as it quits (twice in about 160 test runs, inside Direct3D 12 as the game shuts down). Your saves are already written by then.
