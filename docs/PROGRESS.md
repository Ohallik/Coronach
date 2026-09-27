# CORONACH slice progress

## C3 ground skill contact checkpoint - September 26

**C3 OPEN.** Ember Dash now follows real collision-limited travel and its posed wrist edge; Pulse contact follows its visible expanding crest; Static Net visibly leaves the throwing hand and stops at cover. Pending casts cancel, released effects finish, pause holds them, and field/wave surfaces respect cover. Red tests reject future-path damage, teleported Net, hidden close-hand releases and the padded texture's misleading radius. Sentinel's redundant pellet activation bursts are removed while its persistent armor cue remains. [Ground-skill evidence](validation/C3-ground-skills.md) retains failed attempts and exact hashes.

Fresh integrated suites pass **77/77 PlayMode and 39/39 EditMode, zero skips**; both Windows players rebuilt with BUILD_OK. Final ordinary release routes pass 30 skill and 22 armor checkpoints, all samples focused. Opened frames establish readable seed/wave/field, posed dash, Net in combat and armor cleanup. The named Pulse encounter step happens after the clear; its actual contact is established by tests. Original saves remain unchanged. Continuous normal/slow viewing, audio audition and physical feel remain UNVERIFIED. Refract/buffs, other C3 work, non-station MAP_EYE_TEST and later gates remain open.

## C3 ordinary recoil and armor checkpoint - September 26

**C3 OPEN.** Sentinel now has a short torso recoil distinct from full break, with smaller motion while armored and unchanged walking-leg stride. Its protective ring persists for the actual armor state and clears on expiry, break or death. Stronger motion exposed a one-frame pause advance; immediate animator pause notification fixes the unchanged regression. Failed donor-only attempts, fixture corrections and visual observations are retained in [hit/armor evidence](validation/C3-sentinel-hit.md).

Fresh integrated suites pass **69/69 PlayMode and 39/39 EditMode, zero skips**; both Windows players rebuilt with BUILD_OK. The ordinary release route passes 22 checkpoints with 3,257 focused samples: Scatter Bloom activates armor, companion melee breaks it, the encounter clears and the party returns. Opened frames show the recoil, persistent ring and break/death cleanup. The existing activation bursts overlap noisily under multiple pellets and still need reduction. Original saves remain unchanged. Continuous normal/slow viewing, audio audition and physical feel remain UNVERIFIED. Other C3 work, non-station MAP_EYE_TEST and later gates remain open.

## C3 settled corpse checkpoint - September 26

**C3 OPEN.** Sentinel's terminal pose now settles its raised legs instead of hanging from an extended arm. A new side-view/height control rejects the former pose. Baked mesh calibration and a gradual visual tilt lower the hips from 0.708 m to 0.472 m; real mesh clearance stays at 0.012 m on flat and both 10-degree slopes. The targeted suite passes **4/4**, including hero down/get-up, grounded break/recovery, stable hold and pause. [Corpse evidence](validation/C3-sentinel-corpse.md).

The rebuilt release player passes 23 ordinary-input checkpoints with 3,744 focused samples. Opened collapse/terminal/cleanup/return frames confirm the specific correction. Saves remain unchanged. Full suites and the development build still predate this targeted correction and will be refreshed at the next integrated checkpoint. Normal/slow motion viewing, audio audition and physical feel remain UNVERIFIED. Other C3 work, four non-station maps and later gates remain open.

## C3 Sentinel reaction checkpoint - September 26

**C3 OPEN.** Sentinel now holds an inspected recoil pose while broken and returns on recovery. A red body test rejects ordinary idle during break; the new live pose moves its head 19.6 cm, holds through pause and recovers without moving the gameplay root. Actual generated-mesh support corrects 14.8 cm of idle penetration. The first support attempt incorrectly hit its own capsule; that rejection and a new height bound are retained. Revised support ignores combatants, passes actual-world sole checks and 484 independently cross-checked pose samples. Intake preserves the controller/prefab GUIDs. [Reaction evidence](validation/C3-sentinel-reaction.md).

Fresh integrated suites pass **64/64 PlayMode and 39/39 EditMode, zero skips**; both players rebuilt with BUILD_OK and original saves remain unchanged. The 23-checkpoint release route passes with 3,738 focused samples and a real encounter clear. Opened frames establish break-to-defeat behavior, not recovery: the companion breaks the enemy early and it dies before the named recovery steps. The terminal corpse appears propped off the floor by an extended arm and is **rejected for further correction**, despite its lowest point passing the floor bound. Full C3 choreography/reactions, non-station spatial work and later gates remain open. Continuous normal/slow viewing, audio audition and physical feel remain UNVERIFIED.

## C3 enemy-break interruption checkpoint - September 26

**C3 OPEN.** Red tests reproduce post-break damage from a pending contact, retained enemy warnings and immediate untelegraphed recovery attacks. Break now cancels pending enemy contacts and attack work, drops Sentinel's shield and requires a fresh tell. Both bosses' special attack state also cancels on break after a separate red regression. Released projectiles retain their intended flight. The combined focused checks pass **6/6, zero skips**; normal saves restore unchanged. [Break evidence](validation/C3-enemy-break.md) preserves exact red/green traces. This is a source checkpoint: fresh full suites, package rebuilds and reaction presentation remain pending; existing players still contain `ec0ac23`.

## C2 frame-rate checkpoint - September 26

**C2 OPEN; measured free-travel coverage now passes at 30/60/120 fps for both heroes/forms.** Four new ordinary virtual-gamepad recordings retain focus throughout: Natural/Shaped at 30 fps have 6,695/7,011 samples and measured 33.332 ms median intervals; at 120 fps they have 26,540/27,790 samples and 8.335/8.336 ms intervals. The unchanged direction/cycle/contact analyzer passes all four, with 133/147/151/236 measured stance contacts and no worse than 3.27 mm marker drift. Opened stills cover both heroes at each rate. [Frame-rate evidence](validation/C2-frame-rates.md) distinguishes these calibrated markers from the independent rendered-mesh regressions and retains exact artifact hashes.

The packaged implementation remains `ec0ac23`, with 58/58 PlayMode and 39/39 EditMode integrated results; this checkpoint changes documentation only. Continuous normal/slow video observation, audio audition and physical feel remain UNVERIFIED. Separate-project player activity excludes these captures from clean C1 timing. Lock-on/form-transition review, other C3 work, non-station maps and later gates remain open. Original save hashes remain unchanged.

## MAP_EYE_TEST - Cinder final spatial pass, September 26

**Hub_CinderHalo and Hub_Decks PASS spatial review.** The final three-dock circuit and full public/service/housing walk now pass with uninterrupted focus on the rebuilt `ec0ac23` release player. Opened actual hatch views confirm closed lower-casing joins; paired exterior/interior comparisons and room views establish attached hulls, matching airlocks, usable public/freight routes, repair/stores, common space and bounded cabins. Both heroes complete the interior return/swap/boundary checks, with real Mira dialogue/shop input. [Final spatial review](validation/station-final.md) and [exact evidence](validation/station-cinder-final.json) preserve prior rejections and reviewer authorship.

All four station maps now pass spatial MAP_EYE_TEST. **C1 and C7 remain OPEN**, as do the four non-station workshop maps and later production gates. These captured runs are not clean timing. Continuous normal/slow video observation, audio audition and physical-controller feel remain UNVERIFIED. Current integrated suites are 58/58 PlayMode and 39/39 EditMode, both Windows players are rebuilt, and original save hashes are unchanged. No new art spending or music changes.

## C3 moving-fire candidate - September 26

**C3 OPEN.** Sela's upper body now fires while her legs retain the measured locomotion stride. An independent rendered-sole check rejects the former planted pose (1.8 mm excursion) and passes the new layer at walk/run/sprint, with eight/twelve measured stance contacts and less than 1.2 mm maximum drift. Existing posed release, cover, cancellation, pause and down/revive checks remain green. The first integrated run fails one legacy resumed-melee fixture; standalone/ordered diagnostics pass, and its exact failed pose remains unknown. The fixture now uses a clear explicitly faced pair, retaining the 0.28 s deadline and all assertions. The second full suites pass **58/58 PlayMode and 39/39 EditMode**, zero skips. Both players rebuilt with BUILD_OK. The ordinary-input release route passes all 31 checkpoints with continuous focus; opened frames show stepping legs during fire and the thin revised beam. Those stills do not establish continuous-motion quality. [Moving-fire evidence](validation/C3-moving-fire.md).

The first test cleanup refused restoration and safely retained the original save stash. Codex verified its hashes, quarantined the test fixtures, and restored both originals unchanged. Subsequent automatic restoration passes. The launcher now rejects leftover human stashes before another test run and records process identity on cleanup refusal; a synthetic control verifies rejection without launching Unity or changing saves. Cinder final MAP_EYE_TEST, fresh C1 timing and later gates remain open. Normal/slow video observation, audio audition and physical feel remain UNVERIFIED.

## C3 ranged contact candidate - September 26

**C3 OPEN.** Sela's ground needles and Scatter Bloom now release from the posed left wrist. Thread Lance follows a visible beam that stops at static cover and contacts each exposed body once. Red regressions measured the old ordinary/fan origins 0.751/1.137 m from the hand and demonstrated damage through cover. Seven focused ranged checks now pass with 8 cm origins, pause/resume, defensive/death cancellation, short-target contact, covered beam and single piercing contacts; the preceding combined melee/lifecycle/ranged run passed 13/13. The full integrated suites pass **57/57 PlayMode and 39/39 EditMode, zero skips** (`C3/ranged-integrated-01`), including the final station seam geometry; both players rebuilt with BUILD_OK. The 31-checkpoint release ranged route passes with continuous focus. Opened stills confirm the hand attachment but expose a flat beam treatment and a whole-body firing pose during movement, which remain to correct. Normal save and backup hashes remain unchanged. [Ranged evidence](validation/C3-ranged.md) preserves the failing cases and limits.

The latest Cinder docking recapture (`final-docks-06`) opens the corrected casing join successfully but loses focus during the third dock while a separate project's Unity build is active. It remains rejected; Cinder final MAP_EYE_TEST and fresh C1 timing are still open. Normal/slow video observation, audio audition and physical Logitech feel remain UNVERIFIED. No new art spending or music changes.

## Station pressure/lift checkpoint - September 26

**TallowApproach and TallowDrift pass spatial MAP_EYE_TEST; Cinder final recapture and C1 remain OPEN.** The complete Tallow release route passes all 47 checkpoints, including both heroes, Keeper dialogue, repair/save, launch and redock. Opened unlabelled comparisons and player stills show the aligned shaft/lower cabin, attached apron, bounded refuge, service cells and accessible repair/rest areas. The first station's public/service/housing walk also passes, but closer hatch inspection exposed a lower-casing/bridge gap. That join is now closed in both maintained Cinder scenes and opened renders. Final ordinary-input recapture is pending after focus loss during repeated external Unity activity; rejected traces are retained.

Fresh integrated regression passed **50/50 PlayMode and 39/39 EditMode, zero skips**, before the final lower-casing adjustment (`pressure-integrated-01`). After that geometry-only adjustment, **3/3 station regressions pass**, zero skips (`pressure-integrated-02`); both players rebuilt with BUILD_OK. The full suite has not been rerun after the last seam adjustment. Normal autosave and backup hashes remain unchanged. No new art credits or music changes. [Spatial review](validation/station-final.md), [earlier rejected geometry](validation/station-final02.json) and [packaged evidence](validation/station-pressure.json) keep actual observations separate from route results. Continuous normal/slow playback, audio audition and physical Logitech feel remain UNVERIFIED.

## C1/C2 implementation and station blockouts - September 26

**C1 OPEN; C2 OPEN; final MAP_EYE_TEST OPEN.** The four clean before-runs reproduce first-dialogue hitches around 60-66 ms. CPU profiling identifies first-use Yarn preparation/JIT; the runner now prepares during the scene fade. The camera dead-zone stop/go and commanded-speed-against-wall regressions pass after targeted fixes. Four independently calibrated hero/form tests reject the original sideways run and pass clip-specific import corrections. See [C1 evidence](validation/C1-station-performance.md) and [C2 evidence](validation/C2-locomotion.md); these are partial corrections, not completed quality gates.

The paired Cinder/Decks blockout replaces disconnected ring ornaments and floating pods with three enclosed occupied hulls, a joined utility keel, public and service bridges, actual cabin volumes, galley, market stock and receiving/repair. Tallow now has visible pressure boundaries, waiting space, stores and bounded rest cabins. Codex reviewed actual unlabelled overhead/section/arrival renders and in-player checkpoint images. The full party walk, all three Cinder dock pairs, and Tallow repair/save/launch/redock routes pass. The redesign exposed a stuck companion; baked navigation now routes it through doors using the existing motor, with a red/green regression. [Blockout review](validation/station-blockout.md) records the failures and resulting checks.

**Blockout spatial review PASSED; final generated-art review pending.** Both players were rebuilt from the integrated candidate after the fresh suites and now contain generated station surfaces, enclosed cabins, attached dock aprons and Tallow's corrected exterior envelope. Final recorded routes completed, but inspection exposed an uncollided doorpost. Its unchanged red/green test now passes with separate jamb/lintel collision and rebaked navigation (2/2 including companion following); current clean ordinary-input runs include that correction. A fitted cover now hides Tallow's malformed roof patch in opened editor views; a quieter floor finish and recessed generated pressure backing now pass opened editor views; final player recapture remains pending. No workshop/campaign acceptance is claimed. [Map coverage](maps/README.md) now includes open briefs for all eight implemented workshop zones.

The rebuilt development Decks and Tallow each pass first/warmed timing laps at native 1080p: Decks worst 25.206 ms, Tallow 19.883 ms, neither has a frame over 33.3 ms. Release first/warm comparisons also pass: Decks worst 25.342 ms, Tallow 20.300 ms, with no frame above 33.3 ms in any of the eight development/release laps. The first ten-minute release Decks run also passes all five laps (628.922 s, worst 25.370 ms) with stable warmed memory; native object/voice counters are unsupported in release. Tallow headroom passes; Decks CPU work is below target but two impossible GPU counter values invalidate its GPU evidence. The Tallow stability run is rejected for one lost-focus frame; source/object census runs remain pending. Clean timing now also detects competing Unity players, including the other project's running game. Continuous footage/audio are captured but normal/slow playback, subjective mix audition and physical Logitech feel remain UNVERIFIED because the native pipe is unavailable. Clean timing refuses concurrent editor/build/encoder work; another project's processes remain untouched. Saves and music are preserved; generation spending remains 795/1,200 credits.

The latest integrated source passes **39/39 EditMode and 38/38 PlayMode**, zero skips (`Builds/quality/C2/integrated-01`). The calibrated contact driver, deliberate lock-on heading, bounded turns, short physical start/braking steps and idempotent form refresh are installed. The first ordinary Natural loop completes, while the Shaped loop is rejected for focus loss and safe-pocket form changes. Revised routes require intended forms, measured ground heights and complete torso cycles. Video readback now uses bounded reusable buffers; dense motion routes retain continuous video/telemetry with less frequent checkpoint PNGs. Both development/release builds report BUILD_OK, with packaged hashes in C2-candidate.json. Normal autosave and backup SHA-256 remain unchanged. The revised release Natural and Shaped captures are rejected for lost focus; the Shaped attempt also missed the party swap. Recorder pooling reduces collection pressure but does not turn those attempts into acceptance. New red/green checks pass Flash/pause travel clocks, uphill/downhill rendered sole contact, and companion spacing in all eight leader headings. Slope preview also exposed and corrected a late toe-off overextension; the revised 96-case directional matrix passes. The latest full integrated verification passes 41/41 PlayMode and 39/39 EditMode with zero skips (`C2/integrated-02`); both rebuilt Windows players report BUILD_OK. Exact hashes and preserved red/green evidence are in [C2 support verification](validation/C2-support.json). Ordinary-input replay follows. Final ordinary-input 30/60/120 fps recapture, station census/stability and observed motion remain open.

The current release player (`d0deb1a`) passes both 60 fps Natural and Shaped free-travel recordings: both heroes complete their intended forms, swaps, directions and full torso cycles. Natural has 150 measured stance contacts; Shaped has 219. A preceding Shaped attempt lost focus and missed the swap, so it remains rejected. Exact artifacts are in [C2 player matrix](validation/C2-player04.json). The 30/120 fps cases and watched continuous motion remain open.

## C3 lifecycle candidate - September 26

C3 is **OPEN**. Five new red regressions reproduce premature deletion across all eight enemy types, missing hero collapse/get-up, retained disabled-craft velocity, immediate defeat freeze and lethal damage from a dead owner's pending lunge. A lifecycle candidate and owned skeleton-only Down/Revive clips are installed; all five targeted lifecycle regressions now pass, including the added ground support. The latest integrated suites pass 47/47 PlayMode and 39/39 EditMode with zero skips; both development/release players rebuilt successfully. In-player presentation remains pending. [C3 evidence](validation/C3-lifecycle.md) records the red results and remaining choreography/observed-review work.

Latest C2 player captures retain focus. Natural passes measured motion but fails a straight-line replay checkpoint at the office doorpost; the route now explicitly approaches the opening. Shaped completes the route but fails one reversal/contact facing case. Neither result closes C2. Both Windows players now include the verified lifecycle and free-travel candidate; exact packaged hashes are in [C3 lifecycle manifest](validation/C3-lifecycle.json). Ordinary rebuilt-player recapture follows.

## C3 contact candidate - September 26

**C3 OPEN.** Taren's three cuts and Arc Cleave now resolve from the actual animated wrist edge, with swept contact, one victim per swing, static obstruction and sequence cancellation. Explicit body trigger capsules separate damage surfaces from CharacterController query behavior. The old oversized sphere fails the independent rendered-edge check by 2.575 m. Covered and retreating targets must escape while the same clear target can be hit; injected legacy spheres prove those checks reject the former behavior.

The first full run failed one legacy kill fixture placed inside the arena crystal after incidental AI movement. Diagnostics show controller depenetration to 2.2765 m and obstructed paths, rather than a valid nearby strike. The fixture now uses the clear southern lane and a fully healed real enemy, retaining the original five-second timeout and kill/reward assertions. The complete corrected run passes **50/50 PlayMode and 39/39 EditMode, zero skips** (`C3/contact-integrated-02`). Both Windows players are rebuilt with BUILD_OK and exact hashes in C3-contact.json; normal save and backup hashes are unchanged. [Contact evidence](validation/C3-contact.md) preserves failed launches, ordered red, controls, traces and the final results. Sela's opened shot poses expose a separate muzzle-height mismatch that remains unfixed; skills, reactions and continuous encounter/death review remain open.

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
