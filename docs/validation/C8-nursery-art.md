# Nursery floor art

October 4, 2026. Controlled art and focused regression checks pass; ordinary-player
and clean timing validation remain pending. C8 and full map acceptance remain OPEN.

The nursery's precise concentric slab is replaced with a generated mineral sweep.
Three irregular arms and luminous grooves make the growing passage described by
Sela visible in the room. The fixed camera, egg cradles, discovery, lift, gameplay
objects, collision and baked navigation remain in place. The relief is only six
centimetres thick, starting one centimetre above the existing floor, so walking
feet agree with the surface. The isolated `RefreshNurseryFloor` entry point updates
only this decoration. Saving the scene also serializes the already tested
`SelaNursery`/`Sela` defaults on its discovery point.

[P46](../art/P46-nursery-groove.json) records the reference prompts, hashes and
generation ledger. Two Meshy attempts cost 30 credits; 2,778 remain. The first is
rejected in `Builds/quality/C8/nursery-model-01`: it made flower-like paving stones
and lost the continuous luminous channels. A simplified three-arm reference
produced the accepted shape. Its initial upright import is retained separately;
the intake records a -90-degree pitch for the horizontal floor.

The second raw model has 6,359 triangles. The 6,000-triangle check fails on that
original before the reproducible reduction in `tools/blender/p46_nursery_relief.py`
produces 5,949 triangles with UVs intact. The original, red/green reports and logs
remain under `nursery-model-02`. Opened isolated and in-cave renders establish the
shape and placement; they do not replace normal traversal evidence.

After the scene replacement, full EditMode passes **84/84** and the Hushwell,
opening-chapter and development-loadout PlayMode checks pass **15/15**, zero skipped.
Nathan's save hashes are restored exactly. These complement the full **226/226**
PlayMode pass and fresh **38:37 New Game-to-Tallow chain** immediately before the
decorative change, documented in [the integration report](C8-nursery-integration.md).

The first chained render/refresh launcher stopped because its owned render process
was still exiting. The one-owner guard prevented another Unity instance from
opening Lattice. After the process exited, the isolated refresh completed.

Still open: the cradles' repetitive construction, sparse Sorrel stretches and the
broader opening/ending story work. This improves the discovery's visual meaning;
it does not establish `MAP_EYE_TEST_OK` for the whole chapter.
