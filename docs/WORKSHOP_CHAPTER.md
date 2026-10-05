# Nursery chapter candidate

October 4, 2026. **Ready to try; C7 acceptance and C8 completion remain open.**

Double-click `Builds/Workshop-Chapter/Play Coronach Chapter.cmd`, or run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/workshop.ps1 -Chapter
```

Choose New Game. Saves and sound preferences stay in `Builds/Workshop-Chapter/Profile`;
logs go to its `Logs` folder. Nathan's LocalLow saves and the earlier workshop profile
are separate. The launcher does not erase either. `-Chapter -ShowCommand` prints
the exact command without starting a player.

This frozen release is source **56a013e**, with 243 hash-verified files totalling
616,478,606 bytes. `Builds/Workshop-Chapter/player-manifest.json` records every file.
Subsequent builds in `Builds/Windows` do not change this copy. Builds are local and
git-ignored; a fresh clone builds from the public source with generated fallbacks.
[Purchased-pack setup](OPTIONAL_ART_PACKS.md).

Play Decks -> Sorrel -> Hushwell -> Halo -> Gullet -> Tallow. Orrin's missing
deliveries lead to the silent outpost. After the Burrower, collect the survey key
and descend the drill bore. Sela's nursery discovery completes the chart needed
at the Halo beacon. The lift provides the surface shortcut. The nursery now has
an organic glowing floor sweep that matches the discovery.

Prepare through the shop, bench and equipment menus. Repair at Sorrel's outpost
between expeditions; use the drill bench to replenish gels before the cave.
Approach a downed companion to revive them. The tested route uses earned resources,
including a refined edge and protective frames, and dodges visible attack tells.
Repair at Tallow to save the completed slice.

The fresh ordinary-input chain passes in **38:37**, including real menus,
save/Continue, defeat/Retry, cave discovery, return lift and Cantor. The final save
has both heroes alive at level seven with six gels remaining. Both full suites
pass before the decorative floor change (**84 EditMode / 226 PlayMode**); afterwards
84 EditMode and 15 focused chapter/cave checks pass. The final floor recapture
passes in 446.8 seconds, and clean nursery timing has p95 16.98 ms with no frame
over 25 ms. The packaged executable's full opening passes again in 103.0 seconds.
[Progression evidence](validation/C8-nursery-integration.md),
[art and timing evidence](validation/C8-nursery-art.md).

Still unfinished: repetitive egg-cradle construction, sparse Sorrel stretches,
the opening walkway/Sela arrival scene, more resident interiors, the Burrower
disable finish, the Gullet preview/collar release and Tallow's moving-refuge story.
Continuous audiovisual review and physical-controller feel remain unverified.
The [earlier workshop report](WORKSHOP.md) retains the Q01-Q07 findings and station
GPU measurements. No Nathan approval or whole-chapter acceptance is claimed.
