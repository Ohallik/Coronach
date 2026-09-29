# Flight practice area - workshop brief

2026-09-26. Source inspection by Codex. **Blockout PASS (Claude Code agent review, 2026-09-29); final built with generated art, in-player pass OPEN.** Evidence: [validation/MAP-Arenas.md](../validation/MAP-Arenas.md). Included because the scene is packaged with the workshop practice/diagnostic flow.

The current scene reuses the 40 by 40 m ground square, scattered rock pieces and Hal's standing NPC, adding two Gullet wall rows at x=+/-17. A terrestrial guide standing beside a flight plane inside an otherwise unexplained membrane square is an unresolved contradiction.

```text
Current plan
 x=-17 wall    three flight enemies z=5     wall x=17
                 crystal
             Arrival (0,-4)    standing Hal
          open ends / square textured base
```

The replacement should read as a dockside ship proving berth: visible perimeter beams, a clear entry/exit lane, supported observation/control cabin and distant backdrop under the flight plane. Hal communicates from the protected cabin, with the interaction point accessible at the berth. Place target drones and clearance obstacles deliberately; mark the collision boundary through actual structure. Do not camouflage the existing contradiction with more floating rocks.

The blockout needs a section locating the control cabin, flight plane and berth structure, plus an overhead showing approach and turning clearance. Final ordinary-input evidence must show boosting, braking, roll, lunge and both end boundaries with the guide safely outside combat traffic. Preserve Arrival and arena combat behavior; a believable proving berth still has to support the existing motion tests. No map pass is claimed yet.


### Actual baseline image review

Codex, September 26: Opened the arrival: a standing humanoid and desert boulders share a flat organic floor between repeated ribs. There is no coherent dock, control cabin or flight enclosure. This explicitly fails exterior/interior and functional logic. Evidence: `Builds/quality/workshop/maps-before-01/`. No blockout/final acceptance is claimed.

### Redesign plan and build (September 29, Claude Code)

`ArenaRedesign.FlightBlockout` / `.FlightFinal` (isolated). It keeps `Arrival` at (0, 1, −4), the three Chorister targets at z = 5 and Hal's `ArenaGuide` node, and leaves the core clear at the flight plane. The old membrane walls at x = ±17 and the organic floor are removed.

```text
Section (looking north)                         Plan (north up)
  y 4   post tops                    rail z 28 ─────────────────────────────
  y 1   rails = flight-plane boundary      post ║                        ║ post
        ships fly at y 1               x -24  ║   targets z 5           ║ x 24
  y -1..5  control cabin on posts      CABIN  ║   Arrival z -4          ║
  y -12  lower dock deck               (x -33)║ call pad x -20          ║
        Vorun far below                  rail z -26 ════╗   lane   ╔═══════
                                                        ║ x ±6     ║
                                                        ╚══ z -40 ══╝ lane end
```

Posts rise from the lower deck to above the rails. The rails at the flight plane are the collision boundary on every side, including the railed entry lane and its closed end. The observation and control cabin, a generated docking pod, sits on posts outside the west rail. Hal speaks from it: his call point is a lit pad at the berth edge, with his standing body hidden, so no terrestrial figure stands on the flight plane.
