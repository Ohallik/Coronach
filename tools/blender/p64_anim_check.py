"""Render a strip of animation frames and measure limb-through-cloth penetration.

Built for the pilot-model loop: a skinned skirt is the failure a still render
cannot show, because the legs are inside the garment at rest and only break out
mid-stride. This renders evenly-spaced frames from the view that exposes it
(side, by default) and — because the eye is bad at judging a 2 cm poke — also
reports how far the leg vertices travel outside the skirt surface per frame.

  scripts\\blender.ps1 -Script tools\\blender\\p64_anim_check.py -- \
      --input art-src\\Generated\\P64Lily\\Lily_Meshy_Rigged_walking_fbx.fbx \
      --output docs\\polish\\P64-models\\lily-walk.png --frames 6
"""

import argparse
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
from mathutils import Vector
from _common import cli_args, import_model, mesh_objects, reset, world_bounds

VIEW_DEG = {"front": 0.0, "threequarter": 45.0, "side": 90.0, "back": 180.0}


def point_at(obj, target):
    obj.rotation_euler = ((Vector(target) - obj.location).to_track_quat("-Z", "Y")).to_euler()


def frame_range(action_filter=None):
    """Range of ONE named action when asked for. Taking min/max across every action
    in a 12-clip donor file scrubs a union of unrelated timelines and renders
    whichever clip happened to be assigned on import - so a walk check would not
    necessarily be looking at the walk."""
    chosen = None
    if action_filter:
        wanted = action_filter.lower()
        for action in bpy.data.actions:
            name = action.name.split("|")[-1].lower()
            if name == wanted or wanted in name:
                chosen = action
                break
        if chosen is None:
            print(f"P64_ANIM_WARN action '{action_filter}' not found; "
                  f"have {[a.name.split('|')[-1] for a in bpy.data.actions]}")
    if chosen is not None:
        for obj in bpy.context.scene.objects:
            if obj.type == "ARMATURE":
                if obj.animation_data is None:
                    obj.animation_data_create()
                obj.animation_data.action = chosen
        print(f"P64_ANIM_ACTION {chosen.name.split('|')[-1]}")
        start, end = chosen.frame_range
        return int(start), int(end)

    lo, hi = None, None
    for action in bpy.data.actions:
        start, end = action.frame_range
        lo = start if lo is None else min(lo, start)
        hi = end if hi is None else max(hi, end)
    if lo is None:
        return 1, 1
    return int(lo), int(hi)



def evaluated_bounds():
    """World-space bounds of the mesh as the renderer sees it, after modifiers and
    armature deformation. Object bound_box lies on rigs that carry scale in the
    bone hierarchy."""
    deps = bpy.context.evaluated_depsgraph_get()
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    found = False
    for obj in mesh_objects():
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        for v in mesh.vertices:
            w = ev.matrix_world @ v.co
            lo = Vector((min(lo.x, w.x), min(lo.y, w.y), min(lo.z, w.z)))
            hi = Vector((max(hi.x, w.x), max(hi.y, w.y), max(hi.z, w.z)))
            found = True
        ev.to_mesh_clear()
    if not found:
        raise SystemExit("P64_ANIM_FAIL no evaluated vertices")
    return lo, hi


def lowest_leg_gap(depsgraph):
    """Deepest penetration of any vertex below hip height through the widest
    silhouette at its own height — a coarse but falsifiable stand-in for a real
    cloth-fit test: if a leg leaves the skirt, some vertex at that height sits
    outside the hull the skirt defines there."""
    worst = 0.0
    for obj in mesh_objects():
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        world = [evaluated.matrix_world @ v.co for v in mesh.vertices]
        if not world:
            evaluated.to_mesh_clear()
            continue
        zs = [p.z for p in world]
        lo_z, hi_z = min(zs), max(zs)
        band = (hi_z - lo_z) * 0.06 or 0.01
        # Walk up the figure in bands; in each, the spread of |x| tells whether
        # anything juts past the mass at that height.
        z = lo_z
        while z < lo_z + (hi_z - lo_z) * 0.62:
            slab = [p for p in world if z <= p.z < z + band]
            if len(slab) > 8:
                xs = sorted(abs(p.x) for p in slab)
                median = xs[len(xs) // 2]
                worst = max(worst, xs[-1] - median * 1.9)
            z += band
        evaluated.to_mesh_clear()
    return worst


def assign_action(obj, action):
    """Assign an action so it actually evaluates on Blender 4.4+.

    Slotted Actions (4.4) made `animation_data.action = X` insufficient on its
    own: without a bound action_slot the action holds no channels for that ID and
    evaluation is a silent no-op. Symptom is brutal to diagnose because every
    other signal looks healthy — the action is listed, the frame range is right,
    the armature has the modifier — and 0 of 67 pose bones move.
    """
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

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--input", required=True)
    p.add_argument("--output", required=True)
    p.add_argument("--frames", type=int, default=6)
    p.add_argument("--view", default="side", choices=sorted(VIEW_DEG))
    p.add_argument("--action", default="", help="name (or substring) of the clip to render")
    p.add_argument("--size", type=int, default=520)
    args = p.parse_args(cli_args())

    reset()
    import_model(args.input)
    meshes = mesh_objects()
    if not meshes:
        raise SystemExit("P64_ANIM_FAIL no mesh objects")

    start, end = frame_range(args.action)
    print(f"P64_ANIM_RANGE start={start} end={end} actions={len(bpy.data.actions)}")

    # Measure the EVALUATED mesh, not object bounds. Quaternius animal rigs carry
    # bone-hierarchy scale, so an object's bound_box disagrees with what is
    # actually drawn (the project's own ~6x bounds law) — framing a camera off it
    # aims at empty space, which is exactly what the first run of this check did:
    # it rendered six panels of bare horizon and reported OK.
    lo, hi = evaluated_bounds()
    center = (lo + hi) * .5
    figure = max(hi.z - lo.z, .001)
    extent = max(hi.x - lo.x, hi.y - lo.y, figure, .001)
    print(f"P64_ANIM_BOUNDS size=({hi.x - lo.x:.3f},{hi.y - lo.y:.3f},{figure:.3f})")

    bpy.ops.mesh.primitive_plane_add(size=max(8.0, extent * 3.0), location=(center.x, center.y, lo.z - .01))
    ground = bpy.context.object
    mat = bpy.data.materials.new("Ground")
    mat.diffuse_color = (.16, .18, .21, 1)
    ground.data.materials.append(mat)

    bpy.ops.object.light_add(type="AREA", location=(3, -4, 5))
    key = bpy.context.object
    key.data.energy, key.data.shape, key.data.size = 900, "DISK", 5
    point_at(key, center)
    bpy.ops.object.light_add(type="AREA", location=(-3, 1, 3))
    fill = bpy.context.object
    fill.data.energy, fill.data.color, fill.data.size = 500, (.45, .65, 1), 4
    point_at(fill, center)

    scene = bpy.context.scene
    bpy.ops.object.camera_add(location=(0, 0, 0))
    camera = bpy.context.object
    scene.camera = camera
    fov = camera.data.angle
    radius = (extent * 0.62) / max(math.tan(fov * .5), .01)
    rad = math.radians(VIEW_DEG[args.view])
    camera.location = (
        center.x + radius * math.sin(rad),
        center.y - radius * math.cos(rad),
        center.z + figure * .10,
    )
    point_at(camera, center)
    print(f"P64_ANIM_CAM loc=({camera.location.x:.2f},{camera.location.y:.2f},{camera.location.z:.2f}) "
          f"center=({center.x:.2f},{center.y:.2f},{center.z:.2f}) radius={radius:.2f} extent={extent:.2f} "
          f"meshes={len(mesh_objects())} names={[o.name for o in mesh_objects()][:4]}")

    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = scene.render.resolution_y = args.size
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world = scene.world or bpy.data.worlds.new("AnimWorld")
    scene.world.color = (.10, .12, .16)
    scene.view_settings.look = "AgX - Medium High Contrast"

    out = Path(args.output).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)
    count = max(2, args.frames)
    panels, worst_overall = [], 0.0
    for index in range(count):
        frame = int(start + (end - start) * index / (count - 1)) if end > start else start
        scene.frame_set(frame)
        gap = lowest_leg_gap(bpy.context.evaluated_depsgraph_get())
        worst_overall = max(worst_overall, gap)
        panel = out.with_name(f"{out.stem}_f{index:02d}.png")
        scene.render.filepath = str(panel)
        bpy.ops.render.render(write_still=True)
        panels.append(panel)
        print(f"P64_ANIM_FRAME i={index} frame={frame} spread={gap:.4f}")

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
    print(f"P64_ANIM_OK frames={len(panels)} view={args.view} worst_spread={worst_overall:.4f} {out}")


if __name__ == "__main__":
    main()
