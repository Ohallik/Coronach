"""Required intake data map: extract generated cyan sync pigment, without painting new art."""
import argparse
from pathlib import Path
import numpy as np
from PIL import Image

def extract(source,destination,key='cyan'):
    image=Image.open(source).convert('RGB')
    rgb=np.asarray(image,dtype=np.float32)/255
    if key=='cyan':
        strength=np.clip((np.minimum(rgb[:,:,1],rgb[:,:,2])-rgb[:,:,0]-.22)*5,0,1)
        strength*=np.clip((rgb[:,:,1]-.45)*5,0,1)
    else:
        hsv=np.asarray(image.convert('HSV'),dtype=np.float32)/255
        hues={'amber':.105,'orange':.060,'green':.40,'blue':.61}
        if key=='white':strength=np.clip((hsv[:,:,2]-.91)*14,0,1)*np.clip((.13-hsv[:,:,1])*10,0,1)
        elif key in ('lamp','bulb'):
            # lamp: bright amber glass only (copper trim shares the hue but is darker and redder);
            # bulb: pale yellow bulbs only (ochre gravel shares the hue but is darker and more saturated).
            centre,width,sat_lo,sat_hi,val=((.11,.045,.45,1.01,.8) if key=='lamp' else (.15,.04,.18,.72,.86))
            distance=np.abs(hsv[:,:,0]-centre)
            strength=np.clip((width-distance)*30,0,1)*np.clip((hsv[:,:,1]-sat_lo)*8,0,1)*np.clip((sat_hi-hsv[:,:,1])*8,0,1)*np.clip((hsv[:,:,2]-val)*10,0,1)
        else:
            distance=np.abs(hsv[:,:,0]-hues[key]);distance=np.minimum(distance,1-distance)
            strength=np.clip((.075-distance)*20,0,1)*np.clip((hsv[:,:,1]-.52)*5,0,1)*np.clip((hsv[:,:,2]-.5)*5,0,1)
    Image.fromarray((strength*255).astype(np.uint8)).save(destination)
    print('EMISSION_MASK_OK',Path(destination).name,'coverage',round(float((strength>.15).mean()),4))

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('source');parser.add_argument('destination');parser.add_argument('--key',choices=['cyan','amber','orange','green','blue','white','lamp','bulb'],default='cyan');args=parser.parse_args();extract(args.source,args.destination,args.key)
