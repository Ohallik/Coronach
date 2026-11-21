"""Paint the ground kit's textures (free, local, deterministic): tiling ground layers, the flat detail atlas,
the low cover atlas and the moving-shade maps. Everything is painted near neutral, so a zone's tint decides its colour.

    python tools/ground_textures.py                 # paint everything into Lattice/Assets/_Project/Art/Generated/Ground
    python tools/ground_textures.py --only pavers,decals
    python tools/ground_textures.py --check         # seam check of the tiling textures only (exit 1 on a seam)

Tiling layers (512, RGB + a height in alpha that LatticeToon's splat blend cuts its edges by): ground-pavers,
ground-cobble, ground-scree, ground-gravel, ground-sand, ground-litter, ground-damp, ground-veins, ground-bedrock.
ground-decals.png is a 4x4 atlas (GroundKit.Decal names its cells in the same order as DECALS below);
ground-cover.png is a 4x4 atlas of plant and bone cards (COVER below); ground-shade-cloud/-dapple are the maps a
zone's GroundShade drifts across its sun.
"""
from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Lattice/Assets/_Project/Art/Generated/Ground"
DRESSING = ROOT / "Lattice/Assets/_Project/Art/Generated/Dressing"
N = 512

DECALS = ["CracksA", "CracksB", "PebblesA", "PebblesB", "Slabs", "Flagstones", "SandDrift", "Litter",
          "Scorch", "Stain", "Lichen", "Tracks", "Ruts", "SurveyLine", "Shadow", "LampPool"]
COVER = ["TuftA", "TuftB", "TuftC", "Fern", "Flowers", "Reeds", "Succulent", "Twigs",
         "FungusA", "FungusB", "BonesA", "BonesB", "Sprouts", "Shards", "Spare1", "Spare2"]


# ---------------------------------------------------------------- noise and fields
def smooth(a, b, x):
    t = np.clip((x - a) / (b - a + 1e-9), 0, 1)
    return t * t * (3 - 2 * t)


def value_noise(n, period, rng):
    """Tileable value noise: a period x period lattice, smooth-stepped, wrapped."""
    lattice = rng.random((period, period)).astype(np.float32)
    xs = np.arange(n, dtype=np.float32) * period / n
    i = np.floor(xs).astype(int)
    f = xs - i
    f = f * f * (3 - 2 * f)
    i0, i1 = i % period, (i + 1) % period
    top = lattice[np.ix_(i0, i0)] * (1 - f)[None, :] + lattice[np.ix_(i0, i1)] * f[None, :]
    bottom = lattice[np.ix_(i1, i0)] * (1 - f)[None, :] + lattice[np.ix_(i1, i1)] * f[None, :]
    return top * (1 - f)[:, None] + bottom * f[:, None]


def fbm(n, period, rng, octaves=4):
    total, amp, norm = np.zeros((n, n), np.float32), 1.0, 0.0
    for o in range(octaves):
        total += value_noise(n, period * (2 ** o), rng) * amp
        norm += amp
        amp *= 0.5
    return total / norm


def voronoi(n, points, warp=None):
    """Toroidal Voronoi: nearest id, distance to it, and how far the pixel is inside its cell (d2 - d1)."""
    ys, xs = np.mgrid[0:n, 0:n].astype(np.float32)
    if warp is not None:
        xs, ys = xs + warp[0], ys + warp[1]
    d1 = np.full((n, n), 1e9, np.float32)
    d2 = np.full((n, n), 1e9, np.float32)
    ids = np.zeros((n, n), np.int32)
    for k, (px, py, sx, sy) in enumerate(points):
        dx = np.abs(xs - px) % n
        dx = np.minimum(dx, n - dx) / sx
        dy = np.abs(ys - py) % n
        dy = np.minimum(dy, n - dy) / sy
        d = np.sqrt(dx * dx + dy * dy)
        closer = d < d1
        d2 = np.where(closer, d1, np.minimum(d2, d))
        ids = np.where(closer, k, ids)
        d1 = np.where(closer, d, d1)
    return ids, d1, d2 - d1


def grid_points(n, across, down, rng, jitter=0.36, stretch=(1, 1), bond=False):
    points = []
    for j in range(down):
        for i in range(across):
            x = (i + 0.5 + (0.5 if bond and j % 2 else 0) + rng.uniform(-jitter, jitter)) * n / across
            y = (j + 0.5 + rng.uniform(-jitter, jitter)) * n / down
            points.append((x % n, y % n, stretch[0], stretch[1]))
    return points


def scattered_points(n, count, rng, tries=24):
    """Best-candidate points on the torus: evenly spread with no rows or columns to read."""
    points = [(rng.uniform(0, n), rng.uniform(0, n))]
    while len(points) < count:
        best, far = None, -1.0
        placed = np.array(points)
        for _ in range(tries):
            c = rng.uniform(0, n, 2)
            d = np.abs(placed - c)
            d = np.minimum(d, n - d)
            near = float(np.hypot(d[:, 0], d[:, 1]).min())
            if near > far:
                best, far = c, near
        points.append((float(best[0]), float(best[1])))
    return [(x, y, 1, 1) for x, y in points]


def stone_field(n, points, rng, joint, bevel, tone=(0.74, 0.92), chip=0.5, mottle=0.05, wobble=3.0, bed=0.4):
    """Flat cel-style stones: one tone a stone, a lit lip up-left, a dark lip down-right, dark joints. Returns rgb (n,n,3) and height."""
    warp = ((fbm(n, 8, rng) - 0.5) * wobble * 2, (fbm(n, 8, rng) - 0.5) * wobble * 2)
    ids, d1, inside = voronoi(n, points, warp)
    count = len(points)
    tones = rng.uniform(tone[0], tone[1], count).astype(np.float32)
    warm = rng.uniform(-0.035, 0.035, count).astype(np.float32)
    # a chipped edge: joints widen where a fine noise is high
    chips = fbm(n, 24, rng, 3)
    edge = inside - smooth(0.58, 0.8, chips) * joint * 2.2 * chip
    stone = smooth(joint * 0.5, joint * 0.5 + 1.6, edge)
    gy, gx = np.gradient(ndimage.gaussian_filter(inside, 1.2, mode="wrap"))
    norm = np.sqrt(gx * gx + gy * gy) + 1e-6
    facing = (gx + gy) / norm / math.sqrt(2)      # +1 on a stone's up-left edge (its inside lies down-right), -1 on its down-right edge
    rim = 1 - smooth(joint * 0.5 + bevel * 0.35, joint * 0.5 + bevel, edge)
    value = tones[ids] + (fbm(n, 12, rng, 3) - 0.5) * mottle * 2
    value = value + rim * np.where(facing > 0.15, 0.1, 0) - rim * np.where(facing < -0.15, 0.13, 0)
    # a few worn hollows inside big stones
    worn = smooth(0.66, 0.74, fbm(n, 10, rng, 2)) * smooth(joint + bevel, joint + bevel * 2.5, edge)
    value = value - worn * 0.045
    ground = bed + (fbm(n, 32, rng, 2) - 0.5) * 0.1
    value = value * stone + ground * (1 - stone)
    rgb = np.stack([value * (1 + warm[ids] * stone), value, value * (1 - warm[ids] * stone)], axis=2)
    height = 0.15 + 0.85 * smooth(joint * 0.5, joint * 0.5 + bevel, edge)
    return np.clip(rgb, 0, 1), np.clip(height, 0, 1), ids, stone


def to_image(rgb, alpha=None):
    a = np.clip(rgb * 255 + 0.5, 0, 255).astype(np.uint8)
    if alpha is None:
        return Image.fromarray(a)
    return Image.fromarray(np.dstack([a, np.clip(alpha * 255 + 0.5, 0, 255).astype(np.uint8)]))


# ---------------------------------------------------------------- tiling layers
def pavers(rng):
    rgb, height, _, _ = stone_field(N, scattered_points(N, 34, rng), rng, joint=7, bevel=9)
    return to_image(rgb, height)


def cobble(rng):
    rgb, height, _, _ = stone_field(N, grid_points(N, 8, 12, rng, 0.22, stretch=(1.5, 1), bond=True), rng, joint=4, bevel=7, tone=(0.74, 0.93), chip=0.3, wobble=2, bed=0.52)
    return to_image(rgb, height)


def seamless(crop, rng):
    """Blend a stochastic crop with itself moved half way, along a ragged mask, so its edges are its own middle."""
    n = crop.shape[0]
    moved = np.roll(crop, (n // 2, n // 2), axis=(0, 1))
    ys, xs = np.mgrid[0:n, 0:n].astype(np.float32)
    border = np.minimum(np.minimum(xs, n - 1 - xs), np.minimum(ys, n - 1 - ys)) / (n * 0.5)
    mask = smooth(0.2, 0.34, border + (fbm(n, 10, rng, 3) - 0.5) * 0.3)[:, :, None]
    return crop * mask + moved * (1 - mask)


def from_painted(name, rng, chroma=0.4, mean=0.72):
    """A tiling layer cut from one of the generated ground patches: its solid middle, made seamless and near neutral."""
    source = Image.open(DRESSING / f"{name}_base_color.png").convert("RGBA")
    a = np.asarray(source, np.float32) / 255
    solid = a[:, :, 3] > 0.98
    ys, xs = np.where(solid)
    cy, cx = int(ys.mean()), int(xs.mean())
    half = 10
    while half < min(cy, cx, a.shape[0] - cy, a.shape[1] - cx) - 2 and solid[cy - half - 4:cy + half + 4, cx - half - 4:cx + half + 4].all():
        half += 4
    crop = a[cy - half:cy + half, cx - half:cx + half, :3]
    crop = np.asarray(Image.fromarray((crop * 255).astype(np.uint8)).resize((N, N), Image.LANCZOS), np.float32) / 255
    crop = seamless(crop, rng)
    lum = crop @ np.array([0.299, 0.587, 0.114], np.float32)
    rgb = lum[:, :, None] + (crop - lum[:, :, None]) * chroma
    rgb = rgb * (mean / max(1e-3, float(lum.mean())))
    height = smooth(float(np.percentile(lum, 20)), float(np.percentile(lum, 85)), ndimage.gaussian_filter(lum, 1.5, mode="wrap"))
    return to_image(np.clip(rgb, 0, 1), height)


def sand(rng):
    ys, xs = np.mgrid[0:N, 0:N].astype(np.float32) / N
    warp = fbm(N, 4, rng, 3)
    phase = ys * 26 + np.sin(xs * 2 * math.pi * 3 + warp * 4) * 0.4 + warp * 2.2
    ripple = phase - np.floor(phase)
    broken = smooth(0.42, 0.56, fbm(N, 7, rng, 3))
    line = (1 - smooth(0.03, 0.09, np.abs(ripple - 0.5))) * broken
    lit = (1 - smooth(0.02, 0.07, np.abs(ripple - 0.62))) * broken
    value = 0.88 + (fbm(N, 6, rng, 3) - 0.5) * 0.08 - line * 0.2 + lit * 0.07
    speck = rng.random((N, N)) > 0.9965
    speck = ndimage.binary_dilation(speck, iterations=1)
    value = np.where(speck, value - 0.25, value)
    rgb = np.stack([value * 1.01, value, value * 0.97], axis=2)
    height = np.clip(0.45 + (fbm(N, 5, rng, 2) - 0.5) * 0.5 - line * 0.2, 0, 1)
    return to_image(np.clip(rgb, 0, 1), height)


def wrapped(draw_one, n, scale, base=(0, 0, 0, 0)):
    """A supersampled RGBA canvas whose shapes wrap: draw_one(draw, ox, oy) is called for each of the nine tile offsets."""
    big = Image.new("RGBA", (n * scale, n * scale), base)
    draw = ImageDraw.Draw(big)
    for oy in (-n, 0, n):
        for ox in (-n, 0, n):
            draw_one(draw, ox * scale, oy * scale)
    return big.resize((n, n), Image.LANCZOS)


def leaf(draw, x, y, length, width, angle, tone, scale, rib=True):
    c, s = math.cos(angle), math.sin(angle)
    pts = []
    for t in np.linspace(0, 1, 9):
        w = math.sin(t * math.pi) ** 0.8 * width * 0.5
        pts.append((t * length - length / 2, w))
    outline = pts + [(px, -py) for px, py in reversed(pts)]
    world = [((px * c - py * s + x) * scale, (px * s + py * c + y) * scale) for px, py in outline]
    g = int(tone * 255)
    draw.polygon(world, fill=(g, g, int(g * 0.94), 255))
    if rib:
        d = int(tone * 0.72 * 255)
        draw.line([((-length / 2 * c + x) * scale, (-length / 2 * s + y) * scale), ((length / 2 * c + x) * scale, (length / 2 * s + y) * scale)], fill=(d, d, d, 255), width=max(1, scale // 2))


def litter(rng):
    leaves = [(rng.uniform(0, N), rng.uniform(0, N), rng.uniform(26, 46), rng.uniform(10, 18), rng.uniform(0, math.pi * 2), rng.uniform(0.55, 0.98)) for _ in range(150)]

    def paint(draw, ox, oy):
        for x, y, length, width, angle, tone in leaves:
            if -60 < x + ox / 3 < N + 60 and -60 < y + oy / 3 < N + 60:
                leaf(draw, x + ox / 3, y + oy / 3, length, width, angle, 0.42, 3, rib=False)     # its shadow, a touch down-right
        for x, y, length, width, angle, tone in leaves:
            if -60 < x + ox / 3 < N + 60 and -60 < y + oy / 3 < N + 60:
                leaf(draw, x + ox / 3 - 1.5, y + oy / 3 - 2, length, width, angle, tone, 3)
    layer = np.asarray(wrapped(paint, N, 3), np.float32) / 255
    ground = 0.66 + (fbm(N, 16, rng, 3) - 0.5) * 0.1
    a = layer[:, :, 3:4]
    rgb = layer[:, :, :3] * a + np.stack([ground] * 3, axis=2) * (1 - a)
    height = np.clip(0.2 + 0.8 * a[:, :, 0] * (layer[:, :, 0] > 0.5), 0, 1)
    return to_image(np.clip(rgb, 0, 1), height)


def damp(rng):
    """Wet rock: dark, with a few pale glints along the lips of shallow hollows."""
    shape = fbm(N, 5, rng, 4)
    hollow = smooth(0.44, 0.5, shape)
    lip = (1 - smooth(0.0, 0.03, np.abs(shape - 0.47))) * smooth(0.5, 0.62, fbm(N, 9, rng, 2))
    value = 0.5 - hollow * 0.14 + lip * 0.34 + (fbm(N, 24, rng, 2) - 0.5) * 0.06
    rgb = np.stack([value * 0.96, value, value * 1.05], axis=2)
    return to_image(np.clip(rgb, 0, 1), np.clip(1 - hollow * 0.7, 0, 1))


def veins(rng):
    """Mineral veins through dark rock: thin pale branching seams (the splat tint gives them their colour)."""
    a, b = fbm(N, 4, rng, 4), fbm(N, 6, rng, 4)
    seam = np.minimum(np.abs(a - 0.5), np.abs(b - 0.5) * 1.4)
    line = 1 - smooth(0.006, 0.02, seam)
    halo = 1 - smooth(0.02, 0.07, seam)
    value = 0.42 + (fbm(N, 20, rng, 3) - 0.5) * 0.1 + halo * 0.1 + line * 0.5
    return to_image(np.clip(np.stack([value] * 3, axis=2), 0, 1), np.clip(0.3 + line * 0.7, 0, 1))


# ---------------------------------------------------------------- the flat detail atlas
def blob(n, rng, rx, ry, wobble=0.22, lobes=5, soft=2.0, centre=None, angle=0):
    """A ragged-edged shape's coverage (0..1), crisp to about `soft` pixels."""
    cx, cy = centre or (n / 2, n / 2)
    ys, xs = np.mgrid[0:n, 0:n].astype(np.float32)
    dx, dy = xs - cx, ys - cy
    c, s = math.cos(angle), math.sin(angle)
    u, v = (dx * c + dy * s) / rx, (-dx * s + dy * c) / ry
    r = np.sqrt(u * u + v * v)
    theta = np.arctan2(v, u)
    edge = np.ones_like(r)
    for k in range(1, lobes + 1):
        edge += wobble / k * np.sin(theta * (k + 1) + rng.uniform(0, 6.28))
    edge += (fbm(n, 12, rng, 3) - 0.5) * wobble * 1.2
    return np.clip((edge - r) * min(rx, ry) / soft, 0, 1)


def crack_lines(rng, n, branches, reach):
    """Branching polylines with widths, from the middle outwards."""
    lines = []

    def grow(x, y, heading, width, length, depth):
        pts = [(x, y)]
        travelled = 0
        while travelled < length:
            step = rng.uniform(9, 16)
            heading += rng.normal(0, 0.38)
            x, y = x + math.cos(heading) * step, y + math.sin(heading) * step
            if not (14 < x < n - 14 and 14 < y < n - 14):
                break
            pts.append((x, y))
            travelled += step
            if depth < 2 and rng.random() < 0.16:
                grow(x, y, heading + rng.choice([-1, 1]) * rng.uniform(0.6, 1.2), width * 0.6, length * 0.45, depth + 1)
        lines.append((pts, width))
    for b in range(branches):
        grow(n / 2 + rng.uniform(-20, 20), n / 2 + rng.uniform(-20, 20), b * 2 * math.pi / branches + rng.uniform(-0.5, 0.5), rng.uniform(4.5, 6.5), reach * rng.uniform(0.7, 1), 0)
    return lines


def cracks(rng, branches, reach):
    scale = 3
    img = Image.new("RGBA", (N * scale, N * scale), (0, 0, 0, 0))
    lip = Image.new("RGBA", (N * scale, N * scale), (0, 0, 0, 0))
    draw, lips = ImageDraw.Draw(img), ImageDraw.Draw(lip)
    for pts, width in crack_lines(rng, N, branches, reach):
        count = len(pts)
        for i in range(1, count):
            w = width * (1 - 0.8 * i / count)
            a, b = pts[i - 1], pts[i]
            lips.line([((a[0] + 1.6) * scale, (a[1] + 1.8) * scale), ((b[0] + 1.6) * scale, (b[1] + 1.8) * scale)], fill=(236, 232, 226, 150), width=max(1, int(w * scale * 0.8)))
            draw.line([(a[0] * scale, a[1] * scale), (b[0] * scale, b[1] * scale)], fill=(50, 44, 48, 255), width=max(1, int(w * scale)))
            draw.ellipse([(b[0] - w / 2) * scale, (b[1] - w / 2) * scale, (b[0] + w / 2) * scale, (b[1] + w / 2) * scale], fill=(50, 44, 48, 255))
    lip.alpha_composite(img)
    return lip.resize((N, N), Image.LANCZOS)


def pebble(draw, x, y, r, tone, rng, scale):
    count = 9
    radii = [r * rng.uniform(0.78, 1.12) for _ in range(count)]
    squash, turn = rng.uniform(0.62, 1), rng.uniform(0, math.pi)
    def ring(k, dx=0.0, dy=0.0):
        out = []
        for i in range(count):
            a = i * 2 * math.pi / count
            px, py = math.cos(a) * radii[i] * k, math.sin(a) * radii[i] * k * squash
            out.append(((px * math.cos(turn) - py * math.sin(turn) + x + dx) * scale, (px * math.sin(turn) + py * math.cos(turn) + y + dy) * scale))
        return out
    g = int(tone * 255)
    warm = rng.uniform(-10, 10)
    draw.polygon(ring(1.05, r * 0.16, r * 0.22), fill=(46, 40, 44, 235))
    draw.polygon(ring(1), fill=(int(np.clip(g + warm, 0, 255)), g, int(np.clip(g - warm, 0, 255)), 255))
    if r > 5:
        h = int(min(255, g + 26))
        draw.polygon(ring(0.52, -r * 0.2, -r * 0.24), fill=(h, h, h, 255))


def pebbles(rng, count, spread, big):
    scale = 3
    img = Image.new("RGBA", (N * scale, N * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    stones = []
    for _ in range(count):
        a, d = rng.uniform(0, 6.28), abs(rng.normal(0, spread))
        r = rng.uniform(4, 9) if rng.random() > big else rng.uniform(11, 24)
        stones.append((N / 2 + math.cos(a) * d, N / 2 + math.sin(a) * d * 0.8, r))
    for x, y, r in sorted(stones, key=lambda s: s[1]):
        if 30 < x < N - 30 and 30 < y < N - 30:
            pebble(draw, x, y, r, rng.uniform(0.58, 0.96), rng, scale)
    return img.resize((N, N), Image.LANCZOS)


def stones_cut(rng, across, keep, joint=6, bevel=9, tone=(0.78, 0.94)):
    """Loose slabs: a few stones kept out of a laid field, the rest left to the ground under them."""
    rgb, height, ids, stone = stone_field(N, grid_points(N, across, across, rng, 0.3), rng, joint=joint, bevel=bevel, tone=tone, chip=0.9)
    count = across * across
    centre = np.array([N / 2, N / 2])
    order = sorted(range(count), key=lambda k: np.hypot(*(np.array([(k % across + 0.5) * N / across, (k // across + 0.5) * N / across]) - centre)) + rng.uniform(0, N * 0.16))
    kept = np.isin(ids, order[:keep])
    ys, xs = np.mgrid[0:N, 0:N]
    inside = (np.abs(xs - N / 2) < N * 0.44) & (np.abs(ys - N / 2) < N * 0.44)
    alpha = stone * kept * inside
    # the underside's shadow, down-right of each kept stone
    shadow = np.roll(alpha, (5, 4), axis=(0, 1)) * (1 - alpha) * 0.75
    out = rgb * alpha[:, :, None] + 0.16 * shadow[:, :, None]
    return to_image(out / np.maximum(alpha + shadow, 1e-3)[:, :, None] * (alpha + shadow > 0)[:, :, None], np.clip(alpha + shadow, 0, 1))


def sand_drift(rng):
    cover = blob(N, rng, 205, 120, 0.3, 5, 2.5, angle=rng.uniform(-0.4, 0.4))
    tile = np.asarray(sand(rng).convert("RGB"), np.float32) / 255
    return to_image(tile, cover * 0.92)


def litter_decal(rng):
    scale = 3
    img = Image.new("RGBA", (N * scale, N * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    leaves = []
    for _ in range(46):
        a, d = rng.uniform(0, 6.28), abs(rng.normal(0, 95))
        leaves.append((N / 2 + math.cos(a) * d, N / 2 + math.sin(a) * d * 0.75, rng.uniform(22, 40), rng.uniform(9, 16), rng.uniform(0, 6.28), rng.uniform(0.6, 0.98)))
    leaves = [l for l in leaves if 40 < l[0] < N - 40 and 40 < l[1] < N - 40]
    for x, y, length, width, angle, tone in leaves:
        leaf(draw, x + 1.5, y + 2, length, width, angle, 0.18, scale, rib=False)
    for x, y, length, width, angle, tone in leaves:
        leaf(draw, x, y, length, width, angle, tone, scale)
    return img.resize((N, N), Image.LANCZOS)


def scorch(rng):
    cover = blob(N, rng, 190, 150, 0.34, 7, 3)
    core = blob(N, rng, 110, 85, 0.3, 5, 14)
    streak = fbm(N, 40, rng, 2)
    alpha = np.clip(cover * (0.5 + 0.2 * (streak > 0.5)) + core * 0.32, 0, 0.88)
    value = 0.16 + (fbm(N, 28, rng, 2) - 0.5) * 0.1
    ash = (rng.random((N, N)) > 0.997)
    ash = ndimage.binary_dilation(ash, iterations=2) & (core > 0.3)
    value = np.where(ash, 0.8, value)
    return to_image(np.stack([value * 1.04, value, value * 0.98], axis=2), np.where(ash, 0.9, alpha))


def stain(rng):
    outer = blob(N, rng, 200, 150, 0.3, 6, 2.5)
    inner = blob(N, rng, 120, 90, 0.3, 5, 2.5, centre=(N / 2 + rng.uniform(-30, 30), N / 2 + rng.uniform(-20, 20)))
    rim = np.clip(outer - blob(N, rng, 188, 140, 0.3, 6, 2.5), 0, 1)
    alpha = np.clip(outer * 0.36 + inner * 0.2 + rim * 0.12, 0, 1)
    value = np.full((N, N), 0.2, np.float32)
    return to_image(np.stack([value, value * 0.98, value * 1.02], axis=2), alpha)


def lichen(rng):
    alpha = np.zeros((N, N), np.float32)
    value = np.zeros((N, N), np.float32)
    for _ in range(34):
        a, d = rng.uniform(0, 6.28), abs(rng.normal(0, 105))
        x, y, r = N / 2 + math.cos(a) * d, N / 2 + math.sin(a) * d * 0.8, rng.uniform(9, 40)
        if not (r + 12 < x < N - r - 12 and r + 12 < y < N - r - 12):
            continue
        b = blob(N, rng, r, r * rng.uniform(0.75, 1), 0.2, 6, 1.5, centre=(x, y))
        ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
        rr = np.hypot(xs - x, ys - y) / r
        tone = 0.68 + 0.26 * smooth(0.35, 0.95, rr) + (fbm(N, 30, rng, 2) - 0.5) * 0.08
        value = np.where(b > alpha, tone, value)
        alpha = np.maximum(alpha, b)
    return to_image(np.stack([value * 0.98, value, value * 0.93], axis=2), alpha * 0.9)


def tracks(rng):
    scale = 3
    img = Image.new("RGBA", (N * scale, N * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    dark = (44, 38, 42, 190)
    def print_at(x, y, heading):
        for toe in (-0.5, 0, 0.5):
            a = heading + toe
            tx, ty = x + math.sin(a) * 15, y - math.cos(a) * 15
            draw.ellipse([(tx - 5) * scale, (ty - 8) * scale, (tx + 5) * scale, (ty + 8) * scale], fill=dark)
        draw.ellipse([(x - 9) * scale, (y - 7) * scale, (x + 9) * scale, (y + 9) * scale], fill=dark)
    y, side = N - 50, 1
    while y > 40:
        print_at(N / 2 + side * 32 + rng.uniform(-5, 5), y, rng.uniform(-0.2, 0.2) + side * 0.12)
        y -= rng.uniform(52, 66)
        side = -side
    return img.resize((N, N), Image.LANCZOS)


def ruts(rng):
    """Two wheel ruts, tiling along v: pressed dark with a pale thrown ridge either side."""
    ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
    alpha = np.zeros((N, N), np.float32)
    value = np.zeros((N, N), np.float32)
    ragged = (fbm(N, 6, rng, 3) - 0.5) * 22
    for centre in (N * 0.27, N * 0.73):
        d = np.abs(xs - centre + ragged * 0.5)
        rut = 1 - smooth(20, 24, d)
        ridge = (1 - smooth(31, 35, d)) * (1 - rut) * smooth(0.4, 0.55, fbm(N, 9, rng, 2))
        tread = (np.sin((ys + np.abs(xs - centre) * 0.9) * 2 * math.pi * 12 / N) > 0.55) * rut
        value = np.where(rut > 0, 0.32 - tread * 0.035, value)
        value = np.where(ridge > 0.5, 0.86, value)
        alpha = np.maximum(alpha, np.maximum(rut * 0.6, ridge * 0.55))
    return to_image(np.stack([value] * 3, axis=2), alpha)


def survey_line(rng):
    """An old painted survey line, tiling along v: worn dashes and cross ticks."""
    ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
    worn = smooth(0.34, 0.5, fbm(N, 14, rng, 3))
    line = (np.abs(xs - N / 2) < 11) * ((ys % 128) < 92)
    tick = (np.abs(xs - N / 2) < 46) * (np.abs((ys % 256) - 110) < 6)
    alpha = np.clip((line | tick) * worn, 0, 1)
    alpha = ndimage.gaussian_filter(alpha.astype(np.float32), 0.7, mode="wrap") * 0.85
    return to_image(np.full((N, N, 3), 0.95, np.float32), alpha)


def shadow_blob(rng):
    ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
    r = np.hypot((xs - N / 2) / (N * 0.42), (ys - N / 2) / (N * 0.42))
    alpha = (1 - smooth(0.55, 1.0, r)) * 0.55
    return to_image(np.full((N, N, 3), 0.1, np.float32), alpha)


def lamp_pool(rng):
    """A pool of lamp light in three flat steps with slightly ragged rims, as the toon ramp would band it."""
    ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
    r = np.hypot(xs - N / 2, ys - N / 2) / (N * 0.47) + (fbm(N, 6, rng, 3) - 0.5) * 0.09
    alpha = (1 - smooth(0.3, 0.42, r)) * 0.34 + (1 - smooth(0.56, 0.7, r)) * 0.33 + (1 - smooth(0.84, 0.98, r)) * 0.33
    return to_image(np.ones((N, N, 3), np.float32), alpha)


def atlas(cells, cell, across=4):
    sheet = Image.new("RGBA", (cell * across, cell * across), (0, 0, 0, 0))
    for i, im in enumerate(cells):
        if im.size != (cell, cell):
            im = im.resize((cell, cell), Image.LANCZOS)
        sheet.paste(im, ((i % across) * cell, (i // across) * cell))
    a = np.asarray(sheet, np.float32)
    # colour under clear texels comes from the nearest painted one, so a mip never drags in black
    solid = a[:, :, 3] > 8
    if solid.any() and not solid.all():
        nearest = ndimage.distance_transform_edt(~solid, return_distances=False, return_indices=True)
        a[:, :, :3] = a[nearest[0], nearest[1], :3]
    return Image.fromarray(a.astype(np.uint8))


def decals(rng):
    cells = [cracks(rng, 4, 210), cracks(rng, 2, 240), pebbles(rng, 70, 80, 0.2), pebbles(rng, 34, 110, 0.36),
             stones_cut(rng, 5, 7), stones_cut(rng, 4, 3, tone=(0.74, 0.9)), sand_drift(rng), litter_decal(rng),
             scorch(rng), stain(rng), lichen(rng), tracks(rng), ruts(rng), survey_line(rng), shadow_blob(rng), lamp_pool(rng)]
    return atlas(cells, N)


# ---------------------------------------------------------------- the cover atlas (cards seen from the side)
C = 256


def blade(draw, x0, y0, x1, y1, bend, width, tone, scale, tip=0.0):
    """A tapering leaf from its root to its tip, bowed sideways by `bend`; a darker root, a paler upper edge."""
    pts_l, pts_r = [], []
    steps = 10
    for i in range(steps + 1):
        t = i / steps
        mx, my = (x0 + x1) / 2 + bend, (y0 + y1) / 2
        x = (1 - t) ** 2 * x0 + 2 * t * (1 - t) * mx + t * t * x1
        y = (1 - t) ** 2 * y0 + 2 * t * (1 - t) * my + t * t * y1
        tx = 2 * (1 - t) * (mx - x0) + 2 * t * (x1 - mx)
        ty = 2 * (1 - t) * (my - y0) + 2 * t * (y1 - my)
        length = math.hypot(tx, ty) + 1e-6
        w = width * (math.sin(min(1, t * 1.15 + 0.1) * math.pi) ** 0.7) * (1 - tip * t) * 0.5 + 0.35
        pts_l.append(((x - ty / length * w) * scale, (y + tx / length * w) * scale))
        pts_r.append(((x + ty / length * w) * scale, (y - tx / length * w) * scale))
    g = int(tone * 255)
    draw.polygon(pts_l + pts_r[::-1], fill=(int(g * 0.96), g, int(g * 0.86), 255))
    lit = int(min(255, g + 34))
    draw.line(pts_l[2:], fill=(int(lit * 0.97), lit, int(lit * 0.88), 255), width=max(1, scale))


def tuft(rng, count, height, spread, lean=0.3, seeds=False, tone=(0.5, 0.88), width=9):
    scale = 4
    img = Image.new("RGBA", (C * scale, C * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    base_y = C - 8
    blades = []
    for i in range(count):
        a = rng.uniform(-1, 1)
        h = height * rng.uniform(0.55, 1) * (1 - 0.35 * abs(a))
        blades.append((C / 2 + a * 10, base_y, C / 2 + a * spread + rng.uniform(-8, 8), base_y - h, a * spread * lean + rng.uniform(-10, 10), rng.uniform(width * 0.6, width), rng.uniform(*tone)))
    for b in sorted(blades, key=lambda b: b[6]):
        blade(draw, *b, scale, tip=0.3)
        if seeds and rng.random() < 0.45:
            x1, y1 = b[2], b[3]
            for k in range(4):
                draw.ellipse([(x1 - 3 + rng.uniform(-2, 2)) * scale, (y1 + k * 7 - 3) * scale, (x1 + 3 + rng.uniform(-2, 2)) * scale, (y1 + k * 7 + 4) * scale], fill=(238, 232, 210, 255))
    return img.resize((C, C), Image.LANCZOS)


def fern(rng):
    scale = 4
    img = Image.new("RGBA", (C * scale, C * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    base_y = C - 8
    for i in range(7):
        a = -1 + 2 * i / 6 + rng.uniform(-0.1, 0.1)
        length = rng.uniform(90, 120) * (1 - 0.25 * abs(a))
        tipx, tipy = C / 2 + a * 105, base_y - length * (1 - 0.55 * abs(a))
        tone = rng.uniform(0.55, 0.85)
        for t in np.linspace(0.15, 1, 9):
            px, py = C / 2 + (tipx - C / 2) * t, base_y + (tipy - base_y) * t - math.sin(t * math.pi) * 22
            size = 20 * math.sin(min(1, t + 0.15) * math.pi) + 5
            for side in (-1, 1):
                blade(draw, px, py, px + side * size * 0.5 + a * 6, py - size * 0.8, 0, 7, tone, scale, tip=0.4)
        draw.line([(C / 2 * scale, base_y * scale), (tipx * scale, tipy * scale)], fill=(70, 78, 60, 255), width=scale * 2)
    return img.resize((C, C), Image.LANCZOS)


def flowers(rng):
    scale = 4
    img = tuft(rng, 9, 95, 55, tone=(0.45, 0.7), width=7).resize((C * scale, C * scale), Image.LANCZOS)
    draw = ImageDraw.Draw(img)
    for _ in range(7):
        x, y = C / 2 + rng.uniform(-70, 70), rng.uniform(C - 150, C - 70)
        draw.line([(x * scale, y * scale), ((C / 2 + (x - C / 2) * 0.3) * scale, (C - 10) * scale)], fill=(96, 108, 84, 255), width=scale * 2)
        r = rng.uniform(8, 12)
        for k in range(5):
            a = k * 2 * math.pi / 5 + rng.uniform(0, 1)
            px, py = x + math.cos(a) * r * 0.7, y + math.sin(a) * r * 0.55
            draw.ellipse([(px - r * 0.55) * scale, (py - r * 0.5) * scale, (px + r * 0.55) * scale, (py + r * 0.5) * scale], fill=(252, 250, 244, 255))
        draw.ellipse([(x - r * 0.3) * scale, (y - r * 0.3) * scale, (x + r * 0.3) * scale, (y + r * 0.3) * scale], fill=(240, 200, 120, 255))
    return img.resize((C, C), Image.LANCZOS)


def reeds(rng):
    scale = 4
    img = Image.new("RGBA", (C * scale, C * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    for _ in range(11):
        x = C / 2 + rng.uniform(-60, 60)
        h = rng.uniform(150, 232)
        top = x + rng.uniform(-22, 22)
        blade(draw, x, C - 8, top, C - 8 - h, rng.uniform(-10, 10), 6, rng.uniform(0.5, 0.82), scale, tip=0.5)
        if rng.random() < 0.5:
            draw.ellipse([(top - 5) * scale, (C - 8 - h - 4) * scale, (top + 5) * scale, (C - 8 - h + 34) * scale], fill=(120, 96, 78, 255))
    return img.resize((C, C), Image.LANCZOS)


def succulent(rng):
    scale = 4
    img = Image.new("RGBA", (C * scale, C * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    leaves = []
    for i in range(13):
        a = -1.25 + 2.5 * i / 12 + rng.uniform(-0.08, 0.08)
        length = rng.uniform(80, 112) * (1 - 0.2 * abs(a) / 1.25)
        leaves.append((abs(a), C / 2 + math.sin(a) * 8, C - 10, C / 2 + math.sin(a) * length, C - 10 - math.cos(a) * length * 0.9, math.sin(a) * 6, 26, rng.uniform(0.56, 0.86)))
    for _, x0, y0, x1, y1, bend, width, tone in sorted(leaves, key=lambda l: -l[0]):
        blade(draw, x0, y0, x1, y1, bend, width, tone, scale, tip=0.75)
    return img.resize((C, C), Image.LANCZOS)


def twigs(rng):
    scale = 4
    img = Image.new("RGBA", (C * scale, C * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    def branch(x, y, heading, length, width, depth):
        x1, y1 = x + math.sin(heading) * length, y - math.cos(heading) * length
        g = int(rng.uniform(0.5, 0.74) * 255)
        draw.line([(x * scale, y * scale), (x1 * scale, y1 * scale)], fill=(g, int(g * 0.93), int(g * 0.86), 255), width=max(scale, int(width * scale)))
        if depth < 3:
            for _ in range(2):
                branch(x1, y1, heading + rng.uniform(-0.8, 0.8), length * rng.uniform(0.55, 0.75), width * 0.6, depth + 1)
    for _ in range(4):
        branch(C / 2 + rng.uniform(-14, 14), C - 8, rng.uniform(-0.7, 0.7), rng.uniform(60, 84), 5, 0)
    return img.resize((C, C), Image.LANCZOS)


def fungus(rng, tall):
    scale = 4
    img = Image.new("RGBA", (C * scale, C * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    caps = sorted(((C / 2 + rng.uniform(-70, 70), rng.uniform(40, 120 if tall else 70), rng.uniform(18, 34)) for _ in range(6 if tall else 9)), key=lambda c: -c[1])
    for x, h, r in caps:
        y = C - 10 - h
        draw.polygon([((x - r * 0.22) * scale, (C - 8) * scale), ((x + r * 0.22) * scale, (C - 8) * scale), ((x + r * 0.16) * scale, y * scale), ((x - r * 0.16) * scale, y * scale)], fill=(214, 208, 196, 255))
        draw.pieslice([(x - r) * scale, (y - r * 0.8) * scale, (x + r) * scale, (y + r * 0.8) * scale], 180, 360, fill=(236, 232, 224, 255))
        draw.pieslice([(x - r * 0.72) * scale, (y - r * 0.62) * scale, (x + r * 0.3) * scale, (y + r * 0.3) * scale], 180, 300, fill=(255, 255, 252, 255))
        draw.line([((x - r) * scale, y * scale), ((x + r) * scale, y * scale)], fill=(150, 144, 150, 255), width=scale * 2)
    return img.resize((C, C), Image.LANCZOS)


def bones(rng, ribs):
    scale = 4
    img = Image.new("RGBA", (C * scale, C * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pale, shade = (236, 230, 214, 255), (150, 142, 132, 255)
    if ribs:
        draw.line([(40 * scale, (C - 22) * scale), ((C - 40) * scale, (C - 30) * scale)], fill=shade, width=scale * 9)
        draw.line([(40 * scale, (C - 25) * scale), ((C - 40) * scale, (C - 33) * scale)], fill=pale, width=scale * 6)
        for i in range(6):
            x = 58 + i * 28
            h = 70 + 34 * math.sin((i + 0.5) / 6 * math.pi)
            pts = [((x + math.sin(t * 2.2) * 22) * scale, (C - 30 - t * h) * scale) for t in np.linspace(0, 1, 9)]
            draw.line(pts, fill=shade, width=scale * 7, joint="curve")
            draw.line([(px - scale * 1.5, py - scale) for px, py in pts], fill=pale, width=scale * 4, joint="curve")
    else:
        for _ in range(5):
            x, y, a, length = rng.uniform(40, C - 150), rng.uniform(C - 60, C - 18), rng.uniform(-0.5, 0.5), rng.uniform(60, 100)
            x1, y1 = x + math.cos(a) * length, y - math.sin(a) * length * 0.5
            draw.line([(x * scale, (y + 2) * scale), (x1 * scale, (y1 + 2) * scale)], fill=shade, width=scale * 9)
            draw.line([(x * scale, y * scale), (x1 * scale, y1 * scale)], fill=pale, width=scale * 6)
            for ex, ey in ((x, y), (x1, y1)):
                draw.ellipse([(ex - 8) * scale, (ey - 7) * scale, (ex + 8) * scale, (ey + 7) * scale], fill=pale)
    return img.resize((C, C), Image.LANCZOS)


def shards(rng):
    scale = 4
    img = Image.new("RGBA", (C * scale, C * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    for _ in range(7):
        x, w, h = C / 2 + rng.uniform(-80, 80), rng.uniform(14, 28), rng.uniform(40, 120)
        lean = rng.uniform(-18, 18)
        g = int(rng.uniform(0.6, 0.9) * 255)
        draw.polygon([((x - w / 2) * scale, (C - 8) * scale), ((x + w / 2) * scale, (C - 8) * scale), ((x + lean + w * 0.2) * scale, (C - 8 - h) * scale), ((x + lean - w * 0.15) * scale, (C - 8 - h * 0.86) * scale)], fill=(g, g, g, 255))
        draw.polygon([((x - w / 2) * scale, (C - 8) * scale), ((x - w * 0.1) * scale, (C - 8) * scale), ((x + lean - w * 0.15) * scale, (C - 8 - h * 0.86) * scale)], fill=(min(255, g + 40),) * 3 + (255,))
    return img.resize((C, C), Image.LANCZOS)


def cover(rng):
    blank = Image.new("RGBA", (C, C), (0, 0, 0, 0))
    cells = [tuft(rng, 16, 130, 90), tuft(rng, 12, 215, 60, lean=0.2, seeds=True, width=7), tuft(rng, 22, 90, 112, lean=0.5), fern(rng), flowers(rng), reeds(rng), succulent(rng), twigs(rng),
             fungus(rng, False), fungus(rng, True), bones(rng, True), bones(rng, False), tuft(rng, 9, 64, 70, tone=(0.6, 0.95), width=12), shards(rng), blank, blank]
    return atlas(cells, C)


# ---------------------------------------------------------------- moving shade
def shade_cloud(rng):
    return Image.fromarray(np.clip(fbm(N, 3, rng, 5) * 255, 0, 255).astype(np.uint8))


def shade_dapple(rng):
    """Light through a canopy: many small rounded gaps. The shader cuts it at a threshold, so it's kept as a smooth field."""
    field = np.zeros((N, N), np.float32)
    ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
    for _ in range(190):
        x, y, r = rng.uniform(0, N), rng.uniform(0, N), rng.uniform(9, 30)
        dx = np.abs(xs - x)
        dx = np.minimum(dx, N - dx)
        dy = np.abs(ys - y)
        dy = np.minimum(dy, N - dy)
        field = np.maximum(field, np.clip(1 - np.hypot(dx, dy * rng.uniform(1, 1.5)) / r, 0, 1))
    field = ndimage.gaussian_filter(field, 2, mode="wrap")
    return Image.fromarray(np.clip(field * 255, 0, 255).astype(np.uint8))


# ---------------------------------------------------------------- seams
def seam(im):
    """How hard a tile's wrap edges are beside its own hardest row or column step (1 = as hard as the hardest step inside it; a seam is harder than anything inside)."""
    a = np.asarray(im.convert("RGB"), np.float32).mean(axis=2)
    rows = np.abs(np.diff(a, axis=0)).mean(axis=1)
    columns = np.abs(np.diff(a, axis=1)).mean(axis=0)
    return max(np.abs(a[0] - a[-1]).mean() / max(rows.max(), 1e-6), np.abs(a[:, 0] - a[:, -1]).mean() / max(columns.max(), 1e-6))


LAYERS = {
    "pavers": (pavers, 11), "cobble": (cobble, 12), "scree": (lambda r: from_painted("GroundPebbles", r), 13), "gravel": (lambda r: from_painted("GroundGravel", r), 14),
    "sand": (sand, 15), "litter": (litter, 16), "damp": (damp, 17), "veins": (veins, 18),
    # Cave stone worn flat (Hushwell, November 12): the generated basalt patch's cracked plates, near neutral, its height low in the cracks so a
    # worn patch's edge breaks along the stone's own fractures. Noise-made fractures read as laid crazy paving or as scattered glyphs.
    "bedrock": (lambda r: from_painted("GroundBasalt", r, chroma=0.3, mean=0.62), 19),
}
OTHER = {"decals": (decals, 31, "ground-decals"), "cover": (cover, 32, "ground-cover"), "cloud": (shade_cloud, 33, "ground-shade-cloud"), "dapple": (shade_dapple, 34, "ground-shade-dapple")}


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", help="comma-separated: " + ",".join(list(LAYERS) + list(OTHER)))
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--out", default=str(OUT))
    args = ap.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    want = args.only.split(",") if args.only else None
    worst = 0.0
    for name, (paint, seed) in LAYERS.items():
        path = out / f"ground-{name}.png"
        if not args.check and (want is None or name in want):
            paint(np.random.default_rng(seed)).save(path)
        if path.exists():
            s = seam(Image.open(path))
            worst = max(worst, s)
            print(f"GROUND_TEXTURE {path.name} seam={s:.2f}")
    for name, (paint, seed, file) in OTHER.items():
        if not args.check and (want is None or name in want):
            paint(np.random.default_rng(seed)).save(out / f"{file}.png")
            print(f"GROUND_TEXTURE {file}.png")
    for name in ("ground-shade-cloud", "ground-shade-dapple"):
        if (out / f"{name}.png").exists():
            s = seam(Image.open(out / f"{name}.png"))
            worst = max(worst, s)
            print(f"GROUND_TEXTURE {name}.png seam={s:.2f}")
    print(f"GROUND_TEXTURES_OK worst_seam={worst:.2f}")
    return 1 if worst > 1.1 else 0


if __name__ == "__main__":
    raise SystemExit(main())
