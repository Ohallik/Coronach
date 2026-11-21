"""Group original second-capture world screenshots for review; no game-art synthesis.

One sheet a zone, its three scripts/look.ps1 stills top to bottom, into <folder>/review.

    python tools/look_contact.py                                  # every zone with stills in Builds/logs/look/generated
    python tools/look_contact.py Builds/logs/look/xylos           # another look.ps1 -Folder
    python tools/look_contact.py --zones Rootmarket,BraidedRiver  # only these, in this order

With no --zones it takes every zone that has stills in the folder, in look.ps1's order: chapter 1's six, then each other
ZoneDef by its chapter and then its id. A zone with only some of its three stills stops the run: the look pass that took
them didn't finish.
"""
import re,sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]
# The six the look probe has hand-set viewpoints for, in the order their sheets have always been made. Nothing is added here.
FIRST=['Hub_CinderHalo','Hub_Decks','Sorrel_Ridges','Gullet_Tunnel','TallowApproach','TallowDrift']
args=sys.argv[1:];named=[]
if '--zones' in args:
 at=args.index('--zones');named=[z.strip() for z in args[at+1].split(',') if z.strip()];del args[at:at+2]
folder=ROOT/(args[0] if args else 'Builds/logs/look/generated')
def rank(zone):
 """A ZoneDef's chapter, one with none after those (the build list's order), and a name with no ZoneDef last."""
 asset=ROOT/'Lattice/Assets/_Project/Resources/Definitions/ZoneDef'/(zone+'.asset')
 if not asset.exists():return (2,0,zone)
 said=re.search(r'(?m)^\s*chapter:\s*(\d+)',asset.read_text(encoding='utf-8'));chapter=int(said.group(1)) if said else 0
 return (0,chapter,zone) if chapter>0 else (1,0,zone)
taken={p.name[:-6] for p in folder.glob('*-[0-2].png')}
zones=named or [z for z in FIRST if z in taken]+sorted((z for z in taken if z not in FIRST),key=rank)
if not zones:sys.exit(f'FAILED: no look stills in {folder}')
for zone in zones:
 missing=[f'{zone}-{i}.png' for i in range(3) if not (folder/f'{zone}-{i}.png').exists()]
 if missing:sys.exit(f'FAILED: {", ".join(missing)} missing from {folder}')
out=folder/'review';out.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
for zone in zones:
 sheet=Image.new('RGB',(960,1710),'#172332');draw=ImageDraw.Draw(sheet)
 for i in range(3):
  with Image.open(folder/f'{zone}-{i}.png') as im:sheet.paste(im.resize((960,540),Image.Resampling.LANCZOS),(0,i*570+30))
  draw.text((12,i*570+2),zone+f' / viewpoint {i+1}',font=font,fill='white')
 sheet.save(out/(zone+'.png'))
print(f'LOOK_CONTACT_OK zones={len(zones)} frames={3*len(zones)}')
