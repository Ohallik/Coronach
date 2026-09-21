import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
catalog=json.loads((ROOT/'docs/art/design-catalog.json').read_text(encoding='utf-8'))+json.loads((ROOT/'docs/art/isolated-catalog.json').read_text(encoding='utf-8'))
rows=[]
for name,id,kind,size in [('SentinelHusk','sentinel-husk','biped',2.3),('Ridgehound','ridgehound','quadruped',2.4),('Scrapmite','scrapmite','static',.65),('ChoristerDart','chorister-dart','static',1.6),('ChoristerDrifter','chorister-drifter','static',2.0),('Shellmine','shellmine','static',1.0),('Burrower','burrower','static',3.0),('CantorHead','CantorHead','static',3.0),('CantorBody','CantorBody','static',2.2),('CantorTail','CantorTail','static',2.2)]:
 r=next(x for x in catalog if x['id']==id);assert r.get('accepted_source') and r.get('qa')
 row=dict(name=name,kind=kind,batch='P3-'+name,ref=r['accepted_source'],height=size,family='Enemy',prompt=r['prompt'])
 if kind=='quadruped':row.update(donor='C:/Users/natem/Projects/FrostboundUnity/art-src/Quaternius_UltimateAnimatedAnimals/FBX/Fox.fbx',fit='length')
 rows.append(row)
(ROOT/'docs/art/enemy-batch.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8');print('ENEMY_BATCH_READY '+str(len(rows)))
