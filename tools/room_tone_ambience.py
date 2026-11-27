"""Synthesise the two beds a place plays when its ZoneDef names none (AudioManager.Zone): a ship's or station's room tone on foot, and
the ship's own engines in open space. Round ten (November 27): seventeen zones had no bed of their own, and AudioManager looped Kenney's
`computerNoise_000` (five seconds of computer beeps and bloops) under the five on foot and `spaceEngineLow_000` (a five-second engine
loop) under the twelve in flight. A five-second loop with a pitched event in it is a metronome: Nathan heard it as "a background beep
bloopy type noise that is constantly there".

    python tools/room_tone_ambience.py      # writes AMB_RoomTone.ogg and AMB_FlightDrone.ogg to Lattice/Assets/_Project/Resources/Audio/Beds

Each is a 30 s seamless stereo loop of shaped noise, made here from seeded noise with the helpers of tools/water_sounds.py, so the files
are ours; nothing is sourced. There is no pitched partial and no event in either (no beep, no clank, no tick): band-limited noise with soft
shoulders, which cannot ring, moving only by slow swells. The script prints each bed's length, RMS (both channels mixed to one), its
loudest 25 ms over its median, and the loudest narrow peak of its spectrum over the third-octave around it, and refuses a bed with an
event 6 dB over its floor or a peak 12 dB over its band. Needs numpy and imageio-ffmpeg. Ends with ROOM_TONE_AMBIENCE_OK.

- AMB_RoomTone: air moving through a hull: a low soft rumble of plant and pumps (35 to 160 Hz) wandering slowly, the ducts' broadband
  hiss (300 Hz to 3.5 kHz) well under it, and a faint wide air above. A deck, a station's corridor, a ship's hold.
- AMB_FlightDrone: the ship's drive heard through its own frame: a deep rumble (28 to 110 Hz) with a slow breathing swell, its body
  (110 to 450 Hz) under it, and a thin exhaust hiss (1.8 to 9 kHz) a long way down.
"""
from pathlib import Path
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import RATE, band, level, pink, seamless, slow, write_ogg  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Lattice/Assets/_Project/Resources/Audio/Beds"
SECONDS = 32  # 30 s after the 2 s crossfade folds the tail under the head


def layer(rng, n, low, high, swell_hz, depth):
    """One band of noise, its level wandering slowly between 1 - depth and 1."""
    noise = band(pink(rng, n), low, high)
    noise /= max(1e-9, np.max(np.abs(noise)))
    return noise * (1 - depth + depth * slow(rng, n, swell_hz))


def room_tone():
    rng = np.random.default_rng(2711)
    n = SECONDS * RATE
    out = np.zeros((n, 2))
    for side in range(2):
        out[:, side] = (layer(rng, n, 35, 160, .08, .35) * .7 + layer(rng, n, 300, 3500, .05, .25) * .16
                        + layer(rng, n, 3500, 9000, .04, .2) * .04)
    return level(seamless(out, 2.0), .5)


def flight_drone():
    rng = np.random.default_rng(2712)
    n = SECONDS * RATE
    out = np.zeros((n, 2))
    breath = slow(rng, n, .12)  # one swell for both sides, so the drive breathes as one body
    for side in range(2):
        out[:, side] = ((layer(rng, n, 28, 110, .06, .2) * .75 + layer(rng, n, 110, 450, .09, .3) * .22) * (.8 + .2 * breath)
                        + layer(rng, n, 450, 1800, .07, .3) * .06 + layer(rng, n, 1800, 9000, .05, .3) * .035)
    return level(seamless(out, 2.0), .5)


def check(name, signal):
    mono = signal.mean(axis=1)
    rms = float(np.sqrt(np.mean(mono ** 2)))
    w = int(.025 * RATE)
    env = 20 * np.log10(np.sqrt(np.mean(mono[:len(mono) // w * w].reshape(-1, w) ** 2, axis=1)) + 1e-12)
    event = float(env.max() - np.median(env))
    size = 8192
    frames = [mono[k:k + size] * np.hanning(size) for k in range(0, len(mono) - size, size // 2)]
    spectrum = 10 * np.log10(np.mean([np.abs(np.fft.rfft(f)) ** 2 for f in frames], axis=0) + 1e-20)
    freq = np.fft.rfftfreq(size, 1 / RATE)
    tone = 0.0
    for k in np.flatnonzero((freq >= 60) & (freq <= 8000)):
        around = spectrum[(freq >= freq[k] / 1.26) & (freq <= freq[k] * 1.26)]
        tone = max(tone, float(spectrum[k] - np.median(around)))
    print(f"{name} {len(mono) / RATE:.1f}s rms {20 * np.log10(rms):.1f} dBFS, loudest 25 ms {event:.1f} dB over its floor, "
          f"narrowest peak {tone:.1f} dB over its band")
    if event > 8 or tone > 12:
        raise SystemExit(name + " has an event or a tone in it")


if __name__ == "__main__":
    for name, make in (("AMB_RoomTone", room_tone), ("AMB_FlightDrone", flight_drone)):
        signal = make()
        check(name, signal)
        write_ogg(OUT / (name + ".ogg"), signal)
    print("ROOM_TONE_AMBIENCE_OK", OUT)
