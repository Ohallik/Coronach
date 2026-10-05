# Pressure-organ save and revisit repair

October 4, 2026. **Automated validation PASS; player recapture pending.** The nursery-water player capture exposed a live pressure-organ target in a supposedly completed cave. Inspection found a general revisit defect: organs had no saved state and ignored Bellows completion. This affected real saved progression as well as development fixtures.

Four cases fail on the original runtime, retained in `Builds/quality/C8/organ-persistence-checks/original-red.*`:

- A manually saved broken west organ reloads pumping; its opposite half must remain live.
- Defeating Bellows leaves the remaining organs pumping.
- An older completed cave restores live organs despite its boss-completion flag.
- The completed-cave Gullet fixture contains live organ targets.

Each authored organ now has a stable `hushwell.organ.west` or `hushwell.organ.east` flag. A normal break records that flag and retains the existing collapse/effects. Loading a broken organ restores its zero integrity and collapsed pose quietly. Bellows completion settles both immediately, and its existing completion flag also restores older cleared caves without inventing individual organ history. The operation does not emit organ damage/death events or award anything on load.

`HushwellBuilder.RefreshOrganSaveKeys` adds only the two scene identities; future isolated cave builds author the same keys. Save version remains two because the optional flags are compatible with existing saves. Standard save/checkpoint ownership is unchanged.

The affected chapter/cave checks pass **18/18**. The independent-half save/reload check was then extended to exercise both west and east; the full **85/85 EditMode** suite passes. All three new persistence tests and the strengthened fixture check also pass in the first integrated PlayMode run.

Two full runs remain **REJECTED, 228/229 each**, zero skipped (`full-first.*`, `full-second.*`). The existing all-dialogue branch test threw `System.IO.IOException: Unable to remove the file to be replaced` from `SaveSystem.Save` / `File.Replace`, at different points in its unique temporary C6 branch directory. Its unchanged isolated repeat passes all 29 compiled nodes / 36 outcomes (`dialogue-isolated-green.*`). A separate OS-handle probe reproduces the exact error by holding the backup without delete sharing; the original interfering process is unknown. [Replacement repair](C6-save-replacement.md) is being validated without altering the dialogue test. Nathan's original save hashes and file count are restored exactly after both full runs.

After the bounded save replacement repair, the integrated suite passes **229/229 PlayMode** in 1,261.044 seconds, zero skipped (`full-repaired.*`), alongside **89/89 EditMode**. This includes both saved safe halves and all 36 dialogue outcomes. Nathan's original save hashes and count remain unchanged.

Rebuilt players and the earned cave recapture remain pending. The earlier water traversals remain valid records of their actual runtime, including the live-organ fixture defect; they do not establish a quiet completed cave.
