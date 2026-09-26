# CORONACH slice progress

## C0 baseline recorded — September 26 implementation

**C0_BASELINE_RECORDED.** Added continuous ordinary-input recording with per-frame timing, independent bone directions, actual game video/audio, isolated saves and fail-closed route validation. Recorded two 120-second town routes, civil docking/return, both Natural heroes in eight directions, both Shaped heroes in Arena/Sorrel, real dialogue/shop flow and combat down/revive. See [C0-baseline.md](validation/C0-baseline.md) and [C0-recordings.json](validation/C0-recordings.json) for artifacts, hashes, rejected attempts and limits.

Baseline recorder implementation and route checks pass; quality remains **FAILED/OPEN**. The first-station overhead/arrival comparison fails MAP_EYE_TEST. Functional interior/exterior briefs now exist, but no redesigned map is accepted. C1 clean timing and C2 calibrated motion remain open. Camera stops during roughly one third of captured walking samples; an independent constant-speed PlayMode regression fails on the old camera. A wall-contact regression also fails because the motor reports 6.7 m/s against a wall. Red XML/logs are preserved under `Builds/quality/C0/preflight/`.

Ten offline analyzer controls pass, rejecting relevant broken recordings. One Unity test launch exited without results and was rejected; its bounded retry produced the two expected failing regressions. Normal save and backup hashes remain unchanged. No art credits spent: 795/1,200 consumed, 405 remaining.

Codex opened actual dialogue/shop, gameplay, structural and rig stills. **Continuous normal/slow-motion observation, subjective audio audition and physical Logitech feel remain UNVERIFIED**: the native computer-use pipe was unavailable after bounded recovery. Captured artifacts are retained. This is a baseline checkpoint, not a completed workshop or campaign. Development includes the recorder; release will be rebuilt for C1 comparison and again at integrated handoff.

## Next production pass — September 26 findings

Nathan reports station-walking performance problems, characters running askew in combat form, weak sound effects and inadequate death animations. These are **OPEN**, despite the historical test/route results below. The previous benchmark covered the Gullet only; motion, dialogue and sound observations did not establish complete experience quality.

Additional feedback: the first station's construction/layout fails the eye test. **Q07 is OPEN.** The plan and next-session prompt now require MAP_EYE_TEST at blockout and final in-player stages for every release map, including an explicit first-station redesign covering room relationships, routes, supports and exterior/interior coherence. This pass updates the handoff only; no layout has been rebuilt or newly accepted.

[PRODUCTION_PLAN.md](PRODUCTION_PLAN.md) now defines C0–C10: reproduce the defects, fix station frame pacing and locomotion, complete combat/death/flight/audio/dialogue quality gates, validate the workshop, then build and verify the first chapter and connected campaign. [NEXT_SESSION_PROMPT.md](NEXT_SESSION_PROMPT.md) is the implementation handoff. The current session inspected code and wrote the plan; it did **not** apply these fixes, rerun runtime checks or rebuild the game. The playable build remains the September 23 implementation.

Start with C0. Record per-frame station traversal evidence and eight-direction Natural/Shaped motion for both heroes before choosing fixes. Do not treat the old Gullet FPS average or pose captures as rejection of Nathan's observations. Meshy remains 795/1,200 spent; this documentation pass incurred no generation cost.

## September 23 pass — campaign plan and presentation

2026-09-23: the full-game proposal is now in [STORY_CAMPAIGN.md](STORY_CAMPAIGN.md) and [WORLD_ATLAS.md](WORLD_ATLAS.md): migrating living routes, a convoy of towns, 36 planned destinations and 22 major boss encounters. These documents are plans; the existing Unity slice remains the gameplay workshop.

Story draft 2 makes the **Severance** the overarching campaign: get the growing convoy and Sorrel's nursery through the last shared passage before Stillwater closes it. Each main region now has a cause for visiting, a consequential victory and a later payoff. Nacre's disaster is introduced before it happens, recruited communities remain active through the journey, and the final operation depends on capabilities and relationships earned across the campaign. This is a documentation revision; the playable quest and validated build remain at the presentation-pass implementation.

The playable slice now uses native-resolution surroundings with the tilt blur removed, separate walk/run/sprint clips, a three-strike combo and distinct skill animations with delayed contact and buffered input. Both heroes have new generated spacecraft forms, banking/roll motion and a short form transition. Nathan's four assigned music cues play at the title, town, shops and moon combat, with crossfades. The three other originals are preserved for later locations. Retarget orientation, torso-vane attachment, companion defense and a ground-boss gravity defect were corrected during verification.

Both Windows builds are refreshed. Final verification and opened captures are recorded in [PRESENTATION_REVIEW.md](PRESENTATION_REVIEW.md) and [coronach-presentation.json](validation/coronach-presentation.json). **The professional-quality gate remains open:** bespoke attack choreography, stronger impact feedback, directional movement, environment composition and menus still need work. Ordinary virtual-gamepad review does not establish physical Logitech feel. [ANIMATION_DIRECTION.md](ANIMATION_DIRECTION.md) defines the next combat animation milestone.

Final tests: **35 EditMode + 28 PlayMode passed**, zero skips. The fresh full route passed in **542.1 seconds**, reached level 6, defeated Burrower in **45.7 s** and Cantor in **87.9 s**, docked at Tallow Drift and saved completion. This route includes the final companion/gravity corrections. The Logitech attached in both the route and release-title checks. The editor rendered 116 key-pose/ship views; opened evidence includes the motion/ship contact sheets, all 18 refreshed world captures, interactive town/ground/flight review frames, the final completion screen and the release title.

Final performance: native **1920×1080** Gullet, RTX 5070, at least twelve live enemies, 30.018 s / 26,333 samples. Uncapped mean **877.2 fps**, median **1.103 ms**, p95 **1.585 ms**, p99 **1.854 ms**. The performance capture was opened; normal gameplay retains the 60 fps cap. These measurements apply to this benchmark, not every future campaign map.

Generated production now contains **50 models**. The two new ships cost 30 Meshy credits; total spend is **795/1,200**, leaving **405 credits**.

## Product name — Coronach

2026-09-23: Nathan named the game **Coronach**. Product settings, the title screen and Windows packaging now use this name. The existing Unity project remains `Lattice/`; existing save slots remain in `Nathan/Lattice/Saves`. Earlier build records below retain their historical filenames. This naming change does not close the open visual-quality or controller-feel gates.

Rename verification: rebuilt release and development players; opened both 1080p title captures and confirmed the longer title fits. Both title runs attached the Logitech. The release recognizes the existing save, whose files remain byte-for-byte unchanged after verification. The focused all-zone save/resume regression passed (1 test, zero skips); the full route and suite were not rerun for this naming change. Evidence: [coronach-rename.json](validation/coronach-rename.json).

## Previous review — quality gate reopened

2026-09-21: **QUALITY_REVIEW_REJECTED** after agent-directed play of the opening, a ground encounter, flight combat and menus. The slice does **not** yet meet the professional presentation/feel target. Repetitive environment composition, weak combat silhouettes/feedback and sparse menus remain. The earlier screenshot acceptance below was too lenient and is superseded by [PLAY_REVIEW.md](PLAY_REVIEW.md).

This pass fixes menu-close input leakage, dropped lunge taps, combatant contact being treated as wall damage, flight-partner braking, paused pending attacks and several HUD/world readability issues. Fresh checks are recorded in `docs/validation/play-review.json`. These are corrections to the playable prototype, not a closed quality gate. The `slice-v0.1` tag and its measurements remain historical.

Final correction checks: **35 EditMode / 21 PlayMode** passed with zero skips. The route passed in **532.3 s**, reached level 6 and saved completion; Burrower 66.1 s / Cantor 87.8 s. The route precedes only the final buffered-lunge/menu cancellation guard, covered by the 21-test suite. All eighteen refreshed zone views, step-by-step review frames, final completion and release title were opened. Both Windows packages contain the corrections.

Virtual-gamepad play pauses between screenshot-based decisions. It does not establish physical Logitech feel or completion of the full release route by a human. P4 visual quality and P5 presentation/feel are open; no further approval is needed to improve them.

## Historical handoff — slice-v0.1

2026-09-21: all five zones and Tallow Drift's exterior approach are playable with generated art. All 48 production models, eight expression sheets, textures, skies and UI are imported and reviewed. Meshy spend is **765/1,200 credits**. No model ART_PENDING rows remain.

Both Windows packages contain the generated world. Fresh tests pass **35 EditMode / 17 PlayMode**, zero skips. The final full route passed in **545.8 seconds**, reached level 6 without crafted gear, defeated both bosses, docked at Tallow Drift and saved completion. Burrower measured **60.9 s**, Cantor **109.6 s**. Final world, UI, performance and release-title captures have been opened and accepted.

**P6_HANDOFF_OK**: release at `Builds/Windows/Lattice.exe`, source tag `slice-v0.1`. README, credits, decisions, playtest notes, art reviews and the committed validation snapshot are current. P2/P5 remain open only for the human feel sessions described below.

## Gates

| Gate | Status | Evidence / rejecting condition |
|---|---|---|
| P0_TOOLCHAIN_OK | CLOSED | Unity 6000.4.7f1, URP 17.4, Blender 5.1.2; compile/build, opened title, real Logitech attachment. Missing markers, blank title or absent hardware fail. |
| NATHAN_SPECIES_APPROVED | CLOSED | Actual response below and docs/art/species-approval.json; no inferred approval. |
| P1_DESIGN_OK | CLOSED | docs/art/DESIGN_REVIEW.md; opened species, distinct hero faces, cast, enemies, ships and environment references. |
| P2_CORE_OK | OPEN — human feel only | Core, controller translation, arena smokes and full route pass. The physical 60-second pad session has not been observed. |
| P3_ART_OK | CLOSED | docs/art/ART_REVIEW.md: all models, posed animations, hero forms, Cantor chain, 16 dialogue captures and corrected true-scale row opened. Blank/low-contrast renders, invalid rigs and frozen skin fail intake. |
| P4_CONTENT_OK | REOPENED — visual quality | Content and ordered route exist; ordinary play exposes sparse/repetitive spaces and weak scene composition. Prior WORLD_LOOK_OK acceptance is superseded by PLAY_REVIEW.md. |
| P5_PLAYABLE_OK | OPEN — presentation and feel | Concrete input/partner defects found and corrected during agent-directed play. Combat readability, menus, feedback and the physical controller session remain below/unverified against the target. |
| P6_HANDOFF_OK | CLOSED | Generated release built; title smoke/render and Logitech attachment pass. Documentation, validation snapshot, gate commits and slice-v0.1 tag delivered. Open P2/P5 human-feel requirements are disclosed. |

## NATHAN GATE

**NATHAN_SPECIES_APPROVED, 2026-09-20.** Nathan: "in general the designs of the race are fine. when we actually design the main characters they'll need to be more unique looking faces but that is okay for now. please continue".

- C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/species-sheet-v2.png
- C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/species-faces-v2.png
- C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/taren-board.png
- C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/sela-board-v2.png

Receipt and facial-distinctiveness requirement: D027–D028 in docs/DECISIONS.md. Taren's broad jaw, hooded eyes and nose scar differ from Sela's longer face, raised brow and cheek speckles in production references and portraits. No second design approval is required.

## Run

Double-click **Builds/Windows/Coronach.exe**. Keep it beside Coronach_Data, UnityPlayer.dll and the other runtime files. Select New Game with the bottom face button, dock at Orrin's office and follow the objective panel. At Tallow Drift, dock, talk to the keeper, then repair/save to finish.

Builds/WindowsDev/Coronach.exe contains development probes and diagnostics. Runtime shortcuts are disabled in release. [README](../README.md) has full controls, saves, verification commands and bug reporting.

The Logitech Precision C21A uses its d-pad for movement; **LB + d-pad** cycles targets/items. Synthetic events verify translation/navigation. PAD_BRIDGE_ATTACH vid=046D pid=C21A profile=LogitechPrecision proves real attachment in the graphics player. Neither proves human feel. Windows focus loss can temporarily disable the HID device; the bridge reattaches when enabled again.

## Historical validation evidence — slice-v0.1

The committed snapshot `docs/validation/slice-v0.1.json` records test counts, route/balance/performance measurements, opened captures, hardware identity, credit spend and the release package manifest hash. The P5 automated checkpoint is complete; the named P5 gate remains open for human feel.

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

The authoritative Meshy total is the sum of consumed_credits in docs/art/gen-manifest.json: **795 credits**, all paid tasks complete. Raw sources/prompts remain under ignored art-src/Generated/. Native 1254-square textures and 1774×887 panoramas are recorded honestly; five sky/maps have actual 4× Real-ESRGAN derivatives resized to 4096×2048, indexed in docs/art/world-upscale.json.

## Known limits / BLOCKED

No setup or generation blocker remains. No additional approval is requested.

- P2 and P5 physical controller-feel sessions have not been performed by a human. Attached hardware and automated play do not close them.
- This slice reuses civilian bodies for the unnamed survivor/keeper, has fixed diorama cameras and limited environment modules. Human pacing, comfort and subjective combat tuning remain review items.
- Automated saves are isolated under Builds/; runtime tests shield the user's default save directory. Default saves live at %USERPROFILE%/AppData/LocalLow/Nathan/Lattice/Saves.
- Gate commits: P3 f58bb93, P4 48c049f, P5 automated checkpoint 7ec944a; final handoff is tagged slice-v0.1. The P5 checkpoint deliberately does not claim its human-feel gate closed.
