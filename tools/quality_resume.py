"""Copy only a passed replay's isolated saves; preserve a hashed resume chain."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
from quality_route import validate_route


def stage_resume(previous, output, quality_root):
    root = Path(quality_root).resolve(strict=True)
    source = Path(previous).resolve(strict=True)
    target = Path(output).resolve(strict=True)
    if source == target or source == root or target == root or root not in source.parents or root not in target.parents:
        raise ValueError('resume source and output must be distinct runs inside Builds/quality')
    route = json.loads((source / 'route.json').read_text(encoding='utf-8-sig'))
    run = json.loads((source / 'run.json').read_text(encoding='utf-8-sig'))
    analysis = json.loads((source / 'analysis.json').read_text(encoding='utf-8-sig'))
    if not run.get('valid') or run.get('failures') or not analysis.get('valid') or analysis.get('failures'):
        raise ValueError('resume requires a passed runtime and offline analysis')
    if route.get('scene') != 'Title' or route.get('starterParty', True) or route.get('loadout'):
        raise ValueError('workshop resumes must originate from an unseeded Title route')
    validate_route(route)
    saves = source / 'saves'
    if saves.is_symlink() or saves.resolve(strict=True).parent != source:
        raise ValueError('save directory escapes the prior run')
    slots = [p for p in saves.iterdir() if p.name in {s + '.json' for s in ('autosave', 'slot1', 'slot2', 'slot3')}]
    if not slots:
        raise ValueError('no ordinary save slots in prior run')
    # Validate the entire input before creating/copying anything.
    for slot in slots:
        if slot.is_symlink() or not slot.is_file() or slot.resolve(strict=True).parent != saves:
            raise ValueError('save slot escapes the prior run')
        state = json.loads(slot.read_text(encoding='utf-8-sig'))
        # Copy both supported formats exactly; migration belongs to the player.
        # bool and float compare equal to integers in Python but are not save versions.
        version = state.get('version')
        if type(version) is not int or version not in {1, 2} or not state.get('party'):
            raise ValueError('invalid ordinary save slot')
    destination = target / 'saves'
    destination.mkdir()  # Refuse an existing save directory, even when empty.
    hashes = {}
    for slot in sorted(slots):
        digest = hashlib.sha256(slot.read_bytes()).hexdigest()
        shutil.copyfile(slot, destination / slot.name)
        if hashlib.sha256((destination / slot.name).read_bytes()).hexdigest() != digest:
            raise ValueError('save copy hash mismatch')
        hashes[slot.name] = digest
    manifest = dict(previous=source.relative_to(root).as_posix(), saves=hashes,
                    evidence={name: hashlib.sha256((source / name).read_bytes()).hexdigest()
                              for name in ('run.json', 'route.json', 'analysis.json', 'build.json')})
    (target / 'resume.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    return manifest


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('previous', type=Path); p.add_argument('output', type=Path)
    args = p.parse_args()
    result = stage_resume(args.previous, args.output, Path(__file__).resolve().parents[1] / 'Builds/quality')
    print('RESUME_STAGED ' + result['previous'] + ' ' + ', '.join(result['saves']))
