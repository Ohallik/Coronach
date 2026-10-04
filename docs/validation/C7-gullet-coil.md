# Gullet coil presentation — October 4

**Controlled and ordinary release recaptures pass; full map acceptance remains open.**
The full earned route exposed a broad blue floor and four remote, undersized
wall plates. Gameplay navigation passed while the place remained unconvincing.

The coil now has a recessed bed, raised muscular folds, a darker central hollow
and four recessed mooring seats. The generated membrane keeps continuous texture
coordinates through both transitions. Four purpose-built Compact moorings replace
the wall plates: bolted shoes, winch drums, receiver jaws and small status lights.
The complete machinery stays below the ships' collision clearance. Camera,
encounters, spawns, cache, valves and exit flags are unchanged.

The mooring went through an image design board, an inspected isolated reference,
Meshy T2 (4,056 triangles / 15 credits), external-texture cleanup and isolated
Unity intake. Live balance after generation: **2,808 credits**. The visible art
is owned generated output. [Prompts and provenance](../art/C7-coil-mooring.json).

## Review and rejections

- `gullet-relief-views-01` is rejected. Reused wall panels look like platforms,
  and blending absolute longitudinal UV scales produces a compressed band.
- `gullet-coil-views-02` catches uneven tissue cutting through the generated
  mooring's shoe. It also reveals stretched cells after the first UV correction.
- `gullet-coil-views-03` has continuous integrated UV density and flat pressed
  seats, but receivers face away from the chamber. Intake yaw is corrected.
- `gullet-coil-views-04` shows the complete shoes and receivers facing inward.
  These are controlled perspective views with the actual flight prefab for scale,
  not ordinary combat or a live Cantor encounter.

Two new layout tests fail on the original map: its bed bottoms out at -4 m,
and its small plates lie 44.5 m from the arena centre. The repaired map passes
all six Gullet checks, including the whole travel line, wall ribs and eddy.
Deliberately raising a mooring into flight clearance fails; raising it only
0.6 m fails its contact with the bed. Source and scene are restored afterwards.
The final combined Gullet, entry, boss visibility and camera run passes **15/15**,
zero skipped. No assertion is weakened.

`GulletBuilder.RefreshCoil` updates only this shell and these four moorings in
the existing scene. `GulletBuilder.Rebuild` produces the same geometry for a
deliberate isolated scene rebuild. Shared world preparation is never invoked.

The planned pre-combat side pocket and collar-release encounter are still C8
story/gameplay work. This presentation pass does not implement or claim them.
Full EditMode passes **69/69** and the full integrated PlayMode suite passes
**218/218**, zero skipped. Nathan's original save hashes match after restoration.

## Ordinary release recapture

`Builds/quality/workshop/c7-gullet-coil-01` resumes the exact accepted
`c7-enter-gullet-01` earned arrival save through Title/Continue. It passes runtime
and offline validation in **796.910 seconds / 47,796 focused frames**, reaching
TallowApproach with both level-six heroes alive: Taren 69.91, Sela 189.31 of 210
integrity. This is agent-directed virtual input, with no development loadout.

Codex opened 104, 113, 125 and 163. The approach remains sparse. During combat,
the darker recessed centre and surrounding folds are visible, the four complete
winch shoes are seated, and the full Cantor and both ships remain in view. The
exit prompt and narrowed passage are clear. The moorings still lack working
collar connections, and the planned preview pocket is absent: these remain
explicit C8 work, so this does not claim full `MAP_EYE_TEST_OK` or C7 acceptance.
The recording retains loading/first-use stalls and capture overhead; it is not
clean performance evidence. Continuous video/audio review remains UNVERIFIED.

[Raw artifact and source hashes](C7-gullet-coil-evidence.json).
