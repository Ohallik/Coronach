# MAP_EYE_TEST Gullet blockout — September 28

**Blockout PASSED (Claude Code agent review); final in-player pass OPEN.** The layout and review are in the [Gullet brief](../maps/Gullet_Tunnel.md). The redesign is built by `GulletBuilder.Rebuild`, an isolated entry that rebuilds only this scene and never runs the shared world preparation, from `Lattice.World.GulletProfile`.

## Evidence

| Run | Result |
|---|---|
| `workshop/gullet-before-02` | The old scene through the new review camera set. Rejected: uniform sine-wave strip, abrupt open end, identical chambers. |
| `workshop/gullet-blockout-01` | First build. Opened views exposed membranes poking out of the tube and pods reading as scattered buds. Rejected in part; both corrected. |
| `workshop/gullet-blockout-02` | Membranes fit the pinch; egg clusters and section tints readable. The coil clamps faced edge-on. |
| `workshop/gullet-blockout-03` | Current. The full-length overhead and all ten section views were opened; see the brief for what each shows and what remains weak. |
| Build-time wall check | `GULLET_WALLS_OK samples=714`: both walls are present along the whole travel line at flight height. |
| `workshop/gullet-layout-red01` | **2/2 new layout checks rejected on the old scene**: the travel line struck the old shell and ribs, and membranes sat 47 m from any valve. |
| `workshop/gullet-layout-green01` | **28/28**: layout checks, core play tests (all zones load, including the Gullet) and music cues. |
| `workshop/full-integration-gullet01` | **56/56 EditMode, 166/166 PlayMode**, zero skips. |

## Ordinary flight review: blocked

`tools/gullet_routes.py` regenerates both heroes' combat circuits on the new anatomy, with the same tactics, equipment, AI, damage, tolerances and focus rule as circuit03. Two Taren attempts (`C4/gullet-circuit-taren-05`, `-06`) were rejected because the player window lost focus 4–19 s in and kept losing it. During the second, the foreground belonged to a Frostbound batch Unity process launched by another session. Nothing in this session ran concurrently, and the focus requirement was not relaxed. The first attempt also showed that two navigation legs were too short for braked flight (about 2 m/s); those timings were corrected before the second. The final pass waits for a quiet machine.

The scripted slice smoke (`smoketest.ps1 -Dev -Route slice`) stalls before the Gullet, at the Cinder Office dock pad. It has no recorded pass since the September 26 station redesign and the September 27 hull-sized ship collision, where the pad's solid box keeps the hull sphere just outside the 4 m interaction range. It is recorded as a C7 repair; it is not caused by the Gullet.
