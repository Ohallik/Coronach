import copy
import unittest
from quality_analyze import analyze, analyze_segments, analyze_headroom, analyze_census

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
    def test_profile_cannot_pass_clean_performance(self):
        self.run['profiled']=True
        self.assertIn('profiling overhead disqualifies clean timing',self.result()['failures'])
    def test_first_and_warm_windows_must_cover_every_step(self):
        self.route['segments']=[dict(name='incomplete',firstStep=0,stepCount=1)]
        result=analyze_segments(self.run,self.frames,self.route,True)
        self.assertIn('segments omit or duplicate route steps',result['failures'])
    def test_duplicate_window_rejected(self):
        self.route['segments']=[dict(name='one',firstStep=0,stepCount=2),dict(name='duplicate',firstStep=0,stepCount=2)]
        self.assertFalse(analyze_segments(self.run,self.frames,self.route,True)['valid'])
    def test_window_retains_first_use_hitch(self):
        self.route['segments']=[dict(name='first visit',firstStep=0,stepCount=2)]
        self.frames[300]['ms']='117'
        self.assertIn('first visit: hitch budget exceeded',analyze_segments(self.run,self.frames,self.route,True)['failures'])
    def test_missing_feedback_fails(self):
        self.route['steps'][0].update(expectedUi='dialogue',buttons=['South'])
        self.run['interactions']=[]
        self.assertIn('outbound: UI feedback missing',self.result()['failures'])
    def test_slow_feedback_fails(self):
        self.route['steps'][0].update(expectedUi='shop',buttons=['South'])
        self.run['interactions']=[dict(step=0,expectedUi='shop',visibleResponseMs=101)]
        self.assertIn('outbound: UI feedback exceeds 100 ms',self.result()['failures'])
    def test_warmed_feedback_uses_segment_indexes(self):
        self.route['segments']=[dict(name='warm',firstStep=1,stepCount=1)]
        self.route['steps'][1].update(expectedUi='shop',buttons=['South'])
        self.run['interactions']=[dict(step=1,expectedUi='shop',visibleResponseMs=16)]
        part=analyze_segments(self.run,self.frames,self.route,True)['segments'][0]
        self.assertNotIn('return: UI feedback missing',part['failures'])

    def headroom_fixture(self):
        self.run.update(headroom=True,frameCap=-1,vSync=0)
        for frame in self.frames: frame.update(activeCpuNs='6000000',activeRenderNs='2000000',gpuWorkNs='5000000')
    def test_headroom_control_passes(self):
        self.headroom_fixture()
        self.assertTrue(analyze_headroom(self.run,self.frames,self.route)['valid'])
    def test_slow_gpu_rejects_headroom(self):
        self.headroom_fixture()
        for frame in self.frames: frame['gpuWorkNs']='17000000'
        self.assertFalse(analyze_headroom(self.run,self.frames,self.route)['valid'])
    def test_total_cpu_with_waits_is_not_active_work_evidence(self):
        self.run.update(headroom=True,frameCap=-1,vSync=0)
        for frame in self.frames: frame['mainThreadNs']='16667000'
        self.assertIn('active CPU work was not measured',analyze_headroom(self.run,self.frames,self.route)['failures'])
    def test_missing_gpu_is_explicit_not_zero_work(self):
        self.headroom_fixture()
        for frame in self.frames: frame['gpuWorkNs']='0'
        result=analyze_headroom(self.run,self.frames,self.route)
        self.assertEqual('UNSUPPORTED',result['headroomCounters']['gpuWorkNs'])
    def test_capped_run_cannot_establish_headroom(self):
        self.headroom_fixture();self.run['frameCap']=60
        self.assertFalse(analyze_headroom(self.run,self.frames,self.route)['valid'])
    def test_gpu_timestamp_cannot_pass_as_frame_duration(self):
        self.headroom_fixture()
        self.frames[300]['gpuWorkNs']='1790456037195915776'
        result=analyze_headroom(self.run,self.frames,self.route)
        self.assertIn('gpuWorkNs: impossible frame duration; counter evidence rejected',result['failures'])
        self.assertEqual(1,len(result['headroomCounters']['gpuWorkNs']['impossibleSamples']))
        self.assertGreater(result['headroomCounters']['gpuWorkNs']['worstMs'],1e12)
    def test_real_long_gpu_frame_is_retained(self):
        self.headroom_fixture()
        self.frames[300]['gpuWorkNs']='250000000'
        result=analyze_headroom(self.run,self.frames,self.route)
        self.assertEqual([],result['headroomCounters']['gpuWorkNs']['impossibleSamples'])
        self.assertEqual(250,result['headroomCounters']['gpuWorkNs']['worstMs'])

class ObjectCensusContract(unittest.TestCase):
    def rows(self):
        return [dict(seconds=str(t),sceneGameObjects='100',audioSources='4',playingSources='2',loopingSources='1',samplingMs='.2') for t in range(0,121,5)]
    def test_complete_inventory_records_counts(self):
        result=analyze_census(self.rows(),120)
        self.assertTrue(result['valid'])
        self.assertEqual(100,result['windows'][0]['sceneGameObjects']['median'])
    def test_missing_census_cannot_establish_inventory_coverage(self):
        self.assertFalse(analyze_census([],120)['valid'])
    def test_missing_tail_rejects_coverage(self):
        self.assertFalse(analyze_census(self.rows()[:12],120)['valid'])
    def test_impossible_playing_source_inventory_rejected(self):
        rows=self.rows();rows[10]['playingSources']='8'
        self.assertFalse(analyze_census(rows,120)['valid'])
    def test_sustained_growth_is_visible_in_report(self):
        rows=self.rows()
        for i,row in enumerate(rows):row['sceneGameObjects']=str(100+i*10)
        result=analyze_census(rows,120)
        self.assertGreater(result['windows'][1]['sceneGameObjects']['minimum'],result['windows'][0]['sceneGameObjects']['maximum'])

if __name__=='__main__': unittest.main()
