"""Fail-closed traversal evidence validation; no dropped or winsorized timings."""
import argparse
import csv
import json
import math
from pathlib import Path

def percentile(values, fraction):
    values = sorted(values)
    return values[max(0, math.ceil(len(values) * fraction) - 1)]

def analyze(run, frames, route, performance=False):
    errors = list(run.get('failures', []))
    if not run.get('valid'): errors.append('runtime validation rejected')
    if (run.get('width'), run.get('height')) != (1920,1080): errors.append('wrong resolution')
    duration = sum(s['seconds'] for s in route['steps'] if not s.get('until'))
    if len(frames) < duration * 20: errors.append('missing samples')
    if not frames: return {'valid':False, 'failures':errors+['empty trace']}
    times = [float(f['ms']) for f in frames]
    elapsed = [float(f['elapsed']) for f in frames]
    if elapsed[-1] < duration - .15: errors.append('route truncated')
    if any(b <= a for a,b in zip(elapsed,elapsed[1:])): errors.append('nonmonotonic trace')
    if abs(sum(times)/1000-elapsed[-1])>.05: errors.append('unaccounted frame intervals')
    if any(f['focus']!='True' for f in frames): errors.append('lost focus')
    if any(f['paused']!='False' for f in frames): errors.append('paused simulation')
    expected_steps=set(range(len(route['steps'])))
    if {int(f['step']) for f in frames} != expected_steps: errors.append('checkpoint samples missing')
    positions=[(float(f['x']),float(f['z'])) for f in frames]
    distances=[math.dist(a,b) if frames[i]['scene']==frames[i+1]['scene'] and frames[i]['hero']==frames[i+1]['hero'] else 0 for i,(a,b) in enumerate(zip(positions,positions[1:]))]
    distance=sum(distances)
    if distance < 10: errors.append('stationary or insufficient traversal')
    for i,step in enumerate(route['steps']):
        rows=[f for f in frames if int(f['step'])==i]
        if not rows: continue
        expected=step.get('expectedScene') or route['scene']
        if rows[-1]['scene'] != expected: errors.append(step['name']+': wrong scene')
        if step.get('navigate') and not step.get('approachPartner'):
            target=(step['point']['x'],step['point']['z'])
            if min(math.dist((float(f['x']),float(f['z'])),target) for f in rows)>step.get('tolerance',.65)+.1:
                errors.append(step['name']+': checkpoint not reached')
    hitches={str(t):sum(x>t for x in times) for t in [25,33.3,50,100]}
    if performance:
        if run.get('captured'): errors.append('capture overhead disqualifies clean timing')
        if duration<120: errors.append('timing route shorter than 120 seconds')
        if percentile(times,.95)>18.5: errors.append('p95 exceeds 18.5 ms')
        if percentile(times,.99)>25: errors.append('p99 exceeds 25 ms')
        if hitches['33.3']>1 or hitches['50']>0: errors.append('hitch budget exceeded')
    moving=[]
    for i,d in enumerate(distances,1):
        dt=(elapsed[i]-elapsed[i-1])
        if dt<=0 or d/dt<2 or d/dt>6: continue
        a,b=frames[i-1],frames[i]
        cam=math.hypot(float(b['cameraX'])-float(a['cameraX']),float(b['cameraZ'])-float(a['cameraZ']))/dt
        moving.append(cam)
    return dict(valid=not errors, failures=sorted(set(errors)), samples=len(frames), seconds=elapsed[-1], distance=distance,
        medianMs=percentile(times,.5),p95Ms=percentile(times,.95),p99Ms=percentile(times,.99),worstMs=max(times),hitches=hitches,
        gcCollections=int(frames[-1]['gcCollections'])-int(frames[0]['gcCollections']),
        gcBytesPerFrameMedian=percentile([int(f['gcBytes']) for f in frames],.5),
        movingSamples=len(moving), stoppedCameraWhileWalking=sum(v<.02 for v in moving),
        note='Main/render-thread columns include cap and presentation waits. They are not active CPU/GPU cost. Skeletal yaw requires visual calibration.')

def plot(frames, destination):
    w,h=1600,300; seconds=float(frames[-1]['elapsed']); scale=120
    points=' '.join(f'{float(f["elapsed"])/seconds*w:.1f},{h-min(scale,float(f["ms"]))/scale*h:.1f}' for f in frames)
    content=f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h+50}"><rect width="100%" height="100%" fill="#101723"/><polyline points="{points}" fill="none" stroke="#66ddaa" stroke-width="1"/>'
    for ms in [16.667,25,33.3,50,100]:
        y=h-ms/scale*h
        content+=f'<path d="M0 {y}H{w}" stroke="#586477"/><text x="8" y="{y-2}" fill="white">{ms} ms</text>'
    last=None
    for f in frames:
        if f['step']==last: continue
        last=f['step']; x=float(f['elapsed'])/seconds*w
        content+=f'<path d="M{x} 0V{h}" stroke="#374356"/><text x="{x}" y="{h+18}" fill="white">{last}</text>'
    content+=f'<text x="8" y="{h+42}" fill="white">Seconds 0–{seconds:.2f}; checkpoint indexes along bottom. Plot clips at 120 ms; raw CSV and summary retain all values.</text></svg>'
    destination.write_text(content)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('--performance',action='store_true');args=p.parse_args()
    run=json.loads((args.folder/'run.json').read_text());route=json.loads((args.folder/'route.json').read_text())
    frames=list(csv.DictReader((args.folder/'frames.csv').open()))
    result=analyze(run,frames,route,args.performance)
    (args.folder/'analysis.json').write_text(json.dumps(result,indent=2)+'\n');plot(frames,args.folder/'frame-times.svg')
    print(json.dumps(result,indent=2))
    raise SystemExit(0 if result['valid'] else 1)
