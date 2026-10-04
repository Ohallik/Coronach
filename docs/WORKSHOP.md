# Coronach workshop candidate

October 4, 2026. **Playable candidate; C7 acceptance remains open.**

From the repository root, run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/workshop.ps1
```

Or double-click `Builds/Workshop/Play Coronach.cmd`.

This opens `Builds/Workshop/Player/Coronach.exe` at Title. Choose New Game. Workshop
saves and sound preferences stay in `Builds/Workshop/Profile`; Nathan's existing
LocalLow Lattice saves are untouched. Continue resumes this workshop profile.
`-ShowCommand` prints the launch command without starting the game.

The local candidate is frozen from source `06b0686`, separate from subsequent
`Builds/Windows` development. Its 243 player files total 614,873,502 bytes;
`Builds/Workshop/player-manifest.json` records every SHA-256. It excludes old
Lattice executables/data and Unity's `DoNotShip` debugging folders. Builds remain
git-ignored. To build the public source, run
`powershell -ExecutionPolicy Bypass -File scripts/headless.ps1 -Mode build`.
That writes `Builds/Windows` and does not overwrite this frozen workshop copy.
A fresh clone does not contain the local workshop package; see the optional-pack
setup linked below before building.

The ordinary route is Cinder Halo / Decks -> Sorrel -> Gullet -> Tallow.
Hushwell currently branches from the drill bore after the Burrower; integrating
its nursery into the main story is C8 work. Talk to Hal, craft and equip before
leaving town. On Sorrel, use the outpost repair cradle between expeditions;
the drill bench can turn mined crystals into a refined edge. The Tallow repair
console saves completion. Menus explain current device bindings; full controls
are in [README](../README.md#controls).

The accepted agent-directed route includes New Game, dialogue, shop, fabrication,
equipment, manual save/Continue, a real defeat/Retry and the final refuge save.
Nine segments use exact earned saves: **30:04, 108,115 focused frames**. This is
virtual-gamepad evidence, not Nathan's play or physical Logitech validation.
[Detailed route and preserved rejections](validation/C7-workshop-flow.md).

| Original finding | Current candidate and limits |
| --- | --- |
| Q01 station stutter | Camera-follow/HUD fixes plus a newly reproduced startup VSync defect. Default Decks p95 fell from 266.11 to 17.02 ms. Final release/development first/warm station runs pass at 16.94–17.01 ms p95, and valid GPU runs agree at 1.36–2.04 ms. One intermittent corrupt timing run is retained as rejected. Watched smoothness remains unverified. |
| Q02 sideways running | Calibrated clip orientation and target-facing locomotion; both heroes/forms measured at 30/60/120 fps. Continuous review remains unverified. |
| Q03 weak motion/contact | Stride/contact corrections, buffered input and move-specific contact/recovery; ordinary combat routes pass. Overall physical feel remains unverified. |
| Q04 inadequate deaths | Authored falls, corpse holds, deliberate cleanup and partner rescue; real defeat/Retry exercised. Boss-specific finishes remain chapter work. |
| Q05 weak sound | Owned cue families, surface footsteps, mixer controls and bounded voices are implemented; recordings exist. Audition remains unverified. Music loops remain immediate per Nathan's rule. |
| Q06 dialogue/UI | Per-speaker faces/bodies, 720p fit, device hints, held/rapid input, choices, handoffs and reload controls. Physical disconnect/reconnect remains unverified. |
| Q07 station construction | Decks/Halo redesigned and reviewed together, with ordinary dock/public/service routes. Final map review remains separate from navigation success. |

Burrower/Bellows visibility is repaired and recaptured in ordinary play:
[local hero reveal](validation/C3-boss-visibility.md). The Gullet coil now has a
folded recessed bed and dedicated generated moorings; the earned-save ordinary
recapture passes through Cantor to TallowApproach. Its sparse approach, preview
pocket and collar-release story remain C8 work.

Final checks: both arena smokes, ordered slice (496 s), UI,
24 portrait captures, 69/69 EditMode, 218/218 PlayMode, 113 offline controls, both stations'
release/development first/warm and valid headroom runs, and both ships' clean
Gullet circuits (394 s each, no frame above 25 ms). The frozen copy also passes
the full ordinary opening route (103 s, 6,156 focused frames). Nathan's original
save hashes match after the full suite and candidate runs. Continuous motion/audio review, physical-controller feel and
the remaining chapter/map work are open. This document does not claim
C7_WORKSHOP_ACCEPTED. [Candidate evidence](validation/C7-candidate.md).
Purchased grass/water are local only; the public project uses owned generated
fallbacks. [Fresh-clone and licensed-pack setup](OPTIONAL_ART_PACKS.md).

The next chapter increment is described in
[opening integration](C8_OPENING_INTEGRATION.md). It connects the nursery through
play and migrates legacy saves explicitly; it is not included in this candidate.
