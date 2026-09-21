"""Derive the first paid task from an inspected, approved production reference."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
catalog=json.loads((ROOT/'docs/art/design-catalog.json').read_text(encoding='utf-8'))
row=next(r for r in catalog if r['id']=='taren-natural')
assert row.get('accepted_source') and row.get('qa')
entry=dict(name='TarenNatural',kind='biped',batch='P3-TarenNatural',ref=row['accepted_source'],height=1.9,family='Vael',prompt=row['prompt']+'\nProduction correction: '+(ROOT/'docs/art/prompts/taren-natural-correction.md').read_text(encoding='utf-8'))
(ROOT/'docs/art/pilot-batch.json').write_text(json.dumps([entry],indent=2)+'\n',encoding='utf-8')
# Preserve the exact pre-opaque-prefix briefs actually used by the two early enemy calls.
original="Use case: stylized-concept. LATTICE production design. Crisp cel-shaded anime game art with restrained flat local-color regions and readable silhouettes. Pure flat white background. Even unshadowed studio illumination. No gloss, specular highlights, reflections, rim light, cast shadows, ground shadows, ambient occlusion, bloom spill, scenery, floor, particles, text, labels, watermark, borders or detached decorative parts. Emissive lines are opaque saturated color, not illumination. Every appendage is visibly rooted in its body."
for name in ('sentinel-husk','ridgehound'):
 p=ROOT/f'art-src/Rejected/P1-production/{name}-rejected-alpha.prompt.txt'
 lines=p.read_text(encoding='utf-8').splitlines();lines[0]=original;p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
manifest=ROOT/'docs/art/image-manifest.json';images=json.loads(manifest.read_text(encoding='utf-8'))
for image in images:
 if image.get('status')=='NATHAN_GATE_PENDING':image['status']='NATHAN_SPECIES_APPROVED';image['notes']='Species approved; hero face refinement required and implemented in production references.'
manifest.write_text(json.dumps(images,indent=2)+'\n',encoding='utf-8')
print('PILOT_READY TarenNatural budget_cap=1200')
