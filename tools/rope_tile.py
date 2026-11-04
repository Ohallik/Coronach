"""Cut a seamless rope tile from a generated side-view rope sheet (November 4, Tallow's docking line).

The rope band is found by its non-white rows; the braid period is measured by autocorrelating the band's luminance
along x, and the tile spans a whole number of periods with a short cross-fade at the seam, so a LineRenderer in Tile
mode repeats it without a visible joint. Usage: python tools/rope_tile.py <sheet.png> <out_base_color.png> [periods]
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def main() -> int:
    source, target = Path(sys.argv[1]), Path(sys.argv[2])
    periods = int(sys.argv[3]) if len(sys.argv) > 3 else 4
    image = np.asarray(Image.open(source).convert("RGB")).astype(np.float32)
    white = (image.min(axis=2) > 235)
    rows = np.where((~white).mean(axis=1) > 0.9)[0]
    top, bottom = int(rows.min()) + 3, int(rows.max()) - 3
    band = image[top:bottom]
    luminance = band.mean(axis=(0, 2))
    luminance = luminance - luminance.mean()
    best, period = -1e9, 0
    for lag in range(40, band.shape[1] // 3):
        score = float(np.dot(luminance[:-lag], luminance[lag:]) / (len(luminance) - lag))
        if score > best:
            best, period = score, lag
    # Refine to sub-pixel by averaging the best lags of later multiples.
    span = period * periods
    start = (band.shape[1] - span) // 2
    tile = band[:, start:start + span].copy()
    fade = max(8, period // 4)
    # Cross-fade the tile's last columns into the band just before its start, so tile[-1] meets tile[0].
    before = band[:, start - fade:start]
    weights = np.linspace(0, 1, fade)[None, :, None]
    tile[:, -fade:] = tile[:, -fade:] * (1 - weights) + before * weights
    out = Image.fromarray(np.clip(tile, 0, 255).astype(np.uint8)).resize((512, 128), Image.LANCZOS)
    target.parent.mkdir(parents=True, exist_ok=True)
    out.save(target)
    print(f"ROPE_TILE band={top}..{bottom} period={period}px periods={periods} span={span}px aspect={span / (bottom - top):.3f} -> {target}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
