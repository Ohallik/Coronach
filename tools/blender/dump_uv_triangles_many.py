"""dump_uv_triangles.py for many models in one Blender: each model's triangles in its bind pose (corners in metres, Blender's
Z up; face normal; corner UVs; the bone that carries most of the triangle, by name) to <outdir>/<model stem>.npz, for
tools/townsfolk_dyes.py. Reads only; changes nothing.

  scripts/blender.ps1 -Script tools/blender/dump_uv_triangles_many.py -TimeoutSec 900 -- --list <models.txt> --outdir <dir>

The list holds one model path per line.
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from _common import cli_args, reset, import_model  # noqa: E402

p = argparse.ArgumentParser()
p.add_argument("--list", required=True)
p.add_argument("--outdir", required=True)
a = p.parse_args(cli_args())
out = Path(a.outdir)
out.mkdir(parents=True, exist_ok=True)
for line in Path(a.list).read_text(encoding="utf-8").splitlines():
    model = line.strip()
    if not model:
        continue
    reset()
    import_model(model)
    pos, nrm, uvs, bones = [], [], [], []
    for o in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
        groups = {g.index: g.name for g in o.vertex_groups}
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
            weight = {}
            for v in tri.vertices:
                for g in mesh.vertices[v].groups:
                    weight[g.group] = weight.get(g.group, 0) + g.weight
            bones.append(groups.get(max(weight, key=weight.get), "") if weight else "")
    target = out / (Path(model).stem + ".npz")
    np.savez(target, pos=np.array(pos, dtype=np.float32), nrm=np.array(nrm, dtype=np.float32), uv=np.array(uvs, dtype=np.float32), bone=np.array(bones))
    print(f"UV_TRIANGLES {Path(model).stem} count={len(pos)}")
print("UV_TRIANGLES_MANY_OK")
