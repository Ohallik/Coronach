"""Synthesise chapter 5's ambience bed (Many Hands): the ship the convoy became, heard from its deck.

    python tools/manyhands_ambience.py        # writes AMB_ManyHands.ogg to Lattice/Assets/_Project/Audio/Ambience

A 26 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as tools/wendmire_ambience.py
does), so the file is ours; nothing is sourced. Nobody can listen to it, so it is measured instead: the script prints the bed's RMS and the
ZoneDef volume that plays it at the level the earlier beds play at (file RMS times volume about 0.0224, -33 dB). ManyHandsBuilder holds that
volume as a constant beside the path (CityLevel). Needs numpy and imageio-ffmpeg. Ends with MANYHANDS_AMBIENCE_OK.

- Under everything, the ship: a low hum that beats slowly where two hulls' machinery runs at nearly the same speed, and the air handlers'
  hiss, soft and wide.
- People: a crowd's murmur, nearer and busier than Driftreed's, rising and falling; now and then a laugh.
- The freight spine: a container set down, a chain running out on the gantry, a trolley's wheels over a plate seam.
- The galley: pots knocking, a ladle on a rim, a kettle coming to the boil once and taken off.
- The raft bay: water lapping in the hold, faint.
- The arcade clock's two notes, once in the loop.
"""
from pathlib import Path
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import BEDS, RATE, band, level, ping, pink, seamless, slow, write_ogg  # noqa: E402

TARGET = 0.0224   # file RMS x ZoneDef volume, as every earlier bed plays


def pan_add(out, sound, gain, pan):
    out[:, 0] += sound * gain * (1 - pan)
    out[:, 1] += sound * gain * pan


def norm(x):
    peak = np.max(np.abs(x))
    return x / peak if peak > 0 else x


def many_hands():
    rng = np.random.default_rng(1501)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        # Two hulls' machinery a little out of step: 46 and 47.3 Hz beat about once a second; their second harmonics quieter.
        hum = np.sin(2 * np.pi * 46 * t + side) + 0.8 * np.sin(2 * np.pi * 47.3 * t) + 0.25 * np.sin(2 * np.pi * 92 * t) + 0.15 * np.sin(2 * np.pi * 141 * t + side)
        air = band(pink(rng, n), 900, 5200) * (0.75 + 0.25 * slow(rng, n, 0.2))
        crowd = band(pink(rng, n), 260, 1300) * slow(rng, n, 1.7) ** 2 * (0.45 + 0.55 * slow(rng, n, 0.11))
        lap = band(rng.standard_normal(n), 700, 2400) * slow(rng, n, 1.5) ** 3
        out[:, side] = norm(hum) * 0.2 + norm(air) * 0.22 + norm(crowd) * 0.32 + norm(lap) * 0.07
    # Laughs: a short rising burst of voice-band noise, here and there.
    for at, pan in ((4.4, 0.3), (17.8, 0.7)):
        laugh = band(rng.standard_normal(n), 400, 1800) * np.where((t >= at) & (t < at + 0.7), np.abs(np.sin(2 * np.pi * 6.5 * np.clip(t - at, 0, None))), 0)
        pan_add(out, norm(laugh), 0.06, pan)
    # The freight spine: a container set down (a deep clang with a long ring), a chain running out, a trolley over a seam.
    for at, pan in ((2.0, 0.55), (14.6, 0.4)):
        clang = ping(t, at, 82, 0.9, 0.2) + 0.6 * ping(t, at, 215, 0.5, 0.2) + 0.3 * ping(t, at, 531, 0.25, 0.2)
        pan_add(out, clang, 0.09, pan)
    chain_at = 8.1
    chain = band(rng.standard_normal(n), 1800, 6000) * np.where((t >= chain_at) & (t < chain_at + 1.6), 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 14 * (t - chain_at))), 0)
    pan_add(out, norm(chain), 0.05, 0.62)
    for k in range(6):
        pan_add(out, ping(t, 21.0 + k * 0.42, 340, 0.04, 0.3), 0.05, 0.35 + k * 0.05)
    # The galley: pots knocking, a ladle on a rim, and a kettle that comes to the boil and is taken off.
    for at, hertz in ((5.3, 620), (5.7, 780), (12.2, 1240), (12.35, 980), (23.4, 700)):
        pan_add(out, ping(t, at, hertz, 0.12, 0.1), 0.05, 0.82)
    kettle_at = 15.0
    rise = np.clip((t - kettle_at) / 3.0, 0, 1)
    whistle = np.sin(2 * np.pi * (1800 + 600 * rise) * t) * np.where((t >= kettle_at) & (t < kettle_at + 3.6), rise * np.clip((kettle_at + 3.6 - t) / 0.4, 0, 1), 0)
    pan_add(out, whistle, 0.02, 0.86)
    # The arcade clock's two notes, a fourth apart, ringing out.
    pan_add(out, ping(t, 10.5, 523, 1.4, 0.1) + 0.5 * ping(t, 10.5, 1046, 0.8, 0.1), 0.05, 0.5)
    pan_add(out, ping(t, 11.3, 392, 1.6, 0.1) + 0.5 * ping(t, 11.3, 784, 0.9, 0.1), 0.05, 0.5)
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    signal = many_hands()
    rms = float(np.sqrt(np.mean(signal.mean(axis=1) ** 2)))
    write_ogg(BEDS / "AMB_ManyHands.ogg", signal)
    print(f"AMB_ManyHands rms={rms:.4f} volume={TARGET / rms:.3f}")
    print("MANYHANDS_AMBIENCE_OK", BEDS)
