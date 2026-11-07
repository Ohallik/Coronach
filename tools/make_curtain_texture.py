#!/usr/bin/env python3
"""The strip a curtain of relay light is drawn with: white, with a soft bell of alpha across it and nothing along it. A LineRenderer's V runs
across the line, so a strand is soft at both edges, and overlapping strands make one rippling sheet that the strand's colour fades upward.
URP's particle shader ignores texture tiling, so no window on the flare could do this. A gradient, not art: python tools/make_curtain_texture.py
"""
from math import exp
from pathlib import Path

from PIL import Image

OUT = Path(__file__).resolve().parents[1] / "Lattice/Assets/_Project/Art/Particles/curtain_soft.png"
W, H = 8, 128
image = Image.new("RGBA", (W, H))
for y in range(H):
    v = (y + .5) / H
    alpha = round(255 * exp(-((v - .5) / .26) ** 2))
    for x in range(W):
        image.putpixel((x, y), (255, 255, 255, alpha))
image.save(OUT)
print("CURTAIN_TEXTURE_OK", OUT)
