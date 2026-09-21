"""Prove a skinned clip actually deforms the mesh — no camera involved.

A graft can look correct on every count that is easy to print (bone count, clip
count, vertex groups bound) and still not move the skin: if automatic weights
land on the wrong bones, or the clip drives bones the mesh is not weighted to,
the model just stands there. A render answers this, but a render also depends on
framing, and framing is its own source of false negatives.

So measure the geometry directly: sample the evaluated mesh at several frames of
one named clip and report how far vertices actually travel. A static result is a
dead rig; a foot-sized excursion is a working one.

  scripts\\blender.ps1 -Script tools\\blender\\p64_deform_check.py -- \
      --input art-src\\Generated\\P64Probe\\Wolf_FoxGraft.fbx --action Walk
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
from mathutils import Vector
from _common import cli_args, import_model, mesh_objects, reset


def sample(frame):
    bpy.context.scene.frame_set(frame)
    deps = bpy.context.evaluated_depsgraph_get()
    # In background mode the depsgraph is NOT guaranteed to have re-evaluated after
    # a frame change; without this the armature deform is missed and a working rig
    # reads as STATIC.
    deps.update()
    pts = []
    for obj in mesh_objects():
        ev = obj.evaluated_get(deps)
        # ev.data is the POST-modifier mesh. ev.to_mesh() was returning geometry
        # that ignored the armature deform here, which made a working rig read as
        # STATIC — the native Quaternius Wolf, a shipped asset with a known-good
        # walk, measured 0.0000 vertex travel before this was corrected.
        pts.extend([ev.matrix_world @ v.co for v in ev.data.vertices])
    return pts


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
    p.add_argument("--action", default="Walk")
    p.add_argument("--samples", type=int, default=6)
    p.add_argument("--donor", default="",
                   help="FBX to borrow the clip library from, for a graft that "
                        "deliberately ships geometry-only")
    args = p.parse_args(cli_args())

    reset()
    if args.donor:
        # P70. The donor graft writes NO clips — Unity binds them by transform path
        # off the donor asset instead — so a graft has nothing here to test with.
        # Borrow the takes: import the donor, pin its actions with a fake user so
        # they survive their objects, then throw the donor away and bring in the
        # graft. Same bone names on both sides is exactly what makes this legal,
        # and it is the property under test.
        import_model(args.donor)
        for action in bpy.data.actions:
            action.use_fake_user = True
        for obj in list(bpy.context.scene.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
    import_model(args.input)

    chosen = None
    for action in bpy.data.actions:
        if action.name.split("|")[-1].lower() == args.action.lower():
            chosen = action
            break
    if chosen is None:
        print(f"P64_DEFORM_FAIL action '{args.action}' not present; "
              f"have {[a.name.split('|')[-1] for a in bpy.data.actions]}")
        return
    for obj in bpy.context.scene.objects:
        if obj.type == "ARMATURE":
            assign_action(obj, chosen)

    start, end = (int(v) for v in chosen.frame_range)
    frames = [int(start + (end - start) * i / max(1, args.samples - 1)) for i in range(args.samples)]

    # Separate "the rig is not moving" from "I am not seeing it move".
    arm = next((o for o in bpy.context.scene.objects if o.type == "ARMATURE"), None)
    if arm is not None:
        bpy.context.scene.frame_set(frames[0]); bpy.context.view_layer.update()
        p1 = [pb.matrix.to_translation().copy() for pb in arm.pose.bones]
        bpy.context.scene.frame_set(frames[len(frames)//2]); bpy.context.view_layer.update()
        p2 = [pb.matrix.to_translation().copy() for pb in arm.pose.bones]
        moved = sum(1 for a, b in zip(p1, p2) if (a - b).length > 1e-5)
        print(f"P64_DEFORM_POSE bones_moving={moved}/{len(p1)} action={arm.animation_data.action.name.split('|')[-1] if arm.animation_data and arm.animation_data.action else None}")

    base = sample(frames[0])
    if not base:
        print("P64_DEFORM_FAIL no vertices")
        return
    extent = max((max(p[i] for p in base) - min(p[i] for p in base)) for i in range(3))

    worst = 0.0
    per_frame = []
    for frame in frames[1:]:
        pts = sample(frame)
        if len(pts) != len(base):
            print("P64_DEFORM_FAIL vertex count changed between frames")
            return
        d = max((a - b).length for a, b in zip(pts, base))
        per_frame.append((frame, d))
        worst = max(worst, d)

    ratio = worst / extent if extent > 1e-6 else 0.0
    for frame, d in per_frame:
        print(f"  P64_DEFORM_FRAME frame={frame} max_vertex_travel={d:.4f}")
    # A walk swings a limb through a good fraction of the body's own size. Below
    # ~2% of extent nothing is really moving and the bind is dead.
    verdict = "DEFORMING" if ratio >= 0.02 else "STATIC"
    print(f"P64_DEFORM_{verdict} action={args.action} worst={worst:.4f} "
          f"extent={extent:.4f} ratio={ratio:.3f} verts={len(base)}")


if __name__ == "__main__":
    main()
