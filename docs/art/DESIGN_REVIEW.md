# P1 production design review

P1_DESIGN_OK — 2026-09-20. All twenty downstream boards/references generated with the session image tool, opened and reviewed against the prompt-craft reject list. Species approval and hero facial-distinction feedback are recorded in `species-approval.json`. These are design references; no model or in-engine art gate is implied.

| Design | Review |
|---|---|
| [orrin](../../art-src/Generated/P1-production/refs/orrin-v2.png) | White background, matte colors, separated five-digit hands, clear arm and leg gaps; accepted NPC design reference. |
| [mira](../../art-src/Generated/P1-production/refs/mira-v2.png) | White background, matte colors, separated five-digit hands, clear arm and leg gaps; accepted NPC design reference. |
| [hal](../../art-src/Generated/P1-production/refs/hal-v2.png) | Inspected: matte NPC shared-body variant, white background and separated hands/limbs. Hal pale plume gap; Neve black plume and white sync. |
| [neve](../../art-src/Generated/P1-production/refs/neve-v2.png) | Inspected: matte NPC shared-body variant, white background and separated hands/limbs. Hal pale plume gap; Neve black plume and white sync. |
| [sentinel-husk](../../art-src/Generated/P1-production/refs/sentinel-husk-v2.png) | Inspected: opaque white, distinct upright shield silhouette, connected limbs and clear gaps. Humanoid rig probe still required. |
| [ridgehound](../../art-src/Generated/P1-production/refs/ridgehound-v2.png) | Inspected all four paws and leg gaps, aligned head, rooted crest, tail clear of legs, matte colors, white field. |
| [scrapmite](../../art-src/Generated/P1-production/refs/scrapmite-v2.png) | All six legs and feet now visible and separated; matte amber eye; no detached parts or scenery. |
| [burrower](../../art-src/Generated/P1-production/refs/burrower.png) | Inspected: connected stocky drill grub, distinct three-petal maw and segmented taper, complete silhouette on white; static procedural-motion lane. |
| [chorister-dart](../../art-src/Generated/P1-production/refs/chorister-dart.png) | Sharp dart silhouette, two rooted fins with clear gaps, one aligned eye; matte single view on opaque white. |
| [chorister-drifter](../../art-src/Generated/P1-production/refs/chorister-drifter.png) | Bell silhouette, three visible nozzle mouths and five attached separated tendrils; matte opaque white. |
| [shellmine](../../art-src/Generated/P1-production/refs/shellmine.png) | Six attached blunt spikes, distinct compact mine silhouette, matte white-field single view. |
| [cantor](../../art-src/Generated/P1-production/refs/cantor.png) | Accepted full-creature concept: long connected serpent, three body weak points, distinct from Burrower. Generate isolated head/body/tail references before Meshy; this curved full body is not a model input. |
| [hauler](../../art-src/Generated/P1-production/refs/hauler.png) | Broad connected cargo lobes and central cockpit, clear smooth civilian silhouette, no loose cargo or scenic background. |
| [skiff](../../art-src/Generated/P1-production/refs/skiff.png) | Single compact tapered civilian craft, attached vanes, no gloss/ground, strong distinction from hauler and cutter. |
| [cutter](../../art-src/Generated/P1-production/refs/cutter.png) | Twin-prong patrol silhouette with open central white slot, connected smooth hull, complete framing. |
| [sorrel-environment-board](../../art-src/Generated/P1-production/refs/sorrel-environment-board.png) | All eleven requested Sorrel concepts present: two ground tiles, three ridges, two rooted crystal clusters, anvil, hab, drill, pad. Board only; isolate subjects before Meshy. |
| [halo-environment-board](../../art-src/Generated/P1-production/refs/halo-environment-board.png) | Six concepts inspected: two ring variants, three distinct docking pods and rooted split beacon. White gutters; board only, isolate before Meshy. |
| [decks-environment-board](../../art-src/Generated/P1-production/refs/decks-environment-board.png) | All six modular interior concepts present, connected structural parts and open doorway. Board only. |
| [gullet-environment-board](../../art-src/Generated/P1-production/refs/gullet-environment-board.png) | Two open wall variants, rooted Choir pod and restrained membrane design inspected. Final corridor placement must preserve camera visibility; board only. |
| [tallow-environment-board](../../art-src/Generated/P1-production/refs/tallow-environment-board.png) | Complete connected refuge-station hull, distinct oval habitat and docking arm; matte single view on white. |

Existing generated material/map references complete the environment set: [Sorrel ground](../../art-src/Generated/P3/refs/sorrel-ground.png), [Sorrel rock](../../art-src/Generated/P3/refs/sorrel-rock.png), [Decks panels](../../art-src/Generated/P3/refs/deck-panels.png), [Gullet membrane](../../art-src/Generated/P3/refs/gullet-membrane.png), [Vorun map](../../art-src/Generated/P3/refs/vorun-map.png), [Sorrel map](../../art-src/Generated/P3/refs/sorrel-map.png). Skies are also already generated; native resolution is recorded honestly in the image manifest.

Every prompt was written before generation. `design-catalog.json` records accepted paths and QA notes; `image-manifest.json` also preserves rejected versions. Early NPC references needed opaque background and finger-gap corrections. The Scrapmite needed all six feet made visible. Isolate each environment-board cell with the image tool before Meshy; never feed a multi-object board into reconstruction. Cantor requires isolated head, body and tail inputs.

Hero production inputs and portraits will retain the approved species/palettes while applying distinctive face shapes/markings. Their acceptance requires five clearly separated digits, no external ears, and aligned front A-pose. The first T2 humanoid must pass rigging, Unity avatar/motion gates and actual render review before batching.

Instrument: this inventory fails on any missing reference or absent recorded visual review. The visual judgments remain explicit human-readable notes; file presence alone is not quality evidence.
