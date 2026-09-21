"""Copy a generated source and its exact brief; no image painting or synthesis."""
import argparse, json, shutil
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--id',required=True);p.add_argument('--source',required=True)
p.add_argument('--revision',default='');p.add_argument('--notes',required=True);p.add_argument('--accepted',action='store_true')
p.add_argument('--prompt-file');a=p.parse_args()
rows=json.loads((ROOT/'docs/art/design-catalog.json').read_text(encoding='utf-8'))
row=next(r for r in rows if r['id']==a.id)
name=a.id+('-'+a.revision if a.revision else '')
folder=ROOT/('art-src/Generated/P1-production/refs' if a.accepted else 'art-src/Rejected/P1-production')
folder.mkdir(parents=True,exist_ok=True);dest=folder/(name+'.png')
if dest.exists(): raise SystemExit('Refuse overwrite: '+str(dest))
shutil.copy2(a.source,dest)
prompt=(ROOT/a.prompt_file).read_text(encoding='utf-8') if a.prompt_file else row['prompt']
prompt_path=folder/(name+'.prompt.txt');prompt_path.write_text(prompt,encoding='utf-8')
entry=dict(name=name,generator='session image_gen',source=dest.relative_to(ROOT).as_posix(),prompt=prompt_path.relative_to(ROOT).as_posix(),inspected=True,status='ACCEPTED_REFERENCE' if a.accepted else 'REJECTED_REFERENCE',native_resolution=list(Image.open(dest).size),notes=a.notes)
manifest=ROOT/'docs/art/image-manifest.json';existing=json.loads(manifest.read_text(encoding='utf-8'))
existing=[e for e in existing if e['name']!=name]+[entry];manifest.write_text(json.dumps(existing,indent=2)+'\n',encoding='utf-8')
if a.accepted:
 row['accepted_source']=dest.relative_to(ROOT).as_posix();row['qa']=a.notes
 (ROOT/'docs/art/design-catalog.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('IMAGE_ARCHIVE_OK '+str(dest))
