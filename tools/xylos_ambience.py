"""Synthesise Rootmarket's ambience bed (chapter 3): a river town on root platforms, heard from its landing pool.

    python tools/xylos_ambience.py        # writes Lattice/Assets/_Project/Audio/Ambience/AMB_RiverTown.ogg

A 24 s seamless stereo loop: slack water lapping at root rims (no current: the river's own run is AMB_BraidedRiver's), insects in
the heat, two kinds of bird calling across the pool now and then, a mooring line creaking, a far knock of timber, and under it all
the low murmur of a town that talks a great deal. Nothing is sourced: every sample is made here from seeded noise and sines, with
the synthesis helpers of tools/water_sounds.py, so the file is ours. XylosBuilder sets it on the Rootmarket ZoneDef when it exists.
Needs numpy and imageio-ffmpeg. Ends with XYLOS_AMBIENCE_OK.
"""
from pathlib import Path
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import BEDS, RATE, band, insects, level, ping, pink, seamless, slow, write_ogg  # noqa: E402


def river_town():
    rng = np.random.default_rng(1203)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        # Slack water: a soft low wash that breathes, and small laps against the rims that come and go.
        wash = band(pink(rng, n), 120, 600) * (0.6 + 0.4 * slow(rng, n, 0.3))
        lap = band(rng.standard_normal(n), 900, 3200) * slow(rng, n, 1.6) ** 3
        out[:, side] = wash / np.max(np.abs(wash)) * 0.5 + lap / np.max(np.abs(lap)) * 0.3
        # The town itself: voices too far off to be words, swelling and falling.
        murmur = band(pink(rng, n), 220, 900) * slow(rng, n, 2.2) ** 2 * (0.4 + 0.6 * slow(rng, n, 0.15))
        out[:, side] += murmur / np.max(np.abs(murmur)) * 0.2
    out += insects(rng, n, t, 12, 3400, 7600, 0.42)
    # A bird that whistles two rising notes, and one that answers with three quick falling ones, well apart and never together.
    for at, pan in ((2.6, 0.2), (11.8, 0.75), (19.4, 0.3)):
        for k, (hertz, fall) in enumerate(((1750, -0.9), (2150, -0.9))):
            call = ping(t, at + k * 0.28, hertz, 0.11, fall)
            out[:, 0] += call * 0.09 * (1 - pan)
            out[:, 1] += call * 0.09 * pan
    for at, pan in ((6.9, 0.8), (15.3, 0.15)):
        for k in range(3):
            call = ping(t, at + k * 0.16, 2900 - k * 260, 0.05, 1.4)
            out[:, 0] += call * 0.07 * (1 - pan)
            out[:, 1] += call * 0.07 * pan
    # A mooring line taking the strain: a low creak that wavers, twice in the loop.
    for at in (8.4, 21.6):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + 1.3)
        creak = np.sin(2 * np.pi * (140 + 26 * np.sin(2 * np.pi * 5 * local)) * local) * np.sin(np.pi * np.clip(local / 1.3, 0, 1)) ** 2 * live
        creak *= 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 37 * local))
        out[:, 0] += creak * 0.05
        out[:, 1] += creak * 0.08
    # Timber knocked on timber, somewhere across the pool.
    for at, pan in ((4.9, 0.6), (5.25, 0.6), (13.1, 0.35), (17.9, 0.7), (18.2, 0.7), (18.5, 0.7)):
        knock = ping(t, at, 420, 0.035, 0.5) + 0.5 * ping(t, at, 980, 0.02, 0.5)
        out[:, 0] += knock * 0.06 * (1 - pan)
        out[:, 1] += knock * 0.06 * pan
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    write_ogg(BEDS / "AMB_RiverTown.ogg", river_town())
    print("XYLOS_AMBIENCE_OK", BEDS / "AMB_RiverTown.ogg")
