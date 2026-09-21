"""Read-only Frostbound donor access; copy only the explicitly permitted CC0 particle subset."""
import shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=Path('C:/Users/natem/Projects/FrostboundUnity/art-src/Kenney_ParticlePack')
destination=root/'art-src/Imported/KenneyParticles'
destination.mkdir(parents=True,exist_ok=True)
for name in ['flare_01.png','circle_02.png','slash_01.png']:
    shutil.copy2(source/'PNG (Transparent)'/name,destination/name)
shutil.copy2(source/'License.txt',destination/'License.txt')
print('PARTICLE_DONORS_OK 3 textures, CC0 licence archived')
