# Coronach — production and quality plan

2026-09-26. This is the execution plan for correcting the playable Unity workshop and then building the campaign. Start with the reported defects, prove a polished first chapter, and expand through playable regional milestones. The next-session instructions are in [NEXT_SESSION_PROMPT.md](NEXT_SESSION_PROMPT.md).

The Unity project is `Lattice/`, product Coronach. Read [PROGRESS.md](PROGRESS.md) for current source/build revisions and dated evidence; recheck the working tree before starting. This is the acceptance plan, not a claim that its gates have passed.

## What the previous checks missed

Nathan reports station-walking performance problems, bodies running askew in combat form, weak sound effects, inadequate deaths and an implausible first-station layout. Treat these as open defects even though the September 23 suites and scripted route passed. Motion, flight and dialogue had partial checks; they were not a complete experience-quality gate. Every release map must also pass the spatial-believability gate below.

| Finding | September 26 baseline evidence | First diagnostic; do not assume the cause |
|---|---|---|
| **Q01 — Station walking stutters** | `scripts/performance.ps1` and `Scripts/UI/PerformanceProbe.cs` measure a 30-second, uncapped Gullet fight with very little player movement. They do not measure station traversal or ordinary 60 fps frame pacing. | Reproduce in Hub_Decks and TallowDrift with moving camera, companion, music, prompts and shops. Separate actual frame stalls from camera/animation judder. |
| **Q02 — Shaped bodies face right while running** | `GroundMotor` rotates the actor toward travel. `GeneratedAnimator` selects forward clips by speed; it has no directional blend. `PresentationUpgrade` bakes humanoid root orientation, and the imported body adds another transform hierarchy. | Record actor-forward, calibrated pelvis/chest-forward, velocity, clip/time and target. Compare both Natural and Shaped forms in all eight directions, without and with lock-on. The source of the unwanted yaw is not yet established. |
| **Q03 — Poor stride, transitions and attack motion** | `GroundMotor.Velocity` is commanded velocity set before collision, not measured displacement. `GeneratedAnimator` uses that value. Attacks use compressed donor clips and fixed fractions of action duration for damage. | Compare planted-foot travel, actual movement and contact timing. Inspect blocked movement, start/stop, turns, combo transitions and attack cancellation in continuous motion. |
| **Q04 — Inadequate deaths** | `EnemyBrain.OnDeath` plays a burst and destroys the object after 0.25 seconds. `GeneratedAnimator` has no Down/Dead/Death state selection; hero death sets `ActorState.Down`. | Reproduce enemy death, hero down, partner revive, both heroes down, flight defeat and boss victory. Audit animation, active hit volumes, loot and quest events separately. |
| **Q05 — Weak sound** | `AudioManager` uses one 2D effects source, two generic ambience choices and per-key throttling. The current slice stages a small subset of owned sounds. Barks are text events, not recorded voices. | Capture/listen to a real encounter and town walk with music on; inventory missing or repeated cues. Do not infer a good mix from AudioSource state or loudness numbers alone. |
| **Q06 — Dialogue quality insufficiently covered** | Yarn lines/options and deferred shop/party actions exist. Route checks auto-advance; portrait and UI captures prove appearance at selected states. | Play actual lines and branches with short/held/rapid button presses, keyboard, backtracking and save/load. Check readability, story continuity and input ownership. |
| **Q07 — First station's construction/layout fails the eye test** | Nathan reports that another viewer found its construction implausible. Earlier review already rejected repetitive rooms and weak scene composition. A navigable arrangement of generated modules did not establish a believable station. | Audit Hub_CinderHalo and Hub_Decks together: exterior hull versus interior, docks, room purposes, supports, public/service routes and boundaries. Redesign the layout where needed, then review the built space from ordinary arrival and traversal views. |

The source paths above are under `Lattice/Assets/_Project/`. Additional audit targets include `Core/CameraRig.cs`, `Core/PromptService.cs`, `UI/GameHud.cs`, `Combat/PartyController.cs`, `Dialogue/LinePresenter.cs`, `Dialogue/OptionsPresenter.cs` and `UI/DialoguePanel.cs`.

`GameHud.Update` creates arrays/strings and updates many UI properties each frame; `PartyController` refreshes stats periodically; camera follow uses a dead-zone threshold. These are candidates to profile, not established explanations for the station report. Do not begin with speculative wholesale rewrites.

## Completion and evidence rules

For each gate, record **implementation status, automated result, observed presentation result and physical-controller result separately**. Use OPEN, FAILED, PASSED or UNVERIFIED with dated evidence. An attached controller is not a controller-feel pass. A frame-rate average is not a smoothness pass. A pose sheet is not a motion pass. Audio playback counters are not an audio-quality pass.

- Keep the current professional-quality rejection open until its actual presentation conditions pass. Preserve Nathan's reports alongside the historical green tests.
- Before accepting a check, demonstrate the broken condition that makes it fail. Suitable controls include an injected frame stall in a diagnostic build, a deliberately misoriented fixture, a missing death clip, a repeated reward or a silent required cue. Remove fault injection from the shipped build. Do not write tests that merely assert the implementation's own constants.
- Store raw frames, continuous recordings, audio captures, profiler traces and per-frame CSV under `Builds/quality/<gate>/<run>/`. Commit small summaries to `docs/validation/` with paths, hashes, source revision, settings, findings and limitations. Update `docs/PROGRESS.md` and `docs/DECISIONS.md`; commit at each completed gate.
- Use ordinary input through the game's input stack for traversal and presentation review. Record whether a session was human play, agent-directed virtual input or scripted replay. Route-driver combat calls remain useful regression evidence, not equivalent player experience.
- The existing `-review` harness pauses between decisions. Keep it for diagnosis, but add an explicitly continuous input/capture path for motion review. A paused sequence cannot establish frame pacing. Keep video capture and heavy profiling out of the clean performance measurement run; measure their overhead separately.
- Inspect moving sequences at normal speed and then slow motion. If tools cannot play continuous footage or audition sound, keep that observation UNVERIFIED, retain the artifact and continue other work. Do not silently substitute static frames or waveform analysis.
- Use isolated saves. Never overwrite Nathan's ordinary save, kill an unrelated Unity instance, reset the project or regenerate the entire world as a shortcut.
- Choose routine implementation details autonomously. Physical feel can remain explicitly pending while independent production proceeds; it must not be mislabeled as accepted. No repeat species approval is needed.

## C0 — Reproduce and establish the baseline

Deliver a short defect report for Q01–Q07 before making broad changes. Confirm Unity/editor version from the project, running processes, source/build correspondence, available audio and animation donors, and the current Meshy ledger. For Q07, capture the first station's overhead layout, exterior/interior relationship and ordinary arrival route; label unexplained construction and circulation problems before redesigning.

Record a 120-second station route: office to market, slow walk, sprint, 90/180-degree turns, doorways, wall contact, companion crossing, Mira conversation, shop open/close, return. Repeat a comparable route in TallowDrift. Include Hub_CinderHalo approach/dock and return to distinguish station interiors from flight. Run both heroes where the party is present.

In Arena_Ground and Sorrel, record Natural/Shaped comparisons and all eight travel directions for both heroes; then lock-on movement, attack/return-to-run, dodge, guard, swap, down and revive. Use gameplay camera and a diagnostic view with facing arrows. Capture baseline SFX and dialogue behavior as well.

**C0_BASELINE_RECORDED:** reproducible routes and defect clips exist; unknown causes are named; first fixes are ranked from evidence. This gate records observations, not acceptance of the defects. If a reported problem does not reproduce, preserve exact settings and the discrepancy and extend the relevant coverage instead of declaring it fixed.

## C1 — Station performance and frame pacing

Extend the performance tools to accept named scenes and moving input routes. Keep the Gullet stress test as one case. The new check must fail on a stationary avatar, lost focus, paused simulation, wrong scene/resolution, missing samples or a route that never reaches its checkpoints.

Measure standalone release and development builds separately. Use development CPU/GPU/GC/UI/render traces to locate costs, then verify the correction in release. Deep profiling is a separate diagnostic run. Record CPU/GPU model, display refresh and VRR state if available, resolution, quality, frame cap, VSync, focus, build hash and sample duration. Mark unsupported GPU timings unavailable rather than deriving them from CPU time.

Collect per-frame elapsed time, median/p95/p99/worst frame, counts above 25/33.3/50/100 ms, allocation/collection events, CPU/render/GPU costs where supported, batches, active audio voices and camera/player displacement. Plot frame times against route checkpoints so shop entry or a recurring half-second spike can be identified.

Use first-visit and warmed runs. After a separately recorded load/settle interval, complete two 120-second traversal runs per station and a ten-minute repeat loop for memory/voice/object growth. Include first shop, first dialogue, first footstep/sound and first form swap; do not remove their stalls from the report. Full scene loads are reported separately from movement and must be covered by an intentional transition, not a frozen exposed frame.

Initial acceptance on Nathan's RTX 5070 machine at native 1920×1080 and a 60 fps target:

| Measure | Gate target |
|---|---|
| Ordinary capped traversal | p95 frame interval ≤18.5 ms; p99 ≤25 ms; no repeatable movement/input judder. |
| Hitches in each 120-second traversal | At most one frame >33.3 ms, zero >50 ms. First-use actions during traversal count. |
| Diagnostic headroom | In uncapped, unrecorded runs, p95 active CPU and GPU work each ≤14 ms when measurable. Account for frame-cap/VSync waits separately. |
| Stability | No recurring allocation/collection spike correlated with stutter; no sustained growth in live temporary objects, sources or memory after caches warm. |
| Interaction | Shop/dialogue feedback starts within 100 ms of accepted input; no gameplay button leaks on dismissal. |

These are production targets, not measurements already achieved. Investigate display pacing and supported timing accuracy before interpreting borderline numbers. Any justified target revision needs a recorded reason; lowering thresholds to pass a bad run is unacceptable.

Fix measured causes. Possible interventions are caching static HUD/binding data, updating text only when values change, separating canvases, removing avoidable allocations, preloading first-use assets, simplifying excessive colliders, batching repeated geometry and correcting camera/update timing. Apply only interventions supported by traces. Preserve native sharp rendering and the visible scene; do not hide the defect by restoring blur or stripping town content.

**C1_STATION_PERF_OK:** both stations pass the routes in the final release; profiler evidence explains the original fault or narrows an unreproduced report honestly; continuous viewed traversal is smooth. Carry these routes into every subsequent content milestone.

## C2 — Facing, locomotion and camera motion

1. Establish one authored forward axis per rig and one owner for gameplay heading. Check root, imported model, prefab offsets, avatar mapping, clip root-rotation settings, mirroring and any upper-body twist separately. Fix the actual source of the right-facing run; a blanket actor yaw correction can break attacks, targeting and ships.
2. Separate desired movement, actual ground displacement, gameplay facing and visual aim. Use measured planar motion for stride playback, with intentional treatment of collision, knockback, slopes, dash and teleport. Do not animate running in place against a wall because the input still requests full speed.
3. Build explicit idle/start/walk/jog/sprint/stop/turn transitions. Add directional strafe/backpedal for target-facing movement. Document the rule: free traversal faces travel; deliberate aiming/lock-on may face the target while legs use the correct directional motion. Sela must not carry a sideways firing torso into ordinary free running.
4. Tune stride, foot plant and weapon attachment for both generated bodies. Inspect hands, knees, pelvis, vanes and cloth in motion. Replace unsuitable takes or author corrections rather than speeding up every clip to fit.
5. Check motor, animation, camera and hit-stop clocks at 30/60/120 fps; pause/resume and Flash must preserve their intended relation. Fix any dead-zone snapping or camera update mismatch shown by the station trace.

The matrix is two heroes × Natural/Shaped × eight directions × walk/run/sprint where available, plus diagonal turns, full reversals, lock-on circles, blocked travel, ramps, near-zero input, swap and form transitions. Review a 60-second locomotion loop per hero/form at normal speed, with slow-motion inspection of transitions.

For steady free running after 0.5 s, calibrated pelvis-forward should remain within 10° and cycle-averaged torso-forward within 15° of actual travel. Calibration must reflect the visible rig; do not compare a transform to itself. Action anticipation and deliberate aiming have separate labeled checks. Foot-contact displacement should stay within roughly 5 cm on flat-ground test strides, with no conspicuous skate at gameplay scale. Preserve designed asymmetry without accepting a persistent sideways run.

**C2_MOTION_OK:** Q02's exact sequence no longer produces the defect on either hero; direction/stride measurements and watched motion agree; no regression in aim, attachments, camera or pause. Record the chosen clips and import settings so rebuilding intake preserves the fix.

## C3 — Combat choreography, reactions and death lifecycle

Implement the moveset in [ANIMATION_DIRECTION.md](ANIMATION_DIRECTION.md), starting with the existing attacks and four skills before adding unlocks. Author per-move anticipation, contact, recovery and permitted cancel windows. Put hit timing in explicit move data/events and make the visible edge/projectile agree with the damaging volume. Include enemy stagger, break, armor response and distinct finisher feedback. Camera shake and flashes must keep targets and incoming attacks readable.

Keep input responsive without permitting ghost hits. Record input receipt, action start, contact and recovery; immediate legal actions should give visible feedback within 100 ms, and buffered attacks must fire at the next legal window. Reject hits after dodge/guard/death cancellation, duplicate contacts from a single swing, stale queued inputs after menus and damage before the visible strike. Test normal enemies, shields, bosses, overlapping targets and targets leaving reach.

Replace abrupt deletion with a presentation lifecycle, separate from gameplay resolution:

- **Humanoid/creature enemies:** directional hit reaction where useful, authored death/collapse, stable terminal pose, then deliberate cleanup. Target a readable 0.6–1.5 s death motion and a short corpse hold where density permits; tune by creature, never a universal quarter-second disappearance.
- **Drones/ships:** brief failure motion, sound and controlled destruction/debris; pool temporary effects and limit visual obstruction. Heavy bosses get a distinct finish; narrative release/disable outcomes must not play a kill-and-loot animation by accident.
- **Heroes on ground:** down animation and persistent downed pose, readable revive progress, get-up animation and protected return to control. A downed hero cannot keep running, attacking or producing footsteps.
- **Heroes in flight:** disabled craft with a visible recoverable state and revive cue. Preserve a coherent flight plane and avoid falling out of the map.
- **Both heroes down:** clear defeat transition, retry/checkpoint restoration and no double reward or save corruption.

At lethal resolution, cancel pending action work, disable hostile decision-making and live hurtboxes, and clear targeting appropriately. Award XP, loot and quest progress exactly once. Keep required bodies/visuals alive long enough to finish; cleanup must not own the quest outcome. Define the exception for intentionally designed death attacks such as Scrapmite so generic cancellation does not silently remove them. Revive/pool reuse must reset animation, colliders, materials, timers and event subscriptions.

Review one full five-minute ground encounter for each hero, a mixed encounter with swaps and both slice bosses. Include deliberate failure, revive and retry; inspect a death reel covering every current enemy archetype and each hero/form. Review at gameplay distance as well as slow motion.

**C3_COMBAT_DEATH_OK:** attacks and reactions read clearly; every current defeat/down/revive path has an intentional presentation; no dead actor damages unexpectedly, blocks a quest or duplicates rewards. New meaningful regressions cover the lifecycle, cancellation and contacts. Sound integration is finalized in C5, and this gate is rechecked there.

## C4 — Flight and transformation

Keep the generated compact ship silhouettes. Tune acceleration, thrust direction versus inertia, braking, boost, roll, lunge and aiming as a coherent flight model. Document whether nose, thrust and fire follow input or target; test that rule during sideways drift and reversal. Align muzzle/engine/trail sockets with actual hull geometry rather than the former humanoid wrist coordinates.

Run a three-minute civil flight circuit around Cinder Halo and a five-minute Gullet combat circuit per ship: figure eights, 90/180-degree turns, boost/brake, sustained fire, lunge recovery, roll, wall contact, partner catch-up, swap and docking. Repeat critical integrator/braking paths at 30/60/120 fps; traveled and stopping distances should agree within 5% under matched input duration. Label any deliberate tuning changes.

Check near-wall and low-speed turning for camera oscillation; verify the hull bank never rotates the collision body. Require clear anticipation and finish for lunge/roll, stable partner spacing and no unexplained camera jump on swap. Retain existing tests separating wall damage from actor contact.

Replace the collapse/swap/expand placeholder with a coherent staged transformation: retain visual identity, show attached panels/vanes folding or moving into the ship silhouette, mask only the unavoidable topology change with a brief effect, and keep control/collision state consistent. A genuine continuous skeletal morph is optional; an obvious instant body replacement is not the final visual target. Reuse generated geometry and rigs where possible. Test interruption, pause, swap and rapid repeated zone boundaries without invisible or doubly active forms.

**C4_FLIGHT_OK:** both craft look and behave intentionally in continuous civil/combat play, fire originates correctly, docking works and transitions have no visible pops or state faults. Hardware attachment is logged separately from physical feel.

## C5 — Sound design and mix

Preserve Nathan's seven music originals and the current four cue assignments. Begin with the owned Kenney impact/interface/RPG/science-fiction packs and licensed Sonniss archives listed in [ASSET_SOURCES.md](ASSET_SOURCES.md). Audition/select small subsets, edit/layer them into a consistent sound palette, stage through the asset pipeline and record source/license/derivative history. Do not import a giant archive or buy new audio as a default.

| Sound family | Required distinctions and behavior |
|---|---|
| Movement | Footfalls timed to planted feet; metal deck, rock and later soil surfaces; restrained cloth/armor movement; no steps while airborne, dead or stationary. Use at least four suitable variations for frequently repeated footfalls. |
| Melee | Distinct short/medium/heavy swing, actual hit versus miss, flesh/armor/shield response, guard/perfect guard, break and finisher. At least three variations for common repeated impacts. |
| Ranged and skills | Taren/Sela and different skills remain distinguishable; start/sustain/release where needed; projectiles must not sound like melee impact. |
| Flight | Thrust bed, boost onset/sustain/release, braking, roll, lunge, wall strike, docking and transformation. Loops follow actual state and fade cleanly. |
| Reactions and deaths | Small/large creature, mechanical failure, hero hurt/down/revive, boss defeat or release; clear priorities without every simultaneous death peaking at once. |
| World/UI/dialogue | Station room tones, doors, machinery, interaction/confirm/cancel/error, subtle dialogue advance. Text barks remain labeled as text; full voice acting is a separate future scope decision. |

Add mixer groups and saved controls for Music, SFX, Ambience and UI with master control. Use bounded pooled voices, per-emitter cooldowns, variation, restrained pitch variation and priority/ducking. Keep UI cues readable and world sounds appropriately placed; camera distance must not mute the player's weapon. Preload short first-use sounds as evidence warrants, stream music, and ensure loops/voices stop on death, pause behavior, scene unload and return to title.

Capture and audition at least 60 seconds each of a station walk/shop/dialogue route, a dense ground fight and a flight fight with music on. Inspect isolated families and the actual mix. Listen at low and normal volume for cue readability, repetition, clicks, fatigue and masked telegraphs. Captured mix true peak should remain at or below -1 dBTP, with no clipping or uncontrolled sustained limiting; verify voice counts and DSP load during the C1 loop. These numerical checks supplement listening.

**C5_AUDIO_OK:** required cues exist, track actual actions, survive simultaneous events and sound coherent in the captured mix. Record what was actually heard and through which tool/output. Missing audition capability keeps subjective audio review UNVERIFIED.

## C6 — Dialogue, interaction and narrative checks

Test the opening and all current speakers: Orrin, Mira, Hal, Neve, the Survivor and Keeper, followed by every new branch when its chapter ships. Use actual Yarn dialogue with `AutoAdvance` off for the presentation/input review. Automated fast routes remain separate.

Cover tap-to-reveal then tap-to-advance; held confirm; rapid taps; selection changes; unavailable/all-unavailable options; cancel according to the documented conversation rule; pause/focus loss; keyboard/gamepad switching; shop/bench handoff; repeat visits; join-party and quest rewards; save/load before and after each consequential branch. Cancellation must never silently select an option with a consequential effect. Exiting UI must consume the closing input without attacking or dodging in the world.

Check long lines, speaker changes, expression changes and options at 1280×720, 1920×1080 and 1920×1200. No clipped text, unreadably small type, off-screen options, missing focus or stale portraits. Glyphs/hints must match the active device. Backtracking should acknowledge current world/quest state; repeated conversations must not duplicate joins/items/rewards.

For writing, give speakers distinct motives and voices, keep most compulsory exchanges below ninety seconds, and stage information through actions and consequences. Maintain the Severance story: every main quest has an earlier cause, a visible change and a later payoff. Introduce Nacre before its disaster; keep recruited communities active; track the nursery from Sorrel to the finale. Review actual conversations in their playable context, not only the script text.

**C6_DIALOGUE_OK:** all implemented branches work with real input; text/portraits/focus read correctly; state changes persist exactly once; dialogue advances the connected story without requiring codex reading. Track automated branch coverage and editorial/play review separately.

## MAP_EYE_TEST — Spatial believability for every release map

Fantasy architecture may be extraordinary, but its construction and use must read coherently. The player should be able to infer what a place is for, how people use it, how its major pieces connect and why the route exists. No engineering simulation is required. Explanations that exist only in a design document do not rescue an incoherent visible layout.

Run this gate twice for each new map: **on the blockout before final art**, and **in the integrated player after art, lighting and collision**. Recheck materially changed return-state maps. It is required for the current workshop as well as C8–C10, not deferred until later campaign production.

Before construction, write `docs/maps/<zone-id>.md` with the place's purpose, inhabitants/users, construction or natural formation rules, scale, functional areas and the relationships between them. Include an annotated overhead plan and, where useful, a section showing levels and the exterior envelope. Mark public travel, cargo/service access, entrances/exits, critical interactions and combat spaces. Non-playable rooms may be implied, but their placement and access must remain plausible.

Review these questions against the actual scene:

| Check | Passing evidence / rejection examples |
|---|---|
| **Purpose and adjacency** | Docks connect to receiving, repair or public arrivals; shops face useful foot traffic; homes and services have sensible access. Reject unrelated props/rooms scattered on a tiled platform without a reason for their relationship. |
| **Circulation and scale** | Trace a visitor, worker and delivery route. Doors, stairs, lifts, corridors and turning spaces fit their users and cargo. Paths reach believable destinations; a blocked route has a visible reason. Reject doors into empty space, blocked essential access, routes through unrelated private rooms or furnishings placed across the only walkway. |
| **Construction and enclosure** | Floors, walls, beams, bridges and hull sections join intentionally. Upper levels have apparent support or a clearly established suspension rule. Pressurized areas have readable hull/airlock boundaries; exposed decks look exposed. Reject accidental floating modules, gaps, intersections and unexplained open edges. |
| **Exterior/interior agreement** | Docks, major windows, decks and level changes broadly correspond between outside and inside. Consistent cutaway roofs/walls are acceptable for the camera, with readable boundaries. Reject a room arrangement that visibly cannot belong to its exterior. |
| **Utilities and occupation** | Important machinery has usable access and a plausible connection to what it serves. Housing, storage, repair and daily life are visible or sensibly implied. Reject consoles with no operator space, loading equipment that cannot reach freight or decorative machines blocking normal activity. |
| **Terrain, ecology and arenas** | Rivers follow the terrain; caves and paths have a readable formation; settlements respond to terrain/resources; boss spaces have a prior use or ecological reason. Any exceptional gravity or living architecture follows a visible, consistent world rule. Reject arbitrary arena squares or corridor mazes that exist only to hold enemies. |
| **Immediate visual understanding** | From arrival and normal camera views, the main route, room purpose and major landmark are legible. Interesting asymmetry and additions still belong to the same place. Reject layouts that require the designer's explanation to seem intentional, or repetitive tiling that obscures the structure. |

For the **first station**, create a coherent reference layout before rearranging modules: working docks and receiving lead to a repair/service area; a public arrival route leads to the promenade, office and market; a quieter residential branch and an underlying service spine make the town feel inhabited. Relate these areas to the ring and reused vessel hulls visible outside. This is an initial design direction, not a mandate for a rigid grid. Its modular construction should quietly establish how Cinder Halo can later separate into ships.

Review Hub_CinderHalo and Hub_Decks as two views of one station. Fix structural layout and circulation wherever needed; adding more crates, signage or lights is insufficient if the construction still makes no sense. Update docks, collision, spawn safety, camera sightlines and regression routes to match the corrected layout. Preserve quest behavior and saved progress; do not retain a bad layout merely to keep a hardcoded test route green. Repeat station performance checks after the redesign.

Evidence: open an overhead view, an exterior/section comparison where applicable, and gameplay-scale views of arrival, main junctions, major rooms, level changes and edge cases; walk the public and service routes. Review once with labels/HUD hidden, then during ordinary play. Write concrete observations of what each space appears to be for and what remains confusing. Repair every major unexplained contradiction and recapture. A completed checklist, attractive close-up or technically connected path alone cannot pass the gate.

**MAP_EYE_TEST_OK <zone-id>** requires a recorded blockout pass and final in-player pass with no major construction/circulation contradiction and a convincing ordinary traversal. Record who actually reviewed it. Agent review is permitted; do not invent an independent viewer or Nathan approval, and do not introduce another mandatory user-approval stop. Nathan's first-station rejection remains open until the redesign has been inspected and its reasons for acceptance documented.

## C7 — Accept the gameplay workshop

Rebuild development and release players from the integrated source. Run compile checks, relevant new regressions, the full EditMode/PlayMode suites, both arena smokes, ordered slice route, UI/portrait checks where affected and the new station/flight performance routes. Require fresh results and zero skipped required tests; the historical 35/28 count is a baseline, not a number to preserve by avoiding useful tests.

Then play the ordinary release flow from New Game through town, moon, Gullet and Tallow, including dialogue, shop, crafting/equipment, a death/retry and save/continue. Reopen captures after the final fixes. Review continuous movement and captured audio. Exercise the physical Logitech when possible; log focus loss, disconnect/reconnect, short taps and comfort independently from virtual tests. A full scripted route cannot replace this experience check.

**C7_WORKSHOP_ACCEPTED:** C1–C6 have their required implementation, automated and observed checks passed, every workshop release map passes MAP_EYE_TEST, and the professional presentation defects are resolved in normal play. Record any unavailable human observation explicitly; a technically complete candidate can be handed off with that item pending, but cannot be called physically validated. If visuals, layouts or combat still look poor, iterate even when all tests are green.

The next deliverable to Nathan is a playable workshop build with a concise before/after report for Q01–Q07, final test evidence, actual motion/audio artifacts, station layout comparisons and remaining limitations. Commit the gate and include the runnable EXE path.

October 4 candidate status: [the workshop report](WORKSHOP.md) and [nursery chapter candidate](WORKSHOP_CHAPTER.md) provide isolated launchers, exact packaged revisions and ordinary-input evidence. They are playable candidates; C7 acceptance, unavailable continuous/physical review and outstanding map/story polish remain open.

## C8 — Build and prove the first complete chapter

Use [STORY_CAMPAIGN.md](STORY_CAMPAIGN.md) and [WORLD_ATLAS.md](WORLD_ATLAS.md) as the campaign plan. Turn the Decks, Sorrel Ridges and Hushwell into the first polished chapter, then align the Gullet/Tallow continuation with the nursery and collar-release story. Keep current saves compatible through explicit migrations or separate development fixtures.

For each map: functional brief, overhead layout, exterior/section relationship, traversal-time blockout, landmarks/sightlines, encounter and dialogue graph, checkpoint/return loop, asset list/cost, lighting/audio target and performance route. Pass MAP_EYE_TEST at blockout and final art stages. Build gameplay before final generated art. Enlarge meaningful loops and interiors rather than filling a rectangle with repeated floor modules.

Implement the connected opening, communications shutdown, nursery discovery and contrasting Burrower/Bellows encounters. Expand town residents/interiors, enemy tells, meaningful gear/skill choices, companion behavior and menus as required by this chapter. Author boss patterns with placeholder geometry before commissioning expensive final models; final accepted visible art remains generated.

October 4: the [nursery story connection and version-two save compatibility](validation/C8-nursery-integration.md) pass a fresh 38:37 New Game-to-Tallow chain. [Generated floor art](validation/C8-nursery-art.md), [shoreline refinement](validation/C8-nursery-water.md) and [organ revisit state](validation/C8-organ-persistence.md) have separate evidence/status. The opening walkway, richer town loops, Burrower disable finish, Gullet preview/collar release and Tallow's moving-refuge story remain production work.

**C8_FIRST_CHAPTER_OK:** a fresh profile can complete the chapter and return through its shortcuts; every map passes applicable C1–C6 checks; the story can be understood through play; combat holds up over multiple encounters; no required content remains a stand-in. Measure actual playtime and density instead of declaring atlas time estimates achieved.

## C9 — Build the connected campaign in playable regional milestones

The target remains 32 main maps, four optional destinations, 18 main bosses and four optional bosses. This is a production target, not permission to commission all art at once. Each regional milestone must be completable, saved, replayable and verified before expanding the next. Keep a working end-to-end build throughout.

| Milestone | Content and systemic work | Story and quality exit |
|---|---|---|
| **C9A Xylos / Many Hands** | Rootmarket, river/canopy routes, Seedthane/Floodcrown/Hulljack; changing ship-town, growers/passengers, mixed ground/flight encounter. | The convoy becomes a credible alternative and attracts opposition. Town state changes persist; traversal, audio, dialogue and mixed boss checkpoints pass. |
| **C9B Veyr / Merrow** | Mountain orbit/town/furnace and noble/service city; four bosses; Idra/Oru recruitment, reserve-party selection and their complete motion/flight/down/revive/audio sets. | Both chapter orders work, including dialogue/world consequences. Every new hero must pass C2–C5 and party/save regressions before acceptance. |
| **C9C Pale Exchange / Nacre** | Comet port/interior, wreck routes, Unfinished Welcome, Lenticular/Undertow Tug; distributed navigation mission and tested rendezvous. | The seeded disaster changes the journey and Sela's responsibility. A playable rescue, not a lore-only explanation, establishes the final navigation capability. |
| **C9D Home launch / finale / epilogue** | Returning Cinder and Sorrel, separation and nursery rescues, anchor fleet, Continuance, Meret, final tug rescue and inhabited epilogue. | Familiar people and learned mechanics make the evacuation work. Full ground/flight boss finishes, phase checkpoints, final saves and postgame state pass; an authored ending is playable. |
| **C9E Optional destinations and depth** | Glasswake, Chimera Reef, House Without Weight, Stormcrown; side stories, equipment sidegrades, expert rematches and backtracking. | Optional work has visible payoffs but cannot secretly be required for core survival. Cut or defer optional breadth before sacrificing combat quality or the ending. |

At every milestone, extend the quest-state/save migration matrix, loadout/balance runs, enemy/boss pattern tests, asset/licence manifest, performance routes and dialogue branch coverage. Keep MAP_EYE_TEST for each map, technical performance, gamepad navigation, accessibility/readability, audio mix, narrative causality and observed motion as release conditions. Final boss duration and difficulty must be tuned from ordinary play with legal equipment; do not force the old fast smoke-route timings onto full-game encounters.

## C10 — Full-game release candidate

Verify New Game to credits and playable epilogue, both Veyr/Merrow orders, representative side-quest outcomes, both-heroes-down/retry, every boss checkpoint, save/continue in every region and supported migration from retained saves. Exercise all playable heroes/forms, key builds, inventory/crafting, settings persistence, controller disconnect/reconnect and keyboard fallback.

Run performance captures in each worst-case town, exploration map and dense boss arena, plus an extended region-transition/play loop for memory and voice leaks. Audit final generated-art provenance, missing assets/portraits/sounds, licences, debug affordances and release packaging. Recheck the first hour and final hour after the last content changes.

**C10_RELEASE_CANDIDATE:** the complete authored campaign is playable, required quality gates have current evidence, no critical progression/save defect remains, and unfinished or unverified items are explicit. Deliver the Windows package, Unity source, controls, known limits, credits and reproducible verification commands. Do not substitute a design document, collection of isolated maps or developer-autopilot completion for a finished campaign.

## Assets, budget and tools

Generated art remains the default. D127 explicitly approves Nathan's purchased Stylized Water 3 and Stylized Grass Shader; [their source and derived assets stay private, with working public fallbacks](OPTIONAL_ART_PACKS.md). Use the session image-generation tool for new 2D art and the project's Meshy bridge for needed new 3D work. Read applicable available skills and current art-intake instructions before generation/rig edits. Owned animation skeletons/clips, particles, glyphs, fonts and licensed sounds are permitted; donor body geometry is not final art. Frostbound is read-only.

The September 26 balance was 405 credits; that is historical. Nathan's later available balance funds the current work. The last recorded live balance is **2,778 credits** after the October 4 nursery intake ([P46 ledger](art/P46-nursery-groove.json)). Recheck live balance and consumed credits before paid work. This is not an unlimited campaign budget. Prioritize current-character quality, reuse generated kits and greybox later content. Estimate and record each art batch before submission. If full campaign final art cannot fit, record the exact unfunded assets/cost and continue independent work without pretending stand-ins pass final acceptance. Do not spend credits merely because a later atlas row exists.

Existing tools include `scripts/headless.ps1`, `quality-replay.ps1`, `smoketest.ps1`, `performance.ps1`, `review-step.ps1`, `look.ps1`, `ui-smoke.ps1`, `portrait-smoke.ps1` and `balance.ps1`. Moving ordinary-input routes, continuous capture, frame timing, GPU API cross-checks and dialogue-input regressions are implemented; their dated evidence and remaining observation limits live in `docs/validation/`. Inspect script parameters before calling them. Run the full PlayMode suite with `-TimeoutSec 3600`. Run each player-window session alone as one blocking call; preserve one Unity owner of the project and never stop unrelated processes. Verify Nathan's original save hashes after big runs.

The current priority is the newest human quality report in PROGRESS, followed by the remaining C1–C6/C7 evidence and C8 polish. Do not restart the historical C0 assignment or begin by generating the remaining planets. See [the continuation prompt](NEXT_SESSION_PROMPT.md).
