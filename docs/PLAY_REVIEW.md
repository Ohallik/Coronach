# Play and visual review — 2026-09-21

**QUALITY_REVIEW_REJECTED. The slice is playable, but it does not yet look or feel like a professional game.** The earlier WORLD_LOOK_OK acceptance was too lenient. Generated assets being present, a complete scripted route and passing tests do not establish that standard.

## What was actually played

I opened the release title and started New Game. I then used a development-only virtual gamepad to play from screenshots: issue a bounded stick/button input, observe the resulting frame, and choose the next action. Inputs pass through the ordinary Input System, GameInput and PlayerBrain. Simulation pauses between decisions. This is agent-directed play, not a human holding the Logitech pad, not a continuous real-time play session, and not a full interactively completed route.

- Opening: title, flight to Orrin's office, docking, walking to Orrin, advancing the real Yarn conversation, Sela joining, returning to flight, travelling to Sorrel, docking and talking to the survivor. Evidence: `Builds/review-live/001–023.png` and matching observations.
- Ground combat: approach the first Ridgehound pack from the outpost, attack, take damage, dodge with a successful Flash Move, use Pulse, swap to Sela and use Thread Lance. Three enemies died through normal combat. Open Party and Gear, then close the menu. Evidence: `Builds/review-ground/000–011.png`, matching JSON and `Builds/review-ground.log`.
- Flight combat: boost into the first Gullet chamber, fire at Darts, try and retry a lunge, brake, aim and fire, roll forward, shoot a Drifter and swap. Evidence: `Builds/review-flight/000–010.png`, matching JSON and `Builds/review-flight.log`. The swap exposed a severely lagging partner. This session did not clear the entire chamber or tunnel.

The two combat sessions used the developer starter loadout: level-one heroes, no equipped gear, initial full charge and supplied consumables/materials. Ground and flight were launched directly at the review locations. These are control and presentation checks, not fresh-profile balance or human pacing measurements.

After rebuilding, `Builds/review-fixed/000–014.png` was opened step by step. A boosted stretch and brake left the partner close enough that swapping changed position by about 4.7 m. Menu-close B changed position by only 0.002 m; a subsequent fresh B rolled normally. Fire followed by X produced the lunge, and a later lunge killed a Dart without inflicting wall damage on Sela. Static Net used charge, showed its cooldown and the transparent field. Taren followed into the chamber and attacked. This second flight session also stops before full chamber/tunnel completion. Step 003 is excluded as an input verdict: another player's window had taken focus; restoring the review window made step 004's swap work.

An initial hidden launch produced black frames and was discarded. The first review harness only stopped Time.timeScale, leaving the unscaled action clock running. Opening observations remain useful, but `review-live/024–025` are rejected as combat-timing evidence. The corrected harness stops GameTime and pending attacks; the ground and flight sessions above use that correction.

## Findings and changes

| Finding during play | Change / status |
|---|---|
| B closing the menu also dodged in the world | Consume gameplay buttons until release after closing UI; regression checks closing B and a subsequent fresh B. |
| A short X press immediately after firing disappeared | Buffer lunge for 0.2 seconds and prioritize it over held fire; test uses real pad events through PlayerBrain. |
| Contact with an enemy body applied wall damage | Ignore combatant bodies in the flight wall-impact handler; actual wall collisions still damage in combat. |
| Sela was almost 100 m behind when swapping in flight | Remove permanent partner braking, boost to catch up and prioritize following when separated; test boosted travel and swap distance. |
| Pending attacks could continue while paused | Freeze brains, pending hit volumes, projectiles, fields and charge/lunge coroutines with GameTime. |
| Solid warning discs hid enemy bodies | Use transparent outlined warnings with the permitted CC0 particle texture. Opened in both combat sessions. |
| HUD bars lacked clear labels; hints obscured bindings/costs | Label Integrity, Charge and Thrust; show level, costs, cooldowns and current device bindings. |
| Giant planet dominated the opening and exposed its seam | Move the backdrop farther away and reduce its screen coverage. |
| Decks floor overwhelmed the cast; doors lacked a room boundary | Smaller, quieter generated floor tiles; connect doors to walls; add generated consoles, benches and storage. |
| Sorrel arrival and routes were bare | Align arrival with the landing pad; add the generated skiff, supplies and ridge pieces. |

These fixes improve usability; they do not close the quality rejection.

## Still below the quality target

1. **Environment composition:** large repeated floor/membrane patterns dominate the view. Rooms and travel spaces lack convincing scale, varied structure and deliberate focal points. Sorrel's traversal areas remain sparse.
2. **Combat readability:** small, low-contrast enemies blend into the Gullet. Player, partner and enemy silhouettes need stronger separation; effects and reactions provide weak impact feedback.
3. **Animation and camera:** combat needs more convincing anticipation, contact and recovery. The fixed camera exposes repetitive surfaces and can lose important threats near HUD panels.
4. **Menus:** the Party/Gear screens function but use sparse rows and large empty areas; unavailable/empty states need useful context and a coherent layout.
5. **Polish and feel:** audio mix, real-time controller comfort, encounter pacing and the entire release route still need a physical-pad play session. Hardware attachment is verified; physical operation and feel are not.

Reacceptance requires playing and opening ordinary traversal, active ground/flight combat, dialogue and populated/empty menus at gameplay scale. Reject if the same repetitive surfaces, weak silhouettes, abrupt swaps or unclear feedback remain, even when all technical checks pass.

## Verification

Results from this correction pass are recorded in `docs/validation/play-review.json`. The earlier `slice-v0.1` snapshot and tag remain historical evidence. Review captures and isolated saves remain under `Builds/`; the initial release session's newly created default save was archived and the user's original absent save directory restored.

The complete regression route passed in **532.3 seconds**, reached level 6 without equipped gear and saved completion at Tallow Drift. Burrower took **66.1 s**, Cantor **87.8 s**; this is automated timing. Its completion screenshot was opened. The route includes the partner/input/world corrections and precedes the final small guard that cancels a buffered lunge when a menu opens; the final runtime suite covers that guard. The route driver does not use PlayerBrain input.

Both Windows packages were rebuilt. The release title smoke passed with the Logitech attached and its screenshot was opened. A route was restarted after a concurrent title smoke replaced the shared default Player.log; the smoke runner now supplies scenario-specific log/save paths. Only the rerun with independent logs is valid final-route evidence. Final test counts and package hashes are in the validation snapshot.

Final suites: **35 EditMode / 21 PlayMode**, all passed, zero skips. The added menu-buffer regression confirms that an X queued during fire recovery is cancelled by opening UI rather than unexpectedly executing after closing it.

For another agent review, launch the development player with `-review <absolute-folder> -savepath <isolated-folder>` and keep its window focused. Use `scripts/review-step.ps1 -Folder <folder> -Id <fresh-number>` with stick, trigger, button and bounded duration arguments. Open each numbered image before choosing another input. No gameplay teleport/attack API is exposed by this harness.
