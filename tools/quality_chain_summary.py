"""One-line verdict per quality-replay run folder: exit, validity, duration, focus, p95/max frame, final zone, end marker."""
import json,csv,sys,re
from pathlib import Path
for folder in sys.argv[1:]:
    f=Path(folder)
    run=json.loads((f/'run.json').read_text(encoding='utf-8-sig'))
    rows=list(csv.DictReader((f/'frames.csv').open(encoding='utf-8-sig')))
    ms=sorted(float(r['ms']) for r in rows)
    ex=json.loads((f/'process-exit.json').read_text(encoding='utf-8-sig'))['exitCode']
    log=(f/'player.log').read_text(encoding='utf-8',errors='ignore')
    end=[l for l in log.splitlines() if re.search(r'QUALITY_REPLAY_(OK|FAILED|REJECT)|QUALITY_.*FAIL',l)]
    st=json.loads((f/'final-state.json').read_text(encoding='utf-8-sig')) if (f/'final-state.json').exists() else {}
    print(f.name,'exit',ex,'valid',run['valid'],'s',round(run['seconds'],1),'samples',run['samples'],'unfocused',sum(r['focus']!='True' for r in rows),
          'p95',round(ms[int(.95*len(ms))],2),'max',round(ms[-1],1),'zone',st.get('zone'),'|',end[-1][:140] if end else 'no end marker')
