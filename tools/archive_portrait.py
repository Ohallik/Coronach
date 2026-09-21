"""Archive an image-tool portrait sheet and its reviewed cell mapping."""
import argparse,json,shutil
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--name',required=True);p.add_argument('--source',required=True);p.add_argument('--notes',required=True);a=p.parse_args()
catalogpath=ROOT/'docs/art/portrait-catalog.json';catalog=json.loads(catalogpath.read_text(encoding='utf-8'));row=next(r for r in catalog if r['name']==a.name)
folder=ROOT/'art-src/Generated/P3-portraits/refs';folder.mkdir(parents=True,exist_ok=True);dest=folder/(a.name+'.png')
if dest.exists():raise RuntimeError('Refuse overwrite '+str(dest))
shutil.copy2(a.source,dest);promptpath=dest.with_suffix('.prompt.txt');promptpath.write_text(row['prompt'],encoding='utf-8')
row.update(source=dest.relative_to(ROOT).as_posix(),enum_to_source=list(range(16)),qa=a.notes);catalogpath.write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8')
manifest=ROOT/'docs/art/image-manifest.json';rows=json.loads(manifest.read_text(encoding='utf-8'));rows.append(dict(name='portrait-'+a.name,generator='session image_gen',source=row['source'],prompt=promptpath.relative_to(ROOT).as_posix(),inspected=True,status='ACCEPTED_PORTRAIT',native_resolution=list(Image.open(dest).size),notes=a.notes));manifest.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('PORTRAIT_ARCHIVE_OK '+str(dest))
