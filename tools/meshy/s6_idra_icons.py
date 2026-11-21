#!/usr/bin/env python3
"""S6a skill icons: Idra Sorn's eight skills, her EXTRA move and three spare icons for skills at Sync 16, 20 and 24,
as ONE generated image (image2image costs 12 credits a task), styled from the shipped skill-icons sheet: a 4x3 grid,
cut into its cells and composed into two sheets with the layout the others have (4x2 cells of 443 px):

    skill-icons-4.png   Breach, Tow Line, Shelter, Slag Hammer / Rescue Flare, Counterweight, Avalanche, Hold the Line
    skill-icons-5.png   Last One Out (EXTRA), then the three spares / an empty row

    python tools/meshy/s6_idra_icons.py                # attempt 1: one image task, then compose
    python tools/meshy/s6_idra_icons.py --attempt 2    # the one retry, if the first sheet came back poor
    python tools/meshy/s6_idra_icons.py --compose 1    # compose again from an attempt already downloaded (no credits)

Hard cap: 30 credits for the batch, counted from the manifest, so a third task is refused."""

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
BATCH = "S6-IdraIcons"
OUT = ROOT / "Lattice/Assets/_Project/Resources/UI/Generated"
MANIFEST = ROOT / "docs/art/gen-manifest.json"
CAP, TASK = 30, 12
COLUMNS, ROWS, CELL = 4, 3, 443
PROMPT = (
    "A sprite sheet of twelve game skill icons in a strict grid of 4 columns and 3 rows. Each icon sits alone in the "
    "middle of its own equal cell, fills about 60 percent of the cell, and is separated from its neighbours by wide "
    "empty gutters; no icon touches another icon or the edge of the image. Match the reference sheet's style exactly: "
    "painted glowing sci-fi energy shapes with dark gunmetal accents and small floating shards, on one flat solid very "
    "dark navy background (#0B1220) across the whole image. No text, no letters, no numbers, no borders, no frames, no "
    "grid lines, no cell outlines. Every icon is in deep ember orange, red-hot iron and furnace glow with white-hot "
    "cores: a heavy rescue worker's tools, hotter and redder than gold. "
    "TOP ROW, left to right: "
    "(1) a wedge-shaped ram head bursting through a steel bulkhead door, the plates torn outward in shards; "
    "(2) a harpoon head with a hooked claw flying to the right, trailing a taut glowing cable in one long curve; "
    "(3) a smooth half-dome of glowing light standing on the ground, with three small bolts shattering against its outside; "
    "(4) one massive sledgehammer coming straight down, its head white-hot and dripping molten slag onto cracked glowing ground. "
    "MIDDLE ROW, left to right: "
    "(5) a signal flare rising in an arc and bursting into a bright ember star with a soft mint-green glow at its centre; "
    "(6) a round shield boss seen from the front with three shock rings expanding out from it; "
    "(7) three hammer impacts in a diagonal row, each bigger than the last, with rocks falling between them; "
    "(8) a tall tower shield planted upright in the ground on a glowing line, with four arrowheads from both sides all bending toward it. "
    "BOTTOM ROW, left to right: "
    "(9) a great open blast door with white-gold light pouring out of it inside a wide golden halo ring, a ram prow in silhouette at its centre, the grandest icon of the sheet; "
    "(10) a large shield standing in front of a small shield, with one incoming bolt bending away from the small one into the large one; "
    "(11) a cutting torch: a short thick blade of white-hot plasma slicing a steel plate in two, the cut edge molten; "
    "(12) a heavy bulkhead wall segment of hard light slammed down across the ground, with hazard chevrons on its face."
)


def spent() -> int:
    rows = json.loads(MANIFEST.read_text(encoding="utf-8"))
    return sum((row.get("consumed_credits") if row.get("consumed_credits") is not None else TASK)
               for row in rows if row.get("batch") == BATCH)


def compose(attempt: int) -> None:
    sheet = Image.open(ROOT / "art-src" / "Generated" / BATCH / "refs" / f"sheet-{attempt}.png").convert("RGB")
    width, height = sheet.size
    cell_h = height / ROWS
    # The model seats an icon where it likes, so each is found by what's lit in its row (four runs of lit columns) and cut as a
    # square round its own middle, one size for all twelve so they keep one scale, on the sheet's own navy (as s3_skill_icons.py does).
    lit = np.asarray(sheet).max(axis=2) > 40
    ground = sheet.getpixel((4, 4))
    found = []
    for row in range(ROWS):
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
        if len(runs) != COLUMNS:
            raise SystemExit(f"SKILL_ICON_LAYOUT row {row} has {len(runs)} icons, not {COLUMNS}: {runs}")
        for x0, x1 in runs:
            # Its own rows too: the longest run of lit rows under those columns. The icon below can reach up into this band (a ring's top, a
            # star's ray), and first-to-last lit row took that sliver along and seated the icon off its middle.
            lit_rows, spans, top = band[:, x0:x1].any(axis=1), [], None
            for y in range(len(lit_rows) + 1):
                if y < len(lit_rows) and lit_rows[y]:
                    top = y if top is None else top
                elif top is not None:
                    if spans and top - spans[-1][1] < 14:
                        spans[-1] = (spans[-1][0], y)
                    else:
                        spans.append((top, y))
                    top = None
            y0, y1 = max(spans, key=lambda span: span[1] - span[0])
            found.append((x0, round(row * cell_h) + y0, x1, round(row * cell_h) + y1))
    side = round(max(max(x1 - x0, y1 - y0) for x0, y0, x1, y1 in found) * 1.08)
    tiles = []
    for x0, y0, x1, y1 in found:
        cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
        tile = Image.new("RGB", (side, side), ground)
        # Only its own columns and rows, and only what the image holds: a neighbour's glow doesn't come along, and nothing is cut from past the edge.
        box = (max(0, x0 - 6), max(0, y0 - 6), min(width, x1 + 6), min(height, y1 + 6))
        tile.paste(sheet.crop(box), (box[0] - (cx - side // 2), box[1] - (cy - side // 2)))
        tiles.append(tile.resize((CELL, CELL), Image.LANCZOS))
    for name, first in (("skill-icons-4.png", 0), ("skill-icons-5.png", 8)):
        out = Image.new("RGB", (CELL * 4, CELL * 2), ground)
        for index, tile in enumerate(tiles[first:first + 8]):
            out.paste(tile, ((index % 4) * CELL, (index // 4) * CELL))
        out.save(OUT / name)
    print(f"SKILL_ICONS_OK {OUT} skill-icons-4.png skill-icons-5.png from attempt {attempt} ({width}x{height})", flush=True)


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
               "--name", f"sheet-{opts.attempt}", "--batch", BATCH, "--prompt-id", "S6a", "--aspect-ratio", "4:3",
               "--reference", str(REF)]
        print("SKILL_ICON_RUN", f"sheet-{opts.attempt}", f"spent_before={used}", flush=True)
        subprocess.run(cmd, check=True, cwd=ROOT)
    compose(opts.attempt)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
