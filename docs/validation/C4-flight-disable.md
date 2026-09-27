# C4 disabled craft — September 27

**C4 OPEN. Focused runtime and controlled still checks pass; integrated player review remains pending.** A disabled craft keeps its stationary hull collision while disabling its damage trigger. Rescuers can approach within the existing assistance range without passing through its generated body. Ground and enemy corpse collision behavior is retained.

Failure presentation now gives a brief opposite roll impulse, a settling pitch/bank, a small emergency wing fold and nozzle sparks. It keeps the actor on its flight plane. An amber ring marks the recoverable body; the ring turns cyan during nearby assistance/recovery and fades as the craft levels and reopens. Recovery starts from the current visible pose, including an early interruption. Pause holds the pose and signal. Landing a disabled partner removes the flight cue and solid ship footprint; returning to flight restores them. Repeated down/revive reuses the same signal and mesh copy. SFX integration remains C5 work.

| Raw evidence under `Builds/quality/C4/` | Result |
|---|---|
| `flight-disable-red01` | **0/3**. Craft centres overlap to 0.010608 m; early revival jumps 57.056°; rescue signal is absent. |
| `flight-disable-green01` | **9/10**. Hull/early-recovery correction passes, but pausing allows one more failure-pose update. |
| `flight-disable-boundary-red01` | **0/1**. A landed disabled partner retains the floating rescue ring. |
| `flight-disable-green02` | **16/16**, zero skips: four new cases, five existing hull cases, the existing flight-defeat lifecycle and six staged-form checks. |

The pause correction retains the last applied pose, rather than evaluating the newly frozen clock once more. Disabled body selection now resolves immediately to the zone's form without starting an active transformation. Boundary handling explicitly releases/restores the ship signal and collision. The repeat-down fixture waits beyond the existing revive protection before issuing its second lethal packet; protection is unchanged.

The agent opened **12 actual renders** from the 36-image `flight-disable-poses01` capture: each hero's initial impulse front, mid-failure side, settled front/top, mid-recovery top and restored top. These show intact hulls, a readable surrounding ring, different failure/restoration attitudes and clean signal removal. They establish candidate geometry/cue visibility in controlled views, not continuous failure motion or gameplay-distance acceptance. Exact opened files, raw tests and source hashes are retained in the companion JSON.

Both original save hashes are unchanged. Both packages still contain `feac4b9`; full suites, rebuilds and ordinary recapture follow. Long circuit acceptance, outstanding MAP_EYE_TEST work and later gates remain open. Continuous motion/audio observation and physical feel remain UNVERIFIED.
