# C5 music cues and supplied soundtrack — September 28

**C5 OPEN.** Nathan supplied 20 more original tracks (Suno, converted to OGG), for 27 unique titles in total. All are hashed with objective measurements in [docs/music/tracks.json](../music/tracks.json); nobody has listened to them in this session. The seven earlier originals are byte-identical to their September 23 hashes. `Seed Shakers of Xylos-2.ogg` measures identically to the original and is left out as a duplicate conversion. The MP3 downloads stay local.

## Implemented in the slice

| Context | Before | After |
|---|---|---|
| The Gullet and the flight arena (space combat) | silent | **Starfight** |
| Any active boss encounter (Burrower, Cantor) | the zone's cue | **Alien Boss Battle** from the boss's arrival until its defeat or removal, then back to the zone cue |
| Title, Decks/Cinder/Tallow, shops, Sorrel combat | Title Theme, Hub Town Groove, Moonbase Market, Adventure Awaits | unchanged |

`MusicIntake.Stage` copies only the six tracks the slice plays, after verifying each against the manifest, with the original streamed-Vorbis import settings. The other 21 stay in `Music/` until their maps exist, so builds carry no unused music. The planned regional assignments are in the [atlas music map](../WORLD_ATLAS.md#music-map).

## Results (zero skips)

| Run | Result |
|---|---|
| `music-cues-red01` | **Rejected as intended**: the Gullet's cue was null. |
| `music-cues-green01` | **13/13**: new cue test, the original title/town/shop/moon fade regression, voices and death lifecycle. |
| `full-integration04` | **56/56 EditMode, 161/161 PlayMode**. |

The new test also removes a living boss without a death, as happens on a retry, and requires its cue to end.

## Limits

Listening is **UNVERIFIED**. Many Suno tracks end in a long fade (the last second measures -37 to -67 dB), so each loop dips before restarting. Choosing loop points needs Nathan's ear and is left open rather than guessed.
