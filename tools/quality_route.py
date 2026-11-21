"""Reject ambiguous workshop checkpoints, and anything the driver would misread, before starting a visible player.

The second half is tools/route_lint.py: unknown keys (JsonUtility ignores them without a word), unknown scenes, and
"until"/"stopWhen" forms the driver doesn't know. Every replay passes through here (scripts/quality-replay.ps1).
"""
import argparse
import json
import sys
from pathlib import Path

from route_lint import lint


def validate_route(route):
    if route.get('scene') != 'Title':
        return
    if route.get('starterParty') is not False or route.get('loadout'):
        raise ValueError('Title routes require starterParty=false and no development loadout')
    if not route.get('steps'):
        raise ValueError('Title route has no checkpoints')
    for index, step in enumerate(route['steps']):
        if not isinstance(step.get('expectedScene'), str) or not step['expectedScene'].strip():
            raise ValueError(f"Title checkpoint {index} ({step.get('name', 'unnamed')}) must explicitly name expectedScene")


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('route', type=Path)
    args = parser.parse_args()
    route = json.loads(args.route.read_text(encoding='utf-8-sig'))
    validate_route(route)
    problems = lint(route)
    if problems:
        print('%s: %d problem%s the driver would not report' % (args.route.name, len(problems), '' if len(problems) == 1 else 's'))
        for problem in problems[:40]:
            print('  ' + problem)
        sys.exit(1)
    print('QUALITY_ROUTE_READY ' + args.route.name)
