"""Prepare finished static batch outputs for the guarded Unity intake."""
import argparse,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--manifest',required=True);p.add_argument('--folder',choices=['Ships','Environment'],required=True);p.add_argument('--available',action='store_true');a=p.parse_args()
rows=json.loads((ROOT/a.manifest).read_text(encoding='utf-8'));count=0
for row in rows:
 name=row['name'];folder=ROOT/'art-src/Generated'/row['batch'];source=folder/(name+'_Static.fbx');clean=folder/(name+'_clean.fbx')
 if a.available and not source.is_file():continue
 if not source.is_file():raise FileNotFoundError(source)
 if not clean.is_file():subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File','scripts/blender.ps1','-Script','tools/blender/p64_strip_embedded.py','--input',str(source),'--output',str(clean)],cwd=ROOT,check=True,timeout=650)
 subprocess.run([sys.executable,'tools/model_intake.py','--name',name,'--batch',row['batch'],'--kind','static','--folder',a.folder,'--size',str(row['height']),'--fit',row.get('fit','height'),'--yaw',str(-90 if a.folder=='Ships' else row.get('yaw',0))],cwd=ROOT,check=True,timeout=60)
 count+=1
print('STATIC_BATCH_CLEAN_OK count='+str(count))
