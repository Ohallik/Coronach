# C3/C4 defeat finish and rescue banter — September 28

**C3 and C4 OPEN.** Nathan reported inadequate deaths. Codex's lifecycle work gave enemies readable death motions and settled corpses, but every enemy still vanished between two frames at full size when its hold ended. Airborne Choristers only rolled 58° and dipped 22 cm before that pop. C4 also recorded a context problem: when a hero went down, the automatic swap played the partner's casual banter ("Try to keep up, Vosk.").

| Change | Behaviour |
|---|---|
| Deliberate cleanup | After its hold, every enemy (the whole root, including Cantor's segments) settles 15 cm and shrinks away over 0.45 s with a small dust puff, then is removed. |
| Flier failure | A killed Chorister loses lift: over 1.1 s it tumbles about 150° and falls 1.8 m below the flight plane, holds 0.6 s, then cleans up. |
| Rescue banter | A forced swap after a hero goes down uses a new cover line (Taren: "Stay down a second. I've got this." / Sela: "Hold still, Vosk. I'm covering you."). Ordinary player swaps keep their banter. |

All removal times stay inside the existing lifecycle bounds (at most 2.75 s for normal enemies, 3.6 s for bosses).

## Results (zero skips)

| Run | Result |
|---|---|
| `C3/defeat-finish-red01` | **3/3 rejected as intended** on the old code: the Ridgehound left in a single frame, the Dart hovered 0.22 m below its flight line, and the partner said "Try to keep up, Vosk." |
| `C3/defeat-finish-green01` | **20/20**: new checks plus the death lifecycle, creature defeat, flight disable, cue and swap/revive suites. |
| `C3/full-integration-defeat01` | **56/56 EditMode, 164/164 PlayMode**. |
| `C3/defeat-finish-visual01` | Development-player defeat probe, extended to capture cleanup at 35% and 75%, 104 stills (`CREATURE_DEFEAT_CAPTURE_OK`). Claude opened the Ridgehound terminal/cleanup frames and the Chorister Dart standing/fall/cleanup frames (`defeat-finish-sheet01.png`). The corpse visibly shrinks into the floor, and the Dart tumbles and drops out of its framed position before shrinking. The dust puff itself is not visible in these stills. |

Continuous video and physical feel remain UNVERIFIED. Boss finishes are still the generic lifecycle plus the old crunch, pending designed finishes.
