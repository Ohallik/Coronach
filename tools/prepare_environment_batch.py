import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];catalog=json.loads((ROOT/'docs/art/isolated-catalog.json').read_text(encoding='utf-8'));design=json.loads((ROOT/'docs/art/design-catalog.json').read_text(encoding='utf-8'))
sizes={'MoonGroundA':(16,'length'),'MoonGroundB':(16,'length'),'RidgeRock1':(4,'height'),'RidgeRock2':(3,'height'),'RidgeRock3':(5,'height'),'CrystalClusterA':(2,'height'),'CrystalClusterB':(2.5,'height'),'LatticeAnvil':(1.3,'height'),'OutpostHab':(6,'height'),'OutpostDrill':(8,'height'),'LandingPad':(10,'length'),'RingSegmentA':(16,'length'),'RingSegmentB':(16,'length'),'DockingPodOffice':(12,'length'),'DockingPodOutfitter':(12,'length'),'DockingPodRepair':(12,'length'),'WarpBeacon':(10,'height'),'DeckWall':(3.5,'height'),'DeckFloor':(4,'length'),'DeckDoorway':(3.4,'height'),'DeckConsole':(1.5,'height'),'DeckBench':(1.3,'height'),'DeckCrate':(1.1,'height'),'GulletWallA':(12,'height'),'GulletWallB':(12,'height'),'ChoirPod':(4,'height')}
rows=[]
for name,(size,fit) in sizes.items():
 r=next(x for x in catalog if x['id']==name);assert r.get('accepted_source') and r.get('qa')
 rows.append(dict(name=name,kind='static',batch='P3-'+name,ref=r['accepted_source'],height=size,fit=fit,family='Environment',prompt=r['prompt']))
r=next(x for x in design if x['id']=='tallow-environment-board');assert r.get('qa')
rows.append(dict(name='TallowStationHull',kind='static',batch='P3-TallowStationHull',ref=r['accepted_source'],height=60,fit='length',family='Environment',prompt=r['prompt']))
(ROOT/'docs/art/environment-batch.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8');print('ENVIRONMENT_BATCH_READY count='+str(len(rows)))
