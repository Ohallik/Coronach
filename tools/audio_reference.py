"""Bring every one-shot clip the game plays to one reference on disk, and print what each clip and the music measure.

    python tools/audio_reference.py            # rescale staged clips to the reference, then print the sheet
    python tools/audio_reference.py --check    # print the sheet only; exit 1 if any clip is off the reference
    python tools/audio_reference.py --music    # the music's tracks: peak, RMS, loudest 400 ms, and their median

Round ten (November 27): the palette (Resources/Audio/Palette, made by tools/prepare_audio_palette.py) sat at -10 dBFS sample peak, Kenney's
clips (Resources/Audio/SFX) at about -1, and the made ones (tools/water_sounds.py, tools/coronach_sounds.py) anywhere from -1.6 to -14. A
clip that has to play louder than its file cannot (AudioSource.volume stops at 1), so every one-shot now sits at REFERENCE dBFS sample peak,
and Lattice.Core.CueLevels says how loud each family reaches the listener. A 16-bit WAV is rescaled exactly; an Ogg Vorbis file is re-encoded
only when it is more than a decibel off (laserSmall_000, a stereo file at -5.7 a channel, made mono at the reference). Loops are measured by their RMS, and CueLevels plays them at their old level.
The reference is a sample peak, not a loudness: a 40 ms tick and a 1.4 s fall at one peak are not equally loud, which the families' own
targets in CueLevels carry, and the sheet prints each clip's loudest 400 ms beside its peak so the difference can be read.
Needs numpy and imageio-ffmpeg. Ends with AUDIO_REFERENCE_OK.
"""
import argparse, subprocess, sys, wave
from pathlib import Path

import numpy as np
import imageio_ffmpeg

REFERENCE = -1.0
TOLERANCE = 1.0
ROOT = Path(__file__).resolve().parents[1]
AUDIO = ROOT / "Lattice/Assets/_Project/Resources/Audio"
FOLDERS = (AUDIO / "Palette", AUDIO / "SFX")
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
STEREO = []  # every one-shot is mono: Unity pans a mono voice at -3 dB a channel, which CueLevels.PanLawDb counts


def decode(path, rate=48000, channels=1):
    raw = subprocess.run([FFMPEG, "-v", "error", "-i", str(path), "-f", "f32le", "-ac", str(channels), "-ar", str(rate), "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.float32).astype(np.float64)


def db(x):
    return -120.0 if x <= 1e-9 else 20 * np.log10(x)


def measure(path, rate=48000):
    # Each channel as it is: ffmpeg's downmix of a stereo file to one channel reads it 3 dB hot, and its upmix of a mono one 3 dB cold.
    probe = subprocess.run([FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True, text=True).stderr
    channels = 1 if ", mono," in probe else 2
    if channels != 1:
        STEREO.append(path.name)
    both = decode(path, rate, channels).reshape(-1, channels)
    a = both[:, int(np.argmax(np.abs(both).max(axis=0)))]
    peak, rms = float(np.max(np.abs(a))), float(np.sqrt(np.mean(a ** 2)))
    window = int(rate * .4)
    if a.size <= window:
        loud = rms
    else:
        sq = np.cumsum(np.concatenate([[0.0], a ** 2]))
        loud = float(np.sqrt(np.max(sq[window:] - sq[:-window]) / window))
    return a.size / rate, db(peak), db(rms), db(loud)


def wav_peak(path):
    with wave.open(str(path), "rb") as w:
        if w.getsampwidth() != 2:
            raise SystemExit(f"{path.name}: only 16-bit PCM WAV is rescaled here")
        pcm = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2")
    return db(np.max(np.abs(pcm.astype(np.float64))) / 32767)


def rescale_wav(path, gain_db):
    with wave.open(str(path), "rb") as w:
        params = w.getparams()
        pcm = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").astype(np.float64)
    out = np.clip(np.round(pcm * 10 ** (gain_db / 20)), -32767, 32767).astype("<i2")
    with wave.open(str(path), "wb") as w:
        w.setparams(params)
        w.writeframes(out.tobytes())


def rescale_ogg(path, gain_db):
    temporary = path.with_name(path.stem + ".rescaled.ogg")
    subprocess.run([FFMPEG, "-v", "error", "-y", "-i", str(path), "-af", f"volume={gain_db:.3f}dB", "-c:a", "libvorbis", "-q:a", "6",
                    str(temporary)], check=True)
    temporary.replace(path)


def clips():
    for folder in FOLDERS:
        for path in sorted(folder.iterdir()):
            if path.suffix.lower() in (".wav", ".ogg"):
                yield path


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--music", action="store_true")
    args = parser.parse_args()
    if args.music:
        rows = [(p.name,) + measure(p) for p in sorted((AUDIO / "Music").glob("*.ogg"))]
        for name, seconds, peak, rms, loud in rows:
            print(f"{name[:34]:34s} {seconds:7.1f}s peak {peak:6.1f} rms {rms:6.1f} loudest400ms {loud:6.1f}")
        print("MUSIC_MEDIAN peak %.1f rms %.1f loudest400ms %.1f" % tuple(float(np.median([r[i] for r in rows])) for i in (2, 3, 4)))
        return
    off = []
    for path in clips():
        seconds, peak, rms, loud = measure(path)
        before = peak
        if not args.check and abs(peak - REFERENCE) > .05:
            if path.suffix.lower() == ".wav":
                rescale_wav(path, REFERENCE - wav_peak(path))
            elif abs(peak - REFERENCE) > TOLERANCE:
                rescale_ogg(path, REFERENCE - peak)
            seconds, peak, rms, loud = measure(path)
        if abs(peak - REFERENCE) > TOLERANCE:
            off.append(path.name)
        print(f"{path.parent.name}/{path.name:26s} {seconds:5.2f}s peak {before:6.1f} -> {peak:6.1f}  rms {rms:6.1f}  loudest400ms {loud:6.1f}")
    if STEREO and not args.music:
        print("STEREO_ONE_SHOTS", " ".join(sorted(set(STEREO))))
        sys.exit(1)
    if off:
        print("OFF_REFERENCE", " ".join(off))
        sys.exit(1)
    print("AUDIO_REFERENCE_OK", REFERENCE, "dBFS sample peak")


if __name__ == "__main__":
    main()
