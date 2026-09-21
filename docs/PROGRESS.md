# LATTICE slice progress

## Current work

2026-09-21: all five zones and Tallow Drift's exterior approach are playable with generated art. All 48 production models, eight expression sheets, textures, skies and UI are imported and reviewed. Meshy spend is **765/1,200 credits**. No model ART_PENDING rows remain.

Both Windows packages contain the generated world. Fresh tests pass **35 EditMode / 17 PlayMode**, zero skips. The final full route passed in **545.8 seconds**, reached level 6 without crafted gear, defeated both bosses, docked at Tallow Drift and saved completion. Burrower measured **60.9 s**, Cantor **109.6 s**. Final world, UI, performance and release-title captures have been opened and accepted.

## Gates

| Gate | Status | Evidence / rejecting condition |
|---|---|---|
| P0_TOOLCHAIN_OK | CLOSED | Unity 6000.4.7f1, URP 17.4, Blender 5.1.2; compile/build, opened title, real Logitech attachment. Missing markers, blank title or absent hardware fail. |
| NATHAN_SPECIES_APPROVED | CLOSED | Actual response below and docs/art/species-approval.json; no inferred approval. |
| P1_DESIGN_OK | CLOSED | docs/art/DESIGN_REVIEW.md; opened species, distinct hero faces, cast, enemies, ships and environment references. |
| P2_CORE_OK | OPEN — human feel only | Core, controller translation, arena smokes and full route pass. The physical 60-second pad session has not been observed. |
| P3_ART_OK | CLOSED | docs/art/ART_REVIEW.md: all models, posed animations, hero forms, Cantor chain, 16 dialogue captures and corrected true-scale row opened. Blank/low-contrast renders, invalid rigs and frozen skin fail intake. |
| P4_CONTENT_OK | CLOSED | Eighteen refreshed world views accepted; full ordered route and final completion save pass. Burrower 60.9 s / Cantor 109.6 s; crafted T2 reduces controlled duration by 30.5%. Fresh 35/17 suites pass. |
| P5_PLAYABLE_OK | OPEN — human feel only | Final-art 1080p performance, six-scene Continue, both arenas and UI pass. The human release-route controller session remains unverified. |
| P6_HANDOFF_OK | IN PROGRESS | Generated release built and title smoke/render passed with Logitech attached; handoff commit and tag remain. |

## NATHAN GATE

**NATHAN_SPECIES_APPROVED, 2026-09-20.** Nathan: "in general the designs of the race are fine. when we actually design the main characters they'll need to be more unique looking faces but that is okay for now. please continue".

- C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/species-sheet-v2.png
- C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/species-faces-v2.png
- C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/taren-board.png
- C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/sela-board-v2.png

Receipt and facial-distinctiveness requirement: D027–D028 in docs/DECISIONS.md. Taren's broad jaw, hooded eyes and nose scar differ from Sela's longer face, raised brow and cheek speckles in production references and portraits. No second design approval is required.

## Run

Double-click **Builds/Windows/Lattice.exe**. Keep it beside Lattice_Data, UnityPlayer.dll and the other runtime files. Select New Game with the bottom face button, dock at Orrin's office and follow the objective panel. At Tallow Drift, dock, talk to the keeper, then repair/save to finish.

Builds/WindowsDev/Lattice.exe contains development probes and diagnostics. Runtime shortcuts are disabled in release. [README](../README.md) has full controls, saves, verification commands and bug reporting.

The Logitech Precision C21A uses its d-pad for movement; **LB + d-pad** cycles targets/items. Synthetic events verify translation/navigation. PAD_BRIDGE_ATTACH vid=046D pid=C21A profile=LogitechPrecision proves real attachment in the graphics player. Neither proves human feel. Windows focus loss can temporarily disable the HID device; the bridge reattaches when enabled again.

## Validation evidence

- Fresh XML: Builds/logs/editmode-results.xml (35 passed) and playmode-results.xml (17 passed). The fail-closed runner rejects absent, stale, malformed, incomplete, zero-test, skipped or failed results, including hidden child failures. Its adjudicator accepted one good fixture and rejected eight bad fixtures.
- Runtime coverage: real attacks/projectiles, 8 m lunge, swap/revive, Flash, docks/locked warp, Yarn short-button taps, six menu pages, analog/digital pad separation, title Continue in all six world scenes, defeat/retry, safe-pocket teleport/swap, enemy patterns and civil/combat collision damage.
- Strict WorldBuilder.BuildFinal rejects missing generated prefabs. GULLET_WALLS_OK samples=714 checks both sides throughout the 900 m tube with physics rays at flight height; a collision gap fails.
- Final complete-art route: Builds/logs/route-generated-final-passed.log, completed-generated-final-save.json and route-generated-final-complete.png (opened). Every ordered marker passes, including the new Tallow approach/dock; both heroes are level 6 with no equipped gear, and sliceComplete=true is saved in TallowDrift. Total 545.8 s, Burrower 60.9 s / Cantor 109.6 s. This is automated regression timing, not human playtime. The earlier 568.6 s generated route is preserved under the corresponding generated-first filenames.
- Final controlled fabrication comparison: Builds/logs/balance.json. Four level-one combo hits kill a Ridgehound. Actual three T1 crafts then a T2 craft consume material/crystal inputs. At level three, +30 T2 Edge reduces the stationary 14,000-Integrity Burrower from 28.517 to 19.817 seconds (ratio 0.6949, 30.5% shorter). The prior 18,000 trial remains in balance-18000.json (36.018 / 24.017 s).
- Model renders: Builds/logs/renders/, including all 48 framed tiles, Idle/Walk/Attack, hero Flight/overhead, Cantor chain, contact sheet and true-scale row. Opened review/rejection details: docs/art/ART_REVIEW.md. The original blank far-clipped row is preserved as rejected evidence.
- Portraits: eight 2304×2304 sheets, full 576-pixel cells, actual 4× Real-ESRGAN processing and sixteen opened Neutral/Shocked dialogue panels. See docs/art/PORTRAIT_REVIEW.md.
- World look: Builds/logs/look/first-generated/ preserves earlier views; look/generated/ holds three second-capture 1080p views of each of six scenes, with review pages under review/. Review led to a tighter ground camera, quieter Gullet membrane, visible walls, planet-shadow correction and readable HUD.
- WORLD_LOOK_OK: docs/art/WORLD_REVIEW.md records all eighteen refreshed world views, eight UI screens and both final arena captures opened and accepted. Both arena smokes prove three kills, skills, swap and Flash; Flight additionally emits LUNGE_KILL. Real Logitech attachment was recorded in both runs.
- Final-art performance: Builds/logs/perf-gullet.json and opened perf-gullet.png. RTX 5070, 1920×1080, minimum twelve live enemies, 30.018 seconds / 26,860 samples: uncapped mean 894.8 fps, median 1.083 ms, p95 1.456 ms, p99 1.694 ms. The contract rejects fewer than twelve enemies, wrong scene/resolution, too few samples or mean below 60 fps. Ordinary gameplay is capped at 60 fps.
- Release title: Builds/logs/release-title.log and release-title.png (opened), TITLE_BOOT_OK / TITLE_SMOKE_OK and physical Logitech attachment. The independent release smoke uses its own log and save path. The 235-file, 433,085,062-byte package inventory and hashes are in Builds/logs/release-package-manifest.json.
- Historical blockout routes and rejected runs remain under Builds/logs/; their measurements are in docs/PLAYTEST_NOTES.md. They are not final-art performance evidence.

## Art and licences

All visible models, portraits, environment textures, panoramas, panels and icons are generated. Procedural continuous terrain and tube shell use generated textures. Allowed external resources are skeleton/animation donors, CC0 sounds and particle textures, and TMP fonts; each is listed in docs/CREDITS.md. No additional ART_EXCEPTION is used.

The authoritative Meshy total is the sum of consumed_credits in docs/art/gen-manifest.json: **765 credits**, all paid tasks complete. Raw sources/prompts remain under ignored art-src/Generated/. Native 1254-square textures and 1774×887 panoramas are recorded honestly; five sky/maps have actual 4× Real-ESRGAN derivatives resized to 4096×2048, indexed in docs/art/world-upscale.json.

## Known limits / BLOCKED

No current user-input blocker. Continue autonomously; no additional approval is requested.

- P2 and P5 physical controller-feel sessions have not been performed by a human. Attached hardware and automated play do not close them.
- This slice reuses civilian bodies for the unnamed survivor/keeper, has fixed diorama cameras and limited environment modules. Human pacing, comfort and subjective combat tuning remain review items.
- Automated saves are isolated under Builds/; runtime tests shield the user's default save directory. Default saves live at %USERPROFILE%/AppData/LocalLow/Nathan/Lattice/Saves.
- Final verification and slice-v0.1 tagging are still in progress; no handoff gate is claimed yet.
