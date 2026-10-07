import csv
import json
from pathlib import Path
import tempfile
import unittest
from quality_trace import check


class TraceCoverage(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.rows = [dict(elapsed=.1 + n / 60, ms=60 if n == 2 else 16.67, step=n // 2) for n in range(4)]
        with (self.root / 'frames.csv').open('w', newline='') as stream:
            writer = csv.DictWriter(stream, self.rows[0].keys()); writer.writeheader(); writer.writerows(self.rows)
        self.index = dict(frameLimit=120, cleanTimingEligible=False,
                          segments=[dict(file='trace-000.raw', firstSample=0, lastSample=3)])
        self.report = dict(source='trace-000.raw', first=0, last=3, valid=True, missing=[], frames=[
            dict(sample=n, step=r['step'], elapsed=r['elapsed'], markerMs=100 + n, details=[])
            for n, r in enumerate(self.rows)])
        (self.root / 'trace-000.raw').write_bytes(b'fixture')

    def run_check(self):
        (self.root / 'trace-index.json').write_text(json.dumps(self.index))
        (self.root / 'trace-000.raw.summary.json').write_text(json.dumps(self.report))
        return check(self.root)

    def test_complete_trace_retains_hitch_and_both_frames(self):
        result = self.run_check()
        self.assertTrue(result['valid'], result['failures'])
        self.assertEqual(60, result['hitches'][0]['ms'])
        self.assertEqual([1, 2], [f['sample'] for f in result['hitches'][0]['nativeFrames']])

    def test_tail_only_import_rejected(self):
        self.report['frames'] = self.report['frames'][2:]
        self.assertFalse(self.run_check()['valid'])

    def test_shifted_alignment_rejected(self):
        for frame in self.report['frames']: frame['elapsed'] += 1 / 60
        self.assertFalse(self.run_check()['valid'])

    def test_nonfinite_metadata_rejected(self):
        self.report['frames'][2]['elapsed'] = float('nan')
        self.assertFalse(self.run_check()['valid'])

    def test_missing_marker_rejected(self):
        self.report['frames'][2]['markerMs'] = -1
        self.assertFalse(self.run_check()['valid'])

    def test_duplicate_sample_rejected(self):
        self.report['frames'].append(self.report['frames'][2])
        self.assertFalse(self.run_check()['valid'])

    def test_wrong_step_rejected(self):
        self.report['frames'][2]['step'] = 9
        self.assertFalse(self.run_check()['valid'])

    def test_import_rejection_preserved(self):
        self.report['valid'] = False
        self.report['missing'] = [2]
        self.assertFalse(self.run_check()['valid'])

    def test_missing_raw_trace_rejected(self):
        (self.root / 'trace-000.raw').unlink()
        self.assertFalse(self.run_check()['valid'])

    def test_diagnostic_exclusion_required(self):
        self.index['cleanTimingEligible'] = True
        self.assertFalse(self.run_check()['valid'])

    def test_segment_cannot_skip_beginning(self):
        self.index['segments'][0]['firstSample'] = 1
        self.assertFalse(self.run_check()['valid'])

    def test_invalid_bound_rejected(self):
        self.index['frameLimit'] = 50000
        self.assertFalse(self.run_check()['valid'])


if __name__ == '__main__':
    unittest.main()
