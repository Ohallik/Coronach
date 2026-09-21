import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];catalog=json.loads((ROOT/'docs/art/design-catalog.json').read_text(encoding='utf-8'))
rows=[]
for name,id,size in [('Hauler','hauler',12),('Skiff','skiff',6),('PatrolCutter','cutter',10)]:
 r=next(x for x in catalog if x['id']==id);assert r.get('accepted_source') and r.get('qa')
 rows.append(dict(name=name,kind='static',batch='P3-'+name,ref=r['accepted_source'],height=size,fit='length',family='Ship',prompt=r['prompt']))
(ROOT/'docs/art/ship-batch.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8');print('SHIP_BATCH_READY count=3')
