"""P65-era portrait redo, step 1: slice an AI-generated N x M expression grid
into per-cell RGBA PNGs with the white background keyed out.

Unlike the POLISH-03 extractor (which assumed RMMZ 144px transparent sheets),
this takes a flat-white generated sheet, splits it on an even grid, and keys the
background per cell:
  - flood-fill from the cell border over near-white pixels (the open background),
  - then any remaining near-white connected component bigger than AREA_MIN
    becomes transparent too (enclosed pockets: ear gaps, wrench loop). Small
    near-white islands (teeth, eye catchlights) are kept.
Cells keep their full square canvas — no bbox trimming — so relative scale and
placement across cells survive to the assemble step untouched.

Usage: python portraits_redo_slice.py <sheet.png> <cols> <rows> <outdir> <prefix>
"""
import os
import sys
from collections import deque

import numpy as np
from PIL import Image

WHITE_TOL = 34        # max channel distance from white to count as background
BORDER_TOL = 65       # looser bound for background connected to the cell border
AREA_MIN = 250        # px; enclosed near-white blobs bigger than this are keyed


def key_cell(cell):
    """RGB cell -> RGBA with background transparent."""
    arr = np.asarray(cell.convert("RGB")).astype(np.int16)
    h, w = arr.shape[:2]
    dist = (255 - arr).max(axis=2)
    # background must be NEUTRAL as well as bright: a tinted bright region
    # (Shaza's pale-cyan optics) is character, not background
    spread = arr.max(axis=2) - arr.min(axis=2)
    neutral = spread <= 14
    # components are traced over the looser bound; the strict bound only
    # decides whether an INTERIOR component counts as background at all
    near_white = (dist <= BORDER_TOL) & neutral

    # connected components over the near-white mask (4-neighbour BFS)
    labels = np.zeros((h, w), np.int32)
    sizes = {0: 0}
    touches_border = {0: False}
    nxt = 0
    for sy in range(h):
        for sx in range(w):
            if not near_white[sy, sx] or labels[sy, sx]:
                continue
            nxt += 1
            q = deque([(sy, sx)])
            labels[sy, sx] = nxt
            size, border = 0, False
            while q:
                y, x = q.popleft()
                size += 1
                if y in (0, h - 1) or x in (0, w - 1):
                    border = True
                for ny, nx_ in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                    if 0 <= ny < h and 0 <= nx_ < w \
                            and near_white[ny, nx_] and not labels[ny, nx_]:
                        labels[ny, nx_] = nxt
                        q.append((ny, nx_))
            sizes[nxt] = size
            touches_border[nxt] = border

    def has_holes(comp):
        """True if any non-comp pixel is fully enclosed by comp (e.g. a pupil
        inside an eye-white). Flood the complement from the border; whatever
        the flood can't reach is a hole."""
        reach = np.zeros((h, w), bool)
        q = deque()
        for y in range(h):
            for x in (0, w - 1):
                if not comp[y, x] and not reach[y, x]:
                    reach[y, x] = True
                    q.append((y, x))
        for x in range(w):
            for y in (0, h - 1):
                if not comp[y, x] and not reach[y, x]:
                    reach[y, x] = True
                    q.append((y, x))
        while q:
            y, x = q.popleft()
            for ny, nx_ in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= ny < h and 0 <= nx_ < w \
                        and not comp[ny, nx_] and not reach[ny, nx_]:
                    reach[ny, nx_] = True
                    q.append((ny, nx_))
        return int((~comp & ~reach).sum()) >= 12

    kill = np.zeros((h, w), bool)
    for lab in range(1, nxt + 1):
        comp = labels == lab
        if touches_border[lab]:
            kill |= comp
            continue
        strict = comp & (dist <= WHITE_TOL)
        # enclosed near-white region: background pocket if solid, but an
        # eye-white if it wraps a pupil — protect anything with holes
        if strict.sum() >= AREA_MIN and not has_holes(comp):
            kill |= comp

    rgba = np.dstack([np.asarray(cell.convert("RGB")),
                      np.full((h, w), 255, np.uint8)])
    rgba[..., 3][kill] = 0

    # neighbouring busts bleed slivers over the grid line — but only ever from
    # the cell edge. Kill non-largest opaque components ONLY when they touch
    # the border; interior satellites (Roka's lightning arcs) are character.
    opaque = rgba[..., 3] > 0
    labels2 = np.zeros((h, w), np.int32)
    comp_size = {}
    comp_border = {}
    nxt2 = 0
    for sy in range(h):
        for sx in range(w):
            if not opaque[sy, sx] or labels2[sy, sx]:
                continue
            nxt2 += 1
            q = deque([(sy, sx)])
            labels2[sy, sx] = nxt2
            size, border = 0, False
            while q:
                y, x = q.popleft()
                size += 1
                if y in (0, h - 1) or x in (0, w - 1):
                    border = True
                for ny, nx_ in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                    if 0 <= ny < h and 0 <= nx_ < w \
                            and opaque[ny, nx_] and not labels2[ny, nx_]:
                        labels2[ny, nx_] = nxt2
                        q.append((ny, nx_))
            comp_size[nxt2] = size
            comp_border[nxt2] = border
    if comp_size:
        best_lab = max(comp_size, key=comp_size.get)
        for lab, size in comp_size.items():
            if lab != best_lab and comp_border[lab]:
                rgba[..., 3][labels2 == lab] = 0
    return Image.fromarray(rgba)


def main():
    sheet_path, cols, rows, outdir, prefix = (
        sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5])
    os.makedirs(outdir, exist_ok=True)
    im = Image.open(sheet_path)
    cw, ch = im.width // cols, im.height // rows
    for r in range(rows):
        for c in range(cols):
            cell = im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
            keyed = key_cell(cell)
            i = r * cols + c
            keyed.save(os.path.join(outdir, f"{prefix}__{i}.png"))
            a = np.asarray(keyed.getchannel("A"))
            print(f"cell {i}: opaque {100.0 * (a > 0).mean():.1f}%")
    print("done:", rows * cols, "cells ->", outdir)


if __name__ == "__main__":
    main()
