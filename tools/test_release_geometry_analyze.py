import copy
import csv
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from release_geometry_analyze import analyze, include


def v(x=0,y=0,z=0): return dict(x=x,y=y,z=z)
Q = dict(x=0,y=0,z=0,w=1)
M = [1,0,0,0,0,1,0,0,0,0,1,0,0,0,1,0]


def mesh(role, x, instance, hero=''):
    points=[v(x+dx,dy,10+dz) for dx in (-.5,.5) for dy in (-.5,.5) for dz in (-.5,.5)]
    projected=[v(.5+.5*p['x']/p['z'],.5+.5*p['y']/p['z'],p['z']) for p in points]
    return dict(role=role,hero=hero,instance=instance,meshes=1,available=True,hullRadius=1 if hero else 0,
                position=v(x,0,10),velocity=v(),rotation=Q,minimum=v(x-.5,-.5,9.5),maximum=v(x+.5,.5,10.5),corners=points,
                viewportMinimum={k:min(p[k] for p in projected) for k in 'xyz'},
                viewportMaximum={k:max(p[k] for p in projected) for k in 'xyz'})


def fixture():
    rows=[]
    for i in range(2):
        rows.append(dict(sample=i,frame=100+i,step=7,source=-1,episode=1,elapsed=.1+i*.02,releaseAge=i*.02,gameTime=10+i*.02,paused=False,
                         sourcePosition=v(),sourceRotation=Q,cameraPosition=v(),cameraRotation=Q,viewProjection=M,
                         anatomy=[mesh('head' if j==0 else 'part'+str(j),j,j+1) for j in range(8)],
                         heroes=[mesh('active',20,9,'Taren'),mesh('partner',30,10,'Sela')]))
    frames=[dict(elapsed=str(.1+i*.02),step='7',focus='True',paused='False',hero='Taren',x='20',y='0',z='10',cameraX='0',cameraY='0',cameraZ='0') for i in range(3)]
    run=dict(samples=3,valid=True)
    report=dict(schema=1,complete=True,failure=None,samples=3,frames=2,dropped=0,frameLimit=4096,sourceLimit=16,
                spans=[dict(source=-1,episode=1,firstSample=0,lastSample=1,frames=2,end='removed')])
    return rows,frames,run,report


class ReleaseGeometryTests(unittest.TestCase):
    def test_real_projection_contract(self):
        result=analyze(*fixture());self.assertTrue(result['valid'],result['failures'])
        self.assertEqual(2,result['frames']);self.assertEqual(2,result['heroes']['Sela']['samples'])
        self.assertGreater(result['heroes']['Sela']['minimumPlanarHullClearance'],0)

    def test_projected_overlap_is_retained_not_rejected_or_called_collision(self):
        rows,frames,run,report=fixture()
        # A far-away, physically separate hull projects over the head.
        for row in rows:
            hero=mesh('partner',0,10,'Sela')
            for p in hero['corners']:p['z']+=20
            hero['position']['z']+=20;hero['minimum']['z']+=20;hero['maximum']['z']+=20
            projected=[v(.5+.5*p['x']/p['z'],.5+.5*p['y']/p['z'],p['z']) for p in hero['corners']]
            hero['viewportMinimum']={k:min(p[k] for p in projected) for k in 'xyz'}
            hero['viewportMaximum']={k:max(p[k] for p in projected) for k in 'xyz'}
            row['heroes'][1]=hero
        result=analyze(rows,frames,run,report);self.assertTrue(result['valid'],result['failures'])
        sela=result['heroes']['Sela'];self.assertGreater(sela['minimumPlanarHullClearance'],10);self.assertEqual(2,sela['projectedBoundsOverlapSamples'])

    def test_pause_preserves_geometry(self):
        rows,frames,run,report=fixture()
        for row,frame in zip(rows,frames):row['paused']=True;row['releaseAge']=0;frame['paused']='True'
        result=analyze(rows,frames,run,report);self.assertTrue(result['valid'],result['failures'])

    def test_no_departure_retains_explicit_zero(self):
        rows,frames,run,report=fixture();report.update(frames=0,spans=[])
        result=analyze([],frames,run,report);self.assertTrue(result['valid'],result['failures']);self.assertEqual({},result['heroes'])

    def test_legacy_evidence_does_not_invent_new_observations(self):
        with tempfile.TemporaryDirectory() as folder:
            result=dict(valid=True,failures=[]);include(result,folder)
            self.assertNotIn('releaseGeometry',result)

    def test_promised_but_missing_sidecar_rejects(self):
        with tempfile.TemporaryDirectory() as folder:
            result=dict(valid=True,failures=[]);include(result,folder,1)
            self.assertFalse(result['valid']);self.assertIn('unavailable',result['failures'][0])

    def test_bad_declared_version_rejects(self):
        for version in (True,1.0,2,-1,'1',None):
            with self.subTest(version=version),tempfile.TemporaryDirectory() as folder:
                result=dict(valid=True,failures=[]);include(result,folder,version);self.assertFalse(result['valid'])

    def test_cli_requires_promised_geometry_and_preserves_legacy(self):
        import test_quality_analyze
        traversal=test_quality_analyze.TraversalContract();traversal.setUp()
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)
            (root/'route.json').write_text(json.dumps(traversal.route),encoding='utf-8')
            with (root/'frames.csv').open('w',newline='',encoding='utf-8') as output:
                writer=csv.DictWriter(output,fieldnames=traversal.frames[0]);writer.writeheader();writer.writerows(traversal.frames)
            for version,valid in [(0,True),(1,False)]:
                traversal.run.update(samples=len(traversal.frames),releaseTraceVersion=version)
                (root/'run.json').write_text(json.dumps(traversal.run),encoding='utf-8')
                completed=subprocess.run([sys.executable,str(Path(__file__).with_name('quality_analyze.py')),folder],capture_output=True,text=True)
                self.assertEqual(valid,completed.returncode==0,completed.stdout+completed.stderr)

    def test_cli_excludes_geometry_even_when_capture_flag_disagrees(self):
        import test_quality_analyze
        traversal=test_quality_analyze.TraversalContract();traversal.setUp()
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);traversal.run.update(samples=len(traversal.frames),releaseTraceVersion=1)
            (root/'route.json').write_text(json.dumps(traversal.route),encoding='utf-8')
            (root/'run.json').write_text(json.dumps(traversal.run),encoding='utf-8')
            (root/'build.json').write_text(json.dumps(dict(arguments=[])),encoding='utf-8')
            with (root/'frames.csv').open('w',newline='',encoding='utf-8') as output:
                writer=csv.DictWriter(output,fieldnames=traversal.frames[0]);writer.writeheader();writer.writerows(traversal.frames)
            report=fixture()[3];report.update(samples=len(traversal.frames),frames=0,spans=[])
            (root/'release-geometry.json').write_text(json.dumps(report),encoding='utf-8')
            (root/'release-geometry.jsonl').write_text('',encoding='utf-8')
            for performance,valid in [(False,True),(True,False)]:
                command=[sys.executable,str(Path(__file__).with_name('quality_analyze.py')),folder]
                if performance:command.append('--performance')
                completed=subprocess.run(command,capture_output=True,text=True)
                self.assertEqual(valid,completed.returncode==0,completed.stdout+completed.stderr)
                result=json.loads((root/'analysis.json').read_text(encoding='utf-8'))
                if performance:self.assertIn('evaluated release geometry overhead disqualifies clean timing/headroom',result['failures'])


def fault_test(change):
    def test(self):
        data=copy.deepcopy(fixture());change(*data);result=analyze(*data)
        self.assertFalse(result['valid'],result)
    return test


FAULTS={
    'false_completion':lambda r,f,u,p:p.update(complete=False),
    'producer_failure':lambda r,f,u,p:p.update(failure='overflow'),
    'rejected_replay':lambda r,f,u,p:u.update(valid=False),
    'lost_focus':lambda r,f,u,p:f[0].update(focus='False'),
    'dropped_frames':lambda r,f,u,p:p.update(dropped=1),
    'missing_frame':lambda r,f,u,p:r.pop(),
    'missing_part':lambda r,f,u,p:r[0]['anatomy'].pop(),
    'missing_partner':lambda r,f,u,p:r[0]['heroes'].pop(),
    'unavailable_mesh':lambda r,f,u,p:r[0]['anatomy'][3].update(available=False),
    'wrong_hero':lambda r,f,u,p:r[0]['heroes'][0].update(hero='Sela'),
    'wrong_clock':lambda r,f,u,p:r[0].update(elapsed=.101),
    'wrong_checkpoint':lambda r,f,u,p:r[0].update(step=8),
    'repeated_frame':lambda r,f,u,p:r[1].update(frame=100),
    'wrong_source':lambda r,f,u,p:r[0].update(source=-2),
    'truncated_release':lambda r,f,u,p:p['spans'][0].update(end='capture-ended-mid-release'),
    'zero_bounds':lambda r,f,u,p:r[0]['anatomy'][0].update(maximum=r[0]['anatomy'][0]['minimum']),
    'wrong_world_geometry':lambda r,f,u,p:r[0]['heroes'][0]['maximum'].update(x=25),
    'wrong_projected_geometry':lambda r,f,u,p:r[0]['anatomy'][0]['viewportMaximum'].update(x=5),
    'missing_corners':lambda r,f,u,p:r[0]['anatomy'][0]['corners'].pop(),
    'zero_camera_matrix':lambda r,f,u,p:r[0].update(viewProjection=[0]*16),
    'wrong_camera':lambda r,f,u,p:r[0]['cameraPosition'].update(x=1),
    'wrong_root':lambda r,f,u,p:r[0]['heroes'][0]['position'].update(z=11),
    'no_rotation':lambda r,f,u,p:r[0].update(cameraRotation=v()),
    'nonfinite_corner':lambda r,f,u,p:r[0]['anatomy'][0]['corners'][0].update(x=float('nan')),
    'duplicated_identity':lambda r,f,u,p:r[0]['anatomy'][1].update(instance=1),
    'bad_hull':lambda r,f,u,p:r[0]['heroes'][1].update(hullRadius=0),
    'missing_span':lambda r,f,u,p:p.update(spans=[]),
    'wrong_span_count':lambda r,f,u,p:p['spans'][0].update(frames=1),
}
for name,change in FAULTS.items():setattr(ReleaseGeometryTests,'test_reject_'+name,fault_test(change))

if __name__=='__main__':unittest.main()
