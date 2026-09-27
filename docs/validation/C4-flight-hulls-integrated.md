# C4 integrated hull collision review

September 27, 2026. **C4 OPEN.** Both Windows players now contain `abaf4be`, including measured hull collision, actual nose/nozzle sockets and the preceding Sorrel blockout 06. Full integration `Builds/quality/C4/c4-integrated-04` passes **118/118 PlayMode** in 768.9961687 s and **48/48 EditMode** in 0.4129794 s, zero skips. Both build markers pass. Original autosave and backup hashes are unchanged. This follows the preserved 117/118 failure and direct collision-witness correction in [the source checkpoint](C4-flight-hulls.md).

| Ordinary release capture | Result |
|---|---|
| `gullet-hulls-taren-01` | PASS: 25 checkpoints, 39.8645782 s, all 2,393 frames focused, eight actual lunge kills. Taren finishes at 85.1 HP, Sela at 138. |
| `gullet-hulls-sela-01` | PASS: 27 checkpoints, 40.3632519 s, all 2,423 frames focused, eight actual lunge kills. Sela finishes at 85.1 HP, Taren at 138. |
| `civil-hulls-taren-01` | REJECTED: 309.1565181 s, only 9,804 of 18,481 frames focused. Four outer-route waypoints are missed during lost input focus. Two dock approach goals also lie inside the new pad clearance. The ordinary dock interaction nevertheless reaches Hub_Decks/Natural with both heroes at 120 HP. |
| `civil-hulls-sela-01` | REJECTED: 309.6265496 s, 18,570 of 18,574 frames focused. Every outer-hull waypoint passes; two dock approach goals and focus fail. The ordinary dock interaction reaches Hub_Decks/Natural with both heroes at 120 HP. |

Codex opened the actual Gullet lunge/contact and regroup PNGs. The regroup images now show two separate hulls, correcting the previously recorded interpenetration. The wider damage body also exposes the active craft to more incoming fire than the historical narrow-capsule capture; the ordinary run preserves that result rather than claiming unchanged difficulty. These short routes do not establish the required five-minute encounters or a full chamber clear.

The same review **rejects exhaust readability at gameplay scale**. Unaltered frames extracted at five seconds from both captured videos show tiny, faint strands at the nozzle tips. Attachment/drive-state tests and enlarged controlled renders were insufficient to accept that presentation. The next correction will address the owned flare's padded cross-section and intensity, retaining the actual geometry and command-driven behavior. The lunge edge is also very fine at this distance. No normal-speed or slow-motion video observation is claimed from these individual frames.

Opened Taren civil images show the craft stopped in open space during focus loss, then separated at the office pad and restored to Natural bodies inside the matching arrivals hatch. At full focus, the approach stops near z=-25.98 against the pad while the old navigation goals request z=-23.3/-24.5. New draft goals remain outside that physical clearance; the interaction and collision assertions are not relaxed. The full exterior circuit remains unaccepted and requires a focused retry.

Sela's opened side/rear circuit frames confirm separate craft outside the ring, but expose ragged decorative projections along the repeated ring-panel edges. The joints remain connected; that exposed edge presentation needs cleanup in the station's continuing visual review. These images do not grant a new MAP_EYE_TEST pass or override the recorded focus failure.

[Flight direction contract](../FLIGHT_DIRECTION.md) records the implemented nose/thrust/aim rules. Raw evidence remains under `Builds/quality/C4/`; [the manifest](C4-flight-hulls-integrated.json) retains exact hashes, reports and opened-image authorship. Capture overhead and concurrent foreign work exclude clean C1 timing. Continuous video viewing, captured audio audition and physical Logitech feel remain **UNVERIFIED**. Remaining skills, staged transformation, flight disable presentation, non-station MAP_EYE_TEST and later production gates remain open.
