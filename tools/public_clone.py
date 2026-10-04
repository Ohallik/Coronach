"""Export only public Unity source into a fresh, ignored build-check directory.

Includes current tracked and non-ignored new files. Never copies Library, paid
packs, local overrides, secrets or saves. A file/hash manifest records the exact
working source; the caller runs the ordinary copied headless build script.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    destination = (ROOT / args.output).resolve()
    if not destination.is_relative_to((ROOT / 'Builds/quality').resolve()):
        raise ValueError('Public build export must stay under Builds/quality')
    if destination.exists():
        raise ValueError('Preserve prior evidence; choose a new directory')
    names = subprocess.check_output(['git', 'ls-files', '-z', '--cached', '--others', '--exclude-standard'], cwd=ROOT).decode().split('\0')
    names = sorted(set(n for n in names if n.startswith(('Lattice/Assets/', 'Lattice/Packages/', 'Lattice/ProjectSettings/', 'scripts/'))
                       or n == 'tools/patch_unity_packages.py'))
    if any('staggart' in n.lower() or '/Stylized Water' in n or '/Library/' in n or n.endswith('.env') for n in names):
        raise ValueError('Export selection contains private package/source files')
    destination.mkdir(parents=True)
    manifest = []
    for name in names:
        source = ROOT / name
        if not source.is_file():
            continue  # a tracked working-tree deletion is also absent in the export
        data = source.read_bytes()
        if name == 'Lattice/Packages/packages-lock.json':
            lock = json.loads(data)
            # Unity discovers embedded local packages automatically. They are
            # not manifest dependencies, and cannot appear in a public lock.
            lock['dependencies'].pop('xyz.staggart-creations.stylized-grass', None)
            data = (json.dumps(lock, indent=2) + '\n').encode()
        target = destination / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
        manifest.append({'path': name, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
    (destination / 'source-files.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(f'PUBLIC_SOURCE_EXPORT_OK files={len(manifest)} bytes={sum(f["bytes"] for f in manifest)} path={destination}')


if __name__ == '__main__':
    main()
