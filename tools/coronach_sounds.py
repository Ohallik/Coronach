"""Synthesise chapter 9's Coronach and launch sounds. Nothing is sourced: every sample is made here from seeded noise and sines, so the files are ours.

    python tools/coronach_sounds.py              # write everything
    python tools/coronach_sounds.py --only bell,hiss

Cues (Lattice/Assets/_Project/Resources/Audio/SFX, mono 44.1 kHz WAV; AudioManager finds them by file name):
    school_bell     one soft stroke of the school raft's small bell, heard once in the walk (CoronachDoor, Dov's extra)
    joint_hiss      a breath of vented air as a chalked joint lets its seal go (CoronachLaunch, one per joint)
    joint_release   the walkway joint letting go: a deep knock, the collar's ring, and air rushing out under the deck (CoronachLaunch)
    district_lift   engines taking a district's weight: a low swell that rises in pitch as it lifts (CoronachLaunch, under the looped engine)
Needs numpy (as tools/water_sounds.py). Ends with CORONACH_SOUNDS_OK.
"""
import argparse, wave
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SFX = ROOT / "Lattice/Assets/_Project/Resources/Audio/SFX"
RATE = 44100


def band(noise, low, high, soft=0.25):
    """Noise kept between two frequencies, with soft shoulders so nothing rings."""
    spectrum = np.fft.rfft(noise)
    freq = np.fft.rfftfreq(len(noise), 1 / RATE)
    lo = np.clip((freq - low * (1 - soft)) / max(1e-6, low * soft), 0, 1)
    hi = np.clip((high * (1 + soft) - freq) / max(1e-6, high * soft), 0, 1)
    return np.fft.irfft(spectrum * lo * hi, len(noise))


def envelope(t, attack, decay):
    """Rises over attack seconds, then falls away exponentially with this time constant."""
    return np.clip(t / max(attack, 1e-4), 0, 1) * np.exp(-np.maximum(0, t - attack) / decay)


def write(name, signal, peak_db):
    signal = signal - np.mean(signal)
    fade = min(len(signal), int(.03 * RATE))
    signal[-fade:] *= np.linspace(1, 0, fade)
    signal = signal / max(1e-9, np.max(np.abs(signal))) * 10 ** (peak_db / 20)
    data = (np.clip(signal, -1, 1) * 32767).astype("<i2")
    path = SFX / (name + ".wav")
    with wave.open(str(path), "wb") as out:
        out.setnchannels(1); out.setsampwidth(2); out.setframerate(RATE); out.writeframes(data.tobytes())
    print(f"wrote {path.relative_to(ROOT)} {len(signal) / RATE:.2f}s peak {peak_db} dBFS")


def bell(rng):
    """A small hand bell, struck once and gently: a few inharmonic partials, the high ones dying first, each split a hair so it shimmers; a soft tap of noise for the clapper."""
    seconds = 4.2
    t = np.arange(int(seconds * RATE)) / RATE
    f0 = 784.0
    partials = [(1.0, 1.0, 3.4), (2.04, .42, 2.1), (2.76, .32, 1.5), (3.86, .18, .95), (5.42, .11, .6), (.5, .14, 4.0)]
    out = np.zeros_like(t)
    for ratio, amp, decay in partials:
        # The two halves of a partial start together (a bell is loudest as it is struck) and drift apart slowly: the shimmer, not a swell.
        phase = rng.uniform(0, 2 * np.pi)
        for split in (-.0011, .0011):
            out += amp * .5 * np.sin(2 * np.pi * f0 * ratio * (1 + split) * t + phase) * envelope(t, .002, decay)
    tap = band(rng.standard_normal(len(t)), 1800, 6000) * envelope(t, .0005, .006)
    return out + tap * .25


def hiss(rng):
    """Air let out of a seal: a soft rise, a breath, a tail, with the band drifting down as the pressure goes."""
    seconds = 1.5
    t = np.arange(int(seconds * RATE)) / RATE
    high = band(rng.standard_normal(len(t)), 2400, 7000)
    low = band(rng.standard_normal(len(t)), 600, 2400)
    shape = envelope(t, .06, .42)
    return high * shape * (1 - .4 * t / seconds) + low * shape * .35


def release(rng):
    """The walkway joint letting go: a knock felt more than heard, the collar ringing once, and the air under the deck rushing out and dying away."""
    seconds = 3.8
    t = np.arange(int(seconds * RATE)) / RATE
    sweep = 74 * np.exp(-t / 1.4) + 36
    knock = np.sin(2 * np.pi * np.cumsum(sweep) / RATE) * envelope(t, .004, .45)
    ring = sum(a * np.sin(2 * np.pi * f * t + rng.uniform(0, 6.3)) * envelope(t, .002, d) for f, a, d in [(182, .5, .7), (413, .32, .5), (761, .2, .35), (1290, .1, .22)])
    rush = band(rng.standard_normal(len(t)), 180, 1600) * envelope(t - .08, .35, 1.05) * (t > .08)
    grit = band(rng.standard_normal(len(t)), 2500, 6000) * envelope(t, .01, .25)
    return knock * 1.0 + ring * .45 + rush * .55 + grit * .12


def lift(rng):
    """Engines taking a district's weight: a low swell whose tone climbs as the load comes off the deck, a rumble under it, held and let fade."""
    seconds = 7.0
    t = np.arange(int(seconds * RATE)) / RATE
    pitch = 46 + 22 * np.clip(t / 5.5, 0, 1) ** 1.6
    phase = 2 * np.pi * np.cumsum(pitch) / RATE
    tone = np.sin(phase) + .45 * np.sin(2 * phase + .7) + .22 * np.sin(3 * phase + 1.9)
    rumble = band(rng.standard_normal(len(t)), 30, 180)
    roar = band(rng.standard_normal(len(t)), 180, 900)
    swell = np.clip(t / 2.6, 0, 1) ** 1.5 * np.clip((seconds - t) / 1.8, 0, 1)
    return (tone * .5 + rumble * .9 + roar * .25 * np.clip((t - 1.2) / 3, 0, 1)) * swell


CUES = {"bell": ("school_bell", bell, -14), "hiss": ("joint_hiss", hiss, -12), "release": ("joint_release", release, -6), "lift": ("district_lift", lift, -8)}


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--only", default="", help="comma-separated: " + ",".join(CUES))
    args = parser.parse_args()
    wanted = [w for w in args.only.split(",") if w] or list(CUES)
    for key in wanted:
        name, make, peak = CUES[key]
        # Seeded by the cue's own name, so a rerun writes the same file.
        write(name, make(np.random.default_rng(sum(map(ord, name)))), peak)
    print("CORONACH_SOUNDS_OK")


if __name__ == "__main__":
    main()
