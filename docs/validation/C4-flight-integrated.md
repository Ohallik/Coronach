# C4 flight and form integration checkpoint — September 27

**C4 remains OPEN.** This checkpoint integrates the frame-rate correction, collision-limited dash contact and interrupted-form continuity. It does not accept the complete flight presentation or replace the required long civil/Gullet circuits.

Repeated form requests previously snapped the shrinking body from roughly 0.436 to 0.992 scale. Reversing before the body swap collapsed that same body again, and Civil/Combat flight restarted a transition despite sharing one hull. Three of the first four regressions failed. The corrected controller coalesces requests for the same incoming body, resizes from the current scale, reverses by growing the still-visible body, and updates flight rules without resetting its live bank/roll. A later test separately reproduced retargeting an incoming Flight hull to CivilFlight extending the original transition; retaining the mutable request fixes that case. Both heroes retain exactly one visible body through rapid requests. Pause holds the current scale and resumes the same transition.

| Evidence directory under `Builds/quality/C4` | Result |
|---|---|
| `form-continuity-red` | 1/4 passed, three reproduced failures |
| `form-continuity-green` | 4/4 passed |
| `form-retarget-red` | 5/6 passed, incoming-hull restart reproduced |
| `form-retarget-green` | 6/6 passed, 25.0268752 s |
| `c4-integrated-02` full PlayMode | 104/104 passed, zero skips, 718.464794 s |
| `c4-integrated-02` full EditMode | 44/44 passed, zero skips, 0.3893922 s |

The earlier `contact-integrated-01` full run remains a 97/98 rejection. Its fortune-contaminated Overdrive fixture and correction are documented in [C4-flight-contact.md](C4-flight-contact.md). The new full pass includes that correction, rather than combining targeted results to imply a full green run. [Flight integration](C4-flight-integration.md) retains the original 30/60/120 stopping-distance failure; [contact evidence](C4-flight-contact.md) retains stationary, ahead-of-travel and covered-hit failures.

Both `Builds/WindowsDev/Coronach.exe` and `Builds/Windows/Coronach.exe` rebuilt with `BUILD_OK`; their logs are retained alongside the full suite results. The human `autosave.json` and backup restored to their original SHA-256 values after the full suite: `65b2ca12c171d0167a62b443988b7b66b0ac737a0ce11537a2197ab4278de47a` and `a9b131c2c9bd9ca03f1d69adfaf551a28f82be812f96f4146f26499d64e20745`.

Ordinary release evidence uses continuous virtual gamepad input with the normal starter party and enemies. Arena runs pass 24/26 maneuver checkpoints and retain focus (2,902/2,922 samples), but **fail the additional lunge-contact requirement**: companion fire kills targets before actual dash contact. Those runs are preserved. Moving the contact route to the normal Gullet arrival/encounter gives Taren four logged lunge kills in 39.857 s (25 checks; 2,393 focused samples) and Sela five in 40.356 s (27 checks; 2,423 focused samples). Both parties remain alive. No enemy, damage, difficulty or companion suppression was used. These short routes do not complete the chamber or the required five-minute circuit.

The first Sorrel boundary route fails two form expectations: its start point was already outside the pivot-based safe bounds, and high-speed short reversals never returned inside. The corrected route slows input and resets just inside the real boundary. It passes all 35 checks in 43.056 s with 2,585/2,585 focused samples. Repeated civil/combat music changes establish actual zone crossings; both heroes finish Natural at full health. The targeted tests establish scale continuity; the replay has no per-frame scale metric.

Codex opened actual stills from both arena maneuvers, both Gullet contact runs, the rejected boundary run and the corrected transitions/return. Contact images show nearby enemy bodies and 50-point hits. The Gullet regroup frames visibly intersect the two ship hulls: **close-range hull collision/spacing is rejected for further correction**. Boundary frames show one visible body and its transition effect, then full Natural bodies back in the court. This is still-image inspection only. Exact opened paths, raw artifact hashes, full results and package fingerprints are in [C4-flight-integrated.json](C4-flight-integrated.json).

The transition is still the existing shrink/swap/grow presentation. Authored attached-panel folding, geometry-aligned weapon/engine attachment, remaining flight skill contact and flight defeat presentation require further work. Normal/slow video observation, sound audition and physical Logitech feel remain **UNVERIFIED**. Sorrel in these players is the explicit blockout candidate; its final-art and complete MAP_EYE_TEST are open. First-station spatial acceptance remains separate from C1 performance and workshop acceptance.
