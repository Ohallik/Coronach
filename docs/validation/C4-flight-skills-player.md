# C4 ordinary flight skill review

September 27, 2026. **C4 OPEN.** Both Windows players rebuilt successfully from `feac4b9`. Each release pilot completed the ordinary virtual-gamepad four-skill sequence in Gullet with full focus and a living party. All four `SKILL_OK` slots appear for the requested active hero. Starter equipment, enemy AI and damage remain unchanged.

| Capture under `Builds/quality/C4/` | Checkpoints | Duration | Focus | Minimum / final active HP | Final partner HP |
|---|---:|---:|---:|---:|---:|
| `flight-skills-taren-01` | 21 | 41.3885485 s | 2485/2485 | 66.0 / 70.3 | 120.0 |
| `flight-skills-sela-01` | 23 | 41.8891409 s | 2515/2515 | 30.0 / 48.0 | 120.0 |

Codex opened twelve actual PNGs, including unmodified frames extracted from the recorded video at the times retained in each `extracted-stills.json`. At gameplay scale, four amber Taren and three cyan Sela plumes now visibly connect to their nozzles. Cleave's forward gold arc and Pulse's complete gold edge read distinctly. Taren's hull cue and dash boundary remain narrow and visible. Sela's airborne seed and larger cyan net field are visible; her smaller Refract hull cue reads separately inside it. The exposed pilot loses substantial HP while testing skills; this result is retained rather than changing combat for a pass.

This accepts the stated still-image readability and ordinary input checks. These are short routes, not the required longer flight circuits or watched continuous-motion review. Both current civil repeats fail docking after their back-away step leaves interaction range; Taren also loses focus. Their rejection remains separate and a reapproach is prepared. Gullet's repetitive straight tube remains MAP_EYE_TEST rejected.

The preceding focused regressions are in [the source checkpoint](C4-flight-skills.md); full integrated PlayMode remains pending. Staged transformation, flight disable, long circuits, ring-edge cleanup and later production gates remain open. Normal/slow video viewing, actual captured sound audition and physical Logitech feel remain **UNVERIFIED**. Original human saves are unchanged. [Manifest](C4-flight-skills-player.json) records exact source, build logs, raw artifacts, opened images and save hashes.
