#!/usr/bin/env python3
"""Chapter 3 (Xylos) intake: prepares the generated pieces of tools/meshy/c10_xylos_batch.py for GenIntake.BuildPrefix,
the way tools/c9_intake.py does for chapter 2.

  python tools/c10_intake.py views --only Thornback     # look first: four sides, then four from the gameplay camera's height
  python tools/c10_intake.py prepare                    # every finished piece: strip, albedo, emission mask, intake row
  python tools/c10_intake.py prepare --only OkhariBody  # the rigged body alone: its clean FBX is what the reskins repaint
  python tools/c10_intake.py portraits                  # the eight atlases and the patrol's card, assembled for PortraitIntake.ImportNamed

Static pieces: strip the FBX's embedded textures (Blender), keep a <=1024 albedo and an emission mask, add an intake row.
The Okhari body: Meshy's rigged FBX is stripped and wears the texture of the mesh it was rigged from. Reskins: Meshy
repainted an existing body on its original UVs, so the new texture goes on the ORIGINAL clean model and the row copies the
source's clips. A retexture is then graded to its portrait's colours (GRADE): they come back darker than they were asked.

A creature's row names its enemy only once that EnemyDef asset exists (GenIntake rejects an unknown enemy). So after
XylosBuilder has defined the chapter's enemies, run `prepare` again for the creatures and re-stage them: the row then binds
the prefab, the Rootwarden (a biped on the husk rig) gets its recoil layer, and a boss that BossController.Occludes names
keeps its occlusion-fade shader variant.

Then, from PowerShell:
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.GenIntake.BuildPrefix -Marker GEN_INTAKE_OK -ExtraArgs @('-intake-prefix','Thornback,Sporecaster')
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.PortraitIntake.ImportNamed -Marker PORTRAIT_INTAKE_OK -ExtraArgs @('-portrait-names','Ruva_Natural,Teo_Natural')
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from emission_mask import extract  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
GEN = ROOT / "art-src/Generated/C10-Xylos"
INTAKE = ROOT / "docs/art/intake.json"
ENEMY_DEFS = ROOT / "Lattice/Assets/_Project/Resources/Definitions/EnemyDef"
# The floor the pieces are seen against in the review's second strip, as linear RGB: here the jungle's. None keeps the
# preview's neutral dark grey.
GROUND = "0.115,0.14,0.045"

# id: (size in metres, fit, emission key or None, folder, yaw). Creatures are static meshes that code animates.
STATIC = {
    "Thornback": (2.6, "length", None, "Enemies", 0),
    "Sporecaster": (1.5, "height", "lamp", "Enemies", 0),
    # Two flight bodies are sized for the drawnScale their definitions already carry (XylosBuilder.Enemies: 1.4 and 1.6,
    # chosen for the stand-ins), so they are drawn at 2.2 m and 2.4 m without anyone retuning.
    "Podskimmer": (1.6, "length", "lamp", "Enemies", -90),   # its nose lies along the mesh's X, as the patrol cutter's does
    "Seedthane": (5.5, "height", "lamp", "Enemies", 0),
    "Floodcrown": (8.0, "length", "lamp", "Enemies", 0),
    "SnagvineNode": (1.5, "length", "lime", "Enemies", 0),
    "SeedBarge": (16.0, "length", None, "Environment", -90),   # bow along +Z, the rudder leaf astern
    "SeedHouse": (11.0, "height", "lamp", "Environment", 0),
    "RootArch": (6.5, "height", None, "Environment", 0),
    "MarketAwning": (2.6, "height", None, "Environment", 0),
    "SluiceGate": (3.4, "height", None, "Environment", 0),
    # Binna's three baskets: a pick-up among ordinary plants, so its red and orange pods glow a little. Its pods hang on
    # the front (+Z); from behind it is mostly leaf.
    "SpicePodPlant": (1.1, "height", "orange", "Environment", 0),
}
# The knot came out as a wheel standing on its rim with its heart to the front; flight is seen from above, so it lies
# on its back with the heart up. (A pitch on a model that is already the right way up lays it flat: D213.)
PITCH = {"SnagvineNode": -90}
# How hard a creature's amber parts glow (GenIntake's default is 1.5, a lantern's): enough to pick the sac, the crop, the
# pods and the crown out of a dark frame without washing out what is painted on them.
GLOW = {"Sporecaster": .7, "Podskimmer": .7, "Seedthane": .9, "Floodcrown": 1.1, "SnagvineNode": .9, "SpicePodPlant": .6}
# id: (file stem of the rigged body, size). RuvaBody is the Okhari body as it was made, from her own portrait.
BODY = {"OkhariBody": ("OkhariBody", 1.4), "RuvaBody": ("OkhariBody", 1.4)}
# id: (source intake id, folder, size, emission key, fit or None to keep the source's)
RESKIN = {
    "TeoBody": ("OkhariBody", "Characters", 1.45, None, None),
    "BinnaBody": ("OkhariBody", "Characters", 1.38, None, None),
    "SennBody": ("OkhariBody", "Characters", 1.32, None, None),
    "JaskBody": ("OkhariBody", "Characters", 1.15, None, None),   # an adolescent: the same mesh, shown smaller
    "OkhariGrower": ("OkhariBody", "Characters", 1.4, None, None),
    # The script's two carrier hands have portraits and no order for a body: these are colour grades of coats already
    # paid for (TEXTURE_OF), so each stands in a skin of their own beside the grower they would otherwise share.
    "GaroBody": ("OkhariBody", "Characters", 1.46, None, None),
    "DabbBody": ("OkhariBody", "Characters", 1.5, None, None),
    "HaddaBody": ("Mira", "Characters", 1.84, "amber", None),
    "Reedstalker": ("Ridgehound", "Enemies", 2.8, "amber", None),
    "Rootwarden": ("SentinelHusk", "Enemies", 2.6, "amber", None),
    # "source" keeps the source's own glow mask: the repaint shares its UVs, so the survey drone's lens and vent are the
    # claim drone's emitter and vent, where an orange key would light the whole hazard-yellow hull.
    "ClaimDrone": ("SurveyDrone", "Enemies", 2.4, "source", "length"),
}
TINT = {"ClaimDrone": "#FF5A2E", "SpicePodPlant": "#FF6A3A"}
TEXTURE_OF = {"GaroBody": "TeoBody", "DabbBody": "OkhariGrower"}
# A retexture comes back darker and more saturated than the portrait it was asked to match (Jask's pale yellow-green skin
# arrived as deep leaf green at a third of the brightness). Each band is a colour match measured against the portrait
# sheet: (hue from, hue to, hue shift in degrees, saturation scale, value gamma). Gamma under 1 lifts.
GRADE: dict[str, list[tuple[float, float, float, float, float]]] = {
    "TeoBody": [(62, 125, -30, .8, .6), (20, 50, 0, .75, 1)],        # olive skin, not forest green; mustard, not orange
    "JaskBody": [(50, 125, -13, .5, .35)],                           # pale yellow-green skin
    "SennBody": [(0, 360, 12, .6, .56)],                             # bark brown going grey, not near black
    "BinnaBody": [(62, 125, -15, .6, .85), (345, 20, 0, .72, 1)],    # spring green; rust red, not signal red
    "OkhariGrower": [(62, 125, 0, .75, .75)],
    "GaroBody": [(62, 125, 78, .9, .6), (20, 50, 0, .7, 1)],         # Teo's oilcloth, on teal-green skin
    "DabbBody": [(62, 125, -35, .8, .55)],                           # the grower's plain smock, on olive-yellow skin
    "Rootwarden": [(0, 360, 0, 1, .8)],                              # its legs are near the colour of the hall it guards
    # Hadda's second coat is the right colours at a fifth of the light: near-black teal hair, slate skin gone to shadow.
    "HaddaBody": [(0, 360, 0, .8, .5), (135, 172, -50, .7, .48), (172, 205, 30, .9, .45), (15, 65, 0, .5, .4)],
}
CLIPS = [{"state": state, "path": "Assets/_Project/Art/Animation/UAL-Skeleton.fbx", "name": "Armature|Armature|Armature|" + clip}
         for state, clip in (("Idle", "Idle_Loop"), ("Walk", "Walk_Loop"), ("Attack", "Sword_Attack"))]
# id: the emission key of a new body whose own texture carries glowing marks (chapter 3's bodies have none).
BODY_KEY: dict[str, str] = {}
# id: fields laid over the finished row, for what the tables above don't say: a hero's `character` and `form`, `vanes`,
# or `clips_of` (take the clip list of another row, as a hero's forms take the first heroes').
ROW: dict[str, dict] = {}
PORTRAIT_REFS = None   # where the portrait sheets are, when not this batch's refs folder
# body stem: the share of its height under which tools/blender/cut_bridges.py cuts faces that join the legs.
CUT_BRIDGES: dict[str, float] = {}


def configure(batch: str, **tables) -> None:
    """Later chapters reuse this machinery with their own tables (tools/c11_intake.py): the batch folder and whichever of
    STATIC, BODY, RESKIN, GRADE, PORTRAITS and the rest the chapter has. A table not named is emptied, so nothing of
    chapter 3's leaks into another chapter's run."""
    global GEN
    GEN = ROOT / "art-src/Generated" / batch
    empty = dict(STATIC={}, BODY={}, RESKIN={}, PITCH={}, GLOW={}, TINT={}, TEXTURE_OF={}, GRADE={}, BODY_KEY={}, ROW={},
                 MIRROR_FRILL={}, PORTRAITS=[], GROUND=None, PORTRAIT_REFS=None, CUT_BRIDGES={}, WHITE_CLOTHES=(), COATS={}, COLORIZE={},
                 LIVERY={}, KEEP={})
    unknown = set(tables) - set(empty)
    if unknown:
        raise KeyError(f"configure() does not know {sorted(unknown)}")
    globals().update({**empty, **tables})
    # COATS: stem -> another batch's texture, copied in once as <stem>_base_color.png so TEXTURE_OF can grade a coat
    # that was painted for a different chapter (chapter 6's Vessa wears Hessa Korl's).
    import shutil
    for stem, source in COATS.items():
        target = GEN / f"{stem}_base_color.png"
        if not target.is_file():
            GEN.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / source, target)


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def blender(script: str, *args: str, timeout: int = 600) -> None:
    subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/blender.ps1", "-Script",
                    f"tools/blender/{script}", "-TimeoutSec", str(timeout), *args], cwd=ROOT, check=True, timeout=timeout + 50)


def strip(source: Path, clean: Path) -> Path:
    if not clean.is_file():
        blender("p64_strip_embedded.py", "--input", str(source), "--output", str(clean))
    return clean


def albedo(name: str, source: Path) -> Path:
    if not source.is_file():
        raise FileNotFoundError(source)
    out = GEN / f"{name}_albedo.png"
    image = Image.open(source).convert("RGB")
    image.thumbnail((1024, 1024), Image.Resampling.LANCZOS)
    if name in GRADE:
        image = grade(image, GRADE[name])
    if name in LIVERY:
        image = livery(image, LIVERY[name], KEEP.get(name))
    if name in COLORIZE:
        image = colorize(image, *COLORIZE[name])
    image.save(out)
    return out


def livery(image: Image.Image, bands: list[tuple[float, float, float, str, str, float, float]],
           keep: tuple[float, ...] | None = None) -> Image.Image:
    """A maker's two colours laid over a coat by its light and dark, so every panel line and fold survives in the new
    colours (chapter 10: the Compact's white and teal on chapter 7's brass tender, chapter 8's bronze gun and a slate coat;
    a hue band can't do it, as the brass is one hue from its highlights to its seams). Each band repaints what falls in
    it: its dark colour where the coat's value is at or under `value low`, its light colour at or over `value high`, a
    smooth blend between. A whole-range band takes everything; a hue band has ten degrees of ramp at each edge and reaches
    down to its saturation floor (a worn coat's slate and olive are .1 saturated, under grade()'s reach), so the skin and
    hair beside them, near grey, stay as they were. KEEP (min saturation, min value[, opening in pixels]) leaves a coat's
    lit lamps and lenses as GRADE made them."""
    import numpy as np
    rgb = np.asarray(image, dtype=np.float64) / 255
    high, low = rgb.max(axis=2), rgb.min(axis=2)
    span = high - low
    safe = np.where(span > 0, span, 1)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    hue = np.where(high == r, (g - b) / safe % 6, np.where(high == g, (b - r) / safe + 2, (r - g) / safe + 4)) * 60
    sat = np.where(high > 0, span / np.where(high > 0, high, 1), 0)
    out = rgb.copy()
    for start, end, floor, dark, light, value_low, value_high in bands:
        if end - start >= 360:
            weight = np.ones_like(hue)
        else:
            width = (end - start) % 360
            inside = (hue - start) % 360
            weight = np.clip(np.minimum(inside, width - inside) / 10, 0, 1) * (inside <= width)
            weight = weight * np.clip((sat - floor) / .1, 0, 1)
        t = np.clip((high - value_low) / max(value_high - value_low, 1e-6), 0, 1)
        t = t * t * (3 - 2 * t)
        ends = [np.array([int(c[k:k + 2], 16) / 255 for k in (1, 3, 5)]) for c in (dark, light)]
        colour = ends[0] * (1 - t)[..., None] + ends[1] * t[..., None]
        out = out * (1 - weight)[..., None] + colour * weight[..., None]
    if keep:
        kept = (sat >= keep[0]) & (high >= keep[1])
        if len(keep) > 2:
            # An opening of that many pixels keeps the lamps' blocks and drops the thin bright bevels a painted metal
            # carries in the same colour (chapter 8's gun glowed orange along every edge of its white frame).
            from PIL import ImageFilter
            mask = Image.fromarray(kept.astype(np.uint8) * 255).filter(ImageFilter.MinFilter(keep[2])).filter(ImageFilter.MaxFilter(keep[2]))
            kept = np.asarray(mask) > 127
        out[kept] = rgb[kept]
    return Image.fromarray(np.clip(out * 255 + .5, 0, 255).astype(np.uint8))


def colorize(image: Image.Image, hue: float, saturation: float) -> Image.Image:
    """One hue for the whole coat at the given saturation, keeping its light and dark, and leaving near-white alone.
    A hue band can't move a coat that is nearly grey: grade() fades every band out under 0.2 saturation (chapter 7's
    glass rib, from a pale bone, came out sage green)."""
    import numpy as np
    hsv = np.asarray(image.convert("HSV"), dtype=np.float64)
    white = (hsv[..., 2] > 235) & (hsv[..., 1] < 20)
    hsv[..., 0] = np.where(white, hsv[..., 0], hue / 360 * 255)
    hsv[..., 1] = np.where(white, hsv[..., 1], saturation * 255)
    return Image.fromarray(hsv.round().astype(np.uint8), "HSV").convert("RGB")


def grade(image: Image.Image, bands: list[tuple[float, float, float, float, float]]) -> Image.Image:
    import numpy as np
    rgb = np.asarray(image, dtype=np.float64) / 255
    high, low = rgb.max(axis=2), rgb.min(axis=2)
    span = high - low
    safe = np.where(span > 0, span, 1)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    hue = np.where(high == r, (g - b) / safe % 6, np.where(high == g, (b - r) / safe + 2, (r - g) / safe + 4)) * 60
    sat = np.where(high > 0, span / np.where(high > 0, high, 1), 0)
    val = high.copy()
    shift, scale, lifted = np.zeros_like(hue), np.ones_like(hue), val.copy()
    for start, end, turn, saturation, gamma in bands:
        if end - start >= 360:
            weight = np.ones_like(hue)
        else:
            width = (end - start) % 360
            inside = (hue - start) % 360
            # Ten degrees of ramp at each edge, so a band never ends on a hard line across a gradient.
            weight = np.clip(np.minimum(inside, width - inside) / 10, 0, 1) * (inside <= width)
            weight = weight * np.clip((sat - .1) / .1, 0, 1)   # greys have no hue to belong to a band
        shift += turn * weight
        scale *= 1 + (saturation - 1) * weight
        lifted = lifted * (1 - weight) + val ** gamma * weight
    hue, sat, val = (hue + shift) % 360, np.clip(sat * scale, 0, 1), np.clip(lifted, 0, 1)
    sector = hue / 60
    chroma = val * sat
    second = chroma * (1 - np.abs(sector % 2 - 1))
    zero = np.zeros_like(hue)
    choices = [(chroma, second, zero), (second, chroma, zero), (zero, chroma, second), (zero, second, chroma), (second, zero, chroma), (chroma, zero, second)]
    index = np.clip(sector.astype(int), 0, 5)
    out = np.stack([np.choose(index, [c[k] for c in choices]) for k in range(3)], axis=2) + (val - chroma)[..., None]
    return Image.fromarray(np.clip(out * 255 + .5, 0, 255).astype(np.uint8))


def emission(name: str, source: Path, key: str | None) -> Path:
    out = GEN / f"{name}_emission.png"
    if key is None:
        Image.new("L", Image.open(source).size, 0).save(out)
    else:
        extract(source, out, key)
    return out


def tint(key: str | None) -> str:
    return {"lamp": "#FFB62E", "amber": "#FFB62E", "bulb": "#FFC46B", "lime": "#D6F23A", "cyan": "#7FEFF5", "magenta": "#E0338A", "livid": "#E0338A"}.get(key, "#FF8046")


def enemy_of(name: str, folder: str) -> str | None:
    if folder != "Enemies":
        return None
    if (ENEMY_DEFS / f"{name}.asset").is_file():
        return name
    print("C10_INTAKE_NO_ENEMYDEF", name, "(row left unbound; prepare again once the definition exists)")
    return None


def source_model(name: str, by_id: dict) -> tuple[Path, Path | None]:
    """The file to look at for a piece, and the texture to bind when that file no longer carries its own."""
    if name in STATIC:
        return GEN / f"{name}.fbx", None
    if name in BODY:
        return GEN / f"{BODY[name][0]}_Rigged.fbx", None
    source = RESKIN[name][0]
    model = GEN / f"{BODY[source][0]}_clean.fbx" if source in BODY else ROOT / by_id[source]["model"]
    graded = GEN / f"{name}_albedo.png"   # after `prepare`, look at the texture the game will get
    return model, graded if graded.is_file() else GEN / f"{TEXTURE_OF.get(name, name)}_base_color.png"


def views(only: set[str] | None, size: int) -> None:
    """Two strips per piece, stacked: front, three-quarter, side and back from eye level on the neutral floor, then the same
    four from 40 degrees up, the ground camera's pitch, on the jungle's floor colour."""
    by_id = {r["id"]: r for r in json.loads(INTAKE.read_text(encoding="utf-8"))}
    out = GEN / "views"
    out.mkdir(parents=True, exist_ok=True)
    for name in [*STATIC, *BODY, *RESKIN]:
        if only and name not in only:
            continue
        model, texture = source_model(name, by_id)
        if not model.is_file() or (texture and not texture.is_file()):
            print("C10_VIEWS_MISSING", name)
            continue
        strips = []
        for label, extra in (("eye", ["--elevation", "12"]), ("game", ["--elevation", "40", *(["--ground", GROUND] if GROUND else [])])):
            sheet = out / f"{name}-{label}.png"
            blender("p64_preview.py", "--input", str(model), "--output", str(sheet), "--size", str(size), *extra,
                    *(["--texture", str(texture)] if texture else []))
            strips.append(Image.open(sheet).convert("RGB"))
        review = Image.new("RGB", (strips[0].width, sum(s.height for s in strips)))
        for index, image in enumerate(strips):
            review.paste(image, (0, sum(s.height for s in strips[:index])))
        review.save(out / f"{name}-review.jpg", quality=88)
        print("C10_VIEWS_OK", name, rel(out / f"{name}-review.jpg"))


def overlay(by_id: dict, name: str) -> None:
    """Lay ROW's fields over a finished row at once, so a coat made in the same run copies the row as it will stand."""
    extra = dict(ROW.get(name, {}))
    source = extra.pop("clips_of", None)
    if source:
        extra["clips"] = by_id[source]["clips"]
    by_id[name].update(extra)
    if by_id[name].get("folder") == "Enemies" and "enemy" not in extra:
        by_id[name]["enemy"] = enemy_of(name, "Enemies")


def prepare(only: set[str] | None) -> None:
    by_id = {r["id"]: r for r in json.loads(INTAKE.read_text(encoding="utf-8"))}
    done = []
    for name, (size, fit, key, folder, yaw) in STATIC.items():
        if only and name not in only:
            continue
        source = GEN / f"{name}.fbx"
        if not source.is_file():
            print("C10_INTAKE_MISSING", name)
            continue
        clean = strip(source, GEN / f"{name}_clean.fbx")
        tex = albedo(name, GEN / f"{name}_base_color.png")
        by_id[name] = dict(id=name, model=rel(clean), albedo=rel(tex), emission=rel(emission(name, tex, key)),
                           emissionColor=TINT.get(name, tint(key)),
                           kind="static", folder=folder, size=size, fit=fit, yaw=yaw, clips=[], enemy=enemy_of(name, folder))
        if name in GLOW:
            by_id[name]["emissionStrength"] = GLOW[name]
        if name in PITCH:
            by_id[name]["pitch"] = PITCH[name]
        overlay(by_id, name)
        done.append(name)
    for name, (stem, size) in BODY.items():
        rigged = GEN / f"{stem}_Rigged.fbx"
        if (only and name not in only) or not rigged.is_file():
            continue
        fresh = not (GEN / f"{stem}_clean.fbx").is_file()
        clean = strip(rigged, GEN / f"{stem}_clean.fbx")
        if fresh and stem in CUT_BRIDGES:   # threads between the legs go once, when the clean file is first made
            blender("cut_bridges.py", "--input", str(clean), "--output", str(clean), "--below", str(CUT_BRIDGES[stem]))
        tex = albedo(stem, GEN / f"{stem}_Meshy_base_color.png")
        key = BODY_KEY.get(name)
        by_id[name] = dict(id=name, model=rel(clean), albedo=rel(tex), emission=rel(emission(stem, tex, key)),
                           emissionColor=TINT.get(name, tint(key)),
                           kind="biped", folder="Characters", size=size, fit="height", yaw=0, character=None, form="Natural",
                           enemy=None, clips=CLIPS)
        overlay(by_id, name)
        done.append(name)
    for name, (source_id, folder, size, key, fit) in RESKIN.items():
        if only and name not in only:
            continue
        coat = GEN / f"{TEXTURE_OF.get(name, name)}_base_color.png"
        if not coat.is_file() or source_id not in by_id:
            print("C10_INTAKE_MISSING", name)
            continue
        tex = albedo(name, coat)
        row = dict(by_id[source_id])
        if key == "source":
            mask = GEN / f"{name}_emission.png"
            Image.open(ROOT / row["emission"]).save(mask)
        else:
            mask = emission(name, tex, key)
        row.update(id=name, albedo=rel(tex), emission=rel(mask), emissionColor=TINT.get(name, tint(key)), folder=folder, size=size)
        if fit:
            row["fit"] = fit
        row["enemy"] = enemy_of(name, folder)
        row["character"] = None
        row["form"] = None if folder == "Enemies" else "Natural"
        if name in GLOW:
            row["emissionStrength"] = GLOW[name]
        by_id[name] = row
        overlay(by_id, name)
        done.append(name)
    INTAKE.write_text(json.dumps(list(by_id.values()), indent=2) + "\n", encoding="utf-8")
    print("C10_INTAKE_OK", ",".join(done))


def regrid(sheet: Path, out: Path) -> None:
    """The generated atlases drift a few pixels off an even 4x4 grid, and tools/portraits_redo_slice.py cuts on the even
    one, so a flared frill lost its tip to the next cell. Find each sheet's own gutters and lay the sixteen cells back
    on exact quarters: columns split at the middle of the white gap, rows three pixels inside the end of the bust above,
    so every bust is cut clean by its cell's bottom edge and no pale hairline is left under it."""
    import numpy as np
    image = Image.open(sheet).convert("RGB")
    side = image.width
    if image.size != (side, side) or side % 4:
        raise ValueError(f"portrait sheet must be square with a side divisible by four: {sheet}")
    cell = side // 4
    ink = (np.asarray(image) < 225).any(axis=2)
    # Broad shoulders can meet the next bust's across a column (Dabb's do), so columns are found between the heads:
    # the upper three fifths of each row of cells.
    heads = (np.arange(side) % cell) < cell * .6
    across, down = ink[heads].sum(axis=0), ink.sum(axis=1)

    def gap(profile, k: int, start_of_gap: bool) -> int:
        lo, hi = cell * k - cell // 6, cell * k + cell // 6
        clear = [i for i in range(lo, hi) if profile[i] <= 2]
        if not clear:
            raise ValueError(f"no white gutter near {cell * k} on {sheet.name}")
        runs, run = [], [clear[0]]
        for i in clear[1:]:
            if i == run[-1] + 1:
                run.append(i)
            else:
                runs.append(run)
                run = [i]
        runs.append(run)
        best = max(runs, key=lambda r: (len(r), -abs(r[len(r) // 2] - cell * k)))
        return best[0] - 3 if start_of_gap else best[len(best) // 2]

    last = max(i for i in range(side) if down[i] > 2)
    xs = [0, *(gap(across, k, False) for k in (1, 2, 3)), side]
    ys = [0, *(gap(down, k, True) for k in (1, 2, 3)), min(side, last - 2)]
    result = Image.new("RGB", (side, side), "white")
    crops = [image.crop((xs[c], ys[r], xs[c + 1], ys[r + 1])) for r in range(4) for c in range(4)]
    boxes = [Image.eval(crop, lambda v: 255 if v < 225 else 0).convert("L").getbbox() for crop in crops]   # where the drawing is
    # A sheet whose widest cell is drawn wider than a quarter (an Ilu's antennae flung out sideways) is laid out a
    # little smaller, every cell by the same amount, so no expression changes the size of the face.
    widest = max(box[2] - box[0] for box in boxes if box)
    tallest = max(box[3] - box[1] for box in boxes if box)
    shrink = min(1, (cell + 6) / widest, (cell + 6) / tallest)
    if shrink < .8:
        raise ValueError(f"a cell of {sheet.name} is far larger than a quarter of the sheet ({widest} x {tallest})")
    if shrink < 1:
        crops = [crop.resize((round(crop.width * shrink), round(crop.height * shrink)), Image.Resampling.LANCZOS) for crop in crops]
        boxes = [Image.eval(crop, lambda v: 255 if v < 225 else 0).convert("L").getbbox() for crop in crops]
        print("C10_REGRID_SHRUNK", sheet.name, round(shrink, 3))
    for r in range(4):
        for c in range(4):
            crop, box = crops[r * 4 + c], boxes[r * 4 + c]
            # Centre across, keep the bust on the cell's bottom edge; trim or pad only white. A drawing a few pixels
            # over the cell loses them from the shoulders or the bust's foot, never from the crown or a frill.
            x = (cell - crop.width) // 2
            y = cell - crop.height
            if box:
                wide, tall = box[2] - box[0], box[3] - box[1]
                x = -(box[0] + (wide - cell) // 2) if wide > cell else min(max(x, -box[0]), cell - box[2])
                y = -box[1] if tall > cell else min(max(y, -box[1]), cell - box[3])
            tile = Image.new("RGB", (cell, cell), "white")
            tile.paste(crop, (x, y))
            result.paste(tile, (c * cell, r * cell))
    out.parent.mkdir(parents=True, exist_ok=True)
    result.save(out)
    print("C10_REGRID", sheet.name, "columns", xs[1:4], "rows", ys[1:])


def key_pockets(path: Path) -> int:
    """The slicer keeps every small enclosed white patch, because teeth and eye-whites are such patches. So are the tears
    in Old Senn's frills and the gaps between a frill and a shoulder, which then show as white flecks on the dialogue
    box. Teeth and eyes sit in the middle third of a cell, below the brow; a bright white patch out at the sides or up in
    the crown is background."""
    import numpy as np
    from scipy import ndimage
    image = Image.open(path).convert("RGBA")
    rgba = np.asarray(image).copy()
    rgb = rgba[..., :3].astype(int)
    low = rgb.min(axis=2)
    white = (rgba[..., 3] > 0) & (low >= 190) & (rgb.max(axis=2) - low <= 14)
    labels, count = ndimage.label(white)
    side = rgba.shape[1]
    keyed = 0
    for index in range(1, count + 1):
        patch = labels == index
        if patch.sum() < 10 or ndimage.binary_fill_holes(patch).sum() != patch.sum():
            continue
        rows, columns = np.where(patch)
        x, y = columns.mean() / side, rows.mean() / side
        if (.32 <= x <= .67 and y >= .3) or np.median(low[patch]) < 240:   # above the brow there is only crown or fronds
            continue
        rgba[..., 3][ndimage.binary_dilation(patch, iterations=1) & (low >= 150)] = 0
        keyed += 1
    if keyed:
        Image.fromarray(rgba).save(path)
    return keyed


def keep_white_clothes(path: Path) -> int:
    """The slicer keys every near-white region that touches a cell's edge, and a white coat cut off by the cell's bottom
    edge is one (Meret's, November 12: only its teal piping was left on the dialogue box). The open background beside a
    bust always reaches a side or the top as well; a keyed region that reaches only the bottom edge is clothing, so it
    is made opaque again."""
    import numpy as np
    from scipy import ndimage
    rgba = np.asarray(Image.open(path).convert("RGBA")).copy()
    labels, count = ndimage.label(rgba[..., 3] == 0)
    kept = 0
    for index in range(1, count + 1):
        rows, columns = np.where(labels == index)
        if rows.max() == rgba.shape[0] - 1 and rows.min() > 0 and columns.min() > 0 and columns.max() < rgba.shape[1] - 1:
            rgba[..., 3][labels == index] = 255
            kept += 1
    if kept:
        Image.fromarray(rgba).save(path)
    return kept


# Teo's Neutral, the face every plain line of his uses, was drawn with one frill; his other fifteen have two.
MIRROR_FRILL = {"Teo_Natural": (0,)}
# Sheets whose speaker wears white to the bottom of the bust (see keep_white_clothes).
WHITE_CLOTHES: tuple[str, ...] = ()
# id: (hue in degrees, saturation) laid over the whole coat after GRADE (see colorize).
COLORIZE: dict[str, tuple[float, float]] = {}
# id: bands of (hue from, hue to, saturation floor, dark colour, light colour, value low, value high) laid over the coat
# after GRADE and before COLORIZE (see livery).
LIVERY: dict[str, list[tuple[float, float, float, str, str, float, float]]] = {}
# id: (min saturation, min value[, opening in pixels]): what LIVERY leaves as GRADE made it, a coat's lit lamps and lenses.
KEEP: dict[str, tuple[float, ...]] = {}


def mirror_frill(path: Path) -> None:
    """A frontal cell drawn with only one frill gets the other: the cell is mirrored about the skull's centre line and
    laid UNDER the drawing, on the rows where one side reaches further than the other, so the copy shows only where it
    comes out from behind the head. Nothing already drawn is touched."""
    import numpy as np
    rgba = np.asarray(Image.open(path).convert("RGBA")).copy()
    solid = rgba[..., 3] > 128
    height, width = solid.shape
    figure = np.where(solid.any(axis=1))[0]
    top = figure[0]
    crown = range(top + 2, top + int((figure[-1] - top) * .16))   # the top sixth is skull and nothing else
    axis = float(np.median([(np.where(solid[y])[0][0] + np.where(solid[y])[0][-1]) / 2 for y in crown]))
    reach = [(axis - np.where(solid[y])[0][0], np.where(solid[y])[0][-1] - axis) if solid[y].any() else (0, 0) for y in range(height)]
    rows = [y for y in range(height) if y < height * .72 and abs(reach[y][1] - reach[y][0]) > 5]
    if not rows:
        return
    bare_left = sum(reach[y][1] > reach[y][0] for y in rows) * 2 > len(rows)
    shift = int(round(2 * axis - (width - 1)))
    mirrored = np.zeros_like(rgba)
    flipped = rgba[:, ::-1]
    if shift >= 0:
        mirrored[:, shift:] = flipped[:, :width - shift]
    else:
        mirrored[:, :width + shift] = flipped[:, -shift:]
    side = (np.arange(width) < axis) if bare_left else (np.arange(width) > axis)
    for y in range(rows[0], rows[-1] + 1):
        under = (rgba[y, :, 3] < 255) & side
        top_layer, low_layer = rgba[y].astype(float), mirrored[y].astype(float)
        top_alpha, low_alpha = top_layer[:, 3:4] / 255, low_layer[:, 3:4] / 255
        alpha = top_alpha + low_alpha * (1 - top_alpha)
        colour = np.where(alpha > 0, (top_layer[:, :3] * top_alpha + low_layer[:, :3] * low_alpha * (1 - top_alpha)) / np.maximum(alpha, 1e-6), 0)
        rgba[y][under] = np.clip(np.concatenate([colour, alpha * 255], axis=1)[under] + .5, 0, 255).astype(np.uint8)
    Image.fromarray(rgba).save(path)
    print("C10_FRILL_MIRRORED", path.name, "rows", rows[0], rows[-1])


def portraits(only: set[str] | None) -> None:
    """Regrid, then the steps of tools/portrait_pipeline.py with the side pockets keyed between its slice and its upscale:
    key, 4x Real-ESRGAN, 2304 assembly into art-src/Generated/P3/portraits, where PortraitIntake.ImportNamed looks. Every
    sheet was opened and its sixteen cells read against the PortraitEmotion order (Neutral, Happy, Sad, Angry / Shocked,
    Worried, Intense, Synced / Amused, Smug, Curious, Flustered / Tender, Determined, Playful, Special): all six came
    back in order, and so did Garo's and Dabb's. The patrol has no face: its one emblem fills all sixteen cells."""
    esrgan = Path("C:/Users/natem/Projects/FrostboundUnity/art-src/realesrgan/realesrgan-ncnn-vulkan.exe")
    order = ",".join(str(i) for i in range(16))
    for name in PORTRAITS:
        if only and name not in only:
            continue
        work = ROOT / "art-src/Generated/P3/portraits" / name
        if (work / f"{name}.png").is_file():
            print("C10_PORTRAIT_SKIP", name)
            continue
        if name == "Patrol_Natural":
            patrol_card(GEN / "refs" / "Patrol_Emblem.png", work)
            continue
        sheet = GEN / "portraits" / f"{name}.png"
        regrid((PORTRAIT_REFS or GEN / "refs") / f"{name}.png", sheet)
        cells, up = work / "cells", work / "up4x"
        cells.mkdir(parents=True, exist_ok=True)
        up.mkdir(parents=True, exist_ok=True)
        subprocess.run([sys.executable, str(ROOT / "tools/portraits_redo_slice.py"), str(sheet), "4", "4", str(cells), name],
                       check=True, timeout=300)
        keyed = sum(key_pockets(cell) for cell in sorted(cells.glob(f"{name}__*.png")))
        if name in WHITE_CLOTHES:   # after the pockets: a coat's two halves are side patches to key_pockets
            print("C10_WHITE_CLOTHES_KEPT", name, sum(keep_white_clothes(cell) for cell in sorted(cells.glob(f"{name}__*.png"))))
        for index in MIRROR_FRILL.get(name, ()):
            mirror_frill(cells / f"{name}__{index}.png")
        subprocess.run([str(esrgan), "-i", str(cells), "-o", str(up), "-m", str(esrgan.parent / "models"),
                        "-n", "realesrgan-x4plus-anime", "-s", "4", "-f", "png"], check=True, timeout=600)
        subprocess.run([sys.executable, str(ROOT / "tools/portraits_redo_assemble.py"), str(up), name, str(work), name, order],
                       check=True, timeout=180)
        with Image.open(work / f"{name}.png") as final:
            if final.size != (2304, 2304):
                raise RuntimeError("Canonical portrait sheet must be 2304x2304")
        (work / "mapping.json").write_text(json.dumps({"source": str(sheet), "name": name, "enum_to_source": list(range(16)),
                                                       "upscale": "Real-ESRGAN realesrgan-x4plus-anime 4x",
                                                       "side_pockets_keyed": keyed}, indent=2), encoding="utf-8")
        print("C10_PORTRAIT_OK", name, "pockets keyed", keyed)
    print("C10_PORTRAITS_OK")


def patrol_card(emblem: Path, work: Path) -> None:
    """The radio card of the patrol Steady Hand: the emblem on a round white plate, the same in every cell (the speaker
    only ever uses Neutral). Scaled straight down from the 1024 original, so nothing is cut, keyed or upscaled: the
    slicer would key the white hand out of the badge as background."""
    import numpy as np
    from PIL import ImageDraw
    image = Image.open(emblem).convert("RGB")
    ink = np.argwhere((np.asarray(image) < 225).any(axis=2))
    (top, left), (bottom, right) = ink.min(axis=0), ink.max(axis=0)
    centre = ((left + right) / 2, (top + bottom) / 2)
    radius = max(right - left, bottom - top) / 2 * 1.035          # a thin white margin outside the chain
    cell, fill = 576, .86                                         # the plate's share of a cell, as a bust fills one
    scale = cell * fill / (2 * radius)
    box = [round(centre[0] - cell / scale / 2), round(centre[1] - cell / scale / 2)]
    crop = image.crop((box[0], box[1], box[0] + round(cell / scale), box[1] + round(cell / scale))).resize((cell, cell), Image.Resampling.LANCZOS)
    mask = Image.new("L", (cell * 4, cell * 4), 0)
    plate = cell * fill * 2
    ImageDraw.Draw(mask).ellipse([cell * 2 - plate, cell * 2 - plate, cell * 2 + plate, cell * 2 + plate], fill=255)
    tile = crop.convert("RGBA")
    tile.putalpha(mask.resize((cell, cell), Image.Resampling.LANCZOS))
    sheet = Image.new("RGBA", (cell * 4, cell * 4), (0, 0, 0, 0))
    for index in range(16):
        sheet.paste(tile, (index % 4 * cell, index // 4 * cell))
    work.mkdir(parents=True, exist_ok=True)
    sheet.save(work / f"{work.name}.png")
    (work / "mapping.json").write_text(json.dumps({"source": str(emblem), "name": work.name, "enum_to_source": [0] * 16,
                                                   "upscale": "none: one emblem scaled down and repeated"}, indent=2), encoding="utf-8")
    print("C10_PORTRAIT_OK", work.name, "emblem card")


PORTRAITS = ["Ruva_Natural", "Teo_Natural", "Binna_Natural", "Jask_Natural", "Senn_Natural", "Hadda_Natural",
             "Garo_Natural", "Dabb_Natural", "Patrol_Natural"]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("stage", choices=("views", "prepare", "portraits"))
    parser.add_argument("--only", help="comma-separated ids")
    parser.add_argument("--size", type=int, default=384, help="pixels per review panel")
    a = parser.parse_args()
    only = set(a.only.split(",")) if a.only else None
    if a.stage == "views":
        views(only, a.size)
    elif a.stage == "portraits":
        portraits(only)
    else:
        prepare(only)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
