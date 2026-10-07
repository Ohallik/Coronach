"""Fail-closed traversal evidence validation; no dropped or winsorized timings."""
import argparse
import csv
import json
import math
from pathlib import Path

def percentile(values, fraction):
    values = sorted(values)
    return values[max(0, math.ceil(len(values) * fraction) - 1)]

def stability_windows(frames):
    """Retain all complete minutes, including warmup, for leak review.

    Counters with no provider stay unsupported. A rising allocated heap alone
    cannot establish a leak; compare object counts and repeated route phases.
    """
    result=[]
    for start in range(0,int(float(frames[-1]['elapsed']))-59,60):
        rows=[f for f in frames if start<=float(f['elapsed'])<start+60]
        window={'startSeconds':start,'endSeconds':start+60,'samples':len(rows)}
        for key in ('memoryBytes','sceneObjects','objects','audioVoices'):
            # The first ProfilerRecorder sample can be pending (zero) even
            # though a loaded scene plainly contains objects and memory.
            # Keep the raw sample and disclose the omitted counter count.
            minimum=0 if key=='audioVoices' else 1
            values=[int(f.get(key,-1)) for f in rows if int(f.get(key,-1))>=minimum]
            window[key]=dict(median=percentile(values,.5),maximum=max(values),minimum=min(values),
                validSamples=len(values),unavailableOrPendingSamples=len(rows)-len(values)) if values else 'UNSUPPORTED'
        result.append(window)
    return result

def analyze(run, frames, route, performance=False):
    errors = list(run.get('failures', []))
    if not run.get('valid'): errors.append('runtime validation rejected')
    if (run.get('width'), run.get('height')) != (1920,1080): errors.append('wrong resolution')
    # Steps that may end early (a condition, or a fight its outcome cuts short) do not count toward the length.
    duration = sum(s['seconds'] for s in route['steps'] if not s.get('until') and not s.get('stopWhen'))
    if len(frames) < duration * 20: errors.append('missing samples')
    if not frames: return {'valid':False, 'failures':errors+['empty trace']}
    times = [float(f['ms']) for f in frames]
    elapsed = [float(f['elapsed']) for f in frames]
    if elapsed[-1] < duration - .15: errors.append('route truncated')
    if any(b <= a for a,b in zip(elapsed,elapsed[1:])): errors.append('nonmonotonic trace')
    if abs(sum(times)/1000-elapsed[-1])>.05: errors.append('unaccounted frame intervals')
    if any(f['focus']!='True' for f in frames): errors.append('lost focus')
    def expected_menu_pause(frame):
        index = int(frame['step'])
        if not 0 <= index < len(route['steps']): return False
        observed = frame.get('ui')
        return observed in ('bench', 'pause', 'defeat') and route['steps'][index].get('pauseUi') == observed
    # A workshop may intentionally inspect a bench/menu or a defeat screen.
    # Retain its frames and require the actual UI observation to match the step.
    # Every clean performance mode still rejects any simulation pause.
    paused = [f for f in frames if f['paused'] != 'False']
    if any(performance or run.get('headroom') or not expected_menu_pause(f) for f in paused):
        errors.append('paused simulation')
    # A condition step (until/stopWhen) ends on its first frame when its condition
    # already holds; the runtime rejects an until that never does.
    required_steps={i for i,s in enumerate(route['steps']) if not s.get('until') and not s.get('stopWhen')}
    sampled={int(f['step']) for f in frames}
    if not required_steps<=sampled or not sampled<=set(range(len(route['steps']))): errors.append('checkpoint samples missing')
    positions=[(float(f['x']),float(f['z'])) for f in frames]
    distances=[math.dist(a,b) if frames[i]['scene']==frames[i+1]['scene'] and frames[i]['hero']==frames[i+1]['hero'] else 0 for i,(a,b) in enumerate(zip(positions,positions[1:]))]
    distance=sum(distances)
    if distance < 10: errors.append('stationary or insufficient traversal')
    for i,step in enumerate(route['steps']):
        rows=[f for f in frames if int(f['step'])==i]
        if not rows: continue
        expected=step.get('expectedScene') or route['scene']
        if rows[-1]['scene'] != expected: errors.append(step['name']+': wrong scene')
        if step.get('expectedCharacter') and rows[-1]['hero']!=step['expectedCharacter']:errors.append(step['name']+': wrong hero')
        if step.get('expectedForm') and rows[-1]['form']!=step['expectedForm']:errors.append(step['name']+': wrong body form')
        # Partner and target approaches have moving goals; the runtime records their arrival.
        if step.get('navigate') and not step.get('approachPartner') and not step.get('approachTarget'):
            target=(step['point']['x'],step['point']['z'])
            if min(math.dist((float(f['x']),float(f['z'])),target) for f in rows)>step.get('tolerance',.65)+.1:
                errors.append(step['name']+': checkpoint not reached')
    hitches={str(t):sum(x>t for x in times) for t in [25,33.3,50,100]}
    if performance:
        if run.get('frameCap')!=-1 or run.get('vSync')!=1 or not 59<=run.get('refreshHz',0)<=61:
            errors.append('ordinary timing requires synchronized presentation on the 60 Hz reference display')
        if run.get('captured'): errors.append('capture overhead disqualifies clean timing')
        if run.get('profiled'): errors.append('profiling overhead disqualifies clean timing')
        if duration<120: errors.append('timing route shorter than 120 seconds')
        if percentile(times,.95)>18.5: errors.append('p95 exceeds 18.5 ms')
        if percentile(times,.99)>25: errors.append('p99 exceeds 25 ms')
        if hitches['33.3']>1 or hitches['50']>0: errors.append('hitch budget exceeded')
        if 'interactions' in run:
            for i,step in enumerate(route['steps']):
                if step.get('expectedUi') not in ('dialogue','shop') or not step.get('buttons'):continue
                responses=[r for r in run['interactions'] if r['step']==i and r['expectedUi']==step['expectedUi']]
                if len(responses)!=1 or responses[0]['visibleResponseMs']<0:errors.append(step['name']+': UI feedback missing')
                elif responses[0]['visibleResponseMs']>100:errors.append(step['name']+': UI feedback exceeds 100 ms')
    moving=[]
    for i,d in enumerate(distances,1):
        dt=(elapsed[i]-elapsed[i-1])
        if dt<=0 or d/dt<2 or d/dt>6: continue
        a,b=frames[i-1],frames[i]
        cam=math.hypot(float(b['cameraX'])-float(a['cameraX']),float(b['cameraZ'])-float(a['cameraZ']))/dt
        moving.append(cam)
    return dict(valid=not errors, failures=sorted(set(errors)), samples=len(frames), seconds=elapsed[-1], distance=distance,
        medianMs=percentile(times,.5),p95Ms=percentile(times,.95),p99Ms=percentile(times,.99),worstMs=max(times),hitches=hitches,
        pausedSamples=len(paused),
        gcCollections=int(frames[-1]['gcCollections'])-int(frames[0]['gcCollections']),
        gcBytesPerFrameMedian=percentile([int(f['gcBytes']) for f in frames],.5),
        movingSamples=len(moving), stoppedCameraWhileWalking=sum(v<.02 for v in moving),
        interactions=run.get('interactions','UNMEASURED (legacy recorder)'),
        stabilityWindows=stability_windows(frames),
        worstFrames=[dict(elapsed=float(f['elapsed']),ms=float(f['ms']),checkpoint=route['steps'][int(f['step'])]['name'],
                          gcBytes=int(f['gcBytes']),gcCollections=int(f['gcCollections']),
                          mainThreadNs=int(f.get('mainThreadNs',-1)),renderThreadNs=int(f.get('renderThreadNs',-1)))
                     for f in sorted(frames,key=lambda f:float(f['ms']),reverse=True)[:8]],
        note='Main/render-thread columns include cap and presentation waits. They are not active CPU/GPU cost. Skeletal yaw requires visual calibration.')

def analyze_segments(run, frames, route, performance=False):
    segments=route.get('segments') or []
    if not segments: return analyze(run,frames,route,performance)
    result=analyze(run,frames,route,False)
    result['segments']=[]
    covered=[]
    for segment in segments:
        start=segment['firstStep'];end=start+segment['stepCount'];covered.extend(range(start,end))
        rows=[dict(f) for f in frames if start<=int(f['step'])<end]
        offset=float(rows[0]['elapsed'])-float(rows[0]['ms'])/1000 if rows else 0
        for row in rows:
            row['elapsed']=str(float(row['elapsed'])-offset);row['step']=str(int(row['step'])-start)
        segment_run=dict(run)
        if 'interactions' in run:segment_run['interactions']=[dict(r,step=r['step']-start) for r in run['interactions'] if start<=r['step']<end]
        part=analyze(segment_run,rows,dict(route,steps=route['steps'][start:end]),performance)
        result['segments'].append(dict(name=segment['name'],**part))
        result['failures'] += [segment['name']+': '+f for f in part['failures']]
    if sorted(covered)!=list(range(len(route['steps']))):result['failures'].append('segments omit or duplicate route steps')
    result['valid']=not result['failures']
    return result

def analyze_headroom(run, frames, route):
    result=analyze(run,frames,route,False)
    errors=result['failures']
    if not run.get('headroom'): errors.append('headroom counters were not requested')
    if run.get('frameCap',60)>0 or run.get('vSync',1)!=0: errors.append('headroom requires uncapped VSync-off traversal')
    if run.get('captured') or run.get('profiled'): errors.append('capture/profile overhead disqualifies headroom')
    if run.get('census'): errors.append('object census overhead disqualifies headroom')
    if not frames or float(frames[-1]['elapsed'])<30: errors.append('headroom window shorter than 30 seconds')
    counters={}
    # A completed frame cannot take longer than the entire recorded visit,
    # including load and settle. This only catches impossible counter values;
    # genuine long frames remain in both statistics and the acceptance check.
    visit_ms=(float(frames[-1]['elapsed'])+run.get('loadWaitSeconds',0)+run.get('settleSeconds',0))*1000 if frames else 0
    for key in ('activeCpuNs','activeRenderNs','gpuWorkNs'):
        values=[int(f.get(key,-1))/1e6 for f in frames if int(f.get(key,-1))>0]
        if not values:
            counters[key]='UNSUPPORTED'
            if key=='activeCpuNs': errors.append('active CPU work was not measured')
            continue
        impossible=[dict(elapsed=float(f['elapsed']),nanoseconds=int(f[key])) for f in frames if int(f.get(key,-1))/1e6>visit_ms]
        counters[key]=dict(samples=len(values),unavailableOrPendingSamples=len(frames)-len(values),
            p95Ms=percentile(values,.95),p99Ms=percentile(values,.99),worstMs=max(values),impossibleSamples=impossible)
        if impossible: errors.append(key+': impossible frame duration; counter evidence rejected')
        if len(values)<len(frames)*.9: errors.append(key+': insufficient valid timing samples')
        if percentile(values,.95)>14: errors.append(key+': p95 active work exceeds 14 ms')
    result['headroomCounters']=counters
    if run.get('frameTimingRequested'):
        api, comparison, api_errors = compare_frame_timing(run, frames, counters, visit_ms)
        result['frameTimingApi'] = api
        result['gpuComparison'] = comparison
        errors.extend(api_errors)
    result['valid']=not errors
    result['note']='FrameTiming work counters exclude CPU cap/presentation waits; asynchronous GPU samples are not assigned to a specific input frame. Unsupported counters remain explicit. Impossible durations reject the counter evidence; raw samples and all positive values remain in the statistics.'
    return result

def compare_frame_timing(run, frames, counters, visit_ms):
    """Retain and cross-check Unity's direct API, independent of recorder access.

    Both access paths use Unity's timing backend. This cannot independently
    certify the driver/GPU clock; it exposes disagreements or shared corruption.
    Frame timestamps deduplicate cached API returns, never GPU durations.
    """
    errors = []
    if not run.get('frameTimingEnabled'):
        errors.append('FrameTimingManager is disabled')
    seen = set()
    values = []
    impossible = []
    nonfinite = []
    repeated = pending = 0
    previous = 0
    for row in frames:
        timestamp = int(row.get('ftmTimestamp', 0))
        value = float(row.get('ftmGpuMs', -1))
        if not math.isfinite(value):
            nonfinite.append(dict(elapsed=float(row['elapsed']), value=str(value)))
            continue
        if timestamp <= 0 or value <= 0:
            pending += 1
            continue
        if timestamp < previous:
            errors.append('FrameTimingManager timestamps moved backward')
        previous = timestamp
        if timestamp in seen:
            repeated += 1
            continue
        seen.add(timestamp)
        values.append(value)
        if value > visit_ms:
            impossible.append(dict(elapsed=float(row['elapsed']), milliseconds=value, timestamp=timestamp))
    if nonfinite:
        errors.append('FrameTimingManager returned nonfinite GPU duration')
    if impossible:
        errors.append('FrameTimingManager: impossible frame duration; API evidence rejected')
    api = dict(samples=len(values), repeatedRows=repeated, unavailableOrPendingRows=pending,
               nonfiniteSamples=nonfinite, impossibleSamples=impossible)
    comparison = dict(status='UNVERIFIED')
    if not values:
        api['status'] = 'UNSUPPORTED'
        errors.append('FrameTimingManager GPU work was not measured')
    else:
        api.update(p95Ms=percentile(values, .95), p99Ms=percentile(values, .99), worstMs=max(values))
        if len(values) < len(frames) * .9:
            errors.append('FrameTimingManager: insufficient distinct valid timing samples')
        if api['p95Ms'] > 14:
            errors.append('FrameTimingManager: p95 GPU work exceeds 14 ms')
        profiler = counters.get('gpuWorkNs')
        if not isinstance(profiler, dict):
            errors.append('GPU source agreement unavailable: profiler GPU work missing')
        else:
            difference = abs(api['p95Ms'] - profiler['p95Ms'])
            tolerance = max(.25, .1 * max(api['p95Ms'], profiler['p95Ms']))
            disagreement = difference > tolerance or bool(impossible or nonfinite or profiler['impossibleSamples'])
            comparison = dict(status='DISAGREEMENT_OR_INVALID' if disagreement else 'AGREE',
                              profilerP95Ms=profiler['p95Ms'], apiP95Ms=api['p95Ms'],
                              differenceMs=difference, toleranceMs=tolerance)
            if disagreement:
                errors.append('GPU source agreement rejected; retain both raw sources')
    return api, comparison, errors

def analyze_census(rows, seconds):
    """Report actual object/source inventories; source counts are not voice counts.

    Growth is reviewed alongside the repeated route phase and memory trace,
    rather than declaring an arbitrary heap/object change a leak.
    """
    errors=[]
    if not rows: return dict(valid=False,failures=['missing object/source census'],windows=[])
    times=[float(r['seconds']) for r in rows]
    if times[0]>2 or seconds-times[-1]>7 or any(b-a>7 or b<=a for a,b in zip(times,times[1:])):
        errors.append('incomplete object/source census coverage')
    if any(int(r['sceneGameObjects'])<=0 or not 0<=int(r['loopingSources'])<=int(r['playingSources'])<=int(r['audioSources']) for r in rows):
        errors.append('invalid object/source inventory')
    windows=[]
    for start in range(0,int(seconds)-59,60):
        group=[r for r in rows if start<=float(r['seconds'])<start+60]
        window=dict(startSeconds=start,samples=len(group))
        for key in ('sceneGameObjects','audioSources','playingSources','loopingSources'):
            values=[int(r[key]) for r in group]
            window[key]=dict(minimum=min(values),median=percentile(values,.5),maximum=max(values)) if values else 'UNMEASURED'
        windows.append(window)
    return dict(valid=not errors,failures=errors,windows=windows,worstSamplingMs=max(float(r['samplingMs']) for r in rows),
                note='Includes inactive scene objects/sources. Playing sources may each contain multiple one-shot voices. Review growth at comparable warmed route phases; these inventories do not measure the audible mix.')

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

def launch_diagnostics(build):
    """Unity's native profiler can run without the replay's -quality-profile flag.

    Keep the recorded runtime flag intact; launch evidence is an additional
    source, not a reason to rewrite raw run.json or remove expensive frames.
    """
    arguments=build.get('arguments',[])
    if not isinstance(arguments,list) or any(not isinstance(value,str) for value in arguments):
        return dict(valid=False,failures=['launch manifest arguments malformed; clean timing/headroom rejected'])
    switches={'-profiler-enable','-profiler-log-file','-deepprofiling','-quality-profile','-quality-profile-segments'}
    found=sorted({value.lower() for value in arguments if value.lower() in switches})
    errors=[]
    if found or build.get('nativeProfilerDiagnostic') is True:
        errors.append('launch manifest declares profiling; clean timing/headroom rejected')
    elif '-quality-gpu-clocks' in {value.lower() for value in arguments}:
        errors.append('GPU clock diagnostic overhead disqualifies clean timing/headroom')
    elif build.get('cleanTimingEligible') is False:
        errors.append('launch manifest excludes clean timing/headroom')
    return dict(valid=not errors,failures=errors,profilingArguments=found,
                nativeProfilerDiagnostic=build.get('nativeProfilerDiagnostic',False))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('folder',type=Path);modes=p.add_mutually_exclusive_group();modes.add_argument('--performance',action='store_true');modes.add_argument('--headroom',action='store_true');args=p.parse_args()
    run=json.loads((args.folder/'run.json').read_text());route=json.loads((args.folder/'route.json').read_text())
    frames=list(csv.DictReader((args.folder/'frames.csv').open()))
    result=analyze_headroom(run,frames,route) if args.headroom else analyze_segments(run,frames,route,args.performance)
    if run.get('census'):
        path=args.folder/'census.csv'
        census=analyze_census(list(csv.DictReader(path.open())) if path.exists() else [],result['seconds'])
        result['objectSourceCensus']=census;result['failures']+=census['failures'];result['valid']=not result['failures']
    manifest=args.folder/'build.json'
    if manifest.exists():
        diagnostics=launch_diagnostics(json.loads(manifest.read_text(encoding='utf-8-sig')))
        result['launchDiagnostics']=diagnostics
        if args.performance or args.headroom:
            result['failures']+=diagnostics['failures'];result['valid']=not result['failures']
    (args.folder/'analysis.json').write_text(json.dumps(result,indent=2)+'\n');plot(frames,args.folder/'frame-times.svg')
    print(json.dumps(result,indent=2))
    raise SystemExit(0 if result['valid'] else 1)
