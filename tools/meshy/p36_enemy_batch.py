#!/usr/bin/env python3
"""POLISH-36 original-UV enemy surface batch (five weakest semantic families)."""

from __future__ import annotations

import argparse
from pathlib import Path

from meshy import (Client, HOUSE_TEXTURE_PROMPT, create_record, data_uri,
                   download_task, load_key, wait_for)


ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Lattice" / "Assets" / "_Project" / "Art" / "Packs" / "Quaternius_SciFiEssentialsKit"

TASKS = (
    ("Enemy_EyeDrone.fbx", "P36_EyeDrone_Creator",
     "Creator-era compact eye drone; pale weathered alloy shell, broad graphite recesses, "
     "one unmistakable cyan optic and cyan circuit accents; clean survey machine, not corrupted"),
    ("Enemy_EyeDrone.fbx", "P36_EyeDrone_Hunter",
     "Hunter-corrupted compact eye drone; charcoal-violet armor, chipped iron edges, one "
     "unmistakable red optic and sparse red warning accents; hostile enforcement machine"),
    ("Enemy_Trilobite.fbx", "P36_Trilobite_Creator",
     "Creator-era small arch sentinel; pale alloy segmented shell, slate joints, readable cyan "
     "crest line and cyan sensor nodes; ancient but maintained"),
    ("Enemy_Trilobite.fbx", "P36_Trilobite_Hunter",
     "Hunter-corrupted small arch sentinel; dark iron segmented shell, maroon wear, readable "
     "red crest line and red sensor nodes; no cyan"),
    ("Enemy_QuadShell.fbx", "P36_QuadShell_Field",
     "small quadruped crab mech; weathered bronze and slate shell plates, dark joints, sparse "
     "red hostile sensor accents; broad graphic wear shapes, no photoreal grunge"),
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--interval", type=float, default=12)
    parser.add_argument("--timeout", type=float, default=3600)
    args = parser.parse_args()
    client = Client(load_key())
    for model_name, output_name, direction in TASKS:
        model = PACK / model_name
        output = ROOT / "art-src" / "Generated" / "P36Enemies" / f"{output_name}.fbx"
        if output.is_file():
            print(f"P36_MESHY_SKIP {output_name}", flush=True)
            continue
        prompt = f"{HOUSE_TEXTURE_PROMPT} {direction}. Preserve palette semantics and original UV islands."
        payload = {
            "model_url": data_uri(str(model)),
            "text_style_prompt": prompt,
            "enable_original_uv": True,
            "remove_lighting": True,
            "target_formats": ["fbx"],
        }
        task_id = create_record(client, "retexture", "retexture", payload, prompt,
                                "P36Enemies", "P36")
        task = wait_for(client, "retexture", task_id, args.interval, args.timeout)
        download_task(task, task_id, "P36Enemies", output_name)
    print(f"P36_ENEMY_BATCH_OK tasks={len(TASKS)}", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
