"""Export only the permitted UAL skeleton and selected clips; no donor mesh."""
import argparse, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from _common import cli_args, reset, import_model, export_model, write_json
p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--output',required=True);a=p.parse_args(cli_args())
reset();import_model(a.input)
keep={'Idle_Loop','Walk_Loop','Sword_Attack','Roll','Hit_Chest'}
for obj in list(bpy.data.objects):
 if obj.type!='ARMATURE':bpy.data.objects.remove(obj,do_unlink=True)
for action in list(bpy.data.actions):
 if action.name.split('|')[-1] not in keep:bpy.data.actions.remove(action,do_unlink=True)
 else:action.use_fake_user=True
for arm in bpy.context.scene.objects:
 if arm.animation_data:
  for track in list(arm.animation_data.nla_tracks):arm.animation_data.nla_tracks.remove(track)
  arm.animation_data.action=None
assert not any(o.type=='MESH' for o in bpy.context.scene.objects)
assert len(bpy.data.actions)==len(keep),[x.name for x in bpy.data.actions]
export_model(a.output)
write_json(str(Path(a.output).with_suffix('.json')),dict(source=a.input,meshes=0,actions=[x.name for x in bpy.data.actions]))
print('ANIMATION_DONOR_OK meshes=0 clips='+str(len(keep)))
