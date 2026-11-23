#!/usr/bin/env python3
"""Orrin's body given his portrait's fronds and his reference's trousers (chapter 10 look pass, November 21).

In the whole-game capture (Builds/quality/nov20-whole/s69/014) the reviewers took the man under Orrin's name for somebody
else: a brown-skinned man in tan. It is Orrin's own body (Prefabs/Characters/Orrin, from art-src/Generated/P3-Orrin), but
Meshy painted his silver fronds a dark slate and his charcoal trousers the tan of his wrap, so under the neighbourhood's warm
sun he read as one tan figure with a dark head. His portrait and his body reference (art-src/Generated/P1-production/refs/
orrin-v2.png) have silver-white fronds over grey-blue skin and dark trousers under the tan wrap. This tool, on his own
texture and his own UVs:

  * the fronds (the head over the brow, and its top and back) taken to his portrait's silver, their grooves kept;
  * the trousers (hip to the boots' tops, the tan in them) taken to charcoal, their folds kept.

His face, skin, wrap, sash and boots are left as they are. The triangles come from tools/blender/dump_uv_triangles.py run on
Lattice/Assets/_Project/Art/Generated/Models/Orrin/Orrin_clean.fbx.

  python tools/orrin_grade.py --triangles <tris.npz>     # writes art-src's and the staged base colour

The source is kept beside it the first time (Orrin_base_color_ungraded.png) and always read from there.
"""
from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image

import meret_grade as mg

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "art-src/Generated/P3-Orrin/Orrin_base_color.png"
STAGED = ROOT / "Lattice/Assets/_Project/Art/Generated/Models/Orrin/Orrin_base_color.png"
# On the 1.9 m body in its bind pose (Blender's Z up, his face toward -Y): the brow, his hips, his boots' tops.
BROW, HIPS, BOOTS = 1.745, 1.0, .42
SILVER = np.array([206, 212, 218], np.float32)
CHARCOAL = np.array([58, 60, 66], np.float32)


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--triangles", required=True)
    p.add_argument("--out", default=None)
    a = p.parse_args()
    kept = SRC.with_name(SRC.stem + "_ungraded.png")
    if not kept.exists():
        kept.write_bytes(SRC.read_bytes())
    tex = np.asarray(Image.open(kept).convert("RGB")).astype(np.float32)
    size = tex.shape[0]
    d = np.load(a.triangles)
    pos, nrm, uv = d["pos"], d["nrm"], d["uv"]
    cen = pos.mean(1)
    # Above the wrap's collar; his face (toward -Y, under the brow) is left out.
    head = (cen[:, 2] > 1.63) & (np.abs(cen[:, 0]) < .22)
    fronds = head & ((cen[:, 2] > 1.8) | (nrm[:, 1] > .1) | (nrm[:, 2] > .65) | ((np.abs(nrm[:, 0]) > .6) & (cen[:, 2] > BROW)))
    legs = (cen[:, 2] > BOOTS) & (cen[:, 2] < HIPS) & (np.abs(cen[:, 0]) < .3)
    # Everything else is labelled too (3), so the seams' growth never runs into another island.
    labels = np.full(len(pos), 3, np.uint8)
    labels[fronds] = 1
    labels[legs] = 2
    lab = mg.label_map(pos, uv, labels, size)
    h, s, v = mg.hsv(tex)
    out = tex.copy()
    # Only the fronds' own slate (never a scrap of the wrap's tan or a lit eye that shares a triangle).
    f = (lab == 1) & (v < .58) & (s < .5) & ~((h > 12) & (h < 50) & (s > .18))
    # The fronds' light and dark kept, carried up to silver (their darkest grooves still a mid grey).
    out[f] = SILVER[None, :] * (.42 + .7 * (v[f, None] / max(.05, float(np.percentile(v[f], 95))))).clip(.38, 1.08)
    tan = (lab == 2) & (h > 12) & (h < 50) & (s > .14) & (v > .28)
    out[tan] = CHARCOAL[None, :] * (v[tan, None] / .5).clip(.5, 1.5)
    targets = [Path(a.out)] if a.out else [SRC, STAGED]
    for t in targets:
        Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(t)
    print(f"ORRIN_GRADE_OK fronds={f.mean():.3f} trousers={tan.mean():.3f} wrote={','.join(str(t) for t in targets)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
