"""Cut Oriel's Bridle clamp (Art/Generated/Models/BridleClamp) into its base and its jaw, so the jaw can swing open on its hinge.

The kit clamp is one generated mesh (about a metre cube in its own units): a wide base with two hand seats along the bottom, a glass pillar
standing up one side, and the bronze arm that leaves the pillar's head, arches over and comes down the other side as the jaw onto the base.
The jaw is everything above the base's top (--cut-z) and short of the pillar (--cut-x, the pillar lies at higher X). Faces those two planes
cross are split, not dropped, so neither half has a hole along the cut, and the UVs and the one material stay as they were. Each half is
written as its own FBX in the model's own axes, so the two laid on the same transform make the whole clamp again.

  --inspect          print the model's bounds and three silhouettes as text (for choosing the cut), write nothing
  --cut-z, --cut-x   the two planes, in the model's own units after import
  --preview          a PNG of the cut: the jaw red, the base grey, from the front, the side and three quarters

  scripts/blender.ps1 -Script tools/blender/split_clamp_jaw.py -TimeoutSec 300 --input BridleClamp_clean.fbx --base Base.fbx --jaw Jaw.fbx --cut-z -0.22 --cut-x 0.28 --preview cut.png
"""
import argparse
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

from _common import cli_args, import_model, mesh_objects, reset  # noqa: E402

p = argparse.ArgumentParser()
p.add_argument("--input", required=True)
p.add_argument("--base")
p.add_argument("--jaw")
p.add_argument("--inspect", action="store_true")
p.add_argument("--cut-z", type=float, default=-0.22)
p.add_argument("--cut-x", type=float, default=0.28)
p.add_argument("--preview")
a = p.parse_args(cli_args())


def load():
    reset()
    import_model(a.input)
    found = mesh_objects()
    assert len(found) == 1, f"expected one mesh in {a.input}, found {len(found)}"
    return found[0]


source = load()
points = [source.matrix_world @ v.co for v in source.data.vertices]
low = Vector((min(v.x for v in points), min(v.y for v in points), min(v.z for v in points)))
high = Vector((max(v.x for v in points), max(v.y for v in points), max(v.z for v in points)))
print(f"CLAMP_BOUNDS min={tuple(round(c, 3) for c in low)} max={tuple(round(c, 3) for c in high)} verts={len(points)} faces={len(source.data.polygons)}")
print(f"CLAMP_SOURCE_TRANSFORM loc={tuple(round(c, 4) for c in source.location)} rot={tuple(round(c, 4) for c in source.rotation_euler)} scale={tuple(round(c, 4) for c in source.scale)}")


def silhouette(obj, u_axis, v_axis, cols=60, rows=30):
    grid = [[0] * cols for _ in range(rows)]
    for f in obj.data.polygons:
        c = obj.matrix_world @ f.center
        u = (c[u_axis] - low[u_axis]) / max(1e-6, high[u_axis] - low[u_axis])
        v = (c[v_axis] - low[v_axis]) / max(1e-6, high[v_axis] - low[v_axis])
        grid[min(rows - 1, int(v * rows))][min(cols - 1, int(u * cols))] += 1
    shades = " .:-=+*#%@"
    peak = max(max(r) for r in grid) or 1
    return "\n".join("".join(shades[min(9, int(9 * math.sqrt(n / peak) + (1 if n else 0)))] for n in grid[r]) for r in reversed(range(rows)))


if a.inspect:
    names = "XYZ"
    for (u, v) in ((0, 2), (1, 2), (0, 1)):
        print(f"SILHOUETTE {names[u]} across, {names[v]} up")
        print(silhouette(source, u, v))
    print("CLAMP_INSPECT_OK")
    sys.exit(0)

assert a.base and a.jaw, "--base and --jaw are required unless --inspect"
Z = Vector((0, 0, 1))
X = Vector((1, 0, 0))
at_z = Vector((0, 0, a.cut_z))
at_x = Vector((a.cut_x, 0, 0))


def part(obj, steps):
    """A copy of the mesh cut by each (point, normal, keep side) in turn, the planes in world space: keep 'above' keeps the side the normal
    points to. The result is back in the object's own space, so a part written with the source's transform lies where it lay in the source."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.transform(obj.matrix_world)
    for (co, no, keep) in steps:
        geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, clear_inner=keep == "above", clear_outer=keep == "below")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bm.transform(obj.matrix_world.inverted())
    return bm


def write(bms, name, path, tint):
    mesh = bpy.data.meshes.new(name)
    merged = bmesh.new()
    for bm in bms:
        scratch = bpy.data.meshes.new("scratch")
        bm.to_mesh(scratch)
        merged.from_mesh(scratch)
        bpy.data.meshes.remove(scratch)
    merged.to_mesh(mesh)
    merged.free()
    for m in source.data.materials:
        mesh.materials.append(m)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    # The source's own transform, and the clean intake's own export settings (p64_strip_embedded.py), so the importer gives each part the
    # node transform it gave the whole clamp and the parts can be laid on the whole one's transform as they are.
    obj.matrix_world = source.matrix_world.copy()
    obj.color = tint
    for other in bpy.context.scene.objects:
        other.select_set(other == obj)
    bpy.context.view_layer.objects.active = obj
    out = Path(path)
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(out), use_selection=True, object_types={"MESH"}, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             add_leaf_bones=False, bake_anim=False, path_mode="STRIP", embed_textures=False)
    print(f"CLAMP_PART {name} faces={len(mesh.polygons)} -> {out}")
    return obj


jaw = part(source, [(at_z, Z, "above"), (at_x, X, "below")])
base_low = part(source, [(at_z, Z, "below")])
base_pillar = part(source, [(at_z, Z, "above"), (at_x, X, "above")])
source.hide_render = True
source.hide_set(True)
jaw_obj = write([jaw], "ClampJaw", a.jaw, (.9, .2, .15, 1))
base_obj = write([base_low, base_pillar], "ClampBase", a.base, (.7, .7, .7, 1))
total = len(source.data.polygons)
print(f"CLAMP_CUT source={total} jaw={len(jaw_obj.data.polygons)} base={len(base_obj.data.polygons)}")

if a.preview:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "OBJECT"
    scene.display.shading.light = "STUDIO"
    scene.render.resolution_x = 1500
    scene.render.resolution_y = 500
    scene.render.image_settings.file_format = "PNG"
    world = bpy.data.worlds.new("w")
    scene.world = world
    cam_data = bpy.data.cameras.new("cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 1.4
    cam = bpy.data.objects.new("cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    centre = (low + high) / 2
    shots = []
    for i, (azimuth, elevation) in enumerate(((0, 8), (90, 8), (40, 30))):
        az, el = math.radians(azimuth), math.radians(elevation)
        offset = Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el))) * 4
        cam.location = centre + offset
        cam.rotation_euler = (-offset).to_track_quat("-Z", "Y").to_euler()
        scene.render.resolution_x = 500
        frame = str(Path(a.preview).with_suffix("")) + f"-{i}.png"
        scene.render.filepath = frame
        bpy.ops.render.render(write_still=True)
        shots.append(frame)
    print("CLAMP_PREVIEW " + " ".join(shots))
print("CLAMP_SPLIT_OK")
