import copy
import unittest
from native_gpu_analyze import analyze, TAG_PREFIX

class NativeGpuContract(unittest.TestCase):
    def setUp(self):
        self.identities=[dict(sample=i,frame=100+i,step=0,elapsed=(i+1)/60) for i in range(5)]
        self.frames=[dict(step='0',elapsed=f'{(i+1)/60:.6f}',focus='True') for i in range(5)]
        self.results=[dict(frame=100+i,status=0,begin=1791437013760881280+i*16666666,end=1791437013761881280+i*16666666,
                           frequency=1000000000,fenceRequired=1000+i,fenceCompleted=1000+i,tag=TAG_PREFIX|(100+i),
                           beginCpuQpc=20000000+i*166666,endCpuQpc=20000100+i*166666) for i in range(5)]
        self.run=dict(valid=True,samples=5);self.report=dict(schema=2,fenceKind='private_queue_fence',submitted=5,received=5,complete=True,failure='')
    def result(self):return analyze(self.identities,self.results,self.frames,self.run,self.report)
    def test_exact_coverage(self):self.assertTrue(self.result()['valid'])
    def test_old_unrelated_frame_fence_is_not_private_completion_evidence(self):
        self.report['schema']=1;self.report.pop('fenceKind')
        self.assertFalse(self.result()['valid'])
    def test_missing_and_duplicate_results(self):
        self.results[-1]=copy.deepcopy(self.results[0]);self.assertFalse(self.result()['valid'])
    def test_different_identity_or_time(self):
        self.identities[2]['elapsed']+=.1;self.assertFalse(self.result()['valid'])
    def test_missing_identity_even_with_green_counts(self):
        self.identities.pop();self.assertFalse(self.result()['valid'])
    def test_gpu_tag_cannot_be_stale(self):
        self.results[3]['tag']-=1;self.assertFalse(self.result()['valid'])
    def test_fence_must_complete(self):
        self.results[3]['fenceCompleted']-=1;self.assertFalse(self.result()['valid'])
    def test_clock_and_callback_failures(self):
        for key,value in [('begin',0),('frequency',0),('end',1),('status',15),('endCpuQpc',0),('begin',2**65)]:
            with self.subTest(key=key,value=value):
                original=self.results[2][key];self.results[2][key]=value
                self.assertFalse(self.result()['valid']);self.results[2][key]=original
    def test_native_or_replay_failure_cannot_be_ignored(self):
        self.report['failure']='ring overflow';self.assertFalse(self.result()['valid'])
    def test_focus_loss_cannot_be_ignored(self):
        self.frames[2]['focus']='False';self.assertFalse(self.result()['valid'])
    def test_integer_subtraction_preserves_small_intervals(self):
        for row in self.results:row['end']=row['begin']+1
        result=self.result();self.assertTrue(result['valid']);self.assertEqual(.000001,result['nativeCameraEnvelopeMs']['median'])
    def test_impossible_span_is_retained_and_rejected(self):
        self.results[2]['end']+=10**15
        result=self.result();self.assertFalse(result['valid']);self.assertGreater(result['nativeCameraEnvelopeMs']['maximum'],1000)

if __name__=='__main__':unittest.main()
