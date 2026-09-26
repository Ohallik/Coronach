import unittest
from unittest.mock import patch
from motion_analyze import analyze_motion


class MotionEvidenceTests(unittest.TestCase):
    def fixture(self):
        frames=[];motion=[]
        for i in range(151):
            t=i/60
            stamp=f'{t:.6f}'
            frames.append(dict(elapsed=stamp,step='0',hero='Taren',form='Natural',
                x='0',z=str(t*2.6),pelvisYaw='0',chestYaw='0',forwardX='0',forwardZ='1'))
            row=dict(elapsed=stamp,step='0',hero='Taren',form='Natural',phase=str(t),clip='Walk',
                     transitioning='False',locked='False')
            for side in ['left','right']:
                row.update({side+'ToeX':'.1',side+'ToeY':'.006',side+'ToeZ':'1',side+'GroundY':'0'})
            motion.append(row)
        return dict(motion=True),frames,dict(steps=[dict(name='walk',x=0,y=1)]),motion

    def result(self,data):
        # Route/focus validation is separately covered by quality_analyze's
        # unchanged controls; isolate direction/contact adjudication here.
        with patch('motion_analyze.analyze',return_value=dict(valid=True,failures=[])):
            return analyze_motion(*data)

    def test_aligned_planted_markers_pass(self):
        result=self.result(self.fixture())
        self.assertTrue(result['valid'],result['failures'])
        self.assertGreaterEqual(result['centralContacts'],2)

    def test_original_sideways_pelvis_is_rejected(self):
        data=self.fixture()
        for row in data[1]:row['pelvisYaw']='28'
        self.assertTrue(any('steady pelvis exceeds' in x for x in self.result(data)['failures']))

    def test_skating_marker_is_rejected(self):
        data=self.fixture()
        for row in data[3]:row['leftToeZ']=str(float(row['elapsed'])*2.6)
        self.assertTrue(any('5 cm' in x for x in self.result(data)['failures']))

    def test_missing_markers_do_not_pass(self):
        data=self.fixture();data[3].clear()
        self.assertFalse(self.result(data)['valid'])

    def test_transition_only_cannot_pass(self):
        data=self.fixture()
        for row in data[3]:row['transitioning']='True'
        self.assertIn('no steady free-traversal coverage',self.result(data)['failures'])

    def test_mismatched_hero_does_not_pass(self):
        data=self.fixture();data[3][-1]['hero']='Sela'
        self.assertIn('motion/frame identity mismatch',self.result(data)['failures'])

    def test_penetration_is_rejected(self):
        data=self.fixture()
        for row in data[3]:row['rightToeY']='-.08'
        self.assertTrue(any('penetrates' in x for x in self.result(data)['failures']))

    def test_partial_cycle_cannot_establish_torso_acceptance(self):
        data=self.fixture();del data[1][61:];del data[3][61:]
        self.assertTrue(any('no complete torso cycle' in x for x in self.result(data)['failures']))

    def test_persistent_torso_bias_is_rejected(self):
        data=self.fixture()
        for row in data[1]:row['chestYaw']='28'
        self.assertTrue(any('complete-cycle torso exceeds' in x for x in self.result(data)['failures']))

    def test_natural_body_cannot_pass_shaped_route(self):
        data=self.fixture();data[2]['steps'][0]['expectedForm']='Shaped'
        self.assertIn('walk: wrong body form',self.result(data)['failures'])

    def test_lower_floor_is_not_false_penetration(self):
        data=self.fixture()
        for row in data[3]:
            for side in ['left','right']:row[side+'ToeY']='-.094';row[side+'GroundY']='-.1'
        self.assertTrue(self.result(data)['valid'])

    def test_missing_floor_cannot_pass_penetration_check(self):
        data=self.fixture()
        for row in data[3]:del row['leftGroundY']
        self.assertIn('foot surface height unavailable',self.result(data)['failures'])

    def test_restarted_clip_contacts_are_separate_strides(self):
        data=self.fixture()
        for row in data[3]:
            t=float(row['elapsed'])
            if t>=1.25:
                row['phase']=str(t-1.25)
                row['leftToeZ']=row['rightToeZ']='10'
        result=self.result(data)
        self.assertTrue(result['valid'],result['failures'])
        self.assertEqual(result['worstContactDriftMetres'],0)

    def test_wrong_hero_cannot_satisfy_the_route(self):
        data=self.fixture();data[2]['steps'][0]['expectedCharacter']='Sela'
        self.assertIn('walk: wrong hero',self.result(data)['failures'])


if __name__=='__main__':unittest.main()
