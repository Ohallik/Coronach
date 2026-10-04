# Landscape surfaces — October 3, Codex

**Landscape integration and affected traversal PASS; chapter acceptance remains OPEN.** D131 keeps the public clone complete and the licensed art private. [Setup](../OPTIONAL_ART_PACKS.md), [generated texture provenance](../art/P45-biome-surfaces.json), [raw evidence hashes](C8-landscape-evidence.json).

## Design and opened views

107 patches contain 4,943 low grass sprouts and four water surfaces across Sorrel, the proving yard, Hushwell and Tallow. Grass stays below 0.6 m and outside the authored travel and combat lanes. Dressing itself has no colliders. Tallow's raised recycling bed is outside the public aisle and preserves its starboard boundary route.

The nursery's three pools sit in sculpted basins about 0.3 m below their surfaces. Discovery and lift access stay dry and level. Sorrel's bore now has an actual depression under its scaffold instead of intact sand; its approach remains level. Navigation was rebuilt independently for all changed traversal maps.

Codex opened normal ground-camera views (40° pitch, 20° yaw, 19.5 m distance, 30° FOV) under `Builds/quality/workshop/biomes/`. `local-01` through `local-07` preserve rejected iterations: pale flat water, weak density and an oversized bore cut. `local-08` shows olive grass clumps, blue-green nursery pools with visible ripple/reflection detail, and contained Tallow water with the bed visible through its shallows. `public-01` caught missing shoreline data that turned entire fallback pools into foam; `public-02` shows corrected rims and darker interiors. The static editor images do not validate animation or continuous motion; NPC T poses there are editor state.

These additions improve surface variation and make the nursery's ecology more legible. They do not by themselves close C8's larger chapter, story and map-polish work. Later swamp, ice and volcanic direction is documented, not built.

## Public/private boundary

- Stylized Water 3 3.2.7 archive SHA-256: `d982a8b3fb60c94c3fe38ed2586064177e43c9fca56272298f55bc6939a3185a`.
- Stylized Grass Shader 2.1.0 archive SHA-256: `68f35a29039532368cb3196f8cbcbbbc8800e8e05f9acc194bb18a691aff1292`.
- Local installation: 641 water files and 400 grass files. Installer dry run, traversal/path exclusions and project-owner rejection were checked before installation.
- Public generated cutout and water texture use the existing Toon shader. No vendor source, texture, mesh, copied shader logic or derived material is serialized into a public scene. Private overrides and renderer features are selected through optional resource names.
- `Builds/quality/p5` is a current source export with no Library, local pack or override copied from the main project. Its build succeeds. The first launch had an OS package rename failure; that rejection is preserved at `regressions/cold-p5-package-rename-red.log`, and the rerun resolves/imports and builds. Earlier exports retain the Shader Graph GUID and package-resolution failures. Do not describe them as clean first-launch passes.
- The cold-import execute retry initially failed to recognize CRLF compiler logs. `scripts/test-package-retry.ps1` executes the actual catch policy: the archived old wrapper fails, the corrected one passes all eleven cases, including refusal to retry unrelated errors or retry twice. The public build proves no-pack compilation; automatic retry within a successful first cold launch remains a separate integration check.

## Red/green checks

Raw XML/logs are under `Builds/quality/workshop/biomes/regressions/`.

| Check | Broken evidence |
|---|---|
| Public surfaces in all four scenes | `surfaces-red`: four missing-surface failures |
| Missing local art leaves fallback visible | `basins-fallback-red`: hidden fallback failure |
| Nursery water occupies basins | `basins-fallback-red`: original floor sat above water |
| Bore cuts terrain, level approach | `basins-fallback-red`: intact sand at -0.1 m |
| Water tangent frames | `water-tangents-red`: zero tangents for 321 vertices |
| Public water shoreline data | `water-shoreline-red`: missing world distance to rim |

`all-69-editmode-green` passes the complete EditMode suite, including the shoreline check. The full PlayMode run reached 200/203; three dialogue completion/unload integration defects were repaired, and the combined affected classes then pass 44/44. [C6 details](C6-dialogue-input.md). A final full-suite rerun remains required for the workshop.

## Ordinary input and measurements

`tallow-local-01` is **REJECTED**, 177.4 s / 10,640 focused frames. Interior routes, Keeper conversation, repair, slice completion and garden-side boundaries passed. Its redocking target at z=-2.5 was inside the 1.78 m hero hull's collision clearance; the ship stopped at z=-3.474 with a live dock prompt and then docked successfully. The route now targets reachable z=-4.2 with the same tolerance, and explicitly asserts both return-to-refuge steps. The second capture below passes.

Final local-pipeline ordinary captures:

| Run | Result |
|---|---|
| `tallow-local-02` | PASS, 177.425 s / 10,641 focused frames, all 47 steps including redock and return. Opened garden and boundary views show contained teal water and clear access |
| `sorrel-service-local-01` | PASS, 260.149 s / 15,609 focused frames. Service encounters and repair return remain clear of low olive grass |
| `sorrel-bore-local-01` | PASS, 208.641 s / 12,514 focused frames. Four haul encounters and actual bore-to-Hushwell walk; starts from the cleared-Burrower fixture, not a Burrower fight claim |
| `hushwell-local-01` | REJECTED, 430.562 s / 25,831 focused frames. Clears descent and organs, then both heroes fall in Bellows phase 3. Nursery and lift are not reached. The stationary melee/ranged chase never dodged the expanded breath; a recapture with ordinary evasive input is required |
| `hushwell-local-02` | PASS, 410.834 s / 24,645 focused frames. D133 ordinary dodge input during Bellows rounds; all encounters/organs, 94.8 s Bellows fight, discovery and lift back to Sorrel. Opened views 103/104/107 show dry nursery and lift access around the pools |
| `yard-local-01` | PASS, 92.964 s / 5,579 focused frames. Three opponents, service lane, four corners, east boundary and operator corner |

`evade-tell-red` fails on the first missing dodge before implementation. `evade-tell-green` passes all three replay regressions, including actual damage deflection through the ordinary input path, release before the second tell and opt-in behavior. The runtime combat code and encounter balance are unchanged. Both Nathan save hashes still match the pre-session manifest after the recapture.

Tallow's capture includes 84.1/54.1 ms launch/redock fade frames and is traversal evidence, not a clean performance run. Opened Sorrel frames retain broad bare stretches: the surface pass improves them but does not accept all chapter polish.

Four separate uncapped, capture-free measurements of the private pipeline pass with full focus and no competitors. GPU p95 from the direct API and profiler agrees exactly: Decks release **1.896960 ms**, development **1.991424 ms**; Tallow release **1.273344 ms**, development **1.330432 ms**. All remain below 14 ms. See [C1 measurements](C1-station-performance.md). Continuous audiovisual review and physical-controller feel remain UNVERIFIED.

The dense Sorrel service route also passes a clean release headroom run after the
completion fixes: `sorrel-headroom-release-01`, **259.946 s / 221,548 focused frames**.
GPU p95 is **1.247744 ms** from both sources, active CPU **0.9178 ms**, render
**1.2474 ms**. The direct API has 213,084 distinct positive readings (96.18%),
8,464 pending rows, no repeated or impossible samples. Whole-frame p95/p99/worst
is 1.4232/1.5637/15.7015 ms. All service encounters and the repair return pass.
