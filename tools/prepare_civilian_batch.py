import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];catalog=json.loads((ROOT/'docs/art/design-catalog.json').read_text(encoding='utf-8'))
rows=[]
for name,id in [('CivilianMale','orrin'),('CivilianFemale','mira')]:
 r=next(x for x in catalog if x['id']==id);assert r.get('accepted_source') and r.get('qa')
 rows.append(dict(name=name,kind='biped',batch='P3-'+name,ref=r['accepted_source'],height=1.9,family='Vael',prompt=r['prompt']))
(ROOT/'docs/art/civilian-batch.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8');print('CIVILIAN_BATCH_READY '+str(len(rows)))
