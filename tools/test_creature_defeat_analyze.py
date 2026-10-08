import copy
import unittest
import tempfile
from pathlib import Path
from PIL import Image
from creature_defeat_analyze import check,check_images,STAGES

class CreatureCapture(unittest.TestCase):
    def setUp(self):
        self.report=dict(schema=2,captureFailure=None,focusLost=False,observedFrames=900,bodies=['Burrower'],snapshots=[
            dict(body='Burrower',image=f'Burrower-{s}-{stage}.png',slope=s,seconds=-1 if stage=='standing' else i*.3,
                 minimum=.025,focus=True,liveColliders=5 if stage=='standing' else 0)
            for s in [0,10,-10] for i,stage in enumerate(STAGES)])
    def test_complete_diagnostic_has_no_presentation_acceptance(self):
        result=check(self.report,['Burrower']);self.assertTrue(result['valid'],result['failures']);self.assertIn('UNVERIFIED',result['scope'])
    def test_between_screenshot_focus_loss_rejects(self):
        self.report['focusLost']=True;self.assertFalse(check(self.report,['Burrower'])['valid'])
    def test_screenshot_focus_loss_rejects(self):
        self.report['snapshots'][4]['focus']=False;self.assertFalse(check(self.report,['Burrower'])['valid'])
    def test_missing_screenshot_focus_is_not_an_observed_focus_loss(self):
        del self.report['snapshots'][4]['focus'];result=check(self.report,['Burrower'])
        self.assertFalse(result['valid']);self.assertTrue(any('missing specimen focus evidence' in e for e in result['failures']))
        self.assertFalse(any('unfocused specimen image' in e for e in result['failures']))
    def test_legacy_sparse_focus_is_unavailable(self):
        del self.report['schema'];del self.report['focusLost'];del self.report['observedFrames']
        self.assertFalse(check(self.report,['Burrower'])['valid'])
    def test_missing_cleanup_cannot_be_replaced_by_duplicate_hold(self):
        self.report['snapshots'][-1]=copy.deepcopy(self.report['snapshots'][-3]);self.assertFalse(check(self.report,['Burrower'])['valid'])
    def test_zero_frame_capture_rejects(self):
        self.report['observedFrames']=0;self.assertFalse(check(self.report,['Burrower'])['valid'])
    def test_live_dead_body_hurtbox_rejects(self):
        self.report['snapshots'][3]['liveColliders']=1;self.assertFalse(check(self.report,['Burrower'])['valid'])
    def test_unrequested_specimen_cannot_accept_selection(self):
        self.assertFalse(check(self.report,['Ridgehound'])['valid'])
    def test_encoder_failure_rejects(self):
        self.report['captureFailure']='queue overflow';self.assertFalse(check(self.report,['Burrower'])['valid'])
    def test_missing_geometry_cannot_accept_a_blank_specimen(self):
        self.report['snapshots'][2]['minimum']=float('inf');self.assertFalse(check(self.report,['Burrower'])['valid'])
    def test_backward_lifecycle_time_rejects_misordered_evidence(self):
        self.report['snapshots'][1]['seconds']=2.2;self.assertFalse(check(self.report,['Burrower'])['valid'])

class CaptureFiles(unittest.TestCase):
    def test_complete_png_is_readable(self):
        with tempfile.TemporaryDirectory() as directory:
            folder=Path(directory);Image.new('RGB',(8,8),(24,48,64)).save(folder/'shot.png')
            self.assertEqual([],check_images(folder,dict(snapshots=[dict(image='shot.png')])))
    def test_png_header_without_pixels_is_not_a_complete_capture(self):
        with tempfile.TemporaryDirectory() as directory:
            folder=Path(directory);(folder/'shot.png').write_bytes(b'\x89PNG\r\n\x1a\n')
            self.assertTrue(check_images(folder,dict(snapshots=[dict(image='shot.png')])))

if __name__=='__main__':unittest.main()
