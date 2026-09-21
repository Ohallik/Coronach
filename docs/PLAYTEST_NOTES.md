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

## Required later sessions

P2: 60 seconds on the physical pad with observations and adjustments. P5: complete release route with physical pad, 1080p performance measurement, fresh and mid-slice saves. These requirements remain open.
