"""Generate the ordinary Gullet combat circuits for the redesigned anatomy.

Geometry comes from Lattice/.../Data/GulletProfile.cs (one source of truth).
Tactics follow the previous accepted circuits: lunge chain into the first
chamber, mine clearing from four sides, a braked second-chamber charge with
Pulse, then handling, wall contact, regroup and swaps. Equipment, AI, damage,
tolerances, flags and focus requirements are unchanged.
"""
import json, math, re, sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = (root / 'Lattice/Assets/_Project/Scripts/Data/GulletProfile.cs').read_text(encoding='utf-8')
knots = [tuple(float(v) for v in m) for m in
         re.findall(r'\((-?\d+), (-?\d+), (\d+), (\d+)\)', src.split('knots =')[1].split('};')[0])]
folds = [(float(z), int(s), float(d)) for z, s, d in
         re.findall(r'\((\d+), (-?1), (\d+)\)', src.split('Folds =')[1].split(';')[0])]
cache_z = float(re.search(r'CacheZ = (\d+)', src).group(1))


def sample(z, f):
    z = max(knots[0][0], min(knots[-1][0], z)); i = 0
    while i < len(knots) - 2 and z > knots[i + 1][0]: i += 1
    t = (z - knots[i][0]) / (knots[i + 1][0] - knots[i][0])
    v = lambda k: knots[max(0, min(len(knots) - 1, k))][f + 1]
    p0, p1, p2, p3 = v(i - 1), v(i), v(i + 1), v(i + 2)
    return .5 * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (3 * p1 - p0 - 3 * p2 + p3) * t ** 3)


center = lambda z: sample(z, 0)
right_edge = lambda z: sample(z, 0) + sample(z, 2)


def route_x(z):
    x = center(z)
    for fz, side, depth in folds: x -= side * depth * .75 * math.exp(-((z - fz) / 16) ** 2)
    return max(center(z) - sample(z, 1) + 6, min(right_edge(z) - 6, x))


def build(hero):
    steps = []
    def nav(label, x, z, seconds, fire=True):
        s = dict(name=label, seconds=seconds, navigate=True, point=dict(x=round(x, 3), y=1, z=z), leftTrigger=1, tolerance=.8)
        if fire: s['buttons'] = ['South']
        steps.append(s)
    def thrust(label, seconds, x=0, y=0, **extra): steps.append(dict(name=label, seconds=seconds, x=x, y=y, **extra))
    def mines(chamber, mx, mz, spread, approach=17):
        nav(f'chamber {chamber} approach the mine cluster', mx, mz - 14, approach)
        for label, px, pz, seconds in [('east', mx + spread, mz - 6, 10), ('north', mx, mz + 2, 8), ('west', mx - spread, mz - 6, 9), ('south', mx, mz - 14, 9)]:
            nav(f'chamber {chamber} {label} mine approach', px, pz, seconds)
            thrust(f'chamber {chamber} {label} mine sustained emitter', 3, y=1, leftTrigger=1, buttons=['South'])

    # The party starts with Taren leading; Sela's circuit swaps to her first,
    # exactly as the accepted circuit03 route did.
    if hero == 'sela': steps.append(dict(name='select Sela before the encounter', seconds=.12, buttons=['North']))
    # Navigation holds the brake (about 2 m/s), as in the accepted circuits:
    # the lunge chain, not the approach, carries the ship into the chamber.
    steps.append(dict(name='fly in through the mouth and entry canal', seconds=17, navigate=True, point=dict(x=round(route_x(35), 3), y=1, z=35),
                      leftTrigger=1, tolerance=.8, expectedCharacter=hero.title(), expectedForm='Flight'))
    for n in range(10):
        thrust(f'advance lunge {n + 1}', .12, y=1, buttons=['West'])
        thrust(f'normal thrust and lunge recovery {n + 1}', .62, y=1)
    thrust('brake after the attack chain', 1.2, leftTrigger=1)
    thrust('clear surviving targets with ordinary fire', 6, leftTrigger=1, buttons=['South'])
    steps.append(dict(name='regroup beside the measured hulls', seconds=8, approachPartner=True, navigate=True, leftTrigger=1, tolerance=3.8))
    # The lunge chain ends near 135 m; the cluster lies deeper in the chamber.
    mines(1, route_x(199), 199, 8, approach=30)
    steps.append(dict(name='chamber 1 opens its own membrane', seconds=.2, expectedFlag='clear.Gullet_Chamber_0', until='partyAlive'))

    nav('cross the opened first valve along its real passage', route_x(262), 262, 40, False)
    # Braked navigation covers about 2 m/s: budget each weave leg by its real
    # length (the folds add sideways travel), never less than the old 12 s.
    previous = (route_x(262), 262)
    for z in (285, 305, 330, 350, 368):
        length = math.hypot(route_x(z) - previous[0], z - previous[1])
        nav(f'weave the slalom throat past the folds at {z} m', route_x(z), z, max(12, round(length / 1.8 + 2)))
        previous = (route_x(z), z)
    for n in range(4):
        thrust(f'second chamber lunge {n + 1}', .12, y=1, buttons=['West'])
        thrust(f'second chamber recovery {n + 1}', .62, y=1, buttons=['South'])
    thrust('brake before overrunning the throat', .6, leftTrigger=1)
    thrust('ordinary Pulse in the throat', .12, leftTrigger=1, buttons=['RightShoulder', 'West'])
    thrust('release the visible area skill before sustained fire', .7, leftTrigger=1)
    thrust('brake and fire after the throat charge', 6, leftTrigger=1, buttons=['South'])
    eddy_x = right_edge(cache_z) - 9
    mines(2, eddy_x - 4, cache_z - 4, 7)
    steps.append(dict(name='chamber 2 opens its own membrane', seconds=.2, expectedFlag='clear.Gullet_Chamber_1', until='partyAlive'))

    # Handling in the salvage eddy, the throat's one roomy pocket.
    hx, hz = eddy_x - 8, cache_z + 2
    for n, (dx, dz) in enumerate([(-5, -5), (0, 0), (5, 5), (5, -5), (0, 0), (-5, 5), (-5, -5)]):
        nav(f'eddy figure eight {n + 1}', hx + dx, hz + dz, 6, False)
    thrust('short east acceleration', .6, x=1)
    thrust('unpowered drift', .5)
    thrust('west counter-thrust', .6, x=-1)
    thrust('brake reversal', 1, leftTrigger=1)
    thrust('roll south', .12, y=-1, buttons=['East'])
    thrust('roll finish', .5, leftTrigger=1)
    nav('return to the eddy centre', hx, hz, 10, False)
    thrust('brief north boost', .4, y=1, rightTrigger=1)
    thrust('boost release coast', .4)
    thrust('brake after boost', 1, leftTrigger=1)
    nav('slow approach to the eddy pocket wall', right_edge(hz) - 6, hz, 9, False)
    thrust('light combat wall contact', .65, x=1, leftTrigger=1)
    nav('return from wall contact', hx, hz, 9, False)
    steps.append(dict(name='catch-up at adjacent hull separation', seconds=10, navigate=True, approachPartner=True, tolerance=3.8, leftTrigger=1))
    thrust('swap beside the other visible hull', .12, buttons=['North'])
    thrust('release swap and settle camera', 1, leftTrigger=1, expectedCharacter='Sela' if hero == 'taren' else 'Taren')
    thrust('return to circuit pilot', .12, buttons=['North'])
    thrust('release return swap', 1, leftTrigger=1, expectedCharacter=hero.title())
    steps.append(dict(name='both craft survive two chambers', seconds=.2, until='partyAlive', expectedCharacter=hero.title()))
    duration = sum(s['seconds'] for s in steps)
    if duration < 300: raise RuntimeError(f'Route is shorter than required combat circuit: {duration}')
    return dict(name=f'c4-gullet-circuit04-{hero}', scene='Gullet_Tunnel',
                sourceRevision='Redesigned Gullet anatomy (GulletProfile): same tactics, equipment, AI, damage, tolerances, flags and focus requirements as circuit03; the throat is woven instead of charged blindly and handling moves to the salvage eddy.',
                settleSeconds=1, frameCap=60, starterParty=True, screenshotInterval=2, steps=steps), duration


if __name__ == '__main__':
    for hero in ('taren', 'sela'):
        route, duration = build(hero)
        out = root / f'docs/quality/routes/c4-gullet-circuit-{hero}.json'
        out.write_text(json.dumps(route, indent=2) + '\n', encoding='utf-8')
        print(out.name, len(route['steps']), round(duration, 1))
