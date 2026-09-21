"""Archive generated outputs, preserve prompts, record provenance; no art synthesis."""
import json
import shutil
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path('C:/Users/natem/.codex/generated_images/01a0c14a-c45a-7961-bd43-e4ef0c191121')
ROWS={
    'ui-panel':'exec-df8275f5-4acf-41b5-8da4-f18705fe338c.png',
    'item-icons':'exec-d34efb6a-4ea9-4b40-b4f6-2e7833943479.png',
    'skill-icons':'exec-bc552711-b02d-4f2a-95a7-56b07540b8b3.png',
    'ui-button':'exec-4beb442a-6a18-4963-810f-c6879c32491b.png',
    'ui-controls':'exec-feef6592-9fdc-4e5f-8bbf-ddfee41f8e55.png',
}
manifest_path=ROOT/'docs/art/image-manifest.json'
manifest=json.loads(manifest_path.read_text(encoding='utf-8'))
for name,source in ROWS.items():
    ref=ROOT/f'art-src/Generated/P2/refs/{name}.png'
    runtime=ROOT/f'Lattice/Assets/_Project/Resources/UI/Generated/{name}.png'
    runtime.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(SOURCE/source,ref);shutil.copy2(ref,runtime)
    entry={'name':name,'generator':'session image_gen','source':str(ref.relative_to(ROOT)).replace('\\','/'),
           'prompt':f'art-src/Generated/P2/refs/{name}.prompt.txt','runtime':str(runtime.relative_to(ROOT)).replace('\\','/'),
           'inspected':True,'notes':'Atlas contents and separation inspected; sprite rectangles assembled in Unity.'}
    manifest=[m for m in manifest if m['name']!=name]+[entry]
    im=Image.open(ref)
    print(name,im.size,im.mode,'bbox',im.getbbox(),'alpha',im.getextrema()[-1] if im.mode=='RGBA' else 'opaque')
manifest_path.write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print('UI_ARCHIVE_OK')
