"""C2 ordinary-input locomotion loops. Combat review stays a separate route."""
import json
import math
from pathlib import Path

destination=Path(__file__).resolve().parents[1]/'docs/quality/routes'

def centre(hero,x,z,seconds=9):
    return dict(name=hero+' return to clear circulation',seconds=seconds,navigate=True,
                point=dict(x=x,y=0,z=z),expectedCharacter=hero)

for form,scene,point in [('natural','Hub_Decks',(-28,-11)),('shaped','Sorrel_Ridges',(0,-6))]:
    steps=[]
    for hero in ['Taren','Sela']:
        if hero=='Sela':
            steps.extend([dict(name='swap to Sela',seconds=.2,buttons=['North'],expectedCharacter='Sela'),
                          dict(name='release swap and finish form settling',seconds=.8)])
            if form=='natural':
                align=centre(hero,-28,-3,4)
                align['name']='Sela align with office opening'
                steps.append(align)
        start=len(steps)
        steps.append(centre(hero,*point))
        # Natural uses its actual arrival vestibule. Shaped uses the existing
        # clear landing ground south of the outpost, before encounter triggers.
        # No enemy/player stats, damage rules, physics or form are overridden.
        for gait,magnitude,seconds,rest,boost in [
            ('near-zero starts',.3,.95,.6,False),('ordinary travel',1,1.2,.35,False),
            ('sprint',1,.55 if form=='natural' else .75,.7 if form=='natural' else 1.2,True)]:
            for angle in range(0,360,45):
                # Input is camera-relative; camera yaw is 20 degrees in these
                # real zones. Pair each world direction with a full reversal.
                radians=math.radians(angle-20)
                x,y=math.sin(radians)*magnitude,math.cos(radians)*magnitude
                for sign,label in [(1,'out'),(-1,'reverse')]:
                    steps.append(dict(name=f'{hero} {gait} {angle} {label}',seconds=seconds,
                        x=round(x*sign,7),y=round(y*sign,7),rightTrigger=int(boost),expectedCharacter=hero))
                steps.append(dict(name=f'{hero} {gait} {angle} planted stop',seconds=rest,expectedCharacter=hero))
            steps.append(centre(hero,*point,3))
        # Public promenade is a clear, straight walking connection across all
        # three occupied hulls. Longer strides here cover complete torso cycles;
        # the vestibule's eight-direction sprint reversals are necessarily short.
        long_point=(-28,-3) if form=='natural' else (0,-6)
        steps.append(centre(hero,*long_point,7))
        for gait,magnitude,seconds,boost in [('slow cycle',.3,5,False),('travel cycle',1,3,False),('sprint cycle',1,3,True)]:
            radians=math.radians(90-20)
            for sign,label in [(1,'out'),(-1,'return')]:
                steps.append(dict(name=f'{hero} {gait} {label}',seconds=seconds,
                    x=round(math.sin(radians)*magnitude*sign,7),y=round(math.cos(radians)*magnitude*sign,7),
                    rightTrigger=int(boost),expectedCharacter=hero))
            steps.append(dict(name=hero+' cycle stop',seconds=1.5,expectedCharacter=hero))
        assert sum(s['seconds'] for s in steps[start:])>=60
    for fps in [30,60,120]:
        for step in steps:step['expectedForm']=form.title()
        name=f'c2-{form}-{fps}'
        route=dict(name=name,scene=scene,sourceRevision='C2 candidate; exact packaged hashes in each run',
                   settleSeconds=8,screenshotInterval=8,frameCap=fps,starterParty=True,steps=steps)
        (destination/(name+'.json')).write_text(json.dumps(route,indent=2)+'\n')
        print(name,round(sum(s['seconds'] for s in steps),2),'seconds')
