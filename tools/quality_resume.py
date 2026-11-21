"""Copy only a passed replay's isolated saves; preserve a hashed resume chain."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
from quality_route import validate_route


def stage_resume(previous, output, quality_root, reconnaissance=False, past_faults=False):
    """reconnaissance: the run being staged is itself reconnaissance (D290), which may go on from a passed run or from
    another reconnaissance run that found no fault. A run that is to be evidence may only go on from a passed one.
    past_faults: reconnaissance only. It goes on from a reconnaissance run that found faults too, so one chain can find
    every stop in a chapter instead of the first; the faults it stepped over are written into resume.json, and whatever
    it finds afterwards may be their consequence."""
    root = Path(quality_root).resolve(strict=True)
    source = Path(previous).resolve(strict=True)
    target = Path(output).resolve(strict=True)
    if source == target or source == root or target == root or root not in source.parents or root not in target.parents:
        raise ValueError('resume source and output must be distinct runs inside Builds/quality')
    route = json.loads((source / 'route.json').read_text(encoding='utf-8-sig'))
    run = json.loads((source / 'run.json').read_text(encoding='utf-8-sig'))
    analysis = json.loads((source / 'analysis.json').read_text(encoding='utf-8-sig'))
    passed = run.get('valid') and not run.get('failures') and analysis.get('valid') and not analysis.get('failures')
    scouted = (reconnaissance and run.get('reconnaissance') is True and run.get('reconnaissancePassed') is True and not run.get('failures')
               and analysis.get('reconnaissance') is True and analysis.get('reconnaissancePassed') is True and not analysis.get('failures'))
    stepped = reconnaissance and past_faults and run.get('reconnaissance') is True and analysis.get('reconnaissance') is True
    if not passed and not scouted and not stepped:
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
    faults = [] if passed or scouted else sorted(set(list(run.get('failures') or []) + list(analysis.get('failures') or [])))
    manifest = dict(previous=source.relative_to(root).as_posix(), saves=hashes, reconnaissance=bool(reconnaissance), pastFaults=faults,
                    evidence={name: hashlib.sha256((source / name).read_bytes()).hexdigest()
                              for name in ('run.json', 'route.json', 'analysis.json', 'build.json')})
    (target / 'resume.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    return manifest


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('previous', type=Path); p.add_argument('output', type=Path)
    p.add_argument('--reconnaissance', action='store_true', help='the run being staged is reconnaissance (D290), never evidence')
    p.add_argument('--past-faults', action='store_true', help='with --reconnaissance: go on from reconnaissance that found faults, and say so in resume.json')
    args = p.parse_args()
    result = stage_resume(args.previous, args.output, Path(__file__).resolve().parents[1] / 'Builds/quality', args.reconnaissance, args.past_faults)
    print('RESUME_STAGED ' + result['previous'] + ' ' + ', '.join(result['saves']))
