# Coronach workshop: November 5 (chapter 2, new skills, script pass)

**To play:** double-click `Builds/Workshop-Nov05/Play Coronach.cmd`, then choose **New Game** (or Continue). It keeps its own profile in `Builds/Workshop-Nov05/Profile`; your LocalLow saves and the earlier packages are untouched.

## What's new

**Chapter 2, The Lantern Road.** After the Compact's call at Tallow, talk to Keeper Abri again.
- **The Wick Reach.** A warp off Tallow's approach leads to a space lane where Muster raiders are cutting down a chain of old lantern relays. Relight two relays to open the amber light curtain in front of the dock.
- **Lanternwick.** A canyon town of carved homes, lantern towers, an arch market, an archive and an orchard.
  - Keeper Aumi doesn't trust visitors: the last ones stole her relays.
  - Pell (her apprentice), Ishen (the archivist), Corra (the trader, with her own stock of Refined parts) and Brekk (someone hiding in the orchard shed) all have something for you.
- **Lantern Gorge.** A dark canyon with light-shy Gloomgrazers and a Muster camp. Relight its three relays: lit relays slow the grazers, and relighting one mid-fight staggers nearby enemies.
- **The Sunken Relay.** A buried relay station.
  - The boss is **Hessa Korl**, a Muster scavenger captain and the first person you fight rather than a beast. Watch her last lance thrust: it comes a beat late.
  - If you dodge her chain, she's open.
  - When she raises the crystal, get clear.
- **Three side quests:** Pell's lost lamp, Ishen's record plates and rations for an Unsworn deserter.

**Skills.** Each hero now learns four more skills, at Sync 3, 5, 8 and 12. The pause menu has a new **Skills** tab: choose a slot, then a skill. Skills get stronger and cheaper the more you use them. Your companion now uses its skills too.
- **Taren:**
  - **Anchor Slam:** leap at a target and slam.
  - **Tether Hook:** reel small enemies in; pull yourself to big ones.
  - **Forge Spiral:** a spinning advance.
  - **Faultline:** split the ground in a line ahead.
- **Sela:**
  - **Survey Drone:** a drone that keeps shooting.
  - **Waypoint Blink:** blink away, leaving a charge.
  - **Gravity Fold:** drag a crowd together.
  - **Starfall:** beams fall on a marked circle.

**Script pass.** Every chapter 1 conversation was rewritten so each character sounds like themselves; the stiff no-contraction lines are gone. The Sorrel foreman is **Dace** and Tallow's keeper is **Keeper Abri**. Menus show proper item and place names instead of internal ids.

## Verified, and what isn't

- **Automated checks:** 194 EditMode and 516 PlayMode checks.
  - The new chapter 2, skill and story checks failed first or were run against a deliberately broken state.
  - One older Burrower check is random and failed once; it passed on rerun.
- **The chapter 1 chain** (New Game → Tallow, virtual gamepad, displays awake) still completes: see the [chain note](validation/C7-nov05-chain.md).
- **Chapter 2** is tested by its own checks and by still images. **It has no replay route yet**, so nobody has played it end to end. That is the first job for the QA pass ([test plan](TEST_PLAN.md), [prompt for a testing session](TESTER_PROMPT.md)).
- **Not yet judged by a person:**
  - how chapter 2 plays and how long it takes;
  - whether Hessa is fair on your Precision;
  - how the new skills feel;
  - whether the rewritten lines sound right.

**Known rough edges.**
- Lanternwick has no ordinary residents walking around yet.
- New bosses still use existing bodies with procedural motion.
- Lanternwick's people use reskinned bodies of the chapter 1 cast.
- The Burrower's last phase sometimes beats the automated player.
