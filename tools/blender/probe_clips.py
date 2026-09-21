"""Inspect actual limb variation in animation sources and stripped exports."""
import argparse,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from _common import cli_args,reset,import_model,write_json
p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--report',required=True);a=p.parse_args(cli_args())
reset();import_model(a.input);arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');arm.animation_data_create()
rows=[]
for action in bpy.data.actions:
 if action.name.split('|')[-1] not in {'Walk_Loop','Sword_Attack','Idle_Loop'}:continue
 arm.animation_data.action=action
 if hasattr(action,'slots') and len(action.slots):arm.animation_data.action_slot=action.slots[0]
 poses=[]
 for phase in (.15,.65):
  bpy.context.scene.frame_set(round(action.frame_range[0]+(action.frame_range[1]-action.frame_range[0])*phase));bpy.context.view_layer.update()
  poses.append({n:list(arm.pose.bones[n].matrix.to_quaternion()) for n in ['pelvis','upperarm_l','lowerarm_l','thigh_l','calf_l','hand_r'] if n in arm.pose.bones})
 rows.append(dict(action=action.name,frames=list(action.frame_range),slots=[s.identifier for s in action.slots],poses=poses))
write_json(a.report,dict(source=a.input,clips=rows));print('CLIP_PROBE_OK '+a.report)
