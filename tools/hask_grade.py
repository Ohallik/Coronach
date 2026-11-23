#!/usr/bin/env python3
"""Hask's body given his portrait's fur (November 21, after the whole-game capture).

At Lastlight's pens (Builds/quality/nov20-whole/s33/105) Hask spoke from a pale cream face (his portrait, and his brief:
"grey-brown fur weathered pale, almost cream at the brow ruff") while the man standing there was a dark hooded lump with an
orange-brown face: Meshy painted his fur the colour of his coat's leather trim. The coat and its hood are the portrait's
(a dark brown-grey hide), so only the fur moves, on his own texture and his own UVs:

  * his head, all of it the head bone carries (Meshy painted its crown the coat's grey as if a hood were up; the portrait's
    hood is a cowl round his neck and his head is all fur), and the warm brown fur of his paws, taken to the portrait's
    cream, their light and dark kept (the darkest clumps a weathered grey-brown, as the portrait's cheeks are);
  * his nose pad (Meshy's slate blue), lips and eyes (the darkest of the head), the cowl, the coat, the toggles and the boots are left as they are.

Which texel is where comes from the bone that carries each triangle (tools/blender/dump_uv_triangles_many.py on
Lattice/Assets/_Project/Art/Generated/Models/HaskBody/HaskBody_clean.fbx).

  python tools/hask_grade.py --triangles <HaskBody_clean.npz>     # writes art-src's albedo and the staged base colour

The source is kept beside it the first time (HaskBody_albedo_ungraded.png) and always read from there; a later intake
`prepare` of HaskBody undoes this, so run it again after one (and tools/townsfolk_dyes.py --bodies HaskBody, then
TownsfolkLooks.Make, for the dyes of his body).
"""
from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image

import meret_grade as mg

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "art-src/Generated/C13-Hoarfell/HaskBody_albedo.png"
STAGED = ROOT / "Lattice/Assets/_Project/Art/Generated/Models/HaskBody/HaskBody_base_color.png"
# The portrait's fur in its light (the brow ruff) and in its shade (the cheeks' grey-brown).
CREAM = np.array([206, 196, 180], np.float32)
SHADE = np.array([128, 116, 104], np.float32)
HEAD = {"Head", "neck", "head_end", "headfront"}
HANDS = {"LeftHand", "RightHand"}


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
    bone = d["bone"]
    labels = np.full(len(bone), 3, np.uint8)
    labels[np.isin(bone, list(HEAD))] = 1
    labels[np.isin(bone, list(HANDS))] = 2
    lab = mg.label_map(d["pos"], d["uv"], labels, size)
    # His face, inside the hood's opening: the head's front-facing triangles in the middle of it, between his chin and his
    # brow. Meshy painted it the hood's cold grey; the portrait's face is all fur.
    pos, nrm = d["pos"], d["nrm"]
    head = np.isin(bone, list(HEAD))
    centre = pos.mean(1)
    zs, xs = centre[head, 2], centre[head, 0]
    lo, hi = np.percentile(zs, 18), np.percentile(zs, 72)
    half = np.percentile(np.abs(xs - np.median(xs)), 75)
    front = head & (nrm[:, 1] < -.35) & (centre[:, 2] > lo) & (centre[:, 2] < hi) & (np.abs(centre[:, 0] - np.median(xs)) < .62 * half)
    labels[front] = 4
    lab = mg.label_map(d["pos"], d["uv"], labels, size)
    h, s, v = mg.hsv(tex)
    # The fur: warm browns and tans on the head and paws (the hood and the coat are a cold dark grey), and the face's grey,
    # all but its darkest (the nose pad, the lips, the eyes).
    # Meshy painted the top of his head the coat's grey as if a hood were up; the portrait's hood is a cowl round his neck and
    # his whole head is fur, so everything the head bone carries is fur but its darkest.
    fur = ((lab == 2) & (h > 8) & (h < 52) & (s > .14) & (v > .22)) | (((lab == 1) | (lab == 4)) & (v > .19) & ~((h > 185) & (h < 255) & (s > .22)))
    ref = float(np.percentile(v[fur], 90)) if fur.any() else 1
    t = np.clip((v[fur] / max(.05, ref) - .35) / .75, 0, 1)[:, None]
    out = tex.copy()
    out[fur] = SHADE[None, :] * (1 - t) + CREAM[None, :] * t
    targets = [Path(a.out)] if a.out else [SRC, STAGED]
    for target in targets:
        Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(target)
    print(f"HASK_GRADE_OK fur={fur.mean():.3f} wrote={','.join(str(t) for t in targets)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
