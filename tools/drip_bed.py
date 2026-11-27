"""Lengthen Hushwell's drip bed so its drips stop repeating every 19.5 seconds (round ten, November 27).

    python tools/drip_bed.py      # rewrites Lattice/Assets/_Project/Audio/Ambience/AMB_Drips.ogg in place (keep its .meta: Hushwell's ZoneDef names it)

AMB_Drips is a Sonniss bed copied from Frostbound (docs/ASSET_SOURCES.md, November 6): 19.5 s of irregular drips over a near-silent floor,
looped. Forty-eight drips on a 19.5 s cycle come round as the same rhythm three times a minute, which a player standing in the cave hears
after a while. The file is cut at its quietest moments into pieces and laid out three times in three different orders (no piece beside
itself, every join on a quiet moment with a 20 ms crossfade), 58.5 s in all, so the pattern returns once a minute and never in the
order it went, and it is raised 4 dB so the cave's floor sits inside the beds' band (AudioMixTests), its loudest drips rounded off under -0.5 dBFS.
It reads the original from git's HEAD copy if the file on disk has already been lengthened, so running it twice gives the same file.
Needs numpy and imageio-ffmpeg. Ends with DRIP_BED_OK.
"""
from pathlib import Path
import subprocess, sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from water_sounds import BEDS, RATE, write_ogg  # noqa: E402
import imageio_ffmpeg  # noqa: E402

PATH = BEDS / "AMB_Drips.ogg"
ROOT = Path(__file__).resolve().parents[1]


def decode(data):
    raw = subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), "-v", "error", "-i", "pipe:0", "-f", "f32le", "-ac", "2", "-ar", str(RATE), "-"],
                         input=data, capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.float32).astype(np.float64).reshape(-1, 2)


def original():
    data = PATH.read_bytes()
    signal = decode(data)
    if len(signal) / RATE > 25:  # already lengthened: start again from the committed file
        data = subprocess.run(["git", "show", "HEAD:Lattice/Assets/_Project/Audio/Ambience/AMB_Drips.ogg"], cwd=ROOT, capture_output=True, check=True).stdout
        signal = decode(data)
    return signal


def main():
    signal = original()
    n = len(signal)
    hop = int(.01 * RATE)
    energy = np.array([np.sum(signal[i:i + hop] ** 2) for i in range(0, n - hop, hop)])
    # The quietest 10 ms moments, at least 1.6 s apart and clear of the ends: the cuts.
    cuts = []
    for index in np.argsort(energy):
        at = index * hop
        if at < RATE or at > n - RATE:
            continue
        if all(abs(at - c) > 1.6 * RATE for c in cuts):
            cuts.append(at)
        if len(cuts) == 7:
            break
    cuts = [0] + sorted(cuts) + [n]
    pieces = [signal[a:b] for a, b in zip(cuts[:-1], cuts[1:])]
    rng = np.random.default_rng(1953)
    orders = [list(range(len(pieces)))]
    while len(orders) < 3:
        order = list(rng.permutation(len(pieces)))
        joined = orders[-1][-1:] + order
        if all(abs(a - b) != 1 and a != b for a, b in zip(joined[:-1], joined[1:])) and order[-1] != orders[0][0]:
            orders.append(order)
    fade = int(.02 * RATE)
    ramp = np.linspace(0, 1, fade)[:, None]
    out = pieces[orders[0][0]].copy()
    for order in orders:
        for k, index in enumerate(order):
            if out is not None and order is orders[0] and k == 0:
                continue
            piece = pieces[index]
            out[-fade:] = out[-fade:] * (1 - ramp) + piece[:fade] * ramp
            out = np.concatenate([out, piece[fade:]])
    # The loop's own join, on the first cut's quiet moment.
    out[:fade] = out[:fade] * ramp + out[-fade:] * (1 - ramp)
    # Raised 4 dB, and the few drips that would then pass half scale rounded off softly so none passes -0.5 dBFS: the floor and the
    # ordinary drips keep their shape, the loudest of them lose a little of their top.
    out = out[:-fade] * 10 ** (4 / 20)
    knee, ceiling = .5, 10 ** (-.5 / 20)
    over = np.abs(out) > knee
    out[over] = np.sign(out[over]) * (knee + (ceiling - knee) * np.tanh((np.abs(out[over]) - knee) / (ceiling - knee)))
    write_ogg(PATH, out)
    rms = 20 * np.log10(np.sqrt((out ** 2).mean(axis=0)).max())
    print(f"AMB_Drips {len(out) / RATE:.1f}s from {n / RATE:.1f}s, {len(pieces)} pieces in orders {orders}, peak {20 * np.log10(np.max(np.abs(out))):.1f} dBFS, "
          f"rms a channel {rms:.1f}")
    print("DRIP_BED_OK", PATH)


if __name__ == "__main__":
    main()
