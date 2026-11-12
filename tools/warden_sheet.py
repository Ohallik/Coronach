#!/usr/bin/env python3
"""The Last Warden's portrait atlas (chapter 7): one mask, sixteen cells, two lights.

The Warden is not a face (docs/writing/pending/ch07-manifest.md, "Sheets, for the artist"): a bronze-and-glass mask whose
only expression is the light behind its glass slot. tools/meshy/species_batch.py makes the mask once, by text2image, in
its level white light (Warden_Mask.png). This script tiles that same mask into the 4x4 expression order every portrait
set uses, and re-lights the glass low and amber in the Tender cell only (row 4, column 1). Every other cell repeats
Neutral, as the manifest allows. Local and free; rerun it whenever the mask changes.

  python tools/warden_sheet.py
  python tools/warden_sheet.py --debug <folder>   # also writes the found glass slot and the Tender cell large, to check
"""
from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
REFS = ROOT / "art-src" / "Generated" / "C14-Oriel" / "refs"
SHEET, CELL, FILL = 1024, 256, 0.80          # the other atlases are 1024 square; the mask fills 80% of its cell's height
TENDER = 12                                  # cells read left to right, top to bottom: row 4, column 1
# (core, edge) of the light across the slot's height. The mask comes back with the glass pure #FFFFFF, which on a white
# sheet reads as a hole, and which the portrait intake's pocket keyer (tools/c10_intake.py key_pockets) may cut as
# background; so Neutral is a cold white with a faint blue in it: still white and level, but light rather than paper.
COLD = (np.array([236, 244, 255]), np.array([196, 220, 244]))
AMBER = (np.array([204, 124, 42]), np.array([132, 68, 20]))   # the gardeners' asking light: low, warm, steady


def background(rgb: np.ndarray) -> np.ndarray:
    """The paper: near-white pixels connected to the image border. White inside the mask (the lit glass) is not paper."""
    white = (rgb > 236).all(axis=2)
    labels, _ = ndimage.label(white)
    edge = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    return np.isin(labels, edge[edge > 0])


def glass_slot(rgb: np.ndarray, paper: np.ndarray) -> np.ndarray:
    """The lit glass: pale, bright pixels enclosed by bronze. Bronze highlights and verdigris are never this pale."""
    pale = (rgb.max(axis=2) > 196) & (rgb.min(axis=2) > 168) & ~paper
    labels, count = ndimage.label(pale)
    if not count:
        raise SystemExit("WARDEN_NO_SLOT: no enclosed light found in the mask")
    sizes = ndimage.sum(pale, labels, range(1, count + 1))
    keep = [i + 1 for i, s in enumerate(sizes) if s >= 0.25 * sizes.max()]   # the slot, even if a rivet splits it
    return np.isin(labels, keep)


def relight(rgb: np.ndarray, slot: np.ndarray, light: tuple[np.ndarray, np.ndarray]) -> np.ndarray:
    """Light the glass: brightest along the slot's middle line, falling off toward its top and bottom edges, level from
    end to end (the light is steady), feathered a pixel or two into the bronze frame."""
    core, edge = light
    alpha = np.asarray(Image.fromarray((ndimage.binary_dilation(slot, iterations=2) * 255).astype(np.uint8))
                       .filter(ImageFilter.GaussianBlur(1.5)), dtype=np.float32)[..., None] / 255.0
    rows = np.nonzero(slot)[0]
    middle, half = (rows.min() + rows.max()) / 2, max(1.0, (rows.max() - rows.min()) / 2)
    d = np.clip(np.abs(np.arange(rgb.shape[0]) - middle) / half, 0, 1)[:, None, None] ** 1.5
    glow = core + (edge - core) * d
    return (rgb * (1 - alpha) + glow * alpha).round().clip(0, 255).astype(np.uint8)


def cell_image(rgb: np.ndarray, box: tuple[int, int, int, int]) -> Image.Image:
    crop = Image.fromarray(rgb).crop(box)
    scale = min(CELL * FILL / crop.height, CELL * 0.86 / crop.width)
    crop = crop.resize((max(1, round(crop.width * scale)), max(1, round(crop.height * scale))), Image.LANCZOS)
    cell = Image.new("RGB", (CELL, CELL), "white")
    cell.paste(crop, ((CELL - crop.width) // 2, (CELL - crop.height) // 2))
    return cell


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mask", default=str(REFS / "Warden_Mask.png"))
    parser.add_argument("--out", default=str(REFS / "Warden_Natural.png"))
    parser.add_argument("--debug", help="folder for the slot overlay and a large Tender cell")
    a = parser.parse_args()
    rgb = np.asarray(Image.open(a.mask).convert("RGB")).copy()
    paper = background(rgb)
    rgb[paper] = 255                                          # clean paper, as the other atlases have
    ys, xs = np.nonzero(~paper)
    pad = 4
    box = (max(0, xs.min() - pad), max(0, ys.min() - pad), min(rgb.shape[1], xs.max() + pad + 1), min(rgb.shape[0], ys.max() + pad + 1))
    slot = glass_slot(rgb, paper)
    sy, sx = np.nonzero(slot)
    print(f"WARDEN_MASK box={box} slot_pixels={int(slot.sum())} slot_box=({sx.min()},{sy.min()},{sx.max()},{sy.max()})")
    neutral, tender = cell_image(relight(rgb, slot, COLD), box), cell_image(relight(rgb, slot, AMBER), box)
    sheet = Image.new("RGB", (SHEET, SHEET), "white")
    for i in range(16):
        sheet.paste(tender if i == TENDER else neutral, ((i % 4) * CELL, (i // 4) * CELL))
    Path(a.out).parent.mkdir(parents=True, exist_ok=True)
    sheet.save(a.out)
    if a.debug:
        folder = Path(a.debug)
        folder.mkdir(parents=True, exist_ok=True)
        overlay = rgb.copy()
        overlay[slot] = [255, 0, 255]
        Image.fromarray(overlay).crop(box).save(folder / "warden_slot_overlay.png")
        Image.fromarray(relight(rgb, slot, COLD)).crop(box).save(folder / "warden_neutral_large.png")
        Image.fromarray(relight(rgb, slot, AMBER)).crop(box).save(folder / "warden_tender_large.png")
    print("WARDEN_OK", a.out, flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
