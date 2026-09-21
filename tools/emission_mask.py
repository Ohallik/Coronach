"""Required intake data map: extract generated cyan sync pigment, without painting new art."""
import argparse
from pathlib import Path
import numpy as np
from PIL import Image

def extract(source,destination):
    image=Image.open(source).convert('RGB')
    rgb=np.asarray(image,dtype=np.float32)/255
    strength=np.clip((np.minimum(rgb[:,:,1],rgb[:,:,2])-rgb[:,:,0]-.22)*5,0,1)
    strength*=np.clip((rgb[:,:,1]-.45)*5,0,1)
    Image.fromarray((strength*255).astype(np.uint8)).save(destination)
    print('EMISSION_MASK_OK',Path(destination).name,'coverage',round(float((strength>.15).mean()),4))

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('source');parser.add_argument('destination');args=parser.parse_args();extract(args.source,args.destination)
