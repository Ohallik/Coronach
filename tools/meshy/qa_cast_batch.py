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
HULL_TAIL = ". Preserve palette semantics and original UV islands; flat clean cel-shaded game texture, no baked lighting."

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
    # Chapter 1's first boss is a drill rig, and it read as a giant tan grub in the floor's own colour.
    "Burrower": ("art-src/Generated/P3-Burrower/Burrower_clean.fbx",
        "Massive mining drill machine, a burrowing rig gone rogue: every surface is heavy industrial steel. Dark gunmetal armor plates "
        "with brushed lighter steel along their edges, bold black panel lines and rows of rivets, oil streaks and scorched soot, exposed "
        "hydraulic joints between the segments, three jaws of worn bright bare steel with worn cutting teeth, a glowing cyan core deep "
        "in its maw. One restrained colour family of cold steel greys, with a single stencilled white number. No yellow, no orange, no "
        "stripes, no spots, no pattern. Hard-edged machine, nothing organic, not an animal"),
    # Not a person: the first enemy of chapter 2 read as soft clay on a dark sky. Hard paint and hull lines, warm for hostile as the other flight enemies are.
    "MusterSkiff": ("art-src/Generated/P3-PatrolCutter/PatrolCutter_clean.fbx",
        "Muster raider skiff, a hostile salvage gunship: hard-edged burnt-orange and rust-red painted hull plates separated by bold "
        "near-black panel lines and rows of rivets, cream and black hazard chevrons on the wing tips, gunmetal engine nozzles scorched "
        "black, a crude white tally stencil on the hull, chipped paint at plate edges. High contrast and crisp, nothing muddy or brown"),
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
        subprocess.run(MESHY + ["retexture", "--model", str(ROOT / model), "--prompt", prompt + (HULL_TAIL if name in ("MusterSkiff", "Burrower") else TAIL), "--name", name,
                                "--batch", BATCH, "--prompt-id", "QA-Cast"], check=True, cwd=ROOT)
    print("QA_CAST_OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
