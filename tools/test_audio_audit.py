"""Actual FFmpeg controls for the captured-mix check; no game audio is replaced."""
import math
from pathlib import Path
import struct
import tempfile
import unittest
import wave

from audio_audit import audit, fingerprint, read_summary, level_failures


class AudioAuditTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)

    def tearDown(self):
        self.assertEqual(self.root.resolve().parent, Path(tempfile.gettempdir()).resolve())
        self.temp.cleanup()

    def signal(self, amplitude):
        path = self.root / f'signal-{amplitude}.wav'
        with wave.open(str(path), 'wb') as stream:
            stream.setnchannels(1)
            stream.setsampwidth(2)
            stream.setframerate(48000)
            samples = [round(32767 * amplitude * math.sin(2 * math.pi * 997 * i / 48000))
                       for i in range(24000)]
            stream.writeframes(struct.pack('<' + 'h' * len(samples), *samples))
        return path

    def test_audible_signal_below_ceiling_passes_without_modifying_input(self):
        source = self.signal(.2)
        before = fingerprint(source)
        report = audit([source], self.root / 'normal')
        self.assertTrue(report['valid'])
        self.assertFalse(report['runs'][0]['silent'])
        self.assertAlmostEqual(report['runs'][0]['truePeakDbtp'], -14, delta=.2)
        self.assertEqual(before, fingerprint(source))

    def test_silence_is_rejected_even_though_it_is_below_peak_ceiling(self):
        report = audit([self.signal(0)], self.root / 'silent')
        self.assertFalse(report['valid'])
        self.assertTrue(report['runs'][0]['silent'])

    def test_hot_unclipped_signal_is_rejected_by_true_peak_ceiling(self):
        report = audit([self.signal(.99)], self.root / 'hot')
        self.assertFalse(report['valid'])
        self.assertGreater(report['runs'][0]['truePeakDbtp'], -1)

    def test_bad_audio_is_rejected_with_retained_log(self):
        source = self.root / 'broken.wav'
        source.write_bytes(b'not audio')
        report = audit([source], self.root / 'bad')
        self.assertFalse(report['valid'])
        self.assertTrue((self.root / 'bad/000.log').exists())

    def test_existing_evidence_folder_is_never_overwritten(self):
        folder = self.root / 'existing'
        folder.mkdir()
        with self.assertRaises(FileExistsError):
            audit([self.signal(.2)], folder)

    def test_missing_measurement_summary_cannot_pass(self):
        with self.assertRaises(ValueError):
            read_summary('FFmpeg opened the file but produced no measurements')

    def test_rounded_peak_at_ceiling_does_not_prove_required_margin(self):
        # The summary rounds to 0.1 dB. A printed -1.0 may be -0.96 dBTP.
        self.assertTrue(level_failures({'silent': False, 'truePeakDbtp': -1.0}, -1.0))
        self.assertFalse(level_failures({'silent': False, 'truePeakDbtp': -1.1}, -1.0))


if __name__ == '__main__':
    unittest.main()
