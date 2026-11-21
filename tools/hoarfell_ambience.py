"""Synthesise chapter 6's ambience beds (Hoarfell): the herders' town inside the ribs, and the aurora's lane through the ice ring.

    python tools/hoarfell_ambience.py        # writes AMB_Lastlight.ogg, AMB_AuroraShear.ogg to Lattice/Assets/_Project/Audio/Ambience

Each is a 26 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as chapter 4's
tools/wendmire_ambience.py does), so the files are ours; nothing is sourced. Nobody can listen to them, so each is measured instead: the
script prints every bed's RMS (the file mixed to one channel) and the ZoneDef volume that plays it at the level the earlier beds play at
(file RMS times volume about 0.0224, -33 dB). HoarfellBuilder holds those volumes as constants beside the paths. Needs numpy and
imageio-ffmpeg. Ends with HOARFELL_AMBIENCE_OK.

- Lastlight: a cold wind over the ribs, low and steady, whistling now and then through the bone; under it the vents' warm hiss and their
  slow gurgle; beasts lowing in the pens, a herder's low hum answering them; bone knocking on bone in the carving yard; a far creak of the
  great ribs settling in the cold.
- The Aurora Shear: no air, so no wind: a deep slow drone of the storm, the aurora's crackle and its high singing partials sliding up and
  down, and now and then ice in the ring grinding and cracking far off.
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


def lastlight():
    rng = np.random.default_rng(1601)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        wind = band(pink(rng, n), 180, 900) * (0.55 + 0.45 * slow(rng, n, 0.09))
        gust = band(pink(rng, n), 600, 2600) * slow(rng, n, 0.21) ** 3
        hiss = band(rng.standard_normal(n), 2400, 7800) * (0.5 + 0.5 * slow(rng, n, 0.4))
        out[:, side] = wind / np.max(np.abs(wind)) * 0.55 + gust / np.max(np.abs(gust)) * 0.18 + hiss / np.max(np.abs(hiss)) * 0.08
    # Wind whistling through the ribs: a breathy tone that rises and falls, twice.
    for at, length, hertz, pan in ((2.4, 4.5, 640, 0.25), (13.8, 5.2, 520, 0.75)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + length)
        env = np.sin(np.pi * np.clip(local / length, 0, 1)) ** 2 * live
        whistle = band(rng.standard_normal(n), hertz * 0.96, hertz * 1.04, soft=0.05) * env
        pan_add(out, whistle / max(np.max(np.abs(whistle)), 1e-6), 0.07, pan)
    # The vents: a slow gurgle under the hiss.
    for at, pan in ((5.2, 0.55), (11.1, 0.4), (19.6, 0.6)):
        for k in range(3):
            gulp = ping(t, at + k * 0.23 + 0.06 * rng.random(), 120 + 50 * rng.random(), 0.07, -2.0)
            pan_add(out, gulp, 0.08, pan)
    # Beasts lowing in the pens, far to one side; a herder's low hum answering, steady.
    for at, hertz, pan in ((3.6, 96, 0.12), (9.4, 88, 0.18), (17.2, 104, 0.1), (22.6, 92, 0.15)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + 1.6)
        low = np.sin(2 * np.pi * (hertz + 14 * np.sin(np.pi * np.clip(local / 1.6, 0, 1))) * local) * np.sin(np.pi * np.clip(local / 1.6, 0, 1)) ** 1.5 * live
        low += 0.35 * np.sin(2 * np.pi * 2 * hertz * local) * np.sin(np.pi * np.clip(local / 1.6, 0, 1)) ** 2 * live
        pan_add(out, low, 0.07, pan)
    hum_at, hum_len = 10.6, 4.4
    local = np.clip(t - hum_at, 0, None)
    live = (t >= hum_at) & (t < hum_at + hum_len)
    hum = (np.sin(2 * np.pi * 146 * local) + 0.4 * np.sin(2 * np.pi * 292 * local)) * np.sin(np.pi * np.clip(local / hum_len, 0, 1)) ** 2 * live
    hum *= 0.7 + 0.3 * np.sin(2 * np.pi * 0.8 * local)
    pan_add(out, hum, 0.04, 0.2)
    # Bone on bone in the carving yard: a dry knock, in twos and threes.
    for start, pan in ((6.8, 0.35), (15.2, 0.4), (23.4, 0.3)):
        for k in range(2 + int(rng.random() * 2)):
            knock = ping(t, start + k * 0.42, 820 + 120 * rng.random(), 0.03, 0.3) + 0.5 * ping(t, start + k * 0.42, 1900, 0.015, 0.3)
            pan_add(out, knock, 0.06, pan)
    # The great ribs settling in the cold: a low creak, far off.
    for at, pan in ((8.2, 0.7), (20.4, 0.85)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + 1.8)
        creak = np.sin(2 * np.pi * (70 + 18 * np.sin(2 * np.pi * 3 * local)) * local) * np.sin(np.pi * np.clip(local / 1.8, 0, 1)) ** 2 * live
        creak *= 0.6 + 0.4 * np.sign(np.sin(2 * np.pi * 23 * local))
        pan_add(out, creak, 0.06, pan)
    return level(seamless(out, 2.0), 0.5)


def aurora_shear():
    rng = np.random.default_rng(1602)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        drone = band(pink(rng, n), 40, 180) * (0.55 + 0.45 * slow(rng, n, 0.07))
        crackle = band(rng.standard_normal(n), 3000, 9000) * (rng.random(n) > 0.9965) * 6
        crackle = band(crackle, 2000, 9000) * (0.4 + 0.6 * slow(rng, n, 0.3))
        out[:, side] = drone / np.max(np.abs(drone)) * 0.6 + crackle / max(np.max(np.abs(crackle)), 1e-6) * 0.1
    # The aurora singing: partials sliding slowly up and down, in and out.
    for hertz in (880, 1320, 1760, 2420):
        glide = hertz * (1 + 0.04 * np.sin(2 * np.pi * (0.03 + 0.02 * rng.random()) * t + rng.random() * 6))
        phase = 2 * np.pi * np.cumsum(glide) / RATE
        tone = np.sin(phase) * np.clip(np.sin(2 * np.pi * rng.integers(2, 5) * t / t[-1] + rng.random() * 6), 0, 1) ** 3
        pan_add(out, tone, 0.03, rng.random())
    # Ice in the ring grinding and cracking far off.
    for at, pan in ((4.1, 0.2), (12.7, 0.75), (20.9, 0.4)):
        grind = band(rng.standard_normal(n), 150, 900) * np.where((t >= at) & (t < at + 1.4), np.sin(np.pi * np.clip((t - at) / 1.4, 0, 1)) ** 2, 0)
        crack = ping(t, at + 1.2, 420 + 200 * rng.random(), 0.08, 0.4) + 0.5 * ping(t, at + 1.2, 1300, 0.04, 0.3)
        pan_add(out, grind / max(np.max(np.abs(grind)), 1e-6), 0.08, pan)
        pan_add(out, crack, 0.08, pan)
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    for name, make in (("AMB_Lastlight", lastlight), ("AMB_AuroraShear", aurora_shear)):
        signal = make()
        # Measured as chapters 3 and 4 measured their beds: the file mixed down to one channel.
        rms = float(np.sqrt(np.mean(signal.mean(axis=1) ** 2)))
        write_ogg(BEDS / (name + ".ogg"), signal)
        print(f"{name} rms={rms:.4f} volume={TARGET / rms:.3f}")
    print("HOARFELL_AMBIENCE_OK", BEDS)
