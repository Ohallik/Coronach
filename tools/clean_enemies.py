"""Prepare every enemy model. The quadruped preserves the donor unit contract."""
import json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def run(args):subprocess.run(args,cwd=ROOT,check=True,timeout=650)
rows=json.loads((ROOT/'docs/art/enemy-batch.json').read_text(encoding='utf-8'))
for row in rows:
 name=row['name'];folder=ROOT/'art-src/Generated'/row['batch'];source=folder/(name+('_Static.fbx' if row['kind']=='static' else '_Rigged.fbx'))
 if name=='SentinelHusk' and (folder/(name+'_RepairRig.fbx')).is_file():
  run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File','scripts/blender.ps1','-Script','tools/blender/rebind_sentinel.py','--input',str(folder/(name+'_RepairRig.fbx')),'--output',str(folder/(name+'_clean.fbx'))])
  continue
 args=['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File','scripts/blender.ps1','-Script','tools/blender/p64_strip_embedded.py','--input',str(source),'--output',str(folder/(name+'_clean.fbx'))]
 if name=='Ridgehound':args+=['--scale-all']
 run(args)
 if name=='Ridgehound':continue
 size,fit=(row['height'],'height')
 if name=='ChoristerDart':size,fit=1.6,'length'
 if name=='Burrower':size,fit=5.5,'length'
 if name.startswith('Cantor'):size,fit={'CantorHead':3.8,'CantorBody':2.6,'CantorTail':3.0}[name],'length'
 args=[sys.executable,'tools/model_intake.py','--name',name,'--batch',row['batch'],'--kind',row['kind'],'--folder','Enemies','--size',str(size),'--fit',fit,'--enemy','Cantor' if name.startswith('Cantor') else name,'--key','amber' if name=='Scrapmite' else 'cyan']
 if name in ('CantorBody','CantorTail'):args+=['--form',name.removeprefix('Cantor')]
 run(args)
print('ENEMIES_CLEAN_OK count=10')
