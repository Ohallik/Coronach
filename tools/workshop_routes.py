"""Ordinary-input routes for the remaining workshop map coverage (D126).

No scene edits, forced flags, teleports or combat calls. Loadouts stand in for
reachable arrival saves; each newly traversed encounter must clear in play.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'docs/quality/routes'


class Walk:
    def __init__(self):
        self.steps = []

    def action(self, name, seconds, **kw):
        self.steps.append(dict(name=name, seconds=seconds, **kw))

    def nav(self, name, x, z, seconds, **kw):
        self.action(name, seconds, navigate=True, point=dict(x=x, y=0, z=z), tolerance=.8, **kw)

    def tap(self, name, buttons, **kw):
        self.action(name, .18, buttons=buttons, **kw)
        self.action(name + ' release', .45)

    def fight(self, index):
        flag = f'clear.Sorrel_{index}'
        self.tap(f'lock service encounter {index}', ['LeftShoulder'])
        self.tap(f'Arc Cleave encounter {index}', ['RightShoulder', 'South'])
        self.action(f'ordinary attacks encounter {index}', 14, navigate=True,
                    approachTarget=True, tolerance=2.3, rangedTolerance=12,
                    buttons=['South'], pulseSeconds=.46, stopWhen='flag:' + flag)
        self.tap(f'Pulse encounter {index}', ['RightShoulder', 'West'])
        self.action(f'finish encounter {index}', 16, navigate=True,
                    approachTarget=True, tolerance=2.3, rangedTolerance=12,
                    buttons=['South'], pulseSeconds=.46, stopWhen='flag:' + flag)
        self.action(f'encounter {index} opens', .3, expectedFlag=flag)
        self.action(f'carried repair gel after encounter {index}', .18, leftTrigger=1)
        self.action('release gel', .45)

    def write(self, name, loadout, description):
        route = dict(name=name, scene='Sorrel_Ridges', loadout=loadout,
                     sourceRevision=description, settleSeconds=1, frameCap=60,
                     starterParty=True, screenshotInterval=2, steps=self.steps)
        (DEST / (name + '.json')).write_text(json.dumps(route, indent=2) + '\n', encoding='utf-8')
        print(name, len(self.steps), round(sum(s['seconds'] for s in self.steps), 1))


def service():
    w = Walk()
    w.nav('arrival to common court', 0, 14, 18)
    w.nav('leave the outpost on the haul road', 0, 25, 8)
    w.nav('service fork below the hound clearing', 6, 31, 7)
    w.nav('service spur', 18, 37, 9)
    for label, x, z, sec, encounter in [
        ('discarded equipment clearing', 29, 46, 12, 8),
        ('service trail bend', 39, 64, 11, None),
        ('scrapmite equipment pocket', 36, 73, 7, 9),
        ('console operator side', 31, 83, 8, None),
        ('upper service sentinel', 34, 103, 12, 10),
        ('stores approach', 31, 116, 9, None),
        ('upper junction sentinel', 29, 126, 8, 11),
    ]:
        w.nav(label, x, z, sec)
        if encounter is not None:
            w.fight(encounter)
            w.nav('regroup at ' + label, x, z, 10)
    for label, x, z, sec in [
        ('stores return', 31, 116, 8), ('upper service return', 35, 103, 9),
        ('console return', 31, 83, 12), ('lower bend return', 39, 64, 12),
        ('equipment return', 29, 46, 12), ('service spur return', 18, 37, 9),
        ('lower fork return', 6, 31, 9),
        ('outpost threshold', 0, 25, 10), ('common court return', 0, 14, 9),
        ('repair operator stance', -8, 7, 9),
    ]:
        w.nav(label, x, z, sec)
    w.tap('repair and save the service branch', ['South'])
    w.action('both heroes ready', 2, until='partyAlive', expectedFlag='clear.Sorrel_11')
    w.write('sorrel-service-map', 'moon', 'Ordinary east service branch and return: reachable level 3 moon loadout, both Scrapmite packs and both Sentinels must clear; repair on return.')


def bore():
    w = Walk()
    w.nav('arrival to common court', 0, 14, 18)
    w.nav('leave the outpost', 0, 25, 8)
    for label, x, z, sec, encounter in [
        ('lower haul bend', 9, 39, 10, 4),
        ('haul road below strata', 4, 58, 12, None),
        ('western haul bend', -8, 75, 12, 5),
        ('central wash encounter', -5, 98, 14, 6),
        ('upper haul bend', 9, 111, 12, None),
        ('excavation approach', 16, 129, 12, 7),
    ]:
        w.nav(label, x, z, sec)
        if encounter is not None:
            w.fight(encounter)
            w.nav('regroup at ' + label, x, z, 10)
    w.nav('bench operator access from the south', 7, 139, 10)
    w.nav('haul lane west of the anvil', 2, 147, 8)
    w.nav('through the cleared Burrower entry', 0, 159, 11, expectedFlag='bossdown.Burrower')
    w.nav('cross the excavation floor', 5, 169, 9)
    w.nav('stand beside the drill bore', 8, 174.5, 6)
    w.action('descend into Hushwell', .18, buttons=['South'])
    w.action('arrive in the breach', 8, expectedScene='Hushwell', expectedForm='Natural')
    # The already accepted descent starts here; join its first ordinary waypoint.
    w.nav('breach floor', 0, 8, 7, expectedScene='Hushwell')
    w.write('sorrel-hushwell-approach', 'gullet', 'Ordinary haul-road walk into Hushwell after the warp key: level 5 reachable cleared-Burrower save; four haul encounters clear through input, then the bore changes scenes.')


def gullet():
    from gullet_routes import build, route_x
    route, _ = build('taren')
    stop = next(i for i, s in enumerate(route['steps']) if s['name'] == 'chamber 2 opens its own membrane')
    steps = route['steps'][:stop + 1]

    def action(name, seconds, **kw):
        steps.append(dict(name=name, seconds=seconds, **kw))

    def nav(name, x, z, seconds, fire=False):
        action(name, seconds, navigate=True, point=dict(x=round(x, 3), y=1, z=z),
               tolerance=.8, leftTrigger=1, buttons=['South'] if fire else [])

    def tap(name, buttons, **kw):
        action(name, .18, buttons=buttons, **kw)
        action(name + ' release', .45, leftTrigger=1)

    nav('leave the salvage eddy', route_x(430), 430, 20)
    for z, seconds in [(452, 17), (474, 17), (490, 12), (515, 18), (540, 18)]:
        nav(f'through the second valve toward the nursery {z}', route_x(z), z, seconds, True)
    for n in range(7):
        action(f'nursery lunge {n + 1}', .12, y=1, buttons=['West'])
        action(f'nursery lunge recovery {n + 1}', .62, y=1, buttons=['South'])
    action('brake in the nursery chamber', 1, leftTrigger=1)
    tap('Pulse in the nursery gate', ['RightShoulder', 'West'], leftTrigger=1)
    action('ordinary fire on nursery defenders', 12, leftTrigger=1, buttons=['South'])
    mx, mz = route_x(634), 634
    for side, x, z, sec in [('south approach', mx, mz-14, 24),
                            ('east', mx+8, mz-6, 10), ('north', mx, mz+2, 8),
                            ('west', mx-8, mz-6, 9), ('south', mx, mz-14, 9)]:
        nav(f'nursery mine {side}', x, z, sec, True)
        action(f'nursery mine fire {side}', 4, y=1, leftTrigger=1, buttons=['South'])
    action('nursery gate opens', .3, expectedFlag='clear.Gullet_Chamber_2')
    nav('cross the nursery gate', route_x(700), 700, 46)
    nav('enter the collar chamber', route_x(755), 755, 32)
    nav('engage the Cantor inside its coil', route_x(784), 784, 19, True)
    tap('lock Cantor', ['LeftShoulder'], leftTrigger=1)
    for n in range(10):
        action(f'Cantor emitter pursuit {n + 1}', 18, navigate=True, approachTarget=True,
               tolerance=17, rangedTolerance=17, leftTrigger=1, buttons=['South'],
               stopWhen='flag:bossdown.Cantor')
        tap(f'Cantor Pulse {n + 1}', ['RightShoulder', 'West'], leftTrigger=1)
        action(f'Cantor evasive roll {n + 1}', .18, x=1 if n % 2 == 0 else -1,
               leftTrigger=1, buttons=['East'])
        action('brake after roll', .6, leftTrigger=1)
    action('Cantor releases the exit', 3, expectedFlag='bossdown.Cantor')
    action('regroup after the coil', 18, navigate=True, approachPartner=True,
           tolerance=3.8, leftTrigger=1)
    action('both pilots ready for the exit', .3, until='partyAlive')
    nav('leave the coil by its centre', route_x(850), 850, 44)
    nav('through the exit valve', route_x(876), 876, 18)
    nav('Tallow warp stance', route_x(887), 887, 9)
    action('use the Tallow exit', .18, buttons=['South'])
    action('arrive at Tallow approach', 8, expectedScene='TallowApproach', expectedForm='CivilFlight')
    route.update(name='gullet-gate-coil-exit', loadout='gullet', steps=steps,
                 sourceRevision='Ordinary mouth-to-exit flight using the reachable level 5 warp-key loadout: all three chambers, Cantor and Tallow transition must complete through pad input.')
    (DEST / 'gullet-gate-coil-exit.json').write_text(json.dumps(route, indent=2) + '\n', encoding='utf-8')
    print(route['name'], len(steps), round(sum(s['seconds'] for s in steps), 1))


if __name__ == '__main__':
    service()
    bore()
    gullet()
