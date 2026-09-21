"""Read-only source inspection for generation/donor preparation."""
import argparse, json, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from _common import cli_args, reset, import_model, inspect_scene, write_json
p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--report',required=True);a=p.parse_args(cli_args())
reset();import_model(a.input)
report=inspect_scene(a.input)
report['actions']=[dict(name=x.name,range=list(x.frame_range)) for x in bpy.data.actions]
report['skeletons']=[dict(name=o.name,bones=[dict(name=b.name,parent=b.parent.name if b.parent else None,head=list(o.matrix_world@b.head_local)) for b in o.data.bones]) for o in bpy.context.scene.objects if o.type=='ARMATURE']
report['images']=[dict(name=i.name,path=i.filepath,size=list(i.size),packed=bool(i.packed_file)) for i in bpy.data.images]
write_json(a.report,report);print('SOURCE_INSPECT_OK '+a.report)
