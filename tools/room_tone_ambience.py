"""Synthesise the two beds a place plays when its ZoneDef names none (AudioManager.Zone): a ship's or station's room tone on foot, and
the ship's own drive in open space. Round ten (November 27): seventeen zones had no bed of their own, and AudioManager looped Kenney's
`computerNoise_000` (five seconds of computer beeps and bloops) under the five on foot and `spaceEngineLow_000` (a five-second engine
loop) under the twelve in flight. A five-second loop with a pitched event in it is a metronome: Nathan heard it as "a background beep
bloopy type noise that is constantly there".

Round eleven (the same night): round ten's two beds were shaped noise with a duct's hiss (300 Hz to 3.5 kHz) and a faint air over 3.5 kHz
in them, played at about -30 dBFS RMS a channel. Nathan: "the constant background kind of air noise kind of makes it seem like the music
is muffled". Noise across the band the music's detail lives in is what muffled is, and a rumble only two decibels under the music's own
bass is another. Both are now a room, not air: low, dark tone felt more than heard, with nothing over about 800 Hz (under a hundredth of
the energy over 800 Hz, none of it hiss), and CueLevels plays them at about -46 dBFS RMS a channel, sixteen decibels under round ten.

    python tools/room_tone_ambience.py      # writes AMB_RoomTone.ogg and AMB_FlightDrone.ogg to Lattice/Assets/_Project/Resources/Audio/Beds

Each is a 30 s seamless stereo loop of band-limited noise, made here from seeded noise with the helpers of tools/water_sounds.py, so the
files are ours; nothing is sourced. There is no pitched partial and no event in either (no beep, no clank, no tick, no two tones that
could beat): noise with soft shoulders, which cannot ring, moving only by slow swells. The script prints each bed's length, RMS a channel,
its loudest 25 ms over its median, the loudest narrow peak of its spectrum over the third-octave around it (40 Hz to 400 Hz, inside the
bed's band), and its share of energy over 800 Hz, and refuses a bed with an event 8 dB over its floor, a peak 12 dB over its band, or more
than 1% of its energy over 800 Hz. Needs numpy and imageio-ffmpeg. Ends with ROOM_TONE_AMBIENCE_OK.

- AMB_RoomTone: a hull heard from inside it: the plant's low rumble (30 to 150 Hz) wandering slowly, and its body (150 to 420 Hz) well
  under it. A deck, a station's corridor, a ship's hold.
- AMB_FlightDrone: the ship's drive far behind the seat: a deep rumble (24 to 110 Hz) breathing slowly as one body, and its body (110 to
  360 Hz) under it.
"""
from pathlib import Path
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import RATE, band, level, pink, seamless, slow, write_ogg  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Lattice/Assets/_Project/Resources/Audio/Beds"
SECONDS = 32  # 30 s after the 2 s crossfade folds the tail under the head
TOP = 800  # nothing of either bed above this


def layer(rng, n, low, high, swell_hz, depth):
    """One band of noise, its level wandering slowly between 1 - depth and 1."""
    noise = band(pink(rng, n), low, high)
    noise /= max(1e-9, np.max(np.abs(noise)))
    return noise * (1 - depth + depth * slow(rng, n, swell_hz))


def dark(signal):
    """Everything over about 560 Hz rolled off, to nothing by 700 Hz: what the music's detail lives in is left to the music."""
    return np.stack([band(signal[:, side], 1, 560, soft=.25) for side in range(signal.shape[1])], axis=1)


def room_tone():
    rng = np.random.default_rng(2811)
    n = SECONDS * RATE
    out = np.zeros((n, 2))
    for side in range(2):
        out[:, side] = layer(rng, n, 30, 150, .06, .3) * .8 + layer(rng, n, 150, 420, .05, .3) * .12
    return level(seamless(dark(out), 2.0), .5)


def flight_drone():
    rng = np.random.default_rng(2812)
    n = SECONDS * RATE
    out = np.zeros((n, 2))
    breath = slow(rng, n, .1)  # one swell for both sides, so the drive breathes as one body
    for side in range(2):
        out[:, side] = (layer(rng, n, 24, 110, .05, .2) * .8 + layer(rng, n, 110, 360, .08, .3) * .1) * (.8 + .2 * breath)
    return level(seamless(dark(out), 2.0), .5)


def check(name, signal):
    rms = float(np.sqrt(np.max(np.mean(signal ** 2, axis=0))))
    mono = signal.mean(axis=1)
    w = int(.025 * RATE)
    env = 20 * np.log10(np.sqrt(np.mean(mono[:len(mono) // w * w].reshape(-1, w) ** 2, axis=1)) + 1e-12)
    event = float(env.max() - np.median(env))
    size = 16384
    frames = [mono[k:k + size] * np.hanning(size) for k in range(0, len(mono) - size, size // 2)]
    power = np.mean([np.abs(np.fft.rfft(f)) ** 2 for f in frames], axis=0)
    spectrum = 10 * np.log10(power + 1e-20)
    freq = np.fft.rfftfreq(size, 1 / RATE)
    tone = 0.0
    for k in np.flatnonzero((freq >= 40) & (freq <= 400)):  # inside the bed's band: past its edges the median is the roll-off's, not a band's
        around = spectrum[(freq >= freq[k] / 1.26) & (freq <= freq[k] * 1.26)]
        tone = max(tone, float(spectrum[k] - np.median(around)))
    over = float(power[freq > TOP].sum() / power.sum())
    print(f"{name} {len(mono) / RATE:.1f}s rms a channel {20 * np.log10(rms):.1f} dBFS, loudest 25 ms {event:.1f} dB over its floor, "
          f"narrowest peak {tone:.1f} dB over its band, {100 * over:.3f}% of its energy over {TOP} Hz")
    if event > 8 or tone > 12 or over > .01:
        raise SystemExit(name + " has an event, a tone or a top in it")


if __name__ == "__main__":
    for name, make in (("AMB_RoomTone", room_tone), ("AMB_FlightDrone", flight_drone)):
        signal = make()
        check(name, signal)
        write_ogg(OUT / (name + ".ogg"), signal)
    print("ROOM_TONE_AMBIENCE_OK", OUT)
