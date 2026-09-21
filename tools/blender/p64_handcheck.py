"""Measure the wrist-end bones of a Meshy biped rig.

Unity's humanoid validator declined four of eighteen otherwise identical Meshy
skeletons, and declined them at the HANDS specifically — the name-level map is
accepted, the geometry is not. A hand bone carrying no length is a cause that a
name comparison cannot see, so measure it.

Prints one line per rig so eighteen of them can be compared at a glance.

  scripts\\blender.ps1 -Script tools\\blender\\p64_handcheck.py -- --input <fbx>
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
    p.add_argument("--label", default="")
    args = p.parse_args(cli_args())

    reset()
    import_model(args.input)
    label = args.label or Path(args.input).stem

    armature = next((o for o in bpy.context.scene.objects if o.type == "ARMATURE"), None)
    if armature is None:
        print(f"P64_HAND label={label} armature=NONE")
        return

    bones = {b.name: b for b in armature.data.bones}

    def span(name):
        bone = bones.get(name)
        return 0.0 if bone is None else (Vector(bone.tail_local) - Vector(bone.head_local)).length

    # Normalise by the forearm. These rigs ship at wildly different scales — the
    # crowd bases came back seven metres tall — so an absolute length in metres
    # says nothing about whether a hand is degenerate.
    forearm = max(span("LeftForeArm"), span("RightForeArm"), 1e-6)
    left, right = span("LeftHand"), span("RightHand")
    present = "LeftHand" in bones and "RightHand" in bones
    print(f"P64_HAND label={label} present={present} forearm={forearm:.4f} "
          f"hand_l={left:.4f} hand_r={right:.4f} "
          f"ratio_l={left / forearm:.3f} ratio_r={right / forearm:.3f}")


if __name__ == "__main__":
    main()
