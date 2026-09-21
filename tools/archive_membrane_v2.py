import argparse,json,shutil
from pathlib import Path
from PIL import Image
from emission_mask import extract
p=argparse.ArgumentParser();p.add_argument('--source',required=True);a=p.parse_args();root=Path(__file__).resolve().parents[1]
folder=root/'art-src/Generated/P3-texture-refine/refs';folder.mkdir(parents=True,exist_ok=True)
ref=folder/'gullet-membrane-v2.png';assert not ref.exists();shutil.copy2(a.source,ref)
prompt=folder/'gullet-membrane-v2.prompt.txt';shutil.copy2(root/'docs/art/prompts/gullet-membrane-v2.md',prompt)
runtime=root/'Lattice/Assets/_Project/Art/Generated/Textures/gullet-membrane.png';shutil.copy2(ref,runtime)
extract(runtime,runtime.with_name('gullet-membrane-emission.png'))
path=root/'docs/art/image-manifest.json';rows=json.loads(path.read_text(encoding='utf-8'))
rows.append(dict(name='gullet-membrane-v2',generator='session image_gen',source=ref.relative_to(root).as_posix(),runtime=runtime.relative_to(root).as_posix(),prompt=prompt.relative_to(root).as_posix(),native_resolution=list(Image.open(ref).size),inspected=True,notes='Opened: muted broad navy/violet cells with sparse dim cyan veins. Replaces the busy runtime membrane texture; original source retained. In-world look review pending.'))
path.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8');print('MEMBRANE_V2_ARCHIVED')
