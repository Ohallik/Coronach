"""POLISH-34 original-UV hero retexture (the P30 route-d winner)."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
prompt = (
    "Lattice lit-diorama low-poly game art; flat, clean color shapes with soft "
    "hand-painted gradients; no photorealism; no baked lighting, shadows, or ambient "
    "occlusion in the albedo; saturated colors constrained to the named region palette; "
    "simple readable material separation and broad details that survive a 47-degree "
    "three-quarter gameplay camera; compatible with the Quaternius low-poly kit. "
    "Crownport civic monument: neutral honey-grey cut stone, broad salt-worn blue trim, "
    "restrained warm brass insets, sparse hand-painted edge variation, no moss, no text."
)
command = [
    sys.executable, str(ROOT / "tools/meshy/meshy.py"), "retexture",
    "--model", str(ROOT / "art-src/Generated/P30Bakeoff/BuilderHero_UV.fbx"),
    "--batch", "P34Postcards", "--name", "Crownport_TextureMonument_Meshy",
    "--prompt", prompt,
]
raise SystemExit(subprocess.run(command, cwd=ROOT).returncode)
