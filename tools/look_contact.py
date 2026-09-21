"""Group original second-capture world screenshots for review; no game-art synthesis."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];folder=ROOT/'Builds/logs/look/generated';out=folder/'review';out.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
for zone in ['Hub_CinderHalo','Hub_Decks','Sorrel_Ridges','Gullet_Tunnel','TallowApproach','TallowDrift']:
 sheet=Image.new('RGB',(960,1710),'#172332');draw=ImageDraw.Draw(sheet)
 for i in range(3):
  with Image.open(folder/f'{zone}-{i}.png') as im:sheet.paste(im.resize((960,540),Image.Resampling.LANCZOS),(0,i*570+30))
  draw.text((12,i*570+2),zone+f' / viewpoint {i+1}',font=font,fill='white')
 sheet.save(out/(zone+'.png'))
print('LOOK_CONTACT_OK zones=6 frames=18')
