"""A native Unity trace must not be accepted as clean timing or headroom."""
import csv
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import test_quality_analyze as traversal_fixture


class NativeProfileManifestContract(unittest.TestCase):
    analyzer = Path(__file__).with_name('quality_analyze.py')

    def result(self, arguments, headroom=False, segmented=False, **metadata):
        fixture = traversal_fixture.TraversalContract()
        fixture.setUp()
        fixture.run['profiled'] = False  # The native CLI profiler bypasses the harness switch.
        if headroom:
            fixture.headroom_fixture()
        if segmented:
            fixture.route['segments'] = [dict(name='first visit', firstStep=0, stepCount=2)]
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            for name, value in [('run', fixture.run), ('route', fixture.route),
                                ('build', dict(arguments=arguments, **metadata))]:
                (root / (name + '.json')).write_text(json.dumps(value), encoding='utf-8-sig' if name == 'build' else 'utf-8')
            with (root / 'frames.csv').open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fixture.frames[0].keys())
                writer.writeheader()
                writer.writerows(fixture.frames)
            process = subprocess.run([sys.executable, str(self.analyzer),
                                      str(root), '--headroom' if headroom else '--performance'],
                                     capture_output=True, text=True)
            result = json.loads((root / 'analysis.json').read_text())
            return process.returncode, result

    def test_native_binary_trace_rejected_for_clean_timing(self):
        code, result = self.result(['-profiler-enable', '-profiler-log-file', 'capture.raw'])
        self.assertEqual(1, code)
        self.assertIn('launch manifest declares profiling; clean timing/headroom rejected', result['failures'])

    def test_native_trace_rejected_for_headroom(self):
        code, result = self.result(['-profiler-log-file', 'capture.raw'], headroom=True)
        self.assertEqual(1, code)
        self.assertIn('launch manifest declares profiling; clean timing/headroom rejected', result['failures'])

    def test_native_trace_rejected_for_segmented_timing(self):
        code, result = self.result(['-profiler-enable'], segmented=True)
        self.assertEqual(1, code)
        self.assertIn('launch manifest declares profiling; clean timing/headroom rejected', result['failures'])

    def test_explicit_native_diagnostic_metadata_rejected(self):
        code, result = self.result([], nativeProfilerDiagnostic=True)
        self.assertEqual(1, code)
        self.assertFalse(result['valid'])

    def test_quality_profile_argument_cannot_disagree_with_runtime(self):
        code, result = self.result(['-quality-profile'])
        self.assertEqual(1, code)
        self.assertFalse(result['valid'])

    def test_deep_profile_argument_rejected(self):
        code, result = self.result(['-deepprofiling'])
        self.assertEqual(1, code)
        self.assertFalse(result['valid'])

    def test_explicit_diagnostic_exclusion_rejected(self):
        code, result = self.result([], cleanTimingEligible=False)
        self.assertEqual(1, code)
        self.assertIn('launch manifest excludes clean timing/headroom', result['failures'])

    def test_malformed_launch_arguments_fail_closed(self):
        code, result = self.result('-profiler-enable')
        self.assertEqual(1, code)
        self.assertIn('launch manifest arguments malformed; clean timing/headroom rejected', result['failures'])

    def test_ordinary_launch_still_passes(self):
        code, result = self.result(['-quality-route', 'station.json'])
        self.assertEqual(0, code)
        self.assertTrue(result['valid'], result['failures'])

    def test_profile_word_in_path_does_not_disqualify(self):
        code, result = self.result(['-savepath', 'Builds/profiler-review/Profile', '-logFile', 'profiler.log'])
        self.assertEqual(0, code)
        self.assertTrue(result['valid'], result['failures'])


if __name__ == '__main__':
    unittest.main()
