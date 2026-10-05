# Nursery shoreline refinement

October 4, 2026. **Controlled visual and geometry checks pass; player validation is pending.** This follows the [generated nursery floor](C8-nursery-art.md), without claiming whole-map or C8 acceptance.

Opened fixed-camera images exposed hard blue pool edges and a cracked surface. Two causes were distinct: the licensed shader's former 0.18 setting produced only a 1.8 mm depth fade, and portions of the water mesh ended above the wet basin floor. The original geometry check fails at pool zero: surface -16.085 m versus bed -16.119 m. A wider fade alone could not fix the exposed mesh boundary.

The isolated `BiomeLandscapeUpgrade.RefreshNurseryWater` refreshes three public mesh assets and their generated fallback material. The meshes extend beneath the dry banks, with no added vertices, colliders, terrain or navigation edits. Every boundary vertex and midpoint is checked against the actual basin surface. The fallback uses the existing toon shader's transparency and baked shoreline blend. Scenes retain no licensed dependencies.

`LocalBiomeBake.Build` gives the optional licensed nursery water a 20 cm fade, 0.22 normal strength and 0.16 animation speed, without foam across its centre. Bank detail, refraction and reflections remain. The source material's obsolete `_AnimationSpeed` and `_FoamOpacity` names are removed from the recipe; current animation uses `_Speed`. Tallow retains its previous material values and contained cistern appearance. All licensed derivatives remain ignored.

Raw evidence is under `Builds/quality/C8/`:

- `nursery-floor-02`: original final-floor views, rejected for the hard/cracked water treatment.
- `nursery-water-01`: wider fade and quieter normals; cracked foam remains.
- `nursery-water-02`: surface foam removed; the exposed mesh border still cuts part of the bank.
- `nursery-water-03` and `nursery-water-final`: corrected submerged boundary; final also has the corrected slow animation parameter.
- `nursery-water-public`: rejected opaque fallback, whose polygonal basin intersection remained conspicuous.
- `nursery-water-public-02`: softened public fallback, opened at the same fixed camera.
- `nursery-water-checks/rim-original-red.*`: new boundary check rejects the original assets. `surfaces-green.*` passes all ten surface checks; `final-editmode-green.*` passes all 85 EditMode checks, zero skipped.

These static images establish the shoreline improvement, not continuous motion or audio quality. Standalone traversal and clean timing remain due; the frozen chapter package is still runtime 56a013e.
