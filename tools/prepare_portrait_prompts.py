"""Author eight identity-locked expression-sheet prompts before image generation."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
catalog=json.loads((ROOT/'docs/art/design-catalog.json').read_text(encoding='utf-8'))
identities={
 'Taren':('taren','broad square jaw, broad low nose with healed diagonal bridge scar, hooded amber ring eyes, small left brow notch, wide slightly crooked mouth; dark slate fronds with copper tips, amber-gold sync-lines'),
 'Sela':('sela','long angular face, high expressive brow ridges with one raised brow, narrow soft low nose, asymmetrical confident smile, three dark pigment speckles beneath HER LEFT eye; tall pale lavender fronds and cyan sync-lines'),
 'Orrin':('orrin','mature broad rectangular face, heavy tired hooded green eyes, flattened nose, square jaw, short stone-grey fronds, dim green sync-lines, navy and beige dockmaster wraps'),
 'Mira':('mira','softly rounded face, full expressive cheeks, russet keratin fronds, orange sync-lines, sand and aubergine outfitter wraps, lively amused temperament'),
 'Hal':('hal','lean mature face with strong cheekbones and deep-set eyes, pale keratin fronds with a small gap, blue sync-lines, ochre repair-worker wraps, restrained patient temperament'),
 'Neve':('neve','narrow confident face, long slanted almond eyes, short swept black keratin fronds, white sync-lines, blue-grey pilot wraps, sly composed temperament')}
layout='''Use case: stylized-concept. LATTICE dialogue portrait atlas. Produce exactly ONE square sheet containing FOUR equal-width columns and FOUR equal-height rows of head-and-shoulders portraits of the SAME adult Vael alien, sixteen separate cells total. Crisp professional cel-shaded anime line art, restrained flat colors, soft simple shading, no photorealism. Opaque pure white #FFFFFF background in every cell, no panel dividers, text, labels, numbers, borders, symbols, scenery, hands, extra figures or transparency. Each portrait is centered in its own quarter-width cell, complete crown/fronds and both shoulders within the cell, with 8-12 percent empty white margin all around. Faces all at matching scale, same character identity, same outfit and exact same plume shape and skin markings. Slate-blue matte skin, lighter face and throat, elongated swept-back cranium, flat broad keratin FRONDS rather than hair, no external ears or lobes, no human hair, no furry eyebrows. Expressiveness comes from eyes, fleshy brow ridges, cheeks and mouth. Readable pale sclera and ring irises. Sync markings remain thin saturated flat local colors, no bloom.
Read cells left to right, top to bottom. The sixteen expressions MUST be in this exact order:
Row 1: Neutral (calm closed mouth); Happy (warm open smile); Sad (downturned mouth and eyelids); Angry (knitted brows, clenched teeth).
Row 2: Shocked (very wide eyes and clearly open round mouth); Worried (pinched raised inner brows, tight mouth); Intense (sharp fixed stare, serious mouth); Synced (serene concentration, eyes bright and markings vivid).
Row 3: Amused (knowing half-smile); Smug (one raised brow, self-satisfied crooked grin); Curious (questioning head tilt, attentive eyes); Flustered (averted eyes, embarrassed uneven smile, subtle skin blush only).
Row 4: Tender (soft affectionate eyes and gentle closed smile); Determined (firm jaw and focused brows); Playful (one eye wink, broad mischievous grin); Special (joyful uninhibited laugh, eyes squeezed closed).
Do not rearrange or merge cells. No sweat drops, floating hearts, thought bubbles or decorative expression marks. Full square atlas 2048x2048 or largest native square.
'''
rows=[]
for name,(base,identity) in identities.items():
 for form in (['Natural','Shaped'] if name in ('Taren','Sela') else ['Natural']):
  refid=base+'-'+form.lower() if name in ('Taren','Sela') else base
  design=next(r for r in catalog if r['id']==refid)
  prompt=layout+'\nCharacter: '+name+'. Preserve the attached reference identity and palette. '+identity+'.\n'
  prompt+=('NATURAL form: wear the same muted asymmetric fabric wraps and collar node as the reference, no armor or visor.\n' if form=='Natural' else 'SHAPED form: same biological face, same ivory combat gorget and shoulder armor as reference; thin translucent colored visor lens preserves readable eyes and eyebrows, mouth and jaw fully visible; do not cover expression with a helmet.\n')
  ident=name+'_'+form;path=ROOT/'docs/art/prompts'/('portrait-'+ident+'.md');path.write_text(prompt,encoding='utf-8')
  rows.append(dict(name=ident,character=name,form=form,reference=design['accepted_source'],prompt_file=path.relative_to(ROOT).as_posix(),prompt=prompt))
(ROOT/'docs/art/portrait-catalog.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('PORTRAIT_PROMPTS_READY '+str(len(rows)))
