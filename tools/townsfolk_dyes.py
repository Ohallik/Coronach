#!/usr/bin/env python3
"""Dyes for the bodies townsfolk wear: each body's own painted texture with its clothes and hair redyed, on its own UVs
(the townsfolk pass of November 21, after the whole-game capture's clones: Builds/quality/nov20-whole s49/140, s57/136).

Every ordinary townsperson wears a named person's generated body, and the bodies are a dozen species meshes repainted
per person on one UV layout (Brekk, Drask, Odrun and Tovra are one mesh; so are Dov, Maru, Ona, Pim and Quill). Five of
one mesh in one screen read as one figure five times however each is tinted. This tool gives every body four dyes, each
a different set of clothes and a different head of hair over the same skin and the same painting:

  * where each texel is on the body comes from the triangle it belongs to and the bone that carries that triangle
    (tools/blender/dump_uv_triangles_many.py on the body's clean model): head (Head, neck, head_end, headfront),
    hands, feet, and the clothes between them (spine, shoulders, arms, hips, legs);
  * the skin is the face's own colour (the head's front-facing texels, by their median) and the hands'; whatever on the
    clothes or the head is near either is skin and is left alone, with a soft edge;
  * the rest of the clothes falls into three colour groups (k-means in Lab, largest first) and the rest of the head into
    one (hair, fronds, a hood, a crest's paint); a dye moves each group to its own colour in Lab, keeping every texel's
    difference from its group's mean, so folds, seams and the painted light stay. A group whose new colour would land
    within reach of its old one takes the dye's second choice instead, so no dye leaves a garment as it was;
  * the feet (boots) and anything metal-bright are kept.

Dyes are written at 512 (the play camera never sees more of a townsperson) as JPEG to Lattice/Assets/_Project/Art/
Generated/Townsfolk/<Body>/<Body>_dye<n>.jpg, with docs/art/townsfolk-dyes.json saying what each group was and became.
TownsfolkLooks.Make (Unity) makes a material for each; the townsfolk pass chooses among them.

  python tools/townsfolk_dyes.py --tris <dir of <Body>_clean.npz> [--bodies A,B] [--preview <dir>]
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import meret_grade as mg  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / "Lattice/Assets/_Project/Art/Generated/Models"
OUT = ROOT / "Lattice/Assets/_Project/Art/Generated/Townsfolk"
MANIFEST = ROOT / "docs/art/townsfolk-dyes.json"
SIZE = 1024
HEAD = {"Head", "neck", "head_end", "headfront", "Neck"}
HANDS = {"LeftHand", "RightHand"}
FEET = {"LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase"}
# Four dyes, each a set of cloth colours (largest garment group first) and a hair colour, in sRGB. Two choices for each
# slot: the second is taken where the first would land too near the group's own colour.
DYES = [
    {"name": "teal", "cloth": [((44, 96, 102), (96, 70, 52)), ((196, 178, 140), (70, 74, 82)), ((150, 70, 44), (210, 196, 160))], "hair": ((40, 32, 30), (150, 140, 128))},
    {"name": "oxblood", "cloth": [((104, 36, 40), (58, 72, 96)), ((112, 118, 124), (190, 168, 120)), ((196, 150, 62), (60, 92, 70))], "hair": ((196, 178, 132), (60, 44, 36))},
    {"name": "moss", "cloth": [((82, 98, 52), (128, 60, 46)), ((52, 50, 52), (206, 196, 172)), ((218, 206, 178), (118, 86, 56))], "hair": ((134, 58, 34), (72, 48, 34))},
    {"name": "indigo", "cloth": [((48, 56, 104), (150, 108, 52)), ((168, 132, 92), (88, 92, 100)), ((110, 62, 98), (196, 120, 60))], "hair": ((176, 178, 184), (30, 28, 32))},
]
NEAR = 26.0  # Lab distance under which a new colour is not a change
# Hair is only ever a colour hair grows in: each dye takes the first of these that is neither near the head's own colour nor
# near what an earlier dye gave it.
HAIR = [(40, 32, 30), (196, 178, 132), (134, 58, 34), (176, 178, 184), (72, 48, 34), (214, 214, 220), (110, 70, 44), (128, 126, 124)]
# A body in a force's colours is a uniform (TownsfolkLook.Uniforms): it is never dyed.
UNIFORMS = {"MusterPikeguard", "MusterSergeant", "MusterScav", "CompactWarden", "PatrolBody"}
# Bodies whose head is the species' own, not hair or a hat: skin, plates, fur, fins, antennae or a mask. Their heads keep
# their colour, and a townsperson in one is told apart by what is worn on the head instead (TownsfolkLooks).
HEAD_KEPT = {
    "BrekkBody", "DraskBody", "OdrunBody", "TovraBody",  # stone-skinned, bald
    "BinnaBody", "DabbBody", "GaroBody", "JaskBody", "OkhariBody", "OkhariGrower", "RuvaBody", "SennBody", "TeoBody",  # finned, bald
    "HaskBody", "IruBody", "KeptHerder", "TernBody",  # furred
    "DovBody", "MaruBody", "OnaBody", "PimBody", "QuillBody",  # antennae
    "DorrowBody", "TamBody",  # masked and goggled
    "IdraNatural", "IdraBraced", "PipBody", "VessaBody",  # plated crests
    "MusterPikeguard", "MusterSergeant", "MusterScav", "CompactWarden", "Gardener",  # helmets and frames
}


# ---------------------------------------------------------------- colour
def srgb_to_lab(rgb: np.ndarray) -> np.ndarray:
    c = rgb / 255.0
    c = np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
    x = c @ np.array([[.4124, .2126, .0193], [.3576, .7152, .1192], [.1805, .0722, .9505]], np.float32)
    x = x / np.array([.95047, 1.0, 1.08883], np.float32)
    f = np.where(x > .008856, np.cbrt(x), 7.787 * x + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def lab_to_srgb(lab: np.ndarray) -> np.ndarray:
    fy = (lab[..., 0] + 16) / 116
    fx, fz = fy + lab[..., 1] / 500, fy - lab[..., 2] / 200
    f = np.stack([fx, fy, fz], -1)
    x = np.where(f ** 3 > .008856, f ** 3, (f - 16 / 116) / 7.787) * np.array([.95047, 1.0, 1.08883], np.float32)
    c = x @ np.array([[3.2406, -.9689, .0557], [-1.5372, 1.8758, -.2040], [-.4986, .0415, 1.0570]], np.float32)
    c = np.clip(c, 0, 1)
    c = np.where(c <= .0031308, 12.92 * c, 1.055 * c ** (1 / 2.4) - .055)
    return np.clip(c * 255, 0, 255)


def kmeans(points: np.ndarray, k: int, seed: int = 7, rounds: int = 20) -> tuple[np.ndarray, np.ndarray]:
    rng = np.random.default_rng(seed)
    sample = points[rng.choice(len(points), size=min(len(points), 20000), replace=False)]
    # Spread starts (k-means++), so a small bright group isn't swallowed by a big dull one.
    centres = [sample[rng.integers(len(sample))]]
    for _ in range(1, k):
        d = np.min([((sample - c) ** 2).sum(1) for c in centres], 0)
        centres.append(sample[rng.choice(len(sample), p=d / d.sum())])
    centres = np.array(centres)
    for _ in range(rounds):
        lab = np.argmin(((sample[:, None, :] - centres[None]) ** 2).sum(-1), 1)
        centres = np.array([sample[lab == j].mean(0) if (lab == j).any() else centres[j] for j in range(k)])
    full = np.argmin(((points[:, None, :] - centres[None]) ** 2).sum(-1), 1)
    order = np.argsort([-(full == j).sum() for j in range(k)])
    remap = np.empty(k, int)
    remap[order] = np.arange(k)
    return centres[order], remap[full]


# ---------------------------------------------------------------- one body
def regions(tris: dict, size: int) -> np.ndarray:
    """1 head, 2 hands, 3 feet, 4 clothes, painted over each triangle's UVs."""
    bone = tris["bone"]
    labels = np.full(len(bone), 4, np.uint8)
    labels[np.isin(bone, list(HEAD))] = 1
    labels[np.isin(bone, list(HANDS))] = 2
    labels[np.isin(bone, list(FEET))] = 3
    return mg.label_map(tris["pos"], tris["uv"], labels, size)


def face_texels(tris: dict, size: int) -> np.ndarray:
    """The head's front: head triangles facing the way the body faces (Blender's -Y), between its chin and its brow."""
    bone, pos, nrm = tris["bone"], tris["pos"], tris["nrm"]
    head = np.isin(bone, list(HEAD))
    centre = pos.mean(1)
    z, x = centre[:, 2], centre[:, 0]
    lo, hi = np.percentile(z[head], 20), np.percentile(z[head], 65)
    # The middle of the face only: big hair frames a face, and the head's front edge is hair as often as cheek.
    half = np.percentile(np.abs(x[head] - np.median(x[head])), 80)
    front = head & (nrm[:, 1] < -.7) & (z > lo) & (z < hi) & (np.abs(x - np.median(x[head])) < .35 * half)
    labels = np.where(front, 1, 0).astype(np.uint8)
    return mg.label_map(pos, tris["uv"], labels, size) == 1


def dye_body(body: str, tris: dict) -> tuple[list[np.ndarray], dict]:
    tex = np.asarray(Image.open(MODELS / body / f"{body}_base_color.png").convert("RGB").resize((SIZE, SIZE), Image.LANCZOS)).astype(np.float32)
    region = regions(tris, SIZE)
    lab = srgb_to_lab(tex)
    face = face_texels(tris, SIZE)
    skin = np.median(lab[face], 0) if face.sum() > 50 else np.median(lab[region == 1], 0)
    hands = np.median(lab[region == 2], 0) if (region == 2).sum() > 50 else skin
    # Chroma counts twice: shaded skin moves in lightness, a khaki or a tan cloth beside it moves in hue. Both the face and
    # the hands are skin (a gloved hand's colour is then kept too: Pip's dark grey stays, the rest of her takes the dye).
    weigh = np.array([1, 2, 2], np.float32)
    dist = np.minimum(np.linalg.norm((lab - skin) * weigh, axis=-1), np.linalg.norm((lab - hands) * weigh, axis=-1))
    # Skin, softly: fully kept under 8, fully dyeable past 16 (khaki trousers sit 16 from a tan face this way).
    dyeable = np.clip((dist - 8) / 8, 0, 1)
    # Near-black linework keeps its colour.
    keep = lab[..., 0] < 6
    cloth = (region == 4) & (dyeable > .5) & ~keep
    head = (region == 1) & (dyeable > .5) & ~keep
    info = {"skin": [round(float(v), 1) for v in skin], "cloth": [], "hair": None}
    cloth_centres, cloth_groups = (kmeans(lab[cloth], 3) if cloth.sum() > 400 else (np.zeros((0, 3)), np.zeros(0, int)))
    # Groups nearer than a garment's own wear (a coat and its dirt) are one garment: dyed apart they speckle.
    merged = list(range(len(cloth_centres)))
    for j in range(len(cloth_centres)):
        for i in range(j):
            if merged[i] == i and np.linalg.norm(cloth_centres[j] - cloth_centres[i]) < 22:
                merged[j] = i
                break
    hair_share = head.sum() / max(1, (region == 1).sum())
    hair_centre = np.median(lab[head], 0) if body not in HEAD_KEPT and head.sum() > 300 and hair_share > .12 else None
    # Each texel's group: its nearest centre (the soft edge then blends by dyeable), merged, and the map's speckle taken out
    # with a mode filter (the gutters are grown, so a filter doesn't reach across an island's edge into another's).
    group = np.full(region.shape, -1, int)
    clothes_all = (region == 4) & ~keep
    if len(cloth_centres):
        near = np.argmin(((lab[clothes_all][:, None, :] - cloth_centres[None]) ** 2).sum(-1), 1)
        group[clothes_all] = np.array(merged)[near]
        smooth = np.asarray(Image.fromarray((group + 1).astype(np.uint8)).filter(ImageFilter.ModeFilter(7))).astype(int) - 1
        group = np.where(clothes_all & (smooth >= 0), smooth, group)
    if hair_centre is not None:
        group[(region == 1) & ~keep] = 10
    for j, c in enumerate(cloth_centres):
        info["cloth"].append({"lab": [round(float(v), 1) for v in c], "share": round(float((cloth_groups == j).mean()), 3), "with": merged[j]})
    if hair_centre is not None:
        info["hair"] = {"lab": [round(float(v), 1) for v in hair_centre], "share": round(float(hair_share), 3)}
    dyes = []
    info["dyes"] = []
    taken: dict[str, list[np.ndarray]] = {}

    def target(slot, centre, choices):
        """The dye's first choice for a group, unless it is near the group's own colour or near what an earlier dye gave
        the same group (two dyes alike are one dye); then its second; failing both, the first turned round the hue. Hair
        goes down the list of hair colours instead."""
        options = [srgb_to_lab(np.array(c, np.float32)[None])[0] for c in (HAIR if slot == "hair" else choices)]
        if slot != "hair":
            options.append(np.array([options[0][0], -options[0][2], options[0][1]], np.float32))
        for t in options:
            if np.linalg.norm(t - centre) > NEAR and all(np.linalg.norm(t - o) > 30 for o in taken.get(slot, [])):
                break
        taken.setdefault(slot, []).append(t)
        return t

    for dye in DYES:
        out = lab.copy()
        chosen = {}

        for j, c in enumerate(cloth_centres):
            if merged[j] != j:
                continue
            t = target(f"cloth{j}", c, dye["cloth"][min(j, len(dye["cloth"]) - 1)])
            m = group == j
            # The group's own light and wear kept around the new colour, its old colour's pull gone; held to a cloth's chroma.
            shifted = np.stack([lab[m][:, 0] + (t[0] - c[0]) * .9, t[1] * .85 + (lab[m][:, 1] - c[1]) * .55, t[2] * .85 + (lab[m][:, 2] - c[2]) * .55], -1)
            w = dyeable[m][:, None]
            out[m] = lab[m] * (1 - w) + shifted * w
            chosen[f"cloth{j}"] = [int(v) for v in lab_to_srgb(t[None])[0]]
        if hair_centre is not None:
            t = target("hair", hair_centre, dye["hair"])
            m = group == 10
            shifted = np.stack([lab[m][:, 0] + (t[0] - hair_centre[0]) * .85, t[1] + (lab[m][:, 1] - hair_centre[1]) * .5, t[2] + (lab[m][:, 2] - hair_centre[2]) * .5], -1)
            w = dyeable[m][:, None]
            out[m] = lab[m] * (1 - w) + shifted * w
            chosen["hair"] = [int(v) for v in lab_to_srgb(t[None])[0]]
        rgb = lab_to_srgb(out)
        dyes.append(rgb)
        info["dyes"].append({"name": dye["name"], "to": chosen})
    return dyes, info


# ---------------------------------------------------------------- a still from the play camera's side, for review
def preview(tris: dict, textures: list[np.ndarray], height: int = 220, back: bool = False) -> Image.Image:
    """The bind pose from the play camera's pitch and yaw (40 and 20 degrees), front-lit, each texture side by side."""
    pos = tris["pos"].astype(np.float64)
    uv = tris["uv"].astype(np.float64)
    yaw, pitch = np.radians(200.0 if back else 20.0), np.radians(40.0)
    # Blender Z up, face toward -Y; the camera looks at the face from above, turned 20 degrees.
    rz = np.array([[np.cos(yaw), -np.sin(yaw), 0], [np.sin(yaw), np.cos(yaw), 0], [0, 0, 1]])
    rx = np.array([[1, 0, 0], [0, np.cos(pitch), -np.sin(pitch)], [0, np.sin(pitch), np.cos(pitch)]])
    p = pos @ rz.T @ rx.T
    sx, sy, depth = p[..., 0], p[..., 2], p[..., 1]
    lo_x, hi_x, lo_y, hi_y = sx.min(), sx.max(), sy.min(), sy.max()
    scale = (height - 8) / (hi_y - lo_y)
    width = int((hi_x - lo_x) * scale) + 8
    px = (sx - lo_x) * scale + 4
    py = (hi_y - sy) * scale + 4
    n = np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0])
    n /= np.linalg.norm(n, axis=1, keepdims=True) + 1e-9
    shade = .55 + .45 * np.clip(-n[:, 1] * .7 + n[:, 2] * .5, 0, 1)
    sheets = []
    for tex in textures:
        img = np.zeros((height, width, 3), np.float32) + 40
        zbuf = np.full((height, width), np.inf)
        th, tw = tex.shape[:2]
        for t in range(len(p)):
            x0, x1 = int(max(0, np.floor(px[t].min()))), int(min(width - 1, np.ceil(px[t].max())))
            y0, y1 = int(max(0, np.floor(py[t].min()))), int(min(height - 1, np.ceil(py[t].max())))
            if x1 < x0 or y1 < y0:
                continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
            (ax, bx, cx), (ay, by, cy) = px[t], py[t]
            den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(den) < 1e-9:
                continue
            w0 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den
            w1 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den
            w2 = 1 - w0 - w1
            inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
            if not inside.any():
                continue
            z = w0 * depth[t, 0] + w1 * depth[t, 1] + w2 * depth[t, 2]
            yy, xx = (gy - .5).astype(int), (gx - .5).astype(int)
            closer = inside & (z < zbuf[yy, xx])
            if not closer.any():
                continue
            u = w0 * uv[t, 0, 0] + w1 * uv[t, 1, 0] + w2 * uv[t, 2, 0]
            v = w0 * uv[t, 0, 1] + w1 * uv[t, 1, 1] + w2 * uv[t, 2, 1]
            tx = np.clip((u * tw).astype(int), 0, tw - 1)
            ty = np.clip(((1 - v) * th).astype(int), 0, th - 1)
            zbuf[yy[closer], xx[closer]] = z[closer]
            img[yy[closer], xx[closer]] = tex[ty[closer], tx[closer]] * shade[t]
        sheets.append(img)
    return Image.fromarray(np.concatenate(sheets, 1).clip(0, 255).astype(np.uint8))


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--tris", required=True)
    ap.add_argument("--bodies", default="")
    ap.add_argument("--preview", default="")
    ap.add_argument("--dry", action="store_true", help="previews only: write no dye and no manifest")
    a = ap.parse_args()
    tris_dir = Path(a.tris)
    bodies = [b for b in a.bodies.split(",") if b] or sorted(p.stem.replace("_clean", "") for p in tris_dir.glob("*_clean.npz"))
    bodies = [b for b in bodies if b not in UNIFORMS]
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8")) if MANIFEST.exists() else {}
    for body in bodies:
        tris = dict(np.load(tris_dir / f"{body}_clean.npz"))
        dyes, info = dye_body(body, tris)
        if not a.dry:
            folder = OUT / body
            folder.mkdir(parents=True, exist_ok=True)
            for k, rgb in enumerate(dyes, 1):
                Image.fromarray(rgb.astype(np.uint8)).resize((512, 512), Image.LANCZOS).save(folder / f"{body}_dye{k}.jpg", quality=92)
            manifest[body] = info
        if a.preview:
            base = np.asarray(Image.open(MODELS / body / f"{body}_base_color.png").convert("RGB").resize((SIZE, SIZE)), np.float32)
            Path(a.preview).mkdir(parents=True, exist_ok=True)
            front, behind = preview(tris, [base] + dyes), preview(tris, [base] + dyes, back=True)
            both = Image.new("RGB", (front.width, front.height * 2))
            both.paste(front, (0, 0))
            both.paste(behind, (0, front.height))
            both.save(Path(a.preview) / f"{body}.jpg", quality=88)
        print(f"DYED {body} cloth={len(info['cloth'])} hair={'yes' if info['hair'] else 'no'}")
    if not a.dry:
        MANIFEST.write_text(json.dumps(manifest, indent=1, sort_keys=True), encoding="utf-8")
    print("TOWNSFOLK_DYES_OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
