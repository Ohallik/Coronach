"""Render a rigged body at several frames of a borrowed clip.

A skinning defect is invisible at rest and obvious mid-stride, so a rest-pose render
proves nothing about it. This borrows a clip from the donor the way the game does,
walks it, and renders the same camera at each sampled frame — which is the only way
to put "the back leg elongates when it swings back" in front of someone.

  scripts\\blender.ps1 -Script tools\\blender\\p70_posecheck.py `
      --input <graft.fbx> --donor <donor.fbx> --action Gallop `
      --outdir <dir> --label before [--frames 4] [--azimuth 90]
"""

import argparse
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
from mathutils import Vector
from _common import cli_args, import_model, mesh_objects, reset

ELEVATION_DEG = 8.0
MARGIN = 1.15


def point_at(obj, target):
    obj.rotation_euler = ((Vector(target) - obj.location).to_track_quat("-Z", "Y")).to_euler()


def assign(obj, action):
    if obj.animation_data is None:
        obj.animation_data_create()
    obj.animation_data.use_nla = False
    obj.animation_data.action = action
    slots = getattr(action, "slots", None)
    if slots:
        for slot in slots:
            try:
                obj.animation_data.action_slot = slot
                break
            except Exception:
                continue


def deformed_bounds(objects):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    depsgraph.update()
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for obj in objects:
        ev = obj.evaluated_get(depsgraph)
        for v in ev.data.vertices:
            w = ev.matrix_world @ v.co
            lo = Vector((min(lo.x, w.x), min(lo.y, w.y), min(lo.z, w.z)))
            hi = Vector((max(hi.x, w.x), max(hi.y, w.y), max(hi.z, w.z)))
    return lo, hi


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--input", required=True)
    p.add_argument("--donor", required=True)
    p.add_argument("--action", default="Gallop")
    p.add_argument("--outdir", required=True)
    p.add_argument("--label", required=True)
    p.add_argument("--frames", type=int, default=4)
    p.add_argument("--azimuth", type=float, default=90.0)
    p.add_argument("--elevation", type=float, default=ELEVATION_DEG,
                   help="camera height in degrees. A stance problem — legs too wide, feet "
                        "crossing — is invisible from the side and obvious from above.")
    p.add_argument("--size", type=int, default=420)
    p.add_argument("--flat", action="store_true",
                   help="override every material with neutral clay. A deformation "
                        "comparison is about SHAPE, and a stripped FBX renders magenta.")
    p.add_argument("--focus", choices=["all", "rear", "front"], default="all",
                   help="frame the whole body or half of it. `rear` is how you actually "
                        "look at a hind-leg bind.")
    args = p.parse_args(cli_args())

    reset()
    # Borrow the clip library, then throw the donor body away — the same trick the
    # deform check uses, and legal for the same reason: identical bone names.
    import_model(args.donor)
    for action in bpy.data.actions:
        action.use_fake_user = True
    for obj in list(bpy.context.scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    import_model(args.input)

    chosen = next((a for a in bpy.data.actions
                   if a.name.split("|")[-1].lower() == args.action.lower()), None)
    if chosen is None:
        raise SystemExit(f"P70_POSE_FAIL action '{args.action}' not present")
    arm = next((o for o in bpy.context.scene.objects if o.type == "ARMATURE"), None)
    if arm is None:
        raise SystemExit("P70_POSE_FAIL no armature")
    assign(arm, chosen)

    for material in bpy.data.materials:
        material.blend_method = "OPAQUE"

    if args.flat:
        clay = bpy.data.materials.new("Clay")
        clay.use_nodes = True
        principled = clay.node_tree.nodes.get("Principled BSDF")
        principled.inputs["Base Color"].default_value = (0.74, 0.72, 0.70, 1)
        principled.inputs["Roughness"].default_value = 0.62
        for obj in mesh_objects():
            obj.data.materials.clear()
            obj.data.materials.append(clay)

    meshes = mesh_objects()
    start, end = (int(v) for v in chosen.frame_range)
    frames = [int(start + (end - start) * i / max(1, args.frames)) for i in range(args.frames)]

    # Frame the camera on the WHOLE cycle, so every panel shares one view and a leg
    # that grows is a leg that grows rather than a camera that moved.
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for f in frames:
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        flo, fhi = deformed_bounds(meshes)
        lo = Vector((min(lo.x, flo.x), min(lo.y, flo.y), min(lo.z, flo.z)))
        hi = Vector((max(hi.x, fhi.x), max(hi.y, fhi.y), max(hi.z, fhi.z)))
    # The long axis of a quadruped on this rig is Y, and the head is at -Y.
    if args.focus == "rear":
        lo = Vector((lo.x, lo.y + (hi.y - lo.y) * 0.42, lo.z))
    elif args.focus == "front":
        hi = Vector((hi.x, hi.y - (hi.y - lo.y) * 0.42, hi.z))
    if hi.x < lo.x:
        raise SystemExit(f"P70_POSE_FAIL no vertices sampled from {len(meshes)} mesh(es)")
    centre = (lo + hi) * 0.5
    span = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z, 0.001)
    print(f"P70_POSE_BOUNDS meshes={len(meshes)} span={span:.4f} "
          f"centre=({centre.x:.3f},{centre.y:.3f},{centre.z:.3f})")
    corners = [Vector((x, y, z)) - centre
               for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z, hi.z)]

    bpy.ops.mesh.primitive_plane_add(size=span * 8,
                                     location=(centre.x, centre.y, lo.z - span * 0.01))
    ground = bpy.context.object
    mat = bpy.data.materials.new("Ground")
    mat.use_nodes = True
    mat.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (.15, .17, .2, 1)
    ground.data.materials.append(mat)

    # Lights are placed in SPAN units, not metres. A Meshy export lands at 1/100 scale —
    # two centimetres nose to tail — and a key light parked three metres away is then two
    # hundred body lengths away, which renders a black frame and looks exactly like a
    # broken rig. Distance and power both scale, so any subject is lit the same way.
    def lamp(offset, energy, size, colour=(1, 1, 1)):
        bpy.ops.object.light_add(type="AREA", location=(
            centre.x + offset[0] * span, centre.y + offset[1] * span,
            centre.z + offset[2] * span))
        light = bpy.context.object
        light.data.energy = energy * span * span
        light.data.shape, light.data.size = "DISK", size * span
        light.data.color = colour
        point_at(light, centre)

    lamp((0.9, -1.2, 1.5), 1100, 1.5)
    lamp((-0.9, 0.6, 0.9), 550, 1.2, (.45, .65, 1))

    bpy.ops.object.camera_add(location=(0, 0, 0))
    camera = bpy.context.object
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = scene.render.resolution_y = args.size
    scene.render.image_settings.file_format = "PNG"
    scene.world = scene.world or bpy.data.worlds.new("W")
    scene.world.color = (.10, .12, .16)
    scene.view_settings.look = "AgX - Medium High Contrast"

    # Clip planes scale with the subject. Blender's default near plane is 0.1 m, and a
    # Meshy export lands at 1/100 scale — two centimetres nose to tail — so the camera
    # that frames it sits five centimetres away, INSIDE the near plane, and every frame
    # renders as empty world colour. Indistinguishable from a broken rig until you
    # notice the ground plane is missing too.
    camera.data.clip_start = max(1e-4, span * 0.01)
    camera.data.clip_end = max(100.0, span * 200.0)

    fov = 2 * math.atan((camera.data.sensor_width * .5) / camera.data.lens)
    tan_half = math.tan(fov * .5)
    elev = math.radians(args.elevation)
    rad = math.radians(args.azimuth)
    offset = Vector((math.sin(rad) * math.cos(elev), -math.cos(rad) * math.cos(elev),
                     math.sin(elev)))
    forward = -offset
    right = forward.cross(Vector((0, 0, 1)))
    right = right.normalized() if right.length > 1e-6 else Vector((1, 0, 0))
    up = right.cross(forward).normalized()
    half_w = max(abs(c.dot(right)) for c in corners)
    half_h = max(abs(c.dot(up)) for c in corners)
    camera.location = centre + offset * (MARGIN * max(half_w, half_h) / tan_half + span * .5)
    point_at(camera, centre)

    out = Path(args.outdir).resolve()
    out.mkdir(parents=True, exist_ok=True)
    for i, f in enumerate(frames):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        scene.render.filepath = str(out / f"{args.label}_{i}.png")
        bpy.ops.render.render(write_still=True)
    print(f"P70_POSE_OK {args.label} action={args.action} frames={frames}")


if __name__ == "__main__":
    main()
