#!/usr/bin/env python3
"""S3 skill icons: the six skills learned at Sync 16, 20 and 24 and the two EXTRA moves, as ONE generated sheet
(image2image costs 12 credits a task, so eight single icons as in c9_skill_icons.py would be 96), styled from the
shipped skill-icons sheet, then cut into its 4x2 cells and composed into Resources/UI/Generated/skill-icons-3.png with
the same layout as the other two sheets (Taren on top, Sela below; the fourth of each row is the hero's EXTRA).

    python tools/meshy/s3_skill_icons.py                # attempt 1: one image task, then compose
    python tools/meshy/s3_skill_icons.py --attempt 2    # the one retry, if the first sheet came back poor
    python tools/meshy/s3_skill_icons.py --compose 1    # compose again from an attempt already downloaded (no credits)

Hard cap: 40 credits for the batch, counted from the manifest, so a fourth task is refused."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REF = ROOT / "Lattice/Assets/_Project/Resources/UI/Generated/skill-icons.png"
BATCH = "S3-SkillIcons"
OUT = ROOT / "Lattice/Assets/_Project/Resources/UI/Generated/skill-icons-3.png"
MANIFEST = ROOT / "docs/art/gen-manifest.json"
CAP, TASK = 40, 12
PROMPT = (
    "A sprite sheet of eight game skill icons in a strict grid of 4 columns and 2 rows. Each icon sits alone in the "
    "middle of its own equal cell, fills about 65 percent of the cell, and is separated from its neighbours by wide "
    "empty gutters; no icon touches another icon or the edge of the image. Match the reference sheet's style exactly: "
    "painted glowing sci-fi energy shapes with dark gunmetal accents and small floating shards, on one flat solid very "
    "dark navy background (#0B1220) across the whole image. No text, no letters, no numbers, no borders, no frames, no "
    "grid lines, no cell outlines. "
    "TOP ROW, left to right, all in warm amber, orange and gold energy: "
    "(1) a heavy plated tower shield with a glowing ram point, charging to the right with speed streaks behind it; "
    "(2) a red-hot branding iron stamp: a burning circular seal with a bright ember mark at its centre and sparks; "
    "(3) one huge greatsword cutting straight down through a block of rock, the rock split in two with a molten cut line; "
    "(4) an anvil struck by four hammers at once inside a wide golden halo ring, a white-gold starburst at the impact, the grandest icon of the row. "
    "BOTTOM ROW, left to right, all in cool cyan, electric blue and violet energy: "
    "(5) a low dome of glowing hexagonal lattice over a circle on the ground, with soft mint-green motes of light rising inside it; "
    "(6) a single beam zig-zagging between three floating crystal prisms, bending at each one; "
    "(7) a round camera aperture iris of metal blades, open, pouring one long steady wide beam to the right; "
    "(8) a singing crystal core inside a wide white-gold halo ring, with a lattice of many thin beams fanning out in every direction, the grandest icon of the row."
)


def spent() -> int:
    rows = json.loads(MANIFEST.read_text(encoding="utf-8"))
    return sum((row.get("consumed_credits") if row.get("consumed_credits") is not None else TASK)
               for row in rows if row.get("batch") == BATCH)


def compose(attempt: int) -> None:
    sheet = Image.open(ROOT / "art-src" / "Generated" / BATCH / "refs" / f"sheet-{attempt}.png").convert("RGB")
    width, height = sheet.size
    cell_h, cell = height / 2, 443
    # The model seats an icon where it likes: the aperture stood against its cell's left edge and the last icon's rays ran into its neighbour's cell.
    # So each icon is found by what's lit in its row (four runs of lit columns), and cut as a square round its own middle, one size for all eight
    # so they keep one scale, on the sheet's own navy.
    lit = np.asarray(sheet).max(axis=2) > 40
    ground = sheet.getpixel((4, 4))
    found = []
    for row in range(2):
        band = lit[round(row * cell_h):round((row + 1) * cell_h)]
        on, runs, start = band.sum(axis=0) > 2, [], None
        for x in range(width + 1):
            if x < width and on[x]:
                start = x if start is None else start
            elif start is not None:
                if runs and start - runs[-1][1] < 14:
                    runs[-1] = (runs[-1][0], x)
                else:
                    runs.append((start, x))
                start = None
        runs = [run for run in runs if run[1] - run[0] > 40]
        if len(runs) != 4:
            raise SystemExit(f"SKILL_ICON_LAYOUT row {row} has {len(runs)} icons, not 4: {runs}")
        for x0, x1 in runs:
            rows = np.nonzero(band[:, x0:x1].any(axis=1))[0]
            found.append((x0, round(row * cell_h) + int(rows[0]), x1, round(row * cell_h) + int(rows[-1]) + 1))
    side = round(max(max(x1 - x0, y1 - y0) for x0, y0, x1, y1 in found) * 1.08)
    out = Image.new("RGB", (cell * 4, cell * 2), ground)
    for index, (x0, y0, x1, y1) in enumerate(found):
        cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
        tile = Image.new("RGB", (side, side), ground)
        # Only its own columns: a neighbour's glow doesn't come along.
        tile.paste(sheet.crop((x0 - 6, cy - side // 2, x1 + 6, cy - side // 2 + side)), (x0 - 6 - (cx - side // 2), 0))
        out.paste(tile.resize((cell, cell), Image.LANCZOS), ((index % 4) * cell, (index // 4) * cell))
    out.save(OUT)
    print(f"SKILL_ICONS_OK {OUT} from attempt {attempt} ({width}x{height})", flush=True)


def main() -> int:
    args = argparse.ArgumentParser(description=__doc__)
    args.add_argument("--attempt", type=int, default=1)
    args.add_argument("--compose", type=int)
    opts = args.parse_args()
    if opts.compose:
        compose(opts.compose)
        return 0
    target = ROOT / "art-src" / "Generated" / BATCH / "refs" / f"sheet-{opts.attempt}.png"
    if target.exists():
        print(f"SKILL_ICON_SKIP sheet-{opts.attempt}", flush=True)
    else:
        used = spent()
        if used + TASK > CAP:
            print(f"SKILL_ICON_BUDGET_STOP spent={used} cap={CAP}", flush=True)
            return 2
        cmd = [sys.executable, str(ROOT / "tools/meshy/meshy.py"), "image2image", "--prompt", PROMPT,
               "--name", f"sheet-{opts.attempt}", "--batch", BATCH, "--prompt-id", "S3", "--aspect-ratio", "16:9",
               "--reference", str(REF)]
        print("SKILL_ICON_RUN", f"sheet-{opts.attempt}", f"spent_before={used}", flush=True)
        subprocess.run(cmd, check=True, cwd=ROOT)
    compose(opts.attempt)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
