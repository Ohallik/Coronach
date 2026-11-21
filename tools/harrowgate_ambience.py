"""Synthesise chapter 8's ambience beds (Harrowgate, the Vaskar Reach): the Picket of Teeth, the Scoria Flows, the Bridle Foundry and the town.

    python tools/harrowgate_ambience.py      # writes AMB_PicketOfTeeth.ogg, AMB_ScoriaFlows.ogg, AMB_BridleFoundry.ogg, AMB_Harrowgate.ogg to Lattice/Assets/_Project/Audio/Ambience

Each is a 24 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as tools/wendmire_ambience.py
does), so the files are ours; nothing is sourced. Nobody can listen to them, so each is measured instead: the script prints every bed's RMS
(mixed to one channel) and the ZoneDef volume that plays it at the level the earlier beds play at (file RMS times volume about 0.0224,
-33 dB). HarrowgateBuilder holds those volumes as constants beside the paths. Needs numpy and imageio-ffmpeg. Ends with HARROWGATE_AMBIENCE_OK.

- The Picket: no air, so no wind and no water: the carrier's drives far off, a slow beat between two low hums; the picket's buoys pinging,
  a soft rising sweep now and then, answered from somewhere else; a relay ticking; once, a far flak test thumping in a row.
- The Flows: a deep rumble under everything (the melt moving under the crust); crust ticking and crackling as it cools; steam hissing out
  of a seam; a slab giving way somewhere off with a crack and a slump; ash-laden wind in gusts.
- The Foundry: the furnace's roar and a machine thrum under the floor; hammers far off in the casting gallery, never quite in time;
  chain running through a block; a coolant valve venting; a pour's low glug.
- Harrowgate: harbour water against basalt; the town's murmur; a drill sergeant's call across the field, too far to make out; the
  armourers' hammers, few of them; a bell from the oath hall once; the wind off the sea.
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


def picket():
    rng = np.random.default_rng(1801)
    n = int(SECONDS * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        floor = band(pink(rng, n), 40, 180) * (0.6 + 0.4 * slow(rng, n, 0.06))
        out[:, side] = norm(floor) * 0.45
    # The carrier's two drives, a little apart in pitch, beating slowly.
    drone = np.sin(2 * np.pi * 52 * t) + 0.8 * np.sin(2 * np.pi * 52.7 * t) + 0.3 * np.sin(2 * np.pi * 104.5 * t)
    pan_add(out, norm(drone) * (0.6 + 0.4 * slow(rng, n, 0.05)), 0.16, 0.7)
    # The buoys: a ping, and every so often a soft sweep rising through it; another buoy answers from the other side.
    for at, pan, hertz in ((1.2, 0.2, 1480), (5.9, 0.8, 1320), (9.6, 0.25, 1480), (14.3, 0.75, 1320), (18.8, 0.3, 1480), (22.7, 0.7, 1320)):
        pan_add(out, ping(t, at, hertz, 0.35) + 0.4 * ping(t, at + 0.12, hertz * 1.5, 0.2), 0.05, pan)
    for at, pan in ((3.4, 0.4), (16.2, 0.6)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + 1.6)
        sweep = np.sin(2 * np.pi * (600 * local + 380 * local * local)) * np.sin(np.pi * np.clip(local / 1.6, 0, 1)) ** 2 * live
        pan_add(out, sweep, 0.03, pan)
    # A relay ticking.
    for k in range(int(SECONDS / 0.8)):
        pan_add(out, ping(t, 0.3 + k * 0.8, 3200, 0.012), 0.025, 0.55)
    # Far off, a flak line tested: five thumps in a row.
    for k in range(5):
        thump = band(rng.standard_normal(n), 60, 260) * np.where(t >= 11.0 + k * 0.28, np.exp(-np.clip(t - 11.0 - k * 0.28, 0, None) / 0.18), 0)
        pan_add(out, norm(thump), 0.1, 0.85)
    return level(seamless(out, 2.0), 0.5)


def scoria():
    rng = np.random.default_rng(1802)
    n = int(SECONDS * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        rumble = band(pink(rng, n), 25, 120) * (0.6 + 0.4 * slow(rng, n, 0.1))
        wind = band(pink(rng, n), 350, 2400) * slow(rng, n, 0.18) ** 2
        out[:, side] = norm(rumble) * 0.55 + norm(wind) * 0.22
    # Crust cooling: small ticks and crackles scattered over the loop.
    for k in range(70):
        at = rng.random() * SECONDS
        tick = band(rng.standard_normal(n), 2500, 7000) * np.where(t >= at, np.exp(-np.clip(t - at, 0, None) / 0.008), 0)
        pan_add(out, norm(tick), 0.05 + 0.05 * rng.random(), rng.random())
    # Steam hissing out of a seam, swelling and falling.
    for at, pan, length in ((4.2, 0.3, 2.4), (13.8, 0.75, 3.0), (20.5, 0.5, 1.8)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + length)
        hiss = band(rng.standard_normal(n), 3000, 9000) * np.sin(np.pi * np.clip(local / length, 0, 1)) ** 1.5 * live
        pan_add(out, norm(hiss), 0.1, pan)
    # A slab giving way somewhere off: a crack, then a heavy slump.
    for at, pan in ((8.6, 0.8), (17.9, 0.2)):
        crack = band(rng.standard_normal(n), 900, 4000) * np.where(t >= at, np.exp(-np.clip(t - at, 0, None) / 0.03), 0)
        slump = band(rng.standard_normal(n), 40, 200) * np.where(t >= at + 0.1, np.exp(-np.clip(t - at - 0.1, 0, None) / 0.5), 0)
        pan_add(out, norm(crack), 0.12, pan)
        pan_add(out, norm(slump), 0.2, pan)
    return level(seamless(out, 2.0), 0.5)


def foundry():
    rng = np.random.default_rng(1803)
    n = int(SECONDS * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        roar = band(pink(rng, n), 70, 700) * (0.7 + 0.3 * slow(rng, n, 0.3))
        out[:, side] = norm(roar) * 0.5
    thrum = np.sin(2 * np.pi * 55 * t) + 0.5 * np.sin(2 * np.pi * 110 * t) + 0.25 * np.sin(2 * np.pi * 165 * t)
    pan_add(out, norm(thrum) * (0.85 + 0.15 * np.sin(2 * np.pi * 0.5 * t)), 0.14, 0.5)
    # Hammers far off in the gallery: two smiths, never quite in time.
    for start, step, pan, hertz in ((0.4, 1.3, 0.25, 820), (0.9, 1.45, 0.7, 960)):
        at = start
        while at < SECONDS - 0.5:
            strike = ping(t, at, hertz, 0.08) + 0.5 * ping(t, at, hertz * 2.76, 0.04) + 0.3 * ping(t, at, hertz * 5.4, 0.02)
            pan_add(out, strike, 0.035, pan)
            at += step + 0.08 * rng.random()
    # Chain running through a block.
    for k in range(16):
        at = 6.0 + k * 0.11
        pan_add(out, ping(t, at, 1900 + 300 * rng.random(), 0.02) + 0.6 * ping(t, at, 3100, 0.015), 0.03, 0.4)
    # A coolant valve venting.
    for at, pan in ((11.5, 0.65), (21.0, 0.3)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + 2.2)
        vent = band(rng.standard_normal(n), 2000, 8000) * np.exp(-local / 0.9) * live
        pan_add(out, norm(vent), 0.12, pan)
    # A pour's low glug.
    for k in range(5):
        pan_add(out, ping(t, 15.2 + k * 0.22, 90 + 25 * rng.random(), 0.12, -1.5), 0.12, 0.5)
    return level(seamless(out, 2.0), 0.5)


def harrowgate():
    rng = np.random.default_rng(1804)
    n = int(SECONDS * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        wash = band(pink(rng, n), 90, 480) * (0.6 + 0.4 * slow(rng, n, 0.2))
        slap = band(rng.standard_normal(n), 600, 2400) * slow(rng, n, 1.4) ** 3
        murmur = band(pink(rng, n), 220, 900) * slow(rng, n, 2.0) ** 2 * (0.4 + 0.6 * slow(rng, n, 0.12))
        wind = band(pink(rng, n), 400, 2600) * slow(rng, n, 0.15) ** 2
        out[:, side] = norm(wash) * 0.42 + norm(slap) * 0.22 + norm(murmur) * 0.2 + norm(wind) * 0.12
    # A drill call across the field, too far to make out: a held vowel, then two short.
    for at, pan in ((5.1, 0.15), (18.4, 0.2)):
        for k, (offset, length) in enumerate(((0.0, 0.7), (0.9, 0.18), (1.2, 0.18))):
            local = np.clip(t - at - offset, 0, None)
            live = (t >= at + offset) & (t < at + offset + length)
            voice = np.sin(2 * np.pi * (180 - 10 * k) * local) * (0.6 + 0.4 * np.sin(2 * np.pi * 6 * local)) * np.sin(np.pi * np.clip(local / length, 0, 1)) * live
            pan_add(out, band(voice, 150, 1800), 0.06, pan)
    # The armourers' hammers: few of them.
    for at in (2.3, 3.6, 4.9, 13.0, 14.4):
        pan_add(out, ping(t, at, 1040, 0.07) + 0.4 * ping(t, at, 2870, 0.03), 0.03, 0.8)
    # The oath hall's bell, once.
    bell = ping(t, 9.4, 196, 2.6) + 0.6 * ping(t, 9.4, 392.5, 1.8) + 0.3 * ping(t, 9.4, 588, 1.2) + 0.2 * ping(t, 9.4, 470, 1.4)
    pan_add(out, bell, 0.05, 0.6)
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    for name, make in (("AMB_PicketOfTeeth", picket), ("AMB_ScoriaFlows", scoria), ("AMB_BridleFoundry", foundry), ("AMB_Harrowgate", harrowgate)):
        signal = make()
        rms = float(np.sqrt(np.mean(signal.mean(axis=1) ** 2)))
        write_ogg(BEDS / (name + ".ogg"), signal)
        print(f"{name} rms={rms:.4f} volume={TARGET / rms:.3f}")
    print("HARROWGATE_AMBIENCE_OK", BEDS)
