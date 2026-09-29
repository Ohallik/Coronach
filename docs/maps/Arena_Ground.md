# Ground practice area - workshop brief

2026-09-26. Source inspection by Codex. **Blockout PASS (Claude Code agent review, 2026-09-29); final built with generated art, in-player pass OPEN.** Evidence: [validation/MAP-Arenas.md](../validation/MAP-Arenas.md). This scene ships with the workshop's diagnostic/practice flow and is included in the map review.

The current scene is a 40 by 40 m square floor with eight scattered rock pieces, a crystal, Hal standing beside the arrival, and three training enemy spawns. It has no established enclosure, entrance/service route or visible explanation for the practice activity. Technical combat tests do not accept that composition.

```text
Current plan (north up)
     three opponent spawns at z=5
          crystal (-4,3)
  side rocks       open square       side rocks
        Arrival (0,-4)    Hal (7,-4)
```

Rebuild as an outpost maintenance proving yard: a bounded usable test surface, a clear southern service/arrival entrance, sheltered operator/repair space beside it, and a northern obstacle/target area. Rock outcrops may form one edge; equipment and barriers must be visibly installed or supported. Keep enough open ground for eight-direction motion and meaningful attack tests without placing civilians inside the firing path. Generated workshop/environment pieces can supply the visible structure; no new paid asset is assumed.

Open arrival, operator sightline, all boundaries and an overhead before accepting the blockout. The ordinary run must include the service edge and opposing corners. Preserve Arrival and the arena runtime's test contracts. Final art, motion and collision are separate checks, all still pending.


### Actual baseline image review

Codex, September 26: Opened the overhead: scattered boulders on an exposed square of sand with no visible boundary, arrival or reason for its arrangement. This is a test arena, not an accepted inhabited or naturally formed release place. Evidence: `Builds/quality/workshop/maps-before-01/`. No blockout/final acceptance is claimed.

### Redesign plan and build (September 29, Claude Code)

`ArenaRedesign.GroundBlockout` / `.GroundFinal` (isolated). The yard keeps the old square's 40 × 40 m graded surface and height. It keeps `Arrival` at (0, 0, −4), the three opponent spawns at z = 5 and Hal's `ArenaGuide` node. It also keeps the working core the tests rely on (|x| < 10, −18 < z < 10) clear: the old rocks began at |x| ≈ 11.

```text
Proving yard, north up (metres)
  z 24      rock backstop (ridge strata the yard was graded against)
  z 11-17   obstacle/target area: crate stacks, low cover ridge, rubble, drill bit
  z 5       three opponent posts
  x -23     west rock outcrop          |  x 21  installed deck-panel barrier (east)
  z -4      Arrival
  z -9..-19 operator's sheltered repair pad (SE corner): hab, console, bench,
            crate fence; Hal at its open corner (11.6, -9.2), out of the firing path
  z -20.6   low crate barrier (camera side) either side of the service entrance
  z -31     service lane out to the landing pad and parked skiff
```

Collision follows visible edges: boundary boxes sit on the outcrop, backstop, panel and crate lines, the lane edges and the apron end. The worn outpost apron (110 m) continues around the yard, so it is not a floating tray, and Vorun hangs in the sky.
