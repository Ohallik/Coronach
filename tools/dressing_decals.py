"""Cut the C8 dressing-2 decal sheets into individual game textures (free, local).

Sheets with a removed background (floor inserts, ground patches) are split into
their connected alpha regions in reading order. White-on-black pictograms take
their alpha from luminance; the hazard stripe from its yellow paint; the tyre
tracks from their darkness. Tiling textures are cropped to a whole number of
their own measured periods so they repeat without a seam. Emission masks are
extracted from the cyan/amber parts of the floor inserts.

  python tools/dressing_decals.py art-src/Generated/C8-Dressing2/refs art-src/Generated/C8-Dressing2/decals
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

import emission_mask

INSERTS = ["DecalGrate", "DecalHatch", "DecalTread", "DecalTrench", "DecalStud", "DecalDrain"]
PICTOS = ["PictoGalley", "PictoMarket", "PictoRepair", "PictoCabins", "PictoOffice", "PictoDock"]
GROUND = ["GroundGravel", "GroundClay", "GroundStain", "GroundPebbles", "GroundRipples", "GroundScuff"]
GLOW = {"DecalGrate": "cyan", "DecalTrench": "cyan", "DecalStud": "amber"}


def regions(mask, count, dilate):
    labels, _ = ndimage.label(ndimage.binary_dilation(mask, iterations=dilate))
    boxes = [s for s in ndimage.find_objects(labels) if s is not None]
    boxes = sorted(boxes, key=lambda s: (s[0].stop - s[0].start) * (s[1].stop - s[1].start), reverse=True)[:count]
    if len(boxes) != count:
        raise SystemExit(f"found {len(boxes)} regions, expected {count}")
    rows = 2
    row_height = mask.shape[0] / rows
    return sorted(boxes, key=lambda s: (int(((s[0].start + s[0].stop) / 2) // row_height), s[1].start))


def save(rgba, path, size=512):
    image = Image.fromarray(rgba, "RGBA")
    scale = size / max(image.size)
    image = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.Resampling.LANCZOS)
    image.save(path)
    print("DECAL", path.name, image.size)
    return path


def period(gray, axis, low=40, high=520):
    """Shift (pixels) at which the image best repeats itself along an axis."""
    best, best_err = None, None
    n = gray.shape[axis]
    for s in range(low, min(high, n // 2)):
        a = np.take(gray, range(0, n - s), axis=axis)
        b = np.take(gray, range(s, n), axis=axis)
        err = float(np.abs(a - b).mean())
        if best_err is None or err < best_err:
            best, best_err = s, err
    return best, best_err


def main():
    p = argparse.ArgumentParser()
    p.add_argument("refs")
    p.add_argument("out")
    a = p.parse_args()
    refs, out = Path(a.refs), Path(a.out)
    out.mkdir(parents=True, exist_ok=True)

    # Floor inserts and ground patches: real alpha from background removal.
    for sheet, names in (("floor-inserts", INSERTS), ("ground-patches", GROUND)):
        rgba = np.asarray(Image.open(refs / f"{sheet}.png").convert("RGBA"))
        for name, (ys, xs) in zip(names, regions(rgba[:, :, 3] > 40, len(names), 6)):
            crop = rgba[ys, xs].copy()
            if sheet == "ground-patches":
                # Ground patches blend into the soil: pull the matte edge in past its pale fringe, then feather it.
                alpha = crop[:, :, 3].astype(np.float32)
                alpha = ndimage.grey_erosion(alpha, size=(9, 9))
                alpha = ndimage.gaussian_filter(alpha, 10)
                crop[:, :, 3] = np.clip((alpha - 40) * 1.35, 0, 255).astype(np.uint8)
            path = save(crop, out / f"{name}_base_color.png")
            if name in GLOW:
                emission_mask.extract(path, out / f"{name}_emission.png", GLOW[name])

    # Pictograms: white stencil paint; alpha from luminance, colour left white (tinted by material).
    rgb = np.asarray(Image.open(refs / "floor-pictograms.png").convert("RGB")).astype(np.float32)
    lum = rgb.mean(axis=2)
    for name, (ys, xs) in zip(PICTOS, regions(lum > 110, len(PICTOS), 28)):
        alpha = np.clip((lum[ys, xs] - 60) * 2.2, 0, 255).astype(np.uint8)
        crop = np.dstack([np.full_like(alpha, 255)] * 3 + [alpha])
        h, w = alpha.shape
        side = int(max(h, w) * 1.1)
        square = np.zeros((side, side, 4), np.uint8)
        square[(side - h) // 2:(side - h) // 2 + h, (side - w) // 2:(side - w) // 2 + w] = crop
        save(square, out / f"{name}_base_color.png")

    # Hazard stripe: yellow paint over transparent deck; crop to whole periods so it tiles both ways.
    rgb = np.asarray(Image.open(refs / "hazard-stripe.png").convert("RGB")).astype(np.float32)
    yellow = np.clip(((rgb[:, :, 0] + rgb[:, :, 1]) * .5 - rgb[:, :, 2] - 60) * 3, 0, 255)
    px, ex = period(yellow, 1)
    py, ey = period(yellow, 0)
    k = max(1, min(rgb.shape[1] // px, rgb.shape[0] // py))
    tile = rgb[: k * py, : k * px]
    alpha = yellow[: k * py, : k * px].astype(np.uint8)
    print("HAZARD_PERIOD", px, round(ex, 2), py, round(ey, 2), "periods", k)
    hazard = np.dstack([tile.astype(np.uint8), alpha])
    Image.fromarray(hazard, "RGBA").resize((512, 512), Image.Resampling.LANCZOS).save(out / "DecalHazard_base_color.png")
    print("DECAL DecalHazard_base_color.png (512, 512)")

    # Tyre tracks: dark impressions on white; alpha from darkness, crop to whole tread periods vertically.
    rgb = np.asarray(Image.open(refs / "tyre-tracks.png").convert("RGB")).astype(np.float32)
    dark = np.clip((235 - rgb.mean(axis=2)) * 1.6, 0, 255)
    py, ey = period(dark, 0, 30, 400)
    k = max(1, rgb.shape[0] // py)
    print("TRACK_PERIOD", py, round(ey, 2), "periods", k)
    tracks = np.dstack([np.full(dark.shape + (3,), 255, np.uint8), dark.astype(np.uint8)])[: k * py]
    Image.fromarray(tracks, "RGBA").resize((512, 512), Image.Resampling.LANCZOS).save(out / "GroundTracks_base_color.png")
    print("DECAL GroundTracks_base_color.png (512, 512)")


if __name__ == "__main__":
    main()
