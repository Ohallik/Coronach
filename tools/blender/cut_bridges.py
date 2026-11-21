"""Cut the hair-thin strands image-to-3D sometimes spins between a figure's legs.

Rebuilt as drawn (without Meshy's T-pose mode), the Ancelane guard came back with two threads joining its knees. They are
part of the legs' own surface, so they can't be dropped as loose pieces; and once rigged they would stretch with every
step. This deletes every face that crosses the plane midway between the feet, below --below of the figure's height
(under the crotch), and every vertex lying within --half-width of that plane there, which cuts each thread and leaves
the legs untouched. Weights, UVs and the rest of the mesh are unchanged.

  scripts/blender.ps1 -Script tools/blender/cut_bridges.py -TimeoutSec 300 --input Body_clean.fbx --output Body_clean.fbx --below .38

It refuses to cut more than --most faces: that many across the gap means the gap isn't open there.
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bmesh
import bpy

from _common import cli_args, export_model, import_model, reset


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--input", required=True)
    p.add_argument("--output")
    p.add_argument("--half-width", type=float, default=.04, help="fraction of the stance's width either side of the plane")
    p.add_argument("--below", type=float, default=.38, help="fraction of the model's height, from the ground")
    p.add_argument("--most", type=int, default=120)
    args = p.parse_args(cli_args())
    reset()
    import_model(args.input)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    points = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
    lo = [min(v[k] for v in points) for k in range(3)]
    hi = [max(v[k] for v in points) for k in range(3)]
    # The plane midway between the feet: a shield on one arm and a lance on the other put the bounding box's centre elsewhere.
    feet = [v.x for v in points if v.z < lo[2] + (hi[2] - lo[2]) * .12]
    centre, reach, top = (min(feet) + max(feet)) / 2, (max(feet) - min(feet)) * args.half_width, lo[2] + (hi[2] - lo[2]) * args.below
    faces, verts = {}, {}
    for o in meshes:
        world = [o.matrix_world @ v.co for v in o.data.vertices]
        faces[o.name] = [f.index for f in o.data.polygons
                         if (o.matrix_world @ f.center).z < top
                         and min(world[i].x for i in f.vertices) < centre < max(world[i].x for i in f.vertices)]
        verts[o.name] = [i for i, v in enumerate(world) if abs(v.x - centre) < reach and v.z < top]
    count = sum(len(f) for f in faces.values())
    print(f"CUT_BRIDGES_FOUND faces={count} vertices={sum(len(v) for v in verts.values())} centre={centre:.3f} below={top:.3f}")
    if count > args.most:
        raise SystemExit(f"CUT_BRIDGES_REFUSED {count} faces cross the gap: it is not open")
    if not args.output or not count:
        return
    for o in meshes:
        mesh = bmesh.new()
        mesh.from_mesh(o.data)
        mesh.faces.ensure_lookup_table()
        mesh.verts.ensure_lookup_table()
        doomed = [mesh.verts[i] for i in verts[o.name]]
        bmesh.ops.delete(mesh, geom=[mesh.faces[i] for i in faces[o.name]], context="FACES")
        bmesh.ops.delete(mesh, geom=[v for v in doomed if v.is_valid], context="VERTS")
        mesh.to_mesh(o.data)
        mesh.free()
        o.data.update()
    export_model(args.output)
    print(f"CUT_BRIDGES_OK {Path(args.output).name} faces_removed={count}")


if __name__ == "__main__":
    main()
