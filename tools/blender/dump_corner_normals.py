"""Write every triangle's three corner normals (the shading normals the model carries, as Unity imports them with
normalImportMode Import) in the order tools/blender/dump_uv_triangles.py writes its triangles, to a .npz (round ten,
W1-mooring-05: the Tollkeeper's toon shade falls on faces by these, not by the faces' own facing). Reads only.

  scripts/blender.ps1 -Script tools/blender/dump_corner_normals.py -TimeoutSec 300 -- --input <model.fbx> --output <out.npz>
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from _common import cli_args, reset, import_model  # noqa: E402

p = argparse.ArgumentParser()
p.add_argument("--input", required=True)
p.add_argument("--output", required=True)
a = p.parse_args(cli_args())
reset()
import_model(a.input)
corner = []
for o in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
    mesh = o.data
    mesh.calc_loop_triangles()
    if mesh.uv_layers.active is None:
        continue
    rot = o.matrix_world.to_3x3().normalized()
    normals = mesh.corner_normals
    for tri in mesh.loop_triangles:
        corner.append([list((rot @ normals[l].vector).normalized()) for l in tri.loops])
np.savez(a.output, corner=np.array(corner, dtype=np.float32))
print(f"CORNER_NORMALS_OK count={len(corner)} output={a.output}")
