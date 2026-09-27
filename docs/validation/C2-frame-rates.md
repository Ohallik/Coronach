# C2 frame-rate follow-up

2026-09-26. C2 remains OPEN. The earlier 60 fps Natural/Shaped routes in `C2-player04.json` pass measured free locomotion. This follow-up uses the player packaged with `ec0ac23` (the subsequent `7967f1c` is a spatial-review documentation commit). Raw recordings include ordinary virtual gamepad input, independent bone directions, calibrated sole markers and continuous video/audio. Rendered-mesh sole regressions remain separate from those calibrated markers.

| Recording under Builds/quality/C2 | Route/motion | Measured median interval | Contacts | Worst planted drift |
|---|---|---|---|---|
| player-natural-30-01 | PASS, 6,695 focused samples / 223.459 s | 33.3316 ms | 133 | 0.003260 m |
| player-shaped-30-01 | PASS, 7,011 focused samples / 233.748 s | 33.3317 ms | 147 | 0.000010 m |
| player-natural-120-01 | PASS, 26,540 focused samples / 221.290 s | 8.3352 ms | 151 | 0.000014 m |
| player-shaped-120-01 | PASS, 27,790 focused samples / 231.739 s | 8.3356 ms | 236 | 0.000010 m |

The same 10-degree steady pelvis, 15-degree whole-cycle torso, 0.05 m contact-drift and 0.03 m floor-penetration bounds apply. The Natural 30 fps minimum sole clearance is 0.005999 m. Opened checkpoint images 027/067/116/156 cover both heroes' reverse/stride poses; checkpoint stills can include the start of the following transition and do not establish continuous-motion quality. The exact 30 fps run includes all eight directions, near-zero starts, stops/reversals, faster travel, companion proximity and swapping.

The Shaped 30 fps minimum sole clearance is 0.005999 m; opened 036/071/124/159 show both generated combat bodies at ordinary/run/sprint transition boundaries. Natural 120 fps also has 0.005999 m minimum clearance; opened 027/068/116/157 show both heroes aligned with their measured travel and stepping poses. Foreground hatch frames partially occlude the companion in 068/157, an existing boundary-view limitation preserved in the station review.

Shaped 120 fps passes with 0.005998 m minimum clearance. Opened 036/071/124/159 show separated legs in the running poses and retained vane attachments on both generated bodies. These images also retain Sorrel's sparse, repetitive terrain; its spatial review remains open.

[Exact artifact hashes](C2-player05.json) link these four recordings, the earlier 60 fps evidence and the integrated build/test manifest. All four new recordings retain focus throughout. Normal save and backup hashes remain unchanged. No production code or analyzer thresholds changed for this follow-up.

Normal/slow video observation, audio audition and physical-controller feel remain UNVERIFIED. A separate Frostbound player is active; captured motion evidence is not clean C1 timing. This completes the measured free-travel frame-rate matrix; it does not close all lock-on, form-transition or watched-motion requirements.
