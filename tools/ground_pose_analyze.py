"""Validate evaluated geometry coverage without accepting posture or motion."""
import argparse
import csv
import json
import math
from pathlib import Path

VECTORS = 'root forward velocity hips chest leftHip leftKnee leftAnkle rightHip rightKnee rightAnkle'.split()

def point(row, name):
    result = tuple(float(row[name+axis]) for axis in 'XYZ')
    if not all(math.isfinite(v) for v in result):raise ValueError('nonfinite '+name)
    return result

def knee(row, side):
    h,k,a = (point(row, side+name) for name in ['Hip','Knee','Ankle'])
    u,v = tuple(x-y for x,y in zip(h,k)),tuple(x-y for x,y in zip(a,k))
    lengths = math.dist(h,k),math.dist(a,k)
    if min(lengths) < .01:raise ValueError('degenerate knee geometry')
    return math.degrees(math.acos(max(-1,min(1,sum(x*y for x,y in zip(u,v))/(lengths[0]*lengths[1])))))

def analyze(rows, frames, run, report):
    errors=[];groups={};identities=set();previous_frame=-1
    if report.get('schema')!=1 or report.get('complete') is not True or report.get('failure') not in ('',None):
        errors.append('ground pose producer did not finish successfully')
    if not frames or run.get('samples')!=len(frames) or report.get('samples')!=len(frames) or report.get('rows')!=len(rows) or len(rows)!=len(frames)*2:
        errors.append('ground pose coverage/count mismatch')
    if run.get('valid') is not True or any(f.get('focus')!='True' for f in frames):errors.append('replay rejected or focus unavailable')
    for index,row in enumerate(rows):
        try:
            sample=int(row['sample']);frame=int(row['frame']);role=row['role'];elapsed=float(row['elapsed']);delta=float(row['delta'])
            if not 0<=sample<len(frames):raise ValueError('sample outside replay')
            f=frames[sample]
            if sample!=index//2 or role!=('active' if index%2==0 else 'partner') or (sample,role) in identities:raise ValueError('duplicate, missing or reordered role/sample')
            identities.add((sample,role))
            if frame<=0 or (index%2==0 and frame<=previous_frame) or (index%2==1 and frame!=previous_frame):raise ValueError('wrong evaluated frame')
            previous_frame=frame
            if not math.isfinite(elapsed) or not math.isfinite(float(f['elapsed'])) or abs(elapsed-float(f['elapsed']))>.000005 or int(row['step'])!=int(f['step']):raise ValueError('step/time mismatch')
            if not math.isfinite(delta) or delta<=0:raise ValueError('invalid evaluated delta')
            if row.get('available')!='True':raise ValueError('skeleton unavailable')
            if not row['hero'] or row['form'] not in ('Natural','Shaped') or row['transitioning'] not in ('True','False'):raise ValueError('actor identity unavailable')
            if role=='active' and (row['hero'],row['form'],row['state'])!=(f['hero'],f['form'],f['state']):raise ValueError('active identity mismatch')
            if role=='partner' and (row['hero']==f['hero'] or row['state']!=f['partnerState']):raise ValueError('partner identity mismatch')
            if not all(math.isfinite(float(row[k])) for k in ['phase','grade','requestedDrop']):raise ValueError('nonfinite pose metadata')
            positions={name:point(row,name) for name in VECTORS}
            if not .9<math.sqrt(sum(v*v for v in positions['forward']))<1.1:raise ValueError('invalid heading')
            left,right=knee(row,'left'),knee(row,'right')
            group=groups.setdefault(row['hero']+' '+row['form'],dict(samples=0,transitions=0,hipAboveRoot=[],leftKnee=[],rightKnee=[],moreExtendedKnee=[]))
            group['samples']+=1;group['transitions']+=row['transitioning']=='True'
            group['hipAboveRoot'].append(positions['hips'][1]-positions['root'][1])
            group['leftKnee'].append(left);group['rightKnee'].append(right);group['moreExtendedKnee'].append(max(left,right))
        except (KeyError,ValueError,TypeError,OverflowError) as error:errors.append(f'row {index}: {error}')
    for group in groups.values():
        for key in ['hipAboveRoot','leftKnee','rightKnee','moreExtendedKnee']:
            values=group[key];group[key]=dict(minimum=min(values),maximum=max(values))
    return dict(valid=not errors,failures=errors,rows=len(rows),replaySamples=len(frames),actors=groups,
                scope='Evaluated geometry coverage only; ranges include every transition. No visual, continuous-motion, physical or clean-timing acceptance.')

def read(folder):
    def doc(name):return json.loads((folder/name).read_text(encoding='utf-8-sig'))
    def table(name):
        with (folder/name).open(encoding='utf-8-sig',newline='') as stream:return list(csv.DictReader(stream))
    try:
        result=analyze(table('ground-pose.csv'),table('frames.csv'),doc('run.json'),doc('ground-pose.json'))
        build=doc('build.json')
        if build.get('groundPoseDiagnostic') is not True or '-quality-ground-pose' not in build.get('arguments',[]):
            result['failures'].append('ground pose launch identity missing');result['valid']=False
        return result
    except (OSError,ValueError,TypeError,KeyError,AttributeError) as error:
        return dict(valid=False,failures=['ground pose evidence unavailable or malformed: '+str(error)])

def include(result,folder):
    requested=(folder/'ground-pose.json').exists();p=folder/'build.json'
    if p.exists():
        build=json.loads(p.read_text(encoding='utf-8-sig'));arguments=build.get('arguments',[])
        requested|=build.get('groundPoseDiagnostic') is True or (isinstance(arguments,list) and '-quality-ground-pose' in arguments)
    if requested:
        pose=read(folder);result['groundPose']=pose;result['failures'].extend(pose['failures']);result['valid']=not result['failures']

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('folder',type=Path);parser.add_argument('--output',type=Path)
    args=parser.parse_args();output=args.output or args.folder/'ground-pose-analysis.json'
    if output.exists():raise SystemExit('Preserve earlier pose analysis; choose another output')
    result=read(args.folder);output.write_text(json.dumps(result,indent=2,allow_nan=False)+'\n',encoding='utf-8')
    print(json.dumps(result,indent=2));raise SystemExit(0 if result['valid'] else 1)
