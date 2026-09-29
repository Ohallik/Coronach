"""Cut a generated design board into isolated model references (free, local).

Cells are found as connected non-white regions (dilated so an object's own
parts stay together), ordered in reading order by row then column, and each
is centred on a white square with a margin. The board must hold exactly one
object per cell; a different count fails instead of guessing.

  python tools/isolate_board.py BOARD OUT_DIR Name1 Name2 ...
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

p = argparse.ArgumentParser()
p.add_argument('board'); p.add_argument('out'); p.add_argument('names', nargs='+')
p.add_argument('--size', type=int, default=1024); p.add_argument('--dilate', type=int, default=9)
p.add_argument('--min-area', type=int, default=4000)
a = p.parse_args()

image = Image.open(a.board).convert('RGB'); rgb = np.asarray(image)
ink = (rgb < 235).any(axis=2)
labels, count = ndimage.label(ndimage.binary_dilation(ink, iterations=a.dilate))
boxes = [(s, int((labels[s] > 0).sum())) for s in ndimage.find_objects(labels)]
boxes = [s for s, area in boxes if area >= a.min_area]
if len(boxes) != len(a.names):
    raise SystemExit(f'found {len(boxes)} objects, expected {len(a.names)}')
row_height = image.height / round(len(boxes) ** .5)
boxes.sort(key=lambda s: (int(((s[0].start + s[0].stop) / 2) // row_height), s[1].start))
out = Path(a.out); out.mkdir(parents=True, exist_ok=True)
for name, (ys, xs) in zip(a.names, boxes):
    crop = image.crop((xs.start, ys.start, xs.stop, ys.stop))
    side = int(max(crop.size) * 1.18)
    canvas = Image.new('RGB', (side, side), 'white'); canvas.paste(crop, ((side - crop.width) // 2, (side - crop.height) // 2))
    canvas.resize((a.size, a.size), Image.Resampling.LANCZOS).save(out / f'{name}.png')
    print('ISOLATED', name, crop.size)
