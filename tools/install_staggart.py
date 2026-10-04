"""Install Nathan's licensed local packages into ignored Unity folders.

Dry-run by default. Never downloads, stages, or redistributes paid source.
Run --install only with the Unity project closed; the same ownership guard as
the Unity build scripts is enforced before writing anything.
"""
from __future__ import annotations

import argparse
import hashlib
import os
from pathlib import Path, PurePosixPath
import subprocess
import tarfile

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / 'Lattice'
PACKS = {
    'Stylized Water 3.unitypackage': 'Assets/Stylized Water 3',
    'Stylized Grass Shader for Unity 6.unitypackage': 'Packages/xyz.staggart-creations.stylized-grass',
}


def destination(pathname: str, prefix: str) -> Path:
    """Reject traversal, absolute paths and any path outside this pack's root."""
    path = PurePosixPath(pathname)
    if ('\\' in pathname or ':' in pathname or path.is_absolute()
            or '..' in path.parts or '\x00' in pathname
            or not (pathname == prefix or pathname.startswith(prefix + '/'))):
        raise ValueError('Package path outside its approved root: ' + repr(pathname))
    target = (PROJECT / path).resolve()
    if not target.is_relative_to((PROJECT / prefix).resolve()):
        raise ValueError('Package path resolves outside its approved root')
    return target


def plan(package: Path, prefix: str):
    files = {}
    with tarfile.open(package, 'r:gz') as archive:
        members = {m.name: m for m in archive.getmembers()}
        for name, member in members.items():
            if not name.endswith('/pathname'):
                continue
            if not member.isfile():
                raise ValueError('Package pathname is not a regular file')
            # This grass package adds a literal newline + "00" record suffix.
            # It is not part of the asset path; reject any other extra content.
            raw = archive.extractfile(member).read().decode('utf-8')
            fields = raw.rstrip('\n\r\0').splitlines()
            if len(fields) > 2 or len(fields) == 2 and fields[1] != '00':
                raise ValueError('Unexpected package pathname record')
            pathname = fields[0]
            target = destination(pathname, prefix)
            parent = name.rsplit('/', 1)[0]
            for suffix, output in [('asset', target), ('asset.meta', Path(str(target) + '.meta'))]:
                entry = members.get(parent + '/' + suffix)
                if entry is None:
                    continue
                if not entry.isfile():
                    raise ValueError('Package payload is not a regular file: ' + entry.name)
                payload = archive.extractfile(entry).read()
                if output in files:
                    raise ValueError('Duplicate package destination: ' + str(output))
                files[output] = payload
    if not files:
        raise ValueError('Empty package: ' + str(package))
    return files


def ignored_and_untracked(files):
    paths = [p.relative_to(ROOT).as_posix() for p in files]
    data = '\0'.join(paths) + '\0'
    result = subprocess.run(['git', 'check-ignore', '--no-index', '-z', '--stdin'],
                            cwd=ROOT, input=data, capture_output=True, text=True)
    ignored = set(result.stdout.rstrip('\0').split('\0'))
    missing = set(paths) - ignored
    if missing:
        raise ValueError('Refusing non-ignored paid destinations: ' + ', '.join(sorted(missing)))
    tracked = subprocess.run(['git', 'ls-files', '-z', '--',
                              *[path for p in PACKS.values() for path in ('Lattice/' + p, 'Lattice/' + p + '.meta')]],
                             cwd=ROOT, capture_output=True, text=True, check=True).stdout
    if tracked:
        raise ValueError('Paid package files are already tracked; stop and remove them from Git before importing')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cache', type=Path, default=Path(os.environ.get('APPDATA', '')) /
                        'Unity/Asset Store-5.x/Staggart Creations/Shaders')
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    files = {}
    for filename, prefix in PACKS.items():
        package = args.cache / filename
        incoming = plan(package, prefix)
        if set(files) & set(incoming):
            raise ValueError('Packages overlap')
        files.update(incoming)
        print(f'{filename}: {len(incoming)} files; sha256={hashlib.sha256(package.read_bytes()).hexdigest()}')
    ignored_and_untracked(files)
    changed = {p: b for p, b in files.items() if not p.is_file() or p.read_bytes() != b}
    print(f'Protected destinations verified: {len(files)} files, {len(changed)} new or changed')
    if not args.install:
        print('Dry run. Use --install to import your licensed copies locally.')
        return
    subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command',
                    ". './scripts/unity-process.ps1'; Assert-LatticeUnlocked ((Resolve-Path './Lattice').Path)"],
                   cwd=ROOT, check=True)
    for path, payload in changed.items():
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(payload)
    print(f'STAGGART_LOCAL_IMPORT_OK files={len(changed)}; vendor source remains ignored')


if __name__ == '__main__':
    main()
