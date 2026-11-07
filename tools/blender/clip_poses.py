"""Find a pose inside a donor clip: dump the main bones' world positions through it, as JSON, for tools outside Blender to plot.

The lifecycle clips carry no kneel or slump of their own, but a getting-up clip passes through one. This is how the
yield pose was chosen.

  scripts/blender.ps1 -Script tools/blender/clip_poses.py -TimeoutSec 300 HeroRevive.fbx HeroDeath.fbx out.json
"""
import json
import sys
from pathlib import Path

import bpy

args = sys.argv[sys.argv.index("--") + 1:]
out = args[-1]
BONES = ("pelvis", "spine_03", "neck_01", "Head", "upperarm_l", "lowerarm_l", "hand_l", "upperarm_r", "lowerarm_r", "hand_r",
         "thigh_l", "calf_l", "foot_l", "thigh_r", "calf_r", "foot_r")
clips = {}
for fbx in args[:-1]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    for action in bpy.data.actions:
        arm.animation_data_create()
        arm.animation_data.action = action
        start, end = action.frame_range
        frames = []
        for i in range(0, 41):
            frame = start + (end - start) * i / 40
            bpy.context.scene.frame_set(int(frame), subframe=frame - int(frame))
            bpy.context.view_layer.update()
            pose = {name: list(arm.matrix_world @ arm.pose.bones[name].head) for name in BONES if name in arm.pose.bones}
            frames.append({"phase": i / 40, "bones": pose})
        clips[Path(fbx).stem + "/" + action.name.split("|")[-1]] = frames
Path(out).write_text(json.dumps(clips), encoding="utf-8")
print("CLIP_POSES_OK")
