# MAP_EYE_TEST — Hushwell (atlas map 04), blockout and first final build

2026-09-28, Claude Code. Brief: [maps/Hushwell.md](../maps/Hushwell.md), written before construction.

**Blockout PASS (agent review). Final build complete with the generated kit; the in-player pass is OPEN.**

## What was built

- **Layout.** `HushwellLayout` defines the plan: the route, ten rooms, two side passages, three levels (0, -8, -16) joined by two 26 m ramps, and a clearance function. The builder (`HushwellBuilder.Blockout` / `.Final`, isolated entry points) and `HushwellTests` both read it.
- **Terrain.** One heightfield (0.75 m grid) with floor, wall and cut-top submeshes. Cave walls follow the cutaway rule described in the brief.
- **Encounters.** Two Scrapmite packs and two Ridgehound packs. The Bellows chamber has entry and exit seals, two pressure organs, and B02, prototyped first on placeholder geometry and now on its generated model (below).
- **The pulse.** Six scalding floor vents and two breathing vent-membranes over side passages. The passages lead to the miners' cache and a crystal seam.
- **Nursery.** Five egg cradles around a spiral groove, the nursery discovery, and the lift back to Sorrel.
- **Connections.**
  - The Sorrel bore requires `bossdown.Burrower`, and Sorrel gains a `Hushwell` spawn.
  - Sorrel was rebuilt in blockout mode, matching its committed state. Its terrain asset was byte-identical apart from serializer whitespace and was restored.
  - Hushwell is registered in the build scene list.
- **Music.** Moon Caverns is staged; its hash matches the manifest, 0f5658b7….
- **Art.** Nine generated kit models (P40) cost 147 Meshy credits. Each passed a four-view Blender render before intake, and `GenIntake.BuildPrefix` staged only the Hushwell rows.

## Build-time route proof

Every bake runs the navigation checks and throws on failure: `HUSHWELL_ROUTE_BAKE_OK points=40 rock-samples=4034`. The checks cover:

- all route points, both passages and every room centre connect to the breach, with seals open;
- every ore node has a reachable stance within its interaction range;
- none of the 4,034 rock samples with at least 3 m clearance joins the walkable cave.

First-build failures, each fixed at the cause:

- The bore scaffold stood on the first route point.
- The closed Bellows exit carved the check's navmesh in the editor. Checks now run with obstacles disabled; the sealed state is a play-test assertion.
- Ore probes sampled at crystal-centre height instead of the floor.

## Checks, red before green

`HushwellTests`, five play tests:

1. **Levels, route and seals.** The party arrives on the upper level. The four sampled areas sit on their levels. The whole route before the Bellows is walkable. The entry is open and the exit sealed. The nursery is unreachable until the exit opens and reachable after. All five encounter ids exist.
2. **Sightline sweep.** From the gameplay camera's own pose, no terrain or grooved wall face hides a hero anywhere on the cave floor (2.5 m grid, over 600 samples).
3. **The pulse.** Vent-membrane collision follows the shared cycle and actually changes. An exhale scalds a hound standing in a plume and leaves one on the dry ledge untouched.
4. **Bellows bracing.** Hits are halved while either organ pumps and full once both are broken. Alien Boss Battle plays while the boss lives.
5. **Nursery and connections.** The nursery discovery sets `hushwell.nursery` once. The lift and breach climb go to Sorrel's `Hushwell` spawn, and Sorrel's bore is gated on the Burrower.

The runs, in order:

| Run | Result | Meaning |
|---|---|---|
| Before the scene existed | 0/5 | Red: scene not loadable |
| First blockout | 0/5 | Moon Caverns was not yet staged (`MUSIC_MISSING`); staged it |
| Blockout, music staged | 3/5 | The Sorrel bore was not built yet (expected). The seal check read `Membrane.Open`, which is runtime state not serialized in scenes, so the test now measures collision, the real state |
| First final build | 4/5 | **Sightline rejected**: four floor points hidden by grooved wall slabs. Straight 5 m slabs on curved bends poked their ends into the floor beyond the curve. The blockout passed only because its stand-in walls were thinner, so this check must run on final art |
| Second final build | 4/5 | **Sightline rejected**: a slab at the top of the nursery ramp hid a floor 3 m lower and more than 8 m away. Slabs now take a direct sightline test (ends and centre, 16 m along the camera, every floor level) |
| Third final build | **5/5** | Accepted |
| Mutation: organ shield removed, plume damage removed | 3/5 | Bellows test red (100 taken, expected 50); pulse test red (hound unhurt). Both mutations reverted and confirmed |
| Half-chamber blasts added (the atlas's "choose the safe half"), final boss model and dressing | 5/5, plus MusicCueTests 1/1 | Accepted |
| Mutation: every organ covers the whole chamber | Bellows test red ("Pressure organ east blasts the far half too") | Reverted and confirmed |
| Side-quest test added, before implementation | Red: "entering Hushwell does not start its side quest" | The cave also had no objective: the HUD fell through to the arena's practice text |
| Quest, HUD case and Survivor tier implemented | 5/6 | Killing the generated Bellows raised "mesh is not readable". Rigid death placement reads vertices, and the rule that makes enemy bodies readable lived only in a one-off upgrade tool. `GenIntake` now applies it at intake |
| Re-imported | **6/6** | Accepted |

Full suites: **56/56 EditMode and 171/171 PlayMode**, both before the half-chamber blasts and again on the final code, zero skips.

## Opened views

These are orthographic overheads plus perspective renders from the gameplay camera's pose and field of view, at 14 stations from the breach to the nursery. `Builds/quality/workshop/hushwell-blockout-01`, `hushwell-final-01`, `hushwell-final-02` and `hushwell-final-03` were opened and read.

- **Blockout.** The overhead shows the intended chain: breach, two galleries, the survey stretch, the ramp, three pressure chambers with the east and west passages and their membranes, the round Bellows chamber with both organs and the central slab, the curling descent and the nursery.
- **Final 01 rejected on look.** The floor rendered as bright orange desert sand under a strong key light, and did not read as underground.
- **Final 02.** Floor and rock were darkened and cooled, the key dimmed and ambient lowered. The cave now reads as underground; cyan grooves, vent throats, crystals and the violet egg cradles are the brightest things on screen. The survey stretch reads as a grid of inlaid spiral slabs beside grooved faces, and the nursery reads as a cradle ring around the spiral. The east vent reads as a membrane wall with a glowing throat beside it.

## Second art batch (P41): the Bellows and the dressing

- **Bellows Below.**
  - The reference came from image-to-image, using the Burrower and the kit board as style references: a limbless domed bellows-sac with rock-plated pleats over mauve folds, a skirt-foot, a grinning cyan maw and amber eyes. It is a deliberately different silhouette from the Burrower's worm.
  - Before 3D, the reference passed QA: one compact part, no shadows, no detached pieces.
  - It was generated at the hero band (15 credits) and passed the four-view render.
  - It was taken in as an enemy prefab bound to `EnemyDef BellowsBelow`. It needs no rig: `BossController` breathes it at rest (±3.5 %) and inflates it by 35 % on the inhale from the same rest scale, so the two never fight.
  - A review render with the model turned 200° shows its face toward the camera, so its face is +Z and it faces the hero it turns toward.
- **Floor props.**
  - A 3×3 board produced stalagmites, rubble, a crystal bed, a low ridge, a fallen lamp, a drill bit, Choir growth, hatched shells and crates.
  - The board was cut by the new `tools/isolate_board.py` (connected components in reading order; the cut fails if the count differs). All nine cuts were checked against their names.
  - They were generated at the prop band (9 × 15 credits) and all rendered cleanly, except the crystal bed.
  - The crystal bed carried a thin square backing plate: a separate 62-vertex part spanning the footprint, which would read as a dark square on the floor. The new `tools/blender/remove_base_plate.py` removes only a part that is thin, at the bottom and covering the footprint, and exits if exactly one does not match. The re-render shows the plate gone.
  - The low ridge's 15 % emission coverage was checked: its cyan groove is split across many texture islands, and the mask matches them exactly.
- **Placement.** Twenty-one pieces were placed, each at least 4 m off the route and 5 m from spawners, vents and ore. The bake's route and ore checks still pass. The crystal beds are now Hushwell's own ore nodes.
- **`hushwell-final-03`**, opened. Every area now has its own dressing: miners' leftovers at the breach, rubble and stalagmites in the galleries, low ridges and Choir growth in the pressure chambers, the boss on its spiral slab, and hatched shells toward the nursery.
- **Spend this session:** 321 credits (P40: 147; P41: 12 + 15 for the boss, 12 + 135 for the props; exactly the 4,157 → 3,836 drop). The balance is 3,836, confirmed live.

## Open

- **In-player pass**, awaiting a quiet machine. Needed: continuous traversal from Sorrel through the bore; combat in the galleries and pressure chambers with live vents; the Bellows fight read at gameplay scale; the nursery discovery and the lift ride.
- **Bellows Below** has its final model. Breath tuning from real fights waits for that pass, and so does the atlas's last phase, moving across exposed ribs.
- **Visual.** Large chambers are plain floor between their features. Steep rock faces show the planar-projected rock texture stretched. The toon shader scales point lights down, so mineral light pools read weakly. West-side walls are bare stepped rock by the camera rule.
- **Quest.** Hushwell is not yet part of the chapter-one quest or objective HUD; that waits for the quest rewrite.
- Listening, continuous video and physical feel remain UNVERIFIED.
