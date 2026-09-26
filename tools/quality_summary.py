"""Summarize recorded artifacts without claiming visual/audio acceptance."""
import csv
import hashlib
import json
import math
import statistics
from pathlib import Path

root=Path(__file__).resolve().parents[1]
records=[]
for path in sorted((root/'Builds/quality/C0').glob('*/run.json')):
    folder=path.parent
    record=json.loads(path.read_text())
    frames=list(csv.DictReader((folder/'frames.csv').open()))
    record['path']=folder.relative_to(root).as_posix()
    record['artifacts']={name:{'bytes':(folder/name).stat().st_size,'sha256':hashlib.sha256((folder/name).read_bytes()).hexdigest()}
                         for name in ['frames.csv','route.json','video.mp4','mix.wav','replay.mp4'] if (folder/name).exists()}
    matrix=[]
    for hero in sorted({f['hero'] for f in frames}):
        for form in sorted({f['form'] for f in frames if f['hero']==hero}):
            for clip in ['Walk','Run','Sprint']:
                group=[f for f in frames if f['hero']==hero and f['form']==form and f['clip']==clip and float(f['reportedSpeed'])>.2]
                if not group: continue
                hip=[];chest=[]
                for f in group:
                    actor=math.degrees(math.atan2(float(f['forwardX']),float(f['forwardZ'])))
                    hip.append((float(f['pelvisYaw'])-actor+180)%360-180)
                    chest.append((float(f['chestYaw'])-actor+180)%360-180)
                matrix.append(dict(hero=hero,form=form,clip=clip,samples=len(group),medianSignedPelvisYaw=statistics.median(hip),medianSignedChestYaw=statistics.median(chest)))
    record['uncalibratedFacingVsActor']=matrix
    record['heroStates']=sorted({f['hero']+':'+f['state'] for f in frames})
    records.append(record)
out=dict(gate='C0_BASELINE_RECORDED',implementation='PASSED',automated='PASSED (recording routes; rejected attempts retained)',observedMotion='UNVERIFIED',audioAudition='UNVERIFIED',physicalController='UNVERIFIED',reviewer='Codex',
    note='Facing numbers use independent bilateral bone spans but are not yet visually calibrated. These are diagnostics, not C2 acceptance. Capture timings include encoder/readback and checkpoint PNG overhead.',runs=records)
dest=root/'docs/validation/C0-recordings.json';dest.write_text(json.dumps(out,indent=2)+'\n')
for r in records: print(r['route'],r['valid'],r['samples'],r['heroStates'])
