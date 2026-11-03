"""Copy the current release player into a new, separately profiled workshop package.

Never overwrites an existing package: earlier candidates and the frozen chapter
stay byte-for-byte intact. Usage:
    python tools/package_workshop.py Workshop-Nov03 --revision <git sha>
"""
from datetime import datetime, timezone
from pathlib import Path
import argparse, hashlib, json, re, shutil

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('name')
parser.add_argument('--revision', required=True)
args = parser.parse_args()
if not re.fullmatch(r'Workshop-[A-Za-z0-9-]+', args.name) or args.name in ('Workshop-Chapter',):
    parser.error('package name must be a new Workshop-<name>')
source = root / 'Builds/Windows'
package = root / 'Builds' / args.name
destination = package / 'Player'
destination.mkdir(parents=True, exist_ok=False)
for name in ['Coronach.exe', 'dstorage.dll', 'dstoragecore.dll', 'UnityPlayer.dll', 'UnityCrashHandler64.exe']:
    if (source / name).exists():
        shutil.copy2(source / name, destination / name)
for name in ['Coronach_Data', 'D3D12', 'MonoBleedingEdge', 'Music']:
    if (source / name).exists():
        shutil.copytree(source / name, destination / name)
files = []
for path in sorted(destination.rglob('*')):
    if path.is_file():
        relative = path.relative_to(destination)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        if digest != hashlib.sha256((source / relative).read_bytes()).hexdigest():
            raise RuntimeError(f'Copied player mismatch: {relative}')
        files.append(dict(path=relative.as_posix(), bytes=path.stat().st_size, sha256=digest))
manifest = dict(sourceRevision=args.revision, createdUtc=datetime.now(timezone.utc).isoformat(), files=files)
(package / 'player-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
(package / 'Play Coronach.cmd').write_text(
    '@echo off\npowershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\..\scripts\workshop.ps1" -Package ' + args.name + '\nif errorlevel 1 pause\n')
print('WORKSHOP_PACKAGE_VERIFIED', args.name, len(files), 'files', sum(f['bytes'] for f in files), 'bytes')
