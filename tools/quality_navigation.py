"""Describe recorded navigation shortfalls without changing replay acceptance.

Distances use the same ground-plane coordinates as the existing replay checker.
Recorded endpoints and discontinuities remain explicit. An extrapolated travel
time is only a diagnostic estimate, never proof of a collision or motor fault.
"""
import argparse
import csv
import hashlib
import json
import math
from pathlib import Path


def finite(value, field):
    number = float(value)
    if not math.isfinite(number):
        raise ValueError(f'nonfinite {field}')
    return number


def describe_navigation(run, frames, route):
    if not frames:
        raise ValueError('empty trace')
    parsed = []
    for row in frames:
        index = int(row['step'])
        if not 0 <= index < len(route['steps']):
            raise ValueError('step outside route')
        for flag in ('focus', 'paused', 'blocked'):
            if row[flag] not in ('True', 'False'):
                raise ValueError(f'unknown {flag}')
        parsed.append(dict(row, step=index, elapsed=finite(row['elapsed'], 'elapsed'),
                           x=finite(row['x'], 'x'), z=finite(row['z'], 'z'),
                           ms=finite(row['ms'], 'ms'),
                           reportedSpeed=finite(row['reportedSpeed'], 'reportedSpeed')))
    if any(b['elapsed'] <= a['elapsed'] for a, b in zip(parsed, parsed[1:])):
        raise ValueError('nonmonotonic trace')
    if any(row['ms'] <= 0 for row in parsed):
        raise ValueError('nonpositive frame interval')
    records = []
    first_loss = next((row['elapsed'] for row in parsed if row['focus'] != 'True'), None)
    for index, step in enumerate(route['steps']):
        if not step.get('navigate'):
            continue
        rows = [row for row in parsed if row['step'] == index]
        record = dict(step=index, name=step['name'], authoredSeconds=step['seconds'],
                      authoredInputs={key: step[key] for key in ('leftTrigger', 'rightTrigger', 'buttons', 'stopWhen', 'until') if key in step},
                      samples=len(rows), runtimeFailures=[f for f in run.get('failures', []) if f.startswith(step['name'] + ':')])
        records.append(record)
        if not rows:
            record['observation'] = 'UNOBSERVED'
            continue
        discontinuities = sum((a['scene'], a['hero']) != (b['scene'], b['hero']) for a, b in zip(rows, rows[1:]))
        path = sum(math.hypot(b['x'] - a['x'], b['z'] - a['z']) for a, b in zip(rows, rows[1:])
                   if (a['scene'], a['hero']) == (b['scene'], b['hero']))
        span = rows[-1]['elapsed'] - rows[0]['elapsed']
        record.update(observation='RECORDED', firstSeconds=rows[0]['elapsed'], lastSeconds=rows[-1]['elapsed'],
                      observedSpanSeconds=span, sampledPathMetres=path, discontinuities=discontinuities,
                      focusedSamples=sum(row['focus'] == 'True' for row in rows),
                      pausedSamples=sum(row['paused'] == 'True' for row in rows),
                      blockedSamples=sum(row['blocked'] == 'True' for row in rows),
                      maximumFrameMs=max(row['ms'] for row in rows),
                      firstPosition=dict(x=rows[0]['x'], z=rows[0]['z']),
                      lastPosition=dict(x=rows[-1]['x'], z=rows[-1]['z']))
        if step.get('approachPartner') or step.get('approachTarget'):
            record['goal'] = 'MOVING_TARGET_NOT_RECONSTRUCTED'
            continue
        target = (finite(step['point']['x'], 'target x'), finite(step['point']['z'], 'target z'))
        expected_scene = step.get('expectedScene') or route['scene']
        same_scene = all(row['scene'] == expected_scene for row in rows)
        record['target'] = dict(x=target[0], z=target[1], scene=expected_scene)
        if not same_scene or discontinuities:
            record['goal'] = 'DISCONTINUOUS_COORDINATES'
            continue
        distances = [math.dist((row['x'], row['z']), target) for row in rows]
        radius = finite(step.get('tolerance', .65), 'tolerance')
        if radius < 0:
            raise ValueError('negative tolerance')
        # No +0.1 analyzer margin is added here: this is the authored radius,
        # and this report deliberately does not return an acceptance verdict.
        speed = path / span if span > 0 else 0
        record.update(goal='FIXED_POINT', authoredRadiusMetres=radius,
                      firstDistanceMetres=distances[0], lastDistanceMetres=distances[-1],
                      closestDistanceMetres=min(distances), sampledMeanTravelSpeed=speed)
        if span > 0 and speed > .01 and record['focusedSamples'] == len(rows) and not record['pausedSamples'] and not record['blockedSamples']:
            record['estimatedExtraSecondsAtObservedSpeed'] = max(0, distances[-1] - radius) / speed
        else:
            record['estimatedExtraSecondsAtObservedSpeed'] = None
    return dict(scope='Diagnostic only; no replay acceptance or causal attribution. Each step span excludes the unsampled boundary before its first row. Inputs are authored requests, not observed hardware state.',
                runtimeValid=run.get('valid'), runtimeFailures=list(run.get('failures', [])),
                declaredSamples=run.get('samples'), observedSamples=len(parsed),
                firstFocusLossSeconds=first_loss, navigation=records)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('folder', type=Path)
    parser.add_argument('--output', type=Path, required=True, help='New output file; existing evidence is never overwritten')
    args = parser.parse_args()
    if args.output.exists():
        parser.error('preserve previous output; choose a new file')
    paths = {name: args.folder / name for name in ('run.json', 'route.json', 'frames.csv')}
    run = json.loads(paths['run.json'].read_text(encoding='utf-8-sig'))
    route = json.loads(paths['route.json'].read_text(encoding='utf-8-sig'))
    with paths['frames.csv'].open(encoding='utf-8-sig', newline='') as stream:
        frames = list(csv.DictReader(stream))
    result = describe_navigation(run, frames, route)
    result['inputs'] = [dict(path=str(path), sha256=hashlib.sha256(path.read_bytes()).hexdigest()) for path in paths.values()]
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, allow_nan=False)
        stream.write('\n')
    print(f'{len(result["navigation"])} navigation steps described; diagnostic only, runtime valid={result["runtimeValid"]}')


if __name__ == '__main__':
    main()
