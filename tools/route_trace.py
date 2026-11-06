"""Per-step trace of a replay run: time, zone, lead, position, integrity, partner, UI at the step's end.

Shows where a route lost the game, and is the raw material of the pacing log.
  python tools/route_trace.py Builds/quality/<run> [--from N] [--to N]
"""
import argparse
import csv
import json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument("run")
p.add_argument("--from", dest="first", type=int, default=0)
p.add_argument("--to", dest="last", type=int, default=10 ** 6)
a = p.parse_args()
run = Path(a.run)
route = json.loads((run / "route.json").read_text(encoding="utf-8-sig"))
rows = list(csv.DictReader((run / "frames.csv").open(encoding="utf-8-sig")))
by_step = {}
for row in rows:
    by_step.setdefault(int(row["step"]), []).append(row)
for index, step in enumerate(route["steps"]):
    if index < a.first or index > a.last or index not in by_step:
        continue
    frames = by_step[index]
    head, tail = frames[0], frames[-1]
    target = ""
    if step.get("navigate") and not step.get("approachTarget") and not step.get("approachPartner"):
        point = step["point"]
        gap = ((float(tail["x"]) - point["x"]) ** 2 + (float(tail["z"]) - point["z"]) ** 2) ** 0.5
        target = f" goal=({point['x']},{point['z']}) gap={gap:.1f}"
    low = min(float(f["integrity"]) for f in frames)
    partner = min(float(f["partnerHealth"]) for f in frames)
    states = sorted({f["state"] for f in frames})
    print(
        f"{index:3} t={float(head['elapsed']):7.1f}+{float(tail['elapsed']) - float(head['elapsed']):5.1f} "
        f"{tail['scene']:<14} {tail['hero']:<5} {tail['form']:<10} pos=({float(tail['x']):6.1f},{float(tail['y']):5.1f},{float(tail['z']):6.1f}) "
        f"hp={float(tail['integrity']):5.0f} low={low:4.0f} partner={float(tail['partnerHealth']):4.0f} low={partner:4.0f} ui={tail['ui']:<8} "
        f"{step['name'][:48]}{target} [{','.join(states)[:30]}]"
    )
