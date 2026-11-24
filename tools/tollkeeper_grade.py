#!/usr/bin/env python3
"""The Tollkeeper's white hull without Meshy's tan flecks (November 23, D382).

In the whole-game capture of November 22 (Builds/quality/nov22b-whole/s65/057) the carrier's white plating along its
engine pods and over its window strip carried small tan and brown patches that read at play size as dirt on the
texture. They are Meshy's: its repaint scattered flecks of the gold it gave the trim (the engine rings, the panel lines
along the teal bands) over the white plate, a few texels each, with nothing round them but white. The gold that is
drawn on purpose is a ring, a line or a fitting: larger, or touching the teal it edges. So a warm patch is taken off
only when it is small and stands in white on every side, and the plate round it is laid over it (the mean of the white
texels near it, so the plate's own light and shade run on); every ring, line and fitting keeps its gold.

  python tools/tollkeeper_grade.py              # writes art-src's albedo and the staged base colour of both bodies
  python tools/tollkeeper_grade.py --out <dir>  # writes them to <dir> instead, to look at first

The carrier and its outrigger are both done (the outrigger's arms are the same plate). Each source is kept beside it the
first time (..._albedo_ungraded.png) and always read from there; a later intake `prepare` of either undoes this, so run
it again after one.
"""
from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

import meret_grade as mg

ROOT = Path(__file__).resolve().parents[1]
BODIES = {
    "Tollkeeper": (ROOT / "art-src/Generated/C17-Stillwater/Tollkeeper_albedo.png",
                   ROOT / "Lattice/Assets/_Project/Art/Generated/Models/Tollkeeper/Tollkeeper_base_color.png"),
    "TollkeeperOutrigger": (ROOT / "art-src/Generated/C17-Stillwater/TollkeeperOutrigger_albedo.png",
                            ROOT / "Lattice/Assets/_Project/Art/Generated/Models/TollkeeperOutrigger/TollkeeperOutrigger_base_color.png"),
}
# A fleck: warm (tan to gold), not grey, not white; at most this many texels of a 1024 map; and white on this much of
# the ring of texels round it.
MOST, RING, WHITE_ROUND = 320, 3, .55


def grade(tex: np.ndarray) -> tuple[np.ndarray, int, int]:
    h, s, v = mg.hsv(tex)
    size = tex.shape[0]
    most = MOST * (size / 1024) ** 2
    warm = (h > 14) & (h < 62) & (s > .13) & (v > .2)
    white = (s < .16) & (v > .56)
    labels, count = ndimage.label(warm, structure=np.ones((3, 3), bool))
    if count == 0:
        return tex, 0, 0
    areas = ndimage.sum(np.ones_like(labels), labels, index=np.arange(1, count + 1))
    small = np.zeros(count + 1, bool)
    small[1:] = areas <= most
    # How much of each small patch's surround is white.
    grown = ndimage.grey_dilation(np.where(small[labels], labels, 0), size=(2 * RING + 1, 2 * RING + 1))
    ring = (grown > 0) & (labels == 0)
    round_white = ndimage.sum(white & ring, grown, index=np.arange(1, count + 1))
    round_all = ndimage.sum(ring, grown, index=np.arange(1, count + 1))
    keep = np.zeros(count + 1, bool)
    keep[1:] = small[1:] & (round_all > 0) & (round_white >= WHITE_ROUND * np.maximum(round_all, 1))
    fleck = keep[labels]
    # Every fleck takes the mean of the white plate near it.
    weight = ndimage.uniform_filter(white.astype(np.float32), 13)
    out = tex.copy()
    for c in range(3):
        plate = ndimage.uniform_filter(tex[..., c] * white, 13) / np.maximum(weight, 1e-4)
        channel = out[..., c]
        channel[fleck] = plate[fleck]
    return out, int(keep.sum()), int(fleck.sum())


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--out", default=None)
    a = p.parse_args()
    for name, (src, staged) in BODIES.items():
        kept = src.with_name(src.stem + "_ungraded.png")
        if not kept.exists():
            kept.write_bytes(src.read_bytes())
        tex = np.asarray(Image.open(kept).convert("RGB")).astype(np.float32)
        out, patches, texels = grade(tex)
        targets = [Path(a.out) / f"{name}_base_color.png"] if a.out else [src, staged]
        for target in targets:
            Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(target)
        print(f"TOLLKEEPER_GRADE {name} flecks={patches} texels={texels} ({texels / tex.shape[0] / tex.shape[1]:.4f})")
    print("TOLLKEEPER_GRADE_OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
