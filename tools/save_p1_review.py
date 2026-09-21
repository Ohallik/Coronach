from pathlib import Path
import shutil,json
R=Path(__file__).resolve().parents[1]
S=Path('C:/Users/natem/.codex/generated_images/01a0c14a-c45a-7961-bd43-e4ef0c191121')
refs=R/'art-src/Generated/P1/refs'
images={
 'species-sheet-v2':'exec-8b1a539c-186e-4c62-8ad0-1ee9111efa0f.png',
 'species-faces-v2':'exec-506e1e21-7fb7-4975-b30b-5ab3c32af6ed.png',
 'taren-board':'exec-89a8609d-74aa-4fc2-8edc-f554a4203c55.png',
 'sela-board-v2':'exec-031713a4-73a4-4123-a656-03f11e07a1cf.png'}
manifest=R/'docs/art/image-manifest.json'
rows=json.loads(manifest.read_text(encoding='utf-8'))
for name,file in images.items():
    shutil.copy2(S/file,refs/(name+'.png'))
    rows.append({'name':name,'generator':'session image_gen','source':f'art-src/Generated/P1/refs/{name}.png','prompt':f'art-src/Generated/P1/refs/{name}.prompt.txt','inspected':True,'status':'NATHAN_GATE_PENDING'})
manifest.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('P1_REVIEW_IMAGES_SAVED count=4')
