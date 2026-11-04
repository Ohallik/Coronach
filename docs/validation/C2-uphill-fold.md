# Uphill fold (Sela's "high stepping") - November 4

Status: **diagnosed, not fixed.** The committed walk is unchanged.

## What it is

`Lattice.EditorTools.UphillStepAudit.Run` walks each hero and form up 30 and 39 degree floors at the recorded 1.47 m/s through the real `GroundFeet`. Its bounds reject the recorded live fold (Sela, `C2-evaluated-pose.md`): a 60.88 degree knee level with the hip, hips at 60% of their flat height, a 19 cm bob.

On the committed walk (`Builds/quality/C2/step-height/uphill-step-audit-committed.txt`) all 8 cases are open, and **both heroes fold, not only Sela.** Sela's shaped form at 30 degrees shows a 61.4 degree stepping knee 2 cm above the hip, hips at 74% of flat, and a 17.8 cm bob. At 39 degrees the stepping knee reaches 51 degrees, 13 cm above the hip.

## Why

The worst frame is always late swing (leg phase 0.84-0.93), with no clearance lift. The foot is set on the slope only 0.40-0.50 m below a hip that the uphill balance shift has carried over it.

`GroundFeet` poses the whole leg chain in the slope frame. That tilts the body back and lowers the hips 13%. It then shifts the hips 65% of the way back upright after the foot targets are fixed. To keep the trailing toe planted, the support drop lowers the hips by up to 0.36 m more, with about 20 ms of smoothing. That drop is the pump.

## Tried and reverted

Five bounded variants were tried; logs are in `Builds/quality/C2/step-height/`. None passes the audit without breaking an existing check:

| Variant | Effect | Broke |
|---|---|---|
| Earlier trailing-toe release, a 0.5 shift, a lower drop cap | knee 73-76 degrees, bob 9 cm | flat-walk knee snap (1,205 deg/s), knee-plane flip, 7 cm planted-sole slide |
| Same, flat drop kept | knee 69-72, hips 81% | knee-plane flip (Sela shaped, 6.12 m/s), 5.4 cm slide |
| Heel rises through late stance | knee 62-67 | nothing; but worse than committed |
| Longer release, lower cap | knee 70-73 | flip, 9 cm slide |
| Shift 0.5 only | knee 69-72 | flip |

## Next

Re-pose uphill walking in the gravity frame: an upright torso, slope-aware foot targets, and a trailing heel that lifts at toe-off. Do not retune parameters. The audit is the acceptance check. Continuous motion review still needs a person.
