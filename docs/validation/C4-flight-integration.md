# C4 flight integration checkpoint

September 26, Codex. **C4 OPEN.** Matched timestep tests reproduce and correct a braking-distance defect. These are numerical and headless runtime checks, not observed 30/60/120 fps player sessions or physical-controller acceptance.

The old update adds thrust, damps the entire result, then travels using only the final velocity. That drops too much braking travel on longer frames. An unchanged-behavior extraction of the actual runtime movement calculation exposes this failure:

| Matched timestep | Old two-second brake from 12 m/s | Corrected |
|---|---:|---:|
| 30 fps | 1.143318 m | 1.333958 m |
| 60 fps | 1.235833 m | 1.333958 m |
| 120 fps | 1.283958 m | 1.333957 m |

The old spread is **10.54796%**, failing the production plan's 5% bound. Continuous exponential braking predicts 1.333333 m. The correction integrates constant thrust with drag, bounds speed-cap/turn calculation steps to at most 1/120 second, and accumulates average-velocity displacement. No tuning constants, cruise/boost caps or braking coefficient change. At 60 fps the corrected braking path is about 9.8 cm longer; this is an intentional correction to the shortened numerical path, not an accepted feel judgment. Zero elapsed time preserves momentum, including velocity above the ordinary cruise cap after boost.

The same six-second sequence of thrust, right turn, boosted reversal, boosted left turn, coast and brake agrees within 0.000011 m at its endpoint. Total traveled distance agrees within 5%. Unpowered coast also matches its continuous drag curve. The runtime motor uses the tested displacement function.

Evidence under `Builds/quality/C4/`:

- `integration-red`: **2/3 EditMode pass, 1 fails**, 0.0274707 s. Exact pre-fix runtime/test source retained.
- `integration-green`: **3/3 pass**, 0.0377244 s, unchanged original assertions.
- `integration-extended`: full **44/44 EditMode**, zero skips, 0.5360596 s; targeted **5/5 PlayMode**, zero skips, 36.637631 s. Runtime cases cover ship forms, firing/lunge, partner boost/catch-up, combat-only wall damage and recoverable disabled flight.

Saves retain their original hashes in the companion manifest. This is a source checkpoint: Development still contains Sorrel blockout 06 before this flight correction; Release remains the preceding C3 integrated build. Full PlayMode and rebuilt player pair are pending after further C4 work. Nose/aim/ship-socket review, full civil/combat circuits, lunge contact, transformation continuity, normal/slow video observation, sound audition and physical Logitech feel remain OPEN or UNVERIFIED. No moving footage or audio was observed for this numerical checkpoint.
