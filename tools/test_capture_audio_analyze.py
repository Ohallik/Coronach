import json
from pathlib import Path
import struct
import tempfile
import unittest
from capture_audio_analyze import check, include, MARKER

class BoundedAudioTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.report = dict(sampleRate=48000, channels=2, bufferCount=32, maximumBlockSamples=32768,
                           peakQueuedBlocks=2, sampleBufferBytes=4325376, acceptedSamples=8,
                           writtenSamples=8, writtenBytes=32, completed=True, failure='')
        self.wav = struct.pack('<4sI8sIHHIIHH4sI8f', b'RIFF', 68, b'WAVEfmt ', 16, 3, 2, 48000,
                               384000, 8, 32, b'data', 32, *[.125] * 8)
        (self.folder/'capture.txt').write_text(MARKER)
        self.write()
    def write(self):
        (self.folder/'capture-audio.json').write_text(json.dumps(self.report))
        (self.folder/'mix.wav').write_bytes(self.wav)
    def test_complete(self):
        result = check(self.folder)
        self.assertTrue(result['valid'], result)
        self.assertEqual(result['seconds'], 4/48000)
        self.assertEqual(result['audition'], 'UNVERIFIED')
    def test_malformed_report_rejects(self):
        for value in ['[1,2]', 'null', 'false', '{broken']:
            (self.folder/'capture-audio.json').write_text(value)
            self.assertFalse(check(self.folder)['valid'])
    def test_missing_callback_coverage_cannot_hide_behind_equal_counts(self):
        (self.folder/'run.json').write_text(json.dumps(dict(seconds=1)))
        self.assertFalse(check(self.folder)['valid'])
        (self.folder/'run.json').write_text(json.dumps(dict(seconds=4/48000)))
        self.assertTrue(check(self.folder)['valid'])
    def test_missing_report_rejects(self):
        (self.folder/'capture-audio.json').unlink()
        self.assertFalse(check(self.folder)['valid'])
    def test_legacy_not_relabelled(self):
        (self.folder/'capture-audio.json').unlink()
        (self.folder/'capture.txt').write_text('original capture')
        self.assertIsNone(check(self.folder))
    def test_failed_incomplete_or_corrupt_counts(self):
        for key,value in [('completed',False),('failure','overflow'),('channels',3),('acceptedSamples',10),
                          ('writtenSamples',True),('writtenBytes',28),('sampleBufferBytes',0),
                          ('bufferCount',33),('peakQueuedBlocks',33),('sampleRate',None)]:
            with self.subTest(key=key):
                original=self.report[key];self.report[key]=value;self.write()
                self.assertFalse(check(self.folder)['valid']);self.report[key]=original
    def test_truncated_trailing_or_false_header(self):
        for wav in [self.wav[:-1],self.wav+b'\0',b'FAKE'+self.wav[4:],self.wav[:20]+b'\x01\0'+self.wav[22:]]:
            (self.folder/'mix.wav').write_bytes(wav)
            self.assertFalse(check(self.folder)['valid'])
    def test_nonfinite_kept_and_rejected(self):
        for value in [float('nan'),float('inf'),float('-inf')]:
            (self.folder/'mix.wav').write_bytes(self.wav[:-4]+struct.pack('<f',value))
            self.assertFalse(check(self.folder)['valid'])
    def test_runtime_success_does_not_hide_incomplete_audio(self):
        self.report['completed']=False;self.write()
        result=dict(valid=True,failures=[]);include(result,self.folder)
        self.assertFalse(result['valid']);self.assertTrue(result['failures'])
    def test_earlier_failure_is_preserved(self):
        result=dict(valid=False,failures=['focus lost']);include(result,self.folder)
        self.assertFalse(result['valid']);self.assertEqual(['focus lost'],result['failures'])

if __name__ == '__main__': unittest.main()
