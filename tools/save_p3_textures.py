"""Stage image-tool outputs without changing image content; archive exact prompts."""
import json,shutil
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path('C:/Users/natem/.codex/generated_images/01a0c14a-c45a-7961-bd43-e4ef0c191121')
ROWS={
 'sorrel-ground':'exec-9d3bd07b-2da1-440d-91cc-6ba231f5a12d.png',
 'sorrel-rock':'exec-b350cfc5-6e67-4e5b-b3c8-65d2b349a15f.png',
 'deck-panels':'exec-860e4050-362f-48d7-afd6-335dbf6ed596.png',
 'gullet-membrane':'exec-6a89085c-059d-4d75-b34c-ef5522bb8268.png',
 'vorun-map':'exec-b808d8eb-d036-4a18-ac6a-9b3afb03b09c.png',
 'sorrel-map':'exec-e7b13d5a-7923-4c2d-8088-defec8b3f066.png',
 'halo-sky':'exec-4ce675fe-2933-4bea-a717-e0f7e0806cda.png',
 'gullet-sky':'exec-fe7f6c2b-bbc4-4295-bf4d-7e2bec36a278.png',
 'tallow-sky':'exec-00467c50-4bb2-4fd2-ac68-019caf371d69.png',
}
manifest_path=ROOT/'docs/art/image-manifest.json'
manifest=json.loads(manifest_path.read_text(encoding='utf-8'))
for name,source in ROWS.items():
 ref=ROOT/f'art-src/Generated/P3/refs/{name}.png';runtime=ROOT/f'Lattice/Assets/_Project/Art/Generated/Textures/{name}.png'
 runtime.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(SOURCE/source,ref);shutil.copy2(ref,runtime)
 prompt=ROOT/f'art-src/Generated/P3/refs/{name}.prompt.txt';shutil.copy2(prompt,ROOT/f'docs/art/prompts/{name}.md')
 size=Image.open(ref).size
 entry={'name':name,'generator':'session image_gen','source':str(ref.relative_to(ROOT)).replace('\\','/'),'runtime':str(runtime.relative_to(ROOT)).replace('\\','/'),
        'prompt':str(prompt.relative_to(ROOT)).replace('\\','/'),'inspected':True,'native_resolution':list(size),
        'notes':'Native generated output preserved. In-engine repeat/wrap inspection remains required.'}
 manifest=[row for row in manifest if row['name']!=name]+[entry]
 print(name,size)
manifest_path.write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print('TEXTURE_ARCHIVE_OK')
