# Coronach workshop - November 4 (the first morning) candidate

**To play:** double-click `Builds/Workshop-Nov04/Play Coronach.cmd` (or `powershell -ExecutionPolicy Bypass -File scripts/workshop.ps1 -Package Workshop-Nov04`), then choose **New Game**. It keeps its own profile in `Builds/Workshop-Nov04/Profile`. Your LocalLow saves and the earlier workshop packages are untouched.

- Built from `f931c48`; `player-manifest.json` hashes all 248 files.
- `Workshop-Nov03d`, `Nov03c`, `Nov03b`, `Nov03` and the frozen October 4 chapter were re-verified against their manifests.

## New since Workshop-Nov03d

### The opening scene
A New Game now plays the first town's morning from the story plan. Docking at Orrin's office shows:
- the bridge to the market closed for Taren's repair, with amber gates and a service cover open in the deck;
- Mira's produce cart sitting on his yellow safety line;
- Sela walking across the market with a receipt for a ship that left an hour ago.

Finish the job. The bridge shudders under the last line, then the gates fold away, Sela heads off to wait for Orrin and Mira wheels back to her stall. Continue and older saves skip the scene.

### Places
- **The Halo's roofs:** each hull shows its trade on top (office, market, repair), with solar arrays, tanks, pipes, a mast and corner lamps.
- **Tallow's refuge deck:** crisp hazard paint rings the docking apron.
- **Hushwell's last ramp to the nursery:** no longer black. The floor now takes light in soft pools instead of hard rings, and crystal seams light the shaded side.
- **The Gullet's exit:** grooves worn by the current lead you to the way out, and the coil's scour marks fade out instead of stopping square.

### Two story beats now actually happen
- **The Cantor rests coiled** in its chamber before the fight, breathing, instead of lying straight like a model on a shelf.
- **Tallow's line is a braided rope.** Once you've looked at its free end, the reel winds it in, and it stays wound in later visits.

### Combat
- **Skills hit harder:** Arc Cleave, Ember Dash and Lance deal 2.6 times a normal hit instead of 1.7.
- **The Gullet's ordinary enemies hit 20% harder.** Hushwell is unchanged, because its Scrapmites are dangerous enough already.

## Verified, and what isn't

**Verified by an agent-driven virtual gamepad:**
- New Game → Tallow completes in 42:14 with both heroes alive.
- The displays were awake this time, so the frame-time evidence counts. Outside scene loads, only one frame in 42 minutes went over 33 ms.
- The Decks stutters from October did not come back in two ten-minute runs.
- 188 EditMode and 470 PlayMode checks pass.
- The opening was reviewed frame by frame from a recorded run ([evidence](validation/C8-nov04-batch.md), [chain](validation/C7-nov04-chain.md)).

**Not yet judged by a person:** how the opening and the new combat numbers feel, controller feel, sound, and smooth motion on your screen.

### Known rough edges
- **Sela's high stepping on steep slopes is still there.** It turned out both heroes do it. Re-tuning the leg code broke other leg checks, so it waits for a proper rework ([diagnosis](validation/C2-uphill-fold.md)).
- **The Decks still has no ordinary residents walking around.** That needs new character models.
