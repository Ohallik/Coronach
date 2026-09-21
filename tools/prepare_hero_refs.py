"""Production identities implementing Nathan's approved facial-distinction note."""
import json
from pathlib import Path
from art_catalog import ROOT, STYLE, SINGLE, VAEL, POSE

IDENTITIES={
 'taren-natural': ('Taren', 'art-src/Generated/P1/refs/taren-board.png',
 "Taren Vosk, adult male Vael hero in NATURAL form. Use the attached board for species, wraps and palette, but REFINE his face into a specific new identity: broad short rectangular midface, strong blunt square chin, broad low-bridge nose with one subtle old healed nick, close-set hooded amber ring eyes, straight heavy brow ridges with a small notch in his LEFT brow ridge, a broad slightly crooked closed mouth. Face width is 0.76-0.82 face length; eye aperture width-to-height 2.6-3.0. His expression is calmly watchful with a hint of dry amusement. No angry generic symmetrical mannequin face. Short thick swept-back dark slate keratin fronds #293847 with copper tips #C98254. Matte slate skin #677887 and lighter face #93A2AD. Sparse bold branching amber-gold sync-lines #FFB62E, charcoal underlayer #283541, dusty asymmetric wraps #9C8C76 and copper sash #C98254. Shoulder width 1.25-1.32 hip width, waist 0.88-0.95 hip width; sturdy blade-fighter body, 1.9m. No armour, visor, weapons, vanes or glow effects in this Natural reference. The face must be recognizably broader and blunter than the approved generic species face."),
 'sela-natural': ('Sela', 'art-src/Generated/P1/refs/sela-board-v2.png',
 "Sela Irun, adult female Vael hero in NATURAL form. Use the attached board for species, plume-mane, wraps and palette, but REFINE her face into a distinct identity: long diamond-shaped face with high angular cheek ridges, narrow tapered jaw and small off-center cleft in the chin, longer low-bridge nose with a softly upturned tip, wide-set narrow cyan ring eyes, sharply slanted asymmetric brow ridges (her RIGHT brow slightly raised), thin upper lip and fuller lower lip, a crooked closed half-smile. Three small darker slate pigment speckles under her LEFT eye provide a stable unique marking. Face width 0.64-0.71 face length; eye aperture width-to-height 2.7-3.1. No generic perfectly symmetrical doll face. Pale violet-white flat keratin plume-mane #D7CFF0 with lilac roots #9C8DC1, longer and finer than Taren but clearly thick fronds, not human hair. Matte slate-blue skin #718A9B and face #A2B7C4, fine branched cyan sync-lines #22F5FF. Fitted underlayer #334756, muted sand wraps and lilac sash #AE9CCF. Shoulder width 1.02-1.10 hip width, waist 0.86-0.93 hip width, 1.9m, practical athletic anatomy. No armour, visor, weapons, vanes or glow effects in this Natural reference. Keep long fronds behind shoulders and clear of arms. Her identity must contrast strongly with Taren's short broad face.")
}
path=ROOT/'docs/art/design-catalog.json';rows=json.loads(path.read_text(encoding='utf-8'))
for name,(character,reference,identity) in IDENTITIES.items():
 prompt='\n'.join([STYLE,SINGLE,VAEL,identity,POSE,'IMPORTANT: exactly one complete front A-pose figure on opaque white. Do not reproduce the multi-view board. Four fingers and one thumb clearly separated on BOTH hands.'])
 row=dict(id=name,category='hero',mesh_input=True,character=character,form='Natural',prompt=prompt,references=[reference],source=f'art-src/Generated/P1-production/refs/{name}.png',prompt_path=f'docs/art/prompts/{name}.md')
 rows=[r for r in rows if r['id']!=name]+[row]
 (ROOT/row['prompt_path']).write_text(prompt+'\n',encoding='utf-8')
 (ROOT/f'art-src/Generated/P1-production/refs/{name}.prompt.txt').write_text(prompt+'\n',encoding='utf-8')
path.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8');print(json.dumps(rows))
