#!/usr/bin/env python3
"""C9 Lantern Road intake (D202): prepares the chapter's generated pieces for GenIntake.BuildPrefix.

Static props: strip the FBX's embedded textures (Blender), keep a <=1024 albedo and an emission mask, add an
intake row. Reskins: Meshy retextured an existing rigged body with its original UVs, so the new texture is used
on the ORIGINAL rigged model (the retexture output may lose its skeleton); the row copies the source's clips.

  python tools/c9_intake.py            # every finished piece
  python tools/c9_intake.py --only LanternTower,Gloomgrazer
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
    "BrekkBody": ("SentinelHusk", "Characters", None, 2.25, None),
    "MusterSkiff": ("PatrolCutter", "Enemies", "MusterSkiff", 3.6, None),
    "AumiBody": ("Neve", "Characters", None, 1.86, "amber"),
    "CorraBody": ("Mira", "Characters", None, 1.84, "orange"),
    "IshenBody": ("Hal", "Characters", None, 1.88, None),
    "PellBody": ("SurvivorNatural", "Characters", None, 1.7, None),
}
TEXTURE_OF = {"MusterSergeant": "MusterScav"}


def albedo(name: str) -> Path:
    source = GEN / f"{TEXTURE_OF.get(name, name)}_base_color.png"
    if not source.is_file():
        raise FileNotFoundError(source)
    out = GEN / f"{name}_albedo.png"
    image = Image.open(source).convert("RGB")
    image.thumbnail((1024, 1024), Image.Resampling.LANCZOS)
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
    a = parser.parse_args()
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
    for name, (source_id, folder, enemy, size, key) in RESKIN.items():
        if only and name not in only:
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
