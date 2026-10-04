"""Reject ambiguous workshop checkpoints before starting a visible player."""
import argparse
import json
from pathlib import Path


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
    validate_route(json.loads(args.route.read_text(encoding='utf-8-sig')))
    print('QUALITY_ROUTE_READY ' + args.route.name)
