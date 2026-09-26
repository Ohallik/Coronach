"""Supplemental standalone motion measurements, using calibrated skeletal markers.

Rendered-mesh regressions independently validate these markers. This report does
not replace viewing the generated bodies, starts/stops, weapons or combat aim.
"""
import argparse
import csv
import json
import math
from pathlib import Path
from quality_analyze import analyze


def angle(a, b):
    return (a-b+180) % 360-180


def average_angle(values):
    return math.degrees(math.atan2(sum(math.sin(math.radians(x)) for x in values),
                                  sum(math.cos(math.radians(x)) for x in values)))


def point(row, name):
    return tuple(float(row[name+axis]) for axis in 'XYZ')


def cycle_means(curve):
    """Integrate only whole, continuous animation cycles, never three snapshots."""
    if len(curve)<2 or any(b[0]<=a[0] for a,b in zip(curve,curve[1:])):return []
    means=[]
    for start in range(math.ceil(curve[0][0]),math.floor(curve[-1][0])):
        total_sin=total_cos=0
        for (p,a),(q,b) in zip(curve,curve[1:]):
            lo=max(p,start);hi=min(q,start+1)
            if hi<=lo:continue
            # Angle interpolation follows the short arc across wraparound.
            left=a+angle(b,a)*(lo-p)/(q-p);right=a+angle(b,a)*(hi-p)/(q-p)
            total_sin+=(math.sin(math.radians(left))+math.sin(math.radians(right)))*(hi-lo)*.5
            total_cos+=(math.cos(math.radians(left))+math.cos(math.radians(right)))*(hi-lo)*.5
        means.append(math.degrees(math.atan2(total_sin,total_cos)))
    return means


def analyze_motion(run, frames, route, motion):
    result=analyze(run,frames,route)
    errors=result['failures']
    if not run.get('motion'):errors.append('motion recording was not requested')
    if len(motion)!=len(frames):errors.append('motion/frame coverage mismatch')
    indexed={r['elapsed']:r for r in motion}
    groups={}
    step_start={}
    contacts={}
    minimum_y=math.inf
    for i,f in enumerate(frames):
        step=int(f['step']);t=float(f['elapsed'])
        step_start.setdefault(step,t)
        m=indexed.get(f['elapsed'])
        if m is None:continue
        if (f['hero'],f['form'],f['step'])!=(m['hero'],m['form'],m['step']):
            errors.append('motion/frame identity mismatch');continue
        # Do not pretend navigation, transitions or a deliberate locked aim are
        # steady free traversal. Record the excluded counts and keep every raw row.
        config=route['steps'][step]
        if config.get('expectedForm') and f['form']!=config['expectedForm']:
            errors.append(config['name']+': wrong body form')
        if config.get('navigate') or not (config.get('x') or config.get('y')):continue
        if t-step_start[step]<.5 or m['transitioning']!='False' or m['locked']!='False':continue
        if i==0 or frames[i-1]['step']!=f['step']:continue
        previous=frames[i-1];dt=t-float(previous['elapsed'])
        dx=float(f['x'])-float(previous['x']);dz=float(f['z'])-float(previous['z'])
        speed=math.hypot(dx,dz)/dt
        if speed<.2:continue
        travel=math.degrees(math.atan2(dx,dz))
        key=f"{f['hero']} {f['form']} {config['name']} {m['clip']}"
        group=groups.setdefault(key,dict(samples=0,pelvis=[],torso=[],heading=[],speed=[],curve=[],identity=(f['hero'],f['form'],m['clip'])))
        group['samples']+=1;group['speed'].append(speed)
        group['pelvis'].append(angle(float(f['pelvisYaw']),travel))
        group['torso'].append(angle(float(f['chestYaw']),travel))
        group['curve'].append((float(m['phase']),angle(float(f['chestYaw']),travel)))
        group['heading'].append(angle(math.degrees(math.atan2(float(f['forwardX']),float(f['forwardZ']))),travel))
        phase=float(m['phase']);clip=m['clip']
        for side,shift in [('left',0),('right',.55 if clip=='Sprint' else .5)]:
            p=(phase-shift)%1
            central={'Walk':(.20,.40),'Run':(.08,.16),'Sprint':(.10,.15)}.get(clip)
            if central and central[0]<=p<=central[1]:
                pos=point(m,side+'Toe')
                if not all(math.isfinite(x) for x in pos):errors.append('nonfinite sole marker');continue
                ground=float(m.get(side+'GroundY','nan'))
                if not math.isfinite(ground):errors.append('foot surface height unavailable')
                else:minimum_y=min(minimum_y,pos[1]-ground)
                contact_key=(step,side,clip,math.floor(phase-shift))
                contacts.setdefault(contact_key,[]).append(pos)
    cases=[];cycle_coverage=set();required_cycles=set()
    for key,g in groups.items():
        case=dict(name=key,samples=g['samples'],meanSpeed=sum(g['speed'])/len(g['speed']),
                  maxPelvisDegrees=max(abs(x) for x in g['pelvis']),
                  meanTorsoDegrees=average_angle(g['torso']),maxHeadingDegrees=max(abs(x) for x in g['heading']))
        if not all(math.isfinite(case[k]) for k in ['meanSpeed','maxPelvisDegrees','meanTorsoDegrees','maxHeadingDegrees']):errors.append(key+': invalid direction')
        if case['maxPelvisDegrees']>10:errors.append(key+': steady pelvis exceeds 10 degrees')
        complete=cycle_means(g['curve'])
        case['completeTorsoCycles']=complete
        required_cycles.add(g['identity'])
        if complete:cycle_coverage.add(g['identity'])
        if any(abs(mean)>15 for mean in complete):errors.append(key+': complete-cycle torso exceeds 15 degrees')
        cases.append(case)
    for missing in sorted(required_cycles-cycle_coverage):errors.append(' '.join(missing)+': no complete torso cycle; longer steady coverage required')
    drift=[]
    for key,points in contacts.items():
        if len(points)<2:continue
        maximum=max(math.hypot(a[0]-b[0],a[2]-b[2]) for a in points for b in points)
        drift.append(dict(step=key[0],side=key[1],clip=key[2],cycle=key[3],samples=len(points),driftMetres=maximum))
        if maximum>.05:errors.append(route['steps'][key[0]]['name']+': planted sole travel exceeds 5 cm')
    if not cases:errors.append('no steady free-traversal coverage')
    if not drift:errors.append('no repeated central-stance samples')
    if minimum_y<-.03:errors.append('sole marker penetrates measured surface by more than 3 cm')
    result.update(valid=not errors,failures=sorted(set(errors)),motionCases=cases,
                  retainedSteadySamples=sum(c['samples'] for c in cases),recordedMotionSamples=len(motion),
                  centralContacts=len(drift),worstContactDriftMetres=max((c['driftMetres'] for c in drift),default=None),
                  minimumCentralSoleClearance=minimum_y if math.isfinite(minimum_y) else None,
                  worstContacts=sorted(drift,key=lambda c:c['driftMetres'],reverse=True)[:12],
                  motionNote='Free traversal only, after 0.5 s and outside Animator transitions. Step-window torso means are diagnostic; acceptance integrates only complete contiguous cycles, requiring coverage of every recorded hero/form/clip. Marker positions are calibrated bone transforms, not independent rendered geometry. Unsupported/absent contact coverage is rejected; watched presentation is a separate gate.')
    return result


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('folder',type=Path);args=parser.parse_args()
    folder=args.folder
    result=analyze_motion(json.loads((folder/'run.json').read_text()),list(csv.DictReader((folder/'frames.csv').open())),
                          json.loads((folder/'route.json').read_text()),list(csv.DictReader((folder/'motion.csv').open())))
    (folder/'motion-analysis.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k not in ('motionCases','stabilityWindows','worstFrames','interactions')},indent=2))
    raise SystemExit(0 if result['valid'] else 1)
