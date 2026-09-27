"""Measure captured audio without modifying it; listening remains a separate gate."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

import imageio_ffmpeg


def fingerprint(path: Path) -> dict:
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return {'path': path.as_posix(), 'bytes': path.stat().st_size, 'sha256': digest.hexdigest()}


def read_summary(text: str) -> dict:
    summary = text[text.rfind('Summary:'):]
    peak = re.search(r'True peak:\s+Peak:\s+(-?inf|[-+\d.]+)\s+dBFS', summary)
    loudness = re.search(r'Integrated loudness:\s+I:\s+(-?inf|[-+\d.]+)\s+LUFS', summary)
    if peak is None or loudness is None:
        raise ValueError('FFmpeg did not provide a complete true-peak/loudness summary')
    peak_value, loudness_value = float(peak[1]), float(loudness[1])
    if math.isnan(peak_value) or math.isnan(loudness_value):
        raise ValueError('FFmpeg returned an invalid level')
    return {'truePeakDbtp': peak_value if math.isfinite(peak_value) else None,
            'integratedLufs': loudness_value if math.isfinite(loudness_value) else None,
            'silent': peak_value == -math.inf}


def level_failures(levels: dict, peak_limit: float) -> list[str]:
    if levels['silent']:
        return ['required captured sound is silent']
    if levels['truePeakDbtp'] is None or levels['truePeakDbtp'] >= peak_limit:
        return [f'rounded true peak does not establish margin below {peak_limit:g} dBTP']
    return []


def audit(inputs: list[Path], output: Path, seconds: float | None = None,
          peak_limit: float = -1.0) -> dict:
    if not inputs:
        raise ValueError('At least one input is required')
    if seconds is not None and (not math.isfinite(seconds) or seconds <= 0):
        raise ValueError('Duration must be positive and finite')
    if not math.isfinite(peak_limit):
        raise ValueError('Peak limit must be finite')
    # A unique folder preserves rejected measurements and their exact inputs.
    output.mkdir(parents=True, exist_ok=False)
    executable = imageio_ffmpeg.get_ffmpeg_exe()
    report = {'method': 'FFmpeg ebur128 with oversampled true peak',
              'seconds': seconds, 'peakLimitDbtp': peak_limit, 'peakResolutionDb': 0.1,
              'peakBoundary': 'A rounded value at the ceiling cannot prove compliance; it is rejected.',
              'audition': 'UNVERIFIED; signal measurements do not establish sound quality',
              'tool': fingerprint(Path(__file__)), 'runs': []}
    for index, path in enumerate(inputs):
        entry = {'input': path.as_posix(), 'failures': []}
        report['runs'].append(entry)
        try:
            entry['source'] = fingerprint(path)
            args = [executable, '-hide_banner', '-nostdin', '-i', str(path)]
            if seconds is not None:
                args += ['-t', str(seconds)]
            args += ['-af', 'ebur128=peak=true', '-f', 'null', '-']
            result = subprocess.run(args, capture_output=True, text=True, timeout=120)
            log = output / f'{index:03d}.log'
            log.write_text(result.stderr, encoding='utf-8')
            entry['log'] = fingerprint(log)
            if result.returncode != 0:
                raise ValueError(f'FFmpeg exited with {result.returncode}')
            entry.update(read_summary(result.stderr))
            entry['failures'].extend(level_failures(entry, peak_limit))
            if fingerprint(path) != entry['source']:
                entry['failures'].append('input changed during measurement')
        except (OSError, ValueError, subprocess.TimeoutExpired) as error:
            entry['failures'].append(str(error))
        entry['valid'] = not entry['failures']
    report['valid'] = all(r['valid'] for r in report['runs'])
    (output / 'report.json').write_text(json.dumps(report, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('inputs', type=Path, nargs='+')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--seconds', type=float)
    parser.add_argument('--peak-limit', type=float, default=-1.0)
    args = parser.parse_args()
    report = audit(args.inputs, args.output, args.seconds, args.peak_limit)
    print(json.dumps({'valid': report['valid'], 'report': str(args.output / 'report.json'),
                      'runs': [{'input': r['input'], 'failures': r['failures']} for r in report['runs']]}))
    return 0 if report['valid'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
