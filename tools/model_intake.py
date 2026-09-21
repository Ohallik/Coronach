"""Prepare derived data textures and an intake row from a cleaned generated model."""
import argparse,json
from pathlib import Path
from PIL import Image
from emission_mask import extract
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--name',required=True);p.add_argument('--batch',required=True);p.add_argument('--kind',choices=['biped','generic','static'],required=True)
p.add_argument('--folder',choices=['Characters','Enemies','Ships','Environment'],required=True);p.add_argument('--size',type=float,required=True);p.add_argument('--fit',default='height')
p.add_argument('--character');p.add_argument('--form');p.add_argument('--enemy');p.add_argument('--key',default='cyan');p.add_argument('--donor-clips');p.add_argument('--yaw',type=float,default=0);a=p.parse_args()
folder=ROOT/'art-src/Generated'/a.batch;model=folder/(a.name+'_clean.fbx');assert model.is_file(),model
sources=list(folder.glob(a.name+'_Meshy_base_color*'));assert len(sources)==1,sources
albedo=folder/(a.name+'_base_color.png');im=Image.open(sources[0]).convert('RGB');im.thumbnail((1024,1024),Image.Resampling.LANCZOS);im.save(albedo)
emission=folder/(a.name+'_emission.png');extract(albedo,emission,a.key)
row=dict(id=a.name,model=model.relative_to(ROOT).as_posix(),albedo=albedo.relative_to(ROOT).as_posix(),emission=emission.relative_to(ROOT).as_posix(),kind=a.kind,folder=a.folder,size=a.size,fit=a.fit)
row['emissionColor']={'cyan':'#22F5FF','amber':'#FFB62E','orange':'#FF8046','green':'#49BE87','blue':'#408BFF','white':'#FFFFFF'}[a.key];row['yaw']=a.yaw
for key in ('character','form','enemy'):
 if getattr(a,key):row[key]=getattr(a,key)
if a.kind=='biped':
 clips=json.loads((ROOT/'docs/art/animation-clips.json').read_text(encoding='utf-8'))
 row['clips']=[]
 for state,suffix in [('Idle','Idle_Loop'),('Walk','Walk_Loop'),('Attack','Sword_Attack')]:
  matches=[c for c in clips if c['name'].split('|')[-1]==suffix];assert len(matches)==1
  row['clips'].append(dict(state=state,path=matches[0]['path'],name=matches[0]['name']))
elif a.kind=='generic':
 assert a.donor_clips,'Generic row requires the inspected donor clip mapping.'
 clips=json.loads((ROOT/a.donor_clips).read_text(encoding='utf-8'));row['clips']=[]
 for state in ('Idle','Walk','Attack'):
  matches=[c for c in clips if c['name'].split('|')[-1]==state];assert len(matches)==1
  row['clips'].append(dict(state=state,path=matches[0]['path'],name=matches[0]['name']))
path=ROOT/'docs/art/intake.json';rows=json.loads(path.read_text(encoding='utf-8'));rows=[r for r in rows if r['id']!=a.name]+[row];path.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('INTAKE_ROW_READY '+a.name)
