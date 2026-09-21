"""Run POLISH-34's paid text-to-3D landmark batch sequentially.

Each item uses meshy.py's preview -> refine -> cache -> manifest route.  Keeping
the task list checked in makes credit intent and prompts reviewable before the
background batch starts.
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BRIDGE = ROOT / "tools" / "meshy" / "meshy.py"
HOUSE = (
    "Lattice lit-diorama low-poly game art; flat, clean color shapes with soft "
    "hand-painted gradients; no photorealism; no baked lighting, shadows, or "
    "ambient occlusion in the albedo; saturated colors constrained to the named "
    "region palette; simple readable material separation and broad details that "
    "survive a 47-degree three-quarter gameplay camera; compatible with the "
    "Quaternius low-poly kit. "
)


TASKS = (
    (
        "Glintstone_CrystalCrown_Meshy",
        "One isolated low-poly crystal crown civic monument for a fantasy mountain capital: five asymmetric faceted cyan and violet crystal spires rising from a dark blue-stone twelve-sided dais, a thin cyan halo ring at eye level, broad readable silhouette, no buildings, no ground plane, no text, no scenery, game-ready static prop.",
        "Glintstone palette only: cool navy stone, bounded cyan and violet facets, never white clipping.",
        8000,
    ),
    (
        "HollowLight_DrownedBeacon_Meshy",
        "One isolated low-poly drowned maritime beacon tower: tapered battered dark-stone shaft, three salt-stained teal tide bands, broken asymmetric black crown, one small cold cyan counted light, hanging sea chain, broad ominous silhouette, no ground plane, no text, no scenery, static game landmark.",
        "Hollow Light palette only: deep navy stone, desaturated teal salt wear, near-black Hunter crown, one restrained cyan light; no red.",
        7000,
    ),
    (
        "Sky_CreatorNeedle_Meshy",
        "One isolated low-poly Creator receiver needle for an animal-scale winch line: elegant tapered white-gunmetal spire, four cyan relay bands, open ring receiver crown, lateral winch arm and drum, one hanging cable, no balloon, no airship, no fantasy ship, no ground, no scenery, static game landmark.",
        "Sky Creator palette only: cloud-white alloy, gunmetal joints, bounded cyan emissive bands, small warm brass winch; no baked glow.",
        7000,
    ),
    (
        "Desert_WindcatcherCistern_Meshy",
        "One isolated low-poly desert civic cistern and windcatcher monument: circular honey-stone water basin, visible teal water, twin square wind towers with dark intake mouths, patched canvas shade between them, brass pulley detail, readable town-square silhouette, no ground plane, no text, no scenery.",
        "Dusthaven palette only: bleached honey stone, sand, dark brown wood, faded canvas, restrained brass and one teal water shape.",
        7000,
    ),
    (
        "Sporeveil_CapBridge_Meshy",
        "One isolated low-poly fungal bridge landmark: three giant layered mushroom-cap towers with stout organic stems, a thick root bridge joining them, broad purple-green silhouette, small bounded teal living-glow bands, no ground plane, no forest scenery, no text, static modular game architecture.",
        "Sporeveil palette only: purple caps, moss-green stems, dark roots, restrained amber and teal living glow; never photoreal fungus.",
        8000,
    ),
    (
        "Frost_ArchiveRotunda_Meshy",
        "One isolated low-poly broken Creator archive rotunda: incomplete circular white-alloy colonnade, shattered overhead ring, dark gunmetal floor ring, six inward-facing cyan census tablets, ice damage but no snow ground, broad readable static game landmark, no text, no scenery.",
        "Deep Archive palette only: ice-white alloy, gunmetal, cold blue-grey, restrained cyan tablets, no baked lighting.",
        8000,
    ),
)


def main() -> int:
    for name, prompt, region, polys in TASKS:
        command = [
            sys.executable, str(BRIDGE), "text3d", "--batch", "P34Postcards",
            "--name", name, "--band", "hero", "--target-polycount", str(polys),
            "--prompt", prompt, "--texture-prompt", HOUSE + region,
        ]
        print(f"P34_MESHY_BEGIN name={name}", flush=True)
        result = subprocess.run(command, cwd=ROOT)
        if result.returncode:
            print(f"P34_MESHY_FAILED name={name} exit={result.returncode}", flush=True)
            return result.returncode
        print(f"P34_MESHY_DONE name={name}", flush=True)
    print(f"P34_MESHY_BATCH_OK tasks={len(TASKS)}", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
