"""Run the explicitly requested 4x Real-ESRGAN portrait intake after visual cell mapping.
Example: python tools/portrait_pipeline.py --sheet path.png --name Taren_Natural
         --order 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
The order is a human-reviewed source-cell mapping to PortraitEmotion, never guessed.
"""
import argparse
import json
import subprocess
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_ESRGAN = Path('C:/Users/natem/Projects/FrostboundUnity/art-src/realesrgan/realesrgan-ncnn-vulkan.exe')

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--sheet', type=Path, required=True)
    parser.add_argument('--name', required=True)
    parser.add_argument('--order', required=True)
    parser.add_argument('--esrgan', type=Path, default=DEFAULT_ESRGAN)
    args = parser.parse_args()
    order = [int(i) for i in args.order.split(',')]
    if sorted(order) != list(range(16)) or not args.name.replace('_', '').isalnum():
        raise ValueError('Require safe name and a permutation of all 16 cells')
    approval = ROOT / 'docs/art/species-approval.json'
    if not approval.exists() or json.loads(approval.read_text(encoding='utf-8')).get('status') != 'NATHAN_SPECIES_APPROVED':
        raise RuntimeError('P1 species approval must precede portrait production')
    if not args.esrgan.is_file():
        raise FileNotFoundError(args.esrgan)
    work = ROOT / 'art-src/Generated/P3/portraits' / args.name
    cells, up = work / 'cells', work / 'up4x'
    cells.mkdir(parents=True, exist_ok=True); up.mkdir(parents=True, exist_ok=True)
    subprocess.run([sys.executable, str(ROOT / 'tools/portraits_redo_slice.py'), str(args.sheet.resolve()), '4', '4', str(cells), args.name], check=True, timeout=300)
    subprocess.run([str(args.esrgan), '-i', str(cells), '-o', str(up), '-m', str(args.esrgan.parent / 'models'), '-n', 'realesrgan-x4plus-anime', '-s', '4', '-f', 'png'], check=True, timeout=600)
    for i in range(16):
        name = f'{args.name}__{i}.png'
        with Image.open(cells / name) as original, Image.open(up / name) as enlarged:
            if enlarged.size != (original.width * 4, original.height * 4):
                raise RuntimeError(f'Upscale did not produce 4x pixels: {name}')
    subprocess.run([sys.executable, str(ROOT / 'tools/portraits_redo_assemble.py'), str(up), args.name, str(work), args.name, args.order], check=True, timeout=180)
    with Image.open(work / f'{args.name}.png') as sheet:
        if sheet.size != (2304, 2304):
            raise RuntimeError('Canonical portrait sheet must be 2304x2304')
    (work / 'mapping.json').write_text(json.dumps({'source': str(args.sheet.resolve()), 'name': args.name, 'enum_to_source': order, 'upscale': 'Real-ESRGAN realesrgan-x4plus-anime 4x'}, indent=2), encoding='utf-8')
    print(f'PORTRAIT_ASSEMBLY_OK {work / (args.name + ".png")}')

if __name__ == '__main__':
    main()
