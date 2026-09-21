"""Contact sheet of inspected Unity pose renders. Port of p64_contact_sheet.
Usage: python tools/contact_sheet.py Builds/logs/renders/*.png --out Builds/logs/renders/contact_sheet.png
This composes existing rendered evidence; it never substitutes for generation.
"""
from pathlib import Path
import argparse
import glob
from PIL import Image, ImageDraw, ImageFont

def main():
    p=argparse.ArgumentParser();p.add_argument('images',nargs='+');p.add_argument('--out',required=True)
    args=p.parse_args();paths=[]
    for pattern in args.images: paths.extend(Path(x) for x in glob.glob(pattern))
    paths=[x for x in paths if x.resolve()!=Path(args.out).resolve()]
    if not paths:raise SystemExit('FAILED: no source renders')
    tile_w,tile_h=420,450;cols=min(4,len(paths));rows=(len(paths)+cols-1)//cols
    canvas=Image.new('RGB',(cols*tile_w,rows*tile_h),(16,22,32));draw=ImageDraw.Draw(canvas)
    for i,path in enumerate(paths):
        source=Image.open(path).convert('RGB');source.thumbnail((400,400))
        x=(i%cols)*tile_w;y=(i//cols)*tile_h
        canvas.paste(source,(x+(tile_w-source.width)//2,y+10))
        draw.text((x+12,y+416),path.stem,fill=(230,240,255),font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18))
    out=Path(args.out);out.parent.mkdir(parents=True,exist_ok=True);canvas.save(out)
    print(f'CONTACT_SHEET_OK tiles={len(paths)} path={out}')
if __name__=='__main__':main()
