# C5 owned sound candidates

September 27, 2026. **C5 OPEN.** `tools/prepare_audio_palette.py` now reproducibly prepares 32 small candidates from 37 owned Kenney sources, outside Unity. These are candidate derivatives; they have not been auditioned or assigned to gameplay. Music originals and cue assignments remain untouched.

The 1,069,544-byte set contains four footsteps each for metal, rock and soil; three soft impacts, armor impacts and swings; three emitter variations per hero; and five interface/dialogue cues. Metal steps layer concrete footfalls with restrained plate contact. Impacts combine two weighted source layers; swing candidates combine the knife/cloth sources with different playback rates. These are filename-guided design hypotheses, not listening judgments.

The pipeline decodes to mono 48 kHz, retains weighted layers/delays, applies bounded onset/tail fades and writes 16-bit PCM at a nominal -10 dBFS sample peak. Each output records its exact recipe and source, licence, tool and output hashes. It refuses an existing output folder and only writes under `Builds/quality`. The duplicate-output control rejects the second invocation, and subsequent checks confirm all 37 originals, four licence files and 32 outputs are unchanged.

`palette-candidate-levels-02` measures all 32 outputs with the previously verified rejecting signal checker: all are nonsilent and have true peaks between -10.0 and -9.2 dBTP. These are isolated asset levels, not a combined game mix. The checker's seven real/invalid/silent/hot/rounding controls remain passing. [Manifest](C5-palette-candidate.json) points to exact recipes, outputs and numerical logs.

Reproduce: `python tools/prepare_audio_palette.py --output Builds/quality/C5/<new-folder>`. The optional `--owned-art-root` and `--scifi-art-root` specify existing owned source directories. Originals are read only. No raw donor or candidate audio is committed at this checkpoint.

Runtime cue selection, contact-timed footsteps, layered voice playback, bounded pools, mixer groups and saved controls, flight/defeat families, required captured mixes and listening remain pending. Subjective audition is **UNVERIFIED**; no sound-quality acceptance or completed C5 gate is claimed.
