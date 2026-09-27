# C4 long ordinary circuits — September 27

**C4 OPEN. All five captures below are rejected.** They use the release player built from `feac4b9`, legal starter equipment, live AI and unchanged damage. These are continuous agent-directed virtual gamepad runs. Recording exists; continuous video/audio observation and physical controller feel remain UNVERIFIED.

| Capture under `Builds/quality/C4/` | Seconds | Focused / samples | Result |
|---|---:|---:|---|
| `civil-hulls-taren-02` | 309.186 | 18178 / 18550 | Circuit navigation passes; backing away removes the docking prompt before confirm. Dock/arrival and focus checks reject. |
| `civil-hulls-sela-02` | 309.674 | 18581 / 18581 | Circuit navigation and focus pass; same out-of-range docking input rejects arrival. |
| `gullet-circuit-taren-02` | 337.716 | 18536 / 20254 | Both encounter chambers clear and both heroes survive. Early focus loss leaves the craft short of its first approach and subsequent mine-route goals. |
| `gullet-circuit-sela-02` | 338.256 | 18567 / 20283 | Both chambers clear. Repeated second-chamber lunges end near the wall with 3.1 HP. A later wall strike downs Sela, automatically swaps to Taren, then proximity revives Sela. Navigation, hero identity and focus checks reject. |
| `tallow-hulls-01` | 177.527 | 10648 / 10648 | Actual docking and Natural arrival succeed; approach goal lies inside the wider hull clearance. Navigation check correctly rejects. Both heroes retain 120 HP. |

The Tallow approach stabilizes at Z -3.474 while its target was -2.5; the normal dock prompt is already visible. Its replacement goal is -3.9 followed by a short ordinary approach. The civil repeat keeps its wall-contact/back-away checks and adds a return to prompt range before confirm. The Gullet repeat retains both live chambers and handling checks, but brakes after six second-chamber lunges and uses an ordinary area skill plus sustained fire instead of four additional blind lunges into the wall. These pending routes do not change health, enemy settings, equipment, tolerances or focus requirements.

Actual opened stills: Taren Gullet `065.png` and `091.png`; Sela Gullet `086.png` and `088.png`; Tallow `044.png` and `046.png`. The Taren frames show separated craft and attached exhaust. Sela's down-state frame shows the restoring HUD and **overlapping live/disabled hulls**, a presentation defect still to fix; the later frame shows both craft alive and separated. Tallow's frames show the real dock prompt outside the pad collision and the returned party inside the pressure vestibule. The Gullet remains a repetitive tube and does not pass MAP_EYE_TEST.

The read-only `focus-owners01.csv` records a Unity window taking foreground from Sela's player at 09:40:46 UTC and the player returning at 09:41:15. The Unity process had exited before its project could be identified; this observation is not attributed to a particular project. No external window was closed or process stopped. These captured runs are not clean C1 timing evidence.

The companion JSON preserves raw hashes, exact reports and final samples. Full per-step analysis remains in `Builds/quality/C4/circuits02-analysis.json`. Staged transformation work is a separate source candidate and was absent from these packages.
