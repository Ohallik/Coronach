import copy
import unittest
from ground_pose_analyze import analyze,knee,VECTORS

class GroundPoseContract(unittest.TestCase):
    def setUp(self):
        self.frames=[dict(elapsed=(i+1)/60,step=0,hero='Taren',form='Shaped',state='Move',partnerState='Idle',focus='True') for i in range(3)]
        self.run=dict(valid=True,samples=3);self.report=dict(schema=1,complete=True,samples=3,rows=6,failure='');self.rows=[]
        for i,f in enumerate(self.frames):
            for role,hero in [('active','Taren'),('partner','Sela')]:
                r=dict(sample=i,frame=100+i,elapsed=f['elapsed'],step=0,role=role,hero=hero,form='Shaped',state=f['state'] if role=='active' else f['partnerState'],clip='Walk',phase=.25,transitioning='False',delta=1/60,grade=.5,requestedDrop=.1,available='True')
                for name in VECTORS:
                    for axis in 'XYZ':r[name+axis]=0
                r['forwardZ']=1;r['hipsY']=1;r['chestY']=1.4
                for side in ['left','right']:r[side+'HipY']=1;r[side+'KneeY']=.5;r[side+'KneeZ']=.5;r[side+'AnkleZ']=1
                self.rows.append(r)
    def result(self):return analyze(self.rows,self.frames,self.run,self.report)
    def test_complete_evaluated_poses(self):self.assertTrue(self.result()['valid'])
    def test_measures_known_straight_and_right_angle_geometry(self):
        r=self.rows[0];r['leftKneeZ']=r['leftAnkleZ']=0
        self.assertAlmostEqual(180,knee(r,'left'))
        r['leftAnkleY']=r['leftAnkleZ']=.5;self.assertAlmostEqual(90,knee(r,'left'))
    def test_missing_partner_even_with_revised_green_counts(self):
        self.rows.pop();self.report['rows']=5;self.assertFalse(self.result()['valid'])
    def test_duplicate_role(self):self.rows[1]=copy.deepcopy(self.rows[0]);self.assertFalse(self.result()['valid'])
    def test_stale_pose_frame(self):self.rows[2]['frame']=100;self.assertFalse(self.result()['valid'])
    def test_wrong_partner_frame(self):self.rows[1]['frame']=99;self.assertFalse(self.result()['valid'])
    def test_time_and_identity(self):
        for key,value in [('elapsed',3),('step',1),('hero','Sela'),('available','False'),('transitioning',''),('form','Flight')]:
            with self.subTest(key=key):
                old=self.rows[0][key];self.rows[0][key]=value;self.assertFalse(self.result()['valid']);self.rows[0][key]=old
    def test_nonfinite_clock_and_geometry(self):
        for key in ['delta','phase','hipsX','leftKneeY']:
            with self.subTest(key=key):
                old=self.rows[0][key];self.rows[0][key]=float('nan');self.assertFalse(self.result()['valid']);self.rows[0][key]=old
    def test_degenerate_leg(self):self.rows[0]['leftKneeY']=1;self.rows[0]['leftKneeZ']=0;self.assertFalse(self.result()['valid'])
    def test_unfocused_or_rejected_replay(self):
        self.frames[0]['focus']='False';self.assertFalse(self.result()['valid'])
    def test_unfinished_producer(self):self.report['complete']=False;self.assertFalse(self.result()['valid'])
    def test_transition_rows_are_kept(self):
        self.rows[0]['transitioning']='True';r=self.result();self.assertTrue(r['valid']);self.assertEqual(1,r['actors']['Taren Shaped']['transitions'])

if __name__=='__main__':unittest.main()
