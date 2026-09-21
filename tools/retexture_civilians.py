"""Sequential, resumable paid NPC variants; preserve base UV and rig exactly."""
import json,subprocess,sys,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
catalog=json.loads((ROOT/'docs/art/design-catalog.json').read_text(encoding='utf-8'))
def run(args,label,timeout=3900):
 print(label,flush=True)
 result=subprocess.run(args,cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=timeout)
 path=ROOT/'Builds/logs'/('npc-'+label+'.log');path.write_text(result.stdout+result.stderr,encoding='utf-8')
 if result.returncode:raise RuntimeError(str(path))
for base in ('CivilianMale','CivilianFemale'):
 directory=ROOT/'art-src/Generated'/('P3-'+base)
 for source,dest in ((base+'_Meshy.fbx',base+'_UV.glb'),(base+'_Rigged.fbx',base+'_clean.fbx')):
  if not (directory/dest).is_file():
   run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File','scripts/blender.ps1','-Script','tools/blender/p64_strip_embedded.py','--input',str(directory/source),'--output',str(directory/dest)],base+'-'+Path(dest).suffix[1:],650)
results=[]
for name,base,key in [('Orrin','CivilianMale','green'),('Hal','CivilianMale','blue'),('Mira','CivilianFemale','orange'),('Neve','CivilianFemale','white')]:
 row=next(r for r in catalog if r['id']==name.lower())
 palette={
 'Orrin':'Mature dockmaster. Short stone-grey keratin plumes #7C858D; slate skin #667681, lighter face #98A5AA, green eyes and thin dim-green branching sync-lines #49BE87. Deep navy underlayer #26384A, slate-beige dock wraps #8C8C7D, muted green sash #487764. Heavy low brow, tired hooded eyes.',
 'Hal':'Older gentle repairer. Ivory-grey keratin plumes #C9C9B9; warm-grey skin #7A8284, light face #A5ABAA, blue eyes and thin blue branching sync-lines #388DFF. Charcoal underlayer #303D46, weathered ochre workshop wraps #A49368, blue sash #507DA0. Gentle deep-set eyes, subtle cheek folds.',
 'Mira':'Alert female outfitter. Rust-red keratin plumes #A34D36; slate skin #7E838C, light face #ACAFB2, orange eyes and thin orange branching sync-lines #FF913A. Cream-beige shop wraps #B4AA94, muted terracotta sash #AD725C, dark slate underlayer. Heart-shaped face, asymmetric friendly smile.',
 'Neve':'Female courier. Black keratin plumes #242A37; cool slate skin #727E91, light face #A1AAB6, silver eyes and thin white branching sync-lines. Indigo underlayer #283952, weathered pale-grey travel wraps #9CA6B0, muted plum sash #71657F. Narrow alert eyes, subtle skeptical smile.'}[name]
 prompt='LATTICE cel-shaded anime Vael alien. Keep original UVs, anatomy and garment boundaries. Flat matte colors; no gloss, baked light, shadows, text or scenery. Thick keratin fronds, no hair. Keep facial features legible. '+palette
 assert len(prompt)<=800
 prompt_path=ROOT/'docs/art/prompts'/('retexture-'+name+'.md');prompt_path.write_text(prompt+'\n',encoding='utf-8')
 folder=ROOT/'art-src/Generated'/('P3-'+name);folder.mkdir(parents=True,exist_ok=True)
 if not (folder/(name+'_Meshy.fbx')).is_file():
  run([sys.executable,'tools/meshy/meshy.py','retexture','--model',str(ROOT/'art-src/Generated'/('P3-'+base)/(base+'_UV.glb')),'--prompt',prompt,'--batch','P3-'+name,'--prompt-id','LATTICE_P3','--name',name+'_Meshy'],name+'-retexture')
 shutil.copy2(ROOT/'art-src/Generated'/('P3-'+base)/(base+'_clean.fbx'),folder/(name+'_clean.fbx'))
 run([sys.executable,'tools/model_intake.py','--name',name,'--batch','P3-'+name,'--kind','biped','--folder','Characters','--size','1.9','--character',name,'--form','Natural','--key',key],name+'-intake',60)
 results.append(dict(name=name,base=base,status='ok',prompt=prompt_path.relative_to(ROOT).as_posix()))
 (ROOT/'docs/art/civilian-retexture-results.json').write_text(json.dumps(results,indent=2)+'\n',encoding='utf-8')
print('CIVILIAN_RETEXTURES_OK count=4',flush=True)
