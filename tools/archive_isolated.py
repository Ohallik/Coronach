"""Archive a reviewed individual 3D reconstruction reference and exact prompt."""
import argparse,json,shutil
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--id',required=True);p.add_argument('--source',required=True);p.add_argument('--notes',required=True);p.add_argument('--revision',default='');p.add_argument('--prompt-file');a=p.parse_args()
catalogpath=ROOT/'docs/art/isolated-catalog.json';catalog=json.loads(catalogpath.read_text(encoding='utf-8'));row=next(r for r in catalog if r['id']==a.id)
folder=ROOT/'art-src/Generated/P3-isolated/refs';folder.mkdir(parents=True,exist_ok=True);name=a.id+('-'+a.revision if a.revision else '');dest=folder/(name+'.png')
if dest.exists():raise RuntimeError('Refuse overwrite '+str(dest))
shutil.copy2(a.source,dest);promptpath=dest.with_suffix('.prompt.txt');promptpath.write_text((ROOT/a.prompt_file).read_text(encoding='utf-8') if a.prompt_file else row['prompt'],encoding='utf-8')
row.update(accepted_source=dest.relative_to(ROOT).as_posix(),qa=a.notes);catalogpath.write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8')
manifest=ROOT/'docs/art/image-manifest.json';rows=json.loads(manifest.read_text(encoding='utf-8'));rows.append(dict(name='model-'+name,generator='session image_gen',source=row['accepted_source'],prompt=promptpath.relative_to(ROOT).as_posix(),inspected=True,status='ACCEPTED_REFERENCE',native_resolution=list(Image.open(dest).size),notes=a.notes));manifest.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('ISOLATED_ARCHIVE_OK '+str(dest))
