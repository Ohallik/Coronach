"""Synthesise chapter 4's ambience beds (Wendmire): the raft town, the mire, and the spore veil above them.

    python tools/wendmire_ambience.py        # writes AMB_Driftreed.ogg, AMB_Mire.ogg, AMB_SporeVeil.ogg to Lattice/Assets/_Project/Audio/Ambience

Each is a 24 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as chapter 3's
tools/xylos_ambience.py does), so the files are ours; nothing is sourced. Nobody can listen to them, so each is measured instead: the
script prints every bed's RMS and the ZoneDef volume that plays it at the level the earlier beds play at (file RMS times volume about
0.0224, -33 dB: Lanternwick's city .1024 at .219, the gorge's wind .2117 at .106, Rootmarket's .0634 at .353). WendmireBuilder holds those
volumes as constants beside the paths. Needs numpy and imageio-ffmpeg. Ends with WENDMIRE_AMBIENCE_OK.

- Driftreed: still water lapping under woven rafts, lashings creaking as the rafts ride, a raft knocking on its neighbour, a far murmur of
  voices, a child's two-short-one-long clapped somewhere across the water, frogs at the edge, wind combing the reeds.
- The mire: the same water with no town on it; more frogs and slower ones, a heavy bubbling where gas comes up through the peat, a low
  hooting call far off, wind in the mangroves, insects.
- The spore veil: no air, so no wind and no water: a high glassy shimmer of spores catching light, slow swells under it, and now and then
  a soft chime as a cluster of spores bursts somewhere off the lane.
"""
from pathlib import Path
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import BEDS, RATE, band, insects, level, ping, pink, seamless, slow, write_ogg  # noqa: E402

TARGET = 0.0224   # file RMS x ZoneDef volume, as every earlier bed plays


def pan_add(out, sound, gain, pan):
    out[:, 0] += sound * gain * (1 - pan)
    out[:, 1] += sound * gain * pan


def frogs(out, rng, t, times, low, high, gain, slow_croak=False):
    for at in times:
        hertz = low + (high - low) * rng.random()
        rate = 38 if slow_croak else 60
        croak = ping(t, at, hertz, 0.09 if slow_croak else 0.06, 0.5) * (0.5 + 0.5 * np.sin(2 * np.pi * rate * t))
        pan_add(out, croak, gain, rng.random())


def reeds(rng, n):
    """Wind combing reed beds: a breathy band that swells and falls."""
    return band(pink(rng, n), 1400, 5200) * slow(rng, n, 0.22) ** 2


def driftreed():
    rng = np.random.default_rng(1401)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        wash = band(pink(rng, n), 110, 520) * (0.6 + 0.4 * slow(rng, n, 0.28))
        lap = band(rng.standard_normal(n), 800, 2900) * slow(rng, n, 1.8) ** 3
        murmur = band(pink(rng, n), 230, 880) * slow(rng, n, 2.1) ** 2 * (0.4 + 0.6 * slow(rng, n, 0.14))
        wind = reeds(rng, n)
        out[:, side] = wash / np.max(np.abs(wash)) * 0.5 + lap / np.max(np.abs(lap)) * 0.3 + murmur / np.max(np.abs(murmur)) * 0.18 + wind / np.max(np.abs(wind)) * 0.12
    out += insects(rng, n, t, 8, 3200, 7000, 0.3)
    # Lashings creaking as the rafts ride: a low wavering creak, three times.
    for at, pan in ((3.1, 0.3), (11.6, 0.7), (19.0, 0.45)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + 1.1)
        creak = np.sin(2 * np.pi * (120 + 30 * np.sin(2 * np.pi * 4 * local)) * local) * np.sin(np.pi * np.clip(local / 1.1, 0, 1)) ** 2 * live
        creak *= 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 31 * local))
        pan_add(out, creak, 0.07, pan)
    # A raft's edge knocking on its neighbour's, hollow, in pairs.
    for at, pan in ((6.2, 0.6), (6.55, 0.6), (15.4, 0.25), (15.7, 0.25)):
        knock = ping(t, at, 260, 0.05, 0.4) + 0.4 * ping(t, at, 640, 0.025, 0.4)
        pan_add(out, knock, 0.07, pan)
    # Across the water, a child clapping the town's count: two short, one long (the long a clap and a held "ah" of voices under it).
    for start in (9.0, 20.2):
        for k, at in enumerate((0.0, 0.32, 0.72)):
            clap = band(rng.standard_normal(n), 900, 3600) * np.where(t >= start + at, np.exp(-np.clip(t - start - at, 0, None) / (0.05 if k < 2 else 0.12)), 0)
            pan_add(out, clap / np.max(np.abs(clap)), 0.05, 0.78)
    frogs(out, rng, t, (2.2, 2.5, 13.3, 22.0), 300, 420, 0.1)
    return level(seamless(out, 2.0), 0.5)


def mire():
    rng = np.random.default_rng(1402)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        still = band(pink(rng, n), 80, 420) * (0.7 + 0.3 * slow(rng, n, 0.2))
        lap = band(rng.standard_normal(n), 700, 2400) * slow(rng, n, 1.2) ** 4
        wind = band(pink(rng, n), 300, 1600) * slow(rng, n, 0.12) ** 2
        out[:, side] = still / np.max(np.abs(still)) * 0.6 + lap / np.max(np.abs(lap)) * 0.2 + wind / np.max(np.abs(wind)) * 0.22
    out += insects(rng, n, t, 12, 3000, 6800, 0.42)
    frogs(out, rng, t, (1.4, 1.7, 2.0, 7.9, 12.2, 12.5, 17.6, 21.1, 21.4), 180, 300, 0.16, slow_croak=True)
    # Gas coming up through the peat: a heavy, slow bubbling, a few gulps at a time.
    for at, pan in ((4.6, 0.35), (14.9, 0.7), (18.8, 0.2)):
        for k in range(4):
            gulp = ping(t, at + k * 0.19 + 0.05 * rng.random(), 140 + 60 * rng.random(), 0.06, -2.5)
            pan_add(out, gulp, 0.14, pan)
    # Something far off calling low across the water: a hoot that falls, twice, long apart.
    for at, pan in ((5.8, 0.85), (16.2, 0.1)):
        hoot = ping(t, at, 310, 0.45, -0.35) * (0.6 + 0.4 * np.sin(2 * np.pi * 5 * t))
        pan_add(out, hoot, 0.06, pan)
    return level(seamless(out, 2.0), 0.5)


def spore_veil():
    rng = np.random.default_rng(1403)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        swell = band(pink(rng, n), 60, 240) * (0.5 + 0.5 * slow(rng, n, 0.08))
        shimmer = band(rng.standard_normal(n), 5200, 9800) * (0.4 + 0.6 * slow(rng, n, 0.6) ** 2)
        out[:, side] = swell / np.max(np.abs(swell)) * 0.55 + shimmer / np.max(np.abs(shimmer)) * 0.12
    # Glassy partials that drift in and out: the spores catching the light.
    for hertz in (1320, 1980, 2640, 3300):
        tone = np.sin(2 * np.pi * hertz * t + rng.random() * 6) * np.clip(np.sin(2 * np.pi * rng.integers(2, 6) * t / t[-1] + rng.random() * 6), 0, 1) ** 3
        pan_add(out, tone, 0.035, rng.random())
    # A cluster of spores bursting off the lane: a soft chime.
    for at, pan in ((3.7, 0.3), (10.4, 0.8), (17.1, 0.55), (21.6, 0.15)):
        chime = ping(t, at, 1750 + 600 * rng.random(), 0.6, -0.1) + 0.5 * ping(t, at, 2630 + 400 * rng.random(), 0.4, -0.1)
        pan_add(out, chime, 0.05, pan)
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    for name, make in (("AMB_Driftreed", driftreed), ("AMB_Mire", mire), ("AMB_SporeVeil", spore_veil)):
        signal = make()
        # Measured as chapter 3 measured its beds: the file mixed down to one channel (Rootmarket's .0634 is that figure).
        rms = float(np.sqrt(np.mean(signal.mean(axis=1) ** 2)))
        write_ogg(BEDS / (name + ".ogg"), signal)
        print(f"{name} rms={rms:.4f} volume={TARGET / rms:.3f}")
    print("WENDMIRE_AMBIENCE_OK", BEDS)
