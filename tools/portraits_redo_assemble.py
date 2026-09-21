"""P65-era portrait redo, step 2: clean the 4x upscales and assemble sheets.

- Uniform tight crop: one union content-bbox across ALL cells (squared, small
  pad) so faces fill the 576 frame like the POLISH-03 sheets while keeping
  cell-to-cell alignment identical to the generated sheet.
- POLISH-03 load_clean edge treatment (despeckle, ~3px erode, rounded alpha,
  defringe) ported unchanged from portraits_hd_assemble.py.
- Outputs: the 4x2 canonical game sheet (2304x1152) + the full 4x4 bank +
  a half-scale review strip.

Usage: python portraits_redo_assemble.py <updir> <prefix> <outdir> <sheetname>
       <cell-index list for the 8 canonical slots, e.g. 0,1,3,4,5,6,12,7>
"""
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

CELL = 576
PAD = 14  # px of breathing room around the union bbox, at final 576 scale


def clean(im):
    """POLISH-03 edge treatment on a 576px RGBA cell."""
    a = im.getchannel("A")
    a2 = a.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
    a2 = a2.filter(ImageFilter.MinFilter(7))
    a2 = a2.filter(ImageFilter.GaussianBlur(2.6))
    arr = np.asarray(a2).astype(np.float32) / 255.0
    lo, hi = 0.32, 0.68
    t = np.clip((arr - lo) / (hi - lo), 0.0, 1.0)
    aa = (t * t * (3 - 2 * t) * 255.0).astype(np.uint8)
    rgba = np.asarray(im).copy()
    solid = aa >= 250
    rgb = rgba[..., :3].astype(np.float32)
    w = solid.astype(np.float32)
    rgb_img = Image.fromarray((rgb * w[..., None]).astype(np.uint8))
    w_img = Image.fromarray((w * 255).astype(np.uint8))
    for _ in range(3):
        rgb_img = rgb_img.filter(ImageFilter.GaussianBlur(4))
        w_img = w_img.filter(ImageFilter.GaussianBlur(4))
    rgb_b = np.asarray(rgb_img).astype(np.float32)
    w_b = np.asarray(w_img).astype(np.float32)[..., None] / 255.0
    spread = rgb_b / np.maximum(w_b, 1e-4)
    edge = (aa > 0) & ~solid
    rgba[..., :3][edge] = np.clip(spread[edge], 0, 255).astype(np.uint8)
    rgba[..., 3] = aa
    rgba[..., :3][aa == 0] = 0
    return Image.fromarray(rgba)


def main():
    updir, prefix, outdir, sheetname = sys.argv[1:5]
    slots = [int(s) for s in sys.argv[5].split(",")]
    assert len(slots) == 8, "need 8 canonical cell indices"
    os.makedirs(outdir, exist_ok=True)

    raw = {}
    i = 0
    while os.path.exists(os.path.join(updir, f"{prefix}__{i}.png")):
        raw[i] = Image.open(os.path.join(updir, f"{prefix}__{i}.png")).convert("RGBA")
        i += 1
    print("cells:", len(raw))

    # union content bbox across every cell, squared + padded, one crop for all
    l = t = 10 ** 9
    r = b = -1
    for im in raw.values():
        bx = im.getchannel("A").getbbox()
        l, t = min(l, bx[0]), min(t, bx[1])
        r, b = max(r, bx[2]), max(b, bx[3])
    size = next(iter(raw.values())).size[0]
    side = max(r - l, b - t)
    cx, cy = (l + r) // 2, (t + b) // 2
    half = side // 2 + int(PAD * side / CELL)
    box = (max(0, cx - half), max(0, cy - half),
           min(size, cx + half), min(size, cy + half))
    print("union crop:", box, "of", size)

    cells = {}
    for i, im in raw.items():
        c = im.crop(box).resize((CELL, CELL), Image.LANCZOS)
        cells[i] = clean(c)

    # canonical 4x2 game sheet: Neutral,Happy,Sad,Angry,Shocked,Worried,Intense,Tech
    game = Image.new("RGBA", (4 * CELL, 2 * CELL), (0, 0, 0, 0))
    for slot, idx in enumerate(slots):
        game.paste(cells[idx], ((slot % 4) * CELL, (slot // 4) * CELL), cells[idx])
    game.save(os.path.join(outdir, sheetname + ".png"))
    print("game sheet:", sheetname + ".png")

    # full bank, source order
    cols, rows = 4, (len(cells) + 3) // 4
    bank = Image.new("RGBA", (cols * CELL, rows * CELL), (0, 0, 0, 0))
    for i, im in cells.items():
        bank.paste(im, ((i % cols) * CELL, (i // cols) * CELL), im)
    bank.save(os.path.join(outdir, sheetname + "_Bank.png"))
    print("bank sheet:", sheetname + "_Bank.png")

    review = game.resize((2 * CELL, CELL), Image.LANCZOS)
    bg = Image.new("RGB", review.size, (40, 44, 52))
    bg.paste(review, (0, 0), review)
    bg.save(os.path.join(outdir, sheetname + "_review.png"))
    print("review:", sheetname + "_review.png")


if __name__ == "__main__":
    main()
