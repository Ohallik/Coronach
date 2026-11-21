# Uphill fold (Sela's "high stepping") - November 4, fixed November 12

Status: **fixed in the pose, measured; not yet watched in a player.** `GroundFeet` poses uphill walking upright (UprightClimb, D259). The audit passes every case on the slopes the game has, for all three heroes and both ground forms. Continuous motion and pad feel still need a person.

## What it is

`Lattice.EditorTools.UphillStepAudit.Run` walks each hero and form up a sloped floor at the recorded 1.47 m/s through the real `GroundFeet`. Its bounds reject the recorded live fold (Sela, `C2-evaluated-pose.md`): a 60.88 degree knee level with the hip, hips at 60% of their flat height, a 19 cm bob. Pass: swing knee at least 80 degrees and 12 cm under its hip, lowest hip at least 75% of flat, bob at most 12 cm.

## Why it folded

The leg chain was posed in the slope frame: the support rotation tilted pelvis and legs back off the slope by its whole angle, so the hips sat behind and below both feet. To get them back over the feet, the lower spine was pitched forward 1.9 times that angle, which is the torso diving into the slope, and the hips were slid 65% of the way back upright after the foot targets were fixed. That stretched the trailing leg, so the support drop pulled the hips down by up to 36 cm with about 20 ms of smoothing: the pump. With the hips down, the leading foot, already placed up the slope, had nowhere to go but up beside the hip. Every repair of the tilted frame (the shift, the drop) made the legs fold. Five retunes (table below) moved the fold around without removing it.

## What changed (`GroundFeet`, UprightClimb)

Only when walking, running or sprinting up a grade of more than about 10 degrees (`uprightWeight`, smoothed over 0.2 s; 0 on the flat and downhill, where nothing changed):

- **Hips and torso stand in the gravity frame.** The chain's support rotation fades out, so the torso is upright with no spine counter-pitch and no balance shift.
- **Only the feet keep the support frame** (`footTilt`): each foot's heel-to-toe roll is the take's, laid on the slope.
- **Foot targets follow the ground.** The take's path is raised by the ground's rise under each foot. Behind the hips a lifting foot keeps the level path, which clears the falling slope; the corner between the two is rounded over 4 cm (a sharp one jerked the swinging knee as the foot passed under the hips).
- **The step covers level distance.** The profile's stride is along the slope; upright, it is divided by the same travel factor, so a steep climb takes shorter steps.
- **A climbing step holds its heel to the handover and its toe from it.** The foot rolls flat onto the slope about the heel, and the toe is held where that put it. With both holds loosened at the handover, the foot drifted back to the take's path and the knee straightened at up to 1500 deg/s. A held heel lands over the whole landing ramp. A hold picked up again after a gait blend is not a landing: it plants where the foot is, and its heel lift eases in from there (at 39 degree starts and stops a stale latch planted it in one frame, 9.5 m/s at the knee).
- **The trailing heel peels up** about the planted toe, rigidly, by the least angle that keeps the ankle in reach (at most 60 degrees). It eases in over the toe hold's first 0.065 of the cycle. The toe stays exactly where it was set down. Whatever the heel can't give, the existing support drop still takes.
- **A hold a stop or a gait blend drops lets go over 0.2 s** from where the foot stood. Upright, the take's path is not where a climbing foot was planted, and a planted foot jumped to it at a stop (9 m/s at the knee). The walk's knee room (the hips lowered for a long step) eases out over 0.06 s on a climb, where it used to vanish in one frame at a stop.
- **The steep solver's extra 4 cm reserve** belongs to the tilted chain, so it fades with `uprightWeight`.

## Measured (`Builds/quality/C2/step-height/`)

Slopes from `Lattice.EditorTools.GroundSlopeSurvey.Run` (`slope-survey.txt`). Walkable ground (baked navigation, 35 degree agent limit), 99th percentile / steepest whole-step slope:

| Zone | p99 | max |
|---|---|---|
| Hushwell | 20.7 | 31 |
| Sorrel_Ridges | 28.6 | 34.7 |
| LanternGorge | 29.8 | 34.8 |
| Lanternwick | 31.0 | 34.9 |
| BraidedRiver | 30.4 | 34.6 |
| WalkingCanopy | 25.3 | 26.9 |
| Driftreed | 27.1 | 34.7 |
| DrownedWaystation | 30.0 | 34.3 |
| FrozenHost | 20.6 | 30.6 |

The audit was run at 15, 20, 25, 30, 35 and 39 degrees (39 is past the agent limit but within the capsule's 45).

- **Committed walk** (`audit-committed-range.txt`): 36 of 36 open, from 15 degrees up. Taren at 30: swing knee 61.9, knee 1 cm under the hip, hips 76% of flat, 17.4 cm bob. Sela Shaped at 39: 50.8, 13 cm above the hip, 63%, 22.5 cm.
- **Upright climb** (`audit-after-range.txt`): `UPHILL_STEP_AUDIT PASS`, 36 of 36. Swing knee 90.2 to 100.1 degrees and 14.8 to 29.2 cm under the hip; lowest hip 98.4 to 104.7% of flat; bob 4.8 to 9.8 cm (the flat walk's own is 6.7 to 9.5).

Stills: `stills-committed/` (before, no ground line) and `stills-final4/` (8 frames per step from the side, ground line drawn; `sheet-*.png` are contact sheets).

## Tried and reverted (November 4, tilted frame)

| Variant | Effect | Broke |
|---|---|---|
| Earlier trailing-toe release, a 0.5 shift, a lower drop cap | knee 73-76 degrees, bob 9 cm | flat-walk knee snap (1,205 deg/s), knee-plane flip, 7 cm planted-sole slide |
| Same, flat drop kept | knee 69-72, hips 81% | knee-plane flip (Sela shaped, 6.12 m/s), 5.4 cm slide |
| Heel rises through late stance | knee 62-67 | nothing; but worse than committed |
| Longer release, lower cap | knee 70-73 | flip, 9 cm slide |
| Shift 0.5 only | knee 69-72 | flip |

November 12, upright, also rejected: a forefoot landing (more support drop, 18 cm bob), the hips set back with a forward lean (24 cm bob), a stance path that kept following the ground behind with only planted legs lowering the hips (knee snaps), and an 80 degree heel limit (knee linear speed 6.4 m/s on a fast diagonal).

## Running uphill (November 12, second pass)

`UphillStepAudit.Run -uphill-run` puts the civil jog and sprint (Run take, 4.5 and 6.6 m/s) and the combat run and sprint (6.7, 10.385) up the same slopes, each beside the same body's flat run (`run-audit-before.txt`, `run-audit-after.txt`; the walk's bounds don't apply, a running swing knee folds to about 50 degrees on the flat).

- **Tilted frame (before):** running folded harder than walking. From 20 degrees the 6.6 to 10.4 m/s runs sank to 60 to 80% of their flat hip height, and the stepping knee rose up to 38 cm above the hip (on the flat it stays 5 to 19 cm below). At 39 degrees the combat run crumpled, knees at the chest (`stills-run-before/sheet-TarenShaped-Run6.7-39.png`).
- **Fully upright** (as the walk): hips 94 to 102% of the flat run, torso pitch the flat run's own, but nine 39 degree knee-continuity checks failed (3,000 to 4,000 deg/s, and the backpedalling diagonals' knee planes flipped). At 6.7 m/s up 39 degrees the hips rise 5.4 m/s over a planted foot, and the stance knee straightened from 103 to 151 degrees in 35 ms. No runner climbs that fast.
- **Upright as fast as the legs can lift (after):** a climbing run stands upright only as far as its hips rise at 2.5 m/s or less (`ClimbLift`; the fastest climbing walk rises 2.0); faster than that its stride leans back toward the slope's frame, and a backpedal up a slope keeps the slope's frame. Up to about 20 degrees at a jog it is fully upright. At 39 degrees the runs hold 88 to 95% of their flat hip height (60 to 67% before) and the sprints 78 to 80% (62 to 67%); the stepping knee rises 21 to 31 cm above the hip (33 to 38 before). At 240 Hz the stance knee peaks at 1,817 to 2,633 deg/s and moves at 9 to 15 m/s, inside the run's 3,000 and 15 and the sprint's 18 (`stills-run-final/`).

## The recovering foot

Behind the hips a climbing foot rides the level path, 26 to 34 cm over a 39 degree slope early in its swing when walking. Following half the slope's fall behind brought it to 18 to 24 cm, but the hips had to come down to reach it: the walk audit went from 36 of 36 to 29 open (bob to 19 cm, swing knee to 77 degrees at 39). Left on the level path.

## The speed-change check

`LegContinuityTests.UphillWalkSpeedChangesKeepKneesContinuous` swept the walk to a literal 2.6 m/s, a walk the game can't play (above `GeneratedAnimator.RunAbove`, 2.5, it runs). The sweep now tops out at that constant, at 39 degrees as before, and passes: the 1200.084 deg/s was the 2.6 m/s walk, outside the speeds the game uses, not a fault in the pose.

