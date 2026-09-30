# C4 — ordinary Gullet circuits on the redesigned anatomy, and the slice smoke

2026-09-30, Claude Code. The other session's Frostbound player tests had finished. Every capture ran alone, as one blocking call, with nothing else issued during it. Release and development players were rebuilt from the integrated source before each set.

**Both ordinary Gullet circuits PASS, with full focus. The scripted slice smoke PASSES end to end for the first time since the September 26 station redesign.**

## Results

| Run | Result | Notes |
|---|---|---|
| `C4/gullet-circuit-taren-07` | Rejected | Focus held throughout. Both chambers cleared, then the party died in the salvage eddy after repeated 11.5-point hits (a 12-damage wall contact after plating) |
| `C4/gullet-circuit-taren-08` | Rejected | Same run with wall-contact logging. Wall ribs stood inside the eddy's mouth (`GulletWallA/B` struck at x ≈ 10–13 where the edge is at 14–17), and crates lay among the mines |
| `C4/gullet-circuit-taren-09` | Rejected | After the rib and debris fix, Taren finished at 138/138 and the partner at 25. One rib still intruded at a bend, and one weave leg's time budget was short |
| `C4/gullet-circuit-taren-10` | **PASS** | 393.6 s; 23,614/23,614 frames focused; Taren 138.4, partner 87.5 |
| `C4/gullet-circuit-sela-04` | Rejected | Played well (both about 111–115), but the route's leader check failed: my generator had dropped circuit03's opening "select Sela" swap |
| `C4/gullet-circuit-sela-05` | Rejected | The AI partner sat under Drifter fire in chamber 0 and went down; Sela then fell to the Drifters at chamber 1 on low health |
| `C4/gullet-circuit-sela-06` | **PASS** | After partner shot evasion: 393.7 s; 23,621/23,621 focused; Sela 110.9 (never below 92.9), partner 97.3 throughout |
| `C4/gullet-circuit-taren-11` | **PASS** | Same build as sela-06: 393.6 s; 23,614/23,614 focused; Taren 110.9, partner 114.9 (never below 102) |
| `route-slice` smoke | **PASS** | `ROUTE_MARKER_CONTRACT_OK`, `SLICE_SMOKE_OK elapsed=522.4 level=6` |

## Defects found and fixed

- **Gullet wall ribs stood in the passage.**
  - Each rib slab was centred on the profile edge and fitted by height, so its inner half stood about 2 m inside the passage along the whole Gullet, and more at the eddy's flare.
  - The existing layout test only swept the central travel line, which never enters the eddy.
  - New test: `WallRibsStayInTheWallAcrossTheWholePassage` samples the whole passage on a 0.75 m × 1 m grid with a 0.4 m probe. It was red on the old scene and red again on the first fix, which pushed ribs out by their centre's edge. A straight rib on a bend meets a different edge at its ends.
  - Fix: `GulletBuilder` checks a 9 × 9 grid across each rib's collider against the edge at each point, then pushes the rib out by the worst intrusion. The shell's own collider is the wall. Opened review renders show the ribs as a rib cage outside the membrane shell.
- **Debris among the mines.** The eddy's wreck and crates lay inside the mine cluster's fighting space. New test `TheEddyDebrisLiesClearOfItsMines` was red on the old scene; the debris now rests at the back of the pocket.
- **The flight partner never evaded fire.** The AI partner dodged only melee telegraphs and bosses, and chased spitters to 2.2 m under their three-shot spread. `Projectile.Live` now tracks shots in the air. `PartnerBrain` rolls, or sidesteps on the ground, across any hostile shot due to hit within 0.35 s. New tests `PartnerEvasionTests` (flight and ground) were red before, green after. The circuits show the effect: the partner went from being downed to finishing at 97–115 health.
- **Flight docks were unreachable head-on.** A hull stops against a solid 5 m pad at 4.0–4.3 m from its centre face-on, and at about 5.3 m at a corner, but the prompt range was 4 m. Taren could not dock at the Repair Bay from the south. New test `DockReachTests` sweeps a hull-sized sphere at every open side of every Cinder and Tallow flight dock; it was red before. Flight docks now use `WorldBuilder.FlightDockRange` (6.5 m); ground pads keep 4 m.
- **Route generator.** Sela's circuit regained circuit03's opening swap to Sela. Weave legs are now budgeted by their real length at braked navigation's about 2 m/s (minimum 12 s). Tolerances, tactics, equipment, flags and the focus rule are unchanged.

## Slice smoke repairs

The smoke's flight travel is a straight line and its combat is simple. Each repair makes it do what a player does, without loosening any marker or contract:

- It approaches a flight dock to just inside the prompt's own range.
- It passes south of the repair hull on the way to the moon approach.
- It walks Sorrel's redesigned haul road by its actual bends.
- It walks to a reachable stance within reach of a solid interactable, such as the key's anvil.
- It shoots mines from 7 m instead of closing to 5 m.
- It holds flight skills until a lunge has landed a kill, so the `LUNGE_KILL` marker is proven rather than left to chance.

Every wall contact now logs one line (`WALL_CONTACT`, at most every 0.6 s), which is how the rib defect was found.

## Still open

- The ordinary circuits cover the mouth through the salvage eddy, with two chambers. The nursery gate, the Cantor and the exit were traversed only by the scripted smoke, not by ordinary input.
- Continuous-video review by a person, listening and physical feel remain UNVERIFIED.
