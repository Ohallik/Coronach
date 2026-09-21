"""Graft a rigged donor skeleton onto a rigless mesh, preserving bone names and clips.

WHY THIS EXISTS. Meshy's rigging endpoint is humanoid-only (probed 2026-08-07: a
quadruped returns HTTP 422 while a biped through the identical path succeeds), so
generated animals arrive as bare meshes. p60_autorig.py builds a fresh armature
from bbox fractions but binds everything below the leg line to a STATIC Root bone
-- feet planted by construction -- which is why that lane can only ever author
idles, never a walk.

The owned Quaternius animals already solve this. All twelve share one 22-path
leg/shoulder hierarchy (Fox vs Wolf agree on 52 of 53 full bone paths), and Unity
Generic clips bind BY TRANSFORM PATH -- so a clip authored on Fox drives any mesh
carrying Fox's bone names, and bones the clip never mentions are simply ignored.

So: take the donor's armature and its whole clip library, fit it to the new mesh,
bind with automatic weights, and export with the names untouched. The animal
inherits Walk and Gallop for zero credits and zero hand-authoring.

  scripts\\blender.ps1 -Script tools\\blender\\p64_donor_rig.py -- \
      --target art-src\\Generated\\P64Fox\\Hex_Meshy.fbx \
      --donor  art-src\\Quaternius_UltimateAnimatedAnimals\\FBX\\Fox.fbx \
      --output art-src\\Generated\\P64Fox\\Hex_Rigged.fbx --height 0.60
"""

import argparse
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
from mathutils import Vector
from _common import cli_args, import_model, mesh_objects, reset


def bounds_of(objects):
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for obj in objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            lo = Vector((min(lo.x, world.x), min(lo.y, world.y), min(lo.z, world.z)))
            hi = Vector((max(hi.x, world.x), max(hi.y, world.y), max(hi.z, world.z)))
    return lo, hi


# Donor bone <- the reference rig's bone that marks the SAME anatomical joint.
# The reference is the target's own auto-rig: whatever else it got wrong, an auto-rigger
# fits bones TO geometry, so it knows where this body's joints actually are.
JOINT_MAP = (
    ("L UpperArm", "frontleg"), ("L Forearm", "frontleg0"),
    ("L Hand", "frontleg1"), ("L Finger0", "frontleg2"),
    ("R UpperArm", "R_frontleg"), ("R Forearm", "R_frontleg0"),
    ("R Hand", "R_frontleg1"), ("R Finger0", "R_frontleg2"),
    ("L Thigh", "backleg"), ("L Calf", "backleg0"),
    ("L HorseLink", "backleg1"), ("L Foot", "backleg2"),
    ("R Thigh", "R_backleg"), ("R Calf", "R_backleg0"),
    ("R HorseLink", "R_backleg1"), ("R Foot", "R_backleg2"),
)


def reference_joints(path):
    """Where are this body's OWN joints, as fractions of its own bounding box?

    WHY FRACTIONS. The reference file, the donor and the fitted graft are all at different
    scales and origins; a fraction of the mesh's own bbox is the one coordinate all three
    agree on. Read before the graft's own import, because reset() wipes the scene.
    """
    reset()
    import_model(path)
    arm = next((o for o in bpy.context.scene.objects if o.type == "ARMATURE"), None)
    meshes = mesh_objects()
    if arm is None or not meshes:
        raise SystemExit(f"P64_GRAFT_FAIL reference {path} has no armature+mesh")
    points = [m.matrix_world @ v.co for m in meshes for v in m.data.vertices]
    lo = Vector((min(p[i] for p in points) for i in range(3)))
    hi = Vector((max(p[i] for p in points) for i in range(3)))
    size = Vector((max(hi[i] - lo[i], 1e-9) for i in range(3)))
    out = {}
    for _, reference in JOINT_MAP:
        bone = arm.data.bones.get(reference)
        if bone is None:
            continue
        world = arm.matrix_world @ bone.head_local
        out[reference] = Vector(((world[i] - lo[i]) / size[i] for i in range(3)))
    print(f"P64_GRAFT_REFJOINTS file={Path(path).stem} found={len(out)}/{len(JOINT_MAP)}")
    return out



CENTRE_JOINTS = ("L Forearm", "L Hand", "L Finger0", "R Forearm", "R Hand", "R Finger0")


def limb_centre(points, target, link=0.02):
    """The centre of the limb the joint is supposed to be INSIDE.

    WHY THIS EXISTS. --match-joints places a joint at a fraction of a reference rig's
    bounding box, and --match-axes then lets only some of those axes through. Height-only
    was chosen because taking x and z from a reference whose mesh is not the mesh being
    rigged put the shoulders 0.213 forward and 0.043 right of centre. But height-only keeps
    the DONOR's fore-aft placement, and the donor's bones sit where the DONOR's leg is.

    Measured on Martok, that left every foreleg bone from the elbow down OUTSIDE the mesh --
    93.7mm behind the limb at the elbow, 116.2 at the carpus, 138.1 at the toe, against a
    limb radius of 40-66mm. A hinge outside the limb does not crease it, it shears it: posing
    the forearm at the clip's peak flexion moves every vertex of the lower leg 89.8mm
    depending only on which pivot is used. That is what "rubbery" was.

    So take the height from the reference -- that part was right, and it is what stopped the
    bowing -- and take the other two axes from the MESH: the centre of the limb's own
    cross-section at that height. No reference rig, no bounding box, nothing to be stale.

    Clustered by linking, not by a radius: at the height of a carpus the slab also contains
    the other three legs, and a fixed radius either misses the far side of the limb or
    swallows its neighbour.
    """
    slab = [p for p in points if abs(p[1] - target[1]) <= link]
    if len(slab) < 6:
        return None

    def flat(a, b):
        return math.hypot(a[0] - b[0], a[2] - b[2])

    seed = min(slab, key=lambda p: flat(p, target))
    cluster = [seed]
    remaining = [p for p in slab if p is not seed]
    grew = True
    while grew and remaining:
        grew = False
        keep = []
        for p in remaining:
            if any(flat(p, c) <= link for c in cluster):
                cluster.append(p)
                grew = True
            else:
                keep.append(p)
        remaining = keep
    if len(cluster) < 6:
        return None
    # FORE-AFT ONLY. The lateral placement is already right and is right for a reason:
    # it comes from the donor, whose limbs are symmetric, and it measured as exact L/R
    # mirrors with lateralMove 0.000. Taking lateral from the mesh's cross-section instead
    # re-introduces whatever asymmetry the generated body has -- measured, it doubled the
    # paw's lateral travel (0.081 -> 0.145 at the gallop against the donor's 0.068) while
    # fixing nothing, because the hinge error was entirely fore-aft.
    z = sum(p[2] for p in cluster) / len(cluster)
    return Vector((target[0], target[1], z))


def match_joints(armature, targets, fractions, axes="xyz", enclose=False):
    """Move the donor's limb joints onto the target's own anatomy.

    WHY. A uniform fit matches one number -- here nose-to-tail length -- and two animals of
    the same length are not the same animal. Martok is 25% taller and 33% wider than the
    donor wolf at equal length, and the consequence lands on the forelegs: the donor's
    shoulder anchor sat 8 points of body height BELOW where his mesh's shoulder is, and its
    rest leg covers 46.3 points of vertical drop where his own rig needs 57.6. The clips
    then articulate his leg around joints that are not where his joints are.

    This does NOT give him an elbow -- his foreleg is a smooth tube with no joint topology,
    and no skeleton can crease geometry that has nothing to crease along. What it does is
    stop the bend happening in the wrong PLACE.

    DO NOT USE THIS WITH UN-REBASED DONOR CLIPS. Tried on Martok 2026-08-25 and it works
    exactly as designed and ships a broken animal. The skeleton comes out right -- his
    joints land on his own anatomy to the decimal (58.2 / 34.6 / 13.5 / 5.1 percent of body
    height, the same numbers his auto-rig found) and the limb stops bowing: fold ratio goes
    1.013 -> 0.852 against the donor's 0.944, so it folds instead of curving.

    Then every clip breaks. A clip stores LOCAL rotations against the rest pose it was
    recorded on, so moving a joint changes what its rotations MEAN -- the legs collapsed
    into slabs and the body crumpled. The measurement that looked so good was taken by
    posing bones directly, which is rest-relative and therefore blind to precisely this.

    To use it, the donor's clips have to be rebased onto the new rest first
    -- and NOT by the tempting single pre-multiplier restTarget . restDonor^-1 . animated,
    which preserves the joint angle in the BONE's own frame and therefore lets the swing
    axis rotate with the bone. Moving a joint here rotates the rest frame 95-180 degrees
    while moving the joint itself about 1% of body height, so that form turns a fore-aft
    stride into a sideways one. It shipped on Martok 2026-08-25 and had to be pulled.
    The correct rebase is a two-sided sandwich carrying the deviation in the PARENT's frame,
        ln_b = inv(Rn_p) . Ro_p . lo_b . inv(Ro_b) . Rn_b
    with R the WORLD rest rotations; see Polish72Martok.RestCorrections. Both forms agree at
    the rest pose, so a bind-pose check cannot tell them apart -- assert the world deviation
    at a MOVING frame instead. Rebasing means baking per-body clips
    instead of sharing one library -- a different and much larger job. Until that exists
    this flag is off by default and should stay off.
    """
    if not fractions:
        return 0
    points = [m.matrix_world @ v.co for m in targets for v in m.data.vertices]
    lo = Vector((min(p[i] for p in points) for i in range(3)))
    hi = Vector((max(p[i] for p in points) for i in range(3)))
    size = Vector((max(hi[i] - lo[i], 1e-9) for i in range(3)))

    # Which side is which? Decided by measurement, not by the letter in the name: the
    # reference rig's unprefixed chain is not guaranteed to be the donor's "L".
    def world_of(fraction):
        return Vector((lo[i] + fraction[i] * size[i] for i in range(3)))

    left = fractions.get("frontleg")
    donor_left = armature.data.bones.get("L UpperArm")
    flip = False
    if left is not None and donor_left is not None:
        centre = (lo.x + hi.x) * 0.5
        ref_side = world_of(left).x - centre
        donor_side = (armature.matrix_world @ donor_left.head_local).x - centre
        flip = (ref_side * donor_side) < 0
    print(f"P64_GRAFT_JOINTSIDE flip={flip}")

    def counterpart(reference):
        if not flip:
            return reference
        if reference.startswith("R_"):
            return reference[2:]
        return "R_" + reference

    inverse = armature.matrix_world.inverted()
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode="EDIT")
    moved = 0
    rescued = 0
    try:
        bones = armature.data.edit_bones
        # SNAPSHOT FIRST. Every head/tail below is read against the ORIGINAL skeleton;
        # reading a bone's direction after its own head has already moved gives a vector
        # that is part old and part new, and where the new head lands near the old tail
        # that vector collapses to zero. A zero-length bone does not raise -- it takes the
        # FBX exporter down with no traceback and an empty log after the bind line.
        original = {b.name: (b.head.copy(), b.tail.copy()) for b in bones}
        wanted = {}
        for donor_name, reference in JOINT_MAP:
            fraction = fractions.get(counterpart(reference))
            if fraction is None or donor_name not in bones:
                continue
            placed = inverse @ world_of(fraction)
            head_before = original[donor_name][0]
            # PER-AXIS, because only one axis was ever the argument. The bowing this flag
            # exists to fix is a VERTICAL mis-placement -- the donor's shoulder sits 8
            # points of body height above where the mesh's is, and its rest leg covers 46.3
            # points of drop where this body needs 57.6. Nothing about that is a statement
            # about x or z.
            #
            # And taking x and z anyway is actively harmful, because the fractions are read
            # off a DIFFERENT MESH: the reference is the raw Meshy export and the body
            # being rigged has had its head straightened, which moves the nose and so moves
            # the bounding box the fractions are expressed in. Measured on Martok, taking
            # all three axes put both shoulders 0.213 forward and 0.043 to the RIGHT of
            # centre -- a symmetric animal given an asymmetric skeleton, one foreleg visibly
            # splayed, and a clavicle still aiming at where its shoulder used to be.
            wanted[donor_name] = Vector((
                placed[i] if "xyz"[i] in axes else head_before[i] for i in range(3)))

        # AND THEN PUT THE HINGE INSIDE THE LIMB.
        #
        # The flag is `enclose`, not `centre`: this function already binds a LOCAL named
        # `centre` to the mesh's x-midpoint a few lines above, which shadowed the parameter
        # and -- because a centred mesh puts that midpoint at ~0.0 -- made `if centre:`
        # quietly false. The block did not run and the export came back byte-identical.
        if enclose:
            points = [inverse @ (m.matrix_world @ v.co)
                      for m in targets for v in m.data.vertices]
            span = max((max(p[i] for p in points) - min(p[i] for p in points))
                       for i in range(3))
            centred = 0
            for donor_name in list(wanted):
                if donor_name not in CENTRE_JOINTS:
                    continue
                found = limb_centre(points, wanted[donor_name], link=span * 0.025)
                if found is None:
                    continue
                moved = (found - wanted[donor_name]).length
                wanted[donor_name] = found
                centred += 1
                print(f"P64_GRAFT_CENTRE {donor_name} movedOntoLimbAxis={moved:.4f}")
            print(f"P64_GRAFT_CENTRE joints={centred}/{len(CENTRE_JOINTS)}")

        for donor_name, target in wanted.items():
            bone = bones[donor_name]
            head_before, tail_before = original[donor_name]
            bone.head = target
            # The tail goes to the NEXT joint in the chain if that joint is also being
            # placed; otherwise it rides along with the head so the bone keeps its length
            # and direction.
            child = next((c for c in bone.children if c.name in wanted), None)
            bone.tail = wanted[child.name] if child is not None \
                else target + (tail_before - head_before)
            moved += 1

        # A parent whose tail used to meet this joint has to follow it, or the skeleton
        # comes apart exactly where it was being corrected.
        for donor_name, target in wanted.items():
            bone = bones[donor_name]
            parent = bone.parent
            if parent is None or parent.name in wanted:
                continue
            _, parent_tail = original[parent.name]
            head_before, _ = original[donor_name]
            if (parent_tail - head_before).length < 1e-6 or bone.use_connect:
                parent.tail = target

        for bone in bones:
            if (bone.tail - bone.head).length >= 1e-6:
                continue
            head_before, tail_before = original[bone.name]
            bone.tail = bone.head + (tail_before - head_before)
            if (bone.tail - bone.head).length < 1e-6:
                bone.tail = bone.head + Vector((0.0, 0.0, 1e-4))
            rescued += 1
    finally:
        bpy.ops.object.mode_set(mode="OBJECT")
    print(f"P64_GRAFT_JOINTS moved={moved}/{len(JOINT_MAP)} zeroLengthRescued={rescued}")
    return moved


def weld_weights(obj, weights, epsilon):
    """Give CO-LOCATED vertices in different shells one shared set of weights.

    WHY THIS EXISTS. A generated body is not one watertight mesh. Martok's is 530
    disconnected shells -- fur flakes and patches that only LOOK continuous because their
    boundary vertices sit exactly on top of one another. Nearest-surface transfer decides
    each of those vertices independently, so two verts at the same point can land on
    opposite sides of a donor bone boundary and follow different bones.

    At rest that is invisible: they coincide. It only opens when the bones they disagree
    about rotate apart, so it survives every still, every bind check and every slow clip,
    and then tears the shoulder open the first time a real trot reaches forward. Measured
    on Martok before this existed: 5,255 co-located cross-shell pairs, 532 of them (10.1%)
    diverging by more than 0.5, the worst by 1.5 -- entirely different bones ('L Forearm'
    vs 'L Finger0', 'R Foot' vs 'R HorseLink').

    Smoothing cannot reach it. The smoothing pass walks EDGES, and these vertices have no
    edge between them by definition -- that is what makes them different shells.

    This is a weld of the WEIGHTS only; the mesh keeps its shells, so the fur silhouette
    is untouched. Vertices at the same position have to move together whatever else is
    true, so averaging them cannot cost anything.
    """
    from collections import defaultdict

    grid = defaultdict(list)
    for vertex in obj.data.vertices:
        grid[tuple(int(vertex.co[i] // epsilon) for i in range(3))].append(vertex.index)

    parent = list(range(len(obj.data.vertices)))

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a

    joined = 0
    offsets = [(x, y, z) for x in (-1, 0, 1) for y in (-1, 0, 1) for z in (-1, 0, 1)]
    for vertex in obj.data.vertices:
        key = tuple(int(vertex.co[i] // epsilon) for i in range(3))
        for dx, dy, dz in offsets:
            for other in grid.get((key[0] + dx, key[1] + dy, key[2] + dz), ()):
                if other <= vertex.index:
                    continue
                if (vertex.co - obj.data.vertices[other].co).length > epsilon:
                    continue
                a, b = find(vertex.index), find(other)
                if a != b:
                    parent[a] = b
                    joined += 1

    clusters = defaultdict(list)
    for index in range(len(obj.data.vertices)):
        clusters[find(index)].append(index)

    welded = 0
    for members in clusters.values():
        if len(members) < 2:
            continue
        merged = {}
        for index in members:
            for key, value in weights[index].items():
                merged[key] = merged.get(key, 0.0) + value
        total = sum(merged.values())
        if total <= 1e-9:
            continue
        merged = {k: v / total for k, v in merged.items()}
        for index in members:
            weights[index] = dict(merged)
        welded += len(members)
    print(f"P64_GRAFT_WELD epsilon={epsilon:.6f} joins={joined} "
          f"clusters={sum(1 for m in clusters.values() if len(m) > 1)} verts_welded={welded}")
    return weights


def repair_displaced(obj, armature, weights, tolerance=0.12):
    """Move weight off bones that are no longer anywhere near the vertex holding them.

    ONLY MEANINGFUL AFTER --match-joints. Nearest-surface transfer copies the DONOR's
    spatial correspondence: a vertex at the hip takes the weights of whatever donor surface
    is nearest, and those weights name bones by the layout the DONOR had. Re-anchor a joint
    afterwards and that bone is somewhere else, so the vertex is following a bone it is no
    longer attached to. At rest it is invisible -- that is the bind pose by definition --
    and the moment the bone turns, the vertex is flung out. Rendered, it is a flat sheet of
    fur standing off the flank in one pose and gone in another.

    So: if a vertex's dominant bone is much further away than the nearest bone is, hand that
    weight to the nearest bone. Measured rather than named, because which vertices are
    affected depends on which joints moved and by how much.
    """
    # KEYED BY GROUP INDEX, because that is what cleanup_weights carries. Writing this
    # against bone NAMES made every lookup miss and the pass reported 0 reassigned while
    # doing nothing -- the same shape of silent no-op as the swallowed bpy.ops.
    lookup = {group.index: group.name for group in obj.vertex_groups}
    rest = {b.name: (b.head_local, b.tail_local) for b in armature.data.bones}
    bones = {index: rest[name] for index, name in lookup.items() if name in rest}
    if not bones:
        return 0
    reference = max((tail - head).length for head, tail in bones.values()) or 1.0
    inverse = armature.matrix_world.inverted()
    matrix = obj.matrix_world

    def distance(point, key):
        head, tail = bones[key]
        along = tail - head
        length = along.dot(along)
        if length < 1e-12:
            return (point - head).length
        t = max(0.0, min(1.0, (point - head).dot(along) / length))
        return (point - (head + along * t)).length

    moved = 0
    for index, entry in enumerate(weights):
        if not entry:
            continue
        dominant = max(entry, key=entry.get)
        if dominant not in bones:
            continue
        point = inverse @ (matrix @ obj.data.vertices[index].co)
        near_key, near_distance = min(
            ((key, distance(point, key)) for key in bones), key=lambda kv: kv[1])
        if near_key == dominant:
            continue
        dominant_distance = distance(point, dominant)
        # TWO CONDITIONS, BOTH REQUIRED, because either alone over-fires. An absolute
        # threshold alone reassigned 3,568 of 8,678 vertices -- 41% of the body -- which
        # is not repairing strays, it is throwing away the pack artist's skinning and
        # re-deriving it by proximity. Plenty of vertices are legitimately nearest to a
        # bone they should not follow: the belly sits nearest a thigh, the chest nearest
        # an upper arm. What marks a genuine mismatch is the dominant bone being far away
        # in BOTH senses -- a large absolute gap AND several times the nearest distance.
        if dominant_distance - near_distance <= tolerance * reference:
            continue
        if dominant_distance <= 3.0 * max(near_distance, 1e-6):
            continue
        entry[near_key] = entry.get(near_key, 0.0) + entry[dominant]
        del entry[dominant]
        moved += 1
    print(f"P64_GRAFT_DISPLACED tolerance={tolerance:.2f}*boneRef reassigned={moved} "
          f"of {sum(1 for e in weights if e)} weighted verts")
    return moved


def donor_reach(sources, armature, floor=0.25, margin=1.25):
    """How far, as a multiple of its own length, does each bone reach ON THE DONOR?

    A single global threshold cannot serve every bone. Calibrated on the donor's WORST bone
    it is 2.1x -- and at 2.1x the ear bones pass, because `Ear L/R` are 158mm segments lying
    across the crown, so an ear reaching a third of a metre is unremarkable as a multiple of
    its own length. Measured, that let Martok's ears own 579 and 327 vertices reaching 2.01
    and 1.71 bone-lengths, against the donor's 37 and 37 at 0.62 -- three times too much
    skin, and not even mirrored left to right. Ear bones that own skull vertices drag the
    skull: the head appears to widen at a sprint and the ears fold into each other at a walk.

    So calibrate PER BONE against the animal whose skinning was authored by hand. Both rigs
    carry the same 58 bones, so the donor's own reach for a bone is the best available
    statement of how far that bone is supposed to be felt.
    """
    out = {}
    rest = {b.name: (b.head_local, b.tail_local) for b in armature.data.bones}
    inverse = armature.matrix_world.inverted()
    for obj in sources:
        lookup = {g.index: g.name for g in obj.vertex_groups}
        matrix = obj.matrix_world
        for v in obj.data.vertices:
            point = inverse @ (matrix @ v.co)
            for g in v.groups:
                if g.weight < floor:
                    continue
                name = lookup.get(g.group)
                if name not in rest:
                    continue
                head, tail = rest[name]
                along = tail - head
                d2 = along.dot(along)
                if d2 < 1e-12:
                    continue
                t = max(0.0, min(1.0, (point - head).dot(along) / d2))
                gap = (point - (head + along * t)).length / math.sqrt(d2)
                if gap > out.get(name, 0.0):
                    out[name] = gap
    scaled = {k: v * margin for k, v in out.items()}
    worst = sorted(scaled.items(), key=lambda kv: -kv[1])[:3]
    print("P64_GRAFT_DONORREACH bones=%d margin=%.2f worst=%s"
          % (len(scaled), margin,
             ", ".join(f"{k} {v:.2f}" for k, v in worst)))
    return scaled


def donor_share(sources, armature, floor=0.25):
    """What FRACTION of the donor's skin does each bone actually influence?

    Reach in bone-lengths is not scale-invariant here. The armature is grafted at 1.0 while
    the target mesh is fitted, so Martok's body is 40% wider than the donor's on the same
    58 bones -- which makes almost every bone read ~2x the donor's reach without anything
    being wrong. Share is invariant: whatever the animal's size, an ear should own about as
    much of the skin as an ear owns.

    Measured, the donor's ears influence 37 of 1,591 vertices each -- 2.3% -- and Martok's
    own 583 and 350 of 8,678, which is 10.8% between them, 4.6x the proportion AND not
    mirrored. Ear bones owning that much skin own skull, and skull that follows an ear bone
    widens the head when the ears move.
    """
    counts = {}
    total = 0
    for obj in sources:
        lookup = {g.index: g.name for g in obj.vertex_groups}
        total += len(obj.data.vertices)
        for v in obj.data.vertices:
            for g in v.groups:
                if g.weight >= floor and g.group in lookup:
                    counts[lookup[g.group]] = counts.get(lookup[g.group], 0) + 1
    if total == 0:
        return {}
    return {k: v / total for k, v in counts.items()}


def limit_bone_share(obj, armature, weights, shares, bones_wanted, slack=1.0, floor=0.25):
    """Keep a bone's influence to the vertices NEAREST it, in the donor's proportion.

    Distance-based clipping cannot fix this: these vertices are DOMINATED by the offending
    bone and it is genuinely the nearest bone to them, so there is nothing to demote them
    to. What is wrong is how MANY of them there are. So rank by distance and keep the
    donor's share of them, which also repairs a left/right asymmetry for free -- both ears
    get the same budget.

    LIMIT A WHOLE CHAIN AT ONCE, never one bone of it. Surplus is re-homed to the nearest
    bone that is NOT being limited, so limiting Thigh and Calf alone simply handed the tail
    fur to HorseLink -- the worst edge stayed at 47x and the count of edges stretched past
    2x went UP, 1,336 to 1,792. With the whole hind chain limited the nearest unlimited bone
    is the pelvis or the tail, which is where that fur belongs.

    slack 1.0 = exactly the donor's proportion. 1.5 was tried first and left the ears at 350
    and 397 vertices, still well above the donor's 3.1% of the skin; the donor is the animal
    whose ears work, so its proportion is the target rather than a starting point.
    """
    lookup = {g.index: g.name for g in obj.vertex_groups}
    by_name = {n: i for i, n in lookup.items()}
    rest = {b.name: (b.head_local, b.tail_local) for b in armature.data.bones}
    inverse = armature.matrix_world.inverted()
    matrix = obj.matrix_world
    n = len(obj.data.vertices)
    trimmed = 0
    for name in bones_wanted:
        if name not in by_name or name not in rest or name not in shares:
            continue
        key = by_name[name]
        head, tail = rest[name]
        along = tail - head
        d2 = along.dot(along)
        holders = []
        for index, entry in enumerate(weights):
            if entry.get(key, 0.0) < floor:
                continue
            point = inverse @ (matrix @ obj.data.vertices[index].co)
            if d2 < 1e-12:
                gap = (point - head).length
            else:
                t = max(0.0, min(1.0, (point - head).dot(along) / d2))
                gap = (point - (head + along * t)).length
            holders.append((gap, index))
        budget = max(8, int(round(shares[name] * n * slack)))
        if len(holders) <= budget:
            continue
        holders.sort()
        # Where to send the surplus. Deleting is not an option: after the earlier passes
        # many of these vertices carry the ear and NOTHING else, and a vertex with no
        # weight collapses to the origin. They belong to whatever bone is next-nearest that
        # is not one of the bones being limited -- for an ear, that is the skull.
        others = {i: rest[nm] for i, nm in lookup.items()
                  if nm in rest and nm not in bones_wanted}

        def nearest_other(point):
            best, best_gap = None, 1e18
            for i, (h, t2) in others.items():
                seg = t2 - h
                s2 = seg.dot(seg)
                if s2 < 1e-12:
                    gap = (point - h).length
                else:
                    u = max(0.0, min(1.0, (point - h).dot(seg) / s2))
                    gap = (point - (h + seg * u)).length
                if gap < best_gap:
                    best, best_gap = i, gap
            return best

        for _, index in holders[budget:]:
            entry = weights[index]
            weight = entry.pop(key, 0.0)
            if not entry:
                point = inverse @ (matrix @ obj.data.vertices[index].co)
                host = nearest_other(point)
                if host is None:
                    entry[key] = weight
                    continue
                entry[host] = weight
            total = sum(entry.values())
            if total > 1e-9:
                weights[index] = {k: v / total for k, v in entry.items()}
            trimmed += 1
        print(f"P64_GRAFT_SHARE {name} held={len(holders)} budget={budget} "
              f"donorShare={shares[name] * 100:.1f}%")
    print(f"P64_GRAFT_SHARE trimmedInfluences={trimmed}")
    return trimmed


def mute_bones(obj, armature, weights, names):
    """Take a bone out of the skin entirely, handing its vertices to the nearest bone left.

    FOR A JOINT THE MESH CANNOT ARTICULATE. The donor's clips drive a jaw -- a running wolf
    runs with its mouth open -- and the donor has a jaw to drive: 155 of its 1,591 vertices,
    9.7% of the skin, a modelled lower jaw that swings as a unit. Martok has 31 of 8,678,
    0.36%. His muzzle is one solid piece, so the jaw does not open it, it TEARS it: the
    handful of vertices it owns swing down, the rest stay, and the gap between them is the
    mouth cavity showing through. Head-on at a gallop that reads as the snout warping.

    A joint with no geometry to move is better silent. The mouth then stays shut -- which
    is what the mesh was made as -- and nothing stretches.
    """
    lookup = {group.index: group.name for group in obj.vertex_groups}

    def wanted(bone_name):
        for n in names:
            if n.endswith("*"):
                if bone_name.startswith(n[:-1]):
                    return True
            elif bone_name == n:
                return True
        return False

    doomed = {i for i, n in lookup.items() if wanted(n)}
    names = {n for n in lookup.values() if wanted(n)}
    if not doomed:
        return 0
    rest = {b.name: (b.head_local, b.tail_local) for b in armature.data.bones}
    by_name = {n: i for i, n in lookup.items()}

    # For each muted group, the nearest ANCESTOR that is not itself muted.
    def ancestor(bone_name):
        bone = armature.data.bones.get(bone_name)
        while bone is not None:
            bone = bone.parent
            if bone is not None and bone.name not in names and bone.name in by_name:
                return by_name[bone.name]
        return None

    inherit = {i: ancestor(n) for i, n in lookup.items() if n in names}
    moved = 0
    for index, entry in enumerate(weights):
        held = [k for k in entry if k in doomed]
        if not held:
            continue
        weight = sum(entry.pop(k) for k in held)
        if not entry:
            # TO THE PARENT, NOT TO THE NEAREST. Silencing a bone means its skin belongs to
            # whoever would have held it in a simpler rig -- Jaw and Lip and Cheek all end
            # up on the Head. Picking the nearest bone instead is the Euclidean mistake all
            # over again: a dog's head sits right above its shoulders, so the nearest bone
            # to a cheek vertex can be the UPPER ARM. Measured, that put ear-to-upper-arm
            # edges in the worst-tear list at 5.86x.
            host = inherit.get(held[0])
            if host is not None:
                entry[host] = weight
            else:
                entry[held[0]] = weight   # nothing to inherit to: leave it be
        total = sum(entry.values())
        if total > 1e-9:
            weights[index] = {k: v / total for k, v in entry.items()}
        moved += 1
    print(f"P64_GRAFT_MUTED bones={sorted(names)} vertsReassigned={moved}")
    return moved


def drop_distant_kin(obj, armature, weights, max_hops=3):
    """Forbid a vertex from being shared by bones far apart in the SKELETON.

    Not a distance rule -- a GRAPH rule, and that is the point. Every proximity-based pass
    in this file has eventually mis-fired, because two body parts can touch without being
    related: a thigh beside tail fur, a shoulder beside a throat. The skeleton already
    encodes the relationship, and hop count does not care how the animal is folded.

    Measured on the bone-heat build, the worst residual tear was Head against L UpperArm --
    5.86x, 13 edges. Those are throat vertices the shoulder had picked up because they are
    physically adjacent. Head to L UpperArm is four hops (Head-Neck-Spine2-Clavicle-UpperArm),
    so a vertex owned by both is describing a body that does not exist. Head to Neck is one
    hop and stays.

    OFF BY DEFAULT, because on this body it did not work and the reason is worth keeping.
    The 13 Head/L UpperArm edges are not one vertex shared by two distant bones -- they are
    a vertex OWNED by Head sitting next to a vertex OWNED by L UpperArm. That is a hard seam
    through the throat, and the cure for a seam is a smoother transition across it, not
    fewer influences. Removing the intermediate bones hardened it: 66 -> 62 edges overall
    while the offending pair stayed at 13, and a fresh Tail05/L Calf tear appeared at 7.87x.
    Kept because the rule is sound where a single vertex really is shared by distant kin.
    """
    lookup = {group.index: group.name for group in obj.vertex_groups}
    bones = armature.data.bones
    index_of = {n: i for i, n in lookup.items()}

    def chain(bone):
        out = []
        while bone is not None:
            out.append(bone.name)
            bone = bone.parent
        return out

    ancestry = {b.name: chain(b) for b in bones}

    def hops(a, b):
        if a == b:
            return 0
        ca, cb = ancestry.get(a), ancestry.get(b)
        if not ca or not cb:
            return 0
        shared = set(cb)
        for i, name in enumerate(ca):
            if name in shared:
                return i + cb.index(name)
        return 99

    dropped = 0
    for entry in weights:
        if len(entry) < 2:
            continue
        keeper = max(entry, key=entry.get)
        home = lookup.get(keeper)
        if home is None:
            continue
        for key in [k for k in entry if k != keeper]:
            other = lookup.get(key)
            if other is None:
                continue
            if hops(home, other) > max_hops:
                del entry[key]
                dropped += 1
        total = sum(entry.values())
        if total > 1e-9:
            for k in entry:
                entry[k] /= total
    print(f"P64_GRAFT_KIN maxHops={max_hops} droppedInfluences={dropped}")
    return dropped


def clip_far_influences(obj, armature, weights, factor=2.1, behind=("Ear L", "Ear R"),
                        per_bone=None):
    """Drop an influence whose bone is nowhere near the vertex it is driving.

    WHY repair_displaced CANNOT DO THIS. It inspects each vertex's DOMINANT influence only
    -- one `max(entry, key=entry.get)` -- and asks both its gates about that one bone. A
    vertex correctly dominated by `Head` while also carrying 0.4 from `Neck` is invisible to
    it, because the dominant bone is fine. Measured on Martok, that let nearest-surface
    transfer bleed `Neck` weight 402.7mm out across the skull -- 2.5x the Neck bone's own
    162.1mm -- and hand 922 vertices to the ear bones with a reach of 335mm, against the
    donor's 76 vertices at 101mm. It reads as the head ballooning when the neck whips and
    the ears folding into each other at rest.

    The threshold is each bone's OWN length, not a global distance. `repair_displaced` uses
    `tolerance * reference` = about 33mm here, which is longer than some bones and a tenth
    of others; against its own length, a 160mm neck reaching 400mm is obviously wrong and a
    30mm ear reaching 30mm is obviously fine.

    2.1 is CALIBRATED ON THE DONOR, not chosen. Measured over influences of at least 0.25 --
    reach only matters for a weight heavy enough to drag the vertex, and the donor has far
    vertices too, at 0.015, where they do nothing -- the donor's own worst is Spine2 at
    2.09x its bone length, and its next five run 1.86 to 1.73. Martok before this pass:
    Tail04 2.76, Tail03 2.53, Neck 2.40, Tail05 2.38, Tail02 2.23, Head 2.15. So the donor's
    ceiling is the threshold: anything above it is bleed this pack's own artist did not
    author. Clipping at 1.0 as first proposed would have stripped the donor's own skinning.

    `behind` additionally clips a bone's weight from anything BEHIND its base. Distance
    alone cannot catch the ears: `Ear L/R` are long segments lying across the crown, so a
    temple vertex is genuinely within one bone length of one -- it is on the wrong SIDE of
    the ear's root, not far from it.
    """
    lookup = {group.index: group.name for group in obj.vertex_groups}
    rest = {b.name: (b.head_local, b.tail_local) for b in armature.data.bones}
    bones = {i: rest[n] for i, n in lookup.items() if n in rest}
    if not bones:
        return 0
    inverse = armature.matrix_world.inverted()
    matrix = obj.matrix_world

    def reach(point, key):
        head, tail = bones[key]
        along = tail - head
        length2 = along.dot(along)
        if length2 < 1e-12:
            return (point - head).length, 0.0, 1e-6
        t = (point - head).dot(along) / length2
        clamped = max(0.0, min(1.0, t))
        return (point - (head + along * clamped)).length, t, math.sqrt(length2)

    behind_keys = {i for i, n in lookup.items() if n in behind}
    dropped = 0
    rehomed = 0
    for index, entry in enumerate(weights):
        if len(entry) < 2:
            continue
        point = inverse @ (matrix @ obj.data.vertices[index].co)
        doomed = []
        for key in entry:
            if key not in bones:
                continue
            gap, t, length = reach(point, key)
            limit = (per_bone or {}).get(lookup.get(key), factor)
            if gap > limit * length:
                doomed.append(key)
            elif key in behind_keys and t < -0.05:
                doomed.append(key)
        # Never strip a vertex bare: the heaviest influence survives the DROP -- but if it
        # is itself out of reach it gets re-homed onto the nearest bone rather than kept.
        # Without this the pass stalls short of the target, because the vertices that
        # remain wrong are exactly the ones whose dominant bone is the distant one, and
        # repair_displaced's own two conditions are too strict to catch them.
        keeper = max(entry, key=entry.get)
        for key in doomed:
            if key == keeper or len(entry) < 2:
                continue
            del entry[key]
            dropped += 1
        if keeper in doomed and keeper in bones:
            nearest = min(bones, key=lambda k: reach(point, k)[0])
            gap, _, length = reach(point, nearest)
            _ = (per_bone or {}).get(lookup.get(nearest), factor)
            # ONLY ONTO A BONE THAT ACTUALLY OWNS THIS PLACE. "Nearest" is not the same as
            # "near": for a vertex out in the tail fur the nearest bone can still be a
            # hindquarter, and handing it the weight makes that scrap of fur fly off as a
            # flat sheet the moment the leg swings. If nothing is genuinely close, leaving
            # the far influence alone is the smaller error.
            if nearest != keeper and gap <= length:
                entry[nearest] = entry.get(nearest, 0.0) + entry.pop(keeper)
                rehomed += 1
        total = sum(entry.values())
        if total > 1e-9:
            weights[index] = {k: v / total for k, v in entry.items()}
    print(f"P64_GRAFT_CLIPPED perBone={per_bone is not None} "
          f"droppedInfluences={dropped} rehomedDominant={rehomed}")
    return dropped


def cleanup_weights(obj, armature=None, factor=0.5, repeat=3, limit=4, stray=0.6,
                    sharpen=1.0, floor=0.0, per_bone=None, shares=None, mute=None,
                    repair=True, max_hops=3):
    """Smooth, cap to four influences, renormalise -- WITHOUT bpy.ops.

    The operator forms of these three (vertex_group_smooth / _limit_total /
    _normalize_all) poll on the "object" CONTEXT MEMBER, which the Properties editor
    supplies and a background run does not -- so headless they raise "context is
    incorrect" no matter what is active or how the context is overridden. Silently
    skipping them is the worst outcome available: the bind contract, the bone count
    and the weighted-group count all still read healthy, while the mesh ships with
    more than four influences per vertex. Unity keeps four and drops the rest, and
    the vertex it truncated is no longer normalised -- it shrinks toward the origin
    whenever the dropped bone moves.

    SMOOTH BEFORE LIMITING. Nearest-surface transfer is per-vertex and knows nothing
    about limbs: where donor and target disagree in shape it hands a patch of one part
    the weights of another, and that patch then follows a bone its neighbours do not.
    On Martok that is the tail, which is bushier than the donor's, so its outer
    vertices find their nearest surface on the hindquarters. Smoothing blends those
    islands back into their surroundings; limiting first would lock the bad pick in.
    """
    mesh = obj.data
    count = len(mesh.vertices)
    weights = [dict() for _ in range(count)]
    for vertex in mesh.vertices:
        for group in vertex.groups:
            if group.weight > 0.0:
                weights[vertex.index][group.group] = group.weight

    neighbours = [[] for _ in range(count)]
    for edge in mesh.edges:
        a, b = edge.vertices
        neighbours[a].append(b)
        neighbours[b].append(a)

    # SMOOTH THE OUTLIERS ONLY, not the whole limb.
    #
    # Blending every vertex with its neighbours destroys the thing the transfer was
    # chosen for. Nearest-surface copies the pack artist's authored skinning, which is
    # CRISP -- measured on the Poly Art wolf's own foreleg: 1.81 influences per vertex,
    # mean max weight 0.880, and essentially nothing below the elbow carrying weight from
    # above it (p90 leak 0.000). Three rounds of 50% averaging turned that into 3.42
    # influences, max weight 0.716, and 16% of the shin following the upper arm at more
    # than 0.2. A lower leg that partly follows the upper arm does not pivot at the elbow,
    # it curves through it -- which reads exactly as a rubbery, over-long limb.
    #
    # What the smoothing is actually FOR is the stray patch: nearest-surface is per-vertex
    # and where donor and target disagree in shape it hands a scrap of one part the weights
    # of another, which then follows a bone its neighbours do not. That is an OUTLIER
    # problem. So only vertices that genuinely disagree with their neighbourhood get
    # blended, and a joint -- where neighbours are supposed to differ, but gradually --
    # is left alone.
    smoothed = 0
    for _ in range(repeat if repair else 0):
        blended = []
        for index in range(count):
            near = neighbours[index]
            if not near:
                blended.append(dict(weights[index]))
                continue
            average = {}
            for other in near:
                for key, value in weights[other].items():
                    average[key] = average.get(key, 0.0) + value
            share = 1.0 / len(near)
            average = {k: v * share for k, v in average.items()}
            divergence = sum(abs(weights[index].get(k, 0.0) - average.get(k, 0.0))
                             for k in set(weights[index]) | set(average))
            if divergence < stray:
                blended.append(dict(weights[index]))
                continue
            smoothed += 1
            merged = {}
            for key in set(weights[index]) | set(average):
                merged[key] = (weights[index].get(key, 0.0) * (1.0 - factor)
                               + average.get(key, 0.0) * factor)
            blended.append(merged)
        weights = blended
    print(f"P64_GRAFT_SMOOTH strayThreshold={stray:.2f} blends={smoothed} "
          f"of {count * repeat} vertex-passes")

    # WELD AFTER SMOOTHING, BEFORE LIMITING. Order is not cosmetic here and it was wrong
    # once: welding first and smoothing second undoes the weld exactly, because two
    # co-located vertices in different shells have different EDGE neighbours, so the
    # smoothing pass averages each back toward its own shell and they diverge again. The
    # measured divergence came back byte-identical (532 of 5,255 pairs) with the weld
    # running and reporting success. It has to be last of the two.
    #
    # Still before the 4-influence cap, because averaging two capped maps can produce
    # more than four again.
    span = max((max(v.co[i] for v in mesh.vertices) - min(v.co[i] for v in mesh.vertices))
               for i in range(3))
    # Also a TRANSFER repair: it exists because nearest-surface gives co-located
    # vertices in different shells different answers. Bone heat gives them the same
    # answer by construction, so welding can only blur it.
    if repair:
        weights = weld_weights(obj, weights, max(span * 1e-4, 1e-9))

    # After the weld, before the cap: a reassignment can only add influences, and the cap
    # is what guarantees Unity keeps them all.
    # THESE PASSES ARE REPAIRS FOR NEAREST-SURFACE TRANSFER, AND ONLY FOR THAT.
    #
    # repair_displaced and clip_far_influences both reason by EUCLIDEAN proximity, which is
    # precisely the logic that gives tail fur to a thigh -- they exist to undo the damage
    # transfer does. Bone heat solves through the mesh VOLUME and never makes that mistake,
    # so running them over bone-heat weights only breaks correct answers. Measured: auto
    # weights alone score 47 edges over 1.5x; auto weights with these passes score 1,247,
    # worst edge 3.40x -> 36.54x.
    # MUTING IS NOT A REPAIR -- it runs whatever produced the weights. A joint the mesh
    # cannot articulate tears it under bone heat exactly as it does under transfer.
    if armature is not None and mute:
        mute_bones(obj, armature, weights, mute)

    if armature is not None and max_hops:
        drop_distant_kin(obj, armature, weights, max_hops)

    if armature is not None and repair:
        repair_displaced(obj, armature, weights)
        # And then the influences repair_displaced structurally cannot see.
        clip_far_influences(obj, armature, weights, per_bone=per_bone)
        if shares:
            # Ears at the donor's exact proportion; the NECK with slack, because this
            # animal genuinely has a ruff the donor does not. Measured before this pass:
            # Neck held 904 of 8,678 vertices (10.4%) against the donor's 41 of 1,591
            # (2.6%) -- a 4x over-ownership, the same signature the ears had. A neck that
            # owns skull and muzzle warps the FACE whenever the neck whips, which is why
            # the complaint was "the head warps when starting to sprint".
            limit_bone_share(obj, armature, weights, shares, ("Ear L", "Ear R"), slack=1.0)
            # NOT THE NECK either -- tried, shipped, and it caused a worse defect.
            # Limiting Neck to 2x the donor's share re-homed the surplus onto SPINE2, whose
            # reach went 2.14 -> 2.61 bone-lengths (400mm, onto the HEAD) and which became
            # the worst bone on the body with 879 owned vertices. Same failure as the tail:
            # surplus goes to the nearest UNLIMITED bone, and for ruff vertices that is the
            # withers. Nathan reported "the neck looks messed up" the next day.
            # NOT THE TAIL. Tried and reverted, twice, and the measurements are worth
            # keeping because the failure is instructive.
            #
            # The tail IS the worst thing on this body: every one of the twelve
            # worst-stretching mesh edges is a tail bone paired with a hind-leg bone
            # (Tail04/L Calf, Tail03/L Thigh), stretching 28x to 47x, all on the LEFT --
            # which is exactly why it looks wrong running one direction and not the other.
            #
            # But share-limiting cannot fix it. Limiting Thigh+Calf moved the weight to
            # HorseLink and the worst edge stayed at 47x while edges over 2x went 1,336 ->
            # 1,792. Limiting the whole hind chain moved it to Spine: 68.9x and 1,874.
            # Surplus is re-homed to the nearest UNLIMITED bone, so every bone you limit
            # pushes the fur onto a bone further from it, and the tear gets longer.
            #
            # The real problem is that a bushy tail hanging between the hindquarters
            # occupies the same space as the thigh, and nearest-surface transfer has no way
            # to tell which is which. That needs a spatial rule -- tail fur is BEHIND the
            # pelvis, thigh is below it -- not a proportion.

    # SHARPEN, THEN CAP.
    #
    # Nearest-surface transfer onto a mesh five times denser than its source leaves every
    # vertex sharing itself between more bones than the source artist ever intended:
    # measured, 3.15 influences per vertex against the donor's own 2.15, and a
    # shoulder-to-elbow handover spread across 42% of the limb where the donor's covers
    # 33%. Nothing LEAKS -- below-elbow vertices following the upper arm measured 2.2% and
    # 0.0% -- so the usual leak test passes. What is wrong is subtler and shows only in
    # motion: with every vertex part-owned by its neighbours' bones, a SEGMENT does not
    # move rigidly. Measured under the gallop, the upper arm deformed 46.0% of its own
    # size against the donor's 29.1%, the forearm 46.7% against 26.4%, and the neck 11.0%
    # against 5.4% at the trot. A limb whose segments deform instead of pivoting is
    # exactly what "rubbery" describes, and a neck that deforms is a head that warps.
    #
    # w -> w**gamma renormalised leaves the dominant bone alone and pushes the passengers
    # down. It cannot fix a genuinely wrong assignment -- two bones at 0.50/0.49 stay
    # split, which is why it comes AFTER repair_displaced rather than instead of it.
    if sharpen > 1.0:
        for index in range(count):
            entry = weights[index]
            if len(entry) < 2:
                continue
            raised = {key: value ** sharpen for key, value in entry.items()}
            total = sum(raised.values())
            if total <= 1e-9:
                continue
            weights[index] = {key: value / total for key, value in raised.items()}

    # A floor drops passengers that survive the sharpen but contribute nothing except an
    # influence slot Unity has to spend.
    if floor > 0.0:
        for index in range(count):
            entry = {k: v for k, v in weights[index].items() if v >= floor}
            total = sum(entry.values())
            if entry and total > 1e-9:
                weights[index] = {k: v / total for k, v in entry.items()}

    for index in range(count):
        top = sorted(weights[index].items(), key=lambda kv: kv[1], reverse=True)[:limit]
        total = sum(value for _, value in top)
        weights[index] = {key: value / total for key, value in top} if total > 1e-9 else {}

    every = list(range(count))
    for group in obj.vertex_groups:
        group.remove(every)
    by_group = {}
    for index, entry in enumerate(weights):
        for key, value in entry.items():
            by_group.setdefault(key, []).append((index, value))
    groups = list(obj.vertex_groups)
    for key, entries in by_group.items():
        target = groups[key]
        for index, value in entries:
            target.add([index], value, "REPLACE")

    # NO VERTEX LEAVES WITHOUT A BONE.
    #
    # A weightless vertex is not "unbound", it is pinned at the ORIGIN and dragged there
    # from wherever the mesh is -- one spike per vertex, and the spike is invisible in the
    # bind pose because at rest the skinning matrix is identity. Bone heat leaves a few
    # (interior shells it cannot solve for), and the mute can strip the last influence off
    # a vertex that only a muted bone held. This is a FALLBACK for vertices that have
    # nothing, not a proximity repair of vertices that have something.
    if armature is not None:
        rest_bones = {b.name: (b.head_local, b.tail_local) for b in armature.data.bones}
        by_index = {g.index: g.name for g in obj.vertex_groups}
        usable = {i: rest_bones[n] for i, n in by_index.items() if n in rest_bones}
        inverse = armature.matrix_world.inverted()
        matrix = obj.matrix_world
        rescued = 0
        for index, entry in enumerate(weights):
            if entry or not usable:
                continue
            point = inverse @ (matrix @ mesh.vertices[index].co)
            best, best_gap = None, 1e18
            for i, (head, tail) in usable.items():
                seg = tail - head
                d2 = seg.dot(seg)
                if d2 < 1e-12:
                    gap = (point - head).length
                else:
                    t = max(0.0, min(1.0, (point - head).dot(seg) / d2))
                    gap = (point - (head + seg * t)).length
                if gap < best_gap:
                    best, best_gap = i, gap
            if best is not None:
                weights[index] = {best: 1.0}
                rescued += 1
        if rescued:
            print(f"P64_GRAFT_RESCUED weightlessVerts={rescued} "
                  f"(each would have collapsed to the origin)")

    influences = max((len(entry) for entry in weights), default=0)
    sums = [sum(entry.values()) for entry in weights if entry]
    print(f"P64_GRAFT_CLEANUP mesh={obj.name} max_influences={influences} "
          f"min_weight_sum={min(sums) if sums else 0.0:.4f} "
          f"unweighted_verts={sum(1 for entry in weights if not entry)}")


def strip_meshes(keep_armature):
    """Drop the donor's own body; the armature and its actions are the payload."""
    for obj in list(bpy.context.scene.objects):
        if obj.type == "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)
    return keep_armature


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
    p.add_argument("--target", required=True, help="rigless mesh to be rigged")
    p.add_argument("--donor", required=True, help="rigged FBX whose armature and clips are grafted")
    p.add_argument("--output", required=True)
    p.add_argument("--yaw", type=float, default=0.0,
                   help="degrees to rotate the TARGET about Z before fitting, if it faces the wrong way")
    p.add_argument("--height", type=float, default=0.0,
                   help="final height in metres (0 = leave at the fitted scale)")
    p.add_argument("--fit", choices=["length", "height"], default="length",
                   help="quadrupeds fit on nose-to-tail length; upright species fit on height")
    p.add_argument("--transfer-mapping", default="POLYINTERP_NEAREST",
                   help="vert_mapping for --weights transfer. Nearest-polygon won the "
                        "comparison on Martok, and it did NOT win on group count: "
                        "VNORPROJ scored 40 weighted groups against its 33 and rendered "
                        "as an exploded star of spikes, because a normal that misses the "
                        "source surface takes a weight from wherever it eventually lands. "
                        "A bind's quality is not its group count; the mid-stride render "
                        "is the only thing that settles it.")
    p.add_argument("--weights", choices=["auto", "transfer", "fold"], default="auto",
                   help="auto = bone-heat/envelope from scratch. transfer = copy the "
                        "DONOR MESH's authored weights onto the target by nearest "
                        "surface, which is the only way to inherit skinning the pack "
                        "artist hand-made (IK influences included).")
    p.add_argument("--skip-weight-repair", action="store_true",
                   help="skip the passes that exist to repair nearest-surface "
                        "TRANSFER -- outlier smoothing, repair_displaced, "
                        "clip_far_influences and the share limits. They all reason by "
                        "Euclidean proximity, which is the very mistake they are "
                        "undoing; run them over bone-heat weights and they only break "
                        "correct answers. Implied by --weights auto.")
    p.add_argument("--max-bone-hops", type=int, default=0,
                   help="forbid a vertex from being influenced by a bone more than this "
                        "many joints away, in the SKELETON, from the bone that owns it. "
                        "A graph rule rather than a distance one: a shoulder can sit "
                        "beside a throat without being related to it. 0 disables.")
    p.add_argument("--mute-bones", default="",
                   help="comma-separated bones to remove from the skin entirely, their "
                        "vertices handed to the nearest bone left. For a joint the DONOR "
                        "can articulate and this mesh cannot -- a jaw on a muzzle modelled "
                        "as one solid piece opens a tear rather than a mouth.")
    p.add_argument("--centre-joints", action="store_true",
                   help="after --match-joints, move the lower foreleg joints onto the "
                        "centre of the limb's OWN cross-section at their own height. "
                        "Height still comes from the reference rig; fore-aft and lateral "
                        "come from the mesh. Use whenever the reference rig belongs to a "
                        "different animal than the donor, which is always -- a bone "
                        "outside the limb shears it instead of creasing it.")
    p.add_argument("--weight-sharpen", type=float, default=1.0,
                   help="raise every weight to this power and renormalise, so a vertex "
                        "belongs more decisively to its dominant bone. 1.0 is off. Use it "
                        "when transferring onto a mesh much denser than the donor, where "
                        "the transfer spreads each vertex over more bones than the source "
                        "artist intended and SEGMENTS stop moving rigidly -- the defect "
                        "reads as rubbery limbs and a warping neck, and no leak test sees "
                        "it. Judge it with Polish72Martok.ProbeDeform, not by eye.")
    p.add_argument("--weight-floor", type=float, default=0.0,
                   help="drop influences below this weight after sharpening, then "
                        "renormalise. Frees influence slots for bones that matter.")
    p.add_argument("--match-axes", default="xyz",
                   help="which axes --match-joints is allowed to move a joint along. 'y' "
                        "takes only the height, which is the axis the bowing fix is "
                        "actually about, and leaves the donor's own symmetric x and z "
                        "alone -- the safe choice whenever the reference rig belongs to a "
                        "mesh that is not exactly the mesh being rigged.")
    p.add_argument("--match-chains", default="all", choices=["all", "front"],
                   help="which limbs --match-joints re-anchors. 'front' moves only the "
                        "foreleg chain. Fewer moved joints is fewer bones whose clips need "
                        "rebasing and fewer places for the transferred weights to disagree "
                        "with where the bones ended up, so it is the smaller blast radius "
                        "when only the forelegs are at fault.")
    p.add_argument("--match-joints", default="",
                   help="a rig of the TARGET body -- normally its own auto-rig -- whose "
                        "limb joints say where this animal's joints actually are. The "
                        "donor's limb joints are moved onto them after the fit. A uniform "
                        "fit matches ONE number, and two animals of the same length are "
                        "not the same animal. Moves the rest pose the donor's clips were "
                        "authored against, so re-judge the gait after using it.")
    p.add_argument("--donor-mesh", default="",
                   help="use only the donor mesh with this name as the weight source and "
                        "the proportion reference. Packs ship variants beside the body -- "
                        "the Poly Art wolf carries a 'Magic PA' twin occupying nearly the "
                        "same volume -- and --weights transfer runs every source in turn "
                        "with mix_mode=REPLACE, so the LAST one silently wins. Name the "
                        "body and the answer stops depending on import order.")
    p.add_argument("--mesh-name", default="",
                   help="rename the target mesh OBJECT to this. Some donor clips key the "
                        "mesh object's own transform as well as the bones, and Unity binds "
                        "those curves by path like any other — a graft whose mesh has a "
                        "different name reports them as unbound.")
    args = p.parse_args(cli_args())

    # BEFORE the reset below, which wipes the scene: the reference is a separate file and
    # only its numbers are needed downstream.
    fractions = reference_joints(args.match_joints) if args.match_joints else {}
    if args.match_chains == "front":
        keep = {ref for donor, ref in JOINT_MAP if "front" in ref}
        fractions = {k: v for k, v in fractions.items() if k in keep}
        print(f"P64_GRAFT_CHAINS front-only joints={len(fractions)}")

    reset()

    # --- donor: armature + clips, body discarded ------------------------------
    import_model(args.donor)
    armature = next((o for o in bpy.context.scene.objects if o.type == "ARMATURE"), None)
    if armature is None:
        raise SystemExit("P64_GRAFT_FAIL donor has no armature")
    donor_meshes = mesh_objects()
    if not donor_meshes:
        raise SystemExit("P64_GRAFT_FAIL donor has no mesh to measure proportions from")
    if args.donor_mesh:
        chosen = [o for o in donor_meshes if o.name == args.donor_mesh]
        if not chosen:
            raise SystemExit(f"P64_GRAFT_FAIL donor has no mesh named '{args.donor_mesh}' "
                             f"(has {[o.name for o in donor_meshes]})")
        for obj in donor_meshes:
            if obj not in chosen:
                bpy.data.objects.remove(obj, do_unlink=True)
        donor_meshes = chosen
        print(f"P64_GRAFT_DONORMESH kept={args.donor_mesh}")
    d_lo, d_hi = bounds_of(donor_meshes)
    donor_size = d_hi - d_lo
    actions = [a.name for a in bpy.data.actions]
    bone_count = len(armature.data.bones)
    # The donor's BODY is normally discarded here — the armature and its clips are the
    # payload. When transferring weights it is the payload too, so it survives until the
    # transfer is done and is deleted before the export.
    if args.weights == "transfer":
        for obj in donor_meshes:
            obj.hide_set(False)
        donor_weight_sources = list(donor_meshes)
        # MEASURED HERE, before the fit moves anything and before match_joints re-anchors:
        # this must be the donor's own pristine rest, which is the thing its skinning was
        # authored against.
        donor_limits = donor_reach(donor_weight_sources, armature)
        donor_shares = donor_share(donor_weight_sources, armature)
    else:
        strip_meshes(armature)
        donor_weight_sources = []
        donor_limits = None
        donor_shares = None

    print(f"P64_GRAFT_DONOR bones={bone_count} actions={len(actions)} "
          f"size=({donor_size.x:.3f},{donor_size.y:.3f},{donor_size.z:.3f})")

    # --- target mesh ----------------------------------------------------------
    before = set(bpy.context.scene.objects)
    import_model(args.target)
    targets = [o for o in mesh_objects() if o not in before]
    if not targets:
        raise SystemExit("P64_GRAFT_FAIL target contributed no meshes")
    # A target that arrived with its own skeleton would fight the donor.
    for obj in list(bpy.context.scene.objects):
        if obj.type == "ARMATURE" and obj is not armature:
            bpy.data.objects.remove(obj, do_unlink=True)
    for obj in targets:
        for modifier in list(obj.modifiers):
            if modifier.type == "ARMATURE":
                obj.modifiers.remove(modifier)
        obj.parent = None

    if abs(args.yaw) > 1e-6:
        rot = math.radians(args.yaw)
        for obj in targets:
            obj.rotation_euler.z += rot
        bpy.context.view_layer.update()

    if args.mesh_name:
        for index, obj in enumerate(targets):
            obj.name = args.mesh_name if index == 0 else f"{args.mesh_name}_{index}"
        print(f"P64_GRAFT_MESHNAME {args.mesh_name} objects={len(targets)}")

    t_lo, t_hi = bounds_of(targets)
    target_size = t_hi - t_lo
    print(f"P64_GRAFT_TARGET meshes={len(targets)} "
          f"size=({target_size.x:.3f},{target_size.y:.3f},{target_size.z:.3f})")

    # --- fit the armature to the mesh ----------------------------------------
    # Uniform scale only. A per-axis fit would shear limbs the moment they rotate,
    # which is the whole reason the language-level girth/reach knobs scale authored
    # numbers instead of finished geometry.
    if args.fit == "height":
        donor_ref, target_ref = donor_size.z, target_size.z
    else:
        donor_ref = max(donor_size.x, donor_size.y)
        target_ref = max(target_size.x, target_size.y)
    if donor_ref < 1e-6:
        raise SystemExit("P64_GRAFT_FAIL donor reference dimension is zero")
    scale = target_ref / donor_ref

    # Scale the MESH onto the armature, never the armature onto the mesh. An
    # armature exported at a scale other than 1 loses its skin weights through the
    # FBX writer -- the first version of this tool did exactly that and produced a
    # file with a correct skeleton, correct clips, a correct armature modifier and
    # ZERO vertex groups: a model that animated its bones and never moved its skin.
    inv = 1.0 / scale
    for obj in targets:
        obj.scale = tuple(s * inv for s in obj.scale)
    bpy.context.view_layer.update()

    t_lo, t_hi = bounds_of(targets)
    donor_centre = (d_lo + d_hi) * 0.5
    target_centre = (t_lo + t_hi) * 0.5
    for obj in targets:
        obj.location.x += donor_centre.x - target_centre.x
        obj.location.y += donor_centre.y - target_centre.y
        obj.location.z += d_lo.z - t_lo.z
    bpy.context.view_layer.update()
    print(f"P64_GRAFT_FIT mode={args.fit} mesh_scale={inv:.4f} (armature stays at 1.0)")

    # AFTER the uniform fit and BEFORE the bind: the fit puts the skeleton in roughly the
    # right place and this corrects the limbs onto the body's own joints, so the weights
    # are then solved against a skeleton that matches the geometry.
    match_joints(armature, targets, fractions, args.match_axes, args.centre_joints)

    # --- bind -----------------------------------------------------------------
    #
    # AUTO first, ENVELOPE as the fallback. Bone-heat weighting solves a diffusion
    # problem inside the mesh and it can simply fail — Blender says so in a warning
    # and then hands back vertex groups with no weights in them. That reads as a
    # successful bind on every count you can print (modifier present, 35 groups) and
    # the FBX writer drops the empty groups on export, so the failure only surfaces
    # as a rigged file with no skin. It is not exotic: it happens whenever the donor
    # skeleton does not sit inside solid geometry, which for a canopy of hanging
    # leaves over open roots is most of the body.
    #
    # Envelope weights are cruder — distance to the bone, no solve — but they always
    # produce values, and a slightly soft shoulder is worth having a body that moves.
    def bind(method):
        for obj in targets:
            for modifier in list(obj.modifiers):
                if modifier.type == "ARMATURE":
                    obj.modifiers.remove(modifier)
            obj.vertex_groups.clear()
            obj.parent = None
        bpy.ops.object.select_all(action="DESELECT")
        for obj in targets:
            obj.select_set(True)
        armature.select_set(True)
        bpy.context.view_layer.objects.active = armature
        bpy.ops.object.parent_set(type=method)
        bound = sum(1 for o in targets if any(m.type == "ARMATURE" for m in o.modifiers))
        # Count groups that actually CARRY weight, not groups that exist.
        weighted = set()
        for obj in targets:
            for vertex in obj.data.vertices:
                for g in vertex.groups:
                    if g.weight > 0.0:
                        weighted.add((obj.name, g.group))
        print(f"P64_GRAFT_BIND method={method} meshes_bound={bound}/{len(targets)} "
              f"vertex_groups={sum(len(o.vertex_groups) for o in targets)} "
              f"weighted_groups={len(weighted)}")
        return bound == len(targets) and len(weighted) > 0

    def transfer():
        """Copy the donor's authored weights instead of solving new ones.

        WHY THIS EXISTS. Bone-heat weighting does not reproduce a pack artist's
        skinning; it invents its own, and on the Quaternius animal rig it invents a
        WORSE one. Measured on the four party dogs: the Husky mesh carries weight on
        44 groups including the four IK bones its own clips drive, while the grafted
        bodies carried 30-36 and nothing on the IK bones at all. Worse, bone-heat
        preferred the `_end` LEAF bones — which no clip animates — over their parents,
        so a back leg had a thigh that swings, a paw welded to a bone that never
        moves, and geometry stretching to span the gap. That is the elongation.

        Nearest-surface transfer has none of that freedom. It asks "what is the pack
        mesh doing here" and copies the answer, so the graft inherits the IK weights,
        the shin weights and the tail chain exactly as authored.
        """
        for obj in targets:
            for modifier in list(obj.modifiers):
                if modifier.type == "ARMATURE":
                    obj.modifiers.remove(modifier)
            obj.vertex_groups.clear()
            obj.parent = None
        # ARMATURE_NAME creates one empty group per bone and no weights, which is the
        # blank the transfer fills. ARMATURE_AUTO here would put bone-heat's answer
        # underneath and the transfer would only partly cover it.
        bpy.ops.object.select_all(action="DESELECT")
        for obj in targets:
            obj.select_set(True)
        armature.select_set(True)
        bpy.context.view_layer.objects.active = armature
        bpy.ops.object.parent_set(type="ARMATURE_NAME")

        moved = 0
        for source in donor_weight_sources:
            bpy.ops.object.select_all(action="DESELECT")
            for obj in targets:
                obj.select_set(True)
            source.select_set(True)
            bpy.context.view_layer.objects.active = source
            bpy.ops.object.data_transfer(
                use_reverse_transfer=False,
                data_type="VGROUP_WEIGHTS",
                vert_mapping=args.transfer_mapping,
                layers_select_src="ALL",
                layers_select_dst="NAME",
                mix_mode="REPLACE")
            moved += 1

        weighted = set()
        for obj in targets:
            for vertex in obj.data.vertices:
                for g in vertex.groups:
                    if g.weight > 0.0:
                        weighted.add((obj.name, g.group))
        bound = sum(1 for o in targets if any(m.type == "ARMATURE" for m in o.modifiers))
        print(f"P64_GRAFT_BIND method=TRANSFER sources={moved} "
              f"meshes_bound={bound}/{len(targets)} "
              f"vertex_groups={sum(len(o.vertex_groups) for o in targets)} "
              f"weighted_groups={len(weighted)}")
        return bound == len(targets) and len(weighted) > 0

    def fold_leaf_groups():
        """Bone heat, then fold every `_end` LEAF group into its parent.

        The surgical version of the same fix. The measured defect was never that bone
        heat weighted too FEW bones — it was that it weighted the WRONG ones: it put
        the paws on `FF.L_end` and `FFB.R_end`, terminator bones that exist in the rig
        and that no clip animates. A paw welded to a bone that never moves, with a
        thigh that swings, is a leg that stretches.

        Folding a leaf's weights into its parent moves that geometry onto the bone the
        clips actually drive and touches nothing else. Full weight transfer rebuilds
        the ENTIRE skin from another animal's, which fixed the legs and broke the tail:
        Martok's is bushier than the Husky's, so its outer vertices found their nearest
        source surface on the flank and flared away when a hind leg swung. This cannot
        do that, because it only ever edits groups that were wrong by construction.
        """
        folded = 0
        for obj in targets:
            names = {g.name: g for g in obj.vertex_groups}
            for bone in armature.data.bones:
                if not bone.name.endswith("_end") or bone.parent is None:
                    continue
                leaf = names.get(bone.name)
                if leaf is None:
                    continue
                parent = names.get(bone.parent.name)
                if parent is None:
                    parent = obj.vertex_groups.new(name=bone.parent.name)
                    names[bone.parent.name] = parent
                moved = False
                for vertex in obj.data.vertices:
                    weight = next((g.weight for g in vertex.groups
                                   if g.group == leaf.index), 0.0)
                    if weight <= 0.0:
                        continue
                    have = next((g.weight for g in vertex.groups
                                 if g.group == parent.index), 0.0)
                    parent.add([vertex.index], min(1.0, have + weight), "REPLACE")
                    moved = True
                obj.vertex_groups.remove(leaf)
                names = {g.name: g for g in obj.vertex_groups}
                if moved:
                    folded += 1
        weighted = set()
        for obj in targets:
            for vertex in obj.data.vertices:
                for g in vertex.groups:
                    if g.weight > 0.0:
                        weighted.add((obj.name, g.group))
        print(f"P64_GRAFT_FOLD leaves_folded={folded} weighted_groups={len(weighted)}")
        return len(weighted) > 0

    if args.weights == "fold":
        if not bind("ARMATURE_AUTO"):
            print("P64_GRAFT_REBIND bone-heat left every group empty; "
                  "falling back to envelopes")
            if not bind("ARMATURE_ENVELOPE"):
                raise SystemExit(
                    "P64_GRAFT_FAIL neither automatic weights nor envelopes bound")
        fold_leaf_groups()
    elif args.weights == "transfer":
        if not transfer():
            print("P64_GRAFT_REBIND weight transfer produced nothing; falling back to bone heat")
            if not bind("ARMATURE_AUTO") and not bind("ARMATURE_ENVELOPE"):
                raise SystemExit("P64_GRAFT_FAIL no binding method produced weights")
        for obj in donor_weight_sources:
            bpy.data.objects.remove(obj, do_unlink=True)
    elif not bind("ARMATURE_AUTO"):
        print("P64_GRAFT_REBIND bone-heat left every group empty; falling back to envelopes")
        if not bind("ARMATURE_ENVELOPE"):
            raise SystemExit("P64_GRAFT_FAIL neither automatic weights nor envelopes bound")
    # CLEANUP RUNS FOR EVERY BINDING METHOD, not just transfer.
    #
    # It used to live inside transfer(), so --weights auto skipped the four-influence cap
    # -- and Unity keeps four, dropping the rest and leaving those vertices unnormalised,
    # which shrinks them toward the origin whenever the dropped bone moves. It also skipped
    # the mute, so facial micro-bones this mesh cannot articulate kept their weights.
    #
    # donor_limits / donor_shares are None outside transfer mode, and the passes that use
    # them skip cleanly: bone heat solves through the mesh VOLUME and does not produce the
    # cross-body mis-assignments those passes exist to repair.
    for obj in targets:
        cleanup_weights(obj, armature,
                        sharpen=args.weight_sharpen, floor=args.weight_floor,
                        per_bone=donor_limits, shares=donor_shares,
                        mute={n.strip() for n in args.mute_bones.split(',') if n.strip()},
                        repair=not (args.skip_weight_repair or args.weights == "auto"),
                        max_hops=args.max_bone_hops)

    groups = sum(len(o.vertex_groups) for o in targets)

    # Final metre height is deliberately NOT applied here: it would mean scaling
    # the armature again, which is what broke the weights. Set it on the prefab in
    # Unity, the same way BuildHuman does — height on a rigged body IS a scale.
    if args.height > 0:
        lo, hi = bounds_of(targets)
        current = hi.z - lo.z
        print(f"P64_GRAFT_HEIGHT_NOTE current={current:.3f} requested={args.height:.3f} "
              f"apply_scale_in_unity={args.height / max(current, 1e-6):.4f}")

    # Clips are deliberately NOT written into this file. Unity Generic clips bind
    # BY TRANSFORM PATH, so the donor FBX (already staged) supplies Walk/Gallop to
    # every grafted animal that carries its bone names — the same one-library
    # pattern the human cast already uses with UAL. Baking the donor's actions in
    # here instead meant renaming them to survive the exporter's name-prefixing,
    # and renaming an action in Blender 4.4+ orphans its action_slot, so the takes
    # exported dead. Shipping geometry-only removes that whole class of failure.
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    for obj in bpy.context.scene.objects:
        if obj.type == "ARMATURE" and obj.animation_data:
            obj.animation_data_clear()

    out = Path(args.output).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(out), use_selection=False, apply_unit_scale=True,
        # Match the donor-library exporter.  P60 Generic clips key the armature
        # object's scale as well as every bone; FBX_SCALE_NONE writes a metre-unit
        # graft that Unity compensates with AnimalArmature scale 100, then the
        # donor clip overwrites that compensation with its authored scale 1 and
        # collapses the skin by ~100x.  FBX_SCALE_ALL preserves UnitScaleFactor=100
        # and makes the cross-FBX local-transform contract honest.
        apply_scale_options="FBX_SCALE_ALL",
        add_leaf_bones=False, bake_anim=False,
        path_mode="COPY", embed_textures=True,
    )
    # --- verify what was WRITTEN, not what was intended ----------------------
    # The contract is the bone NAMES: if they match the donor exactly, the donor's
    # clips will drive this mesh in Unity. Re-import and compare.
    donor_bone_names = sorted(b.name for b in armature.data.bones)
    written = str(out)
    reset()
    import_model(written)
    re_meshes = mesh_objects()
    re_groups = sum(len(o.vertex_groups) for o in re_meshes)
    re_arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    re_bone_names = sorted(b.name for a in re_arms for b in a.data.bones)
    missing = [n for n in donor_bone_names if n not in set(re_bone_names)]
    bound_ok = re_groups > 0
    names_ok = not missing

    print(f"P64_GRAFT_VERIFY groups={re_groups} bones={len(re_bone_names)}/{len(donor_bone_names)} "
          f"missing_bones={len(missing)} verdict={'CONTRACT_OK' if (bound_ok and names_ok) else 'BROKEN'}")
    if missing[:6]:
        print(f"  MISSING: {missing[:6]}")
    if not (bound_ok and names_ok):
        raise SystemExit("P64_GRAFT_FAIL exported file breaks the bone-name contract "
                         "or carries no skin weights")

    print(f"P64_GRAFT_OK {out} bytes={out.stat().st_size} bones={bone_count} "
          f"clips=0 (supplied by the donor FBX in Unity) names_preserved=1")
    print("P64_GRAFT_UNIT_SCALE policy=FBX_SCALE_ALL donor_compatible=1")


if __name__ == "__main__":
    main()
