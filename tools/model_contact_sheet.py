"""Assemble already-rendered Unity evidence, without creating or altering game art."""
import json,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];folder=ROOT/'Builds/logs/renders'
rows=json.loads((ROOT/'docs/art/intake.json').read_text(encoding='utf-8'))
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
tiles=[]
for row in rows:
 source=folder/(row['id']+'-idle-3quarter.png')
 if not source.is_file():raise FileNotFoundError(source)
 im=Image.open(source).convert('RGB');im.thumbnail((288,320),Image.Resampling.LANCZOS)
 tile=Image.new('RGB',(310,366),'#2B3545');tile.paste(im,((310-im.width)//2,8));ImageDraw.Draw(tile).text((12,337),row['id'],font=font,fill='white');tiles.append(tile)
def sheet(part,columns):
 out=Image.new('RGB',(310*columns,366*math.ceil(len(part)/columns)),'#2B3545')
 for i,im in enumerate(part):out.paste(im,((i%columns)*310,(i//columns)*366))
 return out
sheet(tiles,8).save(folder/'contact_sheet.png')
for page in range(math.ceil(len(tiles)/12)):sheet(tiles[page*12:(page+1)*12],4).save(folder/f'model-review-page-{page+1}.png')
print(f'MODEL_CONTACT_OK count={len(rows)} pages={math.ceil(len(tiles)/12)}')
