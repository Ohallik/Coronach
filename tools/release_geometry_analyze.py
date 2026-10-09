"""Check evaluated release evidence and distinguish world clearance from projection.

World AABBs, hull circles and projected local-bound rectangles are conservative.
Their overlap is a diagnostic candidate, never proof of a mesh intersection or
visual acceptance. No frames are filtered to improve a quality result.
"""
import csv
import json
import math
from pathlib import Path


def number(value):
    if type(value) not in (int, float) or not math.isfinite(value):
        raise ValueError('nonfinite or missing number')
    return value


def integer(value, minimum=0):
    if type(value) is not int or value < minimum:
        raise ValueError('invalid integer')
    return value


def vector(value, axes='xyz'):
    return tuple(number(value[k]) for k in axes)


def quaternion(value):
    result = vector(value, 'xyzw')
    if abs(sum(v*v for v in result)-1) > .005:
        raise ValueError('invalid rotation')
    return result


def close(a, b, tolerance=.001):
    if len(a) != len(b) or any(abs(x-y) > tolerance for x, y in zip(a, b)):
        raise ValueError('evaluated geometry differs from independent projection/bounds')


def shape(record, matrix, camera, rotation):
    if record.get('available') is not True or integer(record['meshes'], 1)*8 != len(record['corners']):
        raise ValueError('unavailable mesh or missing bound corners')
    if type(record['instance']) is not int or record['instance'] == 0:
        raise ValueError('missing instance')
    vector(record['position']); vector(record['velocity']); quaternion(record['rotation'])
    corners = [vector(c) for c in record['corners']]
    lower, upper = vector(record['minimum']), vector(record['maximum'])
    if any(b <= a for a, b in zip(lower, upper)):
        raise ValueError('degenerate world bounds')
    close(lower, tuple(min(p[k] for p in corners) for k in range(3)))
    close(upper, tuple(max(p[k] for p in corners) for k in range(3)))
    x, y, z, w = rotation
    forward = (2*(x*z+y*w), 2*(y*z-x*w), 1-2*(x*x+y*y))
    projected = []
    for point in corners:
        p = (*point, 1)
        clip = [sum(matrix[r*4+c]*p[c] for c in range(4)) for r in range(4)]
        if abs(clip[3]) < 1e-8:
            raise ValueError('projection at zero depth')
        projected.append((.5+.5*clip[0]/clip[3], .5+.5*clip[1]/clip[3],
                          sum((point[k]-camera[k])*forward[k] for k in range(3))))
    close(vector(record['viewportMinimum']), tuple(min(p[k] for p in projected) for k in range(3)), .002)
    close(vector(record['viewportMaximum']), tuple(max(p[k] for p in projected) for k in range(3)), .002)


def planar_clearance(hero, body):
    p = vector(hero['position']); lo = vector(body['minimum']); hi = vector(body['maximum'])
    return math.hypot(*(max(lo[k]-p[k], 0, p[k]-hi[k]) for k in (0, 2))) - number(hero['hullRadius'])


def rectangle_gap(a, b):
    al, ah = vector(a['viewportMinimum']), vector(a['viewportMaximum'])
    bl, bh = vector(b['viewportMinimum']), vector(b['viewportMaximum'])
    gap = [max(al[k]-bh[k], bl[k]-ah[k]) for k in (0, 1)]
    return math.hypot(*(max(0, v) for v in gap)) if max(gap) > 0 else max(gap)


def analyze(rows, frames, run, report):
    errors, spans, groups, samples, observations = [], {}, {}, {}, []
    try:
        if type(report['schema']) is not int or report['schema'] != 1 or report['complete'] is not True or report.get('failure') not in ('', None):
            raise ValueError('producer is incomplete or unsupported')
        if not frames or report['samples'] != len(frames) or run['samples'] != len(frames):
            raise ValueError('replay sample coverage mismatch')
        if integer(report['frames']) != len(rows) or integer(report['dropped']) != 0 or not 1 <= integer(report['frameLimit']) <= 4096 or len(rows) > report['frameLimit']:
            raise ValueError('missing or dropped release frames')
        if type(report['sourceLimit']) is not int or report['sourceLimit'] != 16 or len(report['spans']) > 16:
            raise ValueError('source bound mismatch')
        if run.get('valid') is not True:
            raise ValueError('replay rejected')
        for span in report['spans']:
            episode = integer(span['episode'], 1)
            if episode in spans or span['end'] not in ('removed', 'recovered'):
                raise ValueError('duplicate or unfinished release span')
            spans[episode] = span
    except (KeyError, ValueError, TypeError, OverflowError) as error:
        errors.append('report: '+str(error))
    previous_sample = -1
    for index, row in enumerate(rows):
        try:
            sample = integer(row['sample']); frame = integer(row['frame'], 1); episode = integer(row['episode'], 1)
            if not 0 <= sample < len(frames) or sample < previous_sample:
                raise ValueError('reversed or out-of-range replay sample')
            previous_sample = sample
            f = frames[sample]
            if f['focus'] != 'True' or type(row['paused']) is not bool or row['paused'] != (f['paused'] == 'True'):
                raise ValueError('focus/pause mismatch')
            elapsed = number(row['elapsed'])
            if elapsed < 0 or abs(elapsed-float(f['elapsed'])) > .000005 or integer(row['step']) != int(f['step']):
                raise ValueError('replay time/step mismatch')
            if sample in samples and samples[sample] != frame:
                raise ValueError('different evaluated frames for one replay sample')
            samples[sample] = frame
            span = spans[episode]
            if type(row['source']) is not int or row['source'] == 0 or row['source'] != span['source']:
                raise ValueError('source identity mismatch')
            number(row['gameTime']); age = number(row['releaseAge'])
            if not 0 <= age <= 3.7:
                raise ValueError('invalid departure age')
            prior = groups.setdefault(episode, [])
            if prior:
                old = prior[-1]
                if sample != old['sample']+1 or frame != old['frame']+1 or elapsed <= old['elapsed'] or age < old['releaseAge']:
                    raise ValueError('missing, duplicated or reversed release frame')
                if row['paused'] and old['paused'] and (age != old['releaseAge'] or row['sourcePosition'] != old['sourcePosition'] or row['anatomy'] != old['anatomy']):
                    raise ValueError('paused departure geometry moved')
            prior.append(row)
            vector(row['sourcePosition']); quaternion(row['sourceRotation'])
            camera = vector(row['cameraPosition']); rotation = quaternion(row['cameraRotation'])
            close(camera, tuple(float(f['camera'+a]) for a in 'XYZ'), .00002)
            matrix = [number(v) for v in row['viewProjection']]
            if len(matrix) != 16 or max(map(abs, matrix)) < .01:
                raise ValueError('camera matrix unavailable')
            anatomy, heroes = row['anatomy'], row['heroes']
            if len(anatomy) != 8 or [a['role'] for a in anatomy] != ['head']+['part'+str(i) for i in range(1, 8)]:
                raise ValueError('missing or reordered anatomy')
            if len(heroes) != 2 or [h['role'] for h in heroes] != ['active', 'partner'] or heroes[0]['hero'] != f['hero'] or heroes[0]['hero'] == heroes[1]['hero'] or not heroes[1]['hero']:
                raise ValueError('missing or mismatched party identity')
            if len({a['instance'] for a in anatomy+heroes}) != 10:
                raise ValueError('duplicated mesh root')
            for item in anatomy+heroes:
                shape(item, matrix, camera, rotation)
            close(vector(heroes[0]['position']), tuple(float(f[a]) for a in 'xyz'), .00002)
            for hero in heroes:
                if number(hero['hullRadius']) <= 0:
                    raise ValueError('hull radius missing')
                clearances = [planar_clearance(hero, body) for body in anatomy]
                gaps = [rectangle_gap(hero, body) for body in anatomy]
                observations.append(dict(sample=sample,elapsed=elapsed,releaseAge=age,episode=episode,hero=hero['hero'],
                    minimumPlanarHullClearance=min(clearances),minimumProjectedBoundsGap=min(gaps),
                    nearestBody=clearances.index(min(clearances)),overlappingProjectedParts=[i for i,gap in enumerate(gaps) if gap <= 0]))
        except (KeyError, IndexError, ValueError, TypeError, OverflowError) as error:
            errors.append(f'row {index}: {error}')
    for episode, span in spans.items():
        group = groups.get(episode, [])
        if not group or span.get('frames') != len(group) or span.get('firstSample') != group[0]['sample'] or span.get('lastSample') != group[-1]['sample']:
            errors.append(f'episode {episode}: span coverage mismatch')
    summary = {}
    for hero in {row['hero'] for row in observations}:
        values = [row for row in observations if row['hero'] == hero]
        summary[hero] = dict(samples=len(values),minimumPlanarHullClearance=min(v['minimumPlanarHullClearance'] for v in values),
            projectedBoundsOverlapSamples=sum(bool(v['overlappingProjectedParts']) for v in values))
    return dict(valid=not errors,failures=errors,frames=len(rows),episodes=len(spans),heroes=summary,
                observations=observations,scope=__doc__.strip())


def include(result, folder, declared_version=0, frames=None, run=None):
    folder = Path(folder); path = folder/'release-geometry.json'
    supported = type(declared_version) is int and declared_version in (0, 1)
    if declared_version == 0 and supported and not path.exists():
        return
    try:
        if not supported:
            raise ValueError('unsupported declared release geometry version')
        report = json.loads(path.read_text(encoding='utf-8-sig'))
        with (folder/'release-geometry.jsonl').open(encoding='utf-8-sig') as stream:
            rows = [json.loads(line) for line in stream]
        if frames is None:
            with (folder/'frames.csv').open(encoding='utf-8-sig',newline='') as stream:
                frames = list(csv.DictReader(stream))
        if run is None:
            run = json.loads((folder/'run.json').read_text(encoding='utf-8-sig'))
        evidence = analyze(rows,frames,run,report)
    except (OSError, ValueError, TypeError, KeyError, OverflowError) as error:
        evidence = dict(valid=False,failures=['release geometry unavailable or malformed: '+str(error)])
    result['releaseGeometry'] = evidence
    result['failures'].extend(evidence['failures']); result['valid'] = not result['failures']
