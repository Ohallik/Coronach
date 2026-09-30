# MAP_EYE_TEST — Hushwell (atlas map 04), blockout and first final build

2026-09-28, Claude Code. Brief: [maps/Hushwell.md](../maps/Hushwell.md), written before construction.

**Blockout PASS (agent review). Final build complete with the generated kit. In-player descent PASS (September 30), below.**

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
| Last-phase test added: no ribs in the scene | Red: "the Bellows chamber has no exposed ribs" | |
| Ribs built, boss logic not yet | Red: "the Bellows has no last phase" (phase 2 at 25 % health) | Both halves proven separately |
| Rib crossing implemented | **7/7** (the crossing reached a rib stance within 9 s) | Accepted |

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

## In-player descent (September 30)

`docs/quality/routes/hushwell-descent.json` plays the whole cave by ordinary pad input from the breach. It starts at level 5 with T2 parts (`loadout: gullet`), the state in which a player reaches Hushwell:
- both mineral galleries (Scrapmite swarms);
- the survey stretch and ramp;
- two pressure chambers with live vents (Ridgehound packs);
- the Bellows, organs first;
- the curling descent, the nursery discovery and the lift back to Sorrel.

Each capture ran alone as one blocking call on a freshly built release player.

| Run | Result | What it found |
|---|---|---|
| `hushwell-descent-01` | Rejected | Taren lost 160 health in 6 s to the first swarm and fell at the second. The swarm bit with the Ridgehound's own 18-damage `Bite`: six mites put twice a hound pack's pressure on the active hero. Also a route error: the breach is a safe pocket, so the hero is Natural there |
| `-02` | Rejected | Both galleries and chambers cleared. At the organs Taren swung at air: with nothing locked he kept aiming at the Bellows, and the organ is only broken once targeted. The Bellows is a Spitter that backs off to 5.6–12 m, so standing melee could never reach it |
| `-03` | Rejected | Organs locked and chased into reach: both broke, the Bellows fell, the nursery was found. The chase rounds used `until`, which is a requirement, so rounds 1–3 "failed" for the boss still living. The lift point stood 3.8 m from the lift, outside its 4 m prompt |
| `-04` | Rejected | Only the lift walk: the nursery is a safe pocket, so the Natural walk (about 1.8 m/s) needed more than 7.6 s |
| `-05` | Accepted by the runtime, then **not accepted** | Every check passed, but on arrival in Sorrel Sela lost 49 health in 4 s: the lift's spawn sits in the Burrower's arena, and the loadout had the warp key with the Burrower still alive |
| `-06` | Rejected | Taren fell in the last phase; Sela then chased to melee range, stood in the breath ring and fell with the Bellows at 272/12,000 |
| `-07` | **PASS** | 405.8 s, 24,345/24,345 frames focused, analyzer valid. All four packs cleared, both organs broken, the Bellows broken five times and killed, the nursery found, Sorrel reached with health unchanged. Taren fell in the last phase; Sela finished it from range and revived him |
| `-08` | **PASS** | The same result on the final code, with the survivor also met in the loadout: 405.8 s, 24,346/24,346 focused, analyzer valid, and "Return to the Halo" on arrival |

### Defects fixed, red before green

- **Scrapmite swarms** (D124). `SwarmPressureTests` computes each swarm's sustained bite from the scene's spawners (count × damage ÷ (tell + cooldown)) against the weakest Ridgehound pack in the same zone. It was red in Sorrel and Hushwell: 46/s against 23/s. The Scrapmite now has its own `Nip` (7 damage, 0.6 s tell, 1.8 s cooldown, 2.4 m reach), giving about 17/s. The death burst is unchanged.
- **The gullet loadout was not a reachable save** (D124). `LoadoutStateTests` loads Sorrel at the lift's `Hushwell` spawn with the loadout, then checks three things: no living Burrower, no damage on arrival, and a Sorrel objective reading "Return to the Halo". It was red three times, each half proven separately:
  - with only the key set;
  - with the Burrower's defeat but not its arena's clear;
  - without the survivor met.

  The loadout now carries all four flags.

### Slice smoke

The gentler swarms changed the smoke's Sorrel fights, and it failed twice on its marker contract: no `LUNGE_KILL`. Every Gullet kill was a projectile. The smoke switches the AI partner on once it has seen a flash move, and that now happens in Sorrel. From there the partner shot every 40-health Dart before the hero could close to lunge range.

The smoke already held its own flight skills until a lunge had killed, for the same reason. It now holds the partner in flight the same way. The marker contract is unchanged, and the next run passed: `ROUTE_MARKER_CONTRACT_OK`, `SLICE_SMOKE_OK elapsed=522.5 level=6`, with the first lunge kill in the first chamber.

Full suites on the final code: **56/56 EditMode and 183/183 PlayMode**, zero skips. `test_quality_analyze` 33/33. Nathan's saves were verified unchanged after every run.

### Replay and analyzer

The replay gained three player-shaped inputs (D124):
- `approachTarget`, with `rangedTolerance`: hold the stick toward the current target whenever it breaks away, and swing only in reach;
- `until: targetDown`: the target locked at the step's start has fallen;
- `stopWhen`: an optional early end, not a requirement.

`quality_analyze.py` treats them as the runtime does. Condition steps may end on their first frame, and moving goals are judged by the runtime's arrival record. Its tests gained two acceptance cases and two guards. The acceptance cases fail on the committed analyzer. The guards (an unsampled plain step, a missed fixed point) still fail as before.

### What the fight showed

- Face-tanking without a single dodge, Taren takes the Bellows through its first two phases on eight gels. The spit is 16 every 3.1 s; breaths strike at 22, scaled by phase.
- In the last phase the crossing lunges (24) and the widened breath put him down in every run.
- A player who rolls through the spit and steps out of the marked ring takes a fraction of this. The tuning is left as is: every attack is telegraphed and avoidable, and the replay cannot dodge.
- The route's evidence that the fight is winnable without dodging is Sela finishing it from 12 m. That is outside the first-phase ring, though still inside the widened last-phase ring.
- One 87.5 ms frame falls on the load into Sorrel. This is a capture run, so its timing is not clean-timing evidence.

## Open

- **Bellows Below.** Tuning against a player who dodges waits for a person's play; the replay's evidence is the no-dodge floor above. Opened top-down and oblique chamber views (`hushwell-final-05`) show the four ribs as curved ridges at the edge, clear of the organs, columns and growth.
- **Entry from Sorrel by ordinary input** (the bore walk) is covered by the slice smoke's scripted travel, not yet by an ordinary route.
- **Visual.** Large chambers are plain floor between their features. Steep rock faces show the planar-projected rock texture stretched. The toon shader scales point lights down, so mineral light pools read weakly. West-side walls are bare stepped rock by the camera rule.
- **Quest.** Hushwell is not yet part of the chapter-one quest or objective HUD; that waits for the quest rewrite.
- Listening, continuous video and physical feel remain UNVERIFIED.
