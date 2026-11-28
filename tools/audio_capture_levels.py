"""What the effects and the music measured in two captures of one route, against the mix sheet (round ten, November 27).

    python tools/audio_capture_levels.py <capture with music at 0> <capture with music at 1>

Each argument is a capture folder or its mix.wav (QualityCapture writes the listener's float mix there). Capture the same route twice
with an `-audio-settings-path` JSON of the five levels: music 0 and every other bus at 1, then all five at 1. The first is the effects,
the beds and the menus alone; the music is what the second adds (its RMS taken as the root of the difference of the two mean squares:
the music does not correlate with the rest, and a replayed route plays the same fight twice). Printed against Lattice.Core.CueLevels'
bands at the listener, every slider at 1:

  effects   the peaks of the transients (50 ms windows standing 6 dB over the 400 ms around them): impacts -9 dBFS to the ceiling, motion -14 to -10,
            steps -20 to -16; the loudest transients are the impacts, so the top tenth's median is read against the impacts' band
  bed       the quietest tenth of 400 ms windows of the music-0 capture: the place's air, -50 to -36 RMS since round eleven (D453's -32 to -28 came down 8 dB, the fallback hum 16; a fight's steps lift it)
  music     -26 to -23 RMS a channel (CueLevels.MusicLowDb/HighDb: the brief's -24 to -21 was read through a downmix, 2.2 dB hot)
  a hit over the music: the top tenth's median over the music's RMS, 10 dB or more
  ceiling   no sample of either capture over -1 dBFS: the master limiter's ceiling (round twelve, D460; the first captures peaked at +2.7)

Listening remains its own gate: these are levels, not whether anything sounds right. Needs numpy. Ends with AUDIO_CAPTURE_LEVELS_OK
or AUDIO_CAPTURE_LEVELS_OUTSIDE.
"""
import struct, sys
from pathlib import Path

import numpy as np

CEILING = -1.0
# The loudest tenth's top is the limiter's ceiling, not one blow's level (D460, by D452's rule: five readings on November 28 before the
# bar was set). One blow lands at -8; blows that land together stack, and the loudest tenth read -5.2 in the Wick Reach, -4.8 in the freight
# hall, -4.0 against the Host Marshal, -1.4 against the Last Warden and -1.0 against the Tollkeeper. What the bar still holds is the floor:
# the effects are not under the music again.
IMPACT, MOTION, STEP, BED, MUSIC, OVER = (-9, CEILING), (-14, -10), (-20, -16), (-50, -36), (-26, -23), 10


def read(path):
    path = Path(path)
    if path.is_dir():
        path = path / "mix.wav"
    blob = path.read_bytes()
    if blob[:4] != b"RIFF" or blob[8:12] != b"WAVE":
        raise SystemExit(f"{path}: not a WAV file")
    at, fmt, data = 12, None, None
    while at + 8 <= len(blob):
        tag, size = blob[at:at + 4], struct.unpack("<I", blob[at + 4:at + 8])[0]
        body = blob[at + 8:at + 8 + size]
        if tag == b"fmt ":
            fmt = struct.unpack("<HHIIHH", body[:16])
        elif tag == b"data":
            data = body
        at += 8 + size + (size & 1)
    if fmt is None or data is None:
        raise SystemExit(f"{path}: no fmt or data chunk")
    code, channels, rate, _, _, bits = fmt
    if code == 3 and bits == 32:
        samples = np.frombuffer(data[:len(data) // 4 * 4], dtype="<f4")
    elif code == 1 and bits == 16:
        samples = np.frombuffer(data[:len(data) // 2 * 2], dtype="<i2") / 32768.0
    else:
        raise SystemExit(f"{path}: format {code}/{bits} bits not read here")
    samples = samples[:len(samples) // channels * channels].astype(np.float64).reshape(-1, channels)
    return samples, rate


def db(x):
    return -120.0 if x <= 1e-9 else 20 * np.log10(x)


def windows(mono, size, fn):
    count = len(mono) // size
    return fn(mono[:count * size].reshape(count, size), axis=1)


def transients(samples, rate):
    loud = np.abs(samples).max(axis=1)
    peak = windows(loud, int(.05 * rate), np.max)
    rms = np.sqrt(windows((samples ** 2).mean(axis=1), int(.05 * rate), np.mean))
    around = np.array([np.median(rms[max(0, i - 4):i + 4]) for i in range(len(rms))])
    hot = [db(p) for p, r, a in zip(peak, rms, around) if db(r) > db(a) + 6 and db(p) > -60]
    return np.array(hot)


def verdict(value, band):
    return "inside" if band[0] - .5 <= value <= band[1] + .5 else "OUTSIDE"


def main():
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    quiet, rate = read(sys.argv[1])
    full, rate_full = read(sys.argv[2])
    if rate != rate_full:
        raise SystemExit("the two captures differ in sample rate")
    outside = []
    peaks = transients(quiet, rate)
    print(f"music 0: {len(quiet) / rate:.1f} s, peak {db(np.abs(quiet).max()):.1f} dBFS, RMS {db(np.sqrt((quiet ** 2).mean())):.1f}")
    if len(peaks) == 0:
        print("no transients: no effects were heard in the capture")
        outside.append("effects")
    else:
        top = np.sort(peaks)[-max(1, len(peaks) // 10):]
        loudest = float(np.median(top))
        print(f"effects: {len(peaks)} transients, median peak {np.median(peaks):.1f}, 10th to 90th percentile {np.percentile(peaks, 10):.1f} to "
              f"{np.percentile(peaks, 90):.1f}, the top tenth's median {loudest:.1f} ({verdict(loudest, IMPACT)} the impacts' {IMPACT}), the loudest {peaks.max():.1f}")
        print(f"         the middle ones read against motion {MOTION} and steps {STEP}: {np.percentile(peaks, 50):.1f} and {np.percentile(peaks, 20):.1f}")
        if verdict(loudest, IMPACT) != "inside":
            outside.append("impacts")
    bed = float(np.percentile(windows((quiet ** 2).mean(axis=1), int(.4 * rate), np.mean), 10))
    print(f"bed: the quietest tenth of 400 ms windows {db(np.sqrt(bed)):.1f} RMS ({verdict(db(np.sqrt(bed)), BED)} {BED})")
    if verdict(db(np.sqrt(bed)), BED) != "inside":
        outside.append("bed")
    music = max(0.0, float((full ** 2).mean() - (quiet ** 2).mean()))
    music_db = db(np.sqrt(music))
    print(f"music 1: {len(full) / rate:.1f} s, RMS {db(np.sqrt((full ** 2).mean())):.1f}; the music alone about {music_db:.1f} RMS ({verdict(music_db, MUSIC)} {MUSIC})")
    if verdict(music_db, MUSIC) != "inside":
        outside.append("music")
    # The sum never leaves full scale (D460): Lattice.Core.MasterLimiter holds it at CEILING where it is heard, ahead of the capture's tap.
    highest = max(db(np.abs(quiet).max()), db(np.abs(full).max()))
    count = int((np.abs(quiet) > 10 ** (CEILING / 20) * 1.006).sum() + (np.abs(full) > 10 ** (CEILING / 20) * 1.006).sum())
    print(f"ceiling: the two captures' highest sample {highest:.2f} dBFS, {count} samples over the limiter's {CEILING} ({'inside' if count == 0 else 'OUTSIDE'})")
    if count:
        outside.append("over the ceiling")
    if len(peaks):
        over = float(np.median(np.sort(peaks)[-max(1, len(peaks) // 10):])) - music_db
        print(f"the loudest tenth of the effects stand {over:.1f} dB over the music's RMS (bar {OVER})")
        if over < OVER:
            outside.append("hit over music")
    print("AUDIO_CAPTURE_LEVELS_OUTSIDE " + ", ".join(outside) if outside else "AUDIO_CAPTURE_LEVELS_OK")


if __name__ == "__main__":
    main()
