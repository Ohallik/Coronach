"""Verify bounded native-trace coverage against every unchanged replay CSV row.

This accepts diagnostic alignment only, never clean timing or presentation.
"""
import argparse
import csv
import json
import math
from pathlib import Path


def check(root):
    root = Path(root)
    read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
    index = read(root / 'trace-index.json')
    with (root / 'frames.csv').open(newline='', encoding='utf-8-sig') as stream:
        rows = list(csv.DictReader(stream))
    errors, matched, boundaries, hitches = [], {}, [], []
    if not rows:
        errors.append('empty replay')
    limit = index.get('frameLimit', 0)
    if not isinstance(limit, int) or not 120 <= limit <= 1800:
        errors.append('invalid segment bound')
    if index.get('cleanTimingEligible') is not False:
        errors.append('diagnostic exclusion missing')
    next_sample = 0
    for segment in index.get('segments', []):
        name = segment['file']
        if Path(name).name != name or '/' in name or '\\' in name:
            errors.append('nonlocal trace path')
            continue
        first, last = segment['firstSample'], segment['lastSample']
        if first != next_sample or last < first or last - first + 1 > limit:
            errors.append('noncontiguous or oversized segment: ' + name)
        next_sample = last + 1
        boundaries.append(first)
        if not (root / name).is_file() or (root / name).stat().st_size == 0:
            errors.append('raw trace missing: ' + name)
        report = read(root / (name + '.summary.json'))
        if not report.get('valid') or report.get('missing'):
            errors.append('native import rejected: ' + name)
        if report.get('source') != name or report.get('first') != first or report.get('last') != last:
            errors.append('native report identity mismatch: ' + name)
        seen = set()
        for frame in report['frames']:
            sample = frame['sample']
            if sample == -1:  # Explicit non-replay native boundary/flush frame.
                continue
            if not isinstance(sample, int) or not first <= sample <= last or sample >= len(rows):
                errors.append('sample outside declared segment: ' + str(sample))
                continue
            if sample in matched:
                errors.append('duplicate sample: ' + str(sample))
            matched[sample] = dict(trace=name, **frame)
            seen.add(sample)
            row = rows[sample]
            elapsed = float(row['elapsed'])
            if (not math.isfinite(elapsed) or not math.isfinite(frame['elapsed']) or
                    int(row['step']) != frame['step'] or abs(elapsed - frame['elapsed']) > .000001):
                errors.append('replay metadata mismatch: ' + str(sample))
            if not math.isfinite(frame['markerMs']) or frame['markerMs'] <= 0:
                errors.append('sample marker absent: ' + str(sample))
        if seen != set(range(first, last + 1)):
            errors.append('segment sample coverage incomplete: ' + name)
    if next_sample != len(rows) or set(matched) != set(range(len(rows))):
        errors.append('replay coverage incomplete')
    for n, row in enumerate(rows):
        if float(row['ms']) > 25:
            hitches.append(dict(sample=n, elapsed=float(row['elapsed']), ms=float(row['ms']), step=int(row['step']),
                rotationBoundary=n in boundaries or n-1 in boundaries,
                nativeFrames=[matched[k] for k in (n-1, n) if k in matched]))
    return dict(valid=not errors, failures=errors, samples=len(rows), matched=len(matched),
                segments=len(index.get('segments', [])), boundaries=boundaries, hitches=hitches,
                acceptance='DIAGNOSTIC ONLY; inclusive costs overlap; all replay rows retained')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('folder', type=Path)
    args = parser.parse_args()
    result = check(args.folder)
    output = args.folder / 'trace-analysis.json'
    if output.exists():
        raise SystemExit('Preserve existing trace-analysis.json')
    output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k != 'hitches'}, indent=2))
    raise SystemExit(0 if result['valid'] else 1)
