#!/usr/bin/env python3
"""November QA pass: bodies that match the faces in the dialogue box.

The review found Brekk, a quiet deserter in a smith's apron, on the armoured SentinelHusk mesh, so a friendly looked like the boss
just fought, and Hessa, Pell and Ishen in colours their portraits don't share. These are original-UV retextures of existing rigged
bodies, one task at a time, each skipped if its output exists. tools/c9_intake.py takes them from here, with --batch QA-Cast.
"""
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BATCH = "QA-Cast"
GEN = ROOT / "art-src" / "Generated" / BATCH
MESHY = [sys.executable, str(ROOT / "tools/meshy/meshy.py")]
TAIL = (". Every exposed part of the body, including face, neck, hands and bare feet, is painted this skin colour. Preserve "
        "palette semantics and original UV islands; flat clean cel-shaded game texture, no baked lighting.")

RESKINS = {
    # name: (source clean fbx, prompt)
    "BrekkBody": ("art-src/Generated/P3-Orrin/Orrin_clean.fbx",
        "Vaskar blacksmith in plain civilian work clothes, a gentle tired man: weathered olive-grey skin grown in large "
        "overlapping plates with slightly darker seams, a long healed scar across one cheek, tired amber eyes, no hair, "
        "dark grey crest behind the skull edged with dull rust; a worn mid-brown leather smith's apron with visible stitching "
        "over a light coarse grey shirt with rolled sleeves, grey-brown trousers, scuffed brown boots, soot smudges. Lightly "
        "coloured and easy to read in sunlight, no armor, no weapons, no glowing parts, no black clothing"),
    "HessaKorl": ("art-src/Generated/P3-SentinelHusk/SentinelHusk_clean.fbx",
        "Muster scavenger captain, a proud Vaskar woman in salvage plate: warm rust-bronze and burnt orange armor plates "
        "with fine black seams and rust-red edging, a gunmetal collar, a ragged bright crimson captain's sash across one "
        "shoulder and hip, tall crimson crest fins on the helm, a bright amber visor and an amber crystal shard bound to the "
        "chest plate. Mostly warm mid-tone orange-bronze rather than black, so the figure reads clearly against a dark floor"),
    "IshenBody": ("art-src/Generated/P3-Hal/Hal_clean.fbx",
        "elderly Vael archivist man: cool pale grey-blue skin with faded violet lines at the temples, thin white combed-back "
        "hair, small round brass reading glasses low on the nose, pale blue eyes; a layered pale sand-coloured scholar's coat "
        "with many small pockets over cream trousers, soft pale cloth shoes, a bundle of paper slips at the collar"),
    "PellBody": ("art-src/Generated/P44-NPCBodies/SurvivorNatural_clean.fbx",
        "young Vael lamp apprentice: light slate-blue pale skin, faint cyan lines on the cheeks, a smudge of soot on one cheek, "
        "green eyes, short spiky pale sand-white hair; a patched pale tan work tunic with rolled sleeves, tan-brown trousers, "
        "soft brown boots, a small brass wick-trimmer on a cord"),
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", help="comma-separated names")
    only = parser.parse_args().only
    GEN.mkdir(parents=True, exist_ok=True)
    for name, (model, prompt) in RESKINS.items():
        if only and name not in only.split(","):
            continue
        if (GEN / f"{name}.fbx").exists():
            print("QA_CAST_SKIP", name, flush=True)
            continue
        subprocess.run(MESHY + ["retexture", "--model", str(ROOT / model), "--prompt", prompt + TAIL, "--name", name,
                                "--batch", BATCH, "--prompt-id", "QA-Cast"], check=True, cwd=ROOT)
    print("QA_CAST_OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
