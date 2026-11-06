#!/usr/bin/env python3
"""QA pass intake: prepares the Survey Drone for GenIntake.BuildPrefix, the way tools/c9_intake.py does static props.

Strips the FBX's embedded textures (Blender), keeps a <=1024 albedo and a cyan emission mask, and adds an intake row.

  python tools/qa_drone_intake.py
"""
from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from emission_mask import extract  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
GEN = ROOT / "art-src/Generated/QA-Drone"
INTAKE = ROOT / "docs/art/intake.json"
NAME = "SurveyDrone"
# A skill's small body: the ring is about 0.75 m across, which reads at the ground camera's distance.
SIZE = 0.62


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def main() -> int:
    source = GEN / f"{NAME}.fbx"
    clean = GEN / f"{NAME}_clean.fbx"
    if not clean.is_file():
        subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/blender.ps1",
                        "-Script", "tools/blender/p64_strip_embedded.py", "--input", str(source), "--output", str(clean)],
                       cwd=ROOT, check=True, timeout=650)
    albedo = GEN / f"{NAME}_albedo.png"
    image = Image.open(GEN / f"{NAME}_base_color.png").convert("RGB")
    image.thumbnail((1024, 1024), Image.Resampling.LANCZOS)
    image.save(albedo)
    emission = GEN / f"{NAME}_emission.png"
    extract(albedo, emission, "cyan")
    rows = json.loads(INTAKE.read_text(encoding="utf-8"))
    by_id = {r["id"]: r for r in rows}
    by_id[NAME] = dict(id=NAME, model=rel(clean), albedo=rel(albedo), emission=rel(emission), emissionColor="#46E6FF",
                       kind="static", folder="Environment", size=SIZE, fit="height", yaw=0, clips=[])
    INTAKE.write_text(json.dumps(list(by_id.values()), indent=2) + "\n", encoding="utf-8")
    lit = sum(1 for p in Image.open(emission).convert("L").getdata() if p > 40)
    print("QA_DRONE_INTAKE_OK", NAME, "emission pixels", lit, "of", Image.open(emission).width * Image.open(emission).height)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
