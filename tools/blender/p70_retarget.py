"""Retarget a clip from one quadruped skeleton onto another by WORLD-SPACE DELTA.

WHY NOT COPY ROTATIONS. Two rigs authored by different people do not agree about
which way a bone's local axes point, so copying local rotation from one to the other
produces a folded mess. What IS comparable is how far a bone has turned away from its
own rest pose, measured in world space — that is a property of the motion, not of the
rig's conventions. So for every frame and every mapped pair:

    delta      = source_pose_world * inverse(source_rest_world)
    target_pose = delta * target_rest_world

and the result is converted back into the target's local space.

WHAT THIS CANNOT DO. The Quaternius animal rig drives its paws through IK bones
parented to the body, not through the leg chain; Meshy's legs are a plain FK chain
four links deep. A rotation delta cannot reproduce an IK solve, so any clip whose
legs matter — a walk, a gallop — will not survive this. It is aimed at the clips
whose motion lives in the spine, head and tail with the feet planted.

  scripts\\blender.ps1 -Script tools\\blender\\p70_retarget.py -- \\
      --source <donor.fbx> --target <rigged.fbx> --output <fbx> --actions Idle,Attack
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
from mathutils import Matrix, Quaternion, Vector
from _common import cli_args, import_model, mesh_objects, reset

#: Quaternius `AnimalArmature` → Meshy quadruped. Every Meshy quadruped shares this
#: skeleton, so the table is written once and serves the whole cast.
#:
#: The spine is a MERGE, not a rename: Quaternius spends Torso/Torso2/Torso3 and three
#: neck bones where Meshy has `chest` and `head`, so the source bone nearest each target
#: is the one that carries it — Torso2 for the chest, Neck3 for the head, because those
#: are where the motion actually accumulates.
#: Named skeleton maps, keyed by `--profile`. The target in every case is the Meshy
#: quadruped, which every Meshy animal shares, so a new source only needs a new column.
MAPS = {
    # Quaternius `AnimalArmature`. The spine is a MERGE: it spends Torso/Torso2/Torso3
    # and three neck bones where Meshy has `chest` and `head`, so the source bone nearest
    # each target carries it. Only the top two links of each leg are mapped — Quaternius
    # drives its paws through IK bones parented to the BODY, and those carry almost none
    # of the swing, so feeding them in cancels the motion the upper links provide.
    "quaternius": {
        "Hips": "Body",
        "chest": "Torso2",
        "head": "Neck3",
        "tail": "Tail1",
        "tailstart": "Tail2",
        "tail1": "Tail3",
        "tail2": "Tail4",
        "tail3": "Tail5",
        "frontleg": "FrontUpperLeg.L",
        "frontleg0": "FrontLowerLeg.L",
        "R_frontleg": "FrontUpperLeg.R",
        "R_frontleg0": "FrontLowerLeg.R",
        "backleg": "BackUpperLeg.L",
        "backleg0": "BackLowerLeg.L",
        "R_backleg": "BackUpperLeg.R",
        "R_backleg0": "BackLowerLeg.R",
    },
    # Rip Vertices "Realistic Furry Wolf". Pure FK — no IK bones anywhere in its fifty —
    # so unlike Quaternius the whole leg chain is real animation and ALL FOUR links map.
    # That is the difference that decides whether a running clip survives the trip.
    #
    # Its root is Spine2 at the chest with the hips hanging off it, the reverse of Meshy's
    # arrangement, so Hips takes the root and `chest` takes the bone that actually carries
    # the front legs.
    "wolf": {
        "Hips": "Spine2",
        "chest": "Spine4",
        "head": "head",
        "tail": "tail_1",
        "tailstart": "tail_2",
        "tail1": "tail_3",
        "tail2": "tail_5",
        "frontleg": "LegFront1.L",
        "frontleg0": "LegFront2.L",
        "frontleg1": "FrontFoot1.L",
        "frontleg2": "FrontFoot2.L",
        "R_frontleg": "LegFront1.R",
        "R_frontleg0": "LegFront2.R",
        "R_frontleg1": "FrontFoot1.R",
        "R_frontleg2": "FrontFoot2.R",
        "backleg": "LegBack1.L",
        "backleg0": "LegBack2.L",
        "backleg1": "BackFoot1.L",
        "backleg2": "BackFoot2.L",
        "R_backleg": "LegBack1.R",
        "R_backleg0": "LegBack2.R",
        "R_backleg1": "BackFoot1.R",
        "R_backleg2": "BackFoot2.R",
    },
}



def armature_of(objects):
    return next((o for o in objects if o.type == "ARMATURE"), None)


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


#: Leg root → the paw at the end of it, per chain. Used to measure a leg's reach and to
#: know which bone to pitch when the paw does not touch the ground.
LEG_CHAINS = [
    ("frontleg", "frontleg2"), ("R_frontleg", "R_frontleg2"),
    ("backleg", "backleg2"), ("R_backleg", "R_backleg2"),
]


MAX_STRETCH = 1.35


def solve_leg_lengths(dst, action, passes=8):
    """How much longer each leg has to be for its paw to reach the floor, in this clip.

    A rotation retarget carries joint ANGLES, and two animals with the same angles do
    not put their feet in the same place. Measured on the wolf walk retargeted onto the
    Meshy husky: the front paws finished the cycle 0.157 trunks in the air while the
    hind paws stood on the ground, against a 0.011 spread in the source clip. That is
    the animal walking on its toes at one end, and it is what reads as floating.

    WHY LENGTH. Three other fixes were tried and measured first. Pitching the leg swings
    the paw along an arc, so it costs more fore-aft travel than it buys height, and the
    solve ran into a 40-degree clamp still short of the floor. Dropping the body and
    curling the other three legs up needs those legs to give up reach they are already
    using. And copying the source's world bone DIRECTIONS instead of its rotation deltas
    -- the `--aim` mode, which removes the rest-pose difference between the two rigs
    entirely -- moved the walk from 0.157 to 0.124 trunks: the rest poses were never the
    problem. What is left is proportion. The husky's forelegs are genuinely short for
    its chest, and a bone's scale along its own axis is the one correction that buys
    height directly: the chain inherits it, the paw goes down by what the leg grew, and
    the cycle's timing and shape do not move.

    Solved by secant on a measured gap, per leg, against the floor the other paws
    already stand on -- so the deepest-reaching leg is never asked to change.
    """
    assign(dst, action)
    # Pose scale is not owned by the action until the action keys it, so the last value
    # written while solving the PREVIOUS clip is still on the bone. Measuring the next
    # clip through it reported an idle that wanted +29% legs when it wanted +5%.
    for root, _ in LEG_CHAINS:
        if root in dst.pose.bones:
            dst.pose.bones[root].scale = (1.0, 1.0, 1.0)
    first, last = (int(v) for v in action.frame_range)
    frames = list(range(first, last + 1))

    def lowest(paw):
        bone = dst.pose.bones.get(paw)
        if bone is None:
            return None
        out = []
        for f in frames:
            bpy.context.scene.frame_set(f)
            bpy.context.view_layer.update()
            out.append((dst.matrix_world @ bone.matrix).to_translation().z)
        return min(out)

    heights = {paw: lowest(paw) for _, paw in LEG_CHAINS if paw in dst.pose.bones}
    if len(heights) < 2:
        return {}, 1.0
    floor = min(heights.values())
    trunk = ((dst.matrix_world @ dst.pose.bones[LEG_CHAINS[0][0]].matrix).to_translation()
             - (dst.matrix_world @ dst.pose.bones[LEG_CHAINS[2][0]].matrix).to_translation()
             ).length or 1.0
    tol = trunk * 0.004

    scales = {}
    for root, paw in LEG_CHAINS:
        if root not in dst.pose.bones or paw not in heights:
            continue
        gap0 = heights[paw] - floor
        if gap0 <= tol:
            continue
        pb = dst.pose.bones[root]

        def gap_at(k):
            pb.scale = (1.0, k, 1.0)
            return lowest(paw) - floor

        samples = [(1.0, gap0), (1.12, gap_at(1.12))]
        best = min(samples, key=lambda q: abs(q[1]))
        for _ in range(passes):
            (k0, g0), (k1, g1) = samples[-2], samples[-1]
            if abs(g1 - g0) < 1e-9 or abs(best[1]) <= tol:
                break
            k = k1 - g1 * (k1 - k0) / (g1 - g0)
            k = max(0.8, min(MAX_STRETCH, k))
            if abs(k - k1) < 1e-5:
                break
            samples.append((k, gap_at(k)))
            best = min(samples, key=lambda q: abs(q[1]))
        pb.scale = (1.0, 1.0, 1.0)
        scales[root] = best[0]
        flag = " CLAMPED" if best[0] >= MAX_STRETCH - 1e-3 else ""
        print("P70_RETARGET_PLANT %s %s->%s gap=%+.4f -> %+.4f trunks, length x%.3f "
              "tries=%d%s" % (action.name, root, paw, gap0 / trunk, best[1] / trunk,
                              best[0], len(samples), flag))
    # NOT paired left against right. Averaging the two sides -- on the theory that a dog
    # with mismatched forelegs limps -- put half the float straight back, because the two
    # sides genuinely need different corrections: the rig's rest legs are symmetric in
    # height but not in splay, so one world rotation applied to two differently-aimed
    # rest legs lands the paws at two different heights. The asymmetry is in the RIG.
    return scales, trunk


def key_scales(dst, action, scales):
    if not scales:
        return
    assign(dst, action)
    lo, hi = (int(v) for v in action.frame_range)
    for root, k in scales.items():
        pb = dst.pose.bones[root]
        for f in (lo, hi):
            bpy.context.scene.frame_set(f)
            pb.scale = (1.0, k, 1.0)
            pb.keyframe_insert("scale", frame=f)


def plant_legs(dst, actions, inherit=None):
    """Solve and key the leg lengths, PER CLIP, then re-measure what is left.

    One set of lengths for the whole library was the first shape of this and it does not
    work: solved on the walk the correction is +12% and +24% on the forelegs, and the
    same numbers applied to the idle push the front paws through the floor and leave the
    hind ones 0.15 trunks in the air. How short a leg is depends on the configuration it
    is in, so it is solved where it is measured.

    What that costs is a proportion change across a transition -- the forelegs grow by
    about a tenth over the blend out of idle. That is a smooth stretch on a moving
    animal, and a far smaller lie than a dog whose feet do not reach the ground.
    """
    inherit = inherit or {}
    solved = {}
    for action in actions:
        scales, _ = solve_leg_lengths(dst, action)
        solved[action.name] = scales
        if not scales:
            print("P70_RETARGET_PLANT %s every paw already reaches the floor"
                  % action.name)
        key_scales(dst, action, scales)

    # Clips with no contact to measure -- a death sprawl -- borrow from a clip that has
    # it, so the legs do not change length on the way into them.
    for name, source in inherit.items():
        action = next((a for a in bpy.data.actions if a.name == name), None)
        if action is None or source not in solved:
            continue
        key_scales(dst, action, solved[source])
        print("P70_RETARGET_PLANT %s inherits %s's leg lengths" % (name, source))

    # Re-measure. A correction that is keyed but not effective is the failure mode this
    # whole exercise started from, and it is silent.
    for action in actions:
        residual, trunk = solve_leg_lengths(dst, action)
        left = max(residual.values(), default=1.0)
        print("P70_RETARGET_PLANT_CHECK %s levelled, %s"
              % (action.name, "flat" if left <= 1.005
                 else "still wants x%.3f" % left))


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--source", required=True, help="rig whose clips are being copied")
    p.add_argument("--target", required=True, help="rig receiving them")
    p.add_argument("--output", required=True)
    p.add_argument("--actions", required=True, help="comma-separated take names")
    p.add_argument("--profile", default="quaternius", choices=sorted(MAPS),
                   help="which source skeleton the clips are coming from")
    p.add_argument("--plant-legs", default="",
                   help="comma-separated CONTACT clips whose paws must reach the same "
                        "plane. A rotation retarget preserves joint angles, not foot "
                        "contact, so a leg the target rig is short on ends the cycle in "
                        "the air. Name only clips with feet on the ground.")
    p.add_argument("--keep", default="",
                   help="the TARGET's own takes to carry through, as NAME or NAME=NEW. "
                        "A body that came with a clip authored for its own proportions "
                        "has something no retarget can produce: feet that reach the "
                        "ground. Use it and retarget only what the body is missing.")
    p.add_argument("--aim", default="off", choices=("off", "legs", "all"),
                   help="copy the source bone's world DIRECTION rather than its rotation "
                        "away from rest. Use when the two rigs' rest poses differ enough "
                        "that carrying the difference through keeps the feet off the "
                        "ground.")
    p.add_argument("--planar-legs", action="store_true",
                   help="strip the SPLAY out of the leg chains — keep the fore-aft swing "
                        "and drop the sideways component. Two rigs rarely agree about a "
                        "leg bone's rest roll, and a world-space delta then reads part of "
                        "the swing as splay: measured on the wolf walk, the retargeted "
                        "track came out twice as wide as the source's. A quadruped's legs "
                        "swing in the sagittal plane, so the lateral part is error.")
    args = p.parse_args(cli_args())

    reset()
    before = set(bpy.context.scene.objects)
    import_model(args.source)
    src = armature_of(o for o in bpy.context.scene.objects if o not in before)
    if src is None:
        raise SystemExit("P70_RETARGET_FAIL source has no armature")
    src.name = "SOURCE_RIG"
    for obj in list(bpy.context.scene.objects):
        if obj.type == "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

    before = set(bpy.context.scene.objects)
    import_model(args.target)
    dst = armature_of(o for o in bpy.context.scene.objects if o not in before)
    if dst is None:
        raise SystemExit("P70_RETARGET_FAIL target has no armature")

    # PARENT FIRST. A pose bone's matrix is resolved against its parent's CURRENT pose,
    # so writing a child before its parent bakes the child against a stale parent and the
    # error compounds down the chain — which is how the first attempt folded the whole
    # animal into a flat dart.
    def depth_of(bone):
        n, b = 0, dst.data.bones[bone]
        while b.parent is not None:
            n, b = n + 1, b.parent
        return n

    MAP = MAPS[args.profile]
    pairs = sorted(((t, s) for t, s in MAP.items()
                    if t in dst.pose.bones and s in src.pose.bones),
                   key=lambda ts: depth_of(ts[0]))
    print(f"P70_RETARGET_MAP profile={args.profile} pairs={len(pairs)}/{len(MAP)} "
          f"missing={[t for t, s in MAP.items() if (t, s) not in pairs]}")
    if not pairs:
        raise SystemExit("P70_RETARGET_FAIL nothing mapped")

    # Rest matrices in world space, captured once.
    src_rest = {s: src.matrix_world @ src.data.bones[s].matrix_local for _, s in pairs}
    dst_rest = {t: dst.matrix_world @ dst.data.bones[t].matrix_local for t, _ in pairs}

    # The body's own lateral axis, taken from the spread of the target's rest pose: the
    # long horizontal axis is the spine, the other one is across it.
    xs = [m.to_translation().x for m in dst_rest.values()]
    ys = [m.to_translation().y for m in dst_rest.values()]
    lateral = Vector((1, 0, 0)) if (max(xs) - min(xs)) < (max(ys) - min(ys)) \
        else Vector((0, 1, 0))
    legs = {t for t in MAP if "leg" in t.lower() or "foot" in t.lower()}
    if args.planar_legs:
        print(f"P70_RETARGET_PLANAR lateral={tuple(lateral)} legs={len(legs)}")

    wanted = [a.strip() for a in args.actions.split(",") if a.strip()]
    made = []
    for name in wanted:
        action = next((a for a in bpy.data.actions
                       if a.name.split("|")[-1].lower() == name.lower()), None)
        if action is None:
            print(f"P70_RETARGET_MISS {name}")
            continue
        assign(src, action)
        start, end = (int(v) for v in action.frame_range)

        baked = bpy.data.actions.new(name)
        baked.use_fake_user = True
        assign(dst, baked)
        for frame in range(start, end + 1):
            bpy.context.scene.frame_set(frame)
            bpy.context.view_layer.update()
            for target, source in pairs:
                sb, db = src.pose.bones[source], dst.pose.bones[target]
                # ROTATION ONLY. The delta also carries translation and scale, and the
                # two skeletons are nothing like the same size — Quaternius bones are an
                # order of magnitude longer — so letting either through collapses the
                # target. What transfers between rigs is how far a bone TURNED.
                delta = ((src.matrix_world @ sb.matrix)
                         @ src_rest[source].inverted()).to_quaternion()
                want_world = (delta.to_matrix().to_4x4()
                              @ dst_rest[target].to_quaternion().to_matrix().to_4x4())
                if args.aim != "off" and (args.aim == "all" or target in legs):
                    # AIM, not delta. The delta preserves how far a bone TURNED FROM ITS
                    # OWN REST, which quietly preserves the difference between the two
                    # rest poses as well: the Meshy husky's forelegs rest further forward
                    # than the wolf's, so wolf angles keep them off vertical and the paw
                    # sweeps an arc whose lowest point never reaches the floor. Measured:
                    # the front paws finished the walk 0.157 trunks in the air while the
                    # hind paws stood on it, and the source clip's spread was 0.011.
                    #
                    # Aiming copies the source bone's world DIRECTION instead, so a
                    # foreleg that is vertical under the plant on the wolf is vertical
                    # under the plant here. It gives up the bone's twist about its own
                    # axis, which for a leg is nothing.
                    src_dir = ((src.matrix_world @ sb.matrix).to_quaternion()
                               @ Vector((0, 1, 0))).normalized()
                    rest_dir = (dst_rest[target].to_quaternion()
                                @ Vector((0, 1, 0))).normalized()
                    want_world = (rest_dir.rotation_difference(src_dir).to_matrix().to_4x4()
                                  @ dst_rest[target].to_quaternion().to_matrix().to_4x4())
                if args.planar_legs and target in legs:
                    # Keep the bone pointing where the delta wants FORE AND AFT, but
                    # return it to the plane its rest pose sits in. The bone's own Y axis
                    # is its direction; flatten that against the body's lateral axis and
                    # rebuild the rotation from rest to the flattened direction, so the
                    # swing survives and the splay does not.
                    rest_dir = (dst_rest[target].to_quaternion()
                                @ Vector((0, 1, 0))).normalized()
                    want_dir = (want_world.to_quaternion() @ Vector((0, 1, 0))).normalized()
                    flat = (want_dir - lateral * want_dir.dot(lateral))
                    if flat.length > 1e-4:
                        flat.normalize()
                        want_world = (rest_dir.rotation_difference(flat).to_matrix().to_4x4()
                                      @ dst_rest[target].to_quaternion().to_matrix().to_4x4())
                head = (dst.matrix_world @ db.matrix).to_translation()
                db.matrix = (dst.matrix_world.inverted()
                             @ (Matrix.Translation(head) @ want_world))
                bpy.context.view_layer.update()
                db.rotation_mode = "QUATERNION"
                db.keyframe_insert("rotation_quaternion", frame=frame)
        made.append((name, end - start + 1, len(baked.fcurves)))
        print(f"P70_RETARGET_BAKED {name} frames={end - start + 1} "
              f"fcurves={len(baked.fcurves)}")

    if not made:
        raise SystemExit("P70_RETARGET_FAIL no action baked")

    if args.plant_legs:
        wanted = [a.strip().lower() for a in args.plant_legs.split(",") if a.strip()]
        baked = {name.lower(): next(a for a in bpy.data.actions if a.name == name)
                 for name, _, _ in made}
        contact = [baked[w] for w in wanted if w in baked]
        # Anything not named is a clip with no ground contact to measure. It borrows the
        # first contact clip's lengths rather than being solved against a floor that is
        # really a shoulder.
        inherit = {name: contact[0].name for name, _, _ in made
                   if name.lower() not in wanted and contact}
        if contact:
            plant_legs(dst, contact, inherit)

    bpy.data.objects.remove(src, do_unlink=True)
    # The source armature is gone but ITS ACTIONS are not — removing an object does not
    # remove the actions it referenced, and `bake_anim_use_all_actions` exports every
    # action in the file. Left alone they ship beside the baked ones under the same
    # names, and Unity picks whichever it meets first.
    # The target's own takes, renamed into the slots the controller asks for. Done
    # before the purge, so what is kept is kept under its shipped name.
    kept = {}
    for spec in (q.strip() for q in args.keep.split(",") if q.strip()):
        was, _, now = spec.partition("=")
        now = now or was
        action = next((a for a in bpy.data.actions
                       if a.name.split("|")[-1].lower() == was.lower()), None)
        if action is None:
            raise SystemExit(f"P70_RETARGET_FAIL --keep '{was}' is not a take on the "
                             f"target: {[a.name for a in bpy.data.actions]}")
        action.name = now
        # A renamed action loses its bound slot in Blender 4.4+, and an action with no
        # slot evaluates as a silent no-op: right name, right frame range, no channels.
        assign(dst, action)
        kept[was] = now
        print(f"P70_RETARGET_KEEP '{was}' -> '{now}' fcurves={len(action.fcurves)}")

    # Only the baked takes survive. Keeping the target's own clip as well was how a
    # duplicate `Walking` shipped: the Meshy walk and the retargeted one had the same
    # name, and Unity takes whichever it meets first.
    keep = {name for name, _, _ in made} | set(kept.values())
    dropped = 0
    for action in list(bpy.data.actions):
        if action.name.split("|")[-1] not in keep:
            bpy.data.actions.remove(action)
            dropped += 1
    print(f"P70_RETARGET_PURGE dropped={dropped} kept={sorted(keep)}")

    out = Path(args.output).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(out), use_selection=False, apply_unit_scale=True,
        add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
        path_mode="COPY", embed_textures=False)

    reset()
    import_model(str(out))
    names = sorted(a.name.split("|")[-1] for a in bpy.data.actions)
    curves = sum(len(a.fcurves) for a in bpy.data.actions)
    groups = sum(len(o.vertex_groups) for o in mesh_objects())
    print(f"P70_RETARGET_OK {out.name} takes={names} fcurves={curves} "
          f"vertex_groups={groups} bytes={out.stat().st_size}")


if __name__ == "__main__":
    main()
