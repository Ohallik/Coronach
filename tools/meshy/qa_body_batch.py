#!/usr/bin/env python3
"""November QA pass: whole new bodies for the people whose portrait and figure are plainly different people.

A retexture can't fix a shape. Hessa was a headless armoured mech under a horned face, Brekk an ordinary man under a stone
one, and Corra and Ishen wore someone else's outline. Each runs the C8 speaker-body path: the portrait sheet is the identity
and style reference for ONE full-body T-pose image, that image becomes a Meshy T2 mesh, and Meshy's humanoid rig skins it.
One task at a time; every step is skipped when its output exists, so a stopped run picks up where it left off.

  python tools/meshy/qa_body_batch.py --only Brekk,Corra,Ishen
  python tools/meshy/qa_body_batch.py --stage ref    # stop after the images, to look at them first
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BATCH = "QA-Hessa"
GEN = ROOT / "art-src" / "Generated" / BATCH
PORTRAITS = ROOT / "art-src" / "Generated" / "C9-Lantern" / "refs"
MESHY = [sys.executable, str(ROOT / "tools/meshy/meshy.py")]
MANIFEST = ROOT / "docs" / "art" / "gen-manifest.json"

HEAD = ("Use case: stylized-concept. Asset type: single 3D game-character modeling reference for Coronach. The attached "
        "portrait sheet is ONLY an identity and style reference: create exactly ONE full-body figure of its character, no "
        "sheet, no extra views. ")
TAIL = (" No weapon, backpack, cape, tools or floating accessories. Front-facing orthographic full-body view, neutral "
        "expression, head straight aligned with torso, natural adult game proportions, both arms straight horizontally in a "
        "T pose, hands open with separated fingers, feet shoulder-width separated and pointing forward. Entire body, head "
        "shape and fingertips comfortably inside image with white margin. Flat matte toon-color illustration, crisp "
        "restrained linework, only subtle local shading, no cast or ground shadows, highlights, gloss, ambient occlusion, "
        "bloom or dramatic lighting; uniform pure white background, no scenery, text, labels, dividers or watermark. This is "
        "ONE single isolated character only.")

BODIES = {
    # name: (portrait, reference file stem, body description, rig height in metres, short mesh prompt)
    "Hessa": ("Hessa_Natural", "Hessa_Body",
        "Hessa Korl, an adult Vaskar alien woman and scavenger captain, tall, lean and heavy-boned with square armored "
        "shoulders. Keep her exact head from the portrait: narrow fierce face of warm rust-bronze orange skin grown in large "
        "overlapping plates across brow, cheeks, jaw and neck with fine black seams, bright hard amber eyes, two tall crimson "
        "ribbed membranous crest-sails rising from the sides and back of the skull like folded fans, large and clearly "
        "visible, a thin cut-glass earring; no hair, no human ears, no helmet. Keep her dark gunmetal salvage armor collar "
        "with rust-red riveted plates and the ragged bright crimson captain's sash across one shoulder. Extend the outfit "
        "naturally: a fitted salvage cuirass of warm rust-red and burnt-orange plates with dark gunmetal straps, the crimson "
        "sash falling across the chest to one hip, bare rust-bronze plated upper arms, rust-red plated forearm guards and "
        "matte dark gloves, a wide belt, fitted dark gunmetal trousers with rust-red plated thigh and shin guards, heavy dark "
        "salvage boots. Mostly warm mid-tone rust-orange and crimson rather than black, so the figure reads clearly on a dark floor.",
        2.0, "Hessa Korl, Vaskar alien scavenger captain woman, rust-bronze plated skin, tall crimson crest-sails, rust-red salvage armor, ragged crimson sash, T pose"),
    "Brekk": ("Brekk_Natural", "Brekk_Body",
        "Brekk Tamar, an adult Vaskar alien man, a quiet tired smith, tall, broad and heavy-boned with thick arms. Keep his "
        "exact head from the portrait: a broad heavy face of weathered olive-grey skin grown in large overlapping stone-like "
        "plates across brow, cheeks, jaw and neck with slightly darker seams, a long pale healed scar across one cheek plate, "
        "tired amber eyes, two tall dark grey ribbed membranous crest-sails edged with dull rust rising from the sides and "
        "back of the skull like folded fans, large and clearly visible; no hair, no human ears, no helmet. Keep his plain "
        "coarse light grey shirt and worn brown leather straps. Extend the outfit naturally: sleeves rolled to the elbow over "
        "bare olive-grey plated forearms, a long worn mid-brown leather smith's apron with visible stitching from chest to "
        "knee, soot smudges, grey-brown work trousers, scuffed brown work boots. Lightly coloured and easy to read in "
        "sunlight, no armor, no glowing parts.",
        2.0, "Brekk Tamar, Vaskar alien smith man, olive-grey stone plated skin, dark crest-sails, brown leather apron over grey shirt, T pose"),
    "Corra": ("Corra_Natural", "Corra_Body",
        "Corra Vey, an adult Vael alien woman, a brisk market trader of medium athletic build. Keep her exact head from the "
        "portrait: strong square face of warm umber-brown skin, bright orange sync-lines along the jaw and down the throat, "
        "dark-bronze ring irises, a large crest of thick copper-red flat keratin fronds, tied up high with a green cloth band "
        "and falling behind her shoulders, a stub of chalk behind one frond; no human hair, no ears. Keep her dark green "
        "market vest with fine gold piping over a cream shirt, and the brown leather strap across the chest. Extend the "
        "outfit naturally: cream sleeves rolled to the elbow, bare umber forearms with orange lines, a broad brown leather "
        "coin belt with small pouches, sturdy warm tan trousers, soft brown boots. Warm and bright, easy to read in sunlight.",
        1.8, "Corra Vey, Vael alien market trader woman, umber skin, copper-red keratin fronds tied with green cloth, dark green vest, cream shirt, tan trousers, T pose"),
    "Pell": ("Pell_Natural", "Pell_Body",
        "Pell, a young adult Vael alien lamp apprentice, barely grown, slight and quick. Keep the exact head from the portrait: a round "
        "open youthful face of light slate-blue skin, big bright green ring irises, a smudge of lamp soot on one cheek, faint cyan "
        "sync-lines on the cheeks and throat, a crown of short springy sand-coloured flat keratin fronds sticking up and back, large "
        "and clearly visible; no human hair. Keep the patched ochre work tunic with green and brown cloth patches at the shoulders "
        "and the small brass wick-trimmer hanging on a cord at the chest. Extend the outfit naturally: sleeves rolled to the elbow "
        "over slim slate-blue forearms, a simple rope belt, tan-brown work trousers patched at one knee, soft brown boots. Warm and "
        "light-coloured, easy to read on an orange canyon floor, and clearly young.",
        1.72, "Pell, young Vael alien apprentice, slate-blue skin, crown of sand-coloured keratin fronds, patched ochre tunic, tan trousers, T pose"),
    "Ishen": ("Ishen_Natural", "Ishen_Body",
        "Ishen, an elderly Vael alien man, a gentle slender archivist. Keep his exact head from the portrait: long gentle face "
        "of cool pale grey-blue skin with deep laugh lines and faded violet sync-lines at the temples and down the throat, "
        "small round brass reading lenses perched low on the nose, pale blue ring irises, a crest of thin white flat keratin "
        "fronds combed back from the brow; no human hair. Keep his layered high-collared pale cream-white scholar's coat with "
        "many small pockets and rolled record slips tucked at the collar and chest, a muted brown under-tunic and brown "
        "leather straps. Extend the outfit naturally: a long pale cream coat to the knee, cool grey-blue hands, soft grey-blue "
        "trousers, soft dark brown shoes. Cool pale colours that stand clearly apart from a warm sandy floor.",
        1.85, "Ishen, elderly Vael alien archivist, pale grey-blue skin, white keratin fronds, round brass glasses, long pale cream coat with pockets, T pose"),
}


def run(*args: str) -> str:
    done = subprocess.run(MESHY + list(args), cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    out = done.stdout + done.stderr
    if done.returncode != 0:
        sys.exit(f"QA_BODY_FAILED {' '.join(args[:2])}\n{out[-800:]}")
    return out


def task_of(out: str) -> str:
    found = re.findall(r"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}) SUCCEEDED", out)
    if not found:
        sys.exit("QA_BODY_NO_TASK\n" + out[-400:])
    return found[-1]


def img3d_task_from_manifest(name: str) -> str | None:
    """A stopped run can still rig a mesh it didn't generate: the provenance ledger knows the task."""
    want = f"{name}Body_Meshy.fbx".lower()
    for row in reversed(json.loads(MANIFEST.read_text(encoding="utf-8-sig"))):
        if row.get("mode") != "img3d":
            continue
        if any(path and Path(path).name.lower() == want for path in (row.get("downloaded") or []) + [row.get("output_path") or ""]):
            return row.get("task_id")
    return None


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", help="comma-separated names")
    parser.add_argument("--stage", choices=("ref", "all"), default="all")
    a = parser.parse_args()
    GEN.mkdir(parents=True, exist_ok=True)
    only = a.only.split(",") if a.only else None
    for name, (portrait, stem, body, height, short) in BODIES.items():
        if only and name not in only:
            continue
        ref = GEN / "refs" / f"{stem}.png"
        if not ref.exists():
            run("image2image", "--prompt", HEAD + body + TAIL, "--name", stem, "--batch", BATCH, "--prompt-id", "QA-Cast",
                "--reference", str(PORTRAITS / f"{portrait}.png"), "--aspect-ratio", "1:1")
            print("QA_BODY_REF", name, flush=True)
        if a.stage == "ref":
            continue
        mesh = GEN / f"{name}Body_Meshy.fbx"
        task = None
        if not mesh.exists():
            task = task_of(run("img3d", "--image", str(ref), "--band", "humanoid", "--target-polycount", "12000", "--batch", BATCH,
                               "--prompt-id", "QA-Cast", "--name", f"{name}Body_Meshy", "--riggable", "--prompt", short))
            print("QA_BODY_MESH", name, task, flush=True)
        rigged = GEN / f"{name}Body_Rigged.fbx"
        if not rigged.exists():
            task = task or img3d_task_from_manifest(name)
            if not task:
                sys.exit(f"QA_BODY_NO_TASK {name}")
            run("rig", "--input-task-id", task, "--height", str(height), "--batch", BATCH, "--prompt-id", "QA-Cast",
                "--name", f"{name}Body_Rigged")
            print("QA_BODY_RIG", name, flush=True)
    print("QA_BODY_OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
