"""Synthesise the Frozen Host's ambience bed (chapter 6): the White Reach's wind and the dig under it.

    python tools/frozen_host_ambience.py        # writes AMB_FrozenHost.ogg to Lattice/Assets/_Project/Audio/Ambience

A 26 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as tools/hoarfell_ambience.py
makes Lastlight's and the Shear's), so the file is ours; nothing is sourced. Nobody can listen to it, so it is measured instead: the script
prints the bed's RMS (the file mixed to one channel) and the ZoneDef volume that plays it at the level every earlier bed plays at (file RMS
times volume about 0.0224, -33 dB); HoarfellBuilder.FrozenHost holds that volume beside the path. Needs numpy and imageio-ffmpeg. Ends with
FROZEN_HOST_AMBIENCE_OK.

- Open wind over snow, harder and higher than Lastlight's sheltered one, in long gusts with the hiss of spindrift running ahead of each.
- The glacier under strain: a deep groan now and then, and a sharp crack far off.
- The Muster's rigs working below: a slow mechanical thump, very low, a few times a loop.
- The herd somewhere out on the Reach: a low call, twice, far to one side.
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


def frozen_host():
    rng = np.random.default_rng(1603)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        gusts = slow(rng, n, 0.12)
        wind = band(pink(rng, n), 260, 1500) * (0.45 + 0.55 * gusts)
        high = band(pink(rng, n), 1200, 4200) * gusts ** 2.5
        drift = band(rng.standard_normal(n), 3600, 9000) * (0.25 + 0.75 * slow(rng, n, 0.33) ** 2)
        out[:, side] = wind / np.max(np.abs(wind)) * 0.6 + high / np.max(np.abs(high)) * 0.2 + drift / np.max(np.abs(drift)) * 0.1
    # The glacier groaning: a low slow glide, rough, rising and falling.
    for at, length, pan in ((3.2, 2.6, 0.3), (16.4, 3.1, 0.7)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + length)
        env = np.sin(np.pi * np.clip(local / length, 0, 1)) ** 2 * live
        glide = 52 + 14 * np.sin(np.pi * np.clip(local / length, 0, 1))
        groan = np.sin(2 * np.pi * np.cumsum(glide) / RATE) * env * (0.65 + 0.35 * np.sign(np.sin(2 * np.pi * 17 * local)))
        pan_add(out, groan, 0.09, pan)
    # Ice cracking far off: a sharp report and its rattle.
    for at, pan in ((7.6, 0.8), (12.1, 0.2), (21.8, 0.6)):
        crack = ping(t, at, 520 + 260 * rng.random(), 0.05, 0.6) + 0.6 * ping(t, at + 0.01, 1700, 0.02, 0.4)
        rattle = band(rng.standard_normal(n), 900, 4000) * np.where((t >= at) & (t < at + 0.5), np.exp(-(t - at) * 9), 0)
        pan_add(out, crack, 0.07, pan)
        pan_add(out, rattle / max(np.max(np.abs(rattle)), 1e-6), 0.04, pan)
    # The rigs below: a slow heavy thump, felt more than heard.
    for at in (1.5, 6.5, 11.5, 16.5, 21.5):
        thump = ping(t, at, 46, 0.18, -0.3) + 0.4 * ping(t, at + 0.08, 92, 0.09, -0.2)
        pan_add(out, thump, 0.06, 0.5)
    # The herd out on the Reach: a low call, far off to one side.
    for at, hertz, pan in ((9.2, 84, 0.08), (19.0, 92, 0.12)):
        local = np.clip(t - at, 0, None)
        live = (t >= at) & (t < at + 1.8)
        env = np.sin(np.pi * np.clip(local / 1.8, 0, 1)) ** 1.5 * live
        call = (np.sin(2 * np.pi * (hertz + 10 * np.sin(np.pi * np.clip(local / 1.8, 0, 1))) * local) + 0.3 * np.sin(2 * np.pi * 2 * hertz * local)) * env
        pan_add(out, call, 0.035, pan)
    return level(seamless(out, 2.0), 0.5)


if __name__ == "__main__":
    signal = frozen_host()
    # Measured as chapters 3 to 6 measure their beds: the file mixed down to one channel.
    rms = float(np.sqrt(np.mean(signal.mean(axis=1) ** 2)))
    write_ogg(BEDS / "AMB_FrozenHost.ogg", signal)
    print(f"AMB_FrozenHost rms={rms:.4f} volume={TARGET / rms:.3f}")
    print("FROZEN_HOST_AMBIENCE_OK", BEDS)
