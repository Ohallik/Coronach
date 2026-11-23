"""Write every triangle of a model's meshes as it stands in its bind pose: the three corners' positions (metres, Blender's Z up),
the face normal and the three corners' UVs, to a .npz a texture tool can classify by where on the body each one is
(tools/meret_grade.py). Reads only; changes nothing.

  scripts/blender.ps1 -Script tools/blender/dump_uv_triangles.py -TimeoutSec 300 -- --input <model.fbx> --output <out.npz>
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
pos, nrm, uvs = [], [], []
for o in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
    mesh = o.data
    mesh.calc_loop_triangles()
    layer = mesh.uv_layers.active
    if layer is None:
        continue
    world = o.matrix_world
    rot = world.to_3x3().normalized()
    for tri in mesh.loop_triangles:
        pos.append([list(world @ mesh.vertices[v].co) for v in tri.vertices])
        nrm.append(list((rot @ tri.normal).normalized()))
        uvs.append([list(layer.data[l].uv) for l in tri.loops])
np.savez(a.output, pos=np.array(pos, dtype=np.float32), nrm=np.array(nrm, dtype=np.float32), uv=np.array(uvs, dtype=np.float32))
print(f"UV_TRIANGLES_OK count={len(pos)} output={a.output}")
