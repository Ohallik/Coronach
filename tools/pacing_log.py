"""Pacing log across a chain of replay runs: when each level-up, skill, zone, boss, quest and story beat arrives.

Times are cumulative route seconds, so gaps show the longest stretch with nothing new (TEST_PLAN 4.3).
  python tools/pacing_log.py Builds/quality/<run> Builds/quality/<run> ...
"""
import csv
import re
import sys
from pathlib import Path

BEATS = re.compile(r"^(LEVEL_UP|SKILL_LEARNED|ZONE_ENTER|BOSS_DOWN|BOSS_DURATION|QUEST_\w+|DISCOVERY|PARTY_JOIN|SLICE_COMPLETE|MASTERY\w*)\b(.*)")
offset = 0.0
beats = []
for folder in sys.argv[1:]:
    run = Path(folder)
    rows = list(csv.DictReader((run / "frames.csv").open(encoding="utf-8-sig")))
    steps = {}
    for row in rows:
        steps.setdefault(int(row["step"]), float(row["elapsed"]))
    current = 0.0
    for line in (run / "player.log").read_text(encoding="utf-8", errors="ignore").splitlines():
        checkpoint = re.match(r"QUALITY_CHECKPOINT_BEGIN (\d+)", line)
        if checkpoint:
            current = steps.get(int(checkpoint.group(1)), current)
            continue
        match = BEATS.match(line)
        if match:
            beats.append((offset + current, run.name, match.group(1), match.group(2).strip()))
    offset += float(rows[-1]["elapsed"])
last_new = 0.0
widest = (0.0, "")
previous_zone = None
for time, run, kind, detail in beats:
    if kind == "ZONE_ENTER" and detail == previous_zone:
        continue
    if kind == "ZONE_ENTER":
        previous_zone = detail
    print(f"{int(time // 60):3}:{int(time % 60):02} {run:<6} {kind:<14} {detail[:70]}")
    if kind in ("LEVEL_UP", "ZONE_ENTER", "BOSS_DOWN", "QUEST_START", "QUEST_DONE", "DISCOVERY", "PARTY_JOIN"):
        if time - last_new > widest[0]:
            widest = (time - last_new, f"before {kind} {detail[:40]} at {int(time // 60)}:{int(time % 60):02}")
        last_new = time
print(f"TOTAL {int(offset // 60)}:{int(offset % 60):02}  widest gap between beats {widest[0]:.0f} s {widest[1]}")
