"""Synthesise the sounds of what comes to live aboard Many Hands after chapter 5: places heard where they stand, not across the ship.

    python tools/manyhands_arrivals_ambience.py      # writes AMB_HandsHerd.ogg and AMB_HandsForge.ogg to Lattice/Assets/_Project/Audio/Ambience

The zone's bed (tools/manyhands_ambience.py) plays everywhere aboard from chapter 5 on, so a herd or a forge mixed into it would be heard
before either has come. These two are loops of their own, made here from seeded noise and sines with the helpers of tools/water_sounds.py
(nothing sourced), and each is played by an AudioSource on its arrival in the scene (ManyHandsBuilder.Arrivals.cs): there only once the
place is, and heard only near it. Seamless, mono in both channels, peaking at -6 dBFS; the builder sets the level and the distance it carries.
Needs numpy and imageio-ffmpeg. Ends with MANYHANDS_ARRIVALS_AMBIENCE_OK.

- The herd (chapter 6, the freight hall's west half): slow heavy breathing from several beasts out of step, a low call now and then that
  slides down, bone bells knocking on their harness, hay pulled and chewed, a hoof set down on deck plate.
- Tovra's forge (chapter 8, the freight hall's east wall): the hearth's roar breathing with the bellows, a hammer on an anvil in the smiths'
  two-and-one, a bar quenched once with a long hiss, and the queue talking under it.
"""
from pathlib import Path
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import BEDS, RATE, band, level, ping, pink, seamless, slow, write_ogg  # noqa: E402


def norm(x):
    peak = np.max(np.abs(x))
    return x / peak if peak > 0 else x


def burst(t, at, length, attack=0.04):
    """1 inside [at, at + length], with soft ends."""
    rise = np.clip((t - at) / attack, 0, 1)
    fall = np.clip((at + length - t) / attack, 0, 1)
    return np.minimum(rise, fall)


def herd():
    rng = np.random.default_rng(601)
    n = int(22 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros(n)
    # Breathing: four beasts, each a slow in-and-out of low rough air at its own pace.
    for beast, rate in enumerate((0.21, 0.26, 0.18, 0.23)):
        air = band(rng.standard_normal(n), 70 + beast * 25, 420 + beast * 60)
        phase = rng.uniform(0, 2 * np.pi)
        breath = np.clip(np.sin(2 * np.pi * rate * t + phase), 0, None) ** 1.6
        out += norm(air) * breath * (0.32 - beast * 0.04)
    # A low call, twice in the loop: a hum that slides down, its harmonics made throaty by a band of noise riding on it.
    for at, pitch in ((3.8, 96), (14.6, 84)):
        span = 1.5
        k = np.clip((t - at) / span, 0, 1)
        f = pitch * (1 - 0.22 * k)
        phase = 2 * np.pi * np.cumsum(f) / RATE
        voice = np.sin(phase) + 0.5 * np.sin(2 * phase) + 0.25 * np.sin(3 * phase) + 0.12 * np.sin(5 * phase)
        throat = band(rng.standard_normal(n), 180, 900) * 0.35
        env = burst(t, at, span, 0.25) * np.sin(np.pi * k) ** 0.5
        out += norm(voice + throat) * env * 0.42
    # Bone bells on the harness: hollow knocks, a few at a time.
    for at in (1.1, 1.35, 6.2, 9.7, 9.9, 12.4, 17.1, 17.4, 19.8):
        hertz = rng.choice((620.0, 780.0, 930.0))
        out += (ping(t, at, hertz, 0.18, 0.15) + 0.4 * ping(t, at, hertz * 2.71, 0.06, 0.15)) * 0.16
    # Hay pulled from the rack and chewed: short bursts of dry high noise.
    for at in (2.4, 2.9, 7.6, 8.0, 8.5, 13.2, 16.0, 16.5, 20.4):
        hay = band(rng.standard_normal(n), 2200, 7000) * burst(t, at, 0.22, 0.03)
        out += norm(hay) * 0.08
    # A hoof set down on deck plate: a dull thud with a little ring.
    for at in (5.1, 11.0, 18.3):
        out += (ping(t, at, 70, 0.12, 0.4) + 0.3 * ping(t, at, 410, 0.05, 0.2)) * 0.3
    out += norm(pink(rng, n)) * 0.03
    mono = level(seamless(out, 1.5), 0.5)
    return np.stack([mono, mono], axis=1)


def forge():
    rng = np.random.default_rng(801)
    n = int(18 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros(n)
    # The hearth: a low roar that swells with each stroke of the bellows.
    bellows = 0.55 + 0.45 * np.clip(np.sin(2 * np.pi * 0.28 * t), 0, None) ** 1.3
    roar = band(rng.standard_normal(n), 60, 520) * (0.7 + 0.3 * slow(rng, n, 3.0))
    out += norm(roar) * bellows * 0.34
    breath = band(rng.standard_normal(n), 300, 1500) * np.clip(np.sin(2 * np.pi * 0.28 * t + 0.4), 0, None) ** 3
    out += norm(breath) * 0.08
    # The hammer: two quick and one held, over and over, with a rest every few bars while the work is turned.
    strikes = []
    bar = 0.0
    while bar < 17.0:
        if rng.random() < 0.82:
            strikes += [bar, bar + 0.34, bar + 0.92]
        bar += 1.7
    for at in strikes:
        if at > 17.6:
            continue
        hot = 1.0 if rng.random() < 0.7 else 0.6
        ring = ping(t, at, 1780, 0.32, 0.05) + 0.6 * ping(t, at, 2930, 0.18, 0.05) + 0.35 * ping(t, at, 4410, 0.09, 0.05)
        thud = ping(t, at, 140, 0.07, 0.3)
        out += (ring * 0.22 * hot + thud * 0.25)
    # A bar quenched once: a sharp hiss falling away.
    q = 11.3
    hiss = band(rng.standard_normal(n), 2600, 9500) * np.where(t >= q, np.exp(-(t - q) / 0.9), 0) * burst(t, q, 3.2, 0.02)
    out += norm(hiss) * 0.2
    # The queue talking under it.
    talk = band(pink(rng, n), 240, 1200) * slow(rng, n, 1.6) ** 2
    out += norm(talk) * 0.1
    mono = level(seamless(out, 1.2), 0.5)
    return np.stack([mono, mono], axis=1)


if __name__ == "__main__":
    for name, make in (("AMB_HandsHerd", herd), ("AMB_HandsForge", forge)):
        signal = make()
        rms = float(np.sqrt(np.mean(signal[:, 0] ** 2)))
        write_ogg(BEDS / (name + ".ogg"), signal)
        print(f"{name} rms={rms:.4f} seconds={len(signal) / RATE:.1f}")
    print("MANYHANDS_ARRIVALS_AMBIENCE_OK", BEDS)
