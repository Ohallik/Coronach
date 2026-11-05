#!/usr/bin/env python3
"""C9 skill icons (D201): eight icons for the skills learned with Sync level, one image task at a time
(parallel image tasks race on the manifest), each styled from the shipped skill-icons sheet, then composed
into Resources/UI/Generated/skill-icons-2.png with the same 4x2 layout (Taren on top, Sela below)."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REF = ROOT / "Lattice/Assets/_Project/Resources/UI/Generated/skill-icons.png"
BATCH = "C9-SkillIcons"
OUT = ROOT / "Lattice/Assets/_Project/Resources/UI/Generated/skill-icons-2.png"
STYLE = ("Match the reference sheet's style exactly: a single glowing game skill icon, painted sci-fi energy shapes with "
         "dark metal accents, centred, filling about 70 percent of the frame, on a flat solid very dark navy background "
         "(#0B1220) with nothing else, no text, no border, no frame, no letters. ")
TAREN = "Warm amber, orange and gold energy (Taren's palette). "
SELA = "Cool cyan, electric blue and violet energy (Sela's palette). "
ICONS = [
    ("anchor-slam", TAREN + "A heavy anchor-shaped blade plunging down into cracked ground, with a shockwave ring bursting outward."),
    ("tether-hook", TAREN + "A glowing barbed hook on a taut energy chain, whipping forward in a curve."),
    ("forge-spiral", TAREN + "A blade spinning in a fiery spiral vortex, two full rings of molten sparks."),
    ("faultline", TAREN + "A straight line of ground rupturing ahead, jagged rock shards erupting in a row with molten light in the crack."),
    ("survey-drone-ring", SELA + "A small hovering survey drone shaped like a glowing lens inside a single flat horizontal ring, like a planet with one ring, three short antenna spikes on top, firing a thin needle beam downward. No wings, no side panels."),
    ("waypoint-blink", SELA + "A sleek figure-shaped afterimage blinking away, leaving behind a glowing diamond waypoint marker about to burst."),
    ("gravity-fold", SELA + "A swirling violet gravity well, space folding inward into a bright singularity with debris spiralling in."),
    ("starfall", SELA + "Several vertical beams of light falling from above into a glowing marked circle on the ground."),
]


def main() -> int:
    refs = ROOT / "art-src" / "Generated" / BATCH / "refs"
    for name, prompt in ICONS:
        if (refs / f"{name}.png").exists():
            print(f"SKILL_ICON_SKIP {name}", flush=True)
            continue
        cmd = [sys.executable, str(ROOT / "tools/meshy/meshy.py"), "image2image", "--prompt", STYLE + prompt,
               "--name", name, "--batch", BATCH, "--prompt-id", "C9", "--aspect-ratio", "1:1", "--reference", str(REF)]
        print("SKILL_ICON_RUN", name, flush=True)
        subprocess.run(cmd, check=True, cwd=ROOT)
    cell = 443
    sheet = Image.new("RGB", (cell * 4, cell * 2), (11, 18, 32))
    for index, (name, _) in enumerate(ICONS):
        icon = Image.open(refs / f"{name}.png").convert("RGB").resize((cell, cell), Image.LANCZOS)
        sheet.paste(icon, ((index % 4) * cell, (index // 4) * cell))
    sheet.save(OUT)
    print(f"SKILL_ICONS_OK {OUT}", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
