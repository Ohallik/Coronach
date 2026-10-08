"""Capture completeness/focus only; never accepts animation or audio quality."""
import argparse
import json
import math
from pathlib import Path
from PIL import Image

BODIES=('Ridgehound','Scrapmite','Burrower','ChoristerDart','ChoristerDrifter','Shellmine','Cantor')
GROUND=set(BODIES[:3])
STAGES=('standing','25','50','75','100','side-held','cleanup35','cleanup75')

def check(report, bodies):
    errors=[]
    if not bodies or len(set(bodies))!=len(bodies) or any(b not in BODIES for b in bodies):
        errors.append('invalid requested specimen set')
    if report.get('schema')!=2:errors.append('continuous focus evidence unavailable: schema 2 required')
    if report.get('captureFailure'):errors.append('capture failure: '+str(report['captureFailure']))
    if report.get('focusLost') is not False:errors.append('focus lost or continuous focus evidence missing')
    if report.get('bodies')!=list(bodies):errors.append('recorded specimen selection differs from request')
    snapshots=report.get('snapshots',[])
    count=report.get('observedFrames')
    if type(count) is not int or count<len(snapshots) or count<=0:errors.append('missing or insufficient observed focus frames')
    expected={f'{b}-{s}-{stage}.png':(b,s,stage) for b in bodies for s in ((0,10,-10) if b in GROUND else (0,)) for stage in STAGES}
    seen=set();last_times={};last_stages={}
    for shot in snapshots:
        name=shot.get('image')
        if name not in expected:errors.append('unexpected specimen image: '+str(name));continue
        if name in seen:errors.append('duplicate specimen image: '+name)
        seen.add(name);body,slope,stage=expected[name]
        if shot.get('body')!=body or shot.get('slope')!=slope:errors.append('specimen identity mismatch: '+name)
        if 'focus' not in shot:errors.append('missing specimen focus evidence: '+name)
        elif shot['focus'] is not True:errors.append('unfocused specimen image: '+name)
        elapsed=shot.get('seconds');colliders=shot.get('liveColliders');minimum=shot.get('minimum')
        if type(minimum) not in (int,float) or not math.isfinite(minimum):errors.append('missing or invalid specimen geometry: '+name)
        key=(body,slope);index=STAGES.index(stage)
        if index!=last_stages.get(key,-1)+1:errors.append('out-of-order specimen stage: '+name)
        last_stages[key]=index
        if type(elapsed) not in (int,float) or not math.isfinite(elapsed) or (elapsed>=0 if stage=='standing' else elapsed<0):
            errors.append('invalid specimen lifecycle time: '+name)
        else:
            if elapsed<last_times.get(key,-math.inf):errors.append('backward specimen lifecycle time: '+name)
            last_times[key]=elapsed
        if type(colliders) is not int or (colliders<=0 if stage=='standing' else colliders!=0):
            errors.append('invalid live-collider lifecycle: '+name)
    missing=sorted(set(expected)-seen)
    if missing:errors.append('missing specimen stages: '+', '.join(missing))
    return dict(valid=not errors,failures=errors,bodies=list(bodies),snapshots=len(snapshots),observedFrames=count,
                scope='Diagnostic completeness/focus and collider lifecycle only. Continuous motion/audio and physical acceptance UNVERIFIED.')

def check_images(folder,report):
    errors=[]
    for shot in report.get('snapshots',[]):
        name=shot.get('image','')
        if not isinstance(name,str) or Path(name).name!=name or not name:
            errors.append('unsafe or missing image name');continue
        path=folder/name
        if not path.is_file():errors.append('missing image file: '+name);continue
        try:
            with Image.open(path) as picture:
                if picture.format!='PNG':errors.append('invalid PNG file: '+name)
                else:picture.load()
        except (OSError,ValueError):errors.append('incomplete or invalid PNG file: '+name)
    return errors

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('--body',action='append',choices=BODIES);args=p.parse_args()
    report=json.loads((args.folder/'report.json').read_text())
    result=check(report,args.body or BODIES)
    result['failures']+=check_images(args.folder,report)
    result['valid']=not result['failures']
    destination=args.folder/'capture-analysis.json'
    if destination.exists():raise SystemExit('Preserve earlier capture analysis')
    destination.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
    raise SystemExit(0 if result['valid'] else 1)
