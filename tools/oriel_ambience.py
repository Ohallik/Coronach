"""Synthesise chapter 7's ambience beds (Oriel): the Orrery, the Skyfields and the Glass Choir.

    python tools/oriel_ambience.py        # writes AMB_Orrery.ogg, AMB_OrielSkyfields.ogg, AMB_GlassChoir.ogg to Lattice/Assets/_Project/Audio/Ambience

Each is a 24 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as chapter 4's
tools/wendmire_ambience.py does), so the files are ours; nothing is sourced. Nobody can listen to them, so each is measured instead: the
script prints every bed's RMS (the file mixed down to one channel) and the ZoneDef volume that plays it at the level the earlier beds play at
(file RMS times volume about 0.0224, -33 dB). OrielBuilder holds those volumes as constants beside the paths. Needs numpy and imageio-ffmpeg.
Ends with ORIEL_AMBIENCE_OK.

The chapter is the quiet one, so these are the sparest beds in the game: what is there is the place's own sound and little else.

- The Orrery: no air, so no wind: the machine. A deep slow swell under everything (the rails' hum), a clock's escapement far off, ticking
  and tocking at the rate the moons keep, a long metallic groan as a rail takes a moon's weight, and now and then a soft two-note chime as a
  row of gates changes, green to amber, far down the lane.
- The Skyfields: high thin air over floating stone. A soft wind that comes and goes, a sustained shimmer where the fibres run (a tone that
  beats against itself), and glass trees ringing somewhere when the wind finds them: a few glassy partials, struck and left to ring.
- The Glass Choir: inside a vast glass body. A very low, very slow drone (the dead road), long decays, glass settling with a creak and a
  tick, and a faint high ringing that never quite resolves. No wind, no water, nothing living.
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


def glass(t, at, hertz, decay, partials=(1.0, 2.76, 5.4, 8.93)):
    """A struck glass: inharmonic partials, the higher ones dying first."""
    out = np.zeros_like(t)
    for k, ratio in enumerate(partials):
        out += ping(t, at, hertz * ratio, decay / (1 + k * 0.9)) / (1 + k)
    return out


def orrery():
    rng = np.random.default_rng(1701)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        hum = band(pink(rng, n), 40, 160) * (0.55 + 0.45 * slow(rng, n, 0.07))
        dust = band(rng.standard_normal(n), 3000, 7000) * (0.3 + 0.7 * slow(rng, n, 0.4) ** 3)
        out[:, side] = hum / np.max(np.abs(hum)) * 0.6 + dust / np.max(np.abs(dust)) * 0.05
    # The rails themselves: two low tones a little apart, so they beat slowly.
    for hertz, pan in ((55.0, 0.35), (55.4, 0.65), (82.5, 0.5)):
        pan_add(out, np.sin(2 * np.pi * hertz * t) * (0.6 + 0.4 * np.sin(2 * np.pi * t / 13 + hertz)), 0.05, pan)
    # The escapement, far off: tick, tock, at the machine's own rate, muffled.
    for k, at in enumerate(np.arange(0.4, 25.6, 1.25)):
        click = band(rng.standard_normal(n), 900, 2600) * np.where(t >= at, np.exp(-np.clip(t - at, 0, None) / 0.012), 0)
        body = ping(t, at, 210 if k % 2 == 0 else 160, 0.05)
        pan_add(out, click / max(1e-9, np.max(np.abs(click))) * 0.6 + body * 0.5, 0.045, 0.42 + 0.16 * (k % 2))
    # A rail taking a moon's weight: a long falling metallic groan, twice.
    for at, pan in ((6.0, 0.2), (17.5, 0.8)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + 3.2)
        groan = np.sin(2 * np.pi * (140 - 18 * local) * local + 2.4 * np.sin(2 * np.pi * 3.1 * local)) * np.sin(np.pi * np.clip(local / 3.2, 0, 1)) ** 2 * live
        pan_add(out, groan, 0.06, pan)
    # A row of gates changing far down the lane: two soft notes, green then amber, or amber then green.
    for at, pan, notes in ((3.2, 0.7, (880, 660)), (11.8, 0.25, (660, 880)), (20.4, 0.55, (880, 660))):
        for k, hertz in enumerate(notes):
            pan_add(out, glass(t, at + k * 0.28, hertz, 1.4), 0.03, pan)
    return level(seamless(out, 2.0), 0.5)


def skyfields():
    rng = np.random.default_rng(1702)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        wind = band(pink(rng, n), 200, 1800) * slow(rng, n, 0.12) ** 2
        high = band(rng.standard_normal(n), 2400, 6000) * slow(rng, n, 0.3) ** 3
        air = band(pink(rng, n), 60, 240) * (0.5 + 0.5 * slow(rng, n, 0.05))
        out[:, side] = wind / np.max(np.abs(wind)) * 0.55 + high / np.max(np.abs(high)) * 0.08 + air / np.max(np.abs(air)) * 0.2
    # The fibres: a shimmer that beats against itself, swelling and fading as you would pass one.
    for hertz, pan in ((1244.5, 0.3), (1247.0, 0.7), (1866.0, 0.5)):
        swell = np.clip(np.sin(2 * np.pi * t / 24 * 2 + hertz), 0, 1) ** 2
        pan_add(out, np.sin(2 * np.pi * hertz * t) * swell, 0.012, pan)
    # Glass trees ringing when the wind finds them: a few struck glasses, bunched as a gust would strike a stand.
    for at, pan in ((2.1, 0.2), (2.35, 0.3), (2.6, 0.22), (9.4, 0.8), (9.55, 0.75), (16.0, 0.45), (16.3, 0.5), (16.45, 0.4), (21.7, 0.65)):
        pan_add(out, glass(t, at, 1500 + 900 * rng.random(), 1.8), 0.035, pan)
    return level(seamless(out, 2.0), 0.5)


def glass_choir():
    rng = np.random.default_rng(1703)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        room = band(pink(rng, n), 30, 120) * (0.6 + 0.4 * slow(rng, n, 0.04))
        hiss = band(rng.standard_normal(n), 4000, 9000) * (0.2 + 0.8 * slow(rng, n, 0.2) ** 4)
        out[:, side] = room / np.max(np.abs(room)) * 0.65 + hiss / np.max(np.abs(hiss)) * 0.03
    # The dead road's drone: a low fifth that drifts very slightly, as if the body still held a note.
    for hertz, pan in ((48.0, 0.4), (72.1, 0.6), (96.3, 0.5)):
        pan_add(out, np.sin(2 * np.pi * hertz * t + 0.3 * np.sin(2 * np.pi * t / 26 * 3)) * (0.5 + 0.5 * slow(rng, n, 0.06)), 0.05, pan)
    # Glass settling: a dry tick, now and then a creak that rises and stops.
    for at, pan in ((1.7, 0.15), (5.2, 0.85), (8.9, 0.5), (14.1, 0.3), (19.6, 0.7), (23.0, 0.4)):
        tick = band(rng.standard_normal(n), 3000, 8000) * np.where(t >= at, np.exp(-np.clip(t - at, 0, None) / 0.006), 0)
        pan_add(out, tick / max(1e-9, np.max(np.abs(tick))), 0.05, pan)
    for at, pan in ((7.4, 0.65), (18.2, 0.3)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + 1.4)
        creak = np.sin(2 * np.pi * (300 + 140 * local) * local) * np.sin(np.pi * np.clip(local / 1.4, 0, 1)) ** 3 * live
        creak *= 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 47 * local))
        pan_add(out, creak, 0.025, pan)
    # A high ringing that never resolves: two close tones far up, very faint.
    for hertz, pan in ((2093.0, 0.35), (2096.5, 0.65)):
        pan_add(out, np.sin(2 * np.pi * hertz * t) * (0.4 + 0.6 * slow(rng, n, 0.08)), 0.006, pan)
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    for name, make in (("AMB_Orrery", orrery), ("AMB_OrielSkyfields", skyfields), ("AMB_GlassChoir", glass_choir)):
        signal = make()
        rms = float(np.sqrt(np.mean(signal.mean(axis=1) ** 2)))
        write_ogg(BEDS / (name + ".ogg"), signal)
        print(f"{name} rms={rms:.4f} volume={TARGET / rms:.3f}")
    print("ORIEL_AMBIENCE_OK", BEDS)
