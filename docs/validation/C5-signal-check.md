# C5 captured-mix signal check — September 27

**C5 OPEN.** `tools/audio_audit.py` measures existing captured mixes with FFmpeg's oversampled true-peak and integrated-loudness filter. It writes a new evidence directory, preserves the raw measurement log and records input/tool hashes. Failed decoding, missing measurements, silence, changed inputs and peaks without demonstrable margin below the ceiling reject the run. The summary has 0.1 dB resolution; a printed −1.0 dBTP is conservatively rejected because rounding can hide a value above the unchanged −1 dBTP production ceiling.

Seven controls pass, including real audible/silent/hot PCM fixtures, invalid audio, missing summaries and refusal to overwrite evidence. The rounded-boundary control first failed against the previous comparison (6/7), then passed after correction (7/7). Raw source snapshots and logs are retained in `Builds/quality/C5/rounded-peak-red` and `rounded-peak-green`. These controls concern measurement integrity, not game sound quality.

The first 60 seconds of two existing captures were measured without modifying them:

| Capture | Integrated loudness | True peak | Numerical result |
|---|---:|---:|---|
| C0 Decks walk, `decks-capture-02/mix.wav` | −22.2 LUFS | −7.4 dBTP | Pass |
| Sorrel western route, `sorrel-west-blockout-walk-02/mix.wav` | −21.6 LUFS | −9.3 dBTP | Pass |

The latter route was rejected for later focus loss; this selected first-minute signal window does not reverse that route rejection. Neither window establishes the required dense-fight, flight-fight or final sound-family coverage. Earlier measurements remain in `baseline-mix-levels-01` and `-02`; the final checker result is `-03`.

Inventory confirms eight staged science-fiction cues and the existing single effects source. Read-only owned-pack inventory records 130 Impact, 100 Interface and 52 RPG clips with their CC0 licence paths/hashes. No sound selection, derivative, music change or gameplay audio implementation is included in this checkpoint. Required sound families, pooled voices, mixer/controls, voice/DSP census and listening remain open. Actual mix audition at low/normal volume is **UNVERIFIED**; no audio was claimed heard through the numerical tool. Exact artifacts are indexed in [C5-signal-check.json](C5-signal-check.json).

Reproduce with `python -m unittest discover -s tools -p test_audio_audit.py -v`, then `python tools/audio_audit.py <capture.wav> --seconds 60 --output <new-evidence-folder>`.
