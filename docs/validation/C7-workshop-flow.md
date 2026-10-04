# Workshop ordinary-input flow - October 3

**OPEN.** Reviewer/operator: Codex, agent-directed virtual gamepad through the
normal input stack. This is a chained playthrough from New Game with earned
gear. No development loadout, actor action calls or health edits are used in
these player runs. Physical Logitech, continuous motion/audio acceptance and
Nathan's assessment remain separate/unverified.

## Recorder and continuation checks

`Builds/quality/workshop/c7-regressions/` retains the evidence.

- Four runtime tests fail on the previous recorder: no Title confirm input,
  dropped Title frames, no observed bench, silently ignored UI expectation.
  `title-menu-runtime-seven-green` then passes those four plus the three existing
  replay regressions (7/7). Missing parties still reject world checkpoints.
- `menu-analysis-red` rejects the old analyzer's blanket menu-pause rule. Eight
  checks pass after adding declared/observed menu evidence. Deliberate removal
  of UI observation, timing-mode rejection and focus rejection each fails the
  corresponding controls (`menu-*-mutation-red`). Menu frames are never dropped.
- Six resume checks pass. Independently removing the evidence, development
  fixture, path, overwrite, slot-validity or byte-integrity guard fails its
  control (`resume-*-mutation-red`). The copier reads only prior isolated runs
  beneath Builds/quality and refuses an existing destination save directory.

Run continuation with `scripts/quality-replay.ps1 -ResumeFrom <prior-run>`.
The route must start at Title with starterParty=false and no loadout. Both
prior `run.json` and `analysis.json` must pass. `resume.json` records exact slot
and source-evidence hashes. Copies contain only ordinary slot JSON files; the
player itself chooses Continue and loads a slot. `final-state.json` is a
read-only snapshot, not a new save operation.

## Current accepted chain

| Run under Builds/quality/workshop | Result | What it establishes |
| --- | --- | --- |
| c7-new-game-03 | 102.791 s; 6,152/6,152 focus | Title New Game, office docking, Orrin/Sela, Mira shop, Hal gift/bench, crafting/equipment, manual slot1 |
| c7-continue-sorrel-03 | 130.331 s; 7,806/7,806 focus | Title Continue loads slot1, launch/flight/landing, Survivor, outpost repair/autosave |
| c7-defeat-retry-01 | 61.586 s; 3,682/3,682 focus | Both heroes fall to live hounds; real defeat menu retries the outpost autosave; repair follows |
| c7-haul-preparation-01 | 231.528 s; 13,878/13,878 focus | Four haul encounters, both ridge crystals, ordinary return to outpost repair |
| c7-drill-equipment-01 | 111.841 s; 6,698/6,698 focus | Three crafted gels, refined edge, two frames, both heroes equipped, manual slot1 |
| c7-burrower-03 | 179.961 s; 10,784/10,784 focus | Continue slot1, repair/haul, whole-party boss entry, 48.1 s Burrower fight, key and autosave |

All six runtime and offline analyses pass. The first run retains 728 paused
bench frames. The defeat run observes 122 defeat-menu frames and 214 frames with
both heroes down. These captured/menu runs are excluded from clean C1 timing.

Town's saved gameplay state matches the final observation: two level-one heroes,
60 scrip, three Scrap Alloy and three Lattice Filament, Edges XP=30, one purchased
EmittersT1 and three crafted EdgesT1. Taren has the emitter and an edge equipped.
Continue preserves their exact instance IDs. The outpost repair restores both
heroes to 120 integrity/100 charge and saves Sorrel_Ridges/Outpost; the Survivor
advances Sorrel to step 2. Retry restores that same earned state.

Opened town views 051/063/072 show the crafted inventory, equipment menu and
manual save selection. Continue views 005/013/021/028 show the chosen slot,
landing form, generated Survivor and outpost repair. Defeat view 009 shows both
heroes down and the actual Retry choice. Earlier town menu inspections exposed
the incorrectly assumed recipe order and were used to correct ordinary d-pad
navigation. No source changes were needed for those route-authoring mistakes.

## Whole-party encounter entry repair - October 4

The haul preparation save contains two level-three heroes at full 156 integrity,
39 alloy, 11 filament, two crystals and 204 scrip. The bench spends earned
materials on three Repair Gels, EdgesT2 and two FramesT1. Slot1 retains the exact
instances: Taren carries the refined edge and a frame, Sela the purchased emitter
and the other frame. It leaves 21 alloy and 11 filament. Opened bench views
035/053/075/087/099 show fabrication and both equipment selections.

`EncounterEntryTests` first rejects the stranded living/downed companion,
incorrect loaded Open state and six-metre wall sightlines (`entry-state-sightlines-red`,
4/4 failures). The initial entry guard alone also failed because the Open property
was not serialized (`entry-initial-guard-red`). The repaired entry waits for every
body to cross the open gate with collision clearance. An isolated scene upgrade
lowers only the two Sorrel seals to 0.6 m. A subsequent disabled-collider mutation
fails the travel-blocking assertion (`seal-collision-mutation-red`). The final
combined Sorrel/Gullet/Hushwell/entry checks pass 17/17 in
`entry-map-collision-seventeen-green`. No enemy tuning changed.

Opened accepted boss views 020/027/063 show both heroes inside, the low entry
seal and key recovery. Both survive at level four, Taren 139.4 and Sela 100.2
of 174 integrity, with two gels left. The large boss body still occludes Taren
at some melee bearings; this route pass does not accept that presentation.

## Preserved rejections and remaining work

- `c7-new-game-01`: dock waypoint extended into hull clearance; Orrin/Hal
  waypoints stopped outside prompt range. Adjusted reachable standing positions,
  unchanged tolerance. It did successfully purchase the emitter.
- `c7-new-game-02`: Repair Gel also sorts before Scrap Edge; the incorrect row
  selected a locked recipe. The capture and absent craft XP expose that failure.
- `c7-continue-sorrel-01`: insufficient braked flight time led to the wrong dock.
  Measured travel received longer steps without changing the destination checks.
- `c7-continue-sorrel-02`: expected Natural at the landing outside the outpost's
  safe pocket. The exact expectation is now Shaped, consistent with the zone and
  visible arrival. The rejection remains intact.
- `c7-ridges-burrower-01`: 380.789 s, rejected. Four haul encounters clear with
  earned gear. Both heroes reach level 3 but arrive near 53/55 of 156 integrity.
  Opened 064/071 show the encounter seal separating the party: Sela remains on
  the far side, then inherits control with the boss behind the opaque membrane.
  Both fall, and later queued confirms retry; that does not salvage the run.
  The entry repair above resolves the stranded-party defect; enemy balance stays unchanged.
- `c7-burrower-02`: 175.966 s, rejected for a missed outpost walking checkpoint.
  The repair detour made the civilian walk about 19 m, too far for its old six
  seconds at 2.6 m/s. It ended at (-1.48, 21.57) instead of (0, 25). Increase
  duration to nine seconds with the same destination/tolerance; `-03` passes.
  Although `-02` cleared the boss, its saves never seed another segment.

Nathan's autosave/backup hashes match the session manifest after the town and
Continue captures and again after the preparation work. Required final integrated validation and New Game-to-Tallow
completion remain due; this document does not close C7 or any remaining map gate.

Raw artifact/source hashes: [C7-workshop-flow-evidence.json](C7-workshop-flow-evidence.json).
Entry/preparation extension: [C7-encounter-entry-evidence.json](C7-encounter-entry-evidence.json).
