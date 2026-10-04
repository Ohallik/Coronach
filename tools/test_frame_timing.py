"""Independent API headroom controls; corrupted/missing evidence must fail."""
import unittest
from test_quality_analyze import TraversalContract
from quality_analyze import analyze_headroom


class FrameTimingContract(unittest.TestCase):
    def setUp(self):
        fixture = TraversalContract()
        fixture.setUp()
        fixture.headroom_fixture()
        self.run, self.frames, self.route = fixture.run, fixture.frames, fixture.route
        self.run.update(frameTimingRequested=True, frameTimingEnabled=True)
        for i, row in enumerate(self.frames):
            row.update(ftmTimestamp=str(i+1), ftmGpuMs='5', ftmCpuMs='6')

    def result(self):
        return analyze_headroom(self.run, self.frames, self.route)

    def test_matching_sources_pass(self):
        self.assertTrue(self.result()['valid'])

    def test_disabled_api_cannot_pass(self):
        self.run['frameTimingEnabled'] = False
        self.assertFalse(self.result()['valid'])

    def test_missing_api_samples_cannot_pass(self):
        for row in self.frames:
            row.pop('ftmGpuMs')
        self.assertFalse(self.result()['valid'])

    def test_zero_gpu_is_unavailable(self):
        for row in self.frames:
            row['ftmGpuMs'] = '0'
        self.assertFalse(self.result()['valid'])

    def test_one_repeated_api_frame_is_not_a_complete_measurement(self):
        for row in self.frames:
            row['ftmTimestamp'] = '1'
        self.assertFalse(self.result()['valid'])

    def test_single_impossible_api_timestamp_rejects_even_with_fast_p95(self):
        self.frames[300]['ftmGpuMs'] = '1790456079119.5764'
        self.assertFalse(self.result()['valid'])

    def test_nonfinite_api_duration_rejects(self):
        self.frames[300]['ftmGpuMs'] = 'nan'
        self.assertFalse(self.result()['valid'])

    def test_disagreeing_sources_are_not_a_headroom_pass(self):
        for row in self.frames:
            row['ftmGpuMs'] = '10'
        self.assertFalse(self.result()['valid'])

    def test_slow_api_gpu_is_not_replaced_by_fast_profiler(self):
        for row in self.frames:
            row['ftmGpuMs'] = '17'
        self.assertFalse(self.result()['valid'])


if __name__ == '__main__':
    unittest.main()
