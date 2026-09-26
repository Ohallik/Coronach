"""Ordinary-input routes matching the paired Cinder redesign; C0 stays immutable."""
import copy
import json
from pathlib import Path

dest=Path(__file__).resolve().parents[1]/'docs/quality/routes'
def move(name,x,z,seconds,sprint=False,**kw):
    return dict(name=name,seconds=seconds,navigate=True,point=dict(x=x,y=0,z=z),rightTrigger=int(sprint),**kw)
def action(name,seconds,buttons=(),**kw):
    return dict(name=name,seconds=seconds,buttons=list(buttons),**kw)
def write(name,scene,steps):
    data=dict(name=name,scene=scene,sourceRevision='paired station redesign; build hashes in evidence',settleSeconds=8,frameCap=60,starterParty=True,steps=steps)
    (dest/(name+'.json')).write_text(json.dumps(data,indent=2)+'\n')
    print(name,sum(s['seconds'] for s in steps))
    return data

baseline=json.loads((dest/'c0-decks.json').read_text())
conversation=copy.deepcopy(baseline['steps'][4:13])
steps=[move('office airlock to promenade',-28,-3,4),move('office slow approach',-28,2,3),
       move('office turn to public route',-28,-3,3),move('public bridge to market',0,-3,12),move('approach Mira',0,2,3)]+conversation+[
       move('back to public promenade',0,-3,3),move('sprint public bridge to repair',28,-3,8,True),
       move('repair public entrance',28,2,3),move('workshop center aisle',28,8,3),move('parts store aisle',28,15,4),
       move('rear receiving gallery',28,21.5,3),move('service bridge to provisions',0,21.5,8,True),
       move('shared galley',0,14.5,4),move('galley to service gallery',0,21.5,4),move('service bridge to housing',-28,21.5,8,True),
       move('quiet cabin corridor',-28,14.5,4),move('housing back to office',-28,2,6),move('office to promenade',-28,-3,3),
       move('dock vestibule return',-28,-11,4),action('swap to Sela',.2,['North'],expectedCharacter='Sela'),action('release swap',.8),
       move('Sela port hull boundary',-38,-11,5),action('port wall contact',3,x=-.939693,y=-.342020),
       move('Sela full reversal to airlock',-28,-11,6)]
decks=write('station-decks','Hub_Decks',steps)

# First visit and warmed laps stay in one player process. No cache clearing,
# teleport, quest reset or direct dialogue call between the measured windows.
repeat_conversation=[action('repeat Mira conversation',4,['South'],expectedUi='dialogue'),action('read repeat line',2),
                     action('advance to stock',2,['South'],expectedUi='shop'),action('inspect warm stock',4),action('close warm shop',1,['East'],expectedUi='world')]
warm_steps=copy.deepcopy(steps[:5]+repeat_conversation+steps[14:])
warm_steps += [move('warm return loop outbound',-20,-11,4),move('warm return loop inbound',-28,-11,4)]

tallow=json.loads((dest/'c0-tallow.json').read_text())
for step in tallow['steps']:
    if step['name']=='keeper advance':step['seconds']=1;step.pop('pulseSeconds',None)
tallow['steps'] += [move('second quay crossing',8,-6,5.5),move('quay return',0,-6,5.5)]
write('station-tallow','TallowDrift',tallow['steps'])
write('station-tallow-map','TallowDrift',tallow['steps']+[
    move('refuge cross aisle',-3,1,5),move('life support service aisle',-3,7.4,4),
    move('rest cabin frontage',5,7.4,5),move('around repair console',8,7.4,2),move('repair service return',8,1,4),
    move('free repair console',6,1.6,2),action('repair and rest',1,['South'],expectedUi='world',expectedFlag='sliceComplete'),
    move('starboard boundary',11,1,3),action('starboard hull contact',2,x=.939693,y=.342020),
    move('central public return',0,1,7),move('pressure vestibule',0,-10,6),
    action('launch Tallow',2,['South'],expectedScene='TallowApproach'),action('release launch confirm',.3,expectedScene='TallowApproach'),
    move('clear beacon docking stem',0,-3,5,leftTrigger=1,expectedScene='TallowApproach'),
    move('redock approach',0,2,5,leftTrigger=1,expectedScene='TallowApproach'),
    action('redock refuge',2,['South']),action('arrival restored',2)])

for base,first,warm in [('station-decks',steps,warm_steps),('station-tallow',tallow['steps'],tallow['steps'])]:
    for count,suffix in [(2,'first-warm'),(5,'ten-minute')]:
        data=copy.deepcopy(decks if base=='station-decks' else tallow);data['name']=base+'-'+suffix;data['sourceRevision']='station redesign; build hashes in evidence';data['steps']=[];data['segments']=[]
        for lap in range(count):
            part=copy.deepcopy(first if lap==0 else warm)
            if base=='station-decks':
                for step in part:
                    if step.get('expectedCharacter'):step['expectedCharacter']='Sela' if lap%2==0 else 'Taren'
            data['segments'].append(dict(name='first visit' if lap==0 else f'warm lap {lap}',firstStep=len(data['steps']),stepCount=len(part)))
            data['steps']+=part
        (dest/(data['name']+'.json')).write_text(json.dumps(data,indent=2)+'\n')
        print(data['name'],sum(s['seconds'] for s in data['steps']))

write('station-dock','Hub_CinderHalo',[
    move('office attached dock approach',-28,-22,9,leftTrigger=1),action('dock office',2,['South'],expectedScene='Hub_Decks'),
    action('interior arrival',2,expectedScene='Hub_Decks'),move('office launch hatch',-28,-13,3,expectedScene='Hub_Decks'),
    action('launch office',2,['South']),action('release launch office',.3),move('brake clear of hull',-28,-31,6,leftTrigger=1),
    move('cross approach to market',0,-31,17,leftTrigger=1),move('market attached dock',0,-22,7,leftTrigger=1),
    action('dock market',2,['South'],expectedScene='Hub_Decks'),action('market arrival',2,expectedScene='Hub_Decks'),
    move('market launch hatch',0,-13,3,expectedScene='Hub_Decks'),action('launch market',2,['South']),action('release launch market',.3),
    move('market braking clearance',0,-31,6,leftTrigger=1),move('receiving approach lane',28,-31,17,leftTrigger=1),
    move('repair attached dock',28,-22,7,leftTrigger=1),action('dock repair',2,['South'],expectedScene='Hub_Decks'),
    action('repair arrival',2,expectedScene='Hub_Decks'),move('repair launch hatch',28,-13,3,expectedScene='Hub_Decks'),
    action('launch repair',2,['South']),action('release launch repair',.3),move('repair braking clearance',28,-31,6,leftTrigger=1)])
