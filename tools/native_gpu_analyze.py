"""Verify a diagnostic D3D12 camera envelope, never clean GPU headroom."""
from pathlib import Path
import argparse
import csv
import json
import math

TAG_PREFIX = 0xC0A0CAFE00000000
UINT64_MAX = (1 << 64) - 1


def analyze(identities, results, frames, run, report):
    errors = []
    if report.get('schema') != 2 or report.get('fenceKind') != 'private_queue_fence' or report.get('complete') is not True or report.get('failure') not in ('', None):
        errors.append('native producer did not report complete successful evidence')
    if not identities or len(identities) != len(frames) or run.get('samples') != len(frames):
        errors.append('native identity coverage does not match every replay sample')
    if len(results) != len(identities) or report.get('submitted') != len(identities) or report.get('received') != len(results):
        errors.append('native submitted/results coverage mismatch')
    if run.get('valid') is not True or any(row.get('focus') != 'True' for row in frames):
        errors.append('underlying replay rejected or focus unavailable')
    expected = {}
    previous_frame = -1
    for index, identity in enumerate(identities):
        try:
            sample, frame, step = (int(identity[k]) for k in ('sample', 'frame', 'step'))
            elapsed = float(identity['elapsed'])
            if sample != index or frame <= previous_frame or frame in expected or not 0 < frame <= 0x7fffffff:
                errors.append(f'identity {index}: duplicate, unordered or mismatched sample/frame')
            if not math.isfinite(elapsed) or index >= len(frames) or step != int(frames[index]['step']) or abs(elapsed-float(frames[index]['elapsed'])) > .000005:
                errors.append(f'identity {index}: step/time does not match its replay sample')
            expected[frame] = identity
            previous_frame = frame
        except (KeyError, ValueError, TypeError, OverflowError):
            errors.append(f'identity {index}: malformed values')
    seen = set()
    spans = []
    for index, row in enumerate(results):
        try:
            frame, status = int(row['frame']), int(row['status'])
            begin, end, frequency, required, complete, tag = (int(row[k]) for k in
                ('begin', 'end', 'frequency', 'fenceRequired', 'fenceCompleted', 'tag'))
            cpu_begin, cpu_end = int(row['beginCpuQpc']), int(row['endCpuQpc'])
            if frame in seen or frame not in expected:
                errors.append(f'result {index}: duplicate or unscheduled frame {frame}')
            seen.add(frame)
            if status != 0:
                errors.append(f'frame {frame}: native status {status}')
            if not all(0 < value <= UINT64_MAX for value in (begin, end, frequency, required, complete, tag)):
                errors.append(f'frame {frame}: missing or out-of-range native clock/fence')
            if tag != TAG_PREFIX | frame:
                errors.append(f'frame {frame}: GPU-written identity tag mismatch')
            if complete < required:
                errors.append(f'frame {frame}: result read before its fence completed')
            if cpu_begin <= 0 or cpu_end < cpu_begin:
                errors.append(f'frame {frame}: invalid callback QPC interval')
            if frequency > 0:
                # Subtract integers before float conversion: these endpoints
                # already exceed the exact-integer range of a double.
                span = (end - begin) * 1000 / frequency
                spans.append(span)
                if not 0 < span <= 1000:
                    errors.append(f'frame {frame}: invalid camera envelope {span} ms')
        except (KeyError, ValueError, TypeError, OverflowError):
            errors.append(f'result {index}: malformed values')
    missing = sorted(set(expected)-seen)
    if missing:
        errors.append(f'missing native results for {len(missing)} frames; see missingFrames')
    ordered = sorted(spans)
    def percentile(fraction):
        return ordered[min(len(ordered)-1, int((len(ordered)-1)*fraction))] if ordered else None
    return dict(valid=not errors, failures=errors, replaySamples=len(frames), identities=len(identities),
                results=len(results), missingFrames=missing,
                nativeCameraEnvelopeMs=dict(minimum=min(spans) if spans else None, median=percentile(.5),
                    p95=percentile(.95), p99=percentile(.99), maximum=max(spans) if spans else None),
                scope='Main-camera bottom-of-pipe envelope; includes CPU submission gaps, excludes later overlays/present. Diagnostic only; no clean GPU headroom or physical acceptance.',
                alignment='Explicit sample/frame/step/time identity plus GPU-written frame tag and completed fence; no row-offset alignment to Unity timing counters.')


def read(folder):
    def document(name):return json.loads((folder/name).read_text(encoding='utf-8-sig'))
    def table(name):
        with (folder/name).open(encoding='utf-8-sig', newline='') as stream:return list(csv.DictReader(stream))
    try:
        result = analyze(table('native-gpu-frames.csv'), table('native-gpu-results.csv'), table('frames.csv'), document('run.json'), document('native-gpu.json'))
        build = document('build.json')
        if build.get('nativeGpuDiagnostic') is not True or '-quality-native-gpu' not in build.get('arguments', []):
            result['failures'].append('native diagnostic launch identity missing')
            result['valid'] = False
        return result
    except (OSError, ValueError, TypeError, KeyError, AttributeError) as error:
        return dict(valid=False, failures=['native GPU evidence unavailable or malformed: '+str(error)])


def include(result, folder):
    build_path = folder/'build.json'
    requested = (folder/'native-gpu.json').exists()
    if build_path.exists():
        build = json.loads(build_path.read_text(encoding='utf-8-sig'))
        arguments = build.get('arguments', [])
        requested |= build.get('nativeGpuDiagnostic') is True or (isinstance(arguments, list) and '-quality-native-gpu' in arguments)
    if requested:
        native = read(folder)
        result['nativeGpuDiagnostic'] = native
        result['failures'].extend(native['failures'])
        result['valid'] = not result['failures']


if __name__ == '__main__':
    parser=argparse.ArgumentParser();parser.add_argument('folder',type=Path);parser.add_argument('--output',type=Path)
    args=parser.parse_args();report=read(args.folder);output=args.output or args.folder/'native-gpu-analysis.json'
    if output.exists():raise SystemExit('Preserve previous native analysis; choose another --output')
    output.write_text(json.dumps(report,indent=2,allow_nan=False)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2));raise SystemExit(0 if report['valid'] else 1)
