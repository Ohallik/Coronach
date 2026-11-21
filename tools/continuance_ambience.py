"""Synthesise chapter 10's Continuance bed: the director's great ship, clean and well kept, on the morning it is boarded.

    python tools/continuance_ambience.py      # writes AMB_Continuance.ogg to Lattice/Assets/_Project/Audio/Ambience

A 24 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as tools/works_ambience.py
does), so the file is ours; nothing is sourced. Nobody can listen to it, so it is measured instead: the script prints the bed's RMS (mixed
to one channel) and the ZoneDef volume that plays it at the level the earlier beds play at (file RMS times volume about 0.0224, -33 dB).
LastMooringBuilder.Continuance holds that volume as a constant beside the path (Continuance.BedLevel). Needs numpy and imageio-ffmpeg.
Ends with CONTINUANCE_AMBIENCE_OK.

- The drive: a deep steady hum a long way aft, in tune with itself (a fifth over it, very faint), never wavering: a ship that is looked after.
- The air: soft filtered air handlers, even, with no rattle in them; a slow swell as a vent cycles.
- The board: now and then the Compact's two-note chime, high and soft, and a pressure door's sigh opening and closing somewhere.
- The crews: footfalls in step on a deck overhead, a murmur too far to make out, a tool set down on a bench.
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


def continuance():
    rng = np.random.default_rng(1014)
    n = int(SECONDS * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    # The air: soft, even, filtered high enough to read as clean vents rather than a hollow keel, swelling as a vent cycles.
    for side in range(2):
        air = band(pink(rng, n), 180, 2400) * (0.8 + 0.2 * slow(rng, n, 0.05))
        out[:, side] = norm(air) * 0.32
    # The drive: one steady low note and its octave and fifth, very slightly beating against itself for warmth, never wavering.
    drive = (np.sin(2 * np.pi * 49.0 * t) + 0.45 * np.sin(2 * np.pi * 98.0 * t) + 0.12 * np.sin(2 * np.pi * 147.2 * t)
             + 0.3 * np.sin(2 * np.pi * 49.35 * t))
    pan_add(out, norm(drive), 0.11, 0.5)
    # The board's chime, twice in the loop: two soft notes a fourth apart.
    for at, pan in ((5.2, 0.35), (16.8, 0.65)):
        pan_add(out, ping(t, at, 1318.5, 0.6) + 0.8 * ping(t, at + 0.32, 1760.0, 0.7), 0.035, pan)
    # A pressure door's sigh: a short breath of filtered air, rising then falling, two doors on different sides.
    for at, pan, length in ((2.4, 0.25, 0.9), (12.1, 0.75, 1.1), (20.3, 0.4, 0.8)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + length)
        env = np.sin(np.pi * np.clip(local / length, 0, 1)) ** 2 * live
        sigh = band(rng.standard_normal(n), 500, 3200) * env
        pan_add(out, norm(sigh), 0.06, pan)
    # Footfalls in step on a deck overhead: a squad crossing, twice.
    for start, pan in ((7.0, 0.6), (18.0, 0.4)):
        for k in range(10):
            at = start + k * 0.5
            pan_add(out, ping(t, at, 90 + 10 * rng.random(), 0.035), 0.05, pan)
    murmur = band(pink(rng, n), 200, 800) * slow(rng, n, 1.4) ** 2 * (0.5 + 0.5 * slow(rng, n, 0.12))
    pan_add(out, norm(murmur), 0.045, 0.55)
    # A tool set down on a bench, once, near.
    pan_add(out, ping(t, 9.6, 2100, 0.04) + 0.6 * ping(t, 9.62, 3150, 0.03), 0.03, 0.3)
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    signal = continuance()
    path = BEDS / "AMB_Continuance.ogg"
    write_ogg(path, signal)
    mono = signal.mean(axis=1)
    rms = float(np.sqrt(np.mean(mono ** 2)))
    print(f"AMB_Continuance rms={rms:.4f} volume={TARGET / max(1e-9, rms):.3f} seconds={len(mono) / RATE:.1f}")
    print("CONTINUANCE_AMBIENCE_OK")
