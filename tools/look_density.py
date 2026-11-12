"""How empty is a frame? Measures the largest stretch of plain, featureless floor in look-dev stills.

A fixed three-quarter camera shows mostly ground, so a zone reads as an empty plane when a large part of the frame
is one flat tone with only fine texture grain (chapter 2's canyon floors, November 11). For each still this splits
the frame into tiles, calls a tile "plain" when it has neither a strong edge nor a tone different from the frame's
commonest tone, and reports the plain share of the frame and the largest connected plain area.

    python tools/look_density.py Builds/quality/look/<run>              # every PNG in the folder
    python tools/look_density.py <folder> --match Lanternwick,Gorge     # only names containing these
    python tools/look_density.py <folder> --max-blob 0.22               # exit 1 if any still exceeds it
    python tools/look_density.py <folder> --mask out.png                # also write a contact sheet of the plain tiles

Calibration (November 11, qa-nov10 stills): dressed chapter 1 rooms measure a largest plain area of about 0.05-0.15,
chapter 2's bare floors 0.35-0.60. The default limit is 0.22.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image

TILE = 16          # px at the working width
WIDTH = 640        # working width
EDGE = 9.0         # mean gradient (0-255 luminance) above which a tile has structure, not grain
TONE = 26.0        # distance in Lab-ish space from the commonest floor tone above which a tile is "another surface"


def tiles(path: Path):
    im = Image.open(path).convert("RGB")
    h = int(im.height * WIDTH / im.width)
    im = im.resize((WIDTH, h), Image.BILINEAR)
    rgb = np.asarray(im, dtype=np.float32)
    lum = rgb @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    # blur away texture grain: a 5x5 box, so cracks in a floor texture don't count as structure
    k = 5
    pad = np.pad(lum, k // 2, mode="edge")
    blur = sum(pad[y:y + lum.shape[0], x:x + lum.shape[1]] for y in range(k) for x in range(k)) / (k * k)
    gy, gx = np.gradient(blur)
    grad = np.hypot(gx, gy) * 4
    ny, nx = h // TILE, WIDTH // TILE
    g = grad[:ny * TILE, :nx * TILE].reshape(ny, TILE, nx, TILE).mean(axis=(1, 3))
    c = rgb[:ny * TILE, :nx * TILE].reshape(ny, TILE, nx, TILE, 3).mean(axis=(1, 3))
    return g, c, im


def measure(path: Path):
    g, c, im = tiles(path)
    quiet = g < EDGE
    if not quiet.any():
        return 0.0, 0.0, quiet, im
    # the commonest tone among quiet tiles is "the floor"; quiet tiles of another tone (a rug, water, a path) are relief
    q = c[quiet]
    key = (q // 24).astype(int)
    vals, counts = np.unique(key, axis=0, return_counts=True)
    floor = q[(key == vals[counts.argmax()]).all(axis=1)].mean(axis=0)
    plain = quiet & (np.linalg.norm(c - floor, axis=2) < TONE)
    # largest 4-connected plain area
    seen = np.zeros_like(plain, dtype=bool)
    best = 0
    ny, nx = plain.shape
    for y in range(ny):
        for x in range(nx):
            if plain[y, x] and not seen[y, x]:
                stack = [(y, x)]
                seen[y, x] = True
                n = 0
                while stack:
                    a, b = stack.pop()
                    n += 1
                    for da, db in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        u, v = a + da, b + db
                        if 0 <= u < ny and 0 <= v < nx and plain[u, v] and not seen[u, v]:
                            seen[u, v] = True
                            stack.append((u, v))
                best = max(best, n)
    total = ny * nx
    return plain.sum() / total, best / total, plain, im


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("folder")
    ap.add_argument("--match", help="comma-separated substrings; a still must contain one")
    ap.add_argument("--max-blob", type=float, default=0.22)
    ap.add_argument("--mask", help="write a sheet with plain tiles tinted red")
    args = ap.parse_args()
    want = args.match.split(",") if args.match else None
    paths = sorted(p for p in Path(args.folder).glob("*.png") if not want or any(w in p.name for w in want))
    if not paths:
        print("LOOK_DENSITY no stills", file=sys.stderr)
        return 2
    worst = 0.0
    failed = []
    marked = []
    for p in paths:
        share, blob, plain, im = measure(p)
        worst = max(worst, blob)
        flag = "EMPTY" if blob > args.max_blob else "ok"
        if blob > args.max_blob:
            failed.append(p.stem)
        print(f"{flag:5} plain={share:.2f} largest={blob:.2f}  {p.stem}")
        if args.mask:
            a = np.asarray(im, dtype=np.float32).copy()
            big = np.kron(plain, np.ones((TILE, TILE), dtype=bool))
            hh, ww = big.shape
            a[:hh, :ww][big] = a[:hh, :ww][big] * 0.55 + np.array([255, 40, 40], dtype=np.float32) * 0.45
            marked.append(Image.fromarray(a.astype(np.uint8)))
    if args.mask and marked:
        cols = 4
        w, h = marked[0].size
        w2, h2 = w // 2, h // 2
        rows = (len(marked) + cols - 1) // cols
        sheet = Image.new("RGB", (cols * w2, rows * h2))
        for i, m in enumerate(marked):
            sheet.paste(m.resize((w2, h2)), ((i % cols) * w2, (i // cols) * h2))
        sheet.save(args.mask)
    print(f"LOOK_DENSITY stills={len(paths)} worst={worst:.2f} limit={args.max_blob:.2f} empty={len(failed)}" + (" " + ",".join(failed) if failed else ""))
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
