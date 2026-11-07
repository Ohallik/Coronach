"""How long the bosses really last: TEST_PLAN 4.4's 2 to 5 minutes, read from the BOSS_DURATION a chain's logs carry.

  python tools/boss_durations.py v2c              every s*/player.log under Builds/quality/QA/v2c
  python tools/boss_durations.py v2 v2b v2c       several runs
  python tools/boss_durations.py some/other/run   a folder anywhere

Prints each fight and then each boss's shortest and longest. A boss a run never met is only mentioned: a part of a chain is a fair thing to read.
Exit status: 0 with every fight inside the band, 1 with any outside it, 2 with nothing to read.
"""
import re
import sys
from pathlib import Path

QA = Path(__file__).resolve().parent.parent / "Builds" / "quality" / "QA"
LOW, HIGH = 120, 300
BOSSES = ("Burrower", "BellowsBelow", "Cantor", "HessaKorl")
LINE = re.compile(r"^BOSS_DURATION (\S+) seconds=([\d.]+)")


def logs_of(run):
    folder = Path(run)
    if not folder.is_dir():
        folder = QA / run
    return sorted(folder.glob("s*/player.log")), folder


def main(runs):
    if not runs:
        print(__doc__)
        return 2
    fights = {}
    for run in runs:
        logs, folder = logs_of(run)
        if not logs:
            print(f"{run}: no s*/player.log under {folder}")
        for log in logs:
            for line in log.read_text(encoding="utf-8", errors="ignore").splitlines():
                match = LINE.match(line)
                if match:
                    fights.setdefault(match.group(1), []).append((float(match.group(2)), f"{run}/{log.parent.name}"))
    if not fights:
        print("No BOSS_DURATION in these logs: nothing measured.")
        return 2
    outside = 0
    for boss in list(BOSSES) + sorted(set(fights) - set(BOSSES)):
        seen = fights.get(boss)
        if not seen:
            print(f"{boss:<13} no fight in these runs")
            continue
        for seconds, where in seen:
            short = seconds < LOW
            long = seconds > HIGH
            outside += short or long
            verdict = "UNDER" if short else "OVER" if long else "ok"
            print(f"{boss:<13} {seconds:6.1f} s  {verdict:<6}{where}")
        if len(seen) > 1:
            times = [seconds for seconds, _ in seen]
            print(f"{boss:<13} {min(times):6.1f} to {max(times):.1f} s over {len(times)} fights")
    print(f"{outside} fight(s) outside {LOW} to {HIGH} s" if outside else f"Every fight inside {LOW} to {HIGH} s")
    return 1 if outside else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
