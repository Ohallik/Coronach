# Coronach — combat animation direction

2026-09-23. Combat is the next production priority. The current pass replaces the slice's repeated walk/swing animation, but it does not establish final combat quality. Expand the campaign only after the first pair can sustain an enjoyable five-minute encounter.

2026-09-26: Nathan reports bodies facing right while running in combat form and inadequate deaths. The sideways-run source is now isolated to donor clip root orientation; per-clip corrections pass independent body-direction regressions. C2 ordinary-player and observed-motion acceptance remains open. [PRODUCTION_PLAN.md](PRODUCTION_PLAN.md), especially C2–C4, now governs continuous motion, facing, stride, combat and death/revive acceptance. Previous key-pose renders do not close these findings.

## Implemented foundation

- Separate walk, jog and sprint clips, selected and timed from actual post-collision unscaled motor displacement. C2 adds generated-sole calibration, bounded two-bone contact correction, deliberate lock-on side/back stepping and short physical start/braking phases; independent flat/ramp/heading tests pass, with ordinary-player and watched-motion acceptance still open. Civil walking is 2.6 m/s; holding sprint reaches 5.4 m/s. Combat retains its faster traversal.
- Taren has three different ordinary swings, a stronger third hit, a short combo input buffer, a held guard pose, roll, stagger, cleave, dash and cast motions. Sela has a mirrored left-arm ranged stance and shot plus skill poses. A masked firing layer preserves her moving leg stride; the base and action clocks remain independent, with rendered-sole/pause regression and ordinary-player still evidence in C3-moving-fire.
- Ground damage waits for the attack wind-up. Taren's cuts/Arc Cleave sweep the visible wrist edge during move-specific animation phases. Sela's needles, Scatter Bloom and Thread Lance release from the posed left hand; the lance stops at cover along its visible path. Dodge, guard, stagger and death cancel pending work, and pause holds it. Ember Dash now moves through the controller during its posed blade sweep; Pulse releases a visible expanding wave, and Static Net throws a visible, cover-limited seed. Overdrive and Refract now arm from explicit pose phases, with persistent wrist cues driven by their actual windows; interception starts a posed Refract return beam. Their continuous observed-motion review remains open. See [C3 ranged evidence](validation/C3-ranged.md).
- The generated Shaped vanes now attach to Chest/Hips. Unity previously rejected parenting into the imported model instance, leaving them behind during torso motion. Intake now unpacks that hierarchy before attaching them and verifies the parent.
- Ground Ridgehound, Scrapmite and Burrower now use mesh-supported falls and individually inspected resting poses. Flat/sloped support and ordinary-play stills pass; rigid limbs, fuller reactions and continuous-motion acceptance remain open. [Creature checkpoint](validation/C3-ground-creatures.md).
- Flight uses two dedicated generated spacecraft: Taren's broad ivory/black/amber hull and Sela's narrow cyan/violet swept hull. Banking, boost pitch and barrel roll act on the visible ship, leaving the gameplay collider stable. Flight integration now preserves matched braking distance across 30/60/120 frame durations. Lunge and dash-skill contact follow actual collision-limited travel with cover checks. The transition still collapses one form and expands the next over 0.6 seconds; it is not yet a skeletal panel-folding transformation. Repeated requests now coalesce, reversal grows the visible body from its current scale, and Civil/Combat flight keeps the same live hull. Targeted interruption/rapid-request/pause checks pass; authored folding and continuous visual acceptance remain open.

Animation sources are the owned CC0 Quaternius Universal Animation Libraries 1 and 2, stripped to skeletons and clips. All visible character and ship geometry remains generated. This is a retargeted working moveset; a library's clip count is not a substitute for authored choreography.

## Next choreography targets

| Fighter / move | Silhouette and purpose | Required mechanical distinction |
|---|---|---|
| Taren — three-cut chain | Short diagonal opener, reverse cut across the body, committed overhead finisher | Clearly separate anticipation/contact/recovery; finisher breaks posture and exposes a punish window. |
| Taren — Rift Step | Shoulder leads a low dash; blade follows in a single long arc | Reposition through a gap, never a teleport through solid walls. |
| Taren — Anchor Break | Both feet plant, vanes brace, a rising heavy strike opens the chest | Slower anti-armour move with a readable charge and interrupt point. |
| Taren — Halo Reap | One complete waist-height sweep, then a firm planted recovery | Crowd control with limited forward reach; no repeated damage on one target from one sweep. |
| Taren — counter riposte | Small guard catch followed by a short, direct thrust | Reward a successful Flash Guard; do not require a long cinematic pause. |
| Sela — needle string | Three left-emitter shots with a progressively stronger braced pose | Reliable movement firing, then a briefly planted precision finish. |
| Sela — Scatter Bloom | Arm crosses the torso, opens outward, five projectiles fan from the hand | Close crowd control with a visible spread, distinct from a piercing line. |
| Sela — Thread Lance | Vanes align and the wrist locks on a target before release | Strong piercing beam with a clear aiming commitment. |
| Sela — Static Net | Throw from a low coil into an open hand, then recoil | Persistent area denial; field placement and activation must be readable. |
| Sela — Refract | Forearm turns a shot aside; hips follow into a return shot | A timing tool whose return animation follows actual interception. |
| Both — ship attacks | Nose-mounted fire; side-mounted energy edge on lunge; barrel roll on evade | Muzzles must follow ship geometry. Boost, braking, lunge and roll each need distinct trails and sound. |

Names and future mechanics above are proposals. Existing four-skill data remains authoritative until each move is implemented and tested. Do not advertise these as an unlocked attack roster.

## Acceptance for the next combat milestone

1. Review at gameplay camera distance and quarter speed: planted feet, no knee inversion, no wrist/weapon separation, no floating vanes and no snap back to idle between chained attacks.
2. Author contact/recovery events per move. Place the visible edge and damaging volume together; reject a hit that arrives before the edge or behind the fighter.
3. Add directional starts, stops and turns, then strafe/backpedal for lock-on. Match stride displacement to ground speed; stop using one forward jog for every fighting direction.
4. Tune cancellation, hit stop, impact sound, enemy recoil and camera response together. Keep the target visible and reserve large flashes for significant hits.
5. Review a full five-minute ground encounter and a flight encounter on the physical Logitech, including rapid taps, movement while attacking, guard/dodge cancellation and character swap. Automated route completion and static pose renders cannot close that feel check.

The current visual-quality rejection remains open. Environment composition, detailed hand/cloth deformation, character-specific movement and attack effects still need work.

## C3 ground skill contact checkpoint

The current existing skills have these explicit contact windows. These implementation values do not accept continuous motion or the proposed future moveset above.

| Move | Action take and duration | Contact / travel | Recovery ends |
|---|---|---|---|
| Ember Dash | Dash, 0.55 s | Root travels through collision at phases 0.25-0.75; visible wrist edge contacts at 0.25-0.78 | 0.495 s |
| Pulse | Buff/open-hand cast, 0.55 s | Phase 0.46 releases a 0.28 s wave to 5 m; one hit per exposed victim | 0.49 s |
| Static Net | Pulse/throw, 0.65 s | Phase 0.255 releases from right hand; 0.34 s arc, at most 6 m, solid cover stops placement | 0.56 s |
| Overdrive | Buff/open-hand, 0.6 s | Phase 0.42 arms the original ten-second 20% bonus and two amber wrist cues | 0.51 s |
| Refract | Buff/open-hand, 0.5 s | Phase 0.34 arms one 1.4-second interception and a cyan left-wrist ward | 0.425 s |
| Refract return | Shoot, 0.45 s | Actual interception begins the pose; phase 0.46 emits a cover-limited 11 m beam | 0.4 s |

Ground cast work cancels on owner/action interruption; already released energy completes its bounded lifetime. Pause holds both. Particle ring crests are mapped to their actual damage boundary, accounting for the owned texture's padding; cover clips the visible surface as well as the damage. [Ground skill evidence](validation/C3-ground-skills.md) retains functional and visual rejections. Existing four-skill definitions remain authoritative; no extra move is unlocked by this checkpoint.
