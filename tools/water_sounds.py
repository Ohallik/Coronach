"""Synthesise the world-mechanics sounds and the two chapter 3 combat-zone beds. Nothing is sourced: every sample is made here from seeded noise and sines, so the files are ours.

    python tools/water_sounds.py              # write everything
    python tools/water_sounds.py --only splash,rush,groan,river,roothall

Cues (Lattice/Assets/_Project/Resources/Audio/SFX, mono 44.1 kHz WAV; AudioManager finds them by file name):
    water_splash   a body stepping into water (WaterVolume, RisingWater)
    water_rush     water let go: a sluice opening; looped while a hall floods (FlowSwitch, RisingWater). The file loops without a seam.
    root_groan     timber taking a load: the island's lurch (ShiftAnchor)
Beds (Lattice/Assets/_Project/Audio/Ambience, stereo OGG, 24 s seamless loops; a ZoneDef's ambientAudio):
    AMB_BraidedRiver   a wide river running past, insects in the heat, a frog now and then
    AMB_RootHall       inside a root island: a low hollow air, drips, water trickling somewhere below, timber settling
Needs numpy and imageio-ffmpeg (already used by tools/prepare_audio_palette.py). Ends with WATER_SOUNDS_OK.
"""
import argparse, subprocess, wave
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SFX = ROOT / "Lattice/Assets/_Project/Resources/Audio/SFX"
BEDS = ROOT / "Lattice/Assets/_Project/Audio/Ambience"
RATE = 44100


def band(noise, low, high, rate=RATE, soft=0.25):
    """Noise kept between two frequencies, with soft shoulders so nothing rings."""
    spectrum = np.fft.rfft(noise)
    freq = np.fft.rfftfreq(len(noise), 1 / rate)
    lo = np.clip((freq - low * (1 - soft)) / max(1e-6, low * soft), 0, 1)
    hi = np.clip((high * (1 + soft) - freq) / max(1e-6, high * soft), 0, 1)
    return np.fft.irfft(spectrum * lo * hi, len(noise))


def pink(rng, count):
    spectrum = np.fft.rfft(rng.standard_normal(count))
    freq = np.fft.rfftfreq(count, 1 / RATE)
    spectrum[1:] /= np.sqrt(freq[1:])
    spectrum[0] = 0
    out = np.fft.irfft(spectrum, count)
    return out / np.max(np.abs(out))


def slow(rng, count, hertz):
    """A wandering value between 0 and 1 that changes about `hertz` times a second, and comes back to where it started."""
    spectrum = np.fft.rfft(rng.standard_normal(count))
    freq = np.fft.rfftfreq(count, 1 / RATE)
    spectrum[freq > hertz] = 0
    spectrum[0] = 0
    out = np.fft.irfft(spectrum, count)
    out -= out.min()
    return out / max(1e-9, out.max())


def seamless(signal, cross):
    """The tail folded under the head, so the end runs into the start."""
    n = int(cross * RATE)
    fade = np.linspace(0, 1, n)
    if signal.ndim == 1:
        head = signal[:n] * fade + signal[-n:] * (1 - fade)
        return np.concatenate([head, signal[n:-n]])
    head = signal[:n] * fade[:, None] + signal[-n:] * (1 - fade[:, None])
    return np.concatenate([head, signal[n:-n]])


def level(signal, peak):
    return signal * (peak / max(1e-9, np.max(np.abs(signal))))


def ping(t, at, hertz, decay, fall=0.0):
    """One struck drop: a sine that dies away, its pitch sagging a little as it goes."""
    local = t - at
    live = local >= 0
    phase = 2 * np.pi * hertz * (local - fall * local * local)
    return np.where(live, np.sin(phase) * np.exp(-np.clip(local, 0, None) / decay), 0)


def splash():
    rng = np.random.default_rng(301)
    n = int(0.62 * RATE)
    t = np.arange(n) / RATE
    body = band(rng.standard_normal(n), 350, 5200) * np.exp(-t / 0.11) * np.clip(t / 0.004, 0, 1)
    spray = band(rng.standard_normal(n), 2500, 9000) * np.exp(-np.clip(t - 0.03, 0, None) / 0.2) * np.clip((t - 0.02) / 0.03, 0, 1) * 0.45
    plunk = np.sin(2 * np.pi * (170 * t - 260 * t * t).clip(0)) * np.exp(-t / 0.045) * 0.9
    drops = sum(ping(t, 0.12 + 0.07 * i + 0.02 * rng.random(), 900 + 700 * rng.random(), 0.02, -6) for i in range(5)) * 0.18
    return level(body / np.max(np.abs(body)) + spray / np.max(np.abs(spray)) * 0.5 + plunk + drops, 0.82)


def rush():
    rng = np.random.default_rng(302)
    n = int(3.6 * RATE)
    low = band(pink(rng, n), 120, 900) * (0.7 + 0.3 * slow(rng, n, 1.2))
    mid = band(rng.standard_normal(n), 900, 4200) * (0.45 + 0.55 * slow(rng, n, 3))
    hiss = band(rng.standard_normal(n), 4200, 10000) * (0.3 + 0.7 * slow(rng, n, 6)) * 0.35
    mix = low / np.max(np.abs(low)) * 0.9 + mid / np.max(np.abs(mid)) * 0.6 + hiss / np.max(np.abs(hiss)) * 0.3
    return level(seamless(mix, 0.6), 0.7)


def groan():
    rng = np.random.default_rng(303)
    n = int(1.9 * RATE)
    t = np.arange(n) / RATE
    pitch = 58 - 14 * t / t[-1] + 5 * slow(rng, n, 3)
    phase = 2 * np.pi * np.cumsum(pitch) / RATE
    # Stick and slip: the timber gives in jerks, so the tone comes in bursts and each burst is rough.
    grip = slow(rng, n, 14) ** 3
    saw = (np.sin(phase) + 0.5 * np.sin(2 * phase + 0.4) + 0.33 * np.sin(3 * phase + 1.1) + 0.2 * np.sin(5 * phase)) * (0.25 + 0.75 * grip)
    creak = band(rng.standard_normal(n), 380, 1500) * grip * 0.55
    thump = np.sin(2 * np.pi * (70 * t - 55 * t * t).clip(0)) * np.exp(-t / 0.12) * 1.1
    shape = np.clip(t / 0.05, 0, 1) * np.clip((t[-1] - t) / 0.5, 0, 1)
    return level((saw / np.max(np.abs(saw)) + creak / np.max(np.abs(creak)) * 0.6) * shape + thump, 0.8)


def insects(rng, n, t, voices, low, high, gain):
    """Trains of short pulses at one pitch each, starting and stopping by themselves: two channels, each voice somewhere between them."""
    out = np.zeros((n, 2))
    for _ in range(voices):
        hertz = low + (high - low) * rng.random()
        rate = 18 + 26 * rng.random()
        pulse = np.clip(np.sin(2 * np.pi * rate * t + rng.random() * 6), 0, 1) ** 3
        # Each voice sings for a while and rests, on its own slow clock, a whole number of times in the loop.
        cycles = rng.integers(3, 9)
        sings = np.clip(np.sin(2 * np.pi * cycles * t / t[-1] + rng.random() * 6) * 3 - 0.6, 0, 1)
        tone = np.sin(2 * np.pi * hertz * t) * pulse * sings * (0.4 + 0.6 * rng.random())
        pan = rng.random()
        out[:, 0] += tone * (1 - pan)
        out[:, 1] += tone * pan
    return out * gain / max(1, voices) ** 0.5


def river():
    rng = np.random.default_rng(304)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        deep = band(pink(rng, n), 90, 700) * (0.75 + 0.25 * slow(rng, n, 0.4))
        run = band(rng.standard_normal(n), 700, 3600) * (0.5 + 0.5 * slow(rng, n, 1.1))
        lap = band(rng.standard_normal(n), 3000, 8000) * slow(rng, n, 2.5) ** 2
        out[:, side] = deep / np.max(np.abs(deep)) * 0.8 + run / np.max(np.abs(run)) * 0.42 + lap / np.max(np.abs(lap)) * 0.14
    out += insects(rng, n, t, 9, 3600, 7200, 0.5)
    # A frog or two, well apart.
    for at in (3.4, 9.1, 9.5, 16.8, 21.2, 21.55):
        croak = ping(t, at, 360 + 80 * rng.random(), 0.07, 0.6) * (0.5 + 0.5 * np.sin(2 * np.pi * 55 * t))
        pan = rng.random()
        out[:, 0] += croak * 0.16 * (1 - pan)
        out[:, 1] += croak * 0.16 * pan
    return level(seamless(out, 2.0), 0.5)


def roothall():
    rng = np.random.default_rng(305)
    n = int(26 * RATE)
    t = np.arange(n) / RATE
    out = np.zeros((n, 2))
    for side in range(2):
        air = band(pink(rng, n), 50, 260) * (0.6 + 0.4 * slow(rng, n, 0.25))
        trickle = band(rng.standard_normal(n), 1800, 6500) * slow(rng, n, 4) ** 2
        out[:, side] = air / np.max(np.abs(air)) * 0.7 + trickle / np.max(np.abs(trickle)) * 0.16
    # Drips, each with an echo behind it from the far wall. None in the last two seconds, which fold under the first.
    for _ in range(22):
        at = 0.4 + 23 * rng.random()
        hertz = 700 + 1500 * rng.random()
        pan = rng.random()
        drop = ping(t, at, hertz, 0.05, -1.5) + 0.35 * ping(t, at + 0.21 + 0.1 * rng.random(), hertz * 0.98, 0.08, -1.5)
        out[:, 0] += drop * 0.2 * (1 - pan)
        out[:, 1] += drop * 0.2 * pan
    # Timber settling, far off: three short dull creaks.
    for at in (5.2, 13.7, 19.9):
        local = np.clip(t - at, 0, None)
        creak = band(rng.standard_normal(n), 180, 700) * np.where(t >= at, np.exp(-local / 0.35), 0) * (0.5 + 0.5 * np.sin(2 * np.pi * 9 * local) ** 2)
        out[:, 0] += creak / np.max(np.abs(creak)) * 0.2
        out[:, 1] += creak / np.max(np.abs(creak)) * 0.14
    out += insects(rng, n, t, 3, 4200, 6000, 0.09)
    return level(seamless(out, 2.0), 0.5)


def write_wav(path, signal):
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes((np.clip(signal, -1, 1) * 32767).astype("<i2").tobytes())


def write_ogg(path, signal):
    import imageio_ffmpeg
    path.parent.mkdir(parents=True, exist_ok=True)
    command = [imageio_ffmpeg.get_ffmpeg_exe(), "-hide_banner", "-loglevel", "error", "-nostdin", "-y", "-f", "f32le", "-ar", str(RATE), "-ac", "2", "-i", "pipe:0",
               "-c:a", "libvorbis", "-q:a", "4", "-bitexact", str(path)]
    subprocess.run(command, input=np.clip(signal, -1, 1).astype("<f4").tobytes(), check=True, timeout=120)


CUES = {"splash": ("water_splash", splash), "rush": ("water_rush", rush), "groan": ("root_groan", groan)}
BED = {"river": ("AMB_BraidedRiver", river), "roothall": ("AMB_RootHall", roothall)}

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--only", help="comma-separated: " + ",".join(list(CUES) + list(BED)))
    wanted = parser.parse_args().only
    wanted = wanted.split(",") if wanted else list(CUES) + list(BED)
    for key in wanted:
        if key in CUES:
            name, make = CUES[key]
            signal = make()
            write_wav(SFX / (name + ".wav"), signal)
            print(f"cue {name}.wav {len(signal) / RATE:.2f}s peak {np.max(np.abs(signal)):.2f} rms {np.sqrt(np.mean(signal ** 2)):.3f}")
        elif key in BED:
            name, make = BED[key]
            signal = make()
            write_ogg(BEDS / (name + ".ogg"), signal)
            print(f"bed {name}.ogg {len(signal) / RATE:.2f}s peak {np.max(np.abs(signal)):.2f} rms {np.sqrt(np.mean(signal ** 2)):.3f}")
        else:
            raise SystemExit("unknown sound " + key)
    print("WATER_SOUNDS_OK")
