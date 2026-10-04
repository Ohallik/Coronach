"""Render a 4-view style-gate sheet (front / three-quarter / side / back) for a
generated character FBX.

The P36 single-view preview hid defects that only one angle showed; the
authoring rule this pass adopts is that every panel gets audited, so the
instrument produces every panel. Height is normalised by default so two
candidates compare fairly; pass --true-scale to judge a model at its shipped
metre height instead.

  scripts\\blender.ps1 -Script tools\\blender\\p64_preview.py -- \
      --input art-src\\Generated\\P64Lily\\Lily_Meshy.fbx \
      --output docs\\polish\\P64-models\\lily-meshy-4view.png
"""

import argparse
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector
from _common import cli_args, import_model, mesh_objects, reset

VIEWS = [("front", 0.0), ("threequarter", 45.0), ("side", 90.0), ("back", 180.0)]

#: Camera sits slightly above the subject so a quadruped's back reads; the old
#: code raised it by a fraction of the span, which tilted more the longer the
#: subject got. A fixed angle keeps all four panels on the same eyeline.
ELEVATION_DEG = 12.0
#: Headroom around the solved fit, so the silhouette never touches the frame edge.
MARGIN = 1.12


def point_at(obj, target):
    obj.rotation_euler = ((Vector(target) - obj.location).to_track_quat("-Z", "Y")).to_euler()


def build_lights(center):
    bpy.ops.object.light_add(type="AREA", location=(3, -4, 5))
    key = bpy.context.object
    key.data.energy, key.data.shape, key.data.size = 900, "DISK", 5
    point_at(key, center)
    bpy.ops.object.light_add(type="AREA", location=(-3, 1, 3))
    fill = bpy.context.object
    fill.data.energy, fill.data.color, fill.data.size = 500, (.45, .65, 1), 4
    point_at(fill, center)


def posed_bounds(meshes):
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in meshes:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        points.extend(evaluated.matrix_world @ v.co for v in mesh.vertices)
        evaluated.to_mesh_clear()
    return {"min": [min(p[i] for p in points) for i in range(3)],
            "max": [max(p[i] for p in points) for i in range(3)]}


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--input", required=True)
    p.add_argument("--output", required=True)
    p.add_argument("--size", type=int, default=760)
    p.add_argument("--true-scale", action="store_true",
                   help="render at the model's own height instead of normalising to 1.7 m")
    p.add_argument("--front-azimuth", type=float, default=0.0,
                   help="authored front direction in orbit degrees; defaults to the "
                        "established -Y/front convention")
    p.add_argument("--elevation", type=float, default=ELEVATION_DEG,
                   help="orbit elevation; a negative value exposes the underside")
    args = p.parse_args(cli_args())

    reset()
    import_model(args.input)
    # Freeze the imported pose before normalization. Rendering evaluates FBX
    # actions; an animated root scale would otherwise undo that normalization
    # after the camera had already been fitted, changing later views too.
    bpy.context.scene.frame_set(bpy.context.scene.frame_current)
    for obj in bpy.context.scene.objects:
        obj.animation_data_clear()
    meshes = mesh_objects()
    if not meshes:
        raise SystemExit("P64_PREVIEW_FAIL no mesh objects in input")

    bounds = posed_bounds(meshes)
    lo, hi = Vector(bounds["min"]), Vector(bounds["max"])
    height = max(.001, hi.z - lo.z)
    if not args.true_scale:
        for obj in bpy.context.scene.objects:
            if obj.parent is None:
                obj.scale *= 1.7 / height

    # Bounds and cameras must see the new root transform before fitting. Without
    # this update a normalized short character is cropped in every panel.
    bpy.context.view_layer.update()
    bounds = posed_bounds(meshes)
    lo, hi = Vector(bounds["min"]), Vector(bounds["max"])
    center = (lo + hi) * .5
    # Every extent matters. The old framing took max(x-extent, z-extent) and never
    # looked at Y, so a quadruped — whose long axis IS Y — was framed as if it were
    # as short as it is tall, and rendered as cropped fragments. Distance is now
    # solved per view from the bounds projected into that view's image plane
    # (below), and this span is only used for scene-scale things like the ground.
    span = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z, .001)
    corners = [Vector((x, y, z)) - center
               for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z, hi.z)]

    # The ground plane sits below the feet, not flush with them, and is shaded
    # through nodes. Setting `diffuse_color` alone leaves the default near-white
    # Principled BSDF in place under EEVEE, and white paws on a white floor read
    # exactly like legs that failed to reconstruct.
    bpy.ops.mesh.primitive_plane_add(size=span * 8, location=(center.x, center.y, lo.z - span * .02))
    ground = bpy.context.object
    ground.hide_render = args.elevation < 0
    mat = bpy.data.materials.new("Ground")
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (.16, .18, .21, 1)
    principled.inputs["Roughness"].default_value = 1.0
    mat.diffuse_color = (.16, .18, .21, 1)   # viewport only; the render reads the nodes
    ground.data.materials.append(mat)
    build_lights(center)

    bpy.ops.object.camera_add(location=(0, 0, 0))
    camera = bpy.context.object
    bpy.context.scene.camera = camera

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE" if bpy.app.version >= (5,0,0) else "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = scene.render.resolution_y = args.size
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world = scene.world or bpy.data.worlds.new("PreviewWorld")
    scene.world.color = (.10, .12, .16)
    scene.view_settings.look = "AgX - Medium High Contrast"

    # Orbit the camera rather than the model: rotating a skinned root drifts the
    # armature relative to its mesh, and the resulting panels would not be the
    # same asset seen from four sides.
    #
    # Distance is SOLVED, not a magic multiplier. `radius = span * 2.35` framed a
    # standing figure adequately and ran a long subject's head off the edge at
    # side-on angles, because the same number cannot fit both. For each view we
    # project the bounding-box corners onto that camera's right/up axes and back
    # off far enough for the frustum to contain them.
    fov = 2 * math.atan((camera.data.sensor_width * .5) / camera.data.lens)
    tan_half = math.tan(fov * .5)
    elev = math.radians(args.elevation)
    out = Path(args.output).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)
    panels = []
    for name, relative_deg in VIEWS:
        deg = args.front_azimuth + relative_deg
        rad = math.radians(deg)
        # Unit vector from the subject towards the camera.
        offset = Vector((math.sin(rad) * math.cos(elev),
                         -math.cos(rad) * math.cos(elev),
                         math.sin(elev)))
        forward = -offset                                    # camera looks along this
        right = forward.cross(Vector((0, 0, 1)))
        right = right.normalized() if right.length > 1e-6 else Vector((1, 0, 0))
        up = right.cross(forward).normalized()
        half_w = max(abs(c.dot(right)) for c in corners)
        half_h = max(abs(c.dot(up)) for c in corners)
        half_d = max(abs(c.dot(forward)) for c in corners)
        distance = max(half_w, half_h) / tan_half * MARGIN + half_d
        camera.location = center + offset * distance
        point_at(camera, center)
        bpy.context.view_layer.update()
        # Audit the actual posed mesh, rather than trusting an imported bounding
        # box. A cropped head makes a four-view review incomplete.
        depsgraph = bpy.context.evaluated_depsgraph_get()
        projected = []
        for obj in meshes:
            evaluated = obj.evaluated_get(depsgraph)
            posed = evaluated.to_mesh()
            projected.extend(world_to_camera_view(scene, camera, evaluated.matrix_world @ v.co) for v in posed.vertices)
            evaluated.to_mesh_clear()
        if any(p.z <= 0 or not .03 <= p.x <= .97 or not .03 <= p.y <= .97 for p in projected):
            raise RuntimeError(f"P64_FRAMING_REJECTED view={name} posed vertices touch or leave frame")
        panel = out.with_name(f"{out.stem}_{name}.png")
        scene.render.filepath = str(panel)
        bpy.ops.render.render(write_still=True)
        panels.append(panel)
        print(f"P64_PANEL_OK view={name} deg={deg:.0f} {panel}")

    strip = bpy.data.images.new("strip", width=args.size * len(panels), height=args.size)
    buf = [0.0] * (args.size * len(panels) * args.size * 4)
    for index, panel in enumerate(panels):
        img = bpy.data.images.load(str(panel))
        px = list(img.pixels)
        for row in range(args.size):
            src = row * args.size * 4
            dst = (row * args.size * len(panels) + index * args.size) * 4
            buf[dst:dst + args.size * 4] = px[src:src + args.size * 4]
        bpy.data.images.remove(img)
    strip.pixels = buf
    strip.filepath_raw = str(out)
    strip.file_format = "PNG"
    strip.save()
    print(f"P64_PREVIEW_OK views={len(panels)} height_m={height:.3f} {out}")


if __name__ == "__main__":
    main()
