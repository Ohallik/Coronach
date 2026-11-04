# Coronach workshop - November 3 (cave and Gullet) candidate

**To play:** double-click `Builds/Workshop-Nov03d/Play Coronach.cmd` (or `powershell -ExecutionPolicy Bypass -File scripts/workshop.ps1 -Package Workshop-Nov03d`) and choose **New Game**. It has its own profile in `Builds/Workshop-Nov03d/Profile`. Your LocalLow saves and the earlier workshop packages are untouched.

Built from `34c6d30`; `player-manifest.json` hashes all 247 files. `Workshop-Nov03c`, `Workshop-Nov03b`, `Workshop-Nov03` and the frozen October 4 chapter were re-verified byte-identical. Same chapter route as before, about 40 minutes played directly.

## New since Workshop-Nov03c: Hushwell and the Gullet

- **Hushwell's lamps finally light the cave.** The whole cave used to be drawn as one object, which meant only six of its lights could touch the floor, so it was one dim brown sheet. It is now drawn in sections, and every work lamp, vent and crystal throws its own light.
- **The miners' level is a working mine.** A rail line runs from the breach up the haul tunnel into both ore galleries, with two ore carts on it and a buffer stop at the scaffold. Work lamps light the tunnels, the miners' camp sits at the breach (power cell, cable, bench, core samples) and their last cache in the east pocket still has a lamp running.
- **The pressure gallery shows you where it burns.** Each vent is ringed by pale scald crust exactly as far as its plume reaches, so the safe ledges between them read at a glance.
- **Crystal seams light the way down,** glowing on the far walls of the galleries and chambers.
- **The nursery is a colony now.** Seven cradles of three kinds (full, hatched, one egg) and different sizes, clustered off your path, instead of five identical cradles in a ring.
- **The Gullet's floor is anatomy.** Faint blue vessels run with the passage (kept quiet so they never outshine your ships or the shots); the feeding chamber has digestive pools and pores; the throat's folds have scarred the floor; silt has settled by the eddy's wreck; egg clusters thicken toward the nursery gate; and the Cantor's coil is scoured in arcs where it lies. Nothing on the floor can touch your ships.

## Verified, and what is not

An agent-driven virtual gamepad completed New Game → Tallow in 41:26, both heroes alive, with no frame-time change in the newly dressed areas ([chain](validation/C7-nov03d-chain.md)). 188 EditMode / 461 PlayMode checks pass. Captured flights and walks through Hushwell and the Gullet were reviewed frame by frame ([evidence](validation/C8-dressing-pass3.md)). That is not human play. Not yet judged by a person: how the places feel, controller feel, combat difficulty, sound mix, motion smoothness on your screen.

Known rough edges: the last ramp down to the nursery is still dark; the coil's scour arcs end abruptly; Sela can step high on slopes; Tallow's line does not reel.
