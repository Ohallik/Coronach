"""Reduce a static generated prop to a triangle budget (collapse decimation, UVs kept).

Scatter pieces (plants, boulders) are placed by the dozen; Meshy's ~4,000-triangle
output is far more than a knee-high plant needs at the fixed 19.5 m gameplay
camera. Static meshes only: refuses anything with an armature.

  scripts\\blender.ps1 -Script tools\\blender\\decimate_static.py -- \
      --input <X_clean.fbx> --output <X_low_clean.fbx> --triangles 1200
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy

from _common import cli_args, export_model, import_model, mesh_objects, reset, triangle_count


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--input", required=True)
    p.add_argument("--output", required=True)
    p.add_argument("--triangles", type=int, required=True)
    args = p.parse_args(cli_args())

    reset()
    import_model(args.input)
    if any(o.type == "ARMATURE" for o in bpy.context.scene.objects):
        raise SystemExit("decimate_static refuses rigged models")
    meshes = list(mesh_objects())
    before = sum(triangle_count(o) for o in meshes)
    ratio = min(1.0, args.triangles / max(1, before))
    for obj in meshes:
        modifier = obj.modifiers.new("Decimate", "DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = ratio
        modifier.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    after = sum(triangle_count(o) for o in mesh_objects())
    export_model(args.output)
    print(f"DECIMATE_STATIC {Path(args.input).name} triangles {before} -> {after} ratio={ratio:.3f}")


if __name__ == "__main__":
    main()
