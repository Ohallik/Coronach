# C4 long circuit repeat — September 27

**C4 OPEN.** The release player contains `a759b1d` (staged forms and recoverable disabled hulls). These are continuous agent-directed virtual gamepad captures, with live AI, starter equipment and unchanged damage. Pending C5 source changes are absent from the player. No continuous video viewing, sound audition or physical Logitech feel is claimed.

| Run under `Builds/quality/C4/` | Seconds | Focused / samples | Result |
|---|---:|---:|---|
| `civil-hulls-taren-03` | 310.319 | 5557 / 18593 | REJECTED. Focus loss interrupts traversal; navigation and docking subsequently fail. |
| `civil-hulls-sela-03` | 310.858 | 18649 / 18649 | PASSED route checks. Complete hull perimeter, figure-eight, wall contact, back-away, prompt reapproach and ordinary docking. Both heroes finish at 120 HP inside the Decks. |
| `gullet-circuit-taren-03` | 336.127 | 3547 / 20149 | REJECTED. Extended focus loss interrupts the first chamber's mine route; subsequent traversal, second clear and final hero check fail. |
| `gullet-circuit-sela-03` | 336.608 | 20197 / 20197 | PASSED route checks. Both live chambers clear; handling, wall contact and swaps complete. Sela finishes at 20.4 HP and Taren at 86.8 HP. |
| `tallow-hulls-02` | 178.200 | 10688 / 10688 | REJECTED by one fixture error: the new pre-dock thrust step inherits interior `TallowDrift` while correctly outside in `TallowApproach`. All navigation, dialogue, launch, dock and return checks pass; both heroes finish at 120 HP. |

The Tallow correction explicitly expects `TallowApproach` on that pre-dock step. Its full repeat remains pending; this rejected run is retained unchanged. The committed routes retain the same scene checks, goals, tolerances, combat conditions and focus requirements.

Actual opened stills: Sela civil `000,003,004,005,007,029,033`; Taren Gullet `025,086`; Sela Gullet `027,064,081,088`; Tallow `044,045,047`. The ships remain visibly separated in these frames. The Tallow sequence shows approach clearance, a real dock prompt and the returned Natural party inside the pressure vestibule. Sela's final low integrity is preserved; successful traversal is not a balance or comfort acceptance.

The civil perimeter exposes severely stretched, ragged ring panels. **The paired first-station final MAP_EYE_TEST is reopened**, superseding the earlier exterior acceptance. Interior circulation evidence remains valid, but the ring needs a controlled construction correction and ordinary recapture. The Gullet's repetitive tubular layout remains rejected. Static frames do not establish smooth motion.

The read-only focus evidence identifies unrelated Frostbound Unity PID 3664 during Taren civil and PID 36360 during Taren Gullet. Another Unity PID 45084 exited before its project was identified. No external application was stopped, altered or forced out of focus. The [companion report](C4-long-circuits03.json) retains exact focus intervals, failures, build/content hashes, 16 opened still hashes and a frozen log snapshot. Captured timing is not clean C1 evidence.

Normal autosave and backup hashes still match the preserved originals. The already recorded full 50/50 EditMode and 137/137 PlayMode results apply to this C4 runtime, not the pending audio source changes. C4's final long Taren repeats, enemy failure presentation, remaining maps and continuous observation remain open.
