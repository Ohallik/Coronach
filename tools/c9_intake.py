#!/usr/bin/env python3
"""C9 Lantern Road intake (D202): prepares the chapter's generated pieces for GenIntake.BuildPrefix.

Static props: strip the FBX's embedded textures (Blender), keep a <=1024 albedo and an emission mask, add an
intake row. Reskins: Meshy retextured an existing rigged body with its original UVs, so the new texture is used
on the ORIGINAL rigged model (the retexture output may lose its skeleton); the row copies the source's clips.

  python tools/c9_intake.py            # every finished piece
  python tools/c9_intake.py --only LanternTower,Gloomgrazer
  python tools/c9_intake.py --batch QA-Cast --only BrekkBody,HessaKorl,IshenBody,PellBody   # the November retextures
  python tools/c9_intake.py --batch QA-Hessa --only HessaKorl,BrekkBody,CorraBody,IshenBody   # and the whole new bodies
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
GEN = ROOT / "art-src/Generated/C9-Lantern"
INTAKE = ROOT / "docs/art/intake.json"

# name: (height m, emission key or None, fit)
STATIC = {
    "LanternTower": (12, "lamp", "height"), "AncelaneArch": (8, "lamp", "height"), "CanyonHouse": (5, "lamp", "height"),
    "RelayMirror": (1.8, None, "height"), "AncelaneColumn": (4.5, None, "height"), "GreatLantern": (4, "lamp", "height"),
    "MusterCrate": (1.1, None, "height"), "MusterLeanTo": (2.8, None, "height"), "MusterCutterRig": (2.2, "orange", "height"),
    "OrchardTree": (3.2, None, "height"), "LampPost": (1.6, "lamp", "height"), "LanternShard": (1.2, None, "height"),
}
# name: (source intake id, folder, enemy id or None, size override, emission key)
RESKIN = {
    "Gloomgrazer": ("Ridgehound", "Enemies", "Gloomgrazer", 2.3, "amber"),
    "MusterScav": ("SentinelHusk", "Enemies", "MusterScav", 2.3, "orange"),
    "MusterSergeant": ("SentinelHusk", "Enemies", "MusterSergeant", 2.75, "orange"),
    "HessaKorl": ("SentinelHusk", "Enemies", "HessaKorl", 2.6, "amber"),
    # A deserter in a smith's apron: the armoured husk made a friendly read as the boss just fought (QA, November).
    "BrekkBody": ("Orrin", "Characters", None, 2.1, None),
    "MusterSkiff": ("PatrolCutter", "Enemies", "MusterSkiff", 3.6, None),
    "AumiBody": ("Neve", "Characters", None, 1.86, "amber"),
    "CorraBody": ("Mira", "Characters", None, 1.84, "orange"),
    "IshenBody": ("Hal", "Characters", None, 1.88, None),
    "PellBody": ("SurvivorNatural", "Characters", None, 1.7, None),
}
# Whole new bodies from tools/meshy/qa_body_batch.py, on the ids the game already uses: id: (file stem, size override).
# The row keeps its clips and folder; only the mesh, the textures and the height change.
BODY = {"HessaKorl": ("Hessa", 2.6), "BrekkBody": ("Brekk", 2.1), "CorraBody": ("Corra", 1.9), "IshenBody": ("Ishen", 1.88)}
TEXTURE_OF = {"MusterSergeant": "MusterScav"}
# Gamma under 1 lifts a texture that came back too dark to read in its zone's light.
LIFT = {"BrekkBody": .72}


def albedo(name: str) -> Path:
    source = GEN / f"{TEXTURE_OF.get(name, name)}_base_color.png"
    if not source.is_file():
        raise FileNotFoundError(source)
    out = GEN / f"{name}_albedo.png"
    image = Image.open(source).convert("RGB")
    image.thumbnail((1024, 1024), Image.Resampling.LANCZOS)
    if name in LIFT:
        image = image.point(lambda v: round(255 * (v / 255) ** LIFT[name]))
    image.save(out)
    return out


def emission(name: str, source: Path, key: str | None) -> Path:
    out = GEN / f"{name}_emission.png"
    if key is None:
        Image.new("L", Image.open(source).size, 0).save(out)
    else:
        extract(source, out, key)
    return out


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only")
    parser.add_argument("--batch", default="C9-Lantern", help="the art-src/Generated folder holding the textures")
    a = parser.parse_args()
    global GEN
    GEN = ROOT / "art-src/Generated" / a.batch
    only = set(a.only.split(",")) if a.only else None
    rows = json.loads(INTAKE.read_text(encoding="utf-8"))
    by_id = {r["id"]: r for r in rows}
    done = []
    for name, (height, key, fit) in STATIC.items():
        if only and name not in only:
            continue
        source = GEN / f"{name}.fbx"
        if not source.is_file():
            print("C9_INTAKE_MISSING", name)
            continue
        clean = GEN / f"{name}_clean.fbx"
        if not clean.is_file():
            subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/blender.ps1",
                            "-Script", "tools/blender/p64_strip_embedded.py", "--input", str(source), "--output", str(clean)],
                           cwd=ROOT, check=True, timeout=650)
        tex = albedo(name)
        row = dict(id=name, model=rel(clean), albedo=rel(tex), emission=rel(emission(name, tex, key)),
                   emissionColor="#FFB62E" if key in ("lamp", "amber") else "#FF8046", kind="static",
                   folder="Environment", size=height, fit=fit, yaw=0, clips=[])
        by_id[name] = row
        done.append(name)
    for name, (stem, size) in BODY.items():
        rigged = GEN / f"{stem}Body_Rigged.fbx"
        if (only and name not in only) or not rigged.is_file():
            continue
        clean = GEN / f"{stem}Body_clean.fbx"
        if not clean.is_file():
            subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/blender.ps1",
                            "-Script", "tools/blender/p64_strip_embedded.py", "--input", str(rigged), "--output", str(clean)],
                           cwd=ROOT, check=True, timeout=650)
        tex = GEN / f"{stem}Body_albedo.png"
        image = Image.open(GEN / f"{stem}Body_Meshy_base_color.png").convert("RGB")
        image.thumbnail((1024, 1024), Image.Resampling.LANCZOS)
        image.save(tex)
        row = dict(by_id[name])
        row.update(model=rel(clean), albedo=rel(tex), emission=rel(emission(f"{stem}Body", tex, None)), size=size)
        # She yields on the way down: the getting-up clip passes through the low, spent pose no other clip has.
        if name == "HessaKorl" and not any(c["state"] == "Revive" for c in row["clips"]):
            row["clips"] = row["clips"] + [{"state": "Revive", "path": "Assets/_Project/Art/Animation/HeroRevive.fbx", "name": "Revive"}]
        by_id[name] = row
        done.append(name)
    for name, (source_id, folder, enemy, size, key) in RESKIN.items():
        if (only and name not in only) or name in done:
            continue
        if not (GEN / f"{TEXTURE_OF.get(name, name)}_base_color.png").is_file():
            print("C9_INTAKE_MISSING", name)
            continue
        source = by_id[source_id]
        tex = albedo(name)
        row = dict(source)
        row.update(id=name, albedo=rel(tex), emission=rel(emission(name, tex, key)), folder=folder, size=size,
                   emissionColor="#FFB62E" if key == "amber" else "#FF8046")
        row["enemy"] = enemy
        row["character"] = None
        row["form"] = None if folder == "Enemies" else "Natural"
        by_id[name] = row
        done.append(name)
    INTAKE.write_text(json.dumps(list(by_id.values()), indent=2) + "\n", encoding="utf-8")
    print("C9_INTAKE_OK", ",".join(done))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
