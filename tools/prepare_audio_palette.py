"""Prepare owned audio candidates outside Unity; no subjective acceptance."""
from pathlib import Path
import argparse,hashlib,json,subprocess,wave
import numpy as np
import imageio_ffmpeg

root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output',type=Path,required=True)
parser.add_argument('--owned-art-root',type=Path,default=Path('C:/Users/natem/Projects/FrostboundUnity/art-src'))
parser.add_argument('--scifi-art-root',type=Path,default=root/'art-src/Kenney_SciFiSounds')
args=parser.parse_args();out=args.output.resolve()
if not out.is_relative_to(root/'Builds/quality'):raise ValueError('Candidates must stay under Builds/quality')
out.mkdir(parents=True,exist_ok=False)
impact=args.owned_art_root/'Kenney_ImpactSounds'
ui=args.owned_art_root/'Kenney_InterfaceSounds'
rpg=args.owned_art_root/'Kenney_RpgAudio'
scifi=args.scifi_art_root
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe();rate=48000
def info(p):return dict(path=p.as_posix(),bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest())
sources={};licenses={}
def layer(pack,name,gain=1,pitch=1,delay=0):return dict(pack=pack,name=name,gain=gain,pitch=pitch,delay=delay)
def decode(spec):
    pack=spec['pack'];p=pack/'Audio'/spec['name'];before=info(p);sources[p.as_posix()]=before
    license=pack/'License.txt';licenses[license.as_posix()]=info(license)
    command=[ffmpeg,'-hide_banner','-loglevel','error','-nostdin','-i',str(p),'-ac','1','-ar',str(rate)]
    if spec['pitch']!=1:command+=['-af',f"asetrate={rate*spec['pitch']},aresample={rate}"]
    command+=['-f','f32le','pipe:1']
    result=subprocess.run(command,capture_output=True,timeout=30,check=True)
    pcm=np.frombuffer(result.stdout,dtype='<f4').copy()
    if len(pcm)==0 or not np.isfinite(pcm).all() or np.max(np.abs(pcm))<.001:raise ValueError('Silent or invalid source '+str(p))
    if info(p)!=before:raise ValueError('Source changed '+str(p))
    peak=float(np.max(np.abs(pcm)));pcm*=spec['gain']/peak
    return pcm
recipes=[]
for i in range(4):
    recipes += [(f'step_metal_{i}',[layer(impact,f'footstep_concrete_{i:03d}.ogg',.8),layer(impact,f'impactPlate_light_{i:03d}.ogg',.2,1.18,.008)]),
                (f'step_rock_{i}',[layer(impact,f'footstep_concrete_{i:03d}.ogg')]),
                (f'step_soil_{i}',[layer(impact,f'footstep_grass_{i:03d}.ogg')])]
for i in range(3):
    recipes += [(f'hit_soft_{i}',[layer(impact,f'impactPunch_medium_{i:03d}.ogg',.75),layer(impact,f'impactSoft_medium_{i:03d}.ogg',.25,1,.008)]),
                (f'hit_armor_{i}',[layer(impact,f'impactMetal_medium_{i:03d}.ogg',.7),layer(impact,f'impactPlate_light_{i:03d}.ogg',.3,.9,.008)]),
                (f'swing_{i}',[layer(rpg,'knifeSlice.ogg' if i%2==0 else 'knifeSlice2.ogg',.8,[1.15,1,.84][i]),layer(rpg,f'cloth{i+1}.ogg',.2)]),
                (f'needle_taren_{i}',[layer(scifi,f'laserRetro_{i:03d}.ogg')]),
                (f'needle_sela_{i}',[layer(scifi,f'laserSmall_{i:03d}.ogg')])]
for name,file in [('ui_confirm','confirmation_001.ogg'),('ui_cancel','back_001.ogg'),('ui_error','error_001.ogg'),('ui_move','select_001.ogg'),('dialogue_tick','tick_001.ogg')]:
    recipes.append((name,[layer(ui,file)]))
manifest={'status':'Prepared candidates only; not staged in game; audition UNVERIFIED','sampleRate':rate,'channels':1,'targetSamplePeakDbfs':-10,'processing':'Decode mono 48k; optional pitch-resample; normalize each layer to unit peak then weighted mix/delay; trim below -60 dB relative activity; 2 ms onset and 12 ms tail fades; normalize composite to -10 dBFS; signed 16-bit PCM.','clips':[]}
for name,specs in recipes:
    arrays=[decode(s) for s in specs];size=max(len(a)+round(s['delay']*rate) for a,s in zip(arrays,specs));mix=np.zeros(size,dtype=np.float64)
    for a,s in zip(arrays,specs):
        start=round(s['delay']*rate);mix[start:start+len(a)]+=a
    active=np.flatnonzero(np.abs(mix)>max(np.max(np.abs(mix))*.001,1e-6));mix=mix[max(0,active[0]-96):min(len(mix),active[-1]+577)]
    n=min(96,len(mix)//2);mix[:n]*=np.linspace(0,1,n);n=min(576,len(mix)//2);mix[-n:]*=np.linspace(1,0,n)
    mix*=10**(-10/20)/np.max(np.abs(mix));pcm=np.round(mix*32767).astype('<i2');path=out/(name+'.wav')
    with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(pcm.tobytes())
    manifest['clips'].append({'key':name,'output':info(path),'seconds':len(mix)/rate,'layers':[{**s,'pack':s['pack'].as_posix()} for s in specs]})
manifest['sources']=list(sources.values());manifest['licenses']=list(licenses.values());manifest['tool']=info(Path(__file__))
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print('AUDIO_CANDIDATES_PREPARED',len(recipes),sum(c['output']['bytes'] for c in manifest['clips']))
