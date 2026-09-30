# CORONACH slice progress

## MAP_EYE_TEST final in-player: Sorrel, arenas - September 30

**Sorrel (outpost, western loop), Arena_Ground and Arena_Flight pass their ordinary in-player traversals with full focus.**

- **Sorrel.** The outpost walk (153.4 s) and the western seam loop (280.6 s, four packs cleared) both pass on final art. The loop had previously been rejected only for focus loss.
- **Proving yard.** The new `arena-yard-walk` route fights the opponents, then walks the service lane, all four corners, the east barrier and the operator corner.
  - Its first run was **rejected** and found a real layout defect: the low cover ridge and the backstop rock formed a pinch that trapped Taren. The target props were moved forward, leaving a clear lane along the rock.
  - `-03` then passed (92.9 s).
- **Proving berth.** The new `arena-berth-flight` route passes (110.7 s): boost, brake and roll, hull contact at the north rail and at the closed lane end, and Hal's call prompt live at the call pad. Both lunge routes also pass in the berth.

`c0-motion-arena_ground` is rejected: the Ridgehounds maul the idle party during its 8-second settle. That is a C2 motion-fixture decision; the route was left unchanged. The fifteen ground-arena test classes pass 91/91. [Arena evidence](validation/MAP-Arenas.md#ordinary-in-player-passes-september-30), [Sorrel](maps/Sorrel_Ridges.md).

## C1 clean station performance - September 30

**C1 timing and stability PASS on both stations and both builds.**

- **Timing.** The four clean first/warm runs are all valid: p95 about 17.0 ms, p99 about 17.1 ms, no frame over 33.3 ms.
- **Stability.** The ten-minute release runs with the object and audio census are valid for both Decks and Tallow, with full focus, flat memory and no object or voice growth.
- **Tallow headroom** passes (p95 CPU 1.0 ms, GPU 1.25 ms).
- **Decks headroom:** CPU passes (p95 1.0 ms). Its GPU counter evidence is still rejected, because Unity's counter returns exactly two timestamp-like values at the route's dialogue openings.

A person viewing continuous traversal, and the Decks GPU counter, remain UNVERIFIED. [Evidence](validation/C1-station-performance.md#clean-integrated-runs-september-30-claude-code).

## C4 ordinary Gullet circuits and slice smoke - September 30

**Both ordinary Gullet circuits PASS on the redesigned anatomy with full focus; the scripted slice smoke PASSES end to end for the first time since the station redesign.** The other session's Frostbound tests had finished, so the player-window work ran, each capture alone.

- **Taren:** `gullet-circuit-taren-11`, 393.6 s, 23,614/23,614 focused. Finished at 110.9 health, partner at 114.9.
- **Sela:** `gullet-circuit-sela-06`, 393.7 s, 23,621/23,621 focused. Finished at 110.9, partner at 97.3.
- **Smoke:** `SLICE_SMOKE_OK` in 522 s, with the route marker contract intact.

The ordinary runs overturned things the controlled checks had accepted, and each is now fixed and tested red then green:

- **Gullet ribs.** Wall ribs stood about 2 m inside the passage, and more at the salvage eddy's flare. Ribs are now checked across their whole footprint and pushed out.
- **Eddy debris.** The debris lay among the mines; it now rests at the back of the pocket.
- **Partner.** The AI partner never evaded fire. It now rolls or sidesteps from shots about to hit, in flight and on the ground.
- **Docks.** Flight docks were unreachable head-on: the Repair Bay's 4 m prompt was inside where a hull stops. Flight docks now use 6.5 m.

The Sela route had also lost its opening swap in my generator; it is restored.

The smoke was taught ordinary pilot sense: a hull waypoint, Sorrel's real haul bends, stances beside solid interactables, range from mines, and holding skills until a lunge kill. Its contract is unchanged. [Evidence](validation/C4-gullet-circuits05.md), D123. The Gullet's final in-player pass now covers the mouth to the eddy; the gate, coil and exit have only scripted traversal. The full suites pass **56/56 EditMode and 181/181 PlayMode**; saves are unchanged.

## Bellows Below last phase - September 29

**B02 complete in code; its fight is not yet played.** Below 30 % health the Bellows' dome collapses onto four exposed ribs at the chamber's edge. It marks a path to the rib nearest the party, lunges along it, striking and staggering anything in the way, and breathes from its new rib. The last phase becomes a chase, completing the atlas design: organ bracing, the choice of a safe half, then the ribs. The new test was **rejected twice**: first because the chamber had no ribs, then with the ribs built but no last phase. It passed once the crossing was implemented. The full suites pass **56/56 EditMode and 176/176 PlayMode**. Top-down and oblique chamber views were opened.

## Cinder ship-borne residents - September 29

**C6 OPEN; Hub_CinderHalo exterior changed and rechecked in controlled views.** Following Nathan's town-music rule, two more crews now hail from their own parked ships beside Neve's skiff. Both carry the story's Act I seeds before Meret appears:

- **Ilo**, an Anchor Compact surveyor, introduces Director Venn's published shelter capacity and its footnotes.
- **Oda**'s household is packed for the Nacre transfer: "one berth, one departure", which foreshadows its disaster.

Each has three conversation tiers and a new 16-expression portrait set. Both speak through `HailPoint` with no standing body, and neither ship collides, so no recorded circuit or dock approach changes. `CinderResidentTests` was **rejected before the residents existed**. It then caught a speaker placeholder capsule whose live collider was an invisible obstacle in the lane, and was **rejected again before the portraits were imported**. The full suites pass **56/56 EditMode and 175/175 PlayMode**. D122; [station notes](maps/Hub_CinderHalo.md). Reading the new lines in playable context and hailing them in flight remain open under C6/C4.

## Hushwell side quest and objectives - September 29

**C6 OPEN.** Hushwell now carries its own side quest, **What the drill found**. It starts the first time the cave is entered and completes on the Bellows kill and the nursery discovery, rewarding 220 XP and 150 scrip. Before this the cave had no objective at all: the HUD fell through to the arenas' practice text. It now leads to the Bellows, then the nursery, then the lift. Back in Sorrel, the Survivor mentions the breathing cave after the warp key; after the discovery they answer with a new nursery conversation, through a later speaker tier checked before the post-quest node.

The new test was **rejected before implementation**. Its first green attempt exposed a real defect: killing the generated Bellows raised an unreadable-mesh error, because the rule making enemy bodies readable lived only in a one-off upgrade tool. `GenIntake` now applies it at intake. The full suites pass **56/56 EditMode and 174/174 PlayMode**; saves are unchanged. D121. Reading the new lines in playable context remains open under C6.

## MAP_EYE_TEST arenas and Sorrel final art - September 29

**Arena_Ground and Arena_Flight blockout PASS (agent review); Sorrel converted to final art; all three in-player passes OPEN.**

- **Proving yard.** The ground practice square is now an outpost maintenance proving yard. A rock outcrop and backstop, an installed panel barrier and a low crate fence around a service entrance bound it. Hal stands in a sheltered operator corner, out of the firing path.
- **Proving berth.** The flight square is now a dockside proving berth. Rails and posts at the flight plane are its visible boundary, around a railed entry lane. A supported control cabin, from which Hal speaks, sits outside the rails, and a dock deck lies far below.
- **Contracts kept.** Both keep their arrival, opponents, dialogue and the clear core the tests use.
- **Tests.** `ArenaLayoutTests` **rejected both old scenes** (scenery inside the core), then passed.
- **Sorrel.** The connected basin now uses its generated art with no layout change. The route bake and the opened views pass: an inhabited outpost, both mineral branches and the excavation with the Hushwell bore. Empty sand between features is noted.

The full suites pass **56/56 EditMode and 173/173 PlayMode**. [Arena evidence](validation/MAP-Arenas.md), [Sorrel brief](maps/Sorrel_Ridges.md), D120. Every workshop map now has a recorded blockout review; the remaining map work is the ordinary in-player passes. Captures stay blocked while the other session's Frostbound player tests hold the foreground. The slice smoke's dock-approach fix is written but unverified, and is held back until it can run.

## MAP_EYE_TEST Hushwell blockout and first final build - September 28

**Hushwell blockout PASS (agent review); final built with generated art; in-player pass OPEN.** Hushwell, atlas map 04, is a new optional moon cave under Sorrel's drill site. The party enters by the bore once the Burrower is dead.

- **Layout.** Three levels descend from the drill breach through the miners' galleries and a survey-grid stretch, down to a three-chamber pressure gallery, the Bellows chamber and the nursery.
- **The breathing rule.** The nursery breathes on a shared six-second cycle. Floor vents scald anything in their plume, hounds included. Vent-membranes open side passages to a miners' cache and a crystal seam.
- **Bellows Below (B02).** Two Kinetic-weak pressure organs brace it (half damage) and blast alternate halves of the chamber on its exhale, so breaking one chooses the safe half. The final model is a limbless domed bellows whose breathing is its animation.
- **Nursery.** Looking at the eggs is the discovery, and it wakes the lift back to Sorrel. Moon Caverns plays throughout, and Alien Boss Battle during the fight.
- **Camera cutaway.** The cave is built to the fixed ground camera: no rock or wall face may hide a hero anywhere on the floor.
- **Art.** Nineteen generated models (a nine-piece kit, the boss, nine dressing props) cost 321 Meshy credits; 3,836 remain.
- **Tools.** `tools/isolate_board.py` cuts boards for free, and `tools/blender/remove_base_plate.py` strips a backing-plate artefact.

**Tests.** The five new play tests were **rejected before the scene existed**. The sightline sweep then **rejected the first two final builds**: wall slabs poked into floors at bends, and one hid a lower floor down the ramp. The blockout had passed only because its stand-ins were thinner. Mutations removing the organ shield, the plume damage and the half-chamber split each turned the matching test red. The full suites pass ****56/56 EditMode and 171/171 PlayMode** on the final code, zero skips**. Sorrel was rebuilt in its committed blockout mode with the bore added; saves are unchanged.

[Brief](maps/Hushwell.md), [evidence](validation/MAP-Hushwell.md), D119. Hushwell stays optional until the chapter-one quest is rewritten. Its in-player traversal and boss fight, the boss's final exposed-ribs phase, listening and physical feel remain open or UNVERIFIED.

## MAP_EYE_TEST Gullet blockout checkpoint - September 28

**Gullet blockout PASS (agent review); final in-player pass OPEN.** The Gullet is rebuilt from one shared profile as a Choir's anatomy, replacing the repeated sine-wave tube. It has a flared mouth, entry canal, a 60 m feeding chamber, sphincter valves whose membranes span exactly the pinch, and a slalom throat with alternating tissue folds. There is a salvage eddy where a lost skiff, crates and Neve's cache have collected, a luminous nursery chamber, the Cantor's 76 m coil with Compact collar clamps, and an exit valve. Each organ tints the same generated membrane differently. All encounter, membrane, spawn, cache and flag identities are unchanged. Opened overhead and section views show the anatomy; two first-build defects (membranes poking out of the tube, pods reading as scattered buds) were caught and fixed. The new layout checks were **rejected on the old scene**, then passed; the full suites pass **56/56 EditMode and 166/166 PlayMode**. [Brief](maps/Gullet_Tunnel.md), [evidence](validation/MAP-Gullet-blockout.md). Both ordinary Gullet circuit attempts were rejected because another session's Frostbound batch Unity kept taking the foreground; the focus rule was not relaxed. The scripted slice smoke has been stale at the Cinder dock since the September 26–27 station and hull-size changes, and is recorded for C7 repair.

## Defeat finish and rescue banter checkpoint - September 28

**C3/C4 OPEN.** Enemy corpses no longer vanish at full size: after their hold they settle and shrink away over 0.45 s with a dust puff. Killed Chorister fliers tumble and fall below the flight plane instead of hovering on a slight tilt. A swap forced by a downed hero now uses a cover line instead of casual banter. The new checks were **rejected 3/3 on the old code**, then passed 20/20; the full suites pass **56/56 EditMode and 164/164 PlayMode**. The extended defeat probe's opened stills show the shrink and the fall. [Evidence](validation/C3-defeat-finish.md). Continuous video remains UNVERIFIED.

## Soundtrack intake and Draft 3 world expansion - September 28

**C5 OPEN; design only for the expansion.** Nathan's 20 new tracks are committed as hashed OGG sources in `docs/music/tracks.json`; the seven earlier originals are unchanged. The Gullet and flight arena, previously silent, now play **Starfight**. Boss encounters play **Alien Boss Battle** until defeat or removal, and the four original assignments are unchanged. The new check was rejected on the old director, then passed 13/13; the full suites pass **56/56 EditMode and 161/161 PlayMode**. [Music evidence](validation/C5-music-cues.md). The world atlas and campaign are now Draft 3: 52 main and five optional maps, adding the Vaskar Muster and volcanic Scoria, ancient Lanternwick and near-magical Oriel, the Wendmire swamp with Driftreed, the Hoarfell ice world with Lastlight, the moving Meridian Crawl, and six new space areas with distinct rules. Each is tied into the Severance, and the remaining tracks are mapped to regions. [Atlas](WORLD_ATLAS.md#draft-3-expansion--new-regions-2026-09-28), [story](STORY_CAMPAIGN.md#draft-3-amendments-2026-09-28). None of the expansion is built; its art is unfunded beyond the 405 remaining Meshy credits. Listening remains UNVERIFIED.

## C5 lifecycle and defence sound checkpoint - September 28

**C5 OPEN.** A second owned palette (13 clips, `lifecycle-candidate.json`) adds creature and machine deaths, ground dodges, Refract interception, hero down and revive. Its preparation tool still reproduces the original 32 clips byte-for-byte. Non-boss enemies now voice their own death at the body by what they are, replacing one generic crunch played at the killer. Downed heroes hit the deck (or fail like a machine in flight), revival rises, a ground dodge moves air and cloth instead of firing a ship thruster, and Refract interception rings. Boss finishes keep the old crunch until each is designed; Cantor's release must never borrow a kill. New checks were **rejected 2/2 without the wiring**, then passed 37/37 across the related suites. The full suites pass 56/56 EditMode and 160/160 PlayMode**, zero skips. The palette test now requires every staged clip to match a committed provenance record. [Evidence](validation/C5-cues.md). Saves and music are unchanged. Skill-specific voices, flight thrust loops, the three captured 60-second mixes and all listening remain open or UNVERIFIED.

## C4 long circuit completion - September 28

**C4 OPEN; every long circuit now passes with full focus.** On the Release player from `54d57fc`, the corrected Tallow route passes (178.2 s, 10,685/10,685 focused), docking into the refuge with both heroes at 120. Taren's live Gullet circuit passes (336.1 s, 20,163/20,163), clearing both chambers and finishing at 37.7 HP with Sela at full health. Together with Sela's September 27 Gullet/civil passes and today's civil passes, both heroes have now completed every C4 circuit. Captured mixes stay below -10 dBTP. The Gullet integrates at -31.3 LUFS because it has no music cue under the preserved assignments, which is recorded as a music question for Nathan. [Circuit record](validation/C4-long-circuits04.json). C4 still needs its drone/ship failure presentation and continuous-video observation. Physical feel remains UNVERIFIED.

## MAP_EYE_TEST Cinder ring and C4 civil circuit checkpoint - September 28

**Hub_CinderHalo/Hub_Decks final MAP_EYE_TEST PASS again; C4 OPEN.** Both players were rebuilt from `54d57fc` with BUILD_OK. On the Release player, the ordinary civil hull circuits pass for Sela (310.9 s, 18,648/18,648 focused) and Taren (310.3 s, 18,617/18,617). Taren's run closes the previously focus-rejected repeat. Both dock into the Decks at full integrity. The opened perimeter stills show the rebuilt ring as a continuous panelled deck band in the hulls' construction, at the viewpoint that reopened the review. The standard overhead/structure pair agrees. One Taren attempt (`civil-hulls-taren-04`) is retained as rejected after losing focus at 133 s while this session ran shell commands. The captured circuit mixes measure -8.4/-10.2 dBTP true peak and -21.7 LUFS, which is numerical only. [Circuit record](validation/C4-long-circuits04.json) and [map brief](maps/Hub_CinderHalo.md). The long Gullet Taren circuit, the corrected Tallow repeat, the Gullet's spatial redesign and fresh C1 timing remain open. Clean C1 timing is still refused while Frostbound Unity batch jobs run. Continuous video, listening and physical feel remain UNVERIFIED.

## C5 cue wiring and Cinder ring source checkpoint - September 28

**C5 OPEN; Cinder final MAP_EYE_TEST still OPEN.** A melee swing now sounds on its contact window, and impacts come only from damage actually resolved: soft on bodies, armored when shielded, resisted, guarded or on a ship hull, with at most three starting per 50 ms. Each hero keeps a needle family for ground shots, ship shots and Static Net. Hostile fire, previously silent, has its own lower shot. Menus play confirm, error, cancel and move from the actual outcome, and dialogue advance ticks. New checks were **rejected 0/4 and 2/2 against the previous runtime**, then passed 28/28 and 21/21. The full suites pass **56/56 EditMode and 158/158 PlayMode**, zero skips; they predate only the two-call Net fix, which its targeted run covers. [Cue evidence](validation/C5-cues.md). The back ring is rebuilt as 96 native-proportion deck-plate/fascia spans in the hulls' own module language, with the section and hull contact unchanged. `StationRedesign.Exterior` touched only the exterior scene, whose object inventory differs only in ring pieces. Controlled before/after views pass, as recorded in the [map brief](maps/Hub_CinderHalo.md). The ordinary in-player perimeter recapture is next. Saves and music are unchanged. Listening, continuous video and physical feel remain UNVERIFIED.

## C5 mixer, saved controls and footstep checkpoint - September 28

**C5 OPEN.** Music, SFX, Ambience and UI now route through an exposed Master bus, with independent title/pause controls saved per profile (isolated files during tests) and the legacy master volume migrated. Effects use at most 24 pooled, priority-reclaimed voices with per-emitter cooldowns and non-repeating variations; world sounds pause and release on unload, and priority SFX briefly duck music. Footsteps fire from the planted sole on the actual supporting collider, with four variations each for metal, rock and soil, and stay silent while blocked, airborne, paused, changing form or downed. Repeated Sela fire exposed gait restarts that suppressed every footfall; pace changes now keep normalised phase. Final results: **22/22 targeted PlayMode, 6/6 preference EditMode, then full 56/56 EditMode and 154/154 PlayMode, zero skips**. Both players rebuilt with BUILD_OK, and the PCM routing fixture accepts 24/24 samples. Opened 720p/1080p settings frames confirm the hidden title menu, marked selection and fitting text, and expose one cosmetic defect: the slider handle sits on the end-cap art at 100%. An initial "missing pause backdrop" reading was wrong; the measured 95% dim is present. [Evidence](validation/C5-mixer-footsteps.md) retains every rejected stage. Implementation is from the Codex session; the final full suites and opened stills are from the Claude Code session. Saves and music originals are unchanged. Melee/skill/flight/death cue families, the three captured 60-second mixes and actual listening remain open; audition, continuous video and physical feel remain UNVERIFIED.

## C4 long circuit and map rejection checkpoint - September 27

**C4 OPEN.** Sela passes the complete 310.858-second civil and 336.608-second live Gullet circuits with full focus; both parties survive. Both Taren runs lose focus to unrelated Unity work and remain rejected. Tallow completes traversal/dialogue/docking with full focus but rejects one incorrect pre-dock scene expectation; its correction awaits repeat. [Evidence](validation/C4-long-circuits03.md) preserves all five results and sixteen actually opened stills. The perimeter frames reopen the paired first-station final MAP_EYE_TEST for stretched, ragged ring panels; the Gullet remains spatially rejected. Saves are unchanged. Both packages still contain `a759b1d`; new C5 source is separate and under focused verification. Continuous video/audio observation and physical feel remain UNVERIFIED.

## C4 staged form and rescue player checkpoint - September 27

**C4 OPEN.** Full suites pass **137/137 PlayMode and 50/50 EditMode, zero skips**; both Windows players contain `a759b1d`. Both heroes complete ordinary dock/launch and wall-disable/automatic-swap/proximity-revive routes with full focus. The 35-check safe-boundary route also passes after preserving and correcting the old fixture's insufficient reset windows. Twenty-three opened gameplay stills support attached full-size folds and visible, separated rescue hulls. [Evidence](validation/C4-staged-player.md) retains four rejected captures and exact fixes. Casual swap banter during rescue is a new C6 context issue. Saves and music remain unchanged. Long circuits, remaining MAP_EYE_TEST work and later gates remain open; continuous video/audio observation and physical feel remain UNVERIFIED.


## C4 disabled flight source checkpoint - September 27

**C4 OPEN.** Disabled ships keep a solid rescue footprint with damage triggers shut down, settle with a small wing fold, and display a state-driven rescue ring. Recovery starts from the current pose. Reproduced hull overlap, a 57° early-revive jump, absent cue, one-frame pause drift and stale landing cue are corrected. The combined matrix passes **16/16 PlayMode, zero skips**; twelve opened controlled views support the candidate pose/cue. [Evidence](validation/C4-flight-disable.md) separates that review from pending ordinary gameplay. Original saves are unchanged. Full suites and both rebuilt packages are next; players still contain `feac4b9`. Long circuits, map work and later gates remain open; video/audio observation and physical feel remain UNVERIFIED.

## C4 staged transformation source checkpoint - September 27

**C4 OPEN.** Full-size humanoids now tuck and fold their attached vanes; generated ship wings fold before replacement and open afterward. Ordinary docking waits for departure folding and opens arrivals during fade-in. Six reproduced runtime failures plus unreadable flight imports are corrected; the focused matrix passes **12/12 PlayMode and 6/6 EditMode, zero skips**. Thirty opened controlled stills support intact candidate geometry, with exchange readability still awaiting player capture. [Evidence](validation/C4-staged-forms.md) retains the failed fixture cleanup and renderer attempt too. Human saves are unchanged. Full suites and both rebuilt players are pending; packages retain `feac4b9`. Disable/overlap, long circuit acceptance, map work and later gates remain open; video/audio observation and physical feel remain UNVERIFIED.

## C4 long circuit rejection checkpoint - September 27

**C4 OPEN.** Both five-minute civil circuits complete navigation but confirm outside docking range. Both Gullet circuits clear their two chambers; focus loss rejects both, and Sela's later wall strike triggers down/automatic swap/revival. Tallow docks and returns with full focus, but its approach target lies inside the enlarged hull clearance. All five remain rejected. [Evidence](validation/C4-long-circuits-rejected.md) retains exact failures and opened stills, including a newly visible overlap between a live and disabled craft. Revised ordinary approach/tactical inputs await recapture with the staged transformation candidate. Full suites, map work and later gates remain open; video/audio observation and physical feel remain UNVERIFIED.

## C4 ordinary flight skill review - September 27

**C4 OPEN.** Both players rebuilt from `feac4b9`. Both short release skill routes pass with all four ordinary input activations, full focus (2485/2485 and 2515/2515 samples) and living parties. Opened gameplay frames now show readable attached exhaust, distinct Cleave/Pulse boundaries, airborne net placement and actual-state hull cues. [Evidence](validation/C4-flight-skills-player.md) preserves the substantial pilot damage and separates still-image acceptance from continuous observation. Both new civil repeats are rejected after backing away outside dock interaction range; Taren also loses focus. Long Gullet runs are still in progress. Full integrated PlayMode, staged transformation/disable, map work and later gates remain open. Original saves are unchanged; video/audio observation and physical feel remain UNVERIFIED.

## C4 visible flight skills source checkpoint - September 27

**C4 OPEN.** Flight Cleave/Pulse now damage at visible travelling edges; Static Net launches from the posed nose, respects cover and range, and retains its flight plane and team after source cleanup. Buffs anticipate before arming and retain hull cues for their actual state. Eight baseline failures and two deliberate faults are preserved; a new source-cleanup test also catches a real friendly-fire bug. The corrected nine flight/eight shared ground checks pass **17/17** after an earlier **31/31** combined pass; full EditMode passes **48/48**, zero skips. Brighter exhaust and lunge energy pass opened controlled attachment views; ordinary-player readability and full integrated PlayMode remain pending. [Evidence](validation/C4-flight-skills.md) separates all results and source/build limits. Both packages still contain `abaf4be` pending rebuild. Original saves remain unchanged. Staged transformation, flight disable, long focused circuits, map work and later gates remain open; video/audio observation and physical feel remain UNVERIFIED.

## C4 integrated hull collision review - September 27

**C4 OPEN.** Full suites pass **118/118 PlayMode and 48/48 EditMode, zero skips**; both Windows players now contain `abaf4be`. Both short ordinary Gullet routes pass with full focus, eight lunge kills each and separated craft in opened regroup images. The active pilot ends at 85.1 HP, preserving the wider damage body's gameplay consequence. Both 309-second civil captures dock successfully with living parties but are **rejected** for focus loss and approach goals inside the new pad clearance; corrected goals await a clean repeat. Actual gameplay frames also reject the faint exhaust despite passing attachment tests. Sela's opened rear ring views expose ragged panel-edge projections for visual cleanup. [Evidence](validation/C4-flight-hulls-integrated.md) retains all results and actual viewing limits; [flight direction](FLIGHT_DIRECTION.md) records the implemented control contract. Original saves remain unchanged. Flight skills, staged transformation/disable, MAP_EYE_TEST work and later gates remain open; motion/audio/physical feel remain UNVERIFIED.

## C4 measured hull collision source checkpoint - September 27

**C4 OPEN.** Flight collision/damage bodies now contain the measured hull through bank/roll; arrival spacing and nearby revival respect those dimensions, and landing restores the cached ground body. Three red regressions reproduce overlapping arrivals, 2.42 m interpenetration and 1.17 m wall penetration. The corrected hull/contact matrix passes 11/11. Lunge contact follows its visible energy edge; an injected early hit is rejected. Opened effect renders also expose padded-texture detachment, corrected with explicit UV mapping. [Evidence](validation/C4-flight-hulls.md) preserves all stages and actual opened stills. Full PlayMode is **117/118**: a fixed centre-position contact fixture fails because the larger hull contacts sooner. Its direct-collision witness passes 1/1 and rejects actor-as-wall damage; the fault is removed. Full EditMode passes **48/48**, zero skips. Full PlayMode retry and both rebuilt players remain pending; packages still contain `e78c606`. Original saves are unchanged. Ordinary revised flight recapture, other C4 work, non-station MAP_EYE_TEST and later gates remain open; motion/audio/physical feel remain UNVERIFIED.

## C5 owned palette preparation checkpoint - September 27

**C5 OPEN.** A reproducible read-only source pipeline now prepares 32 small sound candidates outside Unity: four footstep variations per surface, three common impact/swing/emitter variations and five UI/dialogue cues. Recipes retain all 37 original and four licence hashes. The 1.07 MB candidate set passes silence/peak checks at -10.0 to -9.2 dBTP, and attempted output reuse is rejected without changing audio. [Evidence](validation/C5-palette-candidate.md) separates numerical checks from listening. These derivatives are not yet staged or assigned; runtime pools/mixers/cue timing and actual audition remain pending. Music is unchanged. C4's expanded full integration run is in progress; neither this tool nor its measurements accepts flight, sound quality or the workshop.

## C4 hull emitters and thrust source checkpoint - September 27

**C4 OPEN.** Ordinary/fan shots now release from the posed generated nose; flight lance/counter respect static and movable cover. Measured red origins were about one metre detached, and old near-cover/beam branches dealt damage through cover. Six corrected flight checks pass. Opened hull views reject Sela's first centre engine marker; corrected geometry passes both prefab cases. Nozzle-attached exhaust now follows actual thrust/boost, clears during coast/death and holds during pause; three new red/green state/attachment checks pass. [Evidence](validation/C4-flight-emitters.md) retains exact results and the distinction between opened socket geometry and unreviewed runtime exhaust. Original saves are unchanged. Full suites and rebuilt players remain pending for this source checkpoint; both packages retain `e78c606`. Hull collision, remaining flight/transform work, non-station MAP_EYE_TEST and later gates remain open; video/audio observation and physical feel remain UNVERIFIED.

## C4 integrated flight/contact/form checkpoint - September 27

**C4 OPEN.** Repeated and reversed form requests no longer reset the visible body's scale; Civil/Combat flight retains its live hull. Both stages of reproduced failures and the six-case correction are retained. Full suites pass **104/104 PlayMode and 44/44 EditMode, zero skips**, and both Windows players rebuilt. Ordinary Gullet routes record four Taren and five Sela lunge kills with full focus and living parties; two preceding arena runs pass movement but fail contact coverage. The corrected 35-check safe-boundary route passes for both heroes after the first route's missed crossings are rejected. Opened regroup stills expose intersecting ship hulls, which remains an explicit C4 defect. [Integrated evidence](validation/C4-flight-integrated.md) records hashes, original-save preservation and observed limits. Sorrel remains a blockout in both packages. Staged folding, remaining flight work, non-station MAP_EYE_TEST and later production gates remain open; continuous video/audio observation and physical feel remain UNVERIFIED.

## C5 signal-check tooling checkpoint - September 27

**C5 OPEN.** A repeatable, read-only captured-mix checker now retains FFmpeg true-peak/loudness logs and source hashes. Seven controls pass after a rounded-ceiling case first reproduces a false acceptance. First-minute Decks and Sorrel captures measure -7.4/-9.3 dBTP, below the unchanged -1 dBTP ceiling. [Evidence](validation/C5-signal-check.md) separates these numerical baselines from required sound design and listening. Actual audition remains UNVERIFIED; no game audio or music changed. C4's full integration run remains in progress; this tooling commit does not change either player package or close a production gate.

## C4 actual flight contact source checkpoint - September 27

**C4 OPEN.** Three red cases show lunges hitting through cover, from a stationary craft and ahead of actual travel. Lunge and Taren flight dash now resolve from completed motor segments, with cover checks, one hit per target and distance-based visual bursts. The expanded six-case matrix passes. [Contact evidence](validation/C4-flight-contact.md) preserves the failures. Full PlayMode reached **97/98, zero skips**: an existing Overdrive fixture allowed restored fortune to contaminate an exact damage assertion. Its corrected synchronous samples pass 1/1 with the same 100/120 thresholds. Full green and both rebuilt players remain pending; this is a source checkpoint. Development remains Sorrel blockout 06, Release the preceding integrated C3 package. Original saves are unchanged. Ordinary ship routes and transformation regressions are prepared; observation and all open production gates remain pending.

## C4 flight integration checkpoint - September 26

**C4 OPEN.** Matched 30/60/120 timestep checks reproduce a 10.55% stopping-distance spread. The actual flight motor now integrates thrust/drag and average-velocity travel with bounded calculation steps; the same brake travels 1.334 m at all three rates. Cruise/boost caps and tuning constants remain unchanged. Thrust/reversal, coast and zero-time momentum checks pass. Fresh results: **44/44 full EditMode and 5/5 targeted PlayMode, zero skips**. Original saves are unchanged. [Evidence](validation/C4-flight-integration.md) records the red control, measured behavior change and source/build limits. This is a source checkpoint; full PlayMode and both rebuilt players remain pending. Continuous flight observation, remaining flight/transform work and all earlier open gates remain open.


## MAP Sorrel blockout checkpoint - September 26

**Sorrel blockout/final MAP_EYE_TEST OPEN.** The isolated candidate replaces the repeated three-lane sheet with a connected basin, bending haul/seam/service routes, a landing/receiving/stores sequence, separate repair work space and inward-facing habitation approaches. Existing scene/spawn/encounter/mine identifiers are retained. Rejected views remain archived: the first terrain obscured arrival and read as a rectangular tray; later tests exposed a long cliff bypass and a seal that opened physically without releasing companion navigation. Both reproduced navigation defects now pass unchanged closure/open/return/reclosure checks.

The Development blockout player passes a 36-check ordinary virtual-input outpost route in 153.682 seconds, with 9,134/9,134 focused samples. Both heroes reach hab approaches and stores, two crystals are mined, repair saves at Outpost, and the party returns to landing. Opened player frames show actual access, standing space and companion presence. A four-angle generated-prop audit identifies the hab door on +Z and its real 5.673 m footprint; final inward orientation is prepared but conversion remains pending. The first western loop cleared all four packs but is rejected for an unrecovered downed hero, missed return points and focus loss; the revised run clears all four packs with both heroes alive, but focus loss rejects its return. The outpost rerun is also focus-rejected. Blockout 06 corrects buried launch pads and grades the full northern apron, verified in opened views. An interrupted barrier fade also reproduced a partially erased closed surface; its correction and the companion-navigation check pass 2/2 PlayMode, zero skips. Remote traversal, final-art integration, full suites and the final player pair remain pending for this map. Release still contains the previous integrated C3 checkpoint; Development currently contains the explicit Sorrel blockout. Original saves are unchanged. [Map brief](maps/Sorrel_Ridges.md) records layout and review details; [blockout evidence](validation/MAP-Sorrel-blockout.md) separates checks and limits.

Other-project Unity/player activity again prevents clean C1 timing. Normal/slow video observation, actual sound audition and physical controller feel remain UNVERIFIED. The other three non-station maps and remaining production gates remain open.

## C3 ground creature support checkpoint - September 26

**C3 OPEN.** Ridgehound, Scrapmite and Burrower now support their actual mesh throughout collapse on flat/sloped floors and use individually inspected resting poses. Red regressions expose both mid-fall penetration and bodies propped on extremities; the corrected nine-case matrix passes clearance, body-height, pause, hold and root-position checks. [Creature evidence](validation/C3-ground-creatures.md) retains rejected captures, the actual geometry audit and opened player views.

Fresh integrated suites pass **90/90 PlayMode and 39/39 EditMode, zero skips**. Both Windows players rebuilt, including the preceding projectile correction. The ordinary release route passes all 43 checkpoints with 2,672/2,672 samples focused: three Ridgehounds die, their bodies hold/clear, and both heroes return to the safe pocket. The first route's wrong Shaped expectation inside that pocket remains preserved as a rejected fixture. Original saves are unchanged. Rigid limbs, fuller creature reactions and flight defeat remain open; normal/slow video viewing, sound audition and physical feel remain UNVERIFIED. Other-project Unity work again prevents clean C1 timing. The four non-station maps and later gates remain open; isolated map drafts are prepared for the next geometry pass.


## C3 released projectile checkpoint - September 26

**C3 OPEN.** Projectiles resolve the nearest body/opaque surface, stop on movable props, retain their team after source destruction and reconcile active counts on scene unload. Four red cases reproduce wall penetration, wrong nearest contact, stale accounting and lost allegiance. The combined projectile/ranged/break checks pass **17/17 PlayMode, zero skips**, with original saves unchanged. [Projectile evidence](validation/C3-projectiles.md) records the corrected destruction-order fixture and all red/green results.

This is a source checkpoint; full suites and both player rebuilds await the next integrated checkpoint. Current packages retain `48bdff9` (85/39 integrated). C1 clean timing remains rejected for other-project interference; nonhumanoid death, non-station MAP_EYE_TEST and later gates remain open. Continuous motion/audio observation and physical feel remain UNVERIFIED.

## C3 posed buffs and counter checkpoint - September 26

**C3 OPEN.** Overdrive and Refract now arm at explicit pose phases with wrist cues tied to their actual state. Interception consumes Refract and starts a posed return shot; deliberate dodge/guard replace the ward. Red cases reject damage through fixed/movable cover, an unposed return and a stolen Flash Move. Near-wall release checks stop energy from appearing beyond the cover. [Buff/counter evidence](validation/C3-ground-buffs.md) retains failures and exact hashes.

Fresh integrated suites pass **85/85 PlayMode and 39/39 EditMode, zero skips**; both Windows players rebuilt with BUILD_OK. The final ordinary release route passes 28 checkpoints with all 3,987 samples focused. Opened frames show both buff cues, expiry, interception, return beam, break/death and retreat; no combat difficulty was changed. Original saves remain unchanged. Continuous normal/slow viewing, actual audio audition and physical feel remain UNVERIFIED. Fresh C1 timing, other C3 work, non-station MAP_EYE_TEST and later gates remain open. The final-layout C1 retry is rejected for concurrent other-project Unity work and focus loss; subsequent clean starts refuse that environment. [Rejection record](validation/C1-final-timing-rejected.json).

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
