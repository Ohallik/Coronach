import copy
import unittest
from quality_analyze import analyze

class TraversalContract(unittest.TestCase):
    def setUp(self):
        self.run=dict(valid=True,failures=[],width=1920,height=1080,captured=False)
        self.route=dict(scene='Hub_Decks',steps=[dict(name='outbound',seconds=60,navigate=True,point=dict(x=60,z=0)),dict(name='return',seconds=60,navigate=True,point=dict(x=0,z=0))])
        self.frames=[]
        for i in range(7200):
            t=(i+1)/60
            self.frames.append(dict(ms=str(1000/60),elapsed=str(t),step=str(i//3600),scene='Hub_Decks',hero='Taren',focus='True',paused='False',x=str(t if t<=60 else 120-t),z='0',cameraX=str(t),cameraZ='0',gcCollections='0',gcBytes='0'))
    def result(self): return analyze(self.run,self.frames,self.route,True)
    def test_clean_fixture_passes(self): self.assertTrue(self.result()['valid'])
    def test_injected_100ms_stall_fails(self):
        self.frames[300]['ms']='117'
        self.assertIn('hitch budget exceeded',self.result()['failures'])
    def test_stationary_avatar_fails(self):
        for f in self.frames: f['x']='0'
        self.assertIn('stationary or insufficient traversal',self.result()['failures'])
    def test_focus_loss_fails(self):
        self.frames[120]['focus']='False'
        self.assertIn('lost focus',self.result()['failures'])
    def test_paused_simulation_fails(self):
        self.frames[200]['paused']='True'
        self.assertIn('paused simulation',self.result()['failures'])
    def test_wrong_scene_fails(self):
        for f in self.frames: f['scene']='Arena_Ground'
        self.assertFalse(self.result()['valid'])
    def test_wrong_resolution_fails(self):
        self.run['width']=1280
        self.assertIn('wrong resolution',self.result()['failures'])
    def test_missing_samples_fails(self):
        self.frames=self.frames[:2000]
        self.assertIn('missing samples',self.result()['failures'])
    def test_missed_checkpoint_fails_even_when_avatar_moves(self):
        self.route['steps'][0]['point']['z']=100
        self.assertIn('outbound: checkpoint not reached',self.result()['failures'])
    def test_capture_cannot_pass_clean_performance(self):
        self.run['captured']=True
        self.assertIn('capture overhead disqualifies clean timing',self.result()['failures'])

if __name__=='__main__': unittest.main()
