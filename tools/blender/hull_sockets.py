"""Measure where a generated flight hull's nose and engines are, for FlightSocketSetup.

The first two hulls' sockets were read off by hand. This prints them: the prow's tip, and the centre of each dark engine
well in the stern third (faces that look up or aft and are painted near black), already in the prefab's own space, as
GenIntake lays a hull out: nose along +Z, fitted to --length by depth, centred, keel on the ground.

  scripts\\blender.ps1 -Script tools\\blender\\hull_sockets.py -TimeoutSec 300 --input Hull_clean.fbx --texture Hull_albedo.png --length 3.6

--nose says which way the mesh's nose points in Blender (-X for every hull so far, the one GenIntake's yaw -90 turns to +Z).
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
from mathutils import Vector

from _common import cli_args, import_model, reset


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--input", required=True)
    p.add_argument("--texture", required=True)
    p.add_argument("--length", type=float, required=True)
    p.add_argument("--nose", choices=("-X",), default="-X")
    p.add_argument("--dark", type=float, default=.16, help="a well is painted darker than this (0-1 value)")
    args = p.parse_args(cli_args())
    reset()
    import_model(args.input)
    image = bpy.data.images.load(str(Path(args.texture).resolve()))
    width, height = image.size
    pixels = list(image.pixels)

    def value(uv):
        x = min(width - 1, max(0, int(uv[0] % 1 * width)))
        y = min(height - 1, max(0, int(uv[1] % 1 * height)))
        i = (y * width + x) * 4
        return max(pixels[i], pixels[i + 1], pixels[i + 2])

    faces = []   # (centre, normal, area, value) in world space
    points = []
    for obj in (o for o in bpy.context.scene.objects if o.type == "MESH"):
        mesh, world = obj.data, obj.matrix_world
        uvs = mesh.uv_layers.active.data
        points += [world @ v.co for v in mesh.vertices]
        for poly in mesh.polygons:
            tone = sum(value(uvs[i].uv) for i in poly.loop_indices) / len(poly.loop_indices)
            faces.append((world @ poly.center, (world.to_3x3() @ poly.normal).normalized(), poly.area, tone))
    lo = Vector((min(v.x for v in points), min(v.y for v in points), min(v.z for v in points)))
    hi = Vector((max(v.x for v in points), max(v.y for v in points), max(v.z for v in points)))
    scale = args.length / (hi.x - lo.x)
    centre = (lo + hi) / 2

    def prefab(v):
        """Blender's (x, y, z) with the nose at -X, as the prefab's (x right, y up, z forward)."""
        return ((v.y - centre.y) * scale, (v.z - lo.z) * scale, -(v.x - centre.x) * scale)

    print(f"HULL_BOUNDS width={(hi.y - lo.y) * scale:.3f} height={(hi.z - lo.z) * scale:.3f} length={args.length:.3f}")
    tip = [v for v in points if v.x < lo.x + (hi.x - lo.x) * .02]
    nose = sum(tip, Vector()) / len(tip)
    print("HULL_NOSE x={:.3f} y={:.3f} z={:.3f}".format(*prefab(nose)))
    stern = lo.x + (hi.x - lo.x) * .66
    for side, keep in (("port", lambda c: c.y < centre.y), ("starboard", lambda c: c.y >= centre.y)):
        wells = [(c, a) for c, n, a, tone in faces if c.x > stern and keep(c) and tone < args.dark and (n.z > .3 or n.x > .3)
                 and abs(c.y - centre.y) > (hi.y - lo.y) * .12]
        if not wells:
            print("HULL_ENGINE_NOT_FOUND", side)
            continue
        area = sum(a for _, a in wells)
        middle = sum((c * a for c, a in wells), Vector()) / area
        print("HULL_ENGINE {} x={:.3f} y={:.3f} z={:.3f} dark_area={:.4f}".format(side, *prefab(middle), area * scale * scale))


if __name__ == "__main__":
    main()
