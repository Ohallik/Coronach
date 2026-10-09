"""Validate captured damage without inventing identities for older reports.

Version 2 adds release-time projectile provenance. This checks the recorded
contract, not combat balance or the cause of an older unattributed impact.
"""
import argparse
import json
import math
from pathlib import Path


def analyze(report):
    errors = []
    version = report.get('version', 1)
    if type(version) is not int or version not in (1, 2):
        errors.append('unsupported damage report version')
    if report.get('complete') is not True:
        errors.append('damage capture is incomplete')
    if type(report.get('dropped')) is not int or report['dropped'] != 0:
        errors.append('damage events were dropped or their count is unavailable')
    hits = report.get('hits')
    if not isinstance(hits, list):
        hits = []
        errors.append('damage hit array is missing')
    capture = report.get('captureId')
    if version == 2 and (not isinstance(capture, str) or not capture.strip()):
        errors.append('projectile capture identity is missing')
    if version == 2:
        for key in ('projectileLaunches', 'projectileVolleys'):
            if type(report.get(key)) is not int or report[key] < 0:
                errors.append('invalid ' + key + ' count')
    shots, volleys = set(), {}
    projectile_hits = 0
    previous = -math.inf

    def number(value):
        try:
            return type(value) in (int, float) and math.isfinite(value)
        except OverflowError:
            return False

    def vector(value):
        return isinstance(value, dict) and all(number(value.get(k)) for k in ('x', 'y', 'z'))

    for index, hit in enumerate(hits):
        label = f'hit {index}: '
        if not isinstance(hit, dict):
            errors.append(label + 'invalid hit object')
            continue
        seconds = hit.get('seconds')
        if not number(seconds) or seconds < 0 or seconds < previous:
            errors.append(label + 'invalid or reversed impact clock')
        else:
            previous = seconds
        if type(hit.get('step')) is not int or hit['step'] < 0:
            errors.append(label + 'invalid checkpoint')
        if not vector(hit.get('position')):
            errors.append(label + 'invalid impact position')
        for key in ('requested', 'received', 'remaining'):
            if not number(hit.get(key)) or hit[key] < 0:
                errors.append(label + 'invalid ' + key)
        for key in ('scene', 'victim', 'source', 'type'):
            if not isinstance(hit.get(key), str) or not hit[key].strip():
                errors.append(label + 'missing ' + key)
        for key in ('sourceAvailable', 'sourceAlive', 'deathAttack'):
            if type(hit.get(key)) is not bool:
                errors.append(label + 'missing ' + key)
        if hit.get('sourceAlive') and not hit.get('sourceAvailable'):
            errors.append(label + 'unavailable source cannot be observed alive')
        if version != 2:
            continue
        impact_source = hit.get('impactSourceInstanceId')
        if type(impact_source) is not int or (impact_source != 0) != hit.get('sourceAvailable'):
            errors.append(label + 'source instance at impact is inconsistent')
        if type(hit.get('projectile')) is not bool:
            errors.append(label + 'projectile classification is missing')
            continue
        if not hit['projectile']:
            continue
        projectile_hits += 1
        if hit.get('provenanceAvailable') is not True or hit.get('releaseCapture') != capture:
            errors.append(label + 'release provenance is missing or belongs to another capture')
        volley, shot = hit.get('volleyId'), hit.get('shotId')
        if type(volley) is not int or volley <= 0 or type(shot) is not int or shot <= 0:
            errors.append(label + 'invalid volley or launch identity')
        elif shot in shots:
            errors.append(label + 'projectile launch identity was reused')
        else:
            shots.add(shot)
        if type(shot) is int and type(report.get('projectileLaunches')) is int and shot > report['projectileLaunches']:
            errors.append(label + 'launch identity exceeds recorded releases')
        if type(volley) is int and type(report.get('projectileVolleys')) is int and volley > report['projectileVolleys']:
            errors.append(label + 'volley identity exceeds recorded releases')
        for key in ('sourceInstanceId', 'projectileInstanceId'):
            if type(hit.get(key)) is not int or hit[key] == 0:
                errors.append(label + 'invalid ' + key)
        if not isinstance(hit.get('releaseSource'), str) or not hit['releaseSource'].strip():
            errors.append(label + 'source identity at release is missing')
        if hit.get('sourceAvailable') and (impact_source != hit.get('sourceInstanceId') or hit.get('source') != hit.get('releaseSource')):
            errors.append(label + 'available impact source differs from release source')
        released = hit.get('releasedAt')
        if not number(released) or released < 0 or not number(seconds) or released > seconds:
            errors.append(label + 'release clock is invalid or later than impact')
        if not vector(hit.get('releasePosition')) or not vector(hit.get('releaseVelocity')):
            errors.append(label + 'invalid release geometry')
        elif not any(hit['releaseVelocity'][k] != 0 for k in ('x', 'y', 'z')):
            errors.append(label + 'projectile has no release velocity')
        if type(volley) is int and volley > 0:
            origin = (hit.get('sourceInstanceId'), hit.get('releaseSource'))
            if volley in volleys and volleys[volley] != origin:
                errors.append(label + 'one volley names different source instances')
            volleys[volley] = origin
    return dict(valid=not errors, failures=errors, version=version, hits=len(hits),
                projectileHits=projectile_hits if version == 2 else None,
                distinctLaunches=len(shots) if version == 2 else None,
                distinctVolleys=len(volleys) if version == 2 else None,
                projectileIdentity='UNAVAILABLE in legacy report' if version == 1 else
                'REJECTED' if errors else 'recorded release identities',
                scope='Recorded damage contract only; no balance, visual or historical-cause acceptance.')


def include(result, folder, declared_version=0):
    path = Path(folder) / 'damage.json'
    supported = type(declared_version) is int and declared_version in (0, 1, 2)
    if not path.exists() and supported and declared_version == 0:
        return  # Legacy evidence never promised a sidecar.
    try:
        report = json.loads(path.read_text(encoding='utf-8-sig'))
        if not isinstance(report, dict):
            raise ValueError('report must be an object')
        damage = analyze(report)
        if not supported or declared_version and report.get('version', 1) != declared_version:
            damage['failures'].append('declared damage version differs from the recorded report')
            damage['valid'] = False
    except (OSError, ValueError, TypeError, OverflowError) as error:
        damage = dict(valid=False, failures=['damage report unavailable or malformed: ' + str(error)])
    result['damageEvidence'] = damage
    result['failures'].extend(damage['failures'])
    result['valid'] = not result['failures']


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('report', type=Path)
    args = parser.parse_args()
    result = analyze(json.loads(args.report.read_text(encoding='utf-8-sig')))
    print(json.dumps(result, indent=2, allow_nan=False))
    raise SystemExit(0 if result['valid'] else 1)
