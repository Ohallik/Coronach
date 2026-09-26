"""Author ordinary-input C0 fixtures, with explicit time and navigation coverage."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'docs/quality/routes'
DEST.mkdir(parents=True, exist_ok=True)

def move(name, x, z, seconds, sprint=False, **kw):
    return dict(name=name, seconds=seconds, navigate=True, point=dict(x=x,y=0,z=z), rightTrigger=int(sprint), **kw)

def action(name, seconds, buttons=(), **kw):
    return dict(name=name, seconds=seconds, buttons=list(buttons), **kw)

def write(name, scene, steps):
    data=dict(name=name,scene=scene,sourceRevision='6084c88 gameplay + C0 instrumentation',settleSeconds=8,frameCap=60,starterParty=True,steps=steps)
    (DEST / (name+'.json')).write_text(json.dumps(data,indent=2)+'\n')
    print(name, sum(s['seconds'] for s in steps))

town=[move('office slow approach',-20,0,4),move('office return turn',-20,-5,3),
      move('promenade to market',0,-5,11),move('approach Mira',0,1.3,4),
      action('talk to Mira',4,['South'],expectedUi='dialogue'),
      action('read first line',1.5),action('next line',4,['South']),
      action('release confirm',.5),action('show choices',2,['South']),
      action('release choice',.5),action('choose stock',3,['South'],expectedUi='shop'),
      action('inspect stock',3),action('close shop',1,['East'],expectedUi='world'),
      move('back to promenade',0,-5,4),move('sprint to repair',20,-5,7,True),
      move('repair doorway',20,-9,5),move('return through doorway',20,-5,5),
      action('swap to Sela',.2,['North'],expectedCharacter='Sela'),action('release swap',.3),
      move('Sela market return',0,-5,11),move('Sela office sprint',-20,-5,7,True),
      move('office side wall approach',-30,-5,6),
      action('wall contact',3,x=-.939693,y=-.342020),
      move('Sela reverse to office',-20,-5,7),move('Sela office north',-20,1,4),
      move('Sela 180 turn',-20,-5,4)]
# Reach 120 s by another purposeful out-and-back at slow pace, not stationary padding.
town += [move('slow public junction',-12,-5,7),move('finish at office',-20,-5,8)]
write('c0-decks', 'Hub_Decks', town)

tallow=[]
for lap in range(2):
    for label,x,z,sec in [('arrival',0,-6,3),('port quay',-8,-6,5),('port operator aisle',-8,1,5),('public cross aisle',0,1,5),('starboard service',8,1,5),('starboard quay',8,-6,5),('arrival return',0,-6,5)]:
        tallow.append(move(f'{lap} {label}',x,z,sec,sprint=lap==1))
    tallow += [action('swap',.2,['North']),action('swap release',.3)]
tallow += [move('keeper approach',0,1.3,5),action('keeper conversation',4,['South'],expectedUi='dialogue'),action('read keeper',4),action('keeper advance',12,['South'],pulseSeconds=3),action('dialogue release',1),action('close remaining UI',1,['East']),move('return south',0,-6,7),move('port wall',-11,-6,7),action('wall contact',3,x=-.939693,y=-.342020),move('final arrival',0,-6,9)]
write('c0-tallow','TallowDrift',tallow)

flight=[move('office approach',-20,10,8,leftTrigger=1),action('dock office',2,['South'],expectedScene='Hub_Decks'),
        action('arrival settle',2,expectedScene='Hub_Decks'),move('approach launch hatch',-20,-9,6,expectedScene='Hub_Decks'),
        action('launch',2,['South']),move('back away from office',-20,0,9,leftTrigger=1),
        action('civil east boost',2,x=1,rightTrigger=1),action('brake',3,leftTrigger=1),
        action('civil reverse',2,x=-1),action('brake after reversal',3,leftTrigger=1),
        action('swap ship',.2,['North']),action('release swap',.3),action('Sela thrust',3,y=-1),
        action('Sela brake',3,leftTrigger=1),action('Sela roll',.2,['East'],x=1),action('release roll',2,leftTrigger=1)]
write('c0-cinder-dock','Hub_CinderHalo',flight)

write('c0-down-revive','Arena_Ground',[
    action('receive real enemy attacks until partner down',40,until='partnerDown'),
    dict(name='guard and approach downed partner',seconds=20,navigate=True,approachPartner=True,tolerance=1.25,buttons=['West'],until='partyAlive'),
    action('hold guard after revive',4,['West']),action('swap recovered hero',.2,['North']),action('release swap',1),
    action('resume attacks',4,['South'],pulseSeconds=.6)])

directions=[(0,1),(0,-1),(1,0),(-1,0),(.7071,.7071),(-.7071,-.7071),(-.7071,.7071),(.7071,-.7071)]
for scene in ['Hub_Decks','Arena_Ground','Sorrel_Ridges']:
    steps=[]
    if scene=='Hub_Decks': steps=[move('clear office furnishings',-20,-5,3),move('public promenade',0,-5,12),move('open dock apron',0,-11,4)]
    if scene=='Sorrel_Ridges': steps=[move('cross outpost',0,24,12),move('leave safe pocket',0,32,3)]
    for hero in ['Taren','Sela']:
        for i,(x,y) in enumerate(directions):
            steps += [action(f'{hero} direction {i}',1.2 if scene=='Hub_Decks' else 2,x=x,y=y,expectedCharacter=hero),action(f'{hero} stop {i}',.4)]
        for i,(x,y) in enumerate(directions):
            steps += [action(f'{hero} sprint {i}',.6 if scene=='Hub_Decks' else 1.5,x=x,y=y,rightTrigger=1),action(f'{hero} sprint stop {i}',.4)]
        if scene!='Hub_Decks':
            steps += [action(f'{hero} lock-on',.2,['LeftShoulder']),action('release',.2),action('lock movement',2,x=1),action('reverse',2,x=-1),action('attack',3,['South'],pulseSeconds=.6),action('return to run',2,y=1),action('dodge',.2,['East'],y=-1),action('guard',2,['West'])]
        steps += [action('swap',.2,['North']),action('release swap',.4)]
    write('c0-motion-'+scene.lower(),scene,steps)
