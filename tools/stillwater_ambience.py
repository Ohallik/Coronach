"""Synthesise chapter 10's anchor chamber bed (StillwaterChamber): the last room of the game, a deep still well under the anchor's held weight.

    python tools/stillwater_ambience.py      # writes AMB_StillwaterChamber.ogg to Lattice/Assets/_Project/Audio/Ambience

A 34 s seamless stereo loop made here from seeded noise and sines with the helpers of tools/water_sounds.py (as tools/continuance_ambience.py
does), so the file is ours; nothing is sourced. Nobody can listen to it, so it is measured instead: the script prints the bed's RMS (mixed
to one channel) and the ZoneDef volume that plays it at the level the earlier beds play at (file RMS times volume about 0.0224, -33 dB).
LastMooringBuilder.Chamber holds that volume as a constant beside the path (Chamber.BedLevel). Needs numpy and imageio-ffmpeg.
Ends with STILLWATER_AMBIENCE_OK.

It is not the Continuance's air (no drive, no vents, no chime, no crews): the ship has been left behind overhead. It is quieter and emptier,
and it plays under the boss's track and the release's cue, so it keeps out of the music's middle (almost nothing over 1 kHz) and has no
steady tone in it: nothing a player would tire of over ten minutes of a fight.
- The air: a deep, still air in a great round room, wide, hardly moving.
- The weight: steel taking the anchor's held load somewhere below, a slow rough moan sagging in pitch as it gives in jerks, three times in
  the loop; a link of the chain settling against the next, a dull heavy strike. All of it far down and heard through the well's own long
  reverberation.
- Water far below: a slow deep wash at the bottom of the drop, swelling and falling, and a drop now and then falling all that way.

Every layer is made round the loop (the noises are periodic over it, an event's tail and the reverberation run past the end into the
start), so the file loops without a fold: the 2 s cross-fade the other beds use (water_sounds.seamless) dips a noise floor 2 to 3 dB a
second into each loop, which a bed this still would let a player hear every 34 s.
"""
from pathlib import Path
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import BEDS, RATE, band, level, ping, pink, slow, write_ogg  # noqa: E402

TARGET = 0.0224   # file RMS x ZoneDef volume, as every earlier bed plays
SECONDS = 34


def pan_add(out, sound, gain, pan):
    out[:, 0] += sound * gain * (1 - pan)
    out[:, 1] += sound * gain * pan


def norm(x):
    return x / max(1e-9, np.max(np.abs(x)))


def since(t, at):
    """Seconds since `at`, round the loop: an event near the end goes on into the start."""
    return np.mod(t - at, SECONDS)


def strain(rng, t, n, at, length, start, end):
    """Steel taking a held weight: a slow rough moan whose pitch sags from `start` to `end` as it gives, in jerks (stick and slip), with the
    grind of it. The phase is worked out from the time since it began, so nothing jumps where the loop wraps."""
    local = since(t, at)
    u = np.clip(local / length, 0, 1)
    env = np.sin(np.pi * u) ** 2 * (local < length)
    phase = 2 * np.pi * (start * local + (end - start) * local * local / (2 * length))
    grip = slow(rng, n, 7) ** 3
    tone = sum(np.sin(k * phase + rng.random() * 6.3) * (0.5 if k == 1 else 1) / k ** 1.1 for k in range(1, 7))
    grind = band(rng.standard_normal(n), 160, 800)
    return (norm(tone) * (0.35 + 0.65 * grip) + norm(grind) * grip * 0.3) * env


def link(t, at, hertz, decay):
    """A link of the chain settling against the next under the load: a dull heavy strike with a bar's partials, not a bell's."""
    local = since(t, at)
    strike = ping(local, 0.0, hertz, decay) + 0.4 * ping(local, 0.0, hertz * 2.76, decay * 0.5) + 0.15 * ping(local, 0.0, hertz * 5.40, decay * 0.25)
    return strike * np.clip(local / 0.008, 0, 1)   # heavy, not sharp: it comes on over 8 ms


def hall(rng, x, n, seconds=4.0):
    """The well's own long reverberation (low end lasting, highs dying fast), each side its own, convolved round the loop."""
    m = int(seconds * RATE)
    tt = np.arange(m) / RATE
    out = np.zeros_like(x)
    for side in range(2):
        low = band(rng.standard_normal(m), 30, 500) * np.exp(-tt / 0.6)
        high = band(rng.standard_normal(m), 500, 3000) * np.exp(-tt / 0.18)
        ir = norm(low) + 0.25 * norm(high)
        ir[: int(0.045 * RATE)] = 0          # the far wall: nothing comes back for the first 45 ms
        ir *= np.clip((tt - 0.045) / 0.02, 0, 1)
        ir /= np.sqrt(np.sum(ir ** 2))
        pad = np.zeros(n)
        pad[:m] = ir
        out[:, side] = np.fft.irfft(np.fft.rfft(x[:, side]) * np.fft.rfft(pad), n)
    return out


def stillwater():
    rng = np.random.default_rng(1017)
    n = int(SECONDS * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    # The air: deep and wide, each side its own, swelling only very slowly; a little of the room's hollow over it, so a small speaker hears it.
    for side in range(2):
        air = band(pink(rng, n), 45, 260) * (0.86 + 0.14 * slow(rng, n, 0.03))
        hollow = band(rng.standard_normal(n), 170, 850) * (0.8 + 0.2 * slow(rng, n, 0.05))
        out[:, side] = norm(air) * 0.5 + norm(hollow) * 0.1
    # Water far below: a slow deep wash at the foot of the drop, in the middle (straight down), and the lap of it, fainter.
    wash = band(pink(rng, n), 60, 360) * (0.45 + 0.55 * slow(rng, n, 0.14) ** 2)
    lap = band(rng.standard_normal(n), 240, 1000) * slow(rng, n, 0.8) ** 3 * (0.3 + 0.7 * slow(rng, n, 0.14))
    pan_add(out, norm(wash), 0.2, 0.5)
    pan_add(out, norm(lap), 0.035, 0.5)
    # What is heard through the well's reverberation: the weight, the chain, a drop falling.
    far = np.zeros((n, 2))
    for at, length, start, end, pan in ((2.8, 5.6, 47, 41, 0.3), (14.6, 4.4, 55, 50, 0.72), (24.9, 6.2, 43, 38, 0.5)):
        pan_add(far, strain(rng, t, n, at, length, start, end), 0.17, pan)
    for at, hertz, pan in ((9.3, 132, 0.6), (21.7, 118, 0.35), (31.6, 141, 0.55)):
        pan_add(far, link(t, at, hertz, 0.32), 0.12, pan)
    for at, hertz, pan in ((6.1, 820, 0.45), (18.8, 690, 0.58)):
        drop = band(ping(since(t, at), 0.0, hertz, 0.03, -1.5), 300, 1600)
        pan_add(far, drop, 0.25, pan)
        pan_add(out, drop, 0.07, pan)     # the drop itself, small and clear, before the well gives it back
    out += hall(rng, far, n) * 0.9 + far * 0.12
    # Nothing under 34 Hz: a small speaker can't play it and a large one would only boom under the music.
    for side in range(2):
        out[:, side] = band(out[:, side], 34, 6000)
    return level(out, 0.5)


if __name__ == "__main__":
    signal = stillwater()
    path = BEDS / "AMB_StillwaterChamber.ogg"
    write_ogg(path, signal)
    mono = signal.mean(axis=1)
    rms = float(np.sqrt(np.mean(mono ** 2)))
    print(f"AMB_StillwaterChamber rms={rms:.4f} volume={TARGET / max(1e-9, rms):.3f} seconds={len(mono) / RATE:.1f}")
    print("STILLWATER_AMBIENCE_OK")
