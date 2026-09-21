"""Contact sheets of captured dialogue screenshots, not new game artwork."""
from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[1]/'Builds/logs/portraits'
names=['Taren_Natural','Taren_Shaped','Sela_Natural','Sela_Shaped','Orrin_Natural','Mira_Natural','Hal_Natural','Neve_Natural']
for page in range(4):
 sheet=Image.new('RGB',(1600,680),(30,38,50));draw=ImageDraw.Draw(sheet)
 for index,name in enumerate(names[page*2:page*2+2]):
  for emotion_index,emotion in enumerate(('Neutral','Shocked')):
   source=Image.open(root/(name+'_'+emotion+'.png')).convert('RGB');assert source.size==(1920,1080)
   crop=source.crop((30,520,1890,1060));crop.thumbnail((800,305))
   x=emotion_index*800;y=index*340;sheet.paste(crop,(x,y+25));draw.text((x+10,y+5),name+' / '+emotion,fill='white')
 sheet.save(root/('dialogue-review-'+str(page+1)+'.png'))
print('PORTRAIT_CONTACT_OK count=16 pages=4')
