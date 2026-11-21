"""Synthesise chapter 9's two Duskline beds: the Meridian Crawl (a town on treads that has never stopped) and the Terminator (the open band with the dawn behind).

    python tools/duskline_ambience.py      # writes AMB_MeridianCrawl.ogg and AMB_Terminator.ogg to Lattice/Assets/_Project/Audio/Ambience

Each a 24 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as tools/works_ambience.py does), so the
files are ours; nothing is sourced. Nobody can listen to them here, so they are measured instead: the script prints each bed's RMS (mixed to one channel) and
the ZoneDef volume that plays it at the level the earlier beds play at (file RMS times volume about 0.0224, -33 dB). HomeBuilder.Crawl.BedLevel and
HomeBuilder.Terminator.BedLevel hold those volumes beside the paths. Needs numpy and imageio-ffmpeg. Ends with DUSKLINE_AMBIENCE_OK.

The Crawl:
- The treads: a deep rolling rumble under everything, and the links going over the sprockets as a soft iron knock on a steady beat (the town's walking pace),
  two halls a little out of step.
- The town shaking loose and lashed: chains and lamp hooks clinking on the swing, a long hull creak as a hall lurches, a hammer on a coupling far off.
- The plain going past: dry wind over grit outside the rails.
- People: a murmur from the market, a hand bell once.
The Terminator:
- Open twilight ground: a cold low wind, gusting, grit lifted and dropped.
- Frost: small dry ticks and crackles as rime settles in the hollows.
- The dawn: a far, very low roar from behind the ridge, swelling and falling.
- The Crawl far off: its tread rumble, faint, from one side.
"""
from pathlib import Path
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import BEDS, RATE, band, level, ping, pink, seamless, slow, write_ogg  # noqa: E402

TARGET = 0.0224   # file RMS x ZoneDef volume, as every earlier bed plays
SECONDS = 24


def pan_add(out, sound, gain, pan):
    out[:, 0] += sound * gain * (1 - pan)
    out[:, 1] += sound * gain * pan


def norm(x):
    return x / max(1e-9, np.max(np.abs(x)))


def knock(t, at, hertz, decay):
    """An iron link going over a sprocket: a dull struck tone with a short gritty attack."""
    return ping(t, at, hertz, decay) + 0.4 * ping(t, at, hertz * 2.7, decay * 0.4)


def crawl():
    rng = np.random.default_rng(1409)
    n = int(SECONDS * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        rumble = band(pink(rng, n), 28, 140) * (0.75 + 0.25 * slow(rng, n, 0.15))
        out[:, side] = norm(rumble) * 0.45
    # The links over the sprockets: a knock every 0.64 s (a 0.9 m plate at 1.4 m/s), two halls a little out of step and pitch.
    for offset, hertz, pan in ((0.0, 72, 0.35), (0.27, 66, 0.65)):
        beat = np.zeros(n)
        at = offset
        while at < SECONDS - 0.2:
            beat += knock(t, at, hertz * (0.97 + 0.06 * rng.random()), 0.08) * (0.7 + 0.3 * rng.random())
            at += 0.643
        pan_add(out, norm(beat), 0.12, pan)
    # Wind over grit outside the rails.
    wind = band(pink(rng, n), 300, 2600) * (0.4 + 0.6 * slow(rng, n, 0.25) ** 2)
    pan_add(out, norm(wind), 0.07, 0.5)
    # Chains and lamp hooks clinking on the swing (the town's 3.6 s period).
    for k in range(int(SECONDS / 3.6)):
        for j in range(3):
            at = k * 3.6 + 1.7 + j * 0.09 + 0.04 * rng.random()
            pan_add(out, ping(t, at, 2600 + 900 * rng.random(), 0.03), 0.03, 0.2 + 0.6 * rng.random())
    # A hall lurching: a long low creak, twice.
    for at, pan, hertz, length in ((5.2, 0.3, 88, 2.4), (15.6, 0.7, 96, 2.8)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + length)
        env = np.sin(np.pi * np.clip(local / length, 0, 1)) ** 1.5 * live
        creak = np.sin(2 * np.pi * (hertz * local - 5 * local * local))
        pan_add(out, norm(creak * env), 0.09, pan)
    # A hammer on a coupling far off: three strokes, twice.
    for start, pan in ((8.1, 0.2), (19.3, 0.8)):
        for k in range(3):
            pan_add(out, ping(t, start + k * 0.62, 1250 + 80 * k, 0.07), 0.05, pan)
    # The market's murmur, and a hand bell once.
    murmur = band(pink(rng, n), 200, 900) * slow(rng, n, 1.3) ** 2 * (0.5 + 0.5 * slow(rng, n, 0.1))
    pan_add(out, norm(murmur), 0.05, 0.4)
    for k in range(4):
        pan_add(out, ping(t, 12.4 + k * 0.28, 1480, 0.5) + 0.5 * ping(t, 12.4 + k * 0.28, 2960, 0.3), 0.025, 0.6)
    return level(seamless(out, 2.0), 0.5)


def terminator():
    rng = np.random.default_rng(2207)
    n = int(SECONDS * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        wind = band(pink(rng, n), 60, 900) * (0.35 + 0.65 * slow(rng, n, 0.2) ** 2)
        out[:, side] = norm(wind) * 0.4
    # Gusts lifting grit, now and then.
    grit = band(rng.standard_normal(n), 1800, 6000) * slow(rng, n, 0.3) ** 4
    pan_add(out, norm(grit), 0.05, 0.55)
    # Frost ticking as it settles in the hollows.
    for k in range(70):
        at = rng.random() * (SECONDS - 0.2)
        pan_add(out, ping(t, at, 3800 + 2400 * rng.random(), 0.004), 0.025 * (0.5 + rng.random()), rng.random())
    # The dawn behind the ridge: a far, very low roar, swelling and falling.
    roar = band(pink(rng, n), 22, 90) * (0.3 + 0.7 * slow(rng, n, 0.06) ** 1.5)
    pan_add(out, norm(roar), 0.22, 0.5)
    # The Crawl far off: its rumble and knock, faint, from one side.
    far = np.zeros(n)
    at = 0.1
    while at < SECONDS - 0.2:
        far += knock(t, at, 64, 0.1)
        at += 0.643
    pan_add(out, norm(band(pink(rng, n), 30, 110)) * 0.6 + norm(far) * 0.4, 0.05, 0.15)
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    for name, make in (("AMB_MeridianCrawl", crawl), ("AMB_Terminator", terminator)):
        signal = make()
        write_ogg(BEDS / (name + ".ogg"), signal)
        mono = signal.mean(axis=1)
        rms = float(np.sqrt(np.mean(mono ** 2)))
        print(f"{name} rms={rms:.4f} volume={TARGET / max(1e-9, rms):.3f} seconds={len(mono) / RATE:.1f}")
    print("DUSKLINE_AMBIENCE_OK")
