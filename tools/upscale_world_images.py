"""Production resizing of generated panoramas/maps; retain every native source unchanged."""
import json,shutil,subprocess
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
EXE=Path('C:/Users/natem/Projects/FrostboundUnity/art-src/realesrgan/realesrgan-ncnn-vulkan.exe')
names=['halo-sky','gullet-sky','tallow-sky','vorun-map','sorrel-map']
work=ROOT/'art-src/Generated/P3-world-upscale';native=work/'native';up=work/'4x';native.mkdir(parents=True,exist_ok=True);up.mkdir(exist_ok=True)
runtime=ROOT/'Lattice/Assets/_Project/Art/Generated/Textures';rows=[]
for name in names:
 source=native/(name+'.png')
 if not source.exists():shutil.copy2(runtime/(name+'.png'),source)
 target=up/(name+'.png')
 if not target.exists():
  with (work/(name+'.log')).open('w',encoding='utf-8') as log:
   subprocess.run([str(EXE),'-i',str(source),'-o',str(target),'-m',str(EXE.parent/'models'),'-n','realesrgan-x4plus-anime','-s','4','-t','256','-f','png'],check=True,timeout=300,stdout=log,stderr=subprocess.STDOUT)
 with Image.open(source) as im:dimensions=im.size
 with Image.open(target) as im:
  assert im.size==(dimensions[0]*4,dimensions[1]*4),(name,im.size)
  im.resize((4096,2048),Image.Resampling.LANCZOS).save(runtime/(name+'.png'))
 rows.append(dict(id=name,native=str(source.relative_to(ROOT)).replace('\\','/'),nativeSize=dimensions,upscale='Real-ESRGAN x4plus-anime 4x; Lanczos to 4096x2048',runtimeSize=[4096,2048]))
 print('WORLD_IMAGE_UPSCALED '+name,flush=True)
(ROOT/'docs/art/world-upscale.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('WORLD_UPSCALE_OK count='+str(len(rows)))
