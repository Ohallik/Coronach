# C4 flight dash contact checkpoint

September 27, Codex. **C4 OPEN.** Lunge contact now follows the craft's actual controller movement and checks solid cover. It no longer creates five damaging spheres in distant space over five rendered frames. Taren's flight dash skill uses the same travelled-contact path.

The motor reports each completed dash segment, including its final partial step. Contact uses the existing 1.05 m attack radius, rejects solid static and movable cover, and damages each target at most once per action. Pause suspends travel and contact; a changed action, downed hero, form change or new dash invalidates the old contact. The visual bursts occur along actual travelled space at distance intervals. Damage, lunge distance, skill cost and cooldown remain unchanged.

| Raw evidence under `Builds/quality/C4/` | Result |
|---|---|
| `lunge-contact-red` | 1/4 PlayMode passes, 3 fail, 14.3056718 s. The lunge damages through a wall, hits from a stationary craft, and reaches 2.666715 m beyond the travelled segment. |
| `lunge-contact-green` | Original four tests pass, zero skips, 17.8570273 s. Both heroes hit once along actual movement; interrupted contact is rejected. |
| `skill-dash-contact-red` | Expanded matrix passes 5/6, 28.8964013 s. Near-wall contact and pause/resume pass, but Taren's flight skill still inflicts 90 damage beyond its blocking wall. |
| `skill-dash-contact-green` | 6/6 pass, zero skips, 29.8382315 s. Includes both heroes against static/movable cover at two distances, ordinary positive contact, no-travel/interruption, pause/resume and blocked/unobstructed skill dash. |

`contact-integrated-01` is retained as **97/98 PlayMode, one failure, zero skips**, 680.9819555 s. All new contact and Sorrel tests pass. The existing Overdrive fixture expected 100 after expiry but received 105: it set fortune to zero only before its first sample, then normal half-second equipment refresh restored fortune and allowed a critical bonus on later samples. The fixture now sets zero fortune immediately before each synchronous packet sample, retaining the exact 100/120 damage assertions and real pause/expiry/down checks. Gameplay critical behavior is unchanged. Targeted verification and a fresh full run are required before the integrated build.

`overdrive-fixture-green` passes 1/1, zero skips, 15.151723 s, with the original damage/pause/expiry/down conditions retained. This is a **source checkpoint**. Full integrated green and both player rebuilds remain pending. Development still contains Sorrel blockout 06, and Release the previous integrated C3 package. Original save hashes are unchanged.

Ordinary Taren/Sela routes are prepared with unchanged starter enemies and legal virtual-gamepad input. No new observed flight-quality acceptance is claimed yet. Full civil/Gullet circuits, flight sockets/other skills, ship defeat presentation and transformation review remain open. Continuous normal/slow viewing, actual audio audition and physical-controller feel remain UNVERIFIED.
