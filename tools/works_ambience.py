"""Synthesise chapter 9's Separation Works bed: the keel of Cinder Halo under the promenade on separation day.

    python tools/works_ambience.py      # writes AMB_SeparationWorks.ogg to Lattice/Assets/_Project/Audio/Ambience

A 26 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as tools/harrowgate_ambience.py
does), so the file is ours; nothing is sourced. Nobody can listen to it, so it is measured instead: the script prints the bed's RMS (mixed
to one channel) and the ZoneDef volume that plays it at the level the earlier beds play at (file RMS times volume about 0.0224, -33 dB).
HomeBuilder.Works holds that volume as a constant beside the path (Works.BedLevel). Needs numpy and imageio-ffmpeg. Ends with WORKS_AMBIENCE_OK.

- The keel: a low hollow air in a long iron space, the plant's ventilation fans turning slow and a little out of step with each other.
- The station under strain: hull plates groaning now and then as the collars take weight, a long creak that sags in pitch, a rivet ticking.
- The town overhead through the deck grating: crates being dragged and set down, footfalls in a crowd, a murmur too far to make out.
- The plant: a chain run through a block once, a drip somewhere, and the faint high whine of a Compact rig on a cable, coming and going.
"""
from pathlib import Path
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import BEDS, RATE, band, level, ping, pink, seamless, slow, write_ogg  # noqa: E402

TARGET = 0.0224   # file RMS x ZoneDef volume, as every earlier bed plays
SECONDS = 26


def pan_add(out, sound, gain, pan):
    out[:, 0] += sound * gain * (1 - pan)
    out[:, 1] += sound * gain * pan


def norm(x):
    return x / max(1e-9, np.max(np.abs(x)))


def works():
    rng = np.random.default_rng(1901)
    n = int(SECONDS * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        # The keel's hollow air: low band-limited noise, swelling slowly as if the space breathed.
        hollow = band(pink(rng, n), 45, 260) * (0.65 + 0.35 * slow(rng, n, 0.07))
        out[:, side] = norm(hollow) * 0.5
    # The fans: two slow drones a little apart, each with its blade rate as a soft flutter.
    for hertz, flutter, pan in ((61.0, 5.4, 0.3), (63.2, 6.1, 0.7)):
        fan = np.sin(2 * np.pi * hertz * t) + 0.35 * np.sin(2 * np.pi * hertz * 2.01 * t)
        fan *= 0.75 + 0.25 * np.sin(2 * np.pi * flutter * t) ** 2
        pan_add(out, norm(fan), 0.07, pan)
    # The plates groaning: a long low creak, its pitch sagging, three times in the loop on different sides.
    for at, pan, hertz, length in ((3.1, 0.25, 92, 2.6), (11.8, 0.7, 78, 3.2), (19.6, 0.45, 104, 2.2)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + length)
        env = np.sin(np.pi * np.clip(local / length, 0, 1)) ** 1.5 * live
        creak = np.sin(2 * np.pi * (hertz * local - 6 * local * local)) * (0.7 + 0.3 * band(rng.standard_normal(n), 8, 30) * 4)
        grit = band(rng.standard_normal(n), 300, 1400) * slow(rng, n, 12) ** 2
        pan_add(out, norm(creak * env) * 0.8 + norm(grit * env) * 0.3, 0.16, pan)
    # A rivet ticking as the iron settles after each groan.
    for start in (5.9, 15.2, 22.0):
        for k in range(5):
            at = start + k * (0.18 + 0.05 * rng.random())
            pan_add(out, ping(t, at, 2300 + 400 * rng.random(), 0.012), 0.05, 0.3 + 0.4 * rng.random())
    # Overhead through the grating: crates dragged and set down, muffled (nothing over 900 Hz reaches the keel).
    for at, pan in ((1.6, 0.6), (7.4, 0.35), (9.9, 0.55), (14.6, 0.4), (17.9, 0.65), (23.3, 0.5)):
        local = np.clip(t - at, 0, None)
        drag = band(rng.standard_normal(n), 60, 700) * ((t >= at) & (t < at + 0.9)) * np.sin(np.pi * np.clip(local / 0.9, 0, 1))
        thud = ping(t, at + 0.95, 55 + 20 * rng.random(), 0.09) + 0.6 * band(rng.standard_normal(n), 40, 300) * np.exp(-np.clip(t - at - 0.95, 0, None) / 0.05) * (t >= at + 0.95)
        pan_add(out, norm(drag) * 0.5 + norm(thud) * 0.9, 0.11, pan)
    # Footfalls of a crowd overhead and their murmur, far and low.
    steps = np.zeros(n)
    at = 0.2
    while at < SECONDS - 0.3:
        steps += ping(t, at, 70 + 30 * rng.random(), 0.03) * (0.4 + 0.6 * rng.random())
        at += 0.22 + 0.25 * rng.random()
    murmur = band(pink(rng, n), 180, 700) * slow(rng, n, 1.6) ** 2 * (0.5 + 0.5 * slow(rng, n, 0.1))
    pan_add(out, norm(steps), 0.05, 0.5)
    pan_add(out, norm(murmur), 0.06, 0.45)
    # A chain run through a block, once.
    for k in range(14):
        at = 12.6 + k * 0.1
        pan_add(out, ping(t, at, 1700 + 300 * rng.random(), 0.02) + 0.5 * ping(t, at, 2900, 0.012), 0.025, 0.75)
    # A drip into standing water in the bilge, now and then.
    for at in (4.4, 10.3, 16.7, 21.4, 25.0):
        pan_add(out, ping(t, at, 1100 + 300 * rng.random(), 0.05, -2.0), 0.03, 0.2 + 0.6 * rng.random())
    # The rig's faint whine on its cable, coming and going: the Compact's, high and thin.
    whine = np.sin(2 * np.pi * 3520 * t + 0.4 * np.sin(2 * np.pi * 0.7 * t)) * slow(rng, n, 0.12) ** 3
    pan_add(out, whine, 0.008, 0.6)
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    signal = works()
    path = BEDS / "AMB_SeparationWorks.ogg"
    write_ogg(path, signal)
    mono = signal.mean(axis=1)
    rms = float(np.sqrt(np.mean(mono ** 2)))
    print(f"AMB_SeparationWorks rms={rms:.4f} volume={TARGET / max(1e-9, rms):.3f} seconds={len(mono) / RATE:.1f}")
    print("WORKS_AMBIENCE_OK")
