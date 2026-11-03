# Coronach workshop - November 3 (furnished) candidate

**To play:** double-click `Builds/Workshop-Nov03b/Play Coronach.cmd` (or `powershell -ExecutionPolicy Bypass -File scripts/workshop.ps1 -Package Workshop-Nov03b`) and choose **New Game**. It has its own profile in `Builds/Workshop-Nov03b/Profile`; your LocalLow saves, `Workshop-Nov03` and the frozen October 4 chapter are untouched.

Built from `f019bcf`; `player-manifest.json` hashes all 246 files. Same chapter route as Workshop-Nov03, about 40 minutes played directly.

## New since Workshop-Nov03: places to walk through

- **The Decks are furnished room by room.** Arrivals vestibules have waiting benches, luggage and lockers; Orrin has a desk, records and a visitors' corner; Mira's market has produce stalls and back stock; the galley has tables with stools and herb planters; Hal's hull has a workbench, parts shelves, stores and receiving. Walkway runners show the real routes (dock → promenade → rooms → service gallery), and homes, the office and the galley have rugs.
- **The four cabins are now actually private.** They had open 3.7 m gaps into the office and service gallery behind their "closed" hatches; those are walled, and each cabin is furnished as someone's home.
- **Tallow reads as a lived-in refuge:** a mess table and waiting area, the Keeper's bench and lamp, life-support equipment behind the hatches, bunks in the rest cabins, a runner from the lift.
- **Sorrel's outpost has life** (fuel tank, comms mast, solar panels, water barrels, a parked rover, stock at receiving and the repair pad, an ore hopper at the dig), the excavation cliffs show rock strata, and the ground has a paler packed haul road and clay/dust patches.

Nothing new can hide your heroes from the camera: the builder checks every piece against the fixed camera angle and the replay routes before it saves.

## Verified, and what is not

An agent-driven virtual gamepad completed New Game → Tallow in 41:36 on this exact build, both heroes alive, with no frame-time change in the dressed areas ([evidence](validation/C7-nov03b-chain.md)). 176 EditMode / 460 PlayMode checks pass ([dressing evidence](validation/C8-room-dressing.md)). That is not human play. Not yet judged by a person: how the rooms feel to walk through, controller feel, combat difficulty, sound mix, motion smoothness on your screen.

Known rough edges: the open middles of the Decks hulls are still bare plate (they are the walkways); lamps are small; Sorrel's wide basin floors between paths are still plain; Sela can step high on slopes; Tallow's line does not reel; nursery cradles repeat.
