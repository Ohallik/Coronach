"""Contact sheets of a capture run's per-step screenshots for MAP_EYE_TEST review (free, local).

Each tile is labelled with its step index and the route's step name, so an observation
can be traced back to where the hero was. Sheets are written beside the run.

  python tools/eye_sheet.py Builds/quality/<run> [--per-sheet 12] [--every 1]
"""
import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw

p = argparse.ArgumentParser()
p.add_argument("run")
p.add_argument("--per-sheet", type=int, default=12)
p.add_argument("--every", type=int, default=1)
p.add_argument("--out", help="write sheets here instead of beside the run")
a = p.parse_args()
run = Path(a.run)
folder = next((f for f in [run, *sorted(run.glob("player*"))] if list(f.glob("[0-9][0-9][0-9].png"))), None)
if folder is None:
    raise SystemExit(f"no step screenshots under {run}")
route = json.loads((folder / "route.json").read_text(encoding="utf-8-sig")) if (folder / "route.json").exists() else {}
names = [s.get("name", "") for s in route.get("steps", [])]
shots = sorted(folder.glob("[0-9][0-9][0-9].png"))[:: a.every]
cols, w, h = 4, 640, 360
for page in range(0, len(shots), a.per_sheet):
    batch = shots[page:page + a.per_sheet]
    rows = (len(batch) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * w, rows * h), "black")
    draw = ImageDraw.Draw(sheet)
    for i, shot in enumerate(batch):
        x, y = (i % cols) * w, (i // cols) * h
        sheet.paste(Image.open(shot).convert("RGB").resize((w, h)), (x, y))
        step = int(shot.stem)
        label = f"{step:03d} {names[step] if step < len(names) else ''}"
        draw.rectangle((x, y, x + 8 + 7 * len(label), y + 16), fill="black")
        draw.text((x + 4, y + 2), label, fill="white")
    out = (Path(a.out) if a.out else folder) / f"eye-sheet-{page // a.per_sheet + 1:02d}.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    print("EYE_SHEET", out, len(batch))
