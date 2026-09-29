"""Remove a generated model's flat backing plate, and change nothing else.

Image-to-3D sometimes turns the white ground under a board cell into a thin
square slab: a separate loose part spanning the whole footprint at the very
bottom. On a cave floor it reads as a dark square. A loose part is removed only
when it is thinner than --max-thickness of the model's height AND covers at
least --min-cover of the footprint in both horizontal axes; anything else is
kept. Exits nonzero if no plate, or more than one, matches.

  scripts\\blender.ps1 -Script tools\\blender\\remove_base_plate.py --input <fbx> --output <fbx>
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bmesh
import bpy

from _common import cli_args, export_model, import_model, reset

p = argparse.ArgumentParser()
p.add_argument("--input", required=True); p.add_argument("--output", required=True)
p.add_argument("--max-thickness", type=float, default=.05); p.add_argument("--min-cover", type=float, default=.85)
a = p.parse_args(cli_args())
reset(); import_model(a.input)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
points = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
lo = [min(q[k] for q in points) for k in range(3)]; hi = [max(q[k] for q in points) for k in range(3)]
size = [hi[k] - lo[k] for k in range(3)]
up = 2  # Blender is Z-up after import
removed = 0
for o in meshes:
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
    seen = set(); doomed = []
    for v in bm.verts:
        if v in seen: continue
        stack = [v]; part = []
        while stack:
            u = stack.pop()
            if u in seen: continue
            seen.add(u); part.append(u); stack.extend(e.other_vert(u) for e in u.link_edges)
        world = [o.matrix_world @ u.co for u in part]
        plo = [min(q[k] for q in world) for k in range(3)]; phi = [max(q[k] for q in world) for k in range(3)]
        flat = [k for k in range(3) if k != up]
        thin = (phi[up] - plo[up]) <= a.max_thickness * size[up]
        bottom = plo[up] - lo[up] <= a.max_thickness * size[up]
        covers = all((phi[k] - plo[k]) >= a.min_cover * size[k] for k in flat)
        if thin and bottom and covers: doomed.extend(part)
    if doomed:
        bmesh.ops.delete(bm, geom=doomed, context='VERTS'); bm.to_mesh(o.data); o.data.update(); removed += 1
    bm.free()
if removed != 1: raise SystemExit(f"expected exactly one backing plate, found {removed}")
export_model(a.output)
print(f"BASE_PLATE_REMOVED {Path(a.input).name} -> {Path(a.output).name}")
