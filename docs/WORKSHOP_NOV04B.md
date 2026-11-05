# Coronach workshop - November 4 (evening): feel fixes, boss fights, side stories

**To play:** double-click `Builds/Workshop-Nov04b/Play Coronach.cmd`, then choose **New Game** (or Continue). It keeps its own profile in `Builds/Workshop-Nov04b/Profile`. Your LocalLow saves and the earlier packages are untouched.

- Built from `a2929dd`; `player-manifest.json` hashes all 248 files.
- You played `Builds/WindowsDev` directly this afternoon. That folder has been rebuilt from the same source, but the workshop package is the stable copy.

## Your three reports

**"No NPC will talk to me on the controller."** Your log shows the pad's talk button working: it docked and launched you twice. The problem was the new opening. Orrin, Mira and Sela were switched off until the walkway repair was done, and the repair only appeared once Sela had finished crossing the market. Now:
- all three answer during the opening, with short lines that point you at the job;
- the repair prompt appears as soon as Sela starts walking, and she finishes her walk during the conversation.

**"Walking and running look like a swamp."** The walk and jog animations were being stretched to the wrong speeds. The town "walk" was a slow stroll forced to nearly twice its pace, with long, crouched steps, and the "sprint" played the jog in slow motion. In town:
- the stick or d-pad now jogs at full tilt, with a quicker, shorter step at the jog's own rhythm;
- a light tilt walks at a normal pace;
- sprint runs at the jog's natural stride;
- running leans into speed and banks into turns.

**"Sprinting and turning drops me to walking speed."** Two causes, both fixed:
- In town, sprint was quietly using up Thrust and then flickering on and off. Town sprint is now free.
- In combat, a spent sprint rests until Thrust refills instead of flickering, and turning at speed keeps the pace.

## Boss fights

Every boss attack now marks the exact ground it will hit, filling in as it winds up and flashing when it lands. If you're outside the marking, it misses you; a dodge still beats it. Each boss gains attacks as its health falls, and a short shift marks each new phase: the boss braces, pushes you back and changes rhythm. A boss bar and title card appear when a fight starts.

**The Burrower**
- Charges along a marked lane. Bait it into rock: it wedges, and the bar reads "OPENING · STRIKE NOW".
- Lobs spoil into marked circles and sweeps its tail at anyone behind it.
- Below 60%: calls out Scrapmites, dives, hunts you from below, then erupts under the marking and is briefly exposed.
- Below 30%: rings the ground with shockwaves to outrun or dodge through.

**The Bellows Below**
- Its inhale pulls you in before the exhale lands, so run out or dodge through.
- Its ribs slam out in a star, and from half health it releases drifting pressure bubbles that follow you.

**The Cantor**
- Its choir and collar work as before.
- Once wounded, it sings rotating spirals of shots (fly through the gaps) and lashes with its whole coil.

Two fixes came out of watching the fights:
- You can no longer accidentally launch off a pad mid-fight; your attack button used to reach it.
- Near Sorrel's cliffs, the camera now moves in instead of losing you behind the rock.

## Side stories

People in chapter 1 now ask for help, and each request leaves something behind:
- **An overdue parcel.** On a return visit to Neve at the Halo, she hands you one of Sorrel's missed deliveries.
- **Quiet ridges.** After the Burrower, the Sorrel survivor asks you to switch off four evacuation beacons still calling machinery to the outpost. They blink blue until silenced and stay dark afterwards.
- **A berth for two.** Oda's family filed for the Nacre transfer, but their berth token is stuck on Orrin's desk.
- **The chapter now ends on a hook.** Once Tallow knows it is moving, someone from the Compact calls on the beacon band.

The quest log shows the next step for every task, and a card appears when a task starts, moves on or finishes.

## Verified, and what isn't

**Verified by an agent-driven virtual gamepad:**
- New Game → Tallow completes in 43:51 with the displays awake.
  - Only one frame in 44 minutes went over 33 ms outside scene loads.
  - Every boss fell after three phases: Burrower 55-83 s, Bellows 95 s, Cantor 86 s.
- 194 EditMode and 494 PlayMode checks pass on the packaged source.
  - The new checks failed first, on the old code or a deliberately broken copy.
- The Burrower fight was reviewed frame by frame from three recorded runs. Those runs found the two fixes above and a balance pass ([evidence](validation/C8-nov04b-bosses-sides.md), [chain](validation/C7-nov04b-chain.md), [feel](validation/C2-nov04-feel.md)).

**Not yet judged by a person:** how the new gait looks and how the Precision feels, how hard the bosses feel (the replay driver lost Taren once in the Burrower's last phase), sound, and the side stories in play.

### Known rough edges
- Bosses have no new animation yet. The Burrower rears and lunges procedurally, and the others are as they were.
- Sela's high stepping on steep slopes is unchanged.
- The Decks still has no ordinary residents walking around.
