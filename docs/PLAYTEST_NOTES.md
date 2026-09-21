# LATTICE playtest notes

## 2026-09-20 — P2 automated arena review

Input source: scripted runtime actions and synthetic pad events in tests. Logitech Precision C21A physical attachment is verified in the visible Windows player. No human pad-feel session has been observed.

- Ground arena: three kills, skill, swap and timed Flash Move passed; second screenshot opened. Capsule blockout is expected at P2.
- Flight arena: fire, lunge, skill, swap and Flash Move passed. Review identified the cruise speed clamp truncating the requested dash; fixed with a timed dash branch and a PlayMode displacement assertion (7.5–8.5 m).
- Movement now follows camera yaw, so screen-up input moves into the screen.
- Repeated lunge hit volumes now share a victim set; an enemy takes one lunge hit, not five overlapping hits.
- Save loading initially duplicated default party members; a full two-member slot roundtrip exposed this. Collection replacement fixes it.
- Gear menus paginate all five fabrication disciplines, support second modules, upgrades and salvage, and retain selection after an action. Failed crafting reports its unmet requirements.
- Generated UI sprites are readable in the inspected built-player captures. Bloom required binding the URP renderer's post-process data in addition to camera and volume settings; the corrected ground capture shows the glow.

## 2026-09-20 — automated full route and render review

- First complete route: 395.2 s, every required ordered milestone, Logitech attached, completion saved at Tallow Drift. This is an automated regression route, not the intended human play duration.
- Rejected failures along the way: stale Yarn program missing Orrin; direct travel into a Decks divider; Flash marker before swap instead of after it; cache claimed twice after its collection trigger. Fixed source/route assertions and reran each.
- All fifteen zone angles were opened. The Gullet shell was invisible because its triangles faced outward. The corrected inward shell is visible in the twelve-enemy stress screenshot. Blockout geometry and missing portraits prevent a final visual pass.
- The route ended at level 7; XP is being reduced to target 6. Boss timing is now recorded and their initial low HP is being increased. These changes need a new route run.
- Physical Logitech C21A detection is real. Movement, attack, dodge and menu control in these runs are automated, so no claim of human pad tuning is made.
- Baseline Gullet performance: 1080p, twelve enemies for 30.018 seconds, 1,011.7 fps mean / 1.244 ms p95 on RTX 5070. Recheck after generated meshes, rigs and final effects.

## 2026-09-20 — regression checks

- Fresh fail-closed suites: 35 EditMode and 13 PlayMode tests passed, zero skips. Continue resumed all five zone saves with Sela active and the party/flags intact. Cantor body weak points moved in real frames. Short synthetic A taps advanced the real Yarn presenter without automatic advancement.
- Revised boss sample: Burrower 21.0 s, Cantor 97.8 s with the scripted Sela route. These are measurements, not closed balance targets. The run then failed because travel's 75-second timeout included combat. Travel now excludes nested combat elapsed time while retaining separate combat and overall deadlines; a fresh full run is required.
- Dialogue no longer loses short presses during text reveal delays. Combat timers stop during pause, and ending a Flash cannot unpause a menu.
- The 30,000-Integrity Burrower experiment was rejected: repeated partner knockdowns and a 150-second combat timeout. Archive: `Builds/logs/burrower-overlong-rejected.log`. Longer fights do not scale linearly from the short sample; balance remains open.
- The subsequent 18,000-Integrity Burrower completed in 96.3 seconds. Inspection found a route-driver defect: its 0.08 aim nudge was below GroundMotor's 0.1 facing threshold, so firing could continue in the old direction after a charge. Raise the nudge to 0.15 before using subsequent runs for balance. This changes automated steering, not player controls.
- Death/retry regression reproduced before the fix: `Builds/logs/retry-regression-red.xml` fails because departing dead actors overwrite the loaded save during the scene fade. Party state synchronization is now suspended while SceneFlow is loading; the full 14-test PlayMode suite passed afterward.
- Corrected-aim route passed in 549.9 s at level 6; Burrower 64.7 s, Cantor 99.5 s. Archive: `Builds/logs/route-aim-corrected-passed.log` and `completed-aim-corrected-save.json`. No equipment or fabrication was required.
- Added the specified pack lunge, Dart dive, Drifter three-shot volley and audible wind-up. The complete 15-test PlayMode suite passed, including a runtime check for post-wind-up displacement and three actual active projectile objects. Actor movement now reports the Move state.

## 2026-09-20 — final blockout checkpoint checks

- Both Windows packages rebuilt with the final enemy behaviors. The ordered route passed in 555.5 s at level 6; Burrower 75.9 s, Cantor 100.4 s. Archive: `Builds/logs/route-enemy-patterns-passed.log` and `completed-enemy-patterns-save.json`. Both heroes have no crafted equipment; completion is saved at Tallow Drift. The final completion render was opened.
- Both arena smokes passed again with the Logitech C21A attached: three kills, swap and Flash Move. Flight also emitted `LUNGE_KILL` and skill evidence. Both second-capture screenshots were opened. The 35 EditMode and 15 PlayMode suites are green with zero skips.
- Gullet stress rerun after the enemy changes: 30.019 s, 28,459 samples, minimum twelve enemies, 1080p on RTX 5070; 948.0 fps mean, 1.384 ms p95, 1.595 ms p99. The second capture was opened and shows the attacking enemies. Final-art performance remains unmeasured.
- Final release package title smoke passed with the physical Logitech attached; its second 1080p title capture was opened. No players or Unity editor jobs remain running at checkpoint handoff.

## Required later sessions

P2: 60 seconds on the physical pad with observations and adjustments. P5: complete release route with physical pad, 1080p performance measurement, fresh and mid-slice saves. These requirements remain open.

## 2026-09-21 — generated world, polish and performance

- All 48 generated models and eight portrait sheets are integrated. Eighteen final world views and eight UI screens were opened; review details are in `docs/art/WORLD_REVIEW.md`.
- Ground camera moved from 24 to 19.5 m; HUD frames shrank and the partner line moved inside its panel. The Gullet's wide empty-floor appearance was corrected with a 12–15 m radius, quieter generated membrane and generated walls at the camera edges. Decks, Halo, moon, Gullet and Tallow use their own grades.
- Generated-enemy placement exposed floating ground encounters; spawn at ground height. Teleport/swap exposed stale safe-pocket forms; evaluate the active member's position against the volume each frame. A runtime regression now covers both transitions.
- Attack animation now survives its recovery interval and retriggers with each attack sequence. Shaped wrist energy and flight/impact effects use the allowed CC0 particle textures. Civil Flight collisions bounce harmlessly; combat walls damage the actor. A real wall-contact test checks both outcomes.
- First complete-art route passed in 568.6 s at level 6, with no crafted/equipped gear: Burrower 89.5 s, Cantor 100.6 s. Completion was saved after the new Tallow exterior docking step. Archive: `route-generated-first-passed.log`, `completed-generated-first-save.json`, `route-generated-first-complete.png` (opened).
- Reduced Burrower Integrity from 18,000 to 14,000 after that sample. The next moving-boss route measured **60.9 seconds** with no crafted gear. Controlled level-three Taren trial on the revised boss: 28.517 s baseline, 19.817 s with an actually fabricated +30 T2 edge, ratio 0.6949 (30.5% shorter). Level-one Ridgehound remains four combo hits. No skills or partner damage enter this comparison.
- Final-art performance: RTX 5070, 1920×1080, at least twelve live enemies throughout 30.018 s, 26,860 frame samples. Uncapped mean 894.8 fps, median 1.083 ms, p95 1.456 ms, p99 1.694 ms. `Builds/logs/perf-gullet.json` and its second capture were inspected. Ordinary gameplay remains capped at 60 fps. This supersedes the blockout performance baseline.
- Fresh tests: 35 EditMode / 17 PlayMode, zero skipped; both final generated arena smokes pass and their second screenshots were opened. Logitech C21A attachment was recorded in both players.
- All six scene Continue checks pass. Default saves are protected by the test runner, and probes use isolated directories under Builds. No human pad-feel session is claimed; P2/P5 remain open for that observation even though the automated evidence is green.
- Final route after tuning: **545.8 s**, Burrower **60.9 s**, Cantor **109.6 s**, both heroes level 6 without crafted/equipped gear. All ordered markers pass, including Tallow approach, Civil Flight, dock, Natural form and completion save. `route-generated-final-passed.log`, `completed-generated-final-save.json` and `route-generated-final-complete.png` preserve the evidence; the completion capture was opened.
- Final release title smoke passes with the actual Logitech C21A attached. `release-title.png` was opened: generated key art, selected New Game and all four menu rows are visible, with no development overlay. The user's default save directory remains absent, as it was before the test shield; no test profile is left there.
