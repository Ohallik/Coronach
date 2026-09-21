"""Read-only source geometry, axes and animation inventory."""
import argparse,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from _common import cli_args,reset,import_model,world_bounds,write_json
p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--report',required=True);a=p.parse_args(cli_args())
reset();import_model(a.input)
write_json(a.report,dict(source=a.input,bounds=world_bounds(),clips=[dict(name=c.name,frames=list(c.frame_range)) for c in bpy.data.actions],bones=[dict(name=b.name,head=list(o.matrix_world@b.head_local),tail=list(o.matrix_world@b.tail_local)) for o in bpy.context.scene.objects if o.type=='ARMATURE' for b in o.data.bones]))
print('MODEL_INVENTORY_OK '+a.report)
