#!/usr/bin/env python3
"""Meret Venn's emergency frame (MeretWell) given back her own head, and legs that aren't white (chapter 10 look pass,
November 21).

In the whole-game capture of November 20 (Builds/quality/nov20-whole/s67/116) she fought the last duel as a pale figure
with no face or hair a player could see: Meshy's repaint of her body for the frame (tools/c17_intake.py, RESKIN
"MeretWell") painted her silver fronds as a smooth white cap, her face a dark slate with gold eyes, and white armoured
boots to the knee, so from the fixed camera her head, coat and legs were one white. Her ordinary body (MeretBody,
chapter 5) is the portrait's: lavender-grey face, silver fronds cut by dark grooves. Both bodies are one mesh on one UV
layout (MeretWell_clean.fbx is MeretBody_clean.fbx byte for byte), so a triangle's pixels are the same pixels in both
textures. This tool:

  * the head: every triangle over the collar takes MeretBody's pixels (her face, her fronds, her neck), except the
    frame's own dark teal collar, which keeps the frame's;
  * the boots: below the boots' tops, the white plate turns brushed slate (her trousers' family, as the portrait's body
    reference has her legs), the gold trim and the teal kept;
  * the coat: its white a little under paper white, so the toon ramp shades it instead of clipping it flat;
  * the frame's lights: its cyan core and teal-lit gems and joints go into the emission mask, so they glow as the
    generation prompt asked ("a round cyan core light at the sternum, teal-lit joints").

The triangles come from tools/blender/dump_uv_triangles.py run on the clean model:

  scripts/blender.ps1 -Script tools/blender/dump_uv_triangles.py -TimeoutSec 300 -- --input Lattice/Assets/_Project/Art/Generated/Models/MeretWell/MeretWell_clean.fbx --output <tris.npz>
  python tools/meret_grade.py --triangles <tris.npz>            # writes art-src's albedo and emission and the staged copies

Run it on the intake's own albedo (tools/c17_intake.py prepare writes art-src/Generated/C17-Stillwater/MeretWell_albedo.png
from Meshy's base colour): a later `prepare` of MeretWell undoes this, so run this again after one.
"""
from __future__ import annotations

import argparse
import colorsys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "art-src/Generated/C17-Stillwater"
STAGED = ROOT / "Lattice/Assets/_Project/Art/Generated/Models/MeretWell"
BODY = ROOT / "Lattice/Assets/_Project/Art/Generated/Models/MeretBody/MeretBody_base_color.png"
# Heights on the 1.85 m body in its bind pose (Blender's Z up; checked against the body's own colour by height): the
# frame's collar tops out about 1.62, the boots' tops (their gold V) about .57.
HEAD_FROM, BOOTS_TO = 1.575, .585
# The boots' brushed slate (her trousers' family) and how far under paper white the coat's white is held.
SLATE = np.array([104, 116, 132], np.float32)
COAT_WHITE = .92


def hsv(rgb: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    r, g, b = (rgb[..., i] / 255.0 for i in range(3))
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    v = mx
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    d = np.maximum(mx - mn, 1e-6)
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    return h, s, v


def label_map(pos, uv, labels, size):
    """Each triangle's label painted over its UV triangle (0 is nobody's), then grown 4 px into the gutters so a mip of a
    seam reads the right side's colour."""
    img = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(img)
    for t in range(len(pos)):
        pts = [(float(u) * size, (1 - float(v)) * size) for u, v in uv[t]]
        draw.polygon(pts, fill=int(labels[t]))
    grown = img
    for _ in range(4):
        grown = Image.fromarray(np.where(np.asarray(grown) == 0, np.asarray(grown.filter(ImageFilter.MaxFilter(3))), np.asarray(grown)))
    return np.asarray(grown)


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--triangles", required=True)
    p.add_argument("--well", default=str(SRC / "MeretWell_albedo.png"))
    p.add_argument("--well-emission", default=str(SRC / "MeretWell_emission.png"))
    p.add_argument("--body", default=str(BODY))
    p.add_argument("--out", default=None, help="one albedo to write instead of art-src's and the staged copy")
    p.add_argument("--emission-out", default=None)
    a = p.parse_args()
    # The intake's own albedo and emission are kept beside them the first time (…_ungraded.png) and always read from there,
    # so a second run grades the same source and not its own output.
    for name in ("well", "well_emission"):
        given = Path(getattr(a, name))
        kept = given.with_name(given.stem + "_ungraded.png")
        if given.parent == SRC and given.exists():
            if not kept.exists():
                kept.write_bytes(given.read_bytes())
            setattr(a, name, str(kept))
    d = np.load(a.triangles)
    pos, uv = d["pos"], d["uv"]
    well = np.asarray(Image.open(a.well).convert("RGB")).astype(np.float32)
    body = np.asarray(Image.open(a.body).convert("RGB")).astype(np.float32)
    if well.shape != body.shape:
        raise SystemExit(f"MERET_GRADE_SIZE well {well.shape} body {body.shape}")
    size = well.shape[0]
    cen = pos.mean(1)
    # Each triangle's own mean colour in the frame's texture (sampled at its corners and middle).
    probe = np.concatenate([uv, uv.mean(1, keepdims=True)], 1)
    px = np.clip((probe[..., 0] * (size - 1)).astype(int), 0, size - 1)
    py = np.clip(((1 - probe[..., 1]) * (size - 1)).astype(int), 0, size - 1)
    mean = well[py, px].mean(1)
    _, ts, tv = hsv(mean)
    teal_dark = (ts > .38) & (tv < .5)
    labels = np.full(len(pos), 3, np.uint8)  # 3: the coat and everything else
    labels[(cen[:, 2] > HEAD_FROM) & ~teal_dark] = 1  # 1: her head and neck, from her own body
    labels[(cen[:, 2] < BOOTS_TO)] = 2  # 2: the boots
    lab = label_map(pos, uv, labels, size)
    out = well.copy()
    h, s, v = hsv(well)
    head = lab == 1
    out[head] = body[head]
    boots = lab == 2
    gold = (h > 25) & (h < 65) & (s > .32) & (v > .35)
    teal = (h > 160) & (h < 215) & (s > .3)
    plate = boots & ~gold & ~teal & (s < .3)
    # The plate's light and dark kept (its seams, its scuffs): its value carried onto the slate.
    out[plate] = SLATE[None, :] * (v[plate, None] / .86).clip(.35, 1.12)
    coat = (lab == 3) & (s < .16) & (v > .8)
    out[coat] = well[coat] * COAT_WHITE
    # The frame's lights: its cyan and teal-lit points, bright and saturated, never on the head.
    emission = np.asarray(Image.open(a.well_emission).convert("RGB")).astype(np.float32) if Path(a.well_emission).exists() else np.zeros_like(well)
    if emission.shape != well.shape:
        emission = np.asarray(Image.fromarray(emission.astype(np.uint8)).resize((size, size))).astype(np.float32)
    lit = (h > 168) & (h < 200) & (s > .45) & (v > .55) & (lab != 1)
    lit_img = Image.fromarray((lit * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
    lit = np.asarray(lit_img) > 127
    emission[lit] = np.maximum(emission[lit], 255)
    targets = [Path(a.out)] if a.out else [SRC / "MeretWell_albedo.png", STAGED / "MeretWell_base_color.png"]
    glow = [Path(a.emission_out)] if a.emission_out else ([] if a.out else [SRC / "MeretWell_emission.png", STAGED / "MeretWell_emission.png"])
    for t in targets:
        Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(t)
    for t in glow:
        Image.fromarray(emission.clip(0, 255).astype(np.uint8)).save(t)
    print(f"MERET_GRADE_OK head={head.mean():.3f} boots={plate.mean():.3f} coat={coat.mean():.3f} lit={lit.mean():.4f} "
          f"triangles head={int((labels == 1).sum())} boots={int((labels == 2).sum())} wrote={','.join(str(t) for t in targets + glow)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
