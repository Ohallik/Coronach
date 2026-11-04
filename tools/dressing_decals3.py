"""Cut the C8 dressing-3 decal sheets (Hushwell and the Gullet) into game textures (free, local).

Patch sheets (background removed) are cut per grid cell, not per connected region:
background removal erased two of the six cave patches (the damp seep and the crystal
shards), so a cell is kept only when it still holds a real patch. Patches are feathered
like the Sorrel ground patches. The mine rails and the coil scour are white-background
tiles: alpha comes from how far a pixel is from white, and both are cropped across to
their band and along to a whole number of their own measured periods so they repeat
without a seam. Emission masks come from the cyan parts.

  python tools/dressing_decals3.py art-src/Generated/C8-Dressing3/refs art-src/Generated/C8-Dressing3/decals
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

import emission_mask
from dressing_decals import period, save

# Grid order (row-major, 3x2); None = the cell came back empty or a ghost.
CAVE = ["GroundBasalt", "GroundDust", "GroundCrust", None, None, "GroundFissure"]
GULLET = ["GroundVeins", "GroundPool", "GroundEggs", "GroundSilt", "GroundPore", "GroundScar"]
GLOW = {"GroundVeins": "cyan", "GroundPore": "cyan"}
MIN_COVER = 0.12  # share of the cell's patch box that must be solid for a cell to count
THIN = {"GroundFissure"}  # line art: feathering erodes the cracks away


def cells(rgba, names, out, feather):
    h, w = rgba.shape[:2]
    for index, name in enumerate(names):
        r, c = divmod(index, 3)
        cell = rgba[r * h // 2:(r + 1) * h // 2, c * w // 3:(c + 1) * w // 3]
        solid = cell[:, :, 3] > 40
        if name is None:
            print("CELL_SKIPPED", index, "cover", round(float(solid.mean()), 3))
            continue
        ys, xs = np.where(solid)
        if len(ys) == 0 or solid.mean() < MIN_COVER * 0.25:
            raise SystemExit(f"{name}: cell {index} is empty")
        pad = 6
        crop = cell[max(0, ys.min() - pad):ys.max() + pad, max(0, xs.min() - pad):xs.max() + pad].copy()
        if feather and name not in THIN:
            alpha = crop[:, :, 3].astype(np.float32)
            alpha = ndimage.grey_erosion(alpha, size=(7, 7))
            alpha = ndimage.gaussian_filter(alpha, 7)
            crop[:, :, 3] = np.clip((alpha - 30) * 1.3, 0, 255).astype(np.uint8)
        path = save(crop, out / f"{name}_base_color.png")
        if name in GLOW:
            emission_mask.extract(path, out / f"{name}_emission.png", GLOW[name])
        if name == "GroundEggs":
            pale_glow(path, out / f"{name}_emission.png")
        if name == "GroundVeins":
            vessels_only(path)


def vessels_only(path):
    """The vein sheet paints its vessels on its own dark tissue, which read as a dark halo on the membrane:
    keep the cyan vessels solid and fade the tissue around them to a faint tint."""
    a = np.asarray(Image.open(path).convert("RGBA")).astype(np.float32)
    r, g, b, alpha = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    vessel = np.clip(((g + b) / 2 - r - 40) / 80, 0, 1)
    vessel = ndimage.gaussian_filter(ndimage.grey_dilation(vessel, size=(5, 5)), 1.5)
    a[..., 3] = np.clip(alpha * np.maximum(0.22, vessel), 0, 255)
    Image.fromarray(a.astype(np.uint8)).save(path)
    print("VESSELS_ONLY", Path(path).name, "solid share", round(float((a[..., 3] > 128).mean()), 3))


def pale_glow(source, destination):
    """The eggs glow a pale blue-white the cyan key misses: bright, bluer than red, solid."""
    a = np.asarray(Image.open(source).convert("RGBA")).astype(np.float32)
    r, g, b, alpha = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    mask = ndimage.binary_opening(((g + b) / 2 > 165) & (b > r + 15) & (alpha > 128), iterations=1)
    out = np.zeros(a.shape[:2] + (3,), np.uint8)
    out[mask] = a[mask][:, :3].astype(np.uint8)
    Image.fromarray(out).save(destination)
    print("EMISSION_MASK_OK", Path(destination).name, "coverage", round(float(mask.mean()), 4))


def band_tile(src, name, out, glow=None, fade=0.0):
    rgb = np.asarray(Image.open(src).convert("RGB")).astype(np.float32)
    ink = np.clip((rgb.min(axis=2) * -1 + 245) * 2.2, 0, 255)  # distance from white
    cols = np.where(ink.mean(axis=0) > 25)[0]
    x0, x1 = cols.min(), cols.max() + 1
    py, ey = period(ink[:, x0:x1], 0, 40, 500)
    k = max(1, rgb.shape[0] // py)
    print(name, "BAND", x0, x1, "PERIOD", py, round(ey, 2), "periods", k)
    alpha = ink[: k * py, x0:x1]
    if fade > 0:
        # Worn grooves, not a ribbon: the band fades into the surface toward both of its edges.
        u = np.linspace(0, 1, alpha.shape[1])
        edge = np.clip(np.minimum(u, 1 - u) / fade, 0, 1)
        alpha = alpha * (edge * edge * (3 - 2 * edge))[None, :]
    tile = np.dstack([rgb[: k * py, x0:x1].astype(np.uint8), alpha.astype(np.uint8)])
    image = Image.fromarray(tile, "RGBA")
    aspect = image.width / image.height
    size = (512, max(64, round(512 / aspect))) if aspect >= 1 else (max(64, round(512 * aspect)), 512)
    image.resize(size, Image.Resampling.LANCZOS).save(out / f"{name}_base_color.png")
    print("DECAL", f"{name}_base_color.png", size, "aspect", round(aspect, 3))
    if glow:
        emission_mask.extract(out / f"{name}_base_color.png", out / f"{name}_emission.png", glow)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("refs")
    p.add_argument("out")
    a = p.parse_args()
    refs, out = Path(a.refs), Path(a.out)
    out.mkdir(parents=True, exist_ok=True)
    cells(np.asarray(Image.open(refs / "cave-patches.png").convert("RGBA")), CAVE, out, True)
    cells(np.asarray(Image.open(refs / "gullet-patches.png").convert("RGBA")), GULLET, out, True)
    band_tile(refs / "mine-rails.png", "DecalRails", out)
    band_tile(refs / "gullet-scour.png", "GroundScour", out, "cyan", fade=0.3)


if __name__ == "__main__":
    main()
