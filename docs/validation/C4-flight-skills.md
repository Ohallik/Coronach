# C4 visible flight skill source checkpoint

September 27, 2026. **C4 OPEN; ordinary recapture and full integrated PlayMode pending.** Taren's flight Cleave/Pulse now release visible expanding boundaries. Sela's Static Net travels from the posed nose, stops at opaque cover, retains flight altitude and has a six-metre destination limit from the craft. Flight Overdrive/Refract arm after anticipation and show hull cues tied to their real timed state. Pending work cancels on interruption; released energy can finish.

Cleave deliberately changes the old offset sphere into a forward 130-degree arc reaching 5.4 m after 0.12 s anticipation. Pulse reaches 5 m after 0.18 s anticipation; the net releases after 0.15 s and buffs after 0.12 s. Costs, cooldowns, damage packets and buff durations are retained. Short recovery locks cover the new release times. Ground throws retain their existing arc, floor placement and cast timing.

| Raw evidence under `Builds/quality/C4/` | Result |
|---|---|
| `flight-skills-red01` | 0/8, zero skips, 28.3958223 s. Rejects hidden Cleave/Pulse damage, missing boundaries/buff cues, immediate paused buff arming, a net through cover (1000 to 993.25 HP) and an unbounded distant net (1000 to 986.5 HP). |
| `flight-skills-green01` | 31/31, zero skips, 146.4881353 s. New flight checks plus shared ground skills, flight emitters, thrust and lunge contact pass. |
| `flight-skills-fault-red01` | 0/3, zero skips, 11.394488 s. Deliberate floor projection lands at approximately y=0 instead of 4.8. Deliberate damage behind the passed wave reduces 1000 to 972.5 HP. A separate new source-cleanup test exposes a real net faction bug (ally 120 to 70 HP). |
| `flight-skills-green02` | 17/17, zero skips, 80.7917782 s. Both injected faults are removed. The nine flight and eight ground skill checks pass after the released seed carries its captured faction into the persistent field. |
| `flight-skills-editmode01` | Full 48/48 EditMode, zero skips. |

The wave checks compare the actual rendered edge with the target body at damage, require one hit, reject opaque-cover crossings and prevent a damage-filled interior after the wave passes. The net checks include near-body cover, a banked independently measured nose, an actual floor beneath the flight plane, bounded range, source cleanup and an opposing-body positive control. These are runtime regressions, not observed ordinary play or aesthetic acceptance.

The preceding player captures rejected faint exhaust at gameplay scale. The energy shader now has explicit intensity; the same owned flare's bright cross-section occupies more of the narrow line width. Hull/socket positions and movement tuning are unchanged. This implementation still needs fresh ordinary-player visual acceptance; enlarged controlled renders cannot substitute for it.

Codex opened the four `hull-effects-04` side/top PNGs. Both ships show attached bright plumes and a continuous narrow coloured lunge boundary without the previously rejected heavy white rim. This controlled-scale review does not close ordinary readability.

Both packages still contain the earlier `abaf4be` runtime until rebuilt. Original saves remain unchanged. Staged transformation, flight disable presentation, long focused circuits, station ring-edge cleanup, non-station MAP_EYE_TEST and later production gates remain open. Normal/slow video viewing, captured audio audition and physical Logitech feel remain **UNVERIFIED**. Exact hashes and opened-image authorship are retained in the companion manifest.
