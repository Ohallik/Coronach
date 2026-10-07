# Nursery chapter candidate

October 4, 2026 candidate. **October 6 follow-up: moving-screen lag/tearing and motion acceptance are open. This preserved a3aaad6 package does not contain the new knee repair. Its replacement failed synchronized presentation and has not been published.** See [presentation evidence](validation/C1-C2-presentation-motion.md) and the [terrain follow-up](validation/C2-terrain-followup.md). C7/C8 remain open.

**October 7:** the unchanged replacement packages now pass clean Decks timing with displays awake, and the combat recapture passes motion. [Comparison and limitations](validation/C1-C2-awake-comparison.md). Continuous movement, physical tearing and controller feel remain unverified; this frozen chapter and profile are still preserved, and no replacement has been published as validated. All 243 frozen player files were reverified.

Double-click `Builds/Workshop-Chapter/Play Coronach Chapter.cmd`, or run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/workshop.ps1 -Chapter
```

Choose New Game. Saves and sound preferences stay in `Builds/Workshop-Chapter/Profile`;
logs go to its `Logs` folder. Nathan's LocalLow saves and the earlier workshop profile
are separate. The launcher does not erase either. `-Chapter -ShowCommand` prints
the exact command without starting a player.

This frozen release is source **a3aaad6**, with 243 hash-verified files totalling
616,521,326 bytes. `Builds/Workshop-Chapter/player-manifest.json` records every file.
Subsequent builds in `Builds/Windows` do not change this copy. Builds are local and
git-ignored; a fresh clone builds from the public source with generated fallbacks.
[Purchased-pack setup](OPTIONAL_ART_PACKS.md).

The previous chapter player is preserved in `Builds/Workshop-Chapter/Player-56a013e`
with `player-manifest-56a013e.json`. The existing chapter profile is retained.

Play Decks -> Sorrel -> Hushwell -> Halo -> Gullet -> Tallow. Orrin's missing
deliveries lead to the silent outpost. After the Burrower, collect the survey key
and descend the drill bore. Sela's nursery discovery completes the chart needed
at the Halo beacon. The lift provides the surface shortcut. The nursery now has
an organic glowing floor sweep that matches the discovery, softly blended pool
banks and quieter water. Broken pressure organs now stay broken across saves;
defeating Bellows leaves the whole chamber quiet, including older completed saves.

Prepare through the shop, bench and equipment menus. Repair at Sorrel's outpost
between expeditions; use the drill bench to replenish gels before the cave.
Approach a downed companion to revive them. The tested route uses earned resources,
including a refined edge and protective frames, and dodges visible attack tells.
Repair at Tallow to save the completed slice.

The earlier fresh ordinary-input chain passes in **38:37**, including real menus,
save/Continue, defeat/Retry, cave discovery, return lift and Cantor. That chain ends
with both heroes alive at level seven and six gels. The current runtime passes
**89/89 EditMode and 229/229 PlayMode**, zero skipped. A held-backup save failure
found during validation is repaired with bounded retries and preservation checks.

The current earned cave recapture passes in **443.25 seconds**, with both heroes
alive at level six and five gels. Clean nursery walking has p95 **16.968 ms**,
p99 **17.081 ms** and one **37.782 ms** frame across 378 seconds. The earlier
152.8 ms first-visit hitch remains unexplained and retained. Neither captured
progression run is a clean timing measurement. The packaged executable's full
opening passes in **102.90 seconds / 6,158 focused frames**.
[Progression evidence](validation/C8-nursery-integration.md),
[art](validation/C8-nursery-art.md), [shorelines](validation/C8-nursery-water.md),
[revisit and final package evidence](validation/C8-organ-persistence.md).

Still unfinished: repetitive egg-cradle construction, sparse Sorrel stretches,
the opening walkway/Sela arrival scene, more resident interiors, the Burrower
disable finish, the Gullet preview/collar release and Tallow's moving-refuge story.
Continuous audiovisual review and physical-controller feel remain unverified.
The [earlier workshop report](WORKSHOP.md) retains the Q01-Q07 findings and station
GPU measurements. No Nathan approval or whole-chapter acceptance is claimed.
