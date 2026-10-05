# Pressure-organ save and revisit repair

October 4, 2026. **Repair and playable package validation PASS; broader C7/C8 acceptance remains open.** The nursery-water player capture exposed a live pressure-organ target in a supposedly completed cave. Inspection found a general revisit defect: organs had no saved state and ignored Bellows completion. This affected real saved progression as well as development fixtures. [Raw evidence hashes](C8-organ-persistence-evidence.json).

Four cases fail on the original runtime, retained in `Builds/quality/C8/organ-persistence-checks/original-red.*`:

- A manually saved broken west organ reloads pumping; its opposite half must remain live.
- Defeating Bellows leaves the remaining organs pumping.
- An older completed cave restores live organs despite its boss-completion flag.
- The completed-cave Gullet fixture contains live organ targets.

Each authored organ now has a stable `hushwell.organ.west` or `hushwell.organ.east` flag. A normal break records that flag and retains the existing collapse/effects. Loading a broken organ restores its zero integrity and collapsed pose quietly. Bellows completion settles both immediately, and its existing completion flag also restores older cleared caves without inventing individual organ history. The operation does not emit organ damage/death events or award anything on load.

`HushwellBuilder.RefreshOrganSaveKeys` adds only the two scene identities; future isolated cave builds author the same keys. Save version remains two because the optional flags are compatible with existing saves. Standard save/checkpoint ownership is unchanged.

The affected chapter/cave checks pass **18/18**. The independent-half save/reload check was then extended to exercise both west and east; the full **85/85 EditMode** suite passes. All three new persistence tests and the strengthened fixture check also pass in the first integrated PlayMode run.

Two earlier full runs remain **REJECTED, 228/229 each**, zero skipped (`full-first.*`, `full-second.*`). The existing all-dialogue branch test threw `System.IO.IOException: Unable to remove the file to be replaced` from `SaveSystem.Save` / `File.Replace`, at different points in its unique temporary C6 branch directory. Its unchanged isolated repeat passes all 29 compiled nodes / 36 outcomes (`dialogue-isolated-green.*`). A separate OS-handle probe reproduces the exact error by holding the backup without delete sharing; the original interfering process is unknown. [Replacement repair](C6-save-replacement.md) passes without altering the dialogue test. Nathan's original save hashes and file count are restored exactly after both failed full runs and the final passing run.

After the bounded save replacement repair, the integrated suite passes **229/229 PlayMode** in 1,261.044 seconds, zero skipped (`full-repaired.*`), alongside **89/89 EditMode**. This includes both saved safe halves and all 36 dialogue outcomes. Nathan's original save hashes and count remain unchanged.

Release and development players both build from **a3aaad6**. The earned release recapture (`chapter-08-organ-final`) loads the exact accepted `chapter-07-resupply-02` autosave through Title/Continue. No development loadout or edited state. It passes all cave fights, both organs, Bellows, the seven-line discovery and return lift in **443.249 s / 26,581 focused frames**. The final autosave contains both individual organ flags, Bellows/nursery/quest completion and both living level-six heroes (Taren 210, Sela 124.97 integrity), with five gels left. Opened frames 118/140 show the discovery, softened banks and no stale organ target. Captured audio/video are retained; continuous audition and physical-controller feel remain unverified.

Separate uncaptured release walking (`nursery-organ-performance-release`) uses the completed-cave fixture and passes the unchanged offline performance gate: **378.095 s / 22,684 focused frames**, p95 **16.968 ms**, p99 **17.0814 ms**, worst **37.7817 ms**. There is one frame over 25/33.3 ms at 8.288 s in the first approach, none over 50/100 ms, and no detected competitor. It is included, not filtered. The original water-build 152.8 ms hitch remains unresolved; these results do not identify its cause. The earlier water traversals remain records of their actual live-organ fixture defect.

The current [chapter package](../WORKSHOP_CHAPTER.md) contains **243 verified files / 616,521,326 bytes** from a3aaad6. A deliberately corrupted candidate is rejected before any replacement; restoring the exact bytes passes promotion. The prior 56a013e player and manifest are archived, with the chapter profile and earlier C7 package preserved. The packaged executable's complete New Game opening passes runtime/offline validation in **102.897 s / 6,158 focused frames** (`frozen-chapter-organ-opening`), exercising shop, fabrication, gear and manual saving. Nathan's original save hashes and count remain unchanged after the full suite, earned cave, clean walk and packaged opening.
